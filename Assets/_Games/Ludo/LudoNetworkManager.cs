
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;

public class LudoNetworkManager : NetworkBehaviour
{



    public static LudoNetworkManager instance;

    public List<NetworkConnectionToClient> players = new List<NetworkConnectionToClient>();


    #region Game Timer Syncing
    [Header("UI Timer")]
    // public TextMeshProUGUI countDownGameTimer;

    [Header("Countdown Settings")]
    [SyncVar(hook = nameof(OnTimeChanged))]
    public int remainingTime = 900;

    public int remainingminutes;
    public int remainingseconds;

    private Coroutine countdownCoroutine;

    [Server]
    private IEnumerator ServerCountdown()
    {
        remainingTime = 900;
        Debug.Log("Server Countdown Started." + remainingTime);
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
        //while (remainingTime > 0)
        //{
        //   // if (!NetworkGameManager.Instance.IsPaused)
        //        remainingTime--;

        //    yield return new WaitForSeconds(1f);
        //}
        MatchFlow.Log("Ludo", "match timer ran out (15:00) — winner by tokens home");
        if (serverAuthority && board != null)
        {
            ServerTimeOver();
            yield break;
        }
        // Who is ahead comes from the last board the players reported (SaveBoardState). The dedicated server is not
        // a player: its own playerObjects[].finishedPawns never count up, so they always said "tie" → creator won.
        int creatorFinished, joinerFinished, creatorProgress, joinerProgress;
        bool fromBoard = GameGUIController.TryReportedBoardScore(out creatorFinished, out joinerFinished,
                                                                 out creatorProgress, out joinerProgress);
        if (!fromBoard)
        {
            var players = LudoGame.GameManager.Instance != null ? LudoGame.GameManager.Instance.playerObjects : null;
            creatorFinished = players != null && players.Count > 0 ? players[0].finishedPawns : 0;
            joinerFinished = players != null && players.Count > 1 ? players[1].finishedPawns : 0;
            creatorProgress = joinerProgress = 0;
        }

        string winnerID;

        // More tokens home wins; level → further along the track wins; still level → the creator (as before).
        if (creatorFinished != joinerFinished)
            winnerID = creatorFinished > joinerFinished ? NetworkGameManager.Instance.creatorData.playerId : NetworkGameManager.Instance.joinerData.playerId;
        else if (creatorProgress != joinerProgress)
            winnerID = creatorProgress > joinerProgress ? NetworkGameManager.Instance.creatorData.playerId : NetworkGameManager.Instance.joinerData.playerId;
        else
            winnerID = NetworkGameManager.Instance.creatorData.playerId;

        MatchFlow.SendResult(winnerID,
            $"match time over — tokens home {creatorFinished}–{joinerFinished}, progress {creatorProgress}–{joinerProgress}" +
            (fromBoard ? " (reported board)" : " (no reported board — server count)") +
            (creatorFinished == joinerFinished && creatorProgress == joinerProgress ? ", level — creator wins the tie" : ""),
            GameGUIController.FlowHomeCounts());
        RpcTimerEnded(winnerID);
    }

    // This runs on all clients whenever remainingTime changes
    private void OnTimeChanged(int oldTime, int newTime)
    {
        if (GameGUIController.insta.countDownGameTimer == null) return;
        int minutes = newTime / 60;
        int seconds = newTime % 60;

        GameGUIController.insta.countDownGameTimer.text = $"{minutes:00}:{seconds:00}";

        if (newTime > 180)
            GameGUIController.insta.countDownGameTimer.color = Color.green;
        else if (newTime > 60)
            GameGUIController.insta.countDownGameTimer.color = Color.white;
        else
            GameGUIController.insta.countDownGameTimer.color = Color.red;
    }

