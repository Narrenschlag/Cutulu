#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using System.Collections.Generic;
    using Godot;

    public static class Vector3Sorftf
    {
        public static Vector3[] SortByDistanceTo(this Vector3[] array, Vector3 position, bool ignoreY = false)
        {
            for (int i = 1; i < array.Length; i++)
            {
                var current = array[i];
                var currentDistance = ignoreY ? current.toXY().DistanceTo(position.toXY()) : (current - position).Length();
                var j = i - 1;

                // Move elements of array[0..i-1], that are greater than currentDistance,
                // to one position ahead of their current position
                if (ignoreY)
                {
                    while (j >= 0 && array[j].toXY().DistanceTo(position.toXY()) > currentDistance)
                    {
                        array[j + 1] = array[j];
                        j--;
                    }
                }

                else
                {
                    while (j >= 0 && ((array[j] - position).Length() > currentDistance))
                    {
                        array[j + 1] = array[j];
                        j--;
                    }
                }

                array[j + 1] = current;
            }

            return array;
        }

        public static T[] SortByDistanceTo<T>(this T[] array, Vector3 position, bool ignoreY = false) where T : Node3D
        {
            for (int i = 1; i < array.Length; i++)
            {
                var current = array[i];
                var currentDistance = ignoreY ? current.GlobalPosition.toXY().DistanceTo(position.toXY()) : (current.GlobalPosition - position).Length();
                var j = i - 1;

                // Move elements of array[0..i-1], that are greater than currentDistance,
                // to one position ahead of their current position
                if (ignoreY)
                {
                    while (j >= 0 && array[j].GlobalPosition.toXY().DistanceTo(position.toXY()) > currentDistance)
                    {
                        array[j + 1] = array[j];
                        j--;
                    }
                }

                else
                {
                    while (j >= 0 && ((array[j].GlobalPosition - position).Length() > currentDistance))
                    {
                        array[j + 1] = array[j];
                        j--;
                    }
                }

                array[j + 1] = current;
            }

            return array;
        }

        public static T GetClosestTo<T>(this IEnumerable<T> collection, Vector3 position, bool ignoreY = false) where T : Node3D
        {
            var closestDistance = -1.0f;
            var closest = default(T);

            foreach (var node in collection)
            {
                var distance = ignoreY ? node.GlobalPosition.toXY().DistanceTo(position.toXY()) : (node.GlobalPosition - position).Length();
                if (closestDistance < 0 || distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = node;
                }
            }

            return closest;
        }

        public static K GetClosestTo<K, T>(this IDictionary<K, T> collection, Vector3 position, bool ignoreY = false) where T : Node3D
        {
            var closestDistance = -1.0f;
            var closest = default(K);

            foreach (var pair in collection)
            {
                var distance = ignoreY ? pair.Value.GlobalPosition.toXY().DistanceTo(position.toXY()) : (pair.Value.GlobalPosition - position).Length();
                if (closestDistance < 0 || distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = pair.Key;
                }
            }

            return closest;
        }
    }
}
#endif