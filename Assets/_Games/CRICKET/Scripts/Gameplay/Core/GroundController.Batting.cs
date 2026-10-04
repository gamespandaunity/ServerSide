// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.Batting — BATTER INPUT + BOWLING PARAM partial.
// Owns: GetBattingInput (swing/leave detection — lockstep relays ONE input packet; the follower
// never fabricates a leave), shot selection (swipe → currentShotPlayed tables), the batting timing
// meter (UpdateBattingTimingMeter → perfect/mistimed/hSpeedAdj, relayed in the swing packet),
// batsman lateral movement, and FindBowlingParameters (geometry: bowlingSpot → angle/speed/spot
// length — runs on the BOWLING side at release/plan-mint).
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
    public bool isPowerShot()
    {
        return isPowerShotActive;
    }

    public void MoveBatsman()
    {
        if (batsmanHand == "right")
        {
            batsmanObject.transform.position = new Vector3(batsmanObject.transform.position.x + 5f, batsmanObject.transform.position.y, batsmanObject.transform.position.z);
        }
        else
        {
            batsmanObject.transform.position = new Vector3(batsmanObject.transform.position.x - 5f, batsmanObject.transform.position.y, batsmanObject.transform.position.z);
        }
    }

    public void FindBatsmanCanMakeShot()
    {
        ////ConstantsData_M.MpLog("CASEEE 3 :FindBatsmanCanMakeShot");
        //if (ballTransform.position.z > shotActivationMinLimit.transform.position.z && ballTransform.position.z < shotActivationMaxLimit.transform.position.z && !batsmanTriggeredShot)
        if (temporaryPosition.z > shotActivationMinBoundary.transform.position.z && temporaryPosition.z < shotActivationMaxBoundary.transform.position.z && !batsmanTriggeredAttemptedShot)
        {
            isShotAllowed = true;
        }
    }

    private void CustomRayCastForBattingBallMovement()
    {
        if (!isReplayModeActive)
        {
            Vector3 direction = raycastAnchorBallTransform.TransformDirection(Vector3.forward);
            float maxDistance = 0.7f;
            int num = 256;
            num = ~num;
            if (Physics.Raycast(raycastAnchorBallTransform.position, direction, out var hitInfo, maxDistance, num) && hitInfo.collider.gameObject.transform.parent.name == "Black Board Ad" && currentBallStatus == "shotSuccess")
            {
                BallRebouncesFromBoundary();
            }
        }
    }

    private void FindBowlingParameters()
    {
        if (CONTROLLER.PlayModeSelected != 6)
        {
            Vector3 spotLocalToHand = ballStartPositionGO.transform.InverseTransformPoint(bowlingSpot.position);
            _ballAngle = 90f - Mathf.Atan2(spotLocalToHand.x, spotLocalToHand.z) * radToDeg;
            ////ConstantsData_M.MpLog("bOOWLL : " + ballAngle);
            ballSpotLength = (ballStartPositionGO.transform.position - bowlingSpot.position).magnitude;
            horizontalVelocity = 16f + ballBowlingSpeed / 10f * 2f;
            angleChangeRate = 90f / ballSpotLength * horizontalVelocity;
            swingAnglePerSecond = 180f / ballSpotLength * horizontalVelocity;
            if (Singleton<BallSimulationManager>.instance.CanShowBallSimulation())
            {
                Singleton<BallSimulationManager>.instance.SetTempData(_ballAngle, horizontalVelocity, arcHeight, ballSpotLength, swingIntensity, spinFactor);
            }
            if (!isFullToss)
            {
                creaseImpactSpot.transform.position = new Vector3(bowlingSpot.position.x, creaseImpactSpot.transform.position.y, creaseImpactSpot.transform.position.z);
            }
            float creaseDeltaX = creaseImpactSpot.transform.position.x - bowlingSpot.position.x;
            float creaseDeltaZ = creaseImpactSpot.transform.position.z - bowlingSpot.position.z;
            float creaseDistance = Mathf.Sqrt(creaseDeltaX * creaseDeltaX + creaseDeltaZ * creaseDeltaZ);
            float creaseAngleFromSpin = Mathf.Atan2(creaseDeltaX, creaseDeltaZ) * radToDeg - (90f - _ballAngle - spinFactor);
            float creaseHypotenuse = creaseDistance / Mathf.Cos(creaseAngleFromSpin * degToRad);
            float creaseLateralShift = Mathf.Sqrt(creaseHypotenuse * creaseHypotenuse - creaseDistance * creaseDistance);
            if (!isFullToss)
            {
                if (creaseAngleFromSpin < 0f)
                {
                    creaseImpactSpot.transform.position += new Vector3(creaseLateralShift, 0f, 0f);
                }
                else
                {
                    creaseImpactSpot.transform.position -= new Vector3(creaseLateralShift, 0f, 0f);
                }
            }
            if (!isFullToss)
            {
                stumpImpactSpot.transform.position = new Vector3(bowlingSpot.position.x, stumpImpactSpot.transform.position.y, stumpImpactSpot.transform.position.z);
            }
            float stumpDeltaX = stumpImpactSpot.transform.position.x - bowlingSpot.position.x;
            float stumpDeltaZ = stumpImpactSpot.transform.position.z - bowlingSpot.position.z;
            float stumpDistance = Mathf.Sqrt(stumpDeltaX * stumpDeltaX + stumpDeltaZ * stumpDeltaZ);
            float stumpAngleFromSpin = Mathf.Atan2(stumpDeltaX, stumpDeltaZ) * radToDeg - (90f - _ballAngle - spinFactor);
            float stumpHypotenuse = stumpDistance / Mathf.Cos(stumpAngleFromSpin * degToRad);
            float stumpLateralShift = Mathf.Sqrt(stumpHypotenuse * stumpHypotenuse - stumpDistance * stumpDistance);
            if (!isFullToss)
            {
                if (stumpAngleFromSpin < 0f)
                {
                    stumpImpactSpot.transform.position += new Vector3(stumpLateralShift, 0f, 0f);
                }
                else
                {
                    stumpImpactSpot.transform.position -= new Vector3(stumpLateralShift, 0f, 0f);
                }
            }
        }
        else
        {
            Vector3 spotLocalToHandTM = ballStartPositionGO.transform.InverseTransformPoint(bowlingSpot.position);
            _ballAngle = 90f - Mathf.Atan2(spotLocalToHandTM.x, spotLocalToHandTM.z) * radToDeg;
            ballSpotLength = (ballStartPositionGO.transform.position - bowlingSpot.position).magnitude;
            horizontalVelocity = 16f + ballBowlingSpeed / 10f * 2f;
            float tmOversAngle = Multiplayer.oversData[CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6].bowlingAngle[CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls % 6];
            float tmSpinScale = 4f;
            if (!(bowlerType == "fast") && bowlerType == "spin")
            {
                spinFactor = tmSpinScale * tmOversAngle;
                if (Multiplayer.oversData[CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6].bowlerType == "offspin")
                {
                    bowlerSpinType = 1;
                }
                else
                {
                    bowlerSpinType = 2;
                }
                if (bowlerSpinType == 2)
                {
                    spinFactor *= -1f;
                }
            }
            angleChangeRate = 90f / ballSpotLength * horizontalVelocity;
            swingAnglePerSecond = 180f / ballSpotLength * horizontalVelocity;
            creaseImpactSpot.transform.position = new Vector3(bowlingSpot.position.x, creaseImpactSpot.transform.position.y, creaseImpactSpot.transform.position.z);
            float tmCreaseDeltaX = creaseImpactSpot.transform.position.x - bowlingSpot.position.x;
            float tmCreaseDeltaZ = creaseImpactSpot.transform.position.z - bowlingSpot.position.z;
            float tmCreaseDistance = Mathf.Sqrt(tmCreaseDeltaX * tmCreaseDeltaX + tmCreaseDeltaZ * tmCreaseDeltaZ);
            float tmCreaseAngleFromSpin = Mathf.Atan2(tmCreaseDeltaX, tmCreaseDeltaZ) * radToDeg - (90f - _ballAngle - spinFactor);
            float tmCreaseHypotenuse = tmCreaseDistance / Mathf.Cos(tmCreaseAngleFromSpin * degToRad);
            float tmCreaseLateralShift = Mathf.Sqrt(tmCreaseHypotenuse * tmCreaseHypotenuse - tmCreaseDistance * tmCreaseDistance);
            if (tmCreaseAngleFromSpin < 0f)
            {
                creaseImpactSpot.transform.position += new Vector3(tmCreaseLateralShift, 0f, 0f);
            }
            else
            {
                creaseImpactSpot.transform.position -= new Vector3(tmCreaseLateralShift, 0f, 0f);
            }
            stumpImpactSpot.transform.position = new Vector3(bowlingSpot.position.x, stumpImpactSpot.transform.position.y, stumpImpactSpot.transform.position.z);
            float tmStumpDeltaX = stumpImpactSpot.transform.position.x - bowlingSpot.position.x;
            float tmStumpDeltaZ = stumpImpactSpot.transform.position.z - bowlingSpot.position.z;
            float tmStumpDistance = Mathf.Sqrt(tmStumpDeltaX * tmStumpDeltaX + tmStumpDeltaZ * tmStumpDeltaZ);
            float tmStumpAngleFromSpin = Mathf.Atan2(tmStumpDeltaX, tmStumpDeltaZ) * radToDeg - (90f - _ballAngle - spinFactor);
            float tmStumpHypotenuse = tmStumpDistance / Mathf.Cos(tmStumpAngleFromSpin * degToRad);
            float tmStumpLateralShift = Mathf.Sqrt(tmStumpHypotenuse * tmStumpHypotenuse - tmStumpDistance * tmStumpDistance);
            if (tmStumpAngleFromSpin < 0f)
            {
                stumpImpactSpot.transform.position += new Vector3(tmStumpLateralShift, 0f, 0f);
            }
            else
            {
                stumpImpactSpot.transform.position -= new Vector3(tmStumpLateralShift, 0f, 0f);
            }
        }
        if (ballSpotLength > 17.4f)
        {
            ballSpotHeight = 0.2f;
            ballHeightAtStumps = ballSpotHeight;
            return;
        }
        float num23 = angleChangeRate;
        float num24 = arcHeight;
        float num25 = 180f / num23 * horizontalVelocity;
        float num26 = creaseLineLength / num25 * 180f;
        ballSpotHeight = Mathf.Sin(num26 * degToRad) * num24;
        num26 = (creaseLineLength + 1.2f) / num25 * 180f;
        ballHeightAtStumps = Mathf.Sin(num26 * degToRad) * num24;
    }

    private void GetComputerBattingKeyInput()
    {
        int num = 3;
        if (CONTROLLER.difficultyMode == "hard")
        {
            num = 1;
        }
        else if (CONTROLLER.difficultyMode == "medium")
        {
            num = 2;
        }
        else if (CONTROLLER.difficultyMode == "easy")
        {
            num = 3;
        }
        if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls % num == 0 && CONTROLLER.PlayModeSelected != 7)
        {
            isAIHittingInGap = true;
        }
        else
        {
            isAIHittingInGap = false;
        }

        isLeftArrowKeyPressed = false;
        isUpArrowKeyPressed = false;
        isDownArrowKeyPressed = false;
        isRightArrowKeyPressed = false;
        isPowerKeyPressed = false;
        if (isAIHittingInGap)
        {
            checkCountValue = 0;
            AiFieldScan();
            ScanForUserFielders();
        }
        int num2 = UnityEngine.Random.Range(0, 100);
        int num3 = 0;
        float x = creaseImpactSpot.transform.position.x;
        float num4 = 0f;
        float num5 = 5f;
        float num6 = 1f;
        float num7 = 0.5f;
        float num8 = UnityEngine.Random.Range(0, 10);
        float confidenceVal = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal - 1;
        if (isBatsmanConfident)
        {
            num6 = ((CONTROLLER.currentInnings != 1) ? (10f - (float)int.Parse(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].rank) / 5f + (float)(50 / CONTROLLER.totalOvers)) : (10f - (float)int.Parse(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].rank) / 5f + (float)(50 / CONTROLLER.totalOvers)));
            num4 = confidenceVal / 4f + (CONTROLLER.ReqRunRate / 2) / 5f + (float)UnityEngine.Random.Range(0, 2) + num7 / ((float)CONTROLLER.totalOvers + 2);
            num4 -= num4 * ((float)int.Parse(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].rank) / 20f);
            if (CONTROLLER.difficultyMode == "hard")
            {
                num4 += 1f;
            }
            else
            {
                num4 -= 1f;
            }
            if (CONTROLLER.PowerPlay)
            {
                num4 -= 0.5f;
            }
            num5 = num6 / ((float)((CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets + 1) / 2) + 0.5f) % 3f + 5f;
            if (num4 > num5)
            {
                num4 = num5;
            }
            if (num4 < 0f)
            {
                num4 = 0f;
            }
            if (CONTROLLER.PlayModeSelected != 7 && CONTROLLER.totalOvers * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls <= 12)
            {
                num4 = /*((!(CONTROLLER.ReqRunRate > 0f) || !(CONTROLLER.ReqRunRate <= 3f)) ? (num4 + 2f / (float)(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets + 1)) :*/ (float)UnityEngine.Random.Range(1, 2);
            }
            int num9 = UnityEngine.Random.Range(0, 10);
            float num10 = 100f;
            if (CONTROLLER.oversSelectedIndex < 4)
            {
                num10 = 100f;
            }
            else
            {
                num10 = 100f;
            }
            if ((float)num9 <= num4)
            {
                if (CONTROLLER.PlayModeSelected != 7)
                {
                    isPowerKeyPressed = true;
                }
                else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].LoftMeterFillVal >= 100f)
                {
                    isPowerKeyPressed = true;
                }
                else
                {
                    isPowerKeyPressed = false;
                }
            }
            else
            {
                isPowerKeyPressed = false;
            }
        }
        else
        {
            float num11 = 85f;
            if (CONTROLLER.oversSelectedIndex < 4)
            {
                num11 = 100f;
            }
            else
            {
                num11 = 100f;
            }
            if (num8 < 2f || (num8 < 5f && false))
            {
                if (CONTROLLER.PlayModeSelected != 7)
                {
                    isPowerKeyPressed = true;
                }
                else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].LoftMeterFillVal >= 100f)
                {
                    isPowerKeyPressed = true;
                }
                else
                {
                    isPowerKeyPressed = false;
                }
            }
            else
            {
                isPowerKeyPressed = false;
            }
        }
        int max = 0;
        if (CONTROLLER.difficultyMode == "easy")
        {
            max = 20;
        }
        else if (CONTROLLER.difficultyMode == "medium")
        {
            max = 15;
        }
        else if (CONTROLLER.difficultyMode == "hard")
        {
            max = 8;
        }
        float num12 = UnityEngine.Random.Range(0, max);
        if ((num12 != 3f && num12 != 7f) || aiFielderScanList.Count > 6)
        {
        }
        if (isLineFreeHitActive)
        {
            isPowerKeyPressed = true;
        }
        if (batsmanHand == "right")
        {
            if ((double)x < -0.35)
            {
                isLeftArrowKeyPressed = true;
                if (ballSpotLength > 13f)
                {
                    isDownArrowKeyPressed = true;
                }
                if (ballSpotLength < 13f && UnityEngine.Random.Range(0, 100) < 40)
                {
                    isRightArrowKeyPressed = true;
                    isLeftArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                }
                if (ballSpotLength > 16.8f && UnityEngine.Random.Range(0, 100) < 5)
                {
                    isRightArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                    isLeftArrowKeyPressed = true;
                    isUpArrowKeyPressed = true;
                }
            }
            else if ((double)x >= -0.35 && x < 0f)
            {
                isDownArrowKeyPressed = true;
                if ((double)x >= -0.12)
                {
                    isRightArrowKeyPressed = true;
                    if (ballSpotLength > 14.5f)
                    {
                        isDownArrowKeyPressed = false;
                    }
                }
                if (ballSpotLength < 13f && UnityEngine.Random.Range(0, 100) > 80)
                {
                    isRightArrowKeyPressed = true;
                    isDownArrowKeyPressed = false;
                }
            }
            else if (x >= 0f)
            {
                isRightArrowKeyPressed = true;
                if ((double)x >= 0.3)
                {
                    if (bowlerType == "fast")
                    {
                        isUpArrowKeyPressed = true;
                    }
                    else if (UnityEngine.Random.Range(0, 50) > 45)
                    {
                        isUpArrowKeyPressed = true;
                    }
                }
                if (x < 0.05f && ballSpotLength > 16f)
                {
                    int num13 = UnityEngine.Random.Range(0, 100);
                    int num14 = 5;
                    //if (CONTROLLER.PlayModeSelected == 7)
                    //{
                    //	num14 = 50;
                    //}
                    if (num13 < num14)
                    {
                        isLeftArrowKeyPressed = false;
                        isUpArrowKeyPressed = false;
                        isDownArrowKeyPressed = false;
                        isRightArrowKeyPressed = false;
                    }
                    else if (num13 < 55)
                    {
                        isRightArrowKeyPressed = true;
                    }
                    else
                    {
                        isRightArrowKeyPressed = true;
                        isDownArrowKeyPressed = true;
                    }
                }
            }
        }
        else if (batsmanHand == "left")
        {
            if (x > 0.35f)
            {
                isLeftArrowKeyPressed = true;
                if (ballSpotLength > 13f)
                {
                    isDownArrowKeyPressed = true;
                }
                if (ballSpotLength < 13f && UnityEngine.Random.Range(0, 100) < 40)
                {
                    isRightArrowKeyPressed = true;
                    isLeftArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                }
                if (ballSpotLength > 16.8f && UnityEngine.Random.Range(0, 100) < 5)
                {
                    isRightArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                    isLeftArrowKeyPressed = true;
                    isUpArrowKeyPressed = true;
                }
            }
            else if (x <= 0.35f && x > -0.15f)
            {
                isDownArrowKeyPressed = true;
                if (x <= 0.2f && x >= 0f)
                {
                    isRightArrowKeyPressed = true;
                    if (ballSpotLength < 13f && ballSpotLength > 14.5f)
                    {
                        isDownArrowKeyPressed = false;
                    }
                }
                if (x > 0f && ballSpotLength < 13f && UnityEngine.Random.Range(0, 100) > 90)
                {
                    isRightArrowKeyPressed = true;
                    isDownArrowKeyPressed = false;
                }
            }
            else if (x <= -0.15f)
            {
                isRightArrowKeyPressed = true;
                if (x <= -0.2f)
                {
                    if (bowlerType == "fast")
                    {
                        isUpArrowKeyPressed = true;
                    }
                    else if (UnityEngine.Random.Range(0, 50) > 45)
                    {
                        isUpArrowKeyPressed = true;
                    }
                }
                if (x >= -0.1f && ballSpotLength > 16f)
                {
                    isLeftArrowKeyPressed = false;
                    isUpArrowKeyPressed = false;
                    isDownArrowKeyPressed = false;
                    isRightArrowKeyPressed = false;
                }
            }
        }
        num3 = ((!(CONTROLLER.difficultyMode == "hard")) ? 5 : 6);
        int num15 = UnityEngine.Random.Range(0, 10);
        if (isAIHittingInGap)
        {
            DetermineAIShot();
        }
        float num16 = 0f;
        if (batsmanHand == "right")
        {
            num16 = _batsmanTransform.position.x - batsmanInitialXPosition - creaseImpactSpot.transform.position.x;
        }
        else if (batsmanHand == "left")
        {
            num16 = batsmanInitialXPosition - _batsmanTransform.position.x + creaseImpactSpot.transform.position.x;
        }
        if (UnityEngine.Random.Range(0, 10) < num3 && !isAIHittingInGap && CONTROLLER.PlayModeSelected != 7)
        {
            if (isLeftArrowKeyPressed)
            {
                if (isDownArrowKeyPressed)
                {
                    int num17 = UnityEngine.Random.Range(0, 10);
                    if (num17 < 3)
                    {
                        isDownArrowKeyPressed = false;
                        isUpArrowKeyPressed = true;
                    }
                    else if (num17 < 8)
                    {
                        isLeftArrowKeyPressed = false;
                        isRightArrowKeyPressed = true;
                    }
                    else if (num16 > 0.35f)
                    {
                        isDownArrowKeyPressed = false;
                        isLeftArrowKeyPressed = false;
                        isUpArrowKeyPressed = true;
                        isRightArrowKeyPressed = true;
                    }
                }
                else
                {
                    int num17 = UnityEngine.Random.Range(0, 10);
                    if (num17 < 3)
                    {
                        isUpArrowKeyPressed = true;
                    }
                    else if (num17 < 6)
                    {
                        isLeftArrowKeyPressed = false;
                        isDownArrowKeyPressed = true;
                    }
                    else if (num17 < 7 && num16 > 0.35f)
                    {
                        isDownArrowKeyPressed = false;
                        isLeftArrowKeyPressed = false;
                        isUpArrowKeyPressed = true;
                        isRightArrowKeyPressed = true;
                    }
                }
            }
            if (isDownArrowKeyPressed)
            {
                int num17 = UnityEngine.Random.Range(0, 10);
                if (num17 < 4)
                {
                    isRightArrowKeyPressed = true;
                }
                else if (num17 < 8)
                {
                    isDownArrowKeyPressed = false;
                    isRightArrowKeyPressed = true;
                    isUpArrowKeyPressed = true;
                }
                else
                {
                    isLeftArrowKeyPressed = true;
                }
            }
            if (isRightArrowKeyPressed)
            {
                if (isDownArrowKeyPressed)
                {
                    int num17 = UnityEngine.Random.Range(0, 10);
                    if (num17 < 2)
                    {
                        isDownArrowKeyPressed = true;
                        isRightArrowKeyPressed = false;
                    }
                    else if (num17 < 5 && num16 > 0.35f)
                    {
                        isDownArrowKeyPressed = false;
                        isLeftArrowKeyPressed = true;
                        isUpArrowKeyPressed = true;
                        isRightArrowKeyPressed = false;
                    }
                }
                else if (isUpArrowKeyPressed)
                {
                    int num17 = UnityEngine.Random.Range(0, 10);
                    if (num17 < 4)
                    {
                        isUpArrowKeyPressed = false;
                        isDownArrowKeyPressed = true;
                    }
                    else if (num17 < 6)
                    {
                        isUpArrowKeyPressed = false;
                    }
                }
                else
                {
                    int num17 = UnityEngine.Random.Range(0, 10);
                    if (num17 < 2)
                    {
                        isUpArrowKeyPressed = true;
                    }
                    else if (num17 < 4)
                    {
                        isLeftArrowKeyPressed = true;
                        isUpArrowKeyPressed = true;
                    }
                }
            }
        }
        CONTROLLER.prevPowerShot = isPowerKeyPressed;
    }

    private void BattingBallMovement()
    {
        if (isBallPaused || !(currentBallStatus != "throw") || !(currentBallStatus != string.Empty))
        {
            return;
        }
        BallMovement();
        if (launchAngle >= 360f)
        {
            // Runaway-bounce freeze guard (post-shot counterpart of the delivery guard): if the bounce
            // count runs past any real ball, rest the ball so a non-resolving hit ball can't loop forever.
            if (bounceCount > 8)
            {
                launchAngle = 180f;
                angleChangeRate = 0f;
                horizontalVelocity = 0f;
                arcHeight = 0f;
                return;
            }
            bounceCount++;
            launchAngle = 180f;
            if (bounceCount == 1 && DistanceBetweenTwoVector2(matchBall, groundCenterMarker) < playingAreaRadius)
            {
                // MP six/four authority (Bug-3 fix). Post-shot, the BATTING client owns the
                // boundary result and streams boundaryType to the bowling client via
                // RPC_SyncBallShot. BattingBallMovement() runs every frame on BOTH clients, so on
                // the BOWLING follower a phantom local bounce between 50 ms sync packets used to
                // set boundaryType=4 even when the authority's ball cleared the rope (SIX) →
                // "six on one screen, four on the other". The follower must NOT decide it locally.
                bool isLiveMpShotFollower = GameConstants.isWithAI == false
                    && CONTROLLER.PlayModeSelected == 8
                    && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
                    && isBallHit
                    && !isReplayModeActive;
                if (!isLiveMpShotFollower)
                {
                    boundaryType = 4;
                }
            }
            if (!isEdgeCaught)
            {
                if (!applyBallFriction)
                {
                    angleChangeRate *= 1.1f;
                    distanceToNextPitch += 180f / angleChangeRate * horizontalVelocity;
                }
                else if (applyBallFriction)
                {
                    angleChangeRate *= 2f;
                }
                if (isBallReflectedFromBoundary && isBallOnBoundaryLine)
                {
                    arcHeight = fenceHeight / 2f;
                    fenceHeight = arcHeight;
                }
                else
                {
                    arcHeight *= 0.2f;
                }
                if (arcHeight > 2.5f)
                {
                    arcHeight = 2.2f;
                }
            }
        }
        if (applyBallFriction && !isEdgeCaught)
        {
            horizontalVelocity *= (100f - 90f * BallDt()) / 100f;
        }
        if (!isPowerShotActive && !isEdgeCaught)
        {
            if (!applyFrictionReduction && !isReplayModeActive)
            {
                applyFrictionReduction = true;
                velocityDampingFactor = setRandomFriction();
            }
            horizontalVelocity *= (100f - velocityDampingFactor * BallDt()) / 100f;
            if ((double)horizontalVelocity <= 0.4)
            {
                horizontalVelocity = 0.4f;
            }
        }
        else if (!isEdgeCaught)
        {
            velocityDampingFactor = 20f;
        }
        // A settled GROUND ball must stop even if it never bounced more than three times. The stop below is
        // gated on bounceCount > 3, but a low, hard, straight shot ROLLS rather than bounces — bounceCount can
        // sit at 2-3 for the whole delivery, the gate never opens, and the 0.4 speed floor a few lines up then
        // carries the ball at a crawl all the way to the rope. That is the tester's "batter without loft
        // straight shot khelta hai to ball boundary tak nahi jani chahiye": the friction raise to 30 was
        // supposed to pull these up in the field, and the creep undid it. Deterministic — horizontalVelocity
        // and velocityDampingFactor are both relayed, so both clients settle on the same substep.
        if (bounceCount >= 1 && horizontalVelocity <= 0.4f && matchBallTransform != null
            && matchBallTransform.position.y < 0.5f && !isPowerShotActive && !isEdgeCaught)
        {
            horizontalVelocity = 0f;
        }
        if (bounceCount > 3)
        {
            angleChangeRate = 0f;
            // Must be <= the 0.4 floor above (~line 701), not < it: the floor pins a settling ground shot at
            // exactly 0.4, so a strict `< 0.4f` here never fires and the ball creeps to the rope forever at
            // 0.4 u/s (~15u last stretch = ~40s "slow-motion boundary"). <= lets a settled ball actually STOP
            // in the field so fielders collect it (restores 1s/2s/3s off placed ground shots). Deterministic:
            // velocityDampingFactor is relayed, bounceCount is shared, so both clients stop on the same substep.
            if (horizontalVelocity <= 0.4f)
            {
                horizontalVelocity = 0f;
            }
        }
        int num = Mathf.FloorToInt(DistanceBetweenTwoVector2(groundCenterMarker, matchBall));
        int num2 = Mathf.FloorToInt(DistanceBetweenTwoVector2(fielderFocusObjectToCollectBall, groundCenterMarker));
        if (_ballSubstepIsFinal && (float)num > 30f && firstBounceDistance > 70f && boundaryType == 6)
        {
            // R6 (#4): the six-distance meter must show ONLY on a SIX. The distance gate (num>30 + firstBounce>70)
            // also matched a FOUR that travelled far, so demoted fours showed the meter. boundaryType is the
            // authoritative 4-vs-6 result (set from authority ~1695, demoted to 4 ~652), so gate on ==6.
            Singleton<UILookAt>.instance.show(flag: true);
            sixRunCam.enabled = true;
            if ((float)num >= sixDistanceSaved)
            {
                Singleton<UILookAt>.instance.stripText.text = string.Empty + num + " " + LocalizationData.localizationInstance.getText(538);
                Singleton<UILookAt>.instance.PositionSixDistanceProfile(matchBall, batsmanHand);
                sixDistanceSaved = num;
            }
        }
    }

    public void GetUserBattingKeyboardInput()
    {
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
            if (batsmanHand == "right")
            {
                isLeftArrowKeyPressed = true;
            }
            else
            {
                isRightArrowKeyPressed = true;
            }
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (batsmanHand == "right")
            {
                isRightArrowKeyPressed = true;
            }
            else
            {
                isLeftArrowKeyPressed = true;
            }
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (nonStrikerCurrentStatus == "run")
            {
                if (runsScoredThisBall % 2 == 0)
                {
                    if (_batsmanTransform.position.z > 8.8f || _batsmanTransform.position.z < 0f)
                    {
                        return;
                    }
                }
                else if (_batsmanTransform.position.z < -8.8f || _batsmanTransform.position.z > 0f)
                {
                    return;
                }
            }
            if (nonStrikerCurrentStatus == "run" && !isRunCancelled && !isBallOnBoundaryLine)
            {
                isRunCancelled = true;
                cancelRunningBetweenWicket();
            }
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            isPowerKeyPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (DistanceBetweenTwoGameObjects(matchBall, throwTarget) < 1.5f && fielderHasThrown)
            {
                disableRunCancelBtn();
                return;
            }
            if (!isRunCancelled && !isEdgeCaught && didBallHitBat)
            {
                ////ConstantsData_M.MpLog("!!@@##$$ 3: " + takeRun);

                isRunning = true;
            }
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
            if (batsmanHand == "right")
            {
                isLeftArrowKeyPressed = false;
            }
            else
            {
                isRightArrowKeyPressed = false;
            }
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            if (batsmanHand == "right")
            {
                isRightArrowKeyPressed = false;
            }
            else
            {
                isLeftArrowKeyPressed = false;
            }
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            isPowerKeyPressed = false;
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            isRunning = false;
        }
        if (Input.GetMouseButton(0))
        {
            batsmanStepSize = 1f;
        }
        if (Input.GetMouseButtonUp(0))
        {
            batsmanStepSize = 0.3f;
        }
    }

    private void GetUserBattingInput()
    {
        if (!isReplayModeActive)
        {
            GetUserBattingKeyboardInput();
        }
        if (canBatsmanMoveLaterally)
        {
            if (!isReplayModeActive)
            {
                if (isLeftArrowKeyPressed)
                {
                    if (!isLeftArrowPressedSaved)
                    {
                        leftArrowKeyDownTimes.Add(Time.time - bowlerRunUpStartTime);
                        isLeftArrowPressedSaved = true;
                    }
                }
                else if (!isLeftArrowKeyPressed && isLeftArrowPressedSaved)
                {
                    leftArrowKeyUpTimes.Add(Time.time - bowlerRunUpStartTime);
                    isLeftArrowPressedSaved = false;
                }
                if (isRightArrowKeyPressed)
                {
                    if (!isRightArrowPressedSaved)
                    {
                        rightArrowKeyDownTimes.Add(Time.time - bowlerRunUpStartTime);
                        isRightArrowPressedSaved = true;
                    }
                }
                else if (!isRightArrowKeyPressed && isRightArrowPressedSaved)
                {
                    rightArrowKeyUpTimes.Add(Time.time - bowlerRunUpStartTime);
                    isRightArrowPressedSaved = false;
                }
            }
            if (isReplayModeActive)
            {
                float num = Time.time - bowlerRunUpStartTime;
                bool flag = false;
                for (int i = 0; i < leftArrowKeyDownTimes.Count; i++)
                {
                    float num2 = leftArrowKeyDownTimes[i];
                    float num3 = leftArrowKeyUpTimes[i];
                    if (num >= num2 && num <= num3)
                    {
                        flag = true;
                    }
                }
                isLeftArrowKeyPressed = flag;
                bool flag2 = false;
                for (int j = 0; j < rightArrowKeyDownTimes.Count; j++)
                {
                    float num4 = rightArrowKeyDownTimes[j];
                    float num5 = rightArrowKeyUpTimes[j];
                    if (num >= num4 && num <= num5)
                    {
                        flag2 = true;
                    }
                }
                isRightArrowKeyPressed = flag2;
            }
            if (isLeftArrowKeyPressed)
            {
                if (batsmanHand == "right" && rightHandedBatsmanForwardLimit.transform.position.x < _batsmanTransform.position.x)
                {
                    _batsmanTransform.position -= new Vector3(batsmanStepSize * Time.deltaTime, 0f, 0f);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {
                            //    view.RPC("ChangeBatsmanPosition", RpcTarget.OthersBuffered, _batsmanTransform.position);                                              //Photon Removal
                            if (CricketNetworkManager.ReadyToSend) CricketNetworkManager.instance.CmdChangeBatsmanPosition(staticVariables.UserProfiledata.user._id, _batsmanTransform.position);
                        }
                    }
                }
                else if (batsmanHand == "left" && leftHandedBatsmanForwardLimit.transform.position.x > _batsmanTransform.position.x)
                {
                    _batsmanTransform.position += new Vector3(batsmanStepSize * Time.deltaTime, 0f, 0f);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                        {
                            // view.RPC("ChangeBatsmanPosition", RpcTarget.OthersBuffered, _batsmanTransform.position);
                            if (CricketNetworkManager.ReadyToSend) CricketNetworkManager.instance.CmdChangeBatsmanPosition(staticVariables.UserProfiledata.user._id, _batsmanTransform.position);
                        }

                    }

                }
                if (!isBatsmanMovingLaterally)
                {
                    batsmanAnim.CrossFade("bt6Forward");
                    if (batsmanStepSize == 1f)
                    {
                        batsmanAnim["bt6Forward"].speed = 1f;
                    }
                    else
                    {
                        batsmanAnim["bt6Forward"].speed = 0.5f;
                    }
                    isBatsmanMovingLaterally = true;
                }
            }
            else if (isRightArrowKeyPressed)
            {
                if (batsmanHand == "right" && rightHandedBatsmanBackwardLimit.transform.position.x > _batsmanTransform.position.x)
                {
                    _batsmanTransform.position += new Vector3(batsmanStepSize * Time.deltaTime, 0f, 0f);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                            if (CricketNetworkManager.ReadyToSend) CricketNetworkManager.instance.CmdChangeBatsmanPosition(staticVariables.UserProfiledata.user._id, _batsmanTransform.position);
                        //{
                        //    view.RPC("ChangeBatsmanPosition", RpcTarget.OthersBuffered, _batsmanTransform.position);                                    //Photon Removal
                        //}

                    }

                }
                else if (batsmanHand == "left" && leftHandedBatsmanBackwardLimit.transform.position.x < _batsmanTransform.position.x)
                {
                    _batsmanTransform.position -= new Vector3(batsmanStepSize * Time.deltaTime, 0f, 0f);
                    if (CONTROLLER.PlayModeSelected == 8)
                    {
                        if (GameConstants.isWithAI == false)
                            if (CricketNetworkManager.ReadyToSend) CricketNetworkManager.instance.CmdChangeBatsmanPosition(staticVariables.UserProfiledata.user._id, _batsmanTransform.position);
                        //{
                        //    view.RPC("ChangeBatsmanPosition", RpcTarget.OthersBuffered, _batsmanTransform.position);
                        //}
                    }
                }
                if (!isBatsmanMovingLaterally)
                {
                    batsmanAnim.CrossFade("bt6Backward");
                    if (batsmanStepSize == 1f)
                    {
                        batsmanAnim["bt6Backward"].speed = 1f;
                    }
                    else
                    {
                        batsmanAnim["bt6Backward"].speed = 0.5f;
                    }
                    isBatsmanMovingLaterally = true;
                }
            }
            else if (isBatsmanMovingLaterally)
            {
                isBatsmanMovingLaterally = false;
                batsmanAnim.CrossFade("WCCLite_BatsmanTapLoop");
            }
        }
        else if (!canBatsmanMoveLaterally && isBatsmanMovingLaterally)
        {
            isBatsmanMovingLaterally = false;
            batsmanAnim.CrossFade("WCCLite_BatsmanTapLoop");
        }
    }

    //[PunRPC]
    public void ChangeBatsmanPosition(Vector3 BatsmanPos)
    {
        _batsmanTransform.position = BatsmanPos;
    }

    private void GetBattingInput()
    {
        if (BattingBy == "user")
        {
            GetUserBattingInput();
            if (currentActionState == 3 && matchBallTransform.position.z > 2.5f && !isTouchDeviceShotInputEnabled && !isReplayModeActive)
            {
                Singleton<GameData>.instance.GetShotSelected();
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isMouseDown = false;
        }
        if (matchBallTransform.position.z > shotActivationMaxBoundary.transform.position.z && currentActionState == 3)
        {
            HideBowlingSpot();
            ShowFullTossSpot(_Value: false);
            // Lockstep: only the BATTER declares the leave. This block runs on BOTH clients, and at high ping
            // the follower's ball passes this boundary BEFORE the swing packet arrives — a locally fabricated
            // leave resolved (and locked) the contact as a miss, so the real shot could never apply.
            bool lockstepFollower = LockstepActive && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex;
            if (!lockstepFollower && ((!batsmanTriggeredAttemptedShot && !isReplayModeActive) || (!batsmanTriggeredAttemptedShot && isReplayModeActive && savedPlayedShot == "bt6Leave")))
            {
                batsmanTriggeredAttemptedShot = true;
                currentBatsmanAnimation = "bt6Leave";
                currentShotPlayed = "bt6Leave";
                _lockstepSendSwingNextFrame = LockstepActive;   // lockstep: relay the LEAVE too — the follower's keeper hold releases on it
                canShowCountdown = false;
            }
        }
        if (isReplayModeActive)
        {
            if (isShotAllowed && !batsmanTriggeredAttemptedShot && matchBallTransform.position.z >= savedShotExecutionZPosition)
            {
                batsmanTriggeredAttemptedShot = true;
            }
        }
        else
        {
            if ((((!Input.GetKeyDown(KeyCode.S) && !isTouchDeviceShotInputEnabled) || !(BattingBy == "user")) && !(BattingBy == "computer")) || !isShotAllowed || batsmanTriggeredAttemptedShot)
            {
                return;
            }
            batsmanTriggeredAttemptedShot = true;
            _lockstepSendSwingNextFrame = LockstepActive;   // lockstep: ship the input next frame (selection completes below)
            isPowerShotActive = isPowerKeyPressed;
            if (!isPowerShotActive && BattingBy == "user")
            {
                isPowerShotActive = Singleton<BattingControls>.instance.GetPowerIconStatus();
            }
            float batsmanLateralOffset = 0f;
            if (batsmanHand == "right")
            {
                batsmanLateralOffset = _batsmanTransform.position.x - batsmanInitialXPosition - creaseImpactSpot.transform.position.x;
            }
            else if (batsmanHand == "left")
            {
                batsmanLateralOffset = batsmanInitialXPosition - _batsmanTransform.position.x + creaseImpactSpot.transform.position.x;
            }
            if (isLeftArrowKeyPressed && isUpArrowKeyPressed)
            {
                if ((double)ballSpotLength < 13.5)
                {
                    currentBatsmanAnimation = "bt6LateCut";
                    currentShotPlayed = "bt6LateCut";
                }
                else if (batsmanLateralOffset >= 0.5f)
                {
                    currentBatsmanAnimation = "lateCutLowHeight";
                    currentShotPlayed = "lateCutLowHeight";
                }
                else if (bowlerType == "spin" || bowlerType == "medium")
                {
                    currentBatsmanAnimation = "reverseSweepSlowBall";
                    currentShotPlayed = "reverseSweepSlowBall";
                }
                else if (batsmanLateralOffset <= 0.05f)
                {
                    currentBatsmanAnimation = "bt6ReverseSweep";
                    currentShotPlayed = "bt6ReverseSweep";
                }
                else
                {
                    currentBatsmanAnimation = "reverseSweepSlowBall";
                    currentShotPlayed = "reverseSweepSlowBall";
                }
            }
            else if (isLeftArrowKeyPressed && isDownArrowKeyPressed)
            {
                if (isPowerShotActive)
                {
                    if (batsmanLateralOffset >= 0.65f)
                    {
                        currentBatsmanAnimation = "WCCLite_LoftedSquareDrive";
                        currentShotPlayed = "WCCLite_LoftedSquareDrive";
                    }
                    else if (batsmanLateralOffset < 0.4f)
                    {
                        currentBatsmanAnimation = "loftOffSide";
                        currentShotPlayed = "loftOffSide";
                    }
                    else
                    {
                        currentBatsmanAnimation = "WCCLite_HarbhajanShot";
                        currentShotPlayed = "WCCLite_HarbhajanShot";
                    }
                }
                else
                {
                    if (batsmanLateralOffset <= 0.1f && (double)ballSpotLength < 13.5)
                    {
                        currentBatsmanAnimation = "extraCoverDrive";
                        currentShotPlayed = "extraCoverDrive";
                    }
                    else if (batsmanLateralOffset >= 0.3f && ballSpotLength > 13f)
                    {
                        currentBatsmanAnimation = "backFootOffDrive";
                        currentShotPlayed = "backFootOffDrive";
                    }
                    else if (ballSpotLength < 13f && batsmanLateralOffset >= 0.3f)
                    {
                        currentBatsmanAnimation = "bt6OffDrive";
                        currentShotPlayed = "bt6OffDrive";
                    }
                    if (batsmanLateralOffset >= 0.5f && ballSpotLength < 13f)
                    {
                        currentBatsmanAnimation = "WCCLite_BackFootPunch";
                        currentShotPlayed = "WCCLite_BackFootPunch";
                    }
                    else if (batsmanLateralOffset <= 0.3f)
                    {
                        currentBatsmanAnimation = "bt6CoverDrive";
                        currentShotPlayed = "bt6CoverDrive";
                    }
                    else
                    {
                        currentBatsmanAnimation = "insideOutCoverDrive";
                        currentShotPlayed = "insideOutCoverDrive";
                    }
                }
            }
            else if (isRightArrowKeyPressed && isUpArrowKeyPressed)
            {
                if (bowlerType == "fast" && ballSpotLength <= 15f && isPowerShotActive)
                {
                    currentBatsmanAnimation = "WCCLite_Dilscoop";
                    currentShotPlayed = "WCCLite_Dilscoop";
                }
                else if (batsmanLateralOffset >= 0.5f && ballSpotLength <= 15f)
                {
                    currentBatsmanAnimation = "WCCLite_ABdeVilliers_Shot";
                    currentShotPlayed = "WCCLite_ABdeVilliers_Shot";
                }
                else if (ballSpotLength >= 14.5f && batsmanLateralOffset < 0.15f)
                {
                    currentBatsmanAnimation = "WCCLite_YorkerLegGlanceNew";
                    currentShotPlayed = "WCCLite_YorkerLegGlanceNew";
                }
                else if (ballSpotLength <= 15f && !isPowerShotActive && batsmanLateralOffset < 0.1f)
                {
                    currentBatsmanAnimation = "WCCLite_LegGlanceNew";
                    currentShotPlayed = "WCCLite_LegGlanceNew";
                }
                else if (ballSpotLength < 15f)
                {
                    if (bowlerType == "medium")
                    {
                        currentBatsmanAnimation = "bt6LegGlance";
                        currentShotPlayed = "bt6LegGlance";
                    }
                    else
                    {
                        currentBatsmanAnimation = "WCCLite_LegGlanceNew";
                        currentShotPlayed = "WCCLite_LegGlanceNew";
                    }
                }
                else if (bowlerType == "spin")
                {
                    if (isPowerShotActive)
                    {
                        currentBatsmanAnimation = "legGlanceYorkerLength_new";
                        currentShotPlayed = "legGlanceYorkerLength";
                    }
                    else if (ballSpotLength > 13.85f && batsmanLateralOffset < 0.19f)
                    {
                        currentBatsmanAnimation = "bt6Sweep";
                        currentShotPlayed = "bt6Sweep";
                    }
                    else if (ballSpotLength > 14.5f && batsmanLateralOffset < 0f)
                    {
                        currentBatsmanAnimation = "WCCLite_YorkerLegGlanceNew";
                        currentShotPlayed = "WCCLite_YorkerLegGlanceNew";
                    }
                    else
                    {
                        currentBatsmanAnimation = "paddleSweep";
                        currentShotPlayed = "paddleSweep";
                    }
                }
                else if (isPowerShotActive)
                {
                    currentBatsmanAnimation = "legGlanceYorkerLength_new";
                    currentShotPlayed = "legGlanceYorkerLength";
                }
                else if (ballSpotLength > 13.85f && batsmanLateralOffset < 0.19f && bowlerType == "medium")
                {
                    currentBatsmanAnimation = "bt6Sweep";
                    currentShotPlayed = "bt6Sweep";
                }
                else
                {
                    currentBatsmanAnimation = "WCCLite_YorkerLegGlanceNew";
                    currentShotPlayed = "WCCLite_YorkerLegGlanceNew";
                }
            }
            else if (isRightArrowKeyPressed && isDownArrowKeyPressed)
            {
                if (isPowerShotActive)
                {
                    if (ballSpotLength <= 13.5f && batsmanLateralOffset > 0.2f)
                    {
                        currentBatsmanAnimation = "bt6HookShot";
                        currentShotPlayed = "bt6HookShot";
                    }
                    if (ballSpotLength <= 12.8f && batsmanLateralOffset > 0.15f)
                    {
                        currentBatsmanAnimation = "bt6PullShot";
                        currentShotPlayed = "bt6PullShot";
                    }
                    else if (ballSpotLength >= 15.8f && batsmanLateralOffset < 0.15f)
                    {
                        currentBatsmanAnimation = "WCCLite_HelicopterShot";
                        currentShotPlayed = "WCCLite_HelicopterShot";
                    }
                    if (ballSpotLength <= 13f && batsmanLateralOffset > 0.55f)
                    {
                        currentBatsmanAnimation = "lowPullShot";
                        currentShotPlayed = "lowPullShot";
                    }
                    else if (batsmanLateralOffset >= 0.6f && ballSpotLength > 12f)
                    {
                        currentBatsmanAnimation = "powerfulSweepShot";
                        currentShotPlayed = "powerfulSweepShot";
                    }
                    else if (batsmanLateralOffset <= 0.15f)
                    {
                        currentBatsmanAnimation = "loftLegSide";
                        currentShotPlayed = "loftLegSide";
                    }
                    else if (batsmanLateralOffset > 0.15f)
                    {
                        currentBatsmanAnimation = "loftStraight";
                        currentShotPlayed = "loftStraight";
                    }
                    else
                    {
                        currentBatsmanAnimation = "WCCLite_HelicopterShot";
                        currentShotPlayed = "WCCLite_HelicopterShot";
                    }
                }
                else if (ballSpotLength <= 13f && batsmanLateralOffset > 0.55f)
                {
                    currentBatsmanAnimation = "lowPullShot";
                    currentShotPlayed = "lowPullShot";
                }
                else if (ballSpotLength <= 13f && batsmanLateralOffset > -0.5f && batsmanLateralOffset < -0.1f)
                {
                    currentBatsmanAnimation = "runDownOnDrive";
                    currentShotPlayed = "runDownOnDrive";
                }
                else if (ballSpotLength <= 13f && batsmanLateralOffset < 0.1f)
                {
                    currentBatsmanAnimation = "backFootOnDrive_new";
                    currentShotPlayed = "backFootOnDrive";
                }
                else
                {
                    currentBatsmanAnimation = "bt6OnDrive";
                    currentShotPlayed = "bt6OnDrive";
                }
            }
            else if (isDownArrowKeyPressed)
            {
                if (isPowerShotActive)
                {
                    if (batsmanLateralOffset <= 0.1f)
                    {
                        currentBatsmanAnimation = "loftStraight";
                        currentShotPlayed = "loftStraight";
                    }
                    else if (ballSpotLength <= 14f)
                    {
                        currentBatsmanAnimation = "loftStraightShortBall";
                        currentShotPlayed = "loftStraightShortBall";
                    }
                    else
                    {
                        currentBatsmanAnimation = "straightDrivePowerShot";
                        currentShotPlayed = "straightDrivePowerShot";
                    }
                }
                else if (ballSpotLength <= 13.5f && batsmanLateralOffset >= 0.3f)
                {
                    currentBatsmanAnimation = "backFootStraightDrive_new";
                    currentShotPlayed = "backFootStraightDrive";
                }
                else
                {
                    currentBatsmanAnimation = "bt6StraightDrive";
                    currentShotPlayed = "bt6StraightDrive";
                }
            }
            else if (isRightArrowKeyPressed)
            {
                attemptedSquareLegGlance = true;
                if (isPowerShotActive && ballSpotLength > 14f && bowlerType == "spin" && batsmanLateralOffset < 0f)
                {
                    currentBatsmanAnimation = "lowHookShot";
                    currentShotPlayed = "lowHookShot";
                }
                else if (ballSpotLength < 15f && batsmanLateralOffset < 0.1f && isPowerShotActive)
                {
                    currentBatsmanAnimation = "bt6LegGlance";
                    currentShotPlayed = "bt6LegGlance";
                }
                else if (ballSpotLength < 15f && batsmanLateralOffset >= 0.35f)
                {
                    currentBatsmanAnimation = "paddleSweep";
                    currentShotPlayed = "paddleSweep";
                }
                else if (ballSpotLength >= 15f && batsmanLateralOffset < 0.15f)
                {
                    currentBatsmanAnimation = "WCCLite_YorkerLegGlanceNew";
                    currentShotPlayed = "WCCLite_YorkerLegGlanceNew";
                }
                else if (ballSpotLength < 13f)
                {
                    currentBatsmanAnimation = "lowPullShot";
                    currentShotPlayed = "lowPullShot";
                }
                else
                {
                    attemptedSquareLegGlance = true;
                    if (ballSpotLength <= 15f && !isPowerShotActive && batsmanLateralOffset < 0.1f)
                    {
                        currentBatsmanAnimation = "WCCLite_LegGlanceNew";
                        currentShotPlayed = "WCCLite_LegGlanceNew";
                    }
                    else
                    {
                        currentBatsmanAnimation = "legGlanceYorkerLength_new";
                        currentShotPlayed = "legGlanceYorkerLength";
                    }
                }
            }
            else if (isLeftArrowKeyPressed)
            {
                attemptedSquareCutDrive = true;
                if (batsmanLateralOffset <= 0.3f && !isPowerShotActive)
                {
                    if (ballSpotLength < 14f)
                    {
                        currentBatsmanAnimation = "lateSquareDrive_new";
                        currentShotPlayed = "lateSquareDrive";
                    }
                    else
                    {
                        currentBatsmanAnimation = "frontFootOffDrive";
                        currentShotPlayed = "frontFootOffDrive";
                    }
                }
                else if (ballSpotLength < 13f && batsmanLateralOffset >= 0.3f && !isPowerShotActive)
                {
                    currentBatsmanAnimation = "bt6OffDrive";
                    currentShotPlayed = "bt6OffDrive";
                }
                else if (ballSpotLength < 13f)
                {
                    currentBatsmanAnimation = "bt6SquareCut";
                    currentShotPlayed = "bt6SquareCut";
                }
                else if (ballSpotLength < 15f && batsmanLateralOffset <= 0.8f)
                {
                    currentBatsmanAnimation = "lateSquareDrive_new";
                    currentShotPlayed = "lateSquareDrive";
                }
                else
                {
                    currentBatsmanAnimation = "frontFootOffDrive";
                    currentShotPlayed = "frontFootOffDrive";
                }
            }
            else if (ballSpotLength < 15f)
            {
                currentBatsmanAnimation = "backFootDefenseHighBall";
                currentShotPlayed = "backFootDefenseHighBall";
            }
            else if (Mathf.Abs(creaseImpactSpot.transform.position.x) > 0.6f)
            {
                currentBatsmanAnimation = "frontFootOffSideDefense";
                currentShotPlayed = "frontFootOffSideDefense";
            }
            else
            {
                currentBatsmanAnimation = "bt6Defense";
                currentShotPlayed = "bt6Defense";
            }
            // Online MP guard: this AI play-and-miss "leave" only makes sense for an AI batsman. Online
            // (PlayModeSelected==8) this branch runs on the BOWLING FOLLOWER (BattingTeamIndex==opponentTeamIndex)
            // and forces the opponent batsman to visibly LEAVE a ball the batting authority actually played →
            // "ball to keeper on bowling side, shot on batting side" divergence. Skip online; offline AI untouched.
            if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex && CONTROLLER.PlayModeSelected != 8 && batsmanLateralOffset < -0.8f)
            {
                currentBatsmanAnimation = "bt6Leave";
                currentShotPlayed = "bt6Leave";
            }
            if (isHardcoded)
            {
                currentBatsmanAnimation = "bt6StraightDrive";
                currentShotPlayed = "bt6StraightDrive";
            }
            if (currentShotPlayed != "bt6Leave")
            {
                float num2 = ShotVariables.optimalShotTable[currentShotPlayed + "OptimalShotLength"];
                float num3 = ShotVariables.optimalShotTable[currentShotPlayed + "OptimalShotFrame"];
                optimalShotTiming = num2 / horizontalVelocity;
                batOptimalShotReachTime = num3 * animationFrameInterval;
                optimalShotActivationTiming = optimalShotTiming - batOptimalShotReachTime - 0.1f;
            }
        }
        if (CONTROLLER.PlayModeSelected == 8)
        {
            //if (GameConstants.isWithAI == false)
            //{
            //    StartCoroutine(CallShotPlayedRPC(currentShotPlayed, currentBatsmanAnimation, batOptimalShotReachTime, optimalShotActivationTiming, isPowerShotActive));
            //}


        }
    }

    //Photon Removal  [PunRPC]
    public void RPC_ShotPlayed(string SHOTPLAYED, string BATSMANANIM, float BatReachingTimeForOptimalShotLength, float OptimalShotActivationTime, bool PowerShot)
    {
        if (SHOTPLAYED == currentShotPlayed && BATSMANANIM == currentBatsmanAnimation && BatReachingTimeForOptimalShotLength == batOptimalShotReachTime
            && optimalShotActivationTiming == OptimalShotActivationTime && PowerShot == isPowerShotActive)
        {
            return;
        }

        if (isWicketKeeperActive)
        {
            return;
        }

        currentShotPlayed = SHOTPLAYED;
        savedPlayedShot = currentShotPlayed;
        isPowerShotActiveSaved = PowerShot;
        isPowerShotActive = PowerShot;
        currentBatsmanAnimation = BATSMANANIM;
        batsmanTriggeredAttemptedShot = true;
        batOptimalShotReachTime = BatReachingTimeForOptimalShotLength;
        optimalShotActivationTiming = OptimalShotActivationTime;
    }

    private void ExecuteTheShot()
    {
        ////ConstantsData_M.MpLog("CASEE 3 : ExecuteTheShot");
        if (!batsmanTriggeredAttemptedShot)
        {
            return;
        }
        if (Time.timeScale == 0.4f)
        {
            Time.timeScale = 1f;
        }
        if (!(Time.time > ballReleaseTime + optimalShotActivationTiming) || batsmanCompletedShot)
        {
            return;
        }
        if (!isReplayModeActive)
        {
            savedShotExecutionZPosition = matchBallTransform.position.z;
            savedBatsmanShotExecutionPosition = _batsmanTransform.position;
            savedPlayedShot = currentShotPlayed;
            isPowerShotActiveSaved = isPowerShotActive;
        }
        else if (isReplayModeActive)
        {
            _batsmanTransform.position = savedBatsmanShotExecutionPosition;
        }
        batsmanCompletedShot = true;
        // [ShotTiming] diag (always-late / 2nd-team-can't-hit): the shot EXECUTES at the fixed activation time
        // (ballReleaseTime + optimalShotActivationTiming), and the timing meter then reads the ball's z. If the
        // ball is already past z=0 at that instant, the meter shows LATE regardless of when the player tapped.
        // Capture the exact values so the late offset can be pinned to ballReleaseTime vs the deterministic
        // NetworkTime ball clock (netGap), the activation timing, or horizontalVelocity. MP batting-authority only.
        if (CONTROLLER.PlayModeSelected == 8 && !GameConstants.isWithAI
            && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
            ConstantsData_M.MpLog($"[ShotTiming] meterZ={temporaryPosition.z:F2} ballZ={matchBallTransform.position.z:F2} tSinceRelease={(Time.time - ballReleaseTime):F3} activation={optimalShotActivationTiming:F3} hVel={horizontalVelocity:F1} netGap={(Mirror.NetworkTime.time - _ballSimStartNetTime):F3} det={_useDeterministicBallStep} shot={currentShotPlayed} tapped={batsmanTriggeredAttemptedShot}");
        if (currentShotPlayed == "bt6Leave")
        {
            batsmanAnim[currentBatsmanAnimation].speed = 2f;
            if (Singleton<GameData>.instance != null)
            {
                Singleton<GameData>.instance.EnableShot(boolean: false);
            }
        }
        if (currentShotPlayed != "bt6Leave" && currentShotPlayed != string.Empty)
        {
            float num = ShotVariables.optimalShotTable[currentShotPlayed + "OptimalShotLength"];
            float num2 = ShotVariables.optimalShotTable[currentShotPlayed + "OptimalShotFrame"];
            float num3 = num - DistanceBetweenTwoVector2(ballStartPositionGO, matchBall);
            float num4 = num3 / horizontalVelocity;
            float num5 = num2 * animationFrameInterval;
            desiredAnimationSpeed = num5 / num4;
            if (desiredAnimationSpeed > 3f)
            {
                desiredAnimationSpeed = 3f;
            }
        }
        // Online MP guard: AI miss-mechanic (disables the bat colliders so the AI batsman plays-and-misses a
        // straight ball). Online this runs only on the BOWLING FOLLOWER (BattingTeamIndex==opponentTeamIndex) —
        // the follower then can't raycast-hit its own bat, so the opponent's stroke looks like ball-to-keeper on
        // the bowling side even though the batting authority relayed a real contact. Skip online; offline untouched.
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex && CONTROLLER.PlayModeSelected != 8 && creaseImpactSpot.transform.position.x > -0.1f && creaseImpactSpot.transform.position.x < 0.1f)
        {
            float num6 = 0.35f;
            //if (CONTROLLER.PlayModeSelected == 7)
            //{
            //	num6 = 0.05f;
            //}
            if (UnityEngine.Random.value <= num6 && Singleton<GameData>.instance.dotBallCount >= UnityEngine.Random.Range(CONTROLLER.totalOvers, 60))
            {
                PrimaryBatCollider.SetActive(value: false);
                SecondaryBatCollider.SetActive(value: false);
            }
        }
        if (currentShotPlayed == "bt6Defense" || currentShotPlayed == "backFootDefenseHighBall" || currentShotPlayed == "frontFootOffSideDefense")
        {
            PrimaryBatCollider.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            SecondaryBatCollider.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        }
        // Online MP guard: AI-batsman timing-meter update. Online this runs only on the BOWLING FOLLOWER
        // (BattingTeamIndex==opponentTeamIndex), where temporaryPosition is RPC-fed + can drift between snaps →
        // the follower-computed timing diverges from the authority's. Keep this strictly an offline/AI path so
        // only the batting authority's own ball position drives shot timing online. Offline AI untouched.
        if (CONTROLLER.opponentTeamIndex == CONTROLLER.BattingTeamIndex && CONTROLLER.PlayModeSelected != 8)
        {
            UpdateBattingTimingMeter();
            needsBattingTimingMeterNeedleUpdate = false;
        }
        batsmanAnim.CrossFade(currentBatsmanAnimation, 0.02f);
        batsmanAnim[currentBatsmanAnimation].speed = desiredAnimationSpeed;
        batsmanAnim.PlayQueued("WCCLite_BatsmanIdle", QueueMode.CompleteOthers);
        if (currentBallStatus != "bowled")
        {
            for (int i = 0; i < slipFielders.Count; i++)
            {
                if (slipFielders[i] != null)
                {
                    GameObject gameObject = slipFielders[i];
                    gameObject.GetComponent<Animation>().Play("backToIdle");
                }
            }
        }
        isShotExecuted = true;
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
        {
            Invoke("Bowl", 1.2f);
        }
        currentShotExecutionTime = Time.time;
        isComputerBatsmanAttemptingNewRun = true;
        if (Singleton<GameData>.instance != null)
        {
            Singleton<GameData>.instance.EnableShot(boolean: false);
        }
    }

    public void LookForRunByComputerBatsman()
    {
        if (CONTROLLER.PlayModeSelected == 8)
        {
            return;
        }
        if (!(BattingBy != "computer") && canRun)
        {
            int num = ((CONTROLLER.PlayModeSelected == 7) ? 50 : 45);
            if (isBallPickedByFielder && DistanceBetweenTwoVector2(matchBall, throwTarget) < (float)num)
            {
                isRunning = false;
                isAICancelRun = true;
            }
            else if (canRun && !isEdgeCaught && !isAICancelRun && Time.time > currentShotExecutionTime + 1f && !isBallPickedByFielder && minPickupDistance > 35f && !(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex))
            {
                isRunning = true;
                isComputerBatsmanAttemptingNewRun = false;
            }
        }
    }

    /// <summary>
    /// POST-SHOT full-state sync. Called on the BOWLING client when the BATTING client (the
    /// outcome authority after bat contact) streams its authoritative post-shot kinematic state
    /// at 20 Hz. We overwrite the bowling client's parametric integrator inputs so its per-frame
    /// BattingBallMovement()/BallMovement() continues the flight from the SAME state the batting
    /// client is showing — making the visible six / four / catch / out identical on both screens.
    ///
    /// A position-only lerp was NOT enough: BallMovement() re-derives matchBallTransform.position
    /// from temporaryPosition + launchAngle (+ arcHeight / _ballAngle / horizontalVelocity) every
    /// frame, later in the same Update(), so it would overwrite any lerp. We therefore slave ALL
    /// of those integrator inputs, not just the visible position.
    /// </summary>
    public void RPC_SyncBallShot(Vector3 ballPos, Vector3 tempPos, float ballAngle,
        float curLaunchAngle, float curArcHeight, float curAngleChangeRate, float curHVelocity,
        int curBounceCount, int curBoundaryType, double startNetTime, float dampFactor)
    {
        // Only the BOWLING follower mirrors the batting authority's post-shot flight.
        if (GameConstants.isWithAI != false) return;
        if (CONTROLLER.PlayModeSelected != 8) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return;
        if (!isBallReleased) return;                 // ball must have been released this delivery
        if (isReplayModeActive) return;              // never correct positions during replay
        if (LockstepActive) return;                  // lockstep: the stream is dead weight — local sim owns the flight

        _shotSyncRecvCount++;                        // [CamFollowDiag] post-shot stream packet received
        NoteDeliveryStreamProgress();                // G1 v2: positive liveness for the BOWLING staying client

        // ADOPT-THE-SHOT (spin "ball goes to keeper / bowled on bowling, four on batting" fix):
        // this post-shot stream is ONLY sent after a genuine bat contact on the batting authority, so its
        // arrival is itself proof a shot happened. On a spin delivery the bowling client's local sim often
        // resolves the ball (to the keeper, or even "bowled") BEFORE the bat-contact relay can apply, so
        // isBallHit never flipped, the old `!isBallHit` guard dropped this whole stream, and the wrong
        // local resolution played out while the batting screen showed the shot. Trust the stream: flip
        // into the shot here so the authoritative flight below drives the ball. The final score/wicket is
        // still owned by RpcBallOutcome, so adopting the shot only corrects the VISIBLE flight.
        bool adoptedShotThisCall = false;
        if (!isBallHit)
        {
            isBallHit = true;
            didBallHitBat = true;          // treat as a bat shot so the fielding logic engages
            currentBallStatus = "shotSuccess";
            // Drive the post-shot state machine: Action4Functions (currentActionState==4) is what runs
            // BattingBallMovement (smooth integrator flight, not just 20 Hz snaps), ActivateFielders (the
            // fielders actually chase) and the follow camera. Without this the bowling client stayed in a
            // pre-shot state, so the ball jumped between stream packets (frozen/jerky), the fielders ran
            // their animation on the spot, and the camera jerked tracking the stepped ball.
            if (currentActionState < 4) currentActionState = 4;
            _remoteShotAppliedThisDelivery = true;
            adoptedShotThisCall = true;
            // [DetDiag] Quantify the divergence at adopt: how far the bowling client's LOCAL ball had drifted
            // from the batting authority's at the moment of adopt. Small delta => flight matched, the miss is a
            // bat-CONTACT race (local sim sent the ball keeper-ward before the relay). Large delta => the
            // deterministic flight itself isn't matching. det=_useDeterministicBallStep tells if Phase1/2 armed.
            float _adoptDelta = Vector3.Distance(matchBallTransform.position, ballPos);
            ConstantsData_M.MpLog($"[GroundController][RPC_SyncBallShot] Adopted authoritative shot on bowling client (local contact never registered — spin divergence). [DetDiag] localPos={matchBallTransform.position} authPos={ballPos} delta={_adoptDelta:F2} det={_useDeterministicBallStep} bounce={bounceCount} ballAngle={_ballAngle:F1}");
        }

        // NOTE: a previous "spin-divergence smoothing" attempt here LERPED temporaryPosition toward the
        // authoritative sample to dampen the post-shot camera jerk. REVERTED 2026-06-17: on the bowling
        // client temporaryPosition is ALSO the position the keeper/fielder catch + ball-contact geometry
        // reads — easing it desynced that geometry so the keeper stopped catching and shots stopped
        // registering on the bowling side. This is the same minefield noted at the top of the file
        // (smoothing the ball position is fundamentally incompatible with the collision that reads it).
        //
        // Boundary-shot JERK root (03-07): this 20Hz stream HARD-snapped position + re-based the deterministic
        // clock on EVERY packet. Both sides integrate the bit-identical fixed-step path, so a packet's position
        // differs from ours only by TIME LAG (network latency + the follower's amortize backlog on hitchy
        // build frames) — it is AHEAD ALONG OUR OWN PATH, not off-path. Snapping to it every 50ms cashed that
        // lag out as a forward teleport (slow-then-burst at 20Hz = the boundary jerk), and the per-packet clock
        // re-base erased the amortize backlog so the smooth drain never ran. FIX: the FIRST packet of the
        // stream still seeds EVERYTHING (the authority's shot power/friction RNG must replace the local roll —
        // that's the June spin-desync fix, untouched); after that, skip the position snap + clock re-base while
        // the deviation is explained by lag, and let the local deterministic sim own the position. A GENUINE
        // divergence (deviation beyond what lag explains) still snaps exactly like before.
        bool _snapThisPacket = adoptedShotThisCall || !_shotStreamSeeded;
        if (!_snapThisPacket)
        {
            int _backlogTicks = Mathf.Max(0, BallSimTargetTicks() - _ballSimTicksDone);
            float _lagSec = _backlogTicks * BALL_SIM_FIXED_STEP
                          + Mathf.Max(0f, (float)(Mirror.NetworkTime.time - startNetTime));
            float _explained = _lagSec * Mathf.Max(horizontalVelocity, curHVelocity) + 1.0f;   // + margin (bounce-phase y offsets)
            float _dev = Vector3.Distance(matchBallTransform.position, ballPos);
            _snapThisPacket = _dev > _explained;
            if (_snapThisPacket)
                ConstantsData_M.MpLog($"[ShotStream] GENUINE divergence dev={_dev:F2} > explained={_explained:F2} (backlog={_backlogTicks}t hVel={horizontalVelocity:F1}) — snapping to authority.");
        }
        // Paused-follower divergence (09-07 logs: 300+ 'GENUINE divergence' snaps with hVel=0.0 and a frozen
        // backlog): a LOCAL speculative resolution (keeper catch attempt on a ball the batter actually hit,
        // etc.) had PAUSED our ball while the AUTHORITY kept it flying — the local sim froze, every packet
        // exceeded the dead-band, and the hard snap teleported the ball at 20Hz (the "full toss jerking" +
        // "ball keeper ke paas ruki vs boundary" split). The authority owns the flight: if it streams a LIVE
        // ball while ours is paused pre-outcome, unpause and adopt fully. Conversely once our outcome is
        // committed and BOTH balls are at rest, skip the snap — teleporting a resting ball is visual noise.
        if (isBallPaused && curHVelocity > 0.5f && !_outcomeCommittedThisDelivery)
        {
            isBallPaused = false;
            ShowBall(status: true);
            if (wicketKeeperBallObject != null)
            {
                Renderer _gloveBall = wicketKeeperBallObject.GetComponent<Renderer>();
                if (_gloveBall != null) _gloveBall.enabled = false;   // undo the speculative keeper-glove ball
            }
            _snapThisPacket = true;   // re-seed position + clock from this live packet
            ConstantsData_M.MpLog("[ShotStream] Local ball was PAUSED while the authority streams a live flight — unpaused + adopted.");
        }
        else if (curHVelocity <= 0.5f && horizontalVelocity <= 0.5f
                 && Vector3.Distance(matchBallTransform.position, ballPos) < 6f)
        {
            // Both balls at REST with a small offset: teleporting a resting ball is pure visual noise — this
            // was the "catch ke baad ball fielder ke paas jerk karti hai" (10-07 logs: 33 identical 1.3u snaps
            // at hVel=0 while the outcome relay was still in flight — the old gate also demanded the outcome
            // commit, which lags the rest by seconds). A big offset (>=6u) still snaps once to converge.
            _snapThisPacket = false;
        }
        // Frozen-follower fix (10-07 logs: dev=75u snaps with a 132-tick FROZEN backlog at hVel=0): a local
        // resolution advanced currentActionState out of 4 while the authority still streams a LIVE ball —
        // StepBallDeterministic only runs in state 4, so the local sim froze and every packet hard-snapped
        // (tester case 4: "bowler side ball fielder se pichhe chali gayi"). If the stream is live and our
        // outcome isn't committed, re-enter the flight state so the sim follows instead of teleporting.
        if (isBallHit && !_outcomeCommittedThisDelivery && curHVelocity > 0.5f && currentActionState != 4)
        {
            currentActionState = 4;
            ConstantsData_M.MpLog("[ShotStream] Re-entered flight state 4 — authority still streaming a live ball.");
        }
        if (_snapThisPacket)
        {
            matchBallTransform.position = ballPos;
            temporaryPosition           = tempPos;
            // #1 (behind-shot ball-clipping): the 20Hz post-shot stream can carry a STALE pre-contact angle (logs:
            // 81.8 over the real ~301 behind-shot). Overwriting _ballAngle with it re-bases the deterministic step
            // onto an off-pitch trajectory, so the ball freezes on the pitch instead of travelling behind the keeper.
            // Once RPC_ChangeBallAngle has relayed the authoritative bat-contact angle (the definitive shot
            // direction), KEEP it; otherwise adopt the stream value as before. Converges within ~1 packet either way.
            if (!_contactAngleRelayedThisDelivery)
                _ballAngle              = ballAngle;
            launchAngle                 = curLaunchAngle;
            arcHeight                   = curArcHeight;
            angleChangeRate             = curAngleChangeRate;
            horizontalVelocity          = curHVelocity;

            // Deterministic post-shot (Phase 1): re-base this follower's fixed-step integrator onto the
            // authority's just-snapped state at the authority's send instant (startNetTime). Runs on the seed
            // packet and on genuine-divergence snaps only — NOT per packet (see jerk note above).
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            {
                _ballSimStartNetTime      = startNetTime;
                // Clamp >= 0 (see RPC_SyncBallRelease): clock-sync skew can make NetworkTime.time < startNetTime,
                // truncating to a negative seed → a permanent step deficit. Seed the authority's 0 baseline.
                _ballSimTicksDone         = System.Math.Max(0, (int)((Mirror.NetworkTime.time - startNetTime) / BALL_SIM_FIXED_STEP));
                _ballSimMaxTargetTicks    = _ballSimTicksDone;   // Issue #3: re-seed high-water on re-base so the monotonic floor tracks the new origin
            }
            _shotStreamSeeded = true;
        }
        // Authority-owned constants/verdicts apply every packet (idempotent — no motion-state distortion).
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
        {
            velocityDampingFactor     = dampFactor;
            applyFrictionReduction    = true;   // adopt authority's friction; don't re-randomise locally
            _useDeterministicBallStep = true;
        }

        // Idempotent: only advance the bounce count, never rewind it.
        if (curBounceCount > bounceCount)
        {
            bounceCount = curBounceCount;
        }

        // Six/four authority: the follower's local launchAngle is overwritten above, so its own
        // bounce-detection path (launchAngle>=360 → boundaryType=4) never runs, leaving boundaryType
        // stuck at its default 6 → a bounced FOUR was being signalled as a SIX. Take the batting
        // authority's boundaryType directly. The authority computes it correctly for every case
        // (bounce-inside four AND keeper-beaten edge four), so mirroring it fixes the umpire's call.
        boundaryType = curBoundaryType;

        // Fielder "run-in-place" fix: adopting the shot above makes the ball fly on the bowling client,
        // but the fielding setup (which fielder chases, its chase target + the iTween movement) normally
        // runs only from the LOCAL bat-contact path or RPC_ChangeBallAngle's didBallHitBat branch —
        // neither fired here (the contact never registered locally), so the fielder played its run
        // animation on the spot with no chase target. Now that _ballAngle holds the authoritative shot
        // direction, recompute the fielder geometry and activate the chasers exactly like
        // RPC_ChangeBallAngle does, so the fielders actually MOVE to the ball.
        if (adoptedShotThisCall && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            GetFieldersAngle();
            GetFieldersDistance();
            if (!stopKeeper)
            {
                SetActiveFielders();
            }
        }
    }

    private void BatsmanWaiting()
    {
        if (!CONTROLLER.ReplayShowing)
        {
            Singleton<GameData>.instance.canPauseGameplay = true;
        }
        else
        {
            Singleton<GameData>.instance.canPauseGameplay = false;
        }
        if (Time.time > ballStartTime + batsmanWaitDuration)
        {
            batsmanAnim.CrossFade("WCCLite_BatsmanStanceReady");
            batsmanAnim.PlayQueued("WCCLite_BatsmanTapLoop", QueueMode.CompleteOthers);
            if (bowlerType == "fast")
            {
                keeperAnim.CrossFade("getReady", 0.5f);
            }
            else if (bowlerType == "medium")
            {
                keeperAnim.CrossFade("getReady", 0.5f);
            }
            else
            {
                keeperAnim.CrossFade("getReadyForSpin", 0.5f);
            }

            if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
            {
                currentActionState = 1;
            }
            else
            {
                currentActionState = 1;
            }
        }
    }

    private int SafeBattingBalls()
    {
        if (CONTROLLER.TeamList != null && CONTROLLER.BattingTeamIndex >= 0 && CONTROLLER.BattingTeamIndex < CONTROLLER.TeamList.Length)
            return CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls;
        return -1;
    }

    private void RunnerActions()
    {
        ////ConstantsData_M.MpLog("CASEE 3 RunnerActions");
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
        {
            Singleton<BattingControls>.instance.allBtns.SetActive(false);
        }
        if (isThirdUmpireRunoutReplaySkipped)
        {
            return;
        }
        if (nonStrikerCurrentStatus == "getReady" && (matchBallTransform.position.z > stumpLeftCrease.transform.position.z || currentBallStatus == "shotSuccess"))
        {
            strikerCurrentStatus = "backToCrease";
            nonStrikerCurrentStatus = "backToCrease";
            currentRunner.transform.eulerAngles = new Vector3(currentRunner.transform.eulerAngles.x, 0f, currentRunner.transform.eulerAngles.z);

            runnerAnim.Play("BackToCreaseNew");
        }
        if (nonStrikerCurrentStatus == "run" && canRun)
        {
            if (runsScoredThisBall % 2 == 0)
            {
                if (_batsmanTransform.position.z > 8.8f || _batsmanTransform.position.z < 0f)
                {
                    if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
                    {
                    }
                    Singleton<BattingControls>.instance.hideCancelBtn();
                }
                else if (!isRunCancelled && !Singleton<GameData>.instance.isGamePaused)
                {
                    showRunInterface(boolean: false);
                }
            }
            else if (_batsmanTransform.position.z < -8.8f || _batsmanTransform.position.z > 0f)
            {
                if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
                {
                }
                Singleton<BattingControls>.instance.hideCancelBtn();
            }
            else if (!isRunCancelled && !Singleton<GameData>.instance.isGamePaused)
            {
                showRunInterface(boolean: false);
            }
        }
        else if (nonStrikerCurrentStatus == "reachTheCrease")
        {
            if (!isRunCancelled && canRun)
            {
                showRunInterface(boolean: true);
                if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
                {
                    Singleton<BattingControls>.instance.allBtns.SetActive(false);
                }
            }
            if (isRunOutSaved)
            {
                disableRunCancelBtn();
            }
        }
        if (isReplayModeActive && takeRunTimings.Count > 0 && takeRunTimings.Count > runsScoredThisBall && (nonStrikerCurrentStatus == "backToCrease" || nonStrikerCurrentStatus == "comeToHalt") && saveRunRetryData[runsScoredThisBall, totalSaveAttempts] != 0f)
        {
            float num = saveRunRetryData[runsScoredThisBall, totalSaveAttempts];
            if (Time.time - ballConnectionTiming >= num)
            {
                ////ConstantsData_M.MpLog("!!@@##$$1 : " + takeRun);

                isRunning = true;
                if (saveRunRetryData.Length > totalSaveAttempts)
                {
                    totalSaveAttempts++;
                    isRunCancelled = false;
                }
            }
        }
        else if (isReplayModeActive && cancelRunData.Length > 0 && nonStrikerCurrentStatus == "run" && cancelRunData[runsScoredThisBall, totalCancelAttempts] != 0f)
        {
            float num2 = cancelRunData[runsScoredThisBall, totalCancelAttempts];
            if (Time.time - ballConnectionTiming >= num2)
            {
                if (!triggerRunCancel)
                {
                    cancelRunningBetweenWicket();
                    triggerRunCancel = true;
                }
                if (cancelRunData.Length > totalCancelAttempts)
                {
                    isRunCancelled = true;
                    triggerRunCancel = false;
                    totalCancelAttempts++;
                }
            }
        }
        if (canRun && isRunning && (nonStrikerCurrentStatus == "backToCrease" || nonStrikerCurrentStatus == "comeToHalt"))
        {
            ////ConstantsData_M.MpLog("USER CAN TAKE RUN");
            int num3 = ((CONTROLLER.BattingTeamIndex != CONTROLLER.opponentTeamIndex || CONTROLLER.PlayModeSelected == 8) ? 1 : 0);
            isRunning = false;
            ////ConstantsData_M.MpLog("!!@@##$$5 : " + takeRun);

            isRunBeingTaken = true;
            isRunOutOccurring = true;
            nonStrikerRunningSpeed = 6.5f * (1f + agilityMultiplier * (float)num3);
            strikerRunningSpeed = 6.5f * (1f + agilityMultiplier * (float)num3);
            strikerCurrentStatus = "run";
            nonStrikerCurrentStatus = "run";
            if (isReplayModeActive && takeRunTimings.Count - runsScoredThisBall == 1)
            {
                if (isRunOutSaved)
                {
                    nonStrikerRunningSpeed = 6.3f * (1f + agilityMultiplier * (float)num3);
                    strikerRunningSpeed = 6.3f * (1f + agilityMultiplier * (float)num3);
                }
                else
                {
                    nonStrikerRunningSpeed = 6.7f * (1f + agilityMultiplier * (float)num3);
                    strikerRunningSpeed = 6.7f * (1f + agilityMultiplier * (float)num3);
                }
            }
            if (!isReplayModeActive && !isRunCancelled)
            {
                takeRunTimings.Add(Time.time - ballConnectionTiming);
                ////ConstantsData_M.MpLog("RUNN TAKEN : " + (Time.time - ballConnectedTiming));
                saveRunRetryData[runsScoredThisBall, saveRunRetryCount] = Time.time - ballConnectionTiming;
                ////ConstantsData_M.MpLog("SAVE RUN AGAIN : " + saveRunAgainArray[currentBallNoOfRuns, saveRunAgainCount]);
                saveRunRetryCount++;
            }
            if (!isRunCancelled)
            {
                if (runsScoredThisBall % 2 == 0)
                {
                    strikerPlayer = batsmanObject;
                    nonStrikerPlayer = currentRunner;
                    strikerPlayer.transform.eulerAngles = new Vector3(strikerPlayer.transform.eulerAngles.x, 180f, strikerPlayer.transform.eulerAngles.z);
                    nonStrikerPlayer.transform.eulerAngles = new Vector3(nonStrikerPlayer.transform.eulerAngles.x, 0f, nonStrikerPlayer.transform.eulerAngles.z);
                    strikerRunAngle = AngleBetweenTwoGameObjects(strikerPlayer, rhbStrikerRunSpot);
                    if (bowlerSide == "left")
                    {
                        nonStrikerRunAngle = AngleBetweenTwoVector3(nonStrikerPlayer.transform.position, nonStrikerRunSpotForRunner.transform.position);
                    }
                    else if (bowlerSide == "right")
                    {
                        tempPoint1 = nonStrikerRunSpotForRunner.transform.position;
                        tempPoint1 = new Vector3(tempPoint1.x * -1f, tempPoint1.y, tempPoint1.z);
                        nonStrikerRunAngle = AngleBetweenTwoVector3(nonStrikerPlayer.transform.position, tempPoint1);
                    }
                }
                else
                {
                    strikerPlayer = currentRunner;
                    nonStrikerPlayer = batsmanObject;
                    strikerPlayer.transform.eulerAngles = new Vector3(strikerPlayer.transform.eulerAngles.x, 180f, strikerPlayer.transform.eulerAngles.z);
                    nonStrikerPlayer.transform.eulerAngles = new Vector3(nonStrikerPlayer.transform.eulerAngles.x, 0f, nonStrikerPlayer.transform.eulerAngles.z);
                    if (bowlerSide == "left")
                    {
                        strikerRunAngle = AngleBetweenTwoVector3(strikerPlayer.transform.position, strikerRunSpotForRunner.transform.position);
                    }
                    else if (bowlerSide == "right")
                    {
                        Vector3 position = strikerRunSpotForRunner.transform.position;
                        strikerRunAngle = AngleBetweenTwoVector3(v2: new Vector3(position.x * -1f, position.y, position.z), v1: strikerPlayer.transform.position);
                    }
                    nonStrikerRunAngle = AngleBetweenTwoGameObjects(nonStrikerPlayer, rhbNonStrikerRunSpot);
                }
            }
            strikerAnim = strikerPlayer.GetComponent<Animation>();
            nonStrikerAnim = nonStrikerPlayer.GetComponent<Animation>();
            strikerAnim.Play("run");
            nonStrikerAnim.Play("run");
            nonStrikerAnim["run"].speed = 1f;
            strikerAnim["run"].speed = 1f;
            if (runsScoredThisBall == 0)
            {
                if (!isReplayModeActive)
                {
                    showPreviewCamera(status: true);
                    cameraPreview.rect = new Rect(0.02f, 0.72f, 0.25f, 0.25f);
                }
                if (shouldMoveUmpire)
                {
                    //mainUmpireTransform.localScale = new Vector3(umpireRunDirection, mainUmpireTransform.localScale.x, mainUmpireTransform.localScale.z);
                    mainUmpireAnim.Play("UmpireRun_New");
                    mainUmpireAnim["UmpireRun_New"].speed = 2f;
                    shouldMoveUmpire = false;
                }
            }
        }
        else if (nonStrikerCurrentStatus == "run" || nonStrikerCurrentStatus == "reachTheCrease")
        {
            nonStrikerPlayer.transform.position += new Vector3(Mathf.Cos(nonStrikerRunAngle * degToRad) * nonStrikerRunningSpeed * (float)cancelRunDirection * Time.deltaTime, 0f, Mathf.Sin(nonStrikerRunAngle * degToRad) * nonStrikerRunningSpeed * (float)cancelRunDirection * Time.deltaTime);
            if (!isRunCancelled)
            {
                if (nonStrikerPlayer.transform.position.z > nonStrikerCreaseSpot.transform.position.z && (nonStrikerCurrentStatus == "run" || nonStrikerCurrentStatus == "reachTheCrease"))
                {
                    if (runsScoredThisBall >= 3)
                    {
                        canRun = false;
                        if (Singleton<GameData>.instance != null)
                        {
                            disableRunCancelBtn();
                        }
                    }
                    Singleton<GameData>.instance.ReachedCrease();
                    nonStrikerCurrentStatus = "reachTheCrease";
                    nonStrikerAnim.CrossFade("reachTheCrease");
                    nonStrikerAnim["reachTheCrease"].speed = 1f;
                }
            }
            else if (nonStrikerPlayer.transform.position.z < strikerCreaseSpot.transform.position.z && nonStrikerCurrentStatus == "run")
            {
                Singleton<GameData>.instance.ReachedCrease();
                nonStrikerCurrentStatus = "reachTheCrease";
                nonStrikerAnim.CrossFade("reachTheCrease");
                nonStrikerAnim["reachTheCrease"].speed = 1f;
            }
            float num4;
            if (isRunCancelled)
            {
                num4 = strikerTargetSpot.transform.position.z;
            }
            else
            {
                num4 = nonStrikerTargetSpot.transform.position.z;
                if (nonStrikerPlayer == batsmanObject)
                {
                    num4 = 7.28f;
                }
            }
            if (nonStrikerPlayer.transform.position.z < num4 && isRunCancelled)
            {
                isRunOutOccurring = false;
                if (hasRunOutOccurred || !isBallOnBoundaryLine)
                {
                }
                nonStrikerCurrentStatus = "comeToHalt";
            }
            else if (nonStrikerPlayer.transform.position.z > num4 && !isRunCancelled)
            {
                isRunOutOccurring = false;
                if (!hasRunOutOccurred && !isBallOnBoundaryLine)
                {
                    runsScoredThisBall++;
                    if ((CONTROLLER.currentInnings == 1 && CONTROLLER.PlayModeSelected != 7) || CONTROLLER.PlayModeSelected == 5 || (CONTROLLER.PlayModeSelected == 7 && CONTROLLER.currentInnings == 3))
                    {
                        targetScoreToWin--;
                        if (targetScoreToWin <= 0 && !isReplayModeActive)
                        {
                            canRun = false;
                            isRunning = false;
                            disableRunCancelBtn();
                        }
                    }
                    cancelRunTotalCount = 0;
                    saveRunRetryCount = 0;
                    totalSaveAttempts = 0;
                    totalCancelAttempts = 0;
                }
                nonStrikerCurrentStatus = "comeToHalt";
                if (BattingBy == "computer" && canRun && !isBallPickedByFielder && !isEdgeCaught && !isComputerBatsmanAttemptingNewRun)
                {
                    if (!canRun)
                    {
                        nonStrikerCurrentStatus = "comeToHalt";
                        isComputerBatsmanAttemptingNewRun = true;
                        return;
                    }
                    if (!(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex))
                    {
                        ////ConstantsData_M.MpLog("!!@@##$$ 8: " + takeRun);

                        isRunning = true;
                    }
                }
            }
        }
        else if (nonStrikerCurrentStatus == "comeToHalt")
        {
            nonStrikerPlayer.transform.position += new Vector3(Mathf.Cos(nonStrikerRunAngle * degToRad) * nonStrikerRunningSpeed * (float)cancelRunDirection * Time.deltaTime, 0f, Mathf.Sin(nonStrikerRunAngle * degToRad) * nonStrikerRunningSpeed * (float)cancelRunDirection * Time.deltaTime);
            nonStrikerRunningSpeed *= (100f - 70f * Time.deltaTime) / 100f;
            if (nonStrikerRunningSpeed < 3f && nonStrikerRunningSpeed != 0f)
            {
                isRunCancelled = false;
                cancelRunDirection = 1;
                nonStrikerRunningSpeed = 0f;
                nonStrikerAnim.CrossFade("idle");
            }
        }
        if (strikerCurrentStatus == "run" || strikerCurrentStatus == "reachTheCrease")
        {
            strikerPlayer.transform.position += new Vector3(Mathf.Cos(strikerRunAngle * degToRad) * strikerRunningSpeed * (float)cancelRunDirection * Time.deltaTime, 0f, Mathf.Sin(strikerRunAngle * degToRad) * strikerRunningSpeed * (float)cancelRunDirection * Time.deltaTime);
            if (!isRunCancelled)
            {
                if (strikerPlayer.transform.position.z < strikerCreaseSpot.transform.position.z && strikerCurrentStatus == "run")
                {
                    strikerCurrentStatus = "reachTheCrease";
                    strikerAnim.CrossFade("reachTheCrease");
                    strikerAnim["reachTheCrease"].speed = 1f;
                }
            }
            else if (strikerPlayer.transform.position.z > nonStrikerCreaseSpot.transform.position.z && strikerCurrentStatus == "run")
            {
                strikerCurrentStatus = "reachTheCrease";
                strikerAnim.CrossFade("reachTheCrease");
                strikerAnim["reachTheCrease"].speed = 1f;
            }
            if (strikerPlayer.transform.position.z > nonStrikerTargetSpot.transform.position.z && isRunCancelled)
            {
                strikerCurrentStatus = "comeToHalt";
            }
            else if (strikerPlayer.transform.position.z < strikerTargetSpot.transform.position.z && !isRunCancelled)
            {
                strikerCurrentStatus = "comeToHalt";
            }
        }
        else
        {
            if (!(strikerCurrentStatus == "comeToHalt"))
            {
                return;
            }
            strikerPlayer.transform.position += new Vector3(Mathf.Cos(strikerRunAngle * degToRad) * strikerRunningSpeed * (float)cancelRunDirection * Time.deltaTime, 0f, Mathf.Sin(strikerRunAngle * degToRad) * strikerRunningSpeed * (float)cancelRunDirection * Time.deltaTime);
            strikerRunningSpeed *= (100f - 70f * Time.deltaTime) / 100f;
            if (strikerRunningSpeed < 3f && strikerRunningSpeed != 0f)
            {
                strikerRunningSpeed = 0f;
                if (canRun && !isBallOnBoundaryLine && DistanceBetweenTwoGameObjects(matchBall, throwTarget) > 3f)
                {
                    showRunInterface(boolean: true);
                }
                strikerAnim.CrossFade("idle");
            }
        }
    }

    private void cancelRunningBetweenWicket()
    {
        disableRunCancelBtn();
        //if(CONTROLLER.PlayModeSelected == 8 && !takeRun)
        //{
        //    return;
        //}
        if (!isReplayModeActive)
        {
            cancelRunData[runsScoredThisBall, cancelRunTotalCount] = Time.time - ballConnectionTiming;
            cancelRunTotalCount++;
        }
        if (runsScoredThisBall % 2 == 0)
        {
            strikerPlayer.transform.eulerAngles = new Vector3(strikerPlayer.transform.eulerAngles.x, 0f, strikerPlayer.transform.eulerAngles.z);
            nonStrikerPlayer.transform.eulerAngles = new Vector3(nonStrikerPlayer.transform.eulerAngles.x, 180f, nonStrikerPlayer.transform.eulerAngles.z);
            cancelRunDirection = -1;
            strikerRunAngle = AngleBetweenTwoGameObjects(strikerPlayer, rhbStrikerRunSpot);
            if (bowlerSide == "left")
            {
                nonStrikerRunAngle = AngleBetweenTwoVector3(nonStrikerPlayer.transform.position, nonStrikerRunSpotForRunner.transform.position);
            }
            else if (bowlerSide == "right")
            {
                tempPoint1 = nonStrikerRunSpotForRunner.transform.position;
                tempPoint1 = new Vector3(tempPoint1.x * -1f, tempPoint1.y, tempPoint1.z);
                nonStrikerRunAngle = AngleBetweenTwoVector3(nonStrikerPlayer.transform.position, tempPoint1);
            }
        }
        else
        {
            strikerPlayer.transform.eulerAngles = new Vector3(strikerPlayer.transform.eulerAngles.x, 0f, strikerPlayer.transform.eulerAngles.z);
            nonStrikerPlayer.transform.eulerAngles = new Vector3(nonStrikerPlayer.transform.eulerAngles.x, 180f, nonStrikerPlayer.transform.eulerAngles.z);
            cancelRunDirection = -1;
            if (bowlerSide == "left")
            {
                strikerRunAngle = AngleBetweenTwoVector3(strikerPlayer.transform.position, strikerRunSpotForRunner.transform.position);
            }
            else if (bowlerSide == "right")
            {
                tempPoint2 = strikerRunSpotForRunner.transform.position;
                tempPoint2 = new Vector3(tempPoint2.x * -1f, tempPoint2.y, tempPoint2.z);
                strikerRunAngle = AngleBetweenTwoVector3(strikerPlayer.transform.position, tempPoint2);
            }
            nonStrikerRunAngle = AngleBetweenTwoGameObjects(nonStrikerPlayer, rhbNonStrikerRunSpot);
        }
    }

    private void showRunInterface(bool boolean)
    {
        if (BattingBy != "computer")
        {
            Singleton<BattingControls>.instance.Hide(boolean: false);
        }
        if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
        {

            if (boolean)
            {
                Singleton<GameData>.instance.EnableRun(boolean);
                Singleton<GameData>.instance.EnableCancelRun(!boolean);
            }
            else
            {
                Singleton<GameData>.instance.EnableCancelRun(!boolean);
                Singleton<GameData>.instance.EnableRun(boolean);
            }
        }
    }

    private void disableRunCancelBtn()
    {
        Singleton<GameData>.instance.EnableRun(boolean: false);
        Singleton<GameData>.instance.EnableCancelRun(boolean: false);
    }

    public void touchCancelRun()
    {
        if (nonStrikerCurrentStatus == "run")
        {
            if (runsScoredThisBall % 2 == 0)
            {
                if (_batsmanTransform.position.z > 8.8f || _batsmanTransform.position.z < 0f)
                {
                    return;
                }
            }
            else if (_batsmanTransform.position.z < -8.8f || _batsmanTransform.position.z > 0f)
            {
                return;
            }
        }
        if (nonStrikerCurrentStatus == "run" && !isRunCancelled && !isBallOnBoundaryLine)
        {
            if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
            {
                isRunCancelled = true;
                cancelRunningBetweenWicket();

                if (GameConstants.isWithAI == false)
                {
                    //    view.RPC("RPC_touchCancelRun", RpcTarget.OthersBuffered, (Time.time - ballConnectionTiming));                                            //Photon Removal
                    CricketNetworkManager.instance.CmdTouchCancelRun(staticVariables.UserProfiledata.user._id, (Time.time - ballConnectionTiming));  //Photon Addition
                }
            }
            else
            {
                isRunCancelled = true;
                cancelRunningBetweenWicket();
            }
        }
    }

    //Photon Removal  [PunRPC]
    public void RPC_touchCancelRun(float CANCELRUNTIME)
    {
        hasCancelledRun = true;
        cancelRunTimestamp = CANCELRUNTIME;
    }

    public void batsmanCelebration(string celebration)
    {
        showPreviewCamera(status: false);
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        umpireViewCamera.enabled = false;
        introCutsceneCamera.enabled = false;
        replayViewCamera.enabled = false;
        gameplayCamera.enabled = false;
        slowMotionCamera.enabled = true;
        ultraMotionCameraTransform.position = new Vector3(ultraMotionCameraTransform.position.x, 1f, _batsmanTransform.position.z);
        if (batsmanHand == "right")
        {
            ultraMotionCameraTransform.position = new Vector3(-9f, ultraMotionCameraTransform.position.y, ultraMotionCameraTransform.position.z);
            _batsmanTransform.eulerAngles = new Vector3(_batsmanTransform.eulerAngles.x, 270f, _batsmanTransform.eulerAngles.z);
        }
        else
        {
            ultraMotionCameraTransform.position = new Vector3(12f, ultraMotionCameraTransform.position.y, ultraMotionCameraTransform.position.z);
            _batsmanTransform.eulerAngles = new Vector3(_batsmanTransform.eulerAngles.x, 90f, _batsmanTransform.eulerAngles.z);
        }
        if (Singleton<GameData>.instance.gameState == 2)
        {
            ultraMotionCameraTransform.position = new Vector3(ultraMotionCameraTransform.position.x, 2.5f, ultraMotionCameraTransform.position.z);
        }
        loadCelebrationBatsman(celebration);
    }

    private void loadCelebrationBatsman(string celebration)
    {
        mainUmpireAnim.Play("Idle");
        sideUmpireAnim.Play("Idle");
        if (celebration == "halfcentury")
        {
            batsmanAnim.Play("FiftyCelebration");
        }
        else if (celebration == "century")
        {
            batsmanAnim.Play("HundredCelebration");
        }
        currentActionState = -11;
    }

    public void destroyCelebrationBatsman()
    {
        mainCamTransform.position = new Vector3(-30f, 6.8f, 0f);
        mainCamTransform.eulerAngles = new Vector3(10f, 90f, 0f);
        gameplayCamera.fieldOfView = 45f;
        gameplayCamera.enabled = true;
        slowMotionCamera.enabled = false;
        batsmanSkinRenderer.enabled = true;
        BatsmanCricketKitSkinRenderer.enabled = true;
        BatsmanBatSkinRenderer.enabled = true;
    }

    public void MultiplayerHit()
    {
        HideBowlingSpot();
        ShowFullTossSpot(_Value: false);
        if (CONTROLLER.BALLSTATUS == "Hit")
        {
            if (!isReplayModeActive)
            {
                summarySaved = "connected";
                ballConnectionPositionSaved = matchBallTransform.position;
                ballSpinSpeedZ = UnityEngine.Random.Range(-3600, -1800);
                ballConnectedSpinSpeedZSaved = ballSpinSpeedZ;
            }
            else if (isReplayModeActive)
            {
                matchBallTransform.position = ballConnectionPositionSaved;
                ballSpinSpeedZ = ballConnectedSpinSpeedZSaved;
                temporaryPosition = matchBallTransform.position;
            }

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
                if (Singleton<GameData>.instance != null)
                {
                    Singleton<GameData>.instance.EnableRun(boolean: true);
                }
            }
            SetCurrentBatsmanAnimSpeed(1f);
            BallTiming();
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
            if (!stopKeeper)
            {
                SetActiveFielders();
            }
            if (_ballAngle >= 90f && _ballAngle <= 270f)
            {
                umpireRunSignalDirection = -1;
            }
            else
            {
                umpireRunSignalDirection = 1;
            }
            didBallHitBat = true;
            currentActionState = 4;
            CONTROLLER.BALLSTATUS = string.Empty;
        }
    }

    public void initBatsmanExit()
    {
        closeUpViewCamera.enabled = false;
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        showPreviewCamera(status: false);
        umpireViewCamera.enabled = false;
        replayViewCamera.enabled = false;
        gameplayCamera.enabled = false;
        introCutsceneCamera.enabled = true;
        batsmanAnim.Play("WCCLite_BatsmanIdle");
        runnerAnim.Play("WCCLite_RunnerIdle");
    }

    // Set on the online BATTING client the moment it arms its own swing (ShotSelected), cleared when its own
    // echo comes back (or by the per-delivery reset in GroundController.ResetAll if the echo is lost).
    private bool _shotSelectionArmedLocally;

    //Photon Removal [PunRPC]
    public void RPC_ShotSelected(bool IsPower, int SelectedAngle, float Num3)
    {
        // ONLINE TIMING METER ("bar hamesha late"): the batting client now arms its own swing the instant the
        // player swipes (see ShotSelected below), so its OWN echo coming back off the relay must not re-apply.
        // Re-running this body a round-trip later is what broke the meter: it cleared
        // needsBattingTimingMeterNeedleUpdate at echo time, i.e. ~150-300ms of ball travel after the tap, so the
        // needle froze on a ball that was already past the bat (battingTimingMeterValue = ballZ/5*100, clamped
        // at +100) and every delivery read late no matter when the player tapped. The FOLLOWER (bowling side)
        // still applies the echo exactly as before — that is how it mirrors the batter's stroke.
        if (_shotSelectionArmedLocally)
        {
            _shotSelectionArmedLocally = false;
            return;
        }
        BallAngle(Num3);
        Singleton<GameData>.instance.userInputAction = -1;
        ApplyShotSelectionLocally(IsPower, SelectedAngle, Num3);
    }

    // Batter-side arm of a chosen shot: input flags + the timing-meter freeze/reveal. Shared by the offline
    // path, the online batting client (immediately, at swipe time) and the follower's echo of the batter's shot.
    private void ApplyShotSelectionLocally(bool IsPower, int SelectedAngle, float Num3)
    {
        isPowerKeyPressed = IsPower;
        isTouchDeviceShotInputEnabled = true;
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            needsBattingTimingMeterNeedleUpdate = false;
            if (CONTROLLER.PlayModeSelected != 6)
            {
                Singleton<BattingControls>.instance.battingMeter.SetActive(value: true);
                Singleton<BattingControls>.instance.GreedyAdsImage.SetActive(value: false);
            }
        }
        isLeftArrowKeyPressed = false;
        isDownArrowKeyPressed = false;
        isUpArrowKeyPressed = false;
        isRightArrowKeyPressed = false;
        switch (SelectedAngle)
        {
            case 1:
                isDownArrowKeyPressed = true;
                break;
            case 2:
                isLeftArrowKeyPressed = true;
                isDownArrowKeyPressed = true;
                break;
            case 3:
                isLeftArrowKeyPressed = true;
                break;
            case 4:
                isUpArrowKeyPressed = true;
                isLeftArrowKeyPressed = true;
                break;
            case 6:
                isUpArrowKeyPressed = true;
                isRightArrowKeyPressed = true;
                break;
            case 7:
                isRightArrowKeyPressed = true;
                break;
            case 8:
                isRightArrowKeyPressed = true;
                isDownArrowKeyPressed = true;
                break;
        }
        if (batsmanHand == "left")
        {
            if (isLeftArrowKeyPressed)
            {
                isLeftArrowKeyPressed = false;
                isRightArrowKeyPressed = true;
            }
            else if (isRightArrowKeyPressed)
            {
                isLeftArrowKeyPressed = true;
                isRightArrowKeyPressed = false;
            }
        }
    }

    public void ShotSelected(bool isPower, int SelectedAngle, float Num3)
    {
        // ONLINE TIMING METER ("bar hamesha late") + late swings: this used to send CmdShotSelected and apply
        // NOTHING locally, so the batting client armed its own swing only when the echo came back off the relay
        // (RPC_ShotSelected). Everything that hangs off that arm was therefore a full round-trip late:
        //   * needsBattingTimingMeterNeedleUpdate was cleared ~150-300ms after the tap, so UpdateBattingTimingMeter
        //     kept walking the needle with the ball in between and froze it on a ball already past the bat
        //     (battingTimingMeterValue = ballZ/5*100, clamps at +100) -> every delivery read late;
        //   * isTouchDeviceShotInputEnabled gates the swing arm in GetBattingInput, so ExecuteTheShot only fired at
        //     tap+RTT. The [ShotTiming] diag shows it: tSinceRelease 0.400 vs activation 0.212.
        // The batter now arms locally at swipe time (same body the offline path has always used) and the Cmd is
        // only the relay to the opponent; _shotSelectionArmedLocally makes our own echo a no-op in RPC_ShotSelected.
        // Mode-8-vs-AI is deliberately left exactly as it was.
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            if (GameConstants.isWithAI == false)
            {
                //    view.RPC("RPC_ShotSelected", RpcTarget.AllBuffered, isPower, SelectedAngle, Num3);                                                                          //Photon Removal
                _shotSelectionArmedLocally = true;
                // Ghost-session guard (same reason as CmdShotTiming): after a teardown the spawned
                // CricketNetworkManager is destroyed while the Ground scene keeps running and Mirror's
                // SendCommandInternal throws. The local arm above/below still runs, so the batter keeps playing.
                if (CricketNetworkManager.ReadyToSend)
                {
                    CricketNetworkManager.instance.CmdShotSelected(isPower, SelectedAngle, Num3);
                }
                else if (!_shotSelectedBlockedLogged)
                {
                    _shotSelectedBlockedLogged = true;
                    ConstantsData_M.MpLog("[GroundController][ShotSelected] Relay BLOCKED - network manager not ready (ghost session). Local swing still armed.");
                }
                ApplyShotSelectionLocally(isPower, SelectedAngle, Num3);
            }
        }
        else
        {
            ApplyShotSelectionLocally(isPower, SelectedAngle, Num3);
        }
    }

    private bool _shotSelectedBlockedLogged;   // one-shot for the ghost-session guard above

    public void InitRun(bool boolean)
    {
        if (CONTROLLER.PlayModeSelected == 8)
        {
            float temp = Time.time - ballConnectionTiming;
            //ConstantsData_M.MpLog("!!@@##$$ : " + takeRun);

            isRunning = boolean;
            ////ConstantsData_M.MpLog("DID TOOK RUN : " + boolean);

            if (boolean == true)
            {
                if (GameConstants.isWithAI == false)
                {
                    // view.RPC("RPC_InitRun", RpcTarget.OthersBuffered, temp, boolean);
                    CricketNetworkManager.instance.CmdInitRun(staticVariables.UserProfiledata.user._id, temp, boolean);
                }


            }
            else                                                                                                                                                                                                         //Photon Removal
            {
                if (GameConstants.isWithAI == false)
                {
                    //   view.RPC("RPC_InitRun", RpcTarget.OthersBuffered, temp, boolean);
                    CricketNetworkManager.instance.CmdInitRun(staticVariables.UserProfiledata.user._id, temp, boolean);
                }


            }
        }
        else
        {
            isRunning = boolean;
        }
    }

    //Photon Removal  [PunRPC]
    public void RPC_InitRun(float WhenTakeRun, bool TakeRun)
    {

        if (TakeRun == true)
        {
            takeRunTimestamp = WhenTakeRun;
            hasTakenRun = TakeRun;
            StartCoroutine(MyCoroutine(4f));
        }
        else
        {
        }
    }

    public void MultiplayerTakeRun(float TakeRun)
    {
    }

    public void InItRunNot()
    {
        isRunning = false;
    }

    private void DecreaseConfidenceLevel(string confidenceStatus)
    {
        float confidenceVal = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal;
        float confidenceDec = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceDec;
        confidenceDec = ((CONTROLLER.totalOvers >= 10) ? (confidenceDec * (10f / (float)CONTROLLER.totalOvers)) : (confidenceDec * (2f / (float)CONTROLLER.totalOvers)));
        confidenceVal = ((!(confidenceVal - confidenceDec > 0f)) ? 0f : (confidenceVal - confidenceDec));
        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal = confidenceVal;
    }

    private float GetBatsmanReturnFrameTimeToCrease()
    {
        float num = 0f;
        int num2 = 0;
        if (currentBatsmanAnimation == "bt6CoverDrive")
        {
            num2 = 71;
        }
        else if (currentBatsmanAnimation == "bt6LegGlance")
        {
            num2 = 61;
        }
        else if (currentBatsmanAnimation == "bt6OffDrive")
        {
            num2 = 61;
        }
        else if (currentBatsmanAnimation == "bt6OnDrive")
        {
            num2 = 91;
        }
        else if (currentBatsmanAnimation == "bt6StraightDrive")
        {
            num2 = 65;
        }
        else if (currentBatsmanAnimation == "loftLegSide")
        {
            num2 = 71;
        }
        else if (currentBatsmanAnimation == "loftOffSide")
        {
            num2 = 111;
        }
        else if (currentBatsmanAnimation == "loftStraight")
        {
            num2 = 115;
        }
        else if (currentBatsmanAnimation == "frontFootOffDrive")
        {
            num2 = 61;
        }
        return (float)num2 * animationFrameInterval;
    }

    public void Greedy_clickable_ad()
    {
    }

    private void CheckShotRegion()
    {
        ScanUserFielderListRefined.Clear();
        int num = 0;
        float x = creaseImpactSpot.transform.position.x;
        for (int i = 0; i < _scanForUserFielders.Count; i++)
        {
        }
        foreach (float userFielderScan in _scanForUserFielders)
        {
            float num2 = userFielderScan;
            if (x < 0f)
            {
                if (num == 0)
                {
                    ScanUserFielderListRefined.Add(110f);
                }
                if (!(num2 < 110f) && !(num2 > 270f))
                {
                    ScanUserFielderListRefined.Add(num2);
                }
                if (num == _scanForUserFielders.Count - 1)
                {
                    ScanUserFielderListRefined.Add(270f);
                }
            }
            else if (x >= 0f)
            {
                if (num == 0)
                {
                    ScanUserFielderListRefined.Add(270f);
                }
                if (!(num2 > 70f) || !(num2 <= 270f))
                {
                    if (num2 <= 70f)
                    {
                        ScanUserFielderListRefined.Add(num2 + 360f);
                    }
                    else
                    {
                        ScanUserFielderListRefined.Add(num2);
                    }
                }
                if (num == _scanForUserFielders.Count - 1)
                {
                    ScanUserFielderListRefined.Add(430f);
                }
            }
            num++;
        }
        ScanUserFielderListRefined.Sort();
        for (int j = 0; j < ScanUserFielderListRefined.Count; j++)
        {
        }
    }

    private void MakeAIHitInGap()
    {
        CheckShotRegion();
        int num = UnityEngine.Random.Range(0, ScanUserFielderListRefined.Count - 1);
        aiBallAngle = (ScanUserFielderListRefined[num] + ScanUserFielderListRefined[num + 1]) / 2f;
        if (aiBallAngle > 360f)
        {
            aiBallAngle %= 360f;
        }
        if (CONTROLLER.StrikerHand == "left")
        {
            aiBallAngle = 180f - aiBallAngle + 360f;
            aiBallAngle %= 360f;
        }
    }

    public void DetermineAIShot()
    {
        int selectedAngle = 0;
        float aIBallAngle = aiBallAngle;
        if (aIBallAngle == 0f)
        {
            selectedAngle = 5;
        }
        else if ((aIBallAngle < 22.5f && aIBallAngle >= -22.5f) || (aIBallAngle < 360f && aIBallAngle >= 337.5f))
        {
            selectedAngle = 7;
        }
        else if (aIBallAngle < 67.5f && aIBallAngle >= 22.5f)
        {
            selectedAngle = 6;
        }
        else if (aIBallAngle < 112.5f && aIBallAngle >= 67.5f)
        {
            selectedAngle = 5;
        }
        else if (aIBallAngle < 157.5f && aIBallAngle >= 112.5f)
        {
            selectedAngle = 4;
        }
        else if (aIBallAngle < 202.5f && aIBallAngle >= 157.5f)
        {
            selectedAngle = 3;
        }
        else if (aIBallAngle < 247.5f && aIBallAngle >= 202.5f)
        {
            selectedAngle = 2;
        }
        else if (aIBallAngle < 292.5f && aIBallAngle >= 247.5f)
        {
            selectedAngle = 1;
        }
        else if (aIBallAngle < 337.5f && aIBallAngle >= 292.5f)
        {
            selectedAngle = 8;
        }
        AIShotSelected(selectedAngle);
    }

    public void AIShotSelected(int SelectedAngle)
    {
        isLeftArrowKeyPressed = false;
        isDownArrowKeyPressed = false;
        isUpArrowKeyPressed = false;
        isRightArrowKeyPressed = false;
        switch (SelectedAngle)
        {
            case 1:
                isDownArrowKeyPressed = true;
                break;
            case 2:
                isLeftArrowKeyPressed = true;
                isDownArrowKeyPressed = true;
                break;
            case 3:
                isLeftArrowKeyPressed = true;
                break;
            case 4:
                isUpArrowKeyPressed = true;
                isLeftArrowKeyPressed = true;
                break;
            case 6:
                isUpArrowKeyPressed = true;
                isRightArrowKeyPressed = true;
                break;
            case 7:
                isRightArrowKeyPressed = true;
                break;
            case 8:
                isRightArrowKeyPressed = true;
                isDownArrowKeyPressed = true;
                break;
        }
    }

    private void UpdateBattingTimingMeter()
    {
        // The meter reports the SHOT, so it must stop the moment the shot is played.
        //
        // This runs from BowlingBallMovement, i.e. every frame the ball is in flight, and
        // needsBattingTimingMeterNeedleUpdate is set at release and only ever cleared on the offline/AI
        // path (the clear at the shot-execution site is gated on PlayModeSelected != 8). So online it kept
        // recomputing after contact, and battingTimingMeterValue is just ballZ * 20 — as the struck ball
        // travels on past the crease that value climbs, clamps at +100, and the LAST label written is the
        // very-late one. Hence "late shot, early shot wali bar hamesha late hi show hoti hai": the needle
        // was following the ball, not reporting the stroke.
        //
        // Freezing at contact also holds isPerfectShot / isMistimedShot / horizontalSpeedAdjustment /
        // firstBounceSpeedMultiplier at their contact-time values, which is what they are meant to describe;
        // until now they were being overwritten every frame from the ball's post-contact position, and
        // CmdShotTiming was relaying that to the opponent on every frame of the flight as well.
        if (needsBattingTimingMeterNeedleUpdate && !isReplayModeActive && !batsmanCompletedShot)
        {
            //battingTimingMeter = ballTransform.position.z / 5f * 100f;
            battingTimingMeterValue = temporaryPosition.z / 5f * 100f;
            if (battingTimingMeterValue < -100f)
            {
                battingTimingMeterValue = -100f;
            }
            else if (battingTimingMeterValue > 100f)
            {
                battingTimingMeterValue = 100f;
            }
            int num = 8;
            if (CONTROLLER.PowerPlay)
            {
                num = 6;
            }
            if (Mathf.Abs(battingTimingMeterValue) <= (float)Singleton<BattingControls>.instance.btmPerfectValue)
            {
                isPerfectShot = true;
                horizontalSpeedAdjustment = 1.2f;
                isMistimedShot = false;
                Singleton<BattingControls>.instance.battingTimingNeedleText.text = LocalizationData.localizationInstance.getText(539);
            }
            else if (battingTimingMeterValue < -65f)
            {
                firstBounceSpeedMultiplier = 0.7f;
                horizontalSpeedAdjustment = 0.9f;
                isPerfectShot = false;
                isMistimedShot = true;
                Singleton<BattingControls>.instance.battingTimingNeedleText.text = LocalizationData.localizationInstance.getText(540);
            }
            else if (battingTimingMeterValue < -30f)
            {
                firstBounceSpeedMultiplier = 0.85f;
                horizontalSpeedAdjustment = 0.85f;
                isPerfectShot = false;
                isMistimedShot = true;
                Singleton<BattingControls>.instance.battingTimingNeedleText.text = LocalizationData.localizationInstance.getText(541);
            }
            else if (battingTimingMeterValue < (float)(-Singleton<BattingControls>.instance.btmPerfectValue))
            {
                firstBounceSpeedMultiplier = 0.95f;
                horizontalSpeedAdjustment = 0.92f;
                isPerfectShot = false;
                isMistimedShot = false;
                Singleton<BattingControls>.instance.battingTimingNeedleText.text = LocalizationData.localizationInstance.getText(541) + " - " + LocalizationData.localizationInstance.getText(542);
            }
            else if (battingTimingMeterValue > 65f)
            {
                firstBounceSpeedMultiplier = 0.7f;
                horizontalSpeedAdjustment = 0.85f;
                isPerfectShot = false;
                isMistimedShot = true;
                Singleton<BattingControls>.instance.battingTimingNeedleText.text = LocalizationData.localizationInstance.getText(543);
            }
            else if (battingTimingMeterValue > 30f)
            {
                firstBounceSpeedMultiplier = 0.85f;
                horizontalSpeedAdjustment = 0.85f;
                isPerfectShot = false;
                isMistimedShot = true;
                Singleton<BattingControls>.instance.battingTimingNeedleText.text = LocalizationData.localizationInstance.getText(544);
            }
            else
            {
                firstBounceSpeedMultiplier = 1f;
                isPerfectShot = false;
                isMistimedShot = true;
                Singleton<BattingControls>.instance.battingTimingNeedleText.text = LocalizationData.localizationInstance.getText(545);
            }
            Singleton<BattingControls>.instance.battingMeterPointer.transform.localPosition = new Vector3(battingTimingMeterValue, Singleton<BattingControls>.instance.battingMeterPointer.transform.localPosition.y);
            if (CONTROLLER.PlayModeSelected == 8)
            {
                if (GameConstants.isWithAI == false)
                {                                                                                                                                                                                                                                      //Photon Removal
                                                                                                                                                                                                                                                       //    view.RPC("RPC_ShotTiming", RpcTarget.AllBuffered, firstBounceSpeedMultiplier, horizontalSpeedAdjustment, isPerfectShot, isMistimedShot);
                    // This runs on the per-frame delivery path (Update -> Action3Functions ->
                    // StepDeliveryDeterministic -> BowlingBallMovement). In a ghost session the woven Cmd stub
                    // throws inside Mirror's SendCommandInternal, which aborted Update EVERY frame — the ball
                    // never advanced and the match froze (29-07 "stucking"/"bowler stuck", 88 identical NREs in
                    // one log). The local timing values above are already assigned, so skipping the send loses
                    // nothing: there is no live opponent to mirror them to.
                    if (CricketNetworkManager.ReadyToSend)
                        CricketNetworkManager.instance.CmdShotTiming(firstBounceSpeedMultiplier, horizontalSpeedAdjustment, isPerfectShot, isMistimedShot);
                    else if (!_shotTimingBlockedLogged)
                    {
                        _shotTimingBlockedLogged = true;
                        ConstantsData_M.MpLog("[GroundController][ShotTiming] Send BLOCKED - network manager not ready (ghost session). Local timing values stay applied; per-frame delivery sim continues.");
                    }
                }


            }
        }
    }

    //Photon Removal    [PunRPC]
    private bool _shotTimingBlockedLogged;   // one-shot for the ghost-session guard above

    public void RPC_ShotTiming(float FirstBounceMultiplier, float HorizontalSpeedMultiplier, bool PerfectShot, bool MistimedShot)
    {
        firstBounceSpeedMultiplier = FirstBounceMultiplier;
        horizontalSpeedAdjustment = HorizontalSpeedMultiplier;
        isPerfectShot = PerfectShot;
        isMistimedShot = MistimedShot;
    }

    private void BattingMeterCheck()
    {
        if (CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
        {
            return;
        }
        if (isPerfectShot)
        {
            if (isPowerShotActive)
            {
                firstBounceDistance = 70 + UnityEngine.Random.Range(5, 15);
            }
            else
            {
                horizontalVelocity *= 1.25f;
            }
        }
        else if (isPowerShotActive)
        {
            firstBounceDistance *= firstBounceSpeedMultiplier;
        }
        else
        {
            horizontalVelocity *= horizontalSpeedAdjustment;
        }
    }

    private float GetBattingAbility()
    {
        int num = int.Parse(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].rank);
        float num2 = 70f;
        if (CONTROLLER.StrikerIndex > 6)
        {
            num2 -= 15f;
        }
        else if (CONTROLLER.StrikerIndex > 3)
        {
            num2 -= 10f;
        }
        return num2 - (float)num;
    }

}
