namespace Cutulu
{
    using Godot;

    public partial class Orientation
    {
        public Vector3 Forward { get; set; }
        public Vector3 Right { get; set; }

        public Orientation(Node3D node) : this(node.Forward(), node.Right()) { }

        public Orientation(Vector3 forward, Vector3 right)
        {
            Forward = forward;
            Right = right;
        }
    }
}