using System.Diagnostics;
using UnityEngine.SceneManagement;

namespace CarRace
{
    using Michsky.LSS;
    using Mirror;
    using Mirror.BouncyCastle.Cms;
    using Shapes2D;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.UIElements;
    using UnityExtensions;
    using UnityStandardAssets.ImageEffects;
    using static events;

    public class MultiPlayerGameManager : NetworkBehaviour
    {
        [Header("Player Object and Spawn Points")]
        [SerializeField] GameObject[] playerPrafab;
        [SerializeField] Transform[] spawnLocations;

        [Space(3), Header("Wrong Direction Check WayPoint")]
        public List<Transform> waypoints;

        [Space(3), Header("Lap Triggers")]
        public BoxCollider halfLapTrigger;
        public BoxCollider finishLapTrigger;

        [Space(3), Header("Waiting Panel UIs")]
        [SerializeField] GameObject waitingPanel;
        [SerializeField] TextMeshProUGUI[] playerNameTexts;
        [SerializeField] TextMeshProUGUI matchStartTimerText;
        [SerializeField] TextMeshProUGUI countdownText;
        int matchStartTimer = 3;
        static public bool gameStarted = false;
        bool gameFinished = false;

        [Space(3), Header("Main UI References")]
        public TextMeshProUGUI lapText;
        public GameObject wrongWayPopUp;
        [SerializeField] GameObject mobileControls;
        public GameObject disconnectedPopUp;
        [SerializeField] UnityEngine.UI.Button leaveGameButton;
        public GameObject repairButton;
        [SerializeField] UnityEngine.UI.Button changeControlsButton;
        [SerializeField] GameObject notificationPopup;
        TextMeshProUGUI notificationText;

        [Space(3), Header("Local Player Finish Pop Up")]
        public GameObject FinishPopLocalPlayer;
        public TextMeshProUGUI finishPopTime;

        [Space(3), Header("Race Finish Menu")]
        [SerializeField]public GameObject finishPanel;
        public ResultManagerForCar carWinLosePanel;
        [SerializeField] UnityEngine.UI.Button mainMenuFinishButton;
        [SerializeField] UnityEngine.UI.Button doubleRewardButton;
        [SerializeField] TextMeshProUGUI[] positionLabelTexts;
        [SerializeField] TextMeshProUGUI[] playerNamePositionsTexts;
        [SerializeField] TextMeshProUGUI[] totalTimeTexts;
        [SerializeField] TextMeshProUGUI[] rewardTexts;
        [SerializeField] TextMeshProUGUI playerPositiontext;
        [SerializeField] GameObject[] stars;
        [SerializeField] TextMeshProUGUI remarksText;

        [Space(3), Header("Ads Related UI")]
        [SerializeField] GameObject rewardPopUp;
        [SerializeField] TextMeshProUGUI totalRewardText;
        int userCoins = 0;

        [Space(3), Header("Scripts References")]
        [SerializeField] LoadingScreenManager loadingScreenManager;
        [SerializeField] RCC_Camera RCC_camera;

        [Space(3), Header("Ping")]
        [SerializeField] TextMeshProUGUI pingText;
        [SerializeField] UnityEngine.UI.Image pingIcon;

       public bool isGameLeft = false;
        int currentControls;
        public static MultiPlayerGameManager Instance;
        // Mirror SyncDictionaries for player ready states and finish times
        public readonly SyncDictionary<int, bool> playersReady = new SyncDictionary<int, bool>();
        public readonly SyncDictionary<int, PlayerData> playersData = new SyncDictionary<int, PlayerData>();
          [Header("UI Timer")]
    public TextMeshProUGUI countDownGameTimer;

    [Header("Countdown Settings")]
    [SyncVar(hook = nameof(OnTimeChanged))]
    public int remainingTime = 600; // server owns this

    public int remainingminutes;
    public int remainingseconds;

    private Coroutine countdownCoroutine;
    private Coroutine initialTurnCoroutine;
    private const int ServerRequiredLaps = 2;
    private const float ServerTriggerTolerance = 35f;
    private bool serverResultDeclared;
    private bool serverObservedTwoPlayers;
    private bool isLeavingMultiplayer;

    private sealed class ServerLapProgress
    {
        public bool crossedMiddle;
        public int completedLaps;
    }

    private readonly Dictionary<string, ServerLapProgress> serverLapProgress =
        new Dictionary<string, ServerLapProgress>();

    [Server]
    private bool TryResolveParticipant(NetworkIdentity participant, out string participantId)
    {
        participantId = string.Empty;
        NetworkGameManager networkManager = NetworkGameManager.Instance;
        NetworkConnectionToClient connection = participant != null ? participant.connectionToClient : null;
        if (networkManager == null || connection == null ||
            networkManager.creatorData == null || networkManager.joinerData == null)
        {
            Debug.LogWarning("[CarServerResult] Player mapping failed: manager, connection, or player data is missing.");
            return false;
        }

        if (networkManager.CreatorRef != null &&
            connection.connectionId == networkManager.CreatorRef.connectionId)
            participantId = networkManager.creatorData.playerId;
        else if (networkManager.JoinerRef != null &&
                 connection.connectionId == networkManager.JoinerRef.connectionId)
            participantId = networkManager.joinerData.playerId;
        else
        {
            Debug.LogWarning($"[CarServerResult] Unregistered car connection rejected: {connection.connectionId}.");
            return false;
        }

        return !string.IsNullOrEmpty(participantId);
    }

