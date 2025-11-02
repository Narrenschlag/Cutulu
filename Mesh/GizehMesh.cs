#if GODOT4_0_OR_GREATER
namespace Cutulu.Mesh;

using System.Collections.Generic;
using Cutulu.Core;
using Godot;

/// <summary>
/// GizehMesh is a mesh generator that uses a custom mesh builder to create triangle meshes.
/// This tool is specifically designed to be used for procedural mesh generation with Materials and does thereby not support vertex colors.
/// Created on 26.10.25 by Max for Warlord's modifiable terrain.
/// </summary>
public class GizehMesh
{
    private readonly Dictionary<Vector3, Vector3, int> VertexMap = [];
    private readonly Dictionary<int, Surface> Surfaces = [];

    private readonly Dictionary<int, Vertex> Vertices = [];

    private readonly Dictionary<Vector3, int> PositionIndex = [];
    private readonly Dictionary<int, Vector3> Positions = [];

    public float SmoothAngle { get; set; } = 0.0f;

    public Vector3 Offset { get; set; }
    private ArrayMesh Mesh { get; set; }

    private float CalcSmoothAngleValueCache { get; set; } = float.MaxValue;
    private float CalcSmoothAngle { get; set; }
    private int PositionID { get; set; }
    private int VertexID { get; set; }

    public void Clear()
    {
        Offset = default;

        VertexMap.Clear();
        Surfaces.Clear();

        Vertices.Clear();

        PositionIndex.Clear();
        Positions.Clear();
    }

    public ArrayMesh GetMesh()
    {
        if (Mesh.NotNull()) return Mesh;

        Mesh = new();

        var surfaceTool = new SurfaceTool();

        foreach (var triangles in Surfaces.Values)
        {
            foreach (var surface in Surfaces.Values)
            {
                surface.Commit(surfaceTool, this, Mesh);
            }
        }

        return Mesh;
    }

    public void Apply(MeshInstance3D meshInstance3D)
    {
        if (meshInstance3D.IsNull()) return;

        meshInstance3D.Mesh.Destroy();

        meshInstance3D.Mesh = GetMesh();
    }

    public MeshInstance3D Apply(Node parent)
    {
        var meshInstance = new MeshInstance3D();
        parent.AddChild(meshInstance);

        Apply(meshInstance);
        return meshInstance;
    }

    public void AddCounterClockwise(int surfaceIndex, params Vector3[] vertices) => AddCounterClockwise(surfaceIndex, false, vertices);
    public void AddCounterClockwise(params Vector3[] vertices) => AddCounterClockwise(0, false, vertices);
    public void AddCounterClockwise(int surfaceIndex, bool shadeSmooth, params Vector3[] vertices)
    {
        if (vertices.IsEmpty()) return;

        Mesh.Destroy();
        Mesh = null;

        if (vertices.Length < 3) return;

        var count = Mathf.FloorToInt(vertices.Length / 2f);

        // Reverse vertices
        for (int i = 0, k = vertices.Length - 1; i < count; i++, k--)
        {
            (vertices[i], vertices[k]) = (vertices[k], vertices[i]);
        }

        AddClockwise(surfaceIndex, shadeSmooth, vertices);
    }

    public void AddClockwise(int surfaceIndex, params Vector3[] vertices) => AddClockwise(surfaceIndex, false, vertices);
    public void AddClockwise(params Vector3[] vertices) => AddClockwise(0, false, vertices);
    public void AddClockwise(int surfaceIndex, bool shadeSmooth, params Vector3[] vertices)
    {
        if (vertices.IsEmpty()) return;

        Mesh.Destroy();
        Mesh = null;

        if (vertices.Length < 3) return;

        if (CalcSmoothAngleValueCache != SmoothAngle)
        {
            CalcSmoothAngle = Mathf.Cos(Mathf.DegToRad(SmoothAngle));
            CalcSmoothAngleValueCache = SmoothAngle;
        }

        switch (vertices.Length)
        {
            case 3:
                var n = GetFacingDirection(vertices[0], vertices[1], vertices[2]);

                var a = AddOrBlendVertex(vertices[0], n, shadeSmooth);
                var b = AddOrBlendVertex(vertices[1], n, shadeSmooth);
                var c = AddOrBlendVertex(vertices[2], n, shadeSmooth);

                if (Surfaces.TryGetValue(surfaceIndex, out var surface) == false)
                    Surfaces[surfaceIndex] = surface = new();

                surface.Add(a.VertexID, b.VertexID, c.VertexID);
                break;

            case 4:
                AddClockwise(surfaceIndex, shadeSmooth, vertices[0], vertices[1], vertices[2]);
                AddClockwise(surfaceIndex, shadeSmooth, vertices[2], vertices[3], vertices[0]);
                break;

            default:
                var ydic = new Dictionary<Vector2, float>();
                var points = new Vector2[vertices.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    ydic[
                        points[i] = vertices[i].toXY()
                    ] = vertices[i].Y;
                }

                var tris = TriangulateConcaveShape(
                    SortToPath(points)
                );

                foreach (var tri in tris)
                {
                    AddClockwise(surfaceIndex, shadeSmooth,
                        tri[0].toXZ(ydic[tri[0]]),
                        tri[1].toXZ(ydic[tri[1]]),
                        tri[2].toXZ(ydic[tri[2]])
                    );
                }
                break;
        }
    }

