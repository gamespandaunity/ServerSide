using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cricket;
using UnityEngine.Serialization;

public class ScoreBoardMultiPlayer : MonoBehaviour
{
    public static ScoreBoardMultiPlayer instance;

    [FormerlySerializedAs("_ScoreHandler_Multiplayer")] public GameObject ScoreHandlerMultiplayer;
    [FormerlySerializedAs("GreedyFloatingIcon")] public GameObject GreedyFloatingIcon;
    [FormerlySerializedAs("spinsWon")] public GameObject SpinsWonPanel;
    [FormerlySerializedAs("coinsWon")] public GameObject CoinsWonPanel;
    [FormerlySerializedAs("rewardsPanel")] public GameObject RewardsPanel;
    [FormerlySerializedAs("CurrentScore")] public Text CurrentScoreText;
    [FormerlySerializedAs("BallsFaced")] public Text BallsFacedText;
    [FormerlySerializedAs("positionTxt")] public Text PositionText;
    [FormerlySerializedAs("numberTxt")] public Text NumberText;
    [FormerlySerializedAs("rewardsButtonText")] public Text rewardsButtonLabel;
    [FormerlySerializedAs("postionGO")] public GameObject positionGO;
    [FormerlySerializedAs("StartingPlayersCount")] public int startingPlayersCount  = 5;
    [FormerlySerializedAs("MultiplayerTotalOversLabel")] public Text totalOversLabel;
    [FormerlySerializedAs("multiplayerRank")] public Text[] playerRankTexts;
    [FormerlySerializedAs("multiplayerUsername")] public Text[] playerUsernameTexts;
    [FormerlySerializedAs("multiplayerScore")] public Text[] playerScoreTexts;
    [FormerlySerializedAs("multiplayerLastScore")] public Text[] playerLastScoreTexts;
    [FormerlySerializedAs("MultiplayerGameOverPage")] public GameObject gameOverPanel;
    [FormerlySerializedAs("EmoticonPanel")] public GameObject emoticonPanel;
    [FormerlySerializedAs("multiplayerPositionTxt")] public Text finalPositionText1;
    [FormerlySerializedAs("multiplayerPositionTxt2")] public Text finalPositionText2;
    [FormerlySerializedAs("multiplayerEarningsPoints")] public Text earningsPointsText;
    [FormerlySerializedAs("multiplayerBonusPoints")] public Text bonusPointsText;
    [FormerlySerializedAs("multiplayerGameOverBG")] public Image[] gameOverBackgrounds;
    [FormerlySerializedAs("multiplayerGameOverUserBG")] public Sprite[] gameOverUserBackgrounds;
    [FormerlySerializedAs("multiplayerGameOverRank")] public Text[] gameOverRankTexts;
    [FormerlySerializedAs("multiplayerGameOverUsername")] public Text[] gameOverUsernameTexts;
    [FormerlySerializedAs("multiplayerGameOverScore")] public Text[] gameOverScoreTexts;
    [FormerlySerializedAs("oppLeft")] public GameObject opponentLeftNotification;
    [FormerlySerializedAs("oppLeftText")] public Text opponentLeftText;
    [FormerlySerializedAs("WaitForOthersPanel")] public GameObject waitingForOthersPanel;
    [FormerlySerializedAs("WaitForOthersWicketsOverPanel")] public GameObject waitingForWicketsOrOversPanel;
    [FormerlySerializedAs("MultiplayerPlayerScoreBG")] public GameObject[] playerScoreBackgrounds;
    [FormerlySerializedAs("multiplayerFinalBallCountdown")] public GameObject finalBallCountdown;
    [FormerlySerializedAs("ballsLeftInMultiplayer")] public Text ballsLeftText;
    [FormerlySerializedAs("coinsWonText")] public Text coinsWonText;
    [FormerlySerializedAs("tweenPos")] public GameObject[] scoreTweenPositions;

              //Yahan se krna hai
    private string multiplayerPositionLabel;
    private string shareableContent  = string.Empty;
    private float[] rankYPositions  = new float[5] { -76f, -102f, -128f, -154f, -181f };
    private int currentPlayerCount;
    private int temporaryRank;
    private int matchXP;
    private int xpFromFour;
    private int xpFromMaiden;
    private int xpFromDotBall;
    private int xpFromSix;
    private int xpFromWicketBowled;
    private int xpFromWicketCaught;
    private int xpFromWicketOther;
    private int xpFromFifty;
    private int xpFromCentury;
    private int xpPerOver;
    private int currentPlyrCount;
    private int[] ranks;
    private string[] usernames;
    private string[] scores;


