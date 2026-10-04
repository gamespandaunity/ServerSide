using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class ReadyToGO : MonoBehaviour {
    public static bool hasGameStarted = false;
	public static int count = 0;
    [FormerlySerializedAs("TextArray")] public GameObject[] uiTextElements ;
    [FormerlySerializedAs("blackImage")] public GameObject overlayImage ;
    [FormerlySerializedAs("rcMobileButton")] public HorseMobileButton mobileControlButton;
    [FormerlySerializedAs("LevelMap")] public GameObject levelOverviewMap ;
    [FormerlySerializedAs("PlayerPoints")] public GameObject playerScoreDisplay ;


    public static ReadyToGO Instance;
    private void Awake()
    {
        Instance = this;
    }
    void Start ()
    {
		 if(MultiPlayerGame.isSinglePlayer)
		 {
			 Debug.Log("Single Player: Starting game directly.ReadyToGO");
			  beginGame();
		 }
		 else
		 {
			 Debug.Log("Multiplayer: Waiting to start game.ReadyToGO");
		 }
	    
    }



	//public void readyToGo(){
	public void prepareForGameStart(){

		   overlayImage .SetActive (true);
			startCountdown();
	

	}

	//public void Counter(){
	public void startCountdown(){
		Debug.Log("StartCount1");
        levelOverviewMap .SetActive(false);
        playerScoreDisplay .SetActive(false);
        if ( count < 4 ){
			overlayImage .SetActive (false);

			overlayImage .SetActive (true);

			for (int i = 0; i < uiTextElements .Length; i++) {
				uiTextElements  [i].SetActive (false);
			}
			uiTextElements  [count].SetActive (true);
           // RCC_Camera.instance.thisCam.enabled = false;

            count += 1;
			// LevelsManager.instance.Levels [MConstants.CurrentLevelNumber - 1].GetComponent<Level> ().ActiveCamera(count);
			
			//
			if (!MultiPlayerGame.isChampion)
			{
				LevelsManager.instance.raceTracks  [MConstants.CurrentLevelNumber - 1].GetComponent<Level> ().ActivateCamera(count);
			}
			//

			StartCoroutine(decreaseCountdown());
		
			    
		}else{
			Debug.Log("StartCount2");
			levelOverviewMap .SetActive(true);
            playerScoreDisplay .SetActive(true);
            for (int i = 0; i < uiTextElements .Length; i++) {
				uiTextElements  [i].SetActive (false);
			}
			mobileControlButton.enabled = true;
			mobileControlButton.EnableControls ();
			// LevelsManager.instance.Levels [MConstants.CurrentLevelNumber - 1].GetComponent<Level> ().DeActiveCameras();  
			// LevelsManager.instance.SetStartTime ();
			
			//
			if (!MultiPlayerGame.isChampion)
			{
				LevelsManager.instance.raceTracks  [MConstants.CurrentLevelNumber - 1].GetComponent<Level> ().DeactivateAllCameras();  
				LevelsManager.instance.StartRaceTimer ();
			}
			//
			
			overlayImage .SetActive (false);
			//RCC_Camera.instance.thisCam.enabled = true;
			MultiPlayerEnemyCreator.Instance.InitializeAI();
            if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER && MultiPlayerEnemyCreator.Instance)
			{
				Debug.Log("InitializeAI");
                MultiPlayerEnemyCreator.Instance.InitializeAI();

            }
            DemoGameManagers.Instance.BeginEliminationTimer();
            //Start Game
        }
    }

	//private IEnumerator  DecrementCount()
	private IEnumerator  decreaseCountdown()
	{
		float pauseEndTime = Time.realtimeSinceStartup + 2f;
		while (Time.realtimeSinceStartup < pauseEndTime)
		{
			yield return 0;
		}
			startCountdown();
	}

	//private void GameStart()
	private void beginGame()
	{
		count = 0;
		//Time.timeScale = 0;
		mobileControlButton.DisableControls ();
		mobileControlButton.enabled = false;
		if (MConstants.CurrentGameMode != MConstants.GAME_MODES.MULTI_PLAYER)
		{
			prepareForGameStart();

		}
	}
}
