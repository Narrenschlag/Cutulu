using System.Collections.Generic;
using System;
using Godot;

namespace Cutulu
{
    public readonly struct XInputMap
    {
        public readonly Dictionary<string, Entry> Mapping;

        public XInputMap() { Mapping = new(); }
        public XInputMap(string name, params XInput[] overrides) : this()
        {
            Override(name, overrides);
        }

        public static string ModifyString(string name) => name.Trim().ToLower();

        public void Override(string name, params XInput[] overrides)
        {
            if (overrides == null) return;

            if (Mapping.TryGetValue(name = ModifyString(name), out var entry) == false)
            {
                update(ref entry);
                Mapping.Add(name, entry);
            }

            else
            {
                update(ref entry);
                Mapping[name] = entry;
            }

            void update(ref Entry entry)
            {
                for (int i = 0; i < overrides.Length; i++)
                {
                    entry += overrides[i];
                }
            }
        }

        public void Clear() => Mapping.Clear();
        public void Clear(string name)
        {
            if (Mapping.ContainsKey(name = ModifyString(name)))
            {
                Mapping.Remove(name);
            }
        }

        public readonly struct Entry
        {
            public readonly int[] Inputs;

            public Entry()
            {
                Inputs = null;
            }

            public Entry(params int[] inputs)
            {
                Inputs = inputs;
            }

            public Entry(params XInput[] inputs)
            {
                if (inputs?.Length < 1) Inputs = null;
                else
                {
                    Inputs = new int[inputs.Length];
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        Inputs[i] = (int)inputs[i];
                    }
                }
            }

            public bool IsPressed(int deviceId)
            {
                for (int i = 0; i < Inputs?.Length; i++)
                {
                    if (((XInput)Inputs[i]).IsPressed(deviceId)) return true;
                }

                return default;
            }

            public float GetValue(int deviceId)
            {
                float value = default;
                float max = default;

                for (int i = 0; i < Inputs?.Length; i++)
                {
                    if (Mathf.Abs(value = ((XInput)Inputs[i]).GetValue(deviceId)) >= 1f) return value;
                    if (Math.Abs(value) > Mathf.Abs(max)) max = value;
                }

                return max;
            }

            public static Entry operator +(Entry entry, XInput input)
            {
                var list = entry.Inputs == null ? new() : new List<int>(entry.Inputs);
                if (list.Contains((int)input)) return entry;

                list.Add((int)input);
                return new(list.ToArray());
            }

            public static Entry operator -(Entry entry, XInput input)
            {
                if (entry.Inputs == null) return entry;

                var list = new List<int>(entry.Inputs);
                if (list.Contains((int)input))
                {
                    if (list.Count < 2) return new();
                    else list.Remove((int)input);

                    return new(list.ToArray());
                }

                return entry;
            }
        }
    }
}