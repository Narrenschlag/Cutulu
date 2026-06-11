#if GODOT4_0_OR_GREATER
namespace Cutulu.Core;

using System.Collections.Generic;
using Godot.Collections;
using Godot;

/// <summary>
/// Mathf extension for area calculations.
/// </summary>
public static class Areaf
{
    public static float GetAreaInM2(this Vector2 a, Vector2 b, Vector2 c)
    {
        // Calculate two vectors representing two sides of the triangle
        var side1 = b - a;
        var side2 = c - a;

        // Calculate the cross product of the two sides
        var crossProduct = Vector2f.Cross(side1, side2);

        // Area of the triangle is half of the magnitude of the cross product
        var area = Mathf.Abs(crossProduct) * 0.5f;

        return area;
    }

    public static float GetAreaInM2(this Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var area = (float)Mathf.Abs(0.5 * (
            a.X * b.Y + b.X * c.Y + c.X * d.Y + d.X * a.Y -
            b.X * a.Y - c.X * b.Y - d.X * c.Y - a.X * d.Y
        ));

        return area;
    }

    /// <summary> Returns all bodies in the given Area3D. </summary>
    public static Array<Dictionary> GetShapesInArea(this Area3D area)
    {
        var spaceRid = PhysicsServer3D.BodyGetSpace(area.GetRid());
        var directState = PhysicsServer3D.SpaceGetDirectState(spaceRid);

        var query = new PhysicsShapeQueryParameters3D
        {
            ShapeRid = area.ShapeOwnerGetShape(0, 0).GetRid(),
            Transform = area.GlobalTransform,
            CollisionMask = area.CollisionMask
        };

        return directState.IntersectShape(query);
    }

    public static IEnumerable<Node3D> GetBodiesInArea(this Area3D area)
    {
        var spaceRid = PhysicsServer3D.BodyGetSpace(area.GetRid());
        var directState = PhysicsServer3D.SpaceGetDirectState(spaceRid);
        var query = new PhysicsShapeQueryParameters3D
        {
            ShapeRid = area.ShapeOwnerGetShape(0, 0).GetRid(),
            Transform = area.GlobalTransform,
            CollisionMask = area.CollisionMask
        };

        var seen = new HashSet<ulong>();
        foreach (var result in directState.IntersectShape(query))
        {
            var id = result["collider_id"].AsUInt64();
            if (seen.Add(id)) yield return result["collider"].As<Node3D>();
        }
    }
}
#endif