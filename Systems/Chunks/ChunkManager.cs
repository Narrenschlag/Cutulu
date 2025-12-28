#if GODOT4_0_OR_GREATER
namespace Cutulu.Systems.Chunks;

using System.Collections.Generic;
using Cutulu.Core;
using Godot;

public abstract class ChunkManager<M, C>
    where M : ChunkManager<M, C>
    where C : IChunk<M, C>
{
    private readonly Dictionary<ChunkPoint, C> Chunks = [];

    public virtual Vector3 Center { get; private set; }
    public ushort ChunksPerAxis { get; private set; }
    public Vector2 ChunkSize { get; private set; }
    public int ChunkSizeInM { get; private set; }
    public Vector2 Start { get; private set; }
    public Vector2 Size { get; private set; }
    public Vector2 End { get; private set; }

    private Vector2 ChunkSizeDivisionMultiplier { get; set; }

    public int LoadedChunkCount => Chunks.Count;

    public virtual void ReloadGrid(Vector3 center, float diameter, int chunkSizeInM, bool enableLogging = true)
    {
        Chunks.Clear();

        CalculateParams(center, diameter, chunkSizeInM);

        if (enableLogging)
            Log($"Created grid of [b]{Chunks.Count}[/b] chunks, [b]{ChunksPerAxis}[/b] per axis.");
    }

    protected void CalculateParams(Vector3 center, float diameter, int chunkSizeInM)
    {
        ChunksPerAxis = (ushort)Mathf.Abs(Mathf.CeilToInt(diameter / chunkSizeInM));
        Start = center.toXY() - Vector2.One * diameter * 0.5f;
        ChunkSizeInM = chunkSizeInM;
        Center = center;

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

    public virtual bool HasChunk(ChunkPoint point)
    {
        return point.X >= 0 && point.X < ChunksPerAxis && point.Z >= 0 && point.Z < ChunksPerAxis;
    }

    public bool TryGetChunk(Vector3 position, out C chunk) => TryGetChunk(GetChunkPoint(position, out _), out chunk);

    public bool TryGetChunk(ChunkPoint point, out C chunk)
    {
        return (chunk = GetChunk(point)).NotNull();
    }

    public C GetChunk(Vector3 position) => GetChunk(GetChunkPoint(position, out _));

    public C GetChunk(ChunkPoint point)
    {
        if (Chunks.TryGetValue(point, out var chunk) && chunk.NotNull())
            return chunk;

        else if (HasChunk(point))
        {
            chunk = CreateChunk(point).Init(this as M, point);

            if (chunk.NotNull())
            {
                Chunks[point] = chunk;
                return chunk;
            }
        }

        return default;
    }

    protected abstract C CreateChunk(ChunkPoint point);

    public void Log(string message) => Debug.LogR($"[color=indianred][b][{GetType().Name}][/b][/color] {message}");
}
#endif