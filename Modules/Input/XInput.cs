using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    #region Logic
    public static class XInputf
    {
        #region XInput to XInputType
        public static XInputType GetType(this XInput input) => GetType(ref input);
        public static XInputType GetType(ref XInput input)
        {
            // Valid input
            if (input >= XInput.Axis0)
            {
                // Axis Button
                if (input <= XInput.Axis5) return XInputType.Axis;

                // Axis Button
                else if (input >= XInput.AxisButton0 && input <= XInput.AxisButton11) return XInputType.AxisButton;

                // Button
                else if (input >= XInput.Button0 && input <= XInput.Button21) return XInputType.Button;

                // Key
                else if (input >= XInput.KeyMin && input <= XInput.KeyMax) return XInputType.Key;
            }

            // Invalid
            return XInputType.Invalid;
        }
        #endregion

        #region Input to XInput
        public static XInput GetXInput(this Key key) => (XInput)((int)key - (int)XInput.KeyOffset);
        public static XInput GetXInput(this JoyAxis axis) => (XInput)(int)axis - (int)JoyAxis.LeftX;
        public static XInput GetXInput(this JoyButton button) => (XInput)(int)button - (int)XInput.ButtonOffset;
        public static XInput GetXInput(this JoyAxis axis, bool readNegative) => (XInput)(2 * ((int)axis - (int)JoyAxis.LeftX) + (readNegative ? 1 : 0)) - (int)XInput.AxisButtonOffset;
        #endregion

        #region Get Range
        public static List<XInput> GetRange(params XInputType[] types)
        {
            if (types?.Length < 1) return null;

            List<XInput> list = null;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] != XInputType.Invalid) list ??= new();

                switch (types[i])
                {
                    case XInputType.Axis:
                        for (XInput k = XInput.Axis0; k <= XInput.Axis5; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case XInputType.AxisButton:
                        for (XInput k = XInput.AxisButton0; k <= XInput.AxisButton11; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case XInputType.Button:
                        for (XInput k = XInput.Button0; k <= XInput.Button21; k++)
                        {
                            list.Add(k);
                        }
                        break;

                    case XInputType.Key:
                        for (Key k = Key.Space; k <= Key.Quoteleft; k++)
                        {
                            list.Add(GetXInput(k));
                        }

                        for (Key k = Key.Braceleft; k <= Key.Asciitilde; k++)
                        {
                            list.Add(GetXInput(k));
                        }

                        for (Key k = Key.Yen; k <= Key.Section; k++)
                        {
                            list.Add(GetXInput(k));
                        }

                        for (Key k = Key.Special; k <= Key.Kp9; k++)
                        {
                            list.Add(GetXInput(k));
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

        public static bool IsPressed(this XInput input, int deviceId) => IsPressed(ref deviceId, ref input);
        public static bool IsPressed(this int deviceId, XInput input) => IsPressed(ref deviceId, ref input);
        public static bool IsPressed(ref int deviceId, ref XInput input)
        {
            // Handle value
            switch (GetType(ref input))
            {
                // IO result for buttons
                case XInputType.Button: return Input.IsJoyButtonPressed(deviceId, (JoyButton)(input + (int)XInput.ButtonOffset));

                // IO result for axis values
                case XInputType.AxisButton:
                    var i = (XInput)Mathf.FloorToInt(((int)input + (int)XInput.AxisButtonOffset) / 2f); // Get the axis

                    return (int)input % 2 == 0 ?
                    GetValue(ref deviceId, ref i) >= ButtonPressThreshold : // Upward on Axis
                    GetValue(ref deviceId, ref i) <= -ButtonPressThreshold; // Downward on Axis

                // Float result for axis
                case XInputType.Axis: return Mathf.Abs(GetValue(ref deviceId, ref input)) >= ButtonPressThreshold; // Return -1 to 1

                // IO result for keys
                case XInputType.Key: return Input.IsKeyPressed((Key)(input + (int)XInput.KeyOffset));

                // Invalid input
                default:
                    Debug.LogError($"Invalid xInput.");
                    return false;
            }
        }
        #endregion

        #region GetValue
        public static float GetValue(this XInput input, int deviceId) => GetValue(ref deviceId, ref input);
        public static float GetValue(this int deviceId, XInput input) => GetValue(ref deviceId, ref input);
        public static float GetValue(ref int deviceId, ref XInput input)
        {
            // Handle value
            switch (GetType(ref input))
            {
                case XInputType.Button: return IsPressed(ref deviceId, ref input) ? 1f : 0f;
                case XInputType.AxisButton: return IsPressed(ref deviceId, ref input) ? 1f : 0f;
                case XInputType.Axis: return Input.GetJoyAxis(deviceId, (JoyAxis)input);
                case XInputType.Key: return IsPressed(ref deviceId, ref input) ? 1f : 0f;

                default:
                    Debug.LogError($"Invalid xInput.");
                    return default;
            }
        }
        #endregion
    }
    #endregion

    #region Types
    public enum XInputType : byte
    {
        Invalid = 255,

        Axis = 0,
        AxisButton = 1,
        Button = 2,

        Key = 10,
    }
    #endregion

    #region Values
    public enum XInput : int
    {
        KeyOffset = -512,
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

        KeyMin = (int)Key.Space - KeyOffset,
        KeyMax = KeyMin + (int)Key.Kp9 - (int)Key.Space, // 71 + 120 -> 129 keys
    }
    #endregion
}