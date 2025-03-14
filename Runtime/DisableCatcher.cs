using System.Collections.Generic;
using DSystem.InternalSystems;
using UnityEngine;

namespace DSystem
{
    public class DisableCatcher : MonoBehaviour
    {
        private List<DBehaviour> _dBehaviours;
        private DisableCatchersController _disableCatchersController;
        
        internal void Initialize(DisableCatchersController disableCatchersController)
        {
            _disableCatchersController = disableCatchersController;
            _dBehaviours = new List<DBehaviour>();
        }

        internal void AddOnDispose(DBehaviour dBehaviour)
        {
            _dBehaviours.Add(dBehaviour);
        }

        internal void RemoveOnDispose(DBehaviour dBehaviour)
        {
            _dBehaviours.Remove(dBehaviour);
            if (_dBehaviours.Count == 0)
            {
                _disableCatchersController.AddToPool(this);
            }
        }

        private void OnDestroy()
        {
            foreach (var dBehaviour in _dBehaviours)
            {
                dBehaviour.InternalOnDispose();
            }
            _disableCatchersController.RemoveCatcher(this);
        }
    }
}