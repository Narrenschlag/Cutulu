namespace Cutulu.Patching;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System;
using Core;

public class Builder
{
    public const int DefaultChunkSize = 1024 * 1024; // 1 MB

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Scans <paramref name="sourceDir"/>, chunks every file, writes chunks to
    /// <paramref name="outputDir"/>/chunks/, and writes manifest.json.
    /// Chunk files are written atomically via a .tmp rename so a crashed build
    /// never leaves a corrupt chunk on disk.
    /// </summary>
    public async Task<Manifest> BuildAsync(
        string sourceDir,
        string outputDir,
        int chunkSize = DefaultChunkSize,
        CancellationToken token = default)
    {
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

        var _sourceDir = new Directory(sourceDir, false);
        if (!_sourceDir.Exists()) return new Manifest { ChunkSize = chunkSize };

        var chunkDir = Path.Combine(outputDir, "chunks/");
        _ = new Directory(chunkDir, true);

        // Collect all source files up-front so we can detect duplicates early.
        File[] files = [.. _sourceDir.GetAllFiles()];

        // Thread-safe results collection; merged into manifest after all tasks finish.
        var results = new ConcurrentDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Track which chunk hashes are already written to avoid redundant I/O.
        // Value is unused — we only care about key presence.
        var writtenChunks = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        // Pre-populate from chunks that survived a previous (possibly partial) build.
        foreach (var existing in System.IO.Directory.GetFiles(chunkDir))
            writtenChunks.TryAdd(System.IO.Path.GetFileName(existing), 0);

        await Parallel.ForEachAsync(
            files,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = token
            },
            async (file, ct) =>
            {
                var relativePath = Path.GetRelativePath(sourceDir, file.SystemPath)
                    .Replace('\\', '/');

                byte[] bytes = await file.ReadAsync(ct);

                // Empty files are valid; produce one zero-length chunk so the
                // manifest records them and they get restored on the client side.
                if (bytes.Length == 0)
                {
                    var hash = ChunkHash.Compute(Array.Empty<byte>());
                    await WriteChunkAtomicAsync(chunkDir, hash, Array.Empty<byte>(), writtenChunks, ct);
                    results[relativePath] = [hash];
                    return;
                }

                var hashes = new List<string>(bytes.Length / chunkSize + 1);

                for (int i = 0; i < bytes.Length; i += chunkSize)
                {
                    ct.ThrowIfCancellationRequested();

                    int size = Math.Min(chunkSize, bytes.Length - i);
                    var chunk = new ReadOnlyMemory<byte>(bytes, i, size);
                    var hash = ChunkHash.Compute(chunk.Span);

                    await WriteChunkAtomicAsync(chunkDir, hash, chunk.ToArray(), writtenChunks, ct);
                    hashes.Add(hash);
                }

                results[relativePath] = hashes;
            });

        var manifest = new Manifest
        {
            ChunkSize = chunkSize,
            Files = new Dictionary<string, List<string>>(results, StringComparer.OrdinalIgnoreCase)
        };

        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        await WriteFileAtomicAsync(Path.Combine(outputDir, "manifest.json"), json, token);

        return manifest;
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Writes <paramref name="data"/> to <c>chunkDir/hash</c> only if that file
    /// does not already exist, using a .tmp → rename pattern so the file is
    /// either fully written or absent.
    /// </summary>
    private static async Task WriteChunkAtomicAsync(
        string chunkDir,
        string hash,
        byte[] data,
        ConcurrentDictionary<string, byte> writtenChunks,
        CancellationToken token)
    {
        // Fast path: already present from this build or a prior one.
        if (!writtenChunks.TryAdd(hash, 0)) return;

        var finalPath = Path.Combine(chunkDir, hash);

        // Another parallel task may have just written it.
        if (System.IO.File.Exists(finalPath)) return;

        var tmpPath = finalPath + ".tmp";

        try
        {
            await System.IO.File.WriteAllBytesAsync(tmpPath, data, token);

            // Verify before committing so a disk error during write is caught.
            if (!ChunkHash.Verify(data, hash))
                throw new InvalidOperationException($"Hash mismatch after writing chunk {hash}.");

            // Replace instead of Move to handle the edge case where the final
            // file was created by a concurrent task between our Exists() check
            // and now.
            System.IO.File.Move(tmpPath, finalPath, overwrite: false);
        }
        catch (System.IO.IOException)
        {
            // If another task won the race, the final file now exists — that is fine.
            if (!System.IO.File.Exists(finalPath))
                throw;
        }
        finally
        {
            // Best-effort cleanup of the temp file.
            try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); }
            catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Writes UTF-8 text atomically via a .tmp → rename pattern.
    /// </summary>
    private static async Task WriteFileAtomicAsync(
        string path,
        string text,
        CancellationToken token)
    {
        var tmpPath = path + ".tmp";

        try
        {
            await System.IO.File.WriteAllTextAsync(tmpPath, text, token);
            System.IO.File.Move(tmpPath, path, overwrite: true);
        }
        catch
        {
            try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); }
            catch { /* ignore */ }
            throw;
        }
    }
}