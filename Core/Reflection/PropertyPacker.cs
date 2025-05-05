namespace Cutulu.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Allows for only packing and later applying certain values of a class to reduce data load on small changes
    /// aswell as reduce overhead for constructing new classes/structs to overwrite existant
    /// <para>Only works with classes, not structs</para>
    /// </summary>
    public static class PropertyPacker
    {
        public static byte[] Pack(object _obj, params string[] _properties) => Pack(null, _obj, _properties);

        public static byte[] Pack(object _identifier, object _obj, params string[] _properties)
        {
            if (_obj == null || _properties.IsEmpty()) return [];

            var _packed = new Property[_properties.Length];
            var _manager = PropertyManager.Open(_obj.GetType());

            for (int i = 0; i < _properties.Length; i++)
            {
                var _idx = _manager.GetIndex(_properties[i]);

                _packed[i] = new()
                {
                    Idx = _idx,
                    Buffer = _manager.GetValue(_idx, _obj).Encode(),
                };
            }

            return new Container()
            {
                Identifier = _identifier.Encode(),
                Properties = _packed,
            }.Encode();
        }

        public static bool TryUnpackPackedProperties(this byte[] _buffer, out Container _packed)
        {
            return _buffer.TryDecode(out _packed);
        }

        public static bool TryApplyPackedProperties<KEY, VALUE>(this Dictionary<KEY, VALUE> _dic, byte[] _buffer) where VALUE : new()
        {
            if (TryUnpackPackedProperties(_buffer, out var _packed) && _packed.Identifier.TryDecode(out KEY _key))
            {
                if (_dic.TryGetValue(_key, out var _obj) == false)
                    _dic[_key] = _obj = new();

                var _manager = PropertyManager.Open<VALUE>();

                foreach (var _property in _packed.Properties)
                {
                    if (_property.Buffer.TryDecode(_manager.GetType(_property.Idx), out var _value))
                        _manager.SetValue(_property.Idx, _obj, _value);
                }

                return true;
            }

            return false;
        }

        public static bool TryApplyPackedProperties<KEY, VALUE>(this Dictionary<KEY, VALUE> _dic, Container _packed) where VALUE : new()
        {
            if (_packed.Identifier.TryDecode(out KEY _key))
            {
                if (_dic.TryGetValue(_key, out var _obj) == false)
                    _dic[_key] = _obj = new();

                var _manager = PropertyManager.Open<VALUE>();

                foreach (var _property in _packed.Properties)
                {
                    if (_property.Buffer.TryDecode(_manager.GetType(_property.Idx), out var _value))
                        _manager.SetValue(_property.Idx, _obj, _value);
                }

                return true;
            }

            return false;
        }

        public static bool TryApplyPackedProperties<VALUE>(this VALUE _obj, byte[] _buffer)
        {
            if (TryUnpackPackedProperties(_buffer, out var _packed))
            {
                var _manager = PropertyManager.Open<VALUE>();

                foreach (var _property in _packed.Properties)
                {
                    if (_property.Buffer.TryDecode(_manager.GetType(_property.Idx), out var _value))
                        _manager.SetValue(_property.Idx, _obj, _value);
                }

                return true;
            }

            return false;
        }

        public static void ApplyPackedProperties<VALUE>(this VALUE _obj, Container _packed)
        {
            var _manager = PropertyManager.Open<VALUE>();

            foreach (var _property in _packed.Properties)
            {
                if (_property.Buffer.TryDecode(_manager.GetType(_property.Idx), out var _value))
                    _manager.SetValue(_property.Idx, _obj, _value);
            }
        }

        public struct Container
        {
            public byte[] Identifier { get; set; }
            public Property[] Properties { get; set; }
        }

        public struct Property
        {
            public Number Idx { get; set; }
            public byte[] Buffer { get; set; }

            public Property() { }

            public Property(int _idx, object _obj)
            {
                try
                {
                    Idx = _idx;

                    var _manager = PropertyManager.Open(_obj.GetType());
                    Buffer = _manager.GetValue(_idx, _obj).Encode();
                }
                catch
                {
                    throw new("Packing failed.");
                }
            }

            public Property(string _name, object _obj)
            {
                try
                {
                    var _manager = PropertyManager.Open(_obj.GetType());
                    Idx = _manager.GetIndex(_name);

                    Buffer = _manager.GetValue(Idx, _obj).Encode();
                }
                catch
                {
                    throw new("Packing failed.");
                }
            }

            public readonly bool Unpack(object _obj)
            {
                try
                {
                    var _manager = PropertyManager.Open(_obj.GetType());
                    var _type = _manager.GetType(Idx);

                    _manager.SetValue(Idx, _obj, Buffer.Decode(_type));
                    return true;
                }
                catch (Exception _ex)
                {
                    Debug.LogError($"Unpacking failed. {_ex.Message}");
                    return false;
                }
            }
        }
    }
}