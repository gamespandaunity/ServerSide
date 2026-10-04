//using AYellowpaper.SerializedCollections;
//using BallPool;
//using Fusion;
//using Fusion.Photon.Realtime;
//using NaughtyAttributes;
//using NetworkManagement;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class FusionNetwork : NetworkBehaviour
//{
//    [Header("Fusion Network")]
//    public static NetworkRunner networkRunner;
//    public GameObject runnerObject;
//    public NetworkPrefabRef fusionrpcmanager;
//    public PlayerRef opponent;
//    public static FusionRpcManager _fusionRpcManager;
//    private AightBallPoolNetworkMessenger poolMessenger;

//    // Delegates - keep same interface as PUN2
//    public delegate void OnLobbyJoinedDelegate();
//    public static event OnLobbyJoinedDelegate HandleLobbyJoined;

//    public delegate void OnJoinnedRoomDelegate(SessionInfo session);
//    public static OnJoinnedRoomDelegate HandleRoomJoined;
//    public static OnJoinnedRoomDelegate HandleRoomCreated;
//    public static FusionNetwork instance;
//    public bool isGameBegun = false;
//    public static bool HasWinnerBeenDeclared = false;
//    NetworkEvents events;
//    public NetworkRunner currentNetworkRunner;
//    public string Region;
//    [ContextMenu("GetCurrentNetWorkRunner")]
//    public void GetCurrentNetWorkRunner()
//    {
//        currentNetworkRunner = networkRunner;
//    }
//    private void Start()
//    {
//        if (instance == null)
//        {
//            instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        else
//        {
//            Destroy(instance.gameObject);
//            instance = this;
//            DontDestroyOnLoad(gameObject);
//        }

//    }

//    public virtual void Initialize()
//    {
//        if (!poolMessenger)
//        {
//            poolMessenger = gameObject.AddComponent<AightBallPoolNetworkMessenger>();
//        }
//    }

//    protected virtual void Awake()
//    {

//        if (networkRunner == null)
//        {
//            SetUpRunner();
//        }
//        else
//        {
//            Destroy(networkRunner.gameObject);
//            SetUpRunner();
//        }
//        DontDestroyOnLoad(gameObject);
//        Connect();
//    }



//    public void SetUpRunner()
//    {
//        networkRunner = FindAnyObjectByType<NetworkRunner>();
//        if (networkRunner == null)
//        {
//            GameObject runnerObj = Instantiate(runnerObject);
//            networkRunner = runnerObj.GetComponent<NetworkRunner>();
//            networkRunner.GetComponent<NetworkEvents>().PlayerJoined.AddListener((d, r) =>{ });
//            DontDestroyOnLoad(runnerObj);
//        }
//    }
//    #region Game Joining
//    public void OnStartGameWithPlayer(PlayerProfile player, bool TryJoin,string region="")
//    {
       
//        // Initialize common properties
//        Region = region;
//        FusionNetwork.instance.IsAIControlledHorse = false;
//        staticVariables.isgoldcoins = player.isgoldcoins;
//        staticVariables.currentPrize = player.prize;
//        NetworkManager.opponentPlayer = player;
//        InitializeGameSpecificSettings(player);
//        HandleRoomJoined = GetRoomJoinHandler(player.gameId);
//        TryJoin.Show();
//        if (!TryJoin)
//        {
//            if (networkRunner == null)
//            {
//                networkRunner = FindAnyObjectByType<NetworkRunner>();
//            }

//            networkRunner.Spawn(fusionrpcmanager);
//            if (HandleRoomJoined != null)
//                HandleRoomJoined?.Invoke(networkRunner.SessionInfo);
//            HandleRoomJoined = null;
//        }
//        else
//        {
//            region.Show("Region");
//            var appSettings = PhotonAppSettings.Global;
//            appSettings.AppSettings.FixedRegion = region;
//            JoinGameRoom(player);
//        }
//    }
    

   
//    public void InitializeGameSpecificSettings(PlayerProfile player)
//    {
//        switch (player.gameId)
//        {
//            case "1": // Ball Pool
//                BallPoolGameLogic.playMode = BallPool.PlayMode.OnLine;
//                BallPoolPlayer.players[0].SetCoins(NetworkManager.mainPlayer.coins);
//                BallPoolPlayer.players[1] = new AightBallPoolPlayer(
//                    int.Parse(player.userId),
//                    player.userName,
//                    player.coins,
//                    player.image,
//                    player.imageURL);
//                break;

