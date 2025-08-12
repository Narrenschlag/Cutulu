namespace Cutulu.Network;

using Cutulu.Core;
using Godot;

public partial class SharedSplitter3D : Node3D, ISharedSplitter
{
    [Export] public Node[] ClientRemove { get; set; }
    [Export] public Node[] HostRemove { get; set; }

    [ExportGroup("IShared")]
    [Export] public Node Client { get; set; }
    [Export] public Node Host { get; set; }
    public Node[] Shared { get; set; }

    public void Split(Node parent, bool asClient)
    {
        if (asClient) ClientRemove.ClearAndDestroy();
        else HostRemove.ClearAndDestroy();
    }

    public void _Unpack(bool asClient) { }
}