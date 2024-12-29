namespace Cutulu.Core
{
    using System.Collections.Generic;
    using Godot;

    public static class Arrayf
    {
        public static T[] MoveElements<T>(this T[] array, int moveValue)
        {
            var arr = new T[array.Length];

            for (var i = 0; i < array.Length; i++)
            {
                arr[i] = array[(i + moveValue).AbsMod(array.Length)];
            }

            return arr;
        }

        public static T[] Mask<T>(this T[] array, T[] mask, bool inside = true)
        {
            var list = new List<T>();

            for (var i = 0; i < array.Length; i++)
            {
                if (mask.Contains(array[i]) == inside)
                    list.Add(array[i]);
            }

            return list.ToArray();
        }

        public static bool SequenceEquals<T>(this T[] array, T[] b)
        {
            if (array.Size() != b.Size()) return false;
            if (array == b) return true;

            if (array.NotEmpty())
            {
                for (var i = 0; i < array.Length; i++)
                {
                    if (b.Contains(array[i]) == false)
                        return false;
                }
            }

            return true;
        }

        public static bool Compare<T>(this T[] array, T[] other)
        {
            if (array.Size() == other.Size())
            {
                if (array.Size() > 0)
                {
                    List<T> list = new(other);
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (list.Contains(array[i]) == false) return false;

                        list.Remove(array[i]);
                    }
                }

                return true;
            }

            return false;
        }

        public static void Shuffle<T>(this T[] list)
        {
            if (list.IsEmpty()) return;

            var n = list.Length;
            while (n > 1)
            {
                var k = Random.RangeIncluded(0, --n);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static T ModulatedElement<T>(this T[] array, int i)
        {
            return array.NotEmpty() ? array[i.AbsMod(array.Length)] : default;
        }

        public static bool NotEmpty<T>(this T[] array)
        => array != null && array.Length > 0;

        public static bool IsEmpty<T>(this T[] array)
        => !NotEmpty(array);

        public static T RandomElement<T>(this T[] array, T @default = default)
        => array.NotEmpty() ? array[Random.Range(0, array.Length)] : @default;

        public static T GetClampedElement<T>(this T[] array, int index)
        => array.IsEmpty() ? default : array[Mathf.Clamp(index, 0, array.Length - 1)];

        public static bool Contains<T>(this T[] array, T element)
        => array != null && array.Length > 0 && ((ICollection<T>)array).Contains(element);

        public static T[] AddToArray<T>(this T[] array, T value)
        {
            AddToArray(value, ref array);
            return array;
        }

        public static void AddToArray<T>(this T element, ref T[] array)
        {
            if (array == null)
            {
                array = new T[1] { element };
                return;
            }

            T[] _array = new T[array.Length + 1];

            System.Array.Copy(array, _array, array.Length);
            _array[array.Length] = element;

            array = _array;
        }

        public static void RemoveNull<T>(ref T[] array)
        {
            if (array.IsEmpty()) return;

            List<T> list = new();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != null)
                {
                    list.Add(array[i]);
                }
            }

            array = list.ToArray();
        }

        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            if (array.Length <= index || index < 0)
                throw new System.ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            var result = new T[array.Length - 1];

            System.Array.Copy(array, result, index);
            System.Array.Copy(array, index + 1, result, index, result.Length - index);

            return result;
        }

        public static T[] RemoveFromArray<T>(this T[] array, T value)
        {
            RemoveFromArray(value, ref array);
            return array;
        }

        public static void RemoveFromArray<T>(this T element, ref T[] array, bool removeAllOccurences = false)
        {
            if (array.IsEmpty()) return;

            List<T> list = new();
            bool removed = false;

            for (int i = 0; i < array.Length; i++)
            {
                if ((removed && removeAllOccurences == false) || array[i].Equals(element) == false)
                {
                    list.Add(array[i]);
                }

                else
                {
                    removed = true;
                }
            }

            array = list.ToArray();
        }

        public static T[] OffsetElements<T>(this T[] array, int offset)
        {
            OffsetElements(ref array, offset);
            return array;
        }

        public static void OffsetElements<T>(ref T[] array, int offset)
        {
            T[] result = new T[array.Length];

            // Calculate the effective offset (taking negative offsets into account)
            offset = -offset % array.Length;

            if (offset < 0) offset += array.Length;
            else if (offset == 0) return;

            // Copy the bytes to the result array with the offset
            System.Array.Copy(array, offset, result, 0, array.Length - offset);
            System.Array.Copy(array, 0, result, array.Length - offset, offset);

            array = result;
        }

        public static T[] Duplicate<T>(this T[] array)
        {
            var result = new T[array.Length];

            System.Array.Copy(array, result, array.Length);

            return result;
        }
    }
}