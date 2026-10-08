using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// One readable line per Snooker game event, all prefixed "[Snooker Flow]" so a match server's log can be filtered
/// down to the story of a frame: game started → toss → break → pots / fouls → score → turn changes → result.
/// Written ONLY on the dedicated (Edgegap) server — on phones and in AI games every call is a no-op.
/// The server also keeps the lines of the current match and, when the result is decided, sends them as one JSON
/// to the bug reporter's stats panel (<see cref="SendResult"/>).
/// Logging only — nothing here changes the game.
/// </summary>
public static class SnookerFlow
{
    const string Prefix = "[Snooker Flow] ";

    /// <summary>Set by mainScript so names and the current target can be read.</summary>
    public static mainScript Main;

    /// <summary>True only in the headless match server build.</summary>
    public static bool Enabled => NetworkServer.active && !NetworkClient.active;

    [Serializable] class FlowEvent { public float t; public string time; public string msg; }

    [Serializable]
    class FlowLog
    {
        public string transaction_id, game = "Snooker", winner_id, winner_name, reason, scores, started_at, ended_at;
        public int game_id;
        public List<FlowEvent> events = new List<FlowEvent>();
    }

    const int MaxEvents = 3000;
    static FlowLog _match;
    static float _matchStart;

    public static void Log(string message)
    {
        if (!Enabled) return;
        Debug.Log(Prefix + message);
        if (_match == null) BeginMatch();
        if (_match.events.Count < MaxEvents)
            _match.events.Add(new FlowEvent { t = Time.realtimeSinceStartup - _matchStart, time = DateTime.UtcNow.ToString("o"), msg = message });
    }

    static void BeginMatch()
    {
        _match = new FlowLog { started_at = DateTime.UtcNow.ToString("o") };
        _matchStart = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// The result was decided on the server: log it, then send this match's lines as one JSON to the stats panel.
    /// Call it right before the result goes to the backend (scores are still set). Once per match.
    /// </summary>
    public static void SendResult(string winnerId, string reason)
    {
        if (!Enabled || _match == null) return;
        string scores = Scores();
        Log($"result sent — {Who(winnerId)} wins ({reason}), {scores}");
        var ngm = NetworkGameManager.Instance;
        _match.transaction_id = ngm != null ? ngm.transactionId : null;
        _match.game_id = ngm != null ? ngm.currentGameId : 0;
        _match.winner_id = winnerId;
        _match.winner_name = Who(winnerId);
        _match.reason = reason;
        _match.scores = scores;
        _match.ended_at = DateTime.UtcNow.ToString("o");
        if (!string.IsNullOrEmpty(_match.transaction_id))
            MatchStats.SendMatchLog(JsonUtility.ToJson(_match));
        else
            Debug.LogWarning(Prefix + "no transaction id — match log not sent");
        _match = null;
    }

    /// <summary>Start of a frame (also resets the shot de-duplication).</summary>
    public static void GameStarted(string message)
    {
        _lastShotLogged = -1;
        if (Enabled) BeginMatch();
        Log(message);
    }

    /// <summary>A shot, logged once: the server runs both ExecuteHit and RpcExecuteBallHit for the same shot.</summary>
    static int _lastShotLogged = -1;

    /// <summary>Display name for a turn/winner id: "ai", the local player, or a multiplayer creator/joiner.</summary>
    public static string Who(string id)
    {
        if (string.IsNullOrEmpty(id)) return "?";
        var m = Main;
        if (id == "ai") return m != null ? m.playerNames[1] : "CPU";
        string me = null;
        try { me = staticVariables.UserProfiledata?.user?._id.ToString(); } catch { }
        if (m != null && me == id) return m.playerNames[0];
        var ngm = NetworkGameManager.Instance;
        if (ngm != null)
        {
            if (ngm.creatorData.playerId == id) return Named(ngm.creatorData.playerName, id);
            if (ngm.joinerData.playerId == id) return Named(ngm.joinerData.playerName, id);
        }
        return m != null ? m.playerNames[1] : id;
    }

    static string Named(string name, string id) => string.IsNullOrEmpty(name) ? id : name;

    /// <summary>Snooker ball by point value (1 red … 7 black); 99 = any colour.</summary>
    public static string Ball(int points)
    {
        switch (points)
        {
            case 1: return "red";
            case 2: return "yellow";
            case 3: return "green";
            case 4: return "brown";
            case 5: return "blue";
            case 6: return "pink";
            case 7: return "black";
            case 99: return "any colour";
            case 0: return "nothing";
            default: return "ball " + points;
        }
    }

    public static string Target => Ball((int)SnokerGameManager.snookerTargetBall);

    /// <summary>A shot was played. The first shot of the frame is the break; power 0 is the timer's automatic shot.</summary>
    public static void Shot(int strikeNumber, float power)
    {
        if (!Enabled || strikeNumber == _lastShotLogged) return;
        _lastShotLogged = strikeNumber;
        string who = Who(SnokerGameManager.currentTurn);
        if (power <= 0f) Log($"{who}: turn timer ran out — automatic shot (target {Target})");
        else if (strikeNumber <= 1) Log($"{who} makes the break (power {power:0.00})");
        else Log($"{who} shoots, target {Target} (power {power:0.00}, shot #{strikeNumber})");
    }

    /// <summary>Current scores, for the score-updated line.</summary>
    public static string Scores()
    {
        var ngm = NetworkGameManager.Instance;
        if (SnokerNetwork.IsMultiplayer && ngm != null)
            return $"{Named(ngm.creatorData.playerName, "creator")} {ngm.creatorData.Scores} – {ngm.joinerData.Scores} {Named(ngm.joinerData.playerName, "joiner")}";
        var m = Main;
        if (m == null || m.snookerScoresVal == null || m.snookerScoresVal.Length < 2) return "?";
        return $"{m.playerNames[0]} {m.snookerScoresVal[0]} – {m.snookerScoresVal[1]} {m.playerNames[1]}";
    }
}
