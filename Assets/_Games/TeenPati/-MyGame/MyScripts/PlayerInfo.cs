using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using System.Numerics;
using TMPro;
//using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using POKER;
using Mirror;
using UnityExtensions;

namespace TeenPattiGame
{

    public class PlayerInfo : NetworkBehaviour
    {
        // AI
        public BigInteger AI_Amount;
        public int View_ID_Offline;

        //
        public GameObject My_Player_Indicator;

        public GameObject PlayerDummyCardsToShowParent;
        public GameObject PlayerOrignalCardsToShowParent;
        public GameObject ShowBtn;
        public GameObject viewResultAB;
        public GameObject SeenIndicator;
        public GameObject BlindIndicator;
        public GameObject WinningIndicator;
        public GameObject BetAmountAnim;
        public GameObject myCurrentBetAmountAnim;
        public GameObject myCurrentBetAmountTarget;
        public GameObject tipAmountAnim;
        public GameObject SideShowIndicatorAnim;

        public GameObject FireAnimObj;

        [HideInInspector]




        public RectTransform For3PattiPlayer;

        // public Player player;

        public Image FillerImage;
        public Image PlayerAvatorImage;
        public Image playerFrameImage;

        public TMP_Text PicIndexTxt;
        public TMP_Text player_name;
        public TMP_Text BetAmountAnimText;
        public TMP_Text myCurrentBetAmountText;
        public TMP_Text PackedText;
        public TMP_Text PlayerStateText;




        public TMP_Text playerTotalCash;
        [ShowOnly] public bool isFirstCurrentChaalBool;

        public bool seated;
        public bool gamePlaying;
        //public bool gamePokerisStandUp; 
        [ShowOnly] public bool IsSeen;
        [ShowOnly] public bool IsSideShow;


        [ShowOnly] public bool cardShuffleBool = false;




        // For Dragon Tiger


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



        public List<string> tipsDialouges;


        // poker end



