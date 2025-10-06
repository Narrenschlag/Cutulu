#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Godot;

    public static class Nodef
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
            if (collection.IsEmpty()) return;

            foreach (var node in collection)
            {
                if (node.IsNull()) continue;

                node.Destroy();
            }

            if (collection.IsReadOnly == false)
                collection.Clear();
        }

        public static void DestroyChildrenOf<T>(this Node parent, bool forceInstant = false) where T : Node
        {
            if (parent.IsNull()) return;

            parent.GetNodesInChildren<T>(false).Destroy(forceInstant);
        }

        public static Vector3 Forward(this Node3D node, bool global = true) => node.IsNull() ? Vector3.Forward : -(global ? node.GlobalTransform : node.Transform).Basis.Z;
        public static Vector3 Right(this Node3D node, bool global = true) => node.IsNull() ? Vector3.Right : (global ? node.GlobalTransform : node.Transform).Basis.X;
        public static Vector3 Up(this Node3D node, bool global = true) => node.IsNull() ? Vector3.Up : (global ? node.GlobalTransform : node.Transform).Basis.Y;

        public static bool TryInstantiate<T>(this PackedScene prefab, Node parent, out T instance, int waitMilliseconds = 0)
        => (instance = Instantiate<T>(prefab, parent, waitMilliseconds)) != null;

        public static T Instantiate<T>(this PackedScene prefab, Node parent, int waitMilliseconds = 0)
        {
            if (prefab == null) return default;

            var instance = prefab.Instantiate();
            if (instance.IsNull() || instance is not T t)
            {
                instance.QueueFree();
                return default;
            }

            if (parent.NotNull())
            {
                if (waitMilliseconds > 0) parent.SetChild(instance, waitMilliseconds);
                else parent.AddChild(instance);
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
            if (node.IsNull() || newParent.IsNull() || node == newParent) return;

            // Capture global transform for Node3D
            Transform3D? globalTransform = null;
            if (node is Node3D node3D && node3D.IsInsideTree())
                globalTransform = node3D.GlobalTransform;

            var oldParent = node.GetParent();
            if (oldParent != null)
            {
                // Remove from old parent
                oldParent.RemoveChild(node);

                // Force immediate processing to fully detach
                node.Owner = null;
            }

            // Add to new parent
            newParent.AddChild(node);

            // Restore transform if Node3D
            if (globalTransform.HasValue && node is Node3D node3D2)
                node3D2.GlobalTransform = globalTransform.Value;
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

            return default;
        }

        public static List<T> GetNodesInChildren<T>(this Node node, bool includeSelf = true, byte layerDepth = 0)
        {
            List<T> list = [];
            loop(node, includeSelf, 0);
            return list;

            void loop(Node node, bool includeSelf, byte layer)
            {
                if (node.IsNull()) return;
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
                if (node is T t && (t is not Node n || n.NotNull()))
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

        public static Node GetRoot(this Node node) => node.GetTree().CurrentScene;

        private static Node3D _main3d;
        public static Node3D Main3D { get { _main3d ??= (Engine.GetMainLoop() as SceneTree).CurrentScene.GetNodeInChildren<Node3D>(); return _main3d; } }

        private static Node2D _main2d;
        public static Node2D Main2D { get { _main2d ??= (Engine.GetMainLoop() as SceneTree).CurrentScene.GetNodeInChildren<Node2D>(); return _main2d; } }

        private static Node _main;
        public static Node Main { get { _main ??= (Engine.GetMainLoop() as SceneTree).CurrentScene; return _main; } }

        public static void Log(this Node node, string message, bool rich = false)
        {
            var log = $"[{node.Name}] {message}";

            if (rich) Debug.LogR(log);
            else Debug.Log(log);
        }
    }
}
#endif