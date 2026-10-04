// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.Reconnect — mid-match reconnect recovery. NOTE: a Mirror reconnect RELOADS the
// Ground scene — every non-static instance field resets; only statics + SyncVars survive. State is
// restored from the server snapshot (see CricketNetworkManager) + CancelInFlightDeliveryForReconnect
// cancels a half-flown delivery and re-arms the bowler (state -2 is the ONLY auto-bowl arm site).
// ════════════════════════════════════════════════════════════════════════════════════════════
using Beebyte.Obfuscator;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Cricket;
using UnityEngine.Serialization;

// Partial of GroundController — thematic split (see GroundController.cs for fields + lifecycle).
public partial class GroundController
{
    public void MarkOutcomeCommittedThisDelivery() { _outcomeCommittedThisDelivery = true; }

    // Reconnect snapshot bridge for the free-hit pair. Both are plain instance fields, so a reconnect
    // reloads Ground and drops the free hit a no-ball just awarded — the clients then disagree about
    // whether the next delivery can take a wicket. Captured/restored via MatchStateSnapshot.freeHitActive.
    public bool FreeHitActiveForSnapshot => isFreeHitActive || freeHit;
    public void RestoreFreeHitFromSnapshot(bool active)
    {
        isFreeHitActive = active;
        freeHit = active;
    }
    // Authoritative per-delivery clear, called from GameData.UpdateCurrentBall once a legal ball has been
    // played. Clearing `freeHit` alone is NOT enough: FreeHitActiveForSnapshot above reads isFreeHitActive
    // FIRST, and isFreeHitActive is private with eleven scattered per-outcome clears that a plain dot ball
    // never reaches. So the flag stayed set, every subsequent snapshot pushed freeHitActive=true, and the
    // next reconnect called RestoreFreeHitFromSnapshot(true) — re-arming a free hit that had already been
    // consumed. The 06-08 logs show it plainly: "Per-delivery rule state restored: freeHit=True" at 9453,
    // 12253 and 13823, each followed by another "Free hit consumed" a few hundred lines later.
    public void ClearFreeHitAfterLegalDelivery()
    {
        isFreeHitActive = false;
        freeHit = false;
        // isLineFreeHitActive is a FOURTH free-hit flag — the wide/line variant — cleared in its own set of
        // scattered keeper branches. Leaving it set was the whole of the tester's "again free hit ka panel
        // aata hai bowler side pe AUR ball theek count hoti hai": the counting was right because the flags
        // above were cleared, but the next delivery's setup re-showed the banner off this one:
        //     GroundController.cs ~3485
        //     if (!isReplayModeActive && lastBowledBallType == "overstep" && isLineFreeHitActive)
        //         Singleton<Scoreboard>.instance.showFreeHitBg(canShow: true);
        // A legal delivery ends every kind of free hit, so this method owns all of them.
        isLineFreeHitActive = false;
    }

    [FormerlySerializedAs("TookRun")] public bool hasTakenRun;

    float takeRunDuration;

    float takeRunTimestamp;

    [FormerlySerializedAs("CancelledRun")] public bool hasCancelledRun;

    float cancelRunDuration;

    float cancelRunTimestamp;

    private const int maxRetryAttempts = 3; // Number of retry attempts for acknowledgement (Mirror guarantees reliable delivery)
    private float retryDelayDuration = 1f;

    private bool isBowlingAcknowledged;
    [SerializeField] private GameObject StadiumObject;
    public void MaxPowerUp()
    {
        agilityMultiplier = 0.2f;
        controlMultiplier = 0.2f;
        powerMultiplier = 0.2f;
        isEnhancedModeActive = false;
    }

    public void CheckIfOpponentIsConnected()
    {
        if (CONTROLLER.PlayModeSelected == 8)
        {
            return;
        }
        if (Singleton<GameData>.instance.waitingForOpponentText.text != "Connection Lost...." || Singleton<GameData>.instance.waitingForOpponentText.text != "Opponent Left the Match...")
        {
            StartCoroutine("SendAcknowledgementWithRetry");
        }
    }

    public void LeaveRoomAutomatically(bool Connected)
    {
        StartCoroutine(LeaveRoomAfterDelay(Connected));
    }

    public void Disconnect()
    {
    }

    //Photon Removal [PunRPC]
    public void RPC_OtherPlayerLeftTheRoom()
    {
        StopCoroutine("SendAcknowledgementWithRetry");

        if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
    && (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
        {
            Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
        }
        else
        {
            Singleton<GameData>.instance.waitingForOpponentText1.text = "Opponent Left the Match...";
        }
        Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(true);
        if (Singleton<GameData>.instance.waitingForOpponentText1.text == "Loading")
        {
            StartCoroutine(Launcher.Instance.AnimateDots());
        }
        Time.timeScale = 1f;
        Invoke("AutomaticLeaveRoom", 5f);
    }

    public void LeaveGame()
    {
        Launcher.Instance.StopSyncing();
    }

    public void AutomaticShowResult()
    {
        Singleton<GameOverScreen>.instance.Hide(false);

        Singleton<GameData>.instance.waitingForOpponentPanel.SetActive(false);
        Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(false);

        Launcher.Instance.StopSyncing();
        CONTROLLER.screenToDisplay = "landingPage";
        Singleton<PauseGameScreen>.instance.hideAll();
        Singleton<PauseGameScreen>.instance.midPageGO.SetActive(value: false);
        Time.timeScale = 1f;

    }

    public void AutomaticLeaveRoom()
    {

        Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(false);

        Launcher.Instance.StopSyncing();

        Time.timeScale = 1f;
        Singleton<GameData>.instance.GameQuitted();

    }

    // Online DRS-review "ACTION REPLAY stuck + 5-sec auto-bowl" fix.
    // Offline, the review replay is driven to completion by DRSCameraScript.showUmpireAfterDrs, which
    // only fires when the replay ball physically crosses the stumps. Online, the bowling client
    // suppresses ball physics and the batting client has already consumed the live delivery, so on
    // NEITHER client does the replay reach that trigger: reviewReplay/canShowReplay/DRSreplay and the
    // "ACTION REPLAY" text stay set, the bowler is blocked behind the overlay, and the auto-bowl timer
    // delivers the next ball by itself. This watchdog clears the stuck review overlay after a short
    // bounded wait and resumes the live view (mirroring DRS.NoBtnClicked's resume: currentActionState
    // = 3, umpire view back on). It deliberately does NOT replay the umpire-decision animation
    // (outViaDRS is derived from the absent ball-tracking and would be stale -> could contradict the
    // authoritative RpcBallOutcome, which already recorded this ball over the wire) and does NOT touch
    // any scoring/over logic. Harmless offline / when the review completes normally (it self-cancels).
    public void StartMpReviewWatchdog(float timeoutSeconds = 4f)
    {
        if (CONTROLLER.PlayModeSelected != 8) return;
        if (_mpReviewWatchdog != null) StopCoroutine(_mpReviewWatchdog);
        _mpReviewWatchdog = StartCoroutine(MpReviewWatchdogRoutine(timeoutSeconds));
    }

    private IEnumerator MpReviewWatchdogRoutine(float timeoutSeconds)
    {
        float elapsed = 0f;
        // unscaled: the review can run under an altered Time.timeScale
        while (elapsed < timeoutSeconds && CONTROLLER.reviewReplay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _mpReviewWatchdog = null;
        if (!CONTROLLER.reviewReplay)
        {
            yield break; // review resolved normally -> nothing is stuck
        }
        ForceResolveStuckReview();
    }

    /// <summary>
    /// Called on the STAYING client when the opponent finishes reconnecting. ShowReconnectHudForBowler
    /// re-broadcasts the bowling spot only when the BOWLER is the one who rejoined; if the BATSMAN dropped,
    /// nothing re-sent it and the rejoining batter came back with the scene-default marker — two different
    /// red points, and the next delivery pitched somewhere else on that screen ("red indicators ki position
    /// different dono screens py, jis se ball different"). The bowler owns the spot, so relay ours here.
    /// The server-side de-dup cache is cleared in CmdRequestMatchSnapshot so this send is not swallowed.
    /// </summary>
    public void ReBroadcastBowlingSpotForOpponentReconnect()
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return;
        if (!CricketNetworkManager.ReadyToSend || bowlingSpot == null) return;

        Vector3 spot = bowlingSpot.position;
        CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, spot);
        ConstantsData_M.MpLog($"[GroundController][ReBroadcastBowlingSpot] Bowling side re-sent spot {spot} for the rejoining opponent.");
    }

    // NOTE: an attempt to make this watchdog fire on EVERY delivery (not just hit balls) was REVERTED
    // on 2026-07-25. ForceResolveStuckReview is the REVIEW/DRS resolver — it flushes pending
    // caught-behind OUT outcomes and tears down review/camera state. Running it on leaves/misses, where
    // no review is in progress, produced spurious outcomes and camera teardown: "ek side pe 4, doosri
    // side ball fielder ke haath", a review panel appearing on one screen, and a stuck camera angle.
    // A stuck non-hit delivery needs a resolver that does NOT go through the review system.

