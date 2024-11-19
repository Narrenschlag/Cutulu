namespace Cutulu
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Godot;

    public static class NodeExtension
    {
        public static bool HasNoParent(this Node node) => node.IsNull() || node.GetParent().IsNull();

        public static void Clear(this Node parent, int skip = 0, bool forceInstant = false)
        {
            if (parent.IsNull()) return;

            foreach (Node child in parent.GetChildren())
            {
                if (skip-- > 0 || child.IsNull()) continue;

                child.Destroy(forceInstant);
            }
        }

        public static void Clear<T>(this Node parent, int skip = 0, bool forceInstant = false) where T : Node
        {
            if (parent.IsNull()) return;

            foreach (Node child in parent.GetNodesInChildren<T>())
            {
                if (skip-- > 0 || child.IsNull()) continue;

                child.Destroy(forceInstant);
            }
        }

        public static void ClearAndDestroy<T>(this ICollection<T> collection) where T : Node
        {
            foreach (var node in collection)
            {
                if (node.IsNull()) continue;

                node.Destroy();
            }

            collection.Clear();
        }

        public static async void Destroy(this Node node, float lifeTime, bool forceInstant = false)
        {
            await Task.Delay(Mathf.RoundToInt(lifeTime * 1000));

            if (node.NotNull())
            {
                lock (node)
                {
                    Destroy(node, forceInstant);
                }
            }
        }

        public static void Destroy(this Node node, bool forceInstant = false)
        {
            if (node.IsNull()) return;

            if (forceInstant) node.Free();
            else node.QueueFree();
        }

        public static void Destroy<T>(this T[] nodes, bool forceInstant = false) where T : Node
        {
            if (nodes.IsEmpty()) return;

            foreach (var node in nodes)
                node.Destroy(forceInstant);
        }

        public static Vector3 Forward(this Node3D node, bool global = true) => node == null ? Vector3.Forward : -(global ? node.GlobalTransform : node.Transform).Basis.Z;
        public static Vector3 Right(this Node3D node, bool global = true) => node == null ? Vector3.Right : (global ? node.GlobalTransform : node.Transform).Basis.X;
        public static Vector3 Up(this Node3D node, bool global = true) => node == null ? Vector3.Up : (global ? node.GlobalTransform : node.Transform).Basis.Y;

        public static bool TryInstantiate<T>(this PackedScene prefab, Node parent, out T instance, int waitMilliseconds = 0) where T : Node
        => (instance = Instantiate<T>(prefab, parent, waitMilliseconds)).NotNull();

        public static T Instantiate<T>(this PackedScene prefab, Node parent, int waitMilliseconds = 0) where T : Node
        {
            if (prefab == null) return null;

            T t = (T)prefab.Instantiate();
            if (parent.NotNull())
            {
                if (waitMilliseconds > 0) parent.SetChild(t, waitMilliseconds);
                else parent.AddChild(t);
            }

            return t;
        }

        public static async void SetChild(this Node parent, Node node, int waitMilliseconds)
        {
            await Task.Delay(waitMilliseconds);
            SetChild(parent, node);
        }

        public static void SetChild(this Node newParent, Node node)
        {
            if (node.IsNull() || newParent.IsNull()) return;
            if (node == newParent) return;

            var oldParent = node.GetParent();
            if (oldParent == newParent) return;

            Vector3 position = default, rotation = default;
            if (node is Node3D node3D)
            {
                position = node3D.GlobalPosition;
                rotation = node3D.GlobalRotation;
            }

            if (oldParent.NotNull()) lock (oldParent) oldParent.RemoveChild(node);
            if (newParent.NotNull()) lock (newParent) newParent.AddChild(node);

            if (node is Node3D _node3D)
            {
                _node3D.GlobalPosition = position;
                _node3D.GlobalRotation = rotation;
            }
        }

        public static T Instantiate<T>(this T prefab, Node root) where T : Node
        {
            if (prefab == null) return null;

            T t = (T)prefab.Duplicate();
            root.AddChild(t);

            return t;
        }

        public static void SetActive(this Node node, bool active, bool includeChildren = false)
        {
            if (node.IsNull()) return;

            node.ProcessMode = active ? Node.ProcessModeEnum.Pausable : Node.ProcessModeEnum.Disabled;

            if (node is CollisionObject3D co) co.DisableMode = active ? CollisionObject3D.DisableModeEnum.KeepActive : CollisionObject3D.DisableModeEnum.Remove;
            else if (node is CollisionShape3D cs) cs.Disabled = !active;

            if (node is CanvasItem ci) ci.Visible = active;
            else if (node is Node3D n3) n3.Visible = active;

            // Camera
            if (active && node is Camera2D c2) c2.MakeCurrent();
            else if (node is Camera3D c3) c3.Current = active;

            if (includeChildren)
            {
                foreach (Node child in node.GetNodesInChildren<Node>(false))
                    SetActive(child, false);
            }
        }

        public static bool IsActive(this Node node)
        {
            if (node.IsNull()) return false;

            return node switch
            {
                CanvasItem n => n.Visible,
                Node3D n => n.Visible,
                _ => true
            };
        }

        public static List<T> GetNodesInParents<T>(this Node node, bool includeSelf = true, byte layerDepth = 0)
        {
            var depth = layerDepth > 0;
            var list = new List<T>();

            if (includeSelf == false && node.NotNull())
                node = node.GetParent();
            else layerDepth++;

            while (node.NotNull())
            {
                if (depth && layerDepth-- < 1) break;

                if (node is T t) list.Add(t);

                node = node.GetParent();
            }

            return list;
        }

        public static T GetNodeInParents<T>(this Node node, bool includeSelf = true, byte layerDepth = 0)
        {
            var depth = layerDepth > 0;

            if (includeSelf == false && node.NotNull())
                node = node.GetParent();
            else layerDepth++;

            while (node.NotNull())
            {
                if (depth && layerDepth-- < 1) break;

                if (node is T t) return t;

                node = node.GetParent();
            }

            return default(T);
        }

        public static List<T> GetNodesInChildren<T>(this Node node, bool includeSelf = true, byte layerDepth = 0)
        {
            List<T> list = new();
            loop(node, includeSelf, 0);
            return list;

            void loop(Node node, bool includeSelf, byte layer)
            {
                if (includeSelf) add(node);

                foreach (Node n in node.GetChildren())
                {
                    if (layerDepth < 1 || layer < layerDepth)
                    {
                        loop(n, true, (byte)(layer + 1));
                    }
                }
            }

            void add(Node node)
            {
                if (node is T t)
                    list.Add(t);
            }
        }

        public static T GetNodeInChildren<T>(this Node node, bool includeSelf = true) where T : Node
        {
            T result = null;
            loop(node, includeSelf);
            return result;

            void loop(Node node, bool includeSelf)
            {
                if (result != null) return;

                if (includeSelf && set(node))
                    return;

                foreach (Node n in node.GetChildren())
                {
                    loop(n, true);
                }
            }

            bool set(Node node)
            {
                if (node is not T) return false;

                result = node as T;
                return true;
            }
        }

        public static T GetNode<T>(this Node node) where T : Node => node is T ? node as T : null;
        public static bool TryGetNode<T>(this Node node, out T result) where T : Node
        {
            if (node is T)
            {
                result = node as T;
                return true;
            }

            result = null;
            return false;
        }
    }
}