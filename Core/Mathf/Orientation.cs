#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using Godot;

    public partial class Orientation
    {
        public Vector3 NormalizedForward { get; set; }
        public Vector3 NormalizedRight { get; set; }
        public Vector3 NormUp { get; set; }

        public Vector3 Forward { get; set; }
        public Vector3 Right { get; set; }
        public Vector3 Up { get; set; }

        public float Scale { get; set; }

        public Orientation(Node3D node, float scale = 1f) : this(node.Forward(), scale) { }

        public Orientation(Vector3 forward, float scale = 1f)
        {
            Scale = scale;

            NormalizedForward = forward.Normalized();
            NormUp = NormalizedForward.toUp();
            NormalizedRight = NormalizedForward.toRight(NormUp);

            Forward = NormalizedForward * Scale;
            Right = NormalizedRight * Scale;
            Up = NormUp * Scale;
        }
    }
}
#endif