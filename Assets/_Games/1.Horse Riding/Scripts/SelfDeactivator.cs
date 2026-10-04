using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HorseRiding
{

    public class SelfDeactivator : MonoBehaviour {
    public float timeToDeactive;
	// Use this for initialization
	void OnEnable () {
        Invoke("Deactivate",timeToDeactive);
	}
	
	// Update is called once per frame
	void Deactivate () {
        gameObject.SetActive(false);
	}
}
}
