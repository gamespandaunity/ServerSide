using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
//using Newtonsoft.Json.Bson;

using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class TeamSelectionTWO : Singleton<TeamSelectionTWO>
{
	//Photon Removal	private PhotonView photonView;

	public GameObject Holder;

	public GameObject panel1;

	public GameObject panel2;

	public GameObject outerPanel;

	public GameObject SOPanel;

	public ButtonSpriteSwap quickPlay;

	public ButtonSpriteSwap others;

	public GameObject entryFeePanel;

	public ScreenExitAnimation exitAnim;

	public ButtonSpriteSwap overSpriteSwap;

	public Text MyTeamName;

	public Text MyTeamName2;

	public Text SOMyTeamName;

	private bool canClick = true;

	public Text OppTeamName;

	public Text SOOppTeamName;

	public Text Tittle;

	public Text SubTittle;

	public Image MyTeamFlag;

	public Image MyTeamFlag2;

	public Image SOMyTeamFlag;

	public Image OppTeamFlag;

	public Image SOOppTeamFlag;

	public Image leftFlag;

	public Image midFlag;

	public Image rightFlag;

	public Image leftFlag2;

	public Image midFlag2;

	public Image rightFlag2;

	public Sprite[] btnSprite;

	public Button[] overs;

	public Button[] difficulty;

	private string oversText;

	private List<int> topTeams;

	private List<int> qualifiedTeams;

	private int topEightTeam = 8;

	private List<int> groupAteam;

	private List<int> groupBteam;

	//Multiplayer

	[SerializeField] private Button NextBtm;

	private float secCount;

	[SerializeField] private Image timerImage;

	[SerializeField] private Text timerText;

	[SerializeField] private GameObject Timer;

	private bool READY;

	DG.Tweening.Sequence s;
	protected void Start()
	{
		hideMe();
		READY = false;
		//Photon Removal		photonView = gameObject.GetComponent<PhotonView>();
	}

	private void Update()
	{
		if (CONTROLLER.PlayModeSelected == 8 && difficulty[3].gameObject.activeInHierarchy)
		{
			CONTROLLER.difficultyMode = "easy";
			difficulty[3].gameObject.SetActive(false);
			difficulty[4].gameObject.SetActive(false);
			difficulty[5].gameObject.SetActive(false);
		}
		else if (CONTROLLER.PlayModeSelected != 8 && !difficulty[3].gameObject.activeInHierarchy)
		{
			difficulty[3].gameObject.SetActive(true);
			difficulty[4].gameObject.SetActive(true);
			difficulty[5].gameObject.SetActive(true);
		}
	}

	//Photon Removal	[PunRPC]
	public void RPC_ChangeMyTeamIndex(int MyTeamIndex)
	{
		CONTROLLER.opponentTeamIndex = MyTeamIndex;
		//print("AGAINAA");
		////Debug.Log("TEEEAMM IDX : " + MyTeamIndex + "MYTEAMM : "+CONTROLLER.myTeamIndex);
		SetQuickPlay();
	}

	/// <summary>
	/// Ensures CONTROLLER.TeamList is populated before any team-selection arrow
	/// touches TeamList[...] / TeamList.Length. In the standalone Cricket build,
	/// the Preloader scene parses the team XML before the menu loads. In the
	/// integrated multi-game app the Cricket Preloader can be skipped, leaving
	/// TeamList null and crashing the arrow handlers with a NullReferenceException.
	/// This lazily triggers the prefs loaders (ParseXML is synchronous) so the
	/// list is built on demand. Returns false if it still couldn't be populated,
	/// in which case the caller should no-op rather than crash.
	/// </summary>
	private bool EnsureTeamListReady()
	{
		if (CONTROLLER.TeamList != null && CONTROLLER.TeamList.Length > 0)
			return true;

		try
		{
			// Replicate LoadPlayerPrefsTWO.GetTeamList() directly so this works even
			// when that loader isn't present in the integrated app's scene. The default
			// team list ships in Resources/WorldCupSchedule.bytes — its structure
			// (cricket > schedule > League > team) is exactly what XMLReader.ParseXML
			// expects. User-edited squads persist in the "teamlist" PlayerPref.
			if (PlayerPrefs.HasKey("teamlist"))
			{
				XMLReader.ParseXML(PlayerPrefs.GetString("teamlist"));
			}
			else
			{
				TextAsset xml = Resources.Load<TextAsset>("WorldCupSchedule");
				if (xml != null) XMLReader.ParseXML(xml.text);
			}

			// Fallback: if a loader singleton is present, let it try too.
			if ((CONTROLLER.TeamList == null || CONTROLLER.TeamList.Length == 0)
				&& LoadPlayerPrefsTWO.instance != null)
				LoadPlayerPrefsTWO.instance.GetTeamList();
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[TeamSelectionTWO] TeamList lazy-load failed: " + ex.Message);
		}

		return CONTROLLER.TeamList != null && CONTROLLER.TeamList.Length > 0;
	}

	public void myTeamSelectionLeftButton()
	{
		if (!EnsureTeamListReady()) return;
		if (CONTROLLER.PlayModeSelected == 8 && READY)
		{
			return;
		}
		CONTROLLER.myTeamIndex--;
		////print("Current team _index" +CONTROLLER.myTeamIndex+" : "+CONTROLLER.TeamList[CONTROLLER.myTeamIndex].teamName + " : " + CONTROLLER.TeamList[CONTROLLER.myTeamIndex].teamId);
		myTeamLeftArrowSelected();
		if (CONTROLLER.PlayModeSelected == 7 && (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "KEN" || CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "NED" || CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "SCO" || CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "UAE"))
		{
		}
		else if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			SetQuickPlay();
		}

	}

	private void myTeamLeft()
	{
		CONTROLLER.myTeamIndex--;
		myTeamLeftArrowSelected();
		if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5 || CONTROLLER.PlayModeSelected == 7)
		{
			SetQuickPlay();
		}
		else
		{
			SetTournamentPlay();
		}
		s = DOTween.Sequence();
		s.Insert(0f, midFlag.transform.DOScaleX(1f, 0.2f)).OnComplete(EnableClick);
	}

	private void EnableClick()
	{
		canClick = true;
	}

	public void myTeamSelectionRightButton()
	{
		if (!EnsureTeamListReady()) return;
		if (CONTROLLER.PlayModeSelected == 8 && READY)
		{
			return;
		}
		CONTROLLER.myTeamIndex++;
		myTeamRightArrowSelected();
		if (CONTROLLER.PlayModeSelected == 7 && (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "KEN" || CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "NED" || CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "SCO" || CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "UAE"))
		{
			//myTeamSelectionRightButton();
		}
		else if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			SetQuickPlay();
		}
		//else
		//{
		//	SetTournamentPlay();
		//}
	}

	private void myTeamLeftArrowSelected()
	{
		if (CONTROLLER.PlayModeSelected == 8 && READY)
		{
			return;
		}
		if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			if (CONTROLLER.myTeamIndex == CONTROLLER.opponentTeamIndex)
			{
				CONTROLLER.myTeamIndex--;
			}
			if (CONTROLLER.myTeamIndex < 0)
			{
				CONTROLLER.myTeamIndex = CONTROLLER.TeamList.Length - 1;
				if (CONTROLLER.myTeamIndex == CONTROLLER.opponentTeamIndex)
				{
					CONTROLLER.myTeamIndex--;
				}
			}
		}
		else if (CONTROLLER.myTeamIndex < 0)
		{
			CONTROLLER.myTeamIndex = CONTROLLER.TeamList.Length - 1;
		}
		if (CONTROLLER.PlayModeSelected == 8)
		{
			//Photon Removal	if (PhotonNetwork.IsConnected)
			{
				//Photon Removal      photonView.RPC("RPC_ChangeMyTeamIndex", RpcTarget.OthersBuffered, CONTROLLER.myTeamIndex);
			}
			if (GameConstants.isWithAI == false)
			{
				//Photon Removal       photonView.RPC("RPC_ChangeMyTeamIndex", RpcTarget.OthersBuffered, CONTROLLER.myTeamIndex);
				CricketNetworkManager.instance.CmdChangeMyTeamIndex(staticVariables.UserProfiledata.user._id, CONTROLLER.myTeamIndex);
			}


		}
	}

	private void myTeamRightArrowSelected()
	{
		if (CONTROLLER.PlayModeSelected == 8 && READY)
		{
			return;
		}
		if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			if (CONTROLLER.myTeamIndex >= CONTROLLER.TeamList.Length)
			{
				CONTROLLER.myTeamIndex = 0;
			}
			if (CONTROLLER.myTeamIndex == CONTROLLER.opponentTeamIndex)
			{
				CONTROLLER.myTeamIndex++;
				if (CONTROLLER.myTeamIndex >= CONTROLLER.TeamList.Length)
				{
					CONTROLLER.myTeamIndex = 0;
				}
			}
		}
		else if (CONTROLLER.myTeamIndex >= CONTROLLER.TeamList.Length)
		{
			CONTROLLER.myTeamIndex = 0;
		}
		if (CONTROLLER.PlayModeSelected == 8)
		{
			if (GameConstants.isWithAI == false)
			{
				//Photon Removal	photonView.RPC("RPC_ChangeMyTeamIndex", RpcTarget.OthersBuffered, CONTROLLER.myTeamIndex);
			}
			if (GameConstants.isWithAI == false)
			{
				//Photon Removal       photonView.RPC("RPC_ChangeMyTeamIndex", RpcTarget.OthersBuffered, CONTROLLER.myTeamIndex);
				CricketNetworkManager.instance.CmdChangeMyTeamIndex(staticVariables.UserProfiledata.user._id, CONTROLLER.myTeamIndex);
			}


		}
	}

	public void oppTeamSelectionLeftArrow()
	{
		if (!EnsureTeamListReady()) return;
		foreach (var item in CONTROLLER.TeamList)
		{
			//print("Team  Name:" + item.teamName + " ID:" + item.teamId);
		}

		if (CONTROLLER.PlayModeSelected == 8)
		{
			return;
		}
		CONTROLLER.opponentTeamIndex--;
		oppTeamLeftArrowSelected();
		if (CONTROLLER.PlayModeSelected == 7 && (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "KEN" || CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "NED" || CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "SCO" || CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "UAE"))
		{
			oppTeamSelectionLeftArrow();
		}
		else if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5 || CONTROLLER.PlayModeSelected == 7)
		{
			SetQuickPlay();
		}
		else if (CONTROLLER.PlayModeSelected == 1)
		{
			SetTournamentPlay();
		}
	}

	public void oppTeamSelectionRightArrow()
	{
		if (!EnsureTeamListReady()) return;
		if (CONTROLLER.PlayModeSelected == 8)
		{
			return;
		}
		CONTROLLER.opponentTeamIndex++;
		oppTeamRightArrowSelected();
		if (CONTROLLER.PlayModeSelected == 7 && (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "KEN" || CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "NED" || CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "SCO" || CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "UAE"))
		{
			oppTeamSelectionRightArrow();
		}
		else if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5 || CONTROLLER.PlayModeSelected == 7)
		{
			SetQuickPlay();
		}
		else if (CONTROLLER.PlayModeSelected == 1)
		{
			SetTournamentPlay();
		}
	}

	private void oppTeamLeftArrowSelected()
	{
		if (CONTROLLER.myTeamIndex == CONTROLLER.opponentTeamIndex)
		{
			CONTROLLER.opponentTeamIndex--;
		}
		if (CONTROLLER.opponentTeamIndex < 0)
		{
			CONTROLLER.opponentTeamIndex = CONTROLLER.TeamList.Length - 1;
			if (CONTROLLER.myTeamIndex == CONTROLLER.opponentTeamIndex)
			{
				CONTROLLER.opponentTeamIndex--;
			}
		}
	}

	private void oppTeamRightArrowSelected()
	{
		if (CONTROLLER.opponentTeamIndex >= CONTROLLER.TeamList.Length)
		{
			CONTROLLER.opponentTeamIndex = 0;
		}
		if (CONTROLLER.myTeamIndex == CONTROLLER.opponentTeamIndex)
		{
			CONTROLLER.opponentTeamIndex++;
			if (CONTROLLER.opponentTeamIndex >= CONTROLLER.TeamList.Length)
			{
				CONTROLLER.opponentTeamIndex = 0;
			}
		}
	}

	public void SetQuickPlay()
	{
		if (CONTROLLER.TeamList == null) return; // guard: TeamList not yet populated
		CONTROLLER.TeamList.Length.Show();
		if (CONTROLLER.myTeamIndex >= 0 && CONTROLLER.myTeamIndex < CONTROLLER.TeamList.Length)
		{
			Text sOMyTeamName = SOMyTeamName;
			string text = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].teamName.ToUpper();
			MyTeamName.text = text;
			sOMyTeamName.text = text;
			//Debug.Log("Started :" + SOMyTeamName.text + " _Index :" + CONTROLLER.myTeamIndex);
			if (LocalizationData.localizationInstance.referenceList.Contains(SOMyTeamName.text.ToUpper()))
			{
				//Debug.Log("entered");
				int num = LocalizationData.localizationInstance.referenceList.IndexOf(SOMyTeamName.text);
				Text sOMyTeamName2 = SOMyTeamName;
				text = LocalizationData.localizationInstance.getText(num);
				MyTeamName.text = text;
				sOMyTeamName2.text = text;
				//Debug.Log(num + " " + SOMyTeamName.text);
			}
			string abbrevation = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation;
			Image sOMyTeamFlag = SOMyTeamFlag;
			Sprite sprite = Singleton<FlagHolder>.instance.searchFlagByName(abbrevation);
			MyTeamFlag.sprite = sprite;
			sOMyTeamFlag.sprite = sprite;
		}
		if (CONTROLLER.opponentTeamIndex >= 0 && CONTROLLER.opponentTeamIndex < CONTROLLER.TeamList.Length)
		{
			Text sOOppTeamName = SOOppTeamName;
			string text = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].teamName.ToUpper();
			OppTeamName.text = text;
			sOOppTeamName.text = text;
			//Debug.Log("Started1 :" + SOOppTeamName.text);
			if (LocalizationData.localizationInstance.referenceList.Contains(SOOppTeamName.text.ToUpper()))
			{
				//Debug.Log("entered1");
				int num2 = LocalizationData.localizationInstance.referenceList.IndexOf(SOOppTeamName.text);
				Text sOOppTeamName2 = SOOppTeamName;
				text = LocalizationData.localizationInstance.getText(num2);
				OppTeamName.text = text;
				sOOppTeamName2.text = text;
				//Debug.Log(num2 + " " + SOOppTeamName.text);
			}
			string abbrevation2 = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation;
			Image sOOppTeamFlag = SOOppTeamFlag;
			Sprite sprite = Singleton<FlagHolder>.instance.searchFlagByName(abbrevation2);
			OppTeamFlag.sprite = sprite;
			sOOppTeamFlag.sprite = sprite;
		}
	}

	public void SetTournamentPlay()
	{
		if (CONTROLLER.myTeamIndex >= 0 && CONTROLLER.myTeamIndex < CONTROLLER.TeamList.Length)
		{
			MyTeamName2.text = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].teamName.ToUpper();
			if (LocalizationData.localizationInstance.referenceList.Contains(MyTeamName2.text.ToUpper()))
			{
				int index = LocalizationData.localizationInstance.referenceList.IndexOf(MyTeamName2.text);
				MyTeamName2.text = LocalizationData.localizationInstance.getText(index);
			}
			string abbrevation = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation;
			MyTeamFlag2.sprite = Singleton<FlagHolder>.instance.searchFlagByName(abbrevation);
		}
	}

	private void setDifficulty()
	{

		saveDifficulty();
	}

	private void SendFirebaseOvers()
	{

	}

	private void saveDifficulty()
	{
		if (CONTROLLER.PlayModeSelected == 0)
		{
			ObscuredPrefs.SetString("exdiff", CONTROLLER.difficultyMode);
		}

	}



	public void setTeams()
	{
		if (CONTROLLER.PlayModeSelected == 0)
		{
			string empty = string.Empty;
			empty = empty + CONTROLLER.myTeamIndex + "|";
			empty += CONTROLLER.opponentTeamIndex;
			ObscuredPrefs.SetString("ETM", empty);
		}

	}



	public void selectTopTeams()
	{

	}

	private void selectGroup()
	{
	}

	public void ChooseOtherLeague(int index)
	{
		switch (index)
		{
			case 0:
				XMLReader.LoadTeams(1);
				break;
			case 1:
				XMLReader.LoadTeams(0);
				break;
		}
	}

	//Photon Removal [PunRPC]
	public void RPC_Ready()
	{
		CONTROLLER.READY++;
		if (CONTROLLER.READY >= 2)
		{
			if (GameConstants.isWithAI == false)
			{
				//Photon Removal       photonView.RPC("RPC_ChangeMyTeamIndex", RpcTarget.OthersBuffered, CONTROLLER.myTeamIndex);
				CricketNetworkManager.instance.CmdChangeMyTeamIndex(staticVariables.UserProfiledata.user._id, CONTROLLER.myTeamIndex);
			}
			PaidContinue();
		}
	}

	private void Ready()
	{
		if (GameConstants.isWithAI == false)
		{
			// photonView.RPC("RPC_Ready", RpcTarget.AllBuffered);
			CricketNetworkManager.instance.CmdReady();
		}



	}


	public void Continue()
	{
		// Continue → PaidContinue → SquadPageTWO.SetSquadPage all index into
		// CONTROLLER.TeamList. If the user never touched the team arrows (e.g.,
		// kept the default team) TeamList may still be unpopulated when Cricket
		// is launched without its Preloader in the integrated app. Ensure it's
		// built before proceeding so the squad page doesn't NRE.
		if (!EnsureTeamListReady()) return;

		if (CONTROLLER.PlayModeSelected == 8)
		{
			NextBtm.interactable = false;
			if (GameConstants.isWithAI == false)
			{
				//    photonView.RPC("RPC_Ready", RpcTarget.AllBuffered);
				CricketNetworkManager.instance.CmdReady();
			}
			if (Timer.activeInHierarchy)
			{
				Timer.SetActive(false);
			}


			READY = true;
			return;
		}

		if (CONTROLLER.PlayModeSelected == 4)
		{

		}

		else
		{
			PaidContinue();
		}
	}

	//Photon Removal	[PunRPC]
	public void RPC_ValidateTeamTimer(float Seconds)
	{
		if (!Singleton<SquadPageTWO>.instance.Timer.activeInHierarchy)
		{
			Singleton<SquadPageTWO>.instance.ValidateTeamTimer(Seconds);
		}
		else
		{
		}
	}

	public void OnPlayerDisconnect()
	{
		if (Timer.activeInHierarchy)
		{
			s.Pause();
		}
	}

	public void OnPlayerReConnect()
	{
		if (Timer.activeInHierarchy)
		{
			s.Play();
		}
		else
		{
			PaidContinue();
		}
	}

	public void PaidContinue()
	{
		if (CONTROLLER.PlayModeSelected == 8 && Timer.activeInHierarchy)
		{
			Timer.SetActive(false);
		}

		SendFirebaseOvers();
		if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
		{
			oversText = "QPOvers";
			Singleton<SquadPageTWO>.instance.showMe(1);
			if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER)
			{
				if (GameConstants.isWithAI == false)
				{
					//Photon Removal      photonView.RPC("RPC_ValidateTeamTimer", RpcTarget.AllBuffered, 7f);

					CricketNetworkManager.instance.CmdValidateTeamTimer(7f);
				}


			}
		}

		hideMe();
		CONTROLLER.oversSelectedIndex = ObscuredPrefs.GetInt(oversText);
		setTeams();
	}



	public void ReselectOver()
	{
		Holder.SetActive(value: false);
		Singleton<ReselectOvers>.instance.ShowMe();
	}

	public void Back()
	{
		if (CONTROLLER.PlayModeSelected == 8)
		{
			return;
		}
		if (CONTROLLER.PlayModeSelected < 4)
		{
			Singleton<EntryFeesAndRewards>.instance.ShowMe();
		}
		else
		{
			Singleton<GameModeTWO>.instance.showMe();
			CONTROLLER.pageName = "landingPage";
		}
		hideMe();
		setTeams();
	}

	public void TeamSelectionTimer(float Seconds)
	{
		secCount = Seconds;
		timerImage.fillAmount = 1f;
		s = DOTween.Sequence();
		TweenCallback callback = delegate
		{
			if (!Timer.activeInHierarchy)
			{
				s.Kill();
			}
			SetSecond();
		};
		s.Append(timerImage.DOFillAmount(0f, 5f));
		for (int i = 0; i < Seconds; i++)
		{
			s.InsertCallback(i, callback);
		}
		s.InsertCallback(Seconds, Ready);
	}

	public void SetSecond()
	{
		timerText.text = secCount.ToString();
		secCount--;

	}

	//Photon Removal	[PunRPC]
	public void RPC_SetTeamSelectionTimer(float Seconds)
	{
		Timer.SetActive(true);
		TeamSelectionTimer(Seconds);
	}

	public void showMe()
	{
		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER)
		{
			if (GameConstants.isWithAI == false)
			{
				//Photon Removal       photonView.RPC("RPC_SetTeamSelectionTimer", RpcTarget.AllBuffered, 15f);
				CricketNetworkManager.instance.CmdSetTeamSelectionTimer(15f);
			}
		}
		if (CONTROLLER.PlayModeSelected == 8)
		{
			//Photon Removal	if (PhotonNetwork.IsConnected)
			{
				//Photon Removal       photonView.RPC("RPC_ChangeMyTeamIndex", RpcTarget.OthersBuffered, CONTROLLER.myTeamIndex);
			}
			if (GameConstants.isWithAI == false)
			{
				CricketNetworkManager.instance.CmdChangeMyTeamIndex(staticVariables.UserProfiledata.user._id, CONTROLLER.myTeamIndex);
			}
		}
		//Singleton<NavigationBack>.instance.deviceBack = Back;
		CONTROLLER.GameStartsFromSave = false;
		CONTROLLER.pageName = "teamSelection";
		CONTROLLER.Overs[0] = 3;
		CONTROLLER.Overs[1] = 5;
		CONTROLLER.Overs[2] = 10;
		CONTROLLER.Overs[3] = 20;
		CONTROLLER.Overs[4] = 30;
		CONTROLLER.Overs[5] = 50;
		CONTROLLER.Overs[6] = 15;
		CONTROLLER.Overs[7] = 30;
		CONTROLLER.Overs[8] = 60;
		CONTROLLER.Overs[9] = 90;
		Singleton<TeamSelectionPanelTransition>.instance.panelTransition();
		if (CONTROLLER.PlayModeSelected == 0)
		{
			if (ObscuredPrefs.HasKey("exdiff"))
			{
				CONTROLLER.difficultyMode = "easy";
			}
		}

		if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 7)
		{
			panel1.SetActive(value: true);
			panel2.SetActive(value: false);
			SOPanel.SetActive(value: false);
		}

		if (CONTROLLER.PlayModeSelected == 0)
		{
			if (ObscuredPrefs.HasKey("ETM"))
			{
				string @string = ObscuredPrefs.GetString("ETM");
				string[] array = @string.Split("|"[0]);
				CONTROLLER.myTeamIndex = int.Parse(array[0]);
				CONTROLLER.opponentTeamIndex = int.Parse(array[1]);
			}
			if (CONTROLLER.myTeamIndex == CONTROLLER.opponentTeamIndex)
			{
				if (CONTROLLER.opponentTeamIndex < CONTROLLER.TeamList.Length && CONTROLLER.opponentTeamIndex != 0)
				{
					CONTROLLER.opponentTeamIndex--;
				}
				else if (CONTROLLER.opponentTeamIndex == 0)
				{
					CONTROLLER.opponentTeamIndex++;
				}
			}
			SetQuickPlay();
		}

		if (CONTROLLER.PlayModeSelected == 4)
		{

		}
		else
		{
			outerPanel.SetActive(value: true);
			SOPanel.SetActive(value: false);
		}
		CONTROLLER.difficultyMode = "easy";
		setDifficulty();
		Holder.SetActive(value: true);
		CONTROLLER.CurrentMenu = "teamselection";
		CONTROLLER.menuTitle = "TEAM SELECTION";
		Singleton<GameModeTWO>.instance.updateTitle(_modeSelected: true);
	}

	public void hideMe()
	{
		Holder.SetActive(value: false);
	}
}