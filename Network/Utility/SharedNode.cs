#if GODOT4_0_OR_GREATER
namespace Cutulu.Network
{
    using Godot;

    public partial class SharedNode : Node, IShared
    {
        [Export] public Node Client { get; set; }
        [Export] public Node Host { get; set; }
    }
}
#endif