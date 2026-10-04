using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class GameModeTWO : Singleton<GameModeTWO>
{
    public Button MultiPlayBtn;
    [Header("GameObject")]
    public GameObject Holder;

    public GameObject newUserPopup;

    public GameObject WCHolder;

    public GameObject PLModes;

    public GameObject SixMeterToolTip;

    public GameObject arcadeMeterProgress;

    public GameObject arcadeMeterCompleted;

    public GameObject content;

    public GameObject SigninButton;

    public GameObject SpinWheelPanel;

    public GameObject betaUserReward;

    [Header("Text")]
    public Text[] timerText;

    public Text arcadeProgress;

    public Text PowerPercent;

    public Text ControlPercent;

    public Text AgilityPercent;

    public Text PowerPercent2;

    public Text ControlPercent2;

    public Text AgilityPercent2;

    public Text[] userXP;

    public Text[] userCoins;

    public Text[] userTickets;

    [Header("Image")]
    public Image[] star;

    public Image arcadeMeterFill;

    public Image PowerFillImage;

    public Image ControlFillImage;

    public Image AgilityFillImage;

    public Image PowerFillImage2;

    public Image ControlFillImage2;

    public Image AgilityFillImage2;

    public Image AchievementSpark1;

    public Image AchievementSpark2;

    public Image AchievementSpark3;

    public Image googleUserImage;

    public Image[] UpgradesImages;

    [Header("Sprite")]
    public Sprite defaultImg;

    [Header("Transform")]
    public Transform giftImg;

    public Transform SpinImg;

    [Header("Variables")]
    public static string insuffStatus;

    private Vector3 startPos;

    public bool ButtonTab1Bool;

    public bool ResetTab1;

    private int ReturnState;

    public int kitValue;

    private int starToAnimate;

    private Tweener tween;

    private bool canOpenToolTip = true;

  

    protected void Start()
    {
        if (GameConstants.isWithAI)
        {
            Invoke("getExhibitionState", 1);
        }
    }
    void OnMultiplayer()
    {
        MultiPlayBtn.onClick.Invoke();
    }
    private void Update()
    {
        SpinImg.DORotate(new Vector3(0f, 0f, SpinImg.eulerAngles.z + 113f), 5f, RotateMode.FastBeyond360);
        if (content.transform.localPosition.x <= -500f)
        {
            if (ReturnState == 2)
            {
                ReturnState = 1;
            }
            ButtonTab1Bool = true;
        }
        else
        {
            ButtonTab1Bool = false;
            ReturnState = 2;
            ResetTab1 = true;
        }
        if (ButtonTab1Bool && ReturnState == 1 && ResetTab1)
        {
            Singleton<InfoAnim>.instance.loop1();
            Singleton<settingsAnim>.instance.Settingsloop();
            Singleton<Help_Anim>.instance.loop1();
            Singleton<LeaderBoard_Anim>.instance.loop1();
            Singleton<Like_Anim>.instance.loop1();
            Singleton<Follow_Anim>.instance.loop1();
            Singleton<Rate_Anim>.instance.loop1();
            ResetTab1 = false;
        }
    }



    public void closePopup(int index)
    {
        switch (index)
        {
            case 0:
                CONTROLLER.QuitApp();
                break;
            case 1:
                CONTROLLER.CurrentMenu = "landingpage";
                break;
        }
    }


    public void CloseWorldCupModes()
    {
        WCHolder.SetActive(value: false);
        showMe();
    }


    public void CloseArcadeModes(int index)
    {
        CONTROLLER.pageName = "landingPage";
        if (!Holder.activeInHierarchy)
        {
            showMe();
        }
    }

    public void OpenNoInternetPopup()
    {
        CONTROLLER.PopupName = "noInternet";
        Singleton<Popups>.instance.ShowMe();
    }

    public void CloseNoInternetPopup()
    {
        Singleton<Popups>.instance.HideMe();
    }

    public void PlayGameSound(string SoundType)
    {
        if (CONTROLLER.sndController != null)
        {
            CONTROLLER.sndController.PlayGameSnd(SoundType);
        }
    }

    public void GetMultiplayerState()
    {
        CONTROLLER.PlayModeSelected = 8;
        CONTROLLER.matchType = "oneday";
        UIDataHolder.gameMode = 0;
        //CONTROLLER.PlayModeSelected = 0;
       // WCHolder.SetActive(value: false);
        string empty = string.Empty;
        empty = AutoSave.ReadFile();
        Singleton<GUIRoot>.instance.GetWorldCupTeams("XML/GameMode/WorldCupSeries");
        CONTROLLER.menuTitle = "TEAM SELECTION";
        hideMe();
        Holder.SetActive(false);
        //Singleton<EntryFeesAndRewards>.instance.ShowMe();
        displayGameMode(_bool: false);
        updateTitle(_modeSelected: true);
       
    }

    public void getExhibitionState()
    {
        //Debug.Log("kdr cy");
        CONTROLLER.matchType = "oneday";
        UIDataHolder.gameMode = 0;
        CONTROLLER.PlayModeSelected = 0;
        string empty = string.Empty;
        empty = AutoSave.ReadFile();
        Singleton<GUIRoot>.instance.GetWorldCupTeams("XML/GameMode/WorldCupSeries");
        if (empty == string.Empty)
        {
            CONTROLLER.menuTitle = "TEAM SELECTION";
            hideMe();
            Singleton<EntryFeesAndRewards>.instance.ShowMe();
            displayGameMode(_bool: false);
            updateTitle(_modeSelected: true);
        }
        else
        {
            Singleton<IncompleteMatch>.instance.showMe();
        }
        //FirebaseAnalyticsManager.instance.logEvent("MainMenu_click", "MainMenu", CONTROLLER.userID);
        //FirebaseAnalyticsManager.instance.logEvent("Extras", new string[2] { "ExtrasAction", "QP_Clicked" });
    }

  
    public void ResumeSuperOverSavedGame()
    {
        CONTROLLER.GameStartsFromSave = true;
        AutoSave.LoadGame();
        CONTROLLER.SceneIsLoading = true;
        Singleton<GameModeTWO>.instance.hideMe();
        CONTROLLER.CurrentMenu = string.Empty;
        Singleton<NavigationBack>.instance.deviceBack = null;
        Singleton<LoadingPanelTransition>.instance.PanelTransition1("Ground");
    }

    public void NewUserClaimPopup()
    {
        Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
        newUserPopup.SetActive(value: false);
        CONTROLLER.newUser = false;
        ObscuredPrefs.SetInt("newUser", 1);
        CONTROLLER.pageName = "landingPage";
    }

    public void hideModes()
    {
        displayGameMode(_bool: false);
        hideHomeBtn(_bool: false);
    }

    public void ValidateArcadeMeter()
    {
        if (ObscuredPrefs.HasKey("ArcadeSixes"))
        {
            if (ObscuredPrefs.GetInt("ArcadeSixes") >= 30)
            {
                arcadeMeterCompleted.SetActive(value: true);
                arcadeMeterProgress.SetActive(value: false);
                return;
            }
            arcadeMeterCompleted.SetActive(value: false);
            arcadeMeterProgress.SetActive(value: true);
            arcadeMeterFill.fillAmount = (float)ObscuredPrefs.GetInt("ArcadeSixes") / 30f;
            arcadeProgress.text = ObscuredPrefs.GetInt("ArcadeSixes") + "/30";
        }
        else
        {
            ObscuredPrefs.SetInt("ArcadeSixes", 0);
            arcadeMeterFill.fillAmount = (float)ObscuredPrefs.GetInt("ArcadeSixes") / 30f;
            arcadeProgress.text = ObscuredPrefs.GetInt("ArcadeSixes") + "/30";
        }
    }

    public void MainmenuUpgradesUI()
    {
        int num = Mathf.RoundToInt(CONTROLLER.totalPowerSubGrade / 13);
        int num2 = Mathf.RoundToInt(CONTROLLER.totalControlSubGrade / 13);
        int num3 = Mathf.RoundToInt(CONTROLLER.totalAgilitySubGrade / 13);
        if (num > 100)
        {
            num = 100;
        }
        if (num2 > 100)
        {
            num2 = 100;
        }
        if (num3 > 100)
        {
            num3 = 100;
        }
        float num4 = (float)num / 100f;
        float num5 = (float)num2 / 100f;
        float num6 = (float)num3 / 100f;
        Sequence s = DOTween.Sequence();
        s.Append(PowerFillImage.DOFillAmount(num4, 2f));
        s.Insert(0f, ControlFillImage.DOFillAmount(num5, 2f));
        s.Insert(0f, AgilityFillImage.DOFillAmount(num6, 2f));
        PowerFillImage2.fillAmount = num4;
        ControlFillImage2.fillAmount = num5;
        AgilityFillImage2.fillAmount = num6;
        PowerPercent.text = num + "%";
        ControlPercent.text = num2 + "%";
        AgilityPercent.text = num3 + "%";
        PowerPercent2.text = num + "%";
        ControlPercent2.text = num2 + "%";
        AgilityPercent2.text = num3 + "%";
    }

    public void OpenQuitGamePopup()
    {
        CONTROLLER.PopupName = "exitPopup";
        Singleton<Popups>.instance.ShowMe();
    }

    public void ShowWithoutAnim()
    {
        Singleton<NavigationBack>.instance.deviceBack = OpenQuitGamePopup;
        //Singleton<AdIntegrate>.instance.HideAd();
        Holder.SetActive(value: true);
    }

    private void MainPageAnimations()
    {
        giftImg.DOScale(Vector3.one * 1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(AchievementSpark1.DOFade(1f, 0.25f));
        sequence.Insert(0.25f, AchievementSpark1.DOFade(0f, 1f));
        sequence.Insert(2.25f, AchievementSpark3.DOFade(1f, 1f));
        sequence.Insert(4.5f, AchievementSpark3.DOFade(0f, 1f));
        sequence.Insert(6.5f, AchievementSpark2.DOFade(1f, 1f));
        sequence.Insert(8.25f, AchievementSpark2.DOFade(0f, 1f));
        sequence.Insert(10f, AchievementSpark2.DOFade(0f, 0f));
        sequence.SetLoops(-1, LoopType.Yoyo);
        Sequence s = DOTween.Sequence();
        for (int i = 0; i < UpgradesImages.Length; i++)
        {
            s.Insert(0f, UpgradesImages[i].gameObject.transform.DOPunchScale(Vector3.zero, 0f, 0, 0f));
            s.Insert(0.5f + (float)i * 0.25f, UpgradesImages[i].gameObject.transform.DOPunchScale(Vector3.one * 0.5f, 1f, 3, 0.1f));
        }
    }

    public void temp()
    {
        ResetScroll();
        ResetDetails();
        Holder.SetActive(value: true);
        Singleton<GameModeTWOPanelTransition>.instance.PanelTransition();
        MainmenuUpgradesUI();
    }


    public void showMe()
    {
        Singleton<NavigationBack>.instance.deviceBack = OpenQuitGamePopup;
        //SignOut_Button_Set();
        //Singleton<AdIntegrate>.instance.HideAd();

        //For Try
        FindObjectOfType<TeamSelectionTWO>().Holder.SetActive(false);
        ////Debug.Log("778465adadf");
        CONTROLLER.pageName = "landingPage";
        CONTROLLER.screenToDisplay = "landingPage";

        ValidateArcadeMeter();

        ResetScroll();
        ResetDetails();
        Holder.SetActive(value: true);
       // Launcher.Instance.DisconnectServer();
        Singleton<GameModeTWOPanelTransition>.instance.PanelTransition();
        MainmenuUpgradesUI();

        //quickPlayBtn.onClick.Invoke();
       // Invoke("getExhibitionState", 1);
    }


    public void ResetDetails()
    {
        
        arcadeMeterProgress.SetActive(value: true);
        arcadeMeterFill.fillAmount = (float)ObscuredPrefs.GetInt("ArcadeSixes") / 30f;
        arcadeProgress.text = ObscuredPrefs.GetInt("ArcadeSixes") + "/30";
        ValidateArcadeMeter();
    }

    public void ResetDetailstoZero()
    {
        for (int i = 0; i < 2; i++)
        {
            userTickets[i].text = "0".ToString();
            userXP[i].text = "0".ToString();
            userCoins[i].text = "0".ToString();
        }
        arcadeMeterProgress.SetActive(value: true);
        arcadeMeterFill.fillAmount = (float)ObscuredPrefs.GetInt("ArcadeSixes") / 30f;
        arcadeProgress.text = ObscuredPrefs.GetInt("ArcadeSixes") + "/30";
    }

    public void hideMe()
    {
        //if (CONTROLLER.canShowbannerMainmenu == 1)
        //{
        //	Singleton<AdIntegrate>.instance.ShowAd();
        //}
        CONTROLLER.mainMenuHidden = true;
        Holder.SetActive(value: false);
        Singleton<GameModeTWOPanelTransition>.instance.resetTransition();
    }

    public void utilSelected(int index)
    {
        switch (index)
        {
            case 0:
                CONTROLLER.menuTitle = "CONTROLS";
                Singleton<SettingsPageTWO>.instance.hideMe();
                Singleton<ControlsPageTWO>.instance.showMe();
                displayGameMode(_bool: false);
                break;
            case 1:
                CONTROLLER.menuTitle = "SETTINGS";
                Singleton<ControlsPageTWO>.instance.hideMe();
                Singleton<SettingsPageTWO>.instance.showMe();
                displayGameMode(_bool: false);
                break;
            case 2:
                CONTROLLER.menuTitle = "ABOUT";
                Singleton<SettingsPageTWO>.instance.hideMe();
                Singleton<ControlsPageTWO>.instance.hideMe();
                displayGameMode(_bool: false);
                break;
            case 3:
                Singleton<TeamSelectionTWO>.instance.hideMe();
                Singleton<TossPageTWO>.instance.hideMe();
                Singleton<SquadPageTWO>.instance.hideMe();
                //Singleton<FixturesTWO>.instance.hideMe();
                CONTROLLER.menuTitle = string.Empty;
                Singleton<GameModeTWO>.instance.showMe();
                displayGameMode(_bool: true);
                break;
        }
        updateTitle(_modeSelected: false);
    }

    private void PushNotiCal()
    {
        if (CONTROLLER.PushScreenNumber == 0)
        {
            showMe();
        }
        else if (CONTROLLER.PushScreenNumber == 1)
        {
            getExhibitionState();
        }
      
        else if (CONTROLLER.PushScreenNumber == 8)
        {
            showMe();
           
        }
        else if (CONTROLLER.PushScreenNumber == 11)
        {
            showMe();
        }
        else if (CONTROLLER.PushScreenNumber == 12)
        {
            showMe();
        }
        else if (CONTROLLER.PushScreenNumber == 13)
        {
            showMe();
        }
        else if (CONTROLLER.PushScreenNumber == 14)
        {
            showMe();
        }
      
        CONTROLLER.pushNotiClicked = false;
        CONTROLLER.PushScreenNumber = 0;
    }

    public void DirectLink()
    {
        if (CONTROLLER.pushNotiClicked)
        {
            Invoke("PushNotiCal", 0.5f);
            return;
        }
        Singleton<NavigationBack>.instance.disableDeviceBack = false;
        if (CONTROLLER.screenToDisplay == "landingPage")
        {
            showMe();
        }
     
        else if (CONTROLLER.screenToDisplay == "quickPlay")
        {
            getExhibitionState();
        }
        else if (CONTROLLER.screenToDisplay == "TossPage")
        {
            Time.timeScale = 1f;
            Singleton<TossPageTWO>.instance.showMeDelay();
        }
      
    }

    private void resetAllUtilBtns()
    {
        CONTROLLER.menuTitle = string.Empty;
        updateTitle(_modeSelected: false);
    }

    private void ResetScroll()
    {
        content.transform.position = startPos;
    }

    public void hideHomeBtn(bool _bool)
    {
        if (!_bool)
        {
        }
    }

    public void displayGameMode(bool _bool)
    {
        if (_bool)
        {
            CONTROLLER.CurrentMenu = "landingpage";
            resetAllUtilBtns();
        }
    }

    public void updateTitle(bool _modeSelected)
    {
        if (!_modeSelected)
        {
        }
    }

    private void StartStarAnim()
    {
        int num = (starToAnimate = Random.Range(0, 3));
        tween = star[num].DOFade(1f, 1f).OnComplete(StopStarAnim);
    }

    private void StopStarAnim()
    {
        tween = star[starToAnimate].DOFade(0f, 0.6f);
        Invoke("StartStarAnim", Random.Range(2, 6));
    }

    public void CloseExitPopup()
    {
        Singleton<Popups>.instance.HideMe();
        CONTROLLER.pageName = "landingPage";
    }

    public void GameExit()
    {
        Application.Quit();
    }

   
    public void ClosePremierLeagueModes()
    {
        PLModes.SetActive(value: false);
        showMe();
    }

    private void FreeEntry()
    {
    }

  
    public void OpenSixMeterToolTip()
    {
        if (canOpenToolTip)
        {
            SixMeterToolTip.SetActive(value: true);
            canOpenToolTip = false;
            Invoke("CloseSixMeterToolTip", 3f);
        }
    }

    private void CloseSixMeterToolTip()
    {
        SixMeterToolTip.SetActive(value: false);
        canOpenToolTip = true;
    }

  
}
