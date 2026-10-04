using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SmokeIntanition : MonoBehaviour {
	public WheelCollider[] allWheelColliders;
	public AudioSource audioSkidMarks;
	public GameObject wheelSmoke;
	private List <ParticleSystem> wheelParticles = new List<ParticleSystem>();
	void Awake(){
		SmokeInit ();
	}
	void OnEnable(){
		SmokeInstantiateRate ();
		audioSkidMarks.PlayDelayed (0.2f);
		audioSkidMarks.volume =1;
		Invoke ("DisableSmoke",2f);
		//Invoke ("disaleSkidSound",1);

	}
	void FixedUpdate(){
		audioSkidMarks.volume -= Time.deltaTime;
	}


	void disaleSkidSound(){
		audioSkidMarks.Stop ();

	}

	void DisableSmoke(){

		for(int i = 0; i < wheelParticles.Count; i++){

		

			wheelParticles[i].enableEmission = false;


		}
	}

	public void SmokeInit (){

		for(int i = 0; i < allWheelColliders.Length; i++){
			GameObject ps = (GameObject)Instantiate(wheelSmoke, transform.position, transform.rotation) as GameObject;
			wheelParticles.Add(ps.GetComponent<ParticleSystem>());
			ps.GetComponent<ParticleSystem>().enableEmission = false;
			ps.transform.SetParent(allWheelColliders[i].transform);
			ps.transform.localPosition = Vector3.zero;
		}

	}

	void SmokeInstantiateRate () {

		for(int i = 0; i < allWheelColliders.Length; i++){

			WheelHit CorrespondingGroundHit;
			allWheelColliders[i].GetGroundHit(out CorrespondingGroundHit);

					wheelParticles[i].enableEmission = true;
			

		}

	}
}
