#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using Godot;

    public static class Hexagon1
    {
        /// <summary>
        /// Returns distance between two points
        /// </summary>
        public static float GetDistance(int a, int b)
        {
            return Hexagon2.GetDistance(Hexagon2.ToAxial(a), Hexagon2.ToAxial(b));
        }

        #region Neighbours

        public static int GetNeighbour(this int index, int neighbourIndex)
        {
            return ToIndex(Hexagon2.GetNeighbour(Hexagon2.ToAxial(index), neighbourIndex));
        }

        #endregion

        #region Indexes

        /// <summary>
        /// Returns ring value of an index
        /// </summary>
        public static int GetRingIndex(int index)
        {
            return Mathf.CeilToInt((Mathf.Sqrt(12 * index + 9) - 3) / Hexagon.Num);
        }

        /// <summary>
        /// Returns start index of a ring
        /// </summary>
        public static int GetStartIndex(int ring)
        {
            return Hexagon.GetCellCountInRange(ring - 1);
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Convert axial coordinates (q, r, s) to an index
        /// </summary>
        public static int ToIndex(Vector2I axial)
        {
            return ToIndex(Hexagon3.ToCubic(axial));
        }

        /// <summary>
        /// Convert cubic coordinates (q, r, s) to an index.
        /// </summary>
        public static int ToIndex(Vector3I cubic)
        {
            if (cubic == default) return 0;

            // Determine which ring the cubic coordinate belongs to
            var ring = Hexagon3.GetRingIndex(cubic);

            // Get segment
            var i = Hexagon2.GetSegment(Hexagon2.ToAxial(cubic));

            var delta = cubic // Check if the cubic coordinate is along this segment
            - Hexagon3.Neighbours[i] * ring; // Starting position of the segment

            return GetStartIndex(ring)
            + i * Hexagon.GetCellCountInRing(ring) / Hexagon.Num // Get the number of cells in the ring and calculate side length
            + Mathf.Abs(delta.X).max(Mathf.Abs(delta.Y), Mathf.Abs(delta.Z)); // Offset within the segment
        }

        #endregion

        #region Arrays

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public static int[] GetRange(int index, int ringCount)
        {
            var range = Hexagon2.GetRange(Hexagon2.ToAxial(index), ringCount);
            var iRange = new int[range.Length];

            for (var i = 0; i < range.Length; i++)
            {
                iRange[i] = ToIndex(range[i]);
            }

            return iRange;
        }

        /// <summary>
        /// Returns the neighboring hexagons in a specific ring, centered around the given hex
        /// </summary>
        public static int[] GetRing(int index, int ringCount)
        {
            var ring = Hexagon2.GetRing(Hexagon2.ToAxial(index), ringCount);
            var iRing = new int[ring.Length];

            for (var i = 0; i < ring.Length; i++)
            {
                iRing[i] = ToIndex(ring[i]);
            }

            return iRing;
        }

        #endregion

        #region World Space

        /// <summary>
        /// Converts an index into a world position 
        /// </summary>
        public static Vector3 ToWorld(int index, Orientation orientation)
        {
            return orientation == null ? default : Hexagon2.ToWorld(Hexagon2.ToAxial(index), orientation);
        }

        /// <summary>
        /// Converts a world position into an index
        /// </summary>
        public static int ToIndex(Vector3 position, Orientation orientation)
        {
            return orientation == null ? default : ToIndex(Hexagon2.ToAxial(position, orientation));
        }

        /// <summary>
        /// Returns the six corner points of the hexagon centered at hex coordinates
        /// </summary>
        public static Vector3[] GetVertices(Orientation orientation)
        {
            return GetVertices(default, orientation);
        }

        /// <summary>
        /// Returns the six corner points of the hexagon centered at hex coordinates
        /// </summary>
        public static Vector3[] GetVertices(int index, Orientation orientation)
        {
            return Hexagon2.GetVertices(Hexagon2.ToAxial(index), orientation);
        }

        /// <summary>
        /// Returns a single corner of a hexagon (i is 0 to 5)
        /// </summary>
        public static Vector3 GetVertice(int index, int cornerIndex, Orientation orientation)
        {
            return Hexagon2.GetVertice(Hexagon2.ToAxial(index), cornerIndex, orientation);
        }

        /// <summary>
        /// Returns closest world position on given index to given position
        /// </summary>
        public static Vector3 GetClosestPoint(int index, Vector3 position, Orientation orientation)
        {
            return Hexagon2.GetClosestPoint(Hexagon2.ToAxial(index), position, orientation);
        }

        #endregion

        #region Pathfinding

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public static bool TryGetPath(IPathfindingTarget target, int start, int end, out int[] path)
        {
            return (path = GetPath(target, start, end)).NotEmpty();
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public static int[] GetPath(IPathfindingTarget target, int start, int end)
        {
            Pathfinding.TryFindPath(target, Hexagon2.ToAxial(start), Hexagon2.ToAxial(end), out var _path);

            var path = new int[_path.Length];

            for (int i = 0; i < _path.Length; i++)
            {
                path[i] = ToIndex(_path[i]);
            }

            return path;
        }

        #endregion
    }
}
#endif