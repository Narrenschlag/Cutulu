#if GODOT4_0_OR_GREATER
namespace Cutulu.UI;

using System.Collections.Generic;
using Godot;
using Core;

using TIME = Godot.Time;

[GlobalClass]
public abstract partial class DigeticInterface : Node3D
{
    [Export] private StandardMaterial3D DefaultSubviewportMaterial;

    private readonly Dictionary<SubViewport, (Vector2 LastPos, float LastTime)> cacheMap = [];
    private readonly Dictionary<SubViewport, StandardMaterial3D> cachedMaterials = [];
    private readonly HashSet<Area3D> registeredAreas = [];

    #region Administration

    protected virtual void RegisterArea(Area3D area)
    {
        if (registeredAreas.Add(area) == false) return;

        area.InputEvent += (camera, inputEvent, globalPos, normal, shapeIdx) =>
            OnAreaInput(area, inputEvent, globalPos);
    }

    #endregion

    #region Materials

    public StandardMaterial3D ApplyMaterial(MeshInstance3D mesh, int surfaceIdx, SubViewport viewport)
    {
        if (mesh.IsNull()) return null;

        StandardMaterial3D material = GetMaterial(viewport);

        if (material.IsNull()) return null;

        mesh.SetSurfaceOverrideMaterial(surfaceIdx, material);

        return material;
    }

    public StandardMaterial3D GetMaterial(SubViewport viewport)
    {
        if (viewport.IsNull()) return null;

        if (
            cachedMaterials.TryGetValue(viewport, out StandardMaterial3D material) &&
            material.NotNull()
        ) return material;

        if (DefaultSubviewportMaterial.IsNull()) DefaultSubviewportMaterial = new();

        material = DefaultSubviewportMaterial.Duplicate() as StandardMaterial3D;
        material.ResourceLocalToScene = true;

        material.AlbedoTexture = viewport.GetTexture();
        cachedMaterials[viewport] = material;
        return material;
    }

    #endregion

    #region Input Handling

    protected virtual void OnAreaInput(Area3D area, InputEvent inputEvent, Vector3 globalPos)
    {
        if (
            TryGetMappingSurface(globalPos, area, out CollisionShape3D shape, out SubViewport viewport) == false ||
            TryMapTo01(globalPos, shape, out Vector2 pos2D) == false ||
            shape.IsNull() || viewport.IsNull()
        ) return;

        pos2D *= viewport.Size; // Apply viewport size
        float now = TIME.GetTicksMsec() * 0.001f;

        // Mutate the real event's position instead of creating a new one
        if (inputEvent is InputEventMouse mouseEvent)
        {
            mouseEvent.Position = pos2D;
            mouseEvent.GlobalPosition = pos2D;
        }

        (Vector2 lastPos, float lastTime) = cacheMap.GetValueOrDefault(viewport, (Vector2.Zero, now));

        if (inputEvent is InputEventMouseMotion motionEvent)
        {
            motionEvent.Position = pos2D;
            motionEvent.Relative = lastPos == Vector2.Zero ? Vector2.Zero : pos2D - lastPos;
            motionEvent.Velocity = motionEvent.Relative / (now - lastTime);
        }

        viewport.HandleInputLocally = true;
        viewport.GuiDisableInput = false;
        viewport.PushInput(inputEvent);

        cacheMap[viewport] = (pos2D, now);
    }

    public virtual Vector2 MapTo01(Vector3 globalPos, Node3D center, Vector2 mapSize)
    {
        if (center.IsNull()) return Vector2.Zero;

        Vector3 delta = globalPos - center.GlobalPosition;

        Vector2 pos01 = new Vector2(
            delta.Dot(center.Right()),
            delta.Dot(center.Up())
        ) / mapSize + new Vector2(0.5f, 0.5f);

        pos01.Y = 1.0f - pos01.Y; // UI start from top, so flip y

        return pos01;
    }

    /// <summary> Map the given global position to a 0-1 position on the page based on shape size (X and Y, not Z!). </summary>
    public virtual bool TryMapTo01(Vector3 globalPos, CollisionShape3D shape, out Vector2 pos01)
    {
        if (shape.NotNull() && shape.Shape is BoxShape3D box)
        {
            pos01 = MapTo01(globalPos, shape, new(box.Size.X, box.Size.Y));

            return pos01.X >= 0.0f && pos01.Y >= 0.0f && pos01.X <= 1.0f && pos01.Y <= 1.0f;
        }

        pos01 = default;
        return false;
    }

    public abstract bool TryGetMappingSurface(Vector3 globalPos, Area3D area, out CollisionShape3D shape, out SubViewport viewport);

    #endregion
}
#endif