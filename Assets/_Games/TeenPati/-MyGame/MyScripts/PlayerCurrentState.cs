using System.Collections;
using System.Linq;
using System.Numerics;
using Mirror;
using UnityEngine;
using UnityExtensions;

namespace TeenPattiGame
{
    public class PlayerCurrentState : NetworkBehaviour
    {
        public PlayerState.STATE currentState;

        public PlayerInfo playerInfo;
        PlayerCurrentAnim playerAnim;
        UIManager uiManager;
        public bool giveTurnToNext;

        private void OnEnable()
        {
            playerInfo = GetComponent<PlayerInfo>();
            playerAnim = GetComponent<PlayerCurrentAnim>();
            uiManager = UIManager.Instance;
            giveTurnToNext = true;
        }


        int counter = 0;
        void OnUpdateCurrentState(PlayerState.STATE state)
        {
            switch (state)
            {
                case PlayerState.STATE.OutOfGame:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> OutOfGame (seated but sitting this hand out)");
                    TriggerStateOutofGame();
                    break;

                case PlayerState.STATE.Watching:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> Watching (hand finished for him)");
                    TriggerStateWatching();
                    break;

                case PlayerState.STATE.AbleToJoin:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> AbleToJoin (will take part in the next hand)");
                    TriggerAbleToJoin();
                    break;

                case PlayerState.STATE.WaitingForTurn:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> WaitingForTurn");
                    //Debug.LogError("  ex  ------------------- " + counter++);
                    TriggerWaitingForTurn();

                    break;

                case PlayerState.STATE.ExecutingTurn:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> ExecutingTurn (it is his turn now)");
                    TriggerStateExecutingTurn();
                    break;

                case PlayerState.STATE.BetPlaced:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> BetPlaced (chaal/blind played)");
                    TriggerStateBetPlaced();
                    break;

                case PlayerState.STATE.Packed:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> Packed (folded)");
                    TriggerStatePacked();
                    break;

                case PlayerState.STATE.RecieverSideShow:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> RecieverSideShow (has to accept or reject a side-show)");
                    TriggerStateRecieverSideShow();
                    break;
                case PlayerState.STATE.SenderSideShow:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> SenderSideShow (asked for a side-show)");
                    //Debug.LogError("  ex  ------------------- " + counter++);
                    TriggerStateSenderSideShow();

                    break;

                case PlayerState.STATE.OutOfTable:
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' -> OutOfTable (stood up / left the seat)");
                    TriggerStateOutOfTable();
                    break;
            }
        }

        public PlayerState.STATE GetCurrentState()
        {
            return currentState;
        }

        public void UpdateCurrentPlayerState(PlayerState.STATE state)
        {
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerCurrentState", "REQUEST state change for '" + gameObject.name + "': " + currentState + " -> " + state + " (sending to all clients)");
                // if (PhotonNetwork.IsConnectedAndReady)
                "Tillu".Show();
                // CmdUpdatePlayerState((int)state);
                // photonView.RPC("UpdateCurrentPlayerStateOnNetwork", RpcTarget.All, state);
                "Pillu".Show();
                playerInfo.playerCustomProperties.SetPlayerStateProperty(LocalSettings.playerState, (int)state);
                CmdUpdateCurrentPlayerStateOnNetwork((int)state);
            }
            else
                UpdateCurrentPlayerStateOnNetwork(state);
        }

        [Command(requiresAuthority = false)]
        public void CmdUpdatePlayerState(int state)
        {
            TPLog.Flow("PlayerCurrentState", "CmdUpdatePlayerState on server for '" + gameObject.name + "' -> " + (PlayerState.STATE)state);
            "chipu".Show();
            RpcUpdatePlayerState(state);
        }

