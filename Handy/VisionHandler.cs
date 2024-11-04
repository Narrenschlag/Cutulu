using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Handles visibility based on layers, using keys with assigned idxs.
    /// </summary>
    public static class VisionHandler<Key>
    {
        private readonly static Dictionary<Key, int> Keys = new();
        private readonly static List<int> Indecies = new();

        public static int MaxIndex { get; set; } = 16;

        /// <summary>
        /// Adds key to dictionary and assigns it an index. Ignores if already contained.
        /// </summary>
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

        /// <summary>
        /// Removes key. Ignores if not contained.
        /// </summary>
        public static void Remove(Key key)
        {
            if (key is Node n && n.IsNull()) return;

            if (Keys.TryGetValue(key, out var idx))
            {
                Keys.Remove(key);
                Indecies.Remove(idx);
            }
        }

        /// <summary>
        /// Clears keys.
        /// </summary>
        public static void Clear()
        {
            Keys.Clear();
            Indecies.Clear();
        }

        /// <summary>
        /// Returns player idx. If idx < 1 the key is not assigned an idx.
        /// </summary>
        public static byte GetIdx(Key key) => key != null && Keys.TryGetValue(key, out var i) ? (byte)(i + 1) : default;

        /// <summary>
        /// Applies visibility layers to camera.
        /// </summary>
        public static void Apply(Camera3D camera, params Key[] keys)
        {
            if (camera.IsNull()) return;

            camera.SetLayers(false);
            camera.SetLayer(0, true);

            if (keys.NotEmpty())
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    var idx = GetIdx(keys[i]);

                    if (idx > 0) camera.SetLayer(idx, true);
                }
            }
        }

        /// <summary>
        /// Sets global visibility. Overwrites locals, if globally visible.
        /// </summary>
        public static void SetGlobalVisibility(VisualInstance3D vis, bool value)
        {
            vis.SetLayer(0, value);
        }

        /// <summary>
        /// Sets local visibility. Is overwritten, if globally visible.
        /// </summary>
        public static void SetLocalVisibility(VisualInstance3D vis, Key key, bool value)
        {
            var idx = GetIdx(key);

            if (idx > 0) vis.SetLayer(idx, value);
        }
    }
}