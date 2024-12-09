namespace Cutulu
{
    using System.Collections.Generic;
    using System;
    using Godot;

    /// <summary>
    /// Provides utility for indexing and reading inputs.
    /// Contains information about the device and presets. 
    /// </summary>
    public partial class InputDevice
    {
        public bool RemoteAssigned { get; set; } = false;
        public long RemoteId { get; set; }

        public readonly Enum Type;
        public readonly int DeviceId;
        public readonly string Name;

        public Action<InputKey, object> JustPressed, JustReleased;
        public Action<InputKey, bool, object> Pressed;

        public InputDevice(int deviceId)
        {
            DeviceId = deviceId;

            switch (deviceId)
            {
                case -1:
                    Type = Enum.Keyboard;
                    Name = Type.ToString();
                    break;

                case -2:
                    Type = Enum.Mouse;
                    Name = Type.ToString();
                    break;

                default:
                    Type = Enum.Gamepad;
                    Name = Input.GetJoyName(deviceId);
                    break;
            }
        }

        #region Pressed

        private readonly Dictionary<InputKey, object> PressedKey = new();

        public bool IsAnyPressed() => IsAnyPressed(out InputKey[] _);

        public bool IsAnyPressed(out InputKey[] keys)
        {
            if (PressedKey.Count < 1)
            {
                keys = null;
                return false;
            }

            keys = new List<InputKey>(PressedKey.Keys).ToArray();
            return keys.NotEmpty();
        }

        public bool IsAnyPressed(out InputIndex[] indexes)
        {
            if (Preset == null || Preset.Array.IsEmpty())
            {
                indexes = null;
                return false;
            }

            var list = new List<InputIndex>();
            foreach (var index in Preset.Array)
            {
                if (index.IsPressed(this)) list.Add(index);
            }

            indexes = list.ToArray();
            return indexes.NotEmpty();
        }

        public bool IsPressed(string indexName) => IsPressed(this[indexName]);
        public bool IsPressed(InputIndex index) => index.IsPressed(this);
        public bool IsPressed(InputKey key) => PressedKey.ContainsKey(key);

        public float GetValue(string indexName) => GetValue(this[indexName]);
        public float GetValue(InputIndex index) => index.GetValue(this);
        public float GetValue(InputKey key)
        {
            if (IsPressed(key)) return key.Type switch
            {
                InputKey.Enum.Keyboard => 1f,
                InputKey.Enum.MouseButton => 1f,
                InputKey.Enum.MouseAxis when PressedKey[key] is float f => Mathf.Clamp(f, 0f, 1f),
                InputKey.Enum.JoyButton => 1f,
                InputKey.Enum.JoyAxis when PressedKey[key] is float f => Mathf.Clamp(f, 0f, 1f),
                _ => 1f,
            };

            else return 0f;
        }

        public byte GetValue255(string indexName) => GetValue255(this[indexName]);
        public byte GetValue255(InputIndex index) => index.GetValue255(this);
        public byte GetValue255(InputKey key) => GetValue(key).FloatToByte01();

        public void HandlerPressed(InputKey key, bool active, object value)
        {
            if (active)
            {
                PressedKey[key] = value;
            }

            else
            {
                PressedKey.TryRemove(key);
            }
        }

        #endregion

        #region Just Pressed & Just Released

        private readonly List<InputKey> JustPressedKeys = new(), JustReleasedKeys = new();

        public bool IsJustPressed(string indexName) => IsJustPressed(this[indexName]);
        public bool IsJustPressed(InputKey key) => JustPressedKeys.Contains(key);
        public bool IsJustPressed(InputIndex index) => index.IsJustPressed(this);

        public bool IsJustReleased(string indexName) => IsJustReleased(this[indexName]);
        public bool IsJustReleased(InputKey key) => JustReleasedKeys.Contains(key);
        public bool IsJustReleased(InputIndex index) => index.IsJustReleased(this);

        public void HandlerJustPressed(InputKey key, bool add, bool pressed, object value)
        {
            if (add)
            {
                (pressed ? JustPressedKeys : JustReleasedKeys).TryAdd(key);
                (pressed ? JustPressed : JustReleased)?.Invoke(key, value);
            }

            else
            {
                (pressed ? JustPressedKeys : JustReleasedKeys).TryRemove(key);
            }
        }

        #endregion

        #region Indexes

        private readonly Dictionary<string, InputIndex> Indexes = new();

        public ICollection<string> IndexesRegistered => Indexes.Keys;

        public bool IsIndexed(string indexName) => Indexes.ContainsKey(indexName);
        public InputIndex this[string index]
        {
            get => Indexes.TryGetValue(index, out var i) ? i : default;

            set
            {
                Indexes[index] = value;

                // Update preset data
                Preset.Array = new List<InputIndex>(Indexes.Values).ToArray();
            }
        }

        public partial struct InputIndex
        {
            public string Identifier { get; set; }
            public InputKey[] Keys { get; set; }
            public bool And { get; set; }

            public InputIndex(string identifier, bool and, params InputKey[] keys)
            {
                Keys = keys.IsEmpty() ? Array.Empty<InputKey>() : keys;
                Identifier = identifier;
                And = and;
            }

            public readonly bool IsJustReleased(InputDevice device) => IsPressed(device) && ValidatePress(device.JustReleasedKeys, false);
            public readonly bool IsJustPressed(InputDevice device) => IsPressed(device) && ValidatePress(device.JustPressedKeys, false);
            public readonly bool IsPressed(InputDevice device) => ValidatePress(device.PressedKey.Keys, And);

            public readonly byte GetValue255(InputDevice device) => GetValue(device).FloatToByte01();
            public readonly float GetValue(InputDevice device)
            {
                if (IsPressed(device))
                {
                    var max = 0f;

                    foreach (var key in Keys)
                    {
                        max = Mathf.Max(max, device.GetValue(key));
                    }

                    return max;
                }

                return 0f;
            }

            private readonly bool ValidatePress(ICollection<InputKey> keys, bool and)
            {
                if (keys.IsEmpty() || Keys.IsEmpty()) return false;

                foreach (var key in Keys)
                {
                    var contained = keys.Contains(key);

                    if (and && !contained) return false;
                    else if (!and && contained) return true;
                }

                return Keys.Length > 0;
            }
        }

        #endregion

        #region Preset

        private InputPreset PresetData { get; set; }

        public InputPreset Preset
        {
            get => PresetData ??= new();

            set
            {
                PresetData = value;
                Indexes.Clear();

                Preset.Type = Type;

                var array = Preset.Array;
                if (array.NotEmpty())
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        this[array[i].Identifier] = array[i];
                    }
                }
            }
        }

        #endregion

        public enum Enum : byte
        {
            Invalid,

            Keyboard,
            Mouse,

            Gamepad,
        }
    }
}