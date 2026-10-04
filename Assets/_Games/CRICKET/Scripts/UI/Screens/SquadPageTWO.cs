using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class SquadPageTWO : Singleton<SquadPageTWO>
{
	public GameObject Holder;

	public GameObject team2Panel;

	public GameObject entryFeePopup;

	public Image WarningToss;

	public Text MyTeamName;

	public Text OppTeamName;

	public Image MyTeamFlag;

	public Image OppTeamFlag;

	public GameObject loadingText;

	public Text[] team1playerName;

	public Text[] team2playerName;

	public Image[] team1playerType;

	public Image[] team2playerType;

	public Sprite[] PlayerTypeImage;

	public bool BackKeyEnable;

	private string teamToEdit = "myteam";

	private string[] paidKey = new string[8]
	{
		"QPPaid",
		"T20Paid",
		"NPLPaid",
		"WCPaid",
		string.Empty,
		string.Empty,
		string.Empty,
		"TMOvers"
	};

    //Multiplayer

    [SerializeField] private GameObject Bottom;

    private float secCount;

    [SerializeField] private Image timerImage;

    [SerializeField] private Text timerText;

    [SerializeField] public GameObject Timer;

    protected void Start()
	{
		hideMe();
	}

	public void showMe(int teamIndex)
	{
		if(CONTROLLER.PlayModeSelected == 8)
		{
			Bottom.SetActive(false);
		}
		//Singleton<NavigationBack>.instance.deviceBack = Back;
		if (CONTROLLER.PlayModeSelected > 3 && CONTROLLER.PlayModeSelected != 7 && CONTROLLER.PlayModeSelected !=8)
		{
			team2Panel.SetActive(value: false);
		}
		else
		{
			team2Panel.SetActive(value: true);
		}
		CONTROLLER.pageName = "squads";
		CONTROLLER.menuTitle = "PLAYER LIST";
		Singleton<GameModeTWO>.instance.updateTitle(_modeSelected: true);
		SetSquadPage();
		Holder.SetActive(value: true);
		Singleton<PlayerNamePanelTransition>.instance.panelTransition();
		BackKeyEnable = true;
	}

    public void ValidateTeamTimer(float Seconds)
    {
		Bottom.SetActive(false);
		Timer.SetActive(true);
        secCount = Seconds;
        timerImage.fillAmount = 1f;
        //aiReviewstatus.text = LocalizationData.instance.getText(513);
        Sequence s = DOTween.Sequence();
        TweenCallback callback = delegate
        {
            if (!Holder.activeInHierarchy)
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
        //s.InsertCallback(6f, NoBtnClicked);
        s.InsertCallback(Seconds, ValidateTeam);
    }

	public void OnPlayerDisconnect()
	{
		if (Timer.activeInHierarchy)
		{

		}
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

    public void hideMe()
	{
		BackKeyEnable = false;
		Holder.SetActive(value: false);
	}

	/// <summary>
	/// Ensures CONTROLLER.TeamList is populated before it is indexed. Lazily
	/// triggers the prefs loaders (ParseXML is synchronous) if the Cricket
	/// Preloader was skipped in the integrated app. Returns false if it still
	/// couldn't be populated, so the caller can no-op instead of crashing.
	/// </summary>
	private bool EnsureTeamListReady()
	{
		if (CONTROLLER.TeamList != null && CONTROLLER.TeamList.Length > 0)
			return true;

		try
		{
			// Default team list ships in Resources/WorldCupSchedule.bytes
			// (cricket > schedule > League > team — what XMLReader.ParseXML expects).
			// User-edited squads persist in the "teamlist" PlayerPref.
			if (PlayerPrefs.HasKey("teamlist"))
			{
				XMLReader.ParseXML(PlayerPrefs.GetString("teamlist"));
			}
			else
			{
				TextAsset xml = Resources.Load<TextAsset>("WorldCupSchedule");
				if (xml != null) XMLReader.ParseXML(xml.text);
			}

			if ((CONTROLLER.TeamList == null || CONTROLLER.TeamList.Length == 0)
				&& LoadPlayerPrefsTWO.instance != null)
				LoadPlayerPrefsTWO.instance.GetTeamList();
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[SquadPageTWO] TeamList lazy-load failed: " + ex.Message);
		}

		return CONTROLLER.TeamList != null && CONTROLLER.TeamList.Length > 0;
	}

	public void SetSquadPage()
	{
		// Defensive: TeamList must be populated before indexing. In the integrated
		// multi-game app the Cricket Preloader (which parses the team XML) can be
		// skipped, leaving TeamList null. SetSquadPage is also reachable via the
		// reconnect path (OnPlayerReConnect → PaidContinue), so guard here too.
		if (!EnsureTeamListReady())
		{
			Debug.LogWarning("[SquadPageTWO] TeamList not ready — aborting SetSquadPage to avoid NRE.");
			return;
		}
		teamToEdit = "myteam";
		if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation.ToUpper() == "UAE")
		{
			MyTeamName.text = LocalizationData.localizationInstance.getText(201);
		}
		else
		{
			MyTeamName.text = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].teamName.ToUpper();
			int index = LocalizationData.localizationInstance.referenceList.IndexOf(MyTeamName.text.ToUpper());
			MyTeamName.text = LocalizationData.localizationInstance.getText(index);
		}
		if (CONTROLLER.PlayModeSelected < 4 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation.ToUpper() == "UAE")
			{
				OppTeamName.text = LocalizationData.localizationInstance.getText(201);
			}
			else
			{
				OppTeamName.text = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].teamName.ToUpper();
				int index = LocalizationData.localizationInstance.referenceList.IndexOf(OppTeamName.text.ToUpper());
				OppTeamName.text = LocalizationData.localizationInstance.getText(index);
			}
		}
		string abbrevation = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation;
		MyTeamFlag.sprite = Singleton<FlagHolder>.instance.searchFlagByName(abbrevation);
		if (CONTROLLER.PlayModeSelected < 4 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			abbrevation = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation;
			OppTeamFlag.sprite = Singleton<FlagHolder>.instance.searchFlagByName(abbrevation);
		}
		for (int i = 0; i < team1playerName.Length; i++)
		{
			string playerName = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].PlayerName;
			if (playerName.Length > 12)
			{
				playerName = playerName.Substring(0, 10);
				CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].PlayerName = playerName;
			}
			team1playerName[i].text = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].PlayerName.ToUpper();
			if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].isCaptain)
			{
				team1playerName[i].text += " (C)";
			}
			if (i <= 6)
			{
				if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BowlerList.bowlingRank != string.Empty)
				{
					team1playerType[i].sprite = PlayerTypeImage[2];
				}
				else
				{
					team1playerType[i].sprite = PlayerTypeImage[0];
				}
			}
			else if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].BowlerList.bowlingRank != string.Empty)
			{
				team1playerType[i].sprite = PlayerTypeImage[1];
			}
			else
			{
				team1playerType[i].sprite = PlayerTypeImage[0];
			}
			if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[i].isKeeper)
			{
				team1playerType[i].sprite = PlayerTypeImage[3];
			}
		}
		if (CONTROLLER.PlayModeSelected >= 4 && CONTROLLER.PlayModeSelected != 7 && CONTROLLER.PlayModeSelected != 8)
		{
			return;
		}
		for (int i = 0; i < team2playerName.Length; i++)
		{
			string playerName2 = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].PlayerName;
			if (playerName2.Length > 12)
			{
				playerName2 = playerName2.Substring(0, 10);
				CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].PlayerName = playerName2;
			}
			team2playerName[i].text = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].PlayerName.ToUpper();
			if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].isCaptain)
			{
				team2playerName[i].text += " (C)";
			}
			if (i <= 6)
			{
				if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.bowlingRank != string.Empty)
				{
					team2playerType[i].sprite = PlayerTypeImage[2];
				}
				else
				{
					team2playerType[i].sprite = PlayerTypeImage[0];
				}
				if (!(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.bowlingRank != string.Empty))
				{
				}
			}
			else if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].BowlerList.bowlingRank != string.Empty)
			{
				team2playerType[i].sprite = PlayerTypeImage[1];
			}
			else
			{
				team2playerType[i].sprite = PlayerTypeImage[0];
			}
			if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[i].isKeeper)
			{
				team2playerType[i].sprite = PlayerTypeImage[3];
			}
		}
	}

	public void OnClickContinueButton()
	{
		if (CONTROLLER.PlayModeSelected < 4 && CONTROLLER.PlayModeSelected != 0)
		{
			//if ((CONTROLLER.PlayModeSelected == 2 && CONTROLLER.NPLIndiaTournamentStage > 0) || (CONTROLLER.PlayModeSelected == 3 && CONTROLLER.WCTournamentStage > 0))
			//{
			//	Continue();
			//}
			//else
			//{
			//	Singleton<EntryFeeConfirmation>.instance.ShowMe();
			//}
		}
		else if (CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
		{
			Continue();
		}
		//else if (CONTROLLER.PlayModeSelected == 4)
		//{
		//	Singleton<EntryFeeConfirmation>.instance.ShowMe();
		//}
		//else if (CONTROLLER.PlayModeSelected == 5)
		//{
		//	Singleton<EntryFeeConfirmation>.instance.ShowMe();
		//}
	}

	

	public void Continue()
	{
		//if (CONTROLLER.PlayModeSelected < 4 && CONTROLLER.PlayModeSelected != 0)
		//{
		//	if ((CONTROLLER.PlayModeSelected != 2 || CONTROLLER.NPLIndiaTournamentStage <= 0) && (CONTROLLER.PlayModeSelected != 3 || CONTROLLER.WCTournamentStage <= 0))
		//	{
		//		MakePayment(CONTROLLER.PlayModeSelected);
		//	}
		//}
		//else if (CONTROLLER.PlayModeSelected == 7)
		//{
		//	PaymentDetails.InitPaymentValues();
		//	int num = Singleton<PaymentProcess>.instance.GenerateAmount();
		//	//SavePlayerPrefs.SaveUserTickets(-num, num, 0);
		//	ObscuredPrefs.SetInt(CONTROLLER.PlayModeSelected + "Refund", num);
		//}
		CONTROLLER.GameStartsFromSave = false;
		SendToFirebase();
		ConfigureGameBeforeToss();
	}

	public void CloseEntryFeePopup()
	{
		Singleton<EntryFeeConfirmation>.instance.HideMe();
		CONTROLLER.pageName = "squads";
	}

	private void SendToFirebase()
	{
		//if (CONTROLLER.PlayModeSelected == 1)
		//{
		//	Singleton<Firebase_Events>.instance.Firebase_T20_MatchPro();
		//}
		//else if (CONTROLLER.PlayModeSelected == 3)
		//{
		//	Singleton<Firebase_Events>.instance.Firebase_WC_MatchPro();
		//}
		//else if (CONTROLLER.PlayModeSelected == 2)
		//{
		//	Singleton<Firebase_Events>.instance.Firebase_PRL_MatchPro();
		//}
		//else if (CONTROLLER.PlayModeSelected == 7)
		//{
		//	Singleton<Firebase_Events>.instance.Firebase_TC_MatchPro();
		//}
	}

	public void ValidateTeam()
	{
		int num = 0;
		int captainIndex = 0;
		int num2 = 0;
		for (num = 0; num < team1playerName.Length; num++)
		{
			if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[num].isCaptain)
			{
				captainIndex = num;
				num2++;
				if (num2 >= 2)
				{
					ShowError(1);
					return;
				}
			}
		}
		if (num2 == 0)
		{
			ShowError(2);
			return;
		}
		CONTROLLER.TeamList[CONTROLLER.myTeamIndex].CaptainIndex = captainIndex;
		if (CONTROLLER.PlayModeSelected < 4 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			num2 = 0;
			for (num = 0; num < team2playerName.Length; num++)
			{
				if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[num].isCaptain)
				{
					captainIndex = num;
					num2++;
					if (num2 >= 2)
					{
						ShowError(3);
						return;
					}
				}
			}
			if (num2 == 0)
			{
				ShowError(4);
				return;
			}
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].CaptainIndex = captainIndex;
		}
		int keeperIndex = 0;
		int num3 = 0;
		for (num = 0; num < team1playerName.Length; num++)
		{
			if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[num].isKeeper)
			{
				keeperIndex = num;
				num3++;
				if (num3 >= 2)
				{
					ShowError(5);
					return;
				}
			}
		}
		if (num3 == 0)
		{
			ShowError(6);
			return;
		}
		CONTROLLER.TeamList[CONTROLLER.myTeamIndex].KeeperIndex = keeperIndex;
		if (CONTROLLER.PlayModeSelected < 4 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			num3 = 0;
			for (num = 0; num < team2playerName.Length; num++)
			{
				if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[num].isKeeper)
				{
					keeperIndex = num;
					num3++;
					if (num3 >= 2)
					{
						ShowError(7);
						return;
					}
				}
			}
			if (num3 == 0)
			{
				ShowError(8);
				return;
			}
			CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].KeeperIndex = keeperIndex;
		}
		int num4 = 0;
		for (num = 0; num < team1playerName.Length; num++)
		{
			if (CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[num].BowlerList.bowlingRank != string.Empty)
			{
				num4++;
				CONTROLLER.TeamList[CONTROLLER.myTeamIndex].PlayerList[num].BowlerList.bowlingOrder = num4;
			}
		}
		if (num4 < 5 || num4 > 7)
		{
			ShowError(9);
			return;
		}
		if (CONTROLLER.PlayModeSelected < 4 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			num4 = 0;
			for (num = 0; num < team2playerName.Length; num++)
			{
				if (CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[num].BowlerList.bowlingRank != string.Empty)
				{
					num4++;
					CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].PlayerList[num].BowlerList.bowlingOrder = num4;
				}
			}
			if (num4 < 5 || num4 > 7)
			{
				ShowError(10);
				return;
			}
		}
		ShowError(0);
	}

	private void ShowError(int id)
	{
		if (id > 0)
		{
			string str = string.Empty;
			switch (id)
			{
			case 1:
				str = LocalizationData.localizationInstance.getText(644);
				break;
			case 2:
				str = LocalizationData.localizationInstance.getText(141);
				break;
			case 3:
				str = LocalizationData.localizationInstance.getText(142);
				break;
			case 4:
				str = LocalizationData.localizationInstance.getText(143);
				break;
			case 5:
				str = LocalizationData.localizationInstance.getText(144);
				break;
			case 6:
				str = LocalizationData.localizationInstance.getText(145);
				break;
			case 7:
				str = LocalizationData.localizationInstance.getText(146);
				break;
			case 8:
				str = LocalizationData.localizationInstance.getText(147);
				break;
			case 9:
				str = LocalizationData.localizationInstance.getText(148);
				break;
			case 10:
				str = LocalizationData.localizationInstance.getText(645);
				break;
			}
			Singleton<ErrorPopupTWO>.instance.showMe(str);
		}
		else
		{
			OnClickContinueButton();
		}
	}

	private void ConfigureGameBeforeToss()
	{
		////Debug.Log("+OPPONENT TEAM " + CONTROLLER.opponentTeamIndex);
        ////Debug.Log("+MY TEAM " + CONTROLLER.myTeamIndex);
		if(CONTROLLER.PlayModeSelected == 8)
		{
			Timer.SetActive(false);
		}
        int num = int.Parse(CONTROLLER.TeamList[CONTROLLER.myTeamIndex].rank);
		int num2 = int.Parse(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].rank);
		if (num <= 8 && num2 <= 8)
		{
			CONTROLLER.GoodMatch = true;
		}
		else
		{
			CONTROLLER.GoodMatch = false;
		}
		Singleton<NavigationBack>.instance.deviceBack = null;
		Singleton<LoadingPanelTransition>.instance.PanelTransition();
		hideMe();
		Singleton<GameModeTWO>.instance.hideMe();
		Singleton<EntryFeeConfirmation>.instance.holder.SetActive(value: false);
		CONTROLLER.pageName = string.Empty;
		SetTeamIndex();
		SetSquadPage();
		SavePlayerPrefs.SetTeamList();
		Invoke("GoNextPage", 1.5f);
	}

	private void GoNextPage()
	{
		if (CONTROLLER.PlayModeSelected < 4 || CONTROLLER.PlayModeSelected == 7 || CONTROLLER.PlayModeSelected == 8)
		{
			CricketNetworkManager.instance?.CmdChangeCurrentOpenPanel((int)PanelsInfo.Toss);
			Singleton<TossPageTWO>.instance.showMe();
			hideMe();
			Singleton<LoadingPanelTransition>.instance.HideMe();
			Bottom.SetActive(true);
			"yakam bura uoooooooooooo".Show();
		}
		else
		{
			CONTROLLER.SceneIsLoading = true;
			GetPlayerPrefs.instance.InitializeGame();
			Singleton<NavigationBack>.instance.deviceBack = null;
			Singleton<LoadingPanelTransition>.instance.PanelTransition1("Ground");
		}
	}

	private void SetTeamIndex()
	{
		if (CONTROLLER.currentInnings == 0)
		{
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
		else if (CONTROLLER.meFirstBatting == 1)
		{
			CONTROLLER.BattingTeamIndex = CONTROLLER.opponentTeamIndex;
			CONTROLLER.BowlingTeamIndex = CONTROLLER.myTeamIndex;
		}
		else
		{
			CONTROLLER.BattingTeamIndex = CONTROLLER.myTeamIndex;
			CONTROLLER.BowlingTeamIndex = CONTROLLER.opponentTeamIndex;
		}
	}

	

	public void Back()
	{
		if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 7 )
		{
			Singleton<TeamSelectionTWO>.instance.showMe();
		}
		hideMe();
	}
}
