namespace Cutulu.Core;

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;
using Godot;

using BitMask32 = uint;

/// <summary>
/// A pathfinding map with 32x32 chunks with 32bit chunk side hashes. 
/// Heavily inspired by Tynan Sylvester's RimWorld.
/// Written, with blood, sweat and tears, by Maximilian Schecklmann April 2nd to 4th 2026. 
/// </summary>
[GlobalClass]
public partial class NavMap32 : Node
{
    public const byte CELLS_PER_CHUNK = sizeof(BitMask32) * 8;

    [Export] public float ChunkSize { get; private set; } = 32.0f;
    public float CellSize { get; private set; }

    private readonly Dictionary<Vector2I, NavChunk32> Chunks = [];
    private readonly Dictionary<(Vector2I, short, Vector2I, short), Vector2I[]> PathCache = [];

    public void Init()
    {
        CellSize = ChunkSize / CELLS_PER_CHUNK;
    }

    public NavMarshal32 GetMarshal() => new(this);

    public void InvalidatePathCache() => PathCache.Clear();

    public NavChunk32 GetOrCreateChunk(Vector2I chunk)
    {
        if (Chunks.TryGetValue(chunk, out var c) == false)
            Chunks[chunk] = c = new(this, chunk);
        return c;
    }

    public bool TryGetChunk(Vector3 worldPos, out NavChunk32 chunk, out Vector2I localCell)
    {
        localCell = new(
            Mathf.FloorToInt(worldPos.X / CellSize).AbsMod(CELLS_PER_CHUNK),
            Mathf.FloorToInt(worldPos.Z / CellSize).AbsMod(CELLS_PER_CHUNK)
        );
        return TryGetChunk(new(
            Mathf.FloorToInt(worldPos.X / ChunkSize),
            Mathf.FloorToInt(worldPos.Z / ChunkSize)
        ), out chunk);
    }

    public void CreateChunks(ICollection<Vector2I> chunks)
    {
        if (chunks == null) return;
        foreach (var chunk in chunks)
            GetOrCreateChunk(chunk).Refresh();

        foreach (var chunk in chunks)
            GetChunk(chunk).RefreshNeighbours();

        RefreshNeighbours(chunks);
    }

    public NavChunk32 GetChunk(Vector2I chunk)
        => TryGetChunk(chunk, out var c) ? c : null;

    public bool TryGetChunk(Vector2I chunk, out NavChunk32 c)
        => Chunks.TryGetValue(chunk, out c) && c.NotNull();

    public Vector2I GetChunkCoord(Vector3 worldPos, out Vector2I localCell)
    {
        localCell = new(
            Mathf.FloorToInt(worldPos.X / CellSize).AbsMod(CELLS_PER_CHUNK),
            Mathf.FloorToInt(worldPos.Z / CellSize).AbsMod(CELLS_PER_CHUNK)
        );
        return new(
            Mathf.FloorToInt(worldPos.X / ChunkSize),
            Mathf.FloorToInt(worldPos.Z / ChunkSize)
        );
    }

    #region Obstacles

    public SwapbackArray<Vector2I> AddObstacle(NavObstacle32 obstacle, bool refreshNeighbours = false)
    {
        if (obstacle == null || !obstacle.IsValid()) return null;

        SwapbackArray<Vector2I> affected = [];
        AddAffectedChunks(obstacle.Bounds.ToAabb(), affected);

        for (int i = 0; i < affected.Count; i++)
        {
            var c = GetOrCreateChunk(affected[i]);
            c.AddObstacle(obstacle);
            c.Refresh();
        }

        if (refreshNeighbours)
            RefreshNeighboursWithRing([.. affected]);

        return affected;
    }

    public SwapbackArray<Vector2I> RemoveObstacle(NavObstacle32 obstacle, bool refreshNeighbours = false)
    {
        if (obstacle == null) return null;

        SwapbackArray<Vector2I> affected = [];
        AddAffectedChunks(obstacle.Bounds.ToAabb(), affected);
        obstacle.Invalidate();

        for (int i = 0; i < affected.Count; i++)
        {
            if (TryGetChunk(affected[i], out var c))
            {
                c.RemoveObstacle(obstacle);
                c.Refresh();
            }
        }

        if (refreshNeighbours)
            RefreshNeighboursWithRing([.. affected]);

        return affected;
    }

    private void RefreshNeighbours(ICollection<Vector2I> chunks)
    {
        foreach (var coord in chunks)
            if (TryGetChunk(coord, out var c))
                c.RefreshNeighbours();

        PathCache.Clear();
    }

