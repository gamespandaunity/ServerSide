// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.Lockstep — the DETERMINISTIC LOCKSTEP subsystem (Docs/DETERMINISTIC_LOCKSTEP.md).
// Extracted from the .Delivery partial (Phase 3). Owns: the per-delivery seeded-RNG helpers
// (DetRange), the swing-input packet (send/receive), the canonical contact-plane crossing stash,
// contact resolution (ResolveLockstepBatContact -> shot tables -> post-contact RNG re-seed),
// the 8-ball-style value relay apply, the LOCKED DELIVERY PLAN launch, and the bowling-side
// release-pose hold. Kill-switch: ConstantsData_M.useDeterministicContact.
// RULE: every DetRange call must execute on BOTH clients in the same order.
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

public partial class GroundController
{
    // Deterministic-lockstep shot rolls (Docs/DETERMINISTIC_LOCKSTEP.md): every random in the shot
    // direction tables + power math draws through here. Flag OFF (today) = legacy UnityEngine.Random,
    // byte-identical behaviour. Flag ON = the per-delivery seeded stream, identical on both clients —
    // the batter's computed shot equals what the bowler computes from the same relayed input.
    private float DetRange(float min, float max)
        => ConstantsData_M.useDeterministicContact ? DeterministicRng.Range(min, max) : UnityEngine.Random.Range(min, max);

    private int DetRange(int min, int max)
        => ConstantsData_M.useDeterministicContact ? DeterministicRng.Range(min, max) : UnityEngine.Random.Range(min, max);

    // ── Deterministic-lockstep shot (Docs/DETERMINISTIC_LOCKSTEP.md) ─────────────────────────────
    // The batter's swing travels as ONE input packet; BOTH clients resolve the identical contact at the
    // deterministic tick the ball crosses the contact plane, through the seeded shot tables. No result
    // relay, no 20Hz stream, no adopt/snap. v1 note: a very late-arriving input (ball already past the
    // plane on the follower) resolves at the current substep position — the acceptance rig will show if
    // that residual needs the parametric back-solve.
    private bool _lockstepSwingArmed;
    private bool _lockstepContactResolved;

    // How long the FOLLOWER may hold the contact resolve waiting for the batter's exact numbers
    // (RPC_ChangeBallAngle) before falling back to its seeded local result.
    //
    // The gap is NOT network latency — it reproduces at 0 ping locally. It is the fixed gameplay offset
    // built into the deterministic model: the batting side runs lockstepFollowerFlightDelay (~0.45s)
    // BEHIND, so on a delivery the batter is batting, the bowling/watching client's ball reaches the
    // contact plane ~0.45s EARLIER than the batter's does. The batter can only compute + relay its exact
    // contact numbers when ITS ball arrives, so those numbers land ~0.45s after this client has already
    // crossed. A short window could never cover that, which is why 15 of 21 deliveries dropped.
    //
    // So size the window to the flight delay itself (+ a few ticks margin) — derived, not magic, so it
    // tracks lockstepFollowerFlightDelay if that is retuned. Holding the resolve does NOT stall the ball:
    // when the numbers finally land the existing late-input path rewinds to the stashed canonical crossing
    // and re-flies the post-shot trajectory — the "one smooth correction per shot" the rewind is built for.
    private static int LOCKSTEP_RELAY_GRACE_TICKS =>
        Mathf.CeilToInt(ConstantsData_M.lockstepFollowerFlightDelay / BALL_SIM_FIXED_STEP) + 4;
    private bool _lockstepSendSwingNextFrame;
    private bool _lockstepInputKnown;      // batter's decision (shot OR leave) is known on this client
    private bool _lockstepCrossed;         // ball has crossed the contact plane this delivery
    private Vector3 _lockstepCrossPos;     // deterministic state AT the crossing — the canonical contact point
    private Vector3 _lockstepCrossTemp;
    private int _lockstepCrossTick;
    // Tick at which the batter's swing INPUT reached this client. The relay grace window is anchored at
    // max(crossTick, this) because the batter's exact numbers can only follow its input, never precede it.
    private int _lockstepSwingArmedTick;
    private float _lockstepBatterX;        // batter's x AT the swing, from the input packet (streamed batsman lags at high ping)
    // 8-ball-style VALUE relay: the batter's RESOLVED contact result (shipped via the existing
    // RPC_ChangeBallAngle). With the follower-flight delay it arrives BEFORE this side's ball reaches the
    // bat, so the follower applies the batter's EXACT numbers — the seeded local compute is the fallback
    // for the rare case the relay loses the race.
    // LOCKED DELIVERY PLAN: minted on the bowling side at run-up start (spot frozen, params+seed shipped);
    // armed on the batting side when the plan RPC lands — the batter's own bowler anim event then launches.
    private bool _lockedPlanMinted;   // bowling side: this delivery's release was pre-minted (skip mint/send/hold at the anim event)
    private bool _lockedPlanArmed;    // batting side: plan received — launch at MY OWN anim event
    private Vector3 _lockedPlanBallPos;   // plan's release start — re-applied at launch (nothing may nudge the ball during the 5s run-up)
    private Vector3 _lockedPlanTempPos;
    private bool _lockstepHasRelayedResult;
    // Set when the batting authority relays a NON-bat status (e.g. "onPads") for this delivery. Without it
    // the follower had no way to learn that the batter had ruled at all when the ruling was not a shot, so
    // it waited out the relay window and then resolved a bat contact from the fallback. See the
    // ContactRuling branch in RPC_ChangeBallAngle.
    private bool _lockstepRelayedNoBatContact;
    private float _lockstepRelayedAngle;
    private float _lockstepRelayedHVel;
    private float _lockstepRelayedLaunch;
    private float _lockstepRelayedArc;
    private float _lockstepRelayedFbd;
    private float _lockstepRelayedAcr;
    private float _lockstepRelayedBch;
    private float _lockstepRelayedHAdj;
    private bool _lockstepRelayedEdge;
    private const float LOCKSTEP_CONTACT_Z = 6.3f;   // mean authoritative contact z from [ShotContact] data

