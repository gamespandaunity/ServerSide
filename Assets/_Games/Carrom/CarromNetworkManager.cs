using BEKStudio;
using Mirror;
using Shapes2D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Twelve;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExtensions;

public class CarromNetworkManager : NetworkBehaviour
{
    public static CarromNetworkManager instance;
    public int currentPlayerIndex = 0;
    public GameObject playerPuck;
    public List<NetworkConnectionToClient> players = new List<NetworkConnectionToClient>();
    [SyncVar]
    public string CurrentpuckTag;
    public bool playersSet = false;
    private float timer = 0f;

    [SerializeField] private float totalTime = Constants.PLAY_TIME_FOR_PLAYER; // or set desired default
    private Coroutine timerCoroutine = null;
    private bool isTimerRunning = false;

    // Server-only: who took the most recent shot. The shot-path turn switch must move
    // the turn FROM the shooter — not from whatever PlayercurrentTurnId holds once the
    // settle logic runs, because the shooter-client's racing Cmd may have switched it already.
    private string lastShooterId;

    // Set by the server when the match concludes (win/lose/disconnect-forfeit).
    // Synced so client-side Rpc handlers can also bail out instead of cycling
    // the turn timer or running another turn switch on top of the result UI.
    [SyncVar]
    public bool gameEnded;

    [Server]
    public void StopAllTurnLogic()
    {
        if (gameEnded) return;
        gameEnded = true;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        isTimerRunning = false;
    }
    [SyncVar(hook = nameof(onTurnChanged))]
    public string currentNetworkTurn = string.Empty;
    #region Game Timer Syncing
    [Header("UI Timer")]
    //public TextMeshProUGUI countDownGameTimer;

    [Header("Countdown Settings")]
    [SyncVar(hook = nameof(OnTimeChanged))]
    public int remainingTime = 600; // server owns this

    public int remainingminutes;
    public int remainingseconds;

    private Coroutine countdownCoroutine;
    private Coroutine initialTurnCoroutine;

    [Server]
    private IEnumerator ServerCountdown()
    {
        remainingTime = 600;
        Debug.Log("Server Countdown Started." + remainingTime);
        while (remainingTime > 0)
        {
            if (gameEnded) yield break;
            if (!NetworkGameManager.Instance.IsPaused)
                remainingTime--; // SyncVar updates all clients automatically
            yield return new WaitForSeconds(1f);
        }
        if (gameEnded) yield break;
        // Winner by the board: the creator plays White (home score), the joiner Black (away score) — see the colour
        // rule further down. NetworkGameManager's creator/joiner Scores are never updated by Carrom, so they always
        // said 0–0 and the creator won every timed-out match. Level → the creator, as before.
        int whiteScore = BEKStudio.GameController.currentHomeScore, blackScore = BEKStudio.GameController.currentAwayScore;
        BEKStudio.GameController.Instance.gameWinner = blackScore > whiteScore ? NetworkGameManager.Instance.joinerData.playerId : NetworkGameManager.Instance.creatorData.playerId;
        MatchFlow.Log("Carrom", $"match time over — board {BEKStudio.GameController.FlowScores()}" + (whiteScore == blackScore ? ", level — creator wins the tie" : ""));
        MatchFlow.SendResult(BEKStudio.GameController.Instance.gameWinner, "match time over", BEKStudio.GameController.FlowScores());
        BEKStudio.GameController.currentHomeScore = 0;
        BEKStudio.GameController.currentAwayScore = 0;
        StopAllTurnLogic();
        ApiAndRoomManager._instance.WinnerLossChallenge(BEKStudio.GameController.Instance.gameWinner);
        RpcTimerEnded(BEKStudio.GameController.Instance.gameWinner);
    }

    // This runs on all clients whenever remainingTime changes
    private void OnTimeChanged(int oldTime, int newTime)
    {
        remainingminutes = Mathf.FloorToInt(newTime / 60);
        remainingseconds = Mathf.FloorToInt(newTime % 60);
        BEKStudio.GameController.Instance.countDownGameTimer.text = string.Format("{0:00}:{1:00}", remainingminutes, remainingseconds);

        if (newTime > 180)
            BEKStudio.GameController.Instance.countDownGameTimer.color = Color.green;
        else if (newTime > 60)
            BEKStudio.GameController.Instance.countDownGameTimer.color = Color.white;
        else
            BEKStudio.GameController.Instance.countDownGameTimer.color = Color.red;
    }

    [ClientRpc]
    private void RpcTimerEnded(string winnerID)
    {
        BEKStudio.GameController.Instance.countDownGameTimer.text = "00:00";
        BEKStudio.GameController.Instance.countDownGameTimer.color = Color.red;
        // _mainScript.NetworkWinner(winnerID, timerEnd: true);
        CmdGameStatelose(staticVariables.UserProfiledata.user._id);
    }
    #endregion
    void Awake()
    {
        instance = this;

    }

    private void OnEnable()
    {

        MirrorNetwork.OnWinCall += AnnounceVictory;
        // MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
        // MirrorNetwork.OnGameStateChanged += _SnokerGameManager.PauseUnpauseGame;
    }

    private void OnDisable()
    {
        MirrorNetwork.OnWinCall -= AnnounceVictory;
        //MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
        // MirrorNetwork.OnGameStateChanged -= _SnokerGameManager.PauseUnpauseGame;
    }
    public void AnnounceVictory(string reason)
    {
        // OnWinCall fires on the local (still-connected) player when the
        // 30 s waiting-panel timeout expires. Notify the server so both
        // clients receive the win Rpc and spawn the result UI — without
        // this the opponent never sees the Lose screen when they reconnect.
        // Mirrors 12 Beads' AnnounceWinner -> CmdWinPlayer pattern.
        CmdAnnounceDisconnectWin(staticVariables.UserProfiledata.user._id.ToString());
    }

    [Command(requiresAuthority = false)]
    public void CmdAnnounceDisconnectWin(string winnerId)
    {
        MatchFlow.Log("Carrom", $"opponent of {MatchFlow.Who(winnerId)} disconnected and did not return");
        MatchFlow.SendResult(winnerId, "opponent disconnected", BEKStudio.GameController.FlowScores());
        StopAllTurnLogic();
        RpcHandleDisconnectWin(winnerId);
        this.Delay(1, () =>
        {
            if (MirrorNetwork.Instance != null
                && MirrorNetwork.Instance.edgegapAPIClient != null
                && NetworkGameManager.Instance != null)
            {
                MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(
                    NetworkGameManager.Instance.currentServerRequestId);
            }
        });
    }

