using System.Collections.Generic;

namespace Cutulu
{
    public class Ref<T>
    {
        public System.Action<T> Changed;

        protected T _value;
        public virtual T Value
        {
            get => _value;

            set
            {
                if (_value.Equals(value) == false)
                {
                    _value = value;

                    Changed?.Invoke(value);
                }
            }
        }

        public Ref(T value) => Value = value;
        public Ref() => Value = default;

        // Implicit conversion from Ref<T> to T
        public static implicit operator T(Ref<T> reference)
        {
            return reference.Value;
        }

        // Implicit conversion from T to Ref<T>
        public static implicit operator Ref<T>(T value)
        {
            return new(value);
        }
    }

    public class SharedRef<T> : Ref<T>
    {
        private List<Ref<T>> _bindings { get; set; } = new();

        public override T Value
        {
            get { return _value; }
            set
            {
                if (_value.Equals(value) == false)
                {
                    _value = value;

                    // Update all bound values
                    foreach (var bind in _bindings)
                    {
                        bind.Value = _value;
                    }

                    Changed?.Invoke(value);
                }
            }
        }

        public void Add(Ref<T> target)
        {
            _bindings.Add(target);

            // Initialize the target value with the current value
            target.Value = _value;
        }

        public void Remove(Ref<T> target)
        {
            _bindings.Remove(target);
        }
    }
}