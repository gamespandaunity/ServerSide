using Mirror;
using System.Collections;
using System.Collections.Generic;
using Twelve;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExtensions;

public class TwelveBeadNetworkManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnServerTurnChanged))]
    private int currentPlayerIndex = 0;

    public int CurrentPlayerIndex => currentPlayerIndex;

    [SyncVar]
    private bool hasInitialTurnBroadcastCompleted = false;

    public static bool isTossDone = false;
    public static TwelveBeadNetworkManager instance;

    // Updated by RpcSetTurnData so the SyncVar-hook fallback can tell whether
    // the normal turn broadcast already reached this client.
    private int _lastRpcAppliedTurnId = 0;
    private Coroutine _applySyncedTurnCoroutine;

    // Server holds the saved game state


    void Awake()
    {
        instance = this;
    }

    #region TOSS

    // SyncVar hook fires on clients whenever the authoritative server turn changes.
    // Normal connected clients still use RpcSetTurnData so the toss announcement flow is
    // preserved. Reconnecting clients can miss that RPC, so this hook applies the saved
    // server turn after a short grace window and after local 12-bead objects are ready.
    void OnServerTurnChanged(int oldValue, int newValue)
    {
        Debug.Log($"[HOOK] OnServerTurnChanged | oldValue={oldValue} newValue={newValue} serverActive={NetworkServer.active}");

        if (newValue == 0) return; // ignore default-state writes

        // NOTE: do NOT touch TwelveBeadNetworkManager.isTossDone here.
        // TurnAnnouncementTwelve.ApplyTurnUI() uses that static as its
        // "panel already shown" guard — if we flip it before the toss
        // TargetRpc arrives, the announcement panel is silently skipped.

        // Host runs DoToss() directly, no fallback needed.
        if (NetworkServer.active) return;

        if (_applySyncedTurnCoroutine != null)
            StopCoroutine(_applySyncedTurnCoroutine);

        _applySyncedTurnCoroutine = StartCoroutine(ApplyTurnIfRpcMissed(newValue));
    }

    // Wait for the matching RPC in the normal path. Once the first delayed toss
    // broadcast has happened, reconnects only need a short grace window.
    private IEnumerator ApplyTurnIfRpcMissed(int turnId)
    {
        float waited = 0f;
        while (waited < 5f)
        {
            if (_lastRpcAppliedTurnId == turnId) yield break; // normal flow already handled it
            if (hasInitialTurnBroadcastCompleted && waited >= 0.75f) break;
            waited += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }

        // RPC never arrived (reconnect path). Wait for gameplay objects and local
        // player role to be ready before applying turn UI/input state.
        float spawnTimeout = 5f;
        while (!IsTurnApplyReady() && spawnTimeout > 0f)
        {
            spawnTimeout -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (MultiPlayerGameManagerTwelve.instance == null)
        {
            Debug.LogError("[HOOK] MultiPlayerGameManagerTwelve missing — cannot apply fallback turn");
            yield break;
        }

        // A reconnect should not re-open the toss panel; it only needs the current turn.
        if (hasInitialTurnBroadcastCompleted)
            isTossDone = true;

        Debug.Log($"[HOOK] RPC missed — applying synced server turn {turnId}");
        MultiPlayerGameManagerTwelve.instance.SetPlayerTurn(turnId, false);
    }

    private bool IsTurnApplyReady()
    {
        var multi = MultiPlayerGameManagerTwelve.instance;
        var controller = GamePlayControllerTwelve.instance;

        if (multi == null || controller == null) return false;
        if (GamePlayControllerTwelve.myId == 0) return false;
        if (controller.allNodes == null || controller.allNodes.Count == 0) return false;
        if (controller.emptyBeads == null || controller.emptyBeads.Count < controller.allNodes.Count) return false;
        if (controller.team1Beads == null || controller.team1Beads.Count == 0) return false;
        if (controller.team2Beads == null || controller.team2Beads.Count == 0) return false;

        return true;
    }

    public void NotifyTossResult(int tossWinnerId)
    {
        Debug.LogError($"[SERVER] Notifying toss result | Winner: {tossWinnerId}");

        // tossWinnerId is a PLAYERS enum value: 1 = PLAYER1 (creator), 2 = PLAYER2 (joiner).
        // Use CreatorRef/JoinerRef to map correctly — connectionId != PLAYER enum value.
        var creatorConn = NetworkGameManager.Instance?.CreatorRef;
        var joinerConn = NetworkGameManager.Instance?.JoinerRef;

        if (creatorConn != null)
        {
            bool creatorWon = tossWinnerId == (int)PLAYERS.PLAYER1;
            Debug.LogError($"[SERVER] Sending to Creator | Won: {creatorWon}");
            TargetShowTossResult(creatorConn, creatorWon);
        }

        if (joinerConn != null)
        {
            bool joinerWon = tossWinnerId == (int)PLAYERS.PLAYER2;
            Debug.LogError($"[SERVER] Sending to Joiner | Won: {joinerWon}");
            TargetShowTossResult(joinerConn, joinerWon);
        }
    }

    [TargetRpc]
    void TargetShowTossResult(NetworkConnection target, bool isLocalWon)
    {
        Debug.LogError($"[CLIENT] Toss result | Won: {isLocalWon}");

        // Only show the announcement UI — the actual first turn is set uniformly for
        // all clients by RpcSetTurnData broadcast from DoToss() after this delay.
        var turnAnnounce = FindObjectOfType<TurnAnnouncementTwelve>();
        if (turnAnnounce != null)
        {
            turnAnnounce.ApplyTurnUI(
                isOnlineMultiplayer: true,
                isWithAi: false,
                turn: isLocalWon ? PLAYERS.PLAYER1 : PLAYERS.PLAYER2,
                isLocalWon: isLocalWon
            );
        }
    }

    [Server]
    public void DoToss()
    {
        if (isTossDone) return;
        isTossDone = true; // prevent re-entry from SyncVar hook or double-call

        int tossWinnerId = UnityEngine.Random.Range(1, 3);
        Debug.LogError("🎯 Toss Winner: " + tossWinnerId);

        ServerSetCurrentPlayer((PLAYERS)tossWinnerId);

        // Show per-client toss announcement ("You won/lost the toss")
        NotifyTossResult(tossWinnerId);

        // After the announcement UI finishes, broadcast the SAME first-turn id to ALL
        // clients so currentPlayerTurn is identical on every machine.
        this.Delay(2, () =>
        {
            hasInitialTurnBroadcastCompleted = true;
            ServerBroadcastCurrentTurn();
            if (MultiPlayerGameManagerTwelve.instance != null)
            {
                MultiPlayerGameManagerTwelve.instance.ServerResetTurnTimer();
                MultiPlayerGameManagerTwelve.instance.StartGameTimer();
            }
        });

        TurnAnnouncementTwelve turnAnnounce = FindObjectOfType<TurnAnnouncementTwelve>();
        if (turnAnnounce != null)
        {
            bool serverLocalWin = (tossWinnerId == PLAYERS.PLAYER1.GetHashCode());
            turnAnnounce.ApplyTurnUI(true, false, (PLAYERS)tossWinnerId, serverLocalWin);
        }
    }

    #endregion

    #region Client Start & Auto State Request

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[CLIENT] OnStartClient - Starting client...");

        // Pure clients may be reconnecting into an already-running match.
        if (!NetworkServer.active)
        {
            StartCoroutine(ApplyCurrentTurnAfterClientStart());
        }
    }

    private IEnumerator ApplyCurrentTurnAfterClientStart()
    {
        yield return null;
        yield return new WaitForSeconds(0.2f);

        if (currentPlayerIndex == 0 || _lastRpcAppliedTurnId == currentPlayerIndex)
            yield break;

        if (_applySyncedTurnCoroutine != null)
            StopCoroutine(_applySyncedTurnCoroutine);

        _applySyncedTurnCoroutine = StartCoroutine(ApplyTurnIfRpcMissed(currentPlayerIndex));
    }

    // private IEnumerator AutoRequestStateAfterDelay()
    // {
    //     // Wait for scene to fully load
    //     yield return new WaitForSeconds(2f);

    //     // Check if game controller exists

    //     if (GamePlayControllerTwelve.beadsCreated)
    //     {
    //         Debug.Log("[CLIENT] Auto-requesting game state...");
    //         CmdRequestGameState();
    //     }
    //     else
    //     {
    //         Debug.Log("[CLIENT] Game not ready yet, skipping auto-request");
    //     }
    // }

    #endregion

    #region Turn RPC

    [Server]
    public bool ServerSetCurrentPlayer(PLAYERS player)
    {
        if (player != PLAYERS.PLAYER1 && player != PLAYERS.PLAYER2)
            return false;

        int turnId = (int)player;
        bool changed = currentPlayerIndex != turnId;
        currentPlayerIndex = turnId;
        if (MultiPlayerGameManagerTwelve.instance != null)
            MultiPlayerGameManagerTwelve.instance.ServerApplyTurnState(player);
        return changed;
    }

    [Server]
    public void ServerBroadcastCurrentTurn()
    {
        RpcSetTurnData(true, currentPlayerIndex);
    }

    // CmdSetTurnData(int) and CmdSetNodeOccupied(int, bool) were removed: clients could
    // choose the next turn or overwrite board occupancy. Only the server turn setter and
    // validated move transaction may change those values now.

    [ClientRpc]
    public void RpcSetTurnData(bool playerTurn, int turnId)
    {
        Debug.LogError($"[CLIENT] RpcSetTurnData -> turnId={turnId}");
        _lastRpcAppliedTurnId = turnId; // tell the SyncVar fallback it doesn't need to fire
        if (playerTurn)
        {
            if (MultiPlayerGameManagerTwelve.instance != null)
                MultiPlayerGameManagerTwelve.instance.SetPlayerTurn(turnId);
            else
                Debug.LogError("[RpcSetTurnData] MultiPlayerGameManagerTwelve missing!");
        }
    }
    #endregion

    //    #region GameState Save / Restore (Reconnect support)

    // // Server holds the saved game state
    // [SyncVar]
    // private string serverGameStateJson = null;

    // /// <summary>
    // /// Server auto-saves game state periodically
    // /// Called from MultiPlayerGameManagerTwelve.Update every 2 seconds
    // /// </summary>
    // [Server]
    // public void AutoSaveGameState()
    // {
    //     var controller = FindObjectOfType<GamePlayControllerTwelve>();

    //     // Only save if game controller exists and beads are created
    //     if (GamePlayControllerTwelve.beadsCreated)
    //     {
    //         return;
    //     }

    //     // Don't save if game hasn't started yet
    //     if (GamePlayControllerTwelve.currentPlayerTurn == PLAYERS.EMPTY)
    //     {
    //         Debug.LogWarning("[SERVER] Skipping save - game not started yet (Turn=EMPTY)");
    //         return;
    //     }

    //     try
    //     {
    //         // Get current game state
    //         GameStateData state = controller.SaveGameState();

    //         if (state == null)
    //         {
    //             Debug.LogWarning("[SERVER] SaveGameState returned null");
    //             return;
    //         }

    //         // Serialize to JSON
    //         string json = JsonUtility.ToJson(state);

    //         if (!string.IsNullOrEmpty(json))
    //         {
    //             serverGameStateJson = json;

    //             // Log what we saved (every 10 seconds to avoid spam)
    //             if (Time.frameCount % 600 == 0)
    //             {
    //                 Debug.Log($"[SERVER] ✅ Saved State: Turn={state.currentPlayerTurnHash}, P1={state.player1Score}, P2={state.player2Score}, Time={state.remainingTime}, T1={state.team1BeadStates.Count}, T2={state.team2BeadStates.Count}");
    //             }
    //         }
    //     }
    //     catch (System.Exception ex)
    //     {
    //         Debug.LogError($"[SERVER] Save failed: {ex}");
    //     }
    // }

    // /// <summary>
    // /// Client manually saves state (optional - mainly for debugging)
    // /// </summary>
    // [Command(requiresAuthority = false)]
    // public void CmdSaveGameState(string gameStateJson)
    // {
    //     if (string.IsNullOrEmpty(gameStateJson))
    //     {
    //         Debug.LogWarning("[SERVER] CmdSaveGameState - Empty JSON received");
    //         return;
    //     }

    //     serverGameStateJson = gameStateJson;
    //     Debug.Log($"[SERVER] Game state saved via Command (len={gameStateJson.Length})");
    // }

    // /// <summary>
    // /// Client requests saved state from server
    // /// Called when player reconnects
    // /// </summary>
    // [Command(requiresAuthority = false)]
    // public void CmdRequestGameState(NetworkConnectionToClient sender = null)
    // {
    //     Debug.Log($"🔥 [SERVER] CmdRequestGameState from client {sender?.connectionId}");

    //     if (string.IsNullOrEmpty(serverGameStateJson))
    //     {
    //         Debug.LogWarning("💀 [SERVER] No saved state available!");
    //         return;
    //     }

    //     // Parse and validate state before sending
    //     try
    //     {
    //         GameStateData state = JsonUtility.FromJson<GameStateData>(serverGameStateJson);

    //         if (state == null)
    //         {
    //             Debug.LogError("[SERVER] Failed to parse saved state JSON");
    //             return;
    //         }

    //         if (state.currentPlayerTurnHash == 0)
    //         {
    //             Debug.LogWarning($"[SERVER] Saved state has invalid turn (0) - not sending");
    //             return;
    //         }

    //         Debug.Log($"[SERVER] 📤 Sending state: Turn={state.currentPlayerTurnHash}, P1={state.player1Score}, P2={state.player2Score}, Time={state.remainingTime}");

    //         // Send to requesting client
    //         if (sender != null)
    //         {
    //             Debug.Log($"[SERVER] Sending game state to client {sender.connectionId}");
    //             TargetSendGameState(sender, serverGameStateJson);
    //         }
    //         else if (connectionToClient != null)
    //         {
    //             Debug.Log($"[SERVER] Sending game state to connectionToClient");
    //             TargetSendGameState(connectionToClient, serverGameStateJson);
    //         }
    //         else
    //         {
    //             Debug.LogError("[SERVER] No valid connection to send state to!");
    //         }
    //     }
    //     catch (System.Exception ex)
    //     {
    //         Debug.LogError($"[SERVER] Error validating saved state: {ex}");
    //     }
    // }

    // /// <summary>
    // /// Server sends state to specific client
    // /// </summary>
    // [TargetRpc]
    // public void TargetSendGameState(NetworkConnection target, string gameStateJson)
    // {
    //     Debug.Log("📦 [CLIENT] Received game state from server");

    //     if (string.IsNullOrEmpty(gameStateJson))
    //     {
    //         Debug.LogWarning("[CLIENT] Received empty JSON");
    //         return;
    //     }

    //     StartCoroutine(WaitAndRestoreState(gameStateJson));
    // }

    // /// <summary>
    // /// Wait for game controller to be ready, then restore
    // /// </summary>
    // private IEnumerator WaitAndRestoreState(string json)
    // {
    //     Debug.Log("[CLIENT] Waiting for GamePlayController...");

    //     float timeout = 3f;
    //     float elapsed = 0f;

    //     while (elapsed < timeout)
    //     {
    //         var controller = FindObjectOfType<GamePlayControllerTwelve>();

    //         if (controller != null && GamePlayControllerTwelve.beadsCreated)
    //         {
    //             Debug.Log("✅ [CLIENT] GamePlayController ready - Restoring state...");

    //             try
    //             {
    //                 GameStateData state = JsonUtility.FromJson<GameStateData>(json);

    //                 if (state != null)
    //                 {
    //                     Debug.Log($"[CLIENT] Parsed state: Turn={state.currentPlayerTurnHash}, P1={state.player1Score}, P2={state.player2Score}");
    //                     controller.RestoreGameState(state);
    //                     Debug.Log("🎉 [CLIENT] Game state restored successfully!");
    //                 }
    //                 else
    //                 {
    //                     Debug.LogError("[CLIENT] Failed to parse JSON to GameStateData");
    //                 }
    //             }
    //             catch (System.Exception ex)
    //             {
    //                 Debug.LogError($"[CLIENT] Restore error: {ex}");
    //             }

    //             yield break;
    //         }

    //         elapsed += 0.2f;
    //         yield return new WaitForSeconds(0.2f);
    //     }

    //     Debug.LogError("💀 [CLIENT] Timeout waiting for GamePlayController!");
    // }

    // #endregion

    #region Game Quit

    // Cmd_GameQuite(int PlayerId), Cmd_GameQuiteContinue() and their RPCs were removed.
    //
    // Both were [Command(requiresAuthority = false)], so ANY connected client could call them
    // with any player id: Cmd_GameQuite broadcast a made-up winner to both result panels and
    // then told Edgegap to tear the match down, and Cmd_GameQuiteContinue sent everyone back
    // to Home. Neither had a single caller — no C# reference, and no UnityEvent wiring in the
    // 12 Bead scenes or prefabs — so they were pure wire surface with no gameplay behind them.
    //
    // When the quit button is actually built, it must go through the server the same way the
    // rest of the result flow now does: a forfeit intent on the connection-owned player object
    // (default authority), with the server deriving the seat from `sender` and calling
    // MultiPlayerGameManagerTwelve.ServerFinalize(TwelveEndReason.Forfeit, winnerSeat).

    #endregion

    #region Waiting / Win Panels

    // Three unused waiting/win TargetRpcs were removed; they had no caller in either reconnect flow.

    #endregion
}