    // Keeper hold (lockstep review BUG 2): at 250ms RTT the swing input reaches the follower AFTER its
    // local ball has already reached the keeper (~160ms past the plane) — without this hold the keeper
    // consumed a ball the batter actually hit. Collect waits until the input (shot or relayed leave) is
    // known, or a generous silence-grace (~45 ticks ≈ 750ms) past the crossing has elapsed.
    internal bool LockstepKeeperGateOpen =>
        !LockstepActive || _lockstepInputKnown
        || (_lockstepCrossed && _ballSimTicksDone > _lockstepCrossTick + 45);

    internal bool LockstepActive =>
        ConstantsData_M.useDeterministicContact && CONTROLLER.PlayModeSelected == 8 && !GameConstants.isWithAI;

    // Batter side: fired the frame AFTER the swing input was accepted (shot selection is complete by then).
    private void LockstepSendShotInput()
    {
        if (!LockstepActive || CricketNetworkManager.instance == null) return;
        if (CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex) return;   // only the batter owns the input
        _lockstepSwingArmed = true;   // arm our own tick-resolve too — both sides run the same path
        _lockstepInputKnown = true;
        _lockstepBatterX = _batsmanTransform != null ? _batsmanTransform.position.x : 0f;
        CricketNetworkManager.instance.CmdShotInput(
            staticVariables.UserProfiledata.user._id,
            Mirror.NetworkTime.time,
            currentShotPlayed,
            currentBatsmanAnimation,
            isPowerShotActive,
            shotAngleValue,
            _lockstepBatterX,
            attemptedSquareLegGlance,
            desiredAnimationSpeed,
            isBatsmanConfident,
            isPerfectShot,
            isMistimedShot,
            horizontalSpeedAdjustment,
            firstBounceSpeedMultiplier);
        ConstantsData_M.MpLog($"[Lockstep] Swing input sent: shot={currentShotPlayed} power={isPowerShotActive} angleVal={shotAngleValue:F1} sqLeg={attemptedSquareLegGlance}"
            + $" perf={isPerfectShot} mist={isMistimedShot} hAdj={horizontalSpeedAdjustment:F2} fbM={firstBounceSpeedMultiplier:F2}");
    }