    [ClientRpc]
    private void RpcTimerEnded(string winnerID)
    {
        GameGUIController.insta.countDownGameTimer.text = "00:00";
        GameGUIController.insta.countDownGameTimer.color = Color.red;

        NetworkGameManager.Instance.CmdPlayerFinished(true, int.Parse(winnerID));
    }
    #endregion
    void Awake()
    {
        instance = this;

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
                GameGUIController.FlowMatchStarted();
                if (ServerAuthorityEnabled)
                    this.DelayUntil(() => NetworkGameManager.Instance != null
                                          && !string.IsNullOrEmpty(NetworkGameManager.Instance.creatorData.playerId)
                                          && !string.IsNullOrEmpty(NetworkGameManager.Instance.joinerData.playerId),
                                    ServerStartBoard);
                this.Delay(4, () =>
                {

                    if (countdownCoroutine != null)
                        StopCoroutine(countdownCoroutine);
                    countdownCoroutine = StartCoroutine(ServerCountdown());
                    MatchFlow.Log("Ludo", $"match timer started (15:00) — {GameGUIController.FlowWho(0)} vs {GameGUIController.FlowWho(1)}");
                });
            });
        }
    }






    // ═══════════════════════ SERVER-AUTHORITATIVE LUDO (behind a switch) ═══════════════════════
    // With the switch on, the match server rolls every die, checks every move against its own board
    // (LudoServerBoard), passes the turn and decides the result. Phones only ASK (CmdRequestRoll / CmdRequestMove)
    // and play back what the server sends (RpcServerDice / RpcServerMove / RpcServerTurn / RpcMatchWon) — only the
    // server can send a ClientRpc, so a modified phone can no longer pick its dice, move for the opponent, skip turns
    // or announce a win. The old relayed DiceRoll / PawnMove / turn events are ignored while it is on.
    //
    // OFF until tested on two phones against a match server built with it. To switch on: set ServerAuthorityEnabled
    // = true in BOTH repos (ClientSide + ServerSide), deploy the match server and publish the code together — the RPCs
    // and the SyncVar below must be identical on both sides.
    public const bool ServerAuthorityEnabled = false;

    /// <summary>Set by the server when it runs the board; clients read it to decide whose rules they follow.</summary>
    [SyncVar] public bool serverAuthority;

    public static bool ServerAuthority => instance != null && instance.serverAuthority;

    const float TurnSeconds = 24f;          // the phone's own turn bar is 20 s; the server waits a little longer
    const float AfterDiceSeconds = 1.6f;    // dice animation (0.6 s) + the 1 s the phone waits before passing

    LudoServerBoard board;
    int creatorPl;                          // creator's index in the phones' playerObjects (sorted by user id)
    bool passingTurn, matchOver;
    float turnDeadline;
    Coroutine turnClock;

    [Server]
    void ServerStartBoard()
    {
        var ngm = NetworkGameManager.Instance;
        // Phones bubble-sort playerObjects with string.Compare(a, b) == 1 → swap; the first one starts (firstPlayerInGame = 0).
        creatorPl = string.Compare(ngm.creatorData.playerId, ngm.joinerData.playerId) == 1 ? 1 : 0;
        board = new LudoServerBoard(0, System.Environment.TickCount);
        serverAuthority = true;
        ServerPublishBoard();
        turnDeadline = Time.time + TurnSeconds + 10f;   // first turn: the phones are still setting up
        if (turnClock != null) StopCoroutine(turnClock);
        turnClock = StartCoroutine(ServerTurnClock());
        MatchFlow.Log("Ludo", $"server runs the board — {FlowName(0)} vs {FlowName(1)}, {FlowName(board.Turn)} starts");
    }

    string IdOfPl(int pl)
    {
        var ngm = NetworkGameManager.Instance;
        return pl == creatorPl ? ngm.creatorData.playerId : ngm.joinerData.playerId;
    }

    string FlowName(int pl) => MatchFlow.Who(IdOfPl(pl));

    int PlOf(NetworkConnectionToClient conn)
    {
        var ngm = NetworkGameManager.Instance;
        string id = MatchFlow.IdOf(conn);
        if (ngm == null || string.IsNullOrEmpty(id)) return -1;
        if (id == ngm.creatorData.playerId) return creatorPl;
        if (id == ngm.joinerData.playerId) return 1 - creatorPl;
        return -1;
    }

    /// <summary>A phone asks to roll its die.</summary>
    [Command(requiresAuthority = false)]
    public void CmdRequestRoll(NetworkConnectionToClient sender = null)
    {
        if (!serverAuthority || board == null || matchOver || passingTurn) return;
        int pl = PlOf(sender);
        if (pl < 0) return;
        if (pl != board.Turn)
        {
            MatchFlow.Flag("Ludo", sender, "out_of_turn", "asked to roll on the opponent's turn");
            return;
        }
        if (board.PendingSteps != 0)
        {
            MatchFlow.Flag("Ludo", sender, "roll_twice", $"asked to roll again before moving the {board.PendingSteps}");
            return;
        }

        int steps = board.Roll(pl);
        board.SixesThisTurn = steps == 6 ? board.SixesThisTurn + 1 : 0;
        MatchFlow.Log("Ludo", $"{FlowName(pl)} rolled a {steps} (server dice)");
        RpcServerDice(pl, steps);

        if (board.SixesThisTurn == 3)
        {
            MatchFlow.Log("Ludo", $"third 6 in a row — {FlowName(pl)} loses the turn");
            ServerPassTurn(AfterDiceSeconds);
            return;
        }
        var legal = board.LegalPawns(pl, steps);
        if (legal.Count == 0)
        {
            MatchFlow.Log("Ludo", $"{FlowName(pl)} has no legal move with a {steps}");
            ServerPassTurn(AfterDiceSeconds);
            return;
        }
        board.PendingSteps = steps;
        turnDeadline = Time.time + TurnSeconds;
    }

    /// <summary>A phone asks to move one of its tokens by the server's last roll.</summary>
    [Command(requiresAuthority = false)]
    public void CmdRequestMove(int pawn, NetworkConnectionToClient sender = null)
    {
        if (!serverAuthority || board == null || matchOver || passingTurn) return;
        int pl = PlOf(sender);
        if (pl < 0) return;
        if (pl != board.Turn)
        {
            MatchFlow.Flag("Ludo", sender, "out_of_turn", $"asked to move token {pawn + 1} on the opponent's turn");
            return;
        }
        int steps = board.PendingSteps;
        if (steps <= 0)
        {
            MatchFlow.Flag("Ludo", sender, "move_without_roll", $"asked to move token {pawn + 1} without a roll");
            return;
        }
        if (!board.CanMove(pl, pawn, steps))
        {
            int at = pawn >= 0 && pawn < 4 ? board.Pos[pl][pawn] : -99;
            MatchFlow.Flag("Ludo", sender, "illegal_move", $"token {pawn + 1} by {steps} from {(at < 0 ? "base" : "square " + at)}");
            return;
        }

        var r = board.Apply(pl, pawn, steps);
        board.PendingSteps = 0;
        MatchFlow.Log("Ludo", $"{FlowName(pl)} moved token {pawn + 1} by {steps}: {(r.From < 0 ? "base" : "square " + r.From)} → " +
                              (r.ReachedHome ? "home" : "square " + r.To));
        if (r.Captured >= 0)
            MatchFlow.Log("Ludo", $"{FlowName(1 - pl)}'s token {r.Captured + 1} was captured by {FlowName(pl)} — back to base");
        if (r.ReachedHome)
            MatchFlow.Log("Ludo", $"{FlowName(pl)}'s token {pawn + 1} is home ({board.HomeCount(pl)}/4)");
        RpcServerMove(pl, pawn, steps);
        ServerPublishBoard();

        if (r.Won)
        {
            ServerWin(pl, "all 4 tokens home");
            return;
        }
        // The phone animates step by step (MoveBySteps), then runs its own extra-roll / turn-end.
        float moveSeconds = 1f + steps * 0.35f;
        if (r.ExtraRoll)
        {
            MatchFlow.Log("Ludo", $"{FlowName(pl)} rolls again ({(r.Captured >= 0 ? "capture" : r.ReachedHome ? "token home" : "rolled a 6")})");
            turnDeadline = Time.time + moveSeconds + TurnSeconds;
        }
        else
        {
            ServerPassTurn(moveSeconds + 1f);
        }
    }

    [Server]
    void ServerPassTurn(float delay)
    {
        passingTurn = true;
        board.PendingSteps = 0;
        this.Delay(delay, () =>
        {
            if (matchOver) return;
            board.Turn = 1 - board.Turn;
            board.PendingSteps = 0;
            board.SixesThisTurn = 0;
            passingTurn = false;
            turnDeadline = Time.time + TurnSeconds;
            ServerPublishBoard();
            MatchFlow.Log("Ludo", $"turn → {FlowName(board.Turn)}");
            RpcServerTurn(board.Turn);
        });
    }

    // The server's own turn timer: a player who neither rolls nor moves loses the turn (the phone's bar does the same
    // locally; this one is the one that counts). Paused while a player is disconnected.
    [Server]
    IEnumerator ServerTurnClock()
    {
        var wait = new WaitForSeconds(0.5f);
        while (!matchOver)
        {
            yield return wait;
            var ngm = NetworkGameManager.Instance;
            if (ngm != null && ngm.currentPlayerCount < 2) { turnDeadline += 0.5f; continue; }
            if (passingTurn || Time.time < turnDeadline) continue;
            MatchFlow.Log("Ludo", $"turn timer ran out for {FlowName(board.Turn)}" + (board.PendingSteps > 0 ? $" (rolled a {board.PendingSteps}, no move)" : " (did not roll)"));
            ServerPassTurn(0f);
        }
    }

    // Keep the reconnect snapshot on the server's own board (phones' SaveBoardState reports are ignored while on).
    [Server]
    void ServerPublishBoard()
    {
        if (GameGUIController.insta != null) GameGUIController.insta.reconnectSnapshot = board.Snapshot(creatorPl);
    }

    [Server]
    void ServerWin(int pl, string reason)
    {
        matchOver = true;
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        string winnerId = IdOfPl(pl);
        MatchFlow.SendResult(winnerId, reason, ServerBoardScore());
        // Settlement: on the match server (ServerSide) this submits the result with the deployment's auth; in a
        // player build it only logs. CHECK with the backend that Ludo is not also settled another way.
        ApiAndRoomManager._instance.WinnerLossChallenge(winnerId);
        RpcMatchWon(winnerId);
    }

    [Server]
    void ServerTimeOver()
    {
        int c = creatorPl, j = 1 - creatorPl;
        int ch = board.HomeCount(c), jh = board.HomeCount(j), cp = board.Progress(c), jp = board.Progress(j);
        int winnerPl = ch != jh ? (ch > jh ? c : j) : cp != jp ? (cp > jp ? c : j) : c;   // level → creator, as before
        matchOver = true;
        string winnerId = IdOfPl(winnerPl);
        MatchFlow.SendResult(winnerId, $"match time over — tokens home {ch}–{jh}, progress {cp}–{jp} (server board)" +
                                       (ch == jh && cp == jp ? ", level — creator wins the tie" : ""), ServerBoardScore());
        ApiAndRoomManager._instance.WinnerLossChallenge(winnerId);
        RpcTimerEnded(winnerId);
    }

    string ServerBoardScore() =>
        $"tokens home {FlowName(creatorPl)} {board.HomeCount(creatorPl)} – {board.HomeCount(1 - creatorPl)} {FlowName(1 - creatorPl)}";

    [ClientRpc]
    void RpcServerDice(int pl, int steps)
    {
        var game = LudoGame.GameManager.Instance != null ? LudoGame.GameManager.Instance.miniGame as LudoGameController : null;
        if (game != null) game.ApplyDice(pl, steps);
    }

    [ClientRpc]
    void RpcServerMove(int pl, int pawn, int steps)
    {
        var game = LudoGame.GameManager.Instance != null ? LudoGame.GameManager.Instance.miniGame as LudoGameController : null;
        if (game != null) game.ApplyMove(pl, pawn, steps);
    }

    [ClientRpc]
    void RpcServerTurn(int pl)
    {
        if (GameGUIController.insta != null) GameGUIController.insta.ApplyServerTurn(pl);
    }

    [ClientRpc]
    void RpcMatchWon(string winnerId)
    {
        NetworkGameManager.Instance.CmdPlayerFinished(true, int.Parse(winnerId));
    }
}