        [ClientRpc]
        public void RpcUpdatePlayerState(int state)
        {
            TPLog.Flow("PlayerCurrentState", "RpcUpdatePlayerState received for '" + gameObject.name + "' -> saving property " + (PlayerState.STATE)state);
            playerInfo.playerCustomProperties.SetPlayerStateProperty(LocalSettings.playerState, (int)state);

        }
        //[PunRPC]
        public void UpdateCurrentPlayerStateOnNetwork(PlayerState.STATE state)
        {
            TPLog.Flow("PlayerCurrentState", "APPLYING state on '" + gameObject.name + "': " + currentState + " -> " + state);
            if (!MatchHandler.isOffline())
                playerInfo.playerCustomProperties.SetPlayerStateProperty(LocalSettings.playerState, (int)state);
            currentState = state;
            OnUpdateCurrentState(state);
        }
        [Command(requiresAuthority = false)]
        public void CmdUpdateCurrentPlayerStateOnNetwork(int state)
        {
            TPLog.Flow("PlayerCurrentState", "CmdUpdateCurrentPlayerStateOnNetwork on server for '" + gameObject.name + "' -> " + (PlayerState.STATE)state);
            "Pillu".Show();

            // Every online turn hand-off funnels through here, so this is where the server starts
            // keeping its own record of whose turn it is. connectionToClient is the OWNER of this
            // player object (server-side data), not the caller - deliberately, since this Cmd is
            // requiresAuthority = false and the caller is whoever chose to send it.
            if ((PlayerState.STATE)state == PlayerState.STATE.ExecutingTurn && TeenPattiNNetworkManager.instance != null)
                TeenPattiNNetworkManager.instance.ServerNoteTurnOwner(connectionToClient, gameObject.name);

            // A pack is the other half of that record: it is what decides who is still contesting
            // the pot, and the server settles the hand the moment one seat is left. Same reasoning
            // for reading the seat off the owning connection - this Cmd is requiresAuthority = false.
            if (MatchFlow.Enabled && TeenPattiNNetworkManager.instance != null
                && ((PlayerState.STATE)state == PlayerState.STATE.Packed || (PlayerState.STATE)state == PlayerState.STATE.OutOfTable)
                && !TeenPattiNNetworkManager.instance.FlowIsOut(connectionToClient))
                MatchFlow.Log(TeenPattiNNetworkManager.FlowGame, (PlayerState.STATE)state == PlayerState.STATE.Packed
                    ? $"{TeenPattiNNetworkManager.instance.FlowName(connectionToClient, gameObject.name)} packs (pot {TeenPattiNNetworkManager.instance.FlowPot()})"
                    : $"{TeenPattiNNetworkManager.instance.FlowName(connectionToClient, gameObject.name)} left the seat (stood up or turn timer ran out) — auto pack, pot {TeenPattiNNetworkManager.instance.FlowPot()}");
            if (((PlayerState.STATE)state == PlayerState.STATE.Packed || (PlayerState.STATE)state == PlayerState.STATE.OutOfTable)
                && TeenPattiNNetworkManager.instance != null)
                TeenPattiNNetworkManager.instance.ServerNotePacked(connectionToClient, gameObject.name);

            RpcUpdateCurrentPlayerStateOnNetwork(state);
        }

        [ClientRpc]
        public void RpcUpdateCurrentPlayerStateOnNetwork(int state)
        {
            TPLog.Flow("PlayerCurrentState", "RpcUpdateCurrentPlayerStateOnNetwork received for '" + gameObject.name + "' -> " + (PlayerState.STATE)state);
            UpdateCurrentPlayerStateOnNetwork((PlayerState.STATE)state);
        }

