using UnityEngine;
using System.Collections;

public class SelfDeactivator : MonoBehaviour {

	public float timeToDecative;
	// Use this for initialization
	void OnEnable () {
		Invoke ("DeactiveSelf",timeToDecative);
	}
	
	// Update is called once per frame
	void DeactiveSelf () {
		gameObject.SetActive (false);
	}
}
