using Mirror;
using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityExtensions;

namespace Snake_Ladder
{
    public class SnakeMirrorNetworkManager : NetworkBehaviour
    {
        public static SnakeMirrorNetworkManager Instance;

        [Header("Network Player Prefabs")]
        public GameObject player1NetworkPrefab;
        public GameObject player2NetworkPrefab;

        [SyncVar]
        private int currentEnvironmentIndex = -1;
        [SyncVar]
        public int currentTurnPlayerNumber = -1;
        [SyncVar]
        private bool gameInProgress = false;
        [SyncVar]
        private float currentTimer = 0f;

        [SyncVar]
        private bool isTimerRunning = false;

        private float timerDuration = 10f;
        // Events
        public event System.Action Connected;
        public event System.Action OnRoomJoined;
        public event System.Action<string, bool> PlayerLeft;
        public event System.Action Disconnected;
        public event System.Action<int> FirstTurnPlayer;
        public event System.Action<int> UpdateEnvironment;
        public event System.Action StartGame;

        public bool gameOver = false;
        public bool inGame { get; private set; }

        // Track connected clients using connectionId
        private bool hasGameStarted = false;
        [SyncVar]
        private bool serverResultDeclared;
        [SyncVar]
        private string serverWinnerId = string.Empty;
        [SyncVar]
        private bool serverResultWasDraw;
        private bool serverMoveInProgress;
        private Coroutine reconnectResultDisplayCoroutine;

        // Track spawned network players
        private GameObject networkPlayer1;
        private GameObject networkPlayer2;
        //public void CmdTimer()
        //{
        //    RpcNotifyTimer();
        //}
        //[ClientRpc]
        //public void RpcNotifyTimer()
        //{
        //    if (GameControllerNew.myPlayerNumber == 0)
        //    {
        //        GameControllerNew.instance.timer = GameControllerNew.instance.TimerDuration;
        //        GameControllerNew.instance.timerRunning = true;
        //        GameControllerNew.instance.Player1TimerImage.fillAmount = 0;
        //        GameControllerNew.instance.Player1TimerImage.gameObject.SetActive(true);
        //    }
        //    else
        //    {
        //        GameControllerNew.instance.timer = GameControllerNew.instance.TimerDuration;
        //        GameControllerNew.instance.timerRunning = true;
        //        GameControllerNew.instance.Player2TimerImage.fillAmount = 0;
        //        GameControllerNew.instance.Player2TimerImage.gameObject.SetActive(true);
        //    }
        //}
        #region Game Timer Syncing
        [Header("UI Timer")]
        // public TextMeshProUGUI countDownGameTimer;

        [Header("Countdown Settings")]
        [SyncVar(hook = nameof(OnTimeChanged))]
        public int remainingTime = 600;

        public int remainingminutes;
        public int remainingseconds;

        private Coroutine countdownCoroutine;

        [Server]
        private IEnumerator ServerCountdown()
        {
            remainingTime = 600;
            Debug.Log("Server Countdown Started." + remainingTime);

            //while (remainingTime > 0)
            //{
            //    if (!NetworkGameManager.Instance.IsPaused)
            //        remainingTime--;

            //    yield return new WaitForSeconds(1f);
            //}
            while (remainingTime > 0)
            {
                // Pause the 5-min game clock whenever a player has dropped — wait until both
                // players are present again before ticking. Without this, the timer keeps
                // draining while one side is in the reconnect window and can end the game
                // even though the disconnected player still has time to come back.
                while (NetworkGameManager.Instance != null && NetworkGameManager.Instance.currentPlayerCount < 2)
                {
                    yield return new WaitForSeconds(0.5f);
                }

                remainingTime--; // SyncVar updates all clients automatically
                yield return new WaitForSeconds(1f);
            }
            // The overall game clock never selects a winner by token position/score.
            // Reaching EndNode is still the only normal win condition.
            Debug.Log("[SnakeServerResult] Timer ended: server declared a draw.");
            MatchFlow.Log("Snake & Ladder", "match clock ran out — draw");
            RpcTimerDisplayEnded();
            ServerDeclareDraw();
        }

