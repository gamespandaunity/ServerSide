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

public partial class GameData : Singleton<GameData>
{

	[FormerlySerializedAs("rvPopupText")] public Text rewardPopupText;
	[FormerlySerializedAs("rvPopup")] public GameObject rewardPopup;
	[FormerlySerializedAs("generatingScore")] public GameObject scoreGenerationPanel;
	[FormerlySerializedAs("RVFillmeter")] public Image rewardFillMeter;
	[FormerlySerializedAs("ballList")] public Transform ballIconsParent;
	[FormerlySerializedAs("fill")] public Image matchProgressBar;
	[FormerlySerializedAs("scrambledScore")] public Text currentScoreText;
	[FormerlySerializedAs("scrambledWickets")] public Text currentWicketsText;
	[FormerlySerializedAs("ScoreStr")] public string scoreDisplayString;
	[FormerlySerializedAs("ExtraStr")] public string extraRunsDisplayString;
	[FormerlySerializedAs("OversStr")] public string oversDisplayString;
	[FormerlySerializedAs("isGamePaused")] public bool isGamePaused;
	[FormerlySerializedAs("resumeGO")] public GameObject resumePanel;
	[FormerlySerializedAs("CounterFour")] public int boundaryFoursCount;
	[FormerlySerializedAs("CounterSix")] public int boundarySixesCount;
	[FormerlySerializedAs("CounterMaiden")] public int maidenOversCount;
	[FormerlySerializedAs("CounterDot")] public int dotBallCount;
	[FormerlySerializedAs("CounterWicketBowled")] public int bowledWicketCount;
	[FormerlySerializedAs("CounterWicketCatch")] public int caughtWicketCount;
	[FormerlySerializedAs("CounterWicketOthers")] public int otherWicketCount;
	[FormerlySerializedAs("CounterFifty")] public int fiftyRunsCount;
	[FormerlySerializedAs("CounterCentury")] public int hundredRunsCount;
	[FormerlySerializedAs("CounterPerOversPlayed")] public int oversPlayedCounter;
	[FormerlySerializedAs("CounterOppSix")] public int opponentSixesCounter;
	[FormerlySerializedAs("CounterPlayerDuckOut")] public int duckOutCount;
	[FormerlySerializedAs("BatsmanEntryTime")] public int batsmanEntryDelay = 3;
	[FormerlySerializedAs("fadeTime")] public float fadeEffectDuration = 0.2f;
	[FormerlySerializedAs("renderCamera")] public Camera matchRenderCamera;
	[FormerlySerializedAs("SkipBtn")] public Button skipButton;
	[FormerlySerializedAs("ActionTxt")] public Text actionStatusText;
	[FormerlySerializedAs("loadingText")] public Text loadingInfoText;
	[FormerlySerializedAs("SkipUpdate")] public bool shouldSkipUpdate;
	[FormerlySerializedAs("RunRate")] public static float runRate;
	[FormerlySerializedAs("stateVar")] public int gameState = -1;
	[FormerlySerializedAs("CanResumeGame")] public bool canResumeGameplay;
	[FormerlySerializedAs("CanPauseGame")] public bool canPauseGameplay;
	[FormerlySerializedAs("NewBatsmanIndex")] public int newBatsmanEntryIndex;
	[FormerlySerializedAs("inningsCompleted")] public bool isInningsComplete;
	[FormerlySerializedAs("action")] public int currentAction = -1;
	[FormerlySerializedAs("userAction")] public int userInputAction = -1;
	[FormerlySerializedAs("seq")] public DG.Tweening.Sequence skipAnimationSequence;
	[FormerlySerializedAs("SkipObj")] public GameObject skipPanel;
	[FormerlySerializedAs("EscapeResumedTime")] public float resumeAfterEscapeDelay;
	[FormerlySerializedAs("EscapeGameResumed")] public bool wasGameResumedAfterEscape;
	[FormerlySerializedAs("levelCompleted")] public bool isLevelFinished;
	[FormerlySerializedAs("currentBall")] public int currentBallNumber = -1;
	[FormerlySerializedAs("currentPartnership")] public int partnershipRunCount;
	[FormerlySerializedAs("runsScoredInOver")] public int runsInCurrentOver;
	[FormerlySerializedAs("groundControllerScript")] public GroundController groundController;

	// MULTIPLAYER BALL SYNC:
	// Both players simulate ball physics independently. Due to non-determinism, they can
	// reach different outcomes (boundary on batting side, keeper catch on bowling side).
	// Fix: Batting player is authoritative. Bowling player suppresses ALL local calls to
	// UpdateCurrentBall and waits for UpdateCurrentBallFromNetwork(), which is called by
	// RpcBallOutcome with the batting player's exact params. Both sides then run the same
	// code path with the same inputs → guaranteed identical results.
	// This bool is set to true ONLY by UpdateCurrentBallFromNetwork() so there is zero
	// ambiguity and no race condition between RPC arrival and local physics call timing.
	private bool _ballOutcomeIsFromNetwork = false;

	[FormerlySerializedAs("rebowled")] public bool isBallRebowled;
	[FormerlySerializedAs("rebowlStatus")] public int rebowlStatus = 2;
	[FormerlySerializedAs("ultraEdgeVariables")] public int[] ultraEdgeDataArray = new int[10];
	[FormerlySerializedAs("ultaEdgeBoolean")] public bool isUltraEdgeEnabled;
	[FormerlySerializedAs("vBall")] public int validatedBallIndex;
	[FormerlySerializedAs("bID")] public int bowlerId;
	[FormerlySerializedAs("wType")] public int wicketTypeCode;
	[FormerlySerializedAs("baID")] public int batsmanId;
	[FormerlySerializedAs("cID")] public int catcherId;
	[FormerlySerializedAs("bOut")] public int batsmanOutCode;
	[FormerlySerializedAs("RvCanCount")] public int rewardRunCount;
	[FormerlySerializedAs("RvExtras")] public int rewardExtraCount;
	[FormerlySerializedAs("sessionComplete")] public bool isSessionFinished;


	// Serialized fields (also public through inspector)
	[SerializeField] public GameObject waitingForOpponentPanel;
	[SerializeField] public TextMeshProUGUI waitingForOpponentText;
	[SerializeField] public GameObject waitingForOpponentPanel1;
	[SerializeField] public TextMeshProUGUI waitingForOpponentText1;
	[SerializeField] public GameObject opponentWatchingTutorialPanel;
	[SerializeField] public TextMeshProUGUI opponentWatchingTutorialText;

	private int savedNonStrikerIndex;
	private int savedStrikerIndex;
	private int savedNewStrikerIndex;
	private string rvStatus = string.Empty;
	private bool canPlayAnimation = true;
	private bool hasHitFour;
	private bool hasHitSix;
	private bool hasHitFourAndSix;
	private int boundaryRunsScored;
	private TextAsset gameXmlAsset;
	private float currentBallAngle;
	private int powerPlayOver;
	private bool isWicketBall;

	// Reconnect snapshot bridge: the over-end new-batsman walk-in is gated on isWicketBall, which a scene
	// reload resets — see MatchStateSnapshot.wicketBallPending.
	public bool WicketBallPendingForSnapshot => isWicketBall;
	public void RestoreWicketBallPendingFromSnapshot(bool pending) { isWicketBall = pending; }
	// Wicket-on-last-ball-of-over deadlock fix: tracks whether the new-batsman walk-in flow has been
	// started for the current wicket, so the over-complete branch can run it BEFORE the over-end
	// scorecard, and so it is never started twice (ReplayCompleted vs the over-complete guard).
	private bool wicketBatsmanFlowStarted;
	private int batsmanDismissedIndex;
	private bool hasGameQuit;
	private int bowlingControlMode = -1;
	private bool[] achievementFlags;
	private Touch[] touchPhases;
	private bool isNewTouch;
	private Vector2 touchStartPosition;
	private Vector2 touchEndPosition;
	private int chosenBattingAngle;
	private bool hasReachedMaxRuns;
	private bool hasReachedMaxWides;
	private bool isFromMainMenu;
	private float timeoutBlinkDuration;
	private float replayStartDelay = 1f;
	private int replayBlinkStatus;
	private bool replayFinishedTriggered;
	private Vector3 replayTextScreenPosition;
	private string skipButtonStatus = string.Empty;
	private bool isChaseSuccessful;
	private bool tempChaseResult;
	private Intro introScript;
	private Transform cachedTransform;
	private bool wasTouched;
	private string[] googleDisplayNames = new string[6]
	{
	"CubClass", "YoungLion", "PowerPlayer", "SuperPro", "Champion", "PrideOfChepauk"
	};
	private int currentSubLevel;
	private int rebowlCounter;
	private int rvRunsScored;
	private string lastBowledBallSummary = string.Empty;
	private bool hasShownRv;
	private int currentBowlerIndex;
	private int totalRunsScored;
	private int extraRuns;
	private int wasWicket;
	private bool fourScoredInOver;
	private bool sixScoredInOver;
	private float[] loftAngles = new float[11]
	{
	70f, 70f, 68f, 68f, 66f, 66f, 60f, 58f, 56f, 54f, 52f
	};
	private Vector2 previousMousePosition;
	private bool isBallValid;
	private bool hasReachedMaxSixes;
	private float[,] confidenceMultipliers = new float[3, 3]
	{
	{ 4f, 6f, 8f },
	{ 6f, 9f, 12f },
	{ 8f, 12f, 16f }
	};


	protected bool isTouchEnded = true;
	protected bool isShotCompleted;
	protected float touchStartTime;
	protected float shotMaxDuration = 0.3f;
	protected int slogOverScore;
	protected int wicketsThisOver;
	protected string playerUsername;

	public TextMeshProUGUI ping;
	protected void OnLevelWasLoaded(int _levelIndex)
	{
		if (_levelIndex == 2)
		{
			CONTROLLER.SceneIsLoading = false;
		}
	}

	protected void Awake()
	{

		if (CONTROLLER.PlayModeSelected == 7)
		{
			//ResetTM_PlayerScores();
		}
		GetDifficultyMode();
		isFromMainMenu = true;
		scoreGenerationPanel.SetActive(value: false);
		cachedTransform = base.transform;
		introScript = GameObject.Find("Intro").GetComponent<Intro>();
		CONTROLLER.isQuit = false;
		if (skipPanel != null)
		{
			skipPanel.SetActive(value: true);
		}
		SpaceTextValue();
		if (skipButton != null)
		{
			actionStatusText.gameObject.SetActive(value: false);
		}
		CONTROLLER.gameCompleted = false;
		if (CONTROLLER.PlayModeSelected != 6)
		{
			ResetPlayerScores();
		}
		ObscuredPrefs.GetInt("prevSixCount", CONTROLLER.sixMeterCount);
	}

