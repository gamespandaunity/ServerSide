using System.Collections;
using Com.Google.Android.Gms.Games;
using Cricket;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExtensions;
// if (GameConstants.isWithAI == false)
public class CricketNetworkManager : NetworkBehaviour
{
    public static CricketNetworkManager instance;

    /// <summary>
    /// Gate for HIGH-FREQUENCY streaming Cmds (batsman position, collider relay). After a disconnect /
    /// session teardown the spawned CricketNetworkManager object is destroyed while the Ground scene keeps
    /// running, and the movement streamers kept calling Cmds on the destroyed object — Mirror's
    /// SendCommandInternal then throws NullReferenceException at UnityEngine.Object.GetName EVERY invocation
    /// (tester log 541f768c: 199 identical stacks through CmdChangeBatsmanPosition, "game get stuck").
    /// `instance != null` uses Unity's overloaded == so a destroyed object correctly reads as null.
    /// </summary>
    public static bool ReadyToSend => instance != null && Mirror.NetworkClient.isConnected;
    [SyncVar] public PanelsInfo currentOpenPanel;
    [SyncVar(hook = nameof(OnServerReconnect))] public bool gameStarted = false;

    // NOTE — DO NOT ADD SYNCVARS TO THIS COMPONENT WITHOUT REDEPLOYING THE DEDICATED SERVER IN THE SAME STEP.
    // A [SyncVar] string syncedServerBuildStamp used to live here, to print the server's build alongside the
    // client's. Mirror serialises SyncVars POSITIONALLY, so a client carrying a field the deployed server does
    // not have cannot parse the server's payload at all:
    //     OnDeserialize failed Exception=System.IO.EndOfStreamException object=CricketNetworkManager(Clone)
    // and EVERY SyncVar on this object is then lost — scores, team indices, striker, bowler. The 04-08 reports
    // show it exactly: that error appears ONLY on the builds carrying the extra field (202608041042 / 1055)
    // and never on the ones without it (202608020329 / 202608031830). It was the dominant cause of that day's
    // "Wrong result" reports. Removed. Commands and ClientRpcs are dispatched by name hash and are safe to add
    // one-sided; SyncVars are not.
    [SyncVar] public bool Reconnecting = false;
    [SyncVar] public bool tossSwipeDone = false;

    /// <summary>Server-only: tracks the pending 8-second auto-progress coroutine after a pre-game disconnect.</summary>
    private Coroutine _disconnectTimerCoroutine;

    /// <summary>
    /// Server-only: counts how many clients have signalled their MainMenu scene is loaded.
    /// Once both players signal, the server broadcasts RpcBothPlayersInMenu so panels open
    /// in sync — no more race between Mirror SyncVar delivery and Start() timing.
    /// </summary>
    private int _menuReadyCount = 0;

    // Server-side dedup state — prevents redundant RPC fan-out for high-frequency Cmds
    private Vector3 _lastBowlingSpotSent;
    private Vector3 _lastBatsmanPosSent;
    private bool _lastCanLoadGroundSent;

    [SyncVar(hook = nameof(OnScoreChanged))] public int currentScore;
    [SyncVar(hook = nameof(OnBattingTeamSynced))]  public int BattingTeamIndex;
    [SyncVar(hook = nameof(OnBowlingTeamSynced))]  public int BowlingTeamIndex;

