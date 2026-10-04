using System;
using System.Collections;
using System.Numerics;
using Mirror;
using UnityEngine;
using UnityExtensions;

namespace TeenPattiGame
{
    public class PlayerTurnManager : NetworkBehaviour
    {
        //public PunTurnManager turnManager;
        PlayerStateManager playerStateManagerInstance;
        GameManager gameManagerInstance;

        public static PlayerTurnManager Instance;
        UIManager uIManager;
        bool IsShowingResults;

        bool isMyTurn = false;
        bool isMyTurnClickAb = true;
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            uIManager = UIManager.Instance;
        }
        void Start()
        {

            //this.turnManager = this.gameObject.AddComponent<PunTurnManager>();
            // this.turnManager.TurnManagerListener = this;
            //if (MatchHandler.isWingoLottary())
            //{
            //    gameObject.SetActive(false);
            //    return;
            //}
            gameManagerInstance = GameManager.Instance;
            playerStateManagerInstance = PlayerStateManager.Instance;
            //this.turnManager.TurnDuration = 8f;
        }

        public void GoToNextTurn()
        {
            if (isMyTurn)
            {
                Debug.Log("On Packed, new begin next turn");
                TPLog.Flow("PlayerTurnManager", "GoToNextTurn - it was my turn -> asking the server to begin a new turn");
                isMyTurn = false;
                // this.turnManager.BeginTurn();
                NetworkGameManager.Instance.CmdBeginTurn();
            }
            else
            {
                TPLog.Flow("PlayerTurnManager", "GoToNextTurn ignored - it was not my turn");
            }
        }

        public void AddChaalAmount()
        {
            //BigInteger chalAmount = Pot.instance.CurrentChalAmount;//Mohsin
            BigInteger chalAmount = ConstantsData_M.CurrentBetSpawnAmount;
            Debug.LogError(Pot.instance.ChaalAmountLimit() + " " + chalAmount + " " + Pot.instance.CurrentChalAmount);
            if (UIManager.Instance.GetMyPlayerInfo().IsSeen && Pot.instance.ChaalAmountLimit() > chalAmount)
            {
                TPLog.Flow("PlayerTurnManager", "I am SEEN and still under the chaal limit -> chaal amount recalculated");
                chalAmount = ConstantsData_M.CurrentBetSpawnAmount /** 2*/;
            }
            else
            {
                TPLog.Flow("PlayerTurnManager", "Blind chaal (or limit reached) -> chaal amount stays " + chalAmount);
            }
            TPLog.Flow("PlayerTurnManager", "MY BET: -" + chalAmount + " chips, pot before = " + Pot.instance.potSize);
            gameManagerInstance.PlayerTotalChipsUpdate(-chalAmount);

            Debug.LogError("Add Chaal Amount" + chalAmount);
            UIManager.Instance.TotalBetPlacedAmount += chalAmount;
            Debug.LogError("TotatlbetPlaceAMount   " + UIManager.Instance.TotalBetPlacedAmount);
            UIManager.Instance.GetMyPlayerInfo().AddToPot(chalAmount);

            if (!uIManager.GetMyPlayerInfo().IsSeen)
            {
                //  Debug.LogError("CHeck my Chall " + uIManager.GetMyPlayerInfo().MyChaalsPlayedCounter + " " + UIManager.Instance.TotalChals);
                if (uIManager.GetMyPlayerInfo().MyChaalsPlayedCounter >= uIManager.TotalChals)
                {
                    TPLog.Flow("PlayerTurnManager", "Blind chaal limit reached (" + uIManager.GetMyPlayerInfo().MyChaalsPlayedCounter + "/" + uIManager.TotalChals + ") - auto show is disabled in code");
                    // uIManager.GetMyPlayerInfo().ShowCardsFromBlind();
                }
            }
        }




        public void AddAIChaalAmount()
        {
            //Constants_M.Log("AI Chaal");
            BigInteger chalAmount = Pot.instance.CurrentChalAmount;
            if (UIManager.Instance.GetAIPlayerInfo().IsSeen)
                chalAmount = Pot.instance.CurrentChalAmount * 2;

            if (isIncreaseBet())
            {
                if (Pot.instance.ChaalAmountLimit() > chalAmount)
                {
                    Debug.Log(" Current ChalLimit " + Pot.instance.ChaalLimit / 2 + " current chal amount " + Pot.instance.CurrentChalAmount);
                    {
                        // chalAmount = Pot.instance.CurrentChalAmount /** 2*/;//Mohsin

                    }
                }
            }

            //Debug.LogError("Add Chaal Amount" + chalAmount);
            gameManagerInstance.AITotalChipsUpdate(-chalAmount);

            UIManager.Instance.TotalBetPlacedAmount += chalAmount;
            // Debug.LogError("TotatlbetPlaceAMount   " + UIManager.Instance.TotalBetPlacedAmount);
            UIManager.Instance.GetAIPlayerInfo().AddToPot(chalAmount);
        }