        #region Lucky War Section
        /// <summary>
        /// Lucky War 
        /// </summary>


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
                if (isOwned)
                {
                    GameManager.Instance.myLocalSeat = value;
                }
            }
        }

        public PlayerCustomProperties playerCustomProperties;

        private int privateNetworkSeat;

        public NetworkIdentity this_photonView;


        public PlayerCurrentState currentPlayerStateRef;


        private void Awake()
        {
            if (NetworkServer.active)
            {
                TPLog.Flow("PlayerInfo", "Running on the server -> this build is marked as master client");
                MirrorNetwork.Instance.isMasterClient = true;
            }

        }


        public int GetMineIndexInPlayerList()
        {
            int index = GameManager.Instance.playersList.FindIndex(x => x == this);
            return index;
        }

        void EnableDisableThings()
        {
            if (MatchHandler.IsTeenPatti())
            {
                LocalSettings.SetPosAndRect(player_name.transform.parent.gameObject, For3PattiPlayer, this.gameObject.transform);

            }
            else
            {
                LocalSettings.SetPosAndRect(player_name.transform.parent.gameObject, For3PattiPlayer, this.gameObject.transform);
            }




        }
        private void OnEnable()
        {
            Debug.Log(gameObject.name);
            TPLog.Flow("PlayerInfo", "Player object ENABLED: '" + gameObject.name + "'");
            //Debug.Break();
            GameStartManager.Instance.AddOrRemovePlayer(1);

        }

        int num = 0;
        IEnumerator Start()
        {
            ConstantsData_M.Log($"TeenPatti player Name:{gameObject.name} ");
            yield return new WaitForSeconds(1);
            MyScores = 0;
            MyRank = 0;

            if (!MatchHandler.isOffline())
            {
                if (!MirrorNetwork.Instance.isMasterClient && isOwned)
                {
                    TPLog.Flow("PlayerInfo", "I am a joining (non-master) client -> reading the current room state from the room property");
                    RoomStateManager.Instance.UpdateLocalRoomState(TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.GetRoomStateProperty(LocalSettings.roomState));
                }
            }


            LocalSettings.AI_Amount = UnityEngine.Random.Range(50000, 100000);

            SeedMyCashPropertyBeforeFirstRead();

            MyTotalCashTextUpdate();


            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    TPLog.Flow("PlayerInfo", "Server -> giving '" + gameObject.name + "' a seat");
                    PositionsManager.Instance.AssignPositionOfthisPlayer(this);
                }
                else
                {
                    TPLog.Flow("PlayerInfo", "Client -> the seat for '" + gameObject.name + "' comes from the server");
                }
            }

            //PhotonNetwork.LocalCleanPhotonView(photonView);
            //onenable 
            GetProfilePic();

            //onenable ended

            EnableDisableThings();
            Playeramount();

            playerEnterenceState();

            num++;
            // Debug.LogError("Adding My Player Of time: " + num);
            GameManager.Instance.AddingPlayer(this);
            IsSeen = false;

            // MULTIPLAYER ONLY - offline/AI never needs it (seats are assigned locally there).
            if (!MatchHandler.isOffline() && !NetworkServer.active && !isOwned)
                StartCoroutine(RestoreNetworkSeatFromSyncedProperty());

            if (isOwned)
            {
                TPLog.Flow("PlayerInfo", "This spawned object is MINE -> named '" + LocalSettings.GetPlayerName() + "'");
                UIManager.Instance.MyLocalPlayer = gameObject;
                WinningIndicator.transform.GetChild(0).localScale = UnityEngine.Vector3.one * 1.1f;
                SideShowIndicatorAnim.transform.GetChild(0).localScale = UnityEngine.Vector3.one * 1.1f;
                AssignName(LocalSettings.GetPlayerName());

            }
            else
            {
                TPLog.Flow("PlayerInfo", "This spawned object is the OPPONENT -> named '" + (staticVariables.OpponetProfile != null ? staticVariables.OpponetProfile.userName : "unknown") + "'");
                if (!MatchHandler.isOffline())
                    AssignName(staticVariables.OpponetProfile?.userName);

            }
            this.DelayUntil(() => PositionsManager.Instance, () =>
                           {
                               if (MatchHandler.isOffline())
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
                                       My_Player_Indicator.gameObject.SetActive(false);
                                       UIManager.Instance.MyLocalPlayer = gameObject;
                                       AssignName(LocalSettings.GetPlayerName());
                                   }

                                   WinningIndicator.transform.GetChild(0).localScale = UnityEngine.Vector3.one * 1.1f;
                                   SideShowIndicatorAnim.transform.GetChild(0).localScale = UnityEngine.Vector3.one * 1.1f;

                                   PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
                               }
                               BetAmountAnim.SetActive(false);
                               myCurrentBetAmountAnim.SetActive(false);
                               ResetAllPlayerKeys();
                               //if (!LocalSettings.Get_Auto_Seated_Status() && isOwned)
                               //{
                               //    StandUp();
                               //}

                               GetProfilePic();
                               if (!MatchHandler.isOffline())
                               {
                                   My_Player_Indicator.gameObject.SetActive(isOwned ? true : false);
                                   this_photonView = GetComponent<NetworkIdentity>();
                               }
                           });

        }






        void ResetAllPlayerKeys()
        {
            if (isOwned)
            {
                if (!NetworkServer.active)
                {
                    TPLog.Flow("PlayerInfo", "Clearing my saved win/loss counters for this table");
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
            playerTotalCash.transform.parent.gameObject.SetActive(true);
        }



        void playerEnterenceState()
        {
            if (RoomStateManager.Instance.IsStarted())
            {
                TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' entered while a hand was ALREADY running (room=" + RoomStateManager.Instance.CurrentRoomState + ")");
                Debug.LogError(currentPlayerStateRef.currentState);
                if (!MatchHandler.isOffline())
                {
                    if (isOwned)
                    {
                        TPLog.Flow("PlayerInfo", "It is me -> I sit out this hand (OutOfGame) and join the next one");
                        currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.OutOfGame);
                    }
                    else
                    {
                        if (MatchHandler.IsTeenPatti())
                        {
                            TPLog.Flow("PlayerInfo", "Opponent object -> reading his real state from his player property and rebuilding his cards");
                            currentPlayerStateRef.currentState = (playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState));
                            //Invoke("StartThisCoroutine", 0.5f);
                            StartCoroutine(GiveOrgCardsIfNotInGame(0.5f));
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
                TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' entered BEFORE the hand started (room=" + RoomStateManager.Instance.CurrentRoomState + ")");
                if (!MatchHandler.isOffline())
                {
                    if (MatchHandler.IsTeenPatti())
                    {
                        if (isOwned)
                        {
                            TPLog.Flow("PlayerInfo", "It is me -> I am AbleToJoin, I will play this hand");
                            currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.AbleToJoin);
                        }
                        else
                        {
                            TPLog.Flow("PlayerInfo", "Opponent object -> his own client sets his state");
                        }
                    }
                }
                else
                    currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.AbleToJoin);





            }
            if (!MatchHandler.isOffline())
            {
                if (playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState) == PlayerState.STATE.OutOfTable && !isOwned)
                {
                    TPLog.Flow("PlayerInfo", "Opponent '" + gameObject.name + "' is OutOfTable -> hiding his object on my screen");
                    Debug.Log("off from 1");
                    gameObject.SetActive(false);
                }
            }
            else
            {
                if (currentPlayerStateRef.currentState == PlayerState.STATE.OutOfTable)
                {
                    Debug.Log("off from 2");

                    gameObject.SetActive(false);
                }
            }


        }




        public void SetRoomStateFromNetworkCustomProperty()
        {
            RoomStateManager.Instance.UpdateCurrentRoomState(TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.GetRoomStateProperty(LocalSettings.roomState));
        }
        IEnumerator GiveOrgCardsIfNotInGame(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            //Debug.LogError("Aya k ahi");
            if (playerCustomProperties.GetPlayerStateProperty(LocalSettings.playerState) != PlayerState.STATE.OutOfGame)
            {
                TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' is in the running hand -> rebuilding his 3 cards from his saved card array");
                GameManager gameManager = GameManager.Instance;
                int[] OrgCardsAry = new int[3]; // Initialize OrgCardsAry with a size of 3
                OrgCardsAry[0] = 0; // Assign initial values to array elements if needed
                OrgCardsAry[1] = 0;
                OrgCardsAry[2] = 0;
                //Debug.LogError("Original Card " + TeenPattiNNetworkManager.instance.GetCustomArray(LocalSettings.OrgCardsArray).Length + " NickName is " + player.NickName);

                //Debug.LogError("Original Card " + OrgCardsAry.Length);
                yield return new WaitUntil(() => playerCustomProperties.GetCustomArray(LocalSettings.OrgCardsArray).Length > 0);
                OrgCardsAry = playerCustomProperties.GetCustomArray(LocalSettings.OrgCardsArray);
                // Rebuilt through the shared helper so the reconnect copy REPLACES what is already
                // under the parent. The old loop instantiated on top of it, so a reconnect landing
                // after the deal left six cards stacked on three anchors.
                BuildOriginalCards(this, OrgCardsAry);
            }
        }



        void PlayerSeenCardsStatus()
        {
            //  print(photonView.Controller.NickName + " : " + player.GetCustomBoolData("is_seen"));
            if (RoomStateManager.Instance.GetCurrentRoomState() == RoomState.STATE.GameIsPlaying)
            {
                if (!isOwned && currentPlayerStateRef.currentState != PlayerState.STATE.OutOfGame)
                {
                    TPLog.Flow("PlayerInfo", "Refreshing seen/blind indicator of '" + gameObject.name + "' from his property");
                    IsSeen = playerCustomProperties.GetCustomBoolData("is_seen");
                    BlindIndicator.SetActive(!IsSeen);
                    SeenIndicator.SetActive(IsSeen);
                }
            }
        }



        public void AssignNetworkSeat(int seat)
        {
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerInfo", "AssignNetworkSeat(" + seat + ") for '" + gameObject.name + "' -> sending to every client");
                // this_photonView.RPC(nameof(AssignNetworkSeatToAllInstanceOfThisPlayer), RpcTarget.AllBuffered, seat);
                CmdAssignNetworkSeatToAllInstanceOfThisPlayer(seat);
            }
            else
                AssignNetworkSeatToAllInstanceOfThisPlayer(seat);
        }
        [Command(requiresAuthority = false)]
        public void CmdAssignNetworkSeatToAllInstanceOfThisPlayer(int seat)
        {
            RpcAssignNetworkSeatToAllInstanceOfThisPlayer(seat);
        }

        [ClientRpc]
        public void RpcAssignNetworkSeatToAllInstanceOfThisPlayer(int seat)
        {
            // Implementation here
            AssignNetworkSeatToAllInstanceOfThisPlayer(seat);
        }


        //[PunRPC]
        public void AssignNetworkSeatToAllInstanceOfThisPlayer(int seat)
        {
            TPLog.Flow("PlayerInfo", "SEAT APPLIED: '" + gameObject.name + "' now sits on network seat " + seat);
            myNetworkSeat = seat;
            AdjustGameManagerLocalSeat = seat;
            Debug.Log("Seat No. " + seat);
        }

        // AssignNetworkSeatToAllInstanceOfThisPlayer only ever reaches a client through the
        // plain ClientRpc above, which the server fires once - at the moment that player spawns.
        // Mirror does not replay it, while the Photon original was RpcTarget.AllBuffered (see the
        // commented call in AssignNetworkSeat), so anyone who joins or reconnects later keeps
        // myNetworkSeat at its default 0 for every other player. Both players then resolve to
        // position_availability[0] in AssignMyLocalPositionWithAllOtherClients, the second seat
        // stays null, and GameManager.AssigningPlayersToListForDummyCards finds nobody there -
        // which is why the dealing animation skipped the opponent after a reconnect.
        // The owner writes his real seat into the synced network_position property and that DOES
        // survive a reconnect, so read the seat back from there instead of waiting for the rpc.
        IEnumerator RestoreNetworkSeatFromSyncedProperty()
        {
            float giveUpAt = Time.unscaledTime + 10f;

            while (Time.unscaledTime < giveUpAt)
            {
                if (this == null)
                    yield break;

                if (playerCustomProperties != null
                    && playerCustomProperties.customData.ContainsKey(LocalSettings.networkPosition))
                {
                    int syncedSeat = playerCustomProperties.GetCustomData(LocalSettings.networkPosition);

                    if (myNetworkSeat != syncedSeat)
                    {
                        TPLog.Flow("PlayerInfo", "Seat rpc never reached me for '" + gameObject.name + "' -> restoring seat " + myNetworkSeat + " -> " + syncedSeat + " from the synced property");
                        AssignNetworkSeatToAllInstanceOfThisPlayer(syncedSeat);
                    }
                    else
                    {
                        TPLog.Flow("PlayerInfo", "Seat of '" + gameObject.name + "' already matches the synced property (" + syncedSeat + ")");
                    }

                    if (PositionsManager.Instance != null)
                        PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();

                    yield break;
                }

                yield return null;
            }

            TPLog.Warn("PlayerInfo", "Gave up waiting for the synced seat of '" + gameObject.name + "' - he may be skipped while dealing");
        }

        [Command(requiresAuthority = false)]
        public void CmdSetCustomData(int netId, int position)
        {
            RpcSetCustomData(netId, position);
        }
        [ClientRpc]
        public void RpcSetCustomData(int netId, int position)
        {
            if ((int)this_photonView.netId == netId && this_photonView.isOwned)
            {
                TPLog.Flow("PlayerInfo", "Saving my own network position " + position + " (netId " + netId + ")");
                playerCustomProperties.SetCustomData(LocalSettings.networkPosition, position);
            }
        }
        [Server]
        public void SetCustomData(int netId, int position)
        {
            if ((int)this_photonView.netId == netId && this_photonView.isOwned)
            {
                TPLog.Flow("PlayerInfo", "Server saves network position " + position + " for netId " + netId);
                playerCustomProperties.SetCustomData(LocalSettings.networkPosition, position);
            }
        }
        public void ReAssignNetworkSeat()
        {
            // this_photonView.RPC("ReAssignNetworkSeatToAllInstanceOfThisPlayer", RpcTarget.AllBuffered, TeenPattiNNetworkManager.instance.GetCustomData(LocalSettings.networkPosition));
            CmdReAssignNetworkSeatToAllInstanceOfThisPlayer(playerCustomProperties.GetCustomData(LocalSettings.networkPosition));
        }
        [Command(requiresAuthority = false)]
        public void CmdReAssignNetworkSeatToAllInstanceOfThisPlayer(int networkPosition)
        {
            RpcReAssignNetworkSeatToAllInstanceOfThisPlayer(networkPosition);
        }

        [ClientRpc]
        public void RpcReAssignNetworkSeatToAllInstanceOfThisPlayer(int networkPosition)
        {
            // Implementation here
            ReAssignNetworkSeatToAllInstanceOfThisPlayer(networkPosition);
        }
        //[PunRPC]
        public void ReAssignNetworkSeatToAllInstanceOfThisPlayer(int seat)
        {

            myNetworkSeat = seat;
            AdjustGameManagerLocalSeat = seat;
        }



        public void StandUp()
        {
            TPLog.Flow("PlayerInfo", "STAND UP called for '" + gameObject.name + "' (state = " + getCurrentPlayerState().currentState + ", room = " + RoomStateManager.Instance.CurrentRoomState + ")");
            if (this.gameObject.name != LocalSettings.AI_Name)
            {
                PlayerStateManager.Instance.taptoSitHere.SetActive(true);

                PlayerStateManager.Instance.waitForNextRound.SetActive(false);
                PlayerStateManager.Instance.Amountobject.SetActive(false);
                "Show hoga bottom object false".Show();
            }

            if (getCurrentPlayerState().currentState == PlayerState.STATE.OutOfTable)
            {
                TPLog.Flow("PlayerInfo", "Already OutOfTable -> stand up ignored");
                return;
            }

            if (MatchHandler.IsTeenPatti())
            {

                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
                {
                    if (getCurrentPlayerState().currentState == PlayerState.STATE.ExecutingTurn)
                    {
                        int a = PlayerStateManager.Instance.SideShowNext();
                        if (PlayerStateManager.Instance.PlayingList[a].getCurrentPlayerState().currentState != PlayerState.STATE.RecieverSideShow)
                        {
                            TPLog.Flow("PlayerInfo", "Standing up on my own turn -> the turn will be passed to the next player");
                            // Debug.Log("Player Left Room Called " + PlayingList[a].player.NickName + PlayingList[a].getCurrentPlayerState().currentState);
                            getCurrentPlayerState().giveTurnToNext = true;
                        }
                        else
                        {
                            TPLog.Flow("PlayerInfo", "Standing up on my turn but the next player is answering a side-show -> turn not passed");
                        }

                    }
                    else
                    {
                        if (getCurrentPlayerState().currentState == PlayerState.STATE.RecieverSideShow)
                        {
                            TPLog.Flow("PlayerInfo", "Standing up while a side-show was waiting for my answer -> cancelling it");
                            Game_Play.Instance.OnClickCancelSideShowBtn();
                            getCurrentPlayerState().giveTurnToNext = false;
                        }
                        else
                        {
                            if (getCurrentPlayerState().currentState == PlayerState.STATE.SenderSideShow)
                            {
                                TPLog.Flow("PlayerInfo", "Standing up after asking for a side-show -> cancelling it everywhere");
                                Game_Play.Instance.OnClickCancelSideShowBtn();
                                AllSideShowPanelsFalse(false);
                                getCurrentPlayerState().giveTurnToNext = false;
                            }
                            else
                            {
                                TPLog.Flow("PlayerInfo", "Standing up outside my turn -> telling every client not to pass a turn for me");
                                getCurrentPlayerState().giveTurnToNext = false;
                                //  photonView.RPC("SetgiveTurnToNextBool", RpcTarget.All);
                                CmdSetgiveTurnToNextBool();

                            }
                        }

                    }

                }

            }

            //GoldTransfer.Instance.showMessage("You have stand up from the table");
            if (this.gameObject.name != LocalSettings.AI_Name)
                GameManager.Instance.position_availability[0].is_reserved = null;
            else
                GameManager.Instance.position_availability[1].is_reserved = null;
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerInfo", "Freeing my seat " + GameManager.Instance.myLocalSeat);
                if (MirrorNetwork.Instance.isMasterClient)
                    PositionsManager.Instance.ReleasePosition(GameManager.Instance.myLocalSeat);
                else
                    PositionsManager.Instance.ReleasePosition(GameManager.Instance.myLocalSeat);
            }
            else
            {
                if (this.gameObject.name != LocalSettings.AI_Name)
                    PositionsManager.Instance.ReleasePosition(GameManager.Instance.myLocalSeat);
                else
                    PositionsManager.Instance.ReleasePosition(GameManager.Instance.AILocalSeat);
            }

            TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' state -> OutOfTable, 'sit here' buttons shown again");
            currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.OutOfTable);
            GameManager.Instance.SitHereBtnStatus(true);
        }

        [Command(requiresAuthority = false)]
        public void CmdSetgiveTurnToNextBool()
        {
            RpcSetgiveTurnToNextBool();
        }

        [ClientRpc]
        public void RpcSetgiveTurnToNextBool()
        {
            // Implementation here
            SetgiveTurnToNextBool();
        }


        //[PunRPC]
        public void SetgiveTurnToNextBool()
        {
            TPLog.Flow("PlayerInfo", "giveTurnToNext = false for '" + gameObject.name + "' (received from network)");
            getCurrentPlayerState().giveTurnToNext = false;
        }

        // MULTIPLAYER ONLY. Called at the start of every round, before the countdown that
        // ends in the deal. Each client cleans up its OWN player, so nothing from the finished
        // round can leak into the new one and nothing depends on the master still being here.
        public void ResetMyselfForNewRound()
        {
            if (MatchHandler.isOffline() || !isOwned)
                return;

            TPLog.Flow("PlayerInfo", "Round start -> clearing my own leftovers from the finished round");

            // The master saves each player's three cards so a reconnecting client can rebuild
            // them. Nothing ever cleared that array, so a player who reconnected after a round
            // ended saw the winner's last hand rebuilt face-up. Clear my own copy here.
            playerCustomProperties.SetCustomArray(LocalSettings.OrgCardsArray, new int[0]);

            IsSeen = false;
            playerCustomProperties.SetCustomBoolData("is_seen", false);

            if (currentPlayerStateRef != null
                && currentPlayerStateRef.currentState != PlayerState.STATE.OutOfTable)
            {
                TPLog.Flow("PlayerInfo", "My state " + currentPlayerStateRef.currentState + " -> AbleToJoin for the new round");
                currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.AbleToJoin);
            }
            else
            {
                TPLog.Flow("PlayerInfo", "I am OutOfTable -> staying out of the new round");
            }
        }

        public void ActivatePlayerAgainOnNetwork()
        {

            TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' sat down again -> re-activating his object on every client");
            playerEnterenceState();
            if (!MatchHandler.isOffline())
            {
                // this_photonView.RPC("ActivatePlayerAgain", RpcTarget.All);
                CmdActivatePlayerAgain();
            }
            else
                ActivatePlayerAgain();
        }
        [Command(requiresAuthority = false)]
        public void CmdActivatePlayerAgain()
        {
            RpcActivatePlayerAgain();
        }

        [ClientRpc]
        public void RpcActivatePlayerAgain()
        {
            // Implementation here
            ActivatePlayerAgain();
        }
        //[PunRPC]
        public void ActivatePlayerAgain()
        {

            TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' object activated again (state = " + getCurrentPlayerState().currentState + ")");
            gameObject.SetActive(true);
            PackedText.gameObject.SetActive(false);

            if (getCurrentPlayerState().currentState != PlayerState.STATE.ExecutingTurn && getCurrentPlayerState().currentState != PlayerState.STATE.WaitingForTurn)
            {
                TPLog.Flow("PlayerInfo", "He is not in a live turn -> his open cards are hidden");
                PlayerOrignalCardsToShowParent.gameObject.SetActive(false);
            }

        }

        void OnDestroy()
        {
            TPLog.Warn("PlayerInfo", "PLAYER OBJECT DESTROYED: '" + gameObject.name + "' (netId " + netId + ") - he left or disconnected");
            if (MatchFlow.Enabled) MatchFlow.Log("Teen Patti", $"{gameObject.name} left the table / disconnected");
            PlayerStateManager.Instance.OnPlayerLeftRoom((int)netId);
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
            if (isOwned)
            {
                // Debug.Log(player.NickName + " With Actor " + player.ActorNumber + " is UpdatingProfilePic");
                playerCustomProperties.SetCustomData(LocalSettings.ProfilePic, LocalSettings.GetprofilePic());
                playerCustomProperties.SetCustomData(LocalSettings.ProfileFrame, LocalSettings.GetprofileFrame());
            }
        }


        public void ForAllShowTipToGirl(int dialogueNumber)
        {

            GameManager.Instance.PlayerTotalChipsUpdate(-Pot.instance.potTip);
            playerCustomProperties.CmdSetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips().ToString());


            // photonView.RPC(nameof(TipToGirl), RpcTarget.All, dialogueNumber);
            CmdTipToGirl(dialogueNumber);

        }
        [Command(requiresAuthority = false)]
        public void CmdTipToGirl(int dialogueNumber)
        {
            RpcTipToGirl(dialogueNumber);
        }

        [ClientRpc]
        public void RpcTipToGirl(int dialogueNumber)
        {
            // Implementation here
            TipToGirl(dialogueNumber);
        }


        [ShowOnly]
        public float timeDelay = 1.5f;
        //[PunRPC]
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
        // GetTotalCash() reads the cash text out of the synced player property MyTotalCashKey,
        // but nothing wrote that key before the first read: GameManager.PlayerTotalChipsUpdate()
        // only writes it while UIManager.GetMyPlayerInfo() exists, and at StartingThings() time
        // this player object has not spawned yet. The first read therefore fell through to
        // GetCustomBigIntegerData's "return 0" and the amount flashed to zero until
        // GameStarted() refreshed it ~6 seconds later. Seed the key with my real balance first.
        void SeedMyCashPropertyBeforeFirstRead()
        {
            if (MatchHandler.isOffline() || !isOwned)
            {
                TPLog.Flow("PlayerInfo", "Cash seed skipped for '" + gameObject.name + "' (offline=" + MatchHandler.isOffline() + ", mine=" + isOwned + ")");
                return;
            }

            BigInteger balance = LocalSettings.GetTotalChips();
            if (balance <= 0)
            {
                // First entry on this device: GameManager.Start() stashed the freshly fetched
                // balance under the Poker chips key, TeenPatti's own key is still empty.
                string fetched = PlayerPrefs.GetString(POKER.LocalSettings.TotalChips, "");
                if (BigInteger.TryParse(fetched, out BigInteger fetchedBalance) && fetchedBalance > 0)
                {
                    balance = fetchedBalance;
                    PlayerPrefs.SetString(LocalSettings.TotalChips, balance.ToString());
                    PlayerPrefs.Save();
                    TPLog.Flow("PlayerInfo", "TeenPatti chips key was empty -> using the fetched balance " + balance);
                }
                else
                {
                    TPLog.Warn("PlayerInfo", "No balance found in PlayerPrefs yet -> cash text may still start at 0");
                }
            }

            TPLog.Flow("PlayerInfo", "Seeding my cash property with " + balance + " so the first cash-text read does not show 0");
            playerCustomProperties.CmdSetCustomBigIntegerData(LocalSettings.MyTotalCashKey, balance.ToString());
        }

        public void MyTotalCashTextUpdate()
        {
            if (!MatchHandler.isOffline())
            {
                //\photonView.RPC(nameof(cashCheckOfAllPlayers), RpcTarget.All);
                if (NetworkServer.active)
                {
                    TPLog.Flow("PlayerInfo", "Server refreshes the cash texts locally");
                    cashCheckOfAllPlayers();
                }
                else
                {
                    TPLog.Flow("PlayerInfo", "Client asks the server to refresh everybody's cash text");
                    CmdCashCheckOfAllPlayers();
                }
            }
            else
                cashCheckOfAllPlayers();
        }

        [Command(requiresAuthority = false)]
        public void CmdCashCheckOfAllPlayers()
        {
            cashCheckOfAllPlayers();
            RpcCashCheckOfAllPlayers();
        }

        [ClientRpc]
        public void RpcCashCheckOfAllPlayers()
        {
            // Implementation here
            cashCheckOfAllPlayers();
        }
        //[PunRPC]
        void cashCheckOfAllPlayers()
        {
            StartCoroutine(GetTotalCash());
            //playerTotalCash.text = LocalSettings.Rs(player.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey));
        }

        IEnumerator GetTotalCash()
        {

            yield return new WaitForSeconds(!MatchHandler.isOffline() ? 1f : 0f);
            if (!MatchHandler.isOffline())
            {
                print("online cash");
                TPLog.Flow("PlayerInfo", "Cash text of '" + gameObject.name + "' = " + playerCustomProperties.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey));
                playerCustomProperties.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey).Show();
                playerTotalCash.text = LocalSettings.Rs(playerCustomProperties.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey));
                player_name.text = gameObject.name;
                // ab masla bata msla ye hai k multiplayer pe cash load nhi ho rhi. kuch code uncomment kro to cash load hoti but galat
                //punnetwork ma onplayerenter ki call dek 
            }
            else
            {
                print("offline cash");

                playerTotalCash.text = (this.gameObject.name == LocalSettings.AI_Name) ? LocalSettings.Rs(LocalSettings.AI_Amount) : LocalSettings.Rs(LocalSettings.GetTotalChips());

                //   player_name.text = this.gameObject.name == "AI" ? "AI" ;  ;
            }

            //  Debug.LogError(LocalSettings.Rs(player.GetCustomBigIntegerData(LocalSettings.MyTotalCashKey)));
        }


        public void AddToPot(BigInteger amount)
        {
            if (!MatchHandler.isOffline())
            {

                if (isOwned)
                {
                    if (MatchHandler.IsTeenPatti())
                    {
                        TPLog.Flow("PlayerInfo", "ADD TO POT: " + amount + " from me -> pot becomes " + (Pot.instance.potSize + amount));
                        Pot.instance.potSize += amount;
                        //Constants_M.Log("pot Size RPC" + Pot.instance.potSize);
                        //  photonView.RPC(nameof(UpdatePotSize), RpcTarget.All, Pot.instance.potSize.ToString(), ConstantsData_M.CurrentBetSpawnAmount);
                        CmdUpdatePotSize(Pot.instance.potSize.ToString(), ConstantsData_M.CurrentBetSpawnAmount);
                        TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.SetTableCollectedCashClient(LocalSettings.TableCashKey, Pot.instance.potSize);

                    }


                }
                else
                {
                    TPLog.Flow("PlayerInfo", "AddToPot(" + amount + ") ignored - '" + gameObject.name + "' is not my player, his own client adds it");
                }
            }
            else if (MatchHandler.isOffline())
            {
                Pot.instance.potSize += amount;
                UpdatePotSize(Pot.instance.potSize.ToString(), ConstantsData_M.CurrentBetSpawnAmount);
            }
            // Debug.LogError($"Add Amount {amount} PotSize {Pot.instance.potSize}");
        }

        [Command(requiresAuthority = false)]
        public void CmdUpdatePotSize(string potSize, int currentBetSpawnAmount)
        {
            TPLog.Flow("PlayerInfo", "CmdUpdatePotSize on server -> pot " + potSize + ", bet " + currentBetSpawnAmount);
            if (MatchFlow.Enabled && TeenPattiNNetworkManager.instance != null)
            {
                string flowWho = TeenPattiNNetworkManager.instance.FlowName(connectionToClient, gameObject.name);
                // Before the first turn is handed out (no actor seat yet) this is the boot seeding the pot.
                if (TeenPattiNNetworkManager.instance.CurrentActorSeat != TeenPattiNNetworkManager.TPSeat.None)
                    MatchFlow.Log(TeenPattiNNetworkManager.FlowGame, $"{flowWho} plays {(playerCustomProperties != null && playerCustomProperties.GetCustomBoolData("is_seen") ? "chaal" : "blind")} {TeenPattiNNetworkManager.instance.FlowPotDelta(potSize)} (pot {potSize})");
                else
                    MatchFlow.Log(TeenPattiNNetworkManager.FlowGame, $"boot {(Pot.instance != null ? Pot.instance.startPotAmount.ToString() : "?")} per player collected, pot {potSize}");
            }
            UpdatePotSize(potSize, currentBetSpawnAmount);
            RpcUpdatePotSize(potSize, currentBetSpawnAmount);

        }

        [ClientRpc]
        public void RpcUpdatePotSize(string potSize, int currentBetSpawnAmount)
        {
            TPLog.Flow("PlayerInfo", "RpcUpdatePotSize received -> pot " + potSize + ", bet " + currentBetSpawnAmount);
            // Implementation here
            UpdatePotSize(potSize, currentBetSpawnAmount);
        }
        bool isShowAllCards;
        //[PunRPC]
        public async void UpdatePotSize(string newPotSizeString, int CurrentAmount)
        {
            BigInteger newPotSize = BigInteger.Parse(newPotSizeString);
            var amount = CurrentAmount;
            // Pot.instance.potSize = newPotSize;
            //Pot.instance.SetCashText(Pot.instance.potSize.ToString());
            foreach (PlayerInfo item in GameManager.Instance.playersList)
            {
                item.myCurrentBetAmountAnim.SetActive(false);

            }
            await WaitForPotAmountReached(newPotSizeString);
            BigInteger ChalAmount;
            if (!MatchHandler.isOffline())
            {
                ChalAmount = ConstantsData_M.CurrentBetSpawnAmount;
            }
            else
            {
                ChalAmount = Pot.instance.CurrentChalAmount;// Mohsin
            }

            if (this.gameObject.name != "AI")
            {
                ChalAmount = amount;
            }
            else
            {
                if (UIManager.Instance.GetMyPlayerInfo().IsSeen && UIManager.Instance.GetAIPlayerInfo().IsSeen == false && Pot.instance.ChaalAmountLimit() > Pot.instance.CurrentChalAmount)
                {
                    ChalAmount = Pot.instance.CurrentChalAmount /** 2*/;

                }
                else if (UIManager.Instance.GetAIPlayerInfo().IsSeen && UIManager.Instance.GetMyPlayerInfo().IsSeen == false && Pot.instance.ChaalAmountLimit() > Pot.instance.CurrentChalAmount)
                {

                    ChalAmount = Pot.instance.CurrentChalAmount * 2;
                }
                else
                {
                    ChalAmount = ConstantsData_M.CurrentBetSpawnAmount /** 2*/;
                }

            }

            BetAmountAnimText.text = LocalSettings.Rs(ChalAmount);
            myCurrentBetAmountText.text = LocalSettings.Rs(ChalAmount);
            if (currentPlayerStateRef.currentState != PlayerState.STATE.OutOfGame)
            {
                BetAmountAnim.GetComponent<BetAmountToTargetAnim>().targetPos = Pot.instance.PotPanel.gameObject;
                myCurrentBetAmountAnim.GetComponent<BetAmountToTargetAnim>().targetPos = myCurrentBetAmountTarget;
                if (!Pot.instance.PotPanel.gameObject.activeInHierarchy)
                    Pot.instance.PotPanel.gameObject.SetActive(true);
                BetAmountAnim.SetActive(true);

            }

            // MyChaalsPlayedCounter++;


            string blindOrChal = "";
            if (IsSeen)
                blindOrChal = " Chaal of ";
            else
                blindOrChal = " Blind of ";
            if (RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.CardDistributing)
            {
                TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' played a" + blindOrChal + LocalSettings.Rs(ChalAmount));
                if (!MatchHandler.isOffline())
                    Game_Play.Instance.ShowInfo("photonView.Controller.NickName " + " played a" + blindOrChal + " " + LocalSettings.Rs(ChalAmount), 1f);
                else
                    Game_Play.Instance.ShowInfo(this.gameObject.name + " played a" + blindOrChal + " " + LocalSettings.Rs(ChalAmount), 1f);
            }

            // check if all players played
            if (Pot.instance.potSize >= Pot.instance.PotLimit)
            {
                TPLog.Flow("PlayerInfo", "POT LIMIT reached (" + Pot.instance.potSize + " >= " + Pot.instance.PotLimit + ") -> hand goes straight to the result");
                RoomStateManager.Instance.UpdateCurrentState(RoomState.STATE.WaitingForResults, LocalSettings.textStringOfPotLimitReached);
            }


        }


        //IEnumerator WaitForPotAmountReached(string amount)
        //{
        //    yield return new WaitForSeconds(0.75f);
        //    if (!isFirstCurrentChaalBool)
        //        myCurrentBetAmountAnim.SetActive(true);
        //    yield return new WaitForSeconds(0.5f);
        //    BigInteger newPotSize = BigInteger.Parse(amount);
        //    Pot.instance.potSize = newPotSize;
        //    Pot.instance.SetCashText(Pot.instance.potSize.ToString());
        //    //Debug.LogError($"{Pot.instance.potSize} Pot Cash Readed ");
        //}

        async Task WaitForPotAmountReached(string amount)
        {
            //await Task.Delay(750); // Delay for 0.75 seconds

            if (!isFirstCurrentChaalBool)
                myCurrentBetAmountAnim.SetActive(true);

            await Task.Delay(0); // Delay for 0.5 seconds

            BigInteger newPotSize = BigInteger.Parse(amount);
            Pot.instance.potSize = newPotSize;
            Pot.instance.SetCashText(Pot.instance.potSize.ToString());
            // Debug.LogError($"{Pot.instance.potSize} Pot Cash Readed ");
        }
        //[PunRPC]
        public void GiveChaalAmountOnNetoworkAtStart()
        {

            Game_Play.Instance.ShowInfo(LocalSettings.textStringOnStartBetAmount, 1f);
            BetAmountAnimText.text = LocalSettings.Rs(Pot.instance.CurrentChalAmount);
            SoundManager.Instance?.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
            if (!MatchHandler.isOffline())
            {

                if (!MirrorNetwork.Instance.isMasterClient)
                {
                    if (currentPlayerStateRef.currentState != PlayerState.STATE.OutOfTable && currentPlayerStateRef.currentState != PlayerState.STATE.OutOfGame)
                    {
                        if (isOwned)
                        {
                            if (LocalSettings.GetTotalChips() >= Pot.instance.startPotAmount)
                            {
                                TPLog.Flow("PlayerInfo", "BOOT amount -" + Pot.instance.startPotAmount + " deducted from me at the start of the hand");
                                GameManager.Instance.PlayerTotalChipsUpdate(-Pot.instance.startPotAmount);

                                UIManager.Instance.TotalBetPlacedAmount += Pot.instance.startPotAmount;
                            }
                            else
                            {
                                TPLog.Warn("PlayerInfo", "Not enough chips for the boot amount (" + LocalSettings.GetTotalChips() + " < " + Pot.instance.startPotAmount + ") -> stood up + shop opened");
                                StandUp();
                                UIManager.Instance.quickShop.SetActive(true);
                            }

                        }


                        //Debug.LogError("starting Amount  :   - " + Pot.instance.startPotAmount);
                    }


                }
            }
            else
            {
                print(" offilie");
                print(LocalSettings.GetTotalChips() + " total chips");
                if (LocalSettings.GetTotalChips() >= Pot.instance.startPotAmount)
                {
                    print(LocalSettings.GetTotalChips() + " - pot start amount ");
                    if (!MatchHandler.isOffline())
                    {
                        GameManager.Instance.PlayerTotalChipsUpdate(-Pot.instance.startPotAmount);
                        UIManager.Instance.TotalBetPlacedAmount += Pot.instance.startPotAmount;
                    }
                }
                else
                {
                    StandUp();
                    UIManager.Instance.quickShop.SetActive(true);
                }
            }
            BetAmountAnim.GetComponent<BetAmountToTargetAnim>().targetPos = Pot.instance.PotPanel.gameObject;
            BetAmountAnim.SetActive(true);


        }

        public void GiveChaalAmountOnGameStart()
        {
            //Debug.Log("Start Pot Amount Deduct" + -Pot.instance.startPotAmount);
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerInfo", "Boot amount step -> telling every client to run it");
                // photonView.RPC(nameof(GiveChaalAmountOnNetoworkAtStart), RpcTarget.All);
                CmdGiveChaalAmountOnNetoworkAtStart();
            }
            else
                GiveChaalAmountOnNetoworkAtStart();

        }

        [Command(requiresAuthority = false)]
        public void CmdGiveChaalAmountOnNetoworkAtStart()
        {
            RpcGiveChaalAmountOnNetoworkAtStart();
        }

        [ClientRpc]
        public void RpcGiveChaalAmountOnNetoworkAtStart()
        {
            // Implementation here
            GiveChaalAmountOnNetoworkAtStart();
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
                TPLog.Flow("PlayerInfo", "Sending " + RandomCardsArray.Length + " dealt card indexes to every client");
                // photonView.RPC("UpDatePlayerCardsArray", RpcTarget.All, RandomCardsArray);
                TeenPattiNNetworkManager.instance.CmdUpDatePlayerCardsArray(RandomCardsArray);
            }
            else
                UpDatePlayerCardsArray(RandomCardsArray);
        }

        //[PunRPC]
        public void UpDatePlayerCardsArray(int[] PlayerCardsArray)
        {
            OrignalCardsSetting(PlayerCardsArray);
        }

        /// <summary>Children 0-2 of PlayerOrignalCardsToShowParent are LAYOUT ANCHORS, not cards.
        /// They sit inactive in Player.prefab and all three carry the same sprite
        /// (Assets/_UI/1. GENERIC/Club-10.png), so anything that activates them makes the seat look
        /// like it is holding 10-10-10 - the identical three cards for both players, in every round.
        /// Real cards are always instantiated from index 3 upwards.</summary>
        public const int OrgCardAnchorCount = 3;

        /// <summary>Rebuilds exactly three face-up cards for `target`, throwing away whatever the
        /// previous round left under the same parent. Idempotent on purpose: the deal can be applied
        /// twice for one hand (a resend, or a reconnect snapshot landing on top of it) and this must
        /// leave three cards, not six. Old cards are un-parented before Destroy because Destroy is
        /// deferred to the end of the frame, and GameResultsManager.GettingPlayerScoreAndRank reads
        /// children 3/4/5 to score the hand - it must never see the dead ones.</summary>
        public static void BuildOriginalCards(PlayerInfo target, int[] three)
        {
            if (target == null || target.PlayerOrignalCardsToShowParent == null)
            {
                TPLog.Warn("PlayerInfo", "Deal not applied - the player or his card parent is missing");
                return;
            }

            if (three == null || three.Length < 3)
            {
                TPLog.Warn("PlayerInfo", "Deal not applied to '" + target.name + "' - got "
                    + (three == null ? 0 : three.Length) + " card indexes instead of 3");
                return;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.AllCards == null || gameManager.AllCards.Card == null)
            {
                TPLog.Warn("PlayerInfo", "Deal not applied to '" + target.name + "' - the card deck is not loaded");
                return;
            }

            Transform parent = target.PlayerOrignalCardsToShowParent.transform;
            if (parent.childCount < OrgCardAnchorCount)
            {
                TPLog.Warn("PlayerInfo", "Deal not applied to '" + target.name + "' - only "
                    + parent.childCount + " position anchors under the card parent");
                return;
            }

            for (int c = parent.childCount - 1; c >= OrgCardAnchorCount; c--)
            {
                Transform stale = parent.GetChild(c);
                stale.SetParent(null, false);
                Destroy(stale.gameObject);
            }

            for (int c = 0; c < 3; c++)
            {
                int index = three[c];
                if (index < 0 || index >= gameManager.AllCards.Card.Length)
                {
                    TPLog.Warn("PlayerInfo", "Deal not applied to '" + target.name + "' - card index "
                        + index + " is outside the " + gameManager.AllCards.Card.Length + " card deck");
                    return;
                }

                GameObject card = Instantiate(gameManager.AllCards.Card[index].gameObject);
                RectTransform rt = parent.GetChild(c).gameObject.GetComponent<RectTransform>();
                LocalSettings.SetPosAndRect(card, rt, parent);
            }

            TPLog.Flow("PlayerInfo", "Built cards " + three[0] + "/" + three[1] + "/" + three[2]
                + " for '" + target.name + "'");
        }

        /// <summary>OFFLINE / AI path only. Online, the server addresses each hand by its owner's
        /// netId (TeenPattiNNetworkManager.RpcDealHandsToOwners) rather than by position in
        /// PlayingList: that list is rebuilt locally from synced state, so when the deal arrived it
        /// could still be short - the editor log has deals that built cards "for 1 players" while the
        /// server had dealt to 2, which left the other seat showing nothing but the Club-10 anchors -
        /// and its ORDER is local too, so index j did not have to mean the same player on both
        /// devices.</summary>
        public void OrignalCardsSetting(int[] cardsArray)
        {
            "Original card setting called".Show();
            PlayerStateManager psm = PlayerStateManager.Instance;
            TPLog.Flow("PlayerInfo", "Building the real cards for " + psm.PlayingList.Count + " players from the dealt array");
            psm.PlayingList.Count.Show();

            if (cardsArray == null || cardsArray.Length < psm.PlayingList.Count * 3)
            {
                TPLog.Warn("PlayerInfo", "Deal has " + (cardsArray == null ? 0 : cardsArray.Length)
                    + " indexes but " + psm.PlayingList.Count + " players are in the hand - not building any cards");
                return;
            }

            for (int j = 0; j < psm.PlayingList.Count; j++)
            {
                BuildOriginalCards(psm.PlayingList[j], new int[]
                {
                    cardsArray[(j * 3) + 0],
                    cardsArray[(j * 3) + 1],
                    cardsArray[(j * 3) + 2]
                });
            }


            // The reconnect copy of each player's cards used to be written here, by whichever client
            // held isMasterClient, by reading it back out of the instantiated card GameObjects. The
            // server writes it directly in TeenPattiNNetworkManager.ServerDealHand now, straight from
            // the canonical deal - so it no longer depends on a particular phone still being in the
            // match, and no longer round-trips real money state through a client.


            //ShowCardsFromBlind();
        }

        public void ShowCardsFromBlind()
        {
            Pot.instance.CurrentChalAmount.Show();
            if (this.gameObject.name != LocalSettings.AI_Name)
            {
                PlayerDummyCardsToShowParent.gameObject.SetActive(false);
                PlayerOrignalCardsToShowParent.SetActive(true);

                // This used to activate EVERY child, anchors included. Children 0-2 are position
                // markers that all carry the Club-10 sprite, so a seat whose real cards had not been
                // built showed three tens - the same hand for both players and the same hand every
                // round, which is exactly the "same cards three rounds running" report. Keep the
                // anchors down and reveal only what was actually dealt.
                Transform orgParent = PlayerOrignalCardsToShowParent.transform;
                for (int i = 0; i < OrgCardAnchorCount && i < orgParent.childCount; i++)
                    orgParent.GetChild(i).gameObject.SetActive(false);

                if (orgParent.childCount <= OrgCardAnchorCount)
                    TPLog.Warn("PlayerInfo", "SEEN: '" + gameObject.name + "' has no real cards spawned -> nothing to reveal");

                for (int i = OrgCardAnchorCount; i < orgParent.childCount; i++)
                {
                    if (!orgParent.GetChild(i).gameObject.activeInHierarchy)
                        orgParent.GetChild(i).gameObject.SetActive(true);

                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.CardFlip, false);
                }
            }
            TPLog.Flow("PlayerInfo", "SEEN: '" + gameObject.name + "' opened his cards");
            IsSeen = true;
            if (!MatchHandler.isOffline())
                playerCustomProperties.SetCustomBoolData("is_seen", true);

            if (this.gameObject.name != LocalSettings.AI_Name)
                UIManager.Instance.ChaalTypeText.text = "Chaal";
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerInfo", "Telling every client that '" + gameObject.name + "' is now seen");
                //  photonView.RPC(nameof(SeenAlert), RpcTarget.All, IsSeen);
                CmdSeenAlert(IsSeen);
                if (isOwned)
                {
                    TPLog.Flow("PlayerInfo", "It was me -> refreshing my SHOW / SIDE-SHOW buttons");
                    PlayerStateManager.Instance.SideShowAndShowbtn();
                }
            }
            else
            {
                SeenAlert(IsSeen);
                PlayerStateManager.Instance.SideShowAndShowbtn();
            }




        }

        [Command(requiresAuthority = false)]
        public void CmdSeenAlert(bool isSeen)
        {
            if (MatchFlow.Enabled && isSeen && TeenPattiNNetworkManager.instance != null && TeenPattiNNetworkManager.instance.FlowFirstSeen(connectionToClient))
                MatchFlow.Log(TeenPattiNNetworkManager.FlowGame, $"{TeenPattiNNetworkManager.instance.FlowName(connectionToClient, gameObject.name)} sees the cards");
            RpcSeenAlert(isSeen);
        }

        [ClientRpc]
        public void RpcSeenAlert(bool isSeen)
        {
            // Implementation here
            SeenAlert(isSeen);
        }
        public void GiveSeenAlertToAll()
        {
            // photonView.RPC("SeenAlert", RpcTarget.All, IsSeen);
            CmdSeenAlert(IsSeen);
        }


        //[PunRPC]
        public void SeenAlert(bool seen)
        {
            TPLog.Flow("PlayerInfo", "SeenAlert(" + seen + ") received for '" + gameObject.name + "'");
            if (seen)
            {
                IsSeen = seen;
                BlindIndicator.SetActive(false);
                SeenIndicator.SetActive(true);
                if (!MatchHandler.isOffline())
                {
                    if (Pot.instance.ChaalAmountLimit() != Pot.instance.CurrentChalAmount && isOwned)
                    {
                        TPLog.Flow("PlayerInfo", "I am seen now -> my displayed chaal is doubled to " + (Pot.instance.CurrentChalAmount * 2));
                        UIManager.Instance.UpDateCurrentChalAmountText(Pot.instance.CurrentChalAmount * 2);
                    }
                }
                else
                {
                    if (Pot.instance.ChaalAmountLimit() != Pot.instance.CurrentChalAmount)
                    {
                    }
                    if (UIManager.Instance.GetMyPlayerInfo().IsSeen && UIManager.Instance.GetAIPlayerInfo().IsSeen == false && Pot.instance.ChaalAmountLimit() > Pot.instance.CurrentChalAmount)
                    {
                        UIManager.Instance.UpDateCurrentChalAmountText(Pot.instance.CurrentChalAmount * 2);

                    }
                    else if (UIManager.Instance.GetMyPlayerInfo().IsSeen && UIManager.Instance.GetAIPlayerInfo().IsSeen && Pot.instance.ChaalAmountLimit() > Pot.instance.CurrentChalAmount)
                    {
                        UIManager.Instance.UpDateCurrentChalAmountText(Pot.instance.CurrentChalAmount * 2);

                    }
                    else if (UIManager.Instance.GetAIPlayerInfo().IsSeen && UIManager.Instance.GetMyPlayerInfo().IsSeen == false && Pot.instance.ChaalAmountLimit() > Pot.instance.CurrentChalAmount)
                    {
                        UIManager.Instance.UpDateCurrentChalAmountText(Pot.instance.CurrentChalAmount);

                    }
                    else
                    {
                        UIManager.Instance.UpDateCurrentChalAmountText(Pot.instance.CurrentChalAmount);
                    }
                }
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
                            TPLog.Flow("PlayerInfo", "Both of us are seen and it is my turn -> SIDE-SHOW button enabled");
                            UIManager.Instance.sideShowBtn.interactable = true;
                        }
                    }
                }
            }
        }

        public void GivesideShowAlertToAll(bool sideShow)
        {
            TPLog.Flow("PlayerInfo", "Side-show flag set to " + sideShow + " and sent to every client");
            IsSideShow = sideShow;
            //photonView.RPC("SideShowAlert", RpcTarget.AllBuffered, IsSideShow);
            if (!MatchHandler.isOffline())
            {
                //photonView.RPC("SideShowAlert", RpcTarget.All, IsSideShow);
                CmdSideShowAlert(IsSideShow);
            }
            else
                SideShowAlert(sideShow);
        }
        [Command(requiresAuthority = false)]
        public void CmdSideShowAlert(bool isSideShow)
        {
            if (MatchFlow.Enabled && isSideShow && TeenPattiNNetworkManager.instance != null)
                MatchFlow.Log(TeenPattiNNetworkManager.FlowGame, $"{TeenPattiNNetworkManager.instance.FlowName(connectionToClient, gameObject.name)} asks for a side-show (pot {TeenPattiNNetworkManager.instance.FlowPot()})");
            RpcSideShowAlert(isSideShow);
        }

        [ClientRpc]
        public void RpcSideShowAlert(bool isSideShow)
        {
            // Implementation here
            SideShowAlert(isSideShow);
        }

        //[PunRPC]
        public void SideShowAlert(bool setbool)
        {
            TPLog.Flow("PlayerInfo", "SideShowAlert(" + setbool + ") received -> applying it to all " + PlayerStateManager.Instance.PlayingList.Count + " players in the hand");
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
            //photonView.RPC("SideShowAlert", RpcTarget.AllBuffered, IsSideShow);
            //  photonView.RPC("SideShowPanelsFalse", RpcTarget.All, sideShow);
            CmdSideShowPanelsFalse(sideShow);
        }

        [Command(requiresAuthority = false)]
        public void CmdSideShowPanelsFalse(bool sideShow)
        {
            RpcSideShowPanelsFalse(sideShow);
        }

        [ClientRpc]
        public void RpcSideShowPanelsFalse(bool sideShow)
        {
            // Implementation here
            SideShowPanelsFalse(sideShow);
        }
        //[PunRPC]
        public void SideShowPanelsFalse(bool setbool)
        {

            TPLog.Flow("PlayerInfo", "Side-show panel visibility forced to " + setbool);
            UIManager.Instance.sideShowPanel.SetActive(setbool);
        }

        #endregion

        public void CurrentChalAmountSendToAllPlayers(BigInteger ModifiedChalAmount)
        {
            if (!MatchHandler.isOffline())
            {
                if (isOwned)
                {

                    TPLog.Flow("PlayerInfo", "I changed the chaal amount to " + ModifiedChalAmount + " -> sending it to every client");
                    Pot.instance.CurrentChalAmount = ModifiedChalAmount;
                    //  photonView.RPC(nameof(UpdateAllPlayersCurrentChallAmount), RpcTarget.All, Pot.instance.CurrentChalAmount.ToString());
                    CmdUpdateAllPlayersCurrentChallAmount(Pot.instance.CurrentChalAmount.ToString());
                }
            }
            else
            {
                UpdateAllPlayersCurrentChallAmount(ModifiedChalAmount.ToString());
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdUpdateAllPlayersCurrentChallAmount(string currentChalAmount)
        {
            if (MatchFlow.Enabled && TeenPattiNNetworkManager.instance != null)
                MatchFlow.Log(TeenPattiNNetworkManager.FlowGame, $"{TeenPattiNNetworkManager.instance.FlowName(connectionToClient, gameObject.name)} sets the chaal stake to {currentChalAmount}");
            RpcUpdateAllPlayersCurrentChallAmount(currentChalAmount);
        }

        [ClientRpc]
        public void RpcUpdateAllPlayersCurrentChallAmount(string currentChalAmount)
        {
            // Implementation here
            UpdateAllPlayersCurrentChallAmount(currentChalAmount);
        }
        //[PunRPC]
        public void UpdateAllPlayersCurrentChallAmount(string ChalAmountUpdatedString)
        {
            BigInteger ChalAmountUpdated = BigInteger.Parse(ChalAmountUpdatedString);
            TPLog.Flow("PlayerInfo", "Chaal amount received from network: " + ChalAmountUpdated);
            print(" :   Current ChallAmount  : " + ChalAmountUpdated);

            Pot.instance.CurrentChalAmount = ChalAmountUpdated;
            print(" :  pot Players Current ChallAmount  : " + Pot.instance.CurrentChalAmount);
            // Debug.LogError(Pot.instance.CurrentChalAmount + " 1");
            BigInteger betAmount = Pot.instance.CurrentChalAmount;
            if (IsSeen)
            {
                TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' is seen -> his displayed bet is doubled");
                betAmount = Pot.instance.CurrentChalAmount * 2;
            }
            UIManager.Instance.UpDateCurrentChalAmountText(betAmount);
            print(" :  ChallAmount  : " + betAmount);
        }



        public void StartPotAmount()
        {
            //AddToPot(Pot.instance.startPotAmount * PhotonNetwork.CurrentRoom.PlayerCount);
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("PlayerInfo", "Seeding the pot with the boot amount of all " + PlayerStateManager.Instance.PlayingList.Count + " players");
                AddToPot(Pot.instance.startPotAmount * PlayerStateManager.Instance.PlayingList.Count);
            }
            else
                AddToPot(Pot.instance.startPotAmount);
            if (!MatchHandler.isOffline())
            {
                if (currentPlayerStateRef.currentState != PlayerState.STATE.OutOfTable && currentPlayerStateRef.currentState != PlayerState.STATE.OutOfGame && isOwned)
                {
                    TPLog.Flow("PlayerInfo", "My boot amount -" + Pot.instance.startPotAmount + " deducted");
                    GameManager.Instance.PlayerTotalChipsUpdate(-Pot.instance.startPotAmount);
                    UIManager.Instance.TotalBetPlacedAmount += Pot.instance.startPotAmount;
                }
                else
                {
                    TPLog.Flow("PlayerInfo", "Boot amount not deducted here (state = " + currentPlayerStateRef.currentState + ", mine = " + isOwned + ")");
                }
            }
            else
            {
                if (currentPlayerStateRef.currentState != PlayerState.STATE.OutOfTable && currentPlayerStateRef.currentState != PlayerState.STATE.OutOfGame)
                {
                    if (this.gameObject.name != LocalSettings.AI_Name)
                    {
                        GameManager.Instance.PlayerTotalChipsUpdate(-Pot.instance.startPotAmount);
                        UIManager.Instance.TotalBetPlacedAmount += Pot.instance.startPotAmount;
                    }
                    else
                    {
                        GameManager.Instance.AITotalChipsUpdate(-Pot.instance.startPotAmount);
                    }
                }
            }

        }



        public void GetProfilePic()
        {
            StartCoroutine(waitforLoadPlayerAvatar());
        }

        IEnumerator waitforLoadPlayerAvatar()
        {
            yield return new WaitForSeconds(0.3f);
            if (!MatchHandler.isOffline())
            {
                if (isOwned)
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
                if (this.gameObject.name != LocalSettings.AI_Name)
                {
                    if (staticVariables.ProfilePicture != null)
                        PlayerAvatorImage.sprite = Sprite.Create(staticVariables.ProfilePicture, new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height), UnityEngine.Vector2.zero);// GameManager.Instance.PlayerProfileImages.Sprites[index];
                    else
                        PlayerAvatorImage.sprite = GameManager.Instance.PlayerProfileImages.Sprites[UnityEngine.Random.Range(0, GameManager.Instance.PlayerProfileImages.Sprites.Length)];
                }
                else
                {
                    PlayerAvatorImage.sprite = GameManager.Instance.PlayerProfileImages.Sprites[UnityEngine.Random.Range(0, GameManager.Instance.PlayerProfileImages.Sprites.Length)];

                }
            }
        }


        public void UpdateScore()
        {
            if (isOwned)
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
                TPLog.Flow("PlayerInfo", "IAmWinner(" + winner + ") for '" + gameObject.name + "' -> sending the result to every client");
                //photonView.RPC(nameof(RPCWinner), RpcTarget.All, winner);
                CmdRPCWinner(winner);
            }
            else
            {
                Debug.Log("Decalere winner  : " + gameObject.name + winner);
                RPCWinner(winner);
            }


        }
        [Command(requiresAuthority = false)]
        public void CmdRPCWinner(bool winner)
        {
            RpcRPCWinner(winner);
        }

        [ClientRpc]
        public void RpcRPCWinner(bool winner)
        {
            // Implementation here
            RPCWinner(winner);
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

                    if (!GameManager.Instance.isRunINBackGround)
                    {
                        // Toaster.ShowAToast("Winner Indicator..." + GameManager.Instance.isRunINBackGround);
                        WinningIndicator.SetActive(true);
                        Pot pot = Pot.instance;
                        pot.winAmountText.text = LocalSettings.Rs(pot.potSize);

                        if (MatchHandler.isOffline())
                        {
                            if (this.gameObject.name == LocalSettings.AI_Name)
                            {
                                //LocalSettings.AI_Amount += pot.potSize;
                                playerTotalCash.text = LocalSettings.Rs(LocalSettings.AI_Amount);
                            }
                        }




                        //Debug.LogError("Check here...." + LocalSettings.Rs(PokerActionPanel.Instance.TotalPotAmount()));
                        pot.winAmountAnim.GetComponent<BetAmountToTargetAnim>().targetPos = WinningIndicator;
                        pot.winAmountAnim.SetActive(true);

                        if (MatchHandler.isOffline())
                        {
                            if (isOwned)
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

        //[PunRPC]
        public void RPCWinner(bool net_winner)
        {
            TPLog.Flow("PlayerInfo", "RESULT received: '" + gameObject.name + "' winner=" + net_winner);
            isWinner = net_winner;

            if (staticVariables.isTeenPattiFinished)
            {
                TPLog.Flow("PlayerInfo", "Result already reported to the backend for this hand -> skipping");
                return;
            }
            ConstantsData_M.Log(staticVariables.UserProfiledata.user.silver_balance);
            PlayerInfo firstplayer;
            Dictionary<string, string> data;
            if (!net_winner)
            {
                TPLog.Flow("PlayerInfo", "'" + gameObject.name + "' lost -> nothing to report to the backend from here");
                return;
            }
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

                    StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.End_RoundAI_casino_Url, data));//Mohsin line
                }
            }
            else
            {
                if (isOwned)
                {
                    TPLog.Flow("PlayerInfo", "I am the winner -> posting the round result to the backend (pot " + Pot.instance.potSize + ")");
                    firstplayer = MirrorNetwork.Instance.isMasterClient ? this : GameManager.Instance.playersList.Find(player => player != this);
                    PlayerInfo Secondplayer = MirrorNetwork.Instance.isMasterClient ? GameManager.Instance.playersList.Find(player => player != this) : this;
                    var val = isOwned && net_winner ? staticVariables.UserProfiledata.user._id.ToString() : staticVariables.OpponetProfile?.userId;

                    //Constants_M.Log($"winner amount {Pot.instance.potSize} first {(firstplayer.player.GetCustomBigIntegerData("CashInHand") - BigInteger.Parse(firstplayer.playerTotalCash.text.Trim()))} Second{(Secondplayer.player.GetCustomBigIntegerData("CashInHand") - BigInteger.Parse(Secondplayer.playerTotalCash.text.Trim()))} Winner ID {val}");
                    data = new Dictionary<string, string>
            {
                { "round_id", staticVariables.CurrentRoundID },
                { "winner_id",val},
                { "winner_amount", Pot.instance.potSize.ToString() },
               { "first_player_amount", (firstplayer.playerCustomProperties.GetCustomBigIntegerData("CashInHand") - BigInteger.Parse(firstplayer.playerTotalCash.text.Trim())).ToString() },
                { "second_player_amount",  (Secondplayer.playerCustomProperties.GetCustomBigIntegerData("CashInHand") - BigInteger.Parse(Secondplayer.playerTotalCash.text.Trim())).ToString()},
            };
                    StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.End_Round_casino_Url, data, withSettlementAuth: true));//Mohsin line
                }
                else
                {
                    TPLog.Flow("PlayerInfo", "The winner is the other player -> his client posts the result");
                }

                //Constants_M.Log("link " + gameObject.name == "AI" ? ServerCall.End_RoundAI_casino_Url : ServerCall.End_Round_casino_Url);
                //StartCoroutine(ServerCall.PostRequest(gameObject.name == "AI" ? ServerCall.End_RoundAI_casino_Url : ServerCall.End_Round_casino_Url, data)); //Mohsin
                staticVariables.isTeenPattiFinished = true;

            }
        }

        private void OnDisable()
        {

            TPLog.Flow("PlayerInfo", "Player object DISABLED: '" + gameObject.name + "' (seat " + myNetworkSeat + ")");
            if (MirrorNetwork.Instance.isMasterClient)
            {
                TPLog.Flow("PlayerInfo", "Master client -> freeing seat " + myNetworkSeat);
                // Release the position if the player was the master client
                PositionsManager.Instance.ReleasePosition(myNetworkSeat);
            }
            if (GameStartManager.Instance != null)
            {
                GameStartManager.Instance.AddOrRemovePlayer(-1);
            }
            else
            {
                TPLog.Warn("PlayerInfo", "GameStartManager already gone -> player count not decreased");
            }
            ShowBtn.SetActive(false);
            PlayerOrignalCardsToShowParent.SetActive(false);
            PlayerDummyCardsToShowParent.SetActive(false);
            SeenIndicator.SetActive(false);

            BlindIndicator.SetActive(false);

        }

        #region MasterChange When GameRunINBackGround
        [ShowOnly] public bool checkApplicationBackground;

        public void playerBackGround(bool isTrue)
        {
            //Toaster.ShowAToast("Check GameManager..." + isTrue);
            // photonView.RPC(nameof(playerRunInBackGroundRPC), RpcTarget.All, isTrue);
            CmdPlayerRunInBackGroundRPC(isTrue);
        }
        [Command(requiresAuthority = false)]
        public void CmdPlayerRunInBackGroundRPC(bool isTrue)
        {
            RpcPlayerRunInBackGroundRPC(isTrue);
        }

        [ClientRpc]
        public void RpcPlayerRunInBackGroundRPC(bool isTrue)
        {
            // Implementation here
            playerRunInBackGroundRPC(isTrue);
        }

        //[PunRPC]
        void playerRunInBackGroundRPC(bool isTrue)
        {
            TPLog.Warn("PlayerInfo", "'" + gameObject.name + "' background flag = " + isTrue);
            checkApplicationBackground = isTrue;

            //Toaster.ShowAToast("Check Player....." + checkApplicationBackground);
        }
        #endregion




    }
}
