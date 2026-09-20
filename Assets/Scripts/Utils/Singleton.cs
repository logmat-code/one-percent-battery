using UnityEngine;

namespace OnePercentBattery.Utils
{
    /// <summary>
    /// Base class for a MonoBehaviour that should exist exactly once per scene load.
    /// Not made to survive scene loads on purpose: every mode/run is loaded fresh into the single gameplay scene.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this as T)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this as T)
            {
                _instance = null;
            }
        }
    }
}