//            case "11": // Horse Racing
//                MultiPlayerGame.isTimeTrial = false;
//                MultiPlayerGame.isLastManStandingMode = false;
//                MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;
//                MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playWithFriend;
//                MultiPlayerGame.isSinglePlayer = false;
//                break;

//            case "15": // Snooker
//                GamesPandaLauncher.RoomName = "";
//                GamesPandaLauncher.SceneToLoad = "SnokkerMultiplayer";
//                GamesPandaLauncher.IsMaster = true;
//                break;
//        }
//    }

//    public static StartGameArgs ConfigureSessionSettings(bool IsVisible)
//    {
//        int emptySessionTime = ApiAndRoomManager._instance.currentBetData.data.challenge_time_minutes * 60 * 1000;
        
//        var sessionArgs = new StartGameArgs()
//        {
//            GameMode = GameMode.Host,
//            SessionName = ApiAndRoomManager._instance.challenge_transaction_id,
//            PlayerCount = 2,
//            IsVisible = IsVisible,
//            IsOpen = true,
//            Scene = SceneRef.FromIndex(0) // Current scene
//        };

//        // Store custom properties in a NetworkedProperties component or static manager
//        var customProperties = new Dictionary<string, object>
//        {
//            { "UserName", staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name },
//            { "imgurl", staticVariables.UserProfiledata.user.file_url },
//            { "Prize", NetworkManager.mainPlayer.prize },
//            { "UserID", staticVariables.UserProfiledata.user._id.ToString() },
//            { "GameID", ApiAndRoomManager.currentGameId.ToString() },
//            { "isgoldcoins", staticVariables.isgoldcoins },
//            { "PlayerTTL", 60000 }
//        };

//        // Store properties in session data manager
//        SessionDataManager.SetSessionProperties(customProperties);

//        return sessionArgs;
//    }

//    private bool ShouldJoinSession()
//    {
//        bool connectionReady = networkRunner != null && !networkRunner.IsShutdown && networkRunner.SessionInfo == null;
//        string connectionStatus = $"is connected {networkRunner?.IsConnectedToServer} " +
//                                    $"has session: {networkRunner?.SessionInfo != null} ";
//        connectionStatus.Show();
//        return connectionReady;
//    }

//    private void JoinGameRoom(PlayerProfile player)
//    {

//        PlayerPrefs.Save();
//        StartCoroutine(JoinFusionCoroutine(player));
//    }

//    private IEnumerator JoinFusionCoroutine(PlayerProfile player)
//    {
//        var sceneInfo = new NetworkSceneInfo();
//        sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));
//        var sessionArgs = new StartGameArgs()
//        {
//            GameMode = GameMode.Client,
//            SessionName = player.roomIds,
//            Scene = sceneInfo
//        };

//        if (networkRunner == null)
//        {
//            networkRunner = FindAnyObjectByType<NetworkRunner>();
//        }

//        // Start the game and wait for completion
//        var startGameTask = networkRunner.StartGame(sessionArgs);

//        // Wait until the task is completed
//        yield return new WaitUntil(() => startGameTask.IsCompleted);

//        // Get the result
//        var result = startGameTask.Result;
//        result.StackTrace.Show("Result : ");

//        if (result.Ok)
//        {
//            HandleRoomJoined?.Invoke(networkRunner.SessionInfo);
//            if (Object == null || !Object.IsValid)
//            {
//                Debug.LogError("NetworkObject not initialized!");
//            }
//            //OnStartGameWithPlayer(staticVariables.OpponetProfile);
//            OnJoinedSession();
//        }
//    }
//    public override void Spawned()
//    {
//        Debug.Log("FusionNetwork spawned/initialized!");
//    }
//    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true)]
//    public void RPC_loadSnookerfusion()
//    {

