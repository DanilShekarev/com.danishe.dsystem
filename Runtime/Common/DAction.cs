using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DSystem
{
    public sealed class DAction<T> : IDAction, IEnumerable<T> where T : class
    {
        private readonly List<Action<T>> _catchers = new ();
        private readonly List<Action<T>> _unsubscribeCatchers = new ();
        private readonly List<EventHandler<T>> _handlers = new ();
        private readonly List<T> _listeners = new ();
        
        public readonly Queue<T> NewListeners = new ();
        public readonly Queue<T> RemovedListeners = new ();
        public int Active;
        
        void IDAction.Invoke(Action<object> action) => Invoke(action);
        IEnumerator<object> IDAction.GetEnumerator() => GetEnumerator();
        RegistryResult IDAction.RegistryListener(object listener) => RegistryListener(listener as T);
        void IDAction.RemoveListener(object listener) => RemoveListener(listener as T);
        UnsubscribeToken IDAction.RegistryCatcher(Action<object> onCatchListener) 
            => RegistryCatcher(onCatchListener);
        UnsubscribeToken IDAction.RegistryUnsubscribeCatcher(Action<object> onRemoveListener)
            => RegistryUnsubscribeCatcher(onRemoveListener);

        public void Invoke(Action<T> action)
        {
            Active++;
            foreach (var listener in _listeners)
            {
                var lastHandler = listener;
                if (_handlers.Count > 0)
                {
                    foreach (var handler in _handlers.OrderByDescending(h => h.GetPriority()))
                    {
                        handler.Listener = lastHandler;
                        lastHandler = handler as T;
                    }
                }

                try
                {
                    action.Invoke(lastHandler);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Active--;
            
            if (Active != 0)
                return;
            
            while (RemovedListeners.TryDequeue(out var listener))
            {
                RemoveListener(listener);
            }
                
            while (NewListeners.TryDequeue(out var listener))
            {
                RegistryListener(listener);
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var listener in _listeners)
            {
                var lastHandler = listener;
                if (_handlers.Count > 0)
                {
                    foreach (var handler in _handlers.OrderByDescending(h => h.GetPriority()))
                    {
                        handler.Listener = lastHandler;
                        lastHandler = handler as T;
                    }
                }
                yield return lastHandler;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public RegistryResult RegistryListener(T listener)
        {
            if (_listeners.Contains(listener)) 
                return RegistryResult.TryAddExist;
            
            if (Active >= 1)
            {
                if (NewListeners.Contains(listener)) 
                    return RegistryResult.CurrentlyWaitedAdd;
                NewListeners.Enqueue(listener);
                return RegistryResult.WaitToAdd;
            }
            _listeners.Add(listener);
            InvokeCatchers(listener);
            return RegistryResult.Added;
        }

        public void RemoveListener(T listener)
        {
            if (Active >= 1)
            {
                if (RemovedListeners.Contains(listener))
                    return;
                RemovedListeners.Enqueue(listener);
                return;
            }
            InvokeRemoveCatchers(listener);
            _listeners.Remove(listener);
        }

        public UnsubscribeToken RegistryCatcher(Action<T> onCatchListener)
            => InternalRegistryCatcher(onCatchListener, _catchers);

        public UnsubscribeToken RegistryUnsubscribeCatcher(Action<T> onRemoveListener)
            => InternalRegistryCatcher(onRemoveListener, _unsubscribeCatchers);

        public bool RegisterHandler(EventHandler<T> handler)
        {
            if (_handlers.Contains(handler))
                return false;
            _handlers.Add(handler);
            return true;
        }

        private UnsubscribeToken InternalRegistryCatcher(Action<T> onCatchListener, List<Action<T>> catchers)
        {
            if (catchers.Contains(onCatchListener))
                return null;
            
            var token = new UnsubscribeToken(() =>
            {
                catchers.Remove(onCatchListener);
            });
            catchers.Add(onCatchListener);
            return token;
        }

        private void InvokeCatchers(T listener)
        {
            foreach (var catcher in _catchers)
            {
                catcher?.Invoke(listener);
            }
        }
        
        private void InvokeRemoveCatchers(T listener)
        {
            foreach (var catcher in _unsubscribeCatchers)
            {
                catcher?.Invoke(listener);
            }
        }
    }
    
    public enum RegistryResult
    {
        TryAddExist, CurrentlyWaitedAdd, WaitToAdd, Added
    }
}