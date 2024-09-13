using Godot;

namespace Cutulu
{
    /// <summary>
    /// Hexagonal Compass used to calculate points and positions on a hexagonal grid
    /// </summary>
    public partial struct HexagonalCompass
    {
        public static readonly Vector3I[] Directions = new Vector3I[]
        {
            new(1, -1, 0), new(1, 0, -1), new(0, 1, -1),
            new(-1, 1, 0), new(-1, 0, 1), new(0, -1, 1)
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
        public Vector3I WorldToHex(Vector3 position, Vector3 offset = default)
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
        public Vector3 HexToWorld(Vector3I hex, Vector3 offset = default)
        {
            var x = CellSize * (3f / 2f * hex.X);
            var y = CellSize * (Mathf.Sqrt(3) * (hex.Z + hex.X / 2f));

            return Right * x + Forward * y + offset; // Y is set to 0 (ground level)
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

            return new Vector3I(rx, ry, rz);
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

            return HexToWorld(hex) + dir * CellSize;
        }

        /// <summary>
        /// Returns the neighboring hexagons
        /// </summary>
        public Vector3I[] GetRange(Vector3I hex, int ringCount)
        {
            if (ringCount < 1)
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
            if (ringCount < 1)
                return new[] { hex };

            var result = new Vector3I[6 * ringCount]; // Each ring has 6 * ringCount hexes
            var i = 0;

            // Start with the first hex in the ring, offset from the center hex
            var currentHex = hex + new Vector3I(-ringCount, 0, ringCount);

            // Traverse the hexes in the ring
            foreach (var direction in Directions)
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
        public Vector3I[] GetNeighbors(Vector3I hex)
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
            return hex + Directions[direction % 6];
        }
    }
}