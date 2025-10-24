namespace Cutulu.Async;

using System.Threading.Tasks;

public static class TaskUtils
{
#if GODOT4_0_OR_GREATER
    public static async Task NextFrame()
    {
        var tree = (Godot.SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, Godot.SceneTree.SignalName.ProcessFrame);
    }

    public static async Task WaitFrames(int frames)
    {
        var tree = (Godot.SceneTree)Godot.Engine.GetMainLoop();
        for (int i = 0; i < frames; i++)
            await tree.ToSignal(tree, Godot.SceneTree.SignalName.ProcessFrame);
    }
#endif
}