using System;
using DSystem.Interfaces;

namespace DSystem
{
    public static class Observable
    {
        public static SubscribeListener<TVal> Subscribe<T1, TVal>(Action<TVal> onEvent, bool disableListen = false) where TVal : struct
        {
            if (!MainInjector.Instance.TryGetSystem<T1>(out T1 system)) return null;
            if (system is not IValueProvider<TVal> provider) return null;
            SubscribeListener<TVal> wrapper = new SubscribeListener<TVal>(onEvent.Target, onEvent, provider, disableListen);
            provider.Subscribe(wrapper.OnValChanged);
            return wrapper;
        }
    }
}