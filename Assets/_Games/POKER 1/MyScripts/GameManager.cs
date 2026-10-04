using DG.Tweening;
using Mirror;
using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using Unity.Services.Authentication;

namespace POKER
{

    public class GameManager : MonoBehaviour
    {

        [ShowOnly] public bool isRunINBackGround;

        public GameObject DummyCardPrefab;
        public GameObject playerPrefab, AI_Prefab;

        public Transform PlayerTable;

        //public Sprite[] playerProfileImage;
        public Collections PlayerProfileImages;
        public FramesCollections playerProfileFrameImage;

        public RectTransform positionAvailabilityFull;
        public RectTransform shakeAnimationWhenPositionAvailFul;
        public CardsContainer AllCards;
        public PositionAvailability[] position_availability;
        public List<PlayerInfo> playersList;

        public RectTransform DistributerCardPosition;

        public int TotalPlayers;
        public int[] CardsIndexes;

        public GameObject[] Particles;

        public RectTransform SupportingCardPos;
        [ShowOnly]
        public CardProperty SupportingCard;

        public RectTransform XpShowAdding;

        [ShowOnly]
        public int SupportingCardIndex;

        public int myLocalSeat
        {
            get { return privateLocalSeat; }
            set
            {
                privateLocalSeat = value;
                PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
            }
        }

        public int AILocalSeat
        {
            get { return privateAILocalSeat; }
            set
            {
                privateAILocalSeat = value;
                PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
            }
        }

        [SerializeField]
        private int privateLocalSeat;
        [SerializeField]
        private int privateAILocalSeat = 1;

        public TMP_Text TurnTxt;

