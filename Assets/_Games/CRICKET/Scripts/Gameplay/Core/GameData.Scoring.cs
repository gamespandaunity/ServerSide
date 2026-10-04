// ════════════════════════════════════════════════════════════════════════════════════════════
// GameData.Scoring — BALL ACCOUNTING + MATCH SCORING (extracted from GameData, Phase 3).
// UpdateCurrentBall is the heart: every delivery outcome (runs/extras/wicket/boundary) commits here
// (online: the batting authority commits and relays; the follower applies via
// UpdateCurrentBallFromNetwork). Over/innings advancement: UpdateOverCompleteResult /
// UpdateBallCompleteResult / CurrentBallUpdate / AutoplayCheckForOverComplete. LBW online commits at
// the APPEAL MARK (see GroundController.ReviewReplay). Celebrations/century checks ride along.
// ════════════════════════════════════════════════════════════════════════════════════════════
using CodeStage.AntiCheat.ObscuredTypes;
using Cricket;
using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;

public partial class GameData
{
	/// <summary>
	/// Called ONLY from CricketNetworkManager.RpcBallOutcome on the bowling player.
	/// Sets the network-call flag before forwarding to UpdateCurrentBall so the bowling
	/// player's suppression gate knows this is the authoritative call and lets it through.
	/// </summary>
	public void UpdateCurrentBallFromNetwork(int validBall, int canCountBall, int runsScored,
		int extraRun, int batsmanID, int isWicket, int wicketType, int bowlerID,
		int catcherID, int batsmanOut, bool isBoundary)
	{
		_ballOutcomeIsFromNetwork = true;
		UpdateCurrentBall(validBall, canCountBall, runsScored, extraRun,
			batsmanID, isWicket, wicketType, bowlerID, catcherID, batsmanOut, isBoundary);
		_ballOutcomeIsFromNetwork = false;
	}

