using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using System.Net.NetworkInformation;

namespace POKER
{
    public class PlayerStateManager : MonoBehaviour
    {

        public List<PlayerInfo> PlayingList = new List<PlayerInfo>();

        private static PlayerStateManager _instance;
        public GameObject waitForNextRound;
        public GameObject taptoSitHere;
        public GameObject Amountobject;

        public static PlayerStateManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<PlayerStateManager>();
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance == null)
                _instance = this;
            NetworkGameManager.OnEventReceived += OnNetworkEvent;
        }

        private void OnDestroy()
        {
            NetworkGameManager.OnEventReceived -= OnNetworkEvent;
        }

        /// <summary>
        /// Listens for PlayerPacked events broadcast via NetworkGameManager.
        /// Fires on server (from CmdRiseEvent) and on all clients (from RiseEventRpc).
        /// </summary>
        private void OnNetworkEvent(int eventCode, string data, Mirror.NetworkConnectionToClient sender)
        {
            if (eventCode == (int)EnumNetworkEventCodes.PlayerPacked)
                LocalUpdateListOnPacked();
        }

        public void ArrayToListInPlayingList(int[] viewIds)
        {
            PlayingList.Clear();

            for (int i = 0; i < viewIds.Length; i++)
            {
                for (int j = 0; j < GameManager.Instance.playersList.Count; j++)
                {
                    if (GameManager.Instance.playersList[j].View_ID_Offline == viewIds[i])
                    {
                        PlayingList.Add(GameManager.Instance.playersList[j]);
                        break;
                    }
                }
            }
        }

        private void OnEnable()
        {
            Debug.Log("State player enabled");
        }
        private void OnDisable()
        {
            Debug.Log("State player disabled");
        }
        public void UpdatePlayingList()
        {

            //if (PhotonNetwork.IsMasterClient)
            //{
            // For Current Players Playing List Sorted                
            // photonView.RPC("SetNewPlayerListForGamePlay", RpcTarget.All);

            SetNewPlayerListForGamePlay();
            if (!MatchHandler.isOffline())
            {
                int[] temp = new int[PlayingList.Count];
                for (int i = 0; i < PlayingList.Count; i++)
                {
                    temp[i] = PlayingList[i].View_ID_Offline;
                }

                // PlayingList is derived locally from playersList + AbleToJoin state.
                // No network sync needed — each client builds it via SetNewPlayerListForGamePlay().
            }
            //Debug.LogError("Array Length is + " + PlayingList.ToArray().Length);
            // }
        }

        //[PunRPC]
        public void SetNewPlayerListForGamePlay()
        {
            // Use currentPlayerStateRef.currentState for both online and offline paths.
            // playerCustomProperties.GetPlayerStateProperty() reads from a SyncDictionary that
            // is only written when isOwned=true, so it is never populated on the server
            // (server doesn't own client-spawned player objects). currentState is reliably
            // kept in sync on all machines via [ClientRpc] UpdateCurrentPlayerStateOnNetwork.
            var allPlayers = GameManager.Instance.playersList;
            Debug.Log($"[SetNewPlayerList] playersList.Count={allPlayers.Count}, isServer={NetworkServer.active}");
            for (int i = 0; i < allPlayers.Count; i++)
            {
                var p = allPlayers[i];
                if (p == null) { Debug.Log($"[SetNewPlayerList] player[{i}] is NULL"); continue; }
                string stateRefStatus = p.currentPlayerStateRef != null ? "SET" : "NULL";
                string currentState = p.currentPlayerStateRef != null ? p.currentPlayerStateRef.currentState.ToString() : "N/A";
                Debug.Log($"[SetNewPlayerList] player[{i}] name={p.gameObject.name}, stateRef={stateRefStatus}, currentState={currentState}, ownerPlayerId={p.ownerPlayerId}");
            }
            PlayingList = allPlayers
                .Where(o => o != null && o.currentPlayerStateRef != null && o.currentPlayerStateRef.currentState == PlayerState.STATE.AbleToJoin)
                .OrderBy(o => o.myNetworkSeat)
                .ToList();
            Debug.Log($"[SetNewPlayerList] Sorted PlayingList count: {PlayingList.Count}");
        }



        //public void ShareNewListToOthers()
        //{
        //    //photonView.RPC("shareList", RpcTarget.All);
        //    //this is now handling from Player Current State Script
        //}


        public void UpdateListOnPlayerPack()
        {
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    // Server: update locally then broadcast to all clients via NetworkGameManager
                    LocalUpdateListOnPacked();
                    NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.PlayerPacked, "");
                }
                else
                {
                    // Client: send through NetworkGameManager (already has NetworkIdentity)
                    NetworkGameManager.Instance.CmdRiseEvent((int)EnumNetworkEventCodes.PlayerPacked, "");
                }
            }
            else
            {
                UpdateListOnPackedOnAI();
            }
        }

        //public void UpdateListOnPlayerOnStandUp()
        //{
        //    photonView.RPC("UpdateListOnPackedOnNetwork", RpcTarget.All);
        //}

        // Called locally on every machine via OnNetworkEvent — no Mirror attributes needed
        public void LocalUpdateListOnPacked()
        {
            for (int i = PlayingList.Count - 1; i >= 0; i--)
            {
                if (PlayingList[i] == null || PlayingList[i].currentPlayerStateRef.currentState == PlayerState.STATE.Packed || PlayingList[i].currentPlayerStateRef.currentState == PlayerState.STATE.OutOfTable)
                {
                    PlayingList.RemoveAt(i);
                }
            }
            //PlayerWonGame();
        }
        public void UpdateListOnPackedOnAI()
        {
            for (int i = PlayingList.Count - 1; i >= 0; i--)
            {
                if (PlayingList[i] == null || PlayingList[i].currentPlayerStateRef.currentState == PlayerState.STATE.Packed || PlayingList[i].currentPlayerStateRef.currentState == PlayerState.STATE.OutOfTable)
                {
                    PlayingList.RemoveAt(i);
                }
            }
            //PlayerWonGame();
        }

        public void RemainingPlayerWonGameAutomatic()
        {

            if (PlayingList.Count == 0)
            {
                // Everything in this branch except the reset coroutine at the end is local-player UI
                // and wallet work. A dedicated server has no local player, so GetMyPlayerInfo()
                // returns null there and every line below threw a NullReferenceException - which
                // killed the caller (TriggerStatePacked) half-way through and left the hand in limbo.
                PlayerInfo myInfo = UIManager.Instance != null ? UIManager.Instance.GetMyPlayerInfo() : null;
                if (myInfo != null
                    && (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn))
                {
                    myInfo.IAmWinner(true);


                    LocalSettings.SetPokerBuyInChips(PokerActionPanel.Instance.TotalPotAmount());

                    //GoldWinLoose.Instance.SendGold(GoldWinLoose.Trans.win, PokerActionPanel.Instance.TotalPotAmount().ToString());
                    UIManager.Instance.TotalWinsAmount += PokerActionPanel.Instance.TotalPotAmount();
                    UIManager.Instance.TotalWinHands++;
                    if (MatchHandler.IsPoker())
                    {
                        UIManager.Instance.UpdateTheWinAmount(LocalSettings.totalcashWinLossKey, LocalSettings.TotalHandsKey, LocalSettings.WinHandsKey);
                        myInfo.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, LocalSettings.GetPokerBuyInChips());
                    }

                    PokerActionPanel.Instance.ActionPanelPoker.SetActive(false);



                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);


                }
                //Before
                if (myInfo != null)
                {
                    myInfo.FillerImage.gameObject.SetActive(false);
                    UpdateBuyInChipsForPoker(myInfo);
                }
                if (!MatchHandler.isOffline())
                {
                    if (NetworkServer.active)
                    {
                        StartCoroutine(WaitBeforeReset(1));
                    }
                }
                else
                {
                    Debug.Log("WaitBeforeReset");
                    StartCoroutine(WaitBeforeReset(1));
                }
            }
            else if (PlayingList.Count <= 1)
            {

                Debug.Log("RemainingPlayerWonGameAutomatic Called....." + RoomStateManager.Instance.CurrentRoomState);
                ///Before
                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
                {
                    PlayingList[0].IAmWinner(true);

                    if (CheckOfflineOrOnlineLocalPlayerBool(PlayingList[0]))
                    {

                        if (MatchHandler.IsPoker() || MatchHandler.isOffline())
                        {
                            if (PlayingList[0].gameObject.name != LocalSettings.AI_Name)
                            {
                                LocalSettings.SetPokerBuyInChips(PokerActionPanel.Instance.TotalPotAmount());
                                //GoldWinLoose.Instance.SendGold(GoldWinLoose.Trans.win, PokerActionPanel.Instance.TotalPotAmount().ToString());
                                UIManager.Instance.TotalWinsAmount += PokerActionPanel.Instance.TotalPotAmount();
                                UIManager.Instance.TotalWinHands++;

                                if (MatchHandler.IsPoker())
                                {
                                    UIManager.Instance.UpdateTheWinAmount(LocalSettings.totalcashWinLossKey, LocalSettings.TotalHandsKey, LocalSettings.WinHandsKey);
                                    PlayingList[0].playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, LocalSettings.GetPokerBuyInChips());
                                }
                                PokerActionPanel.Instance.ActionPanelPoker.SetActive(false);
                            }
                            else
                            {
                                LocalSettings.AI_Amount += PokerActionPanel.Instance.TotalPotAmount();
                            }


                        }
                        SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                        SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                    }

                }
                ///After
                // if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
                //    UIManager.Instance.GetMyPlayerInfo().IAmWinner(true);

                //Before
                PlayingList[0].FillerImage.gameObject.SetActive(false);
                PlayingList[0].FillerImage.gameObject.SetActive(false);

                UpdateBuyInChipsForPoker(PlayingList[0]);
                if (!MatchHandler.isOffline())
                {
                    if (NetworkServer.active)
                    {
                        StartCoroutine(WaitBeforeReset(1));
                    }
                }
                else
                {
                    Debug.Log("WaitBeforeReset");

                    StartCoroutine(WaitBeforeReset(1));
                }
                //PlayerstateChangeForNextRound();

            }
            else
            {
                //PlayerTurnManager.Instance.GoToNextTurn();
            }
        }

        bool CheckOfflineOrOnlineLocalPlayerBool(PlayerInfo info)
        {
            if (MatchHandler.IsPoker())
                return PlayingList[0].IsMine();
            return true;
        }
        public void UpdateBuyInChipsForPoker(PlayerInfo pInfo)
        {
            StartCoroutine(UploadBuyInChips(pInfo, 1));
        }

        IEnumerator UploadBuyInChips(PlayerInfo pInfo, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (MatchHandler.IsPoker())
            {
                pInfo.PokerTotalCashTxt.text = LocalSettings.Rs(pInfo.playerCustomProperties.GetCustomBigIntegerData(LocalSettings.TotalChips));
            }
            else
            {
                if (pInfo.gameObject.name != LocalSettings.AI_Name)
                {

                    pInfo.PokerTotalCash = LocalSettings.GetTotalChips();
                    UIManager.Instance.PlayerTotalCashText.text = pInfo.PokerTotalCashTxt.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
                    UIManager.Instance.PlayerTotalCashText.text.Show();
                }
                else
                {
                    pInfo.PokerTotalCash = LocalSettings.AI_Amount;
                    pInfo.PokerTotalCashTxt.text = LocalSettings.Rs(LocalSettings.AI_Amount);
                }
            }

            // Debug.LogError("3.....Check Here For Poker Cash....." + pInfo.PokerTotalCashTxt.text);
            //Debug.LogError("playeName....." + pInfo.player.NickName + "........Pocker Check BetAmount...." + pInfo.player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey));

        }


        IEnumerator WaitBeforeReset(float waitTime)
        {

            yield return new WaitForSeconds(waitTime);
            if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.CardDistributing || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
            {
                RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ShowingResults);
                Debug.Log("Here is your error 3......." + RoomStateManager.Instance.CurrentRoomState);
                //PlayerStateManager.Instance.PlayingList.Clear();
            }
        }



        public PlayerInfo ReturnPlayer(int index)
        {
            // Mirror: use direct index into PlayingList (replaces PUN NickName matching)
            if (index >= 0 && index < PlayingList.Count && PlayingList[index] != null)
                return PlayingList[index];
            return null;
        }


        void UpdateAllListsOnPlayerleft(string ActorNumber)
        {

            UpdatePlayerListOnPlayerLeft(ActorNumber);
            UpdatePlayingListOnPlayerLeft(ActorNumber);
        }

        void UpdatePlayerListOnPlayerLeft(string ActorNumber)
        {
            for (int i = 0; i < GameManager.Instance.playersList.Count; i++)
            {
                if (GameManager.Instance.playersList[i] == null)
                    GameManager.Instance.playersList.RemoveAt(i);
                else if (GameManager.Instance.playersList[i].playerCustomProperties.name == ActorNumber)
                    GameManager.Instance.playersList.RemoveAt(i);
            }
        }

        void UpdatePlayingListOnPlayerLeft(string ActorNumber)
        {
            for (int i = 0; i < PlayingList.Count; i++)
            {
                if (PlayingList[i] == null)
                {
                    PlayingList.RemoveAt(i);
                    //GiveTurnToNext();
                }
                else if (PlayingList[i].playerCustomProperties.name == ActorNumber)
                {
                    if (PlayingList[i].getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                    {
                        int a = i;
                        a++;
                        if (a >= PlayingList.Count)
                        {
                            a = 0;
                        }
                        int CheckNumber = 0;
                        if (MatchHandler.IsPoker())
                        {
                            for (int k = 0; k < PlayingList.Count; k++)
                            {
                                if (!PlayingList[k].isBetPlacedPocker)
                                {
                                    CheckNumber++;
                                    // Debug.LogError("value of CheckNumber in Player State manager.....1" + CheckNumber);
                                }

                            }

                            if (CheckNumber <= 1 && PlayingList.Count > 2)
                            {
                                Debug.LogError("value of CheckNumber in Player State manager.....2" + CheckNumber);
                                RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ABFirstTurn);
                                GiveTurnToNext(PlayingList[i]);
                                //PokerActionPanel.Instance.GiveTurnToNextPlayerPocker();
                            }
                            else
                            {
                                GiveTurnToNext(PlayingList[i]);
                            }




                        }
                        // Debug.Log("Player Left Room Called " + PlayingList[a].player.NickName + PlayingList[a].getCurrentPlayerState().currentState);


                    }
                    else
                    {
                        if (PlayingList[i].IsSideShow && PlayingList[i].currentPlayerStateRef.currentState == PlayerState.STATE.RecieverSideShow)
                        {
                            Game_Play.Instance.OnClickCancelSideShowBtn();
                            // Debug.Log("Player Left Room Called " + PlayingList[i].player.NickName);
                        }
                        else
                        {
                            //Debug.LogError("Player Left Room Called " + PlayingList[i].player.NickName);
                            if (PlayingList[i].IsSideShow && PlayingList[i].currentPlayerStateRef.currentState == PlayerState.STATE.SenderSideShow)
                            {
                                // Debug.LogError("Player Left Room Called " + PlayingList[i].player.NickName);
                                Game_Play.Instance.OnClickCancelSideShowBtn();
                                PlayingList[i].AllSideShowPanelsFalse(false);

                            }
                        }

                    }
                    PlayingList.RemoveAt(i);
                }
            }

        }


        void GiveTurnToNext(PlayerInfo plyerInfo)
        {
            if (NetworkServer.active)
            {
                int nextPlayerInt = PlayingList.IndexOf(plyerInfo);
                //if (!dontIcrease)
                //{
                nextPlayerInt++;
                //}
                //dontIcrease = false;
                if (nextPlayerInt >= PlayingList.Count)
                    nextPlayerInt = 0;

                //Debug.LogError("Current Player Index: " + nextPlayerInt);
                PlayingList[nextPlayerInt].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
            }
        }


        //public void UpdatePlayerIndexFromPlayingList()
        //{
        //    for (int i = 0; i < PlayingList.Count; i++)
        //    {
        //        PlayingList[i].PlayerIndexInPlayingList = i;
        //    }
        //}





        public bool CheckIfAllPlayersPlayedMaxChaals()
        {
            foreach (PlayerInfo plyrinfo in PlayingList)
            {
                if (plyrinfo.MyChaalsPlayedCounter < UIManager.Instance.TotalChals)
                    return false;
            }
            return true;
        }

        public void ShowCardsOfAllPlayers()
        {
            for (int i = 0; i < PlayingList.Count; i++)
            {
                if (PlayingList[i].IsMine())
                {
                    PlayingList[i].ShowCardsFromBlind();
                    PlayingList[i].IsSeen = true;
                    PlayingList[i].playerCustomProperties.SetCustomBoolData("is_seen", true);
                }

                PlayingList[i].GiveSeenAlertToAll();
            }
        }





        public void SideShowAndShowbtn()
        {
            if (PlayingList.Count == 2)
            {
                UIManager.Instance.showBtn.gameObject.SetActive(true);
                UIManager.Instance.sideShowBtn.gameObject.SetActive(false);
                if (UIManager.Instance.GetMyPlayerInfo().IsSeen)
                {
                    UIManager.Instance.showBtn.interactable = true;
                }
            }
            else
            {
                UIManager.Instance.showBtn.gameObject.SetActive(false);
                UIManager.Instance.sideShowBtn.gameObject.SetActive(true);
                SideShowPrev();

            }

        }

        public int SideShowPrev()
        {
            int myPlayerInt = PlayingList.IndexOf(UIManager.Instance.GetMyPlayerInfo());
            if (UIManager.Instance.GetMyPlayerInfo().IsSeen)
            {
                myPlayerInt--;
                if (myPlayerInt < 0)
                {
                    myPlayerInt = PlayingList.Count - 1;
                }



                if (PlayingList[myPlayerInt].IsSeen)
                {
                    UIManager.Instance.sideShowBtn.interactable = true;
                }

            }
            return myPlayerInt;
        }
        public int SideShowNext()
        {
            int myPlayerInt = PlayingList.IndexOf(UIManager.Instance.GetMyPlayerInfo());

            myPlayerInt++;
            if (myPlayerInt >= PlayingList.Count)
            {
                myPlayerInt = 0;
            }
            return myPlayerInt;
        }





        #region PhotonNetworkCallbacks

        //public override void OnPlayerLeftRoom(Player otherPlayer)
        //{

        //    //if (UIManager.Instance.GetMyPlayerInfo().IsSideShow && UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.SideShow)
        //    //{
        //    //    UIManager.Instance.GetMyPlayerInfo().GivesideShowAlertToAll(false);

        //    //}

        //    if (!otherPlayer.IsMasterClient)
        //    {
        //        UpdateAllListsOnPlayerleft(otherPlayer.NickName);
        //        if (MatchHandler.IsPoker())
        //            RemainingPlayerWonGameAutomatic();
        //    }
        //    else
        //    {
        //        // Debug.LogError("Old master client notice ______________________");
        //    }




        //}
        //public void PlayerstateChangeForNextRound()
        //{
        //    if (photonView.IsMine)
        //    {
        //        for (int i = 0; i < GameManager.Instance.playersList.Count; i++)
        //        {
        //            GameManager.Instance.playersList[i].currentStateRef.UpdateCurrentState(PlayerState.STATE.AbleToJoin);

        //        }
        //    }
        //}




        bool dontIcrease;

        //public override void OnMasterClientSwitched(Player newMasterClient)
        //{
        //    //Debug.LogError("New master client is now: " + newMasterClient.NickName);
        //    //if (PhotonNetwork.IsMasterClient)
        //    //{
        //    //    for (int i = 0; i < PlayingList.Count; i++)
        //    //    {
        //    //        if (PlayingList[i].getCurrentState().currentState == PlayerState.STATE.ExecutingTurn)
        //    //        {
        //    //            //dontIcrease = true;
        //    //            GiveTurnToNext(PlayingList[i]);

        //    //        }
        //    //    }
        //    //}
        //}


        public void OnLeftRoom()
        {
            //  Debug.LogError("I have left the Room");

        }

        #endregion



        // For Background Player



    }
}