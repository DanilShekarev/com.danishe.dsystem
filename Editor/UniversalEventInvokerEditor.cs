using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DSystem.Editor
{
    [CustomEditor(typeof(UniversalEventInvoker))]
    public class UniversalEventInvokerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var type = UpdateInterfaceData();
            UpdateMethodData(type);
        }

        private Type UpdateInterfaceData()
        {
            var interfaceTypeProp = serializedObject.FindProperty("interfaceType");
            var fileGUIDProp = serializedObject.FindProperty("fileGUID");
            var interfaceName = interfaceTypeProp.stringValue;
            var currentIndex = Array.IndexOf(CompileTimeData.ListenersNames, interfaceName);

            if (currentIndex == -1)
            {
                var guid = fileGUIDProp.stringValue;
                interfaceName = guid.GetTypeNameFromGUID();
                currentIndex = Array.IndexOf(CompileTimeData.ListenersNames, interfaceName);
            }
            
            var newIndex = EditorGUILayout.Popup("Interface type", 
                Mathf.Max(currentIndex, 0), CompileTimeData.ListenersNames);

            if (newIndex == currentIndex) 
                return CompileTimeData.Listeners[newIndex].type;

            interfaceTypeProp.stringValue = CompileTimeData.ListenersNames[newIndex];
            fileGUIDProp.stringValue = CompileTimeData.Listeners[newIndex].type.GetFileGUIDFromType();

            serializedObject.ApplyModifiedProperties();
            
            return CompileTimeData.Listeners[newIndex].type;
        }

        private void UpdateMethodData(Type type)
        {
            var methodNameProp = serializedObject.FindProperty("methodName");
            var methodName = methodNameProp.stringValue;
            
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic 
                                                              | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .Where(m => m.GetParameters().Length == 0);
            
            if (!methods.Any()) return;
            
            var methodsNames = methods.Select(m => m.Name).ToArray();

            var index = Array.IndexOf(methodsNames, methodName);
            var newIndex = EditorGUILayout.Popup("Method", Mathf.Max(index, 0), methodsNames);

            if (newIndex == index) return;

            methodName = methodsNames[newIndex];
            methodNameProp.stringValue = methodName;
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}