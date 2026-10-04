using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConstantsData_M
{
    public static int CurrentBetSpawnAmount;

    // Perf fix: the Log/Show/LogInfo/etc. helpers below route through Debug.LogError, which on a
    // DEVICE captures a FULL STACK TRACE on EVERY call. These fire dozens of times per over (retry
    // floods like RpcBattingScoreCardContinue ×50, per-ball logs, "REBOWLL KO CALL", DOTween) → a
    // real mobile CPU/GC drain that worsens the ball jitter and the ~2s turn lag. Disable
    // stack-trace capture for Log/Warning/Error (the messages still print — just cheaply). Real
    // thrown exceptions log as LogType.Exception (separate) and KEEP their stack traces for crashes.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void DisableLogStackTraces()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
        // Exception & Assert stack traces are intentionally left ON for real crash debugging.
    }

    // MP verbose-logging gate: the per-RPC/per-ball debug logs (this Log() + the many
    // [CricketNetworkManager] Debug.Log calls, now routed through MpLog) fire dozens of times per
    // over. That floods the in-game debug console — which then throws its OWN NullReferenceException
    // while recycling collapsed entries (IngameDebugConsole.DebugLogRecycledListView) — and costs
    // CPU/GC on device. Default OFF for normal play/release. Set true (and rebuild) to re-enable
    // these logs when debugging a specific issue (reconnect, over-complete, etc.).
    public static bool MpVerboseLogs = true; // DEBUG PHASE: ON so MP logs are captured. Set false before release.

    // ── No-share log export jugaad ──────────────────────────────────────────────────────────────
    // NativeShare (logs.share) does nothing in the editor and was flaky on device. Instead MpLog ALSO
    // appends to a small MP-ONLY text file under Application.persistentDataPath, named with the process
    // id so the editor, a Mac build, and a second (ParrelSync/server) instance each get their OWN file.
    // Read it straight off disk — no share sheet, no export step:
    //   Editor / Mac build:  ~/Library/Application Support/OZ Games Giant/GamesPanda/ritu_mp_<pid>.log
    private static System.IO.StreamWriter _mpFileWriter;
    private static bool _mpFileTried;
    private static int _mpLogsSinceFlush;   // batch disk flushes so a burst of logs in one frame doesn't stall it
    public static string MpLogFilePath;

    private static void EnsureMpFile()
    {
        if (_mpFileTried) return;
        _mpFileTried = true;
        try
        {
            int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            // SERVER repo: write the log co-located with the CLIENT logs and TAG it 'server', so the tester
            // finds + shares server-side logs (e.g. [CmdPushMatchSnapshot] accepted/REJECTED) right next to
            // ritu_mp_<clientpid>.log. The server's persistentDataPath is a DIFFERENT company folder
            // (games/panda.au) than the clients' (OZ Games Giant/GamesPanda), but both sit under the shared
            // 'Application Support' dir → resolve the clients' folder from there. Falls back to this build's own
            // persistentDataPath (e.g. headless/Edgegap) if the GamesPanda folder isn't present.
            string dir = Application.persistentDataPath;
            try
            {
                var appSupport = System.IO.Directory.GetParent(Application.persistentDataPath)?.Parent; // .../Application Support
                if (appSupport != null)
                {
                    string clientDir = System.IO.Path.Combine(appSupport.FullName, "OZ Games Giant", "GamesPanda");
                    if (System.IO.Directory.Exists(clientDir)) dir = clientDir;
                }
            }
            catch { }
            MpLogFilePath = System.IO.Path.Combine(dir, "ritu_mp_server_" + pid + ".log");
            // FileShare.ReadWrite so the file can be read off disk while the game still has it open.
            var fs = new System.IO.FileStream(MpLogFilePath, System.IO.FileMode.Create,
                System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
            // AutoFlush OFF: a burst of ~10 MpLog calls fires in the ball-release frame (RPC syncs +
            // BackwardDiag/DetDiag/BowlerSideDiag) and a synchronous disk flush on EACH stalled that frame ->
            // the deterministic ball-sim ran a 2-tick catch-up and the ball visibly JUMPED at release ("ball
            // start jerking on bowler side", worse deep in a match as the editor console bloats). Flush is
            // batched in MpLog instead; the full Unity console log (ritu_logs_*.txt) still captures every line.
            _mpFileWriter = new System.IO.StreamWriter(fs) { AutoFlush = false };
            _mpFileWriter.WriteLine("=== RITU MP SERVER LOG (pid " + pid + ") ===");
            Debug.Log("[MPLOG FILE] (SERVER) writing to: " + MpLogFilePath);
        }
        catch (System.Exception e) { UnityEngine.Debug.LogWarning("[MPLOG FILE] open failed: " + e.Message); }
    }

    // Ping-stable delivery rework toggle (Stages 2-3). When TRUE the batting client runs the
    // delivery as a LOCAL deterministic simulation (from the release params) and stops letting the
    // 20Hz position stream govern the ball — so the batsman times a smooth, ping-independent ball
    // (the 8Ball/Snooker principle). Default FALSE = current stream-driven behavior (ships dark).
    // Flip to true (and rebuild) to test local-sim delivery at high ping (e.g. 250ms).
    public static bool useLocalDeliverySim = false;

    // Phase 2 of the deterministic rework: layers a NetworkTime FIXED-TIMESTEP integrator on top of the
    // useLocalDeliverySim local sim, so both clients advance the delivery by identical 1/60 steps from the
    // SAME synced release instant → bit-identical delivery flight (no residual frame-based drift between the
    // two local sims). Requires useLocalDeliverySim. Default FALSE = current frame-based local sim (ships
    // dark); flip to true to test fixed-timestep delivery. Post-shot determinism (Phase 1) is already on.
    public static bool useDeterministicDelivery = false;
    // G1 v2 staying-client delivery self-heal watchdog kill-switch (MP-only, gated in TickDeliveryWatchdog).
    // Re-arms a stalled run-up or pre-contact in-flight ball after 10s with the opponent present.
    //
    // ENABLED 2026-08-20. It shipped dark pending a soak test, and while it was off the run-up window was
    // dead no matter what: _live ANDs both _midRunUp and _battingPreContact with this flag, so a client left
    // at currentActionState 2 — bowler running in, ball never released — had no recovery at all. That is the
    // stall behind "bowler side ball hui, batsman side nahi" and "player bhaag kar pitch ke beech ruk gaya",
    // reported across three test rounds. The 20-08 logs settle the soak question the comment asked for: every
    // stalled episode sat at state=2 for 12-32s with both players connected, timeScale at 1.00 and every
    // review/keeper/fielder guard clear, while a normal run-up completes in 2-3s — so the 10s threshold is
    // several times longer than legitimate play and these were genuine stalls, not spurious fires.
    // WATCH: a "[DeliveryWatchdog] Stalled pre-outcome delivery" line during play that did NOT visibly stall
    // means the window is firing early — turn this back to false and raise the threshold instead.
    public static bool useWatchdogG2 = true;
    // POST-SHOT freeze self-heal (default TRUE). Separate from the (dark) pre-contact G2 above. Recovers the
    // "shot hit but ball frozen at the keeper, BLANK status, no committed outcome" deadlock (tester: 1st-innings
    // end panels + game stuck) by re-bowling the stalled ball after a few seconds. Flip false to disable.
    public static bool useWatchdogPostShot = true;
    // DETERMINISTIC LOCKSTEP (see _Games/CRICKET/Docs/DETERMINISTIC_LOCKSTEP.md): full-local sim on both
    // clients, network carries only per-delivery seed + the batter's one shot input + an outcome checksum.
    // ACCEPTANCE PHASE (2026-07-11): ON for the lockstep test rounds — round 1 normal LAN, round 2 with
    // simulated 250ms latency (useSimulatedLatency below). Flip false to fall back to legacy instantly.
    public static bool useDeterministicContact = true;
    // Acceptance round 2: wraps the transport in Mirror's LatencySimulation at boot (editor/dev only) so a
    // LOCAL 2-editor match feels like a 250ms-RTT real-network match. Ships dark; flip true for the rig run.
    public static bool useSimulatedLatency = false;
    public static float simulatedLatencyMs = 125f;   // one-way; 125 each way = 250 RTT
    // Lockstep follower-flight delay (user's "bowling side follows behind" idea): the BOWLING side's ball
    // flight runs this many seconds behind real time, so the batter's swing input ALWAYS arrives before the
    // follower's ball reaches the contact plane — no late-input rewind, no keeper race, ping-proof up to this
    // value. The batter's flight stays real-time (they must react). Visual cost: on the bowling screen the
    // ball leaves the hand this much after the throw anim. 0 = off. 0.45 covers 250ms RTT + jitter.
    public static float lockstepFollowerFlightDelay = 0.45f;   // stabilization: back to the tested value until the plan-layer passes a test round
    // LOCKED DELIVERY PLAN (user design, supersedes the in-hand hold): the bowler's aim (spot/spin/speed/
    // swing/overstep) is already final at bowl-commit — so the ENTIRE release (params + RNG seed) is minted
    // and shipped at RUN-UP START, spot locked from there (the auto-bowl path always did this). The batter
    // launches from its OWN bowler anim event with zero release-time network dependency, and the bowling
    // side runs its whole delivery presentation (run-up included) lockstepFollowerFlightDelay behind real
    // time as a pure follower — internally seamless at ANY delay value.
    public static bool useLockedDeliveryPlan = true;
    // Locked-plan spot-adjust window (user request): after committing spin/speed the bowler keeps this many
    // seconds to fine-tune the bounce spot BEFORE it locks and the plan mints. 0 = lock immediately.
    public static float lockstepSpotAdjustSeconds = 3f;    // ON (2026-07-12): kills the bowling-side release-pose freeze ("bowler stuck 1 sec") —
                                                         // the whole bowling run-up starts lockstepFollowerFlightDelay late instead, so its anim
                                                         // event lands exactly on the delayed flight anchor and the hold auto-skips. Spawn-pos +
                                                         // spotLength plan bugs were fixed before this flip. Flip false = freeze returns but stable.
    // Online DRS FULL-REPLAY restore (ships DARK, default false). When false the online review shows the
    // lightweight cosmetic board (current behaviour). When TRUE the online review runs the SAME ball-tracking
    // replay as offline (the pre-2026-06-16 behaviour) on BOTH clients — restoring the full review the user
    // remembers. It was disabled online because the replay (a) can't complete on the bowling client (physics
    // suppressed) and (b) repositioning the ball corrupted the next delivery. Flip true + playtest to see if
    // the current (deterministic-delivery) codebase still freezes/corrupts; if it does, send logs. Per-repo.
    public static bool useFullReviewReplay = false;

    // ONLINE REVIEW KILL-SWITCH (tester request, 2026-07-26: "disable review functionality for now,
    // umpire directly announces the result, no review panel"). The online DRS flow has been a steady
    // source of divergence — the panel appearing on only one screen, cameras torn down mid-ball, and
    // outcomes flushed by the review resolver. While false, an online match never opens the review:
    // the on-field umpire decision (already committed at the appeal mark before any review would run)
    // simply stands and play continues, which is exactly what declining the review already did.
    // Offline/AI modes are untouched. Flip true to bring the online review back.
    public static bool enableOnlineReview = false;

    // Online DRS-review countdown SYNC. The 6s review timer was local-only on each client (started when each
    // client's own review triggered), so it showed a different countdown on the two screens. When true, the
    // batting authority stamps the review-start NetworkTime and relays it so BOTH clients run the identical
    // countdown off the shared clock. Degrades gracefully (full local 6s) if the relay is missing/stale, so
    // it can't hang the review. Flip false to revert to the old local-only timer. Per-repo (set on both).
    public static bool useSyncedDrsTimer = true;

    // Phase 1 of the server-authoritative MatchStateSnapshot reconnect rework. When TRUE, a reconnecting
    // client that received a VALID full snapshot (matchSnapshotJson) applies it atomically in
    // ApplyReconnectState — overriding the scattered per-field SyncVar restore — which kills the team-swap /
    // score-0-0 / wrong-bowler races at the source. FALLBACK is automatic: an empty/invalid blob (old server,
    // or before the first push) no-ops and the legacy SyncVar restore stands → zero regression until the
    // snapshot is actually present. Flip false to force the legacy path. Per-repo (set on both).
    public static bool useMatchSnapshotRestore = true;

    // Gated MP debug log — no-op unless MpVerboseLogs is on. Used in place of Debug.Log for the
    // high-frequency multiplayer RPC/flow logs.
    // Per-frame / per-delivery diagnostic chatter: kept in the ritu_mp FILE (needed for bug hunts) but
    // filtered from the Unity console, which it was flooding. Add/remove tags here as diagnostics evolve.
    private static readonly string[] _consoleQuietTags =
    {
        // [BowlerSideDiag] was REMOVED from this list. Quiet tags skip Debug.Log — and the file the
        // tester actually sends (ritu_logs_*.txt) is the Debug.Log capture, NOT the ritu_mp_<pid>.log
        // this method also writes. So every quiet diagnostic is invisible in the only log that reaches
        // us: BowlerSideDiag scored ZERO lines across the whole 06-08 session even though SetBowlerSide
        // demonstrably ran. It is the one line that answers "are the two clients on different sides?",
        // it fires once per SetBowlerSide (not per frame), and the console-flood it was quieted for only
        // ever mattered to a developer watching the Editor.
        "[StepBurst]", "[CamFollowDiag]",
        "[RpcChangeBallAngle]", "[RpcSendOppAck]", "[RpcFreezeBowlingSpot]", "[RpcBowlerWaiting]",
        "[RpcChangeBallData]", "[RpcSyncBallRelease]", "[RpcSetSwingParameter]", "[RpcLockSpeed]", "[RpcLockAngle]"
    };

    public static void MpLog(object msg)
    {
        if (!MpVerboseLogs) return;
        string _s = msg?.ToString() ?? string.Empty;
        bool _quiet = false;
        for (int i = 0; i < _consoleQuietTags.Length; i++)
            if (_s.Contains(_consoleQuietTags[i])) { _quiet = true; break; }
        if (!_quiet) Debug.Log(msg);
        EnsureMpFile();
        try
        {
            _mpFileWriter?.WriteLine(System.DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg);
            // Batched flush (AutoFlush is off): flush every 20 lines so a release-frame burst does at most one
            // disk write instead of ~10, while keeping the ritu_mp file current enough to read live.
            if (++_mpLogsSinceFlush >= 20) { _mpLogsSinceFlush = 0; _mpFileWriter?.Flush(); }
        }
        catch { }
    }

    public static void Log(object msg)
    {
        if (!MpVerboseLogs) return;
        Debug.LogError($"<color=green><b>{msg}</b></color>");
    }

    public static void Show(this object data)
    {
        Debug.LogError($"<color=#FFFF00><b>{data}</b></color>");
    }
    public static void ShowBlue(this object data)
    {
        Debug.LogError($"<b><color=blue>{data}</color> </b>");
    }
    public static void ShowBlue(this object data, string startText)
    {
        Debug.LogError($"<b><color=blue>{startText}: {data} </color> </b>");
    }
    public static void Show(this object data, object startText)
    {
        Debug.LogError($"<b><color=red>{startText.ToString()}:</color> <color=yellow>{data}</color></b>");
    }
    public static void LogInfo(object message)
    {
        Debug.LogError($"<b><color=yellow>[INFO]</color> {message} </b>");
    }
    public static void LogSuccess(object message)
    {
        Debug.LogError($"<b><color=green>[SUCCESS]</color> {message}</b>");
    }
    public static void LogCritical(object message)
    {
        Debug.LogError($"<b><color=red>[CRITICAL]</color> {message}</b>");
    }
    public static void Log(string message, Color color)
    {
        string colorCode = ColorUtility.ToHtmlStringRGB(color);
        Debug.Log($"<color=#{colorCode}><b>{message}</b></color>");
    }
    public static Sprite ConvertTextureToSprite(Texture2D texture)
    {
        if (texture == null)
        {
            ConstantsData_M.Log("Texture is null, cannot convert to sprite.");
            return null;
        }

        // Create a new sprite using the texture
        return Sprite.Create(texture,
                             new Rect(0, 0, texture.width, texture.height),
                             new Vector2(0.5f, 0.5f));  // pivot at the center
    }
    public static string GetRandomGuestName()
    {
        // Option 1: Pattern-based guest names (e.g., Guest1234)
        int randomNumber = Random.Range(1000, 9999); // Random 4-digit number
        return $"Guest{randomNumber}";
    }
    public static string GetRandomGuestNameFromList()
    {
        string[] guestNames = {
            "Alpha", "Beta", "Gamma", "Delta", "Epsilon",
            "Zeta", "Theta", "Omega", "Phoenix", "Nova"
        };

        // Pick a random name from the list
        string randomName = guestNames[Random.Range(0, guestNames.Length)];
        int randomNumber = Random.Range(100, 999); // Add random 3-digit number
        return $"{randomName}{randomNumber}";
    }

}