    private void RefreshNeighboursWithRing(Vector2I[] chunks)
    {
        HashSet<Vector2I> toRefresh = [];
        foreach (var chunk in chunks)
        {
            toRefresh.Add(chunk);
            toRefresh.Add(chunk + new Vector2I(1, 0));
            toRefresh.Add(chunk + new Vector2I(-1, 0));
            toRefresh.Add(chunk + new Vector2I(0, 1));
            toRefresh.Add(chunk + new Vector2I(0, -1));
        }
        RefreshNeighbours([.. toRefresh]);
    }

    private void AddAffectedChunks(Aabb aabb, SwapbackArray<Vector2I> affected)
    {
        int minX = Mathf.FloorToInt(aabb.Position.X / ChunkSize);
        int minZ = Mathf.FloorToInt(aabb.Position.Z / ChunkSize);
        int maxX = Mathf.FloorToInt(aabb.End.X / ChunkSize);
        int maxZ = Mathf.FloorToInt(aabb.End.Z / ChunkSize);
        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
                affected.TryAdd(new Vector2I(x, z));
    }

    #endregion

    #region Region-based pathfinding

    private readonly struct RegionNode(Vector2I chunk, short region)
        : IEquatable<RegionNode>
    {
        public readonly Vector2I Chunk = chunk;
        public readonly short Region = region; // -1 = freely walkable chunk

        public bool Equals(RegionNode o) => Chunk == o.Chunk && Region == o.Region;
        public override bool Equals(object o) => o is RegionNode n && Equals(n);
        public override int GetHashCode() => HashCode.Combine(Chunk, Region);
    }

    // Returns the region index that contains local cell, or -1 for free chunks.
    // Returns -2 when cell is blocked / chunk not found.
    private short FindRegionForCell(Vector2I chunkCoord, Vector2I localCell, out NavChunk32 chunk)
    {
        if (!TryGetChunk(chunkCoord, out chunk)) return -2;

        if (chunk.IsFreelyWalkable()) return -1;
        if (chunk.IsFullyBlocked()) return -2;

        if (chunk.Regions.NotEmpty())
        {
            for (short i = 0; i < chunk.Regions.Count; i++)
                if (chunk.Regions[i].Contains(localCell))
                    return i;
        }

        return -2; // cell is inside an obstacle
    }

    private void SeedStartNodes(
        Vector2I chunkCoord, Vector2I? localCell,
        PriorityQueue<RegionNode, int> open,
        Dictionary<RegionNode, int> gScore,
        Vector2I to)
    {
        if (!TryGetChunk(chunkCoord, out var chunk)) return;

        if (chunk.IsFreelyWalkable())
        {
            var n = new RegionNode(chunkCoord, -1);
            gScore[n] = 0;
            open.Enqueue(n, Heuristic(chunkCoord, to));
            return;
        }

        if (chunk.IsFullyBlocked()) return;

        if (localCell.HasValue && chunk.Regions.NotEmpty())
        {
            for (short i = 0; i < chunk.Regions.Count; i++)
            {
                if (!chunk.Regions[i].Contains(localCell.Value)) continue;
                var n = new RegionNode(chunkCoord, i);
                gScore[n] = 0;
                open.Enqueue(n, Heuristic(chunkCoord, to));
                return;
            }
            return;
        }

        if (chunk.Regions.NotEmpty())
        {
            for (short i = 0; i < chunk.Regions.Count; i++)
            {
                var n = new RegionNode(chunkCoord, i);
                gScore[n] = 0;
                open.Enqueue(n, Heuristic(chunkCoord, to));
            }
        }
    }

    private bool IsGoalNode(RegionNode node, Vector2I goalChunk, short goalRegion)
    {
        if (node.Chunk != goalChunk) return false;
        return goalRegion < 0 || node.Region < 0 || node.Region == goalRegion;
    }