    /// <summary>
    /// Careful with this method. This can create duplicates. But it can be loaded safely by refreshing the mesh.
    /// </summary>
    public void MovePosition(Vector3 oldPos, Vector3 newPos)
    {
        if (oldPos == newPos) return;

        Positions[GetPositionID(oldPos)] = newPos;
    }

    #region Backend

    public int GetPositionID(Vector3 position)
    {
        if (PositionIndex.TryGetValue(position, out var id)) return id;

        PositionIndex[position] = id = PositionID++;
        Positions[id] = position;

        return id;
    }

    public Vector3 GetPosition(int id) => Positions.TryGetValue(id, out var position) ? position : default;

    public Vertex AddVertex(Vector3 position, Vector3 normal) => GetVertex(AddVertexIndex(position, normal));
    public Vertex GetVertex(int index) => Vertices[index];

    public bool TryGetVertexIndex(Vector3 position, Vector3 normal, out int index)
    {
        return VertexMap.TryGetValue(position, normal, out index);
    }

    public int AddVertexIndex(Vector3 position, Vector3 normal)
    {
        if (TryGetVertexIndex(position, normal, out var index)) return index;

        var positionId = GetPositionID(position);
        var vertex = new Vertex(++VertexID, positionId, normal);
        VertexMap[position, normal] = vertex.VertexID;

        Vertices.Add(vertex.VertexID, vertex);

        return vertex.VertexID;
    }

    private Vertex AddOrBlendVertex(Vector3 position, Vector3 faceNormal, bool shadeSmooth)
    {
        // If smooth shading is off, behave normally
        if (shadeSmooth == false) return AddVertex(position, faceNormal);

        // Try find existing vertex at same position
        if (VertexMap.TryGetValue(position, out var normals))
        {
            // Average normals if vertex exists
            if (normals.Count > 0)
            {
                var average = Vector3.Zero;

                foreach (var n in normals.Keys)
                {
                    if (n.Dot(faceNormal) >= CalcSmoothAngle)
                        average += n;
                }

                average += faceNormal;
                faceNormal = (average / (normals.Count + 1)).Normalized();
            }
        }

        // Add or replace vertex with blended normal
        var index = AddVertexIndex(position, faceNormal);
        var vertex = new Vertex(faceNormal, GetVertex(index));
        Vertices[index] = vertex;

        return vertex;
    }

    #endregion

    #region Utility

    public void AddQuadDir(Vector3 direction, Color color, Vector3 a, Vector3 b)
    {
        var c = b + direction;

        AddCounterClockwise(a, a + direction, c);
        AddCounterClockwise(c, b, a);
    }

    public void AddWalls(Vector3 direction, params Vector3[] vertices) => AddWalls(direction, default, vertices);
    public void AddWalls(Vector3 direction, Color color, params Vector3[] vertices)
    {
        for (var i = 1; i < vertices.Length; i++)
        {
            AddQuadDir(direction, color, vertices[i - 1], vertices[i]);
        }
    }

    public static Vector3 GetFacingDirection(params Vector3[] points)
    {
        if (points.Size() < 3) return Vector3.Up;

        // Calculate two vectors in the plane
        var AB = points[1] - points[0];
        var AC = points[2] - points[0];

        // Compute the cross product to get the normal vector
        return -AB.Cross(AC).Normalized();
    }

