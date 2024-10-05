using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class XInputManager : Node
    {
        [Export] public int MaxDeviceCount { get; set; } = 4;

        public readonly Dictionary<long, XDevice> Devices = new();
        public ModeEnum Mode { get; set; }

        public delegate void OnNewDeviceEventHandler(XDevice device);
        public OnNewDeviceEventHandler OnDeviceAdd, OnDeviceRem;

        public readonly XInput[]
        XAll = XInputf.GetRange(XInputType.AxisButton, XInputType.Button, XInputType.Mouse, XInputType.Key).ToArray(),
        XGamepad = XInputf.GetRange(XInputType.AxisButton, XInputType.Button).ToArray(),
        XNative = XInputf.GetRange(XInputType.Mouse, XInputType.Key).ToArray();

        public Vector2 MouseMotion { get; private set; }
        private byte ResetMotion { get; set; }

        #region Local Node Events
        public override void _EnterTree()
        {
            Mode = ModeEnum.Open;

            // Add native device
            AddDevice(new(this, -1));

            // Add existing
            foreach (var existing in Input.GetConnectedJoypads())
            {
                OnDeviceChange(existing, true);
            }

            Input.JoyConnectionChanged += OnDeviceChange;
        }

        public override void _ExitTree()
        {
            Input.JoyConnectionChanged -= OnDeviceChange;
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
        private void OnDeviceChange(long udid, bool connected)
        {
            // Connected
            if (connected)
            {
                // Reconnecting device
                if (Devices.TryGetValue(udid, out var device))
                {
                    device.OnReconnect();
                }

                // New device
                else
                {
                    // Clamp device count
                    if (MaxDeviceCount < 1 || MaxDeviceCount > Devices.Count)
                        AddDevice(new(this, udid));
                }
            }

            // Disconnected
            else
            {
                // Device disconnected
                if (Devices.TryGetValue(udid, out var device))
                {
                    device.OnDisconnect();

                    if (Mode == ModeEnum.Open)
                    {
                        Devices.Remove(udid);
                        OnDeviceRem?.Invoke(device);
                    }
                }
            }
        }

        private void AddDevice(XDevice device)
        {
            Devices.Add(device.UDID, device);
            OnDeviceAdd?.Invoke(device);
        }

        public void RequestDevices(OnNewDeviceEventHandler Func)
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
        public bool IsPressed(XInput input)
        {
            foreach (var device in Devices.Values)
            {
                if (device.IsPressed(input)) return true;
            }

            return default;
        }

        public float GetValue(XInput input)
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

        public bool ListenForInput(out (XDevice device, XInput[] inputs)[] devices)
        {
            List<(XDevice device, XInput[] inputs)> list = null;
            foreach (var device in Devices.Values)
            {
                if (device.ListenForInput(out XInput[] input)) (list ??= new()).Add((device, input));
            }

            return (devices = list?.ToArray()) != null;
        }

        public bool ListenForInput(out (XDevice device, XInput[] inputs)[] devices, params XInput[] range)
        {
            List<(XDevice device, XInput[] inputs)> list = null;
            foreach (var device in Devices.Values)
            {
                if (device.ListenForInput(out var input, range)) (list ??= new()).Add((device, input));
            }

            return (devices = list?.ToArray()) != null;
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

        public bool ListenForInput(out (XDevice device, string[] inputs)[] devices, params string[] range)
        {
            List<(XDevice device, string[] inputs)> list = null;
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