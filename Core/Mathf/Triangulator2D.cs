namespace Cutulu.Core;

using System.Collections.Generic;
using Godot;

/// <summary>
/// Standalone 2D polygon triangulator using ear-clipping.
/// Takes a border polygon (any winding order, convex or concave)
/// and returns a flat array of triangles as Vector2[3] each.
///
/// Fixes over GizehMesh's original:
///   - Winding is normalized to CCW before clipping (input order doesn't matter)
///   - Point-in-triangle uses a sign-based barycentric test (no fragile area-equality)
///   - Centroid sort is used only for winding normalization, not as the triangulation strategy
///   - Malformed / degenerate shapes return an empty array instead of null
/// 
/// Created on 18.04.26 by Max for Warlord's territory system. Spent a lot of time on documentation, so enjoy it! ;p
/// </summary>
public static class Triangulator2D
{
    /// <summary>
    /// Triangulate a polygon defined by its border vertices.
    /// </summary>
    /// <param name="border">Polygon outline — at least 3 points, any winding order.</param>
    /// <returns>
    /// Flat array of triangles. Every 3 consecutive elements form one triangle:
    ///   result[i][0], result[i][1], result[i][2]
    /// Returns an empty array if the input is invalid or triangulation fails.
    /// </returns>
    public static Vector2[][] Triangulate(params Vector2[] border)
    {
        if (border == null || border.Length < 3)
            return [];

        // Work on a copy so we never mutate the caller's array.
        var polygon = (Vector2[])border.Clone();

        // Normalize to CCW winding — the ear-clipper requires it.
        if (ComputeSignedArea(polygon) < 0f)
            System.Array.Reverse(polygon);

        return EarClip(polygon);
    }

    // Core ear-clipping
    private static Vector2[][] EarClip(Vector2[] polygon)
    {
        var result = new List<Vector2[]>(polygon.Length - 2);

        // Active indices into `polygon` — we remove ears one at a time.
        var idx = new List<int>(polygon.Length);
        for (var i = 0; i < polygon.Length; i++)
            idx.Add(i);

        var safety = idx.Count * idx.Count; // worst-case iterations
        var iterations = 0;

        while (idx.Count > 3)
        {
            if (++iterations > safety)
            {
                // Shape is degenerate or self-intersecting — bail with what we have.
                GD.PushWarning($"[{nameof(Triangulator2D)}] Ear-clipping stalled — shape may be degenerate.");
                break;
            }

            var earFound = false;
            var n = idx.Count;

            for (var i = 0; i < n; i++)
            {
                var iPrev = idx[(i - 1 + n) % n];
                var iCurr = idx[i];
                var iNext = idx[(i + 1) % n];

                var prev = polygon[iPrev];
                var curr = polygon[iCurr];
                var next = polygon[iNext];

                if (!IsEar(prev, curr, next, polygon, idx))
                    continue;

                result.Add([prev, curr, next]);
                idx.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                GD.PushWarning($"[{nameof(Triangulator2D)}] No ear found — shape may be self-intersecting.");
                break;
            }
        }

        if (idx.Count == 3)
            result.Add([polygon[idx[0]], polygon[idx[1]], polygon[idx[2]]]);

        return [.. result];
    }

