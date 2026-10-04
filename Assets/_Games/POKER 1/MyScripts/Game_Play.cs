//using Photon.Pun;
//using Photon.Realtime;
using POKER;
using System.Collections;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExtensions;

namespace POKER
{
    public class Game_Play : MonoBehaviour
    {
        UIManager uIManager;
        [ShowOnly] public bool secondTurnTurnAb;
        [ShowOnly] public bool Skip_Bet_TurnManager;
        [ShowOnly] public bool StandUpFlag;
        [ShowOnly] public bool MasterClientStandUpFlag;
        [ShowOnly] public bool secondSuperBahar;
        [ShowOnly] public bool isBetTrueForStandUpWLDTLW = false;


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
            // Debug.LogError("Check Bool......" + LocalSettings.isSwitchRoom);
            Skip_Bet_TurnManager = false;
            secondTurnTurnAb = false;
            StandUpFlag = true;
            MasterClientStandUpFlag = false;
            secondSuperBahar = false;
            //StartCoroutine(LoadLevelAsync());
        }

        void OnEnable()
        {
            MirrorNetwork.OnWinCall += AnnounceVictory;
        }

        void OnDisable()
        {
            MirrorNetwork.OnWinCall -= AnnounceVictory;
        }

        /// <summary>
        /// Called when opponent disconnects or 30-sec background timer expires.
        /// Removes opponent from playing lists and awards the round to remaining player.
        /// </summary>
        private void AnnounceVictory(string reason)
        {
            if (MatchHandler.isOffline()) return;

            // Don't trigger if game round is already resolved
            var state = RoomStateManager.Instance.CurrentRoomState;
            if (state == RoomState.STATE.WaitingForResults || state == RoomState.STATE.ShowingResults
                || state == RoomState.STATE.WaitingForPlayers)
                return;

            // Remove disconnected opponents from PlayingList, keeping only local player
            PlayerInfo myPlayer = UIManager.Instance.GetMyPlayerInfo();
            if (myPlayer == null) return;

            for (int i = PlayerStateManager.Instance.PlayingList.Count - 1; i >= 0; i--)
            {
                PlayerInfo p = PlayerStateManager.Instance.PlayingList[i];
                if (p == null || p != myPlayer)
                {
                    PlayerStateManager.Instance.PlayingList.RemoveAt(i);
                }
            }

            // Remaining player wins the round automatically (handles pot distribution)
            PlayerStateManager.Instance.RemainingPlayerWonGameAutomatic();
        }

        #region 3 patti Game
        //public void back()
        //{
        //    PhotonNetwork.LoadLevel("Home");
        //}

