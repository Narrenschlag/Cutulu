namespace Cutulu
{
    using System;
    using Godot;

    public static class Hexe3
    {
        public static readonly Vector3I[] Neighbours = new Vector3I[]{
            new(+1, -1, +0),
            new(+1, +0, -1),
            new(+0, +1, -1),

            new(-1, +1, +0),
            new(-1, +0, +1),
            new(+0, -1, +1),
        };

        /// <summary>
        /// Returns distance between two points
        /// </summary>
        public static float GetDistance(Vector3I a, Vector3I b)
        {
            return (Mathf.Abs(a.X - b.X)
            + Mathf.Abs(a.Y - b.Y)
            + Mathf.Abs(a.Z - b.Z)) / 2;
        }

        #region Conversion

        /// <summary>
        /// Convert axial to cubic coordinates (q, r, s)
        /// </summary>
        public static Vector3I ToCubic(Vector2I axial)
        {
            return new(
                axial.X, // q
                axial.Y, // r
                -axial.X - axial.Y // s = -q - r for cube coordinates
            );
        }

        /// <summary>
        /// Convert an index to cubic coordinates (q, r, s)
        /// </summary>
        public static Vector3I ToCubic(int index)
        {
            return ToCubic(Hexe2.ToAxial(index));
        }

        #endregion

        #region World Space

        /// <summary>
        /// Converts a cubic into a world position 
        /// </summary>
        public static Vector3 ToWorld(Vector3I cubic, Orientation orientation)
        {
            var x = 3f / 2f * cubic.X;
            var y = Mathf.Sqrt(3) * (cubic.Z + cubic.X / 2f);

            return orientation.Right * x + orientation.Forward * y; // Y is set to 0 (ground level)
        }

        /// <summary>
        /// Converts a world position into a cubic 
        /// </summary>
        public static Vector3I ToCubic(Vector3 position, Orientation orientation)
        {
            var position2D = new Vector2(orientation.Right.Dot(position), orientation.Forward.Dot(position));

            var q = 2f / 3f * position2D.X;
            var r = -1f / 3f * position2D.X + Mathf.Sqrt(3) / 3f * position2D.Y;
            var s = -q - r;

            return CubicRound(new Vector3(q, s, r));
        }

        /// <summary>
        /// Returns the six corner points of the hexagon centered at hex coordinates
        /// </summary>
        public static Vector3[] GetVertices(Vector3I cubic, Orientation orientation)
        {
            if (orientation == null) return Array.Empty<Vector3>();

            var neighbours = new Vector3[Hexe.Num];
            var corners = new Vector3[Hexe.Num];

            var world = ToWorld(cubic, orientation);

            for (int i = 0; i < Hexe.Num; i++)
            {
                neighbours[i] = ToWorld(cubic + Neighbours[i], orientation);
            }

            for (int i = 0; i < Hexe.Num; i++)
            {
                corners[i] = (world + neighbours[i] + neighbours.ModulatedElement(i - 1)) / 3f;
            }

            return corners;
        }

        /// <summary>
        /// Returns a single corner of a hexagon (i is 0 to 5)
        /// </summary>
        public static Vector3 GetVertice(Vector3I cubic, int cornerIndex, Orientation orientation)
        {
            var world = ToWorld(cubic, orientation);

            var a = ToWorld(cubic + Neighbours.ModulatedElement(cornerIndex), orientation);
            var z = ToWorld(cubic + Neighbours.ModulatedElement(cornerIndex - 1), orientation);

            return (world + a + z) / 3f;
        }

        /// <summary>
        /// Returns closest world position on given cubic to given position
        /// </summary>
        public static Vector3 GetClosestPoint(Vector3I cubic, Vector3 position, Orientation orientation)
        {
            if (ToCubic(position, orientation).Equals(cubic)) return position;

            var vertices = GetVertices(cubic, orientation).SortByDistanceTo(position);

            return position.TryIntersectFlat(ToWorld(cubic, orientation), vertices[0], vertices[1] - vertices[0], out var C) ? C : position;
        }

        #endregion

        #region Arrays

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public static Vector3I[] GetRange(Vector3I cubic, int ringCount)
        {
            if ((ringCount = Mathf.Abs(ringCount)) < 1)
                return new[] { cubic };

            var result = new Vector3I[1 + 3 * ringCount * (ringCount + 1)];
            var N = ringCount;

            for (int i = 0, q = -N; -N <= q && q <= +N; q++)
            {
                for (var r = Mathf.Max(-N, -q - N); Mathf.Max(-N, -q - N) <= r && r <= Mathf.Min(+N, -q + N); r++)
                {
                    result[i++] = cubic + new Vector3I(q, r, -q - r);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the neighboring hexagons in a specific ring, centered around the given hex
        /// </summary>
        public static Vector3I[] GetRing(Vector3I cubic, int ring)
        {
            if ((ring = Mathf.Abs(ring)) < 1)
                return new[] { cubic };

            var result = new Vector3I[Hexe.GetCellCountInRing(ring)];
            var sideLength = result.Length / Hexe.Num;

            // Start with the first hex in the ring, offset from the center hex
            var currentHex = cubic + Neighbours[0] * ring;

            for (byte k = 0; k < Hexe.Num; k++)
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
        public static int GetRingIndex(Vector3I cubic)
        {
            return Mathf.CeilToInt(GetDistance(cubic, default));
        }

        #endregion

        #region Pathfinding

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public static bool TryGetPath(IPathfindingTarget target, Vector3I start, Vector3I end, out Vector3I[] path)
        {
            return (path = GetPath(target, start, end)).NotEmpty();
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public static Vector3I[] GetPath(IPathfindingTarget target, Vector3I start, Vector3I end)
        {
            Pathfinding.TryFindPath(target, Hexe2.ToAxial(start), Hexe2.ToAxial(end), out var _path);

            var path = new Vector3I[_path.Length];

            for (int i = 0; i < _path.Length; i++)
            {
                path[i] = ToCubic(_path[i]);
            }

            return path;
        }

        #endregion

        #region Backend

        /// <summary>
        /// Rounds cube coordinates to nearest hexagonal coordinates
        /// </summary>
        private static Vector3I CubicRound(Vector3 cubic)
        {
            var rx = Mathf.RoundToInt(cubic.X);
            var ry = Mathf.RoundToInt(cubic.Y);
            var rz = Mathf.RoundToInt(cubic.Z);

            var x_diff = Mathf.Abs(rx - cubic.X);
            var y_diff = Mathf.Abs(ry - cubic.Y);
            var z_diff = Mathf.Abs(rz - cubic.Z);

            if (x_diff > y_diff && x_diff > z_diff)
            {
                rx = -ry - rz;
            }

            else if (y_diff > z_diff)
            {
                ry = -rx - rz;
            }

            else
            {
                rz = -rx - ry;
            }

            return new(rx, ry, rz);
        }

        #endregion
    }
}