    [Server]
    private bool TryResolveWinner(NetworkIdentity participant, bool participantWon, out string winnerId)
    {
        winnerId = string.Empty;
        if (!TryResolveParticipant(participant, out string participantId))
            return false;

        NetworkGameManager networkManager = NetworkGameManager.Instance;
        if (participantWon)
            winnerId = participantId;
        else if (participantId == networkManager.creatorData.playerId)
            winnerId = networkManager.joinerData.playerId;
        else if (participantId == networkManager.joinerData.playerId)
            winnerId = networkManager.creatorData.playerId;

        if (string.IsNullOrEmpty(winnerId))
        {
            Debug.LogWarning("[CarServerResult] Winner ID could not be resolved.");
            return false;
        }

        return true;
    }

    [Server]
    private bool ValidateServerTrigger(NetworkIdentity participant, Collider trigger, string triggerName)
    {
        if (participant == null || participant.connectionToClient == null || trigger == null)
        {
            Debug.LogWarning($"[CarServerResult] {triggerName} rejected: participant or trigger is missing.");
            return false;
        }

        Vector3 closestPoint = trigger.ClosestPoint(participant.transform.position);
        float distance = Vector3.Distance(participant.transform.position, closestPoint);
        if (distance > ServerTriggerTolerance)
        {
            Debug.LogWarning($"[CarServerResult] {triggerName} rejected: netId={participant.netId}, distance={distance:F1}m.");
            return false;
        }

        return TryResolveParticipant(participant, out _);
    }

    [Server]
    public bool ServerRecordMiddleCheckpoint(NetworkIdentity participant)
    {
        if (serverResultDeclared || !serverObservedTwoPlayers ||
            !ValidateServerTrigger(participant, halfLapTrigger, "middle checkpoint") ||
            !TryResolveParticipant(participant, out string participantId))
            return false;

        if (!serverLapProgress.TryGetValue(participantId, out ServerLapProgress progress))
        {
            progress = new ServerLapProgress();
            serverLapProgress[participantId] = progress;
        }

        progress.crossedMiddle = true;
        Debug.Log($"[CarServerResult] Middle checkpoint verified for player={participantId}, lap={progress.completedLaps + 1}.");
        return true;
    }

    [Server]
    public bool ServerRecordFinishCheckpoint(NetworkIdentity participant)
    {
        if (serverResultDeclared || !serverObservedTwoPlayers ||
            !ValidateServerTrigger(participant, finishLapTrigger, "finish checkpoint") ||
            !TryResolveParticipant(participant, out string participantId))
            return false;

        if (!serverLapProgress.TryGetValue(participantId, out ServerLapProgress progress) ||
            !progress.crossedMiddle)
        {
            Debug.LogWarning($"[CarServerResult] Finish rejected for player={participantId}: middle checkpoint was not verified.");
            return false;
        }

        progress.crossedMiddle = false;
        progress.completedLaps++;
        Debug.Log($"[CarServerResult] Finish checkpoint verified for player={participantId}, completedLaps={progress.completedLaps}.");

        return progress.completedLaps < ServerRequiredLaps || ServerDeclareWinner(participant);
    }

    [Server]
    public bool ServerDeclareWinner(NetworkIdentity participant)
    {
        return ServerDeclareResult(participant, true);
    }

    [Server]
    public bool ServerDeclareLoser(NetworkIdentity participant)
    {
        return ServerDeclareResult(participant, false);
    }

    [Server]
    private bool ServerDeclareResult(NetworkIdentity participant, bool participantWon)
    {
        if (serverResultDeclared)
            return false;
        if (ApiAndRoomManager._instance == null)
        {
            Debug.LogError("[CarServerResult] ApiAndRoomManager is missing on the server.");
            return false;
        }
        if (!TryResolveWinner(participant, participantWon, out string winnerId))
            return false;

        NetworkGameManager networkManager = NetworkGameManager.Instance;
        if (string.IsNullOrEmpty(networkManager.transactionId))
        {
            Debug.LogError("[CarServerResult] Transaction ID is empty; result was not submitted.");
            return false;
        }

        serverResultDeclared = true;
        Debug.Log($"[CarServerResult] Server decided winner={winnerId}, connection={participant.connectionToClient.connectionId}.");
        ApiAndRoomManager._instance.winLoseChallengeId = networkManager.transactionId;
        ApiAndRoomManager._instance.WinnerLossChallenge(winnerId);
        RpcShowServerWinner(winnerId);
        ScheduleServerCleanup();
        return true;
    }

    [Server]
    private void ServerDeclareDraw()
    {
        if (serverResultDeclared || ApiAndRoomManager._instance == null || NetworkGameManager.Instance == null)
            return;

        string transactionId = NetworkGameManager.Instance.transactionId;
        if (string.IsNullOrEmpty(transactionId))
            return;

        serverResultDeclared = true;
        ApiAndRoomManager._instance.winLoseChallengeId = transactionId;
        ApiAndRoomManager._instance.DrawChallenge(transactionId);
        RpcShowServerDraw();
        ScheduleServerCleanup();
    }

