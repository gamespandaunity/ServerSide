using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Tells the bug reporter that a multiplayer match was played, for its daily count panel
/// (https://pandabugsreporting.com/stats). Both players report; the challenge id makes the match count once.
/// The match server also sends the match's event log with the result (<see cref="SendMatchLog"/>).
/// Fire and forget: a failed report is never retried and never affects the game.
/// </summary>
public static class MatchStats
{
    /// <summary>The GamesPanda project's key on pandabugsreporting.com (write-only: reports, not reads).</summary>
    public const string BugReporterApiKey = "br_live_eab6fddee73c4b569783a73fbbc78c7f";
    const string Url = "https://pandabugsreporting.com/api/stats/match";

    const string LogUrl = "https://pandabugsreporting.com/api/stats/match-log";
    const string FlagUrl = "https://pandabugsreporting.com/api/stats/flag";

    static readonly HashSet<string> Sent = new HashSet<string>();

    /// <summary>Match server only: one flagged player (see MatchFlow.Flag) for the stats panel. Fire and forget.</summary>
    public static void SendFlag(string json) => Post(FlagUrl, json, "flag");

    /// <summary>
    /// Match server only: sends the match's event log (JSON with transaction_id, game_id, winner, events, flags …) to
    /// the stats panel, where it is shown under that day's matches. Fire and forget.
    /// </summary>
    public static void SendMatchLog(string json) => Post(LogUrl, json, "match log");

    static void Post(string url, string json, string what)
    {
        if (string.IsNullOrEmpty(json)) return;
        var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 20,
        };
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-Api-Key", BugReporterApiKey);
        req.SendWebRequest().completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[MatchStats] {what} upload failed: {req.responseCode} {req.error}");
            req.Dispose();
        };
    }

    /// <summary>Called when this device connects to a challenge's match server.</summary>
    public static void Report(string transactionId)
    {
#if UNITY_EDITOR
        return;   // editor test matches would inflate the count
#else
        if (string.IsNullOrEmpty(transactionId) || !Sent.Add(transactionId)) return;
        string json = "{\"transaction_id\":\"" + transactionId.Replace("\"", "") + "\",\"game_id\":" +
                      ApiAndRoomManager.currentGameId + ",\"build\":\"" + Application.version + "\"}";
        var req = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 15,
        };
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-Api-Key", BugReporterApiKey);
        req.SendWebRequest().completed += _ => req.Dispose();
#endif
    }
}
