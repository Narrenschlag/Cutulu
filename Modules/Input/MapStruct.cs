namespace Cutulu.Input
{
    using System.Collections.Generic;

    public readonly struct MapStruct
    {
        public readonly Dictionary<string, MapEntryStruct> Mapping = new();

        public MapStruct() => Mapping = new();
        public MapStruct(string name, params InputEnum[] overrides) : this()
        {
            Add(name, overrides);
        }

        public static string ModifyString(string name) => name.Trim().ToLower();

        public void Add(string name, params InputEnum[] overrides)
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

            void update(ref MapEntryStruct entry)
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

        public void Set(string name, params InputEnum[] inputs)
        {
            Clear(name);
            Add(name, inputs);
        }
    }
}