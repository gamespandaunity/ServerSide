using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
 
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class TossPageTWO : Singleton<TossPageTWO>
{
    //Photon Removal public PhotonView photonView;

    public ScreenEntryAnimation entryAnim;

	public GameObject Holder;

	public GameObject SwipeUpPanel;

	public GameObject MakeYourCallPanel;

	public Text Tittle;

	public Text tossOutcome;

	public Text displayText1;

	public Text displayText2;

	public GameObject Tossinfo;

	public Text Button1Text;

	public Text Button2Text;

	public Button BatButton;

	public Button BowlButton;

	public Button BackButton;

	public Button continueButton;

	public GameObject Coin;

	private int random;

	public Image flag1;

	public Image flag2;

	public Transform flagEndPosition;

	private string coinStatus;

	private Vector3 flag1StartPos;

	private Vector3 flag1EndPos;

	private Vector3 flag2StartPos;

	private Vector3 flag2EndPos;

	private Vector3 finalPos;

	private string oversText;

	public Button RV_Button;

	public GameObject RV_Panel;

	private Vector2 firstPressPos;

	private Vector2 secondPressPos;

	private Vector2 currentSwipe;

	private bool firstTimeToss;

	public string userSelectedTo = string.Empty;

	private int angle;

	public Image[] sideArrow;

    //Multiplayer

    private float secCount;

    [SerializeField] private Image timerImage;

    [SerializeField] private Text timerText;

    [SerializeField] private GameObject Timer;

	DG.Tweening.Sequence s;

    protected void Start()
	{
		MakeYourCallPanel.GetComponent<Image>().DOFade(0f, 0.5f).SetLoops(-1, LoopType.Yoyo)
			.SetUpdate(isIndependentUpdate: true);
		firstTimeToss = true;
		flag1StartPos = new Vector3(-135f, 33.6f, 0f);
		flag2StartPos = new Vector3(135f, 33.6f, 0f);
		flag1EndPos = new Vector3(-350f, 0f, 0f);
		flag2EndPos = new Vector3(350f, 0f, 0f);
		finalPos = flagEndPosition.localPosition;
		MakeYourCallPanel.SetActive(value: false);
		ArrowAnimation();
		//if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER)
		//{
		//	SwipeUpPanel.SetActive(false);
		//}
		//else
		//{
		//	SwipeUpPanel.SetActive(true);
		//}
	}

	public void ArrowAnimation()
	{
		ResetAnim1();
		DG.Tweening.Sequence sequence = DOTween.Sequence();
		sequence.Insert(0f, sideArrow[0].DOFade(1f, 0.3f));
		sequence.Insert(0.3f, sideArrow[1].DOFade(1f, 0.3f));
		sequence.Insert(0.6f, sideArrow[2].DOFade(1f, 0.3f));
		sequence.Insert(0.9f, sideArrow[0].DOFade(0f, 0.15f));
		sequence.Insert(1.05f, sideArrow[1].DOFade(0f, 0.15f));
		sequence.Insert(1.2f, sideArrow[2].DOFade(0f, 0.15f));
		sequence.SetLoops(-1);
	}

	public void ResetAnim1()
	{
		DG.Tweening.Sequence s = DOTween.Sequence();
		s.Insert(0f, sideArrow[0].DOFade(0f, 0f));
		s.Insert(0f, sideArrow[1].DOFade(0f, 0f));
		s.Insert(0f, sideArrow[2].DOFade(0f, 0f));
	}

	public void RVTossAgainAdCall()
	{
		ResetPosition();
		Singleton<Popups>.instance.HideMe();
		ResetAllAnimations();
		resetButtons();
		Coin.gameObject.transform.localPosition = new Vector3(628.3f, -428f, 743f);
		Coin.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
		RV_Button.gameObject.SetActive(value: false);
		continueButton.gameObject.SetActive(value: false);
		Tittle.gameObject.SetActive(value: true);
		Coin.SetActive(value: true);
		tossOutcome.gameObject.SetActive(value: true);
		CONTROLLER.pageName = "toss";
		flag1.gameObject.SetActive(value: true);
		RV_Panel.SetActive(value: false);
		flag2.gameObject.SetActive(value: true);
		firstTimeToss = true;
		SwipeUpPanel.SetActive(value: true);
		DeactivateButtons();
	}

	private void ResetPosition()
	{
		Tossinfo.SetActive(value: false);
		Tittle.gameObject.SetActive(value: false);
		continueButton.gameObject.SetActive(value: false);
		Coin.SetActive(value: false);
		tossOutcome.gameObject.SetActive(value: false);
		flag1.gameObject.SetActive(value: false);
		flag2.gameObject.SetActive(value: false);
	}

	public void RVButtonClicked()
	{
		RVSelected(1);
	}

	public void RVSelected(int output)
	{
		//switch (output)
		//{
		//case 1:
		//	FirebaseAnalyticsManager.instance.logEvent("RewardedVideoAds", new string[2] { "Ad_Network_Rewarded_Actions", "Spot_Generated_TossAgain" });
		//	Singleton<AdIntegrate>.instance.showRewardedVideo(2);
		//	break;
		//case 0:
		//	continueButton.gameObject.SetActive(value: true);
		//	if (Singleton<AdIntegrate>.instance.isRewardedVideoAvailable)
		//	{
		//		RV_Button.gameObject.SetActive(value: true);
		//	}
		//	else
		//	{
		//		RV_Button.gameObject.SetActive(value: false);
		//		Singleton<AdIntegrate>.instance.requestRewardedVideo();
		//	}
		//	Singleton<Popups>.instance.HideMe();
		//	Tossinfo.SetActive(value: true);
		//	continueButton.gameObject.SetActive(value: true);
		//	Tittle.gameObject.SetActive(value: true);
		//	Coin.SetActive(value: true);
		//	tossOutcome.gameObject.SetActive(value: true);
		//	CONTROLLER.pageName = "toss";
		//	flag1.gameObject.SetActive(value: true);
		//	RV_Panel.SetActive(value: false);
		//	flag2.gameObject.SetActive(value: true);
		//	break;
		//}
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
    }

    public void HeadsButton()
	{
        ////Debug.Log("BATINNGG LE LIYAA H");

        if (userSelectedTo == string.Empty)
		{
            	if(CONTROLLER.PlayModeSelected == 8 && !(GameConstants.isWithAI == false))
            {
                return;
			}
            if (Timer.activeInHierarchy)
            {
                Timer.SetActive(false);
            }
            deactivateBtn("heads");
			hideMePanelTransition();
			CONTROLLER.pageName = string.Empty;
            if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER)
            {
                 if (GameConstants.isWithAI == false)
                {
                    //Photon Removal       photonView.RPC("RPC_TossDecision", RpcTarget.OthersBuffered, false);
					CricketNetworkManager.instance.CmdTossDecision(staticVariables.UserProfiledata.user._id,false);
                }

               
            }
        }
		else
		{
            if(CONTROLLER.PlayModeSelected == 8 && !(GameConstants.isWithAI == false))
            {
                return;
            }
            if (Timer.activeInHierarchy)
            {
                Timer.SetActive(false);
            }
            if (CONTROLLER.PlayModeSelected == 8)
            {
                if (GameConstants.isWithAI == false)
                {
                    //Photon Removal      photonView.RPC("RPC_ChoseTo", RpcTarget.OthersBuffered, true);
                CricketNetworkManager.instance.CmdChoseTo(staticVariables.UserProfiledata.user._id,true);
				}



            }
            Coin.gameObject.transform.localPosition = new Vector3(628.3f, -428f, 743f);
			Coin.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
			Coin.SetActive(value: false);
			hideMe();
			CONTROLLER.meFirstBatting = 1;
			Continue();
            ////Debug.Log("BATINNGG LE LIYAA H");

        }

        MakeYourCallPanel.SetActive(value: false);
	}

    //Photon Removal	[PunRPC]
    public void RPC_TossDecision(bool IsItHeads)
	{
		if (IsItHeads)
		{
			if(coinStatus == "heads" && !Timer.activeInHierarchy)
			{
                activateBtn();
                Timer.gameObject.SetActive(true);
				TossTimer(7f);
            }
            deactivateBtn("heads");

		}
		else
		{
			if(coinStatus == "tail" && !Timer.activeInHierarchy)
			{
                activateBtn();
				Timer.gameObject.SetActive(true);
                TossTimer(7f);
            }
            deactivateBtn("tail");
		}
	}

	// Safe accessor for the opponent team's abbreviation. The toss bat/bowl
	// handlers (RPC_ChoseTo / ReCallChoseTo) index CONTROLLER.TeamList here; if
	// TeamList is null or opponentTeamIndex is out of range, the raw indexer threw
	// a NullReferenceException BEFORE Continue() was reached — so winning the toss
	// and tapping Bat/Bowl appeared to do nothing (the Ground scene never loaded).
	// Returning "" on bad state lets the decision flow finish and load the match.
	private string OpponentAbbrev()
	{
		var tl = CONTROLLER.TeamList;
		int idx = CONTROLLER.opponentTeamIndex;
		if (tl != null && idx >= 0 && idx < tl.Length && tl[idx] != null)
			return tl[idx].abbrevation;
		return "";
	}

    //Photon Removal [PunRPC]
    // How long the toss VERDICT ("<team> chose to bat/bowl") stays on screen before the game moves on.
    // The verdict text only finishes animating in at ~0.5s, so the old 1.5s left it readable for about a
    // second — players reported missing what the toss actually decided. Tester: "jab toss ka decision aaye
    // to screen ko wahan 5 seconds stay karna chahiye." Both clients run this dwell independently and it is
    // the same value on each, so the two sides still leave the toss together.
    private const float TOSS_VERDICT_DWELL = 5f;

    public void RPC_ChoseTo(bool ChoseBatting)
	{
		if (ChoseBatting)
		{
			CONTROLLER.meFirstBatting = 0;
            DOTween.Sequence().Insert(0.5f, Tossinfo.transform.DOScaleX(1f, 0.5f));
            DOTween.Sequence().Insert(0.5f, displayText1.transform.DOScaleX(1f, 0.5f));
            displayText1.text = LocalizationData.localizationInstance.getText(408);
            displayText2.text = " " + OpponentAbbrev() + " ";
            displayText1.text = ReplaceText(displayText1.text, string.Empty, LocalizationData.localizationInstance.getText(231));
			Invoke("Continue", TOSS_VERDICT_DWELL);
		}
		else
		{
			CONTROLLER.meFirstBatting = 1;
            DOTween.Sequence().Insert(0.5f, Tossinfo.transform.DOScaleX(1f, 0.5f));
            DOTween.Sequence().Insert(0.5f, displayText1.transform.DOScaleX(1f, 0.5f));
            displayText1.text = LocalizationData.localizationInstance.getText(408);
            displayText2.text = " " + OpponentAbbrev() + " ";
            displayText1.text = ReplaceText(displayText1.text, string.Empty, LocalizationData.localizationInstance.getText(232));
            Invoke("Continue", TOSS_VERDICT_DWELL);
		}
    }

	public void ReCallChoseTo(bool BattingFirst)
	{
        if (BattingFirst)
        {
			////Debug.Log("BATTING FIRST ME : ");
            CONTROLLER.meFirstBatting = 0;
            DOTween.Sequence().Insert(0.5f, Tossinfo.transform.DOScaleX(1f, 0.5f));
            DOTween.Sequence().Insert(0.5f, displayText1.transform.DOScaleX(1f, 0.5f));
            displayText1.text = LocalizationData.localizationInstance.getText(408);
            displayText2.text = " " + OpponentAbbrev() + " ";
            displayText1.text = ReplaceText(displayText1.text, string.Empty, LocalizationData.localizationInstance.getText(231));
            Invoke("Continue", TOSS_VERDICT_DWELL);
		}
        else
        {
            ////Debug.Log("BATTING FIRST NOT ME : ");

            CONTROLLER.meFirstBatting = 1;
            DOTween.Sequence().Insert(0.5f, Tossinfo.transform.DOScaleX(1f, 0.5f));
            DOTween.Sequence().Insert(0.5f, displayText1.transform.DOScaleX(1f, 0.5f));
            displayText1.text = LocalizationData.localizationInstance.getText(408);
            displayText2.text = " " + OpponentAbbrev() + " ";
            displayText1.text = ReplaceText(displayText1.text, string.Empty, LocalizationData.localizationInstance.getText(232));
            Invoke("Continue", TOSS_VERDICT_DWELL);
		}
    }

	public void TailButton()
	{
        ////Debug.Log("BATINNGG LE LIYAA H");

        if (userSelectedTo == string.Empty)
		{
 if (CONTROLLER.PlayModeSelected == 8 &&!(GameConstants.isWithAI == false))
            {
                return;
            }
            if (Timer.activeInHierarchy)
            {
                Timer.SetActive(false);
            }
            deactivateBtn("tail");
			hideMePanelTransition();
			CONTROLLER.pageName = string.Empty;
			if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER)
			{
                  if (GameConstants.isWithAI == false)
                {
                        // photonView.RPC("RPC_TossDecision", RpcTarget.OthersBuffered, true);
						 CricketNetworkManager.instance.CmdTossDecision(staticVariables.UserProfiledata.user._id,true);
                }



            }
        }
		else
		{
           if (CONTROLLER.PlayModeSelected == 8 &&  !(GameConstants.isWithAI == false))
            {
                return;
            }
            if (Timer.activeInHierarchy)
            {
                Timer.SetActive(false);
            }
            if (CONTROLLER.PlayModeSelected == 8)
			{
                //Photon Removal  if (PhotonNetwork.IsConnected)
                {
                    //Photon Removal      photonView.RPC("RPC_ChoseTo",RpcTarget.OthersBuffered, false);
                }
				if (GameConstants.isWithAI == false)
                {
                CricketNetworkManager.instance.CmdChoseTo(staticVariables.UserProfiledata.user._id,false);
				}

            }
            Coin.gameObject.transform.localPosition = new Vector3(628.3f, -428f, 743f);
			Coin.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
			Coin.SetActive(value: false);
			hideMe();
			CONTROLLER.meFirstBatting = 0;
			Continue();
			////Debug.Log("BATINNGG LE LIYAA H");
        }
        MakeYourCallPanel.SetActive(value: false);
	}

	private void deactivateBtn(string askedFor)
	{
		userSelectedTo = askedFor;
		showResult();
		BackButton.gameObject.SetActive(value: false);
		CONTROLLER.CurrentMenu = string.Empty;
	}

    //Photon Removal	[PunRPC]
    public void RPC_AfterUpSwipe()
	{
		////Debug.Log("SWIPeeD UUp");
		firstTimeToss = false;
		AfterUpSwipe();
	}

	private void CallSwipeUp()
	{
         if (GameConstants.isWithAI == false)
        {
            if (!firstTimeToss)
			{
				return;
			}
            //Photon Removal	photonView.RPC("RPC_AfterUpSwipe", RpcTarget.AllBuffered);
			CricketNetworkManager.instance.CmdAfterUpSwipe();
        }

       

    }

    public void TossTimer(float Seconds)
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
		if (!CONTROLLER.AMIOWNER && !BatButton.gameObject.activeInHierarchy)
		{
	        s.InsertCallback(Seconds+1, CallSwipeUp);
		}
		else if(CONTROLLER.AMIOWNER && BatButton.gameObject.activeInHierarchy)
		{
			s.InsertCallback(Seconds, HeadsButton);
		}
		else if(!CONTROLLER.AMIOWNER && BatButton.gameObject.activeInHierarchy)
		{
            BatButton.interactable = true;
            BowlButton.interactable = true;
            s.InsertCallback(Seconds, HeadsButton);
		}
	}

    public void SetSecond()
    {
        timerText.text = secCount.ToString();
        secCount--;
        if (CONTROLLER.AMIOWNER && BatButton.gameObject.activeInHierarchy && secCount == 0)
        {
			
        }
       
    }

    private void AfterUpSwipe()
	{
		DG.Tweening.Sequence s = DOTween.Sequence();
		s.Append(flag1.transform.parent.DOLocalMove(flag1EndPos, 0.3f));
		s.Insert(0f, flag2.transform.parent.DOLocalMove(flag2EndPos, 0.3f));
		s.Insert(0f, flag1.transform.parent.DOScale(Vector3.one * 1.5f, 0.3f));
		s.Insert(0f, flag2.transform.parent.DOScale(Vector3.one * 1.5f, 0.3f));
		BackButton.gameObject.SetActive(value: false);
		CONTROLLER.CurrentMenu = string.Empty;
		BatButton.image.DOFade(1f, 0.15f);
		BowlButton.image.DOFade(1f, 0.15f);
		Button1Text.DOFade(1f, 0.15f);
		Button2Text.DOFade(1f, 0.15f);
		Coin.gameObject.transform.localPosition = new Vector3(628.3f, -428f, 743f);
		Coin.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
		Coin.SetActive(value: true);
		SwipeUpPanel.SetActive(value: false);
		BackButton.gameObject.SetActive(value: false);
		CONTROLLER.CurrentMenu = string.Empty;
		Coin.transform.DOLocalMove(new Vector3(628.3f, -400f, 732f), 0.5f, snapping: true);
		Coin.transform.DORotate(new Vector3(3625f, 0f, 0f), 0.5f, RotateMode.FastBeyond360);
        if (!(CONTROLLER.PlayModeSelected == 8 && !CONTROLLER.AMIOWNER))
		{
            Invoke("ActivateButtons", 0.5f);
		}
		else if(CONTROLLER.PlayModeSelected == 8 && !CONTROLLER.AMIOWNER)
		{
			if (Timer.activeInHierarchy)
			{
				Timer.SetActive(false);
			}
		}
	}

	private void activateBtn()
	{
		Button1Text.text = LocalizationData.localizationInstance.getText(231);
		Button2Text.text = LocalizationData.localizationInstance.getText(232);
		if(CONTROLLER.PlayModeSelected == 8)
		{
            BatButton.gameObject.SetActive(true);
            BowlButton.gameObject.SetActive(true);
        }
    }

	public void resetButtons()
	{
		Tossinfo.SetActive(value: false);
		BatButton.gameObject.SetActive(value: true);
		BowlButton.gameObject.SetActive(value: true);
		Button1Text.text = LocalizationData.localizationInstance.getText(227);
		Button2Text.text = LocalizationData.localizationInstance.getText(228);
		userSelectedTo = string.Empty;
	}

	private void setTitle()
	{
		CONTROLLER.menuTitle = "TOSS";
		Singleton<GameModeTWO>.instance.updateTitle(_modeSelected: true);
		BackButton.gameObject.SetActive(value: false);
		continueButton.gameObject.SetActive(value: false);
		CONTROLLER.CurrentMenu = "toss";
	}

	public void showMe()
	{
		Singleton<NavigationBack>.instance.deviceBack = null;
		CONTROLLER.pageName = "toss";
		Invoke("showMeDelay", 0f);
	}

	public void showMeDelay()
	{
		Singleton<NavigationBack>.instance.deviceBack = null;
		CONTROLLER.pageName = "toss";
		flag1StartPos = new Vector3(-135f, 33.6f, 0f);
		flag2StartPos = new Vector3(135f, 33.6f, 0f);
		RV_Button.gameObject.SetActive(value: false);
		CONTROLLER.GameStartsFromSave = false;
		CONTROLLER.isFreeHitBall = false;
		AutoSave.DeleteFile();
		string abbrevation = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation;
		flag1.sprite = Singleton<FlagHolder>.instance.searchFlagByName(abbrevation);
		abbrevation = OpponentAbbrev();
		flag2.sprite = Singleton<FlagHolder>.instance.searchFlagByName(abbrevation);
		Coin.gameObject.transform.localPosition = new Vector3(628.3f, -428f, 743f);
		Coin.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
		Coin.SetActive(value: true);
		panelTransition();
		ResetAllAnimations();
		resetButtons();
		setTitle();
		DeactivateButtons();
		Holder.SetActive(value: true);
		if(CONTROLLER.PlayModeSelected == 8 && !CONTROLLER.AMIOWNER && !Timer.activeInHierarchy)
		{
			Timer.SetActive(value: true);
			TossTimer(5f);
		}
	}

	private void DeactivateButtons()
	{
		BatButton.gameObject.SetActive(value: false);
		BowlButton.gameObject.SetActive(value: false);
	}

	private void ActivateButtons()
	{
		MakeYourCallPanel.SetActive(value: true);
		BatButton.gameObject.SetActive(value: true);
		BowlButton.gameObject.SetActive(value: true);
		if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER && !Timer.activeInHierarchy)
		{
			Timer.SetActive(true);
			TossTimer(7f);
		}
	}

	public void hideMe()
	{
		hideMePanelTransition();
		Invoke("hideMeDelay", 0f);
		Coin.SetActive(value: false);
		Coin.gameObject.transform.localPosition = new Vector3(628.3f, -428f, 743f);
		Coin.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
	}

	public void hideMeDelay()
	{
		resetButtons();
		Holder.SetActive(value: false);
		ResetAllAnimations();
	}

	private void showResult()
	{
		if(CONTROLLER.PlayModeSelected == 8 && !CONTROLLER.AMIOWNER)
		{
			return;
		}
		random = Random.Range(0, 100);
		panelTransition();
		coinStatus = string.Empty;
		DOTween.Init();
		if (random > 50)
		{
			coinStatus = "tail";
			Tittle.DOFade(0f, 0.15f);
			Invoke("SetTail", 0.2f);
		}
		else
		{
			coinStatus = "heads";
			Tittle.DOFade(0f, 0.15f);
			Invoke("SetHead", 0.2f);
		}
		if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER)
		{
              if (GameConstants.isWithAI == false)
            {
                //Photon Removal      photonView.RPC("RPC_CoinStatus", RpcTarget.OthersBuffered, coinStatus);
				CricketNetworkManager.instance.CmdCoinStatus(staticVariables.UserProfiledata.user._id,coinStatus);
            }

         
        }

    }

    //Photon Removal	[PunRPC]
    public void RPC_CoinStatus(string IsHeads)
	{
        panelTransition();
        DOTween.Init();
        if (IsHeads == "heads")
		{
            coinStatus = "heads";
            Tittle.DOFade(0f, 0.15f);
            Invoke("SetHead", 0.2f);
        }
        else
		{
           
            coinStatus = "tail";
            Tittle.DOFade(0f, 0.15f);
            Invoke("SetTail", 0.2f);
        }
	}

    public void SetTail()
	{
		Coin.transform.DORotate(new Vector3(25f, 0f, 0f), 0.01f);
		Invoke("Transistion", 0.01f);
	}

	public void SetHead()
	{
		Coin.transform.DORotate(new Vector3(25f, 0f, 180f), 0.01f);
		Invoke("Transistion", 0.01f);
	}

	public void Transistion()
	{
		if (coinStatus == "heads")
		{
			tossOutcome.text = LocalizationData.localizationInstance.getText(227);
			Coin.transform.DOJump(new Vector3(628.3f, -408f, 705f), 15f, 1, 1.5f, snapping: true);
			Coin.transform.DORotate(new Vector3(11430f, 0f, 180f), 1.5f, RotateMode.FastBeyond360);
		}
		else if (coinStatus == "tail")
		{
			tossOutcome.text = LocalizationData.localizationInstance.getText(228);
			Coin.transform.DOJump(new Vector3(628.3f, -408f, 705f), 15f, 1, 1.5f, snapping: true);
			Coin.transform.DORotate(new Vector3(11430f, 0f, 0f), 1.5f, RotateMode.FastBeyond360);
		}
		Invoke("CoinShow", 2f);
	}

	private string ReplaceText(string original, string replace1, string replace2)
	{
		string text = string.Empty;
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		if (original.Contains("# "))
		{
			text = original.Replace("# ", replace1);
		}
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		if (text.Contains("$"))
		{
			text = text.Replace("$", replace2);
		}
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		return text;
	}

	public void CoinShow()
	{
		CONTROLLER.pageName = "toss";
		if (userSelectedTo == coinStatus)
		{
			Tittle.DOFade(1f, 0.15f);
			displayText1.text = LocalizationData.localizationInstance.getText(230);
			displayText2.text = string.Empty;
			activateBtn();
			if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER && !Timer.activeInHierarchy)
			{
				Timer.SetActive(true);
				TossTimer(7f);
			}
		}
		else 
		{
			////Debug.Log(LocalizationData.instance.getText(408)+"*!@#");
			BatButton.gameObject.SetActive(value: false);
			BowlButton.gameObject.SetActive(value: false);
            if (CONTROLLER.PlayModeSelected != 8 )
			{
				continueButton.gameObject.SetActive(value: true);
				
				random = Random.Range(0, 4);
				if (random < 2)
				{
					CONTROLLER.meFirstBatting = 0;
					displayText1.text = LocalizationData.localizationInstance.getText(408);
					displayText2.text = " " + OpponentAbbrev() + " ";
					displayText1.text = ReplaceText(displayText1.text, string.Empty, LocalizationData.localizationInstance.getText(231));
				}
				else
				{
					CONTROLLER.meFirstBatting = 1;
					displayText1.text = LocalizationData.localizationInstance.getText(408);
					displayText2.text = " " + OpponentAbbrev() + " ";
					displayText1.text = ReplaceText(displayText1.text, string.Empty, LocalizationData.localizationInstance.getText(232));
				}
			}
        }
		if (userSelectedTo == coinStatus)
		{
			Tittle.text = LocalizationData.localizationInstance.getText(229) + "!";
			DG.Tweening.Sequence s = DOTween.Sequence();
			s.Append(flag1.transform.parent.DOLocalMove(finalPos, 0.5f));
			s.Insert(0f, flag2.transform.parent.DOLocalMove(new Vector3(1000f, 0f, 0f), 0.5f));
			s.Insert(0f, flag1.transform.parent.DOScale(Vector3.one * 0.65f, 0.5f));
			Tossinfo.SetActive(value: true);
			s.Insert(0.5f, Tossinfo.transform.DOScaleX(1f, 0.5f));
			s.Insert(0.5f, displayText1.transform.DOScaleX(1f, 0.5f));
			s.Insert(0.5f, Tittle.DOFade(1f, 0.5f));
			s.Insert(0.5f, tossOutcome.DOFade(1f, 0.5f));
		}
		else 
		{
			Tittle.text = LocalizationData.localizationInstance.getText(407);
			//if (Singleton<AdIntegrate>.instance.isRewardedVideoAvailable)
			//{
			//	RV_Button.gameObject.SetActive(value: true);
			//}
			//else
			//{
			//	RV_Button.gameObject.SetActive(value: false);
			//	Singleton<AdIntegrate>.instance.requestRewardedVideo();
			//}
			DG.Tweening.Sequence s2 = DOTween.Sequence();
			s2.Append(flag2.transform.parent.DOLocalMove(finalPos, 0.5f));
			s2.Insert(0f, flag1.transform.parent.DOLocalMove(new Vector3(-1000f, 0f, 0f), 0.5f));
			s2.Insert(0f, flag2.transform.parent.DOScale(Vector3.one * 0.65f, 0.5f));
			Tossinfo.SetActive(value: true);
			if(CONTROLLER.PlayModeSelected != 8)
			{
				s2.Insert(0.5f, Tossinfo.transform.DOScaleX(1f, 0.5f));
				s2.Insert(0.5f, displayText1.transform.DOScaleX(1f, 0.5f));
			}
			s2.Insert(0.5f, Tittle.DOFade(1f, 0.5f));
			s2.Insert(0.5f, tossOutcome.DOFade(1f, 0.5f));
		}
		resetTransition();
		panelTransition();
	}

	public void panelTransition()
	{
		BatButton.gameObject.transform.DOLocalMove(new Vector3(-130f, -273.9f, 0f), 0.5f);
		BowlButton.gameObject.transform.DOLocalMove(new Vector3(130f, -273.9f, 0f), 0.5f);
		BatButton.image.DOFade(1f, 0.15f);
		BatButton.interactable = true;
		BowlButton.image.DOFade(1f, 0.15f);
		BowlButton.interactable = true;
		Button1Text.DOFade(1f, 0.15f);
		Button2Text.DOFade(1f, 0.15f);
	}

	public void resetTransition()
	{
	}

	public void hideMePanelTransition()
	{
		BatButton.gameObject.transform.DOLocalMove(new Vector3(-305f, -273.9f, 0f), 0f);
		BowlButton.gameObject.transform.DOLocalMove(new Vector3(305f, -273.9f, 0f), 0f);
		BatButton.image.DOFade(0f, 0.15f);
		BatButton.interactable = false;
		BowlButton.image.DOFade(0f, 0.15f);
		BowlButton.interactable = false;
		Button1Text.DOFade(0f, 0.15f);
		Button2Text.DOFade(0f, 0.15f);
	}

	private void ResetAllAnimations()
	{
		DG.Tweening.Sequence s = DOTween.Sequence();
		s.Append(flag1.transform.parent.DOLocalMove(flag1StartPos, 0f));
		s.Insert(0f, flag1.transform.parent.DOScale(Vector3.one, 0f));
		s.Insert(0f, flag2.transform.parent.DOLocalMove(flag2StartPos, 0f));
		s.Insert(0f, flag2.transform.parent.DOScale(Vector3.one, 0f));
		s.Insert(0f, Tittle.DOFade(0f, 0f));
		s.Insert(0f, tossOutcome.DOFade(0f, 0f));
		s.Insert(0f, Tossinfo.transform.DOScaleX(0f, 0f));
		s.Insert(0f, displayText1.transform.DOScaleX(0f, 0f));
	}

	public void Continue()
	{
		hideMe();
		if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
		{
			oversText = "QPOvers";
		}
		//else if (CONTROLLER.PlayModeSelected == 1)
		//{
		//	oversText = "T20Overs";
		//}
		//else if (CONTROLLER.PlayModeSelected == 2)
		//{
		//	if (CONTROLLER.tournamentType == "NPL")
		//	{
		//		oversText = "NPLOvers";
		//	}
		//	else if (CONTROLLER.tournamentType == "PAK")
		//	{
		//		oversText = "PAKOvers";
		//	}
		//	else if (CONTROLLER.tournamentType == "AUS")
		//	{
		//		oversText = "AUSOvers";
		//	}
		//}
		//else if (CONTROLLER.PlayModeSelected == 3)
		//{
		//	oversText = "WCOvers";
		//}
		CONTROLLER.oversSelectedIndex = ObscuredPrefs.GetInt(oversText);
		Coin.gameObject.transform.localPosition = new Vector3(628.3f, -428f, 743f);
		Coin.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
		CONTROLLER.SceneIsLoading = true;
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
		Singleton<NavigationBack>.instance.deviceBack = null;
		Singleton<LoadingPanelTransition>.instance.PanelTransition1("Ground");
	}

	public void GotoGroundScene()
	{
		CONTROLLER.SceneIsLoading = true;
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
		Singleton<NavigationBack>.instance.deviceBack = null;
		Singleton<LoadingPanelTransition>.instance.PanelTransition1("Ground");
	}

	public void Back()
	{
		hideMe();
		Coin.gameObject.transform.localPosition = new Vector3(628.3f, -428f, 743f);
		Coin.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
		if (CONTROLLER.PlayModeSelected == 0)
		{
			Singleton<TeamSelectionTWO>.instance.showMe();
		}
		//else if (CONTROLLER.PlayModeSelected == 1)
		//{
		//	if (CONTROLLER.TournamentStage == 0)
		//	{
		//		Singleton<TeamSelectionTWO>.instance.Continue();
		//	}
		//	else
		//	{
		//		Singleton<FixturesTWO>.instance.showMe();
		//	}
		//}
		//else if (CONTROLLER.PlayModeSelected == 2)
		//{
		//	if (CONTROLLER.NPLIndiaTournamentStage == 0)
		//	{
		//		Singleton<TeamSelectionTWO>.instance.Continue();
		//	}
		//	else
		//	{
		//		Singleton<NPLIndiaPlayOff>.instance.ShowMe();
		//	}
		//}
		//else if (CONTROLLER.PlayModeSelected == 3)
		//{
		//	if (CONTROLLER.WCTournamentStage == 0)
		//	{
		//		Singleton<WorldCupLeague>.instance.ShowMe();
		//	}
		//	else
		//	{
		//		Singleton<WorldCupPlayOff>.instance.ShowMe();
		//	}
		//}
	}

	private void Update()
	{
		if (Holder.activeInHierarchy && firstTimeToss)
		{
			if(!(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER))
			{
				InputViaTouch();
				InputViaMouse();
			}
		}
        if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.AMIOWNER && SwipeUpPanel.gameObject.activeInHierarchy)
        {
            SwipeUpPanel.SetActive(false);
        }
        
    }

	public void InputViaTouch()
	{
		if (Input.touches.Length <= 0)
		{
			return;
		}
		Touch touch = Input.GetTouch(0);
		if (touch.phase == TouchPhase.Began)
		{
			firstPressPos = new Vector2(touch.position.x, touch.position.y);
		}
		if (touch.phase == TouchPhase.Ended)
		{
			secondPressPos = new Vector2(touch.position.x, touch.position.y);
			currentSwipe = new Vector3(secondPressPos.x - firstPressPos.x, secondPressPos.y - firstPressPos.y);
			currentSwipe.Normalize();
			if (currentSwipe.y > 0f && currentSwipe.x > -0.5f && currentSwipe.x < 0.5f)
			{
				if(CONTROLLER.PlayModeSelected == 8)
				{
                                 if (GameConstants.isWithAI == false)
                                 {																														 //Photon Removal
                    firstTimeToss = false;	
                    //photonView.RPC("RPC_AfterUpSwipe", RpcTarget.AllBuffered);
					CricketNetworkManager.instance.CmdAfterUpSwipe();
                                  }


                    return;
				}
				AfterUpSwipe();
				firstTimeToss = false;
			}
			if (!(currentSwipe.y < 0f) || !(currentSwipe.x > -0.5f) || currentSwipe.x < 0.5f)
			{
			}
			if (!(currentSwipe.x < 0f) || !(currentSwipe.y > -0.5f) || currentSwipe.y < 0.5f)
			{
			}
			if (currentSwipe.x > 0f && currentSwipe.y > -0.5f && !(currentSwipe.y < 0.5f))
			{
			}
		}
	}

	public void InputViaMouse()
	{
		if (Input.GetMouseButtonDown(0))
		{
			firstPressPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		}
		if (Input.GetMouseButtonUp(0))
		{
			secondPressPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			currentSwipe = new Vector2(secondPressPos.x - firstPressPos.x, secondPressPos.y - firstPressPos.y);
			currentSwipe.Normalize();
			if (currentSwipe.y > 0f && currentSwipe.x > -0.5f && currentSwipe.x < 0.5f)
			{
                if (CONTROLLER.PlayModeSelected == 8)
                {
                                  if (GameConstants.isWithAI == false)
                              {
                    firstTimeToss = false;
                    //photonView.RPC("RPC_AfterUpSwipe", RpcTarget.AllBuffered);
					CricketNetworkManager.instance.CmdAfterUpSwipe();															
                                  }



                    return;
				}
                AfterUpSwipe();
				firstTimeToss = false;
			}
			if (!(currentSwipe.y < 0f) || !(currentSwipe.x > -0.5f) || currentSwipe.x < 0.5f)
			{
			}
			if (!(currentSwipe.x < 0f) || !(currentSwipe.y > -0.5f) || currentSwipe.y < 0.5f)
			{
			}
			if (currentSwipe.x > 0f && currentSwipe.y > -0.5f && !(currentSwipe.y < 0.5f))
			{
			}
		}
	}
}
