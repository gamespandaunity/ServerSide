//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using CarRace;
using MalbersAnimations;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// A simple manager script for all demo scenes. It has an array of spawnable player vehicles, public methods, setting new behavior modes, restart, and quit application.
/// </summary>
public class HorseDemo : MonoBehaviour {

	[Header("Spawnable Vehicles")]
    [FormerlySerializedAs("selectableVehicles")] public RCC_CarControllerV3[] availableVehicles;
    [FormerlySerializedAs("lastPosition")] public GameObject previousPosition;
    [FormerlySerializedAs("PlayerFollower")] public FollowTarget playerCameraFollower;
    [FormerlySerializedAs("PauseMenu")] public GameObject pauseMenuUI;



    internal int selectedVehicleIndex  = 0;		// An integer index value used for spawning a new vehicle.
	internal int selectedBehaviorIndex  = 0;		// An integer index value used for setting behavior mode.
   // public void SelectVehicle (int index) {
    public void ChooseVehicle (int index) {

		selectedVehicleIndex  = index;
	
	}

    void Start()
    {

        if (PlayerDataController.instance != null)
        {
            selectedVehicleIndex  = PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1;

        }
        else
        {
            //selectedCarIndex =2 ;

        }
      //  RCC_Settings.Instance.behaviorType = RCC_Settings.BehaviorType.Fun;
        if (MConstants.CurrentGameMode != MConstants.GAME_MODES.MULTI_PLAYER)
        {
            //Spawn();
        }
        
    }

  //  public void Spawn () {
    public void SpawnVehicle () {

        // Last known position and rotation of last active vehicle.
        Vector3 lastKnownPos = previousPosition.transform.position;//new Vector3();
        Quaternion lastKnownRot = previousPosition.transform.rotation;

        // Checking if there is a player vehicle on the scene.
        if (RCC_SceneManager.Instance.activePlayerVehicle){

			lastKnownPos = RCC_SceneManager.Instance.activePlayerVehicle.transform.position;
			lastKnownRot = RCC_SceneManager.Instance.activePlayerVehicle.transform.rotation;

		}

		// If last known position and rotation is not assigned, camera's position and rotation will be used.
		if(lastKnownPos == Vector3.zero){
			
			if(RCC_SceneManager.Instance.activePlayerCamera){
				
				lastKnownPos = RCC_SceneManager.Instance.activePlayerCamera.transform.position;
				lastKnownRot = RCC_SceneManager.Instance.activePlayerCamera.transform.rotation;

			}

		}

		// We don't need X and Z rotation angle. Just Y.
		lastKnownRot.x = 0f;
		lastKnownRot.z = 0f;

		RCC_CarControllerV3 lastVehicle = RCC_SceneManager.Instance.activePlayerVehicle;

		#if BCG_ENTEREXIT

		BCG_EnterExitVehicle lastEnterExitVehicle;
		bool enterExitVehicleFound = false;

		if (lastVehicle) {

			lastEnterExitVehicle = lastVehicle.GetComponentInChildren<BCG_EnterExitVehicle> ();

			if(lastEnterExitVehicle && lastEnterExitVehicle.driver){

				enterExitVehicleFound = true;
				BCG_EnterExitManager.Instance.waitTime = 10f;
				lastEnterExitVehicle.driver.GetOut();

			}

		}

		#endif

		// If we have controllable vehicle by player on scene, destroy it.
		if(lastVehicle)
			Destroy(lastVehicle.gameObject);

        // Here we are creating our new vehicle.
        RCC_CarControllerV3 carControllerV3 = RCC.SpawnRCC(availableVehicles[selectedVehicleIndex ], lastKnownPos, lastKnownRot, true, true, true);

        if (playerCameraFollower != null)
        {
            playerCameraFollower.target = carControllerV3.transform;
            playerCameraFollower.enabled = true;
        }
#if BCG_ENTEREXIT

		if(enterExitVehicleFound){

			lastEnterExitVehicle = null;

			lastEnterExitVehicle = RCC_SceneManager.Instance.activePlayerVehicle.GetComponentInChildren<BCG_EnterExitVehicle> ();

			if(!lastEnterExitVehicle){
				
				lastEnterExitVehicle = RCC_SceneManager.Instance.activePlayerVehicle.gameObject.AddComponent<BCG_EnterExitVehicle> ();

			}

			if(BCG_EnterExitManager.Instance.BCGCharacterPlayer.characterPlayer && lastEnterExitVehicle && lastEnterExitVehicle.driver == null){
				
				BCG_EnterExitManager.Instance.waitTime = 10f;
				BCG_EnterExitManager.Instance.BCGCharacterPlayer.characterPlayer.GetIn(lastEnterExitVehicle);

			}

		}
		
#endif

    }

//	public void SelectBehavior(int index){
	public void ChooseVehicleBehavior(int index){

		selectedBehaviorIndex  = index;

	}

	

  //  public void ResetCar()
    public void ResetVehicle()
    {
       // RCC_CarControllerV3.isRestting = true;
        Invoke("isReset", 0.1f);
        Transform pTransform = null;
        if (GameObject.FindObjectOfType<RCC_Camera>())
        {
            //pTransform = GameObject.FindObjectOfType<RCC_Camera>().GetPlayerCarTranform();
        }
        if (CheckPointController.instance.lastCheckpointReached == null)
        {
            return;
        }

        pTransform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        pTransform.rotation = CheckPointController.instance.lastCheckpointReached.transform.rotation; //Quaternion.identity;
        pTransform.position = new Vector3(
            CheckPointController.instance.lastCheckpointReached.transform.position.x,
            CheckPointController.instance.lastCheckpointReached.transform.position.y + 1,
            CheckPointController.instance.lastCheckpointReached.transform.position.z);




    }

   
   // public void PauseGame()
    public void TogglePauseGame()
    {
        pauseMenuUI.SetActive(true);
    }

  //  public void RestartScene(){
    public void RestartLevel(){

		SceneManager.LoadScene (SceneManager.GetActiveScene().buildIndex);

	}

	//public void Quit(){
	public void ExitGame(){

		Application.Quit();

	}

}
