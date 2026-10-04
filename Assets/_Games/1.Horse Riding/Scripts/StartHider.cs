using UnityEngine;
using System.Collections;

public class StartHider : MonoBehaviour {
	
	void OnTriggerEnter(Collider other){
		if(other.gameObject.CompareTag("PlayerCollider")){
			gameObject.SetActive (false);
		}
	}
}
