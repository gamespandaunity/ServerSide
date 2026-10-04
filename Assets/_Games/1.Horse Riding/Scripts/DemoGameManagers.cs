using Mirror;
using System.Collections;
using System.Collections.Generic;
using Tanks.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DemoGameManagers : NetworkBehaviour
{
    private readonly byte MoveUnitsToTargetPositionEvent = 0;
    public static DemoGameManagers Instance = null;

    [FormerlySerializedAs("InfoText")] public Text InformationText;
    [FormerlySerializedAs("rCC_PhotonDemo")] public HorsePhotonDemo HorsePhotonDemoController;
    [FormerlySerializedAs("AsteroidPrefabs")] public GameObject[] AsteroidPrefabArray;
    [FormerlySerializedAs("mLoadingScreen")] public GameObject loadingScreenUI;
    [FormerlySerializedAs("m_EndGameUiParent")][SerializeField] protected Transform endGameUIParent;
   // [FormerlySerializedAs("m_MultiplayerGameModal")][SerializeField] protected ResultManagerForHorse multiplayerGameModalUI;
  //  [FormerlySerializedAs("m_EndGameModal")] protected ResultManagerForHorse endGameModalUI;
    [FormerlySerializedAs("m_KillLogPhrases")][SerializeField] protected KillLogPhrases killLogPhrasesList;

    [FormerlySerializedAs("aiPlayersLis")] public List<PlayerPositionController> aiPlayersList;
    [FormerlySerializedAs("localPlayer")] public PlayerPositionController playerController;
    [FormerlySerializedAs("MultiPlayerMAXRound")] public int multiplayerMaxRounds = 56;
    [FormerlySerializedAs("waypointsContainer")] public HorseAiWaypointsContainer aiWaypointsContainer;
    [FormerlySerializedAs("levelManager")] public LevelsManager gameLevelManager;
    [FormerlySerializedAs("audioSource")] public AudioSource gameAudioSource;
    [FormerlySerializedAs("nitroSoundClip")] public AudioClip nitroSoundEffect;
    [FormerlySerializedAs("msgsList")] public List<string> gameMessagesList;
    [FormerlySerializedAs("StoryCamera")] public GameObject storyCameraController;
    [FormerlySerializedAs("InfoPanel")] public GameObject informationPanelUI;
    [FormerlySerializedAs("levelTrack")] public GameObject gameLevelTrack;
    [FormerlySerializedAs("waitToStartGame")] public float gameStartDelayTime = 2;

    int previousPosition = 1;
    int currentPlayerPosition = 1;
    bool isFirstGamePlay = true;
    bool isDisplayingLastPlaceMessage;
    float raceStartTime;
    float raceEndTime;
    float initialStartTime = 0;
    bool isAIForceEnabled;
    bool isPlayerReady;

    [SyncVar(hook=nameof(OnRaceOverSyncChanged))]
    bool raceOverSync = false;


    #region UNITY
    bool isGameOver;

    public void Awake()
    {
        MConstants.isRaceOver = false;
        MConstants.isRaceOverEnabled = false;

        Instance = this;
        if (aiPlayersList == null) aiPlayersList = new List<PlayerPositionController>();
        MConstants.isTimeOver = false;
        if (gameLevelManager != null) multiplayerMaxRounds = gameLevelManager.maxMultiplayerRounds;
        if (gameLevelTrack != null) gameLevelTrack.SetActive(true);
    }
    public void OnEnable()
    {
        EliminationCountDownTimer.OnCountdownTimerHasExpired += HandleCountdownExpiration;
    }
    private void Update()
    {
        // Server-driven race end check: keep server authoritative
        if (NetworkServer.active)
        {
            if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER && MConstants.isRaceOver && !isGameOver && !MultiPlayerGame.isLastManStandingMode)
            {
                Debug.Log("CurrentMode: " + MConstants.CurrentGameMode + " isRaceOver: " + MConstants.isRaceOver + " isGameOver: " + isGameOver);
                // Server triggers finish
                if (!isGameOver)
                {
                    TriggerWinLoss();
                }
            }
        }

        if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER && !MConstants.isTimeStart && Time.time >= (initialStartTime + gameStartDelayTime - 5) && !isPlayerReady)
        {
            isPlayerReady = true;
            // Mirror: you can use NetworkMessages or SyncVars if you need to share readiness
            // For now we simply note locally that player is ready (server-side readiness logic should be added as needed)
        }
        if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER && !MConstants.isTimeStart && Time.time >= initialStartTime + gameStartDelayTime)
        {
            isAIForceEnabled = true;
            MultiPlayerGame.setAIData();
            InitializeGame();
        }
    }

    public void updatePosition()
    {
        Debug.Log("🟩 [UpdatePosition] Start Function");

        if (!MConstants.isTimeStart)
        {
            Debug.Log("🟨 [UpdatePosition] Time not started yet (MConstants.isTimeStart = false)");
            // continue anyway; this helps keep clients' HUD in sync
        }

        if (aiPlayersList == null || aiPlayersList.Count == 0)
        {
            Debug.LogWarning("⚠️ [UpdatePosition] aiPlayersList is null or empty.");
            return;
        }

        aiPlayersList.Sort(LeaderboardSort2);

        int index = aiPlayersList.IndexOf(playerController);
        if (index < 0)
        {
            // Player not in list — defensive add
            if (playerController != null)
            {
                aiPlayersList.Add(playerController);
                aiPlayersList.Sort(LeaderboardSort2);
                index = aiPlayersList.IndexOf(playerController);
            }
        }

        Debug.Log("Player Position index: " + index);
        currentPlayerPosition = Mathf.Max(0, index); // avoid -1
        if (HudMenuManager.instance != null)
            HudMenuManager.instance.updatePosition(currentPlayerPosition + 1, aiPlayersList.Count);

        if (MultiPlayerGame.isLastManStandingMode)
        {
            if (EliminationModeController.Instance != null)
            {
                EliminationModeController.Instance.UpdatePlayerPosition(currentPlayerPosition + 1, aiPlayersList.Count);
                if ((currentPlayerPosition + 1) == aiPlayersList.Count)
                {
                    EliminationModeController.Instance.finalPlaceText.text = "You";
                    if (previousPosition != currentPlayerPosition && !isDisplayingLastPlaceMessage)
                    {
                        isDisplayingLastPlaceMessage = true;
                        Invoke(nameof(showLastPlaceMessage), 1);
                    }
                }
                else
                {
                    EliminationModeController.Instance.finalPlaceText.text = aiPlayersList[aiPlayersList.Count - 1].PlayerName;
                }
                previousPosition = currentPlayerPosition;
            }
        }
        else if (aiPlayersList.Count > 0 && aiPlayersList[0].TotalDistanecPoints + 1 >= multiplayerMaxRounds && !MConstants.isRaceOverEnabled)
        {
            // Race end condition detected locally — ask server to finish (server authoritative)
            MConstants.isRaceOverEnabled = true;
           //M RequestFinishGame();
        }

        if (HudMenuManager.instance != null)
            HudMenuManager.instance.updateLapProgress(playerController != null ? playerController.lap + 1 : 1, gameLevelManager.playerCompletedLaps);

        if (EliminationModeController.Instance != null && playerController != null)
            EliminationModeController.Instance.UpdateLapCount(playerController.lap + 1, gameLevelManager.playerCompletedLaps);
    }

    void showLastPlaceMessage()
    {
        isDisplayingLastPlaceMessage = false;
        if ((currentPlayerPosition + 1) == aiPlayersList.Count)
        {
            EliminationModeController.Instance.DisplayNotification(gameMessagesList[2], true);
        }
    }

    public void Start()
    {
        if (MConstants.CurrentGameMode != MConstants.GAME_MODES.MULTI_PLAYER)
        {
            Debug.Log("Single Player Mode Detected: Skipping Multiplayer Initialization");
            if (loadingScreenUI != null) loadingScreenUI.SetActive(false);
           //  InstantiateEndGameModal(multiplayerGameModalUI);
              if (loadingScreenUI != null) loadingScreenUI.SetActive(false);
        MConstants.isRaceOver = false;
        MConstants.isTimeStart = true;
        if(!NetworkServer.active)
          {
       //     Debug.Log("Single Player: Starting game directly.ReadyToGO");
           // if (ReadyToGO.Instance != null) ReadyToGO.Instance.prepareForGameStart();
          }
        
        Time.timeScale = MConstants.TIME_SCALE;
            return;
        }
        Debug.Log("Multiplayer Mode Detected: Initializing Multiplayer Game");

        if (HudMenuManager.instance != null)
            HudMenuManager.instance.updateLapProgress(1, gameLevelManager.playerCompletedLaps);

        if (EliminationModeController.Instance != null)
        {
            EliminationModeController.Instance.lastPlayerStandingUI.SetActive(false);
            EliminationModeController.Instance.timeTrialGameplayUI.SetActive(false);
        }

        if (HudMenuManager.instance != null)
            HudMenuManager.instance.racingUI.SetActive(false);

        initialStartTime = Time.time;

     //   InstantiateEndGameModal(multiplayerGameModalUI);
        //InitializeGame();
    }

    //private void InstantiateEndGameModal(ResultManagerForHorse endGame)
    //{Debug.Log("InstantiateEndGameModal called");
    //    if (endGame == null)
    //    {
    //        return;
    //    }

    //    if (endGameModalUI != null)
    //    {
    //        Destroy(endGameModalUI.gameObject);
    //        endGameModalUI = null;
    //    }

    //    endGameModalUI = Instantiate<ResultManagerForHorse>(endGame);
    //    endGameModel = endGameModalUI.gameObject.GetComponent<ResultManagerForHorse>();
    //    endGameModalUI.transform.SetParent(endGameUIParent, false);
    //    endGameModalUI.gameObject.SetActive(false);
    //}

    //public ResultManagerForHorse endGameModel;

    #endregion

    #region MIRROR CALLBACKS

   

    #endregion

    void Leave()
    {
        // Handle disconnect
        if (isServer)
        {
            // server-side cleanup (optional)
        }
    }

    private void OnApplicationPause(bool pause)
    {
        // Handle pause
    }

    private void InitializeGame()
    {
        if (HorsePhotonDemoController != null)
            HorsePhotonDemoController.SpawnVehicle(0);

        if (loadingScreenUI != null) loadingScreenUI.SetActive(false);
        MConstants.isRaceOver = false;
        MConstants.isTimeStart = true;
        if(MultiPlayerGame.isSinglePlayer)
        {
            Debug.Log("Single Player: Starting game directly.ReadyToGO");
             if (ReadyToGO.Instance != null) ReadyToGO.Instance.prepareForGameStart();
        }
        else
        {
            Debug.Log("Multiplayer: Starting game directly.ReadyToGO");
        }
       
        Time.timeScale = MConstants.TIME_SCALE;
    }

    void EndGame()
    {
        // End game logic (deprecated: use TriggerWinLoss / RpcOnRaceEnd)
    }

    private bool AreAllPlayersReady()
    {
        return true;
    }

    private void FinishGame()
    {
        Debug.Log("FinishGame called");
        Time.timeScale = 1f;
        isGameOver = true;

        if ( !MultiPlayerGame.isTimeTrial)
        {
           // endGameModalUI.setLeadeBoarList(GetLeaderboardElements());
           // endGameModalUI.ShowModel();
            if (MultiPlayerGame.isChampion)
            {
                MainMenuManager.isGoToLevels = true;
                if (WinFxController.LevelComplete)
                {
                    HudMenuManager.instance.gameOverPanel.gameObject.GetComponent<GameOverMenuManager>().UnlockLevel();
                }
            }
        }
        else
        {
            if (EliminationModeController.Instance != null)
                EliminationModeController.Instance.timeTrialEndGameUI.SetActive(true);
        }
    }

    private void HandleCountdownExpiration()
    {
        InitializeGame();
    }

    private void HandleEliminationTimeEnd()
    {
        updatePosition();

        if ((aiPlayersList.IndexOf(playerController) + 1) == aiPlayersList.Count)
        {
            string message = "YOU HAVE BEEN ELIMINATED.";
            if (EliminationModeController.Instance != null)
                EliminationModeController.Instance.DisplayNotification(message, true);

            // Request server to finish the game (authoritative)
            RequestFinishGame();
        }
        else
        {
            PlayerPositionController playerPosition = aiPlayersList[aiPlayersList.Count - 1];
            aiPlayersList.Remove(playerPosition);
            string message = playerPosition.PlayerName + " HAS BEEN ELIMINATED.";
            if (EliminationModeController.Instance != null)
                EliminationModeController.Instance.DisplayNotification(message, true);
            if (playerPosition != null) playerPosition.gameObject.SetActive(false);
            updatePosition();

            if (aiPlayersList.Count <= 1)
            {
                if (EliminationModeController.Instance != null)
                    EliminationModeController.Instance.DisplayNotification(gameMessagesList[3], true);

                // Server should determine end; request it
                RequestFinishGame();

                storyCameraController = LevelsManager.instance.endRaceCamera;
                if (storyCameraController != null && HorseMobileButton.Instance != null)
                {
                    storyCameraController.transform.parent = HorseMobileButton.Instance.horseController.transform;
                    storyCameraController.transform.localPosition = Vector3.zero;
                    storyCameraController.transform.localRotation = Quaternion.identity;
                    storyCameraController.SetActive(true);
                }

                Invoke(nameof(FinishLastManStandingGame), 4);
            }
            else
            {
                Invoke(nameof(BeginEliminationTimer), 3);
            }
        }
    }
    public void BeginEliminationTimer()
    {
        if (MultiPlayerGame.isLastManStandingMode)
        {
            if (EliminationModeController.Instance != null)
            {
                EliminationModeController.Instance.lastPlayerStandingUI.SetActive(true);
                EliminationModeController.Instance.lastPlayerTimeUI.SetActive(true);
                EliminationModeController.Instance.DisplayNotification(gameMessagesList[0]);
            }

            if (isFirstGamePlay)
            {
                if (EliminationModeController.Instance != null)
                {
                    EliminationModeController.Instance.DisplayNotification(gameMessagesList[1]);
                }
                isFirstGamePlay = false;
                InvokeRepeating(nameof(updatePosition), 1f, 1f);
            }
        }
        else
        {
            LevelsManager.instance.raceStartTime = Time.time;
            if (EliminationModeController.Instance != null) EliminationModeController.Instance.lastPlayerStandingUI.SetActive(false);
            if (HudMenuManager.instance != null) HudMenuManager.instance.racingUI.SetActive(!MultiPlayerGame.isTimeTrial);
            if (EliminationModeController.Instance != null) EliminationModeController.Instance.timeTrialGameplayUI.SetActive(MultiPlayerGame.isTimeTrial);
            if (EliminationModeController.Instance != null) EliminationModeController.Instance.timeTrialClock.isTimerRunning = MultiPlayerGame.isTimeTrial;
            if (EliminationModeController.Instance != null) EliminationModeController.Instance.lastPlayerTimeUI.SetActive(false);

            InvokeRepeating(nameof(updatePosition), 1f, 1f);
        }
    }
    public bool GetAIPlayerPriority(PlayerPositionController AiPlayer)
    {
        bool isHighPriority = false;
        aiPlayersList.Sort(LeaderboardSort2);

        if (aiPlayersList.IndexOf(playerController) == 0 && aiPlayersList.IndexOf(AiPlayer) >= 1)
        {
            isHighPriority = true;
        }
        return isHighPriority;
    }

    void FinishLastManStandingGame()
    {
        // Local client display for finalization. The server will also trigger the global finish.
        if (EliminationModeController.Instance != null)
            EliminationModeController.Instance.DisplayRaceResult(false);
    }

    void FinishLastManStandingWin()
    {
        if (EliminationModeController.Instance != null)
            EliminationModeController.Instance.DisplayRaceResult(true);
        Time.timeScale = 1f;
    }

    public virtual List<LeaderboardElement> GetLeaderboardElements()
    {
        List<LeaderboardElement> leaderboardElements = new List<LeaderboardElement>();

        if (informationPanelUI && informationPanelUI.activeInHierarchy)
        {
            informationPanelUI.SetActive(false);
        }

        for (int i = 0; i < aiPlayersList.Count; ++i)
        {
            PlayerPositionController tempPlayer = aiPlayersList[i];
            LeaderboardElement leaderboardElement = null;

            if (tempPlayer.Equals(playerController))
            {
                leaderboardElement = new LeaderboardElement("You", Color.red,
                    (int)(tempPlayer.TotalDistanecPoints * 100 + (tempPlayer.isAtFirst ? 2500 : 0)) - tempPlayer.playerPoisition * 100,
                    tempPlayer.playerCountry, tempPlayer.awatarID);
                leaderboardElement.SetLocal(true);
            }
            else
            {
                leaderboardElement = new LeaderboardElement(tempPlayer.PlayerName, Color.white,
                    (int)(tempPlayer.TotalDistanecPoints * 100) - tempPlayer.playerPoisition * 100,
                    tempPlayer.playerCountry, tempPlayer.awatarID);
            }

            leaderboardElements.Add(leaderboardElement);
        }

        leaderboardElements.Sort(LeaderboardSort);
        for (int i = 0; i < leaderboardElements.Count; ++i)
        {
            leaderboardElements[i].SetRank(i + 1);
        }
        return leaderboardElements;
    }

    public void addAiPlayer(PlayerPositionController player)
    {
        Debug.Log("addAiPlayer " + player);
        if (aiPlayersList == null) aiPlayersList = new List<PlayerPositionController>();
        aiPlayersList.Add(player);
        Debug.Log("aiPlayersList count: " + aiPlayersList.Count);
        if (HudMenuManager.instance != null) HudMenuManager.instance.updatePosition(1, aiPlayersList.Count);
    }

    protected int LeaderboardSort2(PlayerPositionController player1, PlayerPositionController player2)
    {
        return (int)(player2.TotalDistanecPoints * 100) - (int)(player1.TotalDistanecPoints * 100);
    }

    protected int LeaderboardSort(LeaderboardElement player1, LeaderboardElement player2)
    {
        return player2.score - player1.score;
    }

    public void WaitingForOpponentt(bool state)
    {
        PopupMessageManager.instance.SetPanelStaus(state, body: "Waiting for opponent");
    }

    public void playNitroSound()
    {
        if (gameAudioSource != null && nitroSoundEffect != null)
            gameAudioSource.PlayOneShot(nitroSoundEffect);
    }

    public const float ASTEROIDS_MIN_SPAWN_TIME = 5.0f;
    public const float ASTEROIDS_MAX_SPAWN_TIME = 10.0f;
    public const float PLAYER_RESPAWN_TIME = 4.0f;
    public const int PLAYER_MAX_LIVES = 3;

    public const string PLAYER_LIVES = "PlayerLives";
    public const string PLAYER_READY = "IsPlayerReady";
    public const string PLAYER_LOADED_LEVEL = "PlayerLoadedLevel";
    public const string IS_GAME_OVER = "gameOver";

    #region Race End / Server RPCs

    // PUBLIC: clients call this to ask server to finish (server authoritative)
    public void RequestFinishGame()
    {
        Debug.Log("🏁 [FinishTrigger] RequestFinishGame called by client.");
        if(MultiPlayerGame.isSinglePlayer)
        {
            TriggerWinLoss();
        }
        else
        {
            if (NetworkServer.active)
        {
            TriggerWinLoss();
        }
        else
        {
            CmdRequestFinishGame();
        }
        }
        
    }

    [Command]
    private void CmdRequestFinishGame()
    {
        // A client asked the server to finish — server validates and triggers
        TriggerWinLoss();
    }

    // Server-only: central place to declare race over and notify clients
    private void TriggerWinLoss()
{
    Debug.Log("🏁 [FinishTrigger] TriggerWinLoss called on server."+isGameOver);
    if (isGameOver) return;
    isGameOver = true;

    MConstants.isRaceOver = true;

    if (NetworkServer.active)
    {
        // Multiplayer clients ke liye
        raceOverSync = true;
        RpcOnRaceEnd();
    }
    else
    {
        // AI / single player mode ke liye
        FinishGame();
    }
}
    // This runs on all clients
    [ClientRpc]
    private void RpcOnRaceEnd()
    {
        // Make sure clients update HUD and display final UI
        MConstants.isRaceOver = true;

        // Update positions and show finish UI
        updatePosition();
        FinishGame();
    }

    // SyncVar hook for extra safety (runs on clients when raceOverSync changes)
    private void OnRaceOverSyncChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            // Ensure clients also call local finish logic if Rpc missed for any reason
            if (!isGameOver)
            {
                isGameOver = true;
                updatePosition();
                FinishGame();
            }
        }
    }

    #endregion
}
// using Mirror;
// using System.Collections;
// using System.Collections.Generic;
// using Tanks.UI;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.Serialization;
// using UnityEngine.UI;

