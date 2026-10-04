using CarRace;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityExtensions;

public class HR_NetworkManager : NetworkBehaviour //Photon Removal : MonoBehaviourPunCallbacks
{
    public GameObject masterCar; // Sirf ek car prefab
    public Transform[] spawnPoints; // Multiple spawn points array

    public delegate void onPlayerSpawned(HR_PlayerHandler player);
    public static event onPlayerSpawned OnPlayerSpawned;
    public HR_GamePlayHandler hR_GamePlayHandler;
    public static HR_NetworkManager Instance { get; private set; }
    public static bool hasHighWayGameStarted = false;
    private int spawnIndex = 0;
 
    [Header("UI Timer")]
    public TextMeshProUGUI countDownGameTimer;

    [Header("Countdown Settings")]
    [SyncVar(hook = nameof(OnTimeChanged))]
    public int remainingTime = 600; // server owns this

    public int remainingminutes;
    public int remainingseconds;

    private Coroutine countdownCoroutine;
    private Coroutine initialTurnCoroutine;
    private bool serverResultDeclared;

    [Server]
    private bool TryResolveWinner(NetworkIdentity participant, bool participantWon, out string winnerId)
    {
        winnerId = string.Empty;

        NetworkGameManager networkManager = NetworkGameManager.Instance;
        NetworkConnectionToClient connection = participant != null ? participant.connectionToClient : null;
        if (networkManager == null || connection == null ||
            networkManager.creatorData == null || networkManager.joinerData == null)
        {
            Debug.LogWarning("[HighwayServerResult] Player mapping failed: manager, connection, or player data is missing.");
            return false;
        }

        string participantId;
        if (networkManager.CreatorRef != null &&
            connection.connectionId == networkManager.CreatorRef.connectionId)
            participantId = networkManager.creatorData.playerId;
        else if (networkManager.JoinerRef != null &&
                 connection.connectionId == networkManager.JoinerRef.connectionId)
            participantId = networkManager.joinerData.playerId;
        else
        {
            Debug.LogWarning($"[HighwayServerResult] Unregistered car connection rejected: {connection.connectionId}.");
            return false;
        }

        if (participantWon)
            winnerId = participantId;
        else if (participantId == networkManager.creatorData.playerId)
            winnerId = networkManager.joinerData.playerId;
        else if (participantId == networkManager.joinerData.playerId)
            winnerId = networkManager.creatorData.playerId;

        if (string.IsNullOrEmpty(winnerId))
        {
            Debug.LogWarning($"[HighwayServerResult] Winner ID could not be resolved for connection {connection.connectionId}.");
            return false;
        }

        return true;
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
            Debug.LogWarning("[HighwayServerResult] Forfeit rejected: sender connection is missing.");
            return;
        }

        for (int i = 0; i < HighwayCarNetwork.Cars.Count; i++)
        {
            HighwayCarNetwork car = HighwayCarNetwork.Cars[i];
            NetworkIdentity identity = car != null ? car.netIdentity : null;
            if (identity == null || identity.connectionToClient != sender)
                continue;

            Debug.Log($"[HighwayServerResult] Player forfeited from pause menu, connection={sender.connectionId}.");
            ServerDeclareLoser(identity);
            return;
        }

