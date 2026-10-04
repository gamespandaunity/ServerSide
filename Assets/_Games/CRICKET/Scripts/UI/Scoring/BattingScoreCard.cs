using DG.Tweening;

using System.Collections;
using System.Transactions;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class BattingScoreCard : Singleton<BattingScoreCard>
{
	public Font bold;

	public Font normal;

	public GameObject BG;

	public GameObject scoreCard;

	// FINAL stuck-panel backstop (online MP): no scorecard may stay open past 30s, no matter which
	// relay/handshake died. The targeted watchdogs (canLoad defer, ground defer) only arm on their specific
	// deferred states — if the flow never even reached Continue() (lost tick relay, one-sided panel after a
	// reconnect), NOTHING recovered and the tester had to leave the match. This blanket timer force-advances
	// the same proven path those watchdogs use. Re-arms after firing, so a still-stuck panel retries.
	private float _panelStuckAt = -1f;

	private void Update()
	{
		if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
		if (scoreCard == null || !scoreCard.activeInHierarchy) { _panelStuckAt = -1f; return; }
		if (CricketNetworkManager.RestorePending) { return; }   // mid-restore: let the restore finish first
		if (_panelStuckAt < 0f) { _panelStuckAt = Time.unscaledTime; return; }
		// Right after a reconnect the panel-closing relay was missed while disconnected — don't sit the
		// full blanket window; the staying opponent has usually LONG moved on (13-07 over-boundary repro).
		float stuckLimit = (Time.realtimeSinceStartup - CricketNetworkManager.LastRestoreRealtime < 60f) ? 8f : 30f;
		if (Time.unscaledTime - _panelStuckAt < stuckLimit) return;
		_panelStuckAt = Time.unscaledTime;   // re-arm (retry if somehow still open)
		ConstantsData_M.MpLog($"[StuckPanel] BattingScoreCard open >{stuckLimit}s — force-advancing (blanket backstop).");
		CONTROLLER.CanLoadBowlerScoreBoard = true;
		Continue();
	}

	private Transform _transform;

	public Sprite[] batsmanStates;

	public GameObject scrollList;

	private Vector3 startPos;

	public Button ContinueBtn;

	public Button BackBtn;

	public Image SCTeamFlag;

	public Image BtnTeamFlag;

	public Text SCTeamName;

	public Text BtnTeamName;

	public Text OversText;

	public Text ScoreText;

	public Text ExtrasText;

	public bool BackKeyEnable;

	public BatsmanDetails[] batsman;

	public int SelectedInnings = 1;

	public Text SelectedInningsText;

	public Scrollbar scrollView;

	private int battingIndex;

	//Multiplayer

	//Photon Removal	public PhotonView photonView;

	private int secCount;

	[SerializeField] private Image timerImage;

	[SerializeField] private Text timerText;

	[SerializeField] private GameObject Timer;

	private const int MaxRetries = 4; // Reconnect-flood fix: was 50 (25s of resends per Continue → ~150 RPCs/reconnect). Mirror's continue RPC is on the reliable channel so it's delivered; a few resends cover a brief loss window without flooding.
	private float RetryDelay = 0.5f;

	// Reconnect-flood fix: SetBattingScoreCardTimer (5s auto-continue DOTween) and SendRPCContinueWithRetry
	// (50× continue resend) were both created as LOCAL handles, so after a reconnect re-showed the scorecard
	// they STACKED — ~6 timers + 6 retry coroutines → CmdBattingScoreCardContinue flooded ~300× over 2 min,
	// saturating the continue/scorecard path and breaking the LBW review flow. Store both and kill/stop the
	// prior one before starting a new one so at most ONE of each runs. Single-instance flow is unchanged.
	private Coroutine _continueRetryCo;
	private Sequence _battingScoreCardTimerSeq;

	protected void Awake()
	{
		if (CONTROLLER.PlayModeSelected == 8 && Launcher.Instance.GetReconnectCount() != 0)
		{
			////Debug.Log("SKIPPP kR DIyaa");
			return;
		}
		////Debug.Log("SKIPPP kR DIyaa HAII");
		if (CONTROLLER.PlayModeSelected == 8)
		{
			CONTROLLER.CanLoadBowlerScoreBoard = false;
		}
		Hide(boolean: true);
	}

	protected void Start()
	{
		if (CONTROLLER.PlayModeSelected == 8)
		{
			//Photon Removal	PhotonNetwork.IsMessageQueueRunning = true;
		}

		startPos = scrollList.transform.position;
	}

	public void addEventListener()
	{
	}

	public void ResetBattingCard()
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//TMResetBattingCard();
			return;
		}
		int battingTeamIndex = CONTROLLER.BattingTeamIndex;
		SetTeamInfo();
		for (int i = 0; i < batsman.Length; i++)
		{
			batsman[i].Highlight.sprite = batsmanStates[0];
			batsman[i].Name.font = normal;
			batsman[i].Name.text = CONTROLLER.TeamList[battingTeamIndex].PlayerList[i].ScoreboardName.ToUpper();
			batsman[i].Status.text = string.Empty.ToUpper();
			batsman[i].Runs.text = string.Empty;
			batsman[i].Balls.text = string.Empty;
			batsman[i].SR.text = string.Empty;
			batsman[i].Fours.text = string.Empty;
			batsman[i].Sixes.text = string.Empty;
			batsman[i].FOW.text = string.Empty;
		}
		ScoreText.text = Singleton<GameData>.instance.scoreDisplayString;
		ExtrasText.text = Singleton<GameData>.instance.extraRunsDisplayString;
		OversText.text = Singleton<GameData>.instance.oversDisplayString;
		if (OversText.text.Contains("("))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace("(", LocalizationData.localizationInstance.getText(651));
		}
		if (OversText.text.Contains(")"))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace(")", LocalizationData.localizationInstance.getText(652));
		}
	}

	public void RestartGame()
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			return;
		}
		int battingTeamIndex = CONTROLLER.BattingTeamIndex;
		SetTeamInfo();
		for (int i = 0; i < batsman.Length; i++)
		{
			if (CONTROLLER.TeamList[battingTeamIndex].PlayerList[i].BatsmanList.Status == string.Empty)
			{
				batsman[i].Highlight.sprite = batsmanStates[0];
				batsman[i].Name.font = normal;
				batsman[i].Name.text = CONTROLLER.TeamList[battingTeamIndex].PlayerList[i].ScoreboardName.ToUpper();
				batsman[i].Status.text = string.Empty.ToUpper();
				batsman[i].Runs.text = string.Empty;
				batsman[i].Balls.text = string.Empty;
				batsman[i].SR.text = string.Empty;
				batsman[i].Fours.text = string.Empty;
				batsman[i].Sixes.text = string.Empty;
				batsman[i].FOW.text = string.Empty;
			}
			else
			{
				UpdateWicket(i);
			}
		}
		ScoreText.text = Singleton<GameData>.instance.scoreDisplayString;
		ExtrasText.text = Singleton<GameData>.instance.extraRunsDisplayString;
		OversText.text = Singleton<GameData>.instance.oversDisplayString;
		if (OversText.text.Contains("("))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace("(", LocalizationData.localizationInstance.getText(651));
		}
		if (OversText.text.Contains(")"))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace(")", LocalizationData.localizationInstance.getText(652));
		}
	}

	private void SetTeamInfo()
	{
		if (CONTROLLER.currentInnings == 0)
		{
			Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
			foreach (Sprite sprite in flags)
			{
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
				{
					SCTeamFlag.sprite = sprite;
				}
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
				{
					BtnTeamFlag.sprite = sprite;
				}
			}
			SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
			BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
			return;
		}
		Sprite[] flags2 = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite2 in flags2)
		{
			if (sprite2.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
			{
				SCTeamFlag.sprite = sprite2;
			}
			if (sprite2.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
			{
				BtnTeamFlag.sprite = sprite2;
			}
		}
		SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
		BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
	}

	private void TMSetTeamInfo()
	{
		Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite in flags)
		{
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
			{
				SCTeamFlag.sprite = sprite;
			}
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
			{
				BtnTeamFlag.sprite = sprite;
			}
		}
		SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
		BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
	}

	public void UpdateScoreCard()
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//TMUpdateScoreCard();
			return;
		}
		Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite in flags)
		{
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
			{
				SCTeamFlag.sprite = sprite;
			}
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
			{
				BtnTeamFlag.sprite = sprite;
			}
		}
		SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
		BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
		int battingTeamIndex = CONTROLLER.BattingTeamIndex;
		int strikerIndex = CONTROLLER.StrikerIndex;
		if (strikerIndex >= 0 && strikerIndex < CONTROLLER.TeamList[battingTeamIndex].PlayerList.Length)
		{
			batsman[strikerIndex].Status.text = CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.Status.ToUpper();
			batsman[strikerIndex].Runs.text = string.Empty + CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.RunsScored;
			batsman[strikerIndex].Balls.text = string.Empty + CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.BallsPlayed;
			batsman[strikerIndex].SR.text = GetStrikeRate(battingTeamIndex, strikerIndex);
			batsman[strikerIndex].Fours.text = string.Empty + CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.Fours;
			batsman[strikerIndex].Sixes.text = string.Empty + CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.Sixes;
			if (batsman[strikerIndex].Status.text == "not out".ToUpper())
			{
				batsman[strikerIndex].Highlight.sprite = batsmanStates[1];
				batsman[strikerIndex].Name.font = bold;
				batsman[strikerIndex].FOW.text = string.Empty;
			}
			else if (batsman[strikerIndex].Status.text == string.Empty)
			{
				batsman[strikerIndex].Name.font = normal;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[0];
			}
			else
			{
				batsman[strikerIndex].Name.font = normal;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[2];
			}
		}
		strikerIndex = CONTROLLER.NonStrikerIndex;
		if (strikerIndex >= 0 && strikerIndex < CONTROLLER.TeamList[battingTeamIndex].PlayerList.Length)
		{
			batsman[strikerIndex].Status.text = CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.Status.ToUpper();
			batsman[strikerIndex].Runs.text = string.Empty + CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.RunsScored;
			batsman[strikerIndex].Balls.text = string.Empty + CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.BallsPlayed;
			batsman[strikerIndex].SR.text = GetStrikeRate(battingTeamIndex, strikerIndex);
			batsman[strikerIndex].Fours.text = string.Empty + CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.Fours;
			batsman[strikerIndex].Sixes.text = string.Empty + CONTROLLER.TeamList[battingTeamIndex].PlayerList[strikerIndex].BatsmanList.Sixes;
			if (batsman[strikerIndex].Status.text == "not out".ToUpper())
			{
				batsman[strikerIndex].Name.font = bold;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[1];
				batsman[strikerIndex].FOW.text = string.Empty;
			}
			else if (batsman[strikerIndex].Status.text == string.Empty)
			{
				batsman[strikerIndex].Name.font = normal;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[0];
			}
			else
			{
				batsman[strikerIndex].Name.font = normal;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[2];
			}
		}
		ScoreText.text = Singleton<GameData>.instance.scoreDisplayString;
		ExtrasText.text = Singleton<GameData>.instance.extraRunsDisplayString;
		OversText.text = Singleton<GameData>.instance.oversDisplayString;
		if (OversText.text.Contains("("))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace("(", LocalizationData.localizationInstance.getText(651));
		}
		if (OversText.text.Contains(")"))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace(")", LocalizationData.localizationInstance.getText(652));
		}
		CheckBatsmanStatus();
		Singleton<BattingScoreBoardPanelTransition>.instance.PanelTransition();
	}

	public string GetStrikeRate(int teamID, int playerID)
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//return TMGetStrikeRate(teamID, playerID);
		}
		float num = CONTROLLER.TeamList[teamID].PlayerList[playerID].BatsmanList.RunsScored;
		float num2 = CONTROLLER.TeamList[teamID].PlayerList[playerID].BatsmanList.BallsPlayed;
		return string.Concat(arg1: (num2 > 0f) ? ((int)(num / num2 * 100f)) : 0, arg0: string.Empty);
	}

	public void UpdateWicket(int playerID)
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//TMUpdateWicket(playerID);
			return;
		}
		batsman[playerID].Name.font = normal;
		batsman[playerID].Highlight.sprite = batsmanStates[2];
		batsman[playerID].Name.text = GetBatsmanShortName(CONTROLLER.BattingTeamIndex, playerID).ToUpper();
		batsman[playerID].Runs.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.RunsScored;
		batsman[playerID].Balls.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.BallsPlayed;
		batsman[playerID].SR.text = GetStrikeRate(CONTROLLER.BattingTeamIndex, playerID);
		batsman[playerID].Fours.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.Fours;
		batsman[playerID].Sixes.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.Sixes;
		batsman[playerID].Status.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.Status.ToUpper();
		batsman[playerID].FOW.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.FOW;
	}

	public string GetBatsmanShortName(int TeamID, int playerID)
	{
		return CONTROLLER.TeamList[TeamID].PlayerList[playerID].ScoreboardName;
	}

	private void DisplayList(int TeamID)
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//TMDisplayList(TeamID);
			return;
		}
		for (int i = 0; i < batsman.Length; i++)
		{
			if (CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.Status == string.Empty || CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.Status == null)
			{
				batsman[i].Name.font = normal;
				batsman[i].Highlight.sprite = batsmanStates[0];
				batsman[i].Name.text = CONTROLLER.TeamList[TeamID].PlayerList[i].ScoreboardName.ToUpper();
				batsman[i].Status.text = string.Empty.ToUpper();
				batsman[i].Runs.text = string.Empty;
				batsman[i].Balls.text = string.Empty;
				batsman[i].SR.text = string.Empty;
				batsman[i].Fours.text = string.Empty;
				batsman[i].Sixes.text = string.Empty;
				batsman[i].FOW.text = string.Empty;
				continue;
			}
			if (CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.Status == "not out".ToUpper())
			{
				batsman[i].Name.font = bold;
				batsman[i].Highlight.sprite = batsmanStates[1];
				batsman[i].FOW.text = string.Empty;
			}
			else
			{
				batsman[i].Name.font = normal;
				batsman[i].Highlight.sprite = batsmanStates[2];
				batsman[i].FOW.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.FOW;
			}
			batsman[i].Name.text = GetBatsmanShortName(TeamID, i).ToUpper();
			batsman[i].Runs.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.RunsScored;
			batsman[i].Balls.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.BallsPlayed;
			batsman[i].SR.text = GetStrikeRate(TeamID, i);
			batsman[i].Fours.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.Fours;
			batsman[i].Sixes.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.Sixes;
			batsman[i].Status.text = CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.Status.ToUpper();
		}
		string text = CONTROLLER.TeamList[TeamID].currentMatchScores + "/" + CONTROLLER.TeamList[TeamID].currentMatchWickets;
		string text2 = LocalizationData.localizationInstance.getText(235) + " " + CONTROLLER.TeamList[TeamID].currentMatchExtras;
		int num = CONTROLLER.TeamList[TeamID].currentMatchBalls / 6;
		int num2 = CONTROLLER.TeamList[TeamID].currentMatchBalls % 6;
		string text3 = LocalizationData.localizationInstance.getText(184) + " " + num + "." + num2 + "(" + CONTROLLER.totalOvers + ")";
		ScoreText.text = text;
		ExtrasText.text = text2;
		OversText.text = text3;
		if (OversText.text.Contains("("))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace("(", LocalizationData.localizationInstance.getText(651));
		}
		if (OversText.text.Contains(")"))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace(")", LocalizationData.localizationInstance.getText(652));
		}
	}

	public void SwapScorecard()
	{
		scrollList.transform.position = startPos;
		if (BtnTeamName.text.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper())
		{
			DisplayList(CONTROLLER.BattingTeamIndex);
			BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
			SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
			Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
			foreach (Sprite sprite in flags)
			{
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
				{
					BtnTeamFlag.sprite = sprite;
				}
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
				{
					SCTeamFlag.sprite = sprite;
				}
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets > 7)
			{
				scrollView.value = 0f;
			}
			else
			{
				scrollView.value = 1f;
			}
			if (CONTROLLER.PlayModeSelected == 7)
			{
				//ScoreText.text = GetScore(CONTROLLER.BattingTeamIndex);
				//OversText.text = GetOvers(CONTROLLER.BattingTeamIndex);
				//if (OversText.text.Contains("("))
				//{
				//	//Debug.Log(OversText.text);
				//	OversText.text = OversText.text.Replace("(", LocalizationData.instance.getText(651));
				//}
				//if (OversText.text.Contains(")"))
				//{
				//	//Debug.Log(OversText.text);
				//	OversText.text = OversText.text.Replace(")", LocalizationData.instance.getText(652));
				//}
			}
		}
		else
		{
			DisplayList(CONTROLLER.BowlingTeamIndex);
			BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
			SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
			Sprite[] flags2 = Singleton<FlagHolderGround>.instance.flags;
			foreach (Sprite sprite2 in flags2)
			{
				if (sprite2.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
				{
					BtnTeamFlag.sprite = sprite2;
				}
				if (sprite2.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
				{
					SCTeamFlag.sprite = sprite2;
				}
			}
			if (CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchWickets > 7)
			{
				scrollView.value = 0f;
			}
			else
			{
				scrollView.value = 1f;
			}
			if (CONTROLLER.PlayModeSelected == 7)
			{
				//ScoreText.text = GetScore(CONTROLLER.BowlingTeamIndex);
				//OversText.text = GetOvers(CONTROLLER.BowlingTeamIndex);
				//if (OversText.text.Contains("("))
				//{
				//	//Debug.Log(OversText.text);
				//	OversText.text = OversText.text.Replace("(", LocalizationData.instance.getText(651));
				//}
				//if (OversText.text.Contains(")"))
				//{
				//	//Debug.Log(OversText.text);
				//	OversText.text = OversText.text.Replace(")", LocalizationData.instance.getText(652));
				//}
			}
		}
		CheckBatsmanStatus();
		Singleton<BattingScoreBoardPanelTransition>.instance.PanelTransition();
	}

	private void CheckBatsmanStatus()
	{
		BatsmanDetails[] array = batsman;
		foreach (BatsmanDetails batsmanDetails in array)
		{
			if (batsmanDetails.Status.text.ToUpper() == "not out".ToUpper())
			{
				batsmanDetails.Highlight.sprite = batsmanStates[1];
			}
		}
	}

	//Photon Removal	[PunRPC]
	public void RPC_Continue()
	{
		if (scoreCard.activeInHierarchy)
		{
			// The 'myTeamIndex == BowlingTeamIndex' gate was REMOVED. RpcBattingScoreCardContinue already skips the
			// SENDER (the batting player who tapped Continue), so RPC_Continue only ever runs on the OTHER player —
			// which must dismiss/advance its own batting scorecard too. At the innings swap the roles flip (this
			// side becomes BattingTeamIndex before the relay lands), so that gate went FALSE and Continue() was
			// never called → the batting scorecard NEVER dismissed on the other side (tester: "batter side se tick
			// karo to bowler side ka panel nahi hatta"). Always continue on the receiver; Continue()'s own gates
			// then route it correctly (dismiss for the new bowler, advance-to-bowling-scorecard for the new batter).
			// Also force CanLoadBowlerScoreBoard here: the other player CONTINUING is itself the signal to proceed,
			// so this side must NOT deadlock waiting for the separate CmdToggleCanLoadBowlerScoreCard relay — logs
			// show the innings-1 batting side received ZERO of those relays, so Continue() would otherwise defer at
			// its !CanLoadBowlerScoreBoard gate and the panel would sit forever (tester: "bowler side ka panel nahi
			// hatta"). Harmless for the bowling receiver (that flag isn't its gate).
			CONTROLLER.CanLoadBowlerScoreBoard = true;
			Continue();
		}
	}

	private void CallContinue()
	{
		////Debug.Log("CALl DONNEESSSzzzzS");
		Continue();
	}

	public void Continue()
	{
		CONTROLLER.CanLoadBowlerScoreBoard.Show("CanLoadBowlerScoreBoard");
		(CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex).Show();
		if (!CONTROLLER.CanLoadBowlerScoreBoard && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			// Innings-shift deadlock fallback (tester: 2nd innings won't start; logs show the innings-1 batting
			// side received ZERO canLoad relays). CanLoadBowlerScoreBoard is flipped by the bowling side's
			// CmdToggleCanLoadBowlerScoreCard relay, but at the innings swap that NON-BUFFERED ClientRpc can be
			// MISSED (this side briefly re-subscribes), so the flag never flips and Continue() deadlocks here
			// forever. Arm a one-shot timeout that force-sets it true + retries Continue, so a missed relay can
			// never hang the 2nd-innings start. (The relay still flips it faster on the normal path.)
			if (_canLoadWatchdogCo == null && gameObject.activeInHierarchy)
				_canLoadWatchdogCo = StartCoroutine(ForceCanLoadBowlerScoreboardAfterTimeout());
			return;
		}

		if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && CONTROLLER.PlayModeSelected == 8)
		{
			if (Timer.activeInHierarchy)
			{
				Timer.SetActive(false);
			}


			if (GameConstants.isWithAI == false)
			{
				if (_continueRetryCo != null) StopCoroutine(_continueRetryCo); // no stacked retry senders (reconnect-flood fix)
				_continueRetryCo = StartCoroutine(SendRPCContinueWithRetry());
			}



		}

		Hide(boolean: true);

		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			////Debug.Log("LOADDGROUND");
			Singleton<GroundController>.instance.UpdateCanLoadGround(true);
		}

		if (CONTROLLER.gameCompleted)
		{
			Singleton<MatchSummary>.instance.showMe();
		}
		else if (!Singleton<GameData>.instance.isGamePaused)
		{
			if (CONTROLLER.PlayModeSelected < 4 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
			{
				if (CONTROLLER.isFromAutoPlay)
				{
					Singleton<GameData>.instance.isGamePaused = true;
				}

				Singleton<GameData>.instance.ShowBowlingScoreCard();
			}
			else
			{
				Singleton<BowlingScoreCard>.instance.Continue();
			}
		}
		else
		{
			Singleton<GameData>.instance.AgainToGamePauseScreen();
			Singleton<PauseGameScreen>.instance.Hide(boolean: false);
		}
	}

	public void SwapInnings()
	{
		if (SelectedInnings == 2)
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchWickets1 > 7)
			{
				scrollView.value = 0f;
			}
			else
			{
				scrollView.value = 1f;
			}
			SelectedInnings = 1;
			SelectedInningsText.text = LocalizationData.localizationInstance.getText(470);
		}
		else
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchWickets2 > 7)
			{
				scrollView.value = 0f;
			}
			else
			{
				scrollView.value = 1f;
			}
			SelectedInnings = 2;
			SelectedInningsText.text = LocalizationData.localizationInstance.getText(469);
		}
		TMDisplayList(CONTROLLER.BattingTeamIndex);
		CheckBatsmanStatus();
	}

	public void SetBattingScoreCardTimer()
	{
		secCount = 5;
		timerImage.fillAmount = 1f;
		//aiReviewstatus.text = LocalizationData.instance.getText(513);
		_battingScoreCardTimerSeq?.Kill(); // kill any prior auto-continue timer so they don't stack (reconnect-flood fix)
		Sequence s = DOTween.Sequence();
		_battingScoreCardTimerSeq = s;
		TweenCallback callback = delegate
		{
			if (!Timer.activeInHierarchy)
			{
				s.Kill();
			}
			SetSecond();
		};
		s.Append(timerImage.DOFillAmount(0f, 5f));
		for (int i = 0; i < 5; i++)
		{
			s.InsertCallback(i, callback);
		}
		//s.InsertCallback(6f, NoBtnClicked);
		s.InsertCallback(5, CallContinue);
	}

	public void SetSecond()
	{
		timerText.text = secCount.ToString();
		secCount--;
		//if (Singleton<GroundController>.instance.aiGoForDRS())
		//{
		//    if (secCount <= 2)
		//    {
		//        //int index = LocalizationData.instance.refList.IndexOf(teamText.text.ToUpper());
		//        //teamText.text = LocalizationData.instance.getText(index);
		//        //aiReviewstatus.text = " " + teamText.text + " " + LocalizationData.instance.getText(514);
		//    }
		//    if (secCount <= 0)
		//    {
		//        //YesBtnClicked();
		//    }
		//}

		//if(secCount < 0)
		//{
		//	Continue();
		//}
	}

	public void Hide(bool boolean)
	{
		if (boolean)
		{
			////Debug.Log("HIDE BatsaMMan");
			BG.SetActive(value: false);
			BackKeyEnable = false;
			scoreCard.SetActive(value: false);
			DisplayList(CONTROLLER.BattingTeamIndex);
			Timer.SetActive(false);
			CONTROLLER.myTeamIndex.Show("myTeamIndex");
			CONTROLLER.BowlingTeamIndex.Show("BowlingTeamIndex");
			CONTROLLER.BattingTeamIndex.Show("BattingTeamIndex");
			if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
			{
				if (GameConstants.isWithAI == false)
				{
					//photonView.RPC("RPC_ToggleCanLoadBowlerScoreCard", RpcTarget.OthersBuffered, false);
					// Ghost-session guard. The 30-07 ritu_mp logs show THIS send throwing inside Mirror's
					// SendCommandInternal on a reloaded scene, which aborts the rest of the caller — and for
					// CmdSetBowler that means the bowler is never assigned or relayed, i.e. the two sides end up
					// with different bowlers. Only the sends actually seen throwing are guarded.
					if (CricketNetworkManager.ReadyToSend)
					    CricketNetworkManager.instance.CmdToggleCanLoadBowlerScoreCard(staticVariables.UserProfiledata.user._id, false);
					else
					    ConstantsData_M.MpLog("[GhostSend] ToggleCanLoadBowlerScoreCard(false) BLOCKED — network manager not ready (ghost session); caller continues instead of throwing.");
				}
				//          



				CONTROLLER.CanLoadBowlerScoreBoard = false;
			}
			return;
		}
		else
		{
			////Debug.Log("BOwloinngS SCoreedcard");

			if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && CONTROLLER.CanLoadBowlerScoreBoard)
			{
				////Debug.Log("CANNN WEE : " + CONTROLLER.CanLoadBowlerScoreBoard);
				Timer.SetActive(true);
				SetBattingScoreCardTimer();
			}
			if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
			{
				//photonView.RPC("RPC_ToggleCanLoadBowlerScoreCard", RpcTarget.OthersBuffered, true);
				StartCoroutine(SendRPCWithRetry());
				CONTROLLER.CanLoadBowlerScoreBoard = true;
			}
			if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && CONTROLLER.PlayModeSelected == 8)
			{
				Singleton<GroundController>.instance.UpdateCanBowlerBowl(false);
				Singleton<GroundController>.instance.UpdateCanLoadGround(false);
			}
			if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
			{
				ContinueBtn.interactable = false;
			}
			else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
				ContinueBtn.interactable = true;

				if (CricketNetworkManager.instance != null)
				{
					CricketNetworkManager.instance.CmdChangeCurrentOpenPanel((int)PanelsInfo.None); // reverted from BattingScoreCard: leaked into game start (innings-start lineup, no paired reset)
				}
			}
		}

		//if (CONTROLLER.canShowbannerGround == 1)
		//{
		//	Singleton<AdIntegrate>.instance.ShowAd();
		//}
		Singleton<NavigationBack>.instance.deviceBack = null;
		//if (CONTROLLER.PlayModeSelected == 6)
		//{
		//	return;
		//}
		CONTROLLER.pageName = "battingSC";
		if (CONTROLLER.PlayModeSelected > 3 || CONTROLLER.PlayModeSelected == 7)
		{
			BtnTeamName.transform.parent.gameObject.SetActive(value: false);
		}
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//if (CONTROLLER.currentInnings < 2)
			//{
			//	if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchWickets1 > 7)
			//	{
			//		scrollView.value = 0f;
			//	}
			//	else
			//	{
			//		scrollView.value = 1f;
			//	}
			//	SelectedInnings = 1;
			//	SelectedInningsText.text = LocalizationData.instance.getText(470);
			//}
			//else
			//{
			//	if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchWickets2 > 7)
			//	{
			//		scrollView.value = 0f;
			//	}
			//	else
			//	{
			//		scrollView.value = 1f;
			//	}
			//	SelectedInnings = 2;
			//	SelectedInningsText.text = LocalizationData.instance.getText(469);
			//}
			//if (CONTROLLER.currentInnings == 1)
			//{
			//	BtnTeamName.transform.parent.gameObject.SetActive(value: true);
			//}
			//if (CONTROLLER.currentInnings > 1)
			//{
			//	SelectedInningsText.transform.parent.gameObject.SetActive(value: true);
			//}
			//else
			//{
			//	SelectedInningsText.transform.parent.gameObject.SetActive(value: false);
			//}
			//DisplayList(CONTROLLER.BattingTeamIndex);
		}
		else
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets > 7)
			{
			}
			SelectedInningsText.transform.parent.gameObject.SetActive(value: false);
		}
		if (!BG.activeSelf)
		{
			BG.SetActive(value: true);
		}
		if ((!Singleton<GameData>.instance.isGamePaused && !CONTROLLER.gameCompleted) || CONTROLLER.ShowTMGameOver)
		{
			ContinueBtn.gameObject.SetActive(value: true);
			BackBtn.gameObject.SetActive(value: false);
		}
		else if (CONTROLLER.isFromAutoPlay)
		{
			Singleton<GameData>.instance.isGamePaused = false;
			ContinueBtn.gameObject.SetActive(value: true);
			BackBtn.gameObject.SetActive(value: false);
		}
		else
		{
			BackKeyEnable = true;
			ContinueBtn.gameObject.SetActive(value: false);
			BackBtn.gameObject.SetActive(value: true);
		}
		scrollList.transform.position = startPos;
		scoreCard.SetActive(value: true);
		Singleton<BattingControls>.instance.EnableRun(boolean: false);
		Singleton<BattingControls>.instance.EnableCancelRun(boolean: false);
		CheckBatsmanStatus();

	}

	// Innings-shift deadlock fallback (see Continue): force CanLoadBowlerScoreBoard true if the bowling side's
	// canLoad relay was missed at the innings swap, so the 2nd innings can never hang on the scorecard.
	private Coroutine _canLoadWatchdogCo;
	private IEnumerator ForceCanLoadBowlerScoreboardAfterTimeout()
	{
		yield return new WaitForSeconds(5f);
		_canLoadWatchdogCo = null;
		// Only force-advance if THIS batting scorecard is genuinely on-screen right now. Guard added after a
		// premature win/loss: at the innings swap the roles briefly flip (SetTeamIndex bat=4 then snapshot bat=9),
		// and the watchdog — armed while batting in the 1st innings — fired AFTER the swap while momentarily
		// BattingTeamIndex==myTeamIndex was still true on the now-BOWLING client, force-calling Continue() during
		// the transition and triggering a premature match-end cleanup (Edgegap session cleared → wrong win/loss,
		// 0 balls bowled in the 2nd innings). scoreCard.activeInHierarchy is FALSE on the bowling side (it shows
		// the bowling scorecard), so this fires only when the real batting scorecard is still stuck up.
		if (!CONTROLLER.CanLoadBowlerScoreBoard && CONTROLLER.PlayModeSelected == 8
			&& CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !CONTROLLER.gameCompleted
			&& scoreCard != null && scoreCard.activeInHierarchy)
		{
			ConstantsData_M.MpLog("[BattingScoreCard] canLoad relay missed at innings shift — force-advancing the stuck scorecard.");
			CONTROLLER.CanLoadBowlerScoreBoard = true;
			Continue();
		}
	}

	IEnumerator SendRPCWithRetry()
	{
		int retries = 0;
		while (retries < MaxRetries)
		{
			// Match-end crash guard: at the end of the match the CricketNetworkManager is destroyed
			// (disconnect / scene teardown), but this retry loop kept running and called a Cmd on the
			// destroyed instance -> MissingReferenceException (log 02-07 06:21, losing client) which aborted
			// the end-of-match UI transition and left that side stuck. Stop cleanly once it's gone.
			if (CricketNetworkManager.instance == null)
				yield break;
			if (GameConstants.isWithAI == false)
			{
				//photonView.RPC("RPC_ToggleCanLoadBowlerScoreCard", RpcTarget.Others, true);
				// Ghost-session guard. The 30-07 ritu_mp logs show THIS send throwing inside Mirror's
				// SendCommandInternal on a reloaded scene, which aborts the rest of the caller — and for
				// CmdSetBowler that means the bowler is never assigned or relayed, i.e. the two sides end up
				// with different bowlers. Only the sends actually seen throwing are guarded.
				if (CricketNetworkManager.ReadyToSend)
				    CricketNetworkManager.instance.CmdToggleCanLoadBowlerScoreCard(staticVariables.UserProfiledata.user._id, true);
				else
				    ConstantsData_M.MpLog("[GhostSend] ToggleCanLoadBowlerScoreCard(true) BLOCKED — network manager not ready (ghost session); caller continues instead of throwing.");

			}


			yield return new WaitForSeconds(RetryDelay);
			retries++;
		}

		// Handle case where all retries fail (optional)
	}

	IEnumerator SendRPCContinueWithRetry()
	{
		int retries = 0;
		while (retries < MaxRetries)
		{
			if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
			{
				yield break;
			}

			// Same match-end crash guard as SendRPCWithRetry: don't touch a destroyed CricketNetworkManager.
			if (CricketNetworkManager.instance == null)
				yield break;
			if (GameConstants.isWithAI == false)
				CricketNetworkManager.instance.CmdBattingScoreCardContinue(staticVariables.UserProfiledata.user._id);
			//{																																		   //Photon Removal
			//    photonView.RPC("RPC_Continue", RpcTarget.OthersBuffered);
			//}


			yield return new WaitForSeconds(RetryDelay);
			retries++;
		}
	}

	//Photon Removal [PunRPC]
	public void RPC_ToggleCanLoadBowlerScoreCard(bool Can)
	{
		if (scoreCard.activeInHierarchy && !Timer.activeInHierarchy)
		{
			if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
			{
				////Debug.Log("BOWLLER??");
				if (!Timer.activeInHierarchy)
				{
					Timer.SetActive(true);
					SetBattingScoreCardTimer();
				}
			}
		}
		CONTROLLER.CanLoadBowlerScoreBoard = Can;
	}

	public void TMResetBattingCard()
	{
		int battingTeamIndex = CONTROLLER.BattingTeamIndex;
		SetTeamInfo();
		for (int i = 0; i < batsman.Length; i++)
		{
			batsman[i].Highlight.sprite = batsmanStates[0];
			batsman[i].Name.font = normal;
			batsman[i].Name.text = CONTROLLER.TeamList[battingTeamIndex].PlayerList[i].ScoreboardName.ToUpper();
			batsman[i].Status.text = string.Empty.ToUpper();
			batsman[i].Runs.text = string.Empty;
			batsman[i].Balls.text = string.Empty;
			batsman[i].SR.text = string.Empty;
			batsman[i].Fours.text = string.Empty;
			batsman[i].Sixes.text = string.Empty;
			batsman[i].FOW.text = string.Empty;
		}
		ScoreText.text = GetScore(battingTeamIndex);
		ExtrasText.text = string.Empty;
		OversText.text = GetOvers(battingTeamIndex);
		if (OversText.text.Contains("("))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace("(", LocalizationData.localizationInstance.getText(651));
		}
		if (OversText.text.Contains(")"))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace(")", LocalizationData.localizationInstance.getText(652));
		}
	}

	private string GetScore(int TeamID)
	{
		int num;
		int num2;
		if (SelectedInnings == 1)
		{
			num = CONTROLLER.TeamList[TeamID].TMcurrentMatchWickets1;
			num2 = CONTROLLER.TeamList[TeamID].TMcurrentMatchScores1;
			int num3 = CONTROLLER.TeamList[TeamID].TMcurrentMatchBalls1 / 6;
			int num4 = CONTROLLER.TeamList[TeamID].TMcurrentMatchBalls1 % 6;
		}
		else
		{
			num = CONTROLLER.TeamList[TeamID].TMcurrentMatchWickets2;
			num2 = CONTROLLER.TeamList[TeamID].TMcurrentMatchScores2;
			int num3 = CONTROLLER.TeamList[TeamID].TMcurrentMatchBalls2 / 6;
			int num4 = CONTROLLER.TeamList[TeamID].TMcurrentMatchBalls2 % 6;
		}
		string text = string.Empty;
		if (SelectedInnings == 1)
		{
			if (CONTROLLER.TeamList[TeamID].isDeclared1)
			{
				text = " DEC";
			}
		}
		else if (CONTROLLER.TeamList[TeamID].isDeclared2)
		{
			text = " DEC";
		}
		return num2 + "/" + num + text;
	}

	private string GetOvers(int TeamID)
	{
		int num;
		int num2;
		if (SelectedInnings == 1)
		{
			num = CONTROLLER.TeamList[TeamID].TMcurrentMatchBalls1 / 6;
			num2 = CONTROLLER.TeamList[TeamID].TMcurrentMatchBalls1 % 6;
		}
		else
		{
			num = CONTROLLER.TeamList[TeamID].TMcurrentMatchBalls2 / 6;
			num2 = CONTROLLER.TeamList[TeamID].TMcurrentMatchBalls2 % 6;
		}
		return LocalizationData.localizationInstance.getText(184) + string.Empty + LocalizationData.localizationInstance.getText(650) + " " + num + "." + num2 + "(" + CONTROLLER.totalOvers + ")";
	}

	public void TMRestartGame()
	{
		int battingTeamIndex = CONTROLLER.BattingTeamIndex;
		SetTeamInfo();
		for (int i = 0; i < batsman.Length; i++)
		{
			if (CONTROLLER.currentInnings < 2)
			{
				if (CONTROLLER.TeamList[battingTeamIndex].PlayerList[i].BatsmanList.TMStatus1 == string.Empty)
				{
					batsman[i].Highlight.sprite = batsmanStates[0];
					batsman[i].Name.font = normal;
					batsman[i].Name.text = CONTROLLER.TeamList[battingTeamIndex].PlayerList[i].ScoreboardName.ToUpper();
					batsman[i].Status.text = string.Empty.ToUpper();
					batsman[i].Runs.text = string.Empty;
					batsman[i].Balls.text = string.Empty;
					batsman[i].SR.text = string.Empty;
					batsman[i].Fours.text = string.Empty;
					batsman[i].Sixes.text = string.Empty;
					batsman[i].FOW.text = string.Empty;
				}
				else
				{
					TMUpdateWicket(i);
				}
			}
			else if (CONTROLLER.TeamList[battingTeamIndex].PlayerList[i].BatsmanList.TMStatus2 == string.Empty)
			{
				batsman[i].Highlight.sprite = batsmanStates[0];
				batsman[i].Name.font = normal;
				batsman[i].Name.text = CONTROLLER.TeamList[battingTeamIndex].PlayerList[i].ScoreboardName.ToUpper();
				batsman[i].Status.text = string.Empty.ToUpper();
				batsman[i].Runs.text = string.Empty;
				batsman[i].Balls.text = string.Empty;
				batsman[i].SR.text = string.Empty;
				batsman[i].Fours.text = string.Empty;
				batsman[i].Sixes.text = string.Empty;
				batsman[i].FOW.text = string.Empty;
			}
			else
			{
				TMUpdateWicket(i);
			}
		}
		ScoreText.text = GetScore(battingTeamIndex);
		ExtrasText.text = string.Empty;
		OversText.text = GetOvers(battingTeamIndex);
		if (OversText.text.Contains("("))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace("(", LocalizationData.localizationInstance.getText(651));
		}
		if (OversText.text.Contains(")"))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace(")", LocalizationData.localizationInstance.getText(652));
		}
	}

	public void TMUpdateScoreCard()
	{
		Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite in flags)
		{
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
			{
				SCTeamFlag.sprite = sprite;
			}
		}
		SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
		int num = ((battingIndex != 0) ? CONTROLLER.BowlingTeamIndex : CONTROLLER.BattingTeamIndex);
		int strikerIndex = CONTROLLER.StrikerIndex;
		if (strikerIndex >= 0 && strikerIndex < CONTROLLER.TeamList[num].PlayerList.Length)
		{
			if (SelectedInnings == 1)
			{
				batsman[strikerIndex].Status.text = CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMStatus1.ToUpper();
				batsman[strikerIndex].Runs.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMRunsScored1;
				batsman[strikerIndex].Balls.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMBallsPlayed1;
				batsman[strikerIndex].SR.text = TMGetStrikeRate(num, strikerIndex);
				batsman[strikerIndex].Fours.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMFours1;
				batsman[strikerIndex].Sixes.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMSixes1;
			}
			else
			{
				batsman[strikerIndex].Status.text = CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMStatus2.ToUpper();
				batsman[strikerIndex].Runs.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMRunsScored2;
				batsman[strikerIndex].Balls.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMBallsPlayed2;
				batsman[strikerIndex].SR.text = TMGetStrikeRate(num, strikerIndex);
				batsman[strikerIndex].Fours.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMFours2;
				batsman[strikerIndex].Sixes.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMSixes2;
			}
			if (batsman[strikerIndex].Status.text == "not out".ToUpper())
			{
				batsman[strikerIndex].Highlight.sprite = batsmanStates[1];
				batsman[strikerIndex].Name.font = bold;
				batsman[strikerIndex].FOW.text = string.Empty;
			}
			else if (batsman[strikerIndex].Status.text == string.Empty)
			{
				batsman[strikerIndex].Name.font = normal;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[0];
			}
			else
			{
				batsman[strikerIndex].Name.font = normal;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[2];
			}
		}
		strikerIndex = CONTROLLER.NonStrikerIndex;
		if (strikerIndex >= 0 && strikerIndex < CONTROLLER.TeamList[num].PlayerList.Length)
		{
			if (SelectedInnings == 1)
			{
				batsman[strikerIndex].Status.text = CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMStatus1.ToUpper();
				batsman[strikerIndex].Runs.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMRunsScored1;
				batsman[strikerIndex].Balls.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMBallsPlayed1;
				batsman[strikerIndex].SR.text = TMGetStrikeRate(num, strikerIndex);
				batsman[strikerIndex].Fours.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMFours1;
				batsman[strikerIndex].Sixes.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMSixes1;
			}
			else
			{
				batsman[strikerIndex].Status.text = CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMStatus2.ToUpper();
				batsman[strikerIndex].Runs.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMRunsScored2;
				batsman[strikerIndex].Balls.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMBallsPlayed2;
				batsman[strikerIndex].SR.text = TMGetStrikeRate(num, strikerIndex);
				batsman[strikerIndex].Fours.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMFours2;
				batsman[strikerIndex].Sixes.text = string.Empty + CONTROLLER.TeamList[num].PlayerList[strikerIndex].BatsmanList.TMSixes2;
			}
			if (batsman[strikerIndex].Status.text == "not out".ToUpper())
			{
				batsman[strikerIndex].Name.font = bold;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[1];
				batsman[strikerIndex].FOW.text = string.Empty;
			}
			else if (batsman[strikerIndex].Status.text == string.Empty)
			{
				batsman[strikerIndex].Name.font = normal;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[0];
			}
			else
			{
				batsman[strikerIndex].Name.font = normal;
				batsman[strikerIndex].Highlight.sprite = batsmanStates[2];
			}
		}
		ScoreText.text = GetScore(CONTROLLER.BattingTeamIndex);
		ExtrasText.text = string.Empty;
		OversText.text = GetOvers(CONTROLLER.BattingTeamIndex);
		if (OversText.text.Contains("("))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace("(", LocalizationData.localizationInstance.getText(651));
		}
		if (OversText.text.Contains(")"))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace(")", LocalizationData.localizationInstance.getText(652));
		}
		CheckBatsmanStatus();
		Singleton<BattingScoreBoardPanelTransition>.instance.PanelTransition();
	}

	public string TMGetStrikeRate(int teamID, int playerID)
	{
		float num;
		float num2;
		if (SelectedInnings == 1)
		{
			num = CONTROLLER.TeamList[teamID].PlayerList[playerID].BatsmanList.TMRunsScored1;
			num2 = CONTROLLER.TeamList[teamID].PlayerList[playerID].BatsmanList.TMBallsPlayed1;
		}
		else
		{
			num = CONTROLLER.TeamList[teamID].PlayerList[playerID].BatsmanList.TMRunsScored2;
			num2 = CONTROLLER.TeamList[teamID].PlayerList[playerID].BatsmanList.TMBallsPlayed2;
		}
		return string.Concat(arg1: (num2 > 0f) ? ((int)(num / num2 * 100f)) : 0, arg0: string.Empty);
	}

	public void TMUpdateWicket(int playerID)
	{
		batsman[playerID].Name.font = normal;
		batsman[playerID].Highlight.sprite = batsmanStates[2];
		batsman[playerID].Name.text = GetBatsmanShortName(CONTROLLER.BattingTeamIndex, playerID).ToUpper();
		if (SelectedInnings == 1)
		{
			batsman[playerID].Runs.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMRunsScored1;
			batsman[playerID].Balls.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMBallsPlayed1;
			batsman[playerID].SR.text = TMGetStrikeRate(CONTROLLER.BattingTeamIndex, playerID);
			batsman[playerID].Fours.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMFours1;
			batsman[playerID].Sixes.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMSixes1;
			batsman[playerID].Status.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMStatus1.ToUpper();
			batsman[playerID].FOW.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMFOW1;
		}
		else
		{
			batsman[playerID].Runs.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMRunsScored2;
			batsman[playerID].Balls.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMBallsPlayed2;
			batsman[playerID].SR.text = TMGetStrikeRate(CONTROLLER.BattingTeamIndex, playerID);
			batsman[playerID].Fours.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMFours2;
			batsman[playerID].Sixes.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMSixes2;
			batsman[playerID].Status.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMStatus2.ToUpper();
			batsman[playerID].FOW.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.TMFOW2;
		}
	}

	private void TMDisplayList(int TeamID)
	{
		for (int i = 0; i < batsman.Length; i++)
		{
			if (SelectedInnings == 1)
			{
				if (CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMStatus1 == string.Empty || CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMStatus1 == null)
				{
					batsman[i].Name.font = normal;
					batsman[i].Highlight.sprite = batsmanStates[0];
					batsman[i].Name.text = CONTROLLER.TeamList[TeamID].PlayerList[i].ScoreboardName.ToUpper();
					batsman[i].Status.text = string.Empty.ToUpper();
					batsman[i].Runs.text = string.Empty;
					batsman[i].Balls.text = string.Empty;
					batsman[i].SR.text = string.Empty;
					batsman[i].Fours.text = string.Empty;
					batsman[i].Sixes.text = string.Empty;
					batsman[i].FOW.text = string.Empty;
					continue;
				}
				if (CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMStatus1 == "not out".ToUpper())
				{
					batsman[i].Name.font = bold;
					batsman[i].Highlight.sprite = batsmanStates[1];
					batsman[i].FOW.text = string.Empty;
				}
				else
				{
					batsman[i].Name.font = normal;
					batsman[i].Highlight.sprite = batsmanStates[2];
					batsman[i].FOW.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMFOW1;
				}
				batsman[i].Name.text = GetBatsmanShortName(TeamID, i).ToUpper();
				batsman[i].Runs.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMRunsScored1;
				batsman[i].Balls.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMBallsPlayed1;
				batsman[i].SR.text = TMGetStrikeRate(TeamID, i);
				batsman[i].Fours.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMFours1;
				batsman[i].Sixes.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMSixes1;
				batsman[i].Status.text = CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMStatus1.ToUpper();
			}
			else if (CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMStatus2 == string.Empty || CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMStatus2 == null)
			{
				batsman[i].Name.font = normal;
				batsman[i].Highlight.sprite = batsmanStates[0];
				batsman[i].Name.text = CONTROLLER.TeamList[TeamID].PlayerList[i].ScoreboardName.ToUpper();
				batsman[i].Status.text = string.Empty.ToUpper();
				batsman[i].Runs.text = string.Empty;
				batsman[i].Balls.text = string.Empty;
				batsman[i].SR.text = string.Empty;
				batsman[i].Fours.text = string.Empty;
				batsman[i].Sixes.text = string.Empty;
				batsman[i].FOW.text = string.Empty;
			}
			else
			{
				if (CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMStatus2 == "not out".ToUpper())
				{
					batsman[i].Name.font = bold;
					batsman[i].Highlight.sprite = batsmanStates[1];
					batsman[i].FOW.text = string.Empty;
				}
				else
				{
					batsman[i].Name.font = normal;
					batsman[i].Highlight.sprite = batsmanStates[2];
					batsman[i].FOW.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMFOW2;
				}
				batsman[i].Name.text = GetBatsmanShortName(TeamID, i).ToUpper();
				batsman[i].Runs.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMRunsScored2;
				batsman[i].Balls.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMBallsPlayed2;
				batsman[i].SR.text = TMGetStrikeRate(TeamID, i);
				batsman[i].Fours.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMFours2;
				batsman[i].Sixes.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMSixes2;
				batsman[i].Status.text = CONTROLLER.TeamList[TeamID].PlayerList[i].BatsmanList.TMStatus2.ToUpper();
			}
		}
		ScoreText.text = GetScore(CONTROLLER.BattingTeamIndex);
		ExtrasText.text = string.Empty;
		OversText.text = GetOvers(CONTROLLER.BattingTeamIndex);
		if (OversText.text.Contains("("))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace("(", LocalizationData.localizationInstance.getText(651));
		}
		if (OversText.text.Contains(")"))
		{
			//Debug.Log(OversText.text);
			OversText.text = OversText.text.Replace(")", LocalizationData.localizationInstance.getText(652));
		}
	}
}