    // Follower side: the batter's input arrives — mirror the swing locally; the tick path resolves contact.
    public void OnLockstepShotInput(double swingNetTime, string shotPlayed, string batsmanAnimName,
        bool isPower, float angleValue, float batterX, bool squareLegGlance,
        float animSpeed, bool batsmanConfident, bool perfect, bool mistimed, float hSpeedAdj, float fbMult)
    {
        if (!LockstepActive) return;
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex) return; // the batter armed itself already
        currentShotPlayed = shotPlayed;
        currentBatsmanAnimation = batsmanAnimName;
        isPowerShotActive = isPower;
        shotAngleValue = angleValue;
        attemptedSquareLegGlance = squareLegGlance;   // table-affecting input state — must match the batter's
        _lockstepBatterX = batterX;                   // the streamed batsman lags at high ping; tables read this instead
        desiredAnimationSpeed = animSpeed;            // both feed the launch-physics tables — batter-local otherwise
        isBatsmanConfident = batsmanConfident;
        isPerfectShot = perfect;                      // timing quality — don't depend on RPC_ShotTiming arrival order
        isMistimedShot = mistimed;
        horizontalSpeedAdjustment = hSpeedAdj;
        firstBounceSpeedMultiplier = fbMult;
        batsmanTriggeredAttemptedShot = true;
        if (batsmanAnim != null && !string.IsNullOrEmpty(batsmanAnimName))
            StartCoroutine(PlayFollowerSwingAnimOnMyTimeline(batsmanAnimName));
        _lockstepSwingArmed = true;
        _lockstepSwingArmedTick = _ballSimTicksDone;   // anchor for the relay grace window (see below)
        _lockstepInputKnown = true;    // keeper hold releases — the batter's decision is in
        ConstantsData_M.MpLog($"[Lockstep] Swing input received: shot={shotPlayed} — contact resolves at the canonical crossing.");
    }

    // The batter swung against ITS real-time ball; this side's ball runs lockstepFollowerFlightDelay
    // behind. Playing the swing on packet ARRIVAL made the bat swing ~0.45s before the ball reached it
    // on this screen — wait until OUR ball hits the same moment. If the delivery moves on first (status
    // change / crossing resolve), play immediately so the visual never goes missing.
    private IEnumerator PlayFollowerSwingAnimOnMyTimeline(string animName)
    {
        float wait = ConstantsData_M.lockstepFollowerFlightDelay - (float)(Mirror.NetworkTime.rtt / 2.0);
        if (wait > 0f)
        {
            double until = Mirror.NetworkTime.time + wait;
            while (Mirror.NetworkTime.time < until && currentBallStatus == "bowling" && !_lockstepContactResolved)
            {
                yield return null;
            }
        }
        // batsmanTriggeredAttemptedShot resets in ResetAll — a cancelled delivery must not replay a stale swing
        if (batsmanAnim != null && batsmanTriggeredAttemptedShot)
        {
            batsmanAnim.Play(animName);
        }
    }

    // Both sides, per fixed sub-step: stash the canonical crossing state, then resolve when the input is in.
    private void LockstepSubstep()
    {
        if (currentBallStatus != "bowling") return;
        if (!_lockstepCrossed && temporaryPosition.z >= LOCKSTEP_CONTACT_Z)
        {
            _lockstepCrossed   = true;                       // the ONE canonical contact moment, identical on both clients
            _lockstepCrossPos  = matchBallTransform.position;
            _lockstepCrossTemp = temporaryPosition;
            _lockstepCrossTick = _ballSimTicksDone;
            ConstantsData_M.MpLog($"[Lockstep] Crossed contact plane: tick={_lockstepCrossTick} rng={DeterministicRng.Count} pos={temporaryPosition}");
        }
        LockstepTryResolveContact();
    }

    private void LockstepTryResolveContact()
    {
        if (!_lockstepSwingArmed || _lockstepContactResolved) return;
        if (currentBallStatus != "bowling") { _lockstepContactResolved = true; return; }   // pads/bowled beat us — legacy path owns those
        // The batter ruled this delivery did not come off the bat. Stop waiting for numbers that will never
        // come and take the same no-contact path a leave takes — the ball runs on to the keeper — instead of
        // timing out and inventing a bat contact from the fallback.
        if (_lockstepRelayedNoBatContact && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
        {
            _lockstepContactResolved = true;
            // Apply the batter's relayed ball state before returning. Marking the contact resolved and
            // leaving it at that kept the deterministic sim flying the delivery straight on to the rope
            // while the batting screen had the ball stopped dead off the pad — the scores matched, the two
            // balls did not. ApplyRelayedContactResultIfAny is the same routine the bat path uses; a pad
            // deflection is just another set of post-contact numbers.
            ApplyRelayedContactResultIfAny();
            ConstantsData_M.MpLog($"[Lockstep][ContactRuling] Resolved as NO bat contact — applied the batting authority's ball state (angle={_ballAngle:F1} hVel={horizontalVelocity:F1}).");
            return;
        }
        if (!_lockstepCrossed) return;                        // resolve exactly at/after the canonical crossing

        // BALL-DIVERGENCE FIX. The swing INPUT (RpcShotInput) and the batter's exact contact NUMBERS
        // (RPC_ChangeBallAngle) are two separate messages, and the batter can only send the numbers AFTER
        // resolving its own contact — so they always trail the input. This resolve only waited for the
        // input, so the follower resolved from its seeded fallback and RPC_ChangeBallAngle then hit the
        // "arrived AFTER the local resolve — dropped" path: in the 07-25 pair that happened on 15 of 21
        // deliveries, and those balls flew somewhere completely different on the two screens ("ek side pe
        // 4, doosri side ball fielder ke haath"; follower logs show shot angle ~95 = the untouched
        // delivery angle instead of a real shot angle 258-295).
        // So on the FOLLOWER hold the resolve until the numbers land (window = the flight-delay offset,
        // since that IS the gap — see LOCKSTEP_RELAY_GRACE_TICKS). Safe by construction: resolving later
        // rewinds the deterministic state to the stashed canonical crossing (the rewind just below), so
        // the contact point stays identical to the batter's. Bounded — a genuinely lost relay still
        // resolves from the fallback once the window expires.
        // Anchor the window at whichever came LAST — the crossing or the swing input. The batter can only
        // send its numbers AFTER its own contact, and the swing INPUT always precedes those numbers; so if
        // the batter swings late the input itself lands after our crossing, and a window measured from the
        // crossing alone has already burned down before the numbers can possibly arrive. 26-07 ball 0.2:
        // crossing at line 2043, swing input at 2058 (AFTER it), window expired at 2064, numbers arrived at
        // 2076 and were dropped — the two screens then flew 275.0/20.4 against 260.8/25.0. Ball 0.1, where
        // the input preceded the crossing, applied correctly. Measuring from the input fixes the late-swing
        // case without extending the wait at all when the input arrives early.
        int relayWaitAnchor = Mathf.Max(_lockstepCrossTick, _lockstepSwingArmedTick);
        if (!_lockstepHasRelayedResult
            && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex
            && !string.IsNullOrEmpty(currentShotPlayed) && currentShotPlayed != "bt6Leave"
            && _ballSimTicksDone < relayWaitAnchor + LOCKSTEP_RELAY_GRACE_TICKS)
        {
            return;
        }
        if (!_lockstepHasRelayedResult && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex
            && !string.IsNullOrEmpty(currentShotPlayed) && currentShotPlayed != "bt6Leave")
        {
            ConstantsData_M.MpLog($"[Lockstep] Relay window ({LOCKSTEP_RELAY_GRACE_TICKS} ticks) expired without the batter's numbers — resolving from the fallback.");
        }

        _lockstepContactResolved = true;
        if (currentShotPlayed == "bt6Leave" || string.IsNullOrEmpty(currentShotPlayed)) return;   // a leave: ball runs on to the keeper
        // Late input (high ping): the ball has flown past the crossing while the packet travelled. Rewind the
        // deterministic state to the stashed crossing — contact point identical to the batter's — and let the
        // fixed-step catch-up re-fly the post-shot path to 'now' (the one smooth correction per shot).
        if (_ballSimTicksDone > _lockstepCrossTick + 2)
        {
            ConstantsData_M.MpLog($"[Lockstep] Late input — rewinding {_ballSimTicksDone - _lockstepCrossTick} ticks to the canonical crossing for the contact.");
            matchBallTransform.position = _lockstepCrossPos;
            temporaryPosition           = _lockstepCrossTemp;
            _ballSimTicksDone           = _lockstepCrossTick;
        }
        ResolveLockstepBatContact();
    }

    // The live collider bat-contact block (GroundController.OnCustomTriggerEnter), replicated for the tick
    // path: contact point/height come from the DETERMINISTIC ball position — identical on both clients.
    private void ResolveLockstepBatContact()
    {
        int rngBefore = DeterministicRng.Count;   // diagnostics: draw-sequence alignment across clients
        didBallHitBat = true;
        HideBowlingSpot();
        ShowFullTossSpot(_Value: false);
        summarySaved = "connected";
        ballConnectionPositionSaved = matchBallTransform.position;
        ballSpinSpeedZ = DetRange(-3600, -1800);
        ballConnectedSpinSpeedZSaved = ballSpinSpeedZ;
        ballStartPoint.position = new Vector3(temporaryPosition.x, ballStartPoint.position.y, temporaryPosition.z);
        batContactHeight = temporaryPosition.y;
        bounceCount = 0;
        currentBallStatus = "shotSuccess";
        ActivateColliders(boolean: false);
        ActivateStadiumAndSkybox(boolean: true);
        BoundaryBoardCollider.SetActive(value: true);
        if (currentShotPlayed != "bt6Defense" && currentShotPlayed != "backFootDefenseHighBall" && currentShotPlayed != "frontFootOffSideDefense")
        {
            canRun = true;
            if (Singleton<GameData>.instance != null) Singleton<GameData>.instance.EnableRun(boolean: true);
        }
        if (batsmanAnim != null && !string.IsNullOrEmpty(currentBatsmanAnimation))
            SetCurrentBatsmanAnimSpeed(1f);
        BallTiming();   // seeded tables -> identical direction/power on both clients
        ApplyRelayedContactResultIfAny();   // follower: override with the batter's EXACT resolved numbers (8-ball-style value relay)
        if (isEdgeCaught && DistanceBetweenTwoGameObjects(wicketKeeperObject, matchBall) < 13.5f)
        {
            canKeeperCatchBall = true;
        }
        else if (!stopKeeper)
        {
            MoveWicketKeeperToStumps();
        }
        GetFieldersAngle();
        GetFieldersDistance();
        if (!stopKeeper) SetActiveFielders();
        // Follower: overwrite the locally computed chase points with the batting authority's relayed ones
        // (stashed in RPC_SetActiveFielderSetup). Runs here, at OUR resolve, so the chase timing stays on
        // this side's delayed timeline while the destinations match the batter's exactly — the two screens
        // were otherwise sending chasers to different spots from identical contact values.
        ApplyStashedFielderSetupIfAny();
        umpireRunSignalDirection = (_ballAngle >= 90f && _ballAngle <= 270f) ? -1 : 1;
        currentActionState = 4;
        // Diagnostic: catch-vs-chase decision per side (13-07: local chose ground-chase while the batter's
        // fielder CAUGHT it — inputs were identical, so if these lines differ the FIELD PLACEMENT diverged).
        if (activeFielders != null && activeFielders.Count > 0)
        {
            var _fsb = new System.Text.StringBuilder("[Lockstep] FielderSetup:");
            for (int _fi = 0; _fi < activeFielders.Count; _fi++)
            {
                int idx = activeFielders[_fi];
                string act = _fi < activeFieldersActions.Count ? activeFieldersActions[_fi] : "?";
                Vector3 cp = (fielderChasePoints != null && idx < fielderChasePoints.Length && fielderChasePoints[idx] != null)
                    ? fielderChasePoints[idx].transform.position : Vector3.zero;
                Vector3 fp = (fielders != null && idx < fielders.Length && fielders[idx] != null)
                    ? fielders[idx].transform.position : Vector3.zero;
                _fsb.Append($" f{idx}={act}@cp({cp.x:F1},{cp.z:F1})pos({fp.x:F1},{fp.z:F1})");
            }
            ConstantsData_M.MpLog(_fsb.ToString());
        }
        // Post-contact stream re-align: the two clients can consume a DIFFERENT number of table rolls in
        // BallTiming (profile/difficulty-gated branches — test logs: rng 0->5 vs 0->6). The contact itself is
        // covered by the value relay, but every LATER seeded roll (fence rebound, pad deflect) would stay
        // misaligned. Re-seed both streams from the same derived seed so post-contact draws align again.
        DeterministicRng.Seed(DeterministicRng.CurrentSeed ^ unchecked((int)0x5f3759df));
        ConstantsData_M.MpLog($"[Lockstep] Contact resolved: shot={currentShotPlayed} angle={_ballAngle:F1} hVel={horizontalVelocity:F1} contact={temporaryPosition}"
            + $" rng={rngBefore}->{DeterministicRng.Count} pow={isPowerShotActive} conf={isBatsmanConfident} perf={isPerfectShot} mist={isMistimedShot}"
            + $" hAdj={horizontalSpeedAdjustment:F2} fbM={firstBounceSpeedMultiplier:F2} animSpd={desiredAnimationSpeed:F2} edge={isEdgeCaught} slip={isSlipShot}"
            + $" spot={ballSpotLength:F1} ctrl={controlMultiplier:F2} fbd={firstBounceDistance:F1} launch={launchAngle:F1} arc={arcHeight:F2}");
    }

    // Follower only: replace the seeded local table result with the batter's relayed EXACT contact numbers
    // (stashed in RPC_ChangeBallAngle under lockstep). Runs right after BallTiming() inside
    // ResolveLockstepBatContact, so keeper/fielder setup + umpire direction all read the final values.
    // Also refreshes every consumer MultiPlayerBallAngle already fed with the (approximate) local numbers.
    private void ApplyRelayedContactResultIfAny()
    {
        if (!LockstepActive || !_lockstepHasRelayedResult) return;
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex) return;   // the batter computed the authoritative numbers itself
        bool hadEdge = isEdgeCaught;
        _ballAngle = _lockstepRelayedAngle;
        horizontalVelocity = _lockstepRelayedHVel;
        launchAngle = _lockstepRelayedLaunch;
        arcHeight = _lockstepRelayedArc;
        firstBounceDistance = _lockstepRelayedFbd;
        angleChangeRate = _lockstepRelayedAcr;
        batContactHeight = _lockstepRelayedBch;
        horizontalSpeedAdjustment = _lockstepRelayedHAdj;
        isEdgeCaught = _lockstepRelayedEdge;
        distanceToNextPitch = firstBounceDistance;
        savedBallAngle = _ballAngle;
        savedHorizontalBallSpeed = horizontalVelocity;
        savedBallLaunchAngle = launchAngle;
        savedBallLaunchHeight = arcHeight;
        savedBallFirstBounceDistance = firstBounceDistance;
        savedBallLaunchAnglePerSecond = angleChangeRate;
        isBallToFineLeg = _ballAngle > 20f && _ballAngle < 90f;
        ballFirstBounce.transform.position = new Vector3(
            ballStartPoint.position.x + firstBounceDistance * Mathf.Cos(_ballAngle * degToRad),
            ballFirstBounce.transform.position.y,
            ballStartPoint.position.z + firstBounceDistance * Mathf.Sin(_ballAngle * degToRad));
        if (Singleton<LeftFovLerp>.instance != null)
        {
            Singleton<LeftFovLerp>.instance.setBallAngle(_ballAngle);
            Singleton<LeftFovLerp>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (Singleton<RightSmoothFov>.instance != null)
        {
            Singleton<RightSmoothFov>.instance.setBallAngle(_ballAngle);
            Singleton<RightSmoothFov>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (Singleton<MainCameraController>.instance != null)
        {
            Singleton<MainCameraController>.instance.setBallAngle(_ballAngle);
            Singleton<MainCameraController>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (Singleton<GameData>.instance != null)
        {
            Singleton<GameData>.instance.GetFinalBallAngle(_ballAngle);
        }
        if (Singleton<AIFieldingSetupManager>.instance != null)
        {
            Singleton<AIFieldingSetupManager>.instance.lastHittedAngle = _ballAngle;   // was fed the pre-override local angle
        }
        RepositionSideCamera();   // the ShotTables tail positioned the side cam from the pre-override local angle (tester: camera angle differed per side)
        // CATCH-SPOT RECOMPUTE (13-07: f6 goForCatch on the batter but goForChase on the follower with
        // IDENTICAL fielder positions): FixBallCatchingSpot ran inside the tables BEFORE this override, so
        // the follower's catch point — and the goForCatch time comparison built on it — used the divergent
        // local numbers. Recompute from the final authoritative values; SetActiveFielders runs after this.
        preCatchDistance = 1f;
        FixBallCatchingSpot();
        // Arm the keeper for an edge the local compute missed — but ONLY if the batting authority has not
        // already ruled there was no edge.
        //
        // Two separate facts about the same delivery travel separately: the contact relay carries `edge`, and
        // CmdSetUltraEdgeDecision carries `isEdged` from the umpire path. On 04-08 they contradicted each
        // other on one ball, and the tester filed the two halves as a pair — Creator "review this", Joiner
        // "not review":
        //     [RpcSetUltraEdgeDecision] ... decision = notout, isEdged = False     (authority, arrives first)
        //     [Lockstep] Applied batter's exact contact result: ... edge=True      (contact relay, after)
        //     [KeeperState] -> catchAttempt ... clip=spinHipCatchAppeal
        //     [KeeperState] -> catchEnd (CAUGHT)
        // The follower ran a caught-behind appeal for an edge the authority had already said did not happen,
        // so one screen showed a review and the other did not.
        //
        // hasUltraEdgeDecision means the authority has spoken for this delivery; when it has, its word on
        // whether the ball was edged outranks the contact relay's flag. If it has not spoken yet the old
        // behaviour stands, so a genuine edge still arms the keeper as before.
        if (isEdgeCaught && !hadEdge && hasUltraEdgeDecision && !hasEdgeOccurred)
        {
            ConstantsData_M.MpLog("[Lockstep] Contact relay says EDGE but the authority's UltraEdge decision says NOT edged — not arming a caught-behind appeal. The authority's ruling wins.");
        }
        else if (isEdgeCaught && !hadEdge)
        {
            // the local compute missed the edge the batter resolved — arm the keeper the way the table path does
            currentWicketKeeperStatus = string.Empty;
            isWicketKeeperCatchingAnimationSelected = false;
            ActivateWicketKeeper();
        }
        ConstantsData_M.MpLog($"[Lockstep] Applied batter's exact contact result: angle={_ballAngle:F1} hVel={horizontalVelocity:F1} fbd={firstBounceDistance:F1} edge={isEdgeCaught} (seeded local numbers overridden)");
    }

    /// <summary>
    /// LOCKED DELIVERY PLAN — the batting client's own bowler anim event fires this (via BowlerController).
    /// The plan (params + seed) arrived before the run-up; this is the deferred LAUNCH half of
    /// RPC_SyncBallRelease: state flip + deterministic anchor at MY OWN anim instant + visuals. Each side
    /// anchors at its own anim event; the bowling side's run-up is delayed, so the relative flight offset
    /// equals lockstepFollowerFlightDelay by construction. Returns false when no plan is armed (legacy path).
    /// </summary>
    public bool TryLaunchFromLockedPlan()
    {
        if (!_lockedPlanArmed)
        {
            // The bowler's release anim event DID fire, but no plan is armed. Distinguishes "the animation
            // never got here" (no line at all) from "it got here and there was nothing to launch" (this
            // line) — the two have completely different causes and the logs alone could not tell them apart.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                ConstantsData_M.MpLog($"[Lockstep] Release anim event fired with NO plan armed (state={currentActionState} released={isBallReleased}) — nothing to launch on this side.");
            return false;
        }
        _lockedPlanArmed = false;
        matchBallTransform.position = _lockedPlanBallPos;   // the plan's release start — guard against any nudge during the run-up
        temporaryPosition = _lockedPlanTempPos;
        isBallReleased = true;
        currentActionState = 3;
        currentBallStatus = "bowling";
        ballReleaseTime = Time.time;
        // Arm the batting authority's own stall watchdog for this delivery (no-op on the bowling side).
        StartBattingDeliveryWatchdog();
        if (ConstantsData_M.useDeterministicDelivery && ConstantsData_M.useLocalDeliverySim
            && GameConstants.isWithAI == false && CONTROLLER.PlayModeSelected == 8)
        {
            _ballSimStartNetTime = Mirror.NetworkTime.time;   // MY anim event = MY anchor
            _ballSimTicksDone = 0;
            _ballSimMaxTargetTicks = -1;
            _useDeterministicBallStep = true;
        }
        ActivateColliders(boolean: true);
        ShowBall(status: true);
        if (BowlerBallSkinRenderer != null) BowlerBallSkinRenderer.enabled = false;
        if (Singleton<GameData>.instance != null && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !isReplayModeActive)
        {
            Singleton<GameData>.instance.EnableShotSelection(boolean: true);
            // Post-reconnect first-ball input fix (13-07: "reconnect ke baad ball kheli nahi gayi" —
            // activation=0.000, tap dropped, auto-LEAVE latched): the shot-input UI can come back INACTIVE
            // after the scene reload (same class as the auto-bowl timer fix) so the swipe never reaches the
            // selection pipeline. Re-activate the BattingControls panel chain; a no-op when already active.
            // Singleton<BattingControls>.instance is NULL when the reloaded panel came back INACTIVE
            // (Awake never ran) — the earlier instance-based revival silently no-opped, which is why the
            // "pehli ball miss" persisted on every batting-side reconnect. Find the scene object even when
            // inactive; activating it runs Awake and re-registers the singleton for the rest of the match.
            BattingControls _bc = Singleton<BattingControls>.instance;
            if (_bc == null) _bc = UnityEngine.Object.FindFirstObjectByType<BattingControls>(FindObjectsInactive.Include);
            if (_bc != null)
            {
                for (Transform _anc = _bc.transform; _anc != null; _anc = _anc.parent)
                {
                    if (!_anc.gameObject.activeSelf) _anc.gameObject.SetActive(true);
                }
            }
            ConstantsData_M.MpLog($"[Lockstep] Batting input state at launch: uia={Singleton<GameData>.instance.userInputAction}"
                + $" bcActive={(Singleton<BattingControls>.instance != null ? Singleton<BattingControls>.instance.gameObject.activeInHierarchy.ToString() : "null")}"
                + $" shotAllowed={isShotAllowed} tapped={batsmanTriggeredAttemptedShot}");
        }
        ConstantsData_M.MpLog($"[Lockstep] Locked-plan LAUNCH at my anim event: netTime={Mirror.NetworkTime.time:F3} angle={_ballAngle:F1} hVel={horizontalVelocity:F1}");
        return true;
    }

    // Deterministic DELIVERY driver (Phase 2): the run-up/release counterpart of StepBallDeterministic.
    // Advances BowlingBallMovement in fixed BALL_SIM_FIXED_STEP increments to catch up to the shared
    // NetworkTime, so the batting follower's local delivery sim (useLocalDeliverySim) runs the IDENTICAL
    // fixed-step path the bowling authority does from the SAME synced release instant → bit-identical
    // delivery flight, no residual frame-based drift. Same per-frame tick cap + cosmetic-substep guard.
    // Lockstep follower-flight delay (bowling side): freeze the bowler at the release pose with the in-hand
    // ball showing until the delayed flight anchor, then let the throw complete exactly as the ball launches.
    // Bails and restores on any early reset (status/isBallReleased change); animator speed is also restored
    // by ResetAll as a belt-and-braces.
    private IEnumerator HoldReleaseVisualsForFlightDelay()
    {
        double flightAnchor = _ballSimStartNetTime;   // already includes +lockstepFollowerFlightDelay
        ShowBall(status: false);
        if (BowlerBallSkinRenderer != null) BowlerBallSkinRenderer.enabled = true;
        if (bowlerAnimator != null) bowlerAnimator.speed = 0f;
        while (Mirror.NetworkTime.time < flightAnchor && currentBallStatus == "bowling" && isBallReleased)
        {
            yield return null;
        }
        if (bowlerAnimator != null) bowlerAnimator.speed = 1f;
        if (BowlerBallSkinRenderer != null) BowlerBallSkinRenderer.enabled = false;
        if (currentBallStatus == "bowling" && isBallReleased)
        {
            ShowBall(status: true);
        }
    }
}
