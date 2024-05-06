using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class CustomTranslationEntry : Resource
    {
        [Export] public InputCode Input { get; set; } = InputCode.LeftStickRight;
        [Export] public JoyButton GamepadButton { get; set; } = JoyButton.Invalid;
        [Export] public JoyAxis GamepadAxis { get; set; } = JoyAxis.RightX;
        [Export] public string[] NativeGodot { get; set; } = new string[2] { "move_right", "move_left" };
    }
}