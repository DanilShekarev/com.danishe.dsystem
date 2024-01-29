using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DSystem.Attributes;
using DSystem.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DSystem
{
    [DefaultExecutionOrder(-100)]
    public sealed class MainInjector : MonoBehaviour
    {
        public static MainInjector Instance { get; private set; }
    
        private Dictionary<Type, object> _instances;
        private List<IUpdatable> _updatables;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            
            Instance = this;
            
            _instances = new Dictionary<Type, object>();
            _updatables = new List<IUpdatable>();
            Configure();

            SceneManager.sceneLoaded += LoadedScene;
        }

        private void LoadedScene(Scene arg0, LoadSceneMode arg1)
        {
            InjectScene();
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
            Debug.Log($"DSystem register {_instances.Count - scriptableCount} systems and {scriptableCount} scriptable objects.");
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
            var monos = FindObjectsOfType<MonoBehaviour>(true);
            foreach (var mono in monos)
            {
                Type type = mono.GetType();
                
                if (type.GetCustomAttribute<InjectAttribute>() != null)
                {
                    Inject(type, mono);
                    counterInject++;
                }

                if (mono is IDisableInitialize disableInitialize)
                {
                    if (!mono.enabled) continue;
                    counterInited++;
                    disableInitialize.Initialize();
                }
            }
            Debug.Log($"DSystem inject to {counterInject} and inited {counterInited} objects.");
        }

        private object RegistrySingleton(Type type, AutoRegistryAttribute reg = null)
        {
            object instance;
            reg ??= type.GetCustomAttribute<AutoRegistryAttribute>();

            if (!string.IsNullOrEmpty(reg.NameScriptable))
            {
                return RegisterScriptable(type, reg.NameScriptable);
            }
            
            instance = Activator.CreateInstance(type);

            _instances.Add(instance.GetType(), instance);
            
            RegistryInjection(instance, true);

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

        private void Inject(Type type, object instance, bool systemInjection = false)
        {
            if (type == typeof(System.Object) || type == typeof(MonoBehaviour)) return;
            
            if (type.BaseType != null)
                Inject(type.BaseType, instance);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.SetField);
            
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<InjectAttribute>(false) == null) continue;

                if (field.FieldType.IsSubclassOf(typeof(Component)))
                {
                    field.SetValue(instance, (instance as MonoBehaviour)?.GetComponentInChildren(field.FieldType));
                } else if (_instances.TryGetValue(field.FieldType, out object obj))
                {
                    field.SetValue(instance, obj);
                } else if (systemInjection)
                {
                    object instanceSystem = RegistrySingleton(field.FieldType);
                    field.SetValue(instance, instanceSystem);
                }
            }
        }

        public void RegistryInjection(object instance, bool isSystem = false)
        {
            Type type = instance.GetType();

            Inject(type, instance, isSystem);

            if (instance is IDisableInitialize ds) ds.Initialize();
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