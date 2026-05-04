namespace Cutulu.Patching;

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System;
using Core;

/// <summary>
/// Progress snapshot reported after each chunk is written to disk.
/// </summary>
public readonly struct PatchProgress
{
    public readonly string FileName;
    public readonly int ChunksWritten;
    public readonly int TotalChunks;
    public readonly long BytesWritten;

    public PatchProgress(string fileName, int chunksWritten, int totalChunks, long bytesWritten)
    {
        FileName = fileName;
        ChunksWritten = chunksWritten;
        TotalChunks = totalChunks;
        BytesWritten = bytesWritten;
    }

    public float Fraction => TotalChunks > 0 ? (float)ChunksWritten / TotalChunks : 0f;
}

public class Updater
{
    /// <summary>Max files patched simultaneously.</summary>
    public int MaxConcurrentFiles { get; set; } = 3;

    /// <summary>Max simultaneous chunk downloads per file.</summary>
    public int MaxConcurrentDownloads { get; set; } = 4;

    /// <summary>
    /// Downloads and applies all out-of-date files described in <paramref name="manifest"/>.
    /// Files are patched in parallel; each file uses a producer/consumer pipeline so
    /// downloads and disk writes overlap. Writes are atomic: a .patch.tmp file is
    /// verified chunk-by-chunk then renamed into place.
    /// </summary>
    public async Task UpdateAsync(
        string localDir,
        Manifest manifest,
        Func<string, Task<byte[]>> downloadFunc,
        IProgress<PatchProgress> progress = null,
        CancellationToken token = default)
    {
        if (manifest == null) throw new ArgumentNullException(nameof(manifest));
        if (downloadFunc == null) throw new ArgumentNullException(nameof(downloadFunc));

        if (manifest.Version != Manifest.CurrentVersion)
            throw new InvalidOperationException(
                $"Manifest version {manifest.Version} != expected {Manifest.CurrentVersion}.");

        _ = new Directory(localDir, true);

        Debug.Log("=== PATCH START ===");
        Debug.Log($"Files: {manifest.Files.Count}");

        int totalChunks = 0;
        foreach (var f in manifest.Files)
            totalChunks += f.Value?.Count ?? 0;

        int globalWritten = 0;

        // Wrap the caller's progress so we report a global running total.
        IProgress<PatchProgress> globalProgress = progress == null ? null : new Progress<PatchProgress>(p =>
        {
            int written = Interlocked.Increment(ref globalWritten);
            progress.Report(new PatchProgress(p.FileName, written, totalChunks, p.BytesWritten));
        });

        using var fileSemaphore = new SemaphoreSlim(MaxConcurrentFiles, MaxConcurrentFiles);

        var tasks = new List<Task>(manifest.Files.Count);

        foreach (var (key, hashes) in manifest.Files)
        {
            if (hashes == null || hashes.Count == 0) continue;

            tasks.Add(PatchFileAsync(
                key,
                hashes,
                localDir,
                manifest.ChunkSize,
                downloadFunc,
                globalProgress,
                fileSemaphore,
                token));
        }

        await Task.WhenAll(tasks);

        Debug.Log("=== PATCH COMPLETE ===");
    }

    // -------------------------------------------------------------------------