    private void CalculateXPforCurrentMatch()
    {
        xpFromFour = Singleton<GameData>.instance.boundaryFoursCount * 48;
        xpFromSix = Singleton<GameData>.instance.boundarySixesCount * 72;
        xpFromFifty = Singleton<GameData>.instance.fiftyRunsCount * 120;
        xpFromCentury = Singleton<GameData>.instance.hundredRunsCount * 240;
        xpPerOver = Singleton<GameData>.instance.oversPlayedCounter * 12;
        matchXP = xpFromFour + xpFromSix + xpFromFifty + xpFromCentury + xpPerOver;
        //SavePlayerPrefs.SaveUserArcadeXPs(totalExpInaMatch, totalExpInaMatch);
        Singleton<GameData>.instance.DeleteKeys();
    }

    public bool ReturnWaitState()
    {
        return waitingForOthersPanel.activeSelf;
    }

    public void ReplayCountdown(int time)
    {
        if (CONTROLLER.tickets >= Multiplayer.entryTickets)
        {
        }
    }

    public void ReplayMatch()
    {
        //ServerManager.Instance.RematchRoom();
    }

    public void UpdateMultiplayerBallsLeft()
    {
        int num = -1;
        if (Multiplayer.overs == 2)
        {
            if (CONTROLLER.currentMatchBalls > 8)
            {
                num = 12 - CONTROLLER.currentMatchBalls;
            }
        }
        else if (Multiplayer.overs == 5 && CONTROLLER.currentMatchBalls > 23)
        {
            num = 30 - CONTROLLER.currentMatchBalls;
        }
        if (num > 0)
        {
            finalBallCountdown.SetActive(value: true);
            if (num == 1)
            {
                ballsLeftText.text = num + " " + LocalizationData.localizationInstance.getText(531);
            }
            else
            {
                ballsLeftText.text = num + " " + LocalizationData.localizationInstance.getText(531);
            }
        }
        else
        {
            finalBallCountdown.SetActive(value: false);
        }
    }

    public void ShowWait()
    {
        waitingForOthersPanel.SetActive(value: true);
    }

    public void HideWait()
    {
        waitingForOthersPanel.SetActive(value: false);
    }

    public void ShowWicketsWait()
    {
        waitingForWicketsOrOversPanel.SetActive(value: true);
    }

    public void HideWicketsWait()
    {
        waitingForWicketsOrOversPanel.SetActive(value: false);
    }

    public void SetMultiplayerGameOverTexts()
    {
        currentPlyrCount = Multiplayer.playerCount;
        ranks = new int[currentPlyrCount];
        usernames = new string[currentPlyrCount];
        scores = new string[currentPlyrCount];
        for (int i = 0; i < currentPlyrCount; i++)
        {
            ranks[i] = Multiplayer.playerScores[i].Rank;
            usernames[i] = Multiplayer.playerScores[i].Username;
            scores[i] = Multiplayer.playerScores[i].Score;
        }
    }

