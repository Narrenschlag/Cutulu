namespace Cutulu.Core;

using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using System.IO;

/// <summary>
/// Written by Maximilian Schecklmann on 3th of Nov 2025, inspired by Nic Barker's implementation.
/// </summary>
public sealed class SwapbackArray<T> : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable, IReadOnlyCollection<T>
{
    private T[] _data;
    private int _count;

    public int Count => _count;
    public int Length => _count;
    public int Capacity => _data.Length;

    public bool IsReadOnly => false; // Collections are not read-only by default
    public bool IsSynchronized => false; // Not thread-safe
    public object SyncRoot => this; // Use this instance as sync root

    public SwapbackArray(int capacity = 4)
    {
        _data = capacity == 0 ? [] : new T[capacity];
    }

    public SwapbackArray(params T[] data)
    {
        _data = data ?? [];
        _count = _data.Length;
    }

    public SwapbackArray(IList data)
    {
        _data = new T[data.Count]; // <-- was never initialized
        _count = data.Count;       // <-- was _data.Length which threw NullRef
        int i = 0;

        foreach (var item in data)
            _data[i++] = (T)item;
    }

    public ref T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref _data[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (item.IsNull()) return;

        if (_count == _data.Length)
        {
            var newCap = Math.Max(4, _data.Length * 2);
            Array.Resize(ref _data, newCap);
        }

        _data[_count++] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(params T[] items)
    {
        AddRange(items.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<T> items)
    {
        int newCount = _count + items.Length;

        // Ensure capacity
        if (newCount > _data.Length)
        {
            int newCap = Math.Max(_data.Length * 2, newCount);
            Array.Resize(ref _data, newCap);
        }

        // Bulk copy using spans (fastest)
        items.CopyTo(new Span<T>(_data, _count, items.Length));
        _count = newCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _count--;
        if (index < _count)
            _data[index] = _data[_count];
        _data[_count] = default!;
    }

    public T[] ToArray()
    {
        T[] array = new T[_count];
        Array.Copy(_data, 0, array, 0, _count);
        return array;
    }

    public Span<T> AsSpan() => _data.AsSpan(0, _count);

    public Span<T> AsSpan(int count) => _data.AsSpan(0, int.Min(_count, count));

    public Span<T> AsSpan(int start, int count)
    {
        start = Math.Max(0, Math.Min(start, _count));
        count = Math.Min(count, _count - start);
        return _data.AsSpan(start, count);
    }

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(_data, 0, _count); // Only clear if T contains references
        }
        _count = 0;
    }

    public bool Contains(T item)
    {
        return Array.IndexOf(_data, item, 0, _count) >= 0; // Only search within Count
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_data, 0, array, arrayIndex, _count);
    }

    public bool Remove(T item)
    {
        if (item.IsNull()) return false;

        for (int i = 0; i < _count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(_data[i], item))
            {
                RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool Remove(T item, out int index)
    {
        index = 0;

        if (item.IsNull()) return false;

        for (int i = 0; i < _count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(_data[i], item))
            {
                RemoveAt(index = i);
                return true;
            }
        }

        return false;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return _data[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void CopyTo(Array array, int index)
    {
        Array.Copy(_data, 0, array, index, _count);
    }

    /// <summary>
    /// Adds item to the array if it is not null and not already in it.
    /// </summary>
    public bool TryAdd(T item)
    {
        if (item.IsNull() || Contains(item)) return false;

        Add(item);
        return true;
    }

    /// <summary>
    /// Adds items to the array if they are not null and not already in it.
    /// </summary>
    public void TryAddRange(params T[] items)
    {
        if (items.IsEmpty()) return;

        T[] array = new T[items.Length];
        int i = 0;

        Span<T> span = array.AsSpan();
        foreach (ref var item in span)
        {
            if (item.IsNull() || Contains(item)) continue;

            array[i++] = item;
        }

        if (i < 1) return;

        AddRange(array.AsSpan(0, i));
    }
}

class SwapbackArrayEncoder() : BinaryEncoder(typeof(SwapbackArray<>))
{
    private static readonly ConcurrentDictionary<Type, Type> ItemTypeCache = [];

    public override void Encode(BinaryWriter writer, Type type, object value)
    {
        var itemType = ItemTypeCache.GetOrAdd(type, t =>
        {
            return t.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
                    .GetGenericArguments()[0];
        });

        var count = (UNumber)(value == null ? 0 : ((ICollection)value).Count);
        writer.Encode(count);

        if (count > 0)
        {
            foreach (var item in (IEnumerable)value)
                writer.Encode(item, itemType);
        }
    }

    public override object Decode(BinaryReader reader, Type type)
    {
        var itemType = ItemTypeCache.GetOrAdd(type, t =>
        {
            return t.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
                    .GetGenericArguments()[0];
        });

        var count = reader.Decode<UNumber>();
        var arrayType = itemType.MakeArrayType(); // T[]
        var array = (Array)Activator.CreateInstance(arrayType, (int)count);

        for (int i = 0; i < count; i++)
            array.SetValue(reader.Decode(itemType), i);

        return Activator.CreateInstance(type, array); // matches SwapbackArray(params T[] data)
    }
}