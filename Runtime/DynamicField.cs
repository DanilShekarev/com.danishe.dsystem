using System;
using System.Reflection;
using DSystem.Utils;
using UnityEngine;

namespace DSystem
{
    internal class DynamicField : IDisposable
    {
        public readonly FieldInfo Field;
        public readonly object Instance;
        private readonly MethodInfo _onInjectedMethod;
        private readonly Action<DynamicField> _remove;

        public DynamicField(FieldInfo field, object instance, InjectAttribute injectAttr, Action<DynamicField> remove)
        {
            Field = field;
            Instance = instance;
            _remove = remove;

            _onInjectedMethod = instance.GetType().GetOnInjectMethod(injectAttr);
        }

        public void UpdateReference(object newReference)
        {
            if (Instance == null || (Instance is UnityEngine.Object obj && obj == null))
            {
                _remove?.Invoke(this);
                return;
            }

            Field.SetValue(Instance, newReference);

            try
            {
                _onInjectedMethod?.Invoke(Instance, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Dispose()
        {
            _remove.Invoke(this);
        }
    }
}