        public void TriggerStateOutofGame()
        {
            if (playerInfo == null)
                playerInfo = GetComponent<PlayerInfo>();


            playerInfo.PlayerStateText.text = "Out Of Game";
            if (!MatchHandler.isOffline())
            {
                if (netIdentity.isOwned)
                {

                    TPLog.Flow("PlayerCurrentState", "This is MY player and I am out of this hand -> showing 'wait for next round' UI");
                    if (MatchHandler.IsTeenPatti())
                    {
                        BigInteger cash = TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.GetTableCollectedCash(LocalSettings.TableCashKey);
                        Pot.instance.SetCashText(cash.ToString());
                        Pot.instance.PotPanel.SetActive(true);
                        playerInfo.ShowBtn.SetActive(false);

                        playerInfo.SeenIndicator.SetActive(false);
                        playerInfo.BlindIndicator.SetActive(false);
                    }




                    if (RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.ShowingResults)
                    {
                        TPLog.Flow("PlayerCurrentState", "Room is " + RoomStateManager.Instance.CurrentRoomState + " -> hiding the start-wait text");
                        GameStartManager.Instance.GameStarWaitTextGameObject.SetActive(false);
                    }

                    PlayerStateManager.Instance.Amountobject.SetActive(false);
                    "Show hoga bottom object false".Show();

                    PlayerStateManager.Instance.taptoSitHere.SetActive(false);
                    PlayerStateManager.Instance.waitForNextRound.SetActive(true);





                }
                else
                {
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' (the other player) is out of this hand");
                }
            }
            else
            {
                playerInfo.ShowBtn.SetActive(false);

                playerInfo.SeenIndicator.SetActive(false);
                playerInfo.BlindIndicator.SetActive(false);
                if (this.gameObject.name != LocalSettings.AI_Name)
                {

                    PlayerStateManager.Instance.Amountobject.SetActive(false);
                    "Show hoga bottom object false".Show();

                    PlayerStateManager.Instance.taptoSitHere.SetActive(false);
                    PlayerStateManager.Instance.waitForNextRound.SetActive(true);

                }
                if (RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.ShowingResults)
                    GameStartManager.Instance.GameStarWaitTextGameObject.SetActive(false);
            }



            //GameManager gm = GameManager.Instance;
            //for (int i = 0; i < gm.playersList.Count; i++)
            //{
            //    if (gm.playersList[i].GetComponent<PlayerCurrentState>().currentState != PlayerState.STATE.OutOfGame)
            //    {
            //        PlayerStateManager.Instance.PlayingList.Add(gm.playersList[i]);
            //    }
            //}
            if (MatchHandler.IsTeenPatti())
            {
                TPLog.Flow("PlayerCurrentState", "Refreshing everyone's blind/seen indicators after an OutOfGame change");
                PlayerStateManager.Instance.GetPlayerCardStatus();
                Debug.Log("-------- Out Of Game State");
            }

        }





        public void TriggerStateWatching()
        {

            if (playerInfo.IsSideShow && netIdentity.isOwned)
            {
                TPLog.Flow("PlayerCurrentState", "I lost a side-show -> opening the other player's cards for me");
                int nextPlayerInt = PlayerStateManager.Instance.SideShowPrev();

                PlayerStateManager.Instance.PlayingList[nextPlayerInt].ShowCardsFromBlind();
            }
            playerInfo.PlayerStateText.text = "Watching";
            Debug.Log("-------- Watching State");
            //Play 
        }


        private bool AI_Turn;



        public void TriggerAbleToJoin()
        {
            if (playerInfo == null)
                playerInfo = GetComponent<PlayerInfo>();

            if (MatchHandler.isOffline() && this.gameObject.name == LocalSettings.AI_Name)
            {
                AI_Turn = true;
            }



            if (!PlayerStateManager.Instance)
            {
                TPLog.Warn("PlayerCurrentState", "AbleToJoin skipped - PlayerStateManager is missing in this scene");
                return;
            }


            TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' is ready for the next hand -> wait/sit UI hidden");
            PlayerStateManager.Instance.waitForNextRound.SetActive(false);
            PlayerStateManager.Instance.taptoSitHere.SetActive(false);

            playerInfo.FillerImage.gameObject.SetActive(false);
            playerInfo.PlayerStateText.text = "Able To Join";



        }

