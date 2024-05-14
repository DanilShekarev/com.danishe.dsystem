using System;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
        internal string EventName { get; private set; }
        internal bool IncludeInactive { get; private set; }
        internal bool UseGlobal { get; private set; }

        public InjectAttribute() { }
        
        public InjectAttribute(string eventName)
        {
            EventName = eventName;
        }
        
        public InjectAttribute(bool includeInactive)
        {
            IncludeInactive = includeInactive;
        }

        public InjectAttribute(string eventName, bool useGlobal) : this(eventName)
        {
            UseGlobal = useGlobal;
        }
        
        public InjectAttribute(bool includeInactive, bool useGlobal) : this(includeInactive)
        {
            UseGlobal = useGlobal;
        }
    }
}