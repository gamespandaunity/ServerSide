using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool : MonoBehaviour
{
 
    [SerializeField] private GameObject poolPrefab;
    [SerializeField] private int initialPoolCount;
    private List<GameObject> poolList = new List<GameObject>();

    private void Start()
    {
        InstantiatePool();
    }
    private void InstantiatePool()
    {
        for (int i = 0; i < initialPoolCount; i++)
        {
            GameObject obj = Instantiate(poolPrefab, transform);
            poolList.Add(obj);
            obj.SetActive(false);
        }
    }
    public GameObject Retrieve()
    {
        foreach (GameObject obj in poolList)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }
        return Instantiate(poolPrefab, transform);

    }
    public void Restore(GameObject obj)
    {
        obj.SetActive(false);
    }


}