// public class DemoGameManagers : NetworkBehaviour
// {
//     private readonly byte MoveUnitsToTargetPositionEvent = 0;
//     public static DemoGameManagers Instance = null;

//     [FormerlySerializedAs("InfoText")] public Text InformationText;
//     [FormerlySerializedAs("rCC_PhotonDemo")] public HorsePhotonDemo HorsePhotonDemoController;
//     [FormerlySerializedAs("AsteroidPrefabs")] public GameObject[] AsteroidPrefabArray;
//     [FormerlySerializedAs("mLoadingScreen")] public GameObject loadingScreenUI;
//     [FormerlySerializedAs("m_EndGameUiParent")][SerializeField] protected Transform endGameUIParent;
//     [FormerlySerializedAs("m_MultiplayerGameModal")][SerializeField] protected EndGameModal multiplayerGameModalUI;
//     [FormerlySerializedAs("m_EndGameModal")] protected EndGameModal endGameModalUI;
//     [FormerlySerializedAs("m_KillLogPhrases")][SerializeField] protected KillLogPhrases killLogPhrasesList;

//     [FormerlySerializedAs("aiPlayersLis")] public List<PlayerPositionController> aiPlayersList;
//     [FormerlySerializedAs("localPlayer")] public PlayerPositionController playerController;
//     [FormerlySerializedAs("MultiPlayerMAXRound")] public int multiplayerMaxRounds = 56;
//     [FormerlySerializedAs("waypointsContainer")] public HorseAiWaypointsContainer aiWaypointsContainer;
//     [FormerlySerializedAs("levelManager")] public LevelsManager gameLevelManager;
//     [FormerlySerializedAs("audioSource")] public AudioSource gameAudioSource;
//     [FormerlySerializedAs("nitroSoundClip")] public AudioClip nitroSoundEffect;
//     [FormerlySerializedAs("msgsList")] public List<string> gameMessagesList;
//     [FormerlySerializedAs("StoryCamera")] public GameObject storyCameraController;
//     [FormerlySerializedAs("InfoPanel")] public GameObject informationPanelUI;
//     [FormerlySerializedAs("levelTrack")] public GameObject gameLevelTrack;
//     [FormerlySerializedAs("waitToStartGame")] public float gameStartDelayTime = 2;