	private void scoreMoreThanCentury(int batsmanId)
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
		}
		else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].BatsmanList.RunsScored >= 100 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].BatsmanList.RunsScored % 50 >= 0 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].BatsmanList.RunsScored % 50 < 6 && CONTROLLER.PlayModeSelected != 2 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].reachedCentury)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].scoredHundredPlus = true;
		}
		else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].BatsmanList.RunsScored >= 100 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].BatsmanList.RunsScored % 50 >= 10 && CONTROLLER.PlayModeSelected != 2 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].scoredHundredPlus)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanId].scoredHundredPlus = false;
		}
	}

	public void UpdateCurrentBall(int validBall, int canCountBall, int runsScored, int extraRun, int batsmanID, int isWicket, int wicketType, int bowlerID, int catcherID, int batsmanOut, bool isBoundary)
	{
		if (CONTROLLER.PlayModeSelected == 8)
		{
			totalRunsScored = runsScored;
			extraRuns = extraRun;
			wasWicket = isWicket;
		}

		// Caught-behind bowling-record fix: the AUTHORITATIVE RpcBallOutcome arrives via
		// UpdateCurrentBallFromNetwork (_ballOutcomeIsFromNetwork=true) and was being SWALLOWED by this
		// UltraEdge defer block (evaluated before the bowling-suppress gate) → the bowling follower never
		// recorded a reviewed caught-behind wicket. !_ballOutcomeIsFromNetwork lets authoritative calls fall
		// through to the bowling-suppress/record path; the bowling client's OWN local keeper decisionPending
		// call (_ballOutcomeIsFromNetwork=false) still defers to show its review panel correctly.
		// Online, the UltraEdge (caught-behind) review must follow the SAME rule as the main DRS path:
		// ConstantsData_M.enableOnlineReview is off, so the umpire's on-field decision stands and no review
		// panel opens. This block was never brought under that switch, and because of the
		// !_ballOutcomeIsFromNetwork term it could only ever fire on ONE side: the keeper's decision is
		// simulated by the BOWLING client, so its own local call reaches here and shows the panel, while the
		// batting client receives the same outcome through RpcBallOutcome with _ballOutcomeIsFromNetwork
		// true and is filtered out. That is the tester's "bowler side review aata hai, batsman side nahi".
		// Falling through instead of deferring records the outcome immediately, which is the same resolution
		// the DECLINE button produces. Offline and vs-AI keep the full review.
		if (groundController.isUltraEdgeCutscenePlaying && CONTROLLER.PlayModeSelected == 8
			&& GameConstants.isWithAI == false && !ConstantsData_M.enableOnlineReview)
		{
			ConstantsData_M.MpLog("[Review] Online UltraEdge review DISABLED — umpire's on-field decision stands, no panel (kept symmetric with the main DRS path).");
		}
		else if (groundController.isUltraEdgeCutscenePlaying && !_ballOutcomeIsFromNetwork && !groundController.getOverStepBall() && !groundController.isLineFreeHitActive && (groundController.canUserAskForReview || groundController.canAIAskForReview) && groundController.hasUmpireAnimationPlayed)
		{
			groundController.validBallCount = validBall;
			groundController.remainingBallCount = canCountBall;
			groundController.totalRunsScored = runsScored;
			groundController.extraRuns = extraRun;
			groundController.currentBatsmanID = batsmanID;
			groundController.isWicketTaken = isWicket;
			groundController.typeOfWicket = wicketType;
			// wicketType 3 == run out. Make the umpire's signal agree with what the score just recorded, on
			// whichever side got it wrong — this runs on the batting side as it commits and on the bowling side
			// as RpcBallOutcome applies, so both end up showing the same decision.
			groundController.ReconcileRunOutSignalWithOutcome(isWicket == 1 && wicketType == 3);
			groundController.bowlerId = bowlerID;
			groundController.catcherId = catcherID;
			groundController.batsmanOutId = batsmanOut;
			groundController.isBoundaryHit = isBoundary;
			Time.timeScale = 0.5f;
			Singleton<ReviewSystem>.instance.ShowUltraEdgeReview();
			Singleton<ReviewSystem>.instance.StartTimer();
			return;
		}
		hasShownRv = false;
		EnableRun(boolean: false);
		rvRunsScored = runsScored;
		lastBowledBallSummary = groundController.lastBowledBallType;
		//if (CONTROLLER.PlayModeSelected != 6 && Singleton<AdIntegrate>.instance.isRewardedVideoAvailable && Singleton<AdIntegrate>.instance.CheckForInternet())
		if (CONTROLLER.PlayModeSelected != 6)
		{
			isBallRebowled = false;
			//       
		}
		if (validBall == 1 && !_ballOutcomeIsFromNetwork && Singleton<BallSimulationManager>.instance.CanShowBallSimulation())
		{
			string data = ((isWicket == 1) ? "wicket" : ((runsScored > 4) ? "six" : ((runsScored > 3) ? "four" : ((runsScored <= 0) ? "dot" : "run"))));
			Singleton<BallSimulationManager>.instance.SetBallSimulationData(data, groundController.bowlerType, groundController.bowlerSide, groundController.bowlerHand);
		}
		bool overStepBall = groundController.getOverStepBall();
		bool flag = Singleton<GroundController>.instance.IsWide();
		bool flag2 = true;

		Singleton<Scoreboard>.instance.HideStrip(boolean: true);
		bool flag3 = false;
		if (hasGameQuit)
		{
			return;
		}

		// ─── MULTIPLAYER BALL-OUTCOME AUTHORITY ───────────────────────────────────────
		// Placed HERE — after ALL early returns (UltraEdge, hasGameQuit) have passed.
		// This guarantees CmdBallOutcome is sent exactly once per processed delivery, and
		// the bowling player's suppression only fires when the ball is actually going to
		// be counted (not on UltraEdge deferred-processing returns).
		//
		// _ballOutcomeIsFromNetwork is set to true ONLY by UpdateCurrentBallFromNetwork(),
		// which is the entry point called by RpcBallOutcome. This makes the check
		// completely unambiguous regardless of RPC vs local-physics call ordering.
		//
		// BATTING PLAYER  : sends CmdBallOutcome → continues processing normally.
		// BOWLING PLAYER  : local physics call → _ballOutcomeIsFromNetwork is false → return.
		//                   RpcBallOutcome calls UpdateCurrentBallFromNetwork() which sets
		//                   _ballOutcomeIsFromNetwork=true → this check lets it through.
		if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null
			&& CONTROLLER.PlayModeSelected == 8)
		{
			if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
			{
				if (!_ballOutcomeIsFromNetwork)
				{
					// Local physics call on bowling side — suppress it entirely.
					// UpdateCurrentBallFromNetwork() will re-enter with the batting
					// player's authoritative params after RpcBallOutcome arrives.
					Debug.Log("[GameData][UpdateCurrentBall] Bowling: suppressing local physics, waiting for authoritative outcome via RpcBallOutcome.");
					// Backstop (B): arm the outcome watchdog so a never-arriving RpcBallOutcome (e.g. a
					// reviewed caught-behind whose send was lost) can't freeze the bowling client forever.
					if (groundController != null) groundController.StartMpOutcomeWatchdog();
					return;
				}
				// _ballOutcomeIsFromNetwork == true: call is from RpcBallOutcome, process it.
				Debug.Log("[GameData][UpdateCurrentBall] Bowling: processing authoritative outcome from batting side.");
				// Force-correct a follower divergence: a CAUGHT shot (wicketType==3) that our local sim
				// ground-fielded because the catch-fielder sync landed after the ball already bounced near the
				// fielder. Keeps the ball from lying on the ground + the camera from chasing the throw-back.
				// No-ops unless an outfield catcher was synced AND our local sim didn't already catch it.
				// wicketType==3 = outfield catch; wicketType==4 = run-out (different case, not caught).
				if (isWicket == 1 && wicketType == 3 && groundController != null)
				{
					groundController.ForceFollowerCatchVisualOnDivergence();
				}
			}
			else if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && !_ballOutcomeIsFromNetwork)
			{
				// Send exact raw params to server so bowling player gets authoritative values.
				// !_ballOutcomeIsFromNetwork guard prevents double-send if this path were
				// ever re-entered via UpdateCurrentBallFromNetwork (defensive coding).
				CricketNetworkManager.instance.CmdBallOutcome(
					staticVariables.UserProfiledata.user._id,
					validBall, canCountBall, runsScored, extraRun,
					batsmanID, isWicket, wicketType, bowlerID, catcherID, batsmanOut, isBoundary);
				// Ship the free-hit verdict with the outcome. Both sides derive isLineFreeHitActive from
				// their own local ball handling, which is how one client finished a free-hit delivery on
				// 'lineball' and the other on 'overstep' and their bowlers then behaved differently.
				if (groundController != null)
					CricketNetworkManager.instance.CmdSyncFreeHitState(
						staticVariables.UserProfiledata.user._id,
						groundController.isLineFreeHitActive, groundController.lastBowledBallType);
			}
		}
		// ─────────────────────────────────────────────────────────────────────────────

		// Orphan-ball discriminator (mid-delivery reconnect fix): we are now PAST the bowling-follower
		// suppression return (~line 1629) and every early-return (UltraEdge, hasGameQuit), so THIS delivery's
		// outcome is genuinely committing on this client — the batting authority sent CmdBallOutcome, or the
		// follower is processing the authoritative RpcBallOutcome. Stamp the ground controller so a
		// mid-delivery reconnect re-arm can tell this resolved ball from an ORPHAN (released, batter
		// disconnected pre-shot, outcome never arrived) and never double-bowls it. Cleared by ResetAll/new delivery.
		if (groundController != null) groundController.MarkOutcomeCommittedThisDelivery();

		if (CONTROLLER.isConfidenceLevel || CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			IncreaseConfidenceLevel(runsScored, validBall);
		}
		if (Singleton<AIFieldingSetupManager>.instance.enabled && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && runsScored > 2)
		{
			Singleton<AIFieldingSetupManager>.instance.SaveStrikerHittingDirections();
		}
		Singleton<UILookAt>.instance.show(flag: false);
		if (validBall == 1)
		{
			isBallValid = true;
			currentBallNumber++;
			// A FREE HIT LASTS EXACTLY ONE LEGAL DELIVERY. This is the only place that knows a legal ball has
			// been bowled, so it is the only place the rule can be enforced once for every outcome.
			//
			// The flags are cleared in roughly forty separate outcome branches — boundary, wicket, several
			// keeper paths — and a plain DOT has no such branch, so a free hit played for no run stayed
			// armed and the batsman got another one (tester: "free hit py 0 score ho to again free hit mil
			// rhi hai"). Clearing per-outcome cannot be made complete; clearing per legal ball can.
			//
			// A no-ball passes validBall == 0, so the free hit it just awarded is not cleared here — only the
			// legal delivery that follows clears it, which is the rule. The existing per-outcome clears stay:
			// they run earlier for their own reasons and are harmless once this is authoritative.
			if (groundController != null && (groundController.freeHit || CONTROLLER.isFreeHit))
			{
				ConstantsData_M.MpLog($"[GameData][UpdateCurrentBall] Free hit consumed by this legal delivery (runs={runsScored}, wicket={isWicket}).");
				// Clears isFreeHitActive as well as freeHit — the snapshot reads isFreeHitActive first, so
				// clearing only freeHit here left every later snapshot carrying freeHitActive=true and the
				// next reconnect re-armed the consumed free hit. See ClearFreeHitAfterLegalDelivery.
				groundController.ClearFreeHitAfterLegalDelivery();
				CONTROLLER.isFreeHit = false;
				if (Singleton<Scoreboard>.instance != null) Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
			}
			// ─── Reconnect 0/0 fix (G2/#7b): EARLY one-shot-per-ball authoritative progress push ───
			// The only innings-start writer of syncedCurrentInnings/syncedBallNumber is the NewInnings
			// CmdSyncMatchProgress (~822) — a single fire-and-forget [Command]. If the BATTING client drops
			// before it is delivered, those SyncVars stay at -1 and RestoreMatchStateFromSyncVars
			// early-returns → a reconnect restores 0/0. Re-assert the progress snapshot HERE at ball-START
			// so syncedCurrentInnings is non-default from the very first ball and refreshed EVERY ball,
			// making the restore early-return impossible while real progress exists. Sent BEFORE the
			// currentMatchBalls++ below, so balls stays CONSISTENT with the pre-this-ball committed score
			// (never advances balls past the score → no premature innings-complete on the last ball).
			// Single batting writer only (same Cmd + Mathf.Max-guarded restore) → no second Max-writer, no
			// double-count, no within-over counters touched. Overwritten ball-END by the post-ball push.
			if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
				&& CricketNetworkManager.instance != null
				&& CONTROLLER.TeamList != null && CONTROLLER.TeamList.Length >= 2
				&& CONTROLLER.BattingTeamIndex >= 0 && CONTROLLER.BowlingTeamIndex >= 0
				&& CONTROLLER.BattingTeamIndex < CONTROLLER.TeamList.Length
				&& CONTROLLER.BowlingTeamIndex < CONTROLLER.TeamList.Length
				&& CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
				&& (Launcher.Instance == null || !Launcher.Instance.IsReConnecting()))
			{
				CricketNetworkManager.instance.CmdSyncMatchProgress(
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls,
					CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores, CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchWickets, CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchBalls,
					CONTROLLER.currentInnings, CONTROLLER.BattingTeamIndex, CONTROLLER.BowlingTeamIndex);
				CricketNetworkManager.instance.PushMatchSnapshotIfBatting();  // Phase 0: full-snapshot push (alongside the synced* path)
			}
			// ───────────────────────────────────────────────────────────────────────────────────────
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls++;
			// NOTE: Do NOT call CmdSyncGameState / CmdCorrectBallState here.
			// At this point runsScored hasn't been added to currentMatchScores yet (line ~1522),
			// extraRun hasn't been added (line ~1535), wickets haven't been incremented (WicketBall
			// at line ~1618), and AddBall hasn't updated ballCount (lines ~1462+).
			// The authoritative sync is performed AFTER all state is committed — see below.

			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.BallsBowled++;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[currentBallNumber] = string.Empty + (runsScored + extraRun);
			if (isWicket == 1)
			{
				Singleton<ScoreBoardBallList>.instance.AddBall("W", " ");
			}
			else if (extraRun >= 4 && groundController.getWicketKeeperStatus() == "catchMissed")
			{
				Singleton<ScoreBoardBallList>.instance.AddBall(string.Empty + (runsScored + extraRun), "b");
			}
			else
			{
				Singleton<ScoreBoardBallList>.instance.AddBall(string.Empty + (runsScored + extraRun), " ");
			}
			if (CONTROLLER.myTeamIndex != CONTROLLER.BattingTeamIndex)
			{
			}
		}
		else
		{
			isBallValid = false;
			if (flag && overStepBall)
			{
				if (isWicket == 1)
				{
					Singleton<ScoreBoardBallList>.instance.AddBall(string.Empty + "W", string.Empty + "nb");
				}
				else
				{
					Singleton<ScoreBoardBallList>.instance.AddBall(string.Empty + (extraRun - 1), string.Empty + "nb");
				}
			}
			else if (overStepBall)
			{
				if (isWicket == 1)
				{
					Singleton<ScoreBoardBallList>.instance.AddBall(string.Empty + "W", string.Empty + "nb");
				}
				else
				{
					Singleton<ScoreBoardBallList>.instance.AddBall(string.Empty + runsScored, string.Empty + "nb");
				}
			}
			else if (flag)
			{
				if (isWicket == 1)
				{
					Singleton<ScoreBoardBallList>.instance.AddBall(string.Empty + "W", string.Empty + "wd");
				}
				else
				{
					Singleton<ScoreBoardBallList>.instance.AddBall(string.Empty + (extraRun - 1), string.Empty + "wd");
				}
			}
		}
		if (canCountBall == 1)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].BatsmanList.BallsPlayed++;
		}
		CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.RunsGiven += runsScored;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores += runsScored;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].BatsmanList.RunsScored += runsScored;
		if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].BatsmanList.RunsScored >= 100 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].BatsmanList.RunsScored % 50 >= 10 && CONTROLLER.PlayModeSelected != 2 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].scoredHundredPlus)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].scoredHundredPlus = false;
		}

		if (validBall == 1 || overStepBall)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].BatsmanList.currPatnerShipRuns += runsScored;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].BatsmanList.currPatnerShipBalls++;
		}
		CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.RunsGiven += extraRun;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores += extraRun;

		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchExtras += extraRun;
		runsInCurrentOver += runsScored;
		runsInCurrentOver += extraRun;
		if (runsScored == 0 && extraRun == 0)
		{
			if (isBallValid)
			{
				CONTROLLER.continousSixes = 0;
				CONTROLLER.continousBoundaries = 0;
			}
			if (isWicket == 0)
			{
				CONTROLLER.continousWickets = 0;
				if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
				{
					dotBallCount++;
					ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterDot", dotBallCount);
				}
			}
			CONTROLLER.DotInAOver++;
			if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
			{
			}
		}
		if (isWicket == 0)
		{
			CONTROLLER.continousWickets = 0;
		}
		if (extraRun > 0 && canCountBall == 0)
		{
			CONTROLLER.continousWickets = 0;
			CONTROLLER.WideInAOver++;
		}
		scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
		extraRunsDisplayString = LocalizationData.localizationInstance.getText(235) + " " + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchExtras;
		if (CONTROLLER.PlayModeSelected == 6)
		{
		}
		else
		{
			oversDisplayString = GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
		}
		if (isWicket == 1)
		{
			if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
			{
				KitTable.SetKitValues(4);
			}
			if (isBallValid)
			{
				CONTROLLER.continousWickets++;
				CONTROLLER.continousSixes = 0;
				CONTROLLER.continousBoundaries = 0;
			}
			switch (wicketType)
			{
				case 1:
					if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
					{
						bowledWicketCount++;
						ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterWicketBowled", bowledWicketCount);
					}
					break;
				case 3:
				case 5:
					if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
					{
						caughtWicketCount++;
						ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterWicketCatch", caughtWicketCount);
					}
					break;
				case 2:
				case 4:
				case 6:
					if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
					{
						otherWicketCount++;
						ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterWicketOthers", otherWicketCount);
					}
					break;
			}
			WicketBall(validBall, batsmanID, wicketType, bowlerID, catcherID, batsmanOut);
		}
		else if (isBoundary)
		{
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.WicketsInBallCount = 0;
			flag3 = ((CONTROLLER.PlayModeSelected == 7) ? checkForCelebration(runsScored, batsmanID) : checkForCelebration(runsScored, batsmanID));
			if (CONTROLLER.currentInnings < 2)
			{
				Singleton<BattingScoreCard>.instance.SelectedInnings = 1;
			}
			else
			{
				Singleton<BattingScoreCard>.instance.SelectedInnings = 2;
			}
			Singleton<BattingScoreCard>.instance.UpdateScoreCard();
			Singleton<BowlingScoreCard>.instance.UpdateScoreCard();
			switch (runsScored)
			{
				case 4:
					CONTROLLER.totalFours++;
					CONTROLLER.continousSixes = 0;
					CONTROLLER.continousBoundaries++;
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						KitTable.SetKitValues(3);
						boundaryFoursCount++;
						ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterFour", boundaryFoursCount);
					}
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].BatsmanList.Fours++;

					if (!flag3 && !overStepBall)
					{
						InitAnimation(0);
					}
					break;
				case 6:
					CONTROLLER.totalSixes++;
					CONTROLLER.continousSixes++;
					CONTROLLER.continousBoundaries = 0;
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						KitTable.SetKitValues(2);
						boundarySixesCount++;
						int num = (ObscuredPrefs.HasKey("ArcadeSixes") ? ObscuredPrefs.GetInt("ArcadeSixes") : 0);
						num++;
						ObscuredPrefs.SetInt("ArcadeSixes", num);
						SavePlayerPrefs.SaveSixCount();
						ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterSix", boundarySixesCount);
					}
					else if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex)
					{
						opponentSixesCounter++;
						ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterOppSix", opponentSixesCounter);
					}
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanID].BatsmanList.Sixes++;

					if (!flag3 && !overStepBall)
					{
						InitAnimation(1);
					}
					break;
			}
			Singleton<Scoreboard>.instance.UpdateScoreCard();
		}
		else
		{
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.WicketsInBallCount = 0;
			if (CONTROLLER.currentInnings < 2)
			{
				Singleton<BattingScoreCard>.instance.SelectedInnings = 1;
			}
			else
			{
				Singleton<BattingScoreCard>.instance.SelectedInnings = 2;
			}
			Singleton<BattingScoreCard>.instance.UpdateScoreCard();
			Singleton<BowlingScoreCard>.instance.UpdateScoreCard();
			if (runsScored == 1 || runsScored == 3)
			{
				int strikerIndex = CONTROLLER.StrikerIndex;
				CONTROLLER.StrikerIndex = CONTROLLER.NonStrikerIndex;
				CONTROLLER.NonStrikerIndex = strikerIndex;
			}
			flag2 = false;
			Singleton<Scoreboard>.instance.UpdateScoreCard();
		}
		// ─── Multiplayer: persist final committed state to SyncVars (reconnect support) ─
		// All state is now fully committed:
		//   • currentMatchScores includes runsScored + extraRun
		//   • currentMatchWickets is incremented (WicketBall already called)
		//   • ballCount updated (AddBall already called)
		//   • StrikerIndex/NonStrikerIndex swapped for odd runs
		// CmdCorrectBallState writes authoritative post-ball state to SyncVars so that
		// any reconnecting client gets the correct values via Mirror auto-sync.
		// (Live bowling-side correction is handled by CmdBallOutcome → RpcBallOutcome
		//  which is called at the TOP of UpdateCurrentBall before any state is mutated.)
		if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null
			&& CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
		{
			CricketNetworkManager.instance.CmdCorrectBallState(
				staticVariables.UserProfiledata.user._id,
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores,
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets,
				Singleton<ScoreBoardBallList>.instance.ballCount,
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls,
				currentBallNumber,
				CONTROLLER.StrikerIndex,
				CONTROLLER.NonStrikerIndex,
				newBatsmanEntryIndex);

			// ...and the strip ITSELF, not just its count. CmdCorrectBallState carries ballCount, which cannot
			// rebuild chips the bowling client never created. See CmdSyncBallStrip.
			var _sbl = Singleton<ScoreBoardBallList>.instance;
			if (_sbl != null && _sbl.ballList != null && CricketNetworkManager.ReadyToSend)
				CricketNetworkManager.instance.CmdSyncBallStrip(
					staticVariables.UserProfiledata.user._id,
					string.Join("|", _sbl.ballList),
					(_sbl.extras != null) ? string.Join("|", _sbl.extras) : "");

			// Reconnect role/score fix (#6/#7): push the FULL snapshot (innings + BOTH teams'
			// totals + batting/bowling indices) PER BALL too — not only at the over-boundary ACK.
			// CmdCorrectBallState above keeps score/wickets/balls current but does NOT carry the
			// innings or team indices; those live ONLY in CmdSyncMatchProgress. Without a per-ball
			// push, a reconnect early in the 2nd innings (before the first over completes) restored
			// STALE 1st-innings team indices → batting/bowling roles swapped and the live score read
			// the wrong team (showed 0). Same args/signature as the existing over-boundary call.
			if (CONTROLLER.TeamList.Length >= 2)
			{
				CricketNetworkManager.instance.CmdSyncMatchProgress(
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls,
					CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores, CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchWickets, CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchBalls,
					CONTROLLER.currentInnings, CONTROLLER.BattingTeamIndex, CONTROLLER.BowlingTeamIndex);

				// Per-batsman score sync (reconnect scorecard 0/0 fix): push the on-strike + non-strike
				// batsmen's individual runs/balls too — only team totals + indices were synced before,
				// so a reconnecting player's batting card showed those two batsmen at 0/0.
				var _bsPl = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList;
				if (_bsPl != null
					&& CONTROLLER.StrikerIndex >= 0 && CONTROLLER.StrikerIndex < _bsPl.Length
					&& CONTROLLER.NonStrikerIndex >= 0 && CONTROLLER.NonStrikerIndex < _bsPl.Length)
				{
					CricketNetworkManager.instance.CmdSyncBatsmanScores(
						_bsPl[CONTROLLER.StrikerIndex].BatsmanList.RunsScored,
						_bsPl[CONTROLLER.StrikerIndex].BatsmanList.BallsPlayed,
						_bsPl[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored,
						_bsPl[CONTROLLER.NonStrikerIndex].BatsmanList.BallsPlayed);
				}
			}
		}
		if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
		{
			partnershipRunCount += runsScored;
			partnershipRunCount += extraRun;
		}
		AutoSave.ballUpdate = string.Empty;
		for (int i = 0; i < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate.Length; i++)
		{
			AutoSave.ballUpdate += CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[i];
		}
		Singleton<ScoreBoardBallList>.instance.SaveBallList();
		CONTROLLER.isFreeHitBall = groundController.getOverStepBall();
		if (CONTROLLER.isFreeHitBall)
		{
			CONTROLLER.noBallFacedBatsmanId = batsmanID;
		}
		flag3 = ((CONTROLLER.PlayModeSelected == 7) ? checkForCelebration(runsScored, batsmanID) : checkForCelebration(runsScored, batsmanID));
		if (!flag2)
		{
			if (!flag3 || CONTROLLER.PlayModeSelected == 6)
			{
				CurrentBallUpdate(canCountBall, extraRun);
				CheckForOverComplete();
			}
			else
			{
				StartCoroutine(waitForBatsmanCelebration(runsScored, canCountBall, extraRun));
			}
		}
		else if (!flag3 || CONTROLLER.PlayModeSelected == 6)
		{
			if (!hasShownRv)
			{
				CurrentBallUpdate(canCountBall, extraRun);
			}
			else
			{
				rewardRunCount = canCountBall;
				rewardExtraCount = extraRun;
			}
			if (overStepBall)
			{
				if (CONTROLLER.canShowReplay)
				{
					GameIsOnReplay();
					groundController.ShowReplay();
				}
				else
				{
					ReplayIsNotShown();
				}
			}
		}
		else if (isWicket == 0)
		{
			StartCoroutine(waitForBatsmanCelebration(runsScored, canCountBall, extraRun));
		}
		if (CONTROLLER.PlayModeSelected != 6)
		{
			ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "contwik", CONTROLLER.continousWickets);
			ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "contsix", CONTROLLER.continousSixes);
			ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "contfour", CONTROLLER.continousBoundaries);
			if (CONTROLLER.PlayModeSelected != 7 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls > 0)
			{
				runRate = (float)CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores / (float)(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6);
			}
			CheckIfAchievementMet();
		}
		else
		{
			CONTROLLER.currentMatchBalls = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls;
			CONTROLLER.currentMatchScores = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
			CONTROLLER.currentMatchWickets = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			ScoreBoardMultiPlayer.instance.UpdateScoreCard();
			ScoreBoardMultiPlayer.instance.UpdateMultiplayerBallsLeft();
		}
	}

	private IEnumerator waitForBatsmanCelebration(int noOfRuns, int isValid, int extras)
	{
		bool isOverStepBall = groundController.getOverStepBall();
		if (!CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].reachedHalfCentury && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored >= 50)
		{
			groundController.batsmanCelebration("halfcentury");
		}
		if (!CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].reachedHalfCentury && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored >= 50)
		{
			groundController.batsmanCelebration("halfcentury");
		}
		if (!CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].reachedCentury && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored >= 100)
		{
			groundController.batsmanCelebration("century");
		}
		if (!CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].reachedCentury && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored >= 100)
		{
			groundController.batsmanCelebration("century");
		}
		if (CONTROLLER.StrikerIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored > 149 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored % 50 >= 0 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored % 50 < 6 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].reachedCentury && !CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].scoredHundredPlus)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].scoredHundredPlus = true;
			groundController.batsmanCelebration("century");
		}
		if (CONTROLLER.NonStrikerIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored > 149 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored % 50 >= 0 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored % 50 < 6 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].reachedCentury && !CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].scoredHundredPlus)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].scoredHundredPlus = true;
			groundController.batsmanCelebration("century");
		}
		CurrentBallUpdate(isValid, extras);
		yield return new WaitForSeconds(3f);
		groundController.destroyCelebrationBatsman();
		if (!isOverStepBall)
		{
			switch (noOfRuns)
			{
				case 4:
					if (!isOverStepBall)
					{
						InitAnimation(0);
					}
					break;
				case 6:
					if (!isOverStepBall)
					{
						InitAnimation(1);
					}
					break;
				default:
					CheckForOverComplete();
					break;
			}
		}
		else if (isOverStepBall && (noOfRuns == 4 || noOfRuns == 6))
		{
			if (CONTROLLER.canShowReplay)
			{
				GameIsOnReplay();
				groundController.ShowReplay();
			}
			else
			{
				ReplayIsNotShown();
			}
		}
		else
		{
			CheckForOverComplete();
		}
	}

	private bool checkForCelebration(int runs, int playerID)
	{

		if (!CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].reachedHalfCentury)
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.RunsScored % 50 <= 5 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.RunsScored >= 50)
			{
				return true;
			}
		}
		else if (!CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].reachedCentury)
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.RunsScored % 100 <= 5 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.RunsScored >= 100)
			{
				return true;
			}
		}
		else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.RunsScored % 50 <= 5 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].reachedCentury && !CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].scoredHundredPlus && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.RunsScored > 149)
		{
			return true;
		}
		return false;
	}

	public void WicketBall(int validBall, int batsmanID, int wicketType, int bowlerID, int catcherID, int batsmanOut)
	{
		// DIAGNOSTIC (report ab189747): a catch-wicket outcome processed right after a reconnect NREs
		// somewhere in this method and freezes both players (the wicket never applies, next ball never
		// arms). IL2CPP strips the line, so name every reference this method dereferences to pin the
		// null on the next tester build. Pure logging — no behaviour change; remove once fixed.
		try
		{
			int battingIdx = CONTROLLER.BattingTeamIndex, bowlingIdx = CONTROLLER.BowlingTeamIndex;
			var teamList = CONTROLLER.TeamList;
			var battingTeam = (teamList != null && battingIdx >= 0 && battingIdx < teamList.Length) ? teamList[battingIdx] : null;
			var bowlingTeam = (teamList != null && bowlingIdx >= 0 && bowlingIdx < teamList.Length) ? teamList[bowlingIdx] : null;
			// MpLog (not LogWarning): the in-app bug reporter only captures Log/Error/Exception —
			// warnings never reach tester reports, so the earlier LogWarning diagnostic was invisible.
			ConstantsData_M.MpLog(
				"[WicketBall][Diag] wicketType=" + wicketType + " batsmanOut=" + batsmanOut + " bowlerID=" + bowlerID +
				" catcherID=" + catcherID + " newBatsmanEntryIndex=" + newBatsmanEntryIndex +
				" | TeamList=" + (teamList == null ? "NULL" : teamList.Length.ToString()) +
				" battingIdx=" + battingIdx + " bowlingIdx=" + bowlingIdx +
				" | battingTeam=" + (battingTeam == null ? "NULL" : "ok") +
				" battingPlayerList=" + (battingTeam == null ? "-" : (battingTeam.PlayerList == null ? "NULL" : battingTeam.PlayerList.Length.ToString())) +
				" bowlingTeam=" + (bowlingTeam == null ? "NULL" : "ok") +
				" bowlingPlayerList=" + (bowlingTeam == null ? "-" : (bowlingTeam.PlayerList == null ? "NULL" : bowlingTeam.PlayerList.Length.ToString())) +
				" | groundController=" + (groundController == null ? "NULL" : "ok") +
				" FieldersArray=" + (CONTROLLER.FieldersArray == null ? "NULL" : CONTROLLER.FieldersArray.Length.ToString()));
		}
		catch (System.Exception diagEx) { UnityEngine.Debug.LogWarning("[WicketBall][Diag] logging failed: " + diagEx.Message); }

		// Stuck-after-wicket fix (#7): the wicket resolution chain (OUT graphic → new-batsman
		// cutscene → the per-ball opponent ACK via CheckForOverComplete) runs on a Time.timeScale-
		// SCALED Invoke (2.5s) + Time.time cutscene timers. If a client is at timeScale≈0 when the
		// wicket lands (edge/impact slow-mo, DRS, pause), that chain STALLS → the ACK barrier
		// (OnWaitScreen→2) never completes → BOTH players freeze (only the bowler stays movable
		// because UpdateCanBowlerBowl(false) is batting-side only). Restore normal time here so the
		// wicket sequence — and the ACK it carries — always runs to completion.
		Time.timeScale = 1f;
		////Debug.Log("##WICKET");
		if (CONTROLLER.PlayModeSelected == 8)
		{
			CONTROLLER.LastStrikerIndex = batsmanID;
			CONTROLLER.LastNonStrikerIndex = CONTROLLER.NonStrikerIndex;
			CONTROLLER.LastWicketIndex = batsmanOut;
		}
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8)
		{
			Singleton<GroundController>.instance.UpdateCanBowlerBowl(false);
		}
		bool overStepBall = groundController.getOverStepBall();

		if (rebowlStatus == -1 && CONTROLLER.PlayModeSelected != 7)
		{
			return;
		}
		CONTROLLER.wicketType = wicketType;
		ObscuredPrefs.DeleteKey("partnershipXpGained");
		partnershipRunCount = 0;
		canPauseGameplay = false;
		Singleton<Scoreboard>.instance.HidePause(boolean: true);
		if (newBatsmanEntryIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length - 1)
		{
			newBatsmanEntryIndex++;
		}
		if (newBatsmanEntryIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[newBatsmanEntryIndex].BatsmanList.Status = "not out";

			if (CONTROLLER.StrikerIndex == batsmanOut)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.currPatnerShipRuns = 0;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.currPatnerShipBalls = 0;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.currPatnerShipRuns = 0;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.currPatnerShipBalls = 0;
			}
			else
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.currPatnerShipRuns = 0;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.currPatnerShipBalls = 0;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.currPatnerShipRuns = 0;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.currPatnerShipBalls = 0;
			}
		}
		batsmanDismissedIndex = batsmanOut;
		isWicketBall = true;
		if (validBall == 1)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[currentBallNumber] = "W";
		}
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets++;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.FOW = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
		if (wicketType == 4)
		{
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.WicketsInBallCount = 0;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.Status = "run out";
			if (CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
			{
			}
		}
		else
		{
			wicketsThisOver++;
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.Wicket++;
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.WicketsInBallCount++;
			switch (wicketType)
			{
				case 1:
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.Status = "b " + GetBowlerShortName(bowlerID);
					break;
				case 2:
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.Status = "lbw " + GetBowlerShortName(bowlerID);
					break;
				case 3:
					catcherID = CONTROLLER.FieldersArray[catcherID];
					if (bowlerID == catcherID)
					{
						CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.Status = "c & b " + GetBowlerShortName(bowlerID);
					}
					else
					{
						CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.Status = "ct off b " + GetBowlerShortName(bowlerID);

					}
					break;
				case 5:
					catcherID = CONTROLLER.FieldersArray[catcherID];
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.Status = "ct behind b " + GetBowlerShortName(bowlerID);
					break;
				case 6:
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.Status = "st, b " + GetBowlerShortName(bowlerID);
					break;
			}
		}
		if (wicketType == 4)
		{
			if (catcherID == 0 && batsmanOut == CONTROLLER.StrikerIndex)
			{

				CONTROLLER.StrikerIndex = newBatsmanEntryIndex;
			}
			else if (catcherID == 1 && batsmanOut == CONTROLLER.StrikerIndex)
			{
				CONTROLLER.StrikerIndex = CONTROLLER.NonStrikerIndex;
				CONTROLLER.NonStrikerIndex = newBatsmanEntryIndex;
			}
			else if (catcherID == 0 && batsmanOut == CONTROLLER.NonStrikerIndex)
			{
				CONTROLLER.NonStrikerIndex = CONTROLLER.StrikerIndex;
				CONTROLLER.StrikerIndex = newBatsmanEntryIndex;
			}
			else if (catcherID == 1 && batsmanOut == CONTROLLER.NonStrikerIndex)
			{
				CONTROLLER.NonStrikerIndex = newBatsmanEntryIndex;
			}
		}
		else if (batsmanOut == CONTROLLER.StrikerIndex)
		{
			CONTROLLER.StrikerIndex = newBatsmanEntryIndex;
		}
		else if (batsmanOut == CONTROLLER.NonStrikerIndex)
		{
			CONTROLLER.NonStrikerIndex = newBatsmanEntryIndex;
		}
		if (wicketType == 3)
		{
			float num = groundController.strikerZPos();
			if (num < 0f)
			{
				CONTROLLER.StrikerIndex = CONTROLLER.NonStrikerIndex;
				CONTROLLER.NonStrikerIndex = newBatsmanEntryIndex;
			}
			else
			{
				CONTROLLER.StrikerIndex = newBatsmanEntryIndex;
			}
		}
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			int strikeRate = GetStrikeRate(batsmanOut);
			int runsScored = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.RunsScored;
		}
		if (canPlayAnimation)
		{
			if (CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.WicketsInBallCount % 3 == 0 && CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[bowlerID].BowlerList.WicketsInBallCount > 0)
			{
				InitAnimation(3);
			}
			else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.RunsScored <= 0)
			{
				if (CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex || CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.BallsPlayed != 1 || CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[batsmanOut].BatsmanList.Status != "run out")
				{
				}
				if (!overStepBall)
				{
					InitAnimation(2);
				}
			}
			else if (!overStepBall)
			{
				InitAnimation(2);
			}
		}
		scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
	}

	private void UpdateOverCompleteResult()
	{
		if (runsInCurrentOver <= 0 && currentBallNumber >= 5)
		{
			if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
			{
				bool flag = false;
				for (int i = 0; i < 6; i++)
				{
					if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[i] == "W")
					{
						flag = true;
					}
				}
				maidenOversCount++;
				ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterMaiden", maidenOversCount);
				if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
				{
				}
			}
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.Maiden++;
			Singleton<BowlingScoreCard>.instance.UpdateScoreCard();
		}
		if (runsInCurrentOver > 0 && currentBallNumber >= 5 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
		{
			CanShowPopupAgain(9);
		}
	}

	private void UpdateBallCompleteResult()
	{
		if (CONTROLLER.StrikerIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length && !CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].reachedHalfCentury)
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored >= 50)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].reachedHalfCentury = true;
				if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					fiftyRunsCount++;
					ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterFifty", fiftyRunsCount);
				}
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored >= 45 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored < 50 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
			}
		}
		if (CONTROLLER.NonStrikerIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length && !CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].reachedHalfCentury)
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored >= 50)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].reachedHalfCentury = true;
				if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					fiftyRunsCount++;
					ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterFifty", fiftyRunsCount);
				}
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored >= 45 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored < 50 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
			}
		}
		if (CONTROLLER.StrikerIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length && !CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].reachedCentury)
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored >= 100)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].reachedCentury = true;
				if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{

					hundredRunsCount++;
					ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterCentury", hundredRunsCount);
				}
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored >= 90 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored < 100 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
			}
		}
		if (CONTROLLER.NonStrikerIndex >= CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length || CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].reachedCentury)
		{
			return;
		}
		if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored >= 100)
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].reachedCentury = true;
			if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
				hundredRunsCount++;
				ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterCentury", hundredRunsCount);
			}
		}
		if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored >= 90 && CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored < 100 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
		}
	}

	public void CurrentBallUpdate(int canCountBall, int extraRun)
	{
		isInningsComplete = CheckForInningsComplete();
		if (isInningsComplete)
		{
			UpdateBallCompleteResult();
			UpdateOverCompleteResult();
		}
		else if (currentBallNumber == 5 || (currentBallNumber == -1 && canCountBall == 1 && extraRun != 1))
		{
			UpdateBallCompleteResult();
			UpdateOverCompleteResult();
			int strikerIndex = CONTROLLER.StrikerIndex;
			CONTROLLER.StrikerIndex = CONTROLLER.NonStrikerIndex;
			CONTROLLER.NonStrikerIndex = strikerIndex;
			// ─── Multiplayer: push post-over-swap strike to the bowling client ───
			// The over-end crossing runs locally on BOTH clients, but the regular
			// per-ball CmdCorrectBallState (sent earlier in UpdateCurrentBall, BEFORE
			// this swap) carried the PRE-swap indices. Without this push the bowling
			// client keeps the un-crossed striker → left/right-handed batsman desync
			// for the whole next over. Only the batting authority sends.
			if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null
				&& CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
			{
				CricketNetworkManager.instance.CmdSyncStrikeAtOver(
					staticVariables.UserProfiledata.user._id,
					CONTROLLER.StrikerIndex,
					CONTROLLER.NonStrikerIndex);
			}
		}
		else
		{
			UpdateBallCompleteResult();
		}
		AutoSave.currentBall = currentBallNumber;
		AutoSave.runsScoredInOver = runsInCurrentOver;
		AutoSave.WicketsInOver = wicketsThisOver;
		AutoSave.maxRunsReached = hasReachedMaxRuns;
		AutoSave.maxWidesReached = hasReachedMaxWides;
		AutoSave.currentPartnership = partnershipRunCount;
		AutoSave.fourInOver = fourScoredInOver;
		AutoSave.sixInOver = sixScoredInOver;
		AutoSave.maxSixes = hasReachedMaxSixes;
		CONTROLLER.strikerPartnershipRuns = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.currPatnerShipRuns;
		CONTROLLER.strikerPartnershipBall = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.currPatnerShipBalls;
		CONTROLLER.NonstrikerPartnershipRuns = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.currPatnerShipRuns;
		CONTROLLER.NonstrikerPartnershipBall = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.currPatnerShipBalls;
		if ((CONTROLLER.currentInnings == 1 && isInningsComplete && CONTROLLER.PlayModeSelected != 7) || (CONTROLLER.PlayModeSelected == 4 && CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchWickets >= CONTROLLER.totalWickets))
		{
			AutoSave.DeleteFile();
		}
		else
		{
			AutoSave.SaveInGameMatch();
		}

		// POST-ball snapshot push (issue #3, reports 142cedf0/969ff846 "wrong balls history"). The
		// other two PushMatchSnapshotIfBatting calls fire BEFORE this ball's chip is added — the
		// per-ball one (GameData.Scoring ~214) is deliberately pre-currentMatchBalls++ for score/
		// innings-complete consistency, and the other is at over/innings START. So the server's held
		// snapshot was always one ball STALE: a client reconnecting between balls restored a strip
		// missing the last completed ball, diverging from the client that never dropped. This runs at
		// the true end of ball processing (chip added + AutoSave committed above), so the snapshot the
		// server hands a reconnecting client now includes every completed ball. Batting authority only
		// and never mid-reconnect (would clobber the server's good state) — same gate as the others;
		// PushMatchSnapshotIfBatting re-checks PlayMode/AI/batting internally.
		if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
			&& CricketNetworkManager.instance != null
			&& CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
			&& (Launcher.Instance == null || !Launcher.Instance.IsReConnecting()))
		{
			CricketNetworkManager.instance.PushMatchSnapshotIfBatting();
		}
	}

	public void AutoplayCheckForOverComplete()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			isInningsComplete = CheckForInningsComplete();
			if (isInningsComplete)
			{
				canPauseGameplay = false;
				groundController.isFieldRestrictionActive = true;
				groundController.resetNoBallVairables();
				CONTROLLER.fielderChangeIndex = 1;
				CONTROLLER.computerFielderChangeIndex = 0;
				Singleton<Scoreboard>.instance.Hide(boolean: true);
				Singleton<BowlingScoreCard>.instance.InningsCompleted();
				if (CONTROLLER.currentInnings == 0 && CONTROLLER.PlayModeSelected != 7)
				{
					AutoSave.SaveInGameMatch();
					Time.timeScale = 0f;
					if (CONTROLLER.PlayModeSelected != 6)
					{
						Singleton<PauseGameScreen>.instance.Hide(boolean: true);
						Singleton<PauseGameScreen>.instance.BG.SetActive(value: false);
						scoreGenerationPanel.SetActive(value: true);
						Singleton<BattingControls>.instance.Hide(boolean: true);
						groundController.gameplayCamera.enabled = false;
						groundController.umpireViewCamera.enabled = true;
						groundController.ActivateStadiumAndSkybox(boolean: true);
						Singleton<GroundController>.instance.umpireViewCamera.transform.localPosition = new Vector3(-86.5f, 18.5f, -3.8f);
						Singleton<GroundController>.instance.umpireViewCamera.transform.eulerAngles = new Vector3(0f, -90f, 0f);
						Singleton<GroundController>.instance.umpireViewCamera.fieldOfView = 61f;
						CONTROLLER.pageName = string.Empty;
						DG.Tweening.Sequence sequence = DOTween.Sequence();
						sequence.Insert(0f, matchProgressBar.DOFillAmount(0f, 0f));
						sequence.Insert(0f, matchProgressBar.DOFillAmount(1f, 3f)).OnComplete(ShowBattingScoreCard);
						sequence.Insert(0f, currentScoreText.DOText(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores.ToString(), 2f, richTextEnabled: true, ScrambleMode.Numerals));
						sequence.Insert(1f, currentWicketsText.DOText(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets.ToString(), 2f, richTextEnabled: true, ScrambleMode.Numerals));
						sequence.SetUpdate(isIndependentUpdate: true);
					}
				}
				else
				{
					if (CONTROLLER.isFromAutoPlay)
					{
						CONTROLLER.isFromAutoPlay = false;
						Singleton<PauseGameScreen>.instance.Hide(boolean: true);
					}
					AutoSave.DeleteFile();
					checkForDemoPlay();
					Singleton<PauseGameScreen>.instance.Hide(boolean: true);
					Singleton<PauseGameScreen>.instance.BG.SetActive(value: false);
					scoreGenerationPanel.SetActive(value: true);
					Singleton<BattingControls>.instance.Hide(boolean: true);
					groundController.gameplayCamera.enabled = false;
					groundController.umpireViewCamera.enabled = true;
					groundController.ActivateStadiumAndSkybox(boolean: true);
					Singleton<GroundController>.instance.umpireViewCamera.transform.localPosition = new Vector3(-86.5f, 18.5f, -3.8f);
					Singleton<GroundController>.instance.umpireViewCamera.transform.eulerAngles = new Vector3(0f, -90f, 0f);
					Singleton<GroundController>.instance.umpireViewCamera.fieldOfView = 61f;
					CONTROLLER.pageName = string.Empty;
					DG.Tweening.Sequence sequence2 = DOTween.Sequence();
					sequence2.Insert(0f, matchProgressBar.DOFillAmount(0f, 0f));
					sequence2.Insert(0f, matchProgressBar.DOFillAmount(1f, 3f)).OnComplete(ShowGameOverScreen);
					sequence2.Insert(0f, currentScoreText.DOText(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores.ToString(), 2f, richTextEnabled: true, ScrambleMode.Numerals));
					sequence2.Insert(1f, currentWicketsText.DOText(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets.ToString(), 2f, richTextEnabled: true, ScrambleMode.Numerals));
					sequence2.SetUpdate(isIndependentUpdate: true);
				}
			}
			else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls % 6 == 0)
			{
				groundController.showPreviewCamera(status: false);
				NewOver();
			}
			currentAction = 0;
			return;
		}
		DebugLogger.PrintWithSize("CONTROLLER.currentInnings " + CONTROLLER.currentInnings);
		Singleton<BowlingControls>.instance.StopYellowFill();
		isInningsComplete = CheckForInningsComplete();
		if (isInningsComplete)
		{
			if (CONTROLLER.currentInnings == 2)
			{
				SetTargetToWin();
			}
			canPauseGameplay = false;
			groundController.isFieldRestrictionActive = true;
			groundController.resetNoBallVairables();
			CONTROLLER.fielderChangeIndex = 1;
			CONTROLLER.computerFielderChangeIndex = 1;
			Singleton<Scoreboard>.instance.Hide(boolean: true);
			Singleton<PreviewScreen>.instance.Hide(boolean: true);
			if (!CheckForMatchCompletion())
			{
				Time.timeScale = 0f;
				ShowBattingScoreCard();
			}
			else if (CONTROLLER.isFromAutoPlay)
			{
				isGamePaused = false;
				CONTROLLER.isFromAutoPlay = false;
				Singleton<PauseGameScreen>.instance.Hide(boolean: true);
				CONTROLLER.ShowTMGameOver = true;
				ShowBattingScoreCard();
			}
			groundController.currentActionState = -10;
		}
		else if ((CONTROLLER.currentInnings < 2 && CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchBalls1 % 6 == 0) || (CONTROLLER.currentInnings > 1 && CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchBalls2 % 6 == 0))
		{
			CONTROLLER.CurrentBall = -1;
			AutoSave.ballUpdate = string.Empty;
			groundController.resetNoBallVairables();
			groundController.currentActionState = -10;
			canPauseGameplay = false;
			isSessionFinished = checkforSessionComplete();
			groundController.showPreviewCamera(status: false);
			if (!isSessionFinished)
			{
				if (CheckForAIDeclaration())
				{
					Singleton<PauseGameScreen>.instance.declare();
				}
				else
				{
					NewOver();
				}
			}
			else if (CONTROLLER.MaxDays == CONTROLLER.currentDay && CONTROLLER.currentSession == 2)
			{
				isGamePaused = false;
				CONTROLLER.ballsBowledPerDay = 0;
				CONTROLLER.isFromAutoPlay = false;
				CONTROLLER.ShowTMGameOver = true;
				ShowBattingScoreCard();
			}
			else
			{
				Time.timeScale = 0f;
			}
		}
		AutoSave.SaveInGameMatch();
	}
}