    [Server]
    private bool ServerDeclareConnectedPlayerWinner(NetworkConnectionToClient requiredConnection = null)
    {
        PlayerSetup[] players = FindObjectsOfType<PlayerSetup>();
        for (int i = 0; i < players.Length; i++)
        {
            NetworkIdentity identity = players[i] != null ? players[i].netIdentity : null;
            NetworkConnectionToClient connection = identity != null ? identity.connectionToClient : null;
            if (connection == null ||
                (requiredConnection != null && connection != requiredConnection) ||
                !NetworkServer.connections.TryGetValue(connection.connectionId, out NetworkConnectionToClient activeConnection) ||
                activeConnection != connection)
                continue;

            Debug.Log($"[CarServerResult] Existing disconnect flow verified; remaining connection={connection.connectionId} wins.");
            return ServerDeclareWinner(identity);
        }

        Debug.LogWarning("[CarServerResult] No connected Car player was found for the result.");
        return false;
    }

    public bool RequestLocalForfeit()
    {
        if (!NetworkClient.isConnected)
            return false;

        CmdForfeitRace();
        return true;
    }

    [Command(requiresAuthority = false)]
    private void CmdForfeitRace(NetworkConnectionToClient sender = null)
    {
        if (sender == null)
        {
            Debug.LogWarning("[CarServerResult] Forfeit rejected: sender connection is missing.");
            return;
        }

        PlayerSetup[] players = FindObjectsOfType<PlayerSetup>();
        for (int i = 0; i < players.Length; i++)
        {
            NetworkIdentity identity = players[i] != null ? players[i].netIdentity : null;
            if (identity == null || identity.connectionToClient != sender)
                continue;

            Debug.Log($"[CarServerResult] Player forfeited, connection={sender.connectionId}.");
            ServerDeclareLoser(identity);
            return;
        }

        Debug.LogWarning("[CarServerResult] Forfeit rejected: owned Car player was not found.");
    }

