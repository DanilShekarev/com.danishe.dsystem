using System;

namespace DSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ListenerAttribute : Attribute
    {
        internal Type SingletonType { get; private set; }
        internal Type DataType { get; private set; }
        internal bool DisableSubscriber { get; private set; }

        public ListenerAttribute(Type singletonType, Type dataType, bool disableSubscriber = false)
        {
            SingletonType = singletonType;
            DataType = dataType;
            DisableSubscriber = disableSubscriber;
        }
    }
}