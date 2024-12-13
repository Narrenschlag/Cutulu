namespace Cutulu
{
    using Godot;

    public partial class Orientation
    {
        public Vector3 Forward { get; set; }
        public Vector3 Right { get; set; }

        public Orientation(Node3D node, float scale = 1f) : this(node.Forward(), node.Right(), scale) { }

        public Orientation(Vector3 forward, Vector3 right, float scale = 1f)
        {
            Forward = forward.Normalized() * scale;
            Right = right.Normalized() * scale;
        }
    }
}