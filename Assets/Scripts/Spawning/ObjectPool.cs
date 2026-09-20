using System.Collections.Generic;
using UnityEngine;

namespace OnePercentBattery.Spawning
{
    /// <summary>Minimal generic pool - avoids Instantiate/Destroy churn for obstacles and pickups spawned every few seconds.</summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly T _prefab;
        private readonly Transform _parent;

        public ObjectPool(T prefab, Transform parent, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                _inactive.Push(CreateInstance());
            }
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        public T Get()
        {
            T instance = _inactive.Count > 0 ? _inactive.Pop() : CreateInstance();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            instance.gameObject.SetActive(false);
            _inactive.Push(instance);
        }
    }
}
