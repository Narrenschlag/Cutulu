#if GODOT4_0_OR_GREATER
namespace Cutulu.Systems.Chunks;

using System;
using Godot;

public struct ChunkPoint(short x, short z)
{
    public short X { get; private set; } = x;
    public short Z { get; private set; } = z;

    public ChunkPoint(int x, int z) : this((short)x, (short)z) { }
    public ChunkPoint() : this(default, default) { }

    public static implicit operator ChunkPoint(Vector2I vector) => new((short)vector.X, (short)vector.Y);
    public static implicit operator Vector2I(ChunkPoint chunk) => new(chunk.X, chunk.Z);
    public static implicit operator Vector2(ChunkPoint chunk) => new(chunk.X, chunk.Z);

    public static ChunkPoint operator +(ChunkPoint a, ChunkPoint b) => new((short)(a.X + b.X), (short)(a.Z + b.Z));
    public static ChunkPoint operator -(ChunkPoint a, ChunkPoint b) => new((short)(a.X - b.X), (short)(a.Z - b.Z));
    public static ChunkPoint operator *(ChunkPoint a, ChunkPoint b) => new((short)(a.X * b.X), (short)(a.Z * b.Z));
    public static ChunkPoint operator *(ChunkPoint a, int b) => new((short)(a.X * b), (short)(a.Z * b));

    public static bool operator ==(ChunkPoint a, ChunkPoint b) => a.X == b.X && a.Z == b.Z;
    public static bool operator !=(ChunkPoint a, ChunkPoint b) => a == b == false;

    public readonly override bool Equals(object obj) => obj is ChunkPoint other && this == other;
    public readonly override int GetHashCode() => HashCode.Combine(X, Z);

    public readonly float Length() => ((Vector2)this).Length();

    public readonly float DistanceTo(Vector2 b) => ((Vector2)this).DistanceTo(b);

    public readonly override string ToString() => $"({X}, {Z})";
}
#endif