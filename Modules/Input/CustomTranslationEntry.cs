using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class CustomTranslationEntry : Resource
    {
        [Export] public InputCode Input { get; set; }
        [Export] public JoyButton GamepadButton { get; set; }
        [Export] public JoyAxis GamepadAxis { get; set; }
        [Export] public string[] NativeGodot { get; set; }
    }
}