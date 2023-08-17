using System;

namespace DSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegistryAttribute : Attribute
    {
        public int Order { get; }

        public AutoRegistryAttribute(int order = 0)
        {
            Order = order;
        }
    }
}