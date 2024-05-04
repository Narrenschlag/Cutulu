using Godot;

namespace Cutulu
{
    public class InputDevice
    {
        public long UDID { get; private set; } // Unique Device Identification Index
        public int iUDID { get; private set; } // Integer version of above

        public InputDeviceManager Manager { get; private set; }
        public InputDeviceType DeviceType { get; private set; }
        public string RawDeviceName { get; private set; }
        public string DeviceName { get; private set; }
        public bool Connected { get; private set; }

        public string UsbProduct { get; private set; }
        public string UsbVendor { get; private set; }
        public string GUID { get; private set; }

        public int SteamInputIndex { get; private set; }
        public int XInputIndex { get; private set; }

        private bool leftTrigger, rightTrigger;

        public InputDevice(InputDeviceManager manager, long udid)
        {
            Manager = manager;
            iUDID = (int)udid;
            UDID = udid;

            OnConnect();

            Debug.Log($"+device: [{UDID}] as '{DeviceName}'");
        }

        #region Connection Status
        private void OnConnect()
        {
            Connected = true;

            // Native Device aka. Keyboard
            if (iUDID < 0)
            {
                DeviceType = InputDeviceType.Native;
                RawDeviceName = "NativeDevice";
                DeviceName = "Native Input";
            }

            // External Device aka. Gamepad
            else
            {
                DeviceName = Input.GetJoyName(iUDID);
                var data = Input.GetJoyInfo(iUDID);
                GUID = Input.GetJoyGuid(iUDID);

                RawDeviceName = getString("raw_name", DeviceName);
                UsbProduct = getString("product_id");
                UsbVendor = getString("vendor_id");

                SteamInputIndex = getInteger("steam_input_index", -1);
                XInputIndex = getInteger("xinput_index");

                DeviceType =
                    SteamInputIndex >= 0 ? InputDeviceType.Steam :
                    Input.IsJoyKnown(iUDID) ? InputDeviceType.Generic :
                    InputDeviceType.Unknown;

                string getString(string name, string defaultValue = default) =>
                    data.TryGetValue("vendor_id", out var value) &&
                    string.IsNullOrEmpty(value.AsString()) == false ?
                    value.AsString() : defaultValue;

                int getInteger(string name, int defaultValue = default) =>
                    data.TryGetValue("vendor_id", out var value) ?
                    value.AsInt32() : defaultValue;
            }
        }

        public void OnReconnect()
        {
            OnConnect();

            Debug.Log($"+device [{UDID}] as '{DeviceName}'");
            Input.StartJoyVibration(iUDID, 0.5f, 0.5f, 2.5f);
        }

        public void OnDisconnect()
        {
            Connected = false;

            Debug.Log($"-device [{UDID}] as '{DeviceName}'");
        }
        #endregion

        #region Debug Functions
        public void PrintDebug()
        {
            Debug.Log($"#############  Device: {DeviceName}  ##############");
            Debug.Log($"Move: {GetValue(InputCode.MoveRight)}x {GetValue(InputCode.MoveUp)}y");
            Debug.Log($"Look: {GetValue(InputCode.LookRight)}x {GetValue(InputCode.LookUp)}y");
            Debug.Log($"Dpad: {GetValue(InputCode.DpadRight)}x {GetValue(InputCode.DpadUp)}y");

            Debug.Log($"Trigger: {GetValue(InputCode.TriggerLeft)}left {GetValue(InputCode.TriggerRight)}right");
            Debug.Log($"Shoulder: {GetValue(InputCode.ShoulderLeft)}left {GetValue(InputCode.ShoulderRight)}right");
            Debug.Log($"Sticks: {GetValue(InputCode.StickPressLeft)}left {GetValue(InputCode.StickPressRight)}right");

            Debug.Log($"R0:{GetValue(InputCode.RightSouth)} R1:{GetValue(InputCode.RightEast)} R2:{GetValue(InputCode.RightNorth)} R3:{GetValue(InputCode.RightWest)}");
            Debug.Log($"Share:{GetValue(InputCode.Start2)} Start:{GetValue(InputCode.Start)} OS:{GetValue(InputCode.OSHome)}");
        }
        #endregion

        #region Read Inputs
        // Input Map
        public bool GetInput(string inputName, float threshold = .5f)
        => Manager.Map.TryGetValue(inputName, out var inputSet) && inputSet.IsPressed(this, threshold);

        // Input Map
        public float GetInputValue(string inputName)
        => Manager.Map.TryGetValue(inputName, out var inputSet) ? inputSet.Value(this) : default;

        public bool GetKeyDown(InputCode input, ref bool keyMemory, float threshold = 0.5f, params string[] nativeInputNames)
        {
            var previous = keyMemory;

            keyMemory = GetKey(input, threshold, nativeInputNames);
            return previous == false && keyMemory;
        }

        public bool GetKeyUp(InputCode input, ref bool keyMemory, float threshold = 0.5f, params string[] nativeInputNames)
        {
            var previous = keyMemory;

            keyMemory = GetKey(input, threshold, nativeInputNames);
            return previous && keyMemory == false;
        }

