using System.Collections.Generic;
using UnityEngine;

namespace SpaceCleaner.Core
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 20;
        [SerializeField] private Transform poolParent;

        private readonly Queue<GameObject> pool = new();

        private void Awake()
        {
            if (poolParent == null)
                poolParent = transform;

            for (int i = 0; i < initialSize; i++)
                AddToPool(CreateInstance());
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateInstance();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            AddToPool(obj);
        }

        private GameObject CreateInstance()
        {
            GameObject obj = Instantiate(prefab, poolParent);
            obj.SetActive(false);
            return obj;
        }

        private void AddToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            pool.Enqueue(obj);
        }
    }
}
