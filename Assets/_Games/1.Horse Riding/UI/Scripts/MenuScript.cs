using UnityEngine;
using System.Collections;

public class MenuScript : MonoBehaviour {

	public GameObject StartMenu;
	public GameObject GarageMenu;
	public GameObject SettingsMenu;
	public GameObject CarUnlockMenu;
	public GameObject GameModesMenu;



	void Start () {
		StartMenu.SetActive (true);
		GarageMenu.SetActive (false);
	}



	public void StartMenuDrive(){
		StartMenu.SetActive (false);
		GarageMenu.SetActive (true);
		//GarageCars[currentCar-1].SetActive (true);
		//GarageCars[currentCar-1].GetComponent<Animator>().enabled = true;
	}

	public void BackToStartMenu(){
		StartMenu.SetActive (true);
		GarageMenu.SetActive (false);
		//GarageCars[currentCar-1].transform.position = new Vector3(-24.36f, 1.51f, 13.45f);
		//GarageCars[currentCar-1].SetActive(false);
	}

	public void openSettings(){
		SettingsMenu.SetActive (true);
	}

	public void closeSettings(){
		SettingsMenu.SetActive (false);
	}

	public void openCarUnlockPopup(){
		CarUnlockMenu.SetActive (true);
	}
	
	public void closeCarUnlockPopup(){
		CarUnlockMenu.SetActive (false);
	}

	public void openGameModesMenu(){
		GameModesMenu.SetActive (true);
	}

	public void closeGameModesMenu(){
		GameModesMenu.SetActive (false);
	}


}
