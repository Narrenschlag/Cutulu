#if GODOT4_0_OR_GREATER
namespace Cutulu.Mesh
{
    using System.Collections.Generic;
    using Cutulu.Core;
    using Godot;

    public class DrawContainer<KEY>
    {
        public readonly Dictionary<KEY, MeshInstance3D> DrawCalls = [];

        public bool Contains(KEY key) => DrawCalls.TryGetValue(key, out var value) && value.NotNull();

        public void Clear()
        {
            foreach (var drawCall in DrawCalls.Values)
                drawCall.Destroy();

            DrawCalls.Clear();
        }

        public void Remove(params KEY[] keys)
        {
            foreach (var key in keys)
            {
                if (DrawCalls.TryGetValue(key, out var drawCall))
                    drawCall.Destroy();

                DrawCalls.Remove(key);
            }
        }

        public void Add(KEY key, MeshInstance3D drawCall)
        {
            if (drawCall.IsNull()) return;
            DrawCalls.Add(key, drawCall);
        }

        public bool AddIfNull(KEY key, MeshInstance3D drawCall)
        {
            if (drawCall.IsNull() || Contains(key)) return false;

            Add(key, drawCall);
            return true;
        }

        public MeshInstance3D this[KEY key]
        {
            get => DrawCalls.TryGetValue(key, out var value) ? value : default;

            set
            {
                Remove(key);
                Add(key, value);
            }
        }
    }
}
#endif