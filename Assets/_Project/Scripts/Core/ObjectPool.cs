using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Core
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int initialSize = 10;
        }

        [SerializeField] private List<Pool> pools;

        private Dictionary<string, Queue<GameObject>> poolDictionary;
        private Dictionary<string, GameObject> prefabDictionary;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePools();
        }

        private void InitializePools()
        {
            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            prefabDictionary = new Dictionary<string, GameObject>();

            foreach (var pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();
                prefabDictionary[pool.tag] = pool.prefab;

                for (int i = 0; i < pool.initialSize; i++)
                {
                    GameObject obj = CreateNewObject(pool.prefab, pool.tag);
                    objectPool.Enqueue(obj);
                }

                poolDictionary[pool.tag] = objectPool;
            }
        }

        private GameObject CreateNewObject(GameObject prefab, string tag)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = $"{tag}_Pooled";
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            return obj;
        }

        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            GameObject obj;

            if (poolDictionary[tag].Count > 0)
            {
                obj = poolDictionary[tag].Dequeue();
            }
            else
            {
                // Create new object if pool is empty
                obj = CreateNewObject(prefabDictionary[tag], tag);
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            // Reset poolable component if it has one
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnSpawn();

            return obj;
        }

        public void Despawn(string tag, GameObject obj)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist. Destroying object.");
                Destroy(obj);
                return;
            }

            // Call despawn callback
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnDespawn();

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            poolDictionary[tag].Enqueue(obj);
        }

        public void DespawnAll(string tag)
        {
            // Find all active pooled objects with this tag and despawn them
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf && child.name.StartsWith(tag))
                {
                    Despawn(tag, child.gameObject);
                }
            }
        }
    }

    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}
