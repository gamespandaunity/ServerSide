using JetBrains.Annotations;
 
using System.Collections;
using System.Collections.Generic;
using Tanks.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EliminationModeController : MonoBehaviour {
    public static EliminationModeController Instance;

    [FormerlySerializedAs("lastPlace")] public Text finalPlaceText;
    [FormerlySerializedAs("Position")] public Text currentPositionText;
    [FormerlySerializedAs("LapText")] public Text lapDisplayText;
    [FormerlySerializedAs("BestTime")] public Text fastestLapTimeText;
    [FormerlySerializedAs("endGameMenu")] public GameObject gameOverMenu;
    [FormerlySerializedAs("winUI")] public GameObject victoryUI;
    [FormerlySerializedAs("lostUI")] public GameObject defeatUI;
    [FormerlySerializedAs("notificationController")] public NotificationController gameNotificationController;
    [FormerlySerializedAs("lastManUI")] public GameObject lastPlayerStandingUI;
    [FormerlySerializedAs("lastManTimeUI")] public GameObject lastPlayerTimeUI;
    [FormerlySerializedAs("TimeTrialGamePlayeUI")] public GameObject timeTrialGameplayUI;
    [FormerlySerializedAs("TimeTrialGameOverUI")] public GameObject timeTrialEndGameUI;
    [FormerlySerializedAs("timeTrialTimer")] public TimeTrialTimer timeTrialClock;

    [FormerlySerializedAs("m_CountDown")][SerializeField] protected EndGameCountDown gameEndCountdown;


    List<string> notificationsList = new List<string>();

    // Use this for initialization
    void Awake () {
        Instance = this;
	}

    private void Start()
    {
        fastestLapTimeText.text = TimeTrialController.ConvertFloatToTime (PlayerDataController.instance.playerStats.BestTime[PlayerDataController.instance.playerStats.CurrentEnvironment - 1], "#0:00.0");
    }

   // public void showEndRace(bool isWin)
    public void DisplayRaceResult(bool isWin)
    {
        victoryUI.SetActive(isWin);
        defeatUI.SetActive(!isWin);
        
        gameOverMenu.SetActive(true);
        gameEndCountdown.StartCountDown(10, OnCountdownFinished);
        if (isWin)
        {
            int rewardCash = Random.Range(500, 1000);
            int xp = 300;
            PlayerDataController.instance.playerStats.xpoints += xp;

            if (PlayerDataController.instance.playerStats.xpoints > PlayerDataController.instance.playerStats.Rank * 1000)
            {
                MainMenuManager.isRankUp = true;
            }
            PlayerDataController.instance.playerStats.PlayerGold += 400;
            PlayerDataController.instance.playerStats.PlayerCash += rewardCash;
        }
      
        Invoke("FreezVehicle",2);
        
    }
    private void OnCountdownFinished()
    {
        SceneManager.LoadScene("Home");
        //Photon Removal    PhotonNetwork.LeaveRoom();
    }
    public void DisplayNotification(string msg, bool isHighPriority = false)
    {
        if (!notificationsList.Contains(msg))
        {
            if (isHighPriority)
            {
                notificationsList.Insert(0, msg);

            }
            else
            {
                notificationsList.Add(msg);

            }


        }
        if (!gameNotificationController.showingNotification)
        {
            string TempMsg = notificationsList[0];
            notificationsList.Remove(TempMsg);
            gameNotificationController.showNotification(TempMsg);
        }
    }

   // public void OnNotificationEnd()
    public void HandleNotificationEnd()
    {
        Invoke("CheckRemainingNotifications",0.5f);
    }
   // public void setPos(int currentPOs, int TotalPlayer)
    public void UpdatePlayerPosition(int currentPOs, int TotalPlayer)
    {
        currentPositionText.text = "POS: " + currentPOs + "/" + TotalPlayer;

    }

   // public void setLaps(int currentLap, int TotalLaps)
    public void UpdateLapCount(int currentLap, int TotalLaps)
    {
        if (currentLap > TotalLaps)
        {
            currentLap = TotalLaps;
        }
        lapDisplayText.text = "LAP: " + currentLap + "/" + TotalLaps;

    }
   // void CheckRemainingNotifications()
    void ProcessPendingNotifications()
    {
        if (notificationsList.Count>=1)
        {
            string TempMsg = notificationsList[0];
            notificationsList.Remove(TempMsg);
            gameNotificationController.showNotification(TempMsg);
        }
    }
}
