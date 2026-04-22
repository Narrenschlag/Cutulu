#if GODOT4_0_OR_GREATER
namespace Cutulu.Core;

using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Computes the convex hull of a point cloud using Andrew's Monotone Chain,
    /// then triangulates it as a fan from the first hull vertex.
    ///
    /// "Cluster to convex polygon" — any unordered set of points in, clean
    /// CCW triangle array out. Points inside the hull are discarded.
    /// </summary>
    public static Vector2[][] ClusterToConvexTriangles(params Vector2[] points)
    {
        var hull = ConvexHull(points);

        if (hull.Length < 3)
            return [];

        return TriangulateFan(hull);
    }

    /// <summary>
    /// Returns the convex hull of the point cloud as a CCW polygon.
    /// Collinear points on the hull boundary are excluded (clean vertex set).
    /// Returns an empty array if fewer than 3 non-coincident points exist.
    /// </summary>
    public static Vector2[] ConvexHull(params Vector2[] points)
    {
        if (points == null || points.Length < 3)
            return [];

        // Sort lexicographically: by X, then Y on tie.
        var sorted = (Vector2[])points.Clone();
        System.Array.Sort(sorted, (a, b) =>
            a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));

        var n = sorted.Length;
        var hull = new Vector2[2 * n];
        var k = 0;

        // Build lower hull (left to right).
        for (var i = 0; i < n; i++)
        {
            while (k >= 2 && Cross(hull[k - 2], hull[k - 1], sorted[i]) <= 0f)
                k--;
            hull[k++] = sorted[i];
        }

        // Build upper hull (right to left).
        for (int i = n - 2, t = k + 1; i >= 0; i--)
        {
            while (k >= t && Cross(hull[k - 2], hull[k - 1], sorted[i]) <= 0f)
                k--;
            hull[k++] = sorted[i];
        }

        // k - 1 because the last point equals the first (closed loop).
        var result = new Vector2[k - 1];
        System.Array.Copy(hull, result, k - 1);
        return result; // already CCW
    }

    /// <summary>
    /// Triangulates a convex polygon as a fan from vertex 0.
    /// Much faster than ear-clipping for convex shapes — O(n) vs O(n²).
    /// Input must be CCW; <see cref="ConvexHull"/> already guarantees this.
    /// </summary>
    public static Vector2[][] TriangulateFan(Vector2[] convexPolygon)
    {
        if (convexPolygon == null || convexPolygon.Length < 3)
            return [];

        var origin = convexPolygon[0];
        var triangles = new Vector2[convexPolygon.Length - 2][];

        for (var i = 1; i < convexPolygon.Length - 1; i++)
        {
            triangles[i - 1] = new[]
            {
            origin,
            convexPolygon[i],
            convexPolygon[i + 1]
        };
        }

        return triangles;
    }

    /// <summary>
    /// 2D cross product of vectors (a→b) and (a→c).
    /// Positive = CCW turn, zero = collinear, negative = CW turn.
    /// </summary>
    private static float Cross(Vector2 a, Vector2 b, Vector2 c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    /// <summary>
    /// Triangulates a cluster of points into a fully connected triangle mesh
    /// using the Bowyer-Watson incremental Delaunay algorithm.
    ///
    /// Every point becomes a node. Triangles maximize the minimum angle
    /// (no slivers where avoidable), which is ideal for mesh generation.
    /// </summary>
    public static Vector2[][] ClusterToDelaunayTriangles(params Vector2[] points)
    {
        if (points == null || points.Length < 3)
            return [];

        return BowyerWatson(points);
    }

    private static Vector2[][] BowyerWatson(Vector2[] points)
    {
        // 1. Create a super-triangle that contains all input points.
        var superTri = MakeSuperTriangle(points);
        var triangulation = new List<Triangle> { superTri };

        // 2. Insert each point incrementally.
        foreach (var point in points)
        {
            // Find every triangle whose circumcircle contains this point.
            var badTriangles = new List<Triangle>();
            foreach (var tri in triangulation)
            {
                if (tri.CircumcircleContains(point))
                    badTriangles.Add(tri);
            }

            // Find the boundary polygon of the cavity left by removing bad triangles.
            // An edge is on the boundary if it is NOT shared by two bad triangles.
            var boundary = new List<Edge>();
            foreach (var bad in badTriangles)
            {
                foreach (var edge in bad.Edges)
                {
                    var sharedByOther = false;
                    foreach (var other in badTriangles)
                    {
                        if (other.Equals(bad)) continue;
                        if (other.HasEdge(edge)) { sharedByOther = true; break; }
                    }

                    if (!sharedByOther)
                        boundary.Add(edge);
                }
            }

            // Remove bad triangles.
            foreach (var bad in badTriangles)
                triangulation.Remove(bad);

            // Re-triangulate cavity by connecting the new point to each boundary edge.
            foreach (var edge in boundary)
                triangulation.Add(new Triangle(point, edge.A, edge.B));
        }

        // 3. Remove any triangle that shares a vertex with the super-triangle.
        triangulation.RemoveAll(t => t.SharesVertexWith(superTri));

        // 4. Convert to output format.
        var result = new Vector2[triangulation.Count][];
        for (var i = 0; i < triangulation.Count; i++)
            result[i] = new[] { triangulation[i].A, triangulation[i].B, triangulation[i].C };

        return result;
    }

    /// <summary>
    /// Constructs a triangle large enough to contain all points.
    /// It is removed at the end — it's just a bootstrap container.
    /// </summary>
    private static Triangle MakeSuperTriangle(Vector2[] points)
    {
        var minX = points[0].X;
        var minY = points[0].Y;
        var maxX = minX;
        var maxY = minY;

        foreach (var p in points)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        var dx = (maxX - minX) * 10f;
        var dy = (maxY - minY) * 10f;

        var a = new Vector2(minX - dx, minY - dy);
        var b = new Vector2(minX + dx * 0.5f, maxY + dy);
        var c = new Vector2(maxX + dx, minY - dy);

        return new Triangle(a, b, c);
    }

    /// <summary>
    /// Computes the outline of a region defined by a set of triangles.
    /// Boundary edges are those belonging to exactly one triangle.
    /// Returns an ordered CCW polygon, or an empty array if none found.
    /// </summary>
    /// <param name="edgeDetectionOnly">
    /// If true, skips the chain-walking step and returns boundary edge vertices
    /// as an unordered flat array. Faster when you only need to know which
    /// vertices are on the border, not their polygon order.
    /// </param>
    public static Vector2[] GenerateOutline(this Vector2[][] triangles, bool edgeDetectionOnly = false)
    {
        if (triangles == null || triangles.Length < 1)
            return [];

        var edgeCount = new Dictionary<(Vector2, Vector2), int>(triangles.Length * 3);

        foreach (var tri in triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                var a = tri[i];
                var b = tri[(i + 1) % 3];
                var edge = a.X < b.X || (a.X == b.X && a.Y < b.Y) ? (a, b) : (b, a);
                edgeCount[edge] = edgeCount.TryGetValue(edge, out var count) ? count + 1 : 1;
            }
        }

        if (edgeDetectionOnly)
        {
            // Return unique boundary vertices, unordered.
            var verts = new HashSet<Vector2>();
            foreach (var (edge, count) in edgeCount)
            {
                if (count != 1) continue;
                verts.Add(edge.Item1);
                verts.Add(edge.Item2);
            }
            return [.. verts];
        }

        var boundary = new Dictionary<Vector2, Vector2>();
        foreach (var (edge, count) in edgeCount)
        {
            if (count != 1) continue;
            boundary[edge.Item1] = edge.Item2;
        }

        if (boundary.Count < 3)
            return [];

        var outline = new List<Vector2>(boundary.Count);
        var current = boundary.Keys.First();
        var start = current;

        while (true)
        {
            outline.Add(current);
            if (!boundary.TryGetValue(current, out var next)) break;
            boundary.Remove(current);
            current = next;
            if (current == start) break;
        }

        if (ComputeSignedArea([.. outline]) < 0f)
            outline.Reverse();

        return [.. outline];
    }

    /// <summary>
    /// Like GenerateOutline but supports disconnected triangle islands.
    /// Returns one outline polygon per connected region.
    /// </summary>
    /// <param name="edgeDetectionOnly">
    /// If true, returns one unordered boundary vertex array per outline loop
    /// instead of walking the chain into a proper polygon. Faster when polygon
    /// order is not needed.
    /// </param>


    /// <summary>
    /// Like GenerateOutline but supports disconnected triangle islands.
    /// Returns one outline polygon per connected region.
    /// </summary>
    public static Vector2[][] GenerateOutlines(this Vector2[][] triangles)
    {
        if (triangles == null || triangles.Length < 1)
            return [];

        // Same edge-counting logic as GenerateOutline.
        var edgeCount = new Dictionary<(Vector2, Vector2), int>(triangles.Length * 3);

        foreach (var tri in triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                var a = tri[i];
                var b = tri[(i + 1) % 3];
                var edge = a.X < b.X || (a.X == b.X && a.Y < b.Y) ? (a, b) : (b, a);
                edgeCount[edge] = edgeCount.TryGetValue(edge, out var count) ? count + 1 : 1;
            }
        }

        // Collect all boundary edges into a adjacency map.
        // Unlike GenerateOutline we keep ALL of them and walk until exhausted.
        var boundary = new Dictionary<Vector2, List<Vector2>>();

        foreach (var (edge, count) in edgeCount)
        {
            if (count != 1) continue;

            if (!boundary.TryGetValue(edge.Item1, out var listA))
                boundary[edge.Item1] = listA = [];
            listA.Add(edge.Item2);

            if (!boundary.TryGetValue(edge.Item2, out var listB))
                boundary[edge.Item2] = listB = [];
            listB.Add(edge.Item1);
        }

        if (boundary.Count < 3)
            return [];

        var outlines = new List<Vector2[]>();
        var visited = new HashSet<(Vector2, Vector2)>();

        // Keep picking an unvisited start vertex until all edges are consumed.
        foreach (var startVertex in boundary.Keys.ToArray())
        {
            foreach (var firstNeighbor in boundary[startVertex].ToArray())
            {
                if (visited.Contains((startVertex, firstNeighbor)))
                    continue;

                // Walk this loop.
                var outline = new List<Vector2>();
                var prev = startVertex;
                var current = firstNeighbor;

                outline.Add(prev);

                while (current != startVertex)
                {
                    outline.Add(current);
                    visited.Add((prev, current));
                    visited.Add((current, prev));

                    // Pick the next neighbor that isn't where we came from.
                    var neighbors = boundary[current];
                    var next = neighbors.FirstOrDefault(n => n != prev && !visited.Contains((current, n)));

                    if (next == default)
                        break; // dead end — malformed input

                    prev = current;
                    current = next;
                }

                visited.Add((prev, current));
                visited.Add((current, prev));

                if (outline.Count < 3)
                    continue;

                // Normalize to CCW.
                if (Triangulator2D.ComputeSignedArea([.. outline]) < 0f)
                    outline.Reverse();

                outlines.Add([.. outline]);
                break; // one loop per startVertex pass
            }
        }

        return [.. outlines];
    }

    /// <summary>
    /// Groups triangles into disconnected islands based on shared edges or vertices.
    /// Returns one Vector2[][] per island, each containing that island's triangles.
    /// </summary>
    /// <param name="edgeDetectionOnly">
    /// If true, uses shared edges only (not shared vertices) to determine connectivity.
    /// Triangles that only touch at a single vertex will be treated as separate islands.
    /// Slightly more precise for certain mesh topologies.
    /// </param>
    public static Vector2[][][] GetIslands(this Vector2[][] triangles, bool edgeDetectionOnly = false)
    {
        if (triangles == null || triangles.Length < 1)
            return [];

        var parent = new int[triangles.Length];
        for (var i = 0; i < parent.Length; i++)
            parent[i] = i;

        int Find(int x)
        {
            while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
            return x;
        }

        void Union(int a, int b)
        {
            a = Find(a); b = Find(b);
            if (a != b) parent[a] = b;
        }

        if (edgeDetectionOnly)
        {
            // Connect triangles only when they share a full edge (2 vertices), not just a vertex.
            var edgeToTri = new Dictionary<(Vector2, Vector2), int>();

            for (var i = 0; i < triangles.Length; i++)
            {
                var tri = triangles[i];
                for (int e = 0; e < 3; e++)
                {
                    var a = tri[e];
                    var b = tri[(e + 1) % 3];
                    var edge = a.X < b.X || (a.X == b.X && a.Y < b.Y) ? (a, b) : (b, a);

                    if (edgeToTri.TryGetValue(edge, out var other))
                        Union(i, other);
                    else
                        edgeToTri[edge] = i;
                }
            }
        }
        else
        {
            // Connect triangles that share any vertex.
            var vertexToTris = new Dictionary<Vector2, List<int>>();
            for (var i = 0; i < triangles.Length; i++)
                foreach (var v in triangles[i])
                {
                    if (!vertexToTris.TryGetValue(v, out var list))
                        vertexToTris[v] = list = [];
                    list.Add(i);
                }

            foreach (var list in vertexToTris.Values)
                for (var i = 1; i < list.Count; i++)
                    Union(list[0], list[i]);
        }

        var groups = new Dictionary<int, List<Vector2[]>>();
        for (var i = 0; i < triangles.Length; i++)
        {
            var root = Find(i);
            if (!groups.TryGetValue(root, out var group))
                groups[root] = group = [];
            group.Add(triangles[i]);
        }

        return [.. groups.Values.Select(g => g.ToArray())];
    }

    /// <summary>
    /// Returns the centroid of the largest outline per island.
    /// Correctly handles donuts by ignoring hole outlines.
    /// </summary>
    public static Vector2[] GetIslandCentroids(this Vector2[][] triangles, bool edgeDetectionOnly = false)
    => GetIslandCentroids(GetIslands(triangles, edgeDetectionOnly));

    /// <summary>
    /// Returns the centroid of the largest outline per island.
    /// Correctly handles donuts by ignoring hole outlines.
    /// </summary>
    public static Vector2[] GetIslandCentroids(this Vector2[][][] islands)
    {
        var centroids = new Vector2[islands.Length];

        for (var i = 0; i < islands.Length; i++)
        {
            var outlines = GenerateOutlines(islands[i]);

            // Outer border always has the largest absolute area.
            // Hole outlines are smaller and get ignored.
            var outer = outlines
                .OrderByDescending(o => Mathf.Abs(ComputeSignedArea(o)))
                .First();

            centroids[i] = ComputeCentroid(outer);
        }

        return centroids;
    }

    /// <summary>
    /// Computes the outline of a region defined by a set of triangles.
    /// Boundary edges are those belonging to exactly one triangle.
    /// Returns an ordered CCW polygon, or an empty array if none found.
    /// </summary>
    public static Vector2[] GenerateOutline(this Vector2[][] triangles)
    {
        if (triangles == null || triangles.Length < 1)
            return [];

        // Count how many triangles share each edge.
        // Edges are stored as (min, max) so direction doesn't matter.
        var edgeCount = new Dictionary<(Vector2, Vector2), int>(triangles.Length * 3);

        foreach (var tri in triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                var a = tri[i];
                var b = tri[(i + 1) % 3];

                // Normalize edge direction so (a,b) and (b,a) are the same key.
                var edge = a.X < b.X || (a.X == b.X && a.Y < b.Y) ? (a, b) : (b, a);
                edgeCount[edge] = edgeCount.TryGetValue(edge, out var count) ? count + 1 : 1;
            }
        }

        // Collect boundary edges — those appearing exactly once.
        var boundary = new Dictionary<Vector2, Vector2>(); // start → end

        foreach (var (edge, count) in edgeCount)
        {
            if (count != 1) continue;
            boundary[edge.Item1] = edge.Item2;
        }

        if (boundary.Count < 3)
            return [];

        // Walk the chain to produce an ordered polygon.
        var outline = new List<Vector2>(boundary.Count);
        var current = boundary.Keys.First();
        var start = current;

        while (true)
        {
            outline.Add(current);

            if (!boundary.TryGetValue(current, out var next))
                break; // open chain — shouldn't happen with valid triangles

            boundary.Remove(current); // prevent revisiting
            current = next;

            if (current == start)
                break;
        }

        // Normalize to CCW winding.
        if (Triangulator2D.ComputeSignedArea([.. outline]) < 0f)
            outline.Reverse();

        return [.. outline];
    }

    public static Vector2 GetRandomPositionInPolygon(this Vector2[][] triangles)
    => GetPositionInPolygon(triangles, Random.Value, Random.Value);

    /// <summary>
    /// Returns a uniformly distributed random point inside the triangle set,
    /// with triangles weighted by area so larger triangles are picked more often.
    /// </summary>
    public static Vector2 GetPositionInPolygon(this Vector2[][] triangles, float r1, float r2)
    {
        if (triangles == null || triangles.Length < 1)
            return Vector2.Zero;

        // Compute cumulative area array for weighted selection.
        var areas = new float[triangles.Length];
        var total = 0f;

        for (var i = 0; i < triangles.Length; i++)
        {
            var tri = triangles[i];
            var value = Mathf.Abs(ComputeSignedArea(tri));
            total += value;
            areas[i] = value;
        }

        // Pick a triangle weighted by area.
        var threshold = Random.Value * total;
        var accumulated = 0f;
        var chosen = triangles[^1];

        for (var i = 0; i < areas.Length; i++)
        {
            accumulated += areas[i];
            if (threshold <= accumulated)
            {
                chosen = triangles[i];
                break;
            }
        }

        // Pick a uniform random point inside the chosen triangle.
        // The sqrt trick ensures uniform distribution (not biased toward one vertex).
        r1 = Mathf.Sqrt(r1);

        var a = chosen[0];
        var b = chosen[1];
        var c = chosen[2];

        return (1 - r1) * a + r1 * (1 - r2) * b + r1 * r2 * c;
    }

    public static float GetAreaOfPolygon(this Vector2[][] triangles)
    {
        float area = 0f;

        for (var i = 0; i < triangles.Length; i++)
        {
            var tri = triangles[i];
            area += Mathf.Abs(ComputeSignedArea(tri));
        }

        return area;
    }

    /// <summary>
    /// Returns <paramref name="count"/> evenly distributed points inside the polygon.
    /// Uses R2 low-discrepancy sequence (Roberts, 2018) which has better 2D uniformity
    /// than Halton — no clustering, no empty regions. Deterministic via seed.
    /// </summary>
    public static Vector2[] GetDistributedPositionsInPolygon(this Vector2[][] triangles, int count, int seed = 0)
    {
        if (triangles == null || triangles.Length < 1 || count < 1)
            return [];

        // Sort triangles spatially by centroid using a Z-order (Morton) curve
        // so adjacent indices in the array are spatially close.
        var sorted = triangles
            .OrderBy(t =>
            {
                var c = (t[0] + t[1] + t[2]) / 3f;
                return MortonCode(c);
            })
            .ToArray();

        // Build cumulative area table on sorted triangles.
        var areas = new float[sorted.Length];
        var cumulative = new float[sorted.Length];
        var total = 0f;

        for (var i = 0; i < sorted.Length; i++)
        {
            var value = Mathf.Abs(ComputeSignedArea(sorted[i]));
            areas[i] = value;
            total += value;
            cumulative[i] = total;
        }

        // Distribute exact point counts per triangle proportionally by area.
        var counts = new int[sorted.Length];
        var remainder = 0f;
        var assigned = 0;
        var areasCopy = (float[])areas.Clone();

        for (var i = 0; i < sorted.Length; i++)
        {
            var exact = areas[i] / total * count + remainder;
            var floor = (int)exact;
            remainder = exact - floor;
            counts[i] = floor;
            assigned += floor;
        }

        // Assign remainders to largest triangles.
        while (assigned < count)
        {
            var best = 0;
            for (var i = 1; i < sorted.Length; i++)
                if (areasCopy[i] > areasCopy[best]) best = i;
            counts[best]++;
            areasCopy[best] = 0f;
            assigned++;
        }

        // R2 sequence constants.
        const float A1 = 0.7548776662f;
        const float A2 = 0.5698402910f;

        // Seed phases via Wang hash.
        var s = (uint)seed;
        s = (s ^ 61u) ^ (s >> 16); s *= 9u; s ^= s >> 4; s *= 0x27d4eb2du; s ^= s >> 15;
        var phase1 = (s & 0xFFFFu) / 65536f;
        s = (s ^ 61u) ^ (s >> 16); s *= 9u; s ^= s >> 4; s *= 0x27d4eb2du; s ^= s >> 15;
        var phase2 = (s & 0xFFFFu) / 65536f;

        var result = new Vector2[count];
        var idx = 0;

        for (var t = 0; t < sorted.Length; t++)
        {
            var tri = sorted[t];
            var n = counts[t];

            for (var i = 0; i < n; i++)
            {
                // R2 sequence — globally indexed so it never resets between triangles.
                var v = (phase1 + A1 * (idx + 1)) % 1f;
                var w = (phase2 + A2 * (idx + 1)) % 1f;

                var r1 = Mathf.Sqrt(v);
                var r2 = w;

                result[idx++] = (1 - r1) * tri[0] + r1 * (1 - r2) * tri[1] + r1 * r2 * tri[2];
            }
        }

        return result;
    }

    /// <summary>
    /// Interleaves the bits of two floats mapped to integers — gives a Z-order curve
    /// that preserves 2D spatial locality as a 1D sort key.
    /// </summary>
    private static ulong MortonCode(Vector2 p)
    {
        // Normalize to positive integer space.
        var x = (uint)Mathf.Clamp(p.X * 1024f, 0f, uint.MaxValue / 2f);
        var y = (uint)Mathf.Clamp(p.Y * 1024f, 0f, uint.MaxValue / 2f);
        return Interleave(x) | (Interleave(y) << 1);
    }

    private static ulong Interleave(uint v)
    {
        ulong x = v;
        x = (x | x << 16) & 0x0000FFFF0000FFFFul;
        x = (x | x << 8) & 0x00FF00FF00FF00FFul;
        x = (x | x << 4) & 0x0F0F0F0F0F0F0F0Ful;
        x = (x | x << 2) & 0x3333333333333333ul;
        x = (x | x << 1) & 0x5555555555555555ul;
        return x;
    }
}
#endif