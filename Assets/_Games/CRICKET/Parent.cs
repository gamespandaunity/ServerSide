using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeChildPos()
    {
        for(int i = 0; i < gameObject.transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<DestroyPlayer>().changePOS();
        }
    }
}
