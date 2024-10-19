namespace Cutulu.Input
{
    using System.Collections.Generic;
    using System;

    using GD = Godot.Input;

    public partial class Manager
    {
        public static readonly InputEnum[]
        All = Backend.GetRange(InputTypeEnum.AxisButton, InputTypeEnum.Button, InputTypeEnum.Mouse, InputTypeEnum.Key).ToArray(),
        Gamepad = Backend.GetRange(InputTypeEnum.AxisButton, InputTypeEnum.Button).ToArray(),
        Native = Backend.GetRange(InputTypeEnum.Mouse, InputTypeEnum.Key).ToArray();

        public readonly Dictionary<long, Device> Devices = new();
        public bool Running { get; private set; }
        public ModeEnum Mode { get; set; }

        public event Action<Device> AddedDevice, RemovedDevice;

        public Manager()
        {

        }

        public virtual void Start()
        {
            if (Running) return;

            Devices.Clear();

            // Not yet setup
            Mode = ModeEnum.Open;

            // Add native device
            _AddedDevice(new(-1));

            // Add existing
            foreach (var existing in GD.GetConnectedJoypads())
            {
                _ChangedDevice(existing, true);
            }

            GD.JoyConnectionChanged += _ChangedDevice;
        }

        public virtual void Close()
        {
            if (Running == false) return;

            GD.JoyConnectionChanged -= _ChangedDevice;
        }

        private void _ChangedDevice(long udid, bool connected)
        {
            // Connected
            if (connected)
            {
                // Reconnecting device
                if (Devices.TryGetValue(udid, out var device))
                {
                    device._Reconnected();
                }

                // New device
                else
                {
                    _AddedDevice(new(udid));
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

        private void _AddedDevice(Device device)
        {
            Devices.Add(device.UDID, device);
            AddedDevice?.Invoke(device);
        }

        public enum ModeEnum
        {
            Open, Locked
        }
    }
}