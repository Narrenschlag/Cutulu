namespace Cutulu.Modding
{
    using Godot;

    public partial class SharedAsset : Node
    {
        [Export] private Node Client { get; set; }
        [Export] private Node Host { get; set; }

        public N Unpack<N>(Node parent, bool asClient) where N : Node
        {
            if ((asClient ? Client : Host) is not N n)
            {
                Debug.LogR($"[color=orange]Failed to unpack shared asset for [i]{(asClient ? "Client" : "Host")}[/i] as [i]{typeof(N).Name}[/i]. Check your SharedAsset scene.[/color]");
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

            var node = packed.Instantiate<Node>(parent);
            if (node is N n && n.NotNull())
            {
                //Debug.LogError($"Load asset typeof({n.GetType().Name}) as typeof({typeof(N)})");
                return n;
            }

            else if (node is SharedAsset shared && shared.NotNull())
            {
                var unpacked = shared.Unpack<N>(parent, asClient);

                //Debug.LogError($"Unpacking asset as typeof({typeof(N)}): {unpacked.NotNull()}"); //@{unpacked.GetParent().Name}");
                return unpacked;
            }

            else
            {
                //Debug.LogError($"scene {packed.ResourcePath} couldn't be instantiated as typeof({typeof(N).Name}) as it is typeof({node.GetType().Name})");
                node.Destroy();
                return null;
            }
        }
    }
}