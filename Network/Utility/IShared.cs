namespace Cutulu.Network
{
    using Godot;

    using Core;

    public interface IShared
    {
        public Node Client { get; set; }
        public Node Host { get; set; }

        public N Unpack<N>(Node parent, bool asClient) where N : Node
        {
            if (this is not Node godot || godot.IsNull()) return null;

            var node = asClient ? Client : Host;

            // IMPORTANT NOTE
            // If this won't work even though it should
            // It may be that your overwrite the host/client children with different scripts

            if (node is not N n || n.IsNull())
            {
                Debug.LogR($"[color=orange]Failed to unpack shared asset for [i]{(asClient ? "Client" : "Host")}[/i]. Check your SharedAsset scene ({godot.Name}, {typeof(N).Name} != {(node.NotNull() ? node.GetType().Name : "<null>")}).[/color]");
                godot.Destroy();
                return null;
            }

            parent.SetChild(n);

            var children = godot.GetNodesInChildren<IShared>(false, 1);
            if (children.NotEmpty())
            {
                foreach (var child in children)
                {
                    if (child != Client && child != Host)
                    {
                        var unpacked = child.Unpack<Node>(n, asClient);
                    }
                }
            }

            godot.Destroy();

            return n;
        }

        public static N Unpack<N>(PackedScene packed, Node parent, bool asClient) where N : Node
        {
            if (packed.IsNull()) return null;

            var node = packed.Instantiate<Node>(parent);

            if (node is IShared shared && node.NotNull())
            {
                var unpacked = ((IShared)shared).Unpack<N>(parent, asClient);

                //Debug.LogError($"Unpacking asset as typeof({typeof(N)}): {unpacked.NotNull()}"); //@{unpacked.GetParent().Name}");
                return unpacked;
            }

            else if (node is N n && n.NotNull())
            {
                //Debug.LogError($"Load asset typeof({n.GetType().Name}) as typeof({typeof(N)})");
                return n;
            }

            else
            {
                Debug.LogError($"scene {packed.ResourcePath} couldn't be instantiated as typeof({typeof(N).Name}) as it is typeof({node.GetType().Name})");
                node.Destroy();
                return null;
            }
        }

        public static string GetNameOf(PackedScene scene)
        {
            if (scene.ResourcePath.NotEmpty())
            {
                var split = scene.ResourcePath.Split('/', '\\', Constant.StringSplit);

                if (split.Size() > 0)
                    return split[^1];
            }

            return string.Empty;
        }
    }
}