    [ClientRpc]
    public void RpcHandleDisconnectWin(string winnerId)
    {
        if (ResultManager.GameSpawnedFinished) return;
        ResultManager.GameSpawnedFinished = true;
        GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
        if (prefab == null)
        {
            Debug.LogError("WinLose GameManager prefab not found!");
            return;
        }
        // HandleGameResultAltMultiplayer compares winnerId against the local
        // user id to decide Win vs Lose, so the same Rpc payload correctly
        // shows the Win UI on the winner and the Lose UI on the opponent.
        var go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        go.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, winnerId);
    }
    public void onTurnChanged(string oldVal, string newVal)
    {
        if (BEKStudio.GameController.isGameStarted)
        {
            this.Delay(2, () =>
            {
                CmdSetGameValuesFromServer();
            });
        }
    }
    [Command(requiresAuthority = false)]
    public void CmdSetGameValuesFromServer()
    {
        Rpc_ShowPlayer();
        string[] homeNames = BEKStudio.GameController.Instance.homePucksCollected.Select(go => go.name).ToArray();
        string[] awayNames = BEKStudio.GameController.Instance.awayPucksCollected.Select(go => go.name).ToArray();
        RpcApplyDataFromServer(homeNames, awayNames, BEKStudio.GameController.currentHomeScore, BEKStudio.GameController.currentAwayScore);


    }
    [ClientRpc]
    public void RpcApplyDataFromServer(string[] Master, string[] Joiner, int masterCount, int joinerCount)
    {
        var controller = BEKStudio.GameController.Instance;
        controller.homePucksCollected.Clear();
        controller.awayPucksCollected.Clear();


        BEKStudio.GameController.currentHomeScore = masterCount;
        BEKStudio.GameController.currentAwayScore = joinerCount;
        BEKStudio.GameController.currentHomeScore.Show("Current Home Score:");
        BEKStudio.GameController.currentAwayScore.Show("Current Away Score:");

        // Reconstruct home pucks list
        foreach (string name in Master)
        {
            GameObject obj = controller.allPucks?.FirstOrDefault(
                    puck => puck != null && puck.name == name)?.gameObject
                ?? GameObject.Find(name);
            if (obj != null)
            {
                controller.homePucksCollected.Add(obj);
                RestoreCollectedPuckState(obj);
            }
            else
            {
                Debug.LogWarning($"GameObject '{name}' not found for homePucksCollected");
            }
        }

        // Reconstruct away pucks list
        foreach (string name in Joiner)
        {
            GameObject obj = controller.allPucks?.FirstOrDefault(
                    puck => puck != null && puck.name == name)?.gameObject
                ?? GameObject.Find(name);
            if (obj != null)
            {
                controller.awayPucksCollected.Add(obj);
                RestoreCollectedPuckState(obj);
            }
            else
            {
                Debug.LogWarning($"GameObject '{name}' not found for awayPucksCollected");
            }
        }
        controller.ScoreText();
    }

    private static void RestoreCollectedPuckState(GameObject obj)
    {
        var puck = obj != null ? obj.GetComponent<BEKStudio.Puck>() : null;
        if (puck == null)
        {
            Debug.LogWarning($"Collected carrom object '{obj?.name}' has no Puck component.");
            return;
        }

        // On reconnect the collected-name/score RPC can arrive before the puckInHole
        // SyncVars on the rebuilt scene. CheckTurn immediately runs HandleWorstCase,
        // which removes every collected item whose puckInHole is still false and then
        // recalculates the score back to zero. The server's collected lists are the
        // authoritative snapshot, so apply their physical/visual state before that pass.
        puck.OnDisableProperties(true);
    }
    void Start()
    {
        if (playerPuck == null)
        {
            playerPuck = FindAnyObjectByType<PlayerPuck>().gameObject;
            Debug.LogWarning("PlayerPuck Found in Scene");
        }
        else
        {
            Debug.LogWarning("PlayerPuck not found in the scene.");
        }

        // A finished PREVIOUS match leaves ResultManager.isGameFinished == true (static).
        // The shared NetworkGameManager then refuses the waiting panel for carrom
        // (ShouldPauseGame -> false) and MirrorNetwork refuses to reconnect — so a disconnect
        // in the NEXT carrom match showed NO waiting panel, the 30s timer never ran, OnWinCall
        // never fired, and no win/lose result appeared. A carrom scene loading with
        // isGameStarted == false is a FRESH match: reset the per-match flags here.
        // (8-ball got the identical fix in MyEightBallNetwork.OnStartClient.)
        if (!BEKStudio.GameController.isGameStarted)
        {
            ResultManager.isGameFinished = false;
            ResultManager.GameSpawnedFinished = false;
        }

        // ---- RECONNECT RESYNC ----
        // GameController.isGameStarted is a static that survives the scene reload
        // triggered by Mirror's reconnect flow. On a fresh game start it is false
        // here and only set to true later (after the start banner). So this branch
        // uniquely identifies a reconnect — ask the server to push the current
        // turn state back so PlayerPuck.ResetPosition() runs with the correct
        // CurrentTurn and the striker snaps to the right Y.
        if (BEKStudio.GameController.isGameStarted)
        {
            this.Delay(2, () =>
            {
                this.DelayUntil(
                    () => PlayerPuck.Instance != null
                          && BEKStudio.GameController.Instance != null,
                    () => CmdResyncTurnStateForReconnect());
            });
        }
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
                    Rpc_ShowPlayer();
                    var flowNgm = NetworkGameManager.Instance;
                    MatchFlow.Begin("Carrom", $"game started — {BEKStudio.GameController.FlowOwner("White")} (white) vs {BEKStudio.GameController.FlowOwner("Black")} (black), 10 min match"
                        + (string.IsNullOrEmpty(PlayercurrentTurnId) ? "" : $", {MatchFlow.Who(PlayercurrentTurnId)} starts")
                        + (flowNgm != null ? $", prize {flowNgm.Prize} {(flowNgm.IsGoldCoin ? "gold" : "silver")}" : ""));
                    if (countdownCoroutine != null)
                        StopCoroutine(countdownCoroutine);
                    countdownCoroutine = StartCoroutine(ServerCountdown());
                    ScheduleInitialTurnFromJoiner("players connected");
                });
            });
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdgameStarted()
    {
        ServerGameStarted();
    }

    [Server]
    public void ServerGameStarted()
    {
        currentNetworkTurn = "GameStarted";
        "GameStarted".Show();
        ScheduleInitialTurnFromJoiner("game started");
    }

    [Command(requiresAuthority = false)]
    public void CmdShowFoul()
    {

    }

    [ClientRpc]
    public void RpcShowFoul()
    {

    }







    #region PlayerPuck
    [Command(requiresAuthority = false)]
    public void CmdShoot(float forceMultiplier, Vector2 strikerLocalPosition, float strikerRotationZ, float shootAngle)
    {
        // Player has committed their shot — freeze the per-turn timer so it
        // stops draining while the pucks settle and we wait for the opponent.
        // It is re-armed by StartTurnTimer(...) when the next turn begins.
        StopTurnTimer();
        lastShooterId = PlayercurrentTurnId;
        MatchFlow.Log("Carrom", $"{MatchFlow.Who(PlayercurrentTurnId)} strikes (power {forceMultiplier:0.00})");

        if (playerPuck != null)
        {
            ApplyAuthoritativeStrikerShotPose(strikerLocalPosition, strikerRotationZ);
            playerPuck.GetComponent<NetworkIdentity>().RemoveClientAuthority();
            Debug.Log("CmdShoot called on server");
            playerPuck.GetComponent<PlayerPuck>().forceMultiplier = forceMultiplier;
            this.Delay(0.2f, () =>
            {
                // Pass the pre-calculated shoot angle so server doesn't recalculate direction
                playerPuck.GetComponent<PlayerPuck>().Shoot(shootAngle);
            });
        }
        // RpcShoot();
    }

    [Server]
    private float GetAuthoritativeStrikerBaselineY(PlayerPuck pp, string playerId, float fallbackY)
    {
        if (pp == null) return fallbackY;

        if (NetworkGameManager.Instance != null)
        {
            string creatorId = NetworkGameManager.Instance.creatorData != null
                ? NetworkGameManager.Instance.creatorData.playerId
                : string.Empty;
            string joinerId = NetworkGameManager.Instance.joinerData != null
                ? NetworkGameManager.Instance.joinerData.playerId
                : string.Empty;

            if (!string.IsNullOrEmpty(playerId))
            {
                if (playerId == creatorId) return pp.masterPosition.y;
                if (playerId == joinerId) return pp.clientPosition.y;
            }

            // This should never happen mid-game: the turn id no longer matches either
            // registered player, so the baseline below is only a guess and the striker
            // can land on the wrong side. Log loudly so it shows up in server logs.
            Debug.LogWarning($"[Carrom][Server] Striker baseline: turn id '{playerId}' matched neither creator '{creatorId}' nor joiner '{joinerId}' — guessing from current Y {fallbackY}.");
        }

        return Mathf.Abs(fallbackY - pp.masterPosition.y) <= Mathf.Abs(fallbackY - pp.clientPosition.y)
            ? pp.masterPosition.y
            : pp.clientPosition.y;
    }

    [Server]
    private float ClampStrikerX(float x)
    {
        if (BEKStudio.GameController.Instance == null) return x;

        return Mathf.Clamp(
            x,
            BEKStudio.GameController.Instance.playerPuckMinX,
            BEKStudio.GameController.Instance.playerPuckMaxX);
    }

    [Server]
    private void ClearStrikerNetworkTransformBuffers()
    {
        if (playerPuck == null) return;

        NetworkTransformUnreliable networkTransform = playerPuck.GetComponent<NetworkTransformUnreliable>();
        if (networkTransform == null) return;

        networkTransform.serverSnapshots.Clear();
        networkTransform.clientSnapshots.Clear();
    }

    [Server]
    private void ApplyRigidbodyToCurrentStrikerTransform(PlayerPuck pp)
    {
        if (pp == null || pp.rb == null || playerPuck == null) return;

        pp.rb.linearVelocity = Vector2.zero;
        pp.rb.angularVelocity = 0f;
        pp.rb.position = playerPuck.transform.position;
    }

    [Server]
    private void ApplyAuthoritativeStrikerReadyPose(string playerId)
    {
        if (playerPuck == null) return;

        PlayerPuck pp = playerPuck.GetComponent<PlayerPuck>();
        if (pp == null) return;

        float baselineY = GetAuthoritativeStrikerBaselineY(pp, playerId, playerPuck.transform.localPosition.y);

        playerPuck.transform.localPosition = new Vector3(0f, baselineY, 0f);
        playerPuck.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        ApplyRigidbodyToCurrentStrikerTransform(pp);
        ClearStrikerNetworkTransformBuffers();
    }

    [Server]
    public void ServerPrepareStrikerForTurn(string playerId)
    {
        ApplyAuthoritativeStrikerReadyPose(playerId);
    }

    // Reliable-channel copy of the server's ready pose, sent on every turn handoff.
    // The non-owner client has no local snap in ResetPosition (it bails on "not my
    // turn") and previously relied ONLY on the unreliable NetworkTransform relay —
    // a dropped snapshot left the striker on the OLD baseline on the timed-out
    // player's screen (timer-end Y desync: both screens showed different Y).
    [ClientRpc]
    private void RpcApplyStrikerReadyPose(Vector2 localPos, float rotZ)
    {
        if (gameEnded || ResultManager.isGameFinished || ResultManager.GameSpawnedFinished) return;
        if (NetworkServer.active) return; // server already posed it authoritatively

        if (playerPuck == null)
        {
            var puck = FindAnyObjectByType<PlayerPuck>();
            if (puck != null) playerPuck = puck.gameObject;
        }
        if (playerPuck == null) return;

        playerPuck.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        playerPuck.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

        var pp = playerPuck.GetComponent<PlayerPuck>();
        if (pp != null && pp.rb != null)
        {
            pp.rb.linearVelocity = Vector2.zero;
            pp.rb.angularVelocity = 0f;
            pp.rb.position = playerPuck.transform.position;
        }

        // Drop any buffered snapshots still carrying the old owner's pinned baseline,
        // so interpolation can't pull the striker back after this snap.
        var nt = playerPuck.GetComponent<NetworkTransformUnreliable>();
        if (nt != null)
        {
            nt.serverSnapshots.Clear();
            nt.clientSnapshots.Clear();
        }
    }

    [Server]
    private void ApplyAuthoritativeStrikerShotPose(Vector2 strikerLocalPosition, float strikerRotationZ)
    {
        if (playerPuck == null) return;

        PlayerPuck pp = playerPuck.GetComponent<PlayerPuck>();
        if (pp == null) return;

        float snappedY = GetAuthoritativeStrikerBaselineY(pp, PlayercurrentTurnId, strikerLocalPosition.y);
        float clampedX = ClampStrikerX(strikerLocalPosition.x);

        playerPuck.transform.localPosition = new Vector3(clampedX, snappedY, 0f);
        playerPuck.transform.rotation = Quaternion.Euler(0f, 0f, strikerRotationZ);

        ApplyRigidbodyToCurrentStrikerTransform(pp);
        ClearStrikerNetworkTransformBuffers();
    }

    [ClientRpc]
    void RpcShoot(float shootAngle)
    {
        if (playerPuck != null)
        {
            playerPuck.GetComponent<PlayerPuck>().Shoot(shootAngle);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPuckOnHolePlayerPuck(string puckTag, string puckName)
    {
        BEKStudio.GameController.Instance.PuckOnHole(puckTag, puckName);
        RpcPuckOnHolePlayerpuck(puckTag, puckName);
    }

    [ClientRpc]
    public void RpcPuckOnHolePlayerpuck(string puckTag, string puckName)
    {
        BEKStudio.GameController.Instance.PuckOnHole(puckTag, puckName);
    }

    [Command(requiresAuthority = false)]
    public void CmdSendData(bool[] dataArray, bool IsOrigMaster)
    {
        RpcSendData(dataArray, IsOrigMaster);
    }

    [ClientRpc]
    void RpcSendData(bool[] dataArray, bool IsOrigMaster)
    {
        BEKStudio.GameController.Instance.SyncData(dataArray, IsOrigMaster);
    }

    [Command(requiresAuthority = false)]
    public void CmdSliderUp()
    {
        BEKStudio.GameController.Instance.HandleOnSliderPointerUp();
        RpcSliderUp();
    }

    [ClientRpc]
    void RpcSliderUp()
    {
        BEKStudio.GameController.Instance.HandleOnSliderPointerUp();
    }

    [Command(requiresAuthority = false)]
    public void CmdSliderDown()
    {
        BEKStudio.GameController.Instance.HandleOnSliderPointerDown();
        RpcSliderDown();
    }

    [ClientRpc]
    void RpcSliderDown()
    {
        BEKStudio.GameController.Instance.HandleOnSliderPointerDown();
    }
    #endregion

    #region Puck commands And Rpcs
    [Command(requiresAuthority = false)]
    public void CmdResetRedPuck()
    {
        RpcResetRedPuck();
    }

    [ClientRpc]
    public void RpcResetRedPuck()
    {
        BEKStudio.GameController.Instance.redPuck.GetComponent<Puck>().ResetPosition();
        // GameObject Puck = FindAnyObjectByType<Puck>().gameObject;

        // if (Puck != null && Puck.GetComponent<Puck>() != null)
        // {
        //     var puckComponent = Puck.GetComponent<Puck>();

        //     if (puckComponent.practiceMode)
        //     {
        //         Puck.transform.localPosition = new Vector2(Random.Range(-1.89f, 1.89f), Random.Range(-1.16f, 1.16f));
        //     }
        //     else
        //     {
        //         Puck.transform.localPosition = puckComponent.puckStartPos;
        //     }

        //     BEKStudio.GameController.Instance.leftRedPuckIcon.SetActive(false);
        //     BEKStudio.GameController.Instance.rightRedPuckIcon.SetActive(false);

        //     if (puckComponent.networkTransform != null)
        //         puckComponent.networkTransform.enabled = true;

        //     puckComponent.puckInHole = false;
        //     puckComponent.rb.linearVelocity = Vector2.zero;
        //     puckComponent.rb.simulated = true;
        //     puckComponent.spriteRenderer.color = new Color32(255, 255, 255, 255);
        //     puckComponent.AnimRenderer.color = new Color32(255, 255, 255, 255);
        //     puckComponent.lastVel = Vector2.zero;
        //     puckComponent.isMoving = false;

    }



    [ClientRpc]
    public void RpcResetAndRemove(string objectName)
    {
        DoResetAndRemove(objectName);
    }

    public void ResetAndRemove(string objectName)
    {
        // Call this from server
        RpcResetAndRemove(objectName); // send to all clients
        if (isServer)                  // also run locally
            DoResetAndRemove(objectName);
    }

    public void DoResetAndRemove(string objectName)
    {
        Puck[] pucks = BEKStudio.GameController.Instance.allPucks;
        foreach (Puck puck in pucks)
        {
            if (puck.gameObject.name == objectName)
            {
                GameObject puckObj = puck.gameObject;
                var controller = BEKStudio.GameController.Instance;

                controller.homePucksCollected.Remove(puckObj);
                controller.awayPucksCollected.Remove(puckObj);

                puckObj.GetComponent<Puck>().ResetPosition();
                controller.UpdateScoreText();
                break;
            }
        }
    }

    // [ClientRpc]
    // public void RpcResetAndRemove(string objectName)
    // {
    //     Puck[] Pucks = FindObjectsByType<Puck>(FindObjectsSortMode.None);
    //     foreach (Puck puck in Pucks)
    //     {
    //         if (puck.gameObject.name == objectName)
    //         {
    //             GameObject Puck = puck.gameObject;
    //             BEKStudio.GameController Controller = BEKStudio.GameController.Instance;

    //             if (Controller.homePucksCollected.Contains(Puck))
    //             {
    //                 Controller.homePucksCollected.Remove(Puck);
    //             }
    //             else if (Controller.awayPucksCollected.Contains(Puck))
    //             {
    //                 Controller.awayPucksCollected.Remove(Puck);
    //             }

    //             Puck.GetComponent<Puck>().ResetPosition();
    //             Controller.UpdateScoreText();
    //             break;
    //         }
    //     }



    // }

    [Command(requiresAuthority = false)]
    public void CmdHandleCollision(int index, string puckName)
    {
        RpcHandleCollision(index, puckName);
    }

    [ClientRpc]
    public void RpcHandleCollision(int index, string puckName)
    {
        Puck[] Pucks = FindObjectsByType<Puck>(FindObjectsSortMode.None);
        foreach (Puck puck in Pucks)
        {
            if (puck.gameObject.name == puckName)
            {
                GameObject Puck = puck.gameObject;
                Puck.GetComponent<Puck>().ProcessCollisionWithRPC(index);
                break;

            }
        }


    }

    [Command(requiresAuthority = false)]
    public void CmdPuckState(GameObject puck, bool puckInHole, int puckHoleIndex, bool isOrignalMasterClient)
    {
        RpcPuckState(puck, puckInHole, puckHoleIndex, isOrignalMasterClient);
    }

    [ClientRpc]
    public void RpcPuckState(GameObject puck, bool puckInHole, int puckHoleIndex, bool isOrignalMasterClient)
    {
        GameObject Puck = puck;

        if (Puck != null)
        {
            BEKStudio.GameController Controller = BEKStudio.GameController.Instance;

            if (puckInHole)
            {
                if (Puck.tag == "Red") return;

                if (isOrignalMasterClient)
                {
                    if (Puck.tag.Equals("Black") && !Controller.awayPucksCollected.Contains(Puck))
                    {
                        Controller.awayPucksCollected.Add(Puck);
                        Puck.GetComponent<Puck>().OnDisableProperties(true);
                    }
                    else if (Puck.tag.Equals("White") && !Controller.homePucksCollected.Contains(Puck))
                    {
                        Controller.homePucksCollected.Add(Puck);
                        Puck.GetComponent<Puck>().OnDisableProperties(true);
                    }
                }
                else
                {
                    if (Puck.tag.Equals("Black") && !Controller.awayPucksCollected.Contains(Puck))
                    {
                        Controller.awayPucksCollected.Add(Puck);
                        Puck.GetComponent<Puck>().OnDisableProperties(true);
                    }
                    else if (Puck.tag.Equals("White") && !Controller.homePucksCollected.Contains(Puck))
                    {
                        Controller.homePucksCollected.Add(Puck);
                        Puck.GetComponent<Puck>().OnDisableProperties(true);
                    }
                }
                Controller.UpdateScoreText();
            }
            else
            {
                if (Controller.homePucksCollected.Contains(Puck))
                {
                    Puck.GetComponent<Puck>().ResetPosition();
                    Controller.homePucksCollected.Remove(Puck);
                }
                else if (Controller.awayPucksCollected.Contains(Puck))
                {
                    Puck.GetComponent<Puck>().ResetPosition();
                    Controller.awayPucksCollected.Remove(Puck);
                }
                Controller.UpdateScoreText();
            }
        }
    }
    #endregion

    #region Gamecontroller Commands And Rpcs
    //[Command(requiresAuthority = false)]
    //public void CmdResetOpponentTimer()
    //{
    //    RpcResetOpponentTimer();
    //}

    //[ClientRpc]
    //public void RpcResetOpponentTimer()
    //{
    //    BEKStudio.GameController.Instance.timer = Constants.PLAY_TIME_FOR_PLAYER;
    //}
    [SyncVar(hook = nameof(OnPlayerCurrentTurnIdChanged))]
    public string PlayercurrentTurnId;

    // Turn colour is a pure function of the turn holder: the CREATOR (master client)
    // always plays White, the joiner Black. Deriving masterClientTag from the turn id
    // (instead of toggling it on every Rpc) keeps it correct even when an Rpc is
    // missed, duplicated, or arrives out of order — the state always converges.
    public static string TurnColorFor(string turnPlayerId)
    {
        if (string.IsNullOrEmpty(turnPlayerId)) return null;
        if (NetworkGameManager.Instance == null || NetworkGameManager.Instance.creatorData == null)
            return null;
        return turnPlayerId == NetworkGameManager.Instance.creatorData.playerId ? "White" : "Black";
    }

    // PRIMARY turn sync. PlayercurrentTurnId is written ONLY on the server (inside
    // TryAssignStrikerAuthority), so every client — including one that reconnects or
    // missed RpcSwitchTurn — receives the authoritative turn via this SyncVar hook
    // and applies the full local turn state (CurrentTurn + masterClientTag + UI).
    // RpcSwitchTurn still runs for the immediate path; both are idempotent, so
    // whichever arrives (first or alone) the client ends in the same state.
    private void OnPlayerCurrentTurnIdChanged(string oldTurnId, string newTurnId)
    {
        if (oldTurnId == newTurnId || string.IsNullOrEmpty(newTurnId)) return;
        if (!NetworkClient.active) return;
        if (gameEnded || ResultManager.isGameFinished || ResultManager.GameSpawnedFinished) return;

        this.DelayUntil(
            () => BEKStudio.GameController.Instance != null
                  && PlayerPuck.Instance != null
                  && staticVariables.UserProfiledata != null
                  && staticVariables.UserProfiledata.user != null,
            () =>
            {
                var gc = BEKStudio.GameController.Instance;
                var beforeTurn = gc.CurrentTurn;
                var beforeTag = gc.masterClientTag;
                if (!ApplyLocalCurrentTurn(newTurnId)) return;

                string color = TurnColorFor(newTurnId);
                if (!string.IsNullOrEmpty(color))
                    gc.masterClientTag = color;

                // Restore, not shoot-again: this hook is the CATCH-UP path (missed
                // RpcSwitchTurn, or the first SyncVar landing on a scene rebuilt by a
                // reconnect). A shoot-again never reaches here because the turn id does
                // not change, so "Go On" was always the wrong banner.
                if (beforeTurn != gc.CurrentTurn || beforeTag != gc.masterClientTag)
                    gc.CheckTurn(BEKStudio.GameController.TurnBanner.Restore);
            });
    }

    private bool ApplyLocalCurrentTurn(string currentTurnId)
    {
        if (string.IsNullOrEmpty(currentTurnId)) return false;
        if (BEKStudio.GameController.Instance == null) return false;
        if (staticVariables.UserProfiledata == null || staticVariables.UserProfiledata.user == null) return false;

        BEKStudio.GameController.Instance.CurrentTurn =
            currentTurnId == staticVariables.UserProfiledata.user._id.ToString()
                ? BEKStudio.GameController.CurrentPlayer.ME
                : BEKStudio.GameController.CurrentPlayer.OTHER;

        Debug.Log("🔄 Current Turn ID: " + currentTurnId);
        Debug.Log("🔄 whichPlayer" + BEKStudio.GameController.Instance.CurrentTurn);
        return true;
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestInitialTurn(string currentPlayerIdForCreatorFirstTurn)
    {
        if (gameEnded) return;
        if (!string.IsNullOrEmpty(PlayercurrentTurnId))
        {
            Debug.Log($"[Carrom][Server] Ignored initial turn request from {currentPlayerIdForCreatorFirstTurn} (current={PlayercurrentTurnId})");
            return;
        }

        if (!ServerSwitchTurnFrom(currentPlayerIdForCreatorFirstTurn))
        {
            ScheduleInitialTurnFrom(currentPlayerIdForCreatorFirstTurn, "initial turn request");
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSwitchTurn(string punktag)
    {
        if (gameEnded) return;
        CurrentpuckTag = punktag;
        BEKStudio.GameController.Instance.masterClientTag = punktag;
    }
    [Command(requiresAuthority = false)]
    public void CmdSwitchTurn(int currentTurnId)
    {
        if (gameEnded) return;
        // AUTHORITATIVE debounce: only the player who currently HOLDS the turn may pass it.
        // Duplicate/stale switch requests (double events, race bursts) otherwise re-flip the
        // turn and desync the clients — same guard as 8-ball's CmdChangeTurn.
        if (!string.IsNullOrEmpty(PlayercurrentTurnId) && currentTurnId.ToString() != PlayercurrentTurnId)
        {
            Debug.Log($"[Carrom][Server] Ignored stale CmdSwitchTurn from {currentTurnId} (current={PlayercurrentTurnId})");
            return;
        }

        if (!ServerSwitchTurnFrom(currentTurnId.ToString(), allowOffline: true))
        {
            Debug.LogWarning($"[Carrom][Server] CmdSwitchTurn could not assign turn from {currentTurnId} yet.");
        }
    }

    /// <summary>
    /// Switches turn given the CURRENT player's ID string (no int.Parse required).
    /// Used by RpcOnTimerEnd where int.Parse would throw on non-numeric player IDs.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSwitchTurnByPlayerId(string currentPlayerId)
    {
        if (gameEnded) return;
        // Same authoritative debounce as CmdSwitchTurn(int): ignore a switch whose claimed
        // "current" player is not the actual turn holder (duplicate/stale requests).
        if (!string.IsNullOrEmpty(PlayercurrentTurnId) && currentPlayerId != PlayercurrentTurnId)
        {
            Debug.Log($"[Carrom][Server] Ignored stale CmdSwitchTurnByPlayerId from {currentPlayerId} (current={PlayercurrentTurnId})");
            return;
        }
        if (!ServerSwitchTurnFrom(currentPlayerId, allowOffline: true))
        {
            Debug.LogWarning($"[Carrom][Server] CmdSwitchTurnByPlayerId could not assign turn from {currentPlayerId} yet.");
        }
    }

    [Server]
    private void ScheduleInitialTurnFromJoiner(string reason)
    {
        string joinerId = NetworkGameManager.Instance != null && NetworkGameManager.Instance.joinerData != null
            ? NetworkGameManager.Instance.joinerData.playerId
            : string.Empty;

        ScheduleInitialTurnFrom(joinerId, reason);
    }

    [Server]
    private void ScheduleInitialTurnFrom(string currentPlayerId, string reason)
    {
        if (gameEnded || !string.IsNullOrEmpty(PlayercurrentTurnId)) return;
        if (initialTurnCoroutine != null) return;

        initialTurnCoroutine = StartCoroutine(CoEnsureInitialTurn(currentPlayerId, reason));
    }

    [Server]
    private IEnumerator CoEnsureInitialTurn(string currentPlayerId, string reason)
    {
        const int maxAttempts = 40;
        for (int attempt = 0; attempt < maxAttempts && !gameEnded && string.IsNullOrEmpty(PlayercurrentTurnId); attempt++)
        {
            string current = currentPlayerId;
            if (string.IsNullOrEmpty(current) && NetworkGameManager.Instance != null && NetworkGameManager.Instance.joinerData != null)
            {
                current = NetworkGameManager.Instance.joinerData.playerId;
            }

            if (!string.IsNullOrEmpty(current) && ServerSwitchTurnFrom(current))
            {
                initialTurnCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(0.25f);
        }

        if (string.IsNullOrEmpty(PlayercurrentTurnId))
        {
            Debug.LogWarning($"[Carrom][Server] Initial turn was not assigned after retry ({reason}).");
        }

        initialTurnCoroutine = null;
    }

    // allowOffline: mid-game turn changes must succeed even when the incoming turn
    // holder is DISCONNECTED — the turn id (SyncVar) still moves, the striker is posed
    // server-side, and the authority grant is deferred to CmdResyncTurnStateForReconnect
    // when the player returns. Initial-turn assignment stays strict (allowOffline=false)
    // because a fresh join has no resync pass to grant the deferred authority.
    [Server]
    private bool TryAssignStrikerAuthority(string playerId, bool allowOffline = false)
    {
        if (string.IsNullOrEmpty(playerId)) return false;

        if (playerPuck == null)
        {
            var puck = FindAnyObjectByType<PlayerPuck>();
            if (puck != null) playerPuck = puck.gameObject;
        }

        if (playerPuck == null)
        {
            Debug.LogWarning($"[Carrom][Server] Cannot assign striker authority to {playerId}: playerPuck missing.");
            return false;
        }

        var stickIdentity = playerPuck.GetComponent<NetworkIdentity>();
        if (stickIdentity == null)
        {
            Debug.LogWarning("[Carrom][Server] Cannot assign striker authority: NetworkIdentity missing on playerPuck.");
            return false;
        }

        MirrorPlayerPrefab[] playerlist = FindObjectsOfType<MirrorPlayerPrefab>();
        foreach (var item in playerlist)
        {
            string itemPlayerId = item.playerData != null && !string.IsNullOrEmpty(item.playerData.playerId)
                ? item.playerData.playerId
                : item.playerId;

            if (itemPlayerId != playerId || item.connectionToClient == null) continue;

            // SERVER-FIRST HANDOFF: on every turn change the server takes the striker
            // back, poses it on the correct baseline for the new turn holder while it
            // is still server-authoritative, and only then hands authority over. The
            // old order (assign authority first, pose after) let the new owner's
            // ClientToServer stream and stale snapshots overwrite the server's pose,
            // so the striker could sit on the wrong side on the server/opponent view.
            NetworkConnectionToClient targetConn = item.connectionToClient;

            if (stickIdentity.connectionToClient != null)
            {
                stickIdentity.RemoveClientAuthority();
            }

            PlayercurrentTurnId = playerId;
            ApplyAuthoritativeStrikerReadyPose(playerId);

            // Push the exact pose to every client on the reliable channel too —
            // the unreliable NetworkTransform relay alone can drop it (timer-end Y desync).
            RpcApplyStrikerReadyPose(playerPuck.transform.localPosition, 90f);

            // Hand the striker over a moment later so the authoritative pose reaches
            // every client before the new owner starts streaming its own position.
            this.Delay(0.15f, () =>
            {
                if (gameEnded || playerPuck == null) return;
                if (PlayercurrentTurnId != playerId) return; // turn changed meanwhile
                if (targetConn == null || !targetConn.isReady) return;

                var identity = playerPuck.GetComponent<NetworkIdentity>();
                if (identity != null && identity.connectionToClient == null)
                {
                    identity.AssignClientAuthority(targetConn);
                    Debug.Log($"[Carrom][Server] Striker authority assigned to {playerId}");
                }
            });
            return true;
        }

        // OFFLINE TURN ASSIGNMENT: the incoming turn holder has no live connection
        // (disconnected mid-flow). The turn itself must STILL move — PlayercurrentTurnId
        // is the source of truth; the reconnect resync grants striker authority when the
        // player returns. Without this the switch failed outright: the shooter kept the
        // turn id, both clients wedged in SWITCH_MASTER, and on reconnect the WRONG
        // player (the old shooter) was handed the turn again.
        if (allowOffline)
        {
            if (stickIdentity.connectionToClient != null)
            {
                stickIdentity.RemoveClientAuthority();
            }

            PlayercurrentTurnId = playerId;
            ApplyAuthoritativeStrikerReadyPose(playerId);
            RpcApplyStrikerReadyPose(playerPuck.transform.localPosition, 90f);

            Debug.Log($"[Carrom][Server] Turn assigned to OFFLINE player {playerId}; striker authority deferred until reconnect.");
            MatchFlow.Log("Carrom", $"{MatchFlow.Who(playerId)} is offline — turn waits for reconnect");
            return true;
        }

        Debug.LogWarning($"[Carrom][Server] No ready MirrorPlayerPrefab found for turn player {playerId}.");
        return false;
    }

    // Shot-path entry for the DEDICATED SERVER's own GameController flow (SwitchMasterDelay).
    // The server runs the full settle/CheckPucks logic itself, so it must switch through the
    // SAME authoritative path the client Cmds use (server-first handoff + ready-pose Rpc +
    // server-side masterClientTag) instead of the old inline block in GameController.
    // Guarded with lastShooterId: if the shooter-client's racing CmdSwitchTurn already moved
    // the turn, this is a no-op — the two paths can never flip the turn twice.
    [Server]
    public void ServerSwitchTurnAfterShot()
    {
        if (gameEnded) return;
        string fromId = string.IsNullOrEmpty(lastShooterId) ? PlayercurrentTurnId : lastShooterId;
        if (string.IsNullOrEmpty(fromId)) return;
        if (!string.IsNullOrEmpty(PlayercurrentTurnId) && fromId != PlayercurrentTurnId)
        {
            Debug.Log($"[Carrom][Server] Shot-path switch skipped — turn already moved off {fromId} (current={PlayercurrentTurnId}).");
            return;
        }
        if (!ServerSwitchTurnFrom(fromId, allowOffline: true))
        {
            Debug.LogWarning($"[Carrom][Server] Shot-path turn switch from {fromId} failed.");
        }
    }

    // The actual server-side turn switch, kept OUT of the [Command] wrapper so server-side code
    // can invoke it directly. It only starts the turn once striker authority is really assigned
    // (or, with allowOffline, once the turn is assigned to a disconnected player with the
    // authority grant deferred to the reconnect resync).
    [Server]
    private bool ServerSwitchTurnFrom(string currentPlayerId, bool allowOffline = false)
    {
        if (gameEnded) return false;
        if (string.IsNullOrEmpty(currentPlayerId)) return false;
        if (NetworkGameManager.Instance == null || NetworkGameManager.Instance.creatorData == null || NetworkGameManager.Instance.joinerData == null)
            return false;

        string creatorId = NetworkGameManager.Instance.creatorData.playerId;
        string joinerId = NetworkGameManager.Instance.joinerData.playerId;
        if (string.IsNullOrEmpty(creatorId) || string.IsNullOrEmpty(joinerId)) return false;

        string nextTurn = currentPlayerId == creatorId ? joinerId : creatorId;

        bool flowFirstTurn = string.IsNullOrEmpty(PlayercurrentTurnId);
        // Ready pose is applied inside TryAssignStrikerAuthority, while the server
        // still holds the striker (server-first handoff).
        if (!TryAssignStrikerAuthority(nextTurn, allowOffline))
        {
            return false;
        }

        currentPlayerIndex = currentPlayerId == creatorId ? 1 : 0;

        // Server derives its own turn colour from the same mapping the clients use —
        // it no longer depends on the turn-holder client echoing CmdSwitchTurn(myPuck).
        string nextColor = TurnColorFor(nextTurn);
        if (!string.IsNullOrEmpty(nextColor))
        {
            CurrentpuckTag = nextColor;
            if (BEKStudio.GameController.Instance != null)
                BEKStudio.GameController.Instance.masterClientTag = nextColor;
        }

        MatchFlow.Log("Carrom", flowFirstTurn ? $"{MatchFlow.Who(nextTurn)} starts (plays {nextColor})" : $"turn → {MatchFlow.Who(nextTurn)}");
        RpcSwitchTurn(nextTurn);
        StartTurnTimer(nextTurn);
        ResetPlayerPuckPos();
        return true;
    }

    // ---- RECONNECT RESYNC ----
    // Called by a reconnecting client from CarromNetworkManager.Start() when it
    // detects that GameController.isGameStarted is already true (i.e. this scene
    // load is the result of a Mirror reconnect, not a fresh game start).
    //
    // Pushes the FULL current game state from the server so the reconnecting
    // client doesn't end up showing stale data (zero scores, empty pot
    // counters, missing queen icon, wrong turn). Previously this method only
    // synced turn state and relied on the onTurnChanged SyncVar hook to
    // separately push lists/scores — but that hook doesn't always fire on
    // the initial post-reconnect spawn, leaving the reconnector out of sync.
    [Command(requiresAuthority = false)]
    public void CmdResyncTurnStateForReconnect(NetworkConnectionToClient sender = null)
    {
        if (gameEnded)
        {
            Debug.Log("[Carrom Reconnect] Game already ended — skipping turn resync.");
            return;
        }
        if (string.IsNullOrEmpty(PlayercurrentTurnId))
        {
            Debug.LogWarning("[Carrom Reconnect] No current turn id to resync.");
            ScheduleInitialTurnFromJoiner("reconnect missing current turn");
            return;
        }

        // Re-assign striker authority to whoever currently owns the turn.
        // The reconnected conn's MirrorPlayerPrefab is in the scene by now.
        if (!TryAssignStrikerAuthority(PlayercurrentTurnId))
        {
            Debug.LogWarning($"[Carrom Reconnect] Could not reassign striker authority to {PlayercurrentTurnId}.");
            return;
        }

        MatchFlow.Log("Carrom", $"player reconnected — board resynced, turn stays with {MatchFlow.Who(PlayercurrentTurnId)}");
        // ── Push full server state to the reconnecting client ──────────
        // Player avatars/names — idempotent, also reaches the opponent
        // but they already have the right UI.
        Rpc_ShowPlayer();

        // Collected lists + scores. Without this the reconnecting client
        // sees zero scores / empty pot counters even though the opponent
        // has been potting pucks during the absence.
        string[] homeNames = BEKStudio.GameController.Instance.homePucksCollected
            .Select(go => go.name).ToArray();
        string[] awayNames = BEKStudio.GameController.Instance.awayPucksCollected
            .Select(go => go.name).ToArray();
        RpcApplyDataFromServer(
            homeNames,
            awayNames,
            BEKStudio.GameController.currentHomeScore,
            BEKStudio.GameController.currentAwayScore);

        // Turn state + queen icon restoration — TargetRpc only to the
        // reconnecting conn so the opponent isn't disturbed.
        TargetResyncTurnStateForReconnect(
            sender,
            PlayercurrentTurnId,
            BEKStudio.GameController.Instance.redPuckWaiting,
            BEKStudio.GameController.Instance.QueenPendingTag);
    }

    [TargetRpc]
    private void TargetResyncTurnStateForReconnect(
        NetworkConnection target,
        string currentTurnId,
        bool queenPendingCover,
        string queenPendingTag)
    {
        if (gameEnded || ResultManager.isGameFinished || ResultManager.GameSpawnedFinished)
        {
            Debug.Log("🛑 TargetResyncTurnStateForReconnect skipped — game already ended");
            return;
        }

        var gc = BEKStudio.GameController.Instance;
        gc.ApplyAuthoritativeQueenPendingState(queenPendingCover, queenPendingTag);

        // Restore queen icon state. Icons aren't SyncVars — they're flipped
        // by PuckHole.OnComplete when the queen is potted. On a fresh scene
        // load the icons start hidden, so re-derive their state from which
        // collected list now holds the queen (lists were just rebuilt by
        // RpcApplyDataFromServer running before us in the same batch).
        if (queenPendingCover)
        {
            if (queenPendingTag == "White")
                gc.leftRedPuckIcon.SetActive(true);
            else if (queenPendingTag == "Black")
                gc.rightRedPuckIcon.SetActive(true);
        }
        else if (gc.redPuckCollected)
        {
            if (gc.homePucksCollected.Contains(gc.redPuck))
                gc.leftRedPuckIcon.SetActive(true);
            else if (gc.awayPucksCollected.Contains(gc.redPuck))
                gc.rightRedPuckIcon.SetActive(true);
        }

        if (!ApplyLocalCurrentTurn(currentTurnId)) return;

        // CheckTurn() fires Event_MasterClientSwithced which invokes
        // PlayerPuck.OnMasterClientSwitched -> ResetPosition(), and when
        // CurrentTurn == ME it also directly calls PlayerPuck.Instance.ResetPosition().
        // Also runs HandleWorstCase which hides potted pucks visually using
        // the freshly-rebuilt collected lists.
        // Restore: the turn is being RE-APPLIED after the reconnect, not earned by
        // potting a bead. Passing `false` here made the banner announce "Go On" to a
        // player who had just walked back into the match.
        gc.CheckTurn(BEKStudio.GameController.TurnBanner.Restore);
    }

    [ClientRpc]
    public void RpcCheckGameStatusAfterStop()
    {
        BEKStudio.GameController.Instance.CheckGameStatusAfterStop();
    }
    [ClientRpc]
    public void RpcSwitchTurn(string currentTurnId)
    {
        if (gameEnded || ResultManager.isGameFinished || ResultManager.GameSpawnedFinished)
        {
            Debug.Log("🛑 RpcSwitchTurn skipped — game already ended");
            return;
        }
        Debug.Log("🔄 RpcSwitchTurn called");

        if (!ApplyLocalCurrentTurn(currentTurnId)) return;

        // Derive the colour from the turn holder instead of toggling it — a toggle
        // desyncs permanently after one missed/duplicate Rpc; the derivation is
        // idempotent and matches what the SyncVar hook applies. The old
        // CmdSwitchTurn(myPuck) echo is gone too: the server now sets its own
        // tag inside ServerSwitchTurnFrom, so a laggy client echo can no longer
        // arrive late and overwrite the server's tag for the NEXT turn.
        string color = TurnColorFor(currentTurnId);
        if (!string.IsNullOrEmpty(color))
            BEKStudio.GameController.Instance.masterClientTag = color;

        BEKStudio.GameController.Instance.CheckTurn();
    }
    #region Shoot Again Logic
    [Command(requiresAuthority = false)]
    public void CmdGiveTurnToSamePlayer(string currentPlayerId)
    {
        ServerGiveTurnToSamePlayer(currentPlayerId);
    }

    // Shared by the client Cmd above AND the dedicated server's own GameController
    // shoot-again flow, so both use TryAssignStrikerAuthority's server-first handoff
    // + ready-pose Rpc (the old inline block in GameController skipped both).
    [Server]
    public void ServerGiveTurnToSamePlayer(string currentPlayerId)
    {
        if (gameEnded) return;
        Debug.Log($"🔄 Giving turn again to same player: {currentPlayerId}");

        // Same player ko hi authority do (ready pose TryAssignStrikerAuthority ke
        // andar lagti hai, jab striker server ke paas hota hai). allowOffline: agar
        // shooter pot kar ke disconnect ho gaya, turn phir bhi uski hi rehti hai aur
        // authority reconnect resync pe milti hai.
        if (!TryAssignStrikerAuthority(currentPlayerId, allowOffline: true))
        {
            Debug.LogWarning($"[Carrom][Server] Could not give same-player turn to {currentPlayerId}.");
            return;
        }

        var gc = BEKStudio.GameController.Instance;
        bool queenPendingCover = gc != null && gc.redPuckWaiting;
        string pendingQueenTag = queenPendingCover ? gc.QueenPendingTag : null;

        MatchFlow.Log("Carrom", $"turn continues for {MatchFlow.Who(currentPlayerId)}" + (queenPendingCover ? " — must cover the queen" : ""));
        RpcGiveTurnToSamePlayer(currentPlayerId, queenPendingCover, pendingQueenTag);
        StartTurnTimer(currentPlayerId);
        ResetPlayerPuckPos();
    }

    [ClientRpc]
    public void RpcGiveTurnToSamePlayer(
        string currentPlayerId,
        bool queenPendingCover,
        string queenPendingTag)
    {
        if (gameEnded || ResultManager.isGameFinished || ResultManager.GameSpawnedFinished)
        {
            Debug.Log("🛑 RpcGiveTurnToSamePlayer skipped — game already ended");
            return;
        }
        Debug.Log("🔄 RpcGiveTurnToSamePlayer called for: " + currentPlayerId);

        if (!ApplyLocalCurrentTurn(currentPlayerId)) return;

        var gc = BEKStudio.GameController.Instance;
        gc.ApplyAuthoritativeQueenPendingState(queenPendingCover, queenPendingTag);

        Debug.Log("🔄 Same player continues turn: " + currentPlayerId);

        // Master client tag change nahi karna
        // BEKStudio.GameController.Instance.masterClientTag same rahega
        //   playerPuck.GetComponent<PlayerPuck>().ResetPosition();
        gc.CheckTurn(BEKStudio.GameController.TurnBanner.ShootAgain);

    }
    [ClientRpc]
    public void ResetPlayerPuckPos()
    {
        StartCoroutine(BEKStudio.GameController.Instance.ResetPuck());
    }
    [ClientRpc]
    public void RpcResetRedPunk()
    {
        BEKStudio.GameController.Instance.allPucks[^1].GetComponent<Puck>().ResetPosition();

    }
    #endregion
    [Command(requiresAuthority = false)]
    public void CmdGameOver()
    {
        MatchFlow.SendResult(PlayercurrentTurnId, "game over", BEKStudio.GameController.FlowScores());
        StopAllTurnLogic();
        RpcGameOver(PlayercurrentTurnId);
        this.Delay(5, () =>
       {
           "Destroy After Delay".Show();
           NetworkGameManager.Instance.currentServerRequestId.Show("Current Server Id");
           MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
       });
    }

    [ClientRpc]
    public void RpcGameOver(string winnerId)
    {
        if (ResultManager.GameSpawnedFinished == false)
        {
            ResultManager.GameSpawnedFinished = true;
            GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

            if (enemyPrefab != null)
            {
                "1".Show();
                var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, winnerId);
            }
            else
            {
                Debug.LogError("WinLose GameManager prefab not found!");
            }
        }
    }

    [Command(requiresAuthority = false)]
    void CmdLeftRoom()
    {
        MatchFlow.Log("Carrom", "a player left the room");
        RpcLeftRoom();
    }

    [ClientRpc]
    void RpcLeftRoom()
    {
        NetworkManager.singleton.StopClient();
    }
    [ClientRpc]
    public void Rpc_ShowPlayer()
    {
        Debug.Log("Rpc_ShowPlayer called");
        if (MirrorNetwork.Instance.isMasterClient)
        {
            Debug.Log("MyName;" + staticVariables.UserProfiledata.user.first_name + staticVariables.UserProfiledata.user.last_name);
            Debug.Log("MyName;" + staticVariables.ProfilePicture);
            BEKStudio.GameController.Instance.topHomeNameText.text = staticVariables.UserProfiledata.user.first_name + staticVariables.UserProfiledata.user.last_name;
            BEKStudio.GameController.Instance.topHomeAvatar.texture = staticVariables.ProfilePicture;
            BEKStudio.GameController.Instance.topAwayNameText.text = staticVariables.OpponetProfile.userName;
            BEKStudio.GameController.Instance.topAwayAvatar.texture = staticVariables.opponentImage;
        }
        else
        {
            Debug.Log("OpponetName;" + staticVariables.OpponetProfile.userName);
            Debug.Log("OpponetImage:" + staticVariables.opponentImage);
            BEKStudio.GameController.Instance.topAwayNameText.text = staticVariables.UserProfiledata.user.first_name + staticVariables.UserProfiledata.user.last_name;
            BEKStudio.GameController.Instance.topAwayAvatar.texture = staticVariables.ProfilePicture;
            BEKStudio.GameController.Instance.topHomeNameText.text = staticVariables.OpponetProfile.userName;
            BEKStudio.GameController.Instance.topHomeAvatar.texture = staticVariables.opponentImage;

        }
    }
    #endregion
    #region Timer
    [Server]
    public void StartTurnTimer(string playerId)
    {
        if (gameEnded) return;
        Debug.Log("Timer" + playerId);
        // Stop any old timer
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        // Reset timer and start new one
        timerCoroutine = StartCoroutine(ServerTimerRoutine(playerId));
    }

    // Pauses the per-turn countdown WITHOUT ending the match (unlike
    // StopAllTurnLogic which sets gameEnded). Called the instant a player
    // commits their shot so the avatar timer doesn't keep draining while the
    // pucks settle and we wait for the next turn. The next StartTurnTimer(...)
    // re-arms it for whoever plays next (same player on pot-again, or opponent).
    [Server]
    public void StopTurnTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        isTimerRunning = false;
    }

    [Server]
    private IEnumerator ServerTimerRoutine(string playerId)
    {
        isTimerRunning = true;
        timer = totalTime;

        RpcOnTimerReset(playerId, totalTime); // UI reset on all clients

        while (timer > 0)
        {
            if (gameEnded)
            {
                isTimerRunning = false;
                yield break;
            }

            // Only drain the per-turn timer while BOTH players are present and the
            // game isn't paused. On a disconnect the remaining player gets a 30s
            // waiting/reconnect panel (NetworkGameManager sets IsPaused=true and
            // currentPlayerCount<2) — without this gate the turn timer kept draining
            // and could end/switch the turn during that wait. IsPaused also covers a
            // player merely backgrounding the app (count may still be 2); count covers
            // reconnect/setup timing. Holding here freezes the bar at its last fill
            // (no RpcOnTimerUpdate, so no bandwidth/visual jump) and resumes on return.
            NetworkGameManager ngm = NetworkGameManager.Instance;
            bool canRunTurnTimer = ngm != null && !ngm.IsPaused && ngm.currentPlayerCount >= 2;
            if (!canRunTurnTimer)
            {
                yield return null;
                continue;
            }

            timer -= Time.deltaTime;
            RpcOnTimerUpdate(playerId, timer);
            yield return null;
        }

        timer = 0;
        isTimerRunning = false;

        if (gameEnded) yield break;
        MatchFlow.Log("Carrom", $"turn timer ran out for {MatchFlow.Who(playerId)}");

        // The SERVER switches the turn itself, immediately — the old flow sent
        // RpcOnTimerEnd and waited for a client to answer with CmdSwitchTurnByPlayerId.
        // That full round-trip left the timed-out client OWNING the striker (its
        // FixedUpdate Y-pin + ClientToServer stream still active) for RTT longer than
        // needed, widening the race that desynced the striker Y between the screens.
        if (!ServerSwitchTurnFrom(playerId, allowOffline: true))
        {
            Debug.LogWarning($"[Carrom][Server] Timer-end turn switch from {playerId} failed — retrying once.");
            this.Delay(0.5f, () =>
            {
                if (gameEnded || PlayercurrentTurnId != playerId) return;
                ServerSwitchTurnFrom(playerId, allowOffline: true);
            });
        }

        RpcOnTimerEnd(playerId);
    }

    // Client-side cleanup only (aim UI off, touch released). The turn switch itself
    // now happens server-side in ServerTimerRoutine — no client command round-trip.
    [ClientRpc]
    void RpcOnTimerEnd(string playerId)
    {
        if (gameEnded || ResultManager.isGameFinished || ResultManager.GameSpawnedFinished) return;
        Debug.Log("⏰ Timer Ended for " + playerId);
        if (PlayerPuck.Instance == null) return;
        PlayerPuck.Instance.isTouch = false;
        PlayerPuck.Instance.arrow.SetActive(false);
        PlayerPuck.Instance.tutorial.SetActive(false);
        PlayerPuck.Instance.tutorialShowDelay = 2f;

        //   if (staticVariables.UserProfiledata.user._id.ToString() == playerId)
        //      StartCoroutine(BEKStudio.GameController.Instance.WaitForPucks());
    }

    // Called on all clients when server reports timer ended

    #endregion

    [ClientRpc]
    void RpcOnTimerReset(string playerId, float total)
    {
        Debug.Log($"⏰ Timer Reset - Player: {playerId}, My ID: {staticVariables.UserProfiledata.user._id}");

        // ✅ SABHI CLIENTS KE LIYE SAME LOGIC
        bool isMyTurn = playerId == staticVariables.UserProfiledata.user._id.ToString();

        if (MirrorNetwork.Instance.isMasterClient)
        {
            // Master client ke screen par:
            // Home = Master, Away = Opponent
            BEKStudio.GameController.Instance.topHomeAvatarTimer.fillAmount = isMyTurn ? 1 : 0;
            BEKStudio.GameController.Instance.topAwayAvatarTimer.fillAmount = isMyTurn ? 0 : 1;
        }
        else
        {
            // Non-master client ke screen par:
            // Home = Opponent, Away = Self
            BEKStudio.GameController.Instance.topHomeAvatarTimer.fillAmount = isMyTurn ? 0 : 1;
            BEKStudio.GameController.Instance.topAwayAvatarTimer.fillAmount = isMyTurn ? 1 : 0;
        }
    }

    [ClientRpc]
    void RpcOnTimerUpdate(string playerId, float current)
    {
        float fill = Mathf.InverseLerp(0, totalTime, current);
        //Debug.Log($"⏰ Timer Update - Player: {playerId}, Fill: {fill}, My ID: {staticVariables.UserProfiledata.user._id}");

        // ✅ SABHI CLIENTS KE LIYE SAME LOGIC
        bool isMyTurn = playerId == staticVariables.UserProfiledata.user._id.ToString();

        if (MirrorNetwork.Instance.isMasterClient)
        {
            // Master client
            BEKStudio.GameController.Instance.topHomeAvatarTimer.fillAmount = isMyTurn ? fill : 0;
            BEKStudio.GameController.Instance.topAwayAvatarTimer.fillAmount = isMyTurn ? 0 : fill;
        }
        else
        {
            // Non-master client  
            BEKStudio.GameController.Instance.topHomeAvatarTimer.fillAmount = isMyTurn ? 0 : fill;
            BEKStudio.GameController.Instance.topAwayAvatarTimer.fillAmount = isMyTurn ? fill : 0;
        }
    }
    [Command(requiresAuthority = false)]
    public void CmdGameStateWin(int PlayerId)
    {
        MatchFlow.SendResult(BEKStudio.GameController.currentHomeScore == BEKStudio.GameController.currentAwayScore ? "draw"
            : BEKStudio.GameController.currentHomeScore > BEKStudio.GameController.currentAwayScore ? NetworkGameManager.Instance?.creatorData?.playerId
            : NetworkGameManager.Instance?.joinerData?.playerId,
            $"game over — {MatchFlow.Who(PlayerId.ToString())} reported a win, higher board score wins", BEKStudio.GameController.FlowScores());
        StopAllTurnLogic();
        RpcGameStateWin(PlayerId);
        this.Delay(5, () =>
       {
           "Destroy After Delay".Show();
           NetworkGameManager.Instance.currentServerRequestId.Show("Current Server Id");
           MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
           //RpcCleanUp(NetworkGameManager.Instance.requestId);
       });
    }
     [ClientRpc]
    public void RpcGameStateWin(int PlayerId)
    {
        // ResultManagerForCarrom.instance.WinPlayer(true, PlayerId);
        if (ResultManager.GameSpawnedFinished == false)
        {
            ResultManager.GameSpawnedFinished = true;
            GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

            if (enemyPrefab != null)
            {
                "1".Show();
                // Spawn at position (0,0,0)
                var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                var resultManager = gameObject.GetComponent<ResultManager>();
                Debug.Log("currentHomeScore"+BEKStudio.GameController.currentHomeScore);
                Debug.Log("currentAwayScore"+BEKStudio.GameController.currentAwayScore);
                if (BEKStudio.GameController.currentHomeScore == BEKStudio.GameController.currentAwayScore)
                {
                    resultManager.Draw();
                }
                else
                {
                    if (BEKStudio.GameController.currentHomeScore > BEKStudio.GameController.currentAwayScore)
                    {
                       BEKStudio.GameController.Instance.gameWinner = NetworkGameManager.Instance.creatorData.playerId;
                    }
                    else if (BEKStudio.GameController.currentAwayScore > BEKStudio.GameController.currentHomeScore)
                   {
                      BEKStudio.GameController.Instance.gameWinner = NetworkGameManager.Instance.joinerData.playerId;
                   }
                    resultManager.HandleGameResultAltMultiplayer(true, BEKStudio.GameController.Instance.gameWinner);
                }
                
            }
            else
            {
                Debug.LogError("WinLose GameManager prefab not found!");
            }
        }
    }
    [Command(requiresAuthority = false)]
    public void CmdGameStatelose(int PlayerId)
    {
        MatchFlow.SendResult(BEKStudio.GameController.currentHomeScore == BEKStudio.GameController.currentAwayScore ? "draw"
            : BEKStudio.GameController.currentHomeScore > BEKStudio.GameController.currentAwayScore ? NetworkGameManager.Instance?.creatorData?.playerId
            : NetworkGameManager.Instance?.joinerData?.playerId,
            $"game over — {MatchFlow.Who(PlayerId.ToString())} reported a loss, higher board score wins", BEKStudio.GameController.FlowScores());
        StopAllTurnLogic();
        RpcGameStatelose(PlayerId);
        this.Delay(5, () =>
       {
           "Destroy After Delay".Show();
           NetworkGameManager.Instance.currentServerRequestId.Show("Current Server Id");
           MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
           //RpcCleanUp(NetworkGameManager.Instance.requestId);
       });
    }

    [Command(requiresAuthority = false)]
    public void CmdSetPosition(float x, float y)
    {
        var pp = playerPuck.GetComponent<PlayerPuck>();
        pp.rb.linearVelocity = Vector2.zero;

        // The server owns the actual board position. The client may view the board from its
        // own side, so never trust an incoming Y blindly; derive the legal baseline from the
        // current turn holder and use the command only for lane X.
        float snappedY = GetAuthoritativeStrikerBaselineY(pp, PlayercurrentTurnId, y);
        float clampedX = ClampStrikerX(x);

        playerPuck.transform.localPosition = new Vector2(clampedX, snappedY);
        ApplyRigidbodyToCurrentStrikerTransform(pp);
        ClearStrikerNetworkTransformBuffers();
    }
    [ClientRpc]
    public void RpcGameStatelose(int PlayerId)
    {
        // ResultManagerForCarrom.instance.WinPlayer(false, PlayerId);
        if (ResultManager.GameSpawnedFinished == false)
        {
            ResultManager.GameSpawnedFinished = true;
            GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

            if (enemyPrefab != null)
            {
                "1".Show();
                // Spawn at position (0,0,0)
                var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                var resultManager = gameObject.GetComponent<ResultManager>();
                Debug.Log("currentHomeScore"+BEKStudio.GameController.currentHomeScore);
                Debug.Log("currentAwayScore"+BEKStudio.GameController.currentAwayScore);
                if (BEKStudio.GameController.currentHomeScore == BEKStudio.GameController.currentAwayScore)
                {
                    resultManager.Draw();
                }
                else
                {
                    if (BEKStudio.GameController.currentHomeScore > BEKStudio.GameController.currentAwayScore)
                    {
                       BEKStudio.GameController.Instance.gameWinner = NetworkGameManager.Instance.creatorData.playerId;
                    }
                    else if (BEKStudio.GameController.currentAwayScore > BEKStudio.GameController.currentHomeScore)
                   {
                      BEKStudio.GameController.Instance.gameWinner = NetworkGameManager.Instance.joinerData.playerId;
                   }
                    resultManager.HandleGameResultAltMultiplayer(true, BEKStudio.GameController.Instance.gameWinner);
                }
                    
            }
            else
            {
                Debug.LogError("WinLose GameManager prefab not found!");
            }
        }
    }
    [ClientRpc]
    public void RpcResetStrikerPosition()
    {
        playerPuck.GetComponent<PlayerPuck>().ResetPosition();
    }
}
