using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DSystem
{
    [DefaultExecutionOrder(-100)]
    public sealed class MainInjector : MonoBehaviour
    {
        public static MainInjector Instance { get; private set; }

        public event Action<Scene, LoadSceneMode> SceneInjected;
    
        private Dictionary<Type, object> _instances;
        private Dictionary<Type, Action<object>> _injectWaiters;
        private Dictionary<Type, List<object>> _catchers;
        private Dictionary<Type, List<object>> _listeners;
        private Dictionary<Type, Action<object>> _listenersCatchers;
        private Dictionary<Type, Action<object>> _listenersRemoveCatchers;
        private Dictionary<Type, EventFlag> _eventFlags;
        private List<IUpdatable> _updatables;
        
        private class EventFlag
        {
            public Queue<object> NewListeners;
            public Queue<object> RemovedListeners;
            public int Active;
        }

        private MethodInfo _getComponentsInChildrens;

        private Assembly _mainAssembly;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            
            Instance = this;
            
            _getComponentsInChildrens = typeof(Component).GetMethod(nameof(GetComponentsInChildren), new Type[] {typeof(bool)});

            _instances = new Dictionary<Type, object>();
            _injectWaiters = new Dictionary<Type, Action<object>>();
            _listeners = new Dictionary<Type, List<object>>();
            _listenersCatchers = new Dictionary<Type, Action<object>>();
            _listenersRemoveCatchers = new Dictionary<Type, Action<object>>();
            _updatables = new List<IUpdatable>();
            _eventFlags = new ();
            _catchers = new();
            Configure();

            SceneManager.sceneLoaded += LoadedScene;
        }

        private void LoadedScene(Scene arg0, LoadSceneMode arg1)
        {
            InjectScene();
            SceneInjected?.Invoke(arg0, arg1);
        }

        private void Update()
        {
            foreach (var updatable in _updatables)
            {
                updatable.Update();
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= LoadedScene;

            if (_instances != null)
                foreach (var pair in _instances)
                {
                    if (pair.Value is IDisposable disposable)
                        disposable.Dispose();
                }
        }

        private void Configure()
        {
            Assembly assembly = Assembly.Load("Assembly-CSharp");
            _mainAssembly = assembly;
            Assembly assemblyDSystem = Assembly.Load("DSystem");
            Configure(assemblyDSystem);
            Configure(assembly);
        }

        private void Configure(Assembly assembly)
        {
            (Type type, AutoRegistryAttribute reg)[] types = assembly.GetTypes()
                .Select(t => (t, t.GetCustomAttribute<AutoRegistryAttribute>()))
                .Where(p => p.Item2 != null).OrderBy(p => p.Item2.Order).ToArray();
            foreach (var pair in types)
            {
                if (!_instances.ContainsKey(pair.type))
                {
                    RegistrySingleton(pair.type, pair.reg);
                }
            }

            int scriptableCount = _instances.Count(i => i.Key.IsSubclassOf(typeof(ScriptableObject)));
            Debug.Log($"{assembly.GetName().Name} DSystem register {_instances.Count - scriptableCount} systems and {scriptableCount} scriptable objects.");
        }

        private object RegisterScriptable(Type type, string scrName)
        {
            object instance = Resources.Load(scrName, type);
            if (instance == null)
            {
                Debug.LogWarning($"No find {scrName} of type {type.Name} scriptable instance!");
                return null;
            }
            
            _instances.Add(type, instance);
            return instance;
        }

        private void InjectScene()
        {
            int counterInject = 0;
            int counterInited = 0;
            var dBehaviours = FindObjectsOfType<DBehaviour>(true);
            foreach (var dBehaviour in dBehaviours)
            {
                counterInject++;

                if (dBehaviour.GetType().GetCustomAttribute<DisableInitializeAttribute>() != null)
                {
                    if (!dBehaviour.enabled) continue;
                    counterInited++;
                    dBehaviour.Initialize();
                }
            }
            Debug.Log($"DSystem inject to {counterInject} and inited {counterInited} objects.");
        }

        private object RegistrySingleton(Type type, AutoRegistryAttribute reg = null)
        {
            object instance;
            reg ??= type.GetCustomAttribute<AutoRegistryAttribute>();

            if (reg == null) return null;

            if (!string.IsNullOrEmpty(reg.NameScriptable))
            {
                return RegisterScriptable(type, reg.NameScriptable);
            }
            
            instance = Activator.CreateInstance(type);

            _instances.Add(instance.GetType(), instance);
            
            RegistryInjection(instance, true, true);

            if (instance is IInitializable startable)
            {
                startable.Initialize();
            }

            if (instance is IUpdatable updatable)
            {
                _updatables.Add(updatable);
            }

            return instance;
        }

        private void Inject(Type type, object instance, bool systemInjection = false, bool isSystem = false)
        {
            if (type == typeof(System.Object) || type == typeof(MonoBehaviour)) return;
            
            if (type.BaseType != null)
                Inject(type.BaseType, instance);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.SetField);

            foreach (var field in fields)
            {
                var injectAttr = field.GetCustomAttribute<InjectAttribute>(false);
                if (injectAttr == null) continue;

                var fType = field.FieldType;
                if (fType.IsArray) fType = fType.GetElementType();
                if (fType == null) continue;
                
                void OnInjected(object inst)
                {
                    if (instance == null || (instance is UnityEngine.Object obj && obj == null))
                    {
                        if (_injectWaiters.TryGetValue(field.FieldType, out Action<object> eventInst))
                        {
                            eventInst -= OnInjected;
                            _injectWaiters.Remove(field.FieldType);
                            _injectWaiters.Add(field.FieldType, eventInst);
                        }
                        return;
                    }
                    field.SetValue(instance, inst);
                    if (string.IsNullOrEmpty(injectAttr.EventName)) return;
                    var method = type.GetMethod(injectAttr.EventName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                    try
                    {
                        method?.Invoke(instance, null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        if (_injectWaiters.TryGetValue(field.FieldType, out Action<object> eventInst))
                        {
                            eventInst -= OnInjected;
                            _injectWaiters.Remove(field.FieldType);
                            _injectWaiters.Add(field.FieldType, eventInst);
                        }
                    }
                }

                if (fType.IsSubclassOf(typeof(Component)))
                {
                    if (isSystem | injectAttr.Params.HasFlag(InjectParams.UseGlobal))
                    {
                        if (_instances.TryGetValue(field.FieldType, out object instanceObj))
                        {
                            OnInjected(instanceObj);
                            var singletonAttr = field.FieldType.GetCustomAttribute<DynamicSingletonAttribute>();
                            if (singletonAttr != null)
                            {
                                RegistryWaiter(field, OnInjected);
                            }
                        }
                        else
                        {
                            RegistryWaiter(field, OnInjected);
                        }
                    }
                    else
                    {
                        var m = instance as MonoBehaviour;
                        if (m == null) continue;
                        if (field.FieldType.IsArray)
                        {
                            var getComponents = _getComponentsInChildrens.MakeGenericMethod(fType);
                            field.SetValue(instance, getComponents.Invoke(m, new object[] {injectAttr.Params.HasFlag(InjectParams.IncludeInactive)}));
                        }
                        else
                        {
                            field.SetValue(instance, m.GetComponentInChildren(fType, injectAttr.Params.HasFlag(InjectParams.IncludeInactive)));
                        }
                        
                    }
                } else if (_instances.TryGetValue(field.FieldType, out object obj))
                {
                    field.SetValue(instance, obj);
                    var singletonAttr = field.FieldType.GetCustomAttribute<DynamicSingletonAttribute>();
                    if (singletonAttr != null)
                    {
                        RegistryWaiter(field, OnInjected);
                    }
                } else
                {
                    var singletonAttr = field.FieldType.GetCustomAttribute<DynamicSingletonAttribute>();
                    if (singletonAttr == null)
                    {
                        if (systemInjection | injectAttr.Params.HasFlag(InjectParams.UseGlobal))
                        {
                            object instanceSystem = RegistrySingleton(field.FieldType);
                            field.SetValue(instance, instanceSystem);
                        }
                        continue;
                    }
                    RegistryWaiter(field, OnInjected);
                }
            }
        }

        private void RegistryWaiter(FieldInfo field, Action<object> action)
        {
            if (_injectWaiters.TryGetValue(field.FieldType, out Action<object> eventInst))
            {
                eventInst += action;
                _injectWaiters.Remove(field.FieldType);
                _injectWaiters.Add(field.FieldType, eventInst);
            }
            else
            {
                _injectWaiters.Add(field.FieldType, action);
            }
        }

        public void RegistryListener<T>(T listener) where T : class
        {
            RegistryListener(listener, typeof(T));
        }

        public Type GetTypeFromName(string typeName)
        {
            return _mainAssembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
        }

        public void RegistryListener(object listener, Type listenerType)
        {
            List<object> listeners;
            if (_eventFlags.TryGetValue(listenerType, out EventFlag flag));
            if (_listeners.TryGetValue(listenerType, out List<object> list))
            {
                listeners = list;
            }
            else
            {
                listeners = new List<object>();
                _eventFlags.Add(listenerType, new EventFlag() 
                    {NewListeners = new Queue<object>(), RemovedListeners = new Queue<object>()});
                _listeners.Add(listenerType, listeners);
            }

            if (listeners.Contains(listener)) return;
            if (flag is { Active: >= 1 })
            {
                flag.NewListeners.Enqueue(listener);
            }
            else
            {
                listeners.Add(listener);
                if (_listenersCatchers.TryGetValue(listenerType, out Action<object> onCatchListener))
                {
                    onCatchListener.Invoke(listener);
                }
            }
        }

        public void RemoveListener<T>(T listener) where T : class
        {
            RemoveListener(listener, typeof(T));
        }

        public void RemoveListener(object listener, Type listenerType)
        {
            if (_eventFlags.TryGetValue(listenerType, out EventFlag flag));
            if (flag is { Active: >= 1 })
            {
                flag.RemovedListeners.Enqueue(listener);
            }
            else
            {
                if (_listenersRemoveCatchers.TryGetValue(listenerType, out Action<object> action))
                {
                    action?.Invoke(listener);
                }
                if (_listeners.TryGetValue(listenerType, out List<object> listeners))
                {
                    listeners.Remove(listener);
                }
            }
        }

        public UnsubscribeToken RegistryListenerCatcher<T>(Action<T> onCatchListener) where T : class
            => RegistryListenerCatcherTarget(onCatchListener, _listenersCatchers);

        public UnsubscribeToken RegistryUnsubscribeListenerCatcher<T>(Action<T> onCatchListener) where T : class
            => RegistryListenerCatcherTarget(onCatchListener, _listenersRemoveCatchers);

        private UnsubscribeToken RegistryListenerCatcherTarget<T>(Action<T> onAction, 
            Dictionary<Type, Action<object>> target) where T : class
        {
            Action<object> subscribe = (listener) =>
            {
                onAction?.Invoke(listener as T);
            };
            var type = typeof(T);
            UnsubscribeToken token = new UnsubscribeToken(() =>
            {
                RemoveListenerCatcher(type, subscribe, target);
            });
            if (target.TryGetValue(type, out Action<object> onCatch))
            {
                onCatch += subscribe;
                target.Remove(type);
                target.Add(type, onCatch);
                return token;
            }
            target.Add(type, subscribe);
            return token;
        }
        
        internal void RemoveListenerCatcher(Type type, Action<object> catcher, Dictionary<Type, Action<object>> target)
        {
            if (target.TryGetValue(type, out Action<object> onCatch)) return;
            onCatch -= catcher;
            target.Remove(type);
            target.Add(type, onCatch);
        }

        public void RegistryInjection(object instance, bool forceInitializeSystems = false, bool isSystem = false)
        {
            Type type = instance.GetType();

            Inject(type, instance, forceInitializeSystems, isSystem);

            if (instance.GetType().GetCustomAttribute<DisableInitializeAttribute>() != null && instance is DBehaviour dBehaviour)
            {
                dBehaviour.Initialize();
            }
        }

        public void RegistryCatcher<T>(ListenerCatcher<T> listenerCatcher) where T : class
        {
            if (!_catchers.TryGetValue(typeof(T), out var catchers))
            {
                catchers = new List<object>();
                _catchers.Add(typeof(T), catchers);
            }
            catchers.Add(listenerCatcher);
        }

        public void RemoveCatcher<T>(ListenerCatcher<T> listenerCatcher) where T : class
        {
            if (!_catchers.TryGetValue(typeof(T), out var catchers)) return;
            catchers.Remove(listenerCatcher);
        }

        public void InvokeListenersEvent(Type interfaceType, MethodInfo method)
        {
            var invokeMethod = GetType().GetMethod("InvokeListeners").MakeGenericMethod(interfaceType);
            Action<object> action = Action;
            invokeMethod.Invoke(this, new object[] {action});

            void Action(object arg)
            {
                method.Invoke(arg, new object[] {});
            }
        }

        public void InvokeListeners<T>(Action<T> action) where T : class
        {
            var t = typeof(T);
            if (!_listeners.TryGetValue(t, out List<object> listeners)) return;
            bool mutable = _catchers.TryGetValue(t, out List<object> catchers);
            if (_eventFlags.TryGetValue(t, out EventFlag flag));
            if (flag != null)
                flag.Active++;
            if (mutable)
            {
                foreach (var listener in listeners)
                {
                    T lastCatcher = listener as T;
                    foreach (var catcher in catchers)
                    {
                        var cat = catcher as ListenerCatcher<T>;
                        cat.Listener = lastCatcher;
                        lastCatcher = catcher as T;
                    }

                    try
                    {
                        action.Invoke(lastCatcher);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            else
            {
                foreach (var listener in listeners)
                {
                    try
                    {
                        action.Invoke(listener as T);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            if (flag != null)
            {
                flag.Active--;
                
                if (flag.Active != 0)
                    return;

                while (flag.RemovedListeners.TryDequeue(out object listener))
                {
                    RemoveListener(listener, t);
                }
                
                while (flag.NewListeners.TryDequeue(out object listener))
                {
                    RegistryListener(listener, t);
                }
            }
        }

        public IEnumerable<T> ForeachListeners<T>() where T : class
        {
            var t = typeof(T);
            if (!_listeners.TryGetValue(t, out List<object> listeners)) yield break;
            bool mutable = _catchers.TryGetValue(t, out List<object> catchers);
            if (mutable)
            {
                foreach (var listener in listeners)
                {
                    T lastCatcher = listener as T;
                    foreach (var catcher in catchers)
                    {
                        var cat = catcher as ListenerCatcher<T>;
                        cat.Listener = lastCatcher;
                        lastCatcher = catcher as T;
                    }
                    yield return lastCatcher;
                }
            }
            else
            {
                foreach (var l in listeners)
                {
                    yield return l as T;
                }
            }
        }

        public void RegistrySingleton(object instance)
        {
            Type type = instance.GetType();
            _instances.Add(type, instance);
            
            if (!_injectWaiters.TryGetValue(type, out Action<object> onInject)) return;
            
            onInject?.Invoke(instance);
            var singletonAttr = type.GetCustomAttribute<DynamicSingletonAttribute>();
            if (singletonAttr == null)
                _injectWaiters.Remove(type);
        }
        
        public void RemoveSingleton(object instance)
        {
            Type type = instance.GetType();
            _instances.Remove(type);
            
            if (!_injectWaiters.TryGetValue(type, out Action<object> onInject)) return;
            
            var singletonAttr = type.GetCustomAttribute<DynamicSingletonAttribute>();
            if (singletonAttr == null)
            {
                onInject?.Invoke(null);
            }
        }
        
        public void RemoveSingleton<T>()
        {
            Type type = typeof(T);
            _instances.Remove(type);
            
            if (!_injectWaiters.TryGetValue(type, out Action<object> onInject)) return;
            
            var singletonAttr = type.GetCustomAttribute<DynamicSingletonAttribute>();
            if (singletonAttr == null)
            {
                onInject?.Invoke(null);
            }
        }
        
        public bool TryGetSystem(Type type, out object system)
        {
            if (_instances.TryGetValue(type, out object ret))
            {
                system = ret;
                return true;
            }

            system = RegistrySingleton(type);
            return system != null;
        }
        
        public bool TryGetSystem<T>(out T system)
        {
            bool ret = TryGetSystem(typeof(T), out object objSystem);
            system = (T)objSystem;
            return ret;
        }
    }
}