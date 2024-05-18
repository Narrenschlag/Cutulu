using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public class XDevice
    {
        public long UDID { get; private set; } // Unique Device Identification Index

        public int iUDID { get; private set; } // Integer version of above
        public int DeviceId { get => iUDID; }

        public XInputManager Manager { get; private set; }
        public XDeviceType DeviceType { get; private set; }
        public string RawDeviceName { get; private set; }
        public string DeviceName { get; private set; }
        public bool Connected { get; private set; }

        public string UsbProduct { get; private set; }
        public string UsbVendor { get; private set; }
        public string GUID { get; private set; }

        public int SteamInputIndex { get; private set; }
        public int XInputIndex { get; private set; }

        public XInput[] SpecificListenInputs { get; set; } = null;
        public XInputMap InputMap { get; set; }

        public int GetUniqueHash(int externalId) => Encryption.Hash(externalId, iUDID);

        public XDevice(XInputManager manager, long udid, XInputMap map = default)
        {
            Manager = manager;
            iUDID = (int)udid;
            UDID = udid;

            InputMap = map.Mapping == null ? new() : map;
            OnConnect();
        }

        #region Remap Inputs
        public virtual void SetMap(string name) { if (ListenForInput(out XInput[] inputs)) SetMap(name, inputs); }
        public virtual void SetMap(string name, params XInput[] inputs)
        {
            InputMap.Clear(name);
            InputMap.Override(name, inputs);
        }

        public virtual void AddMap(string name) { if (ListenForInput(out XInput[] inputs)) AddMap(name, inputs); }
        public virtual void AddMap(string name, params XInput[] inputs)
        {
            InputMap.Override(name, inputs);
        }

        public virtual void ClearMap(string name)
        {
            InputMap.Clear(name);
        }
        #endregion

        #region Connection Status
        private void OnConnect()
        {
            Connected = true;

            // Native Device aka. Keyboard
            if (iUDID < 0)
            {
                DeviceType = XDeviceType.Native;
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
                    SteamInputIndex >= 0 ? XDeviceType.Steam :
                    Input.IsJoyKnown(iUDID) ? XDeviceType.Generic :
                    XDeviceType.Unknown;

                string getString(string name, string defaultValue = default) =>
                    data.TryGetValue("vendor_id", out var value) &&
                    string.IsNullOrEmpty(value.AsString()) == false ?
                    value.AsString() : defaultValue;

                int getInteger(string name, int defaultValue = default) =>
                    data.TryGetValue("vendor_id", out var value) ?
                    value.AsInt32() : defaultValue;
            }

            Debug.Log($"+device: [{UDID}] as '{DeviceName}'");
        }

        public void OnReconnect()
        {
            OnConnect();

            Input.StartJoyVibration(iUDID, 0.5f, 0.5f, 2.5f);
        }

        public void OnDisconnect()
        {
            Connected = false;

            Debug.Log($"-device [{UDID}] as '{DeviceName}'");
        }
        #endregion

        #region Read Inputs using name
        public bool IsPressed(string name) => InputMap.Mapping.TryGetValue(XInputMap.ModifyString(name), out var entry) && entry.IsPressed(DeviceId);
        public float GetValue(string name) => InputMap.Mapping.TryGetValue(XInputMap.ModifyString(name), out var entry) ? entry.GetValue(DeviceId) : default;

        public bool ListenForInput(out string[] inputs, params string[] range)
        {
            List<string> list = null;
            for (int i = 0; i < range?.Length; i++)
            {
                if (IsPressed(range[i])) (list ??= new()).Add(XInputMap.ModifyString(range[i]));
            }

            return (inputs = list?.ToArray()) != null;
        }
        #endregion

        public bool IsAnythingPressed()
        {
            var range = SpecificListenInputs ?? (DeviceId < 0 ? Manager.XNative : Manager.XGamepad);

            for (int i = 0; i < range?.Length; i++)
            {
                if (IsPressed(range[i])) return true;
            }

            return false;
        }

        #region Read Inputs using XInput
        public bool IsPressed(XInput input) => XInputf.IsPressed(Mathf.Min(0, DeviceId), input);
        public float GetValue(XInput input) => XInputf.GetValue(Mathf.Min(0, DeviceId), input);

        public bool ListenForInput(out XInput[] inputs) => ListenForInput(out inputs, SpecificListenInputs ?? (DeviceId < 0 ? Manager.XNative : Manager.XGamepad));
        public bool ListenForInput(out XInput[] inputs, params XInput[] range)
        {
            List<XInput> list = null;
            for (int i = 0; i < range?.Length; i++)
            {
                if (IsPressed(range[i])) (list ??= new()).Add(range[i]);
            }

            return (inputs = list?.ToArray()) != null;
        }
        #endregion
    }

    public enum XDeviceType : byte
    {
        Unknown,
        Generic,
        Native,
        Steam
    }
}