        [Server]
        private bool TryResolveConnection(NetworkConnectionToClient connection,
            out string playerId, out int playerIndex)
        {
            playerId = string.Empty;
            playerIndex = -1;
            NetworkGameManager networkManager = NetworkGameManager.Instance;
            if (networkManager == null || connection == null ||
                networkManager.creatorData == null || networkManager.joinerData == null)
            {
                Debug.LogWarning("[SnakeServerResult] Player mapping failed: manager, connection, or player data is missing.");
                return false;
            }

            if (networkManager.CreatorRef != null &&
                connection.connectionId == networkManager.CreatorRef.connectionId)
            {
                playerId = networkManager.creatorData.playerId;
                playerIndex = 0;
            }
            else if (networkManager.JoinerRef != null &&
                     connection.connectionId == networkManager.JoinerRef.connectionId)
            {
                playerId = networkManager.joinerData.playerId;
                playerIndex = 1;
            }
            else
            {
                Debug.LogWarning($"[SnakeServerResult] Unregistered connection rejected: {connection.connectionId}.");
                return false;
            }

            return !string.IsNullOrEmpty(playerId);
        }

        [Server]
        private bool TryResolvePlayerIndex(string playerId, out int playerIndex)
        {
            playerIndex = -1;
            NetworkGameManager networkManager = NetworkGameManager.Instance;
            if (networkManager == null || networkManager.creatorData == null || networkManager.joinerData == null)
                return false;

            if (playerId == networkManager.creatorData.playerId)
                playerIndex = 0;
            else if (playerId == networkManager.joinerData.playerId)
                playerIndex = 1;

            return playerIndex >= 0;
        }

        [Server]
        public bool ServerDeclareBoardWinner(string playerId)
        {
            if (serverResultDeclared || !hasGameStarted || !gameInProgress ||
                !TryResolvePlayerIndex(playerId, out int playerIndex) ||
                GameControllerNew.instance == null)
                return false;

            Node playerNode = playerIndex == 0
                ? GameControllerNew.instance.CurrentNodePlayer1
                : GameControllerNew.instance.CurrentNodePlayer2;
            if (playerNode == null || playerNode != GameControllerNew.instance.EndNode)
            {
                Debug.LogWarning($"[SnakeServerResult] Finish rejected for player={playerId}: server board is not at EndNode.");
                MatchFlow.Log("Snake & Ladder", $"finish claim from {MatchFlow.Who(playerId)} rejected — not on 100 (at {(playerNode != null ? playerNode.name : "?")})");
                return false;
            }

            return ServerDeclareWinnerByIndex(playerIndex);
        }

        [Server]
        private bool ServerDeclareWinnerByIndex(int winnerIndex)
        {
            NetworkGameManager networkManager = NetworkGameManager.Instance;
            if (networkManager == null || networkManager.creatorData == null || networkManager.joinerData == null)
                return false;

            string winnerId = winnerIndex == 0
                ? networkManager.creatorData.playerId
                : winnerIndex == 1 ? networkManager.joinerData.playerId : string.Empty;
            return ServerSubmitWinner(winnerId);
        }

        [Server]
        private bool ServerSubmitWinner(string winnerId)
        {
            if (serverResultDeclared)
                return false;
            if (ApiAndRoomManager._instance == null || NetworkGameManager.Instance == null)
            {
                Debug.LogError("[SnakeServerResult] Required server manager is missing.");
                return false;
            }
            if (string.IsNullOrEmpty(winnerId) || string.IsNullOrEmpty(NetworkGameManager.Instance.transactionId))
            {
                Debug.LogError("[SnakeServerResult] Winner ID or transaction ID is empty; result was not submitted.");
                return false;
            }

            serverWinnerId = winnerId;
            serverResultWasDraw = false;
            serverResultDeclared = true;
            serverMoveInProgress = false;
            gameOver = true;
            gameInProgress = false;
            ServerStopTimerCompletely();
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            Debug.Log($"[SnakeServerResult] Server decided winner={winnerId}.");
            MatchFlow.SendResult(winnerId, FlowWinReason(winnerId));
            ApiAndRoomManager._instance.winLoseChallengeId = NetworkGameManager.Instance.transactionId;
            ApiAndRoomManager._instance.WinnerLossChallenge(winnerId);
            RpcShowServerWinner(winnerId);
            ScheduleServerCleanup();
            return true;
        }

