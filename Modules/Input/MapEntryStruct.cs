namespace Cutulu.Input
{
    using System.Collections.Generic;
    using System;
    using Godot;

    public readonly struct MapEntryStruct
    {
        public readonly int[] Inputs;

        public MapEntryStruct()
        {
            Inputs = null;
        }

        public MapEntryStruct(params int[] inputs)
        {
            Inputs = inputs;
        }

        public MapEntryStruct(params InputEnum[] inputs)
        {
            if (inputs?.Length < 1) Inputs = Array.Empty<int>();

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
                if (((InputEnum)Inputs[i]).IsPressed(deviceId)) return true;
            }

            return default;
        }

        public float GetValue(int deviceId)
        {
            var value = default(float);
            var max = default(float);

            for (int i = 0; i < Inputs?.Length; i++)
            {
                if (Mathf.Abs(value = ((InputEnum)Inputs[i]).GetValue(deviceId)) >= 1f) return value;
                if (Mathf.Abs(value) > Mathf.Abs(max)) max = value;
            }

            return max;
        }

        public static MapEntryStruct operator +(MapEntryStruct entry, InputEnum input)
        {
            var list = entry.Inputs == null ? new() : new List<int>(entry.Inputs);
            if (list.Contains((int)input)) return entry;

            list.Add((int)input);
            return new(list.ToArray());
        }

        public static MapEntryStruct operator -(MapEntryStruct entry, InputEnum input)
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