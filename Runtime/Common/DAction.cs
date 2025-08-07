using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace DSystem
{
    public sealed class DAction<T> : IDAction, IEnumerable<T> where T : class
    {
        private readonly List<Action<T>> _catchers = new ();
        private readonly List<Action<T>> _unsubscribeCatchers = new ();
        
        private List<EventHandler<T>> _handlers;
        private EventHandler<T> _firstHandler, _lastHandler;
        
        private readonly List<T> _listeners = new ();
        
        private readonly Queue<T> _newListeners = new ();
        private readonly Queue<T> _removedListeners = new ();
        private int _active;
        
        void IDAction.Invoke(Action<object> action) => Invoke(action);
        IEnumerator<object> IDAction.GetEnumerator() => GetEnumerator();
        RegistryResult IDAction.RegistryListener(object listener) => RegistryListener(listener as T);
        void IDAction.RemoveListener(object listener) => RemoveListener(listener as T);
        UnsubscribeToken IDAction.RegistryCatcher(Action<object> onCatchListener) 
            => RegistryCatcher(onCatchListener);
        UnsubscribeToken IDAction.RegistryUnsubscribeCatcher(Action<object> onRemoveListener)
            => RegistryUnsubscribeCatcher(onRemoveListener);
        public bool RegisterHandler(EventHandler<object> handler) => RegisterHandler(handler as EventHandler<T>);
        public bool RemoveHandler(EventHandler<object> handler) => RemoveHandler(handler as EventHandler<T>);

        public void Invoke(Action<T> action)
        {
            _active++;
            foreach (var listener in _listeners)
            {
                var firstHandler = listener;
                if (_lastHandler != null)
                {
                    _lastHandler.Listener = firstHandler;
                    firstHandler = _firstHandler as T;
                }

                try
                {
                    action.Invoke(firstHandler);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _active--;
            
            if (_active != 0)
                return;
            
            while (_removedListeners.TryDequeue(out var listener))
            {
                RemoveListener(listener);
            }
                
            while (_newListeners.TryDequeue(out var listener))
            {
                RegistryListener(listener);
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var listener in _listeners)
            {
                var firstHandler = listener;
                if (_lastHandler != null)
                {
                    _lastHandler.Listener = firstHandler;
                    firstHandler = _firstHandler as T;
                }
                
                yield return firstHandler;
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
            
            if (_active >= 1)
            {
                if (_newListeners.Contains(listener)) 
                    return RegistryResult.CurrentlyWaitedAdd;
                _newListeners.Enqueue(listener);
                return RegistryResult.WaitToAdd;
            }
            
            _listeners.Add(listener);
            OrderingListeners();
            InvokeCatchers(listener);
            return RegistryResult.Added;
        }

        private void OrderingListeners()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            // _listeners.OrderBy(listener =>
            // {
            //     var attr = AssemblyDataCacher.GetEventListenerAttribute(listener.GetType());
            //     int order = short.MaxValue;
            //     order |= (int)attr.ListenerFlags << 16;
            //     return order;
            // });
            _listeners.Sort((a, b) => GetOrder(a).CompareTo(GetOrder(b)));
            stopwatch.Stop();
            Debug.Log($"Ordering listeners: {stopwatch.ElapsedMilliseconds}ms");
        }

        private static int GetOrder(object t)
        {
            var attr = AssemblyDataCacher.GetEventListenerAttribute(t.GetType());
            int order = short.MaxValue;
            order |= (int)attr.ListenerFlags << 16;
            return order;
        }

        public void RemoveListener(T listener)
        {
            if (_active >= 1)
            {
                if (_removedListeners.Contains(listener))
                    return;
                _removedListeners.Enqueue(listener);
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
            _handlers ??= new();
            if (_handlers.Contains(handler))
                return false;
            _handlers.Add(handler);
            UpdateHandlers();
            return true;
        }
        
        public bool RemoveHandler(EventHandler<T> handler)
        {
            if (_handlers == null)
                return false;
            
            if (!_handlers.Remove(handler))
                return false;
            UpdateHandlers();
            return true;
        }

        private void UpdateHandlers()
        {
            if (_handlers.Count == 0)
            {
                _firstHandler = null;
                _lastHandler = null;
                _handlers = null;
                return;
            }
            int i = 0;
            T lastHandler = null;
            foreach (var handler in _handlers.OrderByDescending(h => h.GetPriority()))
            {
                if (i == 0)
                    _lastHandler = handler;
                else
                    handler.Listener = lastHandler;
                
                lastHandler = handler as T;
                i++;
            }
            _firstHandler = lastHandler as EventHandler<T>;
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