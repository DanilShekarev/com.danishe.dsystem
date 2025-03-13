using System;

namespace DSystem
{
    public class UnsubscribeToken : IDisposable
    {
        private readonly Action _remove;

        internal UnsubscribeToken(Action remove)
        {
            _remove = remove;
        }

        public void Dispose()
        {
            _remove?.Invoke();
        }
    }
}