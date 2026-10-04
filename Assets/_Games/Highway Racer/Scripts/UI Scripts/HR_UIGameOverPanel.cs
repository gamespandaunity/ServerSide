//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Mirror;
#if PHOTON_UNITY_NETWORKING
//using Photon;
 
#endif

[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Gameover Panel")]
public class HR_UIGameOverPanel : MonoBehaviour {

    public GameObject content;

    [Header("UI Texts On Scoreboard")]
    public Text totalScore;
    public Text subTotalMoney;
    public Text totalMoney;

    public Text totalDistance;
    public Text totalNearMiss;
    public Text totalOverspeed;
    public Text totalOppositeDirection;

    public Text totalDistanceMoney;
    public Text totalNearMissMoney;
    public Text totalOverspeedMoney;
    public Text totalOppositeDirectionMoney;
    public Text winOrLoseText;

    public GameObject restartButton;

    private void OnEnable() {

        HR_GamePlayHandler.OnPlayerDied += HR_PlayerHandler_OnPlayerDied;

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON
   //     HR_PhotonHandler.OnNetworkPlayerWon += HR_GamePlayHandler_OnNetworkPlayerWon;
#endif

    }

    private void HR_PlayerHandler_OnPlayerDied(HR_PlayerHandler player, int[] scores) {

        StartCoroutine(DisplayResults(player, scores));

    }

    public IEnumerator DisplayResults(HR_PlayerHandler player, int[] scores) {

        yield return new WaitForSecondsRealtime(1f);

        // content.SetActive(true);
        bool isMultiplayer = NetworkServer.active || PlayerPrefs.GetInt("Multiplayer", 0) != 0;
        if (isMultiplayer)
        {
            if (NetworkServer.active)
            {
                HighwayCarNetwork networkCar = player.GetComponentInParent<HighwayCarNetwork>();
                if (networkCar != null)
                    networkCar.ServerWasEliminated();
            }
        }
        else
        {
            if (ResultManager.GameSpawnedFinished == false)
            {
                ResultManager.GameSpawnedFinished = true;
                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                if (enemyPrefab != null)
                {
                    "1".Show();
                    // Spawn at position (0,0,0)
                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                    gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    Debug.LogError("WinLose GameManager prefab not found!");
                }
            }
            //ResultManagerForHighwayRacing highwayRacing=GetComponent<ResultManagerForHighwayRacing>();
            //TriggerEvent.instance.winLosePanel.gameObject.SetActive(true);
            //highwayRacing.HandleGameResultAlt(true, staticVariables.UserProfiledata.user._id.ToString());
        }


            totalScore.text = Mathf.Floor(player.score).ToString("F0");
        totalDistance.text = (player.distance).ToString("F2");
        totalNearMiss.text = (player.nearMisses).ToString("F0");
        totalOverspeed.text = (player.highSpeedTotal).ToString("F1");
        totalOppositeDirection.text = (player.opposideDirectionTotal).ToString("F1");

        totalDistanceMoney.text = scores[0].ToString("F0");
        totalNearMissMoney.text = scores[1].ToString("F0");
        totalOverspeedMoney.text = scores[2].ToString("F0");
        totalOppositeDirectionMoney.text = scores[3].ToString("F0");

        totalMoney.text = (scores[0] + scores[1] + scores[2] + scores[3]).ToString();

        gameObject.BroadcastMessage("Animate");
        gameObject.BroadcastMessage("GetNumber");

        if (PlayerPrefs.GetInt("Multiplayer", 0) == 1)
            restartButton.SetActive(false);
        else
            restartButton.SetActive(true);

    }

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON

    private void HR_GamePlayHandler_OnNetworkPlayerWon(int viewID) {

       // if (PhotonView.Find(viewID).GetComponent<HR_PlayerHandler>() == HR_GamePlayHandler.Instance.player)
      //      winOrLoseText.text = "Win!";
      //  else
      //      winOrLoseText.text = "Lose!";

    }

#endif

    private void OnDisable() {

        HR_GamePlayHandler.OnPlayerDied -= HR_PlayerHandler_OnPlayerDied;

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON
    //    HR_PhotonHandler.OnNetworkPlayerWon -= HR_GamePlayHandler_OnNetworkPlayerWon;
#endif

    }

}
