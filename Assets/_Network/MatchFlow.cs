using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Readable game-event logs for every multiplayer game on the match server: one line per event, prefixed with the
/// game ("[Ludo Flow] Ali rolled a 6"), so a server log reads as the story of the match. The lines of the current
/// match are kept and, when the result is decided, sent as one JSON to the bug reporter's stats panel
/// (https://pandabugsreporting.com/stats → a day → View log).
/// Written ONLY on the dedicated (Edgegap) server — on phones and in offline/AI games every call is a no-op.
/// Logging only — nothing here changes a game.
/// </summary>
public static class MatchFlow
{
    /// <summary>True only in the headless match server.</summary>
    public static bool Enabled => NetworkServer.active && !NetworkClient.active;

    [Serializable] class FlowEvent { public float t; public string time; public string msg; }

    [Serializable]
    class FlowLog
    {
        public string transaction_id, game, winner_id, winner_name, reason, scores, started_at, ended_at;
        public int game_id;
        public List<FlowEvent> events = new List<FlowEvent>();
    }

    const int MaxEvents = 3000;
    static FlowLog _match;
    static float _matchStart;
    static string _game = "Match";

    /// <summary>A new match of <paramref name="game"/> starts (drops any unsent lines of an earlier one).</summary>
    public static void Begin(string game, string message = null)
    {
        if (!Enabled) return;
        _game = string.IsNullOrEmpty(game) ? "Match" : game;
        _match = new FlowLog { game = _game, started_at = DateTime.UtcNow.ToString("o") };
        _matchStart = Time.realtimeSinceStartup;
        if (message != null) Log(message);
    }

    /// <summary>One event of the current match. Starts the match record if Begin was not called.</summary>
    public static void Log(string game, string message)
    {
        if (!Enabled) return;
        if (_match == null || _game != game) Begin(game);
        Log(message);
    }

    /// <summary>One event of the current match (game set by Begin).</summary>
    public static void Log(string message)
    {
        if (!Enabled) return;
        if (_match == null) Begin(_game);
        Debug.Log("[" + _game + " Flow] " + message);
        if (_match.events.Count < MaxEvents)
            _match.events.Add(new FlowEvent { t = Time.realtimeSinceStartup - _matchStart, time = DateTime.UtcNow.ToString("o"), msg = message });
    }

    /// <summary>
    /// The result is decided on the server: log it and send the match's lines as one JSON. Call it right before the
    /// result goes to the backend. Only the first call of a match sends; later result paths of the same match are
    /// ignored. <paramref name="scores"/> defaults to the creator/joiner scores.
    /// </summary>
    public static void SendResult(string winnerId, string reason, string scores = null)
    {
        if (!Enabled || _match == null) return;
        scores = scores ?? Scores();
        Log($"result sent — {Who(winnerId)} wins ({reason})" + (string.IsNullOrEmpty(scores) ? "" : ", " + scores));
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
            Debug.LogWarning("[" + _game + " Flow] no transaction id — match log not sent");
        _match = null;
    }

    /// <summary>Display name for a player id (creator / joiner name from NetworkGameManager), "AI" for bots.</summary>
    public static string Who(string id)
    {
        if (string.IsNullOrEmpty(id)) return "?";
        if (id == "ai" || id == "AI") return "AI";
        var ngm = NetworkGameManager.Instance;
        if (ngm != null)
        {
            if (ngm.creatorData != null && ngm.creatorData.playerId == id) return Named(ngm.creatorData.playerName, id);
            if (ngm.joinerData != null && ngm.joinerData.playerId == id) return Named(ngm.joinerData.playerName, id);
        }
        return id;
    }

    /// <summary>"creator 3 – 1 joiner" from NetworkGameManager's synced scores, or "" when unavailable.</summary>
    public static string Scores()
    {
        var ngm = NetworkGameManager.Instance;
        if (ngm == null || ngm.creatorData == null || ngm.joinerData == null) return "";
        return $"{Named(ngm.creatorData.playerName, "creator")} {ngm.creatorData.Scores} – {ngm.joinerData.Scores} {Named(ngm.joinerData.playerName, "joiner")}";
    }

    static string Named(string name, string fallback) => string.IsNullOrEmpty(name) ? fallback : name;
}
