using System;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// Server-authoritative FULL match-state snapshot for clean reconnect restore (snooker-style: one
// comprehensive state push instead of ~25 fragmented SyncVars + scattered restore code that races).
//
// Serialized to ONE [SyncVar] string (matchSnapshotJson) via JsonUtility. JsonUtility cannot serialize
// top-level arrays/dictionaries, so EVERYTHING is wrapped in plain [Serializable] classes with arrays
// as fields. The BATTING client is the sole writer (CmdPushMatchSnapshot); Mirror replays the SyncVar
// atomically on (re)spawn, so reconnect is "deserialize + ApplyMatchStateSnapshot once" — no per-field
// hook ordering, no missing state, no team-swap/score-0-0 race.
//
// Owns PERSISTENT match state only. Transient/visual state (in-flight ball position, animations, camera)
// is NOT here — it is re-bowled/reset cleanly by CancelInFlightDeliveryForReconnect / ArmBattingPreview.
//
// Phase 0: built + pushed ALONGSIDE the existing synced* SyncVars (no behaviour change, log-diff only).
// See the 2026-06-22 design (workflow wf_da8075b2-e4a) + memory project_cricket_matchstate_snapshot.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
[Serializable]
public class MatchStateSnapshot
{
    public int version = 1;        // schema version — gate future changes
    public bool valid = false;     // false until the batting client's first real push (apply no-ops on invalid)

    // ── innings / team roles (the team-swap + 0/0 cluster) ──
    public int currentInnings;
    public int battingTeamIndex = -1;
    public int bowlingTeamIndex = -1;
    public bool inningsCompleted;
    public bool mpInningsCompleted;

    // ── batting indices + within-over position ──
    public int strikerIndex = -1;
    public int nonStrikerIndex = -1;
    public int newBatsmanEntryIndex = -1;
    public int currentBallNumber;   // GameData.currentBallNumber — legal-ball index this over (0..5)
    // ScoreBoardBallList.ballCount. NOT a 0..5 index: AddBall keeps it == ballList.Count, i.e. the number of
    // DELIVERIES bowled this over INCLUDING no-balls/wides, so an over with extras legitimately exceeds 5.
    // Never range-gate the over restore on this (that reset currentBallNumber to 0 and broke NewOver()).
    public int ballCount;

    // ── ball-by-ball chip strip (ScoreBoardBallList) ──
    // The bottom-strip chips (this over's deliveries: '1'/'4'/'W', INCLUDING no-ball/wide entries in order).
    // Lives only in the ScoreBoardBallList scene singleton (wiped on reconnect scene-reload) + local AutoSave;
    // ballUpdate[6] above is legal-balls-only (indexed) so it CANNOT reconstruct the exact strip. Carried here
    // so a reconnecting client can repaint the strip. Empty on an old-server snapshot → apply skips (no-op).
    public string ballListInfo = "";   // ScoreBoardBallList.ballList joined by '|'
    public string ballExtras = "";     // ScoreBoardBallList.extras joined by '|' (flat [index,type] pairs)

    // ── bowling ──
    // ── per-delivery rule state that a scene reload would otherwise DESTROY ──
    // Both live as plain instance fields (GroundController.isFreeHitActive/freeHit, GameData.isWicketBall),
    // so a reconnect reloads Ground and silently resets them:
    //  • freeHitActive — the free hit a no-ball awarded just disappears, and the two clients then disagree
    //    about whether the very next delivery can take a wicket.
    //  • wicketBallPending — GameData gates the over-end new-batsman walk-in on isWicketBall
    //    ("Over ended on a wicket — running new-batsman walk-in before NewOver"). Lost, that branch is
    //    skipped, NewOver runs with no incoming batsman, and the next over has no striker — the reported
    //    "over ended on a catch out, reconnect, one side shows the summary and the other is stuck".
    public bool freeHitActive;
    public bool wicketBallPending;

    public int currentBowlerIndex = -1;
    public string bowlerSide = "right";
    public int fielderChangeIndex;

    // ── per-team blocks (both teams, by absolute team index) ──
    public TeamSnapshot[] teams;
}

[Serializable]
public class TeamSnapshot
{
    public int teamIndex = -1;
    // aggregate score (TeamInfo.currentMatch*)
    public int scores;
    public int wickets;
    public int balls;
    public int extras;
    public int lbs;
    public int byes;
    public int noball;
    public int wideBall;
    public int drsLeft = 2;
    public string[] ballUpdate;     // 6 strings: this over's ball-by-ball ('W'/'4'/'6'/runs)
    public BatsmanSnapshot[] batsmen;   // one per PlayerList entry (fixes "other 9 batsmen show 0")
    public BowlerSnapshot[] bowlers;    // one per PlayerList entry (fixes "active bowler 0/0")
}

[Serializable]
public class BatsmanSnapshot
{
    public int idx = -1;            // PlayerList index — set so only ACTIVE batsmen are sent (keeps the blob small)
    public int runs;
    public int balls;
    public int fours;
    public int sixes;
    public int fow;                 // fall-of-wicket order
    public string status = "";      // "not out" / dismissal text
    public int partnershipRuns;
    public int partnershipBalls;
}

[Serializable]
public class BowlerSnapshot
{
    public int idx = -1;            // PlayerList index — set so only ACTIVE bowlers are sent (keeps the blob small)
    public int runsGiven;
    public int ballsBowled;
    public int maiden;
    public int wicket;
    public int wicketsInBallCount;
    public int maxBallInMatch;
}
