using System.Collections;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeenPattiGame
{
    public class Game_Play : MonoBehaviour
    {
        UIManager uIManager;
        [ShowOnly] public bool StandUpFlag;
        [ShowOnly] public bool MasterClientStandUpFlag;



        // Start is called before the first frame update
        #region Creating Instance;
        private static Game_Play _instance;
        public static Game_Play Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<Game_Play>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
        }
        #endregion
        void Start()
        {
            uIManager = UIManager.Instance;
            if (!MatchHandler.isOffline())
            {
                // if (!PhotonNetwork.IsConnected) //photon removal
                {
                    //SceneManager.LoadScene("Home");
                }
            }



            StandUpFlag = true;
            MasterClientStandUpFlag = false;

        }

        #region 3 patti Game
        public void back()
        {
            //photon removal  PhotonNetwork.LoadLevel("Home");
        }

        public void LeaveRoomAndExitToLobby()
        {
            TPLog.Flow("Game_Play", "EXIT pressed -> leaving the table");
            //photon removal  StartCoroutine(ServerCall.GetRequest(ServerCall.End_casino_Url + APIManager.instance.winnerlossBetId));
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("Game_Play", "Online -> forcing every player out of the bet");
                // Say it OUT LOUD before the scene unloads. Without this the server only learns of
                // the exit when the transport notices the connection is gone, and it cannot tell a
                // quit from a temporary drop - so the opponent was made to sit through the
                // 30-second reconnect countdown for a player who had already walked away.
                if (TeenPattiNNetworkManager.instance != null)
                    TeenPattiNNetworkManager.instance.CmdTeenPattiPlayerQuit();
                //photon removal     if (PhotonNetwork.IsConnectedAndReady)
                RoomStateManager.Instance.LeaveBetForce();
            }
            else
            {
                SceneManager.LoadScene("Home");
            }
            uIManager.LoadingPanel.SetActive(true);
            //StartCoroutine(GoToLobby(LocalSettings.isSwitchRoom ? 0 : 1f));
        }

        AsyncOperation asyncOperation;

        public void OnroomLeftLoadScene()
        {
            // if (!LocalSettings.isSwitchRoom)
            //asyncOperation.allowSceneActivation = true;
            SceneManager.LoadScene("Home");
        }

        private IEnumerator LoadLevelAsync()
        {
            yield return new WaitForSeconds(0.5f);
            asyncOperation = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            asyncOperation.allowSceneActivation = false;
            while (!asyncOperation.isDone)
                yield return null;
        }


        IEnumerator GoToLobby(float TimeDelay)
        {

            yield return new WaitForSecondsRealtime(TimeDelay);

            //photon removal   PhotonNetwork.LoadLevel(0);


        }

        public void EndMyTurn()
        {
            TPLog.Flow("Game_Play", "END MY TURN -> chaal number " + (uIManager.GetMyPlayerInfo().MyChaalsPlayedCounter + 1) + ", state goes to BetPlaced");
            uIManager.GetMyPlayerInfo().MyChaalsPlayedCounter++;
            uIManager.GetMyPlayerInfo().UpdatePlayerState(PlayerState.STATE.BetPlaced);

        }

        public void PackThisPlayer()
        {
            TPLog.Flow("Game_Play", "PACK pressed -> my state goes to Packed");
            UIManager.Instance.GetMyPlayerInfo().UpdatePlayerState(PlayerState.STATE.Packed);
        }

        public void OnBettingAmoundIncrease()
        {

            TPLog.Flow("Game_Play", "BET + pressed -> chaal amount " + Pot.instance.CurrentChalAmount + " becomes " + (Pot.instance.CurrentChalAmount * 2));
            Pot.instance.CurrentChalAmount *= 2;

            uIManager.GetMyPlayerInfo().CurrentChalAmountSendToAllPlayers(Pot.instance.CurrentChalAmount);
            uIManager.BetAmountDecreaseBtn.interactable = true;
            uIManager.BetAmountIncreaseBtn.interactable = false;
        }
        public void OnBettingAmoundDecrease()
        {
            TPLog.Flow("Game_Play", "BET - pressed -> chaal amount " + Pot.instance.CurrentChalAmount + " becomes " + (Pot.instance.CurrentChalAmount / 2));
            //if (Pot.instance.CurrentChalAmount > 30)
            Pot.instance.CurrentChalAmount /= 2;
            uIManager.GetMyPlayerInfo().CurrentChalAmountSendToAllPlayers(Pot.instance.CurrentChalAmount);
            uIManager.BetAmountDecreaseBtn.interactable = false;
            uIManager.BetAmountIncreaseBtn.interactable = true;
        }

        public void MyTurnBetAmountLimit()
        {

            uIManager.BetAmountDecreaseBtn.interactable = false;
            uIManager.BetAmountIncreaseBtn.interactable = true;
            BigInteger betAmount = Pot.instance.CurrentChalAmount;

            //if (Pot.instance.ChaalAmountLimit() <= Pot.instance.CurrentChalAmount)
            //  Debug.LogError(Pot.instance.ChaalAmountLimit() + "    " + betAmount);

            if (uIManager.GetMyPlayerInfo().IsSeen && Pot.instance.ChaalAmountLimit() > Pot.instance.CurrentChalAmount)
            {
                TPLog.Flow("Game_Play", "I am SEEN and under the limit -> my bet shown doubled");
                betAmount = Pot.instance.CurrentChalAmount * 2;
            }



            // Debug.LogError("Here is... " + Pot.instance.ChaalAmountLimit() + " " + betAmount + " " + Pot.instance.CurrentChalAmount);
            if (Pot.instance.ChaalAmountLimit() <= betAmount)
            {
                TPLog.Flow("Game_Play", "Table chaal limit (" + Pot.instance.ChaalAmountLimit() + ") reached -> BET + disabled");
                uIManager.BetAmountIncreaseBtn.interactable = false;
            }
            else
            {

                if (Pot.instance.ChaalAmountLimit() / 2 <= betAmount && !uIManager.GetMyPlayerInfo().IsSeen)
                {
                    TPLog.Flow("Game_Play", "Blind player limit is half the table limit -> BET + disabled");
                    uIManager.BetAmountIncreaseBtn.interactable = false;
                }
                else
                {
                    TPLog.Flow("Game_Play", "Bet can still be raised (" + betAmount + " / limit " + Pot.instance.ChaalAmountLimit() + ")");
                }
            }

            uIManager.UpDateCurrentChalAmountText(betAmount);
        }
        public void GiveTipToGirl()
        {
            if (LocalSettings.GetTotalChips() <= LocalSettings.MinBetAmount)
            {
                TPLog.Warn("Game_Play", "Tip refused - my balance " + LocalSettings.GetTotalChips() + " is below the table minimum " + LocalSettings.MinBetAmount);
                uIManager.InfoTxt.text = uIManager.GetMyPlayerInfo().player_name.text + "  Your Table Minimum bet limited is " + LocalSettings.MinBetAmount;
                uIManager.InfoObj.SetActive(true);
                return;
            }
            int dialogueNumber = Random.Range(0, uIManager.GetMyPlayerInfo().tipsDialouges.Count);
            if (uIManager.GetMyPlayerCurrentState().currentState != PlayerState.STATE.OutOfTable)
            {
                TPLog.Flow("Game_Play", "TIP sent to everyone (dialogue " + dialogueNumber + ")");
                UIManager.Instance.GetMyPlayerInfo().ForAllShowTipToGirl(dialogueNumber);
            }
            else
            {
                TPLog.Flow("Game_Play", "I am not seated -> shop opens instead of the tip");
                uIManager.quickShop.SetActive(true);
            }

        }
        public void ShowWinner()
        {
            TPLog.Flow("Game_Play", "ShowWinner -> ending my turn and comparing the cards");
            EndMyTurn();
            GameResultsManager.Instance.CheckWinnerOfThisGame();

            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ShowingResults);
            Debug.Log("Here is your error 1......." + RoomStateManager.Instance.CurrentRoomState);
        }

        public void ShowInfo(string message, float timeDelay)
        {
            uIManager.InfoObj.SetActive(false);
            PerformFunction perf = uIManager.InfoObj.GetComponent<PerformFunction>();
            perf.TimeDelay = timeDelay;
            uIManager.InfoTxt.text = message;
            uIManager.InfoObj.SetActive(true);

        }

        public void OnClickSideShowBtn()
        {
            TPLog.Flow("Game_Play", "SIDE-SHOW pressed -> telling everyone a side-show has started");
            uIManager.GetMyPlayerInfo().GivesideShowAlertToAll(true);
            uIManager.ActionTable.SetActive(false);

            EndMyTurn();
            // check if all players played
            if (Pot.instance.potSize >= Pot.instance.PotLimit)
            {
                TPLog.Flow("Game_Play", "Pot limit reached (" + Pot.instance.potSize + " >= " + Pot.instance.PotLimit + ") -> straight to the result instead of the side-show");
                RoomStateManager.Instance.UpdateCurrentState(RoomState.STATE.WaitingForResults, LocalSettings.textStringOfPotLimitReached);
            }
            else
            {

                int prevPlayerInt = PlayerStateManager.Instance.SideShowPrev();
                TPLog.Flow("Game_Play", "Side-show request sent to '" + PlayerStateManager.Instance.PlayingList[prevPlayerInt].name + "'");
                PlayerStateManager.Instance.PlayingList[prevPlayerInt].getCurrentPlayerState().UpdateCurrentPlayerState(PlayerState.STATE.RecieverSideShow);
                UIManager.Instance.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.SenderSideShow);
            }

            //RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameSideShow);

            //uIManager.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.SenderSideShow);
        }

        public void OnClickAcceptSideShowBtn()
        {
            TPLog.Flow("Game_Play", "SIDE-SHOW ACCEPTED -> comparing the two hands");
            uIManager.sideShowPanel.SetActive(false);
            GameResultsManager.Instance.DeclareWinningSideShowPlayerOfTeenPatti();
            //uIManager.GetMyPlayerInfo().GivesideShowAlertToAll(false);
        }

        public void OnClickCancelSideShowBtn()
        {
            "Cancel side button".Show();
            TPLog.Flow("Game_Play", "SIDE-SHOW CANCELLED/REJECTED -> room back to GameIsPlaying");
            RoomStateManager.Instance.UpdateCurrentStateOnShowBtn(RoomState.STATE.GameIsPlaying);
            uIManager.sideShowPanel.SetActive(false);
            uIManager.GetMyPlayerInfo().GivesideShowAlertToAll(false);
            if (PlayerStateManager.Instance.PlayingList.Count == 2)
            {
                int a = PlayerStateManager.Instance.SideShowNext();
                if (PlayerStateManager.Instance.PlayingList[a].currentPlayerStateRef.currentState != PlayerState.STATE.ExecutingTurn)
                {
                    TPLog.Flow("Game_Play", "Head to head and the other player is not on turn -> the turn comes back to me");
                    uIManager.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
                }
                else
                {
                    TPLog.Flow("Game_Play", "'" + PlayerStateManager.Instance.PlayingList[a].name + "' already has the turn -> nothing changed");
                }
            }


            //int nextPlayerInt = PlayerStateManager.Instance.PlayingList.IndexOf(uIManager.GetMyPlayerInfo());
            //nextPlayerInt++;
            //if (nextPlayerInt >= PlayerStateManager.Instance.PlayingList.Count)
            //    nextPlayerInt = 1;
            //else
            //    nextPlayerInt++;
            //PlayerStateManager.Instance.PlayingList[nextPlayerInt].getCurrentState().UpdateCurrentState(PlayerState.STATE.ExecutingTurn);
        }

        public void OnClickShowBtn()
        {
            TPLog.Flow("Game_Play", "SHOW pressed -> paying the last chaal and going to the result");
            if (PlayerStateManager.Instance.PlayingList[PlayerStateManager.Instance.SideShowNext()].getCurrentPlayerState().currentState != PlayerState.STATE.ExecutingTurn)
            {
                TPLog.Flow("Game_Play", "The other player is not on turn -> taking the turn back for the show");
                uIManager.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
            }
            if (!MatchHandler.isOffline())
            {
                if (uIManager.GetMyPlayerInfo().this_photonView.isOwned)
                {
                    TPLog.Flow("Game_Play", "Deducting my show chaal");
                    PlayerTurnManager.Instance.AddChaalAmount();
                }
                else
                {
                    TPLog.Warn("Game_Play", "SHOW pressed but my PlayerInfo is not owned by me - no chips deducted");
                }
            }
            else
            {
                PlayerTurnManager.Instance.AddChaalAmount();
            }
            TPLog.Flow("Game_Play", "Room state -> WaitingForResults (show)");
            RoomStateManager.Instance.UpdateCurrentStateOnShowBtn(RoomState.STATE.WaitingForResults, LocalSettings.textStringOnShowCard);
            uIManager.ActionTable.SetActive(false);

        }

        #region For AIClickShowButton

        public void AIClickShowBtn()
        {
            if (uIManager.GetAIPlayerInfo().currentPlayerStateRef.currentState != PlayerState.STATE.ExecutingTurn)
            {
                return;
            }

            PlayerTurnManager.Instance.AddAIChaalAmount();

            RoomStateManager.Instance.UpdateCurrentStateOnShowBtn(RoomState.STATE.WaitingForResults, LocalSettings.textStringOnShowCard);
            uIManager.ActionTable.SetActive(false);

        }
        #endregion


        public void StandUp()
        {

            if (MatchHandler.isOffline())
            {
                if (LocalSettings.AI_StandUP)
                {
                    LocalSettings.Show_Dialogue(uIManager.dialogueBox, "You can't stand on this stage.");
                    return;
                }
            }

            TPLog.Flow("Game_Play", "STAND UP -> leaving my seat");
            uIManager.GetMyPlayerInfo().StandUp();
        }






        #endregion





    }
}