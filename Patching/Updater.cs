namespace Cutulu.Patching;

using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Core;

public class Updater
{
    public async Task UpdateAsync(
        string localDir,
        Manifest manifest,
        Func<string, Task<byte[]>> downloadFunc)
    {
        var _localDir = new Directory(localDir, true);

        Debug.Log("=== PATCH START ===");
        Debug.Log($"Files: {manifest.Files.Count}");

        foreach (var file in manifest.Files)
        {
            var outPath = Path.Combine(localDir, file.Key);

            if (await IsUpToDate(outPath, file.Value))
            {
                Debug.Log($"Skipping {file.Key} (up to date)");
                continue;
            }

            var _file = new File(outPath);
            _file.GetParentDirectory(true); // mkdir

            await using var stream = new System.IO.FileStream(
                outPath,
                System.IO.FileMode.Create,
                System.IO.FileAccess.Write
            );

            Debug.Log($"Writing {file.Key}");

            foreach (var hash in file.Value)
            {
                byte[] data;

                if (await HasChunk(hash, localDir))
                {
                    data = await new File(Path.Combine(localDir, "chunks", hash)).ReadAsync();
                }
                else
                {
                    data = await downloadFunc(hash);
                }

                if (data == null || data.Length == 0)
                    throw new Exception($"Empty chunk: {hash}");

                if (!string.Equals(Hash(data), hash, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Hash mismatch: {hash}");

                await stream.WriteAsync(data);
            }

            Debug.Log($"Wrote {file.Key}");
        }

        Debug.Log("=== PATCH COMPLETE ===");
    }

    private string Hash(byte[] data)
    {
        return Convert.ToHexString(SHA256.HashData(data)).ToUpperInvariant();
    }

    private async Task<bool> HasChunk(string hash, string localDir)
    {
        var chunkPath = Path.Combine(localDir, "chunks", hash);
        return new File(chunkPath).Exists();
    }

    private async Task<bool> IsUpToDate(string path, List<string> manifestHashes)
    {
        var file = new File(path);
        if (!file.Exists()) return false;

        var bytes = await file.ReadAsync();
        if (bytes.IsEmpty()) return false;

        var localHashes = new List<string>();

        for (int i = 0; i < bytes.Length; i += 1024 * 1024)
        {
            var size = Math.Min(1024 * 1024, bytes.Length - i);
            var chunk = new byte[size];
            Array.Copy(bytes, i, chunk, 0, size);

            localHashes.Add(Hash(chunk));
        }

        if (localHashes.Count != manifestHashes.Count)
            return false;

        for (int i = 0; i < localHashes.Count; i++)
        {
            if (!string.Equals(localHashes[i], manifestHashes[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}