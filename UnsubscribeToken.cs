using System;

namespace DSystem
{
    public class UnsubscribeToken : IDisposable
    {
        private readonly Action<object> _subscribe;
        private readonly Type _typeListener;
        
        internal UnsubscribeToken(Type typeListener, Action<object> subscribe)
        {
            _typeListener = typeListener;
            _subscribe = subscribe;
        }

        public void Dispose()
        {
            MainInjector.Instance.RemoveListenerCatcher(_typeListener, _subscribe);
        }
    }
}