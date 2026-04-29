namespace Cutulu.Patching;

using System.Threading.Tasks;
using System.Threading;
using System;
using Core;

public partial class PatchingTest
{
    public async Task StartTest(
        string hostFileDirectory,
        string hostPatchDataDirectory,
        string clientDirectory,
        CancellationToken token = default)
    {
        Debug.Log("===== START =====");
        try
        {
            Debug.Log("» Host");
            var manifest = await StartHost(hostFileDirectory, hostPatchDataDirectory, token);

            Debug.Log("» Client");
            await StartClient(clientDirectory, hostPatchDataDirectory + "/chunks", manifest, token);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        Debug.Log("===== DONE =====");
    }

    public async Task<Manifest> StartHost(
        string fileDirectory,
        string patchDataDirectory,
        CancellationToken token = default)
    {
        var builder = new Builder();
        return await builder.BuildAsync(
            sourceDir: fileDirectory,
            outputDir: patchDataDirectory,
            token: token);
    }

    public async Task StartClient(
        string directory,
        string chunkDir,
        Manifest manifest,
        CancellationToken token = default
    )
    {
        var gameDir = Path.Combine(directory, "Game/");
        var updater = new Updater();

        // Wire up a simple progress callback — replace with your UI update logic
        var progress = new Progress<PatchProgress>(p =>
            Debug.Log($"  {p.FileName} [{p.ChunksWritten}/{p.TotalChunks}] {p.Fraction * 100:0}%"));

        Debug.Log(">>> CLIENT START");
        await updater.UpdateAsync(
            gameDir,
            manifest,
            async hash =>
            {
                var path = Path.Combine(chunkDir, hash);
                return await System.IO.File.ReadAllBytesAsync(path, token);
                // Real version:
                // return await httpClient.GetByteArrayAsync(url + hash, token);
            },
            progress,
            token);
        Debug.Log(">>> CLIENT DONE");
    }

    public async Task StartClient(
        string directory,
        Manifest manifest,
        CancellationToken token = default
    )
    {
        var http = new System.Net.Http.HttpClient
        {
            BaseAddress = new Uri("https://yourserver.com/chunks/")
        };

        async Task<byte[]> DownloadChunk(string hash)
        {
            var response = await http.GetAsync(hash, token);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(token);
        }

        var gameDir = Path.Combine(directory, "Game/");
        var updater = new Updater();

        // Wire up a simple progress callback — replace with your UI update logic
        var progress = new Progress<PatchProgress>(p =>
            Debug.Log($"  {p.FileName} [{p.ChunksWritten}/{p.TotalChunks}] {p.Fraction * 100:0}%"));

        Debug.Log(">>> CLIENT START");
        await updater.UpdateAsync(
            gameDir,
            manifest,
            DownloadChunk,
            progress,
            token
        );
        Debug.Log(">>> CLIENT DONE");
    }

    // Example server code:
    /*app.MapGet("/chunks/{hash}", async(string hash) =>
    {
        var path = Path.Combine("chunks", hash);

        if (!File.Exists(path))
            return Results.NotFound();

        return Results.File(path);
    });*/
}