    public bool TryFindChunkPath(
        Vector2I fromChunk, Vector2I toChunk,
        Vector2I fromCell, Vector2I toCell,
        out Vector2I[] path)
    {
        // Validate endpoints first, needed for cache key and same-chunk check
        short startRegion = FindRegionForCell(fromChunk, fromCell, out _);
        short goalRegion = FindRegionForCell(toChunk, toCell, out _);

        if (startRegion == -2 || goalRegion == -2)
        {
            path = null;
            return false;
        }

        // Same chunk
        if (fromChunk == toChunk)
        {
            // Free chunk, whole chunk is one walkable space
            if (startRegion == -1)
            {
                path = [fromChunk];
                return true;
            }

            // Split chunk, but same region
            if (startRegion == goalRegion)
            {
                path = [fromChunk];
                return true;
            }
        }

        // Directed cache lookup (region-aware)
        var cacheKey = (fromChunk, startRegion, toChunk, goalRegion);
        if (PathCache.TryGetValue(cacheKey, out path)) return path != null;

        // A* over region graph
        var open = new PriorityQueue<RegionNode, int>();
        var cameFrom = new Dictionary<RegionNode, RegionNode>();
        var gScore = new Dictionary<RegionNode, int>();

        SeedStartNodes(fromChunk, fromCell, open, gScore, toChunk);

        while (open.Count > 0)
        {
            var cur = open.Dequeue();

            // Stale-entry guard
            if (!gScore.TryGetValue(cur, out int g)) continue;

            if (IsGoalNode(cur, toChunk, goalRegion))
            {
                path = ReconstructChunkPath(cameFrom, cur);
                PathCache[cacheKey] = path;
                return true;
            }

            if (!TryGetChunk(cur.Chunk, out var chunk)) continue;

            if (chunk.IsFreelyWalkable())
            {
                foreach (var dir in Cardinals)
                {
                    var nbCoord = cur.Chunk + dir;
                    if (!TryGetChunk(nbCoord, out var nbChunk)) continue;
                    if (nbChunk.IsFullyBlocked()) continue;

                    var entrySide = Opposite(DirToNeighbour(dir));

                    if (nbChunk.IsFreelyWalkable())
                    {
                        TryRelax(cur, new(nbCoord, -1), nbCoord,
                                 g, open, cameFrom, gScore, toChunk);
                    }
                    else if (nbChunk.Regions.NotEmpty())
                    {
                        for (short r = 0; r < nbChunk.Regions.Count; r++)
                        {
                            if (nbChunk.Regions[r].GetSideHash(entrySide) == 0) continue;
                            TryRelax(cur, new(nbCoord, r), nbCoord,
                                     g, open, cameFrom, gScore, toChunk);
                        }
                    }
                }
            }
            else
            {
                if (chunk.Regions.IsEmpty()
                    || cur.Region < 0
                    || cur.Region >= chunk.Regions.Count)
                    continue;

                var region = chunk.Regions[cur.Region];

                foreach (var nb in region.GetNeighbours())
                {
                    var nbCoord = cur.Chunk + GetDirection(nb.Type);
                    if (!TryGetChunk(nbCoord, out var nbChunk)) continue;
                    if (nbChunk.IsFullyBlocked()) continue;

                    RegionNode nbNode;

                    if (nbChunk.IsFreelyWalkable())
                        nbNode = new(nbCoord, -1);
                    else
                    {
                        if (nb.Region < 0 || nb.Region >= (nbChunk.Regions?.Count ?? 0))
                            continue;
                        nbNode = new(nbCoord, nb.Region);
                    }

                    TryRelax(cur, nbNode, nbCoord, g, open, cameFrom, gScore, toChunk);
                }
            }
        }

        PathCache[cacheKey] = null;
        path = null;
        return false;
    }

    public bool TryFindChunkPath(Vector2I from, Vector2I to, out Vector2I[] path)
    {
        var centre = new Vector2I(CELLS_PER_CHUNK / 2, CELLS_PER_CHUNK / 2);
        return TryFindChunkPath(from, to, centre, centre, out path);
    }

    private static void TryRelax(
        RegionNode cur, RegionNode nb, Vector2I nbCoord, int g,
        PriorityQueue<RegionNode, int> open,
        Dictionary<RegionNode, RegionNode> cameFrom,
        Dictionary<RegionNode, int> gScore,
        Vector2I to)
    {
        int tentative = g + 1;
        if (!gScore.TryGetValue(nb, out int existing) || tentative < existing)
        {
            cameFrom[nb] = cur;
            gScore[nb] = tentative;
            open.Enqueue(nb, tentative + Heuristic(nbCoord, to));
        }
    }

    private static Vector2I[] ReconstructChunkPath(
        Dictionary<RegionNode, RegionNode> cameFrom, RegionNode cur)
    {
        var chunks = new List<Vector2I>();
        while (true)
        {
            if (chunks.Count == 0 || chunks[^1] != cur.Chunk)
                chunks.Add(cur.Chunk);
            if (!cameFrom.TryGetValue(cur, out var prev)) break;
            cur = prev;
        }
        chunks.Reverse();
        return [.. chunks];
    }

