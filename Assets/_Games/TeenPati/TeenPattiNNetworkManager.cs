using System.Collections.Generic;
using System.Security.Cryptography;
using Mirror;
using TeenPattiGame;
using Unity.Mathematics;
using UnityEngine;

public class TeenPattiNNetworkManager : NetworkBehaviour
{
    public static TeenPattiNNetworkManager instance;
    public GameObject playerPrefab;
    public RoomCustomPropertiesTeenPatti roomCustomPropertiesTeenPatti;
    void Awake()
    {
        instance = this;
    }

    // ===================== SERVER AUTHORITY - GUARD STATE =====================
    // Every Cmd in this file relays straight to its Rpc with no checks - a Photon port that
    // never grew a trust model. That lets a modified client deal itself cards, set the pot or
    // announce itself the winner. This region is the server-side state those checks need.
    //
    // Nothing here is a [Command]/[ClientRpc], but the SyncVars still change this
    // NetworkBehaviour's serialization layout, so this file must stay identical in RituGames
    // and RityGamesServer and the two builds must be deployed together. Verify with:
    //   python mirror_hash_manifest.py --diff <client> <server> --filter TeenPatti

    public enum TPSeat { None = 0, Creator = 1, Joiner = 2 }

    /// <summary>Increments once per dealt hand. Every money-changing intent carries the hand it
    /// believed it was acting in, so a replayed or late packet from the previous hand is refused
    /// instead of applied.</summary>
    [SyncVar] public int handId;

    /// <summary>Carrom's gameEnded kill switch (CarromNetworkManager.cs:35). Once a hand is
    /// finalised nothing may move it again - this is what stops a second winner claim.</summary>
    [SyncVar] public bool handEnded;

    /// <summary>Whose turn it is, as a SEAT rather than a player id. The id a client sends is its
    /// own claim (MirrorPlayerPrefab.CmdSetPlayerData takes playerId straight from the client),
    /// but which connection sits in which seat is decided server-side in AssignPlayerRole.
    /// Written ONLY on the server, so a client that missed the Rpc still converges through the
    /// SyncVar - the same reason Carrom made PlayercurrentTurnId a SyncVar.</summary>
    [SyncVar] public int currentActorSeat = (int)TPSeat.None;

    /// <summary>Bumped on every turn hand-off. One token = one action, so capturing a valid packet
    /// and sending it twice (bet again, see my cards again) fails the second time.</summary>
    [SyncVar] public int turnToken;

    // ---- server-only state. Deliberately NOT SyncVar / SyncDictionary ----
    // Mirror replicates a SyncDictionary in full to every observer and has no per-key privacy,
    // so anything held in one is visible to BOTH players. That is exactly how the current
    // reconnect path leaks hole cards (PlayerInfo.SetCustomArray(LocalSettings.OrgCardsArray, ..)).
    // Canonical hands live here instead, on the match-level manager, so they also outlive a
    // PlayerInfo destroyed by a disconnect.
    readonly Dictionary<TPSeat, int[]> serverHands = new Dictionary<TPSeat, int[]>();
    readonly Dictionary<TPSeat, bool> serverSeen = new Dictionary<TPSeat, bool>();
    readonly HashSet<int> consumedTurnTokens = new HashSet<int>();

    /// <summary>Observe-only to begin with: violations are logged but still allowed through, so a
    /// legal flow that trips a guard shows up in real traffic before it can break a live match.
    /// Flipped to true once the logs are clean.</summary>
    public bool enforceServerAuthority = false;

    /// <summary>The seat that walked out, or None. Guards against a second result panel: the quit
    /// Command is immediately followed by the disconnect it causes, and both reach ServerPlayerQuit.
    /// A SyncVar so a client that reconnects into a finished match can still see what happened.</summary>
    [SyncVar] public int quitSeat = (int)TPSeat.None;

    public TPSeat CurrentActorSeat => (TPSeat)currentActorSeat;

    /// <summary>Which seat a connection actually occupies. NetworkGameManager.CreatorRef/JoinerRef
    /// are assigned server-side in MirrorPlayerPrefab.AssignPlayerRole and refreshed on reconnect,
    /// so they are the one piece of identity a client cannot forge - unlike the playerId string,
    /// which the client supplies itself.</summary>
    [Server]
    public TPSeat SeatForConnection(NetworkConnectionToClient conn)
    {
        if (conn == null) return TPSeat.None;
        NetworkGameManager ngm = NetworkGameManager.Instance;
        if (ngm == null) return TPSeat.None;
        if (ngm.CreatorRef == conn) return TPSeat.Creator;
        if (ngm.JoinerRef == conn) return TPSeat.Joiner;
        return TPSeat.None;
    }

    /// <summary>The backend user id sitting in a seat, for settlement and result UI. Derived on
    /// demand rather than stored a second time - this codebase already records what happens when
    /// it keeps two copies of "who is who" and they drift (GameResetManager.cs:164).</summary>
    public string PlayerIdForSeat(TPSeat seat)
    {
        NetworkGameManager ngm = NetworkGameManager.Instance;
        if (ngm == null) return string.Empty;
        if (seat == TPSeat.Creator) return ngm.creatorData != null ? ngm.creatorData.playerId : string.Empty;
        if (seat == TPSeat.Joiner) return ngm.joinerData != null ? ngm.joinerData.playerId : string.Empty;
        return string.Empty;
    }

    /// <summary>The single gate every money-changing intent passes through. intentHandId and
    /// intentToken are what the CLIENT believed it was acting on; both are checked against the
    /// server's own values so a stale or replayed packet cannot land. Returns false when the
    /// action must be refused.</summary>
    [Server]
    public bool ServerValidateActorIntent(NetworkConnectionToClient sender, int intentHandId, int intentToken, string action, out TPSeat seat)
    {
        seat = SeatForConnection(sender);
        string reason = null;

        if (seat == TPSeat.None)
            reason = "sender is not a seated player";
        else if (handEnded)
            reason = "hand " + handId + " is already finished";
        else if (intentHandId != handId)
            reason = "stale hand (client said " + intentHandId + ", server is on " + handId + ")";
        else if (currentActorSeat != (int)seat)
            reason = "not this player's turn (turn belongs to " + (TPSeat)currentActorSeat + ")";
        else if (intentToken != turnToken)
            reason = "stale turn token (client said " + intentToken + ", server is on " + turnToken + ")";
        else if (consumedTurnTokens.Contains(intentToken))
            reason = "token " + intentToken + " was already spent this turn";

        if (reason == null)
            return true;

        // Out of turn and a replayed token cannot come from lag alone (stale hand / stale token can): flag those.
        if (currentActorSeat != (int)seat && seat != TPSeat.None && !handEnded && intentHandId == handId)
            MatchFlow.Flag(FlowGame, sender, "out_of_turn", action + " while it was the opponent's turn");
        else if (seat != TPSeat.None && consumedTurnTokens.Contains(intentToken) && intentToken == turnToken)
            MatchFlow.Flag(FlowGame, sender, "replayed_action", action + " re-sent with an already used turn token");

        TPLog.Warn("TeenPattiNNetworkManager", "REJECT " + action + " from " + seat
            + "/conn=" + (sender != null ? sender.connectionId.ToString() : "null")
            + " - " + reason + (enforceServerAuthority ? "" : "  [observe-only, allowed through]"));
        return !enforceServerAuthority;
    }

