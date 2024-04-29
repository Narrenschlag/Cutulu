using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class InputSet : Resource
    {
        [Export] public string InputName { get; set; } = "action_id";
        [Export] public InputCode[] GamepadKeys { get; set; } = new InputCode[1] { InputCode.MoveRight };
        [Export] public string[] GodotInputs { get; set; } = new string[2] { "move_right", "move_left" };

        public bool IsPressed(InputDevice device, float threshold = 0.5f) => Mathf.Abs(Value(device)) >= threshold;
        public float Value(InputDevice device)
        {
            var code = InputCode.Invalid;

            if (GamepadKeys != null)
            {
                for (int i = 0; i < GamepadKeys.Length; i++)
                {
                    code |= GamepadKeys[i];
                }
            }

            return device.GetValue(code, GodotInputs);
        }
    }
}