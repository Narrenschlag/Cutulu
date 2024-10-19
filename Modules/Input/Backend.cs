namespace Cutulu.Input
{
    using System.Collections.Generic;
    using Godot;

    public static class Backend
    {
        #region XInput to XInputType

        public static InputTypeEnum GetType(this InputEnum input) => GetType(ref input);
        public static InputTypeEnum GetType(ref InputEnum input)
        {
            // Valid input
            if (input >= InputEnum.Axis0)
            {
                // Axis Button
                if (input <= InputEnum.Axis5) return InputTypeEnum.Axis;

                // Axis Button
                else if (input >= InputEnum.AxisButton0 && input <= InputEnum.AxisButton11) return InputTypeEnum.AxisButton;

                // Button
                else if (input >= InputEnum.Button0 && input <= InputEnum.Button21) return InputTypeEnum.Button;

                // Mouse
                else if (input >= InputEnum.MouseMin && input <= InputEnum.MouseMax) return InputTypeEnum.Mouse;

                // Key
                else if (input >= InputEnum.KeyMin && input <= InputEnum.KeyMax) return InputTypeEnum.Key;
            }

            // Invalid
            return InputTypeEnum.Invalid;
        }

        #endregion

        #region Input to XInput

        public static InputEnum GetInput(this Key key) => (InputEnum)((int)key - (int)InputEnum.KeyOffset);
        public static InputEnum GetInput(this JoyAxis axis) => (InputEnum)(int)axis - (int)JoyAxis.LeftX;
        public static InputEnum GetInput(this JoyButton button) => (InputEnum)(int)button - (int)InputEnum.ButtonOffset;
        public static InputEnum GetInput(this MouseButton mouseButton) => (InputEnum)(int)mouseButton - (int)InputEnum.MouseOffset;
        public static InputEnum GetInput(this JoyAxis axis, bool readNegative) => (InputEnum)(2 * ((int)axis - (int)JoyAxis.LeftX) + (readNegative ? 1 : 0)) - (int)InputEnum.AxisButtonOffset;

        #endregion

        #region Get Range

        public static List<InputEnum> GetRange(params InputTypeEnum[] types)
        {
            if (types?.Length < 1) return null;

            List<InputEnum> list = null;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] != InputTypeEnum.Invalid) list ??= new();

                switch (types[i])
                {
                    case InputTypeEnum.Axis:
                        for (InputEnum k = InputEnum.Axis0; k <= InputEnum.Axis5; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case InputTypeEnum.AxisButton:
                        for (InputEnum k = InputEnum.AxisButton0; k <= InputEnum.AxisButton11; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case InputTypeEnum.Button:
                        for (InputEnum k = InputEnum.Button0; k <= InputEnum.Button21; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case InputTypeEnum.Mouse:
                        for (InputEnum k = InputEnum.MouseMin; k <= InputEnum.MouseMax; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case InputTypeEnum.Key:
                        for (Key k = Key.Space; k <= Key.Quoteleft; k++)
                        {
                            list.Add(GetInput(k));
                        }

                        for (Key k = Key.Braceleft; k <= Key.Asciitilde; k++)
                        {
                            list.Add(GetInput(k));
                        }

                        for (Key k = Key.Yen; k <= Key.Section; k++)
                        {
                            list.Add(GetInput(k));
                        }

                        for (Key k = Key.Special; k <= Key.Kp9; k++)
                        {
                            list.Add(GetInput(k));
                        }
                        break;

                    default: break;
                }
            }

            return list;
        }

        #endregion

        #region IsPressed

        public static float ButtonPressThreshold { get; set; } = 0.50f;

        public static bool IsPressed(this InputEnum input, int deviceId) => IsPressed(ref deviceId, ref input);
        public static bool IsPressed(this int deviceId, InputEnum input) => IsPressed(ref deviceId, ref input);
        public static bool IsPressed(ref int deviceId, ref InputEnum input)
        {
            // Handle value
            switch (GetType(ref input))
            {
                // IO result for buttons
                case InputTypeEnum.Button: return Godot.Input.IsJoyButtonPressed(deviceId, (JoyButton)(input + (int)InputEnum.ButtonOffset));

                // IO result for axis values
                case InputTypeEnum.AxisButton:
                    var i = (InputEnum)Mathf.FloorToInt(((int)input + (int)InputEnum.AxisButtonOffset) / 2f); // Get the axis

                    return (int)input % 2 == 0 ?
                    GetValue(ref deviceId, ref i) >= ButtonPressThreshold : // Upward on Axis
                    GetValue(ref deviceId, ref i) <= -ButtonPressThreshold; // Downward on Axis

                // Float result for axis
                case InputTypeEnum.Axis: return Mathf.Abs(GetValue(ref deviceId, ref input)) >= ButtonPressThreshold; // Return -1 to 1

                // IO result for mouse buttons
                case InputTypeEnum.Mouse: return Godot.Input.IsMouseButtonPressed((MouseButton)(input + (int)InputEnum.MouseOffset));

                // IO result for keys
                case InputTypeEnum.Key: return Godot.Input.IsKeyPressed((Key)(input + (int)InputEnum.KeyOffset));

                // Invalid input
                default:
                    Debug.LogError($"Invalid xInput.");
                    return false;
            }
        }

        #endregion

        #region GetValue

        public static float GetValue(this InputEnum input, int deviceId) => GetValue(ref deviceId, ref input);
        public static float GetValue(this int deviceId, InputEnum input) => GetValue(ref deviceId, ref input);
        public static float GetValue(ref int deviceId, ref InputEnum input)
        {
            // Handle value
            switch (GetType(ref input))
            {
                case InputTypeEnum.Button: return IsPressed(ref deviceId, ref input) ? 1f : 0f;
                case InputTypeEnum.Axis: return Godot.Input.GetJoyAxis(deviceId, (JoyAxis)input);

                case InputTypeEnum.Mouse: return IsPressed(ref deviceId, ref input) ? 1f : 0f;
                case InputTypeEnum.Key: return IsPressed(ref deviceId, ref input) ? 1f : 0f;

                case InputTypeEnum.AxisButton:
                    var threshold = ButtonPressThreshold;
                    ButtonPressThreshold = 0.08f;

                    var result = IsPressed(ref deviceId, ref input) ? Mathf.Abs(GetValue(deviceId, ((input - InputEnum.AxisButton0) / 2) + InputEnum.Axis0) - ButtonPressThreshold) / (1f - ButtonPressThreshold) : 0f;
                    ButtonPressThreshold = threshold;

                    return result;

                default:
                    Debug.LogError($"Invalid Input.");
                    return default;
            }
        }

        #endregion

        public static bool GetKey(this string name, float threshhold = .5f) => GetValue(name) >= threshhold;
        public static float GetValue(this string name) => Godot.Input.GetActionRawStrength(name);

        public static Vector2 MousePosition(this Node node, bool clampToScreen = false)
        {
            var screen = node.GetViewport().GetMousePosition();

            if (clampToScreen)
                screen = screen.Clamp(Vector2.Zero, DisplayServer.WindowGetSize());

            return screen;
        }

        public static bool GetMousePosition(this Camera3D camera, out Vector3 globalPosition, uint mask = 4294967295)
        {
            if (RaycastFromCamera(camera, out RaycastHit hit, mask))
            {
                globalPosition = hit.point;
                return true;
            }

            globalPosition = Vector3.Zero;
            return false;
        }

        public static void GetRayAt(this Camera3D camera, Vector2 screenPosition, out Vector3 origin, out Vector3 direction)
        {
            direction = camera.ProjectRayNormal(screenPosition);
            origin = camera.ProjectRayOrigin(screenPosition);
        }

        public static Vector3 GetHit(this Camera3D camera, float y = 0) => GetHit(camera, MousePosition(camera), y);
        public static Vector3 GetHit(this Camera3D camera, Vector2 screenPosition, float y = 0)
        {
            GetRayAt(camera, screenPosition, out var origin, out var direction);
            return Trianglef.RayToY(origin, direction, y);
        }

        public static bool RaycastFromCamera(this Camera3D camera, out RaycastHit hit, uint mask = 4294967295)
        => Physics.Raycast(camera, out hit, camera.Far, mask);

        public static bool Down(this string name, ref bool valueStore, float threshold = .5f)
        {
            bool old = valueStore;

            valueStore = name.GetKey(threshold);
            return !old && valueStore;
        }

        public static bool Up(this string name, ref bool valueStore, float threshold = .5f)
        {
            bool old = valueStore;

            valueStore = name.GetKey(threshold);
            return old && !valueStore;
        }

        private static bool capturedMouse;
        public static void CaptureMouse() => CaptureMouse(!capturedMouse);
        public static void CaptureMouse(bool value)
        {
            capturedMouse = value;

            Godot.Input.MouseMode = capturedMouse ? Godot.Input.MouseModeEnum.Captured : Godot.Input.MouseModeEnum.Visible;
        }

        #region Server - Client Communication

        public static byte ReadBaseInputs()
        {
            byte inputs = 0, i;

            // Movement
            Core.SetBitAt(ref inputs, i = 0, "move_up".GetKey());
            Core.SetBitAt(ref inputs, ++i, "move_right".GetKey());
            Core.SetBitAt(ref inputs, ++i, "move_down".GetKey());
            Core.SetBitAt(ref inputs, ++i, "move_left".GetKey());

            // Jump, Sneak
            Core.SetBitAt(ref inputs, ++i, "jump".GetKey());
            Core.SetBitAt(ref inputs, ++i, "sneak".GetKey());

            // Actions
            Core.SetBitAt(ref inputs, ++i, "attack".GetKey());
            Core.SetBitAt(ref inputs, ++i, "interact".GetKey());

            return inputs;
        }

        public static (Vector2 movement, bool jump, bool sneak, bool attack, bool interact) ReadBaseInputs(this byte inputByte)
        {
            return
                new(
                    // Movement
                    new Vector2(
                        inputByte.GetBitAt_float(3) - inputByte.GetBitAt_float(1), // Inverted to fix wrong movement on x axis
                        inputByte.GetBitAt_float(0) - inputByte.GetBitAt_float(2)
                        ),

                    // Jump, Sneak
                    inputByte.GetBitAt(4),
                    inputByte.GetBitAt(5),

                    // Actions
                    inputByte.GetBitAt(6),
                    inputByte.GetBitAt(7)
                );
        }

        #endregion
    }
}