using System;
using System.Collections.Generic;
using System.Reflection;
using DSystem.Debugging;
using DSystem.Utils;
using UnityEngine;

namespace DSystem
{
    public class Injector : IDisposable
    {
        public static Injector Instance { get; internal set; }
        private static readonly Type DActionType = typeof(IDAction);
        
        internal IReadOnlyDictionary<Type, object> Instances => _instances;
        
        private readonly Dictionary<Type, object> _instances = new ();
        private readonly List<IUpdatable> _updatables = new ();
        private readonly Dictionary<Type, List<DynamicField>> _injectWaiters = new ();
        private readonly Dictionary<(FieldInfo f, object instace), DynamicField> _dynamicFields = new();
        
        private List<DynamicField> _currentDynamicFields;
        private Action _onEndUpdateReferences;
        
        private readonly IInjectorDebugger _debugger;
        
        public Injector(IInjectorDebugger debugger = null)
        {
            _debugger = debugger;
        }

        [Obsolete]
        public DAction<T> GetDAction<T>(bool createInstance = true) where T : class
        {
            return DEventSystem.Instance.GetDAction<T>(createInstance);
        }

        [Obsolete]
        public IDAction GetDAction(Type type, bool createInstance = true)
        {
            return DEventSystem.Instance.GetDAction(type, createInstance);
        }
        
        [Obsolete]
        public bool TryGetSystem(Type type, out object system) => TryGetInstance(type, out system);
        [Obsolete]
        public bool TryGetSystem<T>(out T system)
        {
            var ret = TryGetSystem(typeof(T), out var objSystem);
            system = (T)objSystem;
            return ret;
        }

        [Obsolete]
        public void RegistrySingleton(object instance) => RegisterInstance(instance);
        [Obsolete]
        public void RemoveSingleton<T>() => RemoveSingleton(typeof(T));

        [Obsolete]
        public void RemoveSingleton(object instance) => RemoveInstance(instance.GetType());
        
        public bool RegisterInstance(object instance, Type type = null)
        {
            type ??= instance.GetType();
            if (!_instances.TryAdd(instance.GetType(), instance))
                return false;

            UpdateReferences(type, instance);
            
            return true;
        }
        public bool RemoveInstance<T>()=> RemoveInstance(typeof(T));

        public bool RemoveInstance(Type instanceType)
        {
            if (!_instances.Remove(instanceType))
                return false;
            
            UpdateReferences(instanceType, null);
            return true;
        }

        private void UpdateReferences(Type type, object instance)
        {
            //TODO: Refactor to DAction
            if (!_injectWaiters.TryGetValue(type, out var dynamicFields)) 
                return;

            _currentDynamicFields = dynamicFields;
            
            foreach (var dynamicField in dynamicFields)
                dynamicField.UpdateReference(instance);
            
            _currentDynamicFields = null;
            _onEndUpdateReferences?.Invoke();
            _onEndUpdateReferences = null;
            
            if (instance == null) 
                return;
            
            var singletonAttr = type.GetCustomAttribute<DynamicSingletonAttribute>();
            if (singletonAttr == null)
            {
                foreach (var dynamicField in dynamicFields)
                    _dynamicFields.Remove((dynamicField.Field, instance));
                _injectWaiters.Remove(type);
            }
        }

        public bool TryGetInstance<T>(out T instance)
        {
            if (!TryGetInstance(typeof(T), out var temp))
            {
                instance = default(T);
                return false;
            }
            instance = (T)temp;
            return true;
        }

        public bool TryGetInstance(Type type, out object instance)
        {
            if (!_instances.TryGetValue(type, out instance))
                instance = CreateAndRegisterInstance(type, bypassCheck: true);
            return instance != null;
        }

        public T GetInstance<T>() => (T)GetInstance(typeof(T));
        public object GetInstance(Type type)
        {
            TryGetInstance(type, out object instance);
            return instance;
        }
        
        public void RegistryInjection(object instance, bool forceInitializeSystems = false, bool isComponent = true)
        {
            _debugger?.StartInjection(instance);
            
            Type type = instance.GetType();

            Inject(type, instance, forceInitializeSystems, isComponent);

            if (instance is DBehaviour dBehaviour && type.GetCustomAttribute<DisableInitializeAttribute>() != null)
                dBehaviour.Initialize();
            
            _debugger?.EndInjection(instance);
        }
        
        internal void Update()
        {
            foreach (var updatable in _updatables)
            {
                updatable.Update();
            }
        }
        
