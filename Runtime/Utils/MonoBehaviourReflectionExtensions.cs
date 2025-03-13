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
                if (isParent)
                    return mono.GetComponentsInParent(fType, includeInactive);
                return mono.GetComponentsInChildren(fType, includeInactive);
            }
            if (isParent)
                return mono.GetComponentInParent(fType, includeInactive);
            return mono.GetComponentInChildren(fType, includeInactive);
        }
    }
}