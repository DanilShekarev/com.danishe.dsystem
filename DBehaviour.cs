using System;
using System.Linq;
using System.Reflection;
using DSystem.Attributes;
using UnityEngine;

namespace DSystem
{
    public abstract class DBehaviour : MonoBehaviour
    {
        public static void DInstantiate(DBehaviour original)
        {
            DBehaviour obj = Instantiate(original);
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.DisableInitialize();
            }
        }

        public static void DInstantiate(DBehaviour original, Transform parent)
        {
            DBehaviour obj = Instantiate(original, parent);
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.DisableInitialize();
            }
        }
        
        public static void DInstantiate(DBehaviour original, Vector3 position, Quaternion rotation)
        {
            DBehaviour obj = Instantiate(original, position, rotation);
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.DisableInitialize();
            }
        }
        
        public static void DInstantiate(DBehaviour original, Vector3 position, Quaternion rotation, Transform parent)
        {
            DBehaviour obj = Instantiate(original, position, rotation, parent);
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.DisableInitialize();
            }
        }
        
        private bool _initialized;
        private bool _disabledInitialized;

        private Action _unSubscribe;
        private Action _subscribe;
        
        protected virtual void Awake()
        {
            if (_initialized) return;

            _initialized = true;
            
            MainInjector.Instance.RegistryInjection(this);

            Initialize();

            SubscribeListeners();

            if (_disabledInitialized)
            {
                _subscribe?.Invoke();
            }
        }

        private void OnEnable()
        {
            if (_disabledInitialized) return;
            _subscribe?.Invoke();
        }

        private void OnDisable()
        {
            if (_disabledInitialized) return;
            _unSubscribe?.Invoke();
        }

        private void OnDestroy()
        {
            if (!_disabledInitialized) return;
            _unSubscribe?.Invoke();
        }

        private void SubscribeListeners()
        {
            Type type = GetType();
            var interfaces = type.GetInterfaces().Where(t => t.GetCustomAttribute<ListenerInterfaceAttribute>() != null);
            foreach (var iInterface in interfaces)
            {
                MethodInfo listenerMethod = iInterface.GetMethods()[0];
                Type[] genericsTypes = iInterface.GetGenericArguments();
                
                if (!MainInjector.Instance.TryGetSystem(genericsTypes[0], out object system)) continue;
                
                Type providerInterface = genericsTypes[0]
                    .GetInterfaces().Where(t => t.GetCustomAttribute<ProviderInterfaceAttribute>() != null)
                    .FirstOrDefault(t => t.GetGenericArguments()[0] == genericsTypes[1]);
                
                if (providerInterface == default) continue;

                MethodInfo[] methods = providerInterface.GetMethods();
                MethodInfo subscribeMethod = methods.FirstOrDefault(m => m.GetCustomAttribute<SubscribeAttribute>() != null);
                MethodInfo unSubscribeMethod = methods.FirstOrDefault(m => m.GetCustomAttribute<UnSubscribeAttribute>() != null);
                
                Type actionType = typeof(Action<>);
                actionType = actionType.MakeGenericType(genericsTypes[1]);
                
                var methodDelegate = Delegate.CreateDelegate(actionType, this, listenerMethod);
                
                _subscribe += () =>
                {
                    subscribeMethod?.Invoke(system, new object[] { methodDelegate });
                };
                _unSubscribe += () =>
                {
                    unSubscribeMethod?.Invoke(system, new object[]{methodDelegate});
                };
            }
        }
        
        internal void DisableInitialize()
        {
            _disabledInitialized = true;
            Awake();
        }
        
        protected virtual void Initialize() {}
    }
}