//        Debug.Log("Fusion Scene RPC");
//        SnokerNetwork.IsMultiplayer = true;

//        SceneManager.LoadScene("SnokkerMultiplayer");
//    }
//    private OnJoinnedRoomDelegate GetRoomJoinHandler(string gameId)
//    {
//        gameId.Show("Game ID in Fusion Network");
//        var handlers = new Dictionary<string, OnJoinnedRoomDelegate>
//        {
//            { "1", OnEnter8BallRoom },
//            { "4", OnEnterCarromRoom },
//            { "6", OnEnterSnakeRoom },
//            { "7", OnEnterTwelveBeadRoom },
//            { "11", OnEnterHorseMultiplayerRoom },
//            { "12", OnEnterCarRoom },
//            { "13", OnEnterCricketRoom },
//            { "14", OnEnterHighwayRacingRoom },
//            { "15", OnEnterSnookerFusion }
//        };

//        return handlers.TryGetValue(gameId, out var handler) ? handler : null;
//    }
//    #endregion

//    #region Game Room Handlers

//    void OnEnter8BallRoom(SessionInfo session)
//    {
//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            ApiAndRoomManager._instance.StartChallenge();
//            SessionDataManager.SetSessionClosed(session.Name);
//            NetworkManager.mainPlayer.imageURL = staticVariables.UserProfiledata.user.file_url;
//            NetworkManager.mainPlayer.playerTTL = "12000";
//            NetworkManager.mainPlayer.roomIds = ApiAndRoomManager._instance.challenge_transaction_id;
//            RPC_OnOpponenReadToPlay(NetworkManager.PlayerToStringRoom(NetworkManager.mainPlayer), AightBallPoolNetworkGameAdapter.is3DGraphics);
//        }
//    }

//    void OnEnterCarromRoom(SessionInfo session)
//    {
//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            ApiAndRoomManager._instance.StartChallenge();
//            SessionDataManager.SetSessionClosed(session.Name);

//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }

//            RPC_LoadScene("CarromOffline");
//        }
//    }

//    void OnEnterSnakeRoom(SessionInfo session)
//    {
//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            ApiAndRoomManager._instance.StartChallenge();
//            SessionDataManager.SetSessionClosed(session.Name);
//            Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;

//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }
//            RPC_loadGamePlayRpcSnake("OnlineGameScene");
//        }
//    }

//    void OnEnterHighwayRacingRoom(SessionInfo session)
//    {
//        // Set player as ready (store in NetworkedProperties or local state)
//        PlayerDataManager.SetPlayerReady(networkRunner.LocalPlayer, true);

//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            ApiAndRoomManager._instance.StartChallenge();
//            SessionDataManager.SetSessionClosed(session.Name);

//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }

//            session.Name.Show("Session Name");
//            HR_MainMenuHandler.Instance.SelectMode(0);
//            PlayerPrefs.SetInt("Multiplayer", 1);
//            //Photon Removal HR_PhotonHandler photonHandler = HR_PhotonHandler.Instance;
//            //Photon Removal  if (!photonHandler)
//            Instantiate(Resources.Load("HR_Photon Handler", typeof(GameObject)));

//            RPC_loadHighwayRacing();
//        }
//    }

//    void OnEnterSnookerFusion(SessionInfo session)
//    {
//        session.Show("Session");
//        networkRunner.ActivePlayers.Show("ActivePlayers");
//        SnokerNetwork.IsMultiplayer = true;

//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            ApiAndRoomManager._instance.StartChallenge();
//            NetworkManager.mainPlayer.imageURL = staticVariables.UserProfiledata.user.file_url;
//            NetworkManager.mainPlayer.roomIds = ApiAndRoomManager._instance.challenge_transaction_id;

//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }
//            Debug.Log("Calling rpc fussion");

//            // networkRunner.LoadScene("SnokkerMultiplayer");
//            //RPC_loadSnookerfusion();

//        }
//    }


//    void OnEnterCricketRoom(SessionInfo session)
//    {
//        GameConstants.isWithAI = false;

