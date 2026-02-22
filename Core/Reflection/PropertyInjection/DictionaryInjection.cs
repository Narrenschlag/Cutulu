namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.IO;

    public partial struct DictionaryInjection<KEY, VALUE> where VALUE : new()
    {
        public KEY[] Keys { get; set; }
        public Number[] Index { get; set; }
        public byte[] Buffer { get; set; }

        public DictionaryInjection() { }

        public DictionaryInjection(Dictionary<KEY, VALUE> _ref, ICollection<KEY> _keys, params string[] _name) : this(_ref, _keys, PackedProperties.GetIdx(_name, typeof(VALUE))) { }

        public DictionaryInjection(Dictionary<KEY, VALUE> _ref, params string[] _name) : this(_ref, _ref.Keys, _name) { }

        public DictionaryInjection(Dictionary<KEY, VALUE> _ref, params int[] _idx) : this(_ref, _ref.Keys, _idx) { }

        public DictionaryInjection(Dictionary<KEY, VALUE> _ref, ICollection<KEY> _keys, params int[] _idx)
        {
            try
            {
                using var _memory = new MemoryStream();
                using var _writer = new BinaryWriter(_memory);

                // Assign indicies
                Index = new Number[_idx.Length];
                for (int _k = 0; _k < _idx.Length; _k++)
                {
                    Index[_k] = _idx[_k];
                }

                var _manager = ParameterManager.Open<VALUE>();
                Keys = [.. _keys];

                foreach (var _key in Keys)
                {
                    var _r = _ref[_key];

                    for (short _k = 0; _k < Index.Length; _k++)
                    {
                        var _i = Index[_k];
                        _writer.Encode(_manager.GetValue(_r, _i), _manager.GetType(_i));
                    }
                }

                Buffer = _memory.ToArray();
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Packing failed. {_ex.Message}\n{_ex.StackTrace}");
            }
        }

        public readonly bool Unpack(Dictionary<KEY, VALUE> _dict, params Number[] _ignore_idx)
        {
            try
            {
                using var _memory = new MemoryStream(Buffer);
                using var _reader = new BinaryReader(_memory);

                var _manager = ParameterManager.Open<VALUE>();
                var _ignore_any = _ignore_idx.NotEmpty();

                foreach (var _key in Keys)
                {
                    if (_dict.TryGetValue(_key, out var _entry) == false)
                        _dict[_key] = _entry = new();

                    for (short _k = 0; _k < Index.Length; _k++)
                    {
                        var _idx = Index[_k];

                        if (_reader.TryDecode(_manager.GetType(_idx), out var _value) && (_ignore_any == false || _ignore_idx.Contains(_idx) == false))
                            _manager.SetValue(_entry, _idx, _value);
                    }
                }

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