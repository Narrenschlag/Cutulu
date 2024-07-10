using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public static class ShapeCast3Df
    {
        public static List<T> GetColliders<T>(this ShapeCast3D shapeCast)
        {
            var list = new System.Collections.Generic.List<T>();

            for (var idx = 0; idx < shapeCast.GetCollisionCount(); idx++)
            {
                var body = shapeCast.GetCollider(idx);

                if (body is T t)
                    list.Add(t);
            }

            return list;
        }
    }
}