    public bool RequestExistingDisconnectResult()
    {
        if (!NetworkClient.isConnected)
            return false;

        CmdRequestExistingDisconnectResult();
        return true;
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestExistingDisconnectResult(NetworkConnectionToClient sender = null)
    {
        NetworkGameManager networkManager = NetworkGameManager.Instance;
        if (sender == null || networkManager == null || !serverObservedTwoPlayers ||
            networkManager.currentPlayerCount != 1)
        {
            Debug.LogWarning($"[CarServerResult] Existing disconnect result rejected: count=" +
                             $"{(networkManager != null ? networkManager.currentPlayerCount : -1)}.");
            return;
        }

        ServerDeclareConnectedPlayerWinner(sender);
    }

    [ClientRpc]
    private void RpcShowServerWinner(string winnerId)
    {
        ResultManager.isGameFinished = true;
        if (PopupMessageManager.instance != null && PopupMessageManager.instance.waitingPanel != null)
            PopupMessageManager.instance.waitingPanel.StopTimer();

        ShowServerWinner(winnerId);
    }

    [ClientRpc]
    private void RpcShowServerDraw()
    {
        ShowServerDraw();
    }

    public void ShowServerWinner(string winnerId)
    {
        if (ResultManager.GameSpawnedFinished || !ResultManager.TryCommitResultLatch())
            return;

        ResultManager resultManager = SpawnResultManager();
        if (resultManager == null)
            return;

        ResultManager.GameSpawnedFinished = true;
        ResultManager.isGameFinished = true;
        gameFinished = true;
        gameStarted = false;
        PlayersCarsData.Clear();
        MirrorNetwork.winnerID = winnerId;

        bool localPlayerWon = winnerId == staticVariables.UserProfiledata.user._id.ToString();
        resultManager.finishPanelUI.SetActive(true);
        resultManager.rematchButton.SetActive(true);
        resultManager.winnerText.SetActive(true);
        resultManager.loserText.SetActive(true);
        resultManager.drawText.SetActive(false);
        SetWinnerBox(resultManager, true, localPlayerWon);
        NetworkGameManager.OnRematchRecived += resultManager.RematchRequestRecived;

        string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
        int prizeAmount = NetworkGameManager.Instance != null
            ? NetworkGameManager.Instance.Prize
            : staticVariables.currentPrize;

        if (localPlayerWon)
        {
            resultManager.InitializeWinnerUI(staticVariables.ProfilePicture,
                staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
            resultManager.InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
            resultManager.resultPopupText.text =
                $"Congratulations! Your account has been credited with {prizeAmount} {prizeType} coins";
        }
        else
        {
            resultManager.InitializeLoserUI(staticVariables.ProfilePicture,
                staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
            resultManager.InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
            resultManager.resultPopupText.text =
                $"{prizeAmount} {prizeType} coins has been deducted from your account. Please try again";
        }

        if (ApiAndRoomManager._instance != null)
        {
            ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, response =>
            {
                Single<int> player = JsonUtility.FromJson<Single<int>>(response);
                resultManager.friendRequestButton.SetActive(player.message.Contains("not"));
            });
        }

        if (MirrorNetwork.Instance != null)
        {
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            MirrorNetwork.Instance.OpponentDisconnectReason = DisconnectReason.ApplicationQuit;
            MirrorNetwork.Instance.cleanup();
        }
    }

    private void ShowServerDraw()
    {
        if (ResultManager.GameSpawnedFinished || !ResultManager.TryCommitResultLatch())
            return;

        ResultManager resultManager = SpawnResultManager();
        if (resultManager == null)
            return;

        ResultManager.GameSpawnedFinished = true;
        ResultManager.isGameFinished = true;
        gameFinished = true;
        gameStarted = false;
        PlayersCarsData.Clear();

        resultManager.winnerText.SetActive(false);
        resultManager.loserText.SetActive(false);
        resultManager.drawText.SetActive(true);
        SetWinnerBox(resultManager, false, true);
        resultManager.chest.SetActive(false);
        resultManager.finishPanelUI.SetActive(true);
        resultManager.InitializeWinnerUI(staticVariables.ProfilePicture,
            staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
        resultManager.InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
        resultManager.resultPopupText.text = "It's a draw! No coins have been deducted or credited to your account.";
    }

    private static ResultManager SpawnResultManager()
    {
        GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
        if (prefab == null)
        {
            Debug.LogError("WinLoseGameManager prefab not found!");
            return null;
        }

        ResultManager resultManager = Instantiate(prefab, Vector3.zero, Quaternion.identity)
            .GetComponent<ResultManager>();
        if (resultManager == null)
            Debug.LogError("ResultManager component not found on WinLoseGameManager!");
        return resultManager;
    }

    private static void SetWinnerBox(ResultManager resultManager, bool hasWinner, bool localPlayerWon)
    {
        if (hasWinner && ResultManager.LocalPlayerSideOverride != ResultManager.ResultSide.None &&
            TryResolveResultBlocks(resultManager, out RectTransform winnerBlock, out RectTransform loserBlock))
        {
            bool localShouldBeRight = ResultManager.LocalPlayerSideOverride == ResultManager.ResultSide.Right;
            RectTransform localBlock = localPlayerWon ? winnerBlock : loserBlock;
            RectTransform otherBlock = localPlayerWon ? loserBlock : winnerBlock;
            bool localIsCurrentlyRight = localBlock.anchoredPosition.x > otherBlock.anchoredPosition.x;

            if (localIsCurrentlyRight != localShouldBeRight)
                SwapResultBlockPlacement(winnerBlock, loserBlock);
        }

        GameObject leftWinnerBox = null;
        GameObject rightWinnerBox = null;
        foreach (Transform child in resultManager.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "LeftWinnerBox")
                leftWinnerBox = child.gameObject;
            else if (child.name == "RightWinnerBox")
                rightWinnerBox = child.gameObject;
        }

        if (!hasWinner)
        {
            if (leftWinnerBox != null) leftWinnerBox.SetActive(false);
            if (rightWinnerBox != null) rightWinnerBox.SetActive(false);
            return;
        }

        bool winnerOnRight = resultManager.winnerAvatar != null &&
                             resultManager.loserAvatar != null &&
                             resultManager.winnerAvatar.transform.position.x >
                             resultManager.loserAvatar.transform.position.x;
        if (leftWinnerBox != null) leftWinnerBox.SetActive(!winnerOnRight);
        if (rightWinnerBox != null) rightWinnerBox.SetActive(winnerOnRight);
    }

    private static bool TryResolveResultBlocks(ResultManager resultManager,
        out RectTransform winnerBlock, out RectTransform loserBlock)
    {
        winnerBlock = null;
        loserBlock = null;
        if (resultManager.winnerAvatar == null || resultManager.loserAvatar == null)
            return false;

        List<Transform> winnerChain = new List<Transform>();
        for (Transform current = resultManager.winnerAvatar.transform;
             current != null; current = current.parent)
            winnerChain.Add(current);

        for (Transform loserNode = resultManager.loserAvatar.transform;
             loserNode != null; loserNode = loserNode.parent)
        {
            foreach (Transform winnerNode in winnerChain)
            {
                if (winnerNode == loserNode || winnerNode.parent == null ||
                    winnerNode.parent != loserNode.parent)
                    continue;

                winnerBlock = winnerNode as RectTransform;
                loserBlock = loserNode as RectTransform;
                return winnerBlock != null && loserBlock != null;
            }
        }

        return false;
    }

    private static void SwapResultBlockPlacement(RectTransform first, RectTransform second)
    {
        Vector2 anchorMin = first.anchorMin;
        Vector2 anchorMax = first.anchorMax;
        Vector2 pivot = first.pivot;
        Vector2 anchoredPosition = first.anchoredPosition;

        first.anchorMin = second.anchorMin;
        first.anchorMax = second.anchorMax;
        first.pivot = second.pivot;
        first.anchoredPosition = second.anchoredPosition;

        second.anchorMin = anchorMin;
        second.anchorMax = anchorMax;
        second.pivot = pivot;
        second.anchoredPosition = anchoredPosition;
    }

    [Server]
    private void ScheduleServerCleanup()
    {
        this.Delay(5f, () =>
        {
            if (MirrorNetwork.Instance != null && MirrorNetwork.Instance.edgegapAPIClient != null &&
                NetworkGameManager.Instance != null)
            {
                MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(
                    NetworkGameManager.Instance.currentServerRequestId);
            }
        });
    }

    [Server]
    private IEnumerator ServerCountdown()
    {
        remainingTime = 600;
        Debug.Log("Server Countdown Started." + remainingTime);
        while (remainingTime > 0)
        {
            if (!NetworkGameManager.Instance.IsPaused)
                remainingTime--; // SyncVar updates all clients automatically
            yield return new WaitForSeconds(1f);
        }
        ServerDeclareDraw();
    }

    // This runs on all clients whenever remainingTime changes
    private void OnTimeChanged(int oldTime, int newTime)
    {
        remainingminutes = Mathf.FloorToInt(newTime / 60);
        remainingseconds = Mathf.FloorToInt(newTime % 60);
        countDownGameTimer.text = string.Format("{0:00}:{1:00}", remainingminutes, remainingseconds);

        if (newTime > 180)
            countDownGameTimer.color = Color.green;
        else if (newTime > 60)
            countDownGameTimer.color = Color.white;
        else
            countDownGameTimer.color = Color.red;
    }

    [ClientRpc]
    private void RpcTimerEnded()
    {
        countDownGameTimer.text = "00:00";
        countDownGameTimer.color = Color.red;
        // _mainScript.NetworkWinner(winnerID, timerEnd: true);
        CmdGameWInloss();
    }
    [Command(requiresAuthority = false)]
    public void CmdGameWInloss()
    {
        Debug.LogWarning("[CarServerResult] Client draw/result request ignored; only the server settles Car races.");
    }
    [ClientRpc]
    public void RpcWinloss()
    {
        ShowServerDraw();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("✅ OnStartServer called - Server started");

        if (NetworkServer.active)
        {
            if (halfLapTrigger != null) halfLapTrigger.isTrigger = true;
            if (finishLapTrigger != null) finishLapTrigger.isTrigger = true;

            Debug.Log("🔄 Waiting for players to connect...");
            this.DelayUntil(() => NetworkServer.connections.Count >= 2, () =>
            {

                serverObservedTwoPlayers = true;
                Debug.Log($"✅ {NetworkServer.connections.Count} players connected, setting players...");
                this.Delay(4, () =>
                {
                   // Rpc_ShowPlayer();
                    if (countdownCoroutine != null)
                        StopCoroutine(countdownCoroutine);
                    countdownCoroutine = StartCoroutine(ServerCountdown());
                  //  ScheduleInitialTurnFromJoiner("players connected");
                });
            });
        }
    }
        private void Awake()
        {
            if (PlayerSelections.GameMode == 0) //Offline Mode
            {
                this.enabled = false;
                return;
            }
            Instance = this;
            ResultManager.GameSpawnedFinished = false;
            ResultManager.isGameFinished = false;
            // Subscribe to sync dictionary callbacks in Awake
            playersReady.OnChange += OnPlayersReadyChanged;
            playersData.OnChange += OnPlayersDataChanged;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (isLocalPlayer)
            {
                Debug.Log("Car Client Started Callback");

            }
        }
        public void Update()
        {
            if(halfLapTrigger.isTrigger==false)
            {
                halfLapTrigger.isTrigger = true;
            }

            if (NetworkServer.active)
            {
                if (finishLapTrigger != null && !finishLapTrigger.isTrigger)
                    finishLapTrigger.isTrigger = true;
            }
        }
        IEnumerator Start()
        {

            if (gameStarted)
            {
                gameFinished = false;
                countdownText.gameObject.SetActive(false);
                mobileControls.SetActive(false);
                waitingPanel.SetActive(true);
            }
            StartCoroutine(UpdatePing());
            StartCoroutine(StartTimer());

            leaveGameButton.onClick.RemoveAllListeners();
            mainMenuFinishButton.onClick.RemoveAllListeners();
            leaveGameButton.onClick.AddListener(LeaveGame);
            mainMenuFinishButton.onClick.AddListener(LeaveGame);
            yield return new WaitUntil (()=> MirrorPlayerPrefab.Instance &&  NetworkClient.ready);
            if (NetworkServer.active)
            {
                Debug.Log("Car Server Started");
            }
            else
            {
                Debug.Log("Car Client Started");
                NetworkGameManager.Instance.CmdSpawnCarRacing();
                CmdSetPlayerReady();
            }

        }
       
        // Command to mark player as ready
        [Command]
        void CmdSetPlayerReady()
        {
            if (!playersReady.ContainsKey(netId.GetHashCode()))
            {
                playersReady.Add(netId.GetHashCode(), true);

                string playerName = PlayerPrefs.GetString(PPConst.UserName, "Player");
                if (!playersData.ContainsKey(netId.GetHashCode()))
                {
                    playersData.Add(netId.GetHashCode(), new PlayerData
                    {
                        playerName = playerName,
                        isReady = true,
                        finishTime = 0f
                    });
                }
            }
        }

        // Command to submit finish time
        [Command]
        public void CmdSubmitFinishTime(float totalTime)
        {
            if (playersData.ContainsKey(netId.GetHashCode()))
            {
                PlayerData data = playersData[netId.GetHashCode()];
                data.finishTime = totalTime;
                playersData[netId.GetHashCode()] = data;
            }
        }

        // Callbacks for sync dictionary changes
        void OnPlayersReadyChanged(SyncIDictionary<int, bool>.Operation op, int key, bool item)
        {
            if (isLocalPlayer)
            {
                CheckPlayersReady();
            }
        }

        void OnPlayersDataChanged(SyncIDictionary<int, PlayerData>.Operation op, int key, PlayerData item)
        {
            if (isLocalPlayer)
            {
                CheckPlayersReady();
                TryShowResults();
            }
        }
        public static Dictionary<string, CarRecord> PlayersCarsData = new Dictionary<string, CarRecord>();
        public struct CarRecord
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
            public Quaternion Rotation;
            public int LapCount;
            public bool HasCrossedMiddleCheckPoint;
            public int CurrentWaypointIndex;
        }
        int spawnIndex = 0;

      public void SpawnVehicle(NetworkConnectionToClient conn)
{
    int connectionId = conn.connectionId;

    if (spawnIndex >= spawnLocations.Length)
        spawnIndex = spawnIndex % spawnLocations.Length;

    Vector3 spawnPos = spawnLocations[spawnIndex].position + Vector3.up;
    Quaternion spawnRot = spawnLocations[spawnIndex].rotation;
    spawnRot.x = 0f;
    spawnRot.z = 0f;

    int selectedCar = 0;
    spawnIndex++;

    GameObject vehicleObj = Instantiate(playerPrafab[selectedCar], spawnPos, spawnRot);
    Debug.Log("Car Spawned on server side");

    NetworkServer.Spawn(vehicleObj, conn);

    if (NetworkServer.connections.Count >= 2)
        serverObservedTwoPlayers = true;

    // ✅ Wait a frame so the Spawn message is sent before the TargetRpc
    StartCoroutine(SendSetupAfterSpawn(conn, vehicleObj));
}

private IEnumerator SendSetupAfterSpawn(NetworkConnectionToClient conn, GameObject vehicleObj)
{
    yield return null; // wait one frame

    // Guard: connection may have dropped in that frame
    if (conn == null || !conn.isReady)
    {
        Debug.LogWarning("Connection dropped before TargetSetupVehicle could be sent.");
        yield break;
    }

   NetworkGameManager.Instance.TargetSetupVehicle(conn, vehicleObj);
}

         private void OnEnable()
    {
             MirrorNetwork.OnWinCall += AnnounceWinner;
            MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
            // Add your event subscriptions here
        }

     private void OnDisable()
    {
            MirrorNetwork.OnWinCall -= AnnounceWinner;
            MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
            // Remove your event subscriptions here
    }
       

        public void DirectResult(bool result)
        {
            string winnerId = result
                ? staticVariables.UserProfiledata.user._id.ToString()
                : staticVariables.OpponetProfile.userId;
            ShowServerWinner(winnerId);
        }
        private void AnnounceWinner(string reason)
    {
            if (ResultManager.isGameFinished)
                return;

            Debug.Log("[CarServerResult] Existing disconnect flow completed; requesting server verification.");
            RequestExistingDisconnectResult();
    }
        public static bool IsGameQuite;
        public void LeaveGame()
        {
            IsGameQuite = true;

            if (NetworkClient.isConnected)
            {
                if (!isLeavingMultiplayer)
                    StartCoroutine(LeaveMultiplayerAfterForfeit());
            }
            else
            {
                SceneManager.LoadScene("Home");
            }
        }

        private IEnumerator LeaveMultiplayerAfterForfeit()
        {
            isLeavingMultiplayer = true;
            RequestLocalForfeit();

            // Allow the reliable forfeit Command to reach the server before the
            // owned vehicle and this scene are destroyed.
            yield return new WaitForSecondsRealtime(.5f);

            if (MirrorNetwork.Instance != null)
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            if (NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)DisconnectReason.ApplicationQuit);

            yield return new WaitForSecondsRealtime(.2f);

            RCC_camera.cameraMode = RCC_Camera.CameraMode.TPS;
            isGameLeft = true;
            leaveGameButton.gameObject.SetActive(false);
            finishPanel.SetActive(false);
            if (NetworkClient.active && Mirror.NetworkManager.singleton != null)
                Mirror.NetworkManager.singleton.StopClient();
            SceneManager.LoadScene("Home");
        }
        

