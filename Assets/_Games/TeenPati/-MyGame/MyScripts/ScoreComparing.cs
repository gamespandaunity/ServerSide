//using com.nss.teenpatti.dream;
//using System;
//using UnityEngine;

//public class ScoreComparing : MonoBehaviour
//{
//    public PlayerData2[] playerData;
//    void Start()
//    {
//        playerData = new PlayerData2[GameManager.Instance.playersList.Count];
//        for (int i = 0; i < playerData.Length; i++)
//        {
//            playerData[i] = new PlayerData2();
//            playerData[i].viewID2 = GameManager.Instance.playersList[i].photonView.ViewID;
//            playerData[i].Rank2 = GameManager.Instance.playersList[i].MyRank;
//            playerData[i].Score2 = GameManager.Instance.playersList[i].MyScores;
//        }
//    }
//}

//[Serializable]
//public class PlayerData2
//{
//    public int viewID2;
//    public int Rank2;
//    public int Score2;
//}
