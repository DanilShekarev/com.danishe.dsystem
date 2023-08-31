using System;
using DSystem.Interfaces;
using UnityEngine;

namespace DSystem
{
    public class SubscribeListener<T> : IDisposable where T : struct
    {
        private MonoBehaviour _target;
        private Action<T> _onEvent;
        private IValueProvider<T> _provider;
        private bool _disableListen;

        internal SubscribeListener(object target, Action<T> onEvent, IValueProvider<T> provider, bool disableListen = false)
        {
            _target = (MonoBehaviour)target;
            _onEvent = onEvent;
            _provider = provider;
            _disableListen = disableListen;
        }
        
        internal void OnValChanged(T val)
        {
            if (_target == null)
            {
                Dispose();
                return;
            }
            if (!_disableListen && !_target.enabled) return;
            
            _onEvent.Invoke(val);
        }

        public void Dispose()
        {
            _provider.UnSubscribe(OnValChanged);
            _target = null;
            _onEvent = null;
            _provider = null;
        }
    }
}