//     int previousPosition = 1;
//     int currentPlayerPosition = 1;
//     bool isFirstGamePlay = true;
//     bool isDisplayingLastPlaceMessage;
//     float raceStartTime;
//     float raceEndTime;
//     float initialStartTime = 0;
//     bool isAIForceEnabled;
//     bool isPlayerReady;

//     public EndGameModal endGameModal
//     {
//         get
//         {
//             return endGameModalUI;
//         }
//     }

//     #region UNITY
//     bool isGameOver;

//     public void Awake()
//     {
//         MConstants.isRaceOver = false;
//         MConstants.isRaceOverEnabled = false;

//         Instance = this;
//         aiPlayersList = new List<PlayerPositionController>();
//         MConstants.isTimeOver = false;
//         multiplayerMaxRounds = gameLevelManager.maxMultiplayerRounds;
//         gameLevelTrack.SetActive(true);
//     }
//     public void OnEnable()
//     {
//         EliminationCountDownTimer.OnCountdownTimerHasExpired += HandleCountdownExpiration;
//     }
//     private void Update()
//     {

//         if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER && MConstants.isRaceOver && !isGameOver && !MultiPlayerGame.isLastManStandingMode)
//         {
//             Hashtable props = new Hashtable
//                     {
//                         {IS_GAME_OVER, true}
//                     };
//            // PhotonNetwork.CurrentRoom.SetCustomProperties(props);
//             gameAudioSource .Play();
//             LevelsManager.instance.raceEndTime = Time.time;
//             EliminationModeController.Instance.timeTrialClock.isTimerRunning = false;
//             isGameOver = true;

