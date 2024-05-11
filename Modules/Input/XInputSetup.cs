using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Servers to assign special input maps for specific listenings. Mainly used to ignore the up value of left and right triggers on gamepads.
    /// </summary>
    public partial class XInputSetup : Node
    {
        [Export] private XInputManager Manager { get; set; }

        public readonly Dictionary<int, DeviceSet> Devices = new();
        public delegate void DeviceAddedEvent(DeviceSet set);
        public bool AllReady { get; private set; }

        public DeviceAddedEvent OnDeviceAdd;



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



        private void _AddDevice(XDevice device)
        {
            AllReady = false;

            DeviceSet set = device.DeviceType == XDeviceType.Native ? new NativeSet(device) : new GamepadSet(device);

            Devices.Add(device.DeviceId, set);
            set.OnReady += _OnReady;
            _OnReady();

            OnDeviceAdd?.Invoke(set);
        }

        private void _RemDevice(XDevice device)
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



        public void RequestDevices(DeviceAddedEvent Func)
        {
            if (Func != null)
            {
                foreach (var device in Devices?.Values)
                {
                    Func.Invoke(device);
                }
            }
        }



        public class DeviceSet
        {
            public readonly XDevice Device;
            public virtual bool Ready => true;

            public delegate void Event();
            public Event OnReady;

            public DeviceSet(XDevice device)
            {
                Device = device;

                //Debug.Log($"Setting up {Device.DeviceName} as {GetType().Name}");
            }

            public virtual void _Process(ref double delta)
            {
                // Will only be called if wanted
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
            public NativeSet(XDevice device) : base(device)
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
            public XInput[] IgnoreTriggers { get; private set; }

            public override bool Ready => Device.SpecificListenInputs?.Length > 0;
            private readonly List<XInput> TriggerInputs;

            public GamepadSet(XDevice device) : base(device)
            {
                Device.SpecificListenInputs = System.Array.Empty<XInput>(); // Ignore all inputs
                IgnoreTriggers = null;

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
                if (IgnoreTriggers?.Length != 2)
                {
                    if (Device.ListenForInput(out var inputs, TriggerInputs.ToArray()) && inputs?.Length == 2)
                    {
                        TriggerInputs.Clear();
                        IgnoreTriggers = new XInput[2]{
                            inputs[0] + 1,
                            inputs[1] + 1,
                        };

                        var list = new List<XInput>(Device.Manager.XGamepad);
                        list.Remove(IgnoreTriggers[0]);
                        list.Remove(IgnoreTriggers[1]);

                        Device.SpecificListenInputs = list.ToArray();
                        OnReady?.Invoke();
                    }
                }

                else base._Process(ref delta);
            }
        }


    }
}