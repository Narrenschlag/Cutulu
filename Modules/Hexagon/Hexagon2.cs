namespace Cutulu
{
    using System;
    using Godot;

    public static class Hexagon2
    {
        public static readonly Vector2I[] Neighbours = new Vector2I[]{
            new(+1, -1),
            new(+1, +0),
            new(+0, +1),

            new(-1, +1),
            new(-1, +0),
            new(+0, -1),
        };

        public static readonly float ReferenceAngle = Vector2Extension.GetAngleD(Neighbours[0]).AbsMod(360f);

        /// <summary>
        /// Returns distance between two points
        /// </summary>
        public static float GetDistance(Vector2I a, Vector2I b)
        {
            return (Mathf.Abs(a.X - b.X)
            + Mathf.Abs(a.X + a.Y - b.X - b.Y)
            + Mathf.Abs(a.Y - b.Y)) / 2;
        }

        #region Conversion

        /// <summary>
        /// Convert an cubic to axial (q, r)
        /// </summary>
        public static Vector2I ToAxial(Vector3I cubic)
        {
            // Ignore s, as it is derived from q and r
            return new(
                cubic.X, // q
                cubic.Y // r
            );
        }

        /// <summary>
        /// Convert an index to axial coordinates (q, r)
        /// </summary>
        public static Vector2I ToAxial(int index)
        {
            if (index == 0) return default;

            var ring = Hexagon1.GetRingIndex(index);

            // Milestone
            index -= Hexagon1.GetStartIndex(ring);

            var sideLength = Hexagon.GetCellCountInRing(ring) / Hexagon.Num; // ringLength - neighbourCount
            var sideIndex = Mathf.FloorToInt((float)index / sideLength);

            return Neighbours[sideIndex] * ring // start
            + (Neighbours.ModulatedElement(sideIndex + 1) - Neighbours[sideIndex]) // delta
            * (index - sideIndex * sideLength); // offset
        }

        #endregion

        #region World Space

        /// <summary>
        /// Converts axial to a world position 
        /// </summary>
        public static Vector3 ToWorld(Vector2I axial, Orientation orientation)
        {
            return Hexagon3.ToWorld(Hexagon3.ToCubic(axial), orientation);
        }

        /// <summary>
        /// Converts a world position to hexagonal coordinates
        /// </summary>
        public static Vector2I ToAxial(Vector3 position, Orientation orientation)
        {
            return ToAxial(Hexagon3.ToCubic(position, orientation));
        }

        /// <summary>
        /// Returns the six corner points of the hexagon centered at hex coordinates
        /// </summary>
        public static Vector3[] GetVertices(Vector2I axial, Orientation orientation)
        {
            if (orientation == null) return Array.Empty<Vector3>();

            var neighbours = new Vector3[Hexagon.Num];
            var corners = new Vector3[Hexagon.Num];

            var world = ToWorld(axial, orientation);

            for (int i = 0; i < Hexagon.Num; i++)
            {
                neighbours[i] = ToWorld(axial + Neighbours[i], orientation);
            }

            for (int i = 0; i < Hexagon.Num; i++)
            {
                corners[i] = (world + neighbours[i] + neighbours.ModulatedElement(i - 1)) / 3f;
            }

            return corners;
        }

        /// <summary>
        /// Returns a single corner of a hexagon (i is 0 to 5)
        /// </summary>
        public static Vector3 GetVertice(Vector2I axial, int cornerIndex, Orientation orientation)
        {
            var world = ToWorld(axial, orientation);

            var a = ToWorld(axial + Neighbours.ModulatedElement(cornerIndex), orientation);
            var z = ToWorld(axial + Neighbours.ModulatedElement(cornerIndex - 1), orientation);

            return (world + a + z) / 3f;
        }

        /// <summary>
        /// Returns closest world position on given axial to given position
        /// </summary>
        public static Vector3 GetClosestPoint(Vector2I axial, Vector3 position, Orientation orientation)
        {
            if (ToAxial(position, orientation).Equals(axial)) return position;

            var vertices = GetVertices(axial, orientation).SortByDistanceTo(position);

            return position.TryIntersectFlat(ToWorld(axial, orientation), vertices[0], vertices[1] - vertices[0], out var C) ? C : position;
        }

        #endregion

        #region Arrays

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public static Vector2I[] GetRange(Vector2I axial, int ringCount)
        {
            if ((ringCount = Mathf.Abs(ringCount)) < 1)
                return new[] { axial };

            var result = new Vector2I[1 + 3 * ringCount * (ringCount + 1)];
            var N = ringCount;

            for (int i = 0, q = -N; -N <= q && q <= +N; q++)
            {
                for (var r = Mathf.Max(-N, -q - N); Mathf.Max(-N, -q - N) <= r && r <= Mathf.Min(+N, -q + N); r++)
                {
                    result[i++] = axial + new Vector2I(q, r);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the neighboring hexagons in a specific ring, centered around the given hex
        /// </summary>
        public static Vector2I[] GetRing(Vector2I axial, int ring)
        {
            if ((ring = Mathf.Abs(ring)) < 1)
                return new[] { axial };

            var result = new Vector2I[Hexagon.GetCellCountInRing(ring)];
            var sideLength = result.Length / Hexagon.Num;

            // Start with the first hex in the ring, offset from the center hex
            var currentHex = axial + Neighbours[0] * ring;

            for (byte k = 0; k < Hexagon.Num; k++)
            {
                var delta = Neighbours.ModulatedElement(k + 1) - Neighbours[k];

                for (ushort n = 0; n < sideLength; n++)
                {
                    result[k * sideLength + n] = currentHex;
                    currentHex += delta;
                }
            }

            return result;
        }

        #endregion

        #region Indexes

        /// <summary>
        /// Returns ring value of an index
        /// </summary>
        public static int GetRingIndex(Vector2I axial)
        {
            return Mathf.CeilToInt(GetDistance(axial, default));
        }

        #endregion

        #region Pathfinding

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public static bool TryGetPath(IPathfindingTarget target, Vector2I start, Vector2I end, out Vector2I[] path, Orientation orientation)
        {
            return (path = GetPath(target, start, end, orientation)).NotEmpty();
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public static Vector2I[] GetPath(IPathfindingTarget target, Vector2I start, Vector2I end, Orientation orientation)
        {
            if (orientation == null) return Array.Empty<Vector2I>();

            Pathfinding.TryFindPath(target, start, end, out var path);

            return path;
        }

        #endregion
    }
}