    public void StartMpOutcomeWatchdog()
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        if (_mpOutcomeWatchdog != null) StopCoroutine(_mpOutcomeWatchdog);
        _mpOutcomeWatchdog = StartCoroutine(MpOutcomeWatchdogRoutine());
    }

    private IEnumerator MpOutcomeWatchdogRoutine()
    {
        int startBalls = SafeBattingBalls();
        float elapsed = 0f;
        while (elapsed < 8f)
        {
            elapsed += Time.unscaledDeltaTime;
            // Resolve the INSTANT the authoritative outcome ADVANCES the ball count. SafeBattingBalls() reads
            // TeamList[BattingTeamIndex].currentMatchBalls, restored from the SyncVar snapshot on reconnect — a
            // DURABLE signal, not a fresh instance flag a scene-reload wipes.
            // The old `|| !isBallHit` self-cancel made this quit on the FIRST tick for a LEAVE / dot / keeper-take
            // (isBallHit==false) — which are EXACTLY the deliveries that hang when the batting authority
            // disconnects after the ball is bowled but before RpcBallOutcome lands (tester "game stuck after
            // disconnection": bowling side frozen on "suppressing local physics, waiting for authoritative
            // outcome"; device logs game_stuck_s2 / game_get_stuck / game_got_stuck). Removed so the watchdog
            // actually covers non-hit balls; a genuine outcome still exits early via the ball-advance check.
            if (SafeBattingBalls() != startBalls)
            {
                _mpOutcomeWatchdog = null;
                yield break;
            }
            yield return null;
        }
        _mpOutcomeWatchdog = null;
        // Timeout: 8s and the authoritative ball count NEVER advanced. Only act on a genuinely stuck
        // bowling-suppressed delivery, keyed off DURABLE state (never a scene-reload-wiped instance bool):
        // we are the bowling follower, the ball was released, and no outcome committed. If the outcome had
        // committed OR the ball had advanced we would have exited above. _outcomeCommittedThisDelivery defaults
        // FALSE after a scene reload — the SAFE "needs recovery" direction.
        bool _bowlingSuppressedStuck = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && isBallReleased && !_outcomeCommittedThisDelivery && !isReplayModeActive;
        if (!_bowlingSuppressedStuck) yield break; ;
        // A REAL deferred review/DRS/keeper decision → the review resolver is the correct, unchanged path.
        //
        // `isWicketKeeperActive` is NOT that signal. ActivateWicketKeeper() sets it on ordinary deliveries, so
        // it is true for most balls — and routing an ordinary ball into ForceResolveStuckReview is precisely
        // what the 2026-07-25 revert established must not happen (it tears the camera down and flushes a
        // caught-behind that was never appealed). The 03-08 logs catch it doing exactly that after a
        // reconnect: "[ForceResolveStuckReview] teardown: wkStatus='catchMissed' ... actionState=3" —
        // catchMissed is the keeper missing the ball, not a decision anyone is waiting on. That is the
        // tester's "turn got miss in 2 or 3 cases after reconnection".
        //
        // The batting-side watchdog was already narrowed to genuine keeper DECISION statuses; this one was
        // missed. Same gate, same helper.
        bool _keeperDecisionPending = isWicketKeeperActive && IsKeeperDecisionStatus(currentWicketKeeperStatus);
        bool _reviewPending = isDRSEnabled || _keeperDecisionPending || mpCaughtBehindOutPending
            || isUltraEdgeCutscenePlaying || CONTROLLER.reviewReplay || CONTROLLER.canShowReplay;
        if (_reviewPending)
        {
            ConstantsData_M.MpLog($"[GroundController][MpOutcomeWatchdog] No authoritative outcome after 8s — review pending, resolving via review path. drs={isDRSEnabled} keeperActive={isWicketKeeperActive} keeperDecision={_keeperDecisionPending} keeperStatus='{currentWicketKeeperStatus}'");
            ForceResolveStuckReview();
            yield break; ;
        }
        // Plain leave / dot / miss / keeper-take whose authoritative RpcBallOutcome NEVER arrived (batting
        // authority dropped post-bowl). Use the REVIEW-FREE recovery — NOT ForceResolveStuckReview, which the
        // 2026-07-25 revert (see the note above StartMpOutcomeWatchdog) proved harmful on non-review balls
        // (spurious 4s / "ball fielder ke haath" desync / stuck camera). The ball never counted anywhere
        // (SafeBattingBalls did not advance, else we'd have exited), so re-arming re-bowls it cleanly — the same
        // proven recovery the between-balls / delivery watchdogs use (ReArmStalledDeliveryNonDestructive). No
        // double-count: the un-counted ball simply re-bowls and counts once when re-played.
        ConstantsData_M.MpLog("[GroundController][MpOutcomeWatchdog] No authoritative outcome after 8s (non-review) — non-destructive re-arm (ball never counted; will re-bowl).");
        ReArmStalledDeliveryNonDestructive();
    }

    // BATTING-AUTHORITY delivery watchdog.
    //
    // The existing MpOutcomeWatchdog only covers the BOWLING follower, and only arms when UpdateCurrentBall
    // is actually reached and suppressed. That leaves the case the tester reproduces reliably: a straight
    // centre shot with no loft, where the ball rolls BETWEEN the two converging chasers and NOBODY collects
    // it (29-07 logs a205a70f/415e9c5d end on exactly that — two fielders sent to the SAME chase point from
    // opposite sides, then silence). The batting authority never computes an outcome, so it never sends one
    // AND the bowling side's watchdog is never armed. Both screens sit there forever.
    //
    // Deliberately generous at 20s: this side legitimately takes time (boundary stay windows, replays, DRS,
    // run-outs), and a watchdog that fires early on a healthy delivery is far worse than one that fires late
    // — the 2026-07-25 revert was exactly that mistake. Recovery is the SAME proven non-destructive re-arm
    // the bowling watchdog uses: the ball never counted anywhere, so it simply re-bowls and counts once.
    private Coroutine _battingDeliveryWatchdog;

    // Keeper statuses that represent a real pending DECISION (an appeal, a third-umpire referral, a signal
    // the umpire still owes). Everything else — waitToCollect, collectTheThrow, catchAttempt, catchEnd,
    // loopEnd, finish — is ordinary keeper flow and must never block stall recovery.
    private static bool IsKeeperDecisionStatus(string status)
    {
        return status == "decisionPending"
            || status == "waitForCaughtBehindResult"
            || status == "waitForStumpingResult"
            || status == "waitForResult"
            || status == "waitForWideSignal"
            || status == "showWideSignalBeforeStumpingResult"
            || status == "stumpingAppeal"
            || status == "stumpingAttempt"
            || status == "runOutAppeal"
            || status == "CaughtBehind";
    }

    public void StartBattingDeliveryWatchdog()
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BattingTeamIndex) return;
        if (_battingDeliveryWatchdog != null) StopCoroutine(_battingDeliveryWatchdog);
        _battingDeliveryWatchdog = StartCoroutine(BattingDeliveryWatchdogRoutine());
    }

    private IEnumerator BattingDeliveryWatchdogRoutine()
    {
        int startBalls = SafeBattingBalls();
        float elapsed = 0f;
        while (elapsed < 20f)
        {
            elapsed += Time.unscaledDeltaTime;
            // Same durable exit signal as the bowling watchdog: the authoritative ball count advancing means
            // the delivery resolved, whatever the outcome was.
            if (SafeBattingBalls() != startBalls)
            {
                _battingDeliveryWatchdog = null;
                yield break;
            }
            yield return null;
        }
        _battingDeliveryWatchdog = null;
        // Only act on a genuinely unresolved delivery on THIS side.
        bool _stillStuck = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
            && isBallReleased && !_outcomeCommittedThisDelivery && !isReplayModeActive;
        if (!_stillStuck) yield break;
        // Never disturb a real review/DRS/keeper decision — those own their own resolution path and the
        // 2026-07-25 revert proved that forcing them here corrupts the next delivery.
        //
        // But `isWicketKeeperActive` alone is NOT a review: ActivateWicketKeeper() sets it on ordinary
        // deliveries, so it is true for most balls. Bug a461f541 (31-07) is exactly that — a straight centre
        // drive where both chasers were sent to the SAME point (cp(0.6,-48.7) from x=-14.6 and x=+17.1), no
        // fielder collected the ball, and this watchdog then stood down on
        //   drs=False keeper=True caughtBehind=False ultraEdge=False reviewReplay=False canShowReplay=False
        //   keeperStatus='waitToCollect' ballStatus='shotSuccess'
        // 'waitToCollect' is the keeper waiting for a ball that never arrives — the signature of the stall
        // itself, not of a decision. Requiring a genuine keeper DECISION status keeps every real review
        // untouched while letting the recovery run on the balls that are actually stuck.
        bool _keeperDecisionPending = isWicketKeeperActive && IsKeeperDecisionStatus(currentWicketKeeperStatus);
        if (isDRSEnabled || _keeperDecisionPending || mpCaughtBehindOutPending
            || isUltraEdgeCutscenePlaying || CONTROLLER.reviewReplay || CONTROLLER.canShowReplay)
        {
            // Name the flags. This branch already fired once in the 30-07 logs on a delivery that never
            // resolved (tester: "ball center m sy guzri, dono players ny nhi pkri, game stuck"), so one of
            // these is STALE-true and permanently disarms the watchdog for that delivery. Without knowing
            // which one, the only options are to guess or to force-resolve — and forcing the review path on
            // a non-review ball is exactly what had to be reverted on 2026-07-25.
            ConstantsData_M.MpLog($"[GroundController][BattingDeliveryWatchdog] 20s with no outcome, but a review/DRS is pending — leaving it to the review path. drs={isDRSEnabled} keeperActive={isWicketKeeperActive} keeperDecision={_keeperDecisionPending} caughtBehind={mpCaughtBehindOutPending} ultraEdge={isUltraEdgeCutscenePlaying} reviewReplay={CONTROLLER.reviewReplay} canShowReplay={CONTROLLER.canShowReplay} keeperStatus='{currentWicketKeeperStatus}' ballStatus='{currentBallStatus}'");
            yield break;
        }
        // Also leave a reconnect alone: that machinery has its own restore + re-arm.
        if (Launcher.Instance != null && Launcher.Instance.IsReConnecting())
        {
            ConstantsData_M.MpLog("[GroundController][BattingDeliveryWatchdog] 20s with no outcome, but a reconnect is in progress — the restore owns recovery.");
            yield break;
        }
        ConstantsData_M.MpLog($"[GroundController][BattingDeliveryWatchdog] No outcome 20s after release and no fielder ever resolved the ball (ballStatus='{currentBallStatus}' actionState={currentActionState}) — non-destructive re-arm; the ball never counted and will re-bowl.");
        ReArmStalledDeliveryNonDestructive();
    }

    // Mid-delivery disconnect fix (#6): the in-flight ball (isBallReleased / streamed release+position)
    // is purely transient Cmd/Rpc traffic — it is NOT a SyncVar, so reconnect never restores it. The
    // disconnecting client scene-reloads to a clean pre-delivery state, but the STAYING client is left
    // frozen with a half-flown ball and a partial ACK barrier (no reconnect path ever resets it). This
    // cancels an in-flight delivery and re-arms the over so the ball cleanly RE-BOWLS on both clients
    // (matching the game's own "this ball will be reballed" promise). No-op when not mid-flight.
    public bool IsMidDelivery()
    {
        return isBallReleased && !isReplayModeActive && currentBallStatus != string.Empty;
    }

    public void CancelInFlightDeliveryForReconnect()
    {
        // Unconditional ENTRY diagnostic (MpLog → ritu_mp file): proves the build has this code and shows the
        // EXACT state so we can see which STEP-2 gate (if any) a reconnect-hang slips through. Debug.Log does
        // NOT reach the ritu_mp file — only MpLog does — so all reconnect diagnostics MUST use MpLog.
        ConstantsData_M.MpLog($"[GroundController][CancelInFlight] ENTRY: released={isBallReleased} actionState={currentActionState} ballStatus='{currentBallStatus}' replay={isReplayModeActive} outcomeCommitted={_outcomeCommittedThisDelivery} IsMidDelivery={IsMidDelivery()} myTeam={CONTROLLER.myTeamIndex} bat={CONTROLLER.BattingTeamIndex} bowl={CONTROLLER.BowlingTeamIndex} canBowl={CONTROLLER.CanBowlerBowl} playMode={CONTROLLER.PlayModeSelected}");
        // ── Unified reconnect clean-recovery (option 2) ──────────────────────────────────────────
        // STEP 1 (UNCONDITIONAL, safe): break any stuck per-ball ACK barrier + its watchdog + a stuck
        // auto-bowl timer. This was previously gated behind IsMidDelivery(), so a reconnect that landed
        // in a POST-WICKET / over-transition state (NOT mid-flight) left the barrier/auto-bowl loop
        // running → the bowler auto-bowled, the (reconnecting) batsman couldn't play, the outcome never
        // committed, RpcCallRebowl fired, and it looped forever with the score frozen (log: wicket 5/2 +
        // 2× ApplicationPause reconnects → endless auto-bowl/rebowl). Clearing these just drops the STUCK
        // barriers; the normal flow re-arms them when the bowler is genuinely ready for the next ball, so
        // it is safe to run even on an already-clean reconnect.
        if (ackWatchdog != null) { StopCoroutine(ackWatchdog); ackWatchdog = null; }
        ackBarrierResolved = true;
        CONTROLLER.OnWaitScreen = 0;       // drop any partial per-ball ACK barrier (loop driver)
        // Reconnect-hang fix: kill the auto-bowl timer's tween BEFORE deactivating it so a later re-arm can't
        // append to a killed Sequence (the "inactive/killed Sequence" spam that left CallAutomaticBall never
        // scheduled → bowler never delivered). The hardened SetAutoBowlingTimer re-arms a fresh one next frame.
        if (animationSequence != null && animationSequence.IsActive()) { animationSequence.Kill(); animationSequence = null; }
        if (timerObject != null) timerObject.SetActive(false);  // stop a stuck auto-bowl timer (re-arms normally)

        // Bug-fix S7 (stale "extra ball" near the batsman after reconnect): hide the single in-flight matchBall
        // on EVERY reconnect-cleanup entry. The clean-state early-return below only re-armed bat colliders and
        // never hid the ball, so the interrupted delivery's ball stayed rendered near the batsman until the NEXT
        // delivery's ResetAll. Idempotent — after STEP-2's ResetAll the matchBall stays HIDDEN parked at the
        // release point (the in-hand PROP shows instead); the actual release re-shows the matchBall.
        ShowBall(status: false);
        // Reconnect STUCK-BALL guard (tester: "bowler reconnect while/just before releasing -> ball stuck at the
        // release point, and next ball that stuck ball acts as the bowled ball"). ShowBall(false) HIDES the
        // in-flight ball but its transform stayed at the release point; if this reconnect lands in the clean
        // early-return path below (ResetAll never runs), the ball is re-shown NEXT delivery still at the release
        // point and gets bowled from there. Snap the hidden ball back to its in-hand origin here, UNCONDITIONALLY.
        // Position-only: does NOT touch isBallReleased / currentActionState / the sim, so the STEP-2 gates below
        // still classify the delivery exactly as before (and STEP-2's ResetAll re-homes it again — idempotent).
        if (matchBallTransform != null)
        {
            matchBallTransform.position = initialBallPosition;
            temporaryPosition = initialBallPosition;
        }

        // STEP 2 (delivery IN PROGRESS — FLIGHT or RUN-UP): put the ball back in the bowler's hand and
        // re-arm the over. Gated so we don't disturb a legit between-balls / new-batsman-walk-in /
        // scorecard state (those are NOT a started delivery) — only the ACK/auto-bowl loop above is
        // cleared there, letting the normal next-ball flow resume cleanly.
        //   - IsMidDelivery(): ball already released, in flight.
        //   - _midRunUp: the bowler had STARTED the run-up (currentActionState==1) but the ball was NOT
        //     yet released when the disconnect hit. IsMidDelivery() misses this (isBallReleased=false),
        //     so the reconnecting bowler came back to a LIMBO (run-up gone, never re-armed) → a waiting
        //     panel showed and the game hung (tester: "bowler run-up start karte hi disconnect → panel +
        //     stuck"). Reset here too so the over re-arms and the ball is re-bowled cleanly.
        // State 1 = BowlerWaiting; state 2 = the run-up ANIMATION actually playing (set at the end of the
        // run-up setup, GroundController.Delivery.cs ~4869). The original check only caught state 1, so a
        // disconnect DURING the visible run-up (state 2) slipped through on the STAYING client — its
        // _bowlingOrphan catch needs BowlingTeamIndex==myTeamIndex (false on the batting side) and the other
        // predicates need a released ball — so nothing reset the bowler and it froze MID-PITCH on the batting
        // screen, still frozen on the next delivery (tester #2b: "bowler disconnect while running → on the
        // batter side bowler stuck in middle of pitch"). Include state 2 so ResetAll re-homes the bowler.
        bool _midRunUp = (currentActionState == 1 || currentActionState == 2) && !isBallReleased && !isReplayModeActive;
        //   - _orphanReleased: the ball was RELEASED (isBallReleased=true) but its outcome NEVER committed
        //     because the opponent disconnected pre-shot; the ball flew out / came to rest and a keeper /
        //     throw-collection path blanked currentBallStatus back to "" (so IsMidDelivery() — which needs
        //     currentBallStatus!="" — MISSES it). With no outcome, the bowler is NOT re-armed (-2) and no
        //     CmdBallOutcome/RpcCallRebowl ever fires, so the over deadlocks (log 2026-06-22 00:06: orphan
        //     3rd ball at rest, bowler idle, batter waiting). isBallReleased is cleared by ResetAll on every
        //     clean between-balls / new-batsman / scorecard / over-transition state, so this is FALSE there
        //     and only TRUE for a genuinely released-but-unresolved delivery → re-arm so the ball re-bowls.
        //   !_outcomeCommittedThisDelivery: a POST-SHOT ball (outcome already committed, isBallReleased still
        //   true until the next ResetAll, currentBallStatus blanked on rest) ALSO matches isBallReleased — so
        //   without this guard a reconnect during the 4/6 banner / batsman animation would re-arm and DOUBLE-BOWL
        //   an already-counted ball. The flag is true only after this delivery's outcome committed, so an orphan
        //   (no outcome) stays detectable while a resolved ball is correctly excluded.
        bool _orphanReleased = isBallReleased && !isReplayModeActive && !_outcomeCommittedThisDelivery;
        //   - _bowlingOrphan (robust fallback): if a state-flag combo we didn't anticipate leaves a released
        //     orphan looking "clean" (e.g. isBallReleased got cleared on rest while currentActionState sat at 3 =
        //     in-delivery plumbing, never -2 — which is what the 2026-06-22 02:47 retest hung on: NO STEP-2
        //     marker fired even though the bowler was idle on an un-played ball), the three predicates above all
        //     MISS it. Catch it directly: on the BOWLING side, ANY non-ready state (currentActionState != -2)
        //     with NO committed outcome IS an interrupted delivery that must re-bowl. Guarded by
        //     !_outcomeCommittedThisDelivery so a post-shot ball — and the between-balls / scorecard / over-end
        //     windows, where the LAST outcome is still committed until the next ResetAll — are excluded (no
        //     double-bowl). Bowling-only; the batter's re-arm is its own case--2 branch.
        bool _bowlingOrphan = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex
            && currentActionState != -2 && !isReplayModeActive && !_outcomeCommittedThisDelivery;
        //   - _battingPreReleaseOrphan (STAYING BATTER, the OPPONENT BOWLER disconnected): the mirror of
        //     _bowlingOrphan for the BATTING side. On a bowler-disconnect the batter's own LOCAL bowler MODEL
        //     must be re-homed via ResetAll (which re-applies X via SetBowlerSide + Z per type + idle pose) or
        //     it FREEZES mid-pitch (tester: "reconnection me batter side bowler pitch ke center me hi rehta
        //     hai, reset nahi hota"). _midRunUp already covers the visible run-up (state 1/2); this also catches
        //     state 0 (BatsmanWaiting) and any other PRE-RELEASE cycle state a bowler-disconnect can leave.
        //     Excludes -2 (ready) and -1 (one-time intro cutscene / post-DRS umpire window — ResetAll's forced
        //     state=-2 would stomp those). DOUBLE-BOWL SAFE: the batter never auto-bowls (the UpdateCanBowlerBowl
        //     re-arm in STEP2 is BOWLING-only), so this only RE-HOMES the model; guarded by !isBallReleased (a
        //     released ball is handled by _orphanReleased/IsMidDelivery) and !_outcomeCommittedThisDelivery
        //     (excludes the post-4/6 banner / scorecard / over / innings-change windows) so nothing is re-counted.
        bool _battingPreReleaseOrphan = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex
            && !isBallReleased && currentActionState != -2 && currentActionState != -1
            && !isReplayModeActive && !_outcomeCommittedThisDelivery;
        if (!IsMidDelivery() && !_midRunUp && !_orphanReleased && !_bowlingOrphan && !_battingPreReleaseOrphan)
        {
            // #6 (bat stops connecting after the OPPONENT reconnects mid-shot): on the STAYING BATTING client
            // this early-return is taken when we're already in a clean ready state (actionState=-2, ball not
            // released) — STEP2 is correctly skipped, but that also means ResetAll()/ActivateColliders() never
            // run here. If a PRIOR delivery left PrimaryBatCollider/SecondaryBatCollider disabled (e.g. the
            // offline AI play-and-miss path, or a state RPC_SyncBallRelease's `currentActionState != 2` guard
            // skipped), the bat stays a "ghost" and NO shot connects regardless of timing/posture (tester:
            // "ball bat par hit hi nahi ho rahi, jis marzi tarah posture lagao"). Re-arm the colliders here —
            // idempotent + additive (the bowling side has no bat geometry to hit, so it's harmless there).
            ActivateColliders(boolean: true);
            // STRANDED RELEASE LATCH (staying side, reconnect between a COMPLETED ball and the next one).
            // Reaching this branch means no delivery is live here — but a FINISHED one can still be latched.
            // ballReleaseProcessedThisDelivery and _outcomeCommittedThisDelivery are only ever cleared TOGETHER
            // (ResetAll / the per-delivery reset in BowlNextBall), so an outcome that has already committed
            // guarantees the release latch is still set, and the reconnect is precisely what stops the normal
            // next-ball chain from running that reset here. The opponent then bowls, its plan packet reaches the
            // duplicate-release guard at the top of RPC_SyncBallRelease and is DROPPED: no plan is armed, no
            // launch happens, and the delivery simply never occurs on this screen (tester 11-08: "bowler side
            // ball hui but batsman side nhi hui", and the missed turn that follows) until the 8s/10s watchdogs
            // re-arm both sides and the ball is lost. The staying batter cannot self-heal either — the
            // between-balls watchdog below stands down on isBallReleased, which is still true in this state.
            // Clearing the release latch alone is enough: TryLaunchFromLockedPlan sets the delivery state itself,
            // and there is no live ball in this branch for the guard to be protecting.
            if (ballReleaseProcessedThisDelivery)
            {
                ballReleaseProcessedThisDelivery = false;
                ConstantsData_M.MpLog($"[GroundController][CancelInFlight] Cleared the completed delivery's release latch (outcomeCommitted={_outcomeCommittedThisDelivery} released={isBallReleased}) so the next delivery's plan packet is not swallowed by the duplicate-release guard.");
            }
            // BETWEEN-BALLS RECONNECT (bowling side) — the case nothing else covers. _bowlingOrphan
            // deliberately excludes !_outcomeCommittedThisDelivery so the between-balls / scorecard /
            // over-end windows can't double-bowl. That is right during NORMAL play, where the ball-complete
            // chain arms the next delivery — but a reconnect reloads the scene and breaks that chain, so on a
            // reconnect landing in the gap AFTER a completed ball the bowler is left at its post-ball state
            // and NOBODY ever arms it: the batter waits forever. 26-07 16:05 log, four CancelInFlight entries
            // on the bowling client — the three with an in-flight/uncommitted ball all fired STEP2, while
            // "released=False actionState=3 outcomeCommitted=True" (the reconnect right after a 4) did not,
            // and that is exactly the ball the match froze on. Arm a BOUNDED watchdog instead of re-arming
            // now: the normal flow keeps priority and only a genuine stall is recovered.
            StartBetweenBallsRearmWatchdog();
            return;
        }
        ConstantsData_M.MpLog($"[GroundController][CancelInFlight] STEP2 FIRING: orphanReleased={_orphanReleased} bowlingOrphan={_bowlingOrphan} midRunUp={_midRunUp} battingPreReleaseOrphan={_battingPreReleaseOrphan} → re-arming bowler to -2.");
        _hasNetworkBallPosition = false;   // clear transient stream-receive state
        _ballStreamTimer = 0f;
        isBallHit = false;
        ResetAll();                        // isBallReleased=false, currentBallStatus="", ball back in hand
        // Fresh auto-bowl window (tester 09-07: "reconnect ke baad pehli turn miss", every batting-side case):
        // the pre-disconnect auto-bowl timer keeps its REMAINING time through the disconnect — the -2 re-arm is
        // gated on !timerObject.activeInHierarchy, so an already-active timer is never restarted and could fire
        // the re-bowl almost immediately at the reconnect-notify, while the reconnector was still settling →
        // the first ball flew past an unready batter. Kill the stale timer here; CallAutomaticBall/AutomaticBall
        // are both gated on timerObject.activeInHierarchy so the pending DOTween callback is neutralized, and
        // the -2 branch then arms a FRESH full 10s window.
        if (timerObject != null && timerObject.activeInHierarchy)
        {
            timerObject.SetActive(false);
            ConstantsData_M.MpLog("[GroundController][CancelInFlight] Stale auto-bowl timer killed — fresh 10s window will arm for the re-bowl.");
        }
        // Ghost-ball visual fix (tester: "reconnect ke baad ball release point pe hawa mein stuck dikhti hai,
        // phir re-bowl ke release pe wohi ball batsman ki taraf jati hai — game theek, visual bug"): between
        // balls the matchBall is PARKED at ballStartPosition (the release point) and must stay HIDDEN — the
        // bowler's separate in-hand prop (BowlerBallSkinRenderer, re-enabled by ResetAll above) is what the
        // player should see. ShowBall(true) here rendered the parked ball floating mid-air through the whole
        // re-bowl run-up. Keep it hidden; the release itself shows it (ReleaseTheBall / RPC_SyncBallRelease).
        ShowBall(false);                   // parked at the release point — hidden until the actual release
        currentActionState = -2;           // bowler-ready / pre-delivery (arms CanBowlerBowl + auto-bowl timer)
        // Orphan re-bowl guarantee: the bowling-side auto-bowl re-arm (Update case -2, ~17170) needs
        // CONTROLLER.CanBowlerBowl==true, which is normally BROADCAST by the BATTING client's case--2 branch.
        // On a mid-delivery reconnect that broadcast may not have landed yet (the reconnecter is still
        // restoring), so the staying bowler would sit at -2 with CanBowlerBowl==false and the timer would never
        // arm → the re-bowl silently fails. Force+broadcast it here when WE are the bowling side so the re-bowl
        // is guaranteed; harmless (idempotent) when it was already true. Bowling-only so the staying batter case
        // is untouched (the batter's own case--2 branch owns CanBowlerBowl there).
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
            UpdateCanBowlerBowl(true);
        ConstantsData_M.MpLog("[GroundController] Mid-delivery reconnect — in-flight ball cancelled, over re-armed (ball will be re-bowled).");
    }

    // G1 v2: any inbound delivery-stream packet is positive proof the opponent is still feeding this ball.
    // Stamp it so a BATTING staying client's pre-contact window only times out on a GENUINE silence, and so
    // the BOWLING staying client gets a liveness source via RPC_SyncBallShot.
    public void NoteDeliveryStreamProgress()
    {
        _deliveryWatchProgressMark = Time.unscaledTime;
        _deliveryWatchTimer = 0f;
    }

    private bool _oppGoneHoldActive;   // edge-tracking for the [StallHold] log above
    private Coroutine _betweenBallsRearmWatchdog;
    private bool _oppAckBlockedLogged;   // one-shot for the CallOppAck ghost-session guard

    /// <summary>
    /// Bowling side, post-reconnect: recover the "reconnected between two balls" freeze. See the call site in
    /// CancelInFlightDeliveryForReconnect for why no existing path arms the bowler there. Deliberately a
    /// WATCHDOG, not an immediate re-arm — the normal ball-complete chain gets the full window first, so a
    /// reconnect that lands while the game is about to advance on its own can never produce a double bowl.
    /// </summary>
    private void StartBetweenBallsRearmWatchdog()
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        // BOTH sides. The bowling side is the one that freezes the match (nobody arms the next ball), but
        // the batting side has the same hole for the same reason — _battingPreReleaseOrphan also requires
        // !_outcomeCommittedThisDelivery — and there it leaves the bowler MODEL parked mid-pitch instead of
        // re-homed ("reconnection me batter side bowler pitch ke center me hi rehta hai"). Recovery differs
        // per side (see the routine): the bowler re-arms the delivery, the batter only re-homes.
        // -2 alone is NOT proof the over will continue — see AutoBowlTimerLive. Decline to arm only when
        // a ball is live or the bowler is armed WITH a running timer; otherwise this watchdog is the only
        // thing left that can recover the over.
        if (isBallReleased || (currentActionState == -2 && AutoBowlTimerLive))
        {
            // Was a SILENT exit. In the both-sides-disconnect stall this is one of only two ways the recovery
            // can fail to exist at all, and an unarmed watchdog looks identical in a log to one that armed and
            // stood down. Name it. (isBallReleased stays true across a finished-but-unreset delivery, so this
            // can decline to arm on a side that is NOT actually live.)
            ConstantsData_M.MpLog($"[BetweenBallsRearm] NOT armed after a reconnect — released={isBallReleased} state={currentActionState} (nothing will recover this side if the over is genuinely stalled).");
            return;
        }
        if (_betweenBallsRearmWatchdog != null) StopCoroutine(_betweenBallsRearmWatchdog);
        // Log the ARM as well as the fire. Without this a silent watchdog is ambiguous — it could have
        // never started, or started and stood down — and that ambiguity cost a whole test round to resolve.
        ConstantsData_M.MpLog($"[GroundController][BetweenBallsRearm] Armed after a reconnect (side={(CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex ? "bowling" : "batting")}, actionState={currentActionState}).");
        _betweenBallsRearmWatchdog = StartCoroutine(BetweenBallsRearmWatchdogRoutine());
    }

    private IEnumerator BetweenBallsRearmWatchdogRoutine()
    {
        int startBalls = SafeBattingBalls();
        float elapsed = 0f;
        float deadline = 8f;
        while (elapsed < deadline)
        {
            // A scorecard being up USED to stand this watchdog down for good, on the assumption that
            // Continue() would drive play from there. That assumption fails in the exact case testers keep
            // hitting: one player left at the pitch with no panel while the other stares at a scorecard whose
            // tick does nothing, because the side that must arm the next ball never did — so BOTH sit frozen
            // and the only thing that could have recovered them had already stood down. Don't stand down for
            // it; just allow much longer, so a human genuinely reading the card is never interrupted (any tap
            // that actually works changes the state below and ends this quietly), while a card that is truly
            // dead still gets recovered.
            bool scorecardUp =
                (Singleton<BowlingScoreCard>.instance != null && Singleton<BowlingScoreCard>.instance.scoreCard != null
                    && Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy)
                || (Singleton<BattingScoreCard>.instance != null && Singleton<BattingScoreCard>.instance.scoreCard != null
                    && Singleton<BattingScoreCard>.instance.scoreCard.activeInHierarchy);
            if (scorecardUp) deadline = 25f;

            // Any sign the normal flow recovered by itself → stand down, touch nothing.
            // Same rule on the way out: standing down on a bare -2 is how an armed watchdog abandoned a
            // stalled over one frame after arming. Only a -2 with a running timer counts as recovered.
            if ((currentActionState == -2 && AutoBowlTimerLive) || isBallReleased || SafeBattingBalls() != startBalls
                || CONTROLLER.OnWaitScreen > 0 || isReplayModeActive
                || CONTROLLER.gameCompleted)
            {
                // Say WHICH sign it took as recovery. A stand-down on a signal that did not actually restart the
                // over (state flipped to -2 but the auto-bowl timer is dead, say) removes the only safety net and
                // is indistinguishable from a healthy recovery unless the reason is recorded.
                ConstantsData_M.MpLog($"[BetweenBallsRearm] Stood down after {elapsed:F1}s — state={currentActionState} released={isBallReleased} balls={SafeBattingBalls()}/{startBalls} onWait={CONTROLLER.OnWaitScreen} replay={isReplayModeActive} completed={CONTROLLER.gameCompleted}.");
                _betweenBallsRearmWatchdog = null;
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _betweenBallsRearmWatchdog = null;
        // Still idle after the full window with no barrier, no scorecard and no ball counted: the chain is
        // genuinely broken. Re-arm through the existing non-destructive path — it does NOT touch the over-ACK
        // barrier, and on the batting side it only re-homes the bowler model (the batter never auto-bowls).
        bool amBowling = CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex;
        ConstantsData_M.MpLog($"[GroundController][BetweenBallsRearm] Still idle 8s after a between-balls reconnect (side={(amBowling ? "bowling" : "batting")}, actionState={currentActionState}, balls={SafeBattingBalls()}) — recovering so the over can continue.");
        ReArmStalledDeliveryNonDestructive();
        // CanBowlerBowl is normally BROADCAST by the batting client; only the bowling side needs to force it,
        // and forcing it from the batter would fight that broadcast.
        if (amBowling) UpdateCanBowlerBowl(true);
    }

    // Non-destructive re-arm: re-bowls a stalled ball WITHOUT touching the over-ACK barrier (OnWaitScreen /
    // ackWatchdog / timerObject). The tick has already proven we are NOT inside a live barrier
    // (ackBarrierResolved && OnWaitScreen==0) before calling this, so we only need the STEP2 re-arm.
    private void ReArmStalledDeliveryNonDestructive(bool relayToOpponent = true)
    {
        bool _relayReArm = relayToOpponent;
        ConstantsData_M.MpLog("[GroundController][DeliveryWatchdog] Stalled pre-outcome delivery (>10s, opponent present) — non-destructive re-arm.");
        _hasNetworkBallPosition = false;
        _ballStreamTimer = 0f;
        isBallHit = false;
        ResetAll();                 // isBallReleased=false, currentBallStatus="", ball back in hand
        ShowBall(false);            // parked at the release point — hidden until the actual release (see CancelInFlight ghost-ball note)
        currentActionState = -2;    // bowler-ready / pre-delivery (arms CanBowlerBowl + auto-bowl timer)
        ballReleaseProcessedThisDelivery = false; // belt-and-suspenders (ResetAll already clears it); avoids the RPC_SyncBallRelease duplicate-guard stranding the re-bowled ball
        // TELL THE OTHER CLIENT. Everything above is purely LOCAL, so a watchdog that fired on one side left
        // the other still sitting in the stalled delivery — which is exactly the tester's "game get stuck on
        // BOWLER side": the 03-08 logs show both watchdogs firing and recovering the batting client while the
        // bowling one never heard about it. Reuse the existing rebowl relay so both sides re-arm together.
        // The sender ignores its own echo (RpcCallRebowl returns early on senderId == local), so this cannot
        // re-enter here.
        if (_relayReArm && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CricketNetworkManager.ReadyToSend && staticVariables.UserProfiledata?.user != null)
        {
            ConstantsData_M.MpLog("[GroundController][DeliveryWatchdog] Relaying the re-arm to the opponent so both sides re-arm identically.");
            CricketNetworkManager.instance.CmdRelayStallReArm(staticVariables.UserProfiledata.user._id);
        }
    }

    // Receiver for the relay above. Runs the SAME non-destructive re-arm, and does NOT relay again.
    //
    // The first attempt at this reused CmdCallRebowl, which lands the opponent in Launcher.RPC_CallRebowl —
    // a DIFFERENT recovery from the local re-arm. The two sides then accounted for the ball differently: the
    // 04-08 local run ended with the authority on 3 chips and the other client on 2 ("authority ballCount=3
    // but this client has 2 chips"), which is the 1.2 vs 1.1 on the two screens. Both sides must run the same
    // recovery for the state to match.
    public void ApplyRelayedStallReArm()
    {
        ConstantsData_M.MpLog("[GroundController][DeliveryWatchdog] Applying the opponent's relayed re-arm.");
        ReArmStalledDeliveryNonDestructive(relayToOpponent: false);
    }

    // G1 v2 per-frame tick. MP-only, kill-switchable (ships dark). Re-arms a stalled pre-contact in-flight
    // ball after 10s of GENUINE stream silence with the opponent still present. Holds on every other case.
    // ── STUCK HEARTBEAT (both-sides-disconnect trace) ───────────────────────────────────────────────
    // A stalled over produces NO log output at all — every watchdog either holds or stands down quietly —
    // so "game stuck on both sides" arrives as a silent gap and the state that caused it is gone by the
    // time anyone looks. While nothing is happening, print the full gating picture every few seconds so
    // the stall is visible in BOTH logs and the two can be compared side by side. Idle-only and rate
    // limited, so a running match prints nothing.
    private float _stuckIdleSeconds;
    private float _stuckLastReport;
    private int _stallEpisodeId;            // 0 = no episode in progress; pairs the two devices' lines
    private float _stallPeerAskedAt = -999f;
    private const float STALL_PEER_ASK_EVERY = 15f;
    private const float STUCK_IDLE_BEFORE_REPORT = 12f;   // a normal gap between balls is a few seconds
    private const float STUCK_REPORT_EVERY = 5f;

    // "The bowler is genuinely armed" is state -2 AND a live auto-bowl timer — never -2 on its own.
    // -2 with a dead timer is exactly the stall the 13-08 logs caught (StuckTrace sat on it for 140s),
    // and treating -2 alone as healthy is what made every recovery path decline to help.
    private bool AutoBowlTimerLive => timerObject != null && timerObject.activeInHierarchy;

    private void TickStuckHeartbeat()
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI || CONTROLLER.gameCompleted)
            { _stuckIdleSeconds = 0f; _stallEpisodeId = 0; return; }
        // "Something is happening" = a ball is live, or the bowler is armed and the auto-bowl timer is
        // actually running. State -2 alone is NOT enough: the classic stall is -2 with a dead timer.
        bool _busy = isBallReleased || isReplayModeActive || CONTROLLER.OnWaitScreen > 0
            || (currentActionState == -2 && AutoBowlTimerLive);
        // Episode over — drop the id so the NEXT stall is reported under a fresh one and two separate
        // stalls can never be mistaken for a single long one when the two logs are lined up.
        if (_busy) { _stuckIdleSeconds = 0f; _stallEpisodeId = 0; return; }
        _stuckIdleSeconds += Time.unscaledDeltaTime;
        if (_stuckIdleSeconds < STUCK_IDLE_BEFORE_REPORT) return;
        if (Time.unscaledTime - _stuckLastReport < STUCK_REPORT_EVERY) return;
        _stuckLastReport = Time.unscaledTime;
        // One id per stall EPISODE, minted when the idle first crosses the threshold. It is what lets the
        // two devices' logs be paired: this side prints [StuckTrace] with it, the peer prints [PeerStall]
        // with the same number, and the pair can then be read as one moment instead of two unrelated ones.
        if (_stallEpisodeId == 0)
            _stallEpisodeId = (Mathf.Abs(Time.unscaledTime.GetHashCode()) % 89999) + 10000;
        ConstantsData_M.MpLog($"[StuckTrace] id={_stallEpisodeId} Idle {_stuckIdleSeconds:F0}s — " + BuildStallStateLine());
        // ASK THE PEER WHAT IT IS DOING. Every stall pair so far has come back half-blind: only the idle
        // side reports, because TickStuckHeartbeat's own _busy test is true on the side that IS bowling —
        // so the tester's "bowler side ball ho gayi, batsman side stuck" has never had the bowling side's
        // state for the same instant, and the cause could not be narrowed. This makes the other device
        // print one line, tagged with the same episode id. Once per episode and then every 15s, so a long
        // stall cannot flood the 200-line log window the reporter keeps.
        if (CricketNetworkManager.instance != null && CricketNetworkManager.ReadyToSend
            && Time.unscaledTime - _stallPeerAskedAt >= STALL_PEER_ASK_EVERY)
        {
            _stallPeerAskedAt = Time.unscaledTime;
            CricketNetworkManager.instance.CmdRequestPeerStall(staticVariables.UserProfiledata.user._id, _stallEpisodeId);
        }
    }

    /// <summary>
    /// Prints this client's state on the PEER's request, so a stall is recorded from both sides under the
    /// same episode id. Deliberately unconditional on being idle — the whole point is to capture the side
    /// that believes it is busy.
    /// </summary>
    public void LogPeerStallState(int stallEpisodeId)
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        ConstantsData_M.MpLog($"[PeerStall] id={stallEpisodeId} (peer asked) — " + BuildStallStateLine());
    }

    // The one field set both [StuckTrace] and [PeerStall] print, so the two sides are directly comparable
    // line-for-line rather than needing two different formats to be reconciled by eye.
    private string BuildStallStateLine()
    {
        return $"state={currentActionState} released={isBallReleased}"
            + $" status='{currentBallStatus}' outcomeCommitted={_outcomeCommittedThisDelivery} ackResolved={ackBarrierResolved}"
            + $" onWait={CONTROLLER.OnWaitScreen} canBowl={CONTROLLER.CanBowlerBowl} timerLive={AutoBowlTimerLive}"
            + $" amBowling={(CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)} bowlingBy='{BowlingBy}' battingBy='{BattingBy}'"
            + $" rearmWatchdog={(_betweenBallsRearmWatchdog != null)} oppHold={_oppGoneHoldActive}"
            + $" players={(NetworkGameManager.Instance != null ? NetworkGameManager.Instance.currentPlayerCount : -1)}"
            + $" restorePending={CricketNetworkManager.RestorePending} planArmed={_lockedPlanArmed} planMinted={_lockedPlanMinted}"
            + $" ballNum={(Singleton<GameData>.instance != null ? Singleton<GameData>.instance.currentBallNumber : -99)}"
            + $" bowler={CONTROLLER.CurrentBowlerIndex} side='{bowlerSide}' innings={CONTROLLER.currentInnings}"
            + $" bat={CONTROLLER.BattingTeamIndex} bowl={CONTROLLER.BowlingTeamIndex} striker={CONTROLLER.StrikerIndex}"
            // The delivery watchdog bails on ~25 review/keeper/fielder states, and none of them were being
            // printed — so a stall that the watchdog declines to touch looked identical to one it never saw.
            // The 18-08 pair sat at state=2 for 32s with both players connected and timeScale at 1.00, which
            // rules out the network and slow-mo guards and leaves exactly these. Print them.
            + $" fielderAction='{fielderAction}' wkStatus='{currentWicketKeeperStatus}' replay={isReplayModeActive}"
            + $" drs={isDRSEnabled} wkActive={isWicketKeeperActive} reviewReplay={CONTROLLER.reviewReplay} showing={CONTROLLER.ReplayShowing}.";
    }

    private void TickDeliveryWatchdog()
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI
            || CricketNetworkManager.instance == null
            || (!ConstantsData_M.useWatchdogG2 && !ConstantsData_M.useWatchdogPostShot))
        { _deliveryWatchTimer = 0f; return; }

        // Opponent genuinely gone → the 30s waiting/win + reconnect path owns recovery; HOLD.
        // EDGE-LOGGED because this hold is the prime suspect for "game stuck on BOTH sides after a
        // disconnection from both sides": every delivery-stall recovery below is suspended while it is
        // engaged, on the assumption that the OTHER client is still running and will drive things. When both
        // clients disconnect, both can sit here waiting for each other and nobody recovers. Logging only the
        // TRANSITIONS (not every frame) shows whether the hold engaged and, crucially, whether it ever
        // released once both were back — a hold that never releases is the bug; one that releases means the
        // stall is somewhere else. NetworkGameManager is the locked shared layer, so this only observes it.
        bool _oppHold = NetworkGameManager.Instance == null || NetworkGameManager.Instance.IsPaused
            || NetworkGameManager.Instance.currentPlayerCount < 2;
        if (_oppHold != _oppGoneHoldActive)
        {
            _oppGoneHoldActive = _oppHold;
            ConstantsData_M.MpLog($"[StallHold] Delivery-stall recovery {(_oppHold ? "HELD" : "RELEASED")} — "
                + $"ngmNull={NetworkGameManager.Instance == null} paused={(NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsPaused)} "
                + $"players={(NetworkGameManager.Instance != null ? NetworkGameManager.Instance.currentPlayerCount : -1)} state={currentActionState} released={isBallReleased}.");
        }
        if (_oppHold)
        { _deliveryWatchTimer = 0f; _deliveryWatchScaledMark = Time.time; return; }

        // Never act during a live over-ACK barrier (the OppAckWatchdog owns it).
        if (!ackBarrierResolved || CONTROLLER.OnWaitScreen > 0)
        { _deliveryWatchTimer = 0f; _deliveryWatchScaledMark = Time.time; return; }

        // Slow-mo / pause: bail if timeScale<1 OR the scaled clock has not advanced this frame.
        if (Time.timeScale < 1f || Time.time <= _deliveryWatchScaledMark)
        { _deliveryWatchTimer = 0f; _deliveryWatchScaledMark = Time.time; return; }
        _deliveryWatchScaledMark = Time.time;

        // Review / decision boards — bail on every real guard field/value.
        if (isReplayModeActive || isDRSEnabled || isWicketKeeperActive || mpCaughtBehindOutPending
            || CONTROLLER.reviewReplay || CONTROLLER.ReplayShowing || CONTROLLER.canShowReplay
            || currentWicketKeeperStatus == "stumpingAttempt" || currentWicketKeeperStatus == "stumpingAppeal"
            || currentWicketKeeperStatus == "showWideSignalBeforeStumpingResult" || currentWicketKeeperStatus == "waitForStumpingResult"
            || currentWicketKeeperStatus == "waitFor3rdUmpireResult" || currentWicketKeeperStatus == "Show3rdUmpireResult"
            || currentWicketKeeperStatus == "CaughtBehind" || currentWicketKeeperStatus == "waitForCaughtBehindResult"
            || currentWicketKeeperStatus == "decisionPending" || currentWicketKeeperStatus == "waitForWideSignal"
            || fielderAction == "runOutAppeal" || fielderAction == "waitForResult" || fielderAction == "waitForThirdUmpireSignal"
            || fielderAction == "waitFor3rdUmpireResultForRunout" || fielderAction == "Show3rdUmpireResultForRunout"
            || fielderAction == "bowlerrunoutappealsandout" || fielderAction == "bowlerrunoutappealbutnotout"
            || fielderAction == "lbwAppeal" || fielderAction == "waitForLbwResult" || fielderAction == "umpireNoBallActionForLBW")
        { _deliveryWatchTimer = 0f; return; }

        // LIVE WINDOWS. Window A (P1 run-up) is near-dead (state 1 self-advances in ~0.5s) but harmless;
        // Window B (batting pre-contact in-flight) is the real, verified-safe coverage: a RELEASED ball that
        // does not reach the batsman in 10s is unambiguously a stall (a normal delivery is ~1-2s).
        // State 2 (the run-up ANIMATION actually playing) belongs here as much as state 1. It was missing,
        // so a client frozen mid-run-up — animation running, ball never released — had NO recovery at all:
        // this window skipped it, _battingPreContact needs a released ball, and the between-balls watchdog
        // only arms off a reconnect. The 13-08 12:55 pair caught both clients sitting at state=2,
        // released=False, timerLive=False for 140+ SECONDS until the session was killed — the "game stuck on
        // both sides" report. CancelInFlightDeliveryForReconnect already learned this exact lesson (it tests
        // 1 || 2, added when a disconnect during the visible run-up slipped through); this copy was never
        // brought in line. A real run-up is ~2-3s, so 10s here is unambiguous.
        bool _midRunUp = (currentActionState == 1 || currentActionState == 2) && !isBallReleased && !isReplayModeActive;
        bool _battingPreContact = CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
            && isBallReleased && !isBallHit && currentBallStatus == "bowling";
        // POST-SHOT freeze deadlock (tester: 1st-innings end panels + game stuck; ball frozen at the keeper
        // z=11.41 on BOTH clients with ballHit=True + BLANK status). A shot registered but the ball never flew
        // and no outcome committed, so isBallReleased stays true forever and the over/innings can't advance. All
        // review/board/catch/runout/LBW states are excluded by the guard above, so a hit ball + BLANK status +
        // no committed outcome here is an unambiguous stall — re-bowl it via the same non-destructive recovery.
        bool _postShotStalled = isBallReleased && isBallHit && !_outcomeCommittedThisDelivery
            && string.IsNullOrEmpty(currentBallStatus);
        bool _live = ((_midRunUp || _battingPreContact) && ConstantsData_M.useWatchdogG2)
            || (_postShotStalled && ConstantsData_M.useWatchdogPostShot);
        if (!_live) { _deliveryWatchTimer = 0f; return; }

        // Fresh inbound stream progress within the last second means the opponent is still feeding the ball.
        if (_battingPreContact && (Time.unscaledTime - _deliveryWatchProgressMark) < 1f)
        { _deliveryWatchTimer = 0f; return; }

        _deliveryWatchTimer += Time.unscaledDeltaTime;
        // Post-shot freeze is unambiguous → recover a bit faster (6s) than the pre-contact stall (10s).
        float _watchdogTimeout = _postShotStalled ? 6f : 10f;
        if (_deliveryWatchTimer > _watchdogTimeout)
        {
            _deliveryWatchTimer = 0f;
            ReArmStalledDeliveryNonDestructive();
        }
    }

    public void RPC_SendOppAck()
    {
        CONTROLLER.OnWaitScreen++;
        ConstantsData_M.MpLog("WAITTT : " + CONTROLLER.OnWaitScreen);
        if (CONTROLLER.OnWaitScreen == 1)
        {
            // First ACK of the pair is in; arm the watchdog in case the second one is lost.
            ackBarrierResolved = false;
            if (ackWatchdog != null) StopCoroutine(ackWatchdog);
            ackWatchdog = StartCoroutine(OppAckWatchdog());
        }
        if (CONTROLLER.OnWaitScreen >= 2)
        {
            ackBarrierResolved = true;
            if (ackWatchdog != null) { StopCoroutine(ackWatchdog); ackWatchdog = null; }
            CONTROLLER.OnWaitScreen = 0; // Reset immediately so re-entrant acks don't re-fire CheckForOverComplete
            Launcher.Instance.IsReConnecting().Show();
            if (!Launcher.Instance.IsReConnecting())
            {
                // Force panel active so CheckForOverComplete always takes the hide path (not show+ack path)
                Singleton<GameData>.instance.waitingForOpponentPanel.SetActive(true);
                Singleton<GameData>.instance.CheckForOverComplete();
            }
            if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && !Launcher.Instance.IsReConnecting())
            {
                ConstantsData_M.Log("CHECK CALLED 1");
                Singleton<ScoreBoardBallList>.instance.MultiplayerSendData();
            }
            // Batting player pushes authoritative state to server SyncVars after every ACK pair.
            // This ensures reconnecting players always get current scores/wickets/overs,
            // not just the state from the last rebowl (which was the only previous update path).
            if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
                && !Launcher.Instance.IsReConnecting()
                && CONTROLLER.TeamList != null
                && CricketNetworkManager.instance != null)
            {
                CricketNetworkManager.instance.CmdSyncGameState(
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores,
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets,
                    Singleton<ScoreBoardBallList>.instance != null ? Singleton<ScoreBoardBallList>.instance.ballCount : 0,
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls,
                    Singleton<GameData>.instance != null ? Singleton<GameData>.instance.currentBallNumber : 0,
                    CONTROLLER.StrikerIndex,
                    CONTROLLER.NonStrikerIndex,
                    Singleton<GameData>.instance != null ? Singleton<GameData>.instance.newBatsmanEntryIndex : 0
                );

                // Scenario-6 hardening (personal score lags the team total on reconnect): CmdSyncGameState above
                // pushes the TEAM total every ACK pair, but the per-batsman runs/balls were pushed ONLY from
                // GameData's per-ball block — so a reconnect landing between the two restored the team total CURRENT
                // while the striker/non-striker card stayed STALE ("runs added only to the main scoreboard, not the
                // personal score"). Co-locate the personal push with the team push so every ACKed ball keeps BOTH
                // consistent. CmdSyncBatsmanScores is parity-present on the dedicated server (verified 2026-07-01,
                // 32/32 SyncVars) → NO wire/server change. Bounds-guarded; restored by RestoreBatsmanScores() on
                // reconnect. NOTE: this does NOT recover a ball interrupted mid-shot (that ball never ACKs; it is
                // re-bowled by CancelInFlightDeliveryForReconnect) — it closes the completed-ball divergence.
                var _oppAckPl = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList;
                if (_oppAckPl != null
                    && CONTROLLER.StrikerIndex >= 0 && CONTROLLER.StrikerIndex < _oppAckPl.Length
                    && CONTROLLER.NonStrikerIndex >= 0 && CONTROLLER.NonStrikerIndex < _oppAckPl.Length)
                {
                    CricketNetworkManager.instance.CmdSyncBatsmanScores(
                        _oppAckPl[CONTROLLER.StrikerIndex].BatsmanList.RunsScored,
                        _oppAckPl[CONTROLLER.StrikerIndex].BatsmanList.BallsPlayed,
                        _oppAckPl[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored,
                        _oppAckPl[CONTROLLER.NonStrikerIndex].BatsmanList.BallsPlayed);
                }

                // Reconnect-safe FULL snapshot: both teams' totals + live innings/indices. Needed
                // because the single-team push above + the frozen BattingTeamIndex/BowlingTeamIndex
                // SyncVars (never updated at the innings change) made a 2nd-innings reconnect lose
                // the first-innings target and mis-assign the live score (→ 0). See
                // CricketNetworkManager.CmdSyncMatchProgress / ApplyReconnectState.
                // Target=1 / score-reset fix: capture the two PLAYING teams by their ACTUAL indices
                // (BattingTeamIndex / BowlingTeamIndex — e.g. 4 and 9 from team selection), NOT
                // TeamList[0]/[1] which are EMPTY unused teams here. Pushing [0]/[1] made syncedTeam0/1
                // always 0/0 → on a 2nd-innings reconnect the bowling team's first-innings total was lost
                // → TargetToChase = 0+1 = 1, and the non-current team showed 0/0. t0 = batting team,
                // t1 = bowling team; the restore writes them back by syncedBatting/BowlingTeamIndex.
                if (CONTROLLER.BattingTeamIndex >= 0 && CONTROLLER.BowlingTeamIndex >= 0
                    && CONTROLLER.BattingTeamIndex < CONTROLLER.TeamList.Length
                    && CONTROLLER.BowlingTeamIndex < CONTROLLER.TeamList.Length)
                {
                    CricketNetworkManager.instance.CmdSyncMatchProgress(
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls,
                        CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores, CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchWickets, CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchBalls,
                        CONTROLLER.currentInnings, CONTROLLER.BattingTeamIndex, CONTROLLER.BowlingTeamIndex);
                }
            }
        }
    }

    public void CallOppAck()
    {
        if (GameConstants.isWithAI == false)
        {                                                                                                                     //Photon Removal
                                                                                                                              //    ConstantsData_M.Log("CHECK CALLED");
                                                                                                                              //    view.RPC("RPC_SendOppAck", RpcTarget.AllBuffered);
                                                                                                                              // Ghost-session guard. This is the OVER-ACK handshake: across the reconnect scene reload the
                                                                                                                              // spawned manager is destroyed, and Mirror's SendCommandInternal then NREs here — the ACK is
                                                                                                                              // never sent, the OnWaitScreen 0->1->2 barrier never completes, and BOTH players sit frozen
                                                                                                                              // (tester 26-07: "reconnection pe game hi stuck ho gayi 4 ke baad"; the NRE stack in that log
                                                                                                                              // is exactly CallOppAck -> CmdSendOppAck -> SendCommandInternal). Same gate as the other Cmds;
                                                                                                                              // OppAckWatchdog below still covers a genuinely lost ACK.
            if (CricketNetworkManager.ReadyToSend)
                CricketNetworkManager.instance.CmdSendOppAck();
            else if (!_oppAckBlockedLogged)
            {
                _oppAckBlockedLogged = true;
                ConstantsData_M.MpLog("[GroundController][CallOppAck] Over-ACK send BLOCKED — network manager not ready (ghost session). The watchdog owns recovery from here.");
            }                                                             //Photon Addition
        }



    }

    private IEnumerator OppAckWatchdog()
    {
        // Wait generously so this only ever triggers on genuine ACK loss, never on normal
        // high-latency play. If we're still waiting (barrier never reached 2) after the timeout,
        // force the advance so the pitch can't stay frozen waiting for an ACK that won't arrive.
        // Stuck-after-wicket fix (#7): use REALTIME — a wicket can leave Time.timeScale≈0 (slow-mo/
        // DRS), and a scaled WaitForSeconds would then never tick, so the watchdog could never
        // recover. Realtime guarantees the force-advance fires even while gameplay time is frozen.
        // Recovery-latency tune (2026-06-18): was 12s. A genuine ACK arrives within one RTT (sub-second
        // on LAN, ~1-2s on a real network), so 12s left the pitch visibly FROZEN for ~12s after any
        // dropped ACK (very noticeable when the tester disconnects repeatedly — "game stuck, recovers
        // after a while"). 5s still comfortably clears normal high-latency play but recovers ~2.4× faster.
        // The force-advance is re-entrant-safe (RPC_SendOppAck resets OnWaitScreen at 2), so a slightly
        // early fire on an extreme-latency spike is harmless.
        yield return new WaitForSecondsRealtime(5f);
        // Solo-over-advance guard (tester #5: "batsman mid-shot disconnect → STAYING side scorecard + stuck"):
        // with the opponent GONE only the local ACK can ever arrive, and forcing the barrier here made the
        // staying client run the whole over-transition SOLO (NewOver → scorecard opened on one side only →
        // handshake stuck when the reconnector came back mid-over). Never force while the opponent is absent —
        // the reconnect-notify path (CancelInFlightDeliveryForReconnect) clears the barrier and re-arms.
        bool _oppPresent = NetworkGameManager.Instance != null && !NetworkGameManager.Instance.IsPaused
            && NetworkGameManager.Instance.currentPlayerCount >= 2;
        if (!ackBarrierResolved && CONTROLLER.OnWaitScreen < 2 && _oppPresent
            && CONTROLLER.PlayModeSelected == 8 && !Launcher.Instance.IsReConnecting())
        {
            Debug.LogWarning("[GroundController] Opponent ACK timed out after 5s — forcing next ball to avoid a permanent freeze on the pitch.");
            CONTROLLER.OnWaitScreen = 2;
            RPC_SendOppAck();
        }
    }

    // First-post-reconnect-ball presentation fix (batting side, 2026-06-22): the re-bowled ball after a
    // mid-over batting reconnect arrives via RpcAutomaticBall DIRECTLY, bypassing the normal BowlNextBall ->
    // ResetAll setup that (on every other ball) makes the batsman model visible (ShowAllPlayers) and arms the
    // bowler aim preview (the -2 Update branch runs ZoomCameraToBowler -> ShowBowlingInterface, which shows the
    // spin-angle + power meter to BOTH teams). So the batting reconnecter's FIRST ball showed no batsman and no
    // spin/power preview (the 2nd ball, a normal BowlNextBall, self-corrected). Replicate that pre-ball
    // presentation here: enable all renderers and enter the bowler-ready state (-2) with the camera-zoom timer
    // armed so the -2 branch cascades into ShowBowlingInterface next frame. Visual/state only — it does NOT bowl
    // or touch ball authority; the incoming RpcAutomaticBall then delivers the ball from this clean -2 state
    // exactly as a normal ball would. Batting-only (called from GameData.OnReconnection under that gate).
    public void ArmBattingPreviewAfterReconnect()
    {
        ShowAllPlayers();   // enables batsmanSkinRenderer etc. — but does NOT place/pose the model
        ShowBall(status: false); // Bug-fix S7: clear any stale in-flight ball before the fresh delivery shows it
        // SYNC THE STRIKER'S HAND from the RESTORED striker data BEFORE positioning. The GroundController instance
        // `batsmanHand` resets to its "right" default on the scene reload and CONTROLLER.StrikerHand is a stale
        // static, so the batsman was spawning with the WRONG hand / not matching the actual striker (tester:
        // "left batter spawn howa, synced nahi"). StrikerIndex is restored in ApplyReconnectState and TeamList is
        // static, so PlayerList[StrikerIndex].BatsmanList.BattingHand is authoritative — same source as
        // GameData.SetGameDatas. (Bowler hand/type are re-derived by the RpcSetBowler that follows, so not here.)
        string _srcHand = "?";
        if (CONTROLLER.TeamList != null && CONTROLLER.BattingTeamIndex >= 0
            && CONTROLLER.BattingTeamIndex < CONTROLLER.TeamList.Length
            && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList != null
            && CONTROLLER.StrikerIndex >= 0
            && CONTROLLER.StrikerIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length)
        {
            BatsmenInfo _bl = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList;
            if (_bl != null)
            {
                _srcHand = _bl.BattingHand;
                CONTROLLER.StrikerHand = (_bl.BattingHand == "L") ? "left" : "right";
                batsmanHand = CONTROLLER.StrikerHand;
            }
        }
        // POSITION + ORIENT + IDLE the striker at the crease (mirrors the ResetAll setup ~2813-2827). The direct
        // RpcAutomaticBall re-bowl skips ResetAll for the FIRST post-reconnect ball, so the batsman model was left
        // un-positioned and not in its idle pose → it looked "not spawned" even with the renderer enabled (tester:
        // "batsman spawn hi nahi howa"). The 2nd ball's normal BowlNextBall->ResetAll already fixes it.
        if (_batsmanTransform != null)
        {
            if (batsmanHand == "left")
            {
                _batsmanTransform.position = leftHandedBatsmanInitialPosition;
                _batsmanTransform.localScale = new Vector3(-1f, _batsmanTransform.localScale.y, _batsmanTransform.localScale.z);
                _batsmanTransform.eulerAngles = new Vector3(_batsmanTransform.eulerAngles.x, 90f, _batsmanTransform.eulerAngles.z);
            }
            else
            {
                _batsmanTransform.position = rightHandedBatsmanInitialPosition;
                _batsmanTransform.localScale = new Vector3(1f, _batsmanTransform.localScale.y, _batsmanTransform.localScale.z);
                _batsmanTransform.eulerAngles = new Vector3(_batsmanTransform.eulerAngles.x, 270f, _batsmanTransform.eulerAngles.z);
            }
        }
        if (batsmanAnim != null) batsmanAnim.Play("WCCLite_BatsmanIdle");
        currentActionState = -2;
        bowlerZoomCameraStartTime = Time.time;
        ConstantsData_M.MpLog($"[GroundController] Batting reconnect — armed first-ball presentation: pos={(_batsmanTransform != null ? _batsmanTransform.position.ToString() : "<null>")} hand={batsmanHand} srcBattingHand={_srcHand} strikerIdx={CONTROLLER.StrikerIndex} skinEnabled={(batsmanSkinRenderer != null && batsmanSkinRenderer.enabled)} active={(batsmanObject != null && batsmanObject.activeInHierarchy)}");
        // The BOWLER (position + idle pose) can't be set up yet: his index is re-broadcast by RpcSetBowler
        // which lands a beat AFTER OnReconnection. Defer the FULL setup until it arrives.
        StartCoroutine(CompleteReconnectSetupAfterBowlerKnown());
    }

    private IEnumerator CompleteReconnectSetupAfterBowlerKnown()
    {
        // The first post-reconnect ball arrives via RpcAutomaticBall DIRECTLY, skipping the normal
        // BowlNextBall->ResetAll that positions + idle-animates the BOWLER (and keeper/fielders/camera). So the
        // bowler was left un-positioned / wrong-posed (tester: "bowler ki position aur animation state nahi
        // sync"). CONTROLLER.CurrentBowlerIndex is re-broadcast by RpcSetBowler ~50ms after OnReconnection, so
        // wait briefly, then derive the bowler's data (SetGameDatas) + map the type string exactly like
        // BowlNextBall (~18510-18532), and run the full ResetAll. ResetAll is pure-LOCAL setup — it positions +
        // idle-animates batsman/bowler/keeper/fielders + camera with NO network or delivery side effects
        // (verified), and runs ~8s before the ball, so it cannot disturb the incoming delivery.
        float t = 0f;
        while (t < 1.5f) { t += Time.unscaledDeltaTime; yield return null; }
        Singleton<GameData>.instance?.SetGameDatas();   // derives CONTROLLER.StrikerHand/BowlerHand/BowlerType from the restored indices
        batsmanHand = CONTROLLER.StrikerHand;
        bowlerHand = CONTROLLER.BowlerHand;
        if (CONTROLLER.BowlerType == 0)
        {
            bowlerType = "fast";
        }
        else if (CONTROLLER.BowlerType == 1 || CONTROLLER.BowlerType == 2)
        {
            bowlerType = "spin";
            bowlerSpinType = CONTROLLER.BowlerType;
            if (bowlerHand == "left")
            {
                bowlerSpinType = (bowlerSpinType == 1) ? 2 : ((bowlerSpinType == 2) ? 1 : bowlerSpinType);
            }
        }
        else if (CONTROLLER.BowlerType == 3)
        {
            bowlerType = "medium";
        }
        // R_firstturn fix: the first post-reconnect ball arrives DIRECTLY via the staying bowler's auto-bowl
        // (RpcAutomaticBall/RpcLockAngle -> StartBowling sets currentActionState=0), racing this fixed-1.5s
        // deferred setup. If that delivery has already begun, a blind ResetAll()+state=-2 here STOMPS it (and
        // ResetAll clears isBallReleased/ballReleaseProcessedThisDelivery) -> the bowler's RPC_SyncBallRelease
        // then hits the `currentActionState != 2` guard, is DROPPED (non-buffered ClientRpc = permanent), and the
        // first ball becomes unplayable ("first turn skipped after reconnect"). So only re-pose when NO delivery
        // has started; otherwise preserve the live 0/1/2/3 progression so the release is accepted at state 2.
        bool _deliveryAlreadyStarted = isBallReleased || ballReleaseProcessedThisDelivery
            || currentBallStatus == "bowling" || currentActionState >= 0;
        if (_deliveryAlreadyStarted)
        {
            ConstantsData_M.MpLog($"[GroundController] Reconnect FULL setup: first delivery already started (state={currentActionState} released={isBallReleased} status='{currentBallStatus}') — skipping ResetAll/-2 to keep the first ball playable.");
        }
        else
        {
            ResetAll();
            currentActionState = -2;
            bowlerZoomCameraStartTime = Time.time;
        }
        // Show the bottom score strip (Scoreboard.StripBG): the direct-RpcAutomaticBall re-bowl skips the normal
        // ShowBowlingInterface that activates it. StripBG is only ever SetActive(TRUE) (never false), so it starts
        // INACTIVE after the scene reload and, once shown, persists — but on the first post-reconnect ball it was
        // never activated, so the bottom bar was missing until the 2nd ball (tester). Force it on with the
        // bowler-to-batsman text (CurrentBowlerIndex/StrikerIndex are restored + just re-derived above).
        if (Singleton<Scoreboard>.instance != null)
        {
            // R4 (#10): re-activate the FULL HUD (scoreBoard container + score bar + player names + pause/back
            // button). Like StripBG, these are show-once GameObjects (Scoreboard.Awake -> Hide(true)) that start
            // INACTIVE after the scene reload; a mid-delivery reconnect previously left only the pitch + 4 players
            // visible. Hide(false) is the same call match-start uses — it sets scoreBoard.SetActive(true) +
            // HidePause(false) (un-hides the pause/back button).
            Singleton<Scoreboard>.instance.Hide(boolean: false);
            Singleton<Scoreboard>.instance.HideStrip(boolean: false);
            Singleton<Scoreboard>.instance.BowlerToBatsman();
        }
        ConstantsData_M.MpLog($"[GroundController] Reconnect FULL setup done: bowlerType={bowlerType} bowlerHand={bowlerHand} batHand={batsmanHand} bowlerIdx={CONTROLLER.CurrentBowlerIndex} bowlerPos={(bowlerObject != null ? bowlerObject.transform.position.ToString() : "<null>")}");
    }

    // #2a — BOWLER reconnecter HUD restore. On a bowler disconnect the reconnecter's scene reloads, so the
    // bottom score strip (Scoreboard.StripBG) + the full HUD (scoreBoard container / score bar / names /
    // pause-back) start INACTIVE; the bowler's direct re-bowl path never re-activates them, so the bottom
    // score panel was missing after the bowler reconnected (tester: "bottom score panel not appear if bowler
    // disconnect while standing"). These are show-once GameObjects (only ever SetActive(true)), so re-showing
    // them is idempotent. Deliberately LIGHT: no ResetAll / no re-position / no state change — the bowler's
    // position + keeper/fielders are already re-homed by CancelInFlightDeliveryForReconnect, and the auto-bowl
    // timer is already armed, so touching either could interrupt the bowler's own next delivery.
    public void ShowReconnectHudForBowler()
    {
        // ── Bug-fix S5 (bowler side shows only the pitch / 2-4 players) ──
        // After the bowler's scene reload the player/fielder skin renderers are serialized OFF and the
        // light HUD-only path never re-enabled them (the working BATTING path does, via ShowAllPlayers in
        // ArmBattingPreviewAfterReconnect). ShowAllPlayers/EnableFielders only flip renderer.enabled —
        // idempotent, no position/state/timer side effects — so they are safe on every bowler reconnect.
        ShowAllPlayers();
        EnableFielders(boolean: true);

        // S2b: restore CurrentBowlerIndex on the BOWLING reconnecter from the wire-durable syncedBowlerListIndex
        // BEFORE the SetGameDatas re-derive below, or a stale/default(0) index makes it derive the WRONG BowlerType
        // (fast vs spin) — the desync that persisted 3-4 turns until the over ended (a fresh CmdSetBowler resynced
        // both). RPC_SetBowler/OnBowlerIndexSynced map the index only on the BATTING side; this covers the bowler.
        if (BowlingScoreCard.instance != null) BowlingScoreCard.instance.RestoreBowlerIndexOnReconnect();

        // ── Bug-fix S2 (bowler type differs spin-vs-fast on the two screens) ──
        // bowlerType/bowlerHand reset to their scene-reload defaults ("fast"/"right") on reconnect, and ONLY
        // the batting path re-derived them (CompleteReconnectSetupAfterBowlerKnown), so the bowler-reconnecter
        // stayed "fast" while the batting client held the true spin/medium. Re-derive from the restored indices
        // exactly like that path. Pure-local read; bounds-guarded so a stale/sentinel index can never throw.
        if (CONTROLLER.TeamList != null
            && CONTROLLER.BowlingTeamIndex >= 0 && CONTROLLER.BowlingTeamIndex < CONTROLLER.TeamList.Length
            && CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList != null
            && CONTROLLER.CurrentBowlerIndex >= 0
            && CONTROLLER.CurrentBowlerIndex < CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList.Length)
        {
            Singleton<GameData>.instance?.SetGameDatas(); // derives CONTROLLER.BowlerHand/BowlerType from indices
            bowlerHand = CONTROLLER.BowlerHand;
            if (CONTROLLER.BowlerType == 0)
            {
                bowlerType = "fast";
            }
            else if (CONTROLLER.BowlerType == 1 || CONTROLLER.BowlerType == 2)
            {
                bowlerType = "spin";
                bowlerSpinType = CONTROLLER.BowlerType;
                if (bowlerHand == "left")
                {
                    bowlerSpinType = (bowlerSpinType == 1) ? 2 : ((bowlerSpinType == 2) ? 1 : bowlerSpinType);
                }
            }
            else if (CONTROLLER.BowlerType == 3)
            {
                bowlerType = "medium";
            }
        }
        // NOTE: S8 (bowler shown lying/mis-posed) is deliberately NOT handled here. Re-posing via ResetAll
        // could disturb the bowler's already-armed auto-bowl timer (see the method's original design note),
        // and the /Bowler Animator default state is already the standing idle — so the true cause is unconfirmed
        // and needs a 2-device repro before a re-pose is added.

        if (Singleton<Scoreboard>.instance != null)
        {
            Singleton<Scoreboard>.instance.Hide(boolean: false);      // scoreBoard container + un-hide pause/back
            Singleton<Scoreboard>.instance.HideStrip(boolean: false); // bottom score strip (StripBG)
            Singleton<Scoreboard>.instance.BowlerToBatsman();         // refresh the bowler→batsman strip text
        }

        // Red-indicator sync (tester 09-07 cases 2/4: "reconnection py red indicator point ka difference tha
        // dono screens py, phir ball ki direction automatically change ho gayi"): the reconnected bowler's
        // bowlingSpot reloads at the scene default while the staying batter still shows the pre-disconnect
        // marker — two different red points, and the next delivery aims at the bowler's (default) spot, so the
        // batter sees the direction "change by itself". The bowler owns the spot: re-broadcast its current spot
        // so both markers agree from the first re-bowled ball (same relay a manual drag uses; idempotent).
        if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null && bowlingSpot != null)
        {
            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id,
                new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
            ConstantsData_M.MpLog($"[GroundController][ShowReconnectHudForBowler] Re-broadcast bowling spot {bowlingSpot.position} so both indicators agree.");
        }
        ConstantsData_M.MpLog($"[GroundController] Bowler reconnect — HUD + renderers restored, bowlerType re-derived (#2a). bowlerType={bowlerType} bowlerHand={bowlerHand}");
    }

    // Belt-and-suspenders bowler-type restore, callable the instant ApplyMatchStateSnapshot restores
    // CONTROLLER.CurrentBowlerIndex. The snapshot restores the bowler INDEX but not the local bowlerType/
    // bowlerHand STRINGS that the visual model + ball physics read — those were re-derived only on some reconnect
    // paths (batting: CompleteReconnectSetupAfterBowlerKnown ~1.5s later; bowler: ShowReconnectHudForBowler), so
    // the type could stay stale for the FIRST post-reconnect ball ("bowler type swapped spin<->fast for one turn",
    // #7/#9/#11). Re-derive here immediately on BOTH clients. Pure-local read; bounds-guarded so a stale/sentinel
    // index can never throw (SetGameDatas indexes TeamList[BowlingTeamIndex].PlayerList[CurrentBowlerIndex] + striker).
    public void RefreshBowlerTypeFromRestoredIndex()
    {
        if (CONTROLLER.TeamList == null) return;
        if (CONTROLLER.BowlingTeamIndex < 0 || CONTROLLER.BowlingTeamIndex >= CONTROLLER.TeamList.Length) return;
        if (CONTROLLER.BattingTeamIndex < 0 || CONTROLLER.BattingTeamIndex >= CONTROLLER.TeamList.Length) return;
        // DO NOT derive the bowler while the team pair is collapsed. SetGameDatas reads the rank out of
        // TeamList[BowlingTeamIndex].PlayerList[CurrentBowlerIndex] — so a momentarily wrong BowlingTeamIndex
        // resolves the SAME bowler index to a DIFFERENT player, and therefore a different type. The 05-08
        // logs show it on one device: "idx=9 type=fast" and later "idx=9 type=medium". That is the tester's
        // "reconnection py bowlers different — ik ny spin bowler ny ball ki, opponent side fast bowler", and
        // it is also why the keeper stood at different depths on the two screens (keeper depth derives from
        // bowlerType). The pair genuinely does collapse — the TeamPair marker fired 19 times in these logs.
        // Skip; the caller re-derives once the indices are settled.
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            ConstantsData_M.MpLog($"[GroundController][RefreshBowlerType] SKIPPED — batting and bowling are both {CONTROLLER.BattingTeamIndex}; deriving the bowler now would read the wrong team's player list.");
            return;
        }
        var bpl = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList;
        var tpl = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList;
        if (bpl == null || CONTROLLER.CurrentBowlerIndex < 0 || CONTROLLER.CurrentBowlerIndex >= bpl.Length) return;
        if (tpl == null || CONTROLLER.StrikerIndex < 0 || CONTROLLER.StrikerIndex >= tpl.Length) return;
        Singleton<GameData>.instance?.SetGameDatas();   // derives CONTROLLER.StrikerHand/BowlerHand/BowlerType from the restored indices
        bowlerHand = CONTROLLER.BowlerHand;
        // SetGameDatas re-derives the STRIKER's hand too, but only the bowler's strings were being copied back
        // into the instance fields — and `batsmanHand` re-defaults to "right" on the scene reload. So a LEFT
        // batsman showed as right-handed for exactly one ball after a reconnect, then self-healed on the next
        // BowlNextBall->ResetAll (which does this same copy). This is the only restore path that runs on BOTH
        // clients, so it is the one that has to do it.
        batsmanHand = CONTROLLER.StrikerHand;
        if (CONTROLLER.BowlerType == 0)
        {
            bowlerType = "fast";
        }
        else if (CONTROLLER.BowlerType == 1 || CONTROLLER.BowlerType == 2)
        {
            bowlerType = "spin";
            bowlerSpinType = CONTROLLER.BowlerType;
            if (bowlerHand == "left")
            {
                bowlerSpinType = (bowlerSpinType == 1) ? 2 : ((bowlerSpinType == 2) ? 1 : bowlerSpinType);
            }
        }
        else if (CONTROLLER.BowlerType == 3)
        {
            bowlerType = "medium";
        }
        // bowlTeam is in the line because the same idx resolving to two different types is only explicable by
        // this value having changed between the calls.
        ConstantsData_M.MpLog($"[GroundController] Snapshot bowler-type re-derived: idx={CONTROLLER.CurrentBowlerIndex} type={bowlerType} hand={bowlerHand} batHand={batsmanHand} strikerIdx={CONTROLLER.StrikerIndex} bowlTeam={CONTROLLER.BowlingTeamIndex} batTeam={CONTROLLER.BattingTeamIndex}");
    }

}
