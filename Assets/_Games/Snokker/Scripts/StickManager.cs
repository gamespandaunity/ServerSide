using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Twelve;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityExtensions;
using static mainScript;
public class StickManager : NetworkBehaviour
{
    public static StickManager instance;
    public Transform cueParentTransform;
    public Transform cueObjectTransform;
    public Transform cueGroupTransform;
    public Transform cueSetPosTransform;
    public Transform cueShadowTransform;
    public GameObject cueShadowMesh;
    public GameObject cueObjArray;
    public Transform nettransform;
    public float NetworkedCueRotX;
    public float NetworkedCueRotY;
    public Vector3 NetworkedCueBallPos;
    public int currentPlayerIndex = 0;
    [SyncVar]
    public float turnTimer;
    [SyncVar]
    public bool isTimerRunning = false;
    [SyncVar(hook = nameof(OnServerTossDone))]
    public string NetorkedTurn = string.Empty;
    public readonly SyncList<bool> ballsActiveStatus = new SyncList<bool>();


    //[SyncVar(hook = nameof(OnTurnChanged))]
    //public string CurrentTurn = string.Empty;
    void OnServerTossDone(string oldValue, string newValue)
    {
        this.DelayUntil(() => SnokerNetwork.Instance._SnokerGameManager._SnokerCueBall.initialized, () =>
        {
            if (SnokerNetwork.isTossDone == false)
            {
                SnokerNetwork.Instance.NotifyTossResult(NetorkedTurn);
                Debug.LogError("toss");

            }
            else
            {
                Debug.LogError("Cmd Current turn");
                SnokerNetwork.Instance._SnokerGameManager._SnokerCameraManager.blurGameView(true);
                CmdGetCurrentTurn();
                this.Delay(2, () =>
                {
                    CmdStartTurnTimer();
                });
                this.Delay(2, () =>
                {
                    Debug.LogError(SnokerGameManager.currentTurn != staticVariables.UserProfiledata.user._id.ToString());
                    if (SnokerGameManager.currentTurn != staticVariables.UserProfiledata.user._id.ToString())
                    {
                        CmdResetTurnTimer();
                    }
                });
            }
        });

    }

    void OnTurnChanged(string oldValue, string newValue)
    {
        CmdNextTurn(newValue);
    }
    private void Awake()
    {
        instance = this;
        ballsActiveStatus.Clear();

        // Initialize 21 elements to false
        for (int i = 0; i < 21; i++)
        {
            ballsActiveStatus.Add(false);
        }

        ballsActiveStatus.OnSet += OnItemChanged;

    }
    private void OnDisable()
    {
        ballsActiveStatus.OnSet += OnItemChanged;
    }
    void OnItemChanged(int index, bool oldValue)
    {
        this.Delay(1.2f, () =>
        {
            SnokerNetwork.Instance._SnokerGameManager.ballsArray[index].SetActive(ballsActiveStatus[index]);
        }
        );
    }

    public void Start()
    {

        // SnokerNetwork.Instance.FusionSyncObjects[0] = GetComponent<Fusion.NetworkObject>();
        SnokerNetwork.Instance._mainScript.cueParentObjTransform = cueParentTransform;
        SnokerNetwork.Instance._mainScript.cueObjectTransform = cueObjectTransform;
        SnokerNetwork.Instance._mainScript.cueGroupTransform = cueGroupTransform;
        SnokerNetwork.Instance._mainScript.cueSetPosTransform = cueSetPosTransform;
        SnokerNetwork.Instance._mainScript.cueShadowTransform = cueShadowTransform;
        SnokerNetwork.Instance._mainScript.cueShadowMesh = cueShadowMesh;
        SnokerNetwork.Instance._mainScript._SnokerGameManager.cuesObjArray = cueObjArray;
        SnokerNetwork.Instance._mainScript.spawned = true;
        SnokerNetwork.Instance._mainScript.start();

    }
    public override void OnStartServer()
    {
        base.OnStartServer();

        bool AreAllClientsReady()
        {
            if (SceneManager.GetActiveScene().name == "Home") return false;
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (!conn.isReady)
                {
                    return false;
                }
            }
            return NetworkServer.connections.Count > 0; // Make sure we have clients
        }

