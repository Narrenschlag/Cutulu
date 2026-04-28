#if GODOT4_0_OR_GREATER
namespace Cutulu.Patching;

using Godot;

public partial class PatchingTestScene : Node
{
    [Export] private string HostFileDirectory;
    [Export] private string HostPatchDataDirectory;
    [Export] private string ClientDirectory;

    public override void _Ready()
    {
        _ = new PatchingTest().StartTest(HostFileDirectory, HostPatchDataDirectory, ClientDirectory);
    }
}
#endif