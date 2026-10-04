using MalbersAnimations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NosController : MonoBehaviour {
    public Animal animalController;
    public GameObject nosParticles;

    // Use this for initialization


    // Update is called once per frame
    void FixedUpdate () {
        if (animalController.Shift)
        {
            nosParticles.SetActive(true);
        }
        else
        {
            nosParticles.SetActive(false);
        }
	}
}
