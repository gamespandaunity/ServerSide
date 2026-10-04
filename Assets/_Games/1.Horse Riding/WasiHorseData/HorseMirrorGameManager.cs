using UnityEngine;
using Mirror;
using MalbersAnimations;
using UnityStandardAssets.Utility;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityExtensions;
using TMPro;

public class HorseMirrorGameManager : NetworkBehaviour
{
    [Header("Available Horse Prefabs")]
    public Animal[] availableMounts;

    [Header("References")]
    public HorseSmoothFollow thirdPersonCameraFollow;


    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private int selectedMountIndex = 0;
    private int spawnIndex = 0;

    public static HorseMirrorGameManager Instance;
    public GameObject PlayerMobileButton;
     [Header("UI Timer")]
    public TextMeshProUGUI countDownGameTimer;

    [Header("Countdown Settings")]
    [SyncVar(hook = nameof(OnTimeChanged))]
    public int remainingTime = 600; // server owns this

    public int remainingminutes;
    public int remainingseconds;

    private Coroutine countdownCoroutine;
    private Coroutine initialTurnCoroutine;

    private const float ServerFinishTolerance = 35f;
    private bool serverResultDeclared;
    private bool serverObservedTwoPlayers;

    //	Server side. "3 2 1 GO" poora ho chuka? Isi se pata chalta hai ke naya spawn countdown ka
    //	hissa hai ya reconnect.
    private bool countdownFinished = false;

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
        RpcTimerEnded();
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
    }
    [Command(requiresAuthority = false)]
    public void CmdGameWInloss()
    {
        Debug.LogWarning("[HorseServerResult] Legacy client draw request ignored; the server timer owns the result.");
    }
    [ClientRpc]
    public void RpcWinloss()
    {
        ShowServerDraw();
    }

    [Server]
    private bool TryResolveParticipant(NetworkIdentity participant, out string participantId)
    {
        participantId = string.Empty;
        NetworkGameManager networkManager = NetworkGameManager.Instance;
        NetworkConnectionToClient connection = participant != null ? participant.connectionToClient : null;
        if (networkManager == null || connection == null ||
            networkManager.creatorData == null || networkManager.joinerData == null)
        {
            Debug.LogWarning("[HorseServerResult] Player mapping failed: manager, connection, or player data is missing.");
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
            Debug.LogWarning($"[HorseServerResult] Unregistered horse connection rejected: {connection.connectionId}.");
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
            Debug.LogWarning("[HorseServerResult] Winner ID could not be resolved.");
            return false;
        }

        return true;
    }

    [Server]
    private bool ValidateServerFinish(NetworkIdentity participant)
    {
        if (participant == null || participant.connectionToClient == null)
        {
            Debug.LogWarning("[HorseServerResult] Finish rejected: participant or connection is missing.");
            return false;
        }

        PlayerPositionController progress = participant.GetComponent<PlayerPositionController>();
        if (progress == null || progress.isAI)
        {
            Debug.LogWarning("[HorseServerResult] Finish rejected: a human Horse player was not found.");
            return false;
        }

        Collider finishTrigger = FinishPointMechanism.ins != null
            ? FinishPointMechanism.ins.GetComponent<Collider>()
            : null;
        if (finishTrigger == null)
        {
            Debug.LogWarning("[HorseServerResult] Finish rejected: finish trigger is missing.");
            return false;
        }

        Vector3 horsePosition = participant.transform.position;
        float distance = Vector3.Distance(horsePosition, finishTrigger.ClosestPoint(horsePosition));
        if (distance > ServerFinishTolerance)
        {
            Debug.LogWarning($"[HorseServerResult] Finish rejected: netId={participant.netId}, distance={distance:F1}m.");
            return false;
        }

        int requiredWaypoints = DemoGameManagers.Instance != null
            ? DemoGameManagers.Instance.multiplayerMaxRounds
            : 0;
        if (requiredWaypoints > 0 && progress.GrandTalWaypointPassed + 1 < requiredWaypoints)
        {
            Debug.LogWarning($"[HorseServerResult] Finish rejected: player passed " +
                             $"{progress.GrandTalWaypointPassed}/{requiredWaypoints} server waypoints.");
            return false;
        }

        return TryResolveParticipant(participant, out _);
    }

    [Server]
    public bool ServerRecordFinish(NetworkIdentity participant)
    {
        if (serverResultDeclared || !serverObservedTwoPlayers || !ValidateServerFinish(participant))
            return false;

        return ServerDeclareResult(participant, true);
    }

    [Server]
    private bool ServerDeclareResult(NetworkIdentity participant, bool participantWon)
    {
        if (serverResultDeclared)
            return false;
        if (ApiAndRoomManager._instance == null)
        {
            Debug.LogError("[HorseServerResult] ApiAndRoomManager is missing on the server.");
            return false;
        }
        if (!TryResolveWinner(participant, participantWon, out string winnerId))
            return false;

        NetworkGameManager networkManager = NetworkGameManager.Instance;
        if (string.IsNullOrEmpty(networkManager.transactionId))
        {
            Debug.LogError("[HorseServerResult] Transaction ID is empty; result was not submitted.");
            return false;
        }

        serverResultDeclared = true;
        MConstants.isRaceOver = true;
        MirrorNetwork.winnerID = winnerId;
        Debug.Log($"[HorseServerResult] Server decided winner={winnerId}, connection={participant.connectionToClient.connectionId}.");
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
        {
            Debug.LogError("[HorseServerResult] Transaction ID is empty; draw was not submitted.");
            return;
        }

        serverResultDeclared = true;
        MConstants.isRaceOver = true;
        ApiAndRoomManager._instance.winLoseChallengeId = transactionId;
        ApiAndRoomManager._instance.DrawChallenge(transactionId);
        RpcShowServerDraw();
        ScheduleServerCleanup();
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
            Debug.LogWarning("[HorseServerResult] Forfeit rejected: sender connection is missing.");
            return;
        }

        HorseAnimationSync[] horses = FindObjectsOfType<HorseAnimationSync>();
        for (int i = 0; i < horses.Length; i++)
        {
            NetworkIdentity identity = horses[i] != null ? horses[i].netIdentity : null;
            if (identity == null || identity.connectionToClient != sender)
                continue;

            Debug.Log($"[HorseServerResult] Player forfeited, connection={sender.connectionId}.");
            ServerDeclareResult(identity, false);
            return;
        }

        Debug.LogWarning("[HorseServerResult] Forfeit rejected: owned Horse player was not found.");
    }

    private void RequestExistingDisconnectResult()
    {
        if (NetworkClient.isConnected)
            CmdRequestExistingDisconnectResult();
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestExistingDisconnectResult(NetworkConnectionToClient sender = null)
    {
        NetworkGameManager networkManager = NetworkGameManager.Instance;
        if (sender == null || networkManager == null || !serverObservedTwoPlayers ||
            networkManager.currentPlayerCount != 1)
        {
            Debug.LogWarning($"[HorseServerResult] Existing disconnect result rejected: count=" +
                             $"{(networkManager != null ? networkManager.currentPlayerCount : -1)}.");
            return;
        }

        HorseAnimationSync[] horses = FindObjectsOfType<HorseAnimationSync>();
        for (int i = 0; i < horses.Length; i++)
        {
            NetworkIdentity identity = horses[i] != null ? horses[i].netIdentity : null;
            NetworkConnectionToClient connection = identity != null ? identity.connectionToClient : null;
            if (connection == null || connection != sender ||
                !NetworkServer.connections.TryGetValue(connection.connectionId, out NetworkConnectionToClient activeConnection) ||
                activeConnection != connection)
                continue;

            Debug.Log($"[HorseServerResult] Existing disconnect flow verified; remaining connection={connection.connectionId} wins.");
            ServerDeclareResult(identity, true);
            return;
        }

        Debug.LogWarning("[HorseServerResult] No connected Horse player was found for the result.");
    }

    [ClientRpc]
    private void RpcShowServerWinner(string winnerId)
    {
        ResultManager.isGameFinished = true;
        MConstants.isRaceOver = true;
        if (PopupMessageManager.instance != null && PopupMessageManager.instance.waitingPanel != null)
            PopupMessageManager.instance.waitingPanel.StopTimer();

        ShowServerWinner(winnerId);
    }

    [ClientRpc]
    private void RpcShowServerDraw()
    {
        MConstants.isRaceOver = true;
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
        PlayersHorseData.Clear();
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
        PlayersHorseData.Clear();

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
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("✅ OnStartServer called - Server started");

        if (NetworkServer.active)
        {
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
        Instance = this;

        //	Ye static hai, scene load par apne aap reset nahi hoti. Agar pichhla match isi process
        //	me chala tha to ye true reh jati hai aur agle match ka countdown kabhi start nahi hota.
        ReadyToGO.hasGameStarted = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Only the local player should send spawn request
        // if(isOwned)
        {
            Debug.Log("🎮 Local player requesting spawn...");
            CmdSpawnHorse();
           
        }



    }

    // ----- Server Side -----
    [Command(requiresAuthority = false)]
    public void CmdSpawnHorse(NetworkConnectionToClient sender = null)
    {
        if (sender == null)
        {
            Debug.LogWarning("[HorseServerResult] Horse spawn rejected: sender connection is missing.");
            return;
        }

        Debug.Log($"🐎 [Server] Spawning horse for player {sender.connectionId}");

        // Get spawn position
        Vector3 spawnPos = GetSpawnPosition();
        Quaternion spawnRot = GetSpawnRotation();

        // Instantiate the correct horse prefab
        GameObject horsePrefab = availableMounts[selectedMountIndex].gameObject;
        GameObject spawnedHorse = Instantiate(horsePrefab, spawnPos, spawnRot);

        // Spawn on network and give ownership to the player
        NetworkServer.Spawn(spawnedHorse, sender);

        // Tell client to setup local controls + camera
        TargetSetupHorse(sender, spawnedHorse);
        // RpcAddPlayerToList(spawnedHorse);
        RpcAddPlayerToList(spawnedHorse.GetComponent<NetworkIdentity>().netId);
        if (NetworkServer.connections.Count >= 2)
            serverObservedTwoPlayers = true;
        CheckAllPlayersReadyAndStartCountdown(sender);

    }
    [Server]
    void CheckAllPlayersReadyAndStartCountdown(NetworkConnectionToClient sender)
    {
        int spawnedPlayers = FindObjectsOfType<HorseAnimationSync>().Length;

        Debug.Log($"Spawned players: {spawnedPlayers}");

        if (spawnedPlayers >= 2 && ReadyToGO.Instance != null && !ReadyToGO.hasGameStarted)
        {
            Debug.Log("🎬 All players ready - starting countdown");
            ReadyToGO.hasGameStarted = true;
            StartCoroutine(ServerCountdownSequence());
            return;
        }

        //	Countdown abhi chal raha hai to is player ko alag se kuch bhejne ki zaroorat nahi -
        //	sequence ke aakhir me RpcStartGame() sab ko jata hai, to buttons sab ke sath hi aayenge.
        if (!countdownFinished)
            return;

        //	Yahan pahunche hain to countdown khatam ho chuka tha, yani ye reconnect / late join hai.
        //	Sirf isi player ko game state bhejo (ClientRpc sab ka race timer dobara start kar deta
        //	tha) aur uske buttons foren dikha do.
        Debug.Log("🔁 [Server] Countdown already over - restoring game state for the rejoining player.");
        TargetStartGameForRejoiner(sender);
    }

    [Server]
    IEnumerator ServerCountdownSequence()
    {
        // Prepare all clients
        RpcPrepareCountdown();

        // Countdown sequence: 3, 2, 1, GO
        for (int i = 0; i < 4; i++)
        {
            RpcUpdateCountdownUI(i);
            yield return new WaitForSeconds(2f);
        }

        countdownFinished = true;

        // Start the game - this is the initial start
        RpcStartGame();
    }

    [ClientRpc]
    void RpcPrepareCountdown()
    {
        // HorseAnimationSync.ControlsEnabled = false;

        //	Countdown ke dauran buttons har client par chhupe rehne chahiye. TargetSetupHorse sirf
        //	usi player ke buttons chhupata hai jo abhi spawn hua, to jo pehle aa chuka tha uske
        //	buttons yahan se band hote hain.
        SetPlayerButtonsVisible(false);

        //if (ReadyToGO.Instance != null)
        //{
        //    ReadyToGO.Instance.overlayImage.SetActive(true);
        //    ReadyToGO.Instance.levelOverviewMap.SetActive(false);
        //    ReadyToGO.Instance.playerScoreDisplay.SetActive(false);
        //    ReadyToGO.Instance.mobileControlButton.DisableControls();
        //    ReadyToGO.Instance.mobileControlButton.enabled = false;
        //}

    }

    [ClientRpc]
    void RpcUpdateCountdownUI(int countIndex)
    {
        if (ReadyToGO.Instance != null)
        {

            // Hide all text elements
            for (int i = 0; i < ReadyToGO.Instance.uiTextElements.Length; i++)
            {
                ReadyToGO.Instance.uiTextElements[i].SetActive(false);
            }

            // Show current count
            if (countIndex < ReadyToGO.Instance.uiTextElements.Length)
            {
                ReadyToGO.Instance.uiTextElements[countIndex].SetActive(true);
              
                // Camera activation (if needed)
                if (!MultiPlayerGame.isChampion && countIndex < 3)
                {
                    LevelsManager.instance.raceTracks[MConstants.CurrentLevelNumber - 1]
                        .GetComponent<Level>().ActivateCamera(countIndex + 1);
                }
            }
        }
    }

    /// <summary>
    /// Countdown "GO" par khatam hua. Sab clients ko game state milti hai aur buttons isi lamhe
    /// aate hain - is se pehle kabhi nahi.
    /// </summary>
    [ClientRpc]
    void RpcStartGame()
    {
        Debug.Log("🚀 [ClientRpc] Countdown finished - starting the game on all clients");

        ApplyGameStartedUI();
        SetPlayerButtonsVisible(true);
    }

    /// <summary>
    /// Reconnect / late join. Sirf usi player ko jata hai jo abhi wapas aaya hai, kyunki ClientRpc
    /// se sab ka race timer aur elimination timer dobara start ho jata tha. Iske buttons foren
    /// dikhte hain - countdown ka intezaar nahi, wo kab ka khatam ho chuka.
    /// </summary>
    [TargetRpc]
    void TargetStartGameForRejoiner(NetworkConnection target)
    {
        Debug.Log("🔁 [TargetRpc] Game already running - showing controls right away.");

        ApplyGameStartedUI();
        SetPlayerButtonsVisible(true);
    }

    /// <summary>
    /// Countdown UI hata kar gameplay UI par switch karta hai.
    /// </summary>
    private void ApplyGameStartedUI()
    {

        if (ReadyToGO.Instance == null)
            return;

        // Hide countdown UI
        for (int i = 0; i < ReadyToGO.Instance.uiTextElements.Length; i++)
        {
            ReadyToGO.Instance.uiTextElements[i].SetActive(false);
        }
        ReadyToGO.Instance.overlayImage.SetActive(false);

        // Show game UI
        ReadyToGO.Instance.levelOverviewMap.SetActive(true);
        ReadyToGO.Instance.playerScoreDisplay.SetActive(true);

        // Enable controls
        // ReadyToGO.Instance.mobileControlButton.enabled = true;
        //  ReadyToGO.Instance.mobileControlButton.EnableControls();

        // Deactivate countdown cameras
        if (!MultiPlayerGame.isChampion)
        {
            LevelsManager.instance.raceTracks[MConstants.CurrentLevelNumber - 1]
                .GetComponent<Level>().DeactivateAllCameras();
            LevelsManager.instance.StartRaceTimer();

        }

        // Initialize AI
        // if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER &&
        //     MultiPlayerEnemyCreator.Instance)
        // {
        //     MultiPlayerEnemyCreator.Instance.InitializeAI();
        // }
        // HorseAnimationSync.ControlsEnabled = true;
        DemoGameManagers.Instance.BeginEliminationTimer();

    }

    private void SetPlayerButtonsVisible(bool visible)
    {

        if (PlayerMobileButton != null)
            PlayerMobileButton.SetActive(visible);

    }

    // ----- Client Side -----
    [TargetRpc]
    void TargetSetupHorse(NetworkConnection target, GameObject spawnedHorse)
    {
        Debug.Log("🎯 [Client] Setting up local player horse.");
        if (PlayerMobileButton != null)
            PlayerMobileButton.SetActive(false);
        Animal horseAnimal = spawnedHorse.GetComponent<Animal>();
        PlayerPositionController playerController = spawnedHorse.GetComponent<PlayerPositionController>();

        // Assign camera
        if (playerController != null && PlayerCameraManager.Instance != null)
        {
            DemoGameManagers.Instance.playerController = playerController;
            PlayerCameraManager.Instance.AssignCameraTarget(playerController);

        }


        // Assign mobile controls

    }
    [ClientRpc]
    void RpcAddPlayerToList(uint netId)
    {
        StartCoroutine(AddPlayerWhenReady(netId));
    }

    IEnumerator AddPlayerWhenReady(uint netId)
    {
        // Wait until object is available on client
        float timeout = 3f;
        float timer = 3f;

        while (!NetworkClient.spawned.ContainsKey(netId))
        {
            timer += Time.deltaTime;
            if (timer > timeout)
            {
                Debug.LogWarning("⏰ Timeout waiting for player spawn");
                yield break;
            }
            yield return null;
        }

        GameObject obj = NetworkClient.spawned[netId].gameObject;
        PlayerPositionController playerController = obj.GetComponent<PlayerPositionController>();

        if (playerController != null)
        {
            if (!DemoGameManagers.Instance.aiPlayersList.Contains(playerController))
            {
                DemoGameManagers.Instance.addAiPlayer(playerController);
                Debug.Log("✅ Player added AFTER spawn sync");
            }
        }
    }
    //[ClientRpc]
    //void RpcAddPlayerToList(GameObject spawnedHorse)
    //{
    //    Debug.Log("🎯 [ClientRpc] Adding player to aiPlayersList on all clients");

    //    if (spawnedHorse != null && DemoGameManagers.Instance != null)
    //    {
    //        PlayerPositionController playerController = spawnedHorse.GetComponent<PlayerPositionController>();

    //        if (playerController != null)
    //        {
    //            // ✅ Check for duplicates before adding
    //            if (!DemoGameManagers.Instance.aiPlayersList.Contains(playerController))
    //            {
    //                DemoGameManagers.Instance.addAiPlayer(playerController);
    //                Debug.Log($"✅ Player added. Total: {DemoGameManagers.Instance.aiPlayersList.Count}");
    //            }
    //            else
    //            {
    //                Debug.Log($"⚠️ Player already in list, skipping");
    //            }
    //        }
    //    }
    //}
    // ----- Spawn Position Helpers -----
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
    private void OnDisable()
    {
        MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
        MirrorNetwork.OnWinCall -= AnnounceWinner;
    }

    private void OnEnable()
    {
        MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
        MirrorNetwork.OnWinCall += AnnounceWinner;
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

        Debug.Log("[HorseServerResult] Existing disconnect flow completed; requesting server verification.");
        RequestExistingDisconnectResult();
    }
    // ----- Utility -----
    public void RestartGameScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
    public static Dictionary<string, HorseRecord> PlayersHorseData = new Dictionary<string, HorseRecord>();
    public struct HorseRecord
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public Quaternion Rotation;

        public int CurrentWaypoint;
        public int Lap;
        public int GrandTotalWaypointPassed;
    }
}
