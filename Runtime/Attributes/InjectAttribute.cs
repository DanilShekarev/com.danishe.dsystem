using System;

namespace DSystem
{
    [Flags]
    public enum InjectParams
    {
        None = 0, IncludeInactive = 1, UseGlobal = 2, GetInParents = 4
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
        internal string EventName { get; private set; }
        internal InjectParams Params { get; private set; }

        public InjectAttribute() { }
        
        public InjectAttribute(string eventName)
        {
            EventName = eventName;
        }
        
        public InjectAttribute(InjectParams @params)
        {
            Params = @params;
        }
        
        public InjectAttribute(string eventName, InjectParams @params) : this(eventName)
        {
            Params = @params;
        }
    }
}