        public void TriggerWaitingForTurn()
        {
            //if (netIdentity.isOwned)
            //{
            //    if (uiManager.ActionTable.activeSelf)
            //    {
            //        Debug.LogError("Action panel trouble  1 ----------------------------------------");
            //        uiManager.ActionTable.SetActive(false);
            //    }
            //}
            if (MatchHandler.isOffline())
            {
                if (playerInfo.gameObject.name == LocalSettings.AI_Name)
                {
                    if (AI_Turn)
                    {
                        float outCome = Random.value;


                        if (outCome <= 0.12f && AI_Turn)
                        {
                            AI_Turn = false;
                            playerInfo.ShowBtn.SetActive(false);
                            playerInfo.ShowCardsFromBlind();
                        }
                    }

                }
            }



            playerInfo.FillerImage.gameObject.SetActive(false);
            playerInfo.FillerImage.fillAmount = 0;
            Debug.Log("-------- Waiting For Turn State");
            playerInfo.PlayerStateText.text = "Waiting For Turn";

        }


        public void TriggerStateExecutingTurn()
        {

            if (!playerInfo.IsSideShow)
            {
                TPLog.Flow("PlayerCurrentState", "No side-show running -> normal turn starts for '" + gameObject.name + "'");
                StartCoroutine(MethodOfExecutingTurn());
                if (netIdentity.isOwned)
                    Debug.Log("-------- Executing Turn State");

            }
            else
            {
                TPLog.Flow("PlayerCurrentState", "A side-show is running -> turn for '" + gameObject.name + "' waits until it is finished");
                StartCoroutine(waitSideShowoff());
            }



        }
        IEnumerator MethodOfExecutingTurn()
        {
            if (PlayerStateManager.Instance.PlayingList.Count <= 1)
            {
                TPLog.Warn("PlayerCurrentState", "Turn starting with only " + PlayerStateManager.Instance.PlayingList.Count + " player(s) in the hand");
                yield return null;
            }



            if (!MatchHandler.isOffline())
            {
                if (netIdentity.isOwned && (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn))
                {


                    if (!uiManager.ActionTable.activeSelf)
                    {
                        TPLog.Flow("PlayerCurrentState", "MY TURN -> action buttons opened (vibrate, show/side-show, bet limits)");
                        LocalSettings.Vibrate();
                        uiManager.ActionTable.SetActive(true);
                        PlayerStateManager.Instance.SideShowAndShowbtn();
                        Game_Play.Instance.MyTurnBetAmountLimit();
                        //Debug.LogError("-------- Executing Turn State");
                    }
                    else
                    {
                        TPLog.Flow("PlayerCurrentState", "MY TURN -> action buttons were already open");
                    }


                }
                else
                {

                    TPLog.Flow("PlayerCurrentState", "Turn belongs to '" + gameObject.name + "' (mine=" + netIdentity.isOwned + ", room=" + RoomStateManager.Instance.CurrentRoomState + ") -> my action buttons stay closed");
                    if (uiManager.ActionTable.activeSelf)
                    {
                        uiManager.ActionTable.SetActive(false);
                    }


                }
            }
            else
            {
                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
                {
                    if (this.gameObject.name != LocalSettings.AI_Name)
                    {
                        if (!uiManager.ActionTable.activeSelf)
                        {
                            LocalSettings.Vibrate();
                            uiManager.ActionTable.SetActive(true);
                            PlayerStateManager.Instance.SideShowAndShowbtn();
                            Game_Play.Instance.MyTurnBetAmountLimit();
                            //Debug.LogError("-------- Executing Turn State");
                        }
                        else
                        {
                            if (uiManager.ActionTable.activeSelf)
                            {
                                uiManager.ActionTable.SetActive(false);
                            }
                        }
                    }
                    else
                    {

                        StartCoroutine(waitforAIstate());
                    }

                    playerInfo.FillerImage.fillAmount = 1;
                }
            }

            playerInfo.FillerImage.gameObject.SetActive(true);
            PlayerTurnManager.Instance.alarmTime = LocalSettings.RemainingTikTimer;
            playerInfo.PlayerStateText.text = "Executing Turn";
            TPLog.Flow("PlayerCurrentState", "Turn timer (re)started on the server for '" + gameObject.name + "'");
            NetworkGameManager.Instance.CmdBeginTurn();

        }


