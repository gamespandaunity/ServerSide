using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using System.Numerics;
using System;
using TMPro;
//using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using System.Net.NetworkInformation;
using UnityEngine.SceneManagement;
using System.Linq;
using NaughtyAttributes;
using Unity.Collections.LowLevel.Unsafe;
using Mirror;

namespace POKER

{

    public class PlayerInfo : NetworkBehaviour
    {
        /// <summary>
        /// Set by the server on spawn. Identifies which player this PlayerInfo belongs to.
        /// Used by IsMine() as a replacement for isOwned under server-authority spawning.
        /// </summary>
        [SyncVar] public string ownerPlayerId;

        /// <summary>
        /// Replacement for isOwned. Returns true on the client whose user ID matches
        /// this PlayerInfo's owner. Works with server-authority spawning where isOwned
        /// is always false on clients.
        /// </summary>
        public bool IsMine()
        {
            if (string.IsNullOrEmpty(ownerPlayerId))
            {
                Debug.LogWarning($"[IsMine] ownerPlayerId is EMPTY on {gameObject.name} (netId={netId})");
                return false;
            }
            string myId = staticVariables.UserProfiledata?.user != null ? staticVariables.UserProfiledata.user._id.ToString() : "NULL";
            bool result = myId == ownerPlayerId;
            if (!result)
                Debug.LogWarning($"[IsMine] MISMATCH on {gameObject.name}: myId={myId}, ownerPlayerId={ownerPlayerId}");
            return result;
        }

        // AI
        public BigInteger AI_Amount;
        public BigInteger val;
        public int View_ID_Offline;
        [ShowOnly] public BigInteger pokerAiTotalBetCash = 0;
        //
        //  new poker things
        public GameObject My_Player_Indicator;
        // end new poker things
        public GameObject PlayerDummyCardsToShowParent;
        public GameObject PlayerOrignalCardsToShowParent;
        public GameObject ShowBtn;
        public GameObject viewResultAB;
        public GameObject SeenIndicator;
        public GameObject BlindIndicator;
        public GameObject WinningIndicator;
        public GameObject BetAmountAnim;
        public GameObject tipAmountAnim;
        public GameObject SideShowIndicatorAnim;
        public GameObject ABBettingSection;
        // Lucky War Game K lye
        public GameObject LWBettingSection;

        public GameObject ChatMessageBox;
        public GameObject EmojiAnim;
        public GameObject FireAnimObj;

        [HideInInspector]




        public RectTransform For3PattiPlayer;

        public PlayerCustomProperties playerCustomProperties;

        public Image FillerImage;
        public Image PlayerAvatorImage;
        public Image playerFrameImage;

        public TMP_Text PicIndexTxt;
        public TMP_Text player_name;
        public TMP_Text BetAmountAnimText;
        public TMP_Text PackedText;
        public TMP_Text PlayerStateText;

        public TMP_Text AndarBetAmoutTxt;
        public TMP_Text BaharBetAmoutTxt;
        public TMP_Text TieBetAmoutTxt;
        public TMP_Text BetBetAmoutTxt;
        public TMP_Text SuperAndarBetAmoutTxt;
        public TMP_Text SuperBaharBetAmoutTxt;


        public TMP_Text playerTotalCash;

        public TMP_Text WingoWinCashTxt;

        public bool seated;
        public bool gamePlaying;
        //public bool gamePokerisStandUp; 
        [ShowOnly] public bool IsSeen;
        [ShowOnly] public bool IsSideShow;
        [ShowOnly] public bool AbTurn;
        [ShowOnly] public bool LWTurn;
        [ShowOnly] public bool firstTurnAB;
        [ShowOnly] public bool cardShuffleBool = false;


        [ShowOnly] public bool TieBetWinLw;


        // For Dragon Tiger
        [ShowOnly] public bool iSDragonTigerStart = false;

        public int TableIndex;
        public int PicIndexInt;
        [ShowOnly] public int MyScores;
        [ShowOnly] public int MyRank;
        [ShowOnly] public int[] OrgCardValues = { 0, 0, 0 };
        [ShowOnly]
        public int MyChaalsPlayedCounter;
        //[ShowOnly]
        //public int PlayerIndexInPlayingList;

        [ShowOnly]
        public int myNetworkSeat;

        [ShowOnly] public RectTransform playerRectTransform;

        public PlayerProperties player_properties;

        #region TransforGift



        #endregion

        #region Poker Variables
        /// <summary>
        /// poker 
        /// </summary>

        [Header("Poker Section")]
        public GameObject PokerParentThings;


        public GameObject DummyCardsParent;

        public RectTransform card_1_RectTr;
        public RectTransform card_2_RectTr;

        [ShowOnly] public CardProperty Hole_Card1;
        [ShowOnly] public CardProperty Hole_Card2;

        [ShowOnly] public CardProperty[] HighRankingCards;
        [ShowOnly] public List<CardProperty> remainingCards;

        [ShowOnly] public int PokerPlayerCurrentRank;
        [ShowOnly] public int PokerScores;
        public TMP_Text HandRankLabelTxt;
        public TMP_Text PokerInfoMessage;
        public GameObject BetStatusObj;
        public GameObject CashAllInIndicator;
        public TMP_Text PokerTotalCashTxt;
        public RectTransform isMinePokerTotalCashTranform;
        public BigInteger PokerTotalCash;


        public GameObject PokerBetAmountAnimPrefab;

        [ShowOnly] public GameObject pokerBetAmountAnim;
        [ShowOnly] public BigInteger pokerTotalBetCash = 0;
        [ShowOnly] public BigInteger PokerTotalWholeBetAmount = 0;
        public Transform FirstTargetPokerBetAmount;

        public GameObject Dealer;
        [ShowOnly]
        public bool isAllInCashBet;
        [ShowOnly]
        public bool isBetPlacedPocker = false;
        [ShowOnly]
        public bool isCircleCheckFlag = false;

        public List<string> tipsDialouges;
        [ShowOnly]
        public bool PokerPlayerCashAllIn;
        // poker end
        #endregion


        #region Lucky War Section
        /// <summary>
        /// Lucky War 
        /// </summary>
        [Header("Lucky War Section")]
        public CardProperty PlayerLWCard;
        public GameObject LWDummyCardPrent;

        #endregion
        public int AdjustGameManagerLocalSeat
        {
            get
            {
                return privateNetworkSeat;
            }
            set
            {
                privateNetworkSeat = value;
                if (IsMine())
                {
                    GameManager.Instance.myLocalSeat = value;
                }
            }
        }



        private int privateNetworkSeat;

        public PlayerCurrentState currentPlayerStateRef;


        private void Awake()
        {





        }


        public int GetMineIndexInPlayerList()
        {
            int index = AllScriptsManager.Instance.GameManager.playersList.FindIndex(x => x == this);
            return index;
        }

        void EnableDisableThings()
        {
            if (MatchHandler.IsPoker())
            {
                DeActivateUIOfAndarBahar(false);
            }

            LocalSettings.SetPosAndRect(player_name.transform.parent.gameObject, For3PattiPlayer, this.gameObject.transform);


        }
        int num = 0;
        IEnumerator Start()
        {
            // Constants_M.Log($"Poker player Name:{gameObject.name} ");
            yield return new WaitForSeconds(1);
            Debug.Log($"[PlayerInfo.Start] ownerPlayerId='{ownerPlayerId}', IsMine={IsMine()}, netId={netId}, isServer={NetworkServer.active}");
            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                StartCoroutine(GetPropertyBuyInChips());
            }
            currentPlayerStateRef = GetComponent<PlayerCurrentState>();
            player_properties = GetComponent<PlayerProperties>();

            // Add to playersList EARLY — before any UI code that may throw on the
            // dedicated server (NullRef on UI Text, Image, etc.).  If this runs after
            // crash-prone code, an unhandled exception kills the coroutine and the
            // player is never added, leaving PlayingList permanently empty.
            AllScriptsManager.Instance.GameManager.AddingPlayer(this);

            MyScores = 0;
            MyRank = 0;

            MyTotalCashTextUpdate();
            if (!MatchHandler.isOffline())
            {
                if (IsMine())
                {
                    RoomStateManager.Instance.UpdateLocalRoomState((RoomState.STATE)NetworkGameManager.Instance.syncedPokerRoomState);
                }
            }
            //SetRoomStateFromNetworkCustomProperty();
            if (!MatchHandler.isOffline())
            {
                //Constants_M.Log($"{AllScriptsManager.Instance} {this}");
                if (AllScriptsManager.Instance == null)
                {
                    if (FindAnyObjectByType<AllScriptsManager>())
                        FindAnyObjectByType<AllScriptsManager>()?.Awake();
                }
                if (NetworkServer.active)
                {
                    if (AllScriptsManager.Instance != null)
                        AllScriptsManager.Instance.PositionsManager.AssignPositionOfthisPlayer(this);
                    else
                        PositionsManager.Instance.AssignPositionOfthisPlayer(this);
                }
            }
            //PhotonNetwork.LocalCleanPhotonView(photonView);

            EnableDisableThings();
            Playeramount();

            playerEnterenceState();

            num++;
            // AddingPlayer moved to earlier in Start() — see above.
            IsSeen = false;
            if (IsMine())
            {
                UIManager.Instance.MyLocalPlayer = gameObject;
                WinningIndicator.transform.GetChild(0).localScale = UnityEngine.Vector3.one * 1.1f;
                SideShowIndicatorAnim.transform.GetChild(0).localScale = UnityEngine.Vector3.one * 1.1f;
                AssignName(LocalSettings.GetPlayerName());

                // After reconnection, trigger game state recovery.
                // GameManager.StartingThings() only runs once at scene load. On reconnect,
                // CheckAllPlayersConnected() never re-runs, so the recovery code (rebuild
                // PlayingList, restore hole cards, community cards) is dead code.
                // Also fixes the race: server may send CardDistributing before UIManager
                // is set, causing OnRoomStateChangeToCardDistributing to early-return.
                if (!MatchHandler.isOffline() && GameManager.Instance != null)
                {
                    StartCoroutine(GameManager.Instance.CheckAllPlayersConnected());
                }
            }
            else if (!MatchHandler.isOffline())
                AssignName(staticVariables.OpponetProfile?.userName);
            else
            {
                if (gameObject.name == LocalSettings.AI_Name)
                {
                    UIManager.Instance.AI_Player = gameObject;
                    AssignName(LocalSettings.AI_Name);
                    My_Player_Indicator.gameObject.SetActive(false);
                    ShowBtn.GetComponent<Button>().interactable = false;
                }
                else
                {
                    My_Player_Indicator.gameObject.SetActive(true);
                    UIManager.Instance.MyLocalPlayer = gameObject;
                    AssignName(LocalSettings.GetPlayerName());
                }

            }
            WinningIndicator.transform.GetChild(0).localScale = UnityEngine.Vector3.one * 1.1f;
            SideShowIndicatorAnim.transform.GetChild(0).localScale = UnityEngine.Vector3.one * 1.1f;
            PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
            BetAmountAnim.SetActive(false);
            SetProfileImage += UpdateProfileImageAfterReceivingFromServer;
            ResetAllPlayerKeys();
            //if (!LocalSettings.Get_Auto_Seated_Status() && photonView.IsMine)
            //{
            //    StandUp();
            //}

            GetProfilePic();
            if (!MatchHandler.isOffline())
            {
                My_Player_Indicator.gameObject.SetActive(IsMine());
            }

        }






        void ResetAllPlayerKeys()
        {
            if (IsMine())
            {
                if (!NetworkServer.active)
                {
                    //player.SetCustomBigIntegerData(LocalSettings.totalcashWinLossKey, 0);
                    playerCustomProperties.SetCustomBigIntegerData(LocalSettings.totalcashWinLossKey, BigInteger.Parse(0.ToString()));
                    playerCustomProperties.SetCustomData(LocalSettings.TotalHandsKey, 0);
                    playerCustomProperties.SetCustomData(LocalSettings.WinHandsKey, 0);
                }
                else
                {
                    "Server".Show();
                }

            }
        }

        public void Playeramount()
        {


            playerTotalCash.transform.parent.gameObject.SetActive(false);

        }



