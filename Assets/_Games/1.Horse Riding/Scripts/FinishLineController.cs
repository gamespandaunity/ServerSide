using UnityEngine;
using System.Collections;

public class FinishLineController : MonoBehaviour {

	public static FinishLineController instance;
	public bool isValidFinishLineCrossing;
	public int currentRountLaps=0;
	void Awake(){
		instance = this;
	}
	public void IncreamentRoundLaps(){
		currentRountLaps++;
		if(currentRountLaps >  LevelsManager.instance.currentLevelLapCount){
			currentRountLaps = LevelsManager.instance.currentLevelLapCount;
		}
		//HudMenuManager.instance.lapText.text = currentRountLaps + "/" + LevelsManager.instance.currentLevelsNumbersOfLaps;
		//HudMenuManager.instance.lapText1.text = currentRountLaps + "/" + LevelsManager.instance.currentLevelsNumbersOfLaps;
        HudMenuManager.instance.updateLapProgress(currentRountLaps, LevelsManager.instance.currentLevelLapCount);

        HudMenuManager.instance.notifyLapCompletion ();
	}

	}
