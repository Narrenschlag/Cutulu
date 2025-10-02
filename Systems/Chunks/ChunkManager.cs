namespace Cutulu.Systems.Chunks;

using System.Collections.Generic;
using Cutulu.Core;
using System;
using Godot;

public partial class ChunkManager<CHUNK> : Node3D where CHUNK : Chunk
{
    public readonly Dictionary<ChunkPoint, CHUNK> Chunks = [];

    [Export] private bool ReloadSelf { get; set; } = true;

    [ExportGroup("Reload Params")]
    [Export] public float Diameter { get; set; } = 1024.0f;
    [Export] public int ChunkSizeInM { get; set; } = 64;
    public ushort ChunksPerAxis { get; private set; }
    public Vector2 ChunkSize { get; private set; }
    public Vector2 Start { get; private set; }
    public Vector2 Size { get; private set; }
    public Vector2 End { get; private set; }

    private Vector2 ChunkSizeDivisionMultiplier { get; set; }

    public override void _EnterTree()
    {
        if (ReloadSelf && Chunks.Count < 1) ReloadGrid();
    }

    public virtual void ReloadGrid()
    {
        Chunks.Clear();

        CalculateParams(Diameter, ChunkSizeInM);

        for (short x = 0; x < ChunksPerAxis; x++)
        {
            for (short z = 0; z < ChunksPerAxis; z++)
            {
                var point = new ChunkPoint(x, z);
                Chunks[point] = GetChunk(point);
            }
        }

        Log($"Added [b]{Chunks.Count}[/b] chunks, [b]{ChunksPerAxis}[/b] per axis.");
    }

    protected void CalculateParams(float diameter, int chunkSizeInM)
    {
        ChunksPerAxis = (ushort)Mathf.Abs(Mathf.CeilToInt(diameter / chunkSizeInM));
        Start = GlobalPosition.toXY() - Vector2.One * diameter * 0.5f;
        End = Start + Vector2.One * chunkSizeInM * ChunksPerAxis;
        ChunkSize = Vector2.One * chunkSizeInM;
        Size = End - Start;

        ChunkSizeDivisionMultiplier = new Vector2(1.0f / ChunkSize.X, 1.0f / ChunkSize.Y);
    }

    public ChunkPoint GetChunkPoint(Vector3 globalPosition, out Vector2 localPosition)
    {
        var pos2 = new Vector2(
            get(globalPosition.X - Start.X, Size.X - 0.001f),
            get(globalPosition.Z - Start.Y, Size.Y - 0.001f)
        );

        var index = (Vector2I)(pos2 * ChunkSizeDivisionMultiplier);
        localPosition = pos2 - index * ChunkSize;
        return index;

        static float get(float a, float b) => a < 0 ? 0 : a > b ? b : a;
    }

    public Vector2 GetVector2(ChunkPoint chunkCoord)
    {
        return Start + new Vector2(
            chunkCoord.X * ChunkSize.X,
            chunkCoord.Z * ChunkSize.Y
        );
    }

    public Vector3 GetVector3(ChunkPoint chunkCoord, float y)
    {
        return new(
            Start.X + chunkCoord.X * ChunkSize.X,
            y,
            Start.Y + chunkCoord.Z * ChunkSize.Y
        );
    }

    protected virtual CHUNK GetChunk(ChunkPoint point) => (CHUNK)new Chunk();

    public void Log(string message) => Debug.LogR($"[color=indianred][b][{GetType().Name}][/b][/color] {message}");
}

public readonly struct ChunkPoint(short x, short z)
{
    public readonly short X = x;
    public readonly short Z = z;

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

    public override bool Equals(object obj) => obj is ChunkPoint other && this == other;
    public override int GetHashCode() => HashCode.Combine(X, Z);

    public float Length() => ((Vector2)this).Length();

    public float DistanceTo(Vector2 b) => ((Vector2)this).DistanceTo(b);

    public override string ToString() => $"({X}, {Z})";
}

public class Chunk
{
    public ChunkPoint Point { get; }

    public Chunk(ChunkPoint point)
    {
        Point = point;
        Init();
    }

    public Chunk()
    {

    }

    protected virtual void Init()
    {

    }
}