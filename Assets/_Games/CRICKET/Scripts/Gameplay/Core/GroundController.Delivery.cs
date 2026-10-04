// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.Delivery — the DELIVERY + SHOT-RESOLUTION partial.
// Flow: bowl commit (StartBowling: speed/spin/swing/overstep lock) → run-up → ReleaseTheBall
// (params + per-delivery RNG seed minted + shipped) → deterministic fixed-step flight
// (StepDeliveryDeterministic, 1/60 NetworkTime ticks; bowling side runs lockstepFollowerFlightDelay
// behind) → DETERMINISTIC LOCKSTEP contact at LOCKSTEP_CONTACT_Z (crossing stash → swing-input
// packet → ResolveLockstepBatContact → shot tables in BallTiming/MultiPlayerBallAngle → 8-ball-style
// value relay: the batter's exact resolved numbers override the follower's local compute) → outcome.
// See Docs/DETERMINISTIC_LOCKSTEP.md. Seeded RNG: DeterministicRng (draw ORDER must match on both
// clients — never add a DetRange call that runs on only one side).
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
    public void ShowBowler(bool showStatus)
    {
        BowlerSkinRenderer.enabled = showStatus;
    }

    private void SetBowlerSide()
    {
        // NOTE: the earlier blanket "force bowlerSide=left on every online ball" hard-fix was REMOVED — it made
        // the bowling player unable to change ends via the UI arrow (the arrow set "right", then this reset it to
        // "left" the next frame; tester: "side change hi nahi hoti arrow pe click karne pe bhi, usko enable rakho").
        // The side is already kept in sync WITHOUT clobbering it here: game-start is deterministically LEFT +
        // synced by NewInnings (bowlerSide="left" + CmdSyncBowlerSide), reconnect restores from the
        // syncedBowlerSide SyncVar, and an arrow toggle is applied on BOTH clients with the same newSide by
        // CmdActivateBowlerSideChangeViaUI -> RPC_ActivateBowlerSideChangeViaUI (and syncedBowlerSide is updated
        // for reconnect). SetBowlerSide now just positions the bowler for the current (already-synced) bowlerSide.
        CONTROLLER.BowlerSide = bowlerSide;
        // First-ball bowler-side desync DIAGNOSTIC: prints the FINAL applied side + hand + type + bowler
        // index + roles on each client whenever the bowler side is positioned. Compare the two players'
        // lines for the same ball: if `side` differs → bowlerSide sync race; if `hand`/`type` differ for
        // the same `currentBowler` → team-roster desync (index 4 maps to a different-handed player);
        // if everything matches but it still looks mirrored → camera/perspective inversion.
        ConstantsData_M.MpLog($"[BowlerSideDiag] side={bowlerSide} hand={bowlerHand} type={bowlerType} currentBowler={CONTROLLER.CurrentBowlerIndex} myTeam={CONTROLLER.myTeamIndex} bat={CONTROLLER.BattingTeamIndex} bowl={CONTROLLER.BowlingTeamIndex}"
            // Batsman half, for the recurring "dono sides batsman ki sides different" report. The striker
            // INDEX already agreed on both clients in the 06-08 pair, so the divergence is not the index —
            // it has to be the END/stance the batsman is actually placed at. batHand drives the stance and
            // batsmanX is where the rig ended up, so one line per client now settles which of the two it is.
            + $" striker={CONTROLLER.StrikerIndex} nonStriker={CONTROLLER.NonStrikerIndex} batHand={batsmanHand}"
            + $" batsmanX={(batsmanObject != null ? batsmanObject.transform.position.x.ToString("F2") : "<null>")}");
        if (bowlerSide == "left")
        {
            _runnerTransform.position = new Vector3(runnerInitialPosition.x, _runnerTransform.position.y, _runnerTransform.position.z);
            _runnerTransform.localScale = new Vector3(0f - Mathf.Abs(_runnerTransform.localScale.x), _runnerTransform.localScale.y, _runnerTransform.localScale.z);
            Singleton<PreviewScreen>.instance.UpdateBowlerSideChangeUI("right");
            if (bowlerHand == "right")
            {
                ballStartPositionGO.transform.position = new Vector3(-0.7f, ballStartPositionGO.transform.position.y, ballStartPositionGO.transform.position.z);
                ////ConstantsData_M.MpLog("* position change");
                matchBallTransform.position = new Vector3(ballStartPositionGO.transform.position.x, matchBallTransform.position.y, matchBallTransform.position.z);
                temporaryPosition = matchBallTransform.position;
                if (bowlerType == "fast")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(-0.6f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-1.24f, 0f, -3.38f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(-0.78f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-1.23f, 0f, -6.75f);
                    }
                }
                else if (bowlerType == "spin")
                {
                    _mainUmpireTransform.position = mainUmpireInitialPosition;
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(-1.35f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-0.85f, 0f, -7.87f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(-1.7f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-0.96f, 0f, -8.5f);
                    }
                }
                else if (bowlerType == "medium")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(-0.95f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-0.77f, 0f, -6.11f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(-0.9f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-0.9f, 0f, -7f);
                    }
                }
            }
            else
            {
                if (!(bowlerHand == "left"))
                {
                    return;
                }
                ballStartPositionGO.transform.position = new Vector3(-0.89f, ballStartPositionGO.transform.position.y, ballStartPositionGO.transform.position.z);
                ////ConstantsData_M.MpLog("* position change");
                matchBallTransform.position = new Vector3(ballStartPositionGO.transform.position.x, matchBallTransform.position.y, matchBallTransform.position.z);
                temporaryPosition = matchBallTransform.position;
                if (bowlerType == "fast")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(-0.93f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-0.31f, 0f, -3.4f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(-0.85f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-0.4f, 0f, -6.75f);
                    }
                }
                else if (bowlerType == "spin")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        //bowler.transform.position = new Vector3(-0.2f, bowler.transform.position.y, bowler.transform.position.z);
                        bowlerObject.transform.position = new Vector3(-0.85f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);

                        _mainUmpireTransform.position = new Vector3(0.037f, 0f, -19.5f);
                        fielder10SkinTransform.position = new Vector3(-0.71f, 0f, -7.85f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(-0.68f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-1.5f, 0f, -8.5f);
                    }
                }
                else if (bowlerType == "medium")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(-0.68f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-0.91f, 0f, -6.11f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(-0.95f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(-0.9f, 0f, -7f);
                    }
                }
            }
        }
        else
        {
            if (!(bowlerSide == "right"))
            {
                return;
            }
            _runnerTransform.position = new Vector3(runnerInitialPosition.x * -1f, _runnerTransform.position.y, _runnerTransform.position.z);
            _runnerTransform.localScale = new Vector3(Mathf.Abs(_runnerTransform.localScale.x), _runnerTransform.localScale.y, _runnerTransform.localScale.z);
            Singleton<PreviewScreen>.instance.UpdateBowlerSideChangeUI("left");
            if (bowlerHand == "right")
            {
                ballStartPositionGO.transform.position = new Vector3(0.91f, ballStartPositionGO.transform.position.y, ballStartPositionGO.transform.position.z);
                ////ConstantsData_M.MpLog("* position change");
                matchBallTransform.position = new Vector3(ballStartPositionGO.transform.position.x, matchBallTransform.position.y, matchBallTransform.position.z);
                temporaryPosition = matchBallTransform.position;

                if (bowlerType == "fast")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(0.93f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(0.31f, 0f, -3.39f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(0.93f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(0.5f, 0f, -6.75f);
                    }
                }
                else if (bowlerType == "spin")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        //bowler.transform.position = new Vector3(0.23f, bowler.transform.position.y, bowler.transform.position.z);
                        bowlerObject.transform.position = new Vector3(0.7f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        _mainUmpireTransform.position = new Vector3(0.037f, 0f, -19.5f);
                        fielder10SkinTransform.position = new Vector3(0.725f, 0f, -7.86f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(0.75f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(1.5f, 0f, -8.5f);
                    }
                }
                else if (bowlerType == "medium")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(0.77f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(0.96f, 0f, -6.11f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(1f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(1f, 0f, -7f);
                    }
                }
            }
            else
            {
                if (!(bowlerHand == "left"))
                {
                    return;
                }
                ballStartPositionGO.transform.position = new Vector3(0.7f, ballStartPositionGO.transform.position.y, ballStartPositionGO.transform.position.z);
                ////ConstantsData_M.MpLog("* position change");
                matchBallTransform.position = new Vector3(ballStartPositionGO.transform.position.x, matchBallTransform.position.y, matchBallTransform.position.z);
                temporaryPosition = matchBallTransform.position;
                if (bowlerType == "fast")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(0.7f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(1.32f, 0f, -3.55f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(0.7f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(1.15f, 0f, -6.75f);
                    }
                }
                else if (bowlerType == "spin")
                {
                    _mainUmpireTransform.position = mainUmpireInitialPosition;
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(1.4f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(0.9f, 0f, -7.85f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        bowlerObject.transform.position = new Vector3(1.8f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(0.9f, 0f, -8.5f);
                    }
                }
                else if (bowlerType == "medium")
                {
                    if (bowlerAnimationNumber == 1)
                    {
                        bowlerObject.transform.position = new Vector3(0.91f, bowlerObject.transform.position.y, bowlerObject.transform.position.z);
                        fielder10SkinTransform.position = new Vector3(0.71f, 0f, -6.11f);
                    }
                    else if (bowlerAnimationNumber == 2)
                    {
                        //bowler.transform.position = new Vector3(1.3f, bowler.transform.position.y, -14.4f);
                        bowlerObject.transform.position = new Vector3(1.3f, bowlerObject.transform.position.y, -28.4f);

                        fielder10SkinTransform.position = new Vector3(0.75f, 0f, -7f);
                    }
                }
            }
        }
    }

    public void resetNoBallVairables()
    {
        CONTROLLER.isFreeHitBall = false;
        isLineFreeHitActive = false;
        lastBowledBallType = "lineball";
    }

    public void computerCanNowBowlNoBall()
    {
        if (!isReplayModeActive)
        {
            isOversteppedDelivery = true;
            noBallBowlerHeelPosition = UnityEngine.Random.Range(0.25f, 0.29f);
        }
    }

    public void StartBowling()
    {
        int controlFactor = ((CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex) ? 1 : 0);
        if (CONTROLLER.PlayModeSelected == 8)
        {
            controlFactor = 1;
        }
        if (!CONTROLLER.ReplayShowing)
        {
            canShowCountdown = true;
        }
        float spinMagnitude = Mathf.Abs(CONTROLLER.BowlingAngle) * 0.1f;
        float spinScale;
        if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
        {
            spinScale = 4f * (1f + powerMultiplier);
            if (bowlerType == "fast" || bowlerType == "medium")
            {
                ballBowlingSpeed = (float)CONTROLLER.BowlingSpeed * (1f + powerMultiplier);
            }
            else if (bowlerType == "spin")
            {
                ballBowlingSpeed = CONTROLLER.BowlingSpeed;
            }
        }
        else
        {
            spinScale = 4f;
            ballBowlingSpeed = CONTROLLER.BowlingSpeed;
        }
        if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex || CONTROLLER.PlayModeSelected == 8)
        {
            float arrowPos = Singleton<BowlingControls>.instance.getArrowPos();
            // This meter reading is the ONLY thing that makes a delivery a no-ball, and nothing logged it.
            // ResetSpeedOMeter() (fill = 0.4) + AnimateSpeedOMeter() run per delivery from
            // GameData.ShowBowlingInterface -> ActivateBowlingControls. If that per-delivery setup is skipped —
            // which is exactly what happens on the first ball after a reconnect (see the plan-arm's
            // "per-delivery setup was skipped" re-assert) — the fill stays on the value RPC_LockSpeed last
            // parked it at. Lock a no-ball, disconnect, come back, and the bar is still sitting above 0.7 with
            // no oscillation, so re-setting it reproduces the no-ball every time (tester 12-08: "no ball adjust
            // kar ke disconnection, reconnection pe again ball bar set ki but no ball hi hui"). arrowPos ~0.4
            // here means the meter was re-armed properly and the cause is elsewhere; a stale >0.7 confirms it.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                ConstantsData_M.MpLog($"[NoBallDiag] Release decision: arrowPos={arrowPos:F3} -> noBall={(arrowPos > 0.7f && !isReplayModeActive)} (wasOverstepped={isOversteppedDelivery} freeHit={isFreeHitActive} lastBallType='{lastBowledBallType}').");
            if (arrowPos > 0.7f && !isReplayModeActive)
            {
                isOversteppedDelivery = true;
                noBall = true;
                noBallBowlerHeelPosition = UnityEngine.Random.Range(0.25f, 0.29f);
            }
        }
        AiFieldScan();
        getSlipFielders();
        if (noBall && !isReplayModeActive)
        {
            isFreeHitActive = true;
            freeHit = true;
            noBall = false;
        }
        if (ballBowlingSpeed < 10f * (1f + powerMultiplier * (float)controlFactor))
        {
            noBall = false;
        }
        spinFactor = spinScale * spinMagnitude;
        if (bowlerSpinType == 1)
        {
            spinFactor *= -1f;
        }
        if (bowlerType == "fast")
        {
            swingIntensity = CONTROLLER.BowlingSwing - 2f;
            swingIntensity = swingIntensity * 0.75f * (1f + controlMultiplier * (float)controlFactor);
            if (swingIntensity != 0f)
            {
                isBallSwinging = true;
                spinFactor = (0f - swingIntensity) * 2f;
            }
        }
        else if (bowlerType == "medium")
        {
            swingIntensity = CONTROLLER.BowlingSwing - 2f;
            swingIntensity = swingIntensity * 0.75f * (1f + controlMultiplier * (float)controlFactor);
            if (swingIntensity != 0f)
            {
                isBallSwinging = true;
                spinFactor = (0f - swingIntensity) * 2f;
            }
        }
        //if (CONTROLLER.PlayModeSelected == 6)
        //{
        //	swingValue = 0f;
        //	spinValue = 0f;
        //}
        ////ConstantsData_M.MpLog("* position change");
        matchBallTransform.position = initialBallPosition;
        temporaryPosition = matchBallTransform.position;
        SetBowlerSide();
        matchBallTransform.eulerAngles = new Vector3(0f, 2f, 180f);
        if (CONTROLLER.cameraType == 0)
        {
            ActivateStadiumAndSkybox(boolean: false);
        }
        ballStartTime = Time.time;
        currentActionState = 0;
    }

    /// <summary>
    /// Adopt the batting authority's free-hit verdict for the delivery that just resolved.
    ///
    /// isLineFreeHitActive has ~45 write sites across the delivery, keeper and fielding partials and is
    /// derived independently on each client — nothing ever relayed it. The 08-27 18:23 pair caught the
    /// consequence exactly: from the SAME delivery (arrowPos 0.450, freeHit=True on both) one client came
    /// out with lastBallType='lineball' and the other with 'overstep', and stayed split for every ball
    /// after. On that one delivery the batting client sent its bowler to the batsman's end to collect while
    /// the bowling client left him standing — the tester's #1, open since 08-17. Every later delivery in
    /// that pair agreed perfectly (collect angles 280.0, 28.8, 192.4 on both), so the divergence is this
    /// state and not the collect geometry.
    ///
    /// Same shape as the throw-target and bowler-identity relays: the side that owns the outcome rules it,
    /// the other side stops deriving. Applied at outcome time, which is when this state decides whether the
    /// NEXT ball is a free hit.
    /// </summary>
    public void ApplyAuthoritativeFreeHitState(bool lineFreeHitActive, string lastBallType)
    {
        bool _changed = isLineFreeHitActive != lineFreeHitActive
                        || (!string.IsNullOrEmpty(lastBallType) && lastBowledBallType != lastBallType);
        if (_changed && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            ConstantsData_M.MpLog($"[FreeHitSync] Adopting the batting side's verdict: lineFreeHit {isLineFreeHitActive}->{lineFreeHitActive}, lastBallType '{lastBowledBallType}'->'{lastBallType}'.");
        isLineFreeHitActive = lineFreeHitActive;
        if (!string.IsNullOrEmpty(lastBallType)) lastBowledBallType = lastBallType;
    }

    public void ActivateBowler()
    {
        ////ConstantsData_M.MpLog("CASEE 3 : ActivateBowler");
        if (currentActionState == 0)
        {
            return;
        }
        // WHY THE BOWLER DID NOT GO TO COLLECT (#1: "batsman side bowler batsman ke paas gaya ball pakadne
        // jabki bowler side bowler apni jagah pe hi tha", open since 08-17).
        //
        // The collect gate below is four LOCAL conditions and it only logs when it PASSES, so the side where
        // the bowler stayed put printed nothing at all — the 08-27 18:46 pair fired the collect three times
        // on one device and once on the other, which proves the gate disagrees but not WHICH input did. Each
        // input implies a different fix: currentBallStatus disagreeing means the two sides classified the
        // contact differently (bat vs body — exactly the free-hit case in the report) and the STATUS is what
        // has to be relayed; fielderAction or isBowlerActivationAllowed disagreeing means the fielder state
        // machine drifted and the COLLECT DECISION is what has to be relayed. Print the inputs once per
        // delivery, from the side that did not collect, a beat after release so the contact has resolved.
        if (isBallReleased && !_collectFiredThisDelivery && !_collectGateReported
            && ballReleaseTime > 0f && Time.time - ballReleaseTime > 1.5f
            && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
        {
            _collectGateReported = true;
            ConstantsData_M.MpLog($"[BowlerCollectDiag] NO collect this delivery — gate inputs:"
                + $" bowlerActivation={isBowlerActivationAllowed} status='{currentBallStatus}' shot='{currentShotPlayed}'"
                + $" fielderAction='{fielderAction}' freeHit={isFreeHitActive} lineFreeHit={isLineFreeHitActive}"
                + $" ballHit={isBallHit} outcome='{outcomeOfBall}' amBatting={(CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)}.");
        }
        //if ((BowlerAnimationComponent.IsPlaying("Blitz_" + currentBowlerType + "PaceBowling0" + BowlerAnimNumber) || (CONTROLLER.ReplayShowing && activateBowlerForReplay)) && !canActivateBowler)
        if ((IsPlaying("Blitz_" + bowlerType + "PaceBowling0" + bowlerAnimationNumber) || (CONTROLLER.ReplayShowing && isBowlerActivatedForReplay)) && !isBowlerActivationAllowed)
        {
            if (CONTROLLER.ReplayShowing)
            {
                isBowlerActivatedForReplay = true;
            }
            //if (BowlerAnimationComponent["Blitz_" + currentBowlerType + "PaceBowling0" + BowlerAnimNumber].time > BowlerAnimationComponent["Blitz_" + currentBowlerType + "PaceBowling0" + BowlerAnimNumber].length - 0.16f || BowlerAnimationComponent["Blitz_" + currentBowlerType + "PaceBowling0" + BowlerAnimNumber].time == 0f || (CONTROLLER.ReplayShowing && ballStatus == "shotSuccess"))
            if (bowlerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Blitz_" + bowlerType + "PaceBowling0" + bowlerAnimationNumber))
            {
                // Calculate normalized time considering potential looping
                float normalizedTime = Mathf.Repeat(bowlerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1.0f);

                // Check if animation is near the end (considering a 0.16f buffer)
                if (normalizedTime >= 0.84f || normalizedTime == 0f ||
                    (CONTROLLER.ReplayShowing && currentBallStatus == "shotSuccess"))
                {
                    ////ConstantsData_M.MpLog("ShowFielder10");
                    // Your logic here (e.g., play a new animation, reset state)
                    isBowlerActivatedForReplay = false;
                    ShowFielder10(fielder10Status: true, ball10Status: false);
                    ShowBowler(showStatus: false);
                }
            }
            {

            }
        }
        if (!isAutoBowlerActivationAllowed && currentBallStatus == "onPads" && matchBallTransform.position.z < 6f)
        {
            ////ConstantsData_M.MpLog("ShowFielder10");
            ShowFielder10(fielder10Status: true, ball10Status: false);
            ShowBowler(showStatus: false);
            isAutoBowlerActivationAllowed = true;
        }
        if (isBowlerActivationAllowed && (((currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense") && currentBallStatus == "shotSuccess") || (currentBallStatus == "onPads" && !hasLBWAppeal)))
        {
            if (fielderAction == "idle")
            {
                fielderAction = "run";
                //Fielder10AnimationComponent["run"].speed = 0.15f;
                fielder10Anim.CrossFade("run");
            }
            if (fielderAction == "run")
            {
                float num = AngleBetweenTwoGameObjects(fielder10Object, matchBall);
                fielder10SkinTransform.position += new Vector3(Mathf.Cos(num * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num * degToRad) * defaultFielderSpeed * Time.deltaTime);
                //fielder10Transform.LookAt(new Vector3(ballTransform.position.x, 0f, ballTransform.position.z));
                fielder10SkinTransform.LookAt(new Vector3(temporaryPosition.x, 0f, temporaryPosition.z));
                int num2 = 1;
                if (DistanceBetweenTwoVector2(fielder10Object, matchBall) < (float)num2)
                {
                    fielderAction = "pickupAttempt";
                    fielder10Anim.CrossFade("lowCatch");
                    fielder10Anim["lowCatch"].speed = 5f;
                }
            }
            else if (fielderAction == "pickupAttempt")
            {
                float num3 = 16f;
                float num4 = num3 * animationFrameInterval;
                if (num4 < fielder10Anim["lowCatch"].time || fielder10Anim["lowCatch"].time == 0f)
                {
                    fielderAction = "pickedup";
                    Fielder10BallSkinRenderer.enabled = true;
                    fielder10Anim["lowCatch"].speed = 1f;
                    ShowBall(status: false);
                    isBallPaused = true;
                    shouldStopFielders = true;
                    defaultFielderSpeed = 0f;
                }
            }
            else if (fielderAction == "pickedup")
            {
                if (fielder10Anim["lowCatch"].time == 0f)
                {
                    float num5 = AngleBetweenTwoGameObjects(fielder10Object, stumpLeft) + 90f;
                    iTween.RotateTo(fielder10Object, iTween.Hash("y", num5, "time", 0.2));
                    fielder10Anim.Play("idle");
                    _stayStartTime = Time.time;
                    fielderAction = "end";
                }
            }
            else if (fielderAction == "end")
            {
                if (_stayStartTime - 0.5f + delayBetweenDeliveries < Time.time)
                {
                    if (isOversteppedDelivery)
                    {
                        if (isReplayModeActive)
                        {
                            fielderAction = string.Empty;
                            HideReplay();
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
                            {
                            }
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            return;
                        }
                        showMainUmpireForNoBallAction();
                        fielderAction = "umpireNoBallAction";
                        bool flag = checkForMatchComplete(1, 0);
                        if (CONTROLLER.matchType == "oneday" && !flag)
                        {
                            noBallActionDelay = 4f;
                            mainUmpireAnim.Play("NoBallFreeHit_New");
                            isLineFreeHitActive = true;
                            lastBowledBallType = "overstep";
                        }
                        else
                        {
                            noBallActionDelay = 1.5f;
                            mainUmpireAnim.Play("NoBallFreeHit_New");
                        }
                    }
                    else if (isLineFreeHitActive)
                    {
                        fielderAction = string.Empty;
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        isLineFreeHitActive = false;
                        lastBowledBallType = "lineball";
                        freeHit = false;
                        Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                    }
                    else
                    {
                        fielderAction = string.Empty;
                        if (Singleton<GameData>.instance != null)
                        {
                            if (noBall)
                            {
                                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                                Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                                CONTROLLER.isJokerCall = false;
                                if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                                {
                                }
                            }
                            else if (freeHit)
                            {
                                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                                freeHit = false;
                                isFreeHitActive = false;
                            }
                            else if (!noBall && !freeHit)
                            {
                                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            }
                        }
                    }
                }
            }
            else if (fielderAction == "umpireNoBallAction" && _stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time)
            {
                fielderAction = string.Empty;
                if (Singleton<GameData>.instance != null)
                {
                    if (isOversteppedDelivery)
                    {
                        if (!isReplayModeActive)
                        {
                            noBallRunStatus = "bowlercollectsdotball";
                            if (CONTROLLER.canShowReplay)
                            {
                                Singleton<GameData>.instance.GameIsOnReplay();
                                ShowReplay();
                            }
                            else
                            {
                                Singleton<GameData>.instance.ReplayIsNotShown();
                            }
                        }
                        else
                        {
                            HideReplay();
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                            {
                            }
                        }
                    }
                    else if (isLineFreeHitActive)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        isLineFreeHitActive = false;
                        lastBowledBallType = "lineball";
                    }
                }
            }
        }
        else if (isBowlerActivationAllowed && currentBallStatus == "shotSuccess" && currentShotPlayed != "bt6Defense" && currentShotPlayed != "backFootDefenseHighBall" && currentShotPlayed != "frontFootOffSideDefense" && fielderAction == "idle")
        {
            fielder10Anim.Play("run");
            fielder10SkinTransform.position += new Vector3(0f, 0f, 1f);
            float num6 = AngleBetweenTwoGameObjects(stumpRightSpot, fielderFocusObjectToCollectBall);
            // Where the BOWLER (Fielder10) runs to collect is decided here, purely from local geometry: this
            // angle picks one of four positions, and the 225-315 band sends him to stumpRightCrease — the
            // BATSMAN's end. Nothing about this decision is relayed, and the whole block is gated on local
            // state too, so one client can run the bowler in while the other never enters the block at all and
            // leaves him standing (tester, reported 3x: "ik side bowler batsman ke paas chala ball pakadne, or
            // opponent side bowler apni jagah pe hi raha"). Log the gate inputs and the angle: identical values
            // on both sides would mean the divergence is downstream, while a line present on ONE side only
            // means the gate itself disagreed — and those are different fixes.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                ConstantsData_M.MpLog($"[BowlerCollectDiag] Bowler sent to collect: angle={num6:F1} -> {(num6 >= 45f && num6 <= 135f ? "straight" : (num6 >= 225f && num6 <= 315f ? "straightDown(BATSMAN END)" : (num6 >= 135f && num6 <= 225f ? "offSide" : "legSide")))}"
                    + $" | from={(fielder10Object != null ? fielder10Object.transform.position.ToString("F2") : "<null>")} status='{currentBallStatus}' shot='{currentShotPlayed}' bowlerActivation={isBowlerActivationAllowed} amBatting={(CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)}.");
            float num7 = 0.7f;
            if (bowlerType == "spin")
            {
                num7 = 0.4f;
            }
            if (num6 >= 45f && num6 <= 135f)
            {
                postBattingStumpingFielderDirection = "straight";
                iTween.MoveTo(fielder10Object, iTween.Hash("position", fielderStraightStumpingPosition.transform.position, "time", num7, "easetype", "linear", "oncomplete", "EnableFielder10ToCollectBall", "oncompletetarget", base.gameObject));
                fielder10SkinTransform.LookAt(fielderStraightStumpingPosition.transform);
            }
            else if (num6 >= 225f && num6 <= 315f)
            {
                postBattingStumpingFielderDirection = "straightDown";
                Vector3 position = stumpRightCrease.transform.position;
                position = new Vector3(position.x, position.y, -9f);
                iTween.MoveTo(fielder10Object, iTween.Hash("position", position, "time", num7, "easetype", "linear", "oncomplete", "EnableFielder10ToCollectBall", "oncompletetarget", base.gameObject));
                fielder10SkinTransform.LookAt(stumpRightCrease.transform);
            }
            else if (num6 >= 135f && num6 <= 225f)
            {
                postBattingStumpingFielderDirection = "offSide";
                iTween.MoveTo(fielder10Object, iTween.Hash("position", fielderOffSideStumpingPosition.transform.position, "time", num7, "easetype", "linear", "oncomplete", "EnableFielder10ToCollectBall", "oncompletetarget", base.gameObject));
                fielder10SkinTransform.LookAt(fielderOffSideStumpingPosition.transform);
            }
            else
            {
                postBattingStumpingFielderDirection = "legSide";
                iTween.MoveTo(fielder10Object, iTween.Hash("position", fielderLegSideStumpingPosition.transform.position, "time", num7, "easetype", "linear", "oncomplete", "EnableFielder10ToCollectBall", "oncompletetarget", base.gameObject));
                fielder10SkinTransform.LookAt(fielderLegSideStumpingPosition.transform);
            }
            float y = fielder10SkinTransform.eulerAngles.y;
            fielder10SkinTransform.eulerAngles = new Vector3(fielder10SkinTransform.eulerAngles.x, 0f, fielder10SkinTransform.eulerAngles.z);
            iTween.RotateTo(fielder10Object, iTween.Hash("y", y, "time", 0.2));
            fielderAction = "reachedNonStickerStump";
            _collectFiredThisDelivery = true;
        }
        else if (fielderAction == "waitForBall")
        {
            if (isBallOnBoundaryLine)
            {
                fielder10Anim.CrossFade("idle");
                fielderAction = "finish";
            }
            if (outcomeOfBall == "wicket" && !isLineFreeHitActive)
            {
                fielder10Anim.Play("appeal");
                fielderAction = "finish";
            }
        }
        else if (fielderAction == "waitToCollect" && fielderHasThrown)
        {
            float num8 = 7f;
            float num9 = num8 * animationFrameInterval * horizontalVelocity;

            float num = temporaryPosition.x - fielder10Object.transform.position.x;
            float num2 = temporaryPosition.z - fielder10Object.transform.position.z;
            float num3 = Mathf.Sqrt(num * num + num2 * num2);

            //if (DistanceBetweenTwoVector2(ball, fielder10) < num9)
            if (num3 < num9)
            {
                canRun = false;
                distanceBetweenBallAndCollectingPlayerWhileThrowing = 10000f;
                if (Singleton<GameData>.instance != null)
                {
                    disableRunCancelBtn();
                }
                if ((!isReplayModeActive && isRunOutOccurring) || (isReplayModeActive && throwActionSaved == "collectTheThrowAndStump"))
                {
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Cheer");
                        Singleton<GameData>.instance.PlayGameSound("Bowled");
                    }
                    fielder10Anim.Play("collectAndStump");
                    fielder10Anim.PlayQueued("appeal");
                    fielderAction = "collectTheThrowAndStump";
                    throwActionSaved = "collectTheThrowAndStump";
                }
                else if ((!isReplayModeActive && !isRunOutOccurring) || (isReplayModeActive && throwActionSaved == "collectTheThrow"))
                {
                    fielder10Anim.Play("collectAndStand");
                    fielderAction = "collectTheThrow";
                    throwActionSaved = "collectTheThrow";
                }
            }
        }
        else if (fielderAction == "collectTheThrow" || fielderAction == "collectTheThrowAndStump")
        {
            float num10 = DistanceBetweenTwoVector2(matchBall, fielder10Object);
            if (distanceBetweenBallAndCollectingPlayerWhileThrowing > num10)
            {
                distanceBetweenBallAndCollectingPlayerWhileThrowing = num10;
            }
            else
            {
                distanceBetweenBallAndCollectingPlayerWhileThrowing = -1f;
            }
            if (!isReplayModeActive)
            {
                isRunOutOccurring = isBatsmanRunOut();
            }
            if (num10 < 0.5f || distanceBetweenBallAndCollectingPlayerWhileThrowing == -1f)
            {
                currentBallStatus = string.Empty;
                isBallPaused = true;
                Fielder10BallSkinRenderer.enabled = true;
                ShowBall(status: false);
                if (fielderAction == "collectTheThrow")
                {
                    _stayStartTime = Time.time;
                    fielderAction = "end";
                }
                else if (fielderAction == "collectTheThrowAndStump")
                {
                    float num11 = 9f;
                    fielder10SkinTransform.LookAt(stumpRightSpot.transform);
                    fielder10SkinTransform.eulerAngles -= new Vector3(0f, 5f, 0f);
                    if (fielder10Anim["collectAndStump"].time > num11 * animationFrameInterval)
                    {
                        if (!isReplayModeActive)
                        {
                            hasRunOutOccurred = isRunOutOccurring;
                            if (!isLineFreeHitActive && !isOversteppedDelivery)
                            {
                                isTightRunoutCall = IsTightRunoutCall();
                            }
                            isRunOutSaved = hasRunOutOccurred;
                            currentBallRunsSaved = runsScoredThisBall;
                        }
                        else
                        {
                            hasRunOutOccurred = isRunOutSaved;
                            runsScoredThisBall = currentBallRunsSaved;
                        }
                        if (freeHit)
                        {
                            freeHit = false;
                        }
                        _stayStartTime = Time.time;
                        fielderAction = "runOutAppeal";
                        isRunOutAppealSaved = true;
                        if (isReplayModeActive)
                        {
                            StartCoroutine(UltraSlowMotion());
                        }
                        float num12 = 270f;
                        if (umpireRunSignalDirection == -1)
                        {
                            num12 = 90f;
                        }
                        iTween.RotateTo(fielder10Object, iTween.Hash("y", num12, "time", 0.5, "delay", 0.2));
                        if (postBattingStumpingFielderDirection == "straight")
                        {
                            stump2Anim.Play("legSideStumping");
                        }
                        else if (postBattingStumpingFielderDirection == "straightDown")
                        {
                            stump2Anim.Play("offSideStumping");
                        }
                        else if (postBattingStumpingFielderDirection == "offSide")
                        {
                            stump2Anim.Play("fielderRunoutAway");
                        }
                        else if (postBattingStumpingFielderDirection == "legSide")
                        {
                            stump2Anim.Play("fielderRunoutIn");
                        }
                    }
                }
            }
        }
        else if (fielderAction == "end")
        {
            if (_stayStartTime + 1f + delayBetweenDeliveries < Time.time)
            {
                if (isOversteppedDelivery)
                {
                    if (isReplayModeActive)
                    {
                        fielderAction = string.Empty;
                        HideReplay();
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                        return;
                    }
                    showMainUmpireForNoBallAction();
                    lineNoBallRunsScored = runsScoredThisBall;
                    fielderAction = "umpireNoBallActionAfterBowlerCollectsBallFromFielder";
                    bool flag2 = checkForMatchComplete(runsScoredThisBall + 1, 0);
                    if (CONTROLLER.matchType == "oneday" && !flag2)
                    {
                        noBallActionDelay = 4f;
                        mainUmpireAnim.Play("NoBallFreeHit_New");
                        isLineFreeHitActive = true;
                        lastBowledBallType = "overstep";
                    }
                    else
                    {
                        noBallActionDelay = 1.08f;
                        mainUmpireAnim.Play("NoBallFreeHit_New");
                    }
                }
                else if (isLineFreeHitActive)
                {
                    fielderAction = string.Empty;
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    isLineFreeHitActive = false;
                    lastBowledBallType = "lineball";
                    freeHit = false;
                    Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                }
                else
                {
                    fielderAction = string.Empty;
                    if (Singleton<GameData>.instance != null)
                    {
                        if (noBall)
                        {
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            CONTROLLER.isJokerCall = false;
                            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                            {
                            }
                        }
                        else if (freeHit)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            freeHit = false;
                            isFreeHitActive = false;
                        }
                        else if (!noBall && !freeHit)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        }
                    }
                }
            }
        }
        else if (fielderAction == "umpireNoBallActionAfterBowlerCollectsBallFromFielder")
        {
            if (_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time)
            {
                fielderAction = string.Empty;
                if (Singleton<GameData>.instance != null)
                {
                    if (isOversteppedDelivery)
                    {
                        if (!isReplayModeActive)
                        {
                            noBallRunStatus = "bowlercollectstheball";
                            if (CONTROLLER.canShowReplay)
                            {
                                Singleton<GameData>.instance.GameIsOnReplay();
                                ShowReplay();
                            }
                            else
                            {
                                Singleton<GameData>.instance.ReplayIsNotShown();
                            }
                        }
                        else
                        {
                            HideReplay();
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                            {
                            }
                        }
                    }
                    else if (isLineFreeHitActive)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        isLineFreeHitActive = false;
                        lastBowledBallType = "lineball";
                    }
                }
            }
        }
        else if (fielderAction == "runOutAppeal")
        {
            if (!isFielderAppealingRunOut)
            {
                makeFieldersToCelebrate(null);
                isFielderAppealingRunOut = true;
            }
            if (_stayStartTime + 1f + delayBetweenDeliveries < Time.time)
            {
                _stayStartTime = Time.time;
                fielderAction = "waitForResult";
                showPreviewCamera(status: false);
                gameplayCamera.enabled = false;
                rightFieldCamera.enabled = false;
                leftFieldCamera.enabled = false;
                if (isReplayModeActive)
                {
                    if (!CONTROLLER.runoutThirdUmpireAppeal)
                    {
                        fielderAction = string.Empty;
                        HideReplay();
                        if (!hasRunOutOccurred)
                        {
                            UpdateRunAfterReplay();
                        }
                        return;
                    }
                    if (CONTROLLER.runoutThirdUmpireAppeal)
                    {
                        ShowThirdUmpireRunoutDecisionPendingScreen();
                        return;
                    }
                }
                if (!isReplayModeActive)
                {
                    umpireViewCamera.enabled = true;
                    umpireCamTransform.position = stumpRightCrease.transform.position;
                    umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 1.5f, umpireCamTransform.position.z);
                    iTween.MoveTo(umpireViewCamera.gameObject, iTween.Hash("y", UnityEngine.Random.Range(1.4f, 1.8f), "time", 2));
                    // The umpire's SIGNAL is chosen from the local hasRunOutOccurred below, while the SCORE comes from
                    // the authoritative relay — so a local run-out call that disagrees with the authority makes one
                    // screen signal OUT and the other NOT OUT for the same delivery, exactly as reported ("dono sides
                    // out hi consider hua but umpire ke signs ka difference tha"). Log the input so the next log pair
                    // proves the divergence before the signal is re-keyed to the relayed outcome.
                    // Online, the BATTING side owns the run-out call (it owns the ball/outcome), so it relays its verdict
                    // and the bowling side corrects its signal if it judged the tight call the other way. Prefer a verdict
                    // that has ALREADY arrived; otherwise play the local call now and let OnRunOutVerdictRelayed fix it.
                    if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                    {
                        bool _amBattingAuthority = CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex;
                        if (_amBattingAuthority)
                        {
                            if (CricketNetworkManager.ReadyToSend && staticVariables.UserProfiledata?.user != null)
                                CricketNetworkManager.instance.CmdRunOutVerdict(staticVariables.UserProfiledata.user._id, hasRunOutOccurred);
                        }
                        else if (_relayedRunOutVerdict >= 0 && _relayedRunOutVerdict != (hasRunOutOccurred ? 1 : 0))
                        {
                            ConstantsData_M.MpLog($"[RunOutDiag] Adopting the batting authority's verdict for the umpire signal: local={hasRunOutOccurred} -> relayed={_relayedRunOutVerdict == 1}.");
                            hasRunOutOccurred = _relayedRunOutVerdict == 1;
                        }
                        ConstantsData_M.MpLog($"[RunOutDiag] Umpire signal: hasRunOutOccurred={hasRunOutOccurred} tight={isTightRunoutCall} amBatting={_amBattingAuthority} relayed={_relayedRunOutVerdict}.");
                        // The verdict is ALWAYS still in flight when the bowling side reaches this point — the 18-08 pair
                        // shows relayed=-1 on every one of its three signals, each then corrected a moment later. Playing a
                        // local guess and correcting it means the wrong decision is what the player actually sees, so on the
                        // bowling side hold the ANIMATION until the verdict lands. Game state below is untouched — only the
                        // umpire's gesture waits. A timeout falls back to the local call so a lost verdict cannot leave the
                        // umpire silent, which is the other half of what was reported.
                        if (!_amBattingAuthority && _relayedRunOutVerdict < 0)
                        {
                            _runOutSignalDeferred = true;
                            _runOutSignalPlayed = -1;
                            if (_runOutSignalTimeoutCo != null) StopCoroutine(_runOutSignalTimeoutCo);
                            if (gameObject.activeInHierarchy) _runOutSignalTimeoutCo = StartCoroutine(PlayRunOutSignalIfVerdictNeverArrives(hasRunOutOccurred));
                            ConstantsData_M.MpLog("[RunOutDiag] Holding the umpire signal until the batting side's verdict arrives.");
                        }
                        else _runOutSignalPlayed = hasRunOutOccurred ? 1 : 0;
                    }
                    if (hasRunOutOccurred)
                    {
                        batsmanOutIndexValue = runOutScenario("b");
                        if (isTightRunoutCall && !isOversteppedDelivery && CONTROLLER.isLineFreeHitBallCompleted && CONTROLLER.PlayModeSelected != 6
                            && !(CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false))   // ONLINE: run-out is DIRECT — the third-umpire board/replay was half-broken and desynced (ran wrong / didn't run on one side); the on-field decision is batting-authority-relayed and identical on both clients anyway. Same pattern as mode 6 and the online LBW review.
                        {
                            mainUmpireAnim.Play("3rd Umpire_New");
                            fielderAction = "waitForThirdUmpireSignal";
                            CONTROLLER.runoutThirdUmpireAppeal = true;
                            Singleton<GameData>.instance.canPauseGameplay = false;
                        }
                        else
                        {
                            if (!_runOutSignalDeferred) mainUmpireAnim.Play("Out2_New");
                        }
                        if (Singleton<GameData>.instance != null && !isReplayModeActive)
                        {
                            Singleton<GameData>.instance.PlayGameSound("Cheer");
                        }
                    }
                    else
                    {
                        if (isTightRunoutCall && !isOversteppedDelivery && CONTROLLER.isLineFreeHitBallCompleted && CONTROLLER.PlayModeSelected != 6
                            && !(CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false))   // ONLINE: run-out is DIRECT — the third-umpire board/replay was half-broken and desynced (ran wrong / didn't run on one side); the on-field decision is batting-authority-relayed and identical on both clients anyway. Same pattern as mode 6 and the online LBW review.
                        {
                            mainUmpireAnim.Play("3rd Umpire_New");
                            fielderAction = "waitForThirdUmpireSignal";
                            CONTROLLER.runoutThirdUmpireAppeal = true;
                            Singleton<GameData>.instance.canPauseGameplay = false;
                        }
                        else
                        {
                            if (!_runOutSignalDeferred) mainUmpireAnim.CrossFade("Crouch_toNotOut_New");
                        }
                        if (Singleton<GameData>.instance != null && !isReplayModeActive)
                        {
                            Singleton<GameData>.instance.PlayGameSound("Beaten");
                        }
                    }
                    if (umpireRunSignalDirection == 1)
                    {
                        _mainUmpireTransform.position = umpireLeftSpot.transform.position;
                        _mainUmpireTransform.eulerAngles = new Vector3(_mainUmpireTransform.eulerAngles.x, 90f, _mainUmpireTransform.eulerAngles.z);
                        umpireCamTransform.eulerAngles = new Vector3(umpireCamTransform.eulerAngles.x, 270f, umpireCamTransform.eulerAngles.z);
                        umpireCamTransform.position -= new Vector3(3f, 0f, 0f);
                    }
                    else
                    {
                        //mainUmpireTransform.localScale = new Vector3(1f, mainUmpireTransform.localScale.y, mainUmpireTransform.localScale.z);
                        _mainUmpireTransform.position = umpireRightSpot.transform.position;
                        _mainUmpireTransform.eulerAngles = new Vector3(_mainUmpireTransform.eulerAngles.x, 270f, _mainUmpireTransform.eulerAngles.z);
                        umpireCamTransform.eulerAngles = new Vector3(umpireCamTransform.eulerAngles.x, 90f, umpireCamTransform.eulerAngles.z);
                        umpireCamTransform.position += new Vector3(3f, 0f, 0f);
                    }
                }
            }
        }
        else if (fielderAction == "waitForThirdUmpireSignal")
        {
            if (_stayStartTime + 4f < Time.time)
            {
                fielderAction = string.Empty;
                CONTROLLER.stumpingAttempted = false;
                Singleton<GameData>.instance.GameIsOnThirdUmpireRunoutReplay();
                ShowReplay();
            }
        }
        else if (fielderAction == "waitFor3rdUmpireResultForRunout")
        {
            float num13 = 2f;
            if (isVeryTightRunoutCall)
            {
                num13 = 4f;
            }
            if (_stayStartTime + num13 < Time.time)
            {
                scoreboardScreen.transform.localScale = new Vector3(0f, 0f, 0f);
                iTween.ScaleTo(scoreboardScreen, iTween.Hash("scale", scoreboardScreenScale, "time", 0.4f, "easetype", "spring"));
                if (hasRunOutOccurred)
                {
                    DigitalScreenRenderer.material.mainTexture = digitalBoardTextures[3];
                    if (Singleton<GameData>.instance != null)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Cheer");
                    }
                }
                else
                {
                    DigitalScreenRenderer.material.mainTexture = digitalBoardTextures[2];
                    if (Singleton<GameData>.instance != null)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Beaten");
                    }
                }
                _stayStartTime = Time.time;
                fielderAction = "Show3rdUmpireResultForRunout";
            }
        }
        else if (fielderAction == "Show3rdUmpireResultForRunout")
        {
            if (_stayStartTime + 1f < Time.time)
            {
                HideReplay();
                UpdateThirdUmpireRunoutResult();
                fielderAction = string.Empty;
                _stayStartTime = Time.time;
                return;
            }
        }
        else if (fielderAction == "waitForResult")
        {
            float num14 = 3f;
            if (isReplayModeActive)
            {
                num14 = 2f;
            }
            if (getOverStepBall())
            {
                num14 = 0f;
            }
            if (_stayStartTime + num14 + delayBetweenDeliveries < Time.time)
            {
                batsmanOutIndexValue = runOutScenario("b");
                if (hasRunOutOccurred)
                {
                    if (isOversteppedDelivery)
                    {
                        fielderAction = "bowlerrunoutappealsandout";
                        bool flag3 = checkForMatchComplete(runsScoredThisBall + 1, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets + 1);
                        if (CONTROLLER.matchType == "oneday" && !flag3)
                        {
                            noBallActionDelay = 7f;
                            mainUmpireAnim.Play("OutNoBallFreeHit_New");
                            isLineFreeHitActive = true;
                            lastBowledBallType = "overstep";
                        }
                        else
                        {
                            noBallActionDelay = 2.75f;
                            mainUmpireAnim.Play("OutNoBallFreeHit_New");
                        }
                    }
                    else if (isLineFreeHitActive)
                    {
                        fielderAction = string.Empty;
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 1, batsmanOutIndexValue, isBoundary: false);
                        isLineFreeHitActive = false;
                        lastBowledBallType = "lineball";
                        freeHit = false;

                        Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                    }
                    else
                    {
                        if (isReplayModeActive)
                        {
                            fielderAction = string.Empty;
                            HideReplay();
                            return;
                        }
                        fielderAction = string.Empty;
                        if (Singleton<GameData>.instance != null)
                        {
                            if (noBall)
                            {
                                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                                Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                                CONTROLLER.isJokerCall = false;
                                if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                                {
                                }
                            }
                            else if (freeHit)
                            {
                                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 0, batsmanOutIndexValue, isBoundary: false);
                                freeHit = false;
                            }
                            else if (!noBall && !freeHit)
                            {
                                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 1, batsmanOutIndexValue, isBoundary: false);
                            }
                        }
                    }
                }
                else if (isOversteppedDelivery)
                {
                    fielderAction = "bowlerrunoutappealbutnotout";
                    bool flag4 = checkForMatchComplete(runsScoredThisBall + 1, 0);
                    if (CONTROLLER.matchType == "oneday" && !flag4)
                    {
                        noBallActionDelay = 7f;
                        mainUmpireAnim.Play("NoBallFreeHit_New");
                        isLineFreeHitActive = true;
                        lastBowledBallType = "overstep";
                    }
                    else
                    {
                        noBallActionDelay = 1.5f;
                        mainUmpireAnim.Play("NoBallFreeHit_New");
                    }
                }
                else if (isLineFreeHitActive)
                {
                    fielderAction = string.Empty;
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    freeHit = false;
                    isLineFreeHitActive = false;
                    lastBowledBallType = "lineball";
                    Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                }
                else
                {
                    fielderAction = string.Empty;
                    if (Singleton<GameData>.instance != null)
                    {
                        if (noBall)
                        {
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                            {
                            }
                        }
                        else if (freeHit)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        }
                        else if (!noBall && !freeHit)
                        {
                            if (!isReplayModeActive)
                            {
                                isRunForRunOutFailedPostReplay = true;
                                if (CONTROLLER.canShowReplay)
                                {
                                    Singleton<GameData>.instance.GameIsOnReplay();
                                    ShowReplay();
                                }
                                else
                                {
                                    Singleton<GameData>.instance.ReplayIsNotShown();
                                }
                            }
                            else
                            {
                                HideReplay();
                                UpdateRunAfterReplay();
                            }
                        }
                    }
                }
            }
        }
        else if (fielderAction == "bowlerrunoutappealsandout")
        {
            if (_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time)
            {
                fielderAction = string.Empty;
                if (Singleton<GameData>.instance != null)
                {
                    if (isOversteppedDelivery)
                    {
                        lineNoBallRunsScored = runsScoredThisBall;
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 0, batsmanOutIndexValue, isBoundary: false);
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                    }
                    else if (isLineFreeHitActive)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 0, batsmanOutIndexValue, isBoundary: false);
                        isLineFreeHitActive = false;
                        lastBowledBallType = "lineball";
                    }
                }
            }
        }
        else if (fielderAction == "bowlerrunoutappealbutnotout")
        {
            if (_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time)
            {
                fielderAction = string.Empty;
                if (Singleton<GameData>.instance != null)
                {
                    if (isOversteppedDelivery)
                    {
                        if (!isReplayModeActive)
                        {
                            noBallRunStatus = "bowlerrunoutappealsandnotout";
                            lineNoBallRunsScored = runsScoredThisBall;
                            if (CONTROLLER.canShowReplay)
                            {
                                Singleton<GameData>.instance.GameIsOnReplay();
                                ShowReplay();
                            }
                            else
                            {
                                Singleton<GameData>.instance.ReplayIsNotShown();
                            }
                        }
                        else
                        {
                            HideReplay();
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, batsmanOutIndexValue, isBoundary: false);
                            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                            {
                            }
                        }
                    }
                    else if (isLineFreeHitActive)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, batsmanOutIndexValue, isBoundary: false);
                        isLineFreeHitActive = false;
                        lastBowledBallType = "lineball";
                    }
                }
            }
        }
        else if (fielderAction == "lbwAppeal")
        {
            if (_stayStartTime + 1.5f + delayBetweenDeliveries < Time.time)
            {
                if (isOversteppedDelivery)
                {
                    if (isReplayModeActive)
                    {
                        fielderAction = string.Empty;
                        HideReplay();
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                        return;
                    }
                    showMainUmpireForNoBallAction();
                    fielderAction = "umpireNoBallActionForLBW";
                    bool flag5 = checkForMatchComplete(1, 0);
                    if (CONTROLLER.matchType == "oneday" && !flag5)
                    {
                        noBallActionDelay = 4f;
                        mainUmpireAnim.Play("NoBallFreeHit_New");
                        isLineFreeHitActive = true;
                        lastBowledBallType = "overstep";
                    }
                    else
                    {
                        noBallActionDelay = 1.5f;
                        mainUmpireAnim.Play("NoBallFreeHit_New");
                    }
                }
                else if (isLineFreeHitActive)
                {
                    fielderAction = string.Empty;
                    freeHit = false;
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    isLineFreeHitActive = false;
                    lastBowledBallType = "lineball";
                    Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                }
                else
                {
                    ResetFielders();
                    fielderAction = "waitForLbwResult";
                    showPreviewCamera(status: false);
                    if (!isReplayModeActive || isLBW)
                    {
                    }
                    if (isReplayModeActive)
                    {
                        fielderAction = string.Empty;
                        HideReplay();
                        isDRSEnabled = false;
                        return;
                    }
                    if (!noBall && !freeHit)
                    {
                        isDRSEnabled = true;
                    }
                    else
                    {
                        isDRSEnabled = false;
                    }
                    // ── ONLINE LBW OVER-HANG FIX: commit the outcome NOW, decoupled from the review ──────────
                    // The dot/wicket is normally recorded at the separate "waitForLbwResult" gate (~5467) only
                    // AFTER the review resolves — but the review gate below (~5528) sets currentActionState=-1
                    // THIS frame, and online the DRS review never resolves cleanly, so 5467 never fires, no
                    // RpcBallOutcome is sent, and the over HANGS (~30s + a phantom 2nd auto-bowled ball).
                    // Record the authoritative on-field outcome here (online = no overturn, so the umpire's
                    // on-field decision stands). UpdateCurrentBall sends CmdBallOutcome on the batting authority
                    // and self-suppresses on the bowling follower. The params mirror the 5467 gate EXACTLY.
                    // Guards: online only; once per delivery (mpLbwOutcomeCommitted, which also makes 5467
                    // idempotent); and NOT while a GameData UltraEdge defer is active (it would swallow the
                    // send and relocate the hang). The review panel still opens below — purely cosmetic online.
                    if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                        && !mpLbwOutcomeCommitted && !isUltraEdgeCutscenePlaying
                        && Singleton<GameData>.instance != null)
                    {
                        mpLbwOutcomeCommitted = true;
                        ConstantsData_M.MpLog($"[GroundController][LBW] Online appeal-mark outcome commit: isLBW={isLBW} noBall={noBall} freeHit={freeHit}");
                        if (noBall)
                        {
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            CONTROLLER.isJokerCall = false;
                        }
                        else if (freeHit)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            freeHit = false;
                            isFreeHitActive = false;
                        }
                        else if (isLBW)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 1, 2, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                        }
                        else
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        }
                        // OPTION A — online LBW resolves on the on-field decision ONLY (no review panel / no
                        // third-umpire screen / no replay). The whole review/third-umpire flow proved too fragile
                        // online (panel-show timing, AI auto-review, keeper stumping board, camera + digital board,
                        // bowler-rearm) and kept hanging the match. The outcome is already authoritative + committed
                        // above (the over advances to the bowler-ready state), so SKIP the review entirely:
                        //  - isDRSEnabled=false  -> the review gate (~5528) below won't fire, so no DRS panel.
                        //  - clear the replay/overlay flags so nothing lingers.
                        //  (The keeper third-umpire board is separately skipped in ShowThirdUmpireDecisionBoard when
                        //   mpLbwOutcomeCommitted.) The umpire's on-field NOT-OUT/OUT animation still plays below.
                        isDRSEnabled = false;
                        CONTROLLER.reviewReplay = false;
                        CONTROLLER.canShowReplay = false;
                        CONTROLLER.ReplayShowing = false;
                        if (Singleton<DRS>.instance != null) Singleton<DRS>.instance.DRSreplay = false;
                    }
                    showMainUmpireForNoBallAction();
                    if (isLBW)
                    {
                        if (noBall || freeHit)
                        {
                            mainUmpireAnim.Play("NotOut");
                        }
                        if (!noBall && !freeHit)
                        {
                            mainUmpireAnim.Play("Out");
                        }
                    }
                    else
                    {
                        mainUmpireAnim.Play("NotOut");
                        if (Singleton<GameData>.instance != null && !isReplayModeActive)
                        {
                            Singleton<GameData>.instance.PlayGameSound("Beaten");
                        }
                    }
                }
            }
        }
        else if (fielderAction == "umpireNoBallActionForLBW")
        {
            if (_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time)
            {
                fielderAction = string.Empty;
                if (Singleton<GameData>.instance != null)
                {
                    if (isOversteppedDelivery)
                    {
                        if (!isReplayModeActive)
                        {
                            noBallRunStatus = "lbwappeal";
                            if (CONTROLLER.canShowReplay)
                            {
                                Singleton<GameData>.instance.GameIsOnReplay();
                                ShowReplay();
                            }
                            else
                            {
                                Singleton<GameData>.instance.ReplayIsNotShown();
                            }
                        }
                        else
                        {
                            HideReplay();
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                            {
                            }
                        }
                    }
                    else if (isLineFreeHitActive)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        isLineFreeHitActive = false;
                        lastBowledBallType = "lineball";
                    }
                }
            }
        }
        else if (fielderAction == "waitForLbwResult" && _stayStartTime + 2f + delayBetweenDeliveries < Time.time && !mpLbwOutcomeCommitted)
        {
            if (isLBW)
            {
                fielderAction = string.Empty;
                if (isReplayModeActive)
                {
                    HideReplay();
                    return;
                }
                if (Singleton<GameData>.instance != null)
                {
                    if (noBall)
                    {
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        CONTROLLER.isJokerCall = false;
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                    }
                    else if (freeHit)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        freeHit = false;
                        isFreeHitActive = false;
                    }
                    else if (!noBall && !freeHit)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 1, 2, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                    }
                }
            }
            else
            {
                fielderAction = string.Empty;
                currentActionState = 10;
                if (Singleton<GameData>.instance != null)
                {
                    if (noBall)
                    {
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        CONTROLLER.isJokerCall = false;
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                    }
                    else if (freeHit)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        freeHit = false;
                        isFreeHitActive = false;
                    }
                    else
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    }
                }
            }
        }
        if (isDRSEnabled && Singleton<DRS>.instance.enable && !Singleton<DRS>.instance.DRSreplay && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.PlayModeSelected != 6)
        {
            // Online review kill-switch (ConstantsData_M.enableOnlineReview, default OFF). The umpire's
            // on-field decision is already committed at the appeal mark before any review runs, so simply
            // not opening the review is the same resolution the DECLINE button produced — it clears
            // isDRSEnabled and lets play continue, minus the panel, the countdown and the review-resolver
            // camera teardown that kept diverging the two screens. Offline/AI keep the full review.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && !ConstantsData_M.enableOnlineReview)
            {
                isDRSEnabled = false;
                ConstantsData_M.MpLog("[Review] Online review DISABLED — umpire's on-field decision stands, no DRS panel.");
            }
            else
            {
                currentActionState = -1;
                waitForReview();
            }
        }
    }

    private void FindNewBowlingSpot()
    {
        ShowBowlingSpot();
        if (BowlingBy == "computer")
        {
            //if (CONTROLLER.PlayModeSelected == 6)
            //{
            //	if (currentBatsmanHand == "right")
            //	{
            //		bowlingSpotGO.transform.position = Multiplayer.oversData[CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6].bowlingSpotR[CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls % 6];
            //	}
            //	else if (currentBatsmanHand == "left")
            //	{
            //		bowlingSpotGO.transform.position = Multiplayer.oversData[CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6].bowlingSpotL[CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls % 6];
            //	}
            //	bowlingSpotGO.transform.position = new Vector3(bowlingSpotGO.transform.position.x, bowlingSpotGO.transform.position.y + 0.06f, bowlingSpotGO.transform.position.z);
            //	return;
            //}
            float num = 0f;
            float num2 = 0f;
            if (fullTossOdds == 0 || fullTossOdds == 1)
            {
                fullTossOdds = UnityEngine.Random.Range(3, 10);
            }
            else
            {
                fullTossOdds = UnityEngine.Random.Range(0, 20);
            }
            if (batsmanHand == "right")
            {
                if (CONTROLLER.PlayModeSelected == 6)
                {
                    //bowlingSpotGO.transform.position = Multiplayer.oversData[CONTROLLER.currentMatchBalls / 6].bowlingSpotR[CONTROLLER.currentMatchBalls % 6];
                    //bowlingSpotGO.transform.position = new Vector3(bowlingSpotGO.transform.position.x, bowlingSpotGO.transform.position.y + 0.06f, bowlingSpotGO.transform.position.z);
                }
                else if (fullTossOdds <= 1)
                {
                    num = UnityEngine.Random.Range(RightHandedBatsmanMaxLimit.transform.position.x, RightHandedBatsmanMinLimit.transform.position.x);
                    num2 = UnityEngine.Random.Range(RightHandedBatsmanMaxLimit.transform.position.z, RightHandedBatsmanMinLimit.transform.position.z);
                    bowlingSpot.position = new Vector3(num, bowlingSpot.position.y, UnityEngine.Random.Range(RightHandedBatsmanMaxLimit.transform.position.z, num2));
                }
                else
                {
                    num = UnityEngine.Random.Range(RightHandedBatsmanMaxLimit.transform.position.x, RightHandedBatsmanMinLimit.transform.position.x);
                    num2 = UnityEngine.Random.Range(RightHandedBatsmanMaxLimit.transform.position.z - 6f, RightHandedBatsmanMinLimit.transform.position.z);
                    bowlingSpot.position = new Vector3(num, bowlingSpot.position.y, UnityEngine.Random.Range(RightHandedBatsmanMaxLimit.transform.position.z - 6f, num2));
                }
                float num3 = RightHandedBatsmanMaxLimit.transform.position.z - 6f;
                if (bowlerSide == "right" && bowlingSpot.position.z < 6f && bowlingSpot.position.x < -0.5f)
                {
                    bowlingSpot.position += new Vector3(0.5f, 0f, 0f);
                }
                if (bowlingSpot.position.z > 9.8f)
                {
                    FullTossFunction(AI: false);
                }
            }
            else if (batsmanHand == "left")
            {
                if (fullTossOdds == 0 || fullTossOdds == 1)
                {
                    fullTossOdds = UnityEngine.Random.Range(3, 10);
                }
                else
                {
                    fullTossOdds = UnityEngine.Random.Range(0, 20);
                }
                if (CONTROLLER.PlayModeSelected == 6)
                {
                    //bowlingSpotGO.transform.position = Multiplayer.oversData[CONTROLLER.currentMatchBalls / 6].bowlingSpotL[CONTROLLER.currentMatchBalls % 6];
                    //bowlingSpotGO.transform.position = new Vector3(bowlingSpotGO.transform.position.x, bowlingSpotGO.transform.position.y + 0.06f, bowlingSpotGO.transform.position.z);
                }
                else if (fullTossOdds <= 1)
                {
                    num = UnityEngine.Random.Range(LeftHandedBatsmanMinLimit.transform.position.x, LeftHandedBatsmanMaxLimit.transform.position.x);
                    num2 = UnityEngine.Random.Range(LeftHandedBatsmanMaxLimit.transform.position.z, LeftHandedBatsmanMinLimit.transform.position.z);
                    bowlingSpot.position = new Vector3(num, bowlingSpot.position.y, UnityEngine.Random.Range(LeftHandedBatsmanMaxLimit.transform.position.z - 6f, num2));
                }
                else
                {
                    num = UnityEngine.Random.Range(LeftHandedBatsmanMinLimit.transform.position.x, LeftHandedBatsmanMaxLimit.transform.position.x);
                    num2 = UnityEngine.Random.Range(LeftHandedBatsmanMaxLimit.transform.position.z - 6f, LeftHandedBatsmanMinLimit.transform.position.z);
                    bowlingSpot.position = new Vector3(num, bowlingSpot.position.y, UnityEngine.Random.Range(LeftHandedBatsmanMaxLimit.transform.position.z - 6f, num2));
                }
                if (bowlerSide == "left" && bowlingSpot.position.z < 6f && bowlingSpot.position.x > 0.5f)
                {
                    bowlingSpot.position -= new Vector3(0.5f, 0f, 0f);
                }
                if (bowlingSpot.position.z > 9.8f)
                {
                    FullTossFunction(AI: false);
                }
            }
            AvoidWideBall();
        }
        else if (BowlingBy == "user" && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
        {
            if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.StrikerHand == "right")
            {
                if (GameConstants.isWithAI == false)
                {
                    //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(-0.6f, 0.06f, 5.7f));  
                    //                      //Photon Removal
                    RPC_ChangeBowlinSpot(new Vector3(-0.6f, 0.06f, 5.7f));
                    CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(-0.6f, 0.06f, 5.7f));
                }



            }
            else if (CONTROLLER.PlayModeSelected == 8)
            {
                if (GameConstants.isWithAI == false)
                {
                    //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(0.6f, 0.06f, 5.7f));   
                    RPC_ChangeBowlinSpot(new Vector3(0.6f, 0.06f, 5.7f));
                    CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(0.6f, 0.06f, 5.7f));                  //Photon Removal
                }



            }
            else
            {
                bowlingSpot.position = new Vector3(UnityEngine.Random.Range(userBowlingMinimumLimit.transform.position.x, userBowlingMaximumLimit.transform.position.x), bowlingSpot.position.y, UnityEngine.Random.Range(userBowlingMaximumLimit.transform.position.z, userBowlingMinimumLimit.transform.position.z - 2.75f));
            }
            if (CONTROLLER.tutorialToggle == 1)
            {
                Singleton<GameData>.instance.ShowTutorial(2);
            }
        }
    }

    private void AvoidWideBall()
    {
        int num = 0;
        int num2 = 0;
        int num3 = 5;
        int num4 = 2;
        float x = 0f;
        float num5 = 9f;
        int num6 = UnityEngine.Random.Range(0, 10);
        Vector3 vector = new Vector3(x, bowlingSpot.position.y, bowlingSpot.position.z);
        if (ObscuredPrefs.HasKey("bowlerBowlStraight" + CONTROLLER.PlayModeSelected))
        {
            num = ObscuredPrefs.GetInt("bowlerBowlStraight" + CONTROLLER.PlayModeSelected);
        }
        if (ObscuredPrefs.HasKey("stumpBallCount" + CONTROLLER.PlayModeSelected))
        {
            num2 = ObscuredPrefs.GetInt("stumpBallCount" + CONTROLLER.PlayModeSelected);
        }
        if (num < 5 && num6 <= num3)
        {
            if (num6 < num4 && num2 < 1)
            {
                AIBowlToStump();
                num2++;
                ObscuredPrefs.SetInt("stumpBallCount" + CONTROLLER.PlayModeSelected, num2);
            }
            else
            {
                x = ((bowlerSide == "left") ? ((!(batsmanHand == "left")) ? UnityEngine.Random.Range(-0.6f, -0.2f) : UnityEngine.Random.Range(-0.15f, 0f)) : ((!(batsmanHand == "left")) ? UnityEngine.Random.Range(0f, 0.15f) : UnityEngine.Random.Range(0.2f, 0.6f)));
                bowlingSpot.position = new Vector3(x, bowlingSpot.position.y, bowlingSpot.position.z);
            }
            num++;
            ObscuredPrefs.SetInt("bowlerBowlStraight" + CONTROLLER.PlayModeSelected, num);
        }
    }

    public Vector3 BallTempPos(Vector3 BallTransformTemp)
    {
        if (bowlerSide == "left")
        {
            if (bowlerHand == "right")
            {
                BallTransformTemp = new Vector3(-0.7f, BallTransformTemp.y, BallTransformTemp.z);
            }
            else if (bowlerHand == "left")
            {
                BallTransformTemp = new Vector3(-0.8f, BallTransformTemp.y, BallTransformTemp.z);
            }
        }
        else if (bowlerSide == "right")
        {
            if (bowlerHand == "right")
            {
                BallTransformTemp = new Vector3(0.9f, BallTransformTemp.y, BallTransformTemp.z);
            }
            else if (bowlerHand == "left")
            {
                BallTransformTemp = new Vector3(0.7f, BallTransformTemp.y, BallTransformTemp.z);
            }
        }
        return BallTransformTemp;
    }

    private void FullTossFunction(bool AI)
    {
        isFullToss = true;
        if (!AI)
        {
            fullBallLength = 0f;
            _fullTossDisplacementFromCrease = 0f;
            fullTossCalcA = 0f;
            fullTossCalcB = 0f;
            Vector3 vector = new Vector3(0f, 0f, 8.9f);
            temporaryBallStartPoint.transform.position = matchBallTransform.position;
            temporaryBallStartPoint.transform.position = BallTempPos(temporaryBallStartPoint.transform.position);
            fullBallLength = bowlingSpot.position.z + 8.8f;
            tempVal = userBowlingFullTossTriggerPoint.transform.position.z + 8.8f;
            angleToStumpLine = AngleBetweenTwoVector3(temporaryBallStartPoint.transform.position, bowlingSpot.position);
            pitchDiagonalDistance = (userBowlingFullTossTriggerPoint.transform.position.z + 8.8f) / Mathf.Sin(angleToStumpLine * degToRad);
            vector.x = pitchDiagonalDistance * Mathf.Cos(angleToStumpLine * degToRad);
            vector.x -= temporaryBallStartPoint.transform.position.x * -1f;
            Vector3 vector2 = ballStartPositionGO.transform.InverseTransformPoint(bowlingSpot.position);
            float num = 90f - Mathf.Atan2(vector2.x, vector2.z) * radToDeg;
            bowlingSpotFullTossObject.transform.position = new Vector3(bowlingSpot.position.x, bowlingSpotFullTossObject.transform.position.y, bowlingSpotFullTossObject.transform.position.z);
            deltaXFromPitchOrigin = bowlingSpotFullTossObject.transform.position.x - bowlingSpot.position.x;
            deltaZFromPitchOrigin = bowlingSpot.position.z - bowlingSpotFullTossObject.transform.position.z;
            creaseLineBaseLength = Mathf.Sqrt(deltaXFromPitchOrigin * deltaXFromPitchOrigin + deltaZFromPitchOrigin * deltaZFromPitchOrigin);
            creaseAngleToDiagonal = Mathf.Atan2(deltaXFromPitchOrigin, deltaZFromPitchOrigin) * radToDeg - (90f - num - spinFactor);
            creaseLineLength = creaseLineBaseLength / Mathf.Cos(creaseAngleToDiagonal * degToRad);
            creaseVerticalOffset = Mathf.Sqrt(creaseLineLength * creaseLineLength - creaseLineBaseLength * creaseLineBaseLength);
            fullTossTriggerPoint = userBowlingFullTossTriggerPoint.transform.position.z + 8.8f;
            _fullTossDisplacementFromCrease = fullBallLength - fullTossTriggerPoint;
            swingDisplacement = DistanceBetweenTwoVector2(vector, bowlingSpot.position);
            fullTossCalcA = 90f / fullBallLength * _fullTossDisplacementFromCrease;
            fullTossCalcB = 360f - fullTossCalcA;
            fullTossYPosition = Mathf.Sin(fullTossCalcB * degToRad) * arcHeight - ballSize;
            fullTossXPosition /= 4f;
            if (fullTossYPosition < 0f)
            {
                fullTossYPosition *= -1f;
            }
            if (creaseAngleToDiagonal > 0f && swingIntensity != 0f)
            {
                bowlingSpotFullTossObject.transform.position = new Vector3(bowlingSpotFullTossObject.transform.position.x + creaseVerticalOffset, fullTossYPosition, userBowlingFullTossTriggerPoint.transform.position.z);
            }
            else if (creaseAngleToDiagonal < 0f && swingIntensity != 0f)
            {
                bowlingSpotFullTossObject.transform.position = new Vector3(0f - creaseVerticalOffset + bowlingSpotFullTossObject.transform.position.x, fullTossYPosition, userBowlingFullTossTriggerPoint.transform.position.z);
            }
            else
            {
                bowlingSpotFullTossObject.transform.position = new Vector3(vector.x, fullTossYPosition, userBowlingFullTossTriggerPoint.transform.position.z);
            }
            creaseImpactSpot.transform.position = new Vector3(bowlingSpotFullTossObject.transform.position.x, 0f, bowlingSpotFullTossObject.transform.position.z);
            stumpImpactSpot.transform.position = new Vector3(bowlingSpotFullTossObject.transform.position.x, stumpImpactSpot.transform.position.y, stumpImpactSpot.transform.position.z);
            float num2 = stumpImpactSpot.transform.position.x - bowlingSpotFullTossObject.transform.position.x;
            float num3 = stumpImpactSpot.transform.position.z - bowlingSpotFullTossObject.transform.position.z;
            float num4 = Mathf.Sqrt(num2 * num2 + num3 * num3);
            float num5 = Mathf.Atan2(num2, num3) * radToDeg - (90f - num - spinFactor);
            float num6 = num4 / Mathf.Cos(num5 * degToRad);
            float x = Mathf.Sqrt(num6 * num6 - num4 * num4);
            if (num5 < 0f)
            {
                stumpImpactSpot.transform.position += new Vector3(x, 0f, 0f);
            }
            else
            {
                stumpImpactSpot.transform.position -= new Vector3(x, 0f, 0f);
            }
            if ((double)bowlingSpot.position.z > 9.8)
            {
                bowlingSpotFullTossObject.SetActive(value: true);
                bowlingSpotRenderer.enabled = false;
                if (tutorialIndicatorArrow != null)
                {
                    tutorialIndicatorArrow.SetActive(value: false);
                }
            }
        }
        else
        {
            fullBallLength = 0f;
            _fullTossDisplacementFromCrease = 0f;
            fullTossCalcA = 0f;
            fullTossCalcB = 0f;
            Vector3 vector3 = new Vector3(0f, 0f, 8.9f);
            temporaryBallStartPoint.transform.position = matchBallTransform.position;
            temporaryBallStartPoint.transform.position = BallTempPos(temporaryBallStartPoint.transform.position);
            fullBallLength = bowlingSpot.position.z + 8.8f;
            tempVal = userBowlingFullTossTriggerPoint.transform.position.z + 8.8f;
            angleToStumpLine = AngleBetweenTwoVector3(temporaryBallStartPoint.transform.position, bowlingSpot.position);
            pitchDiagonalDistance = (userBowlingFullTossTriggerPoint.transform.position.z + 8.8f) / Mathf.Sin(angleToStumpLine * degToRad);
            vector3.x = pitchDiagonalDistance * Mathf.Cos(angleToStumpLine * degToRad);
            vector3.x -= temporaryBallStartPoint.transform.position.x * -1f;
            Vector3 vector4 = ballStartPositionGO.transform.InverseTransformPoint(bowlingSpot.position);
            float num7 = 90f - Mathf.Atan2(vector4.x, vector4.z) * radToDeg;
            bowlingSpotFullTossObject.transform.position = new Vector3(bowlingSpot.position.x, bowlingSpotFullTossObject.transform.position.y, bowlingSpotFullTossObject.transform.position.z);
            deltaXFromPitchOrigin = bowlingSpotFullTossObject.transform.position.x - bowlingSpot.position.x;
            deltaZFromPitchOrigin = bowlingSpot.position.z - bowlingSpotFullTossObject.transform.position.z;
            creaseLineBaseLength = Mathf.Sqrt(deltaXFromPitchOrigin * deltaXFromPitchOrigin + deltaZFromPitchOrigin * deltaZFromPitchOrigin);
            creaseAngleToDiagonal = Mathf.Atan2(deltaXFromPitchOrigin, deltaZFromPitchOrigin) * radToDeg - (90f - num7 - spinFactor);
            creaseLineLength = creaseLineBaseLength / Mathf.Cos(creaseAngleToDiagonal * degToRad);
            creaseVerticalOffset = Mathf.Sqrt(creaseLineLength * creaseLineLength - creaseLineBaseLength * creaseLineBaseLength);
            fullTossTriggerPoint = userBowlingFullTossTriggerPoint.transform.position.z + 8.8f;
            _fullTossDisplacementFromCrease = fullBallLength - fullTossTriggerPoint;
            swingDisplacement = DistanceBetweenTwoVector2(vector3, bowlingSpot.position);
            fullTossCalcA = 90f / fullBallLength * _fullTossDisplacementFromCrease;
            fullTossCalcB = 360f - fullTossCalcA;
            fullTossYPosition = Mathf.Sin(fullTossCalcB * degToRad) * arcHeight - ballSize;
            fullTossXPosition /= 4f;
            if (fullTossYPosition < 0f)
            {
                fullTossYPosition *= -1f;
            }
            if (creaseAngleToDiagonal > 0f && swingIntensity != 0f)
            {
                bowlingSpotFullTossObject.transform.position = new Vector3(bowlingSpotFullTossObject.transform.position.x + creaseVerticalOffset, fullTossYPosition, userBowlingFullTossTriggerPoint.transform.position.z);
            }
            else if (creaseAngleToDiagonal < 0f && swingIntensity != 0f)
            {
                bowlingSpotFullTossObject.transform.position = new Vector3(0f - creaseVerticalOffset + bowlingSpotFullTossObject.transform.position.x, fullTossYPosition, userBowlingFullTossTriggerPoint.transform.position.z);
            }
            else
            {
                bowlingSpotFullTossObject.transform.position = new Vector3(vector3.x, fullTossYPosition, userBowlingFullTossTriggerPoint.transform.position.z);
            }
            bowlingSpotFullTossObject.SetActive(value: true);
            bowlingSpotRenderer.enabled = false;
            if (tutorialIndicatorArrow != null)
            {
                tutorialIndicatorArrow.SetActive(value: false);
            }
        }
    }

    //Photon Removal  [PunRPC]
    public void RPC_UserChangingBowlingSpot()
    {
        if (!(BowlingBy == "user") || !canUserBowlerMoveBowlingSpot || !(Time.timeScale >= 1f) || (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8))
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            isUpArrowKeyPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            isDownArrowKeyPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            isLeftArrowKeyPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isRightArrowKeyPressed = true;
        }
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            isUpArrowKeyPressed = false;
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            isDownArrowKeyPressed = false;
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            isLeftArrowKeyPressed = false;
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            isRightArrowKeyPressed = false;
        }
        if (Input.GetKeyDown(KeyCode.S) && !isUserBowlingSpotSelected)
        {
            isUserBowlingSpotSelected = true;
            canUserBowlerMoveBowlingSpot = false;
            FreezeBowlingSpot();
            Singleton<GameData>.instance.ShowTutorial(-1);
        }
        else if (!isUserBowlingSpotSelected && canShowFieldControlPowers)
        {
            float num = 2f;
            float num2 = 5f;
            if ((CONTROLLER.TargetPlatform != "ios" && CONTROLLER.TargetPlatform != "android") || Application.isEditor)
            {
                if (Input.GetMouseButton(0))
                {
                    if (previousMousePosition.x == 0f && previousMousePosition.y == 0f)
                    {
                        previousMousePosition = Input.mousePosition;
                    }
                    float num3 = 20f;
                    Vector2 vector = new Vector2(Input.mousePosition.x - previousMousePosition.x, Input.mousePosition.y - previousMousePosition.y);
                    previousMousePosition = Input.mousePosition;
                    bowlingSpot.position += new Vector3(vector.x * 0.02f, 0f, vector.y * 0.02f);
                    if (bowlingSpot.position.z > userBowlingFullTossTriggerPoint.transform.position.z)
                    {
                        FullTossFunction(AI: false);
                    }
                    if (bowlingSpot.position.z < userBowlingFullTossTriggerPoint.transform.position.z + 1f && bowlingSpot.position.z < 11f)
                    {
                        isFullToss = false;
                        bowlingSpotRenderer.enabled = true;
                        if (tutorialIndicatorArrow != null)
                        {
                            tutorialIndicatorArrow.SetActive(value: true);
                        }
                        bowlingSpotFullTossObject.transform.position = bowlingSpot.position;
                        bowlingSpotFullTossObject.SetActive(value: false);
                    }
                    if (bowlingSpot.position.x < userBowlingMinimumLimit.transform.position.x)
                    {
                        bowlingSpot.position = new Vector3(userBowlingMinimumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    }
                    if (bowlingSpot.position.x > userBowlingMaximumLimit.transform.position.x)
                    {
                        bowlingSpot.position = new Vector3(userBowlingMaximumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    }
                    if (bowlingSpot.position.z > userBowlingMinimumLimit.transform.position.z)
                    {
                        bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMinimumLimit.transform.position.z);
                    }
                    if (bowlingSpot.position.z < userBowlingMaximumLimit.transform.position.z)
                    {
                        bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMaximumLimit.transform.position.z);
                    }
                }
                if (Input.GetMouseButtonUp(0))
                {
                    isLeftArrowKeyPressed = false;
                    isRightArrowKeyPressed = false;
                    isUpArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                    previousMousePosition = Vector2.zero;
                }
            }
            else
            {
                int num4 = 2;
                int num5 = 5;
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    Vector2 deltaPosition = Input.GetTouch(0).deltaPosition;
                    bowlingSpot.position += new Vector3(deltaPosition.x * 0.02f, 0f, deltaPosition.y * 0.06f);
                    if (bowlingSpot.position.z > userBowlingFullTossTriggerPoint.transform.position.z)
                    {
                        FullTossFunction(AI: false);
                    }
                    if (bowlingSpot.position.z < userBowlingFullTossTriggerPoint.transform.position.z + 1f && bowlingSpot.position.z < 11f)
                    {
                        isFullToss = false;
                        bowlingSpotRenderer.enabled = true;
                        if (tutorialIndicatorArrow != null)
                        {
                            tutorialIndicatorArrow.SetActive(value: true);
                        }
                        bowlingSpotFullTossObject.transform.position = bowlingSpot.position;
                        bowlingSpotFullTossObject.SetActive(value: false);
                    }
                    if (bowlingSpot.position.x < userBowlingMinimumLimit.transform.position.x)
                    {
                        bowlingSpot.position = new Vector3(userBowlingMinimumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    }
                    if (bowlingSpot.position.x > userBowlingMaximumLimit.transform.position.x)
                    {
                        bowlingSpot.position = new Vector3(userBowlingMaximumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    }
                    if (bowlingSpot.position.z > userBowlingMinimumLimit.transform.position.z)
                    {
                        bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMinimumLimit.transform.position.z);
                    }
                    if (bowlingSpot.position.z < userBowlingMaximumLimit.transform.position.z)
                    {
                        bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMaximumLimit.transform.position.z);
                    }
                }
                if (!Application.isEditor && Input.touchCount == 0)
                {
                    isLeftArrowKeyPressed = false;
                    isRightArrowKeyPressed = false;
                    isUpArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                }
            }
            if (isLeftArrowKeyPressed)
            {
                bowlingSpot.position -= new Vector3(num * Time.deltaTime, 0f, 0f);
                if (bowlingSpot.position.x < userBowlingMinimumLimit.transform.position.x)
                {
                    bowlingSpot.position = new Vector3(userBowlingMinimumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                }
            }
            if (isRightArrowKeyPressed)
            {
                bowlingSpot.position += new Vector3(num * Time.deltaTime, 0f, 0f);
                if (bowlingSpot.position.x > userBowlingMaximumLimit.transform.position.x)
                {
                    bowlingSpot.position = new Vector3(userBowlingMaximumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                }
            }
            if (isUpArrowKeyPressed)
            {
                bowlingSpot.position += new Vector3(0f, 0f, num2 * Time.deltaTime);
                if (bowlingSpot.position.z > userBowlingMinimumLimit.transform.position.z)
                {
                    bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMinimumLimit.transform.position.z);
                }
            }
            if (isDownArrowKeyPressed)
            {
                bowlingSpot.position -= new Vector3(0f, 0f, num2 * Time.deltaTime);
                if (bowlingSpot.position.z < userBowlingMaximumLimit.transform.position.z)
                {
                    bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMaximumLimit.transform.position.z);
                }
            }
        }
        Singleton<Tutorial>.instance.updateBowlingHolderPos(bowlingSpot.position);
    }

    private void UserChangingBowlingSpot()
    {
        if (!(BowlingBy == "user") || !canUserBowlerMoveBowlingSpot || !(Time.timeScale >= 1f) || (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8))
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            isUpArrowKeyPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            isDownArrowKeyPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            isLeftArrowKeyPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isRightArrowKeyPressed = true;
        }
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            isUpArrowKeyPressed = false;
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            isDownArrowKeyPressed = false;
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            isLeftArrowKeyPressed = false;
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            isRightArrowKeyPressed = false;
        }
        if (Input.GetKeyDown(KeyCode.S) && !isUserBowlingSpotSelected)
        {
            isUserBowlingSpotSelected = true;
            canUserBowlerMoveBowlingSpot = false;
            if (CONTROLLER.PlayModeSelected == 8)
            {
                if (GameConstants.isWithAI == false)
                {
                    //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));    //Photon Removal
                    RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                    CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                }


            }
            FreezeBowlingSpot();
            Singleton<GameData>.instance.ShowTutorial(-1);
        }
        else if (!isUserBowlingSpotSelected && canShowFieldControlPowers)
        {
            float num = 2f;
            float num2 = 5f;
            if ((CONTROLLER.TargetPlatform != "ios" && CONTROLLER.TargetPlatform != "android") || Application.isEditor)
            {
                if (Input.GetMouseButton(0))
                {
                    if (previousMousePosition.x == 0f && previousMousePosition.y == 0f)
                    {
                        previousMousePosition = Input.mousePosition;
                    }
                    float num3 = 20f;
                    Vector2 vector = new Vector2(Input.mousePosition.x - previousMousePosition.x, Input.mousePosition.y - previousMousePosition.y);
                    previousMousePosition = Input.mousePosition;
                    bowlingSpot.position += new Vector3(vector.x * 0.02f, 0f, vector.y * 0.02f);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {
                            //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));             //Photon Removal
                            RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));

                        }

                    }
                    if (bowlingSpot.position.z > userBowlingFullTossTriggerPoint.transform.position.z)
                    {
                        FullTossFunction(AI: false);
                    }
                    if (bowlingSpot.position.z < userBowlingFullTossTriggerPoint.transform.position.z + 1f && bowlingSpot.position.z < 11f)
                    {
                        isFullToss = false;
                        bowlingSpotRenderer.enabled = true;
                        if (tutorialIndicatorArrow != null)
                        {
                            tutorialIndicatorArrow.SetActive(value: true);
                        }
                        bowlingSpotFullTossObject.transform.position = bowlingSpot.position;
                        bowlingSpotFullTossObject.SetActive(value: false);
                    }
                    if (bowlingSpot.position.x < userBowlingMinimumLimit.transform.position.x)
                    {
                        bowlingSpot.position = new Vector3(userBowlingMinimumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    }
                    if (bowlingSpot.position.x > userBowlingMaximumLimit.transform.position.x)
                    {
                        bowlingSpot.position = new Vector3(userBowlingMaximumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    }
                    if (bowlingSpot.position.z > userBowlingMinimumLimit.transform.position.z)
                    {
                        bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMinimumLimit.transform.position.z);
                    }
                    if (bowlingSpot.position.z < userBowlingMaximumLimit.transform.position.z)
                    {
                        bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMaximumLimit.transform.position.z);
                    }
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {
                            //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));                                                                   //Photon Removal
                            RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        }



                    }

                }
                if (Input.GetMouseButtonUp(0))
                {
                    isLeftArrowKeyPressed = false;
                    isRightArrowKeyPressed = false;
                    isUpArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                    previousMousePosition = Vector2.zero;
                }
            }
            else
            {
                int num4 = 2;
                int num5 = 5;
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    Vector2 deltaPosition = Input.GetTouch(0).deltaPosition;
                    bowlingSpot.position += new Vector3(deltaPosition.x * 0.02f, 0f, deltaPosition.y * 0.06f);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {
                            RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));                                                                                                                                     //Photon Removal
                            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        }
                    }
                    if (bowlingSpot.position.z > userBowlingFullTossTriggerPoint.transform.position.z)
                    {
                        FullTossFunction(AI: false);
                    }
                    if (bowlingSpot.position.z < userBowlingFullTossTriggerPoint.transform.position.z + 1f && bowlingSpot.position.z < 11f)
                    {
                        isFullToss = false;
                        bowlingSpotRenderer.enabled = true;
                        if (tutorialIndicatorArrow != null)
                        {
                            tutorialIndicatorArrow.SetActive(value: true);
                        }
                        bowlingSpotFullTossObject.transform.position = bowlingSpot.position;
                        bowlingSpotFullTossObject.SetActive(value: false);
                    }
                    if (bowlingSpot.position.x < userBowlingMinimumLimit.transform.position.x)
                    {
                        bowlingSpot.position = new Vector3(userBowlingMinimumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    }
                    if (bowlingSpot.position.x > userBowlingMaximumLimit.transform.position.x)
                    {
                        bowlingSpot.position = new Vector3(userBowlingMaximumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    }
                    if (bowlingSpot.position.z > userBowlingMinimumLimit.transform.position.z)
                    {
                        bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMinimumLimit.transform.position.z);
                    }
                    if (bowlingSpot.position.z < userBowlingMaximumLimit.transform.position.z)
                    {
                        bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMaximumLimit.transform.position.z);
                    }
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {
                            // view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));                            //Photon Removal
                            RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        }
                    }

                }
                if (!Application.isEditor && Input.touchCount == 0)
                {
                    isLeftArrowKeyPressed = false;
                    isRightArrowKeyPressed = false;
                    isUpArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                }
            }
            if (isLeftArrowKeyPressed)
            {
                bowlingSpot.position -= new Vector3(num * Time.deltaTime, 0f, 0f);
                if (CONTROLLER.PlayModeSelected == 8)
                {
                    if (GameConstants.isWithAI == false)
                    {
                        //view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                    }
                    // Photon Removal
                }
                if (bowlingSpot.position.x < userBowlingMinimumLimit.transform.position.x)
                {
                    bowlingSpot.position = new Vector3(userBowlingMinimumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {
                            //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        }

                    }
                }
            }
            if (isRightArrowKeyPressed)
            {
                bowlingSpot.position += new Vector3(num * Time.deltaTime, 0f, 0f);
                if (CONTROLLER.PlayModeSelected == 8)
                {
                    if (GameConstants.isWithAI == false)
                    {
                        //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                    }

                }
                if (bowlingSpot.position.x > userBowlingMaximumLimit.transform.position.x)
                {
                    bowlingSpot.position = new Vector3(userBowlingMaximumLimit.transform.position.x, bowlingSpot.position.y, bowlingSpot.position.z);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {                                                                                                                                                           //Photon Removal
                                                                                                                                                                                    // view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        }

                    }
                }
            }
            if (isUpArrowKeyPressed)
            {
                bowlingSpot.position += new Vector3(0f, 0f, num2 * Time.deltaTime);
                if (CONTROLLER.PlayModeSelected == 8)
                {
                    if (GameConstants.isWithAI == false)
                    {
                        //view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));                                                                                  //Photon Removal
                        RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                    }
                }
                if (bowlingSpot.position.z > userBowlingMinimumLimit.transform.position.z)
                {
                    bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMinimumLimit.transform.position.z);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {                                                                                                                                                                                                                                               //Photon Removal
                                                                                                                                                                                                                                                                        //    view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        }
                    }
                }
            }
            if (isDownArrowKeyPressed)
            {
                bowlingSpot.position -= new Vector3(0f, 0f, num2 * Time.deltaTime);
                if (CONTROLLER.PlayModeSelected == 8)
                {
                    if (GameConstants.isWithAI == false)
                    {
                        //view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));                                                                             //Photon Removal
                        RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                    }


                }
                if (bowlingSpot.position.z < userBowlingMaximumLimit.transform.position.z)
                {
                    bowlingSpot.position = new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, userBowlingMaximumLimit.transform.position.z);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {
                            //view.RPC("RPC_ChangeBowlinSpot", RpcTarget.AllBuffered, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));                                                                                //Photon Removal
                            RPC_ChangeBowlinSpot(new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                            CricketNetworkManager.instance.CmdChangeBowlinSpot(staticVariables.UserProfiledata.user._id, new Vector3(bowlingSpot.position.x, bowlingSpot.position.y, bowlingSpot.position.z));
                        }


                    }
                }
            }
        }
        Singleton<Tutorial>.instance.updateBowlingHolderPos(bowlingSpot.position);
    }

    public void BallMovement()
    {
        ////ConstantsData_M.MpLog("* call");
        //EditorApplication.isPaused = true;

        float x = Mathf.Cos(_ballAngle * degToRad) * horizontalVelocity * BallDt();
        float z = Mathf.Sin(_ballAngle * degToRad) * horizontalVelocity * BallDt();
        float num = Mathf.Sin(launchAngle * degToRad) * arcHeight - ballSize;
        if (float.IsNaN(num))
        {
            num = 0f;
        }

        matchBallTransform.position = new Vector3(temporaryPosition.x, 0f - num, temporaryPosition.z);

        //ballTransform.position = new Vector3(ballTransform.position.x, 0f - num, ballTransform.position.z);

        //ballTransform.position = new Vector3(ballTransform.position.x, 0f - num, ballTransform.position.z);
        ////ConstantsData_M.MpLog("BALL POSITION BEFORE : " + ballTransform.position + " X : "+x+ " Z : "+z);
        ////ConstantsData_M.MpLog("Temp Position : " + tempPos);
        matchBallTransform.position += new Vector3(x, 0f, z);
        temporaryPosition = matchBallTransform.position;
        matchBallTransform.position = temporaryPosition;
        ////ConstantsData_M.MpLog("BALL POSITION AFTER : " + ballTransform.position + " X : " + x + " Z : " + z);

        launchAngle += angleChangeRate * BallDt();
        //ballRayCastReferenceGOTransform.position = new Vector3(ballTransform.position.x, ballTransform.position.y, ballTransform.position.z);
        raycastAnchorBallTransform.position = temporaryPosition;
        raycastAnchorBallTransform.eulerAngles = new Vector3(raycastAnchorBallTransform.eulerAngles.x, (90f - _ballAngle + 360f) % 360f, raycastAnchorBallTransform.eulerAngles.z);
        if (isBallOnBoundaryLine)
        {
            ballSpinSpeedX = 300 + UnityEngine.Random.Range(0, 61);
            ballSpinSpeedZ = 300 + UnityEngine.Random.Range(0, 61);
        }
        matchBallTransform.Rotate(Vector3.right * Time.deltaTime * ballSpinSpeedX, Space.World);
        matchBallTransform.Rotate(Vector3.forward * Time.deltaTime * ballSpinSpeedZ, Space.World);

        if (isReplayModeActive && currentActionState == 4 && launchAngle >= 360f && !isBallOnBoundaryLine)
        {
            ballSpinSpeedX = UnityEngine.Random.Range(-3600, -1800);
            ballSpinSpeedZ = UnityEngine.Random.Range(-3600, -1800);
        }
        ////ConstantsData_M.MpLog(" X:= " + x + " Z: " + z);
        ////ConstantsData_M.MpLog(" NUM : " + num);
        ////ConstantsData_M.MpLog("BALL POSITION: " + ballTransform.position);
        ////ConstantsData_M.MpLog("BALL RAYCAST REF: " + ballRayCastReferenceGOTransform.position);
        ////ConstantsData_M.MpLog("BALL PROJECTILE ANGLE: " + ballProjectileAngle);
        ////ConstantsData_M.MpLog("BALL PROJECTILE ANGLE PER SECOND : " + ballProjectileAnglePerSecond);
        ////ConstantsData_M.MpLog("TIME>DELTA TIME : " + Time.deltaTime);
    }

    public void ShowBall(bool status)
    {
        BallSkinRenderer.enabled = status;
        ballCollider.enabled = status;
    }

    private void CustomRayCastForBowlingBallMovement()
    {
        if (!isReplayModeActive)
        {
            Vector3 direction = raycastAnchorBallTransform.TransformDirection(Vector3.forward);
            float maxDistance = 1f;
            int num = 256;
            num = ~num;
            if (Physics.Raycast(raycastAnchorBallTransform.position, direction, out var hitInfo, maxDistance, num) && !(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex))
            {
                ////ConstantsData_M.MpLog("%HITTT");
                if (CONTROLLER.PlayModeSelected != 8)
                {
                    OnCustomTriggerEnter(hitInfo.collider);
                    ballRayCastConnectionZPositionSaved = raycastAnchorBallTransform.position.z;
                    ////ConstantsData_M.MpLog("++CONNECTEDDD : " + savedBallRayCastConnectedZposition);
                }
                else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
                {
                    // False-bowled guard: the batting ball trails the authoritative (bowling) ball by
                    // lerp-lag and integrates locally between packets, so its line can clip the narrow
                    // stump collider even when the authoritative line clearly misses. Only accept a clean-bowled
                    // (Stump1Collider, batsman's stumps) raycast hit if the authoritative line at the stump
                    // plane is actually inline. Stump2Collider (post-shot/hit-wicket) is left untouched — by then
                    // the ball is batting-authoritative (isBallHit) and the streamed position is stale.
                    bool isCleanBowledStumpHit = hitInfo.collider.name == "Stump1Collider";
                    // FULL-TOSS "ball stumps ko lagti hi nahi" root: this false-bowled guard extrapolates a
                    // STRAIGHT line through the last two 20Hz streamed samples to the stump plane. A full toss
                    // is a short fast flight sampled only 2-4 times, mid-flight on the SWING CURVE (residual X
                    // up to ~0.3-0.7u off the aim line) — the linear extrapolation therefore lands outside the
                    // ±0.14 stump window and vetoed the GENUINE hit every time (bounced balls sample near the
                    // stumps post-bounce, so they pass). With the deterministic delivery sim armed the LOCAL
                    // flight IS the authoritative flight (bit-identical fixed-step path from the same synced
                    // release params), so the local raycast cannot be a false positive — bypass the stale-stream
                    // veto there. The guard (+ [BowledGuard] log) still protects the legacy frame-delta path.
                    bool _deterministicDeliveryLive = _useDeterministicBallStep
                        && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false;
                    if (isCleanBowledStumpHit && !_deterministicDeliveryLive && !IsAuthoritativeLineInlineWithStumps())
                    {
                        // Authoritative line misses the stumps — ignore this local clip and keep
                        // following the streamed position (ball passes the stumps as a miss).
                        // Observability (full-toss "should have been bowled" reports): this suppression was
                        // SILENT — log it so a device log can prove/disprove a wrongly-dropped genuine bowled.
                        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                            ConstantsData_M.MpLog($"[BowledGuard] Stump1 raycast hit SUPPRESSED (authoritative line not inline) ballPos={matchBallTransform.position} fullToss={isFullToss}");
                    }
                    else
                    {
                        OnCustomTriggerEnter(hitInfo.collider);
                        ballRayCastConnectionZPositionSaved = raycastAnchorBallTransform.position.z;
                        isBallHit = true;
                        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
                        {
                            CONTROLLER.CURRENTCOLLIDER = hitInfo.collider.name;

                            //Photon Removal if (GameConstants.isWithAI == false)
                            {

                                StartCoroutine(CallColliderRPCMultipleTimes(hitInfo.collider.name, ballRayCastConnectionZPositionSaved));
                            }
                        }
                    }
                }
                else
                {

                }
            }
            else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !isBallHit && matchBall.transform.position.z - Stump1Collider.gameObject.transform.position.z > 0.2f)
            {
                CONTROLLER.CURRENTCOLLIDER = string.Empty;
                //Photon Removal   if (GameConstants.isWithAI == false)
                {
                    StartCoroutine(CallColliderRPCMultipleTimes(string.Empty, 0f));

                }


            }

            if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && CONTROLLER.CURRENTCOLLIDER != string.Empty && CONTROLLER.CURRENTCOLLIDER != "Stump2Collider" && CONTROLLER.CURRENTCOLLIDER != "Stump1Collider" && CONTROLLER.BALLANGLE != 0f && CONTROLLER.HORIZONTALSPEED != 0f)
            {

                if (ballRayCastConnectionZPositionSaved - raycastAnchorBallTransform.position.z < 0.2f)
                {
                    (CONTROLLER.CURRENTCOLLIDER).Show();
                    OnCustomTriggerEnter(GameObject.Find(CONTROLLER.CURRENTCOLLIDER).GetComponent<Collider>());
                }
                if (Time.time > currentShotExecutionTime)
                {
                }
            }
            else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && (CONTROLLER.CURRENTCOLLIDER == "Stump2Collider" || CONTROLLER.CURRENTCOLLIDER == "Stump1Collider"))
            {

                if (ballRayCastConnectionZPositionSaved - raycastAnchorBallTransform.position.z < 0.2f)
                {
                    Stump1Collider.SetActive(true);
                    OnCustomTriggerEnter(GameObject.Find(CONTROLLER.CURRENTCOLLIDER).GetComponent<Collider>());
                }
            }

        }
        else if (isReplayModeActive && !Singleton<DRS>.instance.DRSreplay && ballRayCastConnectionZPositionSaved != 0f && raycastAnchorBallTransform.position.z > ballRayCastConnectionZPositionSaved)
        {
            if (summarySaved == "connected" || summarySaved == "catch" || summarySaved == "picked")
            {
                OnCustomTriggerEnter(BatCollider);

            }
            else if (summarySaved == "onPads")
            {
                OnCustomTriggerEnter(padColliderSaved);

            }
            else if (summarySaved == "bowled" && ballRayCastConnectionZPositionSaved != -1f && currentBallStatus != "bowled")
            {
                OnCustomTriggerEnter(Stump1Collider.GetComponent<Collider>());

                ballRayCastConnectionZPositionSaved = -1f;
            }
        }
    }

    public void BowlingBallMovement()
    {
        ////ConstantsData_M.MpLog(" CASEE 3 BowlingBallMovement");
        if (isBallPaused)
        {
            return;
        }
        ////ConstantsData_M.MpLog("HELLOOOWOWWWW");
        UpdateBattingTimingMeter();
        BallMovement();
        BallSwingMovement();
        if (CONTROLLER.PlayModeSelected != 8 && CONTROLLER.tutorialToggle == 1 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && isNextBallInPlay)
        {
            Singleton<GameData>.instance.ShowTutorial(1);
            isNextBallInPlay = false;
        }
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.tutorialToggle == 1 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && isNextBallInPlay && CONTROLLER.BOWLINGSPOTINFO)
        {
            Singleton<GameData>.instance.ShowTutorial(1);
            isNextBallInPlay = false;
        }
        if (Singleton<PreviewScreen>.instance.alertPopup.activeInHierarchy)
        {
            Singleton<PreviewScreen>.instance.alertPopup.SetActive(value: false);
        }
        if (matchBallTransform.position.z > 2.5f && isRayCastEnabled)
        {
            CustomRayCastForBowlingBallMovement();
        }
        // Full-toss keeper arm (tester: "center-line full toss the batter missed went for a BACK boundary —
        // 4 byes"): the keeper only arms at the FIRST BOUNCE, and a full toss's first bounce is AT/BEHIND the
        // keeper line (its landing point IS the aim spot, z 8.8-12) — so on a missed full toss the whole keeper
        // state machine stayed dormant (WicketKeeperPreBattingActions early-returns while unarmed) and the ball
        // sailed to the rope for byes. Arm the keeper the moment a no-bounce full toss crosses the stump plane
        // (z≈10.04), mirroring the bounce-arm below: position-derived, so both clients' identical deterministic
        // sims arm on the same tick — no RPC needed. wkOppositeLength is then computed while the ball still
        // APPROACHES the keeper, so the existing velocity-window catch clause can actually collect it.
        if (isFullToss && !isWicketKeeperActive && !isBallHit && bounceCount == 0
            && currentBallStatus == "bowling" && !isReplayModeActive
            && temporaryPosition.z > 10.6f)   // PAST the batsman (was 10.05 = stump plane): arming while the ball was still hittable raced the shot relay — a HIT full toss could enter keeper catchAttempt on the follower (isBallPaused froze the shot view; tester: "full toss hits ka bowler side view nahi aata"). 10.6 is behind the bat, so only a genuinely missed full toss arms the keeper.
        {
            isWicketKeeperCatchingAnimationSelected = false;
            ActivateWicketKeeper();
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                ConstantsData_M.MpLog($"[KeeperArm] FULL-TOSS stump-plane arm wkOpp={wkOppositeLength:F3} ballZ={temporaryPosition.z:F2} ballY={matchBallTransform.position.y:F2} side={(CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex ? "BOWL" : "BAT")}");
        }



        if (!isBowlingInterfaceHidden && bowlingInterfaceHideSpot.transform.position.z < matchBallTransform.position.z)
        {
            if (Singleton<GameData>.instance != null)
            {
                Singleton<GameData>.instance.ShowBowlingInterface(boolean: false);
            }
            isBowlingInterfaceHidden = true;
        }
        if (!isWideBallChecked)
        {
            isWideBall();
        }
        // Runaway-bounce freeze fix: a corrupted/wild delivery that rolls along the ground (arcHeight
        // shrinks to ~0 each bounce) kept re-crossing launchAngle>=360 EVERY frame, so bounceCount ran
        // into the thousands and the bowling authority spammed BounceSync (CmdSyncBallTrajectory) every
        // frame → the match froze. A real ball bounces only a handful of times. Once the count is clearly
        // past any real ball, bring the ball to rest and pin angleChangeRate=0 so launchAngle stops
        // re-crossing 360 — the bounce loop terminates instead of spinning forever. The rested ball can
        // then be collected by a fielder / resolved normally; the spam + freeze are gone either way.
        if (launchAngle >= 360f && bounceCount > 8)
        {
            launchAngle = 180f;
            angleChangeRate = 0f;
            horizontalVelocity = 0f;
            arcHeight = 0f;
            ConstantsData_M.MpLog($"[BounceRunawayGuard] Capped runaway bounce (count={bounceCount}) — resting ball at {matchBallTransform.position}.");
        }
        if (launchAngle >= 360f)
        {
            // BOUNCE SYNC FIX: in live MP the BATTING client is a follower — the bowling
            // client is authoritative for every bounce (direction/spin/keeper). The batting
            // client still wraps launchAngle locally (so the arc doesn't dip below ground
            // between packets) but must NOT run the gameplay-affecting bounce transition
            // (spin turn, keeper activation, bounceCount), because RpcSyncBallTrajectory —
            // sent at EACH authoritative bounce below — applies all of that. Otherwise a
            // 50 Hz launchAngle snapshot from RpcSyncBallPosition could skip the local
            // bounce (355 -> 180) so the turn/keeper never fired => "ball goes straight",
            // or it fired at a drifted position => bounce on one side only.
            // Stage 2 (ping-stable delivery rework): when useLocalDeliverySim is ON the batting
            // follower computes the bounce LOCALLY (bounceCount, spin turn _ballAngle += spinFactor,
            // ActivateWicketKeeper, isBallInline) from the deterministic release params instead of
            // waiting for RpcSyncBallTrajectory — so the bounce is ping-independent. The bowling
            // client still SENDS RpcSyncBallTrajectory (gated by myTeamIndex==BowlingTeamIndex below,
            // so the follower never sends it), which arrives as a per-bounce CORRECTION on top of the
            // local bounce. Flag OFF (default) = unchanged stream-authoritative behavior.
            bool isLiveMpBattingFollower = GameConstants.isWithAI == false
                && CONTROLLER.PlayModeSelected == 8
                && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
                && !isReplayModeActive
                && !ConstantsData_M.useLocalDeliverySim;

            // Visual-prediction state runs on BOTH clients: wrap launchAngle so the arc
            // doesn't dip below ground, and shrink the arc/rate as a real bounce would.
            // These are idempotent with the authoritative trajectory RPC (which overwrites
            // them to the bowling client's exact post-bounce values).
            launchAngle = 180f;
            angleChangeRate *= 1.1f;
            arcHeight *= 0.6f;
            // Gameplay-affecting bounce state (bounceCount + spin turn + keeper + inline
            // flag) is OWNED by the bowling client. The batting follower no longer derives
            // these locally because a 20 Hz launchAngle snapshot can skip its local 360
            // crossing (=> turn/keeper never fire => "ball goes straight" / bounce on one
            // side only). RPC_SyncBallTrajectory below (sent at EVERY authoritative bounce)
            // applies bounceCount, the spin-turned _ballAngle, keeper and isBallInline.
            if (!isLiveMpBattingFollower)
            {
                bounceCount++;
            }
            if (!isReplayModeActive)
            {
                ballSpinSpeedZ = UnityEngine.Random.Range(-3600, -1800);
                firstBounceBallSpinSpeedZSaved = ballSpinSpeedZ;
            }
            else
            {
                ballSpinSpeedZ = firstBounceBallSpinSpeedZSaved;
            }
            if (!isLiveMpBattingFollower && bounceCount == 1)
            {
                if (bowlerType == "spin")
                {
                    _ballAngle += spinFactor;
                }
                else if (bowlerType == "fast" && swingIntensity != 0f)
                {
                    _ballAngle += spinFactor;
                    swingIntensity = 0f;
                }
                else if (bowlerType == "medium" && swingIntensity != 0f)
                {
                    _ballAngle += spinFactor;
                    swingIntensity = 0f;
                }
                // Authoritative keeper re-arm on the BOWLING side (mirror of the batting-gated re-arm in
                // RPC_SyncBallTrajectory ~12376). The bowling client never receives its own BounceSync RPC, so
                // its keeper has no second pass to recover a missed catch window — clear the latch + status
                // (unless already mid-take) so WicketKeeperPreBattingActions re-enters catchAttempt cleanly
                // against the freshly spin-turned _ballAngle + authoritative bounce geometry.
                if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                    && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
                {
                    isWicketKeeperCatchingAnimationSelected = false;
                    if (currentWicketKeeperStatus != "catchEnd"
                        && currentWicketKeeperStatus != "stumpingAttempt"
                        && currentWicketKeeperStatus != "stumpingAppeal")
                    {
                        currentWicketKeeperStatus = string.Empty;
                    }
                }
                ActivateWicketKeeper();
                if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                {
                    ConstantsData_M.MpLog($"[KeeperArm] first bounce wkOpp={wkOppositeLength:F3} ballZ={temporaryPosition.z:F2} keeperZ={_wicketKeeperTransform.position.z:F2} ballAngle={_ballAngle:F1} side={(CONTROLLER.myTeamIndex==CONTROLLER.BowlingTeamIndex?"BOWL":"BAT")}");
                }
                if (Mathf.Abs(matchBallTransform.position.x) <= 0.1f)
                {
                    isBallInline = true;
                }
            }

            // Multiplayer: bowling player is authoritative for delivery trajectory.
            // Broadcast the exact parametric state at EVERY bounce (not just the first) so
            // the batting player applies the authoritative bounce — including 2nd/3rd
            // bounces of short deliveries, which were previously never synced. The batting
            // side reads bounceCount from this so its later logic doesn't think the ball
            // never bounced.
            if (!isLiveMpBattingFollower
                && GameConstants.isWithAI == false && CONTROLLER.PlayModeSelected == 8
                && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
                && CricketNetworkManager.instance != null && !isReplayModeActive)
            {
                CricketNetworkManager.instance.CmdSyncBallTrajectory(
                    staticVariables.UserProfiledata.user._id,
                    matchBallTransform.position,
                    temporaryPosition,
                    _ballAngle,
                    launchAngle,       // 180f after reset above
                    arcHeight,         // *= 0.6f after reset above
                    angleChangeRate,   // *= 1.1f after reset above
                    horizontalVelocity,
                    bounceCount);
                ConstantsData_M.MpLog($"[GroundController][BounceSync] Sent trajectory to batting player. pos={matchBallTransform.position} angle={_ballAngle} bounce={bounceCount}");
            }
        }
        if (Singleton<DRS>.instance.DRSreplay && ballRayCastConnectionZPositionSaved != 0f && raycastAnchorBallTransform.position.z > ballRayCastConnectionZPositionSaved - 10f)
        {
            replayViewCamera.enabled = false;
            Singleton<DRSCameraScript>.instance.FixAtStump2Crease();
            SetDRSReplayUI();
        }
        if (Singleton<DRS>.instance.DRSreplay && ballRayCastConnectionZPositionSaved != 0f && raycastAnchorBallTransform.position.z > ballRayCastConnectionZPositionSaved)
        {
            Singleton<DRSCameraScript>.instance.DisableAllRenderers();
        }
        if (bounceCount > 0 && remainingDRSChances != 1 && isPitchOutsideLegForDRS)
        {
            Singleton<DRSCameraScript>.instance.MoveCameraToTopOfPitch();
        }
        if (Singleton<DRS>.instance.DRSreplay && remainingDRSChances > 2 && !isPitchOutsideLegForDRS)
        {
            if (isHittingDetected)
            {
                Singleton<DRSCameraScript>.instance.HitStump(0f);
            }
            else
            {
                Singleton<DRSCameraScript>.instance.HitStump(0f);
            }
        }
    }

    public void isWideBall()
    {
        //if (!(stump1Crease.transform.position.z < ballTransform.position.z))
        if (!(stumpLeftCrease.transform.position.z < temporaryPosition.z))
        {
            return;
        }
        isWideBallChecked = true;
        Vector3 vector = Vector3.zero;
        Vector3 vector2 = Vector3.zero;
        if (batsmanHand == "right")
        {
            if (!isFullToss)
            {
                if (_batsmanTransform.position.x >= 0f)
                {
                    vector2 = rightHandedBatsmanMinWideLimit.transform.position;
                    vector = getMaxMinPoint();
                }
                else if (_batsmanTransform.position.x < 0f)
                {
                    vector2 = rightHandedBatsmanMinWideLimit.transform.position;
                    vector = stumpLeftCrease.transform.position;
                    vector += new Vector3(0.18f, 0f, 0f);
                }
            }
            else if (_batsmanTransform.position.x >= 0f)
            {
                vector2 = rightHandedBatsmanMinWideLimit.transform.position;
                vector = getMaxMinPoint();
            }
            else if (_batsmanTransform.position.x < 0f)
            {
                vector2 = rightHandedBatsmanMinWideLimit.transform.position;
                vector = stumpLeftCrease.transform.position;
                vector += new Vector3(0.18f, 0f, 0f);
            }
        }
        else if (batsmanHand == "left")
        {
            if (!isFullToss)
            {
                if (_batsmanTransform.position.x >= 0f)
                {
                    vector2 = stumpLeftCrease.transform.position;
                    vector = leftHandedBatsmanMaxWideLimit.transform.position;
                    vector2 -= new Vector3(0.18f, 0f, 0f);
                }
                else if (_batsmanTransform.position.x < 0f)
                {
                    vector2 = getMaxMinPoint();
                    vector = leftHandedBatsmanMaxWideLimit.transform.position;
                }
            }
            else if (_batsmanTransform.position.x >= 0f)
            {
                vector2 = stumpLeftCrease.transform.position;
                vector = leftHandedBatsmanMaxWideLimit.transform.position;
                vector2 -= new Vector3(0.18f, 0f, 0f);
            }
            else if (_batsmanTransform.position.x < 0f)
            {
                vector2 = getMaxMinPoint();
                vector = leftHandedBatsmanMaxWideLimit.transform.position;
            }
        }
        //if (!(ballTransform.position.x < vector.x) || !(ballTransform.position.x > vector2.x))
        if (!(temporaryPosition.x < vector.x) || !(temporaryPosition.x > vector2.x))
        {
            if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
            {
                isBallWide = true;
                StartCoroutine(CallRPCWideBall());
            }
            else if (CONTROLLER.PlayModeSelected != 8)
            {
                isBallWide = true;
            }
        }
        //if (CONTROLLER.PlayModeSelected == 6)
        //{
        //	wideBall = false;
        //}
    }

    //Photon Removal  [PunRPC]
    public void RPC_WideBall()
    {
        if (isBallWide == false)
        {
            isBallWide = true;
        }
        else
        {
            return;
        }
    }

    public bool IsWide()
    {
        return isBallWide;
    }

    private void BallSwingMovement()
    {
        if (swingIntensity != 0f)
        {
            // Deterministic delivery (Phase 2): swing is part of the delivery's lateral integration, so it
            // must advance by the same fixed step as the rest of the ball during a deterministic delivery
            // flight (else the swing curve diverges per-frame across clients). BallDt() == Time.deltaTime
            // until a deterministic delivery is armed, so this is a no-op for the legacy / post-shot paths.
            float x = Mathf.Cos(swingAngle * degToRad) * swingIntensity * BallDt();
            swingAngle += swingAnglePerSecond * BallDt();
            matchBallTransform.position -= new Vector3(x, 0f, 0f);
            temporaryPosition -= new Vector3(x, 0f, 0f);
        }
    }

    public void UpdateBallShadow()
    {
        if (BallSkinRenderer.enabled)
        {
            shadowObjects[15].SetActive(true);
            shadowTransforms[15].position = new Vector3(shadowReferenceTransforms[15].position.x, 0f, shadowReferenceTransforms[15].position.z);
        }
        else if (enableShadows)
        {
            shadowObjects[15].SetActive(false);
        }
    }

    public void BallAngle(float angle)
    {
        shotAngleValue = angle;
    }

    //Photon Removal [PunRPC]
    public void RPC_ChangeBallAngle(float BallAngle, float HorizontalSpeed, float BallProjectileAngle, float BallProjectileHeight
        , float BallTimingFirstBounceDistance, float BallProjectileAnglePerSecond, float BallBatMeetingHeight, float HorizontalSpeedMultiplier
        , string Ballstatus, bool EdgeCatch, Vector3 ContactPos)
    {
        if (LockstepActive)
        {
            // 8-ball-style value relay: stash the batter's RESOLVED contact numbers; the follower applies
            // them at ITS (delayed) crossing instead of trusting the seeded local recompute. Non-shot relays
            // (pads) stay ignored — the seeded deflection paths own those. Too-late relays (contact already
            // resolved locally from the fallback) are dropped — RpcCorrectBallState remains the last resort.
            if (Ballstatus == "shotSuccess" && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
            {
                if (_lockstepContactResolved)
                {
                    ConstantsData_M.MpLog("[Lockstep] Batter's contact result arrived AFTER the local resolve — dropped (fallback numbers stay).");
                    return;
                }
                _lockstepRelayedAngle = BallAngle;
                _lockstepRelayedHVel = HorizontalSpeed;
                _lockstepRelayedLaunch = BallProjectileAngle;
                _lockstepRelayedArc = BallProjectileHeight;
                _lockstepRelayedFbd = BallTimingFirstBounceDistance;
                _lockstepRelayedAcr = BallProjectileAnglePerSecond;
                _lockstepRelayedBch = BallBatMeetingHeight;
                _lockstepRelayedHAdj = HorizontalSpeedMultiplier;
                _lockstepRelayedEdge = EdgeCatch;
                _lockstepHasRelayedResult = true;
                ConstantsData_M.MpLog($"[Lockstep] Batter's contact result stashed: angle={BallAngle:F1} hVel={HorizontalSpeed:F1} fbd={BallTimingFirstBounceDistance:F1} — applies at the crossing.");
                return;
            }
            // A NON-bat ruling from the batting authority must NOT be ignored. Only "shotSuccess" was
            // handled above; everything else — "onPads" above all — fell into the blanket ignore below and
            // the follower kept its own resolve. Worse, nothing recorded that the batter had ruled at all,
            // so LockstepTryResolveContact sat out its whole relay window, logged "expired without the
            // batter's numbers", and resolved a BAT CONTACT from the fallback. That is the tester's
            // "batsman side batsman ke foot pe ball lagi aur bowler side back ki boundary ho gayi": the
            // batting screen played a pad, the bowling screen played a shot off the same delivery.
            // Record the ruling so the resolve stops waiting and takes the no-bat-contact path instead.
            if (CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex && !_lockstepContactResolved)
            {
                // Take the batter's NUMBERS too, not just the verdict. Cancelling the bat resolve alone
                // fixed the score — the 06-08 16:56 pair agrees on valid=1 runs=0 boundary=False — but the
                // ball itself still diverged: with nothing applied, this side's deterministic sim carried
                // the delivery straight on past the batsman and out to the rope, while the batting screen
                // had it stop dead off the pad. That is the second half of "batsman side foot pe lagi,
                // bowler side back ki boundary". A pad deflection changes the ball exactly the way a bat
                // contact does, so it is stashed exactly the same way and applied at the same crossing.
                _lockstepRelayedAngle = BallAngle;
                _lockstepRelayedHVel = HorizontalSpeed;
                _lockstepRelayedLaunch = BallProjectileAngle;
                _lockstepRelayedArc = BallProjectileHeight;
                _lockstepRelayedFbd = BallTimingFirstBounceDistance;
                _lockstepRelayedAcr = BallProjectileAnglePerSecond;
                _lockstepRelayedBch = BallBatMeetingHeight;
                _lockstepRelayedHAdj = HorizontalSpeedMultiplier;
                _lockstepRelayedEdge = EdgeCatch;
                _lockstepHasRelayedResult = true;
                _lockstepRelayedNoBatContact = true;
                ConstantsData_M.MpLog($"[Lockstep][ContactRuling] Batter ruled '{Ballstatus}' — no bat contact. Adopting its ball state (angle={BallAngle:F1} hVel={HorizontalSpeed:F1} fbd={BallTimingFirstBounceDistance:F1}) and cancelling this side's bat resolve.");
                return;
            }
            ConstantsData_M.MpLog("[Lockstep] RPC_ChangeBallAngle ignored — the local deterministic resolve owns the contact.");
            return;
        }

        bool nothingChanged = CONTROLLER.BALLANGLE == BallAngle &&
        CONTROLLER.HORIZONTALSPEED == HorizontalSpeed &&
        CONTROLLER.BALLPROJECTILEANGLE == BallProjectileAngle &&
        CONTROLLER.BALLPROJECTILEHEIGHT == BallProjectileHeight &&
        CONTROLLER.BALLTIMINGFIRSTBOUNCEDISTANCE == BallTimingFirstBounceDistance &&
        CONTROLLER.BALLPROJECTILEANGLEPERSECOND == BallProjectileAnglePerSecond &&
        CONTROLLER.BALLBATMEETINGHEIGHT == BallBatMeetingHeight &&
        CONTROLLER.HORIZONTALSPEEDMULTIPLIER == HorizontalSpeedMultiplier &&
        CONTROLLER.BALLSTATUS == Ballstatus && CONTROLLER.EDGECATCH == EdgeCatch;

        // A bat shot relays Ballstatus=="shotSuccess"; a pad relay relays "onPads".
        bool isBatShotRelay = Ballstatus == "shotSuccess";

        // Original late-RPC rejection. KEPT for non-bat relays (pads) so their exact timing/behaviour is
        // preserved byte-for-byte, but BYPASSED for bat shots: a genuine bat contact is detected at the very
        // end of the batting client's (lerp-lagged) flight, so this relay frequently arrives after the
        // bowling ball has already passed the contact z; the early-return used to drop the whole shot and
        // the bowling client never launched it ("shot connects on batting, miss on bowling").
        if (!isBatShotRelay && ballRayCastConnectionZPositionSaved - raycastAnchorBallTransform.position.z < 0.25f)
        {
            // ...with ONE exception: a late non-bat ruling that CONTRADICTS a bat contact this side has
            // already taken on must not be thrown away. CmdSetMultiplayerCollider is fired speculatively
            // from the batting raycast while the ball is merely NEAR the bat, so the bowling client can be
            // seeded with "BatCollider" and launch a shot before the batting client has decided what the
            // ball actually hit. When the real contact turns out to be the pad, this relay carries the
            // correction — and dropping it here left the two screens playing different balls: the tester's
            // "batsman side batsman ke foot ko ball lagi aur bowler pakadne gaya, jabke bowler side batsman
            // ne straight shot hit ki". Same precedence the edge ruling already uses: the authority's final
            // ruling outranks the speculative contact relay. A merely-corroborating late relay still drops
            // exactly as before, so the timing this gate protects is unchanged for every other case.
            bool _contradictsLocalBatContact =
                CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
                && (didBallHitBat || CONTROLLER.CURRENTCOLLIDER == "BatCollider" || CONTROLLER.CURRENTCOLLIDER == "BatCollider2");
            if (!_contradictsLocalBatContact)
            {
                return;
            }
            ConstantsData_M.MpLog($"[GroundController][ContactRuling] Authority ruled '{Ballstatus}' after this side was already on a bat contact (collider={CONTROLLER.CURRENTCOLLIDER} hitBat={didBallHitBat}) — accepting the late ruling instead of dropping it.");
        }

        //ConstantsData_M.MpLog("ANGLE : " + BallAngle + " HOrizontal Speed : " + HorizontalSpeed);
        //ballAngle = BallAngle;
        CONTROLLER.BALLANGLE = BallAngle;
        CONTROLLER.HORIZONTALSPEED = HorizontalSpeed;
        CONTROLLER.BALLPROJECTILEANGLE = BallProjectileAngle;
        CONTROLLER.BALLPROJECTILEHEIGHT = BallProjectileHeight;
        CONTROLLER.BALLTIMINGFIRSTBOUNCEDISTANCE = BallTimingFirstBounceDistance;
        CONTROLLER.BALLPROJECTILEANGLEPERSECOND = BallProjectileAnglePerSecond;
        CONTROLLER.BALLBATMEETINGHEIGHT = BallBatMeetingHeight;
        CONTROLLER.HORIZONTALSPEEDMULTIPLIER = HorizontalSpeedMultiplier;
        CONTROLLER.BALLSTATUS = Ballstatus;
        CONTROLLER.EDGECATCH = EdgeCatch;
        isEdgeCaught = EdgeCatch;
        if (ContactPos != Vector3.zero)
        {
            _remoteContactPos = ContactPos;
            _hasRemoteContactPos = true;
        }

        // Bowling-side "shot connects on batting, ball rolls to the keeper on bowling" fix (mostly spin):
        // applying a relayed bat shot in TryApplyRemoteBatContact requires CURRENTCOLLIDER=="BatCollider",
        // which is set by the SEPARATE CmdSetMultiplayerCollider relay. That collider relay is frequently
        // missing/late on the bowling client (the batting raycast that fires it doesn't always register for
        // the slower, turning spin ball), so TryApplyRemoteBatContact kept bailing and the local delivery
        // sim carried the ball through to the keeper — while the batting screen played the shot (four). The
        // angle relay's Ballstatus=="shotSuccess" IS the authoritative bat-contact signal, so seed
        // CURRENTCOLLIDER from it here: the angle relay (which already sets BALLANGLE/HORIZONTALSPEED above)
        // becomes self-sufficient to launch the shot, with no dependency on the separate collider relay.
        if (isBatShotRelay && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && currentBallStatus == "bowling"
            && CONTROLLER.CURRENTCOLLIDER != "BatCollider" && CONTROLLER.CURRENTCOLLIDER != "BatCollider2")
        {
            CONTROLLER.CURRENTCOLLIDER = "BatCollider";
        }

        // Authority says this ball did NOT come off the bat, so retire the speculative bat seed before
        // TryApplyRemoteBatContact gets a chance to act on it. CmdSetMultiplayerCollider fires from the
        // batting raycast while the ball is only NEAR the bat; without this, that earlier guess outlives
        // the later, actual ruling and the bowling side plays a shot off a ball the batting side saw hit
        // the pad. Bowling side only — the batting client owns the ruling and must not clear its own state.
        if (!isBatShotRelay && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && (CONTROLLER.CURRENTCOLLIDER == "BatCollider" || CONTROLLER.CURRENTCOLLIDER == "BatCollider2"))
        {
            ConstantsData_M.MpLog($"[GroundController][ContactRuling] Clearing speculative '{CONTROLLER.CURRENTCOLLIDER}' seed — authority ruled '{Ballstatus}'.");
            CONTROLLER.CURRENTCOLLIDER = string.Empty;
        }

        // Apply a relayed BAT contact immediately + idempotently (snaps the launch origin to the batting
        // contact point and runs OnCustomTriggerEnter, which itself sets currentBallStatus = "shotSuccess").
        bool appliedBat = TryApplyRemoteBatContact();

        if (!appliedBat && !isBatShotRelay)
        {
            // Non-bat status (pad "onPads"): already past the 0.25f gate above, so propagate it — same as
            // the original code. For a still-pending bat shot (appliedBat==false because the collider relay
            // hasn't arrived yet) we deliberately leave currentBallStatus=="bowling" so TryApplyRemoteBatContact
            // can still fire when RPC_SetMultiplayerCollider("BatCollider") lands next.
            currentBallStatus = Ballstatus;
        }

        // NOTE: currentBallStatus + isEdgeCaught are now set above (isEdgeCaught unconditionally;
        // currentBallStatus either via TryApplyRemoteBatContact -> OnCustomTriggerEnter("shotSuccess")
        // for a bat shot, or via the gated fallback for other statuses). The old unconditional
        // "currentBallStatus = Ballstatus" here was removed because it overrode that logic and
        // re-introduced the "shotSuccess stamped before the bat trigger" race.

        // FIELDER FIX (counterpart of the keeper drift fix): on the BOWLING client,
        // the bat-vs-ball OnTriggerEnter often fires locally BEFORE this RPC arrives
        // (network latency vs local physics). When that happens BallTiming() runs
        // with the stale pre-hit _ballAngle (= bowler's release angle), so the
        // following SetActiveFielders() picks fielders along the wrong trajectory
        // — frequently leaving targetFielderForCatch = null and letting the ball
        // sail past for a boundary even though the shot was catchable.
        //
        // Now that CONTROLLER.BALLANGLE holds the authoritative shot angle, push it
        // into the local _ballAngle on the bowling client and, if bat-contact has
        // already been registered locally, re-run the fielder selection so the
        // correct active fielder/targetFielderForCatch is picked from the corrected
        // angle. Same defensive pattern used for the keeper in RPC_SyncBallTrajectory.
        if (!nothingChanged
            && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && didBallHitBat)
        {
            _ballAngle = BallAngle;
            _contactAngleRelayedThisDelivery = true;   // #1: lock in the authoritative bat-contact angle so the 20Hz RPC_SyncBallShot stream can't clobber it with a stale pre-contact value (behind-shot ball-clipping)
            // Catch-point sync ("fielder sprints the wrong way on the bowling side" fix): the target the
            // catcher runs to (ballCatchingPoint, set in FixBallCatchingSpot) is computed from _ballAngle AND
            // firstBounceDistance. firstBounceDistance is chosen with UnityEngine.Random.Range PER CLIENT, so
            // the bowling follower's local value diverges from the batting authority's — and when its random
            // value is small, (firstBounceDistance - preCatchDistance) goes NEGATIVE and the catch point FLIPS
            // to the wrong side (toward the pitch), so the catcher runs the wrong way (intermittently, by how
            // far the two randoms diverge). Adopt the authoritative bounce distance (relayed in this RPC) and
            // recompute the catch point from a clean preCatchDistance so the fielder runs to the SAME spot the
            // batting screen shows. (_ballAngle is already synced above; the ball flight itself is unaffected —
            // it's deterministic / position-streamed — this only corrects the fielder's local prediction.)
            firstBounceDistance = BallTimingFirstBounceDistance;
            preCatchDistance = 1f;
            FixBallCatchingSpot();
            GetFieldersAngle();
            GetFieldersDistance();
            if (!stopKeeper)
            {
                SetActiveFielders();
            }
            ConstantsData_M.MpLog($"[GroundController][RPC_ChangeBallAngle] Re-activated fielders on bowling client with authoritative _ballAngle={_ballAngle:F2} firstBounceDistance={firstBounceDistance:F1}");
        }
        ////ConstantsData_M.MpLog("RPC AFTER BALLANGLE : " + BallAngle);
    }

    /// <summary>
    /// Called on the BATTING player when the bowling player broadcasts their trajectory state
    /// at the first bounce. Overwrites the batting player's local parametric simulation state
    /// so both clients see the ball arrive at the batsman from exactly the same position and angle.
    /// </summary>
    // No-ball was decided on the bowling side's meter (StartBowling) and NEVER relayed — the batter's
    // outcome authority then committed e.g. an LBW as OUT on a no-ball and showed no free-hit. Mirror the
    // exact post-StartBowling flag state the bowling side has.
    private void ApplyRelayedOverstep(bool overstepped)
    {
        if (!overstepped || isOversteppedDelivery) return;
        isOversteppedDelivery = true;
        isFreeHitActive = true;
        freeHit = true;
        ConstantsData_M.MpLog("[GroundController][RPC_SyncBallRelease] NO-BALL relayed — overstep flags mirrored on this client.");
    }

    public void RPC_SyncBallTrajectory(Vector3 ballPos, Vector3 tempPos, float ballAngle,
        float curLaunchAngle, float curArcHeight, float curAngleChangeRate, float curHVelocity,
        int curBounceCount)
    {
        if (!isBallReleased)
        {
            ConstantsData_M.MpLog("[GroundController][RPC_SyncBallTrajectory] Ball not released yet locally — skipping.");
            return;
        }
        if (isReplayModeActive)
        {
            return; // never correct positions during replay
        }
        // Lockstep: the deterministic local sim OWNS the trajectory on both clients — this legacy bounce
        // correction is not needed, and with the bowling side's flight running lockstepFollowerFlightDelay
        // BEHIND, its (delayed) first-bounce relay lands AFTER the batter's contact and yanked the freshly
        // hit ball back onto the delivery path (batter's shot destroyed, camera had nothing to follow —
        // test logs 12-07: "Trajectory corrected. pos z=5.91 angle=86.4" right after "Contact resolved").
        if (LockstepActive)
        {
            ConstantsData_M.MpLog("[Lockstep] RPC_SyncBallTrajectory ignored — the deterministic sim owns the trajectory.");
            return;
        }
        // Legacy path safety: never let a late bounce correction overwrite an already-hit ball.
        if (isBallHit || currentBallStatus == "shotSuccess")
        {
            ConstantsData_M.MpLog("[GroundController][RPC_SyncBallTrajectory] Ball already hit — stale delivery correction dropped.");
            return;
        }

        // Apply bowling player's authoritative state, replacing any local drift
        matchBallTransform.position = ballPos;
        temporaryPosition            = tempPos;
        _ballAngle                   = ballAngle;
        launchAngle                  = curLaunchAngle;
        arcHeight                    = curArcHeight;
        angleChangeRate              = curAngleChangeRate;
        horizontalVelocity           = curHVelocity;

        // Authoritative bounce count. The batting follower no longer increments bounceCount
        // locally (see BowlingBallMovement), so this is the single source of truth — without
        // it, later logic gated on bounceCount (>=1 / ==0) would believe the ball never
        // bounced. Idempotent: only advance, never go backwards.
        // Spin-keeper-stuck fix (#6, local-sim): with useLocalDeliverySim ON the batting follower
        // already advanced bounceCount LOCALLY (Stage 2), so "curBounceCount > bounceCount" is false
        // on this authoritative first-bounce correction → the keeper geometry was NOT recomputed
        // against the just-overwritten authoritative _ballAngle/temporaryPosition → the close spin
        // keeper got stranded in catchAttempt and the delivery never completed (no UpdateCurrentBall
        // → no ACK → pitch frozen). Detect the first bounce by curBounceCount==1 itself when local-
        // sim is on, so the keeper is re-activated against the authoritative trajectory.
        bool isFirstBounceNow = curBounceCount == 1
            && (curBounceCount > bounceCount || ConstantsData_M.useLocalDeliverySim);
        if (curBounceCount > bounceCount)
        {
            bounceCount = curBounceCount;
        }

        // KEEPER FIX (batting-side miss bug): on the batting client, the local
        // launchAngle >= 360 first-bounce trigger frequently fires AFTER the
        // ball has already drifted well past the keeper (e.g. z=26 instead of
        // z=4 — confirmed via [KEEPER-DBG] logs). That meant ActivateWicketKeeper
        // ran with a ball position past the catching zone, the catch animation
        // was selected against the wrong geometry, and catchAttempt never
        // triggered → every "no shot" delivery sailed past for a 6.
        //
        // Since the bowling client is authoritative for the bounce moment AND
        // we just overwrote position/launchAngle/etc. to its values, re-run the
        // keeper activation here so wkOppositeLength / catch animation selection
        // happen against the correct (authoritative) bounce-instant geometry.
        // We also clear the catch-animation latch and reset the keeper status if
        // it didn't already advance, so the per-frame catch trigger in
        // WicketKeeperPreBattingActions can engage cleanly from the corrected
        // state.
        if (isFirstBounceNow
            && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
        {
            // Authoritative "ball pitched in line" flag (the batting follower no longer
            // computes this at its local bounce — see BowlingBallMovement). Derived from the
            // bowling client's exact bounce position so LBW/DRS logic (e.g. the PlayMode 8
            // batting branch in CanAIAssessDRS) behaves identically on both clients.
            if (Mathf.Abs(ballPos.x) <= 0.1f)
            {
                isBallInline = true;
            }

            isWicketKeeperCatchingAnimationSelected = false;
            if (currentWicketKeeperStatus != "catchEnd"
                && currentWicketKeeperStatus != "stumpingAttempt"
                && currentWicketKeeperStatus != "stumpingAppeal")
            {
                currentWicketKeeperStatus = string.Empty;
            }
            ActivateWicketKeeper();
            ConstantsData_M.MpLog($"[GroundController][RPC_SyncBallTrajectory] Re-activated keeper on batting client (first bounce). wkOppositeLength={wkOppositeLength:F3}");
        }

        ConstantsData_M.MpLog($"[GroundController][RPC_SyncBallTrajectory] Trajectory corrected. pos={ballPos} angle={ballAngle} launchAngle={curLaunchAngle}");
    }

    /// <summary>
    /// Called on the batting player at ball release moment.
    /// Overwrites all parametric physics state with the bowling player's authoritative values,
    /// then applies a latency-compensation position advance so the ball appears where it
    /// actually is NOW (accounting for one-way network delay = RTT/2).
    /// This is Phase 1 of the three-phase ball sync strategy and eliminates the primary
    /// divergence caused by a stale bowlingSpot on the batting client.
    /// </summary>
    public void RPC_SyncBallRelease(Vector3 ballPos, Vector3 tempPos,
        float ballAngle, float curLaunchAngle, float curArcHeight,
        float curAngleChangeRate, float curHVelocity,
        float curSpinFactor, float curSwingIntensity, bool curIsFullToss, double startNetTime,
        float spotLength = 0f, bool overstepped = false, int fieldIdx = -1, bool fieldRestriction = false,
        int bowlerIdx = -1)
    {
        if (isReplayModeActive) return;

        // Stale-delivery guard (#1/#6): a delayed RPC_SyncBallRelease from the PREVIOUS ball can
        // arrive during the next delivery's pre-release window, flip the release flags, and make the
        // CURRENT ball's real release get dropped by the duplicate guard below (ball stuck in hand) /
        // show the ball on the batting side with no delivery. The real release always arrives while
        // we are in the bowler run-up phase (currentActionState==2 — RpcBowlerWaiting sets 2 before
        // the run-up, the real release sets 3), so reject anything outside that phase. PARTIAL: a
        // stale packet that lands during the NEXT run-up still slips through; the complete fix is a
        // per-delivery token on the release RPC.
        if (currentActionState != 2) return;

        // Bug-2 guard: ignore a duplicate release sync for the same delivery (duplicate RPC at
        // high ping). Without this the batting client re-ran the whole release → ball thrown twice.
        if (ballReleaseProcessedThisDelivery)
        {
            // MpLog as well as the warning: dropping a release here means this client gets NO BALL for the
            // delivery, and it is invisible in a bug report — the reporter keeps only [Log]/[Error], so a
            // Debug.LogWarning never survives the capture. Costing a whole session to infer this drop from
            // surrounding state is what earned it a line in the file we actually receive. It fires only on a
            // genuine duplicate, so it adds no volume in normal play.
            ConstantsData_M.MpLog($"[GroundController] Duplicate RPC_SyncBallRelease DROPPED — no ball this delivery on this client (outcomeCommitted={_outcomeCommittedThisDelivery} released={isBallReleased} actionState={currentActionState}).");
            Debug.LogWarning("[GroundController] Duplicate RPC_SyncBallRelease ignored (already released this delivery).");
            return;
        }
        ballReleaseProcessedThisDelivery = true;

        // FIELD-PLACEMENT RE-ASSERT (caught-vs-four, 13-07): placement = f(fielderChangeIndex,
        // isFieldRestrictionActive, batsmanHand) but only the index was relayed — edge-triggered and
        // droppable, with SEVEN local un-relayed index resets — so the two clients could stand different
        // fields for the same ball (batter's fielder caught what flew over the follower's fielder). The
        // bowling authority now re-asserts its placement state on EVERY delivery packet; reposition FIRST,
        // preview text second (the change-time RPC did preview first and an NRE could skip the reposition).
        // NOTE: must sit ABOVE the locked-plan early return — the shipping config arrives as the plan.
        if (fieldIdx > 0 && (CONTROLLER.fielderChangeIndex != fieldIdx || isFieldRestrictionActive != fieldRestriction))
        {
            CONTROLLER.fielderChangeIndex = fieldIdx;
            isFieldRestrictionActive = fieldRestriction;   // placement flag only — CONTROLLER.PowerPlay stays locally derived
            ResetFielders();
            if (Singleton<PreviewScreen>.instance != null)
            {
                Singleton<PreviewScreen>.instance.SetFieldPreview();
            }
            ConstantsData_M.MpLog($"[GroundController][RPC_SyncBallRelease] Field placement re-asserted: idx={fieldIdx} restriction={fieldRestriction}");
        }
        // BOWLER IDENTITY RE-ASSERT (tester 13-07: after a bowling-side reconnect the bowler CHANGED on one
        // side / bowling differed per side, then the un-hit ball deflected off the keeper on one screen only —
        // keeper depth derives from bowlerType). The bowling authority indisputably knows who is bowling; on a
        // mismatch adopt its index and re-derive hand/type (+ spin sign) so this delivery's sim uses the right
        // bowler. The bowler MODEL/keeper stance may lag one ball — identity/gameplay values heal NOW.
        // ISOLATED. Nothing in this re-assert is required for the ball to leave the bowler's hand — it only
        // corrects WHO is bowling. The release itself is the code below, and whether it runs must not depend
        // on an identity lookup succeeding. One FormatException in here left this client with no ball at all
        // for the rest of the delivery while the opponent played on.
        try
        {
        if (bowlerIdx >= 0 && CONTROLLER.CurrentBowlerIndex != bowlerIdx
            && CONTROLLER.TeamList != null && CONTROLLER.BowlingTeamIndex < CONTROLLER.TeamList.Length
            && bowlerIdx < CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList.Length)
        {
            int prevIdx = CONTROLLER.CurrentBowlerIndex;
            CONTROLLER.CurrentBowlerIndex = bowlerIdx;
            BowlerInfo _bl = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerIdx].BowlerList;
            CONTROLLER.BowlerHand = (_bl.BowlingHand == "L") ? "left" : "right";
            // bowlingRank is EMPTY for any player who cannot bowl, so a bare int.Parse throws FormatException
            // — and it throws INSIDE THE BALL-RELEASE RPC, which is far worse than a wrong bowler type. The
            // 03-08 ritu_mp log catches the whole chain: "[RPC_SyncBallRelease] Field placement re-asserted"
            // immediately followed by "System.FormatException: Input string was not in a correct format ... at
            // GroundController.RPC_SyncBallRelease", and from then on every incoming trajectory correction is
            // answered with "Ball not released yet locally — skipping". The opponent bowls and plays on while
            // this client never starts the ball at all — the frozen field the tester recorded.
            // This is the same fault as the one fixed in GameData.SetGameDatas; this second copy was missed.
            if (int.TryParse(_bl.bowlingRank, out int _parsedRank))
                CONTROLLER.BowlerType = _parsedRank;
            else
                ConstantsData_M.MpLog($"[GroundController][RPC_SyncBallRelease] bowlingRank '{_bl.bowlingRank}' is not numeric for bowler {bowlerIdx} — keeping BowlerType={CONTROLLER.BowlerType} rather than dropping the release.");
            bowlerHand = CONTROLLER.BowlerHand;
            if (CONTROLLER.BowlerType == 0) bowlerType = "fast";
            else if (CONTROLLER.BowlerType == 1 || CONTROLLER.BowlerType == 2)
            {
                bowlerType = "spin";
                bowlerSpinType = CONTROLLER.BowlerType;
                if (bowlerHand == "left") bowlerSpinType = (bowlerSpinType == 1) ? 2 : 1;
            }
            else if (CONTROLLER.BowlerType == 3) bowlerType = "medium";
            ConstantsData_M.MpLog($"[GroundController][RPC_SyncBallRelease] BOWLER identity re-asserted: {prevIdx} -> {bowlerIdx} type={bowlerType} hand={bowlerHand}");
        }
        }
        catch (System.Exception _idEx)
        {
            ConstantsData_M.MpLog($"[GroundController][RPC_SyncBallRelease] Bowler identity re-assert THREW ({_idEx.GetType().Name}: {_idEx.Message}) — releasing the ball anyway with the bowler we had.");
        }

        // Overwrite parametric physics state with bowling player's authoritative values
        matchBallTransform.position = ballPos;
        temporaryPosition           = tempPos;
        _ballAngle                  = ballAngle;
        launchAngle                 = curLaunchAngle;
        arcHeight                   = curArcHeight;
        angleChangeRate             = curAngleChangeRate;
        horizontalVelocity          = curHVelocity;
        spinFactor                  = curSpinFactor;
        swingIntensity              = curSwingIntensity;
        isFullToss                  = curIsFullToss;

        // LOCKED DELIVERY PLAN (startNetTime < 0 sentinel): this is the PRE-RUN-UP plan, not a live release.
        // Params are applied above; the actual launch happens at THIS client's own bowler anim event
        // (TryLaunchFromLockedPlan) — zero release-time network dependency, so the throw and the ball are
        // perfectly synced on this screen no matter the ping.
        if (startNetTime < 0d)
        {
            // Post-reconnect first ball: the reloaded client's per-delivery setup (GameData.BowlNextBall ->
            // BowlNextBall("user","user")) never ran — the plan RPC arrives directly. BattingBy stays at the
            // scene-reload default "" and the user-shot execution gate in GetBattingInput rejects every swipe:
            // the selection relay round-trips fine but the shot never applies and the ball auto-leaves at the
            // execution Z. Re-assert the control-identity strings + hands here so the delivery is playable.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && (BattingBy != "user" || BowlingBy != "user"))
            {
                ConstantsData_M.MpLog($"[Lockstep] Control identity re-asserted at plan-arm: BattingBy='{BattingBy}' BowlingBy='{BowlingBy}' -> 'user'/'user' (per-delivery setup was skipped).");
                BattingBy = "user";
                BowlingBy = "user";
                batsmanHand = CONTROLLER.StrikerHand;
                bowlerHand = CONTROLLER.BowlerHand;
            }
            swingAnglePerSecond = 2f * angleChangeRate;
            swingAngle = 0f;
            if (spotLength > 0f) ballSpotLength = spotLength;   // tables/shot-selection input — never computed locally on the batter
            ApplyRelayedOverstep(overstepped);
            _lockedPlanBallPos = ballPos;                       // re-applied at launch
            _lockedPlanTempPos = tempPos;
            _lockedPlanArmed = true;
            // THE DELIVERY STARTS NOW, so the stall clock starts now. _deliveryWatchTimer accumulates for as
            // long as this client sits at state 1/2 with no ball released — and that span is not the run-up
            // alone, it also covers the whole wait for the bowler to actually bowl, which the auto-bowl timer
            // allows up to 10 seconds for. A bowler who takes most of his window therefore hands the batting
            // side a watchdog that is already at ~10s when the real delivery finally begins, and it fires a
            // beat later and wipes the freshly armed plan. The 01-09 13:21 pair caught the whole sequence in
            // six lines: "Delivery PLAN armed" -> "Batting run-up started" -> "Stalled pre-outcome delivery
            // (>10s)" -> "ResetAll cleared a PENDING delivery plan before it launched — this side will not
            // get that ball", then 42s idle while the bowling side had already released it. That is the
            // tester's "ik side ball hui or opponent side nhi hui", and the missed turn with it.
            // Resetting here keeps the watchdog's real job intact: a frozen run-up is still caught, now
            // measured from the moment the run-up was supposed to begin rather than from some earlier wait.
            _deliveryWatchTimer = 0f;
            ConstantsData_M.MpLog($"[Lockstep] Delivery PLAN armed: angle={ballAngle:F1} hVel={curHVelocity:F1} spotLen={ballSpotLength:F1} start={ballPos} — launching at my own bowler anim event.");
            return;
        }
        if (spotLength > 0f) ballSpotLength = spotLength;
        ApplyRelayedOverstep(overstepped);
        isBallReleased              = true;

        // Local-sim determinism (Stage 1, ping-stable delivery rework): the batting client must
        // reproduce the bowler's SWING locally from the release params. It previously kept a STALE
        // swingAnglePerSecond (derived from a bowlingSpot that can diverge on the follower), which
        // the 20Hz position stream papered over. swingAnglePerSecond is exactly 2× angleChangeRate
        // (180/L*v vs 90/L*v) and angleChangeRate is synced just above — so derive it here, with no
        // ballSpotLength dependency and no Cmd/Rpc signature change. Reset swingAngle for this ball.
        swingAnglePerSecond = 2f * angleChangeRate;
        swingAngle = 0f;

        // Game-state setup: mirrors what ReleaseTheBall() does for the bowling player.
        // The batting client skips the animation-event ReleaseTheBall() call (guarded in
        // BowlerController), so we must initialise everything it would have done here.
        currentActionState = 3;
        currentBallStatus  = "bowling";
        ballReleaseTime    = Time.time;

        // Deterministic delivery (Phase 2): base this follower's fixed-step local delivery sim on the SAME
        // release instant the bowling authority used, so both advance identical 1/60 steps from release →
        // the trajectory (swing curve, bounce point, keeper) matches bit-for-bit instead of drifting between
        // two frame-based sims. ticksDone seeded to shared elapsed so the first catch-up is ~0 (forward
        // integration, same ~one-RTT-behind feel as the existing local sim — no fast-forward jump). Gated by
        // useDeterministicDelivery (default off → unchanged frame-based local sim).
        if (ConstantsData_M.useDeterministicDelivery && ConstantsData_M.useLocalDeliverySim
            && GameConstants.isWithAI == false
            && CONTROLLER.PlayModeSelected == 8)
        {
            if (LockstepActive)
            {
                // Lockstep: a PURE local flight — anchor at MY arrival instant with tick 0, exactly like
                // the bowling authority does. The old elapsed-tick seeding skewed the tick INDICES by the
                // arrival jitter (crossing tick 57 here vs 54 there in the 12-07 logs), and the latency
                // advance below perturbed the start state (contact y 0.14 vs 0.09). Both sides now run
                // bit-identical tick-indexed flights; wall-clock start shifts by one-way latency, which the
                // follower-flight delay already dwarfs.
                _ballSimStartNetTime = Mirror.NetworkTime.time;
                _ballSimTicksDone = 0;
            }
            else
            {
                _ballSimStartNetTime = startNetTime;
                // Clamp >= 0: when the follower's Mirror.NetworkTime.time trails the authority's startNetTime by
                // the clock-sync skew (~16-33ms), the (int) cast yields -1/-2 — a permanent fixed-step deficit
                // that never re-converges. Seed the same 0 baseline the authority uses, then catch up forward.
                _ballSimTicksDone = System.Math.Max(0, (int)((Mirror.NetworkTime.time - startNetTime) / BALL_SIM_FIXED_STEP));
            }
            _ballSimMaxTargetTicks = _ballSimTicksDone;   // Issue #3: seed high-water to the already-elapsed target so the first catch-up stays forward (no fast-forward jump)
            _useDeterministicBallStep = true;
            ConstantsData_M.MpLog($"[DetDiag] Deterministic DELIVERY armed (batting follower) startNetTime={_ballSimStartNetTime:F3} seededTicks={_ballSimTicksDone}");
        }
        ActivateColliders(boolean: true);
        ShowBall(status: true);
        // Hide the bowler's in-hand ball prop. The bowling player hides it locally in
        // ReleaseTheBall() (BowlerBallSkinRenderer.enabled = false), but the batting
        // client skips ReleaseTheBall() (see comment above), so without this line the
        // batter sees BOTH the still-in-hand prop AND the flying ball simultaneously.
        if (BowlerBallSkinRenderer != null) BowlerBallSkinRenderer.enabled = false;
        if (Singleton<GameData>.instance != null && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !isReplayModeActive)
        {
            Singleton<GameData>.instance.EnableShotSelection(boolean: true);
        }

        // Latency compensation: advance the ball forward by the one-way network delay
        // so the batting player sees the ball at its current actual position rather than
        // where it was when the bowling player sent this message.
        // Lockstep: SKIPPED — it perturbs the deterministic start state (position + launchAngle nudged
        // by a per-connection latency estimate → the two sims integrate different paths). The flight
        // anchors at arrival instead (see the arm block above).
        float oneWayLatency = (float)(Mirror.NetworkTime.rtt / 2.0);
        if (!LockstepActive && oneWayLatency > 0f && oneWayLatency < 1.0f) // sanity clamp
        {
            float rad = ballAngle * Mathf.Deg2Rad;
            matchBallTransform.position += new Vector3(
                Mathf.Cos(rad) * curHVelocity * oneWayLatency,
                0f,
                Mathf.Sin(rad) * curHVelocity * oneWayLatency);
            launchAngle += curAngleChangeRate * oneWayLatency;
        }

        ConstantsData_M.MpLog($"[GroundController][RPC_SyncBallRelease] Release sync applied. pos={matchBallTransform.position} angle={ballAngle} launchAngle={launchAngle} latency={oneWayLatency:F3}s");
    }

    /// <summary>
    /// Called on the batting player every 50 ms (20 Hz) while ball is in flight.
    /// Stores the bowling player's authoritative ball position so Update() can lerp
    /// the local ball toward it each frame, eliminating all trajectory divergence.
    /// Also keeps the parametric simulation state aligned so drift doesn't re-accumulate.
    /// </summary>
    public void RPC_SyncBallPosition(Vector3 ballPos, Vector3 tempPos, float curLaunchAngle)
    {
        NoteDeliveryStreamProgress();                // G1 v2: pre-contact liveness for the BATTING staying client
        // DELIVERY (pre-shot) only. After bat contact the visible-flight authority flips to the
        // batting client and is synced via RPC_SyncBallShot (full kinematic state), so any stale
        // delivery-position packet arriving post-shot must be ignored here.
        if (isBallHit) return;
        if (isReplayModeActive) return;
        // Pre-release guard (#6): ignore the delivery-position stream until THIS ball is released.
        // A stale position packet from the previous ball would otherwise seed _networkBallPosition
        // for the next delivery → the ball appears on the batting side before any delivery is shown.
        if (!isBallReleased) return;

        // Keep the previous authoritative sample so we can estimate the authoritative X
        // at the stump plane when validating a local stump raycast hit (false-bowled guard).
        if (_hasNetworkBallPosition)
        {
            _prevNetworkBallPosition = _networkBallPosition;
            _hasPrevNetworkBallPosition = true;
        }

        // Store authoritative position. Always kept (even under local sim) because the
        // false-bowled X-estimate (AuthoritativeXAtStumpPlane) and, when the flag is OFF,
        // the Stage-3 visible lerp read _networkBallPosition — not temporaryPosition.
        _networkBallPosition = ballPos;
        _hasNetworkBallPosition = true;

        // Ball-rollback fix (#2): when useLocalDeliverySim is ON the local deterministic sim OWNS
        // temporaryPosition/launchAngle (the visible-lerp skip in the _amDeliveryMirror block already
        // defers to it). The batting client's local temporaryPosition LEADS this 20Hz authority sample
        // (release adds a one-way-latency forward jump and the sim advances every frame), so writing
        // temporaryPosition = tempPos here snapped the visible ball BACKWARD once per packet
        // (BallMovement rebuilds matchBallTransform.position from temporaryPosition each frame) — the
        // "ball rolls backward" stutter. Skip the integrator write under local sim; per-bounce
        // RpcSyncBallTrajectory and post-shot RpcSyncBallShot still apply authoritatively. Flag OFF
        // (server / default): unchanged stream-aligned behaviour.
        if (!ConstantsData_M.useLocalDeliverySim)
        {
            // Align the parametric delivery simulation so local drift doesn't fight the lerp.
            temporaryPosition = tempPos;
            launchAngle       = curLaunchAngle;
        }
    }

    //Photon Removal  [PunRPC]
    public void RPC_BallTiming(float BallAngle, float HorizontalSpeed, float BallProjectileAngle, float BallProjectileHeight, float BallTimingFirstBounceDistance, float BallProjectileAnglePerSecond, float BallBatMeetingHeight, float HorizontalSpeedMultiplier)
    {

    }

    private void FixBallCatchingSpot()
    {
        // Bowling follower: once the batting authority's exact catch point has been adopted (RPC_SetCatchFielder),
        // never recompute it locally — the local derivation uses a per-client random firstBounceDistance and
        // would re-diverge (catcher runs the wrong way). Keep the authoritative point.
        if (_hasAuthoritativeCatchPoint && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            return;
        }
        ballCatchingPointTransform.position = new Vector3(ballStartPoint.position.x + (firstBounceDistance - preCatchDistance) * Mathf.Cos(_ballAngle * degToRad), ballCatchingPointTransform.position.y, ballStartPoint.position.z + (firstBounceDistance - preCatchDistance) * Mathf.Sin(_ballAngle * degToRad));
        while (DistanceBetweenTwoVector2(groundCenterMarker, ballCatchingPoint) > playingAreaRadius - 2f)
        {
            preCatchDistance += 1f;
            ballCatchingPointTransform.position = new Vector3(ballStartPoint.position.x + (firstBounceDistance - preCatchDistance) * Mathf.Cos(_ballAngle * degToRad), ballCatchingPointTransform.position.y, ballStartPoint.position.z + (firstBounceDistance - preCatchDistance) * Mathf.Sin(_ballAngle * degToRad));
        }
    }

    public void FreezeTheBowlingSpot()
    {
        if (CONTROLLER.PlayModeSelected == 8 && timerObject.activeInHierarchy)
        {
            timerObject.SetActive(false);
        }
        canShowFieldControlPowers = false;
        runnerAnim.CrossFade("getReadyNew");
        runnerAnim["getReadyNew"].speed = 1.3f;
        nonStrikerCurrentStatus = "getReady";
        canBatsmanMoveLaterally = false;
        batsmanAnim.CrossFade("WCCLite_BatsmanTapLoop");
        if (!isReplayModeActive)
        {
            if (leftArrowKeyDownTimes.Count > leftArrowKeyUpTimes.Count)
            {
                leftArrowKeyUpTimes.Add(Time.time - bowlerRunUpStartTime);
            }
            if (rightArrowKeyDownTimes.Count > rightArrowKeyUpTimes.Count)
            {
                rightArrowKeyUpTimes.Add(Time.time - bowlerRunUpStartTime);
            }
        }
        if (!isUserBowlingSpotSelected)
        {
            canUserBowlerMoveBowlingSpot = false;
            FreezeBowlingSpot();
            Singleton<GameData>.instance.ShowTutorial(-1);
        }
        if (Singleton<GameData>.instance != null && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !isReplayModeActive)
        {
            Singleton<GameData>.instance.EnableMovement(boolean: false);
        }
        if (isReplayModeActive && !isOversteppedDelivery)
        {
            replayActionStatus = "follow";
            if (CONTROLLER.stumpingAttempted)
            {
                replayCameraController.enabled = false;
            }
            else
            {
                replayCameraController.enabled = true;
            }
            Time.timeScale = 0.5f;
        }
    }

    private void checkForLineNoBall()
    {
        float num = 0f;
        //if (BowlerAnimationComponent.IsPlaying("Blitz_" + currentBowlerType + "PaceBowling0" + BowlerAnimNumber))
        if (IsPlaying("Blitz_" + bowlerType + "PaceBowling0" + bowlerAnimationNumber))
        {
            //num = BowlerAnimationComponent["Blitz_" + currentBowlerType + "PaceBowling0" + BowlerAnimNumber].time * 25f;
            num = bowlerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime * 25f;
        }
        if (bowlerType == "fast")
        {
            if (num >= 78f && !isNoBall)
            {
                isNoBall = true;
                setLineNoBall();
            }
            else if (num >= 84f && !isSlowMotionActiveForNoBall)
            {
                isSlowMotionActiveForNoBall = true;
                freezeTimeForLineNoBall();
            }
        }
        else if (bowlerType == "medium")
        {
            if (num >= 156f && !isNoBall)
            {
                isNoBall = true;
                setLineNoBall();
            }
            else if (num >= 166f && !isSlowMotionActiveForNoBall)
            {
                isSlowMotionActiveForNoBall = true;
                freezeTimeForLineNoBall();
            }
        }
        else if (bowlerType == "spin")
        {
            if (num >= 156f && !isNoBall)
            {
                isNoBall = true;
                setLineNoBall();
            }
            else if (num >= 166f && !isSlowMotionActiveForNoBall)
            {
                isSlowMotionActiveForNoBall = true;
                freezeTimeForLineNoBall();
            }
        }
    }

    private void setLineNoBall()
    {
        if (isOversteppedDelivery)
        {
            bowlerObject.transform.position = new Vector3(bowlerObject.transform.position.x, bowlerObject.transform.position.y, bowlerObject.transform.position.z + noBallBowlerHeelPosition);
            ////ConstantsData_M.MpLog("* position change");
            matchBallTransform.position = new Vector3(matchBallTransform.position.x, matchBallTransform.position.y, matchBallTransform.position.z + noBallBowlerHeelPosition);
            fielder10SkinTransform.position = new Vector3(fielder10SkinTransform.position.x, fielder10SkinTransform.position.y, fielder10SkinTransform.position.z + noBallBowlerHeelPosition);
            temporaryPosition = matchBallTransform.position;
        }
    }

    private void freezeTimeForLineNoBall()
    {
        if (isOversteppedDelivery && isReplayModeActive)
        {
            Time.timeScale = 0.05f;
        }
    }

    public void ReleaseTheBall()
    {
        // Bug-2 guard: suppress a duplicate release within the same delivery (re-fired bowler
        // animation event). Replays re-simulate, so they are exempt. The flag is reset every
        // delivery in BowlNextBall/ResetAll, so the first real release always proceeds.
        if (!isReplayModeActive && ballReleaseProcessedThisDelivery)
        {
            Debug.LogWarning("[GroundController] Duplicate ReleaseTheBall ignored (already released this delivery).");
            return;
        }
        if (!isReplayModeActive)
        {
            ballReleaseProcessedThisDelivery = true;
        }

        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            timerObject.SetActive(false);
        }

        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            tutorialIndicatorArrow.SetActive(false);
        }
        Singleton<GameData>.instance.canPauseGameplay = false;
        Singleton<GameData>.instance.canResumeGameplay = false;
        if (isOversteppedDelivery)
        {
            noBallActionDelay = 4f;
        }
        needsBattingTimingMeterNeedleUpdate = true;
        if (isReplayModeActive && isOversteppedDelivery)
        {
            replayActionStatus = "follow";
            replayCameraController.enabled = true;
            Time.timeScale = 0.5f;
        }
        if (!isReplayModeActive)
        {
            if (CONTROLLER.PlayModeSelected != 7)
            {
                if (CONTROLLER.currentInnings == 1 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5)
                {
                    targetScoreToWin = CONTROLLER.TargetToChase - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
                }
                else
                {
                    targetScoreToWin = 0;
                }
            }
            else if (CONTROLLER.currentInnings == 3)
            {
                targetScoreToWin = CONTROLLER.TargetToChase - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchScores2;
            }
            else
            {
                targetScoreToWin = 0;
            }
        }
        if (isBallReleased)
        {
            ballTrailRenderer.enabled = true;
            ballTrailRenderer.time = 0.08f;
            ballTrailRenderer.colorGradient = trailColorGradient;
            ballTrailRenderer.material = trailMaterial;
        }
        ActivateColliders(boolean: true);
        isBallReleased = true;
        currentBallStatus = "bowling";
        FindBowlingParameters();
        ballReleaseTime = Time.time;

        // Deterministic delivery (Phase 2): arm the bowling authority's fixed-step integrator at the synced
        // release instant so its run-up flight advances the same 1/60 steps the batting follower will. Gated
        // by useDeterministicDelivery (default off → legacy frame-based delivery). The release NetworkTime is
        // shipped in CmdSyncBallRelease below so the follower bases its sim on the SAME instant.
        if (ConstantsData_M.useDeterministicDelivery && ConstantsData_M.useLocalDeliverySim
            && GameConstants.isWithAI == false
            && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            _ballSimStartNetTime = Mirror.NetworkTime.time;
            // Lockstep follower-flight delay: this (bowling) side is the pure spectator of the flight — run its
            // ball a fixed lead BEHIND real time so the batter's swing input always lands before the ball
            // reaches the contact plane here. targetTicks stays negative until the delay elapses → toRun
            // clamps to 0 → the ball simply waits in the hand; the batter's flight (real release instant,
            // shipped below) is untouched. NOT under the locked plan — there the whole RUN-UP is already
            // delayed, so this anim event fires late by construction and the anchor is simply "now".
            if (!_lockedPlanMinted && ConstantsData_M.useDeterministicContact && ConstantsData_M.lockstepFollowerFlightDelay > 0f)
                _ballSimStartNetTime += ConstantsData_M.lockstepFollowerFlightDelay;
            _ballSimTicksDone = 0;
            _ballSimMaxTargetTicks = -1;   // Issue #3: reset per-flight monotonic target high-water mark
            _useDeterministicBallStep = true;
            ConstantsData_M.MpLog($"[DetDiag] Deterministic DELIVERY armed (bowling authority) at netTime={_ballSimStartNetTime:F3}");
        }

        // Phase 1 ball-release sync: bowling player sends authoritative physics parameters
        // immediately after FindBowlingParameters() so the batting player starts the
        // parametric simulation from identical initial conditions, eliminating the
        // primary divergence caused by a stale bowlingSpot on the batting client.
        if (GameConstants.isWithAI == false && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && CricketNetworkManager.instance != null && !isReplayModeActive
            && !_lockedPlanMinted)   // locked plan: params+seed already shipped pre-run-up — don't re-mint/re-send
        {
            // Deterministic lockstep: mint this delivery's shared RNG seed at the release (the bowling
            // authority owns it), seed our OWN stream immediately, and ship it with the release params —
            // the batting client seeds in RpcSyncBallRelease before any gameplay roll of this ball.
            int _deliverySeed = UnityEngine.Random.Range(int.MinValue / 2, int.MaxValue / 2);
            DeterministicRng.Seed(_deliverySeed);
            CricketNetworkManager.instance.CmdSyncBallRelease(
                staticVariables.UserProfiledata.user._id,
                matchBallTransform.position,
                temporaryPosition,
                _ballAngle,
                launchAngle,
                arcHeight,
                angleChangeRate,
                horizontalVelocity,
                spinFactor,
                swingIntensity,
                isFullToss,
                Mirror.NetworkTime.time,   // release instant for the follower's deterministic delivery base
                _deliverySeed,
                ballSpotLength,
                isOversteppedDelivery,    // no-ball: was NEVER relayed — the batter committed an LBW as OUT on a no-ball
                CONTROLLER.fielderChangeIndex,
                isFieldRestrictionActive,    // field placement re-assert (caught-vs-four: placements diverged per client)
                CONTROLLER.CurrentBowlerIndex);   // bowler identity re-assert (tester: bowler changed / differed after a bowling-side reconnect)
        }
        ShowBall(status: true);
        matchBall.GetComponent<Rigidbody>().WakeUp();
        BowlerBallSkinRenderer.enabled = false;
        // Lockstep follower-flight delay mask: this (bowling) side's flight anchor is +delay in the future —
        // hold the release pose with the in-hand ball visible until the anchor, so this screen shows one
        // continuous action (no ball floating at the hand). Params/seed already minted + sent above in real
        // time, so the batter is untouched. NOT under the locked plan — there the run-up itself is delayed,
        // the anim event lands exactly on the anchor, and no hold is needed.
        if (!_lockedPlanMinted
            && ConstantsData_M.useDeterministicContact && ConstantsData_M.lockstepFollowerFlightDelay > 0f
            && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && !isReplayModeActive)
        {
            StartCoroutine(HoldReleaseVisualsForFlightDelay());
        }
        if (BattingBy == "computer" && !isReplayModeActive)
        {
            GetComputerBattingKeyInput();
        }
        currentActionState = 3;
        if (Singleton<GameData>.instance != null && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !isReplayModeActive)
        {
            Singleton<GameData>.instance.EnableShotSelection(boolean: true);
        }
    }

    public void BlockBowlerSideChange()
    {
        isBowlerWaiting = false;
    }

    public void RPC_StartBowlerRunUp(int animNumber, double startAtNetTime = 0d)
    {
        if (isReplayModeActive) return;
        if (isBallReleased) return;            // delivery already in flight — ignore stale msg
        if (CONTROLLER.PlayModeSelected != 8) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BattingTeamIndex) return;

        DismissStaleScoreCardsForDelivery();
        bowlerAnimationNumber = animNumber;
        // Lockstep: the bowling client relayed a shared NetworkTime start — wait for that instant so the
        // run-up begins simultaneously on both screens (legacy started here on ARRIVAL = one-way late).
        if (startAtNetTime > 0d && LockstepActive)
        {
            StartCoroutine(StartRunUpAtNetTime(startAtNetTime));
            return;
        }
        bowlerRunUpStartTime = Time.time;      // rebase to the real run-up start
        bowlerAnimator.Play("Blitz_" + bowlerType + "PaceBowling0" + bowlerAnimationNumber);
        ConstantsData_M.MpLog($"[GroundController][RPC_StartBowlerRunUp] Batting client run-up started. anim={animNumber}");
    }

    private IEnumerator StartRunUpAtNetTime(double startAtNetTime)
    {
        while (Mirror.NetworkTime.time < startAtNetTime)
        {
            if (isBallReleased || isReplayModeActive) yield break;   // delivery moved on — stale schedule
            yield return null;
        }
        // CATCH UP IF THE START INSTANT HAS ALREADY PASSED.
        //
        // The loop above only waits for a start that is still in the FUTURE. When the relay arrives after
        // startAtNetTime — a late RPC, a GC hitch, a slow scene settle — the loop exits immediately and the
        // run-up used to play FROM THE BEGINNING however late that was. The release fires off the run-up's own
        // animation event, so the batting side then delivered the ball exactly that late while the bowling side
        // had already bowled it. The 03-08 logs show the spread: most starts are 12-22ms late, but there are
        // 119ms, 387ms twice, and one at 2721ms. That is the tester's "dono bowlers ki ball time different ho
        // jati hai, 3-4 baar".
        //
        // Start the animation the same distance INTO the run-up instead, so the release still lands on the
        // shared instant. Past the clip length there is nothing left to skip and it plays out at the end,
        // which is the closest catch-up available. bowlerRunUpStartTime is rebased to the intended start so
        // the arrow-key timings recorded against it stay honest.
        string _runUpState = "Blitz_" + bowlerType + "PaceBowling0" + bowlerAnimationNumber;
        double _lateSec = Mirror.NetworkTime.time - startAtNetTime;
        float _norm = 0f;
        if (_lateSec > 0.05d)
        {
            float _clipLen = 0f;
            if (bowlerAnimator != null && bowlerAnimator.runtimeAnimatorController != null)
            {
                var _clips = bowlerAnimator.runtimeAnimatorController.animationClips;
                for (int i = 0; i < _clips.Length; i++)
                    if (_clips[i] != null && _clips[i].name == _runUpState) { _clipLen = _clips[i].length; break; }
            }
            if (_clipLen > 0.01f) _norm = Mathf.Clamp01((float)_lateSec / _clipLen);
        }
        bowlerRunUpStartTime = Time.time - (float)Mathf.Max((float)_lateSec, 0f);   // rebase to the INTENDED start
        bowlerAnimator.Play(_runUpState, 0, _norm);
        // animator SPEED belongs in this line: the release-pose hold parks it at 0 and several paths restore
        // it, so a stale 0 here means the clip is frozen — the run-up "starts", never reaches its release
        // event, and this side silently gets no ball. Without the value, a frozen clip and a clip that played
        // fine but had its plan cleared underneath produce the identical log.
        // Second reset point, for the paths that reach a run-up without arming a locked plan first: the
        // run-up beginning is the other unambiguous "this delivery starts now". See the note at the plan-arm.
        _deliveryWatchTimer = 0f;
        ConstantsData_M.MpLog($"[Lockstep] Batting run-up started at shared netTime={startAtNetTime:F3} (late {_lateSec * 1000.0:F0}ms)"
            + $" animSpeed={(bowlerAnimator != null ? bowlerAnimator.speed.ToString("F2") : "<null>")} planArmed={_lockedPlanArmed}"
            + (_norm > 0f ? $" — skipped {_norm * 100f:F0}% into the run-up to catch up." : ""));
    }

    //Photon Removal [PunRPC]
    public void RPC_BowlerWaiting()
    {
        if (!Singleton<GameData>.instance.opponentWatchingTutorialPanel.activeInHierarchy)
        {
            return;
        }
        else
        {
            Time.timeScale = 1f;
            ReleaseTheBall();
            Singleton<GameData>.instance.opponentWatchingTutorialPanel.SetActive(false);
        }
    }

    public void Bowl()
    {
        if (GameConstants.isWithAI == false)
        {
            //Photon Removal     view.RPC("RPC_BowlerWaiting", RpcTarget.OthersBuffered);
            CricketNetworkManager.instance.CmdBowlerWaiting(staticVariables.UserProfiledata.user._id);
        }

        //TEST

    }

    public void BowlerWaiting()
    {
        ////ConstantsData_M.MpLog("*BOWLER WAITING");
        if (!CONTROLLER.ReplayShowing)
        {
            Singleton<GameData>.instance.canPauseGameplay = true;
        }
        else
        {
            Singleton<GameData>.instance.canPauseGameplay = false;
        }
        if (Time.time > ballStartTime + bowlerWaitDuration)
        {
            if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && CONTROLLER.PlayModeSelected == 8)
            {
                if (CONTROLLER.ReplayShowing)
                {
                    StartCoroutine(DelayedFunction());
                }
            }
            else
            {
                if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
                {
                    if (CONTROLLER.ReplayShowing)
                    {
                        StartCoroutine(DelayedFunction());
                    }
                    else
                    {
                        StartCoroutine(CallRpcAckBowl());
                    }
                }
                // RUN-UP SYNC FIX: in MP the batting client must NOT start the bowler
                // run-up here. The bowling client emits CmdSyncBowlerRunUp at the moment its
                // own run-up actually starts (DelayedFunction), and RPC_StartBowlerRunUp then
                // begins the batting client's run-up — so the throw frame lines up with the
                // authoritative ball-release RPC. Playing it immediately here caused the
                // bowler to throw ~1.25s + RTT before the ball spawned on the batting side.
                // During replay, the batting client also starts from DelayedFunction; playing
                // immediately here restarts the same run-up 1.25s later.
                bool isMpBatting = CONTROLLER.PlayModeSelected == 8
                    && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex
                    && GameConstants.isWithAI == false;
                if (!isMpBatting)
                {
                    bowlerAnimator.Play("Blitz_" + bowlerType + "PaceBowling0" + bowlerAnimationNumber);
                }
            }

            canBatsmanMoveLaterally = true;
            bowlerRunUpStartTime = Time.time;
            if (!isReplayModeActive)
            {
                FindNewBowlingSpot();
                canUserBowlerMoveBowlingSpot = true;
                Singleton<Scoreboard>.instance.BowlerToBatsman();
                isLeftArrowPressedSaved = false;
                isRightArrowPressedSaved = false;
                leftArrowKeyDownTimes = new List<float>();
                leftArrowKeyUpTimes = new List<float>();
                rightArrowKeyDownTimes = new List<float>();
                rightArrowKeyUpTimes = new List<float>();
                takeRunTimings = new List<float>();
                cancelRunData = new float[10, 10];
                cancelRunTotalCount = 0;
                totalSaveAttempts = 0;
                totalCancelAttempts = 0;
                saveRunRetryData = new float[10, 10];
                saveRunRetryCount = 0;
            }
            currentActionState = 2;
            if (Singleton<GameData>.instance != null && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !isReplayModeActive)
            {
                Singleton<GameData>.instance.EnableMovement(boolean: true);
            }

        }
    }

    //Photon Removal   [PunRPC]
    public void AckForBowling()
    {
        if (isBowlingAcknowledged)
        {
            return;
        }
        if (GameConstants.isWithAI == false)
        {
            //ConstantsData_M.MpLog("Bowled ---->");
            StartCoroutine(DelayedFunction());
        }
    }

    public bool getOverStepBall()
    {
        return isOversteppedDelivery;
    }

    public bool getLineFreeHitBall()
    {
        return isLineFreeHitActive;
    }

    public IEnumerator updateBoundaryBall()
    {
        bool isMatchOver = checkForMatchComplete(lineNoBallRunsScored + 1, 0);
        if (CONTROLLER.matchType == "oneday" && !isMatchOver)
        {
            yield return new WaitForSeconds(4f);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }
        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
        Singleton<GameData>.instance.UpdateCurrentBall(0, 1, lineNoBallRunsScored, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: true);
        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
        {
        }
    }

    private void BallRebouncesFromBoundary()
    {
        _ballAngle = _ballAngle + 180f + (float)DetRange(-20, 20);   // lockstep: seeded — fence/pad deflections were per-client random (a known cross-screen diverger)
        _ballAngle %= 360f;
        horizontalVelocity *= 0.2f;
        fenceHeight = matchBallTransform.position.y;
        angleChangeRate *= 1.5f;
        applyBallFriction = true;
        isBallReflectedFromBoundary = true;
    }

    //Photon Removal [PunRPC]
    public void RPC_ActivateBowlerSideChangeViaUI(string BowlerSide)
    {
        // BowlerSide is the bowling player's side BEFORE the toggle; the new side is its mirror.
        string newSide = (BowlerSide == "right") ? "left" : "right";

        // Bowler-side desync fix: on the BATTING client BowlingBy != "user" (and isBowlerWaiting is
        // usually false there), so BowlerSideChange() below hits its
        // `!(BowlingBy == "user") || !isBowlerWaiting` guard and yield-breaks — the toggle never
        // applied, so a side change the bowling player made via the UI left the bowler on the
        // OPPOSITE side on the batting player's screen (rare: only when the bowler actually toggles).
        // The bowling client keeps the existing coroutine (fade + over/round-the-wicket strip text);
        // any client that can't run it applies the authoritative new side directly so both screens
        // always agree (RestartBowlerSide is the same call RpcSyncBowlerSide already uses).
        if (BowlingBy == "user" && isBowlerWaiting)
        {
            StartCoroutine(BowlerSideChange(newSide));
        }
        else
        {
            RestartBowlerSide(newSide);
        }
    }

    public void ActivateBowlerSideChangeViaUI()
    {
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.CanBowlerBowl)
        {
            if (GameConstants.isWithAI == false)
            {
                if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.CanBowlerBowl && currentActionState == -2 && timerObject.activeInHierarchy                                           //Photon Removal
                    && CONTROLLER.PlayModeSelected == 8)
                    //        view.RPC("RPC_ActivateBowlerSideChangeViaUI", RpcTarget.AllBuffered, bowlerSide);
                    CricketNetworkManager.instance.CmdActivateBowlerSideChangeViaUI(bowlerSide);
            }


        }
        else if (CONTROLLER.PlayModeSelected != 8)
        {
            if (bowlerSide == "left")
            {
                StartCoroutine(BowlerSideChange("right"));
            }
            else if (bowlerSide == "right")
            {
                StartCoroutine(BowlerSideChange("left"));
            }
        }

    }

    public void RestartBowlerSide(string from)
    {
        bowlerSide = from;
        SetBowlerSide();
    }

    public IEnumerator BowlerSideChange(string from)
    {
        if (!(BowlingBy == "user") || !isBowlerWaiting)
        {
            yield break;
        }
        float fadeInOutTime = 0.2f;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || from == "left")
        {
            if (bowlerSide == "right")
            {
                yield return new WaitForSeconds(fadeInOutTime);
                bowlerSide = "left";
                SetBowlerSide();
                if (bowlerHand == "right")
                {
                    Singleton<Scoreboard>.instance.UpdateStripText("Bowling over the wicket");
                }
                else if (bowlerHand == "left")
                {
                    Singleton<Scoreboard>.instance.UpdateStripText("Bowling round the wicket");
                }
                yield return new WaitForSeconds(fadeInOutTime);
            }
        }
        else if ((Input.GetKeyDown(KeyCode.RightArrow) || from == "right") && bowlerSide == "left")
        {
            yield return new WaitForSeconds(fadeInOutTime);
            bowlerSide = "right";
            SetBowlerSide();
            if (bowlerHand == "right")
            {
                Singleton<Scoreboard>.instance.UpdateStripText("Bowling round the wicket");
            }
            else if (bowlerHand == "left")
            {
                Singleton<Scoreboard>.instance.UpdateStripText("Bowling over the wicket");
            }
            yield return new WaitForSeconds(fadeInOutTime);
        }
    }

    public void BowlNextBall(string batting, string bowling)
    {


        int num = 0;
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            num = 1;
        }
        if (!isWarmUpDoneOnce)
        {
            WarmUpOnce();

            isWarmUpDoneOnce = true;
        }
        CONTROLLER.BALLANGLE = 0f;
        CONTROLLER.HORIZONTALSPEED = 0f;
        CONTROLLER.BALLPROJECTILEANGLE = 0f;
        CONTROLLER.BALLPROJECTILEHEIGHT = 0f;
        CONTROLLER.BALLTIMINGFIRSTBOUNCEDISTANCE = 0f;
        CONTROLLER.BALLPROJECTILEANGLEPERSECOND = 0f;
        CONTROLLER.BALLBATMEETINGHEIGHT = 0f;
        CONTROLLER.HORIZONTALSPEEDMULTIPLIER = 0f;

        CONTROLLER.BATSMANMOVE = false;
        CONTROLLER.BATSMANSHOT = false;
        CONTROLLER.BOWLINGSPOTINFO = false;

        isBallWide = false;
        shouldPlayIntro = false;
        isNewInning = false;
        replayViewCamera.enabled = false;
        Time.timeScale = 1f;
        Singleton<BattingControls>.instance.updateBatsmanTiming(GetBattingAbility());
        isAIHittingInGap = true;
        isRunning = false;
        isBallHit = false;
        ballReleaseProcessedThisDelivery = false; // new delivery: allow exactly one release
        _outcomeCommittedThisDelivery = false;    // new delivery: arm orphan detection until its outcome commits
        _collectFiredThisDelivery = false;        // new delivery: re-arm the collect-gate probe
        _collectGateReported = false;
        isRunOutAppealSaved = false;
        summarySaved = string.Empty;
        pickedUpFielderIndexSaved = -1;
        isRunOutSaved = false;
        currentBallRunsSaved = 0;
        ballRayCastConnectionZPositionSaved = 0f;
        isLbwAppealSaved = false;
        isLbwSaved = false;
        throwActionSaved = string.Empty;
        throwTargetSaved = string.Empty;
        _relayedThrowTarget = string.Empty;   // authority ruling is per delivery
        stumpAnimationToPlaySaved = string.Empty;
        isPowerShotActiveSaved = false;
        previousStumpedState = false;
        returnToCreaseAnimationId = 0;
        HideBatShadow();
        SetDefaultDigitalDisplayContent();
        isWideWithStumpingSignalDisplayed = false;
        CONTROLLER.runoutThirdUmpireAppeal = false;
        isTightRunoutCall = false;
        isVeryTightRunoutCall = false;
        isThirdUmpireRunoutReplaySkipped = false;
        isRunForRunOutFailedPostReplay = false;
        restrictReplayCameraHeight = false;
        BattingBy = batting;
        BowlingBy = bowling;
        batsmanHand = CONTROLLER.StrikerHand;
        bowlerHand = CONTROLLER.BowlerHand;
        isFieldRestrictionActive = CONTROLLER.PowerPlay;
        isHattrickDelivery = CONTROLLER.HattrickBall;
        spinFactor = 0f;
        isBowlerWaiting = false;
        if (BowlingBy == "computer" && CONTROLLER.PlayModeSelected != 6)
        {
            LookToChangeBowlerSideInBetweenTheOver();
        }
        Vector3 vector = new Vector3(0f, 0f, 0f);
        vector = ((!(BattingBy == "user") || !(CONTROLLER.difficultyMode == "hard")) ? new Vector3(2f * (1f + powerMultiplier * (float)num * 5f), 1.5f * (1f + powerMultiplier * (float)num * 5f), 2f * (1f + powerMultiplier * (float)num * 5f)) : new Vector3(1f * (1f + powerMultiplier * (float)num * 5f), 1.2f * (1f + powerMultiplier * (float)num * 5f), 1f * (1f + powerMultiplier * (float)num * 5f)));
        PrimaryBatCollider.transform.localScale = vector;
        SecondaryBatCollider.transform.localScale = Vector3.one;
        // Spin-contact forgiveness (morning#4/#7): a turning spin ball curves away from the straight-line swing
        // target, so a well-timed shot still frequently MISSES the bat. Modestly widen the PRIMARY (bat-face)
        // collider for SPIN deliveries only — pace/medium untouched, and the edge collider (SecondaryBatCollider)
        // is left alone so this does NOT inflate caught-behind edges. Deterministic per-ball setup on both
        // clients (shared bowlerType) → symmetric, no desync. Tunable: dial the 1.35f back if spin feels too easy.
        if (bowlerType == "spin")
        {
            PrimaryBatCollider.transform.localScale = vector * 1.35f;
        }

        if (CONTROLLER.PlayModeSelected == 4 && CONTROLLER.SuperOverMode == "bat")
        {

        }

        else if (CONTROLLER.BowlerType == 0)
        {
            bowlerType = "fast";
        }
        else if (CONTROLLER.BowlerType == 1 || CONTROLLER.BowlerType == 2)
        {
            bowlerType = "spin";
            bowlerSpinType = CONTROLLER.BowlerType;
            if (bowlerHand == "left")
            {
                if (bowlerSpinType == 1)
                {
                    bowlerSpinType = 2;
                }
                else if (bowlerSpinType == 2)
                {
                    bowlerSpinType = 1;
                }
            }
        }
        else if (CONTROLLER.BowlerType == 3)
        {
            bowlerType = "medium";
        }
        if (isBatsmanConfident && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !isFieldRestrictionActive)
        {
            if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal > 7f)
            {
                SetComputerFieldIndex(UnityEngine.Random.Range(8, 11));
            }
            else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal > 5f)
            {
                SetComputerFieldIndex(UnityEngine.Random.Range(6, 8));
            }
            else
            {
                SetComputerFieldIndex(UnityEngine.Random.Range(1, 6));
            }
        }
        if (BowlingBy == "computer" && isBallToFineLeg && (runsScoredThisBall == 6 || runsScoredThisBall == 4))
        {
            if (isFieldRestrictionActive)
            {
                SetComputerFieldIndex(1);
            }
            else if (!isFieldRestrictionActive)
            {
                SetComputerFieldIndex(8);
            }
        }
        if (Singleton<AIFieldingSetupManager>.instance.enabled && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected != 8)
        {
            CONTROLLER.computerFielderChangeIndex = Singleton<AIFieldingSetupManager>.instance.GetAIFieldingIndex();
        }
        if (CONTROLLER.computerFielderChangeIndex != Singleton<AIFieldingSetupManager>.instance.previousFieldSet && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            Singleton<PreviewScreen>.instance.alertPopup.SetActive(value: true);
        }
        if (noBall)
        {
            StartCoroutine(freehitscenario());
        }
        else
        {
            ResetAll();
        }
        if (CONTROLLER.cameraType == 0)
        {
            ActivateStadiumAndSkybox(boolean: false);
        }

    }

    private void LookToChangeBowlerSideInBetweenTheOver()
    {
        int num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls % 6;
        string empty = string.Empty;
        if (num <= 1 || !(BowlingBy == "computer"))
        {
            return;
        }
        int num2 = 0;
        for (int i = num - 2; i < num; i++)
        {
            empty = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[i];
            if (empty == "4" || empty == "6")
            {
                num2++;
            }
        }
        if (num2 == 2)
        {
            if (bowlerSide == "left")
            {
                bowlerSide = "right";
            }
            else
            {
                bowlerSide = "left";
            }
        }
    }

    public void NewOver()
    {
        if (CONTROLLER.PlayModeSelected == 8)
        {
            CONTROLLER.CanLoadGround = false;
        }
        if (PlayerPrefs.HasKey("CTBowler"))
        {
            PlayerPrefs.DeleteKey("CTBowler");
        }
        if (ObscuredPrefs.HasKey("bowlerBowlStraight" + CONTROLLER.PlayModeSelected))
        {
            ObscuredPrefs.DeleteKey("bowlerBowlStraight" + CONTROLLER.PlayModeSelected);
        }
        if (ObscuredPrefs.HasKey("stumpBallCount" + CONTROLLER.PlayModeSelected))
        {
            ObscuredPrefs.DeleteKey("stumpBallCount" + CONTROLLER.PlayModeSelected);
        }
        selectedBowlerIndex = UnityEngine.Random.Range(1, 3);
        if (BowlingBy == "computer")
        {
            if (UnityEngine.Random.Range(0f, 10f) > 5f)
            {
                bowlerSide = "right";
            }
            else
            {
                bowlerSide = "left";
            }
        }
        canShowPartnership = true;
        int num = umpireMainIndex;
        umpireMainIndex = umpireSideIndex;
        umpireSideIndex = num;
        if (CONTROLLER.PlayModeSelected != 7)
        {
            MainUmpireSkinRenderer.materials[0].SetTexture("_PatternTex", umpireTextures[umpireMainIndex]);
            SideUmpireSkinRenderer.materials[0].SetTexture("_PatternTex", umpireTextures[umpireSideIndex]);
        }
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8)
        {
            Singleton<BowlingScoreCard>.instance.ContinueBtn.gameObject.GetComponent<Button>().interactable = false;
        }
        ShowAllPlayers();
    }

    private IEnumerator freehitscenario()
    {
        _stayStartTime = Time.time;
        showPreviewCamera(status: false);
        gameplayCamera.enabled = false;
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        closeUpViewCamera.enabled = false;
        umpireViewCamera.enabled = true;
        if (runsScoredThisBall == 0 || runsScoredThisBall == 4 || runsScoredThisBall == 6)
        {
            umpireRunSignalDirection = 0;
        }
        if (umpireRunSignalDirection == 1)
        {
            umpireCamTransform.position = stumpRightCrease.transform.position;
            umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 1.5f, umpireCamTransform.position.z);
            _mainUmpireTransform.position = umpireLeftSpot.transform.position;
            _mainUmpireTransform.eulerAngles = new Vector3(_mainUmpireTransform.eulerAngles.x, 90f, _mainUmpireTransform.eulerAngles.z);
            umpireCamTransform.eulerAngles = new Vector3(umpireCamTransform.eulerAngles.x, 270f, umpireCamTransform.eulerAngles.z);
            umpireCamTransform.position -= new Vector3(1f, 0f, 0f);
            umpireViewCamera.fieldOfView = 40f;
        }
        else if (umpireRunSignalDirection == -1)
        {
            umpireCamTransform.position = stumpRightCrease.transform.position;
            umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 1.5f, umpireCamTransform.position.z);
            //mainUmpireTransform.localScale = new Vector3(1f, mainUmpireTransform.localScale.y, mainUmpireTransform.localScale.z);
            _mainUmpireTransform.position = umpireRightSpot.transform.position;
            _mainUmpireTransform.eulerAngles = new Vector3(_mainUmpireTransform.eulerAngles.x, 270f, _mainUmpireTransform.eulerAngles.z);
            umpireCamTransform.eulerAngles = new Vector3(umpireCamTransform.eulerAngles.x, 90f, umpireCamTransform.eulerAngles.z);
            umpireCamTransform.position += new Vector3(1f, 0f, 0f);
            umpireViewCamera.fieldOfView = 40f;
        }
        else
        {
            umpireCamTransform.position = mainUmpireInitialPosition;
            umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 2f, umpireCamTransform.position.z);
            umpireCamTransform.position += new Vector3(0f, 0f, 3f);
            umpireCamTransform.eulerAngles = new Vector3(5 + UnityEngine.Random.Range(0, 5), 180f, umpireCamTransform.eulerAngles.z);
            umpireViewCamera.fieldOfView = 40f;
        }
        mainUmpireAnim.Play("NoBallFreeHit_New");
        Singleton<GameData>.instance.InitAnimation(5);
        noBall = false;
        freeHit = true;
        CONTROLLER.isFreeHit = true;
        isFreeHitActive = true;
        yield return new WaitForSeconds(2f);
        ResetAll();
        Singleton<GameData>.instance.canPauseGameplay = true;
        Singleton<Scoreboard>.instance.HidePause(boolean: false);
    }

    private void enableBowlingSpot()
    {
        if (currentAnimationStatus == "idle")
        {
            return;
        }
        if (currentAnimationStatus == "spin")
        {
            bowlingSpot.localEulerAngles += new Vector3(0f, currentRotationSpeed * Time.deltaTime, 0f);
            if (tutorialIndicatorArrow != null)
            {
                tutorialIndicatorArrow.transform.localEulerAngles = new Vector3(tutorialIndicatorArrow.transform.localEulerAngles.x, 0f, tutorialIndicatorArrow.transform.localEulerAngles.z);
            }
            float num = Mathf.PingPong(Time.time * zoomSensitivity, maximumZoom - minimumZoom) + minimumZoom;
        }
        else if (currentAnimationStatus == "spinToFreeze")
        {
            currentRotationSpeed -= initialRotationSpeed / freezeDuration * Time.deltaTime;
            bowlingSpot.localScale -= Vector3.one * ((freezeScale - minimumZoom) / freezeDuration) * Time.deltaTime;
            if (Time.time > freezeStartTime + freezeDuration)
            {
                currentAnimationStatus = "idle";
            }
        }
    }

    public void ShowFullTossSpot(bool _Value)
    {
        bowlingSpotFullTossObject.SetActive(_Value);
    }

    private void HideBowlingSpot()
    {
        bowlingSpotModel.GetComponent<Renderer>().enabled = false;
        if (tutorialIndicatorArrow != null)
        {
            tutorialIndicatorArrow.SetActive(value: false);
        }
    }

    private void ShowBowlingSpot()
    {
        bowlingSpotModel.GetComponent<Renderer>().enabled = true;
        tutorialIndicatorArrow.SetActive(value: true);
        currentRotationSpeed = initialRotationSpeed;
        bowlingSpot.localScale = Vector3.one;
        bowlingSpot.localEulerAngles = new Vector3(0f, 0f, 0f);
        currentAnimationStatus = "spin";
        if (!Singleton<GameData>.instance.canShowTutorial())
        {
        }
    }

    //Photon Removal  [PunRPC]
    public void RPC_ChangeBowlinSpot(Vector3 BowlingSpotPos)
    {
        bowlingSpot.position = BowlingSpotPos;
        if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
        {
            if (!isUserBowlingSpotSelected)
            {
                if (bowlingSpot.position.z > userBowlingFullTossTriggerPoint.transform.position.z)
                {
                    FullTossFunction(AI: false);
                }

                if (bowlingSpot.position.z < userBowlingFullTossTriggerPoint.transform.position.z + 1f && bowlingSpot.position.z < 11f)
                {
                    isFullToss = false;
                    bowlingSpotRenderer.enabled = true;
                    if (tutorialIndicatorArrow != null)
                    {
                        if (currentAnimationStatus != "spinToFreeze")
                        {
                            tutorialIndicatorArrow.SetActive(value: true);
                        }
                    }
                    bowlingSpotFullTossObject.transform.position = bowlingSpot.position;
                    bowlingSpotFullTossObject.SetActive(value: false);
                }
            }
            Singleton<Tutorial>.instance.updateBowlingHolderPos(bowlingSpot.position);
        }
    }

    //Photon Removal  [PunRPC]
    public void RPC_FreezeBowlingSpot()
    {
        canUserBowlerMoveBowlingSpot = false;
        FreezeBowlingSpot();
    }

    private void FreezeBowlingSpot()
    {
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8)
        {

            if (GameConstants.isWithAI == false)
            {

                //    view.RPC("RPC_FreezeBowlingSpot", RpcTarget.OthersBuffered);                                                                                        //Photon Removal
                CricketNetworkManager.instance.CmdFreezeBowlingSpot(staticVariables.UserProfiledata.user._id);
            }

        }
        freezeStartTime = Time.time;
        freezeScale = bowlingSpot.localScale.x;
        currentAnimationStatus = "spinToFreeze";
        if (tutorialIndicatorArrow != null)
        {
            tutorialIndicatorArrow.SetActive(value: false);
        }
    }

    //Photon Removal   [PunRPC]
    public void AutomaticBall()
    {
        if (!timerObject.activeInHierarchy)
        {
            // First-post-reconnect-ball fix (tester: har batting-side reconnect ke baad "current turn nahi
            // kheli gayi"): on the reconnected BATTING client the timer UI's parent panel is still inactive
            // after the scene reload, so this stale-RPC guard silently DROPPED the incoming auto-bowl —
            // BowlAutomatic never ran on the batter, its delivery/shot arming never started, and the ball
            // played out on the bowler's screen only (then stalled and re-bowled into the same drop). The
            // timer is the BOWLER's intent signal; on the batting side an inactive timer is just UI state —
            // re-activate it (same ancestor fix as the -2 auto-bowl arm, GroundController.cs ~4726) and
            // process the delivery normally. The bowling side keeps the strict gate (it owns the timer).
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && timerObject != null)
            {
                for (Transform _anc = timerObject.transform; _anc != null; _anc = _anc.parent)
                {
                    if (!_anc.gameObject.activeSelf) _anc.gameObject.SetActive(true);
                }
                ConstantsData_M.MpLog("[GroundController][AutomaticBall] Batting-side timer UI was inactive — reactivated (post-reconnect first ball) and processing the delivery.");
            }
            if (!timerObject.activeInHierarchy)
            {
                return;
            }
        }
        // Review "auto-bowl steals the delivery" guard: don't let the auto-bowl timer fire the next ball
        // while a DRS review replay is up. DRSreplay is now set ONLY on the OFFLINE DRS review path (online
        // reviews skip the replay and resolve instantly via ForceResolveStuckReview), so this gate effectively
        // only affects offline play, where the replay completes normally and clears the flag — it can never
        // permanently suppress the auto-bowl. Online never sets DRSreplay (and the bowling-spot timer is
        // already deactivated during a delivery anyway), so there is no permanent-freeze risk.
        if (Singleton<DRS>.instance != null && Singleton<DRS>.instance.DRSreplay)
        {
            return;
        }
        DismissStaleScoreCardsForDelivery();   // auto-bowl firing = game live → clear any stuck scorecard overlay
        Singleton<GameData>.instance.BowlAutomatic();

        canUserBowlerMoveBowlingSpot = false;
        FreezeBowlingSpot();
        Singleton<GameData>.instance.ShowTutorial(-1);
        if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
        {
            timerObject.SetActive(false);
        }
    }

    private void CallAutomaticBall()
    {
        if (timerObject.activeInHierarchy)
        {
            if (GameConstants.isWithAI == false)
            {                                                                                                                                                                                                    //Photon Removal

                //    view.RPC("AutomaticBall", RpcTarget.AllBuffered);
                CricketNetworkManager.instance.CmdAutomaticBall();
            }


        }
    }

    public void SetAutoBowlingTimer(float Seconds)
    {
        // Reconnect-hang fix: the per-tick callback below Kills animationSequence when timerObject is NOT
        // active-in-hierarchy. After a mid-delivery reconnect, CancelInFlightDeliveryForReconnect left
        // timerObject (or its parent panel) inactive, so the freshly-built Sequence self-killed and every
        // later Append/InsertCallback — including the InsertCallback(Seconds, CallAutomaticBall) that fires
        // the NEXT ball — threw "inactive/killed Sequence". Result: CallAutomaticBall never scheduled →
        // bowler never auto-bowls → no ReleaseTheBall → both clients hang. Guard the build:
        //  (1) kill any prior sequence before reassigning (no orphan tweens);
        //  (2) if timerObject isn't active-in-hierarchy, do NOT build a self-killing sequence — schedule the
        //      auto-bowl directly via Invoke so the bowler still delivers (CallAutomaticBall/AutomaticBall
        //      already self-guard on activeInHierarchy, so a stale invoke is a harmless no-op); CancelInvoke
        //      first so the per-frame case -2 re-arm can never stack duplicate deliveries.
        if (animationSequence != null && animationSequence.IsActive()) animationSequence.Kill();
        animationSequence = null;

        if (timerObject == null || !timerObject.activeInHierarchy)
        {
            // Do NOT build a sequence here: its per-tick callback Kills itself when the timer isn't
            // active-in-hierarchy → "inactive/killed Sequence" spam, and the entire auto-bowl chain
            // (AutomaticBall + CallAutomaticBall both early-return on !activeInHierarchy) would no-op anyway,
            // so an Invoke fallback can't rescue it. The reconnect re-arm in Update() now re-activates the
            // timer's PARENT before calling this, so the live path never reaches here; bail safely otherwise.
            ConstantsData_M.MpLog("[GroundController] SetAutoBowlingTimer: timerObject not active-in-hierarchy — skipped (no self-killing sequence built).");
            return;
        }

        multiplayerSecondCounter = Seconds;
        timerProgressBar.fillAmount = Seconds;
        animationSequence = DOTween.Sequence();
        TweenCallback callback = delegate
        {
            if (!timerObject.activeInHierarchy)
            {
                animationSequence.Kill();
            }
            SetSecond();
        };
        animationSequence.Append(timerProgressBar.DOFillAmount(0f, Seconds));
        for (int i = 0; i < Seconds; i++)
        {

            animationSequence.InsertCallback(i, callback);
        }
        animationSequence.InsertCallback(Seconds, CallAutomaticBall);
    }

    private void MoveToCollectBallAfterBoundary(int fielderIndex, bool stop = false, bool first = false)
    {
        if (stop)
        {
            fieldersAnim[fielderIndex].Play("runComplete");
            return;
        }
        if (first)
        {
            fieldersAnim[fielderIndex].Stop();
            fieldersAnim[fielderIndex].Play("walk");
        }
        fielderTransforms[fielderIndex].LookAt(new Vector3(matchBallTransform.position.x, 0f, matchBallTransform.position.z));
        fielderTransforms[fielderIndex].position += fielderTransforms[fielderIndex].forward * 1.6f * Time.deltaTime;
    }

    private void SaveBallTravelTime()
    {
        if (!isBallTimeSaved && currentActionState > 2)
        {
            if (temporaryPosition.z - hotspotReferences[3].transform.position.z <= 0f && temporaryPosition.z - hotspotReferences[0].transform.position.z >= 0f)
            {
                totalElapsedTime += Time.deltaTime;
            }
            else if (temporaryPosition.z - hotspotReferences[3].transform.position.z > 0f)
            {
                isBallTimeSaved = true;
            }
        }
    }

    public void BallMovementCustom()
    {
        if (!isCustomBallMovementActive)
        {
            ballTravelTime = UnityEngine.Random.Range(0.09f, 0.1f);
            float num = 0f;
            num = Mathf.Round(hotspotReferences[3].transform.position.x * 1000f) / 1000f;
            Vector3 vector = new Vector3(num, hotspotReferences[3].transform.position.y, hotspotReferences[3].transform.position.z);
            if (hasEdgeOccurred)
            {
                ballPath1Points[0].transform.position = new Vector3(ultraEdgeImpactImage.transform.position.x - minEdgeDistance, ultraEdgeImpactImage.transform.position.y, hotspotReferences[1].transform.position.z);
                ballTweener = matchBall.transform.DOMove(ballPath1Points[0].transform.position, ballTravelTime);
            }
            else
            {
                ballPath1Points[0].transform.position = new Vector3(ultraEdgeImpactImage.transform.position.x - safeEdgeDistance, ultraEdgeImpactImage.transform.position.y, hotspotReferences[1].transform.position.z);
                ballTweener = matchBall.transform.DOMove(ballPath1Points[0].transform.position, ballTravelTime);
            }
            isCustomBallMovementActive = true;
        }
        Singleton<RewindTime>.instance.CanCapture = true;
    }

    //Photon Removal [PunRPC]
    public void RPC_UpdateCanBowlerBowl(bool CanBowlerBowl)
    {
        if (CanBowlerBowl != CONTROLLER.CanBowlerBowl)
        {
            // The bowling side's auto-bowl timer only arms while CanBowlerBowl is true, and that permission
            // is broadcast by the BATTING client every frame from case -2. Whether it actually lands has
            // never been observable: every log line on this path is commented out, so a batting client
            // sitting at state=-2 with canBowl=True while the bowler does nothing (the 08-27 18:46 pair,
            // twice, 32s each, both players connected) could not be told apart from the permission arriving
            // and the bowling side failing to act on it. Those are different bugs.
            //
            // Logged on CHANGE only — the send runs every frame, so anything else would flood the 200-line
            // window the reporter keeps.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                ConstantsData_M.MpLog($"[CanBowlDiag] Permission from the batting side: {CONTROLLER.CanBowlerBowl} -> {CanBowlerBowl}"
                    + $" (state={currentActionState} timerLive={(timerObject != null && timerObject.activeInHierarchy)}"
                    + $" amBowling={(CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)}).");
            CONTROLLER.CanBowlerBowl = CanBowlerBowl;
        }

    }

    private bool _canBowlerBowlBlockedLogged;   // one-shot for the ghost-session guard below

    public void UpdateCanBowlerBowl(bool CanBowlerBowl)
    {
        CONTROLLER.CanBowlerBowl = CanBowlerBowl;
        if (GameConstants.isWithAI == false)
        {                                                                                                                                                                                                  //Photon Removal
                                                                                                                                                                                                           //view.RPC("RPC_UpdateCanBowlerBowl", RpcTarget.OthersBuffered, CanBowlerBowl);
            // Called from GroundController.Update, so during the reconnect scene-reload window the
            // spawned manager is destroyed and Mirror's SendCommandInternal NREs every frame (report
            // 36b38d60: 34x CmdUpdateCanBowlerBowl NRE, "turn miss after reconnect from bowler side").
            // Same ghost-session guard as the other Cmds.
            if (CricketNetworkManager.ReadyToSend)
                CricketNetworkManager.instance.CmdUpdateCanBowlerBowl(staticVariables.UserProfiledata.user._id, CanBowlerBowl);
            else if (!_canBowlerBowlBlockedLogged)
            {
                _canBowlerBowlBlockedLogged = true;
                ConstantsData_M.MpLog("[GroundController][UpdateCanBowlerBowl] Send BLOCKED — network manager not ready (ghost session). Runs every frame from Update, so this logs once.");
            }
        }


    }

}
