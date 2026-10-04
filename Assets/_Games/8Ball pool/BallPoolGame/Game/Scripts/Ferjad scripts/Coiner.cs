using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coiner : MonoBehaviour
{
    public float sp;
    public Transform A;
    public Transform B;
   // public Transform ab;

    //public Transform c;
    //public Transform bc;


    public Transform ab_bc;
    public int noCoin;
    public float waitforsecond;
    List<GameObject> newGb = new List<GameObject>();
    private void Start()
    {
        for(int i = 0; i < noCoin; i++)
        {
            newGb.Add( Instantiate(ab_bc.gameObject, transform));
         

        }
        
    }

    private void Update()
    { 
        sp = (sp + Time.deltaTime) % 1;
     A.position = Vector3.Slerp(A.position, B.position, sp );

        //bc.position = Vector3.Lerp(B.position, c.position, sp);


      //  ab_bc.position = Vector3.Lerp(ab.position, bc.position, sp);
        StartCoroutine(Mover());

    }
    IEnumerator Mover()
    {
        
        for (int i = 0; i < noCoin; i++)
        {
            float range = Random.Range(newGb[i].transform.position.y - 10f, newGb[i].transform.position.y + 10f);
            newGb[i].transform.position = new Vector3(newGb[i].transform.position.x, range, newGb[i].transform.position.z);
            //newGb[i].transform.position = Vector3.Lerp(/*ab.position*/newGb[i].transform.position, bc.position, sp);
            yield return new WaitForSeconds(waitforsecond);
        }
    }
}
