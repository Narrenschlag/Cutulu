using System.Collections.Generic;
using Godot;
using Punk;

namespace Cutulu
{
    public partial class InputDeviceManager : Node
    {
        public Dictionary<long, InputDevice> Devices { get; private set; }
        public ModeEnum Mode { get; set; }

        public Vector2 MouseMotion { get; private set; }
        private byte resetMotion { get; set; }

        public override void _EnterTree()
        {
            Mode = ModeEnum.Open;
            Devices = new();

            Input.JoyConnectionChanged += OnDeviceChange;
        }

        public override void _ExitTree()
        {
            Input.JoyConnectionChanged -= OnDeviceChange;
        }

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
            // Mouse motion event
            if (@event is InputEventMouseMotion motion)
            {
                MouseMotion = motion.Relative;
                resetMotion = 1;
            }
        }

        public enum ModeEnum
        {
            Open, Locked
        }
    }
}