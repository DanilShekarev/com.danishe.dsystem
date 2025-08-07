using System;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegistryAttribute : Attribute
    {
        public readonly int Order;
        public readonly RegistryFlags Flags;
        
        public AutoRegistryAttribute() { }

        public AutoRegistryAttribute(int order = 0)
        {
            Order = order;
        }

        public AutoRegistryAttribute(int order = 0, RegistryFlags flags = RegistryFlags.None) : this(order)
        {
            Flags = flags;
        }
    }

    [Flags]
    public enum RegistryFlags
    {
        None = 0, EditorRegistry = 1
    }
}