//         }

//         if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER && !MConstants.isTimeStart && Time.time >= (initialStartTime  + gameStartDelayTime - 5) && !isPlayerReady )
//         {
//             isPlayerReady  = true;
//             Hashtable props = new Hashtable
//             {
//                 {PLAYER_LOADED_LEVEL, true}
//             };
//            // PhotonNetwork.LocalPlayer.SetCustomProperties(props);
//         }
//         if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER && !MConstants.isTimeStart && Time.time >= initialStartTime  + gameStartDelayTime)
//         {
//             isAIForceEnabled = true;
//             MultiPlayerGame.setAIData();
//          //   MultiPlayerEnemyCreator.Instance.CreateAIPlayer();
//             InitializeGame();
//         }
//     }
//     public void updatePosition()
//     {
//         Debug.Log("🟩 [UpdatePosition] Start Function");

//         if (!MConstants.isTimeStart)
//         {
//             Debug.Log("🟨 [UpdatePosition] Time not started yet (MConstants.isTimeStart = false)");
//            // return;
//         }
//         aiPlayersList.Sort(LeaderboardSort2);

//         Debug.Log("Player Position" + (aiPlayersList.IndexOf(playerController)+1));
//         currentPlayerPosition = aiPlayersList.IndexOf(playerController);
//         HudMenuManager.instance.updatePosition(currentPlayerPosition + 1, aiPlayersList.Count);

