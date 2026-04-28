namespace Cutulu.Patching;

using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using Core;

public class Builder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private const int ChunkSize = 1024 * 1024;

    public async Task<Manifest> BuildAsync(string sourceDir, string outputDir)
    {
        var _sourceDir = new Directory(sourceDir, false);

        var manifest = new Manifest();
        if (_sourceDir.Exists() == false) return manifest;

        var chunkDir = Path.Combine(outputDir, "chunks/");
        _ = new Directory(chunkDir, true);

        File[] _files = [.. _sourceDir.GetAllFiles()];

        foreach (var file in _files)
        {
            var relativePath = Path.GetRelativePath(sourceDir, file.SystemPath);

            var bytes = await file.ReadAsync();

            var chunkHashes = new List<string>();

            for (int i = 0; i < bytes.Length; i += ChunkSize)
            {
                var size = Math.Min(ChunkSize, bytes.Length - i);
                var chunk = new byte[size];
                Array.Copy(bytes, i, chunk, 0, size);

                var hash = Hash(chunk);
                var chunkFile = new File(Path.Combine(chunkDir, hash));

                if (chunkFile.Exists() == false)
                    await chunkFile.WriteAsync(chunk);

                chunkHashes.Add(hash);
            }

            manifest.Files[relativePath] = chunkHashes;
        }

        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        await new File(Path.Combine(outputDir, "manifest.json"))
        .WriteTextAsync(json);

        return manifest;
    }

    private string Hash(byte[] data)
    {
        return Convert.ToHexString(SHA256.HashData(data));
    }
}