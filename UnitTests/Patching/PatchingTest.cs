namespace Cutulu.Patching;

using System.Threading.Tasks;
using Core;

public partial class PatchingTest
{
    public async Task StartTest(string hostFileDirectory, string hostPatchDataDirectory, string clientDirectory)
    {
        Debug.Log("===== START =====");

        try
        {
            Debug.Log("» Host");
            var manifest = await StartHost(hostFileDirectory, hostPatchDataDirectory);

            Debug.Log("» Client");
            await StartClient(clientDirectory, hostPatchDataDirectory + "/chunks", manifest);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }

        Debug.Log("===== DONE =====");
    }

    public async Task<Manifest> StartHost(string fileDirectory, string pathDataDirectory)
    {
        var builder = new Builder();

        return await builder.BuildAsync(
            sourceDir: fileDirectory,
            outputDir: pathDataDirectory
        );
    }

    public async Task StartClient(string directory, string chunkDir, Manifest manifest)
    {
        var gameDir = Path.Combine(directory, "Game/");
        var updater = new Updater();

        Debug.Log(">>> CLIENT START");

        await updater.UpdateAsync(
            gameDir,
            manifest,
            async hash =>
            {
                var path = Path.Combine(chunkDir, hash);
                return await System.IO.File.ReadAllBytesAsync(path);

                // Real version:
                // return await httpClient.GetByteArrayAsync(url + hash);
            }
        );

        Debug.Log(">>> CLIENT DONE");
    }
}