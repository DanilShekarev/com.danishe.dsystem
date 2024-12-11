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
        private Dictionary<Type, IDAction> _actions;
        private List<IUpdatable> _updatables;

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
            
            _getComponentsInChildrens = typeof(Component).GetMethod(nameof(GetComponentsInChildren), 
                new [] {typeof(bool)});

            _instances = new Dictionary<Type, object>();
            _injectWaiters = new Dictionary<Type, Action<object>>();
            _updatables = new List<IUpdatable>();
            _actions = new();
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

            if (_instances == null) return;
            foreach (var pair in _instances)
            {
                if (pair.Value is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private void Configure()
        {
            var assembly = Assembly.Load("Assembly-CSharp");
            _mainAssembly = assembly;
            var assemblyDSystem = Assembly.Load("DSystem");
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

            var scriptableCount = _instances.Count(i => i.Key.IsSubclassOf(typeof(ScriptableObject)));
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

        private static void InjectScene()
        {
            var counterInject = 0;
            var counterInited = 0;
            var dBehaviours = FindObjectsOfType<DBehaviour>(true);
            foreach (var dBehaviour in dBehaviours)
            {
                counterInject++;

                if (dBehaviour.GetType().GetCustomAttribute<DisableInitializeAttribute>() == null) continue;
                if (!dBehaviour.enabled) continue;
                counterInited++;
                dBehaviour.Initialize();
            }
            Debug.Log($"DSystem inject to {counterInject} and inited {counterInited} objects.");
        }

        private object RegistrySingleton(Type type, AutoRegistryAttribute reg = null)
        {
            reg ??= type.GetCustomAttribute<AutoRegistryAttribute>();
            if (reg == null) return null;

            if (!string.IsNullOrEmpty(reg.NameScriptable))
                return RegisterScriptable(type, reg.NameScriptable);
            
            var instance = Activator.CreateInstance(type);
            _instances.Add(instance.GetType(), instance);
            
            RegistryInjection(instance, true, false);

            if (instance is IInitializable startable)
                startable.Initialize();

            if (instance is IUpdatable updatable)
                _updatables.Add(updatable);

            return instance;
        }

        private void Inject(Type type, object instance, bool systemInjection = false, bool isComponent = true)
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

                if (fType.IsSubclassOf(typeof(Component)))
                {
                    if (!isComponent | injectAttr.Params.HasFlag(InjectParams.UseGlobal))
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

                void OnInjected(object inst)
                {
                    if (instance == null || (instance is UnityEngine.Object obj && obj == null))
                    {
                        if (!_injectWaiters.TryGetValue(field.FieldType, out var eventInst)) return;
                        eventInst -= OnInjected;
                        _injectWaiters.Remove(field.FieldType);
                        _injectWaiters.Add(field.FieldType, eventInst);
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
                        if (_injectWaiters.TryGetValue(field.FieldType, out var eventInst))
                        {
                            eventInst -= OnInjected;
                            _injectWaiters.Remove(field.FieldType);
                            _injectWaiters.Add(field.FieldType, eventInst);
                        }
                    }
                }
            }
        }

        private void RegistryWaiter(FieldInfo field, Action<object> action)
        {
            if (_injectWaiters.TryGetValue(field.FieldType, out var eventInst))
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

        public Type GetTypeFromName(string typeName)
        {
            return _mainAssembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
        }
        
        public void RegistryInjection(object instance, bool forceInitializeSystems = false, bool isComponent = true)
        {
            Type type = instance.GetType();

            Inject(type, instance, forceInitializeSystems, isComponent);

            if (instance.GetType().GetCustomAttribute<DisableInitializeAttribute>() != null && instance is DBehaviour dBehaviour)
            {
                dBehaviour.Initialize();
            }
        }

        public DAction<T> GetDAction<T>(bool createInstance = true) where T : class
        {
            return GetDAction(typeof(T), createInstance) as DAction<T>;
        }

        public IDAction GetDAction(Type type, bool createInstance = true)
        {
            if (_actions.TryGetValue(type, out var action) || !createInstance) return action;
            action = IDAction.Create(type);
            _actions.Add(type, action);
            return action;
        }
        
        [Obsolete]
        public RegistryResult RegistryListener<T>(T listener) where T : class
        {
            return GetDAction<T>().RegistryListener(listener);
        }

        [Obsolete]
        public void RemoveListener<T>(T listener) where T : class
        {
            GetDAction<T>(false)?.RemoveListener(listener);
        }

        [Obsolete]
        public UnsubscribeToken RegistryListenerCatcher<T>(Action<T> onCatchListener) where T : class
        {
            return GetDAction<T>().RegistryCatcher(onCatchListener);
        }

        [Obsolete]
        public UnsubscribeToken RegistryUnsubscribeListenerCatcher<T>(Action<T> onCatchListener) where T : class
        {
            return GetDAction<T>().RegistryUnsubscribeCatcher(onCatchListener);
        }
        
        [Obsolete]
        public void InvokeListenersReflection(Type interfaceType, Action<object> action)
        {
            var invokeMethod = GetType().GetMethod(nameof(InvokeListeners)).MakeGenericMethod(interfaceType);
            invokeMethod.Invoke(this, new object[] {action});
        }

        [Obsolete]
        public void InvokeListeners<T>(Action<T> action) where T : class
        {
            GetDAction<T>(false)?.Invoke(action);
        }

        [Obsolete]
        public IEnumerable<T> ForeachListeners<T>() where T : class
        {
            var action = GetDAction<T>(false);
            if (action == null)
                yield break;
                
            foreach (var val in action)
            {
                yield return val;
            }
        }

        public void RegistrySingleton(object instance)
        {
            Type type = instance.GetType();
            _instances.Add(type, instance);
            
            if (!_injectWaiters.TryGetValue(type, out var onInject)) return;
            
            onInject?.Invoke(instance);
            var singletonAttr = type.GetCustomAttribute<DynamicSingletonAttribute>();
            if (singletonAttr == null)
                _injectWaiters.Remove(type);
        }
        
        public void RemoveSingleton(object instance)
        {
            Type type = instance.GetType();
            _instances.Remove(type);
            
            if (!_injectWaiters.TryGetValue(type, out var onInject)) return;
            
            var singletonAttr = type.GetCustomAttribute<DynamicSingletonAttribute>();
            if (singletonAttr == null)
            {
                onInject?.Invoke(null);
            }
        }
        
        public void RemoveSingleton<T>()
        {
            var type = typeof(T);
            _instances.Remove(type);
            
            if (!_injectWaiters.TryGetValue(type, out var onInject)) return;
            
            var singletonAttr = type.GetCustomAttribute<DynamicSingletonAttribute>();
            if (singletonAttr == null)
            {
                onInject?.Invoke(null);
            }
        }
        
        public bool TryGetSystem(Type type, out object system)
        {
            if (_instances.TryGetValue(type, out var ret))
            {
                system = ret;
                return true;
            }

            system = RegistrySingleton(type);
            return system != null;
        }
        
        public bool TryGetSystem<T>(out T system)
        {
            var ret = TryGetSystem(typeof(T), out var objSystem);
            system = (T)objSystem;
            return ret;
        }
    }
}