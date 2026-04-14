#if GODOT4_0_OR_GREATER
namespace Cutulu.Core;

using Godot;

public struct Obb
{
    public Vector3 Center;
    public Vector3 HalfExtents;
    public Basis Rotation;

    public Obb(Vector3 center, Vector3 halfExtents, Basis rotation)
    {
        Center = center;
        HalfExtents = halfExtents;
        Rotation = rotation;
    }

    public readonly Vector3 Size => HalfExtents * 2f;

    public readonly Vector3 GetCenter() => Center;

    public readonly bool HasVolume() => HalfExtents.X > 0f && HalfExtents.Y > 0f && HalfExtents.Z > 0f;

    public readonly bool IsZeroApprox() => HalfExtents.IsZeroApprox();

    public readonly Vector3 GetEndpoint(int idx) => Center + Rotation * new Vector3(
        (idx & 1) != 0 ? HalfExtents.X : -HalfExtents.X,
        (idx & 2) != 0 ? HalfExtents.Y : -HalfExtents.Y,
        (idx & 4) != 0 ? HalfExtents.Z : -HalfExtents.Z
    );

    public readonly bool ContainsPoint(Vector3 point)
    {
        var local = Rotation.Inverse() * (point - Center);
        return Mathf.Abs(local.X) <= HalfExtents.X &&
               Mathf.Abs(local.Y) <= HalfExtents.Y &&
               Mathf.Abs(local.Z) <= HalfExtents.Z;
    }

    public float GetVolume() => Size.X * Size.Y * Size.Z;

    public Obb Expand(Vector3 point)
    {
        var local = Rotation.Inverse() * (point - Center);
        var newMin = (-HalfExtents).Min(local);
        var newMax = HalfExtents.Max(local);
        var newLocalCenter = (newMin + newMax) * 0.5f;
        return new Obb(
            Center + Rotation * newLocalCenter,
            (newMax - newMin) * 0.5f,
            Rotation
        );
    }

    public Obb Grow(float by) => new(Center, HalfExtents + new Vector3(by, by, by), Rotation);

    // Construct from a Transform3D * Aabb (like you'd get from GlobalTransform * GetAabb())
    public static Obb FromTransformedAabb(Transform3D transform, Aabb aabb)
    {
        return new Obb(
            transform * aabb.GetCenter(),
            aabb.Size * 0.5f,
            transform.Basis.Orthonormalized() // strip scale
        );
    }

    public static Obb FromAabb(Aabb aabb)
    {
        return new Obb(
            aabb.GetCenter(),
            aabb.Size * 0.5f,
            Basis.Identity
        );
    }

    // Convert back to an Aabb (axis-aligned, so this is a conservative fit)
    public Aabb ToAabb()
    {
        // Project each axis-aligned half-extent through the rotation
        var x = Rotation.X * HalfExtents.X;
        var y = Rotation.Y * HalfExtents.Y;
        var z = Rotation.Z * HalfExtents.Z;

        var absHalf = new Vector3(
            Mathf.Abs(x.X) + Mathf.Abs(y.X) + Mathf.Abs(z.X),
            Mathf.Abs(x.Y) + Mathf.Abs(y.Y) + Mathf.Abs(z.Y),
            Mathf.Abs(x.Z) + Mathf.Abs(y.Z) + Mathf.Abs(z.Z)
        );

        return new Aabb(Center - absHalf, absHalf * 2f);
    }

