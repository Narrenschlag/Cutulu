using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Servers to assign special input maps for specific listenings. Mainly used to ignore the up value of left and right triggers on gamepads.
    /// </summary>
    public partial class XInputSetup : Node
    {
        [Export] private InputDeviceManager Manager { get; set; }

        private static readonly Dictionary<int, DeviceSet> Devices = new();
        public bool AllReady { get; private set; }



        public override void _EnterTree()
        {
            AllReady = true;

            if (Manager.Devices.Count > 0)
            {
                foreach (var device in Manager.Devices.Values)
                {
                    _AddDevice(device);
                }
            }

            Manager.OnNewDevice += _AddDevice;
            Manager.OnRemoveDevice += _RemDevice;
        }

        public override void _ExitTree()
        {
            Manager.OnNewDevice -= _AddDevice;
            Manager.OnRemoveDevice -= _RemDevice;
        }

        public override void _Process(double delta)
        {
            if (AllReady == false)
            {
                foreach (var device in Devices.Values)
                {
                    if (device?.Ready == false) device?._Process(ref delta);
                }
            }
        }



        private void _AddDevice(InputDevice device)
        {
            AllReady = false;

            DeviceSet set = device.DeviceType == InputDeviceType.Native ? new NativeSet(device) : new GamepadSet(device);

            Devices.Add(device.DeviceId, set);
            set.OnReady += _OnReady;
            _OnReady();
        }

        private void _RemDevice(InputDevice device)
        {
            Devices.Remove(device.DeviceId);
        }

        private void _OnReady()
        {
            AllReady = true;

            foreach (var set in Devices?.Values)
            {
                if ((AllReady = set.Ready) == false) break;
            }

            Debug.Log($"All Devices ready: {AllReady}");
        }



        private class DeviceSet
        {
            protected readonly InputDevice Device;
            public virtual bool Ready => true;

            public delegate void Event();
            public Event OnReady;

            public DeviceSet(InputDevice device)
            {
                Device = device;

                Debug.Log($"Setting up {Device.DeviceName} as {GetType().Name}");
            }

            public virtual void _Process(ref double delta)
            {
                if (Device.ListenForInput(out XInput[] inputs) && inputs?.Length > 0)
                {
                    var str = $"{Device.DeviceName}:";

                    for (int i = 0; i < inputs.Length; i++)
                    {
                        str += $"...{inputs[i]}";
                    }

                    Debug.Log(str);
                }
            }
        }

        private class NativeSet : DeviceSet
        {
            public NativeSet(InputDevice device) : base(device)
            {

            }

            public override void _Process(ref double delta)
            {
                base._Process(ref delta);
            }
        }

        private class GamepadSet : DeviceSet
        {
            // Problematic Inputs
            public XInput TriggerRight { get; private set; }
            public XInput TriggerLeft { get; private set; }

            public override bool Ready => Device.SpecificListenInputs?.Length > 0;
            private readonly List<XInput> TriggerInputs;

            public GamepadSet(InputDevice device) : base(device)
            {
                Device.SpecificListenInputs = System.Array.Empty<XInput>(); // Ignore all inputs
                TriggerRight = XInput.Invalid;
                TriggerLeft = XInput.Invalid;

                // Only listen to the press values of potential trigger axies
                TriggerInputs = new();
                for (XInput i = XInput.AxisButton0; i <= XInput.AxisButton11; i += 2)
                {
                    TriggerInputs.Add(i);
                }
            }

            public override void _Process(ref double delta)
            {
                // Required to ignore the up values of trigger inputs
                if (TriggerLeft == XInput.Invalid)
                {
                    if (Device.ListenForInput(out var inputs, TriggerInputs.ToArray()) && inputs?.Length > 0)
                    {
                        TriggerInputs.Remove(inputs[0]);
                        TriggerLeft = inputs[0] + 1;
                    }
                }

                else if (TriggerRight == XInput.Invalid)
                {
                    if (Device.ListenForInput(out var inputs, TriggerInputs.ToArray()) && inputs?.Length > 0)
                    {
                        TriggerRight = inputs[0] + 1;
                        TriggerInputs.Clear();

                        var list = new List<XInput>(Device.Manager.XGamepad);
                        list.Remove(TriggerRight);
                        list.Remove(TriggerLeft);

                        Device.SpecificListenInputs = list.ToArray();

                        Debug.Log($"Registered Triggers\nRight: {TriggerRight}\nLeft: {TriggerLeft}");
                        OnReady?.Invoke();
                    }
                }

                else base._Process(ref delta);
            }
        }


    }
}