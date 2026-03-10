namespace Cutulu.Core;

using System.Runtime.CompilerServices;
using System.IO;
using System;

public sealed class LocalDecoder : IDisposable
{
    private readonly MemoryStream _memory;
    private readonly BinaryReader _reader;

    public long Length => _memory.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetPosition() => _memory.Position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPosition(long position) => _memory.Position = position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] GetBuffer() => _memory.GetBuffer();

    public LocalDecoder() { _reader = new(_memory = new()); }
    public LocalDecoder(byte[] buffer) { _reader = new(_memory = new(buffer)); }
    // Position = 0 is implicit for buffer ctor — MemoryStream starts at 0

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(byte[] buffer) => Append(buffer.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Span<byte> buffer) => _memory.Write(buffer);

    /// Call once after all Appends, before any reads
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush() => Reset();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _memory.SetLength(0); // Position = 0 implicit

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(byte[] buffer) => Reset(buffer.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => _memory.Position = 0;

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
        long position = _memory.Position;

        if (_reader.TryDecode(out value, enable_logging)) return true;

        // Reset reader if failed to decoder
        _memory.Position = position;

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Decode<T>()
    {
        return _reader.Decode<T>();
    }

    public void Dispose()
    {
        _memory?.Dispose();
        _reader?.Dispose();
    }
}