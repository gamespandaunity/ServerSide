using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class Level : MonoBehaviour {
    [FormerlySerializedAs("smartCarList")] public SAICSmartAICar[] smartAICars;
    [FormerlySerializedAs("noLaps")] public int totalLaps;
    [FormerlySerializedAs("LastKnownPosition")] public GameObject lastKnownPlayerPosition;
    [FormerlySerializedAs("CamerasList")] public GameObject[] cameraViews ;
    [FormerlySerializedAs("timesList")] public List<float> lapTimes ;
    int currentIndex =0;

	void OnEnable(){
		if(PlayerDataController.instance != null && PlayerDataController.instance.playerStats.CurrentMode == 1 || MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER){
			for (int i = 0; i < smartAICars.Length; i++) {
				smartAICars [i].gameObject.SetActive(false);
			}
		}
	}
	//public void ActiveCamera(int index){
	public void ActivateCamera(int index){
//		Debug.Log ("Index "+ index);
		for (int i = 0; i < cameraViews .Length; i++) {
			cameraViews  [i].SetActive (false);
		}
		cameraViews  [index-1].SetActive (true);
	}

	//public void DeActiveCameras(){
	public void DeactivateAllCameras(){
		for (int i = 0; i < cameraViews .Length; i++) {
			cameraViews  [i].SetActive (false);
		}

		for (int i = 0; i < smartAICars.Length; i++) {
			Invoke ("activeCars",i*1f);
			//smartCarList [i].enabled = true;
		}
	}

	//void activeCars(){
	void ActivateAICars(){
		smartAICars [currentIndex].enabled = true;
		currentIndex++;
	}

	//public void setTimesList(){
	public void SetLapTimes(){
		lapTimes  = new List<float> ();
		for (int i = 0; i < smartAICars.Length; i++) {
			lapTimes .Add (smartAICars[i].endTime-smartAICars[i].startTime);
			//sCar.endTime-sCar.startTime)
		}

		lapTimes .Sort ();
	}
}
