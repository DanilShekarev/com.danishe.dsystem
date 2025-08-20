using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace DSystem.InternalSystems
{
    [AutoRegistry, UsedImplicitly]
    public class DisableCatchersController : IInitializable
    {
        private readonly Dictionary<GameObject, DisableCatcher> _disableCatchers = new ();
        private readonly HashSet<DisableCatcher> _pool = new();

        private Transform _poolTr;
        
        void IInitializable.Initialize()
        {
            _poolTr = new GameObject("DisableCatchersPool").transform;
            Object.DontDestroyOnLoad(_poolTr.gameObject);
        }

        internal DisableCatcher RegistryForceOnDestroy(DBehaviour dBehaviour)
        {
            if (!_disableCatchers.TryGetValue(dBehaviour.gameObject, out var disableCatcher))
            {
                disableCatcher = _pool.FirstOrDefault();
                _pool.Remove(disableCatcher);
                
                var obj = new GameObject("DisableCatcher", typeof(DisableCatcher));
                disableCatcher = obj.GetComponent<DisableCatcher>();
                disableCatcher.Initialize(this);
                _disableCatchers.Add(dBehaviour.gameObject, disableCatcher);
                disableCatcher.transform.SetParent(dBehaviour.transform);
            }

            disableCatcher.AddOnDispose(dBehaviour);
            return disableCatcher;
        }

        internal void AddToPool(DisableCatcher disableCatcher)
        {
            disableCatcher.transform.SetParent(_poolTr);
            _pool.Add(disableCatcher);
        }

        internal void RemoveCatcher(DisableCatcher disableCatcher)
        {
            _pool.Remove(disableCatcher);
        }
    }
}