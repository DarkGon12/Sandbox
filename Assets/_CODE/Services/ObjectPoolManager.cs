using System.Collections.Generic;
using UnityEngine;

namespace Services.ObjectPool
{
    public class ObjectPoolManager : MonoBehaviour
    {
        private class Pool
        {
            public Queue<GameObject> Objects = new();
            public GameObject Prefab;
            public Transform Parent;
        }

        private static Dictionary<string, Pool> _pools = new();

        public static void CreatePool(string tag, GameObject prefab, int size)
        {
            if (_pools.ContainsKey(tag))
                return;

            GameObject root = new GameObject($"Pool_{tag}");
            Pool pool = new Pool
            {
                Prefab = prefab,
                Parent = root.transform
            };

            for (int i = 0; i < size; i++)
            {
                GameObject obj = GameObject.Instantiate(prefab, pool.Parent);
                obj.SetActive(false);
                pool.Objects.Enqueue(obj);
            }

            _pools.Add(tag, pool);
        }

        public static GameObject Get(string tag)
        {
            if (!_pools.ContainsKey(tag))
            {
                Debug.LogError($"Pool with tag {tag} not found!");
                return null;
            }

            Pool pool = _pools[tag];

            if (pool.Objects.Count == 0)
            {
                GameObject newObj = GameObject.Instantiate(pool.Prefab, pool.Parent);
                newObj.SetActive(false);
                pool.Objects.Enqueue(newObj);
            }

            GameObject obj = pool.Objects.Dequeue();

            obj.transform.SetParent(null);
            obj.SetActive(true);

            return obj;
        }

        public static void Return(string tag, GameObject obj)
        {
            if (!_pools.ContainsKey(tag))
            {
                GameObject.Destroy(obj);
                return;
            }

            obj.transform.SetParent(_pools[tag].Parent); 
            obj.SetActive(false);
            _pools[tag].Objects.Enqueue(obj);
        }

    }
}
