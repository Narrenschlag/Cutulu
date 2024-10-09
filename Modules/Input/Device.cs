using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public class Device
    {
        public long UDID { get; private set; } // Unique Device Identification Index
        public long RDID { get; set; }

        public long RemoteDeviceId { get => RDID; } // Remote Device Identification Index
        public bool RemoteReady { get; set; } = false; // Remote Ready

        public int iUDID { get => (int)UDID; } // Integer version of above
        public int DeviceId { get => iUDID; }

        public InputManager Manager { get; private set; }
        public InfoStruct Info { get; private set; }
        public bool Connected { get; private set; }
        public InputMap InputMap { get; set; }

        public readonly List<InputEnum> Whitelist = new();
        public readonly List<InputEnum> Blacklist = new();

        public int GetUniqueHash(int externalId) => Encryption.Hash(externalId, iUDID);

        public Device(InputManager manager, long udid, InputMap map = default)
        {
            Manager = manager;
            UDID = udid;

            InputMap = map.Mapping == null ? new() : map;
            OnConnect();
        }

        #region Remap Inputs

        public virtual void SetMap(string name) { if (ListenForInput(out InputEnum[] inputs)) SetMap(name, inputs); }
        public virtual void SetMap(string name, params InputEnum[] inputs)
        {
            InputMap.Clear(name);
            InputMap.Override(name, inputs);
        }

        public virtual void AddMap(string name) { if (ListenForInput(out InputEnum[] inputs)) AddMap(name, inputs); }
        public virtual void AddMap(string name, params InputEnum[] inputs)
        {
            InputMap.Override(name, inputs);
        }

        public virtual void ClearMap(string name)
        {
            InputMap.Clear(name);
        }

        #endregion

        #region Haptic Feedback

        public void Vibrate(float leftMotor, float rightMotor, float duration = 1f) => Godot.Input.StartJoyVibration(iUDID, leftMotor, rightMotor, duration);

        #endregion

        #region Connection Status

        private void OnConnect()
        {
            var data = iUDID != -1 ? Godot.Input.GetJoyInfo(iUDID) : null;
            var i = default(int); // index cache
            var n = string.Empty; // name cache
            Connected = true;

            // Assign device info
            Info = iUDID < 0 ? new()

            // Native Device aka. Keyboard
            {
                DeviceType = TypeEnum.Native,
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
                i >= 0 ? TypeEnum.Steam :
                Godot.Input.IsJoyKnown(iUDID) ? TypeEnum.Generic :
                TypeEnum.Unknown,
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

        public void OnReconnect()
        {
            OnConnect();

            //Vibrate(0.5f, 0.5f, 2.5f);
        }

        public void OnDisconnect()
        {
            Connected = false;

            Debug.Log($"-device({UDID}, {Info.DeviceName})");
        }

        #endregion

        #region Black- and Whitelist

        public InputEnum[] ConsiderBlacklist(InputEnum[] array)
        {
            var list = new List<InputEnum>();

            for (int i = 0; i < array?.Length; i++)
            {
                if (Blacklist.Contains(array[i]) == false)
                    list.Add(array[i]);
            }

            return list.ToArray();
        }

        public InputEnum[] GetRange()
        {
            return Whitelist.NotEmpty() ? Whitelist.ToArray() : ConsiderBlacklist(DeviceId < 0 ? Manager.XNative : Manager.XGamepad);
        }

        #endregion

        #region Read Inputs using name

        public bool IsPressed(string name) => InputMap.Mapping.TryGetValue(InputMap.ModifyString(name), out var entry) && entry.IsPressed(DeviceId);
        public float GetValue01(string name) => InputMap.Mapping.TryGetValue(InputMap.ModifyString(name), out var entry) ? entry.GetValue(DeviceId) : default;
        public byte GetValue255(string name) => InputMap.Mapping.TryGetValue(InputMap.ModifyString(name), out var entry) ? entry.GetValue(DeviceId).FloatToByte01() : default;

        public bool ListenForInput(out string[] inputs, params string[] range)
        {
            List<string> list = null;
            for (int i = 0; i < range?.Length; i++)
            {
                if (IsPressed(range[i])) (list ??= new()).Add(InputMap.ModifyString(range[i]));
            }

            return (inputs = list?.ToArray()) != null;
        }

        public bool IsAnythingPressed()
        {
            var range = GetRange();

            for (int i = 0; i < range?.Length; i++)
            {
                if (IsPressed(range[i])) return true;
            }

            return false;
        }

        #endregion

        #region Read Inputs using XInput

        public bool IsPressed(InputEnum input) => Input.IsPressed(iUDID, input);
        public float GetValue(InputEnum input) => Input.GetValue(iUDID, input);

        public bool ListenForInput(out InputEnum[] inputs) => ListenForInput(out inputs, GetRange());
        public bool ListenForInput(out InputEnum[] inputs, params InputEnum[] range)
        {
            var list = new List<InputEnum>();

            for (int i = 0; i < range?.Length; i++)
            {
                if (IsPressed(range[i]))
                    list.Add(range[i]);
            }

            inputs = list.Count < 1 ? System.Array.Empty<InputEnum>() : list.ToArray();
            return inputs.Length > 0;
        }

        #endregion

        /// <summary>
        /// Type of device
        /// </summary>
        public enum TypeEnum : byte
        {
            Unknown,
            Generic,
            Native,
            Steam
        }

        /// <summary>
        /// Information about XDevice
        /// </summary>
        public struct InfoStruct
        {
            public TypeEnum
                DeviceType;

            public string
                RawDeviceName,
                DeviceName,

                UsbProduct,
                UsbVendor,
                GUID;

            public int
                SteamInputIndex,
                XInputIndex;
        }
    }
}