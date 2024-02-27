using System;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ListenerAttribute : Attribute
    {
        internal bool Global { get; private set; }
        
        public ListenerAttribute(bool global = true)
        {
            Global = global;
        }
    }
}