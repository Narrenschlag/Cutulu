#if GODOT4_0_OR_GREATER
namespace Cutulu.Core;

using Godot;

public readonly struct Edge
{
    public readonly Vector2 A, B;

    public Edge(Vector2 a, Vector2 b) { A = a; B = b; }

    // Edges are equal regardless of direction.
    public bool Equals(Edge other)
        => (A == other.A && B == other.B) || (A == other.B && B == other.A);
}
#endif