        [Server]
        private bool ServerSubmitDisconnectWinner(int winnerIndex)
        {
            NetworkGameManager networkManager = NetworkGameManager.Instance;
            if (serverResultDeclared || networkManager == null || ApiAndRoomManager._instance == null ||
                networkManager.creatorData == null || networkManager.joinerData == null)
                return false;

            string winnerId = winnerIndex == 0
                ? networkManager.creatorData.playerId
                : winnerIndex == 1 ? networkManager.joinerData.playerId : string.Empty;
            if (string.IsNullOrEmpty(winnerId) || string.IsNullOrEmpty(networkManager.transactionId))
            {
                Debug.LogError("[SnakeServerResult] Disconnect winner or transaction ID is missing.");
                return false;
            }

            serverWinnerId = winnerId;
            serverResultWasDraw = false;
            serverResultDeclared = true;
            serverMoveInProgress = false;
            gameOver = true;
            gameInProgress = false;
            ServerStopTimerCompletely();
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            Debug.Log($"[SnakeServerResult] Server decided disconnect winner={winnerId}; enabling late-client delivery.");
            MatchFlow.SendResult(winnerId, "opponent disconnected");
            ApiAndRoomManager._instance.winLoseChallengeId = networkManager.transactionId;

            // Preserve Snake's immediate result UI for the connected player. Then use the
            // existing cross-game disconnect pipeline, which retains and re-sends the same
            // authoritative winner to a client that returns after missing this ClientRpc.
            RpcShowServerWinner(winnerId);
            networkManager.SetDisconnectWinner(winnerId);
            ScheduleServerCleanup(65f);
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
                Debug.LogError("[SnakeServerResult] Transaction ID is empty; draw was not submitted.");
                return;
            }

            serverWinnerId = string.Empty;
            serverResultWasDraw = true;
            serverResultDeclared = true;
            serverMoveInProgress = false;
            gameOver = true;
            gameInProgress = false;
            ServerStopTimerCompletely();
            MatchFlow.SendResult("draw", "match time over");
            ApiAndRoomManager._instance.winLoseChallengeId = transactionId;
            ApiAndRoomManager._instance.DrawChallenge(transactionId);
            RpcShowServerDraw();
            ScheduleServerCleanup();
        }

        // ---- MatchFlow (server game-event log) helpers: logging only ----

        /// <summary>Name of player 1 (creator, index 0) or player 2 (joiner, index 1) for the match log.</summary>
        public static string FlowWho(int playerIndex)
        {
            NetworkGameManager ngm = NetworkGameManager.Instance;
            if (ngm != null)
            {
                if (playerIndex == 0 && ngm.creatorData != null) return MatchFlow.Who(ngm.creatorData.playerId);
                if (playerIndex == 1 && ngm.joinerData != null) return MatchFlow.Who(ngm.joinerData.playerId);
            }
            return "Player " + (playerIndex + 1);
        }

        /// <summary>Why <paramref name="winnerId"/> won: on square 100 → reached 100, otherwise the opponent forfeited.</summary>
        private string FlowWinReason(string winnerId)
        {
            GameControllerNew gc = GameControllerNew.instance;
            if (gc == null || !TryResolvePlayerIndex(winnerId, out int idx))
                return "game over";
            Node node = idx == 0 ? gc.CurrentNodePlayer1 : gc.CurrentNodePlayer2;
            return node != null && node == gc.EndNode ? "reached 100" : "opponent forfeited";
        }

        public bool RequestLocalForfeit()
        {
            if (!NetworkClient.isConnected)
                return false;

            CmdForfeitGame();
            return true;
        }

        [Command(requiresAuthority = false)]
        private void CmdForfeitGame(NetworkConnectionToClient sender = null)
        {
            if (!TryResolveConnection(sender, out _, out int forfeitingIndex))
                return;

            Debug.Log($"[SnakeServerResult] Player {forfeitingIndex + 1} forfeited.");
            if (!serverResultDeclared) MatchFlow.Log("Snake & Ladder", $"{FlowWho(forfeitingIndex)} left the game (forfeit)");
            ServerDeclareWinnerByIndex(forfeitingIndex == 0 ? 1 : 0);
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
            if (sender == null || networkManager == null || !hasGameStarted ||
                networkManager.currentPlayerCount != 1 ||
                !TryResolveConnection(sender, out _, out int remainingPlayerIndex))
            {
                Debug.LogWarning($"[SnakeServerResult] Existing disconnect result rejected: count=" +
                                 $"{(networkManager != null ? networkManager.currentPlayerCount : -1)}.");
                return;
            }

            Debug.Log($"[SnakeServerResult] Existing disconnect flow verified; Player {remainingPlayerIndex + 1} wins.");
            if (!serverResultDeclared) MatchFlow.Log("Snake & Ladder", $"{FlowWho(remainingPlayerIndex == 0 ? 1 : 0)} disconnected and did not return — {FlowWho(remainingPlayerIndex)} remains");
            ServerSubmitDisconnectWinner(remainingPlayerIndex);
        }

