namespace Cutulu.Input
{
    using System.Collections.Generic;

    public class Device
    {
        public readonly List<InputEnum> Whitelist = new();
        public readonly List<InputEnum> Blacklist = new();

        public long UDID { get; private set; } // Unique Device Identification Index
        public long RDID { get; set; }

        public bool RemoteReady { get; set; } = false; // Remote Ready

        public DeviceInfoStruct Info { get; private set; }
        public bool Connected { get; private set; }
        public MapStruct Map { get; set; }

        public bool IsFake { get; private set; }

        public int iUDID { get => (int)UDID; } // Integer version of above
        public int DeviceId { get => iUDID; }

        public int GetUniqueHash(int externalId) => Encryption.Hash(externalId, iUDID);

        public Device(long udid, MapStruct map = default, bool isFake = false)
        {
            UDID = udid;

            Map = map.Mapping == null ? new() : map;
            IsFake = isFake;

            _Connect();
        }

        #region Haptic Feedback

        public void Vibrate(float leftMotor, float rightMotor, float duration = 1f) => Godot.Input.StartJoyVibration(iUDID, leftMotor, rightMotor, duration);

        #endregion

        #region Connection Status

        private void _Connect()
        {
            var data = iUDID != -1 && IsFake == false ? Godot.Input.GetJoyInfo(iUDID) : null;

            var i = default(int); // index cache
            var n = string.Empty; // name cache
            Connected = true;

            // Assign device info
            Info = IsFake ? new()
            {
                DeviceType = DeviceTypeEnum.Unknown,
                RawDeviceName = "FakeDevice",
                DeviceName = "Fake Device",
            } : iUDID < 0 ? new()

            // Native Device aka. Keyboard
            {
                DeviceType = DeviceTypeEnum.Native,
                RawDeviceName = "NativeDevice",
                DeviceName = "Native Device",
            } : new()

            // External Device aka. Gamepad
            {
                DeviceName = n = Godot.Input.GetJoyName(iUDID),
                GUID = Godot.Input.GetJoyGuid(iUDID),

                RawDeviceName = getString("raw_name", n),
                UsbProduct = getString("product_id"),
                UsbVendor = getString("vendor_id"),

                SteamInputIndex = i = getInteger("steam_input_index", -1),
                XInputIndex = getInteger("xinput_index"),

                DeviceType =
                i >= 0 ? DeviceTypeEnum.Steam :
                Godot.Input.IsJoyKnown(iUDID) ? DeviceTypeEnum.Generic :
                DeviceTypeEnum.Unknown,
            };

            string getString(string name, string defaultValue = default) =>
                data.TryGetValue("vendor_id", out var value) &&
                string.IsNullOrEmpty(value.AsString()) == false ?
                value.AsString() : defaultValue;

            int getInteger(string name, int defaultValue = default) =>
                data.TryGetValue("vendor_id", out var value) ?
                value.AsInt32() : defaultValue;

            Debug.Log($"+device({UDID}, {Info.DeviceName})");
        }

        public void _Reconnected()
        {
            _Connect();
        }

        public void _Disconnect()
        {
            Connected = false;

            Debug.Log($"-device({UDID}, {Info.DeviceName})");
        }

        #endregion

        #region Black- and Whitelist

        public InputEnum[] GetRange(out bool whitelist)
        {
            whitelist = Whitelist.NotEmpty();

            return whitelist ? Whitelist.ToArray() : DeviceId < 0 ? Manager.Native : Manager.Gamepad;
        }

        #endregion

        #region Read Inputs using name

        public bool IsJustPressed(string name, ref bool reference)
        {
            var pressed = IsPressed(name);

            var result = pressed && !reference;

            reference = pressed;
            return result;
        }

        public bool IsJustReleased(string name, ref bool reference)
        {
            var pressed = IsPressed(name);

            var result = !pressed && reference;

            reference = pressed;
            return result;
        }

        public bool IsPressed(string name) => Map.Mapping.TryGetValue(MapStruct.ModifyString(name), out var entry) && entry.IsPressed(DeviceId);
        public float GetValue01(string name) => Map.Mapping.TryGetValue(MapStruct.ModifyString(name), out var entry) ? entry.GetValue(DeviceId) : default;
        public byte GetValue255(string name) => Map.Mapping.TryGetValue(MapStruct.ModifyString(name), out var entry) ? entry.GetValue(DeviceId).FloatToByte01() : default;

        public bool ListenForInput(out string[] inputs, params string[] range)
        {
            List<string> list = null;
            for (int i = 0; i < range?.Length; i++)
            {
                if (IsPressed(range[i])) (list ??= new()).Add(MapStruct.ModifyString(range[i]));
            }

            return (inputs = list?.ToArray()) != null;
        }

        public bool IsAnythingPressed()
        {
            var range = GetRange(out var whitelist);

            for (int i = 0; i < range?.Length; i++)
            {
                if ((whitelist || Blacklist.Contains(range[i]) == false) && IsPressed(range[i])) return true;
            }

            return false;
        }

        public Godot.Vector2 GetVectorClamped(string positiveX, string negativeX, string positiveY, string negativeY)
        {
            return new Godot.Vector2(GetAxis(positiveX, negativeX), GetAxis(positiveY, negativeY)).ClampNormalized();
        }

        public Godot.Vector2 GetVector(string positiveX, string negativeX, string positiveY, string negativeY, bool normalize = false)
        {
            var vector = new Godot.Vector2(GetAxis(positiveX, negativeX), GetAxis(positiveY, negativeY));

            return normalize ? vector.Normalized() : vector;
        }

        public float GetAxis(string positive, string negative)
        {
            return GetValue01(positive) - GetValue01(negative);
        }

        #endregion

        #region Read Inputs using XInput

        public bool IsPressed(InputEnum input) => Backend.IsPressed(iUDID, input);
        public float GetValue(InputEnum input) => Backend.GetValue(iUDID, input);

        public bool ListenForInput(out InputEnum[] inputs)
        {
            var range = GetRange(out var whitelist);

            return ListenForInput(whitelist, out inputs, range);
        }

        /// <summary>
        /// Whitelist=true skips the blacklist check
        /// </summary>
        public bool ListenForInput(bool whitelist, out InputEnum[] inputs, params InputEnum[] range)
        {
            var list = new List<InputEnum>();

            for (int i = 0; i < range?.Length; i++)
            {
                if ((whitelist || Blacklist.Contains(range[i]) == false) && IsPressed(range[i]))
                    list.Add(range[i]);
            }

            inputs = list.Count < 1 ? System.Array.Empty<InputEnum>() : list.ToArray();
            return inputs.Length > 0;
        }

        #endregion
    }
}