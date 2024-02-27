using System;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
        internal string EventName { get; private set; }

        public InjectAttribute() { }
        
        public InjectAttribute(string eventName)
        {
            EventName = eventName;
        }
    }
}