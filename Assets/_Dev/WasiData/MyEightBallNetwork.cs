using BallPool;
using BallPool.Mechanics;
using System.Collections.Generic;
using DG.Tweening;
using Mirror;
using Mirror.Examples.Pong;
using NetworkManagement;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using UnityExtensions;
using static events;

public enum EightBallShotPhase
{
    WaitingForPlayers = 0,
    Aiming = 1,
    BallInHand = 2,
    Simulating = 3,
    Resolving = 4,
    GameOver = 5
}

public enum EightBallOutcomeCode
{
    None = 0,
    Scratch = 1,
    WeakBreak = 2,
    WrongBallHit = 3,
    NoRightBallPotted = 4,
    LegalPotContinue = 5,
    TurnChanged = 6,
    LegalBlackWin = 7,
    IllegalBlackLoss = 8
}

public class MyEightBallNetwork : NetworkBehaviour
{
    public static MyEightBallNetwork Instance;
    public AightBallPoolNetworkMessenger poolMessenger;





    [SyncVar(hook = nameof(OnTurnChanged))]
    private int currentTurnId = -1;

    [SyncVar(hook = nameof(OnShotPhaseChanged))]
    private int shotPhase = (int)EightBallShotPhase.WaitingForPlayers;

    [SyncVar(hook = nameof(OnCanShootChanged))]
    private bool canShoot = false;

    public static bool IsTossDone;

    [SyncVar]
    private bool tableOpened = true;


    // Reliable ball-in-hand DRAG flag. Set on the SERVER by the shooter's drag Cmds
    // (SelectBallPosition => true, SetBallPosition => false) and reset on turn change. Being a
    // SyncVar it is reconnect-safe and cannot be missed like the old one-shot cue-visibility relay:
    // its hook (OnBallDraggingChanged -> ShotController.ApplyNetworkBallDragging on the WATCHER)
    // hides the whole cue rig during the drag and shows + rebuilds it when the drag ends, so the
    // stick and aim line can never desync ("stick on, line missing during opponent ball-in-hand").
    // MUST stay between tableOpened and breakShotDone to match the client's SyncVar order (Mirror).
    [SyncVar(hook = nameof(OnBallDraggingChanged))]
    private bool ballIsDragging = false;

    [SyncVar]
    private bool breakShotDone = false;
    // Read-only view for the reconnect flow: breakShotDone==true means this is a MID-MATCH (re)sync — there
    // is real board state to restore, so the loading overlay is worth showing. Fresh starts / pre-break
    // reconnects have nothing to restore and skip it.
    public bool BreakShotDone => breakShotDone;

    [SyncVar]
    private string player1Id = "";

    [SyncVar]
    private string player2Id = "";





    [SyncVar(hook = nameof(OnGameOverChanged))]
    private bool isGameOver = false;
    [SyncVar]
    private string gameWinnerId = "";

    // Flipped by the server ResultPanelRevealDelay seconds after the win is declared, and the reason
    // it is a SyncVar rather than only the RpcOpenResultPanel broadcast: an Rpc reaches ONLY the
    // clients connected at the instant it is sent. Mirror's "reliable" channel guarantees ordered
    // delivery over a live connection — it does not replay anything to a client that was away, so a
    // player who dropped during the reveal window could never receive it. SyncVars ARE part of the
    // spawn payload, so a reconnecting client reads the current value and opens the panel from
    // state instead of from a message it missed.
    [SyncVar(hook = nameof(OnResultPanelDueChanged))]
    private bool resultPanelDue = false;

    // Gap between the "You Win / You Lose" caption and the result panel. Owned by the SERVER so both
    // clients flip at the same moment instead of each running its own local timer.
    private const float ResultPanelRevealDelay = 3f;

    // The Edgegap deployment used to be torn down 1 s after the win was declared. Both the reveal
    // above and the reconnect window need the server ALIVE, so the teardown now waits until well
    // past the reveal.
    private const float ServerCleanupDelay = 10f;







    private float lastCuePivotY, lastCueVerticalX, lastCueSliderZ, lastCueForce;
    private Vector2 lastCueDisplacement;
    private bool hasCueState = false;
    // Last cue-rig visibility relayed through CmdSwitchCueState. The rig show/hide is a one-shot
    // relay stream, so a client that reconnects mid ball-in-hand misses the hide and re-shows the
    // stick every frame from SyncWithNetwork ("stick on both sides after reconnect"). The reconnect
    // restore replays this cached value so the rejoining client converges to the real visibility.
    private bool lastCueVisible = true;
    private int pendingScratchPlayerId = -1;
    private int activeShotPlayerId = -1;


    private float activeShotStartedAtRealtime = 0f;



    private bool[] preShotBallPocketed;
    private bool activeShotAfterBreak;
    private string activeShotImpulse = string.Empty;
    private Vector3 activeShotCueBallPosition;
    private Vector3 activeShotCuePivotPosition;
    private float activeShotCuePivotLocalRotationY;
    private float activeShotCueVerticalLocalRotationX;
    private Vector2 activeShotCueDisplacementLocalPositionXY;
    private float activeShotCueSliderLocalPositionZ;
    private float activeShotForce;
    private Vector3[] activeShotBallPositions = new Vector3[0];
    private int authoritativeShotSequence = 0;
    private int activeShotSequence = 0;
    private int latestStartedShotSequence = 0;
    // Realtime of the last shooter playback frame received this shot. Used for server-side settle
    // detection and to finalise a dropped shooter from the last accepted server board.
    private float lastShotFrameRealtime = 0f;
    private float lastShooterFrameTime = 0f;
    // A frame gap is diagnostic only while the shooter is connected. Unreliable stream silence
    // does not prove the shot ended; the reliable final board must arrive before scoring it.
    private const float ShooterFrameStarvationSeconds = 1.0f;
    public int ActiveShotSequence => latestStartedShotSequence > activeShotSequence ? latestStartedShotSequence : activeShotSequence;
    public int ActiveShotPlayerId => activeShotPlayerId;





    private bool shotStopConsumed = true;




    private float lastTurnPlayTime = 0f;
    private int pendingTimerRewardReconnectPlayerId = -1;
    private int pendingTimerRewardTurnPlayerId = -1;
    private bool timerConnectionMonitorReady = false;
    private bool player1WasConnectedForTimer = false;
    private bool player2WasConnectedForTimer = false;
    private bool reconnectStateSyncCompleteReceived = true;
    private bool reconnectBallsSyncInProgress = false;
    private Coroutine reconnectOverlayHideRoutine;
    private readonly HashSet<string> reconnectStateSyncedPlayerIds = new HashSet<string>();
    // Client-side retry for a DEFERRED reconnect state sync — see RetryReconnectStateSyncAfterDefer.
    // These are instance fields on a server-spawned object, so a fresh reconnect gets a fresh budget.
    private Coroutine reconnectStateRetryRoutine;
    private int reconnectStateRetryAttempts = 0;
    private const int ReconnectStateRetryMaxAttempts = 5;
    private const float ReconnectStateRetryShotWaitTimeout = 30f;

    private enum NetworkTurnChangeReason
    {
        ShotEnd = 0,
        Timeout = 1
    }








    private System.Collections.Generic.Dictionary<string, int> ballTypeByPlayerId
        = new System.Collections.Generic.Dictionary<string, int>();

    // ---- MatchFlow (server game-event log) — logging only, never read by game logic ----
    private string _flowWinReason;
    private int _flowLastOutcomeShot = -1;
    private int _flowLastPotShot = -1;
    private int _flowLastOpenTableShot = -1;

    private static string FlowWho(int playerId) => MatchFlow.Who(playerId.ToString());

    private int FlowOpponent(int playerId)
    {
        if (!int.TryParse(player1Id, out int p1) || !int.TryParse(player2Id, out int p2)) return -1;
        return playerId == p1 ? p2 : p1;
    }

    public Win8Ball winPanel;
    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    private void Start()
    {
        winPanel = FindObjectOfType<Win8Ball>();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        // Unconditional build identity. An Edgegap deployment is ephemeral — when it is deleted its logs go
        // with it — so every incident report must be tied to a build the moment the match starts, not
        // reconstructed afterwards from commit dates (Unity builds the WORKING TREE, so commit dates prove
        // nothing). A conditional diagnostic cannot do this job: its absence only means the branch never ran.
        Debug.Log($"[8Ball][BUILD] server build stamp={BuildStamp.Timestamp}, version={Application.version}, guid={Application.buildGUID}.");
        PlayerPrefs.SetInt("EightballMultiplayer", 1);
        BallPoolGameLogic.isOnLine = true;
        GameModeManager.isAI = false;
        Debug.Log("BallPoolGameisOffline" + BallPoolGameLogic.isOnLine);
        Debug.Log("BallPoolGameisOnLine" + GameModeManager.isAI);
        Debug.Log("[Client] Joined multiplayer game - EightballMultiplayer set" + PlayerPrefs.GetInt("EightballMultiplayer"));
        if (PlayerPrefs.GetInt("EightballMultiplayer") == 1)
        {

            StartCoroutine(InitializeOnlineGameTurn());
        }
        poolMessenger = FindObjectOfType<AightBallPoolNetworkMessenger>();
        StartCoroutine(ServerMonitorPlayerConnectionsForTimerReward());

    }

    [Server]
    private System.Collections.IEnumerator ServerMonitorPlayerConnectionsForTimerReward()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(0.1f);

            if (!int.TryParse(player1Id, out int p1Id) || !int.TryParse(player2Id, out int p2Id)
                || p1Id <= 0 || p2Id <= 0)
                continue;

            bool p1Connected = IsPlayerConnected(p1Id);
            bool p2Connected = IsPlayerConnected(p2Id);
            if (!timerConnectionMonitorReady)
            {
                if (!p1Connected || !p2Connected)
                    continue;
                player1WasConnectedForTimer = true;
                player2WasConnectedForTimer = true;
                timerConnectionMonitorReady = true;
                continue;
            }

            if (MatchFlow.Enabled && !isGameOver)
            {
                if (player1WasConnectedForTimer && !p1Connected) MatchFlow.Log("8 Ball", $"{FlowWho(p1Id)} disconnected");
                if (!player1WasConnectedForTimer && p1Connected) MatchFlow.Log("8 Ball", $"{FlowWho(p1Id)} reconnected");
                if (player2WasConnectedForTimer && !p2Connected) MatchFlow.Log("8 Ball", $"{FlowWho(p2Id)} disconnected");
                if (!player2WasConnectedForTimer && p2Connected) MatchFlow.Log("8 Ball", $"{FlowWho(p2Id)} reconnected");
            }

            if (player1WasConnectedForTimer && !p1Connected)
                ServerRegisterPlayerDisconnectedForTurnTimer(p1Id);
            if (player2WasConnectedForTimer && !p2Connected)
                ServerRegisterPlayerDisconnectedForTurnTimer(p2Id);

