using System;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RegistryListenersAttribute : Attribute
    {
        internal Type[] Types { get; private set; }
        
        public RegistryListenersAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}