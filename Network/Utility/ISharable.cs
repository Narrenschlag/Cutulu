#if GODOT4_0_OR_GREATER
namespace Cutulu.Network;

using Godot;

public interface ISharable
{
    public T Unpack<T>(Node parent, bool asClient);
    public bool DestroyAfterUnpacking();
}
#endif