        IEnumerator waitforAIstate()
        {
            float waitTime = Random.value;
            float delay = waitTime < 0.05f ? Random.Range(0.5f, 18f) : Random.Range(0.5f, 8f);




            yield return new WaitForSeconds(delay);

            if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
            {
                if (currentState == PlayerState.STATE.ExecutingTurn)
                {
                    float outcome = Random.value;
                    Debug.Log("value of OutCome " + outcome);
                    //if(outcome < 0.8f)
                    //{
                    //    LocalSettings.AI_StandUP = true;
                    //    playerInfo.StandUp();
                    //    SitHere.Instance.SetThisForAIPositionByPlayer();
                    //}
                    //else


                    if (outcome < 0.025f)
                    {

                        UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                    }
                    else if (outcome < 0.06f)
                    {
                        Game_Play.Instance.AIClickShowBtn();
                    }
                    else
                    {

                        UpdateCurrentPlayerState(PlayerState.STATE.BetPlaced);
                    }
                }

            }
        }



        //public bool checkPockeBetPlaced()
        //{
        //    foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
        //    {
        //        if (item.isBetPlacedPocker == false)
        //            return false;
        //    }
        //    return true;
        //}
        IEnumerator waitSideShowoff()
        {
            while (playerInfo.IsSideShow)
            {

                TPLog.Change("PlayerCurrentState", "sideShowWait", "Turn of '" + gameObject.name + "' is on hold while the side-show is open");
                playerInfo.FillerImage.gameObject.SetActive(false);
                if (uiManager.ActionTable.activeSelf)
                {
                    uiManager.ActionTable.SetActive(false);
                }
                yield return new WaitUntil(() => playerInfo.IsSideShow);
                StartCoroutine(MethodOfExecutingTurn());
            }
        }

        bool checkisSideShow()
        {
            return playerInfo.IsSideShow;
        }

