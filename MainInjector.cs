﻿using System;
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
    
        private Dictionary<Type, object> _instances;
        private Dictionary<Type, Action<object>> _injectWaiters;
        private Dictionary<Type, List<object>> _listeners;
        private Dictionary<Type, Action<object>> _listenersCatchers;
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
            _injectWaiters = new Dictionary<Type, Action<object>>();
            _listeners = new Dictionary<Type, List<object>>();
            _listenersCatchers = new Dictionary<Type, Action<object>>();
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
            var dBehaviours = FindObjectsOfType<DBehaviour>(true);
            foreach (var dBehaviour in dBehaviours)
            {
                Type type = dBehaviour.GetType();
                
                Inject(type, dBehaviour);
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
                // if (field.FieldType.IsGenericType)
                // {
                //     var typeGeneric = field.FieldType.GetGenericTypeDefinition();
                //     if (typeGeneric != typeof(Sender<>)) continue;
                //     var typeCather = field.FieldType.GetGenericArguments()[0];
                //     object senderInst = Activator.CreateInstance(field.FieldType);
                //     
                //     if (_senders.TryGetValue(typeCather, out List<object> senders))
                //         senders.Add(senderInst);
                //     else
                //         _senders.Add(typeCather, new List<object> {senderInst});
                //     
                //     field.SetValue(instance, senderInst);
                //     continue;
                // }
                var injectAttr = field.GetCustomAttribute<InjectAttribute>(false);
                if (injectAttr == null) continue;
                
                if (field.FieldType.IsSubclassOf(typeof(Component)))
                {
                    if (isSystem)
                    {
                        void OnInjected(object inst)
                        {
                            field.SetValue(instance, inst);
                            if (string.IsNullOrEmpty(injectAttr.EventName)) return;
                            var method = type.GetMethod(injectAttr.EventName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                            method?.Invoke(instance, null);
                        }
                        if (_instances.TryGetValue(field.FieldType, out object instanceObj))
                        {
                            OnInjected(instanceObj);
                        }
                        else
                        {
                            if (_injectWaiters.TryGetValue(field.FieldType, out Action<object> eventInst))
                            {
                                eventInst += OnInjected;
                                _injectWaiters.Remove(field.FieldType);
                                _injectWaiters.Add(field.FieldType, eventInst);
                            }
                            else
                            {
                                _injectWaiters.Add(field.FieldType, OnInjected);
                            }
                        }
                    }
                    else
                    {
                        field.SetValue(instance, (instance as MonoBehaviour)?.GetComponentInChildren(field.FieldType));
                    }
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

        public void RegistryListener(object listener, Type listenerType)
        {
            List<object> listeners;
            if (_listeners.TryGetValue(listenerType, out List<object> list))
            {
                listeners = list;
            }
            else
            {
                listeners = new List<object>();
                _listeners.Add(listenerType, listeners);
            }
            listeners.Add(listener);
            
            if (_listenersCatchers.TryGetValue(listenerType, out Action<object> onCatchListener))
            {
                onCatchListener.Invoke(listener);
            }
        }

        public void RemoveListener(object listener, Type listenerType)
        {
            if (_listeners.TryGetValue(listenerType, out List<object> listeners))
            {
                listeners.Remove(listener);
            }
        }

        public UnsubscribeToken RegistryListenerCatcher<T>(Action<T> onCatchListener) where T : class
        {
            Action<object> subscribe = (listener) =>
            {
                onCatchListener?.Invoke(listener as T);
            };
            var type = typeof(T);
            UnsubscribeToken token = new UnsubscribeToken(type, subscribe);
            if (_listenersCatchers.TryGetValue(type, out Action<object> onCatch))
            {
                onCatch += subscribe;
                _listenersCatchers.Remove(type);
                _listenersCatchers.Add(type, onCatch);
                return token;
            }
            _listenersCatchers.Add(type, subscribe);
            return token;
        }

        internal void RemoveListenerCatcher(Type type, Action<object> catcher)
        {
            if (_listenersCatchers.TryGetValue(type, out Action<object> onCatch)) return;
            onCatch -= catcher;
            _listenersCatchers.Remove(type);
            _listenersCatchers.Add(type, onCatch);
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

        public void InvokeListeners<T>(Action<T> action) where T : class
        {
            if (!_listeners.TryGetValue(typeof(T), out List<object> listeners)) return;
            foreach (var listener in listeners)
            {
                action.Invoke(listener as T);
            }
        }

        public IEnumerable<T> ForeachListeners<T>() where T : class
        {
            if (!_listeners.TryGetValue(typeof(T), out List<object> listeners)) yield break;
            foreach (var l in listeners)
            {
                yield return l as T;
            }
        }

        public void RegistrySingleton(object instance)
        {
            Type type = instance.GetType();
            _instances.Add(type, instance);
            
            if (!_injectWaiters.TryGetValue(type, out Action<object> onInject)) return;
            
            onInject?.Invoke(instance);
            _injectWaiters.Remove(type);
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