namespace Cutulu.Core;

using System.Runtime.CompilerServices;
using System.IO;
using System;

public sealed class LocalDecoder
{
    private readonly MemoryStream _memory;
    private readonly BinaryReader _reader;

    public LocalDecoder() { _reader = new(_memory = new()); }
    public LocalDecoder(byte[] buffer) { _reader = new(_memory = new(buffer)); }
    // Position = 0 is implicit for buffer ctor — MemoryStream starts at 0

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(byte[] buffer) => Append(buffer.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Span<byte> buffer) => _memory.Write(buffer);

    /// Call once after all Appends, before any reads
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush() => _memory.Position = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _memory.SetLength(0); // Position = 0 implicit

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(byte[] buffer) => Reset(buffer.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(Span<byte> buffer)
    {
        Clear();
        Append(buffer);
        Flush();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode<T>(out T value, bool enable_logging = true)
    {
        return _reader.TryDecode(out value, enable_logging);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Decode<T>()
    {
        return _reader.Decode<T>();
    }
}