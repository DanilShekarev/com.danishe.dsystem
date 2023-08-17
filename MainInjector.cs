using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DSystem.Attributes;
using DSystem.Interfaces;
using UnityEngine;

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
            _instances = new Dictionary<Type, object>();
            _updatables = new List<IUpdatable>();
            Configure();

            Instance = this;
            
            InitializeDisableBehaviour();
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
                RegistrySingleton(pair.type);
            }
        }

        private void InitializeDisableBehaviour()
        {
            var dBehaviours = FindObjectsOfType<DBehaviour>(true).Where(b => b.GetType().GetCustomAttribute<DisableInitializeAttribute>() != null);
            foreach (var dBehaviour in dBehaviours)
            {
                dBehaviour.DisableInitialize();
            }
        }

        private void RegistrySingleton(Type type)
        {
            var instance = Activator.CreateInstance(type);

            RegistryInjection(instance);
        
            _instances.Add(instance.GetType(), instance);
        
            if (instance is IInitializable startable)
            {
                startable.Initialize();
            }

            if (instance is IUpdatable updatable)
            {
                _updatables.Add(updatable);
            }
        }

        private void Inject(Type type, object instance)
        {
            if (type == typeof(System.Object) || type == typeof(MonoBehaviour)) return;
            
            if (type.BaseType != null)
            {
                Inject(type.BaseType, instance);
            }
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                        | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.SetField);
            
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<InjectAttribute>(false) == null) continue;

                if (field.FieldType.IsSubclassOf(typeof(Component)))
                {
                    field.SetValue(instance, (instance as MonoBehaviour)?.GetComponent(field.FieldType));
                } else if (_instances.TryGetValue(field.FieldType, out object obj))
                {
                    field.SetValue(instance, obj);
                }
            }
        }

        internal void RegistryInjection(object instance)
        {
            Type type = instance.GetType();

            Inject(type, instance);
        }
        
        public bool TryGetSystem(Type type, out object system)
        {
            if (_instances.TryGetValue(type, out object ret))
            {
                system = ret;
                return true;
            }

            system = default;
            return false;
        }
    }
}