using System;

namespace DSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegistryAttribute : Attribute
    {
        public int Order { get; }
        public string NameScriptable { get; }
        
        public AutoRegistryAttribute()
        {
            
        }

        public AutoRegistryAttribute(int order)
        {
            Order = order;
        }
        
        public AutoRegistryAttribute(string fileScriptable)
        {
            NameScriptable = fileScriptable;
        }
    }
}