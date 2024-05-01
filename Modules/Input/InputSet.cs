using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class InputSet : Resource
    {
        [Export] public string InputName { get; set; } = "action_id";
        [Export] public InputCode GamepadKey { get; set; } = InputCode.MoveRight;
        [Export] public string[] GodotInputs { get; set; } = new string[2] { "move_right", "move_left" };

        public bool IsPressed(InputDevice device, float threshold = 0.5f) => Mathf.Abs(Value(device)) >= threshold;
        public float Value(InputDevice device)
        {
            return device.GetValue(GamepadKey, GodotInputs);
        }
    }
}