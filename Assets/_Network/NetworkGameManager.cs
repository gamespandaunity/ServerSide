using System.Collections.Generic;
using BEKStudio;
using CarRace;
using Mirror;
using NetworkManagement;
using System;
using TeenPattiGame;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityExtensions;
using UnityEngine.UI;
using System.Collections;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Game Manager Settings")]
    public static NetworkGameManager Instance;

    [Header("Player Tracking")]
    [SyncVar(hook = nameof(OnChangePlayer))] public int currentPlayerCount = 0;
    [SyncVar] public string currentGameScene = "";
    [SyncVar] public bool isCreator;
    [SyncVar(hook = nameof(OnSetTransactionId))] public string transactionId;
    [SyncVar] public string currentServerRequestId;

    [Header("Game Configuration")]
    public int maxPlayers = 2;
    public int minPlayersToStart = 2;
    [SyncVar] public int Prize;
   
    [SyncVar] public bool IsGoldCoin;
    [SyncVar(hook = nameof(OnPauseStateChanged))] public bool IsPaused;
    [SyncVar] public int currentGameId;
    public string requestId;

    // Set from Edgegap env var "Creator" at server start — authoritative creator user ID
    [HideInInspector] public string creatorUserId = "";

    // ─── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            NetworkServer.Destroy(gameObject);
        }
    }
   
    public override void OnStartServer()
    {
        base.OnStartServer();

        string envGameId = Environment.GetEnvironmentVariable("GameId");
        string envTransaction = Environment.GetEnvironmentVariable("TransactionId");
        string envRequestId = Environment.GetEnvironmentVariable("ServerRequestId");
        // The backend no longer sets ServerRequestId; Edgegap always sets ARBITRIUM_REQUEST_ID for the deployment.
        if (string.IsNullOrEmpty(envRequestId)) envRequestId = MatchServerApi.RequestId;
        string envPrize = Environment.GetEnvironmentVariable("currentPrize");
        string envGold = Environment.GetEnvironmentVariable("isgoldcoins");
        string envCreator = Environment.GetEnvironmentVariable("Creator");

        if (!string.IsNullOrEmpty(envGameId) && int.TryParse(envGameId, out int gId)) currentGameId = gId;
        if (!string.IsNullOrEmpty(envTransaction)) transactionId = envTransaction;
        if (!string.IsNullOrEmpty(envRequestId)) { currentServerRequestId = envRequestId; if (MirrorNetwork.Instance) MirrorNetwork.Instance.mirrorRequestId = envRequestId; }
        if (!string.IsNullOrEmpty(envPrize) && int.TryParse(envPrize, out int prize)) Prize = prize;
        if (!string.IsNullOrEmpty(envGold) && bool.TryParse(envGold, out bool gold)) IsGoldCoin = gold;
        if (!string.IsNullOrEmpty(envCreator)) creatorUserId = envCreator;

        // Case A: manual local test — JSON pre-written before server boots → load it now.
        // Case B: LocalTestManager — JSON written after Deploy() fires, file missing here → no-op,
        //         LoadLocalConfig() is called again lazily from CmdSetPlayerData() on first connect.
        LoadLocalConfig();
        ApiAndRoomManager._instance.winLoseChallengeId = transactionId;
        Debug.Log($"[Server] OnStartServer — game={currentGameId} tx={transactionId} req={currentServerRequestId} prize={Prize} gold={IsGoldCoin} creator={(string.IsNullOrEmpty(creatorUserId) ? "pending (will load on first connect)" : creatorUserId)}");
    }

    /// <summary>
    /// Reads C:/Temp/server_config.json written by EdgegapAPIClient.WriteLocalServerConfig().
    /// Only fills fields that are still empty — safe to call multiple times.
    /// </summary>
    public void LoadLocalConfig()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "server_config.json");
        if (!System.IO.File.Exists(path)) return;

        try
        {
            string json = System.IO.File.ReadAllText(path);
            var cfg = JsonUtility.FromJson<LocalServerConfig>(json);

            if (currentGameId == 0 && int.TryParse(cfg.GameId, out int gId)) currentGameId = gId;
            if (string.IsNullOrEmpty(transactionId) && !string.IsNullOrEmpty(cfg.TransactionId)) transactionId = cfg.TransactionId;
            if (string.IsNullOrEmpty(currentServerRequestId) && !string.IsNullOrEmpty(cfg.ServerRequestId)) { currentServerRequestId = cfg.ServerRequestId; if (MirrorNetwork.Instance) MirrorNetwork.Instance.mirrorRequestId = cfg.ServerRequestId; }
            if (Prize == 0 && int.TryParse(cfg.currentPrize, out int prize)) Prize = prize;
            if (!IsGoldCoin && bool.TryParse(cfg.isgoldcoins, out bool gold)) IsGoldCoin = gold;
            if (string.IsNullOrEmpty(creatorUserId) && !string.IsNullOrEmpty(cfg.Creator)) creatorUserId = cfg.Creator;

            Debug.Log($"[Server] Local config loaded — game={currentGameId} tx={transactionId} creator={creatorUserId}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Server] Failed to read local config: {e.Message}");
        }
    }

    [Serializable]
    private class LocalServerConfig
    {
        public string GameId;
        public string TransactionId;
        public string ServerRequestId;
        public string currentPrize;
        public string isgoldcoins;
        public string Creator;
    }

    // ─── Player Count ──────────────────────────────────────────────────────────

    public void OnChangePlayer(int oldVal, int newVal)
    {
      // Reconnect-corruption guard (SyncVar hook): side-effects here (OnGameStateChanged subscribers
      // such as UpdatePlayerTimer.restartTimer) can throw a NullReferenceException during a reconnect
      // deserialize when the scene objects aren't ready. An uncaught throw ABORTS the SyncVar batch ->
      // wrong seat/turn state on the reconnecting client. Catch so the deserialize always completes.
      try {
        "Player Count Changed".Show($"{oldVal} -> {newVal}");
        ResultManager.isGameFinished.Show("Is Game Finished On Player Count Change");
        Debug.Log("Scene : " + SceneManager.GetActiveScene().name);

        // More than 2 seats: a dropped seat gets its own badge (LudoNetworkManager.seatList); the table never pauses.
        if (MultiSeatTable.IsMultiSeat) return;

        if (newVal == 2)
        {
            // Don't resume a match that has already been decided (e.g. opponent
            // reconnects after the 30s waiting timeout / disconnect-win was declared).
            if (ResultManager.isGameFinished) return;
            MirrorNetwork.OnGameStateChanged?.Invoke(false);
            CmdSetPauseState(false);
            return;
        }

        if (PopupMessageManager.instance == null) return;

        if (ShouldPauseGame())
        {
            "Pause Game".Show();
            CmdSetPauseState(true);
            PopupMessageManager.instance.waitingPanel.StartTimer();
        }

        MirrorNetwork.OnGameStateChanged?.Invoke(true);
      } catch (System.Exception e) { Debug.LogWarning("[NetworkGameManager] OnChangePlayer hook error (SyncVar deserialize protected): " + e); }
    }

    private bool ShouldPauseGame()
    {
        string scene = SceneManager.GetActiveScene().name;

        if (SnokerNetwork.Instance && SnokerNetwork.isTossDone) return !ResultManager.isGameFinished;
        if (scene == "GameplayTeenPatti") return true;
        if (scene == "LudoGameScene") return !ResultManager.isGameFinished;
        if (scene == "SnakeMultiplayer") return !ResultManager.isGameFinished;
        if (scene == "12OnlineGameScene") return !ResultManager.isGameFinished;
        if (scene == "Map2") return !ResultManager.isGameFinished;
        if (scene == "PokerGameplay") return !staticVariables.isPokerFinished;
        if (scene == "AightBallPool") return !ResultManager.isGameFinished;
        if (scene == "HighwayNight") return !ResultManager.isGameFinished;
        if (scene == "HorseRacingMultiPlayer") return !ResultManager.isGameFinished;
        if (CarromNetworkManager.instance && !ResultManager.isGameFinished) return true;
        if (scene == "Ground") return !ResultManager.isGameFinished;

        return false;
    }

    // ─── Pause ─────────────────────────────────────────────────────────────────

    [Command(requiresAuthority = false)]
    public void CmdSetPauseState(bool pause)
    {
        MirrorNetwork.OnGameStateChanged?.Invoke(pause);
        IsPaused = pause;
        "Pause State Changed".Show(IsPaused.ToString());
    }

    void OnPauseStateChanged(bool oldValue, bool newValue)
    {
        // Reconnect-corruption guard: this is a SyncVar HOOK invoked from DeserializeSyncVars. Its
        // side-effects (ShowPausePanel -> static StartStopGame?.Invoke) can reach a LEAKED subscriber
        // (e.g. Car's RCC_MirrorNetwork.StartStopGame, which subscribes to the static action and never
        // unsubscribes) whose GetComponent throws a NullReferenceException in a non-Car scene like Ludo.
        // An exception here ABORTS the whole SyncVar batch -> the reconnecting client never applies the
        // authoritative seat/turn/player state -> Ludo beads render on the wrong side with swapped colors.
        // Catch so the deserialize always completes and the correct state applies.
        try
        {
            Debug.Log($"[MirrorPlayer] Pause state changed: {oldValue} -> {newValue}");
            if (newValue) OnPlayerBackground();
            else OnPlayerReturnedFromBackground();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[NetworkGameManager] OnPauseStateChanged hook error (SyncVar deserialize protected): " + e);
        }
    }

    // ─── Transaction / Request ID ──────────────────────────────────────────────

    [Command(requiresAuthority = false)]
    public void CmdSetTransactionId(string newId)
    {
        // A different transaction id is a different match, including a rematch. Clear the previous
        // match's settlement flags before adopting it.
        if (transactionId != newId) ServerResetMatchSettlementState($"new transaction '{newId}'");
        transactionId = newId;
        ApiAndRoomManager._instance.winLoseChallengeId = newId; // SyncVar hook doesn't fire on server
    }

    public void OnSetTransactionId(string oldValue, string newValue)
    {
        // Wrapped (board-flip / bead-color-swap on reconnect fix, see client NGM): a throw inside ANY
        // SyncVar hook aborts the WHOLE DeserializeSyncVars batch on reconnect → seat/turn never applies
        // → board renders against the stale seat (Ludo: board flips + colors swap). _instance can be null
        // mid scene-transition. Round-9 protected OnChangePlayer + OnPauseStateChanged but missed this one.
        try { if (ApiAndRoomManager._instance != null) ApiAndRoomManager._instance.winLoseChallengeId = newValue; }
        catch (System.Exception e) { Debug.LogWarning("[NetworkGameManager] OnSetTransactionId hook error (SyncVar deserialize protected): " + e); }
    }

    [Command(requiresAuthority = false)]
    public void CmdSetCurrentServerRequestId(string newId)
    {
        ApplyServerRequestId(newId);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetCurrentServerRequestId1()
    {
        ApplyServerRequestId(MirrorNetwork.Instance.mirrorRequestId);
    }

    [Server]
    private void ApplyServerRequestId(string id)
    {
        currentServerRequestId = id;
        MirrorNetwork.Instance.mirrorRequestId = id;
        RpcAnnounceMethod(id);
    }

    // ─── RPC Methods ───────────────────────────────────────────────────────────

    public static Action OnRematchRecived;

    [ClientRpc]
    public void RpcAnnounceMethod(string requestId)
    {
        "setClient request Id".Show(requestId);
        this.requestId = requestId;
        PlayerPrefs.SetString("RequestId", requestId);
        MirrorNetwork.Instance.mirrorRequestId = requestId;
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestRematch()
    {
        "Rematch Server request".Show();
        currentServerRequestId = MirrorNetwork.Instance.mirrorRequestId;
        PlayerPrefs.SetString("RequestId", MirrorNetwork.Instance.mirrorRequestId);
        RpcAceptRematchMethod(MirrorNetwork.Instance.mirrorRequestId);
    }

    [Command(requiresAuthority = false)]
    public void CmdStartRematch()
    {
        "Rematch Server request".Show();
        currentServerRequestId = MirrorNetwork.Instance.mirrorRequestId;
        PlayerPrefs.SetString("RequestId", MirrorNetwork.Instance.mirrorRequestId);
        MirrorNetwork.Instance.cleanup(true);
        this.Delay(3f, () => MirrorNetwork.Instance.ServerChangeScene(SceneManager.GetActiveScene().name));
        RpcStartRematchMethod(MirrorNetwork.Instance.mirrorRequestId);
    }

    [ClientRpc]
    public void RpcStartRematchMethod(string requestId)
    {
        "setClient request Id".Show(requestId);
        this.requestId = requestId;
        PlayerPrefs.SetString("RequestId", requestId);
        MirrorNetwork.Instance.mirrorRequestId = requestId;
        MirrorNetwork.Instance.cleanup(true);

        if (creatorData.playerId == staticVariables.UserProfiledata.user._id.ToString())
        {
            ApiAndRoomManager._instance.GetRematchApi(ApiAndRoomManager._instance.winLoseChallengeId, (success) =>
            {
                Debug.Log("Rematch done = " + success);
                Single<DataRematch> rematch = JsonUtility.FromJson<Single<DataRematch>>(success);
                Debug.Log("Rematch json = " + rematch.data.transaction_id);

                if (rematch.status)
                {
                    ApiAndRoomManager._instance.winLoseChallengeId = rematch.data.transaction_id;
                    CmdSetTransactionId(ApiAndRoomManager._instance.winLoseChallengeId);
                }
            });
        }
    }

    [ClientRpc]
    public void RpcAceptRematchMethod(string requestId)
    {
        this.requestId = requestId;
        MirrorNetwork.Instance.mirrorRequestId = requestId;
        OnRematchRecived?.Invoke();
        NetworkGameManager.OnRematchRecived = null;
        MirrorNetwork.Instance.cleanup(true);
    }

    [ClientRpc]
    public void RpcAnnouncePlayerJoined(string playerName, int id)
    {
        Debug.Log($"[CLIENT] {playerName} joined! Total players: {id}");
        SnokerNetwork.IsMultiplayer = true;
        if (currentPlayerCount >= 2) OnEnterSnookerFusion();
        MirrorNetwork.OnPlayerCountChanged?.Invoke(id);
    }

    public NetworkConnectionToClient CreatorRef { get; set; }
    public NetworkConnectionToClient JoinerRef { get; set; }

    [SyncVar(hook = nameof(OnCreatorDataChanged))] public NetworkPlayerData creatorData;
    [SyncVar(hook = nameof(OnJoinerDataChanged))] public NetworkPlayerData joinerData;

    private void OnCreatorDataChanged(NetworkPlayerData oldData, NetworkPlayerData newData) =>
        Debug.Log($"Creator: {newData.playerName}");

    private void OnJoinerDataChanged(NetworkPlayerData oldData, NetworkPlayerData newData) =>
        Debug.Log($"Joiner: {newData.playerName}");

    [ClientRpc]
    public void RpcAnnouncePlayerLeft(string playerName, int remainingPlayers)
    {
        Debug.Log($"[CLIENT] {playerName} left! Remaining players: {remainingPlayers}");
        MirrorNetwork.OnPlayerCountChanged?.Invoke(remainingPlayers);
    }

    void OnEnterSnookerFusion()
    {
        SnokerNetwork.IsMultiplayer = true;
        ApiAndRoomManager._instance.StartChallenge();
        NetworkManagement.EightBallPoolNetworkManager.mainPlayer.imageURL = staticVariables.UserProfiledata.user.file_url;
        NetworkManagement.EightBallPoolNetworkManager.mainPlayer.roomIds = ApiAndRoomManager._instance.challenge_transaction_id;
    }

    [ClientRpc]
    public void RpcSceneChangeStarting(string targetScene)
    {
        Debug.Log($"[CLIENT] Scene change starting to: {targetScene}");
        MirrorNetwork.OnSceneChangeStarting?.Invoke(targetScene);
    }

    [ClientRpc]
    public void RpcGameStateChanged(bool gameActive)
    {
        MirrorNetwork.OnGameStateChanged?.Invoke(gameActive);
    }

    // ─── Disconnect ────────────────────────────────────────────────────────────

    /// <summary>
    /// Client-side entry point for an explicit Leave/Quit button.
    /// Other disconnect causes must keep using their own DisconnectReason.
    /// </summary>
    /// <summary>Server: the player id that announced it is going (Leave or app quit) in this match, or null.</summary>
    public string ExplicitLeaverId { get; private set; }

    /// <summary>The other player of this match, or null.</summary>
    public string OtherPlayerId(string playerId)
    {
        if (string.IsNullOrEmpty(playerId) || creatorData == null || joinerData == null) return null;
        if (playerId == creatorData.playerId) return joinerData.playerId;
        if (playerId == joinerData.playerId) return creatorData.playerId;
        return null;
    }

    /// <summary>
    /// Server: before something wipes the match (CmdReloadServer from a result screen's Leave or Ludo's Leave), settle a
    /// match that is still open. Returns true when the wipe may go ahead. Used to throw away 12 Beads / Carrom / Ludo
    /// matches whose remaining player tapped Leave on the result screen before the 30 s abandon watch ended.
    /// </summary>
    [Server]
    public bool ServerSettleBeforeWipe(string why)
    {
        string scene = SceneManager.GetActiveScene().name;
        if (!MirrorNetwork.hasJoinedGame || _serverMatchSettled || _disconnectWinnerDeclared || IsCasinoScene(scene)) return true;

        string winner = OtherPlayerId(ExplicitLeaverId);
        if (string.IsNullOrEmpty(winner) && NetworkServer.connections.Count == 1) winner = ServerFindLoneRemainingPlayerId();
        if (string.IsNullOrEmpty(winner) && NetworkServer.connections.Count == 0 && _dropOrder.Count >= 2)
            winner = _dropOrder[_dropOrder.Count - 1];

        if (string.IsNullOrEmpty(winner))
        {
            Debug.LogWarning($"[Settlement] {why}: refused — match {transactionId} is still open and both players are here.");
            return false;
        }
        Debug.Log($"[Settlement] {why}: match {transactionId} was still open; settling it for {winner} instead of wiping it.");
        SetDisconnectWinner(winner, null, _explicitLeaveAnnounced ? DISCONNECT_WIN_EXPLICIT_LEAVE : DISCONNECT_WIN_ABANDONED);
        return false;
    }

    /// <summary>
    /// Server: a player pressed a game's Leave/forfeit. When their opponent had already gone (or announced leaving),
    /// the player still here is the winner, not the one who quit - Car, Horse, Snake and Highway forfeits always
    /// awarded the other seat. Returns true when it settled the match for the sender (skip the forfeit then).
    /// </summary>
    [Server]
    public bool ServerForfeitAfterOpponentLeft(NetworkConnectionToClient sender, string game)
    {
        var me = sender != null && sender.identity != null ? sender.identity.GetComponent<MirrorPlayerPrefab>() : null;
        string myId = me != null ? me.playerId : null;
        string other = OtherPlayerId(myId);
        if (string.IsNullOrEmpty(other)) return false;
        bool opponentGone = ExplicitLeaverId == other || ServerFindLoneRemainingPlayerId() == myId;
        if (!opponentGone) return false;
        Debug.Log($"[{game}][Forfeit] {myId} pressed Leave after {other} had already gone - {myId} wins, not the player who quit.");
        SetDisconnectWinner(myId, null, DISCONNECT_WIN_ABANDONED);
        return true;
    }

    /// <summary>Server: show a returning player the result the API already holds, without reporting anything.</summary>
    [Server]
    public void ServerShowRecordedResult(NetworkConnectionToClient conn, string winnerId)
    {
        if (conn == null || string.IsNullOrEmpty(winnerId)) return;
        ServerSendDisconnectWinCause(conn);
        TargetDeclareDisconnectWinner(conn, winnerId);
    }

    public void RequestExplicitLeave()
    {
        if (MirrorNetwork.Instance == null)
        {
            Debug.LogWarning("[ExplicitLeave] MirrorNetwork is missing; request was not sent.");
            return;
        }

        MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.UserRequested;
        CmdSendDisconnectType((int)DisconnectReason.UserRequested);
    }

    [Command(requiresAuthority = false)]
    public void CmdSendDisconnectType(int reason, NetworkConnectionToClient sender = null)
    {
        // Once a disconnect-winner has been declared, suppress further disconnect
        // notifications so they don't re-open the waiting panel behind the result UI.
        if (_disconnectWinnerDeclared) return;
        DisconnectReason disconnectReason = (DisconnectReason)reason;
        // More than 2 players: the leaver's seat goes out and the others play on (LudoNetworkManager); the 2-player
        // settlement below ("the other player wins") must not run.
        if (MultiSeatTable.ServerTakesSeatLeave(sender, disconnectReason)) return;
        MirrorNetwork.Instance.currentDisconnectReason = disconnectReason;

        // Record the intent before anything else. The settlement below can decline (the seat may
        // already be gone, the gates may not line up), and the abandon watch would then finish the
        // match and tell the winner their opponent had LOST CONNECTION -- when that player in fact
        // pressed Leave. The reason the winner reads has to survive that hand-off.
        if (disconnectReason == DisconnectReason.UserRequested) _explicitLeaveAnnounced = true;
        if (disconnectReason == DisconnectReason.UserRequested || disconnectReason == DisconnectReason.ApplicationQuit)
        {
            // Who is going (Leave or app quit): their opponent may claim the win before this connection has closed
            // (ClaimedWinGuard) - Snooker 7ebe75db-c768-1 lost its result that way.
            // The FIRST player to announce wins nothing: when the remaining player then quits too, their own announcement
            // must not overwrite it (that would hand the match to the one who left first).
            var leaver = sender != null && sender.identity != null ? sender.identity.GetComponent<MirrorPlayerPrefab>() : null;
            if (leaver != null && !string.IsNullOrEmpty(leaver.playerId) && string.IsNullOrEmpty(ExplicitLeaverId))
                ExplicitLeaverId = leaver.playerId;
        }

        // Only a Leave/Quit button may send UserRequested. App shutdown, backgrounding,
        // transport loss and scene transitions keep their existing non-settlement paths.
        if (disconnectReason == DisconnectReason.UserRequested
            && TrySettleExplicitLeave(sender))
            return;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != sender)
            {
                RpcSendDisconnectType(conn, reason);
            }
        }
    }

    [Server]
    private bool TrySettleExplicitLeave(NetworkConnectionToClient sender)
    {
        if (_serverMatchSettled || _disconnectWinnerDeclared)
        {
            Debug.Log($"[ExplicitLeave][SERVER] Match '{transactionId}' already has a submitted result; this leave will not settle again.");
            return false;
        }

        string scene = SceneManager.GetActiveScene().name;
        if (!IsExplicitLeaveSettlementScene(scene))
        {
            Debug.Log($"[ExplicitLeave][SERVER] Scene '{scene}' is not on the settlement allow-list; leaving it on the old relay path.");
            return false;
        }

        if (!MirrorNetwork.hasJoinedGame || currentPlayerCount != 2 || !ShouldPauseGame())
        {
            Debug.LogWarning($"[ExplicitLeave][SERVER] Ignored outside an active two-player match. scene={scene}, joined={MirrorNetwork.hasJoinedGame}, players={currentPlayerCount}.");
            return false;
        }

        if (string.IsNullOrEmpty(transactionId) || ApiAndRoomManager._instance == null)
        {
            Debug.LogError($"[ExplicitLeave][SERVER] Settlement prerequisites are missing. transaction='{transactionId}', apiManager={ApiAndRoomManager._instance != null}.");
            return false;
        }

        if (sender == null || sender.identity == null)
        {
            Debug.LogWarning("[ExplicitLeave][SERVER] Ignored because the authoritative sender identity is missing.");
            return false;
        }

        MirrorPlayerPrefab leavingPlayer = sender.identity.GetComponent<MirrorPlayerPrefab>();
        string leaverId = leavingPlayer != null ? leavingPlayer.playerId : null;
        string winnerId = null;

        if (!string.IsNullOrEmpty(leaverId) && creatorData != null && joinerData != null)
        {
            if (leaverId == creatorData.playerId && !string.IsNullOrEmpty(joinerData.playerId))
                winnerId = joinerData.playerId;
            else if (leaverId == joinerData.playerId && !string.IsNullOrEmpty(creatorData.playerId))
                winnerId = creatorData.playerId;
        }

        if (string.IsNullOrEmpty(winnerId))
        {
            Debug.LogError($"[ExplicitLeave][SERVER] Could not map leaver '{leaverId}' to creator/joiner data; settlement was not submitted.");
            return false;
        }

        Debug.Log($"[ExplicitLeave][SERVER] scene={scene}, leaver={leaverId}, winner={winnerId}; submitting generic server settlement.");
        SetDisconnectWinner(winnerId, null, DISCONNECT_WIN_EXPLICIT_LEAVE);
        return true;
    }

    /// <summary>
    /// Server-side leave settlement is deliberately limited to these three games. Every other game,
    /// the casino ones included, keeps exactly the behaviour it had before this feature: the leave
    /// only relays a disconnect and the remaining player's own build decides the result.
    /// This is an allow-list rather than a casino block-list on purpose, so no game is ever opted
    /// in by accident just because nobody remembered to add its scene to an exclusion list.
    /// </summary>
    /// <summary>
    /// The three games this server-side settlement work is scoped to. Public so ApiAndRoomManager can
    /// ask the same question rather than keep a second copy of the list.
    /// </summary>
    public static bool IsExplicitLeaveSettlementScene(string scene)
    {
        return scene == "12OnlineGameScene"   // 12 Beads
            || scene == "CarromOnline"        // Carrom
            || scene == "AightBallPool"       // 8 Ball Pool
            // Added 14 Oct 2026: these settled nothing when a player quit/dropped and the other then left too.
            || scene == "LudoGameScene"
            || scene == "SnakeMultiplayer"
            || scene == "SnokkerMultiplayer"
            || scene == "Ground"              // Cricket match
            || scene == "Map2"                // Car race
            || scene == "HorseRacingMultiPlayer"
            || scene == "HighwayNight";
    }

    /// <summary>Casino tables settle per hand, not per match: no match-level result from the shared code.</summary>
    public static bool IsCasinoScene(string scene) => scene == "GameplayTeenPatti" || scene == "PokerGameplay";

    /// <summary>
    /// 2-player match scenes whose player who stays away loses on the server (abandon watch, AwayClock): the three
    /// above plus Snooker, Ludo 2P and Snake 2P, which used to leave that to the waiting phone's own countdown (QA 28 Oct).
    /// </summary>
    public static bool IsTwoPlayerMatchScene(string scene)
    {
        return IsExplicitLeaveSettlementScene(scene)
            || scene == "SnokkerMultiplayer"
            || scene == "LudoGameScene"
            || scene == "SnakeMultiplayer";
    }

    /// <summary>The active scene is one of the three games this work is scoped to.</summary>
    private static bool IsScopedScene()
    {
        return IsExplicitLeaveSettlementScene(SceneManager.GetActiveScene().name);
    }

    [TargetRpc]
    public void RpcSendDisconnectType(NetworkConnectionToClient target, int reason)
    {
        MirrorNetwork.Instance.OpponentDisconnectReason = (DisconnectReason)reason;
        if (ResultManager.isGameFinished) return;
        switch (MirrorNetwork.Instance.OpponentDisconnectReason)
        {
            case DisconnectReason.UserGoesBackground:
                Debug.Log("Opponent went to background.");
                PopupMessageManager.instance?.waitingPanel.StartTimer();
                break;

            case DisconnectReason.UserRequested:
                Debug.Log("Opponent disconnected manually.");
                PopupMessageManager.instance?.waitingPanel?.StopTimer();
                PopupMessageManager.instance?.SetWaitingPanel(false);
                MirrorNetwork.OnWinCall?.Invoke("Opponent has disconnected. You are declared the winner.");
                break;

            case DisconnectReason.ServerShutdown:
                Debug.LogError("Server has shut down.");
                break;

            case DisconnectReason.ConnectionTimeout:
                Debug.LogWarning("Connection lost due to timeout.");
                break;

            case DisconnectReason.ApplicationPause:
                Debug.Log("Opponent application paused.");
                StartStopGame?.Invoke(true);
                PopupMessageManager.instance?.waitingPanel.StartTimer();
                break;

            case DisconnectReason.ApplicationQuit:
                Debug.Log("Opponent application quit.");
                PopupMessageManager.instance?.waitingPanel?.StopTimer();
                PopupMessageManager.instance?.SetWaitingPanel(false);
                MirrorNetwork.OnWinCall?.Invoke("Opponent has disconnected. You are declared the winner.");
                break;

            case DisconnectReason.SceneTransition:
                Debug.Log("Disconnected due to scene transition.");
                break;

            case DisconnectReason.Connected:
                Debug.Log("Opponent reconnected.");
                MirrorNetwork.Instance.OpponentDisconnectReason = DisconnectReason.Connected;
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.Connected;
                PopupMessageManager.instance?.SetWaitingPanel(false);
                break;

            default:
                Debug.LogWarning("Unhandled disconnect reason.");
                break;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSendTeenPattiDisconnectType(int reason, string excludedPlayerId)
    {
        MirrorNetwork.Instance.currentDisconnectReason = (DisconnectReason)reason;
        RpcSendTeenPattiDisconnectType(reason, excludedPlayerId);
    }

    [ClientRpc]
    private void RpcSendTeenPattiDisconnectType(int reason, string excludedPlayerId)
    {
        MirrorNetwork.Instance.OpponentDisconnectReason = (DisconnectReason)reason;

        if (staticVariables.UserProfiledata.user._id.ToString() == excludedPlayerId)
        {
            if (MirrorNetwork.Instance.OpponentDisconnectReason == DisconnectReason.ApplicationQuit)
                Mirror.NetworkManager.singleton.StopClient();

            return;
        }

        if (ResultManager.isGameFinished) return;

        if (MirrorNetwork.Instance.OpponentDisconnectReason == DisconnectReason.ApplicationQuit)
            MirrorNetwork.OnWinCall?.Invoke("Opponent has disconnected. You are declared the winner.");
    }

    // ─── Background / Pause UI ─────────────────────────────────────────────────

    public void OnPlayerBackground()
    {
        Debug.Log("[NetworkGameManager] Game paused — player went to background");
        RpcGameStateChanged(true);
    }

    void OnPlayerReturnedFromBackground()
    {
        Debug.Log("[NetworkGameManager] Player returned from background");
        ShowPausePanel(false);
        OnPlayerReturned();
    }

    public void OnPlayerReturned()
    {
        Debug.Log("[NetworkGameManager] Player returned from background — resuming");
        RpcGameStateChanged(false);
    }

    public void ShowPausePanel(bool show)
    {
        if (PopupMessageManager.instance == null) return;
        if (show) PopupMessageManager.instance.waitingPanel.StartTimer();
        else PopupMessageManager.instance.waitingPanel.StopTimer();

        // The panel is stopped above regardless, but don't RESUME gameplay if the match
        // has already been decided (opponent reconnecting after a declared result).
        if (!show && ResultManager.isGameFinished) return;
        StartStopGame?.Invoke(show);
    }

    // ─── Reload ────────────────────────────────────────────────────────────────

    [Command(requiresAuthority = false)]
    public void CmdReloadServer()
    {
        // A table of more than 2 keeps playing when one player goes; each match has its own server, which the API
        // stops after the result, so there is nothing to reload.
        if (MatchSeats.Active) { Debug.Log("[Seats] CmdReloadServer ignored on an N-player table."); return; }
        if (!ServerSettleBeforeWipe("server reload")) return;
        Debug.Log("Reloading server and returning to home scene.");
        ServerResetMatchSettlementState("server reload");
        MirrorNetwork.Instance.cleanup();
        MirrorNetwork.Instance.SceneChange("Home");
        CreatorRef = null;
        JoinerRef = null;
        creatorData = new NetworkPlayerData();
        joinerData = new NetworkPlayerData();
    }

    // ─── Turn Manager ──────────────────────────────────────────────────────────

    public static UnityAction<string> OnTurnBegin;
    public static UnityAction<int, string, NetworkConnectionToClient> OnEventReceived;

    [SyncVar] public float RemainingSecondsInTurn;
    [SyncVar] public float TurnDuration;
    public float localDuration;
    [SyncVar] public int Turn;

    [Command(requiresAuthority = false)]
    public void CmdSetNextTurn(string myId)
    {
        Debug.Log("Turn Change Cmd");
        OnTurnBegin.Invoke(myId);
        RpcTurnBegin(myId);
    }

    [ClientRpc]
    public void RpcTurnBegin(string currentTurnId) => OnTurnBegin.Invoke(currentTurnId);

    [Command(requiresAuthority = false)]
    public void CmdRiseEvent(int eventCode, string data, NetworkConnectionToClient sender = null)
    {
        Debug.Log($"CmdRiseEvent: Code={eventCode}, Data={data}, Sender={sender?.connectionId}");
        // F4: while the server runs the Ludo board, a phone may not announce a finish or an exit through the relay
        // (FinishedGame reached SetFinishGame -> WinnerLossChallenge here). The server decides the result itself.
        if (IsLudoClientResultEvent(eventCode))
        {
            MatchFlow.Flag("Ludo", sender, "client_result_event", $"event {eventCode} ignored - the server runs the board");
            return;
        }
        OnEventReceived.Invoke(eventCode, data, sender);
        RiseEventRpc(eventCode, data);
    }

    [ClientRpc]
    public void RiseEventRpc(int eventCode, string data) => OnEventReceived.Invoke(eventCode, data, null);

    // Ludo's FinishedGame (178) and ExitGame (180), only while LudoNetworkManager runs the board.
    static bool IsLudoClientResultEvent(int eventCode) =>
        (eventCode == 178 || eventCode == 180)
        && LudoNetworkManager.ServerAuthority
        && SceneManager.GetActiveScene().name == "LudoGameScene";

    [Command(requiresAuthority = false)]
    public void CmdBeginTurn()
    {
        Turn++;
        TurnDuration = 15f;
    }

    [Command(requiresAuthority = false)]
    public void CmdResetTurn() => Turn = 1;

    [Command(requiresAuthority = false)]
    public void CmdSetDuration(float duration, bool isRpc)
    {
        TurnDuration = duration;
        if (duration <= 0) RpcOnTimerEnd();
        if (isRpc) RpcSetLocalDuration(duration);
    }

    public void SetDuration(float duration)
    {
        if (duration <= 0)
        {
            RpcOnTimerEnd();
        }
    }

    private bool teenPattiTurnTimeoutRaised;
    private int teenPattiLastLoggedSecond = int.MinValue;

    [Server]
    public void SetTeenPattiTurnDuration(float duration)
    {
        TurnDuration = Mathf.Max(0f, duration);
        RemainingSecondsInTurn = TurnDuration;

        int currentSecond = Mathf.CeilToInt(TurnDuration);
        if (currentSecond != teenPattiLastLoggedSecond)
        {
            teenPattiLastLoggedSecond = currentSecond;
            Debug.LogWarning(
                $"[TeenPattiTimer] Countdown tick. Turn={Turn}, Players={currentPlayerCount}, " +
                $"Duration={TurnDuration:F2}, Remaining={RemainingSecondsInTurn:F2}");
        }

        if (duration > 0f)
        {
            teenPattiTurnTimeoutRaised = false;
            return;
        }

        if (teenPattiTurnTimeoutRaised)
            return;

        teenPattiTurnTimeoutRaised = true;
        Debug.LogWarning(
            $"[TeenPattiTimer] Server timeout reached. Turn={Turn}, Players={currentPlayerCount}, " +
            $"Duration={duration:F3}");
        RpcOnTimerEnd();
    }

    [Command(requiresAuthority = false)]
    public void CmdSetTimer(int timer) => RemainingSecondsInTurn = timer;

    [ClientRpc]
    public void RpcOnTimerEnd() => PlayerTurnManager.Instance.OnTurnTimeEnds(Turn);

    [ClientRpc]
    public void RpcSetLocalDuration(float duration) => localDuration = duration;

    // ─── Car Racing ────────────────────────────────────────────────────────────

    [Command(requiresAuthority = false)]
    public void CmdSpawnCarRacing(NetworkConnectionToClient sender = null) =>
        CarRace.MultiPlayerGameManager.Instance.SpawnVehicle(sender);

    [TargetRpc]
    public void TargetSetupVehicle(NetworkConnection _, GameObject vehicle)
    {
        if (!vehicle.TryGetComponent<RCC_CarControllerV3>(out var newVehicle)) return;

        RCC.RegisterPlayerVehicle(newVehicle);
        RCC.SetControl(newVehicle, true);

        if (RCC_SceneManager.Instance.activePlayerCamera)
            RCC_SceneManager.Instance.activePlayerCamera.SetTarget(newVehicle.gameObject);
    }

    // ─── Waiting Panel ─────────────────────────────────────────────────────────

    public static Action<bool> StartStopGame;

    [ClientRpc(includeOwner = false)]
    public void RpcShowWaitingPanel(bool val)
    {
        "RPC ShowWaitingPanel".Show();
        ShowPausePanel(val);
    }

    /// <summary>
    /// The away clock (AwayClock): seconds until the away player loses, -1 when nobody is away; reconnecting = a new
    /// connection is loading the game. Same signature in the app's copy, which counts its waiting panel down from it.
    /// </summary>
    [ClientRpc]
    public void RpcAwayClock(float secondsLeft, bool reconnecting) { }

    // ─── Result ────────────────────────────────────────────────────────────────

    [Command(requiresAuthority = false)]
    public void CmdPlayerFinished(bool isWin, int playerId)
    {
        Debug.Log("CmdPlayerFinished");
        // Ludo (server authority off) ends here, and this only showed the result screens: a Ludo win was never reported,
        // so both stakes stayed held. Report it once (the settlement latch drops a second result). A loss names the
        // other player. Until LudoNetworkManager.ServerAuthorityEnabled is on, a win is the client's word.
        // With LudoNetworkManager.ServerAuthority on, ServerWin / ServerTimeOver report the result from the server's own
        // board; every phone then calls this from RpcMatchWon, and a modified phone could call it first - not reported.
        if (NetworkServer.active && SceneManager.GetActiveScene().name == "LudoGameScene" && ApiAndRoomManager._instance != null
            && !LudoNetworkManager.ServerAuthority)
        {
            string winner = isWin ? playerId.ToString() : OtherPlayerId(playerId.ToString());
            if (!string.IsNullOrEmpty(winner) && (winner == creatorData?.playerId || winner == joinerData?.playerId))
            {
                MatchFlow.Log("Ludo", $"result reported by a player: {(isWin ? "win" : "loss")} for {playerId} — winner {winner}");
                ApiAndRoomManager._instance.WinnerLossChallenge(winner);
            }
            else Debug.LogWarning($"[Ludo][Result] CmdPlayerFinished({isWin}, {playerId}) names no player of this match; not reported.");
        }
        RpcActivateUIAndHandleResult(isWin, playerId.ToString());
        this.Delay(1, () => MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(currentServerRequestId));
    }

    [ClientRpc]
    void RpcActivateUIAndHandleResult(bool didWin, string playerId)
    {
        Debug.Log("RpcActivateUIAndHandleResult");
        if (ResultManager.GameSpawnedFinished) return;

        ResultManager.GameSpawnedFinished = true;
        GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
        if (prefab != null)
        {
            "1".Show();
            Instantiate(prefab, Vector3.zero, Quaternion.identity)
                .GetComponent<ResultManager>()
                .HandleGameResultAltMultiplayer(didWin, playerId);
        }
        else
        {
            Debug.LogError("WinLose GameManager prefab not found!");
        }
    }
    //private string _disconnectWinnerId = null;
    //private bool _disconnectWinnerDeclared = false;
    //[Server]
    //public void SetDisconnectWinner(string winnerId)
    //{
    //    Debug.Log($"[NetworkGameManager] Player {winnerId} wins by disconnect");
    //    _disconnectWinnerId = winnerId;
    //    StartCoroutine(CheckGameStatus());
    //}
    //public IEnumerator CheckGameStatus()
    //{
    //    Debug.Log("[NetworkGameManager] Starting CheckGameStatus coroutine to monitor disconnect winner");
    //    int count = 0;
    //    while (count < 30)
    //    {
    //        yield return new WaitForSecondsRealtime(2f);
    //        if (!string.IsNullOrEmpty(_disconnectWinnerId))
    //        {
    //            if (!_disconnectWinnerDeclared)
    //            {
    //                // One-time server work: record scores and call API
    //                _disconnectWinnerDeclared = true;
    //                creatorData.Scores = 0;
    //                joinerData.Scores = 0;
    //                ApiAndRoomManager._instance.WinnerLossChallenge(_disconnectWinnerId);
    //            }
    //            // Re-fire Rpc every iteration so late/reconnecting clients get it
    //            RpcDeclareDisconnectWinner(_disconnectWinnerId);
    //        }
    //        count++;
    //    }
    //}
    //[ClientRpc]
    //private void RpcDeclareDisconnectWinner(string winnerId)
    //{
    //    Debug.Log($"[NetworkGameManager] RpcDeclareDisconnectWinner called with winnerId={winnerId}");
    //    MirrorNetwork.OnWinCall?.Invoke("Your opponent has left the game. You are declared the winner.");
    //}
    // ─── Notify Win ────────────────────────────────────────────────────────────

    [Server]
    public void notify()
    {
        "Notify".Show();
        RpcNotifyAllPlayerToWin();
    }

    [ClientRpc]
    public void RpcNotifyAllPlayerToWin()
    {
        "RPC notify".Show();
        MirrorNetwork.OnWinCall.Invoke("Your opponent has left the game. You are declared the winner.");
    }
   // ─── Disconnect-based win for ALL games ───────────────────────────────────
    // Mirrors StickManager.CheckGameStatus() but works for every game type.
    // Called from MirrorNetwork.OnPlayerReconnected when the reconnect counter
    // exceeds the limit AND there is no StickManager (i.e. non-Snooker game).

    private string _disconnectWinnerId = null;
    private bool _disconnectWinnerDeclared = false;
    private int _disconnectWinCause = DISCONNECT_WIN_GENERIC;
    /// <summary>The winner SetDisconnectWinner declared for this match (null if none) and why: Snake's board takes it
    /// over as its own result (QA 29 Oct).</summary>
    public string ServerDisconnectWinnerId => _disconnectWinnerDeclared ? _disconnectWinnerId : null;
    public int ServerDisconnectWinCause => _disconnectWinCause;

    // Why the disconnect win was declared. The result popup used to be hardcoded to the
    // reconnect-limit sentence, so a player who deliberately tapped Leave was told they had
    // "disconnected 5 times" and the winner was told the same about their opponent. The cause
    // now travels with the winner id so each side reads the real reason.
    public const int DISCONNECT_WIN_GENERIC = 0;          // connection lost / backend already closed the challenge
    public const int DISCONNECT_WIN_RECONNECT_LIMIT = 1;  // opponent used up the reconnect allowance
    public const int DISCONNECT_WIN_EXPLICIT_LEAVE = 2;   // opponent pressed the Leave button
    public const int DISCONNECT_WIN_ABANDONED = 3;        // opponent dropped and never came back

    // Set the moment ANY server-side settlement is submitted for this match, win/loss or draw,
    // from ApiAndRoomManager. TrySettleExplicitLeave needs it because its own liveness
    // gate, ShouldPauseGame(), reads ResultManager.isGameFinished. That static is only ever set
    // by the result UI, and a headless build never spawns the WinLoseGameManager prefab, so on
    // the server the gate reads "match still live" for the whole session. A Leave tapped AFTER a
    // finished match therefore still looked settleable, and would have submitted a SECOND result
    // naming the other player as the winner -- flipping the payout when the actual winner is the
    // one who tapped Leave first.
    private bool _serverMatchSettled = false;

    public bool ServerMatchAlreadySettled => _serverMatchSettled;

    /// <summary>
    /// Server-only latch: this match already has a result on the backend. Not marked [Server]
    /// because presentation code on a player build calls the same ApiAndRoomManager methods;
    /// the NetworkServer.active check makes it a no-op there instead of logging a Mirror warning.
    /// </summary>
    public void ServerMarkMatchSettled(string why)
    {
        if (!NetworkServer.active || _serverMatchSettled) return;
        _serverMatchSettled = true;
        Debug.Log($"[Settlement] Match '{transactionId}' latched as settled ({why}); a later Leave will not re-settle it.");
    }

    /// <summary>
    /// The backend refused the settlement, so this match has no result after all. The latch is set
    /// before the request goes out (so a Leave arriving mid-flight cannot double-submit); without
    /// this counterpart a transient backend failure left it on for good, and both the explicit-leave
    /// path and 12 Beads' own finalize would then stand down for a match that was never paid.
    /// </summary>
    public void ServerClearMatchSettled(string why)
    {
        if (!NetworkServer.active || !_serverMatchSettled) return;
        _serverMatchSettled = false;
        Debug.LogWarning($"[Settlement] Match '{transactionId}' is NOT settled after all ({why}); a later attempt may still settle it.");
    }

    private Coroutine _checkGameStatusRoutine;

    /// <summary>
    /// Clears the settlement state the PREVIOUS match left behind. This object is spawned once in
    /// MirrorNetwork.OnStartServer and kept alive with DontDestroyOnLoad, and CmdReloadServer sends
    /// the same server back to Home for the next match, so nothing here resets on its own. Left
    /// latched, _disconnectWinnerDeclared makes the server refuse to declare a second winner AND
    /// suppress every disconnect relay, while _serverMatchSettled makes every later Leave look
    /// already-settled -- so on a reused server only the first match of its life would settle.
    /// </summary>
    [Server]
    public void ServerResetMatchSettlementState(string why)
    {
        // Every game now settles through the same latch, so every game resets it (a rematch used to keep the last
        // match's "winner declared" flag outside the first three games, which silenced all disconnect relays).
        ExplicitLeaverId = null;
        _dropOrder.Clear();
        AwayClock.Stop(newMatch: true);

        if (_checkGameStatusRoutine != null)
        {
            StopCoroutine(_checkGameStatusRoutine);
            _checkGameStatusRoutine = null;
        }

        if (_abandonWatchRoutine != null)
        {
            StopCoroutine(_abandonWatchRoutine);
            _abandonWatchRoutine = null;
        }

        _explicitLeaveAnnounced = false;

        bool hadState = _disconnectWinnerDeclared || _serverMatchSettled;
        _disconnectWinnerId = null;
        _disconnectWinnerDeclared = false;
        _disconnectWinCause = DISCONNECT_WIN_GENERIC;
        _serverMatchSettled = false;

        if (hadState)
            Debug.Log($"[Settlement] Cleared the previous match's settlement state ({why}).");
    }

    // A player who drops and never comes back used to settle nothing at all. SetDisconnectWinner is
    // only reachable from a reconnect ATTEMPT (the 5-try limit) or the Leave button, so a killed app
    // or a dead radio left the match open for good. The remaining player's own build used to post the
    // result from its 30 s waiting panel, but the player build no longer submits settlements, so that
    // fallback silently stopped working once settlement moved to the server. This closes the hole.
    //
    // Scoped to the same three games as the rest of the server-side settlement work.
    //
    // Matches the waiting panel the player is actually looking at (WaitingPanel.Show30SecondsTimer
    // counts from 30), so the countdown they see is the countdown that decides the match. A shorter
    // grace ended the match while the panel still read 27 and gave a genuine reconnect no chance:
    // Mirror's own transport timeout has to elapse before this even starts.
    public const float ABANDON_GRACE_SECONDS = 30f;

    private Coroutine _abandonWatchRoutine;

    /// <summary>Set when a player announced a deliberate Leave, so the abandon watch can report the
    /// real reason instead of a lost connection.</summary>
    private bool _explicitLeaveAnnounced;

    /// <summary>
    /// Server: a player just dropped. Wait out the grace period, then award the match to whoever is
    /// still here. Safe to call for every disconnect; each non-qualifying case returns immediately.
    /// </summary>
    // Players in the order they dropped. When the opponent leaves first, the remaining player's app shows its win
    // and goes Home - often before the grace period ends - so the watch used to find nobody left and settle
    // nothing, with both stakes held (Snooker 7ebe75db-c768-1, 14 Oct). Then the player who left last wins.
    private readonly List<string> _dropOrder = new List<string>();

    [Server]
    public void ServerBeginAbandonWatch(string droppedPlayerId = null)
    {
        // More than 2 players: each seat has its own drop timer (LudoNetworkManager), not this 2-player watch.
        if (MatchSeats.Active) return;
        if (!string.IsNullOrEmpty(droppedPlayerId))
        {
            _dropOrder.Remove(droppedPlayerId);
            _dropOrder.Add(droppedPlayerId);
        }
        if (_serverMatchSettled || _disconnectWinnerDeclared) return;
        if (!MirrorNetwork.hasJoinedGame) return;
        if (!IsTwoPlayerMatchScene(SceneManager.GetActiveScene().name)) return;

        if (_abandonWatchRoutine != null) StopCoroutine(_abandonWatchRoutine);
        _abandonWatchRoutine = StartCoroutine(AbandonWatch());
    }

    [Server]
    public void ServerCancelAbandonWatch(string why)
    {
        // Only when BOTH players are here again. One player rejoining while the other is still gone used to cancel the
        // watch and forget who had left, so nothing settled (12 Beads 3277960b-c78b-1: Wasi's app was killed, Alex
        // blipped and reconnected, then left - the stakes stayed held).
        if (NetworkServer.connections.Count < 2)
        {
            Debug.Log($"[Abandon][SERVER] {why}, but the opponent is still gone - the watch keeps running.");
            return;
        }
        _dropOrder.Clear();
        ExplicitLeaverId = null;   // both are back: an announced departure no longer counts
        if (_abandonWatchRoutine == null) return;
        StopCoroutine(_abandonWatchRoutine);
        _abandonWatchRoutine = null;
        Debug.Log($"[Abandon][SERVER] Watch cancelled ({why}).");
    }

    private IEnumerator AbandonWatch()
    {
        // The away clock started when the player went silent, usually 15 s before the transport dropped them: wait for
        // the same seconds the waiting phone is counting down, not another 30 from the drop.
        float until = Time.realtimeSinceStartup + (AwayClock.Running ? AwayClock.SecondsLeft : ABANDON_GRACE_SECONDS);
        while (Time.realtimeSinceStartup < until) yield return new WaitForSecondsRealtime(0.25f);
        // A phone that is reconnecting is reloading the game scene before its player is added: give it a little longer.
        float hold = until + AwayClock.ReconnectHoldSeconds;
        while (Time.realtimeSinceStartup < hold && currentPlayerCount < 2 && NetworkServer.connections.Count > 1)
            yield return new WaitForSecondsRealtime(0.25f);
        _abandonWatchRoutine = null;

        // Something else decided the match while we were waiting.
        if (_serverMatchSettled || _disconnectWinnerDeclared) yield break;

        // They are back. A reconnect mid-flight already had the 15 s hold above; a connection that still has no player
        // after it (a reconnect that never finished loading, Snake 12:54 / 13:01, QA 29 Oct) used to keep the match open
        // and settle nothing.
        if (currentPlayerCount >= 2)
        {
            Debug.Log("[Abandon][SERVER] Player returned inside the grace period; nothing settled.");
            yield break;
        }

        string winnerId = ServerFindLoneRemainingPlayerId();
        // Both have gone. The player who first announced leaving (app quit / Leave) loses; without an announcement, the
        // order the server noticed the drops (a killed app is noticed late, after the transport timeout).
        if (string.IsNullOrEmpty(winnerId) && NetworkServer.connections.Count == 0 && !string.IsNullOrEmpty(OtherPlayerId(ExplicitLeaverId)))
        {
            winnerId = OtherPlayerId(ExplicitLeaverId);
            Debug.Log($"[Abandon][SERVER] Both players have gone; {ExplicitLeaverId} announced leaving first, so {winnerId} wins.");
        }
        if (string.IsNullOrEmpty(winnerId) && NetworkServer.connections.Count == 0 && _dropOrder.Count >= 2)
        {
            string lastOut = _dropOrder[_dropOrder.Count - 1], firstOut = _dropOrder[_dropOrder.Count - 2];
            bool bothPlayers = creatorData != null && joinerData != null
                && (lastOut == creatorData.playerId || lastOut == joinerData.playerId)
                && (firstOut == creatorData.playerId || firstOut == joinerData.playerId) && lastOut != firstOut;
            if (bothPlayers)
            {
                winnerId = lastOut;
                Debug.Log($"[Abandon][SERVER] Both players have gone; {firstOut} left first, so {lastOut} wins.");
            }
        }
        if (string.IsNullOrEmpty(winnerId))
        {
            Debug.LogWarning("[Abandon][SERVER] No single remaining player to award; nothing settled.");
            yield break;
        }

        int cause = _explicitLeaveAnnounced ? DISCONNECT_WIN_EXPLICIT_LEAVE : DISCONNECT_WIN_ABANDONED;
        Debug.Log($"[Abandon][SERVER] Opponent did not return within {ABANDON_GRACE_SECONDS}s; winner={winnerId}, " +
                  $"reported as {(_explicitLeaveAnnounced ? "a deliberate leave" : "a lost connection")}.");
        SetDisconnectWinner(winnerId, null, cause);
    }

    /// <summary>The single still-connected player, or null when that is ambiguous.</summary>
    [Server]
    private string ServerFindLoneRemainingPlayerId()
    {
        string found = null;
        int seen = 0;
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;
            MirrorPlayerPrefab player = conn.identity.GetComponent<MirrorPlayerPrefab>();
            if (player == null || string.IsNullOrEmpty(player.playerId)) continue;
            seen++;
            found = player.playerId;
        }
        return seen == 1 ? found : null;
    }

    /// <summary>
    /// Server-side: record which player won due to opponent disconnect and start
    /// the polling coroutine so any (re)connecting client gets the win message.
    /// </summary>
    /// <summary>
    /// Sends the disconnect-win cause, but only for the three scoped games. Everywhere else the wire
    /// carries exactly the messages it carried before this work, and the client falls back to the
    /// generic wording -- which is the sentence those games always showed.
    /// </summary>
    [Server]
    private void ServerSendDisconnectWinCause(NetworkConnection target = null)
    {
        if (!IsScopedScene()) return;
        if (target != null) TargetSetDisconnectWinCause(target, _disconnectWinCause);
        else RpcSetDisconnectWinCause(_disconnectWinCause);
    }

    [Server]
    public void SetDisconnectWinner(string winnerId, NetworkConnectionToClient lateConn = null, int cause = DISCONNECT_WIN_GENERIC)
    {
        Debug.Log($"[NetworkGameManager] Player {winnerId} wins by disconnect (cause {cause})");
        if (_disconnectWinnerDeclared)
        {
            // Already declared — just make sure the freshly-reconnected player gets it.
            if (lateConn != null)
            {
                ServerSendDisconnectWinCause(lateConn);
                TargetDeclareDisconnectWinner(lateConn, winnerId);
            }
            return;
        }
        _disconnectWinnerId = winnerId;
        _disconnectWinnerDeclared = true;
        _disconnectWinCause = cause;
        AwayClock.Stop(newMatch: false);
        RpcAwayClock(-1f, false);
        creatorData.Scores = 0;
        joinerData.Scores = 0;
        ApiAndRoomManager._instance.WinnerLossChallenge(_disconnectWinnerId);
        // Fire the disconnect-win Rpc immediately so both clients get the right
        // win-or-loss UI without a 2 s delay.
        ServerSendDisconnectWinCause();
        RpcDeclareDisconnectWinner(_disconnectWinnerId);
        // Also fire a TargetRpc directly to the freshly-reconnected player —
        // they may still be in the middle of loading the game scene when the
        // ClientRpc above goes out and silently drop it.
        if (lateConn != null)
        {
            ServerSendDisconnectWinCause(lateConn);
            TargetDeclareDisconnectWinner(lateConn, winnerId);
        }
        // Keep re-firing so a player who is still mid-reconnect (and missed the
        // first message) still receives it once their scene loads.
        _checkGameStatusRoutine = StartCoroutine(CheckGameStatus());
    }

    /// <summary>
    /// Re-fires the win Rpc every 2 s (up to 60 s) so reconnecting players
    /// who missed the first message still receive it (same pattern as StickManager).
    /// </summary>
    public IEnumerator CheckGameStatus()
    {
        Debug.Log("[NetworkGameManager] Starting CheckGameStatus coroutine to monitor disconnect winner");
        int count = 0;
        while (count < 30)
        {
            yield return new WaitForSecondsRealtime(2f);
            if (!string.IsNullOrEmpty(_disconnectWinnerId))
            {
                ServerSendDisconnectWinCause();
                RpcDeclareDisconnectWinner(_disconnectWinnerId);
            }
            count++;
        }
    }

    // The two winner Rpcs below MUST keep their original signatures. Mirror hashes the FULL method
    // signature (RemoteCalls.RegisterDelegate -> functionFullName.GetStableHashCode()), so adding a
    // parameter changes the hash: a client built against the new signature does not recognise the
    // old server's message, logs "no receiver for incoming ClientRpc" and DROPS it, and no win/lose
    // UI ever appears. Shipping the client ahead of the server did exactly that.
    //
    // The cause therefore rides its own Rpc instead. An old server never sends it, so the cause
    // simply stays GENERIC and the wording falls back gracefully; an old client never registered it,
    // so it drops that one message and still gets the winner. Client and server can ship apart again.
    [ClientRpc]
    private void RpcSetDisconnectWinCause(int cause)
    {
        _disconnectWinCause = cause;
    }

    [TargetRpc]
    private void TargetSetDisconnectWinCause(NetworkConnection target, int cause)
    {
        _disconnectWinCause = cause;
    }

    [ClientRpc]
    private void RpcDeclareDisconnectWinner(string winnerId)
    {
        Debug.Log($"[NetworkGameManager] RpcDeclareDisconnectWinner called with winnerId={winnerId}, cause={_disconnectWinCause}");
        ShowDisconnectWinUI(winnerId);
    }

    [TargetRpc]
    private void TargetDeclareDisconnectWinner(NetworkConnection target, string winnerId)
    {
        Debug.Log($"[NetworkGameManager] TargetDeclareDisconnectWinner called with winnerId={winnerId}, cause={_disconnectWinCause}");
        ShowDisconnectWinUI(winnerId);
    }

    private void ShowDisconnectWinUI(string winnerId)
    {
        // Close any waiting/reconnection panels that may still be open before the win UI fires —
        // otherwise the waiting timer keeps running on both clients and overlays the result screen.
        if (PopupMessageManager.instance != null)
        {
            PopupMessageManager.instance.waitingPanel?.StopTimer();
            PopupMessageManager.instance.ReconnectionPanelStatus(false);
        }
        ResultManager.isGameFinished = true;

        // Guard immediately so a repeat RPC (CheckGameStatus re-fires every 2 s) can't queue
        // a second popup/spawn, then run the disconnect-win reveal sequence.
        if (ResultManager.GameSpawnedFinished) return;
        ResultManager.GameSpawnedFinished = true;
        StartCoroutine(ShowDisconnectWinUIDelayed(winnerId, _disconnectWinCause));
    }

    /// <summary>
    /// Wording for the disconnect-win popup. Every cause used to render as the reconnect-limit
    /// sentence, which reads as a plain lie on the Leave path: the player who chose to leave was
    /// told they had "disconnected 5 times", and the winner was told the same about them.
    /// </summary>
    private static string DisconnectWinMessage(bool localIsWinner, int cause)
    {
        switch (cause)
        {
            case DISCONNECT_WIN_EXPLICIT_LEAVE:
                return localIsWinner
                    ? "Your opponent left the game. You have been declared the winner!"
                    : "You left the game. Your opponent has been declared the winner.";

            case DISCONNECT_WIN_ABANDONED:
                return localIsWinner
                    ? "Your opponent lost connection and did not return. You have been declared the winner!"
                    : "You lost connection and did not return. Your opponent has been declared the winner.";

            case DISCONNECT_WIN_RECONNECT_LIMIT:
                return localIsWinner
                    ? "Your opponent disconnected 5 times. You have been declared the winner!"
                    : "You disconnected 5 times. Your opponent has been declared the winner.";

            // Anything outside the three scoped games lands here and keeps, word for word, the
            // sentence it showed before this work.
            default:
                return localIsWinner
                    ? "Your opponent disconnected 5 times. You have been declared the winner!"
                    : "You disconnected 5 times. Your opponent has been declared the winner.";
        }
    }

    private IEnumerator ShowDisconnectWinUIDelayed(string winnerId, int cause)
    {
        // Each client compares the winner id against its own user id to decide which
        // side (win / loss) it is on, then shows the matching disconnect notification.
        bool localIsWinner = winnerId == staticVariables.UserProfiledata.user._id.ToString();
        string message = DisconnectWinMessage(localIsWinner, cause);

        // Notify popup shows for 5 s on both sides...
        if (PopupMessageManager.instance != null)
            PopupMessageManager.instance.ShowPopUp(message, "Match Result", 5f);

        // ...then a further 10 s delay before the win/loss result screen appears.
        yield return new WaitForSecondsRealtime(5f);

        // Spawn the result UI directly with the winner's id so each client can
        // compare against their local user id and show win-or-loss correctly.
        // (This is the same flow CmdPlayerFinished → RpcActivateUIAndHandleResult uses,
        //  but driven by the server without needing a per-client Cmd call.)
        GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
        if (prefab != null)
        {
            Instantiate(prefab, Vector3.zero, Quaternion.identity)
                .GetComponent<ResultManager>()
                .HandleGameResultAltMultiplayer(true, winnerId);
        }
        else
        {
            Debug.LogError("WinLose GameManager prefab not found!");
        }
    }

    // ─── Poker ─────────────────────────────────────────────────────────────────

    [SyncVar] public int syncedPokerRoomState;
    [SyncVar] public string syncedCommunityCards = "";
    [SyncVar] public int syncedDropCardsIndex;
    [SyncVar(hook = nameof(OnPokerGameStarted))] public bool syncedPokerGameStarted = false;

    [SyncVar] public int syncedGameCounter;
    [SyncVar] public int syncedDIndex;
    [SyncVar] public string syncedPokerMatchHistory0 = "";
    [SyncVar] public string syncedPokerMatchHistory1 = "";

    [Command(requiresAuthority = false)]
    public void CmdSetDIndex(int val) => syncedDIndex = val;

    [Command(requiresAuthority = false)]
    public void CmdSetPokerMatchHistory(int playerIndex, string csvData)
    {
        if (playerIndex == 0) syncedPokerMatchHistory0 = csvData;
        else syncedPokerMatchHistory1 = csvData;
    }

    void OnPokerGameStarted(bool oldVal, bool newVal)
    {
        if (!newVal || NetworkServer.active) return;
        this.DelayUntil(
            () => POKER.GameManager.Instance != null
               && POKER.GameManager.Instance.playersList.Exists(p => p != null && p.IsMine()),
            () => CmdRequestPokerReconnectState()
        );
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestPokerReconnectState()
    {
        if (!syncedPokerGameStarted) return;
        RpcRestorePokerOnReconnect();
    }

    [ClientRpc]
    public void RpcRestorePokerOnReconnect()
    {
        if (POKER.GameManager.Instance != null)
            POKER.GameManager.Instance.RestorePokerStateOnReconnect();
    }
}

[Serializable]
public struct MirrorPlayerInfos
{
    public string playerName;
    public string playerId;
}

[Serializable]
public class DataRematch
{
    public string transaction_id;
}
