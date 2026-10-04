using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeenPattiGame
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

        public void CheckWinnerOfThisGame()
        {
            //MatchHandler.MATCH match = MatchHandler.CurrentMatch;
            // switch (match)
            //  {
            //     case MatchHandler.MATCH.Classic:
            TPLog.Flow("GameResultsManager", "CheckWinnerOfThisGame - closing the hand and comparing cards");
            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {
                PlayerStateManager.Instance.AllPlayersGameCompleted();
                DeclareWinningPlayerOfTeenPatti();
            }

            //         break;
            // }
        }

        #region Calculating and Declare winning player;
        public void DeclareWinningPlayerOfTeenPatti()
        {
            List<PlayerInfo> playersList = new List<PlayerInfo>();
            playersList = PlayerStateManager.Instance.PlayingList;

            TPLog.Flow("GameResultsManager", "DeclareWinningPlayerOfTeenPatti - comparing " + playersList.Count + " hands");
            // Calculating ranks and scores from every players own cards
            for (int i = 0; i < playersList.Count; i++)
            {
                if (playersList[i])
                {
                    GettingPlayerScoreAndRank(playersList[i]);
                    playersList[i].MyRank = PlayerCardsRankAndScoreCalc.Rank;
                    playersList[i].MyScores = PlayerCardsRankAndScoreCalc.Scores;
                    playersList[i].OrgCardValues = PlayerCardsRankAndScoreCalc.CardValuesArrayForPlayer;
                    TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' rank=" + playersList[i].MyRank + " score=" + playersList[i].MyScores);
                }
                else
                {
                    TPLog.Warn("GameResultsManager", "Playing list slot " + i + " is empty while comparing cards");
                }
            }

            PlayerDataNew[] playerData;
            playerData = new PlayerDataNew[playersList.Count];
            for (int i = 0; i < playerData.Length; i++)
            {

                playerData[i] = new PlayerDataNew();
                playerData[i].rank = playersList[i].MyRank;
                playerData[i].intCardsArray = playersList[i].OrgCardValues;
                if (!MatchHandler.isOffline())
                {
                    playerData[i].PlayerViewID = (int)playersList[i].this_photonView.netId;
                }
                if (MatchHandler.isOffline())
                {
                    playerData[i].PlayerViewID = playersList[i].View_ID_Offline;
                    playersList[i].PlayerDummyCardsToShowParent.SetActive(false);
                    playersList[i].PlayerOrignalCardsToShowParent.SetActive(true);
                }
            }
            // Array Sorting w.r.t Player ranks and scores
            Array.Sort(playerData, new PlayerComparer());

            // Debug.LogError(MatchHandler.CurrentMatch);
            // Debug.LogError(NetworkSettings.Instance.currentRoomFilter);

            // Declaring Winner and running RPC 
            if (playerData[0].rank != 3)
            {

                TPLog.Flow("GameResultsManager", "Best hand rank is " + playerData[0].rank + " (no high-card tie rule needed) -> declaring the winner");
                ShowWinnerFromPlayerDataArray(playersList, playerData);
            }
            else
            {
                TPLog.Flow("GameResultsManager", "Best hand is a high-card hand (rank 3) -> checking how many players share it");
                int NumberofPlayersWithRank3 = 0;
                for (int i = 0; i < playersList.Count; i++)
                {
                    if (playersList[i].MyRank == 3)
                        NumberofPlayersWithRank3++;
                }
                if (NumberofPlayersWithRank3 == 1)
                {
                    TPLog.Flow("GameResultsManager", "Only one player has rank 3 -> he wins directly");
                    ShowWinnerFromPlayerDataArray(playersList, playerData);
                }
                else
                {
                    TPLog.Flow("GameResultsManager", NumberofPlayersWithRank3 + " players share rank 3 -> comparing their scores");
                    int TempInt = 0;
                    PlayerDataRanks[] playerDataRanks = new PlayerDataRanks[NumberofPlayersWithRank3];
                    for (int i = 0; i < playersList.Count; i++)
                    {
                        if (playersList[i].MyRank == 3)
                        {
                            playerDataRanks[TempInt] = new PlayerDataRanks();
                            playerDataRanks[TempInt].Rank = playersList[i].MyRank;
                            playerDataRanks[TempInt].Scores = playersList[i].MyScores;
                            if (!MatchHandler.isOffline())
                            {
                                playerDataRanks[TempInt].viewID = (int)playersList[i].this_photonView.netId;
                            }
                            else
                                playerDataRanks[TempInt].viewID = playersList[i].View_ID_Offline;
                            TempInt++;
                        }
                    }
                    // Debug.LogError(playerDataRanks);
                    Array.Sort(playerDataRanks, new PlayerDataComparer());
                    //Debug.LogError(playerDataRanks);

                    for (int i = 0; i < playersList.Count; i++)
                    {
                        if (playerDataRanks[0].viewID == viewID(playersList[i]))
                        {
                            TPLog.Flow("GameResultsManager", "WINNER (score tie-break): '" + playersList[i].name + "'");
                            playersList[i].IAmWinner(true);
                            if (!MatchHandler.isOffline())
                            {
                                if (playersList[i].this_photonView.isOwned)
                                {
                                    TPLog.Flow("GameResultsManager", "The winner is ME -> pot " + Pot.instance.potSize + " credited");
                                    GameManager.Instance.PlayerTotalChipsUpdate(Pot.instance.potSize);

                                    UIManager.Instance.TotalWinsAmount += Pot.instance.potSize;
                                    UIManager.Instance.TotalWinHands++;
                                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                                    Debug.LogError("Win sound is playing");
                                }
                                else
                                {
                                    TPLog.Flow("GameResultsManager", "The winner is the other player -> no chips credited on my side");
                                }
                            }
                            else
                            {
                                if (playersList[i].gameObject.name != LocalSettings.AI_Name)
                                    GameManager.Instance.PlayerTotalChipsUpdate(Pot.instance.potSize);
                                else
                                    GameManager.Instance.AITotalChipsUpdate(Pot.instance.potSize);

                                UIManager.Instance.TotalWinsAmount += Pot.instance.potSize;
                                UIManager.Instance.TotalWinHands++;
                                SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                                SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                                //Debug.LogError("Win sound is playing");
                            }
                        }
                        else
                        {
                            TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' lost the score tie-break");
                            playersList[i].IAmWinner(false);
                        }
                    }
                }
            }
            if (UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.Watching)
            {
                TPLog.Flow("GameResultsManager", "Hand finished for me -> saving my hands/win statistics");
                if (!MatchHandler.isOffline())
                    UIManager.Instance.UpdateTheWinAmount(LocalSettings.totalcashWinLossKey, LocalSettings.TotalHandsKey, LocalSettings.WinHandsKey);
            }

        }


        int viewID(PlayerInfo info)
        {
            if (MatchHandler.isOffline())
                return info.View_ID_Offline;
            return (int)info.this_photonView.netId;
        }
        void ShowWinnerFromPlayerDataArray(List<PlayerInfo> playersList, PlayerDataNew[] playerData)
        {
            for (int i = 0; i < playersList.Count; i++)
            {
                if (playerData[0].PlayerViewID == viewID(playersList[i]))
                {

                    TPLog.Flow("GameResultsManager", "WINNER: '" + playersList[i].name + "' with rank " + playersList[i].MyRank);
                    if (!MatchHandler.isOffline())
                    {
                        if (playersList[i].this_photonView.isOwned)
                        {
                            TPLog.Flow("GameResultsManager", "The winner is ME -> pot " + Pot.instance.potSize + " credited");
                            playersList[i].IAmWinner(true);
                            GameManager.Instance.PlayerTotalChipsUpdate(Pot.instance.potSize);

                            UIManager.Instance.TotalWinsAmount += Pot.instance.potSize;
                            UIManager.Instance.TotalWinHands++;
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                            //Debug.LogError("Win sound is playing");
                        }
                        else
                        {
                            TPLog.Flow("GameResultsManager", "The winner is the other player -> IAmWinner(true) will arrive through his own Rpc");
                        }
                    }
                    else
                    {
                        playersList[i].IAmWinner(true);
                        if (playersList[i].gameObject.name != LocalSettings.AI_Name)
                            GameManager.Instance.PlayerTotalChipsUpdate(Pot.instance.potSize);
                        else
                            GameManager.Instance.AITotalChipsUpdate(Pot.instance.potSize);

                        UIManager.Instance.TotalWinsAmount += Pot.instance.potSize;
                        UIManager.Instance.TotalWinHands++;
                        SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                        SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                    }
                }
                else
                {
                    TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' lost this hand");
                    playersList[i].IAmWinner(false);
                }
            }
        }
        void GettingPlayerScoreAndRank(PlayerInfo playerInfo)
        {

            Transform PlayerCardsParent = playerInfo.PlayerOrignalCardsToShowParent.transform;
            if (PlayerCardsParent.childCount > 3)
                PlayerCardsRankAndScoreCalc.CalculateRankAndScores(PlayerCardsParent.GetChild(3).gameObject.GetComponent<CardProperty>(), PlayerCardsParent.GetChild(4).gameObject.GetComponent<CardProperty>(), PlayerCardsParent.GetChild(5).gameObject.GetComponent<CardProperty>());
            else
                TPLog.Warn("GameResultsManager", "'" + playerInfo.name + "' has no real cards spawned -> rank cannot be calculated");
        }
        #endregion

        #region Calculating and Declare winning of SideShow player;
        public void DeclareWinningSideShowPlayerOfTeenPatti()
        {
            TPLog.Flow("GameResultsManager", "Side-show accepted -> comparing my hand against the requester");
            StartCoroutine(waitforLoadSideShow());
            //PlayerStateManager.Instance.PlayerstateChangeForNextRound();
        }

        IEnumerator waitforLoadSideShow()
        {
            List<PlayerInfo> playersList = new List<PlayerInfo>();
            int nextPlayerInt = PlayerStateManager.Instance.SideShowNext();
            PlayerInfo sideShowPlayerInfo = PlayerStateManager.Instance.PlayingList[nextPlayerInt];

            for (int i = 0; i < 2; i++)
            {
                switch (i)
                {
                    case 0:
                        playersList.Add(UIManager.Instance.GetMyPlayerInfo());
                        break;
                    case 1:
                        playersList.Add(sideShowPlayerInfo);
                        sideShowPlayerInfo.getCurrentPlayerState().UpdateCurrentPlayerState(PlayerState.STATE.Watching);
                        break;
                }
            }

            // Show Cards
            for (int i = 0; i < playersList.Count; i++)
                playersList[i].ShowCardsFromBlind();
            // playersList = PlayerStateManager.Instance.PlayingList;

            // Calculating ranks and scores from every players own cards
            for (int i = 0; i < playersList.Count; i++)
            {
                if (playersList[i])
                {
                    GettingPlayerScoreAndRank(playersList[i]);
                    playersList[i].MyRank = PlayerCardsRankAndScoreCalc.Rank;
                    playersList[i].MyScores = PlayerCardsRankAndScoreCalc.Scores;
                    playersList[i].OrgCardValues = PlayerCardsRankAndScoreCalc.CardValuesArrayForPlayer;
                }
            }

            ///////////////

            //PlayerDataNew[] playerData;
            //playerData = new PlayerDataNew[playersList.Count];
            //for (int i = 0; i < playerData.Length; i++)
            //{
            //    playerData[i] = new PlayerDataNew();
            //    playerData[i].rank = playersList[i].MyRank;
            //    playerData[i].intCardsArray = playersList[i].OrgCardValues;
            //    playerData[i].PlayerViewID = playersList[i].photonView.ViewID;
            //}
            //// Array Sorting w.r.t Player ranks and scores
            //Array.Sort(playerData, new PlayerComparer());

            //// Declaring Winner and running RPC 
            ////for (int i = 0; i < playersList.Count; i++)
            ////{
            ////    if (playerData[0].PlayerViewID == playersList[i].photonView.ViewID)
            ////        playersList[i].IAmWinner(true);
            ////    else
            ////        playersList[i].IAmWinner(false);
            ////}
            //// Declaring Winner and running RPC 
            //yield return new WaitForSeconds(LocalSettings.GameResultWaitingTime);
            //for (int i = 0; i < playersList.Count; i++)
            //{
            //    if (playerData[0].PlayerViewID == playersList[i].photonView.ViewID)
            //    {
            //        // playersList[i].IAmWinner(true);
            //    }
            //    else
            //        playersList[i].getCurrentState().UpdateCurrentState(PlayerState.STATE.Packed);
            //}
            PlayerDataNew[] playerData;
            playerData = new PlayerDataNew[playersList.Count];
            for (int i = 0; i < playerData.Length; i++)
            {
                playerData[i] = new PlayerDataNew();
                playerData[i].rank = playersList[i].MyRank;
                playerData[i].intCardsArray = playersList[i].OrgCardValues;
                playerData[i].PlayerViewID = (int)playersList[i].this_photonView.netId;
            }
            // Array Sorting w.r.t Player ranks and scores
            Array.Sort(playerData, new PlayerComparer());


            yield return new WaitForSeconds(LocalSettings.GameResultWaitingTime);
            // Declaring Winner and running RPC 
            if (playerData[0].rank != 3)
            {
                TPLog.Flow("GameResultsManager", "Side-show result decided on rank " + playerData[0].rank);
                //ShowWinnerFromPlayerDataArray(playersList, playerData);
                for (int i = 0; i < playersList.Count; i++)
                {
                    if (playerData[0].PlayerViewID == (int)playersList[i].this_photonView.netId)
                    {
                        TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' WON the side-show and stays in the hand");
                    }
                    else
                    {
                        TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' LOST the side-show -> he is packed");
                        playersList[i].getCurrentPlayerState().UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                    }
                }
            }
            else
            {
                int NumberofPlayersWithRank3 = 0;
                for (int i = 0; i < playersList.Count; i++)
                {
                    if (playersList[i].MyRank == 3)
                        NumberofPlayersWithRank3++;
                }
                if (NumberofPlayersWithRank3 == 1)
                {
                    TPLog.Flow("GameResultsManager", "Side-show: only one rank-3 hand -> straight result");
                    for (int i = 0; i < playersList.Count; i++)
                    {

                        if (playerData[0].PlayerViewID == (int)playersList[i].this_photonView.netId)
                        {
                            TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' WON the side-show");
                        }
                        else
                        {
                            TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' LOST the side-show -> he is packed");
                            playersList[i].getCurrentPlayerState().UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                        }
                    }
                }
                else
                {
                    int TempInt = 0;
                    PlayerDataRanks[] playerDataRanks = new PlayerDataRanks[NumberofPlayersWithRank3];
                    for (int i = 0; i < playersList.Count; i++)
                    {
                        if (playersList[i].MyRank == 3)
                        {
                            playerDataRanks[TempInt] = new PlayerDataRanks();
                            playerDataRanks[TempInt].Rank = playersList[i].MyRank;
                            playerDataRanks[TempInt].Scores = playersList[i].MyScores;
                            playerDataRanks[TempInt].viewID = (int)playersList[i].this_photonView.netId;
                            TempInt++;
                        }
                    }

                    Array.Sort(playerDataRanks, new PlayerDataComparer());

                    for (int i = 0; i < playersList.Count; i++)
                    {
                        if (playerDataRanks[0].viewID == (int)playersList[i].this_photonView.netId)
                        {
                            TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' WON the side-show on score");
                        }
                        else
                        {
                            TPLog.Flow("GameResultsManager", "'" + playersList[i].name + "' LOST the side-show on score -> he is packed");
                            playersList[i].getCurrentPlayerState().UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                        }
                    }
                }
            }
        }


        #endregion




        public IEnumerator ShowResult(float TimeDelay, string infoText)
        {
            TPLog.Flow("GameResultsManager", "ShowResult - '" + infoText + "', result in " + TimeDelay + "s");
            Game_Play.Instance.ShowInfo(infoText, 3f);
            yield return new WaitForSeconds(TimeDelay);
            ShowingResult();
        }
        public void ShowingResult()
        {
            TPLog.Flow("GameResultsManager", "ShowingResult - opening everybody's cards");
            CheckWinnerOfThisGame();
            for (int i = 0; i < PlayerStateManager.Instance.PlayingList.Count; i++)
                PlayerStateManager.Instance.PlayingList[i].ShowCardsFromBlind();


            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ShowingResults);
            Debug.Log("Here is your error 2......." + RoomStateManager.Instance.CurrentRoomState);
        }



        public void ResetAllDataAndNewGamestart()
        {
            TPLog.Flow("GameResultsManager", "ResetAllDataAndNewGamestart - next hand will be prepared in " + LocalSettings.ShowingResultAndResetDelayTime + "s");
            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
                StartCoroutine(ResetAllDataAndNewGameBegin(LocalSettings.ShowingResultAndResetDelayTime));

        }

        IEnumerator ResetAllDataAndNewGameBegin(float timeDelay)
        {
            "Reset All Data And New Game Begin".Show();
            yield return new WaitForSeconds(timeDelay);

            TPLog.Flow("GameResultsManager", "Result delay finished -> full table reset for the next hand");
            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {
                GameResetManager.Instance.ResetGameTeenPatti();
            }

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