    public static Vector3[] SortClockwise(params Vector3[] points) => SortClockwise(false, points);
    public static Vector3[] SortClockwise(bool flip, params Vector3[] points)
    {
        if (points.IsEmpty()) return points;

        // Calculate the centroid of the points
        var centroid = CalculateCentroid(points);

        // Calculate angles for each point relative to the centroid
        var angles = new Dictionary<Vector3, float>();
        foreach (var point in points)
        {
            angles[point] = (float)Mathf.Atan2(point.Z - centroid.Z, point.X - centroid.X);
        }

        // Sort points by angle
        points = angles.Keys.ToArray();
        System.Array.Sort(points, (a, b) => flip ? angles[b].CompareTo(angles[a]) : angles[a].CompareTo(angles[b]));

        return points;

        static Vector3 CalculateCentroid(Vector3[] points)
        {
            var centroid = new Vector3();

            foreach (var point in points)
            {
                centroid += point;
            }

            return centroid / points.Length;
        }
    }

    public void RecalculateUVsByXZ(bool relative = true, Vector2 multiplier = default)
    {
        if (multiplier == default) multiplier = Vector2.One;

        if (relative)
        {
            var min = GetPosition(Vertices[0].PositionID).toXY();
            var max = min;

            for (var i = 0; i < Vertices.Count; i++)
            {
                var pos = GetPosition(Vertices[i].PositionID);

                min = min.Min(pos.toXY());
                max = max.Max(pos.toXY());
            }

            for (var i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i];
                var pos = GetPosition(vertex.PositionID);

                vertex.SetUV((pos.toXY() - min) / (max - min) * multiplier);
            }
        }

