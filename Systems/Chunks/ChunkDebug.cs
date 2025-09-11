namespace Cutulu.Systems.Chunks;

using System.Collections.Generic;
using Cutulu.Core;
using Cutulu.Mesh;
using Godot;

public partial class ChunkDebug<CHUNK> : Node3D where CHUNK : Chunk
{
    [Export] private ChunkManager<CHUNK> Manager { get; set; }

    [Export] private Camera3D Camera { get; set; }
    [Export] private Color LineColor { get; set; } = Colors.Magenta;
    [Export] private Color PointColor { get; set; } = Colors.DarkOrange;
    [Export] private Color FrustumColor { get; set; } = Colors.SeaGreen;

    private MeshInstance3D Point { get; set; }

    private ChunkFrustum<CHUNK> ChunkFrustum { get; set; }

    public override void _Ready()
    {
        if (Manager.IsNull()) return;

        ChunkFrustum = new(Manager);

        foreach (var chunk in Manager.Chunks.Values)
        {
            var start = Manager.Start + new Vector2(chunk.Point.X * Manager.ChunkSize.X,
                chunk.Point.Z * Manager.ChunkSize.Y);
            var end = start + Manager.ChunkSize;
            this.DrawLine(LineColor,
                start.toXZ(),
                new Vector3(start.X, 0, end.Y),
                end.toXZ(),
                new Vector3(end.X, 0, start.Y),
                start.toXZ());
        }

        ChunkFrustum.Recalculated.Bind(this, DrawFrustum);
        ChunkFrustum.Tolerance = 15.0f;
        ChunkFrustum.Range = 450.0f;
        ChunkFrustum.FOV = Camera.Fov;

        ChunkFrustum.Angle = Camera.GlobalRotationDegrees.Y;
        ChunkFrustum.GlobalPosition = Camera.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        if (Point.IsNull())
        {
            Point = this.DrawPoint(default, PointColor, 4.0f);
        }

        var chunkPoint = Manager.GetChunkPoint(Camera.GlobalPosition, out var local);
        var chunkWorldStart = Manager.Start.toXZ() +
            new Vector3(chunkPoint.X * Manager.ChunkSize.X, 0,
                chunkPoint.Z * Manager.ChunkSize.Y);
        Point.GlobalPosition = chunkWorldStart + local.toXZ();

        ChunkFrustum.Angle = Camera.GlobalRotationDegrees.Y;
        ChunkFrustum.GlobalPosition = Camera.GlobalPosition;
    }

    private readonly List<MeshInstance3D> Frustum = [];

    private void DrawFrustum()
    {
        Frustum.ClearAndDestroy();
        var chunks = ChunkFrustum.Triangle;

        if (chunks.IsEmpty()) return;

        foreach (var chunk in chunks)
        {
            var mesh = this.DrawLine(PointColor, point(0.5f, 0.5f), point(0, 0), point(1, 0), point(1, 1), point(0, 1), point(0, 0));
            mesh.GlobalPosition = Manager.Start.toXZ() + new Vector3(chunk.X * Manager.ChunkSize.X, 0, chunk.Z * Manager.ChunkSize.Y);
            mesh.GlobalPosition += Manager.ChunkSize.toXZ() * 0.5f;
            Frustum.Add(mesh);
            Vector3 point(float x, float z) => new Vector3(x, 0, z) * Manager.ChunkSizeInM * 1.01f;
        }
    }
}