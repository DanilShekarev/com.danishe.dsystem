using System;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventListenerAttribute : Attribute
    {
        public readonly short Order;
        public readonly ListenerFlags ListenerFlags;

        public EventListenerAttribute(short order = 0)
        {
            Order = order;
        }

        public EventListenerAttribute(ListenerFlags flags = ListenerFlags.None)
        {
            ListenerFlags = flags;
        }

        public EventListenerAttribute(short order = 0, ListenerFlags flags = ListenerFlags.None) : this(order)
        {
            ListenerFlags = flags;
        }
    }
    
    
    [Flags]
    public enum ListenerFlags
    {
        None = 0, RiseEvent = 1, ChangeState = 2, DestroyObj = 4
    }
}