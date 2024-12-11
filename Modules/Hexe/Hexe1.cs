namespace Cutulu
{
    using Godot;

    public static class Hexe1
    {
        /// <summary>
        /// Returns distance between two points
        /// </summary>
        public static float GetDistance(int a, int b)
        {
            return Hexe2.GetDistance(Hexe2.ToAxial(a), Hexe2.ToAxial(b));
        }

        #region Indexes

        /// <summary>
        /// Returns ring value of an index
        /// </summary>
        public static int GetRingIndex(int index)
        {
            return Mathf.CeilToInt((Mathf.Sqrt(12 * index + 9) - 3) / Hexe.Num);
        }

        /// <summary>
        /// Returns start index of a ring
        /// </summary>
        public static int GetStartIndex(int ring)
        {
            return Hexe.GetCellCountInRange(ring - 1);
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Convert axial coordinates (q, r, s) to an index
        /// </summary>
        public static int ToIndex(Vector2I axial)
        {
            return ToIndex(Hexe3.ToCubic(axial));
        }

        /// <summary>
        /// Convert cubic coordinates (q, r, s) to an index.
        /// </summary>
        public static int ToIndex(Vector3I cubic)
        {
            if (cubic == default) return 0;

            // Determine which ring the cubic coordinate belongs to
            var ring = Hexe3.GetRingIndex(cubic);

            var i = Mathf.FloorToInt(
                (Vector2Extension.GetAngleD(Hexe2.ToAxial(cubic)) - Hexe2.ReferenceAngle).AbsMod(360f) // Calculate angle of given cubic in axial space
                / 45f) switch // Determine segment using switch statement on 45° segments
            {
                0 => 0,
                1 => 1,
                2 => 1,
                3 => 2,
                4 => 3,
                5 => 4,
                6 => 4,
                _ => 5,
            };

            var delta = cubic // Check if the cubic coordinate is along this segment
            - Hexe3.Neighbours[i] * ring; // Starting position of the segment

            return GetStartIndex(ring)
            + i * Hexe.GetCellCountInRing(ring) / Hexe.Num // Get the number of cells in the ring and calculate side length
            + Mathf.Abs(delta.X).max(Mathf.Abs(delta.Y), Mathf.Abs(delta.Z)); // Offset within the segment
        }


        #endregion

        #region Arrays

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public static int[] GetRange(int index, int ringCount)
        {
            var range = Hexe2.GetRange(Hexe2.ToAxial(index), ringCount);
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
            var ring = Hexe2.GetRing(Hexe2.ToAxial(index), ringCount);
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
            return orientation == null ? default : Hexe2.ToWorld(Hexe2.ToAxial(index), orientation);
        }

        /// <summary>
        /// Converts a world position into an index
        /// </summary>
        public static int ToIndex(Vector3 position, Orientation orientation)
        {
            return orientation == null ? default : ToIndex(Hexe2.ToAxial(position, orientation));
        }

        /// <summary>
        /// Returns the six corner points of the hexagon centered at hex coordinates
        /// </summary>
        public static Vector3[] GetVertices(int index, Orientation orientation)
        {
            return Hexe2.GetVertices(Hexe2.ToAxial(index), orientation);
        }

        /// <summary>
        /// Returns a single corner of a hexagon (i is 0 to 5)
        /// </summary>
        public static Vector3 GetVertice(int index, int cornerIndex, Orientation orientation)
        {
            return Hexe2.GetVertice(Hexe2.ToAxial(index), cornerIndex, orientation);
        }

        /// <summary>
        /// Returns closest world position on given index to given position
        /// </summary>
        public static Vector3 GetClosestPoint(int index, Vector3 position, Orientation orientation)
        {
            return Hexe2.GetClosestPoint(Hexe2.ToAxial(index), position, orientation);
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
            Pathfinding.TryFindPath(target, Hexe2.ToAxial(start), Hexe2.ToAxial(end), out var _path);

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