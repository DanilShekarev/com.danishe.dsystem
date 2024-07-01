using System;
using System.Collections.Generic;
using System.Reflection;
using DSystem.InternalSystems;
using UnityEngine;

namespace DSystem
{
    public abstract class DBehaviour : MonoBehaviour
    {
        private bool _initialized;
        
        private Dictionary<Type, List<object>> _listeners;
        private Dictionary<Type, Action<object>> _listenerCatchers;
        private Dictionary<Type, Action<object>> _listenerRemoveCatchers;
        
        private Action _onDestroy;
        private Action _onDisable;
        private Action _onEnable;

        private DisableCatcher _disableCatcher;

        internal void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            MainInjector.Instance.RegistryInjection(this);
            var type = GetType();
            var singletonAttr = type.GetCustomAttribute<SingletonAttribute>(true);
            if (singletonAttr != null)
            {
                MainInjector.Instance.RegistrySingleton(this);
                _onDestroy += () =>
                {
                    MainInjector.Instance.RemoveSingleton(this);
                };
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
                        if (dBehaviour == this) continue;
                        dBehaviour._listeners ??= new();
                        if (!dBehaviour._listeners.TryGetValue(inter, out List<object> listener))
                        {
                            listener = new List<object>();
                            dBehaviour._listeners.Add(inter, listener);
                        }
                        if (!listener.Contains(this))
                        {
                            listener.Add(this);
                            dBehaviour.InvokeCatcher(inter, this);
                        }
                        _onDestroy += () =>
                        {
                            RemoveListener(listener, this, inter, dBehaviour);
                        };
                    }
                    continue;
                }
                MainInjector.Instance.RegistryListener(this, inter);
                _onDestroy += () =>
                {
                    MainInjector.Instance.RemoveListener(this, inter);
                };
                _onDisable += () =>
                {
                    MainInjector.Instance.RemoveListener(this, inter);
                };
                _onEnable += () =>
                {
                    MainInjector.Instance.RegistryListener(this, inter);
                };
            }

            var registryAttributes = type.GetCustomAttributes<RegistryListenersAttribute>();
            foreach (var registryAttribute in registryAttributes)
            {
                foreach (var t in registryAttribute.Types)
                {
                    _listeners ??= new Dictionary<Type, List<object>>();
                    var listeners = t.GetCustomAttribute<ListenerAttribute>().Up ? GetComponentsInParent(t) : GetComponentsInChildren(t);

                    if (_listeners.TryGetValue(t, out List<object> list))
                    {
                        list.AddRange(listeners);
                    }
                    else
                    {
                        _listeners.Add(t, new List<object>(listeners));
                    }

                    if (_listenerCatchers == null ||
                        !_listenerCatchers.TryGetValue(t, out Action<object> onCatch)) continue;
                    foreach (var listener in listeners)
                    {
                        onCatch?.Invoke(listener);
                    }
                }
            }

            if (!gameObject.activeInHierarchy)
            {
                if (!MainInjector.Instance.TryGetSystem(out DisableCatchersController disableCatchersController)) return;
                _disableCatcher = disableCatchersController.RegistryForceOnDestroy(this);
            }

            OnInitialize();
        }

        protected virtual void Awake()
        {
            if (!_initialized)
                Initialize();
            if (_disableCatcher != null)
            {
                _disableCatcher.RemoveOnDispose(this);
                _disableCatcher = null;
            }
        }

        protected virtual void OnEnable()
        {
            _onEnable?.Invoke();
        }

        protected virtual void OnDisable()
        {
            _onDisable?.Invoke();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnDispose() { }

        internal void InternalOnDispose()
        {
            OnDestroy();
        }

        protected virtual void OnDestroy()
        {
            _onDestroy?.Invoke();
            OnDispose();
        }

        public Action SubscribeTo<T>(DBehaviour dBehaviour)
        {
            dBehaviour._listeners ??= new Dictionary<Type, List<object>>();
            var inter = typeof(T);
            if (!dBehaviour._listeners.TryGetValue(inter, out List<object> listener))
            {
                listener = new List<object>();
                dBehaviour._listeners.Add(inter, listener);
            }
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
            if (_listenerCatchers == null) return;
            if (_listenerCatchers.TryGetValue(type, out Action<object> onCatch))
            {
                onCatch.Invoke(listener);
            }
        }

        private void RemoveListener(List<object> target, object obj, Type type, DBehaviour parent)
        {
            if (parent._listenerRemoveCatchers != null && parent._listenerRemoveCatchers.TryGetValue(type, out Action<object> onCatch))
            {
                onCatch?.Invoke(obj);
            }
            target?.Remove(obj);
        }

        protected void InvokeListeners<T>(Action<T> action) where T : class
        {
            if (!_listeners.TryGetValue(typeof(T), out List<object> listeners)) return;
            foreach (var listener in listeners)
            {
                action.Invoke(listener as T);
            }
        }

        protected void RegistryCatcher<T>(Action<T> onCatchListener) where T : class
        {
            RegistryCatcher<T>(onCatchListener, ref _listenerCatchers);
        }

        protected void RemoveCatcher<T>()
        {
            RemoveCatcher<T>(ref _listenerCatchers);
        }
        
        protected void RegistryUnsubscribeCatcher<T>(Action<T> onCatchListener) where T : class
        {
            RegistryCatcher<T>(onCatchListener, ref _listenerRemoveCatchers);
        }

        protected void RemoveUnsubscribeCatcher<T>()
        {
            RemoveCatcher<T>(ref _listenerCatchers);
        }

        private void RegistryCatcher<T>(Action<T> onCatchListener, ref Dictionary<Type, Action<object>> target)
            where T : class
        {
            target ??= new Dictionary<Type, Action<object>>();
            
            Action<object> subscribe = (listener) =>
            {
                onCatchListener?.Invoke(listener as T);
            };
            var type = typeof(T);
            if (target.TryGetValue(type, out Action<object> onCatch))
            {
                onCatch += subscribe;
                target.Remove(type);
                target.Add(type, onCatch);
                return;
            }
            target.Add(type, subscribe);
        }

        private void RemoveCatcher<T>(ref Dictionary<Type, Action<object>> target)
        {
            target.Remove(typeof(T));
        }
    }
}