        public override void OnStopClient()
        {
            base.OnStopClient();
            StopCoroutine(UpdatePing());

            if (isGameLeft)
            {
                loadingScreenManager.LoadScene("Home");
            }
            else
            {
                RCC_camera.cameraMode = RCC_Camera.CameraMode.TPS;
               // disconnectedPopUp.SetActive(true);
            }

            // Unsubscribe from callbacks
            playersReady.OnChange -= OnPlayersReadyChanged;
            playersData.OnChange -= OnPlayersDataChanged;
        }

        // Notify when a player leaves
        [ClientRpc]
        void RpcPlayerLeft(string playerName)
        {
            ShowNotification(playerName);
        }

        void ShowNotification(string playerName)
        {
            if (notificationText == null)
                notificationText = notificationPopup.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            notificationText.text = playerName + " has left the Race";
            notificationPopup.SetActive(true);
        }

        public void SetUpUIForCar(bool useDamage)
        {
            changeControlsButton.onClick.AddListener(ChangeControlType);
            currentControls = PlayerPrefs.GetInt(PPConst.ControlType);
            RCC_camera.TPSAutoReverse = System.Convert.ToBoolean(PlayerPrefs.GetInt(PPConst.ReverseCamera));
            repairButton.SetActive(useDamage);
        }

