using DG.Tweening;
using Mirror;

//using Photon.Pun;
using POKER;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace POKER
{
    public class PokerManager : NetworkBehaviour
    {
        public GameObject[] objectsToDisable;
        public GameObject[] objectsToEnable;

        public GameObject BuyInCashPanel;

        public Transform FinalPokerBetAmountPoint;
        public RectTransform dummyCardInitialPos;
        PlayerStateManager psm;
        //PhotonView photonView;
        [HideInInspector] public int sitPosAfterReset;
        #region Creating Instance
        private static POKER.PokerManager _instance;
        public static POKER.PokerManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<POKER.PokerManager>();
                return _instance;
            }
        }
        #endregion
        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            NetworkGameManager.OnEventReceived += OnNetworkEvent;
        }

        private void OnDestroy()
        {
            NetworkGameManager.OnEventReceived -= OnNetworkEvent;
        }

        private void OnNetworkEvent(int eventCode, string data, Mirror.NetworkConnectionToClient sender)
        {
            if (eventCode == (int)EnumNetworkEventCodes.PokerCardsArray)
            {
                string[] parts = data.Split(',');
                int[] arr = new int[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    arr[i] = int.Parse(parts[i]);
                Array.Copy(arr, PokerRandomArrayCards, arr.Length);
                DistributeHoleCards();
                GenerateCommunityCards();
            }
        }
        // Start is called before the first frame update
        void Start()
        {

            LocalSettings.SetPosAndRect(Pot.instance.PotPanel, FinalPokerBetAmountPoint.GetComponent<RectTransform>(), Pot.instance.PotPanel.transform.parent);
            psm = PlayerStateManager.Instance;
            //photonView = GetComponent<PhotonView>();
            LocalSettings.ToggleObjectState(objectsToDisable, false);
            LocalSettings.ToggleObjectState(objectsToEnable, true);
        }





        public void SetStartingAmount()
        {
            BigInteger number = LocalSettings.GetPokerBuyInChips();
            //if (PhotonNetwork.IsConnectedAndReady)
            //    PhotonNetwork.LocalPlayer.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, number);
        }


        public void AdjustCurrentAmount()
        {

        }

        [ShowOnly] public int[] PokerRandomArrayCards = new int[52];
        public void SettingRandomCardsArrayToRoomProperty()
        {
            Array.Clear(PokerRandomArrayCards, 0, PokerRandomArrayCards.Length);
            PokerRandomArrayCards = new int[52];
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    for (int i = 0; i < GameManager.Instance.AllCards.Card.Length; i++)
                    {
                        PokerRandomArrayCards[i] = i;
                    }
                    Shuffle(PokerRandomArrayCards);
                    RandomCardsArrayAI(PokerRandomArrayCards);

                    // Broadcast shuffled card array to all clients via NetworkGameManager event system.
                    // PokerManager has no NetworkIdentity so [ClientRpc] is not available here.
                    NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.PokerCardsArray, string.Join(",", PokerRandomArrayCards));

                }
            }
            else
            {
                for (int i = 0; i < GameManager.Instance.AllCards.Card.Length; i++)
                {
                    PokerRandomArrayCards[i] = i;
                }
                Shuffle(PokerRandomArrayCards);
                RandomCardsArrayAI(PokerRandomArrayCards);

            }
        }

        // RandomCardsArrayRPC removed — card array is now broadcast via
        // NetworkGameManager.RiseEventRpc(PokerCardsArray) to avoid requiring
        // NetworkIdentity on PokerManager. Clients receive it via OnNetworkEvent above.
        public void RandomCardsArrayAI(int[] randArray)
        {
            //Array.Clear(PokerRandomArrayCards, 0, PokerRandomArrayCards.Length);
            //PokerRandomArrayCards = new int[52];
            Array.Copy(randArray, PokerRandomArrayCards, randArray.Length);
            DistributeHoleCards();
            GenerateCommunityCards();
        }

        void DistributeHoleCards()
        {
            int numberOfPlayers = PlayerStateManager.Instance.PlayingList.Count;
            for (int i = 0; i < numberOfPlayers; i++)
            {
                GenerateNewHoleCards(i);
            }
            PokerCheckWinner.Instance.CreateAndDistributeDummyPokerCards();
        }
        int temp_random_no = 0;
        void GenerateNewHoleCards(int playerIndex)
        {
            //temp_random_no = GetRandomNo(0, AllCards.Count);
            int cardIndex = PokerRandomArrayCards[temp_random_no];
            GameObject hole_card1 = Instantiate(GameManager.Instance.AllCards.Card[cardIndex].gameObject);
            PokerCheckWinner.Instance.HoleCardsToDestroy.Add(hole_card1);
            int card1 = cardIndex;
            temp_random_no++;

            //if(UIManager.Instance.GetMyPlayerInfo() != out)

            //temp_random_no = GetRandomNo(0, AllCards.Count);
            cardIndex = PokerRandomArrayCards[temp_random_no];
            GameObject hole_card2 = Instantiate(GameManager.Instance.AllCards.Card[cardIndex].gameObject);
            PokerCheckWinner.Instance.HoleCardsToDestroy.Add(hole_card2);
            int card2 = cardIndex;
            temp_random_no++;
            SetHoleCardForPlayer(hole_card1, hole_card2, playerIndex, card1, card2);
        }
        // Called in GenerateNewHoleCards #### 3
        void SetHoleCardForPlayer(GameObject hole_card1, GameObject hole_card2, int playerIndex, int card1, int card2)
        {

            //int 
            LocalSettings.SetPosAndRect(hole_card1, psm.PlayingList[playerIndex].card_1_RectTr, psm.PlayingList[playerIndex].card_1_RectTr.parent);
            LocalSettings.SetPosAndRect(hole_card2, psm.PlayingList[playerIndex].card_2_RectTr, psm.PlayingList[playerIndex].card_2_RectTr.parent);

            psm.PlayingList[playerIndex].Hole_Card1 = hole_card1.GetComponent<CardProperty>();
            psm.PlayingList[playerIndex].Hole_Card2 = hole_card2.GetComponent<CardProperty>();
            if (!MatchHandler.isOffline())
            {
                psm.PlayingList[playerIndex].playerCustomProperties.SetCustomData(LocalSettings.pokerHoleCard1ForPlayer, card1);
                psm.PlayingList[playerIndex].playerCustomProperties.SetCustomData(LocalSettings.pokerHoleCard2ForPlayer, card2);
            }
            psm.PlayingList[playerIndex].Hole_Card1.gameObject.SetActive(false);
            psm.PlayingList[playerIndex].Hole_Card2.gameObject.SetActive(false);

        }


        void Shuffle(int[] array)
        {
            System.Random _random = new System.Random();
            int p = array.Length;
            for (int n = p - 1; n > 0; n--)
            {
                int r = _random.Next(0, n);
                int t = array[r];
                array[r] = array[n];
                array[n] = t;
            }
        }

        #region Reset Poker Game
        public void ResetPokerGame()
        {
            PlayerInfo playerInfo = UIManager.Instance.GetMyPlayerInfo();
            if (playerInfo)
                playerInfo.PokerPlayerCashAllIn = false;

            dropCardsIndex = 0;
            PokerActionPanel.Instance.isAllIn = false;
            foreach (GameObject obj in PokerCheckWinner.Instance.HoleCardsToDestroy)
            {
                if (obj)
                    Destroy(obj);
            }

            if (MatchHandler.isOffline())
            {
                PlayerInfo AiInfo = UIManager.Instance.GetAIPlayerInfo();
                if (AiInfo)
                    AiInfo.PokerPlayerCashAllIn = false;
            }



            temp_random_no = 0;
            PokerActionPanel.Instance.CallAllInCheckBtnObj.SetActive(true);
            PokerActionPanel.Instance.BetRaiseSliderBtn.SetActive(true);
            PokerActionPanel.Instance.checkPokerBtn.SetActive(false);
            PokerActionPanel.Instance.StartForSelectAmount();
            PokerActionPanel.Instance.CurrentTargetBetAmount = 0;
            PokerActionPanel.Instance.amountPlacedOnBet = 0;
            PokerCheckWinner pcw = PokerCheckWinner.Instance;
            PokerActionPanel.Instance.BetAmountSlider.interactable = true;

            PokerActionPanel.Instance.pokerMinusBtn.interactable = true;
            PokerActionPanel.Instance.pokerPlusBtn.interactable = true;
            if (pcw.playersArray.Length > 0)
                Array.Clear(pcw.playersArray, 0, pcw.playersArray.Length);
            foreach (PlayerInfo pinfo in GameManager.Instance.playersList)
            {
                pinfo.PokerPlayerCashAllIn = false;
                pinfo.pokerTotalBetCash = 0;
                if (pinfo.gameObject.name != LocalSettings.AI_Name)
                    pinfo.PokerTotalCash = LocalSettings.GetPokerBuyInChips();
                else
                    pinfo.PokerTotalCash = LocalSettings.AI_Amount;
                pinfo.CashAllInIndicator.SetActive(false);
                pinfo.PokerPlayerCurrentRank = 0;
                pinfo.PokerScores = 0;
                pinfo.PokerTotalWholeBetAmount = 0;
                pinfo.DummyCardsParent.transform.GetChild(0).gameObject.SetActive(false);
                pinfo.DummyCardsParent.transform.GetChild(1).gameObject.SetActive(false);
                foreach (CardProperty cardProperty in pinfo.remainingCards)
                {
                    if (cardProperty)
                        Destroy(cardProperty);
                }
                pinfo.remainingCards.Clear();
                DestroyHoleCards(pinfo);
                if (pinfo.pokerBetAmountAnim)
                    Destroy(pinfo.pokerBetAmountAnim);
                pinfo.PackedText.text = "FOLD";
                pinfo.PackedText.gameObject.SetActive(false);
                pinfo.HandRankLabelTxt.transform.parent.gameObject.SetActive(false);
                pinfo.Dealer.SetActive(false);

                pinfo.isAllInCashBet = false;
                pinfo.CashAllInIndicator.SetActive(false);
                pinfo.isCircleCheckFlag = false;
                pinfo.isBetPlacedPocker = false;
                pinfo.CashAllInIndicator.SetActive(false);


            }

            UIManager uIManager = UIManager.Instance;
            if (uIManager.GetMyPlayerCurrentState() != null)
            {

                if (uIManager.GetMyPlayerCurrentState().currentState != PlayerState.STATE.OutOfGame && uIManager.GetMyPlayerCurrentState().currentState != PlayerState.STATE.OutOfTable)
                    if (uIManager.GetMyPlayerInfo().PokerTotalCash < LocalSettings.MinBetAmount)
                        uIManager.GetMyPlayerInfo().StandUp();
            }

            if (MatchHandler.isOffline())
            {
                if (uIManager.GetAIPlayerInfo() == null)
                    return;
                if (uIManager.GetAIPlayerInfo().currentPlayerStateRef != null)
                {
                    if (uIManager.GetAIPlayerInfo().currentPlayerStateRef.currentState != PlayerState.STATE.OutOfGame && uIManager.GetAIPlayerInfo().currentPlayerStateRef.currentState != PlayerState.STATE.OutOfTable)
                    {
                        if (uIManager.GetAIPlayerInfo().PokerTotalCash < LocalSettings.MinBetAmount)
                        {
                            LocalSettings.AI_StandUP = true;
                            uIManager.GetAIPlayerInfo().StandUp();
                            SitHere.Instance.SetThisForAIPositionByPlayer();

                        }
                    }
                }
            }


            DestroyCommunityCards();
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    NetworkGameManager.Instance.syncedDropCardsIndex = 0;
                    // Do NOT reset syncedPokerGameStarted here — keeping it true prevents the
                    // false→true cycle that fired the reconnect hook for ALL clients every round.
                    // Mirror re-sends the current value (true) to any reconnecting client,
                    // which is what arms their restore. Connected players ignore it (not OutOfGame).
                }
            }
        }

        #endregion Reset Poker Game

        #region Poker Community Cards

        [ShowOnly] public List<GameObject> CommunityCardsToDestroy;


        public void DestroyHoleCards(PlayerInfo info)
        {
            Destroy(info.Hole_Card1);
            Destroy(info.Hole_Card2);
        }

        public void DestroyCommunityCards()
        {
            foreach (var cardObject in CommunityCardsToDestroy)
            {
                if (cardObject != null)
                    Destroy(cardObject);
            }
        }

        public bool CheckIfAllBetsEqualOrAllIn()
        {
            bool isAllBetsEqualOrAllin = true;
            if (psm.PlayingList.Count < 2)
                return false;
            for (int i = 1; i < psm.PlayingList.Count; i++)
            {
                if (psm.PlayingList[0].pokerTotalBetCash != psm.PlayingList[i].pokerTotalBetCash)
                {
                    if (!psm.PlayingList[i].isAllInCashBet)
                        isAllBetsEqualOrAllin = false;
                }
            }
            if (psm.PlayingList[0].pokerTotalBetCash != psm.PlayingList[1].pokerTotalBetCash)
            {
                if (!psm.PlayingList[0].isAllInCashBet)
                    isAllBetsEqualOrAllin = false;
            }
            return isAllBetsEqualOrAllin;
        }


        // The card set this hand was dealt from. Kept so a drop-stage report to the server can be
        // pinned to this one hand - see ReportDropCardsIndexToServer below.
        string lastCommunityCardsCsv = "";

        // dropCardsIndex is the only record of how many community cards are face up, and the
        // reconnect restore reads it back off the server. The dedicated server cannot produce it on
        // its own: the reveal sequence hangs off GameStartManager.OnRoomStateChangeToABFirstTurn,
        // which returns immediately when there is no local player, so none of the show*CommunityCards
        // coroutines ever run there and syncedDropCardsIndex stayed 0 for the whole hand. A
        // reconnecting player then rebuilt all five community cards through
        // GenerateCommunityCardsForOutOfGame and, with dropIdx == 0, left every one of them hidden -
        // the empty table. Let the clients that DO run the reveal report the stage instead. The CSV
        // pins the report to one hand so a late call cannot bleed into the next one, and the value
        // only ever moves forward, so both clients reporting the same stage is harmless.
        void ReportDropCardsIndexToServer()
        {
            if (MatchHandler.isOffline()) return;
            if (NetworkGameManager.Instance == null) return;

            if (NetworkServer.active)
            {
                if (dropCardsIndex > NetworkGameManager.Instance.syncedDropCardsIndex)
                    NetworkGameManager.Instance.syncedDropCardsIndex = dropCardsIndex;
                return;
            }

            PlayerInfo mine = UIManager.Instance != null ? UIManager.Instance.GetMyPlayerInfo() : null;
            if (mine != null)
                mine.CmdSetPokerDropCardsIndex(dropCardsIndex, lastCommunityCardsCsv);
        }

        void GenerateCommunityCards()
        {
            Transform parent = PokerCheckWinner.Instance.CommunityCardsRectTransform[0].transform.parent;
            PokerCheckWinner.Instance.Community_Cards = new CardProperty[5];
            int[] CommCardsAry = new int[5];
            for (int i = 0; i < PokerCheckWinner.Instance.Community_Cards.Length; i++)
            {
                //temp_random_no = GetRandomNo(0, AllCards.Count);
                int cardIndex = PokerRandomArrayCards[temp_random_no];
                CommCardsAry[i] = cardIndex;
                GameObject newCommunityCard = Instantiate(GameManager.Instance.AllCards.Card[cardIndex].gameObject);
                CommunityCardsToDestroy.Add(newCommunityCard);
                LocalSettings.SetPosAndRect(newCommunityCard, PokerCheckWinner.Instance.CommunityCardsRectTransform[i], parent);
                PokerCheckWinner.Instance.Community_Cards[i] = newCommunityCard.GetComponent<CardProperty>();
                newCommunityCard.SetActive(false);
                //AllCards.RemoveAt(temp_random_no);
                temp_random_no++;
            }
            // Store community card indices in SyncVar for reconnection support.
            lastCommunityCardsCsv = string.Join(",", CommCardsAry);
            if (!MatchHandler.isOffline() && NetworkServer.active)
            {
                NetworkGameManager.Instance.syncedCommunityCards = lastCommunityCardsCsv;
                NetworkGameManager.Instance.syncedPokerGameStarted = true;  // arm reconnect hook
            }
            //SetPokerHistory();
        }

        /// <summary>
        /// Restores community cards on a reconnecting client by reading the synced state
        /// from NetworkGameManager SyncVars. Called from CheckAllPlayersConnected().
        /// </summary>
        public void RestoreCommunityCardsOnReconnect()
        {
            string cardsStr = NetworkGameManager.Instance.syncedCommunityCards;
            int dropIdx = NetworkGameManager.Instance.syncedDropCardsIndex;
            if (string.IsNullOrEmpty(cardsStr) || dropIdx <= 0) return;

            string[] parts = cardsStr.Split(',');
            if (parts.Length != 5) return;

            int[] commCards = new int[5];
            for (int i = 0; i < 5; i++)
                commCards[i] = int.Parse(parts[i]);

            // Determine how many cards to show based on dropCardsIndex
            int cardsToShow = 0;
            if (dropIdx >= 1) cardsToShow = 3;      // flop
            if (dropIdx >= 2) cardsToShow = 4;      // turn
            if (dropIdx >= 3) cardsToShow = 5;      // river

            // Instantiate all 5 community cards
            Transform parent = PokerCheckWinner.Instance.CommunityCardsRectTransform[0].transform.parent;
            PokerCheckWinner.Instance.Community_Cards = new CardProperty[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject card = Instantiate(GameManager.Instance.AllCards.Card[commCards[i]].gameObject);
                CommunityCardsToDestroy.Add(card);
                LocalSettings.SetPosAndRect(card, PokerCheckWinner.Instance.CommunityCardsRectTransform[i], parent);
                PokerCheckWinner.Instance.Community_Cards[i] = card.GetComponent<CardProperty>();
                card.SetActive(i < cardsToShow); // Show only revealed cards
            }

            // Restore local state
            dropCardsIndex = dropIdx;
            if (dropIdx >= 1) PokerStatesManager.Instance.currentState = PokerState.THREE_COMMUNITY_CARDS;
            if (dropIdx >= 2) PokerStatesManager.Instance.currentState = PokerState.FOUR_COMMUNITY_CARDS;
            if (dropIdx >= 3) PokerStatesManager.Instance.currentState = PokerState.FIVE_COMMUNITY_CARDS;
        }

        #endregion
        #region Setting poker history
        int numberOfPlayers;
        void SetPokerHistory()
        {
            PokerHistory ph = PokerHistory.Instance;
            for (int i = 0; i < PokerCheckWinner.Instance.Community_Cards.Length; i++)
            {
                ph.playerPokerRecord[0].CommunityCardsIndex[i] = PokerCheckWinner.Instance.Community_Cards[i].CardIndexInArray;
            }
            numberOfPlayers = psm.PlayingList.Count;
            ph.playerPokerRecord[0].NumberOfPlayersInMatch = numberOfPlayers;
            for (int i = 0; i < numberOfPlayers; i++)
            {
                ph.playerPokerRecord[0].playerRecord[i].viewId = psm.PlayingList[i].View_ID_Offline;
                ph.playerPokerRecord[0].playerRecord[i].nameOfPlayer = psm.PlayingList[i].player_name.text;
                ph.playerPokerRecord[0].playerRecord[i].holeCard1 = psm.PlayingList[i].Hole_Card1.CardIndexInArray;
                ph.playerPokerRecord[0].playerRecord[i].holeCard2 = psm.PlayingList[i].Hole_Card2.CardIndexInArray;
                ph.playerPokerRecord[0].playerRecord[i].RankOfPlayer = "Fold";
                ph.playerPokerRecord[0].playerRecord[i].isWinner = false;

            }
        }
        public void HistoryFinalCall()
        {
            SetWinStatus();
        }
        void SetWinStatus()
        {
            PokerHistory ph = PokerHistory.Instance;
            for (int i = 0; i < numberOfPlayers; i++)
            {
                //  Debug.LogError("Updateing player rank");
                for (int j = 0; j < GameManager.Instance.playersList.Count; j++)
                {
                    if (!MatchHandler.isOffline())
                    {
                        if (GameManager.Instance.playersList[j].View_ID_Offline == ph.playerPokerRecord[0].playerRecord[i].viewId)
                        {
                            if (GameManager.Instance.playersList[j].currentPlayerStateRef.currentState == PlayerState.STATE.Packed || GameManager.Instance.playersList[j].currentPlayerStateRef.currentState == PlayerState.STATE.OutOfTable)
                            {
                                ph.playerPokerRecord[0].playerRecord[i].RankOfPlayer = "Fold"; GameManager.Instance.playersList[j].HandRankLabelTxt.text = "Fold";
                            }
                            else
                            {
                                ph.playerPokerRecord[0].playerRecord[i].RankOfPlayer = GameManager.Instance.playersList[j].HandRankLabelTxt.text;
                            }
                            if (GameManager.Instance.playersList[j].WinningIndicator.activeInHierarchy)
                                ph.playerPokerRecord[0].playerRecord[i].isWinner = true;
                        }
                    }
                    else
                    {
                        if (GameManager.Instance.playersList[j].View_ID_Offline == ph.playerPokerRecord[0].playerRecord[i].viewId)
                        {
                            if (GameManager.Instance.playersList[j].currentPlayerStateRef.currentState == PlayerState.STATE.Packed || GameManager.Instance.playersList[j].currentPlayerStateRef.currentState == PlayerState.STATE.OutOfTable)
                            {
                                ph.playerPokerRecord[0].playerRecord[i].RankOfPlayer = "Fold"; GameManager.Instance.playersList[j].HandRankLabelTxt.text = "Fold";
                            }
                            else
                            {
                                ph.playerPokerRecord[0].playerRecord[i].RankOfPlayer = GameManager.Instance.playersList[j].HandRankLabelTxt.text;
                            }
                            if (GameManager.Instance.playersList[j].WinningIndicator.activeInHierarchy)
                                ph.playerPokerRecord[0].playerRecord[i].isWinner = true;
                        }
                    }
                }
            }
        }
        #endregion
        #region Show Community Card 1st step => First Three Cards, 2nd => 4th card,  3rd => 5th card
        [ShowOnly]
        public int dropCardsIndex = 0;
        PokerCheckWinner pcw;
        public void DropCommunityCard()
        {
            NowDropCommunityCard(0.5f);
        }

        public void NowDropCommunityCard(float delay)
        {
            //Debug.LogError("Now dropping community Cards: " + dropCardsIndex);
            StartCoroutine(DropCommunityCardInSeq(delay));
        }
        IEnumerator DropCommunityCardInSeq(float delay)
        {
            yield return new WaitForSeconds(delay);
            pcw = PokerCheckWinner.Instance;
            if (dropCardsIndex == 0)
                StartCoroutine(showFirst3CommunityCards());
            else if (dropCardsIndex == 1)
                StartCoroutine(show4thCommunityCards());
            else if (dropCardsIndex == 2)
                StartCoroutine(show5thCommunityCards());
            else
            {
                dropCardsIndex++;
                foreach (PlayerInfo pInfo in psm.PlayingList)
                {
                    // Debug.LogError("Here we Go ... !");
                    pInfo.HandRankLabelTxt.transform.parent.gameObject.SetActive(true);
                    if (pInfo.Hole_Card1 != null)
                    {
                        pInfo.Hole_Card1.gameObject.SetActive(true);
                        pInfo.Hole_Card2.gameObject.SetActive(true);
                    }
                }
                Nowreset();
            }
        }

        public void Nowreset()
        {
            // Guard: DropCommunityCardInSeq runs on both clients independently.
            // Skip if WaitingForResults was already sent to avoid duplicate state changes.
            if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.WaitingForResults)
                return;
            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.WaitingForResults);
        }

        void methodAfterCompletingDummyCards()
        {
            // UIManager.Instance.GetMyPlayerInfo().GiveTurnToNextPlayerPocker();

            if (PokerActionPanel.Instance.CheckBoolAllIN())
            {
                NowDropCommunityCard(0);
                return;
            }
            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsPlaying);
            if (UIManager.Instance)
                UIManager.Instance.GetMyPlayerInfo().ResetIsBetPlacedPocker();

        }
        IEnumerator showFirst3CommunityCards()
        {
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < 3; i++)
            {
                SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.CardFlip, false);
                GameObject dmyCard = Instantiate(GameManager.Instance.DummyCardPrefab);
                CommunityCardsToDestroy.Add(dmyCard);
                LocalSettings.SetPosAndRect(dmyCard, dummyCardInitialPos, dummyCardInitialPos.transform.parent);
                dmyCard.SetActive(true);
                StartCoroutine(PlayAnimation(dmyCard.transform, pcw.CommunityCardsRectTransform[i].transform, pcw.Community_Cards[i].gameObject));
                yield return new WaitForSeconds(0.5f);
            }
            yield return new WaitForSeconds(0.5f);
            dropCardsIndex++;
            ReportDropCardsIndexToServer();

            methodAfterCompletingDummyCards();
            PokerStatesManager.Instance.updatePokerState(PokerState.THREE_COMMUNITY_CARDS);
        }

        IEnumerator show4thCommunityCards()
        {
            yield return new WaitForSeconds(0.5f);
            GameObject dmyCard = Instantiate(GameManager.Instance.DummyCardPrefab);
            CommunityCardsToDestroy.Add(dmyCard);
            LocalSettings.SetPosAndRect(dmyCard, dummyCardInitialPos, dummyCardInitialPos.transform.parent);
            dmyCard.SetActive(true);
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.CardFlip, false);
            StartCoroutine(PlayAnimation(dmyCard.transform, pcw.CommunityCardsRectTransform[3].transform, pcw.Community_Cards[3].gameObject));
            yield return new WaitForSeconds(0.5f);
            dropCardsIndex++;
            ReportDropCardsIndexToServer();
            methodAfterCompletingDummyCards();
            PokerStatesManager.Instance.updatePokerState(PokerState.FOUR_COMMUNITY_CARDS);
        }

        IEnumerator show5thCommunityCards()
        {
            yield return new WaitForSeconds(0.5f);
            GameObject dmyCard = Instantiate(GameManager.Instance.DummyCardPrefab);
            CommunityCardsToDestroy.Add(dmyCard);
            LocalSettings.SetPosAndRect(dmyCard, dummyCardInitialPos, dummyCardInitialPos.transform.parent);
            dmyCard.SetActive(true);
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.CardFlip, false);
            StartCoroutine(PlayAnimation(dmyCard.transform, pcw.CommunityCardsRectTransform[4].transform, pcw.Community_Cards[4].gameObject));
            yield return new WaitForSeconds(0.5f);
            dropCardsIndex++;
            ReportDropCardsIndexToServer();
            methodAfterCompletingDummyCards();
            PokerStatesManager.Instance.updatePokerState(PokerState.FIVE_COMMUNITY_CARDS);
        }
        #endregion

        #region  Card animation to target

        public IEnumerator PlayAnimation(Transform ObjToAnimate, Transform targetPosition, GameObject ObjToActivate)
        {
            GameObject objectToActivate = ObjToActivate;
            yield return new WaitForSeconds(0);
            if (targetPosition != null)
                ObjToAnimate.DOMove(targetPosition.position, 0.4f, false).OnComplete(() => OnCompleteAnim(ObjToAnimate.gameObject, objectToActivate));
            float RotationOffSet = targetPosition.eulerAngles.z;
            ObjToAnimate.DOLocalRotate(new UnityEngine.Vector3(0, 0, 360 + RotationOffSet), 0.4f, RotateMode.FastBeyond360);
            ObjToAnimate.DOScale(new UnityEngine.Vector3(1.5f, 1.5f, 1.5f), 0.4f);
        }
        void OnCompleteAnim(GameObject AnimatedObj, GameObject objectToActivate)
        {
            objectToActivate.SetActive(true);
            AnimatedObj.SetActive(false);
        }
        #endregion


        #region Give_CommunityCard_To_Player_State_OutGame

        public void AssignCummintyCardToOutOFGame(int NumberofCards)
        {
            if (NumberofCards == 1)
            {
                dropCardsIndex = 1;
                ShowCardConmmunityCardToOutOFGamePlayer(3);
            }
            if (NumberofCards == 2)
            {
                dropCardsIndex = 2;
                ShowCardConmmunityCardToOutOFGamePlayer(4);
            }
            if (NumberofCards == 3)
            {
                dropCardsIndex = 3;
                ShowCardConmmunityCardToOutOFGamePlayer(5);
            }
        }


        void ShowCardConmmunityCardToOutOFGamePlayer(int loopNumber)
        {
            for (int i = 0; i < loopNumber; i++)
            {
                PokerCheckWinner.Instance.Community_Cards[i].gameObject.SetActive(true);
            }
        }

        public void GenerateCommunityCardsForOutOfGame()
        {
            Transform parent = PokerCheckWinner.Instance.CommunityCardsRectTransform[0].transform.parent;
            PokerCheckWinner.Instance.Community_Cards = new CardProperty[5];
            int[] commCards = new int[5];
            // Parse community cards from synced SyncVar (comma-separated card indices)
            string csv = NetworkGameManager.Instance.syncedCommunityCards;
            if (!string.IsNullOrEmpty(csv))
            {
                // A reconnected client builds the board from here, not from GenerateCommunityCards,
                // so this is where it picks up the CSV that stamps its own later reveal reports.
                lastCommunityCardsCsv = csv;
                string[] parts = csv.Split(',');
                for (int ci = 0; ci < 5 && ci < parts.Length; ci++)
                    int.TryParse(parts[ci], out commCards[ci]);
            }
            for (int i = 0; i < PokerCheckWinner.Instance.Community_Cards.Length; i++)
            {
                int cardIndex = commCards[i];
                GameObject newCommunityCard = Instantiate(GameManager.Instance.AllCards.Card[cardIndex].gameObject);
                CommunityCardsToDestroy.Add(newCommunityCard);
                LocalSettings.SetPosAndRect(newCommunityCard, PokerCheckWinner.Instance.CommunityCardsRectTransform[i], parent);
                PokerCheckWinner.Instance.Community_Cards[i] = newCommunityCard.GetComponent<CardProperty>();
                newCommunityCard.SetActive(false);
            }
        }

        #endregion

        // Room state broadcasting is now handled via NetworkGameManager.CmdRiseEvent /
        // RiseEventRpc + the OnNetworkEvent listener registered in RoomStateManager.
        // No NetworkIdentity is required on PokerManager for state sync.

    }

    /// <summary>
    /// Formatting helpers for the Poker match-server log (MatchFlow). Logging only: every member reads
    /// game state and returns text or writes a MatchFlow line - nothing here changes the game, and on
    /// phones / the AI table MatchFlow is a no-op. Every reader is null-safe and never throws.
    /// </summary>
    internal static class PokerFlow
    {
        public const string Game = "Poker";

        /// <summary>Hand number on this match server (bumped when the server deals a hand).</summary>
        public static int Hand;
        static int _resultHand = -1;

        public static string Who(PlayerInfo p)
        {
            return p == null ? "?" : MatchFlow.Who(p.ownerPlayerId);
        }

        public static string Card(CardProperty c)
        {
            if (c == null) return "?";
            string rank;
            switch (c.Card)
            {
                case CardState.CARDVALUE.JACK: rank = "J"; break;
                case CardState.CARDVALUE.QUEEN: rank = "Q"; break;
                case CardState.CARDVALUE.KING: rank = "K"; break;
                case CardState.CARDVALUE.ACE: rank = "A"; break;
                default: rank = ((int)c.Card).ToString(); break;
            }
            string suit;
            switch (c.Suit)
            {
                case CardState.SUIT.SPADE: suit = "♠"; break;
                case CardState.SUIT.HEART: suit = "♥"; break;
                case CardState.SUIT.DIAMOND: suit = "♦"; break;
                default: suit = "♣"; break;
            }
            return rank + suit;
        }

        public static string CardAt(int index)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.AllCards == null || gm.AllCards.Card == null) return "?";
            if (index < 0 || index >= gm.AllCards.Card.Length) return "?";
            return Card(gm.AllCards.Card[index]);
        }

        /// <summary>Names of the players dealt into the hand.</summary>
        public static string Players()
        {
            PlayerStateManager psm = PlayerStateManager.Instance;
            if (psm == null || psm.PlayingList == null || psm.PlayingList.Count == 0) return "no players";
            List<string> names = new List<string>();
            foreach (PlayerInfo p in psm.PlayingList)
                if (p != null) names.Add(Who(p));
            return string.Join(", ", names);
        }

        /// <summary>The whole pot of the current hand, from the server's synced per-player hand totals.</summary>
        public static BigInteger PotValue()
        {
            BigInteger pot = 0;
            try
            {
                GameManager gm = GameManager.Instance;
                if (gm == null || gm.playersList == null) return 0;
                foreach (PlayerInfo p in gm.playersList)
                    if (p != null) pot += LocalSettings.StringToBigInteger(p.syncedPokerHandBet);
            }
            catch { }
            return pot;
        }

        public static string Pot() { return PotValue().ToString(); }

        /// <summary>"pot 400 · table stacks: Ali 1600, Sara 2400" (stacks as the clients last synced them).</summary>
        public static string Chips()
        {
            try
            {
                List<string> stacks = new List<string>();
                GameManager gm = GameManager.Instance;
                if (gm != null && gm.playersList != null)
                    foreach (PlayerInfo p in gm.playersList)
                        if (p != null && p.playerCustomProperties != null)
                            stacks.Add($"{Who(p)} {p.playerCustomProperties.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey)}");
                return $"pot {Pot()}" + (stacks.Count > 0 ? " · table stacks: " + string.Join(", ", stacks) : "");
            }
            catch { return $"pot {Pot()}"; }
        }

        /// <summary>A player's state change as the server applies it (turn, check, fold, stand up).</summary>
        public static void State(PlayerInfo p, PlayerState.STATE prev, PlayerState.STATE next)
        {
            if (!MatchFlow.Enabled || prev == next) return;
            switch (next)
            {
                case PlayerState.STATE.ExecutingTurn:
                    MatchFlow.Log(Game, $"turn → {Who(p)}");
                    break;
                case PlayerState.STATE.WaitingForTurn:
                    if (prev == PlayerState.STATE.ExecutingTurn)
                        MatchFlow.Log(Game, $"{Who(p)} checks (pot {Pot()})");
                    break;
                case PlayerState.STATE.Packed:
                    MatchFlow.Log(Game, $"{Who(p)} folds (pot {Pot()})");
                    break;
                case PlayerState.STATE.OutOfTable:
                    MatchFlow.Log(Game, $"{Who(p)} stood up / left the table");
                    break;
            }
        }

        /// <summary>A player's bet as the server receives it: call / bet / raise / all-in, with the pot.</summary>
        public static void Bet(PlayerInfo p, string amountString, bool isAllIn)
        {
            if (!MatchFlow.Enabled || p == null) return;
            try
            {
                BigInteger amount = LocalSettings.StringToBigInteger(amountString);
                if (amount <= 0) return;
                BigInteger mine = LocalSettings.StringToBigInteger(p.syncedPokerRoundBet);
                BigInteger top = 0;
                PlayerStateManager psm = PlayerStateManager.Instance;
                if (psm != null && psm.PlayingList != null)
                    foreach (PlayerInfo o in psm.PlayingList)
                    {
                        if (o == null || o == p) continue;
                        BigInteger theirs = LocalSettings.StringToBigInteger(o.syncedPokerRoundBet);
                        if (theirs > top) top = theirs;
                    }
                string who = Who(p);
                string what;
                if (isAllIn) what = $"{who} goes all-in with {amount}";
                else if (mine <= top) what = $"{who} calls {amount}";
                else if (top == 0) what = $"{who} bets {amount}";
                else what = $"{who} raises to {mine} (+{amount})";
                MatchFlow.Log(Game, $"{what} (pot {Pot()})");
            }
            catch { }
        }

        /// <summary>Community cards a client reported face up: index 1 = flop, 2 = turn, 3 = river.</summary>
        public static void Board(int index, string csv)
        {
            if (!MatchFlow.Enabled || string.IsNullOrEmpty(csv)) return;
            string[] parts = csv.Split(',');
            int from = index == 1 ? 0 : index == 2 ? 3 : index == 3 ? 4 : 0;
            int count = index == 1 ? 3 : index == 2 || index == 3 ? 1 : 5;
            string street = index == 1 ? "flop" : index == 2 ? "turn" : index == 3 ? "river" : "board";
            List<string> cards = new List<string>();
            for (int i = from; i < from + count && i < parts.Length; i++)
                cards.Add(int.TryParse(parts[i], out int c) ? CardAt(c) : "?");
            MatchFlow.Log(Game, $"{street}: {string.Join(" ", cards)} (pot {Pot()})");
        }

        static string ShowHand(PlayerInfo p)
        {
            if (p == null) return "?";
            string label = null;
            PokerCheckWinner pcw = PokerCheckWinner.Instance;
            int rank = p.PokerPlayerCurrentRank;
            if (pcw != null && pcw.HandRankLabel != null && rank >= 1 && rank <= pcw.HandRankLabel.Length)
                label = pcw.HandRankLabel[rank - 1];
            if (string.IsNullOrEmpty(label)) label = "rank " + rank;
            return $"{Who(p)} ({label}, {Card(p.Hole_Card1)} {Card(p.Hole_Card2)})";
        }

        /// <summary>
        /// True the first time a winner is accepted in this hand. The server hears the same winner more
        /// than once (its own fold auto-win plus each client's IAmWinner command), and only the first one
        /// may write the result lines - a later one would open a stray match record.
        /// </summary>
        public static bool FirstResult()
        {
            if (!MatchFlow.Enabled || _resultHand == Hand) return false;
            _resultHand = Hand;
            return true;
        }

        /// <summary>
        /// The server accepted a winner for the hand: writes the showdown / win line and returns the reason
        /// for MatchFlow.SendResult. Hole cards are logged here only, at hand end.
        /// </summary>
        public static string Winner(PlayerInfo w)
        {
            bool showdown = RoomStateManager.Instance != null
                && RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.WaitingForResults;
            string reason = showdown ? "best hand at showdown" : "everyone else folded or left";
            if (!MatchFlow.Enabled || w == null) return reason;
            try
            {
                if (showdown)
                {
                    List<string> hands = new List<string>();
                    bool tied = false;
                    PlayerStateManager psm = PlayerStateManager.Instance;
                    if (psm != null && psm.PlayingList != null)
                        foreach (PlayerInfo p in psm.PlayingList)
                        {
                            if (p == null) continue;
                            hands.Add(ShowHand(p));
                            if (p != w && p.PokerPlayerCurrentRank == w.PokerPlayerCurrentRank && p.PokerScores == w.PokerScores)
                                tied = true;
                        }
                    MatchFlow.Log(Game, $"showdown: {string.Join(" vs ", hands)} — {Who(w)} wins pot {Pot()}" + (tied ? " (same rank and score as another hand — pot may be split)" : ""));
                }
                else
                    MatchFlow.Log(Game, $"{Who(w)} wins pot {Pot()} — everyone else folded or left");
            }
            catch { }
            return reason;
        }
    }
}