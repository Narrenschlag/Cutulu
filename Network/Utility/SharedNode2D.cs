#if GODOT4_0_OR_GREATER
namespace Cutulu.Network
{
    using Godot;

    public partial class SharedNode2D : Node2D, IShared
    {
        [Export] public Node Client { get; set; }
        [Export] public Node Host { get; set; }
        
        [Export] public Node[] Shared { get; set; }
        
        public virtual void _Unpack(bool asClient) { }
    }
}
#endif