//         if (MultiPlayerGame.isLastManStandingMode)
//         {
//             EliminationModeController.Instance.UpdatePlayerPosition(currentPlayerPosition + 1, aiPlayersList.Count);
//             if ((currentPlayerPosition + 1) == aiPlayersList.Count)
//             {
//                 EliminationModeController.Instance.finalPlaceText.text = "You";
//                 if (previousPosition != currentPlayerPosition && !isDisplayingLastPlaceMessage)
//                 {
//                     isDisplayingLastPlaceMessage = true;
//                     Invoke("showLastPlaceMessage", 1);
//                     // 

//                 }
//             }
//             else
//             {
//                 EliminationModeController.Instance.finalPlaceText.text = aiPlayersList[aiPlayersList.Count - 1].PlayerName;

//             }
//             previousPosition = currentPlayerPosition;
//         }
//         else if (aiPlayersList[0].TotalDistanecPoints + 1 >= multiplayerMaxRounds && !MConstants.isRaceOverEnabled)
//         {
//             //Debug.Log("Race Over" );

//             MConstants.isRaceOverEnabled = true;

//             //MConstants.isRaceOver = true;
//         }
//         //}
//         //Debug.Log("Update Positopn  " + aiPlayersLis[0].TotalDistanecPoints + " RC " + MConstants.isRaceOverEnabled);

