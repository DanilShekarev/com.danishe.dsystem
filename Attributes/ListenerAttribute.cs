using System;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ListenerAttribute : Attribute
    {
        public bool Global { get; private set; }
        internal bool Up { get; private set; }
        
        public ListenerAttribute(bool global = true, bool up = false)
        {
            Global = global;
            Up = up;
        }
    }
}