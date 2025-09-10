#if GODOT4_0_OR_GREATER
namespace Cutulu.Prototyping;

using Cutulu.Core;
using Godot;

using INP = Godot.Input;

public partial class FreeCamera : Camera3D
{
    [Export] private Vector2 RotationModifier { get; set; } = new(-1.0f, -1.0f);
    [Export] private float RotationSpeed { get; set; } = 0.002f;

    [Export] private Vector3 MoveModifier { get; set; } = new(1.0f, -1.0f, 1.0f);
    [Export] private float MoveSpeed { get; set; } = 50.0f;

    private float RotationX { get; set; }
    private float RotationY { get; set; }

    private Vector2 GetMouseVelocity() => INP.GetLastMouseVelocity() * RotationModifier * RotationSpeed;

    private Vector3 GetMoveVelocity()
    {
        var input = new Vector3(
            INP.GetActionStrength("ui_right") - INP.GetActionStrength("ui_left"),
            INP.GetActionStrength("ui_page_up") - INP.GetActionStrength("ui_page_down"),
            INP.GetActionStrength("ui_up") - INP.GetActionStrength("ui_down")
        ) * MoveModifier;

        return (
            this.Right() * input.X +
            this.Up() * input.Y +
            this.Forward() * input.Z
        ).Normalized() * MoveSpeed;
    }

    public override void _Ready()
    {
        INP.MouseMode = INP.MouseModeEnum.Captured;

        RotationX = GlobalRotation.X;
        RotationY = GlobalRotation.Y;
    }

    public override void _Process(double delta)
    {
        GlobalPosition += GetMoveVelocity() * (float)delta;

        var rotation = GetMouseVelocity() * (float)delta;
        RotationX = Mathf.Clamp(RotationX + rotation.Y, -Mathf.Pi * 0.5f, Mathf.Pi * 0.5f);
        RotationY += rotation.X;

        GlobalRotation = new Vector3(RotationX, RotationY, 0);
    }
}
#endif