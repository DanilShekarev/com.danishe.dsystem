using System;
using UnityEngine;

namespace DSystem
{
    internal sealed class MonoEventHandler : MonoBehaviour
    {
        internal event Action EnableEvent;
        internal event Action DisableEvent;
        internal event Action DestroyEvent;

        private void OnEnable()
        {
            EnableEvent?.Invoke();
        }

        private void OnDisable()
        {
            DisableEvent?.Invoke();
        }

        private void OnDestroy()
        {
            DestroyEvent?.Invoke();
        }
    }
}