using DG.Tweening;

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class BowlingScoreCard : Singleton<BowlingScoreCard>
{
	//Photon Removal	public PhotonView photonView;

	private GroundController groundControllerScript;

	public Font bold;

	public Font normal;

	public GameObject scoreCard;

	// FINAL stuck-panel backstop — see BattingScoreCard.Update for the rationale. Same blanket 30s timer,
	// forcing the same proven path ForceCanLoadGroundAfterTimeout uses.
	private float _panelStuckAt = -1f;

	private void Update()
	{
		if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
		if (scoreCard == null || !scoreCard.activeInHierarchy) { _panelStuckAt = -1f; return; }
		if (CricketNetworkManager.RestorePending) { return; }
		if (_panelStuckAt < 0f) { _panelStuckAt = Time.unscaledTime; return; }
		// Right after a reconnect the panel-closing relay was missed while disconnected — don't sit the
		// full blanket window; the staying opponent has usually LONG moved on (13-07 over-boundary repro).
		float stuckLimit = (Time.realtimeSinceStartup - CricketNetworkManager.LastRestoreRealtime < 60f) ? 8f : 30f;
		if (Time.unscaledTime - _panelStuckAt < stuckLimit) return;
		_panelStuckAt = Time.unscaledTime;
		ConstantsData_M.MpLog($"[StuckPanel] BowlingScoreCard open >{stuckLimit}s — force-advancing (blanket backstop).");
		CONTROLLER.CanLoadGround = true;
		Continue();
	}

	public GameObject BG;

	public Button ContinueBtn;

	public Button BackBtn;

	public Sprite[] bowlerStates;

	public Text ScoreText;

	public Text ExtrasText;

	public Text OversText;

	public Image SCTeamFlag;

	public Image BtnTeamFlag;

	public Text SCTeamName;

	public Text BtnTeamName;

	public BowlerDetails[] bowler;

	private bool firstTimeHide;

	private List<int> BowlersIndexArray;

	// A bowler pick that arrived before BowlersIndexArray existed (reconnect scene reload). -1 = none.
	private int _pendingBowlerListIndex = -1;

	// Online, the BOWLING PLAYER chooses the next over's bowler — the game no longer auto-picks one for them
	// (user decision 2026-08-12). Set when an over/innings boundary opens the selection, cleared the moment the
	// player taps a bowler. It is only a SAFETY-NET marker: if the card is dismissed with no pick made,
	// Continue() falls back to the old GetRandomBowler() so a match can never stall waiting for a choice.
	private bool _awaitingPlayerBowlerPick;

	private int firstSpellOver;

	public static int currentBowler = -1;//mohsinn

	private int lastBowlerIndex = -1;

	private int TMLastbowler = -1;

	private GameObject introCamGO;

	private GameObject introCameraPivot;

	private Camera introCamera;

	private int previousBowler = -1;

	private int SelectedIndex = 1;

	public Text SelectedInningsText;

	public static bool fisrtTimeShow = true;

	private Transform _transform;

	private bool fromDisplayList;

	public static bool newOverCalled;

	//Multiplayer

	private float secCount;

	[SerializeField] private Image timerImage;

	[SerializeField] private Text timerText;

	[SerializeField] public GameObject Timer;

	DG.Tweening.Sequence s;

	protected void Awake()
	{
		firstTimeHide = false;
		groundControllerScript = GameObject.Find("GroundController").GetComponent<GroundController>();
		Hide(boolean: true);
		////Debug.Log("SKIPPP kR DIyaa HAII Humnee");

	}

	public void ResetBowlingCard()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			if (CONTROLLER.PlayModeSelected != 8 || CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
			{
				////Debug.Log("BATTT");
				currentBowler = -1;
				lastBowlerIndex = -1;
			}
			int bowlingTeamIndex = CONTROLLER.BowlingTeamIndex;
			SetTeamInfo();
			GetBowlersInTeam(bowlingTeamIndex);
			for (int i = 0; i < bowler.Length; i++)
			{
				if (CONTROLLER.BowlingTeamIndex == CONTROLLER.opponentTeamIndex)
				{
					bowler[i].Name.font = normal;
					bowler[i].Highlight.sprite = bowlerStates[0];
					bowler[i].ClickBtn.enabled = false;
				}
				else
				{
					bowler[i].ClickBtn.enabled = true;
				}
				if (i < BowlersIndexArray.Count)
				{
					int num = BowlersIndexArray[i];
					bowler[i].Name.text = CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[num].ScoreboardName.ToUpper();
					bowler[i].Type.text = GetBowlerType(bowlingTeamIndex, num).ToUpper();
					bowler[i].Overs.text = "0.0";
					bowler[i].Maiden.text = "0";
					bowler[i].Runs.text = "0";
					bowler[i].Wickets.text = "0";
					bowler[i].ERate.text = GetEconRate(bowlingTeamIndex, num);
					if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
					{
						bowler[i].ClickBtn.enabled = true;
					}
				}
				else
				{
					bowler[i].Name.text = string.Empty.ToUpper();
					bowler[i].Type.text = string.Empty.ToUpper();
					bowler[i].Overs.text = string.Empty;
					bowler[i].Maiden.text = string.Empty;
					bowler[i].Runs.text = string.Empty;
					bowler[i].Wickets.text = string.Empty;
					bowler[i].ERate.text = string.Empty;
					bowler[i].ClickBtn.enabled = false;
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
		else
		{
			TMResetBowlingCard();
		}
	}

	private void SetTeamInfo()
	{
		if (CONTROLLER.PlayModeSelected >= 9)
		{
			return;
		}
		if (CONTROLLER.currentInnings == 0)
		{
			Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
			foreach (Sprite sprite in flags)
			{
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
				{
					SCTeamFlag.sprite = sprite;
				}
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
				{
					BtnTeamFlag.sprite = sprite;
				}
			}
			SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
			BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
			return;
		}
		Sprite[] flags2 = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite2 in flags2)
		{
			if (sprite2.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
			{
				SCTeamFlag.sprite = sprite2;
			}
			if (sprite2.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
			{
				BtnTeamFlag.sprite = sprite2;
			}
		}
		SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
		BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
	}

	public void UpdateScoreCard()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
			foreach (Sprite sprite in flags)
			{
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
				{
					SCTeamFlag.sprite = sprite;
				}
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
				{
					BtnTeamFlag.sprite = sprite;
				}
			}
			SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
			BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
			int bowlingTeamIndex = CONTROLLER.BowlingTeamIndex;
			int currentBowlerIndex = CONTROLLER.CurrentBowlerIndex;
			////Debug.Log("Bowler Idx : "+CONTROLLER.CurrentBowlerIndex);
			////Debug.Log("Bowler : " + currentBowler);
			if (!CONTROLLER.isFromAutoPlay && currentBowler >= 0 && currentBowler < bowler.Length)
			{
				bowler[currentBowler].Name.font = bold;
				bowler[currentBowler].Highlight.sprite = bowlerStates[1];
				bowler[currentBowler].Overs.text = GetOversBowled(bowlingTeamIndex, currentBowlerIndex);
				bowler[currentBowler].Maiden.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.Maiden;
				bowler[currentBowler].Runs.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.RunsGiven;
				bowler[currentBowler].Wickets.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.Wicket;
				bowler[currentBowler].ERate.text = GetEconRate(bowlingTeamIndex, currentBowlerIndex);
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
		else
		{
			TMUpdateScoreCard();
		}
	}

	public string GetOversBowled(int teamID, int playerID)
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			int num = CONTROLLER.TeamList[teamID].PlayerList[playerID].BowlerList.BallsBowled / 6;
			int num2 = CONTROLLER.TeamList[teamID].PlayerList[playerID].BowlerList.BallsBowled % 6;
			return num + "." + num2;
		}
		return TMGetOversBowled(teamID, playerID);
	}

	//Photon Removal	[PunRPC]
	public void RPC_SetBowler(int CurrentBowler)
	{
		// Guard against an invalid/sentinel bowler index. CurrentBowler can arrive as -1 — the static
		// BowlingScoreCard.currentBowler default, the syncedBowlerListIndex SyncVar default (-1) firing its
		// OnBowlerIndexSynced hook at spawn, or a reconnect re-broadcast before GetRandomBowler picks. Without
		// this, currentBowler became -1 and EliminateBowlerKeeper (BowlersIndexArray[-1]) + DisplayList
		// (bowler[-1]) threw IndexOutOfRange, crashing BowlingScoreCard.Continue() at the over-start scorecard
		// BEFORE a real bowler could be set -> the match DEADLOCKED at game start (tester: "game start hote hi
		// stuck"). Ignore the invalid index; a valid RPC_SetBowler follows from GetRandomBowler / the sync.
		if (BowlersIndexArray == null || CurrentBowler < 0 || CurrentBowler >= BowlersIndexArray.Count)
		{
			// REMEMBER a pick that arrived before the list existed. Dropping it silently is how the two
			// clients ended up on different bowlers: the relay lands during a reconnect scene reload while
			// BowlersIndexArray is still null (log: "ignored invalid index 2 (count=-1)"), nothing re-sends
			// it, and this client later falls back to its own selectRandomBowler() — a LOCAL random pick.
			// One side then bowls spin while the other bowls fast.
			if (CurrentBowler >= 0) _pendingBowlerListIndex = CurrentBowler;
			ConstantsData_M.MpLog($"[BowlingScoreCard][RPC_SetBowler] deferred index {CurrentBowler} (count={(BowlersIndexArray != null ? BowlersIndexArray.Count : -1)}) — list not built yet; will re-apply when it is.");
			return;
		}

		if (currentBowler >= 0 && currentBowler < BowlersIndexArray.Count && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			bowler[currentBowler].Name.font = normal;
			bowler[currentBowler].Highlight.sprite = bowlerStates[0];
		}
		currentBowler = CurrentBowler;


		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8)
		{

			//currentBowler = CurrentBowler;
			EliminateBowlerKeeper();
			bowler[currentBowler].Name.font = bold;
			bowler[currentBowler].Highlight.sprite = bowlerStates[1];
			// Sync CONTROLLER.CurrentBowlerIndex so SetGameDatas() on the batting player
			// uses the correct bowler type/hand for spin direction and swing angle.
			// Without this, batting player uses a stale index -> wrong BowlerHand/BowlerType
			// -> ball spins in opposite direction -> keeper misses on batting player side.
			if (currentBowler >= 0 && currentBowler < BowlersIndexArray.Count)
			{
				CONTROLLER.CurrentBowlerIndex = BowlersIndexArray[currentBowler];
			}
		}


	}

	// Maps a scorecard POSITION (what CmdSetBowler/syncedBowlerListIndex carry) to the ABSOLUTE PlayerList
	// index everything else uses. -1 when the list has not been built yet or the position is out of range.
	public int AbsoluteIndexForScorecardPosition(int scorecardPosition)
	{
		if (BowlersIndexArray == null || scorecardPosition < 0 || scorecardPosition >= BowlersIndexArray.Count)
			return -1;
		return BowlersIndexArray[scorecardPosition];
	}

	public void RestoreBowlerIndexOnReconnect()
	{
		if (CricketNetworkManager.instance == null || BowlersIndexArray == null) return;
		int sc = CricketNetworkManager.instance.syncedBowlerListIndex;
		if (sc >= 0 && sc < BowlersIndexArray.Count)
		{
			currentBowler = sc;
			CONTROLLER.CurrentBowlerIndex = BowlersIndexArray[sc];
			ConstantsData_M.MpLog($"[BowlingScoreCard][RestoreBowlerIndexOnReconnect] bowling reconnecter restored CurrentBowlerIndex={CONTROLLER.CurrentBowlerIndex} (scorecardIdx={sc}).");
		}
	}

	public void BowlerSelected(int index)
	{
		// Spin cap (tester #3): reject a tap on a spinner that has hit its over cap, as long as a legal
		// alternative exists (softlock-guarded inside SpinCapReached). Belt-and-suspenders over the
		// grey-out, in case the disabled button was bypassed. Only the bowling client's own pick.
		if (CONTROLLER.PlayModeSelected < 9 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex
			&& index >= 0 && index < BowlersIndexArray.Count && SpinCapReached(index))
		{
			return;
		}
		if (CONTROLLER.PlayModeSelected < 9 && CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex && SCTeamName.text == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper())
		{
			if (currentBowler >= 0 && currentBowler < BowlersIndexArray.Count)
			{
				bowler[currentBowler].Name.font = normal;
				bowler[currentBowler].Highlight.sprite = bowlerStates[0];
			}
			currentBowler = index;
			EliminateBowlerKeeper();
			bowler[currentBowler].Name.font = bold;
			bowler[currentBowler].Highlight.sprite = bowlerStates[1];
		}

		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
		{
			if (GameConstants.isWithAI == false)
			{
				//Photon Removal     photonView.RPC("RPC_SetBowler", RpcTarget.AllBuffered, index);
				// Ghost-session guard. The 30-07 ritu_mp logs show THIS send throwing inside Mirror's
				// SendCommandInternal on a reloaded scene, which aborts the rest of the caller — and for
				// CmdSetBowler that means the bowler is never assigned or relayed, i.e. the two sides end up
				// with different bowlers. Only the sends actually seen throwing are guarded.
				if (CricketNetworkManager.ReadyToSend)
				    CricketNetworkManager.instance.CmdSetBowler(index);
				else
				    ConstantsData_M.MpLog("[GhostSend] SetBowler(index) BLOCKED — network manager not ready (ghost session); caller continues instead of throwing.");
				// Follow the position with the ABSOLUTE index. The position above is resolved through each
				// client's OWN BowlersIndexArray, so the two sides can land on different players from the
				// same number — see CmdSyncBowlerAbsolute. Same guard, and a no-op on clients that already
				// agree.
				if (CricketNetworkManager.ReadyToSend)
				{
					_awaitingPlayerBowlerPick = false;   // the player made the choice — no fallback needed
				int _abs = AbsoluteIndexForScorecardPosition(index);
					if (_abs >= 0)
						CricketNetworkManager.instance.CmdSyncBowlerAbsolute(staticVariables.UserProfiledata.user._id, _abs);
				}
			}
			if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8)
			{
				if (currentBowler >= 0 && currentBowler < BowlersIndexArray.Count)
				{
					bowler[currentBowler].Name.font = normal;
					bowler[currentBowler].Highlight.sprite = bowlerStates[0];
				}
				currentBowler = index;
				EliminateBowlerKeeper();
				bowler[currentBowler].Name.font = bold;
				bowler[currentBowler].Highlight.sprite = bowlerStates[1];
			}
		}
	}

	private void GetBowlersInTeam(int TeamID)
	{
		if (CONTROLLER.PlayModeSelected >= 9)
		{
			return;
		}
		BowlersIndexArray = new List<int>();
		int num = CONTROLLER.TeamList[TeamID].PlayerList.Length;
		for (int i = 0; i < num; i++)
		{
			string style = CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.Style;
			if (style != string.Empty && style != null)
			{
				BowlersIndexArray.Add(i);
			}
		}
		firstSpellOver = (int)((float)CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 0.2f);
		// Dropped-pick self-heal (tester #4): the bowling side's pick arrives once via RpcSetBowler, and
		// RPC_SetBowler silently DROPS it when this array doesn't exist yet (intros are not synced between
		// clients, so the relay can beat ResetBowlingCard here) — the batting client then rendered the first
		// balls with the default/stale bowler and visibly swapped models mid-over when a later re-send landed.
		// The pick is durable on the server (syncedBowlerListIndex, reset to -1 at the toss/innings shift by
		// CmdSetBattingTeams so it can never be a previous match's), so re-apply it the moment the array is
		// ready. RPC_SetBowler is idempotent for an already-applied index.
		if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
			&& TeamID == CONTROLLER.BowlingTeamIndex
			&& CricketNetworkManager.instance != null
			&& CricketNetworkManager.instance.syncedBowlerListIndex >= 0)
		{
			RPC_SetBowler(CricketNetworkManager.instance.syncedBowlerListIndex);
		}
		// Apply a pick that was deferred above. The team gate on the self-heal cannot be trusted during a
		// reconnect — CONTROLLER.BowlingTeamIndex is momentarily re-derived and can be the flipped value —
		// so a deferred relay must still land once the list it needed actually exists.
		if (_pendingBowlerListIndex >= 0 && BowlersIndexArray != null
			&& _pendingBowlerListIndex < BowlersIndexArray.Count)
		{
			int _p = _pendingBowlerListIndex;
			_pendingBowlerListIndex = -1;
			ConstantsData_M.MpLog($"[BowlingScoreCard] Re-applying deferred bowler pick {_p} now that the list is built ({BowlersIndexArray.Count} bowlers).");
			RPC_SetBowler(_p);
		}
	}

	// The bowler's identity travels the wire as a SCORECARD POSITION (syncedBowlerListIndex = an index into
	// BowlersIndexArray), but that list is built PER CLIENT from CONTROLLER.TeamList[bowlingTeamIndex] and is
	// filtered to players with a bowling Style. Any disagreement about which team is bowling — which a
	// reconnect can produce for a moment — makes the same position resolve to a DIFFERENT player, and the two
	// sides then bowl different types (tester: "ik side spin opponent side fast", logs showing
	// RPC_SetBowler=2 -> bowlerIdx=6 medium on one device and RPC_SetBowler=5 -> bowlerIdx=9 fast on the other).
	// The match snapshot carries the ABSOLUTE PlayerList index, which cannot be misread. Map it back to this
	// client's own position so both sides converge on the same man whatever their list ordering.
	/// <summary>
	/// True when <paramref name="absoluteBowlerIndex"/> names a bowler in the list this card is currently
	/// built from. A restored identity is an absolute PlayerList index taken from the OTHER client, and the
	/// snapshot carries no roster with it — so after an innings shift, or against a differently-built team,
	/// the number can name a player who does not bowl here. Callers use this to reject such an index before
	/// installing it. While the list has not been built yet nothing can be disproved, so this answers true
	/// and leaves the existing deferred-pick path to do the work.
	/// </summary>
	public bool CanTrustAbsoluteBowler(int absoluteBowlerIndex)
	{
		if (absoluteBowlerIndex < 0) return false;
		if (BowlersIndexArray == null || BowlersIndexArray.Count == 0) return true;
		return BowlersIndexArray.IndexOf(absoluteBowlerIndex) >= 0;
	}

	public void RestoreBowlerFromAbsoluteIndex(int absoluteBowlerIndex)
	{
		if (absoluteBowlerIndex < 0 || BowlersIndexArray == null) return;
		int pos = BowlersIndexArray.IndexOf(absoluteBowlerIndex);
		if (pos < 0)
		{
			ConstantsData_M.MpLog($"[BowlingScoreCard][RestoreBowlerFromAbsolute] absolute index {absoluteBowlerIndex} is not in this client's bowler list ({BowlersIndexArray.Count} entries) — leaving the current pick alone.");
			return;
		}
		if (currentBowler == pos && CONTROLLER.CurrentBowlerIndex == absoluteBowlerIndex) return;
		ConstantsData_M.MpLog($"[BowlingScoreCard][RestoreBowlerFromAbsolute] absolute={absoluteBowlerIndex} -> local position {pos} (was position {currentBowler}, CurrentBowlerIndex {CONTROLLER.CurrentBowlerIndex}).");
		RPC_SetBowler(pos);
		CONTROLLER.CurrentBowlerIndex = absoluteBowlerIndex;
	}

	private string GetBowlerType(int teamID, int bowlerIndex)
	{
		if (CONTROLLER.PlayModeSelected < 9)
		{
			string result = string.Empty;
			if (CONTROLLER.TeamList[teamID].PlayerList[bowlerIndex].BowlerList.bowlingRank == "0")
			{
				result = ((!(CONTROLLER.TeamList[teamID].PlayerList[bowlerIndex].BowlerList.BowlingHand == "L")) ? "RIGHT ARM FAST" : "LEFT ARM FAST");
			}
			else if (CONTROLLER.TeamList[teamID].PlayerList[bowlerIndex].BowlerList.bowlingRank == "1")
			{
				result = ((!(CONTROLLER.TeamList[teamID].PlayerList[bowlerIndex].BowlerList.BowlingHand == "L")) ? "RIGHT OFF SPIN" : "LEFT OFF SPIN");
			}
			else if (CONTROLLER.TeamList[teamID].PlayerList[bowlerIndex].BowlerList.bowlingRank == "2")
			{
				result = ((!(CONTROLLER.TeamList[teamID].PlayerList[bowlerIndex].BowlerList.BowlingHand == "L")) ? "RIGHT LEG SPIN" : "LEFT LEG SPIN");
			}
			else if (CONTROLLER.TeamList[teamID].PlayerList[bowlerIndex].BowlerList.bowlingRank == "3")
			{
				result = ((!(CONTROLLER.TeamList[teamID].PlayerList[bowlerIndex].BowlerList.BowlingHand == "L")) ? "RIGHT ARM MEDIUM" : "LEFT ARM MEDIUM");
			}
			return result;
		}
		return "-";
	}

	public string GetEconRate(int TeamID, int playerID)
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{

		}
		return TMGetEconRate(TeamID, playerID);
	}

	public void GetRandomBowler()
	{
		if (CONTROLLER.PlayModeSelected >= 9)
		{
			return;
		}



		// Open-overs PACE rule (morning#6): bias the opening overs to PACE — first 2 overs a non-spin
		// (fast/medium) bowler, the 3rd a spinner (the usual opening pattern). The auto-picker was choosing
		// spin for all open overs. Deterministic (both clients share team data + only the bowling client picks
		// then syncs). Falls through to the existing spell logic if the desired type isn't available (all-pace
		// or all-spin attack) → openPick stays -1.
		int _openOver = GetCurrentOver();   // 1-indexed
		int _openPick = (CONTROLLER.PlayModeSelected != 7)
			? (_openOver <= 2 ? FindOpenBowler(false) : (_openOver == 3 ? FindOpenBowler(true) : -1))
			: -1;
		int currentSpell = GetCurrentSpell();
		if (_openPick >= 0)
		{
			currentBowler = _openPick;
		}
		else if (currentSpell == 1)
		{
			int currentOver = GetCurrentOver();
			if (currentOver % 2 == 0)
			{
				if (lastBowlerIndex != 1)
				{
					currentBowler = 1;
				}
				else
				{
					currentBowler = 0;
				}
			}
			else if (lastBowlerIndex != 0)
			{
				currentBowler = 0;
			}
			else
			{
				currentBowler = 1;
			}
		}
		else
		{
			currentBowler = UnityEngine.Random.Range(0, BowlersIndexArray.Count);
			if (currentBowler == TMLastbowler)
			{
				if (currentBowler != 0)
				{
					currentBowler--;
				}
				else
				{
					currentBowler++;
				}
			}
			if (!CanBowlCurrentOver(currentBowler))
			{
				selectRandomBowler();
			}
		}
		if (BowlersIndexArray.Count <= 5)
		{
			fromDisplayList = false;
			checkBowlerSpell();
		}
		EliminateBowlerKeeper();

		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
		{



			if (GameConstants.isWithAI == false)
			{
				//Photon Removal       photonView.RPC("RPC_SetBowler", RpcTarget.AllBuffered, currentBowler);
				// Ghost-session guard. The 30-07 ritu_mp logs show THIS send throwing inside Mirror's
				// SendCommandInternal on a reloaded scene, which aborts the rest of the caller — and for
				// CmdSetBowler that means the bowler is never assigned or relayed, i.e. the two sides end up
				// with different bowlers. Only the sends actually seen throwing are guarded.
				if (CricketNetworkManager.ReadyToSend)
				    CricketNetworkManager.instance.CmdSetBowler(currentBowler);
				else
				    ConstantsData_M.MpLog("[GhostSend] SetBowler(currentBowler) BLOCKED — network manager not ready (ghost session); caller continues instead of throwing.");
				// Follow the position with the ABSOLUTE index. The position above is resolved through each
				// client's OWN BowlersIndexArray, so the two sides can land on different players from the
				// same number — see CmdSyncBowlerAbsolute. Same guard, and a no-op on clients that already
				// agree.
				if (CricketNetworkManager.ReadyToSend)
				{
					int _abs = AbsoluteIndexForScorecardPosition(currentBowler);
					if (_abs >= 0)
						CricketNetworkManager.instance.CmdSyncBowlerAbsolute(staticVariables.UserProfiledata.user._id, _abs);
				}
			}


		}


		if (CONTROLLER.isAutoplay)
		{
			CONTROLLER.CurrentBowlerIndex = BowlersIndexArray[currentBowler];
			lastBowlerIndex = currentBowler;
		}


		bowler[currentBowler].Name.font = bold;
		bowler[currentBowler].Highlight.sprite = bowlerStates[1];
		CONTROLLER.CurrentBowlerIndex = BowlersIndexArray[currentBowler];
		lastBowlerIndex = currentBowler;


	}


	private void selectRandomBowler()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < BowlersIndexArray.Count; i++)
			{
				int ballsBowled = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[i]].BowlerList.BallsBowled;
				int num = (int)((float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6) * 0.2f);
				int num2 = num - ballsBowled;
				if (num2 > 0 && i != lastBowlerIndex)
				{
					list.Add(i);
				}
			}
			if (list.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, list.Count);
				currentBowler = list[index];
			}
			//if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
			//{
			//    photonView.RPC("RPC_SetBowler", RpcTarget.AllBuffered, currentBowler);
			//}
		}
		else
		{
			TMselectRandomBowler();
		}

	}

	private void EliminateBowlerKeeper()
	{
		if (CONTROLLER.PlayModeSelected >= 9)
		{
			return;
		}
		// No valid bowler picked yet (currentBowler is a static defaulting to -1) -> BowlersIndexArray[-1] would
		// throw. Skip the fielder-array setup until a real bowler is chosen; GetRandomBowler re-runs it then.
		if (BowlersIndexArray == null || currentBowler < 0 || currentBowler >= BowlersIndexArray.Count)
		{
			// Returning here leaves FielderArray as the PREVIOUS over built it — excluding the old bowler and
			// still INCLUDING the new one. The new bowler is then placed as a fielder as well as being
			// rendered at the bowling end, which is the tester's "bowlers apas m merge ho jate hain": one
			// player drawn twice. The deferred-pick path re-applies the real index and re-runs this, so the
			// window should now be brief — log it so a lingering one is visible.
			ConstantsData_M.MpLog($"[BowlingScoreCard][EliminateBowlerKeeper] SKIPPED — no valid bowler yet (currentBowler={currentBowler}, list={(BowlersIndexArray != null ? BowlersIndexArray.Count : -1)}). FielderArray still holds the previous over's exclusion, so the incoming bowler may also stand as a fielder until the real pick lands.");
			return;
		}
		int num = 0;
		for (int i = 0; i <= CONTROLLER.totalWickets; i++)
		{
			if (i != BowlersIndexArray[currentBowler] && i != CONTROLLER.wickerKeeperIndex && i < CONTROLLER.FielderArray.Length)
			{
				CONTROLLER.FielderArray[num] = i;
				num++;
			}
		}
	}

	// ── Spin-bowler over cap (tester report #3) ──────────────────────────────────────────────────
	// In short matches a spin bowler could dominate the attack (e.g. 2 of 3 overs). bowlingRank
	// "1"=off spin, "2"=leg spin → spinner. A spinner may bowl at most ceil(totalOvers/3) overs (>=1).
	// Enforced through the SAME chokepoints the generic cap uses — CheckRemainingOvers (grey-out + the
	// AI's CanBowlCurrentOver/selectRandomBowler) and BowlerSelected (human tap). Both MP clients run
	// identical deterministic data so they stay consistent. Softlock-guarded: a spinner is NEVER capped
	// when no other bowler is free to take the over.
	private bool IsSpinBowler(int bowlerArrayIndex)
	{
		if (bowlerArrayIndex < 0 || bowlerArrayIndex >= BowlersIndexArray.Count) return false;
		string rank = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[bowlerArrayIndex]].BowlerList.bowlingRank;
		return rank == "1" || rank == "2";
	}

	// Open-overs PACE rule (morning#6): first ELIGIBLE bowler of the requested type (wantSpin=false → pace/medium,
	// true → spinner) that can legally bowl this over (CanBowlCurrentOver = not the last over's bowler + under the
	// over/spin cap). Deterministic (in-array order). -1 if none of the desired type is available — caller then
	// falls back to the normal selection.
	private int FindOpenBowler(bool wantSpin)
	{
		for (int i = 0; i < BowlersIndexArray.Count; i++)
		{
			if (IsSpinBowler(i) != wantSpin) continue;
			if (!CanBowlCurrentOver(i)) continue;
			return i;
		}
		return -1;
	}

	private int SpinOverCap()
	{
		int totalOvers = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex];
		int cap = (totalOvers + 2) / 3; // integer ceil(totalOvers / 3)
		return cap < 1 ? 1 : cap;
	}

	private int OversBowledByArrayIndex(int bowlerArrayIndex)
	{
		return CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[bowlerArrayIndex]].BowlerList.BallsBowled / 6;
	}

	// True only if some OTHER bowler (not this one, not the last over's bowler) can legally bowl now —
	// either a non-spinner, or a spinner still under its own cap. Prevents a spin-cap softlock.
	private bool HasNonSpinAlternative(int excludeIndex)
	{
		for (int i = 0; i < BowlersIndexArray.Count; i++)
		{
			if (i == excludeIndex || i == lastBowlerIndex) continue;
			if (!IsSpinBowler(i)) return true;
			if (OversBowledByArrayIndex(i) < SpinOverCap()) return true;
		}
		return false;
	}

	private bool SpinCapReached(int bowlerArrayIndex)
	{
		if (CONTROLLER.PlayModeSelected >= 9) return false;
		if (!IsSpinBowler(bowlerArrayIndex)) return false;
		if (OversBowledByArrayIndex(bowlerArrayIndex) < SpinOverCap()) return false;
		return HasNonSpinAlternative(bowlerArrayIndex); // only enforce if an alternative exists
	}

	private bool CheckRemainingOvers(int bowlerindex)
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//return false;
		}
		if (CONTROLLER.PlayModeSelected < 9)
		{
			int ballsBowled = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[bowlerindex]].BowlerList.BallsBowled;
			int num = (int)((float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6) * 0.2f);
			if (num > ballsBowled)
			{
				if (SpinCapReached(bowlerindex)) return true; // spin over cap reached → treat as no overs left
				return false;
			}
			return true;
		}
		return false;
	}

	private int GetCurrentSpell()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			int num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6;
			if (num < firstSpellOver)
			{
				return 1;
			}
			return 2;
		}
		return TMGetCurrentSpell();
	}

	private int GetCurrentOver()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			int num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6;
			return num + 1;
		}
		return TMGetCurrentOver();
	}

	private void checkBowlerSpell()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			int num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6;
			bool flag = false;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			List<int> list = new List<int>();
			for (int i = 0; i < BowlersIndexArray.Count; i++)
			{
				num2 = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[i]].BowlerList.BallsBowled;
				num3 = (int)((float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6) * 0.2f);
				num5 = (num3 - num2) / 6;
				if (num6 < num5)
				{
					list = new List<int>();
					num6 = num5;
					num7 = i;
					list.Add(i);
				}
				else if (num6 == num5)
				{
					list.Add(i);
				}
			}
			num5 = 0;
			for (int j = 0; j < BowlersIndexArray.Count; j++)
			{
				if (j != num7)
				{
					num2 = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[j]].BowlerList.BallsBowled;
					num3 = (int)((float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6) * 0.2f);
					num5 += (num3 - num2) / 6;
				}
			}
			for (int k = 0; k < BowlersIndexArray.Count; k++)
			{
				num2 = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[k]].BowlerList.BallsBowled;
				num3 = (int)((float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6) * 0.2f);
				num4 = num3 - num2;
				if (num2 <= CONTROLLER.oversSelectedIndex * 6 - 6 && num6 > num5 && list.Count <= 1 && k != lastBowlerIndex)
				{
					flag = true;
					currentBowler = num7;
				}
			}
			for (int l = 0; l < BowlersIndexArray.Count; l++)
			{
				if (flag && l != currentBowler)
				{
					bowler[l].ClickBtn.enabled = false;
					bowler[currentBowler].Name.font = normal;
					bowler[l].Highlight.sprite = bowlerStates[2];
				}
			}
		}
		else
		{
			TMcheckBowlerSpell();
		}
	}

	private bool CanBowlCurrentOver(int bowlerID)
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			if (bowlerID == lastBowlerIndex)
			{
				return false;
			}
			if (!CheckRemainingOvers(bowlerID))
			{
				return true;
			}
			return false;
		}
		if (bowlerID == TMLastbowler)
		{
			return false;
		}
		return true;
	}

	public void OverCompleted()
	{
		newOverCalled = true;
		if (CONTROLLER.PlayModeSelected >= 9)
		{
			return;
		}
		if (CONTROLLER.BowlingTeamIndex == CONTROLLER.opponentTeamIndex)
		{
			for (int i = 0; i < BowlersIndexArray.Count; i++)
			{
				if (CheckRemainingOvers(i))
				{
					bowler[i].ClickBtn.enabled = false;
					bowler[i].Name.font = normal;
					bowler[i].Highlight.sprite = bowlerStates[2];
				}
			}
		}
		else
		{
			for (int i = 0; i < BowlersIndexArray.Count; i++)
			{
				if (!CheckRemainingOvers(i))
				{
					bowler[i].ClickBtn.enabled = true;
					bowler[i].Name.font = normal;
					bowler[i].Highlight.sprite = bowlerStates[0];
				}
				else
				{
					bowler[i].ClickBtn.enabled = false;
					bowler[i].Name.font = normal;
					bowler[i].Highlight.sprite = bowlerStates[2];
				}
			}
		}
		if (currentBowler >= 0 && currentBowler < bowler.Length)
		{
			bowler[currentBowler].ClickBtn.enabled = false;
			bowler[currentBowler].Name.font = normal;
			bowler[currentBowler].Highlight.sprite = bowlerStates[2];
			if (CONTROLLER.PlayModeSelected == 7)
			{
			}
		}
		if (CONTROLLER.PlayModeSelected != 8)
		{
			GetRandomBowler();
		}
		else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
		{
			// Reconnect bowler-change fix (tester #6 + the "names wrong for one turn" notice): the reconnect
			// scene reload re-runs intro → OverCompleted on the BOWLING client, and this unguarded re-pick
			// replaced the actual mid-over bowler AND clobbered syncedBowlerListIndex (defeating every later
			// restore). During a reconnect restore ADOPT the durable synced pick instead — exact mirror of the
			// batting-side adopt branch below. Normal over-starts still random-pick.
			bool _reconnectRestore = CricketNetworkManager.RestorePending
				|| (Launcher.Instance != null && Launcher.Instance.IsReConnecting());
			if (_reconnectRestore && CricketNetworkManager.instance != null
				&& CricketNetworkManager.instance.syncedBowlerListIndex >= 0)
			{
				RPC_SetBowler(CricketNetworkManager.instance.syncedBowlerListIndex);
				// RPC_SetBowler maps CONTROLLER.CurrentBowlerIndex only on the BATTING client — mirror
				// GetRandomBowler's scorecard→playerlist mapping here so the reconnected bowler renders the
				// RIGHT bowler immediately (tester: "bowlers change jab tak over complete nahi hua" — it sat
				// on a stale/default index until the next over's fresh pick corrected it).
				int _adoptIdx = CricketNetworkManager.instance.syncedBowlerListIndex;
				if (BowlersIndexArray != null && _adoptIdx >= 0 && _adoptIdx < BowlersIndexArray.Count)
					CONTROLLER.CurrentBowlerIndex = BowlersIndexArray[_adoptIdx];
			}
			else
			{
				// The bowling PLAYER picks now — no auto-pick here. Record who just finished the over first:
				// CanBowlCurrentOver() excludes lastBowlerIndex, and that is what stops the same man bowling two
				// overs running. GetRandomBowler() used to set it as a side effect of choosing, so with the auto-pick
				// gone it must be set explicitly or the rule quietly disappears. In the old auto path this assignment
				// was already true by construction, so nothing changes for offline/AI.
				if (currentBowler >= 0 && BowlersIndexArray != null && currentBowler < BowlersIndexArray.Count)
					lastBowlerIndex = currentBowler;
				_awaitingPlayerBowlerPick = true;
				ConstantsData_M.MpLog($"[BowlingScoreCard] Over complete — waiting for the bowling player to pick the next bowler (outgoing={currentBowler}).");
			}
		}
		else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
			&& CricketNetworkManager.instance != null && CricketNetworkManager.instance.syncedBowlerListIndex >= 0)
		{
			// Game-start / over-start bowler MISMATCH fix (#1/#5b): only the BOWLING client runs GetRandomBowler
			// (Random.Range), so the BATTING client must adopt that pick. Otherwise it keeps a STALE local
			// currentBowler until the OnBowlerIndexSynced hook fires (which can be late / not fire for an unchanged
			// value) → its screen briefly shows a DIFFERENT bowler / type / side than the bowling authority. Adopt
			// the authoritative SyncVar value up front. (The hook still re-applies if a newer pick arrives.)
			RPC_SetBowler(CricketNetworkManager.instance.syncedBowlerListIndex);
		}

		if (CONTROLLER.PlayModeSelected == 8)
		{
			//Photon Removal  PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	private void DisableBowler()
	{
		if (CONTROLLER.PlayModeSelected < 9)
		{
			for (int i = 0; i < BowlersIndexArray.Count; i++)
			{
				bowler[i].ClickBtn.enabled = false;
			}
		}
	}

	public void InningsCompleted()
	{
		if (CONTROLLER.PlayModeSelected < 9)
		{
			for (int i = 0; i < BowlersIndexArray.Count; i++)
			{
				bowler[i].ClickBtn.enabled = false;
				bowler[currentBowler].Name.font = normal;
				bowler[i].Highlight.sprite = bowlerStates[0];
			}
		}
	}

	public void RestartGame()
	{
		if (CONTROLLER.PlayModeSelected >= 9)
		{
			return;
		}
		for (int i = 0; i < BowlersIndexArray.Count; i++)
		{
			if (BowlersIndexArray[i] == CONTROLLER.CurrentBowlerIndex)
			{
				currentBowler = i;
			}
		}
		SetTeamInfo();
		UpdatePreviousBowlers();
		UpdateScoreCard();
		ContinueSelected();
	}

	private void UpdatePreviousBowlers()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			for (int i = 0; i < BowlersIndexArray.Count; i++)
			{
				int num = BowlersIndexArray[i];
				bowler[i].Overs.text = GetOversBowled(CONTROLLER.BowlingTeamIndex, num);
				bowler[i].Maiden.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.Maiden;
				bowler[i].Runs.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.RunsGiven;
				bowler[i].Wickets.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.Wicket;
				bowler[i].ERate.text = GetEconRate(CONTROLLER.BowlingTeamIndex, num);
				if (CheckRemainingOvers(i))
				{
					bowler[i].ClickBtn.enabled = false;
					bowler[currentBowler].Name.font = normal;
					bowler[i].Highlight.sprite = bowlerStates[2];
				}
			}
		}
		else
		{
			TMUpdatePreviousBowlers();
		}
	}

	private void CheckIfNewInnings()
	{
		if (!CONTROLLER.NewInnings)
		{
			return;
		}
		if (CONTROLLER.PlayModeSelected != 8)
		{
			GetRandomBowler();
		}
		else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
		{
			// Same rule at an innings start — the opening over needs a bowler too, and the tester saw the
			// auto-pick there as well ("game start pe bhi bowling card aya hi nahi"). Player chooses; Continue()'s
			// fallback covers a card dismissed without a pick.
			_awaitingPlayerBowlerPick = true;
			ConstantsData_M.MpLog("[BowlingScoreCard] New innings — waiting for the bowling player to pick the opening bowler.");
		}
		else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
			&& CricketNetworkManager.instance != null && CricketNetworkManager.instance.syncedBowlerListIndex >= 0)
		{
			// New-innings bowler MISMATCH fix (#1/#5b): batting client adopts the bowling authority's pick from the
			// SyncVar up front (don't wait for the late OnBowlerIndexSynced hook) so the highlight + bowler/type/side
			// match the bowling screen from ball 1.
			RPC_SetBowler(CricketNetworkManager.instance.syncedBowlerListIndex);
		}
		for (int i = 0; i < bowler.Length; i++)
		{
			bowler[i].Name.font = normal;
			bowler[i].Highlight.sprite = bowlerStates[0];
		}
		bowler[currentBowler].Highlight.sprite = bowlerStates[1];
		if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
		{
			for (int j = 0; j < bowler.Length; j++)
			{
				bowler[j].ClickBtn.enabled = true;
			}
		}
		else
		{
			for (int k = 0; k < bowler.Length; k++)
			{
				bowler[k].ClickBtn.enabled = false;
			}
		}
		CONTROLLER.NewInnings = false;
	}

	public void SwapScorecard()
	{
		if (CONTROLLER.PlayModeSelected < 9)
		{
			if (BtnTeamName.text == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper())
			{
				DisplayList(CONTROLLER.BattingTeamIndex);
				BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
				SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
				bowler[currentBowler].Name.font = normal;
				bowler[currentBowler].Highlight.sprite = bowlerStates[0];
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

			}
			else
			{
				DisplayList(CONTROLLER.BowlingTeamIndex);
				BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
				SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
				if (!CONTROLLER.isFromAutoPlay)
				{
					bowler[currentBowler].Name.font = bold;
					bowler[currentBowler].Highlight.sprite = bowlerStates[1];
				}
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

			}
			if (Singleton<GameData>.instance.isInningsComplete)
			{
				for (int k = 0; k < bowler.Length; k++)
				{
					bowler[k].Name.font = normal;
					bowler[k].Highlight.sprite = bowlerStates[0];
				}
			}
			Singleton<BowlingScoreBoardPanelTransition>.instance.PanelTransition();
		}
		DisableBowlerSelection();
	}

	private void DisplayList(int TeamID)
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			for (int i = 0; i < bowler.Length; i++)
			{
				if (CONTROLLER.BowlingTeamIndex == CONTROLLER.opponentTeamIndex)
				{
					if (firstTimeHide)
					{
						if (CONTROLLER.BowlingTeamIndex == TeamID)
						{
							if (!CONTROLLER.isFromAutoPlay)
							{
								if (currentBowler >= 0 && currentBowler < bowler.Length)   // -1 = no bowler picked yet -> don't index bowler[-1]
								{
									bowler[currentBowler].Name.font = bold;
									bowler[currentBowler].Highlight.sprite = bowlerStates[1];
								}
								bowler[i].ClickBtn.enabled = false;
							}
						}
						else
						{
							if (currentBowler >= 0 && currentBowler < bowler.Length)   // -1 = no bowler picked yet -> don't index bowler[-1]
							{
								bowler[currentBowler].Name.font = normal;
							}
							bowler[i].Highlight.sprite = bowlerStates[0];
							bowler[i].ClickBtn.enabled = false;
						}
					}
				}
				else if (CONTROLLER.BowlingTeamIndex != TeamID)
				{
					bowler[i].Highlight.sprite = bowlerStates[0];
				}
				else
				{
					checkBowlerSpell();
				}
				bowler[i].Name.text = string.Empty.ToUpper();
				bowler[i].Type.text = string.Empty.ToUpper();
				bowler[i].Overs.text = string.Empty;
				bowler[i].Maiden.text = string.Empty;
				bowler[i].Runs.text = string.Empty;
				bowler[i].Wickets.text = string.Empty;
				bowler[i].ERate.text = string.Empty;
			}
			int num = CONTROLLER.TeamList[TeamID].PlayerList.Length;
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				string style = CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.Style;
				if (style != string.Empty && style != null)
				{
					bowler[num2].Name.text = CONTROLLER.TeamList[TeamID].PlayerList[i].ScoreboardName.ToUpper();
					bowler[num2].Type.text = GetBowlerType(TeamID, i).ToUpper();
					bowler[num2].Overs.text = GetOversBowled(TeamID, i);
					bowler[num2].Maiden.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.Maiden;
					bowler[num2].Runs.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.RunsGiven;
					bowler[num2].Wickets.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.Wicket;
					bowler[num2].ERate.text = GetEconRate(TeamID, i);
					num2++;
				}
			}
			int num3 = ((TeamID != CONTROLLER.BowlingTeamIndex) ? CONTROLLER.BowlingTeamIndex : CONTROLLER.BattingTeamIndex);
			string text = CONTROLLER.TeamList[num3].currentMatchScores + "/" + CONTROLLER.TeamList[num3].currentMatchWickets;
			string text2 = LocalizationData.localizationInstance.getText(235) + " " + CONTROLLER.TeamList[num3].currentMatchExtras;
			int num4 = CONTROLLER.TeamList[num3].currentMatchBalls / 6;
			int num5 = CONTROLLER.TeamList[num3].currentMatchBalls % 6;
			string text3 = LocalizationData.localizationInstance.getText(184) + " " + num4 + "." + num5 + "(" + CONTROLLER.totalOvers + ")";
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
			if (Singleton<GameData>.instance.isInningsComplete)
			{
				for (int i = 0; i < bowler.Length; i++)
				{
					bowler[i].Name.font = normal;
					bowler[i].Highlight.sprite = bowlerStates[0];
				}
			}
		}
		else
		{
			TMDisplayList(TeamID);
		}
	}

	public void ContinueSelected()
	{
		DisableBowler();
		lastBowlerIndex = currentBowler;
	}

	// Batting-side bowling-scorecard-stuck fix: set true when a continue is requested (the bowler's
	// RPC_Continue) on the BATTING client before its own CanLoadGround is ready. The continue is then
	// deferred and replayed by GroundController.UpdateCanLoadGround once the flag flips true.
	private bool pendingBattingContinue = false;

	// The BOWLING side's twin of the flag above. GroundController.UpdateCanLoadGround used to re-invoke
	// Continue() on the bowling client UNCONDITIONALLY the moment CanLoadGround flipped true and the card was
	// showing — it never checked whether the bowler had actually asked to continue. Once the opponent's tick
	// opens this card, CanLoadGround flips at almost the same instant, so the card was auto-dismissed before
	// the bowler could look at it (logs 12-08 02:36: 'panelIndex = 2' then the dismiss, with no tap in
	// between and the tap-guard never firing). Track the request like the batting side does, so the re-trigger
	// only ever RESUMES a continue the player actually asked for.
	private bool pendingBowlingContinue = false;
	private Coroutine _groundWatchdogCo;

	// Innings-shift self-heal: when Continue() defers because CanLoadGround is still false, the batting-side
	// flush (GroundController.UpdateCanLoadGround) is supposed to replay us — but at the innings swap the
	// batting/bowling team indices FLIP (toss-frozen -> live snapshot) mid-handshake and that flush never lands,
	// so the bowling scorecard hangs forever. This watchdog force-readies the ground after a timeout so it
	// cannot deadlock. Online 1v1 only.
	private System.Collections.IEnumerator ForceCanLoadGroundAfterTimeout()
	{
		float t = 0f;
		// 6s was long enough that a tester taps once, sees nothing, and files "stuck on scorecard".
		// The re-tap path below still force-completes instantly; this just makes a SINGLE tap recover on
		// its own before anyone gives up. It only changes how long the existing self-heal waits.
		while (t < 2.5f)
		{
			if (CONTROLLER.CanLoadGround) { _groundWatchdogCo = null; yield break; }
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		_groundWatchdogCo = null;
		if (CONTROLLER.PlayModeSelected == 8 && !CONTROLLER.CanLoadGround && gameObject.activeInHierarchy)
		{
			ConstantsData_M.MpLog("[BowlingScoreCard] ForceCanLoadGround watchdog fired — forcing CanLoadGround + Continue() (innings-shift deadlock self-heal).");
			CONTROLLER.CanLoadGround = true;
			Continue();
		}
	}

	public void Continue()
	{
		// SAFETY NET for the player-picks-the-bowler change: the auto-pick at the over/innings boundary is
		// gone, so if this card is dismissed without a choice there is NO bowler for the coming over and the
		// match would sit there. Fall back to exactly the old behaviour in that case — GetRandomBowler()
		// also relays the pick (CmdSetBowler + CmdSyncBowlerAbsolute), so both sides stay in step. Runs
		// BEFORE the bail-outs below so the pick is made and relayed even if the dismiss is deferred.
		if (_awaitingPlayerBowlerPick && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
			&& CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
		{
			_awaitingPlayerBowlerPick = false;
			ConstantsData_M.MpLog("[BowlingScoreCard] Card dismissed with no bowler chosen — falling back to the automatic pick so the over can start.");
			GetRandomBowler();
		}

		////Debug.Log("CANLOADDD" + CONTROLLER.CanLoadGround);
		if (CONTROLLER.PlayModeSelected == 8 && /*!timerImage.gameObject.activeInHierarchy &&*/  !CONTROLLER.CanLoadGround)
		{
			// Batting client: the bowler asked to continue (RPC_Continue) but this client's ground isn't
			// ready yet, so Continue() bails here and the bowling scorecard would stay stuck forever
			// (nothing re-invoked it). Remember the request; UpdateCanLoadGround flushes it when ready.
			if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
			{
				pendingBattingContinue = true;
			}
			else if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
			{
				// The bowler DID ask to continue but the ground was not ready — this is the only case the
				// UpdateCanLoadGround re-trigger is meant to rescue.
				pendingBowlingContinue = true;
			}
			// Reconnect-on-over-end fix (issue #4, report d8066820): the scorecard restored after a
			// reconnect shows but the tick appears dead — CanLoadGround (static) is false and Continue
			// silently bails, so nothing happens until the 6s watchdog self-heals; and if a stale
			// _groundWatchdogCo handle survives (BowlingScoreCard is a persistent Singleton), the
			// `== null` guard below never restarts it and the button is dead FOREVER. So: if a watchdog
			// is already pending when the user taps AGAIN, treat the explicit re-tap as "force now" —
			// the same action the watchdog would take, just user-triggered instead of after the timeout.
			if (_groundWatchdogCo != null && gameObject.activeInHierarchy)
			{
				StopCoroutine(_groundWatchdogCo);
				_groundWatchdogCo = null;
				CONTROLLER.CanLoadGround = true;
				Continue();
				return;
			}
			// Deadlock backstop: if the flush never arrives (team-flip race), force it after a timeout.
			if (_groundWatchdogCo == null && gameObject.activeInHierarchy)
			{
				ConstantsData_M.MpLog("[BowlingScoreCard] Continue deferred — ground not ready yet (CanLoadGround=false); self-heal armed. Tap again to force it immediately.");
				_groundWatchdogCo = StartCoroutine(ForceCanLoadGroundAfterTimeout());
			}
			return;
		}
		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			Singleton<GroundController>.instance.UpdateCanBowlerBowl(true);
		}

		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && CONTROLLER.CanLoadGround)
		{
			////Debug.Log("CANLOADDDINSIDEE" + CONTROLLER.CanLoadGround);

			if (GameConstants.isWithAI == false)
			{
				//Photon Removal    photonView.RPC("RPC_Continue", RpcTarget.OthersBuffered);
				CricketNetworkManager.instance.CmdBowlingScoreCardContinue(staticVariables.UserProfiledata.user._id);
			}


		}

		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && Timer.activeInHierarchy && CONTROLLER.CanLoadGround)
		{
			Timer.SetActive(false);
		}



		if (CONTROLLER.PlayModeSelected == 7)
		{

		}
		else
		{
			firstTimeHide = true;
			Hide(boolean: true);
			if (CONTROLLER.gameCompleted)
			{
				Singleton<MatchSummary>.instance.showMe();
			}
			else if (!Singleton<GameData>.instance.isGamePaused)
			{
				// currentBowler can still be -1 here if the over-start Continue races ahead of the bowler pick/sync;
				// indexing BowlersIndexArray[-1] crashed Continue() and deadlocked game start. Guard it (the bowling
				// authority already has a valid pick by here; the batting side's value lands via the sync moments later).
				if (BowlersIndexArray != null && currentBowler >= 0 && currentBowler < BowlersIndexArray.Count)
				{
					CONTROLLER.CurrentBowlerIndex = BowlersIndexArray[currentBowler];
				}
				BallSimulationClicked();
			}
			else
			{
				Singleton<GameData>.instance.AgainToGamePauseScreen();
				Singleton<PauseGameScreen>.instance.Hide(boolean: false);
			}
		}

		//Singleton<GroundController>.instance.EnableContinueBtn();
	}

	private void BallSimulationClicked()
	{

		ConstantsData_M.Log("ball simulation clicked:::::");
		if (Singleton<BallSimulationManager>.instance.CanShowBallSimulation() && Singleton<BallSimulationManager>.instance.hasSetSimulationData && !Singleton<BallSimulationManager>.instance.isShowingSimulation && !CONTROLLER.isAutoPlayed && CONTROLLER.PlayModeSelected != 8)
		{
			groundControllerScript.hideAllCamera();
			groundControllerScript.EnableAllSkinRenderers(state: false);
			groundControllerScript.ShowBall(status: false);
			Singleton<BallSimulationManager>.instance.StartBallSimulation();
		}
		else
		{
			groundControllerScript.EnableAllSkinRenderers(state: true);
			Singleton<BallSimulationManager>.instance.BallSimulationSkipped();
		}
	}

	private void TMSetTeamInfo()
	{
		Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite in flags)
		{
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
			{
				SCTeamFlag.sprite = sprite;
			}
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation)
			{
				BtnTeamFlag.sprite = sprite;
			}
		}
		SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
		BtnTeamName.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName.ToUpper();
	}

	private void DisableBowlerSelection()
	{
		if (!ContinueBtn.gameObject.activeSelf)
		{
			// "over end pe bowler apna bowler select nahi kar sakta": this is the ONLY place that disables every
			// bowler button at once, and its whole condition is that the Continue (tick) button is hidden. The
			// card hides Continue and shows Back when isGamePaused || gameCompleted — i.e. it opens READ-ONLY —
			// so a pause flag still set at the over boundary silently makes the bowling player's own selection
			// untappable. OnApplicationPause(true) -> GamePaused(true) sets that flag and the resume side has no
			// matching clear, which matters now that disconnects are tested by BACKGROUNDING the app. Log the
			// inputs so the next capture says which of the two conditions did it instead of us inferring.
			if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
			{
				ConstantsData_M.MpLog($"[BowlingScoreCard][DisableBowlerSelection] Disabling ALL bowler buttons — Continue hidden (isGamePaused={(Singleton<GameData>.instance != null ? Singleton<GameData>.instance.isGamePaused.ToString() : "<no GameData>")} gameCompleted={CONTROLLER.gameCompleted} isFromAutoPlay={CONTROLLER.isFromAutoPlay} amBowling={(CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)} currentBowler={currentBowler}).");
			}
			for (int i = 0; i < bowler.Length; i++)
			{
				bowler[i].ClickBtn.enabled = false;
			}
		}
	}

	//Photon Removal	[PunRPC]
	public void RPC_Continue()
	{
		// The OPPONENT continuing must not close this card while THIS player still owes a bowler pick.
		// Both clients show the over-end bowling card and either side's tick relays here, so a batting player
		// tapping through would dismiss the bowler's selection remotely and hand the choice to the fallback
		// auto-pick — which is the behaviour we were asked to remove. Ignore the remote dismiss and let the
		// bowler finish; the batting side has already closed its own card locally and simply waits for the
		// next ball, and the bowler's own tick relays back the moment it is pressed.
		if (_awaitingPlayerBowlerPick && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
			&& CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
		{
			ConstantsData_M.MpLog("[BowlingScoreCard] Opponent's continue ignored — this player still has to pick the bowler.");
			return;
		}
		Continue();
	}

	// Batting-side bowling-scorecard-stuck fix: called by GroundController.UpdateCanLoadGround when the
	// batting client's CanLoadGround finally flips true. If the bowler had already asked to continue
	// (pendingBattingContinue) and the bowling scorecard is still showing, replay the dismiss now.
	// Called by GroundController.UpdateCanLoadGround when CanLoadGround flips true on the BOWLING side.
	// Resumes only a continue the bowler already requested; otherwise it leaves the card alone so the player
	// can pick their bowler.
	public void FlushPendingBowlingContinue()
	{
		if (!pendingBowlingContinue) return;
		pendingBowlingContinue = false;
		ConstantsData_M.MpLog("[BowlingScoreCard] Ground ready — resuming the continue the bowler had already asked for.");
		Continue();
	}

	public void FlushPendingBattingContinue()
	{
		if (pendingBattingContinue
			&& CONTROLLER.PlayModeSelected == 8
			&& CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
			&& CONTROLLER.CanLoadGround
			&& scoreCard != null
			&& scoreCard.activeInHierarchy)
		{
			pendingBattingContinue = false;
			Continue();
		}
	}

	public void CallRPC()
	{
		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
		{
			if (!Timer.activeInHierarchy)
			{
				return;
			}
			if (GameConstants.isWithAI == false)
			{
				//Photon Removal    photonView.RPC("RPC_Continue", RpcTarget.OthersBuffered);
				CricketNetworkManager.instance.CmdBowlingScoreCardContinue(staticVariables.UserProfiledata.user._id);
			}



		}
	}

	public void SetBowlingScoreCardTimer(float Seconds)
	{
		secCount = Seconds;
		timerImage.fillAmount = Seconds;
		s = DOTween.Sequence();
		TweenCallback callback = delegate
		{
			if (!Timer.activeInHierarchy)
			{
				s.Kill();
			}
			SetSecond();
		};
		s.Append(timerImage.DOFillAmount(0f, Seconds));
		for (int i = 0; i < Seconds; i++)
		{
			s.InsertCallback(i, callback);
		}
		s.InsertCallback(Seconds, CallRPC);
	}

	public void SetSecond()
	{
		timerText.text = secCount.ToString();
		secCount--;

	}

	public void Hide(bool boolean)
	{
		if (boolean)
		{
			//Debug.Log("Hide BBOOlwes");
			Timer.SetActive(false);
			if (CONTROLLER.PlayModeSelected != 7 || !Singleton<GameData>.instance.isSessionFinished)
			{
				BG.SetActive(value: false);
			}
			scoreCard.SetActive(value: false);
			if (firstTimeHide)
			{
				DisplayList(CONTROLLER.BowlingTeamIndex);
				SetTeamInfo();
			}
			if (CricketNetworkManager.instance != null)
			{
				// Panel 0 = None: bowling scorecard dismissed, game is back to normal gameplay.
			// Previously this was 3 (AfterOverSummary) which caused reconnect-during-gameplay
			// to incorrectly run the AfterOverSummary reconnect case.
			CricketNetworkManager.instance.CmdChangeCurrentOpenPanel(0);
			}
			if (CONTROLLER.PlayModeSelected == 8)
			{

			}
			return;
		}
		else
		{
			if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
			{
				Timer.SetActive(true);
				SetBowlingScoreCardTimer(10f);

				if (GameConstants.isWithAI == false)
				{
					//Photon Removal    photonView.RPC("RPC_SetBowler", RpcTarget.AllBuffered, currentBowler);
					// Ghost-session guard. The 30-07 ritu_mp logs show THIS send throwing inside Mirror's
					// SendCommandInternal on a reloaded scene, which aborts the rest of the caller — and for
					// CmdSetBowler that means the bowler is never assigned or relayed, i.e. the two sides end up
					// with different bowlers. Only the sends actually seen throwing are guarded.
					if (CricketNetworkManager.ReadyToSend)
					    CricketNetworkManager.instance.CmdSetBowler(currentBowler);
					else
					    ConstantsData_M.MpLog("[GhostSend] SetBowler(currentBowler) BLOCKED — network manager not ready (ghost session); caller continues instead of throwing.");
				}
				if (CricketNetworkManager.instance != null)
				{
					CricketNetworkManager.instance.CmdChangeCurrentOpenPanel((int)PanelsInfo.BowlingScoreCard);
				}

				if (!GameData.instance.CheckForInningsComplete() && (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
				{
				}
			}
			else
			{
			}
		}

		if (CONTROLLER.PlayModeSelected > 3 || CONTROLLER.PlayModeSelected == 7)
		{
			BtnTeamName.transform.parent.gameObject.SetActive(value: false);
		}

		SelectedInningsText.transform.parent.gameObject.SetActive(value: false);
		Singleton<NavigationBack>.instance.deviceBack = null;
		CONTROLLER.pageName = "bowlingSC";
		if (!BG.activeSelf)
		{
			BG.SetActive(value: true);
		}
		if (!Singleton<GameData>.instance.isGamePaused && !CONTROLLER.gameCompleted)
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
			ContinueBtn.gameObject.SetActive(value: false);
			BackBtn.gameObject.SetActive(value: true);
		}
		Singleton<GameOverScreen>.instance.Hide(boolean: true);
		scoreCard.SetActive(value: true);
		DisplayList(CONTROLLER.BowlingTeamIndex);
		UpdateScoreCard();
		Singleton<BowlingScoreBoardPanelTransition>.instance.PanelTransition();

		DisableBowlerSelection();
	}

	public void BackKeySelected()
	{
		Continue();
	}

	public void PauseTimer()
	{
		s.Pause();
	}

	public void ResumeTimer()
	{
		s.Play();
	}

	public void TMResetBowlingCard()
	{
		currentBowler = -1;
		lastBowlerIndex = -1;
		int bowlingTeamIndex = CONTROLLER.BowlingTeamIndex;
		SetTeamInfo();
		GetBowlersInTeam(bowlingTeamIndex);
		for (int i = 0; i < bowler.Length; i++)
		{
			if (CONTROLLER.BowlingTeamIndex == CONTROLLER.opponentTeamIndex)
			{
				bowler[i].Name.font = normal;
				bowler[i].Highlight.sprite = bowlerStates[0];
				bowler[i].ClickBtn.enabled = false;
			}
			else
			{
				bowler[i].ClickBtn.enabled = true;
			}
			if (i < BowlersIndexArray.Count)
			{
				int num = BowlersIndexArray[i];
				bowler[i].Name.text = CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[num].ScoreboardName.ToUpper();
				bowler[i].Type.text = GetBowlerType(bowlingTeamIndex, num).ToUpper();
				bowler[i].Overs.text = "0.0";
				bowler[i].Maiden.text = "0";
				bowler[i].Runs.text = "0";
				bowler[i].Wickets.text = "0";
				bowler[i].ERate.text = TMGetEconRate(bowlingTeamIndex, num);
				if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
				{
					bowler[i].ClickBtn.enabled = true;
				}
			}
			else
			{
				bowler[i].Name.text = string.Empty.ToUpper();
				bowler[i].Type.text = string.Empty.ToUpper();
				bowler[i].Overs.text = string.Empty;
				bowler[i].Maiden.text = string.Empty;
				bowler[i].Runs.text = string.Empty;
				bowler[i].Wickets.text = string.Empty;
				bowler[i].ERate.text = string.Empty;
				bowler[i].ClickBtn.enabled = false;
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

	public void TMUpdateScoreCard()
	{
		Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite in flags)
		{
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation)
			{
				SCTeamFlag.sprite = sprite;
			}
		}
		SCTeamName.text = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName.ToUpper();
		int bowlingTeamIndex = CONTROLLER.BowlingTeamIndex;
		int currentBowlerIndex = CONTROLLER.CurrentBowlerIndex;
		bowler[currentBowler].Name.font = bold;
		bowler[currentBowler].Highlight.sprite = bowlerStates[1];
		bowler[currentBowler].Overs.text = TMGetOversBowled(bowlingTeamIndex, currentBowlerIndex);
		if (SelectedIndex == 1)
		{
			bowler[currentBowler].Maiden.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.TMMaiden1;
			bowler[currentBowler].Runs.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.TMRunsGiven1;
			bowler[currentBowler].Wickets.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.TMWicket1;
			bowler[currentBowler].ERate.text = TMGetEconRate(bowlingTeamIndex, currentBowlerIndex);
		}
		else
		{
			bowler[currentBowler].Maiden.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.TMMaiden2;
			bowler[currentBowler].Runs.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.TMRunsGiven2;
			bowler[currentBowler].Wickets.text = string.Empty + CONTROLLER.TeamList[bowlingTeamIndex].PlayerList[currentBowlerIndex].BowlerList.TMWicket2;
			bowler[currentBowler].ERate.text = TMGetEconRate(bowlingTeamIndex, currentBowlerIndex);
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

	private int TMGetCurrentSpell()
	{
		int num = ((CONTROLLER.currentInnings >= 2) ? (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls2 / 6) : (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls1 / 6));
		if (num < firstSpellOver)
		{
			return 1;
		}
		return 2;
	}

	private int TMGetCurrentOver()
	{
		int num = ((CONTROLLER.currentInnings >= 2) ? (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls2 / 6) : (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls1 / 6));
		return num + 1;
	}

	private void TMcheckBowlerSpell()
	{
		int num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6;
		bool flag = false;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		List<int> list = new List<int>();
		if ((fromDisplayList && SelectedIndex == 1) || (!fromDisplayList && CONTROLLER.currentInnings < 2))
		{
			num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls1 / 6;
		}
		else
		{
			num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls2 / 6;
		}
		for (int i = 0; i < BowlersIndexArray.Count; i++)
		{
			num2 = (((!fromDisplayList || SelectedIndex != 1) && (fromDisplayList || CONTROLLER.currentInnings >= 2)) ? CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[i]].BowlerList.TMBallsBowled2 : CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[i]].BowlerList.TMBallsBowled1);
			num3 = (int)((float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6) * 0.2f);
			num5 = (num3 - num2) / 6;
			if (num6 < num5)
			{
				list = new List<int>();
				num6 = num5;
				num7 = i;
				list.Add(i);
			}
			else if (num6 == num5)
			{
				list.Add(i);
			}
		}
		num5 = 0;
		for (int j = 0; j < BowlersIndexArray.Count; j++)
		{
			if (j != num7)
			{
				num2 = (((!fromDisplayList || SelectedIndex != 1) && (fromDisplayList || CONTROLLER.currentInnings >= 2)) ? CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[j]].BowlerList.TMBallsBowled2 : CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[j]].BowlerList.TMBallsBowled1);
				num3 = (int)((float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6) * 0.2f);
				num5 += (num3 - num2) / 6;
			}
		}
		for (int k = 0; k < BowlersIndexArray.Count; k++)
		{
			num2 = (((!fromDisplayList || SelectedIndex != 1) && (fromDisplayList || CONTROLLER.currentInnings >= 2)) ? CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[k]].BowlerList.TMBallsBowled2 : CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[k]].BowlerList.TMBallsBowled1);
			num3 = (int)((float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6) * 0.2f);
			num4 = num3 - num2;
			if (num2 <= CONTROLLER.oversSelectedIndex * 6 - 6 && num6 > num5 && list.Count <= 1 && k != lastBowlerIndex)
			{
				flag = true;
				currentBowler = num7;
			}
		}
		for (int l = 0; l < BowlersIndexArray.Count; l++)
		{
			if (flag && l != currentBowler)
			{
				bowler[l].ClickBtn.enabled = false;
				bowler[currentBowler].Name.font = normal;
				bowler[l].Highlight.sprite = bowlerStates[2];
			}
		}
	}

	private void TMUpdatePreviousBowlers()
	{
		if (CONTROLLER.PlayModeSelected >= 9)
		{
			return;
		}
		for (int i = 0; i < BowlersIndexArray.Count; i++)
		{
			int num = BowlersIndexArray[i];
			if (CONTROLLER.currentInnings < 2)
			{
				bowler[i].Overs.text = GetOversBowled(CONTROLLER.BowlingTeamIndex, num);
				bowler[i].Maiden.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.TMMaiden1;
				bowler[i].Runs.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.TMRunsGiven1;
				bowler[i].Wickets.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.TMWicket1;
				bowler[i].ERate.text = TMGetEconRate(CONTROLLER.BowlingTeamIndex, num);
			}
			else
			{
				bowler[i].Overs.text = GetOversBowled(CONTROLLER.BowlingTeamIndex, num);
				bowler[i].Maiden.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.TMMaiden2;
				bowler[i].Runs.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.TMRunsGiven2;
				bowler[i].Wickets.text = string.Empty + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[num].BowlerList.TMWicket2;
				bowler[i].ERate.text = TMGetEconRate(CONTROLLER.BowlingTeamIndex, num);
			}
			if (CheckRemainingOvers(i))
			{
				bowler[i].ClickBtn.enabled = false;
				bowler[currentBowler].Name.font = normal;
				bowler[i].Highlight.sprite = bowlerStates[2];
			}
		}
	}

	public void SwapInnings()
	{
		if (SelectedIndex == 2)
		{
			SelectedIndex = 1;
			SelectedInningsText.text = LocalizationData.localizationInstance.getText(470);
		}
		else
		{
			SelectedIndex = 2;
			SelectedInningsText.text = "FIRST INNINGS";
			SelectedInningsText.text = LocalizationData.localizationInstance.getText(469);
		}
		TMDisplayList(CONTROLLER.BowlingTeamIndex);
		if (!CONTROLLER.isFromAutoPlay)
		{
			bowler[currentBowler].Name.font = bold;
			bowler[currentBowler].Highlight.sprite = bowlerStates[1];
			if (TMLastbowler != -1)
			{
				bowler[TMLastbowler].Highlight.sprite = bowlerStates[2];
			}
		}
		if ((CONTROLLER.currentInnings < 2 && SelectedIndex == 2) || (CONTROLLER.currentInnings >= 2 && SelectedIndex == 1))
		{
			for (int i = 0; i < bowler.Length; i++)
			{
				bowler[i].Name.font = normal;
				bowler[i].Highlight.sprite = bowlerStates[0];
			}
		}
		DisableBowlerSelection();
		Singleton<BowlingScoreBoardPanelTransition>.instance.PanelTransition();
	}

	private void TMDisplayList(int TeamID)
	{
		for (int i = 0; i < bowler.Length; i++)
		{
			if (CONTROLLER.BowlingTeamIndex == CONTROLLER.opponentTeamIndex)
			{
				if (firstTimeHide)
				{
					if (CONTROLLER.BowlingTeamIndex == TeamID)
					{
						bowler[currentBowler].Name.font = bold;
						bowler[currentBowler].Highlight.sprite = bowlerStates[1];
						bowler[i].ClickBtn.enabled = false;
					}
					else
					{
						bowler[currentBowler].Name.font = normal;
						bowler[i].Highlight.sprite = bowlerStates[0];
						bowler[i].ClickBtn.enabled = false;
					}
				}
			}
			else if (CONTROLLER.BowlingTeamIndex != TeamID)
			{
				bowler[i].Highlight.sprite = bowlerStates[0];
			}
			else
			{
				fromDisplayList = true;
				checkBowlerSpell();
			}
			bowler[i].Name.text = string.Empty.ToUpper();
			bowler[i].Type.text = string.Empty.ToUpper();
			bowler[i].Overs.text = string.Empty;
			bowler[i].Maiden.text = string.Empty;
			bowler[i].Runs.text = string.Empty;
			bowler[i].Wickets.text = string.Empty;
			bowler[i].ERate.text = string.Empty;
		}
		int num = CONTROLLER.TeamList[TeamID].PlayerList.Length;
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			string style = CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.Style;
			if (style != string.Empty && style != null)
			{
				if (SelectedIndex == 1)
				{
					bowler[num2].Name.text = CONTROLLER.TeamList[TeamID].PlayerList[i].ScoreboardName.ToUpper();
					bowler[num2].Type.text = GetBowlerType(TeamID, i).ToUpper();
					bowler[num2].Overs.text = TMGetOversBowled(TeamID, i);
					bowler[num2].Maiden.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.TMMaiden1;
					bowler[num2].Runs.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.TMRunsGiven1;
					bowler[num2].Wickets.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.TMWicket1;
					bowler[num2].ERate.text = TMGetEconRate(TeamID, i);
					num2++;
				}
				else
				{
					bowler[num2].Name.text = CONTROLLER.TeamList[TeamID].PlayerList[i].ScoreboardName.ToUpper();
					bowler[num2].Type.text = GetBowlerType(TeamID, i).ToUpper();
					bowler[num2].Overs.text = TMGetOversBowled(TeamID, i);
					bowler[num2].Maiden.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.TMMaiden2;
					bowler[num2].Runs.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.TMRunsGiven2;
					bowler[num2].Wickets.text = string.Empty + CONTROLLER.TeamList[TeamID].PlayerList[i].BowlerList.TMWicket2;
					bowler[num2].ERate.text = TMGetEconRate(TeamID, i);
					num2++;
				}
			}
		}
		int num3 = ((TeamID != CONTROLLER.BowlingTeamIndex) ? CONTROLLER.BowlingTeamIndex : CONTROLLER.BattingTeamIndex);
		string text = CONTROLLER.TeamList[num3].currentMatchScores + "/" + CONTROLLER.TeamList[num3].currentMatchWickets;
		string text2 = LocalizationData.localizationInstance.getText(235) + " " + CONTROLLER.TeamList[num3].currentMatchExtras;
		int num4 = CONTROLLER.TeamList[num3].currentMatchBalls / 6;
		int num5 = CONTROLLER.TeamList[num3].currentMatchBalls % 6;
		string text3 = LocalizationData.localizationInstance.getText(184) + " " + num4 + "." + num5 + "(" + CONTROLLER.totalOvers + ")";
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
		if (Singleton<GameData>.instance.isInningsComplete)
		{
			for (int i = 0; i < bowler.Length; i++)
			{
				bowler[i].Name.font = normal;
				bowler[i].Highlight.sprite = bowlerStates[0];
			}
		}
		for (int i = 0; i < bowler.Length; i++)
		{
			bowler[i].Name.font = normal;
			bowler[i].Highlight.sprite = bowlerStates[0];
			if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
			{
				bowler[i].ClickBtn.enabled = true;
			}
			else
			{
				bowler[i].ClickBtn.enabled = false;
			}
		}
		bowler[currentBowler].Name.font = bold;
		bowler[currentBowler].Highlight.sprite = bowlerStates[1];
		if (TMLastbowler != -1)
		{
			bowler[TMLastbowler].ClickBtn.enabled = false;
			bowler[TMLastbowler].Highlight.sprite = bowlerStates[2];
		}
	}

	public string TMGetEconRate(int TeamID, int playerID)
	{
		float num = 0f;
		float num2;
		float num3;
		if (SelectedIndex == 1)
		{
			num2 = CONTROLLER.TeamList[TeamID].PlayerList[playerID].BowlerList.TMRunsGiven1;
			num3 = CONTROLLER.TeamList[TeamID].PlayerList[playerID].BowlerList.TMBallsBowled1;
		}
		else
		{
			num2 = CONTROLLER.TeamList[TeamID].PlayerList[playerID].BowlerList.TMRunsGiven2;
			num3 = CONTROLLER.TeamList[TeamID].PlayerList[playerID].BowlerList.TMBallsBowled2;
		}
		if (num3 > 0f)
		{
			num = num2 / num3 * 6f;
			num = Mathf.Round(num * 100f) / 100f;
		}
		return num.ToString();
	}

	private void TMselectRandomBowler()
	{
		if (CONTROLLER.PlayModeSelected >= 9)
		{
			return;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < BowlersIndexArray.Count; i++)
		{
			int num = 0;
			int num2 = 1200;
			num = ((CONTROLLER.currentInnings >= 2) ? CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[i]].BowlerList.TMBallsBowled2 : CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[BowlersIndexArray[i]].BowlerList.TMBallsBowled1);
			int num3 = num2 - num;
			if (num3 > 0 && i != lastBowlerIndex)
			{
				list.Add(i);
			}
		}
		if (list.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, list.Count);
			currentBowler = list[index];
		}
	}

	private string GetScore(int TeamID)
	{
		int num;
		int num2;
		if (SelectedIndex < 2)
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
		if (SelectedIndex == 1)
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

	public string TMGetOversBowled(int teamID, int playerID)
	{
		int num;
		int num2;
		if (SelectedIndex == 1)
		{
			num = CONTROLLER.TeamList[teamID].PlayerList[playerID].BowlerList.TMBallsBowled1 / 6;
			num2 = CONTROLLER.TeamList[teamID].PlayerList[playerID].BowlerList.TMBallsBowled1 % 6;
		}
		else
		{
			num = CONTROLLER.TeamList[teamID].PlayerList[playerID].BowlerList.TMBallsBowled2 / 6;
			num2 = CONTROLLER.TeamList[teamID].PlayerList[playerID].BowlerList.TMBallsBowled2 % 6;
		}
		return num + "." + num2;
	}

	private string GetOvers(int TeamID)
	{
		int num;
		int num2;
		if (SelectedIndex < 2)
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
}