        void ChangeControlType()
        {
            currentControls += 1;
            currentControls = currentControls > 3 ? 0 : currentControls;

            switch (currentControls)
            {
                case 0:
                    RCC.SetMobileController(RCC_Settings.MobileController.TouchScreen);
                    break;
                case 1:
                    RCC.SetMobileController(RCC_Settings.MobileController.Joystick);
                    break;
                case 2:
                    RCC.SetMobileController(RCC_Settings.MobileController.SteeringWheel);
                    break;
                case 3:
                    RCC.SetMobileController(RCC_Settings.MobileController.Gyro);
                    break;
            }
            PlayerPrefs.SetInt(PPConst.ControlType, currentControls);
        }

        void CheckPlayersReady()
        {
            if (gameStarted)
                return;

            foreach (TextMeshProUGUI playerNames in playerNameTexts)
                playerNames.text = "";

            int index = 0;
            foreach (var kvp in playersData)
            {
                if (index < playerNameTexts.Length)
                {
                    string readyStatus = kvp.Value.isReady ? "Ready!" : "Not Ready!";
                    playerNameTexts[index].text = kvp.Value.playerName + "  :  " + readyStatus;
                    index++;
                }
            }

            bool allReady = CheckIfAllPlayerReady();
            waitingPanel.SetActive(false);
            countdownText.gameObject.SetActive(allReady);

            if (allReady)
                matchStartTimer = 2;
        }

