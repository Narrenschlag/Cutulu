namespace Cutulu.Core;

using Godot;

/// <summary>
/// A pathfinding map with 32x32 chunks with 32bit chunk side hashes. 
/// Heavily inspired by Tynan Sylvester's RimWorld.
/// Written, with blood, sweat and tears, by Maximilian Schecklmann April 2nd to 4th 2026. 
/// </summary>
[GlobalClass]
public partial class NavObstacleNode : Node3D
{
    public SwapbackArray<Obb> GetObstacles()
    {
        var nodes = this.GetNodesInChildren<Node3D>(false);
        if (nodes.IsEmpty())
        {
            //Debug.LogError($"- No obstacles found");
            return [];
        }

        SwapbackArray<Obb> obstacles = new(nodes.Size());

        foreach (var node in nodes)
        {
            var obb = node.GetNodeObb();

            if (obb.Size.IsZeroApprox())
                continue;

            //Debug.LogError($"- Obstacle[{node.Name}] Size: {obb.Size} : {obb.Center} Center");
            obstacles.Add(obb);
        }

        //Debug.LogError($"Found Obstacle Count: {obstacles.Count}");

        return obstacles;
    }

    public static bool TryGetObstacleCollection(Node node, out NavObstacle32 collection, Node customValidator = null, byte depth = 0)
    {
        if (node.NotNull())
        {
            var nodes = node.GetNodesInChildren<NavObstacleNode>(true, depth);

            if (nodes.NotEmpty())
            {
                var swapbackArray = new SwapbackArray<Obb>(nodes.Count);

                foreach (var child in nodes)
                {
                    swapbackArray.AddRange(child.GetObstacles().AsSpan());
                }

                if (swapbackArray.Count > 0)
                {
                    collection = new(customValidator ?? node, null, swapbackArray);
                    return true;
                }
            }
        }

        collection = null;
        return false;
    }
}