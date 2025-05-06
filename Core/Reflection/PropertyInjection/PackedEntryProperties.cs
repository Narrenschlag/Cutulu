namespace Cutulu.Core
{
    using System.Collections.Generic;

    public partial struct PackedEntryProperties
    {
        public byte[] KeyBuffer { get; set; }
        public PackedProperty[] Properties { get; set; }

        public PackedEntryProperties() { }

        public PackedEntryProperties(object _ref, object _key, params int[] _idx)
        {
            try
            {
                Properties = new PackedProperty[_idx.Length];
                KeyBuffer = _key.Encode();

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

        public PackedEntryProperties(object _ref, object _key, params string[] _name)
        {
            try
            {
                Properties = new PackedProperty[_name.Length];
                KeyBuffer = _key.Encode();

                for (int i = 0; i < Properties.Length; i++)
                {
                    Properties[i] = new(_ref, _name[i]);
                }
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Packing failed. {_ex.Message}");
            }
        }

        public readonly bool Unpack<KEY>(out KEY _key, out PackedProperty[] _properties)
        {
            try
            {
                _key = KeyBuffer.Decode<KEY>();
                _properties = Properties;

                return true;
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Unpacking failed. {_ex.Message}");

                _properties = default;
                _key = default;

                return false;
            }
        }

        public readonly bool Unpack<KEY, VALUE>(Dictionary<KEY, VALUE> _dict) where VALUE : new()
        {
            try
            {
                var _key = KeyBuffer.Decode<KEY>();

                if (_dict.TryGetValue(_key, out var _entry) == false)
                    _entry = new();

                foreach (var _property in Properties)
                    _property.Unpack(_entry);

                return true;
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Unpacking failed. {_ex.Message}");

                return false;
            }
        }
    }
}