        private object RegisterScriptable(Type type)
        {
            object instance = Resources.LoadAll(String.Empty, type)[0];
            if (instance == null)
            {
                Debug.LogError($"No found {type.Name} scriptable instance!");
                return null;
            }
            
            RegisterInstance(instance);
            return instance;
        }
        
        public object CreateAndRegisterInstance(Type type, AutoRegistryAttribute reg = null, bool bypassCheck = false)
        {
            if (!bypassCheck && TryGetInstance(type, out var instance))
                return instance;
            
            reg ??= type.GetCustomAttribute<AutoRegistryAttribute>();
            if (reg == null)
            {
                Debug.LogWarning($"Fail register singleton {type.Name}! {type.Name} not have AutoRegistryAttribute!");
                return null;
            }
        
            if (type.IsSubclassOf(typeof(ScriptableObject)))
                return RegisterScriptable(type);
            
            instance = Activator.CreateInstance(type);
            RegisterInstance(instance, type);
            RegistryInjection(instance, true, false);

            if (instance is DSystemBase { AutoRegistrationEvents: true })
                DEventSystem.Instance.Subscribe(instance);
            
            if (instance is IInitializable startable)
                startable.Initialize();
            
            if (instance is IUpdatable updatable)
                _updatables.Add(updatable);
        
            return instance;
        }

        private void InjectToField(FieldInfo field, object instance, object inst, InjectAttribute injectAttr)
        {
            if (instance == null || (instance is UnityEngine.Object obj && obj == null))
            {
                if (!_dynamicFields.Remove((field, instance), out var dynamicField))
                    return;
                
                dynamicField.Dispose();
                return;
            }
            
            var singletonAttr = field.FieldType.GetCustomAttribute<DynamicSingletonAttribute>();

            if (inst == null || singletonAttr != null)
            {
                RegistryWaiter(field, instance, injectAttr);
                if (inst == null)
                    return;
            }
            
            field.SetValue(instance, inst);

            instance.GetType().GetOnInjectMethod(injectAttr)?.Invoke(instance, null);
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
                if (injectAttr == null) 
                    continue;

                var fType = field.FieldType;
                bool isArray = fType.IsArray;
                if (isArray)
                    fType = fType.GetElementType();
                if (fType == null)
                    continue;

                object inst;
                if (DActionType.IsAssignableFrom(fType))
                {
                    var genericType = fType.GetGenericArguments()[0];
                    if (!isComponent | injectAttr.Params.HasFlag(InjectParams.UseGlobal))
                        inst = DEventSystem.Instance.GetDAction(genericType);
                    else
                    {
                        if (instance is DBehaviour dBehaviour)
                        {
                            inst = dBehaviour.GetDAction(genericType);
                        }
                        else
                        {
                            Debug.LogWarning("Trying injection DAction on no DBehaviour!");
                            inst = null;
                        }
                    }
                }
                else if (fType.IsSubclassOf(typeof(Component)))
                {
                    if (!isComponent | injectAttr.Params.HasFlag(InjectParams.UseGlobal))
                    {
                        inst = GetInstance(field.FieldType);
                    }
                    else
                    {
                        inst = instance.GetComponents(fType, isArray, injectAttr);
                        if (inst == null)
                        {
                            Debug.LogWarning($"Fail inject {fType} component for {instance}!");
                            continue;   
                        }
                    }
                } 
                else if (!TryGetInstance(field.FieldType, out inst))
                {
                    var singletonAttr = field.FieldType.GetCustomAttribute<DynamicSingletonAttribute>();
                    if (singletonAttr == null)
                    {
                        if (systemInjection | injectAttr.Params.HasFlag(InjectParams.UseGlobal))
                            inst = CreateAndRegisterInstance(field.FieldType);
                    }
                }
                InjectToField(field, instance, inst, injectAttr);
            }
        }
        
        private void RegistryWaiter(FieldInfo field, object instance, InjectAttribute injectAttr)
        {
            if (!_injectWaiters.TryGetValue(field.FieldType, out var dynamicFields))
            {
                dynamicFields = new List<DynamicField>();
                _injectWaiters.Add(field.FieldType, dynamicFields);
            }

            var dynamicField = new DynamicField(field, instance, injectAttr, f =>
            {
                if (_currentDynamicFields == dynamicFields)
                {
                    _onEndUpdateReferences += Remove;
                    return;
                }
                Remove();
                return;

                void Remove() => dynamicFields.Remove(f);
            });
            
            _dynamicFields.Add((field, instance), dynamicField);
            
            dynamicFields.Add(dynamicField);
        }

        public void Dispose()
        {
            if (_instances == null) return;
            foreach (var pair in _instances)
            {
                if (pair.Value is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}