    // Merge: returns a new OBB in the same orientation that contains both
    // NOTE: merged OBB keeps this OBB's rotation — orientation is not averaged
    public Obb Merge(Obb other)
    {
        // Project other's center and extents into this OBB's local space
        var localCenter = Rotation.Inverse() * (other.Center - Center);

        // Project other's half extents into this local frame
        var otherLocalHalf = new Vector3(
            Mathf.Abs((Rotation.Inverse() * (other.Rotation.X * other.HalfExtents.X)).X) +
            Mathf.Abs((Rotation.Inverse() * (other.Rotation.Y * other.HalfExtents.Y)).X) +
            Mathf.Abs((Rotation.Inverse() * (other.Rotation.Z * other.HalfExtents.Z)).X),

            Mathf.Abs((Rotation.Inverse() * (other.Rotation.X * other.HalfExtents.X)).Y) +
            Mathf.Abs((Rotation.Inverse() * (other.Rotation.Y * other.HalfExtents.Y)).Y) +
            Mathf.Abs((Rotation.Inverse() * (other.Rotation.Z * other.HalfExtents.Z)).Y),

            Mathf.Abs((Rotation.Inverse() * (other.Rotation.X * other.HalfExtents.X)).Z) +
            Mathf.Abs((Rotation.Inverse() * (other.Rotation.Y * other.HalfExtents.Y)).Z) +
            Mathf.Abs((Rotation.Inverse() * (other.Rotation.Z * other.HalfExtents.Z)).Z)
        );

        // Expand local AABB to contain both
        var minA = -HalfExtents;
        var maxA = HalfExtents;
        var minB = localCenter - otherLocalHalf;
        var maxB = localCenter + otherLocalHalf;

        var newMin = minA.Min(minB);
        var newMax = maxA.Max(maxB);

        var newLocalCenter = (newMin + newMax) * 0.5f;
        var newHalfExtents = (newMax - newMin) * 0.5f;

        return new Obb(
            Center + Rotation * newLocalCenter,
            newHalfExtents,
            Rotation
        );
    }

    public Obb Merge(Aabb aabb) => Merge(FromAabb(aabb));

    // SAT intersection test (Separating Axis Theorem)
    public bool Intersects(Obb other)
    {
        // 15 axes to test: 3 from each OBB + 9 cross products
        var axes = new Vector3[15];
        axes[0] = Rotation.X;
        axes[1] = Rotation.Y;
        axes[2] = Rotation.Z;
        axes[3] = other.Rotation.X;
        axes[4] = other.Rotation.Y;
        axes[5] = other.Rotation.Z;

        int idx = 6, i, j;
        for (i = 0; i < 3; i++)
        {
            for (j = 0; j < 3; j++)
            {
                var a = i switch { 0 => Rotation.X, 1 => Rotation.Y, _ => Rotation.Z };
                var b = j switch { 0 => other.Rotation.X, 1 => other.Rotation.Y, _ => other.Rotation.Z };
                axes[idx++] = a.Cross(b);
            }
        }

        var translation = other.Center - Center;

        foreach (var axis in axes)
        {
            if (axis.LengthSquared() < 1e-10f) continue; // skip near-zero cross products

            float ra = ProjectedHalfExtent(axis);
            float rb = other.ProjectedHalfExtent(axis);
            float dist = Mathf.Abs(translation.Dot(axis.Normalized()));

            if (dist > ra + rb) return false;
        }

        return true;
    }

    public bool Intersects(Aabb aabb) => Intersects(FromAabb(aabb));

    // How far this OBB extends along an arbitrary axis
    private float ProjectedHalfExtent(Vector3 axis)
    {
        axis = axis.Normalized();
        return
            Mathf.Abs((Rotation.X * HalfExtents.X).Dot(axis)) +
            Mathf.Abs((Rotation.Y * HalfExtents.Y).Dot(axis)) +
            Mathf.Abs((Rotation.Z * HalfExtents.Z).Dot(axis));
    }

    // Build directly from a Node's global transform
    public static Obb FromNode(Node node)
    {
        var aabb = node.GetNodeAabb(); // your existing extension
        // GetNodeAabb already returns world-space, so no transform needed
        // But we lose rotation info — pass transform explicitly if you have it
        return FromAabb(aabb);
    }

    // Build from a Node3D preserving rotation
    public static Obb FromNode3D(Node3D node, Aabb localAabb)
    {
        return FromTransformedAabb(node.GlobalTransform, localAabb);
    }

    public override string ToString()
        => $"Obb(center={Center}, half={HalfExtents}, rot={Rotation})";
}
#endif