    public void ShowMultiPlayerGameOver()
    {
        CalculateXPforCurrentMatch();
        CONTROLLER.CurrentPage = string.Empty;
        ScoreHandlerMultiplayer.SetActive(value: false);
        emoticonPanel.SetActive(value: false);
        waitingForOthersPanel.SetActive(value: false);
        waitingForWicketsOrOversPanel.SetActive(value: false);
        finalBallCountdown.SetActive(value: false);
        positionGO.SetActive(value: false);
        gameOverPanel.SetActive(value: true);
        CONTROLLER.pageName = "PopupPage";
        int num = 0;
        for (int i = 0; i < 5; i++)
        {
            if (i < currentPlyrCount)
            {
                gameOverRankTexts[i].text = ranks[i].ToString();
                gameOverUsernameTexts[i].text = usernames[i];
                gameOverScoreTexts[i].text = scores[i];
                if (usernames[i] == CONTROLLER.username)
                {
                    gameOverBackgrounds[i].sprite = gameOverUserBackgrounds[0];
                    if (ranks[i] == 1)
                    {
                        if (Multiplayer.roomType == 0)
                        {
                            //spinsWon.SetActive(value: true);
                            //SavePlayerPrefs.SaveSpins(1);
                        }
                        else
                        {
                            SpinsWonPanel.SetActive(value: false);
                        }
                        multiplayerPositionLabel = "ST";
                    }
                    else
                    {
                        SpinsWonPanel.SetActive(value: false);
                        if (Multiplayer.playerScores[i].Rank == 2)
                        {
                            multiplayerPositionLabel = "ND";
                        }
                        else if (Multiplayer.playerScores[i].Rank == 3)
                        {
                            multiplayerPositionLabel = "RD";
                        }
                        else
                        {
                            multiplayerPositionLabel = "TH";
                        }
                    }
                    int num2 = 0;
                    CoinsWonPanel.SetActive(value: false);
                    if (i != startingPlayersCount  - 1 && Multiplayer.roomType == 0)
                    {
                        if (ranks[i] == 1)
                        {
                            num2 = 1000;
                        }
                        else if (ranks[i] == 2)
                        {
                            num2 = 750;
                        }
                        else if (ranks[i] == 3)
                        {
                            num2 = 500;
                        }
                        else if (ranks[i] == 4)
                        {
                            num2 = 250;
                        }
                        if (num2 > 0)
                        {
                            CoinsWonPanel.SetActive(value: true);
                            coinsWonText.text = num2.ToString();
                            //SavePlayerPrefs.SaveUserCoins(num2, 0, num2);
                        }
                    }
                    finalPositionText1.text = ranks[i].ToString();
                    finalPositionText2.text = multiplayerPositionLabel;
                    if (currentPlyrCount == 1)
                    {
                        if (num == 0)
                        {
                            shareableContent  = CONTROLLER.username + " came " + ranks[i] + multiplayerPositionLabel + " in GamesPanda - " + (startingPlayersCount  + 1) + " player Multiplayer match.";
                        }
                        num = 1;
                    }
                    else
                    {
                        if (num == 0)
                        {
                            shareableContent  = CONTROLLER.username + " came " + ranks[i] + multiplayerPositionLabel + " in GamesPanda - " + startingPlayersCount  + " player Multiplayer match.";
                        }
                        num = 1;
                    }
                    gameOverRankTexts[i].color = Color.black;
                    gameOverUsernameTexts[i].color = Color.black;
                    gameOverScoreTexts[i].color = Color.black;
                }
                else
                {
                    gameOverBackgrounds[i].sprite = gameOverUserBackgrounds[1];
                    gameOverRankTexts[i].color = Color.white;
                    gameOverUsernameTexts[i].color = Color.white;
                    gameOverScoreTexts[i].color = Color.white;
                }
            }
            else
            {
                gameOverBackgrounds[i].gameObject.SetActive(value: false);
                gameOverRankTexts[i].text = string.Empty;
                gameOverUsernameTexts[i].text = string.Empty;
                gameOverScoreTexts[i].text = string.Empty;
            }
        }
        earningsPointsText.text = matchXP.ToString();
        if (Multiplayer.roomType == 0)
        {
            if (Multiplayer.overs == 2)
            {
                //FirebaseAnalyticsManager.instance.logEvent("MP_PB_2_Rank", "Multiplayer", CONTROLLER.userID);
            }
            else if (Multiplayer.overs == 5)
            {
                //FirebaseAnalyticsManager.instance.logEvent("MP_PB_5_Rank", "Multiplayer", CONTROLLER.userID);
            }
        }
        else if (Multiplayer.overs == 2)
        {
            //FirebaseAnalyticsManager.instance.logEvent("MP_PV_2_Rank", "Multiplayer", CONTROLLER.userID);
        }
        else if (Multiplayer.overs == 5)
        {
            //FirebaseAnalyticsManager.instance.logEvent("MP_PV_5_Rank", "Multiplayer", CONTROLLER.userID);
        }
        //ServerManager.Instance.Disconnect();
        Multiplayer.roomType = -1;
    }

    public void ToggleRewardsPanel()
    {
        if (rewardsButtonLabel.text.ToUpper() == "REWARDS")
        {
            RewardsPanel.SetActive(value: true);
            rewardsButtonLabel.text = "CLOSE";
        }
        else
        {
            RewardsPanel.SetActive(value: false);
            rewardsButtonLabel.text = "REWARDS";
        }
    }

