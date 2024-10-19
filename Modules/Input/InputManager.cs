namespace Cutulu
{
    using System.Collections.Generic;
    using System;
    using Godot;

    [GlobalClass]
    public partial class InputManager : Node
    {
        [Export] public int MaxDeviceCount { get; set; } = 4;

        public readonly Dictionary<long, Device> Devices = new();
        public ModeEnum Mode { get; set; }

        public delegate void OnNewDeviceEventHandler(Device device);
        public event Action<Device> AddedDevice, RemovedDevice;

        public readonly InputEnum[]
        XAll = Input.GetRange(Input.TypeEnum.AxisButton, Input.TypeEnum.Button, Input.TypeEnum.Mouse, Input.TypeEnum.Key).ToArray(),
        XGamepad = Input.GetRange(Input.TypeEnum.AxisButton, Input.TypeEnum.Button).ToArray(),
        XNative = Input.GetRange(Input.TypeEnum.Mouse, Input.TypeEnum.Key).ToArray();

        public Vector2 MouseMotion { get; private set; }
        private byte ResetMotion { get; set; }

        #region Local Node Events
        public override void _EnterTree()
        {
            // Not yet setup
            if (Devices.IsEmpty())
            {
                Mode = ModeEnum.Open;

                // Add native device
                _AddDevice(new(this, -1));

                // Add existing
                foreach (var existing in Godot.Input.GetConnectedJoypads())
                {
                    _DeviceChange(existing, true);
                }
            }

            Godot.Input.JoyConnectionChanged += _DeviceChange;
        }

        public override void _ExitTree()
        {
            Godot.Input.JoyConnectionChanged -= _DeviceChange;
        }

        public override void _Process(double delta)
        {
            if (ResetMotion > 0 && ResetMotion++ > 1)
            {
                MouseMotion = default;
                ResetMotion = 0;
            }
        }

        public override void _Input(InputEvent @event)
        {
            switch (@event)
            {
                // Mouse motion
                case InputEventMouseMotion _motion:
                    MouseMotion = _motion.Relative;
                    ResetMotion = 1;
                    break;

                // Other input
                default: break;
            }
        }
        #endregion

        #region Device Event
        private void _DeviceChange(long udid, bool connected)
        {
            // Connected
            if (connected)
            {
                // Reconnecting device
                if (Devices.TryGetValue(udid, out var device))
                {
                    device._Reconnect();
                }

                // New device
                else
                {
                    // Clamp device count
                    if (MaxDeviceCount < 1 || MaxDeviceCount > Devices.Count)
                        _AddDevice(new(this, udid));
                }
            }

            // Disconnected
            else
            {
                // Device disconnected
                if (Devices.TryGetValue(udid, out var device))
                {
                    device._Disconnect();

                    if (Mode == ModeEnum.Open)
                    {
                        Devices.Remove(udid);
                        RemovedDevice?.Invoke(device);
                    }
                }
            }
        }

        private void _AddDevice(Device device)
        {
            Devices.Add(device.UDID, device);
            AddedDevice?.Invoke(device);
        }

        public void RequestDevices(Action<Device> Func)
        {
            if (Func != null)
            {
                foreach (var device in Devices?.Values)
                {
                    Func.Invoke(device);
                }
            }
        }
        #endregion

        public bool IsAnythingPressed()
        {
            foreach (var device in Devices?.Values)
            {
                if (device.IsAnythingPressed()) return true;
            }

            return false;
        }

        #region Global based on XInput
        public bool IsPressed(InputEnum input)
        {
            foreach (var device in Devices.Values)
            {
                if (device.IsPressed(input)) return true;
            }

            return default;
        }

        public float GetValue(InputEnum input)
        {
            var maxValue = 0f;

            foreach (var device in Devices.Values)
            {
                var value = device.GetValue(input);

                if (value.abs() >= 1f) return value;
                else if (value.abs() > maxValue.abs()) maxValue = value;
            }

            return maxValue;
        }

        public bool ListenForInput(out (Device device, InputEnum[] inputs)[] devices)
        {
            List<(Device device, InputEnum[] inputs)> list = null;
            foreach (var device in Devices.Values)
            {
                if (device.ListenForInput(out InputEnum[] input)) (list ??= new()).Add((device, input));
            }

            return (devices = list?.ToArray()) != null;
        }

        public bool ListenForInput(bool whitelist, out (Device device, InputEnum[] inputs)[] devices, params InputEnum[] range)
        {
            var list = new List<(Device device, InputEnum[] inputs)>();

            foreach (var device in Devices.Values)
            {
                if (device.ListenForInput(whitelist, out var input, range)) list.Add((device, input));
            }

            devices = list.ToArray();
            return devices.Length > 0;
        }
        #endregion

        #region Global based on name
        public bool IsPressed(string name)
        {
            foreach (var device in Devices.Values)
            {
                if (device.IsPressed(name)) return true;
            }

            return default;
        }

        public float GetValue(string name)
        {
            var maxValue = 0f;

            foreach (var device in Devices.Values)
            {
                var value = device.GetValue01(name);

                if (value.abs() >= 1f) return value;
                else if (value.abs() > maxValue.abs()) maxValue = value;
            }

            return maxValue;
        }

        public bool ListenForInput(out (Device device, string[] inputs)[] devices, params string[] range)
        {
            List<(Device device, string[] inputs)> list = null;
            foreach (var device in Devices.Values)
            {
                if (device.ListenForInput(out var input, range)) (list ??= new()).Add((device, input));
            }

            return (devices = list?.ToArray()) != null;
        }
        #endregion

        public enum ModeEnum
        {
            Open, Locked
        }
    }
}