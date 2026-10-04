// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.ReviewReplay — DRS / third-umpire / replay flows + edge decisions.
// CanProduceEdge: under lockstep this is a SEEDED 11% roll (both clients identical); offline keeps
// the legacy prefs-based chance. Online LBW review: outcome commits at the APPEAL MARK
// (mpLbwOutcomeCommitted) so the over can never hang on review UI; the panel is cosmetic online.
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
    public void SkipThirdUmpireRunoutReplay()
    {
        SkipReplay();
        currentActionState = 4;
        horizontalVelocity = 0f;
        fielderAction = "runOutAppeal";
        hasRunOutOccurred = isRunOutSaved;
        runsScoredThisBall = currentBallRunsSaved;
        isThirdUmpireRunoutReplaySkipped = true;
        ShowThirdUmpireRunoutDecisionPendingScreen();
    }

    private void ShowThirdUmpireRunoutDecisionPendingScreen()
    {
        Time.timeScale = 1f;
        Singleton<GameData>.instance.GameIsNotOnStumpingReplay();
        replayViewCamera.enabled = false;
        introCutsceneCamera.enabled = true;
        introCamTransform.position = new Vector3(-76f, 17f, -3.8f);
        introCamTransform.eulerAngles = new Vector3(0f, -90f, 0f);
        DigitalScreenRenderer.material.mainTexture = digitalBoardTextures[1];
        scoreboardScreen.transform.localScale = scoreboardScreenScale;
        _stayStartTime = Time.time;
        fielderAction = "waitFor3rdUmpireResultForRunout";
    }

    private void UpdateThirdUmpireRunoutResult()
    {
        if (hasRunOutOccurred)
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
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 4, CONTROLLER.CurrentBowlerIndex, 1, batsmanOutIndexValue, isBoundary: false);
            }
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
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, currentBallRunsSaved, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            }
        }
    }

    public void SkipStumpingReplay()
    {
        SkipReplay();
        currentActionState = 3;
        horizontalVelocity = 0f;
        isWicketKeeperActive = true;
        isPlayerStumped = previousStumpedState;
        ShowThirdUmpireDecisionBoard();
    }

    public void ShowThirdUmpireDecisionBoard(bool fromAuthoritativeRelay = false)
    {
        // ONLINE LBW guard (the REAL "3rd UMPIRE DECISION PENDING" stuck fix): an onPads LBW ball that
        // deflects to the keeper spuriously enters this stumping third-umpire board. But the LBW outcome was
        // already committed at the appeal mark (mpLbwOutcomeCommitted) and the over advanced the bowler to the
        // ready state (currentActionState=-2) — which does NOT run the keeper state machine. So
        // "waitFor3rdUmpireResult" (whose 3s Time.time timer + transition only tick at state 3/4) NEVER
        // progresses, and the intro camera + "DECISION PENDING" digital board stay frozen forever → the bowler
        // never bowls. Skip the spurious board entirely when the LBW outcome already stands (the umpire's
        // on-field decision is authoritative online; there is no stumping on a defended/missed LBW ball).
        // ...UNLESS this board was triggered by the BATTING authority's keeper-stumping relay
        // (RPC_ShowThirdUmpireReview, fromAuthoritativeRelay=true). That fires only when the batting side
        // actually showed a genuine keeper third-umpire review, so the bowling follower MUST mirror it —
        // suppressing it here (mpLbwOutcomeCommitted left stale from an earlier LBW this over) was the cause of
        // "keeper hits wicket + umpire decision on batting side, but bowler side shows NOTHING" (#4/morning#5).
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && mpLbwOutcomeCommitted && !fromAuthoritativeRelay)
        {
            ConstantsData_M.MpLog("[GroundController][ShowThirdUmpireDecisionBoard] Skipped spurious keeper 3rd-umpire review — LBW outcome already committed this ball.");
            currentWicketKeeperStatus = "loopEnd";
            return;
        }
        // Review-sync fix (#5): the whole third-umpire/stumping review runs off the LOCAL wicketkeeper
        // state machine + RNG, so it played only on the batting (authoritative) client; the bowling
        // client diverged (different RNG / keeper just collected) and was left on the plain pitch view.
        // This method is the SINGLE funnel every stumping third-umpire board passes through, so the
        // batting authority broadcasts onset + outcome (isPlayerStumped) here; the bowling client drives
        // its own identical board via RPC_ShowThirdUmpireReview. Gated to batting so there is no echo.
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex
            && GameConstants.isWithAI == false && CricketNetworkManager.instance != null)
        {
            CricketNetworkManager.instance.CmdShowThirdUmpireReview(staticVariables.UserProfiledata.user._id, isPlayerStumped);
        }
        Time.timeScale = 1f;
        Singleton<GameData>.instance.GameIsNotOnStumpingReplay();
        replayViewCamera.enabled = false;
        introCutsceneCamera.enabled = true;
        introCamTransform.position = new Vector3(-76f, 17f, -3.8f);
        introCamTransform.eulerAngles = new Vector3(0f, -90f, 0f);
        DigitalScreenRenderer.material.mainTexture = digitalBoardTextures[1];
        scoreboardScreen.transform.localScale = scoreboardScreenScale;
        _stayStartTime = Time.time;
        currentWicketKeeperStatus = "waitFor3rdUmpireResult";
    }

    // Review-sync fix (#5): driven on the BOWLING client by the batting authority's CmdShowThirdUmpireReview.
    // Reuses the same self-contained board path SkipStumpingReplay() already uses (currentActionState=3 +
    // isPlayerStumped + isWicketKeeperActive -> ShowThirdUmpireDecisionBoard), so the existing
    // waitFor3rdUmpireResult -> Show3rdUmpireResult states then render OUT/NOTOUT from isPlayerStumped.
    // MP fix (#7): Always override isPlayerStumped with the batting authority's value BEFORE the
    // idempotent check so that if the bowling side started the review with the wrong local result,
    // the Show3rdUmpireResult state will still display the correct OUT/NOTOUT outcome.
    // Bowler-side stump-review TIMING sync: holds the bowling follower on the "3rd umpire reviewing" board until
    // the BATTING authority reaches its OWN result (its 2nd CmdShowThirdUmpireReview, sent from
    // ShowThirdUmpireDecisionBoard after the batter's deliberation + replay). Without it the follower flashed
    // OUT/NOTOUT ~5s early while the batter was still reviewing (tester: "batter side full review, bowler side
    // foran OUT board"). The batter's ball-tracking REPLAY itself is deliberately NOT mirrored on the follower —
    // re-bowling the ball there corrupts the NEXT delivery (see ConstantsData_M.useFullReviewReplay), so the
    // follower shows the review BOARD held in sync, not the replay.
    private bool _bowlerStumpReviewHold;
    // Stump-path follower hold (see GroundController.Fielding stumpingAppeal decision): when this follower is
    // waiting at the stump decision for the batting authority's review relay, this marks the wait start
    // (-1 = not holding). Reset per delivery in ResetAll alongside _bowlerStumpReviewHold.
    internal float _stumpFollowerHoldStart = -1f;

    public void RPC_ShowThirdUmpireReview(bool playerStumped)
    {
        if (CONTROLLER.PlayModeSelected != 8 || CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            return;
        // Always accept the authoritative outcome even if the review board already started locally.
        isPlayerStumped = playerStumped;
        previousStumpedState = playerStumped;
        // Result already rendering — nothing more to do.
        if (currentWicketKeeperStatus == "Show3rdUmpireResult")
            return;
        // 2nd relay (the batting side reached its OWN result board): release the hold so the follower now advances
        // waitFor3rdUmpireResult -> the OUT/NOTOUT result, in sync with the batting side.
        if (currentWicketKeeperStatus == "waitFor3rdUmpireResult")
        {
            _bowlerStumpReviewHold = false;
            return;
        }
        // 1st relay (review onset): show the deliberation board and HOLD it (don't auto-advance to the result)
        // until the batting side finishes. waitFor3rdUmpireResult has a fallback timeout so a lost 2nd relay
        // can never hang the board.
        isWicketKeeperActive = true;
        currentActionState = 3;
        _bowlerStumpReviewHold = true;
        ShowThirdUmpireDecisionBoard(fromAuthoritativeRelay: true);   // authoritative keeper relay — bypass the LBW guard
    }

    public void EnableHardCode()
    {
        isHardcoded = true;
    }

    private bool CanProduceEdge()
    {
        // Lockstep review BUG 3: this method mixes per-client ObscuredPrefs counters with unseeded
        // UnityEngine.Random — the two clients would disagree on whether a shot edged. One seeded roll,
        // identical on both (rate ~11% matches the tuned num4/num5 percentages below).
        if (LockstepActive)
        {
            return DetRange(0, 100) < 11;
        }
        if (CONTROLLER.PlayModeSelected == 8)
        {
            if (!ObscuredPrefs.HasKey("perMatchEdgeCount" + CONTROLLER.PlayModeSelected))
            {
                ObscuredPrefs.SetInt("perMatchEdgeCount" + CONTROLLER.PlayModeSelected, 0);
            }
        }

        if (CONTROLLER.PlayModeSelected != 7)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 4;
            if (ObscuredPrefs.HasKey("perMatchEdgeCount" + CONTROLLER.PlayModeSelected))
            {
                num2 = ObscuredPrefs.GetInt("perMatchEdgeCount" + CONTROLLER.PlayModeSelected);
            }
            if (CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] < 2)
            {
                num3 = 2;
            }
            ////ConstantsData_M.MpLog("num 3: " + num3 + " num2 : " + num2);
            if (num2 < num3)
            {
                if (ObscuredPrefs.HasKey("newUserEdgeCount"))
                {
                    num = ObscuredPrefs.GetInt("newUserEdgeCount");

                }
                else
                {
                    ObscuredPrefs.SetInt("newUserEdgeCount", num);

                }
                int num4 = 11;
                int num5 = 11;
                if (CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] < 3 && CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex)
                {
                    num5 = 11;
                }
                if (num < 5)
                {
                    // Too-many-edges fix: num4/num5 are percentages (the sibling PlayMode-7 branch below
                    // rolls Random.Range(0,100) < 30). Here the roll was Random.Range(0,10) (0-9), so
                    // "< 11" was ALWAYS true => a guaranteed edge on every eligible ball until the
                    // per-match cap (4), which is what let a spin bowler take a hat-trick of caught-behinds.
                    // Roll 0-100 so num4/num5==11 means an 11% chance, matching the intended design.
                    if (UnityEngine.Random.Range(0, 100) < num4)
                    {
                        num++;
                        ObscuredPrefs.SetInt("newUserEdgeCount", num);
                        num2++;
                        ObscuredPrefs.SetInt("perMatchEdgeCount" + CONTROLLER.PlayModeSelected, num2);
                        ////ConstantsData_M.MpLog("EDGE: " + true);
                        return true;
                    }
                }
                else if (UnityEngine.Random.Range(0, 100) < num5)
                {
                    num2++;
                    ObscuredPrefs.SetInt("perMatchEdgeCount" + CONTROLLER.PlayModeSelected, num2);
                    ////ConstantsData_M.MpLog("EDGE: " + true);

                    return true;
                }
            }
        }
        else
        {
            int num6 = 0;
            int num7 = 0;
            if (ObscuredPrefs.HasKey("perMatchEdgeCount" + CONTROLLER.PlayModeSelected))
            {
                num7 = ObscuredPrefs.GetInt("perMatchEdgeCount" + CONTROLLER.PlayModeSelected);
            }
            if (num7 < 2)
            {
                if (ObscuredPrefs.HasKey("newUserEdgeCount"))
                {
                    num6 = ObscuredPrefs.GetInt("newUserEdgeCount");
                }
                else
                {
                    ObscuredPrefs.SetInt("newUserEdgeCount", num6);
                }
                int num8 = 30;
                int num9 = 15;
                if (num6 < 5)
                {
                    if (UnityEngine.Random.Range(0, 100) < num8)
                    {
                        num6++;
                        ObscuredPrefs.SetInt("newUserEdgeCount", num6);
                        num7++;
                        ObscuredPrefs.SetInt("perMatchEdgeCount" + CONTROLLER.PlayModeSelected, num7);
                        ////ConstantsData_M.MpLog("EDGE: " + true);

                        return true;
                    }
                }
                else if (UnityEngine.Random.Range(0, 100) < num9)
                {
                    num7++;
                    ObscuredPrefs.SetInt("perMatchEdgeCount" + CONTROLLER.PlayModeSelected, num7);
                    ////ConstantsData_M.MpLog("EDGE: " + true);

                    return true;
                }
            }
        }
        ////ConstantsData_M.MpLog("EDGE: " + false);

        return false;
    }

    public void SetDRSTrailRenderer()
    {
        ActivateColliders(boolean: false);
        Singleton<DRS>.instance.DRSRedTrail.SetActive(value: true);
        Singleton<DRS>.instance.DRSBallTrailMaterial.color = Color.red;
        Singleton<DRS>.instance.DRSRedTrail.GetComponent<TrailRenderer>().time = 15f;
    }

    public void waitForReview()
    {
        int myTeamIndex = CONTROLLER.myTeamIndex;
        int opponentTeamIndex = CONTROLLER.opponentTeamIndex;
        if (BattingBy == "user" && CONTROLLER.TeamList[myTeamIndex].noofDRSLeft > 0 && initialUmpireDecision && CONTROLLER.PlayModeSelected != 8)
        {
            canAIAssessDRS = false;
            drsByBattingSide = 1;
            drsByUser = 1;
            ShowDRSReplayPanel();
            return;
        }
        if (BowlingBy == "user" && CONTROLLER.TeamList[myTeamIndex].noofDRSLeft > 0 && !initialUmpireDecision && CONTROLLER.PlayModeSelected != 8)
        {
            canAIAssessDRS = false;
            drsByBattingSide = 0;
            drsByUser = 1;
            ShowDRSReplayPanel();
            return;
        }

        //Multiplayer

        if (BattingBy == "user" && CONTROLLER.TeamList[myTeamIndex].noofDRSLeft > 0 && initialUmpireDecision && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            canAIAssessDRS = false;
            drsByBattingSide = 1;
            drsByUser = 1;
            ShowDRSReplayPanel();
            return;
        }

        if (BowlingBy == "user" && CONTROLLER.TeamList[myTeamIndex].noofDRSLeft > 0 && !initialUmpireDecision && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
        {
            canAIAssessDRS = false;
            drsByBattingSide = 0;
            drsByUser = 1;
            ShowDRSReplayPanel();
            return;
        }
        //End  Multiplayer
        isBallPaused = true;
        canAIAssessDRS = false;

        int num = 0;
        int num2 = 0;

        if (CONTROLLER.PlayModeSelected != 8)
        {
            num = UnityEngine.Random.Range(0, 50);
            num2 = UnityEngine.Random.Range(0, 10);
        }
        else if (CONTROLLER.PlayModeSelected == 8)
        {
            num = 45;
            num2 = 7;
        }

        //int num = 45;
        //      int num2 = 7;

        if (BowlingBy == "user" && CONTROLLER.PlayModeSelected != 8)
        {
            if (!isBallInline && Mathf.Abs(matchBallTransform.position.x) > 0.16f && num2 <= 8)
            {
                num = 45;
            }
        }
        else if (BattingBy == "user" && isBallInline && Mathf.Abs(matchBallTransform.position.x) < 0.172f && num2 <= 8 && CONTROLLER.PlayModeSelected != 8)
        {
            num = 45;
        }
        if (BowlingBy == "user" && CONTROLLER.PlayModeSelected != 8)
        {
            drsByBattingSide = 1;
            drsByUser = 0;
        }
        else if (BattingBy == "user" && CONTROLLER.PlayModeSelected != 8)
        {
            drsByBattingSide = 0;
            drsByUser = 0;
        }
        if (num > 25 && BowlingBy == "user" && CONTROLLER.TeamList[opponentTeamIndex].noofDRSLeft > 0 && initialUmpireDecision && CONTROLLER.PlayModeSelected != 8)
        {
            canAIAssessDRS = !isBallInline;
            if (num > 30)
            {
                canAIAssessDRS = true;
            }
            if (ballHeightAtStumps > 0.75f)
            {
                canAIAssessDRS = true;
            }
            ShowDRSReplayPanel();
        }
        else if (num > 25 && BattingBy == "user" && CONTROLLER.TeamList[opponentTeamIndex].noofDRSLeft > 0 && !initialUmpireDecision && CONTROLLER.PlayModeSelected != 8)
        {
            canAIAssessDRS = isBallInline;
            if (num > 30)
            {
                canAIAssessDRS = true;
            }
            if (ballHeightAtStumps < 0.74f)
            {
                canAIAssessDRS = true;
            }
            ShowDRSReplayPanel();
        }
        else if (CONTROLLER.PlayModeSelected != 8)
        {
            isDRSEnabled = false;
            currentActionState = 3;
        }

        //Multiplayer
        if (BowlingBy == "user" && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
        {
            drsByBattingSide = 1;
            drsByUser = 0;
        }
        else if (BattingBy == "user" && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
        {
            drsByBattingSide = 0;
            drsByUser = 0;
        }

        if (BowlingBy == "user" && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            if (!isBallInline && Mathf.Abs(matchBallTransform.position.x) > 0.16f && num2 <= 8)
            {
                num = 45;
            }
        }
        else if (BattingBy == "user" && isBallInline && Mathf.Abs(matchBallTransform.position.x) < 0.172f && num2 <= 8 && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
        {
            num = 45;
        }

        if (num > 25 && BowlingBy == "user" && CONTROLLER.TeamList[opponentTeamIndex].noofDRSLeft > 0 && initialUmpireDecision && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
        {
            canAIAssessDRS = !isBallInline;
            if (num > 30)
            {
                canAIAssessDRS = true;
            }
            if (ballHeightAtStumps > 0.75f)
            {
                canAIAssessDRS = true;
            }
            ShowDRSReplayPanel();
        }
        else if (num > 25 && BattingBy == "user" && CONTROLLER.TeamList[opponentTeamIndex].noofDRSLeft > 0 && !initialUmpireDecision && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            canAIAssessDRS = isBallInline;
            if (num > 30)
            {
                canAIAssessDRS = true;
            }
            if (ballHeightAtStumps < 0.74f)
            {
                canAIAssessDRS = true;
            }
            ShowDRSReplayPanel();
        }
        else if (CONTROLLER.PlayModeSelected == 8)
        {
            isDRSEnabled = false;
            currentActionState = 3;
        }
    }

    public bool aiGoForDRS()
    {
        return canAIAssessDRS;
    }

    private void ShowDRSReplayPanel()
    {
        Singleton<DRS>.instance.ShowMe();
    }

    public void showUmpireAfterDrs()
    {
        currentActionState = -1;
        Time.timeScale = 1f;
        isReplayModeActive = false;
        remainingDRSChances = 0;
        Singleton<DRS>.instance.DRSBlueTrail.GetComponent<TrailRenderer>().Clear();
        Singleton<DRS>.instance.DRSRedTrail.GetComponent<TrailRenderer>().Clear();
        Singleton<DRS>.instance.DRSRedTrail.transform.SetParent(matchBall.transform);
        Singleton<DRS>.instance.DRSRedTrail.transform.localPosition = Vector3.zero;
        Singleton<DRS>.instance.DRSRedTrail.transform.localEulerAngles = Vector3.zero;
        Singleton<DRS>.instance.DRSRedTrail.SetActive(value: false);
        Singleton<DRS>.instance.DRSBlueTrail.SetActive(value: false);
        impactMarkerBall.SetActive(value: false);
        int num = 0;
        if (CONTROLLER.PlayModeSelected != 8)
        {
            num = ((drsByBattingSide == 1) ? ((!(BattingBy == "user")) ? CONTROLLER.opponentTeamIndex : CONTROLLER.myTeamIndex) : ((!(BattingBy == "user")) ? CONTROLLER.myTeamIndex : CONTROLLER.opponentTeamIndex));
        }
        else
        {
            if (drsByBattingSide == 1)
            {
                if (CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
                {
                    num = CONTROLLER.opponentTeamIndex;
                }
                else
                {
                    num = CONTROLLER.myTeamIndex;
                }
            }
            else
            {
                if (CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
                {
                    num = CONTROLLER.myTeamIndex;
                }
                else
                {
                    num = CONTROLLER.opponentTeamIndex;
                }
            }
        }
        showMainUmpireForNoBallAction();
        // MP full-replay: drive the umpire OUT/NOT-OUT decision from the AUTHORITATIVE synced LBW result
        // (IsLbwSavedDecision — stamped on BOTH clients via RPC_LBWDecision), identically on each side, and
        // DO NOT decrement noofDRSLeft here. The original branch logic below keys off perspective-relative
        // drsByBattingSide / myTeamIndex and the stale outViaDRS==isLbwSaved (which the IsLbwSavedDecision
        // getter's own note warns against) — none of which the bowling FOLLOWER sets, so online it would play
        // a CONTRADICTING verdict and decrement a DIFFERENT team's review count (a durable, reconnect-surviving
        // DRS-count desync). The review-count decrement already happened at review start (ReviewSystem), so the
        // 15483/15503 decrements here are the known double-decrement. Mirrors ShowDrsDecisionBoardOnly.
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && ConstantsData_M.useFullReviewReplay)
        {
            if (IsLbwSavedDecision)
            {
                mainUmpireAnim.Play("DRS_Out");
                StartCoroutine(playDrsAnimation("DRS_Out", 0, 0));
            }
            else
            {
                mainUmpireAnim.Play("DRS_SorryNotOut");
                StartCoroutine(playDrsAnimation("DRS_SorryNotOut", 1, 1));
                Singleton<GameData>.instance.PlayGameSound("Cheer");
            }
            return;
        }
        if (drsByBattingSide == 1)
        {
            if (outViaDRS == isLbwSaved)
            {
                mainUmpireAnim.Play("DRS_Out");
                if (!isUmpireCallScenario)
                {
                    CONTROLLER.TeamList[num].noofDRSLeft--;
                }
                StartCoroutine(playDrsAnimation("DRS_Out", 0, 0));
            }
            else
            {
                mainUmpireAnim.Play("DRS_SorryNotOut");
                StartCoroutine(playDrsAnimation("DRS_SorryNotOut", 1, 1));
                Singleton<GameData>.instance.PlayGameSound("Cheer");
            }
        }
        else if (outViaDRS == isLbwSaved)
        {
            mainUmpireAnim.Play("DRS_NotOut");
            if (!PlayerPrefs.HasKey("drs"))
            {
                PlayerPrefs.SetInt("drs", 1);
            }
            if (!isUmpireCallScenario)
            {
                CONTROLLER.TeamList[num].noofDRSLeft--;
            }
            StartCoroutine(playDrsAnimation("DRS_NotOut", 1, 2));
        }
        else
        {
            mainUmpireAnim["DRS_SorryOut"].speed = 0.2f;
            mainUmpireAnim.Play("DRS_SorryOut");
            StartCoroutine(playDrsAnimation("DRS_SorryOut", 0, 3));
            Singleton<GameData>.instance.PlayGameSound("Cheer");
        }
    }

    private IEnumerator playDrsAnimation(string animName, int status, int animationType)
    {
        float waitTime2 = 0f;
        waitTime2 = ((!mainUmpireAnim.IsPlaying("DRS_SorryOut")) ? mainUmpireAnim[animName].length : (mainUmpireAnim[animName].length / 1.5f));
        yield return new WaitForSeconds(waitTime2);
        // Review "action replay" stuck + auto-bowl fix: a throw inside drsOut/drsNotOut (e.g.
        // UpdateCurrentBall -> BallSimulationManager.SetBallSimulationData IndexOutOfRange when
        // simBallIndex overran the over's array on a DRS re-entry) ABORTED this coroutine BEFORE the
        // cleanup below — so the "action replay" text stayed on screen and ReplayShowing/canShowReplay
        // stayed true, the over never resumed, and the auto-bowl timer fired the next ball by itself.
        // Catch so the cleanup (hide the replay text, clear the replay flags) ALWAYS runs; the
        // authoritative ball outcome still arrives separately via RpcBallOutcome.
        try
        {
            if (status == 0) drsOut();
            else drsNotOut();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GroundController] DRS result handler error (replay cleanup still runs): " + e);
        }
        Singleton<GameData>.instance.actionStatusText.gameObject.SetActive(value: false);
        CONTROLLER.ReplayShowing = false;
        CONTROLLER.canShowReplay = false;
        CONTROLLER.reviewReplay = false;
        Singleton<DRS>.instance.DRSreplay = false;
        outViaDRS = true;
        drsByBattingSide = -1;
        drsByUser = -1;
        // MP full-replay: after the cosmetic umpire decision, do the PROVEN clean ball-state reset (the same
        // resume the online board path uses) so the re-simmed replay ball can't leak a corrupted flight state
        // into the next delivery. drsOut/drsNotOut already skipped the scoring online (authoritative outcome
        // stands), so this is purely the camera/HUD/ball reset. Offline path untouched.
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && ConstantsData_M.useFullReviewReplay)
        {
            ForceResolveStuckReview();
        }
    }

    // Clean DRS-review resolution. Called by the watchdog (timeout fallback) AND directly from
    // DRS.YesBtnClicked ONLINE, where the ball-tracking replay is SKIPPED entirely: that replay can't
    // complete online (the bowling client suppresses physics) and repositioning the ball for it left the
    // ball/delivery state corrupted, which leaked into the NEXT ball as an off-course runaway-bounce that
    // froze the match. The authoritative RpcBallOutcome already stands, so we just clear the review
    // overlay, restore the gameplay view, and reset the ball to a clean pre-delivery state.
    // MP DRS-board fix (#2 — LBW path): online, DRS.YesBtnClicked used to jump straight to ForceResolveStuckReview
    // so no OUT/NOT-OUT umpire board ever rendered ("Yes" looked like nothing happened). This renders a LIGHTWEIGHT
    // authoritative board (umpire DRS_Out/DRS_SorryNotOut anim + DRS result panel) with NO physics replay and NO
    // ball reposition, then performs the SAME clean resume (ForceResolveStuckReview) the online path already used.
    // isOut is the batting authority's isLbwSaved (relayed via RpcDRS_Decision). Idempotent: a duplicate call while
    // a board is already showing is ignored, so the reviewer's local render + the relayed render can't stack.
    public void ShowDrsDecisionBoardOnly(bool isOut)
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI)
        {
            // Never reachable from the gated callers; if it is, just do the clean resume (no board).
            ForceResolveStuckReview();
            return;
        }
        if (_drsBoardOnlyRoutine != null) return; // already rendering this review's board — ignore duplicate
        _drsBoardOnlyRoutine = StartCoroutine(DrsDecisionBoardOnlyRoutine(isOut));
    }

    private IEnumerator DrsDecisionBoardOnlyRoutine(bool isOut)
    {
        Time.timeScale = 1f;
        if (Singleton<DRS>.instance != null) Singleton<DRS>.instance.ResetPanelTransition();
        // Umpire camera + HUD hide — the SAME no-physics helper showUmpireAfterDrs uses (no ball reposition).
        showMainUmpireForNoBallAction();
        string animName = isOut ? "DRS_Out" : "DRS_SorryNotOut";
        if (mainUmpireAnim != null) mainUmpireAnim.Play(animName);
        if (Singleton<DRS>.instance != null)
        {
            if (isOut) Singleton<DRS>.instance.ShowDRSResultPanel(0, "OUT", 0);
            else Singleton<DRS>.instance.ShowDRSResultPanel(0, "NOT OUT", 1);
        }
        if (!isOut && Singleton<GameData>.instance != null) Singleton<GameData>.instance.PlayGameSound("Cheer");
        // Hold the board for a bounded UNSCALED window (review can run under an altered timeScale).
        float elapsed = 0f;
        float hold = (mainUmpireAnim != null && mainUmpireAnim[animName] != null)
            ? Mathf.Clamp(mainUmpireAnim[animName].length, 1.5f, 4f) : 2.5f;
        while (elapsed < hold)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (Singleton<DRS>.instance != null) Singleton<DRS>.instance.Hide();
        _drsBoardOnlyRoutine = null;
        ForceResolveStuckReview(); // the proven ball-state reset / clean resume — unchanged
    }

    public void ForceResolveStuckReview()
    {
        ConstantsData_M.MpLog("[GroundController] Resolving DRS review (online skip-replay / watchdog) — clean resume.");
        Time.timeScale = 1f;
        // Caught-behind review-stuck fix: if a caught-behind OUT was decided but a review interrupted
        // before decisionPending could record it, flush it now so the authoritative RpcBallOutcome
        // reaches the bowling client (otherwise it waits for the outcome forever — stuck on review,
        // camera jerking, fielders chasing the edge). UpdateCurrentBall sends it on the batting
        // authority; on the bowling client it self-suppresses (no double count). Idempotent via flag.
        if (mpCaughtBehindOutPending)
        {
            mpCaughtBehindOutPending = false;
            // Clear the UltraEdge defer-guard fields BEFORE the flush — else the flush UpdateCurrentBall
            // re-hits the GameData defer block (~1551), re-shows the review, and records NOTHING (the orphan).
            // On the batting authority this flush is the real record → WicketBall → CmdBallOutcome → RpcBallOutcome;
            // on the bowling client it self-suppresses at the bowling gate (no double-count).
            isUltraEdgeCutscenePlaying = false;
            canUserAskForReview = false;
            canAIAskForReview = false;
            hasUmpireAnimationPlayed = false;
            if (Singleton<GameData>.instance != null)
            {
                ConstantsData_M.MpLog("[GroundController][ForceResolveStuckReview] Flushing pending caught-behind OUT outcome.");
                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 1, 5, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
            }
        }
        // GENERAL DEFERRED-OUTCOME FLUSH (game-stuck-after-review fix): GameData.UpdateCurrentBall (~1557)
        // DEFERS the ball outcome into validBallCount/isWicketTaken/... and shows the ReviewSystem UltraEdge
        // panel whenever a review is offered (e.g. an onPads LBW-not-out ball that deflects to the keeper).
        // ReviewSystem.NoBtnClicked commits that stored outcome on DECLINE, but the online YES / TookReview /
        // timeout paths (ReviewSystem 147/272/336) only call THIS resolver WITHOUT committing it → no
        // RpcBallOutcome is ever sent → the over HANGS (bowler stuck in "waiting", next ball never bowled).
        // Commit the stored on-field outcome here (online no-overturn → the umpire's initial decision stands,
        // same as NoBtnClicked). The batting authority sends it; the bowling follower self-suppresses (no
        // double-count). Gated on the still-set defer flag so it can't double-commit (the caught-behind branch
        // above and NoBtnClicked both clear it, and a 2nd resolver call finds it false).
        else if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && isUltraEdgeCutscenePlaying
                 && (canUserAskForReview || canAIAskForReview))
        {
            isUltraEdgeCutscenePlaying = false;
            canUserAskForReview = false;
            canAIAskForReview = false;
            hasUmpireAnimationPlayed = false;
            if (Singleton<GameData>.instance != null)
            {
                int outNum = (umpireInitialDecision == "out") ? 1 : 0;
                ConstantsData_M.MpLog("[GroundController][ForceResolveStuckReview] Flushing deferred review outcome (review taken/timed out; on-field decision stands).");
                Singleton<GameData>.instance.UpdateCurrentBall(validBallCount, remainingBallCount, totalRunsScored, extraRuns, currentBatsmanID, outNum, typeOfWicket, bowlerId, catcherId, batsmanOutId, isBoundaryHit);
            }
        }
        isReplayModeActive = false;
        isDRSEnabled = false;
        CONTROLLER.cameraType = 1;
        // Pause the StandbyCam rotate tween the review panel started (DRS/ReviewSystem ShowMe), exactly
        // like DRS.NoBtnClicked does, so it doesn't linger/jitter after the resume.
        if (Singleton<StandbyCam>.instance != null) Singleton<StandbyCam>.instance.PauseTween();
        if (infraredCamera != null) infraredCamera.SetActive(value: false);
        if (umpireViewCamera != null) umpireViewCamera.enabled = true;
        // THIRD-UMPIRE / KEEPER-REVIEW teardown (the REAL "3rd UMPIRE DECISION PENDING" stuck fix): an onPads
        // ball that deflects to the keeper spuriously triggers the keeper third-umpire review (~8943): it
        // switches to the introCutscene camera, sets the stadium digital board to "DECISION PENDING"
        // (digitalBoardTextures[1]) and currentWicketKeeperStatus="waitFor3rdUmpireResult" — and that review
        // never resolves online. So even though the LBW outcome committed and the over advanced, the VIEW stays
        // frozen on the DECISION-PENDING third-umpire screen and the bowler never bowls. Force the gameplay view
        // back: kill the intro camera, reset the digital board to the default scoreboard, and clear the keeper/
        // run-out 3rd-umpire wait state. (Run-out resolves via the fielderAction state machine, which never
        // calls this resolver, so this can't blank a legit run-out board.)
        if (introCutsceneCamera != null) introCutsceneCamera.enabled = false;
        SetDefaultDigitalDisplayContent();
        // Backstop: clear ANY keeper third-umpire / review-pending state so its state machine (which only
        // ticks at currentActionState 3/4 and re-paints the digital board) can't re-show "DECISION PENDING"
        // after we reset it. Set to the terminal "loopEnd" (no handler re-processes it). Unconditional log so
        // we always see the exact state in the next capture.
        ConstantsData_M.MpLog($"[GroundController][ForceResolveStuckReview] teardown: wkStatus='{currentWicketKeeperStatus}' fielderAction='{fielderAction}' actionState={currentActionState} timerActive={(timerObject != null && timerObject.activeInHierarchy)} introCam={(introCutsceneCamera != null && introCutsceneCamera.enabled)} boardTex0Null={(digitalBoardTextures == null || digitalBoardTextures.Length == 0 || digitalBoardTextures[0] == null)} mpLbwCommitted={mpLbwOutcomeCommitted}");
        if (currentWicketKeeperStatus == "waitFor3rdUmpireResult" || currentWicketKeeperStatus == "Show3rdUmpireResult"
            || currentWicketKeeperStatus == "decisionPending" || currentWicketKeeperStatus == "waitForCaughtBehindResult"
            || currentWicketKeeperStatus == "waitForStumpingResult" || currentWicketKeeperStatus == "CaughtBehind"
            || currentWicketKeeperStatus == "stumpingAppeal" || currentWicketKeeperStatus == "showWideSignalBeforeStumpingResult"
            || currentWicketKeeperStatus == "BadCallStumpingResult" || currentWicketKeeperStatus == "waitForResult")
        {
            currentWicketKeeperStatus = "loopEnd";
        }
        if (fielderAction == "waitFor3rdUmpireResultForRunout")
            fielderAction = string.Empty;
        GameData gd = Singleton<GameData>.instance;
        if (gd != null)
        {
            if (gd.actionStatusText != null) gd.actionStatusText.gameObject.SetActive(value: false);
            if (gd.skipButton != null) gd.skipButton.gameObject.SetActive(value: false);
        }
        CONTROLLER.ReplayShowing = false;
        CONTROLLER.canShowReplay = false;
        CONTROLLER.reviewReplay = false;
        if (Singleton<DRS>.instance != null) Singleton<DRS>.instance.DRSreplay = false;
        // REVIEW-PANEL TEARDOWN (stuck "DECISION PENDING" fix): this resolver runs on the watchdog /
        // online skip-replay path WITHOUT ever calling DRS.Yes/NoBtnClicked or ReviewSystem.Yes/No/test —
        // the ONLY sites that hide those review panels. So an LBW radar (DRS.holder + its "DECISION PENDING"
        // aiReviewstatus + live 6s countdown) AND/OR the onPads-to-keeper UltraEdge panel (ReviewSystem
        // userReview/AiReview) were left active forever, blocking the next delivery even though the outcome
        // committed, the over advanced and the bowler re-armed to -2. Tear BOTH down here so EVERY resolver
        // path clears the overlay. DismissReviewPanel also kills the dangling countdown so it can't late-fire
        // a stale NoBtnClicked. The player still SEES the panel during the review — it is only hidden here,
        // on resolve. Null-safe; idempotent (panels already inactive on the Yes/No paths).
        if (Singleton<DRS>.instance != null) Singleton<DRS>.instance.DismissReviewPanel();
        if (Singleton<ReviewSystem>.instance != null)
        {
            if (Singleton<ReviewSystem>.instance.userReview != null) Singleton<ReviewSystem>.instance.userReview.SetActive(value: false);
            if (Singleton<ReviewSystem>.instance.AiReview != null) Singleton<ReviewSystem>.instance.AiReview.SetActive(value: false);
        }
        outViaDRS = true;
        drsByBattingSide = -1;
        drsByUser = -1;
        // re-show the HUD that GameData.GameIsOnReplay() hid (matches GameData.ReplayCompleted())
        Singleton<Scoreboard>.instance.Hide(boolean: false);
        Singleton<PreviewScreen>.instance.Hide(boolean: false);
        // BALL-STATE RESET: reset the transient flight state to a clean pre-delivery (ball back in the
        // bowler's hand) so the resumed state machine cannot run a delivery on the dead/reviewed ball.
        isBallReleased = false;
        isBallHit = false;
        isBallPaused = false;
        currentBallStatus = string.Empty;
        bounceCount = 0;
        launchAngle = 0f;
        horizontalVelocity = 0f;
        arcHeight = 0f;
        angleChangeRate = 0f;
        matchBallTransform.position = initialBallPosition;
        matchBallTransform.eulerAngles = new Vector3(0f, 2f, 180f);
        temporaryPosition = matchBallTransform.position;
        // DETERMINISTIC-DELIVERY DISARM (game-stuck fix): this reset put the ball back in the bowler's hand,
        // but the fixed-step delivery integrator (StepDeliveryDeterministic, gated on _useDeterministicBallStep)
        // was left ARMED from the resolved delivery. It then kept stepping the dead ball every frame
        // (StepBurst "toRun=N ... advanced 0.00" at the release z) and the MATCH FROZE with no recovery until
        // the app was killed. Disarm it exactly like ResetAll (~2600) so the legacy integrator owns the idle
        // ball and the NEXT delivery re-arms cleanly. (Symptom seen after an onPads review force-resolved via
        // ReviewSystem while a deterministic delivery was armed.)
        _useDeterministicBallStep = false;
        _deterministicShotArmed = false;
        _ballSimTicksDone = 0;
        _ballSimMaxTargetTicks = -1;
        ShowBall(status: true);
        // resume exactly like declining a review (DRS.NoBtnClicked): hand control back for the next ball.
        // EXCEPT: online, when the LBW outcome was already committed at the appeal mark, the over already
        // advanced — so FORCE the bowler-ready state -2 here (only -2 re-arms the auto-bowl; state 3 is
        // in-delivery plumbing and does NOT bowl the next ball). Earlier we merely SKIPPED the =3 write hoping
        // the OppAck handshake had set -2, but the bowler still hung (wkStatus/fielderAction were already clear,
        // yet no next ball) — so set -2 explicitly to guarantee the auto-bowl re-arms. (case -2 in Update walks
        // up timerObject's parents + re-activates it, so the timer deactivated just below re-arms next frame.)
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && mpLbwOutcomeCommitted)
            currentActionState = -2;
        else
            currentActionState = 3;
        // drop the stale timer so the bowler-ready logic starts a fresh auto-bowl window for the next ball
        if (timerObject != null) timerObject.SetActive(value: false);
    }

    private void drsOut()
    {
        if (Singleton<GameData>.instance != null)
        {
            noBallRunStatus = "drsOut";
            // MP full-replay: the authoritative RpcBallOutcome already recorded this delivery over the wire;
            // re-recording here would DOUBLE-COUNT (and on the follower fight the suppressed-scoring gate).
            // Online the replay + umpire animation are cosmetic only, so skip the scoring tail.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && ConstantsData_M.useFullReviewReplay)
                return;
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 1, 2, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
    }

    private void drsNotOut()
    {
        if (Singleton<GameData>.instance != null)
        {
            noBallRunStatus = "drsNotOut";
            // MP full-replay: authoritative RpcBallOutcome already recorded this delivery; skip the scoring
            // tail online (cosmetic replay + umpire animation only) so we don't DOUBLE-COUNT the ball.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && ConstantsData_M.useFullReviewReplay)
                return;
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
    }

    public void SetDRSReplayUI()
    {
        if (remainingDRSChances == 0)
        {
            isUmpireCallScenario = false;
            if (isLbwSaved)
            {
                Singleton<DRS>.instance.ShowDRSResultPanel(0, "OUT", 0);
            }
            else
            {
                Singleton<DRS>.instance.ShowDRSResultPanel(0, "NOT OUT", 1);
            }
            remainingDRSChances = 1;
        }
        else if (remainingDRSChances == 1)
        {
            if (isFullToss)
            {
                isPitchingDetected = true;
                if (matchBallTransform.position.z >= 9.3f)
                {
                    remainingDRSChances = 2;
                }
            }
            else
            {
                if (bounceCount <= 0)
                {
                    return;
                }
                if (Mathf.Abs(matchBallTransform.position.x) < 0.14f)
                {
                    isPitchingDetected = true;
                    Singleton<DRS>.instance.ShowDRSResultPanel(1, "IN-LINE", 1);
                }
                else if (matchBallTransform.position.x <= -0.14f)
                {
                    if (batsmanHand == "right")
                    {
                        isPitchingDetected = true;
                        if (isFullToss)
                        {
                            Singleton<DRS>.instance.ShowDRSResultPanel(1, "FULL TOSS", 1);
                        }
                        else
                        {
                            Singleton<DRS>.instance.ShowDRSResultPanel(1, "OUTSIDE OFF", 1);
                        }
                    }
                    else if (!isFullToss)
                    {
                        isPitchingDetected = false;
                        Singleton<DRS>.instance.ShowDRSResultPanel(1, "OUTSIDE LEG", 0);
                    }
                    else
                    {
                        isPitchingDetected = true;
                        if (isFullToss)
                        {
                            Singleton<DRS>.instance.ShowDRSResultPanel(1, "FULL TOSS", 1);
                        }
                        else
                        {
                            Singleton<DRS>.instance.ShowDRSResultPanel(1, "OUTSIDE OFF", 1);
                        }
                    }
                }
                else if (matchBallTransform.position.x >= 0.14f)
                {
                    if (batsmanHand == "left")
                    {
                        isPitchingDetected = true;
                        if (isFullToss)
                        {
                            Singleton<DRS>.instance.ShowDRSResultPanel(1, "FULL TOSS", 1);
                        }
                        else
                        {
                            Singleton<DRS>.instance.ShowDRSResultPanel(1, "OUTSIDE OFF", 1);
                        }
                    }
                    else if (!isFullToss)
                    {
                        isPitchingDetected = false;
                        Singleton<DRS>.instance.ShowDRSResultPanel(1, "OUTSIDE LEG", 0);
                    }
                    else
                    {
                        isPitchingDetected = true;
                        if (isFullToss)
                        {
                            Singleton<DRS>.instance.ShowDRSResultPanel(1, "FULL TOSS", 1);
                        }
                        else
                        {
                            Singleton<DRS>.instance.ShowDRSResultPanel(1, "OUTSIDE OFF", 1);
                        }
                    }
                }
                if (!isPitchingDetected)
                {
                    isPitchOutsideLegForDRS = true;
                    remainingDRSChances = 4;
                    outViaDRS = false;
                }
                else
                {
                    remainingDRSChances = 2;
                }
            }
        }
        else
        {
            if (remainingDRSChances != 2 || !(matchBallTransform.position.z >= 9.92f))
            {
                return;
            }
            if (matchBallTransform.position.y <= 0.775f && Mathf.Abs(matchBallTransform.position.x) < 0.13f)
            {
                isHittingDetected = true;
                Singleton<DRS>.instance.ShowDRSResultPanel(3, "HITTING", 1);
            }
            else if (matchBallTransform.position.y <= 0.817f && Mathf.Abs(matchBallTransform.position.x) <= 0.171f)
            {
                Singleton<DRS>.instance.ShowDRSResultPanel(3, "UMPIRE'S CALL", 2);
                if (isLbwSaved)
                {
                    isHittingDetected = true;
                }
                else
                {
                    isHittingDetected = false;
                }
                isUmpireCallScenario = true;
                outViaDRS = isHittingDetected;
            }
            else
            {
                outViaDRS = false;
                isHittingDetected = false;
                Singleton<DRS>.instance.ShowDRSResultPanel(3, "MISSING", 0);
            }
            remainingDRSChances = 3;
        }
    }

    public void CheckDRSImpactOnPad()
    {
        impactOnOffsideDuringShot = false;
        if (!shouldDisplayImpact || !wasReplayImpacted)
        {
            return;
        }
        if (Mathf.Abs(impactMarkerBall.transform.position.x) < 0.12f)
        {
            isImpactDetected = true;
            Singleton<DRS>.instance.ShowDRSResultPanel(2, "IN-LINE", 1);
        }
        else if (impactMarkerBall.transform.position.x <= -0.1f)
        {
            if (batsmanHand == "right")
            {
                Singleton<DRS>.instance.ShowDRSResultPanel(2, "OUTSIDE OFF", 0);
                if (currentBatsmanAnimation != "bt6Leave")
                {
                    isImpactDetected = false;
                    impactOnOffsideDuringShot = true;
                    Singleton<DRS>.instance.ShowDRSResultPanel(4, string.Empty, 0);
                }
                else
                {
                    isImpactDetected = true;
                }
            }
            else if (impactMarkerBall.transform.position.x <= -0.16f)
            {
                isImpactDetected = false;
                Singleton<DRS>.instance.ShowDRSResultPanel(2, "OUTSIDE LEG", 0);
            }
            else if (impactMarkerBall.transform.position.x < -0.1f && impactMarkerBall.transform.position.x > -0.16f)
            {
                Singleton<DRS>.instance.ShowDRSResultPanel(2, "UMPIRE'S CALL", 2);
                isUmpireDecisionDone = true;
                isImpactDetected = true;
            }
        }
        else if (impactMarkerBall.transform.position.x >= 0.1f)
        {
            if (batsmanHand == "left")
            {
                Singleton<DRS>.instance.ShowDRSResultPanel(2, "OUTSIDE OFF", 0);
                isImpactDetected = false;
                if (currentBatsmanAnimation != "bt6Leave")
                {
                    isImpactDetected = false;
                    impactOnOffsideDuringShot = true;
                    Singleton<DRS>.instance.ShowDRSResultPanel(4, string.Empty, 0);
                }
                else
                {
                    isImpactDetected = true;
                }
            }
            else if (impactMarkerBall.transform.position.x >= 0.16f)
            {
                isImpactDetected = false;
                Singleton<DRS>.instance.ShowDRSResultPanel(2, "OUTSIDE LEG", 0);
            }
            else if (impactMarkerBall.transform.position.x > 0.1f && impactMarkerBall.transform.position.x < 0.16f)
            {
                Singleton<DRS>.instance.ShowDRSResultPanel(2, "UMPIRE'S CALL", 2);
                isUmpireDecisionDone = true;
                isImpactDetected = true;
            }
        }
        if (!isImpactDetected)
        {
            outViaDRS = false;
            if (!isUmpireDecisionDone)
            {
            }
        }
    }

    //Photon Removal [PunRPC]
    public void RPC_LBWDecision(bool LbwAppeal, bool BallInline, bool lbw)
    {
        CONTROLLER.LBWAPPEAL = LbwAppeal;
        CONTROLLER.BALLINLINE = BallInline;
        CONTROLLER.Lbw = lbw;

        // MP LBW fix (bowling client): the LBW review flow must be driven from the synced umpire
        // decision, NOT from a local pad collision. In MP the bowling client receives the ball's
        // "onPads" status via RPC_ChangeBallAngle (sets currentBallStatus=="onPads"), so the local
        // leg-collider branch in OnCustomTriggerEnter — which requires currentBallStatus=="bowling"
        // — never fires. That branch was the ONLY place hasLBWAppeal/initialUmpireDecision got set,
        // so on the bowling client they stayed false -> ShowFielder10 never entered "lbwAppeal" ->
        // waitForReview() was never called -> no review panel (the reported NOTOUT symptom).
        // Set them here from the synced decision so the flow starts deterministically. Idempotent
        // with OnCustomTriggerEnter's Block B (same values) if a local collision also happens to fire.
        if (CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex
            && LbwAppeal)
        {
            hasLBWAppeal = true;
            isLBW = lbw;
            initialUmpireDecision = lbw;
            isLbwAppealSaved = hasLBWAppeal;
            isLbwSaved = isLBW;
            // Bowling client never ran OnCustomTriggerEnter's pad Block B (it got "onPads" via
            // RPC_ChangeBallAngle, not a local collision), so isBowlerActivationAllowed is still TRUE
            // from ball release (line 4184). With it true, ShowFielder10 (called per-frame at line 4192
            // once currentBallStatus=="onPads") early-returns at its guard (line 2281) and never sets
            // fielderAction="lbwAppeal" -> the review flow never starts. Reset it false here, exactly
            // like the batting path does at line 16009, so the next ShowFielder10 enters the lbwAppeal flow.
            isBowlerActivationAllowed = false;
        }
    }

    public void ShowReplay()
    {
        if (CONTROLLER.reviewReplay)
        {
            ReviewReplay();
            return;
        }
        destroyCelebrationBatsman();
        isReplayModeActive = true;
        replayCameraController.FollowDistance = 10f;
        ResetAll();
        ActivateReplayCamera();
        Time.timeScale = 1f;
        replayCameraBoundaryRotationAngle = 5 + UnityEngine.Random.Range(-2, 3) * 10;
        currentShotPlayed = savedPlayedShot;
        StartBowling();
        ActivateStadiumAndSkybox(boolean: true);
        if (!CONTROLLER.stumpingAttempted)
        {
            ReplayLookAtBowler();
        }
        else
        {
            replayActionStatus = "StumpingLookAt";
        }
    }

    private void ReviewReplay()
    {
        isReplayModeActive = true;
        // ── MP LBW DRS-replay COMPLETION (useFullReviewReplay; DEFAULT-ON, so this path is LIVE) ───────
        // The online ball-tracking replay's camera-handoff gates in BowlingBallMovement (the
        // "DRSreplay && ballRayCastConnectionZPositionSaved != 0f" checks ~10144/10150) need that impact z
        // non-zero to ever reach DRSCameraScript.DRSCompleted -> showUmpireAfterDrs. The BATTING AUTHORITY
        // captures it live at the pad contact (~17615) and it PERSISTS (ResetAll does NOT clear it — only
        // BowlNextBall does, on the NEXT delivery), so its replay already completes. The BOWLING FOLLOWER
        // never runs the pad-collision branch (it gets "onPads" via RPC_ChangeBallAngle, not a local hit),
        // so its z stays 0 and its replay would animate then HANG. Capture the impact z + the (synced) LBW
        // flags here, and re-seed the gate AFTER StartBowling (below): the follower (z==0) falls back to the
        // stump-impact z so its gate fires as the replay ball reaches the stumps. A watchdog armed below
        // bounds any residual hang (e.g. a no-bounce re-sim that sticks in SetDRSReplayUI). LBW(onPads) only.
        bool mpLbwReplayComplete = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && ConstantsData_M.useFullReviewReplay
            && (summarySaved == "onPads" || isLbwAppealSaved || hasLBWAppeal);
        float mpLbwReplayImpactZ = ballRayCastConnectionZPositionSaved;
        // ──────────────────────────────────────────────────────────────────────────────────────────────
        ResetAll();
        Time.timeScale = 1f;
        currentShotPlayed = savedPlayedShot;
        // DRS-REPLAY BALL-NOT-TRAVELING FIX: the live delivery leaves isUltraEdgeCutscenePlaying=true and
        // isShotExecuted=true, and currentBatsmanAnimation still names the finished live shot clip (its
        // .time is past startingFramePoint). ResetAll() does NOT clear any of these, so on the very first
        // replay Update() CheckForHotspotPosition() would immediately set isBallPaused=true +
        // Time.timeScale=0.035f — the bowler animates but BowlingBallMovement() early-returns on
        // isBallPaused, so the ball never flies. Re-arm the cutscene state here so the hotspot freeze only
        // triggers AFTER the replay re-bowl reaches the shot/edge frame (isShotExecuted is set true again
        // by ExecuteTheShot during the re-sim). Replay-only: does not touch live run-up/ball-position sync.
        isShotExecuted = false;
        isBallPaused = false;
        isBallPausedAtImpact = false;
        isAnimationStarted = false;
        isBallPlaced = false;
        isCustomBallMovementActive = false;
        isBallMovementChanged = false;
        RewindTime.CaptureCount = 0;
        if (!string.IsNullOrEmpty(currentBatsmanAnimation) && batsmanAnim[currentBatsmanAnimation] != null)
        {
            batsmanAnim[currentBatsmanAnimation].time = 0f;
            SetCurrentBatsmanAnimSpeed(1f);
        }
        StartBowling();
        ActivateStadiumAndSkybox(boolean: true);
        // Re-seed the camera-handoff gate so the online LBW replay can COMPLETE on the bowling follower
        // (reach DRSCompleted -> showUmpireAfterDrs) instead of animating and hanging. See note above.
        if (mpLbwReplayComplete)
        {
            if (mpLbwReplayImpactZ == 0f && stumpImpactSpot != null)
                mpLbwReplayImpactZ = stumpImpactSpot.transform.position.z;
            if (mpLbwReplayImpactZ != 0f)
                ballRayCastConnectionZPositionSaved = mpLbwReplayImpactZ;
            // Safety net: if the re-sim sticks (e.g. SetDRSReplayUI's remainingDRSChances 1->2 step needs a
            // bounce — bounceCount<=0 returns forever), the review would freeze with no recovery (the online
            // auto-bowl is suppressed while DRSreplay is true). Bound it: force a clean resume after a window
            // longer than a full DRS camera sequence (~16s). Self-cancels the instant the replay completes
            // normally (reviewReplay goes false), so it's a no-op on the happy path.
            StartMpReviewWatchdog(25f);
        }
    }

    private void ReplayLookAtBowler()
    {
        //Constants_M.Log("ReplayLookAtBowler");
        replayActionStatus = "lookAt";
        replayControllerTransform.eulerAngles = new Vector3(replayControllerTransform.eulerAngles.x, 0f, replayControllerTransform.eulerAngles.z);
        replayCameraController.enabled = false;
        if (bowlerType == "fast")
        {
            replayControllerTransform.position = new Vector3(0f, 2.3f, -24.9f);
            iTween.MoveTo(replayController, iTween.Hash("position", new Vector3(0f, 2.3f, -8.88f), "time", 3.2f, "easetype", "easeInOutSine", "delay", 0.5f));
        }
        else if (bowlerType == "spin")
        {
            replayControllerTransform.position = new Vector3(0f, 2.3f, -12f);
            iTween.MoveTo(replayController, iTween.Hash("position", new Vector3(0f, 2.3f, -8.88f), "time", 4, "easetype", "easeInOutSine", "delay", 2.5f));
        }
        else if (bowlerType == "medium")
        {
            replayControllerTransform.position = new Vector3(0f, 2.3f, -16f);
            iTween.MoveTo(replayController, iTween.Hash("position", new Vector3(0f, 2.3f, -8.88f), "time", 4, "easetype", "easeInOutSine", "delay", 2.5f));
        }
        if (!isOversteppedDelivery)
        {
            if (bowlerSide == "left")
            {
                replayCamTransform.position = new Vector3(-6f, 2.3f, 0f);
                if (UnityEngine.Random.Range(-10f, 10f) > 0f)
                {
                    if (bowlerType == "spin")
                    {
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(-6f, 5.3f, -5f), "time", 4, "easetype", "easeInOutSine", "delay", 2));
                    }
                    else if (bowlerType == "fast")
                    {
                        replayCamTransform.position = new Vector3(-16f, 1.3f, -37f);
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(-10f, 2.3f, -15f), "time", 3.2f, "easetype", "easeInOutSine", "delay", 0.5f));
                    }
                    else if (bowlerType == "medium")
                    {
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(-6f, 5.3f, -5f), "time", 4, "easetype", "easeInOutSine", "delay", 2));
                    }
                }
            }
            else if (bowlerSide == "right")
            {
                replayCamTransform.position = new Vector3(6f, 2.3f, 0f);
                if (UnityEngine.Random.Range(-10f, 10f) > 0f)
                {
                    if (bowlerType == "spin")
                    {
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(6f, 5.3f, -5f), "time", 4, "easetype", "easeInOutSine", "delay", 2));
                    }
                    else if (bowlerType == "fast")
                    {
                        replayCamTransform.position = new Vector3(16f, 1.3f, -37f);
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(10f, 2.3f, -15f), "time", 3.2, "easetype", "easeInOutSine", "delay", 0.5));
                    }
                    else if (bowlerType == "medium")
                    {
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(6f, 5.3f, -5f), "time", 4, "easetype", "easeInOutSine", "delay", 2));
                    }
                }
            }
        }
        else if (bowlerSide == "left")
        {
            replayCamTransform.position = new Vector3(-11f, 2.3f, -8.8f);
            if (UnityEngine.Random.Range(-10f, 10f) > 0f)
            {
                replayCamTransform.position = new Vector3(-11f, 2.3f, -8.8f);
                if (UnityEngine.Random.Range(-10f, 10f) > 0f)
                {
                    if (bowlerType == "spin")
                    {
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(-10f, 2.3f, -8.8f), "time", 4, "easetype", "easeInOutSine", "delay", 2));
                    }
                    else if (bowlerType == "fast")
                    {
                        replayCamTransform.position = new Vector3(-11f, 1.3f, -10f);
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(-10f, 2.3f, -8.8f), "time", 3, "easetype", "easeInOutSine", "delay", 0.5f));
                    }
                    else if (bowlerType == "medium")
                    {
                        iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(-10f, 2.3f, -8.8f), "time", 4, "easetype", "easeInOutSine", "delay", 2));
                    }
                }
            }
        }
        else if (bowlerSide == "right")
        {
            replayCamTransform.position = new Vector3(11f, 2.3f, -8.8f);
            if (UnityEngine.Random.Range(-10f, 10f) > 0f)
            {
                if (bowlerType == "spin")
                {
                    iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(10f, 2.3f, -8.8f), "time", 4, "easetype", "easeInOutSine", "delay", 2));
                }
                else if (bowlerType == "fast")
                {
                    replayCamTransform.position = new Vector3(11f, 1.3f, -10f);
                    iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(10f, 2.3f, -8.8f), "time", 3, "easetype", "easeInOutSine", "delay", 0.5f));
                }
                else if (bowlerType == "medium")
                {
                    iTween.MoveTo(replayViewCamera.gameObject, iTween.Hash("position", new Vector3(10f, 2.3f, -8.8f), "time", 4, "easetype", "easeInOutSine", "delay", 2));
                }
            }
        }
        replayViewCamera.gameObject.transform.LookAt(replayControllerTransform);
    }

    public void HideReplay()
    {
        isReplayModeActive = false;
        if (ballTweener != null && ballTweener.IsPlaying())
        {
            ballTweener.Pause();
        }
        ////ConstantsData_M.MpLog("* position change");
        matchBallTransform.position = initialBallPosition;
        temporaryPosition = matchBallTransform.position;
        matchBallTransform.eulerAngles = new Vector3(0f, 2f, 180f);
        Time.timeScale = 1f;
        Singleton<RewindTime>.instance.CanCapture = false;
        Singleton<RewindTime>.instance.StopRewind();
        isBallPaused = false;
        CONTROLLER.cameraType = 1;
        infraredCamera.SetActive(value: false);
        replayCameraController.enabled = false;
        currentActionState = -10;
        replayCameraController.enabled = false;
        if (BowlingBy == "computer" && (runsScoredThisBall == 4 || runsScoredThisBall == 6))
        {
            if (isFieldRestrictionActive)
            {
                SetComputerFieldIndex(UnityEngine.Random.Range(1, 6));
            }
            else
            {
                SetComputerFieldIndex(UnityEngine.Random.Range(6, 10));
            }
        }
        Singleton<GameData>.instance.ReplayCompleted();
        if (CONTROLLER.isFreeHit)
        {
            CONTROLLER.isFreeHit = false;
        }
    }

    private IEnumerator UltraSlowMotion()
    {
        Time.timeScale = 0.03f;
        yield return new WaitForSeconds(0.1f);
        if (isReplayModeActive)
        {
            Time.timeScale = 0.5f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    public void SkipReplay()
    {
        ////ConstantsData_M.MpLog("@$REPLAY SKIP ");
        if (CONTROLLER.reviewReplay)
        {
            return;
        }
        isBallPaused = false;
        CONTROLLER.cameraType = 1;
        infraredCamera.SetActive(value: false);
        rightCamTransform.position = new Vector3(-28f, 7.5f, 0f);
        leftCamTransform.position = new Vector3(28f, 7.5f, 0f);
        iTween.Stop(wicketKeeperObject);
        iTween.Stop(fielder10Object);
        iTween.Stop(replayController);
        iTween.Stop(batsmanObject);
        iTween.Stop(replayViewCamera.gameObject);
        iTween.Stop(umpireViewCamera.gameObject);
        iTween.Stop(gameplayCamera.gameObject);
        if (ballTweener != null && ballTweener.IsPlaying())
        {
            ballTweener.Pause();
        }
        ////ConstantsData_M.MpLog("* position change");
        matchBallTransform.position = initialBallPosition;
        matchBallTransform.eulerAngles = new Vector3(0f, 2f, 180f);
        temporaryPosition = matchBallTransform.position;
        Singleton<RewindTime>.instance.CanCapture = false;
        Singleton<RewindTime>.instance.StopRewind();
        currentWicketKeeperStatus = string.Empty;
        fielderAction = string.Empty;
        for (int i = 0; i < numberOfFielders; i++)
        {
            activeFieldersActions.Add(string.Empty);
            GameObject gameObject = fielders[i + 1];
            gameObject.GetComponent<Animation>().Play("idle");
        }
        keeperAnim.Play("idle");
        bowlerAnimator.Play("Blitz_" + bowlerType + "Idle");
        fielder10Anim.Play("idle");
        HideReplay();
        if (isRunForRunOutFailedPostReplay)
        {
            UpdateRunAfterReplay();
        }
        if (noBallRunStatus == "beatenball")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (noBallRunStatus == "bowlercollectsdotball")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (noBallRunStatus == "wideboundary")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball += 5;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 5, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (noBallRunStatus == "cleanbowled")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, CONTROLLER.StrikerIndex, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (noBallRunStatus == "lbwappeal")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (noBallRunStatus == "keepercollectstheball")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, lineNoBallRunsScored, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (noBallRunStatus == "bowlercollectstheball")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, lineNoBallRunsScored, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (noBallRunStatus == "bowlerrunoutappealsandnotout")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, lineNoBallRunsScored, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else if (noBallRunStatus == "keeperrunoutappealsandnotout")
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
    }

    private void UpdateRunAfterReplay()
    {
        if (isOversteppedDelivery)
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, currentBallRunsSaved, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
            {
            }
        }
        else
        {
            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, currentBallRunsSaved, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
        }
    }

    public void SetDefaultDigitalDisplayContent()
    {
        // Null-safe (reconnect-stuck fix): on a reconnect the GameData NewGame → NewInnings chain runs
        // via a DelayUntil WHILE the scene is still mid-transition ("Changing Scene"), before this
        // GroundController's own init has assigned DigitalScreenRenderer (GetComponent on
        // scoreboardScreen, ~line 2093). The unguarded deref here threw a NullReferenceException that
        // ABORTED the entire NewGame coroutine → NewInnings never finished AND the end-of-NewGame
        // reconnect restore (RestoreMatchStateFromSyncVars) never ran → reconnecting player stuck at
        // 0/0. Self-heal the renderer reference and guard every deref so NewInnings always completes.
        if (DigitalScreenRenderer == null && scoreboardScreen != null)
            DigitalScreenRenderer = scoreboardScreen.GetComponent<Renderer>();
        if (DigitalScreenRenderer != null && DigitalScreenRenderer.material != null
            && digitalBoardTextures != null && digitalBoardTextures.Length > 0 && digitalBoardTextures[0] != null)
            DigitalScreenRenderer.material.mainTexture = digitalBoardTextures[0];
        if (scoreboardScreen != null)
        {
            // If init hasn't captured the real scale yet, take it now so we don't collapse the board to 0.
            if (scoreboardScreenScale == Vector3.zero)
                scoreboardScreenScale = scoreboardScreen.transform.localScale;
            scoreboardScreen.transform.localScale = scoreboardScreenScale;
        }
    }

    private void showMainUmpireForNoBallAction()
    {
        showPreviewCamera(status: false);
        //mainUmpireTransform.localScale = new Vector3(1f, 1f, 1f);
        _stayStartTime = Time.time;
        gameplayCamera.enabled = false;
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        closeUpViewCamera.enabled = false;
        umpireViewCamera.enabled = true;
        umpireCamTransform.position = mainUmpireInitialPosition;
        umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 2f, umpireCamTransform.position.z);
        umpireCamTransform.position += new Vector3(0f, 0f, 3f);
        umpireCamTransform.eulerAngles = new Vector3(umpireCamTransform.eulerAngles.x, 180f, umpireCamTransform.eulerAngles.z);
        umpireCamTransform.eulerAngles = new Vector3(5 + UnityEngine.Random.Range(0, 5), umpireCamTransform.eulerAngles.y, umpireCamTransform.eulerAngles.z);
        Singleton<Scoreboard>.instance.Hide(boolean: true);
        Singleton<PreviewScreen>.instance.Hide(boolean: true);
        Singleton<BowlingControls>.instance.Hide(boolean: true);
        Singleton<BattingControls>.instance.Hide(boolean: true);
        Singleton<PauseGameScreen>.instance.Hide(boolean: true);
    }

    private void SaveEdgePosition()
    {
        if (!isEdgePositionSaved && isShotExecuted)
        {
            SetFrames();
            midFramePoint = (startingFramePoint + endingFramePoint) / 2f;
            if (batsmanAnim[currentBatsmanAnimation].time >= midFramePoint)
            {
                //edgeRefs.position = new Vector3(edgeRefs.position.x, ball.transform.position.y, edgeRefs.position.z);
                edgeReferences.position = new Vector3(edgeReferences.position.x, temporaryPosition.y, edgeReferences.position.z);
                savedEdgePositionValue = edgeReferences.position;
                hotspotReferences[0].transform.position = new Vector3(hotspotReferences[0].transform.position.x, hotspotReferences[0].transform.position.y, savedEdgePositionValue.z - 0.25f);
                hotspotReferences[1].transform.position = new Vector3(hotspotReferences[1].transform.position.x, hotspotReferences[1].transform.position.y, savedEdgePositionValue.z + 0.25f);
                isEdgePositionSaved = true;
            }
        }
    }

    private void PlaceUltraEdgeCam()
    {
        if (batsmanHand == "right")
        {
            snickoMeterObject.transform.localPosition = new Vector3(0f - snickoTransformPosition.x, snickoTransformPosition.y, snickoTransformPosition.z);
            infraredCamera.transform.position = ultraEdgeViewPos;
            infraredCamera.transform.eulerAngles = ultraEdgeViewRot;
            sideCamera.transform.position = replaySideCamPos;
            sideCamera.transform.eulerAngles = replaySideCamRot;
        }
        else
        {
            snickoMeterObject.transform.localPosition = snickoTransformPosition;
            infraredCamera.transform.position = new Vector3(0f - ultraEdgeViewPos.x, ultraEdgeViewPos.y, ultraEdgeViewPos.z);
            infraredCamera.transform.eulerAngles = new Vector3(ultraEdgeViewRot.x, 0f - ultraEdgeViewRot.y, ultraEdgeViewRot.z);
            sideCamera.transform.position = new Vector3(0f - replaySideCamPos.x, replaySideCamPos.y, replaySideCamPos.z);
            sideCamera.transform.eulerAngles = new Vector3(replaySideCamRot.x, 0f - replaySideCamRot.y, replaySideCamRot.z);
        }
    }

    private void SetUltraEdgeDecision()
    {
        canUserAskForReview = false;
        canAIAskForReview = false;
        Singleton<BattingControls>.instance.Hide(boolean: true);

        if (!(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex))
        {
            // Out/Not-out desync fix: previously umpireInitialDecision (out/notout) and hasEdgeOccurred
            // were INDEPENDENT random rolls, so a caught-behind "out" WITHOUT an edge (impossible) was
            // produced and broadcast — one screen then showed OUT from the umpire decision while the
            // authoritative ball outcome (derived from the edge) was wicket=0 (NOT OUT) → one player OUT,
            // the other NOT OUT. Roll the EDGE first, then allow "out" ONLY when an edge actually occurred
            // so the decision is internally consistent and matches the synced ball outcome on both clients.
            hasEdgeOccurred = (UnityEngine.Random.Range(1, 101) <= edgeProbabilityChance);
            if (hasEdgeOccurred && UnityEngine.Random.Range(1, 101) <= umpireDecisionChance)
            {
                umpireInitialDecision = "out";
            }
            else
            {
                umpireInitialDecision = "notout";
            }
            hasUltraEdgeDecision = true;
        }

        // BATTING client owns the decision: broadcast the source facts (decision + edged) to the
        // BOWLING client so both apply the SAME caught-behind outcome instead of a stale/empty value.
        // Broadcast on EVERY invocation (not once): this method can run more than once per delivery
        // (BallTiming + MultiPlayerBallAngle paths) and the RNG block re-randomizes each time, so a
        // "send once" guard could leave bowling stuck on a value batting later changed. The Mirror
        // channel is reliable+ordered, so bowling converges on batting's final value. This mirrors the
        // original Photon design which relayed inside this method every call (RpcTarget.OthersBuffered).
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null)
            {
                CricketNetworkManager.instance.CmdSetUltraEdgeDecision(staticVariables.UserProfiledata.user._id, umpireInitialDecision, hasEdgeOccurred);
            }
        }

        RecomputeDRSAvailability();
    }

    // Recomputes DRS review availability from the (possibly synced) umpireInitialDecision and the
    // LOCAL team perspective. "user" vs "AI/opponent" is client-relative, so these bools are never
    // synced over the network — each client derives them from the shared source fact (the decision).
    private void RecomputeDRSAvailability()
    {
        canUserAskForReview = false;
        canAIAskForReview = false;

        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
        {
            if (umpireInitialDecision == "out")
            {
                if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].noofDRSLeft > 0)
                {
                    canUserAskForReview = true;
                }
            }
            else if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].noofDRSLeft > 0)
            {
                canAIAskForReview = true;
            }
        }
        else if (umpireInitialDecision == "out")
        {
            if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].noofDRSLeft > 0)
            {
                canAIAskForReview = true;
            }
        }
        else if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].noofDRSLeft > 0)
        {
            canUserAskForReview = true;
        }
    }

    //Photon Removal  [PunRPC]  — now driven by Mirror CmdSetUltraEdgeDecision/RpcSetUltraEdgeDecision
    public void RPC_SetUltraEdgeDecision(string umpireInitialDecision, bool IsEdged)
    {
        this.umpireInitialDecision = umpireInitialDecision;
        hasEdgeOccurred = IsEdged;
        hasUltraEdgeDecision = true;
        // DRS availability is client-perspective; recompute it locally from the synced decision.
        RecomputeDRSAvailability();
    }

    private void CheckForHotspotPosition()
    {
        SaveEdgePosition();
        SaveBallTravelTime();
        // DRS-REPLAY BALL-NOT-TRAVELING FIX: also gate on isShotExecuted. isUltraEdgeCutscenePlaying leaks
        // into the replay as true (set during the live edge, never cleared by ResetAll), so without this
        // the hotspot freeze (isBallPaused + Time.timeScale=0.035f below) fires on frame 1 — before the
        // replay re-bowl — and BowlingBallMovement() then early-returns on isBallPaused so the ball never
        // flies. isShotExecuted is reset false in ReviewReplay() and set true again only when ExecuteTheShot
        // runs during the re-sim, so the cutscene now activates at the correct moment (ball flies first,
        // THEN freezes at the shot/edge frame).
        if (!isReplayModeActive || !isUltraEdgeCutscenePlaying || !isShotExecuted)
        {
            return;
        }
        DebugLogger.PrintWithSize("=============" + RewindTime.CaptureCount + "==============");
        if (batsmanAnim[currentBatsmanAnimation].time >= startingFramePoint)
        {
            gameplayCamera.enabled = false;
            isBallPaused = true;
            Time.timeScale = 0.035f;
            if (isRecordingEnabled)
            {
                if (!isAnimationStarted)
                {
                    SetFrames();
                    batsmanAnim[currentBatsmanAnimation].time = startingFramePoint;
                    SetCurrentBatsmanAnimSpeed(1f);
                    batsmanAnim.Play(currentBatsmanAnimation);
                    isAnimationStarted = true;
                }
                if (batsmanAnim[currentBatsmanAnimation].time >= endingFramePoint)
                {
                    Singleton<RewindTime>.instance.StartRewind();
                    batsmanAnim[currentBatsmanAnimation].speed = -1f;
                    isRecordingEnabled = false;
                }
            }
            SetBallPositionForCutscene();
            CheckForEdge();
            BallMovementCustom();
            if (!hasWaveTweenPlayed)
            {
                DG.Tweening.Sequence s = DOTween.Sequence();
                s.Insert(0f, waveImage.transform.DOLocalMoveX(-94f, 0f));
                s.Insert(0.001f, waveImage.transform.DOLocalMoveX(-800f, 2f));
                hasWaveTweenPlayed = true;
            }
            ShowUltraEdgeCam();
        }
        if (RewindTime.CaptureCount > 4)
        {
            ShowUmpireAnim();
            ResetEdgeDetectionVariables();
            if ((!hasEdgeOccurred && umpireInitialDecision == "notout") || (hasEdgeOccurred && umpireInitialDecision == "out"))
            {
                Invoke("EndCutScene", 1f);
            }
            else
            {
                Invoke("EndCutScene", 4f);
            }
        }
    }

    private void SetFrames()
    {
        if (currentShotPlayed == "bt6StraightDrive")
        {
            startingFramePoint = 0.45f;
            endingFramePoint = 0.5f;
        }
    }

    private void ShowUltraEdgeCam(bool canShow = true)
    {
        IEnumerator enumerator = ultraEdgeCamera.transform.GetEnumerator();
        try
        {
            while (enumerator.MoveNext())
            {
                Transform transform = (Transform)enumerator.Current;
                if (transform.GetComponent<Camera>() != null)
                {
                    transform.GetComponent<Camera>().enabled = canShow;
                }
            }
        }
        finally
        {
            IDisposable disposable;
            if ((disposable = enumerator as IDisposable) != null)
            {
                disposable.Dispose();
            }
        }
        ultraEdgeCamera.SetActive(canShow);
    }

    private void SetBallPositionForCutscene()
    {
        if (!isBallPlaced)
        {
            if (batsmanHand == "right")
            {
                minEdgeDistance = 0.1f;
                safeEdgeDistance = 0.13f;
                ballDeviationThreshold = 0.1f;
            }
            else
            {
                minEdgeDistance = -0.1f;
                safeEdgeDistance = -0.13f;
                ballDeviationThreshold = -0.1f;
            }
            float num = 0.3f;
            if (ballSpotLength < 12f)
            {
                num = 0f;
            }
            ballPath1Points[2].transform.parent.eulerAngles = new Vector3(ballPath1Points[2].transform.parent.eulerAngles.x, 90f, ballPath1Points[2].transform.parent.eulerAngles.y);
            hotspotReferences[3].transform.position = new Vector3(savedEdgePositionValue.x - minEdgeDistance, savedEdgePositionValue.y + num, hotspotReferences[1].transform.position.z + 0.1f);
            referencePathObject.transform.position = new Vector3(ballPath1Points[1].transform.position.x, ballPath1Points[1].transform.position.y - 0.3f, matchBall.transform.position.z);
            matchBall.transform.position = new Vector3(ballPath1Points[1].transform.position.x, ballPath1Points[1].transform.position.y, matchBall.transform.position.z);
            if (!hasEdgeOccurred)
            {
                matchBall.transform.position = new Vector3(ultraEdgeImpactImage.transform.position.x - safeEdgeDistance, ultraEdgeImpactImage.transform.position.y, hotspotReferences[0].transform.position.z);
                hotspotReferences[3].transform.localPosition = new Vector3(hotspotReferences[3].transform.localPosition.x - safeEdgeDistance, hotspotReferences[3].transform.localPosition.y, hotspotReferences[3].transform.localPosition.z);
            }
            else
            {
                //ball.transform.position = new Vector3(ultraEdgeImpact.transform.position.x - edgeDistance, ultraEdgeImpact.transform.position.y, hotspotReference[0].transform.position.z);
                if (batsmanHand == "right")
                {
                    temporaryPosition = new Vector3(ultraEdgeImpactImage.transform.position.x - 0.1f, ultraEdgeImpactImage.transform.position.y, ultraEdgeImpactImage.transform.position.z - 0.05f);
                }
                else
                {
                    temporaryPosition = new Vector3(ultraEdgeImpactImage.transform.position.x + 0.1f, ultraEdgeImpactImage.transform.position.y, ultraEdgeImpactImage.transform.position.z - 0.05f);
                }
                matchBall.transform.position = temporaryPosition;
                //ball.transform.position = new Vector3(ultraEdgeImpact.transform.position.x, ultraEdgeImpact.transform.position.y, ultraEdgeImpact.transform.position.z);
                hotspotReferences[3].transform.localPosition = new Vector3(hotspotReferences[3].transform.localPosition.x, hotspotReferences[3].transform.localPosition.y, hotspotReferences[3].transform.localPosition.z);
                ////ConstantsData_M.MpLog("EDGEDDD!! DONE "+ ball.transform.position.x);
            }
            Singleton<RewindTime>.instance.CanCapture = true;
            isBallPlaced = true;
        }
    }

    private void CheckForEdge()
    {
        if (hasEdgeOccurred)
        {
            if (batsmanAnim[currentBatsmanAnimation].time >= 0.48f)
            {
                if (batsmanAnim[currentBatsmanAnimation].time <= 0.485f)
                {
                    if (!isBallMovementChanged && ballTweener != null && isCustomBallMovementActive)
                    {
                        Vector3 vector = new Vector3(ballPath1Points[0].transform.position.x - ballDeviationThreshold, ballPath1Points[0].transform.position.y, hotspotReferences[3].transform.position.z);
                        ballTweener.ChangeEndValue(vector, snapStartValue: true);
                        isBallMovementChanged = true;
                    }
                    waveImage.SetActive(value: false);
                    impactImage.SetActive(value: true);
                    if (RewindTime.CaptureCount == 4 && !isBallPausedAtImpact)
                    {
                        snickoStatusIndicator.sprite = snickoEdgedSprite;
                        StartCoroutine(PauseAtImpact());
                    }
                }
                else
                {
                    waveImage.SetActive(value: true);
                    impactImage.SetActive(value: false);
                }
            }
            else
            {
                waveImage.SetActive(value: true);
                impactImage.SetActive(value: false);
                ultraEdgeImpactImage.SetActive(value: false);
            }
        }
        else if (batsmanAnim[currentBatsmanAnimation].time >= 0.475f && batsmanAnim[currentBatsmanAnimation].time <= 0.48f && RewindTime.CaptureCount == 4 && !isBallPausedAtImpact)
        {
            snickoStatusIndicator.sprite = snickoNotEdgedSprite;
            StartCoroutine(PauseAtImpact());
        }
    }

    private IEnumerator PauseAtImpact()
    {
        Time.timeScale = 0f;
        isUltraEdgeCutscenePlaying = false;
        Singleton<RewindTime>.instance.StopRewind();
        snickoStatusIndicator.DOFade(1f, 0.75f).SetUpdate(isIndependentUpdate: true);
        yield return new WaitForSecondsRealtime(3.5f);
        isBallPausedAtImpact = true;
        isUltraEdgeCutscenePlaying = true;
        RewindTime.CaptureCount++;
    }

    private void EndCutScene()
    {
        isUltraEdgeCutscenePlaying = false;
        gameplayCamera.enabled = true;
        Singleton<Scoreboard>.instance.UpdateScoreCard();
        if (!hasEdgeOccurred)
        {
            showNotOutAnimation = true;
            Singleton<GameData>.instance.UpdateCurrentBall(validBallCount, remainingBallCount, totalRunsScored, extraRuns, currentBatsmanID, 0, 0, bowlerId, catcherId, batsmanOutId, isBoundaryHit);
        }
        else
        {
            Singleton<GameData>.instance.UpdateCurrentBall(validBallCount, remainingBallCount, totalRunsScored, extraRuns, currentBatsmanID, 1, 5, bowlerId, catcherId, batsmanOutId, isBoundaryHit);
        }
        hasEdgeOccurred = false;
    }

    private void ShowUmpireAnim()
    {
        umpireViewCamera.enabled = true;
        umpireCamTransform.position = mainUmpireInitialPosition;
        umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 2f, umpireCamTransform.position.z);
        umpireCamTransform.position += new Vector3(0f, 0f, 3f);
        umpireCamTransform.eulerAngles = new Vector3(5 + UnityEngine.Random.Range(0, 5), 180f, 0f);
        if (!hasEdgeOccurred)
        {
            if (umpireInitialDecision == "notout")
            {
                mainUmpireAnim.Play("NotOut");
            }
            else
            {
                mainUmpireAnim.Play("DRS_SorryNotOut");
            }
        }
        else
        {
            makeFieldersToCelebrate(null);
            if (umpireInitialDecision == "out")
            {
                mainUmpireAnim.Play("Out2_New");
            }
            else
            {
                mainUmpireAnim["DRS_SorryOut"].speed = 0.2f;
                mainUmpireAnim.Play("DRS_SorryOut");
            }
        }
        if ((umpireInitialDecision == "out" && !hasEdgeOccurred) || (umpireInitialDecision == "notout" && hasEdgeOccurred))
        {
            CONTROLLER.TeamList[Singleton<ReviewSystem>.instance.TeamIndex].noofDRSLeft++;
        }
        Singleton<GameData>.instance.PlayGameSound("Cheer");
    }

    private void ResetEdgeDetectionVariables()
    {
        currentActionState = 20;
        umpireDecisionChance = 50;
        edgeProbabilityChance = 50;
        ultraEdgeImpactImage.SetActive(value: false);
        CONTROLLER.canShowReplay = false;
        CONTROLLER.reviewReplay = false;
        CONTROLLER.ReplayShowing = false;
        RewindTime.CaptureCount = 0;
        infraredCamera.SetActive(value: false);
        ShowUltraEdgeCam(canShow: false);
        hasWaveTweenPlayed = false;
        waveImage.SetActive(value: true);
        isRecordingEnabled = true;
        impactImage.SetActive(value: false);
        isPositionSaved = false;
        if (ballTweener != null && ballTweener.IsPlaying())
        {
            ballTweener.Pause();
        }
        isAnimationStarted = false;
        CONTROLLER.cameraType = 1;
        ////ConstantsData_M.MpLog("* position change");
        matchBallTransform.position = initialBallPosition;
        matchBallTransform.eulerAngles = new Vector3(0f, 2f, 180f);
        temporaryPosition = matchBallTransform.position;
        Time.timeScale = 1f;
        Singleton<RewindTime>.instance.CanCapture = false;
        Singleton<RewindTime>.instance.StopRewind();
        isBallPlaced = false;
        isShotExecuted = false;
        totalElapsedTime = 0f;
        isHardcoded = false;
        snickoStatusIndicator.DOFade(0f, 0f).SetUpdate(isIndependentUpdate: true);
        isEdgeCaught = false;
        showNotOutAnimation = false;
        timeRequiredToTravel = 0f;
        isReplayModeActive = false;
        CONTROLLER.EDGECATCH = false;
        isCustomBallMovementActive = false;
        isBallMovementChanged = false;
        hasUmpireAnimationPlayed = false;
        isBallTimeSaved = false;
        isEdgePositionSaved = false;
        isBallPausedAtImpact = false;
        for (int i = 0; i < 4; i++)
        {
            hotspotReferences[i].transform.localPosition = defaultHotspotPositions[i];
        }
        for (int j = 0; j < 4; j++)
        {
            ballPath1Points[j].transform.localPosition = defaultBallPathPoints[j];
        }
        Singleton<GameData>.instance.ReplayCompleted();
        Singleton<GameData>.instance.actionStatusText.gameObject.SetActive(value: false);
        HideReplay();
        edgeReferences.localPosition = savedEdgeTransformValue;
    }

    //Photon Removal[PunRPC]
    public void RPC_UpdateReviewDecision(bool TookReview)
    {

    }

    public void UpdateReviewDecision(bool TookReview)
    {
        if (GameConstants.isWithAI == false)
        {
            //    view.RPC("RPC_UpdateReviewDecision", RpcTarget.AllBuffered, TookReview);                                                                                                                    //Photon Removal
        }


    }

    public void EdgeOut()
    {
        edgeProbabilityChance = 105;
    }

    public void EdgeNotOut()
    {
        edgeProbabilityChance = -1;
    }

    public void UmpireOut()
    {
        umpireDecisionChance = 105;
    }

    public void UmpireNotOut()
    {
        umpireDecisionChance = -1;
    }

    public void DrsHardcode()
    {
        isDRSHardcoded = true;
    }

}
