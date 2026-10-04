using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModeInfoController : MonoBehaviour
{
    public Text requiredRank;
    public Text currentRank;

    public void setRankInfo(int requiredInfo)
    {
        requiredRank.text = requiredInfo.ToString();
        currentRank.text = PlayerDataController.instance.playerStats.Rank.ToString();

    }
}
