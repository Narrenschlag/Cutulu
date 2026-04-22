namespace Cutulu.Core;

using Godot;

public readonly struct Triangle
{
    public readonly Vector2 A, B, C;

    // Circumcircle cache
    private readonly Vector2 _circumCenter;
    private readonly float _circumRadiusSq;

    public Triangle(Vector2 a, Vector2 b, Vector2 c)
    {
        A = a; B = b; C = c;
        (_circumCenter, _circumRadiusSq) = ComputeCircumcircle(a, b, c);
    }

    public Edge[] Edges =>
    [
        new(A, B),
            new(B, C),
            new(C, A)
    ];

    public bool HasEdge(Edge e)
        => Edges[0].Equals(e) || Edges[1].Equals(e) || Edges[2].Equals(e);

    /// <summary>True if <paramref name="p"/> is inside or on the circumcircle.</summary>
    public bool CircumcircleContains(Vector2 p)
        => (p - _circumCenter).LengthSquared() <= _circumRadiusSq + Mathf.Epsilon;

    public bool SharesVertexWith(Triangle other)
        => A == other.A || A == other.B || A == other.C
        || B == other.A || B == other.B || B == other.C
        || C == other.A || C == other.B || C == other.C;

    public bool Equals(Triangle other)
        => A == other.A && B == other.B && C == other.C;

    private static (Vector2 center, float radiusSq) ComputeCircumcircle(Vector2 a, Vector2 b, Vector2 c)
    {
        // Standard circumcenter formula via perpendicular bisectors.
        var ax = b.X - a.X;
        var ay = b.Y - a.Y;
        var bx = c.X - a.X;
        var by = c.Y - a.Y;

        var D = 2f * (ax * by - ay * bx);

        if (Mathf.Abs(D) < Mathf.Epsilon)
        {
            // Degenerate (collinear points) — return a point far away so it
            // never falsely contains anything.
            return (new Vector2(float.MaxValue, float.MaxValue), 0f);
        }

        var ux = (by * (ax * ax + ay * ay) - ay * (bx * bx + by * by)) / D;
        var uy = (ax * (bx * bx + by * by) - bx * (ax * ax + ay * ay)) / D;

        var center = new Vector2(a.X + ux, a.Y + uy);
        var radiusSq = (center - a).LengthSquared();

        return (center, radiusSq);
    }

    public static implicit operator Triangle(Vector2[] vertices)
        => vertices.NotEmpty() ? new(vertices[0], vertices[1], vertices[2]) : default;

    public static implicit operator Triangle((Vector2 a, Vector2 b, Vector2 c) vertices)
    => new(vertices.a, vertices.b, vertices.c);

    public static implicit operator Vector2[](Triangle t)
    => [t.A, t.B, t.C];

    public static Triangle[] ToTriangles(Vector2[][] tris)
    {
        if (tris == null || tris.Length == 0)
            return [];

        var result = new Triangle[tris.Length];
        for (var i = 0; i < tris.Length; i++)
            result[i] = tris[i];

        return result;
    }

    public static Vector2[][] ToArray(Triangle[] tris)
    {
        if (tris == null || tris.Length == 0)
            return [];

        var result = new Vector2[tris.Length][];
        for (var i = 0; i < tris.Length; i++)
            result[i] = tris[i];

        return result;
    }
}