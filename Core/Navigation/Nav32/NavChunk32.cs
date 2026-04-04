namespace Cutulu.Core;

using System;
using Godot;

using BitMask32 = uint;

/// <summary>
/// A pathfinding map with 32x32 chunks with 32bit chunk side hashes. 
/// Heavily inspired by Tynan Sylvester's RimWorld.
/// Written, with blood, sweat and tears, by Maximilian Schecklmann April 2nd to 4th 2026. 
/// </summary>
public class NavChunk32
{
    public const byte CELLS_PER_CHUNK = NavMap32.CELLS_PER_CHUNK;

    public readonly Vector2I Chunk;
    public readonly NavMap32 Map;

    public SwapbackArray<NavObstacle32> Obstacles;
    public SwapbackArray<NavRegion32> Regions;
    private readonly BoolByte[] Cells;

    public float CellSize => Map.CellSize;

    public NavChunk32(NavMap32 map, Vector2I chunk)
    {
        Chunk = chunk;
        Map = map;

        Cells = new BoolByte[(int)float.Ceiling(
            CELLS_PER_CHUNK * CELLS_PER_CHUNK /
            (float)BoolByte.BoolCountCapacity
        )];

        Array.Fill(Cells, BoolByte.True);
        Obstacles = null;
        Regions = null;
    }

    public bool IsFreelyWalkable() => Regions.IsEmpty() && Obstacles.IsEmpty();

    public bool IsFullyBlocked() => Regions.IsEmpty() && Obstacles.NotEmpty();

    public void Refresh(bool refreshNeighbours = true)
    {
        RefreshCells();
        RefreshRegions();

        if (refreshNeighbours)
            RefreshNeighbours();
    }

    public bool ConnectsTo(NAV_NEIGHBOUR type, out short localRegionIdx)
    {
        if (IsFreelyWalkable())
        {
            localRegionIdx = -1;
            return true;
        }

        if (Regions.NotEmpty())
        {
            NavRegion32 region;

            for (localRegionIdx = 0; localRegionIdx < Regions.Count; localRegionIdx++)
            {
                region = Regions[localRegionIdx];

                if (region.NotNull() && region.GetSideHash(type) > BitMask32.MinValue)
                    return true;
            }
        }

        localRegionIdx = -2;
        return false;
    }

    public bool TryGetRegion(byte x, byte z, out NavRegion32 region)
    {
        if (IsFreelyWalkable())
        {
            region = null;
            return true;
        }

        // Iterate through regions
        else if (this[x, z])
        {
            for (int i = 0; i < Regions.Count; i++)
            {
                region = Regions[i];

                if (region.NotNull() && region[x, z])
                    return true;
            }
        }

        region = null;
        return false;
    }

    #region Regions

    public void RefreshRegions()
    {
        // No obstacles, no regions
        if (Obstacles.IsEmpty())
        {
            Regions = null;
            return;
        }

        // Initialize Regions array
        if (Regions.IsNull()) Regions = [];
        else Regions.Clear();

        int length = CELLS_PER_CHUNK;
        int totalCells = length * length;

        // Keep track of visited cells
        var visited = new BoolByte[Cells.Length];

        // Temporary stack for flood-fill
        const int MAX = CELLS_PER_CHUNK * CELLS_PER_CHUNK;
        Span<int> stack = stackalloc int[MAX];

        var neighbours = CellNeighbours;

        for (int startIdx = 0; startIdx < totalCells; startIdx++)
        {
            // Already visited or blocked?
            int cellIndex = startIdx / BoolByte.BoolCountCapacity;
            int bitIndex = startIdx % BoolByte.BoolCountCapacity;

            if (!this[startIdx] || visited[cellIndex][bitIndex])
                continue;

            // Create a new region with its own BoolByte array
            var region = new NavRegion32();

            int stackSize = 0;
            stack[stackSize++] = startIdx;

            while (stackSize > 0)
            {
                int idx = stack[--stackSize];
                cellIndex = idx / BoolByte.BoolCountCapacity;
                bitIndex = idx % BoolByte.BoolCountCapacity;

                if (!this[idx] || visited[cellIndex][bitIndex])
                    continue;

                // Mark visited
                var v = visited[cellIndex];
                v[bitIndex] = true;
                visited[cellIndex] = v;

                // Compute x/z
                int x = idx % length;
                int z = idx / length;

                // Mark cell in **region only**
                region[x, z] = true;

                // Flood-fill neighbours
                for (int n = 0; n < 4; n++)
                {
                    int nx = x + neighbours[n].X;
                    int nz = z + neighbours[n].Z;

                    if ((uint)nx >= length || (uint)nz >= length)
                        continue;

                    int nIdx = nx + nz * length;
                    int ni = nIdx / BoolByte.BoolCountCapacity;
                    int nb = nIdx % BoolByte.BoolCountCapacity;

                    if (this[nIdx] && !visited[ni][nb])
                        stack[stackSize++] = nIdx;
                }
            }

            // Add completed region
            region.FinalizeRegion(); // computes side hashes once, here, not in RefreshNeighbours
            Regions.Add(region);
        }
    }

