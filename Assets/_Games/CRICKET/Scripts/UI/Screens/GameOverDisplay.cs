using System.Collections;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class GameOverDisplay : Singleton<GameOverDisplay>
{
    public GameObject holder;

    public GameObject screenOne;

    public GameObject screenTwo;


    public GameObject screenFour;

    public GameObject doubleRewards;

    public GameObject cupImg;

    public GameObject matchStatusImg;

    public GameObject MatchStatsSubImg;

    public GameObject PLWon;

    public GameObject achievementsClaim;

    public GameObject achievementsHolder;

    public GameObject noAchievementsHolder;

    public GameObject replayImage;

    public GameObject replayButton;

    public Transform ScoreCardContent;


    public static int pageNumber = 1;

    public Sprite[] matchStatusSprite;

    public Sprite[] cupImgSprite;

    public Sprite[] WinTextSprite;

    public Sprite[] WinningCup;

    public Sprite WinningBG;

    public Image WinningCupImg;

    public ScrollRect achievementsScroll;

    public EndAchievementDetails achievementTab;

    private EndAchievementDetails extraAchievement;

    public Text matchResult;

    public Text tourStatus;

    public Text description;

    public Text matchWonBy;

    public Text coins;

    public Text bonusCoins;

    public Text freeSpins;

    public Text matchXp;

    public Text matchXp1;

    public Text extras;

    public Text score;

    public Text spins;

    public Text extrasSubText;

    public Text scoreSubText;

    public Text achCount;

    public Text replayFees;

    private bool alreadySavedXP;

    public Image XPFill;

    private int XPFromFour;

    private int XPFromMaiden;

    private int XPFromDot;

    private int XPFromSix;

    private int XPFromWicketBowled;

    private int XPFromWicketCatch;

    private int XPFromWicketOthers;

    private int XPFromFifty;

    private int XPFromCentury;

    private int XPFromPerOver;

    private int totalExpInaMatch;

    private int CoinsFromFour;

    private int CoinsFromMaiden;

    private int CoinsFromDot;

    private int CoinsFromSix;

    private int CoinsFromWicketBowled;

    private int CoinsFromWicketCatch;

    private int CoinsFromWicketOthers;

    private int CoinsFromFifty;

    private int CoinsFromCentury;

    private int totalCoinsInaMatch;

    private int myTeamScore;

    private int oppTeamScore;

    private int myTeamWicket;

    private int oppTeamWicket;

    private int coinAmount;

    private int count;

    private float difficultyMultiplier;

    private float oversMultiplier;

    private int milestoneIndex;

    public string matchStatus;

    public string index;

    public string key;

    private string value;

    private string levelValue;

    private string limitValue;

    private string triggerValue;

    private string coinValue;

    private int rewardMultiplier = 1;

    private float XPGained;

    private int amountToReduce;

    private int tempXP;

    private int startingXP;

    private int endingXP;

    private int totalXP;

    [Header("Leaderboard")]
    public GameObject LeaderboardPosition;

    public GameObject rankIncreased1;

    public GameObject rankIncreased2;

    public GameObject achievementPresent;

    public Text positionChanged;

    public Text positionChanged2;

    public Text LBdescription;

    public Text AchievementCoins;

    public Image up;

    public Image down;

    public Image up2;

    public Image down2;

    public GameObject bottomWidget;

    public Text upgradeNameText;

    public Text upgradeAmountText;

    public Text upgradeNameText2;

    public Text upgradeAmountText2;

    public Text replayBtnText;

    private GameObject sideScreen;

    private GameObject mainScreen;

    private float sideScreenPos;

    private float mainScreenPos;

    public GameObject leftBtn;

    public GameObject rightBtn;

    private bool isAnimating;

    [Header("ScreenOneAnimation")]
    public Transform TopImage;

    public Transform LeaderboardTransform;

    public Transform UpgradeTransform;

    public Transform sideTransform;

    public Transform LBIcon;

    public Transform PUIcon;

    public Transform LBIcon2;

    public Transform PUIcon2;

    public Transform ACIcon;

    public Transform PUDesc;

    public Image shine;

    private bool canShowAchievement;

    private bool showUpgrade;

    private bool canShowLeaderboard;

    [Header("ScreenTwoAnimation")]
    public Transform LeaderboardTransform2;
    public Transform UpgradeTransform2;
    public Transform AchievementTransform;

    private int[] xpAmount = new int[34]
    {
        1000, 2500, 5000, 7500, 10000, 12500, 25000, 50000, 75000, 100000,
        125000, 150000, 175000, 200000, 250000, 300000, 350000, 400000, 450000, 500000,
        550000, 600000, 650000, 700000, 750000, 800000, 850000, 900000, 950000, 1000000,
        1500000, 2000000, 2500000, 5000000
    };

    public Image dynamicImage;

    public Image winStatusImg;

    public Sprite[] dynamicSprites;

    public Sprite[] winStatusSprite;

    private float subgrades;

    private float limit;

    private int[] XPmilestones = new int[10] { 0, 5000, 10000, 20000, 35000, 55000, 80000, 110000, 145000, 185000 };

    private void Start()
    {
        alreadySavedXP = false;
        if (CONTROLLER.PlayModeSelected == 0 && ObscuredPrefs.HasKey("doubleRewards"))
        {
            doubleRewards.SetActive(value: true);
            rewardMultiplier = 2;
        }
        else
        {
            doubleRewards.SetActive(value: false);
            rewardMultiplier = 1;
        }
        coinAmount = 0;
        GameOverDetails.InitDescriptionValues();
        HideMe();
    }

    private void CalculateXPforCurrentMatch()
    {

        totalExpInaMatch = XPFromFour + XPFromMaiden + XPFromDot + XPFromSix + XPFromWicketBowled + XPFromWicketCatch + XPFromWicketOthers + XPFromFifty + XPFromCentury + XPFromPerOver;
        totalCoinsInaMatch = (CoinsFromFour + CoinsFromMaiden + CoinsFromDot + CoinsFromSix + CoinsFromWicketBowled + CoinsFromWicketCatch + CoinsFromWicketOthers + CoinsFromFifty + CoinsFromCentury) * rewardMultiplier;
        //SavePlayerPrefs.SaveUserCoins(totalCoinsInaMatch, 0, totalCoinsInaMatch);
        //SavePlayerPrefs.SaveUserXP();
        Singleton<GameData>.instance.DeleteKeys();
    }

    public void ScreenOneDetails()
    {
        index = CONTROLLER.PlayModeSelected.ToString() + GameOverScreen.stage;
        if (matchStatus == "Win")
        {
            MatchWonBy(0);
        }
        else if (matchStatus == "Loss")
        {
            MatchWonBy(1);
        }
        else
        {
            MatchWonBy(0);
        }
        ScreenTwoDetails();
    }


    private void ScreenTwoDetails()
    {
        DecidePageToShow();
        GenerateMultiplierValues();
        GenerateXPGained();
        float num = 0f;
        //SavePlayerPrefs.SaveUserCoins((int)num, 0, (int)num);
        bonusCoins.text = ((float)totalCoinsInaMatch + num).ToString();
    }

    private string GenerateBaseEarningsKey()
    {
        string empty = string.Empty;
        empty += CONTROLLER.PlayModeSelected;

        return empty + "|" + matchStatus;
    }

    private void GenerateMultiplierValues()
    {

        if (CONTROLLER.PlayModeSelected == 8 || CONTROLLER.PlayModeSelected == 0)
        {
            difficultyMultiplier = 1f;
        }
        if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
        {
            if (CONTROLLER.oversSelectedIndex == 0)
            {
                oversMultiplier = 1f;
            }
            else if (CONTROLLER.oversSelectedIndex == 1)
            {
                oversMultiplier = 2.5f;
            }
            else if (CONTROLLER.oversSelectedIndex == 2)
            {
                oversMultiplier = 5f;
            }
        }

    }

    public void ReplayMatch()
    {
        if (CONTROLLER.PlayModeSelected == 0)
        {

            ObscuredPrefs.SetInt("QPPaid", 1);
            CONTROLLER.PlayModeSelected = 0;
            Singleton<GroundController>.instance.ResetAll();
            Singleton<GameData>.instance.ResetVariables();
            HideMe();
            Time.timeScale = 1f;
            CONTROLLER.screenToDisplay = "TossPage";
            CONTROLLER.NewInnings = true;
            CONTROLLER.InningsCompleted = false;
            //Singleton<Firebase_Events>.instance.Firebase_QP_Mode();
            //Singleton<Firebase_Events>.instance.Firebase_QPReplay_Mode();
            Singleton<GameData>.instance.GameQuitted();
        }
        else
        {
            Singleton<GameOverScreen>.instance.GameQuit(1);
        }
    }


    private void GenerateXPGained()
    {
        if (matchStatus == "Win")
        {
            XPGained = (float)(CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 100) * difficultyMultiplier;
        }
    }



    private void AnimateXPbar()
    {
        float fillAmount = (float)startingXP / (float)xpAmount[milestoneIndex];
        XPFill.fillAmount = fillAmount;
        float endValue = (float)endingXP / (float)xpAmount[milestoneIndex];
        XPFill.DOFillAmount(endValue, 2f).SetUpdate(isIndependentUpdate: true);
        matchXp.DOText(endingXP.ToString(), 2f, richTextEnabled: true, ScrambleMode.Numerals).SetUpdate(isIndependentUpdate: true);
    }


    public void ScreenFourDetails(int teamIndex)
    {
        extras.text = CONTROLLER.TeamList[teamIndex].currentMatchExtras.ToString();
        score.text = CONTROLLER.TeamList[teamIndex].currentMatchScores.ToString();
        if (CONTROLLER.TeamList[teamIndex].currentMatchWideBall + CONTROLLER.TeamList[teamIndex].currentMatchNoball != CONTROLLER.TeamList[teamIndex].currentMatchExtras)
        {
            extrasSubText.text = "(W " + (CONTROLLER.TeamList[teamIndex].currentMatchExtras - CONTROLLER.TeamList[teamIndex].currentMatchNoball) + ", NB " + CONTROLLER.TeamList[teamIndex].currentMatchNoball + ")";
        }
        else
        {
            extrasSubText.text = "(W " + CONTROLLER.TeamList[teamIndex].currentMatchWideBall + ", NB " + CONTROLLER.TeamList[teamIndex].currentMatchNoball + ")";
        }
        int num = CONTROLLER.TeamList[teamIndex].currentMatchBalls / 6;
        int num2 = CONTROLLER.TeamList[teamIndex].currentMatchBalls % 6;
        string text = num + "." + num2;
        scoreSubText.text = "(" + CONTROLLER.TeamList[teamIndex].currentMatchWickets + " wkts, " + text + " ov)";
    }

    public void Claim()
    {

    }

    private void DecidePageToShow()
    {
        if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
        {
            ObscuredPrefs.SetInt("QPPaid", 0);
            CONTROLLER.screenToDisplay = "landingPage";
        }

        if (GameOverScreen.stage == 3)
        {

            CONTROLLER.screenToDisplay = "landingPage";
        }
        if (CONTROLLER.screenToDisplay == "landingPage" && CONTROLLER.PlayModeSelected != 0 && myTeamScore < oppTeamScore)
        {
            Singleton<TournamentFailedPopUp>.instance.bottomComponent.SetActive(value: false);
            Singleton<TournamentFailedPopUp>.instance.showMe = true;
        }
    }

    private void AlignImages()
    {
        if (CONTROLLER.PlayModeSelected != 0 && GameOverScreen.stage == 3)
        {
            PLWon.SetActive(value: true);
            dynamicImage.sprite = dynamicSprites[2];
            if (CONTROLLER.PlayModeSelected != 2)
            {
                cupImg.GetComponent<Image>().sprite = cupImgSprite[CONTROLLER.PlayModeSelected];
            }

            WinningCupImg.gameObject.SetActive(value: true);
            dynamicImage.SetNativeSize();
            dynamicImage.gameObject.transform.localPosition = new Vector3(-63.5f, 0f, 0f);
            if (myTeamScore >= oppTeamScore)
            {
                PLWon.GetComponent<Text>().text = LocalizationData.localizationInstance.getText(535) + "\n" + LocalizationData.localizationInstance.getText(536) + "!";
            }
            else
            {
                PLWon.GetComponent<Text>().text = LocalizationData.localizationInstance.getText(537) + "\n" + LocalizationData.localizationInstance.getText(536) + "!";
            }
            MatchStatsSubImg.SetActive(value: false);
            bottomWidget.SetActive(value: false);
        }
        else
        {
            WinningCupImg.gameObject.SetActive(value: false);
            if (myTeamScore >= oppTeamScore || CONTROLLER.DIRECTWIN)
            {
                CONTROLLER.DIRECTWIN = false;
                dynamicImage.sprite = dynamicSprites[0];
                dynamicImage.SetNativeSize();
                dynamicImage.gameObject.transform.localPosition = new Vector3(-253f, 0f, 0f);
            }
            else
            {
                dynamicImage.sprite = dynamicSprites[1];
                dynamicImage.SetNativeSize();
                dynamicImage.gameObject.transform.localPosition = new Vector3(180f, 0f, 0f);
            }
        }
    }

    public void MoveToPreviousPage()
    {
        if (!isAnimating)
        {
            if (pageNumber == 2)
            {
                leftBtn.SetActive(value: false);
                mainScreen = screenFour;
                sideScreen = screenOne;
            }

            if (pageNumber > 1)
            {
                pageNumber--;
                isAnimating = true;
                sideScreenPos = -1500f;
                mainScreenPos = 1500f;
                AnimatePageMovement();
            }
        }
    }

    public void MoveToNextPage()
    {
        GetComponent<ResultManagerForCricket>().FinishPanel.SetActive(false);
        if (!isAnimating)
        {
            if (pageNumber == 1)
            {
                leftBtn.SetActive(value: true);
                Singleton<MatchSummary>.instance.SortPlayerForSummary(0);

                sideScreen = screenFour;
                mainScreen = screenOne;
            }

            else if (pageNumber == 2 || CONTROLLER.PlayModeSelected == 8)
            {
                Singleton<GameOverScreen>.instance.GameQuit(1);
            }
            if (pageNumber < 2)
            {
                isAnimating = true;
                Singleton<NavigationBack>.instance.deviceBack = MoveToPreviousPage;
                pageNumber++;
                sideScreenPos = 1500f;
                mainScreenPos = -1500f;
                AnimatePageMovement();
            }
        }
    }

    private void ScreenCleanup()
    {
        isAnimating = false;
        mainScreen.SetActive(value: false);
        mainScreen.transform.DOLocalMoveX(0f, 0f);
    }

    private void ScreenOneAnimation()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Insert(0f, TopImage.DOScale(Vector3.one * 1.2f, 0.75f));
        sequence.Insert(0.75f, TopImage.DOScale(Vector3.one, 0.1f));
        sequence.OnComplete(LeaderBoardAnim);
        sequence.SetUpdate(isIndependentUpdate: true);
        ShineAnim();
        Sequence s = DOTween.Sequence();
        s.Insert(0.5f, PUIcon.DOPunchRotation(new Vector3(0f, 0f, 8f), 1f, 20, 0.8f)).SetLoops(-1).SetUpdate(isIndependentUpdate: true);
        s.Insert(0.5f, PUDesc.DOScale(Vector3.one * 1.1f, 1f)).SetLoops(-1, LoopType.Yoyo).SetUpdate(isIndependentUpdate: true);
        s.Insert(0.5f, PUIcon.GetComponent<Image>().DOColor(Color.green, 1f)).SetLoops(-1).SetUpdate(isIndependentUpdate: true);
        s.Insert(0.5f, PUIcon2.DOPunchRotation(new Vector3(0f, 0f, 8f), 1f, 20, 0.8f)).SetLoops(-1).SetUpdate(isIndependentUpdate: true);
        s.Insert(0.5f, PUIcon2.GetComponent<Image>().DOColor(Color.green, 1f)).SetLoops(-1).SetUpdate(isIndependentUpdate: true);
    }

    private void ScreenTwoAnimation()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Insert(0f, TopImage.DOScale(Vector3.one * 1.2f, 1f));
        sequence.Insert(1f, TopImage.DOScale(Vector3.one, 0.1f));
        sequence.OnComplete(LeaderBoardAnim);
        sequence.SetUpdate(isIndependentUpdate: true);
        ShineAnim();
    }

    private void LeaderBoardAnim()
    {
        int num = 1;
        num = 1;

        canShowLeaderboard = false;
        LeaderboardPosition.SetActive(value: false);
        LeaderboardTransform2.gameObject.SetActive(value: false);
        if (canShowLeaderboard)
        {
            LeaderboardTransform.DOScale(Vector3.one, 0.5f).SetUpdate(isIndependentUpdate: true).OnComplete(UpgradeAnim);
        }
        else
        {
            UpgradeAnim();
        }
    }

    private void UpgradeAnim()
    {
        if (showUpgrade)
        {
            UpgradeTransform.DOScale(Vector3.one, 0.5f).SetUpdate(isIndependentUpdate: true);
        }
    }

    private void ShineAnim()
    {
        Sequence s = DOTween.Sequence();
        s.Insert(0.75f, shine.transform.DOLocalMoveX(240f, 1.5f)).SetLoops(-1).SetUpdate(isIndependentUpdate: true);
    }

    private void AnimatePageMovement()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Insert(0f, sideScreen.transform.DOLocalMoveX(sideScreenPos, 0f));
        sideScreen.SetActive(value: true);
        sequence.Insert(0f, mainScreen.transform.DOLocalMoveX(mainScreenPos, 0.5f));
        sequence.Insert(0.1f, sideScreen.transform.DOLocalMoveX(0f, 0.75f));
        sequence.SetUpdate(isIndependentUpdate: true);
        sequence.OnComplete(ScreenCleanup);
    }

    public void ScreenTransition(int index)
    {
        switch (index)
        {
            case 0:
                CONTROLLER.pageName = "screen2";
                screenTwo.SetActive(value: true);
                screenOne.SetActive(value: false);
                ScreenTwoDetails();
                break;
            case 1:
                CONTROLLER.pageName = "screen4";
                Singleton<MatchSummary>.instance.SortPlayerForSummary(0);
                screenTwo.SetActive(value: false);
                screenFour.SetActive(value: true);
                break;
            case 2:
                CONTROLLER.pageName = "screen4";
                Singleton<MatchSummary>.instance.SortPlayerForSummary(0);
                screenFour.SetActive(value: true);
                break;
            case -1:
                CONTROLLER.pageName = "screen1";
                screenTwo.SetActive(value: false);
                screenOne.SetActive(value: true);
                break;
            case -2:
                CONTROLLER.pageName = "screen2";
                screenTwo.SetActive(value: true);
                break;
            case -3:
                CONTROLLER.pageName = "screen2";
                screenFour.SetActive(value: false);
                screenTwo.SetActive(value: true);
                ScreenTwoDetails();
                break;
        }
    }

    private void MatchWonBy(int index)
    {
        if (myTeamScore == oppTeamScore)
        {
            if (index == 0)
            {
                matchWonBy.text = string.Empty;
            }
            else
            {
                matchWonBy.text = string.Empty;
            }
        }
        else if (index == 0)
        {
            if (CONTROLLER.meFirstBatting == 1)
            {
                matchWonBy.text = LocalizationData.localizationInstance.getText(471);
            }
            else
            {
                matchWonBy.text = LocalizationData.localizationInstance.getText(474);
            }
        }
        else if (CONTROLLER.meFirstBatting == 0)
        {
            matchWonBy.text = LocalizationData.localizationInstance.getText(471);
        }
        else
        {
            matchWonBy.text = LocalizationData.localizationInstance.getText(474);
        }
    }

    public void ShowMe()
    {
        CONTROLLER.DIRECTWIN = true;
        Singleton<BowlingScoreCard>.instance.Hide(true);
        Singleton<BattingScoreCard>.instance.Hide(true);
        GroundController.isQPMatchStarted = false;
        LBdescription.text = LocalizationData.localizationInstance.getText(534);
        rankIncreased1.SetActive(value: false);
        rankIncreased2.SetActive(value: false);

        LeaderboardTransform2.gameObject.SetActive(value: false);
        bottomWidget.SetActive(value: false);
        if (GameOverScreen.stage == 3)
        {
            bottomWidget.SetActive(value: false);
        }
        PLWon.SetActive(value: false);
        leftBtn.SetActive(value: false);
        pageNumber = 1;
        ObscuredPrefs.DeleteKey("perMatchEdgeCount" + CONTROLLER.PlayModeSelected);
        Singleton<NavigationBack>.instance.deviceBack = null;
        SavePlayerPrefs.SaveSixCount();
        tempXP = CONTROLLER.XPs;
        Singleton<PauseGameScreen>.instance.Hide(boolean: true);
        CONTROLLER.pageName = string.Empty;
        CalculateXPforCurrentMatch();
        myTeamScore = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchScores;
        oppTeamScore = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchScores;
        myTeamWicket = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].currentMatchWickets;
        oppTeamWicket = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].currentMatchWickets;
        if (myTeamScore > oppTeamScore || CONTROLLER.DIRECTWIN)
        {
            cupImg.SetActive(value: false);
            MatchStatsSubImg.SetActive(value: true);
            matchStatus = "Win";
            MatchStatsSubImg.GetComponent<Image>().sprite = WinTextSprite[0];
            sideTransform.localPosition = new Vector3(246f, sideTransform.localPosition.y, sideTransform.localPosition.z);
            KitTable.SetKitValues(5);
            ConstantsData_M.Log("Jeet gaye hain janab");
            if (GameConstants.isWithAI == true)
            {
                ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                GetComponent<ResultManagerForCricket>().WinPlayer(true, staticVariables.UserProfiledata.user._id.ToString());
            }
            else
            {
                ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                GetComponent<ResultManagerForCricket>().WinPlayer(true, staticVariables.UserProfiledata.user._id.ToString());

            }
        }
        else if (myTeamScore < oppTeamScore)
        {
            cupImg.SetActive(value: false);
            MatchStatsSubImg.GetComponent<Image>().sprite = WinTextSprite[2];
            sideTransform.localPosition = new Vector3(-246f, sideTransform.localPosition.y, sideTransform.localPosition.z);
            MatchStatsSubImg.SetActive(value: true);
            matchStatus = "Loss";
            ConstantsData_M.Log("Haar gaye hain bhai jaan");
            if (GameConstants.isWithAI == true)
            {
                ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
                GetComponent<ResultManagerForCricket>().WinPlayer(true, "ai");
            }
            else
            {
                ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId.ToString());
                GetComponent<ResultManagerForCricket>().WinPlayer(true, staticVariables.OpponetProfile.userId.ToString());
            }
        }
        else
        {
            MatchStatsSubImg.SetActive(value: true);
            cupImg.SetActive(value: false);
            MatchStatsSubImg.GetComponent<Image>().sprite = WinTextSprite[1];
            sideTransform.localPosition = new Vector3(246f, sideTransform.localPosition.y, sideTransform.localPosition.z);
            matchStatus = "Tie";
        }
        if (GameOverScreen.stage != 3 || myTeamScore <= oppTeamScore || CONTROLLER.PlayModeSelected != 0)
        {
        }
        //if (Singleton<GameModel>.instance.CheckIfAlreadyAchieved(15) && matchStatus == "Win" && !CONTROLLER.isAutoPlayed)
        //{
        //	Singleton<AchievementsSyncronizer>.instance.UpdateAchievement(15);
        //	AchievementTable.SetAchievementValues(15);
        //}
        if (CONTROLLER.PlayModeSelected == 0)
        {
            replayBtnText.text = LocalizationData.localizationInstance.getText(276);
        }
        else
        {
            replayBtnText.text = LocalizationData.localizationInstance.getText(416);
        }
        if (GameOverScreen.stage == 3)
        {
            replayBtnText.transform.parent.gameObject.SetActive(value: false);
        }
        holder.SetActive(value: true);
        screenOne.SetActive(value: true);
        //ShowAvailableUpgrade();
        XPGeneralAwards();
        ScreenOneDetails();
        ScreenOneAnimation();
        //SendToFirebase();
        AlignImages();
    }

    public void HideMe()
    {
        holder.SetActive(value: false);
        screenOne.SetActive(value: false);
        screenTwo.SetActive(value: false);
        screenFour.SetActive(value: false);
    }


    public void ContentScrollDown()
    {
        ScoreCardContent.transform.DOLocalMove(new Vector3(0f, 150f, 0f), 0.5f).SetUpdate(isIndependentUpdate: true);
    }

    public void XPGeneralAwards()
    {
        if (GameOverScreen.stage == 3)
        {
            int num = 0;

            totalExpInaMatch += num;
        }
    }

}