//         HudMenuManager.instance.updateLapProgress(playerController.lap + 1, gameLevelManager.playerCompletedLaps);
//         EliminationModeController.Instance.UpdateLapCount(playerController.lap + 1, gameLevelManager.playerCompletedLaps);

//         // if (aiPlayersList == null)
//         // {
//         //     Debug.LogError("❌ [UpdatePosition] aiPlayersList is NULL!");
//         //     return;
//         // }

//         // Debug.Log($"🟦 [UpdatePosition] Before Sorting | aiPlayersList Count: {aiPlayersList.Count}");
//         // aiPlayersList.Sort(LeaderboardSort2);
//         // Debug.Log("🟦 [UpdatePosition] After Sorting | Top Player: " +
//         //           (aiPlayersList.Count > 0 ? aiPlayersList[0].PlayerName : "None"));

//         // // ✅ Fix: Make sure current player exists in the list
//         // if (!aiPlayersList.Contains(playerController))
//         // {
//         //     Debug.LogWarning($"⚠️ [UpdatePosition] Player '{playerController.PlayerName}' not found in aiPlayersList. Adding now...");
//         //     aiPlayersList.Add(playerController);
//         //     aiPlayersList.Sort(LeaderboardSort2); // resort after adding
//         // }

//         // currentPlayerPosition = aiPlayersList.IndexOf(playerController);
//         // Debug.Log($"🟩 [UpdatePosition] Current Player: {playerController.PlayerName} | Position Index: {currentPlayerPosition}");

//         // // --- HUD ---
//         // if (HudMenuManager.instance == null)
//         // {
//         //     Debug.LogError("❌ [UpdatePosition] HudMenuManager.instance is NULL!");
//         // }
//         // else
//         // {
//         //     Debug.Log("🟩 [UpdatePosition] Updating HUD position...");
//         //     HudMenuManager.instance.updatePosition(currentPlayerPosition + 1, aiPlayersList.Count);
//         // }

//         // // --- LAST MAN STANDING MODE ---
//         // if (MultiPlayerGame.isLastManStandingMode)
//         // {
//         //     Debug.Log("🟦 [UpdatePosition] Last Man Standing Mode Active");

//         //     if (EliminationModeController.Instance == null)
//         //     {
//         //         Debug.LogError("❌ [UpdatePosition] EliminationModeController.Instance is NULL!");
//         //     }
//         //     else
//         //     {
//         //         Debug.Log("🟩 [UpdatePosition] Updating Player Position in Elimination Mode...");
//         //         EliminationModeController.Instance.UpdatePlayerPosition(currentPlayerPosition + 1, aiPlayersList.Count);

//         //         if ((currentPlayerPosition + 1) == aiPlayersList.Count)
//         //         {
//         //             Debug.Log("🟥 [UpdatePosition] Player is in LAST PLACE!");

//         //             EliminationModeController.Instance.finalPlaceText.text = "You";

//         //             if (previousPosition != currentPlayerPosition && !isDisplayingLastPlaceMessage)
//         //             {
//         //                 Debug.Log("🟩 [UpdatePosition] Showing Last Place Message...");
//         //                 isDisplayingLastPlaceMessage = true;
//         //                 Invoke(nameof(showLastPlaceMessage), 1);
//         //             }
//         //         }
//         //         else
//         //         {
//         //             string lastPlayerName = aiPlayersList[aiPlayersList.Count - 1].PlayerName;
//         //             Debug.Log("🟩 [UpdatePosition] Last Player in List: " + lastPlayerName);
//         //             EliminationModeController.Instance.finalPlaceText.text = lastPlayerName;
//         //         }

//         //         previousPosition = currentPlayerPosition;
//         //         Debug.Log("🟩 [UpdatePosition] Updated previousPosition = " + previousPosition);
//         //     }
//         // }
//         // // --- NORMAL MODE ---
//         // else if (aiPlayersList.Count > 0 && aiPlayersList[0].TotalDistanecPoints + 1 >= multiplayerMaxRounds && !MConstants.isRaceOverEnabled)
//         // {
//         //     Debug.Log("🟧 [UpdatePosition] Race Over Trigger Condition Met!");
//         //     MConstants.isRaceOverEnabled = true;
//         // }

//         // // --- LAP UPDATES ---
//         // if (HudMenuManager.instance != null)
//         // {
//         //     Debug.Log("🟩 [UpdatePosition] Updating Lap Progress...");
//         //     HudMenuManager.instance.updateLapProgress(playerController.lap + 1, gameLevelManager.playerCompletedLaps);
//         // }

//         // if (EliminationModeController.Instance != null)
//         // {
//         //     Debug.Log("🟩 [UpdatePosition] Updating Lap Count in Elimination Mode...");
//         //     EliminationModeController.Instance.UpdateLapCount(playerController.lap + 1, gameLevelManager.playerCompletedLaps);
//         // }

//         // Debug.Log("✅ [UpdatePosition] Function Completed Successfully\n");
//     }

//     void showLastPlaceMessage()
//     {
//         isDisplayingLastPlaceMessage = false;
//         if ((currentPlayerPosition + 1) == aiPlayersList.Count)
//         {
//             EliminationModeController.Instance.DisplayNotification(gameMessagesList[2], true);
//         }
//     }

