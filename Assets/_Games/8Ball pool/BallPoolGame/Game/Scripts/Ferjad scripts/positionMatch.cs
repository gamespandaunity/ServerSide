using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class positionMatch : MonoBehaviour
{
    public Transform curball;

    float yPos ,radius;

    // Start is called before the first frame update
    void Start()
    { 
        yPos= transform.position.y - curball.position.y;
        radius = GetComponent<SphereCollider>().radius;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(curball.position.x , curball.position.y  + yPos, curball.position.z) ;  
    }
    private void OnTriggerEnter(Collider other)
    {
        //print("on enter trigger");
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            Vector3 direction = (curball.position- other.transform.position).normalized;
            curball.position = other.transform.position + direction * (2 * radius);
            
          //print(gameObject.name + "has something entered" + other.gameObject.name);
        }
    }
}
