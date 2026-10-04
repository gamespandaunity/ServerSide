using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensions;
//using Unity.Services.Authentication;

namespace TeenPattiGame
{

    public class GameManager : MonoBehaviour
    {
        public Image BalanceBg;
        public Sprite BlanceGold, BlanceSilver;
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
        public GameObject GameHistoryObject;

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
        }
        private void OnEnable()
        {
            BalanceBg.sprite = staticVariables.isgoldcoins ? BlanceGold : BlanceSilver;
            MirrorNetwork.OnWinCall += ShowGameHistory;
        }
        void OnDisable()
        {
            MirrorNetwork.OnWinCall -= ShowGameHistory;
        }

        public void ShowGameHistory(string text)
        {
            TPLog.Flow("GameManager", "Win event received ('" + text + "') -> opening the game history panel");
            GameHistoryObject.SetActive(true);
        }
        // Start is called before the first frame update
        void Start()
        {
            if (NetworkClient.active)
            {
                TPLog.Flow("GameManager", "Scene started as a CLIENT - player '" + staticVariables.userNickName + "'");
                MatchHandler.CurrentMatch = MatchHandler.MATCH.TeenPatti;
                POKER.LocalSettings.SetPlayername(staticVariables.userNickName);

                PlayerPrefs.SetString(POKER.LocalSettings.TotalChips, staticVariables.isgoldcoins ? ApiAndRoomManager.LastFetchedCoins.data.gold_balance : ApiAndRoomManager.LastFetchedCoins.data.silver_balance);
            }
            if (NetworkServer.active)
            {
                TPLog.Flow("GameManager", "Scene started as the dedicated SERVER");
                MatchHandler.CurrentMatch = MatchHandler.MATCH.TeenPatti;
                POKER.LocalSettings.SetPlayername("Server");
                PlayerPrefs.SetString(POKER.LocalSettings.TotalChips, "50000");
            }
            if (!MatchHandler.isOffline())
            {

                TPLog.Flow("GameManager", "Online mode -> waiting for TeenPattiNNetworkManager before starting");
                this.DelayUntil(() => TeenPattiNNetworkManager.instance, () =>
                {
                    TPLog.Flow("GameManager", "Network manager ready -> minimum players set to 2");
                    LocalSettings.SetMinPlayers(2);
                    StartingThings();
                });
            }
            else
            {
                LocalSettings.SetMinPlayers(2);
                StartingThings();
            }

            if (!MatchHandler.isOffline())
            {
                LocalSettings.GetSetGameName = MatchHandler.CurrentMatch.ToString();
            }
            else
                StartCoroutine(StartingAIThings());
        }




