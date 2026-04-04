namespace Warlords.Test;

using System.Collections.Generic;
using Cutulu.Network;
using Cutulu.Core;
using Cutulu.Mesh;
using Godot;
using Host;

public partial class NavTest : Node3D
{
    [Export] private NavMap32 Map;
    [Export] private Node ObstacleParent;
    [Export] private Node GizmoParent;
    [Export] private Camera3D Camera;
    [Export] private PackedScene TestBuilding;

    private MeshInstance3D CellGizmoA, CellGizmoB, ChunkPathGizmo, CellPathGizmo;

    private readonly Dictionary<Vector2I, Node3D> ChunkGizmos = [];

    private NavMarshal32 GetMarshal() => new(Map);

    public override void _Ready()
    {
        NavMarshal32 marshal = GetMarshal();

        var mapSizeRadius = Map.ChunkSize * 10;
        marshal.CreateMap(Vector3.One * -mapSizeRadius, Vector3.One * mapSizeRadius);

        SwapbackArray<Obb> obstacles = [];

        var children = ObstacleParent.GetNodesInChildren<Node>(false, 1);
        foreach (var child in children)
        {
            var obb = child.GetNodeObb();

            if (obb.Size.IsZeroApprox()) continue;

            obstacles.TryAdd(obb);
        }

        Debug.Log($"Obstacle Count: {obstacles.Count}");

        if (NavObstacleNode.TryGetObstacleCollection(ObstacleParent, out var obstacle))
            marshal.AddObstacle(obstacle);

        GizmoParent.Clear();

        /*
        var navChunks = Map.GetRawChunks();

        Vector3 center;

        foreach (var chunk in navChunks)
        {
            Color walkable = chunk.State == NavChunk.STATE.FreelyWalkable ? Colors.White : Colors.SeaGreen;
            Color blocked = Colors.IndianRed;
            walkable.A = blocked.A = 0.1f;

            center = chunk.GetCenterPosition();

            center += Vector3.Up * Map.CellSize;

            GizmoParent.DrawRay(
                chunk.ConnectedTo(NavChunk.NEIGHBOUR_TYPE.NORTH) ? walkable : blocked,
                center,
                new Vector3(0, 0, Map.ChunkSize * 0.5f)
            );

            GizmoParent.DrawRay(
                chunk.ConnectedTo(NavChunk.NEIGHBOUR_TYPE.EAST) ? walkable : blocked,
                center,
                new Vector3(Map.ChunkSize * 0.5f, 0, 0)
            );

            GizmoParent.DrawRay(
                chunk.ConnectedTo(NavChunk.NEIGHBOUR_TYPE.SOUTH) ? walkable : blocked,
                center,
                new Vector3(0, 0, -Map.ChunkSize * 0.5f)
            );

            GizmoParent.DrawRay(
                chunk.ConnectedTo(NavChunk.NEIGHBOUR_TYPE.WEST) ? walkable : blocked,
                center,
                new Vector3(-Map.ChunkSize * 0.5f, 0, 0)
            );
        }
        */
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("switch_mode"))
        {
            var building = TestBuilding.UnpackHost<Node3D>(ObstacleParent);
            building.GlobalPosition = Camera.GetHit(0);
            building.GlobalRotationDegrees = Vector3.Up * Random.Value * 360.0f;

            if (NavObstacleNode.TryGetObstacleCollection(building, out var obstacle))
            {
                var chunks = GetMarshal().AddObstacle(obstacle);

                foreach (var chunk in chunks)
                    DebugDrawGizmos(chunk);
            }
            else Debug.LogError($"No obstacles found");

            building.Visible = false;
        }

