using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DSystem.Utils
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<T> GetAttributes<T>(this Type type) where T : Attribute
        {
            var temp = type;
            while (true)
            {
                var attributes = temp.GetCustomAttributes<T>(false);
                foreach (var attribute in attributes)
                {
                    yield return attribute;
                }
                temp = temp.BaseType;
                if (temp == null || temp == typeof(MonoBehaviour) || temp == typeof(DBehaviour))
                {
                    yield break;
                }
            }
        }

        public static MethodInfo GetOnInjectMethod(this Type type, InjectAttribute injectAttribute)
        {
            if (!string.IsNullOrEmpty(injectAttribute.EventName))
            {
                return type.GetMethod(injectAttribute.EventName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly |
                    BindingFlags.Instance);
            }
            return null;
        }
    }
}