using DG.Tweening;

using UnityEngine;
using UnityEngine.UI;
using Cricket;
using Cricket;

public class DRS : Singleton<DRS>
{
	public GameObject holder;

	public bool enable = true;

	public bool DRSreplay;

	public Transform[] DRSResultUIPanel = new Transform[4];

	public Text[] DRSResultUIText = new Text[4];

	public GameObject DRSResultUIHolder;

	public Material DRSBallTrailMaterial;

	public GameObject DRSRedTrail;

	public GameObject DRSBlueTrail;

	private bool noBtnClickedCalled;

	private bool yesBtnClickedCalled;

	// MP DRS-board fix (#2): the 6s review countdown is a DOTween Sequence that auto-fires NoBtnClicked at
	// the end. Store it so a Yes/No decision can KILL it — otherwise it lingers and fires a stale NoBtnClicked
	// (and a contradictory CmdDRS_Decision(false)) ~6s after the review was already resolved.
	private Sequence _drsTimerSeq;

	public Image teamFlag;

	public Text teamText;

	public Text noOfdrs;

	public GameObject userReviewPanel;

	public GameObject aiReviewPanel;

	public Image timerImage;

	public Text timerText;

	public Text aiReviewstatus;

	private int secCount;

	//Photon Removal	public PhotonView photonView;

	public void ShowMe()
	{
		// timeScale-INDEPENDENT 2s delay (was Invoke, which is scaled). When an onPads/edge ball is caught,
		// a catch/impact slow-mo drops Time.timeScale well below 1; a scaled Invoke then crawls so the review
		// panel + its 6s auto-resolve countdown (SetDRSTimer) take 30s+ to appear/fire. Online there is no
		// human on the AI panel, so the auto-NoBtnClicked never lands in time and the LBW over HANGS. Run the
		// delay on unscaled time so the review always resolves in ~8s real and the over advances via the
		// proven post-review record path. Matches the SetUpdate(true) already used on the DRS result panels.
		DOTween.Sequence().AppendInterval(2f).AppendCallback(DelayShowMe).SetUpdate(isIndependentUpdate: true);
	}

