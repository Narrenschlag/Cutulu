using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class CinemaPerspective : Resource
    {
        [Export] public bool UsePhysicsUpdate;

        [ExportGroup("Position")]
        [Export] public Vector3 GlobalOffset;
        [Export] public Vector3 LocalOffset;
        [Export] public float LerpSpeed;
        [Export] public bool LerpPosition;

        [ExportGroup("Rotation Left-Right")]
        [Export] public bool TranslateToInstant;
        [Export] public float RotationSpeedY;
        [Export] public float CameraAngle;

        [ExportGroup("Rotation Up-Down")]
        [Export] public float RotationSpeedX;
        [Export] public float MinAngleX;
        [Export] public float MaxAngleX;
    }
}