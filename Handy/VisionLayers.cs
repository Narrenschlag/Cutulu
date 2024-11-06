using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Handles visibility based on layers, using keys with assigned idxs.
    /// </summary>
    public static class VisionLayers
    {
        private readonly static Dictionary<object, int> Keys = new();
        private readonly static List<int> Indecies = new();

        public static int MaxIndex { get; set; } = 16;

        /// <summary>
        /// Adds key to dictionary and assigns it an index. Ignores if already contained.
        /// </summary>
        public static void Add(object key)
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
        public static void Remove(object key)
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
        public static byte GetIdx(object key) => key != null && Keys.TryGetValue(key, out var i) ? (byte)(i + 1) : default;

        /// <summary>
        /// Applies visibility layers to camera.
        /// </summary>
        public static void Apply(Camera3D camera, params object[] keys)
        {
            if (camera.IsNull()) return;

            Debug.LogR($"[color=orange]>>>>>>");
            camera.SetLayers(false);
            Debug.Log(camera.CullMask);
            camera.SetLayer(0, true);
            Debug.Log(camera.CullMask);
            Debug.LogR($"[color=orange]>>>>>>");

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
        public static void SetGlobalVisibility(this VisualInstance3D vis, bool value)
        {
            vis.SetLayer(0, value);
        }

        /// <summary>
        /// Sets local visibility. Is overwritten, if globally visible.
        /// </summary>
        public static void SetLocalVisibility(this VisualInstance3D vis, object key, bool value, bool disableGlobalVisibility = true)
        {
            if (disableGlobalVisibility) SetGlobalVisibility(vis, false);

            var idx = GetIdx(key);
            if (idx > 0) vis.SetLayer(idx, value);
        }
    }
}