        public void TriggerStateBetPlaced()
        {
            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {


                Debug.Log("-------- Bet Placed State");
                playerInfo.PlayerStateText.text = "Bet Placed";
                BigInteger chalAmount = Pot.instance.CurrentChalAmount;
                if (playerInfo.IsSeen && Pot.instance.ChaalAmountLimit() > chalAmount)
                {
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' is SEEN -> his chaal is doubled");
                    chalAmount = Pot.instance.CurrentChalAmount * 2;
                }
                playerInfo.isFirstCurrentChaalBool = false;

                if (this.gameObject.name != LocalSettings.AI_Name)
                {
                    Debug.Log("Its A bet State");
                    if (LocalSettings.GetTotalChips() >= chalAmount)
                    {
                        TPLog.Flow("PlayerCurrentState", "Enough chips (" + LocalSettings.GetTotalChips() + ") for a chaal of " + chalAmount);
                        if (!MatchHandler.isOffline())
                        {
                            if (netIdentity.isOwned)
                            {
                                PlayerTurnManager.Instance.AddChaalAmount();
                            }
                            else
                            {
                                TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' bet is not mine -> only his own client deducts the chips");
                            }
                        }
                        else
                        {
                            PlayerTurnManager.Instance.AddChaalAmount();
                        }

                        PlayerTurnManager.Instance.GoToNextTurn();
                        playerInfo.FillerImage.gameObject.SetActive(true);
                        //Debug.LogError("Changing wait for turn ______________");
                        //if (playerInfo.IsSideShow)
                        //    UpdateCurrentPlayerState(PlayerState.STATE.SenderSideShow);
                        //else
                        if (!GameManager.Instance.isRunINBackGround)
                        {
                            TPLog.Flow("PlayerCurrentState", "Bet done -> '" + gameObject.name + "' goes back to WaitingForTurn");
                            UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
                        }
                        else
                        {
                            TPLog.Warn("PlayerCurrentState", "App is in the background -> state NOT moved to WaitingForTurn");
                        }
                        //Debug.LogError("Win sound is playing");
                    }
                    else
                    {
                        TPLog.Warn("PlayerCurrentState", "NOT enough chips (" + LocalSettings.GetTotalChips() + " < " + chalAmount + ") -> shop opens and the player is stood up");
                        if (!MatchHandler.isOffline())
                        {
                            if (netIdentity.isOwned)
                            {
                                //   UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                                uiManager.quickShop.SetActive(true);
                                Game_Play.Instance.StandUp();
                            }
                        }
                        else
                        {
                            uiManager.quickShop.SetActive(true);
                            Game_Play.Instance.StandUp();
                        }
                    }
                }
                else
                {
                    if (LocalSettings.AI_Amount >= chalAmount)
                    {

                        PlayerTurnManager.Instance.AddAIChaalAmount();

                        PlayerTurnManager.Instance.GoToNextTurn();
                        playerInfo.FillerImage.gameObject.SetActive(true);
                        //Debug.LogError("Changing wait for turn ______________");
                        //if (playerInfo.IsSideShow)
                        //    UpdateCurrentPlayerState(PlayerState.STATE.SenderSideShow);
                        //else
                        if (!GameManager.Instance.isRunINBackGround)
                        {
                            UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
                        }
                        //Debug.LogError("Win sound is playing");
                    }
                    else
                    {


                        //   UpdateCurrentPlayerState(PlayerState.STATE.Packed);

                        Game_Play.Instance.StandUp();

                    }
                }
                if (!MatchHandler.isOffline())
                {
                    if (playerInfo.this_photonView.isOwned)
                    {
                        TPLog.Flow("PlayerCurrentState", "My bet is finished -> I hand the turn to the next player");
                        SetNextPlayerToExecutingTurn();
                    }
                    else
                    {
                        TPLog.Flow("PlayerCurrentState", "Bet of '" + gameObject.name + "' finished -> his client will pass the turn on");
                    }
                }
                else
                {
                    uiManager.ActionTable.SetActive(false);
                    SetNextPlayerToExecutingTurn();
                }
            }


        }



        //int nextPlayerInt;
        void SetNextPlayerToExecutingTurn()
        {

            int nextPlayerInt = PlayerStateManager.Instance.PlayingList.IndexOf(playerInfo);
            nextPlayerInt++;
            if (nextPlayerInt >= PlayerStateManager.Instance.PlayingList.Count)
            {
                TPLog.Flow("PlayerCurrentState", "End of the playing list -> turn wraps back to the first player");
                nextPlayerInt = 0;
            }

            TPLog.Flow("PlayerCurrentState", "NEXT TURN -> '" + PlayerStateManager.Instance.PlayingList[nextPlayerInt].name + "' (index " + nextPlayerInt + " of " + PlayerStateManager.Instance.PlayingList.Count + ")");
            PlayerStateManager.Instance.PlayingList[nextPlayerInt].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
        }