    /// <summary>
    /// A vertex is an "ear" when:
    ///   1. The triangle it forms with its neighbours is CCW (convex corner).
    ///   2. No other active vertex lies strictly inside that triangle.
    /// </summary>
    private static bool IsEar(
        Vector2 prev, Vector2 curr, Vector2 next,
        Vector2[] polygon, List<int> activeIdx)
    {
        // Must be a convex (CCW) corner.
        if (!IsConvex(prev, curr, next))
            return false;

        // No other active vertex may lie inside the candidate ear.
        foreach (var i in activeIdx)
        {
            var p = polygon[i];

            // Skip the three vertices that form the ear itself.
            if (p == prev || p == curr || p == next)
                continue;

            if (IsPointInTriangle(p, prev, curr, next))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Signed area via the shoelace formula.
    /// Positive = CCW, Negative = CW.
    /// </summary>
    public static float ComputeSignedArea(Vector2[] poly)
    {
        var area = 0f;
        var n = poly.Length;

        for (var i = 0; i < n; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % n];
            area += a.X * b.Y - b.X * a.Y;
        }

        return area * 0.5f;
    }

    /// <summary>True when the turn prev → curr → next is counter-clockwise (left turn).</summary>
    public static bool IsConvex(Vector2 prev, Vector2 curr, Vector2 next)
    {
        // Cross product of (curr-prev) × (next-curr). Positive = CCW.
        return (curr.X - prev.X) * (next.Y - prev.Y)
             - (curr.Y - prev.Y) * (next.X - prev.X) > 0f;
    }

    /// <summary>
    /// Sign-based point-in-triangle test (robust against floating-point drift).
    /// Returns true if <paramref name="p"/> is strictly inside or on the edge of
    /// triangle (a, b, c) wound CCW.
    /// </summary>
    public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var d1 = Sign(p, a, b);
        var d2 = Sign(p, b, c);
        var d3 = Sign(p, c, a);

        var hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        var hasPos = d1 > 0f || d2 > 0f || d3 > 0f;

        // Point is inside when all signs agree (all ≥ 0 or all ≤ 0).
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p, Vector2 a, Vector2 b)
        => (p.X - b.X) * (a.Y - b.Y) - (a.X - b.X) * (p.Y - b.Y);

    /// <summary>Area-weighted centroid of a polygon (same formula as GizehMesh).</summary>
    public static Vector2 ComputeCentroid(Vector2[] poly)
    {
        var signedArea = 0f;
        var cx = 0f;
        var cy = 0f;
        var n = poly.Length;

        for (var i = 0; i < n; i++)
        {
            var p0 = poly[i];
            var p1 = poly[(i + 1) % n];
            var cross = p0.X * p1.Y - p1.X * p0.Y;

            signedArea += cross;
            cx += (p0.X + p1.X) * cross;
            cy += (p0.Y + p1.Y) * cross;
        }

        signedArea *= 0.5f;

        if (Mathf.Abs(signedArea) < Mathf.Epsilon)
            return poly[0];

        return new Vector2(cx / (6f * signedArea), cy / (6f * signedArea));
    }

    /// <summary>
    /// Tests whether <paramref name="point"/> lies inside the polygon defined by <paramref name="border"/>.
    /// Uses the ray-casting algorithm — fires a ray in the +X direction and counts edge crossings.
    /// Works for convex and concave polygons. Does not require any particular winding order.
    /// </summary>
    public static bool IsInsidePolygonExclusive(Vector2 point, params Vector2[] border)
    {
        if (border == null || border.Length < 3)
            return false;

        var inside = false;
        var n = border.Length;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var a = border[i];
            var b = border[j];

            // Does the edge a→b straddle the horizontal ray from `point` going right?
            if ((a.Y > point.Y) != (b.Y > point.Y))
            {
                // X coordinate where the edge crosses point.Y
                var xIntersect = (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X;

                if (point.X < xIntersect)
                    inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Same as IsInsidePolygonExclusive but treats points within <paramref name="epsilon"/>
    /// of any edge as inside. Useful for border snapping or touch detection.
    /// </summary>
    public static bool IsInsidePolygonInclusive(Vector2 point, float epsilon, params Vector2[] border)
    {
        if (IsInsidePolygonExclusive(point, border))
            return true;

        var n = border.Length;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (DistanceToSegment(point, border[j], border[i]) <= epsilon)
                return true;
        }

        return false;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var len2 = ab.LengthSquared();

        if (len2 < Mathf.Epsilon)
            return p.DistanceTo(a);

        var t = Mathf.Clamp((p - a).Dot(ab) / len2, 0f, 1f);
        return p.DistanceTo(a + t * ab);
    }
}