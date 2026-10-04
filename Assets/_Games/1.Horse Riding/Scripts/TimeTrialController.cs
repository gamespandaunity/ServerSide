 
using System.Collections;
using System.Collections.Generic;
using Tanks.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TimeTrialController : MonoBehaviour {
    [FormerlySerializedAs("goldRewardForTimeMode")] public Text timeModeGoldText ;
    [FormerlySerializedAs("cashRewardForTimeMode")] public Text timeModeCashText ;
    [FormerlySerializedAs("besttimeForTimeMode")] public Text timeModeBestTimeText;
    [FormerlySerializedAs("currenttimeForTimeMode")] public Text timeModeCurrentTimeText;
    [FormerlySerializedAs("m_CountDown")][SerializeField] protected EndGameCountDown timeModeCountdown;

    // Use this for initialization
    void OnEnable()
    {
        int rewardCash = Random.Range(500, 1000);
        int rewardGold = Random.Range(200, 300);
        int xp = Random.Range(300, 400);
        timeModeGoldText .text = rewardGold.ToString();
        timeModeCashText .text = rewardCash.ToString();
        float currentTime = (LevelsManager.instance.raceEndTime - LevelsManager.instance.raceStartTime);
        if (currentTime < PlayerDataController.instance.playerStats.BestTime[PlayerDataController.instance.playerStats.CurrentEnvironment-1])
        {
            PlayerDataController.instance.playerStats.BestTime[PlayerDataController.instance.playerStats.CurrentEnvironment - 1] = currentTime;
        }
        timeModeBestTimeText.text = ConvertFloatToTime (PlayerDataController.instance.playerStats.BestTime[PlayerDataController.instance.playerStats.CurrentEnvironment - 1], "#0:00.0");
        timeModeCurrentTimeText.text = ConvertFloatToTime (currentTime, "#0:00.0");

        PlayerDataController.instance.playerStats.PlayerGold += rewardGold;
        PlayerDataController.instance.playerStats.PlayerCash += rewardCash;
        PlayerDataController.instance.playerStats.xpoints += xp;
        if (PlayerDataController.instance.playerStats.xpoints > PlayerDataController.instance.playerStats.Rank * 1000)
        {
            MainMenuManager.isRankUp = true;
        }

        PlayerDataController.instance.SaveData();
        timeModeCountdown.StartCountDown(10, OnCountdownFinished);
       
    }

   // private void FinishedCountDown()
    private void OnCountdownFinished()
    {
        //Photon Removal    PhotonNetwork.LeaveRoom();


    }

    // public static string FloatToTime(float toConvert, string format)
    public static string ConvertFloatToTime (float toConvert, string format)
    {
        switch (format)
        {
            case "00.0":
                return string.Format("{0:00}:{1:0}",
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 10) % 10));//miliseconds
               
            case "#0.0":
                return string.Format("{0:#0}:{1:0}",
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 10) % 10));//miliseconds
             
            case "00.00":
                return string.Format("{0:00}:{1:00}",
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 100) % 100));//miliseconds
               
            case "00.000":
                return string.Format("{0:00}:{1:000}",
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 1000) % 1000));//miliseconds
                
            case "#00.000":
                return string.Format("{0:#00}:{1:000}",
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 1000) % 1000));//miliseconds
               
            case "#0:00":
                return string.Format("{0:#0}:{1:00}",
                    Mathf.Floor(toConvert / 60),//minutes
                    Mathf.Floor(toConvert) % 60);//seconds
              
            case "#00:00":
                return string.Format("{0:#00}:{1:00}",
                    Mathf.Floor(toConvert / 60),//minutes
                    Mathf.Floor(toConvert) % 60);//seconds
             
            case "0:00.0":
                return string.Format("{0:0}:{1:00}.{2:0}",
                    Mathf.Floor(toConvert / 60),//minutes
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 10) % 10));//miliseconds
               
            case "#0:00.0":
                return string.Format("{0:#0}:{1:00}.{2:0}",
                    Mathf.Floor(toConvert / 60),//minutes
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 10) % 10));//miliseconds
               
            case "0:00.00":
                return string.Format("{0:0}:{1:00}.{2:00}",
                    Mathf.Floor(toConvert / 60),//minutes
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 100) % 100));//miliseconds
                
            case "#0:00.00":
                return string.Format("{0:#0}:{1:00}.{2:00}",
                    Mathf.Floor(toConvert / 60),//minutes
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 100) % 100));//miliseconds
                
            case "0:00.000":
                return string.Format("{0:0}:{1:00}.{2:000}",
                    Mathf.Floor(toConvert / 60),//minutes
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 1000) % 1000));//miliseconds
                
            case "#0:00.000":
                return string.Format("{0:#0}:{1:00}.{2:000}",
                    Mathf.Floor(toConvert / 60),//minutes
                    Mathf.Floor(toConvert) % 60,//seconds
                    Mathf.Floor((toConvert * 1000) % 1000));//miliseconds
        }
        return "error";
    }


}
