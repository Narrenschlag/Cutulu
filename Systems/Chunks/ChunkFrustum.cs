#if GODOT4_0_OR_GREATER
namespace Cutulu.Systems.Chunks;

using System.Collections.Generic;
using Cutulu.Core;
using Godot;

public class ChunkFrustum<MANAGER, CHUNK>(ChunkManager<MANAGER, CHUNK> manager) where MANAGER : ChunkManager<MANAGER, CHUNK> where CHUNK : IChunk<MANAGER, CHUNK>
{
    public readonly ChunkManager<MANAGER, CHUNK> Manager = manager;
    public bool EnableAutoRecalc { get; set; } = true;

    public readonly Notification Recalculated = new();

    private readonly Dictionary<ChunkPoint, float> Precomputed = [];
    public readonly List<ChunkPoint> Triangle = [];
    public readonly List<ChunkPoint> Circle = [];

    public ChunkPoint ChunkPoint { get; private set; }
    public Vector3 LocalPosition { get; private set; }
    public Vector2 Forward { get; private set; }
    public Vector2 Right { get; private set; }

    private bool HasBeenCalced { get; set; } = false;

    private Vector3 globalPosition;
    public Vector3 GlobalPosition
    {
        get => globalPosition;
        set
        {
            var chunkPoint = Manager.GetChunkPoint(value, out var local);
            LocalPosition = local.toXZ(value.Y);
            globalPosition = value;

            if (HasBeenCalced && ChunkPoint.Equals(chunkPoint)) return;
            ChunkPoint = chunkPoint;
            HasBeenCalced = true;

            if (EnableAutoRecalc)
                Recalc(true);
        }
    }

    private float angle;
    public float Angle
    {
        get => angle;
        set
        {
            var oldValue = angle;
            angle = Mathf.RoundToInt(value / 5.0f) * 5.0f;

            if (HasBeenCalced && value == oldValue) return;

            Forward = (-angle - 90.0f).GetDirectionD();
            Right = Forward.RotatedD(90);

            if (EnableAutoRecalc)
                Recalc(false);
        }
    }

    private float range;
    public float Range
    {
        get => range;
        set
        {
            var oldValue = range;
            range = value;

            if (HasBeenCalced == false || value == oldValue) return;

            if (EnableAutoRecalc)
                Recalc(true);
        }
    }

    private float fov;
    public float FOV
    {
        get => fov;
        set
        {
            var oldValue = fov;
            fov = value;

            if (HasBeenCalced == false || value == oldValue) return;

            if (EnableAutoRecalc)
                Recalc(false);
        }
    }

    private float tolerance;
    public float Tolerance
    {
        get => tolerance;
        set
        {
            var oldValue = tolerance;
            tolerance = value;

            if (HasBeenCalced == false || value == oldValue) return;

            if (EnableAutoRecalc)
                Recalc(false);
        }
    }

    public void Recalc(float fov, float range)
    {
        this.range = range;
        this.fov = fov;

        Recalc(true);
    }

    public void RecalcAt(Vector3 position, float angle)
    {
        var before = EnableAutoRecalc;
        EnableAutoRecalc = true;

        GlobalPosition = position;
        Angle = angle;

        EnableAutoRecalc = before;
    }

    private void Recalc(bool calcCircle)
    {
        if (calcCircle) CalcCircle();
        CalcTriangle();

        Recalculated.Invoke();
    }

    private void CalcCircle()
    {
        var radius = (short)Mathf.Abs(Mathf.CeilToInt(Range / Manager.ChunkSizeInM));
        Precomputed.Clear();
        Circle.Clear();

        for (var x = (short)(-radius + 1); x < radius; x++)
        {
            for (var y = (short)(-radius + 1); y < radius; y++)
            {
                var chunk = new ChunkPoint(x, y);

                if (chunk.Length() > radius || Manager.HasChunk(chunk += ChunkPoint) == false) continue;

                Precomputed[chunk] = new Vector2(x, y).GetAngleD(Vector2.Up);
                Circle.Add(chunk);
            }
        }
    }

    private void CalcTriangle()
    {
        var apex = ChunkPoint - Forward.Normalized() * 1.5f;
        var halfFov = FOV * 0.6f;
        var angle = -Angle;
        Triangle.Clear();

        foreach (var chunkPoint in Circle)
            if (IsChunkInFrustum(chunkPoint))
                Triangle.Add(chunkPoint);

        bool IsChunkInFrustum(ChunkPoint point)
        {
            var net = point - apex;

            var dotf = net.Dot(Forward);
            if (dotf < 0) return false;

            var dotr = net.Dot(Right);
            if (Mathf.Abs(dotr) <= 1.8f) return true;

            var diff = (angle - Precomputed[point]) % 360.0f;
            if (diff < -180.0f) diff += 360.0f;
            if (diff > 180.0f) diff -= 360.0f;
            diff = Mathf.Abs(diff);

            return diff >= -halfFov && diff <= halfFov;
        }
    }
}
#endif