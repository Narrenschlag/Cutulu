#if GODOT4_0_OR_GREATER
namespace Cutulu.GPU;

using Cutulu.Core;
using Godot;

[GlobalClass]
public partial class ComputeShaderTest : Node
{
    [Export] private string ShaderFilePath;

    public override void _Ready()
    {
        float[] input = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f];

        var manager = new GPUComputeManager();
        manager.LoadShader(".", ShaderFilePath);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var output = manager.ExecuteCompute(".", input, input.Length / 10, 1, 1);

        stopwatch.Stop();

        Debug.Log($"Output: {string.Join(", ", output)}");

        Debug.Log($"GPU time: {stopwatch.ElapsedMilliseconds}ms");

        Application.Quit();
    }
}
#endif