//     public void Start()
//     {
//        // MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;
//         if (MConstants.CurrentGameMode != MConstants.GAME_MODES.MULTI_PLAYER)
//         {
//             Debug.Log("Single Player Mode Detected: Skipping Multiplayer Initialization");
//             loadingScreenUI.SetActive(false);
//             return;
//         }
//         Debug.Log("Multiplayer Mode Detected: Initializing Multiplayer Game");

//         HudMenuManager.instance.updateLapProgress(1, gameLevelManager.playerCompletedLaps);
//         EliminationModeController.Instance.lastPlayerStandingUI.SetActive(false);
//         HudMenuManager.instance.racingUI.SetActive(false);
//         EliminationModeController.Instance.timeTrialGameplayUI.SetActive(false);

//         initialStartTime = Time.time;

//         InstantiateEndGameModal(multiplayerGameModalUI);
//         InitializeGame();
//     }

//     private void InstantiateEndGameModal(EndGameModal endGame)
//     {
//         if (endGame == null)
//         {
//           //  return;
//         }

//         if (endGameModalUI != null)
//         {
//             Destroy(endGameModalUI.gameObject);
//             endGameModalUI = null;
//         }

//         endGameModalUI = Instantiate<EndGameModal>(endGame);
//         endGameModel = endGameModalUI.gameObject.GetComponent<MultiplayerEndGameModal>();
//         endGameModalUI.transform.SetParent(endGameUIParent, false);
//         endGameModalUI.gameObject.SetActive(false);
//     }

//     public MultiplayerEndGameModal endGameModel;

//     #endregion

//     #region MIRROR CALLBACKS

//     public override void OnStartServer()
//     {
//         base.OnStartServer();
//         // Server-specific initialization
//     }

//     public override void OnStartClient()
//     {
//         base.OnStartClient();
//         // Client-specific initialization
//     }

//     #endregion

//     void Leave()
//     {
//         // Handle disconnect
//     }

//     private void OnApplicationPause(bool pause)
//     {
//         // Handle pause
//     }

//     private void InitializeGame()
//     {
//         HorsePhotonDemoController.SpawnVehicle(0);
//         loadingScreenUI.SetActive(false);
//         MConstants.isRaceOver = false;
//         MConstants.isTimeStart = true;
//         ReadyToGO.Instance.prepareForGameStart();
//         Time.timeScale = MConstants.TIME_SCALE;
//     }

//     void EndGame()
//     {
//         // End game logic
//     }

//     private bool AreAllPlayersReady()
//     {
//         return true;
//     }

//     private void FinishGame()
//     {
//         Time.timeScale = 1f;
//         isGameOver = true;

//         if (endGameModalUI != null && !MultiPlayerGame.isTimeTrial)
//         {
//             endGameModalUI.setLeadeBoarList(GetLeaderboardElements());
//             endGameModalUI.ShowModel();
//             if (MultiPlayerGame.isChampion)
//             {
//                 MainMenuManager.isGoToLevels = true;
//                 if (WinFxController.LevelComplete)
//                 {
//                     HudMenuManager.instance.gameOverPanel.gameObject.GetComponent<GameOverMenuManager>().UnlockLevel();
//                 }
//             }
//         }
//         else
//         {
//             EliminationModeController.Instance.timeTrialEndGameUI.SetActive(true);
//         }
//     }

//     private void HandleCountdownExpiration()
//     {
//         InitializeGame();
//     }

//     private void HandleEliminationTimeEnd()
//     {
//         updatePosition();

//         if ((aiPlayersList.IndexOf(playerController) + 1) == aiPlayersList.Count)
//         {
//             string message = "YOU HAVE BEEN ELIMINATED.";
//             EliminationModeController.Instance.DisplayNotification(message, true);
//             MConstants.isRaceOver = true;

//             Invoke(nameof(FinishLastManStandingGame), 2);
//         }
//         else
//         {
//             PlayerPositionController playerPosition = aiPlayersList[aiPlayersList.Count - 1];
//             aiPlayersList.Remove(playerPosition);
//             string message = playerPosition.PlayerName + " HAS BEEN ELIMINATED.";
//             EliminationModeController.Instance.DisplayNotification(message, true);
//             playerPosition.gameObject.SetActive(false);
//             updatePosition();

//             if (aiPlayersList.Count <= 1)
//             {
//                 EliminationModeController.Instance.DisplayNotification(gameMessagesList[3], true);
//                 MConstants.isRaceOver = true;
//                 storyCameraController = LevelsManager.instance.endRaceCamera;
//                 storyCameraController.transform.parent = HorseMobileButton.Instance.horseController.transform;
//                 storyCameraController.transform.localPosition = Vector3.zero;
//                 storyCameraController.transform.localRotation = Quaternion.identity;
//                 LevelsManager.instance.finishLineTarget = HorseMobileButton.Instance.horseController.gameObject;

//                 storyCameraController.SetActive(true);
//                 Invoke(nameof(FinishLastManStandingGame), 4);
//             }
//             else
//             {
//                 Invoke(nameof(BeginEliminationTimer), 3);
//             }
//         }
//     }
//     public void BeginEliminationTimer()
//     {
//         if (MultiPlayerGame.isLastManStandingMode)
//         {
//             EliminationModeController.Instance.lastPlayerStandingUI.SetActive(true);
//             EliminationModeController.Instance.lastPlayerTimeUI.SetActive(true);


//             HudMenuManager.instance.racingUI.SetActive(false);
//             EliminationModeController.Instance.timeTrialGameplayUI.SetActive(false);

