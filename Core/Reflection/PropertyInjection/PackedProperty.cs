namespace Cutulu.Core
{
    public struct PackedProperty
    {
        public Number Idx { get; set; }
        public byte[] Buffer { get; set; }

        public PackedProperty() { }

        public PackedProperty(object _ref, int _idx) : this()
        {
            try
            {
                Construct(ref _ref, ParameterManager.Open(_ref.GetType()), _idx);
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Packing failed. {_ex.Message}");
            }
        }

        public PackedProperty(object _ref, string _name) : this()
        {
            try
            {
                var _manager = ParameterManager.Open(_ref.GetType());

                Construct(ref _ref, _manager, _manager.GetIndex(_name));
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Packing failed. {_ex.Message}");
            }
        }

        private void Construct(ref object _ref, ParameterManager _manager, int _idx)
        {
            Buffer = _manager.GetValue(_ref, Idx = _idx).Encode();
        }

        public readonly bool Unpack(object _ref)
        {
            try
            {
                var _manager = ParameterManager.Open(_ref.GetType());

                _manager.SetValue(_ref, Idx, Buffer.Decode(_manager.GetType(Idx)));
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