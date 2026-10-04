#if PHOTON_UNITY_NETWORKINGf
using AYellowpaper.SerializedCollections;
using BallPool;
using Cysharp.Threading.Tasks.Triggers;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using NaughtyAttributes;
using NetworkManagement;
 
using Photon.Pun.Demo.Asteroids;
 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class PunNetwork : NetworkEngine, AightBallPoolMessenger
{
    public PhotonView punView;
    public Player opponent;
    private PlayerRef opponentfusion;

    private AightBallPoolNetworkMessenger poolMessenger;
    public delegate void OnLobbyJoinedDelegate();
    public static event OnLobbyJoinedDelegate HandleLobbyJoined;
    public delegate void OnJoinnedRoomDelegate(Photon.Realtime.Room room);
    public static OnJoinnedRoomDelegate HandleRoomJoined;
    public static OnJoinnedRoomDelegate HandleRoomCreated;
    public static PunNetwork instance;
    public bool isGameBegun = false;
    public bool HasWinnerBeenDeclared = false;


    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            PhotonNetwork.AddCallbackTarget(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public override void Initialize()
    {
        if (!poolMessenger)
        {
            poolMessenger = gameObject.AddComponent<AightBallPoolNetworkMessenger>();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        sendRate = 30;
        PhotonNetwork.SendRate = sendRate;
        PhotonNetwork.SerializationRate = sendRate;
        punView = gameObject.AddComponent<PhotonView>();
        punView.ObservedComponents = new List<Component>(0);
        punView.ObservedComponents.Add(this);
        punView.Synchronization = ViewSynchronization.Off;
        punView.OwnershipTransfer = OwnershipOption.Fixed;
        punView.ViewID = 101;
        PhotonNetwork.KeepAliveInBackground = 10;
        var peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
        peer.DisconnectTimeout = 10000;
        peer.TimePingInterval = 1000;
       // Connect();
        PhotonNetwork.GetPing();
        PhotonNetwork.NetworkingClient.EventReceived += OnMinimalHeartbeatEvent;

    }
    #region Game Joining
    public void OnStartGameWithPlayer(PlayerProfile player, bool mustrpc = false)
    {
        // Initialize common properties
        PunNetwork.instance.IsAIControlledHorse = false;
        PhotonNetwork.AutomaticallySyncScene = false;
        staticVariables.isgoldcoins = player.isgoldcoins;
        staticVariables.currentPrize = player.prize;
        staticVariables.currentPrize.Show("Current Prize");
        staticVariables.isgoldcoins.Show("isgoldcoins");
        PhotonNetwork.NickName = staticVariables.userNickName;
        NetworkManager.opponentPlayer = player;

        InitializeGameSpecificSettings(player);
        //if (ShouldJoinRoom())
        {
            JoinGameRoom(player);
        }

    }

    public void InitializeGameSpecificSettings(PlayerProfile player)
    {
        switch (player.gameId)
        {
            case "1": // Ball Pool
                BallPoolGameLogic.playMode = BallPool.PlayMode.OnLine;
                BallPoolPlayer.players[0].SetCoins(NetworkManager.mainPlayer.coins);
                BallPoolPlayer.players[1] = new AightBallPoolPlayer(
                    int.Parse(player.userId),
                    player.userName,
                    player.coins,
                    player.image,
                    player.imageURL);
                break;

            case "11": // Horse Racing
                MultiPlayerGame.isTimeTrial = false;
                MultiPlayerGame.isLastManStandingMode = false;
                MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;
                MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playWithFriend;
                MultiPlayerGame.isSinglePlayer = false;

                string playerName = MultiPlayerGame.getPlayerName();
                if (!string.IsNullOrEmpty(playerName))
                {
                    PhotonNetwork.LocalPlayer.NickName = playerName;
                }
                else
                {
                    ConstantsData_M.Log("Player Name is invalid.");
                }
                break;

            case "15": // Snooker
                GamesPandaLauncher.RoomName = "";
                GamesPandaLauncher.SceneToLoad = "SnokkerMultiplayer";
                GamesPandaLauncher.IsMaster = true;
                break;
        }
    }
    public static RoomOptions ConfigureRoomSettings(bool IsVisible)
    {
        int emptyRoomTime = ApiAndRoomManager._instance.currentBetData.data.challenge_time_minutes * 60 * 1000;
        int playerTTL = 0;

        RoomOptions _RoomOptions = new RoomOptions
        {
            IsVisible = IsVisible,
            IsOpen = true,
            MaxPlayers = 2,
            EmptyRoomTtl = emptyRoomTime,
            PlayerTtl = playerTTL,
        };
        string userName = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;
        string gameId = ApiAndRoomManager.currentGameId.ToString();
        string userId = staticVariables.UserProfiledata.user._id.ToString();
        bool isgoldcoins = staticVariables.isgoldcoins;
        _RoomOptions.CustomRoomProperties = new Hashtable
        {
            { "UserName", userName},
            { "imgurl", staticVariables.UserProfiledata.user.file_url },
            { "Prize", NetworkManager.mainPlayer.prize },
            { "UserID", userId },
            { "GameID", gameId },
            { "isgoldcoins", isgoldcoins },
            { "PlayerTTL", 60000 },
            //{"BetAmount",staticVariables.currentPrize },
        };

        string[] _CustomLobbyProperties = new string[7];
        _CustomLobbyProperties[0] = "UserName";
        _CustomLobbyProperties[1] = "imgurl";
        _CustomLobbyProperties[2] = "Prize";
        _CustomLobbyProperties[3] = "UserID";
        _CustomLobbyProperties[4] = "GameID";
        _CustomLobbyProperties[5] = "isgoldcoins";
        _CustomLobbyProperties[6] = "PlayerTTL";
        //_CustomLobbyProperties[7] = "BetAmount";

        _RoomOptions.CustomRoomPropertiesForLobby = _CustomLobbyProperties;

        return _RoomOptions;
    }
    private bool ShouldJoinRoom()
    {
        bool connectionReady = PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom;
        string connectionStatus = $"is connected {PhotonNetwork.IsConnected} " +
                                    $"is connected n ready: {PhotonNetwork.IsConnectedAndReady} " +
                                    $"in room: {PhotonNetwork.InRoom} " +
                                    $"in lobby: {PhotonNetwork.InLobby} Cricket not Connected";
        connectionStatus.Show();
        return connectionReady;
    }

    private void JoinGameRoom(PlayerProfile player)
    {
        player.prize.Show("Prize in PunNetwork");
        player.roomIds.Show("roomIds");
        player.gameId.Show("gameId");
        PhotonNetwork.NetworkClientState.Show("Network State");
        // Update prize status for all games
        adapter.homeMenuManager.UpdatePrizeStatus(player.prize);
        PlayerPrefs.Save();
        HandleRoomJoined = GetRoomJoinHandler(player.gameId);
        PhotonNetwork.JoinRoom(player.roomIds);
    }

    private OnJoinnedRoomDelegate GetRoomJoinHandler(string gameId)
    {
        var handlers = new Dictionary<string, OnJoinnedRoomDelegate>
    {
        { "1", OnEnter8BallRoom },
        { "4", OnEnterCarromRoom },
        { "6", OnEnterSnakeRoom },
        { "7", OnEnterTwelveBeadRoom },
        { "11", OnEnterHorseMultiplayerRoom },
        { "12", OnEnterCarRoom },
        { "13", OnEnterCricketRoom },
        { "14", OnEnterHighwayRacingRoom },
        { "15", OnEnterSnooker }
        };
        return handlers.TryGetValue(gameId, out var handler) ? handler : null;
    }
    void OnEnter8BallRoom(Photon.Realtime.Room room)
    {
        if (room.PlayerCount > 1)
        {
            ApiAndRoomManager._instance.StartChallenge();
            room.IsOpen = false;
            room.IsVisible = false;
            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            NetworkManager.mainPlayer.imageURL = staticVariables.UserProfiledata.user.file_url;
            NetworkManager.mainPlayer.playerTTL = "12000";
            NetworkManager.mainPlayer.roomIds = ApiAndRoomManager._instance.challenge_transaction_id;
            punView.RPC("OnOpponenReadToPlay", RpcTarget.Others, NetworkManager.PlayerToStringRoom(NetworkManager.mainPlayer), AightBallPoolNetworkGameAdapter.is3DGraphics);
        }
    }
    void OnEnterCarromRoom(Photon.Realtime.Room room)
    {

        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            ApiAndRoomManager._instance.StartChallenge();
            room.IsOpen = false;
            room.IsVisible = false;
            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }

            punView.RPC("LoadScene", RpcTarget.All, "CarromOffline");
        }
    }
    void OnEnterSnakeRoom(Photon.Realtime.Room room)
    {
        Hashtable props = new Hashtable
            {
            {"StartTime", (int)PhotonNetwork.ServerTimestamp}
            };
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            ApiAndRoomManager._instance.StartChallenge();
            room.IsOpen = false;
            room.IsVisible = false;
            Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;

            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }
            punView.RPC("loadGamePlayRpcSnake", RpcTarget.All, "OnlineGameScene");
        }
    }
    void OnEnterHighwayRacingRoom(Photon.Realtime.Room room)
    {
        Hashtable props = new Hashtable
            {
            {"StartTime", (int)PhotonNetwork.ServerTimestamp}
            };

        Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
        hash["Ready"] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        Hashtable roomhash = PhotonNetwork.CurrentRoom.CustomProperties;
        PlayerPrefs.SetString("SelectedScene", (string)roomhash["scene"]);
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            ApiAndRoomManager._instance.StartChallenge();
            room.IsOpen = false;
            room.IsVisible = false;

            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }

            PhotonNetwork.CurrentRoom.Name.Show("Room ka Name");
            HR_MainMenuHandler.Instance.SelectMode(0);
            PlayerPrefs.SetInt("Multiplayer", 1);
            HR_PhotonHandler photonHandler = HR_PhotonHandler.Instance;
            if (!photonHandler)
                Instantiate(Resources.Load("HR_Photon Handler", typeof(GameObject)));

            punView.RPC("loadHighwayRacing", RpcTarget.All);
            //  loadHighwayRacing();
        }
    }
    [PunRPC]
    public void loadHighwayRacing()
    {
        PlayerPrefs.SetInt("Multiplayer", 1);
        SceneManager.LoadScene("HighwayNight");
    }

    void OnEnterSnooker(Photon.Realtime.Room room)
    {
        Hashtable props = new Hashtable
            {
            {"StartTime", (int)PhotonNetwork.ServerTimestamp}
            };

        Hashtable roomhash = PhotonNetwork.CurrentRoom.CustomProperties;
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            ApiAndRoomManager._instance.StartChallenge();
            room.IsOpen = false;
            room.IsVisible = false;
            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            NetworkManager.mainPlayer.imageURL = staticVariables.UserProfiledata.user.file_url;
            NetworkManager.mainPlayer.roomIds = ApiAndRoomManager._instance.challenge_transaction_id;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }
            punView.RPC("loadSnooker", RpcTarget.All);
        }
    }
    [PunRPC]
    public void loadSnooker()
    {
        PhotonNetwork.NickName = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;
        SnokerNetwork.IsMultiplayer = true;
        SceneManager.LoadScene("SnokkerMultiplayer");
    }
   
    void OnEnterCricketRoom(Photon.Realtime.Room room)
    {
        GameConstants.isWithAI = false;
        Hashtable props = new Hashtable
            {
            {"StartTime", (int)PhotonNetwork.ServerTimestamp}
            };
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            ApiAndRoomManager._instance.StartChallenge();
            room.IsOpen = false;
            room.IsVisible = false;

            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }
            if (PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("totalOvers"))
                {
                    CONTROLLER.oversSelectedIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["totalOvers"];
                    SelectOver.SelectedOver = CONTROLLER.oversSelectedIndex;
                    CONTROLLER.totalOvers = CONTROLLER.Overs[(int)PhotonNetwork.CurrentRoom.CustomProperties["totalOvers"]];
                }
                else
                {
                }
            }
            punView.RPC("LoadCricketScene", RpcTarget.All);
        }
    }
    void OnEnterTwelveBeadRoom(Photon.Realtime.Room room)
    {
        ApiAndRoomManager._instance.StartChallenge();

        Hashtable props = new Hashtable
            {
            {"StartTime", (int)PhotonNetwork.ServerTimestamp}
            };

        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            room.SetCustomProperties(props);
            room.IsOpen = false;
            room.IsVisible = false;
            Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;

            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }
            punView.RPC("Load12BeadScene", RpcTarget.All, "12OnlineGameScene");
        }
    }
    public void OnEnterHorseAIRoom(Photon.Realtime.Room room)
    {
        Hashtable props = new Hashtable
            {
                {AsteroidsGame.PLAYER_LOADED_LEVEL, false},
             {"StartTime", (int)PhotonNetwork.ServerTimestamp}
            };
        CarRace.PlayerSelections.GameMode = 1;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            // PhotonNetwork.LoadLevel("Map2");
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }
            room.IsOpen = true;
            room.IsVisible = true;
            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
        }

        ApiAndRoomManager._instance.addressablesManager.LoadAddressableScene(AddressablesBundleSpawning.instance.horseAddressableSceneName);
        //PhotonNetwork.LoadLevel("Racecourse_Track_Final_Mud 16_New");
        MConstants.isRaceOver = false;

    }
    public void OnEnterCarRoom(Photon.Realtime.Room room)
    {
        CarRace.PlayerSelections.GameMode = 1;
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            ApiAndRoomManager._instance.StartChallenge();
            // PhotonNetwork.LoadLevel("Map2");
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }
            room.IsOpen = true;
            room.IsVisible = true;
            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            punView.RPC("loadCarScene", RpcTarget.All);
        }


        //view.RPC("loadGamePlayRpcCar", RpcTarget.AllBuffered);
    }
    [PunRPC]
    public void loadCarScene()
    {
        CarRace.PlayerSelections.GameMode = 1;
        ApiAndRoomManager._instance.addressablesManager.LoadAddressableScene(AddressablesBundleSpawning.instance.carAddressableSceneName);

    }

    public void OnEnterHorseMultiplayerRoom(Photon.Realtime.Room room)
    {

        Hashtable props = new Hashtable
            {
                {AsteroidsGame.PLAYER_LOADED_LEVEL, false},
            {"StartTime", (int)PhotonNetwork.ServerTimestamp}
            };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            ApiAndRoomManager._instance.StartChallenge();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    opponent = player;
                }
            }
            room.IsOpen = true;
            room.IsVisible = true;
            RoomCollection.Remove(RoomCollection.Find(x => x.Name == room.Name));
            punView.RPC("HorseSceneLoad", RpcTarget.All);
        }
    }
    [PunRPC]
    public void HorseSceneLoad()
    {
        MultiPlayerGame.isSinglePlayer = false;
        MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;
        MConstants.isRaceOver = false;
        ApiAndRoomManager._instance.addressablesManager.LoadAddressableScene(AddressablesBundleSpawning.instance.horseAddressableSceneName);
    }



    [PunRPC]
    public void LoadScene(string scenename)
    {
        Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;
        //  MatchHandler.CurrentMatch = MatchHandler.MATCH.Classic;
        BEKStudio.GameManager.Instance.currentGameMode = BEKStudio.GameManager.GameMode.AgainstFriend;
        ExitGames.Client.Photon.Hashtable userHastable = new ExitGames.Client.Photon.Hashtable();

        if (PhotonNetwork.IsMasterClient)
        {
            userHastable.Add("tag", "White");
        }
        else
        {
            userHastable.Add("tag", "Black");
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(userHastable);
        SceneManager.LoadScene(scenename);

    }

    [PunRPC]
    public void Load12BeadScene(string scenename)
    {
        Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;
        //  MatchHandler.CurrentMatch = MatchHandler.MATCH.Classic;
        BEKStudio.GameManager.Instance.currentGameMode = BEKStudio.GameManager.GameMode.AgainstFriend;
        ExitGames.Client.Photon.Hashtable userHastable = new ExitGames.Client.Photon.Hashtable();

        if (PhotonNetwork.IsMasterClient)
        {
            userHastable.Add("tag", "White");
        }
        else
        {
            userHastable.Add("tag", "Black");
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(userHastable);
        AddressablesBundleSpawning.instance.LoadAddressableScene(AddressablesBundleSpawning.instance.twelveOnlineAddressableSceneName);
    }
    [PunRPC]
    public void LoadCricketScene()
    {
        AddressablesBundleSpawning.instance.LoadAddressableScene(AddressablesBundleSpawning.instance.cricketMenuAddressableSceneName);
    }
    #endregion
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (ApiAndRoomManager.currentGameId == 4)
        {
            BEKStudio.GameController.Instance.hostTag = (string)newMasterClient.CustomProperties["tag"];
            if (BEKStudio.GameController.Instance.activePlayer == BEKStudio.GameController.WhichPlayer.ME)
            {
                BEKStudio.GameController.Instance.activePlayer = BEKStudio.GameController.WhichPlayer.OTHER;
            }
            else
            {
                BEKStudio.GameController.Instance.activePlayer = BEKStudio.GameController.WhichPlayer.ME;
            }
            BEKStudio.GameController.Instance.VerifyPlayerTurn();
        }
    }
    public override void Disable()
    {
        base.Disable();
    }

    public override void SendRemoteMessage(string message, params object[] args)
    {
        punView.RPC(message, opponent, args);
    }


    public void ToggleCueVisibility(bool activated)
    {
        punView.RPC("SwitchCueState", RpcTarget.All, activated);
    }
    [PunRPC]
    public void SwitchCueState(bool Isactive)
    {
        ShotController.shotControllerInstance.SwitchCue(Isactive);
    }

    [PunRPC]
    public void SwitchShadowMode(bool isactive)
    {

        ShotController.shotControllerInstance.mainCueBall.listener._collider.enabled = !isactive;
        ShotController.shotControllerInstance.mainCueBall.listener.body.isKinematic = isactive;
    }
    public void CallingShadowToggleMethod(bool isActive)
    {

        punView.RPC("SwitchShadowMode", RpcTarget.All, isActive);
    }

    #region TICK SOUND 

    public void PlayTickSound(bool activated)
    {
        punView.RPC("TickSfx", RpcTarget.All, activated);
    }
    [PunRPC]
    public void TickSfx(bool activated)
    {
        CircularTimeController.instance.audioSource.gameObject.SetActive(activated);
        CircularTimeController.instance.audioSource.clip = CircularTimeController.instance.timerClip;
        CircularTimeController.instance.audioSource.loop = true;


        if (activated)
        {
            CircularTimeController.instance.audioSource.Play();
            CircularTimeController.instance.InitializeBallParticles();
        }
        else
        {
            CircularTimeController.instance.ToggleBallGlowEffects(false);
            CircularTimeController.instance.audioSource.Stop();
        }
    }
    #endregion



    public override void OnJoinedRoom() //RAR
    {

        Hashtable playerProps = new Hashtable();
        // Save whether THIS player is master client

        OnServerDisconnected?.Invoke(false, "");
        currentState = ConnectionState.Connected;
        disconnectedPlayer = null;

        OnAwaitingOpponent?.Invoke(false);
        CheckGameStatusOnJoin();
        if (isGameActive) return;
        playerProps["IsMaster"] = PhotonNetwork.IsMasterClient;
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
        isPaused = false;
        isGameBegun = true;
        HasWinnerBeenDeclared = false;
        HandleRoomJoined?.Invoke(PhotonNetwork.CurrentRoom);
        staticVariables.currentRoomName = PhotonNetwork.CurrentRoom.Name;
        StartGame();
        HandleRoomJoined = null;
        staticVariables.isfromreferllinks = false;
        GameObject[] objectsToDestroy = GameObject.FindGameObjectsWithTag("AcceptNotificationPopUp");
        if (objectsToDestroy.Length > 0)
        {
            foreach (GameObject obj in objectsToDestroy)
            {
                Destroy(obj);
            }
        }
        if (GameManager.instance != null)
        {
            GameManager.instance.ConnectionReset();
            staticVariables.Isrejoining = false;

        }
        CallNetworkState(NetworkState.JoinedToRoom);
        if (staticVariables.OpponetProfile != null && staticVariables.OpponetProfile.userId != "")
            staticVariables.lastOpponentId = staticVariables.OpponetProfile.userId;


    }

    public Root userModel;
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (isGameActive)
        {
            ApiAndRoomManager._instance.GetChallengeStatus(ApiAndRoomManager._instance.winLoseChallengeId, success =>
            {
                userModel = JsonUtility.FromJson<Root>(success);
                if (userModel.status)
                {
                    if (userModel.data.status.Contains("active") || userModel.data.status.Contains("draw"))
                    {
                        OnServerDraw?.Invoke(false, "");
                    }
                    else
                    {
                        OnServerDraw?.Invoke(true, "");
                    }

                }
            }, failed =>
            {

            });
        }

        ConstantsData_M.Log("Failed to join the room: " + message);
        Debug.LogError($"Join room failed: {message} (Code: {returnCode})");
    }

    #region Reconnecting 
    public bool IsInGame = false;
    public DateTime PlayerGameTime, OpponentGameTime, GameBeginningTime;

    public static event Action OnDisconnect, OnWin, OnLose, OnReconnect;
    public static event Action<bool> OnAwaitingOpponent;
    public static event Action<bool, string> OnServerDisconnected;
    public static event Action<bool, string> OnServerDraw;
    public Action<float> OnDisconnectionCountdown;

    public bool Isreconnectingvar = false;
    public bool isgameInBg = false;

    [Header("Detection Settings")]
    public float connectionWarningDelay = 2.5f;
    public float officialDisconnectTimeout = 30f;
    public float connectionCheckInterval = 0.5f;

    // Unified connection state
    public enum ConnectionState
    {
        Connected,
        SelfDisconnected,
        OpponentDisconnected,
        GameEnded
    }

    public ConnectionState currentState = ConnectionState.Connected;
    private Player disconnectedPlayer;
    private float disconnectionStartTime;
    private bool isGameActive;

    // Heartbeat tracking
    [SerializedDictionary("Player", "HeartBeat")]
    public SerializedDictionary<Player, double> lastHeartbeatTime = new SerializedDictionary<Player, double>();

    // Single monitoring coroutine
    private Coroutine connectionMonitorCoroutine;
    private Coroutine HeartBeatCoroutine;

    #region Game Initialization

    public void StartGame()
    {
        if (isGameActive) return;
        var peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
        peer.DisconnectTimeout = 5000;
        retrycount = 0;
        isGameActive = true;
        HasWinnerBeenDeclared = false;
        currentState = ConnectionState.Connected;

        InitializeHeartbeatTracking();
        StartConnectionMonitoring();

        Debug.Log("Game started with simplified disconnection detection");
    }

    private void InitializeHeartbeatTracking()
    {
        lastHeartbeatTime.Clear();
        double currentTime = PhotonNetwork.Time;
        SendMinimalHeartbeat();
        foreach (var player in PhotonNetwork.PlayerList)
        {
            lastHeartbeatTime[player] = currentTime;
        }
    }

    private void StartConnectionMonitoring()
    {
        if (connectionMonitorCoroutine != null)
            StopCoroutine(connectionMonitorCoroutine);

        connectionMonitorCoroutine = StartCoroutine(MonitorAllConnections());
        if (HeartBeatCoroutine != null)
            StopCoroutine(HeartBeatCoroutine);
        HeartBeatCoroutine = StartCoroutine(SendHeartBeatAfterDelay());
    }

    #endregion

    #region Unified Connection Monitoring

    private IEnumerator MonitorAllConnections()
    {
        float lastLocalTime = Time.unscaledTime;
        double lastServerTime = PhotonNetwork.Time;

        while (isGameActive && !HasWinnerBeenDeclared)
        {
            yield return new WaitForSecondsRealtime(connectionCheckInterval);
            CheckOpponentConnections();
        }
    }
    const byte HeartbeatEventCode = 59;


    private IEnumerator SendHeartBeatAfterDelay()
    {
        while (isGameActive && !HasWinnerBeenDeclared)
        {
            yield return new WaitForSecondsRealtime(connectionCheckInterval);
            if (currentState == ConnectionState.SelfDisconnected) continue;
            if (PhotonNetwork.GetPing() < 350) SendMinimalHeartbeat();

        }
    }
    private void SendMinimalHeartbeat()
    {
        // Send nothing - just the event itself indicates the player is alive
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(HeartbeatEventCode, null, options, SendOptions.SendUnreliable);
    }

    private void OnMinimalHeartbeatEvent(EventData photonEvent)
    {
        if (photonEvent.Code == HeartbeatEventCode)
        {
            Player sender = PhotonNetwork.CurrentRoom.GetPlayer(photonEvent.Sender);

            if (sender != null && sender != PhotonNetwork.LocalPlayer)
            {
                // Use current time instead of sent timestamp
                lastHeartbeatTime[sender] = PhotonNetwork.Time;
                StartCoroutine(ShowAwaitPanelThenReconnect(sender));
            }
        }
    }

    [PunRPC]
    private void ReceiveHeartbeat(double timestamp, PhotonMessageInfo info)
    {
        lastHeartbeatTime[info.Sender] = timestamp;
        StartCoroutine(ShowAwaitPanelThenReconnect(info.Sender));
    }
    private void CheckOpponentConnections()
    {
        if (currentState == ConnectionState.SelfDisconnected) return;

        double currentTime = PhotonNetwork.Time;
        if (PhotonNetwork.PlayerList.Length < 2)
        {
            HandleOpponentDisconnection(null);
            return;
        }
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) continue;

            if (lastHeartbeatTime.ContainsKey(player))
            {
                double timeSinceHeartbeat = currentTime - lastHeartbeatTime[player];

                if (timeSinceHeartbeat > connectionWarningDelay && PhotonNetwork.GetPing() < 350)
                {
                    HandleOpponentDisconnection(player);
                    break; // Only handle one disconnection at a time
                }
            }
        }
    }

    #endregion

    #region Connection Event Handlers

    private void HandleOpponentDisconnection(Player player)
    {
        if (currentState != ConnectionState.Connected || HasWinnerBeenDeclared) return;

        Debug.Log($"Opponent disconnection detected: {player?.NickName}");

        currentState = ConnectionState.OpponentDisconnected;
        disconnectedPlayer = player;
        disconnectionStartTime = (float)PhotonNetwork.Time;
        OnAwaitingOpponent?.Invoke(true);
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!targetPlayer.IsInactive && currentState == ConnectionState.OpponentDisconnected)
        {

            HandleOpponentReconnection(targetPlayer);
        }
    }
    private void HandleOpponentReconnection(Player player)
    {

        if (disconnectedPlayer != player && currentState != ConnectionState.OpponentDisconnected)
            return;
        Debug.Log($"Opponent reconnected: {player.NickName}");

        currentState = ConnectionState.Connected;
        disconnectedPlayer = null;

        OnAwaitingOpponent?.Invoke(false);
        CheckGameStatusOnJoin();
    }
    #endregion

    #region Photon Callbacks

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!isGameActive || HasWinnerBeenDeclared) return;
        CheckGameStatusOnJoin();
        Debug.Log("Game Left wow");
        if (otherPlayer.IsInactive)
        {
            // Player disconnected - let existing system handle it
            if (currentState != ConnectionState.SelfDisconnected && currentState != ConnectionState.OpponentDisconnected)
            {
                HandleOpponentDisconnection(otherPlayer);
            }
        }
        else
        {
            // Player left intentionally
            DeclareWin("Opponent left the game");
        }
        //  lastHeartbeatTime.Remove(otherPlayer);

    }


    private IEnumerator ShowAwaitPanelThenReconnect(Photon.Realtime.Player player)
    {
        yield return null;
        HandleOpponentReconnection(player);
    }

    #endregion

    #region Game End

    public void DeclareDefeat(string reason)
    {
        if (HasWinnerBeenDeclared) return;
        if (PhotonNetwork.InRoom)
        {
            // Check room properties first to avoid race conditions
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("HasWinnerBeenDeclared") &&
                (bool)PhotonNetwork.CurrentRoom.CustomProperties["HasWinnerBeenDeclared"])
            {
                HasWinnerBeenDeclared = true;
                Cleanup();

                return;
            }
        }

        HasWinnerBeenDeclared = true;
        currentState = ConnectionState.GameEnded;
        Debug.Log($"Defeat! Reason: {reason}");
        if (PhotonNetwork.InRoom)
            // Set room property to indicate game has ended
            SetGameEndedInRoomProperties(reason, false); // false = this player lost

        Cleanup();
        OnLose?.Invoke();
        if (isGameActive == false)
        {

        }
    }

    public void DeclareWin(string reason)
    {
        if (HasWinnerBeenDeclared) return;

        // Check room properties first to avoid race conditions
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("HasWinnerBeenDeclared") &&
            (bool)PhotonNetwork.CurrentRoom.CustomProperties["HasWinnerBeenDeclared"])
        {
            HasWinnerBeenDeclared = true;
            OnLose.Invoke();
            Cleanup();
            return;
        }

        HasWinnerBeenDeclared = true;
        currentState = ConnectionState.GameEnded;
        Debug.Log($"Victory! Reason: {reason}");

        // Set room property to indicate game has ended
        SetGameEndedInRoomProperties(reason, true); // true = this player won

        Cleanup();
        OnWin?.Invoke();
    }
    private void SetGameEndedInRoomProperties(string reason, bool didLocalPlayerWin)
    {
        if (!PhotonNetwork.InRoom) return;

        var roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["HasWinnerBeenDeclared"] = true;
        roomProps["GameEndReason"] = reason;
        roomProps["GameEndTime"] = PhotonNetwork.ServerTimestamp;

        // Store winner information
        if (didLocalPlayerWin)
        {
            roomProps["WinnerId"] = PhotonNetwork.LocalPlayer.UserId;
            roomProps["WinnerName"] = PhotonNetwork.LocalPlayer.NickName;
        }
        else
        {
            // If we lost, the other player won (assuming 2-player game)
            var otherPlayer = PhotonNetwork.PlayerListOthers.FirstOrDefault();
            if (otherPlayer != null)
            {
                roomProps["WinnerId"] = otherPlayer.UserId;
                roomProps["WinnerName"] = otherPlayer.NickName;
            }
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }
    public void CheckGameStatusOnJoin()
    {
        if (!PhotonNetwork.InRoom) return;

        var roomProps = PhotonNetwork.CurrentRoom.CustomProperties;

        if (roomProps.ContainsKey("HasWinnerBeenDeclared") && (bool)roomProps["HasWinnerBeenDeclared"])
        {
            HasWinnerBeenDeclared = true;
            currentState = ConnectionState.GameEnded;

            string reason = roomProps.ContainsKey("GameEndReason") ?
                           (string)roomProps["GameEndReason"] : "Game already ended";

            Debug.Log($"Game already ended when joining. Reason: {reason}");

            // Determine if this player was the winner
            bool wasWinner = false;
            if (roomProps.ContainsKey("WinnerId"))
            {
                string winnerId = (string)roomProps["WinnerId"];
                wasWinner = winnerId == PhotonNetwork.LocalPlayer.UserId;
            }

            // Don't call Cleanup() here as the game might not be fully initialized yet
            // Just set the state and let the game handle it appropriately

            if (wasWinner)
            {
                OnWin?.Invoke();
            }
            else
            {
                OnLose?.Invoke();
            }
        }
    }
    #endregion

    #region Cleanup

    public void Cleanup()
    {
        isGameActive = false;
        var peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
        peer.DisconnectTimeout = 10000;
        if (connectionMonitorCoroutine != null)
        {
            StopCoroutine(connectionMonitorCoroutine);
            connectionMonitorCoroutine = null;
        }
        if (HeartBeatCoroutine != null)
        {
            StopCoroutine(HeartBeatCoroutine);
            HeartBeatCoroutine = null;
        }
        disconnectedPlayer = null;

        OnAwaitingOpponent?.Invoke(false);
        CheckGameStatusOnJoin();

    }

    void OnDestroy()
    {
        Cleanup();
        PhotonNetwork.RemoveCallbackTarget(this);
        PhotonNetwork.NetworkingClient.EventReceived -= OnMinimalHeartbeatEvent;

    }

    #endregion

    #endregion

    public void VerifyConnection()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsConnectedAndReady)
        {
            //Debug.Log("Network State : Connected and ready");
            if (!PhotonNetwork.InLobby)
                PhotonNetwork.JoinLobby();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }

    }

    public override void OnConnected()
    {
        CallNetworkState(NetworkState.Connected);
        PhotonNetwork.Reconnect();
        PopupMessageManager.instance.ReconnectionPanelStatus(false);
    }

    public bool IsAIControlledHorse;
    public void SetOnServerDisconnected(bool state, string reson)
    {
        if (SceneManager.GetActiveScene().name == "Home")
            PopupMessageManager.instance.ReconnectionPanelStatus(state);
    }
    private void OnEnable()
    {
        PunNetwork.OnServerDisconnected += SetOnServerDisconnected;

    }

    private void OnDisable()
    {
        PunNetwork.OnServerDisconnected -= SetOnServerDisconnected;

    }
    int retrycount = 0;
    public void CreateRoom(RoomOptions roomOptions)
    {
        Cleanup();
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // GameObject gb = Instantiate(SpritesManager.Instance.spritesScriptable.CreateChallenge_coinTransfer, HomeMenuManager.instance.MainCanvas.transform);

            ApiAndRoomManager.currentGameId.Show("Game ID");
            "On Create Room".Show();
            PhotonNetwork.CreateRoom(ApiAndRoomManager._instance.challenge_transaction_id, roomOptions, expectedUsers: new string[] { PhotonNetwork.LocalPlayer.UserId });
        }
        else
        {
            // Connect();
        }
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Photon Disconnected :" + cause.ToString());
        if (IsAIControlledHorse)
        {
            PhotonNetwork.OfflineMode = true;
           // Connect();
        }
        else
        {
            OnDisconnect?.Invoke();
            //if (isGameActive)
            //{
            //    retrycount++;
            //    if (retrycount >= 8)
            //    {
            //        PopupMessageManager.instance.ReconnectionPanelStatus(false);
            //        DeclareDefeat("Internet Doesn't Recovered");

            //    }
            //    else
            //    {
            //        OnServerDisconnected?.Invoke(true, "Internet Disconnected");

            //        currentState = ConnectionState.SelfDisconnected;
            //    }
            //}
            //else
            {
                OnServerDisconnected?.Invoke(true, "Internet Disconnected");
            }
            Reconnect();
        }

    }

    public void Reconnect()
    {
        StartCoroutine(ResetConnection());
    }

    public void CheckOpponentStatus(string value)
    {
        punView.RPC("VerifyOpponentConnection", RpcTarget.Others, value);
    }

    private IEnumerator ResetConnection()
    {
        while (PhotonNetwork.NetworkingClient.LoadBalancingPeer.PeerState != ExitGames.Client.Photon.PeerStateValue.Disconnected)
        {
            yield return new WaitForSeconds(0.2f);
        }

        if (!PhotonNetwork.ReconnectAndRejoin())
        {
            if (PhotonNetwork.Reconnect())
            {
                PopupMessageManager.instance.ReconnectionPanelStatus(false);
            }
        }
        else
        {

            isPaused = false;
        }

    }

    public override void Resset()
    {
        LeftRoom();
    }
    public override void LeftRoom()
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Joined)
        {
            ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);

            PhotonNetwork.LeaveRoom();
            Physics.simulationMode = SimulationMode.FixedUpdate;
        }
    }
    public override void Connect()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        if (ApiAndRoomManager._instance != null)
        {
            PhotonNetwork.NickName = staticVariables.userNickName;
        }
    }
    public override void Disconnect()
    {
        if (PhotonNetwork.NetworkClientState != ClientState.Disconnected)
        {
            PhotonNetwork.Disconnect();
        }
    }
    public override void OnJoinedLobby() //RAR
    {
        if (HandleLobbyJoined != null)
        {
            HandleLobbyJoined();
        }
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("I am ready to join Room");
        //RoomOptions roomOptions = new RoomOptions()
        //{
        //    IsVisible = true,
        //    IsOpen = true,
        //    MaxPlayers = 2,
        //    CleanupCacheOnLeave = false,
        //    PlayerTtl = 30000, // e.g., 1 minute
        //    EmptyRoomTtl = 60000 // e.g., 1 minute
        //};
        //PhotonNetwork.JoinOrCreateRoom("HelloWorld", roomOptions,TypedLobby.Default);
        if (IsAIControlledHorse && PhotonNetwork.OfflineMode)
        {
            MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;
            MultiPlayerGame.isSinglePlayer = true;
            MultiPlayerGame.setAIData();
            HandleRoomJoined = null;
            HandleRoomJoined += PunNetwork.instance.OnEnterHorseAIRoom;
            ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gameType", MultiPlayerGame.isLastManStandingMode ? 2 : 1 } };
            bool isSucces = PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 4);
            if (isSucces)
                Debug.Log("Joined");
            else
                Debug.Log("rolla");
        }
        else
        {

            base.OnConnectedToMaster();
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }
        // PhotonNetwork.LocalPlayer.SetCustomString("PlayerID", staticVariables.UserProfiledata.user._id.ToString());

    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ConstantsData_M.Log("room creation failed  " + message);

    }
    public override void OnCreatedRoom()
    {
        HandleRoomCreated?.Invoke(PhotonNetwork.CurrentRoom);
        //Debug.Log("OnCreateRoom: " + PhotonNetwork.CurrentRoom.Name);
        CallNetworkState(NetworkState.CreatedRoom);
        HandleRoomCreated = null;
    }
    public override void OnLeftRoom()
    {
        //ConstantsData_M.Log("local player left" + PhotonNetwork.LocalPlayer.IsInactive);

        opponent = null;
        PhotonNetwork.RemoveRPCs(PhotonNetwork.LocalPlayer);
        CallNetworkState(NetworkState.LeftRoom);
        ApiAndRoomManager._instance.ModifyUserBalance();
    }

    public List<RoomInfo> RoomCollection = new List<RoomInfo>();

    public bool isPaused = false;

    public void CallPauseStateRPC(bool isPaused)
    {
        punView.RPC("SetPauseState", RpcTarget.All, isPaused);
    }
    [PunRPC]
    private void VerifyOpponentConnection(string value)
    {
        string[] possibleFormats = new string[]
           {
                          "yyyy-MM-dd'T'HH:mm:ss.fffffff", // 7 fractional digits
                "yyyy-MM-dd'T'HH:mm:ss.ffffff",  // 6 fractional digits

           };
        DateTime parsedatetime = DateTime.Now;
        foreach (string format in possibleFormats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedatetime))
            {
                OpponentGameTime = DateTime.ParseExact(value, format, CultureInfo.InvariantCulture);
                break;
            }
        }
        if (GameManager.instance)
            GameManager.instance?.waitingForOpponentPanel.SetActive(false);

    }

    [PunRPC]
    private void SetPauseState(bool value)
    {
        isPaused = value;
        //  OnAwaitingOpponent?.Invoke(!value);      // mohsin8
        ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    }

    [PunRPC]
    public override void OnOpponenReadToPlay(string playerData, bool is3DGraphicMode)
    {
        NetworkManager.opponentPlayer = NetworkManager.PlayerFromStringRoom(playerData);
        AightBallPoolNetworkGameAdapter.isSameGraphicsMode = AightBallPoolNetworkGameAdapter.is3DGraphics == is3DGraphicMode;

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
        //   GetComponent<AudioSource>().Play();
        OnStartGameWithPlayer(staticVariables.OpponetProfile);
        int turnId = UnityEngine.Random.Range(0, 2);
        int turnIdForSend = turnId == 1 ? 0 : 1;
        OnOpponenStartToPlay(turnId);
        punView.RPC("OnOpponenStartToPlay", RpcTarget.Others, turnIdForSend);
    }

    [PunRPC]
    public override void OnOpponenStartToPlay(int turnId) // join
    {
        adapter.SetTurn(turnId);
        // ApiAndRoomManager._instance.FetchCurrentTime(OnTimeFetched, failed => { ConstantsData_M.Log(failed); });
        SceneLoaderUtility.LoadScene("AightBallPool");
    }

    [PunRPC]
    public override void OnSendTime(float time01)
    {
        poolMessenger.SetTime(time01);
    }
    [PunRPC]
    public override void StartSimulate(string impulse)
    {
        poolMessenger.StartSimulate(impulse);
    }
    [PunRPC]
    public override void EndSimulate(string ballsState)
    {
        poolMessenger.EndSimulate(ballsState);
    }
    [PunRPC]
    public override void OnOpponenWaitingForYourTurn()
    {
        base.OnOpponenWaitingForYourTurn();
    }
    [PunRPC]
    public override void OnOpponenInGameScene()
    {
        StartCoroutine(poolMessenger.OnOpponenInGameScene());
    }
    [PunRPC]
    public override void OnOpponentForceGoHome()
    {
        poolMessenger.OnOpponentForceGoHome();
    }
    #region AightBallPool Interface
    [PunRPC]
    public void OnSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
    {
        poolMessenger.OnSendCueControl(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
    }
    [PunRPC]
    public void OnForceSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
    {
        poolMessenger.OnForceSendCueControl(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
    }
    [PunRPC]
    public void OnMoveBall(Vector3 ballPosition)
    {
        poolMessenger.OnMoveBall(ballPosition);
    }
    [PunRPC]
    public void SelectBallPosition(Vector3 ballPosition)
    {
        poolMessenger.SelectBallPosition(ballPosition);
    }
    [PunRPC]
    public void SetBallPosition(Vector3 ballPosition)
    {
        poolMessenger.SetBallPosition(ballPosition);
    }

    [PunRPC]
    public void SetMechanicalStatesFromNetwork(int ballId, string mechanicalStateData)
    {
        poolMessenger.SetMechanicalStatesFromNetwork(ballId, mechanicalStateData);
    }
    [PunRPC]
    public void WaitAndStopMoveFromNetwork(float time)
    {
        poolMessenger.WaitAndStopMoveFromNetwork(time);
    }
    [PunRPC]
    public void SendOpponentCueURL(string url)
    {
        poolMessenger.SetOpponentCueURL(url);
    }
    [PunRPC]
    public void SendOpponentTableURLs(string boardURL, string clothURL, string clothColor)
    {
        poolMessenger.SetOpponentTableURLs(boardURL, clothURL, clothColor);
    }



    #endregion

}
#endif
