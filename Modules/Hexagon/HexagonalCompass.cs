using Godot;

namespace Cutulu
{
    /// <summary>
    /// Hexagonal Compass used to calculate points and positions on a hexagonal grid
    /// </summary>
    public partial struct HexagonalCompass
    {
        // Hexagonal properties
        private readonly float CellSize;
        private readonly Vector3 Forward, Right, Up;
        private readonly Vector3[] Values = new Vector3[12];

        // Constructor
        public HexagonalCompass(Vector3 forward, float cellSize)
        {
            CellSize = cellSize;

            Forward = forward.Normalized();
            Up = forward.toUp().Normalized();
            Right = forward.toRight(Up).Normalized();

            InitializeValues();
        }

        // Initializes the 12 directional values
        private void InitializeValues()
        {
            for (int i = 0; i < 12; i++)
            {
                var angle = (30f * i).toRadians(); // Rotating by 30° increments

                var x = Mathf.Cos(angle);
                var y = 0; // Assuming flat-top hexes; for pointy-top, adjust this logic
                var z = Mathf.Sin(angle);

                Values[i] = new Vector3(x, y, z).Normalized();
            }
        }

        // Converts a world position (Vector3) to hexagonal coordinates
        public Vector3I WorldToHex(Vector3 position)
        {
            var q = (2f / 3f * position.X) / CellSize;
            var r = (-1f / 3f * position.X + Mathf.Sqrt(3) / 3f * position.Z) / CellSize;
            var s = -q - r;

            return CubeRound(new Vector3(q, -q - r, r));
        }

        // Converts hexagonal coordinates to world position
        public Vector3 HexToWorld(Vector3I hex, Vector3 offset = default)
        {
            var x = CellSize * (3f / 2f * hex.X);
            var z = CellSize * (Mathf.Sqrt(3) * (hex.Z + hex.X / 2f));

            return new Vector3(x, 0, z) + offset; // Y is set to 0 (ground level)
        }

        // Rounds cube coordinates to nearest hexagonal coordinates
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

        // Returns the six corner points of the hexagon centered at hex coordinates
        public Vector3[] GetHexCorners(Vector3I hex)
        {
            var corners = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                var corner = HexCorner(hex, i);
                corners[i] = corner;
            }

            return corners;
        }

        // Returns a single corner of a hexagon (i is 0 to 5)
        private Vector3 HexCorner(Vector3I hex, int cornerIndex)
        {
            var angle = (60f * cornerIndex).toRadians(); // 60° between corners
            var worldPos = HexToWorld(hex);

            var x = worldPos.X + CellSize * Mathf.Cos(angle);
            var z = worldPos.Z + CellSize * Mathf.Sin(angle);

            return new Vector3(x, 0, z); // Y is set to 0
        }

        // Returns the neighboring hex in a given direction (0 to 5 for the six main directions)
        public Vector3I GetNeighbor(Vector3I hex, int direction)
        {
            // Hexagonal grid directions (Q,R,S)
            var directions = new Vector3I[]
            {
                new(1, -1, 0), new(1, 0, -1), new(0, 1, -1),
                new(-1, 1, 0), new(-1, 0, 1), new(0, -1, 1)
            };

            return hex + directions[direction % 6];
        }
    }
}