    private async Task PatchFileAsync(
        string relativePath,
        List<string> hashes,
        string localDir,
        int chunkSize,
        Func<string, Task<byte[]>> downloadFunc,
        IProgress<PatchProgress> progress,
        SemaphoreSlim fileSemaphore,
        CancellationToken token)
    {
        await fileSemaphore.WaitAsync(token);
        try
        {
            // Normalise to forward-slash, strip leading separators, forbid traversal.
            relativePath = relativePath
                .Replace('\\', '/')
                .TrimStart('/')
                .Replace("..", string.Empty);

            if (string.IsNullOrWhiteSpace(relativePath))
                throw new InvalidOperationException("Manifest contains an empty or invalid file path.");

            var outPath = Path.Combine(localDir, relativePath);

            if (await IsUpToDate(outPath, hashes, chunkSize, token))
            {
                Debug.Log($"Skipping {relativePath} (up to date)");
                // Still advance the global progress counter for all chunks.
                if (progress != null)
                    foreach (var _ in hashes)
                        progress.Report(new PatchProgress(relativePath, 0, hashes.Count, 0));
                return;
            }

            // Ensure parent directory exists.
            var parentDir = System.IO.Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(parentDir))
                System.IO.Directory.CreateDirectory(parentDir);

            var tmpPath = outPath + ".patch.tmp";

            try
            {
                await DownloadAndWriteAsync(
                    relativePath, hashes, tmpPath, chunkSize, downloadFunc, progress, token);

                await VerifyAssembledFile(tmpPath, hashes, chunkSize, token);

                // Atomic replace.
                System.IO.File.Move(tmpPath, outPath, overwrite: true);

                Debug.Log($"Patched {relativePath}");
            }
            catch
            {
                SafeDelete(tmpPath);
                throw;
            }
        }
        finally
        {
            fileSemaphore.Release();
        }
    }

    /// <summary>
    /// Producer/consumer pipeline:
    ///   Producer  — downloads chunks concurrently (bounded by MaxConcurrentDownloads).
    ///   Consumer  — writes chunks to disk in strict order, buffering out-of-order arrivals.
    /// The channel backpressure prevents the producer from racing too far ahead of the consumer.
    /// </summary>
    private async Task DownloadAndWriteAsync(
        string relativePath,
        List<string> hashes,
        string tmpPath,
        int chunkSize,
        Func<string, Task<byte[]>> downloadFunc,
        IProgress<PatchProgress> progress,
        CancellationToken token)
    {
        // Bounded channel keeps memory usage proportional to MaxConcurrentDownloads.
        var channel = Channel.CreateBounded<(int index, byte[] data)>(new BoundedChannelOptions(MaxConcurrentDownloads * 2)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        using var downloadSemaphore = new SemaphoreSlim(MaxConcurrentDownloads, MaxConcurrentDownloads);
        long bytesWritten = 0;

        // --- Producer ---
        var producer = Task.Run(async () =>
        {
            try
            {
                var downloadTasks = new Task[hashes.Count];

                for (int i = 0; i < hashes.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    int index = i;
                    string hash = hashes[i];

                    await downloadSemaphore.WaitAsync(token);

                    downloadTasks[index] = Task.Run(async () =>
                    {
                        try
                        {
                            byte[] data = await DownloadChunkWithRetry(hash, downloadFunc, token);

                            if (data == null || data.Length == 0)
                                throw new InvalidOperationException($"Server returned empty chunk: {hash}");

                            if (!ChunkHash.Verify(data, hash))
                                throw new InvalidOperationException($"Hash mismatch on downloaded chunk: {hash}");

                            await channel.Writer.WriteAsync((index, data), token);
                        }
                        finally
                        {
                            downloadSemaphore.Release();
                        }
                    }, token);
                }

                await Task.WhenAll(downloadTasks);
            }
            finally
            {
                // Always complete the writer so the consumer can finish.
                channel.Writer.Complete();
            }
        }, token);

        // --- Consumer ---
        var consumer = Task.Run(async () =>
        {
            await using var stream = new System.IO.FileStream(
                tmpPath,
                System.IO.FileMode.Create,
                System.IO.FileAccess.Write,
                System.IO.FileShare.None,
                bufferSize: Math.Max(chunkSize, 4096),
                useAsync: true);

            var reorderBuffer = new Dictionary<int, byte[]>();
            int nextExpected = 0;
            int chunksWritten = 0;

            await foreach (var (index, data) in channel.Reader.ReadAllAsync(token))
            {
                reorderBuffer[index] = data;

                while (reorderBuffer.TryGetValue(nextExpected, out var chunk))
                {
                    reorderBuffer.Remove(nextExpected);
                    await stream.WriteAsync(chunk, token);

                    bytesWritten += chunk.Length;
                    chunksWritten++;
                    nextExpected++;

                    progress?.Report(new PatchProgress(relativePath, chunksWritten, hashes.Count, bytesWritten));
                }
            }

            // Guard against a manifest that lists more chunks than we received.
            if (nextExpected != hashes.Count)
                throw new InvalidOperationException(
                    $"Expected {hashes.Count} chunks for '{relativePath}' but received {nextExpected}.");

            await stream.FlushAsync(token);
        }, token);

        // Propagate exceptions from both sides cleanly.
        await Task.WhenAll(producer, consumer);
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Retries a chunk download up to 3 times with an exponential back-off.
    /// </summary>
    private static async Task<byte[]> DownloadChunkWithRetry(
        string hash,
        Func<string, Task<byte[]>> downloadFunc,
        CancellationToken token,
        int maxAttempts = 3)
    {
        int delay = 500; // ms

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                return await downloadFunc(hash);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                Debug.LogError($"Chunk {hash} download attempt {attempt} failed: {ex.Message}. Retrying in {delay} ms…");
                await Task.Delay(delay, token);
                delay *= 2;
            }
        }

        // Final attempt — let any exception propagate.
        return await downloadFunc(hash);
    }

    /// <summary>
    /// Stream-hashes the assembled temp file in chunk-sized windows and compares
    /// against the manifest. Never loads the full file into memory.
    /// </summary>
    private static async Task VerifyAssembledFile(
        string path,
        List<string> expectedHashes,
        int chunkSize,
        CancellationToken token)
    {
        await using var stream = new System.IO.FileStream(
            path,
            System.IO.FileMode.Open,
            System.IO.FileAccess.Read,
            System.IO.FileShare.None,
            bufferSize: Math.Max(chunkSize, 4096),
            useAsync: true);

        var buffer = new byte[chunkSize];

        for (int i = 0; i < expectedHashes.Count; i++)
        {
            token.ThrowIfCancellationRequested();

            int offset = 0;
            while (offset < chunkSize)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, chunkSize - offset), token);
                if (read == 0) break;
                offset += read;
            }

            if (offset == 0 && i < expectedHashes.Count)
                throw new InvalidOperationException(
                    $"File truncated: expected chunk {i} but stream ended.");

            if (!ChunkHash.Verify(buffer.AsSpan(0, offset), expectedHashes[i]))
                throw new InvalidOperationException(
                    $"Post-write verification failed on chunk {i}.");
        }

        // Ensure there is no extra data beyond what the manifest describes.
        int trailing = await stream.ReadAsync(buffer.AsMemory(0, 1), token);
        if (trailing != 0)
            throw new InvalidOperationException("Assembled file is larger than the manifest describes.");
    }

    /// <summary>
    /// Stream-hashes the on-disk file and compares against the manifest.
    /// Returns false on any I/O error so the file gets re-patched rather than crash.
    /// </summary>
    private static async Task<bool> IsUpToDate(
        string path,
        List<string> manifestHashes,
        int chunkSize,
        CancellationToken token)
    {
        if (!System.IO.File.Exists(path)) return false;

        try
        {
            await using var stream = new System.IO.FileStream(
                path,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read,
                System.IO.FileShare.Read,
                bufferSize: Math.Max(chunkSize, 4096),
                useAsync: true);

            var buffer = new byte[chunkSize];
            int chunkIndex = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                int offset = 0;
                while (offset < chunkSize)
                {
                    int read = await stream.ReadAsync(buffer.AsMemory(offset, chunkSize - offset), token);
                    if (read == 0) break;
                    offset += read;
                }

                if (offset == 0) break; // clean EOF

                if (chunkIndex >= manifestHashes.Count) return false; // file is larger than expected

                if (!ChunkHash.Verify(buffer.AsSpan(0, offset), manifestHashes[chunkIndex]))
                    return false;

                chunkIndex++;
            }

            return chunkIndex == manifestHashes.Count;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false; // Unreadable file → re-patch.
        }
    }

    private static void SafeDelete(string path)
    {
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        catch { /* best-effort, don't mask the original exception */ }
    }
}