        public bool GetKey(InputCode input, float threshold = 0.5f, params string[] nativeInputNames)
        => GetValue(input, nativeInputNames) >= threshold;

        public float GetValue(InputCode input, params string[] nativeInputNames)
        {
            // Default value
            if (DeviceType == InputDeviceType.Native)
            {
                switch (input)
                {
                    case InputCode.DpadRight: return axis();
                    case InputCode.DpadUp: return axis();

                    case InputCode.MoveRight: return axis();
                    case InputCode.MoveUp: return axis();

                    case InputCode.LookRight: return nativeInputNames.Size() > 0 ? axis() : Manager.MouseMotion.X;
                    case InputCode.LookUp: return nativeInputNames.Size() > 0 ? axis() : Manager.MouseMotion.Y;

                    default: return nativeInputNames.Size() > 0 ? nativeInputNames[0].GetValue() : default;
                }

                float axis() => nativeInputNames.Size() > 1 ? nativeInputNames[0].GetValue() - nativeInputNames[1].GetValue() : default;
            }

            else
            {
                switch (input)
                {
                    case InputCode.OSHome: return button(JoyButton.RightStick);
                    case InputCode.Start: return button(JoyButton.LeftStick);
                    case InputCode.Start2: return button(JoyButton.Start);

                    case InputCode.MoveRight: return axis(JoyAxis.LeftX);
                    case InputCode.MoveUp: return -axis(JoyAxis.LeftY);

                    case InputCode.LookRight: return axis(JoyAxis.RightY);
                    case InputCode.LookUp: return -axis(JoyAxis.TriggerLeft);

                    case InputCode.BothShoulders: return Input.IsJoyButtonPressed(iUDID, JoyButton.Guide) && Input.IsJoyButtonPressed(iUDID, JoyButton.Back) ? 1 : 0;
                    case InputCode.ShoulderRight: return button(JoyButton.Guide);
                    case InputCode.ShoulderLeft: return button(JoyButton.Back);

                    case InputCode.TriggerRight:
                        if (rightTrigger == false && (rightTrigger = axis(JoyAxis.TriggerRight) != 0) == false) return 0f;
                        return axis(JoyAxis.TriggerRight) * .5f + .5f;

                    case InputCode.TriggerLeft:
                        if (leftTrigger == false && (leftTrigger = axis(JoyAxis.RightX) != 0) == false) return 0f;
                        return axis(JoyAxis.RightX) * .5f + .5f;


                    case InputCode.StickPressRight: return button(JoyButton.RightShoulder);
                    case InputCode.StickPressLeft: return button(JoyButton.LeftShoulder);

                    case InputCode.RightNorth: return button(JoyButton.Y);
                    case InputCode.RightWest: return button(JoyButton.X);
                    case InputCode.RightSouth: return button(JoyButton.A);
                    case InputCode.RightEast: return button(JoyButton.B);

                    case InputCode.LeftNorth: return button(JoyButton.DpadUp);
                    case InputCode.LeftWest: return button(JoyButton.DpadLeft);
                    case InputCode.LeftSouth: return button(JoyButton.DpadDown);
                    case InputCode.LeftEast: return button(JoyButton.DpadRight);

                    case InputCode.DpadRight: return button(JoyButton.DpadRight) - button(JoyButton.DpadLeft);
                    case InputCode.DpadUp: return button(JoyButton.DpadUp) - button(JoyButton.DpadDown);

                    default: return default;
                }

                float button(JoyButton button) => Input.IsJoyButtonPressed(iUDID, button) ? 1f : 0f;
                float axis(JoyAxis axis) => Input.GetJoyAxis(iUDID, axis);
            }
        }
        #endregion

        #region Vibration
        public void StartVibration(float weak, float strong, float duration = .1f) => Input.StartJoyVibration(iUDID, weak, strong, duration);
        public void StopVibration() => Input.StopJoyVibration(iUDID);

        public Vector2 GetVibrationStrength() => Input.GetJoyVibrationStrength(iUDID);
        public float GetVibrationDuration() => Input.GetJoyVibrationDuration(iUDID);

        public void GetVibration(out Vector2 strength, out float duration)
        {
            strength = GetVibrationStrength();
            duration = GetVibrationDuration();
        }
        #endregion
    }

    public enum InputDeviceType : byte
    {
        Unknown,
        Generic,
        Native,
        Steam
    }

    public enum InputCode : byte
    {
        Invalid,

        Start,
        Start2,
        OSHome,
        BothShoulders,

        MoveRight,
        MoveUp,

        LookRight,
        LookUp,

        ShoulderRight,
        TriggerRight,

        ShoulderLeft,
        TriggerLeft,

        StickPressRight,
        StickPressLeft,

        RightNorth,
        RightWest,
        RightSouth,
        RightEast,

        DpadRight,
        DpadUp,

        LeftNorth,
        LeftWest,
        LeftSouth,
        LeftEast
    }
}