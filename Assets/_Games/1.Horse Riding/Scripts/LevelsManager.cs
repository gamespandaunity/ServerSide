 
using UnityEngine;
using UnityEngine.Serialization;

public class LevelsManager : MonoBehaviour
{
    public static LevelsManager instance;

    [FormerlySerializedAs("Levels")] public GameObject[] raceTracks ;
    [FormerlySerializedAs("currentLevelsNumbersOfLaps")] public int currentLevelLapCount = 2;
    [FormerlySerializedAs("totalVehicleReachedBeforePlayer")] public int vehiclesFinishedBeforePlayer = 0;
    [FormerlySerializedAs("PlayerPosition")] public int playerPosition;
    [FormerlySerializedAs("PlayerLaps")] public int playerCompletedLaps = 1;
    [HideInInspector, FormerlySerializedAs("startTime")] public float raceStartTime = 0f;
    [HideInInspector, FormerlySerializedAs("endTime")] public float raceEndTime = 0f;
    [FormerlySerializedAs("FinishSToryCamera")] public GameObject endRaceCamera;
    [FormerlySerializedAs("Target")] public GameObject finishLineTarget ;
    [FormerlySerializedAs("MultiPlayerMAXRound")] public int maxMultiplayerRounds = 56;
    [FormerlySerializedAs("bl_MiniMap")] public bl_MiniMap miniMap ;
    [FormerlySerializedAs("finishPoint")] public GameObject finishLine ;


    // Use this for initialization
    void Awake () {
		instance = this;
	}

	void Start(){
        //Photon Removal    PunNetwork.instance.IsAIControlledHorse = false;
        //Photon Removal     PhotonNetwork.CurrentRoom.Name.Show("Room Name");
        if (MConstants.CurrentGameMode != MConstants.GAME_MODES.MULTI_PLAYER)
        {
            Debug.Log("Single Player Mode: Disabling MiniMap");
            HudMenuManager.instance.updateLapProgress(playerCompletedLaps, LevelsManager.instance.currentLevelLapCount);
        }

        if (finishLine  && MultiPlayerGame.isLastManStandingMode)
        {
            finishLine .SetActive(false);
        }
        //HudMenuManager.instance.lapText.text = PlayerLaps+ "/" + LevelsManager.instance.currentLevelsNumbersOfLaps;
        //HudMenuManager.instance.lapText1.text = PlayerLaps + "/" + LevelsManager.instance.currentLevelsNumbersOfLaps;

    }

	//public void SetStartTime(){
	public void StartRaceTimer(){
		raceStartTime = Time.time;
	}

	//public void SetEndTime(){
	public void EndRaceTimer(){
		raceEndTime = Time.time;

	}

}
