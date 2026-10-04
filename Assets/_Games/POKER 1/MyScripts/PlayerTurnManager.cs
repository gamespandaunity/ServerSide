//using Photon.Pun;
//using Photon.Pun.UtilityScripts;
//using Photon.Realtime;
using Mirror;
using System;
using System.Collections;
using System.Numerics;
using UnityEngine;

namespace POKER
{
    public class PlayerTurnManager : NetworkBehaviour//, IPunTurnManagerCallbacks
    {
        //[ShowOnly] public PunTurnManager turnManager;
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
         //   this.turnManager.TurnManagerListener = this;
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
                isMyTurn = false;
                //this.turnManager.BeginTurn();
            }
        }

        public void AddChaalAmount()
        {
            BigInteger chalAmount = Pot.instance.CurrentChalAmount;
            if (UIManager.Instance.GetMyPlayerInfo().IsSeen)
                chalAmount = Pot.instance.CurrentChalAmount * 2;
            gameManagerInstance.PlayerTotalChipsUpdate(-chalAmount);

            UIManager.Instance.TotalBetPlacedAmount += chalAmount;
            // Debug.LogError("TotatlbetPlaceAMount   " + UIManager.Instance.TotalBetPlacedAmount);

        }


        PlayerInfo GetCurrentPlayingPlayer()
        {
            foreach (PlayerInfo plyrInfo in playerStateManagerInstance.PlayingList)
            {
                if (plyrInfo == null || plyrInfo.gameObject == null) continue;
                if (plyrInfo.getCurrentPlayerState() == null) continue;
                if (plyrInfo.getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                {
                    Debug.Log($"[GetCurrentPlayingPlayer] currentPlayer -> {plyrInfo.gameObject.name}, fillAmount={(plyrInfo.FillerImage != null ? plyrInfo.FillerImage.fillAmount.ToString() : "no-filler")}, fillerActive={(plyrInfo.FillerImage != null && plyrInfo.FillerImage.gameObject.activeInHierarchy)}");
                    if (plyrInfo.FillerImage == null) return plyrInfo;
                    if (!plyrInfo.FillerImage.gameObject.activeInHierarchy)
                    {
                        plyrInfo.FillerImage.gameObject.SetActive(true);

                        plyrInfo.FillerImage.fillAmount = 1;
                        alarmTime = LocalSettings.RemainingTikTimer;
                    }
                    return plyrInfo;
                }
            }
            return null;
        }
        PlayerInfo currentPlayer;
        public int alarmTime = 6;
        // Update is called once per frame
        // ── Round settle fallback ────────────────────────────────────────────────
        // The casino settle POST can only be sent by a player's own client - it goes out with that
        // player's bearer token, and a dedicated server has no logged-in user. Only the winner sends
        // it (PlayerInfo.RPCWinner), so when the winner's client is gone as the hand resolves,
        // nothing credits the round at all.
        //
        // The server waits for the winner's confirmation and, ONLY when the winner is provably no
        // longer connected, asks the surviving client to post the same payload. That condition is
        // what keeps this safe: while the winner is still connected its own POST is the only one
        // that can be in flight, so a lost confirmation can never turn into a second,
        // double-crediting request.
        const float settleFallbackGraceSeconds = 10f;
        string settledRoundId;
        Coroutine settleFallbackRoutine;

        [Command(requiresAuthority = false)]
        public void CmdPokerRoundSettled(string roundId)
        {
            settledRoundId = roundId;
            Debug.Log($"[RoundEnd] round {roundId} confirmed settled by the winner's own client.");
        }

        [Server]
        public void ServerWatchRoundSettle(string roundId, string winnerId, string firstAmount, string secondAmount)
        {
            if (string.IsNullOrEmpty(roundId))
                return;

            if (settleFallbackRoutine != null)
                StopCoroutine(settleFallbackRoutine);

            settleFallbackRoutine = StartCoroutine(SettleFallbackAfterGrace(roundId, winnerId, firstAmount, secondAmount));
        }

        IEnumerator SettleFallbackAfterGrace(string roundId, string winnerId, string firstAmount, string secondAmount)
        {
            yield return new WaitForSecondsRealtime(settleFallbackGraceSeconds);
            settleFallbackRoutine = null;

            if (settledRoundId == roundId)
                yield break;

            NetworkGameManager networkGameManager = NetworkGameManager.Instance;
            if (networkGameManager != null && networkGameManager.currentPlayerCount >= 2)
            {
                // Everybody is still here, so the winner's own POST is the only one that can be in
                // flight (slow network, or it failed and logged SETTLE FAILED). Sending another one
                // from a second client is exactly the double-credit case - never do it.
                Debug.LogWarning($"[RoundEnd] round {roundId} is still unconfirmed but both players are " +
                                 $"connected - leaving it to the winner's own client.");
                yield break;
            }

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn == null || !conn.isReady) continue;

                Debug.LogWarning($"[RoundEnd] round {roundId} was never settled and the winner ({winnerId}) is " +
                                 $"no longer connected - asking the remaining client to post it.");
                TargetPokerSettleFallback(conn, roundId, winnerId, firstAmount, secondAmount);
                yield break;
            }

            Debug.LogError($"[RoundEnd] round {roundId} was never settled and nobody is left to post it " +
                           $"(winner={winnerId}, first={firstAmount}, second={secondAmount}).");
        }

        [TargetRpc]
        void TargetPokerSettleFallback(NetworkConnection target, string roundId, string winnerId, string firstAmount, string secondAmount)
        {
            if (!BigInteger.TryParse(firstAmount, out BigInteger first)) first = 0;
            if (!BigInteger.TryParse(secondAmount, out BigInteger second)) second = 0;

            System.Collections.Generic.Dictionary<string, string> data = new System.Collections.Generic.Dictionary<string, string>
            {
                { "round_id", roundId },
                { "winner_id", winnerId },
                { "winner_amount", (first + second).ToString() },
                { "first_player_amount", first.ToString() },
                { "second_player_amount", second.ToString() },
            };

            Debug.LogWarning($"[RoundEnd] posting round {roundId} on behalf of the disconnected winner {winnerId}.");
            StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.End_Round_casino_Url, data,
                success => Debug.Log($"[RoundEnd] fallback settle for round {roundId} OK: {success}"),
                failure => Debug.LogError($"[RoundEnd] fallback settle FAILED for round {roundId}: {failure} - " +
                                          $"the backend may only accept the winner's own client for this call."), withSettlementAuth: true));
        }

        // ── Server-side turn backstop ────────────────────────────────────────────
        // The timer in Update() below is client-only, and only the turn holder itself auto-acts
        // (currentPlayer.IsMine()). So a client that is present but never acts - a frozen UI, a
        // client that came back from a reconnect without an action panel, a backgrounded app -
        // used to hang the hand forever for BOTH players, because nothing else can end that turn.
        // Teen Patti has a server-authoritative clock for exactly this (NetworkGameManager
        // .SetTeenPattiTurnDuration); Poker never got one. Run that clock here, on the dedicated
        // server, and fold the stalled player as a last resort.
        //
        // Deliberately slower than the client timer (PlayerTurnDurationPoker + grace, vs ~16s for
        // the client filler) so a healthy client always acts first and this never fires in a normal
        // hand. A hand where somebody is actually gone stays with the pause / waiting-panel flow -
        // this only guards the everybody-connected-but-nobody-acting case.
        const float serverTurnGraceSeconds = 8f;
        PlayerInfo serverTurnPlayer;
        float serverTurnElapsed;

        // Second half of the backstop: a live hand where NOBODY holds ExecutingTurn. A reconnect
        // used to leave exactly this behind - the reconnecting client rewrote its own state to
        // OutOfGame, the turn it was holding vanished, and the opponent kept waiting for a turn that
        // no longer existed, because PlayingList only drops Packed / OutOfTable players and never
        // OutOfGame ones. Fold whoever sits in the hand list without actually playing, which lets
        // the auto-win inside TriggerStatePacked finish the hand; if everyone looks valid, hand the
        // turn to the first of them as a last resort.
        const float serverNoTurnGraceSeconds = 10f;
        float serverNoTurnElapsed;

        void ServerNoTurnWatchdog()
        {
            System.Collections.Generic.List<PlayerInfo> playing = PlayerStateManager.Instance.PlayingList;
            if (playing.Count < 2)
            {
                serverNoTurnElapsed = 0f;
                return;
            }

            serverNoTurnElapsed += Time.deltaTime;
            if (serverNoTurnElapsed < serverNoTurnGraceSeconds)
                return;
            serverNoTurnElapsed = 0f;

            for (int i = playing.Count - 1; i >= 0; i--)
            {
                PlayerInfo p = playing[i];
                if (p == null || p.currentPlayerStateRef == null) continue;

                PlayerState.STATE state = p.currentPlayerStateRef.currentState;
                if (state == PlayerState.STATE.OutOfGame
                    || state == PlayerState.STATE.AbleToJoin
                    || state == PlayerState.STATE.OutOfTable)
                {
                    Debug.LogWarning($"[ServerTurnBackstop] Nobody holds the turn and {p.gameObject.name} " +
                                     $"is {state} while still in PlayingList - folding it so the hand can resolve.");
                    p.UpdatePlayerState(PlayerState.STATE.Packed);
                    return;
                }
            }

            PlayerInfo fallback = playing[0];
            if (fallback != null && fallback.currentPlayerStateRef != null)
            {
                Debug.LogWarning($"[ServerTurnBackstop] Nobody held the turn for {serverNoTurnGraceSeconds}s " +
                                 $"and every player looks valid - handing the turn to {fallback.gameObject.name}.");
                fallback.currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
            }
        }

        void ServerTurnBackstop()
        {
            if (MatchHandler.isOffline() || !MatchHandler.IsPoker())
                return;

            RoomState.STATE roomState = RoomStateManager.Instance.CurrentRoomState;
            if (roomState != RoomState.STATE.GameIsPlaying && roomState != RoomState.STATE.ABFirstTurn)
            {
                serverTurnPlayer = null;
                return;
            }

            NetworkGameManager networkGameManager = NetworkGameManager.Instance;
            if (networkGameManager == null || networkGameManager.IsPaused || networkGameManager.currentPlayerCount < 2)
            {
                serverTurnPlayer = null;
                return;
            }

            PlayerInfo turnPlayer = null;
            foreach (PlayerInfo p in PlayerStateManager.Instance.PlayingList)
            {
                if (p == null || p.currentPlayerStateRef == null) continue;
                if (p.currentPlayerStateRef.currentState == PlayerState.STATE.ExecutingTurn)
                {
                    turnPlayer = p;
                    break;
                }
            }

            if (turnPlayer == null)
            {
                serverTurnPlayer = null;
                ServerNoTurnWatchdog();
                return;
            }

            serverNoTurnElapsed = 0f;

            if (turnPlayer != serverTurnPlayer)
            {
                serverTurnPlayer = turnPlayer;
                serverTurnElapsed = 0f;
                return;
            }

            serverTurnElapsed += Time.deltaTime;
            if (serverTurnElapsed < LocalSettings.PlayerTurnDurationPoker + serverTurnGraceSeconds)
                return;

            PlayerInfo stalled = serverTurnPlayer;
            serverTurnPlayer = null;
            serverTurnElapsed = 0f;

            Debug.LogWarning($"[ServerTurnBackstop] {stalled.gameObject.name} held the turn for " +
                             $"{LocalSettings.PlayerTurnDurationPoker + serverTurnGraceSeconds}s without acting - " +
                             $"folding server-side so the hand can continue.");

            // Work out the next player BEFORE folding: TriggerStatePacked removes the folding player
            // from PlayingList, and it only passes the turn on that player's own client (IsMine),
            // which a dedicated server never is. With exactly 2 players the fold ends the hand
            // outright through the auto-win path inside TriggerStatePacked, so no turn is passed.
            PlayerInfo next = null;
            System.Collections.Generic.List<PlayerInfo> playing = PlayerStateManager.Instance.PlayingList;
            if (playing.Count > 2)
            {
                int index = playing.IndexOf(stalled);
                if (index >= 0)
                    next = playing[(index + 1) % playing.Count];
            }

            // Same path a real fold takes (PokerActionPanel.OnFolding), and on the server
            // UpdateCurrentPlayerState writes the authoritative property and broadcasts the ClientRpc.
            stalled.UpdatePlayerState(PlayerState.STATE.Packed);

            if (next != null && next.currentPlayerStateRef != null && playing.Contains(next))
                next.currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
        }

        void Update()
        {
            // Dedicated server has no UI — skip all turn timer/filler logic
            if (NetworkServer.active && !NetworkClient.active)
            {
                ServerTurnBackstop();
                return;
            }

            if (RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.GameIsPlaying && RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.ABFirstTurn)
            {
                //turnManager.TurnDuration += Time.deltaTime;
                return;
            }


            if (PokerActionPanel.Instance.checkPockeBetPlaced())//RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
            {

                // Debug.LogError("Check Here status");
                return;
            }


            //Debug.Log("Turn Time Get" + turnManager.RemainingSecondsInTurn);

            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                if (playerStateManagerInstance.PlayingList.Count <= 1)
                    return;
            }

            //if (MatchHandler.IsAndarBahar() && !uIManager.GetMyPlayerInfo().AbTurn)
            //{
            //    turnManager.TurnDuration += Time.deltaTime;
            //    return;
            //}


            if (MatchHandler.IsPoker())
            {

                //if (PhotonNetwork.IsConnected && uIManager.DisconnectedPanel.activeSelf == true)
                //    uIManager.DisconnectedPanel.SetActive(false);
                //else if (!PhotonNetwork.IsConnected && !uIManager.DisconnectedPanel.activeSelf != true)
                //    uIManager.DisconnectedPanel.SetActive(true);

                if (1 >= 1)
                {
                    if (currentPlayer != null)
                    {
                        if (currentPlayer.getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                        {
                            // Multiplayer turn timer: the old Photon line
                            // (turnManager.RemainingSecondsInTurn) was commented out in the
                            // Mirror port, so the filler never depleted online. Deplete it
                            // locally on every client at the same rate as offline.
                            if (currentPlayer.FillerImage != null)
                            {
                                currentPlayer.FillerImage.fillAmount -= Time.deltaTime * 0.06f;
                                PlayTikTimerSoundOnLessTime();
                                if (currentPlayer.FillerImage.fillAmount <= 0.05f && currentPlayer.FillerImage.gameObject.activeSelf)
                                {
                                    // Only the player whose turn it is auto-acts, so the
                                    // action reaches the server exactly once.
                                    if (currentPlayer.IsMine())
                                    {
                                        if (PokerActionPanel.Instance.checkPokerBtn.activeInHierarchy && !isturnTrue)
                                        {
                                            Debug.Log($"[TurnTimeout] AUTO-CHECK firing for {currentPlayer.gameObject.name}: fillAmount={currentPlayer.FillerImage.fillAmount}, isturnTrue was false");
                                            isturnTrue = true;
                                            PokerActionPanel.Instance.OnClickCheckBtn();
                                            Invoke(nameof(waitForNextTurnBoolOnceTime), 0.3f);
                                        }
                                        else if (!isturnTrue)
                                        {
                                            // This is the point that mis-fired live: if fillAmount
                                            // arrives here already near-zero on the very first frame
                                            // of a brand-new turn (see [ExecutingTurn]/[GetCurrentPlayingPlayer]
                                            // logs just before this for the fillAmount history), it means
                                            // the reset in TriggerStateExecutingTurn() didn't happen/didn't
                                            // stick — StandUp() below forces the player off the table
                                            // instead of a real timeout.
                                            Debug.LogWarning($"[TurnTimeout] AUTO-STANDUP firing for {currentPlayer.gameObject.name}: fillAmount={currentPlayer.FillerImage.fillAmount}, checkBtnActive={PokerActionPanel.Instance.checkPokerBtn.activeInHierarchy}, isturnTrue was false");
                                            isturnTrue = true;
                                            Game_Play.Instance.StandUp();
                                            Invoke(nameof(waitForNextTurnBoolOnceTime), 0.3f);
                                        }
                                    }
                                }
                            }
                            else
                                PlayTikTimerSoundOnLessTime();
                        }
                        else
                            currentPlayer = GetCurrentPlayingPlayer();
                    }
                    else
                        currentPlayer = GetCurrentPlayingPlayer();


                    if (checkPlayerRunInBackGround(currentPlayer))
                    {
                        Debug.LogWarning($"[TurnTimeout] checkPlayerRunInBackGround TRUE for {currentPlayer.gameObject.name} -> forcing OutOfTable (PlayingList.Count={playerStateManagerInstance.PlayingList.Count})");
                        if (playerStateManagerInstance.PlayingList.Count > 2)
                        {

                            if (NetworkServer.active && !isturnTrue)
                            {
                                isturnTrue = true;
                                SetNextPlayerToExecutingTurnPokeRunInBackGround(currentPlayer);
                                Invoke(nameof(waitForNextTurnBoolOnceTime), 0.3f);
                            }
                        }
                        currentPlayer.currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.OutOfTable);
                    }
                    else
                    {
                        if (0 > 0.11f && MatchHandler.IsPoker())
                        {
                            GameManager.Instance.isRunINBackGround = false;
                        }
                    }

                }

            }
            else
            {
                if (PlayerStateManager.Instance.PlayingList.Count > 1)
                {
                    if (currentPlayer != null)
                    {
                        if (currentPlayer.getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                        {
                            currentPlayer.FillerImage.fillAmount -= Time.deltaTime * 0.06f;
                            PlayTikTimerSoundOnLessTime();
                            if (currentPlayer.FillerImage.fillAmount <= 0.05f && currentPlayer.FillerImage.gameObject.activeSelf)
                            {
                                Debug.Log(currentPlayer.name);
                                if (currentPlayer.gameObject.name != LocalSettings.AI_Name)
                                {
                                    if (PokerActionPanel.Instance.checkPokerBtn.activeInHierarchy && !isturnTrue)
                                    {
                                        isturnTrue = true;
                                        PokerActionPanel.Instance.OnClickCheckBtn();
                                        Invoke(nameof(waitForNextTurnBoolOnceTime), 0.3f);
                                        return;
                                    }
                                    else
                                    {
                                        Game_Play.Instance.StandUp();
                                    }
                                }
                                else
                                {
                                    if (uIManager.GetMyPlayerInfo().pokerTotalBetCash == currentPlayer.pokerTotalBetCash && !isturnTrue)
                                    {
                                        isturnTrue = true;
                                        PokerActionPanel.Instance.AICheckBtn();
                                        SetNextPlayerToExecutingTurnPokeRunInBackGround(currentPlayer);
                                        Invoke(nameof(waitForNextTurnBoolOnceTime), 0.3f);
                                        return;
                                    }
                                    else
                                    {
                                        LocalSettings.AI_StandUP = true;
                                        currentPlayer.StandUp();
                                        SitHere.Instance.SetThisForAIPositionByPlayer();
                                    }
                                }
                            }
                        }
                        else
                            currentPlayer = GetCurrentPlayingPlayer();
                    }
                    else
                        currentPlayer = GetCurrentPlayingPlayer();
                }
            }





        }

        void PlayTikTimerSoundOnLessTime()
        {
            int remainingMin = (int)(5);

            if (currentPlayer.IsMine())
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
        void PlayTikTimerSoundOnLessTime(PlayerInfo info)
        {
            int remainingMin = (int)(5);

            if (info.IsMine())
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
           // Debug.Log("OnTurnBegins() turn: " + turn);

            IsShowingResults = false;
        }

        public void OnTurnCompleted(int obj)
        {
          //  Debug.Log("OnTurnCompleted: " + obj);

            //this.UpdateScores();
            this.OnEndTurn();
        }

        // when a player moved (but did not finish the turn)
        public void OnPlayerMove(Player photonPlayer, int turn, object move)
        {
            Debug.Log("OnPlayerMove: " + photonPlayer + " turn: " + turn + " action: " + move);
            throw new NotImplementedException();
        }

        // when a player made the last/final move in a turn
        public void OnPlayerFinished(Player photonPlayer, int turn, object move)
        {
            Debug.Log("OnTurnFinished: " + photonPlayer + " turn: " + turn + " action: " + move);

            //if (photonPlayer.IsLocal)
            //{
            //    Debug.Log("My Player Ends the Turn");
            //    //this.localSelection = (Hand)(byte)move;
            //}
            //else
            //{
            //    Debug.Log("Other Player Ends the Turn");
            //    //this.remoteSelection = (Hand)(byte)move;
            //}
        }
        public void OnTurnTimeEnds(int obj)
        {
            if (RoomStateManager.Instance.GetCurrentRoomState() != RoomState.STATE.GameIsPlaying)
                return;
            if (MatchHandler.IsPoker())
            {
                if (currentPlayer != null)
                {
                    if (currentPlayer.IsMine())
                    {
                        OnTurnCompleted(-1);
                        //if (turnManager.Turn > 1)
                        //{
                           
                            Game_Play.Instance.StandUp();

                        //}

                    }
                }
            }



        }
        //private void UpdateScores()
        //{

        //}

        #endregion

        #region Core Gameplay Methods


        /// <summary>Call to start the turn (only the Master Client will send this).</summary>
        public void StartTurn()
        {
            //if (PhotonNetwork.IsMasterClient)
            //{
            //    this.turnManager.BeginTurn();
            //}
        }

        /*    public void MakeTurn(Hand selection)
            {
                this.turnManager.SendMove((byte)selection, true);
            }*/

        public void OnEndTurn()
        {
            //this.StartCoroutine("ShowResultsBeginNextTurnCoroutine");
        }
        //public IEnumerator ShowResultsBeginNextTurnCoroutine()
        //{
        //    IsShowingResults = true;

        //    yield return new WaitForSeconds(1.0f);

        //    this.StartTurn();
        //}
        public void EndGame()
        {
            Debug.Log("EndGame");
        }

        //private void UpdatePlayerTexts()
        //{
        //    Player remote = PhotonNetwork.LocalPlayer.GetNext();
        //    Player local = PhotonNetwork.LocalPlayer;

        //    if (remote != null)
        //    {
        //        // should be this format: "name        00"
        //        //this.RemotePlayerText.text = remote.NickName;
        //        //this.RemotePlayerValueText.text = remote.GetScore().ToString("D2");
        //    }
        //    else
        //    {

        //        //TimerFillImage.anchorMax = new Vector2(0f, 1f);
        //        uIManager.TimeText.text = "";
        //        //this.RemotePlayerText.text = "A espera de um Oponente...";
        //        //this.RemotePlayerValueText.text = "00";
        //    }

        //    if (local != null)
        //    {
        //        // should be this format: "YOU   00"
        //        //this.LocalPlayerText.text = local.GetScore().ToString("D2");
        //    }
        //}
        public void OnClickConnect()
        {
            //PhotonNetwork.ConnectUsingSettings();
            // PhotonHandler.StopFallbackSendAckThread();  // this is used in the demo to timeout in background!
        }

        public void OnClickReConnectAndRejoin()
        {
            //PhotonNetwork.Disconnect();
            //PhotonNetwork.ReconnectAndRejoin();
            //PhotonHandler.StopFallbackSendAckThread();  // this is used in the demo to timeout in background!
        }
        #endregion

        void RefreshUIViews()
        {
            uIManager.FillerImage.fillAmount = 1;
            //ConnectUiView.gameObject.SetActive(!PhotonNetwork.InRoom);
            //GameUiView.gameObject.SetActive(PhotonNetwork.InRoom);
        }
        public  void OnLeftRoom()
        {
            //Debug.Log("You Left the Room");

            //RefreshUIViews();
        }

        //public override void OnPlayerLeftRoom(Player otherPlayer)
        //{
        //    Debug.Log("Other Player named " + otherPlayer.NickName + " left this Room");
        //    base.OnPlayerLeftRoom(otherPlayer);
        //}



        //public  void OnJoinedRoom()
        //{
        //    RefreshUIViews();

        //    if (PhotonNetwork.CurrentRoom.Players.Count == 2)
        //    {
        //        if (this.turnManager.Turn == 0)
        //        {
        //            // when the room has two players, start the first turn (later on, joining players won't trigger a turn)
        //            this.StartTurn();
        //        }
        //    }
        //    else
        //    {
        //        Debug.Log("Waiting for another player");
        //    }
        //}

        //public override void OnPlayerEnteredRoom(Player newPlayer)
        //{
        //    if (PhotonNetwork.CurrentRoom.Players.Count == 2)
        //    {
        //        if (this.turnManager.Turn == 0)
        //        {
        //            // when the room has two players, start the first turn (later on, joining players won't trigger a turn)
        //            this.StartTurn();
        //        }
        //    }
        //}



        void SetNextPlayerToExecutingTurnPokeRunInBackGround(PlayerInfo pInfo)
        {

            int nextPlayerInt = PlayerStateManager.Instance.PlayingList.IndexOf(pInfo);
            nextPlayerInt++;
            if (nextPlayerInt >= PlayerStateManager.Instance.PlayingList.Count)
                nextPlayerInt = 0;

            PlayerStateManager.Instance.PlayingList[nextPlayerInt].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
        }

        bool checkPlayerRunInBackGround(PlayerInfo pInfo)
        {
            return MatchHandler.IsPoker()
                && NetworkGameManager.Instance.RemainingSecondsInTurn > 0.05f
                && NetworkGameManager.Instance.RemainingSecondsInTurn < 0.1f
                && pInfo.checkApplicationBackground
                && pInfo.currentPlayerStateRef.currentState == PlayerState.STATE.ExecutingTurn;
        }

        [ShowOnly] public bool isturnTrue = false;
        void waitForNextTurnBoolOnceTime()
        {
            isturnTrue = false;
        }
    }
}