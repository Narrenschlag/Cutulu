namespace Cutulu
{
    using Godot;

    public static class Hexe1
    {
        #region Indexes

        /// <summary>
        /// Returns ring value of an index
        /// </summary>
        public static int GetRingIndex(int index)
        {
            return Mathf.CeilToInt((Mathf.Sqrt(12 * index + 9) - 3) / Hexe2.Neighbours.Length);
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
            var index = GetStartIndex(ring); // Get starting index for the ring

            // Get the number of cells in the ring and calculate side length
            var ringLength = Hexe.GetCellCountInRing(ring);
            var sideLength = ringLength / Hexe3.Neighbours.Length;

            // Iterate over the segments to determine the correct one
            for (byte i = 0; i < Hexe3.Neighbours.Length; i++)
            {
                // Starting position of the segment
                var start = Hexe3.Neighbours[i] * ring;

                // Ending position of the segment
                var end = Hexe3.Neighbours[(i + 1) % Hexe3.Neighbours.Length] * ring;

                // Check if the cubic coordinate is along this segment
                var delta = cubic - start;

                // Check bounds: delta must be non-negative and within segment length
                if (delta.X * end.X >= 0 && delta.Y * end.Y >= 0 && delta.Z * end.Z >= 0 &&
                    Mathf.Abs(delta.X + delta.Y + delta.Z) <= sideLength)
                {
                    // Offset within the segment
                    var offset = Mathf.Abs(delta.X).max(Mathf.Abs(delta.Y), Mathf.Abs(delta.Z));
                    return index + i * sideLength + offset;
                }
            }

            // Fallback: Check against all cells in the ring if segment match fails
            var cells = Hexe3.GetRing(default, ring);
            for (int i = 0; i < ringLength; i++)
            {
                if (cells[i] == cubic)
                    return index + i;
            }

            // If nothing matches, return the starting index as a fallback
            return index;
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
        /// Converts a world position into an index (Vector3) 
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

        #region Backend

        /// <summary>
        /// Helper function to find the position of an axial coordinate within a ring
        /// Really wished this was simpler to find somewhere so enjoy it. It scales.
        /// </summary>
        private static int GetPositionInRing(Vector2I axial, int ring)
        {
            // Start with the first hex in the ring, offset from the center hex
            var currentHex = Hexe2.Neighbours[4] * ring;
            if (currentHex == axial) return 0;

            var _i = 0;

            var ang = Vector2.Zero.GetAngleD(axial).AbsMod(360);
            if (ang <= 0) ang = 360;

            for (byte i = 0; i < Hexe2.Neighbours.Length; i++)
            {
                var min = Vector2.Zero.GetAngleD(Hexe2.Neighbours.ModulatedElement(i - 2));

                var max = Vector2.Zero.GetAngleD(Hexe2.Neighbours.ModulatedElement(i - 1)).AbsMod(360);
                if (max <= 0) max = 360;

                if (min >= ang || ang > max)
                {
                    currentHex += Hexe2.Neighbours[i] * ring;
                    _i += ring;
                }

                else break;
            }

            return _i + Mathf.FloorToInt(Hexe2.GetDistance(currentHex, axial));
        }

        #endregion
    }
}