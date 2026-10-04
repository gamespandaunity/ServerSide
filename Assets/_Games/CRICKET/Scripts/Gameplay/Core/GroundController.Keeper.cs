// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.Keeper — WICKET-KEEPER + STUMPS subsystem (extracted from .Fielding, Phase 3).
// Keeper state machine (currentWicketKeeperStatus strings), pre/post-batting actions, collect gating
// (LockstepKeeperGateOpen + y<1.3 beaten-ball collect), stump animations + bowled/stumped decisions
// (IsStumpOut*, false-bowled guard via the authoritative X estimate at the stump plane).
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
    public void AIBowlToStump()
    {
        bowlingSpot.position = new Vector3(0f, 0.06f, 9f);
    }

    public void BowlToStump()
    {
        bowlingSpot.position = new Vector3(9.5f, bowlingSpot.position.y, 0f);
    }

    public void ActivateWicketKeeper()
    {
        isWicketKeeperActive = true;
        //float num = wicketKeeperTransform.position.x - ballTransform.position.x;
        //float num2 = wicketKeeperTransform.position.z - ballTransform.position.z;

        float num = _wicketKeeperTransform.position.x - temporaryPosition.x;
        float num2 = _wicketKeeperTransform.position.z - temporaryPosition.z;
        wkAdjacentLength = Mathf.Sqrt(num * num + num2 * num2);
        wkThetaBetweenAdjAndHyp = Mathf.Atan2(num, num2) * radToDeg - (90f - _ballAngle);
        wkHypotenuse = wkAdjacentLength / Mathf.Cos(wkThetaBetweenAdjAndHyp * degToRad);
        wkOppositeLength = Mathf.Sqrt(wkHypotenuse * wkHypotenuse - wkAdjacentLength * wkAdjacentLength);
    }

    private bool keeperCatch()
    {
        //if ((Mathf.Abs(wicketKeeperTransform.position.z - ballTransform.position.z) < 0.5f || wicketKeeperTransform.position.z < ballTransform.position.z) && wicketKeeperStatus == string.Empty && wicketKeeperOppositeLength > wicketKeeprMaxCatchingDistance)
        if ((Mathf.Abs(_wicketKeeperTransform.position.z - temporaryPosition.z) < 0.5f || _wicketKeeperTransform.position.z < temporaryPosition.z) && currentWicketKeeperStatus == string.Empty && wkOppositeLength > wicketKeeperMaxCatchingDistance)
        {
            return false;
        }
        return true;
    }

    private void WicketKeeperPreBattingActions()
    {
        ////ConstantsData_M.MpLog("CASSEE 3 WicketKeeperPreBattingActions() CALLED");
        if (!isWicketKeeperActive)
        {
            return;
        }
        //float num = wicketKeeperTransform.position.x - ballTransform.position.x;
        //float num2 = wicketKeeperTransform.position.z - ballTransform.position.z;

        float num = _wicketKeeperTransform.position.x - temporaryPosition.x;
        float num2 = _wicketKeeperTransform.position.z - temporaryPosition.z;

        float num3 = Mathf.Sqrt(num * num + num2 * num2);
        if (!isWicketKeeperCatchingAnimationSelected)
        {
            bool flag = keeperCatch();
            isWicketKeeperCatchingAnimationSelected = true;
            float num4 = 4f;
            float num5 = 6f;
            float num6 = 10f;
            float num7 = 4f;
            float num8 = 8f;
            if (wkOppositeLength < 0.3f)
            {
                if (isEdgeCaught && flag)
                {
                    num4 = 7f;
                    wicketKeeperCurrentAnimationClip = "chestCatchAppeal";
                    keeperCaughtSpecialCatch = false;
                }
                else
                {
                    wicketKeeperCurrentAnimationClip = "chestCatch";
                }
                wicketKeeperCatchingFrameTime = num4;
                if (bowlerType == "spin" && DistanceBetweenTwoVector2(bowlingSpotObject, wicketKeeperObject) < 6f)
                {
                    if (isEdgeCaught && flag)
                    {
                        num7 = 6f;
                        wicketKeeperCurrentAnimationClip = "spinHipCatchAppeal";
                        keeperCaughtSpecialCatch = false;
                    }
                    else
                    {
                        wicketKeeperCurrentAnimationClip = "spinHipCatch";
                    }
                    wicketKeeperCatchingFrameTime = num7;
                }
            }
            else if (wkThetaBetweenAdjAndHyp > 0f)
            {
                if (wkOppositeLength < 0.8f)
                {
                    if (isEdgeCaught && flag)
                    {
                        num5 = 11f;
                        wicketKeeperCurrentAnimationClip = "rightCatchAppeal";
                        keeperCaughtSpecialCatch = false;
                    }
                    else
                    {
                        wicketKeeperCurrentAnimationClip = "rightShortCatch";
                    }
                    wicketKeeperCatchingFrameTime = num5;
                }
                else if (wkOppositeLength < 1.2f)
                {
                    if (isEdgeCaught && flag)
                    {
                        num6 = 11f;
                        wicketKeeperCurrentAnimationClip = "rightCatchAppeal";
                        keeperCaughtSpecialCatch = false;
                    }
                    else
                    {
                        wicketKeeperCurrentAnimationClip = "rightSideCatch";
                    }
                    wicketKeeperCatchingFrameTime = num6;
                }
                else if (wkOppositeLength < 1.75f)
                {
                    if (isEdgeCaught && flag)
                    {
                        num8 = 14f;
                        wicketKeeperCurrentAnimationClip = "diveRightAppeal";
                        keeperCaughtSpecialCatch = true;
                    }
                    else
                    {
                        wicketKeeperCurrentAnimationClip = "extremeRightJump2";
                    }
                    wicketKeeperCatchingFrameTime = num8;
                }
                else
                {
                    num8 = 18f;
                    wicketKeeperCurrentAnimationClip = "extremeRightJump2";
                    wicketKeeperCatchingFrameTime = num8;
                }
            }
            else if (wkOppositeLength < 0.8f)
            {
                if (isEdgeCaught && flag)
                {
                    num5 = 8f;
                    wicketKeeperCurrentAnimationClip = "spinLeftShortAppeal";
                    keeperCaughtSpecialCatch = false;
                }
                else
                {
                    wicketKeeperCurrentAnimationClip = "leftShortCatch";
                }
                wicketKeeperCatchingFrameTime = num5;
            }
            else if (wkOppositeLength < 1.2f)
            {
                if (isEdgeCaught && flag)
                {
                    num6 = 12f;
                    wicketKeeperCurrentAnimationClip = "spinLeftAppeal";
                    keeperCaughtSpecialCatch = false;
                }
                else
                {
                    wicketKeeperCurrentAnimationClip = "leftSideCatch";
                }
                wicketKeeperCatchingFrameTime = num6;
            }
            else if (wkOppositeLength < 1.75f)
            {
                if (isEdgeCaught && flag)
                {
                    num8 = 16f;
                    wicketKeeperCurrentAnimationClip = "diveCatchLeftAppeal";
                    keeperCaughtSpecialCatch = true;
                }
                else
                {
                    wicketKeeperCurrentAnimationClip = "extremeLeftJump";
                }
                wicketKeeperCatchingFrameTime = num8;
            }
            else
            {
                num8 = 18f;
                wicketKeeperCurrentAnimationClip = "extremeLeftJump";
                wicketKeeperCatchingFrameTime = num8;
            }
            if (!isReplayModeActive)
            {
                previousKeeperAnimationClip = wicketKeeperCurrentAnimationClip;
                previousKeeperCatchingFrame = wicketKeeperCatchingFrameTime;
                wasKeeperCatchDifferent = keeperCaughtSpecialCatch;
            }
            else if (isReplayModeActive)
            {
                wicketKeeperCurrentAnimationClip = previousKeeperAnimationClip;
                wicketKeeperCatchingFrameTime = previousKeeperCatchingFrame;
                keeperCaughtSpecialCatch = wasKeeperCatchDifferent;
            }
            canShowCountdown = false;
        }
        if ((currentWicketKeeperStatus == string.Empty || currentWicketKeeperStatus == "catchAttempt") && currentBallStatus == "bowled")
        {
            if (isOversteppedDelivery || !isLineFreeHitActive)
            {
                keeperAnim.Play("appealFast");
            }
            _stayStartTime = Time.time;
            currentWicketKeeperStatus = "bowledEnd";
        }
        // Keeper-stop fix: a thin edge / nick that rolls BEHIND the keeper used to slip past for a cheap
        // boundary because the velocity-scaled catch window (the first clause) didn't trigger. Also enter
        // catchAttempt when the ball is LOW (grounded, y<0.5 — not a lofted catch), WITHIN the keeper's
        // lateral reach (wkOppositeLength <= max), and right at the keeper's stumps line. This reuses the
        // existing catchAttempt -> catchEnd collect (isBallPaused, ball goes to the keeper) so the ball is
        // stopped instead of running away for 4. MP-only + reach-gated to stay conservative; balls wider
        // than the keeper's reach still go for byes/boundary as before. PLAYTEST: confirm normal catches,
        // wide byes, and legit boundaries behind square are unaffected.
        else if ((num3 < wicketKeeperCatchingFrameTime * animationFrameInterval * horizontalVelocity
                  || (CONTROLLER.PlayModeSelected == 8
                      && LockstepKeeperGateOpen   // lockstep: hold the collect until the batter's input is known (else the keeper eats a hit ball at high ping)
                      && wkOppositeLength <= wicketKeeperMaxCatchingDistance
                      // Beaten-ball collect (tester, repeatedly: "jo ball bat ke paas se guzar ke pichhe jati
                      // hai wo LAZMI boundary hoti hai"): this y-window was 0.5 (anti low-nick), then extended
                      // to 1.3 only for isFullToss — but the BOUNCE clears isFullToss, so a full toss that
                      // bounced at the aim spot approached the (back) keeper with the clause dead at y 0.6-1.1
                      // and sailed to the rope for 4 byes every time (09-07 + 10-07 logs: keeper at z=18.6
                      // armed but never entered catchAttempt). This machine only runs PRE-SHOT, so the ball
                      // here is always an UN-HIT delivery: within lateral reach at the keeper's line, anything
                      // under head height (1.3) must be collectible — that's what a keeper is. Genuinely wide
                      // byes (wkOpp > reach) and balls over his head still run.
                      && matchBallTransform.position.y < 1.3f
                      // Bowling-side "keeper catches on batting, ball runs behind on bowling" visual fix:
                      // online the bowling client's ball z is stream-fed (20 Hz), so a quick delivery can
                      // JUMP from before the keeper's line to past it between packets, skipping the <1.0f
                      // catch window -> catchAttempt never entered -> the keeper let it run for byes while
                      // the batting screen collected it. Also enter the attempt once the ball has reached
                      // or passed the keeper's z line (same clause catchEnd already uses), still gated by
                      // the keeper's lateral reach + a low ball so genuine wide byes/boundaries are unaffected.
                      && (Mathf.Abs(_wicketKeeperTransform.position.z - temporaryPosition.z) < 1.0f
                          || _wicketKeeperTransform.position.z < temporaryPosition.z)
                      && !isReplayModeActive))
                 && currentWicketKeeperStatus == string.Empty)
        {
            if (bowlerType == "spin")
            {
                keeperAnim[wicketKeeperCurrentAnimationClip].speed = 1.5f;
            }
            else
            {
                keeperAnim[wicketKeeperCurrentAnimationClip].speed = 1f;
            }
            keeperAnim.CrossFade(wicketKeeperCurrentAnimationClip);
            currentWicketKeeperStatus = "catchAttempt";
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                ConstantsData_M.MpLog($"[KeeperState] -> catchAttempt ballZ={temporaryPosition.z:F2} keeperZ={_wicketKeeperTransform.position.z:F2} dz={Mathf.Abs(_wicketKeeperTransform.position.z - temporaryPosition.z):F2} wkOpp={wkOppositeLength:F3} clip={wicketKeeperCurrentAnimationClip} side={(CONTROLLER.myTeamIndex==CONTROLLER.BowlingTeamIndex?"BOWL":"BAT")}");
            if (wicketKeeperCurrentAnimationClip == "chestCatch" || wicketKeeperCurrentAnimationClip == "spinHipCatch")
            {
                if (wkThetaBetweenAdjAndHyp > 0f)
                {
                    iTween.MoveTo(wicketKeeperObject, iTween.Hash("x", _wicketKeeperTransform.position.x - wkOppositeLength, "time", 0.2));
                }
                else
                {
                    iTween.MoveTo(wicketKeeperObject, iTween.Hash("x", _wicketKeeperTransform.position.x + wkOppositeLength, "time", 0.2));
                }
            }
        }
        //else if ((Mathf.Abs(wicketKeeperTransform.position.z - ballTransform.position.z) < 0.5f || wicketKeeperTransform.position.z < ballTransform.position.z) && wicketKeeperStatus == "catchAttempt" && wicketKeeperOppositeLength <= wicketKeeprMaxCatchingDistance)
        else if ((Mathf.Abs(_wicketKeeperTransform.position.z - temporaryPosition.z) < 0.5f || _wicketKeeperTransform.position.z < temporaryPosition.z) && currentWicketKeeperStatus == "catchAttempt" && wkOppositeLength <= wicketKeeperMaxCatchingDistance)
        {
            _stayStartTime = Time.time;
            if (isEdgeCaught)
            {
                makeFieldersToCelebrate(null);
            }
            currentWicketKeeperStatus = "catchEnd";
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                ConstantsData_M.MpLog($"[KeeperState] -> catchEnd (CAUGHT) ballZ={temporaryPosition.z:F2} keeperZ={_wicketKeeperTransform.position.z:F2} dz={Mathf.Abs(_wicketKeeperTransform.position.z - temporaryPosition.z):F2} wkOpp={wkOppositeLength:F3} side={(CONTROLLER.myTeamIndex==CONTROLLER.BowlingTeamIndex?"BOWL":"BAT")}");
            wicketKeeperBallObject.GetComponent<Renderer>().enabled = true;
            ShowBall(status: false);
            isBallPaused = true;
            if (!(currentWicketKeeperStatus == "catchEnd") || !(bowlerType == "spin") || isEdgeCaught || freeHit || !(wicketKeeperCurrentAnimationClip != "extremeLeftJump") || !(wicketKeeperCurrentAnimationClip != "extremeRightJump2") || (!(currentBatsmanAnimation == "bt6CoverDrive") && !(currentBatsmanAnimation == "bt6LegGlance") && !(currentBatsmanAnimation == "bt6OnDrive") && !(currentBatsmanAnimation == "loftLegSide") && !(currentBatsmanAnimation == "loftOffSide") && !(currentBatsmanAnimation == "loftStraight") && !(currentBatsmanAnimation == "frontFootOffDrive")) || CONTROLLER.isFreeHitBall)
            {
                return;
            }
            currentWicketKeeperStatus = "stumpingAttempt";
            iTween.Stop(wicketKeeperObject);
            Vector3 position = _wicketKeeperTransform.position;
            position.z -= 0.5f;
            _wicketKeeperTransform.position = position;
            keeperAnim.CrossFade("collectStumpAppeal");
            if (currentBatsmanAnimation == "bt6CoverDrive" || currentBatsmanAnimation == "bt6LegGlance")
            {
                batsmanAnim[currentBatsmanAnimation].time = GetBatsmanReturnFrameTimeToCrease();
                batsmanAnim[currentBatsmanAnimation].speed = 2.5f;
                stumpingAnimationType = 1;
            }
            else
            {
                Vector3 position2 = _batsmanTransform.position;
                position2.x = shadowReferenceTransforms[11].position.x;
                position2.z = shadowReferenceTransforms[11].position.z;
                if (!isReplayModeActive && position2.z > 7.5f && ((batsmanHand == "right" && (double)position2.x > 0.1 && (double)position2.x < 0.6) || (batsmanHand == "left" && (double)position2.x < -0.1 && (double)position2.x > -0.6)))
                {
                    returnToCreaseAnimationId = 1;
                }
                if (returnToCreaseAnimationId == 1)
                {
                    Vector3 eulerAngles = _batsmanTransform.eulerAngles;
                    if (batsmanHand == "right")
                    {
                        eulerAngles.y = 290f;
                    }
                    else
                    {
                        eulerAngles.y = 70f;
                    }
                    _batsmanTransform.eulerAngles = eulerAngles;
                }
                _batsmanTransform.position = position2;
                batsmanAnim.Play("ReturnToCrease2");
                float speed = (isReplayModeActive ? batsmanReturnToCreaseSpeed : (batsmanReturnToCreaseSpeed = UnityEngine.Random.Range(1f, 1.2f)));
                batsmanAnim["ReturnToCrease2"].speed = speed;
                stumpingAnimationType = 2;
            }
            if (Singleton<GameData>.instance != null)
            {
                Singleton<GameData>.instance.PlayGameSound("Beaten");
            }
        }
        else if (currentWicketKeeperStatus == "stumpingAttempt")
        {
            float num9 = 8f;
            _wicketKeeperTransform.LookAt(stumpLeftSpot.transform);
            if (!(keeperAnim["collectStumpAppeal"].time > num9 * animationFrameInterval))
            {
                return;
            }
            // Stumping-verdict AUTHORITY (online MP): only the BATTING client runs IsStumpOut()/IsStumpOut2() — it
            // owns the batsman's exact crease position + animation timing. On the BOWLING follower the batsman is a
            // synced APPROXIMATION, so computing the verdict here returned a DIFFERENT result and CLOBBERED the value
            // relayed by the batting authority (RPC_ShowThirdUmpireReview sets isPlayerStumped) → tester's "one side
            // out, the other not out, review only on one side". Skip the local compute on the bowling follower; it
            // keeps the relayed verdict, which arrives during the ~1.5s stumpingAppeal wait BEFORE the path decision
            // (~2579) reads isPlayerStumped. Offline / vs-AI keep computing locally as before.
            bool _bowlingFollowerOnlineStump = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex;
            if (!isReplayModeActive && !_bowlingFollowerOnlineStump)
            {
                if (stumpingAnimationType == 1)
                {
                    wasPlayerStumped = IsStumpOut();
                }
                else
                {
                    wasPlayerStumped = IsStumpOut2();
                }
                isPlayerStumped = wasPlayerStumped;
                previousStumpedState = isPlayerStumped;
            }
            else if (isReplayModeActive && !isOversteppedDelivery && !isLineFreeHitActive)
            {
                Time.timeScale = 0.02f;
                isPlayerStumped = previousStumpedState;
            }
            _stayStartTime = Time.time;
            if (isOversteppedDelivery || !CONTROLLER.isLineFreeHitBallCompleted)
            {
                currentWicketKeeperStatus = "catchEnd";
            }
            else if (isLineFreeHitActive || !CONTROLLER.isLineFreeHitBallCompleted)
            {
                currentWicketKeeperStatus = "catchEnd";
            }
            else
            {
                currentWicketKeeperStatus = "stumpingAppeal";
            }
            makeFieldersToCelebrate(null);
            float num10 = 90f;
            iTween.RotateTo(wicketKeeperObject, iTween.Hash("y", num10, "time", 0.5, "delay", 0.2));
            stump1Anim.Play("legSideStumping");
        }
        else if (currentWicketKeeperStatus == "stumpingAppeal")
        {
            if (isReplayModeActive)
            {
                if ((double)_stayStartTime + 0.1 < (double)Time.time)
                {
                    Time.timeScale = 0.5f;
                }
            }
            else if (!isReplayModeActive && isBallWide && !isWideWithStumpingSignalDisplayed && (double)_stayStartTime + 1.0 < (double)Time.time)
            {
                isWideWithStumpingSignalDisplayed = true;
                currentWicketKeeperStatus = "showWideSignalBeforeStumpingResult";
                gameplayCamera.enabled = false;
                rightFieldCamera.enabled = false;
                leftFieldCamera.enabled = false;
                closeUpViewCamera.enabled = false;
                umpireViewCamera.enabled = true;
                umpireCamTransform.position = mainUmpireInitialPosition;
                umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 2f, umpireCamTransform.position.z);
                umpireCamTransform.position += new Vector3(0f, 0f, 3f);
                umpireCamTransform.eulerAngles = new Vector3(5 + UnityEngine.Random.Range(0, 5), 180f, umpireCamTransform.eulerAngles.z);
                _stayStartTime = Time.time;
                mainUmpireAnim.Play("WideBall_New");
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
            }
            if (!(_stayStartTime + 1.5f < Time.time) || !(currentWicketKeeperStatus != "showWideSignalBeforeStumpingResult"))
            {
                return;
            }
            // Stump-path AUTHORITY (bowling follower): the branch below picks between a quick direct NOT-OUT
            // (LOCAL Random.Range roll!) and the full third-umpire review — both batting-authority decisions.
            // Deciding here raced the relay: the keeper catch resolves at t+0 but the batting side's
            // CmdShowThirdUmpireReview lands ~t+3-5s, so this follower rolled its OWN dice with a stale
            // isPlayerStumped and crouched straight to NOT OUT while the batter ran the full review — and the
            // late relay then early-returned off the already-showing result board (tester 03-07: "batter side
            // full review, bowler side direct NOT OUT"). HOLD here instead: the relay's
            // RPC_ShowThirdUmpireReview drives the identical board (it flips currentWicketKeeperStatus, which
            // exits this state). If the relay never arrives, fall back to the same board funnel locally.
            bool _stumpPathFollower = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && !isReplayModeActive;
            if (_stumpPathFollower)
            {
                if (_stumpFollowerHoldStart <= 0f)
                {
                    _stumpFollowerHoldStart = Time.time;
                    ConstantsData_M.MpLog("[StumpSync] Bowling follower holding at the stump decision — waiting for the batting authority's review relay.");
                }
                if (Time.time < _stumpFollowerHoldStart + 12f)
                {
                    return;
                }
                ConstantsData_M.MpLog("[StumpSync] Review relay never arrived (12s) — falling back to the local review board.");
                _stumpFollowerHoldStart = -1f;
                RPC_ShowThirdUmpireReview(isPlayerStumped);   // same funnel the relay uses; renders from the last-known verdict
                return;
            }
            _stayStartTime = Time.time;
            gameplayCamera.enabled = false;
            showPreviewCamera(status: false);
            rightFieldCamera.enabled = false;
            leftFieldCamera.enabled = false;
            if (!isReplayModeActive)
            {
                umpireViewCamera.enabled = true;
                if (sideUmpireCameraPosition != null)
                {
                    Vector3 position3 = sideUmpireCameraPosition.transform.position;
                    position3.x = 21f;
                    umpireCamTransform.position = position3;
                    umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 1.5f, umpireCamTransform.position.z);
                }
                umpireCameraAnchor.transform.position = sideUmpireObject.transform.position;
                umpireCameraAnchor.transform.eulerAngles = new Vector3(0f, 90f, 0f);
                umpireCamTransform.eulerAngles = new Vector3(umpireCamTransform.eulerAngles.x, 90f, umpireCamTransform.eulerAngles.z);
                umpireCamTransform.parent = umpireCameraAnchor.transform;
                // Lockstep: this quick-not-out coin used to be a LOCAL Random roll — each client branched
                // independently ("stumping: one side out, other side not out"). Derive it from the shared
                // per-delivery seed instead (NO stream draw — draw order stays untouched on both clients).
                bool stumpingQuickNotOut = LockstepActive
                    ? (DeterministicRng.CurrentSeed & 0x7FFFFFFF) % 100 < 90
                    : UnityEngine.Random.Range(1, 100) < 90;
                if (!isPlayerStumped && (stumpingQuickNotOut || CONTROLLER.PlayModeSelected == 6))
                {
                    // MP fix (#3/#5/#6): quick not-out path skips ShowThirdUmpireDecisionBoard, so
                    // bowling client never gets CmdShowThirdUmpireReview and shows nothing.
                    // Send the CMD inline so the bowling side can show its own review board.
                    if (CONTROLLER.PlayModeSelected == 8 && !GameConstants.isWithAI
                        && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex
                        && CricketNetworkManager.instance != null)
                    {
                        CricketNetworkManager.instance.CmdShowThirdUmpireReview(staticVariables.UserProfiledata.user._id, false);
                    }
                    sideUmpireAnim.CrossFade("Crouch_toNotOut_New");
                    currentWicketKeeperStatus = "Show3rdUmpireResult";
                }
                else
                {
                    iTween.RotateTo(umpireCameraAnchor, iTween.Hash("y", 0, "time", 1.5f, "delay", 1.5, "easetype", "easeInOutSine"));
                    sideUmpireAnim.CrossFade("3rd Umpire_New");
                    // #2 (stumping desync): the OUT / full-review stumping path enters waitForStumpingResult and (online)
                    // calls ShowReplay() WITHOUT broadcasting, so the BOWLING follower never gets CmdShowThirdUmpireReview
                    // and shows only the plain pitch while the batting side runs the keeper-stumping review (one side
                    // reviews, the other doesn't). Broadcast the review onset + outcome (isPlayerStumped) here, exactly
                    // like the not-out path above, so both clients show the same review board.
                    if (CONTROLLER.PlayModeSelected == 8 && !GameConstants.isWithAI
                    	&& CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex
                    	&& CricketNetworkManager.instance != null)
                    {
                    	CricketNetworkManager.instance.CmdShowThirdUmpireReview(staticVariables.UserProfiledata.user._id, isPlayerStumped);
                    }
                    currentWicketKeeperStatus = "waitForStumpingResult";
                    Singleton<GameData>.instance.canPauseGameplay = false;
                }
            }
            else if (isReplayModeActive)
            {
                currentWicketKeeperStatus = "waitForStumpingResult";
            }
            distanceBetweenUmpireAndFielder(boolean: false);
            if (Singleton<GameData>.instance != null && !isReplayModeActive)
            {
                Singleton<GameData>.instance.PlayGameSound("Cheer");
            }
        }
        else if (currentWicketKeeperStatus == "showWideSignalBeforeStumpingResult")
        {
            if (_stayStartTime + 2f < Time.time)
            {
                _stayStartTime = Time.time;
                currentWicketKeeperStatus = "stumpingAppeal";
            }
        }
        else if (currentWicketKeeperStatus == "BadCallStumpingResult")
        {
            if (!(_stayStartTime + 3f < Time.time))
            {
                return;
            }
            currentWicketKeeperStatus = "loopEnd";
            if (!(Singleton<GameData>.instance != null))
            {
                return;
            }
            if (noBall)
            {
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                {
                }
            }
            else if (freeHit)
            {
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            }
            else if (isBallWide)
            {
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWideBall++;
                Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                {
                }
            }
            else if (!freeHit && !noBall)
            {
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            }
        }
        else if (currentWicketKeeperStatus == "waitForStumpingResult")
        {
            if (_stayStartTime + 4f < Time.time && !isReplayModeActive && CONTROLLER.PlayModeSelected != 6)
            {
                currentWicketKeeperStatus = "loopEnd";
                umpireCamTransform.parent = null;
                CONTROLLER.stumpingAttempted = true;
                replayCamTransform.position = new Vector3(-7f, 1.2f, 8.8f);
                Singleton<GameData>.instance.GameIsOnStumpingReplay();
                ShowReplay();
            }
            else if ((_stayStartTime + 0.1f < Time.time && isReplayModeActive) || CONTROLLER.PlayModeSelected == 6)
            {
                ShowThirdUmpireDecisionBoard();
            }
        }
        else if (currentWicketKeeperStatus == "waitFor3rdUmpireResult")
        {
            // Bowler-side stump-review sync: hold on the "reviewing" board until the batting authority reaches its
            // own result (RPC_ShowThirdUmpireReview clears _bowlerStumpReviewHold), so the follower's OUT/NOTOUT
            // shows WITH the batter's, not ~5s early. Fallback: release after 10s so a lost relay can't hang it.
            // Batter never sets this flag (it's set only in the bowling-gated RPC), so the batting flow is unchanged.
            if (_bowlerStumpReviewHold && Time.time < _stayStartTime + 10f)
            {
                return;
            }
            _bowlerStumpReviewHold = false;
            if (!(_stayStartTime + 3f < Time.time))
            {
                return;
            }
            scoreboardScreen.transform.localScale = new Vector3(0f, 0f, 0f);
            iTween.ScaleTo(scoreboardScreen, iTween.Hash("scale", scoreboardScreenScale, "time", 0.4f, "easetype", "spring"));
            if (isPlayerStumped)
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
            currentWicketKeeperStatus = "Show3rdUmpireResult";
        }
        else if (currentWicketKeeperStatus == "Show3rdUmpireResult")
        {
            if (!(_stayStartTime + 2f < Time.time))
            {
                return;
            }
            if (isPlayerStumped)
            {
                HideReplay();
                if (Singleton<GameData>.instance != null)
                {
                    if (noBall)
                    {
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                    }
                    else if (freeHit)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    }
                    else if (isBallWide)
                    {
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWideBall++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 1, CONTROLLER.StrikerIndex, 1, 6, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                    }
                    else if (!noBall && !freeHit)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 1, 6, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                    }
                }
            }
            else
            {
                HideReplay();
                if (Singleton<GameData>.instance != null)
                {
                    if (noBall)
                    {
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                    }
                    else if (freeHit)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    }
                    else if (isBallWide)
                    {
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWideBall++;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                    }
                    else if (!freeHit && !noBall)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    }
                }
            }
            _stayStartTime = Time.time;
            currentWicketKeeperStatus = string.Empty;
        }
        //else if ((Mathf.Abs(wicketKeeperTransform.position.z - ballTransform.position.z) < 0.5f || wicketKeeperTransform.position.z < ballTransform.position.z) && wicketKeeperStatus == "catchAttempt" && wicketKeeperOppositeLength > wicketKeeprMaxCatchingDistance)
        else if ((Mathf.Abs(_wicketKeeperTransform.position.z - temporaryPosition.z) < 0.5f || _wicketKeeperTransform.position.z < temporaryPosition.z) && currentWicketKeeperStatus == "catchAttempt" && wkOppositeLength > wicketKeeperMaxCatchingDistance)
        {
            currentWicketKeeperStatus = "catchMissed";
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                ConstantsData_M.MpLog($"[KeeperState] -> catchMissed (OUT OF REACH) ballZ={temporaryPosition.z:F2} keeperZ={_wicketKeeperTransform.position.z:F2} dz={Mathf.Abs(_wicketKeeperTransform.position.z - temporaryPosition.z):F2} wkOpp={wkOppositeLength:F3} max={wicketKeeperMaxCatchingDistance:F2} side={(CONTROLLER.myTeamIndex==CONTROLLER.BowlingTeamIndex?"BOWL":"BAT")}");
            boundaryType = 4;
            if (isEdgeCaught)
            {
                canKeeperCatchBall = false;
                SetActiveFielders();
            }
            // Keeper-miss camera switch — the old bowling-follower suppression is RETIRED (tester #2/#3:
            // "beaten ball / byes to the back boundary: bowler side stays on the pitch then snaps at the
            // rope"). The suppression was a band-aid for the pre-deterministic era when the spin/beaten ball
            // DIVERGED past the keeper on the follower; with the deterministic fixed-step flight + synced
            // keeper there is no divergence left, and keeping it meant the bowling side never followed a
            // non-wide ball that beat the keeper (byes/back-boundary) while the batting side switched to the
            // full side view. Switch on BOTH sides now — identical to the batting client. Camera-only; the
            // keeper status / boundaryType above are unchanged (score stays authoritative via RpcBallOutcome).
            if (CONTROLLER.cameraType != 0)
            {
                leftFieldCamera.enabled = true;
                leftFieldCamera.fieldOfView = 35f;
                gameplayCamera.enabled = false;
            }
        }
        else if (currentWicketKeeperStatus == "bowledEnd")
        {
            if (!(_stayStartTime + 1f + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            //         if (!(stayStartTime+ timeBetweenBalls > Time.time))
            //{
            //	return;
            //}

            if (isOversteppedDelivery)
            {
                if (isReplayModeActive)
                {
                    currentWicketKeeperStatus = "loopEnd";
                    HideReplay();
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                    Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                    if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                    {
                    }
                    return;
                }
                showMainUmpireForNoBallAction();
                currentWicketKeeperStatus = "umpireNoBallActionForBowled";
                bool flag2 = checkForMatchComplete(1, 0);
                if (CONTROLLER.matchType == "oneday" && !flag2)
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
                return;
            }
            if (isLineFreeHitActive)
            {
                currentWicketKeeperStatus = "loopEnd";
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                isLineFreeHitActive = false;
                freeHit = false;
                lastBowledBallType = "lineball";
                Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                return;
            }
            currentWicketKeeperStatus = "loopEnd";
            if (isReplayModeActive)
            {
                HideReplay();
            }
            else
            {
                if (!(Singleton<GameData>.instance != null))
                {
                    return;
                }
                if (isOversteppedDelivery)
                {
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                    Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                    CONTROLLER.isJokerCall = false;
                    if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                    {
                    }
                }
                else if (isLineFreeHitActive)
                {
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                    freeHit = false;
                }
                else if (!isOversteppedDelivery && !isLineFreeHitActive)
                {
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 1, 1, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                }
            }
        }
        else if (currentWicketKeeperStatus == "umpireNoBallActionForBowled")
        {
            if (!(_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            currentWicketKeeperStatus = "loopEnd";
            if (!(Singleton<GameData>.instance != null))
            {
                return;
            }
            if (isOversteppedDelivery)
            {
                if (!isReplayModeActive)
                {
                    noBallRunStatus = "cleanbowled";
                    if (CONTROLLER.canShowReplay)
                    {
                        Singleton<GameData>.instance.GameIsOnReplay();
                        ShowReplay();
                    }
                    else
                    {
                        Singleton<GameData>.instance.ReplayIsNotShown();
                    }
                    return;
                }
                HideReplay();
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                {
                }
            }
            else if (isLineFreeHitActive)
            {
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                isLineFreeHitActive = false;
                lastBowledBallType = "lineball";
            }
        }
        else if (currentWicketKeeperStatus == "CaughtBehind")
        {
            _stayStartTime = Time.time;
            currentWicketKeeperStatus = "waitForCaughtBehindResult";
        }
        else if (currentWicketKeeperStatus == "waitForCaughtBehindResult")
        {
            if (!(_stayStartTime + 1f + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            _stayStartTime = Time.time;
            currentWicketKeeperStatus = "decisionPending";
            gameplayCamera.enabled = false;
            showPreviewCamera(status: false);
            rightFieldCamera.enabled = false;
            leftFieldCamera.enabled = false;
            if (isReplayModeActive)
            {
                currentWicketKeeperStatus = "loopEnd";
                HideReplay();
                return;
            }
            if (!isReplayModeActive)
            {
                umpireViewCamera.enabled = true;
                umpireCamTransform.position = mainUmpireInitialPosition;
                umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 2f, umpireCamTransform.position.z);
                umpireCamTransform.position += new Vector3(0f, 0f, 3f);
                umpireCamTransform.eulerAngles = new Vector3(5 + UnityEngine.Random.Range(0, 5), 180f, 0f);
            }
            if (!noBall && !freeHit && bounceCount == 0)
            {
                if (umpireInitialDecision == "out")
                {
                    mainUmpireAnim.CrossFade("Out2_New");
                    // Online: mark the caught-behind OUT as pending-record so that if a review interrupts
                    // before decisionPending records it, ForceResolveStuckReview still sends RpcBallOutcome.
                    if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                        mpCaughtBehindOutPending = true;
                }
                else
                {
                    mainUmpireAnim.CrossFade("NotOut");
                }
                hasUmpireAnimationPlayed = true;
            }
            else
            {
                mainUmpireAnim.CrossFade("NotOut");
            }
            if (Singleton<GameData>.instance != null && !isReplayModeActive)
            {
                Singleton<GameData>.instance.PlayGameSound("Cheer");
            }
        }
        else if (currentWicketKeeperStatus == "decisionPending")
        {
            if (!(_stayStartTime + 1f + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            float num11 = 3f;
            if (isReplayModeActive)
            {
                num11 = 0.5f;
            }
            if (!(_stayStartTime + num11 + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            currentWicketKeeperStatus = "loopEnd";
            if (isReplayModeActive)
            {
                HideReplay();
            }
            else
            {
                if (!(Singleton<GameData>.instance != null))
                {
                    return;
                }
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
                else if (bounceCount != 0)
                {
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                }
                else if (!noBall && !freeHit)
                {
                    if (umpireInitialDecision == "out")
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 5, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                        // Only clear when the call ACTUALLY recorded (no review up). If a review is showing,
                        // the UpdateCurrentBall above hit the GameData defer block and recorded NOTHING, so keep
                        // the flag set so the review resolver (ForceResolveStuckReview) can flush the buffered OUT.
                        if (!isUltraEdgeCutscenePlaying)
                            mpCaughtBehindOutPending = false;
                    }
                    else
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
                    }
                }
                canKeeperCatchBall = false;
            }
        }
        else if (currentWicketKeeperStatus == "catchEnd")
        {
            if (!(_stayStartTime + 1f + delayBetweenDeliveries < Time.time))
            {
                return;
            }


            //if (stayStartTime + 1f +timeBetweenBalls > Time.time)
            //{
            //	return;
            //}


            if (isOversteppedDelivery)
            {
                if (isReplayModeActive)
                {
                    currentWicketKeeperStatus = "loopEnd";
                    HideReplay();
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                    Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                    {
                    }
                    return;
                }
                showMainUmpireForNoBallAction();
                currentWicketKeeperStatus = "umpireNoBallAction";
                iTween.MoveTo(umpireViewCamera.gameObject, iTween.Hash("y", UnityEngine.Random.Range(1.4f, 1.8f), "time", 2));
                bool flag3 = checkForMatchComplete(1, 0);
                if (CONTROLLER.matchType == "oneday" && !flag3)
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
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
                return;
            }
            if (isLineFreeHitActive)
            {
                if (isBallWide)
                {
                    showMainUmpireForNoBallAction();
                    currentWicketKeeperStatus = "waitForWideSignal";
                    mainUmpireAnim.Play("WideBall_New");
                    if (BowlingBy == "computer")
                    {
                        bowlerSide = "left";
                    }
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Beaten");
                    }
                }
                else
                {
                    currentWicketKeeperStatus = "loopEnd";
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    isLineFreeHitActive = false;
                    freeHit = false;
                    lastBowledBallType = "lineball";
                    Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                }
                return;
            }
            if (isEdgeCaught)
            {
                currentWicketKeeperStatus = "CaughtBehind";
                _stayStartTime = Time.time;
                WicketKeeperBallSkin.enabled = true;
                ShowBall(status: false);
                isBallPaused = true;
                return;
            }
            if (isBallWide)
            {
                showMainUmpireForNoBallAction();
                currentWicketKeeperStatus = "waitForWideSignal";
                if (noBall)
                {
                    mainUmpireAnim.Play("NoBallFreeHit_New");
                }
                else
                {
                    mainUmpireAnim.Play("WideBall_New");
                }
                if (BowlingBy == "computer")
                {
                    bowlerSide = "left";
                }
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
                return;
            }
            currentWicketKeeperStatus = "loopEnd";
            if (!(Singleton<GameData>.instance != null))
            {
                return;
            }
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
        else if (currentWicketKeeperStatus == "umpireNoBallAction")
        {
            if (!(_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time))
            {
                return;
            }

            currentWicketKeeperStatus = "loopEnd";
            if (!(Singleton<GameData>.instance != null))
            {
                return;
            }
            if (isOversteppedDelivery)
            {
                if (!isReplayModeActive)
                {
                    noBallRunStatus = "beatenball";
                    if (CONTROLLER.canShowReplay)
                    {
                        Singleton<GameData>.instance.GameIsOnReplay();
                        ShowReplay();
                    }
                    else
                    {
                        Singleton<GameData>.instance.ReplayIsNotShown();
                    }
                    return;
                }
                HideReplay();
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                {
                }
            }
            else if (isLineFreeHitActive)
            {
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                isLineFreeHitActive = false;
                lastBowledBallType = "lineball";
            }
        }
        else if (currentWicketKeeperStatus == "waitForWideSignal")
        {
            if (!(_stayStartTime + 2f + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            currentWicketKeeperStatus = "loopEnd";
            if (Singleton<GameData>.instance != null)
            {
                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWideBall++;
                Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                {
                }
            }
        }
        else if (currentWicketKeeperStatus == "BadCallBowledResult" && (double)_stayStartTime + 1.5 < (double)Time.time)
        {
            currentWicketKeeperStatus = "loopEnd";
            if (Singleton<GameData>.instance != null)
            {
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            }
        }
    }

    private void WicketKeeperPostBattingActions()
    {
        if (stopKeeper)
        {
            return;
        }
        if (currentWicketKeeperStatus == "waitForBall")
        {
            if (isBallOnBoundaryLine)
            {
                keeperAnim.CrossFade("idle");
                currentWicketKeeperStatus = "finish";
            }
            if (outcomeOfBall == "wicket")
            {
                keeperAnim.Play("idle");
                currentWicketKeeperStatus = "finish";
            }
        }
        else if (currentWicketKeeperStatus == "waitToCollect")
        {
            float num = 6f;
            float num2 = num * animationFrameInterval * horizontalVelocity;

            float nums = temporaryPosition.x - wicketKeeperObject.transform.position.x;
            float num2s = temporaryPosition.z - wicketKeeperObject.transform.position.z;
            float num3 = Mathf.Sqrt(nums * nums + num2s * num2s);

            if (!(num3 < num2))
            {
                return;
            }
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
                keeperAnim.Play("collectStumpAppeal");
                currentWicketKeeperStatus = "collectTheThrowAndStump";
                throwActionSaved = "collectTheThrowAndStump";
            }
            else if ((!isReplayModeActive && !isRunOutOccurring) || (isReplayModeActive && throwActionSaved == "collectTheThrow"))
            {
                keeperAnim.Play("collectAndStand");
                currentWicketKeeperStatus = "collectTheThrow";
                throwActionSaved = "collectTheThrow";
            }
        }
        else if (currentWicketKeeperStatus == "collectTheThrow" || currentWicketKeeperStatus == "collectTheThrowAndStump")
        {
            //float num3 = DistanceBetweenTwoVector2(ball, wicketKeeper);
            float num = temporaryPosition.x - wicketKeeperObject.transform.position.x;
            float num2 = temporaryPosition.z - wicketKeeperObject.transform.position.z;
            float num3 = Mathf.Sqrt(num * num + num2 * num2);

            if (distanceBetweenBallAndCollectingPlayerWhileThrowing > num3)
            {
                distanceBetweenBallAndCollectingPlayerWhileThrowing = num3;
            }
            else
            {
                distanceBetweenBallAndCollectingPlayerWhileThrowing = -1f;
            }
            if (!isReplayModeActive)
            {
                isRunOutOccurring = isBatsmanRunOut();
            }
            if (!(num3 < 0.5f) && distanceBetweenBallAndCollectingPlayerWhileThrowing != -1f)
            {
                return;
            }
            currentBallStatus = string.Empty;
            isBallPaused = true;
            WicketKeeperBallSkin.enabled = true;
            ShowBall(status: false);
            if (currentWicketKeeperStatus == "collectTheThrow")
            {
                _stayStartTime = Time.time;
                currentWicketKeeperStatus = "end";
            }
            else
            {
                if (!(currentWicketKeeperStatus == "collectTheThrowAndStump"))
                {
                    return;
                }
                float num4 = 8f;
                _wicketKeeperTransform.LookAt(stumpLeftSpot.transform);
                if (keeperAnim["collectStumpAppeal"].time > num4 * animationFrameInterval)
                {
                    if (!isReplayModeActive)
                    {
                        hasRunOutOccurred = isRunOutOccurring;
                        isRunOutSaved = hasRunOutOccurred;
                        currentBallRunsSaved = runsScoredThisBall;
                    }
                    else
                    {
                        hasRunOutOccurred = isRunOutSaved;
                        runsScoredThisBall = currentBallRunsSaved;
                    }
                    _stayStartTime = Time.time;
                    currentWicketKeeperStatus = "runOutAppeal";
                    isRunOutAppealSaved = true;
                    if (isReplayModeActive)
                    {
                        StartCoroutine(UltraSlowMotion());
                    }
                    float num5 = 90f;
                    iTween.RotateTo(wicketKeeperObject, iTween.Hash("y", num5, "time", 0.5, "delay", 0.2));
                    if (wicketKeeperDirectionAfterBatting == "straight")
                    {
                        stump1Anim.Play("legSideStumping");
                    }
                    else if (wicketKeeperDirectionAfterBatting == "offSide")
                    {
                        stump1Anim.Play("fielderRunoutIn");
                    }
                    else if (wicketKeeperDirectionAfterBatting == "legSide")
                    {
                        stump1Anim.Play("fielderRunoutAway");
                    }
                }
            }
        }
        else if (currentWicketKeeperStatus == "end")
        {
            if (!(_stayStartTime + 1f + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            ////ConstantsData_M.MpLog("WICKETKEEPER");
            //         if(!(stayStartTime + 0.25f + timeBetweenBalls < Time.time))
            //{
            //             return;
            //         }



            if (isOversteppedDelivery)
            {
                if (isReplayModeActive)
                {
                    currentWicketKeeperStatus = "loopEnd";
                    HideReplay();
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                    Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                    {
                    }
                    return;
                }
                showMainUmpireForNoBallAction();
                currentWicketKeeperStatus = "umpireNoBallActionAfterWKCollectsBallFromFielder";
                lineNoBallRunsScored = runsScoredThisBall;
                bool flag = checkForMatchComplete(runsScoredThisBall + 1, 0);
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
                return;
            }
            if (isLineFreeHitActive)
            {
                currentWicketKeeperStatus = "loopEnd";
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                freeHit = false;
                isLineFreeHitActive = false;
                lastBowledBallType = "lineball";
                Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                return;
            }
            currentWicketKeeperStatus = "loopEnd";
            if (!(Singleton<GameData>.instance != null))
            {
                return;
            }
            if (noBall)
            {
                ////ConstantsData_M.MpLog("FREEEHITT");
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
                ////ConstantsData_M.MpLog("FREEEHITT");
                freeHit = false;
                isFreeHitActive = false;
            }
            else if (!noBall && !freeHit)
            {
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            }
        }
        else if (currentWicketKeeperStatus == "umpireNoBallActionAfterWKCollectsBallFromFielder")
        {
            if (!(_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            currentWicketKeeperStatus = "loopEnd";
            if (!(Singleton<GameData>.instance != null) || !isOversteppedDelivery)
            {
                return;
            }
            if (!isReplayModeActive)
            {
                noBallRunStatus = "keepercollectstheball";
                if (CONTROLLER.canShowReplay)
                {
                    Singleton<GameData>.instance.GameIsOnReplay();
                    ShowReplay();
                }
                else
                {
                    Singleton<GameData>.instance.ReplayIsNotShown();
                }
                return;
            }
            HideReplay();
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (currentWicketKeeperStatus == "runOutAppeal")
        {
            if (!isFielderAppealingRunOut)
            {
                makeFieldersToCelebrate(null);
                isFielderAppealingRunOut = true;
            }
            if (!(_stayStartTime + 1f + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            _stayStartTime = Time.time;
            currentWicketKeeperStatus = "waitForResult";
            gameplayCamera.enabled = false;
            showPreviewCamera(status: false);
            rightFieldCamera.enabled = false;
            leftFieldCamera.enabled = false;
            if (isReplayModeActive)
            {
                currentWicketKeeperStatus = "loopEnd";
                HideReplay();
                if (!hasRunOutOccurred)
                {
                    UpdateRunAfterReplay();
                }
                return;
            }
            if (!isReplayModeActive)
            {
                umpireViewCamera.enabled = true;
                umpireCamTransform.position = sideUmpireCameraPosition.transform.position;
                umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 1.5f, umpireCamTransform.position.z);
                umpireCamTransform.eulerAngles = new Vector3(umpireCamTransform.eulerAngles.x, 90f, umpireCamTransform.eulerAngles.z);
                iTween.MoveTo(umpireViewCamera.gameObject, iTween.Hash("x", umpireCamTransform.position.x + 3f, "time", 2, "easetype", "easeInOutSine"));
            }
            distanceBetweenUmpireAndFielder(boolean: false);
            if (hasRunOutOccurred)
            {
                sideUmpireAnim.CrossFade("Out2_New");
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Cheer");
                }
            }
            else
            {
                sideUmpireAnim.CrossFade("Crouch_toNotOut_New");
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
            }
        }
        else if (currentWicketKeeperStatus == "waitForResult")
        {
            float num6 = 3f;
            if (isReplayModeActive)
            {
                num6 = 2f;
            }
            if (!(_stayStartTime + num6 + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            batsmanOutIndexValue = runOutScenario("w");
            if (hasRunOutOccurred)
            {
                if (isOversteppedDelivery)
                {
                    currentWicketKeeperStatus = "keepercollectsandout";
                    showMainUmpireForNoBallAction();
                    mainUmpireAnim.Play("IdleGetReady");
                    bool flag2 = checkForMatchComplete(runsScoredThisBall + 1, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets + 1);
                    if (CONTROLLER.matchType == "oneday" && !flag2)
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
                    return;
                }
                if (isLineFreeHitActive)
                {
                    currentWicketKeeperStatus = "loopEnd";
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 0, batsmanOutIndexValue, isBoundary: false);
                    isLineFreeHitActive = false;
                    freeHit = false;
                    lastBowledBallType = "lineball";
                    Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                    return;
                }
                currentWicketKeeperStatus = "loopEnd";
                if (isReplayModeActive)
                {
                    HideReplay();
                }
                else
                {
                    if (!(Singleton<GameData>.instance != null))
                    {
                        return;
                    }
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
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 0, batsmanOutIndexValue, isBoundary: false);
                    }
                }
            }
            else
            {
                if (!(Singleton<GameData>.instance != null))
                {
                    return;
                }
                if (isOversteppedDelivery)
                {
                    currentWicketKeeperStatus = "keepercollectsandnotout";
                    showMainUmpireForNoBallAction();
                    mainUmpireAnim.Play("IdleGetReady");
                    bool flag3 = checkForMatchComplete(runsScoredThisBall + 1, 0);
                    if (CONTROLLER.matchType == "oneday" && !flag3)
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
                    return;
                }
                if (isLineFreeHitActive)
                {
                    currentWicketKeeperStatus = "loopEnd";
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, batsmanOutIndexValue, isBoundary: false);
                    isLineFreeHitActive = false;
                    freeHit = false;
                    lastBowledBallType = "lineball";
                    Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                    return;
                }
                currentWicketKeeperStatus = "loopEnd";
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
                    freeHit = false;
                    isFreeHitActive = false;
                }
                else
                {
                    if (freeHit || noBall)
                    {
                        return;
                    }
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
        else if (currentWicketKeeperStatus == "keepercollectsandout")
        {
            if (!(_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            currentWicketKeeperStatus = "loopend";
            if (!(Singleton<GameData>.instance != null))
            {
                return;
            }
            if (isOversteppedDelivery)
            {
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
        else
        {
            if (!(currentWicketKeeperStatus == "keepercollectsandnotout") || !(_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time))
            {
                return;
            }
            currentWicketKeeperStatus = "loopend";
            if (!(Singleton<GameData>.instance != null))
            {
                return;
            }
            if (isOversteppedDelivery)
            {
                if (!isReplayModeActive)
                {
                    noBallRunStatus = "keeperrunoutappealsandnotout";
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

    private void MoveWicketKeeperToStumps()
    {
        if (stopKeeper)
        {
            return;
        }
        float num = 0f;
        keeperAnim.CrossFade("keeperRun");
        if (bowlerType == "fast")
        {
            num = 1f;
        }
        else if (bowlerType == "medium")
        {
            num = 0.7f;
        }
        if (wicketKeeperDirectionAfterBatting == "straight")
        {
            if (bowlerType == "spin")
            {
                num = 0.3f;
            }
            iTween.MoveTo(wicketKeeperObject, iTween.Hash("position", wicketKeeperStraightStumpingPosition.transform.position, "time", num, "easetype", "linear", "oncomplete", "EnableWicketKeeperToCollectBall", "oncompletetarget", base.gameObject));
            _wicketKeeperTransform.LookAt(wicketKeeperStraightStumpingPosition.transform);
        }
        else if (wicketKeeperDirectionAfterBatting == "offSide")
        {
            if (bowlerType == "spin")
            {
                num = 0.7f;
            }
            iTween.MoveTo(wicketKeeperObject, iTween.Hash("position", wicketKeeperOffSideStumpingPosition.transform.position, "time", num, "easetype", "linear", "oncomplete", "EnableWicketKeeperToCollectBall", "oncompletetarget", base.gameObject));
            _wicketKeeperTransform.LookAt(wicketKeeperOffSideStumpingPosition.transform);
        }
        else if (wicketKeeperDirectionAfterBatting == "legSide")
        {
            if (bowlerType == "spin")
            {
                num = 0.4f;
            }
            iTween.MoveTo(wicketKeeperObject, iTween.Hash("position", wicketKeeperLegSideStumpingPosition.transform.position, "time", num, "easetype", "linear", "oncomplete", "EnableWicketKeeperToCollectBall", "oncompletetarget", base.gameObject));
            _wicketKeeperTransform.LookAt(wicketKeeperLegSideStumpingPosition.transform);
        }
    }

    private void EnableWicketKeeperToCollectBall()
    {
        isWicketKeeperAtStump = true;
        keeperAnim.Play("idle");
        _wicketKeeperTransform.LookAt(fielderFocusObjectToCollectBall.transform);
        currentWicketKeeperStatus = "waitToCollect";
    }

    // Estimates the authoritative (bowling-streamed) ball X at the stump plane and tests it
    // against the stump collider bounds (+ a small sync tolerance). Used by the batting client
    // to reject false "bowled" hits caused by local lerp-lag / parametric drift.
    private bool IsAuthoritativeLineInlineWithStumps()
    {
        // No authoritative sample yet — don't override the local result.
        if (!_hasNetworkBallPosition) return true;
        if (Stump1Collider == null) return true;
        Collider stumpCol = Stump1Collider.GetComponent<Collider>();
        if (stumpCol == null) return true;

        Bounds b = stumpCol.bounds;
        float halfX = b.extents.x + 0.02f; // collider half-width + small sync tolerance
        float authX = AuthoritativeXAtStumpPlane(b.center.z);
        return Mathf.Abs(authX - b.center.x) <= halfX;
    }

    // Linearly interpolates/extrapolates the authoritative X to the stump z-plane using the last
    // two streamed positions. Falls back to the latest sample's X when no usable second sample.
    private float AuthoritativeXAtStumpPlane(float stumpZ)
    {
        if (!_hasPrevNetworkBallPosition) return _networkBallPosition.x;
        float dz = _networkBallPosition.z - _prevNetworkBallPosition.z;
        if (Mathf.Abs(dz) < 0.0001f) return _networkBallPosition.x;
        float t = (stumpZ - _prevNetworkBallPosition.z) / dz;
        t = Mathf.Clamp(t, -1f, 2f); // guard against wild extrapolation
        return _prevNetworkBallPosition.x + (_networkBallPosition.x - _prevNetworkBallPosition.x) * t;
    }

    public string getWicketKeeperStatus()
    {
        return currentWicketKeeperStatus;
    }

    private void StumpAnimation(GameObject stump, float contactPoint)
    {
        // MP stump-fall sync (tester report #1): which stump breaks (off / mid / leg / all-three) is chosen
        // from the LOCAL ball X, which diverges slightly across clients near the ±0.06 threshold — so each
        // screen could show a different stump falling for the SAME wicket. The batting authority relays its
        // resolved clip; the bowling follower plays ONLY that (suppressing its own divergent recompute), so
        // both screens match. Mirror ClientRpc is reliable, so the relay is guaranteed (no standing-stumps).
        bool isBowlingFollower = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex;
        if (isBowlingFollower && !isReplayModeActive)
        {
            // Don't play a locally-recomputed (possibly divergent) clip — OnSyncedStumpAnim drives the
            // authoritative one. Replay still plays the saved clip below.
            return;
        }
        string empty = string.Empty;
        if (!isReplayModeActive)
        {
            empty = ((bowlerType != "medium") ? ((contactPoint < -0.06f) ? (bowlerType + "OffStump") : ((contactPoint > 0.06f) ? (bowlerType + "LegStump") : ((!(Mathf.Abs(contactPoint) < 0.03f)) ? "allThreeStumps" : (bowlerType + "MidStump")))) : ((contactPoint < -0.06f) ? "fastOffStump" : ((contactPoint > 0.06f) ? "fastLegStump" : ((!(Mathf.Abs(contactPoint) < 0.03f)) ? "allThreeStumps" : "fastMidStump"))));
            stump.GetComponent<Animation>().Play(empty);
            stumpAnimationToPlaySaved = empty;
            // Batting authority relays the resolved clip + which stump (0 = bowled/stumpLeft, 1 = hit-wicket/
            // stumpRight) so the bowling follower plays the identical break.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
                && CricketNetworkManager.instance != null)
            {
                int stumpSide = (stump == stumpRight) ? 1 : 0;
                CricketNetworkManager.instance.CmdSyncStumpAnim(staticVariables.UserProfiledata.user._id, empty, stumpSide);
            }
        }
        else if (isReplayModeActive)
        {
            stump.GetComponent<Animation>().Play(stumpAnimationToPlaySaved);
        }
    }

    // MP stump-fall sync (tester report #1): bowling follower plays the batting authority's resolved
    // stump-break clip on the correct stump, so both screens show the SAME stump fall regardless of local
    // ball-X divergence. stumpSide: 0 = bowled (stumpLeft), 1 = hit-wicket (stumpRight).
    public void OnSyncedStumpAnim(string clip, int stumpSide)
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return;
        if (string.IsNullOrEmpty(clip)) return;
        GameObject stump = (stumpSide == 1) ? stumpRight : stumpLeft;
        if (stump == null) return;
        stumpAnimationToPlaySaved = clip;
        // Lockstep: the batter relays the break at ITS real-time bowled moment; this side's ball runs
        // lockstepFollowerFlightDelay behind — playing on arrival broke the stumps ~0.45s before the ball
        // got there on this screen. Wait for OUR ball's moment.
        if (LockstepActive && ConstantsData_M.lockstepFollowerFlightDelay > 0f)
        {
            StartCoroutine(PlaySyncedStumpAnimOnMyTimeline(stump, clip));
            return;
        }
        Animation anim = stump.GetComponent<Animation>();
        if (anim != null) anim.Play(clip);
    }

    private IEnumerator PlaySyncedStumpAnimOnMyTimeline(GameObject stump, string clip)
    {
        float wait = ConstantsData_M.lockstepFollowerFlightDelay - (float)(Mirror.NetworkTime.rtt / 2.0);
        if (wait > 0f)
        {
            double until = Mirror.NetworkTime.time + wait;
            while (Mirror.NetworkTime.time < until)
            {
                yield return null;
            }
        }
        if (stump == null) yield break;
        Animation anim = stump.GetComponent<Animation>();
        if (anim != null) anim.Play(clip);
    }

    private bool IsStumpOut()
    {
        bool result = false;
        float time = batsmanAnim[currentBatsmanAnimation].time;
        int num = 0;
        int num2 = 0;
        if (currentBatsmanAnimation == "bt6CoverDrive")
        {
            num = 22;
            num2 = 87;
        }
        else if (currentBatsmanAnimation == "bt6LegGlance")
        {
            num = 16;
            num2 = 83;
        }
        else if (currentBatsmanAnimation == "bt6OffDrive")
        {
            num = 16;
            num2 = 67;
        }
        else if (currentBatsmanAnimation == "bt6OnDrive")
        {
            num = 14;
            num2 = 115;
        }
        else if (currentBatsmanAnimation == "bt6StraightDrive")
        {
            num = 17;
            num2 = 81;
        }
        else if (currentBatsmanAnimation == "loftLegSide")
        {
            num = 3;
            num2 = 99;
        }
        else if (currentBatsmanAnimation == "loftOffSide")
        {
            num = 3;
            num2 = 136;
        }
        else if (currentBatsmanAnimation == "loftStraight")
        {
            num = 1;
            num2 = 188;
        }
        else if (currentBatsmanAnimation == "frontFootOffDrive")
        {
            num = 20;
            num2 = 64;
        }
        if (time >= (float)num * animationFrameInterval && time <= (float)num2 * animationFrameInterval)
        {
            result = true;
        }
        return result;
    }

    private bool IsStumpOut2()
    {
        bool flag = false;
        float time = batsmanAnim["ReturnToCrease2"].time;
        int num = 9;
        if (time < (float)num * animationFrameInterval || Mathf.Abs(batEdgeObject.transform.position.z) < 8.7f)
        {
            flag = true;
        }
        if (flag && (leftLegEdgeObject.transform.position.z > 8.7f || rightLegEdgeObject.transform.position.z > 8.7f || batterLeftShoeBackEdge.transform.position.z > 8.7f || batterRightShoeBackEdge.transform.position.z > 8.7f))
        {
            flag = false;
        }
        return flag;
    }
}
