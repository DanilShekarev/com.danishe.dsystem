﻿using System.Collections.Generic;
using UnityEngine;

namespace DSystem
{
    public class DSystemSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        public static DSystemSettings Instance
        {
            get
            {
                var array = Resources.LoadAll<DSystemSettings>(""); 
                if (array is { Length: > 0 })
                    _instance ??= array[0];
                if (_instance == null)
                {
                    _instance = CreateInstance<DSystemSettings>();
                    #if UNITY_EDITOR
                    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                    {
                        UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    UnityEditor.AssetDatabase.CreateAsset(_instance, "Assets/Resources/DSystemSettings.asset");
                    #endif
                }
                return _instance;
            }
        }
        
        private static DSystemSettings _instance;

        public IReadOnlyCollection<string> AssembliesNames => assembliesToInject;

        [SerializeField] private string[] assembliesToInject;

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            if (assembliesToInject == null || assembliesToInject.Length == 0)
            {
                assembliesToInject = new []
                {
                    "DSystem",
                    "Assembly-CSharp"
                };
            }
        }
    }
}