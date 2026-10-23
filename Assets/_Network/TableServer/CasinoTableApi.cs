using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GamesPanda.Tables;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// The match server's calls for a Teen Patti 5 / Poker 6 table ("Teen Patti and Poker Tables", backend, 22 Oct 2026).
/// All go through MatchServerApi.Send, so they carry x-match-key and x-match-transaction and no Authorization.
///  - open a hand:   POST  /casinos/create-new-round/{tx}  { hand_no, players, boot }            → _id (round id)
///  - settle a hand: POST  /casinos/handle-rounds          { round_id, …, players[], commission } → stacks
///  - stand a player up: PATCH /casinos/tables/{tx}/leave  { user_id, stack, reason, result_id }
///  - close:         PATCH /casinos/tables/{tx}/close      { result_id, reason, stacks[] }
/// Network failures are retried with the same body (and result_id); a refusal is reported back at once.
/// A settled hand never stops the server; only close does.
/// </summary>
public static class CasinoTableApi
{
    public struct Answer
    {
        public bool Ok;
        public bool Answered;
        public string Message;
        public JObject Json;
        public override string ToString() => Ok ? "ok" + (string.IsNullOrEmpty(Message) ? "" : " (" + Message + ")") : (Answered ? "refused: " : "no answer: ") + Message;
    }

    static string Tx => MatchServerApi.TransactionId;

    static IEnumerator Call(string method, string path, object body, string what, Action<Answer> done, int tries = 4)
    {
        for (int i = 1; i <= tries; i++)
        {
            MatchServerApi.Reply r = default;
            yield return MatchServerApi.Send(method, path, body, x => r = x);
            var a = new Answer { Ok = r.Ok, Answered = r.Answered, Json = r.json, Message = r.message ?? r.error ?? ("HTTP " + r.code) };
            if (r.Ok || (r.Answered && r.code < 500))
            {
                if (!r.Ok) Debug.LogWarning($"[Table] {what} refused: HTTP {r.code} {r.body}");
                done?.Invoke(a);
                yield break;
            }
            Debug.LogWarning($"[Table] {what} try {i} failed ({a.Message}); retrying");
            if (i < tries) yield return new WaitForSecondsRealtime(2f * i);
            else done?.Invoke(a);
        }
    }

    /// <summary>Open hand <paramref name="handNo"/>. done(round id or null, answer). Asking again for the open hand's
    /// number returns the same id, so a retry is safe.</summary>
    public static IEnumerator OpenHand(int handNo, IList<string> players, long boot, Action<string, Answer> done)
    {
        var body = new Dictionary<string, object> { ["hand_no"] = handNo, ["players"] = players.ToArray() };
        if (boot > 0) body["boot"] = boot;
        yield return Call("POST", "/casinos/create-new-round/" + Tx, body, $"open hand {handNo}", a =>
        {
            string id = a.Json?["_id"]?.ToString() ?? a.Json?["data"]?["_id"]?.ToString();
            if (a.Ok && string.IsNullOrEmpty(id)) { a.Ok = false; a.Message = "no round id in the answer"; }
            done?.Invoke(a.Ok ? id : null, a);
        });
    }

    /// <summary>Result id of a hand, the same on every retry: "{tx}:hand:{n}:1".</summary>
    public static string HandResultId(int handNo) => $"{Tx}:hand:{handNo}:1";

    /// <summary>
    /// Settle one hand. done(stacks the backend now holds (user id → stack), expected stacks on a "stack mismatch",
    /// answer). On a mismatch nothing was saved and the table must close.
    /// </summary>
    public static IEnumerator SettleHand(string roundId, int handNo, string reason, HandLedger.Result result,
                                         IList<Card> board, IDictionary<string, Card[]> shown,
                                         Action<Dictionary<string, long>, Dictionary<string, long>, Answer> done)
    {
        var body = new Dictionary<string, object>
        {
            ["round_id"] = long.TryParse(roundId, out long rid) ? (object)rid : roundId,
            ["transaction_id"] = Tx,
            ["hand_no"] = handNo,
            ["result_id"] = HandResultId(handNo),
            ["reason"] = reason,
            ["players"] = result.Players.Select(p => new { p.user_id, p.seat, p.contributed, p.won, p.stack_after }).ToArray(),
            ["commission"] = result.Commission,
        };
        if (board != null && board.Count > 0) body["board"] = board.Select(c => c.ToString()).ToArray();
        if (shown != null && shown.Count > 0) body["shown"] = shown.ToDictionary(x => x.Key, x => x.Value.Select(c => c.ToString()).ToArray());

        yield return Call("POST", "/casinos/handle-rounds", body, $"settle hand {handNo}", a =>
        {
            done?.Invoke(a.Ok ? Stacks(a.Json?["stacks"] ?? a.Json?["data"]?["stacks"], "stack") : null,
                         !a.Ok ? Stacks(a.Json?["data"]?["expected"] ?? a.Json?["expected"], "stack") : null,
                         a);
        });
    }

    /// <summary>Stand a player up and pay their stack back. reason: left | away | busted.</summary>
    public static IEnumerator Leave(string userId, long stack, string reason, int attempt, Action<Answer> done) =>
        Call("PATCH", $"/casinos/tables/{Tx}/leave",
             new { user_id = userId, stack, reason, result_id = $"{Tx}:leave:{userId}:{attempt}" }, $"leave {userId} ({reason}, {stack})", done);

    /// <summary>Close the table: everyone still seated is paid; the backend then stops this server. reason: time | empty | error.</summary>
    public static IEnumerator Close(string reason, IDictionary<string, long> stacks, Action<Answer> done) =>
        Call("PATCH", $"/casinos/tables/{Tx}/close",
             new { result_id = $"{Tx}:close:1", reason, stacks = stacks.Select(x => new { user_id = x.Key, amount = x.Value }).ToArray() },
             $"close ({reason})", done);

    /// <summary>A table player's join ticket: verify answers with the user, their seat and their stack.</summary>
    public static IEnumerator VerifyTicket(string ticket, Action<string, int, long, Answer> done)
    {
        if (string.IsNullOrEmpty(ticket)) { done?.Invoke(null, -1, 0, new Answer { Answered = true, Message = "no ticket" }); yield break; }
        yield return Call("POST", "/match-server/join-ticket/verify",
                          new { transaction_id = Tx, ticket, request_id = MatchServerApi.RequestId }, "join ticket", a =>
        {
            JToken d = a.Json?["data"] is JObject o ? o : (JToken)a.Json;
            string user = d?["user_id"]?.ToString();
            int seat = int.TryParse(d?["seat"]?.ToString(), out int s) ? s : -1;
            long stack = long.TryParse(d?["stack"]?.ToString(), out long st) ? st : 0;
            done?.Invoke(a.Ok ? user : null, seat, stack, a);
        }, tries: 2);
    }

    /// <summary>[{user_id, stack}] or {"10363": 2503} → user id → amount.</summary>
    static Dictionary<string, long> Stacks(JToken list, string field)
    {
        if (list is JObject map)
            return map.Properties().Where(x => long.TryParse(x.Value.ToString(), out _))
                      .ToDictionary(x => x.Name, x => long.Parse(x.Value.ToString()));
        if (!(list is JArray arr)) return null;
        var d = new Dictionary<string, long>();
        foreach (var row in arr)
        {
            string id = row["user_id"]?.ToString();
            if (id != null && long.TryParse(row[field]?.ToString() ?? row["amount"]?.ToString(), out long v)) d[id] = v;
        }
        return d;
    }
}
