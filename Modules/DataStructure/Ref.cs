using System.Collections.Generic;

namespace Cutulu
{
    /// <summary>
    /// Keeps track of a value of any type. Use 'Changed' to hook on to any changes.
    /// </summary>
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

    /// <summary>
    /// Keeps track of a value of any type. Use 'Changed' to hook on to any changes. Allows to bind Ref<T> and update them all at the same time before calling 'Changed'.
    /// </summary>
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
                    // Update all bound values
                    for (var i = _bindings.Count - 1; i >= 0; i--)
                    {
                        if (_bindings[i] == null)
                            _bindings.RemoveAt(i);

                        else
                            _bindings[i].Value = value;
                    }

                    _value = value;

                    Changed?.Invoke(value);
                }
            }
        }

        public void Bind(Ref<T> target)
        {
            _bindings.Add(target);

            // Initialize the target value with the current value
            target.Value = _value;
        }

        public void Unbind(Ref<T> target)
        {
            _bindings.Remove(target);
        }
    }
}