using System;
using System.Collections.Generic;
using System.Reflection;
using DSystem.InternalSystems;
using DSystem.Utils;
using UnityEngine;

namespace DSystem
{
    public abstract class DBehaviour : MonoBehaviour
    {
        private bool _initialized;

        private Dictionary<Type, IDAction> _actions;
        
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
                        if (dBehaviour == this) 
                            continue;
                        
                        var dAction = dBehaviour.GetDAction(inter, false);
                        if (dAction == null) 
                            continue;
                        
                        dAction.RegistryListener(this);
                        _onDestroy += () =>
                        {
                            dAction.RemoveListener(this);
                        };
                    }
                    continue;
                }
                var daction = MainInjector.Instance.GetDAction(inter);
                daction.RegistryListener(this);
                _onDestroy += () =>
                {
                    daction.RemoveListener(this);
                };
                _onDisable += () =>
                {
                    daction.RemoveListener(this);
                };
                _onEnable += () =>
                {
                    daction.RegistryListener(this);
                };
            }
            
            var registryAttributes = type.GetAttributes<RegistryListenersAttribute>();
            foreach (var registryAttribute in registryAttributes)
            {
                foreach (var t in registryAttribute.Types)
                {
                    var listeners = t.GetCustomAttribute<ListenerAttribute>().Up ? GetComponentsInParent(t) : GetComponentsInChildren(t);

                    var dAction = GetDAction(t);
                    foreach (var listener in listeners)
                    {
                        dAction.RegistryListener(listener);
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

        public DAction<T> GetDAction<T>(bool createInstance = true) where T : class
        {
            return GetDAction(typeof(T), createInstance) as DAction<T>;
        }
        
        public IDAction GetDAction(Type type, bool createInstance = true)
        {
            _actions ??= new();
            if (!_actions.TryGetValue(type, out var action) && createInstance)
            {
                action = IDAction.Create(type);
                _actions.Add(type, action);
            }
            return action;
        }

        [Obsolete]
        public Action SubscribeTo<T>(DBehaviour dBehaviour) where T : class
        {
            var dAction = dBehaviour.GetDAction<T>();
            dAction.RegistryListener(this as T);

            Action remove = () =>
            {
                dAction.RemoveListener(this as T);
            };
            _onDestroy += remove;
            return remove;
        }

        [Obsolete]
        protected void InvokeListeners<T>(Action<T> action) where T : class
        {
            var dAction = GetDAction<T>(false);
            if (dAction == null) return;
            
            foreach (var listener in dAction)
            {
                action.Invoke(listener);
            }
        }

        [Obsolete]
        protected UnsubscribeToken RegistryCatcher<T>(Action<T> onCatchListener) where T : class
        {
            return GetDAction<T>().RegistryCatcher(onCatchListener);
        }
        
        [Obsolete]
        protected UnsubscribeToken RegistryUnsubscribeCatcher<T>(Action<T> onCatchListener) where T : class
        {
            return GetDAction<T>().RegistryUnsubscribeCatcher(onCatchListener);
        }
    }
}