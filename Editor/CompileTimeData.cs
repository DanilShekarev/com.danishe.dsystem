using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace DSystem
{
    public static class CompileTimeData
    {
        public static Assembly Assembly { get; private set; }
        public static (Type type, ListenerAttribute reg)[] Listeners { get; private set; }

        public static string[] ListenersNames
        {
            get { return _listenersNames ??= Listeners.Select(p => p.type.Name).ToArray(); }
        }

        private static string[] _listenersNames;

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            Assembly = Assembly.Load("Assembly-CSharp");
            Listeners = Assembly.GetTypes().Where(t => t.IsInterface)
                .Select(t => (t, t.GetCustomAttribute<ListenerAttribute>()))
                .Where(p => p.Item2 is { Global: true }).ToArray();
        }
        
        public static string GetFileGUIDFromType(this Type type)
        {
            var assets = AssetDatabase.FindAssets(type.Name);
            if (assets == null || assets.Length == 0) return default;
            if (assets.Length > 1)
            {
                var files = assets.Select(AssetDatabase.GUIDToAssetPath);
                Debug.LogWarning($"Found more then one interface assets: {string.Join(" ,", files)}");
            }
            return assets[0];
        }

        public static string GetTypeNameFromGUID(this string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return Path.GetFileName(path).Replace(".cs", "");
        }
    }
}