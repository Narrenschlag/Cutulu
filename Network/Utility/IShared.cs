#if GODOT4_0_OR_GREATER
namespace Cutulu.Network
{
    using Godot;

    using Core;

    public interface IShared
    {
        public Node Client { get; set; }
        public Node Host { get; set; }

        public N Unpack<N>(Node parent, bool asClient)
        {
            if (this is not Node godot || godot.IsNull()) return default;

            var _node = asClient ? Client : Host;

            // IMPORTANT NOTE
            // If this won't work even though it should
            // It may be that your overwrite the host/client children with different scripts

            if (_node is not N n || _node.IsNull())
            {
                Debug.LogR($"[color=orange]Failed to unpack shared asset for [i]{(asClient ? "Client" : "Host")}[/i]. Check your SharedAsset scene ({godot.Name}, {typeof(N).Name} != {(_node.NotNull() ? _node.GetType().Name : "<null>")}).[/color]");
                godot.Destroy();
                return default;
            }

            parent.SetChild(_node);

            var children = godot.GetNodesInChildren<IShared>(false, 1);
            if (children.NotEmpty())
            {
                foreach (var child in children)
                {
                    if (child != Client && child != Host)
                    {
                        child.Unpack<Node>(_node, asClient);
                    }
                }
            }

            godot.Destroy();

            return n;
        }

        public static N Unpack<N>(PackedScene packed, Node parent, bool asClient)
        {
            if (packed.IsNull()) return default;

            var _node = packed.Instantiate<Node>(parent);

            if (_node is IShared shared && _node.NotNull())
            {
                var unpacked = shared.Unpack<N>(parent, asClient);

                //Debug.LogError($"Unpacking asset as typeof({typeof(N)}): {unpacked.NotNull()}"); //@{unpacked.GetParent().Name}");
                return unpacked;
            }

            else if (_node is N n && _node.NotNull())
            {
                //Debug.LogError($"Load asset typeof({n.GetType().Name}) as typeof({typeof(N)})");
                return n;
            }

            else
            {
                Debug.LogError($"scene {packed.ResourcePath} couldn't be instantiated as typeof({typeof(N).Name}) as it is typeof({_node.GetType().Name})");
                _node.Destroy();
                return default;
            }
        }

        public static string GetNameOf(PackedScene scene)
        {
            if (scene.ResourcePath.NotEmpty())
            {
                var split = scene.ResourcePath.Split('/', '\\', CONST.StringSplit);

                if (split.Size() > 0)
                    return split[^1];
            }

            return string.Empty;
        }
    }

    public static class SharedNodeUtility
    {
        public static N Unpack<N>(this PackedScene packed, Node parent, bool asClient)
        {
            return IShared.Unpack<N>(packed, parent, asClient);
        }

        public static N UnpackClient<N>(this PackedScene packed, Node parent)
        {
            return IShared.Unpack<N>(packed, parent, true);
        }

        public static N UnpackHost<N>(this PackedScene packed, Node parent)
        {
            return IShared.Unpack<N>(packed, parent, false);
        }
    }
}
#endif