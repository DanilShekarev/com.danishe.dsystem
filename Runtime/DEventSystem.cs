using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DSystem
{
    public class DEventSystem
    {
        public static DEventSystem Instance { get; internal set; }
        
        private readonly Dictionary<Type, IDAction> _actions = new ();
        
        public DAction<T> GetDAction<T>(bool createInstance = true) where T : class
        {
            return GetDAction(typeof(T), createInstance) as DAction<T>;
        }

        public DAction<T> GetDAction<T>(Action<T> action, bool createInstance = true) where T : class
        {
            var dAction = GetDAction<T>(createInstance);
            dAction.Invoke(action);
            return dAction;
        }

        public IDAction GetDAction(Type type, bool createInstance = true)
        {
            if (_actions.TryGetValue(type, out var action) || !createInstance) return action;
            action = IDAction.Create(type);
            _actions.Add(type, action);
            return action;
        }
        
        public void Subscribe(object instance)
        {
            var interfaces = instance.GetType().GetInterfaces();
            foreach (var inter in interfaces)
            {
                var registryAttr = inter.GetCustomAttribute<ListenerAttribute>();
                if (registryAttr == null)
                    continue;
                if (!registryAttr.Global)
                {
                    Debug.LogWarning($"The system {instance.GetType().Name} has a local event {inter.Name}!");
                    continue;
                }
                GetDAction(inter).RegistryListener(instance);
            }
        }
    }
}