	public void DelayShowMe()
	{
		noBtnClickedCalled = false;
		yesBtnClickedCalled = false;
		Singleton<GroundController>.instance.umpireViewCamera.enabled = false;
		Singleton<StandbyCam>.instance.RotateStandbyCam();
		holder.SetActive(value: true);
		if (Singleton<GroundController>.instance.drsByBattingSide == 1)
		{
			teamText.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ToString();
			noOfdrs.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].noofDRSLeft + " " + LocalizationData.localizationInstance.getText(512);
			teamFlag.sprite = Singleton<FlagHolderGround>.instance.searchFlagByName(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation);
		}
		else
		{
			teamText.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].ToString();
			noOfdrs.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].noofDRSLeft + " " + LocalizationData.localizationInstance.getText(512);
			teamFlag.sprite = Singleton<FlagHolderGround>.instance.searchFlagByName(CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation);
		}

		if (CONTROLLER.PlayModeSelected == 8 && Singleton<GroundController>.instance.drsByBattingSide == 1 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			userReviewPanel.SetActive(value: true);
			aiReviewPanel.SetActive(value: false);
		}
		else if (CONTROLLER.PlayModeSelected == 8 && Singleton<GroundController>.instance.drsByBattingSide == 1 && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
		{
			userReviewPanel.SetActive(value: false);
			aiReviewPanel.SetActive(value: true);
		}

		if (CONTROLLER.PlayModeSelected == 8 && Singleton<GroundController>.instance.drsByBattingSide == 0 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			userReviewPanel.SetActive(value: false);
			aiReviewPanel.SetActive(value: true);
		}
		else if (CONTROLLER.PlayModeSelected == 8 && Singleton<GroundController>.instance.drsByBattingSide == 0 && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex)
		{
			userReviewPanel.SetActive(value: true);
			aiReviewPanel.SetActive(value: false);
		}



		if (Singleton<GroundController>.instance.drsByUser == 1 && CONTROLLER.PlayModeSelected != 8)
		{
			userReviewPanel.SetActive(value: true);
			aiReviewPanel.SetActive(value: false);
		}
		else if (CONTROLLER.PlayModeSelected != 8)
		{
			userReviewPanel.SetActive(value: false);
			aiReviewPanel.SetActive(value: true);
		}
		SetDRSTimer();
	}

	public void YesBtnClicked()
	{
		// MP DRS-board fix (#2): the network relay now happens ONCE inside the !yesBtnClickedCalled block as
		// the 3-arg form carrying the authoritative isOut (so the bowling client can render the OUT/NOT-OUT
		// board). The old early 2-arg send here is removed.
		if (!yesBtnClickedCalled)
		{
			yesBtnClickedCalled = true;
			noBtnClickedCalled = true;
			DrsReviewAnchorNetTime = -1.0; // review resolved -> next review re-anchors fresh
			_drsTimerSeq?.Kill(); // stop the 6s countdown so it can't fire a stale NoBtnClicked after this
			Time.timeScale = 1f;
			holder.SetActive(value: false);
			Singleton<GroundController>.instance.isDRSEnabled = false;
			if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && !ConstantsData_M.useFullReviewReplay)
			{
				// ONLINE cosmetic board (default, ships safe): render a lightweight authoritative OUT/NOT-OUT
				// umpire board BEFORE the clean resume — WITHOUT the physics replay (it can't complete online and
				// repositioning the ball corrupted the next delivery). ShowDrsDecisionBoardOnly ends by calling
				// ForceResolveStuckReview internally, so the proven ball-state reset is preserved.
				GroundController gc = Singleton<GroundController>.instance;
				bool isOut = gc.IsLbwSavedDecision;
				if (userReviewPanel.activeInHierarchy && CricketNetworkManager.instance != null)
				{
					CricketNetworkManager.instance.CmdDRS_Decision(staticVariables.UserProfiledata.user._id, true, isOut);
				}
				gc.ShowDrsDecisionBoardOnly(isOut);
			}
			else
			{
				// OFFLINE, or ONLINE with useFullReviewReplay (restored full ball-tracking replay — the pre-zx
				// behaviour). When online + the kill-switch is on, relay so the OTHER client runs the SAME replay
				// (CNM RpcDRS_Decision routes YES -> RPC_DRS_Decision(true) -> full replay when the switch is on),
				// instead of the cosmetic board. Reviewer-gated so only the actual reviewer relays.
				if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
					&& userReviewPanel.activeInHierarchy && CricketNetworkManager.instance != null)
				{
					CricketNetworkManager.instance.CmdDRS_Decision(staticVariables.UserProfiledata.user._id, true, false);
				}
				Singleton<StandbyCam>.instance.PauseTween();
				CONTROLLER.canShowReplay = true;
				CONTROLLER.reviewReplay = true;
				DRSreplay = true;
				Singleton<GameData>.instance.AnimationCompleted();
			}
			//FirebaseAnalyticsManager.instance.logEvent("Extras", new string[2] { "ExtrasAction", "DRS_Review" });
		}
	}

	//Photon Removal	[PunRPC]
	public void RPC_DRS_Decision(bool Decision)
	{
		if (Decision)
		{
			YesBtnClicked();
		}
		else
		{
			NoBtnClicked();
		}
	}

	public void NoBtnClicked()
	{
		if (CONTROLLER.PlayModeSelected == 8 && userReviewPanel.activeInHierarchy)
		{
			if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null)
			{
				//Photon Removal — 3-arg form; the isOut arg is unused on the NOT-OUT/decline path (signature parity).
				CricketNetworkManager.instance.CmdDRS_Decision(staticVariables.UserProfiledata.user._id, false, false);
			}


		}

		if (!noBtnClickedCalled)
		{
			noBtnClickedCalled = true;
			DrsReviewAnchorNetTime = -1.0; // review resolved -> next review re-anchors fresh
			_drsTimerSeq?.Kill(); // stop the 6s countdown (also covers the auto-fire calling itself)
			Singleton<StandbyCam>.instance.PauseTween();
			Time.timeScale = 1f;
			holder.SetActive(value: false);
			DRSreplay = false;
			Singleton<GroundController>.instance.isDRSEnabled = false;
			Singleton<GroundController>.instance.umpireViewCamera.enabled = true;
			ConstantsData_M.Log("action setted to 3:::");
			// Online, if the LBW outcome was already committed at the appeal mark, the OppAck handshake has
			// already re-armed the bowler to currentActionState=-2; writing 3 here would clobber it and hang the
			// bowler. Skip the write in that case (the panel is already hidden above); resume from the -2.
			if (!(CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
				&& Singleton<GroundController>.instance.MpLbwOutcomeCommitted))
				Singleton<GroundController>.instance.currentActionState = 3;
		}
	}

	// Online: the shared review-start NetworkTime so BOTH clients run the identical countdown. -1 = unset.
	public static double DrsReviewAnchorNetTime = -1.0;

	public void SetDRSTimer()
	{
		// ONLINE TIMER SYNC: the batting authority stamps the shared review-start NetworkTime ONCE and relays it
		// so both clients run the same countdown off the synced clock (the timer was local-only -> a different
		// countdown on each screen). Degrades gracefully (full local 6s) if no/stale anchor -> no hang risk.
		if (ConstantsData_M.useSyncedDrsTimer && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
			&& CricketNetworkManager.instance != null && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex
			&& DrsReviewAnchorNetTime < 0.0)
		{
			DrsReviewAnchorNetTime = Mirror.NetworkTime.time;
			CricketNetworkManager.instance.CmdSyncDrsTimer(staticVariables.UserProfiledata.user._id, DrsReviewAnchorNetTime);
		}
		StartDrsTimerCountdown();
	}

	// Builds the countdown. remaining=6 normally; online with a FRESH shared anchor it starts at the shared
	// remaining so both screens match. Freshness-guarded (elapsed in [-1,6.5)) so a stale anchor never
	// instant-resolves — outside that window it falls back to a full local 6s.
	private void StartDrsTimerCountdown()
	{
		float remaining = 6f;
		if (ConstantsData_M.useSyncedDrsTimer && CONTROLLER.PlayModeSelected == 8 && DrsReviewAnchorNetTime >= 0.0
			&& CricketNetworkManager.instance != null)
		{
			float elapsed = (float)(Mirror.NetworkTime.time - DrsReviewAnchorNetTime);
			if (elapsed > -1f && elapsed < 6.5f)
				remaining = Mathf.Clamp(6f - elapsed, 0.25f, 6f);
		}
		secCount = Mathf.Clamp(Mathf.CeilToInt(remaining), 1, 6);
		timerImage.fillAmount = remaining / 6f;
		aiReviewstatus.text = LocalizationData.localizationInstance.getText(513);
		TweenCallback callback = delegate
		{
			SetSecond();
		};
		_drsTimerSeq?.Kill(); // kill any prior countdown before starting a fresh one
		Sequence s = DOTween.Sequence();
		// timeScale-INDEPENDENT: a catch/impact slow-mo (Time.timeScale << 1) would otherwise stall this
		// countdown so the auto-NoBtnClicked never fires and the online LBW over hangs (same as the DRS result
		// panels at ~261/277).
		s.SetUpdate(isIndependentUpdate: true);
		_drsTimerSeq = s; // store so Yes/No can stop the auto-fire (#2)
		s.Append(timerImage.DOFillAmount(0f, remaining));
		for (int i = 0; i < secCount; i++)
		{
			s.InsertCallback(i, callback);
		}
		s.InsertCallback(remaining, NoBtnClicked);
	}

	// The non-reviewer client started its timer locally before the authority's anchor arrived; re-base it to
	// the shared anchor so both screens match. No-op once a decision has latched.
	public void ApplySyncedTimerAnchor(double startNetTime)
	{
		if (!ConstantsData_M.useSyncedDrsTimer) return;
		DrsReviewAnchorNetTime = startNetTime;
		if (holder != null && holder.activeInHierarchy && !noBtnClickedCalled && !yesBtnClickedCalled)
			StartDrsTimerCountdown();
	}

	public void SetSecond()
	{
		timerText.text = secCount.ToString();
		secCount--;
		if (Singleton<GroundController>.instance.aiGoForDRS())
		{
			if (secCount <= 2)
			{
				// MP fix: in online play teamText.text is a dynamic team/player name that is NOT a
				// localization reference key, so IndexOf returns -1 -> getText(-1) threw an
				// IndexOutOfRange inside this DOTween review-timer callback. The throw aborted the
				// countdown before the secCount<=0 auto-YesBtnClicked could fire, so the
				// action-replay/review text never cleared and the bowling flow stalled (auto-bowl).
				// Only re-localize when the name is a known reference; otherwise keep what is shown.
				int index = LocalizationData.localizationInstance.referenceList.IndexOf(teamText.text.ToUpper());
				if (index >= 0)
				{
					teamText.text = LocalizationData.localizationInstance.getText(index);
				}
				aiReviewstatus.text = " " + teamText.text + " " + LocalizationData.localizationInstance.getText(514);
			}
			if (secCount <= 0)
			{
				YesBtnClicked();
			}
		}
	}

	public void ResetPanelTransition()
	{
		DRSResultUIHolder.SetActive(value: true);
		for (int i = 0; i < DRSResultUIPanel.Length - 1; i++)
		{
			DRSResultUIPanel[i].GetChild(0).DOScaleY(0f, 0f).SetUpdate(isIndependentUpdate: true);
			DRSResultUIPanel[i].GetChild(1).DOScaleY(0f, 0f).SetUpdate(isIndependentUpdate: true);
		}
	}

	public void ShowDRSResultPanel(int index, string result, int color)
	{
		if (index == 3 && Singleton<GroundController>.instance.impactOnOffsideDuringShot)
		{
			return;
		}
		if (index == 4)
		{
			DRSResultUIPanel[index].gameObject.SetActive(value: true);
			return;
		}
		DRSResultUIPanel[index].GetChild(0).DOScaleY(1f, 0.6f).SetUpdate(isIndependentUpdate: true);
		DRSResultUIPanel[index].GetChild(1).DOScaleY(1f, 0.6f).SetUpdate(isIndependentUpdate: true);
		DRSResultUIText[index].text = result;
		switch (color)
		{
			case 1:
				DRSResultUIPanel[index].GetChild(1).GetComponent<Image>().color = new Color(0f, 0.9f, 0.1f, 0.8f);
				DRSResultUIPanel[index].GetChild(1).GetComponentInChildren<Text>().color = Color.black;
				break;
			case 0:
				DRSResultUIPanel[index].GetChild(1).GetComponent<Image>().color = new Color(1f, 0f, 0f, 0.8f);
				DRSResultUIPanel[index].GetChild(1).GetComponentInChildren<Text>().color = Color.white;
				break;
			default:
				DRSResultUIPanel[index].GetChild(1).GetComponent<Image>().color = new Color(0.9f, 0.35f, 0f, 0.8f);
				DRSResultUIPanel[index].GetChild(1).GetComponentInChildren<Text>().color = Color.white;
				break;
		}
	}

	public void Hide()
	{
		DRSResultUIPanel[4].gameObject.SetActive(value: false);
		DRSResultUIHolder.SetActive(value: false);
	}

	// MP review-stuck fix: a watchdog/skip-replay resolution (GroundController.ForceResolveStuckReview)
	// resolves an online review WITHOUT ever running Yes/NoBtnClicked — the only two sites that hid the
	// review radar panel (holder, carrying the "DECISION PENDING" aiReviewstatus text) and killed the 6s
	// countdown (_drsTimerSeq). So after a watchdog resolve the green LBW radar + "DECISION PENDING" stayed
	// up forever and the still-alive countdown could late-fire a stale NoBtnClicked. This tears the review
	// panel down EXACTLY like NoBtnClicked's panel cleanup (holder hide + Kill + latch both *Called so the
	// countdown can't re-fire) but WITHOUT writing currentActionState — the caller (ForceResolveStuckReview)
	// already owns the ACK-armed -2 / state=3 decision. Idempotent and null-safe.
	public void DismissReviewPanel()
	{
		yesBtnClickedCalled = true;
		noBtnClickedCalled = true;
		DrsReviewAnchorNetTime = -1.0;
		_drsTimerSeq?.Kill();
		_drsTimerSeq = null;
		if (holder != null) holder.SetActive(value: false);
		Hide();
	}
}
