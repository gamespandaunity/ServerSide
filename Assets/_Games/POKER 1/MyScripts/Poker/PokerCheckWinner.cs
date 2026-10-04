//using Photon.Realtime;
using Mirror;
using POKER;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace POKER
{
    public class PokerCheckWinner : MonoBehaviour
    {
        public RectTransform pokerDummyCardPos;
        public string[] HandRankLabel;
        public CardsContainer ScriptableCards;

        public RectTransform[] CommunityCardsRectTransform;
        [ShowOnly] public List<CardProperty> AllCards;
        [ShowOnly] public List<GameObject> HoleCardsToDestroy = new List<GameObject>();
        [ShowOnly] public CardProperty[] Community_Cards;
        [ShowOnly] public CardProperty[] Altered_Community_Cards = new CardProperty[5];

        PlayerStateManager psm;

        #region Creating Instance
        private static PokerCheckWinner _instance;
        public static PokerCheckWinner Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<PokerCheckWinner>();
                return _instance;
            }
        }
        #endregion


        private void Awake()
        {

            if (_instance == null)
                _instance = this;
        }
        private void Start()
        {

            psm = PlayerStateManager.Instance;
            SettingCards();
            //Invoke(nameof(CreateAndDistributeDummyPokerCards), 1f);
        }

        int temp_random_no;
        int totalPlayersInGame = 5;
        // ####### 1
        public void SettingCards()
        {
            PokerScoreCalculation.Rank = 0;
            resetButton();
            AllCards.Clear();
            AllCards = ScriptableCards.Card.ToList();
            //GenerateCommunityCards();
            if (psm == null)
            {
                psm = PlayerStateManager.Instance;
            }

            // limit is total number of players
            //totalPlayersInGame = psm.PlayingList.Count;
            //for (int i = 0; i < totalPlayersInGame; i++)
            //{
            //    GenerateNewHoleCards(i);
            //}

            //PokerResultCheck(Community_Cards);
            //Debug.Log("<color=green> Community_Cards Rank is " + PokerScoreCalculation.Rank + "</color>");
        }

        // Call on  0 community cards to update The ranks
        public void UpdateRankOnNoCardState()
        {
            PlayerInfo playerInfo = UIManager.Instance.GetMyPlayerInfo();
            //PokerScoreCalculation.CalculateRankAndScores(playerInfo.Hole_Card1, playerInfo.Hole_Card2);
            //int rank = PokerScoreCalculation.Rank;
            //int scores = PokerScoreCalculation.Scores;
            //UpdateHandRankTextRPC(rank, scores);
            if (playerInfo != null)
            {
                if (playerInfo.Hole_Card1 != null)
                {
                    PokerScoreCalculation.CalculateRankAndScores(playerInfo.Hole_Card1, playerInfo.Hole_Card2);
                    int rank = PokerScoreCalculation.Rank;
                    int scores = PokerScoreCalculation.Scores;
                    UpdateHandRankTextRPC(rank, scores);

                }
            }
            if (MatchHandler.isOffline())
            {
                PlayerInfo AiInfo = UIManager.Instance.GetAIPlayerInfo();
                if (AiInfo != null)
                {
                    if (AiInfo.Hole_Card1 != null)
                    {
                        PokerScoreCalculation.CalculateRankAndScores(playerInfo.Hole_Card1, AiInfo.Hole_Card2);
                        int rank = PokerScoreCalculation.Rank;
                        int scores = PokerScoreCalculation.Scores;
                        AiUpdateHandRankTextRPC(rank, scores);

                    }
                }
            }
        }

        // Call on  3 community cards to update The ranks
        public void UpdateRankOnThreeCardState()
        {
            PlayerInfo playerInfo = UIManager.Instance.GetMyPlayerInfo();


            if (playerInfo.Hole_Card1 == null)
                return;
            PokerScoreCalculation.CalculateRankAndScores(Community_Cards[0], Community_Cards[1], Community_Cards[2], playerInfo.Hole_Card1, playerInfo.Hole_Card2);
            int rank = PokerScoreCalculation.Rank;
            int scores = PokerScoreCalculation.Scores;
            UpdateHandRankTextRPC(rank, scores);
            if (MatchHandler.isOffline())
            {
                PlayerInfo AiInfo = UIManager.Instance.GetMyPlayerInfo();
                if (AiInfo.Hole_Card1 == null)
                    return;
                PokerScoreCalculation.CalculateRankAndScores(Community_Cards[0], Community_Cards[1], Community_Cards[2], AiInfo.Hole_Card1, AiInfo.Hole_Card2);
                int rankAi = PokerScoreCalculation.Rank;
                int scoresAi = PokerScoreCalculation.Scores;
                AiUpdateHandRankTextRPC(rankAi, scoresAi);
            }
        }

        // Call on  4 community cards to update The ranks
        public void UpdateRankOnFourCardState()
        {
            int highRankOnFirstRound = 0;
            int highRankOnSecondRound = 0;
            int scoress1 = 0;
            int scoress2 = 0;

            PlayerInfo playerInfo = UIManager.Instance.GetMyPlayerInfo();
            if (playerInfo.Hole_Card1 == null)
                return;
            PokerScoreCalculation.CalculateRankAndScores(Community_Cards[0], Community_Cards[1], Community_Cards[2], Community_Cards[3], playerInfo.Hole_Card1);

            highRankOnFirstRound = PokerScoreCalculation.Rank;
            scoress1 = PokerScoreCalculation.Scores;

            PokerScoreCalculation.CalculateRankAndScores(Community_Cards[0], Community_Cards[1], Community_Cards[2], Community_Cards[3], playerInfo.Hole_Card2);

            highRankOnSecondRound = PokerScoreCalculation.Rank;
            scoress2 = PokerScoreCalculation.Scores;

            int rank = 0;//highRankOnFirstRound >= highRankOnSecondRound ? highRankOnFirstRound : highRankOnSecondRound;
            int score = 0;
            if (highRankOnFirstRound >= highRankOnSecondRound)
            {
                rank = highRankOnFirstRound;
                score = scoress1;
            }
            else
            {
                rank = highRankOnSecondRound;
                score = scoress2;
            }
            PokerScoreCalculation.CalculateRankAndScores(playerInfo.Hole_Card1, playerInfo.Hole_Card2, Community_Cards[0], Community_Cards[1], Community_Cards[2]);
            if (rank < PokerScoreCalculation.Rank)
            {
                rank = PokerScoreCalculation.Rank;
                score = PokerScoreCalculation.Scores;
            }
            PokerScoreCalculation.CalculateRankAndScores(playerInfo.Hole_Card1, playerInfo.Hole_Card2, Community_Cards[0], Community_Cards[1], Community_Cards[3]);
            if (rank < PokerScoreCalculation.Rank)
            {
                rank = PokerScoreCalculation.Rank;
                score = PokerScoreCalculation.Scores;
            }
            PokerScoreCalculation.CalculateRankAndScores(playerInfo.Hole_Card1, playerInfo.Hole_Card2, Community_Cards[0], Community_Cards[2], Community_Cards[3]);
            if (rank < PokerScoreCalculation.Rank)
            {
                rank = PokerScoreCalculation.Rank;
                score = PokerScoreCalculation.Scores;
            }
            PokerScoreCalculation.CalculateRankAndScores(playerInfo.Hole_Card1, playerInfo.Hole_Card2, Community_Cards[1], Community_Cards[2], Community_Cards[3]);
            if (rank < PokerScoreCalculation.Rank)
            {
                rank = PokerScoreCalculation.Rank;
                score = PokerScoreCalculation.Scores;
            }


            UpdateHandRankTextRPC(rank, score);
            if (MatchHandler.isOffline())
            {
                int highRankOnFirstRoundAi = 0;
                int highRankOnSecondRoundAi = 0;
                int scoress1Ai = 0;
                int scoress2Ai = 0;

                PlayerInfo AiInfo = UIManager.Instance.GetAIPlayerInfo();
                if (AiInfo.Hole_Card1 == null)
                    return;
                PokerScoreCalculation.CalculateRankAndScores(Community_Cards[0], Community_Cards[1], Community_Cards[2], Community_Cards[3], AiInfo.Hole_Card1);

                highRankOnFirstRoundAi = PokerScoreCalculation.Rank;
                scoress1Ai = PokerScoreCalculation.Scores;

                PokerScoreCalculation.CalculateRankAndScores(Community_Cards[0], Community_Cards[1], Community_Cards[2], Community_Cards[3], AiInfo.Hole_Card2);

                highRankOnSecondRoundAi = PokerScoreCalculation.Rank;
                scoress2Ai = PokerScoreCalculation.Scores;

                int rankAi = 0;//highRankOnFirstRound >= highRankOnSecondRound ? highRankOnFirstRound : highRankOnSecondRound;
                int scoreAi = 0;
                if (highRankOnFirstRoundAi >= highRankOnSecondRoundAi)
                {
                    rankAi = highRankOnFirstRoundAi;
                    scoreAi = scoress1Ai;
                }
                else
                {
                    rankAi = highRankOnSecondRoundAi;
                    scoreAi = scoress2Ai;
                }
                PokerScoreCalculation.CalculateRankAndScores(AiInfo.Hole_Card1, AiInfo.Hole_Card2, Community_Cards[0], Community_Cards[1], Community_Cards[2]);
                if (rankAi < PokerScoreCalculation.Rank)
                {
                    rankAi = PokerScoreCalculation.Rank;
                    scoreAi = PokerScoreCalculation.Scores;
                }
                PokerScoreCalculation.CalculateRankAndScores(AiInfo.Hole_Card1, AiInfo.Hole_Card2, Community_Cards[0], Community_Cards[1], Community_Cards[3]);
                if (rankAi < PokerScoreCalculation.Rank)
                {
                    rankAi = PokerScoreCalculation.Rank;
                    scoreAi = PokerScoreCalculation.Scores;
                }
                PokerScoreCalculation.CalculateRankAndScores(AiInfo.Hole_Card1, AiInfo.Hole_Card2, Community_Cards[0], Community_Cards[2], Community_Cards[3]);
                if (rankAi < PokerScoreCalculation.Rank)
                {
                    rankAi = PokerScoreCalculation.Rank;
                    scoreAi = PokerScoreCalculation.Scores;
                }
                PokerScoreCalculation.CalculateRankAndScores(AiInfo.Hole_Card1, AiInfo.Hole_Card2, Community_Cards[1], Community_Cards[2], Community_Cards[3]);
                if (rankAi < PokerScoreCalculation.Rank)
                {
                    rankAi = PokerScoreCalculation.Rank;
                    scoreAi = PokerScoreCalculation.Scores;
                }


                AiUpdateHandRankTextRPC(rankAi, scoreAi);

            }
        }
        // Called on button
        // Call on all 5 community cards

        public void UpdateRankOnFiveCardState(PlayerInfo playerInfo)
        {
            playerInfo.PokerPlayerCurrentRank = PokerScoreCalculation.Rank;
            playerInfo.PokerScores = PokerScoreCalculation.Scores;
            playerInfo.HighRankingCards = new CardProperty[5];
            Array.Copy(Community_Cards, playerInfo.HighRankingCards, Altered_Community_Cards.Length);


            ChangeOneCardToAllPositions(playerInfo);
            ChangeBothCardsToAllPositions(playerInfo);
            HighlightHighAndActiveCards(playerInfo);
            if (playerInfo.gameObject.name != LocalSettings.AI_Name)
                UpdateHandRankTextRPC(playerInfo.PokerPlayerCurrentRank, playerInfo.PokerScores);
            else
                AiUpdateHandRankTextRPC(playerInfo.PokerPlayerCurrentRank, playerInfo.PokerScores);
            // Debug.LogError("My Rank: " + playerInfo.PokerPlayerCurrentRank);
            playerInfo.updateHighRankCardsPoker(CreateCardsToIntArray(playerInfo));
            //  Debug.LogError("High cards length: " + playerInfo.HighRankingCards.Length);
        }






        int[] CreateCardsToIntArray(PlayerInfo pInfo)
        {
            int[] ary = new int[5];
            for (int i = 0; i < pInfo.HighRankingCards.Length; i++)
            {
                ary[i] = pInfo.HighRankingCards[i].CardIndexInArray;
            }
            return ary;
        }




        void UpdateHandRankTextRPC(int rank, int scores)
        {
            UIManager.Instance.GetMyPlayerInfo().UpdateHandRankLabel(HandRankLabel[rank - 1], rank, scores);
        }
        void AiUpdateHandRankTextRPC(int rank, int scores)
        {
            UIManager.Instance.GetAIPlayerInfo().UpdateHandRankLabel(HandRankLabel[rank - 1], rank, scores);
        }


        void resetButton()
        {
            foreach (var obj in HoleCardsToDestroy)
            {
                if (obj)
                    Destroy(obj);
            }
            HoleCardsToDestroy.Clear();
        }
        // ####### 2
        //void GenerateCommunityCards()
        //{
        //    Transform parent = CommunityCardsRectTransform[0].transform.parent;
        //    Community_Cards = new CardProperty[5];
        //    for (int i = 0; i < Community_Cards.Length; i++)
        //    {
        //        temp_random_no = GetRandomNo(0, AllCards.Count);
        //        GameObject newCommunityCard = Instantiate(AllCards[temp_random_no].gameObject);
        //        ObjectsToDestroyOnReset.Add(newCommunityCard);
        //        LocalSettings.SetPosAndRect(newCommunityCard, CommunityCardsRectTransform[i], parent);
        //        Community_Cards[i] = newCommunityCard.GetComponent<CardProperty>();
        //        AllCards.RemoveAt(temp_random_no);
        //    }
        //}
        // ####### 3  Called in 1
        //void GenerateNewHoleCards(int playerIndex)
        //{
        //    temp_random_no = GetRandomNo(0, AllCards.Count);
        //    GameObject hole_card1 = Instantiate(AllCards[temp_random_no].gameObject);
        //    ObjectsToDestroyOnReset.Add(hole_card1);
        //    AllCards.RemoveAt(temp_random_no);

        //    temp_random_no = GetRandomNo(0, AllCards.Count);
        //    GameObject hole_card2 = Instantiate(AllCards[temp_random_no].gameObject);
        //    ObjectsToDestroyOnReset.Add(hole_card2);
        //    AllCards.RemoveAt(temp_random_no);

        //    SetHoleCardForPlayer(hole_card1, hole_card2, playerIndex);
        //    //LocalSettings.SetPosAndRect(newHoleCard, CommunityCardsRectTransform[i], parent);
        //}



        //// Called on button
        //// Call on all 5 community cards
        //public void UpdateRankOnFiveCardState(PlayerInfo playerInfo)
        //{
        //    playerInfo.PokerPlayerCurrentRank = PokerScoreCalculation.Rank;
        //    playerInfo.HighRankingCards = new CardProperty[5];
        //    Array.Copy(Community_Cards, playerInfo.HighRankingCards, Altered_Community_Cards.Length);
        //    ChangeOneCardToAllPositions(playerInfo);
        //    ChangeBothCardsToAllPositions(playerInfo);
        //    HighlightHighAndActiveCards(playerInfo);
        //}
        //public List<CardProperty> allCardsToCompare;
        public List<CardProperty> AllCommAndHoleCardsForPlayer;
        void HighlightHighAndActiveCards(PlayerInfo playerInfo)
        {
            if (HoleCardsToDestroy.Count > 0)
            {
                foreach (GameObject obj in HoleCardsToDestroy)
                {
                    if (obj != null)
                        obj.GetComponent<Image>().color = Color.white;
                }
            }
            bool isCardMatched = false;
            playerInfo.remainingCards = new List<CardProperty>();
            playerInfo.remainingCards.Clear();
            AllCommAndHoleCardsForPlayer = new List<CardProperty>();
            AllCommAndHoleCardsForPlayer.Clear();
            AllCommAndHoleCardsForPlayer.AddRange(Community_Cards);
            //AllCommAndHoleCardsForPlayer.Add(player_Hole_Cards[playerIndex].org_card1);
            //AllCommAndHoleCardsForPlayer.Add(player_Hole_Cards[playerIndex].org_card2);
            AllCommAndHoleCardsForPlayer.Add(playerInfo.Hole_Card1);
            AllCommAndHoleCardsForPlayer.Add(playerInfo.Hole_Card2);


            for (int i = 0; i < AllCommAndHoleCardsForPlayer.Count; i++)
            {
                isCardMatched = false;
                for (int j = 0; j < playerInfo.HighRankingCards.Length; j++)
                {
                    if (AllCommAndHoleCardsForPlayer[i] == playerInfo.HighRankingCards[j])
                    {
                        isCardMatched = true;
                        break;
                    }
                }
                if (!isCardMatched)
                {
                    if (AllCommAndHoleCardsForPlayer[i] != null)
                    {
                        AllCommAndHoleCardsForPlayer[i].GetComponent<Image>().color = Color.cyan;
                        playerInfo.remainingCards.Add(AllCommAndHoleCardsForPlayer[i]);
                    }
                }
            }
        }

        void ChangeOneCardToAllPositions(PlayerInfo playerInfo)
        {
            for (int i = 0; i < Community_Cards.Length; i++)
            {
                // replacing One card at a time
                ChangeOneCardToAllPos(0, i, playerInfo);
                ChangeOneCardToAllPos(1, i, playerInfo);
            }
        }
        // replacing One card at a time
        void ChangeOneCardToAllPos(int HoleCardIndex, int HoleCardPosInCommCardArry, PlayerInfo playerInfo)
        {
            Altered_Community_Cards = new CardProperty[5];
            Array.Copy(Community_Cards, Altered_Community_Cards, Community_Cards.Length);
            Altered_Community_Cards[HoleCardPosInCommCardArry] = HoleCardIndex == 0 ? playerInfo.Hole_Card1 : playerInfo.Hole_Card2;

            PokerResultCheck(Altered_Community_Cards);
            int rankOfCards = PokerScoreCalculation.Rank;
            int scoreOfCards = PokerScoreCalculation.Scores;
            if (rankOfCards > playerInfo.PokerPlayerCurrentRank)
            {
                playerInfo.PokerPlayerCurrentRank = rankOfCards;
                playerInfo.PokerScores = scoreOfCards;
                playerInfo.HighRankingCards = new CardProperty[5];
                Array.Copy(Altered_Community_Cards, playerInfo.HighRankingCards, Altered_Community_Cards.Length);

                //player_Hole_Cards[playerIndex].CardToReplacePosition.Clear();
                //player_Hole_Cards[playerIndex].CardToReplacePosition.Add(HoleCardPosInCommCardArry);

                Debug.Log("<color=cyan>Player Index: " + playerInfo.name +
                    "    Hole Card: " + HoleCardIndex +
                    "   Card Pos in Comm cards: " + HoleCardPosInCommCardArry +
                    "    Now Rank is " + rankOfCards + "</color>");

                playerInfo.HandRankLabelTxt.text = HandRankLabel[rankOfCards - 1];
            }
        }

        void ChangeBothCardsToAllPositions(PlayerInfo playerInfo)
        {
            for (int i = 0; i < Altered_Community_Cards.Length; i++)
            {
                for (int j = i + 1; j < Altered_Community_Cards.Length; j++)
                {
                    Altered_Community_Cards = new CardProperty[5];
                    Array.Copy(Community_Cards, Altered_Community_Cards, Community_Cards.Length);
                    Altered_Community_Cards[i] = playerInfo.Hole_Card1;
                    Altered_Community_Cards[j] = playerInfo.Hole_Card2;

                    PokerResultCheck(Altered_Community_Cards);
                    int rankOfCards = PokerScoreCalculation.Rank;
                    int scoreOfCards = PokerScoreCalculation.Scores;
                    if (rankOfCards > playerInfo.PokerPlayerCurrentRank)
                    {
                        playerInfo.PokerPlayerCurrentRank = rankOfCards;
                        playerInfo.PokerScores = scoreOfCards;
                        playerInfo.HighRankingCards = new CardProperty[5];
                        Array.Copy(Altered_Community_Cards, playerInfo.HighRankingCards, Altered_Community_Cards.Length);
                        Debug.Log("<color=cyan>Player Index: " + playerInfo.name +
                            "    Hole Card 1 Pos: " + i +
                            "    Hole Card 2 Pos: " + j +
                            "    Now Rank is " + rankOfCards + "</color>");
                        //player_Hole_Cards[playerIndex].HandRankLabelTxt.text = HandRankLabel[rankOfCards - 1];
                        playerInfo.HandRankLabelTxt.text = HandRankLabel[rankOfCards - 1];
                    }
                }
            }
        }


        // ####### 4
        void PokerResultCheck(CardProperty[] temp_cards)
        {
            PokerScoreCalculation.CalculateRankAndScores(temp_cards[0], temp_cards[1], temp_cards[2], temp_cards[3], temp_cards[4]);
        }

        int GetRandomNo(int minInclusive, int maxExclusive)
        {
            return UnityEngine.Random.Range(minInclusive, maxExclusive);
        }


        //public void DeclareWinningPlayerOfPoker()
        //{
        //    List<PlayerInfo> playersList = new List<PlayerInfo>();
        //    playersList = PlayerStateManager.Instance.PlayingList;

        //    // Calculating ranks and scores from every players own cards
        //    for (int i = 0; i < playersList.Count; i++)
        //    {
        //        if (playersList[i])
        //        {
        //            // First calculate the scores, ranks, and cards array and then
        //            // set score, rank and cards array in player info script for each player
        //            //GettingPlayerScoreAndRank(playersList[i]);
        //            //playersList[i].MyRank = PlayerCardsRankAndScoreCalc.Rank;
        //            //playersList[i].MyScores = PlayerCardsRankAndScoreCalc.Scores;
        //            //playersList[i].OrgCardValues = PlayerCardsRankAndScoreCalc.CardValuesArrayForPlayer;
        //        }
        //    }
        //}

        #region  arranging players to an array w.r.t ranks and high card

        //public PlayerDataPoker[] playerDataPoker;
        public PlayerDataPoker[] playersArray;
        public void DeclareWinnerOfPoker()
        {
            #region Previous Logic
            //playerDataPoker = new PlayerDataPoker[psm.PlayingList.Count];
            //for (int i = 0; i < psm.PlayingList.Count; i++)
            //{
            //    PlayerInfo pInfo = psm.PlayingList[i];
            //    playerDataPoker[i] = new PlayerDataPoker();
            //    playerDataPoker[i].rank = pInfo.PokerPlayerCurrentRank;
            //    playerDataPoker[i].PlayerViewID = pInfo.photonView.ViewID;
            //    playerDataPoker[i].intCardsArray = new int[5];
            //    for (int j = 0; j < 5; j++)
            //    {
            //        playerDataPoker[i].intCardsArray[j] = new int();
            //        playerDataPoker[i].intCardsArray[j] = pInfo.HighRankingCards[j].Power;
            //    }
            //}

            //Array.Sort(playerDataPoker, new PlayerComparerPoker());
            //for (int i = 0; i < psm.PlayingList.Count; i++)
            //{
            //    PlayerInfo pInfo = psm.PlayingList[i];
            //    if (pInfo.photonView.ViewID == playerDataPoker[0].PlayerViewID)
            //    {
            //        pInfo.IAmWinner(true);
            //        if (pInfo.photonView.IsMine)
            //        {
            //            LocalSettings.SetPokerBuyInChips(PokerActionPanel.Instance.TotalPotAmount());
            //            GoldWinLoose.Instance.SendGold(GoldWinLoose.Trans.win, PokerActionPanel.Instance.TotalPotAmount().ToString());
            //            pInfo.player.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, LocalSettings.GetPokerBuyInChips());
            //            Debug.LogError(LocalSettings.Rs(pInfo.player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey)));
            //        }


            //    }
            //   // pInfo.PokerTotalCashTxt.text = LocalSettings.Rs(pInfo.player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey));
            //}
            #endregion

            foreach (PlayerInfo pInfo in GameManager.Instance.playersList)
                pInfo.CashAllInIndicator.SetActive(false);
            int numberOfPlayers = psm.PlayingList.Count;
            if (playersArray.Length > 0)
                Array.Clear(playersArray, 0, playersArray.Length);

            playersArray = new PlayerDataPoker[numberOfPlayers];
            for (int i = 0; i < numberOfPlayers; i++)
            {
                PlayerInfo pInfo = psm.PlayingList[i];
                int[] cardsIntArray = new int[5];
                for (int j = 0; j < pInfo.HighRankingCards.Length; j++)
                    cardsIntArray[j] = pInfo.HighRankingCards[j].Power;

                int[] holeCardsIntArray = new int[2];
                holeCardsIntArray[0] = pInfo.Hole_Card1.Power;
                holeCardsIntArray[1] = pInfo.Hole_Card2.Power;

                Array.Sort(cardsIntArray);
                Array.Reverse(cardsIntArray);

                Array.Sort(holeCardsIntArray);
                Array.Reverse(holeCardsIntArray);

                //int playerViewID, int rank, int scores, int[] HighRankingCards, int[] HoleCards
                PlayerDataPoker onePlayerData;
                if (!MatchHandler.isOffline())
                    onePlayerData = new PlayerDataPoker(pInfo.playerCustomProperties.name, pInfo.View_ID_Offline, pInfo.PokerPlayerCurrentRank, pInfo.PokerScores, cardsIntArray, holeCardsIntArray);
                else
                {

                    onePlayerData = new PlayerDataPoker(pInfo.gameObject.name, pInfo.View_ID_Offline, pInfo.PokerPlayerCurrentRank, pInfo.PokerScores, cardsIntArray, holeCardsIntArray);


                }


                // Add the player to the array
                playersArray[i] = onePlayerData;
            }

            // Sort the array of players
            Array.Sort(playersArray, new PokerPlayerComparer());

            // Dividing reward if two players have same rank and same hole cards 
            bool isRewardSplit = false;
            if (playersArray[0].rank == playersArray[1].rank)
            {
                if (playersArray[0].HoleCards[0] == playersArray[1].HoleCards[0] && playersArray[0].HoleCards[1] == playersArray[1].HoleCards[1])
                {
                    isRewardSplit = true;
                }
            }

            for (int i = 0; i < psm.PlayingList.Count; i++)
            {
                PlayerInfo pInfo = psm.PlayingList[i];
                Debug.Log("<color=yellow>Player Name: " + pInfo.View_ID_Offline + "   Rank: " + pInfo.PokerPlayerCurrentRank + "   Score: " + pInfo.PokerScores + "</color>");
                Debug.Log("<color=yellow>Player Name: " + playersArray[0].playerViewID + "   Rank: " + playersArray[0].playerName + "   Score: " + playersArray[0].scores + "</color>");
                Debug.Log("<color=yellow>Player Name: " + playersArray[1].playerViewID + "   Rank: " + playersArray[1].playerName + "   Score: " + playersArray[1].scores + "</color>");
                if (!MatchHandler.isOffline())
                {
                    if (pInfo.View_ID_Offline== playersArray[0].playerViewID)
                    {
                        // Debug.LogError("Player Name: " + pInfo.player.NickName);
                        pInfo.IAmWinner(true);
                        BigInteger totaPotAmount = PokerActionPanel.Instance.TotalPotAmount();
                        if (isRewardSplit)
                            totaPotAmount /= 2;
                        if (pInfo.IsMine())
                        {
                            //LocalSettings.SetPokerBuyInChips(PokerActionPanel.Instance.TotalPotAmount());
                            LocalSettings.SetPokerBuyInChips(totaPotAmount);
                            UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());
                            pInfo.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, LocalSettings.GetPokerBuyInChips());
                            //UIManager.Instance.TotalWinsAmount += PokerActionPanel.Instance.TotalPotAmount();
                            UIManager.Instance.TotalWinsAmount += totaPotAmount;
                            UIManager.Instance.TotalWinHands++;
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                        }

                        //PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(pInfo.photonView.ViewID, PokerActionPanel.Instance.TotalPotAmount());
                        //  PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(pInfo.photonView.ViewID, totaPotAmount);
                    }
                }
                else
                {
                    if (pInfo.View_ID_Offline == playersArray[0].playerViewID)
                    {
                        // Debug.LogError("Player Name: " + pInfo.player.NickName);
                        pInfo.IAmWinner(true);
                        BigInteger totaPotAmount = PokerActionPanel.Instance.TotalPotAmount();
                        if (isRewardSplit)
                            totaPotAmount /= 2;
                        if (pInfo.gameObject.name != LocalSettings.AI_Name)
                        {
                            //LocalSettings.SetPokerBuyInChips(PokerActionPanel.Instance.TotalPotAmount());
                            LocalSettings.SetPokerBuyInChips(totaPotAmount);
                            UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());

                            //UIManager.Instance.TotalWinsAmount += PokerActionPanel.Instance.TotalPotAmount();
                            UIManager.Instance.TotalWinsAmount += totaPotAmount;
                            UIManager.Instance.TotalWinHands++;
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                        }
                        else
                        {
                            LocalSettings.AI_Amount += totaPotAmount;


                            //UIManager.Instance.TotalWinsAmount += PokerActionPanel.Instance.TotalPotAmount();
                            UIManager.Instance.TotalWinsAmount += totaPotAmount;
                            UIManager.Instance.TotalWinHands++;
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                        }

                        //PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(pInfo.photonView.ViewID, PokerActionPanel.Instance.TotalPotAmount());
                        //  PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(pInfo.photonView.ViewID, totaPotAmount);
                    }


                }
                if (isRewardSplit)
                {
                    if (!MatchHandler.isOffline())
                    {
                        if (pInfo.View_ID_Offline == playersArray[1].playerViewID)
                        {
                            // Debug.LogError("Player Name: " + pInfo.player.NickName);
                            pInfo.IAmWinner(true);
                            BigInteger totaPotAmount = PokerActionPanel.Instance.TotalPotAmount() / 2;
                            if (pInfo.IsMine())
                            {
                                LocalSettings.SetPokerBuyInChips(totaPotAmount);
                                //GoldWinLoose.Instance.SendGold(GoldWinLoose.Trans.win, PokerActionPanel.Instance.TotalPotAmount().ToString());
                                UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());
                                pInfo.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, LocalSettings.GetPokerBuyInChips());
                                UIManager.Instance.TotalWinsAmount += (totaPotAmount);
                                UIManager.Instance.TotalWinHands++;

                                //SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                                //SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                            }

                            //PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(pInfo.photonView.ViewID, totaPotAmount);
                        }
                    }
                    else
                    {
                        if (pInfo.View_ID_Offline == playersArray[1].playerViewID)
                        {
                            // Debug.LogError("Player Name: " + pInfo.player.NickName);
                            pInfo.IAmWinner(true);
                            BigInteger totaPotAmount = PokerActionPanel.Instance.TotalPotAmount() / 2;
                            if (pInfo.gameObject.name != LocalSettings.AI_Name)
                            {
                                LocalSettings.SetPokerBuyInChips(totaPotAmount);
                                //GoldWinLoose.Instance.SendGold(GoldWinLoose.Trans.win, PokerActionPanel.Instance.TotalPotAmount().ToString());
                                UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());

                                UIManager.Instance.TotalWinsAmount += (totaPotAmount);
                                UIManager.Instance.TotalWinHands++;

                                //SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                                //SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                            }
                            else
                            {
                                LocalSettings.AI_Amount += totaPotAmount;
                                //GoldWinLoose.Instance.SendGold(GoldWinLoose.Trans.win, PokerActionPanel.Instance.TotalPotAmount().ToString());



                            }

                            //PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(pInfo.photonView.ViewID, totaPotAmount);
                        }

                    }
                }
            }

            if (UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.Watching)
            {
                if (!MatchHandler.isOffline())
                    UIManager.Instance.UpdateTheWinAmount(LocalSettings.totalcashWinLossKey, LocalSettings.TotalHandsKey, LocalSettings.WinHandsKey);
            }
            PokerManager.Instance.HistoryFinalCall();
            // PokerHistory.Instance.SortPokerRecord();
        }


        private void amountPokcerUpdate(PlayerInfo info)
        {

        }
        #endregion


        #region Poker Dummy cards Distribution
        List<GameObject> DummyPokerCards;
        int numberOfPlayers;
        public void CreateAndDistributeDummyPokerCards()
        {
            numberOfPlayers = psm.PlayingList.Count;
            //numberOfPlayers = GameManager.Instance.playersList.Count;

            DummyPokerCards = new List<GameObject>();
            for (int i = 0; i < numberOfPlayers * 2; i++)
            {
                GameObject dumyCard = Instantiate(GameManager.Instance.DummyCardPrefab);
                DummyPokerCards.Add(dumyCard);
                LocalSettings.SetPosAndRect(dumyCard, pokerDummyCardPos, pokerDummyCardPos.transform.parent);
            }
            StartCoroutine(DistributeDummyCards());
        }
        IEnumerator DistributeDummyCards()
        {
            // Server has no local player and no UI; card animation is client-only.
            // Prevent NullReferenceException on GetMyPlayerInfo() and skip useless animation work.
            if (!MatchHandler.isOffline() && NetworkServer.active)
                yield break;

            int cardIndex = 0;
            for (int i = 0; i < 2; i++)
            {
                //numberOfPlayers = psm.PlayingList.Count;
                for (int j = 0; j < psm.PlayingList.Count; j++)
                {
                    GameObject dmyCard = DummyPokerCards[cardIndex];
                    cardIndex++;
                    GameObject objToAct = null;
                    if (psm.PlayingList[j])
                    {
                        if (!MatchHandler.isOffline())
                        {
                            if (psm.PlayingList[j].IsMine())
                            {
                                if (i == 0)
                                {
                                    if (psm.PlayingList[j])
                                        if (psm.PlayingList[j].Hole_Card1)
                                            objToAct = psm.PlayingList[j].Hole_Card1.gameObject;
                                }
                                else
                                {
                                    if (psm.PlayingList[j])
                                        if (psm.PlayingList[j].Hole_Card2)
                                            objToAct = psm.PlayingList[j].Hole_Card2.gameObject;
                                }
                            }
                            else
                                objToAct = psm.PlayingList[j].DummyCardsParent.transform.GetChild(i).gameObject;
                        }
                        else
                        {
                            if (psm.PlayingList[j].gameObject.name != LocalSettings.AI_Name)
                            {
                                if (i == 0)
                                {
                                    if (psm.PlayingList[j])
                                        if (psm.PlayingList[j].Hole_Card1)
                                            objToAct = psm.PlayingList[j].Hole_Card1.gameObject;
                                }
                                else
                                {
                                    if (psm.PlayingList[j])
                                        if (psm.PlayingList[j].Hole_Card2)
                                            objToAct = psm.PlayingList[j].Hole_Card2.gameObject;
                                }
                            }
                            else
                                objToAct = psm.PlayingList[j].DummyCardsParent.transform.GetChild(i).gameObject;
                        }
                    }
                    if (objToAct)
                    {
                        StartCoroutine(GameManager.Instance.PlayAnimation(dmyCard.transform, psm.PlayingList[j].DummyCardsParent.transform.GetChild(i).transform, objToAct));
                    }
                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.CardFlip, false);
                    yield return new WaitForSeconds(0.51f);
                }
            }
            for (int j = 0; j < psm.PlayingList.Count; j++)
            {
                if (!MatchHandler.isOffline())
                {
                    // Use isOwned (Mirror's per-client authority flag) to identify the local player.
                    // View_ID_Offline is a legacy PUN2 field that is never assigned a unique value
                    // per-player in Mirror multiplayer (all default to 0), so the old comparison
                    // would match ALL players and reveal every opponent's hole cards.
                    if (psm.PlayingList[j].IsMine())
                    {
                        if (psm.PlayingList[j].Hole_Card1 != null)
                        {
                            psm.PlayingList[j].Hole_Card1.gameObject.SetActive(true);
                            psm.PlayingList[j].Hole_Card2.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    if (psm.PlayingList[j].View_ID_Offline == UIManager.Instance.GetMyPlayerInfo().View_ID_Offline)
                    {
                        if (psm.PlayingList[j].Hole_Card1 != null)
                        {
                            psm.PlayingList[j].Hole_Card1.gameObject.SetActive(true);
                            psm.PlayingList[j].Hole_Card2.gameObject.SetActive(true);
                        }
                    }
                }
            }
            ClearDummyCardsList();
            // GameStartManager.Instance._1stCurrentChaalBool = true;
            ChageRoomStateToGamePlaying();
            UIManager.Instance.GetMyPlayerInfo().HandRankLabelTxt.transform.parent.gameObject.SetActive(true);
            // Debug.LogError("Here we Go ... 2");
            //UpdateRankOnNoCardState();
            //UpdateRankOnThreeCardState();
            //UpdateRankOnFourCardState();
            //UpdateRankOnFiveCardState(UIManager.Instance.GetMyPlayerInfo());

            PokerStatesManager.Instance.updatePokerState(PokerState.NO_COMMUNITY_CARDS);
        }

        void ClearDummyCardsList()
        {
            foreach (GameObject card in DummyPokerCards)
            {
                Destroy(card);
            }
            DummyPokerCards.Clear();
        }

        void ChageRoomStateToGamePlaying()
        {
            // In dedicated server mode, only clients run DistributeDummyCards() because
            // RiseEventRpc(PokerCardsArray) is a [ClientRpc] that the server never receives.
            // PlayingList is correctly populated via currentPlayerStateRef.currentState
            // which is synced to all machines by [ClientRpc] UpdateCurrentPlayerStateOnNetwork.
            Debug.Log($"[PokerCheckWinner] ChageRoomStateToGamePlaying called. isServer={Mirror.NetworkServer.active}");
         //   if (PlayerStateManager.Instance.PlayingList.Count > 1)
                RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsPlaying);
         //   else
             //   RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ShowingResults);
        }

        #endregion
    }

    #region Comparison of player ranks and their high Cards

    [Serializable]
    public class PlayerDataPoker
    {
        public string playerName;
        public int playerViewID;
        public int rank;
        public int scores;
        public int[] HighRankingCards = new int[5];
        public int[] HoleCards = new int[2];

        public PlayerDataPoker(string namee, int playerViewID, int rank, int scores, int[] HighRankingCards, int[] HoleCards)
        {
            this.playerName = namee;
            this.playerViewID = playerViewID;
            this.rank = rank;
            this.scores = scores;
            this.HighRankingCards = HighRankingCards;
            this.HoleCards = HoleCards;
        }

    }
    public class PokerPlayerComparer : IComparer<PlayerDataPoker>
    {
        public int Compare(PlayerDataPoker a, PlayerDataPoker b)
        {
            if (a.rank != b.rank)
            {
                return b.rank.CompareTo(a.rank); // Sort by rank, high rank at index 0
            }
            else if (a.rank == 10 || a.rank == 9)
            {
                return b.scores.CompareTo(a.scores); // Sort by scores for rank 10 and 9
            }
            else if (a.rank == 6 || a.rank == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (a.HighRankingCards[i] != b.HighRankingCards[i])
                    {
                        return b.HighRankingCards[i].CompareTo(a.HighRankingCards[i]);
                    }
                }
            }
            else if (a.rank >= 4)
            {
                //return b.scores.CompareTo(a.scores); // Sort by scores for rank 4 and above
                int scoreComparison = b.scores.CompareTo(a.scores);
                if (scoreComparison != 0)
                {
                    return scoreComparison; // Compare scores
                }

                int holeCard1Comparison = b.HoleCards[1].CompareTo(a.HoleCards[1]);
                if (holeCard1Comparison != 0)
                {
                    return holeCard1Comparison; // Compare HoleCards[1]
                }

                return b.HoleCards[0].CompareTo(a.HoleCards[0]); // Compare HoleCards[0], high HoleCard wins
            }
            else if (a.rank >= 3)
            {
                //return b.scores.CompareTo(a.scores); // Sort by scores for rank 3 and above
                int scoreComparison = b.scores.CompareTo(a.scores);
                if (scoreComparison != 0)
                {
                    return scoreComparison; // Compare scores
                }

                int holeCard1Comparison = b.HoleCards[1].CompareTo(a.HoleCards[1]);
                if (holeCard1Comparison != 0)
                {
                    return holeCard1Comparison; // Compare HoleCards[1]
                }

                return b.HoleCards[0].CompareTo(a.HoleCards[0]); // Compare HoleCards[0], high HoleCard wins
            }
            else if (a.rank == 2)
            {
                int scoreComparison = b.scores.CompareTo(a.scores);
                if (scoreComparison != 0)
                {
                    return scoreComparison; // Compare scores
                }

                int holeCard1Comparison = b.HoleCards[1].CompareTo(a.HoleCards[1]);
                if (holeCard1Comparison != 0)
                {
                    return holeCard1Comparison; // Compare HoleCards[1]
                }

                return b.HoleCards[0].CompareTo(a.HoleCards[0]); // Compare HoleCards[0], high HoleCard wins
            }
            else if (a.rank == 1)
            {
                //return b.HoleCards[1].CompareTo(a.HoleCards[1]); // Sort by HoleCards[1] for rank 1
                int holeCard0Comparison = b.HoleCards[0].CompareTo(a.HoleCards[0]);
                int holeCard1Comparison = b.HoleCards[1].CompareTo(a.HoleCards[1]);

                if (holeCard0Comparison != 0)
                {
                    return holeCard0Comparison;
                }

                return holeCard1Comparison;
            }

            return b.HoleCards[1].CompareTo(a.HoleCards[1]); // Default case: Sort by HoleCards[1]
        }
    }
    #endregion
}