    private static NAV_NEIGHBOUR DirToNeighbour(Vector2I dir) =>
        dir.X == 1 ? NAV_NEIGHBOUR.EAST :
        dir.X == -1 ? NAV_NEIGHBOUR.WEST :
        dir.Y == 1 ? NAV_NEIGHBOUR.NORTH :
                      NAV_NEIGHBOUR.SOUTH;

    private static NAV_NEIGHBOUR Opposite(NAV_NEIGHBOUR t) => t switch
    {
        NAV_NEIGHBOUR.NORTH => NAV_NEIGHBOUR.SOUTH,
        NAV_NEIGHBOUR.EAST => NAV_NEIGHBOUR.WEST,
        NAV_NEIGHBOUR.SOUTH => NAV_NEIGHBOUR.NORTH,
        _ => NAV_NEIGHBOUR.EAST
    };

    #endregion

    #region Cell-based path

    public bool TryFindCellPath(Vector3 fromWorld, Vector3 toWorld, out Vector3[] path)
    {
        path = null;
        var fromChunk = GetChunkCoord(fromWorld, out var fromCell);
        var toChunk = GetChunkCoord(toWorld, out var toCell);

        if (!TryFindChunkPath(fromChunk, toChunk, fromCell, toCell, out var chunkPath))
            return false;

        // Build corridor bounds
        int minCX = int.MaxValue, minCZ = int.MaxValue;
        int maxCX = int.MinValue, maxCZ = int.MinValue;

        foreach (var c in chunkPath)
        {
            foreach (var d in Cardinals)
            {
                int cx = c.X + d.X, cz = c.Y + d.Y;
                if (cx < minCX) minCX = cx; if (cx > maxCX) maxCX = cx;
                if (cz < minCZ) minCZ = cz; if (cz > maxCZ) maxCZ = cz;
            }
            if (c.X < minCX) minCX = c.X; if (c.X > maxCX) maxCX = c.X;
            if (c.Y < minCZ) minCZ = c.Y; if (c.Y > maxCZ) maxCZ = c.Y;
        }

        int cW = maxCX - minCX + 1, cH = maxCZ - minCZ + 1;
        var corridorFlat = new bool[cW * cH];

        foreach (var c in chunkPath)
        {
            corridorFlat[(c.X - minCX) + (c.Y - minCZ) * cW] = true;
            foreach (var d in Cardinals)
            {
                int cx = c.X + d.X - minCX, cz = c.Y + d.Y - minCZ;
                if ((uint)cx < cW && (uint)cz < cH)
                    corridorFlat[cx + cz * cW] = true;
            }
        }

        int minX = minCX * CELLS_PER_CHUNK, minZ = minCZ * CELLS_PER_CHUNK;
        int cellW = cW * CELLS_PER_CHUNK, cellH = cH * CELLS_PER_CHUNK;
        int total = cellW * cellH;

        int CellIdx(Vector2I v) => (v.X - minX) + (v.Y - minZ) * cellW;
        bool InBounds(Vector2I v) { int x = v.X - minX, z = v.Y - minZ; return (uint)x < cellW && (uint)z < cellH; }
        bool InCorridor(Vector2I cc) { int x = cc.X - minCX, z = cc.Y - minCZ; return (uint)x < cW && (uint)z < cH && corridorFlat[x + z * cW]; }

        var visitedArr = System.Buffers.ArrayPool<int>.Shared.Rent(total);
        var cameFromArr = System.Buffers.ArrayPool<int>.Shared.Rent(total);

        try
        {
            var visited = visitedArr.AsSpan(0, total);
            var cameFrom = cameFromArr.AsSpan(0, total);
            visited.Clear();
            cameFrom.Fill(-1);

            var start = ToGlobal(fromChunk, fromCell);
            var goal = ToGlobal(toChunk, toCell);
            int startIdx = CellIdx(start), goalIdx = CellIdx(goal);

            var open = new PriorityQueue<int, int>();
            visited[startIdx] = 1;
            open.Enqueue(startIdx, Heuristic(start, goal));

            while (open.Count > 0)
            {
                int cur = open.Dequeue();
                if (visited[cur] < 0) continue;

                if (cur == goalIdx)
                {
                    path = CellsToWorld(StringPull(
                        ReconstructCells(cameFromArr, goalIdx, startIdx, minX, minZ, cellW)));
                    return true;
                }

                int curG = visited[cur] - 1;
                visited[cur] = -1;

                int cx = (cur % cellW) + minX, cz = (cur / cellW) + minZ;

                foreach (var d in Cardinals)
                {
                    var nb = new Vector2I(cx + d.X, cz + d.Y);
                    if (!InBounds(nb)) continue;
                    int nbIdx = CellIdx(nb);
                    if (visited[nbIdx] < 0) continue;

                    var nbc = ToChunk(nb);
                    if (!InCorridor(nbc) || !TryGetChunk(nbc, out var nc)) continue;
                    if (nc.IsFullyBlocked()) continue;
                    if (!nc.IsFreelyWalkable() &&
                        !nc[nb.X.AbsMod(CELLS_PER_CHUNK), nb.Y.AbsMod(CELLS_PER_CHUNK)]) continue;

                    int tentative = curG + 1;
                    if (visited[nbIdx] == 0 || tentative < visited[nbIdx] - 1)
                    {
                        visited[nbIdx] = tentative + 1;
                        cameFromArr[nbIdx] = cur;
                        open.Enqueue(nbIdx, tentative + Heuristic(nb, goal));
                    }
                }
            }

            return false;
        }
        finally
        {
            System.Buffers.ArrayPool<int>.Shared.Return(visitedArr);
            System.Buffers.ArrayPool<int>.Shared.Return(cameFromArr);
        }
    }