        Debug.LogWarning($"[HighwayServerResult] Forfeit rejected: no Highway car belongs to connection {sender.connectionId}.");
    }

    [Server]
    private bool ServerDeclareResult(NetworkIdentity participant, bool participantWon)
    {
        if (serverResultDeclared)
            return false;
        if (ApiAndRoomManager._instance == null)
        {
            Debug.LogError("[HighwayServerResult] ApiAndRoomManager is missing on the server.");
            return false;
        }
        if (!TryResolveWinner(participant, participantWon, out string winnerId))
            return false;

        NetworkGameManager networkManager = NetworkGameManager.Instance;
        if (string.IsNullOrEmpty(networkManager.transactionId))
        {
            Debug.LogError("[HighwayServerResult] Transaction ID is empty; result was not submitted.");
            return false;
        }

        serverResultDeclared = true;
        Debug.Log($"[HighwayServerResult] Server decided winner={winnerId}, connection={participant.connectionToClient.connectionId}.");
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

    [ClientRpc]
    private void RpcShowServerWinner(string winnerId)
    {
        StartCoroutine(ShowServerWinnerAfterDelay(winnerId));
    }

    private IEnumerator ShowServerWinnerAfterDelay(string winnerId)
    {
        TriggerEvent finishTrigger = TriggerEvent.instance;
        if (finishTrigger != null && finishTrigger.ResultImage != null)
        {
            bool localPlayerWon = winnerId == staticVariables.UserProfiledata.user._id.ToString();
            finishTrigger.ResultImage.sprite = localPlayerWon
                ? finishTrigger.WinImage
                : finishTrigger.LoseImage;
            finishTrigger.ResultImage.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(3f);

        if (finishTrigger != null && finishTrigger.ResultImage != null)
            finishTrigger.ResultImage.gameObject.SetActive(false);

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
        hasHighWayGameStarted = false;
        HighWayCarData.Clear();
        MirrorNetwork.winnerID = winnerId;

        bool localPlayerWon = winnerId == staticVariables.UserProfiledata.user._id.ToString();
        resultManager.finishPanelUI.SetActive(true);
        resultManager.rematchButton.SetActive(true);
        resultManager.winnerText.SetActive(true);
        resultManager.loserText.SetActive(true);
        resultManager.drawText.SetActive(false);
        SetWinnerBox(resultManager, true, localPlayerWon);
        resultManager.UpdateResultSummary(localPlayerWon);
        NetworkGameManager.OnRematchRecived += resultManager.RematchRequestRecived;

        if (localPlayerWon)
        {
            resultManager.InitializeWinnerUI(staticVariables.ProfilePicture,
                staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
            resultManager.InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
        }
        else
        {
            resultManager.InitializeLoserUI(staticVariables.ProfilePicture,
                staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
            resultManager.InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
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
        hasHighWayGameStarted = false;
        HighWayCarData.Clear();

        resultManager.winnerText.SetActive(false);
        resultManager.loserText.SetActive(false);
        resultManager.drawText.SetActive(true);
        SetWinnerBox(resultManager, false, true);
        resultManager.UpdateResultSummary(true, isDraw: true);
        resultManager.chest.SetActive(false);
        resultManager.finishPanelUI.SetActive(true);
        resultManager.InitializeWinnerUI(staticVariables.ProfilePicture,
            staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
        resultManager.InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
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

            // Auto uses ResultManager's prefab fallback side, which is Left.
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
        //ApiAndRoomManager._instance.WinnerLossChallenge(BEKStudio.GameController.Instance.gameWinner);
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
        Debug.LogWarning("Highway result request ignored: only the server can settle the race.");
    }
    [ClientRpc]
    public void RpcWinloss()
    {
        ShowServerDraw();
    }
    private void Awake()
    {
        Instance = this;
        ResultManager.GameSpawnedFinished = false;
        ResultManager.isGameFinished = false;

        //	HighwaySunny (single player) me RCC_SceneManager ka registerFirstVehicleAsPlayer off hai,
        //	HighwayNight me on. On hone par RCC har spawn hone wali car ko "player" register kar deta
        //	hai - yani opponent ki car spawn hote hi camera us par chala jata tha. RegisterPlayer ke
        //	andar SetTarget naya CameraTarget banata hai, is liye hood camera bhi opponent ki car se
        //	resolve hota tha, aur ResetCamera chuna hua camera mode wapas TPS par le aata tha.
        //	Camera sirf apni car par rehna chahiye - usay Target_AssignCar khud register karta hai.
        if (RCC_SceneManager.Instance != null)
            RCC_SceneManager.Instance.registerFirstVehicleAsPlayer = false;
    }
    
    private void OnDisable()
    {
        MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
    }

    private void OnEnable()
    {
        MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
    }

    public void DirectResult(bool result)
    {
        string winnerId = result
            ? staticVariables.UserProfiledata.user._id.ToString()
            : staticVariables.OpponetProfile.userId;
        ShowServerWinner(winnerId);
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("✅ OnStartServer called - Server started");

        if (NetworkServer.active)
        {
            Debug.Log("🔄 Waiting for players to connect...");
            this.DelayUntil(() => NetworkServer.connections.Count >= 2, () =>
            {

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
    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log("🎮 Local player requesting spawn...");
        CmdHighWaySpawn();
    }

    [Command(requiresAuthority = false)]
    public void CmdHighWaySpawn(NetworkConnectionToClient sender = null)
    {
        // Har player ko apni lane milti hai (side by side spawn).
        int assignedLane = spawnPoints != null && spawnPoints.Length > 0 ? spawnIndex % spawnPoints.Length : 0;

        // Get spawn position aur rotation
        Vector3 spawnPos = GetSpawnPosition();
        Quaternion spawnRot = GetSpawnRotation();

        // Sirf master car spawn karen
        GameObject car = Instantiate(masterCar, spawnPos, spawnRot);

        // Lane info SyncVars me Spawn se pehle set karo, taake clients ko spawn payload me hi mil jaye.
        HighwayCarNetwork carNetwork = car.GetComponent<HighwayCarNetwork>();

        if (carNetwork != null)
        {
            carNetwork.laneIndex = assignedLane;
            carNetwork.resetLaneX = spawnPos.x;
        }

        NetworkServer.Spawn(car, sender);

        Target_AssignCar(sender, car);
        OnPlayerSpawned?.Invoke(car.GetComponent<HR_PlayerHandler>());

        // Check if both players ready.
        //	Race pehle se chal rahi ho - yani ye rejoin hai - to countdown dobara nahi chalta,
        //	hasHighWayGameStarted true reh chuka hota hai. Us surat me RpcStartCountdown bhejna
        //	bhi ghalat hai kyunke wo opponent ka countdown bhi dobara shuru kar deta. Sirf rejoin
        //	karne wale client ko chalti hui race ki state bhej do.
        if (hasHighWayGameStarted)
            Target_ResumeRace(sender);
        else
            CheckAllPlayersReadyAndStartCountdown();
    }

    [Server]
    void CheckAllPlayersReadyAndStartCountdown()
    {
        int spawnedPlayers = FindObjectsOfType<HR_PlayerHandler>().Length;

        Debug.Log($"Spawned players: {spawnedPlayers}");

        if (spawnedPlayers >= 2 && !hasHighWayGameStarted)
        {
            Debug.Log("🎬 All players ready - starting countdown");
            hasHighWayGameStarted = true;
            RpcStartCountdown();
        }
    }

    [ClientRpc]
    private void RpcStartCountdown()
    {
        if (HR_GamePlayHandler.Instance != null)
        {
            StartCoroutine(HR_GamePlayHandler.Instance.ServerStartRaceDelayed());
           
            Debug.Log("✅ Countdown started on client!");
        }
        else
        {
            Debug.LogError("❌ HR_GamePlayHandler.Instance is null!");
        }
    }

    /// <summary>
    /// Reconnect ke baad race already chal rahi hoti hai. RpcStartCountdown sirf ek dafa chalta
    /// hai, is liye rejoin karne wale client par gameStarted false reh jata tha aur gameplay
    /// button wapas nahi aata tha - car wahin khari reh jati thi. Ye sirf usi client ko race ki
    /// chalti hui state deta hai, opponent ko chhere baghair.
    /// </summary>
    [TargetRpc]
    private void Target_ResumeRace(NetworkConnection target)
    {
        if (HR_GamePlayHandler.Instance == null)
        {
            Debug.LogError("HR_GamePlayHandler.Instance is null on reconnect!");
            return;
        }

        HR_GamePlayHandler.Instance.ResumeRaceAfterReconnect();
        Debug.Log("Race state restored on reconnected client.");
    }

    [TargetRpc]
    private void Target_AssignCar(NetworkConnection target, GameObject car)
    {
        var controller = car.GetComponent<RCC_CarControllerV3>();
        var playerHandler = car.GetComponent<HR_PlayerHandler>();

        if (controller != null)
        {
            RCC.RegisterPlayerVehicle(controller);
            RCC.SetControl(controller, true);

            if (!controller.engineRunning)
                controller.StartEngine();
        }

        // Assign to GamePlayHandler for UI updates
        if (HR_GamePlayHandler.Instance != null && playerHandler != null)
        {
            HR_GamePlayHandler.Instance.player = playerHandler;
            Debug.Log("✅ Player assigned to GamePlayHandler - UI should now work!");
        }

        // Trigger camera's player spawned event
        if (HR_CarCamera.Instance != null && playerHandler != null)
        {
            HR_CarCamera.Instance.OnPlayerSpawned(playerHandler);
        }

        OnPlayerSpawned?.Invoke(playerHandler);

        if (playerHandler != null)
        {
            playerHandler.enabled = true;
        }
    }

    // ----- Spawn Position Helpers (Horse wali tarah) -----
    Vector3 GetSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Vector3.zero;

        Vector3 pos = spawnPoints[spawnIndex % spawnPoints.Length].position;
        spawnIndex++;
        return pos + Vector3.up * 0.5f;
    }

    Quaternion GetSpawnRotation()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Quaternion.identity;

        Quaternion rot = spawnPoints[(spawnIndex - 1 + spawnPoints.Length) % spawnPoints.Length].rotation;
        rot.x = 0f;
        rot.z = 0f;
        return rot;
    }

    // Traffic vehicle methods
    [Server]
    public void EnableTrafficvehicle(GameObject trafficVehicle)
    {
        RpcEnableTrafficVehicle(trafficVehicle);
    }

    [Server]
    public void DisableTrafficvehicle(GameObject trafficVehicle)
    {
        RpcDisableTrafficVehicle(trafficVehicle);
    }

    [ClientRpc]
    private void RpcEnableTrafficVehicle(GameObject trafficVehicle)
    {
        trafficVehicle.SetActive(true);
    }

    [ClientRpc]
    private void RpcDisableTrafficVehicle(GameObject trafficVehicle)
    {
        trafficVehicle.SetActive(false);
    }

    void Start()
    {
        PlayerPrefs.SetInt("Multiplayer", 1);
        if (NetworkServer.active)
        {
            Debug.Log("Car Server Started");
        }
        else
        {
            Debug.Log("Car Client Started");
        }
    }

    public static Dictionary<string, HighWayCarRecord> HighWayCarData = new Dictionary<string, HighWayCarRecord>();

    public struct HighWayCarRecord
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public Quaternion Rotation;
        public float Score;
    }
    //public GameObject masterCar, clientCar;
    //public Transform clientSpawnPosition, masterSpawnPosition;
    //public delegate void onPlayerSpawned(HR_PlayerHandler player);
    //public static event onPlayerSpawned OnPlayerSpawned;
    //public HR_GamePlayHandler hR_GamePlayHandler;
    //public static HR_NetworkManager Instance { get; private set; }
    //private int playerCount = 0;

    //private void Awake()
    //{
    //    // Singleton pattern
    //    //Instance = this;
    //    //  if (Instance != null && Instance != this)
    //    // {
    //    //Photon Removal Destroy(gameObject);
    //    //    return;
    //    // }

    //    Instance = this;

    //}
    //public override void OnStartClient()
    //{
    //    base.OnStartClient();

    //    // Only the local player should send spawn request
    //    // if(isOwned)
    //    {
    //        Debug.Log("🎮 Local player requesting spawn...");
    //        CmdHighWaySpawn();
    //    }



    //}

    //[Command(requiresAuthority = false)]
    //public void CmdHighWaySpawn(NetworkConnectionToClient sender = null)
    //{
    //    playerCount++;

    //    GameObject prefabToSpawn;
    //    Transform spawnPos;

    //    if (playerCount == 1)
    //    {
    //        prefabToSpawn = masterCar;
    //        spawnPos = masterSpawnPosition;
    //    }
    //    else
    //    {
    //        prefabToSpawn = clientCar;
    //        spawnPos = clientSpawnPosition;
    //    }

    //    GameObject car = Instantiate(prefabToSpawn, spawnPos.position, spawnPos.rotation);
    //    NetworkServer.Spawn(car, sender);

    //    Target_AssignCar(sender, car);
    //    OnPlayerSpawned?.Invoke(car.GetComponent<HR_PlayerHandler>());
    //    RpcStartCountdown();
    //}
    //[ClientRpc]
    //private void RpcStartCountdown()
    //{
    //    if (HR_GamePlayHandler.Instance != null)
    //    {
    //        StartCoroutine(HR_GamePlayHandler.Instance.ServerStartRaceDelayed());
    //        Debug.Log("✅ Countdown started on client!");
    //    }
    //    else
    //    {
    //        Debug.LogError("❌ HR_GamePlayHandler.Instance is null!");
    //    }
    //}

    //[TargetRpc]
    //private void Target_AssignCar(NetworkConnection target, GameObject car)
    //{
    //    var controller = car.GetComponent<RCC_CarControllerV3>();
    //    var playerHandler = car.GetComponent<HR_PlayerHandler>();

    //    if (controller != null)
    //    {
    //        RCC.RegisterPlayerVehicle(controller);
    //        RCC.SetControl(controller, true);

    //        // Start the engine if not running
    //        if (!controller.engineRunning)
    //            controller.StartEngine();
    //    }

    //    // ✅ CRITICAL FIX: Assign to GamePlayHandler for UI updates
    //    if (HR_GamePlayHandler.Instance != null && playerHandler != null)
    //    {
    //        HR_GamePlayHandler.Instance.player = playerHandler;
    //        Debug.Log("✅ Player assigned to GamePlayHandler - UI should now work!");
    //    }
    //    else
    //    {
    //        Debug.LogError("❌ Failed to assign player to GamePlayHandler!");
    //    }

    //    // Trigger camera's player spawned event
    //    if (HR_CarCamera.Instance != null && playerHandler != null)
    //    {
    //        HR_CarCamera.Instance.OnPlayerSpawned(playerHandler);
    //    }

    //    // Trigger event for other systems
    //    OnPlayerSpawned?.Invoke(playerHandler);

    //    // Make sure player handler is enabled
    //    if (playerHandler != null)
    //    {
    //        playerHandler.enabled = true;
    //    }
    //}
    //public static Dictionary<string, HighWayCarRecord> HighWayCarData = new Dictionary<string, HighWayCarRecord>();
    //public struct HighWayCarRecord
    //{
    //    public Vector3 Position;
    //    public Vector3 Velocity;
    //    public Vector3 AngularVelocity;
    //    public Quaternion Rotation;
    //}
    //[Server]
    //public void EnableTrafficvehicle(GameObject trafficVehicle)
    //{
    //    RpcEnableTrafficVehicle(trafficVehicle);
    //}

    //[Server]
    //public void DisableTrafficvehicle(GameObject trafficVehicle)
    //{
    //    RpcDisableTrafficVehicle(trafficVehicle);
    //}

    //[ClientRpc]
    //private void RpcEnableTrafficVehicle(GameObject trafficVehicle)
    //{
    //    trafficVehicle.SetActive(true);
    //}

    //[ClientRpc]
    //private void RpcDisableTrafficVehicle(GameObject trafficVehicle)
    //{
    //    trafficVehicle.SetActive(false);
    //}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //  void Start() // Your existing Start method
    //  {
    //  PlayerPrefs.SetInt("Multiplayer", 1);
    //  if (NetworkServer.active)
    // {
    //    Debug.Log("Car Server Started");
    //    return;
    //   }
    //   else
    //   {
    //    Debug.Log("Car Client Started");

    //     }
    // Spawn the vehicle for this player


    //GameObject selectedCar = PhotonNetwork.IsMasterClient ? masterCar : clientCar;
    //Transform spawnPosition = PhotonNetwork.IsMasterClient ? masterSpawnPosition : clientSpawnPosition;

    //GameObject spawnedCar = PhotonNetwork.Instantiate(selectedCar.name, spawnPosition.position, Quaternion.identity);
    //var controller = spawnedCar.GetComponent<RCC_CarControllerV3>();

    //// *** ADD THIS LINE - Register with Race Progress UI ***
    //RegisterCarWithProgressUI(spawnedCar);                                                                                                                                                 //Photon Removal

    //RCC.RegisterPlayerVehicle(controller);

    //if (controller.GetComponent<PhotonView>().IsMine)
    //{
    //    RCC.SetControl(controller, true);
    //    HR_GamePlayHandler.Instance.player = spawnedCar.GetComponent<HR_PlayerHandler>();
    //    if (RCC_SceneManager.Instance.activePlayerCamera)
    //        RCC_SceneManager.Instance.activePlayerCamera.SetTarget(spawnedCar);
    //}

    //OnPlayerSpawned?.Invoke(spawnedCar.GetComponent<HR_PlayerHandler>());
    //StartCoroutine(HR_GamePlayHandler.Instance.StartRaceDelayed());
    // }

    // *** ADD THIS NEW METHOD ***
    // void RegisterCarWithProgressUI(GameObject spawnedCar)
    // {
    //// Option 1: If your car has CarController component
    //CarController carController = spawnedCar.GetComponent<CarController>();
    //if (carController != null && RaceProgressUI.Instance != null)
    //{
    //    RaceProgressUI.Instance.RegisterCar(carController);
    //    Debug.Log($"Registered car with progress UI: {spawnedCar.name}");
    //    return;
    //}                                                                                                                                                                                                                                  //Photon Removal

    //// Option 2: If you don't have CarController, create a simple wrapper
    //CarController newCarController = spawnedCar.GetComponent<CarController>();
    //if (newCarController == null)
    //{
    //    newCarController = spawnedCar.AddComponent<CarController>();
    //}

    //if (RaceProgressUI.Instance != null)
    //{
    //    RaceProgressUI.Instance.RegisterCar(newCarController);
    //    Debug.Log($"Added CarController and registered: {spawnedCar.name}");
    //}
    //else
    //{
    //    Debug.LogError("RaceProgressUI.Instance is null! Make sure RaceProgressUI is in the scene.");
    //}
    // }

    //   public void EnableTrafficvehicle(GameObject trafficVehicle)
    //   {
    //if (PhotonNetwork.IsMasterClient && trafficVehicle != null)
    //{
    //    PhotonView pv = trafficVehicle.GetComponent<PhotonView>();
    //    if (pv != null)                                                                                                                                                                                //Photon Removal
    //    {
    //        pv.RPC("RPC_EnableTrafficVehicle", RpcTarget.AllBuffered);
    //    }
    //}
    //   }

    /// <summary>
    /// Disables a traffic vehicle on all clients.
    /// </summary>
    //   public void DisableTrafficvehicle(GameObject trafficVehicle)
    //    {
    //if (PhotonNetwork.IsMasterClient && trafficVehicle != null)
    //{
    //    PhotonView pv = trafficVehicle.GetComponent<PhotonView>();
    //    if (pv != null)
    //    {                                                                                                                                                                    //Photon Removal
    //        pv.RPC("RPC_DisableTrafficVehicle", RpcTarget.AllBuffered);
    //    }
    //}
    //   }


    //[PunRPC]
    //void RPC_EnableTrafficVehicle()                                                                                                                          //Photon Removal
    //{
    //    gameObject.SetActive(true);
    //}

    //[PunRPC]
    //void RPC_DisableTrafficVehicle()
    //{                                                                                                                                                                                 //Photon Removal
    //    gameObject.SetActive(false);
    //}
}
