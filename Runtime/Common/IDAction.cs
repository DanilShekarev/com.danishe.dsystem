using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace DSystem
{
    public interface IDAction
    {
        public void Invoke(Action<object> action);
        public IEnumerator<object> GetEnumerator();
        
        public RegistryResult RegistryListener(object listener);
        public void RemoveListener(object listener);
        
        public UnsubscribeToken RegistryCatcher(Action<object> onCatchListener);
        public UnsubscribeToken RegistryUnsubscribeCatcher(Action<object> onRemoveListener);
        
        public bool RegisterHandler(EventHandler<object> handler);
        public bool RemoveHandler(EventHandler<object> handler);
        
        [Pure]
        public static IDAction Create(Type type)
        {
            return Activator.CreateInstance(typeof(DAction<>).MakeGenericType(type)) as IDAction;
        }
    }
}