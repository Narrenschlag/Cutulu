namespace Cutulu.Core
{
#if GODOT4_0_OR_GREATER
    using Godot;
#endif

    public static class GodotObjectf
    {
        public static bool NotNull(this object obj) => !IsNull(obj);

        public static bool IsNull(this object obj)
        {
#if GODOT4_0_OR_GREATER
            return obj == null || (obj is GodotObject gd && GodotObject.IsInstanceValid(gd) == false);
#else
            return obj == null;
#endif
        }

#if GODOT4_0_OR_GREATER
        public static bool Destroy(this GodotObject obj, bool freeNow = false)
        {
            if (obj.IsNull()) return false;

            if (obj is Node node)
            {
                if (freeNow) node.Free();
                else node.QueueFree();
            }

            obj.Dispose();
            return true;
        }
#endif
    }
}