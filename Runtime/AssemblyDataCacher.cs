using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DSystem
{
    public static class AssemblyDataCacher
    {
        private static readonly Dictionary<Type, EventListenerAttribute> EventListenersAttributes = new ();

        static AssemblyDataCacher()
        {
            foreach (var assembly in DEntry.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes().Where(t => t.IsClass))
                {
                    var attr = type.GetCustomAttribute<EventListenerAttribute>();
                    if (attr == null)
                        continue;
                    
                    EventListenersAttributes.Add(type, attr);
                }
            }
        }

        public static EventListenerAttribute GetEventListenerAttribute(Type type)
        {
            return EventListenersAttributes.GetValueOrDefault(type);
        }
    }
}