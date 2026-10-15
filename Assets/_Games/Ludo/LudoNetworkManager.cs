
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;

public class LudoNetworkManager : NetworkBehaviour, IMultiSeatTable
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
            while (TableShort())
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
        if (IsMultiSeat)
        {
            LudoFourPlayerResult.Show(winnerID, "Time over - most tokens home wins");
            return;
        }

        NetworkGameManager.Instance.CmdPlayerFinished(true, int.Parse(winnerID));
    }
    #endregion
    void Awake()
    {
        instance = this;
        MultiSeatTable.Register(this);

    }





    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("✅ OnStartServer called - Server started");
        ServerInitSeats();

        if (NetworkServer.active)
        {
            Debug.Log("🔄 Waiting for players to connect...");
            this.DelayUntil(() => NetworkServer.connections.Count >= SeatCount, () =>
            {

                Debug.Log($"✅ {NetworkServer.connections.Count} players connected, setting players...");
                GameGUIController.FlowMatchStarted();
                if (ServerAuthorityEnabled)
                    this.DelayUntil(() => MatchSeats.Active
                                          ? MatchSeats.Current.AllConnected
                                          : NetworkGameManager.Instance != null
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
    public const bool ServerAuthorityEnabled = true;

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
        if (MatchSeats.Active)
        {
            // N players: player index = seat (MatchSeats, from the deployment's Seats list); seat 0 starts.
            creatorPl = 0;
            board = new LudoServerBoard(0, System.Environment.TickCount, MatchSeats.Current.Count);
        }
        else
        {
            var ngm = NetworkGameManager.Instance;
            // Phones bubble-sort playerObjects with string.Compare(a, b) == 1 → swap; the first one starts (firstPlayerInGame = 0).
            creatorPl = string.Compare(ngm.creatorData.playerId, ngm.joinerData.playerId) == 1 ? 1 : 0;
            board = new LudoServerBoard(0, System.Environment.TickCount);
        }
        serverAuthority = true;
        boardStarted = true;
        if (MatchSeats.Active)
            for (int s = 0; s < board.Seats; s++) ServerSetSeat(s, connected: MatchSeats.Current.IsConnected(s));
        ServerPublishBoard();
        turnDeadline = Time.time + TurnSeconds + 10f;   // first turn: the phones are still setting up
        if (turnClock != null) StopCoroutine(turnClock);
        turnClock = StartCoroutine(ServerTurnClock());
        MatchFlow.Log("Ludo", $"server runs the board — {FlowPlayers()}, {FlowName(board.Turn)} starts");
    }

    /// <summary>Players this match waits for: the deployment's seat count, else 2.</summary>
    static int SeatCount => MatchSeats.Active ? MatchSeats.Current.Count : 2;

    /// <summary>Fewer than 2 players connected: the clocks wait (2 players: one of them dropped).</summary>
    static bool TableShort()
    {
        if (MatchSeats.Active) return MatchSeats.Current.ConnectedCount < 2;
        return NetworkGameManager.Instance != null && NetworkGameManager.Instance.currentPlayerCount < 2;
    }

    string FlowPlayers()
    {
        var names = new List<string>();
        for (int s = 0; s < board.Seats; s++) names.Add(FlowName(s));
        return string.Join(" vs ", names);
    }

    string IdOfPl(int pl)
    {
        if (MatchSeats.Active) return MatchSeats.Current.IdOf(pl);
        var ngm = NetworkGameManager.Instance;
        return pl == creatorPl ? ngm.creatorData.playerId : ngm.joinerData.playerId;
    }

    string FlowName(int pl) => MatchFlow.Who(IdOfPl(pl));

    int PlOf(NetworkConnectionToClient conn)
    {
        if (MatchSeats.Active) return conn == null ? -1 : MatchSeats.Current.SeatOfConnection(conn.connectionId);
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
        foreach (var c in r.Captures)
            MatchFlow.Log("Ludo", $"{FlowName(c.Seat)}'s token {c.Pawn + 1} was captured by {FlowName(pl)} — back to base");
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
            int next = board.NextTurn();
            if (next >= 0) board.Turn = next;
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
            if (TableShort()) { turnDeadline += 0.5f; continue; }
            if (passingTurn || Time.time < turnDeadline) continue;
            MatchFlow.Log("Ludo", $"turn timer ran out for {FlowName(board.Turn)}" + (board.PendingSteps > 0 ? $" (rolled a {board.PendingSteps}, no move)" : " (did not roll)"));
            ServerPassTurn(0f);
        }
    }

    // Keep the reconnect snapshot on the server's own board (phones' SaveBoardState reports are ignored while on).
    [Server]
    void ServerPublishBoard()
    {
        if (GameGUIController.insta != null)
            GameGUIController.insta.reconnectSnapshot = board.Seats > 2 ? board.SnapshotV2() : board.Snapshot(creatorPl);
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
        if (board.Seats > 2)
        {
            // Rule A: the best-placed player wins (tokens home, then progress; a full tie keeps seat order).
            var ranking = board.Ranking();
            matchOver = true;
            string topId = IdOfPl(ranking[0]);
            MatchFlow.SendResult(topId, "match time over — ranking " + string.Join(", ", ranking.ConvertAll(s =>
                $"{FlowName(s)} {board.HomeCount(s)} home / {board.Progress(s)}")) + " (server board)", ServerBoardScore());
            ApiAndRoomManager._instance.WinnerLossChallenge(topId);
            RpcTimerEnded(topId);
            return;
        }
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

    string ServerBoardScore()
    {
        if (board.Seats > 2)
        {
            var parts = new List<string>();
            for (int s = 0; s < board.Seats; s++) parts.Add($"{FlowName(s)} {board.HomeCount(s)}");
            return "tokens home " + string.Join(" – ", parts);
        }
        return $"tokens home {FlowName(creatorPl)} {board.HomeCount(creatorPl)} – {board.HomeCount(1 - creatorPl)} {FlowName(1 - creatorPl)}";
    }

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
        if (IsMultiSeat)
        {
            int othersPlaying = 0;
            foreach (var seat in seatList) if (seat.playerId != winnerId && seat.state == SeatPlaying) othersPlaying++;
            LudoFourPlayerResult.Show(winnerId, othersPlaying == 0 ? "Last player at the table" : "All 4 tokens home");
            return;
        }
        NetworkGameManager.Instance.CmdPlayerFinished(true, int.Parse(winnerId));
    }

    // ═══════════════════════ N-PLAYER SEATS (MaxPlayers > 2, ludo-4-player plan §4.2–4.4) ═══════════════════════
    // Every phone sees the table through seatList (who sits where, playing / finished / left / abandoned, connected).
    // A seat that leaves or is abandoned is skipped and its tokens go home; the others play on. The match is reported
    // once, when one seat is left playing (rule A: that seat wins) or someone gets all 4 tokens home.

    public const int SeatPlaying = 0, SeatFinished = 1, SeatLeft = 2, SeatAbandoned = 3;

    [System.Serializable]
    public struct SeatInfo
    {
        public int seat;
        public string playerId;
        public string name;
        public int state;
        public bool connected;
    }

    /// <summary>2 for a normal match; 3–4 when the deployment seats more players.</summary>
    [SyncVar] public int seatCount = 2;
    public readonly SyncList<SeatInfo> seatList = new SyncList<SeatInfo>();

    /// <summary>This table has more than 2 seats (readable on phones and on the server).</summary>
    public static bool IsMultiSeat => instance != null && instance.seatCount > 2;

    const float SeatDropGraceSeconds = 30f;   // same grace as the 2-player abandon watch
    const int SeatReconnectLimit = 5;         // same limit as the 2-player reconnect rule
    readonly Dictionary<int, Coroutine> seatDropTimers = new Dictionary<int, Coroutine>();
    readonly Dictionary<int, int> seatReconnects = new Dictionary<int, int>();
    bool boardStarted;

    [Server]
    void ServerInitSeats()
    {
        if (!MatchSeats.Active) return;
        var t = MatchSeats.Current;
        seatCount = t.Count;
        seatList.Clear();
        for (int s = 0; s < t.Count; s++)
            seatList.Add(new SeatInfo { seat = s, playerId = t.IdOf(s), name = t.NameOf(s) ?? "", state = SeatPlaying, connected = t.IsConnected(s) });
        var who = new List<string>();
        for (int s = 0; s < t.Count; s++) who.Add($"seat {s} {MatchFlow.Who(t.IdOf(s))}");
        MatchFlow.Log("Ludo", $"{t.Count}-player table: {string.Join(", ", who)}");
    }

    [Server]
    void ServerSetSeat(int seat, int? state = null, bool? connected = null)
    {
        if (seat < 0 || seat >= seatList.Count) return;
        var info = seatList[seat];
        if (state.HasValue) info.state = state.Value;
        if (connected.HasValue) info.connected = connected.Value;
        string name = MatchSeats.Current.NameOf(seat);
        if (!string.IsNullOrEmpty(name)) info.name = name;
        seatList[seat] = info;
    }

    // IMultiSeatTable: the shared networking reports seat events here while the Ludo scene runs (MultiSeatTable).
    public void ServerSeatConnected(int seat) => ServerSeatBack(seat);
    public void ServerSeatDisconnected(int seat) => ServerSeatDrop(seat);
    public void ServerSeatLeft(int seat, string why) => ServerSeatGone(seat, SeatLeft, why);
    bool IMultiSeatTable.IsMultiSeat => seatCount > 2;

    void OnDestroy() => MultiSeatTable.Unregister(this);

    [Server]
    void ServerSeatBack(int seat)
    {
        if (seatDropTimers.TryGetValue(seat, out Coroutine timer) && timer != null) StopCoroutine(timer);
        bool wasDropped = seatDropTimers.Remove(seat);
        ServerSetSeat(seat, connected: true);
        if (!boardStarted) return;

        int n = seatReconnects.TryGetValue(seat, out int c) ? c + 1 : 1;
        seatReconnects[seat] = n;
        MatchFlow.Log("Ludo", $"{FlowName(seat)} is back (reconnect {n}{(wasDropped ? "" : ", no drop seen")})");
        if (n >= SeatReconnectLimit)
            ServerSeatGone(seat, SeatAbandoned, $"reconnected {n} times");
        else if (GameGUIController.insta != null && board != null)
            ServerPublishBoard();   // the returning phone asks for ReconnectGame and gets the server board
    }

    [Server]
    void ServerSeatDrop(int seat)
    {
        ServerSetSeat(seat, connected: false);
        if (matchOver || board == null || seat >= board.Seats || board.State[seat] != LudoServerBoard.SeatState.Playing) return;
        MatchFlow.Log("Ludo", $"{FlowName(seat)} dropped — seat kept for {SeatDropGraceSeconds:0} s");
        if (seatDropTimers.TryGetValue(seat, out Coroutine old) && old != null) StopCoroutine(old);
        seatDropTimers[seat] = StartCoroutine(SeatDropTimer(seat));
    }

    IEnumerator SeatDropTimer(int seat)
    {
        yield return new WaitForSecondsRealtime(SeatDropGraceSeconds);
        seatDropTimers.Remove(seat);
        if (!MatchSeats.Current.IsConnected(seat))
            ServerSeatGone(seat, SeatAbandoned, $"did not come back within {SeatDropGraceSeconds:0} s");
    }

    /// <summary>Seat <paramref name="seat"/> is out of the match: its tokens go home, its turns are skipped, the others
    /// play on. One seat left playing wins (rule A).</summary>
    [Server]
    void ServerSeatGone(int seat, int state, string why)
    {
        if (matchOver) return;
        if (board == null)
        {
            // Before the board starts nobody has moved: the backend refunds a leaver before the start (plan §5.3).
            MatchFlow.Log("Ludo", $"{MatchFlow.Who(MatchSeats.Current.IdOf(seat))} {why} before the start");
            ServerSetSeat(seat, state: state);
            RpcSeatState(seat, state);
            return;
        }
        if (seat < 0 || seat >= board.Seats || board.State[seat] != LudoServerBoard.SeatState.Playing) return;

        bool hadTurn = board.Turn == seat;
        board.Leave(seat);
        ServerSetSeat(seat, state: state);
        RpcSeatState(seat, state);
        MatchFlow.Log("Ludo", $"{FlowName(seat)} {why} — {(state == SeatLeft ? "left" : "abandoned")}, tokens back to base ({board.PlayingCount} still playing)");
        ServerPublishBoard();

        if (board.PlayingCount == 1)
        {
            for (int s = 0; s < board.Seats; s++)
                if (board.State[s] == LudoServerBoard.SeatState.Playing) { ServerWin(s, "the last player still playing"); return; }
        }
        if (board.PlayingCount == 0) return;   // cannot happen: the match ends at one
        if (hadTurn) ServerPassTurn(0f);
    }

    [ClientRpc]
    void RpcSeatState(int seat, int state)
    {
        if (GameGUIController.insta != null) GameGUIController.insta.ApplySeatState(seat, state);
    }
}
