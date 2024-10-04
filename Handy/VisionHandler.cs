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

        public static void SetVision(MeshInstance3D mesh, params Key[] keys)
        {
            SetVision(new[] { mesh }, keys);
        }

        public static void SetVision(MeshInstance3D[] meshes, params Key[] keys)
        {
            if (meshes.IsEmpty()) return;

            if (keys.IsEmpty())
            {
                for (var i = 0; i < meshes.Length; i++)
                {
                    if (meshes[i].IsNull()) continue;

                    meshes[i].Visible = true;
                    meshes[i].Layers = 1;
                }

                return;
            }

            var bitBuilder = new BitBuilder((uint)0);

            for (var i = 0; i < keys.Length; i++)
            {
                if (Keys.TryGetValue(keys[i], out var maskIdx))
                {
                    bitBuilder[maskIdx + 1] = true;
                }
            }

            var layers = bitBuilder.ByteBuffer.Decode<uint>();

            for (var i = 0; i < meshes.Length; i++)
            {
                if (meshes[i].IsNull()) continue;

                meshes[i].Visible = true;
                meshes[i].Layers = layers;
            }
        }

        public static void EnableVision(MeshInstance3D mesh, params Key[] keys)
        {
            if (mesh.IsNull()) return;

            if (keys.IsEmpty())
            {
                mesh.Visible = true;
                mesh.Layers = 1;

                return;
            }

            var bitBuilder = new BitBuilder(mesh.Layers);
            mesh.Visible = true;

            for (var i = 0; i < keys.Length; i++)
            {
                if (Keys.TryGetValue(keys[i], out var maskIdx))
                {
                    bitBuilder[maskIdx + 1] = true;
                }
            }

            mesh.Layers = bitBuilder.ByteBuffer.Decode<uint>();
        }

        public static void Apply(Camera3D camera, params Key[] keys)
        {
            if (camera.IsNull()) return;

            var bitBuilder = new BitBuilder((uint)1);

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