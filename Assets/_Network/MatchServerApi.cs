using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// The Edgegap match server's side of the Player API contract ("Edgegap server - changes needed", 12 Oct 2026):
///  - the match values come from the deployment environment (TransactionId, MatchKey, MatchAttempt, ARBITRIUM_REQUEST_ID);
///  - every call to the API carries x-match-key (plus x-match-transaction on /match-server/...) and no Authorization;
///  - self-check when the server starts (keep:false = this is not the live server for the match, shut down);
///  - join tickets verified before a player is let in;
///  - results reported with PATCH, with a result_id that stays the same across retries so it is not saved twice.
/// The MatchKey value is logged only while LogMatchKeyForTesting is on.
/// </summary>
public static class MatchServerApi
{
    public const string MatchKeyHeader = "x-match-key";
    public const string MatchTransactionHeader = "x-match-transaction";

    /// <summary>
    /// TESTING ONLY: print the full MatchKey in the start-up log line. The log goes to S3 and the stats panel, and
    /// anyone holding the key can report this match's result - set false before a production build.
    /// </summary>
    public const bool LogMatchKeyForTesting = true;

    /// <summary>Seconds between deciding a result and reporting it (the API stops the server right after).</summary>
    public const float ResultDelaySeconds = 5f;

    static bool read;
    static string transactionId, matchKey, attempt, requestId;

    static void Read()
    {
        if (read) return;
        read = true;
        transactionId = Env("TransactionId");
        matchKey = Env("MatchKey");
        attempt = Env("MatchAttempt");
        requestId = Env("ARBITRIUM_REQUEST_ID");
    }

    static string Env(string name)
    {
        try { return Environment.GetEnvironmentVariable(name)?.Trim(); }
        catch (Exception) { return null; }
    }

    public static string TransactionId { get { Read(); return transactionId; } }
    public static string Attempt { get { Read(); return attempt; } }
    public static string RequestId { get { Read(); return requestId; } }
    public static bool HasMatchKey { get { Read(); return !string.IsNullOrEmpty(matchKey); } }

    /// <summary>True inside an Edgegap deployment the backend started (it always sets MatchKey there).</summary>
    public static bool IsMatchServer => HasMatchKey;

    /// <summary>
    /// Put the match headers on a request to the Player API. Returns true when it did: the caller must then
    /// leave out the Authorization header (the contract says the server sends none).
    /// </summary>
    public static bool ApplyHeaders(UnityWebRequest request, string path)
    {
        if (!IsMatchServer) return false;
        request.SetRequestHeader(MatchKeyHeader, matchKey);
        if (!string.IsNullOrEmpty(transactionId) && path != null && path.StartsWith("/match-server/", StringComparison.Ordinal))
            request.SetRequestHeader(MatchTransactionHeader, transactionId);
        return true;
    }

    public struct Reply
    {
        public long code;
        public string body;
        public string error;       // transport error (no answer from the API), null when the API answered
        public bool status;        // the body's "status"
        public string message;     // the body's "message"
        public JObject json;
        public bool Answered => error == null;
        public bool Ok => Answered && code >= 200 && code < 300 && status;
    }

