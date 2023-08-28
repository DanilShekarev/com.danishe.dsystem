using System;

namespace DSystem
{
    public class ValueProvider<T> where T : struct
    {
        public event Action<T> ValueChangedEvent;

        public T Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value))return;
                _value = value;
                ValueChangedEvent?.Invoke(_value);
            }
        }

        private T _value;

        public static explicit operator T(ValueProvider<T> val) => val._value;
    }
}