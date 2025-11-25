namespace Cutulu.Core;

using System.Runtime.CompilerServices;
using System;

/// <summary>
/// Written by Maximilian Schecklmann on 3th of Nov 2025, inspired by Nic Barker's implementation.
/// </summary>
public sealed class SwapbackArray<T>
{
    private T[] _data;
    private int _count;

    public int Count => _count;
    public int Capacity => _data.Length;

    public SwapbackArray(int capacity = 4)
    {
        _data = capacity == 0 ? [] : new T[capacity];
    }

    public SwapbackArray(params T[] data)
    {
        _data = data ?? [];
        _count = _data.Length;
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

    public Span<T> AsSpan() => _data.AsSpan(0, _count);

    class Encoder : BinaryEncoder<SwapbackArray<T>>
    {
        public override void Encode(System.IO.BinaryWriter writer, ref object value)
        {
            writer.Encode(((SwapbackArray<T>)value)._data);
        }

        public override object Decode(System.IO.BinaryReader reader)
        {
            return new SwapbackArray<T>(reader.Decode<T[]>());
        }
    }
}
