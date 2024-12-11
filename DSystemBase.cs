namespace DSystem
{
    public abstract class DSystemBase
    {
        [System.Obsolete]
        protected void RegistryListener<T>(T listener) where T : class
        {
            MainInjector.Instance.RegistryListener(listener);
        }

        [System.Obsolete]
        protected void RemoveListener<T>(T listener) where T : class
        {
            MainInjector.Instance.RemoveListener(listener);
        }

        [System.Obsolete]
        protected UnsubscribeToken RegistryListenerCatcher<T>(System.Action<T> onCatchListener) where T : class
        {
            return MainInjector.Instance.RegistryListenerCatcher(onCatchListener);
        }

        [System.Obsolete]
        protected UnsubscribeToken RegistryUnsubscribeListenerCatcher<T>(System.Action<T> onCatchListener) where T : class
        {
            return MainInjector.Instance.RegistryUnsubscribeListenerCatcher(onCatchListener);
        }

        protected void RegistryInjection(object instance, bool forceInitializeSystems = false, bool isComponent = false)
        {
            MainInjector.Instance.RegistryInjection(instance, forceInitializeSystems, isComponent);
        }

        [System.Obsolete]
        protected void InvokeListeners<T>(System.Action<T> action) where T : class
        {
            MainInjector.Instance.InvokeListeners(action);
        }

        [System.Obsolete]
        protected void InvokeListeners(System.Type interfaceType, System.Action<object> action)
        {
            MainInjector.Instance.InvokeListenersReflection(interfaceType, action);
        }

        protected DAction<T> GetDAction<T>() where T : class
        {
            return MainInjector.Instance.GetDAction<T>();
        }

        [System.Obsolete]
        protected System.Collections.Generic.IEnumerable<T> ForeachListeners<T>() where T : class
        {
            return MainInjector.Instance.ForeachListeners<T>();
        }

        protected void RegistrySingleton(object instance)
        {
            MainInjector.Instance.RegistrySingleton(instance);
        }

        protected void RemoveSingleton(object instance)
        {
            MainInjector.Instance.RemoveSingleton(instance);
        }

        protected void RemoveSingleton<T>()
        {
            MainInjector.Instance.RemoveSingleton<T>();
        }

        protected bool TryGetSystem(System.Type type, out object system)
        {
            return MainInjector.Instance.TryGetSystem(type, out system);
        }

        protected bool TryGetSystem<T>(out T system)
        {
            return MainInjector.Instance.TryGetSystem(out system);
        }
    }
}