using System.Collections;
using Mirror;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityExtensions;

namespace TeenPattiGame
{
    public class GameStartManager : MonoBehaviourPunCallBacksWithNSSCallBacks
    {
        [ShowOnly]
        public float startWaitTime;
        public float waitTimeBeforeGame;
        [ShowOnly]
        public float sideShowCount;

        public GameObject GameStarWaitTextGameObject;


        public TMP_Text GameStartWaitText;
        public TMP_Text sideShowTimeText;

        [ShowOnly]
        public bool MinimumPlayerSatisfied;
        [ShowOnly]
        public bool GameIsGoingToStart;
        [ShowOnly]
        public bool IsGameStartingState;
        [ShowOnly]
        public bool _1stCurrentChaalBool = true;

        public PlayerTurnManager playerTurnManager;

        [ShowOnly]
        public int _currentNumberOfPlayers;

        #region Creating Instance
        private static GameStartManager _instance;
        public static GameStartManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindAnyObjectByType<GameStartManager>();
                return _instance;
            }
        }
        #endregion
        private void Awake()
        {
            if (_instance == null)
                _instance = this;
        }

        #region Add_Or_Remove_Player
        public void AddOrRemovePlayer(int plyr)
        {
            _currentNumberOfPlayers += plyr;
            TPLog.Flow("GameStartManager", "AddOrRemovePlayer(" + plyr + ") -> players at table = " + _currentNumberOfPlayers
                + (_currentNumberOfPlayers <= 1 ? " (not enough -> reset + wait)" : " (enough -> refresh wait text)"));


            if (_currentNumberOfPlayers <= 1)
            {
                if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
                {
                    if (RoomStateManager.Instance.GetIsNotInStartedState())
                    {
                        TPLog.Flow("GameStartManager", "Room is still in a pre-game state (" + RoomStateManager.Instance.CurrentRoomState + ") -> full table reset");
                        GameResetManager.Instance.ResetGameTeenPatti();
                    }
                    else
                    {
                        TPLog.Flow("GameStartManager", "Room already past start (" + RoomStateManager.Instance.CurrentRoomState + ") -> NOT resetting, hand keeps running");
                    }
                }


                if (!MatchHandler.isOffline())
                {

                    if (MirrorNetwork.Instance.isMasterClient)
                    {
                        if (MatchHandler.IsTeenPatti())
                        {
                            TPLog.Flow("GameStartManager", "Master client -> start countdown reset to " + LocalSettings.GameStartWaitTime + "s");
                            startWaitTime = LocalSettings.GameStartWaitTime;
                        }

                    }
                    else
                    {
                        TPLog.Flow("GameStartManager", "Not master client -> countdown will come from master");
                    }
                }
                else
                    startWaitTime = LocalSettings.GameStartWaitTime;

            }
            else
            {
                if (RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.GameIsPlaying)
                {
                    TPLog.Flow("GameStartManager", "Enough players and hand not running -> refreshing the waiting text");
                    ResetTxt();
                }
                else
                {
                    TPLog.Flow("GameStartManager", "Hand is already running -> waiting text left as it is");
                }
            }
        }
        void ResetTxt()
        {
            if (_currentNumberOfPlayers <= 1)
            {

                TPLog.Flow("GameStartManager", "ResetTxt: alone at table -> showing 'Waiting For Other Players'");
                GameStarWaitTextGameObject.SetActive(true);

                GameStartWaitText.text = "Waiting For Other Players";
                Pot.instance.PotTxt.text = "";

            }
            else
            {
                TPLog.Flow("GameStartManager", "ResetTxt: " + _currentNumberOfPlayers + " players -> clearing the waiting text");
                GameStartWaitText.text = "";

                //Pot.instance.SetCashText("");
            }

        }


        #endregion


        private void Start()
        {
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("GameStartManager", "Start (online) - waiting for TeenPattiNNetworkManager to exist");
                this.DelayUntil(() => TeenPattiNNetworkManager.instance != null, () =>
                {
                    {
                        // MULTIPLAYER ONLY. _currentNumberOfPlayers only counts the PlayerInfo
                        // objects spawned on THIS client, but the line below pushes the room state
                        // to everyone. While my second PlayerInfo is still spawning, the other
                        // client has already counted both players and started its countdown, so
                        // announcing "WaitingForPlayers" here resets everyone's countdown and can
                        // trigger a whole second deal. Ask the network how many players are really
                        // connected instead.
                        int connectedPlayers = NetworkGameManager.Instance != null
                            ? NetworkGameManager.Instance.currentPlayerCount
                            : _currentNumberOfPlayers;

                        TPLog.Flow("GameStartManager", "Network manager ready. spawnedHere=" + _currentNumberOfPlayers + " connected=" + connectedPlayers + " min=" + LocalSettings.GetMinPlayers());
                        if (connectedPlayers < LocalSettings.GetMinPlayers())
                        {
                            TPLog.Flow("GameStartManager", "Below minimum players -> room state WaitingForPlayers");
                            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.WaitingForPlayers);
                        }
                        else
                        {
                            TPLog.Flow("GameStartManager", "Minimum players already connected -> keeping current room state (my own objects may still be spawning)");
                        }
                    }
                });
                // if (PhotonNetwork.IsConnectedAndReady)

            }
            else
            {
                this.DelayUntil(() => RoomStateManager.Instance != null, () =>
                {
                    if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
                    {
                        RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.WaitingForPlayers);
                    }
                });

            }
        }

        private void Update()
        {

            LocalSettings.GamePlayTimeCount += Time.deltaTime;

            if (!MatchHandler.isOffline())
            {
                if (MirrorNetwork.Instance.isMasterClient && !MinimumPlayerSatisfied)
                {
                    TPLog.Change("GameStartManager", "minCheck", "Master client + minimum not satisfied yet -> checking player count every frame");
                    CheckingMinimumPlayersRequired();
                }
                else
                {
                    TPLog.Change("GameStartManager", "minCheck", "Not checking minimum players (master=" + MirrorNetwork.Instance.isMasterClient + ", minimumSatisfied=" + MinimumPlayerSatisfied + ")");
                }
            }
            else
            {
                if (!MinimumPlayerSatisfied)
                    CheckingMinimumPlayersRequired();
            }

            if (!MatchHandler.isOffline())
            {
                if (MirrorNetwork.Instance.isMasterClient && GameIsGoingToStart)
                {
                    if (!IsGameStartingState)
                    {
                        TPLog.Flow("GameStartManager", "First frame of the start countdown (master client)");
                        IsGameStartingState = true;
                        if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.WaitingForPlayers)
                        {
                            TPLog.Flow("GameStartManager", "Room was WaitingForPlayers -> moving to GameIsStarting");
                            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsStarting);
                        }
                        else
                        {
                            TPLog.Flow("GameStartManager", "Room already in " + RoomStateManager.Instance.CurrentRoomState + " -> not forcing GameIsStarting");
                        }

                    }
                    OtherPlayersWait();
                }
                else
                {
                    TPLog.Change("GameStartManager", "startTick", "Countdown not ticking here (master=" + MirrorNetwork.Instance.isMasterClient + ", GameIsGoingToStart=" + GameIsGoingToStart + ")");
                }
            }
            else
            {
                if (GameIsGoingToStart)
                {
                    if (!IsGameStartingState)
                    {
                        IsGameStartingState = true;
                        if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.WaitingForPlayers)
                            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsStarting);

                    }
                    OtherPlayersWait();
                }
            }
            sideShowTimeCounter();

        }

        void sideShowTimeCounter()
        {
            if (UIManager.Instance.GetMyPlayerInfo() == null)
            {
                TPLog.Change("GameStartManager", "sideShowNoPlayer", "sideShowTimeCounter skipped - my PlayerInfo does not exist yet");
                return;
            }
            if (UIManager.Instance.GetMyPlayerInfo().getCurrentPlayerState().currentState == PlayerState.STATE.RecieverSideShow)
            {
                Debug.Log("Check It Brother");
                TPLog.Change("GameStartManager", "sideShowTick", "I received a side-show request -> answer timer running");
                sideShowCount -= Time.deltaTime;
                sideShowTimeText.text = Mathf.RoundToInt(sideShowCount) + "S".ToString();
                if (sideShowCount <= 0)
                {
                    TPLog.Flow("GameStartManager", "Side-show answer timer hit 0 -> auto cancel, back to WaitingForTurn");
                    UIManager.Instance.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);
                    Game_Play.Instance.OnClickCancelSideShowBtn();
                }

            }

        }


        public void OtherPlayersWait()
        {
            // Debug.Log("other player wait 1:" + _currentNumberOfPlayers);
            if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
            {
                TPLog.Change("GameStartManager", "otherWait", "OtherPlayersWait: dropped below minimum (" + _currentNumberOfPlayers + "/" + LocalSettings.GetMinPlayers() + ") -> countdown cancelled");
                MinimumPlayerSatisfied = false;
                GameIsGoingToStart = false;
                return;
            }
            startWaitTime -= Time.deltaTime;
            // startWaitTime.Show("Start Wait Time");
            if (!MatchHandler.isOffline())
            {
                // photonView.RPC(nameof(WaitForOtherPlayersToJoinBeforeStart), RpcTarget.All, startWaitTime);
                if (!NetworkServer.active)
                {
                    TPLog.Change("GameStartManager", "otherWait", "OtherPlayersWait: I drive the countdown and broadcast it to the other player");
                    WaitForOtherPlayersToJoinBeforeStart(startWaitTime);
                    TeenPattiNNetworkManager.instance.CmdWaitForOtherPlayersToJoinBeforeStart(staticVariables.UserProfiledata.user._id, startWaitTime);
                }
                else
                {
                    TPLog.Change("GameStartManager", "otherWait", "OtherPlayersWait: running on the dedicated server -> countdown not broadcast from here");
                }
            }
            else
                WaitForOtherPlayersToJoinBeforeStart(startWaitTime);
        }

        void CheckingMinimumPlayersRequired()
        {

            if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
            {
                TPLog.Change("GameStartManager", "minPlayers", "Minimum players NOT met (" + _currentNumberOfPlayers + "/" + LocalSettings.GetMinPlayers() + ") -> waiting");
                GameStarWaitTextGameObject.SetActive(true);
                GameStartWaitText.text = "Waiting For Other Players";
                Pot.instance.SetCashText("");
            }
            else
            {

                TPLog.Change("GameStartManager", "minPlayers", "Minimum players MET (" + _currentNumberOfPlayers + "/" + LocalSettings.GetMinPlayers() + ") -> start countdown enabled");
                MinimumPlayerSatisfied = true;
                GameIsGoingToStart = true;
            }
        }

        //  int Tempnumber = 3; //network_time / 2;
        // int Tempnumber2 = -1; //network_time / 2;

        //[PunRPC]
        public void WaitForOtherPlayersToJoinBeforeStart(float network_time)
        {
            Debug.Log(" player wait 1");
            if (GameIsGoingToStart)
            {
                TPLog.Change("GameStartManager", "waitStart", "Countdown accepted from network: " + network_time.ToString("0.0") + "s left");
                startWaitTime = network_time;
            }
            else
            {
                TPLog.Change("GameStartManager", "waitStart", "Game not starting yet -> countdown parked at " + waitTimeBeforeGame + "s");
                startWaitTime = waitTimeBeforeGame;
            }

            if (UIManager.Instance.GetMyPlayerCurrentState() == null)
            {
                TPLog.Change("GameStartManager", "waitNoState", "Countdown ignored - my player state object does not exist yet");
                return;
            }

            if (UIManager.Instance.GetMyPlayerCurrentState().currentState != PlayerState.STATE.OutOfTable)
            {
                //Debug.LogError("check for other player entry .... 1");

                TPLog.Change("GameStartManager", "waitText", "I am seated -> showing 'Game Starting In " + network_time.ToString("0") + " seconds'");
                GameStartWaitText.text = "Game Starting In " + network_time.ToString("0") + " seconds";

            }

            else
            {
                TPLog.Change("GameStartManager", "waitText", "I am OutOfTable -> showing the plain waiting text instead of the countdown");
                ResetTxt();
            }


            if (network_time < 0 && GameIsGoingToStart)
            {
                TPLog.Flow("GameStartManager", "Countdown finished -> starting the hand");
                GameIsGoingToStart = false;
                GameStarted();

            }
        }


        public void CountingSoundForWingow()
        {
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ToonSound, false);
        }




        void GameStarted()
        {
            // if (MatchHandler.IsLuckyWar())
            //    RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsPlaying);
            // else
            TPLog.Flow("GameStartManager", "GameStarted -> room state CardDistributing, refreshing balances from API");
            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.CardDistributing);
            ApiAndRoomManager._instance.ModifyUserBalance(onfetchbalance =>
            {

                foreach (var item in GameManager.Instance.playersList)
                {
                    if (item.this_photonView.isOwned)
                    {
                        item.playerTotalCash.text = staticVariables.isgoldcoins ? onfetchbalance.data.gold_balance.ToString() : onfetchbalance.data.silver_balance.ToString();
                        Debug.Log("Updating Cash" + item.playerTotalCash.text);
                        //Constants_M.Log($"Updating Cash {onfetchbalance.data.silver_balance}");
                        PlayerPrefs.SetString(LocalSettings.TotalChips, item.playerTotalCash.text);
                        PlayerPrefs.SetString("CashInHand", item.playerTotalCash.text);
                        PlayerPrefs.Save();
                        GameManager.Instance.PlayerTotalChipsUpdate(0);
                    }
                }
                if (MatchHandler.isOffline())
                    PlayerPrefs.SetString("CashInHand", staticVariables.isgoldcoins ? onfetchbalance.data.gold_balance.ToString() : onfetchbalance.data.silver_balance.ToString());

            });
            //Constants_M.Log("Round id is about to create");

            // Offline opens its round here. ONLINE it is opened by the dedicated server from
            // ServerBeginCardDistribution instead: this method is only ever called by
            // WaitForOtherPlayersToJoinBeforeStart, which OtherPlayersWait invokes solely under
            // !NetworkServer.active and which early-returns without a local player object - so the
            // server never got here, and gating the round-open on NetworkServer.active *inside* this
            // method meant no online hand ever opened a casino round at all.
            if (MatchHandler.isOffline())
                OpenCasinoRound();
            else
                TPLog.Flow("GameStartManager", "Online -> the round is opened by the dedicated server, the id arrives over the network");
        }

        /// <summary>The id the backend knows this match by.
        /// On the dedicated server this comes from NetworkGameManager.transactionId - the value the
        /// server itself loaded from the Edgegap deployment environment - and it WINS over anything
        /// already in ApiAndRoomManager.winLoseChallengeId. That field is a client-side value: every
        /// place that sets it (challenge accept, Firebase notification, matchmaker) is a flow a
        /// headless build never runs, so on the server it is empty or left over from something else.
        /// ApiAndRoomManager.WinnerLossChallenge already performs exactly this overwrite before it
        /// settles, so doing it here too keeps the round-open and the settlement on the same id.</summary>
        string CasinoChallengeId()
        {
            if (NetworkServer.active
                && NetworkGameManager.Instance != null
                && !string.IsNullOrEmpty(NetworkGameManager.Instance.transactionId))
            {
                string serverId = NetworkGameManager.Instance.transactionId;

                if (ApiAndRoomManager._instance != null && ApiAndRoomManager._instance.winLoseChallengeId != serverId)
                {
                    TPLog.Flow("GameStartManager", "Challenge id taken from the server's deployment config: " + serverId
                        + " (local value was '" + ApiAndRoomManager._instance.winLoseChallengeId + "')");
                    ApiAndRoomManager._instance.winLoseChallengeId = serverId;
                }

                return serverId;
            }

            return ApiAndRoomManager._instance != null ? ApiAndRoomManager._instance.winLoseChallengeId : null;
        }

        /// <summary>Opens the casino round for the hand about to be dealt. Must run before the hand
        /// finishes: the settlement call is keyed on the round id this returns, and until now nothing
        /// called this online, so staticVariables.CurrentRoundID stayed empty for every hand.
        /// The request carries the deployment auth key, which never ships in the APK.</summary>
        public void OpenCasinoRound()
        {
            string challengeId = CasinoChallengeId();
            if (string.IsNullOrEmpty(challengeId))
            {
                TPLog.Warn("GameStartManager", "Round NOT opened - no challenge/transaction id available; settlement for this hand would have nothing to key on");
                return;
            }

            TPLog.Flow("GameStartManager", "Asking backend to open a new round for challenge " + challengeId);
            StartCoroutine(ServerConnection.GetApiRequest(ServerConnection.Start_Round_casino_Url + challengeId, jsonString =>
             {
                 JObject jsonObj = JObject.Parse(jsonString);
                 staticVariables.CurrentRoundID = jsonObj["_id"].ToString();
                 TPLog.Flow("GameStartManager", "Backend returned roundId=" + staticVariables.CurrentRoundID);
                 if (!MatchHandler.isOffline() && TeenPattiNNetworkManager.instance != null)
                       // Sent straight out as the ClientRpc rather than through the Cmd: the server IS the
                       // origin now. RpcSetRoundValue skips the sender, so 0 (no real user id) makes every
                       // client apply it.
                       TeenPattiNNetworkManager.instance.RpcSetRoundValue(0, staticVariables.CurrentRoundID);
             }, withSettlementAuth: true));
        }



        // [PunRPC]
        public void setRoundValue(string roundId)
        {
            TPLog.Flow("GameStartManager", "Round id received from master: " + roundId);
            staticVariables.CurrentRoundID = roundId;
        }
        public void ResetWaiting()
        {
            if (MirrorNetwork.Instance.isMasterClient && GameIsGoingToStart)
            {
                TPLog.Flow("GameStartManager", "ResetWaiting - master cancels the running start countdown");
                MinimumPlayerSatisfied = false;
                GameIsGoingToStart = false;
                if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
                {
                    TPLog.Flow("GameStartManager", "Below minimum players (" + _currentNumberOfPlayers + ") -> back to WaitingForPlayers");
                    RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.WaitingForPlayers);
                }
                else
                {
                    TPLog.Flow("GameStartManager", "Still enough players -> room state kept as " + RoomStateManager.Instance.CurrentRoomState);
                }
            }
            else
            {
                TPLog.Flow("GameStartManager", "ResetWaiting ignored (master=" + MirrorNetwork.Instance.isMasterClient + ", GameIsGoingToStart=" + GameIsGoingToStart + ")");
            }
        }



        #region Room_Changes_ Call_Backs

        #region Room_Waiting_For_Player_State

        // MULTIPLAYER ONLY. Every round now begins with each client resetting its own player,
        // before the countdown that ends in the deal. Until now that cleanup only happened via
        // ShowingResults -> GameResetManager, which only the master could trigger - and
        // isMasterClient is handed out once at match creation and never re-elected, so with the
        // master gone the finished round's state leaked into the next one.
        public override void OnRoomStateChangeToGameIsStarting()
        {
            TPLog.Flow("GameStartManager", "CALLBACK GameIsStarting - new round, clearing the old one before the countdown");

            if (MatchHandler.isOffline())
                return;

            // Screen first - this does not need my own player object to exist yet, so the old
            // round disappears immediately rather than after the wait below.
            if (GameResetManager.Instance != null)
                GameResetManager.Instance.ResetTableVisualsForNewRound();

            StartCoroutine(ResetMyOwnPlayerForNewRound());
        }

        // MULTIPLAYER ONLY
        IEnumerator ResetMyOwnPlayerForNewRound()
        {
            float giveUpAt = Time.unscaledTime + 3f;

            while (UIManager.Instance.GetMyPlayerInfo() == null && Time.unscaledTime < giveUpAt)
            {
                TPLog.Change("GameStartManager", "roundReset", "Round-start reset waiting - my player object does not exist yet");
                yield return null;
            }

            PlayerInfo me = UIManager.Instance.GetMyPlayerInfo();
            if (me == null)
            {
                TPLog.Warn("GameStartManager", "Round-start reset skipped - my player object never showed up");
                yield break;
            }

            // If the deal already started while we were waiting, leave everything alone -
            // clearing the card array now would wipe the cards of the round that just began.
            RoomState.STATE room = RoomStateManager.Instance.CurrentRoomState;
            if (room != RoomState.STATE.GameIsStarting && room != RoomState.STATE.WaitingForPlayers)
            {
                TPLog.Warn("GameStartManager", "Round-start reset skipped - room already moved on to " + room);
                yield break;
            }

            // Run the screen cleanup once more: player objects that spawned during the wait
            // (a reconnect brings them in late) would otherwise still carry the old round.
            if (GameResetManager.Instance != null)
                GameResetManager.Instance.ResetTableVisualsForNewRound();

            me.ResetMyselfForNewRound();
        }

        public override void OnRoomStateChangeToWaitingForPlayers()
        {

            TPLog.Flow("GameStartManager", "CALLBACK WaitingForPlayers - table is idle again");
            GameStartWaitText.text = "Waiting For Other Players";
            if (MatchHandler.IsTeenPatti())
            {
                TPLog.Flow("GameStartManager", "Start countdown reset to " + LocalSettings.GameStartWaitTime + "s");
                startWaitTime = LocalSettings.GameStartWaitTime;
            }


        }
        #endregion

        #region Room_Card_Distribution_State
        int counter = 0;



        public override void OnRoomStateChangeToCardDistributing()
        {
            "On Room change to cards Distributing".Show();
            TPLog.Flow("GameStartManager", "CALLBACK CardDistributing");
            GameStarWaitTextGameObject.SetActive(false);
            if (UIManager.Instance.MyLocalPlayer == null)
            {
                TPLog.Warn("GameStartManager", "Card distribution skipped - my local player object is still null");
                return;
            }

            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {
                CardDistributionDelay();
            }






            MinimumPlayerSatisfied = true;
            GameIsGoingToStart = false;
            IsGameStartingState = true;
        }



        // Server-only entry into the deal. The CardDistributing callback that normally leads here is
        // raised by RoomStateManager's callback tree, and the dedicated server never runs that tree
        // for a state a CLIENT pushed - so TeenPattiNNetworkManager.CmdUpdateThisStateOnNetwork calls
        // this directly. Everything below it (DealWhenEveryoneIsSpawned ->
        // MarkSeatedPlayersAbleToJoinBeforeDeal -> DoCardDistribution) already works without a local
        // player object: it reads GameManager.playersList and the synced player states.
        bool serverDistributionRunning;

        public void ServerBeginCardDistribution()
        {
            if (!NetworkServer.active || MatchHandler.isOffline())
                return;

            if (serverDistributionRunning)
            {
                TPLog.Warn("GameStartManager", "Server card distribution already running -> second CardDistributing ignored");
                return;
            }

            serverDistributionRunning = true;
            TPLog.Flow("GameStartManager", "Server -> beginning card distribution");

            // Open the casino round first: the hand cannot be settled without a round id, and this is
            // the earliest point in an online hand that the server reliably reaches.
            OpenCasinoRound();

            CardDistributionDelay();
        }

        void CardDistributionDelay()
        {
            "Cards Distributing Delay".Show();

            // MULTIPLAYER ONLY. CardDistributing arrives from the master the moment its own
            // countdown ends. A client that is still spawning the other player's object would
            // otherwise deal with a local count of 1 - three dummy cards into one seat - which
            // is exactly why the opponent gets no dealing animation. Wait for everybody first.
            if (!MatchHandler.isOffline())
            {
                StartCoroutine(DealWhenEveryoneIsSpawned());
                return;
            }

            DoCardDistribution();
        }

        // MULTIPLAYER ONLY
        IEnumerator DealWhenEveryoneIsSpawned()
        {
            int expected = NetworkGameManager.Instance != null
                ? NetworkGameManager.Instance.currentPlayerCount
                : 0;
            float giveUpAt = Time.unscaledTime + 5f;

            while (expected > 1
                   && GameManager.Instance.playersList.Count < expected
                   && Time.unscaledTime < giveUpAt)
            {
                TPLog.Change("GameStartManager", "dealWait", "Dealing held - only " + GameManager.Instance.playersList.Count + " of " + expected + " player objects have spawned here");
                yield return null;
            }

            if (expected > 1 && GameManager.Instance.playersList.Count < expected)
                TPLog.Warn("GameStartManager", "Dealing anyway - only " + GameManager.Instance.playersList.Count + " of " + expected + " player objects arrived within 5s");
            else
                TPLog.Flow("GameStartManager", "All " + GameManager.Instance.playersList.Count + " player objects are here -> dealing now");

            MarkSeatedPlayersAbleToJoinBeforeDeal();
            yield return StartCoroutine(WaitForSeatedPlayersToBeAbleToJoin());

            DoCardDistribution();
        }

        // MULTIPLAYER ONLY.
        // GameResetManager.UpDateAllPlayersStates() is what marks every seated player
        // AbleToJoin for the next hand, and only the master client may do it. After a
        // reconnect the master runs that reset from AddOrRemovePlayer, while playersList
        // still holds nothing but its own freshly spawned object - the opponent is never
        // marked, his synced player state stays whatever it was, and
        // SetNewPlayerListForGamePlay then drops him from the playing list. The hand is
        // dealt for one player: too few cards, no turn ever starts, and
        // ChageRoomStateToGamePlaying jumps straight to ShowingResults, which is what
        // brings the 5 second countdown back.
        // Everybody is spawned by the time we get here, so let the master mark them again.
        void MarkSeatedPlayersAbleToJoinBeforeDeal()
        {
            if (MirrorNetwork.Instance == null || !MirrorNetwork.Instance.isMasterClient)
            {
                TPLog.Flow("GameStartManager", "Not master client -> player states for this hand are set by the master");
                return;
            }

            GameManager gameManager = GameManager.Instance;
            for (int i = 0; i < gameManager.playersList.Count; i++)
            {
                PlayerInfo player = gameManager.playersList[i];
                if (player == null || player.playerCustomProperties == null)
                    continue;

                PlayerState.STATE synced = player.playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState);

                if (synced == PlayerState.STATE.AbleToJoin)
                {
                    TPLog.Flow("GameStartManager", "'" + player.name + "' is already AbleToJoin");
                    continue;
                }

                if (synced == PlayerState.STATE.OutOfTable)
                {
                    TPLog.Flow("GameStartManager", "'" + player.name + "' is OutOfTable -> he stays out of this hand");
                    continue;
                }

                TPLog.Warn("GameStartManager", "'" + player.name + "' is seated but his synced state is " + synced + " -> marking him AbleToJoin so he is dealt into this hand");
                player.UpdatePlayerState(PlayerState.STATE.AbleToJoin);
            }
        }

        // MULTIPLAYER ONLY. The states above travel Cmd -> server -> SyncDictionary, so they
        // are not readable back on the same frame. Dealing before they land would rebuild the
        // playing list from stale values and lose the player all over again.
        IEnumerator WaitForSeatedPlayersToBeAbleToJoin()
        {
            float giveUpAt = Time.unscaledTime + 3f;

            while (Time.unscaledTime < giveUpAt)
            {
                if (SeatedPlayersAreReadyToPlay())
                {
                    TPLog.Flow("GameStartManager", "Every seated player is AbleToJoin -> playing list will be complete");
                    yield break;
                }

                TPLog.Change("GameStartManager", "stateWait", "Waiting for the seated players' states to sync before dealing");
                yield return null;
            }

            TPLog.Warn("GameStartManager", "Dealing anyway - the seated players' states did not all reach AbleToJoin within 3s");
        }

        bool SeatedPlayersAreReadyToPlay()
        {
            GameManager gameManager = GameManager.Instance;
            for (int i = 0; i < gameManager.playersList.Count; i++)
            {
                PlayerInfo player = gameManager.playersList[i];
                if (player == null || player.playerCustomProperties == null)
                    continue;

                PlayerState.STATE synced = player.playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState);
                if (synced != PlayerState.STATE.AbleToJoin && synced != PlayerState.STATE.OutOfTable)
                    return false;
            }
            return true;
        }

        void DoCardDistribution()
        {
            TPLog.Flow("GameStartManager", "CardDistributionDelay - rebuilding the playing list before dealing");
            PlayerStateManager.Instance.UpdatePlayingList();

            // The server opens the hand before a single card moves, so handId and turnToken are
            // already valid by the time the first player intent arrives. Gated on
            // NetworkServer.active rather than isMasterClient: isMasterClient is ALSO true on the
            // challenge creator's phone (DirectChallengeMatchmaker.cs:607), and that is exactly the
            // authority this work is taking away from the client.
            if (!MatchHandler.isOffline() && NetworkServer.active && TeenPattiNNetworkManager.instance != null)
                TeenPattiNNetworkManager.instance.ServerBeginHand("card distribution");
            if (!MatchHandler.isOffline())
            {
                // The deal has moved off the players' phones. It used to run wherever isMasterClient
                // was true, and that flag is set on the challenge creator's device as well as on the
                // server (DirectChallengeMatchmaker.cs:607), so the creator was shuffling the cards
                // that decide real money. NetworkServer.active is the only honest gate here.
                if (NetworkServer.active && TeenPattiNNetworkManager.instance != null)
                    TeenPattiNNetworkManager.instance.ServerDealHand();
                else
                    TPLog.Flow("GameStartManager", "Client -> the server deals; the cards arrive over the network");
            }
            else
                GameManager.Instance.AssignCardsToAllPlayers();

            // MULTIPLAYER ONLY: the real cards are generated from PlayingList, so the dummy
            // cards must be counted from the same list - _currentNumberOfPlayers is a local
            // count and can still be behind. Offline keeps its old behaviour.
            int dealForPlayers = !MatchHandler.isOffline()
                ? PlayerStateManager.Instance.PlayingList.Count
                : _currentNumberOfPlayers;

            TPLog.Flow("GameStartManager", "Playing dealing animation for " + dealForPlayers + " players (spawnedHere=" + _currentNumberOfPlayers + ")");
            GameManager.Instance.InstantiateDummyPlayerCard(dealForPlayers);
            _1stCurrentChaalBool = true;

            // Scoped to the whole distribution run, so a repeated CardDistributing cannot start a
            // second deal for the same hand. ServerDealHand also refuses to deal a handId twice.
            if (NetworkServer.active)
                serverDistributionRunning = false;
        }

        #endregion

        #region Room_Game_Is_Playing_State
        public override void OnRoomStateChangeToGameIsPlaying()
        {
            TPLog.Flow("GameStartManager", "CALLBACK GameIsPlaying (firstChaalPending=" + _1stCurrentChaalBool + ")");
            if (_1stCurrentChaalBool)
            {
                TPLog.Flow("GameStartManager", "First time entering GameIsPlaying -> starting the turn manager");
                _1stCurrentChaalBool = false;
                StartTurnManager();
            }
            else
            {
                TPLog.Flow("GameStartManager", "GameIsPlaying again during the same hand -> turn manager NOT restarted");
            }

        }

        void StartTurnManager()
        {
            if (UIManager.Instance.GetMyPlayerCurrentState() == null)
            {
                TPLog.Warn("GameStartManager", "StartTurnManager aborted - my player state object is null");
                return;
            }

            TPLog.Flow("GameStartManager", "StartTurnManager - my state is " + UIManager.Instance.GetMyPlayerCurrentState().currentState);
            if (!MatchHandler.isOffline())
            {
                if (MirrorNetwork.Instance.isMasterClient)
                {
                    int gameCounter = TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.GetCustomRoomData(LocalSettings.Game_Counter_Key);
                    gameCounter++;
                    TPLog.Flow("GameStartManager", "Master client -> hand number is now " + gameCounter);
                    // if (PhotonNetwork.IsConnectedAndReady)
                    TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.SetCustomRoomData(LocalSettings.Game_Counter_Key, gameCounter);
                }
            }

            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
                if (!MatchHandler.isOffline())
                    if (NetworkServer.active)                                       //photon removal
                    {
                        TPLog.Flow("GameStartManager", "Server sets the turn duration directly to " + LocalSettings.PlayerTurnDuration + "s");
                        NetworkGameManager.Instance.TurnDuration = LocalSettings.PlayerTurnDuration;
                    }
                    else
                    {
                        TPLog.Flow("GameStartManager", "Client asks the server for a turn duration of " + LocalSettings.PlayerTurnDuration + "s");
                        NetworkGameManager.Instance.CmdSetDuration(LocalSettings.PlayerTurnDuration, true);
                    }







            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {
                foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
                {
                    item.isFirstCurrentChaalBool = true;
                }


                if (UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.AbleToJoin)
                {
                    TPLog.Flow("GameStartManager", "I am AbleToJoin -> I am in this hand, paying the boot amount");
                    UIManager.Instance.isPlayerPlayedThisHand = true;

                    UIManager.Instance.GetMyPlayerInfo().GiveChaalAmountOnGameStart();


                    PlayerStateManager.Instance.GetPlayerCardStatus();
                    Pot.instance.PotPanel.SetActive(true);

                }
                else
                {
                    TPLog.Flow("GameStartManager", "I am " + UIManager.Instance.GetMyPlayerCurrentState().currentState + " -> sitting this hand out");
                }
                StartingChaalAmount();
                if (!MatchHandler.isOffline())
                {
                    if (MirrorNetwork.Instance.isMasterClient)
                    {
                        if (UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.OutOfTable || UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.OutOfGame)
                        {
                            TPLog.Flow("GameStartManager", "Master is not playing this hand -> first player in the list gets the turn");
                            PlayerStateManager.Instance.PlayingList[0].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
                        }
                        else
                        {
                            TPLog.Flow("GameStartManager", "Master is playing -> master takes the first turn");
                            UIManager.Instance.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
                        }
                    }
                    else
                    {
                        TPLog.Flow("GameStartManager", "Not master client -> waiting for the master to hand out the first turn");
                    }
                }
                else
                {
                    PlayerStateManager.Instance.PlayingList[0].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
                    UIManager.Instance.GetAIPlayerInfo().currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);
                }

            }


        }
        public void StartingChaalAmount()
        {


            if (!MatchHandler.isOffline())
            {
                if (MirrorNetwork.Instance.isMasterClient)
                {
                    TPLog.Flow("GameStartManager", "Master client -> seeding the pot with the boot amount");
                    UIManager.Instance.GetMyPlayerInfo().StartPotAmount();
                }
                else
                {
                    TPLog.Flow("GameStartManager", "Not master client -> pot seeding is done by the master");
                }
            }
            else
            {
                UIManager.Instance.GetMyPlayerInfo().StartPotAmount();
                UIManager.Instance.GetAIPlayerInfo().StartPotAmount();
            }
        }




        #endregion

        #region Room_Waiting_For_Result_State
        public override void OnRoomStateChangeToWaitingForResults(string infoText)
        {
            TPLog.Flow("GameStartManager", "CALLBACK WaitingForResults info='" + infoText + "' -> hiding action buttons, result in " + LocalSettings.GameResultWaitingTime + "s");
            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {
                UIManager.Instance.ActionTable.SetActive(false);
                StartCoroutine(GameResultsManager.Instance.ShowResult(LocalSettings.GameResultWaitingTime, infoText));
            }





        }


        #endregion

        #region Room_Showing_Result
        public override void OnRoomStateChangeToShowingResults()
        {
            "Room State Changed".Show();
            TPLog.Flow("GameStartManager", "CALLBACK ShowingResults -> reset + next hand scheduled");
            if (MatchHandler.IsTeenPatti() || MatchHandler.isOffline())
            {
                GameResultsManager.Instance.ResetAllDataAndNewGamestart();
            }


        }

        #endregion

        #region Room_Change_AB_First_Turn_State
        public override void OnRoomStateChangeToABFirstTurn()
        {



        }



        #endregion

        #region Room_change_AB_2nd_Turn_state
        public override void OnRoomStateChangeToABSecondTurn()
        {

        }

        #endregion

        // public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        // {
        //     base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);   //photon removal
        // }
        #endregion



        #region Adding_Or_Subtraction_Event_System_Of_RoomProperty
        private void OnEnable()
        {
            RegisterRoomEvents();
        }

        private void OnDisable()
        {
            UnregisterRoomEvents();
            CancelInvoke("ResetTxt");
        }
        #endregion
    }
}