        IEnumerator StartTimer()
        {
            if (gameStarted)
            {
                mobileControls.SetActive(true);
                waitingPanel.SetActive(false);
                countdownText.gameObject.SetActive(false);
            }
            else
            {
                while (matchStartTimer > 0)
                {
                    matchStartTimer -= 1;
                    matchStartTimerText.text = "Race Starts In : 00:" + matchStartTimer;
                    countdownText.text = matchStartTimer.ToString();
                    yield return new WaitForSeconds(1f);
                }
                gameStarted = true;
                waitingPanel.SetActive(false);
                countdownText.gameObject.SetActive(false);
                mobileControls.SetActive(true);
                EventManager.OnMultiplayergameStarted?.Invoke();
            }
        }

        void TryShowResults()
        {
            "1".Show("Finish");
            if (/*!isLocalPlayer ||*/ gameFinished)
                return;
            "2".Show("Finish");

            //if (!CheckIfAllPlayersSubmitResults())
            //    return;
            3.Show();
            gameFinished = true;
            StartCoroutine(winPlayer());
            
            List<PlayerPositionData_OnlineMode> playersDataForPositions = new List<PlayerPositionData_OnlineMode>();

            foreach (var kvp in playersData)
            {
                if (kvp.Value.finishTime > 0)
                {
                    playersDataForPositions.Add(new PlayerPositionData_OnlineMode(kvp.Value.playerName, kvp.Value.finishTime));
                }
            }

            playersDataForPositions.Sort((a, b) => a.totalTime.CompareTo(b.totalTime));
            DisplayResult(playersDataForPositions);
            //Invoke(nameof(ShowScoreBoard), 5);
            mainMenuFinishButton.onClick.RemoveAllListeners();
            mainMenuFinishButton.onClick.AddListener(LeaveGame);
            4.Show();
            GiveRewardToUser(playersDataForPositions);
        }

        void DisplayResult(List<PlayerPositionData_OnlineMode> playersDataForPositions)
        {
            // Disable All Entries first
            for (int i = 0; i < positionLabelTexts.Length; i++)
            {
                positionLabelTexts[i].gameObject.SetActive(false);
                playerNamePositionsTexts[i].gameObject.SetActive(false);
                totalTimeTexts[i].gameObject.SetActive(false);
                rewardTexts[i].gameObject.SetActive(false);
            }

            // Fill Entries
            int position = 0;
            int reward = 150 * playersDataForPositions.Count;

            foreach (PlayerPositionData_OnlineMode playerData in playersDataForPositions)
            {
                if (position < positionLabelTexts.Length)
                {
                    positionLabelTexts[position].gameObject.SetActive(true);
                    playerNamePositionsTexts[position].gameObject.SetActive(true);
                    totalTimeTexts[position].gameObject.SetActive(true);
                    rewardTexts[position].gameObject.SetActive(true);

                    playerNamePositionsTexts[position].text = playerData.playerName;
                    totalTimeTexts[position].text = System.Math.Round(playerData.totalTime, 3).ToString();
                    rewardTexts[position].text = (reward / (position + 1)).ToString();
                    position++;
                }
            }
        }

        void ShowScoreBoard()
        {
          //  FinishPopLocalPlayer.SetActive(false);
         //   finishPanel.SetActive(true);
        }

        void GiveRewardToUser(List<PlayerPositionData_OnlineMode> playersData)
        {
            int playerPosition = playersData.FindIndex(playerData =>
                playerData.playerName == PlayerPrefs.GetString(PPConst.UserName));
            playerPosition.Show();
            if (playerPosition >= 0)
            {
                SetLocalPlayerData(playerPosition + 1);
            }
        }

