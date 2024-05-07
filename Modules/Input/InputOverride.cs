using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class InputOverride : Resource
    {
        [Export] public InputCode Input { get; set; } = InputCode.RStickEast;

        [Export] public JoyButton GamepadButton { get; set; } = JoyButton.Invalid;
        [Export] public JoyAxis GamepadAxis { get; set; } = JoyAxis.RightX;
        [Export] public string NativeGodot { get; set; } = "move_right";
    }
}