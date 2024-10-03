using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Highly GPU optimized mesh generation tool for fast and easy use. Allows for fast polygon and wall meshes to be added.
    /// </summary>
    public class DynamicMesh
    {
        #region Core

        public SurfaceTool Gen { get; private set; }
        public readonly MeshType Type;

        public Color BaseColor { get; set; }
        public Vector3 Offset { get; set; }

        private Dictionary<Vector3, Dictionary<Vector3, int>> Vertices;
        private bool useAlpha;
        private int lastIdx;

        public DynamicMesh(MeshType type) : this(type, Colors.White) { }
        public DynamicMesh(MeshType type, Color baseColor)
        {
            BaseColor = baseColor;
            Type = type;

            Clear();
        }

        public void Add(Vector3 normal, params Vector3[] vertices) => Add(normal, BaseColor, vertices);
        public void Add(Vector3 normal, Color color, params Vector3[] vertices)
        {
            if (vertices.IsEmpty()) return;

            for (int i = 0; i < vertices.Length; i++)
            {
                AddIndex(vertices[i] + Offset, normal, out bool added);
                if (added) AddVertex(vertices[i] + Offset, normal, color);
            }
        }

        private void AddVertex(Vector3 vertex, Vector3 normal, Color color, Vector2 uv = default)
        {
            if (color.A < 1f) useAlpha = true;

            Gen.SetNormal(normal);
            Gen.SetColor(color);
            Gen.SetUV(uv);

            Gen.AddVertex(vertex);
        }

        private void AddIndex(Vector3 vertex, Vector3 normal, out bool added) => Gen.AddIndex(GetIndex(vertex, normal, out added));
        private int GetIndex(Vector3 vertex, Vector3 normal, out bool added)
        {
            if (Vertices.TryGetValue(vertex, out var normals))
            {
                if (normals.TryGetValue(normal, out var idx))
                {
                    added = false;
                    return idx;
                }

                else
                {
                    normals.Add(normal, lastIdx);
                }
            }

            else
            {
                Vertices.Add(vertex, new() { { normal, lastIdx } });
            }

            added = true;
            return lastIdx++;
        }

        public void Apply(MeshInstance3D meshInstance3D, bool useVertexColor = false)
        {
            if (meshInstance3D.IsNull()) return;

            if (meshInstance3D.Mesh.NotNull())
                meshInstance3D.Mesh.Dispose();

            meshInstance3D.Mesh = Gen.Commit();

            if (useVertexColor)
                meshInstance3D.MaterialOverride = useAlpha ?
                Renderf.VertexMaterialAlpha : Renderf.VertexMaterial;
        }

        public void Clear()
        {
            Offset = default;

            useAlpha = false;
            lastIdx = 0;

            if (Vertices == null) Vertices = new();
            else Vertices.Clear();

            if (Gen.NotNull())
            {
                Gen.Clear();
            }

            else
            {
                Gen = new();
                Gen.Begin((Mesh.PrimitiveType)(int)Type);
            }
        }

        #endregion

        #region Extra

        public void AddQuad(params Vector3[] vertices) => AddQuad(false, vertices);
        public void AddQuad(bool flip, params Vector3[] vertices) => AddQuad(BaseColor, flip, vertices);
        public void AddQuad(Color color, bool flip, params Vector3[] vertices)
        {
            if (vertices.Size() < 4) return;

            var normal = GetFacingDirection(vertices);

            var t1 = new Vector3[] { vertices[0], vertices[flip ? 3 : 1], vertices[2] };
            var t2 = new Vector3[] { vertices[2], vertices[flip ? 1 : 3], vertices[0] };

            Add(normal, color, t1);
            Add(normal, color, t2);
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

            if (flip)
            {
                for (var i = indicies.Length - 1; i >= 0; i--)
                {
                    Add(normal, color, vertices[indicies[i]]);
                }
            }

            else
            {
                for (var i = 0; i < indicies.Length; i++)
                {
                    Add(normal, color, vertices[indicies[i]]);
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

        private static Vector3 GetFacingDirection(Vector3[] points)
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
    }
}