        void SetLocalPlayerData(int position)
        {
            stars[0].SetActive(false);
            stars[1].SetActive(false);
            stars[2].SetActive(false);

            switch (position)
            {
                case 1:
                    stars[0].SetActive(true);
                    stars[1].SetActive(true);
                    stars[2].SetActive(true);
                    playerPositiontext.text = "1st";
                    remarksText.text = "Beat The Opponents";

                    break;
                case 2:
                    stars[0].SetActive(true);
                    stars[1].SetActive(true);
                    playerPositiontext.text = "2nd";
                    remarksText.text = "Runner Up!";
                    break;
                case 3:
                    stars[0].SetActive(true);
                    playerPositiontext.text = "3rd";
                    remarksText.text = "Good Race! Try Again";
                    break;
                case 4:
                    stars[0].SetActive(true);
                    playerPositiontext.text = "4th";
                    remarksText.text = "Need improvement! Keep Practicing";
                    break;
            }
        }
        //IEnumerator MultiplayerDealyWin(int PlayerId)
        //{
        //    yield return new WaitForSeconds(3f);
        //    ResultManagerForHighwayRacing.Notification.SetActive(false);
        //    ResultManagerForHighwayRacing.blurview.enabled = false;
        //    Myplayer.CmdPlayerFinished(PlayerId);// staticVariables.UserProfiledata.user._id);

        //}
        public GameObject Notification;
        public BlurOptimized blurview;
        public IEnumerator winPlayer()
        {
          
         //   Notification.SetActive(false);
         //   blurview.enabled = true;
            Debug.Log("WinPlayer Called");
            yield return new WaitForSeconds(0.1f);
          //  Notification.SetActive(false);
           // blurview.enabled = false;
            Debug.Log("[CarServerResult] Local finish presentation complete; waiting for the server result.");
            //if (ResultManager.GameSpawnedFinished == false)
            //{
            //    ResultManager.GameSpawnedFinished = true;
            //    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

            //    if (enemyPrefab != null)
            //    {
            //        "1".Show();
            //        // Spawn at position (0,0,0)
            //        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
            //        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, staticVariables.UserProfiledata.user._id.ToString());
            //    }
            //    else
            //    {
            //        Debug.LogError("WinLose GameManager prefab not found!");
            //    }
            //}
            //this.Delay(1, () =>
            //{
            //    MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
            //});
        }
         [Command(requiresAuthority = false)]
        public void CmdHandleGameResult(bool IsWin, string PlayerId)
        {
            Debug.LogWarning("[CarServerResult] Legacy client result ignored; the Car server derives the winner.");
        }
        [ClientRpc]
        public void RpcHandleGameResultAlt(bool IsWin, string PlayerId)
        {
            Debug.LogWarning("[CarServerResult] Legacy result RPC ignored; waiting for the authoritative server result.");
        }
        private bool CheckIfAllPlayerReady()
        {
            foreach (var kvp in playersData)
            {
                if (!kvp.Value.isReady)
                    return false;
            }
            return playersData.Count > 0;
        }

        private bool CheckIfAllPlayersSubmitResults()
        {
            foreach (var kvp in playersData)
            {
                if (kvp.Value.finishTime <= 0)
                    return false;
            }
            return playersData.Count > 0;
        }

        public void ShowFinishCinematics()
        {
           // FinishPopLocalPlayer.SetActive(true);
            TryShowResults();
            int rand = Random.Range(10, 100);

            if (rand % 2 == 0)
            {
                RCC_camera.useFixedCameraMode = true;
                RCC_camera.cameraMode = RCC_Camera.CameraMode.FIXED;
            }
            else
            {
                RCC_camera.useCinematicCameraMode = true;
                RCC_camera.cameraMode = RCC_Camera.CameraMode.CINEMATIC;
            }
            Invoke(nameof(ShowPopUp), 6);
        }

        void ShowPopUp()
        {
            RCC_camera.cameraMode = RCC_Camera.CameraMode.TPS;
        }

        void GiveReward()
        {
            doubleRewardButton.interactable = false;
            rewardPopUp.SetActive(true);
            int doubleReward = 2 * userCoins;
            totalRewardText.text = " Congratulations!!\n You Got $" + doubleReward;
            UserData.SubtractCoins(userCoins);
            UserData.AddCoins(doubleReward);
        }

        IEnumerator UpdatePing()
        {
            while (NetworkClient.isConnected)
            {
                if (NetworkTime.rtt > 0)
                {
                    float ping = (float)NetworkTime.rtt * 1000f;
                    pingText.text = Mathf.RoundToInt(ping) + " ms";

                    // Color code based on ping
                    if (ping < 50)
                        pingIcon.color = Color.green;
                    else if (ping < 100)
                        pingIcon.color = Color.yellow;
                    else
                        pingIcon.color = Color.red;
                }
                yield return new WaitForSeconds(1f);
            }
        }

        public void WaitingForOpponentt(bool state)
        {
            PopupMessageManager.instance.SetPanelStaus(state, "Opponent Connection Lost");
        }

        public void AnnounceWinner()
        {
            //if (TimerCorotine != null)
            //    StopCoroutine(TimerCorotine);
           
              RequestExistingDisconnectResult();
        }

        public void Announcelosser()
        {
              Debug.LogWarning("[CarServerResult] Legacy client loser declaration ignored.");
        }
    }

    // Struct for player data (must be serializable for Mirror SyncDictionary)
    [System.Serializable]
    public struct PlayerData
    {
        public string playerName;
        public bool isReady;
        public float finishTime;
    }

    public class PlayerPositionData_OnlineMode
    {
        public string playerName;
        public float totalTime;

        public PlayerPositionData_OnlineMode(string PN, float TT)
        {
            playerName = PN;
            totalTime = TT;
        }
    }
}