	protected void Start()
	{
		rewardPopup.SetActive(value: false);
		hasHitSix = (hasHitFour = (hasHitFourAndSix = false));
		Time.timeScale = 1f;
		groundController = FindAnyObjectByType<GroundController>();
		skipButton.gameObject.SetActive(value: false);
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "contsix"))
		{
			CONTROLLER.continousSixes = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "contsix");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "contwik"))
		{
			CONTROLLER.continousWickets = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "contwik");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "contfour"))
		{
			CONTROLLER.continousBoundaries = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "contfour");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterFour"))
		{
			boundaryFoursCount = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterFour");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterSix"))
		{
			boundarySixesCount = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterSix");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterOppSix"))
		{
			opponentSixesCounter = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterOppSix");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterMaiden"))
		{
			maidenOversCount = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterMaiden");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterDot"))
		{
			dotBallCount = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterDot");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterWicketBowled"))
		{
			bowledWicketCount = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterWicketBowled");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterWicketCatch"))
		{
			caughtWicketCount = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterWicketCatch");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterWicketOthers"))
		{
			otherWicketCount = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterWicketOthers");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterFifty"))
		{
			fiftyRunsCount = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterFifty");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterCentury"))
		{
			hundredRunsCount = ObscuredPrefs.GetInt("CounterCentury");
		}
		if (ObscuredPrefs.HasKey(CONTROLLER.PlayModeSelected + "CounterPerOversPlayed"))
		{
			oversPlayedCounter = ObscuredPrefs.GetInt(CONTROLLER.PlayModeSelected + "CounterPerOversPlayed");
		}
		if (ObscuredPrefs.HasKey("CounterPlayerDuckOut"))
		{
			duckOutCount = ObscuredPrefs.GetInt("CounterPlayerDuckOut");
		}
		if (!CONTROLLER.GameStartsFromSave || CONTROLLER.PlayModeSelected == 6)
		{
			if (GameConstants.isWithAI == false)
			{
				// Reconnect-corruption fix: wait until OnStartClient has run (ClientStarted) before NewGame,
				// so the reconnect determination is complete first. Otherwise NewGame (and its Gap-C
				// match-progress push) could run on a reconnecting client BEFORE any reconnect flag was set
				// — pushing reset/zeroed indices+totals onto the authoritative SyncVars and flipping the
				// innings on both screens (target→1, scores 0/0). On the dedicated server (NetworkServer
				// .active) OnStartClient never fires, so don't gate on it there.
				this.DelayUntil(() => CricketNetworkManager.instance != null
					&& (Mirror.NetworkServer.active || CricketNetworkManager.instance.ClientStarted), () =>
				{
					NewGame();
				});

			}
			else
			{
				NewGame();

			}
		}
		else
		{
			Singleton<NavigationBack>.instance.disableDeviceBack = false;
			ContinueGame();
		}
		Vector2 vector = new Vector2(matchRenderCamera.pixelWidth, matchRenderCamera.pixelHeight);
		Vector3 vector2 = matchRenderCamera.ScreenToWorldPoint(new Vector3(vector.x, 0f, 0f));
		if (ObscuredPrefs.HasKey("tstatus"))
		{
			CONTROLLER.tournamentStatus = ObscuredPrefs.GetString("tstatus");
		}
	}


	private void GetDifficultyMode()
	{
		if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
		{
			CONTROLLER.difficultyMode = "easy";
		}

	}

	public void GetMyTeamList()
	{
		gameXmlAsset = Resources.Load("XML/Teams/CHE") as TextAsset;
		XMLReader.ParseXML(gameXmlAsset.text);
		GetOppTeamList();
	}

	private void GetOppTeamList()
	{
		gameXmlAsset = Resources.Load("XML/Teams/CHE") as TextAsset;
		XMLReader.ParseXML(gameXmlAsset.text);
	}

	private void ShowSuperOverResult()
	{
		InningsCompleteFn();
	}

	private void SaveSOLevelDetails()
	{
		string empty = string.Empty;
		empty = empty + CONTROLLER.CurrentLevelCompleted + "|";
		empty = empty + CONTROLLER.LevelFailed + "|";
		empty = empty + CONTROLLER.LevelId + "|";
		ObscuredPrefs.SetString("SuperOverLevelDetail", empty);
	}

	public void ResetCurrentMatchDetails()
	{
		currentBallNumber = -1;
		_ballOutcomeIsFromNetwork = false; // reset per-innings ball sync gate
		CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchBalls = 0;
		CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchScores = 0;
		CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchWickets = 0;
		newBatsmanEntryIndex = 1;
		CONTROLLER.StrikerIndex = 0;
		CONTROLLER.NonStrikerIndex = 1;
		CONTROLLER.totalSixes = 0;
		CONTROLLER.totalFours = 0;
		CONTROLLER.continousBoundaries = 0;
		CONTROLLER.continousSixes = 0;
		for (int i = 0; i < CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList.Length; i++)
		{
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.RunsScored = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.BallsPlayed = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.Fours = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.Sixes = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.Status = string.Empty;
		}
		if (CONTROLLER.PlayModeSelected != 6)
		{
			Singleton<BattingScoreCard>.instance.ResetBattingCard();
		}
	}

	public void UpdateChaseTargetLevel()
	{
		if (CONTROLLER.CTSubLevelCompleted < CONTROLLER.SubLevelCompletedArray.Length && CONTROLLER.CTLevelCompleted < CONTROLLER.MainLevelCompletedArray.Length)
		{
			currentSubLevel = CONTROLLER.CTSubLevelCompleted;
			//if (SuccessfulChase && CONTROLLER.SubLevelCompletedArray[CONTROLLER.CTSubLevelCompleted] == 0 && CONTROLLER.MainLevelCompletedArray[CONTROLLER.CTLevelCompleted] == 0 && !SuperChaseResult.isReplayMode)
			if (isChaseSuccessful && CONTROLLER.SubLevelCompletedArray[CONTROLLER.CTSubLevelCompleted] == 0 && CONTROLLER.MainLevelCompletedArray[CONTROLLER.CTLevelCompleted] == 0)
			{
				isChaseSuccessful = false;
				CONTROLLER.SubLevelCompletedArray[CONTROLLER.CTSubLevelCompleted] = 1;
				CONTROLLER.CTSubLevelCompleted++;
				if (CONTROLLER.CTSubLevelCompleted == 5)
				{
					CONTROLLER.MainLevelCompletedArray[CONTROLLER.CTLevelCompleted] = 1;
					ObscuredPrefs.SetInt("CTMainArray", CONTROLLER.CTLevelCompleted);
					if (CONTROLLER.CTLevelCompleted < googleDisplayNames.Length - 1)
					{
						CONTROLLER.CTLevelCompleted++;
						CONTROLLER.CTSubLevelCompleted = 0;
					}
					else
					{
						CONTROLLER.CTSubLevelCompleted = 5;
					}
				}
				string empty = string.Empty;
				empty = empty + CONTROLLER.CTLevelCompleted + "|";
				empty = empty + CONTROLLER.CTSubLevelCompleted + "|";
				ObscuredPrefs.SetString("ChaseTargetLevelDetail", empty);
			}
		}
		ClearLevelDetails();
	}

	public int GetCurrentSubLevel()
	{
		return currentSubLevel;
	}

	public void ClearLevelDetails()
	{
		if (ObscuredPrefs.HasKey("ChaseTargetDetail") && CONTROLLER.gameMode == "chasetarget")
		{
			ObscuredPrefs.DeleteKey("ChaseTargetDetail");
			ObscuredPrefs.DeleteKey("chasetargetPlayerDetails");
		}
	}

	private void ResetPlayerScores()
	{
		if (CONTROLLER.myTeamIndex >= 0)
		{
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchScores = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchBalls = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchWickets = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchExtras = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchLbs = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchbyes = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchNoball = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchWideBall = 0;
		}
		if (CONTROLLER.opponentTeamIndex >= 0)
		{
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchScores = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchBalls = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchWickets = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchExtras = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchLbs = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchbyes = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchNoball = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchWideBall = 0;
		}
		int num = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList.Length;
		for (int i = 0; i < num; i++)
		{
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].ConfidenceVal = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].ConfidenceLevel;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].LoftMeterFillVal = loftAngles[i];
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].reachedHalfCentury = false;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].reachedCentury = false;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].ConfidenceVal = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].ConfidenceLevel;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].LoftMeterFillVal = loftAngles[i];
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].reachedHalfCentury = false;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].reachedCentury = false;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.Status = string.Empty;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.RunsScored = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.BallsPlayed = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.Fours = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.Sixes = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BatsmanList.FOW = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BatsmanList.Status = string.Empty;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BatsmanList.RunsScored = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BatsmanList.BallsPlayed = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BatsmanList.Fours = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BatsmanList.Sixes = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BatsmanList.FOW = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BowlerList.RunsGiven = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BowlerList.BallsBowled = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BowlerList.Maiden = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BowlerList.Wicket = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BowlerList.maxBallInMatch = 0;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BowlerList.WicketsInBallCount = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.RunsGiven = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.BallsBowled = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.Maiden = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.Wicket = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.maxBallInMatch = 0;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.WicketsInBallCount = 0;
		}
	}

	public void NewGame()
	{
		Singleton<BattingControls>.instance.LoftMeterFill(CONTROLLER.StrikerIndex);

		DeleteKeys();
		DeleteAchievementFlags();
		FormatNames();
		TrimLastSpaces();
		StartGame();
		PostGoogleAnalyticsEvent();
		// ─── Reconnect race-fix ───
		// NewGame ALWAYS zeroes the match and runs on every MP scene-load, including
		// reconnect. On a mid-match reconnect the authoritative SyncVars are already on
		// the client, so re-apply them HERE (after the zero) — this is immune to the race
		// with the 2s-delayed ApplyReconnectState path that was leaving the reconnecting
		// player stuck at 0/0.
		// RECONNECT-ONLY (game-start bowler-side desync, log 03-07 08:19): the old assumption
		// "on a fresh match the SyncVars are still at their defaults, so the restore is a no-op"
		// is FALSE when the dedicated server session persists across matches — the CNM SyncVars
		// still hold the PREVIOUS match's values (e.g. syncedBowlerSide="right"), and this restore
		// applied them 20ms AFTER NewInnings had force-set/pinned/pushed LEFT → the bowling client
		// silently flipped to RIGHT at ball one while the batting client stayed LEFT. Only restore
		// during an actual reconnect; a fresh start must keep the just-initialized state.
		bool _reconnectRestore = CricketNetworkManager.RestorePending
			|| (Launcher.Instance != null && Launcher.Instance.IsReConnecting());
		if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null && _reconnectRestore)
		{
			if (CricketNetworkManager.instance.RestoreMatchStateFromSyncVars())
			{
				Singleton<Scoreboard>.instance?.UpdateScoreCard();
			}
			else if (Launcher.Instance != null && Launcher.Instance.IsReConnecting())
			{
				// Persistent-zero fix: the NewGame launch gate (GameData.Start) only waits for
				// CricketNetworkManager.instance to EXIST, not for its SyncVar PAYLOAD to deserialize.
				// On a slow reconnect the restore above no-ops (SyncVars still at -1/0 defaults) and
				// the zeroed match sticks → "score/balls reset to 0". Retry in the background until
				// the authoritative state actually arrives (bounded), then refresh the scoreboard.
				StartCoroutine(RetryRestoreMatchStateFromSyncVars());
			}
		}
	}

	private IEnumerator RetryRestoreMatchStateFromSyncVars()
	{
		float elapsed = 0f;
		while (elapsed < 6f)
		{
			yield return null;
			elapsed += Time.unscaledDeltaTime;
			if (CricketNetworkManager.instance != null
				&& CricketNetworkManager.instance.RestoreMatchStateFromSyncVars())
			{
				Singleton<Scoreboard>.instance?.UpdateScoreCard();
				Debug.Log("[GameData][RetryRestoreMatchStateFromSyncVars] Late authoritative state restored after reconnect.");
				yield break;
			}
		}
		Debug.Log("[GameData][RetryRestoreMatchStateFromSyncVars] Timed out waiting for authoritative SyncVars.");
	}

	private void ContinueGame()
	{
		Singleton<BattingControls>.instance.LoftMeterFill(CONTROLLER.StrikerIndex);

		{
			CONTROLLER.totalWickets = 10;
		}
		AutoSave.LoadGame();

		FormatNames();
		TrimLastSpaces();
		RestartGame();

	}

	public void StartGame()
	{
		CONTROLLER.NewInnings = true;
		if (CONTROLLER.PlayModeSelected == 4)
		{

		}
		else
		{
			CONTROLLER.totalWickets = 10;
		}

		if (CONTROLLER.PlayModeSelected == 7)
		{
		}
		else
		{
			CONTROLLER.currentInnings = 0;
		}
		ResetVariables();
		CONTROLLER.achievements = string.Empty;
	}

	private void CheckIfDayIsOver()
	{
		if (CONTROLLER.ballsBowledPerDay >= CONTROLLER.Overs[ObscuredPrefs.GetInt("TMOvers")] * 6)
		{
			CONTROLLER.ballsBowledPerDay = 0;
			CONTROLLER.currentDay++;
			CONTROLLER.currentSession = 0;
			Singleton<Scoreboard>.instance.UpdateScoreCard();
		}
	}


	public void RestartGame()
	{
		currentBallNumber = AutoSave.currentBall;
		runsInCurrentOver = AutoSave.runsScoredInOver;
		wicketsThisOver = AutoSave.WicketsInOver;
		hasReachedMaxRuns = AutoSave.maxRunsReached;
		hasReachedMaxWides = AutoSave.maxWidesReached;
		partnershipRunCount = AutoSave.currentPartnership;
		fourScoredInOver = AutoSave.fourInOver;
		sixScoredInOver = AutoSave.sixInOver;
		hasReachedMaxSixes = AutoSave.maxSixes;
		if (CONTROLLER.PlayModeSelected == 7)
		{
		}
		else
		{
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.currPatnerShipRuns = CONTROLLER.strikerPartnershipRuns;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.currPatnerShipBalls = CONTROLLER.strikerPartnershipBall;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.currPatnerShipRuns = CONTROLLER.NonstrikerPartnershipRuns;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.currPatnerShipBalls = CONTROLLER.NonstrikerPartnershipBall;
			newBatsmanEntryIndex = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets + 1;
			if (newBatsmanEntryIndex >= CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList.Length)
			{
				newBatsmanEntryIndex = CONTROLLER.totalWickets;
			}
			for (int k = 0; k < AutoSave.ballUpdate.Length; k++)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[k] = Convert.ToString(AutoSave.ballUpdate[k]);
			}
			for (int l = AutoSave.ballUpdate.Length; l < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate.Length; l++)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[l] = string.Empty;
			}
		}
		scoreMoreThanCentury(CONTROLLER.StrikerIndex);
		scoreMoreThanCentury(CONTROLLER.NonStrikerIndex);
		if (Singleton<BallSimulationManager>.instance.CanShowBallSimulation())
		{
			Singleton<BallSimulationManager>.instance.loadSavedData = true;
		}
		string[] array = AutoSave.ballListInfo.Split('|');
		for (int m = 0; m < array.Length - 1 && !(array[m] == " "); m++)
		{
			Singleton<ScoreBoardBallList>.instance.AddBall(array[m], " ");
		}
		array = AutoSave.ballextras.Split('|');
		for (int n = 0; n < array.Length - 1; n += 2)
		{
			Singleton<ScoreBoardBallList>.instance.extras.Add(array[n]);
			Singleton<ScoreBoardBallList>.instance.extras.Add(array[n + 1]);
		}
		if (CONTROLLER.currentInnings == 1 && CONTROLLER.PlayModeSelected != 7)
		{
			CONTROLLER.TargetToChase = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores + 1;
			RepairCollapsedTarget();
		}
		Singleton<AIFieldingSetupManager>.instance.SetSavedField();
		SetTeamIndex();
		SetFielders();
		if (CONTROLLER.PlayModeSelected == 7)
		{

		}
		else
		{
			scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			extraRunsDisplayString = LocalizationData.localizationInstance.getText(235) + " " + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchExtras;
		}
		oversDisplayString = GetOverStr() + "(" + CONTROLLER.totalOvers + ")";

		Singleton<BattingScoreCard>.instance.RestartGame();
		Singleton<BowlingScoreCard>.instance.ResetBowlingCard();
		Singleton<PreviewScreen>.instance.SetFieldPreview();
		Singleton<BowlingScoreCard>.instance.RestartGame();
		Singleton<Scoreboard>.instance.UpdateScoreCard();
		groundController.NewInnings();
		groundController.ResetFielders();
		groundController.RestartBowlerSide(CONTROLLER.BowlerSide);

		isInningsComplete = CheckForInningsComplete();
		if (isInningsComplete)
		{
			InningsCompleteFn();
		}
		else if (currentBallNumber == 5)
		{
			groundController.showPreviewCamera(status: false);
			NewOver();
		}
		else
		{
			BowlNextBall();
		}
	}

	public void ResetVariables()
	{
		Singleton<GroundController>.instance.batsmanObject.transform.position = new Vector3(-65f, 0f, -6f);
		canPauseGameplay = false;
		newBatsmanEntryIndex = 0;
		currentBallNumber = -1;
		runsInCurrentOver = 0;
		isWicketBall = false;
		batsmanDismissedIndex = -1;
		isInningsComplete = false;
		isLevelFinished = false;
		replayStartDelay = 1f;
		currentAction = -1;
		wicketsThisOver = 0;
		ObscuredPrefs.DeleteKey("partnershipXpGained");
		partnershipRunCount = 0;
		replayBlinkStatus = 0;
		hasReachedMaxSixes = false;
		isBallValid = true;
		sixScoredInOver = false;
		fourScoredInOver = false;
		replayFinishedTriggered = false;
		previousMousePosition = default(Vector2);
		hasReachedMaxWides = false;
		totalRunsScored = 0;
		extraRuns = 0;
		wasWicket = 0;
		CONTROLLER.SCORE = string.Empty;
		CONTROLLER.EXTRAINFO = string.Empty;
		NewInnings();
	}

	public void ResetAllLocalVariables()
	{
		if (CONTROLLER.PlayModeSelected == 6)
		{
			//CanPauseGame = false;
			//IsWicketBall = false;
			//inningsCompleted = false;
			//levelCompleted = false;
			//action = -1;
			//WicketsInOver = 0;
			//currentBall = -1;
			//CONTROLLER.currentMatchBalls = 0;
			//CONTROLLER.currentMatchScores = 0;
			//CONTROLLER.currentMatchWickets = 0;
		}
	}

	public void DeleteKeys()
	{
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "duckout");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "3wides");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "aifirstsix");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "lastwicket");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "firstwicket");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "lastball");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterFour");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterSix");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterMaiden");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterDot");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterWicketBowled");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterWicketCatch");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterWicketOthers");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterFifty");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterCentury");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterPerOversPlayed");
		ObscuredPrefs.DeleteKey(CONTROLLER.PlayModeSelected + "CounterOppSix");
		boundaryFoursCount = (boundarySixesCount = (maidenOversCount = (bowledWicketCount = (caughtWicketCount = (otherWicketCount = (fiftyRunsCount = (hundredRunsCount = (oversPlayedCounter = (opponentSixesCounter = 0)))))))));
	}

	public void NewInnings()
	{
		//if (CONTROLLER.PlayModeSelected == 7)
		//{
		//	CONTROLLER.NewInnings = true;
		//	CONTROLLER.GameStartsFromSave = false;
		//	ObscuredPrefs.DeleteKey("perMatchEdgeCount" + CONTROLLER.PlayModeSelected);
		//}
		CONTROLLER.TeamList[CONTROLLER.myTeamIndex].noofDRSLeft = 2;
		CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].noofDRSLeft = 2;
		SetTeamIndex();

		// Reconnect early-innings fix (Gap C): push the FULL snapshot (innings + both teams'
		// totals + batting/bowling indices) the MOMENT the innings flips — not only after the new
		// batting player commits their first ball. Otherwise a reconnect in that sub-ball window
		// restores the stale previous-innings roles. Batting authority only; NEVER during a
		// reconnect (would overwrite the server's good SyncVars with this local NewGame state).
		if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
			&& CricketNetworkManager.instance != null
			&& CONTROLLER.TeamList != null
			&& CONTROLLER.BattingTeamIndex >= 0 && CONTROLLER.BowlingTeamIndex >= 0
			&& CONTROLLER.BattingTeamIndex < CONTROLLER.TeamList.Length
			&& CONTROLLER.BowlingTeamIndex < CONTROLLER.TeamList.Length
			&& CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
			&& (Launcher.Instance == null || !Launcher.Instance.IsReConnecting()))
		{
			// t0 = batting team, t1 = bowling team, captured by ACTUAL index (teams sit at 4/9, not 0/1
			// — pushing TeamList[0]/[1] sent empty teams → target=1 on 2nd-innings reconnect).
			CricketNetworkManager.instance.CmdSyncMatchProgress(
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls,
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores, CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchWickets, CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchBalls,
				CONTROLLER.currentInnings, CONTROLLER.BattingTeamIndex, CONTROLLER.BowlingTeamIndex);
			CricketNetworkManager.instance.PushMatchSnapshotIfBatting();  // Phase 0: full-snapshot push (alongside the synced* path)
		}
		CONTROLLER.RunRate = 0f;
		CONTROLLER.NewOver = true;
		CONTROLLER.StrikerIndex = 0;
		CONTROLLER.NonStrikerIndex = 1;
		CONTROLLER.continousWickets = 0;
		CONTROLLER.continousSixes = 0;
		CONTROLLER.continousBoundaries = 0;
		Singleton<PreviewScreen>.instance.alertPopup.SetActive(value: false);
		CONTROLLER.RunsInAOver = 0;
		CONTROLLER.WideInAOver = 0;
		CONTROLLER.DotInAOver = 0;
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//ScoreStr = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			//ExtraStr = LocalizationData.instance.getText(235) + " " + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchExtras;
		}
		else
		{
			scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			extraRunsDisplayString = LocalizationData.localizationInstance.getText(235) + " " + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchExtras;
		}
		if (CONTROLLER.PlayModeSelected != 6)
		{
			oversDisplayString = GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
		}
		int num = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 2 * 6 + 1;
		CONTROLLER.Wagon_PlayedBy = new int[num];
		CONTROLLER.Wagon_RunScored = new int[num];
		CONTROLLER.Wagon_NoOfBounce = new int[num];
		CONTROLLER.Wagon_BallAngle = new float[num];
		CONTROLLER.Wagon_FirstPitchPoint = new Vector3[num];
		CONTROLLER.Wagon_SecondPitchPoint = new Vector3[num];
		CONTROLLER.Wagon_ThirdPitchPoint = new Vector3[num];
		CONTROLLER.Wagon_FinalPitchPoint = new Vector3[num];
		CONTROLLER.Wagon_FirstPitchHeight = new float[num];
		CONTROLLER.Wagon_SecondPitchHeight = new float[num];
		CONTROLLER.Wagon_ThirdPitchHeight = new float[num];
		groundController.NewInnings();
		groundController.ResetFielders();
		if (CONTROLLER.PlayModeSelected == 6)
		{
			//introCompleted();
		}
		else if (CONTROLLER.PlayModeSelected == 4)
		{
			//Singleton<SuperOverLevelInfo>.instance.ShowMe();
		}
		else if (CONTROLLER.PlayModeSelected == 5)
		{
			//Singleton<SuperChaseLevelInfo>.instance.ShowMe();
		}
		else
		{
			ShowGroundPreview();
		}
		Singleton<Tutorial>.instance.hideTutorial();
	}

	private void SetTeamIndex()
	{
		// Reconnect role-swap fix (Gap A): NewGame zeroes currentInnings, so on a 2nd-innings
		// reconnect the derivation below picks the 1st-innings (SWAPPED) batting/bowling teams,
		// and the fielder/camera/bowler-side layout (groundController.NewInnings, run right after
		// this) uses those wrong indices — visible as the ~1s batting/bowling swap until
		// ApplyReconnectState corrects it ~2s later. Restore the live innings from the authoritative
		// snapshot FIRST so the existing derivation produces the CORRECT indices the first time.
		bool reconnectAuthoritativeIndices = false;
		// Gate fix (R1/R2/R3 team-shuffle on reconnect): during the restore the Ground scene has RELOADED, so
		// Launcher.Instance can be NULL (IsReConnecting() can't even be evaluated) — yet CricketNetworkManager
		// .RestorePending (a STATIC that survives the reload, set in DispatchReconnectRestore, cleared at the END
		// of ApplyReconnectState) is true for the WHOLE restore window. Keying only on Launcher.IsReConnecting()
		// left a window (logs: reconnect=False restorePending=True syncedInnings=1) where this stayed false, so
		// the derivation below fell back to the LOCAL currentInnings (reset to 0 by NewGame) and produced the
		// 1st-innings SWAPPED teams on a 2nd-innings reconnect → visible team shuffle + wrong bowler picked
		// (tester: "disconnect karne se teams shuffle / bowler change ho jata hai"). Accept RestorePending too.
		if (CONTROLLER.PlayModeSelected == 8
			&& ((Launcher.Instance != null && Launcher.Instance.IsReConnecting()) || CricketNetworkManager.RestorePending)
			&& CricketNetworkManager.instance != null && CricketNetworkManager.instance.syncedCurrentInnings >= 0)
		{
			CONTROLLER.currentInnings = CricketNetworkManager.instance.syncedCurrentInnings;
			// Flag that the AUTHORITATIVE absolute team indices are available (pushed by the batting
			// player). Used below to override the myTeamIndex-based derivation, which collapses to 0,0
			// on reconnect because myTeamIndex/opponentTeamIndex aren't restored from team selection.
			if (CricketNetworkManager.instance.syncedBattingTeamIndex >= 0
				&& CricketNetworkManager.instance.syncedBowlingTeamIndex >= 0)
			{
				reconnectAuthoritativeIndices = true;
			}
		}
		if (CONTROLLER.PlayModeSelected == 7)
		{

		}
		else
		{
			if (CONTROLLER.PlayModeSelected == 6)
			{

			}
			else if (CONTROLLER.currentInnings == 0)
			{
				Singleton<Scoreboard>.instance.ShowTargetScreen(boolean: false);
				if (CONTROLLER.meFirstBatting == 1)
				{
					CONTROLLER.BattingTeamIndex = CONTROLLER.myTeamIndex;
					CONTROLLER.BowlingTeamIndex = CONTROLLER.opponentTeamIndex;
				}
				else
				{
					CONTROLLER.BattingTeamIndex = CONTROLLER.opponentTeamIndex;
					CONTROLLER.BowlingTeamIndex = CONTROLLER.myTeamIndex;
				}
			}
			else
			{
				if (CONTROLLER.meFirstBatting == 1)
				{
					CONTROLLER.BattingTeamIndex = CONTROLLER.opponentTeamIndex;
					CONTROLLER.BowlingTeamIndex = CONTROLLER.myTeamIndex;
				}
				else
				{
					CONTROLLER.BattingTeamIndex = CONTROLLER.myTeamIndex;
					CONTROLLER.BowlingTeamIndex = CONTROLLER.opponentTeamIndex;
				}
				Singleton<Scoreboard>.instance.ShowTargetScreen(boolean: true);
			}
			// Reconnect "score not restoring" fix (bowling player): the derivation above sets
			// BattingTeamIndex/BowlingTeamIndex from myTeamIndex/opponentTeamIndex, but on reconnect those
			// collapse to 0,0 (not restored from team selection) → BattingTeamIndex == BowlingTeamIndex == 0,
			// so the live score landed on the wrong TeamList entry and never showed on the reconnecting
			// BOWLING player (the batting player self-heals because it keeps pushing). Override with the
			// AUTHORITATIVE absolute indices the batting player pushed, and recover which absolute team is
			// "mine" (meFirstBatting is invariant across the match; innings parity says if I'm batting now)
			// so later role checks (myTeamIndex == BattingTeamIndex) and the next-innings derivation are right.
			if (reconnectAuthoritativeIndices && CricketNetworkManager.instance != null)
			{
				CONTROLLER.BattingTeamIndex = CricketNetworkManager.instance.syncedBattingTeamIndex;
				CONTROLLER.BowlingTeamIndex = CricketNetworkManager.instance.syncedBowlingTeamIndex;
				bool iAmBattingNow = (CONTROLLER.meFirstBatting == 1) == (CONTROLLER.currentInnings == 0);
				CONTROLLER.myTeamIndex = iAmBattingNow ? CONTROLLER.BattingTeamIndex : CONTROLLER.BowlingTeamIndex;
				CONTROLLER.opponentTeamIndex = iAmBattingNow ? CONTROLLER.BowlingTeamIndex : CONTROLLER.BattingTeamIndex;
				Singleton<Scoreboard>.instance.ShowTargetScreen(CONTROLLER.currentInnings != 0);
			}
			// R1 diag (#6/#8/#12/#13 team-swap + score 0/0): pins the reconnect role/innings restore. IsReConnecting()
			// already OR's RestorePending, so if teams still swap the cause is here — either the authoritative snapshot
			// hadn't arrived yet (syncedInnings/Bat/Bowl == -1 -> authIdx=false -> derivation used default myTeam=0), or
			// the snapshot/iAmBattingNow itself is wrong. Compare BOTH devices' lines for the same reconnect. (MpLog so
			// it reaches the ritu_mp file.)
			if (CONTROLLER.PlayModeSelected == 8)
				ConstantsData_M.MpLog($"[GameData][SetTeamIndex] reconnect={(Launcher.Instance != null && Launcher.Instance.IsReConnecting())} restorePending={CricketNetworkManager.RestorePending} authIdx={reconnectAuthoritativeIndices} syncedInnings={(CricketNetworkManager.instance != null ? CricketNetworkManager.instance.syncedCurrentInnings : -99)} syncedBat={(CricketNetworkManager.instance != null ? CricketNetworkManager.instance.syncedBattingTeamIndex : -99)} syncedBowl={(CricketNetworkManager.instance != null ? CricketNetworkManager.instance.syncedBowlingTeamIndex : -99)} meFirstBatting={CONTROLLER.meFirstBatting} innings={CONTROLLER.currentInnings} myTeam={CONTROLLER.myTeamIndex} opp={CONTROLLER.opponentTeamIndex} bat={CONTROLLER.BattingTeamIndex} bowl={CONTROLLER.BowlingTeamIndex}");
			// In multiplayer the host pushes its authoritative BattingTeamIndex
			// onto SyncVars so the joiner's CONTROLLER gets overridden via the
			// SyncVar hooks in CricketNetworkManager. This protects against the
			// case where the toss-decision RPC leaves the joiner's meFirstBatting
			// in an inverted state from the host's — which would otherwise place
			// the opponent at the wrong pitch end on the joiner (the "opponent
			// appears behind player" bug).
			// RECORD WHY THIS PUSH DOES OR DOES NOT HAPPEN. Across two full matches on 20-08 CmdSetBattingTeams
			// was sent ZERO times, so syncedBattingTeam/syncedBowlingTeam never moved off their innings-0
			// values and every reconnect after the innings swap restored the FIRST innings' teams.
			// NOTE: this repo still gates on NetworkServer.active while the client repo now gates on
			// isMasterClient — the two have diverged on exactly this line, which is worth resolving once the
			// logs say which gate can actually be true in the Edgegap relay topology.
			if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
				ConstantsData_M.MpLog($"[TeamPush] innings={CONTROLLER.currentInnings} bat={CONTROLLER.BattingTeamIndex} bowl={CONTROLLER.BowlingTeamIndex}"
					+ $" | cnm={(CricketNetworkManager.instance != null)} serverActive={Mirror.NetworkServer.active}"
					+ $" -> {((CricketNetworkManager.instance != null && Mirror.NetworkServer.active) ? "PUSHING" : "NOT pushing — synced team pair stays stale")}.");
			if (CONTROLLER.PlayModeSelected == 8
				&& CricketNetworkManager.instance != null
				&& Mirror.NetworkServer.active)
			{
				CricketNetworkManager.instance.CmdSetBattingTeams(
					CONTROLLER.BattingTeamIndex, CONTROLLER.BowlingTeamIndex);
			}
			if (CONTROLLER.ConfidenceVal == 1)
			{
			}
			if (CONTROLLER.PlayModeSelected == 6)
			{

			}
			else
			{
				CONTROLLER.totalOvers = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex];
				powerPlayOver = (int)Mathf.Floor((float)CONTROLLER.totalOvers * 0.3f);
				slogOverScore = (int)((float)CONTROLLER.totalOvers - Mathf.Floor((float)CONTROLLER.totalOvers * 0.2f));
			}
			CONTROLLER.wickerKeeperIndex = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].KeeperIndex;
			CONTROLLER.BattingTeamName = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName;
			CONTROLLER.BowlingTeamName = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName;
		}
		Singleton<Scoreboard>.instance.HideStrip(boolean: true);
	}

	public void ShowGroundPreview()
	{
		currentAction = -20;
		Singleton<Intro>.instance.initGameIntro();
	}

	public void introCompleted()
	{
		currentAction = -1;
		CONTROLLER.StrikerIndex = 0;
		CONTROLLER.NonStrikerIndex = 1;
		newBatsmanEntryIndex = CONTROLLER.NonStrikerIndex;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Status = "not out";
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.Status = "not out";

		Singleton<BattingScoreCard>.instance.ResetBattingCard();
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Status = "not out";
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.Status = "not out";

		Singleton<BowlingScoreCard>.instance.ResetBowlingCard();
		Singleton<BowlingScoreCard>.instance.OverCompleted();
		Singleton<Scoreboard>.instance.NewOver();
		// INNINGS-BOUNDARY snapshot push (tester #4: "second innings mein first innings ke last over ki
		// balls history fetch hui"). NewOver() above empties the chip strip for the new innings, but the
		// snapshot held on the server is still the one pushed during the PREVIOUS innings' last ball, and
		// it carries that over's six chips in ballListInfo. A client reconnecting in the second innings
		// restored them and showed the previous innings' history. Same fault, and the same fix, as the
		// over-boundary re-push in OverCompleted: publish again now that the strip is actually empty.
		// Batting authority only and never mid-reconnect — same gate as every other push.
		if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
			&& CricketNetworkManager.instance != null
			&& CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
			&& (Launcher.Instance == null || !Launcher.Instance.IsReConnecting()))
		{
			CricketNetworkManager.instance.PushMatchSnapshotIfBatting();
		}
		Singleton<Intro>.instance.GroundModel.transform.eulerAngles = Vector3.zero;
		Singleton<Intro>.instance.GroundQuad.SetActive(value: true);
		Singleton<GroundController>.instance.StopIntroFielderAnimation();
		Invoke("IntroOver", 0f);
	}

	public void IntroOver()
	{
		if (Singleton<Intro>.instance != null)
		{
			Singleton<Intro>.instance.destroyGO();
		}
		Singleton<BatsmanRecord>.instance.Hide(boolean: true);
		groundController.introCompleted();
		if (CONTROLLER.PlayModeSelected == 6 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5)
		{
		}
		else
		{
			ShowBattingScoreCard();
		}
		Invoke("HideFadeView", fadeEffectDuration);
	}

	public void HideFadeView()
	{
	}

	private void InitBatsmanInfo()
	{
		// Idempotent per wicket: the over-complete branch may also kick off the new-batsman flow on a
		// wicket that falls on the over's last ball, so guard against running the walk-in twice.
		if (wicketBatsmanFlowStarted)
		{
			return;
		}
		wicketBatsmanFlowStarted = true;
		canPauseGameplay = false;
		Singleton<BatsmanInfo>.instance.UpdateRecord(CONTROLLER.BattingTeamIndex, batsmanDismissedIndex);
		Invoke("ShowBatsmamInfoView", fadeEffectDuration);
	}

	public void ShowBatsmamInfoView()
	{
		Singleton<Scoreboard>.instance.HidePause(boolean: true);
		Singleton<Scoreboard>.instance.Hide(boolean: true);
		Singleton<PreviewScreen>.instance.Hide(boolean: true);
		Singleton<BatsmanInfo>.instance.Hide(boolean: false);
		currentAction = 3;
		Singleton<Intro>.instance.initBatsmanExit();
	}

	public void batsmanExitStopped()
	{
		currentAction = -1;
		Invoke("stoppedExitView", fadeEffectDuration);
	}

	public void stoppedExitView()
	{
		Singleton<BatsmanInfo>.instance.Hide(boolean: true);

		Singleton<Scoreboard>.instance.UpdateScoreCard();
		if (CONTROLLER.currentInnings < 2)
		{
			Singleton<BattingScoreCard>.instance.SelectedInnings = 1;
		}
		else
		{
			Singleton<BattingScoreCard>.instance.SelectedInnings = 2;
		}
		Singleton<BattingScoreCard>.instance.UpdateWicket(batsmanDismissedIndex);
		Singleton<BattingScoreCard>.instance.UpdateScoreCard();
		Singleton<BowlingScoreCard>.instance.UpdateScoreCard();
		bool flag = CheckForAllOut();
		bool flag2 = CheckForInningsComplete();

		if (!flag && !flag2)
		{
			InitBatsmanRecord();
			return;
		}
		if (Singleton<Intro>.instance != null)
		{
			Singleton<Intro>.instance.destroyGO();
		}
		groundController.introCompleted();
		CheckForOverComplete();
	}

	private void InitBatsmanRecord()
	{
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[newBatsmanEntryIndex].BatsmanList.Status = "not out";

		Singleton<BatsmanRecord>.instance.UpdateRecord(CONTROLLER.BattingTeamIndex, newBatsmanEntryIndex);
		ShowBatsmanRecord();
	}

	private void ShowBatsmanRecord()
	{
		if (Singleton<Intro>.instance != null)
		{
			Singleton<Intro>.instance.initBatsmanEntry();
		}
		else
		{
			Singleton<Intro>.instance.initBatsmanEntry();
		}
		Singleton<BatsmanRecord>.instance.Hide(boolean: false);
		Invoke("HoldRecordView", fadeEffectDuration);
	}

	public void HoldRecordView()
	{
		currentAction = 2;
	}

	public void batsmanEntryStopped()
	{
		currentAction = -1;
		Invoke("BatsmanEntryView", fadeEffectDuration);
	}

	public void BatsmanEntryView()
	{
		if (Singleton<Intro>.instance != null)
		{
			Singleton<Intro>.instance.GroundModel.transform.eulerAngles = Vector3.zero;
			Singleton<Intro>.instance.GroundQuad.SetActive(value: true);
			Singleton<Intro>.instance.destroyGO();
		}
		Singleton<PreviewScreen>.instance.Hide(boolean: false);
		Singleton<BatsmanRecord>.instance.Hide(boolean: true);
		groundController.introCompleted();
		CheckForOverComplete();
		Invoke("BatsmanInfoView", fadeEffectDuration);
	}

	public void BatsmanInfoView()
	{
	}

	protected void Update()
	{
		if (GameConstants.isWithAI == false)
		{
			// if (SnokerNetwork.Instance.isLocalPlayer)
			{
				if (ping)
					ping.text = ((int)(NetworkTime.rtt * 1000)).ToString() + " ms";
			}
		}
		if (hasGameQuit || shouldSkipUpdate || !CONTROLLER.GameIsOnFocus)
		{
			return;
		}
		switch (currentAction)
		{
			case -20:
				if (Singleton<Intro>.instance != null)
				{
					Singleton<Intro>.instance.UpdateIntro();
				}
				break;
			case 2:
				if (Singleton<Intro>.instance != null)
				{
					Singleton<Intro>.instance.UpdateIntro();
				}
				break;
			case 3:
				if (Singleton<Intro>.instance != null)
				{
					Singleton<Intro>.instance.UpdateIntro();
				}
				break;
		}
		GetKeyBoardInput();
		// Mac/desktop-standalone "batsman doesn't play the shot" fix: this picks TOUCH input
		// (DetectBatsmanMove/Shot) vs MOUSE input (…Mouse). CONTROLLER.TargetPlatform is NEVER assigned
		// (declared string.Empty, no setter), so the original guard collapsed to just `!Application.isEditor`
		// → EVERY non-editor build (incl. Mac/Windows standalone) took the TOUCH path. Touch never fires on
		// desktop (Input.touchCount==0) → no shot, while Editor (mouse) + Android (touch) worked. Add the
		// real platform test: only a genuine MOBILE device uses touch; everything else (standalone/web/editor)
		// uses the mouse path. Android/iOS unchanged.
		if (!Application.isEditor && CONTROLLER.TargetPlatform != "standalone" && CONTROLLER.TargetPlatform != "web" && Application.isMobilePlatform)
		{
			switch (userInputAction)
			{
				case 10:
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						DetectBatsmanMove();
					}
					break;
				case 11:
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						DetectBatsmanShot();
					}
					break;
			}
			if (canPauseGameplay && !CONTROLLER.ReplayShowing)
			{
				Singleton<Scoreboard>.instance.HidePause(boolean: false);
			}
			else
			{
				Singleton<Scoreboard>.instance.HidePause(boolean: true);
			}
		}
		else
		{
			switch (userInputAction)
			{
				case 10:
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						DetectBatsmanMoveMouse();
					}
					break;
				case 11:
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						DetectBatsmanShotMouse();
					}
					break;
			}
		}
		if (currentAction == 2 || currentAction == 3 || currentAction == -20 || Singleton<BallSimulationManager>.instance.isShowingSimulation)
		{
			skipPanel.SetActive(value: true);
		}
		else
		{
			skipPanel.SetActive(value: false);
		}
		if (CONTROLLER.ReplayShowing || skipButtonStatus == "stumpingReplay")
		{
			BlinkActionReplay();
		}
	}

	public void BlinkActionReplay()
	{
		if (timeoutBlinkDuration + 0.25f < Time.time)
		{
			timeoutBlinkDuration = Time.time;
			if (replayBlinkStatus == 0)
			{
				actionStatusText.color = new Color(1f, 0f, 0f, 1f);
				replayBlinkStatus = 1;
			}
			else
			{
				replayBlinkStatus = 0;
				actionStatusText.color = new Color(1f, 1f, 1f, 1f);
			}
		}
		if (replayStartDelay + 2f < Time.time && replayStartDelay > 0f)
		{
			replayStartDelay = -1f;
			if (skipButtonStatus == "stumpingReplay" || skipButtonStatus == "thirdUmpireRunoutReplay")
			{
				actionStatusText.text = LocalizationData.localizationInstance.getText(518);
			}
			else
			{
				actionStatusText.text = LocalizationData.localizationInstance.getText(519);
			}
		}
	}

	public void ShowBattingScoreCard()
	{
		Singleton<NavigationBack>.instance.disableDeviceBack = false;
		groundController.showPreviewCamera(status: false);
		if (CONTROLLER.PlayModeSelected != 6)
		{
			matchProgressBar.fillAmount = 0f;
			currentScoreText.text = "0";
			currentWicketsText.text = "0";
			scoreGenerationPanel.SetActive(value: false);
			if (!isGamePaused)
			{
				currentAction = 0;
			}
			Singleton<PauseGameScreen>.instance.Hide(boolean: true);
			canPauseGameplay = false;
			groundController.isSlipShot = false;
			if (CONTROLLER.PlayModeSelected == 4 && CONTROLLER.SuperOverMode == "bowl")
			{

			}
			else if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls == 0 || isInningsComplete || isFromMainMenu || CONTROLLER.PlayModeSelected > 3)
			{
				////Debug.Log("BOwloinngS SCoreedcard");

				Singleton<PauseGameScreen>.instance.BG.SetActive(value: true);
				Singleton<BattingScoreCard>.instance.Hide(boolean: false);
				Singleton<BattingScoreCard>.instance.UpdateScoreCard();
				Singleton<BattingScoreCard>.instance.SCTeamName.DOFade(1f, 0f);
				isFromMainMenu = false;
			}
			else
			{
				Singleton<AfterOverSummary>.instance.ShowMe();
			}
		}
	}

	public void ShowScoreCard()
	{
		currentAction = -1;
		if (CONTROLLER.isFromAutoPlay)
		{
			AutoSave.SaveInGameMatch();
		}
		if (!isInningsComplete)
		{
			SetFielders();
			isGamePaused = false;
			groundController.canShowCountdown = false;
			groundController.GameIsPaused(pauseStatus: false);
			Singleton<BowlingScoreCard>.instance.ContinueSelected();
			Singleton<Scoreboard>.instance.UpdateScoreCard();
			BowlNextBall();
			return;
		}
		canPauseGameplay = false;
		Singleton<Scoreboard>.instance.HidePause(boolean: true);
		if (CONTROLLER.currentInnings == 0 || CONTROLLER.PlayModeSelected == 7)
		{
			Singleton<Scoreboard>.instance.Hide(boolean: true);
			Singleton<PreviewScreen>.instance.Hide(boolean: true);
			Singleton<BowlingControls>.instance.Hide(boolean: true);
			Singleton<BattingControls>.instance.Hide(boolean: true);
			if (!(CONTROLLER.STORE != "facebook"))
			{
				return;
			}
			if (CONTROLLER.PlayModeSelected == 7)
			{
			}
			else
			{
				Singleton<TargetScreen>.instance.Hide(boolean: false);
				currentAction = 10;
			}
		}
		else
		{
			CONTROLLER.gameCompleted = true;
			Singleton<Scoreboard>.instance.Hide(boolean: true);
			Singleton<PreviewScreen>.instance.Hide(boolean: true);
			Singleton<BowlingControls>.instance.Hide(boolean: true);
			Singleton<BattingControls>.instance.Hide(boolean: true);
		}
	}

	public void BowlNextBall()
	{
		////Debug.Log("&**BOWWWLLLINGG");
		Singleton<GroundController>.instance.hasTakenRun = false;
		Singleton<GroundController>.instance.hasCancelledRun = false;
		isFromMainMenu = false;
		canPlayAnimation = true;
		savedNonStrikerIndex = CONTROLLER.NonStrikerIndex;
		savedStrikerIndex = CONTROLLER.StrikerIndex;
		savedNewStrikerIndex = newBatsmanEntryIndex;
		isNewTouch = false;
		touchStartPosition = Vector2.zero;
		touchEndPosition = Vector2.zero;
		chosenBattingAngle = 0;
		isShotCompleted = false;
		userInputAction = -1;
		previousMousePosition = Vector2.zero;
		NewBall();
		SetGameDatas();
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			if (CONTROLLER.PlayModeSelected == 8)
			{
				groundController.BowlNextBall("user", "user");
			}
			else
			{
				groundController.BowlNextBall("user", "computer");
			}
		}
		else
		{
			if (CONTROLLER.PlayModeSelected == 8)
			{
				groundController.BowlNextBall("user", "user");
			}
			else
			{
				groundController.BowlNextBall("computer", "user");
			}
		}
		Singleton<BattingControls>.instance.LoftMeterFill(CONTROLLER.StrikerIndex);
		if (CONTROLLER.fielderPrevIndex != CONTROLLER.fielderChangeIndex)
		{
			CONTROLLER.fielderPrevIndex = CONTROLLER.fielderChangeIndex;
		}
	}

	private void NewBall()
	{
		groundController.ClearTrace();
		if (!isWicketBall)
		{
			CONTROLLER.HattrickBall = false;
		}
		batsmanDismissedIndex = -1;
		isWicketBall = false;
		wicketBatsmanFlowStarted = false;
		isShotCompleted = false;
		if (CONTROLLER.PlayModeSelected == 6)
		{
			//ScoreBoardMultiPlayer.instance.Hide(boolean: false);
		}
		else
		{
			Singleton<Scoreboard>.instance.Hide(boolean: false);
		}
		Singleton<PreviewScreen>.instance.Hide(boolean: false);
	}

	public void SetGameDatas()
	{
		BatsmenInfo batsmanList = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList;
		if (batsmanList.BattingHand == "L")
		{
			CONTROLLER.StrikerHand = "left";
		}
		else if (batsmanList.BattingHand == "R")
		{
			CONTROLLER.StrikerHand = "right";
		}
		BowlerInfo bowlerList = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList;
		if (bowlerList.BowlingHand == "L")
		{
			CONTROLLER.BowlerHand = "left";
		}
		else if (bowlerList.BowlingHand == "R")
		{
			CONTROLLER.BowlerHand = "right";
		}
		if (CONTROLLER.PlayModeSelected == 4)
		{

		}
		else
		{
			// bowlingRank is EMPTY for any player who is not a bowler (SquadPage treats "" as
			// "cannot bowl"), so a bare int.Parse throws FormatException whenever CurrentBowlerIndex
			// lands on such a player — which is exactly what happened on the reconnect restore path
			// (RefreshBowlerTypeFromRestoredIndex -> here), taking the rest of ApplyReconnectState
			// down with it. Keep the previous BowlerType on a bad rank instead of throwing.
			string _rank = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.bowlingRank;
			if (int.TryParse(_rank, out int _parsedRank))
				CONTROLLER.BowlerType = _parsedRank;
			else
				ConstantsData_M.MpLog($"[GameData][SetGameDatas] bowlingRank '{_rank}' is not numeric for bowler index {CONTROLLER.CurrentBowlerIndex} — keeping BowlerType={CONTROLLER.BowlerType}.");
		}
	}

	public void ShowBowlingInterface(bool boolean)
	{
		if (hasGameQuit)
		{
			return;
		}
		if (boolean)
		{
			if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
			{
				bowlingControlMode = 0;
			}
			currentAction = 4;
			Singleton<Scoreboard>.instance.HideStrip(boolean: false);
			Singleton<Scoreboard>.instance.BowlerToBatsman();
			Singleton<BowlingControls>.instance.Hide(boolean: false);
			Singleton<BowlingControls>.instance.ActivateBowlingControls();
			if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
			{
				Singleton<PreviewScreen>.instance.hideBtns(boolean: true);
			}
		}
		else
		{
			currentAction = -1;
			if (CONTROLLER.currentInnings == 1)
			{
				Singleton<Scoreboard>.instance.HideStrip(boolean: false);
				Singleton<Scoreboard>.instance.TargetToWin();
			}
			else
			{
				Singleton<Scoreboard>.instance.HideStrip(boolean: true);
			}
			Singleton<BowlingControls>.instance.Hide(boolean: true);
		}
	}

	public void speedLocked()
	{
		groundController.BlockBowlerSideChange();
		Singleton<PreviewScreen>.instance.hideBtns(boolean: false);
	}

	private float AngleBetweenTwoVector3(Vector3 v31, Vector3 v32)
	{
		float num = 180f / (float)Math.PI;
		float y = v31.x - v32.x;
		float x = v31.z - v32.z;
		float num2 = Mathf.Atan2(y, x) * num;
		return (270f - num2 + 360f) % 360f;
	}

	public void GetFinalBallAngle(float angle)
	{
		currentBallAngle = angle;
		if (currentBallAngle > 360f)
		{
			currentBallAngle -= 360f;
		}
	}



	public void stopCommentarySnd()
	{
	}

	public void PlayCollectionSound(string SoundPath)
	{
		if (!hasGameQuit)
		{
		}
	}

	public void PlayGameSound(string SoundType)
	{
		if (!hasGameQuit && CONTROLLER.sndController != null)
		{
			CONTROLLER.sndController.PlayGameSnd(SoundType);
		}
	}

	protected void OnApplicationPause(bool pauseStatus)
	{
		if (CONTROLLER.PlayModeSelected == 6 && !CONTROLLER.MPInningsCompleted)
		{
			if (ManageScenes._instance.getCurrentLoadedSceneName() == "MainMenu")
			{
				InterfaceHandler.instance.ShowServerDisconnectedPopup();
			}
			else
			{
				GroundScriptHandler.Instance.ShowServerDisconnectedPopup();
			}
		}
		else if (!Singleton<TargetScreen>.instance.holder.activeSelf && Singleton<GameData>.instance.canPauseGameplay && pauseStatus && !CONTROLLER.SceneIsLoading && !resumePanel.activeSelf && !CONTROLLER.ReplayShowing && !Singleton<PauseGameScreen>.instance.BG.activeInHierarchy)
		{
			GamePaused(boolean: true);
		}
	}

	public string GetBowlerShortName(int playerID)
	{
		return CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[playerID].ScoreboardName;
	}

	private void ShowGameOverScreen()
	{
		Singleton<NavigationBack>.instance.disableDeviceBack = false;
		matchProgressBar.fillAmount = 0f;
		scoreGenerationPanel.SetActive(value: false);
		Singleton<GameOverScreen>.instance.Hide(boolean: false);
	}

	private void checkForDemoPlay()
	{
		if (CONTROLLER.noOfTrails > 0)
		{
			CONTROLLER.noOfTrails--;
			ObscuredPrefs.SetInt("NoOfTrails", CONTROLLER.noOfTrails);
		}
	}




	public void SetRVStatus(string status)
	{
		rewardFillMeter.fillAmount = 1f;
		rebowlStatus = -1;
		isBallRebowled = false;
		rvStatus = status;
		rewardPopup.SetActive(value: true);
		if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
		{
			rewardPopupText.text = LocalizationData.localizationInstance.getText(638);
		}
		else
		{
			rewardPopupText.text = LocalizationData.localizationInstance.getText(262);
		}
		if (rvStatus == "aifirstsix")
		{
		}
		Time.timeScale = 0.3f;
		TweenCallback callback = delegate
		{
			DisableRVPopUp();
		};
		skipAnimationSequence = DOTween.Sequence();
		skipAnimationSequence.InsertCallback(5f, callback);
		skipAnimationSequence.Insert(0f, rewardFillMeter.DOFillAmount(0f, 5f));
		skipAnimationSequence.SetUpdate(isIndependentUpdate: true);
		skipAnimationSequence.SetLoops(1);
	}

	public void DisableRVPopUp()
	{
		if (isBallRebowled || rebowlStatus == 2)
		{
			return;
		}
		rebowlStatus = 0;
		rewardPopup.SetActive(value: false);
		Time.timeScale = 1f;
		Singleton<Scoreboard>.instance.UpdateScoreCard();
		if (rvStatus == "firstwicket" || rvStatus == "duckout" || rvStatus == "lastwicket")
		{
			WicketBall(validatedBallIndex, bowlerId, wicketTypeCode, batsmanId, catcherId, batsmanOutCode);
			if (hasShownRv)
			{
				CurrentBallUpdate(rewardRunCount, rewardExtraCount);
				hasShownRv = false;
			}
		}
		else
		{
			CheckForOverComplete();
		}
		isBallRebowled = true;
		rebowlStatus = 2;
	}



	public void RebowlLastBall()
	{
		if (rebowlStatus > 0 /*|| CONTROLLER.PlayModeSelected == 8*/)
		{
			return;
		}
		ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + rvStatus, 1);
		Time.timeScale = 1f;
		rebowlStatus = 1;
		isBallRebowled = true;
		Singleton<BowlingScoreCard>.instance.Hide(boolean: true);
		Singleton<BattingScoreCard>.instance.Hide(boolean: true);
		Singleton<GameOverDisplay>.instance.HideMe();
		groundController.lastBowledBallType = lastBowledBallSummary;
		if (lastBowledBallSummary == "overstep")
		{
			groundController.isLineFreeHitActive = true;
		}
		else
		{
			groundController.isLineFreeHitActive = false;
		}
		if (rvStatus == "duckout" || rvStatus == "firstwicket" || rvStatus == "lastwicket")
		{
			//print("Rebowl Executed ");

			rewardPopup.SetActive(value: false);
			CONTROLLER.StrikerIndex = savedStrikerIndex;
			CONTROLLER.NonStrikerIndex = savedNonStrikerIndex;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[newBatsmanEntryIndex].BatsmanList.Status = string.Empty;
			newBatsmanEntryIndex = savedNewStrikerIndex;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].ballUpdate[currentBallNumber] = string.Empty;
			CONTROLLER.currentMatchBalls--;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchBalls--;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.BallsPlayed--;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Status = "not out";
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.Status = "not out";
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.BallsBowled--;
			currentBallNumber--;
			if (rvRunsScored > 0)
			{
				CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchScores -= rvRunsScored;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored -= rvRunsScored;
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.RunsGiven -= rvRunsScored;
			}
			scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			oversDisplayString = GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
			CurrentBallUpdate(0, 0);
			groundController.ResetAll();
			Singleton<ScoreBoardBallList>.instance.RemoveLastBall();
			Singleton<ScoreBoardBallList>.instance.SaveBallList();
			Singleton<Scoreboard>.instance.HideLastBallInfo();
			Singleton<Scoreboard>.instance.UpdateScoreCard();
			CheckForOverComplete();
		}
		else if (rvStatus == "3wides")
		{
			//print("Rebowl Executed ");

			rewardPopup.SetActive(value: false);
			if (isWicketBall)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[newBatsmanEntryIndex].BatsmanList.Status = string.Empty;
				newBatsmanEntryIndex--;
				CONTROLLER.StrikerIndex = savedStrikerIndex;
				wicketsThisOver--;
				CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchWickets--;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Status = "not out";
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.Status = "not out";
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.Wicket--;
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.WicketsInBallCount--;
			}
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchScores -= rvRunsScored;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchExtras--;
			scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			oversDisplayString = Singleton<GameData>.instance.GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
			CurrentBallUpdate(0, 0);
			groundController.ResetAll();
			Singleton<Scoreboard>.instance.HideLastBallInfo();
			Singleton<ScoreBoardBallList>.instance.RemoveLastBall();
			Singleton<ScoreBoardBallList>.instance.RemoveLastExtra();
			Singleton<ScoreBoardBallList>.instance.SaveBallList();
			Singleton<Scoreboard>.instance.UpdateScoreCard();
			CheckForOverComplete();
		}
		else if (rvStatus == "aifirstsix" || rvStatus == "lastball")
		{
			//print("Rebowl Executed ");

			CONTROLLER.StrikerIndex = savedStrikerIndex;
			CONTROLLER.NonStrikerIndex = savedNonStrikerIndex;
			if (!isBallValid)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchExtras--;
				Singleton<ScoreBoardBallList>.instance.RemoveLastExtra();
			}
			else
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[currentBallNumber] = string.Empty;
			}
			rewardPopup.SetActive(value: false);
			currentBallNumber--;
			if (rvStatus == "aifirstsix")
			{
				Singleton<BallSimulationManager>.instance.totalSetBallData--;
				Singleton<BallSimulationManager>.instance.simBallIndex--;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Sixes--;
				if (rvRunsScored > 0)
				{
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored -= rvRunsScored;
					CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.RunsGiven -= rvRunsScored;
				}
			}
			if (rvStatus == "lastball")
			{
				if (rvRunsScored == 6)
				{
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Sixes--;
				}
				else if (rvRunsScored == 4)
				{
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Fours--;
				}
				else if (rvRunsScored > 0)
				{
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored -= rvRunsScored;
					CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.RunsGiven -= rvRunsScored;
				}
			}
			if (isWicketBall)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[newBatsmanEntryIndex].BatsmanList.Status = string.Empty;
				newBatsmanEntryIndex--;
				CONTROLLER.StrikerIndex = savedStrikerIndex;
				wicketsThisOver--;
				CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchWickets--;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Status = "not out";
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.Status = "not out";
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.Wicket--;
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.WicketsInBallCount--;
			}
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores -= rvRunsScored;
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.BallsBowled--;
			CONTROLLER.currentMatchBalls--;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls--;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.BallsPlayed--;
			scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			oversDisplayString = Singleton<GameData>.instance.GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
			CurrentBallUpdate(0, 0);
			groundController.ResetAll();
			Singleton<ScoreBoardBallList>.instance.RemoveLastBall();
			Singleton<ScoreBoardBallList>.instance.SaveBallList();
			Singleton<Scoreboard>.instance.HideLastBallInfo();
			Singleton<Scoreboard>.instance.UpdateScoreCard();
			if (rvStatus != "lastball")
			{
				CheckForOverComplete();
			}
		}

		rvStatus = string.Empty;
		rebowlStatus = 2;
		Singleton<BattingScoreCard>.instance.UpdateScoreCard();
		Singleton<BowlingScoreCard>.instance.UpdateScoreCard();
	}

	public void RebowlLastBallMultiplayer()
	{

		ConstantsData_M.Log("Rebowl Happened---->");
		Time.timeScale = 1f;
		rebowlStatus = 1;
		isBallRebowled = true;
		Singleton<BowlingScoreCard>.instance.Hide(boolean: true);
		Singleton<BattingScoreCard>.instance.Hide(boolean: true);
		Singleton<GameOverDisplay>.instance.HideMe();
		groundController.lastBowledBallType = lastBowledBallSummary;
		if (lastBowledBallSummary == "overstep")
		{
			Debug.Log("FREE HITT HAI");
			groundController.isLineFreeHitActive = true;
		}
		else
		{
			Debug.Log("FREE HITT HAI NAII");
			groundController.isLineFreeHitActive = false;
		}

		//if (RVStatus == "duckout" || RVStatus == "firstwicket" || RVStatus == "lastwicket")
		wasWicket.Show("wasWicket");
		extraRuns.Show("extraRuns");
		"totalRunsScored".Show("totalRunsScored");
		if (wasWicket == 1)
		{

			if (!CONTROLLER.UNEQUALSCORE)
			{
				CONTROLLER.StrikerIndex = savedStrikerIndex;
				CONTROLLER.NonStrikerIndex = savedNonStrikerIndex;
			}
			else
			{
				CONTROLLER.NonStrikerIndex = CONTROLLER.LastNonStrikerIndex;
				CONTROLLER.StrikerIndex = CONTROLLER.LastStrikerIndex;
			}

			CONTROLLER.LastNewBatsmanIndex = savedNewStrikerIndex;



			rewardPopup.SetActive(value: false);

			Singleton<BallSimulationManager>.instance.totalSetBallData--;

			Singleton<BallSimulationManager>.instance.simBallIndex--;

			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.LastNewBatsmanIndex].BatsmanList.Status = string.Empty;
			newBatsmanEntryIndex--;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[currentBallNumber] = string.Empty;
			wicketsThisOver--;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets--;
			CONTROLLER.currentMatchBalls--;
			if (CONTROLLER.wicketType != 4)
			{
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.Wicket--;
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.WicketsInBallCount--;
			}
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls--;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.BallsPlayed--;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Status = "not out";
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.Status = "not out";
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.BallsBowled--;
			currentBallNumber--;
			if (totalRunsScored > 0)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores -= totalRunsScored;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored -= totalRunsScored;
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.RunsGiven -= totalRunsScored;
			}
			scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			oversDisplayString = Singleton<GameData>.instance.GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
			CurrentBallUpdate(0, 0);
			groundController.ResetAll();
			Singleton<ScoreBoardBallList>.instance.RemoveLastBall();
			Singleton<ScoreBoardBallList>.instance.SaveBallList();
			Singleton<Scoreboard>.instance.HideLastBallInfo();
			Singleton<Scoreboard>.instance.UpdateScoreCard();
			if (!CONTROLLER.UNEQUALSCORE)
			{
				CheckForOverComplete();
			}
			else
			{
				CallCheckOverComplete();
			}
		}
		else if (extraRuns != 0)
		{

			rewardPopup.SetActive(value: false);
			if (lastBowledBallSummary == "overstep")
			{
				groundController.isLineFreeHitActive = false;
			}

			if (totalRunsScored >= 0)
			{
				if (totalRunsScored == 1 || totalRunsScored == 3)
				{
					int i = CONTROLLER.StrikerIndex;
					CONTROLLER.StrikerIndex = savedStrikerIndex;
					CONTROLLER.NonStrikerIndex = savedNonStrikerIndex;
				}
				else
				{
					////print("Rebowl Executed ");
					CONTROLLER.StrikerIndex = savedStrikerIndex;
					CONTROLLER.NonStrikerIndex = savedNonStrikerIndex;
				}

				rewardPopup.SetActive(value: false);

				if (totalRunsScored == 6)
				{
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Sixes--;
				}
				else if (totalRunsScored == 4)
				{
					CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Fours--;
				}


				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored -= totalRunsScored;
				CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.RunsGiven -= totalRunsScored;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores -= totalRunsScored;
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.BallsPlayed--;

			}
			Singleton<BallSimulationManager>.instance.totalSetBallData--;


			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores -= extraRuns;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchExtras--;
			//currentBall--;
			scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			oversDisplayString = Singleton<GameData>.instance.GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
			CurrentBallUpdate(0, 0);
			groundController.ResetAll();
			Singleton<Scoreboard>.instance.HideLastBallInfo();
			Singleton<ScoreBoardBallList>.instance.RemoveLastBall();
			Singleton<ScoreBoardBallList>.instance.RemoveLastExtra();
			Singleton<ScoreBoardBallList>.instance.SaveBallList();
			Singleton<Scoreboard>.instance.UpdateScoreCard();
			if (!CONTROLLER.UNEQUALSCORE)
			{
				CheckForOverComplete();
			}
			else
			{
				CallCheckOverComplete();
			}
		}
		else if (totalRunsScored >= 0)
		{
			if (totalRunsScored == 1 || totalRunsScored == 3)
			{
				int i = CONTROLLER.StrikerIndex;
				CONTROLLER.StrikerIndex = savedStrikerIndex;
				CONTROLLER.NonStrikerIndex = savedNonStrikerIndex;
			}
			else
			{
				CONTROLLER.StrikerIndex = savedStrikerIndex;
				CONTROLLER.NonStrikerIndex = savedNonStrikerIndex;
			}
			if (!isBallValid)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchExtras--;
				Singleton<ScoreBoardBallList>.instance.RemoveLastExtra();
			}
			else
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[currentBallNumber] = string.Empty;
			}
			rewardPopup.SetActive(value: false);
			currentBallNumber--;
			Singleton<BallSimulationManager>.instance.totalSetBallData--;

			Singleton<BallSimulationManager>.instance.simBallIndex--;

			if (totalRunsScored == 6)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Sixes--;
			}
			else if (totalRunsScored == 4)
			{
				CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Fours--;
			}

			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored -= totalRunsScored;
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.RunsGiven -= totalRunsScored;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores -= totalRunsScored;
			CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].BowlerList.BallsBowled--;
			CONTROLLER.currentMatchBalls--;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls--;
			CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.BallsPlayed--;
			scoreDisplayString = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
			oversDisplayString = Singleton<GameData>.instance.GetOverStr() + "(" + CONTROLLER.totalOvers + ")";
			CurrentBallUpdate(0, 0);
			groundController.ResetAll();
			Singleton<ScoreBoardBallList>.instance.RemoveLastBall();
			Singleton<ScoreBoardBallList>.instance.SaveBallList();
			Singleton<Scoreboard>.instance.HideLastBallInfo();
			Singleton<Scoreboard>.instance.UpdateScoreCard();
			if (rvStatus != "lastball")
			{
			}

			if (!CONTROLLER.UNEQUALSCORE)
			{
				CheckForOverComplete();
			}
			else
			{
				CallCheckOverComplete();
			}
		}
		rvStatus = string.Empty;
		rebowlStatus = 2;
		Singleton<BattingScoreCard>.instance.UpdateScoreCard();
		Singleton<BowlingScoreCard>.instance.UpdateScoreCard();
	}

	private void CheckIfAchievementMet()
	{
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			int strikeRate = GetStrikeRate(CONTROLLER.StrikerIndex);
			int strikeRate2 = GetStrikeRate(CONTROLLER.NonStrikerIndex);
			if (CONTROLLER.RunRate < 13f)
			{
				CanShowPopupAgain(11);
			}
			if (strikeRate < 175 || strikeRate2 < 175)
			{
				CanShowPopupAgain(12);
			}
			if (CONTROLLER.continousBoundaries < 2)
			{
				CanShowPopupAgain(3);
			}
			if (CONTROLLER.continousSixes < 2)
			{
				CanShowPopupAgain(4);
			}
			if (CONTROLLER.continousBoundaries < 5)
			{
				CanShowPopupAgain(7);
			}
			if (CONTROLLER.continousSixes < 5)
			{
				CanShowPopupAgain(8);
			}
		}
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			if (CONTROLLER.continousWickets < 2)
			{
				CanShowPopupAgain(5);
			}
			if (CONTROLLER.continousWickets < 5)
			{
				CanShowPopupAgain(6);
			}
		}
	}

	public void CallCheckForOverComplete()
	{
		CheckForOverComplete();
	}

	IEnumerator ClosePanel()
	{
		waitingForOpponentText.text = "Internet is disconnected, so this ball will be reballed";
		if (!waitingForOpponentPanel.activeInHierarchy)
		{
			waitingForOpponentPanel.SetActive(true);
		}
		yield return new WaitForSeconds(5f);
		waitingForOpponentPanel.SetActive(false);
	}

	// MP reconnect "scorecard pops up again and again" fix: a flapping/leftover reconnect
	// re-fires OnReconnection (locally via ApplyReconnectState AND remotely via
	// RpcNotifyOtherPlayerIAmReconnected), re-showing the scorecard repeatedly. Coalesce
	// duplicate gameplay-resume side-effects within a short window. The waiting-UI-active
	// case always proceeds (a genuine resume is needed there).
	private const float OnlineReconnectDebounceSeconds = 5f;
	private static float s_lastOnlineReconnectHandledAt = -999f;

	public void OnReconnection(bool authoritative = false)
	{
		// MP disconnect fix: once the match result is decided (incl. a disconnect-win when the
		// opponent leaves for good), the reconnect handshake must NOT re-open gameplay scorecards.
		// A flapping/leftover reconnect kept re-firing this path → the remaining player saw the
		// scorecard pop up continuously after the opponent disconnected.
		if (ResultManager.isGameFinished) return;

		// Online MP only: suppress duplicate reconnect side-effects from a flapping connection
		// so the scorecard can't re-show "again and again". BUT the LOCAL authoritative reconnect
		// restore (ApplyReconnectState, authoritative==true) must ALWAYS run — only the remote
		// opponent-notify path is debounced. Otherwise the real post-restore resume gets
		// suppressed (a premature opponent-notify call already stamped the timer) and the game
		// sticks on the scorecard. Stamp is updated either way so a following remote notify
		// within the window doesn't replay side-effects.
		if (CONTROLLER.PlayModeSelected == 8)
		{
			float now = Time.realtimeSinceStartup;
			if (!authoritative)
			{
				bool waitingUiActive =
					(waitingForOpponentPanel != null && waitingForOpponentPanel.activeInHierarchy) ||
					(waitingForOpponentPanel1 != null && waitingForOpponentPanel1.activeInHierarchy);
				if (!waitingUiActive && now - s_lastOnlineReconnectHandledAt < OnlineReconnectDebounceSeconds)
				{
					Debug.Log("[GameData][OnReconnection] Duplicate reconnect side-effects suppressed (debounce).");
					return;
				}
			}
			s_lastOnlineReconnectHandledAt = now;
		}

		if (groundController != null)
		{
			groundController.ReBroadcastBowlingSpotForOpponentReconnect();
		}

		// Opponent reconnected: hide waiting panel and any open scorecards on the persistent player side
		waitingForOpponentPanel1.SetActive(false);
		Singleton<BattingScoreCard>.instance?.Hide(true);
		Singleton<BowlingScoreCard>.instance?.Hide(true);
		// Bottom-strip fix (tester 14-07: staying side's bottom score line vanished after the opponent's
		// reconnect; after two reconnects BOTH sides lost it): a scorecard open at disconnect time had run
		// Scoreboard.Hide(true) (HUD hidden); force-hiding the scorecards above SKIPS their Continue flow —
		// the only place Scoreboard.Hide(false) normally runs — so the HUD (incl. StripBG) stayed hidden
		// forever. Restore it here; the over flow re-hides it if a scorecard legitimately opens next.
		if (Singleton<Scoreboard>.instance != null)
		{
			Singleton<Scoreboard>.instance.Hide(boolean: false);
			Singleton<Scoreboard>.instance.HideStrip(boolean: false);
			ConstantsData_M.MpLog("[GameData][OnReconnection] Gameplay HUD restored (scoreboard + bottom strip).");
		}
		if (waitingForOpponentPanel.activeInHierarchy)
		{
			CheckForOverComplete();
			// Reconnect over-ACK barrier oscillation fix (MP): the over-ACK handshake (CONTROLLER.OnWaitScreen
			// 0->1->2) must only be RE-SEEDED on a genuine panel-active over-wait resume. On a Case-None mid-over
			// reconnect (waitingForOpponentPanel INACTIVE) BOTH clients ran OnReconnection and the previously-
			// unconditional CallOppAck below seeded a PHANTOM ACK pair; with IsReConnecting() already false, the
			// ==2 resolve fired CmdCallCheckForOverComplete -> CheckForOverComplete with the panel INACTIVE, which
			// re-took the CallOppAck-and-return branch and re-opened the barrier every ~0.7s forever (bowler never
			// bowled). Moving the seed INSIDE this guard stops the phantom pair; the next-ball flow re-arms the
			// barrier normally, and CancelInFlightDeliveryForReconnect already zeroed it — no softlock, no double-advance.
			Singleton<GroundController>.instance.CallOppAck();
		}
		waitingForOpponentText.text = "Waiting for Opponent...";
		//waitingForOpponentPanel.SetActive(true);

		// RECONNECT RESUME-TO-LIVE-PLAY FIX: when the disconnect happened DURING gameplay (currentOpenPanel ==
		// None), the fresh scene reload re-runs the over-START scorecard sequence and STRANDS a BATTING
		// reconnecter on the bowling scorecard (its Continue() waits for the bowler's one-shot continue RPC,
		// already consumed by the staying player -> never fires -> stuck). Poll for the re-shown bowling scorecard
		// and force-resume live play. Gated to a genuine gameplay reconnect (currentOpenPanel==None) + batting side.
		if (authoritative && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
		{
			Debug.Log($"[GameData][OnReconnection] resume-eval: panel={(CricketNetworkManager.instance != null ? CricketNetworkManager.instance.currentOpenPanel.ToString() : "<noCNM>")} battingMe={(CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)} finished={ResultManager.isGameFinished} running={_reconnectResumeRunning}");
			if (!ResultManager.isGameFinished && CricketNetworkManager.instance != null
				&& CricketNetworkManager.instance.currentOpenPanel == PanelsInfo.None)
			{
				if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					// First-post-reconnect-ball presentation: the re-bowled ball arrives via RpcAutomaticBall directly,
					// skipping the normal setup, so the batsman model + bowler aim preview (spin/power) were missing on
					// the batting reconnecter's first ball. Arm them here (the 2nd ball self-corrects via BowlNextBall).
					Singleton<GroundController>.instance?.ArmBattingPreviewAfterReconnect();
					if (!_reconnectResumeRunning)
					{
						_reconnectResumeRunning = true;
						StartCoroutine(ForceResumeLiveBowlingAfterReconnect());
					}
				}
				else
				{
					// BOWLER reconnecter (#2a): its scene reloaded so the bottom score strip + full HUD start
					// INACTIVE, and the bowler's direct re-bowl path never re-activates them → the bottom score
					// panel was missing after a bowler disconnect (tester: "bottom score panel not appear if bowler
					// disconnect while standing"). The bowler/keeper/fielders are already re-homed by
					// CancelInFlightDeliveryForReconnect's ResetAll, so we ONLY re-show the HUD here — deliberately
					// NOT a full ResetAll, which could interrupt the bowler's already-armed auto-bowl timer.
					Singleton<GroundController>.instance?.ShowReconnectHudForBowler();
				}
			}
		}
	}

	private bool _reconnectResumeRunning = false;
	private IEnumerator ForceResumeLiveBowlingAfterReconnect()
	{
		Debug.Log("[GameData][OnReconnection] resume coroutine STARTED (batting gameplay reconnect) — polling up to 25s for a re-shown over-start scorecard.");
		float elapsed = 0f;
		while (elapsed < 25f)
		{
			if (ResultManager.isGameFinished) { _reconnectResumeRunning = false; yield break; }
			BowlingScoreCard bsc = Singleton<BowlingScoreCard>.instance;
			BattingScoreCard batsc = Singleton<BattingScoreCard>.instance;
			bool bswUp = bsc != null && bsc.scoreCard != null && bsc.scoreCard.activeInHierarchy;
			bool batUp = batsc != null && batsc.scoreCard != null && batsc.scoreCard.activeInHierarchy;
			if (bswUp || batUp)
			{
				Debug.Log($"[GameData][OnReconnection] FOUND over-start scorecard (bowling={bswUp} batting={batUp}) at {elapsed:F1}s — force-resuming live play.");
				CONTROLLER.CanLoadGround = true;
				if (batUp && !bswUp)
				{
					ShowBowlingScoreCard();
					yield return null;
					bsc = Singleton<BowlingScoreCard>.instance;
				}
				batsc?.Hide(boolean: true);
				bsc?.Continue();
				_reconnectResumeRunning = false;
				yield break;
			}
			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}
		Debug.Log("[GameData][OnReconnection] resume coroutine TIMED OUT (25s) — no over-start scorecard ever became active; the stuck state is NOT the bowling scorecard.");
		_reconnectResumeRunning = false;
	}
	public void CheckForOverComplete()
	{
		"thushk".Show();
		GC.Collect();
		Resources.UnloadUnusedAssets();
		if (!waitingForOpponentPanel.activeInHierarchy && CONTROLLER.PlayModeSelected == 8 && !Launcher.Instance.IsReConnecting())
		{

			//waitingForOpponentText.text = "Waiting for Opponent...";
			//waitingForOpponentPanel.SetActive(true);
			Singleton<GroundController>.instance.CallOppAck();
			return;
		}
		if (CONTROLLER.PlayModeSelected == 8)
		{
			if (false)
			{
				StartCoroutine(ClosePanel());
			}
			else
			{
				waitingForOpponentPanel.SetActive(false);
			}
			Singleton<GroundController>.instance.hasTakenRun = false;
			Singleton<GroundController>.instance.hasCancelledRun = false;
			CONTROLLER.OnWaitScreen = 0;

		}
		if (waitingForOpponentPanel1.activeInHierarchy && currentBallNumber == 5)
		{
			return;
		}
		if (rebowlStatus == -1)
		{
			return;
		}
		rebowlStatus = 2;

		isInningsComplete = CheckForInningsComplete();

		if (isInningsComplete)
		{
			CONTROLLER.CanLoadBowlerScoreBoard = false;
			float num = 0.7f * (float)CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
			canPauseGameplay = false;
			groundController.isFieldRestrictionActive = true;
			groundController.resetNoBallVairables();
			CONTROLLER.fielderChangeIndex = 1;
			CONTROLLER.computerFielderChangeIndex = 0;
			Singleton<Scoreboard>.instance.Hide(boolean: true);
			Singleton<PreviewScreen>.instance.Hide(boolean: true);
			if (CONTROLLER.PlayModeSelected != 6)
			{
				CONTROLLER.InningsCompleted = true;
				CONTROLLER.CanLoadGround = false;
			}
			Singleton<BowlingScoreCard>.instance.InningsCompleted();
			if (CONTROLLER.currentInnings == 0)
			{

				if (CONTROLLER.PlayModeSelected != 6)
				{
					ShowBattingScoreCard();
				}
			}
			else
			{
				checkForDemoPlay();
				Singleton<GameOverScreen>.instance.Hide(boolean: false);
			}
		}
		else if (currentBallNumber == 5)
		{
			if (CONTROLLER.PlayModeSelected != 6)
			{
				if (waitingForOpponentPanel1.activeInHierarchy)
				{
					BowlNextBall();
					return;
				}
				// Wicket-on-last-ball-of-over deadlock fix: when a wicket falls on the last ball of an
				// over, the ACK-resolved CheckForOverComplete reached here and ran NewOver() directly —
				// opening the over-end scorecard WITHOUT ever bringing in the new batsman, so the next
				// over had no striker and the game froze. Run the new-batsman walk-in FIRST; its tail
				// (BatsmanEntryView) calls CheckForOverComplete again, which re-enters this branch with
				// wicketBatsmanFlowStarted==true and then proceeds to NewOver normally.
				if (isWicketBall && !wicketBatsmanFlowStarted)
				{
					ConstantsData_M.MpLog("[OverEndWicket] Over ended on a wicket — running new-batsman walk-in before NewOver.");
					InitBatsmanInfo();
					return;
				}
				KitTable.SetKitValues(1);
				oversPlayedCounter++;
				ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterPerOversPlayed", oversPlayedCounter);
				groundController.showPreviewCamera(status: false);
				NewOver();
			}
		}
		else if (CONTROLLER.PlayModeSelected != 6)
		{
			// After reconnect in between-overs state currentBallNumber is -1 (NewOver
			// was already called before disconnect). Re-trigger the batting scorecard
			// flow so both players see the scorecards and the bowling player can pick
			// a new bowler, instead of jumping straight to BowlNextBall.
			// BUT currentBallNumber == -1 does NOT mean "between overs" on its own: it also holds when the
			// over's FIRST delivery was a wide or a no-ball, because an extra does not advance the legal ball
			// count. That re-opened the over-end scorecards mid-over and let the bowler be changed after a
			// single illegal delivery — a bowler swap the other client never makes, i.e. a straight desync
			// (tester 12-08: "first ball of over wide/no ball -> scorecards appear again -> bowler change").
			// ScoreBoardBallList.ballCount separates the two: Scoreboard.NewOver() -> ResetBallList() zeroes it
			// at every over start, and AddBall() increments it for EVERY delivery INCLUDING extras. So between
			// overs it is 0, while after a first-ball extra it is already >= 1 — only the former is the
			// reconnect case this branch exists for. Null-guarded: a missing singleton keeps the old behaviour.
			int _deliveriesThisOver = (Singleton<ScoreBoardBallList>.instance != null)
				? Singleton<ScoreBoardBallList>.instance.ballCount : 0;
			if (CONTROLLER.PlayModeSelected == 8 && currentBallNumber < 0 && _deliveriesThisOver > 0)
			{
				ConstantsData_M.MpLog($"[GameData] Over's first delivery was an extra ({_deliveriesThisOver} bowled, currentBallNumber={currentBallNumber}) — continuing the over instead of re-opening the over-end scorecards.");
				BowlNextBall();
			}
			else if (CONTROLLER.PlayModeSelected == 8 && currentBallNumber < 0)
			{
				ShowBattingScoreCard();
			}
			else
			{
				BowlNextBall();
			}
		}
	}

	public void CallCheckOverComplete()
	{

		Singleton<GroundController>.instance.hasTakenRun = false;
		Singleton<GroundController>.instance.hasCancelledRun = false;
		if (isBallRebowled)
		{
			StartCoroutine(ClosePanel());
		}
		else
		{
			waitingForOpponentPanel.SetActive(false);
		}
		if (rebowlStatus == -1)
		{
			return;
		}
		rebowlStatus = 2;

		isInningsComplete = CheckForInningsComplete();
		if (isInningsComplete)
		{
			////Debug.Log("INNINGSDONNEEE");
			float num = 0.7f * (float)CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
			canPauseGameplay = false;
			groundController.isFieldRestrictionActive = true;
			groundController.resetNoBallVairables();
			CONTROLLER.fielderChangeIndex = 1;
			CONTROLLER.computerFielderChangeIndex = 0;
			Singleton<Scoreboard>.instance.Hide(boolean: true);
			Singleton<PreviewScreen>.instance.Hide(boolean: true);
			if (CONTROLLER.PlayModeSelected != 6)
			{
				CONTROLLER.InningsCompleted = true;
				CONTROLLER.CanLoadGround = false;
			}
			Singleton<BowlingScoreCard>.instance.InningsCompleted();
			if (CONTROLLER.currentInnings == 0)
			{
				if (CONTROLLER.PlayModeSelected != 6)
				{
					ShowBattingScoreCard();
				}
			}
			else
			{
				checkForDemoPlay();
				Singleton<GameOverScreen>.instance.Hide(boolean: false);
			}
		}
		else if (currentBallNumber == 5)
		{
			if (CONTROLLER.PlayModeSelected != 6)
			{

				KitTable.SetKitValues(1);
				oversPlayedCounter++;
				ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "CounterPerOversPlayed", oversPlayedCounter);
				groundController.showPreviewCamera(status: false);
				NewOver();
			}
		}
		else if (CONTROLLER.PlayModeSelected != 6)
		{
			BowlNextBall();
		}
		CONTROLLER.UNEQUALSCORE = false;
	}

	private void SpaceTextValue()
	{
		skipButtonStatus = string.Empty;
	}


	private void CanShowPopupAgain(int index)
	{
	}

	public void DeleteAchievementFlags()
	{
	}





	// TEST-ONLY shortcut (gated by ConstantsData_M.MpVerboseLogs — the dev flag, false in release): force the
	// current batting innings to END NOW with the given score, so the innings-shift is reached in seconds instead
	// of bowling 3 full overs every test. Bound to SPACE in GroundController.Update(). Press SPACE on BOTH windows
	// so both sides end together (it acts locally on each — no networking added).
	public void DebugForceInningsEnd(int score = 20)
	{
		if (!ConstantsData_M.MpVerboseLogs) return;   // never in release (dev flag off)
		if (isInningsComplete) return;                 // already ending
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores = score;
		isInningsComplete = true;
		ConstantsData_M.MpLog($"[TEST] DebugForceInningsEnd — batting score set to {score}, forcing innings end (SPACE).");
		InningsCompleteFn();
	}

	// Target-divergence repair (tester: "2nd innings ka target ik side 1, opponent side 7"): after a
	// reconnect storm one client's local TeamList total can be zeroed, collapsing its locally-derived
	// TargetToChase to 1 while the other side computes the real total. The CmdSyncMatchProgress SyncVars
	// still hold the authoritative innings-1 totals — when the local derivation collapses, repair from them
	// (at a 2nd-innings start the first-innings total is the larger of the two synced team totals).
	public void RepairCollapsedTarget()
	{
		if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
		if (CONTROLLER.TargetToChase > 1 || CricketNetworkManager.instance == null) return;
		int _authTotal = Mathf.Max(CricketNetworkManager.instance.syncedTeam0Scores,
		                           CricketNetworkManager.instance.syncedTeam1Scores);
		if (_authTotal > 0)
		{
			CONTROLLER.TargetToChase = _authTotal + 1;
			ConstantsData_M.MpLog($"[TargetRepair] Local target collapsed to <=1 — repaired to {CONTROLLER.TargetToChase} from the synced innings totals.");
		}
	}

	public bool CheckForInningsComplete()
	{
		// False Win/Lose-on-reconnect guard (#9): never declare the innings/match complete while the
		// authoritative reconnect restore is still in flight (scores/indices/TargetToChase not yet
		// applied), or on an obviously un-restored 2nd-innings state. Otherwise the ACK-barrier
		// CheckForOverComplete path evaluates against momentarily-reset state and spawns a bogus result.
		if (CONTROLLER.PlayModeSelected == 8)
		{
			if (CricketNetworkManager.RestorePending
				|| (Launcher.Instance != null && Launcher.Instance.IsReConnecting()))
				return false;
			// Solo innings-advance guard (tester: opponent ke disconnect ke doran staying side pe scorecard
			// khul ke game stuck): never evaluate over/innings completion while the OPPONENT IS ABSENT — a
			// half-committed interrupted ball can momentarily satisfy the completion math, and a solo
			// scorecard/innings transition can never handshake. The reconnect-notify flow owns recovery
			// (same liveness hold OppAckWatchdog and TickDeliveryWatchdog already use).
			if (NetworkGameManager.Instance == null || NetworkGameManager.Instance.IsPaused
				|| NetworkGameManager.Instance.currentPlayerCount < 2)
				return false;
			if (CONTROLLER.currentInnings == 1 && CONTROLLER.TargetToChase <= 1)
				return false;
			// Issue #5 (disconnect→team-swap→instant false win): a valid chase target is always the
			// first-innings team's total + 1, so TargetToChase MUST exceed the bowling team's recorded
			// score. If indices got swapped on reconnect the target collapses below that — refuse to
			// declare a result. Never triggers in normal play (target == bowlingTeamScore + 1 > score).
			if (CONTROLLER.currentInnings == 1
				&& CONTROLLER.BowlingTeamIndex >= 0 && CONTROLLER.BowlingTeamIndex < CONTROLLER.TeamList.Length
				&& CONTROLLER.TargetToChase <= CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchScores)
				return false;
		}
		int currentMatchScores = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
		if (CONTROLLER.PlayModeSelected == 5)
		{

		}
		else
		{
			if (CONTROLLER.currentInnings == 1 && currentMatchScores >= CONTROLLER.TargetToChase)
			{
				return true;
			}

			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls >= CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6)
			{
				return true;
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets >= CONTROLLER.totalWickets)
			{
				return true;
			}
		}
		return false;
	}

	private void InningsCompleteFn()
	{
		groundController.isFieldRestrictionActive = true;
		// The last ball of an innings has no following delivery, so ResetAll's per-delivery FOV reset never
		// runs for it and the replay's zoom stands through the innings break (tester #7).
		groundController.ResetSideCameraFraming();
		CONTROLLER.fielderChangeIndex = 1;
		CONTROLLER.computerFielderChangeIndex = 0;
		Singleton<Scoreboard>.instance.Hide(boolean: true);
		Singleton<PreviewScreen>.instance.Hide(boolean: true);
		Singleton<BowlingScoreCard>.instance.InningsCompleted();
		if (CONTROLLER.PlayModeSelected != 7)
		{
			if (CONTROLLER.currentInnings == 0)
			{
				Singleton<TargetScreen>.instance.Hide(boolean: false);
			}
			else
			{
				Singleton<GameOverScreen>.instance.Hide(boolean: false);
			}
		}
		else
		{
			ShowBattingScoreCard();
		}
	}

	public void NewOver()
	{
		canPauseGameplay = false;
		fourScoredInOver = false;
		sixScoredInOver = false;
		hasReachedMaxRuns = false;
		if (CONTROLLER.PlayModeSelected != 8)
		{
			if (!CONTROLLER.PowerPlay && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
			{
				CONTROLLER.computerFielderChangeIndex = UnityEngine.Random.Range(6, 11);
			}
			else if (CONTROLLER.PowerPlay && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
			{
				CONTROLLER.computerFielderChangeIndex = UnityEngine.Random.Range(1, 6);
			}
		}
		currentBallNumber = -1;
		runsInCurrentOver = 0;
		Singleton<Scoreboard>.instance.NewOver();
		Singleton<Scoreboard>.instance.HidePause(boolean: true);
		Singleton<Scoreboard>.instance.Hide(boolean: true);
		Singleton<PreviewScreen>.instance.Hide(boolean: true);
		Singleton<BowlingControls>.instance.Hide(boolean: true);
		Singleton<BattingControls>.instance.Hide(boolean: true);
		Singleton<BattingScoreCard>.instance.SCTeamName.DOFade(1f, 0f);
		if (CONTROLLER.PlayModeSelected == 6)
		{
			//Singleton<BowlingScoreCard>.instance.Continue();
		}
		else
		{
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls % CONTROLLER.greedyRefreshRate == 0)
			{
				//	SingletoneBase<GreedyCampaignLoader>.Instance.refreshGreedyAds();
			}
			//if (CONTROLLER.PlayModeSelected == 5)
			//{
			//	Singleton<Scoreboard>.instance.CTTargetToWin();
			//}
			ShowBattingScoreCard();
		}
		if (!CONTROLLER.isFromAutoPlay)
		{
			Singleton<BowlingScoreCard>.instance.OverCompleted();
		}

		groundController.NewOver();
		Singleton<BattingControls>.instance.LoftMeterFill(CONTROLLER.StrikerIndex);

		// OVER-BOUNDARY snapshot push ("pichle over ki balls bhi fetch kar li" / "8-8 balls history").
		// The post-ball push at the end of CurrentBallUpdate correctly captures the completed over's
		// 6-chip strip — but the strip is CLEARED here, later, by Scoreboard.NewOver(), and nothing
		// re-pushed. So the server kept holding the OLD over's full strip: a client reconnecting
		// between overs restored those 6 chips and then appended the new over's deliveries on top,
		// showing 7-8 balls in the history. Push again now that the strip is empty and
		// currentBallNumber is reset, so the held snapshot matches the fresh over. Batting authority
		// only and never mid-reconnect — same gate as the other pushes.
		if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
			&& CricketNetworkManager.instance != null
			&& CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
			&& (Launcher.Instance == null || !Launcher.Instance.IsReConnecting()))
		{
			CricketNetworkManager.instance.PushMatchSnapshotIfBatting();
		}
	}

	public void ShowBowlingScoreCard()
	{
		if (!isGamePaused)
		{
			currentAction = 1;
		}
		Singleton<BowlingScoreCard>.instance.Hide(boolean: false);
	}

	public void UpdatePreview(Dictionary<string, object> hash)
	{
		Singleton<PreviewScreen>.instance.UpdatePreviewScreen(hash);
	}

	public void SendBowlingDatasToGame()
	{
		bowlingControlMode = 2;
		groundController.StartBowling();
	}

	public void InitAnimation(int type)
	{
		ConstantsData_M.MpLog($"[BoundaryBanner] InitAnimation type={type} (0=FOUR 1=SIX 2/3=wicket-ish 5=skip)");
		canPauseGameplay = false;
		Singleton<Scoreboard>.instance.HidePause(boolean: true);
		if (CONTROLLER.stumpingAttempted || CONTROLLER.runoutThirdUmpireAppeal)
		{
			replayFinishedTriggered = true;
			ReplayIsNotShown();
		}
		else if (CONTROLLER.PlayModeSelected == 8 && canPlayAnimation)
		{
			Singleton<AnimationScreen>.instance.StartAnimation(type);
		}
		else if (CONTROLLER.PlayModeSelected == 6 || type == 5 || !canPlayAnimation)
		{
			if (Singleton<GroundController>.instance.getOverStepBall() && (type == 0 || type == 1))
			{
				StartCoroutine(Singleton<GroundController>.instance.updateBoundaryBall());
			}
			else
			{
				AnimationCompleted();
			}
		}
		else if (canPlayAnimation)
		{
			Singleton<AnimationScreen>.instance.StartAnimation(type);
		}
		gameState = type;
	}

	public void setReplayCompletedVariable()
	{
		replayFinishedTriggered = true;
	}

	public void AnimationCompleted()
	{
		Singleton<UILookAt>.instance.show(flag: false);
		groundController.HideReplay();
		if (canPlayAnimation)
		{
			replayFinishedTriggered = true;
			if (gameState == 5 && CONTROLLER.canShowReplay)
			{
				SkipReplay();
				gameState = -1;
			}
			else if (CONTROLLER.canShowReplay)
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

	public void ReplayIsNotShown()
	{
		skipButtonStatus = "replay";
		CONTROLLER.ReplayShowing = true;
		SkipReplay();
		////Debug.Log("##SKIP BY REPLAY NOT SHOWN");
	}

	public void GameIsOnReplay()
	{
		canPauseGameplay = false;
		CONTROLLER.ReplayShowing = true;
		Singleton<Scoreboard>.instance.Hide(boolean: true);
		Singleton<PreviewScreen>.instance.Hide(boolean: true);
		Singleton<BowlingControls>.instance.Hide(boolean: true);
		Singleton<BattingControls>.instance.Hide(boolean: true);
		Singleton<PauseGameScreen>.instance.Hide(boolean: true);
		if (skipButton != null)
		{
			actionStatusText.text = LocalizationData.localizationInstance.getText(519);
			actionStatusText.color = new Color(1f, 1f, 1f, 1f);
			actionStatusText.gameObject.SetActive(value: true);
			skipButton.gameObject.SetActive(value: true);
			skipButtonStatus = "replay";
			timeoutBlinkDuration = Time.time;
			replayStartDelay = Time.time;
			replayBlinkStatus = 0;
		}
	}

	public void GameIsOnStumpingReplay()
	{
		CONTROLLER.ReplayShowing = true;
		Singleton<PreviewScreen>.instance.Hide(boolean: true);
		canPauseGameplay = false;
		Singleton<Scoreboard>.instance.Hide(boolean: true);
		if (skipButton != null)
		{
			actionStatusText.text = LocalizationData.localizationInstance.getText(518);
			actionStatusText.color = new Color(1f, 1f, 1f, 1f);
			actionStatusText.gameObject.SetActive(value: true);
			skipButton.gameObject.SetActive(value: true);
			skipButtonStatus = "stumpingReplay";
		}
	}

	public void GameIsNotOnStumpingReplay()
	{
		if (skipButton != null)
		{
			actionStatusText.gameObject.SetActive(value: false);
			skipButton.gameObject.SetActive(value: false);
			skipButtonStatus = string.Empty;
			Singleton<Scoreboard>.instance.Hide(boolean: true);
			Singleton<PreviewScreen>.instance.Hide(boolean: true);
			Singleton<BowlingControls>.instance.Hide(boolean: true);
			Singleton<BattingControls>.instance.Hide(boolean: true);
			Singleton<PauseGameScreen>.instance.Hide(boolean: true);
		}
	}

	public void GameIsOnThirdUmpireRunoutReplay()
	{
		canPauseGameplay = false;
		CONTROLLER.ReplayShowing = true;
		Singleton<Scoreboard>.instance.Hide(boolean: true);
		Singleton<PreviewScreen>.instance.Hide(boolean: true);
		Singleton<BowlingControls>.instance.Hide(boolean: true);
		Singleton<BattingControls>.instance.Hide(boolean: true);
		Singleton<PauseGameScreen>.instance.Hide(boolean: true);
		if (skipButton != null)
		{
			actionStatusText.text = LocalizationData.localizationInstance.getText(518);
			actionStatusText.color = new Color(1f, 1f, 1f, 1f);
			actionStatusText.gameObject.SetActive(value: true);
			skipButton.gameObject.SetActive(value: true);
			skipButtonStatus = "thirdUmpireRunoutReplay";
		}
	}

	public void SkipReplay()
	{
		if (skipButtonStatus == "replay")
		{
			groundController.SkipReplay();
			////Debug.Log("REPLAYY SHOWMNNNN !@#$");

		}
		else if (skipButtonStatus == "stumpingReplay")
		{
			groundController.SkipStumpingReplay();
		}
		else if (skipButtonStatus == "thirdUmpireRunoutReplay")
		{
			groundController.SkipThirdUmpireRunoutReplay();
		}
	}

	public void ReplayCompleted()
	{
		if (!CONTROLLER.ReplayShowing)
		{
			return;
		}
		if (skipButton != null)
		{
			actionStatusText.gameObject.SetActive(value: false);
			skipButton.gameObject.SetActive(value: false);
			skipButtonStatus = string.Empty;
		}
		CONTROLLER.ReplayShowing = false;
		if (CONTROLLER.PlayModeSelected == 6)
		{
			//ScoreBoardMultiPlayer.instance.Hide(boolean: false);
		}
		else
		{
			Singleton<Scoreboard>.instance.Hide(boolean: false);
		}
		Singleton<PreviewScreen>.instance.Hide(boolean: false);
		if (isWicketBall)
		{
			if (CONTROLLER.PlayModeSelected == 6)
			{
				//batsmanExitStopped();
			}
			else
			{
				InitBatsmanInfo();
			}
		}
		else if (replayFinishedTriggered && !CONTROLLER.isJokerCall && !CONTROLLER.isFreeHit)
		{
			CheckForOverComplete();
		}
		replayFinishedTriggered = false;
	}

	public void GamePaused(bool boolean)
	{
		if (CONTROLLER.PlayModeSelected == 8)
		{
			return;
		}
		if (boolean)
		{
			canResumeGameplay = true;
			canPauseGameplay = false;
			Singleton<BatsmanRecord>.instance.Hide(boolean: true);
			Singleton<BoundaryAnimation2>.instance.HideMe();
			Singleton<BatsmanInfo>.instance.Hide(boolean: true);
			Singleton<Scoreboard>.instance.Hide(boolean: true);
			Singleton<PreviewScreen>.instance.Hide(boolean: true);
			Singleton<BowlingControls>.instance.Hide(boolean: true);
			Singleton<BattingControls>.instance.Hide(boolean: true);
			if (!Singleton<PauseGameScreen>.instance.midPageGO.activeSelf)
			{
				Singleton<PauseGameScreen>.instance.Hide(boolean: false);
			}
			if (CONTROLLER.currentInnings == 1)
			{
				Singleton<PauseGameScreen>.instance.NeedText1.gameObject.SetActive(value: true);
				Singleton<PauseGameScreen>.instance.NeedText2.gameObject.SetActive(value: true);
			}
			else
			{
				Singleton<PauseGameScreen>.instance.NeedText1.gameObject.SetActive(value: false);
				Singleton<PauseGameScreen>.instance.NeedText2.gameObject.SetActive(value: false);
			}
			Singleton<Scoreboard>.instance.HidePause(boolean: true);
		}
		isGamePaused = boolean;
		groundController.GameIsPaused(boolean);
		if (!boolean && !hasGameQuit)
		{
			canPauseGameplay = true;
			Singleton<BoundaryAnimation2>.instance.HideMe();
			if (currentAction == 2)
			{
				Singleton<BatsmanRecord>.instance.Hide(boolean: false);
				skipButton.gameObject.SetActive(value: true);
			}
			if (currentAction == 3)
			{
				Singleton<BatsmanInfo>.instance.Hide(boolean: false);
				skipButton.gameObject.SetActive(value: true);
			}
			if (currentAction == 4)
			{
				Singleton<BowlingControls>.instance.Hide(boolean: false);
				Singleton<Scoreboard>.instance.Hide(boolean: false);
				Singleton<PreviewScreen>.instance.Hide(boolean: false);
			}
			else if (currentAction == -1 || currentAction == 6)
			{
				Singleton<Scoreboard>.instance.Hide(boolean: false);
				Singleton<PreviewScreen>.instance.Hide(boolean: false);
			}
			else if (currentAction == 5)
			{
				Singleton<PreviewScreen>.instance.Hide(boolean: false);
				Singleton<BowlingControls>.instance.Hide(boolean: false);
				Singleton<BattingControls>.instance.Hide(boolean: false);
				Singleton<Scoreboard>.instance.Hide(boolean: false);
			}
		}
	}

	private bool IsPointerOverUIObject()
	{
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, list);
		return list.Count > 0;
	}

	public void HideUIMenu(bool hide)
	{
		if (hide)
		{
			cachedTransform.localPosition = new Vector3(cachedTransform.localPosition.x, cachedTransform.localPosition.y, -10f);
		}
		else
		{
			cachedTransform.localPosition = new Vector3(cachedTransform.localPosition.x, cachedTransform.localPosition.y, 1f);
		}
	}

	public void GamePauseMenuSelected()
	{
		canResumeGameplay = false;
	}

	public void AgainToGamePauseScreen()
	{
		canResumeGameplay = true;
	}

	private bool CheckForAllOut()
	{
		bool result = false;
		if (newBatsmanEntryIndex > CONTROLLER.totalWickets)
		{
			result = true;
		}
		return result;
	}

	public Vector3 GetScreenPoint(Vector3 pos)
	{
		return matchRenderCamera.WorldToScreenPoint(pos);
	}

	private void GetKeyBoardInput()
	{
		if (Input.GetKeyDown(KeyCode.A) && !isGamePaused)
		{
			if (currentAction == 4 && bowlingControlMode == 0)
			{
				Singleton<BowlingControls>.instance.ChangeSwingParameter();
			}
		}
		else if ((Input.GetKeyDown(KeyCode.S) || Input.GetMouseButtonDown(0)) && !isGamePaused && !IsPointerOverUIObject())
		{
			if (Singleton<FreeHitValidation>.instance.holder.activeSelf)
			{
				return;
			}
			if (CONTROLLER.ReplayShowing)
			{
				////Debug.Log("##SKIP BY TOUCH");
				SkipReplay();
			}
			else if (skipButtonStatus == "stumpingReplay")
			{
				groundController.SkipStumpingReplay();
			}
			else if (currentAction == -20)
			{
				introCompleted();
			}
			else if (currentAction == 0)
			{
				Singleton<BattingScoreCard>.instance.Hide(boolean: true);
				ShowBowlingScoreCard();
			}
			else if (currentAction == 1)
			{
				// The bowling scorecard is where the bowling player picks the next over's bowler — and this
				// branch is reached by a RAW SCREEN TAP (GetMouseButtonDown(0) above; the method name says
				// keyboard, but on a phone every tap outside a UI element lands here). ShowBowlingScoreCard()
				// sets currentAction=1 the moment the card opens, so the player's NEXT tap anywhere dismissed
				// it instantly: the card was on screen for a fraction of a second and the tester only ever saw
				// the batting one ("bowling card aya hi nahi", both at game start and at over end — logs
				// 12-08 02:11 show panelIndex=2 immediately followed by panelIndex=0). The over-end bowler
				// selection was being destroyed by an ordinary tap. Online, on the side that actually picks,
				// require the tick BUTTON (a UI tap, which IsPointerOverUIObject already excludes from this
				// path) so the card stays up until the player is done with it. Everything else is unchanged:
				// offline, the batting side, and the tick button itself all continue exactly as before.
				if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
					&& CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex)
				{
					ConstantsData_M.MpLog("[BowlingScoreCard] Screen tap ignored on the bowling scorecard — the bowler picks here; use the tick button to continue.");
				}
				else
				{
					Singleton<BowlingScoreCard>.instance.Continue();
				}
			}
			else if (currentAction == 2)
			{
				if (Singleton<Intro>.instance != null)
				{
					Singleton<Intro>.instance.batsmanEntryStopped();
				}
			}
			else if (currentAction == 3)
			{
				if (Singleton<Intro>.instance != null)
				{
					Singleton<Intro>.instance.batsmanExitStopped();
				}
			}
			else if (currentAction == 4)
			{
				if (bowlingControlMode == 0)
				{

					if (CONTROLLER.CanBowlerBowl && CONTROLLER.PlayModeSelected == 8)
					{
						bowlingControlMode = 1;
						Singleton<BowlingControls>.instance.LockSpeed();
					}
					else if (CONTROLLER.PlayModeSelected != 8)
					{

						bowlingControlMode = 1;
						Singleton<BowlingControls>.instance.LockSpeed();
					}

				}
				else if (bowlingControlMode == 1)
				{
					bowlingControlMode = 2;
					Singleton<BowlingControls>.instance.LockAngle();
				}
				else if (bowlingControlMode == 2)
				{
					if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
					{
						Singleton<Tutorial>.instance.hideTutorial();
						Time.timeScale = 1f;
					}
					else if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex != CONTROLLER.BattingTeamIndex && CONTROLLER.BATSMANMOVE)
					{
						CONTROLLER.BOWLINGSPOTINFO = true;
						Singleton<Tutorial>.instance.hideTutorial();
						Time.timeScale = 1f;
					}
					else
					{
						Singleton<Tutorial>.instance.hideTutorial();
						Time.timeScale = 1f;
					}
				}
			}
			else if (currentAction == 5)
			{
				if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
				{
					Singleton<Tutorial>.instance.hideTutorial();
					Time.timeScale = 1f;
				}
				else
				{
					Singleton<Tutorial>.instance.hideTutorial();
					Time.timeScale = 1f;
				}
			}
			else if (currentAction == 10)
			{
				currentAction = -1;
			}
		}
		int touchCount = Input.touchCount;
		if (!isGamePaused && touchCount == 1 && !wasTouched)
		{
			wasTouched = true;
			if (currentAction == -20)
			{
				introCompleted();
			}
			else if (currentAction == 2)
			{
				if (Singleton<Intro>.instance != null)
				{
					Singleton<Intro>.instance.batsmanEntryStopped();
				}
			}
			else if (currentAction == 3 && Singleton<Intro>.instance != null)
			{
				Singleton<Intro>.instance.batsmanExitStopped();
			}
		}
		if (Input.GetMouseButtonUp(0) && !IsPointerOverUIObject())
		{
			touchCount = 0;
			wasTouched = false;
		}
		if (wasGameResumedAfterEscape)
		{
			CheckForResume();
		}
	}

	public void BowlAutomatic()
	{
		bowlingControlMode = 1;
		Singleton<BowlingControls>.instance.LockSpeed();
		bowlingControlMode = 2;
		Singleton<BowlingControls>.instance.LockAngle();
		Singleton<Tutorial>.instance.hideTutorial();
		Time.timeScale = 1f;
	}

	public string GetOverStr()
	{
		int num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6;
		int num2 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls % 6;
		string result = num + "." + num2;
		if (num >= powerPlayOver)
		{
			CONTROLLER.PowerPlay = false;
		}
		else
		{
			CONTROLLER.PowerPlay = true;
		}
		if (num >= slogOverScore)
		{
			CONTROLLER.SlogOvers = true;
		}
		else
		{
			CONTROLLER.SlogOvers = false;
		}
		float num3 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
		float num4 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls;
		float num5 = 0f;
		if (num4 > 0f)
		{
			num5 = num3 / num4 * 6f;
			int num6 = (int)(num5 * 100f);
			num5 = (CONTROLLER.RunRate = num6 / 100);
		}
		if (CONTROLLER.currentInnings == 1)
		{
			float num7 = (float)CONTROLLER.TargetToChase - num3;
			float num8 = (float)(CONTROLLER.totalOvers * 6) - num4;
			if (num8 > 0f)
			{
				num5 = num7 / num8 * 6f;
				int num9 = (int)(num5 * 100f);
				num5 = (CONTROLLER.ReqRunRate = num9 / 100);
			}
		}
		return result;
	}

	public void SetFielders()
	{
		CONTROLLER.FieldersArray = new int[11];
		CONTROLLER.FieldersArray[0] = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].KeeperIndex;
		CONTROLLER.FieldersArray[10] = CONTROLLER.CurrentBowlerIndex;
		int num = 0;
		for (int i = 0; i < CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList.Length; i++)
		{
			if (i != CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].KeeperIndex && i != CONTROLLER.CurrentBowlerIndex)
			{
				num++;
				CONTROLLER.FieldersArray[num] = i;
			}
		}
	}

	public void ShowTutorial(int tutorialValue)
	{
		if (CONTROLLER.PlayModeSelected == 8)
		{
			return;
		}
		Singleton<Tutorial>.instance.hideTutorial();
		if (CONTROLLER.tutorialToggle == 1)
		{
			Singleton<Tutorial>.instance.tutorialToggle.SetActive(value: true);
		}
		else
		{
			Singleton<Tutorial>.instance.tutorialToggle.SetActive(value: false);
		}
		if (!CONTROLLER.ReplayShowing && !(CONTROLLER.TargetPlatform == "standalone") && !(CONTROLLER.TargetPlatform == "web") && CONTROLLER.tutorialToggle != 0)
		{
			switch (tutorialValue)
			{
				case 0:
					Singleton<Tutorial>.instance.showPositionHolder();
					break;
				case 1:
					Singleton<Tutorial>.instance.showShotHolder();
					break;
				case 2:
					Singleton<Tutorial>.instance.showBowlingHolder();
					break;
			}
		}
	}

	public bool canShowTutorial()
	{
		if (CONTROLLER.ReplayShowing || CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.TargetPlatform == "standalone" || CONTROLLER.TargetPlatform == "web" || CONTROLLER.tutorialToggle == 0 || CONTROLLER.PlayModeSelected == 6)
		{
			return false;
		}
		return true;
	}

	public void EnableMovement(bool boolean)
	{
		if (boolean)
		{
			EnableShot(boolean: true);
			userInputAction = 10;
			if (CONTROLLER.tutorialToggle == 1)
			{
				ShowTutorial(0);
			}
			Singleton<GroundController>.instance.isNextBallInPlay = true;
		}
		else
		{
			ShowTutorial(-1);
		}
	}

	public void DetectBatsmanMoveMouse()
	{
		if (Input.GetMouseButton(0))
		{
			if (previousMousePosition.x == 0f && previousMousePosition.y == 0f)
			{
				previousMousePosition = Input.mousePosition;
			}
			Vector2 vector = new Vector2(Input.mousePosition.x - previousMousePosition.x, Input.mousePosition.y - previousMousePosition.y);
			previousMousePosition = Input.mousePosition;
			if (vector.x < 0f)
			{
				MoveLeft(boolean: true);
			}
			else
			{
				MoveLeft(boolean: false);
			}
			if (vector.x > 0f)
			{
				MoveRight(boolean: true);
			}
			else
			{
				MoveRight(boolean: false);
			}
		}
		if (Input.GetMouseButtonUp(0))
		{
			MoveLeft(boolean: false);
			MoveRight(boolean: false);
		}
	}

	public void DetectBatsmanMove()
	{
		int touchCount = Input.touchCount;
		Touch[] touches = Input.touches;
		if (touchCount > 0)
		{
			Vector2 deltaPosition = Input.GetTouch(0).deltaPosition;
			if (deltaPosition.x < 0f)
			{
				MoveLeft(boolean: true);
			}
			else
			{
				MoveLeft(boolean: false);
			}
			if (deltaPosition.x > 0f)
			{
				MoveRight(boolean: true);
			}
			else
			{
				MoveRight(boolean: false);
			}
		}
		else
		{
			MoveLeft(boolean: false);
			MoveRight(boolean: false);
		}
	}

	public void MoveLeft(bool boolean)
	{
		groundController.MoveLeftSide(boolean);
	}

	public void MoveRight(bool boolean)
	{
		groundController.MoveRightSide(boolean);
	}

	public void EnableShot(bool boolean)
	{
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !CONTROLLER.ReplayShowing)
		{
			Singleton<BattingControls>.instance.Hide(boolean: false);
			Singleton<BattingControls>.instance.EnableShot(boolean);
			Singleton<BattingControls>.instance.ChangeButtonImage(0);
			if (boolean)
			{
				currentAction = 5;
			}
			else
			{
				currentAction = -1;
			}
		}
		if (!boolean)
		{
			ShowTutorial(-1);
		}
	}

	public void EnableShotSelection(bool boolean)
	{
		if (boolean)
		{
			userInputAction = 11;
		}
	}

	private bool GetCanMakeShot(Vector2 pos)
	{
		if (isGamePaused)
		{
			return false;
		}
		return true;
	}

	public void DetectBatsmanShotMouse()
	{
		if (Input.GetMouseButtonDown(0) && !isNewTouch)
		{
			if (!GetCanMakeShot(Input.mousePosition))
			{
				return;
			}
			isNewTouch = true;
			isTouchEnded = false;
			touchStartPosition = Input.mousePosition;
			touchStartTime = Time.time;
		}
		if (Input.GetMouseButtonUp(0) && isNewTouch)
		{
			touchEndPosition = Input.mousePosition;
			GetTheShot();
		}
	}

	public void DetectBatsmanShot()
	{
		int touchCount = Input.touchCount;
		if (touchCount <= 0 || isShotCompleted)
		{
			return;
		}
		touchPhases = Input.touches;
		bool flag = true;
		if (touchPhases.Length > 0 && touchPhases[0].phase == TouchPhase.Began)
		{
			flag = GetCanMakeShot(touchPhases[0].position);
			if (!flag)
			{
				return;
			}
		}
		if (flag && !isNewTouch && (touchPhases[0].phase == TouchPhase.Began || touchPhases[0].phase == TouchPhase.Stationary))
		{
			isNewTouch = true;
			isTouchEnded = false;
			touchStartPosition = touchPhases[0].position;
			touchStartTime = Time.time;
		}
		else if (touchPhases[0].phase == TouchPhase.Ended && !isTouchEnded)
		{
			touchEndPosition = touchPhases[0].position;
			GetTheShot();
		}
	}

	public void GetShotSelected()
	{
		if (!isNewTouch || isShotCompleted || isInningsComplete)
		{
			return;
		}
		if ((CONTROLLER.TargetPlatform == "ios" || CONTROLLER.TargetPlatform == "android") && !Application.isEditor && touchPhases.Length > 0)
		{
			touchPhases = Input.touches;
			if (!isTouchEnded)
			{
				touchEndPosition = touchPhases[0].position;
				GetTheShot();
			}
		}
		else if (isNewTouch && !isTouchEnded)
		{
			touchEndPosition = Input.mousePosition;
			GetTheShot();
		}
	}

	public float DistanceBetweenTwoVector2(Transform go1, Transform go2)
	{
		float num = go1.position.x - go2.position.x;
		float num2 = go1.position.z - go2.position.z;
		return Mathf.Sqrt(num * num + num2 * num2);
	}

	public void GetTheShot()
	{
		if (!isNewTouch || isTouchEnded)
		{
			return;
		}
		isNewTouch = false;
		isTouchEnded = true;
		touchStartTime = 0f;
		isShotCompleted = true;
		float num = touchEndPosition.x - touchStartPosition.x;
		float num2 = touchEndPosition.y - touchStartPosition.y;
		if (num != 0f || num2 != 0f)
		{
			float num3 = Mathf.Atan2(num2, num) * 57.29578f;
			num3 = (num3 + 360f) % 360f;
			if (num3 == 0f)
			{
				chosenBattingAngle = 5;
			}
			else if ((num3 < 22.5f && num3 >= -22.5f) || (num3 < 360f && num3 >= 337.5f))
			{
				chosenBattingAngle = 7;
			}
			else if (num3 < 67.5f && num3 >= 22.5f)
			{
				chosenBattingAngle = 6;
			}
			else if (num3 < 112.5f && num3 >= 67.5f)
			{
				chosenBattingAngle = 5;
			}
			else if (num3 < 157.5f && num3 >= 112.5f)
			{
				chosenBattingAngle = 4;
			}
			else if (num3 < 202.5f && num3 >= 157.5f)
			{
				chosenBattingAngle = 3;
			}
			else if (num3 < 247.5f && num3 >= 202.5f)
			{
				chosenBattingAngle = 2;
			}
			else if (num3 < 292.5f && num3 >= 247.5f)
			{
				chosenBattingAngle = 1;
			}
			else if (num3 < 337.5f && num3 >= 292.5f)
			{
				chosenBattingAngle = 8;
			}
			Singleton<GroundController>.instance.BallAngle(num3);
			// Reconnect hardening: a reloaded-but-inactive BattingControls has a NULL singleton — the old
			// unguarded InnerRing read threw an NRE HERE, after isShotCompleted was already latched at the
			// top of GetTheShot, killing input for the whole delivery (post-reconnect "pehli ball miss").
			bool _powerRing = Singleton<BattingControls>.instance != null
				&& Singleton<BattingControls>.instance.InnerRing != null
				&& Singleton<BattingControls>.instance.InnerRing.sprite != Singleton<BattingControls>.instance.btnStates[0];
			ShotSelected(_powerRing, chosenBattingAngle, num3);
		}
	}

	public void ShotSelected(bool isPower, int SelectedAngle, float Num3)
	{
		userInputAction = -1;
		groundController.ShotSelected(isPower, SelectedAngle, Num3);
	}

	public void EnableRun(bool boolean)
	{
		userInputAction = -1;
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !CONTROLLER.ReplayShowing)
		{
			if (boolean)
			{
				currentAction = 6;
			}
			else
			{
				currentAction = -1;
			}
			StartCoroutine(Singleton<BattingControls>.instance.EnableRun(boolean));
		}
	}

	public void EnableCancelRun(bool boolean)
	{
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && !CONTROLLER.ReplayShowing)
		{
			Singleton<BattingControls>.instance.EnableCancelRun(boolean);
		}
	}

	public void InitRun(bool boolean)
	{
		groundController.InitRun(boolean);
		Invoke("initRunFalse", 1f);
	}

	private void initRunFalse()
	{
		groundController.InitRun(boolean: false);
	}

	public void CancelRun()
	{
		groundController.touchCancelRun();
	}

	public void ReachedCrease()
	{
		Singleton<BattingControls>.instance.RunCompleted();
	}

	public void ShowQuitPopup()
	{
		GameObject original = Resources.Load("Prefabs/GameQuitConfirm") as GameObject;
		GameObject gameObject = UnityEngine.Object.Instantiate(original);
		gameObject.name = "GameQuitConfirm";
		gameObject.transform.localPosition = new Vector3(0f, 0f, 1f);
	}

	public void GameQuitted()
	{
		hasGameQuit = true;
		CONTROLLER.isQuit = true;
		GamePaused(boolean: false);
		stopCommentarySnd();
		groundController.isFieldRestrictionActive = true;
		CONTROLLER.fielderChangeIndex = 1;
		CONTROLLER.computerFielderChangeIndex = 0;
		//ResetAllLocalVariables();
		if (CONTROLLER.sndController != null)
		{
			CONTROLLER.sndController.RemoveGameSounds();
		}
		CONTROLLER.SceneIsLoading = true;
		if (CONTROLLER.PlayModeSelected == 6)
		{

		}
		else if (Singleton<TournamentFailedPopUp>.instance.showMe)
		{
			Singleton<TournamentFailedPopUp>.instance.ShowMe();
		}
		else
		{
			LoadMainMenuScene();
		}
	}

	public void LoadMainMenuScene()
	{
		Singleton<NavigationBack>.instance.deviceBack = null;
		Singleton<LoadingPanelTransition>.instance.PanelTransition1("Home");
	}

	private void IncreaseConfidenceLevel(int runsScored, int validBall)
	{
		if (validBall == 0)
		{
			return;
		}
		float num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal;
		float confidenceInc = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceInc;
		confidenceInc = ((CONTROLLER.totalOvers >= 10) ? (confidenceInc * (10f / (float)CONTROLLER.totalOvers)) : (confidenceInc * (4f / (float)CONTROLLER.totalOvers)));
		if (runsScored <= 0)
		{
			confidenceInc = -0.05f;
		}
		if (num + confidenceInc < 10f)
		{
			if (runsScored < 6)
			{
				num += confidenceInc;
			}
		}
		else
		{
			num = 10f;
		}
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ConfidenceVal = num;
	}

	private int GetStrikeRate(int playerID)
	{
		float num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.RunsScored;
		float num2 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[playerID].BatsmanList.BallsPlayed;
		if (num2 > 0f)
		{
			int num3 = (int)(num / num2 * 100f);
			return (int)(Mathf.Round(num3 * 100) / 100f);
		}
		return 0;
	}

	private void CheckForResume()
	{
		if (Time.realtimeSinceStartup > resumeAfterEscapeDelay + 0.1f)
		{
			wasGameResumedAfterEscape = false;
			if (canResumeGameplay)
			{
				Singleton<PauseGameScreen>.instance.Hide(boolean: true);
				GamePaused(boolean: false);
			}
		}
	}

	private void FormatNames()
	{
		for (int i = 0; i < CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList.Length; i++)
		{
			string text = string.Empty;
			string text2 = string.Empty;
			string[] array = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].PlayerName.Split(" "[0]);
			string[] array2 = new string[array.Length];
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] != string.Empty && array[j] != " ")
				{
					string text3 = array[j];
					string text4 = text3.Substring(0, 1);
					string text5 = text3.Substring(1);
					text4 = text4.ToUpper();
					text5 = text5.ToLower();
					text3 = text4 + text5;
					string text6 = ((j >= array.Length - 1) ? (text4 + text5) : text4);
					array[j] = text3;
					array2[j] = text6;
				}
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] != string.Empty && array[j] != " ")
				{
					if (j < array.Length - 1)
					{
						text = text + array[j] + " ";
						text2 = text2 + array2[j] + " ";
					}
					else
					{
						text += array[j];
						text2 += array2[j];
					}
				}
			}
			if (text.Length > 12)
			{
				text = text.Substring(0, 12);
			}
			if (text2.Length > 12)
			{
				text2 = text2.Substring(0, 12);
			}
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].PlayerName = text;
			CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].ScoreboardName = text2;
			text = string.Empty;
			text2 = string.Empty;
			array = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].PlayerName.Split(" "[0]);
			array2 = new string[array.Length];
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] != string.Empty && array[j] != " ")
				{
					string text3 = array[j];
					string text4 = text3.Substring(0, 1);
					string text5 = text3.Substring(1);
					text4 = text4.ToUpper();
					text5 = text5.ToLower();
					text3 = text4 + text5;
					string text6 = ((j >= array.Length - 1) ? (text4 + text5) : text4);
					array[j] = text3;
					array2[j] = text6;
				}
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] != string.Empty && array[j] != " ")
				{
					if (j < array.Length - 1)
					{
						text = text + array[j] + " ";
						text2 = text2 + array2[j] + " ";
					}
					else
					{
						text += array[j];
						text2 += array2[j];
					}
				}
			}
			if (text.Length > 12)
			{
				text = text.Substring(0, 12);
			}
			if (text2.Length > 12)
			{
				text2 = text2.Substring(0, 12);
			}
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].PlayerName = text;
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].ScoreboardName = text2;
		}
	}

	private void TrimLastSpaces()
	{
		for (int i = 0; i < CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList.Length; i++)
		{
			string playerName = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].PlayerName;
			if (playerName[playerName.Length - 1] == ' ')
			{
				TrimSpaces(i, CONTROLLER.myTeamIndex);
			}
			playerName = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].PlayerName;
			if (playerName[playerName.Length - 1] == ' ')
			{
				TrimSpaces(i, CONTROLLER.opponentTeamIndex);
			}
		}
	}

	private void TrimSpaces(int index, int teamIndex)
	{
		string empty = string.Empty;
		empty = CONTROLLER.TeamList[teamIndex].PlayerList[index].PlayerName;
		empty = empty.Substring(0, empty.Length - 1);
		CONTROLLER.TeamList[teamIndex].PlayerList[index].PlayerName = empty;
		if (empty[empty.Length - 1] == ' ')
		{
			TrimSpaces(index, teamIndex);
		}
	}

	public void UpdateAction(int pageID)
	{
		currentAction = pageID;
	}

	public int GetAction()
	{
		return currentAction;
	}

	public void PostGoogleAnalyticsEvent()
	{
		if (CONTROLLER.PlayModeSelected != 0 && CONTROLLER.PlayModeSelected != 1)
		{
		}
	}

	public int getBowlingTeamIndex()
	{
		int num = 0;
		return CONTROLLER.BowlingTeam[CONTROLLER.currentInnings];
	}


	public int getCurrentBattingTeam(int _index)
	{
		return CONTROLLER.BattingTeam[CONTROLLER.currentInnings];
	}

	public int getCurrentBowlTeam(int _index)
	{
		return CONTROLLER.BowlingTeam[CONTROLLER.currentInnings];
	}

	private void SetTargetToWin()
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i <= CONTROLLER.currentInnings; i++)
		{
			if (i < 2)
			{
				if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					if (CONTROLLER.BattingTeam[i] == CONTROLLER.myTeamIndex)
					{
						num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
					}
					else
					{
						num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
					}
				}
				else if (CONTROLLER.BattingTeam[i] == CONTROLLER.myTeamIndex)
				{
					num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
				}
				else
				{
					num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
				}
			}
			else if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
				if (CONTROLLER.BattingTeam[i] == CONTROLLER.myTeamIndex)
				{
					num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
				}
				else
				{
					num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
				}
			}
			else if (CONTROLLER.BattingTeam[i] == CONTROLLER.myTeamIndex)
			{
				num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
			}
			else
			{
				num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
			}
		}
		int value = num - num2;
		CONTROLLER.TargetToChase = Mathf.Abs(value);
		AutoSave.SaveInGameMatch();
	}

	public bool checkforSessionComplete()
	{
		int num = CONTROLLER.ballsBowledPerDay / 6 / (CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] / CONTROLLER.noOfSession);
		if (CONTROLLER.ballsBowledPerDay / 6 % (CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] / CONTROLLER.noOfSession) == 0 && CONTROLLER.ballsBowledPerDay / 6 != 0)
		{
			if (CONTROLLER.MaxDays == CONTROLLER.currentDay && CONTROLLER.currentSession == 3)
			{
				return true;
			}
			return true;
		}
		return false;
	}

	public void setSession()
	{
		int num = CONTROLLER.ballsBowledPerDay / 6 / (CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] / CONTROLLER.noOfSession);
		if (CONTROLLER.currentSession < num)
		{
			CONTROLLER.currentSession++;
			if (CONTROLLER.currentDay < CONTROLLER.MaxDays && CONTROLLER.currentSession % 3 == 0)
			{
				CONTROLLER.currentDay++;
				CONTROLLER.currentSession = 0;
				CONTROLLER.ballsBowledPerDay = 0;
			}
			isSessionFinished = false;
			AutoSave.SaveInGameMatch();
		}
	}

	public bool CheckForMatchCompletion()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		bool flag = false;
		if (CONTROLLER.MaxDays == CONTROLLER.currentDay && CONTROLLER.currentSession >= 2)
		{
			int num6 = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6 - CONTROLLER.ballsBowledPerDay;
			for (int i = 0; i <= CONTROLLER.currentInnings; i++)
			{
				num4++;
			}
			if ((num4 >= 4 || num6 <= 0) && num4 < 4 && num6 == 0)
			{
				return true;
			}
		}
		if (CONTROLLER.currentInnings == 2 && CONTROLLER.isFollowOn)
		{
			num4 = 0;
			num5 = 0;
			num = 0;
			num2 = 0;
			int num7 = 0;
			for (int j = 0; j <= CONTROLLER.currentInnings; j++)
			{
				if (j < 2)
				{
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						if (CONTROLLER.BattingTeam[j] == CONTROLLER.myTeamIndex)
						{
							num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores1;
							num4++;
							num7++;
						}
						else
						{
							num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores1;
							num4++;
							num5++;
						}
					}
					else if (CONTROLLER.BattingTeam[j] == CONTROLLER.myTeamIndex)
					{
						num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores1;
						num4++;
						num5++;
					}
					else
					{
						num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores1;
						num4++;
						num7++;
					}
				}
				else if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					if (CONTROLLER.BattingTeam[j] == CONTROLLER.myTeamIndex)
					{
						num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores2;
						num4++;
						num7++;
					}
					else
					{
						num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores2;
						num4++;
						num5++;
					}
				}
				else if (CONTROLLER.BattingTeam[j] == CONTROLLER.myTeamIndex)
				{
					num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores2;
					num4++;
					num5++;
				}
				else
				{
					num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores2;
					num4++;
					num7++;
				}
			}
			if (num5 == 1)
			{
				flag = true;
			}
			if (num < num2 && CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchWickets1 >= CONTROLLER.totalWickets && CONTROLLER.currentInnings < 2)
			{
				return true;
			}
			if (num < num2 && CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchWickets2 >= CONTROLLER.totalWickets && CONTROLLER.currentInnings >= 2)
			{
				return true;
			}
			if (num4 == 3 && num < num2 && !flag)
			{
				return true;
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].isDeclared1 && CONTROLLER.currentInnings < 2)
			{
				if (num4 == 3 && num7 == 2 && num < num2)
				{
					return true;
				}
			}
			else if (CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].isDeclared2 && CONTROLLER.currentInnings >= 2 && num4 == 3 && num7 == 2 && num < num2)
			{
				return true;
			}
		}
		if (CONTROLLER.currentInnings == 2 && !CONTROLLER.isFollowOn)
		{
			num4 = 0;
			num5 = 0;
			num = 0;
			num2 = 0;
			int num8 = 0;
			for (int k = 0; k <= CONTROLLER.currentInnings; k++)
			{
				if (k < 2)
				{
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						if (CONTROLLER.BattingTeam[k] == CONTROLLER.myTeamIndex)
						{
							num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[k]].TMcurrentMatchScores1;
							num4++;
							num8++;
						}
						else
						{
							num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[k]].TMcurrentMatchScores1;
							num4++;
							num5++;
						}
					}
					else if (CONTROLLER.BattingTeam[k] == CONTROLLER.myTeamIndex)
					{
						num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[k]].TMcurrentMatchScores1;
						num4++;
						num5++;
					}
					else
					{
						num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[k]].TMcurrentMatchScores1;
						num4++;
						num8++;
					}
				}
				else if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					if (CONTROLLER.BattingTeam[k] == CONTROLLER.myTeamIndex)
					{
						num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[k]].TMcurrentMatchScores2;
						num4++;
						num8++;
					}
					else
					{
						num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[k]].TMcurrentMatchScores2;
						num4++;
						num5++;
					}
				}
				else if (CONTROLLER.BattingTeam[k] == CONTROLLER.myTeamIndex)
				{
					num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[k]].TMcurrentMatchScores2;
					num4++;
					num5++;
				}
				else
				{
					num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[k]].TMcurrentMatchScores2;
					num4++;
					num8++;
				}
			}
			if (num5 == 1)
			{
				flag = true;
			}
			if (num < num2 && CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchWickets1 >= CONTROLLER.totalWickets && CONTROLLER.currentInnings < 2)
			{
				return true;
			}
			if (num < num2 && CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchWickets2 >= CONTROLLER.totalWickets && CONTROLLER.currentInnings >= 2)
			{
				return true;
			}
			if (num4 == 3 && num < num2 && !flag)
			{
				return true;
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].isDeclared1 && CONTROLLER.currentInnings < 2)
			{
				if (num4 == 3 && num8 == 2 && num < num2)
				{
					return true;
				}
			}
			else if (CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].isDeclared2 && CONTROLLER.currentInnings >= 2 && num4 == 3 && num8 == 2 && num < num2)
			{
				return true;
			}
		}
		if (CONTROLLER.currentInnings == 3)
		{
			num3 = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchScores2;
			num = 0;
			num2 = 0;
			for (int l = 0; l <= CONTROLLER.currentInnings; l++)
			{
				if (l < 2)
				{
					if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
					{
						if (CONTROLLER.BattingTeam[l] == CONTROLLER.myTeamIndex)
						{
							num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[l]].TMcurrentMatchScores1;
						}
						else
						{
							num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[l]].TMcurrentMatchScores1;
						}
					}
					else if (CONTROLLER.BattingTeam[l] == CONTROLLER.myTeamIndex)
					{
						num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[l]].TMcurrentMatchScores1;
					}
					else
					{
						num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[l]].TMcurrentMatchScores1;
					}
				}
				else if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					if (CONTROLLER.BattingTeam[l] == CONTROLLER.myTeamIndex)
					{
						num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[l]].TMcurrentMatchScores2;
					}
					else
					{
						num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[l]].TMcurrentMatchScores2;
					}
				}
				else if (CONTROLLER.BattingTeam[l] == CONTROLLER.myTeamIndex)
				{
					num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[l]].TMcurrentMatchScores2;
				}
				else
				{
					num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[l]].TMcurrentMatchScores2;
				}
			}
			if (num3 > CONTROLLER.TargetToChase)
			{
				return true;
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchWickets1 >= CONTROLLER.totalWickets && CONTROLLER.currentInnings < 2)
			{
				return true;
			}
			if (CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchWickets2 >= CONTROLLER.totalWickets && CONTROLLER.currentInnings >= 2)
			{
				return true;
			}
		}
		return false;
	}

	public void setOversAfterSession()
	{
		setSession();
		canPauseGameplay = false;
		hasReachedMaxRuns = false;
		currentBallNumber = -1;
		runsInCurrentOver = 0;
		Singleton<Scoreboard>.instance.NewOver();
		Singleton<Scoreboard>.instance.HidePause(boolean: true);
		if (CONTROLLER.currentInnings == 0)
		{
			SetFielders();
			groundController.NewOver();
		}
		canPauseGameplay = true;
		isGamePaused = false;
		if (CONTROLLER.BattingTeam[CONTROLLER.currentInnings] == CONTROLLER.opponentTeamIndex)
		{
			if (!isInningsComplete && !BowlingScoreCard.newOverCalled)
			{
				Singleton<BowlingScoreCard>.instance.OverCompleted();
			}
			ShowBattingScoreCard();
		}
		else
		{
			if (!isInningsComplete)
			{
				Singleton<BowlingScoreCard>.instance.OverCompleted();
			}
			ShowBattingScoreCard();
		}
	}

	public bool CheckForAIDeclaration()
	{
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.currentInnings == 3)
		{
			return false;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		if (CONTROLLER.currentDay >= 2 && CONTROLLER.BattingTeam[0] == CONTROLLER.opponentTeamIndex && CONTROLLER.currentInnings < 2)
		{
			num = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchScores1;
			num2 = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchBalls1;
			if (num > CONTROLLER.AIFirstInningsDeclareRuns && num2 > CONTROLLER.AIFirstInningsDeclareBalls)
			{
				return true;
			}
		}
		else if (CONTROLLER.currentDay > 1 && CONTROLLER.BattingTeam[1] == CONTROLLER.opponentTeamIndex && CONTROLLER.currentInnings < 2)
		{
			num = CONTROLLER.TeamList[CONTROLLER.BattingTeam[1]].TMcurrentMatchScores1;
			num3 = CONTROLLER.TeamList[CONTROLLER.BattingTeam[0]].TMcurrentMatchScores1;
			if (num > num3 + CONTROLLER.AiFirstInngsLeadDeclareRuns && CONTROLLER.currentDay < 5)
			{
				return true;
			}
		}
		else if (CONTROLLER.BattingTeam[CONTROLLER.currentInnings] == CONTROLLER.opponentTeamIndex && CONTROLLER.currentDay > 3 && CONTROLLER.currentInnings > 1 && CONTROLLER.currentSession > 1)
		{
			int num4 = 0;
			int tMcurrentMatchBalls = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchBalls2;
			num3 = 0;
			for (int i = 0; i <= CONTROLLER.currentInnings; i++)
			{
				if (i < 2)
				{
					if (CONTROLLER.BattingTeam[i] == CONTROLLER.opponentTeamIndex)
					{
						num4 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
					}
					else
					{
						num3 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
					}
				}
				else if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex)
				{
					num4 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
				}
				else
				{
					num3 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
				}
			}
			if (num4 > num3 + CONTROLLER.AiTargetDeclareRuns)
			{
				return true;
			}
			if (CONTROLLER.currentDay == 5)
			{
				int num5 = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6 - tMcurrentMatchBalls;
				num4 = 0;
				for (int j = 0; j <= CONTROLLER.currentInnings; j++)
				{
					if (CONTROLLER.BattingTeam[j] == CONTROLLER.opponentTeamIndex)
					{
						num4 = ((j >= 2) ? (num4 + CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores2) : (num4 + CONTROLLER.TeamList[CONTROLLER.BattingTeam[j]].TMcurrentMatchScores1));
					}
				}
				if (num4 > num3 + CONTROLLER.AiTargetDeclareRuns && CONTROLLER.currentSession > 0 && num4 - num3 > num5 * 7)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void checkForDeclaration()
	{
		isInningsComplete = CheckForInningsComplete();
		if (!isInningsComplete)
		{
			return;
		}
		canPauseGameplay = false;
		isGamePaused = false;
		groundController.isFieldRestrictionActive = true;
		groundController.resetNoBallVairables();
		CONTROLLER.fielderChangeIndex = 1;
		CONTROLLER.computerFielderChangeIndex = 1;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i <= CONTROLLER.currentInnings; i++)
		{
			if (i < 2)
			{
				if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
				}
				else
				{
					num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
				}
			}
			else if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
				num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
			}
			else
			{
				num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
			}
		}
		AutoSave.SaveInGameMatch();
		if (!CheckForMatchCompletion())
		{
			if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex)
			{
			}
			ShowBattingScoreCard();
		}
		else
		{
			isGamePaused = false;
			CONTROLLER.ShowTMGameOver = true;
			ShowBattingScoreCard();
		}
	}

	public void showTargetScreen()
	{
		int value = CONTROLLER.totalOvers - CONTROLLER.ballsBowledPerDay / 6;
		float num = (float)CONTROLLER.ballsBowledPerDay / 6f - (float)(CONTROLLER.totalOvers / CONTROLLER.noOfSession) * ((float)CONTROLLER.currentSession + 1f);
		if (CONTROLLER.currentDay == CONTROLLER.MaxDays && CONTROLLER.currentSession == 2 && Mathf.Abs(value) <= 1)
		{
			isGamePaused = false;
			CONTROLLER.ShowTMGameOver = true;
			ShowBattingScoreCard();
			return;
		}
		if (num < 0f && num > -1f)
		{
			CONTROLLER.ballsBowledPerDay = CONTROLLER.totalOvers / CONTROLLER.noOfSession * 6 * (CONTROLLER.currentSession + 1);
			CONTROLLER.isSkipBallsForSession = true;
			AutoSave.SaveInGameMatch();
			//Singleton<TMSessionScreen>.instance.ShowMe();
			currentAction = 10;
			return;
		}
		int num2 = CONTROLLER.ballsBowledPerDay % 6;
		if (num2 == 0)
		{
			CONTROLLER.ballsBowledPerDay += num2;
		}
		else
		{
			CONTROLLER.ballsBowledPerDay += 6 - num2;
		}
		//Singleton<TMSessionScreen>.instance.ShowMe();
		currentAction = 10;
	}


}
