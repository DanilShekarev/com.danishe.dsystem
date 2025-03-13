using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DSystem
{
    [InitializeOnLoad]
    public static class DEditorEntry
    {
        static DEditorEntry()
        {
            DEntry.EditorInitialize();
            Configure();
        }
        
        private static void Configure()
        {
            foreach (var assemblyName in DSystemSettings.Instance.AssembliesNames)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    Configure(assembly);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        
        private static void Configure(Assembly assembly)
        {
            (Type type, AutoRegistryAttribute reg)[] types = assembly.GetTypes()
                .Select(t => (t, t.GetCustomAttribute<AutoRegistryAttribute>()))
                .Where(p => p.Item2 != null)
                .Where(p => p.Item2.Flags.HasFlag(RegistryFlags.EditorRegistry))
                .OrderBy(p => p.Item2.Order).ToArray();
            
            foreach (var pair in types)
                Injector.Instance.CreateAndRegisterInstance(pair.type, pair.reg);
        }
    }
}