//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            ApiAndRoomManager._instance.StartChallenge();
//            SessionDataManager.SetSessionClosed(session.Name);

//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }

//            // Handle cricket overs settings through NetworkedProperties
//            CONTROLLER.oversSelectedIndex = SessionDataManager.GetTotalOvers();
//            SelectOver.SelectedOver = CONTROLLER.oversSelectedIndex;
//            CONTROLLER.totalOvers = CONTROLLER.Overs[SessionDataManager.GetTotalOvers()];

//            RPC_LoadCricketScene();
//        }
//    }

//    void OnEnterTwelveBeadRoom(SessionInfo session)
//    {
//        ApiAndRoomManager._instance.StartChallenge();

//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            SessionDataManager.SetSessionClosed(session.Name);
//            Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;

//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }
//            RPC_Load12BeadScene("12OnlineGameScene");
//        }
//    }

//    public void OnEnterHorseAIRoom(SessionInfo session)
//    {
//        CarRace.PlayerSelections.GameMode = 1;
//        PlayerDataManager.SetPlayerLoadedLevel(networkRunner.LocalPlayer, false);

//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }
//        }

//        ApiAndRoomManager._instance.addressablesManager.LoadAddressableScene(AddressablesBundleSpawning.instance.horseAddressableSceneName);
//        MConstants.isRaceOver = false;
//    }

//    public void OnEnterCarRoom(SessionInfo session)
//    {
//        CarRace.PlayerSelections.GameMode = 1;
//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            ApiAndRoomManager._instance.StartChallenge();

//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }
//            SessionDataManager.SetSessionOpen(session.Name, true);
//            RPC_loadCarScene();
//        }
//    }

//    public void OnEnterHorseMultiplayerRoom(SessionInfo session)
//    {
//        PlayerDataManager.SetPlayerLoadedLevel(networkRunner.LocalPlayer, false);

//        if (networkRunner.ActivePlayers.Count() > 1)
//        {
//            ApiAndRoomManager._instance.StartChallenge();
//            foreach (PlayerRef player in networkRunner.ActivePlayers)
//            {
//                if (player != networkRunner.LocalPlayer)
//                {
//                    opponent = player;
//                }
//            }
//            SessionDataManager.SetSessionOpen(session.Name, true);
//            RPC_HorseSceneLoad();
//        }
//    }

//    #endregion

//    #region Fusion RPCs (converted from PUN RPCs)

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    public void RPC_loadHighwayRacing()
//    {
//        PlayerPrefs.SetInt("Multiplayer", 1);
//        SceneManager.LoadScene("HighwayNight");
//    }


//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    public void RPC_loadCarScene()
//    {
//        CarRace.PlayerSelections.GameMode = 1;
//        ApiAndRoomManager._instance.addressablesManager.LoadAddressableScene(AddressablesBundleSpawning.instance.carAddressableSceneName);
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    public void RPC_HorseSceneLoad()
//    {
//        MultiPlayerGame.isSinglePlayer = false;
//        MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;
//        MConstants.isRaceOver = false;
//        ApiAndRoomManager._instance.addressablesManager.LoadAddressableScene(AddressablesBundleSpawning.instance.horseAddressableSceneName);
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    public void RPC_LoadScene(string scenename)
//    {
//        Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;
//        BEKStudio.GameManager.Instance.currentGameMode = BEKStudio.GameManager.GameMode.AgainstFriend;

//        // Set player properties through NetworkedProperties or local state
//        if (Object.HasStateAuthority)
//        {
//            PlayerDataManager.SetPlayerTag(networkRunner.LocalPlayer, "White");
//        }
//        else
//        {
//            PlayerDataManager.SetPlayerTag(networkRunner.LocalPlayer, "Black");
//        }

//        SceneManager.LoadScene(scenename);
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    public void RPC_Load12BeadScene(string scenename)
//    {
//        Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;
//        BEKStudio.GameManager.Instance.currentGameMode = BEKStudio.GameManager.GameMode.AgainstFriend;

//        if (Object.HasStateAuthority)
//        {
//            PlayerDataManager.SetPlayerTag(networkRunner.LocalPlayer, "White");
//        }
//        else
//        {
//            PlayerDataManager.SetPlayerTag(networkRunner.LocalPlayer, "Black");
//        }

