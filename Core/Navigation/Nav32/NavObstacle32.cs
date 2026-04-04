#if GODOT4_0_OR_GREATER
namespace Cutulu.Core;

using System.Collections.Generic;
using Godot;

/// <summary>
/// A pathfinding map with 32x32 chunks with 32bit chunk side hashes. 
/// Heavily inspired by Tynan Sylvester's RimWorld.
/// Written, with blood, sweat and tears, by Maximilian Schecklmann April 2nd to 4th 2026. 
/// </summary>
public class NavObstacle32
{
    public object Validator;

    public readonly IReadOnlyCollection<Aabb> Aabb;
    public readonly IReadOnlyCollection<Obb> Obb;

    public readonly Obb Bounds;

    public NavObstacle32(object validator, IReadOnlyCollection<Aabb> aabb, IReadOnlyCollection<Obb> obb)
    {
        Validator = validator;

        Aabb = aabb;
        Obb = obb;

        Obb bounds = new();
        bool first = true;

        if (Aabb.NotEmpty())
            foreach (var a in Aabb)
                if (first)
                {
                    first = false;
                    bounds = Core.Obb.FromAabb(a);
                }
                else bounds = bounds.Merge(a);

        if (Obb.NotEmpty())
            foreach (var o in Obb)
                if (first)
                {
                    first = false;
                    bounds = o;
                }
                else bounds = bounds.Merge(o);

        Bounds = bounds;
    }

    public NavObstacle32(object validator, params Aabb[] aabb)
    : this(validator, aabb, null) { }

    public NavObstacle32(object validator, params Obb[] obb)
    : this(validator, null, obb) { }

    public bool IsValid() => Validator.NotNull() && Bounds.Size.IsZeroApprox() == false;

    public void Invalidate() => Validator = null;
}
#endif