    /// <summary>
    /// Pushes BattingTeamIndex/BowlingTeamIndex from the host's authoritative
    /// post-toss values onto the SyncVars so the joiner's local CONTROLLER
    /// values get overridden via the hooks.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSetBattingTeams(int batIdx, int bowlIdx)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetBattingTeams] batIdx={batIdx} bowlIdx={bowlIdx}");
        BattingTeamIndex = batIdx;
        BowlingTeamIndex = bowlIdx;
        // Persistent-session hygiene (tester #4): syncedBowlerListIndex is only ever written by CmdSetBowler,
        // so on a server session that outlives a match it still carries the PREVIOUS match's pick and the
        // batting client adopted it at the next match start (stale bowler for the first balls until the fresh
        // pick landed → visible mid-over bowler swap). This Cmd fires at the toss AND at the innings shift —
        // both points where a FRESH pick always follows (GetRandomBowler → CmdSetBowler), so parking the
        // SyncVar at -1 here just means "wait for the real pick" instead of adopting a stale one.
        syncedBowlerListIndex = -1;
        syncedBowlerAbsoluteIndex = -1;   // same staleness applies to the absolute twin
    }

    void OnBattingTeamSynced(int oldVal, int newVal)
    {
        if (NetworkServer.active) return;
        if (newVal == oldVal || newVal == 0) return;
        // Bug-4 fix, RECONNECT-ONLY: on a 2nd-innings reconnect this SyncVar re-delivers the stale
        // toss-era value, so during reconnect/restore the CmdSyncMatchProgress snapshot wins. But in
        // LIVE play this hook fires at the INNINGS SHIFT carrying the correctly-SWAPPED teams while
        // the snapshot still holds the PREVIOUS innings — preferring the snapshot there UNDID the
        // swap on both clients ("Ignoring toss-frozen 9; using live snapshot 4") → the same team
        // batted both innings → instant bogus win/loss. So: snapshot wins only while reconnecting.
        bool _restoringBat = RestorePending || (Launcher.Instance != null && Launcher.Instance.IsReConnecting());
        if (_restoringBat && syncedCurrentInnings >= 0 && syncedBattingTeamIndex >= 0)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][OnBattingTeamSynced] Reconnect: ignoring toss-frozen {newVal}; using live snapshot {syncedBattingTeamIndex}.");
            CONTROLLER.BattingTeamIndex = syncedBattingTeamIndex;
            // Same one-hook-at-a-time collapse the live path below guards against — but this branch
            // returns before ApplyTeamPairAtomically can close it, and here the correct sibling is the
            // SNAPSHOT value, not the toss SyncVar. Close it inline so the pair is never observed
            // collapsed mid-restore, which is exactly when the keeper depth, bowler type, fielder setup
            // and batsman side are re-derived off it.
            if (CONTROLLER.BattingTeamIndex == CONTROLLER.BowlingTeamIndex
                && syncedBowlingTeamIndex > 0 && syncedBattingTeamIndex != syncedBowlingTeamIndex)
            {
                ConstantsData_M.MpLog($"[CricketNetworkManager][TeamPair] restore batting hook left batting == bowling == {CONTROLLER.BattingTeamIndex}; applying snapshot sibling (bowl={syncedBowlingTeamIndex}).");
                CONTROLLER.BowlingTeamIndex = syncedBowlingTeamIndex;
            }
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][OnBattingTeamSynced] {oldVal} → {newVal}. Overriding CONTROLLER.BattingTeamIndex on joiner.");
        CONTROLLER.BattingTeamIndex = newVal;
        ApplyTeamPairAtomically("batting");
    }

    // The batting and bowling indices arrive as SEPARATE SyncVar hooks, and Mirror fires hooks ONE AT A TIME.
    // Between the two there is always a frame where one index holds the new value and the other still holds
    // the old — and on a swap that means the pair is briefly IDENTICAL:
    //     before  bat=9 bowl=4
    //     hook 1  bat=4 bowl=4   <-- both 4
    //     hook 2  bat=4 bowl=9
    // Anything reading the pair in that window sees a state that cannot exist. Worse, the collapse makes
    // `myTeamIndex == BattingTeamIndex` true on the client it should be false for, which is exactly the test
    // PushMatchSnapshotIfBatting uses to decide it is the authority — so the collapsed client publishes, and
    // publishes garbage. The 04-08 11:11 pair caught it on a FRESH match: [SetTeamIndex] bat=9 bowl=4
    // immediately followed by [PushMatchSnapshot] CLIENT sending: bat=4 bowl=4.
    //
    // Both SyncVar FIELDS are already deserialized by the time either hook runs, so whichever hook fires
    // first can close the window itself by applying its sibling too. The second hook then finds nothing to do.
    private void ApplyTeamPairAtomically(string which)
    {
        if (CONTROLLER.BattingTeamIndex != CONTROLLER.BowlingTeamIndex) return;
        if (BattingTeamIndex <= 0 || BowlingTeamIndex <= 0 || BattingTeamIndex == BowlingTeamIndex) return;
        ConstantsData_M.MpLog($"[CricketNetworkManager][TeamPair] {which} hook left batting == bowling == {CONTROLLER.BattingTeamIndex}; applying the sibling now (bat={BattingTeamIndex} bowl={BowlingTeamIndex}) so the pair is never observed collapsed.");
        CONTROLLER.BattingTeamIndex = BattingTeamIndex;
        CONTROLLER.BowlingTeamIndex = BowlingTeamIndex;
    }

    void OnBowlingTeamSynced(int oldVal, int newVal)
    {
        if (NetworkServer.active) return;
        if (newVal == oldVal || newVal == 0) return;
        // Bug-4 fix, RECONNECT-ONLY: see OnBattingTeamSynced — in live play the incoming value IS the
        // innings-swap update and must be applied; the snapshot only wins mid-reconnect.
        bool _restoringBowl = RestorePending || (Launcher.Instance != null && Launcher.Instance.IsReConnecting());
        if (_restoringBowl && syncedCurrentInnings >= 0 && syncedBowlingTeamIndex >= 0)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][OnBowlingTeamSynced] Reconnect: ignoring toss-frozen {newVal}; using live snapshot {syncedBowlingTeamIndex}.");
            CONTROLLER.BowlingTeamIndex = syncedBowlingTeamIndex;
            // Mirror image of the batting hook above — see the comment there.
            if (CONTROLLER.BowlingTeamIndex == CONTROLLER.BattingTeamIndex
                && syncedBattingTeamIndex > 0 && syncedBattingTeamIndex != syncedBowlingTeamIndex)
            {
                ConstantsData_M.MpLog($"[CricketNetworkManager][TeamPair] restore bowling hook left batting == bowling == {CONTROLLER.BowlingTeamIndex}; applying snapshot sibling (bat={syncedBattingTeamIndex}).");
                CONTROLLER.BattingTeamIndex = syncedBattingTeamIndex;
            }
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][OnBowlingTeamSynced] {oldVal} → {newVal}. Overriding CONTROLLER.BowlingTeamIndex on joiner.");
        CONTROLLER.BowlingTeamIndex = newVal;
        ApplyTeamPairAtomically("bowling");
    }

    // Reconnect state — Mirror auto-syncs these to reconnecting clients on scene reload
    [SyncVar] public int syncedMatchScores;
    [SyncVar] public int syncedMatchWickets;
    [SyncVar] public int syncedBallCount;       // ScoreBoardBallList.ballCount (balls in current over, 0-5)
    [SyncVar] public int syncedMatchBalls = -1; // currentMatchBalls (total balls — drives X.Y overs display)
    [SyncVar] public int syncedBallNumber = -1; // GameData.currentBallNumber (within-over index)
    [SyncVar] public string syncedBowlerSide = "left";   // game-start desync fix: networked default must be LEFT (matches GroundController.bowlerSide default + the online NewInnings force-left). Was "right" → before the bowling client pushed "left", a client applying this default (spawn / RestartBowlerSide) put the bowler on the RIGHT while the other showed LEFT = the game-start bowler-side desync.
    // Batsman indices — needed because AutoSave is disabled in multiplayer (PlayModeSelected==8)
    // so these are never persisted by AutoSave. Without them a reconnecting player sees the wrong
    // player names on the scoreboard and batting scorecard.
    [SyncVar] public int syncedStrikerIndex = 0;
    [SyncVar] public int syncedNonStrikerIndex = 1;
    [SyncVar] public int syncedBatsmanEntryIndex = 1; // GameData.newBatsmanEntryIndex (next batsman slot)

    // ── Full match-progress snapshot (reconnect-safe) ───────────────────────────────────────────
    // The single-team syncedMatchScores above only restores the LIVE batting team, and the
    // BattingTeamIndex/BowlingTeamIndex SyncVars are frozen at their first-innings (toss) values —
    // they are NEVER updated at the innings change. So a player who reconnected during the SECOND
    // innings restored (a) reversed batting/bowling indices, so the live score landed on the wrong
    // team (the actual batting team showed 0), and (b) a zeroed first-innings total (the target).
    // Result: score + target showed 0 / 1 ("lost match progress"). These index-agnostic per-team
    // totals + the live innings/index values are pushed by the batting player every ball and let a
    // reconnecting player rebuild the FULL match state regardless of which innings is in progress.
    [SyncVar] public int syncedTeam0Scores;
    [SyncVar] public int syncedTeam0Wickets;
    [SyncVar] public int syncedTeam0Balls       = -1;
    [SyncVar] public int syncedTeam1Scores;
    [SyncVar] public int syncedTeam1Wickets;
    [SyncVar] public int syncedTeam1Balls       = -1;
    [SyncVar] public int syncedCurrentInnings   = -1;
    [SyncVar] public int syncedBattingTeamIndex = -1;
    [SyncVar] public int syncedBowlingTeamIndex = -1;
    // First-ball bowler-side fix (#1): the selected bowler index is pushed ONLY via the non-buffered
    // RpcSetBowler, which the batting client can MISS on ball 1 (spawn/subscription window). Then the
    // batting client keeps default CurrentBowlerIndex=0 -> bowler #0's hand/type -> the bowler model is
    // mirrored/placed on the wrong side vs the bowling client. Back it with a SyncVar (appended LAST so
    // the existing SyncVar field order — the byte-identical networked surface — is preserved) so Mirror
    // auto-delivers it to a late/reconnecting batting client. Also hardens the spin-sign reconnect path.
    [SyncVar(hook = nameof(OnBowlerIndexSynced))] public int syncedBowlerListIndex = -1;
    // The DURABLE twin of RpcSyncBowlerAbsolute. syncedBowlerListIndex above is a scorecard POSITION —
    // an index into BowlingScoreCard.BowlersIndexArray, which lives in a scene singleton that a reconnect
    // scene-reload rebuilds, and which grows by one every over. So after an over boundary the same position
    // resolves to a DIFFERENT player, and the two screens end up on different bowlers (tester 8b2ffdb5:
    // "ik side fast bowler or opponent side spin bowler" — the mismap even changes bowler TYPE, dragging
    // keeper depth and the bowler's standing position with it). The absolute index was already relayed, but
    // only over a one-shot ClientRpc, which a reconnecting client is never sent. Persisting it here means the
    // reconnecter gets the identity that cannot be re-mapped.
    // Must stay in lockstep with the client's copy of this file — a SyncVar that exists on one side only
    // desynchronises Mirror's generated serialisation for this NetworkBehaviour.
    [SyncVar] public int syncedBowlerAbsoluteIndex = -1;

    // Field-placement fix (#3): fielderChangeIndex (which fielding-restriction layout is active) was
    // pushed ONLY via the non-buffered RpcFielderChangeIndex, so a reconnecting client kept its stale
    // local layout -> fielders in the wrong positions vs the opponent. Back it with a SyncVar (appended
    // LAST to preserve the byte-identical networked surface) so Mirror auto-delivers it on reconnect.
    [SyncVar] public int syncedFielderChangeIndex = 1;

    // Per-batsman score fix (reconnect scorecard 0/0): only TEAM totals + striker/nonStriker INDICES
    // were synced — the individual BatsmanList.RunsScored/.BallsPlayed were NEVER sent, so a
    // reconnecting player's batting card showed the on-strike + non-strike batsmen at 0/0 (runs OR
    // balls zero) even with a correct team total. Backed by SyncVars (appended LAST to preserve the
    // byte-identical networked field order) + restored via RestoreBatsmanScores().
    [SyncVar] public int syncedStrikerRuns     = 0;
    [SyncVar] public int syncedStrikerBalls    = 0;
    [SyncVar] public int syncedNonStrikerRuns  = 0;
    [SyncVar] public int syncedNonStrikerBalls = 0;

    // ─── Server-authoritative FULL match-state snapshot (Phase 0, 2026-06-23) ───────────────────────
    // Snooker-style ONE-blob reconnect state: the BATTING client serializes the ENTIRE match state
    // (MatchStateSnapshot) and pushes it via CmdPushMatchSnapshot; Mirror delivers this single SyncVar
    // atomically on (re)spawn → reconnect = deserialize + ApplyMatchStateSnapshot once (no per-field race,
    // no hook ordering, no missing state). Phase 0: built + pushed ALONGSIDE the synced* vars above (NO
    // behaviour change) + logged on reconnect for diff. Empty until first push (valid=false → apply no-ops).
    [SyncVar] public string matchSnapshotJson = "";
    private MatchStateSnapshot _serverSnapshot;        // server-side cache for the regression guard
    private static string _serverSnapshotJson = "";    // server STATIC — survives the player-object reconnect
                                                       // lifecycle; served on explicit request (the large string
                                                       // SyncVar doesn't reliably reach a reconnecter on spawn).

    void Awake()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][Awake] Instance set.");
        instance = this;
    }
    public void Start()
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][Start] Active scene: {SceneManager.GetActiveScene().name}");
        // BUILD FINGERPRINT — printed once per match, first thing in Ground. Deciding "is this fix even in
        // the build?" from a tester log used to take a whole round-trip (and produced two wrong verdicts),
        // because most fixes are silent guards that log nothing unless they fire. This line answers it
        // outright: the build stamp identifies the build, and the tag list names the reconnect/lockstep work
        // present in it. Add a tag here whenever a fix lands so the log keeps proving itself.
        ConstantsData_M.MpLog($"[Cricket][BUILD] stamp={BuildStamp.Timestamp} v={Application.version} tags="
            + "relayGraceWindow,swingAnchoredWindow,fielderSetupStash,noChaserFallback,betweenBallsRearm,"
            + "scorecardExtendedWait,freeHitRestore,wicketBallRestore,ackSendGuard,onlineReviewDisabled,"
            + "batHandRestore,rebowlUndoGuards,shotTimingGuard,boundaryVerdictRelay,"
            + "bowlingRankTryParse,snapshotApplyIsolated,restorePendingExpiry,"
            + "boundaryVerdictAllBranches,battingDeliveryWatchdog,ballExtrasAligned,boundaryPanelMarkers,duplicateBannerSuppressed,noUnverifiedSnapshotPush,ghostSendGuards,keeperDecisionOnly,chaseDiag,fielderSpeedRestore,bowlerAbsoluteRestore,chipStripParent,fielderRopeClamp,rollingBallStops,walkToRestingBall,serverBuildStamp,runUpCatchUp,standByResume,rearmRelayed,bowlingKeeperDecisionOnly,extrasIndexedByChips,releaseRpcNeverThrows,symmetricReArm,noDedupAtRest,resumeRestoresSpeed,oneChaserPerPoint,noCollapsedTeamSnapshot,atomicTeamPair,noServerStampSyncVar,droppedChaserIdles,ballStripRelay,edgeRulingWins,freeHitOneLegalBall,noBowlerDeriveWhileCollapsed,reconnectDeferNotDrop,restorePathTeamPair,throwTargetRelay,ultraEdgeReviewSymmetric,contactRulingOutranksSeed,freeHitClearsSnapshotFlag,lockstepHonoursNonBatRuling,reconnectAttemptOwnsRetry,bowlerSelectionBeatsSnapshot,bowlerSideDiagVisible,preGameRetryPerMatch,padRulingCarriesBallState,lineFreeHitCleared,bowlerAbsoluteRelay");
        if (SceneManager.GetActiveScene().name == "Ground")
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][Start] Scene is Ground.");
            if (NetworkServer.active)
            {
                ConstantsData_M.MpLog("[CricketNetworkManager][Start] NetworkServer is active. Setting gameStarted = true.");
                MatchFlow.Log("Cricket", $"ground loaded — match on ({FlowPlayers()})");
                gameStarted = true;
                this.Delay(3, () =>
                {
                    ConstantsData_M.MpLog("[CricketNetworkManager][Start] Delay 3s done. Setting Reconnecting = true.");
                    Reconnecting = true;
                });
            }
            else
            {
                ConstantsData_M.MpLog("[CricketNetworkManager][Start] NetworkServer is NOT active.");
            }
        }
        else
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][Start] Scene is NOT Ground. Skipping server setup.");
        }
    }

    public void OnScoreChanged(int oldValue, int newValue)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][OnScoreChanged] Score changed from {oldValue} to {newValue}.");
    }
    public void CmdSetTeam()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][CmdSetTeam] Called.");
    }

    /// <summary>
    /// Called by the BATTING PLAYER ONLY after each valid delivery to keep SyncVars authoritative.
    /// Mirror auto-syncs these to any reconnecting client when the scene reloads.
    ///
    /// IMPORTANT — only the batting player must call this. Both players simulate ball physics
    /// locally, so they can reach different results (e.g. boundary vs keeper catch) due to
    /// physics non-determinism. If both players called this, the server would receive two
    /// conflicting score/wicket values per ball, corrupting the state and breaking the
    /// target-to-chase and win/lose panels.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSyncGameState(int scores, int wickets, int ballCount, int matchBalls, int ballNumber,
                                  int strikerIndex, int nonStrikerIndex, int batsmanEntryIndex)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncGameState] scores={scores} wickets={wickets} ballCount={ballCount} matchBalls={matchBalls} ballNumber={ballNumber} striker={strikerIndex} nonStriker={nonStrikerIndex}");
        syncedMatchScores = scores;
        syncedMatchWickets = wickets;
        syncedBallCount = ballCount;
        syncedMatchBalls = matchBalls;
        syncedBallNumber = ballNumber;
        syncedStrikerIndex = strikerIndex;
        syncedNonStrikerIndex = nonStrikerIndex;
        syncedBatsmanEntryIndex = batsmanEntryIndex;
    }

    /// <summary>
    /// Called by the BATTING PLAYER ONLY (alongside CmdSyncGameState) after each delivery to keep a
    /// FULL, index-agnostic snapshot of both teams' totals + the live innings/indices on the server.
    /// A reconnecting player restores from this so the SECOND-innings target (first-innings total)
    /// and the correct batting/bowling team indices survive a reconnect — see RPC restore in
    /// ApplyReconnectState. Only the batting player calls it, so there is no two-writer conflict.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSyncMatchProgress(int t0Scores, int t0Wickets, int t0Balls,
                                      int t1Scores, int t1Wickets, int t1Balls,
                                      int innings, int battingIdx, int bowlingIdx)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncMatchProgress] innings={innings} bat={battingIdx} bowl={bowlingIdx} T0={t0Scores}/{t0Wickets} T1={t1Scores}/{t1Wickets}");
        // ── Reconnect SyncVar-clobber guard (#5: reconnect swaps teams + game stuck) ──────────────
        // RUNS SERVER-SIDE (this is a [Command] body) — the single chokepoint for ALL batting push
        // sites. On reconnect, the reconnecting client's freshly-respawned NetworkManager runs
        // StartGame()->NewInnings() BEFORE its reconnect flags engage, so its Gap-C push fires with
        // RESET state (innings=0, toss-default bat/bowl, 0/0/0/0) and — landing just AFTER the good
        // restore — clobbers the authoritative snapshot (teams swap, scores zero, game stuck).
        // Reject a push that REGRESSES an already-established snapshot: a LOWER innings (innings only
        // advances 0->1 in MP; it resets to 0 only in NewGame's match-zero, never legitimately
        // mid-match), OR an all-zero team-total push while a non-zero snapshot already exists. We do
        // NOT reject a mere same-innings score DECREASE — RebowlLastBallMultiplayer can legitimately
        // lower the batting team's score. A genuinely fresh match has syncedCurrentInnings==-1, so the
        // first push of every match always passes (guard precondition is false).
        if (syncedCurrentInnings >= 0)
        {
            bool inningsRegressed = innings < syncedCurrentInnings;
            bool allZeroPush = t0Scores == 0 && t0Wickets == 0 && t1Scores == 0 && t1Wickets == 0;
            bool establishedNonZero = syncedTeam0Scores > 0 || syncedTeam1Scores > 0
                                      || syncedTeam0Wickets > 0 || syncedTeam1Wickets > 0;
            if (inningsRegressed || (allZeroPush && establishedNonZero))
            {
                ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncMatchProgress] REJECTED stale reset push (innings={innings} bat={battingIdx} T0={t0Scores}/{t0Wickets} T1={t1Scores}/{t1Wickets}) over live innings={syncedCurrentInnings} bat={syncedBattingTeamIndex} T0={syncedTeam0Scores}/{syncedTeam0Wickets} T1={syncedTeam1Scores}/{syncedTeam1Wickets}.");
                return;
            }
        }
        bool _inningsChangedThisPush = innings != syncedCurrentInnings;
        if (_inningsChangedThisPush)
            MatchFlow.Log("Cricket", innings <= 0
                ? "innings 1 starts"
                : $"innings 1 over — {t1Scores}/{t1Wickets} ({FlowOvers(t1Balls)} ov), target {t1Scores + 1}; innings 2 starts");
        syncedTeam0Scores = t0Scores; syncedTeam0Wickets = t0Wickets; syncedTeam0Balls = t0Balls;
        syncedTeam1Scores = t1Scores; syncedTeam1Wickets = t1Wickets; syncedTeam1Balls = t1Balls;
        syncedCurrentInnings   = innings;
        syncedBattingTeamIndex = battingIdx;
        syncedBowlingTeamIndex = bowlingIdx;
        // Over-counter authority (09-07 logs: across one match the restores saw balls=4 → 2 → 6 while runs went
        // 8 → 12 → 16 — i.e. the ball counters REGRESSED): syncedMatchBalls/BallNumber/BallCount were pushed only
        // by the ACK-pair CmdSyncGameState, which the reconnect storms skip (a mid-shot disconnect never
        // completes its ACK pair), so a restore could resume at a wrong ball / a fresh over ("bowling 3 overs
        // se onward continue hui" after a 2.5-over disconnect, state corrupt into innings 2). Stamp them HERE
        // too — this Cmd fires per valid ball from the batting authority, so they can never lag. t0 is the
        // batting team by contract; the within-over counters derive from the total (ballCount = balls % 6,
        // currentBallNumber = ballCount - 1, matching the live convention). Never regress on a late/reordered
        // push — except across an innings change, where the new innings legitimately restarts at 0.
        if (_inningsChangedThisPush || t0Balls >= syncedMatchBalls)
        {
            syncedMatchBalls   = t0Balls;
            syncedMatchScores  = t0Scores;
            syncedMatchWickets = t0Wickets;
            syncedBallCount    = t0Balls % 6;
            syncedBallNumber   = (t0Balls % 6) - 1;
        }
    }

    /// <summary>
    /// Batting player only — pushes the on-strike + non-strike batsmen's individual runs/balls each
    /// ball so a reconnecting player's batting scorecard restores their scores instead of showing
    /// 0/0. The INDICES travel via syncedStrikerIndex/syncedNonStrikerIndex; this carries the
    /// per-player totals for those indices. Restored by RestoreBatsmanScores().
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSyncBatsmanScores(int sRuns, int sBalls, int nsRuns, int nsBalls)
    {
        syncedStrikerRuns     = sRuns;
        syncedStrikerBalls    = sBalls;
        syncedNonStrikerRuns  = nsRuns;
        syncedNonStrikerBalls = nsBalls;
    }

    // ─── MatchStateSnapshot build / push / apply (Phase 0 — runs ALONGSIDE the synced* path) ──────────
    /// <summary>BATTING client only: serialize the full live match state + push to the server. Single
    /// chokepoint (replaces the 5 overlapping CmdSync* in a later phase). Call at SETTLED events only.</summary>
    public void PushMatchSnapshotIfBatting()
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BattingTeamIndex) return;   // single-writer rule
        // NEVER push from UNVERIFIED post-scene-reload state. A reconnect reloads Ground, and the first
        // SetTeamIndex on the reloaded scene can run BEFORE Reconnecting/RestorePending are set and before any
        // SyncVar has landed — its own reconnect guard is false, so it re-derives the teams from the invariant
        // meFirstBatting and can land on the FLIPPED pair. The 30-07 ritu_mp log catches it exactly:
        //   [SetTeamIndex] reconnect=False restorePending=False authIdx=False syncedInnings=-1 syncedBat=-1
        //                  syncedBowl=-1 meFirstBatting=1 innings=0 myTeam=4 opp=9 bat=4 bowl=9
        //   [PushMatchSnapshot] CLIENT sending: innings=0 bat=4 bowl=9 ...
        // — the match was at bat=9/bowl=4, and that flipped pair was then shipped as AUTHORITATIVE. The local
        // flip self-heals when ApplyReconnectState lands ~2s later; the pushed snapshot does not, so the OTHER
        // client restores the swapped teams and ends up with a different bowler (tester: "game again start ki
        // or bowler different sides py khray thy").
        // A fresh match (no reconnect yet) is unaffected. After a reconnect we require the authoritative
        // SyncVars to have arrived before this client is allowed to write the snapshot.
        bool _hasReconnected = Launcher.Instance != null && Launcher.Instance.GetReconnectCount() > 0;
        if (_hasReconnected && syncedCurrentInnings < 0)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][PushMatchSnapshot] SKIPPED — post-reconnect state not verified yet (syncedInnings={syncedCurrentInnings}, bat={CONTROLLER.BattingTeamIndex}, bowl={CONTROLLER.BowlingTeamIndex}). Refusing to publish possibly-flipped team indices.");
            return;
        }
        // NEVER publish a snapshot whose batting and bowling teams are the SAME. That state is structurally
        // impossible and always corrupt — every batsman identity, position and hand is derived from those two
        // indices, so a snapshot carrying it puts the two clients on different players. The 04-08 11:11 pair
        // catches it at a FRESH match start (no reconnect at all):
        //   [SetTeamIndex] ... meFirstBatting=0 myTeam=4 opp=9 bat=9 bowl=4
        //   [PushMatchSnapshot] CLIENT sending: innings=0 bat=4 bowl=4 striker=0 bowler=6
        // while the other client pushed a correct bat=9 bowl=4. The indices momentarily collapse onto
        // myTeamIndex, and because that also satisfies the single-writer test above, the bad snapshot is the
        // one that goes out. The post-reconnect guard below cannot help here — reconnectCount is 0 on a fresh
        // match. Refusing the write costs nothing: a correct snapshot follows within the same over.
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][PushMatchSnapshot] REFUSED — battingTeamIndex and bowlingTeamIndex are both {CONTROLLER.BattingTeamIndex}. The indices have collapsed; publishing that would put the two clients on different batsmen.");
            return;
        }
        MatchStateSnapshot snap = BuildSnapshotFromLiveState();
        if (snap == null) { ConstantsData_M.MpLog("[CricketNetworkManager][PushMatchSnapshot] CLIENT build returned null (TeamList?)."); return; }
        string json = JsonUtility.ToJson(snap);
        // CLIENT-side proof the batting client builds + sends the snapshot. If this line appears but the
        // reconnect still logs "SNAPSHOT empty", the SERVER is not running Phase-0 code (CmdPushMatchSnapshot
        // is a [Command] = runs server-side; an old dedicated server drops the unknown command).
        ConstantsData_M.MpLog($"[CricketNetworkManager][PushMatchSnapshot] CLIENT sending: innings={snap.currentInnings} bat={snap.battingTeamIndex} bowl={snap.bowlingTeamIndex} striker={snap.strikerIndex} bowler={snap.currentBowlerIndex} len={json.Length}");
        CmdPushMatchSnapshot(json);
    }

    [Command(requiresAuthority = false)]
    public void CmdPushMatchSnapshot(string json)
    {
        MatchStateSnapshot incoming = null;
        try { incoming = JsonUtility.FromJson<MatchStateSnapshot>(json); } catch { }
        if (incoming == null || !incoming.valid) return;
        // Server-side twin of the sender's refusal above — a collapsed pair must never become authoritative,
        // whatever client sent it or which build that client is running.
        if (incoming.battingTeamIndex == incoming.bowlingTeamIndex)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][CmdPushMatchSnapshot] REJECTED — incoming snapshot has battingTeamIndex == bowlingTeamIndex == {incoming.battingTeamIndex}.");
            return;
        }
        // PORT the CmdSyncMatchProgress stale-reset guard: a freshly-respawned reconnecting client runs
        // NewInnings()/NewGame() and would push RESET state (innings=0, 0/0) just AFTER the good restore,
        // clobbering the authoritative snapshot. Reject a regressed/all-zero push over an established one.
        if (_serverSnapshot != null && _serverSnapshot.valid)
        {
            bool inningsRegressed   = incoming.currentInnings < _serverSnapshot.currentInnings;
            bool allZeroPush        = SnapTotalScores(incoming) == 0 && SnapTotalWickets(incoming) == 0;
            bool establishedNonZero = SnapTotalScores(_serverSnapshot) > 0 || SnapTotalWickets(_serverSnapshot) > 0;
            if (inningsRegressed || (allZeroPush && establishedNonZero))
            {
                ConstantsData_M.MpLog($"[CricketNetworkManager][CmdPushMatchSnapshot] REJECTED stale/regressed push (innings={incoming.currentInnings}) over live innings={_serverSnapshot.currentInnings}.");
                return;
            }
        }
        _serverSnapshot = incoming;
        _serverSnapshotJson = json;  // static mirror — survives the reconnecter's object respawn; served on request
        matchSnapshotJson = json;    // SyncVar → also buffered to all (works for the bowling/observer follower)
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdPushMatchSnapshot] accepted: innings={incoming.currentInnings} bat={incoming.battingTeamIndex} bowl={incoming.bowlingTeamIndex} striker={incoming.strikerIndex} bowler={incoming.currentBowlerIndex} len={json.Length}");
    }

    // Reliable snapshot delivery on reconnect: the large matchSnapshotJson SyncVar did NOT reach a reconnecting
    // BATTING client on spawn (small int synced* delivered, the big string didn't). So the reconnecter EXPLICITLY
    // requests it and the server replies via TargetRpc from the static cache (which survives the player-object
    // reconnect lifecycle). 3-4 KB over a reliable TargetRpc delivers fine.
    [Command(requiresAuthority = false)]
    public void CmdRequestMatchSnapshot(NetworkConnectionToClient sender = null)
    {
        if (string.IsNullOrEmpty(_serverSnapshotJson) || sender == null) return;
        // Red-indicator resync: CmdChangeBowlinSpot de-dups on _lastBowlingSpotSent and returns WITHOUT
        // relaying when the spot is unchanged — which it always is across a disconnect, since the bowler
        // never moved it. That silently turned the reconnect re-broadcast into a no-op, so the rejoining
        // client kept the scene-default marker and the next delivery pitched elsewhere on that screen.
        // Clearing the cache here means the next spot send is always relayed to both clients.
        _lastBowlingSpotSent = Vector3.positiveInfinity;
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdRequestMatchSnapshot] serving cached snapshot (len={_serverSnapshotJson.Length}) to reconnecter.");
        TargetReceiveMatchSnapshot(sender, _serverSnapshotJson);
    }

    [TargetRpc]
    public void TargetReceiveMatchSnapshot(NetworkConnectionToClient target, string json)
    {
        if (!ConstantsData_M.useMatchSnapshotRestore || string.IsNullOrEmpty(json)) return;
        MatchStateSnapshot snap = null;
        try { snap = JsonUtility.FromJson<MatchStateSnapshot>(json); } catch { }
        if (snap == null || !snap.valid) return;
        ApplyMatchStateSnapshot(snap);
        // Re-draw the UI that ApplyReconnectState already painted from the (then-empty) blob.
        if (Singleton<GameData>.instance != null && CONTROLLER.TeamList != null
            && CONTROLLER.BattingTeamIndex >= 0 && CONTROLLER.BattingTeamIndex < CONTROLLER.TeamList.Length)
        {
            Singleton<GameData>.instance.scoreDisplayString =
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" +
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
            Singleton<GameData>.instance.oversDisplayString =
                Singleton<GameData>.instance.GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
        }
        if (Singleton<Scoreboard>.instance != null) Singleton<Scoreboard>.instance.UpdateScoreCard();
        ConstantsData_M.MpLog($"[CricketNetworkManager][TargetReceiveMatchSnapshot] APPLIED requested snapshot: innings={snap.currentInnings} bat={CONTROLLER.BattingTeamIndex} bowl={CONTROLLER.BowlingTeamIndex} striker={CONTROLLER.StrikerIndex} bowler={CONTROLLER.CurrentBowlerIndex}");
    }

    private static int SnapTotalScores(MatchStateSnapshot s)
    { int n = 0; if (s != null && s.teams != null) foreach (var x in s.teams) if (x != null) n += x.scores; return n; }
    private static int SnapTotalWickets(MatchStateSnapshot s)
    { int n = 0; if (s != null && s.teams != null) foreach (var x in s.teams) if (x != null) n += x.wickets; return n; }

    /// <summary>Serialize the entire live match state into a MatchStateSnapshot (called on the batting client).</summary>
    public MatchStateSnapshot BuildSnapshotFromLiveState()
    {
        if (CONTROLLER.TeamList == null) return null;
        var snap = new MatchStateSnapshot();
        snap.valid = true;
        snap.currentInnings = CONTROLLER.currentInnings;
        snap.battingTeamIndex = CONTROLLER.BattingTeamIndex;
        snap.bowlingTeamIndex = CONTROLLER.BowlingTeamIndex;
        snap.inningsCompleted = CONTROLLER.InningsCompleted;
        snap.mpInningsCompleted = CONTROLLER.MPInningsCompleted;
        snap.strikerIndex = CONTROLLER.StrikerIndex;
        snap.nonStrikerIndex = CONTROLLER.NonStrikerIndex;
        GameData gd = GameData.instance;
        snap.newBatsmanEntryIndex = gd != null ? gd.newBatsmanEntryIndex : -1;
        snap.currentBallNumber = gd != null ? gd.currentBallNumber : 0;
        var sbl = Singleton<ScoreBoardBallList>.instance;
        snap.ballCount = sbl != null ? sbl.ballCount : 0;
        if (sbl != null)
        {
            snap.ballListInfo = (sbl.ballList != null) ? string.Join("|", sbl.ballList) : "";
            snap.ballExtras   = (sbl.extras   != null) ? string.Join("|", sbl.extras)   : "";
        }
        snap.currentBowlerIndex = CONTROLLER.CurrentBowlerIndex;
        var gc = Singleton<GroundController>.instance;
        snap.bowlerSide = (gc != null && !string.IsNullOrEmpty(gc.bowlerSide)) ? gc.bowlerSide : "right";
        snap.fielderChangeIndex = CONTROLLER.fielderChangeIndex;
        // Per-delivery rule state a scene reload would destroy — see MatchStateSnapshot for why.
        var _gcSnap = Singleton<GroundController>.instance;
        snap.freeHitActive = _gcSnap != null && _gcSnap.FreeHitActiveForSnapshot;
        snap.wicketBallPending = gd != null && gd.WicketBallPendingForSnapshot;

        snap.teams = new TeamSnapshot[CONTROLLER.TeamList.Length];
        for (int ti = 0; ti < CONTROLLER.TeamList.Length; ti++)
        {
            var t = CONTROLLER.TeamList[ti];
            var ts = new TeamSnapshot { teamIndex = ti };
            if (t != null)
            {
                ts.scores = t.currentMatchScores; ts.wickets = t.currentMatchWickets; ts.balls = t.currentMatchBalls;
                ts.extras = t.currentMatchExtras; ts.lbs = t.currentMatchLbs; ts.byes = t.currentMatchbyes;
                ts.noball = t.currentMatchNoball; ts.wideBall = t.currentMatchWideBall; ts.drsLeft = t.noofDRSLeft;
                ts.ballUpdate = (t.ballUpdate != null) ? (string[])t.ballUpdate.Clone() : new string[6];
                if (t.PlayerList != null)
                {
                    // Only ACTIVE players (have batted / are batting / have bowled) — keeps the blob small
                    // enough for one Mirror message. An all-default record (un-batted #N) is omitted; on apply
                    // those PlayerList entries stay at their post-scene-reload default (0/0/""), which is correct.
                    var batList = new System.Collections.Generic.List<BatsmanSnapshot>();
                    var bowList = new System.Collections.Generic.List<BowlerSnapshot>();
                    for (int pi = 0; pi < t.PlayerList.Length; pi++)
                    {
                        var p = t.PlayerList[pi];
                        if (p == null) continue;
                        var b = p.BatsmanList;
                        if (b != null && (b.RunsScored != 0 || b.BallsPlayed != 0 || b.Fours != 0 || b.Sixes != 0
                                          || b.FOW != 0 || !string.IsNullOrEmpty(b.Status)))
                        {
                            batList.Add(new BatsmanSnapshot { idx = pi, runs = b.RunsScored, balls = b.BallsPlayed,
                                fours = b.Fours, sixes = b.Sixes, fow = b.FOW, status = b.Status ?? "",
                                partnershipRuns = b.currPatnerShipRuns, partnershipBalls = b.currPatnerShipBalls });
                        }
                        var w = p.BowlerList;
                        if (w != null && (w.BallsBowled != 0 || w.RunsGiven != 0 || w.Wicket != 0 || w.Maiden != 0))
                        {
                            bowList.Add(new BowlerSnapshot { idx = pi, runsGiven = w.RunsGiven, ballsBowled = w.BallsBowled,
                                maiden = w.Maiden, wicket = w.Wicket, wicketsInBallCount = w.WicketsInBallCount,
                                maxBallInMatch = w.maxBallInMatch });
                        }
                    }
                    ts.batsmen = batList.ToArray();
                    ts.bowlers = bowList.ToArray();
                }
            }
            snap.teams[ti] = ts;
        }
        return snap;
    }

    /// <summary>Apply a snapshot to the live state in deterministic order. NOT called in Phase 0 (built +
    /// log-diffed only). Wired into OnStartClient in Phase 1, replacing the scattered restore.</summary>
    public void ApplyMatchStateSnapshot(MatchStateSnapshot snap)
    {
        if (snap == null || !snap.valid || CONTROLLER.TeamList == null) return;
        // 1. team roles  2. innings
        CONTROLLER.BattingTeamIndex = snap.battingTeamIndex;
        CONTROLLER.BowlingTeamIndex = snap.bowlingTeamIndex;
        CONTROLLER.currentInnings = snap.currentInnings;
        CONTROLLER.InningsCompleted = snap.inningsCompleted;
        CONTROLLER.MPInningsCompleted = snap.mpInningsCompleted;
        // 3. per-team scores/extras/DRS/history + 4. records (all players)
        if (snap.teams != null)
        {
            foreach (var t in snap.teams)
            {
                if (t == null || t.teamIndex < 0 || t.teamIndex >= CONTROLLER.TeamList.Length) continue;
                var team = CONTROLLER.TeamList[t.teamIndex];
                if (team == null) continue;
                bool isBat = t.teamIndex == snap.battingTeamIndex;   // MAX-clamp batting team only (anti-regression)
                team.currentMatchScores  = isBat ? Mathf.Max(team.currentMatchScores,  t.scores)  : t.scores;
                team.currentMatchWickets = isBat ? Mathf.Max(team.currentMatchWickets, t.wickets) : t.wickets;
                team.currentMatchBalls   = isBat ? Mathf.Max(team.currentMatchBalls,   t.balls)   : t.balls;
                team.currentMatchExtras = t.extras; team.currentMatchLbs = t.lbs; team.currentMatchbyes = t.byes;
                team.currentMatchNoball = t.noball; team.currentMatchWideBall = t.wideBall; team.noofDRSLeft = t.drsLeft;
                if (t.ballUpdate != null && team.ballUpdate != null)
                    System.Array.Copy(t.ballUpdate, team.ballUpdate, Mathf.Min(team.ballUpdate.Length, t.ballUpdate.Length));
                if (team.PlayerList != null)
                {
                    // Records are now a SPARSE list (active players only) keyed by idx — map back by idx.
                    if (t.batsmen != null)
                    {
                        foreach (var bs in t.batsmen)
                        {
                            if (bs == null || bs.idx < 0 || bs.idx >= team.PlayerList.Length) continue;
                            var p = team.PlayerList[bs.idx];
                            if (p == null || p.BatsmanList == null) continue;
                            var b = p.BatsmanList;
                            b.RunsScored = bs.runs; b.BallsPlayed = bs.balls; b.Fours = bs.fours; b.Sixes = bs.sixes;
                            b.FOW = bs.fow; b.Status = bs.status; b.currPatnerShipRuns = bs.partnershipRuns;
                            b.currPatnerShipBalls = bs.partnershipBalls;
                        }
                    }
                    if (t.bowlers != null)
                    {
                        foreach (var bw in t.bowlers)
                        {
                            if (bw == null || bw.idx < 0 || bw.idx >= team.PlayerList.Length) continue;
                            var p = team.PlayerList[bw.idx];
                            if (p == null || p.BowlerList == null) continue;
                            var w = p.BowlerList;
                            w.RunsGiven = bw.runsGiven; w.BallsBowled = bw.ballsBowled; w.Maiden = bw.maiden;
                            w.Wicket = bw.wicket; w.WicketsInBallCount = bw.wicketsInBallCount; w.maxBallInMatch = bw.maxBallInMatch;
                        }
                    }
                }
            }
        }
        // 5. batting indices + within-over (keep 0-5 validation)
        // Striker "changed side after reconnect" fix (tester; e.g. batter dropped right after a power
        // shot's runs): the snapshot can be pushed by EITHER client, and the bowling side's striker view
        // lags a commit (it only updates via RpcCorrectBallState) — its push overwrites the batter's
        // fresher snapshot on the server, and this apply then restored the PRE-SWAP striker → the wrong
        // batsman stood at the crease (real log: snapshot nonStriker=2 vs SyncVar nonStriker=1). The
        // syncedStriker/NonStriker SyncVars are written ONLY on the batting authority's commit path
        // (CmdCorrectBallState/CmdSyncGameState) → always the committed truth. Prefer them, snapshot =
        // fallback — same pattern as bowlerSide → syncedBowlerSide below.
        // ...but "have the SyncVars ever been WRITTEN?" cannot be asked with >= 0 here: unlike the other
        // SyncVars, these default to 0 and 1 — which are also the REAL opening-batsman indices — so
        // `syncedStrikerIndex >= 0` was ALWAYS true and an unwritten pair silently forced striker=0 /
        // nonStriker=1 over the snapshot's correct values. That is the recurring "batsman ki side dono
        // screens pe different" report: one client restored the true striker, the reconnecting one got 0/1
        // (real logs: 10494 restored striker=0 while 10495 held striker=1). Gate on syncedBallNumber, the
        // -1-defaulted "a sync has actually happened" signal the other restore blocks in this file use.
        bool _batsmanSyncVarsWritten = syncedBallNumber >= 0;
        CONTROLLER.StrikerIndex = _batsmanSyncVarsWritten ? syncedStrikerIndex : snap.strikerIndex;
        CONTROLLER.NonStrikerIndex = _batsmanSyncVarsWritten ? syncedNonStrikerIndex : snap.nonStrikerIndex;
        if (CONTROLLER.StrikerIndex != snap.strikerIndex || CONTROLLER.NonStrikerIndex != snap.nonStrikerIndex)
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Striker restore: {(_batsmanSyncVarsWritten ? "SyncVars" : "snapshot (SyncVars never written)")} ({CONTROLLER.StrikerIndex}/{CONTROLLER.NonStrikerIndex}) vs snapshot ({snap.strikerIndex}/{snap.nonStrikerIndex})."); 
        // ballCount is ScoreBoardBallList.ballCount, which AddBall keeps == ballList.Count: the number of
        // DELIVERIES bowled this over INCLUDING no-balls/wides — NOT a 0-5 legal-ball index. An over with
        // extras legitimately pushes it past 5, which made overValid FALSE and then reset currentBallNumber
        // to 0, destroying the reconnected client's real over position: the `currentBallNumber == 5` over-end
        // branch (GameData ~2347) never fired at the true boundary, so Scoreboard.NewOver() never ran and the
        // ball-chip strip kept accumulating ACROSS overs (tester: 8 chips on the reconnected side vs 5 on the
        // other). Validate ONLY currentBallNumber (a genuine 0-5 index); restore ballCount verbatim.
        bool overValid = snap.currentBallNumber >= 0 && snap.currentBallNumber <= 5;
        GameData gd = GameData.instance;
        if (gd != null) { gd.currentBallNumber = overValid ? snap.currentBallNumber : 0; gd.newBatsmanEntryIndex = snap.newBatsmanEntryIndex; }
        var sbl = Singleton<ScoreBoardBallList>.instance;
        if (sbl == null) sbl = UnityEngine.Object.FindFirstObjectByType<ScoreBoardBallList>(FindObjectsInactive.Include);
        if (sbl != null) sbl.ballCount = Mathf.Max(0, snap.ballCount);
        // Ball-by-ball chip strip rebuild: ScoreBoardBallList.ballList is a scene singleton WIPED by the
        // reconnect scene-reload and NOT covered by ballUpdate[6] (legal-balls-only, no nb/wd chips), so the
        // reconnected bottom strip came back EMPTY ("deliveries ki history reconnection pe wapas nahi aati").
        // Restore the exact chips (incl. no-ball/wide, in delivery order) from the snapshot. Written straight
        // into the public lists (not via AddBall) so AddBall's own ballCount++ side-effect can't fire; then
        // pin ballCount to ballList.Count so the AddBall(ballCount-1) extra-indexing invariant the live game
        // relies on holds for the NEXT delivery. Old-server snapshot has ballListInfo="" → block is skipped
        // (strip stays as-is, exactly the prior behaviour). The strip is REDRAWN by the UpdateScoreCard that
        // ApplyReconnectState / TargetReceiveMatchSnapshot run right after this apply.
        // Never repaint a COMPLETED innings' strip. Those chips belong to an over that will never
        // continue: once the innings ended, the next thing to happen is a fresh innings whose strip
        // starts empty.
        //
        // This is the half of the innings-boundary fix that does not depend on anyone being connected.
        // The re-push added in introCompleted is made BY THE BATTING CLIENT, so when both players drop
        // across the innings shift nobody publishes the cleared strip, and the copy the server still
        // holds is the one taken during the PREVIOUS innings' last ball - the tester's "second innings
        // mein first innings ke last over ki balls history fetch hui", reproduced with both sides gone.
        // The server cannot be told to clear it either: CmdSetBattingTeams is the only server-side
        // innings hook and it is gated on isMasterClient, which is exactly the client that just left.
        // The snapshot already carries the flag, so the restoring client can rule on this alone.
        if (snap.inningsCompleted || snap.mpInningsCompleted)
        {
            int _heldChips = 0;
            if (!string.IsNullOrEmpty(snap.ballListInfo))
                foreach (var _chip in snap.ballListInfo.Split('|'))
                    if (!string.IsNullOrEmpty(_chip)) _heldChips++;
            if (sbl != null)
            {
                if (sbl.ballList != null) sbl.ballList.Clear();
                if (sbl.extras != null) sbl.extras.Clear();
                sbl.ballCount = 0;   // ballCount was set from the snapshot above; without this it would
                                     // outlive the chips and trip the AddBall extra-indexing invariant.
            }
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Ball-chip strip NOT restored — the snapshot was captured with the innings already completed (innings={snap.currentInnings}, {_heldChips} chips held). The new innings starts with an empty strip.");
        }
        else if (sbl != null && sbl.ballList != null && !string.IsNullOrEmpty(snap.ballListInfo))
        {
            sbl.ballList.Clear();
            foreach (var _chip in snap.ballListInfo.Split('|'))
                if (!string.IsNullOrEmpty(_chip)) sbl.ballList.Add(_chip);
            if (sbl.extras != null)
            {
                sbl.extras.Clear();
                if (!string.IsNullOrEmpty(snap.ballExtras))
                    foreach (var _ex in snap.ballExtras.Split('|'))
                        if (!string.IsNullOrEmpty(_ex)) sbl.extras.Add(_ex);
            }
            sbl.ballCount = sbl.ballList.Count;
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Ball-chip strip restored: {sbl.ballList.Count} chips, {(sbl.extras != null ? sbl.extras.Count : 0)} extra-tokens.");
        }
        // 6. bowler / side / fielder (apply on BOTH clients — fixes the batting-only hook gap)
        //
        // Prefer the SyncVar over the snapshot for WHO is bowling — the same precedence, and for exactly the
        // same reason, as _restoreSide a few lines below. syncedBowlerListIndex is written by CmdSetBowler at
        // bowler SELECTION; snap.currentBowlerIndex is pushed by the batting client at ball-START. Disconnect
        // at an over boundary — the tester's exact repro, "over complete hone pe bowling side se
        // disconnection" — and the selection has happened while no ball has been bowled yet, so the snapshot
        // still names the PREVIOUS over's bowler and clobbers the correct live pick. The 06-08 log catches it:
        //     [RestoreBowlerFromAbsolute] absolute=1 -> local position 0 (was position 2 ...)
        // and the damage does not stay local: the reconnecting client then mints its ball release carrying
        // that stale identity, and RPC_SyncBallRelease's identity re-assert drags the OTHER client onto it
        // too ("BOWLER identity re-asserted: 6 -> 1") — so the two screens finish on different bowlers, and
        // the keeper depth and bowler standing position that derive from bowler type differ along with it.
        int _restoreBowlerAbs = snap.currentBowlerIndex;
        if (syncedBowlerListIndex >= 0 && BowlingScoreCard.instance != null)
        {
            int _fromSelection = BowlingScoreCard.instance.AbsoluteIndexForScorecardPosition(syncedBowlerListIndex);
            if (_fromSelection >= 0 && _fromSelection != _restoreBowlerAbs)
            {
                ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Bowler: preferring the SyncVar selection (scorecardPos={syncedBowlerListIndex} -> absolute={_fromSelection}) over the snapshot's stale {_restoreBowlerAbs}.");
                _restoreBowlerAbs = _fromSelection;
            }
        }
        // Validate before installing. _restoreBowlerAbs is an absolute PlayerList index produced by the
        // OTHER client, and no part of the snapshot carries the roster it was resolved against, so it can
        // name a player who does not bowl on this client at all (an index left over from the previous
        // innings' bowling team is the common case). Installing it anyway is what the 08-27 pair caught:
        // CurrentBowlerIndex pointed at a non-bowler, RefreshBowlerTypeFromRestoredIndex below then read an
        // empty bowlingRank and defaulted the model to left-arm fast, while RestoreBowlerFromAbsoluteIndex
        // at the end of this method correctly refused the same number and left the scorecard on the old
        // pick — so the two screens finished on different bowlers with different hands. Keep the local pick
        // when the index cannot be trusted; the next real selection resynchronises both sides.
        if (BowlingScoreCard.instance != null && !BowlingScoreCard.instance.CanTrustAbsoluteBowler(_restoreBowlerAbs))
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Bowler: absolute {_restoreBowlerAbs} does not name a bowler in this client's list — keeping the local pick {CONTROLLER.CurrentBowlerIndex}.");
        else
            CONTROLLER.CurrentBowlerIndex = _restoreBowlerAbs;
        CONTROLLER.fielderChangeIndex = snap.fielderChangeIndex;
        // Restore the per-delivery rule state the reload wiped (free hit + over-end wicket-pending).
        var _gcRestore = Singleton<GroundController>.instance;
        if (_gcRestore != null) _gcRestore.RestoreFreeHitFromSnapshot(snap.freeHitActive);
        if (gd != null) gd.RestoreWicketBallPendingFromSnapshot(snap.wicketBallPending);
        if (snap.freeHitActive || snap.wicketBallPending)
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Per-delivery rule state restored: freeHit={snap.freeHitActive} wicketBallPending={snap.wicketBallPending}.");
        var gc = Singleton<GroundController>.instance;
        // RestartBowlerSide (not a bare field set) so the bowler is actually REPOSITIONED to the restored side
        // (it sets bowlerSide + calls SetBowlerSide()). A bare assign left the side value right but the bowler
        // MODEL on the wrong end → "bowler side sometimes not synced on reconnect".
        // Reconnect side-flip fix (tester: "bowler-side reconnect ke baad batsman ki sides different"): the
        // snapshot's bowlerSide is pushed by the batting client only at innings-start / ball-START, so an
        // end-toggle between balls (or the snapshot's 'right' default) leaves it one event STALE — and this
        // apply runs LAST in the restore chain, overriding the fresher syncedBowlerSide (kept current by BOTH
        // side writers: CmdSyncBowlerSide + the UI toggle Cmd) on the RECONNECTING client only → runner/ball
        // mirrored vs the staying client. Prefer the SyncVar; the snapshot field is only a fallback.
        string _restoreSide = !string.IsNullOrEmpty(syncedBowlerSide) ? syncedBowlerSide : snap.bowlerSide;
        if (gc != null && !string.IsNullOrEmpty(_restoreSide)) gc.RestartBowlerSide(_restoreSide);
        // #7/#9/#11 (bowler type swapped for a turn on reconnect): the snapshot restored the bowler INDEX above,
        // but the local bowlerType/hand STRINGS the model + ball physics read stay stale until a later path
        // re-derives them. Re-derive immediately here so the first post-reconnect ball uses the correct type.
        if (gc != null) gc.RefreshBowlerTypeFromRestoredIndex();
        // Converge the SCORECARD onto the snapshot's ABSOLUTE bowler index. Everything else about the bowler
        // travels as a scorecard POSITION into a per-client, per-team filtered list, so the two sides can
        // resolve the same relayed number to different players and bowl different types. The snapshot's
        // absolute PlayerList index is unambiguous; map it back to this client's own position. No-op when the
        // scorecard already agrees or when the list has not been built yet (the deferred-pick path covers that).
        if (BowlingScoreCard.instance != null && _restoreBowlerAbs >= 0)
            BowlingScoreCard.instance.RestoreBowlerFromAbsoluteIndex(_restoreBowlerAbs);
    }

    /// <summary>
    /// Restores the on-strike + non-strike batsmen's individual runs/balls onto the reconnecting
    /// client's BatsmanList (team totals + indices alone left the per-player card at 0/0). Bounds-
    /// guarded; uses syncedBattingTeamIndex (falls back to the live BattingTeamIndex).
    /// </summary>
    private void RestoreBatsmanScores()
    {
        if (CONTROLLER.TeamList == null) return;
        int bt = (syncedBattingTeamIndex >= 0 && syncedBattingTeamIndex < CONTROLLER.TeamList.Length)
                 ? syncedBattingTeamIndex : CONTROLLER.BattingTeamIndex;
        if (bt < 0 || bt >= CONTROLLER.TeamList.Length) return;
        var pl = CONTROLLER.TeamList[bt].PlayerList;
        if (pl == null) return;
        if (syncedStrikerIndex >= 0 && syncedStrikerIndex < pl.Length)
        {
            pl[syncedStrikerIndex].BatsmanList.RunsScored  = syncedStrikerRuns;
            pl[syncedStrikerIndex].BatsmanList.BallsPlayed = syncedStrikerBalls;
        }
        if (syncedNonStrikerIndex >= 0 && syncedNonStrikerIndex < pl.Length)
        {
            pl[syncedNonStrikerIndex].BatsmanList.RunsScored  = syncedNonStrikerRuns;
            pl[syncedNonStrikerIndex].BatsmanList.BallsPlayed = syncedNonStrikerBalls;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RestoreBatsmanScores] striker={syncedStrikerRuns}({syncedStrikerBalls}) nonStriker={syncedNonStrikerRuns}({syncedNonStrikerBalls}) team={bt}");
    }

    /// <summary>
    /// Bowling player calls this after NewInnings() to make bowler side consistent across clients.
    /// The batting player receives the INVERTED side (left↔right mirror of the bowler's perspective).
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSyncBowlerSide(int senderId, string bowlerSide)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncBowlerSide] senderId={senderId} bowlerSide={bowlerSide}");
        syncedBowlerSide = bowlerSide;
        RpcSyncBowlerSide(senderId, bowlerSide);
    }

    [ClientRpc]
    public void RpcSyncBowlerSide(int senderId, string bowlerSide)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return; // sender already applied locally
        // Both players share the same batting-end camera (Y rotation = 0), so world-space
        // "left" is left for everyone. Apply the same side — no inversion.
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSyncBowlerSide] Applying bowlerSide={bowlerSide}");
        Singleton<GroundController>.instance?.RestartBowlerSide(bowlerSide);
    }

    // ── Reconnect-restore trigger (copied from the robust 8 Ball / Snooker netcode) ────────────
    // Cricket's reconnect restore used to be triggered ONLY by the gameStarted SyncVar HOOK
    // (OnServerReconnect). A SyncVar hook fires on VALUE CHANGE, which Mirror does NOT reliably
    // re-raise when a client reconnects (the server's value is unchanged), so a mid-match reconnect
    // could leave the player on a stale 0/0 scorecard with the restore never running → game stuck.
    // 8 Ball / Snooker reconnect is rock-solid because it uses OnStartClient — which Mirror ALWAYS
    // calls on every (re)join/spawn — as the restore entry point, gated by a "match in progress"
    // SyncVar (there: currentTurnId != -1). We copy that: OnStartClient is the reliable trigger,
    // gated on syncedCurrentInnings >= 0 (only set once play begins, so it can't fire on a fresh
    // first spawn). Both this and the legacy hook funnel through the guarded DispatchReconnectRestore
    // so the restore runs exactly once per reconnect.
    private bool _reconnectRestoreDispatched = false;
    // False Win/Lose-on-reconnect fix (#9): true from the moment a reconnect restore is dispatched
    // until ApplyReconnectState has applied the authoritative scores/indices. The IsReConnecting()
    // guard is cleared too EARLY (before scores/TargetToChase are restored), so the innings-complete
    // evaluation could fire on momentarily-reset state and spawn a bogus result. CheckForInningsComplete
    // checks this and refuses to declare the match over while it is true.
    // Backed by a timestamp so it can EXPIRE. It is cleared at the authoritative end of
    // ApplyReconnectState, but anything that throws mid-restore skips that line and then the flag blocks
    // the innings-complete evaluation for the rest of the match — a frozen game produced by a missing
    // assignment. The snapshot apply is now isolated so that specific path can't do it, but the flag must
    // not be one unguarded throw away from wedging the match, so it also stands itself down.
    private static bool _restorePending;
    private static float _restorePendingSetAt = -999f;
    private const float RESTORE_PENDING_MAX_SECONDS = 20f;
    public static bool RestorePending
    {
        get
        {
            if (_restorePending && Time.realtimeSinceStartup - _restorePendingSetAt > RESTORE_PENDING_MAX_SECONDS)
            {
                _restorePending = false;
                ConstantsData_M.MpLog($"[CricketNetworkManager] RestorePending auto-cleared after {RESTORE_PENDING_MAX_SECONDS}s — the restore never reached its end. Innings-complete evaluation is unblocked; check the log above for a throw mid-restore.");
            }
            return _restorePending;
        }
        set
        {
            _restorePending = value;
            if (value) _restorePendingSetAt = Time.realtimeSinceStartup;
        }
    }
    // Stamped when a reconnect restore runs — the stuck-panel backstops use a SHORT window right after a
    // reconnect (the panel-closing relay is known to have been missed while disconnected; 13-07: the over-
    // boundary reconnect sat frozen on the BattingScoreCard for the full 30s blanket window).
    public static float LastRestoreRealtime = -999f;

    // Set true at the END of OnStartClient — i.e. AFTER Mirror has applied the spawn SyncVar payload
    // and the reconnect determination below has run. GameData.Start gates NewGame on this (client side)
    // so NewGame can never run BEFORE we know whether this is a reconnect — which closes the race where
    // a reconnecting batting client pushed reset match-progress (Gap-C) onto the SyncVars during NewGame
    // before any reconnect flag was set, corrupting the snapshot (innings swap / target→1 / scores 0/0).
    public bool ClientStarted = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Mirror applies the spawn's SyncVar payload BEFORE OnStartClient, so syncedCurrentInnings /
        // Reconnecting already hold the server's current values here. If the match is already in
        // progress when this client spawns, this is a RECONNECT (a fresh first spawn happens before
        // NewInnings pushes syncedCurrentInnings and before the server flips Reconnecting true).
        // REVERTED (03-07 evening, tester Aqib: every disconnect→reconnect came back to a wrong scorecard +
        // stuck game, both sides): the morning change additionally required GetReconnectCount() > 0 here to
        // stop a persistent-LOCAL-server rematch from spuriously restoring the previous match's snapshot
        // (bowler type flip). But on a REAL reconnect the count is still 0 in this window (log 17:23:
        // 'Not a real reconnect (Reconnecting false or reconnectCount==0)' on every cycle, ApplyReconnectState
        // ran ZERO times, IsReConnecting() also false at SetTeamIndex) — so the count-gate blocked the ONLY
        // working restore dispatch and the reconnecting client re-ran the fresh-match flow mid-match (wrong
        // scorecard, desynced state, stuck). The original discriminator IS correct for production: each match
        // runs a fresh dedicated server, so a first spawn sees syncedCurrentInnings=-1 (log-verified at every
        // fresh match) and only a mid-match rejoin sees >= 0. The rematch-stale-bowler artifact is instead
        // covered by CmdSetBattingTeams parking syncedBowlerListIndex=-1 at the toss.
        if (Reconnecting && syncedCurrentInnings >= 0)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][OnStartClient] Reconnect detected (match in progress: innings={syncedCurrentInnings}) — dispatching restore.");
            DispatchReconnectRestore();
        }
        ClientStarted = true;
    }

    // Guarded single-shot dispatch: waits for the Ground-scene singletons, then applies the
    // authoritative reconnect state. Shared by OnStartClient (reliable trigger) and the legacy
    // gameStarted hook so they never double-apply.
    private void DispatchReconnectRestore()
    {
        if (_reconnectRestoreDispatched)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][DispatchReconnectRestore] Already dispatched this scene-load — skipping.");
            return;
        }
        _reconnectRestoreDispatched = true;
        RestorePending = true; // (#9) block innings-complete eval until ApplyReconnectState finishes
        this.DelayUntil(
            () => Singleton<GameData>.instance != null
               && Singleton<GroundController>.instance != null
               && CONTROLLER.TeamList != null,
            () => ApplyReconnectState()
        );
    }

    public void OnServerReconnect(bool oldValue, bool newValue)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][OnServerReconnect] gameStarted changed from {oldValue} to {newValue}.");
        "value changed".Show();
        this.Delay(2, () =>
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][OnServerReconnect] Delay 2s done. Reconnecting = {Reconnecting}, currentOpenPanel = {currentOpenPanel}.");
            Launcher.Instance.Reconnecting = false;
            Reconnecting.Show();
            // Fresh-match panel-burst fix (#1): the `Reconnecting` SyncVar is set true on EVERY Ground
            // load (Start, 3s after gameStarted) and NEVER cleared, so without this gate
            // OnServerReconnect ran the full reconnect-restore (ApplyReconnectState → cross-client
            // OnReconnection) on a FRESH match too → a burst of ~7-8 panel show/hide toggles at match
            // start, and the reconnect machinery permanently engaged (also destabilising real
            // reconnects). Only proceed when a REAL reconnect has actually occurred: GetReconnectCount()
            // is 0 on a fresh match and >0 only after a genuine disconnect/reconnect.
            if (!Reconnecting || Launcher.Instance == null || Launcher.Instance.GetReconnectCount() == 0)
            {
                ConstantsData_M.MpLog("[CricketNetworkManager][OnServerReconnect] Not a real reconnect (Reconnecting false or reconnectCount==0). No action taken.");
                return;
            }

            ConstantsData_M.MpLog("[CricketNetworkManager][OnServerReconnect] Reconnecting is TRUE. Dispatching restore (legacy hook path).");
            // Secondary trigger. The primary, reliable trigger is now OnStartClient (8 Ball pattern);
            // this hook path remains as a fallback and shares the same guarded dispatch so the restore
            // applies exactly once even if both fire.
            DispatchReconnectRestore();
        });
    }

    private void ApplyReconnectState()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] Singletons ready — applying authoritative SyncVar state.");
        LastRestoreRealtime = Time.realtimeSinceStartup;
        // Spot-input dead on the first post-reconnect ball (tester 14-07): Time.timeScale is GLOBAL —
        // a disconnect during a slow-mo/pause (0/0.5) survives the scene reload and the spot-move gate
        // requires timeScale >= 1. A restore must always resume at normal speed.
        if (Time.timeScale < 1f)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] timeScale was {Time.timeScale:F2} — forcing 1.");
            Time.timeScale = 1f;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] Post-reconnect fast panel-backstop armed.");
        // Phase 0 (snapshot rework): the full snapshot is NOT applied yet — just log what survived the
        // reconnect via matchSnapshotJson so we can diff it against the old SyncVar restore below. Confirms
        // the single blob carries the complete, correct state before we switch the read path in Phase 1.
        if (!string.IsNullOrEmpty(matchSnapshotJson))
        {
            MatchStateSnapshot _ms = null;
            try { _ms = JsonUtility.FromJson<MatchStateSnapshot>(matchSnapshotJson); } catch { }
            if (_ms != null)
                ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] SNAPSHOT received (Phase0, not applied): valid={_ms.valid} innings={_ms.currentInnings} bat={_ms.battingTeamIndex} bowl={_ms.bowlingTeamIndex} striker={_ms.strikerIndex} nonStriker={_ms.nonStrikerIndex} bowler={_ms.currentBowlerIndex} side={_ms.bowlerSide} ballNum={_ms.currentBallNumber} ballCount={_ms.ballCount} teams={(_ms.teams != null ? _ms.teams.Length : 0)} jsonLen={matchSnapshotJson.Length}");
            else
                ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] SNAPSHOT parse FAILED (Phase0) jsonLen={matchSnapshotJson.Length}");
        }
        else
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] SNAPSHOT empty (Phase0) — no push happened before this reconnect.");
        }

        // Apply authoritative SyncVar state for ALL panel cases — AutoSave may have stale data
        if (syncedBallNumber >= 0)
        {
            // Innings/over-boundary reconnect fix: syncedBallCount is "balls in the CURRENT over" and is
            // only ever 0-5. A reconnect right at an innings/over change can restore STALE within-over
            // counters from the previous over/innings that were never reset (observed: ballCount=7,
            // ballNumber=5) → CheckForOverComplete's `currentBallNumber == 5` branch then fires forever
            // (the over can never "complete" from a corrupt count) → the bowler loops auto-bowling /
            // CheckForOverComplete and the game hangs for minutes. When either within-over counter is out
            // of the valid 0-5 range, the snapshot is stale → restore a FRESH over (0/0) instead.
            bool _overCountersValid = syncedBallCount >= 0 && syncedBallCount <= 5
                                      && syncedBallNumber >= 0 && syncedBallNumber <= 5;
            if (Singleton<GameData>.instance != null)
                Singleton<GameData>.instance.currentBallNumber = _overCountersValid ? syncedBallNumber : 0;
            if (Singleton<ScoreBoardBallList>.instance != null)
                Singleton<ScoreBoardBallList>.instance.ballCount = _overCountersValid ? syncedBallCount : 0;
            if (CONTROLLER.TeamList != null)
            {
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores = syncedMatchScores;
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets = syncedMatchWickets;
                if (syncedMatchBalls >= 0)
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls = syncedMatchBalls;
            }
            // Restore bowler side — both players use the same world-space value (no inversion).
            if (!string.IsNullOrEmpty(syncedBowlerSide) && Singleton<GroundController>.instance != null)
            {
                Singleton<GroundController>.instance.RestartBowlerSide(syncedBowlerSide);
                ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Restored bowlerSide={syncedBowlerSide}");
            }
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Applied SyncVars: scores={syncedMatchScores} wickets={syncedMatchWickets} balls={syncedBallCount} ballNum={syncedBallNumber}");
        }

        // ── Reconnect-safe FULL match-progress restore ─────────────────────────
        // Must run BEFORE the Display-string and TargetToChase blocks below. The single-team
        // restore above writes TeamList[BattingTeamIndex], but BattingTeamIndex/BowlingTeamIndex
        // are frozen at their first-innings (toss) values — never updated at the innings change —
        // so on a SECOND-innings reconnect the live score lands on the WRONG team (actual batting
        // team → 0) and the first-innings total (the target) is lost. Here we (a) restore the
        // correct live indices and innings, and (b) rebuild BOTH teams' totals by absolute index,
        // overriding any mis-targeted write from the single-team block above.
        if (syncedCurrentInnings >= 0 && CONTROLLER.TeamList != null && CONTROLLER.TeamList.Length >= 2)
        {
            if (syncedBattingTeamIndex >= 0 && syncedBowlingTeamIndex >= 0)
            {
                CONTROLLER.BattingTeamIndex = syncedBattingTeamIndex;
                CONTROLLER.BowlingTeamIndex = syncedBowlingTeamIndex;
            }
            CONTROLLER.currentInnings = syncedCurrentInnings;

            // t0 = batting team, t1 = bowling team — written back by ACTUAL index (teams sit at e.g. 4/9,
            // NOT 0/1). The old code wrote TeamList[0]/[1] (empty teams) so the real bowling team's
            // first-innings total was never restored → TargetToChase = 0+1 = 1 on a 2nd-innings reconnect.
            if (syncedTeam0Balls >= 0 && syncedBattingTeamIndex >= 0 && syncedBattingTeamIndex < CONTROLLER.TeamList.Length)
            {
                // Score-ZERO-on-reconnect fix: the per-team snapshot (syncedTeam0*) can be STALE/0 if the
                // batting authority disconnected mid-delivery before its CmdSyncMatchProgress pushed the
                // latest — while the single-team SyncVar (syncedMatchScores, restored just above) holds the
                // correct live total. A raw assign here clobbered the batting team back to 0 (display 0/0).
                // Take the MAX so a stale per-team value can never LOWER the authoritative live score
                // (scores/wickets/balls only increase within an innings). Bowling team (target) keeps its.
                CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchScores  = Mathf.Max(CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchScores, syncedTeam0Scores);
                CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchWickets = Mathf.Max(CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchWickets, syncedTeam0Wickets);
                CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchBalls   = Mathf.Max(CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchBalls, syncedTeam0Balls);
            }
            if (syncedTeam1Balls >= 0 && syncedBowlingTeamIndex >= 0 && syncedBowlingTeamIndex < CONTROLLER.TeamList.Length)
            {
                CONTROLLER.TeamList[syncedBowlingTeamIndex].currentMatchScores  = syncedTeam1Scores;
                CONTROLLER.TeamList[syncedBowlingTeamIndex].currentMatchWickets = syncedTeam1Wickets;
                CONTROLLER.TeamList[syncedBowlingTeamIndex].currentMatchBalls   = syncedTeam1Balls;
            }
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Match-progress restored: " +
                      $"innings={syncedCurrentInnings} bat={syncedBattingTeamIndex} bowl={syncedBowlingTeamIndex} " +
                      $"T0={syncedTeam0Scores}/{syncedTeam0Wickets} T1={syncedTeam1Scores}/{syncedTeam1Wickets}");
        }

        // ── Restore batsman indices ────────────────────────────────────────────
        if (syncedBallNumber >= 0)
        {
            CONTROLLER.StrikerIndex = syncedStrikerIndex;
            CONTROLLER.NonStrikerIndex = syncedNonStrikerIndex;
            if (Singleton<GameData>.instance != null)
                Singleton<GameData>.instance.newBatsmanEntryIndex = syncedBatsmanEntryIndex;
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Batsman indices restored: striker={syncedStrikerIndex} nonStriker={syncedNonStrikerIndex} entry={syncedBatsmanEntryIndex}");
            RestoreBatsmanScores();
        }

        // ── Phase 1: authoritative MatchStateSnapshot restore ──────────────────
        // When the batting client has pushed a VALID full snapshot, it is the single source of truth: OVERWRITE
        // everything the scattered SyncVar restore above set (team roles, innings, BOTH teams' scores+extras+DRS
        // +ballHistory, EVERY batsman & bowler record, indices, bowler index/side) with one atomic deterministic
        // apply — killing the team-swap / score-0-0 / wrong-bowler races at the source. FALLBACK: empty/invalid
        // blob (old server, or before the first push) → ApplyMatchStateSnapshot no-ops and the restore above
        // stands (zero regression). Runs BEFORE the display/Target/Scoreboard refresh below so they read the
        // snapshot values. Kill-switchable via ConstantsData_M.useMatchSnapshotRestore.
        if (ConstantsData_M.useMatchSnapshotRestore)
        {
            MatchStateSnapshot _applySnap = null;
            if (!string.IsNullOrEmpty(matchSnapshotJson))
                try { _applySnap = JsonUtility.FromJson<MatchStateSnapshot>(matchSnapshotJson); } catch { }
            if (_applySnap != null && _applySnap.valid)
            {
                // ISOLATED: everything after this call — the panel switch, OnReconnection, the
                // opponent notify, CancelInFlightDeliveryForReconnect, and the RestorePending=false
                // that unblocks innings-complete — is the actual restore. A throw inside the snapshot
                // apply used to abort all of it and leave RestorePending stuck TRUE forever, which is a
                // permanently frozen match (29-07: FormatException from SetGameDatas' bowlingRank
                // int.Parse, reached via RefreshBowlerTypeFromRestoredIndex at the tail of the apply).
                // The root is fixed, but the restore must not be one throw away from a freeze again.
                try
                {
                    ApplyMatchStateSnapshot(_applySnap);
                }
                catch (System.Exception _snapEx)
                {
                    ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Snapshot apply THREW ({_snapEx.GetType().Name}: {_snapEx.Message}) — continuing the restore with the SyncVar state already applied.");
                }
                ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Phase1: APPLIED snapshot (overrode SyncVar restore) innings={_applySnap.currentInnings} bat={CONTROLLER.BattingTeamIndex} bowl={CONTROLLER.BowlingTeamIndex} striker={CONTROLLER.StrikerIndex} bowler={CONTROLLER.CurrentBowlerIndex} side={(Singleton<GroundController>.instance != null ? Singleton<GroundController>.instance.bowlerSide : "?")}");
            }
            else
            {
                // The big string SyncVar didn't arrive on spawn → explicitly request the server's cached snapshot
                // (applied asynchronously via TargetReceiveMatchSnapshot a moment later, overriding the legacy restore).
                ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] snapshot blob empty on spawn — requesting it from the server (TargetRpc).");
                CmdRequestMatchSnapshot();
            }
        }

        // ── Team-data & Scoreboard UI refresh ──────────────────────────────────
        if (Singleton<GameData>.instance != null && CONTROLLER.TeamList != null)
        {
            Singleton<GameData>.instance.scoreDisplayString =
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" +
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
            Singleton<GameData>.instance.oversDisplayString =
                Singleton<GameData>.instance.GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
            ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] Display strings refreshed: " +
                      $"score={Singleton<GameData>.instance.scoreDisplayString} " +
                      $"overs={Singleton<GameData>.instance.oversDisplayString}");
        }

        // TargetToChase is not persisted by AutoSave — derive from bowling team's first-innings total.
        if (CONTROLLER.currentInnings == 1 && CONTROLLER.PlayModeSelected != 7 && CONTROLLER.TeamList != null)
        {
            int expectedTarget = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores + 1;
            if (CONTROLLER.TargetToChase != expectedTarget)
            {
                CONTROLLER.TargetToChase = expectedTarget;
                ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] TargetToChase corrected → {CONTROLLER.TargetToChase}");
            }
        }

        // Force Scoreboard UI redraw.
        if (Singleton<Scoreboard>.instance != null)
        {
            Singleton<Scoreboard>.instance.UpdateScoreCard();
            if (CONTROLLER.currentInnings == 1 && CONTROLLER.TargetToChase > 0)
                Singleton<Scoreboard>.instance.ShowTargetScreen(true);
            ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] Scoreboard UI refreshed.");
        }

        switch (currentOpenPanel)
        {
            case PanelsInfo.BattingScoreCard:
                ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] Case: BattingScoreCard.");
                "Batting Score Card Open".Show();
                GameData.instance?.OnReconnection(true); // authoritative local reconnect restore — must bypass debounce
                CmdNotifyOtherPlayerIAmReconnected(staticVariables.UserProfiledata?.user?._id ?? 0);
                break;
            case PanelsInfo.BowlingScoreCard:
                ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] Case: BowlingScoreCard.");
                "Bowling Score Card Open".Show();
                GameData.instance?.OnReconnection(true); // authoritative local reconnect restore — must bypass debounce
                CmdNotifyOtherPlayerIAmReconnected(staticVariables.UserProfiledata?.user?._id ?? 0);
                break;
            case PanelsInfo.AfterOverSummary:
                ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] Case: AfterOverSummary.");
                GameData.instance?.setOversAfterSession();
                "After Over Summary Open".Show();
                // Over-end reconnect: notify the staying peer (cancel its in-flight ball, run its OnReconnection,
                // re-broadcast the bowler) AND run our own restore — same as the BattingScoreCard/BowlingScoreCard
                // cases. Before the currentOpenPanel enum was corrected, an over-end reconnect mislabeled itself as
                // BowlingScoreCard and got this notify by accident; now AfterOverSummary is cased correctly so it
                // must do the notify itself or the staying-peer bowler is left un-armed.
                GameData.instance?.OnReconnection(true);
                CmdNotifyOtherPlayerIAmReconnected(staticVariables.UserProfiledata?.user?._id ?? 0);
                break;
            case PanelsInfo.None:
                ConstantsData_M.MpLog("[CricketNetworkManager][ApplyReconnectState] Case: None.");
                Launcher.Instance.Reconnecting = false;
                // Mid-delivery disconnect fix (#6): the LOCAL reconnecting player cancels any in-flight
                // ball (scene reload usually cleared it, but be explicit) so the over re-bowls cleanly.
                Singleton<GroundController>.instance?.CancelInFlightDeliveryForReconnect();
                BattingScoreCard.instance?.Hide(true);
                BowlingScoreCard.instance?.Hide(true);
                Launcher.Instance.OnPlayerEnteredRoom();
                // Ball-stuck-at-release-point root (tester "most cases", + "bowler-side indicator stuck /
                // ball resting near the batsman / phantom score for that ball"): this NO-PANEL case is the
                // COMMON gameplay disconnect (before/at a ball, no scorecard showing). Every OTHER case above
                // runs OnReconnection + CmdNotifyOtherPlayerIAmReconnected IMMEDIATELY; only this one deferred
                // BOTH behind a 3s Delay. In that window the STAYING peer (bowler) is never told to cancel its
                // in-flight ball — its indicator/arc stays stuck and the frozen ball can be committed by a late
                // RpcBallOutcome (phantom count) — and the reconnecting player's own authoritative re-home
                // waited 3s too, so the ball sat at the release point / wherever the last stream left it. Run
                // both NOW, exactly like the scorecard/over cases; the local CancelInFlightDeliveryForReconnect
                // above already proves the GroundController + scene are ready, so no settle delay is needed.
                GameData.instance?.OnReconnection(true); // authoritative local reconnect restore — must bypass debounce
                CmdNotifyOtherPlayerIAmReconnected(staticVariables.UserProfiledata?.user?._id ?? 0);
                "No Panel Open".Show();
                break;
            default:
                ConstantsData_M.MpLog($"[CricketNetworkManager][ApplyReconnectState] DEFAULT (unhandled panel = {currentOpenPanel}).");
                break;
        }
        // (#9) Authoritative scores/indices/TargetToChase were applied above (before the switch), so it
        // is now safe to let the innings-complete evaluation run again.
        RestorePending = false;
        // ROOT reconnect-flag fix: clear the Launcher reconnect flag HERE (the authoritative end of the
        // restore) for ALL panel cases, regardless of OnStartClient/OnJoinedRoom ordering — otherwise a
        // Reconnecting=true set by a late OnJoinedRoom would stick and keep IsReConnecting() true forever.
        if (Launcher.Instance != null) Launcher.Instance.Reconnecting = false;
    }
    /// <summary>
    /// Race-proof reconnect restore. NewGame() runs (and ZEROES the match) on every MP
    /// scene-load, including reconnect, and on some devices it wins the race against the
    /// 2s-delayed ApplyReconnectState path — so the reconnecting player ends up at 0/0.
    /// GameData.NewGame calls this at its very END: if authoritative match-progress SyncVars
    /// exist (i.e. a mid-match reconnect), they are re-applied AFTER the zero, so it never
    /// sticks. On a FRESH match nothing has been pushed yet (SyncVars at their -1 defaults),
    /// so this is a no-op and returns false. NewGame is the ONLY caller of itself per scene
    /// load (innings change uses NewInnings, not NewGame), so this never fires mid-innings.
    /// Restores DATA only; the caller refreshes the scoreboard UI.
    /// </summary>
    public bool RestoreMatchStateFromSyncVars()
    {
        if (CONTROLLER.TeamList == null || CONTROLLER.TeamList.Length < 2) return false;
        // Nothing pushed yet → fresh match start, leave the zeroed state alone.
        if (syncedCurrentInnings < 0 && syncedBallNumber < 0) return false;

        // Bug-fix S1 (score / balls-faced / target reset to 0 after reconnect): the innings/index SyncVar
        // (syncedCurrentInnings >= 0) can arrive BEFORE the batting client has re-pushed the per-ball score/over
        // SyncVars (syncedTeam0Balls / syncedBallNumber still -1). The guard above only bails when BOTH are < 0,
        // so this used to PROCEED on that partial state, apply ZEROS (every score/over/striker block below is
        // gated on syncedTeam0Balls>=0 or syncedBallNumber>=0, so all are skipped), and return TRUE — which
        // SUPPRESSED GameData.NewGame's retry coroutine (it only retries when this returns FALSE). The zeroed
        // score/target/balls-faced then stuck until a second scene-reload. Treat "indices present but score/over
        // not yet pushed" as not-ready and return false so the retry keeps polling until the full payload lands.
        // Fresh match (both -1) already returned above; normal mid-match (either >= 0) is unchanged. Do NOT
        // loosen the AND-guard above to an OR — that would restore garbage on a fresh match.
        if (syncedTeam0Balls < 0 && syncedBallNumber < 0) return false;

        // Live innings + batting/bowling indices (toss-frozen indices would mis-assign otherwise).
        if (syncedCurrentInnings >= 0)
        {
            if (syncedBattingTeamIndex >= 0 && syncedBowlingTeamIndex >= 0)
            {
                CONTROLLER.BattingTeamIndex = syncedBattingTeamIndex;
                CONTROLLER.BowlingTeamIndex = syncedBowlingTeamIndex;
            }
            CONTROLLER.currentInnings = syncedCurrentInnings;
        }

        // t0 = batting team, t1 = bowling team — written by ACTUAL index (teams sit at e.g. 4/9, NOT
        // 0/1). Writing TeamList[0]/[1] left the real teams unrestored → bowling team 0 → target=1.
        if (syncedTeam0Balls >= 0 && syncedBattingTeamIndex >= 0 && syncedBattingTeamIndex < CONTROLLER.TeamList.Length)
        {
            // Score-ZERO-on-reconnect fix (see ApplyReconnectState): MAX so a stale/0 per-team snapshot
            // can't clobber the authoritative live batting-team total. Bowling team (target) keeps its.
            CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchScores  = Mathf.Max(CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchScores, syncedTeam0Scores);
            CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchWickets = Mathf.Max(CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchWickets, syncedTeam0Wickets);
            CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchBalls   = Mathf.Max(CONTROLLER.TeamList[syncedBattingTeamIndex].currentMatchBalls, syncedTeam0Balls);
        }
        if (syncedTeam1Balls >= 0 && syncedBowlingTeamIndex >= 0 && syncedBowlingTeamIndex < CONTROLLER.TeamList.Length)
        {
            CONTROLLER.TeamList[syncedBowlingTeamIndex].currentMatchScores  = syncedTeam1Scores;
            CONTROLLER.TeamList[syncedBowlingTeamIndex].currentMatchWickets = syncedTeam1Wickets;
            CONTROLLER.TeamList[syncedBowlingTeamIndex].currentMatchBalls   = syncedTeam1Balls;
        }

        // Fallback: per-team push not received yet but single-team push was — restore batting side.
        if (syncedTeam0Balls < 0 && syncedTeam1Balls < 0 && syncedBallNumber >= 0)
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores  = syncedMatchScores;
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets = syncedMatchWickets;
            if (syncedMatchBalls >= 0)
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls = syncedMatchBalls;
        }

        // Over-progress + striker indices.
        if (syncedBallNumber >= 0)
        {
            // Innings/over-boundary reconnect fix: syncedBallCount is "balls in the CURRENT over" and is
            // only ever 0-5. A reconnect right at an innings/over change can restore STALE within-over
            // counters from the previous over/innings that were never reset (observed: ballCount=7,
            // ballNumber=5) → CheckForOverComplete's `currentBallNumber == 5` branch then fires forever
            // (the over can never "complete" from a corrupt count) → the bowler loops auto-bowling /
            // CheckForOverComplete and the game hangs for minutes. When either within-over counter is out
            // of the valid 0-5 range, the snapshot is stale → restore a FRESH over (0/0) instead.
            bool _overCountersValid = syncedBallCount >= 0 && syncedBallCount <= 5
                                      && syncedBallNumber >= 0 && syncedBallNumber <= 5;
            if (Singleton<GameData>.instance != null)
                Singleton<GameData>.instance.currentBallNumber = _overCountersValid ? syncedBallNumber : 0;
            if (Singleton<ScoreBoardBallList>.instance != null)
                Singleton<ScoreBoardBallList>.instance.ballCount = _overCountersValid ? syncedBallCount : 0;
            CONTROLLER.StrikerIndex    = syncedStrikerIndex;
            CONTROLLER.NonStrikerIndex = syncedNonStrikerIndex;
            if (Singleton<GameData>.instance != null)
                Singleton<GameData>.instance.newBatsmanEntryIndex = syncedBatsmanEntryIndex;
            RestoreBatsmanScores();
        }

        // Restore bowler side here too (reconnect L/R-flip fix): ApplyReconnectState restores it
        // only ~2s later, so without this the forced "left" from GroundController.NewInnings (run
        // earlier in the same NewGame) persists until then → the momentary left/right bowler-side
        // flip on reconnect. Same call + no-inversion rule ApplyReconnectState uses.
        if (!string.IsNullOrEmpty(syncedBowlerSide) && Singleton<GroundController>.instance != null)
            Singleton<GroundController>.instance.RestartBowlerSide(syncedBowlerSide);

        // 2nd-innings target derives from the bowling team's first-innings total.
        if (CONTROLLER.currentInnings == 1 && CONTROLLER.PlayModeSelected != 7)
            CONTROLLER.TargetToChase = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores + 1;

        ConstantsData_M.MpLog($"[CricketNetworkManager][RestoreMatchStateFromSyncVars] Applied: innings={syncedCurrentInnings} " +
                  $"bat={CONTROLLER.BattingTeamIndex} bowl={CONTROLLER.BowlingTeamIndex} " +
                  $"T0={syncedTeam0Scores}/{syncedTeam0Wickets} T1={syncedTeam1Scores}/{syncedTeam1Wickets} " +
                  $"ballNum={syncedBallNumber} ballCount={syncedBallCount} striker={syncedStrikerIndex}");
        return true;
    }

    // Update is called once per frame
    void Update()
    {

    }
    [Command(requiresAuthority = false)]
    public void CmdNotifyOtherPlayerIAmReconnected(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdNotifyOtherPlayerIAmReconnected] CMD received. senderId = {senderId}.");
        MatchFlow.Log("Cricket", $"{FlowWho(senderId)} reconnected" + (syncedCurrentInnings >= 0 ? $" ({FlowScoreSummary()})" : ""));
        RpcNotifyOtherPlayerIAmReconnected(senderId);
    }
    [ClientRpc]
    public void RpcNotifyOtherPlayerIAmReconnected(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcNotifyOtherPlayerIAmReconnected] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcNotifyOtherPlayerIAmReconnected] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcNotifyOtherPlayerIAmReconnected] Calling GameData.instance.OnReconnection().");
        // Mid-delivery disconnect fix (#6) — load-bearing: the STAYING (connected) player is frozen
        // with the opponent's half-flown ball and a partial ACK barrier that NO reconnect path ever
        // resets. Cancel it here so the interrupted ball re-bowls cleanly on this client too.
        if (CONTROLLER.PlayModeSelected == 8)
            Singleton<GroundController>.instance?.CancelInFlightDeliveryForReconnect();
        GameData.instance.OnReconnection();

        // Spin-sign fix (#1): CONTROLLER.CurrentBowlerIndex is NOT part of the reconnect SyncVar
        // snapshot, and RpcSetBowler is a non-buffered ClientRpc — so a reconnected BATTING player
        // resumes with a STALE bowler index → wrong BowlerType/Hand → wrong spin SIGN (spinFactor) →
        // the ball drifts to the wrong side of the bat and well-timed shots miss for the whole over
        // (also mis-places the keeper, feeding the phantom-FOUR bug). The BOWLING player (the
        // authority on the current bowler) re-broadcasts it through the existing CmdSetBowler chain
        // so the reconnected batting player re-stamps CurrentBowlerIndex before the next delivery.
        if (CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && BowlingScoreCard.currentBowler >= 0)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][RpcNotifyOtherPlayerIAmReconnected] Re-broadcasting bowler {BowlingScoreCard.currentBowler} to resync reconnected batter's spin.");
            CmdSetBowler(BowlingScoreCard.currentBowler);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdCheckForOverComplete()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][CmdCheckForOverComplete] CMD received.");
        RpcCheckForOverComplete();
    }
    [ClientRpc]
    public void RpcCheckForOverComplete()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcCheckForOverComplete] RPC received. Calling CheckForOverComplete.");
        GameData.instance.CheckForOverComplete();
    }
    [Command(requiresAuthority = false)]
    public void CmdChangeCurrentOpenPanel(int panelIndex)
    {
        currentOpenPanel = (PanelsInfo)panelIndex;
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdChangeCurrentOpenPanel] CMD received. panelIndex = {currentOpenPanel.ToString()}.");

    }


    private void OnEnable()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][OnEnable] Subscribing to events.");
        MirrorNetwork.OnWinCall += AnnounceVictory;
        MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
    }

    private void OnDisable()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][OnDisable] Unsubscribing from events.");
        MirrorNetwork.OnWinCall -= AnnounceVictory;
        MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
    }

    public override void OnStartServer()
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][OnStartServer] Server build stamp {BuildStamp.Timestamp}.");
        ConstantsData_M.MpLog("[CricketNetworkManager][OnStartServer] Subscribing to disconnect event.");
        NetworkServer.OnDisconnectedEvent += OnClientDisconnectedFromServer;
    }

    public override void OnStopServer()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][OnStopServer] Unsubscribing from disconnect event.");
        NetworkServer.OnDisconnectedEvent -= OnClientDisconnectedFromServer;
    }

    /// <summary>
    /// Called server-side when any client disconnects.
    /// If we are in the pre-game flow (TeamSelection or Toss), starts an 8-second timer.
    /// If the player does not reconnect within 8 seconds the game auto-progresses.
    /// </summary>
    private void OnClientDisconnectedFromServer(NetworkConnectionToClient conn)
    {
        MatchFlow.Log("Cricket", (conn != null && conn.identity != null && conn.identity.GetComponent<MirrorPlayerPrefab>() != null
                ? MatchFlow.Who(conn.identity.GetComponent<MirrorPlayerPrefab>().playerId)
                : "a player")
            + (gameStarted ? " disconnected" + (syncedCurrentInnings >= 0 ? $" ({FlowScoreSummary()})" : "") : $" disconnected before the match (panel {currentOpenPanel})"));
        // In-game disconnects are handled by the shared NetworkGameManager.ShouldPauseGame() system.
        if (gameStarted) return;
        // Only act if we are past the initial waiting-for-opponent stage
        if (currentOpenPanel <= PanelsInfo.WaitingForOpponent) return;

        ConstantsData_M.MpLog($"[CricketNetworkManager][OnClientDisconnectedFromServer] Pre-game disconnect detected (panel={currentOpenPanel}). Starting 8s auto-progress timer.");
        if (_disconnectTimerCoroutine != null) StopCoroutine(_disconnectTimerCoroutine);
        _disconnectTimerCoroutine = StartCoroutine(DisconnectAutoProgressTimer());
    }

    /// <summary>
    /// Waits 8 seconds. If no reconnect arrives in that window, auto-progresses the pre-game.
    /// </summary>
    private IEnumerator DisconnectAutoProgressTimer()
    {
        yield return new WaitForSeconds(8f);
        _disconnectTimerCoroutine = null;
        if (!gameStarted && currentOpenPanel > PanelsInfo.WaitingForOpponent)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][DisconnectAutoProgressTimer] 8s elapsed, no reconnect — auto-progressing pre-game.");
            MatchFlow.Log("Cricket", $"no reconnect within 8s — auto-progressing pre-game (panel {currentOpenPanel})");
            StartCoroutine(AutoProgressPreGame());
        }
    }

    // Max time to hold a disconnect-victory while THIS client is still restoring. Long enough for a
    // reconnect to finish, short enough that a genuine walk-off still resolves promptly.
    private const float ANNOUNCE_VICTORY_RESTORE_GRACE = 20f;
    private Coroutine _deferredVictoryCo;

    public void AnnounceVictory(string reason)
    {
        // Do NOT declare a disconnect-win while this client is mid-restore. When BOTH players drop at the
        // same moment — the tester's "innings ki last ball pe dono sides se disconnection" — each side sees
        // the other as gone, and the first one to reach here ends a match that was actually recovering: the
        // 13-08 log has AnnounceVictory('Opponent has disconnected. You are declared the winner.') one line
        // after SetTeamIndex restorePending=True, on a live 17/4 at 2.5 overs. The result screen that
        // followed is what got reported as "new game start".
        // This only DELAYS the call while a restore is in flight, and stands it down only if the opponent is
        // demonstrably back — you cannot win by disconnect against a player who is connected. A real
        // walk-off still resolves once the grace expires.
        bool _restoring = RestorePending || (Launcher.Instance != null && Launcher.Instance.IsReConnecting());
        if (_restoring && _deferredVictoryCo == null && gameObject.activeInHierarchy)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][AnnounceVictory] HELD ('{reason}') — a restore is in flight (restorePending={RestorePending}); waiting before ending the match.");
            _deferredVictoryCo = StartCoroutine(AnnounceVictoryAfterRestore(reason));
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][AnnounceVictory] Called with reason = '{reason}'. Sending CMD.");
        CmdAnnounceVictory(staticVariables.UserProfiledata.user._id, reason);
    }

    private IEnumerator AnnounceVictoryAfterRestore(string reason)
    {
        float _waited = 0f;
        while (_waited < ANNOUNCE_VICTORY_RESTORE_GRACE
            && (RestorePending || (Launcher.Instance != null && Launcher.Instance.IsReConnecting())))
        {
            _waited += Time.unscaledDeltaTime;
            yield return null;
        }
        _deferredVictoryCo = null;
        int _players = NetworkGameManager.Instance != null ? NetworkGameManager.Instance.currentPlayerCount : -1;
        if (_players >= 2)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][AnnounceVictory] STOOD DOWN after {_waited:F1}s — both players are connected again (players={_players}); the match continues.");
            yield break;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][AnnounceVictory] Proceeding after {_waited:F1}s — opponent still absent (players={_players}).");
        CmdAnnounceVictory(staticVariables.UserProfiledata.user._id, reason);
    }
    [Command(requiresAuthority = false)]
    public void CmdAnnounceVictory(int playerId, string reaosn)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdAnnounceVictory] CMD received. playerId = {playerId}, reason = '{reaosn}'.");
        MatchFlow.Log("Cricket", $"{FlowWho(playerId)} declared winner — {(string.IsNullOrEmpty(reaosn) ? "opponent disconnected" : reaosn)}");
        MatchFlow.SendResult(playerId.ToString(), "opponent disconnected", FlowScoreSummary());
        ApiAndRoomManager._instance.WinnerLossChallenge(playerId.ToString());
        RpcAnnounceVictory(playerId);
        NetworkGameManager.Instance.creatorData.Scores = 0;
        NetworkGameManager.Instance.joinerData.Scores = 0;
    }
    // Match-end desync fix (tester: "end mein ek side waiting panel, doosri side win/loss"): whichever path
    // delivers the result — the victory RPC (winner) or DirectResult (loser, via the API session-status on
    // disconnect) — must also clear any lingering "Waiting for Opponent" panel, or that side sits on the
    // waiting screen while the other shows win/loss. The match is decided here, so there is nothing left to
    // wait for. Null-guarded because GameData may already be torn down at match end.
    private void HideWaitingPanelsForResult()
    {
        GameData gd = Singleton<GameData>.instance;
        if (gd == null) return;
        if (gd.waitingForOpponentPanel != null) gd.waitingForOpponentPanel.SetActive(false);
        if (gd.waitingForOpponentPanel1 != null) gd.waitingForOpponentPanel1.SetActive(false);
    }

    [ClientRpc]
    public void RpcAnnounceVictory(int playerId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcAnnounceVictory] RPC received. playerId = {playerId}.");
        HideWaitingPanelsForResult();
        if (ResultManager.GameSpawnedFinished == false && ResultManager.TryCommitResultLatch())
        {
            ResultManager.GameSpawnedFinished = true;
            GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
            if (prefab != null)
                Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, playerId.ToString());
            else
                Debug.LogError("WinLose GameManager prefab not found!");
        }
    }

    // public void DirectResult(bool result)
    // {
    //     ConstantsData_M.MpLog($"[CricketNetworkManager][DirectResult] Called with result = {result}.");
    //     HideWaitingPanelsForResult();
    //     if (ResultManager.GameSpawnedFinished == false && ResultManager.TryCommitResultLatch())
    //     {
    //         ResultManager.GameSpawnedFinished = true;
    //         GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
    //         if (prefab != null)
    //             Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<ResultManager>().HandleGameResultAltMultiplayer(result, staticVariables.UserProfiledata.user._id.ToString());
    //         else
    //             Debug.LogError("WinLose GameManager prefab not found!");
    //     }
    // }
    public void DirectResult(bool result)
        {
            if (result)
            {
                // Victory(staticVariables.UserProfiledata.user._id, "Opponent has disconnected. You are declared the winner.");
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }
            else
            {
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(false, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }

        }
    [Command(requiresAuthority = false)]
    public void CmdSelectTeam(NetworkConnectionToClient sender = null)
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][CmdSelectTeam] CMD received.");
        // If the panel was ALREADY at TeamSelection when this arrives, the sender is a late-joiner
        // who missed the previous RpcSelectTeam broadcast.  Send them an extra targeted RPC so
        // their local SELECTTEAM counter catches up to 2 and they advance to the squad page.
        bool wasAlreadyTeamSelection = (currentOpenPanel == PanelsInfo.TeamSelection);
        currentOpenPanel = PanelsInfo.TeamSelection;
        RpcSelectTeam();
        if (wasAlreadyTeamSelection && sender != null)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][CmdSelectTeam] Late-joiner detected — sending catch-up TargetRpc.");
            TargetCatchUpSelectTeam(sender);
        }
    }
    [ClientRpc]
    public void RpcSelectTeam()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcSelectTeam] RPC received. Calling RPC_SelectTeams.");
        if (RoomMenu.Instance != null)
            RoomMenu.Instance.RPC_SelectTeams();
        else
            // Scene still loading — retry once it's ready (handles late-loading clients)
            this.DelayUntil(() => RoomMenu.Instance != null, () => RoomMenu.Instance.RPC_SelectTeams());
    }
    /// <summary>
    /// Sent ONLY to a late-joining client whose scene loaded after the first RpcSelectTeam was
    /// already broadcast.  The extra invocation brings their SELECTTEAM counter to 2 so they
    /// advance to the squad-selection page alongside the other player.
    /// </summary>
    [TargetRpc]
    private void TargetCatchUpSelectTeam(NetworkConnectionToClient conn)
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][TargetCatchUpSelectTeam] Catching up SELECTTEAM for late-joining client.");
        if (RoomMenu.Instance != null)
            RoomMenu.Instance.RPC_SelectTeams();
        else
            this.DelayUntil(() => RoomMenu.Instance != null, () => RoomMenu.Instance.RPC_SelectTeams());
    }

    [Command(requiresAuthority = false)]
    public void CmdOnOpponentConnected(NetworkConnectionToClient sender = null)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdOnOpponentConnected] CMD received. currentOpenPanel={currentOpenPanel}, gameStarted={gameStarted}");

        // Mirror fires a reconnect during scene transitions (MainMenu→Ground).
        // Once gameStarted=true we are in the Ground scene — ignore these false reconnects.
        if (gameStarted)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][CmdOnOpponentConnected] gameStarted=true — scene-change reconnect, ignoring.");
            return;
        }

        // Player reconnected before the 8-second auto-progress timer expired — cancel it.
        if (_disconnectTimerCoroutine != null)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][CmdOnOpponentConnected] Cancelling pending disconnect timer (player reconnected in time).");
            StopCoroutine(_disconnectTimerCoroutine);
            _disconnectTimerCoroutine = null;
        }

        if (currentOpenPanel > PanelsInfo.WaitingForOpponent)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][CmdOnOpponentConnected] Reconnect mid-flow — panel={currentOpenPanel}. Restoring panel for reconnecting client.");
            MatchFlow.Log("Cricket", $"a player rejoined before the match (panel {currentOpenPanel})");
            // Restore the current panel for the reconnecting client only.
            // Do NOT auto-progress here — the 8-second timer handles that if the player drops again.
            if (sender != null)
                TargetSyncPreGamePanel(sender, currentOpenPanel);
            return;
        }

        currentOpenPanel = PanelsInfo.WaitingForOpponent;
        RpcOnOpponentConnected();
