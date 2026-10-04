using DG.Tweening;

using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class PauseGameScreen : Singleton<PauseGameScreen>
{
    public GameObject BG;

    public GameObject skipbutton;

    public Image MyTeamFlag;

    public Image OppTeamFlag;

    public Text TeamName1Txt;

    public Text TeamName2Txt;

    public Text PauseScreenTittle1;

    public Text PauseScreenTittle2;

    public Text ScoreTxt1;

    public Text ScoreTxt2;

    public Text APScore;

    public Text NeedText1;

    public Text NeedText2;

    public Button autoplayBtn;

    public GameObject autoPlayDisableBtn;

    public GameObject bowlingScoreCard;

    public GameObject battingScoreCard;

    public GameObject declareBtn;

    public GameObject noteText;

    public GameObject midPageGO;

    public GameObject RVPanel;

    public int remainingOvers;

    //Multiplayer

    //Photon Removal  public PhotonView photonView;

    private float secCount;

    [SerializeField] private Image timerImage;

    [SerializeField] private Text timerText;

    [SerializeField] private GameObject Timer;

    private static int ClosePauseMenu;

    private bool PlayBtnClicked;

    private const int MaxRetries = 3; // Number of retry attempts for acknowledgement
    private float RetryDelay = 0.5f;

    protected void Awake()
    {
        Hide(boolean: true);
    }

    public void addEventListener()
    {
    }

    private string ReplaceText(string original, string replace1, string replace2)
    {
        string text = string.Empty;
        if (original.Contains("#"))
        {
            text = original.Replace("#", replace1);
        }
        if (text.Contains("$"))
        {
            text = text.Replace("$", replace2);
        }
        return text;
    }

    private void UpdateGamePause()
    {
        Singleton<GamePausePanelTransition>.instance.panelTransition();
        midPageGO.SetActive(value: true);
        Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
        foreach (Sprite sprite in flags)
        {
            if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation)
            {
                MyTeamFlag.sprite = sprite;
            }
            if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation)
            {
                OppTeamFlag.sprite = sprite;
            }
        }
        Singleton<BowlingControls>.instance.Hide(boolean: true);
        //Debug.Log(TeamName1Txt.text + " " + TeamName2Txt.text);
        TeamName1Txt.text = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].teamName;
        int index = LocalizationData.localizationInstance.referenceList.IndexOf(TeamName1Txt.text.ToUpper());
        TeamName1Txt.text = LocalizationData.localizationInstance.getText(index);
        TeamName2Txt.text = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].teamName;
        index = LocalizationData.localizationInstance.referenceList.IndexOf(TeamName2Txt.text.ToUpper());
        TeamName2Txt.text = LocalizationData.localizationInstance.getText(index);
        //Debug.Log(TeamName1Txt.text + " " + TeamName2Txt.text);
        ScoreTxt1.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation.ToUpper() + " " + Singleton<GameData>.instance.scoreDisplayString;
        ScoreTxt2.text = " " + LocalizationData.localizationInstance.getText(253);
        ScoreTxt2.text = ReplaceText(ScoreTxt2.text, string.Empty, Singleton<GameData>.instance.oversDisplayString);
        //Debug.Log(ScoreTxt2.text);
        if (ScoreTxt2.text.Contains("("))
        {
            //Debug.Log(ScoreTxt2.text);
            ScoreTxt2.text = ScoreTxt2.text.Replace("(", LocalizationData.localizationInstance.getText(651));
        }
        //Debug.Log(ScoreTxt2.text);
        if (ScoreTxt2.text.Contains(")"))
        {
            //Debug.Log(ScoreTxt2.text);
            ScoreTxt2.text = ScoreTxt2.text.Replace(")", LocalizationData.localizationInstance.getText(652));
        }
        //Debug.Log(ScoreTxt2.text);
        int num = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls;
        int num2 = CONTROLLER.TargetToChase - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
        if (CONTROLLER.PlayModeSelected != 7)
        {
            NeedText1.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation.ToUpper();
            NeedText2.text = " " + LocalizationData.localizationInstance.getText(260);
            NeedText2.text = ReplaceText(NeedText2.text, num2.ToString(), num.ToString());
        }
        else
        {
            string text = (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls1 / 6).ToString();
            string text2 = (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls1 % 6).ToString();
            if (CONTROLLER.currentInnings == 0)
            {
                ScoreTxt1.text = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation.ToUpper() + " " + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchScores1 + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchWickets1;
                ScoreTxt2.text = " " + LocalizationData.localizationInstance.getText(260);
                ScoreTxt2.text = ReplaceText(ScoreTxt2.text, string.Empty, text + "." + text2);
            }
            else
            {
                ScoreTxt1.text = string.Empty;
                ScoreTxt2.text = Singleton<Scoreboard>.instance.str;
            }
            NeedText1.text = string.Empty;
            NeedText2.text = string.Empty;
        }
        if (!Singleton<GroundController>.instance.isBallReleased)
        {
            noteText.SetActive(value: false);
            autoplayBtn.interactable = true;
        }
        else
        {
            noteText.SetActive(value: true);
            autoplayBtn.interactable = false;
        }

        if ((bool)GoogleAnalytics.instance)
        {
            GoogleAnalytics.instance.LogEvent("Game", "GamePause");
        }
    }

    //Photon Removal	[PunRPC]
    private void RPC_ClosePauseMenu(bool pause)
    {
        if (!pause)
        {
            ClosePauseMenu = 0;
            PlayBtnClicked = false;

            return;
        }
        ClosePauseMenu++;
        if (ClosePauseMenu >= 2)
        {
            ClosePause();
        }
    }

    public void menuClicked(int index)
    {
        Singleton<GameData>.instance.GamePauseMenuSelected();
        switch (index)
        {
            case 0:
                if (CONTROLLER.PlayModeSelected == 8 && ClosePauseMenu <= 1 && !PlayBtnClicked)
                {
                    //Photon Removal  if (PhotonNetwork.IsConnected)
                    {
                        //Photon Removal      photonView.RPC("RPC_ClosePauseMenu", RpcTarget.AllBuffered,true);
                    }


                    PlayBtnClicked = true;
                    return;
                }
                if (PlayBtnClicked && ClosePauseMenu == 1)
                {
                    return;
                }

                CONTROLLER.pageName = "Ground";
                Screen.sleepTimeout = -1;
                Hide(boolean: true);
                Singleton<GameData>.instance.groundController.canShowCountdown = true;
                Singleton<GameData>.instance.GamePaused(boolean: false);
                LocalizeTextUpdate();
                Timer.SetActive(false);

                if (CONTROLLER.PlayModeSelected == 8)
                {
                    //Photon Removal  if (PhotonNetwork.IsConnected)
                    {
                        //Photon Removal     photonView.RPC("RPC_ClosePauseMenu", RpcTarget.AllBuffered, false);
                    }



                }


                break;
            case 1:
                Hide(boolean: true);

                Singleton<BattingScoreCard>.instance.Hide(boolean: false);
                if (CONTROLLER.PlayModeSelected != 7)
                {
                    Singleton<BattingScoreCard>.instance.UpdateScoreCard();
                }
                Singleton<NavigationBack>.instance.deviceBack = Singleton<BattingScoreCard>.instance.Continue;
                ////Debug.Log("SkiP CoNtINue");

                break;
            case 2:
                Hide(boolean: true);
                Singleton<GameData>.instance.ShowBowlingScoreCard();
                Singleton<NavigationBack>.instance.deviceBack = Singleton<BowlingScoreCard>.instance.Continue;
                break;
            case 3:
                midPageGO.SetActive(value: false);
                Singleton<SettingsPageTWO>.instance.hideMe();
                break;
            case 4:
                midPageGO.SetActive(value: false);
                Singleton<SettingsPageTWO>.instance.showMe();
                break;
            case 5:
                showQuitPopup();
                break;
            case 6:
                autoPlaySelected();
                break;
        }
    }

    public void LocalizeTextUpdate()
    {
        Singleton<PreviewScreen>.instance.SetFieldPreview();

    }

    public void declare()
    {
        if (CONTROLLER.currentInnings < 2)
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].isDeclared1 = true;
        }
        else
        {
            CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].isDeclared2 = true;
        }
        Singleton<GameData>.instance.checkForDeclaration();
        if (CONTROLLER.RunRate >= 4f && CONTROLLER.currentInnings != 2)
        {
            CONTROLLER.declareMode = true;
        }
        midPageGO.SetActive(value: false);
    }

    private void autoPlaySelected()
    {
        CONTROLLER.pageName = "autoplay";


        midPageGO.SetActive(value: false);


        RVPanel.SetActive(value: true);

    }

    public void RVAutoPlaySelected()
    {
    }

    public void autoPlayOperations()
    {

        RVPanel.SetActive(value: false);
        midPageGO.SetActive(value: false);
        iTween.Stop(Singleton<BowlingControls>.instance.SpeedArrow);
        Singleton<BowlingControls>.instance.stopSwingAngle();
        generateScore();
    }

    private void generateScore()
    {
        Singleton<NavigationBack>.instance.disableDeviceBack = true;
        if (CONTROLLER.PlayModeSelected != 7)
        {
            remainingOvers = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6;
            if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
            {
                Singleton<AutoPlay>.instance.SimulateOvers(int.Parse(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].rank), int.Parse(CONTROLLER.TeamList[CONTROLLER.myTeamIndex].rank), remainingOvers);
            }
            else
            {
                Singleton<AutoPlay>.instance.SimulateOvers(int.Parse(CONTROLLER.TeamList[CONTROLLER.myTeamIndex].rank), int.Parse(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].rank), remainingOvers);
            }
            return;
        }
        remainingOvers = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] - CONTROLLER.ballsBowledPerDay / 6;
        if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            if (CONTROLLER.currentInnings < 2)
            {
                Singleton<AutoPlay>.instance.CalculateAutoPlayFirstInnings(int.Parse(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].rank), int.Parse(CONTROLLER.TeamList[CONTROLLER.myTeamIndex].rank));
            }
            else
            {
                Singleton<AutoPlay>.instance.CalculateAutoPlaySecondInnings(int.Parse(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].rank), int.Parse(CONTROLLER.TeamList[CONTROLLER.myTeamIndex].rank));
            }
        }
        else if (CONTROLLER.currentInnings < 2)
        {
            Singleton<AutoPlay>.instance.CalculateAutoPlayFirstInnings(int.Parse(CONTROLLER.TeamList[CONTROLLER.myTeamIndex].rank), int.Parse(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].rank));
        }
        else
        {
            Singleton<AutoPlay>.instance.CalculateAutoPlaySecondInnings(int.Parse(CONTROLLER.TeamList[CONTROLLER.myTeamIndex].rank), int.Parse(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].rank));
        }
    }

    public void showQuitPopup()
    {
        Singleton<NavigationBack>.instance.deviceBack = Singleton<QuitConfirm>.instance.hideMe;
        midPageGO.SetActive(value: false);
        Singleton<QuitConfirm>.instance.showMe();
    }

    public void hideAll()
    {
        midPageGO.SetActive(value: false);
    }

    private void OpenPause()
    {
        Hide(boolean: false);
    }

    private void ClosePause()
    {
        menuClicked(0);
    }

    public void Hide(bool boolean)
    {

        if (boolean)
        {
            if (!CONTROLLER.gameCompleted)
            {
                BG.SetActive(value: false);
            }
            else
            {
                BG.SetActive(value: true);
            }
            midPageGO.SetActive(value: false);
            if (CONTROLLER.PlayModeSelected == 8)
            {

            }
            return;
        }
        if (CONTROLLER.PlayModeSelected == 8)
        {
            return;
        }

        if (CONTROLLER.PlayModeSelected == 7)
        {
        }

        else
        {
            declareBtn.SetActive(value: false);
        }
        if (CONTROLLER.PlayModeSelected == 8)
        {
            Timer.SetActive(true);
            SetPauseScreenTimer(30f);
        }
        Singleton<NavigationBack>.instance.deviceBack = ClosePause;
        CONTROLLER.pageName = "GamePause";
        CONTROLLER.CurrentMenu = "pauseScreen";
        DisplayTittle();

        Singleton<GroundController>.instance.showPreviewCamera(status: false);
        if (!BG.activeSelf)
        {
            BG.SetActive(value: true);
        }
        UpdateGamePause();
        midPageGO.SetActive(value: true);
        if (CONTROLLER.PlayModeSelected > 3 && CONTROLLER.PlayModeSelected != 7)
        {
            autoplayBtn.gameObject.SetActive(value: false);
        }
        else
        {
            //autoplayBtn.gameObject.SetActive(value: true);
        }
        Singleton<Tutorial>.instance.getBoolean();
    }

    public void SetPauseScreenTimer(float Seconds)
    {
        secCount = Seconds;
        timerImage.fillAmount = 1f;
        Sequence s = DOTween.Sequence();
        s.SetUpdate(true);
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
        //s.InsertCallback(6f, NoBtnClicked);
        s.InsertCallback(Seconds, ClosePause);
    }

    public void SetSecond()
    {
        timerText.text = secCount.ToString();
        secCount--;
        if (Singleton<GameData>.instance.waitingForOpponentText.text != "Opponent Left the Match..." && ((secCount % 5) == 0 || secCount == 1))
        {
            Singleton<GroundController>.instance.CheckIfOpponentIsConnected();
        }
        if (secCount == 1)
        {
            Time.timeScale = 1f;
            Singleton<GroundController>.instance.CheckIfOpponentIsConnected();
        }

    }
    public void DisplayTittle()
    {
        if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
        {
            PauseScreenTittle1.text = LocalizationData.localizationInstance.getText(247) + " " + LocalizationData.localizationInstance.getText(650) + " " + LocalizationData.localizationInstance.getText(183);
            PauseScreenTittle2.text = string.Empty;
        }

    }
}
