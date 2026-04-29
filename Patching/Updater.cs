namespace Cutulu.Patching;

using System.Collections.Concurrent;
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
    /// Downloads and applies all out-of-date files in <paramref name="manifest"/>.
    /// Files are patched in parallel; each file uses a download/write pipeline so
    /// downloads and disk writes overlap.
    /// Writes are atomic: a .patch.tmp file is verified then renamed into place.
    /// </summary>
    public async Task UpdateAsync(
        string localDir,
        Manifest manifest,
        Func<string, Task<byte[]>> downloadFunc,
        IProgress<PatchProgress> progress = null,
        CancellationToken token = default)
    {
        if (manifest.Version != Manifest.CurrentVersion)
            throw new InvalidOperationException(
                $"Manifest version {manifest.Version} != expected {Manifest.CurrentVersion}. Update your client.");

        var _localDir = new Directory(localDir, true);

        // Build chunk cache once, avoids thousands of filesystem Exists() calls
        var chunkDir = Path.Combine(localDir, "chunks");
        //System.IO.Directory.CreateDirectory(chunkDir); // No more chunk caching
        var cachedChunks = BuildChunkCache(chunkDir);

        Debug.Log("=== PATCH START ===");
        Debug.Log($"Files: {manifest.Files.Count}");

        var fileSemaphore = new SemaphoreSlim(MaxConcurrentFiles, MaxConcurrentFiles);

        var tasks = new List<Task>(manifest.Files.Count);
        foreach (var file in manifest.Files)
        {
            var fileKey = file.Key;
            var fileHashes = file.Value;
            tasks.Add(PatchFileAsync(fileKey, fileHashes, localDir, chunkDir, cachedChunks,
                                     manifest.ChunkSize, downloadFunc, progress, fileSemaphore, token));
        }

        await Task.WhenAll(tasks);

        Debug.Log("=== PATCH COMPLETE ===");
    }

    // -------------------------------------------------------------------------

    private async Task PatchFileAsync(
        string relativePath,
        List<string> hashes,
        string localDir,
        string chunkDir,
        ConcurrentDictionary<string, byte> cachedChunks,
        int chunkSize,
        Func<string, Task<byte[]>> downloadFunc,
        IProgress<PatchProgress> progress,
        SemaphoreSlim fileSemaphore,
        CancellationToken token)
    {
        await fileSemaphore.WaitAsync(token);
        try
        {
            var outPath = Path.Combine(localDir, relativePath);

            if (await IsUpToDate(outPath, hashes, chunkSize, token))
            {
                Debug.Log($"Skipping {relativePath} (up to date)");
                return;
            }

            new File(outPath).GetParentDirectory(true); // mkdir

            var tmpPath = outPath + ".patch.tmp";

            try
            {
                await DownloadAndWriteAsync(
                    relativePath, hashes, chunkDir, cachedChunks,
                    tmpPath, chunkSize, downloadFunc, progress, token);

                // Verify assembled file before replacing the real one
                await VerifyAssembledFile(tmpPath, hashes, chunkSize, token);

                // Atomic rename
                if (System.IO.File.Exists(outPath))
                    System.IO.File.Delete(outPath);
                System.IO.File.Move(tmpPath, outPath);

                Debug.Log($"Patched {relativePath}");
            }
            catch
            {
                // Clean up temp on any failure, don't leave partial file
                if (System.IO.File.Exists(tmpPath))
                    System.IO.File.Delete(tmpPath);
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
    /// - Producer: downloads chunks concurrently (bounded by MaxConcurrentDownloads).
    /// - Consumer: writes chunks to disk in order, buffering out-of-order arrivals.
    /// </summary>
    private async Task DownloadAndWriteAsync(
        string relativePath,
        List<string> hashes,
        string chunkDir,
        ConcurrentDictionary<string, byte> cachedChunks,
        string tmpPath,
        int chunkSize,
        Func<string, Task<byte[]>> downloadFunc,
        IProgress<PatchProgress> progress,
        CancellationToken token)
    {
        // Unbounded channel: producer is already bounded by the semaphore
        var channel = Channel.CreateBounded<(int index, byte[] data)>(new BoundedChannelOptions(32)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        var downloadSemaphore = new SemaphoreSlim(MaxConcurrentDownloads, MaxConcurrentDownloads);
        long bytesWritten = 0;

        // --- Producer ---
        var producer = Task.Run(async () =>
        {
            try
            {
                var downloadTasks = new List<Task>(hashes.Count);
                for (int i = 0; i < hashes.Count; i++)
                {
                    var index = i;
                    var hash = hashes[i];

                    async Task<byte[]> DownloadAndStore(string hash)
                    {
                        byte[] data = await downloadFunc.Invoke(hash);

                        // No more chunk caching
                        /*var localChunkPath = Path.Combine(chunkDir, hash);

                        try
                        {
                            using var fs = new System.IO.FileStream(
                                localChunkPath,
                                System.IO.FileMode.CreateNew,
                                System.IO.FileAccess.Write,
                                System.IO.FileShare.None);

                            await fs.WriteAsync(data, token);
                        }
                        catch (System.IO.IOException)
                        {
                            // already exists
                        }*/

                        cachedChunks.TryAdd(hash, 0);

                        return data;
                    }

                    await downloadSemaphore.WaitAsync(token);
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            byte[] data;

                            if (cachedChunks.TryGetValue(hash, out _))
                            {
                                var path = Path.Combine(chunkDir, hash);

                                try
                                {
                                    data = await new File(path).ReadAsync(token);

                                    if (!ChunkHash.Verify(data, hash))
                                    {
                                        System.IO.File.Delete(path);
                                        cachedChunks.TryRemove(hash, out _);
                                        data = await DownloadAndStore(hash);
                                    }
                                }
                                catch
                                {
                                    cachedChunks.TryRemove(hash, out _);
                                    data = await DownloadAndStore(hash);
                                }
                            }
                            else
                            {
                                data = await DownloadAndStore(hash);
                            }

                            if (data == null || data.Length == 0)
                                throw new Exception($"Empty chunk: {hash}");

                            if (!ChunkHash.Verify(data, hash))
                                throw new Exception($"Hash mismatch on chunk: {hash}");

                            await channel.Writer.WriteAsync((index, data), token);
                        }
                        finally
                        {
                            downloadSemaphore.Release();
                        }
                    }, token));
                }

                await Task.WhenAll(downloadTasks);
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, token);

        // --- Consumer: write in order ---
        var consumer = Task.Run(async () =>
        {
            await using var stream = new System.IO.FileStream(
                tmpPath,
                System.IO.FileMode.Create,
                System.IO.FileAccess.Write,
                System.IO.FileShare.None,
                bufferSize: chunkSize,
                useAsync: true);

            // Reorder buffer for chunks that arrive out of order
            var reorderBuffer = new Dictionary<int, byte[]>(16);
            int nextExpected = 0;
            int chunksWritten = 0;

            await foreach (var (index, data) in channel.Reader.ReadAllAsync(token))
            {
                reorderBuffer[index] = data;

                // Flush all consecutive chunks we now have
                while (reorderBuffer.TryGetValue(nextExpected, out var chunk))
                {
                    reorderBuffer.Remove(nextExpected);
                    await stream.WriteAsync(chunk, token);

                    bytesWritten += chunk.Length;
                    chunksWritten++;
                    nextExpected++;

                    progress?.Report(new PatchProgress(
                        relativePath, chunksWritten, hashes.Count, bytesWritten));
                }
            }

            await stream.FlushAsync(token);
        }, token);

        await Task.WhenAll(producer, consumer);
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies an assembled file by stream-hashing it in chunk-sized windows.
    /// Never loads the full file into memory.
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
            System.IO.FileShare.Read,
            bufferSize: chunkSize,
            useAsync: true);

        var buffer = new byte[chunkSize];
        for (int i = 0; i < expectedHashes.Count; i++)
        {
            token.ThrowIfCancellationRequested();

            int read = 0, offset = 0;
            while (offset < chunkSize)
            {
                read = await stream.ReadAsync(buffer.AsMemory(offset, chunkSize - offset), token);
                if (read == 0) break;
                offset += read;
            }

            var hash = ChunkHash.Compute(buffer.AsSpan(0, offset));
            if (!string.Equals(hash, expectedHashes[i], StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Post-write verification failed on chunk {i} of assembled file.");
        }
    }

    /// <summary>
    /// Stream-hashes the existing file in chunk-sized windows and compares against manifest.
    /// Never loads the full file into memory.
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
                bufferSize: chunkSize,
                useAsync: true);

            var buffer = new byte[chunkSize];
            int chunkIndex = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                int offset = 0;
                int read;
                while (offset < chunkSize)
                {
                    read = await stream.ReadAsync(buffer.AsMemory(offset, chunkSize - offset), token);
                    if (read == 0) break;
                    offset += read;
                }

                if (offset == 0) break; // EOF

                if (chunkIndex >= manifestHashes.Count) return false; // more chunks than expected

                var hash = ChunkHash.Compute(buffer.AsSpan(0, offset));
                if (!string.Equals(hash, manifestHashes[chunkIndex], StringComparison.OrdinalIgnoreCase))
                    return false;

                chunkIndex++;
            }

            return chunkIndex == manifestHashes.Count;
        }
        catch
        {
            return false; // Unreadable = not up to date
        }
    }

    /// <summary>
    /// Loads existing chunk filenames into a HashSet for O(1) lookups.
    /// </summary>
    private static ConcurrentDictionary<string, byte> BuildChunkCache(string chunkDir)
    {
        var set = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        if (!System.IO.Directory.Exists(chunkDir)) return set;

        foreach (var file in System.IO.Directory.GetFiles(chunkDir))
            set.TryAdd(System.IO.Path.GetFileName(file), 0);

        return set;
    }
}