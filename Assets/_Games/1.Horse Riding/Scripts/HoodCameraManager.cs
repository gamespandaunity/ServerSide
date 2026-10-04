using System.Collections;
using System.Collections.Generic;
using CarRace;
using UnityEngine;

public class HoodCameraManager : MonoBehaviour {
	public MeshRenderer[] VehicleMain;
	public GameObject []hoodCameraEntities;
     RCC_CharacterController characterController;
	// Use this for initialization
	void Start () {
        characterController = gameObject.GetComponent<RCC_CharacterController>();

        HideHoodCamera ();
	
	}

	public void showHoodCamera(){
        //characterController.enabledAnimator = true;

        for (int i = 0; i < VehicleMain.Length; i++) {
			VehicleMain [i].enabled = false;
		}
		for (int i = 0; i < hoodCameraEntities.Length; i++) {
			hoodCameraEntities [i].SetActive(true);
		}
	}
	
	public void HideHoodCamera(){
        //characterController.enabledAnimator = false;

        for (int i = 0; i < VehicleMain.Length; i++) {
			VehicleMain [i].enabled = true;
		}
		for (int i = 0; i < hoodCameraEntities.Length; i++) {
			hoodCameraEntities [i].SetActive(false);
		}
	}
}
