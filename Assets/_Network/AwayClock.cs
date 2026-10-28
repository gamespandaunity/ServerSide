using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One 30 s clock for a 2-player match while a player is away, from the moment the server notices (5 s of silence,
/// SilentConnectionWatch, or the transport drop if that comes first) until the player is back or the match is settled.
///
/// QA 28 Oct, Snooker / Ludo / 8 Ball / Snake:
///  - the waiting player saw two counts: the silent-player panel counted UP (1..15), then the transport's 20 s drop
///    started a second count DOWN from 30. Now the phones get the seconds left of this one clock (RpcAwayClock);
///  - a phone coming back takes a few seconds to reload the game scene before the server adds its player again, and
///    the 30 s could run out meanwhile ("reconnect hoty hoty win/lose"). A new connection during the clock shows
///    "Opponent is reconnecting" at once and the result waits up to <see cref="ReconnectHoldSeconds"/> more for it;
///  - the disconnection limit was never applied: it counted in MirrorNetwork.OnPlayerReconnected, which Mirror calls
///    only for a second AddPlayer on the SAME connection, and a phone that lost the network always comes back on a new
///    one. Every time away (a silence of 5 s or a drop) now counts here, once per time away.
/// The match is settled by NetworkGameManager's abandon watch at the end of this clock, not by the waiting phone.
/// Casino tables and 3-4 seat matches have their own away handling and are left alone.
/// </summary>
public static class AwayClock
{
    public const float GraceSeconds = 30f;
    public const float ReconnectHoldSeconds = 15f;

    static string awayId;
    static float deadline;
    static bool reconnecting;
    static readonly Dictionary<string, int> timesAway = new Dictionary<string, int>();

    public static bool Running => awayId != null;
    public static string AwayPlayerId => awayId;
    public static bool IsReconnecting => Running && reconnecting;

    /// <summary>Seconds until the away player loses the match, or -1 when nobody is away.</summary>
    public static float SecondsLeft => Running ? Mathf.Max(0f, deadline - Time.realtimeSinceStartup) : -1f;

    /// <summary>The time away that loses the match: the 7th in Snooker, the 5th elsewhere (the old reconnect numbers).</summary>
    public static int Limit => SceneManager.GetActiveScene().name == "SnokkerMultiplayer" ? 7 : 5;

    static bool Applies()
    {
        var ngm = NetworkGameManager.Instance;
        return NetworkServer.active && ngm != null && !TableEnv.IsTable && !MatchSeats.Active
               && MirrorNetwork.hasJoinedGame && !ngm.ServerMatchAlreadySettled
               && NetworkGameManager.IsTwoPlayerMatchScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>A player went silent or dropped. Starts the clock, or does nothing while it already runs for them.</summary>
    public static void Away(string playerId, string why)
    {
        if (!Applies() || string.IsNullOrEmpty(playerId)) return;
        if (Running)
        {
            if (awayId != playerId) MatchFlow.Log($"{MatchFlow.Who(playerId)} is away too ({why}) while {MatchFlow.Who(awayId)}'s clock runs");
            return;
        }
        int n = timesAway.TryGetValue(playerId, out int before) ? before + 1 : 1;
        timesAway[playerId] = n;
        var ngm = NetworkGameManager.Instance;
        if (n >= Limit)
        {
            string winner = ngm.OtherPlayerId(playerId);
            MatchFlow.Log($"{MatchFlow.Who(playerId)} away ({why}) for the {n}th time - limit {Limit}, {MatchFlow.Who(winner)} wins");
            if (!string.IsNullOrEmpty(winner))
                ngm.SetDisconnectWinner(winner, null, NetworkGameManager.DISCONNECT_WIN_RECONNECT_LIMIT);
            return;
        }
        awayId = playerId;
        reconnecting = false;
        deadline = Time.realtimeSinceStartup + GraceSeconds;
        MatchFlow.Log($"{MatchFlow.Who(playerId)} away ({why}) - time away {n} of {Limit - 1} allowed, {GraceSeconds:0} s to come back");
        Broadcast();
    }

    /// <summary>A new connection arrived while a player is away: most likely them, loading the game scene again.</summary>
    public static void ConnectionArrived()
    {
        if (!Running || reconnecting) return;
        reconnecting = true;
        MatchFlow.Log($"{MatchFlow.Who(awayId)} is reconnecting ({SecondsLeft:0} s left)");
        Broadcast();
    }

    /// <summary>The connection that was coming back dropped again before its player was added.</summary>
    public static void ReconnectFailed()
    {
        if (!IsReconnecting) return;
        reconnecting = false;
        MatchFlow.Log($"{MatchFlow.Who(awayId)}'s reconnect dropped ({SecondsLeft:0} s left)");
        Broadcast();
    }

    /// <summary>Both players are here again (network back, or the player added again after a reconnect).</summary>
    public static void Back(string why)
    {
        if (!Running) return;
        MatchFlow.Log($"{MatchFlow.Who(awayId)} is back ({why}) after {GraceSeconds - SecondsLeft:0} s");
        awayId = null;
        reconnecting = false;
        if (NetworkGameManager.Instance != null) NetworkGameManager.Instance.RpcAwayClock(-1f, false);
    }

    /// <summary>The match was settled or a new one starts: stop the clock (and, for a new match, forget the counts).</summary>
    public static void Stop(bool newMatch)
    {
        awayId = null;
        reconnecting = false;
        if (newMatch) timesAway.Clear();
    }

    static void Broadcast()
    {
        if (NetworkGameManager.Instance != null) NetworkGameManager.Instance.RpcAwayClock(SecondsLeft, reconnecting);
    }
}