    #endregion

    #region Path simplification

    private List<Vector2I> StringPull(List<Vector2I> cells)
    {
        if (cells.Count <= 2) return cells;

        var result = new List<Vector2I> { cells[0] };
        int anchor = 0;
        int i = 2;

        while (i < cells.Count)
        {
            if (!HasLineOfSight(cells[anchor], cells[i]))
            {
                result.Add(cells[i - 1]);
                anchor = i - 1;
            }
            i++;
        }

        result.Add(cells[^1]);
        return result;
    }

    private bool HasLineOfSight(Vector2I from, Vector2I to)
    {
        int x0 = from.X, y0 = from.Y, x1 = to.X, y1 = to.Y;
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx - dy;
        Vector2I lastCC = new(int.MinValue, int.MinValue);
        NavChunk32 chunk = null;

        while (true)
        {
            var cc = ToChunk(new Vector2I(x0, y0));
            if (cc != lastCC) { if (!TryGetChunk(cc, out chunk)) return false; lastCC = cc; }
            if (chunk.IsFullyBlocked()) return false;
            if (!chunk.IsFreelyWalkable() && !chunk[x0.AbsMod(CELLS_PER_CHUNK), y0.AbsMod(CELLS_PER_CHUNK)]) return false;
            if (x0 == x1 && y0 == y1) break;
            int e2 = err * 2;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
        return true;
    }

    private Vector3[] CellsToWorld(List<Vector2I> cells)
    {
        var result = new Vector3[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            if (!TryGetChunk(ToChunk(cells[i]), out var chunk)) continue;
            result[i] = chunk.GetCellPosition(
                cells[i].X.AbsMod(CELLS_PER_CHUNK),
                cells[i].Y.AbsMod(CELLS_PER_CHUNK)) + Vector3.Up * 0.1f;
        }
        return result;
    }

    #endregion

    #region Helpers

    private static List<Vector2I> ReconstructCells(int[] cameFrom, int goalIdx, int startIdx, int minX, int minZ, int cellW)
    {
        var path = new List<Vector2I>();
        int idx = goalIdx;
        while (idx != startIdx)
        {
            path.Add(new((idx % cellW) + minX, (idx / cellW) + minZ));
            idx = cameFrom[idx];
            if (idx < 0) break;
        }
        path.Add(new((startIdx % cellW) + minX, (startIdx / cellW) + minZ));
        path.Reverse();
        return path;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2I ToGlobal(Vector2I chunk, Vector2I cell) => chunk * CELLS_PER_CHUNK + cell;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2I ToChunk(Vector2I global) => new(
        Mathf.FloorToInt((float)global.X / CELLS_PER_CHUNK),
        Mathf.FloorToInt((float)global.Y / CELLS_PER_CHUNK));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Heuristic(Vector2I a, Vector2I b)
        => Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I GetDirection(NAV_NEIGHBOUR type) => type switch
    {
        NAV_NEIGHBOUR.NORTH => new(0, 1),
        NAV_NEIGHBOUR.EAST => new(1, 0),
        NAV_NEIGHBOUR.SOUTH => new(0, -1),
        _ => new(-1, 0)
    };

    private static readonly Vector2I[] Cardinals = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    #endregion
}