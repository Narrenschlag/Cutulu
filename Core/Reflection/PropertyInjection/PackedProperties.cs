namespace Cutulu.Core
{
    public struct PackedProperties
    {
        public PackedProperty[] Properties { get; set; }

        public PackedProperties() { }

        public PackedProperties(object _ref, params string[] _name) : this(_ref, GetIdx(_name, _ref.GetType())) { }

        public PackedProperties(object _ref, params int[] _idx)
        {
            try
            {
                Properties = new PackedProperty[_idx.Length];

                for (int i = 0; i < Properties.Length; i++)
                {
                    Properties[i] = new(_ref, _idx[i]);
                }
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Packing failed. {_ex.Message}");
            }
        }

        public readonly bool Unpack(object _ref)
        {
            try
            {
                foreach (var _property in Properties)
                {
                    _property.Unpack(_ref);
                }

                return true;
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Unpacking failed. {_ex.Message}");
                return false;
            }
        }

        public static int[] GetIdx(string[] _name, System.Type _type)
        {
            var _idx = new int[_name.Size()];

            if (_idx.Length > 0)
            {
                var _manager = ParameterManager.Open(_type);

                for (int i = 0; i < _idx.Length; i++)
                {
                    _idx[i] = _manager.GetIndex(_name[i]);
                }
            }

            return _idx;
        }
    }
}