        else
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].SetUV(GetPosition(Vertices[i].PositionID).toXY() * multiplier);
            }
        }
    }

    #endregion

    #region Structs

    public struct Surface
    {
        public readonly Dictionary<int, Vector3I> Triangles;

        public Surface()
        {
            Triangles = new();
        }

        public int Add(int a, int b, int c)
        {
            var value = new Vector3I(a, b, c);
            var index = Triangles.Count;

            Triangles[index] = value;

            return index;
        }

        public void Commit(SurfaceTool surfaceTool, GizehMesh builder, ArrayMesh mesh)
        {
            surfaceTool.Begin(Godot.Mesh.PrimitiveType.Triangles);

            var indexes = new Dictionary<int, int>();

            foreach (var triangle in Triangles.Values)
            {
                addVertex(builder.Vertices[triangle.X]);
                addVertex(builder.Vertices[triangle.Y]);
                addVertex(builder.Vertices[triangle.Z]);

                void addVertex(Vertex vertex)
                {
                    if (indexes.ContainsKey(vertex.VertexID)) return;

                    indexes[vertex.VertexID] = indexes.Count;

                    surfaceTool.SetNormal(vertex.Normal);
                    surfaceTool.SetUV(vertex.GetUV());

                    surfaceTool.AddVertex(builder.GetPosition(vertex.PositionID) + builder.Offset);
                }
            }

            foreach (var triangle in Triangles.Values)
            {
                surfaceTool.AddIndex(indexes[triangle.X]);
                surfaceTool.AddIndex(indexes[triangle.Y]);
                surfaceTool.AddIndex(indexes[triangle.Z]);
            }

            surfaceTool.Commit(mesh);
        }
    }

    private static Vector2 GetAreaWeightedCentroid(Vector2[] vertices)
    {
        if (vertices == null || vertices.Length < 3)
            throw new System.ArgumentException($"[{nameof(GizehMesh)}] A polygon must have at least 3 vertices. ({vertices?.Length ?? 0})");

        var signedArea = 0f;
        var centroidX = 0f;
        var centroidY = 0f;

        var count = vertices.Length;
        for (var i = 0; i < count; i++)
        {
            var p0 = vertices[i];
            var p1 = vertices[(i + 1) % count]; // Wrap around for last edge

            var cross = p0.X * p1.Y - p1.X * p0.Y; // Shoelace formula
            signedArea += cross;

            centroidX += (p0.X + p1.X) * cross;
            centroidY += (p0.Y + p1.Y) * cross;
        }

        signedArea *= 0.5f;
        if (Mathf.Abs(signedArea) < Mathf.Epsilon) // Prevent division by zero
            return vertices[0]; // Fallback to first vertex

        centroidX /= 6f * signedArea;
        centroidY /= 6f * signedArea;

        return new Vector2(centroidX, centroidY);
    }

    private static Vector2[] SortToPath(params Vector2[] points)
    {
        return SortToPath(
            // Find the centroid of all points weighted on area
            GetAreaWeightedCentroid(points),
            points
        );
    }

    private static Vector2[] SortToPath(Vector2 centroid, params Vector2[] points)
    {
        if (points == null || points.Length < 3)
        {
            //Debug.LogError("At least 3 points are required to form a loop.");
            return points;
        }

        // Sort points by angle around the centroid
        var sortedPoints = new System.Collections.Generic.List<Vector2>(points);
        sortedPoints.Sort((a, b) =>
        {
            // Compute angles relative to the centroid
            var angleA = Mathf.Atan2(a.Y - centroid.Y, a.X - centroid.X);
            var angleB = Mathf.Atan2(b.Y - centroid.Y, b.X - centroid.X);

            return angleA.CompareTo(angleB);
        });

        // Return the sorted points as a loop
        return sortedPoints.ToArray();
    }

    private static List<Vector2[]> TriangulateConcaveShape(Vector2[] sortedPoints)
    {
        if (sortedPoints == null || sortedPoints.Length < 3)
        {
            Debug.LogError($"[{nameof(GizehMesh)}] At least 3 points are required to form a shape.");
            return null;
        }

        // List to hold the resulting triangles
        var triangles = new List<Vector2[]>();

        // Create a working list of indices
        var indices = new List<int>();
        for (var i = 0; i < sortedPoints.Length; i++)
        {
            indices.Add(i);
        }

        // Ear clipping loop
        while (indices.Count > 3)
        {
            var earFound = false;

            // Check each triplet of points to find an ear
            for (var i = 0; i < indices.Count; i++)
            {
                var prevIndex = indices[(i - 1 + indices.Count) % indices.Count];
                var currIndex = indices[i];
                var nextIndex = indices[(i + 1) % indices.Count];

                var prev = sortedPoints[prevIndex];
                var curr = sortedPoints[currIndex];
                var next = sortedPoints[nextIndex];

                // Check if the triangle (prev, curr, next) is an ear
                if (IsEar(prev, curr, next, sortedPoints, indices))
                {
                    // Add the triangle to the result
                    triangles.Add(new Vector2[] { prev, curr, next });

                    // Remove the ear vertex
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            // If no ear was found, the shape might be malformed
            if (!earFound)
            {
                Debug.LogError($"[{nameof(GizehMesh)}] Failed to find an ear. The shape might be malformed or degenerate.");
                return null;
            }
        }

        // Add the final triangle
        if (indices.Count == 3)
        {
            triangles.Add(new Vector2[] {
                    sortedPoints[indices[0]], sortedPoints[indices[1]], sortedPoints[indices[2]]
                });
        }

        return triangles;

        static bool IsEar(Vector2 prev, Vector2 curr, Vector2 next, Vector2[] points, List<int> indices)
        {
            // Check if the triangle is counter-clockwise
            if (!IsCounterClockwise(prev, curr, next))
                return false;

            // Check if any other point is inside the triangle
            for (var i = 0; i < points.Length; i++)
            {
                // Ignore points that are part of the triangle
                if (!indices.Contains(i)) continue;

                var point = points[i];

                if (point != prev && point != curr && point != next && IsPointInTriangle(point, prev, curr, next))
                {
                    return false;
                }
            }

            return true;

            // Barycentric method to check if the point is inside the triangle
            static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                return Mathf.IsEqualApprox(
                    TriangleArea(a, b, c),
                    TriangleArea(p, b, c) + TriangleArea(p, c, a) + TriangleArea(p, a, b)
                );
            }

            // Cross product > 0 means counter-clockwise
            static bool IsCounterClockwise(Vector2 a, Vector2 b, Vector2 c)
            {
                return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X) > 0;
            }

            static float TriangleArea(Vector2 a, Vector2 b, Vector2 c)
            {
                return Mathf.Abs((a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y)) / 2f);
            }
        }
    }

    public struct Vertex
    {
        public Vector2[] UVs { get; set; }
        public int VertexID { get; set; }

        public Vector3 Normal { get; set; }
        public int PositionID { get; set; }

        public Vertex(int vertexIdx, int positionId, Vector3 normal, Vector2 uv = default, Vector2 uv2 = default) : this()
        {
            PositionID = positionId;
            VertexID = vertexIdx;
            Normal = normal;

            UVs[0] = uv;
            UVs[1] = uv2;
        }

        public Vertex()
        {
            UVs = new Vector2[2];
            VertexID = default;

            PositionID = default;
            Normal = default;
        }

        public Vertex(Vector3 normal, Vertex original) : this(original.VertexID, original.PositionID, normal, original.UVs[0], original.UVs[1]) { }

        public readonly void SetUV(Vector2 uv) => UVs[0] = uv;
        public readonly Vector2 GetUV() => UVs[0];

        public readonly void SetUV2(Vector2 uv2) => UVs[1] = uv2;
        public readonly Vector2 GetUV2() => UVs[1];
    }

    #endregion
}
#endif