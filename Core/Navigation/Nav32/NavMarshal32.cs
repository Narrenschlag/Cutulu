#if GODOT4_0_OR_GREATER
namespace Cutulu.Core;

using Godot;

/// <summary>
/// A pathfinding map with 32x32 chunks with 32bit chunk side hashes. 
/// Heavily inspired by Tynan Sylvester's RimWorld.
/// Written, with blood, sweat and tears, by Maximilian Schecklmann April 2nd to 4th 2026. 
/// </summary>
public readonly ref struct NavMarshal32(NavMap32 map)
{
    private readonly NavMap32 Map = map;

    public readonly bool TryGetChunk(Vector2I chunk, out NavChunk32 c)
        => Map.TryGetChunk(chunk, out c);

    public readonly void CreateMap(Vector3 from, Vector3 to)
    {
        Map.Init();

        Vector3 min = new(Mathf.Min(from.X, to.X), 0f, Mathf.Min(from.Z, to.Z));
        Vector3 max = new(Mathf.Max(from.X, to.X), 0f, Mathf.Max(from.Z, to.Z));
        Vector2I minChunk = Map.GetChunkCoord(min, out _);
        Vector2I maxChunk = Map.GetChunkCoord(max, out _);

        int sizeX = maxChunk.X - minChunk.X + 1;
        int sizeZ = maxChunk.Y - minChunk.Y + 1;
        var chunks = new Vector2I[sizeX * sizeZ];
        int i = 0;
        for (int x = minChunk.X; x <= maxChunk.X; x++)
            for (int z = minChunk.Y; z <= maxChunk.Y; z++, i++)
                chunks[i] = new Vector2I(x, z);

        Map.CreateChunks(chunks);
    }

    public readonly SwapbackArray<Vector2I> AddObstacle(NavObstacle32 obstacle)
        => Map.AddObstacle(obstacle, true);

    public readonly SwapbackArray<Vector2I> RemoveObstacle(NavObstacle32 obstacle)
        => Map.RemoveObstacle(obstacle, true);

    public readonly bool TryFindPath(Vector3 from, Vector3 to, out Vector3[] path)
        => Map.TryFindCellPath(from, to, out path);
}
#endif