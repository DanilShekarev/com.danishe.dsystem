using System;
using System.Reflection;
using UnityEngine;

namespace DSystem
{
    //TODO: Move class to DSystemUtils
    [AddComponentMenu("Universal Event Invoker (Experimental)")]
    public class UniversalEventInvoker : MonoBehaviour
    {
        //TODO: Refactor to TypeContainer
        [SerializeField] private string interfaceType;
        [SerializeField] private string fileGUID;
        [SerializeField] private string methodName;

        private Type _type;
        private MethodInfo _method;

        public void Invoke()
        {
            _type ??= DEntry.GetTypeFromName(interfaceType);

            if (_type == null) return;
            
            _method ??= _type.GetMethod(methodName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
            
            DEventSystem.Instance.GetDAction(_type)?.Invoke(listener =>
            {
                _method.Invoke(listener, new [] {listener});
            });
        }
    }
}