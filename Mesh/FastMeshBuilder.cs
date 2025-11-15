namespace Cutulu.Mesh;

using System.Collections.Generic;
using Cutulu.Core;
using Godot;

/// <summary>
/// Ultra-fast mesh builder with optional normal smoothing. Written by Maximilian Schecklmann 06.11.2025.
/// </summary>
public class FastMeshBuilder
{
    private readonly Dictionary<int, int> PositionToIndex; // Use int hash instead of Vector3 key for much faster performance

    public bool SmoothNormals { get; set; }

    private SwapbackArray<Vector3> Vertices;
    private SwapbackArray<Vector3> Normals;
    private SwapbackArray<Color> Colors;
    private SwapbackArray<int> Indices;

    /// <summary>
    /// Smooth normals is a bit slower, but it's worth it for better visuals. For larger meshes, it's recommended to enable it as smoothNormals merges vertices.
    /// </summary>
    public FastMeshBuilder(int estimatedTriangles = 16384, bool smoothNormals = true)
    {
        var capacity = estimatedTriangles * 3;
        Vertices = new(capacity);
        Normals = new(capacity);
        Indices = new(capacity);
        Colors = new(capacity);

        SmoothNormals = smoothNormals;
        if (smoothNormals) PositionToIndex = new(capacity);
    }

    // Used instead of classic dictionary for faster lookups
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static int HashPosition(Vector3 v)
    {
        // Spatial hash - quantize to grid and hash to int
        // Multiply by large primes to spread values
        const float quantize = 10000f; // Adjust based on your scale
        int x = (int)(v.X * quantize);
        int y = (int)(v.Y * quantize);
        int z = (int)(v.Z * quantize);

        return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private int GetOrAddVertexSmooth(Vector3 position, Vector3 normal)
    {
        int hash = HashPosition(position);

        if (PositionToIndex.TryGetValue(hash, out int index))
        {
            if (Vertices[index].DistanceSquaredTo(position) < 0.0001f)
            {
                ref var existingNormal = ref Normals[index];
                existingNormal = (existingNormal + normal).Normalized();
                return index;
            }
        }

        index = Vertices.Count;
        PositionToIndex[hash] = index;
        Vertices.Add(position);
        Normals.Add(normal);
        return index;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private int GetOrAddVertexSmooth(Vector3 position, Vector3 normal, Color vertexColor)
    {
        int hash = HashPosition(position);

        if (PositionToIndex.TryGetValue(hash, out int index))
        {
            if (Vertices[index].DistanceSquaredTo(position) < 0.0001f)
            {
                ref var existingNormal = ref Normals[index];
                existingNormal = (existingNormal + normal).Normalized();
                return index;
            }
        }

        index = Vertices.Count;
        PositionToIndex[hash] = index;
        Colors.Add(vertexColor);
        Vertices.Add(position);
        Normals.Add(normal);
        return index;
    }

    public void Clear()
    {
        Vertices = new(Vertices.Capacity);
        Normals = new(Normals.Capacity);
        Indices = new(Indices.Capacity);
        Colors = new(Colors.Capacity);
        PositionToIndex?.Clear();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 normal)
    {
        if (SmoothNormals)
        {
            Indices.Add(GetOrAddVertexSmooth(v0, normal));
            Indices.Add(GetOrAddVertexSmooth(v1, normal));
            Indices.Add(GetOrAddVertexSmooth(v2, normal));
        }
        else
        {
            int baseIndex = Vertices.Count;

            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);

            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);

            Indices.Add(baseIndex);
            Indices.Add(baseIndex + 1);
            Indices.Add(baseIndex + 2);
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 normal, Color vertexColor)
    {
        if (SmoothNormals)
        {
            Indices.Add(GetOrAddVertexSmooth(v0, normal, vertexColor));
            Indices.Add(GetOrAddVertexSmooth(v1, normal, vertexColor));
            Indices.Add(GetOrAddVertexSmooth(v2, normal, vertexColor));
        }
        else
        {
            int baseIndex = Vertices.Count;

            Colors.Add(vertexColor);
            Colors.Add(vertexColor);
            Colors.Add(vertexColor);

            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);

            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);

            Indices.Add(baseIndex);
            Indices.Add(baseIndex + 1);
            Indices.Add(baseIndex + 2);
        }
    }

    public ArrayMesh BuildMesh()
    {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        arrays[(int)Mesh.ArrayType.Vertex] = Vertices.AsSpan().ToArray();
        if (Colors.Count == Vertices.Count) arrays[(int)Mesh.ArrayType.Color] = Colors.AsSpan().ToArray();
        else Debug.Log($"Colors count mismatch: {Colors.Count} != {Vertices.Count}");

        arrays[(int)Mesh.ArrayType.Normal] = Normals.AsSpan().ToArray();
        arrays[(int)Mesh.ArrayType.Index] = Indices.AsSpan().ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        return mesh;
    }

    public void Apply(MeshInstance3D meshInstance)
    {
        Apply(meshInstance, BuildMesh());
    }

    public static void Apply(MeshInstance3D meshInstance, ArrayMesh arrayMesh)
    {
        meshInstance.Mesh?.Dispose();
        meshInstance.Mesh = arrayMesh;
    }
}