            player1WasConnectedForTimer = p1Connected;
            player2WasConnectedForTimer = p2Connected;
        }
    }

    public System.Collections.IEnumerator InitializeOnlineGameTurn()
    {

        while (NetworkGameManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }


        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (NetworkGameManager.Instance.creatorData != null &&
                !string.IsNullOrEmpty(NetworkGameManager.Instance.creatorData.playerId) &&
                NetworkGameManager.Instance.joinerData != null &&
                !string.IsNullOrEmpty(NetworkGameManager.Instance.joinerData.playerId))
            {

                player1Id = NetworkGameManager.Instance.creatorData.playerId;
                player2Id = NetworkGameManager.Instance.joinerData.playerId;

                Debug.Log($"[Server] Player1 (Creator): {player1Id}, Player2 (Joiner): {player2Id}");


                int randomTurn = Random.Range(0, 2);

                if (randomTurn == 0)
                {

                    if (int.TryParse(player1Id, out int p1Id))
                    {
                        currentTurnId = p1Id;
                        Debug.Log($"[Server] Turn set to Player1 (Creator): {currentTurnId}");
                    }
                }
                else
                {

                    if (int.TryParse(player2Id, out int p2Id))
                    {
                        currentTurnId = p2Id;
                        Debug.Log($"[Server] Turn set to Player2 (Joiner): {currentTurnId}");
                    }
                }


                MatchFlow.Begin("8 Ball", $"game started — {MatchFlow.Who(player1Id)} (creator) vs {MatchFlow.Who(player2Id)} (joiner)");
                MatchFlow.Log("8 Ball", $"toss: {FlowWho(currentTurnId)} breaks");
                RpcNotifyTurnSet(currentTurnId);
                ServerSetControlState(EightBallShotPhase.Aiming, true);

                yield break;
            }

            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        Debug.LogWarning("[Server] Timeout waiting for both players!");
    }





    private int turnApplySeq = 0;

    [ClientRpc]
    private void RpcNotifyTurnSet(int turnPlayerId)
    {
        Debug.Log($"[Client] Server decided turn for Player: {turnPlayerId}");
        IsTossDone = true;

        turnApplySeq++;
        StartCoroutine(WaitAndApplyTurn(turnPlayerId, turnApplySeq));
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        PlayerPrefs.SetInt("EightballMultiplayer", 1);
        BallPoolGameLogic.isOnLine = true;
        GameModeManager.isAI = false;
        Debug.Log("BallPoolGameisOffline" + BallPoolGameLogic.isOnLine);
        Debug.Log("BallPoolGameisOnLine" + GameModeManager.isAI);
        Debug.Log("[Client] Joined multiplayer game - EightballMultiplayer set" + PlayerPrefs.GetInt("EightballMultiplayer"));






        if (!isGameOver)
        {
            ResultManager.isGameFinished = false;
            ResultManager.GameSpawnedFinished = false;
        }

        if (PlayerPrefs.GetInt("EightballMultiplayer") == 1 && currentTurnId != -1)
        {
            turnApplySeq++;
            StartCoroutine(WaitAndApplyTurn(currentTurnId, turnApplySeq));
        }
        poolMessenger = FindObjectOfType<AightBallPoolNetworkMessenger>();




        if (isGameOver)
        {
            // Reconnected into a finished match. Mirror applies the spawn payload before OnStartClient
            // runs, so isGameOver / gameWinnerId / resultPanelDue are already correct here. The one-shot
            // Rpcs sent while this client was away are gone for good — the state is not, which is the
            // whole point of carrying the reveal on a SyncVar. Delayed a little so the scene's
            // GameManager / PopupMessageManager have finished waking up.
            this.Delay(1.5f, () =>
            {
                ShowLocalWinLoseText(gameWinnerId);
                if (resultPanelDue)
                    OpenResultPanel(gameWinnerId, "reconnect into finished match");
                // Reveal not due yet: OnResultPanelDueChanged fires when the server flips it.
            });
        }
    }

    private System.Collections.IEnumerator WaitAndApplyTurn(int playerId, int seq)
    {

        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {

            if (seq != turnApplySeq) yield break;
            if (BallPoolPlayer.initialized && BallPoolPlayer.players != null &&
                BallPoolPlayer.players.Length >= 2 &&
                BallPoolPlayer.players[0] != null && BallPoolPlayer.players[1] != null)
            {


                ApplyTurn(currentTurnId != -1 ? currentTurnId : playerId);
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        Debug.LogWarning("[Client] Timeout waiting for BallPoolPlayer initialization!");
    }





    [Server]
    public void ServerSetControlState(EightBallShotPhase phase, bool allowShoot)
    {
        shotPhase = (int)phase;
        canShoot = allowShoot;
        Debug.Log($"[8Ball][ControlState] phase={phase}, canShoot={allowShoot}, turn={currentTurnId}");
    }

    public EightBallShotPhase ShotPhase => (EightBallShotPhase)shotPhase;
    public bool CanShoot => canShoot;
    // Authoritative turn holder (SyncVar, resyncs on the reconnect respawn). UI that keys off the
    // scene-surviving BallPoolPlayer.myTurn flag uses this to reject a STALE local turn: myTurn lives
    // in the static players array, so a turn that flipped while this device was disconnected leaves
    // myTurn=true until ApplyTurn runs — long enough for turn-gated UI (power slider) to show on
    // BOTH devices at once after a reconnect.
    public int CurrentTurnId => currentTurnId;




    public bool LocalPlayerCanShoot()
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1) return true;
        if (staticVariables.UserProfiledata == null || staticVariables.UserProfiledata.user == null) return false;
        int localId = staticVariables.UserProfiledata.user._id;
        return canShoot
            && !postShotControlLocked
            && currentTurnId == localId
            && (shotPhase == (int)EightBallShotPhase.Aiming || shotPhase == (int)EightBallShotPhase.BallInHand);
    }

    void OnShotPhaseChanged(int oldPhase, int newPhase)
    {
        Debug.Log($"[8Ball][ControlState] client phase {(EightBallShotPhase)oldPhase} -> {(EightBallShotPhase)newPhase} (canShoot={canShoot}, turn={currentTurnId}).");
    }

    void OnCanShootChanged(bool oldValue, bool newValue)
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1) return;
        if (newValue && !postShotControlLocked && currentTurnId != -1)
            RefreshLocalTurnControl(currentTurnId, "server opened shooting (canShoot)");
    }

    // Ball-in-hand drag state changed on the server -> drive the watcher's cue-rig visibility from it.
    // ApplyNetworkBallDragging is a no-op off the watcher (it checks controlFromNetwork), so the
    // shooter's own rig (driven by local input) and the dedicated server are unaffected.
    void OnBallDraggingChanged(bool oldValue, bool newValue)
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1) return;
        if (poolMessenger == null || poolMessenger.shotController == null) return;
        poolMessenger.shotController.ApplyNetworkBallDragging(newValue);
    }

    void OnTurnChanged(int oldTurn, int newTurn)
    {



        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1 || newTurn == -1) return;
        if (oldTurn != -1)
        {




            postShotControlLocked = true;
        }
        turnApplySeq++;
        if (BallPoolPlayer.initialized && BallPoolPlayer.players != null &&
            BallPoolPlayer.players.Length >= 2 &&
            BallPoolPlayer.players[0] != null && BallPoolPlayer.players[1] != null)
        {
            ApplyTurn(newTurn);
        }
        else
        {
            StartCoroutine(WaitAndApplyTurn(newTurn, turnApplySeq));
        }
    }

    /// <summary>
    /// R3 — restores myTurn after the player objects are rebuilt. BallPoolPlayer.players is a STATIC array
    /// that survives the scene reload, and BallPoolPlayer.initialized is just `players != null`, which is
    /// unfalsifiable. So after a reconnect the STALE player objects pass every readiness check, the
    /// currentTurnId SyncVar hook fires immediately, and SetTurn writes myTurn onto objects that
    /// AightBallPoolNetworkGameAdapter / AightBallPoolGameManager are about to REPLACE. myTurn is
    /// { get; private set; }, so the fresh instances default to false and nothing re-runs SetTurn — leaving
    /// myTurn FALSE ON BOTH CLIENTS. Everything keyed off it then misreads: BallPoolGameLogic.controlInNetwork
    /// is false for everyone, so the turn player gets no cue (tester issue 4), and
    /// CircularTimeController.OnUpdateTimer gates the power slider on mainPlayer.myTurn (issue 6). It is a
    /// race, which is why it needs several reconnects to show ("3-3 dafa").
    ///
    /// Called once the rebuild has definitely finished, so this writes the authoritative turn to the LIVE
    /// objects. Deliberately NOT fixed by nulling players[] in Deactivate(): mainPlayer feeds
    /// BallPoolGameLogic.controlFromNetwork, which dereferences mainPlayer.myTurn UNGUARDED, so a null entry
    /// would NRE across the whole module between Deactivate and the rebuild.
    /// </summary>
    public void ReapplyTurnAfterPlayersRebuilt()
    {
        StartCoroutine(ReapplyTurnWhenKnown());
    }

    /// <summary>
    /// The first cut of this called ApplyTurn directly and bailed on currentTurnId == -1. Tester logs from
    /// 2026-07-17 (27 reconnects) show it NEVER fired: the players finish rebuilding before the currentTurnId
    /// SyncVar has landed, so the early-return swallowed every call and issues 4/6 survived. Wait for the turn
    /// to be known instead, then apply it to the (by now definitely rebuilt) live player objects. Bounded so a
    /// match that never gets a turn cannot leave this spinning.
    /// </summary>
    System.Collections.IEnumerator ReapplyTurnWhenKnown()
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline && currentTurnId == -1)
            yield return null;

        if (currentTurnId == -1)
        {
            Debug.LogWarning("[8Ball][TurnControl] Re-apply skipped — currentTurnId still -1 after 10s.");
            yield break;
        }

        Debug.Log($"[8Ball][TurnControl] Re-applying turn {currentTurnId} after the players rebuild (myTurn does not survive the static players[] being replaced).");
        ApplyTurn(currentTurnId);
    }

    void ApplyTurn(int playerId)
    {

        if (BallPoolPlayer.players == null || BallPoolPlayer.players.Length < 2)
        {
            Debug.LogError("[Client] Cannot apply turn - BallPoolPlayer not initialized!");
            return;
        }

        BallPoolPlayer.SetTurn(playerId);
        RefreshLocalTurnControl(playerId, "ApplyTurn");
        string turnPlayerName = GetPlayerName(playerId);
        ShowTurnNotification(playerId, turnPlayerName);
    }

    private void RefreshLocalTurnControl(int turnPlayerId, string reason)
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1)
            return;
        if (turnPlayerId <= 0)
            return;
        if (!NetworkClient.active)
            return;
        if (staticVariables.UserProfiledata == null || staticVariables.UserProfiledata.user == null)
            return;

        BallPoolGameLogic.playMode = BallPool.PlayMode.OnLine;
        BallPoolGameLogic.isOnLine = true;
        GameModeManager.isAI = false;

        ShotController shot = poolMessenger != null && poolMessenger.shotController != null
            ? poolMessenger.shotController
            : ShotController.shotControllerInstance != null
                ? ShotController.shotControllerInstance
                : FindObjectOfType<ShotController>();

        if (shot == null)
        {
            Debug.LogWarning($"[8Ball][TurnControl] Could not refresh local control after {reason}; ShotController is not ready.");
            return;
        }

        if (postShotControlLocked)
        {



            shot.SuppressCueVisualsWhileShotSettling();
            StartCoroutine(SettleThenSendPostShotReady(reason));
            return;
        }

        bool isMyTurn = turnPlayerId == staticVariables.UserProfiledata.user._id;
        bool canControl = LocalPlayerCanShoot() || BallPoolGameLogic.playMode == BallPool.PlayMode.HotSeat;
        bool physicsMoving = shot.cuePhysicsManager != null && shot.cuePhysicsManager.IsInMove;
        if (!shot.isMoving && !physicsMoving)
        {
            shot.ActivateControl(canControl);
            Debug.Log($"[8Ball][TurnControl] {reason}: localUser={staticVariables.UserProfiledata.user._id}, turn={turnPlayerId}, isMyTurn={isMyTurn}, canControl={canControl}, canShoot={canShoot}, phase={(EightBallShotPhase)shotPhase}, controlInNetwork={BallPoolGameLogic.controlInNetwork}, controlFromNetwork={BallPoolGameLogic.controlFromNetwork}.");
        }
        else
        {
            shot.SuppressCueVisualsWhileShotSettling();
            Debug.Log($"[8Ball][TurnControl] Deferred control refresh after {reason}; shotMoving={shot.isMoving}, physicsMoving={physicsMoving}.");
        }

        StartCoroutine(RefreshLocalTurnControlDelayed(turnPlayerId, reason));
    }

    private System.Collections.IEnumerator RefreshLocalTurnControlDelayed(int turnPlayerId, string reason)
    {
        float timeout = 6f;
        while (timeout > 0f)
        {
            yield return null;

            if (currentTurnId != -1 && currentTurnId != turnPlayerId)
                yield break;

            ShotController shot = poolMessenger != null && poolMessenger.shotController != null
                ? poolMessenger.shotController
                : ShotController.shotControllerInstance != null
                    ? ShotController.shotControllerInstance
                    : FindObjectOfType<ShotController>();

            if (shot == null)
                yield break;

            bool physicsMoving = shot.cuePhysicsManager != null && shot.cuePhysicsManager.IsInMove;
            if (shot.isMoving || physicsMoving)
            {
                timeout -= Time.deltaTime;
                continue;
            }

            bool isMyTurn = turnPlayerId == staticVariables.UserProfiledata.user._id;
            bool canControl = LocalPlayerCanShoot() || BallPoolGameLogic.playMode == BallPool.PlayMode.HotSeat;
            shot.ActivateControl(canControl);
            Debug.Log($"[8Ball][TurnControl] Delayed {reason}: localUser={staticVariables.UserProfiledata.user._id}, turn={turnPlayerId}, isMyTurn={isMyTurn}, canControl={canControl}, canShoot={canShoot}, phase={(EightBallShotPhase)shotPhase}.");
            yield break;
        }

        Debug.LogWarning($"[8Ball][TurnControl] Deferred control refresh timed out after {reason}; turn={turnPlayerId}.");
    }



    private bool postShotControlLocked = false;
    private int expectedBarrierToken = -1;
    private int lastAckedBarrierToken = -1;


    private const float PostShotBarrierTimeout = 2.0f;
    private const float ScratchStopWatchdogTimeout = 12.0f;
    private const float MaxShotSeconds = 15.0f;
    private int postShotBarrierToken = 0;
    private int activeBarrierToken = -1;
    private int activeBarrierTurnId = -1;
    private readonly System.Collections.Generic.HashSet<int> barrierReadyConnIds
        = new System.Collections.Generic.HashSet<int>();

    private ShotController ResolveShotController()
    {
        if (poolMessenger != null && poolMessenger.shotController != null)
            return poolMessenger.shotController;
        if (ShotController.shotControllerInstance != null)
            return ShotController.shotControllerInstance;
        return FindObjectOfType<ShotController>();
    }



    private System.Collections.IEnumerator SettleThenSendPostShotReady(string reason)
    {
        float timeout = 8f;
        while (timeout > 0f)
        {
            if (!postShotControlLocked)
                yield break;

            ShotController shot = ResolveShotController();
            if (shot == null)
                yield break;

            bool moving = shot.isMoving || (shot.cuePhysicsManager != null && shot.cuePhysicsManager.IsInMove);
            bool haveToken = expectedBarrierToken >= 0;
            if (!moving && haveToken)
            {
                if (lastAckedBarrierToken != expectedBarrierToken)
                {
                    lastAckedBarrierToken = expectedBarrierToken;
                    Debug.Log($"[8Ball][TurnControl] Post-shot ready ({reason}); token={expectedBarrierToken}.");
                    CmdPostShotReady(expectedBarrierToken);
                }
                yield break;
            }

            timeout -= Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"[8Ball][TurnControl] Post-shot ready timed out locally ({reason}); awaiting server barrier timeout.");
    }

    [Command(requiresAuthority = false)]
    public void CmdPostShotReady(int token, NetworkConnectionToClient sender = null)
    {
        if (token != activeBarrierToken)
            return;

        if (sender != null)
            barrierReadyConnIds.Add(sender.connectionId);

        if (barrierReadyConnIds.Count >= CountActivePlayerConnections())
            CloseTurnControlBarrier("both clients presented shot");
    }

    [Server]
    private void OpenTurnControlBarrier(int turnPlayerId)
    {
        postShotBarrierToken++;
        activeBarrierToken = postShotBarrierToken;
        activeBarrierTurnId = turnPlayerId;
        barrierReadyConnIds.Clear();
        StartCoroutine(BarrierTimeoutRoutine(activeBarrierToken));
    }

    [Server]
    private void CloseTurnControlBarrier(string reason)
    {
        if (activeBarrierToken == -1)
            return;
        int token = activeBarrierToken;
        int turnId = activeBarrierTurnId;
        activeBarrierToken = -1;
        barrierReadyConnIds.Clear();
        Debug.Log($"[8Ball][TurnControl] Opening turn control on all clients ({reason}); token={token}, turn={turnId}.");
        RpcOpenTurnControl(turnId, token);
    }

    private System.Collections.IEnumerator BarrierTimeoutRoutine(int token)
    {
        yield return new WaitForSeconds(PostShotBarrierTimeout);
        if (activeBarrierToken == token)
            CloseTurnControlBarrier("barrier timeout");
    }

    [Server]
    private int CountActivePlayerConnections()
    {
        int count = 0;
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || !conn.isReady || conn.identity == null)
                continue;
            var player = conn.identity.GetComponent<MirrorPlayerPrefab>();
            if (player != null && player.playerData != null && !string.IsNullOrEmpty(player.playerData.playerId))
                count++;
        }
        return count < 1 ? 1 : count;
    }



    [ClientRpc]
    private void RpcOpenTurnControl(int turnPlayerId, int barrierToken)
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1 || turnPlayerId == -1)
            return;
        if (!postShotControlLocked)
            return;
        if (expectedBarrierToken >= 0 && barrierToken != expectedBarrierToken)
            return;
        postShotControlLocked = false;
        expectedBarrierToken = -1;
        lastAckedBarrierToken = -1;
        RefreshLocalTurnControl(turnPlayerId, "post-shot barrier opened");
    }





    [ClientRpc]
    private void RpcEngagePostShotBarrier(int turnPlayerId, int barrierToken)
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1 || turnPlayerId == -1)
            return;
        if (!NetworkClient.active)
            return;
        postShotControlLocked = true;
        expectedBarrierToken = barrierToken;
        lastAckedBarrierToken = -1;
        ShotController shot = ResolveShotController();
        if (shot != null)
        {
            bool isMyTurn = staticVariables.UserProfiledata != null && staticVariables.UserProfiledata.user != null
                && turnPlayerId == staticVariables.UserProfiledata.user._id;
            if (isMyTurn)
                shot.ActivateControl(false);
            shot.SuppressCueVisualsWhileShotSettling();
        }
        StartCoroutine(SettleThenSendPostShotReady("same-turn post-shot"));
    }

    private string GetPlayerName(int playerId)
    {
        if (BallPoolPlayer.players != null)
        {
            foreach (BallPoolPlayer player in BallPoolPlayer.players)
            {
                if (player != null && player.playerId == playerId && !string.IsNullOrWhiteSpace(player.name))
                    return player.name.Trim();
            }
        }

        string playerIdStr = playerId.ToString();
        NetworkGameManager networkGame = NetworkGameManager.Instance;

        if (networkGame?.creatorData != null && networkGame.creatorData.playerId == playerIdStr
            && !string.IsNullOrWhiteSpace(networkGame.creatorData.playerName))
        {
            return networkGame.creatorData.playerName.Trim();
        }
        if (networkGame?.joinerData != null && networkGame.joinerData.playerId == playerIdStr
            && !string.IsNullOrWhiteSpace(networkGame.joinerData.playerName))
        {
            return networkGame.joinerData.playerName.Trim();
        }

        if (staticVariables.OpponetProfile != null
            && staticVariables.OpponetProfile.userId == playerIdStr
            && !string.IsNullOrWhiteSpace(staticVariables.OpponetProfile.userName))
        {
            return staticVariables.OpponetProfile.userName.Trim();
        }

        // Never expose a numeric account ID as a display name.
        return string.Empty;
    }

    private Coroutine pendingTurnNotification;

    private void ShowTurnNotification(int playerId, string playerName)
    {
        bool isMyTurn = staticVariables.UserProfiledata != null
            && staticVariables.UserProfiledata.user != null
            && playerId == staticVariables.UserProfiledata.user._id;

        if (isMyTurn)
        {
            DisplayTurnNotification("Your Turn");
            return;
        }

        if (!string.IsNullOrWhiteSpace(playerName))
        {
            DisplayTurnNotification($"{playerName.Trim()}'s Turn");
            return;
        }

        if (pendingTurnNotification != null)
            StopCoroutine(pendingTurnNotification);
        pendingTurnNotification = StartCoroutine(ShowTurnNotificationWhenNameReady(playerId));
    }

    private System.Collections.IEnumerator ShowTurnNotificationWhenNameReady(int playerId)
    {
        float timeout = 5f;
        while (timeout > 0f && currentTurnId == playerId)
        {
            string playerName = GetPlayerName(playerId);
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                pendingTurnNotification = null;
                DisplayTurnNotification($"{playerName}'s Turn");
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.1f);
            timeout -= 0.1f;
        }

        pendingTurnNotification = null;
        if (currentTurnId == playerId)
        {
            Debug.LogWarning($"[8Ball][TurnUI] Name for turn player {playerId} did not sync in time; showing generic opponent label without exposing the ID.");
            DisplayTurnNotification("Opponent's Turn");
        }
    }

    private void DisplayTurnNotification(string message)
    {
        Debug.Log($"[UI] {message}");

        if (BallPoolGameManager.instance != null)
        {
            BallPoolGameManager.instance.SetGameInfo(message);
        }
    }


    [Command(requiresAuthority = false)]
    public void CmdChangeTurn(int CurrentPlayerId, int requestReason, NetworkConnectionToClient sender = null)
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1)
            return;

        int senderPlayerId = ResolvePlayerId(sender);
        if (sender != null && senderPlayerId != CurrentPlayerId)
        {
            Debug.LogWarning($"[8Ball][TurnGuard] Ignored spoofed turn request. sender={senderPlayerId}, payload={CurrentPlayerId}.");
            return;
        }

        NetworkTurnChangeReason reason = (NetworkTurnChangeReason)requestReason;
        if (reason == NetworkTurnChangeReason.ShotEnd)
        {
            Debug.Log($"[8Ball][TurnGuard] Ignored client shot-end turn request from {CurrentPlayerId}; server shot-end is authoritative.");
            return;
        }

        if (reason == NetworkTurnChangeReason.Timeout)
        {
            if (activeShotPlayerId != -1 || IsPoolPhysicsMoving())
            {
                Debug.LogWarning($"[8Ball][TurnGuard] Ignored timeout turn request from {CurrentPlayerId}; shot is still active for {activeShotPlayerId}.");
                return;
            }

            MatchFlow.Log("8 Ball", $"turn timer ran out for {FlowWho(CurrentPlayerId)}");
            RequestTurnShiftAck(CurrentPlayerId, "client timeout request", true);
            return;
        }

        Debug.LogWarning($"[8Ball][TurnGuard] Ignored turn request from {CurrentPlayerId}; unknown reason={requestReason}.");
    }

    [Server]
    private bool TryChangeTurnOnServer(int CurrentPlayerId, string reason, bool clearActiveShot = true)
    {

        if (!int.TryParse(player1Id, out int p1Id) || !int.TryParse(player2Id, out int p2Id))
        {
            Debug.LogError("[Server] Player IDs not properly set for turn change");
            return false;
        }







        if (currentTurnId != -1 && CurrentPlayerId != currentTurnId)
        {
            Debug.Log($"[Server] Ignored stale CmdChangeTurn from {CurrentPlayerId} (current turn={currentTurnId})");
            return false;
        }


        int nextPlayerId = (CurrentPlayerId == p1Id) ? p2Id : p1Id;
        currentTurnId = nextPlayerId;
        ClearPendingReconnectTimerReward("turn changed");
        // Safety: never carry a stuck drag flag across a turn (e.g. a player who dropped mid-drag).
        ballIsDragging = false;
        if (clearActiveShot)
            activeShotPlayerId = -1;


        lastTurnPlayTime = 0f;





        hasCueState = false;
        lastCueVisible = true;
        // The turn has moved on — any still-armed turn-shift receipt request is now stale.
        ClearPendingTurnAck();

        Debug.Log($"[Server] Turn changed to: {currentTurnId} ({reason})");
        MatchFlow.Log("8 Ball", $"turn → {FlowWho(nextPlayerId)}");
        // Force the cue rig (stick + aim line) back up on BOTH clients. `ballIsDragging = false` above
        // cannot do it: Mirror fires a SyncVar hook only when the value CHANGES, and on a turn TIMEOUT
        // nobody dragged, so that write is false-over-false and the watcher stayed stickless/lineless
        // until someone dragged the cue ball. A ClientRpc always arrives. Each client re-checks its own
        // state before showing (ShotController.ResetCueRigOnTurnChange).
        RpcResetCueRigOnTurnChange();



        OpenTurnControlBarrier(nextPlayerId);





        if (NetworkClient.active)
            postShotControlLocked = true;
        ApplyTurn(nextPlayerId);


        RpcNotifyTurnChanged(nextPlayerId, activeBarrierToken);
        if (pendingScratchPlayerId == CurrentPlayerId)
            pendingScratchPlayerId = -1;
        return true;
    }

    // ---- Turn-shift ack handshake (multiplayer only) --------------------------------------------
    // The server never flips the turn on its own after a shot ends or a turn timer expires: it
    // first sends a receipt request (TargetRpc) to the player whose turn is ending and flips only
    // when that client answers (CmdAckTurnShift). If that client is disconnected the request stays
    // armed and is re-sent at the end of its reconnect restore (GetBallsmechanicalStateData), so
    // the turn can never advance past a player who never saw their turn end. The token guards
    // against stale/duplicate acks; any successful turn change clears the pending request.
    private int pendingTurnAckPlayerId = -1;
    private int pendingTurnAckToken = 0;
    private string pendingTurnAckReason = string.Empty;
    private bool pendingTurnAckClearActiveShot = true;

    [Server]
    private void ClearPendingTurnAck()
    {
        pendingTurnAckPlayerId = -1;
        pendingTurnAckReason = string.Empty;
        pendingTurnAckClearActiveShot = true;
    }

    [Server]
    private NetworkConnectionToClient FindConnectionForPlayer(int playerId)
    {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null)
                continue;
            var player = conn.identity.GetComponent<MirrorPlayerPrefab>();
            if (player != null && player.playerData != null
                && int.TryParse(player.playerData.playerId, out int id) && id == playerId)
                return conn;
        }
        return null;
    }

    [Server]
    private void RequestTurnShiftAck(int currentPlayerId, string reason, bool clearActiveShot)
    {
        if (isGameOver)
            return;
        if (currentTurnId != -1 && currentPlayerId != currentTurnId)
        {
            Debug.Log($"[8Ball][TurnAck] Not arming turn-shift receipt for stale player {currentPlayerId} (current turn={currentTurnId}).");
            return;
        }

        pendingTurnAckPlayerId = currentPlayerId;
        pendingTurnAckReason = reason;
        pendingTurnAckClearActiveShot = clearActiveShot;
        pendingTurnAckToken++;

        NetworkConnectionToClient conn = FindConnectionForPlayer(currentPlayerId);
        if (conn != null)
        {
            TargetRpcRequestTurnShiftAck(conn, pendingTurnAckToken);
            Debug.Log($"[8Ball][TurnAck] Sent turn-shift receipt request to {currentPlayerId} ({reason}), token={pendingTurnAckToken}.");
        }
        else
        {
            Debug.Log($"[8Ball][TurnAck] Player {currentPlayerId} is disconnected; turn HELD ({reason}). Receipt request will replay on reconnect, token={pendingTurnAckToken}.");
        }
    }

    [TargetRpc]
    private void TargetRpcRequestTurnShiftAck(NetworkConnectionToClient target, int token)
    {
        // Client: answer immediately. This is only the delivery receipt the server needs before
        // it advances the turn — no local state changes here.
        CmdAckTurnShift(token);
    }

    [Command(requiresAuthority = false)]
    private void CmdAckTurnShift(int token, NetworkConnectionToClient sender = null)
    {
        if (isGameOver)
            return;
        if (pendingTurnAckPlayerId == -1 || token != pendingTurnAckToken)
        {
            Debug.Log($"[8Ball][TurnAck] Ignored stale turn-shift ack (token={token}, pending token={pendingTurnAckToken}, pending player={pendingTurnAckPlayerId}).");
            return;
        }
        int senderPlayerId = ResolvePlayerId(sender);
        if (sender != null && senderPlayerId != pendingTurnAckPlayerId)
        {
            Debug.LogWarning($"[8Ball][TurnAck] Ignored turn-shift ack from wrong player {senderPlayerId}; expected {pendingTurnAckPlayerId}.");
            return;
        }

        int playerId = pendingTurnAckPlayerId;
        string reason = pendingTurnAckReason;
        bool clearActiveShot = pendingTurnAckClearActiveShot;
        ClearPendingTurnAck();
        TryChangeTurnOnServer(playerId, reason + " (acked)", clearActiveShot);
    }

    [Server]
    public void OnAuthoritativeShotEnded(bool needToChangeTurn, bool cueBallWasPocketedAtShotEnd)
    {







        if (activeShotPlayerId == -1)
        {
            Debug.LogWarning("[8Ball][TurnGuard] Ignored server shot end with no active shot (duplicate/stale completion).");
            return;
        }

        int shotPlayerId = activeShotPlayerId;
        activeShotPlayerId = -1;
        activeShotImpulse = string.Empty;
        activeShotBallPositions = new Vector3[0];

        BroadcastCueBallRespotIfNeeded(needToChangeTurn, cueBallWasPocketedAtShotEnd, "authoritative shot end");
        AssignBreakPocketedBallTypesIfUnambiguous(shotPlayerId);

        if (needToChangeTurn)
        {
            if (currentTurnId == shotPlayerId)
            {
                RequestTurnShiftAck(shotPlayerId, "server shot end", false);
            }
            else
            {
                Debug.Log($"[8Ball][TurnGuard] Server shot end for {shotPlayerId} did not flip turn; turn already advanced to {currentTurnId}.");
            }
        }
        else
        {



            OpenTurnControlBarrier(currentTurnId);
            RpcEngagePostShotBarrier(currentTurnId, activeBarrierToken);
        }

        var stateAfterShot = AightBallPoolGameLogic.gameState;
        bool ballInHandAfter = stateAfterShot != null && stateAfterShot.cueBallInHand;
        ServerSetControlState(ballInHandAfter ? EightBallShotPhase.BallInHand : EightBallShotPhase.Aiming, true);
    }

    [Server]
    private void BroadcastCueBallRespotIfNeeded(bool needToChangeTurn, bool cueBallWasPocketedAtShotEnd, string reason)
    {
        if (!needToChangeTurn)
            return;
        if (poolMessenger == null || poolMessenger.shotController == null)
            return;
        if (AightBallPoolGameLogic.gameState == null)
            return;

        var state = AightBallPoolGameLogic.gameState;
        if (!state.cueBallInHand)
            return;

        ShotController shotController = poolMessenger.shotController;
        BallPool.Mechanics.Ball cueBall = shotController.mainCueBall;
        if (cueBall == null)
            return;

        Vector3 cuePosition = cueBall.position;

        // This is the same authoritative snapshot used to publish Scratch vs WrongBallHit/etc.
        // Never re-infer a scratch here from mutable pocket references, pocket ids, or a transient
        // server playback position; doing so can make the cue teleport while the outcome says no pot.
        if (cueBallWasPocketedAtShotEnd)
        {
            cuePosition = shotController.RespotCueBallInHand();
            Debug.Log($"[8Ball][Scratch] Broadcast server cue-ball respot after {reason}: {cuePosition}");
        }
        else
        {
            Debug.Log($"[8Ball][BallInHand] Preserved on-table cue position after {reason}: {cuePosition}");
        }

        state.cueBallInPocket = false;

        RpcRespotCueBall(cuePosition, true, false);
    }

    [ClientRpc]
    private void RpcNotifyTurnChanged(int newTurnPlayerId, int barrierToken)
    {
        string playerName = GetPlayerName(newTurnPlayerId);
        Debug.Log($"[Client] Turn changed to: {playerName} (ID: {newTurnPlayerId})");
        if (PlayerPrefs.GetInt("EightballMultiplayer") == 1 && newTurnPlayerId != -1)
        {



            postShotControlLocked = true;
            expectedBarrierToken = barrierToken;
            lastAckedBarrierToken = -1;

            turnApplySeq++;
            if (BallPoolPlayer.initialized && BallPoolPlayer.players != null &&
                BallPoolPlayer.players.Length >= 2 &&
                BallPoolPlayer.players[0] != null && BallPoolPlayer.players[1] != null)
            {
                ApplyTurn(newTurnPlayerId);
            }
            else
            {

                StartCoroutine(WaitAndApplyTurn(newTurnPlayerId, turnApplySeq));
            }
        }
    }


    [Command(requiresAuthority = false)]
    public void CmdSetPlayerBallTypes(bool mainPlayerIsSolids, bool mainPlayerIsStripes,
                                       bool otherPlayerIsSolids, bool otherPlayerIsStripes, NetworkConnectionToClient sender = null)
    {




        {
            int senderType = mainPlayerIsSolids ? 1 : (mainPlayerIsStripes ? 2 : 0);
            int opponentType = otherPlayerIsSolids ? 1 : (otherPlayerIsStripes ? 2 : 0);



            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                var pp = (conn != null && conn.identity != null) ? conn.identity.GetComponent<MirrorPlayerPrefab>() : null;
                if (pp == null) continue;
                string pid = pp.playerData.playerId;
                if (string.IsNullOrEmpty(pid)) continue;
                ballTypeByPlayerId[pid] = (conn == sender) ? senderType : opponentType;
            }
            string dbg = ""; foreach (var kv in ballTypeByPlayerId) dbg += kv.Key + "=" + kv.Value + " ";
            Debug.Log("[8Ball][BallTypePersist] stored by id -> " + dbg);
        }

        if (PlayerPrefs.GetInt("EightballMultiplayer") == 1)
        {
            int mainPlayerBallType = mainPlayerIsSolids ? 1 : (mainPlayerIsStripes ? 2 : 0);
            int otherPlayerBallType = otherPlayerIsSolids ? 1 : (otherPlayerIsStripes ? 2 : 0);
            tableOpened = false;

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn == sender)
                {
                    OnMainPlayerBallTypeChanged(conn, mainPlayerBallType, otherPlayerBallType);
                }
                else
                {
                    OnMainPlayerBallTypeChanged(conn, otherPlayerBallType, mainPlayerBallType);
                }
            }

            if (sender.identity.GetComponent<MirrorPlayerPrefab>() != null)
            {
                MirrorPlayerPrefab mainplayer = sender.identity.GetComponent<MirrorPlayerPrefab>();
                if (mainplayer.playerData.isCreator)
                {
                    AightBallPoolPlayer.mainPlayer.isSolids = (mainPlayerBallType == 1);
                    AightBallPoolPlayer.mainPlayer.isStripes = (mainPlayerBallType == 2);
                    AightBallPoolPlayer.mainPlayer.isBlack = false;
                    AightBallPoolPlayer.otherPlayer.isSolids = (otherPlayerBallType == 1);
                    AightBallPoolPlayer.otherPlayer.isStripes = (otherPlayerBallType == 2);
                    AightBallPoolPlayer.otherPlayer.isBlack = false;
                }
                else
                {
                    AightBallPoolPlayer.otherPlayer.isSolids = (mainPlayerBallType == 1);
                    AightBallPoolPlayer.otherPlayer.isStripes = (mainPlayerBallType == 2);
                    AightBallPoolPlayer.otherPlayer.isBlack = false;
                    AightBallPoolPlayer.mainPlayer.isSolids = (otherPlayerBallType == 1);
                    AightBallPoolPlayer.mainPlayer.isStripes = (otherPlayerBallType == 2);
                    AightBallPoolPlayer.mainPlayer.isBlack = false;
                }
            }
            UpdatePlayerUI();


        }
    }

    [TargetRpc]
    void OnMainPlayerBallTypeChanged(NetworkConnectionToClient target, int mainPlayerBallType, int otherPlayerBallType)
    {
        if (AightBallPoolPlayer.mainPlayer != null)
        {




            if (mainPlayerBallType != 0)
            {
                AightBallPoolPlayer.mainPlayer.isSolids = (mainPlayerBallType == 1);
                AightBallPoolPlayer.mainPlayer.isStripes = (mainPlayerBallType == 2);
                AightBallPoolPlayer.mainPlayer.isBlack = false;
            }
            if (otherPlayerBallType != 0)
            {
                AightBallPoolPlayer.otherPlayer.isSolids = (otherPlayerBallType == 1);
                AightBallPoolPlayer.otherPlayer.isStripes = (otherPlayerBallType == 2);
                AightBallPoolPlayer.otherPlayer.isBlack = false;
            }
        }

        if (AightBallPoolGameLogic.gameState != null)
        {
            AightBallPoolGameLogic.gameState.playersHasBallType = true;
            AightBallPoolGameLogic.gameState.tableIsOpened = false;
        }

        UpdatePlayerUI();

        TryShowLocalBallTypeAssignedText();
    }
    [Command(requiresAuthority = false)]
    public void GetBallsmechanicalStateData(bool showLoadingOverlay, NetworkConnectionToClient sender = null)
    {
        if (sender == null || poolMessenger == null || poolMessenger.shotController == null || poolMessenger.gameManager == null)
        {
            // Always tell the client (it arms the retry; the scene blur stays up until a
            // successful sync completes, so the request MUST eventually be answered).
            if (sender != null)
                TargetRpcCancelReconnectStateSync(sender);

            Debug.LogWarning("[8Ball][Reconnect] Snapshot request skipped; sender or pool objects are not ready.");
            return;
        }

        TryApplyReconnectTurnTimerReward(sender);

        bool showReconnectLoadingOverlay = ShouldShowReconnectStateLoading(showLoadingOverlay, sender);

        if (shotPhase == (int)EightBallShotPhase.Simulating && activeShotPlayerId != -1)
        {
            float shotAge = Time.realtimeSinceStartup - activeShotStartedAtRealtime;
            float staleGrace = MaxShotSeconds + 2.0f;
            float physicsShotDuration = (poolMessenger != null && poolMessenger.shotController != null
                && poolMessenger.shotController.cuePhysicsManager != null)
                ? poolMessenger.shotController.cuePhysicsManager.shotDuration : 0f;
            // FIX (mid-shot freeze): do NOT treat "!IsPoolPhysicsMoving()" alone as stuck.
            // IsInMove is legitimately false during the shot-start window (before InitiateShot)
            // and can flicker; a reconnect snapshot landing then used to force-complete the shot
            // and freeze BOTH clients at a mid-shot position. Only consider physics stalled once
            // the shot is well past its start window (age grace); otherwise DEFER to the server's
            // own shot-end / watchdog. A disconnected shooter or a past-max-time shot still count.
            bool physicsLikelyStalled = !IsPoolPhysicsMoving() && shotAge > 4.0f;
            bool stuck = !IsPlayerConnected(activeShotPlayerId)
                || physicsLikelyStalled
                || shotAge > staleGrace
                || physicsShotDuration > staleGrace;
            if (stuck)
            {


                ServerForceCompleteStuckShot($"reconnect while still Simulating (age={shotAge:F1}s, dur={physicsShotDuration:F1}s)");
            }
            else
            {
                TargetRpcCancelReconnectStateSync(sender);

                Debug.Log("[8Ball][Reconnect] Legit live shot still resolving â€” deferring reconnect snapshot to the server's own shot-end broadcast.");
                return;
            }
        }



        ServerFreezeAuthoritativeBoard("reconnect snapshot");
        if (showReconnectLoadingOverlay)
            TargetRpcBeginReconnectStateSync(sender);









        bool cueBelowTableBeforeRestore = poolMessenger.shotController.mainCueBall.position.y < -0.1f;
        if (cueBelowTableBeforeRestore)
        {
            Vector3 cueRespot = poolMessenger.shotController.RespotCueBallInHand();
            if (AightBallPoolGameLogic.gameState != null)
            {
                AightBallPoolGameLogic.gameState.cueBallInHand = true;
                AightBallPoolGameLogic.gameState.needToChangeTurn = true;



                AightBallPoolGameLogic.gameState.cueBallInPocket = false;
            }
            RpcRespotCueBall(cueRespot, true, false);
            // Do NOT re-arm pendingScratchPlayerId from currentTurnId here. This block has just FORCED
            // cueBallInHand + needToChangeTurn true, and the recovery check below tests exactly those two
            // plus `currentTurnId == pendingScratchPlayerId` — so re-arming the sentinel to whoever happens
            // to hold the turn made every condition trivially true and flipped that player's turn away.
            // Real case: A scratches -> B is awarded ball-in-hand -> the cue ball is still below the table
            // (its respot can be late) -> B RECONNECTS -> sentinel re-armed to B -> B == B -> B loses the
            // turn to A, having never scratched (tester issue 9, "is ki turn kyun hui").
            // pendingScratchPlayerId is set by ServerRegisterCueBallPocketed and identifies WHO actually
            // scratched; -1 means we do not know, and guessing is what caused the bug. Leaving it -1 simply
            // skips the recovery, which is correct: the recovery only exists to advance a turn the scratcher
            // still holds, and the normal shot-end path covers that anyway.
            Debug.Log("[8Ball][Reconnect] Cue ball was below table on reconnect â€” server respotted to " + cueRespot + " and broadcast to both clients.");
        }

        if (AightBallPoolGameLogic.gameState != null
            && AightBallPoolGameLogic.gameState.cueBallInHand
            && AightBallPoolGameLogic.gameState.needToChangeTurn
            && pendingScratchPlayerId != -1
            && currentTurnId == pendingScratchPlayerId)
        {
            TryChangeTurnOnServer(pendingScratchPlayerId, "scratch reconnect recovery", clearActiveShot: false);
        }

        TargetRpcSetBallPosition(sender, poolMessenger.shotController.mainCueBall.position);









        // Replay the cached cue-rig visibility LAST (TargetRpcs to one connection are ordered), so it
        // wins over the show that ForceCueControl performs. Without this the rejoining client only ever
        // guesses "visible": the hide relays (CmdSwitchCueState(false) during the opponent's ball-in-hand
        // drag) are one-shots it missed during the scene reload, and SyncWithNetwork force-shows the rig
        // every frame after that — the "stick on both sides / stick standing while the white ball moves"
        // reconnect report. (Visibility replay is queued after the pose branch below.)
        if (hasCueState)








            TargetRpcForceCueControl(sender, lastCuePivotY, lastCueVerticalX, lastCueDisplacement, 0f, 0f);
        else
            TargetRpcResetCueOnReconnect(sender);
        TargetRpcRestoreCueVisibility(sender, lastCueVisible);
        foreach (BallPool.Mechanics.Ball ball in poolMessenger.gameManager.allBalls)
        {




            Vector3 restorePos = ball.position;
            if (ball.id != 0 && ball.inPocket)
                restorePos.y = -1f;
            TargetRpcSetBallPos(sender, ball.id, restorePos, ball.listener.body.linearVelocity, ball.listener.body.angularVelocity, currentTurnId);
        }







        {
            var allBallsForReconcile = poolMessenger.gameManager.allBalls;
            Vector3[] reconcilePositions = new Vector3[allBallsForReconcile.Length];
            for (int i = 0; i < reconcilePositions.Length; i++)
            {
                reconcilePositions[i] = allBallsForReconcile[i].position;

                if (i != 0 && allBallsForReconcile[i].inPocket)
                    reconcilePositions[i].y = -1f;
            }
            // FIX (mid-shot freeze): send the reconnect reconcile ONLY to the reconnecting client
            // (sender), NOT to every client. As a broadcast it used to force-end a healthy live
            // shot on the OTHER (non-reconnecting) player and snap them to a frozen board.
            TargetRpcSyncBallsAfterReconnect(sender, reconcilePositions, currentTurnId);
        }

        if (sender.identity.GetComponent<MirrorPlayerPrefab>() != null && !tableOpened)
        {
            MirrorPlayerPrefab reconnectingPlayer = sender.identity.GetComponent<MirrorPlayerPrefab>();


            int mainPlayerBallType = 0;
            int otherPlayerBallType = 0;




            string reconnectId = reconnectingPlayer.playerData.playerId;
            mainPlayerBallType = (!string.IsNullOrEmpty(reconnectId) && ballTypeByPlayerId.ContainsKey(reconnectId))
                                 ? ballTypeByPlayerId[reconnectId] : 0;
            otherPlayerBallType = 0;
            foreach (var kv in ballTypeByPlayerId)
                if (kv.Key != reconnectId) otherPlayerBallType = kv.Value;
            Debug.Log("[8Ball][BallTypeRestore] reconnectId=" + reconnectId + " mainType=" + mainPlayerBallType + " otherType=" + otherPlayerBallType);




            if (mainPlayerBallType != 0 || otherPlayerBallType != 0)
                OnMainPlayerBallTypeChanged(sender, mainPlayerBallType, otherPlayerBallType);
        }




        if (breakShotDone && tableOpened)
        {
            Debug.Log("[8Ball][GetBallsmechanicalStateData] Break shot done, tableOpened still true â€” restoring tableIsOpened=false on reconnecting client.");
            TargetRpcRestoreTableState(sender);
        }








        if (AightBallPoolGameLogic.gameState != null)
        {
            var gs = AightBallPoolGameLogic.gameState;





            TargetRpcRestoreGameFlags(
                sender,
                gs.cueBallInHand || cueBelowTableBeforeRestore,
                gs.needToChangeTurn,
                gs.cueBallInPocket || cueBelowTableBeforeRestore,
                gs.cueBallHasHitRightBall,
                gs.cueBallHasHitSomeBall,
                gs.hasRightBallInPocket,
                gs.ballsHitBoardCount);
        }





        TargetRpcRestorePlayTime(sender, lastTurnPlayTime);








        TargetRpcRefreshCueAfterReconnect(
            sender,
            hasCueState ? lastCuePivotY : 0f,
            hasCueState ? lastCueVerticalX : 0f,
            hasCueState ? lastCueDisplacement : Vector2.zero,
            0f,
            0f);






        TargetRpcRefreshActiveBalls(sender);

        // Turn-shift ack replay: if this player's turn ended (shot end / timeout) while they were
        // disconnected, the receipt request could not be delivered and the turn was deliberately
        // HELD. Re-send it now that they are back; their ack completes the deferred turn change.
        // (If the scratch-recovery block above already advanced the turn, TryChangeTurnOnServer
        // cleared the pending request and this is skipped.)
        if (pendingTurnAckPlayerId != -1 && ResolvePlayerId(sender) == pendingTurnAckPlayerId)
        {
            Debug.Log($"[8Ball][TurnAck] Replaying pending turn-shift receipt request to reconnected player {pendingTurnAckPlayerId}, token={pendingTurnAckToken}.");
            TargetRpcRequestTurnShiftAck(sender, pendingTurnAckToken);
        }

        RememberReconnectStateSynced(sender);
        // Always sent (was gated on the overlay flag): the client-side scene blur stays up until
        // this lands, on FRESH starts too, so every successful sync must announce completion.
        TargetRpcReconnectStateSyncComplete(sender);
    }

    [Server]
    public void ServerRegisterPlayerDisconnectedForTurnTimer(int disconnectedPlayerId)
    {
        bool turnWaitingForShot = disconnectedPlayerId > 0
            && currentTurnId > 0
            && disconnectedPlayerId != currentTurnId
            && activeShotPlayerId == -1
            && canShoot
            && (shotPhase == (int)EightBallShotPhase.Aiming
                || shotPhase == (int)EightBallShotPhase.BallInHand);

        if (!turnWaitingForShot)
            return;

        pendingTimerRewardReconnectPlayerId = disconnectedPlayerId;
        pendingTimerRewardTurnPlayerId = currentTurnId;
        Debug.Log($"[8Ball][ReconnectTimer] Player {disconnectedPlayerId} disconnected before turn player {currentTurnId} started a shot; timer reward armed.");
    }

    [Server]
    private void TryApplyReconnectTurnTimerReward(NetworkConnectionToClient sender)
    {
        int reconnectingPlayerId = ResolvePlayerId(sender);
        bool rewardStillValid = reconnectingPlayerId > 0
            && reconnectingPlayerId == pendingTimerRewardReconnectPlayerId
            && currentTurnId == pendingTimerRewardTurnPlayerId
            && activeShotPlayerId == -1
            && canShoot
            && (shotPhase == (int)EightBallShotPhase.Aiming
                || shotPhase == (int)EightBallShotPhase.BallInHand);

        if (!rewardStillValid)
            return;

        int rewardedTurnPlayerId = currentTurnId;
        lastTurnPlayTime = 0f;
        if (BallPoolGameManager.instance != null)
            BallPoolGameManager.instance.SetPlayTime(0f);

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.isReady)
                TargetRpcResetTurnTimerAfterOpponentReconnect(conn, rewardedTurnPlayerId, reconnectingPlayerId);
        }

        ClearPendingReconnectTimerReward("reward applied");
        Debug.Log($"[8Ball][ReconnectTimer] Reset turn timer for {rewardedTurnPlayerId} because opponent {reconnectingPlayerId} reconnected before the shot started.");
    }

    [Server]
    private void ClearPendingReconnectTimerReward(string reason)
    {
        if (pendingTimerRewardReconnectPlayerId != -1)
            Debug.Log($"[8Ball][ReconnectTimer] Cleared pending timer reward ({reason}).");
        pendingTimerRewardReconnectPlayerId = -1;
        pendingTimerRewardTurnPlayerId = -1;
    }

    [TargetRpc]
    private void TargetRpcResetTurnTimerAfterOpponentReconnect(NetworkConnectionToClient target,
        int rewardedTurnPlayerId, int reconnectingPlayerId)
    {
        if (BallPoolGameManager.instance != null)
            BallPoolGameManager.instance.SetPlayTime(0f);
        Debug.Log($"[8Ball][ReconnectTimer] Local timer reset for turn player {rewardedTurnPlayerId}; opponent {reconnectingPlayerId} reconnected.");
    }



    private bool ShouldShowReconnectStateLoading(bool requestedByClient, NetworkConnectionToClient sender)
    {
        if (requestedByClient)
            return true;

        string playerId = GetReconnectStatePlayerId(sender);
        return !string.IsNullOrEmpty(playerId) && reconnectStateSyncedPlayerIds.Contains(playerId);
    }

    private void RememberReconnectStateSynced(NetworkConnectionToClient sender)
    {
        string playerId = GetReconnectStatePlayerId(sender);
        if (!string.IsNullOrEmpty(playerId))
            reconnectStateSyncedPlayerIds.Add(playerId);
    }

    private string GetReconnectStatePlayerId(NetworkConnectionToClient sender)
    {
        if (sender == null || sender.identity == null)
            return string.Empty;

        MirrorPlayerPrefab player = sender.identity.GetComponent<MirrorPlayerPrefab>();
        if (player == null)
            return string.Empty;

        if (player.playerData != null && !string.IsNullOrEmpty(player.playerData.playerId))
            return player.playerData.playerId;

        return player.playerId;
    }

    [TargetRpc]
    void TargetRpcBeginReconnectStateSync(NetworkConnectionToClient target)
    {
        reconnectStateSyncCompleteReceived = false;
        reconnectBallsSyncInProgress = false;

        if (reconnectOverlayHideRoutine != null)
        {
            StopCoroutine(reconnectOverlayHideRoutine);
            reconnectOverlayHideRoutine = null;
        }

        // Scene-authored blur already covers the screen on the client; nothing to show.
    }

    [TargetRpc]
    void TargetRpcCancelReconnectStateSync(NetworkConnectionToClient target)
    {
        if (reconnectOverlayHideRoutine != null)
        {
            StopCoroutine(reconnectOverlayHideRoutine);
            reconnectOverlayHideRoutine = null;
        }

        reconnectStateSyncCompleteReceived = true;
        reconnectBallsSyncInProgress = false;
        // Blur stays up on the client until a successful sync completes (retry below).

        // The server DEFERRED this sync — it logged "Legit live shot still resolving" and returned WITHOUT
        // pushing any ball state. It will not come back on its own: GetBallsmechanicalStateData has exactly
        // ONE caller (AightBallPoolGameManager's one-shot WaitForMultiplayerPlayersReady coroutine), so
        // without a retry this client keeps the scene's AUTHORED DEFAULT RACK for the rest of the match —
        // the tester's "reconnection pe frame recreate ho jata hai" (issue 2). Ask again once the shot the
        // server was protecting has resolved.
        if (reconnectStateRetryRoutine != null)
            StopCoroutine(reconnectStateRetryRoutine);
        reconnectStateRetryRoutine = StartCoroutine(RetryReconnectStateSyncAfterDefer());
    }

    /// <summary>
    /// Re-requests the reconnect state sync that the server deferred. The defer reason is always "a live shot
    /// is still resolving", and shotPhase is a SyncVar, so we can simply wait for that shot to leave
    /// Simulating and then ask once more. Bounded twice — a hard wait deadline and an attempt cap — so a
    /// wedged shot can never spin this forever. Deliberately does NOT block input: Layer 1's
    /// ServerBoardAgreesWithShotRequest already rejects (and repairs) a shot fired from a stale board, and
    /// blocking here would remove that self-heal path.
    /// </summary>
    System.Collections.IEnumerator RetryReconnectStateSyncAfterDefer()
    {
        if (reconnectStateRetryAttempts >= ReconnectStateRetryMaxAttempts)
        {
            Debug.LogWarning($"[8Ball][Reconnect] State-sync retry cap reached ({ReconnectStateRetryMaxAttempts}) — giving up; the board may stay stale until the next shot is rejected.");
            reconnectStateRetryRoutine = null;
            yield break;
        }
        reconnectStateRetryAttempts++;

        float deadline = Time.realtimeSinceStartup + ReconnectStateRetryShotWaitTimeout;
        while (Time.realtimeSinceStartup < deadline && ShotPhase == EightBallShotPhase.Simulating)
            yield return null;

        reconnectStateRetryRoutine = null;
        if (NetworkServer.active || isGameOver)
            yield break;

        yield return new WaitForSecondsRealtime(0.25f);   // let the server's own shot-end settle first
        Debug.Log($"[8Ball][Reconnect] Re-requesting state sync after defer (attempt {reconnectStateRetryAttempts}/{ReconnectStateRetryMaxAttempts}, phase={ShotPhase}).");
        GetBallsmechanicalStateData(breakShotDone);   // mid-match => show the loading overlay while balls restore
    }

    [TargetRpc]
    void TargetRpcReconnectStateSyncComplete(NetworkConnectionToClient target)
    {
        reconnectStateSyncCompleteReceived = true;

        if (reconnectOverlayHideRoutine != null)
            StopCoroutine(reconnectOverlayHideRoutine);

        reconnectOverlayHideRoutine = StartCoroutine(HideReconnectStateOverlayWhenReady());
    }

    System.Collections.IEnumerator HideReconnectStateOverlayWhenReady()
    {
        float timeout = 15f;
        while (reconnectBallsSyncInProgress && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        for (int i = 0; i < 4; i++)
            yield return null;

        // Client build drops its scene blur here (Blur2D.DisableSyncBlur); no UI on the server.
        reconnectOverlayHideRoutine = null;
    }

    [TargetRpc]
    void TargetRpcRefreshCueAfterReconnect(NetworkConnectionToClient target,
        float pivotY, float verticalX, Vector2 displacement, float sliderZ, float force)
    {
        if (poolMessenger != null && poolMessenger.shotController != null)
            poolMessenger.shotController.RefreshCueAfterReconnect(pivotY, verticalX, displacement, sliderZ, force);
    }



    [TargetRpc]
    void TargetRpcRefreshActiveBalls(NetworkConnectionToClient target)
    {
        if (poolMessenger != null && poolMessenger.gameManager != null)
            poolMessenger.gameManager.RefreshActiveBallsUI();
    }

    [TargetRpc]
    void TargetRpcRestoreTableState(NetworkConnectionToClient target)
    {
        if (AightBallPoolGameLogic.gameState != null)
        {
            AightBallPoolGameLogic.gameState.tableIsOpened = false;
            Debug.Log("[8Ball][TargetRpcRestoreTableState] tableIsOpened restored to false on reconnect â€” cue ball can move freely.");
        }
    }






    [TargetRpc]
    void TargetRpcRestoreGameFlags(NetworkConnectionToClient target,
        bool cueBallInHand, bool needToChangeTurn, bool cueBallInPocket,
        bool cueBallHasHitRightBall, bool cueBallHasHitSomeBall,
        bool hasRightBallInPocket, int ballsHitBoardCount)
    {
        if (AightBallPoolGameLogic.gameState != null)
        {
            var gs = AightBallPoolGameLogic.gameState;
            gs.cueBallInHand = cueBallInHand;
            gs.needToChangeTurn = needToChangeTurn;
            gs.cueBallInPocket = cueBallInPocket;
            gs.cueBallHasHitRightBall = cueBallHasHitRightBall;
            gs.cueBallHasHitSomeBall = cueBallHasHitSomeBall;
            gs.hasRightBallInPocket = hasRightBallInPocket;
            gs.ballsHitBoardCount = ballsHitBoardCount;

            postShotControlLocked = false;
            RefreshLocalTurnControl(BallPoolPlayer.turnId, "RestoreGameFlags");
            Debug.Log($"[8Ball][TargetRpcRestoreGameFlags] Reconnect restore â€” cueBallInHand={cueBallInHand}, needToChangeTurn={needToChangeTurn}, cueBallInPocket={cueBallInPocket}.");
        }
    }










    [TargetRpc]
    void TargetRpcResetCueOnReconnect(NetworkConnectionToClient target)
    {
        if (poolMessenger?.shotController != null)
        {
            // Restore the AUTHORED rest pose, not identity — see ShotController.ResetCueOnReconnect.
            poolMessenger.shotController.ResetCueOnReconnect();
            Debug.Log("[8Ball][TargetRpcResetCueOnReconnect] Cue stick reset to its authored rest pose on the reconnecting client only.");
        }
    }

    void UpdatePlayerUI()
    {
        if (BallPoolGameManager.instance != null)
        {
            var gameManager = BallPoolGameManager.instance as AightBallPoolGameManager;
            if (gameManager != null)
            {
                gameManager.UpdateActiveBalls();
            }
        }
    }



    [Command(requiresAuthority = false)]
    public void CmdSetBreakShotDone(bool value)
    {
        ServerSetBreakShotDone(value);
    }

    [Server]
    public void ServerSetBreakShotDone(bool value)
    {
        breakShotDone = value;
        Debug.Log($"[8Ball][BreakShot] breakShotDone set to {value} on server.");

        // Break is over -> the table is "closed" for ball-in-hand purposes: the cue ball must be
        // movable across the WHOLE table (clothPosition), not just the break/kitchen zone
        // (initialMoveSpace). On online clients OnEndShot bails out early (server-authoritative),
        // so gameLogic.OnEndShot() -> gameState.tableIsOpened=false NEVER runs client-side. Types
        // may not be assigned yet (weak/foul break with nothing potted), so the type-assignment
        // RPCs won't close it either. Broadcast the close to every client here. NOTE: we do NOT
        // touch the server-side `tableOpened` field so the reconnect restore path
        // (if (breakShotDone && tableOpened) TargetRpcRestoreTableState) stays intact.
        if (value)
            RpcCloseTableAfterBreak();
    }

    // Broadcast to ALL clients: close the table so ball-in-hand can move over the full cloth.
    [ClientRpc]
    void RpcCloseTableAfterBreak()
    {
        if (AightBallPoolGameLogic.gameState != null)
        {
            AightBallPoolGameLogic.gameState.tableIsOpened = false;
            Debug.Log("[8Ball][BreakShot] RpcCloseTableAfterBreak -> tableIsOpened=false on client (cue ball can move over full table).");
        }
    }


    [Command(requiresAuthority = false)]
    public void CmdSetCueBallInHand(bool value, NetworkConnectionToClient sender = null)
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") == 1)
        {
            Debug.LogWarning($"[8Ball][Scratch] Ignored client cue-ball-in-hand report value={value} from {ResolvePlayerId(sender)}; only the server may mutate scratch state.");
        }
    }

    [Server]
    public void ServerRegisterCueBallPocketed()
    {
        int scratchPlayerId = activeShotPlayerId != -1 ? activeShotPlayerId : currentTurnId;
        if (scratchPlayerId <= 0 || currentTurnId != scratchPlayerId)
        {
            Debug.LogWarning($"[8Ball][Scratch] Server ignored cue-ball pocket without a valid active shooter. active={activeShotPlayerId}, turn={currentTurnId}.");
            return;
        }

        pendingScratchPlayerId = scratchPlayerId;
        var state = AightBallPoolGameLogic.gameState;
        if (state != null)
        {
            state.cueBallInHand = true;
            state.cueBallInPocket = true;
            state.needToChangeTurn = true;
        }

        Debug.Log($"[8Ball][Scratch] Server detected cue-ball pocket for shooter {scratchPlayerId}; respot will run before the turn decision.");
        ServerCompleteScratchShotEnd(scratchPlayerId);
    }







    [Server]
    private void ServerCompleteScratchShotEnd(int scratchPlayerId)
    {
        if (poolMessenger == null)
            return;
        if (shotStopConsumed)
            return;
        if (currentTurnId != scratchPlayerId)
            return;
        if (activeShotPlayerId == -1 || activeShotPlayerId != scratchPlayerId)
            return;








        StartCoroutine(ServerScratchStopWatchdog(scratchPlayerId, activeShotSequence));
    }

    private System.Collections.IEnumerator ServerScratchStopWatchdog(int scratchPlayerId, int shotSeq)
    {
        float deadline = Time.realtimeSinceStartup + ScratchStopWatchdogTimeout;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (shotStopConsumed)
                yield break;
            if (activeShotSequence != shotSeq || activeShotPlayerId == -1)
                yield break;
            yield return new WaitForFixedUpdate();
        }

        if (shotStopConsumed || activeShotPlayerId != scratchPlayerId || currentTurnId != scratchPlayerId)
            yield break;



        shotStopConsumed = true;
        activeShotImpulse = string.Empty;
        activeShotBallPositions = new Vector3[0];
        ApplyAuthoritativeShotEndSnapshot(scratchPlayerId);
        float time = (poolMessenger != null && poolMessenger.shotController != null && poolMessenger.shotController.cuePhysicsManager != null)
            ? poolMessenger.shotController.cuePhysicsManager.shotDuration
            : 0f;
        if (poolMessenger != null)
            poolMessenger.WaitAndStopMoveFromNetwork(time);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || !conn.isReady)
                continue;
            TargetRpcWaitAndStopMove(conn, time);
        }
        Debug.LogWarning($"[8Ball][Scratch] Watchdog forced authoritative shot-end for scratch by {scratchPlayerId} after no client stop arrived; time={time}.");
    }

    [Server]
    private System.Collections.IEnumerator ServerAutonomousShotEndWatchdog(int shotSeq)
    {
        // FIX (mid-shot freeze): only the "balls settled early" completion is allowed once the
        // server has actually observed the shot moving at least once. If the server never
        // registered motion (shot-start window / slow InitiateShot), a persistent
        // !IsPoolPhysicsMoving() must NOT force-complete the shot early — that snapped both
        // clients to a frozen mid-shot board. In that case we fall through to the hard deadline.
        bool physicsObservedMoving = false;
        float moveDeadline = Time.realtimeSinceStartup + 2.0f;
        while (Time.realtimeSinceStartup < moveDeadline)
        {
            if (activeShotSequence != shotSeq || activeShotPlayerId == -1 || shotStopConsumed)
                yield break;
            if (IsPoolPhysicsMoving())
            {
                physicsObservedMoving = true;
                break;
            }
            yield return new WaitForFixedUpdate();
        }

        float hardDeadline = Time.realtimeSinceStartup + MaxShotSeconds;
        float settleStart = -1f;
        while (Time.realtimeSinceStartup < hardDeadline)
        {
            if (activeShotSequence != shotSeq || activeShotPlayerId == -1 || shotStopConsumed)
                yield break;

            // Playback can look settled while packets are delayed. A connected shooter owns the
            // simulation, so wait for its reliable final board (bounded by the hard deadline).
            if (PhysicsHandeler.UseNetworkPlayback && IsPlayerConnected(activeShotPlayerId))
            {
                settleStart = -1f;
            }
            else if (!IsPoolPhysicsMoving())
            {
                // Only count a settle if the server truly saw the shot move first.
                if (physicsObservedMoving)
                {
                    if (settleStart < 0f)
                        settleStart = Time.realtimeSinceStartup;
                    else if (Time.realtimeSinceStartup - settleStart >= 1.5f)
                    {
                        ServerForceCompleteStuckShot("server balls settled; shooter stop absent");
                        yield break;
                    }
                }
            }
            else
            {
                physicsObservedMoving = true;
                settleStart = -1f;
            }
            yield return new WaitForFixedUpdate();
        }

        ServerForceCompleteStuckShot("server shot watchdog hard timeout");
    }

    // If the shooter disconnects mid-shot, continuing to relay the last server frame leaves the
    // watcher frozen while the reconnecting shooter may keep running private local physics.
    // Finalise from the last accepted server board instead; both seats then receive the same
    // reliable snapshot and the turn can only move through the normal authoritative shot-end path.
    private System.Collections.IEnumerator ServerStreamToWatcherOnShooterDisconnect(int shotSeq)
    {
        bool reportedFrameGap = false;
        while (true)
        {
            yield return new WaitForFixedUpdate();
            if (activeShotSequence != shotSeq || activeShotPlayerId == -1 || shotStopConsumed)
                yield break;

            bool shooterConnected = IsPlayerConnected(activeShotPlayerId);
            bool shooterFramesStarved = lastShotFrameRealtime > 0f
                && Time.realtimeSinceStartup - lastShotFrameRealtime >= ShooterFrameStarvationSeconds;

            if (shooterConnected)
            {
                if (shooterFramesStarved && !reportedFrameGap)
                    Debug.LogWarning($"[8Ball][ShotFinalWait] Shot {shotSeq}, shooter={activeShotPlayerId}: playback frames delayed; waiting for the reliable final board before scoring.");
                reportedFrameGap = shooterFramesStarved;
                continue;
            }

            string cause = "shooter disconnected mid-shot";
            Debug.LogWarning($"[8Ball][DisconnectFreeze] Shooter {activeShotPlayerId} {cause}; finalising from the last server-accepted frame so neither client can continue a private simulation.");
            ServerForceCompleteStuckShot(cause);
            yield break;
        }
    }

    [Server]
    private void BroadcastServerSimFrameToWatchers(float frameTime)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null || poolMessenger.gameManager.allBalls == null)
            return;

        var balls = poolMessenger.gameManager.allBalls;
        List<int> ids = new List<int>(balls.Length);
        List<Vector3> pos = new List<Vector3>(balls.Length);
        List<Vector3> vel = new List<Vector3>(balls.Length);
        List<int> pockets = new List<int>(balls.Length);
        for (int i = 0; i < balls.Length; i++)
        {
            var b = balls[i];
            if (b == null || b.listener == null || b.listener.body == null)
                continue;
            ids.Add(b.id);
            pos.Add(b.listener.body.position);
            vel.Add(b.listener.body.linearVelocity);
            pockets.Add(b.inPocket ? b.pocketId : -1);
        }
        if (ids.Count == 0)
            return;

        int[] idArr = ids.ToArray();
        Vector3[] posArr = pos.ToArray();
        Vector3[] velArr = vel.ToArray();
        int[] pocketArr = pockets.ToArray();
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || !conn.isReady)
                continue;
            TargetRpcShotPlaybackFrame(conn, activeShotSequence, frameTime, idArr, posArr, velArr, pocketArr);
        }
    }

    [Server]
    private bool ServerForceCompleteStuckShot(string reason)
    {
        if (shotStopConsumed || activeShotPlayerId == -1)
            return false;
        int shooterId = activeShotPlayerId;
        shotStopConsumed = true;
        activeShotImpulse = string.Empty;
        activeShotBallPositions = new Vector3[0];
        ServerFreezeAuthoritativeBoard(reason);
        // Reliable final board. An unreliable playback frame can be lost exactly when the shooter's
        // transport dies; this keeps the watcher from stopping on an older table state.
        RpcForceFreezeShotAtServerBoard(CaptureAuthoritativeBoardPositions(), activeShotSequence);
        ApplyAuthoritativeShotEndSnapshot(shooterId);
        float time = (poolMessenger != null && poolMessenger.shotController != null && poolMessenger.shotController.cuePhysicsManager != null)
            ? poolMessenger.shotController.cuePhysicsManager.shotDuration
            : 0f;
        if (poolMessenger != null)
            poolMessenger.WaitAndStopMoveFromNetwork(time);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || !conn.isReady)
                continue;
            TargetRpcWaitAndStopMove(conn, time);
        }
        Debug.LogWarning($"[8Ball][Autonomous] Server force-completed shot by {shooterId} ({reason}). Shooter's WaitAndStopMoveFromNetwork was not required.");
        return true;
    }

    [Server]
    private Vector3[] CaptureAuthoritativeBoardPositions()
    {
        if (poolMessenger == null || poolMessenger.gameManager == null || poolMessenger.gameManager.allBalls == null)
            return new Vector3[0];

        var balls = poolMessenger.gameManager.allBalls;
        Vector3[] positions = new Vector3[balls.Length];
        for (int i = 0; i < balls.Length; i++)
        {
            var ball = balls[i];
            if (ball == null)
                continue;

            positions[i] = ball.position;
            if (ball.id != 0 && ball.inPocket)
                positions[i].y = -1f;
        }
        return positions;
    }

    [Server]
    private void ServerFreezeAuthoritativeBoard(string reason)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null || poolMessenger.gameManager.allBalls == null)
            return;
        int frozen = 0;
        foreach (var ball in poolMessenger.gameManager.allBalls)
        {
            if (ball == null || ball.listener == null || ball.listener.body == null)
                continue;
            if (ball.id != 0 && ball.inPocket)
            {
                Vector3 p = ball.position; p.y = -1f; ball.position = p;
            }
            var body = ball.listener.body;
            if (body.isKinematic)
                continue;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
            frozen++;
        }
        Debug.Log($"[8Ball][FreezeBoard] Server froze {frozen} balls to zero velocity ({reason}) â€” authoritative board is now final truth.");
    }


    [Server]
    private bool IsPlayerConnected(int playerId)
    {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null)
                continue;
            var player = conn.identity.GetComponent<MirrorPlayerPrefab>();
            if (player != null && player.playerData != null
                && int.TryParse(player.playerData.playerId, out int id) && id == playerId)
                return true;
        }
        return false;
    }

    [Server]
    private int ResolvePlayerId(NetworkConnectionToClient conn)
    {
        var player = conn != null && conn.identity != null
            ? conn.identity.GetComponent<MirrorPlayerPrefab>()
            : null;
        if (player != null && int.TryParse(player.playerData.playerId, out int playerId))
            return playerId;
        return currentTurnId;
    }

    [Server]
    private bool IsSenderCurrentTurn(NetworkConnectionToClient sender, string context)
    {
        int senderPlayerId = ResolvePlayerId(sender);
        if (sender != null && currentTurnId != -1 && senderPlayerId != currentTurnId)
        {
            Debug.LogWarning($"[8Ball][{context}] Ignored stale command from player {senderPlayerId}; current turn is {currentTurnId}.");
            return false;
        }
        return true;
    }

    [ClientRpc]
    void RpcSetCueBallInHand(bool value)
    {
        if (AightBallPoolGameLogic.gameState != null)
        {
            AightBallPoolGameLogic.gameState.cueBallInHand = value;
            if (value)
            {
                AightBallPoolGameLogic.gameState.cueBallInPocket = true;
                AightBallPoolGameLogic.gameState.needToChangeTurn = true;
            }
        }
    }

    public int GetCurrentTurn()
    {
        return currentTurnId;
    }

    public bool IsMainPlayerTurn()
    {
        return currentTurnId == staticVariables.UserProfiledata.user._id;
    }

    public string GetPlayer1Id() => player1Id;
    public string GetPlayer2Id() => player2Id;





    [Server]
    void RememberCueState(float pivotY, float verticalX, Vector2 displacement, float sliderZ, float force)
    {
        lastCuePivotY = pivotY;
        lastCueVerticalX = verticalX;
        lastCueDisplacement = displacement;
        lastCueSliderZ = sliderZ;
        lastCueForce = force;
        hasCueState = true;
    }

    [Command(requiresAuthority = false)]
    public void OnSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force, NetworkConnectionToClient sender = null)
    {
        if (!IsSenderCurrentTurn(sender, "CueControl") || poolMessenger == null)
            return;
        poolMessenger.OnSendCueControl(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
        RememberCueState(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == sender)
            {

            }
            else
            {

                TargetRpcCueControl(conn, cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
            }
        }
    }
    [TargetRpc]
    public void TargetRpcCueControl(NetworkConnectionToClient target, float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
    {
        poolMessenger.OnSendCueControl(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
    }






    [Command(requiresAuthority = false)]
    public void OnForceSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force, NetworkConnectionToClient sender = null)
    {
        if (!IsSenderCurrentTurn(sender, "ForceCueControl") || poolMessenger == null)
            return;
        poolMessenger.OnForceSendCueControl(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
        RememberCueState(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != sender)
                TargetRpcForceCueControl(conn, cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
        }
    }

    [TargetRpc]
    public void TargetRpcForceCueControl(NetworkConnectionToClient target, float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
    {
        poolMessenger.OnForceSendCueControl(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
    }

    // Every rejection below silently DROPS a ball-in-hand packet, so the watcher's cue ball simply
    // stops following the shooter with no trace anywhere. Worst case: if the PICK-UP packet is
    // rejected, AllowBallUpdateFromNetwork never turns on here and then EVERY drag packet after it is
    // rejected by the requireActiveSession check — the watcher's ball never moves for the whole drag.
    // Log it (throttled — this runs per drag packet, tens per second) so the next tester log shows it.
    private float lastCueBallPlacementRejectLogTime = -999f;

    [Server]
    private bool RejectCueBallPlacement(string why, Vector3 ballPosition)
    {
        if (Time.realtimeSinceStartup - lastCueBallPlacementRejectLogTime >= 1f)
        {
            lastCueBallPlacementRejectLogTime = Time.realtimeSinceStartup;
            AightBallPoolGameState s = AightBallPoolGameLogic.gameState;
            Debug.LogWarning($"[8Ball][BallInHand] Cue-ball placement packet REJECTED ({why}); pos={ballPosition}, "
                + $"cueBallInHand={(s == null ? "gameState NULL" : s.cueBallInHand.ToString())}, "
                + $"phase={(EightBallShotPhase)shotPhase}, turn={currentTurnId}. "
                + "While this repeats, the watcher's cue ball will NOT follow the drag. (throttled to 1/s)");
        }
        return false;
    }

    [Server]
    private bool CanAcceptCueBallPlacement(Vector3 ballPosition, bool requireActiveSession)
    {
        if (poolMessenger == null || poolMessenger.shotController == null)
            return RejectCueBallPlacement("pool objects not ready", ballPosition);

        AightBallPoolGameState state = AightBallPoolGameLogic.gameState;
        if (state == null || !state.cueBallInHand)
            return RejectCueBallPlacement("not ball-in-hand on the server", ballPosition);

        bool placementPhase = shotPhase == (int)EightBallShotPhase.Aiming
            || shotPhase == (int)EightBallShotPhase.BallInHand;
        if (!placementPhase)
            return RejectCueBallPlacement("wrong shot phase", ballPosition);

        ShotController shotController = poolMessenger.shotController;
        if (requireActiveSession && !shotController.AllowBallUpdateFromNetwork)
            return RejectCueBallPlacement("no active drag session (the pick-up packet never landed)", ballPosition);
        if (!IsFiniteVector(ballPosition) || ballPosition.y < -0.1f || shotController.mainCueBall == null)
            return RejectCueBallPlacement("position not finite / below table / no cue ball", ballPosition);

        // Keep the server guard limited to rejecting off-table/pocket-path packets. The existing
        // local placement code still enforces the smaller opening-break (kitchen) area.
        Transform placementSpace = shotController.clothPosition != null
            ? shotController.clothPosition
            : shotController.initialMoveSpace;
        if (placementSpace == null
            || !BallGeometry.SphereInCube(ballPosition, shotController.mainCueBall.radius, placementSpace))
            return RejectCueBallPlacement("position outside the placement area", ballPosition);

        return true;
    }

    [Command(requiresAuthority = false)]
    public void OnMoveBall(Vector3 ballPosition, NetworkConnectionToClient sender = null)
    {
        if (!IsSenderCurrentTurn(sender, "MoveBall") || poolMessenger == null)
            return;
        if (!CanAcceptCueBallPlacement(ballPosition, true))
            return;
        poolMessenger.OnMoveBall(ballPosition);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == sender)
            {

            }
            else
            {

                TargetRpcOnMoveBall(conn, ballPosition);
            }
        }
    }
    [TargetRpc]
    public void TargetRpcOnMoveBall(NetworkConnectionToClient target, Vector3 ballPosition)
    {
        poolMessenger.OnMoveBall(ballPosition);
    }
    [Command(requiresAuthority = false)]
    public void CmdSwitchCueState(bool Isactive, NetworkConnectionToClient sender = null)
    {
        lastCueVisible = Isactive;
        ShotController.shotControllerInstance.SwitchCue(Isactive);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == sender)
            {

            }
            else
            {

                RpcSwitchCueState(conn, Isactive);
            }
        }
    }
    [TargetRpc]
    public void RpcSwitchCueState(NetworkConnectionToClient target, bool Isactive)
    {
        ShotController.shotControllerInstance.SwitchCue(Isactive);
    }
    // Turn change -> both clients bring the cue rig (stick + aim line) back up. See
    // ShotController.ResetCueRigOnTurnChange for why this is an Rpc and not the ballIsDragging SyncVar.
    [ClientRpc]
    public void RpcResetCueRigOnTurnChange()
    {
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1)
            return;
        ShotController shot = poolMessenger != null && poolMessenger.shotController != null
            ? poolMessenger.shotController
            : ShotController.shotControllerInstance;
        if (shot == null)
            return;
        shot.ResetCueRigOnTurnChange();
    }
    // Reconnect-only: converge the rejoining client's cue-rig visibility to the server's cached value.
    // "visible" simply re-shows (same as a live relay). "hidden" must be STICKY — a one-shot hide is
    // overridden on the very next frame by SyncWithNetwork's ShowCueRigFromNetworkCueControlIfAllowed —
    // so it arms the suppression flag that gate already checks; any live cue-control packet or a real
    // show relay clears it again (see ShotController.SuppressNetworkCueRigForReconnect).
    [TargetRpc]
    public void TargetRpcRestoreCueVisibility(NetworkConnectionToClient target, bool visible)
    {
        if (ShotController.shotControllerInstance == null)
            return;
        if (visible)
            ShotController.shotControllerInstance.SwitchCue(true);
        else
            ShotController.shotControllerInstance.SuppressNetworkCueRigForReconnect();
        Debug.Log("[8Ball][Reconnect] Cue-rig visibility restored from server cache: visible=" + visible + ".");
    }
    [Command(requiresAuthority = false)]
    public void SelectBallPosition(Vector3 ballPosition, NetworkConnectionToClient sender = null)
    {
        if (!IsSenderCurrentTurn(sender, "SelectBallPosition") || poolMessenger == null)
            return;
        if (!CanAcceptCueBallPlacement(ballPosition, false))
            return;
        // Authoritative "opponent is dragging the cue ball" signal (see ballIsDragging). Idempotent:
        // fires on pick-up and every drag move; Mirror only syncs on change.
        ballIsDragging = true;
        poolMessenger.SelectBallPosition(ballPosition);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == sender)
            {

            }
            else
            {

                TargetRpcSelectBallPosition(conn, ballPosition);
            }
        }
    }
    [TargetRpc]
    public void TargetRpcSelectBallPosition(NetworkConnectionToClient target, Vector3 ballPosition)
    {
        poolMessenger.SelectBallPosition(ballPosition);
    }
    [Command(requiresAuthority = false)]
    public void SetBallPosition(Vector3 ballPosition, NetworkConnectionToClient sender = null)
    {
        if (!IsSenderCurrentTurn(sender, "SetBallPosition") || poolMessenger == null)
            return;
        // THE DROP IS UNCONDITIONAL (user-directed). This is the ONE packet that decides where the cue
        // ball actually ends up, so it must never be discarded. It used to go through
        // CanAcceptCueBallPlacement(requireActiveSession: true), which needs the PICK-UP packet to have
        // landed first — and the pick-up is gated on `controlInNetwork` while the drop is gated on
        // `isOnLine` (GameManager.ShotController_OnSelectBall vs _OnUnselectBall, two DIFFERENT
        // conditions). So a pick-up that never went out silently threw the drop away and left the
        // shooter, the server and the watcher on THREE different cue-ball positions — the tester's
        // "drop krty to dono side diff place pe drop hui hoti, game ka behavior abnormal hojata".
        // Only a non-finite position is still refused: a NaN/Inf here is not a gameplay rule but
        // corruption — it poisons both clients' physics and cannot be recovered from.
        if (!IsFiniteVector(ballPosition))
        {
            Debug.LogWarning($"[8Ball][BallInHand] Drop REFUSED — position is not finite ({ballPosition}).");
            return;
        }
        // Drag ended (ball placed) -> the watcher's hook shows the cue rig + rebuilds the aim line.
        ballIsDragging = false;
        poolMessenger.SetBallPosition(ballPosition);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == sender)
            {

            }
            else
            {

                TargetRpcSetBallPosition(conn, ballPosition);
            }
        }
    }
    [TargetRpc]
    public void TargetRpcSetBallPosition(NetworkConnectionToClient target, Vector3 ballPosition)
    {
        poolMessenger.SetBallPosition(ballPosition);
    }



    [Command(requiresAuthority = false)]
    public void SetMechanicalStatesFromNetwork(int ballId, string mechanicalStateData, NetworkConnectionToClient sender = null)
    {
        int senderPlayerId = ResolvePlayerId(sender);
        if (sender != null && currentTurnId != -1 && senderPlayerId != currentTurnId)
        {
            Debug.LogWarning($"[8Ball][MechanicalState] Ignored stale state from player {senderPlayerId}; current turn is {currentTurnId}.");
            return;
        }
        if (poolMessenger == null || poolMessenger.gameManager == null
            || ballId < 0 || ballId >= poolMessenger.gameManager.allBalls.Length)
        {
            Debug.LogWarning($"[8Ball][MechanicalState] Ignored invalid state for ball {ballId}; pool objects are not ready.");
            return;
        }
        poolMessenger.SetMechanicalStatesFromNetwork(ballId, mechanicalStateData);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == sender)
            {

            }
            else
            {

                TargetRpcMechanicalStates(conn, ballId, mechanicalStateData);
            }
        }
    }






    [Command(channel = Channels.Unreliable, requiresAuthority = false)]
    public void SetShotPlaybackFrameFromNetwork(int shotSequence, float time, int[] ballIds, Vector3[] positions, Vector3[] velocities, int[] pocketIds, NetworkConnectionToClient sender = null)
    {
        int senderPlayerId = ResolvePlayerId(sender);
        if (sender != null && !IsAuthorizedShotSender(senderPlayerId))
        {
            return;
        }
        if (activeShotSequence <= 0 || shotStopConsumed)
        {
            return;
        }
        if (shotSequence > 0 && shotSequence != activeShotSequence)
        {
            return;
        }
        if (ballIds == null || positions == null || velocities == null || pocketIds == null)
        {
            return;
        }
        if (ballIds.Length != positions.Length || ballIds.Length != velocities.Length || ballIds.Length != pocketIds.Length)
        {
            return;
        }
        if (StreamFrameHasInvalidValues(positions, velocities))
        {
            return;
        }
        if (FramePocketIdsAreInvalid(pocketIds))
        {
            return;
        }

        // Drive the SERVER's board from the shooter's frames EXACTLY like a watcher: feed them into the
        // same playback pipeline (EnqueuePlaybackKeyframe -> DrivePlayback). Combined with
        // IsWatcherPlaybackContext() returning true on the dedicated server, StartSimulate puts the
        // server in playback mode instead of its own sim, so it no longer fights the frames.
        if (poolMessenger != null)
            poolMessenger.SetShotPlaybackFrameFromNetwork(activeShotSequence, time, ballIds, positions, velocities, pocketIds);

        // Accepted pocket flags must reach scoring even if the delayed playback cursor has not
        // consumed this frame when the shooter disconnects or the hard deadline expires.
        ApplyFinalShotFrameToServerBoard(ballIds, positions, velocities, pocketIds);

        // Track transport activity separately from the reliable shot-completion signal.
        lastShooterFrameTime = time;
        lastShotFrameRealtime = Time.realtimeSinceStartup;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn != sender)
            {
                TargetRpcShotPlaybackFrame(conn, activeShotSequence, time, ballIds, positions, velocities, pocketIds);
            }
        }
    }

    private bool IsAuthorizedShotSender(int senderPlayerId)
    {
        int expected = activeShotPlayerId != -1 ? activeShotPlayerId : currentTurnId;
        return expected == -1 || senderPlayerId == expected;
    }

    private static bool StreamFrameHasInvalidValues(Vector3[] positions, Vector3[] velocities)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            if (!IsFiniteVector(positions[i]) || !IsFiniteVector(velocities[i]))
                return true;
        }
        return false;
    }

    private static bool FramePocketIdsAreInvalid(int[] pocketIds)
    {
        for (int i = 0; i < pocketIds.Length; i++)
        {
            if (pocketIds[i] < -1)
                return true;
        }
        return false;
    }

    private static bool IsFiniteVector(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
              || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
    }

    [TargetRpc(channel = Channels.Unreliable)]
    public void TargetRpcShotPlaybackFrame(NetworkConnectionToClient target, int shotSequence, float time, int[] ballIds, Vector3[] positions, Vector3[] velocities, int[] pocketIds)
    {
        if (poolMessenger == null)
        {
            return;
        }
        if (IsIncomingPlaybackFrameStale(shotSequence))
        {
            return;
        }
        poolMessenger.SetShotPlaybackFrameFromNetwork(shotSequence, time, ballIds, positions, velocities, pocketIds);
    }



    [TargetRpc]
    public void TargetRpcSetBallPos(NetworkConnectionToClient target, int ballId, Vector3 pos, Vector3 linVel, Vector3 angVel, int snapshotTurnId)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null) return;
        if (snapshotTurnId > 0 && currentTurnId != -1 && currentTurnId != snapshotTurnId)
        {
            Debug.Log($"[8Ball][ReconnectSnapshot] Dropped stale ball restore for ball {ballId}. snapshotTurn={snapshotTurnId}, currentTurn={currentTurnId}.");
            return;
        }
        if (poolMessenger.shotController != null
            && (poolMessenger.shotController.isMoving
                || (poolMessenger.shotController.cuePhysicsManager != null && poolMessenger.shotController.cuePhysicsManager.IsInMove)))
        {
            Debug.Log($"[8Ball][ReconnectSnapshot] Dropped ball restore for ball {ballId} because a shot is in progress.");
            return;
        }








        if (ballId == 0 && pos.y < -0.1f)
        {
            // Cue ball is pocketed in the reconnect snapshot. Do NOT recompute a respot LOCALLY here — that is
            // exactly the two-white-balls bug (tester "white ball 1/2", reports 8328bf30 / 13eb3ed1): the old
            // code called shotController.FindCueBallRespotPosition() -> RepositionBallInCube, a scan of the
            // LOCAL board, so each device picked a DIFFERENT on-table spot and the two screens showed the cue
            // ball in two places. ShotController.cs itself warns against this ("Never let each machine
            // RepositionBallInCube independently — that produced a different cue spot per screen").
            //
            // The server already owns the respot: the reconnect sequence computes ONE position
            // (RespotCueBallInHand, ~line 1130) and broadcasts RpcRespotCueBall(cueRespot,...) to ALL clients,
            // which lands identically on both. That authoritative respot is the single source of truth, so we
            // just leave the cue ball to it here. If it hasn't arrived yet the ball is briefly still below the
            // table (self-heals the instant RpcRespotCueBall lands) — strictly better than a permanent ghost
            // cue ball at a divergent local spot. BallSpawner tray entry is cleared so no stale tray icon lingers.
            BallSpawner.RemoveTrayBall(0);
            Debug.Log("[8Ball][Reconnect] Cue ball pocketed in snapshot — leaving the respot to the authoritative RpcRespotCueBall (no local recompute, so both screens agree).");
            return;
        }
        poolMessenger.gameManager.allBalls[ballId].position = pos;
        if (pos.y < -0.1)
        {
            var pocketed = poolMessenger.gameManager.allBalls[ballId];
            if (pocketed.listener != null && pocketed.listener.body != null && !pocketed.listener.body.isKinematic)
            {
                pocketed.listener.body.linearVelocity = Vector3.zero;
                pocketed.listener.body.angularVelocity = Vector3.zero;
                pocketed.listener.body.Sleep();
            }






            if (ballId != 0)
            {
                // Restore, not a fresh pot: rebuild this tray ball immediately with the scripted drop.
                // The queued/gravity path is wrong here — it trickles one ball every 0.35 s, and it
                // needs scene physics to be stepping, which it is not during a reconnect restore.
                BallSpawner.RestoreTrayBall(ballId);
            }
            poolMessenger.gameManager.allBalls[ballId].inPocket = true;
            poolMessenger.gameManager.ShadowToggle.ballShadows.Remove(poolMessenger.gameManager.allBalls[ballId].ballShadow.gameObject);
            poolMessenger.gameManager.allBalls[ballId].ballShadow.gameObject.SetActive(false);



            if (poolMessenger.gameManager.allBalls[ballId].ballBlick != null)
                poolMessenger.gameManager.allBalls[ballId].ballBlick.gameObject.SetActive(false);
        }
        else
        {
            var ball = poolMessenger.gameManager.allBalls[ballId];




            if (ball.inPocket)
            {
                ball.isActive = false;
                ball.inPocket = false;
                ball.pocketId = -1;
                ball.hitShapeId = -2;
                ball.OnState(BallPool.Mechanics.BallState.ExitFromPocket);

                BallSpawner.RemoveTrayBall(ballId);
            }
            ball.listener.body.linearVelocity = Vector3.zero;
            ball.listener.body.angularVelocity = Vector3.zero;
            ball.listener.body.Sleep();
            ball.SetBallShadowAndBlickBlick();
            if (ballId == 0 && poolMessenger != null && poolMessenger.shotController != null)
                poolMessenger.shotController.AnchorCuePivotToCueBall();
        }
    }









    [ClientRpc]
    public void RpcSyncBallsAfterShot(Vector3[] positions, int shotSequence)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null) return;
        StartCoroutine(SyncBallsAfterShotRoutine(positions, shotSequence));
    }

    // Used only when the active shooter disappears. Do not wait for a local replay to settle:
    // that replay is no longer authoritative once its source has disconnected. Stop it first,
    // then apply the one server board both clients must see.
    [ClientRpc]
    private void RpcForceFreezeShotAtServerBoard(Vector3[] positions, int shotSequence)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null)
            return;
        if (IsPostShotSnapshotStale(shotSequence))
            return;

        ShotController shot = poolMessenger.shotController;
        if (shot != null && shot.cuePhysicsManager != null)
            shot.cuePhysicsManager.StopPlaybackForReconcile();
        if (shot != null)
            shot.ClearMovingForReconcile();

        StartCoroutine(SyncBallsAfterShotRoutine(positions, 0));
    }

    [TargetRpc]
    public void TargetRpcSyncBallsAfterReconnect(NetworkConnectionToClient target, Vector3[] positions, int snapshotTurnId)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null) return;
        reconnectBallsSyncInProgress = true;
        StartCoroutine(SyncBallsAfterReconnectRoutine(positions, snapshotTurnId));
    }

    System.Collections.IEnumerator SyncBallsAfterReconnectRoutine(Vector3[] positions, int snapshotTurnId)
    {
        yield return null;
        if (snapshotTurnId <= 0 || (currentTurnId != -1 && currentTurnId != snapshotTurnId))
        {
            Debug.Log($"[8Ball][ReconnectSnapshot] Dropped stale reconnect ball snapshot. snapshotTurn={snapshotTurnId}, currentTurn={currentTurnId}.");
            FinishReconnectBallsSync();
            yield break;
        }

        if (IsLocalShotReplayMoving() && BallPool.BallPoolGameLogic.controlFromNetwork)
        {
            Debug.Log("[8Ball][ReconnectSnapshot] Force-ending stale watcher replay so the authoritative board can snap and stick.");
            if (poolMessenger.shotController != null && poolMessenger.shotController.cuePhysicsManager != null)
                poolMessenger.shotController.cuePhysicsManager.StopPlaybackForReconcile();
            if (poolMessenger.shotController != null)
                poolMessenger.shotController.ClearMovingForReconcile();
            yield return null;
        }

        yield return StartCoroutine(SyncBallsAfterShotRoutine(positions, 0));
        FinishReconnectBallsSync();
    }

    private void FinishReconnectBallsSync()
    {
        reconnectBallsSyncInProgress = false;

        if (!reconnectStateSyncCompleteReceived)
            return;

        if (reconnectOverlayHideRoutine != null)
            StopCoroutine(reconnectOverlayHideRoutine);

        reconnectOverlayHideRoutine = StartCoroutine(HideReconnectStateOverlayWhenReady());
    }






    [ClientRpc]
    public void RpcRespotCueBall(Vector3 position, bool cueBallInHand, bool cueBallInPocket)
    {
        if (poolMessenger == null || poolMessenger.shotController == null) return;
        if (BallPool.AightBallPoolGameLogic.gameState != null)
        {
            BallPool.AightBallPoolGameLogic.gameState.cueBallInHand = cueBallInHand;
            BallPool.AightBallPoolGameLogic.gameState.cueBallInPocket = cueBallInPocket;
        }
        poolMessenger.shotController.ApplyCueBallRespot(position);
        BallSpawner.RemoveTrayBall(0);
    }

    System.Collections.IEnumerator SyncBallsAfterShotRoutine(Vector3[] positions, int shotSequence)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null) yield break;
        if (IsPostShotSnapshotStale(shotSequence))
            yield break;

        if (IsLocalShotReplayMoving() && IsSelfHealedWatcher())
        {
            Debug.Log("[8Ball][NetworkReplay] Force-ending self-healed watcher sim so the server final board applies at shot-end (not on reconnect).");
            if (poolMessenger.shotController != null && poolMessenger.shotController.cuePhysicsManager != null)
                poolMessenger.shotController.cuePhysicsManager.StopPlaybackForReconcile();
            if (poolMessenger.shotController != null)
                poolMessenger.shotController.ClearMovingForReconcile();
            yield return null;
        }
        else
        {
            float timeout = 12f;
            while (timeout > 0f && IsLocalShotReplayMoving())
            {
                if (IsPostShotSnapshotStale(shotSequence))
                    yield break;
                timeout -= UnityEngine.Time.deltaTime;
                yield return null;
            }
        }

        if (poolMessenger == null || poolMessenger.gameManager == null) yield break;
        if (IsPostShotSnapshotStale(shotSequence))
            yield break;
        if (IsLocalShotReplayMoving())
            Debug.LogWarning("[8Ball][NetworkReplay] Applying post-shot ball sync after local settle timeout.");

        var allBalls = poolMessenger.gameManager.allBalls;
        int count = UnityEngine.Mathf.Min(positions.Length, allBalls.Length);




        for (int i = 0; i < count; i++)
        {






            if (i == 0 && BallPool.AightBallPoolGameLogic.gameState != null
                && (BallPool.AightBallPoolGameLogic.gameState.cueBallInHand
                    || BallPool.AightBallPoolGameLogic.gameState.cueBallInPocket))
                continue;
            if (positions[i].y < -0.1f && !allBalls[i].inPocket)
            {





                allBalls[i].position = positions[i];
                allBalls[i].inPocket = true;
                allBalls[i].OnState(BallPool.Mechanics.BallState.EnterInPocket);

                if (i != 0)
                {
                    // shotSequence == 0 marks the reconnect restore (SyncBallsAfterReconnectRoutine
                    // passes 0); a genuine post-shot sync carries the shot's own sequence and keeps
                    // the normal pot cadence.
                    if (shotSequence == 0)
                        BallSpawner.RestoreTrayBall(i);
                    else
                        BallSpawner.EnqueueTrayBall(i);
                }
            }
        }

        allBalls = poolMessenger.gameManager.allBalls;
        count = UnityEngine.Mathf.Min(positions.Length, allBalls.Length);
        for (int i = 0; i < count; i++)
        {
            // RpcRespotCueBall is sent after this snapshot is captured, while this routine may
            // apply later after local playback settles. Preserve that authoritative cue respot;
            // otherwise the second pass can move it back to the pre-respot pocket position.
            if (shotSequence > 0 && i == 0 && BallPool.AightBallPoolGameLogic.gameState != null
                && (BallPool.AightBallPoolGameLogic.gameState.cueBallInHand
                    || BallPool.AightBallPoolGameLogic.gameState.cueBallInPocket))
                continue;

            if (positions[i].y < -0.1f) continue;
            var ball = allBalls[i];
            if (ball.inPocket)
            {




                ball.position = positions[i];
                ball.isActive = false;
                ball.inPocket = false;
                ball.pocketId = -1;
                ball.hitShapeId = -2;
                ball.OnState(BallPool.Mechanics.BallState.ExitFromPocket);
                ball.listener.body.linearVelocity = UnityEngine.Vector3.zero;
                ball.listener.body.angularVelocity = UnityEngine.Vector3.zero;
                ball.listener.body.Sleep();


                BallSpawner.RemoveTrayBall(i);
            }
            else
            {
                ball.position = positions[i];
                ball.listener.body.linearVelocity = UnityEngine.Vector3.zero;
                ball.listener.body.angularVelocity = UnityEngine.Vector3.zero;
                ball.listener.body.Sleep();
                ball.OnState(BallPool.Mechanics.BallState.SetState);






                if (i != 0 && BallSpawner.HasTrayBall(i))
                    BallSpawner.RemoveTrayBall(i);
            }
        }

        if (poolMessenger != null && poolMessenger.shotController != null)
            poolMessenger.shotController.AnchorCuePivotToCueBall();



        if (poolMessenger != null && poolMessenger.gameManager != null)
            poolMessenger.gameManager.RefreshActiveBallsUI();
    }

    private bool IsPostShotSnapshotStale(int shotSequence)
    {
        if (shotSequence <= 0 || latestStartedShotSequence <= 0)
            return false;
        if (shotSequence >= latestStartedShotSequence)
            return false;

        Debug.Log($"[8Ball][NetworkReplay] Dropped stale post-shot ball sync. snapshotSeq={shotSequence}, latestStartedSeq={latestStartedShotSequence}.");
        return true;
    }

    private bool IsIncomingShotStartStale(int shotSequence)
    {
        if (shotSequence <= 0 || latestStartedShotSequence <= 0)
            return false;
        if (shotSequence >= latestStartedShotSequence)
            return false;

        Debug.Log($"[8Ball][NetworkReplay] Dropped stale StartSimulate. startSeq={shotSequence}, latestStartedSeq={latestStartedShotSequence}.");
        return true;
    }

    private bool IsIncomingPlaybackFrameStale(int shotSequence)
    {
        if (shotSequence <= 0)
            return false;
        if (latestStartedShotSequence <= 0)
            return true;
        if (shotSequence == latestStartedShotSequence)
            return false;
        if (shotSequence > latestStartedShotSequence)
            return true;

        Debug.Log($"[8Ball][Playback] Dropped stale playback frame. frameSeq={shotSequence}, latestStartedSeq={latestStartedShotSequence}.");
        return true;
    }

    [TargetRpc]
    public void TargetRpcMechanicalStates(NetworkConnectionToClient target, int ballId, string mechanicalStateData)
    {
        poolMessenger.SetMechanicalStatesFromNetwork(ballId, mechanicalStateData);
    }
    [Command(requiresAuthority = false)]
    public void WaitAndStopMoveFromNetwork(float time, NetworkConnectionToClient sender = null)
    {
        int senderPlayerId = ResolvePlayerId(sender);
        if (sender != null && !IsAuthorizedShotSender(senderPlayerId))
        {
            Debug.LogWarning($"[8Ball][NetworkReplay] Ignored stale WaitAndStopMove from player {senderPlayerId}; active shooter is {(activeShotPlayerId != -1 ? activeShotPlayerId : currentTurnId)}.");
            return;
        }
        if (poolMessenger == null)
            return;





        if (shotStopConsumed)
        {
            Debug.LogWarning($"[8Ball][NetworkReplay] Ignored already-consumed/stale stop from player {senderPlayerId} (this shot's stop was already processed).");
            return;
        }
        shotStopConsumed = true;
        activeShotImpulse = string.Empty;
        activeShotBallPositions = new Vector3[0];


        ApplyAuthoritativeShotEndSnapshot(activeShotPlayerId != -1 ? activeShotPlayerId : senderPlayerId);
        poolMessenger.WaitAndStopMoveFromNetwork(time);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == sender)
            {

            }
            else
            {

                TargetRpcWaitAndStopMove(conn, time);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSubmitEightBallShotFinalFrameV2(float time, int shotSequence, int[] ballIds, Vector3[] positions, Vector3[] velocities, int[] pocketIds, NetworkConnectionToClient sender = null)
    {
        int senderPlayerId = ResolvePlayerId(sender);
        if (sender != null && !IsAuthorizedShotSender(senderPlayerId))
        {
            Debug.LogWarning($"[8Ball][NetworkReplay] Ignored stale reliable WaitAndStopMove from player {senderPlayerId}; active shooter is {(activeShotPlayerId != -1 ? activeShotPlayerId : currentTurnId)}.");
            return;
        }
        if (poolMessenger == null)
            return;
        if (shotStopConsumed)
        {
            // Repair the board if the reliable final frame lands after a disconnect/hard timeout.
            // Keep completion idempotent: a late board must not score the same shot twice.
            if ((sender == null || IsAuthorizedShotSender(senderPlayerId))
                && TryApplyReliableFinalShotFrame(shotSequence, time, ballIds, positions, velocities, pocketIds))
            {
                Debug.LogWarning($"[8Ball][NetworkReplay] Final board from player {senderPlayerId} landed AFTER shot {activeShotSequence} was force-completed; applied late so every pocket flag is authoritative again.");
                RpcSyncBallsAfterShot(CaptureAuthoritativeBoardPositions(), activeShotSequence);
            }
            else
            {
                Debug.LogWarning($"[8Ball][NetworkReplay] Ignored already-consumed/stale reliable stop from player {senderPlayerId} (this shot's stop was already processed).");
            }
            return;
        }
        if (shotSequence > 0 && shotSequence != activeShotSequence)
        {
            Debug.LogWarning($"[8Ball][NetworkReplay] Ignored stale reliable shot stop. stopSeq={shotSequence}, activeSeq={activeShotSequence}.");
            return;
        }

        if (!TryApplyReliableFinalShotFrame(shotSequence, time, ballIds, positions, velocities, pocketIds))
        {
            Debug.LogWarning($"[8Ball][NetworkReplay] Reliable final shot frame was invalid/missing for shot {activeShotSequence}; falling back to current server board.");
        }

        shotStopConsumed = true;
        activeShotImpulse = string.Empty;
        activeShotBallPositions = new Vector3[0];

        ApplyAuthoritativeShotEndSnapshot(activeShotPlayerId != -1 ? activeShotPlayerId : senderPlayerId);
        poolMessenger.WaitAndStopMoveFromNetwork(time);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != sender)
            {
                TargetRpcWaitAndStopMove(conn, time);
            }
        }
    }

    [Server]
    private bool TryApplyReliableFinalShotFrame(int shotSequence, float time, int[] ballIds, Vector3[] positions, Vector3[] velocities, int[] pocketIds)
    {
        if (activeShotSequence <= 0)
            return false;
        if (shotSequence > 0 && shotSequence != activeShotSequence)
            return false;
        if (ballIds == null || positions == null || velocities == null || pocketIds == null)
            return false;
        if (ballIds.Length == 0 || ballIds.Length != positions.Length || ballIds.Length != velocities.Length || ballIds.Length != pocketIds.Length)
            return false;
        if (StreamFrameHasInvalidValues(positions, velocities))
            return false;
        if (FramePocketIdsAreInvalid(pocketIds))
            return false;

        if (poolMessenger != null)
            poolMessenger.SetFinalShotPlaybackFrameFromNetwork(activeShotSequence, time, ballIds, positions, velocities, pocketIds);
        ApplyFinalShotFrameToServerBoard(ballIds, positions, velocities, pocketIds);
        lastShooterFrameTime = Mathf.Max(lastShooterFrameTime, time);
        lastShotFrameRealtime = Time.realtimeSinceStartup;
        return true;
    }

    [Server]
    private void ApplyFinalShotFrameToServerBoard(int[] ballIds, Vector3[] positions, Vector3[] velocities, int[] pocketIds)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null || poolMessenger.gameManager.allBalls == null)
            return;

        var balls = poolMessenger.gameManager.allBalls;
        for (int i = 0; i < ballIds.Length; i++)
        {
            int id = ballIds[i];
            if (id < 0 || id >= balls.Length)
                continue;
            var ball = balls[id];
            if (ball == null || ball.listener == null || ball.listener.body == null)
                continue;
            int pocketId = pocketIds[i];
            if (pocketId != -1)
            {
                ApplyFinalFramePocketToServerBoard(ball, positions[i], pocketId);
                continue;
            }
            if (id != 0 && ball.inPocket)
                continue;

            ball.position = positions[i];
            if (!ball.listener.body.isKinematic)
                ball.listener.body.linearVelocity = velocities[i];
        }
    }

    [Server]
    private void ApplyFinalFramePocketToServerBoard(BallPool.Mechanics.Ball ball, Vector3 position, int pocketId)
    {
        if (ball == null || ball.listener == null || ball.listener.body == null)
            return;

        ball.position = position;
        if (ball.inPocket)
        {
            ball.pocketId = pocketId;
            return;
        }

        PocketDetector pocket = ResolvePocketDetector(pocketId);
        if (pocket != null)
        {
            ball.listener.OnBallEnterPocket(pocket);
        }
        else
        {
            ball.inPocket = true;
            ball.pocketId = pocketId;
            ball.hitShapeId = -2;
            ball.OnState(BallPool.Mechanics.BallState.EnterInPocket);
            if (ball.id != 0)
                BallSpawner.EnqueueTrayBall(ball.id);
        }
    }

    private PocketDetector ResolvePocketDetector(int pocketId)
    {
        if (poolMessenger == null
            || poolMessenger.shotController == null
            || poolMessenger.shotController.cuePhysicsManager == null
            || poolMessenger.shotController.cuePhysicsManager.PocketListeners == null
            || pocketId < 0
            || pocketId >= poolMessenger.shotController.cuePhysicsManager.PocketListeners.Length)
        {
            return null;
        }
        return poolMessenger.shotController.cuePhysicsManager.PocketListeners[pocketId];
    }
    [TargetRpc]
    public void TargetRpcWaitAndStopMove(NetworkConnectionToClient target, float time)
    {
        poolMessenger.WaitAndStopMoveFromNetwork(time);
    }




    /// <summary>
    /// FIX (legal pot, turn STILL shifted — stale per-shot rule flags). `needToChangeTurn` has exactly ONE
    /// clear site in the whole project: AightBallPoolGameLogic.RessetState(), and it is reached only through
    /// the physics event chain (PhysicsHandeler.ApplyImpulse -> OnCueStrike ->
    /// AightBallPoolGameManager.OnStartShot, and only AFTER base.OnStartShot returns). Shot end never
    /// recomputes it — AightBallPoolGameLogic.OnEndShot only ever sets it TRUE — so if that chain fails to
    /// reach RessetState for one shot, the PREVIOUS shot's needToChangeTurn survives and
    /// AightBallPoolGameManager.DeriveOutcomeCode (which reads the FLAG, not hasRightBallInPocket) publishes
    /// NoRightBallPotted — "No ball potted, turn passes" — on a pot ReconcileAuthoritativePocketResult had
    /// already credited. This build runs playback instead of physics, so it leans on that chain harder than
    /// a real client does.
    ///
    /// Clearing the flags where the server ACCEPTS the shot makes the authoritative decision independent of
    /// that chain. Deliberately NOT a full RessetState(): cueBallInHand and gameIsComplete are left alone so
    /// ApplyShotStartSnapshot still sees exactly what it sees today. [Server] only — offline/AI keeps
    /// resetting exactly where it always did.
    /// </summary>
    [Server]
    private void ServerResetShotRuleStateForNewShot()
    {
        var gs = AightBallPoolGameLogic.gameState;
        if (gs == null)
            return;

        gs.needToChangeTurn = false;
        gs.cueBallHasHitRightBall = false;
        gs.cueBallHasHitSomeBall = false;
        gs.hasRightBallInPocket = false;
        gs.ballsHitBoardCount = 0;
        gs.cueBallInPocket = false;
        if (AightBallPoolPlayer.mainPlayer != null)
            AightBallPoolPlayer.mainPlayer.isCueBall = false;
        if (AightBallPoolPlayer.otherPlayer != null)
            AightBallPoolPlayer.otherPlayer.isCueBall = false;
    }

    [Server]
    private void CaptureServerPreShotState()
    {
        activeShotAfterBreak = breakShotDone;
        if (poolMessenger != null && poolMessenger.gameManager != null && poolMessenger.gameManager.allBalls != null)
        {
            var balls = poolMessenger.gameManager.allBalls;
            preShotBallPocketed = new bool[balls.Length];
            for (int i = 0; i < balls.Length; i++)
                preShotBallPocketed[i] = balls[i] != null && (balls[i].inPocket || balls[i].position.y < -0.1f);
        }
        else
        {
            preShotBallPocketed = null;
        }
    }







    [Server]
    public void ReconcileAuthoritativePocketResult(int shooterPlayerId)
    {
        var gs = AightBallPoolGameLogic.gameState;
        if (gs == null || preShotBallPocketed == null || poolMessenger == null
            || poolMessenger.gameManager == null || poolMessenger.gameManager.allBalls == null)
        {
            Debug.LogWarning($"[8Ball][ServerPocketRule] Could not reconcile shot for shooter={shooterPlayerId}; authoritative snapshot is unavailable.");
            return;
        }

        AightBallPoolPlayer shooter = null;
        if (BallPoolPlayer.players != null)
        {
            foreach (BallPoolPlayer player in BallPoolPlayer.players)
            {
                if (player != null && player.playerId == shooterPlayerId)
                {
                    shooter = player as AightBallPoolPlayer;
                    break;
                }
            }
        }

        if (shooter == null)
        {
            Debug.LogError($"[8Ball][ServerPocketRule] Shooter {shooterPlayerId} was not found; refusing to decide the turn from a local myTurn value.");
            return;
        }

        var newlyPocketed = new List<int>();
        var ownBallsPocketed = new List<int>();
        var opponentBallsPocketed = new List<int>();
        bool cueBallPocketed = false;
        bool blackBallPocketed = false;
        bool anyObjectBallPocketed = false;
        var balls = poolMessenger.gameManager.allBalls;

        for (int i = 0; i < balls.Length; i++)
        {
            BallPool.Mechanics.Ball ball = balls[i];
            if (ball == null)
                continue;

            bool wasPocketed = i < preShotBallPocketed.Length && preShotBallPocketed[i];
            bool isPocketedNow = ball.inPocket || ball.position.y < -0.5f;
            if (wasPocketed || !isPocketedNow)
                continue;

            int ballId = ball.id;
            newlyPocketed.Add(ballId);

            if (AightBallPoolGameLogic.isCueBall(ballId))
            {
                cueBallPocketed = true;
                continue;
            }

            if (AightBallPoolGameLogic.isBlackBall(ballId))
            {
                blackBallPocketed = true;
                continue;
            }

            anyObjectBallPocketed = true;
            if (AightBallPoolPlayer.PlayerHasSomeBallType(shooter, ballId))
                ownBallsPocketed.Add(ballId);
            else
                opponentBallsPocketed.Add(ballId);
        }

        // Turn rules are derived from the server's before/after board, not from a client
        // pocket callback. This covers fast/deep pots whose mechanical state reaches the
        // server but whose OnBallPocketed event is missed or arrives too late.
        if (gs.tableIsOpened)
            gs.hasRightBallInPocket = anyObjectBallPocketed;
        else if (gs.playersHasBallType)
            gs.hasRightBallInPocket = ownBallsPocketed.Count > 0;
        else
        {
            // FIX (legal pot, turn STILL shifted — the unassigned window): the break is over, so
            // tableIsOpened is already false (AightBallPoolGameLogic.OnEndShot clears it
            // unconditionally), but no group has been assigned yet — the break potted one ball of
            // EACH group, or potted nothing at all, and AssignBreakPocketedBallTypesIfUnambiguous
            // deliberately leaves that table unassigned. This branch did not exist, so in that
            // window the turn was decided from the pocket EVENT alone with NO board fallback, and a
            // single dropped keyframe cost the shooter the turn on a perfectly legal pot. The event
            // path treats every object ball as legal here (AightBallPoolGameLogic.OnBallInPocket:
            // `if (!gs.playersHasBallType) { if (!isBlackBall) hasRightBallInPocket = true; }`), so
            // the authoritative board must say exactly the same thing.
            gs.hasRightBallInPocket = anyObjectBallPocketed;
        }

        // Preserve the missing-contact fallback for delayed collision keyframes. A pot alone
        // cannot prove the first contact was legal: never overwrite an observed wrong-ball hit.
        bool boardProvesLegalFirstContact = !cueBallPocketed && anyObjectBallPocketed
            && (gs.tableIsOpened || !gs.playersHasBallType || ownBallsPocketed.Count > 0);
        if (boardProvesLegalFirstContact && !gs.cueBallHasHitSomeBall && !gs.cueBallHasHitRightBall)
        {
            gs.cueBallHasHitRightBall = true;
            Debug.LogWarning($"[8Ball][ServerPocketRule] Repaired a missing first-contact event for shooter={shooterPlayerId} from the authoritative board (own pot: [{string.Join(",", ownBallsPocketed)}], any object ball potted: {anyObjectBallPocketed}).");
        }

        if (cueBallPocketed)
        {
            gs.cueBallInPocket = true;
            gs.cueBallInHand = true;
            gs.needToChangeTurn = true;
            shooter.isCueBall = true;
        }

        string shooterType = shooter.isSolids ? "solids" : (shooter.isStripes ? "stripes" : "unassigned");
        Debug.Log($"[8Ball][ServerPocketRule] shooter={shooterPlayerId} type={shooterType}, newly=[{string.Join(",", newlyPocketed)}], own=[{string.Join(",", ownBallsPocketed)}], opponent=[{string.Join(",", opponentBallsPocketed)}], cue={cueBallPocketed}, black={blackBallPocketed}, hasRightPocket={gs.hasRightBallInPocket}.");
        if (MatchFlow.Enabled && _flowLastPotShot != activeShotSequence)
        {
            _flowLastPotShot = activeShotSequence;
            string potter = FlowWho(shooterPlayerId);
            foreach (int potted in ownBallsPocketed)
                MatchFlow.Log("8 Ball", $"{potter} potted the {potted} ({(AightBallPoolGameLogic.isSolidsBall(potted) ? "solids" : "stripes")}, own ball)");
            foreach (int potted in opponentBallsPocketed)
                MatchFlow.Log("8 Ball", $"{potter} potted the {potted} ({(AightBallPoolGameLogic.isSolidsBall(potted) ? "solids" : "stripes")}{(gs.playersHasBallType ? ", opponent's ball" : ", table open")})");
            if (blackBallPocketed)
                MatchFlow.Log("8 Ball", $"{potter} potted the 8-ball");
            if (cueBallPocketed)
                MatchFlow.Log("8 Ball", $"{potter} potted the cue ball");
        }
    }

    [Server]
    private void ApplyAuthoritativeShotEndSnapshot(int shooterPlayerId)
    {
        if (poolMessenger == null || poolMessenger.gameManager == null || poolMessenger.gameManager.allBalls == null)
            return;
        if (AightBallPoolGameLogic.gameState == null || AightBallPoolGameLogic.gameState.playersHasBallType)
            return;
        if (!activeShotAfterBreak)
            return;




        var balls = poolMessenger.gameManager.allBalls;
        int firstPocketedThisShot = -1;
        for (int i = 1; i < balls.Length; i++)
        {
            var ball = balls[i];
            if (ball == null || AightBallPoolGameLogic.isBlackBall(i))
                continue;
            // FIX (false type-assignment): deep y sentinel, not the shallow -0.1f proxy — see
            // AssignBreakPocketedBallTypesIfUnambiguous. A transient mid-shot snap must not read as a pocket.
            bool pocketedNow = ball.inPocket || ball.position.y < -0.5f;
            if (!pocketedNow)
                continue;
            bool wasPocketedBeforeShot = preShotBallPocketed != null
                && i < preShotBallPocketed.Length && preShotBallPocketed[i];
            if (wasPocketedBeforeShot)
                continue;
            firstPocketedThisShot = i;
            break;
        }

        AssignMissingBallTypesFromPocket(firstPocketedThisShot, shooterPlayerId);
    }

    [Server]
    private void AssignBreakPocketedBallTypesIfUnambiguous(int shooterPlayerId)
    {
        if (!breakShotDone)
            return;
        if (poolMessenger == null || poolMessenger.gameManager == null || poolMessenger.gameManager.allBalls == null)
            return;
        if (AightBallPoolGameLogic.gameState == null || AightBallPoolGameLogic.gameState.playersHasBallType)
            return;

        var balls = poolMessenger.gameManager.allBalls;
        int solidBall = -1;
        int stripeBall = -1;
        for (int i = 1; i < balls.Length; i++)
        {
            var ball = balls[i];
            if (ball == null || AightBallPoolGameLogic.isBlackBall(i))
                continue;
            // FIX (false type-assignment): use the authoritative pocket flag + a DEEP y sentinel,
            // not the shallow -0.1f proxy. A mid-shot snap/rattle can dip a ball's y just below
            // -0.1 for the instant this shot-end scan samples the board — that was mis-read as
            // "pocketed" and assigned ball types with NO ball actually potted. Genuine pockets are
            // kinematic (inPocket) or reconciled to y=-1, so -0.5f excludes transient snaps only.
            if (!(ball.inPocket || ball.position.y < -0.5f))
                continue;
            // Per-shot, not cumulative: this fallback used to scan the WHOLE board, so once one ball of EACH
            // group had ever been pocketed (e.g. both groups on the break, or a mixed history before types
            // were decided) it was ambiguous FOREVER and the table stayed unassigned — types (0,0) locked,
            // every legal pot reading as NoRightBallPotted. Only balls potted THIS shot may drive the
            // assignment; preShotBallPocketed is the same-indexed snapshot CaptureServerPreShotState takes
            // when the shot is accepted. Fails open (old behaviour) if no snapshot exists.
            if (preShotBallPocketed != null && i < preShotBallPocketed.Length && preShotBallPocketed[i])
                continue;

            if (AightBallPoolGameLogic.isSolidsBall(i))
                solidBall = solidBall == -1 ? i : solidBall;
            else if (AightBallPoolGameLogic.isStripesBall(i))
                stripeBall = stripeBall == -1 ? i : stripeBall;
        }

        if (solidBall != -1 && stripeBall == -1)
        {
            AssignMissingBallTypesFromPocket(solidBall, shooterPlayerId);
        }
        else if (stripeBall != -1 && solidBall == -1)
        {
            AssignMissingBallTypesFromPocket(stripeBall, shooterPlayerId);
        }
        else if (solidBall != -1 && stripeBall != -1)
        {
            Debug.Log($"[8Ball][BallTypePersist] break pocketed both groups (solid={solidBall}, stripe={stripeBall}); leaving table unassigned.");
            if (MatchFlow.Enabled && _flowLastOpenTableShot != activeShotSequence)
            {
                _flowLastOpenTableShot = activeShotSequence;
                MatchFlow.Log("8 Ball", $"both groups potted (the {solidBall} and the {stripeBall}) — table stays open");
            }
        }
    }

    [Server]
    private void AssignMissingBallTypesFromPocket(int ballId, int shooterPlayerId)
    {
        if (ballId <= 0 || AightBallPoolGameLogic.isBlackBall(ballId))
            return;
        if (AightBallPoolGameLogic.gameState == null || AightBallPoolGameLogic.gameState.playersHasBallType)
            return;
        if (shooterPlayerId <= 0)
            shooterPlayerId = activeShotPlayerId != -1 ? activeShotPlayerId : currentTurnId;
        if (shooterPlayerId <= 0)
            return;

        int shooterType = AightBallPoolGameLogic.isSolidsBall(ballId) ? 1 : 2;
        int opponentType = shooterType == 1 ? 2 : 1;

        if (int.TryParse(player1Id, out int p1Id) && int.TryParse(player2Id, out int p2Id))
        {
            int opponentPlayerId = shooterPlayerId == p1Id ? p2Id : p1Id;
            ballTypeByPlayerId[shooterPlayerId.ToString()] = shooterType;
            ballTypeByPlayerId[opponentPlayerId.ToString()] = opponentType;
        }

        ApplyServerPlayerType(AightBallPoolPlayer.mainPlayer, shooterPlayerId, shooterType, opponentType);
        ApplyServerPlayerType(AightBallPoolPlayer.otherPlayer, shooterPlayerId, shooterType, opponentType);

        AightBallPoolGameLogic.gameState.playersHasBallType = true;
        AightBallPoolGameLogic.gameState.tableIsOpened = false;
        tableOpened = false;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || !conn.isReady || conn.identity == null)
                continue;

            var player = conn.identity.GetComponent<MirrorPlayerPrefab>();
            if (player == null || player.playerData == null || string.IsNullOrEmpty(player.playerData.playerId))
                continue;

            int targetPlayerId;
            if (!int.TryParse(player.playerData.playerId, out targetPlayerId))
                continue;

            bool targetIsShooter = targetPlayerId == shooterPlayerId;
            OnMainPlayerBallTypeChanged(conn, targetIsShooter ? shooterType : opponentType,
                targetIsShooter ? opponentType : shooterType);
        }

        UpdatePlayerUI();
        MatchFlow.Log("8 Ball", $"groups set — {FlowWho(shooterPlayerId)} is {(shooterType == 1 ? "solids" : "stripes")}, {FlowWho(FlowOpponent(shooterPlayerId))} is {(shooterType == 1 ? "stripes" : "solids")} (from the {ballId})");
        Debug.Log($"[8Ball][BallTypePersist] assigned from authoritative shot-end pocket ball={ballId}, shooter={shooterPlayerId}, shooterType={(shooterType == 1 ? "solids" : "stripes")}.");
    }

    [Server]
    private void ApplyServerPlayerType(AightBallPoolPlayer player, int shooterPlayerId, int shooterType, int opponentType)
    {
        if (player == null)
            return;

        int type = player.playerId == shooterPlayerId ? shooterType : opponentType;
        if (type == 1)
            player.isSolids = true;
        else if (type == 2)
            player.isStripes = true;
        player.isBlack = false;
    }

    /// <summary>
    /// Stale-board guard for an incoming shot request. A reconnecting client scene-reloads onto the AUTHORED
    /// DEFAULT RACK (the balls are scene objects — there is no rack code), and LocalPlayerCanShoot() reads only
    /// SyncVars, which are valid the instant this object respawns. So a shot can be fired before the reconnect
    /// snapshot lands. That request carries the client's whole board in ballPositions, and ApplyShotStartSnapshot
    /// ADOPTS it — ShotController.ApplyShotStartBallPositions un-pockets every ball the request places on the
    /// table, and the result is broadcast to BOTH screens, permanently destroying a real pot (tester: "ek ball
    /// pocket hone ke bawajood frame recreate hua").
    ///
    /// The server already holds the truth: this reads the same board CaptureServerPreShotState does. Compare
    /// ONLY pocketed-vs-on-table — a binary, drift-proof disagreement — never positions, so a legitimate shot
    /// (both sides settled from the same RpcSyncBallsAfterShot) can never false-positive.
    ///
    /// The CUE BALL is deliberately skipped: on ball-in-hand the client legitimately places it back on the table
    /// while the server may still hold it pocketed (the respot can be late — see the scratch path). Its placement
    /// is validated separately by CanAcceptCueBallPlacement.
    ///
    /// Only the dangerous direction is rejected (server-pocketed -> request says on-table). The reverse (client
    /// saw a pot the server has not processed yet) is a normal race and cannot resurrect a ball, so it passes.
    /// Fails OPEN when there is no server board to compare against.
    /// </summary>
    [Server]
    private bool ServerBoardAgreesWithShotRequest(Vector3[] ballPositions)
    {
        if (ballPositions == null || ballPositions.Length == 0)
            return true;
        if (poolMessenger == null || poolMessenger.gameManager == null || poolMessenger.gameManager.allBalls == null)
            return true;

        var balls = poolMessenger.gameManager.allBalls;
        int count = Mathf.Min(balls.Length, ballPositions.Length);
        for (int i = 0; i < count; i++)
        {
            if (balls[i] == null || balls[i].id == 0)
                continue;   // cue ball: ball-in-hand legitimately puts it back on the table

            bool serverPocketed = balls[i].inPocket || balls[i].position.y < -0.1f;
            bool requestSaysOnTable = ballPositions[i].y > -0.1f;
            if (serverPocketed && requestSaysOnTable)
            {
                Debug.LogWarning($"[8Ball][ShotRequest] Stale board: ball {balls[i].id} is pocketed on the server but the request places it on the table (y={ballPositions[i].y:F3}) — the sender has not received the authoritative state yet.");
                return false;
            }
        }
        return true;
    }

    [Command(requiresAuthority = false)]
    public void StartSimulate(string impulse, Vector3 cueBallPosition, Vector3 cuePivotPosition,
        float cuePivotLocalRotationY, float cueVerticalLocalRotationX,
        Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ,
        float force, Vector3[] ballPositions, NetworkConnectionToClient sender = null)
    {
        int senderPlayerId = ResolvePlayerId(sender);




        if (IsPoolPhysicsMoving() && (sender == null || activeShotPlayerId == senderPlayerId))
        {
            RebroadcastActiveShotToObservers(sender, impulse, cueBallPosition, cuePivotPosition,
                cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY,
                cueSliderLocalPositionZ, force, ballPositions);
            return;
        }





        if (sender != null)
        {
            bool turnOk = currentTurnId != -1 && senderPlayerId == currentTurnId;
            bool phaseOk = shotPhase == (int)EightBallShotPhase.Aiming || shotPhase == (int)EightBallShotPhase.BallInHand;
            bool noActiveShot = activeShotPlayerId == -1;
            bool serverIdle = !IsPoolPhysicsMoving();
            bool boardOk = ServerBoardAgreesWithShotRequest(ballPositions);
            if (!turnOk || !canShoot || !phaseOk || !noActiveShot || !serverIdle || !boardOk)
            {
                Debug.LogWarning($"[8Ball][ShotRequest] REJECTED shot from player {senderPlayerId}: turnOk={turnOk}, canShoot={canShoot}, phaseOk={phaseOk} (phase={(EightBallShotPhase)shotPhase}), noActiveShot={noActiveShot} (active={activeShotPlayerId}), serverIdle={serverIdle}, boardOk={boardOk}, turn={currentTurnId}.");
                RejectShotRequest(sender, senderPlayerId);
                return;
            }
        }


        int shotPhaseBeforeFlowShot = shotPhase; // MatchFlow log only
        activeShotSequence = ++authoritativeShotSequence;
        if (pendingTimerRewardTurnPlayerId == currentTurnId)
            ClearPendingReconnectTimerReward("turn player started a shot");
        latestStartedShotSequence = activeShotSequence;
        activeShotPlayerId = senderPlayerId;
        shotStopConsumed = false;
        activeShotStartedAtRealtime = Time.realtimeSinceStartup;
        lastShotFrameRealtime = 0f; // reset per shot; set when the first shooter frame arrives
        lastShooterFrameTime = 0f;
        ServerSetControlState(EightBallShotPhase.Simulating, false);
        StartCoroutine(ServerAutonomousShotEndWatchdog(activeShotSequence));
        StartCoroutine(ServerStreamToWatcherOnShooterDisconnect(activeShotSequence));
        ServerResetShotRuleStateForNewShot();
        CaptureServerPreShotState();
        activeShotImpulse = impulse;
        RememberActiveShotSnapshot(cueBallPosition, cuePivotPosition, cuePivotLocalRotationY,
            cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ,
            force, ballPositions);
        if (MatchFlow.Enabled)
        {
            if (!breakShotDone)
                MatchFlow.Log("8 Ball", $"{FlowWho(senderPlayerId)} makes the break (power {force:0.00})");
            else
                MatchFlow.Log("8 Ball", $"{FlowWho(senderPlayerId)} shoots (power {force:0.00}, shot #{activeShotSequence}){(shotPhaseBeforeFlowShot == (int)EightBallShotPhase.BallInHand ? " from ball in hand" : "")}");
        }
        Debug.Log("StartSimulate CMD called with impulse: " + impulse);
        if (sender != null && sender.isReady)
            TargetRpcNoteShotSequence(sender, activeShotSequence);
        ApplyShotStartSnapshot(cueBallPosition, cuePivotPosition, cuePivotLocalRotationY,
            cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ,
            force, ballPositions);
        poolMessenger.StartSimulate(impulse);
        BroadcastStartSimulateToObservers(impulse, cueBallPosition, cuePivotPosition,
            cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY,
            cueSliderLocalPositionZ, force, ballPositions, activeShotSequence, sender, "start");
    }

    private void RebroadcastActiveShotToObservers(NetworkConnectionToClient sender, string impulse,
        Vector3 cueBallPosition, Vector3 cuePivotPosition, float cuePivotLocalRotationY, float cueVerticalLocalRotationX,
        Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force, Vector3[] ballPositions)
    {
        Debug.LogWarning("[8Ball][NetworkReplay] Duplicate StartSimulate while authoritative physics is already moving; resending impulse to observers without restarting server physics.");
        if (string.IsNullOrEmpty(activeShotImpulse))
        {
            activeShotImpulse = impulse;
            RememberActiveShotSnapshot(cueBallPosition, cuePivotPosition, cuePivotLocalRotationY,
                cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ,
                force, ballPositions);
        }
        BroadcastStartSimulateToObservers(activeShotImpulse, activeShotCueBallPosition, activeShotCuePivotPosition,
            activeShotCuePivotLocalRotationY, activeShotCueVerticalLocalRotationX,
            activeShotCueDisplacementLocalPositionXY, activeShotCueSliderLocalPositionZ,
            activeShotForce, activeShotBallPositions, activeShotSequence, sender, "duplicate");
    }

    [Server]
    private void RejectShotRequest(NetworkConnectionToClient sender, int senderPlayerId)
    {
        if (sender == null)
            return;
        TargetRpcRejectShot(sender);
        if (poolMessenger != null && poolMessenger.gameManager != null && poolMessenger.gameManager.allBalls != null)
        {
            foreach (BallPool.Mechanics.Ball ball in poolMessenger.gameManager.allBalls)
            {
                if (ball == null || ball.listener == null || ball.listener.body == null)
                    continue;
                Vector3 restorePos = ball.position;
                if (ball.id != 0 && ball.inPocket)
                    restorePos.y = -1f;
                TargetRpcSetBallPos(sender, ball.id, restorePos,
                    ball.listener.body.linearVelocity, ball.listener.body.angularVelocity, currentTurnId);
            }
            TargetRpcRefreshActiveBalls(sender);
        }
        TargetRpcRejectShotRecover(sender, currentTurnId);
        Debug.Log($"[8Ball][ShotRequest] Restored authoritative board to rejected shooter {senderPlayerId}.");
    }




    [TargetRpc]
    void TargetRpcRejectShot(NetworkConnectionToClient target)
    {
        Debug.LogWarning("[8Ball][ShotRequest] Server rejected our shot â€” aborting local sim, reverting to server board.");
        postShotControlLocked = true;
        ShotController shot = ResolveShotController();
        if (shot != null)
        {
            shot.ResetShot();
            shot.SuppressCueVisualsWhileShotSettling();
            shot.ActivateControl(false);
        }
    }

    [TargetRpc]
    void TargetRpcRejectShotRecover(NetworkConnectionToClient target, int turnPlayerId)
    {
        postShotControlLocked = false;
        if (staticVariables.UserProfiledata != null
            && staticVariables.UserProfiledata.user != null
            && turnPlayerId == staticVariables.UserProfiledata.user._id
            && BallPool.BallPoolGameManager.instance != null)
        {
            BallPool.BallPoolGameManager.instance.ResumeTurnTimerAfterRejectedShot();
        }
        RefreshLocalTurnControl(turnPlayerId, "shot rejected restore");
    }

    private void BroadcastStartSimulateToObservers(string impulse, Vector3 cueBallPosition, Vector3 cuePivotPosition,
        float cuePivotLocalRotationY, float cueVerticalLocalRotationX,
        Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ,
        float force, Vector3[] ballPositions, int shotSequence, NetworkConnectionToClient sender, string reason)
    {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn == sender)
                continue;

            if (!conn.isReady)
            {
                Debug.LogWarning($"[8Ball][NetworkReplay] Skipped StartSimulate {reason} resend to conn={conn.connectionId}; connection is not ready.");
                continue;
            }

            TargetRpcStartSimulate(conn, impulse, cueBallPosition, cuePivotPosition,
                cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY,
                cueSliderLocalPositionZ, force, ballPositions, shotSequence);
        }
    }

    [TargetRpc]
    public void TargetRpcNoteShotSequence(NetworkConnectionToClient target, int shotSequence)
    {
        if (shotSequence > latestStartedShotSequence)
            latestStartedShotSequence = shotSequence;
    }

    [TargetRpc]
    public void TargetRpcStartSimulate(NetworkConnectionToClient target, string impulse,
        Vector3 cueBallPosition, Vector3 cuePivotPosition,
        float cuePivotLocalRotationY, float cueVerticalLocalRotationX,
        Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ,
        float force, Vector3[] ballPositions, int shotSequence)
    {
        if (IsIncomingShotStartStale(shotSequence))
            return;
        if (shotSequence > latestStartedShotSequence)
            latestStartedShotSequence = shotSequence;
        if (IsPoolPhysicsMoving())
        {
            Debug.LogWarning("[8Ball][NetworkReplay] Ignored incoming StartSimulate while local physics is already moving.");
            return;
        }
        ApplyShotStartSnapshot(cueBallPosition, cuePivotPosition, cuePivotLocalRotationY,
            cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ,
            force, ballPositions);
        poolMessenger.StartSimulate(impulse);
    }

    private void RememberActiveShotSnapshot(Vector3 cueBallPosition, Vector3 cuePivotPosition,
        float cuePivotLocalRotationY, float cueVerticalLocalRotationX,
        Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ,
        float force, Vector3[] ballPositions)
    {
        activeShotCueBallPosition = cueBallPosition;
        activeShotCuePivotPosition = cuePivotPosition;
        activeShotCuePivotLocalRotationY = cuePivotLocalRotationY;
        activeShotCueVerticalLocalRotationX = cueVerticalLocalRotationX;
        activeShotCueDisplacementLocalPositionXY = cueDisplacementLocalPositionXY;
        activeShotCueSliderLocalPositionZ = cueSliderLocalPositionZ;
        activeShotForce = force;
        activeShotBallPositions = ballPositions != null ? (Vector3[])ballPositions.Clone() : new Vector3[0];
    }

    private void ApplyShotStartSnapshot(Vector3 cueBallPosition, Vector3 cuePivotPosition,
        float cuePivotLocalRotationY, float cueVerticalLocalRotationX,
        Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ,
        float force, Vector3[] ballPositions)
    {
        if (poolMessenger == null || poolMessenger.shotController == null)
            return;

        poolMessenger.shotController.ApplyShotStartSnapshot(cueBallPosition, cuePivotPosition,
            cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY,
            cueSliderLocalPositionZ, force, ballPositions);
    }

    private bool IsPoolPhysicsMoving()
    {
        return poolMessenger != null
            && poolMessenger.shotController != null
            && poolMessenger.shotController.cuePhysicsManager != null
            && poolMessenger.shotController.cuePhysicsManager.IsInMove;
    }

    private bool IsLocalShotReplayMoving()
    {
        if (poolMessenger == null || poolMessenger.shotController == null)
            return false;

        ShotController shot = poolMessenger.shotController;
        return shot.isMoving
            || (shot.cuePhysicsManager != null && shot.cuePhysicsManager.IsInMove);
    }

    private bool IsSelfHealedWatcher()
    {
        return false;
    }

    [Command(requiresAuthority = false)]
    public void OnSendTime(float time01, NetworkConnectionToClient sender = null)
    {
        int senderPlayerId = ResolvePlayerId(sender);
        if (sender != null && (senderPlayerId <= 0 || senderPlayerId != currentTurnId))
        {
            if (time01 >= 0.99f)
                Debug.LogWarning($"[8Ball][TurnTimer] Ignored stale timer expiry from player {senderPlayerId}; current turn={currentTurnId}.");
            return;
        }

        bool shotInProgress = activeShotPlayerId != -1
            || IsPoolPhysicsMoving()
            || shotPhase == (int)EightBallShotPhase.Simulating;
        if (shotInProgress)
        {
            if (time01 >= 0.99f)
                Debug.LogWarning($"[8Ball][TurnTimer] Ignored late timer expiry while shot {activeShotSequence} is active for {activeShotPlayerId}.");
            return;
        }

        time01 = Mathf.Clamp01(time01);
        if (time01 + 0.001f < lastTurnPlayTime)
            return;

        lastTurnPlayTime = time01;
        poolMessenger.SetTime(time01);
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == sender)
            {

            }
            else
            {

                TargetRpcSendTimee(conn, time01);
            }
        }
    }
    [TargetRpc]
    public void TargetRpcSendTimee(NetworkConnectionToClient target, float time01)
    {
        poolMessenger.SetTime(time01);
    }






    [TargetRpc]
    public void TargetRpcRestorePlayTime(NetworkConnectionToClient target, float time01)
    {
        StartCoroutine(RestorePlayTimeRoutine(time01));
    }

    private System.Collections.IEnumerator RestorePlayTimeRoutine(float time01)
    {
        if (time01 <= 0f || time01 >= 1f)
            yield break;
        float window = 2f;
        while (window > 0f)
        {
            var gm = BallPool.BallPoolGameManager.instance;
            if (gm == null)
                yield break;
            if (gm.playTime < time01 - 0.01f)
                gm.SetPlayTime(time01);
            window -= Time.deltaTime;
            yield return null;
        }
    }
    [Command(requiresAuthority = false)]
    public void SendOpponentCueURL(string url)
    {
        poolMessenger.SetOpponentCueURL(url);
    }
    [Command(requiresAuthority = false)]
    public void SendOpponentTableURLs(string boardURL, string clothURL, string clothColor)
    {
        poolMessenger.SetOpponentTableURLs(boardURL, clothURL, clothColor);
    }


    [Command(requiresAuthority = false)]
    public void EndSimulate(string ballsState)
    {
        poolMessenger.EndSimulate(ballsState);
    }

    [Command(requiresAuthority = false)]
    public void OnOpponenInGameScene()
    {
        StartCoroutine(poolMessenger.OnOpponenInGameScene());
    }
    [Command(requiresAuthority = false)]
    public void OnOpponentForceGoHome()
    {
        poolMessenger.OnOpponentForceGoHome();
    }
    [Command(requiresAuthority = false)]
    public void CmdPlayerFinished()
    {
        MatchFlow.Log("8 Ball", "player finished — match closing");

        RpcActivateUIAndHandleResult();
        Debug.Log("CmdPlayerFinished");
        this.Delay(1, () =>
        {
            MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
        });
    }

    [ClientRpc]
    void RpcActivateUIAndHandleResult()
    {
        Debug.Log("RpcActivateUIAndHandleResult");


        GameUIController.EightballPoolLeaveGame = true;

    }
    [Command(requiresAuthority = false)]
    public void CmdGameLeave()
    {

        MatchFlow.Log("8 Ball", "a player left the match (home / leave button)");
        RpcGameLeave();
        Debug.Log("CmdGameLeave");

    }

    [ClientRpc]
    void RpcGameLeave()
    {

        GameUIController.EightballPoolLeaveGame = true;
        Debug.Log("RpcGameLeave"+ GameUIController.EightballPoolLeaveGame);

    }
    [Command(requiresAuthority = false)]
    public void CmdGameWin(int PlayerId)
    {
        if (MatchFlow.Enabled && !isGameOver && _flowWinReason == null)
        {
            bool opponentConnected = IsPlayerConnected(FlowOpponent(PlayerId));
            _flowWinReason = opponentConnected ? "game complete (client report)" : "opponent left / disconnected";
            MatchFlow.Log("8 Ball", $"{FlowWho(PlayerId)} claims the win ({_flowWinReason})");
            // The server decides a finished frame itself (end of the last shot). A win claimed while the opponent is
            // still connected and the server has not ended the frame is not backed by the table.
            if (opponentConnected)
                MatchFlow.Flag("8 Ball", PlayerId.ToString(), "unbacked_win_claim", "claimed the win while the opponent was connected and the server had not ended the frame");
        }

        ServerDeclareGameWin(PlayerId);
    }

    [Server]
    public void ServerDeclareGameWin(int winnerId)
    {



        if (isGameOver) return;


        isGameOver = true;
        gameWinnerId = winnerId.ToString();
        MatchFlow.Log("8 Ball", $"game over — {FlowWho(winnerId)} wins");
        MatchFlow.SendResult(gameWinnerId, _flowWinReason ?? "game over");

        // Two-step reveal, both driven from here: caption now, panel ResultPanelRevealDelay later.
        RpcShowWinLoseText(gameWinnerId);
        StartCoroutine(ServerRevealResultPanelAfterDelay());
        Debug.Log($"[8Ball][ServerWin] Server declared winner {winnerId}; panel reveal in {ResultPanelRevealDelay:F0}s.");
        this.Delay(ServerCleanupDelay, () =>
        {
            MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
        });
    }

    [Server]
    private System.Collections.IEnumerator ServerRevealResultPanelAfterDelay()
    {
        yield return new WaitForSecondsRealtime(ResultPanelRevealDelay);
        // Order matters: set the SyncVar FIRST so a client that is mid-reconnect picks the reveal up
        // from state even though the broadcast below will never reach it.
        resultPanelDue = true;
        RpcOpenResultPanel(gameWinnerId);
        Debug.Log("[8Ball][ServerWin] Result panel reveal broadcast.");
    }

    [Server]
    public void ServerPublishShotOutcome(int shooterPlayerId, int outcomeCode, bool needToChangeTurn, bool gameIsEnd, int winnerId)
    {
        var gs = AightBallPoolGameLogic.gameState;
        EnsureShotOutcomeHasBallTypes(shooterPlayerId);
        gs = AightBallPoolGameLogic.gameState;
        bool cueInHand = gs != null && gs.cueBallInHand;
        bool cueInPocket = gs != null && gs.cueBallInPocket;

        bool needChange = needToChangeTurn;

        int p1Id = -1, p1Type = 0; bool p1Black = false;
        int p2Id = -1, p2Type = 0; bool p2Black = false;
        if (BallPoolPlayer.players != null && BallPoolPlayer.players.Length >= 2)
        {
            var p1 = BallPoolPlayer.players[0] as AightBallPoolPlayer;
            var p2 = BallPoolPlayer.players[1] as AightBallPoolPlayer;
            if (p1 != null) { p1Id = p1.playerId; p1Type = p1.isSolids ? 1 : (p1.isStripes ? 2 : 0); p1Black = p1.isBlack; }
            if (p2 != null) { p2Id = p2.playerId; p2Type = p2.isSolids ? 1 : (p2.isStripes ? 2 : 0); p2Black = p2.isBlack; }
        }

        Debug.Log($"[8Ball][ShotOutcome] server publish shot {activeShotSequence}: code={(EightBallOutcomeCode)outcomeCode}, shooter={shooterPlayerId}, turnAfter={currentTurnId}, needChange={needChange}, cueInHand={cueInHand}, gameEnd={gameIsEnd}, winner={winnerId}.");
        if (MatchFlow.Enabled && _flowLastOutcomeShot != activeShotSequence)
        {
            _flowLastOutcomeShot = activeShotSequence;
            string shooterName = FlowWho(shooterPlayerId);
            string opponentName = FlowWho(FlowOpponent(shooterPlayerId));
            switch ((EightBallOutcomeCode)outcomeCode)
            {
                case EightBallOutcomeCode.Scratch:
                    MatchFlow.Log("8 Ball", $"foul — {shooterName} potted the cue ball (scratch)");
                    break;
                case EightBallOutcomeCode.WeakBreak:
                    MatchFlow.Log("8 Ball", $"foul — weak break by {shooterName} (not enough balls hit a rail)");
                    break;
                case EightBallOutcomeCode.WrongBallHit:
                    MatchFlow.Log("8 Ball", $"foul — {shooterName} hit the wrong ball first (or nothing)");
                    break;
                case EightBallOutcomeCode.NoRightBallPotted:
                    MatchFlow.Log("8 Ball", $"{shooterName} potted nothing of theirs");
                    break;
                case EightBallOutcomeCode.LegalPotContinue:
                    MatchFlow.Log("8 Ball", $"{shooterName} potted legally");
                    break;
                case EightBallOutcomeCode.LegalBlackWin:
                    MatchFlow.Log("8 Ball", $"{shooterName} potted the 8-ball legally — {FlowWho(winnerId)} wins");
                    _flowWinReason = "8-ball potted legally";
                    break;
                case EightBallOutcomeCode.IllegalBlackLoss:
                    MatchFlow.Log("8 Ball", $"{shooterName} potted the 8-ball illegally — loses, {FlowWho(winnerId)} wins");
                    _flowWinReason = "8-ball foul by " + shooterName;
                    break;
            }
            if (!gameIsEnd)
            {
                if (needChange && cueInHand)
                    MatchFlow.Log("8 Ball", $"ball in hand for {opponentName}");
                else if (!needChange)
                    MatchFlow.Log("8 Ball", $"turn continues — {shooterName} shoots again");
            }
        }
        RpcApplyShotOutcome(activeShotSequence, shooterPlayerId, currentTurnId, needChange, cueInHand, cueInPocket,
            p1Id, p1Type, p1Black, p2Id, p2Type, p2Black, gameIsEnd, winnerId, outcomeCode);
    }

    [Server]
    private void EnsureShotOutcomeHasBallTypes(int shooterPlayerId)
    {
        var gs = AightBallPoolGameLogic.gameState;
        if (gs == null || gs.playersHasBallType || !gs.hasRightBallInPocket)
            return;

        int resolvedShooter = shooterPlayerId > 0
            ? shooterPlayerId
            : (activeShotPlayerId != -1 ? activeShotPlayerId : currentTurnId);
        if (resolvedShooter <= 0)
            return;

        AssignBreakPocketedBallTypesIfUnambiguous(resolvedShooter);
        ApplyAuthoritativeShotEndSnapshot(resolvedShooter);
    }

    [ClientRpc]
    void RpcApplyShotOutcome(int shotSequence, int shooterPlayerId, int currentTurnIdAfter,
        bool needToChangeTurn, bool cueBallInHand, bool cueBallInPocket,
        int player1Id, int player1Type, bool player1IsBlack,
        int player2Id, int player2Type, bool player2IsBlack,
        bool gameIsEnd, int winnerId, int outcomeCode)
    {

        if (NetworkServer.active) return;
        if (PlayerPrefs.GetInt("EightballMultiplayer") != 1) return;
        if (shotSequence > 0 && latestStartedShotSequence > 0 && shotSequence < latestStartedShotSequence)
        {
            Debug.LogWarning($"[8Ball][ShotOutcome] Dropped stale outcome. shot={shotSequence}, latest={latestStartedShotSequence}.");
            return;
        }

        var gs = AightBallPoolGameLogic.gameState;

        ApplyOutcomePlayerState(player1Id, player1Type, player1IsBlack);
        ApplyOutcomePlayerState(player2Id, player2Type, player2IsBlack);

        bool typesAssigned = player1Type != 0 || player2Type != 0;
        if (gs != null)
        {
            if (typesAssigned)
            {
                gs.playersHasBallType = true;
                gs.tableIsOpened = false;
            }
            gs.cueBallInHand = cueBallInHand;
            gs.cueBallInPocket = cueBallInPocket;
            gs.needToChangeTurn = needToChangeTurn;
        }

        if (poolMessenger != null && poolMessenger.gameManager != null)
            poolMessenger.gameManager.RefreshActiveBallsUI();

        RenderOutcomeText((EightBallOutcomeCode)outcomeCode, shooterPlayerId, winnerId);

        if (typesAssigned)
            TryShowLocalBallTypeAssignedText();

        Debug.Log($"[8Ball][ShotOutcome] client applied shot {shotSequence}: code={(EightBallOutcomeCode)outcomeCode}, shooter={shooterPlayerId}, turnAfter={currentTurnIdAfter}, needChange={needToChangeTurn}, cueInHand={cueBallInHand}, types(p1={player1Type},p2={player2Type}), gameEnd={gameIsEnd}, winner={winnerId}. (does NOT open stick)");

    }


    void ApplyOutcomePlayerState(int playerId, int type, bool isBlack)
    {
        if (playerId < 0 || BallPoolPlayer.players == null) return;
        foreach (var bp in BallPoolPlayer.players)
        {
            if (bp == null || bp.playerId != playerId) continue;
            var ap = bp as AightBallPoolPlayer;
            if (ap == null) continue;
            if (type == 1) ap.isSolids = true;
            else if (type == 2) ap.isStripes = true;
            ap.isBlack = isBlack;
            break;
        }
    }

    private bool localBallTypeAssignedTextShown = false;
    void TryShowLocalBallTypeAssignedText()
    {
        if (localBallTypeAssignedTextShown) return;
        // Reconnect restore replays OnMainPlayerBallTypeChanged (BallTypeRestore step), and this object is
        // freshly respawned so the one-shot flag above has reset — the "You Are solids/stripes" banner then
        // popped mid-table on every reconnect (tester issue 5). During the restore window
        // (Begin..Complete, reconnectStateSyncCompleteReceived=false) consume the one-shot WITHOUT showing:
        // the type was announced when it was first assigned, a restore is not news.
        if (!reconnectStateSyncCompleteReceived)
        {
            localBallTypeAssignedTextShown = true;
            return;
        }
        if (GameManager.instance == null || GameManager.instance.selectedBallText == null || BallPoolPlayer.players == null) return;
        int localId = (staticVariables.UserProfiledata != null && staticVariables.UserProfiledata.user != null)
            ? staticVariables.UserProfiledata.user._id : -1;
        if (localId < 0) return;
        AightBallPoolPlayer local = null;
        foreach (var bp in BallPoolPlayer.players)
            if (bp != null && bp.playerId == localId) { local = bp as AightBallPoolPlayer; break; }
        if (local == null) return;
        string info = local.isStripes ? "You Are Stripes" : (local.isSolids ? "You Are solids" : "");
        if (string.IsNullOrEmpty(info)) return;

        var txt = GameManager.instance.selectedBallText;
        txt.DOKill();
        var col = txt.color;
        col.a = 1f;
        txt.color = col;
        txt.text = info;
        float textFadeDuration = 1f, textFadeDelay = 0.05f;
        txt.rectTransform.DOAnchorPos(new Vector3(0, 100f, 0), 2.5f, true).SetEase(Ease.OutQuad)
            .OnComplete(() => txt.DOFade(0f, textFadeDuration).SetDelay(textFadeDelay).OnComplete(() => { txt.text = " "; }));
        localBallTypeAssignedTextShown = true;
    }



    void RenderOutcomeText(EightBallOutcomeCode code, int shooterPlayerId, int winnerId)
    {
        if (BallPoolGameManager.instance == null) return;
        int localId = (staticVariables.UserProfiledata != null && staticVariables.UserProfiledata.user != null)
            ? staticVariables.UserProfiledata.user._id : -1;
        bool iAmShooter = shooterPlayerId == localId;
        string opp = GameManager.newOpponentName;
        string info = "";
        switch (code)
        {
            case EightBallOutcomeCode.Scratch:
                info = iAmShooter ? "You pocketed the cue ball\n" + opp + " has cue ball in hand"
                                  : opp + " pocketed the cue ball\nYou have cue ball in hand";
                break;
            case EightBallOutcomeCode.WeakBreak:
                info = iAmShooter ? "Break was too weak\n" + opp + " has cue ball in hand"
                                  : "Break was too weak\nYou have cue ball in hand";
                break;
            case EightBallOutcomeCode.WrongBallHit:
                info = iAmShooter ? "Wrong ball hit, foul\n" + opp + " has cue ball in hand"
                                  : opp + " hit the wrong ball\nYou have cue ball in hand";
                break;
            case EightBallOutcomeCode.NoRightBallPotted:
                info = iAmShooter ? "No ball potted, turn passes" : "Your turn";
                break;
            case EightBallOutcomeCode.TurnChanged:
                info = iAmShooter ? opp + "'s turn" : "Your turn";
                break;
            case EightBallOutcomeCode.LegalBlackWin:
            case EightBallOutcomeCode.IllegalBlackLoss:
                info = (winnerId == localId) ? "You win!" : opp + " wins";
                break;
            case EightBallOutcomeCode.LegalPotContinue:
            case EightBallOutcomeCode.None:
            default:
                info = "";
                break;
        }
        if (!string.IsNullOrEmpty(info))
            BallPoolGameManager.instance.SetGameInfo(info);
    }

    // Last-resort net: isGameOver is a SyncVar, so Mirror delivers it to every client even when both
    // the caption and the reveal broadcasts were lost to a reconnect/scene-load window. Waits past
    // the server's own reveal, then opens from synced state if nothing else did.
    private void OnGameOverChanged(bool oldValue, bool newValue)
    {
        if (!newValue || NetworkServer.active) return;
        ShowLocalWinLoseText(gameWinnerId);
        StartCoroutine(EnsureWinLoseShown());
    }

    private System.Collections.IEnumerator EnsureWinLoseShown()
    {
        float timeout = ResultPanelRevealDelay + 4f;
        while (timeout > 0f)
        {
            if (ResultManager.GameSpawnedFinished) yield break;
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.LogWarning("[8Ball] Result panel still missing after game over — isGameOver fallback opening it.");
        OpenResultPanel(gameWinnerId, "isGameOver SyncVar fallback");
    }

    [ClientRpc]
    void RpcShowWinLoseText(string winnerId)
    {
        ShowLocalWinLoseText(winnerId);
    }

    [ClientRpc]
    void RpcOpenResultPanel(string winnerId)
    {
        OpenResultPanel(winnerId, "server reveal");
    }

    /// <summary>
    /// "You Win" / "You Lose" caption. Safe to call more than once.
    /// </summary>
    /// <remarks>
    /// DIVERGENCE FROM THE CLIENT REPO — do not "fix" this by copying the client version.
    /// The client's GameManager owns the caption UI (`winLoseTxt` + `ShowWinPanel`); this project's
    /// GameManager has neither field, so referencing them here does not compile. That is fine: on a
    /// dedicated server this method is unreachable anyway. Its only callers are the two ClientRpc
    /// bodies (never executed server-side), the OnStartClient reconnect block (no local client), and
    /// OnGameOverChanged, which returns early when NetworkServer.active. The caption is drawn purely
    /// by the client build; the server just needs this to compile and to stop the waiting timer.
    /// </remarks>
    private void ShowLocalWinLoseText(string winnerId)
    {
        if (PopupMessageManager.instance != null)
            PopupMessageManager.instance.waitingPanel?.StopTimer();
    }

    /// <summary>
    /// The single place the result panel is opened, from every trigger (server reveal, the
    /// resultPanelDue SyncVar, reconnect, fallback).
    /// </summary>
    /// <remarks>
    /// GameSpawnedFinished is latched HERE and nowhere earlier, because it means "the panel exists",
    /// not "the panel was requested". It used to be set the moment the caption appeared, several
    /// seconds before the panel was instantiated — and since it is a static that no scene reload
    /// clears (only ResultManager.OnDisable does, and no ResultManager had been created yet), a
    /// client that dropped inside that window came back with the flag stranded at true. Every
    /// reconnect safety net then read "already shown" and skipped, so the player sat on a finished
    /// table with no result at all.
    ///
    /// The `true` first argument is NOT "did I win" — it is the gate that lets ResultManager process
    /// the result at all (ResultManager.cs:181). The real win/lose decision happens INSIDE it, from
    /// `PlayerId == staticVariables.UserProfiledata.user._id`. It must always be true; the winner id
    /// is the second argument.
    /// </remarks>
    private void OpenResultPanel(string winnerId, string reason)
    {
        if (ResultManager.GameSpawnedFinished) return;
        if (string.IsNullOrEmpty(winnerId)) return;

        GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
        if (prefab == null)
        {
            Debug.LogError("[8Ball] WinLoseGameManager prefab not found!");
            return;
        }

        ResultManager.GameSpawnedFinished = true;
        if (PopupMessageManager.instance != null)
            PopupMessageManager.instance.waitingPanel?.StopTimer();
        Debug.Log($"[8Ball][ServerWin] Opening result panel ({reason}); winner={winnerId}.");
        Instantiate(prefab, Vector3.zero, Quaternion.identity)
            .GetComponent<ResultManager>()
            .HandleGameResultAltMultiplayer(true, winnerId);
    }

    private void OnResultPanelDueChanged(bool oldValue, bool newValue)
    {
        if (!newValue || NetworkServer.active) return;
        OpenResultPanel(gameWinnerId, "resultPanelDue SyncVar");
    }
    private void OnDisable()
    {
        MirrorNetwork.OnWinCall -= AnnounceWinner;
        if (reconnectOverlayHideRoutine != null)
        {
            StopCoroutine(reconnectOverlayHideRoutine);
            reconnectOverlayHideRoutine = null;
        }

        reconnectBallsSyncInProgress = false;
        reconnectStateSyncCompleteReceived = true;
    }

    private void OnEnable()
    {
        MirrorNetwork.OnWinCall += AnnounceWinner;

    }










    private void AnnounceWinner(string reason)
    {
        CmdGameWin(staticVariables.UserProfiledata.user._id);
    }





    public void DirectResult(bool result)
    {
        if (ResultManager.GameSpawnedFinished) return;
        ResultManager.GameSpawnedFinished = true;
        GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
        if (prefab != null)
        {
            Instantiate(prefab, Vector3.zero, Quaternion.identity)
                .GetComponent<ResultManager>()
                .HandleGameResultAltMultiplayer(result, staticVariables.UserProfiledata.user._id.ToString());
        }
        else
        {
            Debug.LogError("WinLose GameManager prefab not found!");
        }
    }
}