#if !UNITY_SERVER
        if (!NetworkClient.active) RoomMenu.Instance?.RPC_OnOpponentConnected();
#endif
    }

    /// <summary>
    /// Called ONLY on the reconnecting client to give brief visual feedback before auto-progress fires.
    /// </summary>
    [TargetRpc]
    private void TargetSyncPreGamePanel(NetworkConnectionToClient conn, PanelsInfo panel)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][TargetSyncPreGamePanel] Restoring panel={panel} briefly (auto-progress will fire in 2s).");
        MultiplayerPanel.Instance?.CloseMenu("Room");
        switch (panel)
        {
            case PanelsInfo.TeamSelection:
                SquadPageTWO.instance?.SetSquadPage();
                break;
            case PanelsInfo.Toss:
                TossPageTWO.instance?.showMe();
                break;
            default:
                RoomMenu.Instance?.RPC_OnOpponentConnected();
                break;
        }
    }

    /// <summary>
    /// Server-only coroutine: auto-completes pre-game panels (TeamSelection → Toss → Ground)
    /// so neither player gets stuck after a reconnect.
    /// </summary>
    private IEnumerator AutoProgressPreGame()
    {
        // Wait for the reconnecting client to finish loading the scene
        yield return new WaitForSeconds(2f);

        if (currentOpenPanel == PanelsInfo.TeamSelection)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][AutoProgressPreGame] Auto-advancing TeamSelection → Toss.");
            MatchFlow.Log("Cricket", "team selection auto-completed → toss");
            currentOpenPanel = PanelsInfo.Toss;
            tossSwipeDone = true;
            RpcAutoAdvanceToToss();