//        AddressablesBundleSpawning.instance.LoadAddressableScene(AddressablesBundleSpawning.instance.twelveOnlineAddressableSceneName);
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    public void RPC_LoadCricketScene()
//    {
//        AddressablesBundleSpawning.instance.LoadAddressableScene(AddressablesBundleSpawning.instance.cricketMenuAddressableSceneName);
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    public void RPC_loadGamePlayRpcSnake(string sceneName)
//    {
//        SceneManager.LoadScene(sceneName);
//    }

//    #endregion

//    #region State Authority Management (replaces Master Client)

//    public void OnStateAuthorityChanged(PlayerRef previousAuthority, PlayerRef newAuthority)
//    {
//        if (ApiAndRoomManager.currentGameId == 4)
//        {
//            string newHostTag = PlayerDataManager.GetPlayerTag(newAuthority);
//            BEKStudio.GameController.Instance.hostTag = newHostTag;

//            if (BEKStudio.GameController.Instance.activePlayer == BEKStudio.GameController.WhichPlayer.ME)
//            {
//                BEKStudio.GameController.Instance.activePlayer = BEKStudio.GameController.WhichPlayer.OTHER;
//            }
//            else
//            {
//                BEKStudio.GameController.Instance.activePlayer = BEKStudio.GameController.WhichPlayer.ME;
//            }
//            BEKStudio.GameController.Instance.VerifyPlayerTurn();
//        }
//    }

//    #endregion

//    #region Network Engine Methods

//    public virtual void Disable()
//    {
//        // Cleanup
//    }

//    public virtual void SendRemoteMessage(string message, params object[] args)
//    {
//        // Convert to RPC call based on message name
//        // This would need specific implementation based on your message types
//    }

//    public void ToggleCueVisibility(bool activated)
//    {
//        RPC_SwitchCueState(activated);
//    }

//    [Rpc(RpcSources.All, RpcTargets.All)]
//    public void RPC_SwitchCueState(bool Isactive)
//    {
//        ShotController.shotControllerInstance.SwitchCue(Isactive);
//    }

//    [Rpc(RpcSources.All, RpcTargets.All)]
//    public void RPC_SwitchShadowMode(bool isactive)
//    {
//        ShotController.shotControllerInstance.mainCueBall.listener._collider.enabled = !isactive;
//        ShotController.shotControllerInstance.mainCueBall.listener.body.isKinematic = isactive;
//    }

//    public void CallingShadowToggleMethod(bool isActive)
//    {
//        RPC_SwitchShadowMode(isActive);
//    }

//    #endregion

//    #region TICK SOUND 

//    public void PlayTickSound(bool activated)
//    {
//        RPC_TickSfx(activated);
//    }

//    [Rpc(RpcSources.All, RpcTargets.All)]
//    public void RPC_TickSfx(bool activated)
//    {
//        CircularTimeController.instance.audioSource.gameObject.SetActive(activated);
//        CircularTimeController.instance.audioSource.clip = CircularTimeController.instance.timerClip;
//        CircularTimeController.instance.audioSource.loop = true;

//        if (activated)
//        {
//            CircularTimeController.instance.audioSource.Play();
//            CircularTimeController.instance.InitializeBallParticles();
//        }
//        else
//        {
//            CircularTimeController.instance.ToggleBallGlowEffects(false);
//            CircularTimeController.instance.audioSource.Stop();
//        }
//    }

//    #endregion

//    #region Session Management (replaces Room Management)

//    public virtual void OnJoinedSession()
//    {
//        //OnServerDisconnected?.Invoke(false, "");
//        //currentState = ConnectionState.Connected;
//        //OnAwaitingOpponent?.Invoke(false);
//        //CheckGameStatusOnJoin();
//        //if (isGameActive) return;
//        isPaused = false;
//        isGameBegun = true;
//        HasWinnerBeenDeclared = false;
//        HandleRoomJoined?.Invoke(networkRunner.SessionInfo);
//        staticVariables.currentRoomName = networkRunner.SessionInfo.Name;
//        HandleRoomJoined = null;
//        staticVariables.isfromreferllinks = false;

