using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace DSystem.InternalSystems
{
    [AutoRegistry, UsedImplicitly]
    public class DisableCatchersController
    {
        private readonly Dictionary<GameObject, DisableCatcher> _disableCatchers = new ();
        private readonly Stack<DisableCatcher> _pool = new();

        internal DisableCatcher RegistryForceOnDestroy(DBehaviour dBehaviour)
        {
            if (_disableCatchers.TryGetValue(dBehaviour.gameObject, out var disableCatcher))
            {
                
            }
            else if (_pool.TryPop(out disableCatcher))
            {
                disableCatcher.AddOnDispose(dBehaviour);
                disableCatcher.transform.SetParent(dBehaviour.transform);
            }
            else
            {
                var obj = new GameObject("DisableCatcher", typeof(DisableCatcher));
                obj.transform.SetParent(dBehaviour.transform);
                disableCatcher = obj.GetComponent<DisableCatcher>();
                disableCatcher.Initialize(this);
                _disableCatchers.Add(dBehaviour.gameObject, disableCatcher);
            }
            disableCatcher.AddOnDispose(dBehaviour);
            return disableCatcher;
        }

        internal void AddToPool(DisableCatcher disableCatcher)
        {
            _pool.Push(disableCatcher);
        }
    }
}