        public void StartingThings()
        {
            ReferenceEquals(UIManager.Instance.GetMyPlayerInfo(), null).Show("Refference Equal");
            if (ReferenceEquals(UIManager.Instance.GetMyPlayerInfo(), null))
            {
                TPLog.Flow("GameManager", "I have no player object yet -> spawning mine");
                StartCoroutine(StartPlayerInstantiateNow());
            }
            else
            {

                TPLog.Flow("GameManager", "My player object already exists -> only refreshing the seat layout");
                Invoke(nameof(RefreshAfterSomeTime), 0.5f);
                SitHereBtnStatus(false);
            }

            StartCoroutine(CheckAllPlayersConnected());
            PlayerTotalChipsUpdate(0);
            SitHereBtnStatus(false);
        }
        public IEnumerator StartingAIThings()
        {
            //if (UIManager.Instance.GetMyPlayerInfo() == null)
            yield return new WaitForSeconds(1);
            if (ReferenceEquals(UIManager.Instance.GetAIPlayerInfo(), null))
                StartAIrInstantiateNow();
            else
            {

                Invoke(nameof(RefreshAfterSomeTime), 0.5f);
                SitHereBtnStatus(false);
            }

            StartCoroutine(CheckAllPlayersConnected());
            PlayerTotalChipsUpdate(0);
            SitHereBtnStatus(false);
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

        public IEnumerator StartPlayerInstantiateNow()
        {
            if (!MatchHandler.isOffline())
            {
                yield return new WaitUntil(() => TeenPattiNNetworkManager.instance);

                if (TeenPattiNNetworkManager.instance && !NetworkServer.active)
                {
                    if (!MirrorNetwork.Instance.isMasterClient)
                    {
                        TPLog.Flow("GameManager", "I am not the master client -> waiting 2s so the master spawns first");
                        yield return new WaitForSeconds(2);
                    }
                    TPLog.Flow("GameManager", "Requesting my player spawn from the server");
                    Debug.Log("Is Ready");
                    // GameObject NewPlayerInstantiated = PhotonNetwork.Instantiate(playerPrefab.name, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, 0);       
                    TeenPattiNNetworkManager.instance.CmdSpawnPlayer(staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                    Debug.Log("Player Spawned1");

                    // NewPlayerInstantiated.name = NewPlayerInstantiated.GetComponent<PhotonView>().Controller.NickName;
                    Debug.Log("Player Spawned2");
                }
            }
            else
            {
                yield return new WaitForSeconds(0.01f);
                "Server".Show();
                GameObject NewPlayerInstantiated = Instantiate(playerPrefab, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
                NewPlayerInstantiated.name = LocalSettings.GetPlayerName();
            }
        }

        public void StartAIrInstantiateNow()
        {

            GameObject NewPlayerInstantiated = Instantiate(AI_Prefab, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
            NewPlayerInstantiated.name = LocalSettings.AI_Name;

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


            TPLog.Flow("GameManager", "Dealt " + CardsIndexes.Length + " cards for " + PlayerStateManager.Instance.PlayingList.Count + " players");
            //  Debug.LogError("Master called NumberOf Times: ");
            if (UIManager.Instance.GetMyPlayerInfo() != null)
                UIManager.Instance.GetMyPlayerInfo().SendPlayerCardsArray(CardsIndexes);
            else
                TPLog.Warn("GameManager", "Cards generated but my PlayerInfo is null -> they could not be sent");
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
            UpdateCard(0, 1);
            UpdateCard(1, 0);
            //UpdateCard(2, 0);
            //UpdateCard(3, 4);
            //UpdateCard(4, 3);

            // MULTIPLAYER ONLY. The slots above come from position_availability[].is_reserved,
            // which is filled using every player's myNetworkSeat. A client that joined or
            // reconnected late may never have received someone's seat rpc, so both players
            // collapse onto seat 0 and one slot stays empty - that player then gets no dealing
            // animation at all. Fill any empty slot from the playing list, which is built from
            // synced player state and therefore survives a reconnect.
            if (!MatchHandler.isOffline())
                FillEmptyDummyCardSlotsFromPlayingList();
        }

        // MULTIPLAYER ONLY
        void FillEmptyDummyCardSlotsFromPlayingList()
        {
            PlayerStateManager psm = PlayerStateManager.Instance;
            if (psm == null)
                return;

            for (int i = 0; i < psm.PlayingList.Count; i++)
            {
                PlayerInfo player = psm.PlayingList[i];
                if (player == null || !player.gameObject.activeInHierarchy)
                    continue;

                bool alreadyListed = false;
                for (int j = 0; j < TempPlayersListForDummyCards.Length; j++)
                {
                    if (TempPlayersListForDummyCards[j] == player.gameObject)
                    {
                        alreadyListed = true;
                        break;
                    }
                }
                if (alreadyListed)
                    continue;

                for (int j = 0; j < TempPlayersListForDummyCards.Length; j++)
                {
                    if (TempPlayersListForDummyCards[j] == null)
                    {
                        TPLog.Warn("GameManager", "Dealing slot " + j + " was empty (seat map incomplete) -> '" + player.name + "' put there from the playing list");
                        TempPlayersListForDummyCards[j] = player.gameObject;
                        break;
                    }
                }
            }
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
                    if (!MatchHandler.isOffline())
                        plyrinfo.ShowBtn.SetActive(plyrinfo.this_photonView.isOwned);          //photon Removal
                    else
                        plyrinfo.ShowBtn.SetActive(true);

                }
            }

            Invoke(nameof(RemoveRemainingDummyCards), 0.4f);
        }

        void ChageRoomStateToGamePlaying()
        {
            PlayerStateManager.Instance.PlayingList.Count.Show("Current state set to is playing");
            if (PlayerStateManager.Instance.PlayingList.Count > 1)
            {
                TPLog.Flow("GameManager", "Dealing finished with " + PlayerStateManager.Instance.PlayingList.Count + " players -> GameIsPlaying");
                RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsPlaying);
            }
            else
            {
                TPLog.Warn("GameManager", "Dealing finished but only " + PlayerStateManager.Instance.PlayingList.Count + " player left -> jumping to ShowingResults");
                RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ShowingResults);
            }
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

        void SetOtherPlayersPositioning(NetworkIdentity photonView)
        {
            if (!photonView.isOwned)
            {
                //SetPosAndRect(photonView.gameObject, GetAvailableTransform(photonView.gameObject), PlayerTable);
            }

        }

        void SetAllPlayersPositions()
        {
            for (int i = 0; i < playersList.Count; i++)
            {
                if (!playersList[i].seated)
                {
                    if (MatchHandler.isOffline())
                        SetOtherPlayersPositioning(playersList[i].GetComponent<NetworkIdentity>());
                    playersList[i].seated = true;
                }
            }
        }

        private IEnumerator CheckAllPlayersConnected()
        {
            if (!MatchHandler.isOffline())
            {
                if (TeenPattiNNetworkManager.instance == null)
                {
                    TPLog.Warn("GameManager", "CheckAllPlayersConnected aborted - no TeenPattiNNetworkManager in the scene");
                    yield break;
                }

                TPLog.Flow("GameManager", "Waiting until the connected player count matches my spawned player objects");
                yield return new WaitUntil(() => NetworkGameManager.Instance && NetworkGameManager.Instance.currentPlayerCount == playersList.Count);
                yield return null;
            }
            TPLog.Flow("GameManager", "All players connected - " + playersList.Count + " player objects on my side");
            Debug.Log("All Players Connected");
            SetAllPlayersPositions();
            Debug.Log("All Players Connected 2");
            yield return new WaitUntil(() => PositionsManager.Instance);

            PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
            if (!MatchHandler.isOffline())
            {
                if (playersList[0].this_photonView.isOwned && playersList[0].getCurrentPlayerState().currentState == PlayerState.STATE.OutOfGame)
                {


                    TPLog.Flow("GameManager", "I joined while a hand was already running -> reading the playing list from the room");
                    PlayerStateManager.Instance.ArrayToListInPlayingList(TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.GetPlayingList(LocalSettings.playingListToArray));
                    if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
                    {
                        TPLog.Flow("GameManager", "Hand is live (" + RoomStateManager.Instance.CurrentRoomState + ") -> showing the dummy cards of the players in it");
                        foreach (var item in PlayerStateManager.Instance.PlayingList)
                        {
                            if (MatchHandler.IsTeenPatti())
                            {
                                item.PlayerDummyCardsToShowParent.SetActive(true);
                                item.PlayerDummyCardsToShowParent.transform.GetChild(0).gameObject.SetActive(true);
                                item.PlayerDummyCardsToShowParent.transform.GetChild(1).gameObject.SetActive(true);
                                item.PlayerDummyCardsToShowParent.transform.GetChild(2).gameObject.SetActive(true);
                            }


                        }

                    }


                }

            }
        }
        public void AddingPlayer(PlayerInfo info)
        {
            playersList.Add(info);
            TPLog.Flow("GameManager", "PLAYER ADDED: '" + info.name + "' -> " + playersList.Count + " player object(s) on my screen");
            SetAllPlayersPositions();

            // A reconnect reloads the scene only for the reconnecting client. The
            // opponent keeps this GameManager, so refresh its local UI layout when
            // the newly spawned remote PlayerInfo is added.
            if (!MatchHandler.isOffline() && PositionsManager.Instance != null)
            {
                TPLog.Flow("GameManager", "Re-laying out the seats after '" + info.name + "' joined (also covers reconnects)");
                PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
            }

        }


        // public override void OnJoinedRoom()
        // {
        //     Debug.Log("Another Player Joined");          //photon Removal

        // }

        public void UpdateAllTextsOfCash(BigInteger cash)
        {
            //All Texts Data
            Debug.Log("Updated TExt is " + cash);
        }
        // public override void OnPlayerEnteredRoom(Player newPlayer)
        // {
        //     Debug.Log("Player " + newPlayer.NickName + " has joined the room.");         //photon Removal
        // }


        void PlayersDestroy()
        {
            for (int i = 0; i < playersList.Count; i++)
            {
                Destroy(playersList[i]);
            }
        }

        public void PlayerTotalChipsUpdate(BigInteger chips)
        {
            LocalSettings.SetTotalChips(chips);
            if (UIManager.Instance.GetMyPlayerInfo() != null)
            {
                if (!MatchHandler.isOffline())
                {
                    UIManager.Instance.GetMyPlayerInfo().playerCustomProperties.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
                }
                UIManager.Instance.GetMyPlayerInfo().MyTotalCashTextUpdate();
            }
            UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
        }

        // public override void OnPlayerLeftRoom(Player otherPlayer)
        // {
        //     GameStartManager.Instance.ResetWaiting();
        //     StartCoroutine(CheckAllPlayersConnected());                      //photon Removal
        //     base.OnPlayerLeftRoom(otherPlayer);


        // }


        // public override void OnLeftRoom()
        // {

        //     if (Game_Play.Instance != null)
        //         Game_Play.Instance.OnroomLeftLoadScene();            //photon Removal

        // }

        // public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        // {
        //     Debug.Log("Player properties updated: " + changedProps.ToStringFull() + targetPlayer.NickName);                                  //photon Removal
        //     if (changedProps.ContainsKey(LocalSettings.networkPosition))
        //     {
        //         PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
        //         Debug.Log("updated Network Position");
        //     }
        // }

        public void ShowParticle(RectTransform obj)
        {
            GameObject particle = Instantiate(Particles[UnityEngine.Random.Range(0, GameManager.Instance.Particles.Length)]);
            particle.transform.position = obj.transform.position;
            LocalSettings.SetPosAndRect(particle, obj, PlayerTable);
            particle.SetActive(true);
        }

        // For Extra Players


        //dsfklsdlkjfsdklj
        // public override void OnRoomListUpdate(List<RoomInfo> roomList)
        // {
        //     if (NetworkSettings.roomInfos != null)
        //         NetworkSettings.roomInfos.Clear();
        //     NetworkSettings.roomInfos = roomList;
        //     Debug.LogError("Room Name: " + NetworkSettings.roomInfos.Count + " | Player Count: " + "/");
        //     foreach (RoomInfo roomInfo in NetworkSettings.roomInfos)
        //     {
        //         if (roomInfo == null)
        //         {
        //             NetworkSettings.roomInfos.Remove(roomInfo);
        //         }
        //         Debug.LogError(roomInfo.CustomProperties +
        //          "   Room Name: " + roomInfo.Name + " | Player Count: " + roomInfo.PlayerCount + "/" + roomInfo.MaxPlayers);             //photon Removal
        //     }
        // }




        #region When application goes to background
        // Player In background In mobile Game
        private void OnApplicationPause(bool pause)
        {
            bool istrue = PlayerStateManager.Instance.PlayingList.Exists(playerInfo => playerInfo.this_photonView.netId == UIManager.Instance.GetMyPlayerInfo().this_photonView.netId);



            if (pause)
            {

                TPLog.Warn("GameManager", "APP WENT TO BACKGROUND (I am in the current hand: " + istrue + ")");
                if (TeenPattiNNetworkManager.instance && MirrorNetwork.Instance.isMasterClient)               //photon Removal
                {
                    if (!MirrorNetwork.Instance.isMasterClient)
                    {
                        return;
                    }
                    // if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
                    // {
                    //     return;
                    // }

                    // PhotonNetwork.SetMasterClient(PhotonNetwork.MasterClient.GetNext());
                    //  PhotonNetwork.SendAllOutgoingCommands();
                }
            }
        }
        #endregion

        #region AI Chips Update

        public void AITotalChipsUpdate(BigInteger chips)
        {
            //Debug.LogError($"chips {LocalSettings.AI_Amount} {chips}");
            if (UIManager.Instance.GetAIPlayerInfo() != null)
            {
                LocalSettings.AI_Amount += chips;
                //Debug.LogError($"chips After Addition {LocalSettings.AI_Amount}");
                UIManager.Instance.GetAIPlayerInfo().playerTotalCash.text = LocalSettings.Rs(LocalSettings.AI_Amount);
                UIManager.Instance.GetAIPlayerInfo().MyTotalCashTextUpdate();
            }
        }
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
