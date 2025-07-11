#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
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
    }
}
#endif