//        // Clean up notification popups
//        GameObject[] objectsToDestroy = GameObject.FindGameObjectsWithTag("AcceptNotificationPopUp");
//        if (objectsToDestroy.Length > 0)
//        {
//            foreach (GameObject obj in objectsToDestroy)
//            {
//                Destroy(obj);
//            }
//        }

//        if (GameManager.instance != null)
//        {
//            GameManager.instance.ConnectionReset();
//            staticVariables.Isrejoining = false;
//        }

//        CallNetworkState(NetworkState.JoinedToRoom);
//        if (staticVariables.OpponetProfile != null && staticVariables.OpponetProfile.userId != "")
//            staticVariables.lastOpponentId = staticVariables.OpponetProfile.userId;
//    }

 
//    #endregion

//    #region Reconnecting (adapted for Fusion)

//    public bool Isreconnectingvar = false;
//    public bool isgameInBg = false;
//    public bool IsInGame = false;
//    public DateTime PlayerGameTime, OpponentGameTime, GameBeginningTime;

    
   

   

//    #endregion

   

  
   
//    public void VerifyConnection()
//    {
//        if (networkRunner != null && networkRunner.IsConnectedToServer)
//        {
//            Debug.Log("Fusion Network State: Connected and ready");
//        }
//        else
//        {
//            Connect();
//        }
//    }

//    public virtual void OnConnected()
//    {
//        CallNetworkState(NetworkState.Connected);
//        PopupMessageManager.instance.ReconnectionPanelStatus(false);
//    }

//    public bool IsAIControlledHorse;



//    public async void CreateSession(StartGameArgs sessionArgs)
//    {       
//        if (networkRunner != null && !networkRunner.IsShutdown)
//        {
//            ApiAndRoomManager.currentGameId.Show("Game ID");
//            "On Create Session".Show();

//            var result = await networkRunner.StartGame(sessionArgs);

//            if (result.Ok)
//            {
//                OnCreatedSession();
//                HandleRoomCreated?.Invoke(networkRunner.SessionInfo);
//            }
//            else
//            {
//                OnCreateSessionFailed(result.ShutdownReason.ToString());
//            }
//        }
//    }
//    public void Reconnect()
//    {
//        StartCoroutine(ResetConnection());
//    }

//    private IEnumerator ResetConnection()
//    {
//        while (networkRunner != null && !networkRunner.IsShutdown)
//        {
//            yield return new WaitForSeconds(0.2f);
//        }

//        Connect();
//        PopupMessageManager.instance.ReconnectionPanelStatus(false);
//    }

//    public virtual void Resset()
//    {
//        LeftRoom();
//    }

//    public virtual void LeftRoom()
//    {
//        if (networkRunner != null && networkRunner.SessionInfo != null)
//        {
//            ConstantsData_M.Log("Leaving session: " + networkRunner.SessionInfo.Name);
//            _ = networkRunner.Shutdown();
//            Physics.simulationMode = SimulationMode.FixedUpdate;
//        }
//    }

//    public virtual void Connect()
//    {
//        if (networkRunner == null)
//        {
//            GameObject runnerObj = new GameObject("FusionNetworkRunner");
//            networkRunner = runnerObj.AddComponent<NetworkRunner>();
//            DontDestroyOnLoad(runnerObj);
//        }

//        // Connection will happen when starting a session
//    }

//    public virtual void Disconnect()
//    {
//        if (networkRunner != null && !networkRunner.IsShutdown)
//        {
//            _ = networkRunner.Shutdown(false);
//        }
//    }

//    public virtual void OnJoinedLobby()
//    {
//        if (HandleLobbyJoined != null)
//        {
//            HandleLobbyJoined();
//        }
//    }

//    public virtual void OnConnectedToMaster()
//    {
//        Debug.Log("Ready to join sessions");
//        OnConnected();

//        if (HandleLobbyJoined != null)
//        {
//            HandleLobbyJoined();
//        }
//    }