        #region Creating Instance
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<GameManager>();
                return _instance;
            }
        }
        #endregion
        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            NetworkGameManager.OnEventReceived += OnEvent;

        }
        private void OnDestroy()
        {
            NetworkGameManager.OnEventReceived -= OnEvent;

        }
        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Game Manager Start : " + MatchHandler.isOffline());

            if (NetworkClient.active)
            {
                MatchHandler.CurrentMatch = MatchHandler.MATCH.Poker;
                POKER.LocalSettings.SetPlayername(staticVariables.userNickName);
                string cachedBalance = staticVariables.isgoldcoins
                    ? ApiAndRoomManager.LastFetchedCoins.data.gold_balance
                    : ApiAndRoomManager.LastFetchedCoins.data.silver_balance;
                PlayerPrefs.SetString(POKER.LocalSettings.TotalChips, cachedBalance);
                PlayerPrefs.SetString(POKER.LocalSettings.PokerTotalBuyInChips, cachedBalance);

                // All Awakes have completed before Start, so UIManager is available here.
                // Do not leave PlayerTotalChips showing the scene's serialized placeholder
                // while the fresh balance request is still in flight.
                UIManager.Instance?.RefreshPlayerTotalChips(LocalSettings.GetPokerBuyInChips());
            }
            if (NetworkServer.active)
            {
                MatchHandler.CurrentMatch = MatchHandler.MATCH.Poker;
                POKER.LocalSettings.SetPlayername("Server");
                PlayerPrefs.SetString(POKER.LocalSettings.TotalChips, "50000");
            }
            Application.runInBackground = true;




            if (MatchHandler.IsPoker())
            {
                LocalSettings.SetMinPlayers(2);
            }
            GameManager.Instance.StartingThings();

            PokerManager.Instance.SetStartingAmount();
            PokerActionPanel.Instance.StartForSelectAmount();
            //if (LocalSettings.GetTotalChips() >= (LocalSettings.GetStartingMinAmountPoker()))
            //{

            //    PokerTableAmount.Instance.OnJoinNowBtnClick();
            //    Debug.Log("Here is Player Instantiate");

            //}
            //else
            //{
            //    Debug.LogError("Here is Player Instantiate  1");
            //    PokerManager.Instance.SetStartingAmount();
            //    StartingThings();
            //}

            //  UIManager.Instance.GetMyPlayerInfo().startCash = LocalSettings.GetPokerBuyInChips();
            if (!MatchHandler.isOffline())
            {

                // LocalSettings.GetSetRoomID = PhotonNetwork.CurrentRoom.Name.ToString();
                LocalSettings.GetSetGameName = MatchHandler.CurrentMatch.ToString();
                // LocalSettings.GetSetTableName = NetworkSettings.Instance.RoomName;
            }
            else
                StartingAIThings();




            //StartCoroutine(StandupPlayer());
        }




        public void StartingThings()
        {

            Debug.LogError("starting thingss");
            //if (UIManager.Instance.GetMyPlayerInfo() == null)
            if (UIManager.Instance.GetMyPlayerInfo() == null)
            {
                StartCoroutine(StartPlayerInstantiateNow());
            }
            else
            {
                PositionsManager.Instance.SitHere(PokerManager.Instance.sitPosAfterReset);
                Invoke(nameof(RefreshAfterSomeTime), 0.5f);
                SitHereBtnStatus(false);
            }

            StartCoroutine(CheckAllPlayersConnected());
            //  PlayerTotalChipsUpdate(0);
            SitHereBtnStatus(false);


        }

        public void StartingAIThings()
        {
            //if (UIManager.Instance.GetMyPlayerInfo() == null)
            if (ReferenceEquals(UIManager.Instance.GetAIPlayerInfo(), null))
                StartCoroutine(StartAIrInstantiateNow());
            else
            {

                Invoke(nameof(RefreshAfterSomeTime), 0.5f);
                SitHereBtnStatus(false);
            }

            StartCoroutine(CheckAllPlayersConnected());
            AITotalChipsUpdate(0);
            SitHereBtnStatus(false);
        }

        public IEnumerator StartAIrInstantiateNow()
        {
            yield return new WaitForSeconds(0.01f);
            GameObject NewPlayerInstantiated = Instantiate(AI_Prefab, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
            NewPlayerInstantiated.name = LocalSettings.AI_Name;

        }
        void RefreshAfterSomeTime()
        {
            PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
        }


        public void SitHereBtnStatus(bool val)
        {
            foreach (var item in position_availability)
            {
                item.sitHere.SetActive(val);
                item.sitHere.transform.parent.GetComponent<Image>().enabled = !item.sitHere.activeSelf;
            }
        }
        private void OnEvent(int eventcode, string CustomData, NetworkConnectionToClient sender = null)
        {
            Debug.Log("received event: " + eventcode);
            switch (eventcode)
            {
                case (int)EnumNetworkEventCodes.SpawnPlayer:
                    {
                        if (NetworkServer.active)
                        {
                            // Player ID is passed as CustomData from the client (MirrorPlayerPrefab.playerId
                            // is only set on the client — SyncVars don't sync client→server in Mirror).
                            string senderId = CustomData;
                            Debug.Log($"[SpawnPlayer] SERVER senderId='{senderId}'");

                            // Reconnect guard: skip spawn if PlayerInfo already exists for this player.
                            bool alreadyExists = !string.IsNullOrEmpty(senderId)
                                && playersList.Exists(p => p != null && p.ownerPlayerId == senderId);
                            Debug.Log($"[SpawnPlayer] alreadyExists={alreadyExists}, playersList.Count={playersList.Count}");
                            if (!alreadyExists)
                            {
                                GameObject gameManagerGO = Instantiate(playerPrefab);
                                var playerInfo = gameManagerGO.GetComponent<PlayerInfo>();
                                playerInfo.ownerPlayerId = senderId;       // set BEFORE spawn so it syncs
                                Debug.Log($"[SpawnPlayer] Spawning with ownerPlayerId='{senderId}'");
                                NetworkServer.Spawn(gameManagerGO);         // server-owned — survives disconnect
                            }
                        }
                    }
                    break;
                case (int)EnumNetworkEventCodes.ExitGame:
                    {
                        string message = (string)CustomData;
                        if (message == staticVariables.UserProfiledata.user._id.ToString())
                        {
                            // PhotonNetwork.LeaveRoom();
                            SceneManager.LoadScene("Home");
                        }
                        else
                        {
                            // StopAndFinishGame();
                        }
                    }
                    break;
                case (int)EnumNetworkEventCodes.BookSeat:
                    {
                        string message = (string)CustomData;
                        PositionsManager.Instance.isBooked[int.Parse(message)] = true;

                    }
                    break;

                default:
                    Debug.LogWarning("Unhandled event code: " + eventcode);
                    break;
            }
        }


        public IEnumerator StartPlayerInstantiateNow()
        {
            Debug.Log("Not Ready" + MatchHandler.IsPoker());
            if (MatchHandler.IsPoker())
            {
                if (NetworkClient.active)
                {
                    yield return new WaitForSeconds(2f);

                    NetworkGameManager.Instance.CmdRiseEvent((int)EnumNetworkEventCodes.SpawnPlayer, staticVariables.UserProfiledata.user._id.ToString());

                    Debug.Log("Is Ready");
                    //GameObject gameManagerGO = Instantiate(playerPrefab);
                    //NetworkServer.Spawn(gameManagerGO);
                }

            }
            else
            {
                yield return new WaitForSeconds(0.01f);
                GameObject NewPlayerInstantiated = Instantiate(playerPrefab, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
                NewPlayerInstantiated.name = LocalSettings.GetPlayerName();
            }
        }
        // Assign Real Cards to All Players
        #region Assign Real Cards to All Players
        public void AssignCardsToAllPlayers()
        {
            // Transfering all cards to temporary list
            List<GameObject> TempRealCardsList = new List<GameObject>();
            for (int i = 0; i < AllCards.Card.Length; i++)
            {
                TempRealCardsList.Add(AllCards.Card[i].gameObject);
            }

            // Generating list of random selected cards according to number of players
            List<GameObject> SelectedRandomCardsList = new List<GameObject>();
            //for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount * 6; i++)
            int addSupportingCard = 0;

            for (int i = 0; i < (PlayerStateManager.Instance.PlayingList.Count * 3) + addSupportingCard; i++)
            {
                int randomNumber = UnityEngine.Random.Range(0, TempRealCardsList.Count);
                SelectedRandomCardsList.Add(TempRealCardsList[randomNumber].gameObject);
                TempRealCardsList.RemoveAt(randomNumber);
            }
            //if (MatchHandler.CurrentMatch == MatchHandler.MATCH.HUKM)
            //{

            //    GameObject supCard = Instantiate(SelectedRandomCardsList[SelectedRandomCardsList.Count - 1], PlayerTable);
            //    SelectedRandomCardsList.RemoveAt(SelectedRandomCardsList.Count - 1);
            //    SupportingCard = supCard.GetComponent<CardProperty>();
            //    SetPosAndRect(SupportingCard.gameObject, SupportingCardPos, PlayerTable);
            //    SupportingCard.gameObject.SetActive(false);
            //}
            // Getting indexes of Selected cards for players
            CardsIndexes = new int[SelectedRandomCardsList.Count];
            for (int i = 0; i < SelectedRandomCardsList.Count; i++)
                CardsIndexes[i] = SelectedRandomCardsList[i].GetComponent<CardProperty>().CardIndexInArray;


            //  Debug.LogError("Master called NumberOf Times: ");
            if (UIManager.Instance.GetMyPlayerInfo() != null)
                UIManager.Instance.GetMyPlayerInfo().SendPlayerCardsArray(CardsIndexes);
        }

        #endregion

        /// Dummy cards distribution ////       
        #region Dummy Cards Generation and distribution
        List<GameObject> DummyCardsList;

        GameObject[] TempPlayersListForDummyCards;
        public void InstantiateDummyPlayerCard(int NumberOfPlayers)
        {
            AssigningPlayersToListForDummyCards();
            DummyCardsList = new List<GameObject>();
            int totalCardsToGenerate = NumberOfPlayers * 3;
            for (int i = 0; i < totalCardsToGenerate; i++)
            {
                GameObject card = Instantiate(DummyCardPrefab);
                DummyCardsList.Add(card);
                LocalSettings.SetPosAndRect(card, DistributerCardPosition.transform.GetChild(0).gameObject.GetComponent<RectTransform>(), DistributerCardPosition);
            }

            StartCoroutine(DistributeDummyCards());
        }
        void AssigningPlayersToListForDummyCards()
        {
            TempPlayersListForDummyCards = new GameObject[position_availability.Length];
            UpdateCard(0, 2);
            UpdateCard(1, 1);
            UpdateCard(2, 0);
            UpdateCard(3, 4);
            UpdateCard(4, 3);
        }
        void UpdateCard(int IndexDummyCardsAry, int indexReserved)
        {
            if (position_availability[indexReserved].is_reserved)
            {
                if (position_availability[indexReserved].is_reserved.activeInHierarchy)
                    TempPlayersListForDummyCards[IndexDummyCardsAry] = position_availability[indexReserved].is_reserved;
                else
                    TempPlayersListForDummyCards[IndexDummyCardsAry] = null;
            }
        }
        IEnumerator DistributeDummyCards()
        {
            yield return new WaitForSeconds(0.1f);
            float delay = 0.4f * (PlayerStateManager.Instance.PlayingList.Count * 3);
            Invoke(nameof(ChageRoomStateToGamePlaying), delay);
            posIndexTemp = 0;
            PlyerCrdDummyIndex = 0;
            {
                foreach (GameObject card in DummyCardsList)
                {
                    yield return new WaitForSeconds(0.4f);
                    GameObject dumPos = GetPosForDummyCard();
                    //Debug.LogError("DummyCard index: " + PlyerCrdDummyIndex);
                    if (PlyerCrdDummyIndex <= dumPos.GetComponent<PlayerInfo>().PlayerDummyCardsToShowParent.transform.childCount - 2)
                    {
                        Transform pos = dumPos.GetComponent<PlayerInfo>().PlayerDummyCardsToShowParent.transform.GetChild(PlyerCrdDummyIndex).transform;
                        GameObject childObj = dumPos.GetComponent<PlayerInfo>().PlayerDummyCardsToShowParent.transform.GetChild(PlyerCrdDummyIndex).gameObject;
                        StartCoroutine(PlayAnimation(card.transform, pos, childObj));
                        SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.CardFlip, false);
                        //Debug.LogError("card distribute sound is playing");
                    }
                }

                PlayerStateManager psm = PlayerStateManager.Instance;
                foreach (PlayerInfo plyrinfo in psm.PlayingList)
                {
                    plyrinfo.PlayerDummyCardsToShowParent.transform.GetChild(0).gameObject.SetActive(true);
                    plyrinfo.PlayerDummyCardsToShowParent.transform.GetChild(1).gameObject.SetActive(true);
                    plyrinfo.PlayerDummyCardsToShowParent.transform.GetChild(2).gameObject.SetActive(true);
                    plyrinfo.PlayerDummyCardsToShowParent.SetActive(true);

                    plyrinfo.ShowBtn.SetActive(plyrinfo.IsMine());

                }
            }

            Invoke(nameof(RemoveRemainingDummyCards), 0.4f);
        }

        void ChageRoomStateToGamePlaying()
        {
            if (PlayerStateManager.Instance.PlayingList.Count > 1)
                RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsPlaying);
            else
                RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ShowingResults);
        }
        void RemoveRemainingDummyCards()
        {
            for (int i = DummyCardsList.Count - 1; i >= 0; i--)
                Destroy(DummyCardsList[i]);
            DummyCardsList.Clear();
            if (SupportingCard)
                SupportingCard.gameObject.SetActive(true);
        }

        int posIndexTemp = 0;
        int PlyerCrdDummyIndex = 0;
        // return target pos
        GameObject GetPosForDummyCard()
        {
            if (posIndexTemp >= TempPlayersListForDummyCards.Length)
            {
                posIndexTemp = 0;
                PlyerCrdDummyIndex++;
            }
            if (CheckAvailability(posIndexTemp))
            {
                GameObject pos = TempPlayersListForDummyCards[posIndexTemp];
                posIndexTemp++;
                return pos;
            }
            else
            {
                posIndexTemp++;
                return GetPosForDummyCard();
            }
        }

        bool CheckAvailability(int value)
        {
            return TempPlayersListForDummyCards[value] != null;
        }
        //public void PlayAnimation(Transform ObjToAnimate, Transform targetPosition, GameObject ObjToActivate)
        public IEnumerator PlayAnimation(Transform ObjToAnimate, Transform targetPosition, GameObject ObjToActivate)
        {
            GameObject objectToActivate = ObjToActivate;
            yield return new WaitForSeconds(0);
            if (targetPosition != null)
                ObjToAnimate.DOMove(targetPosition.position, 0.4f, false).OnComplete(() => OnCompleteAnim(ObjToAnimate.gameObject, objectToActivate));
            float RotationOffSet = targetPosition.eulerAngles.z;
            if (ObjToAnimate != null)
            {
                ObjToAnimate.DOLocalRotate(new UnityEngine.Vector3(0, 0, 360 + RotationOffSet), 0.4f, RotateMode.FastBeyond360);
                ObjToAnimate.DOScale(new UnityEngine.Vector3(1.5f, 1.5f, 1.5f), 0.4f);
            }
        }




        void OnCompleteAnim(GameObject AnimatedObj, GameObject objectToActivate)
        {
            objectToActivate.SetActive(true);
            AnimatedObj.SetActive(false);
        }
        #endregion

        public int CountPlayers()
        {
            return playersList.Count;
        }

        // SetOtherPlayersPositioning removed — positioning now handled by event system + SyncDicts.

        void SetAllPlayersPositions()
        {
            for (int i = 0; i < playersList.Count; i++)
            {
                if (!playersList[i].seated)
                {
                    // Player positioning handled via AssignPositionOfthisPlayer and events
                    playersList[i].seated = true;
                }
            }
        }

        public IEnumerator CheckAllPlayersConnected()
        {
            if (!MatchHandler.isOffline())
            {
                yield return new WaitUntil(() => !string.IsNullOrEmpty(NetworkGameManager.Instance.creatorData.playerId) && !string.IsNullOrEmpty(NetworkGameManager.Instance.joinerData.playerId));
            }

            SetAllPlayersPositions();

            PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
            // Reconnect restore is handled by the Snooker-style flow in NetworkGameManager:
            // syncedPokerGameStarted hook → CmdRequestPokerReconnectState → RpcRestorePokerOnReconnect → RestorePokerStateOnReconnect
        }

        private void RestoreLocalPokerBalances(PlayerInfo myPlayer)
        {
            BigInteger fallbackTableCash = LocalSettings.GetPokerBuyInChips();

            if (myPlayer?.playerCustomProperties == null)
            {
                UIManager.Instance?.RefreshPlayerTotalChips(fallbackTableCash);
                return;
            }

            bool hasSyncedTableCash = myPlayer.playerCustomProperties.customBigIntegerData
                .ContainsKey(LocalSettings.PlayerPokerTableCashKey);
            BigInteger syncedTableCash = myPlayer.playerCustomProperties
                .GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey);

            if (hasSyncedTableCash)
            {
                LocalSettings.SetPokerBuyInChipsAbsolute(syncedTableCash);
                UIManager.Instance?.RefreshPlayerTotalChips(syncedTableCash);

                if (PokerActionPanel.Instance != null)
                {
                    PokerActionPanel.Instance.MaxBetAmount = syncedTableCash;
                    PokerActionPanel.Instance.SetMinBetAmount();
                }

                Debug.Log($"[RestorePokerStateOnReconnect] Restored table stack={syncedTableCash}");
            }
            else
            {
                // The SyncDictionary may not have arrived yet on a very early reconnect.
                // Show the locally cached balance instead of the scene placeholder.
                UIManager.Instance?.RefreshPlayerTotalChips(fallbackTableCash);
            }

            bool hasSyncedTotalChips = myPlayer.playerCustomProperties.customBigIntegerData
                .ContainsKey(LocalSettings.TotalChips);
            if (hasSyncedTotalChips)
            {
                BigInteger syncedTotalChips = myPlayer.playerCustomProperties
                    .GetCustomBigIntegerData(LocalSettings.TotalChips);
                LocalSettings.SetTotalServerChips(syncedTotalChips.ToString());
                myPlayer.PokerTotalCash = syncedTotalChips;
                if (myPlayer.PokerTotalCashTxt != null)
                    myPlayer.PokerTotalCashTxt.text = LocalSettings.Rs(syncedTotalChips);
            }
        }

        public void RestorePokerStateOnReconnect()
        {
            if (MatchHandler.isOffline()) return;

            PlayerInfo myPlayer = playersList.Find(p => p != null && p.IsMine());
            if (myPlayer == null) return;
            RestoreLocalPokerBalances(myPlayer);
            if (myPlayer.getCurrentPlayerState()?.currentState != PlayerState.STATE.OutOfGame) return;

            RoomState.STATE syncedState = (RoomState.STATE)NetworkGameManager.Instance.syncedPokerRoomState;
            bool gameInProgress = syncedState == RoomState.STATE.GameIsPlaying
                               || syncedState == RoomState.STATE.ABFirstTurn
                               || syncedState == RoomState.STATE.ABSecondTurn
                               || syncedState == RoomState.STATE.WaitingForResults
                               || syncedState == RoomState.STATE.ShowingResults
                               // CardDistributing belongs here too: the cards are already dealt by
                               // the time hasCardData is true below, and leaving it out meant a
                               // player who dropped between the deal and the first turn was written
                               // off as OutOfGame with no restore - the opponent then waited forever
                               // for a turn that no longer existed. Keep this list identical to the
                               // one in PlayerInfo.playerEnterenceState().
                               || syncedState == RoomState.STATE.CardDistributing;

            bool hasCardData = myPlayer.playerCustomProperties.GetCustomData(LocalSettings.pokerHoleCard1ForPlayer) != 0
                            || myPlayer.playerCustomProperties.GetCustomData(LocalSettings.pokerHoleCard2ForPlayer) != 0;

            Debug.Log($"[RestorePokerStateOnReconnect] gameInProgress={gameInProgress}, hasCardData={hasCardData}, syncedState={syncedState}");

            if (!gameInProgress || !hasCardData) return;

            // Rebuild PlayingList from player states stored in SyncDictionaries
            PlayerStateManager.Instance.PlayingList.Clear();
            foreach (var player in playersList)
            {
                if (player == null) continue;
                var state = player.playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState);
                if (state != PlayerState.STATE.OutOfTable && state != PlayerState.STATE.OutOfGame
                    && state != PlayerState.STATE.AbleToJoin && state != PlayerState.STATE.Packed)
                {
                    PlayerStateManager.Instance.PlayingList.Add(player);
                }
                else if (state == PlayerState.STATE.Packed)
                {
                    // Somebody who folded while I was away is out of the hand on every other
                    // machine - PlayerStateManager.LocalUpdateListOnPacked drops Packed players -
                    // but this rebuild used to put them back in. My client then believed the hand
                    // still had two live players (wrong bet-equality checks, a turn expected from a
                    // seat that is already done) and the loop below dealt them a dummy pair of cards
                    // they are not holding. Keep them out, and put the FOLD label back instead.
                    player.PackedText.text = "FOLD";
                    player.PackedText.gameObject.SetActive(true);
                }
            }

            // Show dummy hole cards for all playing players
            foreach (var player in PlayerStateManager.Instance.PlayingList)
            {
                if (player == null) continue;
                player.DummyCardsParent.transform.GetChild(0).gameObject.SetActive(true);
                player.DummyCardsParent.transform.GetChild(1).gameObject.SetActive(true);
            }

            // Instantiate the reconnecting player's actual hole cards face-up
            int c1 = myPlayer.playerCustomProperties.GetCustomData(LocalSettings.pokerHoleCard1ForPlayer);
            int c2 = myPlayer.playerCustomProperties.GetCustomData(LocalSettings.pokerHoleCard2ForPlayer);
            if (c1 != 0 || c2 != 0)
            {
                GameObject hole1 = Instantiate(AllCards.Card[c1].gameObject);
                GameObject hole2 = Instantiate(AllCards.Card[c2].gameObject);
                PokerCheckWinner.Instance.HoleCardsToDestroy.Add(hole1);
                PokerCheckWinner.Instance.HoleCardsToDestroy.Add(hole2);
                LocalSettings.SetPosAndRect(hole1, myPlayer.card_1_RectTr, myPlayer.card_1_RectTr.parent);
                LocalSettings.SetPosAndRect(hole2, myPlayer.card_2_RectTr, myPlayer.card_2_RectTr.parent);
                myPlayer.Hole_Card1 = hole1.GetComponent<CardProperty>();
                myPlayer.Hole_Card2 = hole2.GetComponent<CardProperty>();
                myPlayer.Hole_Card1.gameObject.SetActive(true);
                myPlayer.Hole_Card2.gameObject.SetActive(true);
                // Hide dummy cards for our own player since real cards are shown
                myPlayer.DummyCardsParent.transform.GetChild(0).gameObject.SetActive(false);
                myPlayer.DummyCardsParent.transform.GetChild(1).gameObject.SetActive(false);
            }

            // Re-enable the coin/chip amount display — TriggerStateOutofGame hides it unconditionally
            if (PlayerStateManager.Instance?.Amountobject != null)
                PlayerStateManager.Instance.Amountobject.SetActive(true);

            // Re-enable the raise slider button — may have been hidden by TriggerStateOutofGame flow
            if (PokerActionPanel.Instance?.BetRaiseSliderBtn != null)
                PokerActionPanel.Instance.BetRaiseSliderBtn.SetActive(true);

            // Hide "Wait For Next Round" panel — TriggerStateOutofGame shows it unconditionally
            if (PlayerStateManager.Instance?.waitForNextRound != null)
                PlayerStateManager.Instance.waitForNextRound.SetActive(false);

            // Hide the "Game Starting" text
            if (GameStartManager.Instance != null)
                GameStartManager.Instance.GameStarWaitTextGameObject.SetActive(false);

            // Money on the table. Nothing wagered before I reconnected exists on this machine - every
            // amount arrived as a [ClientRpc] I was not here for - so the pot read 0 and the call
            // amount was wrong. The server-side totals are the only surviving record.
            BigInteger restoredPot = 0;
            BigInteger totalRoundBets = 0;
            BigInteger highestRoundBet = 0;
            foreach (var player in playersList)
            {
                if (player == null) continue;
                BigInteger handBet = LocalSettings.StringToBigInteger(player.syncedPokerHandBet);
                BigInteger roundBet = LocalSettings.StringToBigInteger(player.syncedPokerRoundBet);
                player.PokerTotalWholeBetAmount = handBet;
                player.pokerTotalBetCash = roundBet;
                // The chips that belong in front of this seat for the round in progress. Skipping
                // this left a reconnected table with no money visible on it anywhere.
                player.RestorePokerBetChipVisual(roundBet);
                restoredPot += handBet;
                totalRoundBets += roundBet;
                if (roundBet > highestRoundBet) highestRoundBet = roundBet;
            }
            if (restoredPot > 0)
            {
                if (PokerActionPanel.Instance != null)
                {
                    PokerActionPanel.Instance.amountPlacedOnBet = restoredPot;
                    PokerActionPanel.Instance.CurrentTargetBetAmount = highestRoundBet;
                }

                // A live hand does NOT keep the whole wager in the middle. Money sits as chips in
                // front of each seat for the length of a betting round, and only moves into the pot
                // when the round ends (AllPokerBetsGoToFinalPoint, which is also what first turns the
                // pot panel on). Showing the panel unconditionally put a pot on the table mid-round
                // that nobody else could see - a "3000" that appeared out of nowhere on reconnect.
                // What has actually been swept is everything wagered this hand MINUS what is still
                // sitting in front of the players for the round in progress; when that is zero we are
                // still in the first round and the panel belongs hidden, exactly as before the drop.
                BigInteger sweptPot = restoredPot - totalRoundBets;
                if (Pot.instance != null && sweptPot > 0)
                {
                    Pot.instance.PotTxt.text = LocalSettings.Rs(sweptPot);
                    Pot.instance.PotPanel.SetActive(true);
                }
                Debug.Log($"[RestorePokerStateOnReconnect] Restored wagered={restoredPot}, inFront={totalRoundBets}, pot={sweptPot}, callTarget={highestRoundBet}");
            }

            // Restore community cards directly here — TriggerStateOutofGame() runs too early
            // (before SyncVars arrive on the reconnecting client) so its GenerateCommunityCardsForOutOfGame
            // may get empty data. By this point the SyncVar hook has already fired (DelayUntil IsMine),
            // so syncedCommunityCards and syncedDropCardsIndex are guaranteed to be populated.
            if (!string.IsNullOrEmpty(NetworkGameManager.Instance.syncedCommunityCards))
            {
                PokerManager.Instance.DestroyCommunityCards();
                PokerManager.Instance.GenerateCommunityCardsForOutOfGame();
                int dropIdx = NetworkGameManager.Instance.syncedDropCardsIndex;
                if (dropIdx > 0)
                    PokerManager.Instance.AssignCummintyCardToOutOFGame(dropIdx);
            }

            // My own action UI has to be re-armed explicitly. The loop above only writes the
            // currentState field (deliberately, so no Command mutates the authoritative hand) and
            // nothing else re-runs that state's local trigger - so a player who reconnected while
            // holding the turn got no action buttons and no turn timer, and the hand sat there
            // forever. Re-apply locally, and only for the two states whose trigger is pure UI:
            // TriggerStateBetPlaced() actually places a bet (PokerActionPanel.OnRaiseSliderBtnClick)
            // and must never be replayed here.
            PlayerState.STATE myRestoredState = myPlayer.playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState);
            if (myRestoredState == PlayerState.STATE.ExecutingTurn || myRestoredState == PlayerState.STATE.WaitingForTurn)
            {
                Debug.Log($"[RestorePokerStateOnReconnect] Re-arming local UI for my restored state {myRestoredState}");
                myPlayer.currentPlayerStateRef.UpdateCurrentPlayerStateAI(myRestoredState);
            }

            Debug.Log("✅ [Poker] Reconnect restore complete.");
        }

        public bool CheckAllPlayersTurn()
        {
            foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
            {
                if (item == null) continue; // destroyed during disconnect
                if (item.LWTurn)
                    return false;
            }
            return true;
        }
        //void ResetAvailablePositions()
        //{
        //    for (int i = 0; i < position_availability.Length; i++)
        //    {
        //        if (i != 0)
        //            position_availability[i].is_reserved = null;
        //    }
        //}


        public void AddingPlayer(PlayerInfo info)
        {
            //Constants_M.Log($"Adding Player {info.gameObject.name}");
            playersList.Add(info);
            SetAllPlayersPositions();


        }


        //public override void OnJoinedRoom()
        //{
        //    Debug.Log("Another Player Joined");

        //    GameObject NewPlayerInstantiated = PhotonNetwork.Instantiate(playerPrefab.name, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, 0);
        //    NewPlayerInstantiated.name = NewPlayerInstantiated.GetComponent<PhotonView>().Controller.NickName;
        //}





        public void UpdateAllTextsOfCash(BigInteger cash)
        {
            //All Texts Data
            Debug.Log("Updated TExt is " + cash);
        }



        //public override void OnPlayerEnteredRoom(Player newPlayer)
        //{
        //    Debug.Log("Player " + newPlayer.NickName + " has joined the room.");
        //    StartCoroutine(CheckAllPlayersConnected());
        //}


        void PlayersDestroy()
        {
            for (int i = 0; i < playersList.Count; i++)
            {
                Destroy(playersList[i]);
            }
        }

        public void PlayerTotalChipsUpdate(BigInteger chips)
        {
            chips.Show("Chips");
            LocalSettings.SetTotalChips(chips);
            if (chips < 0)
            {
                // Minus To server Chips
                // RestAPILuqman.Instance.SubtractChips(chips);
            }
            else if (chips > 0)
            {
                // Add To Server Chips
                // RestAPILuqman.Instance.AddChips(chips);
            }
            // Debug.LogError("Check Winner sound" + SoundManager.AllSounds.Reward.name + "....Chips   " + chips);

            // SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.Reward, false);
            //   UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
            UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());
            LocalSettings.GetTotalChips().Show();
            LocalSettings.GetPokerBuyInChips().Show();
            // UIManager.Instance.GetMyPlayerInfo().startCash.ShowBlue();

        }
        public void AITotalChipsUpdate(BigInteger chips)
        {
            if (UIManager.Instance.GetAIPlayerInfo() != null)
            {
                //  UIManager.Instance.GetAIPlayerInfo().AI_Amount += chips;
                LocalSettings.AI_Amount += chips;
                UIManager.Instance.GetAIPlayerInfo().PokerTotalCashTxt.text = LocalSettings.Rs(LocalSettings.AI_Amount);



                UIManager.Instance.GetAIPlayerInfo().MyTotalCashTextUpdate();
            }
            if (chips < 0)
            {
                // Minus To server Chips
                // RestAPILuqman.Instance.SubtractChips(chips);
            }
            else if (chips > 0)
            {
                // Add To Server Chips
                // RestAPILuqman.Instance.AddChips(chips);
            }
            // Debug.LogError("Check Winner sound" + SoundManager.AllSounds.Reward.name + "....Chips   " + chips);

            // SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.Reward, false);
            //   UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
            UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());

        }

        //public override void OnPlayerLeftRoom(Player otherPlayer)
        //{
        //    GameStartManager.Instance.ResetWaiting();
        //    StartCoroutine(CheckAllPlayersConnected());
        //    //Debug.Log("Other Player named " + otherPlayer.NickName + " left this Room");
        //    base.OnPlayerLeftRoom(otherPlayer);


        //}


        //public override void OnLeftRoom()
        //{

        //    Game_Play.Instance.OnroomLeftLoadScene();

        //}

        //public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        //{
        //    Debug.LogWarning("Player properties updated: " + changedProps.ToStringFull() + targetPlayer.NickName);
        //    if (changedProps.ContainsKey(LocalSettings.networkPosition))
        //    {
        //        PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
        //        Debug.Log("updated Network Position");
        //    }
        //}

        public void ShowParticle(RectTransform obj)
        {
            GameObject particle = Instantiate(Particles[UnityEngine.Random.Range(0, GameManager.Instance.Particles.Length)]);
            particle.transform.position = obj.transform.position;
            LocalSettings.SetPosAndRect(particle, obj, PlayerTable);
            particle.SetActive(true);
        }

        // For Extra Players


        //dsfklsdlkjfsdklj
        //public override void OnRoomListUpdate(List<RoomInfo> roomList)
        //{
        //    if (NetworkSettings.roomInfos != null)
        //        NetworkSettings.roomInfos.Clear();
        //    NetworkSettings.roomInfos = roomList;
        //    Debug.Log("Room Name: " + NetworkSettings.roomInfos.Count + " | Player Count: " + "/");
        //    foreach (RoomInfo roomInfo in NetworkSettings.roomInfos)
        //    {
        //        if (roomInfo == null)
        //        {
        //            NetworkSettings.roomInfos.Remove(roomInfo);
        //        }
        //        Debug.LogError(roomInfo.CustomProperties +
        //         "   Room Name: " + roomInfo.Name + " | Player Count: " + roomInfo.PlayerCount + "/" + roomInfo.MaxPlayers);
        //    }
        //}




        #region When application goes to background
        // Player In background In mobile Game
        private void OnApplicationPause(bool pause)
        {
            //bool istrue = PlayerStateManager.Instance.PlayingList.Exists(playerInfo => playerInfo.photonView.ViewID == UIManager.Instance.GetMyPlayerInfo().photonView.ViewID);

            //if (MatchHandler.IsPoker() && istrue)
            //{
            //    playersList[0].playerBackGround(pause);
            //    PhotonNetwork.SendAllOutgoingCommands();

            //}

            //if (pause)
            //{
            //    if (MatchHandler.IsPoker())
            //        isRunINBackGround = pause;
            //    // GameOnBackGround = true;
            //    if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            //    {
            //        if (!PhotonNetwork.IsMasterClient)
            //        {
            //            return;
            //        }
            //        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
            //        {
            //            return;
            //        }

            //        PhotonNetwork.SetMasterClient(PhotonNetwork.MasterClient.GetNext());
            //        PhotonNetwork.SendAllOutgoingCommands();
            //    }
            //}
        }
        #endregion

        #region Update xp for current player




        #endregion
    }




    [System.Serializable]
    public class PositionAvailability
    {
        public RectTransform Pos;
        public GameObject is_reserved;
        [ShowOnly]
        public int networkSeat;
        public GameObject sitHere;
    }


}
public enum EnumNetworkEventCodes
{
    BeginPrivateGame = 171,
    NextPlayerTurn = 172,
    StartWithBots = 173,
    StartGame = 174,
    SpawnPlayer = 175,
    BookSeat = 176,
    FinishedGame = 178,
    ReconnectGame = 181,
    ReadyToPlay = 179,
    ExitGame = 180,
    UpdateRoomState = 182,   // Poker room state broadcast via NetworkGameManager event system
    PlayerPacked = 183,      // Broadcast that a player has packed their cards
    PokerCardsArray = 184,   // Shuffled 52-card array sent from server to clients
    SetRoundId = 185,        // Round ID sent from server to clients after casino API call
}