        this.DelayUntil(() => AreAllClientsReady(), () =>
        {
            SnokerNetwork.Instance.DoToss();

        });

    }
    public override void OnStartClient()
    {
        this.Delay(2, () =>
        {
            for (int index = 0; index < SnokerNetwork.Instance._SnokerGameManager.ballsArray.Length; index++)
            {
                SnokerNetwork.Instance._SnokerGameManager.ballsArray[index].SetActive(ballsActiveStatus[index]);
            }

            // cueBallParentObj is NOT in ballsArray — it's managed separately.
            // start() called activateBalls(false) which deactivated it on the client.
            // The server re-activates it via startGame(), but that path is never called
            // on the client in multiplayer. Activate it explicitly here.
            SnokerNetwork.Instance._mainScript.cueBallParentObj.SetActive(true);
        });
    }


    [Command(requiresAuthority = false)]
    public void CmdStartTurnTimer()
    {
        // On a dedicated server a ClientRpc body never runs server-side, so this Cmd used to arm the timer on
        // the CLIENTS only — after a reconnect the server had no running timer of its own. With the timeout
        // auto-hit now server-only (SnokerNetwork.TurnTimer), the authority must be armed whenever the clients
        // are, or a timed-out turn would simply hang.
        SnokerNetwork.Instance.StartTurnTimer();
        RpcStartTurnTimer();
    }
    [ClientRpc]
    public void RpcStartTurnTimer()
    {
        SnokerNetwork.Instance.StartTurnTimer();
    }
    [ClientRpc]
    public void RpcshowNotification(string text, float time)
    {
        SnokerNetwork.Instance._SnokerUIManager.showNotification(text, time);
    }



    [ClientRpc]
    public void RpcChangeturnrpc(string turn, bool cueBallPotted, bool firstballTouched, bool TurnChanged, int val)
    {
        SnokerNetwork.Instance.RPC_Changeturnrpc(turn, cueBallPotted, firstballTouched, TurnChanged, val);
    }
    [Command(requiresAuthority = false)]
    public void CmdNextTurn(string turn, NetworkConnectionToClient sender = null)
    {
        // This runs on server
        NextTurn(turn);
    }
    [Command(requiresAuthority = false)]
    public void CmdGetCurrentTurn()
    {
        this.Delay(3, () =>
        {
            // This runs on server   
            SnokerGameManager.bBallInHand.Show("Ball in hand Server");

            //StartCoroutine(CheckGameStatus());
            RpcSyncAllPlayerData(SnokerGameManager.currentTurn, SnokerGameManager.bBallInHand, SnokerNetwork.Instance._SnokerGameManager.ballIsStanding, SnokerNetwork.Instance._SnokerGameManager.bTossDone, SnokerGameManager.snookerRedPottedCount, (int)SnokerGameManager.snookerTargetBall, mainScript.foulInThisTurn);
            AssignStickAuthority();
        });

        // NextTurn(SnokerGameManager.currentTurn);
        //SnokerNetwork.Instance.NextTurn(SnokerGameManager.currentTurn);
    }
    //public IEnumerator CheckGameStatus()
    //{
    //    int count = 0;
    //    while (count < 30)
    //    {
    //        yield return new WaitForSecondsRealtime(2f);
    //        if (!string.IsNullOrEmpty(winnerID))
    //        {
    //            GameWinnerId(winnerID, true);
    //        }
    //        count++;
    //    }
    //}

    [Server]
    public void NextTurn(string turn)
    {
        // StartCoroutine(CheckGameStatus());

        string nextTurn = turn == NetworkGameManager.Instance.creatorData.playerId
            ? NetworkGameManager.Instance.joinerData.playerId
            : NetworkGameManager.Instance.creatorData.playerId;

        currentPlayerIndex = turn == NetworkGameManager.Instance.creatorData.playerId ? 1 : 0;

        SnokerNetwork.Instance.NextTurn(nextTurn);
        AssignStickAuthority();
        Vector3[] BallsPositons = new Vector3[SnokerNetwork.Instance._SnokerGameManager.ballsArray.Length];
        int i = 0;
        foreach (GameObject pos in SnokerNetwork.Instance._SnokerGameManager.ballsArray)
        {
            BallsPositons[i] = pos.transform.position;
            i++;
        }
        RpcOnTurnChanged(BallsPositons);
    }
    [ClientRpc]
    void RpcOnTurnChanged(Vector3[] ballspos)
    {

        int i = 0;
        foreach (GameObject pos in SnokerNetwork.Instance._SnokerGameManager.ballsArray)
        {
            pos.transform.position = ballspos[i];
            StartCoroutine(ResetRigidbody(pos, pos.GetComponent<Rigidbody>().isKinematic));
            i++;
        }
        IEnumerator ResetRigidbody(GameObject pos, bool state)
        {
            pos.GetComponent<Rigidbody>().isKinematic = !state;
            yield return new WaitForSecondsRealtime(0.5f);
            pos.GetComponent<Rigidbody>().isKinematic = state;

        }
    }

    void OnCurrentPlayerChanged(int oldIndex, int newIndex)
    {
        // SyncVar hook - called when currentPlayerIndex changes
        Debug.Log($"Turn changed from {oldIndex} to {newIndex}");
    }
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Debug.Log("Authoriy is Assigned");
    }
    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        Debug.Log("Authoriy is Removed");

    }
    public void AssignStickAuthority()
    {
        if (this == null || !gameObject) return;

        var stickIdentity = GetComponent<NetworkIdentity>();

        // Remove current authority
        if (stickIdentity.connectionToClient != null)
            stickIdentity.RemoveClientAuthority();
        Debug.Log("creator : " + NetworkGameManager.Instance.CreatorRef.connectionId);
        Debug.Log("Joiner : " + NetworkGameManager.Instance.JoinerRef.connectionId);
        // Assign to current player
        bool isCreatorTurn = SnokerGameManager.currentTurn == NetworkGameManager.Instance.creatorData.playerId;
        var targetConnection = isCreatorTurn
            ? NetworkGameManager.Instance.CreatorRef
            : NetworkGameManager.Instance.JoinerRef;

        stickIdentity.AssignClientAuthority(targetConnection);

    }
    public void UpdateCueBallAuthority(bool cue)
    {
        cue.Show("Update Cue Ball Authority");
        if (cue)
        {
            if (SnokerNetwork.Instance._mainScript.GetComponent<NetworkIdentity>().connectionToClient != null)
                SnokerNetwork.Instance._mainScript.GetComponent<NetworkIdentity>().RemoveClientAuthority();
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkIdentity>().AssignClientAuthority(SnokerGameManager.currentTurn == NetworkGameManager.Instance.joinerData.playerId ? NetworkGameManager.Instance.JoinerRef : NetworkGameManager.Instance.CreatorRef);
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkRigidbodyUnreliable>().syncDirection = SyncDirection.ClientToServer;
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkTransformUnreliable>().syncDirection = SyncDirection.ClientToServer;
        }
        else
        {
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkIdentity>().RemoveClientAuthority();
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkRigidbodyUnreliable>().syncDirection = SyncDirection.ServerToClient;
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkTransformUnreliable>().syncDirection = SyncDirection.ServerToClient;

        }
    }
    public GuideLineData Guidedata;
    // ClientRpc - Mirror automatically calls this on all clients
    [ClientRpc]
    public void RpcUpdateGuideLine(GuideLineData data)
    {
        Guidedata = data;
        SnokerNetwork.Instance._mainScript.ApplyGuideLineVisuals(data);
    }

    [Command(requiresAuthority = false)]
    public void CmdOnClickCuePlaced()
    {
        SnokerNetwork.Instance._mainScript.onClickPlaceCueOkBtn();

        SnokerNetwork.Instance.RpcOnClickCuePlaced();
        // Server receives the request
        RpcOnClickCuePlaced();
    }

    [ClientRpc]
    public void RpcOnClickCuePlaced()
    {
        SnokerNetwork.Instance.RpcOnClickCuePlaced();
    }

    [Command(requiresAuthority = false)]
    public void CmdExecuteBallHit(
       float shotPower,
       Vector3 direction,
       Vector3 cueBallReboundVector,
       Vector3 cueballpos,
       Vector3 lastTargetVector,
       int targetBall,
       Vector3 cuestickpos,
       Vector3 x,
       Vector3 y)
    {
        // Server forwards to all clients
        Debug.LogError("execute hit");
        SnokerNetwork.Instance.ExecuteHit(shotPower, direction, cueBallReboundVector, cueballpos,
                              lastTargetVector, targetBall, cuestickpos, x, y);


    }
    public void ExecuteBallHit(
       float shotPower,
       Vector3 direction,
       Vector3 cueBallReboundVector,
       Vector3 cueballpos,
       Vector3 lastTargetVector,
       int targetBall,
       Vector3 cuestickpos,
       Vector3 x,
       Vector3 y)
    {
        // Server forwards to all clients
        Debug.LogError("execute hit");
        SnokerNetwork.Instance.ExecuteHit(shotPower, direction, cueBallReboundVector, cueballpos,
                              lastTargetVector, targetBall, cuestickpos, x, y);


    }

    [ClientRpc]
    public void RpcExecuteBallHit(float shotPower, Vector3 direction, Vector3 cueBallReboundVector, Vector3 cueballpos, Vector3 lastTargetVector, int TargetBall, Vector3 cuestickpos, Vector3 x, Vector3 y)
    {
        SnokerNetwork.Instance.RpcExecuteBallHit(shotPower, direction, cueBallReboundVector, cueballpos,
                              lastTargetVector, TargetBall, cuestickpos, x, y);
    }

    [ClientRpc]
    public void RespotRpc(int index, Vector3 pos)
    {
        StartCoroutine(resetball(index, pos, 0.2f));
        StartCoroutine(resetball(index, pos, 0.5f));
    }
    IEnumerator resetball(Index i, Vector3 pos, float delay)
    {
        SnokerNetwork.Instance._SnokerGameManager.ballsArray[i].SetActive(false);
        yield return new WaitForSecondsRealtime(delay);
        SnokerNetwork.Instance._SnokerGameManager.ballsArray[i].transform.position = pos;
        SnokerNetwork.Instance._SnokerGameManager.ballsRigidbodyArray[i].linearVelocity = Vector3.zero;
        SnokerNetwork.Instance._SnokerGameManager.ballsRigidbodyArray[i].constraints |= RigidbodyConstraints.FreezePositionY;
        yield return new WaitForSecondsRealtime(0.2f);

        SnokerNetwork.Instance._SnokerGameManager.ballsArray[i].SetActive(true);

    }
    public void ChangeScoreText(string toPlayer, int val)
    {
        SnokerNetwork.Instance.RpcChangeScoreText(toPlayer, val);
    }
    [ClientRpc]
    public void RpcUpdateScoreText(int creatorscore, int joinerscore)
    {
        SnokerNetwork.Instance.OnScoreSync(creatorscore, joinerscore);
    }


    public void CueBallPotted()
    {
        SnokerNetwork.Instance.RpcCueBallPotted();
        RpcCueBallPotted(); // server broadcasts to all clients
    }
    [ClientRpc]
    public void RpcCueBallPotted()
    {
        SnokerNetwork.Instance.RpcCueBallPotted();
    }


    [Command(requiresAuthority = false)]
    public void CmdSetBallInHand(bool ballInHand)
    {
        SetBallInHand(ballInHand);
        SnokerNetwork.Instance.RpcSetBallInHand(ballInHand);
        RpcSetBallInHand(ballInHand);
    }
    public void SetBallInHand(bool ballInHand)
    {
        UpdateCueBallAuthority(ballInHand);
    }

    [ClientRpc]
    public void RpcSetBallInHand(bool ballInHand)
    {
        SnokerNetwork.Instance.RpcSetBallInHand(ballInHand);
    }
    // Reconnect hardening (2026-07-27, tester "game stuck after disconnection", build 1.8.2):
    // this state-restore RPC used to dereference SnokerNetwork.Instance._SnokerGameManager /
    // ._mainScript / ._SnokerCameraManager and NetworkGameManager.Instance.creatorData|joinerData
    // with NO null guards. On a RECONNECT the scene has just reloaded, so those refs can still be
    // unwired when the RPC lands (the server sends it 3s after CmdGetCurrentTurn) — the handler then
    // threw a NullReferenceException INSIDE Mirror's RPC dispatch and ABORTED the restore half-way.
    // Because blurGameView(false) was the LAST statement, any earlier null left the table BLURRED
    // with no turn set = the "stuck" the tester sees. Snooker's own code did not change; the SHARED
    // reconnect layer's timing did, so the fix lives here (shared _Network / Mirror stay LOCKED).
    private Coroutine _syncAllPlayerDataCo;

    [ClientRpc]
    public void RpcSyncAllPlayerData(string currentTurn, bool ballInHand, bool ballStanding, bool tossDone, int snookerRedPottedCount, int snookerTargetBall, bool foulInThisTurn)
    {
        // Restart (never stack) the bounded wait so a re-sent sync always wins and we can't leak
        // two concurrent retries.
        if (_syncAllPlayerDataCo != null) StopCoroutine(_syncAllPlayerDataCo);
        _syncAllPlayerDataCo = StartCoroutine(ApplySyncAllPlayerDataWhenReady(
            currentTurn, ballInHand, ballStanding, tossDone, snookerRedPottedCount, snookerTargetBall, foulInThisTurn));
    }

    // True only when EVERY reference the restore below dereferences actually exists.
    private static bool SnookerSyncRefsReady()
    {
        return SnokerNetwork.Instance != null
            && SnokerNetwork.Instance._SnokerGameManager != null
            && SnokerNetwork.Instance._SnokerGameManager._SnokerCameraManager != null
            && SnokerNetwork.Instance._mainScript != null
            && NetworkGameManager.Instance != null
            && NetworkGameManager.Instance.creatorData != null
            && NetworkGameManager.Instance.joinerData != null;
    }

    private IEnumerator ApplySyncAllPlayerDataWhenReady(string currentTurn, bool ballInHand, bool ballStanding, bool tossDone, int snookerRedPottedCount, int snookerTargetBall, bool foulInThisTurn)
    {
        // Bounded wait for the reloaded scene to finish wiring. Realtime so it still ticks if the
        // reconnect lands while the game clock is paused/slow. On the NORMAL first-join path the refs
        // are already up, so this exits on the first check and behaviour is unchanged.
        float deadline = Time.realtimeSinceStartup + 8f;
        while (!SnookerSyncRefsReady() && Time.realtimeSinceStartup < deadline)
        {
            if (this == null) yield break;              // our object was destroyed mid-wait
            yield return new WaitForSecondsRealtime(0.1f);
        }
        if (this == null) yield break;
        _syncAllPlayerDataCo = null;
        // Apply best-effort even if the wait timed out — every step is individually guarded, so we
        // restore as much as possible instead of silently doing nothing.
        ApplySyncAllPlayerData(currentTurn, ballInHand, ballStanding, tossDone, snookerRedPottedCount, snookerTargetBall, foulInThisTurn);
    }

    // Per-step guarded apply, original order preserved EXCEPT the un-blur is hoisted to the front:
    // it is the one step that must never be skipped, since leaving it undone is what makes the table
    // look frozen. Idempotent — every step just re-assigns already-authoritative synced state, so a
    // retry (or a second CmdGetCurrentTurn) re-applies the same values harmlessly.
    private void ApplySyncAllPlayerData(string currentTurn, bool ballInHand, bool ballStanding, bool tossDone, int snookerRedPottedCount, int snookerTargetBall, bool foulInThisTurn)
    {
        SnokerNetwork sn = SnokerNetwork.Instance;
        if (sn == null)
        {
            Debug.LogWarning("[Snooker][RpcSyncAllPlayerData] SnokerNetwork.Instance still null after wait — state restore skipped.");
            return;
        }
        SnokerGameManager gm = sn._SnokerGameManager;

        ballInHand.Show("Ball in hand Client");

        // Each step runs ISOLATED (SafeStep). The readiness gate can only check the refs THIS method
        // touches — but the SnokerNetwork calls below dereference further nested state of their own
        // (FetchCurrentTurnStatus -> _mainScript.thisRigidbody/.igSnookerTurnIndicator, OnScoreSync ->
        // MirrorNetwork.Instance/_mainScript.snookerScoresText[], SetBallInHandUI ->
        // staticVariables.UserProfiledata.user). Without isolation, ONE of those throwing would again
        // skip every later step — the exact half-applied-restore failure we are fixing. Failures are
        // LOGGED per step (never silently swallowed) so a real bug is still visible in the log.
        SafeStep("unblur", () => { if (gm != null && gm._SnokerCameraManager != null) gm._SnokerCameraManager.blurGameView(false); });
        SafeStep("turn", () => sn.FetchCurrentTurnStatus(currentTurn));
        SafeStep("ballInHand", () => sn.RpcSetBallInHand(ballInHand));
        SafeStep("scores", () =>
        {
            if (NetworkGameManager.Instance != null
                && NetworkGameManager.Instance.creatorData != null
                && NetworkGameManager.Instance.joinerData != null)
                sn.OnScoreSync(NetworkGameManager.Instance.creatorData.Scores, NetworkGameManager.Instance.joinerData.Scores);
        });
        SafeStep("tableState", () =>
        {
            if (gm != null)
            {
                gm.ballIsStanding = ballStanding;
                gm.bTossDone = tossDone;
            }
            SnokerGameManager.snookerRedPottedCount = snookerRedPottedCount;
            mainScript.foulInThisTurn = foulInThisTurn;
        });
        SafeStep("targetBall", () => { if (sn._mainScript != null) sn._mainScript.snookerSetTargetBAll(snookerTargetBall); });
        SafeStep("coinStatus", () => { if (gm != null) gm.updateCoinStatus(); });
        // Cue restore is gated on the SYNCED ballStanding: SetBallInHandUI's first act is
        // toggleSelectedCue(!ballInHand), which showed the stick immediately even when the reconnect landed
        // mid-shot with balls still rolling ("stick with moving ball"). The server already sends ballStanding
        // in this very RPC; when the balls are moving we simply defer — the ball-stop watcher in
        // mainScript ("All balls stopped -> resetting cue") re-shows the cue through the normal turn flow,
        // and bBallInHand itself was already restored by the earlier "ballInHand" step.
        SafeStep("ballInHandUI", () =>
        {
            if (ballStanding)
                sn.SetBallInHandUI(ballInHand);
            else
                Debug.Log("[Snooker][RpcSyncAllPlayerData] cue restore deferred — balls still moving; the ball-stop watcher will show it.");
        });
        SafeStep("guideColor", () => { if (sn._mainScript != null) sn._mainScript.setGuideColorRed(); });
    }

    // Runs one restore step in isolation so a throw inside it cannot abort the remaining steps.
    // Deliberately LOGS (does not hide) the failure, tagged with the step name for diagnosis.
    private static void SafeStep(string label, Action step)
    {
        try { step(); }
        catch (Exception e)
        {
            Debug.LogWarning($"[Snooker][RpcSyncAllPlayerData] step '{label}' failed, continuing with the rest: {e.Message}");
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdResetTurnTimer()
    {
        Debug.LogError("Reset Turn Timer Cmd");
        SnokerNetwork.Instance.StartTurnTimer();
    }


    public void showNotificationRpc(string text, bool isFoul)
    {
        RpcShowNotification(text, isFoul);
    }
    [Command(requiresAuthority = false)]
    public void CmdShowNotification(string text, bool isFoul)
    {
        RpcShowNotification(text, isFoul);
    }
    [ClientRpc]
    public void RpcShowNotification(string text, bool isFoul)
    {
        SnokerNetwork.Instance.RpcShowNotification(text, isFoul);
    }
    [Command(requiresAuthority = false)]
    public void CmdAnnounceVictory(int playerId, string reaosn)
    {
        SnookerFlow.SendResult(playerId.ToString(), "victory announced: " + reaosn);
        ApiAndRoomManager._instance.WinnerLossChallenge(playerId.ToString());
        RpcAnnounceVictory(playerId, reaosn);
        NetworkGameManager.Instance.creatorData.Scores = 0;
        NetworkGameManager.Instance.joinerData.Scores = 0;
        if (SnokerNetwork.Instance.TimerCorotine != null)
            StopCoroutine(SnokerNetwork.Instance.TimerCorotine);
        this.Delay(5, () =>
        {
            "Destroy After Delay".Show();
            NetworkGameManager.Instance.currentServerRequestId.Show("Current Server Id");
            MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
            //RpcCleanUp(NetworkGameManager.Instance.requestId);
        });
    }

    [ClientRpc]
    public void RpcAnnounceVictory(int playerId, string reason)
    {
        SnokerNetwork.Instance.Victory(playerId, reason);

    }
    [Server]
    public void GameWinnerId(string playerId, bool DueToDisconnect)
    {
        SnookerFlow.SendResult(playerId, DueToDisconnect ? "opponent disconnected" : "game over");
        NetworkGameManager.Instance.creatorData.Scores = 0;
        NetworkGameManager.Instance.joinerData.Scores = 0;
        ApiAndRoomManager._instance.WinnerLossChallenge(playerId.ToString());
        RpcGameWinnerId(playerId, DueToDisconnect);
        this.Delay(5, () =>
        {
            "Destroy After Delay".Show();
            NetworkGameManager.Instance.currentServerRequestId.Show("Current Server Id");
            MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
            //RpcCleanUp(NetworkGameManager.Instance.requestId);
        });
    }

    [ClientRpc]
    public void RpcGameWinnerId(string playerId, bool DueToDisconnect)
    {
        SnokerNetwork.Instance._mainScript.NetworkWinner(playerId, DueToDisconnect);
    }

    [Command(requiresAuthority = false)]
    public void CmdCheckRematchStatus(string playerId)
    {
        RpcCheckRematchStatus(playerId, NetworkServer.connections.Count);
    }
    [ClientRpc]
    public void RpcCheckRematchStatus(string playerId, int players)
    {
        if (playerId == staticVariables.UserProfiledata.user._id.ToString())
        {
            ResultManager.instance.RematchStatus(players);
            // GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");
            // if (enemyPrefab != null)
            // {
            //     // Spawn at position (0,0,0)
            //     var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
            //     gameObject.GetComponent<ResultManager>().RematchStatus(players);
            // }
            // else
            // {
            //     Debug.LogError("WinLose GameManager prefab not found!");
            // }
        }

    }
}

public struct InputData
{
    public float cueRotationX;
    public float cueRotationY;
    public Vector3 cueballpos;
    public bool hasBallInHand;
}
