namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System;

    public static class Listf
    {
        public static bool TryAdd<T>(this List<T> list, T value)
        {
            if (list.Contains(value)) return false;

            list.Add(value);
            return true;
        }

        public static bool TryRemove<T>(this List<T> list, T value)
        {
            if (value == null || list.Contains(value) == false) return false;

            list.Remove(value);
            return true;
        }

        public static T RandomElement<T>(this List<T> list, T @default = default) => list.NotEmpty() ? list[Random.Range(0, list.Count)] : @default;

        public static T ModulatedElement<T>(this List<T> list, int i)
        {
            return list.NotEmpty() ? list[i.AbsMod(list.Count)] : default;
        }

        public static T GetClampedElement<T>(this List<T> list, int index)
        => list.IsEmpty() ? default : list[Math.Clamp(index, 0, list.Count - 1)];

        public static void Shuffle<T>(this List<T> list)
        {
            if (list.IsEmpty()) return;

            var n = list.Count;
            while (n > 1)
            {
                var k = Random.RangeIncluded(0, --n);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

#if GODOT4_0_OR_GREATER
        public static List<Godot.Vector3> ClampDistanceRelative(this List<Godot.Vector3> list, float percentage)
        {
            percentage = Math.Clamp(percentage, 0, 1);
            if (percentage >= 1 || list.IsEmpty()) return list;
            if (percentage <= 0) return null;

            float sum = 0;

            for (int i = 0; i < list.Count - 1; i++)
                sum += list[i].DistanceTo(list[i + 1]);

            return ClampDistance(list, sum * percentage);
        }

        public static List<Godot.Vector2> ClampDistanceRelative(this List<Godot.Vector2> list, float percentage)
        {
            percentage = Math.Clamp(percentage, 0, 1);
            if (percentage >= 1 || list.IsEmpty()) return list;
            if (percentage <= 0) return null;

            var sum = 0f;

            for (int i = 0; i < list.Count - 1; i++)
                sum += list[i].DistanceTo(list[i + 1]);

            return ClampDistance(list, sum * percentage);
        }

        public static List<Godot.Vector3> ClampDistance(this List<Godot.Vector3> list, float distance)
        {
            List<Godot.Vector3> result = [];
            float current = 0;

            for (int i = 0; i < list.Count - 1; i++)
            {
                float next = current + list[i].DistanceTo(list[i + 1]);

                // Overtook distance
                if (next > distance)
                {
                    float a = next - distance;
                    float b = next - current;

                    result.Add(list[i].Lerp(list[i + 1], (b - a) / b));
                    break;
                }

                result.Add(list[i]);

                // Stopped exactly there
                if (next == distance) break;

                current = next;
            }

            return result;
        }

        public static List<Godot.Vector2> ClampDistance(this List<Godot.Vector2> list, float distance)
        {
            List<Godot.Vector2> result = [];
            float current = 0;

            for (int i = 0; i < list.Count - 1; i++)
            {
                float next = current + list[i].DistanceTo(list[i + 1]);

                // Overtook distance
                if (next > distance)
                {
                    float a = next - distance;
                    float b = next - current;

                    result.Add(list[i].Lerp(list[i + 1], (b - a) / b));
                    break;
                }

                result.Add(list[i]);

                // Stopped exactly there
                if (next == distance) break;

                current = next;
            }

            return result;
        }
#endif
    }
}