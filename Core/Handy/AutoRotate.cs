#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using Godot;

    public partial class AutoRotate : Node3D
    {
        [Export] public Vector3 Speed;

        public override void _Process(double delta)
        {
            RotationDegrees += (float)delta * Speed;
        }
    }
}
#endif