        public void TriggerStatePacked()
        {
            Debug.Log("-------- Packed State");

            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {
                if (UIManager.Instance.ActionTable.activeSelf)
                {
                    TPLog.Flow("PlayerCurrentState", "Somebody packed -> closing the action buttons");
                    UIManager.Instance.ActionTable.SetActive(false);
                }

                playerInfo.FillerImage.gameObject.SetActive(false);
                playerAnim.PackAnim();
                playerInfo.PackedText.gameObject.SetActive(true);
                if (!MatchHandler.isOffline())
                    updaePlayerProperties(playerInfo);
                playerInfo.PlayerStateText.text = "Packed";
                playerInfo.PackedText.text = "PACKED";
                if (!playerInfo.IsSideShow)
                {
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' packed normally -> removing him from the playing list");
                    PlayerStateManager.Instance.UpdateListOnPlayerPack();
                }
                else
                {
                    TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' packed because he lost a side-show");
                }
                this.Delay(1f, () =>
                {

                    if (PlayerStateManager.Instance.PlayingList.Count > 1)
                    {

                        if (playerInfo.this_photonView.isOwned && !playerInfo.IsSideShow)
                        {
                            TPLog.Flow("PlayerCurrentState", "I packed -> handing the turn to the next player");
                            SetNextPlayerToExecutingTurn();
                        }


                        if (playerInfo.IsSideShow)
                        {
                            TPLog.Flow("PlayerCurrentState", "Side-show finished -> room goes back to GameIsPlaying");
                            RoomStateManager.Instance.UpdateCurrentStateOnShowBtn(RoomState.STATE.GameIsPlaying);
                        }
                        PlayerStateManager.Instance.UpdateListOnPlayerPack();
                    }
                    PlayerStateManager.Instance.PlayingList.Count.Show();
                    if (PlayerStateManager.Instance.PlayingList.Count <= 1)
                    {
                        TPLog.Flow("PlayerCurrentState", "Everyone else packed -> the last player wins automatically");
                        PlayerStateManager.Instance.RemainingPlayerWonGameAutomatic();


                    }




                    if (playerInfo.IsSideShow)
                    {
                        TPLog.Flow("PlayerCurrentState", "Clearing the side-show flag for '" + gameObject.name + "' on every client");
                        playerInfo.SideShowIndicatorAnim.SetActive(false);
                        playerInfo.GivesideShowAlertToAll(false);

                    }
                });

            }



        }
        public void TriggerStateSenderSideShow()
        {
            //if (netIdentity.isOwned)
            //{
            //    if (uiManager.ActionTable.activeSelf)
            //    {
            //        Debug.LogError("Action panel trouble  1 ----------------------------------------");
            //        uiManager.ActionTable.SetActive(false);
            //    }
            //}

            playerInfo.SideShowIndicatorAnim.SetActive(true);
            playerInfo.FillerImage.gameObject.SetActive(false);
            Debug.Log("-------- Waiting For Turn State");
            playerInfo.PlayerStateText.text = "Waiting For Turn";

        }
        public void TriggerStateRecieverSideShow()
        {
            Debug.Log("-------- Side Show State");

            if (netIdentity.isOwned)
            {
                int requestFromSideShowInt = PlayerStateManager.Instance.SideShowNext();
                TPLog.Flow("PlayerCurrentState", "SIDE-SHOW REQUEST for me from '" + PlayerStateManager.Instance.PlayingList[requestFromSideShowInt].name + "' -> panel opened, " + LocalSettings.SideShowCountDownTime + "s to answer");
                uiManager.playerName.text = PlayerStateManager.Instance.PlayingList[requestFromSideShowInt].name.ToString();
                GameStartManager.Instance.sideShowCount = LocalSettings.SideShowCountDownTime;
                LocalSettings.Vibrate();
                uiManager.sideShowPanel.SetActive(true);

            }
            else
            {
                TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' has to answer a side-show (not my screen)");
            }
            playerInfo.SideShowIndicatorAnim.SetActive(true);
            playerInfo.PlayerStateText.text = "Side Show";

        }

        public void TriggerStateOutOfTable()
        {

            TPLog.Flow("PlayerCurrentState", "'" + gameObject.name + "' left the seat -> his player object is hidden");
            gameObject.SetActive(false);
            if (!MatchHandler.isOffline())
            {
                if (netIdentity.isOwned)
                {
                    TPLog.Flow("PlayerCurrentState", "It was MY seat -> freeing seat " + GameManager.Instance.myLocalSeat + " and showing the 'sit here' buttons");
                    GameManager.Instance.position_availability[0].is_reserved = null;
                    if (MirrorNetwork.Instance.isMasterClient)
                        PositionsManager.Instance.ReleasePosition(GameManager.Instance.myLocalSeat);
                    GameManager.Instance.SitHereBtnStatus(true);
                }
                else
                {
                    TPLog.Flow("PlayerCurrentState", "The other player left his seat");
                }
            }
            else
            {
                GameManager.Instance.position_availability[0].is_reserved = null;
                PositionsManager.Instance.ReleasePosition(GameManager.Instance.myLocalSeat);
                GameManager.Instance.SitHereBtnStatus(true);
            }




            OutOfTableThings();

            playerInfo.PlayerStateText.text = "";
            if (netIdentity.isOwned)
            {

                TPLog.Flow("PlayerCurrentState", "Re-arranging the seat layout after I left my seat");
                PositionsManager.Instance.ReArrangePlayerSeatsAccordingToNetworkPositions();
            }


        }


