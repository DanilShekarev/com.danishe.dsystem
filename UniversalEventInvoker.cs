using System;
using System.Reflection;
using UnityEngine;

namespace DSystem
{
    [AddComponentMenu("Universal Event Invoker (Experimental)")]
    public class UniversalEventInvoker : MonoBehaviour
    {
        [SerializeField] private string interfaceType;
        [SerializeField] private string fileGUID;
        [SerializeField] private string methodName;

        private Type _type;
        private MethodInfo _method;

        public void Invoke()
        {
            _type ??= MainInjector.Instance.GetTypeFromName(interfaceType);

            if (_type == null) return;
            
            _method ??= _type.GetMethod(methodName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
            
            MainInjector.Instance.GetDAction(_type)?.Invoke(listener =>
            {
                _method.Invoke(listener, new [] {listener});
            });
        }
    }
}