    /// <summary>Marks this turn's single action as spent. Call only after an intent is accepted.</summary>
    [Server]
    public void ServerConsumeTurnToken(int token)
    {
        consumedTurnTokens.Add(token);
    }

    /// <summary>Server-side turn hand-off. Kept OUT of any [Command] wrapper (Carrom's
    /// ServerSwitchTurnFrom pattern) so the server's own game code can call it directly and the
    /// offline path never reaches it.</summary>
    [Server]
    public void ServerSetActorSeat(TPSeat seat, string why)
    {
        if (handEnded)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Turn hand-off to " + seat + " ignored (" + why + ") - hand " + handId + " already finished");
            return;
        }
        currentActorSeat = (int)seat;
        turnToken++;
        TPLog.Change("TeenPattiNNetworkManager", "actorSeat", "Turn -> " + seat + " (hand " + handId + ", token " + turnToken + ") because " + why);
    }

    /// <summary>Opens a new hand. Clears every per-hand server record so nothing leaks between
    /// rounds - the leak the master-client workarounds in GameStartManager.cs:488 paper over.</summary>
    [Server]
    public void ServerBeginHand(string why)
    {
        handId++;
        handEnded = false;
        turnToken++;
        currentActorSeat = (int)TPSeat.None;
        serverHands.Clear();
        serverSeen.Clear();
        consumedTurnTokens.Clear();
        serverPacked.Clear();
        TPLog.Change("TeenPattiNNetworkManager", "handId", "Hand " + handId + " opened (" + why + ")");
        if (MatchFlow.Enabled) MatchFlow.Begin(FlowGame, $"game started — hand {handId}, {FlowSeatName(TPSeat.Creator)} vs {FlowSeatName(TPSeat.Joiner)}, boot {(Pot.instance != null ? Pot.instance.startPotAmount.ToString() : "?")}");
    }

    /// <summary>Closes the hand exactly once. Returns false if it was already closed, so every
    /// terminal path (showdown, pack, show, side-show, timeout, disconnect, pot limit) can call
    /// this and only the first one settles.</summary>
    [Server]
    public bool ServerEndHand(string reason)
    {
        if (handEnded)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Hand " + handId + " end ignored (" + reason + ") - already finished");
            return false;
        }
        handEnded = true;
        currentActorSeat = (int)TPSeat.None;
        TPLog.Change("TeenPattiNNetworkManager", "handEnded", "Hand " + handId + " finished: " + reason);
        return true;
    }

    // ===================== SERVER-SIDE SETTLEMENT =====================
    // Closing the round on the backend was the last part of a hand still owned by a phone.
    // PlayerInfo.RPCWinner posted /casinos/handle-rounds from whichever client had isOwned, and that
    // request carried winner_id, the pot and both stakes - every number a modified client would want
    // to write. The player build no longer sends it ("Client not submitting teenpatti round result"),
    // and nothing here replaced it: RPCWinner is only reachable through a [ClientRpc], which a
    // dedicated server never runs, and its POST sits behind an isOwned check that is false on a
    // server anyway. So the round this build opened with /casinos/create-new-round was never closed
    // and no winner was ever paid.
    //
    // Everything below reads server-owned state only: the hands this build dealt in ServerDealHand,
    // the packs recorded as their state Commands land here, PlayerCustomProperties' SyncDictionary
    // (written on the server) and the room's collected-cash property. No new SyncVar, Command or
    // ClientRpc, so the serialization layout this file shares with the client build is untouched.

    /// <summary>Seats that are out of the current hand - packed, side-show losers, walk-outs.</summary>
    readonly HashSet<TPSeat> serverPacked = new HashSet<TPSeat>();

    /// <summary>Guards against paying the same hand twice: a pack, a showdown and a quit can all
    /// reach the settlement within a few frames of each other.</summary>
    int settledForHandId = -1;

    /// <summary>Records a seat as out of the hand. Called from
    /// PlayerCurrentState.CmdUpdateCurrentPlayerStateOnNetwork for Packed and OutOfTable - the one
    /// place every pack funnels through, exactly like ServerNoteTurnOwner above. The seat comes from
    /// the player object's owning connection because that Cmd is requiresAuthority = false, so
    /// nothing in its payload can be trusted to name a seat.</summary>
    [Server]
    public void ServerNotePacked(NetworkConnectionToClient owner, string who)
    {
        TPSeat seat = SeatForConnection(owner);
        if (seat == TPSeat.None)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "'" + who + "' is out of the hand but its connection sits in neither seat - not recorded");
            return;
        }

        if (!serverPacked.Add(seat))
            return;

        TPLog.Change("TeenPattiNNetworkManager", "packed", seat + " ('" + who + "') is out of hand " + handId);
        ServerSettleIfOneSeatLeft(seat + " is out of the hand");
    }

    /// <summary>True while a seat is still contesting the pot: it was dealt in, it has not packed
    /// and it has not walked out.</summary>
    bool SeatIsLive(TPSeat seat)
    {
        return serverHands.ContainsKey(seat) && !serverPacked.Contains(seat) && quitSeat != (int)seat;
    }

    /// <summary>Pays out the moment a pack leaves one player standing. Teen Patti ends there - no
    /// showdown follows it, so without this nothing would ever close the round.</summary>
    [Server]
    public void ServerSettleIfOneSeatLeft(string why)
    {
        bool creatorLive = SeatIsLive(TPSeat.Creator);
        bool joinerLive = SeatIsLive(TPSeat.Joiner);
        if (creatorLive == joinerLive)
            return;

        ServerSettleHand(creatorLive ? TPSeat.Creator : TPSeat.Joiner, why);
        ServerEndHand(why);
    }

    /// <summary>Decides who won and closes the round on the backend. Pass a seat for the endings
    /// that need no card comparison (a pack or a quit leaves one player standing); pass
    /// TPSeat.None for a showdown and the server compares the hands it dealt itself.</summary>
    [Server]
    public void ServerSettleHand(TPSeat forcedWinner, string why)
    {
        if (settledForHandId == handId)
        {
            TPLog.Flow("TeenPattiNNetworkManager", "Hand " + handId + " is already settled - '" + why + "' pays nothing");
            return;
        }

        string roundId = staticVariables.CurrentRoundID;
        if (string.IsNullOrEmpty(roundId))
        {
            if (MatchFlow.Enabled) MatchFlow.Log(FlowGame, $"hand {handId} NOT settled ({why}) — no casino round id");
            TPLog.Warn("TeenPattiNNetworkManager", "Hand " + handId + " NOT settled (" + why
                + ") - there is no round id, so /casinos/create-new-round never came back");
            return;
        }

        TPSeat winner = forcedWinner != TPSeat.None ? forcedWinner : ServerDecideWinner();
        if (winner == TPSeat.None)
        {
            if (MatchFlow.Enabled) MatchFlow.Log(FlowGame, $"hand {handId} NOT settled ({why}) — no winner could be decided");
            TPLog.Warn("TeenPattiNNetworkManager", "Hand " + handId + " NOT settled (" + why + ") - no winner could be decided");
            return;
        }

        string winnerId = PlayerIdForSeat(winner);
        if (string.IsNullOrEmpty(winnerId))
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Hand " + handId + " NOT settled (" + why + ") - "
                + winner + " has no backend player id");
            return;
        }

        System.Numerics.BigInteger pot = roomCustomPropertiesTeenPatti != null
            ? roomCustomPropertiesTeenPatti.GetTableCollectedCash(LocalSettings.TableCashKey)
            : 0;

        Dictionary<string, string> data = new Dictionary<string, string>
        {
            { "round_id", roundId },
            { "winner_id", winnerId },
            { "winner_amount", pot.ToString() },
            { "first_player_amount", SeatStake(TPSeat.Creator).ToString() },
            { "second_player_amount", SeatStake(TPSeat.Joiner).ToString() },
        };

        settledForHandId = handId;
        TPLog.Change("TeenPattiNNetworkManager", "settlement", "Hand " + handId + " won by " + winner
            + " (playerId '" + winnerId + "', pot " + pot + ") because " + why + " -> closing round " + roundId);

        if (MatchFlow.Enabled)
        {
            TPSeat loser = winner == TPSeat.Creator ? TPSeat.Joiner : TPSeat.Creator;
            bool flowShowdown = forcedWinner == TPSeat.None && SeatIsLive(TPSeat.Creator) && SeatIsLive(TPSeat.Joiner);
            string flowReason = flowShowdown ? "showdown"
                : quitSeat == (int)loser ? $"{FlowSeatName(loser)} left the match"
                : $"{FlowSeatName(loser)} packed";
            if (flowShowdown)
                MatchFlow.Log(FlowGame, $"show: {FlowSeatName(TPSeat.Creator)} ({FlowHand(TPSeat.Creator)}) vs {FlowSeatName(TPSeat.Joiner)} ({FlowHand(TPSeat.Joiner)}) — {FlowSeatName(winner)} wins pot {pot}");
            else
                MatchFlow.Log(FlowGame, $"{FlowSeatName(winner)} wins pot {pot} — {flowReason}");
            MatchFlow.SendResult(winnerId, flowReason,
                $"pot {pot}; staked {FlowSeatName(TPSeat.Creator)} {SeatStake(TPSeat.Creator)} – {SeatStake(TPSeat.Joiner)} {FlowSeatName(TPSeat.Joiner)}");
        }

        StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.End_Round_casino_Url, data,
            accepted =>
            {
                TPLog.Flow("TeenPattiNNetworkManager", "Round " + roundId + " closed by the backend");
            },
            rejected =>
            {
                // Drop the guard so a later terminal path can try again rather than losing the
                // payout silently - the same retry rule ApiAndRoomManager.DrawChallenge follows.
                settledForHandId = -1;
                TPLog.Warn("TeenPattiNNetworkManager", "Round " + roundId + " REJECTED by the backend: " + rejected);
            }, withSettlementAuth: true));
    }

    /// <summary>The showdown, decided from serverHands. Mirrors the client's own comparison
    /// (GameResultsManager.PlayerComparer, with the pair tie-break of PlayerDataComparer) so the
    /// backend pays exactly the player the table is being shown.</summary>
    TPSeat ServerDecideWinner()
    {
        bool creatorLive = SeatIsLive(TPSeat.Creator);
        bool joinerLive = SeatIsLive(TPSeat.Joiner);
        if (creatorLive != joinerLive)
            return creatorLive ? TPSeat.Creator : TPSeat.Joiner;
        if (!creatorLive)
            return TPSeat.None;

        int creatorRank, creatorScore, joinerRank, joinerScore;
        int[] creatorValues, joinerValues;
        if (!ServerEvaluateSeat(TPSeat.Creator, out creatorRank, out creatorScore, out creatorValues) ||
            !ServerEvaluateSeat(TPSeat.Joiner, out joinerRank, out joinerScore, out joinerValues))
            return TPSeat.None;

        TPLog.Flow("TeenPattiNNetworkManager", "Showdown - Creator rank " + creatorRank + " score " + creatorScore
            + " against Joiner rank " + joinerRank + " score " + joinerScore);

        // Two pairs of the same rank are separated by the pair itself, not by the sorted card
        // values - the client does exactly this in its rank-3 branch.
        if (creatorRank == 3 && joinerRank == 3 && creatorScore != joinerScore)
            return creatorScore > joinerScore ? TPSeat.Creator : TPSeat.Joiner;

        int comparison = CompareHands(creatorRank, creatorValues, joinerRank, joinerValues);
        if (comparison != 0)
            return comparison > 0 ? TPSeat.Creator : TPSeat.Joiner;

        TPLog.Warn("TeenPattiNNetworkManager", "Showdown of hand " + handId + " is an exact tie (rank "
            + creatorRank + ") - the pot goes to the Creator, who was dealt first");
        return TPSeat.Creator;
    }

    /// <summary>Positive when hand A wins: higher rank first, then the sorted card values.</summary>
    static int CompareHands(int rankA, int[] valuesA, int rankB, int[] valuesB)
    {
        if (rankA != rankB)
            return rankA > rankB ? 1 : -1;

        for (int i = 0; i < 3 && i < valuesA.Length && i < valuesB.Length; i++)
        {
            if (valuesA[i] != valuesB[i])
                return valuesA[i] > valuesB[i] ? 1 : -1;
        }
        return 0;
    }

    /// <summary>Rank, score and sorted card values of a seat's hand, read straight from the deal
    /// this server made. PlayerCardsRankAndScoreCalc.CardValuesArrayForPlayer is deliberately NOT
    /// used: that static is only refreshed inside the sequence checks, so a trail (rank 7) leaves
    /// the previous hand's values sitting in it.</summary>
    bool ServerEvaluateSeat(TPSeat seat, out int rank, out int scores, out int[] values)
    {
        rank = 0;
        scores = 0;
        values = null;

        int[] hand;
        if (!serverHands.TryGetValue(seat, out hand) || hand == null || hand.Length < 3)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Cannot rank " + seat + "'s hand - it was never dealt here");
            return false;
        }

        TeenPattiGame.GameManager gameManager = TeenPattiGame.GameManager.Instance;
        if (gameManager == null || gameManager.AllCards == null || gameManager.AllCards.Card == null)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Cannot rank " + seat + "'s hand - the card deck is not loaded here");
            return false;
        }

        CardProperty[] deck = gameManager.AllCards.Card;
        for (int c = 0; c < 3; c++)
        {
            if (hand[c] < 0 || hand[c] >= deck.Length || deck[hand[c]] == null)
            {
                TPLog.Warn("TeenPattiNNetworkManager", "Cannot rank " + seat + "'s hand - card index "
                    + hand[c] + " is not in the " + deck.Length + " card deck");
                return false;
            }
        }

        CardProperty card0 = deck[hand[0]];
        CardProperty card1 = deck[hand[1]];
        CardProperty card2 = deck[hand[2]];

        PlayerCardsRankAndScoreCalc.CalculateRankAndScores(card0, card1, card2);
        rank = PlayerCardsRankAndScoreCalc.Rank;
        scores = PlayerCardsRankAndScoreCalc.Scores;

        values = new int[] { (int)card0.Card, (int)card1.Card, (int)card2.Card };
        System.Array.Sort(values);
        System.Array.Reverse(values);
        return true;
    }

    /// <summary>The PlayerInfo sitting in a seat, matched by owning connection rather than by
    /// position in a list - PlayingList is rebuilt locally from synced state, so its order is not
    /// the same on every build.</summary>
    PlayerInfo PlayerInfoForSeat(TPSeat seat)
    {
        TeenPattiGame.GameManager gameManager = TeenPattiGame.GameManager.Instance;
        if (gameManager == null || gameManager.playersList == null)
            return null;

        for (int i = 0; i < gameManager.playersList.Count; i++)
        {
            PlayerInfo info = gameManager.playersList[i];
            if (info != null && SeatForConnection(info.connectionToClient) == seat)
                return info;
        }
        return null;
    }

    /// <summary>What a seat put into this pot: the cash it sat down with, less what it has left.
    /// Both halves live in PlayerCustomProperties' SyncDictionary, which is written on the server -
    /// so this is the number the client used to read off its own UI text, without the UI.</summary>
    System.Numerics.BigInteger SeatStake(TPSeat seat)
    {
        PlayerInfo info = PlayerInfoForSeat(seat);
        if (info == null || info.playerCustomProperties == null)
            return 0;

        return info.playerCustomProperties.GetCustomBigIntegerData("CashInHand")
             - info.playerCustomProperties.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey);
    }

    /// <summary>Records the turn owner from the one place every online turn hand-off funnels
    /// through: PlayerCurrentState.CmdUpdateCurrentPlayerStateOnNetwork(ExecutingTurn). Both
    /// PlayerStateManager.GiveTurnToNext and PlayerCurrentState.SetNextPlayerToExecutingTurn end
    /// there, so this one hook sees them all. The owning connection is server-side data, so the
    /// seat it resolves to cannot be forged - which matters, because that Cmd is
    /// requiresAuthority = false and any client can invoke it for any player object.</summary>
    [Server]
    public void ServerNoteTurnOwner(NetworkConnectionToClient owner, string who)
    {
        TPSeat seat = SeatForConnection(owner);
        if (seat == TPSeat.None)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Turn went to '" + who + "' but its connection sits in neither seat - turn ownership not tracked");
            return;
        }
        if (currentActorSeat == (int)seat)
            return;
        if (MatchFlow.Enabled && !handEnded) MatchFlow.Log(FlowGame, $"turn → {FlowSeatName(seat)}");
        ServerSetActorSeat(seat, "'" + who + "' began his turn");
    }

    // ---- the server is the dealer ----

    /// <summary>Guards against dealing the same hand twice. ServerDealHand is reachable from a
    /// retried or duplicated room-state change, and a second shuffle mid-hand would quietly hand
    /// both players new cards.</summary>
    int dealtForHandId = -1;

    /// <summary>Shuffles and deals. This replaces GameManager.AssignCardsToAllPlayers() for online
    /// play, where the shuffle ran on whichever device had isMasterClient set - and that is the
    /// challenge creator's phone as well as the server (DirectChallengeMatchmaker.cs:607). So the
    /// player who opened the challenge was dealing the cards that decide real money.
    /// UnityEngine.Random is deliberately not used: it is a per-process seeded PRNG whose stream a
    /// modified client can reproduce.</summary>
    [Server]
    public void ServerDealHand()
    {
        if (dealtForHandId == handId)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Deal ignored - hand " + handId + " has already been dealt");
            return;
        }

        // Fully qualified on purpose: 8-Ball declares a GameManager in the GLOBAL namespace
        // (8Ball pool/BallPoolGame/Game/Scripts/Game/GameManager.cs:18) and this file is global too,
        // so a bare `GameManager` binds to that one instead of TeenPatti's.
        TeenPattiGame.GameManager gameManager = TeenPattiGame.GameManager.Instance;
        PlayerStateManager psm = PlayerStateManager.Instance;
        if (gameManager == null || gameManager.AllCards == null || gameManager.AllCards.Card == null || psm == null)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Deal aborted - GameManager/PlayerStateManager not ready yet");
            return;
        }

        int deckSize = gameManager.AllCards.Card.Length;
        int players = psm.PlayingList.Count;
        if (players <= 0 || players * 3 > deckSize)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Deal aborted - " + players + " players against a " + deckSize + " card deck");
            return;
        }

        // Fisher-Yates across the whole deck, then take the top 3 per player. Shuffling the deck and
        // drawing off the top (rather than picking a random card at a time, which is what
        // AssignCardsToAllPlayers did) keeps every hand equally likely and leaves an order that can
        // be replayed if a hand ever has to be audited.
        int[] deck = new int[deckSize];
        for (int i = 0; i < deckSize; i++)
            deck[i] = i;

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            for (int i = deckSize - 1; i > 0; i--)
            {
                int j = NextBelow(rng, i + 1);
                int swap = deck[i];
                deck[i] = deck[j];
                deck[j] = swap;
            }
        }

        int[] dealt = new int[players * 3];
        // Every triple is tagged with the netId of the player it belongs to, so no client has to
        // infer ownership from its own PlayingList order.
        uint[] owners = new uint[players];
        serverHands.Clear();
        serverSeen.Clear();

        for (int p = 0; p < players; p++)
        {
            int[] hand = new int[3];
            for (int c = 0; c < 3; c++)
            {
                int card = deck[(p * 3) + c];
                dealt[(p * 3) + c] = card;
                hand[c] = card;
            }

            PlayerInfo info = psm.PlayingList[p];
            owners[p] = info != null && info.this_photonView != null ? info.this_photonView.netId : 0u;
            TPSeat seat = info != null ? SeatForConnection(info.connectionToClient) : TPSeat.None;
            if (seat == TPSeat.None)
            {
                TPLog.Warn("TeenPattiNNetworkManager", "Dealt to '" + (info != null ? info.name : "null")
                    + "' but its connection sits in neither seat - hand not recorded server-side");
                continue;
            }

            serverHands[seat] = hand;
            serverSeen[seat] = false;

            // Reconnect copy, written by the SERVER now. It used to be written by whichever client
            // had isMasterClient, inside OrignalCardsSetting - so a reconnecting player could only
            // get his cards back if that particular phone was still around to have written them.
            if (info.playerCustomProperties != null)
                info.playerCustomProperties.SetCustomArray(LocalSettings.OrgCardsArray, hand);
        }

        dealtForHandId = handId;
        TPLog.Change("TeenPattiNNetworkManager", "deal", "Server dealt hand " + handId + " to "
            + players + " player(s) from a " + deckSize + " card deck");
        if (MatchFlow.Enabled) MatchFlow.Log(FlowGame, $"cards dealt to {players} player(s)");

        // Delivery still goes out on a broadcast. Making the server the DEALER and making each
        // player's cards PRIVATE are two separate changes: private delivery only works once the
        // server also owns hand termination, because every termination path today (Game_Play.ShowWinner,
        // GameResultsManager.ShowingResult, PlayerStateManager.RemainingPlayerWonGameAutomatic and the
        // side-show) compares both hands locally the instant it fires. serverHands above is the
        // canonical copy that private delivery and server-side evaluation will read from.
        //
        // It goes out addressed by OWNER netId rather than as a bare array in PlayingList order.
        // The old RpcUpDatePlayerCardsArray required every client to have already rebuilt an
        // identically ordered PlayingList at the instant the deal landed, and neither half of that
        // holds: the list is rebuilt from synced state (deals were logged building cards "for 1
        // players" while the server had dealt to 2, leaving the other seat with only its anchors)
        // and its order is client-local, so index j did not have to mean the same player everywhere.
        RpcDealHandsToOwners(owners, dealt);
    }

    /// <summary>The deal, with each triple tagged by the netId of the player it belongs to.
    /// cards[i*3 .. i*3+2] are owners[i]'s three cards.</summary>
    [ClientRpc]
    public void RpcDealHandsToOwners(uint[] owners, int[] cards)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "Deal received for " + (owners != null ? owners.Length : 0)
            + " owner(s), " + (cards != null ? cards.Length : 0) + " card indexes");
        StartCoroutine(ApplyDealWhenOwnersSpawn(owners, cards));
    }

    /// <summary>Applies each triple as soon as its owner's object exists here. The deal can beat the
    /// player objects onto a client, so an owner that is not spawned yet is retried rather than
    /// dropped - dropping it is what left a seat showing nothing but its three Club-10 anchors.</summary>
    System.Collections.IEnumerator ApplyDealWhenOwnersSpawn(uint[] owners, int[] cards)
    {
        if (owners == null || cards == null || cards.Length < owners.Length * 3)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Deal ignored - " + (owners == null ? 0 : owners.Length)
                + " owners against " + (cards == null ? 0 : cards.Length) + " card indexes");
            yield break;
        }

        List<int> pending = new List<int>();
        for (int i = 0; i < owners.Length; i++)
            pending.Add(i);

        float giveUpAt = Time.unscaledTime + 5f;

        while (pending.Count > 0)
        {
            for (int k = pending.Count - 1; k >= 0; k--)
            {
                int i = pending[k];
                PlayerInfo target = FindPlayerByNetId(owners[i]);
                if (target == null)
                    continue;

                PlayerInfo.BuildOriginalCards(target, new int[]
                {
                    cards[(i * 3) + 0],
                    cards[(i * 3) + 1],
                    cards[(i * 3) + 2]
                });
                pending.RemoveAt(k);
            }

            if (pending.Count == 0)
                break;

            if (Time.unscaledTime > giveUpAt)
            {
                string missing = "";
                for (int k = 0; k < pending.Count; k++)
                    missing += (k > 0 ? ", " : "") + owners[pending[k]];
                TPLog.Warn("TeenPattiNNetworkManager", "Deal incomplete - netId(s) " + missing
                    + " never spawned here, those seats have no cards");
                yield break;
            }

            yield return null;
        }

        TPLog.Change("TeenPattiNNetworkManager", "dealApplied", "Deal applied for all "
            + owners.Length + " player(s)");
    }

    static PlayerInfo FindPlayerByNetId(uint netId)
    {
        // Fully qualified: 8-Ball declares a GameManager in the GLOBAL namespace and so is this file.
        TeenPattiGame.GameManager gameManager = TeenPattiGame.GameManager.Instance;
        if (gameManager == null || gameManager.playersList == null)
            return null;

        for (int i = 0; i < gameManager.playersList.Count; i++)
        {
            PlayerInfo candidate = gameManager.playersList[i];
            if (candidate != null && candidate.this_photonView != null && candidate.this_photonView.netId == netId)
                return candidate;
        }

        return null;
    }

    /// <summary>Uniform random int from 0 up to exclusiveMax-1. Plain `bytes % max` would bias the low end
    /// of the deck, which in a 52-card shuffle is a real edge rather than a rounding curiosity.</summary>
    static int NextBelow(RandomNumberGenerator rng, int exclusiveMax)
    {
        if (exclusiveMax <= 1)
            return 0;

        // The fold limit MUST stay in ulong. Computed in uint it overflows to 0 whenever
        // exclusiveMax is a power of two - (2^32 / max) * max is then exactly 2^32 - and
        // `value < 0` is never true, so the rejection loop never exits. A 52-card Fisher-Yates
        // walks exclusiveMax down through 32, 16, 8, 4 and 2, so that hung the dedicated server's
        // main thread on the very first deal.
        ulong limit = (0x100000000UL / (ulong)exclusiveMax) * (ulong)exclusiveMax;

        byte[] buffer = new byte[4];

        // Bounded on purpose. A rejection streak this long is astronomically unlikely, but this
        // runs on the game server's main thread, where an unbounded loop freezes the whole match
        // rather than just producing a bad number.
        for (int attempt = 0; attempt < 64; attempt++)
        {
            rng.GetBytes(buffer);
            uint value = System.BitConverter.ToUInt32(buffer, 0);
            if (value < limit)
                return (int)(value % (uint)exclusiveMax);
        }

        rng.GetBytes(buffer);
        return (int)(System.BitConverter.ToUInt32(buffer, 0) % (uint)exclusiveMax);
    }

    // =========================================================================
    [Command(requiresAuthority = false)]
    public void CmdSpawnPlayer(string name, NetworkConnectionToClient conn = null)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdSpawnPlayer on server for '" + name + "' (conn=" + (conn != null ? conn.connectionId.ToString() : "null") + ")");
        GameObject player = Instantiate(playerPrefab, UnityEngine.Vector3.zero, Quaternion.identity);
        NetworkServer.Spawn(player, conn);
        TPLog.Flow("TeenPattiNNetworkManager", "Player object spawned + given authority to conn=" + (conn != null ? conn.connectionId.ToString() : "null"));
    }
    [ClientRpc]
    public void RpcSpawnPlayer()
    {

    }
    [Command(requiresAuthority = false)]
    public void CmdWaitForOtherPlayersToJoinBeforeStart(int id, float startWaitTime)
    {
        TPLog.Change("TeenPattiNNetworkManager", "cmdWait", "CmdWaitForOtherPlayersToJoinBeforeStart from user " + id + " -> broadcasting countdown " + startWaitTime.ToString("0.0"));
        RpcWaitForOtherPlayersToJoinBeforeStart(id, startWaitTime);
    }

    [ClientRpc]
    public void RpcWaitForOtherPlayersToJoinBeforeStart(int id, float startWaitTime)
    {
        if (id == staticVariables.UserProfiledata.user._id)
        {
            TPLog.Change("TeenPattiNNetworkManager", "rpcWaitSelf", "RpcWaitForOtherPlayersToJoinBeforeStart IGNORED - this countdown was sent by me (" + id + ")");
            return;
        }
        TPLog.Change("TeenPattiNNetworkManager", "rpcWait", "RpcWaitForOtherPlayersToJoinBeforeStart from user " + id + " -> countdown " + startWaitTime.ToString("0.0"));
        GameStartManager.Instance.WaitForOtherPlayersToJoinBeforeStart(startWaitTime);
    }
    [Command(requiresAuthority = false)]
    public void CmdSetRoundValue(int senderId, string currentRoundID)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdSetRoundValue from user " + senderId + " roundId=" + currentRoundID);
        RpcSetRoundValue(senderId, currentRoundID);
    }

    [ClientRpc]
    public void RpcSetRoundValue(int senderId, string currentRoundID)
    {
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            TPLog.Flow("TeenPattiNNetworkManager", "RpcSetRoundValue IGNORED - I am the sender (" + senderId + ")");
            return;
        }
        TPLog.Flow("TeenPattiNNetworkManager", "RpcSetRoundValue accepted, roundId=" + currentRoundID + " (sent by " + senderId + ")");
        GameStartManager.Instance.setRoundValue(currentRoundID);
    }
    [Command(requiresAuthority = false)]
    public void CmdUpdateThisStateOnNetwork(int state, string message)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdUpdateThisStateOnNetwork -> room state " + (RoomState.STATE)state + " msg='" + message + "'");
        if (MatchFlow.Enabled && (RoomState.STATE)state == RoomState.STATE.WaitingForResults)
            MatchFlow.Log(FlowGame, $"{FlowSeatName(CurrentActorSeat)} calls show" + (string.IsNullOrEmpty(message) ? "" : $" ({message})") + $", pot {FlowPot()}");
        RpcUpdateThisStateOnNetwork(state, message);

        // Keep the SERVER's own copy of the room state in step. This Cmd relayed the Rpc and nothing
        // else, and a ClientRpc does not execute on a dedicated server - so on Edgegap
        // RoomStateManager.CurrentRoomState never moved past whatever the server itself had pushed,
        // which is only ever WaitingForPlayers / GameIsStarting. Two things quietly depended on it:
        // PlayerTurnManager's server-side countdown only ticks while the SERVER sees GameIsPlaying,
        // and the deal only happens on CardDistributing.
        //
        // Only the field is set here, not RoomStateManager's whole callback tree: those callbacks
        // drive UI and assume a local player object, which a headless build never has.
        if (RoomStateManager.Instance != null)
            RoomStateManager.Instance.UpdateLocalRoomState((RoomState.STATE)state);

        if ((RoomState.STATE)state == RoomState.STATE.CardDistributing && GameStartManager.Instance != null)
            GameStartManager.Instance.ServerBeginCardDistribution();
    }

    [ClientRpc]
    public void RpcUpdateThisStateOnNetwork(int state, string message)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "RpcUpdateThisStateOnNetwork received -> room state " + (RoomState.STATE)state + " msg='" + message + "'");
        RoomStateManager.Instance.UpdateThisStateOnNetwork((RoomState.STATE)state, message);
    }
    [Command(requiresAuthority = false)]
    public void CmdLeaveBetRpc()
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdLeaveBetRpc -> telling every client to leave the table");
        RpcLeaveBetRpc();
    }

    [ClientRpc]
    public void RpcLeaveBetRpc()
    {
        TPLog.Flow("TeenPattiNNetworkManager", "RpcLeaveBetRpc received -> leaving table");
        RoomStateManager.Instance.LeaveBetRpc();
    }
    [Command(requiresAuthority = false)]
    public void CmdUpdateIsBookedArray(bool[] localIsBooked)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdUpdateIsBookedArray -> seats [" + string.Join(",", localIsBooked) + "]");
        RpcUpdateIsBookedArray(localIsBooked);
    }
    [ClientRpc]
    public void RpcUpdateIsBookedArray(bool[] localIsBooked)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "RpcUpdateIsBookedArray received -> seats [" + string.Join(",", localIsBooked) + "]");
        PositionsManager.Instance.UpdateIsBookedArray(localIsBooked);
    }

    [Command(requiresAuthority = false)]
    public void CmdBookThisSeat(int pos)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdBookThisSeat -> seat " + pos);
        RpcBookThisSeat(pos);
    }

    [ClientRpc]
    public void RpcBookThisSeat(int pos)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "RpcBookThisSeat received -> seat " + pos + " marked booked");
        PositionsManager.Instance.BookThisSeat(pos);
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateListOnPackedOnNetwork()
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdUpdateListOnPackedOnNetwork -> asking all clients to drop packed players");
        RpcUpdateListOnPackedOnNetwork();
    }

    [ClientRpc]
    public void RpcUpdateListOnPackedOnNetwork()
    {
        TPLog.Flow("TeenPattiNNetworkManager", "RpcUpdateListOnPackedOnNetwork received -> cleaning PlayingList");
        PlayerStateManager.Instance.UpdateListOnPackedOnNetwork();
    }

    [Command(requiresAuthority = false)]
    public void CmdUpDateAllPlayersGameCompleted()
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdUpDateAllPlayersGameCompleted -> hand finished for everyone");
        RpcUpDateAllPlayersGameCompleted();

        // The hand is over for everybody, so this is the server's cue to compare the cards it dealt
        // and close the round on the backend. Both clients send this Cmd; ServerSettleHand pays once.
        ServerSettleHand(TPSeat.None, "showdown");
        ServerEndHand("showdown");
    }

    [ClientRpc]
    public void RpcUpDateAllPlayersGameCompleted()
    {
        TPLog.Flow("TeenPattiNNetworkManager", "RpcUpDateAllPlayersGameCompleted received -> all players set to Watching");
        PlayerStateManager.Instance.UpDateAllPlayersGameCompleted();
    }
    [Command(requiresAuthority = false)]
    public void CmdPlayerCardStatus()
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdPlayerCardStatus -> refresh seen/blind indicators everywhere");
        RpcPlayerCardStatus();
    }

    [ClientRpc]
    public void RpcPlayerCardStatus()
    {
        TPLog.Flow("TeenPattiNNetworkManager", "RpcPlayerCardStatus received -> refreshing seen/blind indicators");
        PlayerStateManager.Instance.PlayerCardStatus();
    }

    [Command(requiresAuthority = false)]
    public void CmdUpDatePlayerCardsArray(int[] randomCardsArray)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "CmdUpDatePlayerCardsArray -> dealing " + (randomCardsArray != null ? randomCardsArray.Length : 0) + " card indexes to all clients");
        RpcUpDatePlayerCardsArray(randomCardsArray);
    }

    [ClientRpc]
    public void RpcUpDatePlayerCardsArray(int[] randomCardsArray)
    {
        // Implementation here
        TPLog.Flow("TeenPattiNNetworkManager", "RpcUpDatePlayerCardsArray received -> " + (randomCardsArray != null ? randomCardsArray.Length : 0) + " card indexes, building original cards");
        UIManager.Instance.myPlayerInfo.UpDatePlayerCardsArray(randomCardsArray);
    }

    // ===================== A PLAYER WALKS OUT =====================
    // A deliberate quit and a dropped connection look identical to the server: both end with Mirror
    // destroying the player object, which is why the only leave handling this game had was
    // PlayerInfo.OnDestroy -> PlayerStateManager.OnPlayerLeftRoom, running independently on every
    // machine. They must not be treated the same. A DROP deserves the 30-second WaitingPanel so the
    // player can reconnect. A QUIT is final, and making the opponent wait out that countdown for
    // someone who is never coming back left him on a dead table with no result panel at all -
    // AnnounceWinner and DirectResult do not appear once in the editor logs.
    //
    // So the leaving client says so explicitly, and the SERVER decides the match is over.

    /// <summary>Sent by a player who pressed EXIT, before his scene unloads. requiresAuthority is
    /// false to match every other Command in this file, so the sender is authenticated from its
    /// connection rather than from anything in the payload.</summary>
    [Command(requiresAuthority = false)]
    public void CmdTeenPattiPlayerQuit(NetworkConnectionToClient sender = null)
    {
        TPSeat seat = SeatForConnection(sender);
        if (seat == TPSeat.None)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Quit ignored - conn="
                + (sender != null ? sender.connectionId.ToString() : "null") + " sits in neither seat");
            return;
        }

        ServerPlayerQuit(seat);
    }

    /// <summary>Ends the match and tells the table who walked out. [Server] and deliberately OUTSIDE
    /// the Command wrapper (the Carrom ServerSwitchTurnFrom pattern) so the server's own disconnect
    /// handling can call it directly.</summary>
    [Server]
    public void ServerPlayerQuit(TPSeat leaver)
    {
        if (quitSeat != (int)TPSeat.None)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Quit from " + leaver + " ignored - "
                + (TPSeat)quitSeat + " already quit this match");
            return;
        }

        quitSeat = (int)leaver;
        if (MatchFlow.Enabled) MatchFlow.Log(FlowGame, $"{FlowSeatName(leaver)} left the match (pressed exit), pot {FlowPot()}");

        // The seat still here wins by walkover, and the round has to be closed from this path: the
        // showdown that would normally settle it is never going to happen now.
        ServerSettleHand(leaver == TPSeat.Creator ? TPSeat.Joiner : TPSeat.Creator, leaver + " quit the match");
        ServerEndHand(leaver + " quit the match");

        string leaverId = PlayerIdForSeat(leaver);
        TPLog.Change("TeenPattiNNetworkManager", "quit", leaver + " (playerId '" + leaverId
            + "') quit -> the other seat wins the match");
        RpcTeenPattiPlayerQuit(leaverId);
    }

    /// <summary>Opens the match result panel on the player who is still here. The quitter is named
    /// by backend playerId rather than netId: his object is being destroyed on every client as this
    /// goes out, so a netId would already resolve to nothing by the time it lands.</summary>
    [ClientRpc]
    public void RpcTeenPattiPlayerQuit(string leaverPlayerId)
    {
        string me = staticVariables.UserProfiledata != null && staticVariables.UserProfiledata.user != null
            ? staticVariables.UserProfiledata.user._id.ToString()
            : null;

        if (!string.IsNullOrEmpty(me) && me == leaverPlayerId)
        {
            TPLog.Flow("TeenPattiNNetworkManager", "I am the one who quit -> no result panel here");
            return;
        }

        TPLog.Flow("TeenPattiNNetworkManager", "Opponent (playerId '" + leaverPlayerId
            + "') quit -> showing my WIN result");
        DirectResult(true);
    }

    // ===================== MATCH RESULT PANEL =====================
    // These four never existed in the server build, which is why the two copies of this file had
    // diverged. RpcTeenPattiPlayerQuit calls DirectResult, and a ClientRpc has to compile
    // identically in both repos, so the server build needs them too - inert there, because of the
    // NetworkClient.active guard below.


    // ===================== MATCH FLOW LOGS (server log only) =====================
    // Wording helpers for MatchFlow. Read-only: nothing here changes a hand, a seat or a SyncVar.
    // Callers gate on MatchFlow.Enabled, so these only ever run on the dedicated server.

    public const string FlowGame = "Teen Patti";

    /// <summary>"handId:seat" keys already logged as "sees cards", so a repeated seen Cmd logs once.</summary>
    readonly HashSet<string> flowSeenKeys = new HashSet<string>();

    string FlowSeatName(TPSeat seat)
    {
        string id = PlayerIdForSeat(seat);
        return string.IsNullOrEmpty(id) ? seat.ToString() : MatchFlow.Who(id);
    }

    /// <summary>Display name of the player owning a connection, or the fallback (the object's name).</summary>
    public string FlowName(NetworkConnectionToClient conn, string fallback)
    {
        TPSeat seat = conn != null ? SeatForConnection(conn) : TPSeat.None;
        if (seat != TPSeat.None) return FlowSeatName(seat);
        return string.IsNullOrEmpty(fallback) ? "?" : fallback;
    }

    /// <summary>The pot as the room property holds it right now.</summary>
    public string FlowPot()
    {
        return roomCustomPropertiesTeenPatti != null
            ? roomCustomPropertiesTeenPatti.GetTableCollectedCash(LocalSettings.TableCashKey).ToString()
            : "?";
    }

    /// <summary>What a pot update added: the new pot minus the room's pot before it (the client
    /// writes the room pot right after its pot Cmd, so at Cmd time it still holds the old value).</summary>
    public string FlowPotDelta(string newPot)
    {
        System.Numerics.BigInteger next;
        if (roomCustomPropertiesTeenPatti == null || !System.Numerics.BigInteger.TryParse(newPot, out next)) return "?";
        return (next - roomCustomPropertiesTeenPatti.GetTableCollectedCash(LocalSettings.TableCashKey)).ToString();
    }

    /// <summary>True when the owner's seat is already out of this hand (or the hand is over), so a
    /// repeated Packed/OutOfTable state is not logged twice.</summary>
    public bool FlowIsOut(NetworkConnectionToClient conn)
    {
        TPSeat seat = conn != null ? SeatForConnection(conn) : TPSeat.None;
        return handEnded || (seat != TPSeat.None && serverPacked.Contains(seat));
    }

    /// <summary>True the first time the owner's seat reports seeing its cards in this hand.</summary>
    public bool FlowFirstSeen(NetworkConnectionToClient conn)
    {
        TPSeat seat = conn != null ? SeatForConnection(conn) : TPSeat.None;
        return flowSeenKeys.Add(handId + ":" + seat);
    }

    /// <summary>"pair: K♠ K♥ 7♦" for a seat's dealt hand. Calls ServerEvaluateSeat, so it must
    /// run Creator then Joiner (the order ServerDecideWinner uses) to leave the rank statics as they were.</summary>
    string FlowHand(TPSeat seat)
    {
        int rank, score;
        int[] values;
        int[] hand;
        if (!ServerEvaluateSeat(seat, out rank, out score, out values) || !serverHands.TryGetValue(seat, out hand))
            return "hand unknown";

        CardProperty[] deck = TeenPattiGame.GameManager.Instance.AllCards.Card;
        string rankName = rank == 7 ? "trail" : rank == 6 ? "pure sequence" : rank == 5 ? "sequence"
            : rank == 4 ? "colour" : rank == 3 ? "pair" : "high card";
        return rankName + ": " + FlowCard(deck[hand[0]]) + " " + FlowCard(deck[hand[1]]) + " " + FlowCard(deck[hand[2]]);
    }

    static string FlowCard(CardProperty card)
    {
        string value;
        switch (card.Card)
        {
            case CardState.CARDVALUE.ACE: value = "A"; break;
            case CardState.CARDVALUE.KING: value = "K"; break;
            case CardState.CARDVALUE.QUEEN: value = "Q"; break;
            case CardState.CARDVALUE.JACK: value = "J"; break;
            default: value = ((int)card.Card).ToString(); break;
        }
        string suit = card.Suit == CardState.SUIT.SPADE ? "♠" : card.Suit == CardState.SUIT.HEART ? "♥"
            : card.Suit == CardState.SUIT.DIAMOND ? "♦" : "♣";
        return value + suit;
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

    // Fired on the still-connected player when the opponent DROPS and the 30-second waiting-panel
    // countdown expires (WaitingPanel -> MirrorNetwork.OnWinCall). A deliberate quit does not wait
    // for that countdown - it comes straight through RpcTeenPattiPlayerQuit.
    private void AnnounceWinner(string reason)
    {
        TPLog.Flow("TeenPattiNNetworkManager", "AnnounceWinner - opponent disconnect timer expired, reason='" + reason + "' -> I win");
        if (MatchFlow.Enabled) MatchFlow.Log(FlowGame, $"reconnect timer ran out for the disconnected player ({reason}), pot {FlowPot()}");
        DirectResult(true);
    }

    public void DirectResult(bool result)
    {
        // ClientRpc bodies never run on a dedicated server, but this is also reachable from the
        // MirrorNetwork static events, which DO fire there. Everything below loads a UI prefab and
        // reads the logged-in user's id - neither exists on a headless build.
        if (!NetworkClient.active)
        {
            TPLog.Flow("TeenPattiNNetworkManager", "DirectResult(" + result + ") ignored - no local player on this build");
            return;
        }

        TPLog.Flow("TeenPattiNNetworkManager", "DirectResult(" + result + ") - result screen requested (GameSpawnedFinished=" + ResultManager.GameSpawnedFinished + ")");

        if (ResultManager.GameSpawnedFinished)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Result skipped - a result screen was already spawned for this match");
            return;
        }

        GameObject resultPrefab = Resources.Load<GameObject>("WinLoseGameManager");
        if (resultPrefab == null)
        {
            TPLog.Warn("TeenPattiNNetworkManager", "Result skipped - WinLoseGameManager prefab not found in Resources");
            return;
        }

        ResultManager.GameSpawnedFinished = true;
        TPLog.Flow("TeenPattiNNetworkManager", "Showing " + (result ? "WIN" : "LOSE") + " result panel");
        GameObject spawned = Instantiate(resultPrefab, Vector3.zero, Quaternion.identity);
        spawned.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(result, staticVariables.UserProfiledata.user._id.ToString());
    }
}
