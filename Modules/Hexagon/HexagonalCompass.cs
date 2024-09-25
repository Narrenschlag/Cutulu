namespace Cutulu
{
    using Cutulu;
    using Godot;

    /// <summary>
    /// Hexagonal Compass used to calculate points and positions on a hexagonal grid
    /// </summary>
    public partial struct HexagonalCompass
    {
        public static readonly Vector3I[] CubeNeighbours = new Vector3I[]
        {
            new(+0, -1, +1), new(+1, -1, +0), new(+1, +0, -1),
            new(+0, +1, -1), new(-1, +1, +0), new(-1, +0, +1)
        };

        public static readonly Vector2I[] GridNeighbours = new Vector2I[]{
            new(+0, -1), new(+1, -1), new(+1, +0),
            new(+0, +1), new(-1, +1), new(-1, +0),
        };

        // Hexagonal properties
        public readonly float CellSize;
        public readonly Vector3 Forward, Right, Up;

        /// <summary>
        /// Constructor
        /// </summary>
        public HexagonalCompass(Vector3 forward, float cellSize)
        {
            CellSize = cellSize;

            Up = forward.toUp();
            Right = forward.toRight(Up);
            Forward = forward.Normalized();
        }

        /// <summary>
        /// Converts a world position (Vector3) to hexagonal coordinates
        /// </summary>
        public Vector3I WorldToCube(Vector3 position, Vector3 offset = default)
        {
            var position2D = new Vector2(Right.Dot(position -= offset), Forward.Dot(position));

            var q = (2f / 3f * position2D.X) / CellSize;
            var r = (-1f / 3f * position2D.X + Mathf.Sqrt(3) / 3f * position2D.Y) / CellSize;
            var s = -q - r;

            return CubeRound(new Vector3(q, s, r));
        }

        /// <summary>
        /// Converts hexagonal coordinates to world position
        /// </summary>
        public Vector3 CubeToWorld(Vector3I hex, Vector3 offset = default)
        {
            var x = CellSize * (3f / 2f * hex.X);
            var y = CellSize * (Mathf.Sqrt(3) * (hex.Z + hex.X / 2f));

            return Right * x + Forward * y + offset; // Y is set to 0 (ground level)
        }

        /// <summary>
        /// Converts 2D grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorld(Vector2I grid, Vector3 offset = default)
        {
            return CubeToWorld(GridToCube(grid), offset);
        }

        /// <summary>
        /// Converts world position to hexagonal coordinates
        /// </summary>
        public Vector2I WorldToGrid(Vector3 position, Vector3 offset = default)
        {
            return CubeToGrid(WorldToCube(position, offset));
        }

        /// <summary>
        /// Converts Cube coordinates to 2D grid coordinates (Vector2I)
        /// </summary>
        public Vector2I CubeToGrid(Vector3I cube)
        {
            // For axial coordinates (x = q, y = r)
            var x = cube.X; // q
            var y = cube.Y; // r

            // Ignore s, as it is derived from q and r
            return new(x, y);
        }

        /// <summary>
        /// Converts 2D grid coordinates to Cube coordinates (Vector3I)
        /// </summary>
        public Vector3I GridToCube(Vector2I grid)
        {
            var q = grid.X;
            var r = grid.Y;
            var s = -q - r; // s = -q - r for cube coordinates

            return new(q, r, s);
        }

        /// <summary>
        /// Rounds cube coordinates to nearest hexagonal coordinates
        /// </summary>
        private Vector3I CubeRound(Vector3 cube)
        {
            var rx = Mathf.RoundToInt(cube.X);
            var ry = Mathf.RoundToInt(cube.Y);
            var rz = Mathf.RoundToInt(cube.Z);

            var x_diff = Mathf.Abs(rx - cube.X);
            var y_diff = Mathf.Abs(ry - cube.Y);
            var z_diff = Mathf.Abs(rz - cube.Z);

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
        /// Returns the six corner points of the hexagon centered at hex coordinates
        /// </summary>
        public Vector3[] GetVertices(Vector3I hex)
        {
            var corners = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                var corner = GetVertice(hex, i);
                corners[i] = corner;
            }

            return corners;
        }

        /// <summary>
        /// Returns a single corner of a hexagon (i is 0 to 5)
        /// </summary>
        private Vector3 GetVertice(Vector3I hex, int cornerIndex)
        {
            var dir = Forward.Rotated(Up, (60f * cornerIndex + 30f).toRadians()); // 60° between corners

            return CubeToWorld(hex) + dir * CellSize;
        }

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public Vector3I[] GetRange(Vector3I hex, int ringCount)
        {
            if ((ringCount = Mathf.Abs(ringCount)) < 1)
                return new[] { hex };

            var result = new Vector3I[1 + 3 * ringCount * (ringCount + 1)];
            var N = ringCount;

            for (int i = 0, q = -N; -N <= q && q <= +N; q++)
            {
                for (var r = Mathf.Max(-N, -q - N); Mathf.Max(-N, -q - N) <= r && r <= Mathf.Min(+N, -q + N); r++)
                {
                    var s = -q - r;

                    result[i++] = hex + new Vector3I(q, r, s);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the neighboring hexagons in a specific ring, centered around the given hex
        /// </summary>
        public Vector3I[] GetRing(Vector3I hex, int ringCount)
        {
            if ((ringCount = Mathf.Abs(ringCount)) < 1)
                return new[] { hex };

            var result = new Vector3I[6 * ringCount]; // Each ring has 6 * ringCount hexes
            var i = 0;

            // Start with the first hex in the ring, offset from the center hex
            var currentHex = hex + new Vector3I(-ringCount, 0, ringCount);

            // Traverse the hexes in the ring
            foreach (var direction in CubeNeighbours)
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
        public Vector3I[] GetNeighbors(Vector3I hex = default)
        {
            // Hexagonal grid directions (Q,R,S)
            return new[]{
                GetNeighbor(hex, 0),
                GetNeighbor(hex, 1),
                GetNeighbor(hex, 2),
                GetNeighbor(hex, 3),
                GetNeighbor(hex, 4),
                GetNeighbor(hex, 5),
            };
        }

        /// <summary>
        /// Returns the neighboring hexagon in a given direction (0 to 5 for the six main directions)
        /// </summary>
        public Vector3I GetNeighbor(Vector3I hex, int direction)
        {
            // Hexagonal grid directions (Q,R,S)
            return hex + CubeNeighbours[direction % 6];
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public bool TryGetPath(IPathfindingTarget target, Vector3I start, Vector3I end, out Vector3I[] path)
        {
            return (path = GetPath(target, start, end)).NotEmpty();
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public bool TryGetPath(IPathfindingTarget target, Vector2I start, Vector2I end, out Vector2I[] path)
        {
            return (path = GetPath(target, start, end)).NotEmpty();
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public Vector3I[] GetPath(IPathfindingTarget target, Vector3I start, Vector3I end)
        {
            Pathfinding.TryFindPath(target, CubeToGrid(start), CubeToGrid(end), out var _path);

            var path = new Vector3I[_path.Length];

            for (int i = 0; i < _path.Length; i++)
            {
                path[i] = GridToCube(_path[i]);
            }

            return path;
        }

        /// <summary>
        /// Returns path based on path cost
        /// </summary>
        public Vector2I[] GetPath(IPathfindingTarget target, Vector2I start, Vector2I end)
        {
            Pathfinding.TryFindPath(target, start, end, out var path);

            return path;
        }
    }
}