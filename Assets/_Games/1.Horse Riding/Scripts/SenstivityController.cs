using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Serialization;
public class SenstivityController : MonoBehaviour {
	
	//public SmoothMouseLook smoothMouseLook;
	float sensitivityValue =1f;
	bool hasCompletedTutorial;
    [FormerlySerializedAs("slr")] public Slider volumeSlider ;
    [FormerlySerializedAs("pauseMenuManager")] public PauseMenuManager pauseMenuController;

    // Use this for initialization
    void OnEnable () {
		if(PlayerDataController.instance != null){
			sensitivityValue = PlayerDataController.instance.playerStats.SensivityValue;
			if(sensitivityValue < 0.3f){
				sensitivityValue = 0.3f;
			}
		}

		volumeSlider .value = sensitivityValue;
		HudMenuManager.inputSensitivity = sensitivityValue;
		//smoothMouseLook.sensitivity = Value;
	}

	//void HideFireTutorial(){
	void hideFireControlTutorial(){
		//TutorialManager.instance.HideTutorial ();
		pauseMenuController.Resume ();
	}
	// Update is called once per frame
	//public void OnValueChanged (float Value) {
	public void handleSliderValueChanged (float Value) {
		//Debug.Log ("Value "+Value);
		if(PlayerDataController.instance != null){
			PlayerDataController.instance.playerStats.SensivityValue =Value ;
		}
		HudMenuManager.inputSensitivity = Value;
		//if( TutorialManager.instance && TutorialManager.instance.currentTutorialState==TutorialManager.TUTORIAL_STATE.SENSTIVITY_CONTROLLER_2 && !isTutorialDone){
		//	isTutorialDone = true;
		//	Invoke ("HideFireTutorial",0.2f);
		//}
		//smoothMouseLook.sensitivity = Value;
	}
}
