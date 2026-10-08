using Mirror;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;

namespace POKER
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
        [ShowOnly]
        public bool _waitingForResultsStarted = false;

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
                    _instance = FindObjectOfType<GameStartManager>();
                return _instance;
            }
        }
        #endregion
        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            NetworkGameManager.OnEventReceived += OnNetworkEvent;
        }

        private void OnDestroy()
        {
            NetworkGameManager.OnEventReceived -= OnNetworkEvent;
        }

        private void OnNetworkEvent(int eventCode, string data, Mirror.NetworkConnectionToClient sender)
        {
            if (eventCode == (int)EnumNetworkEventCodes.SetRoundId)
                setRoundValue(data);
        }

        #region Add_Or_Remove_Player
        public void AddOrRemovePlayer(int plyr)
        {
            _currentNumberOfPlayers += plyr;
            Debug.Log("Current Players : " + _currentNumberOfPlayers);
            if (_currentNumberOfPlayers <= 1)
            {
                // Teen Patti parity: if the table empties during the pre-start window
                // (waiting/starting/distributing), reset the round so pot/cards/states
                // don't linger and the room returns to WaitingForPlayers. Offline only —
                // online reconnection blips briefly drop the count to 1 and must never
                // wipe the game.
                if (MatchHandler.isOffline())
                {
                    if (AllScriptsManager.Instance.RoomStateManager.GetIsNotInStartedState())
                        AllScriptsManager.Instance.GameResetManager.ResetGamePoker();
                }

                if (!MatchHandler.isOffline())
                {
                    //  if (NetworkServer.active)
                    {
                        if (MatchHandler.IsPoker())
                        {
                            // Only reset timer when genuinely waiting for players.
                            // During reconnection, OnEnable fires for re-synced objects causing
                            // _currentNumberOfPlayers to briefly hit 1 — don't reset the timer
                            // if the game is already starting or in progress.
                            if (RoomStateManager.Instance == null
                                || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.WaitingForPlayers)
                            {
                                startWaitTime = LocalSettings.GameStartWaitTimePoker;
                            }
                        }
                    }
                }
                else
                    startWaitTime = LocalSettings.GameStartWaitTimePoker;

            }
            else
            {
                // Teen Patti parity (and || -> && bugfix: the old condition was always
                // true): don't touch the wait text while a hand is actively playing.
                if (RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.GameIsPlaying
                    && RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.ABFirstTurn
                    && RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.ABSecondTurn)
                    ResetTxt();
            }
        }
        void ResetTxt()
        {
            if (_currentNumberOfPlayers <= 1)
            {

                GameStarWaitTextGameObject.SetActive(true);

                GameStartWaitText.text = "Waiting For Other Players";
                Pot.instance.PotTxt.text = "";

            }
            else
            {
                GameStartWaitText.text = "";

                //Pot.instance.SetCashText("");
            }

        }


        #endregion


        private void Start()
        {
            if (NetworkServer.active || NetworkClient.active)
                MatchHandler.CurrentMatch = MatchHandler.MATCH.Poker;
            if (!MatchHandler.isOffline())
            {
                if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
                {
                    // Use local-only update here — Mirror is not fully initialized at Start() time.
                    // Broadcasting via PokerManager RPC at this point throws NullReferenceException
                    // because netIdentity.connectionToServer is not yet set up.
                    RoomStateManager.Instance.UpdateThisStateOnNetwork(RoomState.STATE.WaitingForPlayers, "");
                }
            }
            else
            {
                if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
                {
                    RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.WaitingForPlayers);
                }
            }
        }

        private void Update()
        {

            LocalSettings.GamePlayTimeCount += Time.deltaTime;

            // On reconnect, the local room state is restored from the SyncVar in
            // PlayerInfo.playerEnterenceState() (only when card data confirms a real
            // reconnection). Check the LOCAL state here — not the SyncVar — to avoid
            // false positives from stale SyncVars on reused servers.
            if (!MatchHandler.isOffline() && MatchHandler.IsPoker())
            {
                RoomState.STATE localState = RoomStateManager.Instance.CurrentRoomState;
                if (localState == RoomState.STATE.GameIsPlaying
                    || localState == RoomState.STATE.ABFirstTurn
                    || localState == RoomState.STATE.ABSecondTurn)
                {
                    GameIsGoingToStart = false;
                    MinimumPlayerSatisfied = true;
                    GameStarWaitTextGameObject.SetActive(false);
                    return;
                }
            }

            if (!MatchHandler.isOffline())
            {
                if (!MinimumPlayerSatisfied)
                {
                    CheckingMinimumPlayersRequired();
                }
            }
            else
            {
                if (!MinimumPlayerSatisfied)
                    CheckingMinimumPlayersRequired();
            }

            if (!MatchHandler.isOffline())
            {
                if (GameIsGoingToStart)
                {
                    if (!IsGameStartingState)
                    {
                        IsGameStartingState = true;
                        // ✅ FIX: Only the server should broadcast GameIsStarting.
                        // On reconnect, the client's CurrentRoomState defaults to WaitingForPlayers
                        // before it is restored, which caused the client to send CmdRiseEvent(182,"1")
                        // and overwrite syncedPokerRoomState on the server → PlayerInfo saw IsStarted()=false
                        // → set AbleToJoin → game restarted instead of restoring cards/turn state.
                        if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.WaitingForPlayers
                            && (MatchHandler.isOffline() || NetworkServer.active))
                            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsStarting);

                    }
                    OtherPlayersWait();
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

        }




        public void OtherPlayersWait()
        {
            // Same guard as Teen Patti: if players dropped below minimum mid-countdown
            // (e.g. busted AI stood up and its 3-11s re-sit coroutine hasn't finished),
            // cancel the countdown. CheckingMinimumPlayersRequired re-arms it with a
            // fresh timer once the AI sits back down.
            if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
            {
                MinimumPlayerSatisfied = false;
                GameIsGoingToStart = false;
                return;
            }
            // Both server and client count down so the UI timer shows on both sides.
            // GameStarted() is guarded inside WaitForOtherPlayersToJoinBeforeStartAI so only server triggers it.
            startWaitTime -= Time.deltaTime;
            WaitForOtherPlayersToJoinBeforeStartAI(startWaitTime);
        }

        void CheckingMinimumPlayersRequired()
        {

            if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
            {
                GameStarWaitTextGameObject.SetActive(true);
                GameStartWaitText.text = "Waiting For Other Players";
                Pot.instance.SetCashText("");
            }
            else
            {

                MinimumPlayerSatisfied = true;
                GameIsGoingToStart = true;
            }
        }

        //  int Tempnumber = 3; //network_time / 2;
        // int Tempnumber2 = -1; //network_time / 2;

        private void WaitForOtherPlayersToJoinBeforeStart(float network_time)
        {
            if (GameIsGoingToStart)
                startWaitTime = network_time;
            else
                startWaitTime = waitTimeBeforeGame;

            if (UIManager.Instance.GetMyPlayerCurrentState() == null)
                return;

            if (UIManager.Instance.GetMyPlayerCurrentState().currentState != PlayerState.STATE.OutOfTable)
            {

                GameStartWaitText.text = "Game Starting In " + network_time.ToString("0") + " seconds";

            }

            else
                ResetTxt();

            if (network_time < 0 && GameIsGoingToStart)
            {
                GameIsGoingToStart = false;
                GameStarted();

            }
        }
        private void WaitForOtherPlayersToJoinBeforeStartAI(float network_time)
        {
            if (GameIsGoingToStart)
                startWaitTime = network_time;
            else
                startWaitTime = waitTimeBeforeGame;
            if (NetworkServer.active)
            {
                GameStartWaitText.text = "Game Starting In " + network_time.ToString("0") + " seconds";

            }
            else
            {
                if (UIManager.Instance.GetMyPlayerCurrentState() == null)
                    return;

                if (UIManager.Instance.GetMyPlayerCurrentState().currentState != PlayerState.STATE.OutOfTable || NetworkServer.active)
                {

                    GameStartWaitText.text = "Game Starting In " + network_time.ToString("0") + " seconds";

                }

                else
                    ResetTxt();
            }

            if (network_time < 0 && GameIsGoingToStart)
            {
                GameIsGoingToStart = false;
                // Only the server calls GameStarted() to avoid double-firing in multiplayer.
                // Clients receive the CardDistributing state update via NetworkGameManager RiseEventRpc.
                if (MatchHandler.isOffline() || NetworkServer.active)
                    GameStarted();
                ResetTxt();

            }
        }


        public void CountingSoundForWingow()
        {
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ToonSound, false);
        }




        void GameStarted()
        {
            // Safety net: never deal a round below the player minimum (busted AI may
            // still be re-seating). Flags reset so the countdown re-arms when it's back.
            if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
            {
                MinimumPlayerSatisfied = false;
                GameIsGoingToStart = false;
                return;
            }
            // if (MatchHandler.IsLuckyWar())
            //    RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsPlaying);
            // else
            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.CardDistributing);

            // MULTIPLAYER ORDERING FIX:
            // UpdateCurrentRoomState calls UpdateThisStateOnNetwork() locally first, which fires
            // OnRoomStateChangeToCardDistributing() on the server BEFORE RiseEventRpc(UpdateRoomState)
            // is sent to clients. If SettingRandomCardsArrayToRoomProperty() is called inside that
            // handler it enqueues RiseEventRpc(PokerCardsArray) BEFORE RiseEventRpc(UpdateRoomState).
            // Clients then receive PokerCardsArray first with PlayingList still empty -> no cards.
            // Fix: call SettingRandomCardsArrayToRoomProperty() here, AFTER UpdateCurrentRoomState,
            // so RiseEventRpc(UpdateRoomState) is always enqueued and delivered to clients first.
            if (!MatchHandler.isOffline() && NetworkServer.active)
            {
                PokerStatesManager.Instance.UpdateDIndex();
                PokerManager.Instance.SettingRandomCardsArrayToRoomProperty();

                PokerFlow.Hand++;
                if (PokerFlow.Hand == 1)
                    MatchFlow.Begin("Poker", $"game started — {PokerFlow.Players()}");
                MatchFlow.Log("Poker", $"hand {PokerFlow.Hand} — {PokerFlow.Players()}, blinds {LocalSettings.MinBetAmount / 2}/{LocalSettings.MinBetAmount}");
                MatchFlow.Log("Poker", "hole cards dealt");
            }
            ApiAndRoomManager._instance.ModifyUserBalance(onfetchbalance =>
            {

                foreach (var item in GameManager.Instance.playersList)
                {

                    if (item.IsMine())
                    {
                        string currentBalanceText = staticVariables.isgoldcoins
                            ? onfetchbalance.data.gold_balance
                            : onfetchbalance.data.silver_balance;

                        if (!BigInteger.TryParse(currentBalanceText, out BigInteger currentBalance))
                        {
                            Debug.LogWarning($"[PokerBalance] Ignoring invalid balance response '{currentBalanceText}'.");
                            continue;
                        }

                        if (item.playerTotalCash != null)
                            item.playerTotalCash.text = currentBalanceText;

                        Debug.Log("Updating Cash" + currentBalanceText);

                        // The balance request starts at CardDistributing and can return after a
                        // blind/bet has already reduced the local table stack. If the synchronized
                        // stack exists, preserve it instead of resetting the player back to the API
                        // wallet balance. Otherwise this is the initial authoritative stack.
                        bool hasSyncedTableCash = item.playerCustomProperties.customBigIntegerData
                            .ContainsKey(LocalSettings.PlayerPokerTableCashKey);
                        BigInteger tableBalance = hasSyncedTableCash
                            ? item.playerCustomProperties.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey)
                            : currentBalance;

                        LocalSettings.SetPokerBuyInChipsAbsolute(tableBalance);
                        if (!hasSyncedTableCash)
                            LocalSettings.SetTotalServerChips(currentBalanceText);
                        PlayerPrefs.SetString("CashInHand", currentBalanceText);

                        item.PokerTotalCash = currentBalance;
                        if (item.PokerTotalCashTxt != null)
                            item.PokerTotalCashTxt.text = LocalSettings.Rs(currentBalance);
                        UIManager.Instance?.RefreshPlayerTotalChips(tableBalance);

                        // Teen Patti parity: RPCWinner reports each player stake as
                        // (this snapshot - live balance), so the snapshot has to live on the synced
                        // property too - PlayerPrefs is invisible to the other player client.
                        // item.IsMine() is required here as well: the server repo runs this block
                        // for View_ID_Offline == 1001, and a dedicated server writing its own
                        // (meaningless) balance would overwrite the real snapshot on the synced property.
                        if (!MatchHandler.isOffline())
                        {
                            item.playerCustomProperties.SetCustomBigIntegerData("CashInHand", currentBalance);
                            item.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.TotalChips, currentBalance);
                            if (!hasSyncedTableCash)
                                item.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, tableBalance);
                        }
                        LocalSettings.AI_Amount = UnityEngine.Random.Range(125000, 160000);//(50000, 100000);

                        PlayerPrefs.SetString("CashInHandAI", LocalSettings.AI_Amount.ToString());
                        PlayerPrefs.Save();
                        //PlayerPrefs.SetString(LocalSettings.PokerTotalBuyInChips, "0");
                        // POKER.GameManager.Instance.PlayerTotalChipsUpdate(0);
                    }
                }
                if (MatchHandler.isOffline())
                {
                    string cash = staticVariables.isgoldcoins ? onfetchbalance.data.gold_balance.ToString() : onfetchbalance.data.silver_balance.ToString();
                    cash.Show("Show");
                    PlayerPrefs.SetString(LocalSettings.PokerTotalBuyInChips, staticVariables.isgoldcoins ? onfetchbalance.data.gold_balance.ToString() : onfetchbalance.data.silver_balance.ToString());
                    //PlayerPrefs.SetString(LocalSettings.PokerTotalBuyInChips, "0");
                }
            });
            ////Constants_M.Log("Round id is about to create");

            if (MatchHandler.isOffline() || NetworkServer.active)
            {
                // Reset so stale IDs from a previous round are never re-used
                staticVariables.CurrentRoundID = null;

                string roundUrl = ServerConnection.Start_Round_casino_Url + ApiAndRoomManager._instance.winLoseChallengeId;
                Debug.Log("[CreateRound] GET " + roundUrl + "  winLoseChallengeId=" + ApiAndRoomManager._instance.winLoseChallengeId);

                StartCoroutine(ServerConnection.GetApiRequest(roundUrl, jsonString =>
                {
                    JObject jsonObj = JObject.Parse(jsonString);
                    staticVariables.CurrentRoundID = jsonObj["_id"].ToString();
                    ConstantsData_M.Log("Round id :s" + staticVariables.CurrentRoundID);
                    // Broadcast roundId to all clients (server-only block, so RiseEventRpc is safe here).
                    if (!MatchHandler.isOffline() && NetworkGameManager.Instance != null)
                        NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.SetRoundId, staticVariables.CurrentRoundID);
                }, failed =>
                {
                    Debug.LogError("[CreateRound] FAILED: " + failed + "  URL=" + roundUrl);
                }, withSettlementAuth: true));
            }
        }
        public void setRoundValue(string roundId)
        {
            staticVariables.CurrentRoundID = roundId;
        }

        public void ResetWaiting()
        {
            //  if (PhotonNetwork.IsMasterClient && GameIsGoingToStart)
            {
                MinimumPlayerSatisfied = false;
                GameIsGoingToStart = false;
                if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
                {
                    RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.WaitingForPlayers);
                }
            }
        }



        #region Room_Changes_ Call_Backs

        #region Room_Waiting_For_Player_State

        public void OnRoomStateChangeToWaitingForPlayers()
        {

            GameStartWaitText.text = "Waiting For Other Players";

            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
                startWaitTime = LocalSettings.GameStartWaitTimePoker;


        }
        #endregion

        #region Room_Card_Distribution_State
        int counter = 0;

        public override void OnRoomStateChangeToCardDistributing()
        {
            GameStarWaitTextGameObject.SetActive(false);

            // Must run on server too so PlayingList is populated before DealerToNext() runs.
            PlayerStateManager.Instance.UpdatePlayingList();
            foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
            {
                item.isBetPlacedPocker = false;
            }

            if (UIManager.Instance.MyLocalPlayer == null)
                return;  // Dedicated server: PlayingList updated above; skip client-only UI setup.

            Debug.LogError("entering in poker");
            if (MatchHandler.IsPoker())
            {
                if (MatchHandler.isOffline())
                {
                    // Offline / AI mode: handle card generation here as normal.
                    PokerStatesManager.Instance.UpdateDIndex();
                    PokerManager.Instance.SettingRandomCardsArrayToRoomProperty();
                }
                else
                {
                    // Multiplayer: UpdateDIndex runs on all machines for UI.
                    // Server calls SettingRandomCardsArrayToRoomProperty() from GameStarted()
                    // AFTER UpdateCurrentRoomState() so RiseEventRpc(UpdateRoomState) is sent
                    // to clients before RiseEventRpc(PokerCardsArray) — correct ordering.
                    PokerStatesManager.Instance.UpdateDIndex();
                }
            }
            else
            {
                PokerStatesManager.Instance.UpdateDIndex();
                PokerManager.Instance.SettingRandomCardsArrayToRoomProperty();
            }



            MinimumPlayerSatisfied = true;
            GameIsGoingToStart = false;
            IsGameStartingState = true;
        }




        #endregion

        #region Room_Game_Is_Playing_State
        public override void OnRoomStateChangeToGameIsPlaying()
        {
            Debug.Log($"[GameStart] OnRoomStateChangeToGameIsPlaying called. _1stCurrentChaalBool={_1stCurrentChaalBool}, isServer={NetworkServer.active}");
            if (_1stCurrentChaalBool)
            {
                _1stCurrentChaalBool = false;
                StartTurnManager();
            }
        }

        void StartTurnManager()
        {
            // On a dedicated server, GetMyPlayerCurrentState() is null (no local player).
            // We use a coroutine to delay DealerToNext() by one frame so that the
            // GameIsPlaying RiseEventRpc (queued by UpdateCurrentRoomState just before this
            // method runs) is sent and received by clients BEFORE we queue
            // UpdateCurrentPlayerStateOnNetwork(ExecutingTurn) via DealerToNext().
            // Without this delay the client sees ExecutingTurn while CurrentRoomState is
            // still CardDistributing, so MethodOfExecutingTurn() hides the action buttons.
            if (!MatchHandler.isOffline() && NetworkServer.active)
            {
                StartCoroutine(ServerDealerToNextDelayed());
                return;
            }

            if (UIManager.Instance.GetMyPlayerCurrentState() == null)
                return;

            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    NetworkGameManager.Instance.syncedGameCounter++;
                }
            }




            if (UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.AbleToJoin)
            {
                NetworkGameManager.Instance.CmdSetDuration(LocalSettings.PlayerTurnDurationPoker, false);
            }





            if (MatchHandler.IsPoker())
            {
                // Debug.LogError("I am in Poker");
                if (UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.AbleToJoin)
                {
                    UIManager.Instance.isPlayerPlayedThisHand = true;
                }
                if (NetworkServer.active)
                {
                    if (PokerStatesManager.Instance.GetDIndexRoomProperty() < PlayerStateManager.Instance.PlayingList.Count)
                        DealerToNext();
                    else
                        DealerToNext();
                    // Yahan per hum ny circle men baari deni hy.

                }
                else
                {

                    // Bounds-checked for the same reason as DealerToNext(): a stale dealer index
                    // from a previous hand can be past the end of a shorter PlayingList.
                    int markerIndex = PokerStatesManager.Instance.GetDIndexRoomProperty();
                    if (markerIndex >= 0 && markerIndex < PlayerStateManager.Instance.PlayingList.Count
                        && PlayerStateManager.Instance.PlayingList[markerIndex] != null)
                        PlayerStateManager.Instance.PlayingList[markerIndex].Dealer.SetActive(true);
                }
            }
            else
            {
                if (UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.AbleToJoin)
                {
                    UIManager.Instance.isPlayerPlayedThisHand = true;
                }
                DealerToNext();

                //PlayerStateManager.Instance.PlayingList[PokerStatesManager.Instance.GetDIndexRoomProperty()].Dealer.SetActive(true);

            }

        }

        // Waits a few frames so the GameIsPlaying RiseEventRpc (sent at end of the frame
        // UpdateCurrentRoomState was called) arrives at clients before DealerToNext()
        // queues UpdateCurrentPlayerStateOnNetwork(ExecutingTurn).
        // Using 3 frames to give clients enough time to process the GameIsPlaying event.
        // Also waits for PlayingList to be populated (it can be empty if the AbleToJoin
        // state hasn't been set yet on the server when CardDistributing fires).
        IEnumerator ServerDealerToNextDelayed()
        {
            yield return null;
            yield return null;
            yield return null;

            // Defensive: wait for PlayingList to be populated (up to ~1 second)
            if (PlayerStateManager.Instance.PlayingList.Count < 2)
            {
                Debug.LogWarning($"[ServerDealerToNextDelayed] PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count} < 2, waiting up to 60 frames...");
                for (int i = 0; i < 60 && PlayerStateManager.Instance.PlayingList.Count < 2; i++)
                    yield return null;
                Debug.Log($"[ServerDealerToNextDelayed] After wait: PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count}");
            }

            // If still empty, try re-populating PlayingList as a fallback
            if (PlayerStateManager.Instance.PlayingList.Count < 2)
            {
                Debug.LogWarning($"[ServerDealerToNextDelayed] PlayingList still empty! Re-calling UpdatePlayingList...");
                PlayerStateManager.Instance.UpdatePlayingList();
                Debug.Log($"[ServerDealerToNextDelayed] After re-populate: PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count}");
            }

            if (PlayerStateManager.Instance.PlayingList.Count >= 2)
            {
                DealerToNext();
            }
            else
            {
                Debug.LogError($"[ServerDealerToNextDelayed] CANNOT start game - PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count}. No players with AbleToJoin state found.");
            }
        }

        int counternwe = 0;
        void DealerToNext()
        {
            Debug.Log($"[DealerToNext] START. isServer={NetworkServer.active}");
            PlayerStateManager Psm = PlayerStateManager.Instance;

            int dealerIndex = PokerStatesManager.Instance.GetDIndexRoomProperty();
            Debug.Log($"[DealerToNext] dealerIndex={dealerIndex}, PlayingList.Count={Psm.PlayingList.Count}");

            // The dealer index is a room property that outlives players leaving and rejoining, so it
            // can point past the end of a shorter PlayingList. Indexing it then threw
            // IndexOutOfRangeException on the server and the hand was never dealt at all.
            if (Psm.PlayingList.Count == 0)
            {
                Debug.LogError("[DealerToNext] PlayingList is empty - cannot deal.");
                return;
            }
            if (dealerIndex < 0 || dealerIndex >= Psm.PlayingList.Count)
            {
                Debug.LogWarning($"[DealerToNext] Stale dealer index {dealerIndex} for {Psm.PlayingList.Count} " +
                                 $"players (someone left or rejoined) - wrapping to 0.");
                dealerIndex = 0;
            }

            Psm.PlayingList[dealerIndex].Dealer.SetActive(true);
            MatchFlow.Log("Poker", $"dealer {PokerFlow.Who(Psm.PlayingList[dealerIndex])}");


            dealerIndex = DealerToNextPlayerTurn(dealerIndex);

            //  SetBetAmountOnDealerpoker(Psm.PlayingList.Count == 2 ? false : true, Psm.PlayingList[dealerIndex].photonView.ViewID);
            //  dealerIndex = DealerToNextPlayerTurn(dealerIndex);
            //  SetBetAmountOnDealerpoker(Psm.PlayingList.Count == 2 ? true : false, Psm.PlayingList[dealerIndex].photonView.ViewID);
            // Old Is Gold F
            Debug.Log(dealerIndex + " " + counternwe++);
            PokerActionPanel.Instance.SetBetAmountOnDealer(Psm.PlayingList.Count == 2 ? false : true, Psm.PlayingList[dealerIndex]);
            MatchFlow.Log("Poker", $"{PokerFlow.Who(Psm.PlayingList[dealerIndex])} posts blind {(Psm.PlayingList.Count == 2 ? LocalSettings.MinBetAmount : LocalSettings.MinBetAmount / 2)} (pot {PokerFlow.Pot()})");

            dealerIndex = DealerToNextPlayerTurn(dealerIndex);
            Debug.Log(dealerIndex + " " + counternwe++);
            PokerActionPanel.Instance.SetBetAmountOnDealer(Psm.PlayingList.Count == 2 ? true : false, Psm.PlayingList[dealerIndex]);
            MatchFlow.Log("Poker", $"{PokerFlow.Who(Psm.PlayingList[dealerIndex])} posts blind {(Psm.PlayingList.Count == 2 ? LocalSettings.MinBetAmount / 2 : LocalSettings.MinBetAmount)} (pot {PokerFlow.Pot()})");

            dealerIndex = Psm.PlayingList.Count == 2 ? dealerIndex : DealerToNextPlayerTurn(dealerIndex);

            Debug.Log($"[DealerToNext] About to set ExecutingTurn on PlayingList[{dealerIndex}], ownerPlayerId={Psm.PlayingList[dealerIndex].ownerPlayerId}");
            Psm.PlayingList[dealerIndex].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
            Debug.Log($"[DealerToNext] ExecutingTurn SET successfully");
            Psm.PlayingList[dealerIndex].SetCircleFlatPoker();


        }
        int DealerToNextPlayerTurn(int Number)
        {
            Number++;
            if (Number >= PlayerStateManager.Instance.PlayingList.Count)
            {
                Number = 0;
            }
            return Number;
        }

        public void SetBetAmountOnDealerpoker(bool isHalf, int viewID)
        {
            // No longer used — game calls PokerActionPanel.Instance.SetBetAmountOnDealer() directly.
        }
        void SetBetAmountOnDealerPokerRPC(bool isHalf, int viewID)
        {

            Debug.LogError("Check View ID...1..." + viewID);
            for (int i = 0; i < PlayerStateManager.Instance.PlayingList.Count; i++)
            {
                PlayerInfo pInfo = PlayerStateManager.Instance.PlayingList[i];
                if (pInfo.View_ID_Offline == viewID)
                {
                    Debug.LogError("Check View ID...2..." + viewID);
                    PokerActionPanel.Instance.SetBetAmountOnDealer(isHalf, pInfo);

                }
            }
            //if (UIManager.Instance.GetMyPlayerInfo().photonView.ViewID == viewID)
            //{
            //}
        }
        #endregion

        #region Room_Waiting_For_Result_State
        public override void OnRoomStateChangeToWaitingForResults(string infoText)
        {
            // Dedicated server has no local player or UI, skip client-side logic
            if (NetworkServer.active && !NetworkClient.active) return;

            // Guard: prevent duplicate WaitingForResults from starting the winner coroutine twice.
            // Both clients independently call PokerManager.Nowreset() when community cards finish,
            // causing duplicate CmdRiseEvent(WaitingForResults) on the server which broadcasts twice.
            if (_waitingForResultsStarted) return;
            _waitingForResultsStarted = true;

            Debug.LogError("23.....Check Here For Poker Cash.....");

            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                var myState = UIManager.Instance.GetMyPlayerCurrentState();
                if (myState != null && myState.currentState != PlayerState.STATE.OutOfGame && myState.currentState != PlayerState.STATE.OutOfTable)
                    myState.UpdateCurrentPlayerState(PlayerState.STATE.Watching);

                StartCoroutine(waitForGameShowReseltAndReset(5f));
            }



        }

        IEnumerator waitForGameShowReseltAndReset(float delay)
        {
            yield return new WaitForSeconds(1);

            PokerCheckWinner.Instance.DeclareWinnerOfPoker();
            yield return new WaitForSeconds(1);
            // Debug.LogError("Pocker Check BetAmount" + UIManager.Instance.GetMyPlayerInfo().player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey));
            foreach (var item in PlayerStateManager.Instance.PlayingList)
            {
                if (!MatchHandler.isOffline())
                {
                    item.PokerTotalCashTxt.text = LocalSettings.Rs(item.playerCustomProperties.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey));
                }
                else
                {

                    item.PokerTotalCashTxt.text = (item.gameObject.name != LocalSettings.AI_Name) ? LocalSettings.Rs(LocalSettings.GetPokerBuyInChips()) : LocalSettings.Rs(LocalSettings.AI_Amount);

                    item.PokerTotalCashTxt.text.Show();
                }
                Debug.LogError("2.....Check Here For Poker Cash....." + item.PokerTotalCashTxt.text);

            }

            yield return new WaitForSeconds(delay - 2);
            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.ShowingResults);
        }
        #endregion

        #region Room_Showing_Result
        public override void OnRoomStateChangeToShowingResults()
        {
            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                // Only the server broadcasts new room state — clients receive it via RiseEventRpc.
                // Without this guard the client sends CmdRiseEvent(GameIsStarting) to the server,
                // causing multiple spurious GameIsStarting events and countdown restarts.
                if (MatchHandler.isOffline() || NetworkServer.active)
                {
                    if (PlayerStateManager.Instance.PlayingList.Count < 1)
                    {
                        if (_currentNumberOfPlayers < LocalSettings.GetMinPlayers())
                            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.WaitingForPlayers);
                        else
                            RoomStateManager.Instance.UpdateCurrentRoomState(RoomState.STATE.GameIsStarting);
                    }
                }
                GameResultsManager.Instance.ResetAllDataAndNewGamestart();
            }
        }

        #endregion

        #region Room_Change_AB_First_Turn_State
        public override void OnRoomStateChangeToABFirstTurn()
        {
            // Dedicated server has no local player or UI, skip client-side logic
            if (NetworkServer.active && !NetworkClient.active) return;

            var myState = UIManager.Instance.GetMyPlayerCurrentState();
            if ((MatchHandler.IsPoker() || MatchHandler.isOffline()) && myState != null && (myState.currentState != PlayerState.STATE.OutOfGame && myState.currentState != PlayerState.STATE.OutOfTable))
            {
                // Debug.LogError("Check Bool " + CheckPlayerExistOrNot());
                UIManager.Instance.GetMyPlayerInfo().PokerBetGoToThePot();
            }
            else
            {
                PokerActionPanel.Instance.AllPokerBetsGoToFinalPoint();
            }

        }



        #endregion


        //public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        //{
        //    base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        //}
        #endregion




    }
}
