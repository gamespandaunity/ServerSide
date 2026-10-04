//using Photon.Pun;
using POKER;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POKER
{
    public class GameResultsManager : MonoBehaviourPunCallBacksWithNSSCallBacks //MonoBehaviour
    {

        bool _isGameCompleted;

        public bool isGameCompleted
        {
            get { return _isGameCompleted; }
            set { _isGameCompleted = value; }
        }

        #region Creating Instance
        private static GameResultsManager _instance;
        public static GameResultsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<GameResultsManager>();
                return _instance;
            }
        }
        #endregion
        void Awake()
        {
            if (_instance == null)
                _instance = this;
        }















        public void ResetAllDataAndNewGamestart()
        {

            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
                StartCoroutine(ResetAllDataAndNewGameBegin(LocalSettings.ShowingResultAndResetDelayTime - 3));
        }

        IEnumerator ResetAllDataAndNewGameBegin(float timeDelay)
        {
            yield return new WaitForSeconds(timeDelay);


            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
                GameResetManager.Instance.ResetGamePoker();

        }
    }
    // Comparison of player ranks and their high Cards
    #region Comparison of player ranks and their high Cards
    public class PlayerDataNew
    {
        public int rank;
        public int[] intCardsArray;
        public int PlayerViewID;
    }
    public class PlayerComparer : IComparer<PlayerDataNew>
    {
        public int Compare(PlayerDataNew p1, PlayerDataNew p2)
        {
            // Compare ranks first
            int rankComparison = p2.rank.CompareTo(p1.rank);
            if (rankComparison != 0)
            {
                return rankComparison;
            }

            // If ranks are the same, compare integer arrays
            for (int i = 0; i < p1.intCardsArray.Length; i++)
            {
                int intComparison = p2.intCardsArray[i].CompareTo(p1.intCardsArray[i]);
                if (intComparison != 0)
                {
                    return intComparison;
                }
            }

            // If everything is equal, return 0
            return 0;
        }
    }
    #endregion

    #region Comparison of player ranks and their high Cards
    [System.Serializable]
    public class PlayerDataRanks
    {
        public int viewID;
        public int Rank;
        public int Scores;
    }
    public class PlayerDataComparer : IComparer<PlayerDataRanks>
    {
        public int Compare(PlayerDataRanks x, PlayerDataRanks y)
        {
            int result = y.Rank.CompareTo(x.Rank);

            if (result == 0)
            {
                result = y.Scores.CompareTo(x.Scores);
            }

            return result;
        }
    }
    #endregion
}