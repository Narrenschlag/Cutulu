#if GODOT4_0_OR_GREATER
namespace Cutulu.Core;

using System.Runtime.CompilerServices;
using System;
using Godot;

using BitMask32 = uint;

/// <summary>
/// A pathfinding map with 32x32 chunks with 32bit chunk side hashes. 
/// Heavily inspired by Tynan Sylvester's RimWorld.
/// Written, with blood, sweat and tears, by Maximilian Schecklmann April 2nd to 4th 2026. 
/// </summary>
public class NavRegion32
{
    public const byte CELLS_PER_CHUNK = NavMap32.CELLS_PER_CHUNK;

    private readonly SwapbackArray<NavNeighbour32> Neighbours = [];
    private readonly BitMask32[] SideHashes;
    public readonly BoolByte[] Cells;

    //private byte MinX, MinZ, MaxX, MaxZ;
    private int _walkableCount = 0;

    public int GetWalkableCount() => _walkableCount;

    public NavRegion32()
    {
        Cells = new BoolByte[(int)float.Ceiling(
            CELLS_PER_CHUNK * CELLS_PER_CHUNK /
            (float)BoolByte.BoolCountCapacity
        )];

        SideHashes = new BitMask32[4];

        /*MinX = byte.MaxValue;
        MinZ = byte.MaxValue;
        MaxX = byte.MinValue;
        MaxZ = byte.MinValue;*/
    }

    public void FinalizeRegion()
    {
        RefreshSideHashes();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Vector2I localPoint)
    => this[localPoint.X, localPoint.Y];

    /*public Vector3 GetCenterPosition(NavChunk32 chunk)
    {
        int x = MinX + (MaxX - MinX) / 2;
        int z = MinZ + (MaxZ - MinZ) / 2;
        return chunk.GetCellPosition(x, z);
    }*/

    #region Neighbours

    public bool IsIsolated() => Neighbours.IsEmpty();

    public bool HasAnyNeighbour() => Neighbours.NotEmpty();

    public ReadOnlySpan<NavNeighbour32> GetNeighbours() => Neighbours.AsSpan();

    public void RefreshNeighbours(Vector2I chunk, Func<Vector2I, NavChunk32> getNeighbourChunk)
    {
        Neighbours.Clear();

        NavChunk32 c;
        Vector2I dir;

        for (NAV_NEIGHBOUR i = NAV_NEIGHBOUR.NORTH; i <= NAV_NEIGHBOUR.WEST; i++)
        {
            var selfHash = GetSideHash(i);
            if (selfHash == BitMask32.MinValue) continue;

            dir = GetDirection(i);
            c = getNeighbourChunk?.Invoke(chunk + dir) ?? null;
            if (c.IsNull()) continue;

            if (c.IsFreelyWalkable())
            {
                Neighbours.Add(new(i, -1));
                continue;
            }
            else if (c.IsFullyBlocked()) continue;
            else if (c.Regions.NotEmpty())
            {
                short regionId = 0;

                foreach (var region in c.Regions)
                {
                    var otherHash = region.GetSideHash(Opposite(i));

                    if ((selfHash & otherHash) != 0)
                        Neighbours.Add(new(i, regionId));

                    regionId++;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NAV_NEIGHBOUR Opposite(NAV_NEIGHBOUR t) => t switch
    {
        NAV_NEIGHBOUR.NORTH => NAV_NEIGHBOUR.SOUTH,
        NAV_NEIGHBOUR.EAST => NAV_NEIGHBOUR.WEST,
        NAV_NEIGHBOUR.SOUTH => NAV_NEIGHBOUR.NORTH,
        _ => NAV_NEIGHBOUR.EAST
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I GetDirection(NAV_NEIGHBOUR type)
    => type switch
    {
        NAV_NEIGHBOUR.NORTH => new(0, 1),
        NAV_NEIGHBOUR.EAST => new(1, 0),
        NAV_NEIGHBOUR.SOUTH => new(0, -1),
        _ => new(-1, 0)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitMask32 GetSideHash(NAV_NEIGHBOUR type)
    => SideHashes[(byte)type];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitMask32 GetInvertedSideHash(NAV_NEIGHBOUR type)
    => GetSideHash(Opposite(type));

    public void RefreshSideHashes()
    {
        byte x, z, xv, zv, y, length = CELLS_PER_CHUNK - 1;
        BitMask32 hash;

        for (NAV_NEIGHBOUR i = NAV_NEIGHBOUR.NORTH; i <= NAV_NEIGHBOUR.WEST; i++)
        {
            switch (i)
            {
                case NAV_NEIGHBOUR.NORTH:
                    x = 0;
                    z = length;
                    xv = 1;
                    zv = 0;
                    break;

                case NAV_NEIGHBOUR.EAST:
                    x = length;
                    z = 0;
                    xv = 0;
                    zv = 1;
                    break;

                case NAV_NEIGHBOUR.SOUTH:
                    x = 0;
                    z = 0;
                    xv = 1;
                    zv = 0;
                    break;

                default:
                    x = 0;
                    z = 0;
                    xv = 0;
                    zv = 1;
                    break;
            }

            hash = BitMask32.MinValue;

            for (y = 0; y <= length; y++)
            {
                if (this[x + xv * y, z + zv * y])
                    hash |= 1u << y;
            }

            SideHashes[(byte)i] = hash;
        }
    }

    #endregion

    #region Indexing

    public bool this[int x, int z]
    {
        get => this[x + z * CELLS_PER_CHUNK];
        set => this[x + z * CELLS_PER_CHUNK] = value;
        /*{
            if (this[x + z * CELLS_PER_CHUNK] = value)
            {
                MinX = byte.Min(MinX, (byte)x);
                MaxX = byte.Max(MaxX, (byte)x);
                MinZ = byte.Min(MinZ, (byte)z);
                MaxZ = byte.Max(MaxZ, (byte)z);
            }
        }*/
    }

    public bool this[int idx]
    {
        get => Cells[idx / BoolByte.BoolCountCapacity][idx % BoolByte.BoolCountCapacity];
        set
        {
            int i = (int)float.Floor(idx / (float)BoolByte.BoolCountCapacity);
            var b = Cells[i];
            bool old = b[idx % BoolByte.BoolCountCapacity];

            if (old == value) return; // no change, skip write and count update

            b[idx % BoolByte.BoolCountCapacity] = value;
            Cells[i] = b;

            _walkableCount += value ? 1 : -1;
        }
    }

    public void Reset(bool walkable)
    {
        Array.Fill(Cells, walkable ? BoolByte.True : BoolByte.False);
        Array.Fill(SideHashes, walkable ? BitMask32.MaxValue : BitMask32.MinValue);

        _walkableCount = walkable ? CELLS_PER_CHUNK * CELLS_PER_CHUNK : 0;

        /*MinX = byte.MaxValue;
        MinZ = byte.MaxValue;
        MaxX = byte.MinValue;
        MaxZ = byte.MinValue;*/
    }

    #endregion
}
#endif