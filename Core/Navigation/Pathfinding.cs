using System.Collections.Generic;
using Godot;

namespace Cutulu.Core
{
    /// <summary>
    /// You can use this by inherting from IPathfindingTarget and overriding the cost. Just return 0 if you want to make a point unwalkable.
    /// That's it. When that's done use the extension methods to call your pathfinding.  
    /// </summary>
    public static class Pathfinding
    {
        /// <summary>
        /// Uses A* algorithm to try find a path from start to end.
        /// Uses the costFinder to get the cost of each neighbour.
        /// </summary>
        public static bool TryFindPath(this IPathfindingTarget costFinder, Vector2I start, Vector2I goal, out Vector2I[] path)
        {
            return (path = FindPath(costFinder, start, goal)).NotEmpty();
        }

        /// <summary>
        /// Uses A* algorithm to try find a path from start to end.
        /// Uses the costFinder to get the cost of each neighbour.
        /// </summary>
        public static Vector2I[] FindPath(this IPathfindingTarget costFinder, Vector2I start, Vector2I end)
        {
            var openSet = new SortedSet<(float fScore, Vector2I pos)>(Comparer<(float, Vector2I)>.Create((a, b) => a.Item1 == b.Item1 ? (a.Item2.X == b.Item2.X ? a.Item2.Y.CompareTo(b.Item2.Y) : a.Item2.X.CompareTo(b.Item2.X)) : a.Item1.CompareTo(b.Item1)));
            var cameFrom = new Dictionary<Vector2I, Vector2I>();
            var gScore = new Dictionary<Vector2I, float>();
            var fScore = new Dictionary<Vector2I, float>();

            var neighbours = costFinder.GetNeighbors();
            Vector2I neighbour;

            gScore[start] = 0;
            fScore[start] = HeuristicCostEstimate(start, end);

            openSet.Add((fScore[start], start));

            while (openSet.Count > 0)
            {
                var current = openSet.Min.pos;
                openSet.Remove(openSet.Min);

                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }

                foreach (var _neighbour in neighbours)
                {
                    neighbour = _neighbour + current;

                    var walkableCost = costFinder.GetCost(ref current, ref neighbour);
                    if (walkableCost < 1) continue;

                    var tentativeGScore = gScore[current] + walkableCost;

                    if (!gScore.ContainsKey(neighbour) || tentativeGScore < gScore[neighbour])
                    {
                        cameFrom[neighbour] = current;
                        gScore[neighbour] = tentativeGScore;
                        fScore[neighbour] = gScore[neighbour] + HeuristicCostEstimate(neighbour, end);

                        if (!openSet.Contains((fScore[neighbour], neighbour)))
                        {
                            openSet.Add((fScore[neighbour], neighbour));
                        }
                    }
                }
            }

            return System.Array.Empty<Vector2I>(); // No path found
        }

        /// <summary>
        /// Returns path cost based on manhattan distance
        /// Uses A* algorithm
        /// </summary>
        private static float HeuristicCostEstimate(Vector2I a, Vector2I b)
        {
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
        }

        /// <summary>
        /// Reconstructs path from cameFrom dictionary
        /// </summary>
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
    }

    /// <summary>
    /// Interface for pathfinding targets.
    /// You can use this by inherting from IPathfindingTarget and overriding the cost. Just return 0 if you want to make a point unwalkable.
    /// </summary>
    public interface IPathfindingTarget
    {
        /// <summary>
        /// Return 0 if you want to make a point unwalkable.
        /// </summary>
        public int GetCost(ref Vector2I previous, ref Vector2I next);

        /// <summary>
        /// Returns possible neighbours
        /// </summary>
        public IEnumerable<Vector2I> GetNeighbors();
    }
}