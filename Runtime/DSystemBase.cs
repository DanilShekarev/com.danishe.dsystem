using System;

namespace DSystem
{
    public abstract class DSystemBase
    {
        public virtual bool AutoRegistrationEvents => true;
        
        protected void RegistryInjection(object instance, bool forceInitializeSystems = false, bool isComponent = false)
        {
            Injector.Instance.RegistryInjection(instance, forceInitializeSystems, isComponent);
        }
        
        protected DAction<T> GetDAction<T>(bool createInstance = true) where T : class
        {
            return DEventSystem.Instance.GetDAction<T>(createInstance);
        }
        
        protected DAction<T> GetDAction<T>(Action<T> action, bool createInstance = true) where T : class
        {
            return DEventSystem.Instance.GetDAction<T>(action, createInstance);
        }
        
        protected void RegisterInstance(object instance)
        {
            Injector.Instance.RegisterInstance(instance);
        }
        
        protected void RemoveInstance(Type type)
        {
            Injector.Instance.RemoveInstance(type);
        }
        
        protected void RemoveInstance<T>()
        {
            Injector.Instance.RemoveInstance<T>();
        }
        
        protected bool TryGetInstance(Type type, out object system)
        {
            return Injector.Instance.TryGetInstance(type, out system);
        }

        protected bool TryGetInstance<T>(out T system)
        {
            return Injector.Instance.TryGetInstance(out system);
        }
        
        [Obsolete]
        protected void RegistrySingleton(object instance)
        {
            Injector.Instance.RegisterInstance(instance);
        }

        [Obsolete]
        protected void RemoveSingleton(object instance)
        {
            Injector.Instance.RemoveInstance(instance.GetType());
        }

        [Obsolete]
        protected void RemoveSingleton<T>()
        {
            Injector.Instance.RemoveInstance<T>();
        }

        [Obsolete]
        protected bool TryGetSystem(System.Type type, out object system)
        {
            return Injector.Instance.TryGetInstance(type, out system);
        }
        
        [Obsolete]
        protected bool TryGetSystem<T>(out T system)
        {
            return Injector.Instance.TryGetInstance(out system);
        }
    }
}