namespace DSystem
{
    public abstract class DSystemBase
    {
        protected void RegistryListener<T>(T listener) where T : class
        {
            MainInjector.Instance.RegistryListener<T>(listener);
        }

        protected void RegistryListener(object listener, System.Type listenerType)
        {
            MainInjector.Instance.RegistryListener(listener, listenerType);
        }

        protected void RemoveListener<T>(T listener) where T : class
        {
            MainInjector.Instance.RemoveListener<T>(listener);
        }

        protected void RemoveListener(object listener, System.Type listenerType)
        {
            MainInjector.Instance.RemoveListener(listener, listenerType);
        }

        protected UnsubscribeToken RegistryListenerCatcher<T>(System.Action<T> onCatchListener) where T : class
        {
            return MainInjector.Instance.RegistryListenerCatcher(onCatchListener);
        }

        protected UnsubscribeToken RegistryUnsubscribeListenerCatcher<T>(System.Action<T> onCatchListener) where T : class
        {
            return MainInjector.Instance.RegistryUnsubscribeListenerCatcher(onCatchListener);
        }

        protected void RegistryInjection(object instance, bool forceInitializeSystems = false, bool isSystem = false)
        {
            MainInjector.Instance.RegistryInjection(instance, forceInitializeSystems, isSystem);
        }

        protected void RegistryCatcher<T>(ListenerCatcher<T> listenerCatcher) where T : class
        {
            MainInjector.Instance.RegistryCatcher(listenerCatcher);
        }

        protected void RemoveCatcher<T>(ListenerCatcher<T> listenerCatcher) where T : class
        {
            MainInjector.Instance.RemoveCatcher(listenerCatcher);
        }

        protected void InvokeListeners<T>(System.Action<T> action) where T : class
        {
            MainInjector.Instance.InvokeListeners(action);
        }

        protected void InvokeListeners(System.Type interfaceType, System.Action<object> action)
        {
            MainInjector.Instance.InvokeListeners(interfaceType, action);
        }

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