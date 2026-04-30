namespace Cutulu.Patching;

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
    /// </summary>
    public async Task<Manifest> BuildAsync(
        string sourceDir,
        string outputDir,
        int chunkSize = DefaultChunkSize,
        CancellationToken token = default)
    {
        var _sourceDir = new Directory(sourceDir, false);
        var manifest = new Manifest { ChunkSize = chunkSize };

        if (!_sourceDir.Exists()) return manifest;

        var chunkDir = Path.Combine(outputDir, "chunks/");
        _ = new Directory(chunkDir, true);

        File[] files = [.. _sourceDir.GetAllFiles()];

        // Lock for manifest.Files — parallel writes need coordination
        var dictLock = new object();

        await Parallel.ForEachAsync(
            files,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = token
            },
            async (file, ct) =>
            {
                var relativePath = Path.GetRelativePath(sourceDir, file.SystemPath);
                var bytes = await file.ReadAsync(ct);
                var chunkHashes = new List<string>();

                for (int i = 0; i < bytes.Length; i += chunkSize)
                {
                    ct.ThrowIfCancellationRequested();

                    var size = Math.Min(chunkSize, bytes.Length - i);

                    var chunk = new byte[size];
                    Buffer.BlockCopy(bytes, i, chunk, 0, size);

                    var hash = ChunkHash.Compute(chunk);

                    var chunkFile = new File(Path.Combine(chunkDir, hash));
                    if (!chunkFile.Exists())
                        await chunkFile.WriteAsync(chunk, ct);

                    chunkHashes.Add(hash);
                }

                lock (dictLock)
                    manifest.Files[relativePath] = chunkHashes;
            });

        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        await new File(Path.Combine(outputDir, "manifest.json")).WriteTextAsync(json, token);

        return manifest;
    }
}