        else if (
            Input.IsActionJustPressed("attack") &&
            Map.TryGetChunk(Camera.GetHit(0), out var chunk, out var localCell)
        )
        {
            if (Input.IsActionPressed("shift") == false || CellGizmoA.IsNull())
            {
                CellGizmoB.Destroy();
                if (CellGizmoA.IsNull())
                    CellGizmoA = GizmoParent.DrawPoint(Vector3.Zero, Colors.White, Map.CellSize * 0.5f);

                CellGizmoA.GlobalPosition = chunk.GetCellPosition(localCell.X, localCell.Y);
            }

            else
            {
                if (CellGizmoB.IsNull()) CellGizmoB = GizmoParent.DrawPoint(Vector3.Zero, Colors.Magenta, Map.CellSize * 0.5f);
                else CellGizmoA.GlobalPosition = CellGizmoB.GlobalPosition;

                CellGizmoB.GlobalPosition = chunk.GetCellPosition(localCell.X, localCell.Y);

                var a = Map.GetChunkCoord(CellGizmoA.GlobalPosition, out var localCellA);
                var b = Map.GetChunkCoord(CellGizmoB.GlobalPosition, out var localCellB);

                DrawChunkPath(a, b);
                DrawCellPath(CellGizmoA.GlobalPosition, CellGizmoB.GlobalPosition);
            }
        }
    }

    private void DebugDrawGizmos(Vector2I chunkPoint)
    {
        if (ChunkGizmos.TryGetValue(chunkPoint, out var parent))
            parent.Destroy();

        GizmoParent.AddChild(parent = new Node3D());
        ChunkGizmos[chunkPoint] = parent;

        if (Map.TryGetChunk(chunkPoint, out var chunk) == false) return;

        //Color walkable = chunk.State == NavChunk.STATE.FreelyWalkable ? Colors.White : Colors.SeaGreen;
        Color blocked = Colors.IndianRed;

        int x, z, count = NavMap32.CELLS_PER_CHUNK;

        for (x = 0; x < count; x++)
            for (z = 0; z < count; z++)
            {
                if (chunk[x, z]) continue;

                parent.DrawPoint(
                    chunk.GetCellPosition(x, z),
                    blocked,
                    Map.CellSize * 0.1f
                );
            }

        if (chunk.Regions.Size() > 1)
        {
            Color[] colors = [Colors.SeaGreen, Colors.Yellow, Colors.Cyan];
            Color color;
            int idx = -1;

            Debug.Log($"Draw regions: {chunk.Regions.Size()}");
            foreach (var region in chunk.Regions)
            {
                Debug.Log($"- {region.GetWalkableCount()}");
                color = colors.ModulatedElement(++idx);

                for (x = 0; x < count; x++)
                    for (z = 0; z < count; z++)
                    {
                        if (region[x, z] == false) continue;

                        parent.DrawPoint(
                            chunk.GetCellPosition(x, z),
                            color,
                            Map.CellSize * 0.1f
                        );
                    }

                /*Vector3 center = region.GetCenterPosition(chunk);
                parent.DrawPoint(center, color, Map.CellSize * 0.25f);

                if (region.HasAnyNeighbour())
                {
                    var span = region.GetNeighbours();

                    for (var n = 0; n < span.Length; n++)
                    {
                        parent.DrawRay(color, center, ((Vector2)NavRegion32.GetDirection(span[n].Type)).toXZ() * 100.0f);
                        Debug.Log($"NeighbourOf[{idx}]: {span[n].Type}[{span[n].Region}]");
                    }
                }

                else Debug.Log($"No neighbours: {region.GetWalkableCount()}");*/
            }
        }
    }

    private void DrawChunkPath(Vector2I from, Vector2I to)
    {
        if (!Map.TryFindChunkPath(from, to, out var path))
        {
            Debug.LogError($"No chunk-path found from {from} to {to}");
            return;
        }

        Vector3[] line = new Vector3[path.Length];

        for (int i = 0; i < path.Length; i++)
        {
            if (!Map.TryGetChunk(path[i], out var chunk))
                return;

            line[i] = chunk.GetCenterPosition() + Vector3.Up * 0.1f;
        }

        ChunkPathGizmo.Destroy();
        Debug.Log($"Chunk Path: {path.Length} cells");
        ChunkPathGizmo = GizmoParent.DrawLine(Colors.Magenta, line);
    }

    private void DrawCellPath(Vector3 from, Vector3 to)
    {
        if (!GetMarshal().TryFindPath(from, to, out var path))
            return;

        Vector3[] line = new Vector3[path.Length];

        for (int i = 0; i < path.Length; i++)
        {
            line[i] = path[i] + Vector3.Up * 0.1f;
        }

        CellPathGizmo.Destroy();
        CellPathGizmo = GizmoParent.DrawLine(Colors.Yellow, line);
    }
}