using DG.Tweening;
using System.Net.NetworkInformation;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace TeenPattiGame
{
    public class GameResetManager : MonoBehaviour
    {
        public static GameResetManager Instance;
        GameManager gameManager;
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
        }

        public void ResetGameTeenPatti()
        {
            TPLog.Flow("GameResetManager", "==== TABLE RESET for the next hand ====");
            SetGameStartBooleans();
            ResetPlayerDummyAndOriginalCards();
            ResetSeenBlindStatesAndImages();
            ResetTurnManagerValues();
            ResetActionPanelAndPot();

            UpDateAllPlayersStates();

            RoomStateUpdate();
            AdjustSideShowButtonsOfAllPlayers();
            // SetTableCashReset();
            UIManager.Instance.isPlayerPlayedThisHand = false;
        }



        // MULTIPLAYER ONLY. Pure local screen cleanup for the start of a round: throw away the
        // cards still lying on the table from the finished round, put everybody back to blind,
        // and clear the packed / winner / bet leftovers. It touches no room state, no turn
        // counter, no countdown flags and sends nothing over the network - every client simply
        // cleans its own screen. Safe to run while the start countdown is running, unlike
        // ResetGameTeenPatti() which would also reset the countdown itself.
        public void ResetTableVisualsForNewRound()
        {
            if (gameManager == null)
            {
                TPLog.Warn("GameResetManager", "Round-start visual reset skipped - GameManager missing");
                return;
            }

            TPLog.Flow("GameResetManager", "Round start -> clearing the finished round from the screen (" + gameManager.playersList.Count + " players)");

            for (int i = 0; i < gameManager.playersList.Count; i++)
            {
                PlayerInfo plyrInfo = gameManager.playersList[i];
                if (plyrInfo == null)
                    continue;

                // face-down cards: hidden again, they get revealed one by one while dealing
                if (plyrInfo.PlayerDummyCardsToShowParent != null)
                {
                    Transform dummycardsParent = plyrInfo.PlayerDummyCardsToShowParent.transform;
                    dummycardsParent.gameObject.SetActive(true);
                    for (int c = 0; c < dummycardsParent.childCount && c < 4; c++)
                        dummycardsParent.GetChild(c).gameObject.SetActive(false);
                }

                // the real face-up cards of the finished round. Children 0-2 are the position
                // anchors, everything from 3 on is an instantiated card - destroy them all, so
                // nothing can pile up under this round's fresh cards.
                if (plyrInfo.PlayerOrignalCardsToShowParent != null)
                {
                    Transform orgcardsParent = plyrInfo.PlayerOrignalCardsToShowParent.transform;
                    orgcardsParent.gameObject.SetActive(false);
                    for (int c = orgcardsParent.childCount - 1; c >= 3; c--)
                        Destroy(orgcardsParent.GetChild(c).gameObject);
                }

                // back to blind, and last round's markers off
                plyrInfo.IsSeen = false;
                plyrInfo.MyChaalsPlayedCounter = 0;
                plyrInfo.ShowBtn.SetActive(false);
                plyrInfo.SeenIndicator.SetActive(false);
                plyrInfo.BlindIndicator.SetActive(false);
                plyrInfo.WinningIndicator.SetActive(false);
                plyrInfo.FillerImage.gameObject.SetActive(false);
                plyrInfo.PackedText.gameObject.SetActive(false);
                plyrInfo.myCurrentBetAmountAnim.SetActive(false);
                plyrInfo.SideShowIndicatorAnim.SetActive(false);
            }

            if (gameManager.SupportingCard)
                Destroy(gameManager.SupportingCard.gameObject);

            if (UIManager.Instance != null)
                UIManager.Instance.ChaalTypeText.text = "Blind";
        }

        void RoomStateUpdate()
        {
            if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying && PlayerStateManager.Instance.PlayingList.Count > 1)
            {
                TPLog.Flow("GameResetManager", "Reset skipped the room state - a hand is still running with " + PlayerStateManager.Instance.PlayingList.Count + " players");
                return;
            }
            if (GameStartManager.Instance._currentNumberOfPlayers < 1)
            {
                TPLog.Flow("GameResetManager", "Nobody at the table -> room state left as it is");
                return;
            }
            if (RoomStateManager.Instance != null)
            {
                if (GameStartManager.Instance._currentNumberOfPlayers < LocalSettings.GetMinPlayers())
                {
                    TPLog.Flow("GameResetManager", "After reset: only " + GameStartManager.Instance._currentNumberOfPlayers + " player(s) -> WaitingForPlayers");
                    RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.WaitingForPlayers);
                }
                else
                {
                    TPLog.Flow("GameResetManager", "After reset: " + GameStartManager.Instance._currentNumberOfPlayers + " players -> GameIsStarting");
                    RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsStarting);
                }
            }
        }



        void SetGameStartBooleans()
        {
            if (GameStartManager.Instance == null)
            {
                TPLog.Warn("GameResetManager", "SetGameStartBooleans skipped - GameStartManager is missing");
                return;
            }
            TPLog.Flow("GameResetManager", "Start flags cleared, countdown parked at " + LocalSettings.GameStartAfterReset + "s");
            GameStartManager gameStartManager = GameStartManager.Instance;
            gameStartManager.MinimumPlayerSatisfied = false;
            gameStartManager.GameIsGoingToStart = false;
            gameStartManager.IsGameStartingState = false;
            gameStartManager._1stCurrentChaalBool = true;
            gameStartManager.GameStarWaitTextGameObject.gameObject.SetActive(true);
            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
                gameStartManager.startWaitTime = LocalSettings.GameStartAfterReset;


        }

        void UpDateAllPlayersStates()
        {
            if (gameManager != null)
                for (int i = 0; i < gameManager.playersList.Count; i++)
                {
                    gameManager.playersList[i].getCurrentPlayerState().currentState.ToString().Show();
                    if (gameManager.playersList[i].getCurrentPlayerState().currentState != PlayerState.STATE.OutOfTable)
                    {
                        if (!MatchHandler.isOffline())
                        {
                            // MULTIPLAYER ONLY. This used to be master-only: the master reset
                            // EVERY player for the next round. isMasterClient is handed out once
                            // when the match is created and is never re-elected anywhere in the
                            // project, so the moment the creator disconnects nobody resets anybody
                            // and the finished round's state sticks around.
                            // Each client now resets only its own player - it owns that player, and
                            // it no longer needs the master to be present.
                            if (gameManager.playersList[i].isOwned)
                            {
                                TPLog.Flow("GameResetManager", "Resetting MY player '" + gameManager.playersList[i].name + "' -> AbleToJoin for the next hand");
                                gameManager.playersList[i].UpdatePlayerState(PlayerState.STATE.AbleToJoin);      //photon network
                            }
                            else
                            {
                                TPLog.Flow("GameResetManager", "'" + gameManager.playersList[i].name + "' is not mine -> his own client resets him");
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

        void UpDateAllPlayersStatesWingo()
        {
            for (int i = 0; i < gameManager.playersList.Count; i++)
            {
                if (gameManager.playersList[i].getCurrentPlayerState().currentState != PlayerState.STATE.OutOfTable)
                {
                    if (MirrorNetwork.Instance.isMasterClient)
                        gameManager.playersList[i].UpdatePlayerState(PlayerState.STATE.AbleToJoin);

                    //gameManager.playersList[i].gameObject.GetComponent<Button>().interactable = true;
                    gameManager.playersList[i].transform.DOScale(new UnityEngine.Vector3(1f, 1f, 1), 1);

                }
            }
        }
        void UpDateAllPlayersStatesDT()
        {
            for (int i = 0; i < gameManager.playersList.Count; i++)
            {
                if (gameManager.playersList[i].getCurrentPlayerState().currentState != PlayerState.STATE.OutOfTable)
                {
                    if (MirrorNetwork.Instance.isMasterClient)
                        gameManager.playersList[i].UpdatePlayerState(PlayerState.STATE.AbleToJoin);

                    //gameManager.playersList[i].gameObject.GetComponent<Button>().interactable = true;
                    gameManager.playersList[i].transform.DOScale(new UnityEngine.Vector3(1f, 1f, 1), 1);

                }
            }
        }



        void ResetPlayerDummyAndOriginalCards()
        {
            for (int i = 0; i < gameManager.playersList.Count; i++)
            {
                // Disable dummy cards
                if (gameManager.playersList[i].PlayerDummyCardsToShowParent == null)
                {
                    TPLog.Warn("GameResetManager", "Card reset stopped - '" + gameManager.playersList[i].name + "' has no dummy-card parent");
                    return;
                }
                Transform dummycardsParent = gameManager.playersList[i].PlayerDummyCardsToShowParent.transform;
                dummycardsParent.gameObject.SetActive(true);
                dummycardsParent.GetChild(0).gameObject.SetActive(false);
                dummycardsParent.GetChild(1).gameObject.SetActive(false);
                dummycardsParent.GetChild(2).gameObject.SetActive(false);
                dummycardsParent.GetChild(3).gameObject.SetActive(false);


                // Destroy original cards if exists                
                Transform orgcardsParent = gameManager.playersList[i].PlayerOrignalCardsToShowParent.transform;
                orgcardsParent.gameObject.SetActive(false);
                if (orgcardsParent.childCount > 5)
                    Destroy(orgcardsParent.GetChild(5).gameObject);
                if (orgcardsParent.childCount > 4)
                    Destroy(orgcardsParent.GetChild(4).gameObject);
                if (orgcardsParent.childCount > 3)
                    Destroy(orgcardsParent.GetChild(3).gameObject);

            }
            if (gameManager.SupportingCard)
                Destroy(gameManager.SupportingCard.gameObject);
        }

        void ResetSeenBlindStatesAndImages()
        {
            if (PlayerStateManager.Instance != null)
            {
                if (PlayerStateManager.Instance.PlayingList == null)
                {
                    TPLog.Warn("GameResetManager", "Seen/blind reset skipped - the playing list is null");
                    return;
                }
                TPLog.Flow("GameResetManager", "Clearing seen/blind, packed text, win indicator and chaal counters for every player");
                for (int i = 0; i < gameManager.playersList.Count; i++)
                {
                    PlayerInfo plyrInfo = gameManager.playersList[i];
                    if (plyrInfo != null)
                    {
                        plyrInfo.IsSeen = false;
                        plyrInfo.ShowBtn.SetActive(false);
                        plyrInfo.SeenIndicator.SetActive(false);
                        plyrInfo.BlindIndicator.SetActive(false);
                        plyrInfo.MyChaalsPlayedCounter = 0;
                        plyrInfo.WinningIndicator.SetActive(false);
                        plyrInfo.FillerImage.gameObject.SetActive(false);
                        plyrInfo.PackedText.gameObject.SetActive(false);
                        plyrInfo.myCurrentBetAmountAnim.SetActive(false);

                        plyrInfo.GivesideShowAlertToAll(false);
                        if (!MatchHandler.isOffline())
                        {
                            //if (PhotonNetwork.IsConnectedAndReady)
                            plyrInfo.playerCustomProperties.SetCustomBoolData("is_seen", false);      //photon removal
                        }

                    }
                }
            }
        }

        void ResetTurnManagerValues()
        {
            if (PlayerStateManager.Instance != null)
            {
                if (PlayerStateManager.Instance.PlayingList.Count > 0 && !MatchHandler.isOffline())
                {
                    TPLog.Flow("GameResetManager", "Asking the server to reset the turn counter");
                    // if (PhotonNetwork.IsConnectedAndReady)
                    // PlayerTurnManager.Instance.turnManager.ResetTurn();          //photon removal
                    NetworkGameManager.Instance.CmdResetTurn();

                }
                else
                {
                    TPLog.Flow("GameResetManager", "Turn counter not reset (players in hand = " + PlayerStateManager.Instance.PlayingList.Count + ")");
                }

            }
        }



        void ResetActionPanelAndPot()
        {
            if (Pot.instance == null)
            {
                TPLog.Warn("GameResetManager", "Pot reset skipped - Pot instance missing");
                return;
            }
            if (UIManager.Instance == null)
            {
                TPLog.Warn("GameResetManager", "Pot reset skipped - UIManager missing");
                return;
            }

            TPLog.Flow("GameResetManager", "Pot cleared, action buttons hidden, chaal type back to Blind");
            Pot.instance.ResetPot();

            UIManager.Instance.ActionTable.SetActive(false);
            BigInteger betAmount = Pot.instance.CurrentChalAmount;
            UIManager.Instance.UpDateCurrentChalAmountText(betAmount);

            UIManager.Instance.ChaalTypeText.text = "Blind";
            UIManager.Instance.sideShowBtn.interactable = false;
            UIManager.Instance.showBtn.interactable = false;
            staticVariables.isTeenPattiFinished = false;
            GameStartManager.Instance.GameIsGoingToStart = true;
            GameStartManager.Instance.startWaitTime = LocalSettings.GameStartAfterReset;
            //Constants_M.Log("Get total Chips"+LocalSettings.GetTotalChips());
            ConstantsData_M.CurrentBetSpawnAmount = 5;
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("GameResetManager", "Saving my balance " + LocalSettings.GetTotalChips() + " as the starting cash of the next hand");
                UIManager.Instance.myPlayerInfo.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
                UIManager.Instance.myPlayerInfo.playerCustomProperties.SetCustomBigIntegerData("CashInHand", LocalSettings.GetTotalChips());
            }
            if (UIManager.Instance.GetMyPlayerInfo() != null)
                if (UIManager.Instance.GetMyPlayerInfo().IsSeen)
                    betAmount = Pot.instance.CurrentChalAmount * 2;

        }


        void ResetActionPanelAndPotAB()
        {
            if (Pot.instance == null)
                return;
            if (UIManager.Instance == null)
                return;
            Pot.instance.ResetPotAB();


        }

        void AdjustSideShowButtonsOfAllPlayers()
        {
            UIManager.Instance.sideShowBtn.interactable = false;
            UIManager.Instance.showBtn.interactable = false;
        }

    }
}