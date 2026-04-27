namespace Cutulu.Core;

using System.Runtime.CompilerServices;
using System.IO;
using System;

public sealed class LocalEncoder : IDisposable
{
    private readonly MemoryStream _memory;
    private readonly BinaryWriter _writer;

    public long Length => _memory.Length;
    public long Position => _memory.Position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetPosition() => _memory.Position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPosition(long position) => _memory.Position = position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] GetBuffer() => _memory.ToArray();

    public LocalEncoder()
    {
        _writer = new(_memory = new());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<T>(T value)
    {
        _writer.Encode(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWrite<T>(T value, bool enable_logging = true)
    {
        return _writer.TryEncode(value, enable_logging);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _memory.SetLength(0);
        _memory.Position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LocalDecoder ToDecoder() => new(GetBuffer());

    public void Dispose()
    {
        _writer?.Dispose();
        _memory?.Dispose();
    }
}