//             //Hashtable props = new Hashtable
//            //         {
//             //            {EliminationCountDownTimer.CountdownStartTime, (float) PhotonNetwork.Time}
//               //      };
//           //  PhotonNetwork.CurrentRoom.SetCustomProperties(props);

//             EliminationModeController.Instance.DisplayNotification(gameMessagesList [0]);
//             if (isFirstGamePlay)
//             {
//                 EliminationModeController.Instance.DisplayNotification(gameMessagesList [1]);
//                 isFirstGamePlay = false;
//                 InvokeRepeating("updatePosition", 1f, 1f);

//             }

//         }
//         else
//         {
//             LevelsManager.instance.raceStartTime = Time.time;
//             EliminationModeController.Instance.lastPlayerStandingUI.SetActive(false);
//             HudMenuManager.instance.racingUI.SetActive(!MultiPlayerGame.isTimeTrial);
//             EliminationModeController.Instance.timeTrialGameplayUI.SetActive(MultiPlayerGame.isTimeTrial);
//             EliminationModeController.Instance.timeTrialClock.isTimerRunning = MultiPlayerGame.isTimeTrial;
//             EliminationModeController.Instance.lastPlayerTimeUI.SetActive(false);

//             InvokeRepeating("updatePosition", 1f, 1f);

//         }



//     }
//     public bool GetAIPlayerPriority(PlayerPositionController AiPlayer)
//     {
//         bool isHighPriority = false;
//         aiPlayersList.Sort(LeaderboardSort2);

//         if (aiPlayersList.IndexOf(playerController) == 0 && aiPlayersList.IndexOf(AiPlayer) >= 1)
//         {
//             isHighPriority = true;
//         }
//         return isHighPriority;
//     }

//     void FinishLastManStandingGame()
//     {
//         EliminationModeController.Instance.DisplayRaceResult(false);
//     }

//     void FinishLastManStandingWin()
//     {
//         EliminationModeController.Instance.DisplayRaceResult(true);
//         Time.timeScale = 1f;
//     }

//     public virtual List<LeaderboardElement> GetLeaderboardElements()
//     {
//         List<LeaderboardElement> leaderboardElements = new List<LeaderboardElement>();

//         if (informationPanelUI && informationPanelUI.activeInHierarchy)
//         {
//             informationPanelUI.SetActive(false);
//         }

//         try
//         {
//         }
//         catch (System.Exception)
//         {
//             informationPanelUI.SetActive(true);
//             throw;
//         }

//         for (int i = 0; i < aiPlayersList.Count; ++i)
//         {
//             PlayerPositionController tempPlayer = aiPlayersList[i];
//             LeaderboardElement leaderboardElement = null;

//             if (tempPlayer.Equals(playerController))
//             {
//                 leaderboardElement = new LeaderboardElement("You", Color.red,
//                     (int)(tempPlayer.TotalDistanecPoints * 100 + (tempPlayer.isAtFirst ? 2500 : 0)) - tempPlayer.playerPoisition * 100,
//                     tempPlayer.playerCountry, tempPlayer.awatarID);
//                 leaderboardElement.SetLocal(true);
//             }
//             else
//             {
//                 leaderboardElement = new LeaderboardElement(tempPlayer.PlayerName, Color.white,
//                     (int)(tempPlayer.TotalDistanecPoints * 100) - tempPlayer.playerPoisition * 100,
//                     tempPlayer.playerCountry, tempPlayer.awatarID);
//             }

//             leaderboardElements.Add(leaderboardElement);
//         }

//         leaderboardElements.Sort(LeaderboardSort);
//         for (int i = 0; i < leaderboardElements.Count; ++i)
//         {
//             leaderboardElements[i].SetRank(i + 1);
//         }
//         return leaderboardElements;
//     }

//     public void addAiPlayer(PlayerPositionController player)
//     {
//         Debug.Log("addAiPlayer" + player);
//         aiPlayersList.Add(player);
//         Debug.Log("aiPlayersList" + aiPlayersList);
//         HudMenuManager.instance.updatePosition(1, aiPlayersList.Count);
        
//     }

//     protected int LeaderboardSort2(PlayerPositionController player1, PlayerPositionController player2)
//     {
//         return (int)(player2.TotalDistanecPoints * 100) - (int)(player1.TotalDistanecPoints * 100);
//     }

//     protected int LeaderboardSort(LeaderboardElement player1, LeaderboardElement player2)
//     {
//         return player2.score - player1.score;
//     }

//     public void WaitingForOpponentt(bool state)
//     {
//         if (state == true)
//         {
//             PopupMessageManager.instance.SetPanelStaus(state, body: "Waiting for opponent");
//         }
//         else
//         {
//             PopupMessageManager.instance.SetPanelStaus(state, body: "Waiting for opponent");
//         }
//     }

//     public void playNitroSound()
//     {
//         gameAudioSource.PlayOneShot(nitroSoundEffect);
//     }

//     public const float ASTEROIDS_MIN_SPAWN_TIME = 5.0f;
//     public const float ASTEROIDS_MAX_SPAWN_TIME = 10.0f;
//     public const float PLAYER_RESPAWN_TIME = 4.0f;
//     public const int PLAYER_MAX_LIVES = 3;

//     public const string PLAYER_LIVES = "PlayerLives";
//     public const string PLAYER_READY = "IsPlayerReady";
//     public const string PLAYER_LOADED_LEVEL = "PlayerLoadedLevel";
//     public const string IS_GAME_OVER = "gameOver";
// }