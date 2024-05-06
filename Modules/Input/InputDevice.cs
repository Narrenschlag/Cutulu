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

        private InputTranslation Translation;

        public int GetUniqueHash(int externalId) => Encryption.Hash(externalId, iUDID);

        public InputDevice(InputDeviceManager manager, InputTranslation translation, long udid)
        {
            Translation = translation;
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
                DeviceName = "Native Device";
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
            Debug.Log($"Move: {GetValue(InputCode.LeftStickRight)}x {GetValue(InputCode.LeftStickUp)}y");
            Debug.Log($"Look: {GetValue(InputCode.RightStickRight)}x {GetValue(InputCode.RightStickUp)}y");
            Debug.Log($"Dpad: {GetValue(InputCode.DpadRight)}x {GetValue(InputCode.DpadUp)}y");

            Debug.Log($"Trigger: {GetValue(InputCode.LeftTrigger)}left {GetValue(InputCode.RightTrigger)}right");
            Debug.Log($"Shoulder: {GetValue(InputCode.LeftShoulder)}left {GetValue(InputCode.RightShoulder)}right");
            Debug.Log($"Sticks: {GetValue(InputCode.LeftStickPress)}left {GetValue(InputCode.RightStickPress)}right");

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

            keyMemory = GetKey(input, threshold);
            return previous == false && keyMemory;
        }

        public bool GetKeyUp(InputCode input, ref bool keyMemory, float threshold = 0.5f, params string[] nativeInputNames)
        {
            var previous = keyMemory;

            keyMemory = GetKey(input, threshold);
            return previous && keyMemory == false;
        }

        public bool GetKey(InputCode input, float threshold = 0.5f)
        => GetValue(input) >= threshold;

        public float GetValue(InputCode input)
        {
            // Get values
            Translation.Translate(input, out var _button, out var _axis, out var natives);

            // Extra overrides for native devices
            if (natives == null && DeviceType == InputDeviceType.Native)
            {
                switch (input)
                {
                    case InputCode.RightStickRight: return Manager.MouseMotion.X;
                    case InputCode.RightStickUp: return Manager.MouseMotion.Y;

                    default:
                        break;
                }
            }

            // Overrides for special inputs
            switch (input)
            {
                case InputCode.BothShoulders: return GetKey(InputCode.LeftShoulder) && GetKey(InputCode.RightShoulder) ? 1f : default;

                case InputCode.DpadRight: return GetValue(InputCode.LeftEast) - GetValue(InputCode.LeftWest);
                case InputCode.DpadUp: return GetValue(InputCode.LeftNorth) - GetValue(InputCode.LeftSouth);

                default: break;
            }

            // Native inputs
            if (DeviceType == InputDeviceType.Native) return natives != null ? natives.Length > 1 ? natives[0].GetValue() - natives[1].GetValue() : natives[0].GetValue() : default;

            // Gamepad inputs
            else return
                _button != JoyButton.Invalid ? button(_button) :
                _axis != JoyAxis.Invalid ? axis(_axis) : default;
            float axis(JoyAxis axis) => Input.GetJoyAxis(iUDID, axis);
            float button(JoyButton button) => Input.IsJoyButtonPressed(iUDID, button) ? 1f : 0f;
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
}