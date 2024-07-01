using System;
using JetBrains.Annotations;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Class), BaseTypeRequired(typeof(DBehaviour))]
    public class RegistryListenersAttribute : Attribute
    {
        internal Type[] Types { get; private set; }
        
        public RegistryListenersAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}