        public void LeaveRoomAndExitToLobby()
        {

            StartCoroutine(ServerConnection.GetApiRequest(ServerConnection.End_Round_casino_Url + ApiAndRoomManager._instance.winLoseChallengeId, withSettlementAuth: true));
            if (!MatchHandler.isOffline())
            {

                    StandUp();
                    MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                    if (NetworkGameManager.Instance)
                        NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                    this.Delay(1, () => Mirror.NetworkManager.singleton.StopClient());
                    SceneManager.LoadScene("Home");

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
            "Leave Room".Show("Leave Room");
            // if (!LocalSettings.isSwitchRoom)
            //asyncOperation.allowSceneActivation = true;
            //PhotonNetwork.LoadLevel("Home");
            SceneManager.LoadSceneAsync("Home");
        }

        private IEnumerator LoadLevelAsync()
        {
            yield return new WaitForSeconds(0.5f);
            asyncOperation = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            asyncOperation.allowSceneActivation = false;
            while (!asyncOperation.isDone)
                yield return null;
        }




        public void EndMyTurn()
        {
            uIManager.GetMyPlayerInfo().MyChaalsPlayedCounter++;
            uIManager.GetMyPlayerInfo().UpdatePlayerState(PlayerState.STATE.BetPlaced);
            if (!uIManager.GetMyPlayerInfo().IsSeen)
            {
                if (uIManager.GetMyPlayerInfo().MyChaalsPlayedCounter >= uIManager.TotalChals)
                {
                    uIManager.GetMyPlayerInfo().ShowCardsFromBlind();
                }
            }
        }

        public void PackThisPlayer()
        {
            UIManager.Instance.GetMyPlayerInfo().UpdatePlayerState(PlayerState.STATE.Packed);
        }

        public void OnBettingAmoundIncrease()
        {
            Pot.instance.CurrentChalAmount *= 2;
            uIManager.GetMyPlayerInfo().CurrentChalAmountSendToAllPlayers(Pot.instance.CurrentChalAmount);
            uIManager.BetAmountDecreaseBtn.interactable = true;
            uIManager.BetAmountIncreaseBtn.interactable = false;
        }
        public void OnBettingAmoundDecrease()
        {
            if (Pot.instance.CurrentChalAmount > 30)
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
            if (Pot.instance.ChaalAmountLimit() <= betAmount)
                uIManager.BetAmountIncreaseBtn.interactable = false;
            else
                if (uIManager.GetMyPlayerInfo().IsSeen)
                betAmount = Pot.instance.CurrentChalAmount * 2;

            uIManager.UpDateCurrentChalAmountText(betAmount);
        }
        public void GiveTipToGirl()
        {
            if (LocalSettings.GetTotalChips() <= LocalSettings.MinBetAmount)
            {
                uIManager.InfoTxt.text = uIManager.GetMyPlayerInfo().player_name.text + "  Your Table Minimum bet limited is " + LocalSettings.MinBetAmount;
                uIManager.InfoObj.SetActive(true);
                return;
            }
            int dialogueNumber = Random.Range(0, uIManager.GetMyPlayerInfo().tipsDialouges.Count);
            if (uIManager.GetMyPlayerCurrentState().currentState != PlayerState.STATE.OutOfTable)
                UIManager.Instance.GetMyPlayerInfo().ForAllShowTipToGirl(dialogueNumber);
            else
            {
                uIManager.quickShop.SetActive(true);
                ConstantsData_M.Log(PlayerPrefs.GetString("TotalChips") + "2");
            }

        }
        public void ShowWinner()
        {
            EndMyTurn();


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
            uIManager.GetMyPlayerInfo().GivesideShowAlertToAll(true);


            EndMyTurn();
            // check if all players played
            if (Pot.instance.potSize >= Pot.instance.PotLimit)
            {
                RoomStateManager.Instance.UpdateCurrentState(RoomState.STATE.WaitingForResults, LocalSettings.textStringOfPotLimitReached);
            }
            else
            {

                int prevPlayerInt = PlayerStateManager.Instance.SideShowPrev();
                PlayerStateManager.Instance.PlayingList[prevPlayerInt].getCurrentPlayerState().UpdateCurrentPlayerState(PlayerState.STATE.RecieverSideShow);
                UIManager.Instance.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.SenderSideShow);
            }

            //RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameSideShow);

            //uIManager.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.SenderSideShow);
        }



        public void OnClickCancelSideShowBtn()
        {
            RoomStateManager.Instance.UpdateCurrentStateOnShowBtn(RoomState.STATE.GameIsPlaying);
            uIManager.sideShowPanel.SetActive(false);
            uIManager.GetMyPlayerInfo().GivesideShowAlertToAll(false);
            if (PlayerStateManager.Instance.PlayingList.Count == 2)
            {
                int a = PlayerStateManager.Instance.SideShowNext();
                if (PlayerStateManager.Instance.PlayingList[a].currentPlayerStateRef.currentState != PlayerState.STATE.ExecutingTurn)
                    uIManager.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
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
            if (PlayerStateManager.Instance.PlayingList[PlayerStateManager.Instance.SideShowNext()].getCurrentPlayerState().currentState != PlayerState.STATE.ExecutingTurn)
            {
                uIManager.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
            }
            if (uIManager.GetMyPlayerInfo().IsMine())
            {
                PlayerTurnManager.Instance.AddChaalAmount();
            }
            RoomStateManager.Instance.UpdateCurrentStateOnShowBtn(RoomState.STATE.WaitingForResults, LocalSettings.textStringOnShowCard);


        }


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


            uIManager.GetMyPlayerInfo().StandUp();
        }






        #endregion




        public void SwitchToRoom()
        {

            LeaveRoomAndExitToLobby();
            //PhotonNetwork.LeaveRoom();
            //uIManager.LoadingPanel.SetActive(true); // Leave the current room
            //PhotonNetwork.LoadLevel(0);
            //NetworkSettings.Instance.RoomEntranceProperty(MatchHandler.CurrentMatch, (int)LocalSettings.MinBetAmount);
            //PhotonNetwork.JoinOrCreateRoom(NetworkSettings.Instance.RoomName, new RoomOptions(), TypedLobby.Default);


        }


        public void OnClickGoldTranferBtn()
        {
            uIManager.CheckConditionGoldTranfer();
        }

        public void OnClickLevelPanel()
        {
            StartCoroutine(waitForPanelLevelPanelOff());
        }

        IEnumerator waitForPanelLevelPanelOff()
        {
            yield return new WaitForSeconds(1f);
            UIManager.Instance.levelUpPanel.SetActive(false);
        }

    }
}