//    public virtual void OnCreateSessionFailed(string message)
//    {
//        ConstantsData_M.Log("Session creation failed: " + message);
//    }

//    public virtual void OnCreatedSession()
//    {
//        HandleRoomCreated?.Invoke(networkRunner.SessionInfo);
//        CallNetworkState(NetworkState.CreatedRoom);
//        HandleRoomCreated = null;
//    }

//    public virtual void OnLeftSession()
//    {
//        opponent = default;
//        CallNetworkState(NetworkState.LeftRoom);
//        ApiAndRoomManager._instance.ModifyUserBalance();
//    }

//    public List<SessionInfo> SessionCollection = new List<SessionInfo>();
//    public bool isPaused = false;

//    public void CallPauseStateRPC(bool isPaused)
//    {
//        RPC_SetPauseState(isPaused);
//    }

//    [Rpc(RpcSources.All, RpcTargets.All)]
//    private void RPC_SetPauseState(bool value)
//    {
//        isPaused = value;
//        ConstantsData_M.Log("Pause state set: " + value);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    private void RPC_VerifyOpponentConnection(string value)
//    {
//        DateTime parsedatetime = DateTime.Now;
//        string[] possibleFormats = new string[]
//        {
//            "yyyy-MM-dd'T'HH:mm:ss.fffffff",
//            "yyyy-MM-dd'T'HH:mm:ss.ffffff",
//        };

//        foreach (string format in possibleFormats)
//        {
//            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedatetime))
//            {
//                OpponentGameTime = DateTime.ParseExact(value, format, CultureInfo.InvariantCulture);
//                break;
//            }
//        }

//        if (GameManager.instance)
//            GameManager.instance?.waitingForOpponentPanel.SetActive(false);
//    }

//    public void CheckOpponentStatus(string value)
//    {
//        RPC_VerifyOpponentConnection(value);
//    }

//    // Placeholder for NetworkState enum and CallNetworkState method
//    public enum NetworkState
//    {
//        Connected,
//        CreatedRoom,
//        JoinedToRoom,
//        LeftRoom
//    }

//    public virtual void CallNetworkState(NetworkState state)
//    {
//        Debug.Log($"Network State: {state}");
//    }

//    #region 8Ball Pool RPCs

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public virtual void RPC_OnOpponenReadToPlay(string playerData, bool is3DGraphicMode)
//    {
//        NetworkManager.opponentPlayer = NetworkManager.PlayerFromStringRoom(playerData);
//        AightBallPoolNetworkGameAdapter.isSameGraphicsMode = AightBallPoolNetworkGameAdapter.is3DGraphics == is3DGraphicMode;

//#if UNITY_ANDROID || UNITY_IOS
//        Handheld.Vibrate();
//#endif
//        OnStartGameWithPlayer(staticVariables.OpponetProfile, false);
//        int turnId = UnityEngine.Random.Range(0, 2);
//        int turnIdForSend = turnId == 1 ? 0 : 1;
//        RPC_OnOpponenStartToPlay(turnId);
//        RPC_OnOpponenStartToPlay(turnIdForSend);
//    }

//    [Rpc(RpcSources.All, RpcTargets.All)]
//    public virtual void RPC_OnOpponenStartToPlay(int turnId)
//    {
//        //adapter.SetTurn(turnId);
//        SceneLoaderUtility.LoadScene("AightBallPool");
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public virtual void RPC_OnSendTime(float time01)
//    {
//        poolMessenger.SetTime(time01);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public virtual void RPC_StartSimulate(string impulse)
//    {
//        poolMessenger.StartSimulate(impulse);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public virtual void RPC_EndSimulate(string ballsState)
//    {
//        poolMessenger.EndSimulate(ballsState);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public virtual void RPC_OnOpponenWaitingForYourTurn()
//    {
//        // Base implementation
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public virtual void RPC_OnOpponenInGameScene()
//    {
//        StartCoroutine(poolMessenger.OnOpponenInGameScene());
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public virtual void RPC_OnOpponentForceGoHome()
//    {
//        poolMessenger.OnOpponentForceGoHome();
//    }

