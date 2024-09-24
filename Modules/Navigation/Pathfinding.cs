using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// You can use this by inherting from IPathfindingTarget and overriding the cost. Just return 0 if you want to make a point unwalkable.
    /// That's it. When that's done use the extension methods to call your pathfinding.  
    /// </summary>
    public static class Pathfinding
    {
        public static bool TryFindPath(this IPathfindingTarget costFinder, Vector2I start, Vector2I goal, out Vector2I[] path)
        {
            return (path = FindPath(costFinder, start, goal)).NotEmpty();
        }

        public static Vector2I[] FindPath(this IPathfindingTarget costFinder, Vector2I start, Vector2I goal)
        {
            var openSet = new SortedSet<(float fScore, Vector2I pos)>(Comparer<(float, Vector2I)>.Create((a, b) => a.Item1 == b.Item1 ? (a.Item2.X == b.Item2.X ? a.Item2.Y.CompareTo(b.Item2.Y) : a.Item2.X.CompareTo(b.Item2.X)) : a.Item1.CompareTo(b.Item1)));
            var cameFrom = new Dictionary<Vector2I, Vector2I>();
            var gScore = new Dictionary<Vector2I, float>();
            var fScore = new Dictionary<Vector2I, float>();

            var neighbours = costFinder.GetNeighbors();
            Vector2I neighbour;

            gScore[start] = 0;
            fScore[start] = HeuristicCostEstimate(start, goal);

            openSet.Add((fScore[start], start));

            while (openSet.Count > 0)
            {
                var current = openSet.Min.pos;
                openSet.Remove(openSet.Min);

                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }

                foreach (var _neighbour in neighbours)
                {
                    neighbour = _neighbour + current;

                    var walkableCost = costFinder.GetCost(neighbour);
                    if (walkableCost < 1) continue;

                    var tentativeGScore = gScore[current] + walkableCost;

                    if (!gScore.ContainsKey(neighbour) || tentativeGScore < gScore[neighbour])
                    {
                        cameFrom[neighbour] = current;
                        gScore[neighbour] = tentativeGScore;
                        fScore[neighbour] = gScore[neighbour] + HeuristicCostEstimate(neighbour, goal);

                        if (!openSet.Contains((fScore[neighbour], neighbour)))
                        {
                            openSet.Add((fScore[neighbour], neighbour));
                        }
                    }
                }
            }

            return System.Array.Empty<Vector2I>(); // No path found
        }

        private static float HeuristicCostEstimate(Vector2I a, Vector2I b)
        {
            // Manhattan distance
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
        }

        private static Vector2I[] ReconstructPath(Dictionary<Vector2I, Vector2I> cameFrom, Vector2I current)
        {
            var totalPath = new List<Vector2I> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Add(current);
            }

            totalPath.Reverse();
            return totalPath.ToArray();
        }

        private static IEnumerable<Vector2I> GetNeighbors(Vector2I node)
        {
            var directions = new Vector2I[]
            {
                new(0, 1),
                new(1, 0),
                new(0, -1),
                new(-1, 0),

                new(1, 1),
                new(1, -1),
                new(-1, 1),
                new(-1, -1),
            };

            foreach (var direction in directions)
            {
                yield return new Vector2I(node.X + direction.X, node.Y + direction.Y);
            }
        }
    }

    public interface IPathfindingTarget
    {
        /// <summary>
        /// Return 0 if you want to make a point unwalkable.
        /// </summary>
        public int GetCost(Vector2I point);

        public IEnumerable<Vector2I> GetNeighbors();
    }
}