using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;
using UnityExtensions;

namespace Twelve
{
    /// <summary>Why the server ended the match. Server-side only — never sent by a client.</summary>
    public enum TwelveEndReason
    {
        ScoreLimit, // someone reached GameConstants.SCORE_TO_WIN
        Timeout,    // the 5-minute match clock ran out
        Forfeit,    // a player quit or was declared out
        Checkmate   // the next player has no legal move
    }

    public class MultiPlayerGameManagerTwelve : NetworkBehaviour
    {
        private static MultiPlayerGameManagerTwelve _instance;

        public static MultiPlayerGameManagerTwelve instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogWarning("GameManager instance not found in scene!");
                }
                return _instance;
            }
        }
        public GamePlayControllerTwelve gamePlayController;
        public TurnTimerTwelve turnTimer;
        public int lastPlayerTurnId;
        public GameObject gameBoard;
        public GameObject twelveBeadNetworkPrefab;

        // SyncVars to replace Photon Custom Properties
        [SyncVar(hook = nameof(OnPlayerTurnChanged))]
        public int currentTurnId;

        [SyncVar]
        public bool isGameOver = false;

        [SyncVar]
        public bool allPlayersLoaded = false;

        public Dictionary<int, bool> playerLoadedStatus = new Dictionary<int, bool>();
        public bool countdownStarted = false;

        // Mirror auto-syncs these SyncVars to joining/reconnecting clients before OnStartClient,
        // so no static cache or manual save/restore is needed.
        [SyncVar] public int Myplayer1Score = 0;
        [SyncVar] public int Myplayer2Score = 0;

        [SyncVar] public uint twelveRevision = 0;

        private BeadScriptTwelve forcedChainBead;
        private BeadScriptTwelve lastMovedBeadP1;
        private BeadScriptTwelve lastMovedBeadP2;
        private TwelveState serverRulesState;
        private bool serverRulesReady;
        private readonly Dictionary<NetworkConnectionToClient, Queue<uint>> recentRequestIds = new Dictionary<NetworkConnectionToClient, Queue<uint>>();
        private readonly Dictionary<NetworkConnectionToClient, HashSet<uint>> recentRequestIdSets = new Dictionary<NetworkConnectionToClient, HashSet<uint>>();
        private readonly Dictionary<NetworkConnectionToClient, Queue<double>> submitTimes = new Dictionary<NetworkConnectionToClient, Queue<double>>();
        private readonly Dictionary<string, uint> rejectedMoveCounters = new Dictionary<string, uint>();
        public IReadOnlyDictionary<string, uint> RejectedMoveCounters => rejectedMoveCounters;
        public uint staleRequestCount { get; private set; }
        public uint duplicateRequestCount { get; private set; }
        public uint duplicateFinalizationCount { get; private set; }
        private const int MAX_REQUEST_IDS_PER_CONNECTION = 64;
        private const int MAX_VALID_SUBMITS_PER_SECOND = 8;
        private const float MOVE_PENDING_TIMEOUT_SECONDS = 4f;

        private bool movePending;
        private uint pendingRequestId;
        private Coroutine movePendingTimeoutCoroutine;
        private uint nextRequestId = 1;
        private readonly Queue<TwelveCommittedMove> committedMoves = new Queue<TwelveCommittedMove>();
        private Coroutine committedMoveCoroutine;

        public bool IsMovePending => movePending;

        private struct TwelveCommittedMove
        {
            public byte fromNode;
            public byte toNode;
            public byte capturedNode;
            public PLAYERS seat;
            public bool chainContinues;
            public uint revision;
        }
        #region Game Timer Syncing
        [Header("UI Timer")]
        public TextMeshProUGUI countDownGameTimer;

        [Header("Countdown Settings")]
        [SyncVar(hook = nameof(OnTimeChanged))]
        public int remainingTime = 300; // server owns this

        private Coroutine countdownCoroutine;

        [Server]
        private IEnumerator ServerCountdown()
        {
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
                if (serverRulesReady) serverRulesState.matchSecondsRemaining = remainingTime;
                yield return new WaitForSeconds(1f);
            }

            // Decide and settle the result HERE, on the server, before telling anyone.
            // RpcTimerEnded() used to call gamePlayController.OnTimerEnd(), but a ClientRpc
            // body never executes on a headless build — so on the real dedicated server the
            // timeout result was never computed and WinnerLossChallenge was never sent.
            // (Host mode hid this, because a host has a local client that does run the body.)
            if (serverRulesReady)
            {
                serverRulesState.player1Score = (byte)Myplayer1Score;
                serverRulesState.player2Score = (byte)Myplayer2Score;
                TwelveRulesEngine.ExpireMatch(ref serverRulesState);
            }
            ServerFinalize(TwelveEndReason.Timeout);
            RpcTimerEnded();
            yield return new WaitForSeconds(1f);
            "Cleanup".Show();
            MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
        }

        // This runs on all clients whenever remainingTime changes
        private void OnTimeChanged(int oldTime, int newTime)
        {
            int minutes = Mathf.FloorToInt(newTime / 60);
            int seconds = Mathf.FloorToInt(newTime % 60);
            countDownGameTimer.text = string.Format("{0:00}:{1:00}", minutes, seconds);

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
            // Presentation only. The result itself arrives through RpcMatchFinalized, which
            // ServerFinalize() sends after the server has decided and settled. Calling
            // gamePlayController.OnTimerEnd() here would spawn a second, client-decided
            // result panel. Offline/AI is untouched — it still calls OnTimerEnd() itself
            // from GamePlayControllerTwelve.StartCountdown().
        }
        #endregion

        #region Compact authoritative board (Stage 5)

        // The whole board, exactly as the server's rules engine sees it:
        //   0      = empty
        //   1..12  = PLAYER1 piece ids
        //   13..24 = PLAYER2 piece ids
        // Piece identity is carried, not just ownership, because the repetition rule is per-piece.
        // Repetition history itself stays server-only — clients never need it.
        //
        // This is what a reconnecting client should rebuild from: one 25-byte snapshot instead of
        // orchestrating ~49 spawned NetworkIdentities and hoping every SyncVar hook lands in order.
        public readonly SyncList<byte> boardCells = new SyncList<byte>();

        [SyncVar] public byte boardTurn;
        [SyncVar] public byte boardForcedPieceId = TwelveBoardTopology.NoNode;
        [SyncVar] public byte boardPhase;
        [SyncVar] public byte boardOutcome;

        /// <summary>
        /// Server-only: mirror `serverRulesState` onto the wire. Call this after EVERY engine
        /// mutation. Only changed cells are written, so a normal move costs 2-3 cell deltas.
        /// </summary>
        [Server]
        private void ServerPublishBoard()
        {
            byte[] cells = serverRulesState.cells;
            if (cells == null) return;

            if (boardCells.Count != cells.Length)
            {
                boardCells.Clear();
                for (int i = 0; i < cells.Length; i++)
                    boardCells.Add(cells[i]);
            }
            else
            {
                for (int i = 0; i < cells.Length; i++)
                    if (boardCells[i] != cells[i])
                        boardCells[i] = cells[i];
            }

            boardTurn = (byte)serverRulesState.turn;
            boardForcedPieceId = serverRulesState.forcedPieceId;
            boardPhase = (byte)serverRulesState.phase;
            boardOutcome = (byte)serverRulesState.outcome;
        }

        // Publishing from one place beats sprinkling calls after each of the seven engine
        // mutation sites — one missed site is a desync that only shows up under a rare rule.
        // ServerPublishBoard() is change-detected (it writes only differing cells, and Mirror
        // SyncVar setters no-op on an equal value), so an idle frame costs a 25-byte compare
        // and puts nothing on the wire.
        private void LateUpdate()
        {
            if (NetworkServer.active && serverRulesReady)
                ServerPublishBoard();
        }

        /// <summary>
        /// Read the replicated board back out as engine state. Clients use this to render a
        /// snapshot; it deliberately carries no repetition data, which is server-only.
        /// </summary>
        public bool TryGetReplicatedBoard(out TwelveState state)
        {
            state = default;
            if (boardCells.Count != TwelveBoardTopology.CellCount) return false;

            state.cells = new byte[TwelveBoardTopology.CellCount];
            for (int i = 0; i < TwelveBoardTopology.CellCount; i++)
                state.cells[i] = boardCells[i];

            state.turn = (PLAYERS)boardTurn;
            state.forcedPieceId = boardForcedPieceId;
            state.phase = (TwelveMatchPhase)boardPhase;
            state.outcome = (TwelveOutcome)boardOutcome;
            state.player1Score = (byte)Myplayer1Score;
            state.player2Score = (byte)Myplayer2Score;
            state.revision = twelveRevision;
            return true;
        }

        #endregion

        #region Server-authoritative result

        // Set once, by the server, the moment the match is decided. Guards against a second
        // finalize from a later capture, the timeout coroutine, or a reconnect replay.
        [SyncVar] public bool matchFinalized = false;

        // ServerRecordCapture was removed after the rules-engine migration. Capture scoring is
        // part of the single validated move transaction and cannot be invoked independently.

        /// <summary>
        /// Server-only: decide the winner from the server's own scores, settle it with the
        /// backend, then tell both clients what to show. Seat -> user id comes from
        /// NetworkGameManager (PLAYER1 = creator, PLAYER2 = joiner), never from a client.
        /// </summary>
        [Server]
        public void ServerFinalize(TwelveEndReason reason, PLAYERS forcedWinner = PLAYERS.EMPTY)
        {
            // Quits, abandoned matches and reconnect-limit wins are settled by NetworkGameManager on
            // its own path, which never touches matchFinalized. The match clock here is only PAUSED
            // while a seat is empty, not stopped, so if that seat is ever filled again the clock can
            // run out and land a SECOND settlement, awarding the match to whoever happened to lead on
            // score rather than to the player who actually won it.
            NetworkGameManager ngmSettled = NetworkGameManager.Instance;
            if (!matchFinalized && ngmSettled != null && ngmSettled.ServerMatchAlreadySettled)
            {
                Debug.LogWarning($"[12Bead] Finalize ignored (reason={reason}); this match already has a server-side result.");
                matchFinalized = true;
                isGameOver = true;
                if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
                if (turnTimer != null) turnTimer.isTimerRunning = false;
                return;
            }

            if (matchFinalized)
            {
                duplicateFinalizationCount++;
                Debug.LogWarning($"[12Bead] Duplicate finalization ignored reason={reason} count={duplicateFinalizationCount}");
                return;
            }
            matchFinalized = true;
            isGameOver = true;

            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            if (turnTimer != null) turnTimer.isTimerRunning = false;

            PLAYERS winnerSeat = forcedWinner;
            if (winnerSeat == PLAYERS.EMPTY)
            {
                if (Myplayer1Score > Myplayer2Score) winnerSeat = PLAYERS.PLAYER1;
                else if (Myplayer2Score > Myplayer1Score) winnerSeat = PLAYERS.PLAYER2;
                // equal scores -> stays EMPTY -> draw, nobody's balance moves
            }

            var ngm = NetworkGameManager.Instance;
            string winnerId = null;
            if (ngm != null && winnerSeat != PLAYERS.EMPTY)
                winnerId = (winnerSeat == PLAYERS.PLAYER1) ? ngm.creatorData.playerId : ngm.joinerData.playerId;

            Debug.Log($"[SERVER] Finalize | reason={reason} seat={winnerSeat} winnerId={winnerId} " +
                      $"P1={Myplayer1Score} P2={Myplayer2Score}");

            // Settlement. On the headless build this posts to the backend with the deployment
            // auth key; on a client build the same call is a no-op that only refreshes balance.
            if (ApiAndRoomManager._instance != null)
            {
                if (winnerSeat == PLAYERS.EMPTY)
                    ApiAndRoomManager._instance.DrawChallenge(ngm != null ? ngm.transactionId : null);
                else
                    ApiAndRoomManager._instance.WinnerLossChallenge(winnerId);
            }
            else
            {
                Debug.LogError("[SERVER] ApiAndRoomManager missing — 12 Bead result not settled!");
            }

            RpcMatchFinalized(winnerId, winnerSeat == PLAYERS.EMPTY);
        }

        /// <summary>
        /// Clients: open the result UI for a result the server already decided and settled.
        /// </summary>
        [ClientRpc]
        private void RpcMatchFinalized(string winnerUserId, bool isDraw)
        {
            if (countDownGameTimer != null)
                countDownGameTimer.gameObject.SetActive(false);

            if (ResultManager.GameSpawnedFinished) return;
            ResultManager.GameSpawnedFinished = true;

            GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
            if (prefab == null)
            {
                Debug.LogError("WinLose GameManager prefab not found!");
                return;
            }

            var resultManager = Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<ResultManager>();
            if (isDraw)
                resultManager.Draw();
            else
                // IsWin describes winnerUserId's outcome, so each client works out its own side.
                resultManager.HandleGameResultAltMultiplayer(true, winnerUserId);
        }

        #endregion
        #region UNITY
        // The per-frame prize log and empty autosave poll were removed; they only generated noise and work.


        [ClientRpc]
        public void RpcOnTurnTimerEnd()
        {
            "RpcOnTurnTimerEnd".Show();
            GamePlayControllerTwelve.instance.OnTurnTimerExipred();
        }
        [SyncVar]
        public float turnTimerSecond;


        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(_instance.gameObject);
            }

            _instance = this;
        }
        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        [Command(requiresAuthority = false)]
        public void CmdOnServerResetValues()
        {
            GamePlayControllerTwelve.beadsCreated = false;
            TwelveRulesEngine.CreateInitialState(ref serverRulesState);
            serverRulesReady = true;
            twelveRevision = 0;
            Myplayer1Score = 0;
            Myplayer2Score = 0;
            matchFinalized = false;
        }
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!MirrorNetwork.Instance.isMasterClient)
            {
                gameBoard.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }

        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            TwelveRulesEngine.CreateInitialState(ref serverRulesState);
            serverRulesReady = true;
            //  gamePlayController.MirrorCreateTeamsBeads();
        }

        [Server]
        public void StartGameTimer()
        {
            Debug.Log("[SERVER] Starting countdown timer...");
            if (countdownCoroutine != null)
                StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(ServerCountdown());
        }
        private void OnDisable()
        {
        }


        public void SpawnNetworkObject()
        {
            var network = Instantiate(twelveBeadNetworkPrefab);
            NetworkServer.Spawn(network);
        }
        public void Start()
        {
            // Set player IDs based on server/client
            // if (NetworkServer.active)
            // {

            //     GamePlayControllerTwelve.myId = PLAYERS.PLAYER1.GetHashCode();
            //     GamePlayControllerTwelve.aiId = PLAYERS.PLAYER2.GetHashCode();
            // }
            // else
            // {
            //     GamePlayControllerTwelve.myId = PLAYERS.PLAYER2.GetHashCode();
            //     GamePlayControllerTwelve.aiId = PLAYERS.PLAYER1.GetHashCode();
            // }

            // // Notify server that this player has loaded
            // if (isClient)
            // {
            //     CmdSetPlayerLoaded();
            // }

            if (NetworkServer.active)
            {
                SpawnNetworkObject();
                StartCoroutine(WaitForBothPlayersThenStartCountdown());
            }
            else
            {
                //   Debug.Log("PlayerClientSide");
                GamePlayControllerTwelve.myId = MirrorNetwork.Instance.isMasterClient ? PLAYERS.PLAYER1.GetHashCode() : PLAYERS.PLAYER2.GetHashCode();
                GamePlayControllerTwelve.aiId = MirrorNetwork.Instance.isMasterClient ? PLAYERS.PLAYER2.GetHashCode() : PLAYERS.PLAYER1.GetHashCode();
                //  Debug.Log("PlayerClientSidemyId" + GamePlayControllerTwelve.myId);
                //  Debug.Log("PlayerClientSideaiId" + GamePlayControllerTwelve.aiId);
            }

        }

        public float countdownTime = 3f;

        //public TextMeshProUGUI countdownText;
        private IEnumerator StartCountdown()
        {
            float timeLeft = countdownTime;

            while (timeLeft > 0)
            {
                // if (countdownText != null)
                //    countdownText.text = Mathf.Ceil(timeLeft).ToString();

                yield return new WaitForSeconds(1f);
                timeLeft--;
            }

            // if (countdownText != null)
            //    countdownText.text = "GO!";

            StartGame();
        }

        // Server: don't fire the toss until BOTH players are present and ready.
        // If a player disconnects pre-toss, this loop pauses; once they reconnect
        // and re-register their MirrorPlayerPrefab (which refreshes Creator/JoinerRef),
        // the toss fires normally instead of leaving the reconnected client stuck.
        [Server]
        private IEnumerator WaitForBothPlayersThenStartCountdown()
        {
            Debug.Log("[SERVER] Waiting for both players to be present before toss...");
            float waited = 0f;
            while (true)
            {
                var ngm = NetworkGameManager.Instance;
                bool bothReady = ngm != null
                    && ngm.CreatorRef != null && ngm.CreatorRef.isReady
                    && ngm.JoinerRef  != null && ngm.JoinerRef.isReady;

                if (bothReady) break;

                waited += 0.5f;
                if (waited % 5f < 0.5f)
                    Debug.Log($"[SERVER] still waiting for players (creator={ngm?.CreatorRef != null}, joiner={ngm?.JoinerRef != null})...");
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[SERVER] Both players present, starting countdown...");
            yield return StartCountdown();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
        }

        #endregion

        #region Mirror Callbacks
        // CmdUpdateScore(p1, p2) was removed deliberately. It let any client write both
        // scores straight into the server's SyncVars — one call with (12, 0) won the match.
        // The server counts captures inside ServerCommitTwelveMove(); scores travel
        // server -> client only, through the SyncVars and RpcUpdateScoreUI below.

        [ClientRpc]
        private void RpcUpdateScoreUI(int p1Score, int p2Score)
        {
            if (gamePlayController != null)
            {
                // Scores were already written to SyncVars on the server. This RPC only
                // refreshes presentation after those authoritative values changed.
                gamePlayController.RefreshScore();

                Debug.Log($"[CLIENT] Score UI Updated: P1={p1Score}, P2={p2Score}");
            }
        }
        /// <summary>
        /// Called when client disconnects
        /// </summary>
        public void OnClientDisconnect()
        {
            Debug.LogError("Disconnection");
            if (NetworkServer.active || NetworkClient.active)
            {
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.SceneTransition;
                if (NetworkServer.active)
                    NetworkManager.singleton.StopHost();
                else
                    NetworkManager.singleton.StopClient();
            }
            SceneManager.LoadScene("Home");
        }

        /// <summary>
        /// Called when leaving room
        /// </summary>
        public void OnLeaveRoom()
        {
            if (NetworkClient.isConnected)
            {
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.UserRequested;
                NetworkClient.Disconnect();
            }
            SceneManager.LoadScene("Home");
        }

        // OnPlayerDisconnected(int) was removed: nothing called it (no C# caller, no
        // UnityEvent wiring in either scene or prefab), and its only job was to fire
        // CmdWinPlayer with a client-decided winner. The explicit Leave button is handled
        // by NetworkGameManager's generic server-side disconnect settlement.

        #endregion

        #region Commands (Client to Server)

        // CmdSetPlayerLoaded(), CmdSendBeadSelected(...) and CmdSendMoveSelected(...)
        // were removed. The first was unused; the relay Commands trusted client-chosen
        // player/bead ids and replayed them without validating board state.

        public void SubmitTwelveMove(byte fromNode, byte toNode)
        {
            if (movePending || !NetworkClient.active) return;
            movePending = true;
            uint requestId = nextRequestId++;
            pendingRequestId = requestId;
            uint expectedRevision = twelveRevision;
            Debug.Log($"[12Bead] SubmitTwelveMove from={fromNode} to={toNode} revision={expectedRevision} requestId={requestId}");
            if (movePendingTimeoutCoroutine != null)
                StopCoroutine(movePendingTimeoutCoroutine);
            movePendingTimeoutCoroutine = StartCoroutine(TwelveMovePendingTimeout(requestId));
            CmdTwelveSubmitMove(fromNode, toNode, expectedRevision, requestId);
        }

        private IEnumerator TwelveMovePendingTimeout(uint requestId)
        {
            yield return new WaitForSecondsRealtime(MOVE_PENDING_TIMEOUT_SECONDS);
            if (!movePending || pendingRequestId != requestId) yield break;

            movePending = false;
            movePendingTimeoutCoroutine = null;
            gamePlayController?.ClearTwelveMovePending();
            const string message = "The move timed out. Please try again.";
            Debug.LogWarning($"[12Bead] Move pending timeout requestId={requestId} revision={twelveRevision}");
            if (PopupMessageManager.instance != null)
                PopupMessageManager.instance.ShowPopUp(message, "Connection", 3f);
            else
                Toaster.ShowAToast(message);
        }

        private void ClearTwelveMovePending(uint requestId)
        {
            if (movePending && pendingRequestId != requestId) return;
            movePending = false;
            if (movePendingTimeoutCoroutine != null)
            {
                StopCoroutine(movePendingTimeoutCoroutine);
                movePendingTimeoutCoroutine = null;
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdTwelveSubmitMove(byte fromNode, byte toNode, uint expectedRevision,
            uint requestId, NetworkConnectionToClient sender = null)
        {
            ServerHandleTwelveSubmitMove(fromNode, toNode, expectedRevision, requestId, sender);
        }

        // Kept separate from Mirror's generated Command wrapper so a dedicated-server build can
        // fuzz the exact handler without creating a host-mode client in the same process.
        [Server]
        private void ServerHandleTwelveSubmitMove(byte fromNode, byte toNode, uint expectedRevision,
            uint requestId, NetworkConnectionToClient sender)
        {
            var ngm = NetworkGameManager.Instance;

            // 1. Match must be live and both seats present.
            if (matchFinalized || ngm == null || ngm.currentPlayerCount < 2 ||
                ngm.CreatorRef == null || !ngm.CreatorRef.isReady ||
                ngm.JoinerRef == null || !ngm.JoinerRef.isReady)
            {
                RejectTwelveMove(sender, requestId, "Match is not in play");
                return;
            }

            // 2. Identity comes only from Mirror's injected sender.
            PLAYERS seat;
            if (!TryGetTwelveSeat(sender, out seat))
            {
                RejectTwelveMove(sender, requestId, "Sender is not seated");
                return;
            }

            // 3. The mapped seat must own the current turn.
            if (TwelveBeadNetworkManager.instance == null ||
                TwelveBeadNetworkManager.instance.CurrentPlayerIndex != (int)seat)
            {
                RejectTwelveMove(sender, requestId, "Out of turn");
                return;
            }

            // 4. Reject stale and duplicate state submissions.
            if (IsDuplicateTwelveRequest(sender, requestId))
            {
                duplicateRequestCount++;
                RejectTwelveMove(sender, requestId, "Duplicate request");
                return;
            }
            if (expectedRevision != twelveRevision)
            {
                RejectTwelveMove(sender, requestId, "Stale revision");
                return;
            }

            // 5. Bound duplicate memory and submit bursts before applying any rules state.
            if (!AcceptTwelveRequest(sender, requestId))
            {
                RejectTwelveMove(sender, requestId, "Duplicate or rate-limited request");
                return;
            }

            if (!serverRulesReady)
            {
                TwelveRulesEngine.CreateInitialState(ref serverRulesState);
                serverRulesState.turn = seat;
                serverRulesReady = true;
            }
            TwelveState previousRulesState = serverRulesState.Clone();
            TwelveApplyResult rulesResult = TwelveRulesEngine.TryApplyMove(ref serverRulesState,
                seat, fromNode, toNode);
            if (!rulesResult.applied)
            {
                RejectTwelveMove(sender, requestId, rulesResult.rejectReason.ToString());
                return;
            }

            // Scene objects are presentation mirrors now. A mismatch is logged against the
            // Stage 3 validator, but only a broken view binding can prevent the engine commit.
            if (gamePlayController == null || gamePlayController.allNodes == null ||
                fromNode >= gamePlayController.allNodes.Count || toNode >= gamePlayController.allNodes.Count)
            {
                serverRulesState = previousRulesState;
                RejectTwelveMove(sender, requestId, "Board view is unavailable");
                return;
            }

            NodeScriptTwelve origin = gamePlayController.allNodes[fromNode];
            NodeScriptTwelve destination = gamePlayController.allNodes[toNode];
            BeadScriptTwelve movingBead = origin != null ? origin.currentBead : null;
            bool isJump = rulesResult.move.IsCapture;
            NodeScriptTwelve midpoint = isJump && rulesResult.move.capturedNode < gamePlayController.allNodes.Count
                ? gamePlayController.allNodes[rulesResult.move.capturedNode] : null;
            BeadScriptTwelve capturedBead = midpoint != null ? midpoint.currentBead : null;
            bool viewReady = movingBead != null && movingBead.playerID == seat && !movingBead.isDead &&
                movingBead.gameObject.activeSelf && movingBead.currentNode == origin && destination != null &&
                !destination.isOccupied && (!isJump || capturedBead != null &&
                    capturedBead.playerID != PLAYERS.EMPTY && capturedBead.playerID != seat &&
                    !capturedBead.isDead && capturedBead.gameObject.activeSelf);
            if (!viewReady)
            {
                serverRulesState = previousRulesState;
                RejectTwelveMove(sender, requestId, "Board view is out of sync");
                return;
            }

            bool legacyJump;
            NodeScriptTwelve legacyMidpoint;
            bool legacyAccepted = TryResolveTwelveMove(origin, destination, out legacyJump, out legacyMidpoint) &&
                (!legacyJump || legacyMidpoint == midpoint) &&
                (forcedChainBead == null || movingBead == forcedChainBead && legacyJump) &&
                (movingBead.isCanMove(destination) || !ServerHasAnyUnrestrictedMove(seat));
            if (!legacyAccepted || legacyJump != isJump)
                Debug.LogError($"[12Bead] Rules shadow mismatch engineAccepted=true legacyAccepted={legacyAccepted} from={fromNode} to={toNode}");

            Debug.Log($"[12Bead] Server accept from={fromNode} to={toNode} revision={expectedRevision} requestId={requestId} actor={seat}");
            ServerCommitTwelveMove(fromNode, toNode, seat, movingBead, destination,
                isJump, midpoint, capturedBead, rulesResult);
        }

        private bool TryGetTwelveSeat(NetworkConnectionToClient sender, out PLAYERS seat)
        {
            seat = PLAYERS.EMPTY;
            var ngm = NetworkGameManager.Instance;
            if (sender == null || ngm == null) return false;
            if (sender == ngm.CreatorRef) seat = PLAYERS.PLAYER1;
            else if (sender == ngm.JoinerRef) seat = PLAYERS.PLAYER2;
            return seat != PLAYERS.EMPTY;
        }

        private void RejectTwelveMove(NetworkConnectionToClient sender, uint requestId, string reason)
        {
            uint count;
            rejectedMoveCounters.TryGetValue(reason, out count);
            rejectedMoveCounters[reason] = count + 1;
            if (reason == "Stale revision") staleRequestCount++;
            Debug.LogWarning($"[SERVER] 12 Bead move rejected: {reason} request={requestId} revision={twelveRevision}");
            if (sender != null)
                TargetTwelveMoveRejected(sender, requestId, reason, twelveRevision);
        }

        private bool AcceptTwelveRequest(NetworkConnectionToClient sender, uint requestId)
        {
            Queue<uint> idQueue;
            HashSet<uint> idSet;
            if (!recentRequestIds.TryGetValue(sender, out idQueue))
            {
                idQueue = new Queue<uint>();
                idSet = new HashSet<uint>();
                recentRequestIds.Add(sender, idQueue);
                recentRequestIdSets.Add(sender, idSet);
            }
            else
            {
                idSet = recentRequestIdSets[sender];
            }

            if (idSet.Contains(requestId)) return false;

            Queue<double> times;
            if (!submitTimes.TryGetValue(sender, out times))
            {
                times = new Queue<double>();
                submitTimes.Add(sender, times);
            }

            double now = NetworkTime.time;
            while (times.Count > 0 && now - times.Peek() >= 1d)
                times.Dequeue();
            if (times.Count >= MAX_VALID_SUBMITS_PER_SECOND) return false;

            times.Enqueue(now);
            idQueue.Enqueue(requestId);
            idSet.Add(requestId);
            while (idQueue.Count > MAX_REQUEST_IDS_PER_CONNECTION)
                idSet.Remove(idQueue.Dequeue());
            return true;
        }

        private bool IsDuplicateTwelveRequest(NetworkConnectionToClient sender, uint requestId)
        {
            HashSet<uint> ids;
            return sender != null && recentRequestIdSets.TryGetValue(sender, out ids) && ids.Contains(requestId);
        }

        private bool TryResolveTwelveMove(NodeScriptTwelve origin, NodeScriptTwelve destination,
            out bool isJump, out NodeScriptTwelve midpoint)
        {
            isJump = false;
            midpoint = null;
            if (origin == null || destination == null || origin.connectedNodesList == null)
                return false;

            for (int i = 0; i < origin.connectedNodesList.Count; i++)
            {
                ConnectedNodesTwelve connection = origin.connectedNodesList[i];
                if (connection != null && connection.nextNode == destination)
                    return true;
            }

            for (int i = 0; i < origin.connectedNodesList.Count; i++)
            {
                ConnectedNodesTwelve connection = origin.connectedNodesList[i];
                if (connection != null && connection.nextNextNode == destination)
                {
                    isJump = true;
                    midpoint = connection.nextNode;
                    return midpoint != null;
                }
            }
            return false;
        }

        [Server]
        private void ServerCommitTwelveMove(byte fromNode, byte toNode, PLAYERS seat,
            BeadScriptTwelve movingBead, NodeScriptTwelve destination, bool isJump,
            NodeScriptTwelve midpoint, BeadScriptTwelve capturedBead, TwelveApplyResult rulesResult)
        {
            bool wasInCaptureChain = forcedChainBead != null;
            NodeScriptTwelve origin = gamePlayController.allNodes[fromNode];

            // One authoritative transaction: origin, destination, then derived capture.
            origin.OnOccupied(false);
            movingBead.currentNode = destination;
            movingBead.ServerSetNodeValue(destination.nodeNo);

            // Move the SERVER's own transform too. Every bead prefab carries
            // NetworkTransformReliable with syncDirection = ServerToClient, syncPosition = 1 and
            // syncInterval = 0, so the server's transform is broadcast to both clients every
            // frame. Leaving it parked on the origin here meant the client played the committed
            // move and was then snapped straight back — the bead visibly stepped forward and
            // returned. The pre-Stage-3 code got this for free because the server replayed the
            // move through OnMoveSelected(), which tweened its own transform.
            // Offset matches MirrorCreateTeamsBeads() and BeadScriptTwelve.OnNodeChanged().
            movingBead.transform.position = destination.transform.position + new Vector3(0, 0, -2);

            destination.OnOccupied(true);
            destination.SetBead(movingBead);

            byte capturedNode = byte.MaxValue;
            if (isJump)
            {
                capturedNode = (byte)gamePlayController.allNodes.IndexOf(midpoint);
                capturedBead.isDead = true;
                midpoint.OnOccupied(false);
                capturedBead.SetVisible(false);
                capturedBead.gameObject.SetActive(false);
                Myplayer1Score = serverRulesState.player1Score;
                Myplayer2Score = serverRulesState.player2Score;
                Debug.Log($"[SERVER] Capture by {seat} | P1={Myplayer1Score} P2={Myplayer2Score}");
                RpcUpdateScoreUI(Myplayer1Score, Myplayer2Score);
            }

            ServerRecordTwelveHistory(movingBead, destination, wasInCaptureChain);

            bool legacyChainContinues = isJump && ServerHasCaptureForBead(movingBead, seat, true);
            bool chainContinues = rulesResult.chainContinues;
            bool turnChanged = rulesResult.turnChanged;
            if (legacyChainContinues != chainContinues)
                Debug.LogError($"[12Bead] Rules shadow mismatch chain engine={chainContinues} legacy={legacyChainContinues}");
            if (chainContinues)
            {
                forcedChainBead = movingBead;
            }
            else
            {
                forcedChainBead = null;
                if (turnChanged && TwelveBeadNetworkManager.instance != null)
                    TwelveBeadNetworkManager.instance.ServerSetCurrentPlayer(serverRulesState.turn);
            }

            if (!matchFinalized && rulesResult.match.ended)
            {
                PLAYERS winner = rulesResult.match.outcome == TwelveOutcome.Player1
                    ? PLAYERS.PLAYER1 : rulesResult.match.outcome == TwelveOutcome.Player2
                        ? PLAYERS.PLAYER2 : PLAYERS.EMPTY;
                TwelveEndReason reason = rulesResult.match.reason == TwelveMatchEndReason.ScoreLimit
                    ? TwelveEndReason.ScoreLimit : TwelveEndReason.Checkmate;
                ServerFinalize(reason, winner);
            }

            if (matchFinalized)
            {
                forcedChainBead = null;
                chainContinues = false;
            }

            twelveRevision = rulesResult.newRevision;
            Debug.Log($"[12Bead] ServerCommitTwelveMove from={fromNode} to={toNode} captured={capturedNode} turnChanged={turnChanged} newRevision={twelveRevision}");
            RpcTwelveMoveCommitted(fromNode, toNode, capturedNode, (byte)seat,
                chainContinues, twelveRevision);

            if (turnChanged && !matchFinalized)
            {
                ServerResetTurnTimer();
                TwelveBeadNetworkManager.instance.ServerBroadcastCurrentTurn();
            }
        }

        [Server]
        private void ServerRecordTwelveHistory(BeadScriptTwelve movingBead,
            NodeScriptTwelve destination, bool wasInCaptureChain)
        {
            if (wasInCaptureChain) return;
            bool isPlayerOne = movingBead.playerID == PLAYERS.PLAYER1;
            BeadScriptTwelve previous = isPlayerOne ? lastMovedBeadP1 : lastMovedBeadP2;
            if (previous != null && previous != movingBead)
                previous.ClearMoveHistory();
            if (isPlayerOne) lastMovedBeadP1 = movingBead;
            else lastMovedBeadP2 = movingBead;
            movingBead.RecordMoveNode(destination);
        }

        private bool ServerHasCaptureForBead(BeadScriptTwelve bead, PLAYERS seat,
            bool requireUnrestricted)
        {
            if (bead == null || bead.isDead || !bead.gameObject.activeSelf || bead.currentNode == null)
                return false;

            for (int i = 0; i < bead.currentNode.connectedNodesList.Count; i++)
            {
                ConnectedNodesTwelve connection = bead.currentNode.connectedNodesList[i];
                NodeScriptTwelve middle = connection != null ? connection.nextNode : null;
                NodeScriptTwelve landing = connection != null ? connection.nextNextNode : null;
                if (middle == null || landing == null || landing.isOccupied || !middle.isOccupied)
                    continue;
                BeadScriptTwelve enemy = middle.currentBead;
                if (enemy == null || enemy.playerID == PLAYERS.EMPTY || enemy.playerID == seat || enemy.isDead)
                    continue;
                if (!requireUnrestricted || bead.isCanMove(landing)) return true;
            }
            return false;
        }

        private bool ServerHasAnyUnrestrictedMove(PLAYERS seat)
        {
            if (forcedChainBead != null)
                return ServerHasCaptureForBead(forcedChainBead, seat, true);

            List<BeadScriptTwelve> team = seat == PLAYERS.PLAYER1
                ? gamePlayController.team1Beads : gamePlayController.team2Beads;
            for (int i = 0; i < team.Count; i++)
            {
                BeadScriptTwelve bead = team[i];
                if (bead == null || bead.isDead || !bead.gameObject.activeSelf || bead.currentNode == null)
                    continue;

                for (int j = 0; j < bead.currentNode.connectedNodesList.Count; j++)
                {
                    ConnectedNodesTwelve connection = bead.currentNode.connectedNodesList[j];
                    NodeScriptTwelve adjacent = connection != null ? connection.nextNode : null;
                    if (adjacent != null && !adjacent.isOccupied && bead.isCanMove(adjacent))
                        return true;
                }
                if (ServerHasCaptureForBead(bead, seat, true)) return true;
            }
            return false;
        }

        private bool ServerHasAnyLegalMove(PLAYERS seat)
        {
            List<BeadScriptTwelve> team = seat == PLAYERS.PLAYER1
                ? gamePlayController.team1Beads : gamePlayController.team2Beads;
            for (int i = 0; i < team.Count; i++)
            {
                BeadScriptTwelve bead = team[i];
                if (bead == null || bead.isDead || !bead.gameObject.activeSelf || bead.currentNode == null)
                    continue;
                for (int j = 0; j < bead.currentNode.connectedNodesList.Count; j++)
                {
                    ConnectedNodesTwelve connection = bead.currentNode.connectedNodesList[j];
                    if (connection != null && connection.nextNode != null && !connection.nextNode.isOccupied)
                        return true;
                }
                if (ServerHasCaptureForBead(bead, seat, false)) return true;
            }
            return false;
        }

        [TargetRpc]
        private void TargetTwelveMoveRejected(NetworkConnectionToClient target, uint requestId,
            string reason, uint serverRevision)
        {
            ClearTwelveMovePending(requestId);
            Debug.LogWarning($"[CLIENT] 12 Bead move {requestId} rejected: {reason}");
            StartCoroutine(ReRenderTwelveAfterRevision(serverRevision));
        }

        [ClientRpc]
        private void RpcTwelveMoveCommitted(byte fromNode, byte toNode, byte capturedNode,
            byte seat, bool chainContinues, uint newRevision)
        {
            ClearTwelveMovePending(pendingRequestId);
            Debug.Log($"[12Bead] RpcTwelveMoveCommitted from={fromNode} to={toNode} captured={capturedNode} seat={seat} chain={chainContinues} newRevision={newRevision}");
            committedMoves.Enqueue(new TwelveCommittedMove
            {
                fromNode = fromNode,
                toNode = toNode,
                capturedNode = capturedNode,
                seat = (PLAYERS)seat,
                chainContinues = chainContinues,
                revision = newRevision
            });
            if (committedMoveCoroutine == null)
                committedMoveCoroutine = StartCoroutine(ApplyCommittedTwelveMoves());
        }

        private IEnumerator ApplyCommittedTwelveMoves()
        {
            while (committedMoves.Count > 0)
            {
                TwelveCommittedMove move = committedMoves.Peek();
                float waitStarted = Time.realtimeSinceStartup;
                bool waitLogged = false;
                while (twelveRevision < move.revision)
                {
                    if (!waitLogged && Time.realtimeSinceStartup - waitStarted >= 1f)
                    {
                        waitLogged = true;
                        Debug.LogWarning($"[12Bead] ApplyCommittedTwelveMoves waiting localRevision={twelveRevision} committedRevision={move.revision}");
                    }
                    yield return null;
                }

                committedMoves.Dequeue();
                if (gamePlayController != null)
                    gamePlayController.ApplyTwelveMoveCommitted(move.fromNode, move.toNode,
                        move.capturedNode, move.seat, move.chainContinues);
                yield return new WaitForSeconds(0.16f);
            }
            committedMoveCoroutine = null;
        }

        private IEnumerator ReRenderTwelveAfterRevision(uint serverRevision)
        {
            while (twelveRevision < serverRevision)
                yield return null;
            if (gamePlayController != null)
                gamePlayController.ReRenderTwelveServerState();
        }

        // CmdSetGameOver was removed with its SetGameOver() wrapper: nothing called either,
        // and it let a client flip the server's isGameOver flag. ServerFinalize() owns that now.

        #endregion

        #region ClientRpc (Server to All Clients)

        // The opponent-only TargetMoveSelectedMessage/TargetBeadSelectedMessage relay,
        // SendToOpponent/SendBeadToOpponent and SendToServer were removed. A committed
        // server delta is the only network path that can move or capture a bead.

        #endregion

        #region SyncVar Hooks

        private void OnPlayerTurnChanged(int oldTurnId, int newTurnId)
        {
            SetPlayerTurn(newTurnId);
        }

        #endregion

        #region Game Logic

        private bool CheckAllPlayersLoaded()
        {
            if (playerLoadedStatus.Count < 2)
            {
                return false;
            }

            foreach (var status in playerLoadedStatus.Values)
            {
                if (!status)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnCountdownTimerIsExpired()
        {
            Debug.Log("CountDown Has Been Expired");
            StartGame();
        }

        // Called when countdown/timer ends to start the game
        // private void StartGame()
        // {
        //     if (NetworkServer.active)
        //     {
        //         SetTurnData( UnityEngine.Random.Range(1, 3));
        //     }
        // }
        private void StartGame()
        {
            Debug.LogError("[SERVER] StartGame() called!");

            if (NetworkServer.active && !TwelveBeadNetworkManager.isTossDone)
            {
                Debug.LogError("[SERVER] Starting toss since it's not done yet...");
                TwelveBeadNetworkManager.instance.DoToss();
            }
            else if (TwelveBeadNetworkManager.isTossDone)
            {
                Debug.LogError("[SERVER] Toss already done — skipping new toss.");
            }
        }
        public void SetPlayerTurn(int turnId, bool resetTimer = true)
        {
            Debug.LogError("SetPlayerTurn" + turnId);
            if (gamePlayController == null) return;

            GamePlayControllerTwelve.currentPlayerTurn = (PLAYERS)turnId;
            // Debug.LogError("GamePlayControllerTwelve.currentPlayerTurn" +GamePlayControllerTwelve.currentPlayerTurn);
            lastPlayerTurnId = turnId;

            gamePlayController.StartGame();

            if (resetTimer && turnTimer != null && !GamePlayControllerTwelve.isOnlineMultiplayer)
            {
                Debug.Log("Timer" + turnTimer);
                turnTimer.ResetRound();
            }
        }

        // CmdResetRound() was removed: clients could reset the server turn timer without
        // making a move. Only a committed turn change or the initial toss resets it now.

        [Server]
        public void ServerApplyTurnState(PLAYERS turn)
        {
            if (!serverRulesReady)
            {
                TwelveRulesEngine.CreateInitialState(ref serverRulesState);
                serverRulesReady = true;
            }
            serverRulesState.turn = turn;
            currentTurnId = (int)turn;
            GamePlayControllerTwelve.currentPlayerTurn = turn;
        }

        // ServerAdvanceTurn was removed: only an accepted move or an expired turn may advance
        // the authoritative rules state.

        [Server]
        public void ServerResetTurnTimer()
        {
            if (turnTimer == null) return;
            turnTimer.ResetRound();
            turnTimerSecond = turnTimer.roundTime;
        }

        [Server]
        public void ServerExpireTurn()
        {
            if (matchFinalized) return;
            if (!serverRulesReady)
            {
                TwelveRulesEngine.CreateInitialState(ref serverRulesState);
                serverRulesState.turn = (PLAYERS)currentTurnId;
                serverRulesReady = true;
            }
            TwelveTurnResult rulesResult = TwelveRulesEngine.ExpireTurn(ref serverRulesState);
            PLAYERS expiredSeat = rulesResult.expiredPlayer;
            forcedChainBead = null;
            bool turnChanged = rulesResult.turnChanged && TwelveBeadNetworkManager.instance != null &&
                TwelveBeadNetworkManager.instance.ServerSetCurrentPlayer(rulesResult.nextPlayer);
            if (!turnChanged) return;

            bool legacyNoMove = !ServerHasAnyLegalMove((PLAYERS)currentTurnId);
            if (legacyNoMove != rulesResult.match.ended)
                Debug.LogError($"[12Bead] Rules shadow mismatch expired-turn noMove engine={rulesResult.match.ended} legacy={legacyNoMove}");
            if (rulesResult.match.ended)
                ServerFinalize(TwelveEndReason.Checkmate, expiredSeat);

            twelveRevision = rulesResult.newRevision;
            if (!matchFinalized)
                ServerResetTurnTimer();

            // State changed first. These RPCs only refresh client presentation.
            RpcOnTurnTimerEnd();
            TwelveBeadNetworkManager.instance.ServerBroadcastCurrentTurn();
        }

        #endregion

        #region Public Methods (Called by GamePlayController)

        // CmdMoveSelectedMessage(...) and CmdSetBeadStatus(...) were removed: clients
        // could choose movement/capture identities. ServerCommitTwelveMove derives both.

        public void LeaveRoom()
        {
            OnLeaveRoom();
        }

        #endregion
    }
}