    public void ShareMultiplayer()
    {
        string text = "GamesPanda";
        string text2 = shareableContent  + " You can download GamesPanda from " + CONTROLLER.BOC_2_Link;
        AndroidJavaClass androidJavaClass = new AndroidJavaClass("android.content.Intent");
        AndroidJavaObject androidJavaObject = new AndroidJavaObject("android.content.Intent");
        androidJavaObject.Call<AndroidJavaObject>("setAction", new object[1] { androidJavaClass.GetStatic<string>("ACTION_SEND") });
        androidJavaObject.Call<AndroidJavaObject>("setType", new object[1] { "text/plain" });
        androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2]
        {
            androidJavaClass.GetStatic<string>("EXTRA_SUBJECT"),
            text
        });
        androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2]
        {
            androidJavaClass.GetStatic<string>("EXTRA_TEXT"),
            text2
        });
        AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject @static = androidJavaClass2.GetStatic<AndroidJavaObject>("currentActivity");
        @static.Call("startActivity", androidJavaObject);
    }

    public void GoToMainMenu()
    {
        //ServerManager.Instance.ExitRoom();
        Singleton<GameData>.instance.ResetCurrentMatchDetails();
        Singleton<GameData>.instance.ResetVariables();
        //Singleton<GameModel>.instance.ResetAllLocalVariables();
        CONTROLLER.PlayModeSelected = -1;
        GroundScriptHandler.Instance.ShowLoadingScreen();
        Singleton<NavigationBack>.instance.deviceBack = null;
        SceneManager.LoadSceneAsync("MainMenu");
    }

    public void SortAndRankScores()
    {
        MultiplayerScore[] array = new MultiplayerScore[Multiplayer.playerCount];
        MultiplayerScore multiplayerScore = array[0];
        for (int i = 0; i < Multiplayer.playerCount; i++)
        {
            array[i] = Multiplayer.playerScores[i];
        }
        for (int j = 0; j < array.Length; j++)
        {
            for (int k = j + 1; k < array.Length; k++)
            {
                if (int.Parse(array[j].Score) < int.Parse(array[k].Score))
                {
                    multiplayerScore = array[j];
                    array[j] = array[k];
                    array[k] = multiplayerScore;
                }
            }
        }
        for (int l = 0; l < array.Length; l++)
        {
            for (int m = l + 1; m < array.Length; m++)
            {
                if (int.Parse(array[l].Score) == int.Parse(array[m].Score) && array[l].Wickets > array[m].Wickets)
                {
                    multiplayerScore = array[l];
                    array[l] = array[m];
                    array[m] = multiplayerScore;
                }
            }
        }
        int num = 1;
        int num2 = 1;
        for (int n = 0; n < array.Length; n++)
        {
            if (n < array.Length - 1)
            {
                if (array[n].Score == array[n + 1].Score && array[n].Wickets == array[n + 1].Wickets)
                {
                    array[n].Rank = num;
                    array[n].RankPos = num2;
                    num2++;
                }
                else
                {
                    array[n].Rank = num;
                    num++;
                    array[n].RankPos = num2;
                    num2++;
                }
            }
            else if (array[n - 1].Score == array[n].Score && array[n - 1].Wickets == array[n].Wickets)
            {
                array[n].Rank = num;
                array[n].RankPos = num2;
                num2++;
            }
            else
            {
                array[n].Rank = num;
                num++;
                array[n].RankPos = num2;
                num2++;
            }
        }
        UpdateMultiplayerScores();
    }

    public void UpdateMultiplayerScores()
    {
        for (int i = 0; i < 5; i++)
        {
            if (i < Multiplayer.playerCount)
            {
                playerRankTexts[i].text = Multiplayer.playerScores[i].Rank.ToString();
                playerUsernameTexts[i].text = Multiplayer.playerScores[i].Username;
                playerScoreTexts[i].text = Multiplayer.playerScores[i].Score.ToString() + "/" + Multiplayer.playerScores[i].Wickets;
                playerLastScoreTexts[i].text = Multiplayer.playerScores[i].LastBallScore.ToString();
                playerScoreBackgrounds[i].SetActive(value: true);
                if (Multiplayer.playerScores[i].PlayerId == CONTROLLER.userID)
                {
                    if (Multiplayer.playerScores[i].Rank == 1)
                    {
                        NumberText.text = 1.ToString();
                        PositionText.text = "ST";
                    }
                    else if (Multiplayer.playerScores[i].Rank == 2)
                    {
                        NumberText.text = 2.ToString();
                        PositionText.text = "ND";
                    }
                    else if (Multiplayer.playerScores[i].Rank == 3)
                    {
                        NumberText.text = 3.ToString();
                        PositionText.text = "RD";
                    }
                    else
                    {
                        NumberText.text = Multiplayer.playerScores[i].Rank.ToString();
                        PositionText.text = "TH";
                    }
                    playerUsernameTexts[i].color = new Color32(byte.MaxValue, 175, 24, byte.MaxValue);
                }
                else
                {
                    playerUsernameTexts[i].color = Color.white;
                }
            }
            else
            {
                playerRankTexts[i].text = string.Empty;
                playerUsernameTexts[i].text = string.Empty;
                playerScoreTexts[i].text = string.Empty;
                playerLastScoreTexts[i].text = string.Empty;
                playerScoreBackgrounds[i].SetActive(value: false);
            }
        }
        UpdateScoreCard();
        AnimateBoard();
        if (currentPlayerCount != Multiplayer.playerCount)
        {
            currentPlayerCount = Multiplayer.playerCount;
        }
    }

    public void AnimateBoard()
    {
        for (int i = 0; i < Multiplayer.playerCount; i++)
        {
            temporaryRank = Multiplayer.playerScores[i].RankPos;
            if (temporaryRank == 0 || temporaryRank > Multiplayer.playerScores.Length)
            {
                break;
            }
            scoreTweenPositions[i].transform.DOLocalMoveY(rankYPositions [temporaryRank - 1], 1f);
        }
    }

    public void OnCompleteAnimateBoard(GameObject go)
    {
    }

    protected void Awake()
    {
        rankYPositions  = new float[5] { 24.65f, -15.05f, -54.75f, -94.45f, -134.15f };
        instance = this;
        currentPlayerCount = Multiplayer.playerCount;
        startingPlayersCount  = Multiplayer.playerCount;
        Hide(boolean: true);
    }

    public void pauseGame()
    {
        Singleton<GameData>.instance.GamePaused(boolean: true);
    }

    public void UpdateScoreCard()
    {
        CurrentScoreText.text = (string.Empty + Singleton<GameData>.instance.scoreDisplayString).ToUpper();
        BallsFacedText.text = "(" + Singleton<GameData>.instance.oversDisplayString + ")";
        if (BallsFacedText.text == "()")
        {
            BallsFacedText.text = "(0.0)";
        }
        totalOversLabel.text = Multiplayer.overs + LocalizationData.localizationInstance.getText(184) + " :";
    }

    public IEnumerator NewOver()
    {
        yield return new WaitForSeconds(0.01f);
        CONTROLLER.ballUpdate[0] = string.Empty;
        CONTROLLER.ballUpdate[1] = string.Empty;
        CONTROLLER.ballUpdate[2] = string.Empty;
        CONTROLLER.ballUpdate[3] = string.Empty;
        CONTROLLER.ballUpdate[4] = string.Empty;
        CONTROLLER.ballUpdate[5] = string.Empty;
    }

    public void Hide(bool boolean)
    {
        float num = 1.33333337f;
        float num2 = Screen.width;
        float num3 = Screen.height;
        float num4 = num2 / num3;
        CONTROLLER.xOffSet = (num - num4) * 300f;
        Singleton<PreviewScreen>.instance.Hide(boolean);
        if (boolean)
        {
            ScoreHandlerMultiplayer.SetActive(value: false);
            waitingForOthersPanel.SetActive(value: false);
            waitingForWicketsOrOversPanel.SetActive(value: false);
            positionGO.SetActive(value: false);
            gameOverPanel.SetActive(value: false);
            emoticonPanel.SetActive(value: false);
            finalBallCountdown.SetActive(value: false);
            return;
        }
        UpdateScoreCard();
        GreedyFloatingIcon.SetActive(value: false);
        //if (CONTROLLER.receivedAdEvent)
        //{
        //	Singleton<AdIntegrate>.instance.HideAd();
        //}
        CONTROLLER.pageName = string.Empty;
        ScoreHandlerMultiplayer.SetActive(value: true);
        positionGO.SetActive(value: true);
        emoticonPanel.SetActive(value: true);
    }

    public void FadeThisObject(bool state)
    {
    }

    public void watchVideo()
    {
    }

    private void ShowToast()
    {
        GameObject original = Resources.Load("Prefabs/Toast") as GameObject;
        GameObject gameObject = Object.Instantiate(original);
        gameObject.name = "Toast";
        gameObject.GetComponent<Toast>().setMessge("No video Available");
    }
}