    public void RefreshNeighbours()
    {
        if (Regions.IsEmpty()) return;

        for (int i = 0; i < Regions.Count; i++)
            Regions[i].RefreshNeighbours(Chunk, Map.GetChunk);
    }

    #endregion

    #region Positions and Bounds

    public Vector3 GetCellPosition(int x, int z, float pivot = 0.5f, float y = 0.0f)
    {
        Vector2 cell = CellSize * (
            Chunk * CELLS_PER_CHUNK + new Vector2(x + pivot, z + pivot)
        );
        return new Vector3(cell.X, y, cell.Y);
    }

    public Vector3 GetCenterPosition(float y = 0.0f) => (
        GetCellPosition(0, 0, 0.0f, y) +
        GetCellPosition(CELLS_PER_CHUNK - 1, CELLS_PER_CHUNK - 1, 1.0f, y)
    ) * 0.5f;

    public Aabb GetAabb()
    {
        var a = GetCellPosition(0, 0, 0.0f, 0.0f);
        var b = GetCellPosition(CELLS_PER_CHUNK - 1, CELLS_PER_CHUNK - 1, 1.0f, 1000.0f);
        return new Aabb(a, b - a);
    }

    #endregion

    #region Obstacles

    public bool AddObstacle(NavObstacle32 obstacle)
    {
        if (
            obstacle.IsNull() ||
            obstacle.IsValid() == false ||
            obstacle.Bounds.Intersects(GetAabb()) == false
        ) return false;

        if (Obstacles.IsNull()) Obstacles = [obstacle];
        else Obstacles.Add(obstacle);

        return true;
    }

    public bool RemoveObstacle(NavObstacle32 obstacle)
    {
        if (Obstacles.IsNull() || obstacle.IsNull()) return false;

        for (int i = Obstacles.Count - 1; i >= 0; i--)
        {
            if (Obstacles[i].Equals(obstacle) || obstacle.Validator == Obstacles[i].Validator)
            {
                Obstacles.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Cells

    public void RefreshCells()
    {
        // Purge invalidated obstacles
        if (Obstacles.NotNull())
        {
            for (int i = Obstacles.Count - 1; i >= 0; i--)
                if (Obstacles[i].IsValid() == false)
                    Obstacles.RemoveAt(i);

            if (Obstacles.Count == 0) Obstacles = null;
        }

        // Rebuild cell data from scratch
        Array.Fill(Cells, BoolByte.True);

        if (Obstacles.NotNull())
        {
            var self = GetAabb();
            foreach (var col in Obstacles)
            {
                if (col.Aabb.NotEmpty())
                    foreach (var a in col.Aabb)
                        SetWalkable(self, a);

                if (col.Obb.NotEmpty())
                    foreach (var o in col.Obb)
                        SetWalkable(self, o);
            }
        }

        Map.InvalidatePathCache();
    }

    private void SetWalkable(Aabb self, Aabb other, bool walkable = false)
    {
        if (self.Intersects(other) == false) return;

        int size = CELLS_PER_CHUNK;
        float cellSize = CellSize;
        float originX = Chunk.X * size * cellSize;
        float originZ = Chunk.Y * size * cellSize;

        int minX = Mathf.Clamp(Mathf.FloorToInt((other.Position.X - originX) / cellSize), 0, size - 1);
        int minZ = Mathf.Clamp(Mathf.FloorToInt((other.Position.Z - originZ) / cellSize), 0, size - 1);
        int maxX = Mathf.Clamp(Mathf.FloorToInt((other.End.X - originX) / cellSize), 0, size - 1);
        int maxZ = Mathf.Clamp(Mathf.FloorToInt((other.End.Z - originZ) / cellSize), 0, size - 1);

        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
                this[x, z] = walkable;
    }

    private void SetWalkable(Aabb self, Obb obb, bool walkable = false)
    {
        if (!obb.Intersects(self)) return;

        byte size = CELLS_PER_CHUNK;
        float cellSize = CellSize;

        int baseX = Chunk.X * size;
        int baseZ = Chunk.Y * size;

        byte x, z;

        for (x = 0; x < size; x++)
            for (z = 0; z < size; z++)
            {
                if (this[x, z] == walkable) continue;

                float wx = (baseX + x + 0.5f) * cellSize;
                float wz = (baseZ + z + 0.5f) * cellSize;
                if (obb.ContainsPoint(new Vector3(wx, obb.Center.Y, wz)))
                    this[x, z] = walkable;
            }
    }

    private static readonly (sbyte X, sbyte Z)[] CellNeighbours =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1)
    ];

    public bool this[int x, int z]
    {
        get => this[x + z * CELLS_PER_CHUNK];
        set => this[x + z * CELLS_PER_CHUNK] = value;
    }

    public bool this[int idx]
    {
        get => Cells[idx / BoolByte.BoolCountCapacity][idx % BoolByte.BoolCountCapacity];
        set
        {
            int i = (int)float.Floor(idx / (float)BoolByte.BoolCountCapacity);
            var b = Cells[i];
            b[idx % BoolByte.BoolCountCapacity] = value;
            Cells[i] = b;
        }
    }

    #endregion
}