        void OutOfTableThings()
        {
            playerInfo.FillerImage.gameObject.SetActive(false);

            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {
                if (!MatchHandler.isOffline())
                {
                    if (giveTurnToNext && netIdentity.isOwned)
                    {
                        TPLog.Flow("PlayerCurrentState", "I stood up on my own turn -> closing my action buttons");
                        if (UIManager.Instance.ActionTable.activeSelf)
                            UIManager.Instance.ActionTable.SetActive(false);
                    }
                    else
                    {
                        TPLog.Flow("PlayerCurrentState", "Stand-up handled without touching my action buttons (giveTurnToNext=" + giveTurnToNext + ", mine=" + netIdentity.isOwned + ")");
                    }
                }
                else
                {
                    if (UIManager.Instance.ActionTable.activeSelf)
                        UIManager.Instance.ActionTable.SetActive(false);
                }


                PlayerStateManager.Instance.UpdateListOnPlayerPack();
                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
                {
                    TPLog.Flow("PlayerCurrentState", "Player left the table mid-hand -> " + PlayerStateManager.Instance.PlayingList.Count + " players still in");
                    if (PlayerStateManager.Instance.PlayingList.Count > 1)
                    {

                        if (playerInfo.this_photonView.isOwned && !playerInfo.IsSideShow && giveTurnToNext)
                        {
                            TPLog.Flow("PlayerCurrentState", "Passing my turn on before I leave");
                            SetNextPlayerToExecutingTurn();
                        }
                    }

                    giveTurnToNext = true;
                }
                else
                {
                    TPLog.Flow("PlayerCurrentState", "Player left while the room was " + RoomStateManager.Instance.CurrentRoomState + " -> no turn to pass on");
                }
            }
            "Yahan aaya hai".Show();
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerCurrentState", "Running OutOfTheTableMethod cleanup after the stand-up");
                TurnManagerOfflineTeenPatti.Instance.OutOfTheTableMethod();
            }
            else
            {

                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
                    if (PlayerStateManager.Instance.PlayingList.Count <= 1)
                        PlayerStateManager.Instance.RemainingPlayerWonGameAutomatic();
            }


            playerInfo.SideShowIndicatorAnim.SetActive(false);

        }









        bool checkPlayerIsRunInBackGround()
        {
            for (int i = 0; i < PlayerStateManager.Instance.PlayingList.Count; i++)
            {
                if (PlayerStateManager.Instance.PlayingList[i].checkApplicationBackground)
                    return true;
            }
            return false;
        }

        bool checkPlayerBool()
        {
            foreach (var item in PlayerStateManager.Instance.PlayingList)
            {
                if (playerInfo.netIdentity.isOwned)
                    if (item.this_photonView.netId == playerInfo.this_photonView.netId)
                        return true;
            }
            return false;
        }

        void updaePlayerProperties(PlayerInfo playerInfo)
        {
            if (playerInfo.this_photonView.isOwned && RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
            {
                TPLog.Flow("PlayerCurrentState", "I packed during a live hand -> saving my hands/win statistics");
                if (!MatchHandler.isOffline())
                    UIManager.Instance.UpdateTheWinAmount(LocalSettings.totalcashWinLossKey, LocalSettings.TotalHandsKey, LocalSettings.WinHandsKey);
            }
        }
    }
}