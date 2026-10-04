using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ExplosionController : MonoBehaviour {
	public List<GameObject> partsToDeactive;
	public List<GameObject> partsToExplode;

	public GameObject particle;

	void OnEnable(){
		//Explode ();
	}
	// Use this for initialization
	public void Explode () {
		particle.transform.parent = null;
		particle.SetActive (true);
	
		for (int i = 0; i < partsToExplode.Count; i++) {
			partsToExplode [i].transform.parent = null;
			Rigidbody rb = partsToExplode [i].GetComponent<Rigidbody> ();
			rb.AddExplosionForce(15*rb.mass, transform.position, 20,20, ForceMode.Impulse);

		}

		for (int i = 0; i < partsToDeactive.Count; i++) {
			partsToDeactive [i].SetActive (false);

		}
		Invoke("DeactiveteObjects",10);

	}

    public void ExplodeButNotParent()
    {
        particle.transform.parent = null;
        particle.SetActive(true);

        for (int i = 0; i < partsToExplode.Count; i++)
        {
            partsToExplode[i].transform.parent = null;
            Rigidbody rb = partsToExplode[i].GetComponent<Rigidbody>();
            rb.AddExplosionForce(25 * rb.mass, transform.position, 20, 40, ForceMode.Impulse);

        }

        
        Invoke("DeactiveteObjects", 5);

    }

    void DeactiveteObjects()
	{
		particle.SetActive(false);
		Destroy(particle, 0.1f);

		for (int i = 0; i < partsToExplode.Count; i++)
		{
			partsToExplode[i].SetActive(false);

			Destroy(partsToExplode[i] , 0.5f*i);


		}
	}

}
