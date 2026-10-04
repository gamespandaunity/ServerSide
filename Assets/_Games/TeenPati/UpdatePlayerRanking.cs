using TeenPattiGame;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdatePlayerRanking : MonoBehaviour
{

    public GameObject playerRank;

    List<GameObject> playerInfoObj = new List<GameObject>();
    TeenPattiGame.GameManager gameManagerIns;
    List<PlayerInfo> playerInfos = new List<PlayerInfo>();
    public Sprite[] first3Ranksspritesl;


    private void OnEnable()
    {

        gameManagerIns = TeenPattiGame.GameManager.Instance;

        PlayerInfoIns();
    }

    private void OnDisable()
    {
        foreach (var item in playerInfoObj)
        {
            Destroy(item);
        }
        playerInfoObj.Clear();
        playerInfos.Clear();

    }

    public void PlayerInfoIns()
    {
        playerInfoObj = new List<GameObject>();
        playerInfos = new List<PlayerInfo>();


        playerInfos = gameManagerIns.playersList.OrderByDescending(playerinfo => playerinfo.playerCustomProperties.GetCustomBigIntegerData(LocalSettings.totalcashWinLossKey)).ToList();

        for (int i = 0; i < playerInfos.Count; i++)
        {
            GameObject playerRankGameObj = Instantiate(playerRank);
            LocalSettings.SetPosAndRect(playerRankGameObj, playerRank.GetComponentInChildren<RectTransform>(), playerRank.transform.parent);
            string rankText = playerInfos[i].playerCustomProperties.GetCustomBigIntegerData(LocalSettings.totalcashWinLossKey) > 0 ? (i + 1).ToString() : "--";

            playerRankGameObj.transform.GetChild(0).transform.GetComponent<TMP_Text>().text = rankText;
            if (i + 1 <= 3 && rankText != "--")
            {
                playerRankGameObj.transform.GetChild(1).transform.GetComponent<Image>().sprite = first3Ranksspritesl[i];
                playerRankGameObj.transform.GetChild(1).gameObject.SetActive(true);
            }
            playerRankGameObj.transform.GetChild(2).transform.GetComponent<Image>().sprite = playerInfos[i].PlayerAvatorImage.sprite;

            playerRankGameObj.transform.GetChild(3).transform.GetComponent<TMP_Text>().text = playerInfos[i].player_name.text;
            int totalHands = playerInfos[i].playerCustomProperties.GetCustomData(LocalSettings.TotalHandsKey);
            int winHands = playerInfos[i].playerCustomProperties.GetCustomData(LocalSettings.WinHandsKey);


            BigInteger totalWinLoss = playerInfos[i].playerCustomProperties.GetCustomBigIntegerData(LocalSettings.totalcashWinLossKey);
            string totalWinLossT = totalWinLoss > 0 ? "+" + totalWinLoss : "" + totalWinLoss;
            string totalAmount = totalWinLossT;
            TMP_Text TotalWinLossTxt = playerRankGameObj.transform.GetChild(5).transform.GetComponent<TMP_Text>(); if (totalAmount.Contains("-"))
            {
                TotalWinLossTxt.color = Color.red;
                string aa = "-";
                totalWinLossT = totalAmount.Replace(aa, "");
                TotalWinLossTxt.text = "-" + LocalSettings.Rs(totalWinLossT);
            }
            else
            {
                TotalWinLossTxt.color = Color.green;
                TotalWinLossTxt.text = LocalSettings.Rs(totalWinLossT);
            }
            playerRankGameObj.transform.GetChild(4).transform.GetComponent<TMP_Text>().text = (winHands + "/" + totalHands);
            playerInfoObj.Add(playerRankGameObj);
            playerRankGameObj.SetActive(true);
        }


    }
}








