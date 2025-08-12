namespace Cutulu.Network;

using Cutulu.Core;
using Godot;

public partial class SharedSplitter3D : Node3D, ISharable
{
    [Export] private bool DestroyByDefault { get; set; } = true;
    [Export] public Node[] ClientExclusive { get; set; }
    [Export] public Node[] HostExclusive { get; set; }

    public virtual T Unpack<T>(Node parent, bool asClient)
    {
        var array = asClient ? HostExclusive : ClientExclusive;

        if (array.NotEmpty())
        {
            foreach (var item in array)
                if (item.NotNull()) Disable(item);
        }

        parent.SetChild(this);

        return this is T t ? t : default;
    }

    public virtual void Disable(Node node)
    {
        if (DestroyByDefault) node.Destroy();
        else node.SetActive(false);
    }
}