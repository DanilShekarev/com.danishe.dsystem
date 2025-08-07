using System;
using UnityEngine;

namespace DSystem.Utils
{
    public static class MonoBehaviourReflectionExtensions
    {
        public static object GetComponents(this object instance, Type fType, bool isArray, InjectAttribute injectAttribute)
        {
            if (instance is not MonoBehaviour mono)
            {
                Debug.LogError($"{instance.GetType().Name} is not a MonoBehaviour!");
                return null;
            }

            bool isParent = injectAttribute.Params.HasFlag(InjectParams.GetInParents);
            bool includeInactive = injectAttribute.Params.HasFlag(InjectParams.IncludeInactive);

            if (isArray)
            {
                Component[] components;
                if (isParent)
                    components = mono.GetComponentsInParent(fType, includeInactive);
                else
                    components = mono.GetComponentsInChildren(fType, includeInactive);

                var newArray = Array.CreateInstance(fType, components.Length);
                Array.Copy(components, 0, newArray, 0, components.Length);
                return newArray;
            }
            if (isParent)
                return mono.GetComponentInParent(fType, includeInactive);
            return mono.GetComponentInChildren(fType, includeInactive);
        }
    }
}