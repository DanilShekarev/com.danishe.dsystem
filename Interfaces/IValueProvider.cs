using System;
using DSystem.Attributes;

namespace DSystem.Interfaces
{
    [ProviderInterface]
    public interface IValueProvider<T> where T : struct
    {
        ValueProvider<T> ValueProvider { get; }
        
        [Subscribe]
        public void Subscribe(Action<T> action)
        {
            ValueProvider.ValueChangedEvent += action;
            action?.Invoke(ValueProvider.Value);
        }

        [UnSubscribe]
        public void UnSubscribe(Action<T> action)
        {
            ValueProvider.ValueChangedEvent -= action;
        }
    }
}