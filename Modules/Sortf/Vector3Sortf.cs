using Godot;

namespace Cutulu
{
    public static class Vector3Sorftf
    {
        public static Vector3[] SortByDistanceTo(this Vector3[] array, Vector3 position)
        {
            for (int i = 1; i < array.Length; i++)
            {
                var current = array[i];
                var currentDistance = (current - position).Length();
                var j = i - 1;

                // Move elements of array[0..i-1], that are greater than currentDistance,
                // to one position ahead of their current position
                while (j >= 0 && ((array[j] - position).Length() > currentDistance))
                {
                    array[j + 1] = array[j];
                    j--;
                }

                array[j + 1] = current;
            }

            return array;
        }
    }
}