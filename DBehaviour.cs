using UnityEngine;

namespace DSystem
{
    public abstract class DBehaviour : MonoBehaviour
    {
        public static void DInstantiate(DBehaviour original)
        {
            DBehaviour obj = Instantiate(original);
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.DisableInitialize();
            }
        }

        public static void DInstantiate(DBehaviour original, Transform parent)
        {
            DBehaviour obj = Instantiate(original, parent);
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.DisableInitialize();
            }
        }
        
        public static void DInstantiate(DBehaviour original, Vector3 position, Quaternion rotation)
        {
            DBehaviour obj = Instantiate(original, position, rotation);
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.DisableInitialize();
            }
        }
        
        public static void DInstantiate(DBehaviour original, Vector3 position, Quaternion rotation, Transform parent)
        {
            DBehaviour obj = Instantiate(original, position, rotation, parent);
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.DisableInitialize();
            }
        }
        
        private bool _initialized;
        
        private void Awake()
        {
            if (_initialized) return;

            _initialized = true;
            
            MainInjector.Instance.RegistryInjection(this);

            Initialize();
        }
        
        internal void DisableInitialize()
        {
            Awake();
        }
        
        protected virtual void Initialize() {}
    }
}