        [TargetRpc]
        private void TargetRestoreFinishedResult(NetworkConnectionToClient target, string winnerId, bool wasDraw)
        {
            QueueFinishedResultDisplay(winnerId, wasDraw);
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

        private void TryShowSyncedFinishedResult()
        {
            if (serverResultDeclared)
                QueueFinishedResultDisplay(serverWinnerId, serverResultWasDraw);
        }

        private void QueueFinishedResultDisplay(string winnerId, bool wasDraw)
        {
            if (reconnectResultDisplayCoroutine == null)
                reconnectResultDisplayCoroutine = StartCoroutine(
                    ShowFinishedResultWhenClientReady(winnerId, wasDraw));
        }

        private IEnumerator ShowFinishedResultWhenClientReady(string winnerId, bool wasDraw)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline &&
                   (GameControllerNew.instance == null || NetworkGameManager.Instance == null ||
                    staticVariables.UserProfiledata == null || staticVariables.UserProfiledata.user == null))
            {
                yield return null;
            }

            reconnectResultDisplayCoroutine = null;
            if (staticVariables.UserProfiledata == null || staticVariables.UserProfiledata.user == null)
            {
                Debug.LogWarning("[SnakeServerResult] Reconnect result UI could not load because local profile data is missing.");
                yield break;
            }

            ResultManager.isGameFinished = true;
            if (PopupMessageManager.instance != null)
            {
                PopupMessageManager.instance.waitingPanel?.StopTimer();
                PopupMessageManager.instance.ReconnectionPanelStatus(false);
            }

            if (wasDraw)
                ShowServerDraw();
            else if (!string.IsNullOrEmpty(winnerId))
                ShowServerWinner(winnerId);
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
            MirrorNetwork.winnerID = winnerId;
            SetSnakeResultSide();

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
            SetSnakeResultSide();
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

