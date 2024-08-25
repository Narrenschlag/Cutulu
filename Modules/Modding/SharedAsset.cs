using Cutulu;
using Godot;

namespace Cutulu.Modding
{
    public partial class SharedAsset : Node
    {
        [Export] private Node Client { get; set; }
        [Export] private Node Host { get; set; }

        public N Unpack<N>(Node parent, bool asClient) where N : Node
        {
            if ((asClient ? Client : Host) is not N n)
            {
                this.Destroy();
                return null;
            }

            parent.SetChild(n);
            this.Destroy();

            return n;
        }

        public static string GetNameOf(PackedScene scene)
        {
            if (scene.ResourcePath.NotEmpty())
            {
                var split = scene.ResourcePath.Split('/', '\\', Core.StringSplit);

                if (split.Size() > 0)
                    return split[^1];
            }

            return string.Empty;
        }

        public static N Instantiate<N>(PackedScene packed, Node parent, bool asClient) where N : Node
        {
            if (packed.IsNull()) return null;

            var node = packed.Instantiate<Node>();
            if (node is SharedAsset shared)
            {
                return shared.Unpack<N>(parent, asClient);
            }

            else if (node is N n)
            {
                return n;
            }

            else
            {
                node.Destroy();
                return null;
            }
        }
    }
}