        // Is this seat still sitting in a live hand as far as the SERVER is concerned? Read from this
        // player's OWN SyncDictionary, which arrives with this object's own spawn payload - unlike
        // NetworkGameManager's SyncVars, which ride a different object and can be a frame behind.
        bool ServerHasMeInALiveHand()
        {
            if (MatchHandler.isOffline()) return false;
            PlayerState.STATE serverState = playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState);
            return serverState == PlayerState.STATE.ExecutingTurn
                || serverState == PlayerState.STATE.WaitingForTurn
                || serverState == PlayerState.STATE.BetPlaced
                || serverState == PlayerState.STATE.Packed
                || serverState == PlayerState.STATE.Watching;
        }

        void playerEnterenceState()
        {
            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                if ((LocalSettings.GetTotalChips() < LocalSettings.GetStartingMinAmountPoker() && LocalSettings.GetPokerBuyInChips() < LocalSettings.GetStartingMinAmountPoker()) && currentPlayerStateRef.currentState != PlayerState.STATE.OutOfTable)
                {
                    if (!MatchHandler.isOffline())
                    {
                        if (IsMine())
                        {
                            ConstantsData_M.Log(PlayerPrefs.GetString("TotalChips") + "1");
                            UIManager.Instance.quickShop.SetActive(true);

                            StandUp();
                            return;
                        }
                    }
                    else
                    {
                        if (this.gameObject.name != LocalSettings.AI_Name)
                        {
                            ConstantsData_M.Log(PlayerPrefs.GetString("TotalChips") + "5");
                            UIManager.Instance.quickShop.SetActive(true);
                            StandUp();
                            return;
                        }
                    }
                }
                PokerParentThings.SetActive(true);
                if (!MatchHandler.isOffline())
                {
                    if (IsMine())
                    {
                        PokerTotalCash = LocalSettings.GetTotalChips();
                        PokerTotalCash.Show("PokerTotalCash");

                    }
                    else
                    {
                        PokerTotalCash = playerCustomProperties.GetCustomBigIntegerData(LocalSettings.TotalChips);
                        PokerTotalCash.Show("PokerTotalCash");
                    }
                }
                else
                {
                    if (this.gameObject.name != LocalSettings.AI_Name)
                        PokerTotalCash = LocalSettings.GetTotalChips();
                    else
                        PokerTotalCash = LocalSettings.AI_Amount;
                }

                PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);// Mohsin
                // PokerTotalCashTxt.text.ShowBlue();
                // startCash = PokerTotalCash;
                // Debug.LogError("Here is you Bug     Buyin chios: " + LocalSettings.GetPokerBuyInChips());
                if (!MatchHandler.isOffline())
                {
                    if (IsMine())
                    {
                        LocalSettings.SetPosAndRect(PokerTotalCashTxt.transform.parent.gameObject, isMinePokerTotalCashTranform, isMinePokerTotalCashTranform.parent);
                        //  player_name.transform.parent.gameObject.SetActive(false);
                    }
                }
                else
                {
                    LocalSettings.SetPosAndRect(PokerTotalCashTxt.transform.parent.gameObject, isMinePokerTotalCashTranform, isMinePokerTotalCashTranform.parent);
                }

                PokerTotalCashTxt.transform.parent.gameObject.SetActive(true);


            }



            // On reconnect the local RoomStateManager.CurrentRoomState is the default
            // (WaitingForPlayers) because it is a plain field, not a SyncVar.
            // Restore it from the SyncVar so IsStarted() returns the correct value.
            // PlayerInfo is now server-owned and survives disconnect, so the SyncDict
            // data (hole cards) is intact — no persistent fallback needed.
            bool pokerReconnectIntoLiveHand = false;
            if (!MatchHandler.isOffline() && IsMine() && MatchHandler.IsPoker())
            {
                RoomState.STATE syncedState = (RoomState.STATE)NetworkGameManager.Instance.syncedPokerRoomState;

                // NetworkGameManager's SyncVars ride a DIFFERENT object's spawn payload than this
                // player's own SyncDictionary, and they can land a frame later. A client that
                // reconnected mid-hand read syncedPokerRoomState as WaitingForPlayers here even
                // though its own seat still held hole cards and a placed bet: the gate below failed,
                // IsStarted() came out false, and the else-branch fired UpdateCurrentPlayerState
                // (AbleToJoin) as a Command, overwriting the authoritative live-hand state on the
                // server. The opponent's reconnect restore then dropped that seat from PlayingList,
                // its own ExecutingTurn found PlayingList.Count == 1, the action panel was never
                // shown and the hand sat there forever. This player's own state DID arrive with its
                // own object, so trust that rather than waiting on the other object's SyncVars.
                if (syncedState == RoomState.STATE.WaitingForPlayers && ServerHasMeInALiveHand())
                    syncedState = RoomState.STATE.GameIsPlaying;

                bool hasCardData = playerCustomProperties.GetCustomData(LocalSettings.pokerHoleCard1ForPlayer) != 0
                                || playerCustomProperties.GetCustomData(LocalSettings.pokerHoleCard2ForPlayer) != 0;

                if (syncedState != RoomState.STATE.WaitingForPlayers && hasCardData)
                {
                    RoomStateManager.Instance.UpdateLocalRoomState(syncedState);

                    // Only skip the OutOfGame Command below when RestorePokerStateOnReconnect is
                    // guaranteed to run and reapply the real state - these are exactly the
                    // conditions it checks. If the two ever disagreed, the server and the opponent
                    // would keep my ExecutingTurn while my own client sat in spectator mode with no
                    // buttons, which is the same deadlock from the other side.
                    bool gameInProgress = syncedState == RoomState.STATE.GameIsPlaying
                                       || syncedState == RoomState.STATE.ABFirstTurn
                                       || syncedState == RoomState.STATE.ABSecondTurn
                                       || syncedState == RoomState.STATE.WaitingForResults
                                       || syncedState == RoomState.STATE.ShowingResults
                                       || syncedState == RoomState.STATE.CardDistributing;
                    pokerReconnectIntoLiveHand = gameInProgress && NetworkGameManager.Instance.syncedPokerGameStarted;
                }
            }

            if (RoomStateManager.Instance.IsStarted())
            {
                //Debug.LogError(currentPlayerStateRef.currentState);
                if (!MatchHandler.isOffline())
                {
                    if (IsMine())
                    {
                        if (pokerReconnectIntoLiveHand)
                        {
                            // Reconnecting into a hand I still hold cards for: my authoritative
                            // state on the server is the real one (ExecutingTurn / WaitingForTurn /
                            // BetPlaced ...). Sending the OutOfGame Command here overwrote it and
                            // deleted my turn - RestorePokerStateOnReconnect then read OutOfGame
                            // back out of the SyncDictionary, left me out of PlayingList, and the
                            // opponent's PlayerTurnManager saw PlayingList.Count <= 1 and stopped
                            // its timer, so both clients froze with no action buttons. Show the
                            // spectator state locally only (no Command) and let
                            // RestorePokerStateOnReconnect reapply the real state.
                            currentPlayerStateRef.UpdateCurrentPlayerStateAI(PlayerState.STATE.OutOfGame);
                        }
                        else
                        {
                            currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.OutOfGame);
                        }
                    }
                    else
                    {
                        if (MatchHandler.IsPoker())
                        {
                            currentPlayerStateRef.currentState = (playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState));
                            //Give original Cards to all player
                            StartCoroutine(GiveOrgPokersCardsIfNotInGame(0.5f));
                        }

                    }
                }
                else
                {
                    currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.OutOfGame);
                }

            }
            else
            {



                if (MatchHandler.IsPoker())
                {
                    // Belt and braces for the same overwrite described above: whatever the local room
                    // state says, a client whose seat the SERVER still has in a live hand must never
                    // send AbleToJoin - that Command is what deleted the hand. Show the spectator
                    // state locally only and let RestorePokerStateOnReconnect reapply the real one.
                    if (IsMine() && !MatchHandler.isOffline() && ServerHasMeInALiveHand())
                        currentPlayerStateRef.UpdateCurrentPlayerStateAI(PlayerState.STATE.OutOfGame);
                    // Server must also set AbleToJoin directly; IsMine() is always false
                    // on the dedicated server (user._id=0), so the old client-Command path
                    // never populated PlayingList on the server → DealerToNext() crashed.
                    else if (IsMine() || NetworkServer.active)
                        currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.AbleToJoin);
                }
                else
                {
                    currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.AbleToJoin);
                }



            }
            if (!MatchHandler.isOffline())
            {
                if (playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState) == PlayerState.STATE.OutOfTable && !IsMine())
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                if (currentPlayerStateRef.currentState == PlayerState.STATE.OutOfTable)
                {
                    gameObject.SetActive(false);
                }
            }


        }




        public void SetRoomStateFromNetworkCustomProperty()
        {
            // PUN room property no longer needed — room state is synced via
            // syncedPokerRoomState SyncVar + event system.
        }
        IEnumerator GiveOrgCardsIfNotInGame(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            //Debug.LogError("Aya k ahi");
            if (playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState) != PlayerState.STATE.OutOfGame)
            {
                GameManager gameManager = GameManager.Instance;
                int[] OrgCardsAry = new int[3]; // Initialize OrgCardsAry with a size of 3
                OrgCardsAry[0] = 0; // Assign initial values to array elements if needed
                OrgCardsAry[1] = 0;
                OrgCardsAry[2] = 0;
                //Debug.LogError("Original Card " + player.GetCustomArray(LocalSettings.OrgCardsArray).Length + " NickName is " + player.NickName);

                //Debug.LogError("Original Card " + OrgCardsAry.Length);
                yield return new WaitUntil(() => playerCustomProperties.GetCustomArray(LocalSettings.OrgCardsArray).Length > 0);
                OrgCardsAry = playerCustomProperties.GetCustomArray(LocalSettings.OrgCardsArray);
                if (OrgCardsAry.Length > 0)
                {
                    for (int i = 0; i < OrgCardsAry.Length; i++)
                    {
                        GameObject card = Instantiate(gameManager.AllCards.Card[OrgCardsAry[i]].gameObject);
                        RectTransform rt = PlayerOrignalCardsToShowParent.transform.GetChild(i).gameObject.GetComponent<RectTransform>();
                        Transform parntObj = PlayerOrignalCardsToShowParent.transform;
                        LocalSettings.SetPosAndRect(card, rt, parntObj.transform);
                    }
                }
            }
        }



        void PlayerSeenCardsStatus()
        {
            // Debug: print(ownerPlayerId + " : " + playerCustomProperties.GetCustomBoolData("is_seen"));
            if (RoomStateManager.Instance.GetCurrentRoomState() == RoomState.STATE.GameIsPlaying)
            {
                if (!IsMine() && currentPlayerStateRef.currentState != PlayerState.STATE.OutOfGame)
                {
                    IsSeen = playerCustomProperties.GetCustomBoolData("is_seen");
                    BlindIndicator.SetActive(!IsSeen);
                    SeenIndicator.SetActive(IsSeen);
                }
            }
        }

        //public int counter;
        void Update()
        {
            // Old PUN guard removed — Update() body is empty debug code.
            //if (Input.GetKeyDown(KeyCode.UpArrow))
            //{
            //    counter++;
            //    GameManager.Instance.TurnTxt.text = counter.ToString();
            //    //photonView.RPC(counter.ToString(), photonView,"asdf");
            //    photonView.RPC("increment", RpcTarget.All, counter);
            //}

        }

        //[PunRPC]
        //public void increment(int counter2)
        //{
        //    counter = counter2;
        //    GameManager.Instance.TurnTxt.text = counter.ToString();
        //}


        public void AssignNetworkSeat(int seat)
        {
            //    if (!matchhandler.isoffline())
            //        this_photonview.rpc("assignnetworkseattoallinstanceofthisplayer", rpctarget.allbuffered, seat);
            //    else
            //        assignnetworkseattoallinstanceofthisplayer(seat);
        }

        [ClientRpc]
        public void AssignNetworkSeatToAllInstanceOfThisPlayer(int seat)
        {
            myNetworkSeat = seat;
            AdjustGameManagerLocalSeat = seat;
        }


        public void ReAssignNetworkSeat()
        {
            int seat = playerCustomProperties.GetCustomData(LocalSettings.networkPosition);
            if (NetworkServer.active)
            {
                // Same reason as CmdReAssignNetworkSeat: ReAssignNetworkSeatToAllInstanceOfThisPlayer
                // is a [ClientRpc], so its body never runs on a dedicated server and myNetworkSeat
                // would stay stale - OnDisable would then release somebody else's seat.
                myNetworkSeat = seat;
                ReAssignNetworkSeatToAllInstanceOfThisPlayer(seat);
            }
            else
                CmdReAssignNetworkSeat(seat);
        }

        [ClientRpc]
        public void ReAssignNetworkSeatToAllInstanceOfThisPlayer(int seat)
        {
            myNetworkSeat = seat;
            AdjustGameManagerLocalSeat = seat;
        }



        public void StandUp()
        {
            if (this.gameObject.name != LocalSettings.AI_Name)
            {
                PlayerStateManager.Instance.taptoSitHere.SetActive(true);

                PlayerStateManager.Instance.waitForNextRound.SetActive(false);
                PlayerStateManager.Instance.Amountobject.SetActive(false);
            }

            if (getCurrentPlayerState().currentState == PlayerState.STATE.OutOfTable)
                return;

            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                //Debug.LogError("Check player Status about Player Standup.....1");
                if (getCurrentPlayerState().currentState == PlayerState.STATE.OutOfTable)
                {
                    return;
                }
                //else
                //{
                //    if (currentPlayerStateRef.currentState != PlayerState.STATE.Packed)
                //    {
                //        currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                //        // Invoke(nameof(Game_Play.Instance.StandUp), 1f);
                //        Debug.LogError("ata hai ya nahi");
                //        return;
                //    }
                //}
            }

            //GoldTransfer.Instance.showMessage("You have stand up from the table");

            if (this.gameObject.name != LocalSettings.AI_Name)
                GameManager.Instance.position_availability[0].is_reserved = null;
            else
                GameManager.Instance.position_availability[1].is_reserved = null;
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    PositionsManager.Instance.ReleasePosition(GameManager.Instance.myLocalSeat);
            }
            else
            {
                if (this.gameObject.name != LocalSettings.AI_Name)
                    PositionsManager.Instance.ReleasePosition(GameManager.Instance.myLocalSeat);
                else
                    PositionsManager.Instance.ReleasePosition(GameManager.Instance.AILocalSeat);
            }
            currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.OutOfTable);
            GameManager.Instance.SitHereBtnStatus(true);
        }




        [ClientRpc]
        public void SetgiveTurnToNextBool()
        {
            getCurrentPlayerState().giveTurnToNext = false;
        }

        public void ActivatePlayerAgainOnNetwork()
        {

            playerEnterenceState();

            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    // A [ClientRpc] body never executes on a dedicated server, so the server's
                    // own PlayerInfo object stayed disabled after a stand up / pack. OnEnable
                    // never fired -> AddOrRemovePlayer(+1) never ran -> the server's
                    // _currentNumberOfPlayers stuck below the minimum -> GameStarted() was never
                    // reached and the room froze in WaitingForPlayers forever (clients showed
                    // 2 players and a finished countdown). Activate locally first, then broadcast.
                    ActivatePlayerAgainLocal();
                    ActivatePlayerAgain();
                }
                else
                    CmdActivatePlayerAgain();
            }
            else
                ActivatePlayerAgainAI();
        }

        [ClientRpc]
        public void ActivatePlayerAgain()
        {
            ActivatePlayerAgainLocal();
        }
        public void ActivatePlayerAgainAI()
        {
            ActivatePlayerAgainLocal();
        }

        // Plain local method - runs on whichever machine calls it, dedicated server included.
        void ActivatePlayerAgainLocal()
        {

            gameObject.SetActive(true);
            PackedText.gameObject.SetActive(false);

            if (getCurrentPlayerState().currentState != PlayerState.STATE.ExecutingTurn && getCurrentPlayerState().currentState != PlayerState.STATE.WaitingForTurn)
            {
                PlayerOrignalCardsToShowParent.gameObject.SetActive(false);
            }

        }



        public PlayerCurrentState getCurrentPlayerState()
        {
            return currentPlayerStateRef;
        }


        public void AssignName(string name)
        {
            player_name.text = name;
            gameObject.name = name;
        }


        private void UpdateProfilePic()
        {
            if (IsMine())
            {
                //   Debug.Log(player.name + " With Actor " + player.name + " is UpdatingProfilePic");
                playerCustomProperties.SetCustomData(LocalSettings.ProfilePic, LocalSettings.GetprofilePic());
                playerCustomProperties.SetCustomData(LocalSettings.ProfileFrame, LocalSettings.GetprofileFrame());
            }
        }


        public void ForAllShowTipToGirl(int dialogueNumber)
        {

            GameManager.Instance.PlayerTotalChipsUpdate(-Pot.instance.potTip);
            playerCustomProperties.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());


            if (NetworkServer.active)
                TipToGirl(dialogueNumber);
            else
                CmdTipToGirl(dialogueNumber);

        }



        [ShowOnly]
        public float timeDelay = 1.5f;
        [ClientRpc]
        public void TipToGirl(int dialogueNumber)
        {
            Pot potInstance = Pot.instance;

            GameObject tipCollect = Instantiate(tipAmountAnim, this.transform);
            tipCollect.GetComponent<BetAmountToTargetAnim>().targetPos = potInstance.targeToTipGirl;
            tipCollect.SetActive(false);
            tipCollect.GetComponentInChildren<TMP_Text>().text = LocalSettings.Rs(potInstance.potTip);
            //potInstance.tipFromPlayerNameText.text = "";
            //potInstance.tipFromPlayerNameText.text = "Thanks you, " + player_name.text + ". May the good hands be with you!";


            if (!potInstance.tipFromPlayerNameText.transform.parent.gameObject.activeSelf)
            {
                potInstance.tipFromPlayerNameText.text = "Thanks you, " + player_name.text + ". " + tipsDialouges[dialogueNumber] + "!";
                potInstance.NextTipOfPlayer(timeDelay, this);
            }




            potInstance.tipDialogueTextObject.Add("Thanks you, " + player_name.text + ". " + tipsDialouges[dialogueNumber] + "!");
            potInstance.tipFromPlayerNameText.transform.parent.gameObject.SetActive(true);

            //GameManager.Instance.PlayerTotalChipsUpdate(-potInstance.potTip);





            MyTotalCashTextUpdate();
            tipCollect.SetActive(true);

            Destroy(tipCollect, 4f);

        }




        bool isShowAllCards;




        [ClientRpc]
        public void GiveChaalAmountOnNetoworkAtStart()
        {

            Game_Play.Instance.ShowInfo(LocalSettings.textStringOnStartBetAmount, 1f);
            BetAmountAnimText.text = LocalSettings.Rs(Pot.instance.CurrentChalAmount);
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);

            if (currentPlayerStateRef.currentState != PlayerState.STATE.OutOfTable && currentPlayerStateRef.currentState != PlayerState.STATE.OutOfGame)
            {
                if (IsMine())
                {
                    if (LocalSettings.GetTotalChips() >= Pot.instance.startPotAmount)
                    {
                        GameManager.Instance.PlayerTotalChipsUpdate(-Pot.instance.startPotAmount);

                        UIManager.Instance.TotalBetPlacedAmount += Pot.instance.startPotAmount;
                    }
                    else
                    {
                        StandUp();
                        UIManager.Instance.quickShop.SetActive(true);
                        ConstantsData_M.Log(PlayerPrefs.GetString("TotalChips") + "6");
                    }

                }


                //Debug.LogError("starting Amount  :   - " + Pot.instance.startPotAmount);
            }



            BetAmountAnim.GetComponent<BetAmountToTargetAnim>().targetPos = Pot.instance.PotPanel.gameObject;
            BetAmountAnim.SetActive(true);
        }

        public void GiveChaalAmountOnGameStart()
        {
            //Debug.Log("Start Pot Amount Deduct" + -Pot.instance.startPotAmount);
            if (NetworkServer.active)
                GiveChaalAmountOnNetoworkAtStart();
            else
                CmdGiveChaalAmountOnGameStart();
        }



        /// <summary>
        /// Player cards indexes sending and receiving
        /// </summary>
        /// <param name="ModifiedChalAmount"></param>
        #region Sending And Getting Player cards array
        public void SendPlayerCardsArray(int[] RandomCardsArray)
        {
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    UpDatePlayerCardsArray(RandomCardsArray);
                else
                    CmdSendPlayerCardsArray(RandomCardsArray);
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSendPlayerCardsArray(int[] RandomCardsArray)
        {
            UpDatePlayerCardsArray(RandomCardsArray);
        }

        #region Command Bridges for //workingrpc conversions
        // These Commands bridge client calls to existing [ClientRpc] methods
        // since PlayerInfo is server-owned (no client authority).

        [Command(requiresAuthority = false)]
        public void CmdReAssignNetworkSeat(int seat)
        {
            // ClientRpc bodies do not set this runtime field on a dedicated server.
            // OnDisable uses it to release the player's current seat.
            myNetworkSeat = seat;
            ReAssignNetworkSeatToAllInstanceOfThisPlayer(seat);
        }

        // Chips are already deducted locally by PokerActionPanel before SendPokerBet is called, and
        // SendPokerBetRPC never deducts any - it only adds to the pot / totals and updates the
        // labels - so routing the bet through the server cannot double-charge the player.
        [Command(requiresAuthority = false)]
        public void CmdSendPokerBet(string pokerCurrentBetAmountString, bool isAllIn)
        {
            ServerAccumulatePokerBet(LocalSettings.StringToBigInteger(pokerCurrentBetAmountString));
            SendPokerBetRPC(pokerCurrentBetAmountString, isAllIn);
        }

        // Every bet reaches a client as a [ClientRpc], and pokerTotalBetCash /
        // PokerTotalWholeBetAmount / PokerActionPanel.amountPlacedOnBet are all plain local
        // accumulators - so a client that was away for part of the hand never saw the money wagered
        // while it was gone, and came back to a pot of 0 and a wrong call amount. Mirror both totals
        // as SyncVars: syncedPokerHandBet is the whole hand (the pot), syncedPokerRoundBet the
        // current betting round (the call target).
        [SyncVar] public string syncedPokerHandBet = "0";
        [SyncVar] public string syncedPokerRoundBet = "0";

        [Server]
        public void ServerAccumulatePokerBet(BigInteger amount)
        {
            if (amount <= 0) return;
            syncedPokerHandBet = LocalSettings.BigIntegerToString(
                LocalSettings.StringToBigInteger(syncedPokerHandBet) + amount);
            syncedPokerRoundBet = LocalSettings.BigIntegerToString(
                LocalSettings.StringToBigInteger(syncedPokerRoundBet) + amount);
        }

        // Re-create the chip stack that belongs in front of this seat. In a live hand that visual is
        // built inside SendPokerBetRPC, one prefab per bet - and a reconnecting client missed every
        // one of those RPCs, so its table showed nobody's money at all. One stack for the whole
        // round bet is the best that can be rebuilt: the per-bet breakdown went with the RPCs, only
        // the total survived on the server.
        public void RestorePokerBetChipVisual(BigInteger amount)
        {
            if (amount <= 0 || PokerBetAmountAnimPrefab == null) return;
            if (pokerBetAmountAnim != null) return;

            GameObject betAnimation = Instantiate(PokerBetAmountAnimPrefab);
            betAnimation.SetActive(true);
            LocalSettings.SetPosAndRect(betAnimation, PokerBetAmountAnimPrefab.GetComponent<RectTransform>(), PokerBetAmountAnimPrefab.transform.parent);
            pokerBetAmountAnim = betAnimation;
            betAnimation.GetComponent<PokerBetAmountAnim>().PlayAnimation(FirstTargetPokerBetAmount, false, amount);
        }

        // See PokerManager.ReportDropCardsIndexToServer for why the server cannot work out how many
        // community cards are face up by itself.
        [Command(requiresAuthority = false)]
        public void CmdSetPokerDropCardsIndex(int index, string communityCardsCsv)
        {
            if (NetworkGameManager.Instance == null) return;
            if (index <= NetworkGameManager.Instance.syncedDropCardsIndex) return;
            // Only accept a report stamped with the hand that is actually on the table. Without this
            // a reveal reported just as a hand ends could land after the reset and make the next
            // hand's reconnect show a flop that has not been dealt yet.
            if (string.IsNullOrEmpty(communityCardsCsv)
                || communityCardsCsv != NetworkGameManager.Instance.syncedCommunityCards) return;

            NetworkGameManager.Instance.syncedDropCardsIndex = index;

            // A new community card also opens a new betting round - the clients zero
            // pokerTotalBetCash at exactly this point (BetsGoToFinalPoints) - so the per-round
            // totals the call amount is rebuilt from have to be zeroed here too, or a reconnecting
            // player would come back holding last round's call target.
            if (GameManager.Instance != null)
                foreach (PlayerInfo p in GameManager.Instance.playersList)
                    if (p != null) p.syncedPokerRoundBet = "0";
        }

        [Command(requiresAuthority = false)]
        public void CmdActivatePlayerAgain() { ActivatePlayerAgainLocal(); ActivatePlayerAgain(); }

        [Command(requiresAuthority = false)]
        public void CmdTipToGirl(int dialogueNumber) { TipToGirl(dialogueNumber); }

        [Command(requiresAuthority = false)]
        public void CmdGiveChaalAmountOnGameStart() { GiveChaalAmountOnNetoworkAtStart(); }

        [Command(requiresAuthority = false)]
        public void CmdSeenAlert(bool seen) { SeenAlert(seen); }

        [Command(requiresAuthority = false)]
        public void CmdSideShowAlert(bool sideShow) { SideShowAlert(sideShow); }

        [Command(requiresAuthority = false)]
        public void CmdSideShowPanelsFalse(bool sideShow) { SideShowPanelsFalse(sideShow); }

        [Command(requiresAuthority = false)]
        public void CmdUpdateAllPlayersCurrentChallAmount(string amount) { UpdateAllPlayersCurrentChallAmount(amount); }

        [Command(requiresAuthority = false)]
        public void CmdSendGoldTransferAndUpdateChips() { sendGoldTranferAndUpdateChipsRPC(); }

        [Command(requiresAuthority = false)]
        public void CmdCashCheckOfAllPlayers() { cashCheckOfAllPlayers(); }

        [Command(requiresAuthority = false)]
        public void CmdSetCircleFlagPoker() { SetCircleFlagPokerRPC(); }

        [Command(requiresAuthority = false)]
        public void CmdPlayerRunInBackGround(bool isTrue) { playerRunInBackGroundRPC(isTrue); }

        [Command(requiresAuthority = false)]
        void CmdSuperBaharBetOnNetwork(string amount) { SuperBaharBetOnNetwork(amount); }

        #endregion

        [ClientRpc]
        public void UpDatePlayerCardsArray(int[] PlayerCardsArray)
        {
            OrignalCardsSetting(PlayerCardsArray);
        }

        public void OrignalCardsSetting(int[] cardsArray)
        {
            int CardPosNumber;
            GameManager gameManager = GameManager.Instance;

            PlayerStateManager psm = PlayerStateManager.Instance;
            GameObject card = null;
            RectTransform rt;
            Transform parntObj;
            int PlayerNumber = 0;
            for (int i = 0; i < psm.PlayingList.Count; i++)
            {
                if (psm.PlayingList[i] == this)
                {
                    PlayerNumber = i; break;
                }
            }

            for (int j = 0; j < psm.PlayingList.Count; j++)
            {
                card = Instantiate(gameManager.AllCards.Card[cardsArray[(j * 3) + 0]].gameObject);
                rt = psm.PlayingList[j].PlayerOrignalCardsToShowParent.transform.GetChild(0).gameObject.GetComponent<RectTransform>();
                parntObj = psm.PlayingList[j].PlayerOrignalCardsToShowParent.transform;
                LocalSettings.SetPosAndRect(card, rt, parntObj.transform);

                card = Instantiate(gameManager.AllCards.Card[cardsArray[(j * 3) + 1]].gameObject);
                rt = psm.PlayingList[j].PlayerOrignalCardsToShowParent.transform.GetChild(1).gameObject.GetComponent<RectTransform>();
                parntObj = psm.PlayingList[j].PlayerOrignalCardsToShowParent.transform;
                LocalSettings.SetPosAndRect(card, rt, parntObj.transform);

                card = Instantiate(gameManager.AllCards.Card[cardsArray[(j * 3) + 2]].gameObject);
                rt = psm.PlayingList[j].PlayerOrignalCardsToShowParent.transform.GetChild(2).gameObject.GetComponent<RectTransform>();
                parntObj = psm.PlayingList[j].PlayerOrignalCardsToShowParent.transform;
                LocalSettings.SetPosAndRect(card, rt, parntObj.transform);

            }


            if (NetworkServer.active)
            {
                int[] orgCardsAry = new int[3];

                for (int i = 0; i < PlayerStateManager.Instance.PlayingList.Count; i++)
                {
                    orgCardsAry[0] = psm.PlayingList[i].PlayerOrignalCardsToShowParent.transform.GetChild(3).gameObject.GetComponent<CardProperty>().CardIndexInArray;
                    orgCardsAry[1] = psm.PlayingList[i].PlayerOrignalCardsToShowParent.transform.GetChild(4).gameObject.GetComponent<CardProperty>().CardIndexInArray;
                    orgCardsAry[2] = psm.PlayingList[i].PlayerOrignalCardsToShowParent.transform.GetChild(5).gameObject.GetComponent<CardProperty>().CardIndexInArray;
                    psm.PlayingList[i].playerCustomProperties.SetCustomArray(LocalSettings.OrgCardsArray, orgCardsAry);
                    string koibhi = "";
                    for (int j = 0; j < orgCardsAry.Length; j++)
                    {
                        koibhi += orgCardsAry[j] + ", ";
                    }
                    // Debug.LogError("Player Orignal Cards : " + koibhi);
                }
            }


            //ShowCardsFromBlind();
        }

        public void ShowCardsFromBlind()
        {
            PlayerDummyCardsToShowParent.gameObject.SetActive(false);
            PlayerOrignalCardsToShowParent.SetActive(true);
            for (int i = 0; i < PlayerOrignalCardsToShowParent.transform.childCount; i++)
            {
                if (!PlayerOrignalCardsToShowParent.transform.GetChild(i).gameObject.activeInHierarchy)
                    PlayerOrignalCardsToShowParent.transform.GetChild(i).gameObject.SetActive(true);

                SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.CardFlip, false);
            }
            IsSeen = true;
            playerCustomProperties.SetCustomBoolData("is_seen", true);
            UIManager.Instance.ChaalTypeText.text = "Chaal";
            if (NetworkServer.active)
                SeenAlert(IsSeen);
            else
                CmdSeenAlert(IsSeen);
            if (IsMine())
            {
                PlayerStateManager.Instance.SideShowAndShowbtn();
            }

        }

        public void GiveSeenAlertToAll()
        {
            if (NetworkServer.active)
                SeenAlert(IsSeen);
            else
                CmdSeenAlert(IsSeen);
        }


        [ClientRpc]
        public void SeenAlert(bool seen)
        {
            if (seen)
            {
                IsSeen = seen;
                BlindIndicator.SetActive(false);
                SeenIndicator.SetActive(true);
                if (Pot.instance.ChaalAmountLimit() != Pot.instance.CurrentChalAmount && IsMine())
                    UIManager.Instance.UpDateCurrentChalAmountText(Pot.instance.CurrentChalAmount * 2);
            }
            if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
            {
                int PrevPlayer = PlayerStateManager.Instance.SideShowPrev();
                // Debug.LogError("Player Current state:-----  " + PrevPlayer);
                if (PrevPlayer >= 0)
                {
                    if (PlayerStateManager.Instance.PlayingList[PrevPlayer].IsSeen)
                    {
                        if (UIManager.Instance.GetMyPlayerCurrentState().currentState == PlayerState.STATE.ExecutingTurn)
                        {
                            UIManager.Instance.sideShowBtn.interactable = true;
                        }
                    }
                }
            }
        }

        public void GivesideShowAlertToAll(bool sideShow)
        {
            IsSideShow = sideShow;
            if (NetworkServer.active)
                SideShowAlert(IsSideShow);
            else
                CmdSideShowAlert(IsSideShow);
        }


        [ClientRpc]
        public void SideShowAlert(bool setbool)
        {
            IsSideShow = setbool;
            foreach (var item in PlayerStateManager.Instance.PlayingList)
            {
                item.IsSideShow = setbool;
                if (!setbool)
                    item.SideShowIndicatorAnim.SetActive(setbool);
            }
        }
        public void AllSideShowPanelsFalse(bool sideShow)
        {
            if (NetworkServer.active)
                SideShowPanelsFalse(sideShow);
            else
                CmdSideShowPanelsFalse(sideShow);
        }


        [ClientRpc]
        public void SideShowPanelsFalse(bool setbool)
        {

            UIManager.Instance.sideShowPanel.SetActive(setbool);
        }

        #endregion

        public void CurrentChalAmountSendToAllPlayers(BigInteger ModifiedChalAmount)
        {
            if (IsMine())
            {
                Pot.instance.CurrentChalAmount = ModifiedChalAmount;
                if (NetworkServer.active)
                    UpdateAllPlayersCurrentChallAmount(Pot.instance.CurrentChalAmount.ToString());
                else
                    CmdUpdateAllPlayersCurrentChallAmount(Pot.instance.CurrentChalAmount.ToString());
            }
        }
        [ClientRpc]
        public void UpdateAllPlayersCurrentChallAmount(string ChalAmountUpdatedString)
        {
            BigInteger ChalAmountUpdated = BigInteger.Parse(ChalAmountUpdatedString);
            Pot.instance.CurrentChalAmount = ChalAmountUpdated;
            BigInteger betAmount = Pot.instance.CurrentChalAmount;
            if (IsSeen)
                betAmount = Pot.instance.CurrentChalAmount * 2;
            UIManager.Instance.UpDateCurrentChalAmountText(betAmount);
        }

        //public void AddToPotForNewPlayer(int amount)
        //{
        //    if (photonView.IsMine)
        //    {
        //        Pot.instance.potSize += amount;
        //        photonView.RPC("UpdatePotSizeForNewPlayer", RpcTarget.All, Pot.instance.potSize);
        //    }
        //}
        //[PunRPC]
        //public void UpdatePotSizeForNewPlayer(int newPotSize)
        //{

        //    Pot.instance.potSize = newPotSize;
        //    Pot.instance.PotTxt.text = "Rs." + Pot.instance.potSize;



        //    //Debug.Log("New pot size: " + Pot.instance.potSize);
        //}







        public void GetProfilePic()
        {
            StartCoroutine(waitforLoadPlayerAvatar());
        }

        IEnumerator waitforLoadPlayerAvatar()
        {
            yield return new WaitForSeconds(0.3f);
            if (!MatchHandler.isOffline())
            {
                if (IsMine())
                {
                    if (staticVariables.ProfilePicture != null)
                        PlayerAvatorImage.sprite = Sprite.Create(staticVariables.ProfilePicture, new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height), UnityEngine.Vector2.zero);// GameManager.Instance.PlayerProfileImages.Sprites[index];
                    else
                        PlayerAvatorImage.sprite = GameManager.Instance.PlayerProfileImages.Sprites[UnityEngine.Random.Range(0, GameManager.Instance.PlayerProfileImages.Sprites.Length)];
                }
                else
                {
                    if (staticVariables.opponentImage != null)
                        PlayerAvatorImage.sprite = Sprite.Create(staticVariables.opponentImage, new Rect(0, 0, staticVariables.opponentImage.width, staticVariables.opponentImage.height), UnityEngine.Vector2.zero);// GameManager.Instance.PlayerProfileImages.Sprites[index];
                    else
                        PlayerAvatorImage.sprite = GameManager.Instance.PlayerProfileImages.Sprites[UnityEngine.Random.Range(0, GameManager.Instance.PlayerProfileImages.Sprites.Length)];
                }
            }
            else
            {

                PlayerAvatorImage.sprite = this.gameObject.name != LocalSettings.AI_Name ? ConstantsData_M.ConvertTextureToSprite(staticVariables.ProfilePicture) : GameManager.Instance.PlayerProfileImages.Sprites[UnityEngine.Random.Range(0, GameManager.Instance.PlayerProfileImages.Sprites.Length)];
            }
        }


        public void UpdateScore()
        {
            if (IsMine())
                playerCustomProperties.SetCustomData(LocalSettings.Score, UnityEngine.Random.Range(0, 100));
        }

        public void GetScore()
        {
            PicIndexTxt.text = playerCustomProperties.GetCustomData(LocalSettings.Score).ToString();
        }

        public void IAmWinner(bool winner)
        {
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    RPCWinner(winner);
                    if (winner) ServerStartSettleWatch();
                }
                else
                    CmdIAmWinner(winner);
            }
            else
            {
                AIWinner(winner);
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdIAmWinner(bool winner)
        {
            // Server-side validation: each client independently determines the winner,
            // but if hand ranks aren't fully synced, each client may claim itself as winner.
            // The server has both players' ranks (stored in CmdUpdateHandRankLabel) and
            // rejects claims from players who don't actually have the best hand.
            if (winner)
            {
                var psm = PlayerStateManager.Instance;
                if (psm != null && psm.PlayingList != null)
                {
                    foreach (var other in psm.PlayingList)
                    {
                        if (other == null || other == this) continue;
                        // If another player has a strictly better rank, reject this claim
                        if (other.PokerPlayerCurrentRank > this.PokerPlayerCurrentRank)
                            return;
                        // Same rank but strictly better score — reject
                        if (other.PokerPlayerCurrentRank == this.PokerPlayerCurrentRank
                            && other.PokerScores > this.PokerScores)
                            return;
                    }
                }
            }
            RPCWinner(winner);
            if (winner) ServerStartSettleWatch();
        }

        // Arms the settle fallback on the server. The payload is built from the same synced
        // properties the winner's own client uses, so the surviving client can post an identical
        // request if the winner never does. Nothing is sent from here - see
        // PlayerTurnManager.ServerWatchRoundSettle for why the fallback only fires once the winner
        // is provably disconnected.
        [Server]
        void ServerStartSettleWatch()
        {
            if (MatchHandler.isOffline() || !MatchHandler.IsPoker()) return;
            if (PlayerTurnManager.Instance == null || GameManager.Instance == null) return;
            if (string.IsNullOrEmpty(staticVariables.CurrentRoundID)) return;

            PlayerInfo firstplayer = GameManager.Instance.playersList.Count > 0 ? GameManager.Instance.playersList[0] : null;
            PlayerInfo secondPlayer = GameManager.Instance.playersList.Count > 1 ? GameManager.Instance.playersList[1] : null;

            PlayerTurnManager.Instance.ServerWatchRoundSettle(
                staticVariables.CurrentRoundID,
                ownerPlayerId,
                PokerStakeOfThisHand(firstplayer).ToString(),
                PokerStakeOfThisHand(secondPlayer).ToString());
        }

        public void PlayerPacked()
        {
            currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.Packed);
        }

        public void UpdatePlayerState(PlayerState.STATE state)
        {
            currentPlayerStateRef.UpdateCurrentPlayerState(state);
        }

        bool isWinner
        {
            get { return isWinner; }
            set
            {
                if (value == true)
                {

                    if (!GameManager.Instance.isRunINBackGround || !MatchHandler.IsPoker())
                    {
                        // Toaster.ShowAToast("Winner Indicator..." + GameManager.Instance.isRunINBackGround);
                        WinningIndicator.SetActive(true);
                        Pot pot = Pot.instance;
                        if (MatchHandler.IsPoker() || MatchHandler.isOffline())
                        {
                            pot.PotPanel.SetActive(false);
                            pot.potLimitTxt.text = "";
                            pot.winAmountText.text = LocalSettings.Rs(PokerActionPanel.Instance.TotalPotAmount());
                            LocalSettings.SetPosAndRect(pot.winAmountAnim, PokerManager.Instance.FinalPokerBetAmountPoint.GetComponent<RectTransform>(), pot.winAmountAnim.transform.parent);


                            foreach (var item in GameManager.Instance.playersList)
                            {
                                if (item.pokerBetAmountAnim != null)
                                    Destroy(item.pokerBetAmountAnim);
                            }
                            //Debug.LogError("Check here...." + LocalSettings.Rs(PokerActionPanel.Instance.TotalPotAmount()));

                        }
                        //Debug.LogError("Check here...." + LocalSettings.Rs(PokerActionPanel.Instance.TotalPotAmount()));
                        pot.winAmountAnim.GetComponent<BetAmountToTargetAnim>().targetPos = WinningIndicator;
                        pot.winAmountAnim.SetActive(true);

                        if (!MatchHandler.isOffline())
                        {
                            if (IsMine())
                            {
                                SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                                SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                            }
                        }
                        else
                        {
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.WinFinal, false);
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipsCollect, false);
                        }
                    }
                }
                else
                {
                    WinningIndicator.SetActive(false);
                }

            }

        }

        [ClientRpc]
        public void RPCWinner(bool net_winner)
        {
            // Guard MUST be checked BEFORE setting isWinner, because the isWinner property
            // setter triggers WinningIndicator.SetActive(true). Without this ordering,
            // a second RPCWinner (from the other client's IAmWinner call) would show the
            // WinningIndicator before the guard could block it.
            if (staticVariables.isPokerFinished)
            {
                return;
            }
            isWinner = net_winner;
            gameObject.name.Show("Object Name");
            pokerTotalBetCash.Show("pokerTotalBetCash");
            PokerTotalWholeBetAmount.Show("PokerTotalWholeBetAmount");
            Pot.instance.potSize.Show("potSize");
            //Constants_M.Log(staticVariables.UserProfiledata.user.silver_balance);
            PlayerInfo firstplayer;
            Dictionary<string, string> data;
            if (!net_winner) return;
            if (MatchHandler.isOffline())
            {
                if (staticVariables.isGuest)
                {
                    if (gameObject.name == "AI")
                        GuestDataManager._instance.UpdateGuestCoins(-1 * 0);
                    else
                        GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);
                }
                else
                {
                    firstplayer = GameManager.Instance.playersList[0];
                    //Debug.Log("HandCash:" + PlayerPrefs.GetString("CashInHand"));
                    //Debug.Log("now cash:" + firstplayer.playerTotalCash.text.Trim());
                    BigInteger investedAmount = BigInteger.Parse(PlayerPrefs.GetString("CashInHand")) -
                                        BigInteger.Parse(firstplayer.playerTotalCash.text.Trim());
                    //Constants_M.Log("total :" + investedAmount); //Mohsin
                    data = new Dictionary<string, string>
                    {
                    { "round_id", staticVariables.CurrentRoundID },
                    { "winner_id", gameObject.name=="AI"?"ai":staticVariables.UserProfiledata.user._id.ToString()},
                    { "winner_amount",Pot.instance.potSize.ToString() },
                    { "first_player_amount",  investedAmount.ToString() },
                    };
                    //Debug.Log("HandCash:" + PlayerPrefs.GetString("CashInHand"));
                    //PlayerInfo Secondplayer = GameManager.Instance.playersList[1];
                    //BigInteger investedAmount = (firstplayer.player.GetCustomBigIntegerData("CashInHand") - BigInteger.Parse(firstplayer.PokerTotalCashTxt.text));
                    //ConstantsData_M.LogSuccess("startCash :" + firstplayer.player.GetCustomBigIntegerData("CashInHand").ToString() + $"     Second Start Cash {Secondplayer.player.GetCustomBigIntegerData("CashInHand")}");
                    ////Constants_M.Log("total :" + investedAmount); //Mohsin
                    //data = new Dictionary<string, string>
                    // {
                    //    { "round_id", staticVariables.CurrentRoundID },
                    //    { "winner_id", gameObject.name=="AI"?"ai":staticVariables.UserProfiledata.user._id.ToString()},
                    //    { "winner_amount",(firstplayer.player.GetCustomBigIntegerData("CashInHand")- BigInteger.Parse(firstplayer.PokerTotalCashTxt.text)+(Secondplayer.player.GetCustomBigIntegerData("CashInHand")- BigInteger.Parse(Secondplayer.PokerTotalCashTxt.text))).ToString()},
                    //    { "first_player_amount",  investedAmount.ToString() },
                    //    { "second_player_amount",  (Secondplayer.player.GetCustomBigIntegerData("CashInHand")- BigInteger.Parse(Secondplayer.PokerTotalCashTxt.text)).ToString() },
                    // };

                    StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.End_RoundAI_casino_Url, data));//Mohsin line
                }
            }
            else
            {
                if (IsMine())
                {
                    // Mirror: use playersList order (index 0 = creator, index 1 = joiner)
                    firstplayer = GameManager.Instance.playersList[0];
                    PlayerInfo Secondplayer = GameManager.Instance.playersList.Count > 1
                        ? GameManager.Instance.playersList[1] : null;
                    var val = IsMine() && net_winner ? staticVariables.UserProfiledata.user._id.ToString() : staticVariables.OpponetProfile?.userId;

                    // Teen Patti parity. A player stake for the hand is
                    // (balance snapshot taken when the hand started) - (balance now), and the gross
                    // pot is the sum of both stakes. Two bugs made this payload garbage before:
                    // the "CashInHand" snapshot was never written on Poker (so it read 0), and the
                    // difference was taken against PokerTotalCashTxt (table cash) instead of the
                    // player balance, so the backend received 0 - balance for both players
                    // (e.g. first_player_amount = -9061165) and the coins never moved.
                    BigInteger firstAmount = PokerStakeOfThisHand(firstplayer);
                    BigInteger secondAmount = PokerStakeOfThisHand(Secondplayer);

                    if (string.IsNullOrEmpty(staticVariables.CurrentRoundID))
                    {
                        Debug.LogError("[RoundEnd] CurrentRoundID is empty - skipping handle-rounds instead of posting a null round_id.");
                    }
                    else
                    {
                        data = new Dictionary<string, string>
                        {
                            { "round_id", staticVariables.CurrentRoundID },
                            { "winner_id", val },
                            { "winner_amount", (firstAmount + secondAmount).ToString() },
                            { "first_player_amount", firstAmount.ToString() },
                            { "second_player_amount", secondAmount.ToString() },
                        };
                        // Report the outcome instead of firing and forgetting. A failed settle used
                        // to vanish silently and the hand simply never got credited. Nothing can retry
                        // it automatically either: the casino API is called with the player's own
                        // bearer token, and the dedicated server has no logged-in user, so only this
                        // client could have sent it.
                        string settleRoundId = staticVariables.CurrentRoundID;
                        StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.End_Round_casino_Url, data,
                            success =>
                            {
                                Debug.Log($"[RoundEnd] round {settleRoundId} settled OK: {success}");
                                // Tell the server it is done, so the fallback below never fires.
                                if (PlayerTurnManager.Instance != null)
                                    PlayerTurnManager.Instance.CmdPokerRoundSettled(settleRoundId);
                            },
                            failure => Debug.LogError($"[RoundEnd] SETTLE FAILED for round {settleRoundId} " +
                                                      $"(winner={val}, first={firstAmount}, second={secondAmount}): {failure} " +
                                                      $"- this hand was NOT credited on the backend."), withSettlementAuth: true));
                    }
                }

                //Constants_M.Log("link " + gameObject.name == "AI" ? ServerCall.End_RoundAI_casino_Url : ServerCall.End_Round_casino_Url);
                //StartCoroutine(ServerCall.PostRequest(gameObject.name == "AI" ? ServerCall.End_RoundAI_casino_Url : ServerCall.End_Round_casino_Url, data)); //Mohsin
                staticVariables.isPokerFinished = true;

            }
        }
        // Teen Patti parity helper: how much this player put into the pot during this hand.
        // "CashInHand" is the balance snapshot written at the start of the hand (GameStartManager
        // + GameResetManager); MyTotalCashKey carries the live balance and playerTotalCash only
        // renders it, so the property is read first and the label is just a fallback.
        static BigInteger PokerStakeOfThisHand(PlayerInfo player)
        {
            if (player == null)
                return 0;

            BigInteger startCash = player.playerCustomProperties.GetCustomBigIntegerData("CashInHand");
            BigInteger currentCash = player.playerCustomProperties.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey);
            if (currentCash == 0 && player.playerTotalCash != null)
            {
                if (!BigInteger.TryParse(player.playerTotalCash.text.Trim(), out currentCash))
                    currentCash = startCash;   // an unparsable label must not turn into a bogus amount
            }
            return startCash - currentCash;
        }

        public void AIWinner(bool net_winner)
        {
            isWinner = net_winner;
            Debug.Log($"AI Winner called, isPokerFinished: {staticVariables.isPokerFinished}");

            if (staticVariables.isPokerFinished || !net_winner)
                return;

            bool isAI = gameObject.name == "AI";

            if (staticVariables.isGuest)
            {
                GuestDataManager._instance.UpdateGuestCoins(isAI ? 0 : staticVariables.currentPrize * 2);
            }
            else
            {
                var firstPlayer = GameManager.Instance.playersList[0];
                var secondPlayer = GameManager.Instance.playersList[1];

                BigInteger firstStart = BigInteger.Parse(PlayerPrefs.GetString("CashInHand"));
                BigInteger secondStart = BigInteger.Parse(PlayerPrefs.GetString("CashInHandAI"));

                BigInteger firstInvested = firstStart - BigInteger.Parse(firstPlayer.PokerTotalCashTxt.text.Trim());
                BigInteger secondInvested = secondStart - BigInteger.Parse(secondPlayer.PokerTotalCashTxt.text.Trim());

                var data = new Dictionary<string, string>
        {
            { "round_id", staticVariables.CurrentRoundID },
            { "winner_id", isAI ? "ai" : staticVariables.UserProfiledata.user._id.ToString() },
            { "winner_amount", (firstInvested + secondInvested).ToString() },
            { "first_player_amount", firstInvested.ToString() },
            { "second_player_amount", secondInvested.ToString() },
        };

                StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.End_RoundAI_casino_Url, data));
            }

            staticVariables.isPokerFinished = true;
        }


        private void OnDisable()
        {

            if (NetworkServer.active)
            {
                // Release the position on the server
                AllScriptsManager.Instance.PositionsManager.ReleasePosition(myNetworkSeat);
            }
            if (GameStartManager.Instance != null)
            {
                GameStartManager.Instance.AddOrRemovePlayer(-1);
            }
            ShowBtn.SetActive(false);

            PlayerOrignalCardsToShowParent.SetActive(false);
            PlayerDummyCardsToShowParent.SetActive(false);
            SeenIndicator.SetActive(false);
            BlindIndicator.SetActive(false);
        }

        private void OnEnable()
        {
            Debug.Log("PlayerInfo OnEnable is called" + name);
            FindAnyObjectByType<AllScriptsManager>().GameStartManager.AddOrRemovePlayer(1);
        }

        IEnumerator GetPropertyBuyInChips()
        {
            if (!MatchHandler.isOffline())
            {
                if (IsMine())
                {
                    playerCustomProperties.SetCustomBigIntegerData(LocalSettings.TotalChips, LocalSettings.GetTotalChips());// PlayerPrefs.GetString(POKER.LocalSettings.TotalChips));
                    PokerTotalCashTxt.text = LocalSettings.GetTotalChips().ToString();

                }
                yield return new WaitUntil(() => playerCustomProperties.GetCustomBigIntegerData(LocalSettings.TotalChips) > 0);
                if (!IsMine())
                    PokerTotalCashTxt.text = LocalSettings.Rs(playerCustomProperties.GetCustomBigIntegerData(LocalSettings.TotalChips));

            }
            else
            {
                yield return new WaitForSeconds(0.2f);
                if (View_ID_Offline == 1002)
                {

                    PokerTotalCash = LocalSettings.AI_Amount;
                    PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
                }
                else
                {
                    LocalSettings.GetPokerBuyInChips().Show("GetPokerBuyInChips");
                    PokerTotalCash = LocalSettings.GetPokerBuyInChips();
                    PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
                }
            }
        }

        public void sendGoldTranferAndUpdateChips()
        {
            if (NetworkServer.active)
                sendGoldTranferAndUpdateChipsRPC();
            else
                CmdSendGoldTransferAndUpdateChips();
        }

        [ClientRpc]
        public void sendGoldTranferAndUpdateChipsRPC()
        {
            StartCoroutine(GetPropertyBuyInChipsDuringGoldTransfer());
        }

        IEnumerator GetPropertyBuyInChipsDuringGoldTransfer()
        {

            yield return new WaitForSeconds(1f);
            if (!IsMine())
            {
                playerTotalCash.text = LocalSettings.Rs(playerCustomProperties.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey));
            }
            //Debug.LogError("3.....Check Here For Poker Cash....." + PokerTotalCashTxt.text);

        }

        #region AndarBaharSection




        public void DeActivateUIOfAndarBahar(bool istrue)
        {
            ABBettingSection.SetActive(istrue);
            LWBettingSection.SetActive(istrue);

        }


        public void UpdateTextForOtherPlayer()
        {
            if (NetworkServer.active)
                UpdateAndarBaharTextTotalCash();
            else
                CmdUpdateAndarBaharTextTotalCash();
        }

        [Command(requiresAuthority = false)]
        void CmdUpdateAndarBaharTextTotalCash() { UpdateAndarBaharTextTotalCash(); }


        [ClientRpc]
        void UpdateAndarBaharTextTotalCash()
        {
            StartCoroutine(SetTextOfOtherPlayers());
        }

        IEnumerator SetTextOfOtherPlayers()
        {
            yield return new WaitForSeconds(1f);
            playerTotalCash.text = LocalSettings.Rs(playerCustomProperties.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey));
        }

        [ClientRpc]
        void BaharBetOnNetwork(string baharAmmount)
        {
            BetAmountAnimText.text = LocalSettings.Rs(baharAmmount);
            BetAmountAnim.GetComponent<BetAmountToTargetAnim>().targetPos = ABBettingSection.transform.GetChild(2).gameObject;
            BetAmountAnim.SetActive(true);
            StartCoroutine(LoadTextForBet(BaharBetAmoutTxt, baharAmmount));
            //BaharBetAmoutTxt.text = baharAmmount;

            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
            //Debug.LogError("Chip adding sound is playing");
        }
        [ClientRpc]
        void firstTurnFromMaster(bool turn)
        {
            if (GameManager.Instance != null && GameManager.Instance.playersList != null)
            {
                foreach (PlayerInfo item in GameManager.Instance.playersList)
                {
                    item.firstTurnAB = turn;
                }
            }
        }






        /// <summary>
        ///  Super andar bahar section
        /// </summary>


        public void PlaceBetSuperBahar(BigInteger SuperBaharAmount)
        {
            //Pot.instance.AddSuperBaharAmount(AndarBaharPositionsManager.Instance.SuperBaharTotalBetAmount);

            playerCustomProperties.SetCustomBigIntegerData(LocalSettings.abSuperBaharAmountKey, SuperBaharAmount);

            if (NetworkServer.active)
                SuperBaharBetOnNetwork(SuperBaharAmount.ToString());
            else
                CmdSuperBaharBetOnNetwork(SuperBaharAmount.ToString());
            GameManager.Instance.PlayerTotalChipsUpdate(-SuperBaharAmount);
            playerCustomProperties.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
            UpdateTextForOtherPlayer();
            //GoldWinLoose.Instance.SendGold(GoldWinLoose.Trans.bet, SuperBaharAmount.ToString());
        }

        [ClientRpc]
        void SuperAndarBetOnNetwork(string andarAmount)
        {
            SuperAndarBetAmoutTxt.transform.parent.gameObject.SetActive(true);
            SuperAndarBetAmoutTxt.text = LocalSettings.Rs(andarAmount);
        }

        [ClientRpc]
        void SuperBaharBetOnNetwork(string baharAmmount)
        {
            BetAmountAnimText.text = LocalSettings.Rs(baharAmmount);
            BetAmountAnim.GetComponent<BetAmountToTargetAnim>().targetPos = ABBettingSection.transform.GetChild(0).gameObject;
            BetAmountAnim.SetActive(true);
            SuperBaharBetAmoutTxt.transform.parent.gameObject.SetActive(true);



            StartCoroutine(LoadTextForBet(SuperBaharBetAmoutTxt, baharAmmount));

            //SuperBaharBetAmoutTxt.text = baharAmmount;

            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
            //Debug.LogError("Chips adding sound is playing");
        }

        IEnumerator LoadTextForBet(TMP_Text BetAmountText, string Amount)
        {
            //Debug.LogError("kia bana");
            yield return new WaitUntil(() => !this.BetAmountAnim.gameObject.activeSelf);

            if (TieBetWinLw)
            {
                //Debug.LogError("betting amount: " + Amount);
                BigInteger newAmount = BigInteger.Parse(Amount);
                newAmount *= 2;
                BetAmountText.text = LocalSettings.Rs(newAmount);
            }
            else
                BetAmountText.text = LocalSettings.Rs(Amount);
        }
        //public void TurnOnOffAb(bool turn)
        //{
        //    photonView.RPC(nameof(TurnForAllPlayerOfAB), RpcTarget.All, turn);
        //}
        //public void SecondTurnAB(bool turn)
        //{
        //    photonView.RPC(nameof(SecondTurnAndarBahar), RpcTarget.All, turn);
        //}
        #endregion

        #region Wingo Lottary Section


        Transform[] BetPointsArray = new Transform[13];



        void PlayAnimation(Transform ObjToAnimate, UnityEngine.Vector2 TouchPos, float TimeToReach)
        {
            ObjToAnimate.DOMove(TouchPos, TimeToReach, false);
        }

        //IEnumerator PlayAnimationCoRoutine(Transform ObjToAnimate, UnityEngine.Vector2 TouchPos, float TimeToReach)
        //{
        //    yield return new WaitForSeconds(0.01f);
        //    ObjToAnimate.DOMove(TouchPos, TimeToReach, false);
        //}


        // When result is called



        #endregion

        #region Chat messages Section






        #endregion






        public void MyTotalCashTextUpdate()
        {
            // Debug.Break();
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    cashCheckOfAllPlayers();
                else
                    CmdCashCheckOfAllPlayers();
            }
            else
                cashCheckOfAllPlayersAI();
        }
        [ClientRpc]
        void cashCheckOfAllPlayers()
        {
            StartCoroutine(GetTotalCash());
            //playerTotalCash.text = LocalSettings.Rs(player.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey));
        }
        void cashCheckOfAllPlayersAI()
        {
            StartCoroutine(GetTotalCash());
            //playerTotalCash.text = LocalSettings.Rs(player.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey));
        }

        IEnumerator GetTotalCash()
        {
            yield return new WaitForSeconds(1f);
            if (!MatchHandler.isOffline())
            {
                playerTotalCash.text = LocalSettings.Rs(playerCustomProperties.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey));

            }
            else
            {
                playerTotalCash.text = this.gameObject.name == LocalSettings.AI_Name ?
                  LocalSettings.Rs(LocalSettings.AI_Amount) : LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());
            }


            //  Debug.LogError(LocalSettings.Rs(player.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey)));
        }



        #region Poker Section



        public void UpdateMessage(string message)
        {
            PokerInfoMessage.text = message;
            BetStatusObj.SetActive(true);
            Invoke(nameof(StatusToFalse), 2f);
        }

        public void UpdateHandRankLabel(string message, int rank, int scores)
        {
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    UpdatingHandRankLabelOnNextwork(message, rank, scores);
                else
                    CmdUpdateHandRankLabel(message, rank, scores);
            }
            else
                UpdatingHandRankLabelOnAI(message, rank, scores);
        }

        [Command(requiresAuthority = false)]
        void CmdUpdateHandRankLabel(string message, int rank, int scores)
        {
            // Store rank on server so CmdIAmWinner can validate the winner
            PokerPlayerCurrentRank = rank;
            PokerScores = scores;
            UpdatingHandRankLabelOnNextwork(message, rank, scores);
        }

        [ClientRpc]
        void UpdatingHandRankLabelOnNextwork(string message, int rank, int scores)
        {
            HandRankLabelTxt.text = message;
            PokerPlayerCurrentRank = rank;
            PokerScores = scores;
        }
        void UpdatingHandRankLabelOnAI(string message, int rank, int scores)
        {
            HandRankLabelTxt.text = message;
            PokerPlayerCurrentRank = rank;
            PokerScores = scores;
        }

        void StatusToFalse()
        {
            BetStatusObj.SetActive(false);
        }

        public void BetStartAmountBet(BigInteger pokerCurrentBetAmount, string viewID)
        {
            // Mirror migration: replaced Photon RPC with [ClientRpc].
            // BetStartAmountBet is only called from DealerToNext() which runs on the server,
            // so calling [ClientRpc] directly here is correct and safe.
            if (!MatchHandler.isOffline())
                BetStartAmountBetRPC(LocalSettings.BigIntegerToString(pokerCurrentBetAmount), viewID);
            else
                BetStartAmountBetAI(LocalSettings.BigIntegerToString(pokerCurrentBetAmount), viewID);
        }


        [ClientRpc]
        public void BetStartAmountBetRPC(string pokerCurrentBetAmount, string ViewID)
        {
            BigInteger betAmount = LocalSettings.StringToBigInteger(pokerCurrentBetAmount);
            int viewID = int.Parse(ViewID);
            //Debug.LogError("Check View ID...1..." + viewID + "...Check AMount...." + betAmount);
            if (!MatchHandler.isOffline())
            {
                "idhr aarha hai hai code bhai".Show();
                if (viewID == int.Parse(UIManager.Instance.GetMyPlayerInfo().ownerPlayerId))
                {
                    "chal ja na".Show();
                    //Debug.LogError("Check View ID...2..." + viewID + "...Check AMount...." + betAmount);
                    LocalSettings.SetPokerBuyInChips(-betAmount);
                    PokerTotalCash = LocalSettings.GetTotalChips();
                    PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
                    // Debug.LogError("4.....Check Here For Poker Cash....." + PokerTotalCashTxt.text);
                    //startCash = BigInteger.Parse(PokerTotalCashTxt.text) + 500;
                    //startCash.Show("object name"+ this.gameObject.name);
                    if (!MatchHandler.isOffline())
                        playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, LocalSettings.GetPokerBuyInChips());

                }
            }


            //foreach (var item in PlayerStateManager.Instance.PlayingList)
            //{
            //    if (item.photonView.ViewID == viewID)
            //    {

            //        if (player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey) != null)
            //        {
            //            PokerTotalCash = player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey);
            //            PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
            //        }
            //    }
            //}
            StartCoroutine(UpdateCash(viewID));

        }
        public void BetStartAmountBetAI(string pokerCurrentBetAmount, string ViewID)
        {
            BigInteger betAmount = LocalSettings.StringToBigInteger(pokerCurrentBetAmount);
            int viewID = int.Parse(ViewID);
            //Debug.LogError("Check View ID...1..." + viewID + "...Check AMount...." + betAmount);

            "idhr q aarha hai hai code bhai".Show();

            if (viewID == View_ID_Offline && this.gameObject.name != LocalSettings.AI_Name)
            {
                //Debug.LogError("Check View ID...2..." + viewID + "...Check AMount...." + betAmount);
                LocalSettings.SetPokerBuyInChips(-betAmount);
                PokerTotalCash = LocalSettings.GetPokerBuyInChips();

                LocalSettings.Rs(PokerTotalCash).Show();

                PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
                // Debug.LogError("4.....Check Here For Poker Cash....." + PokerTotalCashTxt.text);
                if (!MatchHandler.isOffline())
                    playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, LocalSettings.GetPokerBuyInChips());

            }
            else
            {
                LocalSettings.AI_Amount -= betAmount;
                PokerTotalCash = LocalSettings.AI_Amount;
                PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
                "AI cash initialized".Show();
                PokerTotalCashTxt.text.Show();
            }


            //foreach (var item in PlayerStateManager.Instance.PlayingList)
            //{
            //    if (item.photonView.ViewID == viewID)
            //    {

            //        if (player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey) != null)
            //        {
            //            PokerTotalCash = player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey);
            //            PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
            //        }
            //    }
            //}
            StartCoroutine(UpdateCash(viewID));

        }

        IEnumerator UpdateCash(int viewID)
        {
            yield return new WaitForSeconds(0.5f);


            foreach (var item in PlayerStateManager.Instance.PlayingList)
            {
                if (!MatchHandler.isOffline())
                {
                    if (int.Parse(item.ownerPlayerId) == viewID)
                    {

                        if (playerCustomProperties.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey) != null)
                        {
                            PokerTotalCash = playerCustomProperties.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey);
                            PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
                            // Debug.LogError("6.....Check Here For Poker Cash....." + PokerTotalCashTxt.text);
                        }
                    }
                }
                else
                {
                    if (item.View_ID_Offline == viewID)
                    {
                        if (this.gameObject.name != LocalSettings.AI_Name)
                        {
                            PokerTotalCash = LocalSettings.GetPokerBuyInChips();
                            LocalSettings.GetPokerBuyInChips().Show();
                        }
                        else
                        {
                            PokerTotalCash = LocalSettings.AI_Amount;
                        }
                        PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
                    }
                }
            }
        }

        public void SendPokerBet(BigInteger pokerCurrentBetAmount, bool isAllIn)
        {
            // Server-driven bets (the blinds from DealerToNext) broadcast straight through the
            // [ClientRpc]. A player's own raise / call used to take the offline path
            // (SendPokerBetAI) and was applied ONLY on that player's own client: nothing but the
            // "BetPlaced" state change ever crossed the wire, so the opponent never learned the
            // amount. Each client then accumulated a different pot - its own bets plus the blinds -
            // which is why the two sides showed different totals. Route it through the server so the
            // same amount is applied everywhere.
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    ServerAccumulatePokerBet(pokerCurrentBetAmount);
                    SendPokerBetRPC(LocalSettings.BigIntegerToString(pokerCurrentBetAmount), isAllIn);
                }
                else
                    CmdSendPokerBet(LocalSettings.BigIntegerToString(pokerCurrentBetAmount), isAllIn);
            }
            else
            {
                SendPokerBetAI(LocalSettings.BigIntegerToString(pokerCurrentBetAmount), isAllIn);
            }
        }
        [ClientRpc]
        public void SendPokerBetRPC(string pokerCurrentBetAmountString, bool isAllIn)
        {


            BigInteger pokerCurrentBetAmount = LocalSettings.StringToBigInteger(pokerCurrentBetAmountString);
            pokerTotalBetCash += pokerCurrentBetAmount;
            if (!MatchHandler.isOffline())
            {
                if (IsMine())
                    UIManager.Instance.TotalBetPlacedAmount += pokerCurrentBetAmount;
            }
            else
            {
                if (this.gameObject.name != LocalSettings.AI_Name)
                {
                    UIManager.Instance.TotalBetPlacedAmount += pokerCurrentBetAmount;
                }
                else
                {
                    UIManager.Instance.TotalBetPlacedAmount += pokerCurrentBetAmount;
                    // yahan se next krna hai jaha first Bet Ja rahe hai
                }
            }
            PokerActionPanel.Instance.BetPlacedAmount(pokerCurrentBetAmount);
            PokerTotalWholeBetAmount += pokerCurrentBetAmount;
            //  PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(photonView.ViewID, PokerTotalWholeBetAmount);
            if (pokerCurrentBetAmount > 0)
            {

                GameObject betAnimation = Instantiate(PokerBetAmountAnimPrefab);
                betAnimation.SetActive(true);

                LocalSettings.SetPosAndRect(betAnimation, PokerBetAmountAnimPrefab.GetComponent<RectTransform>(), PokerBetAmountAnimPrefab.transform.parent);

                if (pokerBetAmountAnim == null)
                {
                    pokerBetAmountAnim = betAnimation;

                    betAnimation.GetComponent<PokerBetAmountAnim>().PlayAnimation(FirstTargetPokerBetAmount, false, pokerCurrentBetAmount);
                }
                else
                {
                    betAnimation.GetComponent<PokerBetAmountAnim>().PlayAnimation(pokerBetAmountAnim.transform, false, pokerCurrentBetAmount);
                }
            }

            if (!MatchHandler.isOffline())
            {
                if (IsMine())
                {
                    PokerTotalCash = LocalSettings.GetTotalChips();

                    Debug.LogError("Check Animation on Start.....1" + PokerTotalCash.ToString());
                }
                else
                {
                    PokerTotalCash = playerCustomProperties.GetCustomBigIntegerData(LocalSettings.TotalChips);
                    Debug.LogError("Check Animation on Start.....2" + PokerTotalCash.ToString());

                }
            }
            else
            {
                if (this.gameObject.name != LocalSettings.AI_Name)
                    PokerTotalCash = LocalSettings.GetPokerBuyInChips();
                else
                    PokerTotalCash = LocalSettings.AI_Amount;
            }

            PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
            PokerTotalCashTxt.text.Show();
            // Debug.LogError("7.....Check Here For Poker Cash....." + PokerTotalCashTxt.text);
            // Debug.LogError("first check -------------- 1");
            SetCurrentTargetBetAmount();
            CheckAllIncash(isAllIn);
            if (PokerActionPanel.Instance.checkPockeBetPlaced())//isCircleCheckFlag)
            {
                if ((checkForAllPlayersbetAreEqual() || PokerActionPanel.Instance.CheckBoolAllIN()) && PokerActionPanel.Instance.checkPockeBetPlaced())
                    StartCoroutine(BetsGoToFinalPoints(1.5f));
            }
            else
                CheckIfNoOneHasCircleFlag();

        }
        public void SendPokerBetAI(string pokerCurrentBetAmountString, bool isAllIn)
        {


            BigInteger pokerCurrentBetAmount = LocalSettings.StringToBigInteger(pokerCurrentBetAmountString);
            pokerTotalBetCash += pokerCurrentBetAmount;
            if (!MatchHandler.isOffline())
            {
                if (IsMine())
                    UIManager.Instance.TotalBetPlacedAmount += pokerCurrentBetAmount;
            }
            else
            {
                if (this.gameObject.name != LocalSettings.AI_Name)
                {
                    UIManager.Instance.TotalBetPlacedAmount += pokerCurrentBetAmount;
                }
                else
                {
                    UIManager.Instance.TotalBetPlacedAmount += pokerCurrentBetAmount;
                    // yahan se next krna hai jaha first Bet Ja rahe hai
                }
            }
            PokerActionPanel.Instance.BetPlacedAmount(pokerCurrentBetAmount);
            PokerTotalWholeBetAmount += pokerCurrentBetAmount;
            //  PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(photonView.ViewID, PokerTotalWholeBetAmount);
            if (pokerCurrentBetAmount > 0)
            {

                GameObject betAnimation = Instantiate(PokerBetAmountAnimPrefab);
                betAnimation.SetActive(true);

                LocalSettings.SetPosAndRect(betAnimation, PokerBetAmountAnimPrefab.GetComponent<RectTransform>(), PokerBetAmountAnimPrefab.transform.parent);

                if (pokerBetAmountAnim == null)
                {
                    pokerBetAmountAnim = betAnimation;

                    betAnimation.GetComponent<PokerBetAmountAnim>().PlayAnimation(FirstTargetPokerBetAmount, false, pokerCurrentBetAmount);
                }
                else
                {
                    betAnimation.GetComponent<PokerBetAmountAnim>().PlayAnimation(pokerBetAmountAnim.transform, false, pokerCurrentBetAmount);
                }
            }

            if (!MatchHandler.isOffline())
            {
                if (IsMine())
                {
                    PokerTotalCash = LocalSettings.GetPokerBuyInChips();

                    //Debug.LogError("Check Animation on Start.....1");
                }
                else
                {
                    //Debug.LogError("Check Animation on Start.....2");
                    PokerTotalCash = playerCustomProperties.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey);
                }
            }
            else
            {
                if (this.gameObject.name != LocalSettings.AI_Name)
                    PokerTotalCash = LocalSettings.GetPokerBuyInChips();
                else
                    PokerTotalCash = LocalSettings.AI_Amount;
            }

            PokerTotalCashTxt.text = LocalSettings.Rs(PokerTotalCash);
            PokerTotalCashTxt.text.Show();
            // Debug.LogError("7.....Check Here For Poker Cash....." + PokerTotalCashTxt.text);
            // Debug.LogError("first check -------------- 1");
            SetCurrentTargetBetAmount();
            CheckAllIncash(isAllIn);
            if (PokerActionPanel.Instance.checkPockeBetPlaced())//isCircleCheckFlag)
            {
                if ((checkForAllPlayersbetAreEqual() || PokerActionPanel.Instance.CheckBoolAllIN()) && PokerActionPanel.Instance.checkPockeBetPlaced())
                    StartCoroutine(BetsGoToFinalPoints(1.5f));
            }
            else
                CheckIfNoOneHasCircleFlag();

        }




        public bool checkForAllPlayersbetAreEqual()
        {
            Debug.Log($"[BetEqual] START: myName={gameObject.name}, myBet={pokerTotalBetCash}, myState={currentPlayerStateRef.currentState}, PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count}");
            for (int i = 0; i < PlayerStateManager.Instance.PlayingList.Count; i++)
            {
                var other = PlayerStateManager.Instance.PlayingList[i];
                Debug.Log($"[BetEqual] Comparing: myBet={pokerTotalBetCash} vs PlayingList[{i}]={other.gameObject.name} bet={other.pokerTotalBetCash}");
                //if (pokerTotalBetCash != PlayerStateManager.Instance.PlayingList[i].pokerTotalBetCash)
                //    return false;

                //if (CheckBoolAllIN())
                //    return true;
                if (currentPlayerStateRef.currentState == PlayerState.STATE.Packed || currentPlayerStateRef.currentState == PlayerState.STATE.OutOfTable)
                {
                    if (pokerTotalBetCash < PlayerStateManager.Instance.PlayingList[i].pokerTotalBetCash)
                    {
                        Debug.Log($"[BetEqual] RESULT: true (packed/out and myBet < otherBet)");
                        return true;
                    }
                }


                if (pokerTotalBetCash != PlayerStateManager.Instance.PlayingList[i].pokerTotalBetCash)
                {
                    Debug.Log($"[BetEqual] RESULT: false (bets NOT equal → CALL should show)");
                    return false;
                }


                // There will be check if all in bet placed
            }
            Debug.Log($"[BetEqual] RESULT: true (all bets equal → CHECK should show)");
            return true;
        }
        void CheckIfNoOneHasCircleFlag()
        {
            bool isThereNoCircleFlag = true;
            foreach (PlayerInfo pinfo in PlayerStateManager.Instance.PlayingList)
            {
                if (pinfo.isCircleCheckFlag)
                {
                    isThereNoCircleFlag = false;
                }
            }
            if (isThereNoCircleFlag)
                isCircleCheckFlag = true;
        }



        public void SendBetToThePot()
        {

            // photonView.RPC(nameof(BetGoToThePot), RpcTarget.All);
        }


        //[PunRPC]

        public void PokerBetGoToThePot()
        {
            StartCoroutine(BetsGoToFinalPoints(0.2f));

        }

        public void BetGoToFinalPointsForPacked(float delay)
        {
            StartCoroutine(BetsGoToFinalPoints(delay));
        }

        /// <summary>
        /// Just Checking List of Amjad
        /// </summary>
        [ShowOnly]
        public List<PlayerInfo> playerInfos = new List<PlayerInfo>();

        IEnumerator BetsGoToFinalPoints(float DelayTime)
        {
            // Check bets for Alla players
            PlayerStateManager PSM = PlayerStateManager.Instance;
            Debug.Log("Here is 0");
            if (isAllInCashBet)
            {

                BigInteger pokerAllInTotalBetCash = PSM.PlayingList
             .Min(info => info.pokerTotalBetCash);

                for (int i = 0; i < PSM.PlayingList.Count; i++)
                {
                    if (pokerAllInTotalBetCash < PSM.PlayingList[i].pokerTotalBetCash)
                    {
                        BigInteger RemainingAmount = PSM.PlayingList[i].pokerTotalBetCash - pokerAllInTotalBetCash;
                        PSM.PlayingList[i].pokerTotalBetCash = pokerAllInTotalBetCash;
                        PokerActionPanel.Instance.BetPlacedAmount(-RemainingAmount);
                        Pot.instance.PotTxt.text = LocalSettings.Rs(PokerActionPanel.Instance.TotalPotAmount());
                        if (!MatchHandler.isOffline())
                        {
                            if (PSM.PlayingList[i].IsMine())
                            {
                                LocalSettings.SetPokerBuyInChips(RemainingAmount);

                                PSM.PlayingList[i].PokerTotalCash = LocalSettings.GetTotalChips();
                                PSM.PlayingList[i].playerCustomProperties.SetCustomBigIntegerData(LocalSettings.TotalChips, LocalSettings.GetTotalChips());
                                PSM.PlayingList[i].PokerTotalCashTxt.text = LocalSettings.Rs(LocalSettings.GetTotalChips());

                                Debug.Log("Check Here player Status   " + LocalSettings.Rs(LocalSettings.GetTotalChips()));
                            }
                            else
                            {
                                StartCoroutine(GetPropertyBuyInChips());
                            }
                        }
                        else
                        {



                            if (PSM.PlayingList[i].gameObject.name != LocalSettings.AI_Name)
                            {
                                LocalSettings.SetPokerBuyInChips(RemainingAmount);
                                Debug.Log("Here is 1");
                                PSM.PlayingList[i].PokerTotalCash = LocalSettings.GetTotalChips();
                                PSM.PlayingList[i].PokerTotalCashTxt.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
                                PSM.PlayingList[i].PokerTotalCashTxt.text.Show();
                            }
                            else
                            {
                                Debug.Log("Here is 2");
                                LocalSettings.AI_Amount += RemainingAmount;
                                PSM.PlayingList[i].PokerTotalCash = LocalSettings.AI_Amount;
                                StartCoroutine(GetPropertyBuyInChips());
                            }
                        }

                    }
                }
            }

            //IEnumerator GetPropertyBuyInChipsForReturnCash()
            //{
            //    if (PhotonNetwork.IsConnectedAndReady)
            //    {
            //        yield return new WaitUntil(() => player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey) > 0);
            //        if (!photonView.IsMine)
            //            PokerTotalCashTxt.text = LocalSettings.Rs(player.GetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey));
            //        //Debug.LogError("3.....Check Here For Poker Cash....." + PokerTotalCashTxt.text);
            //    }
            //}

            // Disable the poker action panel for everyone and show the cards
            foreach (PlayerInfo pinfo in PlayerStateManager.Instance.PlayingList)
            {
                pinfo.pokerTotalBetCash = 0;

            }
            yield return new WaitForSeconds(DelayTime);

            PokerActionPanel.Instance.AllPokerBetsGoToFinalPoint();

            //Debug.LogError("packed State....3...");

            // Debug.LogError("Second check -------------- 2");
            SetCurrentTargetBetAmount();
        }

        public void SetCircleFlatPoker()
        {
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    SetCircleFlagPokerRPC();
                else
                    CmdSetCircleFlagPoker();
            }
            else
                SetCircleFlagPokerAI();
        }
        [ClientRpc]
        public void SetCircleFlagPokerRPC()
        {
            isCircleCheckFlag = true;
        }
        public void SetCircleFlagPokerAI()
        {
            isCircleCheckFlag = true;
        }
        void SetCurrentTargetBetAmount()
        {

            //Debug.LogError("packed State....4...");
            // Debug.LogError("Current TargetBet Amount " + PokerActionPanel.Instance.CurrentTargetBetAmount + "  " + pokerTotalBetCash);

            if (PokerActionPanel.Instance.CurrentTargetBetAmount < pokerTotalBetCash)
                PokerActionPanel.Instance.CurrentTargetBetAmount = pokerTotalBetCash;
            PokerActionPanel.Instance.SetMinBetAmount();
        }

        public void GiveTurnToNextPlayerPocker()
        {
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    GiveTurnToNextPlayer();
                else
                    CmdGiveTurnToNextPlayerPocker();
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdGiveTurnToNextPlayerPocker()
        {
            GiveTurnToNextPlayer();
        }

        [ClientRpc]
        void GiveTurnToNextPlayer()
        {
            if (IsMine())
            {
                int myIndex = PlayerStateManager.Instance.SideShowNext();
                //if (checkPockeBetPlaced())
                //    photonView.RPC(nameof(resetBoolValue), RpcTarget.All);
                PlayerStateManager.Instance.PlayingList[myIndex].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
            }
        }
        void CheckAllIncash(bool isAllIn)
        {
            if (isAllIn)
            {
                isAllInCashBet = true;
                CashAllInIndicator.SetActive(true);
            }
        }




        public void myPockerTurnComplete(bool isTrue)
        {
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    placedMyBet(isTrue);
                else
                {
                    isBetPlacedPocker = isTrue; // Set locally for immediate checks
                    CmdPlacedMyBet(isTrue);     // Sync to all clients via server
                }
            }
            else
            {
                placedMyBetAI(isTrue);
            }
        }

        [Command(requiresAuthority = false)]
        void CmdPlacedMyBet(bool istrue)
        {
            placedMyBet(istrue);
        }

        [ClientRpc]
        void placedMyBet(bool istrue)
        {
            isBetPlacedPocker = istrue;
        }
        void placedMyBetAI(bool istrue)
        {
            isBetPlacedPocker = istrue;
        }
        public void ResetIsBetPlacedPocker()
        {
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    resetBoolValue();
                else
                {
                    resetBoolValueAI();  // Reset locally immediately
                    CmdResetBoolValue(); // Sync reset to all clients via server
                }
            }
            else
            {
                resetBoolValueAI();
            }
        }

        [Command(requiresAuthority = false)]
        void CmdResetBoolValue()
        {
            resetBoolValue();
        }

        [ClientRpc]
        public void resetBoolValue()
        {
            foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
            {
                item.isBetPlacedPocker = false;
            }

        }
        public void resetBoolValueAI()
        {
            foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
            {
                item.isBetPlacedPocker = false;
            }

        }

        IEnumerator GiveOrgPokersCardsIfNotInGame(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            if (playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState) != PlayerState.STATE.OutOfGame)
            {
                GameManager gameManager = GameManager.Instance;

                int card1Index = playerCustomProperties.GetCustomData(LocalSettings.pokerHoleCard1ForPlayer);
                int card2Index = playerCustomProperties.GetCustomData(LocalSettings.pokerHoleCard2ForPlayer);

                yield return new WaitForSeconds(0.2f);
                GameObject hole_card1 = Instantiate(GameManager.Instance.AllCards.Card[card1Index].gameObject);
                GameObject hole_card2 = Instantiate(GameManager.Instance.AllCards.Card[card2Index].gameObject);
                PokerCheckWinner.Instance.HoleCardsToDestroy.Add(hole_card1);
                PokerCheckWinner.Instance.HoleCardsToDestroy.Add(hole_card2);
                LocalSettings.SetPosAndRect(hole_card1, card_1_RectTr, card_1_RectTr.parent);
                LocalSettings.SetPosAndRect(hole_card2, card_2_RectTr, card_2_RectTr.parent);

                Hole_Card1 = hole_card1.GetComponent<CardProperty>();
                Hole_Card2 = hole_card2.GetComponent<CardProperty>();
                if (RoomStateManager.Instance.GetCurrentRoomState() == RoomState.STATE.WaitingForResults || RoomStateManager.Instance.GetCurrentRoomState() == RoomState.STATE.ShowingResults)
                {
                    Hole_Card1.gameObject.SetActive(true);
                    Hole_Card2.gameObject.SetActive(true);
                }
                else
                {
                    Hole_Card1.gameObject.SetActive(false);
                    Hole_Card2.gameObject.SetActive(false);
                    DummyCardsParent.transform.GetChild(0).gameObject.SetActive(true);
                    DummyCardsParent.transform.GetChild(1).gameObject.SetActive(true);
                }
            }
        }

        public void updateHighRankCardsPoker(int[] cardIndexArray)
        {
            string aa = "";
            foreach (int item in cardIndexArray)
            {
                aa = aa + item + " -- ";
            }
            // Debug.LogError(photonView.Controller.NickName + ":    " + aa);
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                    updateHighRankCardsPokerRPC(cardIndexArray);
                else
                    CmdUpdateHighRankCardsPoker(cardIndexArray);
            }
            else
                updateHighRankCardsPokerAI(cardIndexArray);
        }

        [Command(requiresAuthority = false)]
        void CmdUpdateHighRankCardsPoker(int[] cardIndexArray)
        {
            updateHighRankCardsPokerRPC(cardIndexArray);
        }
        [ClientRpc]
        public void updateHighRankCardsPokerRPC(int[] cardIndexArray)
        {
            if (HighRankingCards.Length == 0)
            {
                HighRankingCards = new CardProperty[5];
            }
            for (int i = 0; i < cardIndexArray.Length; i++)
            {
                HighRankingCards[i] = GetCard(cardIndexArray[i]);
            }
        }
        public void updateHighRankCardsPokerAI(int[] cardIndexArray)
        {
            if (HighRankingCards.Length == 0)
            {
                HighRankingCards = new CardProperty[5];
            }
            for (int i = 0; i < cardIndexArray.Length; i++)
            {
                HighRankingCards[i] = GetCard(cardIndexArray[i]);
            }
        }

        CardProperty GetCard(int cardIndex)
        {
            if (Hole_Card1.CardIndexInArray == cardIndex)
                return Hole_Card1;
            if (Hole_Card2.CardIndexInArray == cardIndex)
                return Hole_Card2;
            PokerCheckWinner pcw = PokerCheckWinner.Instance;
            for (int i = 0; i < pcw.Community_Cards.Length; i++)
            {
                if (pcw.Community_Cards[i].CardIndexInArray == cardIndex)
                    return pcw.Community_Cards[i];
            }
            return null;
        }
        #endregion


        #region MasterChange When GameRunINBackGround
        [ShowOnly] public bool checkApplicationBackground;

        public void playerBackGround(bool isTrue)
        {
            //Toaster.ShowAToast("Check GameManager..." + isTrue);
            if (NetworkServer.active)
                playerRunInBackGroundRPC(isTrue);
            else
                CmdPlayerRunInBackGround(isTrue);
        }

        [ClientRpc]
        void playerRunInBackGroundRPC(bool isTrue)
        {
            checkApplicationBackground = isTrue;

            //Toaster.ShowAToast("Check Player....." + checkApplicationBackground);
        }
        #endregion



        #region API For Profile Image
        Action<Sprite> SetProfileImage;
        Sprite ProfilePic;



        private IEnumerator DownloadImageAndConvertToSprite(string imageUrl, Action<Sprite> onCompleteMethod)
        {
            WWW www = new WWW(imageUrl); // Start downloading the image

            yield return www; // Wait for the download to complete

            if (!string.IsNullOrEmpty(www.error))
            {
                StartCoroutine(waitforLoadPlayerAvatar());
                Debug.LogError("Error downloading image: " + www.error);
                yield break;
            }

            Texture2D texture = www.texture; // Get the downloaded texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), UnityEngine.Vector2.one * 0.5f);

            //imageDisplay.sprite = sprite; // Set the sprite on the Image component

            onCompleteMethod.Invoke(sprite);
        }

        public void UpdateProfileImageAfterReceivingFromServer(Sprite sprite)
        {

            ProfilePic = sprite;

        }
        #endregion

        [Button]
        public void DebugValues()
        {
            ConstantsData_M.Show($" pokerTotalBetCash:{pokerTotalBetCash}     PokerTotalWholeBetAmount:{PokerTotalWholeBetAmount}     Pot.instance.potSize:{Pot.instance.potSize}    Pot.instance.CurrentChalAmount{Pot.instance.CurrentChalAmount}");

        }
        [ClientRpc]
        public void leaveRoom()
        {
            // PhotonNetwork.LeaveRoom();
        }
    }
}