    /// <summary>One call to the Player API with the match headers and a JSON body (or none).</summary>
    public static IEnumerator Send(string method, string path, object body, Action<Reply> done)
    {
        Read();
        using (var req = new UnityWebRequest(ServerConnection.Main_URL() + path, method))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            if (body != null)
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("accept", "*/*");
            ApplyHeaders(req, path);
            req.timeout = 20;
            yield return req.SendWebRequest();

            var r = new Reply { code = req.responseCode, body = req.downloadHandler?.text ?? "" };
            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.DataProcessingError || r.code == 0)
                r.error = string.IsNullOrEmpty(req.error) ? "no answer" : req.error;
            try
            {
                r.json = string.IsNullOrEmpty(r.body) ? null : JObject.Parse(r.body);
                r.status = r.json?["status"]?.Type == JTokenType.Boolean && (bool)r.json["status"];
                r.message = r.json?["message"]?.ToString();
            }
            catch (Exception) { /* not JSON: an HTML error page or empty */ }
            done?.Invoke(r);
        }
    }

    static string Short(Reply r) => r.error != null ? r.error : $"HTTP {r.code}{(string.IsNullOrEmpty(r.message) ? "" : " " + r.message)}";

    // ── self-check ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ask the API whether this server is the live one for the match. keep:false, "not the current owner" or
    /// "match server stopped" mean an old/replaced server: it quits so it cannot take players or report a result.
    /// A missing answer (network) keeps the server running: the API also stops it after 25 minutes.
    /// </summary>
    public static IEnumerator SelfCheck()
    {
        if (!IsMatchServer) { Debug.Log("[MatchServer] No MatchKey in the environment - not an Edgegap match server, self-check skipped."); yield break; }
        var body = new { transaction_id = TransactionId, request_id = RequestId, attempt = Attempt };
        for (int i = 1; i <= 3; i++)
        {
            Reply r = default;
            yield return Send("POST", "/match-server/self-check", body, x => r = x);
            if (!r.Answered)
            {
                Debug.LogWarning($"[MatchServer] Self-check try {i}: {r.error}");
                yield return new WaitForSecondsRealtime(3f * i);
                continue;
            }
            bool keep = r.json?["keep"]?.Type == JTokenType.Boolean ? (bool)r.json["keep"] : r.Ok;
            if (r.Ok && keep) { Debug.Log("[MatchServer] Self-check: keep=true - this is the live server for the match."); yield break; }
            if (IsReplaced(r) || (r.Ok && !keep))
            {
                Debug.LogError($"[MatchServer] Self-check says this is not the live server ({Short(r)}, keep={keep}) - shutting down.");
                Application.Quit();
                yield break;
            }
            Debug.LogError($"[MatchServer] Self-check refused: {Short(r)} - check the match key / x-match-transaction.");
            yield break;
        }
    }

    static bool IsReplaced(Reply r)
    {
        string m = (r.message ?? r.body ?? "").ToLowerInvariant();
        return m.Contains("not the current owner") || m.Contains("match server stopped");
    }

    // ── join ticket ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verify the ticket a connecting player sent. done(ok, answered, userId, message): ok = let that user in;
    /// answered = false when the API could not be reached. A ticket works once.
    /// </summary>
    public static IEnumerator VerifyJoinTicket(string ticket, Action<bool, bool, string, string> done)
    {
        if (string.IsNullOrEmpty(ticket)) { done?.Invoke(false, true, null, "no ticket"); yield break; }
        Reply r = default;
        yield return Send("POST", "/match-server/join-ticket/verify", new { transaction_id = TransactionId, ticket, request_id = RequestId }, x => r = x);
        string userId = r.json?["user_id"]?.ToString() ?? r.json?["data"]?["user_id"]?.ToString();
        Debug.Log($"[MatchServer] Join ticket for user {userId ?? "?"}: {(r.Ok ? "verified" : "refused - " + Short(r))}");
        done?.Invoke(r.Ok, r.Answered && r.code < 500, userId, r.Ok ? null : (r.message ?? Short(r)));
    }

    // ── results ───────────────────────────────────────────────────────────────────────────────

    /// <summary>"tx:win:12345:attempt" - 8 to 80 characters of letters, digits and : _ . - , the same on every retry.</summary>
    public static string ResultId(string kind)
    {
        Read();
        var sb = new StringBuilder();
        foreach (char c in $"{transactionId}:{kind}:{attempt}")
            sb.Append(char.IsLetterOrDigit(c) && c < 128 || c == ':' || c == '_' || c == '.' || c == '-' ? c : '_');
        string id = sb.ToString();
        if (id.Length > 80) id = id.Substring(0, 80);
        while (id.Length < 8) id += "_";
        return id;
    }

    /// <summary>PATCH /session/session-winner/{tx}/user/{winnerUserId}. done(saved, what the API said).</summary>
    public static IEnumerator ReportWinner(string transaction, string winnerUserId, Action<bool, string> done)
    {
        string tx = string.IsNullOrEmpty(TransactionId) ? transaction : TransactionId;
        yield return Report($"/session/session-winner/{tx}/user/{winnerUserId}", ResultId("win:" + winnerUserId), $"winner {winnerUserId} for {tx}", done);
    }

    /// <summary>PATCH /session/update-status/to-draw/{tx}. done(saved, what the API said).</summary>
    public static IEnumerator ReportDraw(string transaction, Action<bool, string> done)
    {
        string tx = string.IsNullOrEmpty(TransactionId) ? transaction : TransactionId;
        yield return Report($"/session/update-status/to-draw/{tx}", ResultId("draw"), $"draw for {tx}", done);
    }

    static IEnumerator Report(string path, string resultId, string what, Action<bool, string> done)
    {
        if (!IsMatchServer)
        {
            Debug.LogError($"[Settlement] Not sent ({what}): no MatchKey in this server's environment - only an Edgegap match server can report a result.");
            done?.Invoke(false, "no match key");
            yield break;
        }
        // The API stops this server about a second after it saves the result. Several games still need the server
        // after deciding the winner: Ludo and Carrom (timeout) do a client->server->client round trip before the
        // result screen, 8 Ball reveals its panel 3 s later, Highway Racer waits 3 s. Give them that time first.
        Debug.Log($"[Settlement] Reporting {what} in {ResultDelaySeconds:0} s (clients get their result screens first).");
        yield return new WaitForSecondsRealtime(ResultDelaySeconds);

        // Network failures are retried with the SAME result_id, so a result that did reach the API is not saved twice.
        for (int i = 1; i <= 4; i++)
        {
            Reply r = default;
            yield return Send("PATCH", path, new { result_id = resultId }, x => r = x);
            string said = Short(r);
            if (r.Ok) { Debug.Log($"[Settlement] Result saved: {what} ({said})."); done?.Invoke(true, said); yield break; }
            if (r.Answered && (r.message ?? "").ToLowerInvariant().Contains("result already saved"))
            {
                Debug.Log($"[Settlement] Result was already saved: {what}.");
                done?.Invoke(true, said);
                yield break;
            }
            if (r.Answered && r.code < 500)
            {
                Debug.LogError($"[Settlement] Result REFUSED: {what} - {said} {r.body}");
                done?.Invoke(false, said);
                yield break;
            }
            Debug.LogWarning($"[Settlement] Result try {i} for {what} failed ({said}); retrying with the same result_id.");
            yield return new WaitForSecondsRealtime(2f * i);
        }
        Debug.LogError($"[Settlement] Result NOT saved after 4 tries: {what}.");
        done?.Invoke(false, "no answer");
    }

    // ── start-up ──────────────────────────────────────────────────────────────────────────────

    sealed class Runner : MonoBehaviour { }
    static Runner runner;

    /// <summary>Somewhere to run the coroutines from that survives scene changes.</summary>
    public static Coroutine Run(IEnumerator routine)
    {
        if (runner == null)
        {
            var go = new GameObject("MatchServerApi");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }
        return runner.StartCoroutine(routine);
    }

    /// <summary>One line with the match values read from the environment, for testing.</summary>
    public static void LogEnvironment()
    {
        Read();
        string key = string.IsNullOrEmpty(matchKey) ? "(not set)"
            : LogMatchKeyForTesting ? matchKey
            : $"set ({matchKey.Length} chars)";
        Debug.Log($"[MatchServer] Environment: TransactionId={Show(transactionId)} MatchKey={key} MatchAttempt={Show(attempt)} ARBITRIUM_REQUEST_ID={Show(requestId)}");
    }

    static string Show(string v) => string.IsNullOrEmpty(v) ? "(not set)" : v;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartUp()
    {
        LogEnvironment();
        if (IsMatchServer) Run(SelfCheck());
    }
}
