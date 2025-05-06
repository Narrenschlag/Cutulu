namespace Cutulu.Core
{
    public struct PackedProperty
    {
        public Number Idx { get; set; }
        public byte[] Buffer { get; set; }

        public PackedProperty() { }

        public PackedProperty(object _ref, int _idx)
        {
            try
            {
                Idx = _idx;

                var _manager = PropertyManager.Open(_ref.GetType());
                Buffer = _manager.GetValue(_ref, _idx).Encode();
            }
            catch (System.Exception _ex)
            {
                Debug.LogError($"Packing failed. {_ex.Message}");
            }
        }

        public PackedProperty(object _ref, string _name)
        {
            try
            {
                var _manager = PropertyManager.Open(_ref.GetType());
                Idx = _manager.GetIndex(_name);

                Buffer = _manager.GetValue(_ref, Idx).Encode();
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
                var _manager = PropertyManager.Open(_ref.GetType());

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