//    #region AightBallPool Interface

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_OnSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
//    {
//        poolMessenger.OnSendCueControl(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_OnForceSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
//    {
//        poolMessenger.OnForceSendCueControl(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_OnMoveBall(Vector3 ballPosition)
//    {
//        poolMessenger.OnMoveBall(ballPosition);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_SelectBallPosition(Vector3 ballPosition)
//    {
//        poolMessenger.SelectBallPosition(ballPosition);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_SetBallPosition(Vector3 ballPosition)
//    {
//        poolMessenger.SetBallPosition(ballPosition);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_SetMechanicalStatesFromNetwork(int ballId, string mechanicalStateData)
//    {
//        poolMessenger.SetMechanicalStatesFromNetwork(ballId, mechanicalStateData);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_WaitAndStopMoveFromNetwork(float time)
//    {
//        poolMessenger.WaitAndStopMoveFromNetwork(time);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_SendOpponentCueURL(string url)
//    {
//        poolMessenger.SetOpponentCueURL(url);
//    }

//    [Rpc(RpcSources.All, RpcTargets.Proxies)]
//    public void RPC_SendOpponentTableURLs(string boardURL, string clothURL, string clothColor)
//    {
//        poolMessenger.SetOpponentTableURLs(boardURL, clothURL, clothColor);
//    }

//    #endregion
//    #endregion

//}

//// Helper classes to manage session data (since Fusion doesn't have built-in custom properties)
//public static class SessionDataManager
//{
//    private static Dictionary<string, Dictionary<string, object>> sessionProperties = new Dictionary<string, Dictionary<string, object>>();
//    private static Dictionary<string, bool> sessionStates = new Dictionary<string, bool>();

//    public static void SetSessionProperties(Dictionary<string, object> properties)
//    {
//        // Store session properties
//    }

//    public static void SetSessionClosed(string sessionName)
//    {
//        sessionStates[sessionName] = false;
//    }

//    public static void SetSessionOpen(string sessionName, bool isOpen)
//    {
//        sessionStates[sessionName] = isOpen;
//    }

//    public static int GetTotalOvers()
//    {
//        // Return stored overs value or default
//        return 0;
//    }

//    public static bool HasWinnerBeenDeclared()
//    {
//        // Check if game has ended
//        return false;
//    }

//    public static void SetGameEnded(string reason, bool localPlayerWon)
//    {
//        // Store game end data
//    }

//    public static string GetGameEndReason()
//    {
//        return "Game ended";
//    }

//    public static bool WasLocalPlayerWinner(PlayerRef localPlayer)
//    {
//        return false;
//    }
//}

//public static class PlayerDataManager
//{
//    private static Dictionary<PlayerRef, Dictionary<string, object>> playerProperties = new Dictionary<PlayerRef, Dictionary<string, object>>();

//    public static void SetPlayerReady(PlayerRef player, bool ready)
//    {
//        if (!playerProperties.ContainsKey(player))
//            playerProperties[player] = new Dictionary<string, object>();
//        playerProperties[player]["Ready"] = ready;
//    }

//    public static void SetPlayerLoadedLevel(PlayerRef player, bool loaded)
//    {
//        if (!playerProperties.ContainsKey(player))
//            playerProperties[player] = new Dictionary<string, object>();
//        playerProperties[player]["LoadedLevel"] = loaded;
//    }

//    public static void SetPlayerTag(PlayerRef player, string tag)
//    {
//        if (!playerProperties.ContainsKey(player))
//            playerProperties[player] = new Dictionary<string, object>();
//        playerProperties[player]["tag"] = tag;
//    }

//    public static string GetPlayerTag(PlayerRef player)
//    {
//        if (playerProperties.ContainsKey(player) && playerProperties[player].ContainsKey("tag"))
//            return (string)playerProperties[player]["tag"];
//        return "";
//    }

//    public static void SetPlayerMaster(PlayerRef player, bool isMaster)
//    {
//        if (!playerProperties.ContainsKey(player))
//            playerProperties[player] = new Dictionary<string, object>();
//        playerProperties[player]["IsMaster"] = isMaster;
//    }
//}
