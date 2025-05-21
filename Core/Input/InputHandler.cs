namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System;
    using Godot;

    /// <summary>
    /// Processes incomming input and devices connected.
    /// </summary>
    public partial class InputHandler : Node
    {
        private readonly List<(byte TimeStamp, int DeviceId, InputKey Key, bool justPressed)> Queue = [];
        private readonly Dictionary<int, Dictionary<InputKey, byte>> MouseQueue = [];
        public readonly Dictionary<int, InputDevice> Devices = [];

        [Export] public float MouseDeadZone = 2.0f;
        [Export] public float MouseMaxVal = 8.0f;
        [Export] public float DeadZone = 0.2f;

        [ExportCategory("Advanced")]
        [Export] public bool SplitMouseAndKeyboard = false;
        [Export] public bool EnableMouseButton = false;
        [Export] public bool EnableMouseAxis = false;

        public Action<InputDevice> DeviceConnected, DeviceDisconnected;

        private byte TimeStamp { get; set; }

        public override void _EnterTree()
        {
            _JoyChanged(-1, true);

            _JoyChanged(-2, SplitMouseAndKeyboard && (EnableMouseButton || EnableMouseAxis));

            foreach (var deviceId in Input.GetConnectedJoypads())
                _JoyChanged(deviceId, true);

            Input.JoyConnectionChanged += _JoyChanged;
        }

        public override void _ExitTree()
        {
            Input.JoyConnectionChanged -= _JoyChanged;
        }

        private void _JoyChanged(long longId, bool connected)
        {
            var deviceId = (int)longId;

            if (connected && Devices.TryGetValue(deviceId, out var device) && device != null) return;

            if (connected) Devices[deviceId] = device = new InputDevice(deviceId);

            else if (Devices.TryGetValue(deviceId, out device))
                Devices.Remove(deviceId);

            if (device != null)
            {
                if (connected) DeviceConnected?.Invoke(device);
                else DeviceDisconnected?.Invoke(device);
            }
        }

        public override void _UnhandledInput(InputEvent @event) => HandleInput(@event);
        public override void _Input(InputEvent @event) => HandleInput(@event);
        private void HandleInput(InputEvent @event)
        {
            int deviceIdx;
            InputKey key;
            object value;
            bool valid;

            switch (@event)
            {
                case InputEventKey keyboard:
                    key = new(InputKey.Enum.Keyboard, (int)keyboard.Keycode);
                    deviceIdx = -1;

                    valid = keyboard.Pressed;
                    value = null;
                    break;

                case InputEventMouseButton mouseButton:
                    if (EnableMouseButton == false) return;

                    key = new(InputKey.Enum.MouseButton, (int)mouseButton.ButtonIndex);
                    deviceIdx = SplitMouseAndKeyboard ? -2 : -1;

                    valid = mouseButton.Pressed;
                    value = null;
                    break;

                case InputEventMouseMotion mouseAxis:
                    if (EnableMouseAxis == false) return;

                    deviceIdx = SplitMouseAndKeyboard ? -2 : -1;

                    var x = mouseAxis.Relative.X;
                    var y = mouseAxis.Relative.Y;

                    valid = Mathf.Abs(x) > MouseDeadZone || Mathf.Abs(y) > MouseDeadZone;
                    key = new(InputKey.Enum.MouseAxis, valid == false ? 0
                    : Mathf.Abs(x) > Mathf.Abs(y)
                    ? x > 0 ? 1 : 2
                    : y > 0 ? 3 : 4
                    );

                    value = Mathf.Clamp(key.Id < 1 ? 0f : Mathf.Abs(key.Id < 2 ? x : y) / MouseMaxVal, 0f, 1f);
                    break;

                case InputEventJoypadButton joyButton:
                    key = new(InputKey.Enum.JoyButton, (int)joyButton.ButtonIndex);
                    deviceIdx = joyButton.Device;

                    valid = joyButton.Pressed;
                    value = null;
                    break;

                case InputEventJoypadMotion joyAxis:
                    key = new(InputKey.Enum.JoyAxis, (int)joyAxis.Axis * 2 + 1);
                    deviceIdx = joyAxis.Device;

                    valid = joyAxis.AxisValue > DeadZone;
                    value = Mathf.Abs(joyAxis.AxisValue);

                    // Used for inverting axis values
                    apply(deviceIdx, -joyAxis.AxisValue > DeadZone, new(key.Type, key.Id - 1), value);
                    break;

                case InputEventMidi midi:
                    return; // TODO: research this.

                default: return;
            }

            apply(deviceIdx, valid, key, value);

            void apply(int deviceId, bool valid, InputKey key, object value)
            {
                // Device not found
                if (Devices.TryGetValue(deviceIdx, out InputDevice device) == false || device == null)
                {
                    Debug.LogR($"[color=red]Input Device {deviceId} is not contained in registry");
                    return;
                }

                var pressed = device.IsPressed(key);

                if (valid)
                {
                    device.HandlerPressed(key, true, value);
                    device.Pressed?.Invoke(key, pressed, value);

                    if (pressed == false)
                    {
                        device.HandlerJustPressed(key, true, true, value);

                        Queue.Add((TimeStamp, deviceId, key, true));
                    }

                    if (key.Type == InputKey.Enum.MouseAxis)
                    {
                        if (MouseQueue.TryGetValue(deviceId, out var mouseQueue) == false)
                        {
                            MouseQueue[deviceId] = mouseQueue = [];
                        }

                        mouseQueue[key] = TimeStamp;
                    }
                }

                else
                {
                    device.HandlerPressed(key, false, null);

                    if (pressed) ReleasedKey(device, key, value);
                }
            }
        }

        private void ReleasedKey(InputDevice device, InputKey key, object value)
        {
            device.HandlerJustPressed(key, true, false, value);

            Queue.Add((TimeStamp, device.DeviceId, key, false));
        }

        public override void _Process(double delta)
        {
            for (int i = Queue.Count - 1; i >= 0; i--)
            {
                if (Queue[i].TimeStamp != TimeStamp && Devices.TryGetValue(Queue[i].DeviceId, out var device))
                {
                    device.HandlerJustPressed(Queue[i].Key, false, Queue[i].justPressed, null);
                    Queue.RemoveAt(i);
                }
            }

            var mouseKeys = MouseQueue.Keys;
            foreach (var DeviceId in mouseKeys)
            {
                if (Devices.TryGetValue(DeviceId, out var device) == false) continue;

                var mouseQueue = MouseQueue[DeviceId];
                var keys = mouseQueue.Keys;

                foreach (var key in keys)
                {
                    if (mouseQueue[key] != TimeStamp)
                    {
                        device.HandlerPressed(key, false, null);
                        ReleasedKey(device, key, null);
                    }
                }
            }

            TimeStamp++;
        }
    }
}