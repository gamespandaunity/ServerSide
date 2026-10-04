// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.DeterministicSim — the FIXED-TIMESTEP BALL INTEGRATOR (extracted Phase 3).
// Drives the legacy parametric integrators (BowlingBallMovement/BattingBallMovement) in 1/60s ticks
// anchored to Mirror NetworkTime, so both clients run the identical steps for the same elapsed time.
// BallSimTargetTicks keeps a monotonic high-water target (a backward NetworkTime resync is a no-op,
// never a freeze→burst). The bowling side's delivery anchor runs lockstepFollowerFlightDelay behind.
// Kill-switches: useDeterministicDelivery (delivery), _deterministicShotArmed (post-shot).
// ════════════════════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public partial class GroundController
{
    // ───────── Deterministic ball-flight stepping (cricket_determenstic, Phase 0) ─────────
    // The ball integrator (BallMovement/BattingBallMovement) historically advanced with Time.deltaTime —
    // frame-rate AND ping dependent, so each client integrated a slightly different path → divergence
    // (worst on spin: keeper-miss, ball-behind, camera jerk; the 20Hz sync RPCs are band-aids that
    // re-converge it). The rework drives the SAME integrator from Mirror's synced NetworkTime in FIXED
    // steps, so both clients run the identical number of identical steps for the same elapsed time →
    // matching flight with NO per-frame sync. While OFF (default) BallDt() returns Time.deltaTime, so
    // behaviour is unchanged; later phases flip it on per flight-phase (post-shot first, then delivery).
    private bool _useDeterministicBallStep = false;
    private float _ballStepDt;            // the fixed dt fed to the integrator during a deterministic step
    private double _ballSimStartNetTime;  // NetworkTime.time at flight start (delivery release / bat contact)
    private int _ballSimTicksDone;        // fixed steps already integrated this flight
    private const float BALL_SIM_FIXED_STEP = 1f / 60f;
    private bool _deterministicShotArmed = false;  // batting authority armed the post-shot deterministic flight
    // Issue #3 fix: per-flight high-water mark of the shared-clock target tick. A Mirror NetworkTime resync can
    // move NetworkTime.time BACKWARD mid-flight, which would park targetTicks BELOW _ballSimTicksDone -> toRun=0
    // (ball frozen in the bowler's hand ~1s), then a one-frame 8-tick burst snaps it through the pitch to the
    // keeper once the clock recovers. Clamping the per-frame target up to this monotonic high-water mark makes a
    // backward clock blip a no-op (no new ticks, no lost ticks) instead of a freeze, so the deficit -> burst can
    // never accumulate. It NEVER re-bases the seed and NEVER touches temporaryPosition/matchBallTransform, so the
    // two clients still integrate the SAME steps off the SAME shared clock and stay bit-identical. -1 = unset.
    private int _ballSimMaxTargetTicks = -1;
    private bool _ballSubstepIsFinal = true;       // false on intermediate fixed-step catch-up sub-steps so
                                                   // per-frame COSMETIC side-effects fire once (last sub-step
                                                   // only), while the physics integrator still runs every step

    // Issue #3 fix: shared-clock target tick count for the current deterministic flight, made monotonic
    // non-decreasing via _ballSimMaxTargetTicks so a backward Mirror.NetworkTime resync cannot push the target
    // below _ballSimTicksDone (which froze the ball in hand, then burst it to the keeper on recovery). Reads the
    // SAME shared clock both clients read and only ever raises the floor to a target THIS client already saw, so
    // it cannot invent ticks beyond real elapsed shared time -> both clients converge identically. Callers keep
    // the existing Mathf.Clamp(target - ticksDone, 0, 8) cap as the final per-frame safety bound.
    private int BallSimTargetTicks()
    {
        int rawTarget = (int)((Mirror.NetworkTime.time - _ballSimStartNetTime) / BALL_SIM_FIXED_STEP);
        if (rawTarget > _ballSimMaxTargetTicks) _ballSimMaxTargetTicks = rawTarget;
        return _ballSimMaxTargetTicks;
    }

    // dt the ball integrator uses this step: a fixed step while running the deterministic network-clock
    // catch-up, else the normal frame delta (legacy / offline / not-yet-converted flight phases).
    private float BallDt()
    {
        return _useDeterministicBallStep ? _ballStepDt : Time.deltaTime;
    }

    // Deterministic post-shot driver (Phase 1): instead of advancing the ball one frame-delta step per
    // frame (which makes the bowling follower's path diverge from the batting authority's), run the
    // integrator in FIXED steps to catch up to the shared NetworkTime. Both clients, started from the
    // same params + same _ballSimStartNetTime, run the identical number of identical steps for the same
    // elapsed networkTime → bit-identical flight. Per-frame tick count is capped so a hitch/stall can
    // never spin an unbounded catch-up loop (the ball just lags a frame or two, then re-converges).
    private void StepBallDeterministic()
    {
        int targetTicks = BallSimTargetTicks();   // Issue #3: monotonic shared-clock target (no backward-resync freeze)
        // Post-shot jerk fix (bowling side, power/boundary shots): the shot's camera zoom + side-cam switch
        // spikes the frame time on the FOLLOWER, so this driver caught up to 8 ticks in ONE rendered frame —
        // the ball visibly teleported (StepBurst bursts in build logs = the "jerking"). Cap the follower's
        // catch-up to 4 ticks/frame: same deterministic step SEQUENCE (final flight identical), the backlog
        // just drains over the next frames (~3 extra ticks/frame at 60fps) instead of one giant hop. This
        // works WITH the RPC_SyncBallShot dead-band (Batting.cs): the 20Hz stream no longer snaps/re-bases
        // per packet, so the drain actually runs instead of being teleported through every 50ms. Outcomes are
        // relayed from the batting authority, so the brief visual lag is invisible. The batting AUTHORITY
        // keeps the full 8 cap — its sim drives contact/outcome timing and must stay realtime.
        bool _followerAmortize = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex;
        int toRun = Mathf.Clamp(targetTicks - _ballSimTicksDone, 0, _followerAmortize ? 4 : 8);
        _ballStepDt = BALL_SIM_FIXED_STEP;
        for (int i = 0; i < toRun; i++)
        {
            // Physics (BallMovement + threshold-gated bounce/boundary) runs every catch-up sub-step so a
            // bounce occurring mid-frame is never skipped; the per-frame COSMETIC tail (six-distance UI /
            // six cam) is suppressed until the LAST sub-step so a low-FPS (toRun>1) frame doesn't re-run it.
            _ballSubstepIsFinal = (i == toRun - 1);
            BattingBallMovement();
        }
        _ballSubstepIsFinal = true;   // restore for any non-catch-up (legacy / single) BattingBallMovement call
        _ballSimTicksDone += toRun;
    }

    private void StepDeliveryDeterministic()
    {
        int targetTicks = BallSimTargetTicks();   // Issue #3: monotonic shared-clock target (no backward-resync freeze)
        int toRun = Mathf.Clamp(targetTicks - _ballSimTicksDone, 0, 8);
        _ballStepDt = BALL_SIM_FIXED_STEP;
        float _burstPreZ = temporaryPosition.z;   // [StepBurst] detect ball jumping the keeper z-band in 1 frame
        // Issue #4 (2nd-innings batter can't hit): the shot-activation gate FindBatsmanCanMakeShot() samples
        // temporaryPosition.z and only arms isShotAllowed while the ball is inside shotActivationMin/MaxBoundary.
        // When a low-FPS frame runs a multi-tick burst (toRun>1, ~0.55–1.1z/tick), a SINGLE post-loop sample in
        // Action3Functions can carry z from before the band to past it without ever sampling inside it, so
        // isShotAllowed never latches, the swing never arms (gate at line ~11304/11311), and the ball runs to
        // the keeper. Sample the gate per fixed sub-step (mirroring CustomRayCastForBowlingBallMovement, which
        // already runs per BowlingBallMovement step) so a burst can't skip the in-band frame. FindBatsmanCanMakeShot
        // is idempotent (only ever sets isShotAllowed=true, guarded by !batsmanTriggeredAttemptedShot; never reset
        // false mid-flight), so once it latches inside the band it stays latched — no determinism impact, no write
        // to temporaryPosition/matchBallTransform. MP-only (this loop only runs when _useDeterministicBallStep,
        // i.e. useDeterministicDelivery + useLocalDeliverySim in live MP; extra PlayMode8 && !isWithAI for safety)
        // so offline/legacy single-step framestepping keeps its existing single post-loop sample unchanged.
        bool _sampleShotGatePerStep = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false;
        for (int i = 0; i < toRun; i++)
        {
            _ballSubstepIsFinal = (i == toRun - 1);
            BowlingBallMovement();
            if (_sampleShotGatePerStep)
            {
                FindBatsmanCanMakeShot();   // arm isShotAllowed on the exact sub-step the ball is in-band
                if (ConstantsData_M.useDeterministicContact)
                    LockstepSubstep();   // lockstep: crossing stash + the deterministic bat contact
            }
        }
        _ballSubstepIsFinal = true;
        _ballSimTicksDone += toRun;
        if (toRun > 1 && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            ConstantsData_M.MpLog($"[StepBurst] toRun={toRun} preZ={_burstPreZ:F2} postZ={temporaryPosition.z:F2} keeperZ={_wicketKeeperTransform.position.z:F2} (ball advanced {(temporaryPosition.z - _burstPreZ):F2} in one frame — catch window may be skipped)");
    }
}
