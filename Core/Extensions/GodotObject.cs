namespace Cutulu.Core
{
    using Godot;

    public static class GodotObjectf
    {
        public static bool IsNull(this object obj) => obj == null || (obj is GodotObject gd && GodotObject.IsInstanceValid(gd) == false);
        public static bool NotNull(this object obj) => !IsNull(obj);

        public static bool Destroy(this GodotObject obj)
        {
            if (obj.IsNull()) return false;

            obj.Dispose();
            return true;
        }
    }
}