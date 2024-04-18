using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public partial class InputDeviceManager : Node
    {
        public Dictionary<long, InputDevice> Devices { get; private set; }
        public ModeEnum Mode { get; set; }

        public Vector2 MouseMotion { get; private set; }
        private byte resetMotion { get; set; }

        #region Local Node Events
        public override void _EnterTree()
        {
            Mode = ModeEnum.Open;
            Devices = new()
            {
                {-1, new(this, -1)} // Add native device
            };

            Input.JoyConnectionChanged += OnDeviceChange;
        }

        public override void _ExitTree()
        {
            Input.JoyConnectionChanged -= OnDeviceChange;
        }

        public override void _Process(double delta)
        {
            if (resetMotion > 0 && resetMotion++ > 1)
            {
                MouseMotion = default;
                resetMotion = 0;
            }
        }

        public override void _Input(InputEvent @event)
        {
            switch (@event)
            {
                // Mouse motion
                case InputEventMouseMotion _motion:
                    MouseMotion = _motion.Relative;
                    resetMotion = 1;
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
                if (Devices.TryGetValue(udid, out var entry))
                {
                    entry.OnReconnect();
                }

                // New device
                else
                {
                    var info = new InputDevice(this, udid);
                    Devices.Add(udid, info);
                }
            }

            // Disconnected
            else
            {
                if (Mode == ModeEnum.Open)
                {
                    Mode = ModeEnum.Locked;
                    Debug.Log($"Locked.");
                }

                // Device disconnected
                if (Devices.TryGetValue(udid, out var entry))
                {
                    entry.OnDisconnect();

                    if (Mode == ModeEnum.Open) Devices.Remove(udid);
                }
            }
        }
        #endregion

        #region Read Input
        public bool GetKeyDown(int iUDID, ref bool downRef, InputCode code, float threshold = 0.5f, params string[] nativeInputNames)
        => Devices.TryGetValue(iUDID, out var device) ? device.GetKeyDown(code, ref downRef, threshold, nativeInputNames) : default;

        public bool GetKeyUp(int iUDID, ref bool upRef, InputCode code, float threshold = 0.5f, params string[] nativeInputNames)
        => Devices.TryGetValue(iUDID, out var device) ? device.GetKeyUp(code, ref upRef, threshold, nativeInputNames) : default;

        public bool GetKey(int iUDID, InputCode code, float threshold = 0.5f, params string[] nativeInputNames)
        => Devices.TryGetValue(iUDID, out var device) ? device.GetKey(code, threshold, nativeInputNames) : default;

        public float GetValue(int iUDID, InputCode code, params string[] nativeInputNames)
        => Devices.TryGetValue(iUDID, out var device) ? device.GetValue(code, nativeInputNames) : default;

        public bool GetKeyDown(InputCode code, ref bool downRef, float threshold = 0.5f, params string[] nativeInputNames)
        {
            var previous = downRef;

            downRef = GetKey(code, threshold, nativeInputNames);
            return previous == false && downRef;
        }

        public bool GetKeyUp(InputCode code, ref bool upRef, float threshold = 0.5f, params string[] nativeInputNames)
        {
            var previous = upRef;

            upRef = GetKey(code, threshold, nativeInputNames);
            return previous && upRef == false;
        }

        public bool GetKey(InputCode code, float threshold = 0.5f, params string[] nativeInputNames) => GetValue(code, nativeInputNames) >= threshold;
        public float GetValue(InputCode code, params string[] nativeInputNames)
        {
            var maxValue = 0f;

            foreach (var device in Devices.Values)
            {
                var value = device.GetValue(code, nativeInputNames);

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