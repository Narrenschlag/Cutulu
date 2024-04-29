using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class InputMap : Resource
    {
        [Export] public InputSet[] Default { get; set; }
        [Export] public InputSet[] Override { get; set; }

        public Dictionary<string, InputSet> GetMap(params InputSet[] additionalOverrides)
        => CreateMap(additionalOverrides, Override, Default);

        public static Dictionary<string, InputSet> CreateMap(params InputSet[][] layersInOrder)
        {
            var map = new Dictionary<string, InputSet>();

            if (layersInOrder != null)
            {
                for (int i = 0; i < layersInOrder.Length; i++)
                {
                    register(layersInOrder[i]);
                }
            }

            return map;

            void register(InputSet[] array)
            {
                if (array == null) return;

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] != null && map.ContainsKey(array[i].InputName) == false)
                    {
                        map.Add(array[i].InputName, array[i]);
                    }
                }
            }
        }
    }
}