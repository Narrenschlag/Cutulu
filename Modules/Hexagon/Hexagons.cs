namespace Cutulu
{
    using Godot;

    /// <summary>
    /// Hexagonal Compass used to calculate points and positions on a hexagonal grid
    /// </summary>
    public readonly partial struct Hexagons
    {
        #region Local

        // Hexagonal properties
        public readonly Vector3 Forward, Right, Up;
        public readonly float CellSize;

        /// <summary>
        /// Constructor
        /// </summary>
        public Hexagons(Vector3 forward = default, float cellSize = 1f)
        {
            CellSize = cellSize;

            if (forward == default)
                forward = Vector3.Forward;

            Up = forward.toUp();
            Right = forward.toRight(Up);
            Forward = forward.Normalized();
        }

        /// <summary>
        /// Converts a world position (Vector3) to hexagonal coordinates
        /// </summary>
        public readonly Vector3I WorldToCubic(Vector3 position, Vector3 offset = default)
        {
            var position2D = new Vector2(Right.Dot(position -= offset), Forward.Dot(position));

            var q = 2f / 3f * position2D.X / CellSize;
            var r = (-1f / 3f * position2D.X + Mathf.Sqrt(3) / 3f * position2D.Y) / CellSize;
            var s = -q - r;

            return CubicRound(new Vector3(q, s, r));
        }

        /// <summary>
        /// Converts hexagonal coordinates to world position
        /// </summary>
        public Vector3 CubicToWorld(Vector3I cubic, Vector3 offset = default)
        {
            var x = CellSize * (3f / 2f * cubic.X);
            var y = CellSize * (Mathf.Sqrt(3) * (cubic.Z + cubic.X / 2f));

            return Right * x + Forward * y + offset; // Y is set to 0 (ground level)
        }

        /// <summary>
        /// Converts 2D grid coordinates to world position
        /// </summary>
        public Vector3 AxialToWorld(Vector2I axial, Vector3 offset = default)
        {
            return CubicToWorld(AxialToCubic(axial), offset);
        }

        /// <summary>
        /// Converts world position to hexagonal coordinates
        /// </summary>
        public Vector2I WorldToAxial(Vector3 position, Vector3 offset = default)
        {
            return CubicToAxial(WorldToCubic(position, offset));
        }

        /// <summary>
        /// Converts a world position into an index (Vector3) 
        /// </summary>
        public readonly int WorldToIndex(Vector3 position, Vector3 offset = default)
        {
            return AxialToIndex(WorldToAxial(position, offset));
        }

        /// <summary>
        /// Converts an index to world position (Vector3)
        /// </summary>
        public readonly Vector3 IndexToWorld(int index, Vector3 offset = default)
        {
            return AxialToWorld(IndexToAxial(index), offset);
        }

        /// <summary>
        /// Returns the six corner points of the hexagon centered at hex coordinates
        /// </summary>
        public Vector3[] GetVertices(Vector3I cubic)
        {
            var corners = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                corners[i] = GetVertice(cubic, i);
            }

            return corners;
        }

        /// <summary>
        /// Returns a single corner of a hexagon (i is 0 to 5)
        /// </summary>
        public Vector3 GetVertice(Vector3I cubic, int cornerIndex)
        {
            var dir = Forward.Rotated(Up, (60f * cornerIndex + 30f).toRadians()); // 60° between corners

            return CubicToWorld(cubic) + dir * CellSize;
        }

        /// <summary>
        /// Returns the six corner points of the hexagon centered at hex coordinates
        /// </summary>
        public Vector3[] GetVertices(Vector2I axial)
        {
            var corners = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                corners[i] = GetVertice(axial, i);
            }

            return corners;
        }

        /// <summary>
        /// Returns a single corner of a hexagon (i is 0 to 5)
        /// </summary>
        public Vector3 GetVertice(Vector2I axial, int cornerIndex)
        {
            var dir = Forward.Rotated(Up, (60f * cornerIndex + 30f).toRadians()); // 60° between corners

            return AxialToWorld(axial) + dir * CellSize;
        }

        #endregion

        #region Static

        /// <summary>
        /// Cubic hexagonal neighbours
        /// </summary>
        public static readonly Vector3I[] CubicNeighbours = new Vector3I[]
        {
            new(+1, +0, -1),
            new(+0, +1, -1),
            new(-1, +1, +0),

            new(-1, +0, +1),
            new(+0, -1, +1),
            new(+1, -1, +0),
        };

        /// <summary>
        /// Grid hexagonal neighbours
        /// </summary>
        public static readonly Vector2I[] AxialNeighbours = new Vector2I[]{
            new(+1, +0),
            new(+0, +1),
            new(-1, +1),

            new(-1, +0),
            new(+0, -1),
            new(+1, -1),
        };

        /// <summary>   
        /// Returns s value for cubic coordinate from axial coordinate
        /// </summary>
        public static int GetS(Vector2I axial) => -axial.X - axial.Y;

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

        /// <summary>
        /// Converts Cube coordinates to 2D grid coordinates (Vector2I)
        /// </summary>
        public static Vector2I CubicToAxial(Vector3I cubic)
        {
            // For axial coordinates (x = q, y = r)
            var x = cubic.X; // q
            var y = cubic.Y; // r

            // Ignore s, as it is derived from q and r
            return new(x, y);
        }

        /// <summary>
        /// Converts 2D grid coordinates to Cube coordinates (Vector3I)
        /// </summary>
        public static Vector3I AxialToCubic(Vector2I axial)
        {
            var q = axial.X;
            var r = axial.Y;
            var s = -q - r; // s = -q - r for cube coordinates

            return new(q, r, s);
        }

        /// <summary>
        /// Convert an index to cubic coordinates (q, r, s)
        /// </summary>
        public static Vector3I IndexToCubic(int index)
        {
            return AxialToCubic(IndexToAxial(index));
        }

        /// <summary>
        /// Convert cubic coordinates (q, r, s) to an index
        /// </summary>
        public static int CubicToIndex(Vector3I cubic)
        {
            return AxialToIndex(CubicToAxial(cubic));
        }

        /// <summary>
        /// Convert axial coordinates (q, r) to an index
        /// </summary>
        public static int AxialToIndex(Vector2I axial)
        {
            var ring = Mathf.Max(Mathf.Abs(axial.X), Mathf.Max(Mathf.Abs(axial.Y), Mathf.Abs(-axial.X - axial.Y)));
            if (ring == 0) return 0;

            // First cell of the current ring
            var indexStartOfRing = 1 + 3 * (ring - 1) * ring;

            // Position in the ring
            var positionInRing = GetPositionInRing(axial, ring);

            return indexStartOfRing + positionInRing;
        }

        /// <summary>
        /// Convert an index to axial coordinates (q, r)
        /// </summary>
        public static Vector2I IndexToAxial(int index)
        {
            if (index == 0) return default;

            var ring = GetRing(index);

            // Start with the first hex in the ring, offset from the center hex
            var positionInRing = index - GetStartIndex(ring);
            var currentHex = AxialNeighbours[4] * ring;
            var i = 0;

            var mod = positionInRing % ring;
            var cnt = (positionInRing - mod) / ring;

            for (; i < cnt;)
            {
                currentHex += AxialNeighbours[i++] * ring;
            }

            return currentHex + AxialNeighbours[i] * mod;
        }

        // Helper function to find the position of an axial coordinate within a ring
        // Really wished this was simpler to find somewhere so enjoy it. It scales.
        private static int GetPositionInRing(Vector2I axial, int ring)
        {
            // Start with the first hex in the ring, offset from the center hex
            var currentHex = AxialNeighbours[4] * ring;
            if (currentHex == axial) return 0;

            var _i = 0;

            var ang = Vector2.Zero.GetAngleD(axial).AbsMod(360);
            if (ang <= 0) ang = 360;

            for (byte i = 0; i < AxialNeighbours.Length; i++)
            {
                var min = Vector2.Zero.GetAngleD(AxialNeighbours.ModulatedElement(i - 2));

                var max = Vector2.Zero.GetAngleD(AxialNeighbours.ModulatedElement(i - 1)).AbsMod(360);
                if (max <= 0) max = 360;

                if (min >= ang || ang > max)
                {
                    currentHex += AxialNeighbours[i] * ring;
                    _i += ring;
                }

                else break;
            }

            return _i + Mathf.FloorToInt(GetDistance(currentHex, axial));
        }

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
        public static Vector3I[] GetRing(Vector3I cubic, int ringCount)
        {
            if ((ringCount = Mathf.Abs(ringCount)) < 1)
                return new[] { cubic };

            var result = new Vector3I[6 * ringCount]; // Each ring has 6 * ringCount hexes
            var i = 0;

            // Start with the first hex in the ring, offset from the center hex
            var currentHex = cubic + CubicNeighbours[4] * ringCount;

            // Traverse the hexes in the ring
            foreach (var direction in CubicNeighbours)
            {
                for (var step = 0; step < ringCount; step++)
                {
                    result[i++] = currentHex;
                    currentHex += direction; // Move in the current direction
                }
            }

            return result;
        }

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
        public static Vector2I[] GetRing(Vector2I axial, int ringCount)
        {
            if ((ringCount = Mathf.Abs(ringCount)) < 1)
                return new[] { axial };

            var result = new Vector2I[6 * ringCount]; // Each ring has 6 * ringCount hexes
            var i = 0;

            // Start with the first hex in the ring, offset from the center hex
            var currentHex = axial + AxialNeighbours[4] * ringCount;

            // Traverse the hexes in the ring
            foreach (var direction in AxialNeighbours)
            {
                for (var step = 0; step < ringCount; step++)
                {
                    result[i++] = currentHex;
                    currentHex += direction; // Move in the current direction
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public static int[] GetRange(int index, int ringCount)
        {
            var range = GetRange(IndexToAxial(index), ringCount);
            var iRange = new int[range.Length];

            for (var i = 0; i < range.Length; i++)
            {
                iRange[i] = AxialToIndex(range[i]);
            }

            return iRange;
        }

        /// <summary>
        /// Returns the neighboring hexagons in a specific ring, centered around the given hex
        /// </summary>
        public static int[] GetRing(int index, int ringCount)
        {
            var ring = GetRing(IndexToAxial(index), ringCount);
            var iRing = new int[ring.Length];

            for (var i = 0; i < ring.Length; i++)
            {
                iRing[i] = AxialToIndex(ring[i]);
            }

            return iRing;
        }

        /// <summary>   
        /// Returns distance between two points
        /// </summary>
        public static float GetDistance(Vector3I a, Vector3I b)
        {
            return (Mathf.Abs(a.X - b.X)
            + Mathf.Abs(a.Y - b.Y)
            + Mathf.Abs(a.Z - b.Z)) / 2;
        }

        /// <summary>
        /// Returns distance between two points
        /// </summary>
        public static float GetDistance(Vector2I a, Vector2I b)
        {
            return (Mathf.Abs(a.X - b.X)
            + Mathf.Abs(a.X + a.Y - b.X - b.Y)
            + Mathf.Abs(a.Y - b.Y)) / 2;
        }

        /// <summary>
        /// Returns distance between two points
        /// </summary>
        public static float GetDistance(int a, int b)
        {
            return GetDistance(IndexToAxial(a), IndexToAxial(b));
        }

        /// <summary>
        /// Returns ring value of an index
        /// </summary>
        public static int GetRing(Vector3I cubic)
        {
            return Mathf.CeilToInt(GetDistance(cubic, default));
        }

        /// <summary>
        /// Returns ring value of an index
        /// </summary>
        public static int GetRing(Vector2I axial)
        {
            return Mathf.CeilToInt(GetDistance(axial, default));
        }

        /// <summary>
        /// Returns ring value of an index
        /// </summary>
        public static int GetRing(int index)
        {
            return Mathf.CeilToInt((Mathf.Sqrt(12 * index + 9) - 3) / 6f);
        }

        /// <summary>
        /// Returns start index of a ring
        /// </summary>
        public static int GetStartIndex(int ring)
        {
            return GetCellCount(ring - 1);
        }

        /// <summary>
        /// Returns cell count of a ring and all the previous rings
        /// </summary>
        public static int GetCellCount(int ringCount)
        {
            return ringCount < 1 ? 1 : 3 * (int)Mathf.Pow(ringCount, 2) + 3 * ringCount + 1;
        }

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public static Vector3I[] GetNeighbors(Vector3I cubic = default)
        {
            // Hexagonal grid directions (Q,R,S)
            return new[]{
                GetNeighbor(cubic, 0),
                GetNeighbor(cubic, 1),
                GetNeighbor(cubic, 2),
                GetNeighbor(cubic, 3),
                GetNeighbor(cubic, 4),
                GetNeighbor(cubic, 5),
            };
        }

        /// <summary>
        /// Returns the neighboring hexagon in a given direction (0 to 5 for the six main directions)
        /// </summary>
        public static Vector3I GetNeighbor(Vector3I cubic, int direction)
        {
            // Hexagonal grid directions (Q,R,S)
            return cubic + CubicNeighbours[direction % 6];
        }

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public static Vector2I[] GetNeighbors(Vector2I axial = default)
        {
            // Hexagonal grid directions (Q,R,S)
            return new[]{
                GetNeighbor(axial, 0),
                GetNeighbor(axial, 1),
                GetNeighbor(axial, 2),
                GetNeighbor(axial, 3),
                GetNeighbor(axial, 4),
                GetNeighbor(axial, 5),
            };
        }

        /// <summary>
        /// Returns the neighboring hexagon in a given direction (0 to 5 for the six main directions)
        /// </summary>
        public static Vector2I GetNeighbor(Vector2I axial, int direction)
        {
            // Hexagonal grid directions (Q,R,S)
            return axial + AxialNeighbours[direction % 6];
        }

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
        public static bool TryGetPath(IPathfindingTarget target, Vector2I start, Vector2I end, out Vector2I[] path)
        {
            return (path = GetPath(target, start, end)).NotEmpty();
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public static Vector3I[] GetPath(IPathfindingTarget target, Vector3I start, Vector3I end)
        {
            Pathfinding.TryFindPath(target, CubicToAxial(start), CubicToAxial(end), out var _path);

            var path = new Vector3I[_path.Length];

            for (int i = 0; i < _path.Length; i++)
            {
                path[i] = AxialToCubic(_path[i]);
            }

            return path;
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public static Vector2I[] GetPath(IPathfindingTarget target, Vector2I start, Vector2I end)
        {
            Pathfinding.TryFindPath(target, start, end, out var path);

            return path;
        }

        #endregion
    }
}