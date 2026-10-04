// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.ShotTables — the SHOT OUTCOME TABLES (extracted from .Delivery, Phase 3).
// BallTiming() routes to the mode-specific table; MultiPlayerBallAngle() is the online table:
// base angle per shot (±jitter scaled by controlFactor), then the launch-physics block
// (hVel/launchAngle/arcHeight/firstBounceDistance) — under lockstep BOTH clients run it with the
// SAME seeded rolls, and the follower then overrides with the batter's exact relayed numbers.
// Phase 5 will lift these constants into ScriptableObjects.
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
    public void BallTiming()
    {


        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex
            && !ConstantsData_M.useDeterministicContact)   // lockstep: no relayed-angle wait — the seeded tables below run identically on both clients
        {
            if (Time.time > ballReleaseTime + optimalShotActivationTiming && batsmanCompletedShot)
            {

                _ballAngle = CONTROLLER.BALLANGLE;
                MultiPlayerBallAngle();
                return;
            }
        }
        else if (CONTROLLER.PlayModeSelected == 8
            && (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || ConstantsData_M.useDeterministicContact))
        {
            // Lockstep review BUG 1: without the OR, the bowling side fell through to the OFFLINE table code
            // below while the batter ran MultiPlayerBallAngle — different code paths draw a different number
            // of seeded rolls → guaranteed divergence. Both sides take the same branch under the isDriveArcShot.
            MultiPlayerBallAngle();
            return;
        }

        float preContactBallAngle = _ballAngle;
        int controlFactor = ((CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8) ? 1 : 0);

        animationValue = temporaryPosition.z;
        bool isDriveArcShot = false;

        if (currentShotPlayed == "lowHookShot")
        {
            _ballAngle = 20f + DetRange(-20f * (1f - controlMultiplier * (float)controlFactor), 20f * (1f - controlMultiplier * (float)controlFactor));
        }

        if (currentShotPlayed == "bt6HookShot")
        {
            _ballAngle = 330f + DetRange(-20f * (1f - controlMultiplier * (float)controlFactor), 20f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "bt6LegGlance" || currentShotPlayed == "legGlanceYorkerLength" || currentShotPlayed == "sweep" || currentShotPlayed == "paddleSweep")
        {
            if ((currentShotPlayed == "bt6LegGlance" || currentShotPlayed == "legGlanceYorkerLength") && attemptedSquareLegGlance)
            {
                _ballAngle = 15f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed != "bt6LegGlance")
            {
                _ballAngle = 55f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else
            {
                _ballAngle = 30f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
        }
        else if (currentShotPlayed == "WCCLite_YorkerLegGlanceNew")
        {
            if (attemptedSquareLegGlance)
            {
                _ballAngle = 19f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else
            {
                _ballAngle = 45f + DetRange(-10f * (1f - controlMultiplier * (float)controlFactor), 10f * (1f - controlMultiplier * (float)controlFactor));
            }
        }
        else if (currentShotPlayed == "WCCLite_Dilscoop")
        {
            _ballAngle = 67.5f + DetRange(-10f * (1f - controlMultiplier * (float)controlFactor), 10f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "bt6LateCut" || currentShotPlayed == "bt6ReverseSweep" || currentShotPlayed == "lateCutLowHeight" || currentShotPlayed == "reverseSweepSlowBall")
        {
            _ballAngle = 120f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "bt6SquareCut")
        {
            _ballAngle = 160f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "squareDrive" || currentShotPlayed == "lateSquareDrive" || currentShotPlayed == "frontFootOffDrive")
        {
            _ballAngle = 180f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "bt6CoverDrive" || currentShotPlayed == "extraCoverDrive")
        {
            _ballAngle = 210f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            isDriveArcShot = true;
        }
        else if (currentShotPlayed == "insideOutCoverDrive")
        {
            _ballAngle = 210f + DetRange(-30f * (1f - controlMultiplier * (float)controlFactor), 30f * (1f - controlMultiplier * (float)controlFactor));
            ////ConstantsData_M.MpLog("==BALL ANGLE : " + ballAngle);
        }
        else if (currentShotPlayed == "bt6OffDrive" || currentShotPlayed == "backFootOffDrive" || currentShotPlayed == "loftOffSide" || currentShotPlayed == "WCCLite_HarbhajanShot" || currentShotPlayed == "WCCLite_LoftedSquareDrive")
        {
            _ballAngle = 240f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            if (currentShotPlayed != "WCCLite_HarbhajanShot" || currentShotPlayed != "WCCLite_LoftedSquareDrive")
            {
                isDriveArcShot = true;
            }
            if (attemptedSquareCutDrive && currentShotPlayed == "bt6OffDrive")
            {
                _ballAngle = 180f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
        }
        else if (currentShotPlayed == "bt6StraightDrive" || currentShotPlayed == "backFootStraightDrive" || currentShotPlayed == "loftStraight" || currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense" || currentShotPlayed == "loftStraightShortBall" || currentShotPlayed == "straightDrivePowerShot")
        {
            _ballAngle = 270f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            if (currentShotPlayed == "bt6StraightDrive" || currentShotPlayed == "backFootStraightDrive" || currentShotPlayed == "loftStraight")
            {
                isDriveArcShot = true;
            }
        }
        else if (currentShotPlayed == "WCCLite_BackFootPunch")
        {
            _ballAngle = 225f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "WCCLite_ABdeVilliers_Shot")
        {
            _ballAngle = 40f + DetRange(-10f * (1f - controlMultiplier * (float)controlFactor), 10f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "WCCLite_LegGlanceNew")
        {
            if (attemptedSquareLegGlance)
            {
                _ballAngle = 17f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else
            {
                _ballAngle = 22.5f + DetRange(0f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
        }
        else if (currentShotPlayed == "bt6Sweep")
        {
            _ballAngle = 26.5f + DetRange(0f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "pullShot" || currentShotPlayed == "lowPullShot")
        {
            _ballAngle = 341f + DetRange(-19f * (1f - controlMultiplier * (float)controlFactor), 19f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "lowPullShot1")
        {
            _ballAngle = 355f + DetRange(-19f * (1f - controlMultiplier * (float)controlFactor), 19f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "bt6OnDrive" || currentShotPlayed == "backFootOnDrive" || currentShotPlayed == "loftLegS   " || currentShotPlayed == "runDownOnDrive")
        {
            _ballAngle = 303f + DetRange(-19f * (1f - controlMultiplier * (float)controlFactor), 19f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "backFootOnDrive_new")
        {
            _ballAngle = 325f + DetRange(-19f * (1f - controlMultiplier * (float)controlFactor), 19f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "WCCLite_HelicopterShot" || currentShotPlayed == "powerfulSweepShot")
        {
            _ballAngle = 320f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
        }
        else if (currentShotPlayed == "bt6PullShot")
        {
            _ballAngle = 335f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
        }
        //if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            _ballAngle = shotAngleValue + DetRange(-10f * (1f - controlMultiplier * (float)controlFactor), 10f * (1f - controlMultiplier * (float)controlFactor));

            ////ConstantsData_M.MpLog("RPC Before BALLANGLE : " + ballAngle);
            ////ConstantsData_M.MpLog("RPC Before SHOTANGLE : " + shotAngle);
            ////ConstantsData_M.MpLog("RPC Before CONTROLFAC : " + controlFactor);

            if (Time.time > ballReleaseTime + optimalShotActivationTiming && batsmanCompletedShot && CONTROLLER.PlayModeSelected == 8)
            {
                ////ConstantsData_M.MpLog("MAAR DIYA SHOT");
                //photonView.RPC("RPC_ChangeBallAngle", RpcTarget.AllBuffered, ballAngle,shotAngle,controlFactor);
                //            if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
                //            {
                //                photonView.RPC("RPC_BallTiming", RpcTarget.AllBuffered, ballAngle, horizontalSpeed, ballProjectileAngle, ballProjectileHeight, ballTimingFirstBounceDistance, ballProjectileAnglePerSecond, ballBatMeetingHeight, horizontalSpeedMultiplier);
                //            }
            }


            if (currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense")
            {
                _ballAngle = 270 + DetRange(-10, 10);
                isDriveArcShot = true;
            }
            if (CONTROLLER.StrikerHand == "left")
            {
                _ballAngle = 180f - _ballAngle + 360f;
                _ballAngle %= 360f;
            }
        }
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex && isAIHittingInGap && CONTROLLER.PlayModeSelected != 8)
        {
            _ballAngle = aiBallAngle;
            if (currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense")
            {
                _ballAngle = 270 + DetRange(-10, 10);
                isDriveArcShot = true;
            }
        }
        if (isDriveArcShot && DetRange(0f, 100f) > 98f * (1f + controlMultiplier * (float)controlFactor))
        {
            isSlipShot = true;
            _ballAngle = 105f;
        }
        if (Singleton<LeftFovLerp>.instance != null)
        {
            Singleton<LeftFovLerp>.instance.setBallAngle(_ballAngle);
        }
        if (Singleton<RightSmoothFov>.instance != null)
        {
            Singleton<RightSmoothFov>.instance.setBallAngle(_ballAngle);
        }
        if (Singleton<MainCameraController>.instance != null)
        {
            Singleton<MainCameraController>.instance.setBallAngle(_ballAngle);
        }
        Singleton<GameData>.instance.GetFinalBallAngle(_ballAngle);
        if (_ballAngle > 20f && _ballAngle < 90f)
        {
            isBallToFineLeg = true;
        }
        bool ballOutsideBatReach = false;
        // Lockstep: this bool feeds the shot tables, so both clients must evaluate it against the SAME batsman x.
        // The streamed batsman transform lags at high ping — use the x captured in the swing input packet.
        float lockstepAwareBatterX = LockstepActive ? _lockstepBatterX : _batsmanTransform.position.x;
        if (batsmanHand == "right")
        {
            if (matchBallTransform.position.x > lockstepAwareBatterX - 0.95f)
            {
                ballOutsideBatReach = false;
            }
            else
            {
                ballOutsideBatReach = true;
            }
        }
        else if (batsmanHand == "left")
        {
            if ((double)matchBallTransform.position.x < (double)lockstepAwareBatterX + 0.75)
            {
                ballOutsideBatReach = false;
            }
            else
            {
                ballOutsideBatReach = true;
            }
        }
        DebugLogger.PrintWithSize("Shot played: " + currentShotPlayed);
        if (currentShotPlayed == "bt6StraightDrive" && !isReplayModeActive)
        {
            //if (!hardcoded && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.PlayModeSelected != 6 && CanProduceEdge() && !lineFreeHit && !IsFullTossBall && !overStepBall && ballSpotLength <= 14.8f)
            if (!isHardcoded && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.PlayModeSelected != 6 && CanProduceEdge() && !isLineFreeHitActive && !isFullToss && !isOversteppedDelivery && ballSpotLength <= 14.8f)
            {
                isSlipShot = false;
                _ballAngle = 95f + DetRange(0.1f, 0.6f);
                isEdgeCaught = true;
                ballOutsideBatReach = true;
                currentWicketKeeperStatus = string.Empty;
                if (Singleton<GameData>.instance != null)
                {
                    disableRunCancelBtn();
                }
                if (activeFielders.Count > 0)
                {
                    activeFielders.Clear();
                }
                previousEdgeCatch = isEdgeCaught;
                SetUltraEdgeDecision();
                if (canAIAskForReview || canUserAskForReview)
                {
                    isUltraEdgeCutscenePlaying = true;
                    PlaceUltraEdgeCam();
                }
            }
        }
        else if (isPowerShotActive && (currentShotPlayed == "bt6HookShot" || currentShotPlayed == "bt6SquareCut" || currentShotPlayed == "pullShot") && hasTopEdge)
        {
            if (DetRange(0, 10) > 3)
            {
                isSlipShot = false;
                _ballAngle = DetRange(95, 125);
            }
            else
            {
                isSlipShot = false;
                _ballAngle = DetRange(50, 80);
            }
        }
        if (isHardcoded && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.PlayModeSelected != 6 && !isLineFreeHitActive && !isFullToss && ballSpotLength <= 14.8f)
        {
            isSlipShot = false;
            _ballAngle = 95f + DetRange(0.1f, 0.6f);
            isEdgeCaught = true;
            ballOutsideBatReach = true;
            currentWicketKeeperStatus = string.Empty;
            if (Singleton<GameData>.instance != null)
            {
                disableRunCancelBtn();
            }
            if (activeFielders.Count > 0)
            {
                activeFielders.Clear();
            }
            previousEdgeCatch = isEdgeCaught;
            SetUltraEdgeDecision();
            if (canAIAskForReview || canUserAskForReview)
            {
                isUltraEdgeCutscenePlaying = true;
                PlaceUltraEdgeCam();
            }
        }
        if (batsmanHand == "left")
        {
            _ballAngle = 180f - _ballAngle + 360f;
            _ballAngle %= 360f;
        }
        //if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
        //{
        //    photonView.RPC("RPC_BallTiming", RpcTarget.AllBuffered, ballAngle, horizontalSpeed, ballProjectileAngle, ballProjectileHeight, ballTimingFirstBounceDistance, ballProjectileAnglePerSecond);
        //}
        if (Singleton<GameData>.instance != null && !isEdgeCaught && !isUltraEdgeCutscenePlaying)
        {
            Singleton<GameData>.instance.PlayGameSound("Bat");
        }

        Singleton<AIFieldingSetupManager>.instance.lastHittedAngle = _ballAngle;
        if (currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense")
        {
            ////ConstantsData_M.MpLog("DEFENCE");
            horizontalVelocity = 5 + DetRange(-1, 1);
            launchAngle = 270f;
            arcHeight = batContactHeight;
            firstBounceDistance = batContactHeight * 3f;
            angleChangeRate = 90f / firstBounceDistance * horizontalVelocity;
        }
        else if (isSlipShot)
        {
            ////ConstantsData_M.MpLog("SLIPSHOT");
            firstBounceDistance = 19f;
            horizontalVelocity = 20f + DetRange(0f, 4f);
            arcHeight = DetRange(2, 4);
            float slipLaunchLiftDeg = Mathf.Asin(batContactHeight / arcHeight) * radToDeg;
            launchAngle = 180f + slipLaunchLiftDeg;
            angleChangeRate = (180f - slipLaunchLiftDeg) / firstBounceDistance * horizontalVelocity;
        }
        else if (isPowerShotActive || isPowerShotActiveSaved)
        {
            ////ConstantsData_M.MpLog("POOOWER SHOTTTT");
            if (Singleton<GameData>.instance != null && !isReplayModeActive)
            {
                Singleton<GameData>.instance.PlayGameSound("Beaten");
            }
            float powerDistanceBase = 0f;
            float confidenceVal = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal;
            powerDistanceBase = ((!(confidenceVal * 6f < 20f)) ? (confidenceVal * 6f) : 20f);
            if (CONTROLLER.totalOvers * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls < 12)
            {
                powerDistanceBase += 10f;
            }
            if (isBatsmanConfident)
            {
                if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
                //if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
                {
                    firstBounceDistance = confidenceVal * 4f + (float)DetRange(25, 30);
                    firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                }
                else
                {
                    firstBounceDistance = confidenceVal * 6.5f;
                }
                horizontalVelocity = confidenceVal + 15f + (float)DetRange(1, 6);
            }
            else
            {
                firstBounceDistance = confidenceVal * 6.5f - Mathf.Abs(0.8f - desiredAnimationSpeed) * 10f;
                firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                horizontalVelocity = 22 + DetRange(1, 6);
            }
            float bigHitChance = 0f;
            bigHitChance = ((CONTROLLER.PlayModeSelected != 7) ? (confidenceVal / 10f * 2f) : (confidenceVal / 10f * 1f));
            if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex || CONTROLLER.PlayModeSelected == 8)
            {
                bigHitChance += 3.5f;
            }
            if (CONTROLLER.difficultyMode == "hard" && CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex || CONTROLLER.PlayModeSelected != 8)
            {
                bigHitChance += 3f;
            }
            if ((float)DetRange(0, 10) < bigHitChance && currentShotPlayed != "bt6LateCut" && currentShotPlayed != "lateCutLowHeight")
            {
                firstBounceDistance = DetRange(90, 105);
            }
            else
            {
                if ((float)DetRange(0, 10) > confidenceVal || firstBounceDistance > 90f)
                {
                    if (CONTROLLER.totalOvers * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls < 12)
                    {
                        if (DetRange(0, 10) < 2)
                        {
                            firstBounceDistance -= DetRange(10, 20);
                        }
                    }
                    else
                    {
                        firstBounceDistance -= DetRange(10, 20);
                    }
                }
                if (firstBounceDistance < 40f)
                {
                    firstBounceDistance = 40 + DetRange(10, 20);
                }
            }
            if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
            //if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex )
            {
                if (isPerfectShot)
                {
                    firstBounceDistance = 80 + DetRange(5, 15);
                }
                else if (isMistimedShot && firstBounceDistance > 60f)
                {
                    firstBounceDistance = 40 + DetRange(10, 20);
                }
                else
                {
                    firstBounceDistance *= firstBounceSpeedMultiplier;
                }
            }
            if (CONTROLLER.difficultyMode == "hard" && BattingBy == "user" && !isEdgeCaught)
            {
                firstBounceDistance *= 0.9f;
                firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
            }
            if (currentShotPlayed == "bt6LateCut" || currentShotPlayed == "lateCutLowHeight" || currentShotPlayed == "bt6ReverseSweep")
            {
                firstBounceDistance /= 4f;
                firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                horizontalVelocity = 18 + DetRange(0, 5);
            }
            if (hasTopEdge && !isReplayModeActive)
            {
                horizontalVelocity = 9 + DetRange(2, 3);
                firstBounceDistance /= 2f;
                firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                arcHeight = firstBounceDistance / (float)(2 + DetRange(0, 2));
                float topEdgeLaunchLiftDeg = Mathf.Asin(batContactHeight / arcHeight) * radToDeg;
                launchAngle = 180f + topEdgeLaunchLiftDeg;
                angleChangeRate = (180f - topEdgeLaunchLiftDeg) / firstBounceDistance * horizontalVelocity;
            }
            else if (!isEdgeCaught && !isReplayModeActive)
            {
                arcHeight = firstBounceDistance / (float)(6 + DetRange(-1, 2));
                float cleanHitLaunchLiftDeg = Mathf.Asin(batContactHeight / arcHeight) * radToDeg;
                launchAngle = 180f + cleanHitLaunchLiftDeg;
                angleChangeRate = (180f - cleanHitLaunchLiftDeg) / firstBounceDistance * horizontalVelocity;
            }
        }
        else if (!isPowerShotActive && !hasTopEdge && !isEdgeCaught && !previousEdgeCatch)
        {
            ////ConstantsData_M.MpLog("NOT POWER SHOT");
            if (Singleton<GameData>.instance != null && !isReplayModeActive)
            {
                Singleton<GameData>.instance.PlayGameSound("Beaten");
            }
            if (_ballAngle > 250f && _ballAngle < 290f)
            {
                horizontalVelocity = 15 + DetRange(0, 10);
                launchAngle = 270f;
                horizontalVelocity *= horizontalSpeedAdjustment;
                arcHeight = batContactHeight;
                firstBounceDistance = batContactHeight * 10f;
                firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                angleChangeRate = 90f / firstBounceDistance * horizontalVelocity;
            }
            else
            {
                if (batContactHeight < 0.5f)
                {
                    horizontalVelocity = 15 + DetRange(0, 10);
                }
                else
                {
                    horizontalVelocity = 25f;
                }
                arcHeight = batContactHeight * 2f;
                if (currentShotPlayed == "bt6OffDrive" && DetRange(0, 10) < 8)
                {
                    arcHeight = batContactHeight;
                }
                horizontalVelocity *= horizontalSpeedAdjustment;
                firstBounceDistance = arcHeight * 10f;
                firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                float groundShotLaunchLiftDeg = Mathf.Asin(batContactHeight / arcHeight) * radToDeg;
                launchAngle = 180f + groundShotLaunchLiftDeg;
                angleChangeRate = (180f - groundShotLaunchLiftDeg) / firstBounceDistance * horizontalVelocity;
            }
        }
        if (!isReplayModeActive)
        {
            savedBallAngle = _ballAngle;
            savedHorizontalBallSpeed = horizontalVelocity;
            savedBallLaunchAngle = launchAngle;
            savedBallLaunchHeight = arcHeight;
            savedBallFirstBounceDistance = firstBounceDistance;
            savedBallLaunchAnglePerSecond = angleChangeRate;
            isSlipShotSaved = isSlipShot;
            isBallHitToFineLegSaved = isBallToFineLeg;
            previousEdgeCatch = isEdgeCaught;
        }
        else if (isReplayModeActive)
        {
            isEdgeCaught = previousEdgeCatch;
            if (isEdgeCaught)
            {
                currentWicketKeeperStatus = string.Empty;
            }
            _ballAngle = savedBallAngle;
            horizontalVelocity = savedHorizontalBallSpeed;
            launchAngle = savedBallLaunchAngle;
            arcHeight = savedBallLaunchHeight;
            firstBounceDistance = savedBallFirstBounceDistance;
            angleChangeRate = savedBallLaunchAnglePerSecond;
            isSlipShot = isSlipShotSaved;
            isBallToFineLeg = isBallHitToFineLegSaved;
        }
        if (isEdgeCaught)
        {
            isWicketKeeperCatchingAnimationSelected = false;
            ActivateWicketKeeper();
        }
        distanceToNextPitch = firstBounceDistance;
        ballConnectionTiming = Time.time;
        ballFirstBounce.transform.position = new Vector3(ballStartPoint.position.x + firstBounceDistance * Mathf.Cos(_ballAngle * degToRad), ballFirstBounce.transform.position.y, ballStartPoint.position.z + firstBounceDistance * Mathf.Sin(_ballAngle * degToRad));
        if (Singleton<LeftFovLerp>.instance != null)
        {
            Singleton<LeftFovLerp>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (Singleton<RightSmoothFov>.instance != null)
        {
            Singleton<RightSmoothFov>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (Singleton<MainCameraController>.instance != null)
        {
            Singleton<MainCameraController>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (firstBounceDistance > 55f && !isReplayModeActive && !isUltraEdgeCutscenePlaying)
        {
            Transform transform = UnityEngine.Object.Instantiate(ballImpactEffect, PrimaryBatCollider.transform.position, PrimaryBatCollider.transform.rotation);
            transform.transform.position = matchBall.transform.position;
            transform.transform.localScale = Vector3.one * 0.2f;
        }
        FixBallCatchingSpot();
        if (_ballAngle >= 90f && _ballAngle <= 210f)
        {
            wicketKeeperDirectionAfterBatting = "offSide";
        }
        else if (_ballAngle > 210f && _ballAngle < 330f)
        {
            wicketKeeperDirectionAfterBatting = "straight";
        }
        else
        {
            wicketKeeperDirectionAfterBatting = "legSide";
        }
        RepositionSideCamera();
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
        {
            //photonView.RPC("RPC_BallTiming", RpcTarget.AllBuffered, ballAngle, horizontalSpeed, ballProjectileAngle, ballProjectileHeight, ballTimingFirstBounceDistance, ballProjectileAnglePerSecond, ballBatMeetingHeight, horizontalSpeedMultiplier);
        }
        ////ConstantsData_M.MpLog("BALL ANGLE : " + ballAngle);
    }

    public void MultiPlayerBallAngle()
    {
        ////ConstantsData_M.MpLog("!!SHOTTT : " + shotPlayed);
        float preContactBallAngle = _ballAngle;
        int controlFactor = ((CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8) ? 1 : 0);
        ////ConstantsData_M.MpLog("!!NUM 2 = " + controlFactor);

        //animval = ballTransform.position.z;
        animationValue = temporaryPosition.z;
        bool isDriveArcShot = false;

        if ((CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            || (ConstantsData_M.useDeterministicContact && CONTROLLER.PlayModeSelected == 8))   // lockstep: both clients compute from the seeded tables
        {
            if (currentShotPlayed == "lowHookShot")
            {
                _ballAngle = 20f + DetRange(-20f * (1f - controlMultiplier * (float)controlFactor), 20f * (1f - controlMultiplier * (float)controlFactor));
            }
            //if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
            //{

            //}
            if (currentShotPlayed == "bt6HookShot")
            {
                _ballAngle = 330f + DetRange(-20f * (1f - controlMultiplier * (float)controlFactor), 20f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "bt6LegGlance" || currentShotPlayed == "legGlanceYorkerLength" || currentShotPlayed == "sweep" || currentShotPlayed == "paddleSweep")
            {
                if ((currentShotPlayed == "bt6LegGlance" || currentShotPlayed == "legGlanceYorkerLength") && attemptedSquareLegGlance)
                {
                    _ballAngle = 15f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                }
                else if (currentShotPlayed != "bt6LegGlance")
                {
                    _ballAngle = 55f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                }
                else
                {
                    _ballAngle = 30f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                }
            }
            else if (currentShotPlayed == "WCCLite_YorkerLegGlanceNew")
            {
                if (attemptedSquareLegGlance)
                {
                    _ballAngle = 19f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                }
                else
                {
                    _ballAngle = 45f + DetRange(-10f * (1f - controlMultiplier * (float)controlFactor), 10f * (1f - controlMultiplier * (float)controlFactor));
                }
            }
            else if (currentShotPlayed == "WCCLite_Dilscoop")
            {
                _ballAngle = 67.5f + DetRange(-10f * (1f - controlMultiplier * (float)controlFactor), 10f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "bt6LateCut" || currentShotPlayed == "bt6ReverseSweep" || currentShotPlayed == "lateCutLowHeight" || currentShotPlayed == "reverseSweepSlowBall")
            {
                _ballAngle = 120f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "bt6SquareCut")
            {
                _ballAngle = 160f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "squareDrive" || currentShotPlayed == "lateSquareDrive" || currentShotPlayed == "frontFootOffDrive")
            {
                _ballAngle = 180f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "bt6CoverDrive" || currentShotPlayed == "extraCoverDrive")
            {
                _ballAngle = 210f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                isDriveArcShot = true;
            }
            else if (currentShotPlayed == "insideOutCoverDrive")
            {
                _ballAngle = 210f + DetRange(-30f * (1f - controlMultiplier * (float)controlFactor), 30f * (1f - controlMultiplier * (float)controlFactor));
                ////ConstantsData_M.MpLog("==BALL ANGLE : " + ballAngle);
            }
            else if (currentShotPlayed == "bt6OffDrive" || currentShotPlayed == "backFootOffDrive" || currentShotPlayed == "loftOffSide" || currentShotPlayed == "WCCLite_HarbhajanShot" || currentShotPlayed == "WCCLite_LoftedSquareDrive")
            {
                _ballAngle = 240f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                if (currentShotPlayed != "WCCLite_HarbhajanShot" || currentShotPlayed != "WCCLite_LoftedSquareDrive")
                {
                    isDriveArcShot = true;
                }
                if (attemptedSquareCutDrive && currentShotPlayed == "bt6OffDrive")
                {
                    _ballAngle = 180f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                }
            }
            else if (currentShotPlayed == "bt6StraightDrive" || currentShotPlayed == "backFootStraightDrive" || currentShotPlayed == "loftStraight" || currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense" || currentShotPlayed == "loftStraightShortBall" || currentShotPlayed == "straightDrivePowerShot")
            {
                _ballAngle = 270f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                if (currentShotPlayed == "bt6StraightDrive" || currentShotPlayed == "backFootStraightDrive" || currentShotPlayed == "loftStraight")
                {
                    isDriveArcShot = true;
                }
            }
            else if (currentShotPlayed == "WCCLite_BackFootPunch")
            {
                _ballAngle = 225f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "WCCLite_ABdeVilliers_Shot")
            {
                _ballAngle = 40f + DetRange(-10f * (1f - controlMultiplier * (float)controlFactor), 10f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "WCCLite_LegGlanceNew")
            {
                if (attemptedSquareLegGlance)
                {
                    _ballAngle = 17f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                }
                else
                {
                    _ballAngle = 22.5f + DetRange(0f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
                }
            }
            else if (currentShotPlayed == "bt6Sweep")
            {
                _ballAngle = 26.5f + DetRange(0f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "pullShot" || currentShotPlayed == "lowPullShot")
            {
                _ballAngle = 341f + DetRange(-19f * (1f - controlMultiplier * (float)controlFactor), 19f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "lowPullShot1")
            {
                _ballAngle = 355f + DetRange(-19f * (1f - controlMultiplier * (float)controlFactor), 19f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "bt6OnDrive" || currentShotPlayed == "backFootOnDrive" || currentShotPlayed == "loftLegSide" || currentShotPlayed == "runDownOnDrive")
            {
                _ballAngle = 303f + DetRange(-19f * (1f - controlMultiplier * (float)controlFactor), 19f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "backFootOnDrive_new")
            {
                _ballAngle = 325f + DetRange(-19f * (1f - controlMultiplier * (float)controlFactor), 19f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "WCCLite_HelicopterShot" || currentShotPlayed == "powerfulSweepShot")
            {
                _ballAngle = 320f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            else if (currentShotPlayed == "bt6PullShot")
            {
                _ballAngle = 335f + DetRange(-15f * (1f - controlMultiplier * (float)controlFactor), 15f * (1f - controlMultiplier * (float)controlFactor));
            }
            //if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
            if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
            {
                _ballAngle = shotAngleValue + DetRange(-10f * (1f - controlMultiplier * (float)controlFactor), 10f * (1f - controlMultiplier * (float)controlFactor));


                if (currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense")
                {
                    _ballAngle = 270 + DetRange(-10, 10);
                    isDriveArcShot = true;
                }
                if (CONTROLLER.StrikerHand == "left")
                {
                    _ballAngle = 180f - _ballAngle + 360f;
                    _ballAngle %= 360f;
                }
            }

            if (isDriveArcShot && DetRange(0f, 100f) > 98f * (1f + controlMultiplier * (float)controlFactor))
            {
                isSlipShot = true;
                _ballAngle = 105f;
            }
            if (currentShotPlayed == "bt6StraightDrive" && !isReplayModeActive)
            {
                //if (!hardcoded && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.PlayModeSelected != 6 && CanProduceEdge() && !lineFreeHit && !IsFullTossBall && !overStepBall && ballSpotLength <= 14.8f)
                if (!isHardcoded && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.PlayModeSelected != 6 && CanProduceEdge() && !isLineFreeHitActive && !isFullToss && !isOversteppedDelivery && ballSpotLength <= 14.8f)
                {
                    isSlipShot = false;
                    _ballAngle = 95f + DetRange(0.1f, 0.6f);
                    isEdgeCaught = true;
                    currentWicketKeeperStatus = string.Empty;
                    if (Singleton<GameData>.instance != null)
                    {
                        disableRunCancelBtn();
                    }
                    if (activeFielders.Count > 0)
                    {
                        activeFielders.Clear();
                    }
                    previousEdgeCatch = isEdgeCaught;
                    SetUltraEdgeDecision();
                    if (canAIAskForReview || canUserAskForReview)
                    {
                        isUltraEdgeCutscenePlaying = true;
                        PlaceUltraEdgeCam();
                    }
                }
            }
            else if (isPowerShotActive && (currentShotPlayed == "bt6HookShot" || currentShotPlayed == "bt6SquareCut" || currentShotPlayed == "pullShot") && hasTopEdge)
            {
                if (DetRange(0, 10) > 3)
                {
                    isSlipShot = false;
                    _ballAngle = DetRange(95, 125);
                }
                else
                {
                    isSlipShot = false;
                    _ballAngle = DetRange(50, 80);
                }
            }
            if (isHardcoded && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.PlayModeSelected != 6 && !isLineFreeHitActive && !isFullToss && ballSpotLength <= 14.8f)
            {
                isSlipShot = false;
                _ballAngle = 95f + DetRange(0.1f, 0.6f);
                isEdgeCaught = true;
                currentWicketKeeperStatus = string.Empty;
                if (Singleton<GameData>.instance != null)
                {
                    disableRunCancelBtn();
                }
                if (activeFielders.Count > 0)
                {
                    activeFielders.Clear();
                }
                previousEdgeCatch = isEdgeCaught;
                SetUltraEdgeDecision();
                if (canAIAskForReview || canUserAskForReview)
                {
                    isUltraEdgeCutscenePlaying = true;
                    PlaceUltraEdgeCam();
                }
            }
            if (batsmanHand == "left")
            {
                _ballAngle = 180f - _ballAngle + 360f;
                _ballAngle %= 360f;
            }
        }


        if (Singleton<LeftFovLerp>.instance != null)
        {
            Singleton<LeftFovLerp>.instance.setBallAngle(_ballAngle);
        }
        if (Singleton<RightSmoothFov>.instance != null)
        {
            Singleton<RightSmoothFov>.instance.setBallAngle(_ballAngle);
        }
        if (Singleton<MainCameraController>.instance != null)
        {
            Singleton<MainCameraController>.instance.setBallAngle(_ballAngle);
        }
        Singleton<GameData>.instance.GetFinalBallAngle(_ballAngle);
        if (_ballAngle > 20f && _ballAngle < 90f)
        {
            isBallToFineLeg = true;
        }
        bool ballOutsideBatReach = false;
        // Lockstep: this bool feeds the shot tables, so both clients must evaluate it against the SAME batsman x.
        // The streamed batsman transform lags at high ping — use the x captured in the swing input packet.
        float lockstepAwareBatterX = LockstepActive ? _lockstepBatterX : _batsmanTransform.position.x;
        if (batsmanHand == "right")
        {
            if (matchBallTransform.position.x > lockstepAwareBatterX - 0.95f)
            {
                ballOutsideBatReach = false;
            }
            else
            {
                ballOutsideBatReach = true;
            }
        }
        else if (batsmanHand == "left")
        {
            if ((double)matchBallTransform.position.x < (double)lockstepAwareBatterX + 0.75)
            {
                ballOutsideBatReach = false;
            }
            else
            {
                ballOutsideBatReach = true;
            }
        }
        DebugLogger.PrintWithSize("Shot played: " + currentShotPlayed);
        if (currentShotPlayed == "bt6StraightDrive" && !isReplayModeActive)
        {
            if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && !isHardcoded && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.EDGECATCH && CONTROLLER.PlayModeSelected != 6 && !isLineFreeHitActive && !isFullToss && !isOversteppedDelivery && ballSpotLength <= 14.8f)
            {
                isSlipShot = false;
                isEdgeCaught = true;
                ballOutsideBatReach = true;
                currentWicketKeeperStatus = string.Empty;
                if (Singleton<GameData>.instance != null)
                {
                    disableRunCancelBtn();
                }
                if (activeFielders.Count > 0)
                {
                    activeFielders.Clear();
                }
                previousEdgeCatch = isEdgeCaught;
                SetUltraEdgeDecision();
                if (canAIAskForReview || canUserAskForReview)
                {
                    isUltraEdgeCutscenePlaying = true;
                    PlaceUltraEdgeCam();
                }
            }
        }
        else if (isPowerShotActive && (currentShotPlayed == "bt6HookShot" || currentShotPlayed == "bt6SquareCut" || currentShotPlayed == "pullShot") && hasTopEdge)
        {
            if (CONTROLLER.BowlerHand == "right")
            {
                isSlipShot = false;
            }
            else
            {
                isSlipShot = false;

            }
        }
        if (isHardcoded && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && isHardcoded && CONTROLLER.PlayModeSelected != 4 && CONTROLLER.PlayModeSelected != 5 && CONTROLLER.PlayModeSelected != 6 && !isLineFreeHitActive && !isFullToss && ballSpotLength <= 14.8f)
        {
            isSlipShot = false;

            isEdgeCaught = true;
            ballOutsideBatReach = true;
            currentWicketKeeperStatus = string.Empty;
            if (Singleton<GameData>.instance != null)
            {
                disableRunCancelBtn();
            }
            if (activeFielders.Count > 0)
            {
                activeFielders.Clear();
            }
            previousEdgeCatch = isEdgeCaught;
            SetUltraEdgeDecision();
            if (canAIAskForReview || canUserAskForReview)
            {
                isUltraEdgeCutscenePlaying = true;
                PlaceUltraEdgeCam();
            }
        }
        if (Singleton<GameData>.instance != null && !isEdgeCaught && !isUltraEdgeCutscenePlaying)
        {
            Singleton<GameData>.instance.PlayGameSound("Bat");
        }

        Singleton<AIFieldingSetupManager>.instance.lastHittedAngle = _ballAngle;

        // Lockstep acceptance BUG: this launch-physics block (hVel/launchAngle/arc/firstBounce) was
        // batting-only — the follower ran the angle tables above but SKIPPED this, then adopted the legacy
        // relayed CONTROLLER.HORIZONTALSPEED (never set under the isDriveArcShot, RPC_ChangeBallAngle is ignored)
        // → hVel=0, ball dead on the pitch. Both clients must run the same seeded physics.
        if ((CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            || (ConstantsData_M.useDeterministicContact && CONTROLLER.PlayModeSelected == 8))
        {
            if (currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense")
            {
                ////ConstantsData_M.MpLog("DEFENCE");
                horizontalVelocity = 5 + DetRange(-1, 1);
                launchAngle = 270f;
                arcHeight = batContactHeight;
                firstBounceDistance = batContactHeight * 3f;
                angleChangeRate = 90f / firstBounceDistance * horizontalVelocity;
            }
            else if (isSlipShot)
            {
                ////ConstantsData_M.MpLog("SLIPSHOT");
                firstBounceDistance = 19f;
                horizontalVelocity = 20f + DetRange(0f, 4f);
                arcHeight = DetRange(2, 4);
                float slipLaunchLiftDeg = Mathf.Asin(batContactHeight / arcHeight) * radToDeg;
                launchAngle = 180f + slipLaunchLiftDeg;
                angleChangeRate = (180f - slipLaunchLiftDeg) / firstBounceDistance * horizontalVelocity;
            }
            else if (isPowerShotActive || isPowerShotActiveSaved)
            {
                ////ConstantsData_M.MpLog("POOOWER SHOTTTT");
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
                float powerDistanceBase = 0f;
                float confidenceVal = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal;
                powerDistanceBase = ((!(confidenceVal * 6f < 20f)) ? (confidenceVal * 6f) : 20f);
                if (CONTROLLER.totalOvers * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls < 12)
                {
                    powerDistanceBase += 10f;
                }
                if (isBatsmanConfident)
                {
                    if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
                    //if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
                    {
                        firstBounceDistance = confidenceVal * 4f + (float)DetRange(25, 30);
                        firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                    }
                    else
                    {
                        firstBounceDistance = confidenceVal * 6.5f;
                    }
                    horizontalVelocity = confidenceVal + 15f + (float)DetRange(1, 6);
                }
                else
                {
                    firstBounceDistance = confidenceVal * 6.5f - Mathf.Abs(0.8f - desiredAnimationSpeed) * 10f;
                    firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                    horizontalVelocity = 22 + DetRange(1, 6);
                }
                float bigHitChance = 0f;
                bigHitChance = ((CONTROLLER.PlayModeSelected != 7) ? (confidenceVal / 10f * 2f) : (confidenceVal / 10f * 1f));
                if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex || CONTROLLER.PlayModeSelected == 8)
                {
                    bigHitChance += 3.5f;
                }
                if (CONTROLLER.difficultyMode == "hard" && CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex || CONTROLLER.PlayModeSelected != 8)
                {
                    bigHitChance += 3f;
                }
                if ((float)DetRange(0, 10) < bigHitChance && currentShotPlayed != "bt6LateCut" && currentShotPlayed != "lateCutLowHeight")
                {
                    firstBounceDistance = DetRange(80, 105);
                }
                else
                {
                    if ((float)DetRange(0, 10) > confidenceVal || firstBounceDistance > 90f)
                    {
                        if (CONTROLLER.totalOvers * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls < 12)
                        {
                            if (DetRange(0, 10) < 2)
                            {
                                firstBounceDistance -= DetRange(10, 20);
                            }
                        }
                        else
                        {
                            firstBounceDistance -= DetRange(10, 20);
                        }
                    }
                    if (firstBounceDistance < 40f)
                    {
                        firstBounceDistance = 40 + DetRange(10, 20);
                    }
                }
                if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
                {
                    if (isPerfectShot)
                    {
                        firstBounceDistance = 80 + DetRange(5, 15);
                    }
                    else if (isMistimedShot && firstBounceDistance > 60f)
                    {
                        firstBounceDistance = 40 + DetRange(10, 20);
                    }
                    else
                    {
                        firstBounceDistance *= firstBounceSpeedMultiplier;
                    }
                }
                if (CONTROLLER.difficultyMode == "hard" && BattingBy == "user" && !isEdgeCaught)
                {
                    firstBounceDistance *= 0.9f;
                    firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                }
                if (currentShotPlayed == "bt6LateCut" || currentShotPlayed == "lateCutLowHeight" || currentShotPlayed == "bt6ReverseSweep")
                {
                    firstBounceDistance /= 4f;
                    firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                    horizontalVelocity = 18 + DetRange(0, 5);
                }
                if (hasTopEdge && !isReplayModeActive)
                {
                    horizontalVelocity = 9 + DetRange(2, 3);
                    firstBounceDistance /= 2f;
                    firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                    arcHeight = firstBounceDistance / (float)(2 + DetRange(0, 2));
                    float topEdgeLaunchLiftDeg = Mathf.Asin(batContactHeight / arcHeight) * radToDeg;
                    launchAngle = 180f + topEdgeLaunchLiftDeg;
                    angleChangeRate = (180f - topEdgeLaunchLiftDeg) / firstBounceDistance * horizontalVelocity;
                }
                else if (!isEdgeCaught && !isReplayModeActive)
                {
                    arcHeight = firstBounceDistance / (float)(6 + DetRange(-1, 2));
                    float cleanHitLaunchLiftDeg = Mathf.Asin(batContactHeight / arcHeight) * radToDeg;
                    launchAngle = 180f + cleanHitLaunchLiftDeg;
                    angleChangeRate = (180f - cleanHitLaunchLiftDeg) / firstBounceDistance * horizontalVelocity;
                }
            }
            else if (!isPowerShotActive && !hasTopEdge && !isEdgeCaught && !previousEdgeCatch)
            {
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
                if (_ballAngle > 250f && _ballAngle < 290f)
                {
                    horizontalVelocity = 15 + DetRange(0, 10);
                    launchAngle = 270f;
                    horizontalVelocity *= horizontalSpeedAdjustment;
                    arcHeight = batContactHeight;
                    firstBounceDistance = batContactHeight * 10f;
                    firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                    angleChangeRate = 90f / firstBounceDistance * horizontalVelocity;
                }
                else
                {
                    if (batContactHeight < 0.5f)
                    {
                        horizontalVelocity = 15 + DetRange(0, 10);
                    }
                    else
                    {
                        horizontalVelocity = 25f;
                    }
                    arcHeight = batContactHeight * 2f;
                    if (currentShotPlayed == "bt6OffDrive" && DetRange(0, 10) < 8)
                    {
                        arcHeight = batContactHeight;
                    }
                    horizontalVelocity *= horizontalSpeedAdjustment;
                    firstBounceDistance = arcHeight * 10f;
                    firstBounceDistance *= 1f + powerMultiplier * (float)controlFactor;
                    float groundShotLaunchLiftDeg = Mathf.Asin(batContactHeight / arcHeight) * radToDeg;
                    launchAngle = 180f + groundShotLaunchLiftDeg;
                    angleChangeRate = (180f - groundShotLaunchLiftDeg) / firstBounceDistance * horizontalVelocity;
                }
            }
            if (Time.time > ballReleaseTime + optimalShotActivationTiming && batsmanCompletedShot && CONTROLLER.PlayModeSelected == 8
                && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)   // lockstep: the follower runs this block too now — only the batter relays
            {
                if (!_ballAngleRPCSent)  // only fire once per delivery; pad contact can override below
                {
                    _ballAngleRPCSent = true;
                    if (_ballAngleRPCCoroutine != null) StopCoroutine(_ballAngleRPCCoroutine);
                    // Relay the bat-contact world position as the shot's launch origin so the bowling client
                    // launches from the exact spot the batter hit it (fixes "shot connects on batting, miss on bowling").
                    _ballAngleRPCCoroutine = StartCoroutine(CallRPCChangeBallAngle(_ballAngle, horizontalVelocity, launchAngle, arcHeight, firstBounceDistance, angleChangeRate, batContactHeight, horizontalSpeedAdjustment, currentBallStatus, isEdgeCaught, ballConnectionPositionSaved));
                }



            }
        }
        else
        {
            if (currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense")
            {

            }
            else if (isSlipShot)
            {

            }
            else if (isPowerShotActive || isPowerShotActiveSaved)
            {
                ////ConstantsData_M.MpLog("POOOWER SHOTTTT");
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
                float powerDistanceBase = 0f;
                float confidenceVal = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal;
                powerDistanceBase = ((!(confidenceVal * 6f < 20f)) ? (confidenceVal * 6f) : 20f);
                if (CONTROLLER.totalOvers * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls < 12)
                {
                    powerDistanceBase += 10f;
                }
                if (isBatsmanConfident)
                {
                    if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
                    {
                    }
                    else
                    {
                    }
                }
                else
                {
                }
                float bigHitChance = 0f;
                bigHitChance = ((CONTROLLER.PlayModeSelected != 7) ? (confidenceVal / 10f * 2f) : (confidenceVal / 10f * 1f));
                if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex || CONTROLLER.PlayModeSelected == 8)
                {
                    bigHitChance += 3.5f;
                }
                if (CONTROLLER.difficultyMode == "hard" && CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex || CONTROLLER.PlayModeSelected != 8)
                {
                    bigHitChance += 3f;
                }
                if (3f < bigHitChance && currentShotPlayed != "bt6LateCut" && currentShotPlayed != "lateCutLowHeight")
                {

                }
                else
                {
                    if (5.3f > confidenceVal || firstBounceDistance > 90f)
                    {
                        if (CONTROLLER.totalOvers * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls < 12)
                        {
                            if (CONTROLLER.BowlerType < 2)
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                    if (firstBounceDistance < 40f)
                    {

                    }
                }
                if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
                {

                }
                if (CONTROLLER.difficultyMode == "hard" && BattingBy == "user" && !isEdgeCaught)
                {
                }
                if (currentShotPlayed == "bt6LateCut" || currentShotPlayed == "lateCutLowHeight" || currentShotPlayed == "bt6ReverseSweep")
                {
                }
                if (hasTopEdge && !isReplayModeActive)
                {

                }
                else if (!isEdgeCaught && !isReplayModeActive)
                {

                }
            }
            else if (!isPowerShotActive && !hasTopEdge && !isEdgeCaught && !previousEdgeCatch)
            {
                ////ConstantsData_M.MpLog("NOT POWER SHOT");
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
                if (_ballAngle > 250f && _ballAngle < 290f)
                {

                }
                else
                {
                    if (batContactHeight < 0.5f)
                    {

                    }
                    else
                    {
                    }
                    if (currentShotPlayed == "bt6OffDrive" && CONTROLLER.BowlerType > 1)
                    {
                    }

                }
            }
            horizontalVelocity = CONTROLLER.HORIZONTALSPEED;
            launchAngle = CONTROLLER.BALLPROJECTILEANGLE;
            arcHeight = CONTROLLER.BALLPROJECTILEHEIGHT;
            angleChangeRate = CONTROLLER.BALLPROJECTILEANGLEPERSECOND;
            firstBounceDistance = CONTROLLER.BALLTIMINGFIRSTBOUNCEDISTANCE;
            batContactHeight = CONTROLLER.BALLBATMEETINGHEIGHT;
            horizontalSpeedAdjustment = CONTROLLER.HORIZONTALSPEEDMULTIPLIER;
        }


        if (!isReplayModeActive)
        {
            savedBallAngle = _ballAngle;
            savedHorizontalBallSpeed = horizontalVelocity;
            savedBallLaunchAngle = launchAngle;
            savedBallLaunchHeight = arcHeight;
            savedBallFirstBounceDistance = firstBounceDistance;
            savedBallLaunchAnglePerSecond = angleChangeRate;
            isSlipShotSaved = isSlipShot;
            isBallHitToFineLegSaved = isBallToFineLeg;
            previousEdgeCatch = isEdgeCaught;
        }
        else if (isReplayModeActive)
        {
            isEdgeCaught = previousEdgeCatch;
            if (isEdgeCaught)
            {
                currentWicketKeeperStatus = string.Empty;
            }
            _ballAngle = savedBallAngle;
            horizontalVelocity = savedHorizontalBallSpeed;
            launchAngle = savedBallLaunchAngle;
            arcHeight = savedBallLaunchHeight;
            firstBounceDistance = savedBallFirstBounceDistance;
            angleChangeRate = savedBallLaunchAnglePerSecond;
            isSlipShot = isSlipShotSaved;
            isBallToFineLeg = isBallHitToFineLegSaved;
        }
        if (isEdgeCaught)
        {
            isWicketKeeperCatchingAnimationSelected = false;
            ActivateWicketKeeper();
        }
        distanceToNextPitch = firstBounceDistance;
        ballConnectionTiming = Time.time;
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            isBallHit = true;
        }
        ballFirstBounce.transform.position = new Vector3(ballStartPoint.position.x + firstBounceDistance * Mathf.Cos(_ballAngle * degToRad), ballFirstBounce.transform.position.y, ballStartPoint.position.z + firstBounceDistance * Mathf.Sin(_ballAngle * degToRad));
        if (Singleton<LeftFovLerp>.instance != null)
        {
            Singleton<LeftFovLerp>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (Singleton<RightSmoothFov>.instance != null)
        {
            Singleton<RightSmoothFov>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (Singleton<MainCameraController>.instance != null)
        {
            Singleton<MainCameraController>.instance.setBallFirstBounce(firstBounceDistance);
        }
        if (firstBounceDistance > 55f && !isReplayModeActive && !isUltraEdgeCutscenePlaying)
        {
            Transform transform = UnityEngine.Object.Instantiate(ballImpactEffect, PrimaryBatCollider.transform.position, PrimaryBatCollider.transform.rotation);
            transform.transform.position = matchBall.transform.position;
            transform.transform.localScale = Vector3.one * 0.2f;
        }
        FixBallCatchingSpot();
        if (_ballAngle >= 90f && _ballAngle <= 210f)
        {
            wicketKeeperDirectionAfterBatting = "offSide";
        }
        else if (_ballAngle > 210f && _ballAngle < 330f)
        {
            wicketKeeperDirectionAfterBatting = "straight";
        }
        else
        {
            wicketKeeperDirectionAfterBatting = "legSide";
        }
        RepositionSideCamera();
    }
}
