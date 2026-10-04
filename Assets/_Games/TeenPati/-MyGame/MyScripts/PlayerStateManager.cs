using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using System.Net.NetworkInformation;

namespace TeenPattiGame
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
        }


        public void ArrayToListInPlayingList(int[] viewIds)
        {
            TPLog.Flow("PlayerStateManager", "ArrayToListInPlayingList - rebuilding PlayingList from " + (viewIds != null ? viewIds.Length : 0) + " network ids");
            PlayingList.Clear();

            for (int i = 0; i < viewIds.Length; i++)
            {
                for (int j = 0; j < GameManager.Instance.playersList.Count; j++)
                {
                    if (GameManager.Instance.playersList[j].this_photonView.netId == viewIds[i])
                    {
                        TPLog.Flow("PlayerStateManager", "netId " + viewIds[i] + " matched player '" + GameManager.Instance.playersList[j].name + "' -> added to PlayingList");
                        PlayingList.Add(GameManager.Instance.playersList[j]);
                        break;
                    }
                }
            }
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
                TPLog.Flow("PlayerStateManager", "UpdatePlayingList - " + PlayingList.Count + " players are in this hand");
                int[] temp = new int[PlayingList.Count];
                for (int i = 0; i < PlayingList.Count; i++)
                {
                    temp[i] = (int)PlayingList[i].this_photonView.netId;
                }

                // For New Player Who is Out of Game 
                // Will Get Playing List from this Property of Room
                if (MirrorNetwork.Instance.isMasterClient)
                {
                    TPLog.Flow("PlayerStateManager", "Master client -> saving the playing list on the room so late joiners can read it");
                    //if (PhotonNetwork.IsConnectedAndReady)
                    TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.SetPlayingList(LocalSettings.playingListToArray, temp);
                }
                else
                {
                    TPLog.Flow("PlayerStateManager", "Not master client -> playing list is only local here");
                }
            }
            //Debug.LogError("Array Length is + " + PlayingList.ToArray().Length);
            // }
        }

        //[PunRPC]
        public void SetNewPlayerListForGamePlay()
        {

            //            List<PlayerInfo> SortedList = GameManager.Instance.playersList.Where(o => o.getCurrentPlayerState().currentState == PlayerState.STATE.AbleToJoin).OrderBy(o => o.myNetworkSeat).ToList();
            List<PlayerInfo> SortedList = new List<PlayerInfo>();
            if (!MatchHandler.isOffline())
            {
                // Old for Multiplayer work prev 100%
                //  List<PlayerInfo> SortedList = GameManager.Instance.playersList.Where(o => o.player.GetPlayerStateProperty(LocalSettings.playerState) == PlayerState.STATE.AbleToJoin).OrderBy(o => o.myNetworkSeat).ToList();
                // New from online AMJAD
                SortedList = GameManager.Instance.playersList.Where(o => o.playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState) == PlayerState.STATE.AbleToJoin).OrderBy(o => o.myNetworkSeat).ToList();
                TPLog.Flow("PlayerStateManager", "Online: " + SortedList.Count + " of " + GameManager.Instance.playersList.Count + " players are AbleToJoin -> they play this hand");
            }
            else
            {
                SortedList = GameManager.Instance.playersList.Where(o => o.currentPlayerStateRef.currentState == PlayerState.STATE.AbleToJoin).OrderBy(o => o.myNetworkSeat).ToList();
            }
            Debug.Log("Sorted List is " + SortedList);
            PlayingList = new List<PlayerInfo>();
            PlayingList = SortedList;


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
                TPLog.Flow("PlayerStateManager", "UpdateListOnPlayerPack -> asking everyone to drop packed / left players");
                //  photonView.RPC("UpdateListOnPackedOnNetwork", RpcTarget.All);
                TeenPattiNNetworkManager.instance.CmdUpdateListOnPackedOnNetwork();
            }
            else
                UpdateListOnPackedOnNetwork();
        }




        public void UpdateListOnPackedOnNetwork()
        {
            for (int i = 0; i < PlayingList.Count; i++)
            {

                if (PlayingList[i].getCurrentPlayerState().currentState == PlayerState.STATE.Packed || PlayingList[i].getCurrentPlayerState().currentState == PlayerState.STATE.OutOfTable)
                {

                    "Remove at".Show();
                    TPLog.Flow("PlayerStateManager", "'" + PlayingList[i].name + "' is " + PlayingList[i].getCurrentPlayerState().currentState + " -> removed from PlayingList (" + (PlayingList.Count - 1) + " left)");
                    PlayingList.RemoveAt(i);
                }
            }
            //PlayerWonGame();
        }




        /// <summary>The local player's PlayerInfo, or null. There is no local player on the
        /// dedicated server, so UIManager.GetMyPlayerInfo() returns null there - and the leave path
        /// dereferenced it straight away. That threw two NullReferenceExceptions on every single
        /// leave (RemainingPlayerWonGameAutomatic and SideShowPrev, both reachable from
        /// PlayerInfo.OnDestroy -> OnPlayerLeftRoom), which aborted the server's own leave
        /// bookkeeping half way through.</summary>
        PlayerInfo LocalPlayerOrNull()
        {
            return UIManager.Instance != null ? UIManager.Instance.GetMyPlayerInfo() : null;
        }

        public void RemainingPlayerWonGameAutomatic()
        {

            TPLog.Flow("PlayerStateManager", "RemainingPlayerWonGameAutomatic - players left in hand = " + PlayingList.Count + ", room state = " + RoomStateManager.Instance.CurrentRoomState);
            if (PlayingList.Count == 0)
            {
                TPLog.Flow("PlayerStateManager", "Nobody left in the playing list -> I am declared the winner");
                PlayerInfo localPlayer = LocalPlayerOrNull();
                if (localPlayer == null)
                {
                    TPLog.Flow("PlayerStateManager", "No local player here (dedicated server) -> nothing to declare, the server ends the hand instead");
                    return;
                }

                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
                {
                    localPlayer.IAmWinner(true);

                    UIManager.Instance.TotalWinsAmount += Pot.instance.potSize;
                    UIManager.Instance.TotalWinHands++;
                    if (!MatchHandler.isOffline())
                        UIManager.Instance.UpdateTheWinAmount(LocalSettings.totalcashWinLossKey, LocalSettings.TotalHandsKey, LocalSettings.WinHandsKey);
                    if (MatchHandler.IsTeenPatti())
                    {
                        if (UIManager.Instance.GetMyPlayerInfo().this_photonView.isOwned)
                        {
                            TPLog.Flow("PlayerStateManager", "Pot of " + Pot.instance.potSize + " credited to me");
                            GameManager.Instance.PlayerTotalChipsUpdate(Pot.instance.potSize);


                        }
                        else
                        {
                            TPLog.Flow("PlayerStateManager", "This PlayerInfo is not mine -> no chips credited here");
                        }
                    }




                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);


                }
                //Before
                localPlayer.FillerImage.gameObject.SetActive(false);
                localPlayer.FillerImage.gameObject.SetActive(false);
                UIManager.Instance.ActionTable.SetActive(false);

                if (MirrorNetwork.Instance.isMasterClient)
                {
                    TPLog.Flow("PlayerStateManager", "Master client -> table reset scheduled in 1s");
                    StartCoroutine(WaitBeforeReset(1));
                }
                else
                {
                    TPLog.Flow("PlayerStateManager", "Not master client -> reset will be driven by the master");
                }
            }
            else if (PlayingList.Count <= 1)
            {

                TPLog.Flow("PlayerStateManager", "Only one player left ('" + PlayingList[0].name + "') -> he wins the hand by default");
                if (LocalPlayerOrNull() == null)
                {
                    TPLog.Flow("PlayerStateManager", "No local player here (dedicated server) -> the win UI and the pot credit are a client's job");
                    return;
                }
                RoomStateManager.Instance.CurrentRoomState.ToString().Show();
                ///Before
                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
                {
                    PlayingList[0].IAmWinner(true);

                    if (CheckOfflineOrOnlineLocalPlayerBool(PlayingList[0]))
                    {

                        TPLog.Flow("PlayerStateManager", "The last player standing is ME -> counting the win and the pot");
                        UIManager.Instance.TotalWinsAmount += Pot.instance.potSize;
                        UIManager.Instance.TotalWinHands++;

                        if (!MatchHandler.isOffline())
                        {
                            UIManager.Instance.UpdateTheWinAmount(LocalSettings.totalcashWinLossKey, LocalSettings.TotalHandsKey, LocalSettings.WinHandsKey);
                        }
                        if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
                        {
                            if (!MatchHandler.isOffline())
                            {
                                TPLog.Flow("PlayerStateManager", "Crediting pot " + Pot.instance.potSize + " to my balance");
                                GameManager.Instance.PlayerTotalChipsUpdate(Pot.instance.potSize);
                            }
                            else
                            {
                                if (PlayingList[0].gameObject.name == LocalSettings.AI_Name)
                                {
                                    GameManager.Instance.AITotalChipsUpdate(Pot.instance.potSize);
                                    // Debug.LogError("Here is check" + Pot.instance.potSize);
                                }
                                else
                                {
                                    GameManager.Instance.PlayerTotalChipsUpdate(Pot.instance.potSize);
                                    // Debug.LogError("Here is check...2" + Pot.instance.potSize );
                                }
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
                UIManager.Instance.ActionTable.SetActive(false);
                if (!MatchHandler.isOffline())
                {
                    if (MirrorNetwork.Instance.isMasterClient)
                    {
                        TPLog.Flow("PlayerStateManager", "Master client -> table reset scheduled in 1s");
                        StartCoroutine(WaitBeforeReset(1));
                    }
                    else
                    {
                        TPLog.Flow("PlayerStateManager", "Not master client -> reset will be driven by the master");
                    }
                }
                else
                    StartCoroutine(WaitBeforeReset(1));
                //PlayerstateChangeForNextRound();

            }
            else
            {
                TPLog.Flow("PlayerStateManager", PlayingList.Count + " players still in the hand -> game continues, no automatic winner");
                //PlayerTurnManager.Instance.GoToNextTurn();
            }
        }

        //Check Offline or Online Local player
        bool CheckOfflineOrOnlineLocalPlayerBool(PlayerInfo info)
        {
            if (MatchHandler.IsTeenPatti())
            {
                TPLog.Flow("PlayerStateManager", "Is the remaining player mine? " + PlayingList[0].this_photonView.isOwned);
                return PlayingList[0].this_photonView.isOwned;
            }
            return true;
        }




        IEnumerator WaitBeforeReset(float waitTime)
        {

            yield return new WaitForSeconds(waitTime);
            if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.CardDistributing || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
            {
                TPLog.Flow("PlayerStateManager", "WaitBeforeReset: room was " + RoomStateManager.Instance.CurrentRoomState + " -> moving to ShowingResults");
                RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ShowingResults);
                Debug.Log("Here is your error 3......." + RoomStateManager.Instance.CurrentRoomState);
                //PlayerStateManager.Instance.PlayingList.Clear();
            }
            else
            {
                TPLog.Flow("PlayerStateManager", "WaitBeforeReset: room is " + RoomStateManager.Instance.CurrentRoomState + " -> nothing to reset here");
            }
        }



        public PlayerInfo ReturnPlayer(int index)
        {
            for (int j = 0; j < PlayingList.Count; j++)
            {
                if (PlayingList[j] != null)
                {
                    // if (PhotonNetwork.PlayerList[index].NickName == PlayingList[j].GetComponent<PhotonView>().Controller.NickName)
                    return PlayingList[j].GetComponent<PlayerInfo>();
                }
            }
            return null;
        }


        void UpdateAllListsOnPlayerleft(int netId)
        {

            TPLog.Flow("PlayerStateManager", "Player with netId " + netId + " left -> cleaning both lists");
            UpdatePlayerListOnPlayerLeft(netId);
            UpdatePlayingListOnPlayerLeft(netId);
        }

        void UpdatePlayerListOnPlayerLeft(int netId)
        {
            for (int i = 0; i < GameManager.Instance.playersList.Count; i++)
            {
                if (GameManager.Instance.playersList[i] == null)
                {
                    TPLog.Flow("PlayerStateManager", "Found a destroyed entry in playersList -> removed");
                    GameManager.Instance.playersList.RemoveAt(i);
                }
                else if (GameManager.Instance.playersList[i].netId == netId)
                {
                    TPLog.Flow("PlayerStateManager", "'" + GameManager.Instance.playersList[i].name + "' (netId " + netId + ") removed from playersList");
                    GameManager.Instance.playersList.RemoveAt(i);
                }
            }
        }

        void UpdatePlayingListOnPlayerLeft(int netId)
        {
            for (int i = 0; i < PlayingList.Count; i++)
            {
                if (PlayingList[i] == null)
                {
                    TPLog.Flow("PlayerStateManager", "Found a destroyed entry in PlayingList -> removed");
                    PlayingList.RemoveAt(i);
                    //GiveTurnToNext();
                }
                else if (PlayingList[i].netId == netId)
                {
                    TPLog.Flow("PlayerStateManager", "The player who left was in the hand, his state was " + PlayingList[i].getCurrentPlayerState().currentState);
                    if (PlayingList[i].getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                    {
                        int a = i;
                        a++;
                        if (a >= PlayingList.Count)
                        {
                            a = 0;
                        }

                        if (MatchHandler.IsTeenPatti())
                            if (PlayingList[a].getCurrentPlayerState().currentState != PlayerState.STATE.RecieverSideShow)
                            {                                                                                                                                                //photon removal
                                // Debug.Log("Player Left Room Called " + PlayingList[a].player.NickName + PlayingList[a].getCurrentPlayerState().currentState);
                                TPLog.Flow("PlayerStateManager", "He left on his own turn -> passing the turn to '" + PlayingList[a].name + "'");

                                GiveTurnToNext(PlayingList[i]);
                            }

                    }
                    else
                    {
                        if (PlayingList[i].IsSideShow && PlayingList[i].currentPlayerStateRef.currentState == PlayerState.STATE.RecieverSideShow)
                        {
                            TPLog.Flow("PlayerStateManager", "He left while he had to answer a side-show -> cancelling the side-show");
                            Game_Play.Instance.OnClickCancelSideShowBtn();
                            // Debug.Log("Player Left Room Called " + PlayingList[i].player.NickName);
                        }
                        else
                        {
                            //Debug.LogError("Player Left Room Called " + PlayingList[i].player.NickName);
                            if (PlayingList[i].IsSideShow && PlayingList[i].currentPlayerStateRef.currentState == PlayerState.STATE.SenderSideShow)
                            {
                                TPLog.Flow("PlayerStateManager", "He left after asking for a side-show -> cancelling it and closing the panels");
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
            if (MirrorNetwork.Instance.isMasterClient)
            {
                TPLog.Flow("PlayerStateManager", "Master client -> moving the turn on from '" + plyerInfo.name + "'");
                int nextPlayerInt = PlayingList.IndexOf(plyerInfo);
                //if (!dontIcrease)
                //{
                nextPlayerInt++;
                //}
                //dontIcrease = false;
                if (nextPlayerInt >= PlayingList.Count)
                    nextPlayerInt = 0;

                //Debug.LogError("Current Player Index: " + nextPlayerInt);
                TPLog.Flow("PlayerStateManager", "Turn given to '" + PlayingList[nextPlayerInt].name + "' (index " + nextPlayerInt + ")");
                PlayingList[nextPlayerInt].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
            }
            else
            {
                TPLog.Flow("PlayerStateManager", "Not master client -> turn change ignored here, master decides");
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
                {
                    TPLog.Flow("PlayerStateManager", "'" + plyrinfo.name + "' has played " + plyrinfo.MyChaalsPlayedCounter + "/" + UIManager.Instance.TotalChals + " chaals -> not everyone is done");
                    return false;
                }
            }
            TPLog.Flow("PlayerStateManager", "Every player has played the maximum number of chaals");
            return true;
        }

        public void ShowCardsOfAllPlayers()
        {
            for (int i = 0; i < PlayingList.Count; i++)
            {
                if (PlayingList[i].this_photonView.isOwned)
                {
                    TPLog.Flow("PlayerStateManager", "Showing MY cards (forced show) and marking me as seen");
                    PlayingList[i].ShowCardsFromBlind();
                    PlayingList[i].IsSeen = true;
                    PlayingList[i].playerCustomProperties.SetCustomBoolData("is_seen", true);
                }
                else
                {
                    TPLog.Flow("PlayerStateManager", "'" + PlayingList[i].name + "' is not mine -> only telling everyone about his seen state");
                }

                PlayingList[i].GiveSeenAlertToAll();
            }
        }


        public void AllPlayersGameCompleted()
        {
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerStateManager", "AllPlayersGameCompleted -> telling everyone the hand is over");
                // photonView.RPC("UpDateAllPlayersGameCompleted", RpcTarget.All);
                TeenPattiNNetworkManager.instance.CmdUpDateAllPlayersGameCompleted();
            }
            else
                UpDateAllPlayersGameCompleted();
        }

        //  [PunRPC]
        public void UpDateAllPlayersGameCompleted()
        {
            TPLog.Flow("PlayerStateManager", "Hand marked complete -> " + PlayingList.Count + " players switched to Watching, timers off");
            GameResultsManager.Instance.isGameCompleted = true;
            for (int i = 0; i < PlayingList.Count; i++)
            {
                PlayingList[i].FillerImage.gameObject.SetActive(false);
                PlayingList[i].getCurrentPlayerState().currentState = PlayerState.STATE.Watching;
                PlayingList[i].IsSideShow = false;
            }
        }

        public void SideShowAndShowbtn()
        {
            // Pure local-HUD work: which of MY buttons is on, and whether I have seen MY cards.
            // The dedicated server has neither, and reaching SideShowPrev without a local player is
            // the second NullReferenceException every leave used to produce.
            if (LocalPlayerOrNull() == null)
            {
                TPLog.Flow("PlayerStateManager", "SHOW / SIDE-SHOW buttons skipped - no local player on this build");
                return;
            }

            if (PlayingList.Count == 2)
            {
                TPLog.Flow("PlayerStateManager", "Only 2 players left -> SHOW button is used (side-show hidden)");
                UIManager.Instance.showBtn.gameObject.SetActive(true);
                UIManager.Instance.sideShowBtn.gameObject.SetActive(false);
                if (UIManager.Instance.GetMyPlayerInfo().IsSeen)
                {
                    TPLog.Flow("PlayerStateManager", "I have seen my cards -> SHOW button enabled");
                    UIManager.Instance.showBtn.interactable = true;
                }
                else
                {
                    TPLog.Flow("PlayerStateManager", "I am still blind -> SHOW button stays disabled");
                }
            }
            else
            {
                TPLog.Flow("PlayerStateManager", PlayingList.Count + " players -> SIDE-SHOW button is used instead of SHOW");
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
                    TPLog.Flow("PlayerStateManager", "Previous player '" + PlayingList[myPlayerInt].name + "' has also seen his cards -> side-show allowed");
                    UIManager.Instance.sideShowBtn.interactable = true;
                }
                else
                {
                    TPLog.Flow("PlayerStateManager", "Previous player '" + PlayingList[myPlayerInt].name + "' is blind -> side-show not allowed");
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
        public void GetPlayerCardStatus()
        {
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerStateManager", "GetPlayerCardStatus -> refreshing blind/seen indicators on all clients");
                //   photonView.RPC("PlayerCardStatus", RpcTarget.All);
                TeenPattiNNetworkManager.instance.CmdPlayerCardStatus();
            }
            else
                PlayerCardStatus();
        }

        public void PlayerCardStatus()
        {
            for (int i = 0; i < PlayingList.Count; i++)
            {
                if (PlayingList[i].this_photonView.isOwned)
                {
                    TPLog.Flow("PlayerStateManager", "My player set back to BLIND at the start of the hand");
                    PlayingList[i].SeenIndicator.SetActive(false);
                    PlayingList[i].BlindIndicator.SetActive(true);
                    // ShowBtn.SetActive(true);
                }
                else
                {

                    TPLog.Flow("PlayerStateManager", "'" + PlayingList[i].name + "' set back to BLIND, his SHOW button hidden on my screen");
                    PlayingList[i].ShowBtn.SetActive(false);
                    PlayingList[i].SeenIndicator.SetActive(false);
                    PlayingList[i].BlindIndicator.SetActive(true);

                }
            }

        }




        #region PhotonNetworkCallbacks

        public void OnPlayerLeftRoom(int net)
        {
            TPLog.Flow("PlayerStateManager", "OnPlayerLeftRoom(netId " + net + ") - a player object was destroyed / left");



           // if (!MirrorNetwork.Instance.isMasterClient)
            {
                UpdateAllListsOnPlayerleft(net);                    //photon removal

                RemainingPlayerWonGameAutomatic();
            }
          //  else
            {
                // Debug.LogError("Old master client notice ______________________");
            }

            if (MatchHandler.IsTeenPatti())
            {
                TPLog.Flow("PlayerStateManager", "Refreshing SHOW / SIDE-SHOW buttons after the player left");
                SideShowAndShowbtn();
            }


        }





        bool dontIcrease;


        #endregion



        // For Background Player



    }
}
