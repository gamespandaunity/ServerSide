using UnityEngine;
using System.Collections;

public class SelfActiveController : MonoBehaviour {
	public ParticleSystem []particles;
	// Use this for initialization
	void Start () {
		Invoke ("deactiveParticle", 15);
	}

	void deactiveParticle(){
		for (int i = 0; i < particles.Length; i++) {
			particles [i].enableEmission = false;
		}
	}

}
