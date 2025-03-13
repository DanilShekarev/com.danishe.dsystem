using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DSystem.Debugging;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DSystem
{
    public static class DEntry
    {
        public static Assembly MainAssembly { get; private set; }

        private static Injector _injector;
        
        public static event Action<Scene, LoadSceneMode> SceneInjected;
        
        #if UNITY_EDITOR
        public static readonly InjectorDebugger InjectorDebugger = new ();
        private static IInjectorDebugger _injectorDebugger => InjectorDebugger;
        
        public static void EditorInitialize()
        {
            FirstStepInitialize(_injectorDebugger);
            
        }
        #else
        private static IInjectorDebugger _injectorDebugger => null;
        #endif
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            #if !UNITY_EDITOR
            FirstStepInitialize(_injectorDebugger);
            #endif
            
            Configure();
            
            SceneManager.sceneLoaded += LoadedScene;
            
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var subSub = playerLoop.subSystemList[5].subSystemList;
            PlayerLoopSystem[] systems = new PlayerLoopSystem[subSub.Length + 1];
            for (int i = 0; i < systems.Length; i++)
            {
                if (i != 0)
                {
                    systems[i] = subSub[i-1];
                }
                else
                {
                    systems[i] = new PlayerLoopSystem
                    {
                        type = typeof(DEntry)
                    };
                    systems[i].updateDelegate += OnUpdate;
                }
            }
            
            playerLoop.subSystemList[5].subSystemList = systems;
            
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        private static void OnUpdate()
        {
            _injector.Update();
        }

        private static void FirstStepInitialize(IInjectorDebugger debugger = null)
        {
            MainAssembly = Assembly.Load("Assembly-CSharp");
            DEventSystem.Instance = new DEventSystem();
            _injector = new Injector(debugger);
            Injector.Instance = _injector;
        }
        
        public static Type GetTypeFromName(string typeName)
        {
            return MainAssembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
        }
        
        private static void LoadedScene(Scene arg0, LoadSceneMode arg1)
        {
            InjectScene();
            SceneInjected?.Invoke(arg0, arg1);
        }
        
        private static void InjectScene()
        {
            var counterInject = 0;
            var counterInited = 0;
            var dBehaviours = Object.FindObjectsOfType<DBehaviour>(true);
            foreach (var dBehaviour in dBehaviours)
            {
                counterInject++;
        
                if (!dBehaviour.enabled)
                    continue;
                if (dBehaviour.GetType().GetCustomAttribute<DisableInitializeAttribute>() == null)
                    continue;
                
                counterInited++;
                dBehaviour.Initialize();
            }
            Debug.Log($"DSystem inject to {counterInject} and inited {counterInited} objects.");
        }

        internal static IEnumerable<Assembly> GetAssemblies()
        {
            foreach (var assemblyName in DSystemSettings.Instance.AssembliesNames)
            {
                Assembly assembly = null;
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                if (assembly != null)
                    yield return assembly;
            }
        }
        
        private static void Configure()
        {
            foreach (var assembly in GetAssemblies())
            {
                try
                {
                    Configure(assembly);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private static IEnumerable<(Type type, AutoRegistryAttribute reg)> GetTypes(Assembly assembly)
        {
            return assembly.GetTypes()
                .Select(t => (t, t.GetCustomAttribute<AutoRegistryAttribute>()))
                .Where(p => p.Item2 != null).OrderBy(p => p.Item2.Order);
        }
        
        private static void Configure(Assembly assembly)
        {
            foreach (var pair in GetTypes(assembly))
                _injector.CreateAndRegisterInstance(pair.type, pair.reg);
        
            var scriptableCount = _injector.Instances.Count(i => 
                i.Key.IsSubclassOf(typeof(ScriptableObject)));
            
            Debug.Log($"{assembly.GetName().Name} DSystem register {_injector.Instances.Count - scriptableCount}" +
                      $" systems and {scriptableCount} scriptable objects.");
        }
    }
}