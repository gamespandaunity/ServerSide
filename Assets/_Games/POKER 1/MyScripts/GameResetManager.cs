using DG.Tweening;
using Mirror;

//using Photon.Pun;
using POKER;
using System.Net.NetworkInformation;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace POKER
{
    public class GameResetManager : MonoBehaviour
    {
        public static POKER.GameResetManager Instance;
        POKER.GameManager gameManager;
        private void Awake()
        {
            //Constants_M.Log("bhaag ja");

            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
        }


        public void ResetGamePoker()
        {
            //Debug.LogError("Reseting Poker Game");
            SetGameStartBooleans();
            //ResetPlayerDummyAndOriginalCards();
            //ResetSeenBlindStatesAndImages();
            ResetTurnManagerValues();
            //ResetActionPanelAndPot();

            UpDateAllPlayersStates();

            RoomStateUpdate();
           POKER.PokerManager.Instance.ResetPokerGame();
            //AdjustSideShowButtonsOfAllPlayers();
            // SetTableCashReset();
            UIManager.Instance.isPlayerPlayedThisHand = false;
        }

        
 

        void RoomStateUpdate()
        {
            // Only the server should broadcast room state changes after a reset.
            // Without this guard the client sends CmdRiseEvent(GameIsStarting) to the server,
            // causing duplicate GameIsStarting events and multiple countdown restarts.
            if (!MatchHandler.isOffline() && !NetworkServer.active)
                return;

            if (AllScriptsManager.Instance.RoomStateManager.CurrentRoomState == RoomState.STATE.GameIsPlaying && AllScriptsManager.Instance.PlayerStateManager.PlayingList.Count > 1)
                return;
            // Also guard ABFirstTurn/ABSecondTurn — RoomStateUpdate() runs on the server
            // where CurrentRoomState is always up to date.
            if (AllScriptsManager.Instance.RoomStateManager.CurrentRoomState == RoomState.STATE.ABFirstTurn
                || AllScriptsManager.Instance.RoomStateManager.CurrentRoomState == RoomState.STATE.ABSecondTurn)
                return;
            if (AllScriptsManager.Instance.GameStartManager._currentNumberOfPlayers < 1)
                return;
            if (RoomStateManager.Instance != null)
            {
                if (AllScriptsManager.Instance.GameStartManager._currentNumberOfPlayers < LocalSettings.GetMinPlayers())
                    AllScriptsManager.Instance.RoomStateManager.UpdateCurrentRoomState(RoomState.STATE.WaitingForPlayers);
                else
                    AllScriptsManager.Instance.RoomStateManager.UpdateCurrentRoomState(RoomState.STATE.GameIsStarting);
            }
        }

        void SetGameStartBooleans()
        {
            if (GameStartManager.Instance == null)
                return;
            GameStartManager gameStartManager = GameStartManager.Instance;
            gameStartManager.MinimumPlayerSatisfied = false;
            gameStartManager.GameIsGoingToStart = false;
            gameStartManager.IsGameStartingState = false;
            gameStartManager._1stCurrentChaalBool = true;
            gameStartManager.GameStarWaitTextGameObject.gameObject.SetActive(true);
            gameStartManager._waitingForResultsStarted = false;
            staticVariables.isPokerFinished = false;
            // Reset ALL Poker reconnection SyncVars on the server so a fresh game
            // doesn't falsely trigger the reconnection path.
            if (NetworkServer.active && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.syncedPokerRoomState = 0;
                NetworkGameManager.Instance.syncedCommunityCards = "";
                NetworkGameManager.Instance.syncedDropCardsIndex = 0;

                // Same reason as the hole cards cleared in UpDateAllPlayersStates: leftover bet
                // totals would let a player who reconnects during the reset window rebuild the
                // previous hand's pot.
                if (GameManager.Instance != null)
                    foreach (PlayerInfo p in GameManager.Instance.playersList)
                        if (p != null)
                        {
                            p.syncedPokerHandBet = "0";
                            p.syncedPokerRoundBet = "0";
                        }
            }
         //   PhotonNetwork.LocalPlayer.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
           // PhotonNetwork.LocalPlayer.SetCustomBigIntegerData(LocalSettings.PokerTotalBuyInChips,0);
          //  PhotonNetwork.LocalPlayer.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
           // PhotonNetwork.LocalPlayer.SetCustomBigIntegerData("CashInHand", LocalSettings.GetTotalChips());
            // Teen Patti parity (its GameResetManager does the same two writes): publish my balance
            // as the starting cash of the next hand. Without this the synced "CashInHand" stays 0
            // and every handle-rounds payload comes out as a large negative number.
            if (!MatchHandler.isOffline() && UIManager.Instance != null && UIManager.Instance.GetMyPlayerInfo() != null)
            {
                UIManager.Instance.GetMyPlayerInfo().playerCustomProperties.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
                UIManager.Instance.GetMyPlayerInfo().playerCustomProperties.SetCustomBigIntegerData("CashInHand", LocalSettings.GetTotalChips());
            }
            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
                gameStartManager.startWaitTime = LocalSettings.PokerGameStartAfterReset;

        }

        void UpDateAllPlayersStates()
        {
            if (gameManager != null)
                for (int i = 0; i < gameManager.playersList.Count; i++)
                {
                    if (gameManager.playersList[i] == null) continue; // destroyed during disconnect
                    if (gameManager.playersList[i].getCurrentPlayerState().currentState != PlayerState.STATE.OutOfTable)
                    {
                        if (!MatchHandler.isOffline())
                        {
                            if (NetworkServer.active)
                            {
                                gameManager.playersList[i].UpdatePlayerState(PlayerState.STATE.AbleToJoin);
                                // Clear stale hole card data so reconnecting players during the
                                // reset window don't falsely trigger the reconnection path.
                                gameManager.playersList[i].playerCustomProperties.SetCustomData(LocalSettings.pokerHoleCard1ForPlayer, 0);
                                gameManager.playersList[i].playerCustomProperties.SetCustomData(LocalSettings.pokerHoleCard2ForPlayer, 0);
                            }
                        }
                        else
                        {

                            gameManager.playersList[i].UpdatePlayerState(PlayerState.STATE.AbleToJoin);
                        }

                        if (i < gameManager.playersList.Count)
                        {
                            if (gameManager.playersList[i] != null)
                            {
                                gameManager.playersList[i].gameObject.GetComponent<Button>().interactable = true;

                                gameManager.playersList[i].transform.DOScale(new UnityEngine.Vector3(1f, 1f, 1), 1);
                            }
                        }
                    }
                }
        }

        void ResetTurnManagerValues()
        {
            if (PlayerStateManager.Instance != null)
            {
                //if (PlayerStateManager.Instance.PlayingList.Count > 0)
                //    if (!MatchHandler.isOffline())
                //        if (PhotonNetwork.IsConnectedAndReady)
                //            PlayerTurnManager.Instance.turnManager.ResetTurn();
            }
        }

       

        

    }
}