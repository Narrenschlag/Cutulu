using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public static class VisionHandler<Key>
    {
        private readonly static Dictionary<Key, int> Keys = new();
        private readonly static List<int> Indecies = new();

        public static int MaxIndex { get; set; } = 16;

        public static void Add(Key key)
        {
            if (Keys.ContainsKey(key))
                return;

            else if (Keys.IsEmpty())
            {
                Keys[key] = 0;
                Indecies.Add(0);
                return;
            }

            else
            {
                MaxIndex = Mathf.Clamp(MaxIndex, 1, 31);

                for (var i = 1; i < MaxIndex; i++)
                {
                    if (Indecies.Contains(i) == false)
                    {
                        Indecies.Add(Keys[key] = i);
                        return;
                    }
                }
            }

            throw new($"Cannot add key. Max keys reached.");
        }

        public static void Remove(Key key)
        {
            if (key is Node n && n.IsNull()) return;

            if (Keys.TryGetValue(key, out var idx))
            {
                Keys.Remove(key);
                Indecies.Remove(idx);
            }
        }

        public static void Clear()
        {
            Keys.Clear();
            Indecies.Clear();
        }

        public static bool IsVisible(MeshInstance3D node, params Key[] keys) => IsVisibleT(node, keys);
        public static bool IsVisible(CanvasItem node, params Key[] keys) => IsVisibleT(node, keys);
        private static bool IsVisibleT(Node node, params Key[] keys)
        {
            if (node.IsNull() || keys.IsEmpty()) return false;

            var value = node is CanvasItem ci ? ci.VisibilityLayer : node is MeshInstance3D mi ? mi.Layers : 0;
            if (value < 1) return keys.IsEmpty();

            var bitBuilder = new BitBuilder(value);
            for (var i = 0; i < keys.Length; i++)
            {
                if (keys[i] == null || (keys[i] is Node n && n.IsNull())) continue;

                if (Keys.TryGetValue(keys[i], out var maskIdx))
                {
                    if (bitBuilder[maskIdx + 1] == false) return false;
                }

                else return false;
            }

            return true;
        }

        public static void SetVision(MeshInstance3D mesh, bool value, params Key[] keys) => SetVision(new[] { mesh }, value, keys);
        public static void SetVision(CanvasItem canvas, bool value, params Key[] keys) => SetVision(new[] { canvas }, value, keys);

        public static void SetVision(MeshInstance3D[] nodes, bool value, params Key[] keys) => SetVisionT(value, nodes, keys);
        public static void SetVision(CanvasItem[] nodes, bool value, params Key[] keys) => SetVisionT(value, nodes, keys);

        private static void SetVisionT<T>(bool value, T[] nodes, params Key[] keys) where T : Node
        {
            if (nodes.IsEmpty()) return;

            if (keys.IsEmpty())
            {
                ResetVision(nodes);
                return;
            }

            var bitBuilder = new BitBuilder((uint)0);

            for (var i = 0; i < keys.Length; i++)
            {
                if (keys[i] == null || (keys[i] is Node n && n.IsNull())) continue;

                if (Keys.TryGetValue(keys[i], out var maskIdx))
                {
                    bitBuilder[maskIdx + 1] = value;
                }
            }

            var layers = bitBuilder.ByteBuffer.Decode<uint>();

            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsNull()) continue;

                switch (nodes[i])
                {
                    case MeshInstance3D mesh:
                        mesh.Layers = layers;
                        mesh.Visible = true;
                        break;

                    case CanvasItem ci:
                        ci.VisibilityLayer = layers;
                        ci.Visible = true;
                        break;
                }
            }
        }

        public static void ResetVision<T>(T[] nodes) where T : Node
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsNull()) continue;

                switch (nodes[i])
                {
                    case MeshInstance3D mesh:
                        mesh.Layers = 1;
                        mesh.Visible = true;
                        break;

                    case CanvasItem ci:
                        ci.VisibilityLayer = 1;
                        ci.Visible = true;
                        break;
                }
            }
        }

        public static void EnableVision(MeshInstance3D node, params Key[] keys) => SetVision(node, true, keys);
        public static void EnableVision(CanvasItem node, params Key[] keys) => SetVision(node, true, keys);

        public static void DisableVision(MeshInstance3D node, params Key[] keys) => SetVision(node, false, keys);
        public static void DisableVision(CanvasItem node, params Key[] keys) => SetVision(node, false, keys);

        public static void Apply(Camera3D camera, params Key[] keys)
        {
            if (camera.IsNull()) return;

            var bitBuilder = new BitBuilder((uint)0)
            {
                [0] = true
            };

            if (keys.NotEmpty())
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    if (Keys.TryGetValue(keys[i], out var maskIdx))
                    {
                        bitBuilder[maskIdx + 1] = true;
                    }
                }
            }

            camera.CullMask = bitBuilder.ByteBuffer.Decode<uint>();
        }
    }
}