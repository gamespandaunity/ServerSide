using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
namespace Twelve
{
    public class PoolTwelve : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("poolPrefab")] public GameObject pooledObjectPrefab;
        [SerializeField, FormerlySerializedAs("initialPoolCount")] public int startingPoolSize;

        private List<GameObject> objectPool = new List<GameObject>();

        private void Start()
        {
            CreatePoolObjects();
        }
        //private void InstantiatePool()
        private void CreatePoolObjects()
        {
            for (int i = 0; i < startingPoolSize; i++)
            {
                GameObject obj = Instantiate(pooledObjectPrefab, transform);
                objectPool.Add(obj);
                obj.SetActive(false);
            }
        }
        //  public GameObject Retrieve()
        public GameObject GetPooledObject()
        {
            foreach (GameObject obj in objectPool)
            {
                if (!obj.activeInHierarchy)
                {
                    return obj;
                }
            }
            return Instantiate(pooledObjectPrefab, transform);

        }
        //   public void Restore(GameObject obj)
        public void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
        }


    }
}