        private static void SetSnakeResultSide()
        {
            if (GameControllerNew.myPlayerNumber == 0)
                ResultManager.LocalPlayerSideOverride = ResultManager.ResultSide.Left;
            else if (GameControllerNew.myPlayerNumber == 1)
                ResultManager.LocalPlayerSideOverride = ResultManager.ResultSide.Right;
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
        private void ScheduleServerCleanup(float delaySeconds = 5f)
        {
            this.Delay(delaySeconds, () =>
            {
                if (MirrorNetwork.Instance != null && MirrorNetwork.Instance.edgegapAPIClient != null &&
                    NetworkGameManager.Instance != null)
                {
                    MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(
                        NetworkGameManager.Instance.currentServerRequestId);
                }
            });
        }

        // SyncVar hook
        private void OnTimeChanged(int oldTime, int newTime)
        {
            remainingminutes = Mathf.FloorToInt(newTime / 60);
            remainingseconds = Mathf.FloorToInt(newTime % 60);

            GameControllerNew.instance.countDownGameTimer.text = string.Format("{0:00}:{1:00}", remainingminutes, remainingseconds);

            if (newTime > 180)
                GameControllerNew.instance.countDownGameTimer.color = Color.green;
            else if (newTime > 60)
                GameControllerNew.instance.countDownGameTimer.color = Color.white;
            else
                GameControllerNew.instance.countDownGameTimer.color = Color.red;
        }

        [ClientRpc]
        private void RpcTimerDisplayEnded()
        {
            GameControllerNew.instance.countDownGameTimer.text = "00:00";
            GameControllerNew.instance.countDownGameTimer.color = Color.red;
        }

        #endregion
        #region Timer
        private bool IsTurnTimerPaused()
        {
            return NetworkGameManager.Instance != null
                && (NetworkGameManager.Instance.IsPaused
                    || NetworkGameManager.Instance.currentPlayerCount < 2);
        }

        private void Update()
        {
            if (isServer && isTimerRunning && gameInProgress)
            {
                // Freeze the per-turn clock whenever a player has dropped — same reason the
                // 10-min game clock pauses in ServerCountdown. currentPlayerCount is the
                // server-side head count (MirrorNetwork keeps it in sync); with only one
                // player in the room the turn must not time out and flip while the other
                // side is still inside its reconnect window.
                if (IsTurnTimerPaused())
                    return;

                currentTimer -= Time.deltaTime;

                if (currentTimer <= 0)
                {
                    Debug.Log($"⏰ [Server] Player {currentTurnPlayerNumber + 1}'s time is up!");
                    MatchFlow.Log("Snake & Ladder", $"turn timer ran out for {FlowWho(currentTurnPlayerNumber)} — turn skipped, no roll");

                    // ✅ Switch turn
                    currentTurnPlayerNumber = currentTurnPlayerNumber == 0 ? 1 : 0;

                    Debug.Log($"🔄 [Server] Switching to Player {currentTurnPlayerNumber + 1}");
                    MatchFlow.Log("Snake & Ladder", $"turn → {FlowWho(currentTurnPlayerNumber)}");

                    // ✅ Reset timer for next player
                    currentTimer = timerDuration;

                    // ✅ Notify all clients
                    RpcNotifyFirstTurn(currentTurnPlayerNumber);
                }
            }
        }

        // Start timer once at game start
        [Server]
        public void ServerStartTimer()
        {
            if (!isTimerRunning)
            {
                currentTimer = timerDuration;
                isTimerRunning = true;
                Debug.Log("🕐 [Server] Timer system started");
            }
        }

        // ✅ Reset timer when player manually takes turn
        [Server]
        public void ServerResetTimer()
        {
            currentTimer = timerDuration;
            Debug.Log("🕐 [Server] Timer reset for new turn");
        }

        [Server]
        public void ServerCompleteMove()
        {
            serverMoveInProgress = false;
        }

        // ✅ Only stop timer when game ends
        [Server]
        public void ServerStopTimerCompletely()
        {
            isTimerRunning = false;
            currentTimer = 0;
            Debug.Log("🕐 [Server] Timer stopped completely");
        }



        // Sync timer to clients
        private void LateUpdate()
        {
            if (isServer && isTimerRunning)
            {
                // Keep the client-side UI state aligned with the authoritative pause.
                RpcSyncTimer(currentTimer, isTimerRunning && !IsTurnTimerPaused());
            }
        }

        [ClientRpc]
        public void RpcSyncTimer(float timeRemaining, bool running)
        {
            if (GameControllerNew.instance != null)
            {
                GameControllerNew.instance.timer = timeRemaining;
                GameControllerNew.instance.timerRunning = running;
            }
        }

        #endregion
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("✅ SnakeMirrorNetworkManager Instance created");
            }
            else
            {
                Debug.LogWarning("⚠️ Duplicate SnakeMirrorNetworkManager destroyed");
                Destroy(gameObject);
            }
        }

        // ========== CLIENT SIDE - AUTO REQUEST ==========
        public override void OnStartClient()
        {
            base.OnStartClient();

            // A reconnecting client may have missed the original result RPC while it
            // was offline. Initial SyncVar state is already applied before this hook.
            Invoke(nameof(TryShowSyncedFinishedResult), 0.5f);

            if (!isServer)
            {
                Debug.Log("🎮 [Client] Connected! Requesting player assignment...");
                Invoke(nameof(RequestAssignment), 0.2f);
            }
        }
        private void OnDisable()
        {
            CancelInvoke(nameof(TryShowSyncedFinishedResult));
            if (reconnectResultDisplayCoroutine != null)
            {
                StopCoroutine(reconnectResultDisplayCoroutine);
                reconnectResultDisplayCoroutine = null;
            }
            MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
            MirrorNetwork.OnWinCall -= AnnounceWinner;
            ResultManager.LocalPlayerSideOverride = ResultManager.ResultSide.Auto;
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

            Debug.Log("[SnakeServerResult] Existing disconnect flow completed; requesting server verification.");
            RequestExistingDisconnectResult();
        }
        void RequestAssignment()
        {
            CmdRequestPlayerAssignment(staticVariables.UserProfiledata.user._id.ToString());
        }

        // ========== SERVER SIDE - AUTO ASSIGN ==========
        [Command(requiresAuthority = false)]
        void CmdRequestPlayerAssignment(string playerID, NetworkConnectionToClient sender = null)
        {
            if (!TryResolveConnection(sender, out string authoritativePlayerId, out _))
                return;

            if (serverResultDeclared)
            {
                Debug.Log($"[SnakeServerResult] Restoring finished result to reconnecting connection {sender.connectionId}.");
                TargetRestoreFinishedResult(sender, serverWinnerId, serverResultWasDraw);
                return;
            }

            // Never trust the ID supplied by the client. The server maps the
            // connection to Creator/Joiner data and uses that ID for spawning.
            playerID = authoritativePlayerId;
            int connectionId = sender.connectionId;

            Debug.Log($"🎲 [Server] Player assignment request from connectionId: {connectionId}");

            // Check if already assigned
            if ((NetworkGameManager.Instance.creatorData.playerId == playerID && networkPlayer1) || (NetworkGameManager.Instance.joinerData.playerId == playerID && networkPlayer2))
            {
                MatchFlow.Log("Snake & Ladder", $"{MatchFlow.Who(playerID)} reconnected");
                if (hasGameStarted && currentEnvironmentIndex >= 0)
                {
                    Debug.Log($"🔄 [Server] Game running - syncing environment {currentEnvironmentIndex} to reconnecting player");
                    TargetSyncEnvironment(sender, currentEnvironmentIndex);
                }
                if (gameInProgress && currentTurnPlayerNumber >= 0)
                {
                    Debug.Log($"🔄 [Server] Syncing turn state - Current turn: Player {currentTurnPlayerNumber + 1}");
                    TargetSyncTurnState(
                        sender,
                        currentTurnPlayerNumber,
                        currentTimer,
                        isTimerRunning && !IsTurnTimerPaused());
                }
                return;
            }
            // ✅ Spawn network player object for this client
            SpawnNetworkPlayerForClient(sender, playerID);


            CheckAllPlayersReadyAndStartGame();




        }
        [TargetRpc]
        void TargetSyncTurnState(
            NetworkConnectionToClient target,
            int firstPlayerNumber,
            float timeRemaining,
            bool running)
        {
            Debug.Log($"🎯 [Client] Syncing turn state - Player {firstPlayerNumber + 1}'s turn");

            if (GameControllerNew.instance == null)
            {
                Debug.LogWarning("⚠️ [Client] Cannot sync turn timer before GameControllerNew is ready.");
                return;
            }

            GameControllerNew controller = GameControllerNew.instance;
            float duration = Mathf.Max(controller.TimerDuration, 0.001f);
            float syncedTime = Mathf.Clamp(timeRemaining, 0f, duration);
            float syncedFill = 1f - Mathf.Clamp01(syncedTime / duration);

            controller.timer = syncedTime;
            controller.timerRunning = running;

            // ❌ Sab timers band
            controller.Player1TimerImage.gameObject.SetActive(false);
            controller.Player2TimerImage.gameObject.SetActive(false);

            // Restore the authoritative turn for every reconnecting client, even when it is
            // currently the opponent's turn. Set fill after activation so OnEnable cannot reset it.
            if (firstPlayerNumber == 0)
            {
                controller.Player1TimerImage.gameObject.SetActive(true);
                controller.Player1TimerImage.fillAmount = syncedFill;
            }
            else
            {
                controller.Player2TimerImage.gameObject.SetActive(true);
                controller.Player2TimerImage.fillAmount = syncedFill;
            }
            FirstTurnPlayer?.Invoke(firstPlayerNumber);
        }
        [TargetRpc]
        void TargetSyncEnvironment(NetworkConnectionToClient target, int envIndex)
        {
            Debug.Log($"🎨 [Client] Syncing environment: {envIndex}");

            if (GameControllerNew.instance != null)
            {
                GameControllerNew.instance.SetEnvironment(envIndex);
                UpdateEnvironment?.Invoke(envIndex);
                Debug.Log($"✅ [Client] Environment synced successfully!");
            }
            else
            {
                Debug.LogError("❌ GameControllerNew.instance is null!");
            }
        }
        // ========== SERVER - SPAWN NETWORK PLAYER ==========
        [Server]
        void SpawnNetworkPlayerForClient(NetworkConnectionToClient connection, string playerID)
        {
            Debug.Log($"🎮 [Server] Spawning network player {playerID} for connection {connection.connectionId}");

            GameObject playerPrefab = (NetworkGameManager.Instance.creatorData.playerId == playerID) ? player1NetworkPrefab : player2NetworkPrefab;

            if (playerPrefab != null)
            {
                // Spawn player object
                GameObject playerObject = Instantiate(playerPrefab);

                // Assign to the client
                NetworkServer.Spawn(playerObject);
                playerObject.GetComponent<SnakePlayerRef>().playerID = playerID;
                // Track spawned players
                if (NetworkGameManager.Instance.creatorData.playerId == playerID)
                {
                    networkPlayer1 = playerObject;
                    GameControllerNew.instance.Player1Soldier = playerObject.transform;
                }
                else
                {
                    networkPlayer2 = playerObject;
                    GameControllerNew.instance.Player2Soldier = playerObject.transform;

                }
                Debug.Log($"✅ [Server] Network Player {playerID} spawned and assigned to connection {connection.connectionId}");
            }
            else
            {
                Debug.LogError($"❌ [Server] Player {playerID} prefab is null!");
            }
        }


        // ========== SERVER - CHECK IF READY TO START ==========
        [Server]
        void CheckAllPlayersReadyAndStartGame()
        {
            if (!hasGameStarted)
            {
                Debug.Log("🎬 [Server] Both players connected! Starting game...");
                MatchFlow.Begin("Snake & Ladder", $"game started — {FlowWho(0)} vs {FlowWho(1)}");
                hasGameStarted = true;
                StartCoroutine(ServerGameStartSequence());
                if (countdownCoroutine != null)
                    StopCoroutine(countdownCoroutine);
                countdownCoroutine = StartCoroutine(ServerCountdown());
            }
        }

        // ========== SERVER - GAME START SEQUENCE ==========
        [Server]
        System.Collections.IEnumerator ServerGameStartSequence()
        {
            yield return new WaitForSeconds(0.3f);

            // Set random environment
            int randomEnvIndex = UnityEngine.Random.Range(
                0,
                Snake_Ladder.GameControllerNew.instance.AllEnvironments.Count
            );
            currentEnvironmentIndex = randomEnvIndex;
            Debug.Log($"🌍 [Server] Setting environment index: {randomEnvIndex}");
            RpcSetEnvironment(randomEnvIndex);
            if (GameControllerNew.instance != null)
            {
                // ✅ Store environment index in GameController too
                GameControllerNew.instance.currentEnvironmentIndex = randomEnvIndex;
                GameControllerNew.instance.Environment = GameControllerNew.instance.AllEnvironments[randomEnvIndex];
                GameControllerNew.instance.EnvironmentImage.sprite = GameControllerNew.instance.Environment.environmentSprite;
                Debug.Log($"🌍 [Client] Setting environment: {randomEnvIndex}");
                UpdateEnvironment?.Invoke(randomEnvIndex);
            }
            yield return new WaitForSeconds(0.3f);

            // Decide who goes first
            //  int firstPlayer = UnityEngine.Random.Range(0, 2);
            int firstPlayer = 1;
            Debug.Log($"🎲 [Server] Decided: Player {firstPlayer + 1} goes first");
            MatchFlow.Log("Snake & Ladder", $"board {randomEnvIndex}, {FlowWho(firstPlayer)} starts");
            currentTurnPlayerNumber = firstPlayer;
            gameInProgress = true;

            RpcNotifyFirstTurn(firstPlayer);
            ServerStartTimer();
            yield return new WaitForSeconds(0.2f);

            // Start the game
            Debug.Log("🚀 [Server] Broadcasting game start!");
            RpcStartGame();
            StartGame?.Invoke();

            inGame = true;
        }
        // ========== CLIENT RPC - ALL CLIENTS ==========
        [ClientRpc]
        void RpcSetEnvironment(int envIndex)
        {
            if (GameControllerNew.instance != null)
            {
                // ✅ Store environment index in GameController too
                GameControllerNew.instance.currentEnvironmentIndex = envIndex;
                GameControllerNew.instance.Environment = GameControllerNew.instance.AllEnvironments[envIndex];
                GameControllerNew.instance.EnvironmentImage.sprite = GameControllerNew.instance.Environment.environmentSprite;
                Debug.Log($"🌍 [Client] Setting environment: {envIndex}");
                UpdateEnvironment?.Invoke(envIndex);
            }
        }

        [ClientRpc]
        public void RpcNotifyFirstTurn(int firstPlayerNumber)
        {
            Debug.Log($"🎯 [Client] Player {firstPlayerNumber + 1}'s turn");

            // Reset timers on all clients
            GameControllerNew.instance.Player1TimerImage.gameObject.SetActive(false);
            GameControllerNew.instance.Player2TimerImage.gameObject.SetActive(false);

            // Show timer for the player whose turn it is
            if (firstPlayerNumber == 0)
            {
                GameControllerNew.instance.Player1TimerImage.gameObject.SetActive(true);
                GameControllerNew.instance.Player1TimerImage.fillAmount = 0;
            }
            else
            {
                GameControllerNew.instance.Player2TimerImage.gameObject.SetActive(true);
                GameControllerNew.instance.Player2TimerImage.fillAmount = 0;
            }

            FirstTurnPlayer?.Invoke(firstPlayerNumber);

            // ❌ Remove this - Timer ab continuously chal raha hai
            // if (isServer && gameInProgress)
            // {
            //     ServerStartTimer();
            // }
        }


        [ClientRpc]
        void RpcStartGame()
        {
            Debug.Log("🚀 [Client] Game starting!");
            StartGame?.Invoke();
        }

        [Command(requiresAuthority = false)]
        public void CmdMovePlayern(int diceNumber, string playerID, NetworkConnectionToClient sender = null)
        {
            if (serverResultDeclared || serverMoveInProgress || !gameInProgress ||
                !TryResolveConnection(sender, out string authoritativePlayerId, out int playerIndex))
                return;
            if (playerIndex != currentTurnPlayerNumber)
            {
                Debug.LogWarning($"[SnakeServerResult] Out-of-turn move rejected from Player {playerIndex + 1}.");
                MatchFlow.Log("Snake & Ladder", $"roll from {FlowWho(playerIndex)} rejected — not their turn");
                return;
            }
            if (diceNumber < 1 || diceNumber > 6)
            {
                Debug.LogWarning($"[SnakeServerResult] Invalid dice value rejected: {diceNumber}.");
                return;
            }

            // The caller cannot select which token moves.
            playerID = authoritativePlayerId;
            diceNumber = UnityEngine.Random.Range(1, 7);
            serverMoveInProgress = true;
            Debug.Log($"[SnakeServerResult] Server rolled {diceNumber} for Player {playerIndex + 1}.");

            // Stop server timer while move animation is in progress to prevent double-turn
            if (isTimerRunning)
                ServerStopTimerCompletely();

            RollDiceNetwork(playerID, diceNumber);

            StartCoroutine(GameControllerNew.instance.MovePlayer(playerID, diceNumber));
        }
        [ClientRpc]
        public void RollDiceNetwork(string playernumber, int turnNumber)
        {
            bool isfirstplayer = playernumber == NetworkGameManager.Instance.creatorData.playerId;
            Debug.Log("RollDiceNetwork " + playernumber + "  " + turnNumber + " isfirstplayer " + isfirstplayer);
            if (isfirstplayer)
            {
                GameControllerNew.instance.player1Dice.AnimateDice(turnNumber);
            }
            else
            {
                GameControllerNew.instance.player2Dice.AnimateDice(turnNumber);
            }
        }

        // ========== CLEANUP ==========

        [Command(requiresAuthority = false)]
        public void CmdPlayerFinished(int ignoredPlayerId, NetworkConnectionToClient sender = null)
        {
            if (!TryResolveConnection(sender, out string authoritativePlayerId, out _))
                return;

            Debug.Log("[SnakeServerResult] Client finish request received; validating server board.");
            if (!serverResultDeclared) MatchFlow.Log("Snake & Ladder", $"{MatchFlow.Who(authoritativePlayerId)} claims to have reached 100 — checking board");
            ServerDeclareBoardWinner(authoritativePlayerId);
        }

        [ClientRpc]
        public void RpcActivateUIAndHandleResult(bool didWin, string playerId)
        {
            ShowServerWinner(playerId);
        }
         [Command(requiresAuthority = false)]
        public void CmdTimerEnd(int ignoredPlayerId)
        {
            Debug.LogWarning("[SnakeServerResult] Legacy client timer result ignored; the server timer owns the result.");
        }

        [ClientRpc]
        public void RpcTimerEnd(bool didWin, string playerId)
        {
            ShowServerWinner(playerId);
        }
    }
}
