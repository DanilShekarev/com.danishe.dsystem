using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DSystem
{
    public abstract class DBehaviour : MonoBehaviour
    {
        public bool OnDisableInitialized { get; private set; }
        
        private bool _initialized;
        
        private Dictionary<Type, List<object>> _listeners;
        private Dictionary<Type, Action<object>> _listenerCatchers;
        
        private Action _onDestroy;

        internal void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            MainInjector.Instance.RegistryInjection(this);
            var type = GetType();
            var singletonAttr = type.GetCustomAttribute<SingletonAttribute>(false);
            if (singletonAttr != null)
            {
                MainInjector.Instance.RegistrySingleton(this);
            }

            var interfaces = type.GetInterfaces();
            foreach (var inter in interfaces)
            {
                var registryAttr = inter.GetCustomAttribute<ListenerAttribute>();
                if (registryAttr == null) continue;
                if (!registryAttr.Global)
                {
                    var dBehaviours = registryAttr.Up ? GetComponentsInChildren<DBehaviour>() : GetComponentsInParent<DBehaviour>();
                    foreach (var dBehaviour in dBehaviours)
                    {
                        if (dBehaviour._listeners == null) continue;
                        if (!dBehaviour._listeners.TryGetValue(inter, out List<object> listener)) continue;
                        if (!listener.Contains(this))
                        {
                            listener.Add(this);
                            dBehaviour.InvokeCatcher(inter, this);
                        }
                        _onDestroy += () =>
                        {
                            listener?.Remove(this);
                        };
                    }
                    continue;
                }
                MainInjector.Instance.RegistryListener(this, inter);
                _onDestroy += () =>
                {
                    MainInjector.Instance.RemoveListener(this, inter);
                };
            }

            var registryAttributes = type.GetCustomAttributes<RegistryListenersAttribute>();
            foreach (var registryAttribute in registryAttributes)
            {
                foreach (var t in registryAttribute.Types)
                {
                    _listeners ??= new Dictionary<Type, List<object>>();
                    var listeners = t.GetCustomAttribute<ListenerAttribute>().Up ? GetComponentsInParent(t) : GetComponentsInChildren(t);
                    
                    _listeners.Add(t, new List<object>(listeners));
                    
                    if (_listenerCatchers == null ||
                        !_listenerCatchers.TryGetValue(t, out Action<object> onCatch)) continue;
                    foreach (var listener in listeners)
                    {
                        onCatch?.Invoke(listener);
                    }
                }
            }

            var dia = type.GetCustomAttribute<DisableInitializeAttribute>();
            if (dia != null)
            {
                OnDisableInitialized = true;
                gameObject.SetActive(true);
                gameObject.SetActive(false);
                OnDisableInitialized = false;
            }
        }

        protected virtual void Awake()
        {
            if (!_initialized)
                Initialize();
        }

        protected virtual void OnDestroy()
        {
            _onDestroy?.Invoke();
        }

        protected Action SubscribeTo<T>(DBehaviour dBehaviour)
        {
            if (dBehaviour._listeners == null) return null;
            var inter = typeof(T);
            if (!dBehaviour._listeners.TryGetValue(inter, out List<object> listener)) return null;
            if (!listener.Contains(this))
            {
                listener.Add(this);
                dBehaviour.InvokeCatcher(inter, this);
            }

            Action remove = () =>
            {
                listener?.Remove(this);
            };
            _onDestroy += remove;
            return remove;
        }

        private void InvokeCatcher(Type type, object listener)
        {
            if (_listenerCatchers.TryGetValue(type, out Action<object> onCatch))
            {
                onCatch.Invoke(listener);
            }
        }

        protected void InvokeListener<T>(Action<T> action) where T : class
        {
            if (!_listeners.TryGetValue(typeof(T), out List<object> listeners)) return;
            foreach (var listener in listeners)
            {
                action.Invoke(listener as T);
            }
        }

        protected void RegistryCatcher<T>(Action<T> onCatchListener) where T : class
        {
            _listenerCatchers ??= new Dictionary<Type, Action<object>>();
            
            Action<object> subscribe = (listener) =>
            {
                onCatchListener?.Invoke(listener as T);
            };
            var type = typeof(T);
            if (_listenerCatchers.TryGetValue(type, out Action<object> onCatch))
            {
                onCatch += subscribe;
                _listenerCatchers.Remove(type);
                _listenerCatchers.Add(type, onCatch);
                return;
            }
            _listenerCatchers.Add(type, subscribe);
        }

        protected void RemoveCatcher<T>()
        {
            _listenerCatchers.Remove(typeof(T));
        }
    }
}