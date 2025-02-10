namespace Cutulu.Mesh
{
    using System.Collections.Generic;
    using Godot;

    using Core;

    /// <summary>
    /// Highly GPU optimized mesh generation tool for fast and easy use. Allows for fast polygon and wall meshes to be added.
    /// </summary>
    public partial class MeshBuilder
    {
        // Mesh Type
        public readonly MeshType Type;

        // Triangles
        private readonly Dictionary<Vector3, Dictionary<Vector3, int>> VerticeMap = new();
        private readonly Dictionary<int, Surface> Surfaces = new();
        private readonly Dictionary<int, Vertex> Vertices = new();

        // Lines
        private readonly Dictionary<Vector3, HashSet<int>> LineMap = new();
        private readonly Dictionary<int, (Vector3 Start, Vector3 End, Color Color)> Lines = new();

        // LineStrip
        private readonly Dictionary<Vector3, Color> LineStrips = new();

        // Points
        private readonly Dictionary<Vector3, Color> Points = new();

        public Color DefaultColor { get; set; }
        public Vector3 Offset { get; set; }

        public bool UseVertexColor { get; set; }

        private ArrayMesh Mesh { get; set; }
        private bool UseAlpha { get; set; }

        public MeshBuilder(MeshType type) : this(type, Colors.White) { }
        public MeshBuilder(MeshType type, Color baseColor)
        {
            switch (type)
            {
                case MeshType.Triangles: break;
                case MeshType.Lines: break;
                case MeshType.Points: break;
                case MeshType.LineStrip: break;

                default: throw new($"[{nameof(MeshBuilder)}] MeshType({type}) is not supported.");
            }

            DefaultColor = baseColor;
            Type = type;
        }

        public void Clear()
        {
            Offset = default;

            VerticeMap.Clear();
            Surfaces.Clear();
            Vertices.Clear();

            LineStrips.Clear();
            LineMap.Clear();
            Lines.Clear();

            Points.Clear();
        }

        public ArrayMesh GetMesh()
        {
            if (Mesh.NotNull()) return Mesh;

            Mesh = new();

            var surfaceTool = new SurfaceTool();
            UseAlpha = false;

            switch (Type)
            {
                case MeshType.Lines:
                    surfaceTool.Begin((Mesh.PrimitiveType)(byte)Type);

                    foreach (var line in Lines.Values)
                    {
                        if (UseVertexColor) surfaceTool.SetColor(Color(line.Color));

                        surfaceTool.AddVertex(line.Start);
                        surfaceTool.AddVertex(line.End);
                    }

                    surfaceTool.Commit(Mesh);
                    break;

                case MeshType.LineStrip:
                    surfaceTool.Begin((Mesh.PrimitiveType)(byte)Type);

                    foreach (var line in LineStrips)
                    {
                        if (UseVertexColor) surfaceTool.SetColor(Color(line.Value));

                        surfaceTool.AddVertex(line.Key);
                    }

                    surfaceTool.Commit(Mesh);
                    break;

                case MeshType.Points:
                    surfaceTool.Begin((Mesh.PrimitiveType)(byte)Type);

                    foreach (var point in Points)
                    {
                        if (UseVertexColor) surfaceTool.SetColor(Color(point.Value));

                        surfaceTool.AddVertex(point.Key);
                    }

                    surfaceTool.Commit(Mesh);
                    break;

                case MeshType.Triangles:
                    var useAlpha = false;

                    foreach (var triangles in Surfaces.Values)
                    {
                        foreach (var surface in Surfaces.Values)
                        {
                            surface.Commit(surfaceTool, this, Mesh, ref useAlpha);
                        }
                    }

                    UseAlpha = useAlpha;
                    break;

                default: break;
            }

            Color Color(Color color)
            {
                switch (Type)
                {
                    case MeshType.LineStrip: break;
                    case MeshType.Lines: break;

                    case MeshType.Triangles:
                        if (UseAlpha == false) UseAlpha = color.A < 1f;

                        return color;

                    default:
                        color = color == default ? DefaultColor : color;

                        if (UseAlpha == false) UseAlpha = color.A < 1f;

                        return color;
                }

                return default;
            }

            if (UseAlpha == false) UseAlpha = DefaultColor.A < 1f;

            return Mesh;
        }

        public void Apply(MeshInstance3D meshInstance3D)
        {
            if (meshInstance3D.IsNull()) return;

            meshInstance3D.Mesh.Destroy();

            meshInstance3D.Mesh = GetMesh();

            if (UseVertexColor)
            {
                meshInstance3D.MaterialOverride = UseAlpha
                ? Render.VertexMaterialAlpha
                : Render.VertexMaterial;
            }
        }

        public MeshInstance3D Apply(Node parent)
        {
            var meshInstance = new MeshInstance3D();
            parent.AddChild(meshInstance);

            Apply(meshInstance);
            return meshInstance;
        }

        public void AddCounterClockwise(int surfaceIndex, params Vector3[] vertices) => AddCounterClockwise(default, surfaceIndex, vertices);
        public void AddCounterClockwise(Color color, params Vector3[] vertices) => AddCounterClockwise(color, 0, vertices);
        public void AddCounterClockwise(params Vector3[] vertices) => AddCounterClockwise(default, 0, vertices);
        public void AddCounterClockwise(Color color, int surfaceIndex, params Vector3[] vertices)
        {
            if (vertices.IsEmpty()) return;

            Mesh.Destroy();
            Mesh = null;

            switch (Type)
            {
                case MeshType.Triangles:
                    if (vertices.Length < 3) break;

                    var count = Mathf.FloorToInt(vertices.Length / 2f);

                    // Reverse vertices
                    for (int i = 0, k = vertices.Length - 1; i < count; i++, k--)
                    {
                        (vertices[i], vertices[k]) = (vertices[k], vertices[i]);
                    }

                    AddClockwise(color, surfaceIndex, vertices);
                    break;

                default:
                    AddClockwise(color, surfaceIndex, vertices);
                    break;
            }
        }

        public void AddClockwise(int surfaceIndex, params Vector3[] vertices) => AddClockwise(default, surfaceIndex, vertices);
        public void AddClockwise(Color color, params Vector3[] vertices) => AddClockwise(color, 0, vertices);
        public void AddClockwise(params Vector3[] vertices) => AddClockwise(default, 0, vertices);
        public void AddClockwise(Color color, int surfaceIndex, params Vector3[] vertices)
        {
            if (vertices.IsEmpty()) return;

            Mesh.Destroy();
            Mesh = null;

            switch (Type)
            {
                case MeshType.Points:
                    foreach (var point in vertices)
                        Points[point] = color;
                    break;

                case MeshType.Lines:
                    for (int i = 1; i < vertices.Length; i++)
                    {
                        var start = vertices[i - 1];
                        var end = vertices[i];

                        if (LineMap.TryGetValue(start, out var lines1) == false)
                            LineMap[start] = lines1 = new();

                        if (LineMap.TryGetValue(end, out var lines2) == false)
                            LineMap[end] = lines2 = new();

                        var alreadyRegistered = false;

                        foreach (var line in lines1)
                        {
                            var s = Lines[line].Start;
                            var e = Lines[line].End;

                            if (s != start && s != end) continue;
                            if (e != start && e != end) continue;

                            alreadyRegistered = true;
                            break;
                        }

                        if (alreadyRegistered == false)
                        {
                            var index = Lines.Count;
                            Lines.Add(index, new(start, end, color));

                            lines1.Add(index);
                            lines2.Add(index);
                        }
                    }
                    break;

                case MeshType.LineStrip:
                    foreach (var lineVertex in vertices)
                        LineStrips[lineVertex] = color;
                    break;

                case MeshType.Triangles:
                    if (vertices.Length < 3) break;

                    switch (vertices.Length)
                    {
                        case 3:
                            var n = GetFacingDirection(vertices[0], vertices[1], vertices[2]);

                            var a = AddVertex(vertices[0], n, color);
                            var b = AddVertex(vertices[1], n, color);
                            var c = AddVertex(vertices[2], n, color);

                            if (Surfaces.TryGetValue(surfaceIndex, out var surface) == false)
                                Surfaces[surfaceIndex] = surface = new();

                            surface.Add(a.Index, b.Index, c.Index);
                            break;

                        case 4:
                            AddClockwise(color, surfaceIndex, vertices[0], vertices[1], vertices[2]);
                            AddClockwise(color, surfaceIndex, vertices[2], vertices[3], vertices[0]);
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
                                AddClockwise(color, surfaceIndex,
                                    tri[0].toXZ(ydic[tri[0]]),
                                    tri[1].toXZ(ydic[tri[1]]),
                                    tri[2].toXZ(ydic[tri[2]])
                                );
                            }
                            break;
                    }
                    break;

                default: break;
            }
        }

        #region Backend

        public Vertex AddVertex(Vector3 position, Vector3 normal, Color color) => GetVertex(AddVertexIndex(position, normal, color));
        public Vertex GetVertex(int index) => Vertices[index];

        public bool TryGetVertexIndex(Vector3 position, Vector3 normal, out int index)
        {
            if (VerticeMap.TryGetValue(position, out var normals) && normals.TryGetValue(normal, out index))
                return true;

            index = default;
            return false;
        }

        public int AddVertexIndex(Vector3 position, Vector3 normal, Color color = default)
        {
            if (TryGetVertexIndex(position, normal, out var index)) return index;

            if (VerticeMap.TryGetValue(position, out var normals) == false)
                VerticeMap[position] = normals = new();

            var vertex = new Vertex(Vertices.Count, position, normal, color);
            normals[normal] = vertex.Index;
            Vertices.Add(vertex.Index, vertex);

            return vertex.Index;
        }

        #endregion

        #region Utility

        public void AddQuadDir(Vector3 direction, Color color, Vector3 a, Vector3 b)
        {
            var c = b + direction;

            AddCounterClockwise(color, a, a + direction, c);
            AddCounterClockwise(color, c, b, a);
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
                var min = Vertices[0].Position.toXY();
                var max = min;

                for (var i = 0; i < Vertices.Count; i++)
                {
                    var vertex = Vertices[i];

                    min = min.Min(vertex.Position.toXY());
                    max = max.Max(vertex.Position.toXY());
                }

                for (var i = 0; i < Vertices.Count; i++)
                {
                    var vertex = Vertices[i];

                    vertex.SetUV((vertex.Position.toXY() - min) / (max - min) * multiplier);
                }
            }

            else
            {
                for (var i = 0; i < Vertices.Count; i++)
                {
                    Vertices[i].SetUV(Vertices[i].Position.toXY() * multiplier);
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

            public void Commit(SurfaceTool surfaceTool, MeshBuilder builder, ArrayMesh mesh, ref bool useAlpha)
            {
                surfaceTool.Begin((Mesh.PrimitiveType)(byte)builder.Type);

                var indexes = new Dictionary<int, int>();

                foreach (var triangle in Triangles.Values)
                {
                    addVertex(builder.Vertices[triangle.X], ref useAlpha);
                    addVertex(builder.Vertices[triangle.Y], ref useAlpha);
                    addVertex(builder.Vertices[triangle.Z], ref useAlpha);

                    void addVertex(Vertex vertex, ref bool useAlpha)
                    {
                        if (indexes.ContainsKey(vertex.Index)) return;

                        indexes[vertex.Index] = indexes.Count;

                        if (builder.UseVertexColor)
                        {
                            var color = vertex.GetColor(builder);
                            surfaceTool.SetColor(color);

                            if (useAlpha == false) useAlpha = color.A < 1f;
                        }

                        else
                        {
                            surfaceTool.SetNormal(vertex.Normal);
                            surfaceTool.SetUV(vertex.GetUV());
                        }

                        surfaceTool.AddVertex(vertex.Position + builder.Offset);
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
                throw new System.ArgumentException($"[{nameof(MeshBuilder)}] A polygon must have at least 3 vertices. ({vertices?.Length ?? 0})");

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
                Debug.LogError($"[{nameof(MeshBuilder)}] At least 3 points are required to form a shape.");
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
                    Debug.LogError($"[{nameof(MeshBuilder)}] Failed to find an ear. The shape might be malformed or degenerate.");
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
            public int Index { get; set; }

            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }

            public Color[] Colors { get; set; }

            public Vertex(int index, Vector3 position, Vector3 normal, Color color = default, Vector2 uv = default, Vector2 uv2 = default) : this()
            {
                Position = position;
                Normal = normal;
                Index = index;

                UVs[0] = uv;
                UVs[1] = uv2;

                if (color != default) Colors = new[] { color };
            }

            public Vertex()
            {
                UVs = new Vector2[2];
                Index = default;

                Position = Normal = default;
                Colors = null;
            }

            public readonly void SetUV(Vector2 uv) => UVs[0] = uv;
            public readonly Vector2 GetUV() => UVs[0];

            public readonly void SetUV2(Vector2 uv2) => UVs[1] = uv2;
            public readonly Vector2 GetUV2() => UVs[1];

            public readonly Color GetColor(MeshBuilder source) => Colors.NotEmpty() ? Colors[0] : source.DefaultColor;
        }

        #endregion
    }

    public enum MeshType : byte
    {
        //
        // Summary:
        //     Render array as points (one vertex equals one point).
        Points = 0,

        //
        // Summary:
        //     Render array as lines (every two vertices a line is created).
        Lines = 1,

        //
        // Summary:
        //     Render array as line strip.
        LineStrip = 2,

        //
        // Summary:
        //     Render array as triangles (every three vertices a triangle is created).
        Triangles = 3,

        //
        // Summary:
        //     Render array as triangle strips.
        TriangleStrip = 4
    }
}