        bool isIncreaseBet()
        {
            int randomNumer = UnityEngine.Random.Range(0, 4);
            // Debug.LogError(randomNumer);
            return randomNumer >= 2 ? true : false;
        }

        PlayerInfo GetCurrentPlayingPlayer()
        {
            foreach (PlayerInfo plyrInfo in playerStateManagerInstance.PlayingList)
            {
                if (plyrInfo.getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                {
                    if (!plyrInfo.FillerImage.gameObject.activeInHierarchy)
                    {
                        TPLog.Flow("PlayerTurnManager", "TURN IS NOW WITH '" + plyrInfo.name + "' -> timer ring shown");
                        plyrInfo.FillerImage.gameObject.SetActive(true);
                        PlayerTurnManager.Instance.alarmTime = LocalSettings.RemainingTikTimer;
                    }
                    return plyrInfo;
                }
            }
            TPLog.Change("PlayerTurnManager", "noTurnPlayer", "No player is in ExecutingTurn right now");
            return null;
        }
        PlayerInfo currentPlayer;
        public int alarmTime = 6;
        float nextTeenPattiTimerDebugTime;

        // Update is called once per frame
        void Update()
        {
            if (!MatchHandler.isOffline() && Time.unscaledTime >= nextTeenPattiTimerDebugTime)
            {
                nextTeenPattiTimerDebugTime = Time.unscaledTime + 1f;

                NetworkGameManager networkGameManager = NetworkGameManager.Instance;
                RoomStateManager roomStateManager = RoomStateManager.Instance;
                int playingCount = playerStateManagerInstance != null
                    ? playerStateManagerInstance.PlayingList.Count
                    : -1;
                string roomState = roomStateManager != null
                    ? roomStateManager.CurrentRoomState.ToString()
                    : "null";

                Debug.LogWarning(
                    $"[TeenPattiTimer] Heartbeat. ServerActive={NetworkServer.active}, " +
                    $"TeenNetworkReady={TeenPattiNNetworkManager.instance != null}, " +
                    $"NetworkGameManagerReady={networkGameManager != null}, RoomState={roomState}, " +
                    $"PlayingList={playingCount}, NetworkPlayers={(networkGameManager != null ? networkGameManager.currentPlayerCount : -1)}, " +
                    $"Turn={(networkGameManager != null ? networkGameManager.Turn : -1)}, " +
                    $"Duration={(networkGameManager != null ? networkGameManager.TurnDuration : -1f):F2}, " +
                    $"Remaining={(networkGameManager != null ? networkGameManager.RemainingSecondsInTurn : -1f):F2}");
            }

            if (!MatchHandler.isOffline())
                if (TeenPattiNNetworkManager.instance == null)
                {
                    TPLog.Change("PlayerTurnManager", "updGate", "Turn loop paused - TeenPattiNNetworkManager does not exist yet");
                    return;
                }

            // Multiplayer turn time must stay frozen while an opponent is disconnected.
            // The server restores currentPlayerCount to 2 after a successful reconnect,
            // so the same remaining duration continues from where it was paused.
            if (!MatchHandler.isOffline()
                && (NetworkGameManager.Instance == null
                    || NetworkGameManager.Instance.currentPlayerCount != 2))
            {
                TPLog.Change("PlayerTurnManager", "updGate", "Turn loop FROZEN - need exactly 2 connected players (now "
                    + (NetworkGameManager.Instance != null ? NetworkGameManager.Instance.currentPlayerCount.ToString() : "no NetworkGameManager") + ")");
                return;
            }
            TPLog.Change("PlayerTurnManager", "updGate", "Turn loop running - 2 players connected");

            if (RoomStateManager.Instance && RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.GameIsPlaying && RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.ABFirstTurn)
            {
                TPLog.Change("PlayerTurnManager", "roomGate", "Room is " + RoomStateManager.Instance.CurrentRoomState + " (not playing) -> turn timer only counts down, no turn logic");
                // MULTIPLAYER ONLY: clamp at zero. Letting this run to -11s and beyond while
                // the table is idle makes the timer end fire continuously, and the first one
                // after the room becomes GameIsPlaying stands somebody up straight away.
                float remainingTurnTime = NetworkGameManager.Instance.TurnDuration - Time.deltaTime;
                if (remainingTurnTime < 0f)
                    remainingTurnTime = 0f;

                NetworkGameManager.Instance.TurnDuration = remainingTurnTime;
                NetworkGameManager.Instance.SetDuration(remainingTurnTime);
                return;

            }
            else
            {
                TPLog.Change("PlayerTurnManager", "roomGate", "Room is " + (RoomStateManager.Instance != null ? RoomStateManager.Instance.CurrentRoomState.ToString() : "null") + " -> turn logic active");
            }
            if (playerStateManagerInstance.PlayingList.Count <= 1)
            {
                TPLog.Change("PlayerTurnManager", "listGate", "Turn logic stopped - only " + playerStateManagerInstance.PlayingList.Count + " player(s) in the hand");
                return;
            }

            if (!MatchHandler.isOffline()
                && NetworkServer.active
                && RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying
                && NetworkGameManager.Instance != null
                && NetworkGameManager.Instance.currentPlayerCount == 2)
            {
                TPLog.Change("PlayerTurnManager", "srvTimer", "Server is the one counting the turn timer down");
                NetworkGameManager.Instance.SetTeenPattiTurnDuration(
                    NetworkGameManager.Instance.TurnDuration - Time.deltaTime);
            }

            if (!MatchHandler.isOffline())
            {
                // if (uIManager.DisconnectedPanel.activeSelf == true)         
                //     uIManager.DisconnectedPanel.SetActive(false);
                // else if (!uIManager.DisconnectedPanel.activeSelf != true)
                //     uIManager.DisconnectedPanel.SetActive(true);
            }

            if (!MatchHandler.isOffline())
            {
                if (NetworkGameManager.Instance.currentPlayerCount > 1)
                {
                    if (currentPlayer != null)
                    {
                        if (currentPlayer.getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                        {
                            TPLog.Change("PlayerTurnManager", "turnOwner", "Turn timer ticking for '" + currentPlayer.name + "'");
                            currentPlayer.FillerImage.fillAmount = NetworkGameManager.Instance.TurnDuration / 15;
                            PlayTikTimerSoundOnLessTime();
                        }
                        else
                        {
                            TPLog.Change("PlayerTurnManager", "turnOwner", "'" + currentPlayer.name + "' is no longer executing his turn (" + currentPlayer.getCurrentPlayerState().currentState + ") -> looking for the new turn owner");
                            currentPlayer = GetCurrentPlayingPlayer();
                        }
                    }
                    else
                    {
                        TPLog.Change("PlayerTurnManager", "turnOwner", "No turn owner cached -> looking for the player whose turn it is");
                        currentPlayer = GetCurrentPlayingPlayer();
                    }

                }
                else
                {
                    TPLog.Change("PlayerTurnManager", "turnOwner", "Only " + NetworkGameManager.Instance.currentPlayerCount + " player connected -> turn timer not running");
                }
            }
            else
            {
                this.DelayUntil(() => PlayerStateManager.Instance != null, () =>
            {

                if (PlayerStateManager.Instance.PlayingList.Count > 1)
                {
                    if (currentPlayer != null)
                    {
                        if (currentPlayer.getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                        {
                            currentPlayer.FillerImage.fillAmount -= Time.deltaTime * 0.06f;
                            PlayTikTimerSoundOnLessTime();
                            if (currentPlayer.FillerImage.fillAmount <= 0.05)
                            {
                                if (currentPlayer.gameObject.name != LocalSettings.AI_Name)
                                    Game_Play.Instance.StandUp();
                                else
                                {
                                    LocalSettings.AI_StandUP = true;
                                    currentPlayer.StandUp();
                                    SitHere.Instance.SetThisForAIPositionByPlayer();
                                }
                            }
                        }
                        else
                            currentPlayer = GetCurrentPlayingPlayer();
                    }
                    else
                        currentPlayer = GetCurrentPlayingPlayer();
                }

            });

            }





        }

        void PlayTikTimerSoundOnLessTime()
        {
            int remainingMin = (int)(NetworkGameManager.Instance.RemainingSecondsInTurn);

            if (currentPlayer.this_photonView.isOwned)
            {
                if (remainingMin < alarmTime && alarmTime > 0)
                {
                    if (remainingMin <= 0)
                    {
                        TPLog.Change("PlayerTurnManager", "tik", "My turn timer hit 0 - no more tick sounds");
                        return;
                    }
                    TPLog.Flow("PlayerTurnManager", "My turn: " + remainingMin + "s left -> tick sound");
                    // Debug.LogError("playing tik sound    Retime: " + remainingMin + "    Alarm time: " + alarmTime);
                    alarmTime--;
                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ClockTikSound, false);
                }
            }
        }
        void PlayTikTimerSoundOnLessTime(PlayerInfo info)
        {
            int remainingMin = (int)(NetworkGameManager.Instance.RemainingSecondsInTurn);

            if (info.this_photonView.isOwned)
            {
                if (remainingMin < alarmTime && alarmTime > 0)
                {
                    if (remainingMin <= 0)
                        return;
                    // Debug.LogError("playing tik sound    Retime: " + remainingMin + "    Alarm time: " + alarmTime);
                    alarmTime--;
                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ClockTikSound, false);
                }
            }
        }








        bool firstTurnEndsFlag;

        #region TurnManager Callbacks
        /// <summary>Called when a turn begins (Master Client set a new Turn number).</summary>
        public void OnTurnBegins(int turn)
        {
            Debug.Log("OnTurnBegins() turn: " + turn);
            TPLog.Flow("PlayerTurnManager", "TURN BEGINS - turn number " + turn);

            IsShowingResults = false;
        }

        public void OnTurnCompleted(int obj)
        {
            Debug.Log("OnTurnCompleted: " + obj);
            TPLog.Flow("PlayerTurnManager", "TURN COMPLETED - turn number " + obj);

            //this.UpdateScores();
            this.OnEndTurn();
        }

        // when a player moved (but did not finish the turn)


        public void OnTurnTimeEnds(int obj)
        {
            string roomState = RoomStateManager.Instance != null
                ? RoomStateManager.Instance.GetCurrentRoomState().ToString()
                : "null";
            string playerName = currentPlayer != null ? currentPlayer.gameObject.name : "null";
            bool isOwned = currentPlayer != null
                && currentPlayer.this_photonView != null
                && currentPlayer.this_photonView.isOwned;

            Debug.LogWarning(
                $"[TeenPattiTimer] Timeout RPC received. RpcTurn={obj}, SyncedTurn={NetworkGameManager.Instance.Turn}, " +
                $"RoomState={roomState}, CurrentPlayer={playerName}, IsOwned={isOwned}");

            if (RoomStateManager.Instance.GetCurrentRoomState() != RoomState.STATE.GameIsPlaying)
            {
                TPLog.Warn("PlayerTurnManager", "Turn timeout ignored - room is " + RoomStateManager.Instance.GetCurrentRoomState() + ", not GameIsPlaying");
                Debug.LogWarning("[TeenPattiTimer] StandUp skipped: room is not GameIsPlaying.");
                return;
            }

            if (currentPlayer == null)
            {
                TPLog.Warn("PlayerTurnManager", "Turn timeout ignored - I do not know whose turn it was");
                Debug.LogWarning("[TeenPattiTimer] StandUp skipped: currentPlayer is null.");
                return;
            }

            if (!isOwned)
            {
                TPLog.Flow("PlayerTurnManager", "Turn timeout was for '" + playerName + "' who is not mine -> his own client handles it");
                Debug.Log($"[TeenPattiTimer] StandUp ignored on non-owner client for {playerName}.");
                return;
            }

            // MULTIPLAYER ONLY. currentPlayer is only a cached reference and this is an rpc
            // that can land on any frame - after a reconnect the turn timer arrives stale and
            // fires every frame. Standing the player up on a timeout that does not belong to
            // his turn is what kicked the opponent off the table right after the cards were
            // dealt. Only a player who really is on turn may be stood up.
            PlayerState.STATE currentPlayerState = currentPlayer.getCurrentPlayerState() != null
                ? currentPlayer.getCurrentPlayerState().currentState
                : PlayerState.STATE.OutOfGame;

            if (currentPlayerState != PlayerState.STATE.ExecutingTurn)
            {
                TPLog.Warn("PlayerTurnManager", "Turn timeout ignored - '" + playerName + "' is " + currentPlayerState + ", it is not his turn (stale timer)");
                return;
            }

            TPLog.Warn("PlayerTurnManager", "MY turn timed out -> I will be stood up from the table");

            OnTurnCompleted(-1);

            if (NetworkGameManager.Instance.Turn <= 1)
            {
                TPLog.Warn("PlayerTurnManager", "Stand up cancelled - this is still turn " + NetworkGameManager.Instance.Turn + " (first turn is protected)");
                Debug.LogWarning(
                    $"[TeenPattiTimer] StandUp skipped: synced Turn is {NetworkGameManager.Instance.Turn}.");
                return;
            }

            Debug.LogWarning($"[TeenPattiTimer] Calling StandUp for owner player {playerName}.");
            Game_Play.Instance.StandUp();
        }
        //private void UpdateScores()
        //{

        //}

        #endregion

        #region Core Gameplay Methods


        /// <summary>Call to start the turn (only the Master Client will send this).</summary>
        public void StartTurn()
        {
            if (MirrorNetwork.Instance.isMasterClient)
            {
                TPLog.Flow("PlayerTurnManager", "Master client -> asking the server to begin the first turn");
                NetworkGameManager.Instance.CmdBeginTurn();
            }
            else
            {
                TPLog.Flow("PlayerTurnManager", "Not master client -> first turn will be started by the master");
            }
        }


        public void OnEndTurn()
        {
        }

        public void EndGame()
        {
            Debug.Log("EndGame");
        }



        #endregion
        public override void OnStartClient()
        {
            TPLog.Flow("PlayerTurnManager", "OnStartClient - joining the room in 2s");
            this.Delay(2, () =>
            {
                "On Start Clinet on PlayerTurnManager".Show();
                OnJoinedRoom();
            });

        }
        void RefreshUIViews()
        {
            uIManager.FillerImage.fillAmount = 1;
            //ConnectUiView.gameObject.SetActive(!PhotonNetwork.InRoom);
            //GameUiView.gameObject.SetActive(PhotonNetwork.InRoom);
        }
        // public override void OnLeftRoom()        
        // {
        //     //Debug.Log("You Left the Room");

        // }

        // public override void OnPlayerLeftRoom(Player otherPlayer)
        // {
        //     Debug.Log("Other Player named " + otherPlayer.NickName + " left this Room");     
        //     base.OnPlayerLeftRoom(otherPlayer);
        // }



        public void OnJoinedRoom()
        {
            RefreshUIViews();

            TPLog.Flow("PlayerTurnManager", "OnJoinedRoom - connected players = " + NetworkGameManager.Instance.currentPlayerCount + ", turn = " + NetworkGameManager.Instance.Turn);
            if (NetworkGameManager.Instance.currentPlayerCount == 2)
            {
                if (NetworkGameManager.Instance.Turn == 0)
                {
                    TPLog.Flow("PlayerTurnManager", "Both players in and no turn started yet -> starting turn 1");
                    // when the room has two players, start the first turn (later on, joining players won't trigger a turn)
                    this.StartTurn();
                }
                else
                {
                    TPLog.Flow("PlayerTurnManager", "Both players in but turn " + NetworkGameManager.Instance.Turn + " is already running -> not restarting");
                }
            }
            else
            {
                TPLog.Flow("PlayerTurnManager", "Waiting for the second player");
                Debug.Log("Waiting for another player");
            }
        }

        public void OnPlayerEnteredRoom()
        {
            TPLog.Flow("PlayerTurnManager", "OnPlayerEnteredRoom - connected players = " + NetworkGameManager.Instance.currentPlayerCount + ", turn = " + NetworkGameManager.Instance.Turn);
            if (NetworkGameManager.Instance.currentPlayerCount == 2)
            {
                if (NetworkGameManager.Instance.Turn == 0)
                {
                    TPLog.Flow("PlayerTurnManager", "Second player arrived and no turn yet -> starting turn 1");
                    // when the room has two players, start the first turn (later on, joining players won't trigger a turn)
                    this.StartTurn();
                }
                else
                {
                    TPLog.Flow("PlayerTurnManager", "Second player arrived mid-hand (turn " + NetworkGameManager.Instance.Turn + ") -> turn not restarted");
                }
            }
            else
            {
                TPLog.Flow("PlayerTurnManager", "Still not 2 players -> nothing started");
            }
        }



        void SetNextPlayerToExecutingTurnPokeRunInBackGround(PlayerInfo pInfo)
        {

            int nextPlayerInt = PlayerStateManager.Instance.PlayingList.IndexOf(pInfo);
            nextPlayerInt++;
            if (nextPlayerInt >= PlayerStateManager.Instance.PlayingList.Count)
            {
                TPLog.Flow("PlayerTurnManager", "Reached the end of the playing list -> wrapping back to the first player");
                nextPlayerInt = 0;
            }

            TPLog.Flow("PlayerTurnManager", "(background path) turn handed to '" + PlayerStateManager.Instance.PlayingList[nextPlayerInt].name + "'");
            PlayerStateManager.Instance.PlayingList[nextPlayerInt].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
        }



        [ShowOnly] public bool isturnTrue = false;
        void waitForNextTurnBoolOnceTime()
        {
            isturnTrue = false;
        }
    }
}
