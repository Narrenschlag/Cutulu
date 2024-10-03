using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Highly GPU optimized mesh generation tool for fast and easy use. Allows for fast polygon and wall meshes to be added.
    /// </summary>
    public class MeshBuilder
    {
        #region Core

        public readonly MeshType Type;

        public Color BaseColor { get; set; }
        public Vector3 Offset { get; set; }

        private readonly Dictionary<Vector3, Dictionary<Vector3, int>> PositionNormals = new();
        private readonly List<Triangle> Triangles = new();
        private readonly List<Vertex> Vertices = new();

        private bool useAlpha;
        private int lastIdx;

        public MeshBuilder(MeshType type) : this(type, Colors.White) { }
        public MeshBuilder(MeshType type, Color baseColor)
        {
            BaseColor = baseColor;
            Type = type;

            Clear();
        }

        public Vertex GetVertex(Vector3 position, Vector3 normal) => GetVertex(GetVertexIdx(position, normal));
        public Vertex GetVertex(int idx) => Vertices[idx];

        public int GetVertexIdx(Vertex vertex) => GetVertexIdx(vertex.Position, vertex.Normal);
        public int GetVertexIdx(Vector3 position, Vector3 normal)
        {
            if (PositionNormals.TryGetValue(position, out var normals))
            {
                if (normals.TryGetValue(normal, out var vertex))
                {
                    return vertex;
                }

                normals[normal] = Vertices.Count;
                Vertices.Add(new() { Position = position, Normal = normal });

                return Vertices.Count - 1;
            }

            else
            {
                PositionNormals[position] = new() { { normal, Vertices.Count } };
                Vertices.Add(new() { Position = position, Normal = normal });

                return Vertices.Count - 1;
            }
        }

        public int AddTriangle(int a, int b, int c, bool clockwise = true)
        {
            return AddTriangle(GetVertex(a), GetVertex(b), GetVertex(c), clockwise);
        }

        public int AddTriangle(Vertex a, Vertex b, Vertex c, bool clockwise = true)
        {
            var idx = Triangles.Count;

            var triangle = new Triangle()
            {
                A = GetVertexIdx(a),
                B = GetVertexIdx(b),
                C = GetVertexIdx(c),
                Clockwise = clockwise,
            };

            Triangles.Add(triangle);
            a.Triangles.Add(idx);
            b.Triangles.Add(idx);
            c.Triangles.Add(idx);

            return idx;
        }

        public void Clear()
        {
            Offset = default;

            useAlpha = false;
            lastIdx = 0;

            PositionNormals.Clear();
            Triangles.Clear();
            Vertices.Clear();
        }

        public void Apply(MeshInstance3D meshInstance3D, bool useVertexColor = false)
        {
            if (meshInstance3D.IsNull()) return;

            if (meshInstance3D.Mesh.NotNull())
                meshInstance3D.Mesh.Dispose();

            var surfaceTool = new SurfaceTool();
            surfaceTool.Begin((Mesh.PrimitiveType)(int)Type);

            foreach (var vertex in Vertices)
            {
                surfaceTool.SetNormal(vertex.Normal);
                surfaceTool.SetColor(vertex.Color);
                surfaceTool.SetUV(vertex.UV);

                surfaceTool.AddVertex(vertex.Position + Offset);
            }

            if (Type == MeshType.Triangles)
            {
                foreach (var triangle in Triangles)
                {
                    surfaceTool.AddIndex(triangle.A);
                    surfaceTool.AddIndex(triangle.B);
                    surfaceTool.AddIndex(triangle.C);
                }
            }

            meshInstance3D.Mesh = surfaceTool.Commit();

            if (useVertexColor)
                meshInstance3D.MaterialOverride = useAlpha ?
                Renderf.VertexMaterialAlpha : Renderf.VertexMaterial;
        }

        #endregion

        #region Extra

        public void AddQuad(params Vector3[] vertices) => AddQuad(false, vertices);
        public void AddQuad(bool flip, params Vector3[] vertices) => AddQuad(BaseColor, flip, vertices);
        public void AddQuad(Color color, bool flip, params Vector3[] vertices)
        {
            if (vertices.Size() < 4) return;

            var normal = GetFacingDirection(vertices);

            var a = GetVertex(vertices[0], normal);
            var b = GetVertex(vertices[flip ? 3 : 1], normal);
            var c = GetVertex(vertices[2], normal);
            var d = GetVertex(vertices[flip ? 1 : 3], normal);

            a.Color = b.Color = c.Color = d.Color = color;

            AddTriangle(a, b, c);
            AddTriangle(c, d, a);
        }

        public void AddQuadDir(Vector3 direction, params Vector3[] vertices) => AddQuadDir(direction, false, vertices);
        public void AddQuadDir(Vector3 direction, bool flip, params Vector3[] vertices) => AddQuadDir(direction, BaseColor, flip, vertices);
        public void AddQuadDir(Vector3 direction, Color color, bool flip, params Vector3[] vertices)
        {
            if (vertices.Size() < 2) return;

            var a = vertices[0];
            var b = vertices[1];

            vertices = flip ?
                new[] { a, a + direction, b + direction, b } :
                new[] { a, b, b + direction, a + direction };

            AddQuad(color, false, vertices);
        }

        public void AddPolygon(params Vector3[] vertices) => AddPolygon(false, vertices);
        public void AddPolygon(bool flip, params Vector3[] vertices) => AddPolygon(BaseColor, flip, vertices);
        public void AddPolygon(Color color, bool flip, params Vector3[] vertices)
        {
            var indicies = vertices.Triangulate().ToArray();
            var normal = GetFacingDirection(vertices);
            var buffer = new List<int>();

            for (var i = 0; i < indicies.Length; i++)
            {
                var idx = GetVertexIdx(vertices[indicies[i]], normal);

                var vertex = GetVertex(idx);
                vertex.Color = color;

                buffer.Add(idx);

                if (buffer.Count >= 3)
                {
                    AddTriangle(buffer[0], buffer[1], buffer[2], !flip);
                    buffer.Clear();
                }
            }
        }

        public void AddWalls(Vector3 direction, params Vector3[] vertices) => AddWalls(direction, BaseColor, vertices);
        public void AddWalls(Vector3 direction, Color color, params Vector3[] vertices)
        {
            for (var i = 0; i < vertices.Length; i++)
            {
                var k = (i + 1) % vertices.Length;

                AddQuadDir(direction, color, false, vertices[i], vertices[k]);
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

        #endregion

        #region UV Mapping

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

                    vertex.UV = (vertex.Position.toXY() - min) / (max - min) * multiplier;
                }
            }

            else
            {
                for (var i = 0; i < Vertices.Count; i++)
                {
                    Vertices[i].UV = Vertices[i].Position.toXY() * multiplier;
                }
            }
        }

        #endregion

        public class Vertex
        {
            public readonly List<int> Triangles = new();

            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 UV2;
            public Vector2 UV;

            public Color Color;
        }

        public class Triangle
        {
            public bool Clockwise = true;
            public int A, B, C;
        }
    }

    public enum MeshType
    {
        Points,
        Lines, LineFan,
        Triangles, TriangleFan,
    }
}