#if !UNITY_SERVER
            if (!NetworkClient.active)
            {
                MultiplayerPanel.Instance?.CloseMenu("Room");
                SquadPageTWO.instance?.hideMe();
                TossPageTWO.instance?.showMe();
            }
#endif
            // Brief pause so toss page is visible before completing
            yield return new WaitForSeconds(1f);
        }

        if (currentOpenPanel == PanelsInfo.Toss)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][AutoProgressPreGame] Auto-completing Toss.");
            bool ownerBatsFirst = UnityEngine.Random.value > 0.5f;
            MatchFlow.Log("Cricket", $"toss auto-completed (player absent) — room owner {(ownerBatsFirst ? "bats" : "bowls")} first");
            currentOpenPanel = PanelsInfo.None;
            RpcAutoCompleteToss(ownerBatsFirst);
#if !UNITY_SERVER
            if (!NetworkClient.active)
            {
                // Server editor acts as master client
                CONTROLLER.meFirstBatting = ownerBatsFirst ? 1 : 0;
                TossPageTWO.instance?.Continue();
            }
#endif
        }
    }

    /// <summary>Sent to all clients to jump straight to the Toss page, skipping TeamSelection.</summary>
    [ClientRpc]
    private void RpcAutoAdvanceToToss()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcAutoAdvanceToToss] Auto-advancing to Toss.");
        MultiplayerPanel.Instance?.CloseMenu("Room");
        SquadPageTWO.instance?.hideMe();
        TossPageTWO.instance?.showMe();
    }

    /// <summary>
    /// Sent to all clients to immediately complete the toss without user input.
    /// ownerBatsFirst: true = master client bats first, false = joiner bats first.
    /// </summary>
    [ClientRpc]
    private void RpcAutoCompleteToss(bool ownerBatsFirst)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcAutoCompleteToss] Auto-completing toss. ownerBatsFirst={ownerBatsFirst}");
        bool myTeamBatsFirst = MirrorNetwork.Instance.isMasterClient ? ownerBatsFirst : !ownerBatsFirst;
        CONTROLLER.meFirstBatting = myTeamBatsFirst ? 1 : 0;
        TossPageTWO.instance?.Continue();
    }

    [ClientRpc]
    public void RpcOnOpponentConnected()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcOnOpponentConnected] RPC received. Calling RPC_OnOpponentConnected.");
        if (RoomMenu.Instance != null)
            RoomMenu.Instance.RPC_OnOpponentConnected();
        else
            this.DelayUntil(() => RoomMenu.Instance != null, () => RoomMenu.Instance.RPC_OnOpponentConnected());
    }

    /// <summary>
    /// Called by each client from RoomMenu.Start() once their MainMenu scene is fully loaded.
    /// The server waits until BOTH clients signal ready, then broadcasts RpcBothPlayersInMenu
    /// so every panel opens in sync — no SyncVar race, no missed RPCs.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdMenuSceneReady(NetworkConnectionToClient sender = null)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdMenuSceneReady] CMD received. currentOpenPanel={currentOpenPanel}, _menuReadyCount={_menuReadyCount}");

        if (gameStarted)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][CmdMenuSceneReady] gameStarted=true — ignoring (Ground scene reconnect).");
            return;
        }

        // Cancel any pending auto-progress disconnect timer — player is back in time.
        if (_disconnectTimerCoroutine != null)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][CmdMenuSceneReady] Cancelling pending disconnect timer.");
            StopCoroutine(_disconnectTimerCoroutine);
            _disconnectTimerCoroutine = null;
        }

        // Late-join: game already advanced past WaitingForOpponent — restore state for this client.
        if (currentOpenPanel > PanelsInfo.WaitingForOpponent)
        {
            ConstantsData_M.MpLog($"[CricketNetworkManager][CmdMenuSceneReady] Late-join — restoring panel={currentOpenPanel} for reconnecting client.");
            MatchFlow.Log("Cricket", $"a player rejoined before the match (panel {currentOpenPanel})");
            if (sender != null)
                TargetSyncPreGamePanel(sender, currentOpenPanel);
            return;
        }

        _menuReadyCount++;
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdMenuSceneReady] _menuReadyCount now {_menuReadyCount}.");

        if (_menuReadyCount >= 2)
        {
            _menuReadyCount = 0;
            currentOpenPanel = PanelsInfo.WaitingForOpponent;
            ConstantsData_M.MpLog("[CricketNetworkManager][CmdMenuSceneReady] Both players confirmed in MainMenu — broadcasting room panel.");
            MatchFlow.Begin("Cricket", $"game started — {FlowPlayers()}");
            RpcBothPlayersInMenu();
#if !UNITY_SERVER
            if (!NetworkClient.active) RoomMenu.Instance?.RPC_OnOpponentConnected();
#endif
        }
    }

    /// <summary>
    /// Sent to ALL clients once both have confirmed their MainMenu scene is loaded.
    /// Opens the Room panel and starts the team-selection countdown in sync.
    /// </summary>
    [ClientRpc]
    private void RpcBothPlayersInMenu()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcBothPlayersInMenu] Both players in MainMenu — opening Room panel.");
        MultiplayerPanel.Instance?.CloseMenu("Loading");
        MultiplayerPanel.Instance?.OpenMenu("Room");
        if (RoomMenu.Instance != null)
            RoomMenu.Instance.RPC_OnOpponentConnected();
        else
            this.DelayUntil(() => RoomMenu.Instance != null, () => RoomMenu.Instance.RPC_OnOpponentConnected());
    }

    [Command(requiresAuthority = false)]
    public void CmdSetOver(int senderId, int selectedTeamIndex)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetOver] CMD received. senderId = {senderId}, selectedTeamIndex = {selectedTeamIndex}.");
        MatchFlow.Log("Cricket", (CONTROLLER.Overs != null && selectedTeamIndex >= 0 && selectedTeamIndex < CONTROLLER.Overs.Length && CONTROLLER.Overs[selectedTeamIndex] > 0)
            ? $"{FlowWho(senderId)} set the match to {CONTROLLER.Overs[selectedTeamIndex]} overs"
            : $"{FlowWho(senderId)} set the overs option #{selectedTeamIndex}");
        RpcSetOver(senderId, selectedTeamIndex);
    }

    [ClientRpc]
    public void RpcSetOver(int senderId, int selectedTeamIndex)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetOver] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcSetOver] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetOver] Calling SetOvers with selectedTeamIndex = {selectedTeamIndex}.");
        RoomMenu.Instance.SetOvers(selectedTeamIndex);
    }

    [Command(requiresAuthority = false)]
    public void CmdReady()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][CmdReady] CMD received.");
        RpcReady();
    }
    [ClientRpc]
    public void RpcReady()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcReady] RPC received. Calling RPC_Ready.");
        TeamSelectionTWO.instance.RPC_Ready();
    }


    [Command(requiresAuthority = false)]
    public void CmdChangeMyTeamIndex(int senderId, int selectedTeamIndex)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdChangeMyTeamIndex] CMD received. senderId = {senderId}, selectedTeamIndex = {selectedTeamIndex}.");
        RpcChangeMyTeamIndex(senderId, selectedTeamIndex);
    }

    [ClientRpc]
    public void RpcChangeMyTeamIndex(int senderId, int selectedTeamIndex)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChangeMyTeamIndex] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcChangeMyTeamIndex] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChangeMyTeamIndex] Calling RPC_ChangeMyTeamIndex with selectedTeamIndex = {selectedTeamIndex}.");
        TeamSelectionTWO.instance.RPC_ChangeMyTeamIndex(selectedTeamIndex);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetTeamSelectionTimer(float Second)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetTeamSelectionTimer] CMD received. Second = {Second}.");
        RpcSetTeamSelectionTimer(Second);
    }
    [ClientRpc]
    public void RpcSetTeamSelectionTimer(float Second)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetTeamSelectionTimer] RPC received. Calling RPC_SetTeamSelectionTimer with Second = {Second}.");
        TeamSelectionTWO.instance.RPC_SetTeamSelectionTimer(Second);
    }

    [Command(requiresAuthority = false)]
    public void CmdValidateTeamTimer(float timer)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdValidateTeamTimer] CMD received. timer = {timer}.");
        RpcValidateTeamTimer(timer);
    }
    [ClientRpc]
    public void RpcValidateTeamTimer(float timer)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcValidateTeamTimer] RPC received. Calling RPC_ValidateTeamTimer with timer = {timer}.");
        TeamSelectionTWO.instance.RPC_ValidateTeamTimer(timer);
    }



    [Command(requiresAuthority = false)]
    public void CmdTossDecision(int senderId, bool ChoseBatting)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdTossDecision] CMD received. senderId = {senderId}, ChoseBatting = {ChoseBatting}.");
        MatchFlow.Log("Cricket", $"toss: {FlowWho(senderId)} calls {(ChoseBatting ? "heads" : "tails")}");
        currentOpenPanel = PanelsInfo.Toss;
        RpcTossDecision(senderId, ChoseBatting);
    }

    [ClientRpc]
    public void RpcTossDecision(int senderId, bool ChoseBatting)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcTossDecision] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcTossDecision] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcTossDecision] Calling RPC_TossDecision with ChoseBatting = {ChoseBatting}.");
        TossPageTWO.instance.RPC_TossDecision(ChoseBatting);
    }
    [Command(requiresAuthority = false)]
    public void CmdChoseTo(int senderId, bool ChoseBatting)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdChoseTo] CMD received. senderId = {senderId}, ChoseBatting = {ChoseBatting}.");
        MatchFlow.Log("Cricket", $"toss: {FlowWho(senderId)} won, chose to {(ChoseBatting ? "bat" : "bowl")}");
        currentOpenPanel = PanelsInfo.Toss;
        RpcChoseTo(senderId, ChoseBatting);
    }

    [ClientRpc]
    public void RpcChoseTo(int senderId, bool ChoseBatting)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChoseTo] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcChoseTo] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChoseTo] Calling RPC_ChoseTo with ChoseBatting = {ChoseBatting}.");
        TossPageTWO.instance.RPC_ChoseTo(ChoseBatting);
    }

    [Command(requiresAuthority = false)]
    public void CmdAfterUpSwipe()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][CmdAfterUpSwipe] CMD received.");
        currentOpenPanel = PanelsInfo.Toss;
        tossSwipeDone = true;
        RpcAfterUpSwipe();
    }
    [ClientRpc]
    public void RpcAfterUpSwipe()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcAfterUpSwipe] RPC received. Calling RPC_AfterUpSwipe.");
        TossPageTWO.instance.RPC_AfterUpSwipe();
    }

    [Command(requiresAuthority = false)]
    public void CmdCoinStatus(int senderId, string IsHeads)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdCoinStatus] CMD received. senderId = {senderId}, IsHeads = {IsHeads}.");
        MatchFlow.Log("Cricket", $"toss: coin flipped by {FlowWho(senderId)} lands {(IsHeads == "heads" ? "heads" : "tails")}");
        RpcCoinStatus(senderId, IsHeads);
    }

    [ClientRpc]
    public void RpcCoinStatus(int senderId, string IsHeads)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCoinStatus] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcCoinStatus] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCoinStatus] Calling RPC_CoinStatus with IsHeads = {IsHeads}.");
        TossPageTWO.instance.RPC_CoinStatus(IsHeads);
    }
    [Command(requiresAuthority = false)]
    public void CmdALoadScene()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][CmdALoadScene] CMD received. Calling SceneChange to Ground.");
        MatchFlow.Log("Cricket", "loading the ground");
        MirrorNetwork.Instance.SceneChange("Ground");
    }


    [Command(requiresAuthority = false)]
    public void CmdBattingScoreCardContinue(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdBattingScoreCardContinue] CMD received. senderId = {senderId}.");
        RpcBattingScoreCardContinue(senderId);
    }

    [ClientRpc]
    public void RpcBattingScoreCardContinue(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcBattingScoreCardContinue] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcBattingScoreCardContinue] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcBattingScoreCardContinue] Calling RPC_Continue on BattingScoreCard.");
        BattingScoreCard.instance.RPC_Continue();
    }


    [Command(requiresAuthority = false)]
    public void CmdUpdateCanLoadGround(int senderId, bool canLoadGround)
    {
        if (canLoadGround == _lastCanLoadGroundSent) return;
        _lastCanLoadGroundSent = canLoadGround;
        RpcUpdateCanLoadGround(senderId, canLoadGround);
    }

    [ClientRpc]
    public void RpcUpdateCanLoadGround(int senderId, bool canLoadGround)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return;
        GroundController.instance.RPC_UpdateCanLoadGround(canLoadGround);
    }

    [Command(requiresAuthority = false)]
    public void CmdBowlingScoreCardContinue(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdBowlingScoreCardContinue] CMD received. senderId = {senderId}.");
        RpcBowlingScoreCardContinue(senderId);
    }

    [ClientRpc]
    public void RpcBowlingScoreCardContinue(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcBowlingScoreCardContinue] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcBowlingScoreCardContinue] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcBowlingScoreCardContinue] Calling RPC_Continue on BowlingScoreCard.");
        BowlingScoreCard.instance.RPC_Continue();
    }

    // Cross-client bowler identity, sent ALONGSIDE CmdSetBowler and carrying the ABSOLUTE PlayerList
    // index instead of a scorecard position.
    //
    // RpcSetBowler relays a POSITION into BowlersIndexArray, and every client builds that array itself in
    // GetBowlersInTeam by filtering its own TeamList — so the same relayed number can resolve to DIFFERENT
    // players on the two devices. The 06-08 17:01 pair caught it exactly: from one shared starting state
    // (currentBowler=9 on both), the reconnecting client resolved position 2 to absolute 7 while the other
    // stayed on 9, and the two only re-converged when the next over sent a fresh pick.
    //
    //   [BowlerSideDiag] ... type=medium currentBowler=9 ...   (batting client)
    //   [BowlerSideDiag] ... type=fast   currentBowler=7 ...   (bowling client)
    //
    // The absolute index is unambiguous, which is why RestoreBowlerFromAbsoluteIndex already exists and why
    // the snapshot carries it. This is a separate Cmd/Rpc pair rather than a change to CmdSetBowler's
    // signature: Mirror dispatches by NAME HASH, so adding a pair is safe while changing an existing
    // signature would silently break the old one across a mixed deploy.
    [Command(requiresAuthority = false)]
    public void CmdSyncBowlerAbsolute(int senderId, int absoluteBowlerIndex)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncBowlerAbsolute] CMD received. senderId={senderId} absolute={absoluteBowlerIndex}.");
        // Persist it as well as relaying it: the Rpc below is one-shot, so a client that reconnects after
        // this point would otherwise be left with only the scorecard POSITION, which re-maps across an
        // over boundary. This body runs on the SERVER, so without this line the SyncVar is never written
        // and the client-side restore has nothing to read. See syncedBowlerAbsoluteIndex.
        if (absoluteBowlerIndex >= 0) syncedBowlerAbsoluteIndex = absoluteBowlerIndex;
        RpcSyncBowlerAbsolute(senderId, absoluteBowlerIndex);
    }

    [ClientRpc]
    public void RpcSyncBowlerAbsolute(int senderId, int absoluteBowlerIndex)
    {
        if (absoluteBowlerIndex < 0) return;
        if (CONTROLLER.CurrentBowlerIndex == absoluteBowlerIndex) return;   // already agreed
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSyncBowlerAbsolute] Bowler identity from the bowling side: {CONTROLLER.CurrentBowlerIndex} -> {absoluteBowlerIndex}.");
        CONTROLLER.CurrentBowlerIndex = absoluteBowlerIndex;
        if (BowlingScoreCard.instance != null)
            BowlingScoreCard.instance.RestoreBowlerFromAbsoluteIndex(absoluteBowlerIndex);
        var _gc = Singleton<GroundController>.instance;
        if (_gc != null) _gc.RefreshBowlerTypeFromRestoredIndex();
    }

    // Peer state dump for a stall. The idle client asks; the other client answers by printing one line
    // under the SAME episode id. Without this only the stuck side is ever in the logs — the side that is
    // happily bowling never trips its own heartbeat — so "bowler side ball ho gayi, batsman side stuck"
    // could never be read as one event. Deliberately a separate Cmd/Rpc pair: Mirror dispatches by name
    // hash, so adding a pair is safe where changing an existing signature would break the old one.
    [Command(requiresAuthority = false)]
    public void CmdRequestPeerStall(int senderId, int stallEpisodeId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdRequestPeerStall] CMD received. senderId={senderId} id={stallEpisodeId}.");
        RpcRequestPeerStall(senderId, stallEpisodeId);
    }

    [ClientRpc]
    public void RpcRequestPeerStall(int senderId, int stallEpisodeId)
    {
        // The asker already printed its own [StuckTrace] for this id.
        if (senderId == staticVariables.UserProfiledata.user._id) return;
        var _gc = Singleton<GroundController>.instance;
        if (_gc != null) _gc.LogPeerStallState(stallEpisodeId);
    }

    // Free-hit verdict relay. Sent by the batting authority alongside CmdBallOutcome; see
    // GroundController.ApplyAuthoritativeFreeHitState for what went wrong without it. A separate pair
    // rather than extra fields on CmdBallOutcome: Mirror dispatches by name hash, so adding a pair is safe
    // where changing an existing signature would silently break the old one across a mixed deploy.
    [Command(requiresAuthority = false)]
    public void CmdSyncFreeHitState(int senderId, bool lineFreeHitActive, string lastBallType)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncFreeHitState] CMD received. senderId={senderId} lineFreeHit={lineFreeHitActive} lastBallType='{lastBallType}'.");
        RpcSyncFreeHitState(senderId, lineFreeHitActive, lastBallType);
    }

    [ClientRpc]
    public void RpcSyncFreeHitState(int senderId, bool lineFreeHitActive, string lastBallType)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return;   // the authority already has it
        var _gc = Singleton<GroundController>.instance;
        if (_gc != null) _gc.ApplyAuthoritativeFreeHitState(lineFreeHitActive, lastBallType);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetBowler(int currentBowler)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetBowler] CMD received. currentBowler = {currentBowler}.");
        // Persist the index (#1) so Mirror re-delivers it to a client that missed the one-shot Rpc.
        syncedBowlerListIndex = currentBowler;
        RpcSetBowler(currentBowler);
    }

    [ClientRpc]
    public void RpcSetBowler(int currentBowler)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetBowler] RPC received. Calling RPC_SetBowler with currentBowler = {currentBowler}.");
        BowlingScoreCard.instance.RPC_SetBowler(currentBowler);
    }

    // First-ball bowler-side fix (#1): SyncVar hook — re-applies the bowler index on a client that
    // joined/subscribed (or reconnected) AFTER the non-buffered RpcSetBowler was sent. Guarded to the
    // BATTING client only, mirroring RPC_SetBowler's own usage; RPC_SetBowler is idempotent (it just
    // re-stamps CurrentBowlerIndex from the already-populated BowlersIndexArray), so re-applying is safe.
    void OnBowlerIndexSynced(int oldVal, int newVal)
    {
        if (newVal < 0) return;
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex
            && BowlingScoreCard.instance != null)
            BowlingScoreCard.instance.RPC_SetBowler(newVal);
    }
    [Command(requiresAuthority = false)]
    public void CmdToggleCanLoadBowlerScoreCard(int senderId, bool canLoad)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdToggleCanLoadBowlerScoreCard] CMD received. senderId = {senderId}, canLoad = {canLoad}.");
        RpcToggleCanLoadBowlerScoreCard(senderId, canLoad);
    }

    [ClientRpc]
    public void RpcToggleCanLoadBowlerScoreCard(int senderId, bool canLoad)
    {
        //   ConstantsData_M.MpLog($"[CricketNetworkManager][RpcToggleCanLoadBowlerScoreCard] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");//
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            // ConstantsData_M.MpLog("[CricketNetworkManager][RpcToggleCanLoadBowlerScoreCard] Sender is local player. Returning early.");
            return;
        }
        //  ConstantsData_M.MpLog($"[CricketNetworkManager][RpcToggleCanLoadBowlerScoreCard] Calling RPC_ToggleCanLoadBowlerScoreCard with canLoad = {canLoad}.");
        // Innings-shift deadlock fix (tester: 2nd innings won't start, both stuck on scorecards). Set the flag on
        // the CONTROLLER DIRECTLY so a relay that lands while this (batting) side's BattingScoreCard instance isn't
        // ready — the innings swap reloads the scorecards — is NOT lost. Previously the null instance threw here,
        // BattingScoreCard.RPC_ToggleCanLoadBowlerScoreCard (which sets CanLoadBowlerScoreBoard) never ran, so
        // CanLoadBowlerScoreBoard stayed false and BattingScoreCard.Continue() deadlocked at its !CanLoadBowlerScoreBoard
        // early-return forever. The method call (UI timer) is now null-safe; the flag is authoritative regardless.
        CONTROLLER.CanLoadBowlerScoreBoard = canLoad;
        BattingScoreCard.instance?.RPC_ToggleCanLoadBowlerScoreCard(canLoad);
    }


    [Command(requiresAuthority = false)]
    public void CmdActivateBowlerSideChangeViaUI(string bowlerSide)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdActivateBowlerSideChangeViaUI] CMD received. bowlerSide = {bowlerSide}.");
        // Keep the authoritative SyncVar fresh so a reconnect after a UI side-toggle restores the
        // POST-toggle side (this Cmd's RPC flips bowlerSide to its mirror; CmdSyncBowlerSide is the
        // only other writer of syncedBowlerSide, so without this a toggled side was lost on reconnect).
        syncedBowlerSide = (bowlerSide == "right") ? "left" : "right";
        RpcActivateBowlerSideChangeViaUI(bowlerSide);
    }

    [ClientRpc]
    public void RpcActivateBowlerSideChangeViaUI(string bowlerSide)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcActivateBowlerSideChangeViaUI] RPC received. Calling RPC_ActivateBowlerSideChangeViaUI with bowlerSide = {bowlerSide}.");
        GroundController.instance.RPC_ActivateBowlerSideChangeViaUI(bowlerSide);
    }
    [Command(requiresAuthority = false)]
    public void CmdAckForBowling(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdAckForBowling] CMD received. senderId = {senderId}.");
        RpcAckForBowling(senderId);
    }

    [ClientRpc]
    public void RpcAckForBowling(int senderId)
    {
        //   ConstantsData_M.MpLog($"[CricketNetworkManager][RpcAckForBowling] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");//
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            //  ConstantsData_M.MpLog("[CricketNetworkManager][RpcAckForBowling] Sender is local player. Returning early.");
            return;
        }
        // ConstantsData_M.MpLog("[CricketNetworkManager][RpcAckForBowling] Calling AckForBowling on GroundController.");
        GroundController.instance.AckForBowling();
    }
    [Command(requiresAuthority = false)]
    public void CmdBowlerWaiting(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdBowlerWaiting] CMD received. senderId = {senderId}.");
        RpcBowlerWaiting(senderId);
    }

    [ClientRpc]
    public void RpcBowlerWaiting(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcBowlerWaiting] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcBowlerWaiting] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcBowlerWaiting] Calling RPC_BowlerWaiting on GroundController.");
        GroundController.instance.RPC_BowlerWaiting();
    }

    /// <summary>
    /// Bowling player broadcasts the exact moment its run-up animation actually starts
    /// (from DelayedFunction), so the batting player starts the bowler run-up on receipt
    /// instead of playing it early in BowlerWaiting. This keeps the batting-side throw frame
    /// aligned with the authoritative ball release/spawn (both RPCs travel the same one-way latency).
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSyncBowlerRunUp(int senderId, int animNumber, double startAtNetTime)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncBowlerRunUp] CMD received. senderId={senderId} animNumber={animNumber}");
        RpcSyncBowlerRunUp(senderId, animNumber, startAtNetTime);
    }

    [ClientRpc]
    public void RpcSyncBowlerRunUp(int senderId, int animNumber, double startAtNetTime)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return;
        GroundController.instance?.RPC_StartBowlerRunUp(animNumber, startAtNetTime);
    }

    [Command(requiresAuthority = false)]
    public void CmdChangeBallAngle(int senderId, float ballAngle, float horizontalSpeed, float ballProjectileAngle, float ballProjectileHeight, float ballTimingFirstBounceDistance, float ballProjectileAnglePerSecond, float ballBatMeetingHeight, float horizontalSpeedMultiplier, string ballstatus, bool edgeCatch, Vector3 contactPos)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdChangeBallAngle] CMD received. senderId = {senderId}, ballAngle = {ballAngle}, ballstatus = {ballstatus}, edgeCatch = {edgeCatch}, contactPos = {contactPos}.");
        RpcChangeBallAngle(senderId, ballAngle, horizontalSpeed, ballProjectileAngle, ballProjectileHeight, ballTimingFirstBounceDistance, ballProjectileAnglePerSecond, ballBatMeetingHeight, horizontalSpeedMultiplier, ballstatus, edgeCatch, contactPos);
    }

    [ClientRpc]
    public void RpcChangeBallAngle(int senderId, float ballAngle, float horizontalSpeed, float ballProjectileAngle, float ballProjectileHeight, float ballTimingFirstBounceDistance, float ballProjectileAnglePerSecond, float ballBatMeetingHeight, float horizontalSpeedMultiplier, string ballstatus, bool edgeCatch, Vector3 contactPos)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChangeBallAngle] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcChangeBallAngle] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChangeBallAngle] Calling RPC_ChangeBallAngle. ballstatus = {ballstatus}, edgeCatch = {edgeCatch}, contactPos = {contactPos}.");
        GroundController.instance.RPC_ChangeBallAngle(ballAngle, horizontalSpeed, ballProjectileAngle, ballProjectileHeight, ballTimingFirstBounceDistance, ballProjectileAnglePerSecond, ballBatMeetingHeight, horizontalSpeedMultiplier, ballstatus, edgeCatch, contactPos);
    }

    /// <summary>
    /// Caught-behind umpire decision sync. The BATTING client owns edge/contact detection and
    /// computes the random umpire decision (out/notout) + whether the ball edged. The BOWLING
    /// client never computed it (RNG block skipped) and the old Photon relay was commented out,
    /// so it kept a stale/empty umpireInitialDecision and fell into the NOT-OUT branch — clients
    /// disagreed on caught-behind outcomes. Batting broadcasts the source facts; bowling applies
    /// them and recomputes its own DRS availability (which is client-perspective).
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSetUltraEdgeDecision(int senderId, string decision, bool isEdged)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetUltraEdgeDecision] CMD received. senderId = {senderId}, decision = {decision}, isEdged = {isEdged}.");
        RpcSetUltraEdgeDecision(senderId, decision, isEdged);
    }

    [ClientRpc]
    public void RpcSetUltraEdgeDecision(int senderId, string decision, bool isEdged)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetUltraEdgeDecision] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcSetUltraEdgeDecision] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetUltraEdgeDecision] Calling RPC_SetUltraEdgeDecision. decision = {decision}, isEdged = {isEdged}.");
        GroundController.instance.RPC_SetUltraEdgeDecision(decision, isEdged);
    }

    [Command(requiresAuthority = false)]
    public void CmdMultiplayerReview(int senderId, bool tookReview)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdMultiplayerReview] CMD received. senderId = {senderId}, tookReview = {tookReview}.");
        RpcMultiplayerReview(senderId, tookReview);
    }

    [ClientRpc]
    public void RpcMultiplayerReview(int senderId, bool tookReview)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcMultiplayerReview] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcMultiplayerReview] Sender is local player. Returning early.");
            return;
        }

        Singleton<ReviewSystem>.instance.MultiplayerReview(tookReview);
    }

    /// <summary>
    /// Bowling player broadcasts their authoritative ball trajectory state at first bounce so the
    /// batting player corrects any parametric drift that built up since ball release.
    /// Both clients then simulate identically from the same state and see the same delivery.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSyncBallTrajectory(int senderId, Vector3 ballPos, Vector3 tempPos,
        float ballAngle, float curLaunchAngle, float curArcHeight,
        float curAngleChangeRate, float curHorizontalVelocity, int curBounceCount)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncBallTrajectory] CMD received. senderId={senderId} pos={ballPos} angle={ballAngle} bounce={curBounceCount}");
        RpcSyncBallTrajectory(senderId, ballPos, tempPos, ballAngle, curLaunchAngle,
            curArcHeight, curAngleChangeRate, curHorizontalVelocity, curBounceCount);
    }

    [ClientRpc]
    public void RpcSyncBallTrajectory(int senderId, Vector3 ballPos, Vector3 tempPos,
        float ballAngle, float curLaunchAngle, float curArcHeight,
        float curAngleChangeRate, float curHorizontalVelocity, int curBounceCount)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return; // sender already has correct state
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSyncBallTrajectory] Applying trajectory correction. pos={ballPos} angle={ballAngle} bounce={curBounceCount}");
        GroundController.instance?.RPC_SyncBallTrajectory(ballPos, tempPos, ballAngle,
            curLaunchAngle, curArcHeight, curAngleChangeRate, curHorizontalVelocity, curBounceCount);
    }

    /// <summary>
    /// Option A — Server-authoritative ball position streaming.
    /// Bowling player calls this every 50 ms (20 Hz) while the ball is in flight.
    /// The server relays the position to all clients; the batting player lerps its
    /// local ball toward the received position each frame, eliminating all forms of
    /// trajectory divergence (bat/pad hit-miss, bounce drift, frame-rate mismatch, etc.).
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSyncBallPosition(int senderId, Vector3 ballPos, Vector3 tempPos, float curLaunchAngle)
    {
        RpcSyncBallPosition(senderId, ballPos, tempPos, curLaunchAngle);
    }

    [ClientRpc]
    public void RpcSyncBallPosition(int senderId, Vector3 ballPos, Vector3 tempPos, float curLaunchAngle)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return; // bowling player already has correct state
        GroundController.instance?.RPC_SyncBallPosition(ballPos, tempPos, curLaunchAngle);
    }

    /// <summary>
    /// POST-SHOT trajectory streaming. After bat contact the outcome authority is the BATTING
    /// player, so it also owns the visible flight. The batting player calls this every 50 ms
    /// (20 Hz) while the hit ball is in flight; the bowling player overwrites its full parametric
    /// integrator state in RPC_SyncBallShot so both screens show the same six / four / catch / out.
    /// </summary>
    // startNetTime/dampFactor (deterministic post-shot, Phase 1): the batting authority's NetworkTime at
    // which it armed the deterministic flight + the random friction it chose, so the bowling follower
    // integrates the SAME fixed-step path from the SAME start instant + friction → bit-matching flight.
    [Command(requiresAuthority = false)]
    public void CmdSyncBallShot(int senderId, Vector3 ballPos, Vector3 tempPos, float ballAngle,
        float curLaunchAngle, float curArcHeight, float curAngleChangeRate,
        float curHorizontalVelocity, int curBounceCount, int curBoundaryType,
        double startNetTime, float dampFactor)
    {
        RpcSyncBallShot(senderId, ballPos, tempPos, ballAngle, curLaunchAngle,
            curArcHeight, curAngleChangeRate, curHorizontalVelocity, curBounceCount, curBoundaryType,
            startNetTime, dampFactor);
    }

    [ClientRpc]
    public void RpcSyncBallShot(int senderId, Vector3 ballPos, Vector3 tempPos, float ballAngle,
        float curLaunchAngle, float curArcHeight, float curAngleChangeRate,
        float curHorizontalVelocity, int curBounceCount, int curBoundaryType,
        double startNetTime, float dampFactor)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return; // batting player already has correct state
        GroundController.instance?.RPC_SyncBallShot(ballPos, tempPos, ballAngle, curLaunchAngle,
            curArcHeight, curAngleChangeRate, curHorizontalVelocity, curBounceCount, curBoundaryType,
            startNetTime, dampFactor);
    }

    /// <summary>
    /// Over-boundary strike sync (handedness desync fix). At the end of an over the two
    /// batsmen cross ends; the batting (authority) client performs the swap locally in
    /// GameData.CurrentBallUpdate and then calls this so the bowling client adopts the SAME
    /// post-swap striker/non-striker. Needed because the regular per-ball CmdCorrectBallState
    /// carries the PRE-over-swap indices (it is sent BEFORE CurrentBallUpdate runs the swap),
    /// which otherwise leaves the two clients disagreeing on which (left/right-handed) batsman
    /// is on strike for the new over. Sent after CmdCorrectBallState, so on the reliable ordered
    /// RPC channel it arrives last and wins. Also persisted to SyncVars for reconnect.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSyncStrikeAtOver(int senderId, int strikerIndex, int nonStrikerIndex)
    {
        syncedStrikerIndex    = strikerIndex;
        syncedNonStrikerIndex = nonStrikerIndex;
        RpcSyncStrikeAtOver(senderId, strikerIndex, nonStrikerIndex);
    }

    [ClientRpc]
    public void RpcSyncStrikeAtOver(int senderId, int strikerIndex, int nonStrikerIndex)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return; // batting player already swapped locally
        CONTROLLER.StrikerIndex    = strikerIndex;
        CONTROLLER.NonStrikerIndex = nonStrikerIndex;
    }

    /// <summary>
    /// Phase 1 ball sync — bowling player sends authoritative release-time physics parameters
    /// to the batting player immediately when the ball is released. The batting player applies
    /// these values (overwriting any stale local state from a stale bowlingSpot) and advances
    /// the ball position by the one-way network latency so both clients simulate identically.
    /// </summary>
    // startNetTime (deterministic delivery, Phase 2): the bowling authority's NetworkTime at release, so the
    // batting follower bases its fixed-step local delivery sim on the SAME release instant → identical flight.
    [Command(requiresAuthority = false)]
    public void CmdSyncBallRelease(int senderId, Vector3 ballPos, Vector3 tempPos,
        float ballAngle, float curLaunchAngle, float curArcHeight,
        float curAngleChangeRate, float curHorizontalVelocity,
        float curSpinFactor, float curSwingIntensity, bool curIsFullToss, double startNetTime,
        int deliverySeed, float spotLength, bool overstepped, int fieldIdx, bool fieldRestriction, int bowlerIdx)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncBallRelease] CMD received. senderId={senderId} pos={ballPos} angle={ballAngle} seed={deliverySeed}");
        if (fieldIdx > 0) syncedFielderChangeIndex = fieldIdx;   // keep the reconnect backstop fresh (local index resets never refreshed it)
        RpcSyncBallRelease(senderId, ballPos, tempPos, ballAngle, curLaunchAngle,
            curArcHeight, curAngleChangeRate, curHorizontalVelocity,
            curSpinFactor, curSwingIntensity, curIsFullToss, startNetTime, deliverySeed, spotLength, overstepped, fieldIdx, fieldRestriction, bowlerIdx);
    }

    [ClientRpc]
    public void RpcSyncBallRelease(int senderId, Vector3 ballPos, Vector3 tempPos,
        float ballAngle, float curLaunchAngle, float curArcHeight,
        float curAngleChangeRate, float curHorizontalVelocity,
        float curSpinFactor, float curSwingIntensity, bool curIsFullToss, double startNetTime,
        int deliverySeed, float spotLength, bool overstepped, int fieldIdx, bool fieldRestriction, int bowlerIdx)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return; // bowling player already has correct state
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSyncBallRelease] Applying release sync. pos={ballPos} angle={ballAngle} seed={deliverySeed}");
        // Deterministic lockstep: seed the shared per-delivery RNG stream BEFORE any gameplay roll of
        // this ball can run on this client (see Docs/DETERMINISTIC_LOCKSTEP.md).
        DeterministicRng.Seed(deliverySeed);
        GroundController.instance?.RPC_SyncBallRelease(ballPos, tempPos, ballAngle,
            curLaunchAngle, curArcHeight, curAngleChangeRate, curHorizontalVelocity,
            curSpinFactor, curSwingIntensity, curIsFullToss, startNetTime, spotLength, overstepped, fieldIdx, fieldRestriction, bowlerIdx);
    }

    [Command(requiresAuthority = false)]
    public void CmdChangeBowlinSpot(int senderId, Vector3 bowlingSpot)
    {
        if (bowlingSpot == _lastBowlingSpotSent) return;
        _lastBowlingSpotSent = bowlingSpot;
        RpcChangeBowlinSpot(senderId, bowlingSpot);
    }

    [ClientRpc]
    public void RpcChangeBowlinSpot(int senderId, Vector3 bowlingSpot)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return;
        GroundController.instance.RPC_ChangeBowlinSpot(bowlingSpot);
    }
    [Command(requiresAuthority = false)]
    public void CmdFielderChangeIndex(int fielderChangeIdx)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdFielderChangeIndex] CMD received. fielderChangeIdx = {fielderChangeIdx}.");
        syncedFielderChangeIndex = fielderChangeIdx;   // #3: persist on server for reconnect auto-delivery
        RpcFielderChangeIndex(fielderChangeIdx);
    }

    [ClientRpc]
    public void RpcFielderChangeIndex(int fielderChangeIdx)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcFielderChangeIndex] RPC received. Calling RPC_FielderChangeIndex with fielderChangeIdx = {fielderChangeIdx}.");
        GroundController.instance.RPC_FielderChangeIndex(fielderChangeIdx);
    }

    // Catch-decision sync: the BATTING authority computes which fielder catches a lofted shot and
    // broadcasts that fielder's index so the BOWLING client doesn't independently (and divergently)
    // decide catch-vs-chase. RPC_SetCatchFielder self-gates to the bowling follower.
    // catchPoint: the batting authority's exact ballCatchingPoint, relayed so the bowling follower's catcher
    // runs to the SAME spot (the local point is derived from a per-client random firstBounceDistance → diverged).
    [Command(requiresAuthority = false)]
    public void CmdSetCatchFielder(int fielderIndex, Vector3 catchPoint)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetCatchFielder] CMD received. fielderIndex = {fielderIndex}, catchPoint = {catchPoint}.");
        RpcSetCatchFielder(fielderIndex, catchPoint);
    }

    [ClientRpc]
    public void RpcSetCatchFielder(int fielderIndex, Vector3 catchPoint)
    {
        GroundController.instance?.RPC_SetCatchFielder(fielderIndex, catchPoint);
    }

    // Confirmed-catch relay: the moment the BATTING authority completes a CLEAN outfield catch, it tells the
    // BOWLING follower to resolve its (possibly-diverged) local ball as that catch IMMEDIATELY — instead of
    // waiting for the slower authoritative RpcBallOutcome, by which time the follower's ball has already bounced
    // near the fielder + the camera chased the throw-back (the "ball fell + camera jerk" bug). Sent once per
    // delivery and ONLY on a real catch → no dropped-catch false positive. OnConfirmedOutfieldCatch self-gates
    // to the bowling follower.
    [Command(requiresAuthority = false)]
    public void CmdConfirmOutfieldCatch(int catcherIndex)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdConfirmOutfieldCatch] CMD received. catcherIndex = {catcherIndex}.");
        RpcConfirmOutfieldCatch(catcherIndex);
    }

    [ClientRpc]
    public void RpcConfirmOutfieldCatch(int catcherIndex)
    {
        GroundController.instance?.OnConfirmedOutfieldCatch(catcherIndex);
    }

    // Full fielding-setup relay: the batting authority sends its FINAL active-fielder indices + actions
    // (0=chase,1=catch) + chase points so the bowling follower's chasers run to the SAME spots (the local
    // chase radius is derived from a per-client random firstBounceDistance/horizontalVelocity → diverged).
    [Command(requiresAuthority = false)]
    public void CmdSetActiveFielderSetup(int[] indices, byte[] actions, Vector3[] chasePoints)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetActiveFielderSetup] CMD received. count = {indices?.Length}.");
        RpcSetActiveFielderSetup(indices, actions, chasePoints);
    }

    [ClientRpc]
    public void RpcSetActiveFielderSetup(int[] indices, byte[] actions, Vector3[] chasePoints)
    {
        GroundController.instance?.RPC_SetActiveFielderSetup(indices, actions, chasePoints);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetMultiplayerCollider(int senderId, string other, float savedBallRayCastConnectedZposition)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetMultiplayerCollider] CMD received. senderId = {senderId}, other = {other}, savedBallRayCastConnectedZposition = {savedBallRayCastConnectedZposition}.");
        RpcSetMultiplayerCollider(senderId, other, savedBallRayCastConnectedZposition);
    }

    [ClientRpc]
    public void RpcSetMultiplayerCollider(int senderId, string other, float savedBallRayCastConnectedZposition)
    {
        //   ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetMultiplayerCollider] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");//
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            //  ConstantsData_M.MpLog("[CricketNetworkManager][RpcSetMultiplayerCollider] Sender is local player. Returning early.");
            return;
        }
        //  ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetMultiplayerCollider] Calling RPC_SetMultiplayerCollider. other = {other}, zPos = {savedBallRayCastConnectedZposition}.");//
        GroundController.instance.RPC_SetMultiplayerCollider(other, savedBallRayCastConnectedZposition);
    }

    [Command(requiresAuthority = false)]
    public void CmdChangeOppTutorial(int senderId, bool on)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdChangeOppTutorial] CMD received. senderId = {senderId}, on = {on}.");
        RpcChangeOppTutorial(senderId, on);
    }

    [ClientRpc]
    public void RpcChangeOppTutorial(int senderId, bool on)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChangeOppTutorial] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcChangeOppTutorial] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChangeOppTutorial] Calling RPC_ChangeOppTutorial with on = {on}.");
        GroundController.instance.RPC_ChangeOppTutorial(on);
    }
    [Command(requiresAuthority = false)]
    public void CmdWideBall(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdWideBall] CMD received. senderId = {senderId}.");
        RpcWideBall(senderId);
    }

    [ClientRpc]
    public void RpcWideBall(int senderId)
    {
        //  ConstantsData_M.MpLog($"[CricketNetworkManager][RpcWideBall] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            // ConstantsData_M.MpLog("[CricketNetworkManager][RpcWideBall] Sender is local player. Returning early.");
            return;
        }
        //   ConstantsData_M.MpLog("[CricketNetworkManager][RpcWideBall] Calling RPC_WideBall on GroundController.");//
        GroundController.instance.RPC_WideBall();
    }

    [Command(requiresAuthority = false)]
    public void CmdChangeBatsmanPosition(int senderId, Vector3 position)
    {
        if (position == _lastBatsmanPosSent) return;
        _lastBatsmanPosSent = position;
        RpcChangeBatsmanPosition(senderId, position);
    }

    [ClientRpc]
    public void RpcChangeBatsmanPosition(int senderId, Vector3 position)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return;
        GroundController.instance.ChangeBatsmanPosition(position);
    }

    [Command(requiresAuthority = false)]
    public void CmdShotPlayed(int senderId, string shotPlayed, string batsmanAnim, float batReachingTimeForOptimalShotLength, float optimalShotActivationTime, bool powerShot)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdShotPlayed] CMD received. senderId = {senderId}, shotPlayed = {shotPlayed}, powerShot = {powerShot}.");
        RpcShotPlayed(senderId, shotPlayed, batsmanAnim, batReachingTimeForOptimalShotLength, optimalShotActivationTime, powerShot);
    }

    [ClientRpc]
    public void RpcShotPlayed(int senderId, string shotPlayed, string batsmanAnim, float batReachingTimeForOptimalShotLength, float optimalShotActivationTime, bool powerShot)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcShotPlayed] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcShotPlayed] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcShotPlayed] Calling RPC_ShotPlayed. shotPlayed = {shotPlayed}, powerShot = {powerShot}.");
        GroundController.instance.RPC_ShotPlayed(shotPlayed, batsmanAnim, batReachingTimeForOptimalShotLength, optimalShotActivationTime, powerShot);
    }

    [Command(requiresAuthority = false)]
    public void CmdTouchCancelRun(int senderId, float timeDifference)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdTouchCancelRun] CMD received. senderId = {senderId}, timeDifference = {timeDifference}.");
        RpcTouchCancelRun(senderId, timeDifference);
    }

    [ClientRpc]
    public void RpcTouchCancelRun(int senderId, float timeDifference)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcTouchCancelRun] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcTouchCancelRun] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcTouchCancelRun] Calling RPC_touchCancelRun with timeDifference = {timeDifference}.");
        GroundController.instance.RPC_touchCancelRun(timeDifference);
    }
    [Command(requiresAuthority = false)]
    public void CmdSendOppAck()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][CmdSendOppAck] CMD received.");
        RpcSendOppAck();
    }

    [ClientRpc]
    public void RpcSendOppAck()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcSendOppAck] RPC received. Calling RPC_SendOppAck on GroundController.");
        GroundController.instance.RPC_SendOppAck();
    }

    [Command(requiresAuthority = false)]
    public void CmdLBWDecision(int senderId, bool lbwAppeal, bool ballInLine, bool lbw)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdLBWDecision] CMD received. senderId = {senderId}, lbwAppeal = {lbwAppeal}, ballInLine = {ballInLine}, lbw = {lbw}.");
        RpcLBWDecision(senderId, lbwAppeal, ballInLine, lbw);
    }

    [ClientRpc]
    public void RpcLBWDecision(int senderId, bool lbwAppeal, bool ballInLine, bool lbw)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcLBWDecision] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcLBWDecision] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcLBWDecision] Calling RPC_LBWDecision. lbwAppeal = {lbwAppeal}, ballInLine = {ballInLine}, lbw = {lbw}.");
        GroundController.instance.RPC_LBWDecision(lbwAppeal, ballInLine, lbw);
    }

    [Command(requiresAuthority = false)]
    public void CmdShotInput(int senderId, double swingNetTime, string shotPlayed, string batsmanAnimName,
        bool isPower, float angleValue, float batterX, bool squareLegGlance, float animSpeed, bool batsmanConfident,
        bool perfect, bool mistimed, float hSpeedAdj, float fbMult)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdShotInput] shot={shotPlayed} power={isPower} anim={batsmanAnimName}");
        RpcShotInput(senderId, swingNetTime, shotPlayed, batsmanAnimName, isPower, angleValue, batterX, squareLegGlance, animSpeed, batsmanConfident, perfect, mistimed, hSpeedAdj, fbMult);
    }

    [ClientRpc]
    public void RpcShotInput(int senderId, double swingNetTime, string shotPlayed, string batsmanAnimName,
        bool isPower, float angleValue, float batterX, bool squareLegGlance, float animSpeed, bool batsmanConfident,
        bool perfect, bool mistimed, float hSpeedAdj, float fbMult)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return; // the batter armed itself locally
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcShotInput] Applying lockstep swing: shot={shotPlayed}");
        GroundController.instance?.OnLockstepShotInput(swingNetTime, shotPlayed, batsmanAnimName, isPower, angleValue, batterX, squareLegGlance, animSpeed, batsmanConfident, perfect, mistimed, hSpeedAdj, fbMult);
    }

    [Command(requiresAuthority = false)]
    public void CmdShotSelected(bool isPower, int selectedAngle, float num3)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdShotSelected] CMD received. isPower = {isPower}, selectedAngle = {selectedAngle}, num3 = {num3}.");
        RpcShotSelected(isPower, selectedAngle, num3);
    }

    [ClientRpc]
    public void RpcShotSelected(bool isPower, int selectedAngle, float num3)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcShotSelected] RPC received. Calling RPC_ShotSelected. isPower = {isPower}, selectedAngle = {selectedAngle}, num3 = {num3}.");
        GroundController.instance.RPC_ShotSelected(isPower, selectedAngle, num3);
    }

    [Command(requiresAuthority = false)]
    public void CmdInitRun(int senderId, float temp, bool boolean)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdInitRun] CMD received. senderId = {senderId}, temp = {temp}, boolean = {boolean}.");
        RpcInitRun(senderId, temp, boolean);
    }

    [ClientRpc]
    public void RpcInitRun(int senderId, float temp, bool boolean)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcInitRun] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcInitRun] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcInitRun] Calling RPC_InitRun. temp = {temp}, boolean = {boolean}.");
        GroundController.instance.RPC_InitRun(temp, boolean);
    }

    [Command(requiresAuthority = false)]
    public void CmdFreezeBowlingSpot(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdFreezeBowlingSpot] CMD received. senderId = {senderId}.");
        RpcFreezeBowlingSpot(senderId);
    }

    [ClientRpc]
    public void RpcFreezeBowlingSpot(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcFreezeBowlingSpot] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcFreezeBowlingSpot] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcFreezeBowlingSpot] Calling RPC_FreezeBowlingSpot on GroundController.");
        GroundController.instance.RPC_FreezeBowlingSpot();
    }
    [Command(requiresAuthority = false)]
    public void CmdAutomaticBall()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][CmdAutomaticBall] CMD received.");
        MatchFlow.Log("Cricket", "bowling timer ran out — automatic delivery");
        RpcAutomaticBall();
    }

    [ClientRpc]
    public void RpcAutomaticBall()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcAutomaticBall] RPC received. Calling AutomaticBall on GroundController.");
        GroundController.instance.AutomaticBall();
    }

    [Command(requiresAuthority = false)]
    public void CmdShotTiming(float firstBounceSpeedMultiplier, float horizontalSpeedAdjustment, bool isPerfectShot, bool isMistimedShot)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdShotTiming] CMD received. isPerfectShot = {isPerfectShot}, isMistimedShot = {isMistimedShot}.");
        RpcShotTiming(firstBounceSpeedMultiplier, horizontalSpeedAdjustment, isPerfectShot, isMistimedShot);
    }

    [ClientRpc]
    public void RpcShotTiming(float firstBounceSpeedMultiplier, float horizontalSpeedAdjustment, bool isPerfectShot, bool isMistimedShot)
    {
        // ConstantsData_M.MpLog($"[CricketNetworkManager][RpcShotTiming] RPC received. Calling RPC_ShotTiming. isPerfectShot = {isPerfectShot}, isMistimedShot = {isMistimedShot}.");//
        GroundController.instance.RPC_ShotTiming(firstBounceSpeedMultiplier, horizontalSpeedAdjustment, isPerfectShot, isMistimedShot);
    }

    // Relays a stall watchdog's non-destructive re-arm so BOTH clients recover the same way. Deliberately
    // separate from CmdCallRebowl: that lands the receiver in Launcher.RPC_CallRebowl, a different recovery,
    // and the two sides then disagree on the ball count (04-08: authority 3 chips, opponent 2 — the 1.2 vs
    // 1.1 on the two screens).
    // Relays the batting authority's EXACT ball-chip strip after every delivery.
    //
    // The bowling client only ever calls AddBall from the authoritative outcome path, so a delivery whose
    // outcome never reached it produces NO chip — and its over history is then permanently shorter than the
    // batting side's. The 04-08 logs show it plainly:
    //     [RpcCorrectBallState] Correcting bowling-side state: ballCount=5 ballNum=4
    //     [RpcCorrectBallState] authority ballCount=5 but this client has 3 chips
    // Sending only the COUNT (as CmdCorrectBallState does) cannot repair that — you cannot rebuild missing
    // chips from a number. Send the strip itself, in the same pipe-joined form the reconnect snapshot already
    // uses, and let the receiver replace its own wholesale.
    //
    // Deliberately a NEW Cmd/Rpc pair rather than extra parameters on CmdCorrectBallState: Mirror dispatches
    // remote calls by NAME HASH, so a peer that does not know this one simply drops it, whereas changing an
    // existing signature would silently stop the existing call from being delivered at all.
    [Command(requiresAuthority = false)]
    public void CmdSyncBallStrip(int senderId, string ballListInfo, string ballExtras)
    {
        RpcSyncBallStrip(senderId, ballListInfo, ballExtras);
    }

    [ClientRpc]
    public void RpcSyncBallStrip(int senderId, string ballListInfo, string ballExtras)
    {
        if (staticVariables.UserProfiledata?.user != null && staticVariables.UserProfiledata.user._id == senderId)
            return;   // the authority already has it
        var sbl = Singleton<ScoreBoardBallList>.instance;
        if (sbl == null || sbl.ballList == null) return;

        int before = sbl.ballList.Count;
        sbl.ballList.Clear();
        if (!string.IsNullOrEmpty(ballListInfo))
            foreach (var chip in ballListInfo.Split('|'))
                if (!string.IsNullOrEmpty(chip)) sbl.ballList.Add(chip);
        if (sbl.extras != null)
        {
            sbl.extras.Clear();
            if (!string.IsNullOrEmpty(ballExtras))
                foreach (var ex in ballExtras.Split('|'))
                    if (!string.IsNullOrEmpty(ex)) sbl.extras.Add(ex);
        }
        // Written straight into the lists, not via AddBall, so its ballCount++ side effect cannot fire; then
        // pinned so the AddBall(ballList.Count - 1) extra-indexing invariant holds for the next delivery.
        sbl.ballCount = sbl.ballList.Count;
        if (before != sbl.ballList.Count)
            ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSyncBallStrip] Over strip rebuilt from the authority: {before} chips -> {sbl.ballList.Count} (extras {(sbl.extras != null ? sbl.extras.Count : 0)}).");
        if (Singleton<Scoreboard>.instance != null) Singleton<Scoreboard>.instance.UpdateScoreCard();
    }

    [Command(requiresAuthority = false)]
    public void CmdRelayStallReArm(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdRelayStallReArm] CMD received. senderId={senderId}.");
        RpcRelayStallReArm(senderId);
    }

    [ClientRpc]
    public void RpcRelayStallReArm(int senderId)
    {
        if (staticVariables.UserProfiledata?.user != null && staticVariables.UserProfiledata.user._id == senderId)
            return;   // the sender already re-armed locally
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcRelayStallReArm] RPC received from {senderId} — applying the same re-arm here.");
        if (GroundController.instance != null)
            GroundController.instance.ApplyRelayedStallReArm();
    }

    [Command(requiresAuthority = false)]
    public void CmdBoundaryVerdict(int senderId, int runs, bool hitBat)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdBoundaryVerdict] CMD received. senderId={senderId} runs={runs} hitBat={hitBat}.");
        RpcBoundaryVerdict(senderId, runs, hitBat);
    }

    [ClientRpc]
    public void RpcBoundaryVerdict(int senderId, int runs, bool hitBat)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcBoundaryVerdict] RPC received. senderId={senderId} runs={runs} hitBat={hitBat}.");
        if (GroundController.instance != null)
            GroundController.instance.OnBoundaryVerdictRelayed(senderId, runs, hitBat);
    }

    // RUN-OUT VERDICT (batting authority -> bowling follower). The umpire's OUT / NOT-OUT signal was
    // chosen from each client's own hasRunOutOccurred while only the SCORE was authoritative, so a tight
    // call that the two sides judged differently showed OUT on one screen and NOT OUT on the other for a
    // delivery both had already counted as a wicket (tester 12-08 #3). Same shape as CmdThrowTarget below.
    [Command(requiresAuthority = false)]
    public void CmdRunOutVerdict(int senderId, bool isOut)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdRunOutVerdict] CMD received. senderId={senderId} isOut={isOut}.");
        RpcRunOutVerdict(senderId, isOut);
    }

    [ClientRpc]
    public void RpcRunOutVerdict(int senderId, bool isOut)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcRunOutVerdict] RPC received. senderId={senderId} isOut={isOut}.");
        if (GroundController.instance != null)
            GroundController.instance.OnRunOutVerdictRelayed(senderId, isOut);
    }

    [Command(requiresAuthority = false)]
    public void CmdThrowTarget(int senderId, string target)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdThrowTarget] CMD received. senderId={senderId} target={target}.");
        RpcThrowTarget(senderId, target);
    }

    [ClientRpc]
    public void RpcThrowTarget(int senderId, string target)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcThrowTarget] RPC received. senderId={senderId} target={target}.");
        if (GroundController.instance != null)
            GroundController.instance.OnThrowTargetRelayed(senderId, target);
    }

    [Command(requiresAuthority = false)]
    public void CmdEnableContinueBtn(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdEnableContinueBtn] CMD received. senderId = {senderId}.");
        RpcEnableContinueBtn(senderId);
    }

    [ClientRpc]
    public void RpcEnableContinueBtn(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcEnableContinueBtn] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcEnableContinueBtn] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcEnableContinueBtn] Calling RPC_EnableContinueBtn on GroundController.");
        GroundController.instance.RPC_EnableContinueBtn();
    }
    [Command(requiresAuthority = false)]
    public void CmdUpdateCanBowlerBowl(int senderId, bool canBowlerBowl)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdUpdateCanBowlerBowl] CMD received. senderId = {senderId}, canBowlerBowl = {canBowlerBowl}.");
        RpcUpdateCanBowlerBowl(senderId, canBowlerBowl);
    }

    [ClientRpc]
    public void RpcUpdateCanBowlerBowl(int senderId, bool canBowlerBowl)
    {
        // ConstantsData_M.MpLog($"[CricketNetworkManager][RpcUpdateCanBowlerBowl] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            // ConstantsData_M.MpLog("[CricketNetworkManager][RpcUpdateCanBowlerBowl] Sender is local player. Returning early.");
            return;
        }
        // ConstantsData_M.MpLog($"[CricketNetworkManager][RpcUpdateCanBowlerBowl] Calling RPC_UpdateCanBowlerBowl with canBowlerBowl = {canBowlerBowl}.");
        GroundController.instance.RPC_UpdateCanBowlerBowl(canBowlerBowl);
    }


    [Command(requiresAuthority = false)]
    public void CmdLockSpeed(float fillAmount)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdLockSpeed] CMD received. fillAmount = {fillAmount}.");
        RpcLockSpeed(fillAmount);
    }

    [ClientRpc]
    public void RpcLockSpeed(float fillAmount)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcLockSpeed] RPC received. Calling RPC_LockSpeed with fillAmount = {fillAmount}.");
        BowlingControls.instance.RPC_LockSpeed(fillAmount);
    }

    [Command(requiresAuthority = false)]
    public void CmdLockAngle(float eulerAngleZ, int swingValue)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdLockAngle] CMD received. eulerAngleZ = {eulerAngleZ}, swingValue = {swingValue}.");
        RpcLockAngle(eulerAngleZ, swingValue);
    }

    [ClientRpc]
    public void RpcLockAngle(float eulerAngleZ, int swingValue)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcLockAngle] RPC received. Calling RPC_LockAngle. eulerAngleZ = {eulerAngleZ}, swingValue = {swingValue}.");
        BowlingControls.instance.RPC_LockAngle(eulerAngleZ, swingValue);
    }

    [Command(requiresAuthority = false)]
    public void CmdChangeBallData(int senderId, string score, string extraInfo)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdChangeBallData] CMD received. senderId = {senderId}, score = {score}, extraInfo = {extraInfo}.");
        RpcChangeBallData(senderId, score, extraInfo);
    }

    [ClientRpc]
    public void RpcChangeBallData(int senderId, string score, string extraInfo)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChangeBallData] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcChangeBallData] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcChangeBallData] Calling RPC_ChangeBallData. score = {score}, extraInfo = {extraInfo}.");
        ScoreBoardBallList.instance.RPC_ChangeBallData(score, extraInfo);
    }

    [Command(requiresAuthority = false)]
    public void CmdCallCheckForOverComplete(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdCallCheckForOverComplete] CMD received. senderId = {senderId}.");
        MatchFlow.Log("Cricket", $"end of over {(syncedMatchBalls < 0 ? 0 : syncedMatchBalls / 6)} — {syncedMatchScores}/{syncedMatchWickets}");
        RpcCallCheckForOverComplete(senderId);
    }

    [ClientRpc]
    public void RpcCallCheckForOverComplete(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCallCheckForOverComplete] RPC received. senderId={senderId} — triggering on all clients.");
        ScoreBoardBallList.instance.RPC_CallCheckForOverComplete();
    }

    [Command(requiresAuthority = false)]
    public void CmdSetSwingParameter(int swingValue)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSetSwingParameter] CMD received. swingValue = {swingValue}.");
        RpcSetSwingParameter(swingValue);
    }

    [ClientRpc]
    public void RpcSetSwingParameter(int swingValue)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcSetSwingParameter] RPC received. Calling RPC_SetSwingParameter with swingValue = {swingValue}.");
        BowlingControls.instance.RPC_SetSwingParameter(swingValue);
    }

    // DRS-board fix (#2): 3-arg form — carries the authoritative isOut so the remote (bowling) client can
    // render the OUT/NOT-OUT board instead of silently resolving. ALL call sites migrated atomically.
    [Command(requiresAuthority = false)]
    public void CmdDRS_Decision(int senderId, bool decision, bool isOut)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdDRS_Decision] CMD received. senderId = {senderId}, decision = {decision}, isOut = {isOut}.");
        MatchFlow.Log("Cricket", decision ? $"DRS review ({FlowWho(senderId)}): {(isOut ? "OUT" : "NOT OUT")}" : $"DRS: {FlowWho(senderId)} did not review");
        RpcDRS_Decision(senderId, decision, isOut);
    }

    [ClientRpc]
    public void RpcDRS_Decision(int senderId, bool decision, bool isOut)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcDRS_Decision] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}, decision = {decision}, isOut = {isOut}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcDRS_Decision] Sender is local player. Returning early.");
            return;
        }
        if (decision)
        {
            if (ConstantsData_M.useFullReviewReplay)
            {
                // FULL-REPLAY restore (kill-switch on): run the SAME ball-tracking replay on the remote client
                // (DRS.RPC_DRS_Decision(true) -> YesBtnClicked -> full replay), matching the reviewer. The remote
                // is not the reviewer (no review panel), so it won't re-relay; yesBtnClickedCalled guards re-entry.
                DRS.instance.RPC_DRS_Decision(true);
            }
            else if (GroundController.instance != null)
            {
                // Default cosmetic board: render the BOARD-ONLY authoritative OUT/NOT-OUT on the remote client.
                GroundController.instance.ShowDrsDecisionBoardOnly(isOut);
            }
        }
        else
        {
            // NO (review declined): unchanged dismiss path on the remote client.
            DRS.instance.RPC_DRS_Decision(false);
        }
    }

    // DRS review-timer SYNC: the batting authority relays the shared review-start NetworkTime so BOTH clients
    // run the identical 6s countdown off the synced clock (the timer was local-only -> different on each side).
    [Command(requiresAuthority = false)]
    public void CmdSyncDrsTimer(int senderId, double startNetTime)
    {
        RpcSyncDrsTimer(senderId, startNetTime);
    }

    [ClientRpc]
    public void RpcSyncDrsTimer(int senderId, double startNetTime)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return; // sender already anchored locally
        if (DRS.instance != null) DRS.instance.ApplySyncedTimerAnchor(startNetTime);
    }

    // Review-sync fix (#5): batting authority broadcasts the third-umpire/stumping review onset +
    // outcome so the bowling client shows the same review board instead of just the pitch.
    [Command(requiresAuthority = false)]
    public void CmdShowThirdUmpireReview(int senderId, bool playerStumped)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdShowThirdUmpireReview] CMD received. senderId = {senderId}, playerStumped = {playerStumped}.");
        MatchFlow.Log("Cricket", $"third umpire stumping review: {(playerStumped ? "OUT" : "NOT OUT")}");
        RpcShowThirdUmpireReview(senderId, playerStumped);
    }

    [ClientRpc]
    public void RpcShowThirdUmpireReview(int senderId, bool playerStumped)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcShowThirdUmpireReview] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcShowThirdUmpireReview] Sender is local player. Returning early.");
            return;
        }
        if (GroundController.instance != null)
            GroundController.instance.RPC_ShowThirdUmpireReview(playerStumped);
    }

    // Stump-fall sync (tester report #1): the batting authority relays its resolved stump-break clip + which
    // stump so the bowling follower shows the SAME stump falling (the clip is otherwise chosen from each
    // client's slightly-diverged local ball X, so screens could disagree on which stump breaks).
    [Command(requiresAuthority = false)]
    public void CmdSyncStumpAnim(int senderId, string clip, int stumpSide)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdSyncStumpAnim] CMD received. senderId = {senderId}, clip = {clip}, stumpSide = {stumpSide}.");
        RpcSyncStumpAnim(senderId, clip, stumpSide);
    }

    [ClientRpc]
    public void RpcSyncStumpAnim(int senderId, string clip, int stumpSide)
    {
        if (senderId == staticVariables.UserProfiledata.user._id)
            return;
        if (GroundController.instance != null)
            GroundController.instance.OnSyncedStumpAnim(clip, stumpSide);
    }

    [Command(requiresAuthority = false)]
    public void CmdPauseGame(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdPauseGame] CMD received. senderId = {senderId}.");
        RpcPauseGame(senderId);
    }

    [ClientRpc]
    public void RpcPauseGame(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcPauseGame] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcPauseGame] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcPauseGame] Calling RPC_PauseGame on Scoreboard.");
        Scoreboard.instance.RPC_PauseGame();
    }
    [Command(requiresAuthority = false)]
    public void CmdReBowlLastBall(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdReBowlLastBall] CMD received. senderId = {senderId}.");
        RpcReBowlLastBall(senderId);
    }

    [ClientRpc]
    public void RpcReBowlLastBall(int senderId)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcReBowlLastBall] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcReBowlLastBall] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog("[CricketNetworkManager][RpcReBowlLastBall] Calling RPC_ReBowlLastBall on ScoreBoardBallList.");
        ScoreBoardBallList.instance.RPC_ReBowlLastBall();
    }

    [Command(requiresAuthority = false)]
    public void CmdCallRebowl(int senderId, bool rebowl)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdCallRebowl] CMD received. senderId = {senderId}, rebowl = {rebowl}.");
        RpcCallRebowl(senderId, rebowl);
    }

    [ClientRpc]
    public void RpcCallRebowl(int senderId, bool rebowl)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCallRebowl] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcCallRebowl] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCallRebowl] Calling RPC_CallRebowl with rebowl = {rebowl}.");
        Launcher.Instance.RPC_CallRebowl(rebowl);
    }

    [Command(requiresAuthority = false)]
    public void CmdEqualScore(int senderId, int currentMatchScores, int ballCount, int matchBalls, int currentMatchWickets, int currentBallNumber)
    {
        RpcEqualScore(senderId, currentMatchScores, ballCount, matchBalls, currentMatchWickets, currentBallNumber);
    }

    [ClientRpc]
    public void RpcEqualScore(int senderId, int currentMatchScores, int ballCount, int matchBalls, int currentMatchWickets, int currentBallNumber)
    {
        if (senderId == staticVariables.UserProfiledata.user._id) return;
        Launcher.Instance.RPC_EqualScore(currentMatchScores, ballCount, matchBalls, currentMatchWickets, currentBallNumber);
    }

    /// <summary>
    /// Called by the BATTING PLAYER at the very TOP of UpdateCurrentBall, BEFORE any
    /// state is mutated. Relays the exact raw params to the bowling player via
    /// RpcBallOutcome so they can call UpdateCurrentBall with identical args — guaranteeing
    /// both sides run the same code path with the same inputs and reach the same result.
    /// This is the primary fix for ball sync (boundary vs keeper catch, score mismatch,
    /// wrong wickets, wrong target, wrong win/lose panel).
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdBallOutcome(int senderId, int validBall, int canCountBall, int runsScored,
                               int extraRun, int batsmanID, int isWicket, int wicketType,
                               int bowlerID, int catcherID, int batsmanOut, bool isBoundary)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdBallOutcome] senderId={senderId} " +
                  $"valid={validBall} runs={runsScored} extra={extraRun} wicket={isWicket}");
        if (MatchFlow.Enabled)
            MatchFlow.Log("Cricket", FlowBall(senderId, validBall, canCountBall, runsScored, extraRun, isWicket, wicketType, isBoundary));
        RpcBallOutcome(senderId, validBall, canCountBall, runsScored, extraRun,
                       batsmanID, isWicket, wicketType, bowlerID, catcherID, batsmanOut, isBoundary);
    }

    /// <summary>
    /// Received by ALL clients after every delivery. The batting player (senderId) skips
    /// it — they already processed the ball locally. The bowling player uses these exact
    /// params to call UpdateCurrentBall, which is now in the gate-open state waiting for
    /// this authoritative call. Both sides run identical code = identical outcome.
    /// </summary>
    [ClientRpc]
    public void RpcBallOutcome(int senderId, int validBall, int canCountBall, int runsScored,
                               int extraRun, int batsmanID, int isWicket, int wicketType,
                               int bowlerID, int catcherID, int batsmanOut, bool isBoundary)
    {
        // Six/four panel-on-batsman fix (#7): the batting AUTHORITY is the RpcBallOutcome SENDER, so it
        // hits this early-return BEFORE the unconditional bowling-side ShowMe below. Its own 4/6 banner
        // only comes from the deep UpdateCurrentBall -> InitAnimation(1) path, which is GUARDED by
        // (!flag3 && !overStepBall) — so on a milestone six (50/100/150) or a no-ball six the batsman's
        // SIX panel was missing/badly delayed while the bowler's showed promptly. Show the banner for the
        // batting authority here too (direct ShowMe only — purely visual, auto-hides ~2.5s; NOT
        // InitAnimation, which would wrongly schedule AnimationCompleted), then skip the rest.
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            if (isBoundary && (runsScored == 4 || runsScored == 6)
                && Singleton<BoundaryAnimation2>.instance != null)
            {
                Singleton<BoundaryAnimation2>.instance.ShowMe(runsScored == 6 ? 1 : 0);
            }
            return;
        }

        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcBallOutcome] Applying authoritative ball outcome: " +
                  $"valid={validBall} runs={runsScored} extra={extraRun} wicket={isWicket} boundary={isBoundary}");

        if (Singleton<GameData>.instance != null)
        {
            // Call UpdateCurrentBallFromNetwork (not UpdateCurrentBall directly).
            // This sets _ballOutcomeIsFromNetwork=true before forwarding so the
            // bowling player's suppression gate in UpdateCurrentBall lets it through.
            Singleton<GameData>.instance.UpdateCurrentBallFromNetwork(
                validBall, canCountBall, runsScored, extraRun,
                batsmanID, isWicket, wicketType, bowlerID, catcherID, batsmanOut, isBoundary);
        }

        // 4/6 banner fix (#5): show the boundary banner on the BOWLING side too. The bowling
        // player's score arrives via these authoritative RPCs; the deep UpdateCurrentBall path
        // drops the "FOUR"/"SIX" banner here (canPlayAnimation/branch divergence on the bowling
        // side), so previously only the batting player saw it. Call BoundaryAnimation2.ShowMe
        // DIRECTLY — it is purely visual and auto-hides after 2.5s — NOT InitAnimation/
        // StartAnimation, which ALSO schedule AnimationCompleted() and would wrongly advance the
        // network-driven bowling flow. Use authoritative isBoundary/runsScored (a score delta
        // would include extras and misfire). type 0 = FOUR, 1 = SIX.
        if (isBoundary && (runsScored == 4 || runsScored == 6)
            && Singleton<BoundaryAnimation2>.instance != null)
        {
            Singleton<BoundaryAnimation2>.instance.ShowMe(runsScored == 6 ? 1 : 0);
        }
    }

    /// <summary>
    /// Called by the BATTING PLAYER once per delivery, AFTER all score/wicket/ball-count
    /// state in UpdateCurrentBall is fully committed (runs added, WicketBall returned,
    /// AddBall called). Serves two purposes:
    ///   1. Persists authoritative post-ball state to SyncVars for future reconnects.
    ///   2. Broadcasts RpcCorrectBallState to the bowling player so their locally-simulated
    ///      (physics non-deterministic) outcome is overwritten with the batting side's
    ///      authoritative values — fixing score mismatch, wrong target, wrong win/lose.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdCorrectBallState(int senderId, int scores, int wickets, int ballCount,
                                     int matchBalls, int ballNumber,
                                     int strikerIndex, int nonStrikerIndex, int batsmanEntryIndex)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdCorrectBallState] senderId={senderId} " +
                  $"scores={scores} wickets={wickets} ballCount={ballCount} ballNum={ballNumber}");
        if (MatchFlow.Enabled)
        {
            MatchFlow.Log("Cricket", $"score {scores}/{wickets} after {FlowOvers(matchBalls)} ov");
            // The match result itself is decided on the clients (GameOverDisplay); the server can only see the
            // two certain 2nd-innings endings here: target passed, or the chasing side all out (10 wickets).
            if (syncedCurrentInnings == 1 && scores > syncedTeam1Scores)
            {
                MatchFlow.Log("Cricket", $"target {syncedTeam1Scores + 1} chased — {FlowWho(senderId)} wins with {10 - wickets} wicket(s) left");
                MatchFlow.SendResult(senderId.ToString(), "target chased", $"{syncedTeam1Scores}/{syncedTeam1Wickets} vs {scores}/{wickets} ({FlowOvers(matchBalls)} ov)");
            }
            else if (syncedCurrentInnings == 1 && wickets >= 10 && scores == syncedTeam1Scores)
            {
                MatchFlow.Log("Cricket", $"chasing side all out level on {scores} — match tied");
                MatchFlow.SendResult("draw", "tie — chasing side all out level", $"{syncedTeam1Scores}/{syncedTeam1Wickets} vs {scores}/{wickets} ({FlowOvers(matchBalls)} ov)");
            }
            else if (syncedCurrentInnings == 1 && wickets >= 10 && FlowOpponentId(senderId) != null)
            {
                MatchFlow.Log("Cricket", $"chasing side all out for {scores} — {FlowOpponent(senderId)} wins by {syncedTeam1Scores - scores} run(s)");
                MatchFlow.SendResult(FlowOpponentId(senderId), "chasing side all out", $"{syncedTeam1Scores}/{syncedTeam1Wickets} vs {scores}/{wickets} ({FlowOvers(matchBalls)} ov)");
            }
        }
        // Persist to SyncVars — reconnecting clients pick these up automatically.
        syncedMatchScores     = scores;
        syncedMatchWickets    = wickets;
        syncedBallCount       = ballCount;
        syncedMatchBalls      = matchBalls;
        syncedBallNumber      = ballNumber;
        syncedStrikerIndex    = strikerIndex;
        syncedNonStrikerIndex = nonStrikerIndex;
        syncedBatsmanEntryIndex = batsmanEntryIndex;
        // Broadcast correction to all clients; only the bowling player will act on it.
        RpcCorrectBallState(senderId, scores, wickets, ballCount, matchBalls, ballNumber,
                            strikerIndex, nonStrikerIndex, batsmanEntryIndex);
    }

    /// <summary>
    /// Received by ALL clients after each ball. The batting player (senderId) skips it;
    /// the bowling player applies the authoritative values, correcting any divergence
    /// caused by local physics non-determinism (e.g. keeper catch vs boundary).
    /// </summary>
    [ClientRpc]
    public void RpcCorrectBallState(int senderId, int scores, int wickets, int ballCount,
                                     int matchBalls, int ballNumber,
                                     int strikerIndex, int nonStrikerIndex, int batsmanEntryIndex)
    {
        // Batting player already has the correct state — nothing to do.
        if (senderId == staticVariables.UserProfiledata.user._id) return;

        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCorrectBallState] Correcting bowling-side state: " +
                  $"scores={scores} wickets={wickets} ballCount={ballCount} ballNum={ballNumber}");

        // Override locally-computed (wrong physics) state with batting authority.
        if (CONTROLLER.TeamList != null)
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores = scores;
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets = wickets;
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls = matchBalls;
        }
        // Do NOT let the authoritative count outrun the chips it is supposed to count. This used to assign
        // ballCount alone, leaving ballList untouched — and every extra label is placed by index, so the two
        // drifting apart puts "nb"/"wd" on the wrong row (the player-name rows). Extras are now indexed off
        // ballList, and the count only moves with it; a genuine divergence is logged rather than baked in.
        if (Singleton<ScoreBoardBallList>.instance != null)
        {
            var _sbl = Singleton<ScoreBoardBallList>.instance;
            if (_sbl.ballList != null && ballCount != _sbl.ballList.Count)
                ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCorrectBallState] authority ballCount={ballCount} but this client has {_sbl.ballList.Count} chips — keeping the chips as the source of truth.");
            _sbl.ballCount = (_sbl.ballList != null) ? _sbl.ballList.Count : ballCount;
        }
        if (Singleton<GameData>.instance != null)
            Singleton<GameData>.instance.currentBallNumber = ballNumber;

        CONTROLLER.StrikerIndex    = strikerIndex;
        CONTROLLER.NonStrikerIndex = nonStrikerIndex;
        if (Singleton<GameData>.instance != null)
            Singleton<GameData>.instance.newBatsmanEntryIndex = batsmanEntryIndex;

        // Refresh computed display strings so scoreboard shows correct values.
        if (Singleton<GameData>.instance != null && CONTROLLER.TeamList != null)
        {
            Singleton<GameData>.instance.scoreDisplayString =
                scores + "/" + wickets;
            Singleton<GameData>.instance.oversDisplayString =
                Singleton<GameData>.instance.GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
        }

        // Recalculate TargetToChase in 2nd innings so win/lose uses the correct target.
        if (CONTROLLER.currentInnings == 1 && CONTROLLER.PlayModeSelected != 7
            && CONTROLLER.TeamList != null)
        {
            CONTROLLER.TargetToChase =
                CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores + 1;
        }

        // Force the Scoreboard to redraw with the corrected values.
        if (Singleton<Scoreboard>.instance != null)
            Singleton<Scoreboard>.instance.UpdateScoreCard();
    }

    [Command(requiresAuthority = false)]
    public void CmdCheckScore(int senderId, int currentMatchScores, int ballCount, int currentMatchWickets)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdCheckScore] CMD received. senderId = {senderId}, currentMatchScores = {currentMatchScores}, ballCount = {ballCount}, currentMatchWickets = {currentMatchWickets}.");
        RpcCheckScore(senderId, currentMatchScores, ballCount, currentMatchWickets);
    }

    [ClientRpc]
    public void RpcCheckScore(int senderId, int currentMatchScores, int ballCount, int currentMatchWickets)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCheckScore] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcCheckScore] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCheckScore] Calling RPC_CheckScore. scores = {currentMatchScores}, balls = {ballCount}, wickets = {currentMatchWickets}.");
        Launcher.Instance.RPC_CheckScore(currentMatchScores, ballCount, currentMatchWickets);
    }
    [Command(requiresAuthority = false)]
    public void CmdCheckScene(int senderId, string sceneName, int meFirstBatting)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][CmdCheckScene] CMD received. senderId = {senderId}, sceneName = {sceneName}, meFirstBatting = {meFirstBatting}.");
        RpcCheckScene(senderId, sceneName, meFirstBatting);
    }

    [ClientRpc]
    public void RpcCheckScene(int senderId, string sceneName, int meFirstBatting)
    {
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCheckScene] RPC received. senderId = {senderId}, localId = {staticVariables.UserProfiledata.user._id}.");
        if (senderId == staticVariables.UserProfiledata.user._id)
        {
            ConstantsData_M.MpLog("[CricketNetworkManager][RpcCheckScene] Sender is local player. Returning early.");
            return;
        }
        ConstantsData_M.MpLog($"[CricketNetworkManager][RpcCheckScene] Calling RPC_CheckScene. sceneName = {sceneName}, meFirstBatting = {meFirstBatting}.");
        Launcher.Instance.RPC_CheckScene(sceneName, meFirstBatting);
    }

    // ─── Match-flow log helpers ("[Cricket Flow]" lines, see MatchFlow) ──────────────────────────────
    // Wording only: read state, never change it. MatchFlow writes only on the dedicated server.

    /// <summary>Display name for a user id sent by a client (creator / joiner name).</summary>
    static string FlowWho(int id) => MatchFlow.Who(id.ToString());

    /// <summary>The OTHER player's id (creator ↔ joiner), or null when it cannot be mapped.</summary>
    static string FlowOpponentId(int id)
    {
        var ngm = NetworkGameManager.Instance;
        if (ngm == null || ngm.creatorData == null || ngm.joinerData == null) return null;
        string s = id.ToString();
        if (ngm.creatorData.playerId == s) return ngm.joinerData.playerId;
        if (ngm.joinerData.playerId == s) return ngm.creatorData.playerId;
        return null;
    }

    static string FlowOpponent(int id)
    {
        string o = FlowOpponentId(id);
        return string.IsNullOrEmpty(o) ? "opponent" : MatchFlow.Who(o);
    }

    static string FlowPlayers()
    {
        var ngm = NetworkGameManager.Instance;
        if (ngm == null || ngm.creatorData == null || ngm.joinerData == null) return "?";
        return $"{MatchFlow.Who(ngm.creatorData.playerId)} vs {MatchFlow.Who(ngm.joinerData.playerId)}";
    }

    /// <summary>Legal balls → "overs.balls" (e.g. 15 → "2.3").</summary>
    static string FlowOvers(int balls) => balls < 0 ? "0.0" : $"{balls / 6}.{balls % 6}";

    static string FlowWicket(int wicketType)
    {
        switch (wicketType)
        {
            case 1: return "bowled";
            case 2: return "lbw";
            case 3: return "caught";
            case 4: return "run out";
            case 5: return "caught behind";
            case 6: return "stumped";
            default: return "out";
        }
    }

    /// <summary>One delivery from CmdBallOutcome's raw params, e.g. "over 2.3 — Sara bowls, Ali hits FOUR".</summary>
    string FlowBall(int senderId, int validBall, int canCountBall, int runsScored, int extraRun,
                    int isWicket, int wicketType, bool isBoundary)
    {
        string bat = FlowWho(senderId);
        int b = syncedMatchBalls < 0 ? 0 : syncedMatchBalls;   // legal balls BEFORE this delivery
        string what;
        if (isWicket == 1) what = $"WICKET — {bat} {FlowWicket(wicketType)}" + (runsScored > 0 ? $" ({runsScored} run{(runsScored == 1 ? "" : "s")} completed)" : "");
        else if (isBoundary && runsScored == 6) what = $"{bat} hits SIX";
        else if (isBoundary && runsScored == 4) what = $"{bat} hits FOUR";
        else if (runsScored > 0) what = $"{bat} takes {runsScored} run{(runsScored == 1 ? "" : "s")}";
        else what = $"{bat} — dot ball";
        string extras = "";
        if (validBall == 0) extras = $", {(canCountBall == 0 ? "WIDE" : "NO-BALL")} (+{extraRun})";
        else if (extraRun > 0) extras = $", +{extraRun} extras";
        return $"over {b / 6}.{b % 6 + 1} — {FlowOpponent(senderId)} bowls, {what}{extras}";
    }

    /// <summary>Score summary from the batting authority's synced totals (t0 = batting side, t1 = bowling side).</summary>
    string FlowScoreSummary()
    {
        if (syncedCurrentInnings < 0) return "";
        if (syncedCurrentInnings == 0)
            return $"innings 1: {syncedTeam0Scores}/{syncedTeam0Wickets} ({FlowOvers(syncedTeam0Balls)} ov)";
        return $"innings 1: {syncedTeam1Scores}/{syncedTeam1Wickets}, innings 2: {syncedTeam0Scores}/{syncedTeam0Wickets} ({FlowOvers(syncedTeam0Balls)} ov), target {syncedTeam1Scores + 1}";
    }

}

public enum PanelsInfo
{
    None = 0,   // default — first join, no special panel open
    BattingScoreCard = 1,
    BowlingScoreCard = 2,
    AfterOverSummary = 3,
    // Pre-game (MainMenu scene) states — set by Cmd methods so reconnecting client can restore
    WaitingForOpponent = 4, // both connected, over-selection timer running
    TeamSelection = 5, // team selection screen open
    Toss = 6, // toss / bat-or-bowl choice screen open
}