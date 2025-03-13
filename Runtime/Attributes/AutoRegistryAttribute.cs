using System;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegistryAttribute : Attribute
    {
        public readonly int Order;
        public readonly RegistryFlags Flags;
        
        public AutoRegistryAttribute()
        {
            
        }

        public AutoRegistryAttribute(int order = 0)
        {
            Order = order;
        }

        public AutoRegistryAttribute(int order = 0, RegistryFlags flags = RegistryFlags.None)
        {
            Order = order;
            Flags = flags;
        }
        
        [Obsolete]
        public AutoRegistryAttribute(string fileScriptable) {}
    }

    [Flags]
    public enum RegistryFlags
    {
        None = 0, EditorRegistry = 1
    }
}