using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public partial class InputDeviceManager : Node
    {
        [Export] public CutuluInputMap InputMapFile { get; set; }
        [Export] public int MaxDeviceCount { get; set; } = 4;

        public Dictionary<long, InputDevice> Devices { get; private set; }
        public Dictionary<string, InputSet> Map { get; private set; }
        public ModeEnum Mode { get; set; }

        public delegate void OnNewDeviceEventHandler(InputDevice device);
        public OnNewDeviceEventHandler OnNewDevice, OnRemoveDevice;

        public Vector2 MouseMotion { get; private set; }
        private byte ResetMotion { get; set; }

        private InputTranslation GetTranslation() => Translation ??= CreateTranslation();
        public virtual InputTranslation CreateTranslation() => new();
        public InputTranslation Translation { get; private set; }

        #region Local Node Events
        public override void _EnterTree()
        {
            Map = InputMapFile.NotNull() ? InputMapFile.GetMap() : new();
            Debug.Log($"{Map.Count} inputs have been registered.");

            Mode = ModeEnum.Open;
            Devices = new();

            // Add native device
            AddDevice(new(this, GetTranslation(), -1));

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
                        AddDevice(new(this, GetTranslation(), udid));
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
                        OnRemoveDevice?.Invoke(device);
                        Devices.Remove(udid);
                    }
                }
            }
        }

        private void AddDevice(InputDevice device)
        {
            Devices.Add(device.UDID, device);
            OnNewDevice?.Invoke(device);
        }
        #endregion

        #region Read Input
        // Input Map
        public bool GetInput(int iUDID, string inputName)
        => Map.TryGetValue(inputName, out var inputSet) && Devices.TryGetValue(iUDID, out var device) && inputSet.IsPressed(device);

        // Input Map
        public bool GetInput(string inputName, float threshold = 0.5f)
        {
            if (Map.TryGetValue(inputName, out var inputSet))
            {
                foreach (var device in Devices.Values)
                {
                    if (inputSet.IsPressed(device, threshold)) return true;
                }
            }

            return default;
        }

        // Input Map
        public float GetInputValue(int iUDID, string inputName)
        => Map.TryGetValue(inputName, out var inputSet) && Devices.TryGetValue(iUDID, out var device) ? inputSet.Value(device) : default;

        // Input Map
        public float GetInputValue(string inputName)
        {
            if (Map.TryGetValue(inputName, out var inputSet) == false) return default;
            var maxValue = 0f;

            foreach (var device in Devices.Values)
            {
                var value = inputSet.Value(device);

                if (value.abs() >= 1) return value;
                else if (value.abs() > maxValue.abs()) maxValue = value;
            }

            return maxValue;
        }

        public bool GetKeyDown(int iUDID, ref bool downRef, InputCode code, float threshold = 0.5f)
        => Devices.TryGetValue(iUDID, out var device) && device.GetKeyDown(code, ref downRef, threshold);

        public bool GetKeyUp(int iUDID, ref bool upRef, InputCode code, float threshold = 0.5f)
        => Devices.TryGetValue(iUDID, out var device) && device.GetKeyUp(code, ref upRef, threshold);

        public bool GetKey(int iUDID, InputCode code, float threshold = 0.5f)
        => Devices.TryGetValue(iUDID, out var device) && device.GetKey(code, threshold);

        public float GetValue(int iUDID, InputCode code)
        => Devices.TryGetValue(iUDID, out var device) ? device.GetValue(code) : default;

        public bool GetKeyDown(InputCode code, ref bool downRef, float threshold = 0.5f)
        {
            var previous = downRef;

            downRef = GetKey(code, threshold);
            return previous == false && downRef;
        }

        public bool GetKeyUp(InputCode code, ref bool upRef, float threshold = 0.5f)
        {
            var previous = upRef;

            upRef = GetKey(code, threshold);
            return previous && upRef == false;
        }

        public bool GetKey(InputCode code, float threshold = 0.5f)
        {
            foreach (var device in Devices.Values)
            {
                if (Mathf.Abs(device.GetValue(code)) >= threshold) return true;
            }

            return default;
        }

        public float GetValue(InputCode code)
        {
            var maxValue = 0f;

            foreach (var device in Devices.Values)
            {
                var value = device.GetValue(code);

                if (value.abs() >= 1) return value;
                else if (value.abs() > maxValue.abs()) maxValue = value;
            }

            return maxValue;
        }
        #endregion

        public enum ModeEnum
        {
            Open, Locked
        }
    }
}