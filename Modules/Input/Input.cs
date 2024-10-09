using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    #region Logic

    public static class Input
    {
        #region XInput to XInputType

        public static TypeEnum GetType(this InputEnum input) => GetType(ref input);
        public static TypeEnum GetType(ref InputEnum input)
        {
            // Valid input
            if (input >= InputEnum.Axis0)
            {
                // Axis Button
                if (input <= InputEnum.Axis5) return TypeEnum.Axis;

                // Axis Button
                else if (input >= InputEnum.AxisButton0 && input <= InputEnum.AxisButton11) return TypeEnum.AxisButton;

                // Button
                else if (input >= InputEnum.Button0 && input <= InputEnum.Button21) return TypeEnum.Button;

                // Mouse
                else if (input >= InputEnum.MouseMin && input <= InputEnum.MouseMax) return TypeEnum.Mouse;

                // Key
                else if (input >= InputEnum.KeyMin && input <= InputEnum.KeyMax) return TypeEnum.Key;
            }

            // Invalid
            return TypeEnum.Invalid;
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

        public static List<InputEnum> GetRange(params TypeEnum[] types)
        {
            if (types?.Length < 1) return null;

            List<InputEnum> list = null;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] != TypeEnum.Invalid) list ??= new();

                switch (types[i])
                {
                    case TypeEnum.Axis:
                        for (InputEnum k = InputEnum.Axis0; k <= InputEnum.Axis5; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case TypeEnum.AxisButton:
                        for (InputEnum k = InputEnum.AxisButton0; k <= InputEnum.AxisButton11; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case TypeEnum.Button:
                        for (InputEnum k = InputEnum.Button0; k <= InputEnum.Button21; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case TypeEnum.Mouse:
                        for (InputEnum k = InputEnum.MouseMin; k <= InputEnum.MouseMax; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case TypeEnum.Key:
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
                case TypeEnum.Button: return Godot.Input.IsJoyButtonPressed(deviceId, (JoyButton)(input + (int)InputEnum.ButtonOffset));

                // IO result for axis values
                case TypeEnum.AxisButton:
                    var i = (InputEnum)Mathf.FloorToInt(((int)input + (int)InputEnum.AxisButtonOffset) / 2f); // Get the axis

                    return (int)input % 2 == 0 ?
                    GetValue(ref deviceId, ref i) >= ButtonPressThreshold : // Upward on Axis
                    GetValue(ref deviceId, ref i) <= -ButtonPressThreshold; // Downward on Axis

                // Float result for axis
                case TypeEnum.Axis: return Mathf.Abs(GetValue(ref deviceId, ref input)) >= ButtonPressThreshold; // Return -1 to 1

                // IO result for mouse buttons
                case TypeEnum.Mouse: return Godot.Input.IsMouseButtonPressed((MouseButton)(input + (int)InputEnum.MouseOffset));

                // IO result for keys
                case TypeEnum.Key: return Godot.Input.IsKeyPressed((Key)(input + (int)InputEnum.KeyOffset));

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
                case TypeEnum.Button: return IsPressed(ref deviceId, ref input) ? 1f : 0f;
                case TypeEnum.Axis: return Godot.Input.GetJoyAxis(deviceId, (JoyAxis)input);

                case TypeEnum.Mouse: return IsPressed(ref deviceId, ref input) ? 1f : 0f;
                case TypeEnum.Key: return IsPressed(ref deviceId, ref input) ? 1f : 0f;

                case TypeEnum.AxisButton:
                    var threshold = ButtonPressThreshold;
                    ButtonPressThreshold = 0.08f;

                    var result = IsPressed(ref deviceId, ref input) ? Mathf.Abs(GetValue(deviceId, ((input - InputEnum.AxisButton0) / 2) + InputEnum.Axis0) - ButtonPressThreshold) / (1f - ButtonPressThreshold) : 0f;
                    ButtonPressThreshold = threshold;

                    return result;

                default:
                    Debug.LogError($"Invalid xInput.");
                    return default;
            }
        }

        #endregion

        #region Types

        public enum TypeEnum : byte
        {
            Invalid = 255,

            Axis = 0,
            AxisButton = 1,
            Button = 2,

            Mouse = 10,
            Key = 11,
        }

        #endregion
    }

    #endregion

    #region Values

    public enum InputEnum : int
    {
        KeyOffset = -1024,
        MouseOffset = -512,
        ButtonOffset = -256,
        AxisButtonOffset = -128,
        Invalid = -1,

        Axis0 = (int)JoyAxis.LeftX,
        Axis1,
        Axis2,
        Axis3,
        Axis4,
        Axis5,

        AxisButton0 = -AxisButtonOffset,
        AxisButton1,
        AxisButton2,
        AxisButton3,
        AxisButton4,
        AxisButton5,
        AxisButton6,
        AxisButton7,
        AxisButton8,
        AxisButton9,
        AxisButton10,
        AxisButton11,

        Button0 = (int)JoyButton.A - ButtonOffset,
        Button1,
        Button2,
        Button3,
        Button4,
        Button5,
        Button6,
        Button7,
        Button8,
        Button9,
        Button10,
        Button11,
        Button12,
        Button13,
        Button14,
        Button15,
        Button16,
        Button17,
        Button18,
        Button19,
        Button20,
        Button21,

        MouseMin = (int)MouseButton.Left - MouseOffset,
        MouseMax = MouseMin + (int)MouseButton.Xbutton2 - (int)MouseButton.Left,

        KeyMin = (int)Key.Space - KeyOffset,
        KeyMax = KeyMin + (int)Key.Kp9 - (int)Key.Space,
    }

    #endregion
}