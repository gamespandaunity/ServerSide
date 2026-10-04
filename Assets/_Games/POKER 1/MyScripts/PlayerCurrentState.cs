using Mirror;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Numerics;
using UnityEngine;

namespace POKER
{
    public class PlayerCurrentState : NetworkBehaviour
    {
        public PlayerState.STATE currentState;

        PlayerInfo playerInfo;
        PlayerCurrentAnim playerAnim;
        UIManager uiManager;
        public bool giveTurnToNext;

        private void OnEnable()
        {
            playerInfo = GetComponent<PlayerInfo>();
            playerAnim = GetComponent<PlayerCurrentAnim>();
            uiManager = UIManager.Instance;
            giveTurnToNext = true;
        }


        int counter = 0;
        void OnUpdateCurrentState(PlayerState.STATE state)
        {
            switch (state)
            {
                case PlayerState.STATE.OutOfGame:
                    TriggerStateOutofGame();
                    break;

                case PlayerState.STATE.Watching:
                    TriggerStateWatching();
                    break;

                case PlayerState.STATE.AbleToJoin:
                    TriggerAbleToJoin();
                    break;

                case PlayerState.STATE.WaitingForTurn:
                    //Debug.LogError("  ex  ------------------- " + counter++);
                    TriggerWaitingForTurn();

                    break;

                case PlayerState.STATE.ExecutingTurn:
                    TriggerStateExecutingTurn();
                    break;

                case PlayerState.STATE.BetPlaced:
                    TriggerStateBetPlaced();
                    break;

                case PlayerState.STATE.Packed:
                    TriggerStatePacked();
                    break;

                case PlayerState.STATE.RecieverSideShow:
                    TriggerStateRecieverSideShow();
                    break;
                case PlayerState.STATE.SenderSideShow:
                    //Debug.LogError("  ex  ------------------- " + counter++);
                    TriggerStateSenderSideShow();

                    break;

                case PlayerState.STATE.OutOfTable:
                    TriggerStateOutOfTable();
                    break;
            }
        }

        public PlayerState.STATE GetCurrentState()
        {
            return currentState;
        }

        public void UpdateCurrentPlayerState(PlayerState.STATE state)
        {
            if (!MatchHandler.isOffline())
            {
                playerInfo.playerCustomProperties.SetPlayerStateProperty(LocalSettings.playerState, (int)state);
                if (NetworkServer.active)
                {
                    // Server updates its own state and broadcasts to all clients
                    Debug.Log($"[UpdateState] SERVER setting {state} on {gameObject.name} (ownerPlayerId={playerInfo.ownerPlayerId})");
                    currentState = state;
                    OnUpdateCurrentState(state);
                    Debug.Log($"[UpdateState] SERVER about to send ClientRpc for {state}");
                    UpdateCurrentPlayerStateOnNetwork(state);
                    Debug.Log($"[UpdateState] SERVER ClientRpc sent for {state}");
                }
                else
                {
                    Debug.Log($"[UpdateState] CLIENT sending Cmd for {state} on {gameObject.name}");
                    CmdUpdatePlayerState(state);
                }
            }
            else
            {
                UpdateCurrentPlayerStateAI(state);
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdUpdatePlayerState(PlayerState.STATE state)
        {
            playerInfo.playerCustomProperties.SetPlayerStateProperty(LocalSettings.playerState, (int)state);
            currentState = state;
            OnUpdateCurrentState(state);
            UpdateCurrentPlayerStateOnNetwork(state);
        }

        [ClientRpc]
        public void UpdateCurrentPlayerStateOnNetwork(PlayerState.STATE state)
        {
            Debug.Log($"[UpdateState] ClientRpc received: {state} on {gameObject.name}, isServer={NetworkServer.active}");
            currentState = state;
            OnUpdateCurrentState(state);
        }
        public void UpdateCurrentPlayerStateAI(PlayerState.STATE state)
        {
            currentState = state;
            OnUpdateCurrentState(state);
        }


        public void TriggerStateOutofGame()
        {
            if (playerInfo == null)
                playerInfo = GetComponent<PlayerInfo>();

            // PlayerStateManager.Instance.PlayingList.Clear();

            playerInfo.PlayerStateText.text = "Out Of Game";
            if (playerInfo.IsMine())
            {


                if (MatchHandler.IsPoker())
                {
                    // Full deck not needed on client — community cards available via syncedCommunityCards

                    playerInfo.HandRankLabelTxt.transform.parent.gameObject.SetActive(false);

                    // Generate community cards for out-of-game spectating using synced data
                    if (!string.IsNullOrEmpty(NetworkGameManager.Instance.syncedCommunityCards))
                        PokerManager.Instance.GenerateCommunityCardsForOutOfGame();

                    // Show all revealed community cards regardless of room state —
                    // the old GameIsPlaying/ABFirstTurn guard was too restrictive and
                    // hid cards when reconnecting during WaitingForResults / ShowingResults.
                    int dropIdx = NetworkGameManager.Instance.syncedDropCardsIndex;
                    if (dropIdx > 0)
                        PokerManager.Instance.AssignCummintyCardToOutOFGame(dropIdx);
                }

                if (RoomStateManager.Instance.CurrentRoomState != RoomState.STATE.ShowingResults)
                    GameStartManager.Instance.GameStarWaitTextGameObject.SetActive(false);

                PlayerStateManager.Instance.Amountobject.SetActive(false);
                PlayerStateManager.Instance.taptoSitHere.SetActive(false);
                PlayerStateManager.Instance.waitForNextRound.SetActive(true);



            }



            //GameManager gm = GameManager.Instance;
            //for (int i = 0; i < gm.playersList.Count; i++)
            //{
            //    if (gm.playersList[i].GetComponent<PlayerCurrentState>().currentState != PlayerState.STATE.OutOfGame)
            //    {
            //        PlayerStateManager.Instance.PlayingList.Add(gm.playersList[i]);
            //    }
            //}


        }





        public void TriggerStateWatching()
        {

            if (playerInfo.IsSideShow && playerInfo.IsMine())
            {
                int nextPlayerInt = PlayerStateManager.Instance.SideShowPrev();

                PlayerStateManager.Instance.PlayingList[nextPlayerInt].ShowCardsFromBlind();
            }
            playerInfo.PlayerStateText.text = "Watching";
            Debug.Log("-------- Watching State");
            //Play 
        }

        public void TriggerAbleToJoin()
        {
            if (playerInfo == null)
                playerInfo = GetComponent<PlayerInfo>();



            PlayerStateManager.Instance.waitForNextRound.SetActive(false);
            PlayerStateManager.Instance.taptoSitHere.SetActive(false);
            if (!MatchHandler.isOffline())
            {
                if (playerInfo.IsMine())
                    PlayerStateManager.Instance.Amountobject.SetActive(true);
            }
            else
            {
                if (playerInfo.gameObject.name != LocalSettings.AI_Name)
                    PlayerStateManager.Instance.Amountobject.SetActive(true);
            }

            playerInfo.FillerImage.gameObject.SetActive(false);
            playerInfo.PlayerStateText.text = "Able To Join";
            if (!MatchHandler.isOffline())
                playerInfo.HandRankLabelTxt.transform.parent.gameObject.SetActive(playerInfo.IsMine());
            else
                playerInfo.HandRankLabelTxt.transform.parent.gameObject.SetActive(playerInfo.gameObject.name != LocalSettings.AI_Name ? true : false);


        }

        public void TriggerWaitingForTurn()
        {
            //if (photonView.IsMine)
            //{
            //    if (uiManager.ActionTable.activeSelf)
            //    {
            //        Debug.LogError("Action panel trouble  1 ----------------------------------------");
            //        uiManager.ActionTable.SetActive(false);
            //    }
            //}
            playerInfo.FillerImage.gameObject.SetActive(false);
            playerInfo.FillerImage.fillAmount = 0;
            Debug.Log("-------- Waiting For Turn State");
            playerInfo.PlayerStateText.text = "Waiting For Turn";

        }


        // A disconnect can leave another player's isBetPlacedPocker stuck at true: the flag lives on
        // each client's own copy of that player and is cleared by whichever client drives the betting
        // round forward - which may be the client that just vanished. The old
        // WaitUntil(PlayingList.All(x => !x.isBetPlacedPocker)) then waited forever, so the next
        // player's action panel never appeared (and with the server turn backstop in place that
        // player would eventually be folded for a stall that was not their fault). It also threw as
        // soon as any list entry had been destroyed. Wait null-safely and give up after a bounded
        // time, so a hand always moves on.
        const float betsSettledTimeoutSeconds = 6f;

        IEnumerator WaitForBetsToSettle()
        {
            float waited = 0f;
            while (waited < betsSettledTimeoutSeconds)
            {
                bool allCleared = true;
                foreach (PlayerInfo p in PlayerStateManager.Instance.PlayingList)
                {
                    if (p == null) continue;               // destroyed during a disconnect
                    if (p.isBetPlacedPocker)
                    {
                        allCleared = false;
                        break;
                    }
                }

                if (allCleared)
                    yield break;

                waited += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning($"[Poker] Bets did not clear within {betsSettledTimeoutSeconds}s - a disconnected " +
                             $"player can leave isBetPlacedPocker set, so continuing instead of waiting forever.");
        }

        public void TriggerStateExecutingTurn()
        {


            if (playerInfo != null)
            {
                Debug.Log($"[ExecutingTurn] TriggerStateExecutingTurn on {gameObject.name}, IsMine={playerInfo.IsMine()}, ownerPlayerId={playerInfo.ownerPlayerId}, isServer={NetworkServer.active}");

                // BUG FIX: online turns never reset FillerImage.fillAmount to 1 (offline/AI
                // always does, inside MethodOfExecutingTurn). If the filler bar was left
                // active/near-zero from this player's previous turn, PlayerTurnManager.Update()
                // saw it "already empty" on frame one of the NEW turn and force-called
                // StandUp() before the player could act — looked like an instant fold/leave
                // right after the opponent raised. Reset it here, once, exactly when the
                // turn is actually assigned.
                if (!MatchHandler.isOffline())
                {
                    if (playerInfo.FillerImage != null)
                    {
                        Debug.Log($"[ExecutingTurn] FillerImage reset for {gameObject.name}: fillAmount was {playerInfo.FillerImage.fillAmount}, now 1");
                        playerInfo.FillerImage.fillAmount = 1;
                        playerInfo.FillerImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning($"[ExecutingTurn] FillerImage is NULL for {gameObject.name} — cannot reset turn timer, PlayerTurnManager.Update() will read a stale/default value");
                    }
                }

                StartCoroutine(MethodOfExecutingTurn());
                if (playerInfo.IsMine())
                    Debug.Log("-------- Executing Turn State");

            }


        }
        IEnumerator MethodOfExecutingTurn()
        {
            Debug.Log($"[ExecutingTurn] MethodOfExecutingTurn START: PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count}, IsMine={playerInfo.IsMine()}, RoomState={RoomStateManager.Instance.CurrentRoomState}, currentState={currentState}, isServer={NetworkServer.active}, ownerPlayerId={playerInfo.ownerPlayerId}");
            // Defensive wait: if PlayingList isn't populated yet, wait up to 10 frames
            if (PlayerStateManager.Instance.PlayingList.Count <= 1)
            {
                Debug.LogWarning($"[ExecutingTurn] PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count} <= 1, waiting...");
                for (int wait = 0; wait < 10 && PlayerStateManager.Instance.PlayingList.Count <= 1; wait++)
                    yield return null;
                Debug.Log($"[ExecutingTurn] After wait: PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count}");
            }
            if (!MatchHandler.isOffline())
            {
                bool isMine = playerInfo.IsMine();
                var roomState = RoomStateManager.Instance.CurrentRoomState;
                bool roomStateOK = roomState == RoomState.STATE.GameIsPlaying || roomState == RoomState.STATE.ABFirstTurn;

                // Defensive wait: if IsMine but room state hasn't arrived yet, wait up to 10 frames
                if (isMine && !roomStateOK)
                {
                    Debug.LogWarning($"[ExecutingTurn] IsMine=true but RoomState={roomState}, waiting for GameIsPlaying...");
                    for (int wait = 0; wait < 10; wait++)
                    {
                        yield return null;
                        roomState = RoomStateManager.Instance.CurrentRoomState;
                        roomStateOK = roomState == RoomState.STATE.GameIsPlaying || roomState == RoomState.STATE.ABFirstTurn;
                        if (roomStateOK) break;
                    }
                    Debug.Log($"[ExecutingTurn] After RoomState wait: RoomState={RoomStateManager.Instance.CurrentRoomState}, roomStateOK={roomStateOK}");
                }

                if (isMine && roomStateOK)
                {

                    if (MatchHandler.IsPoker())
                    {

                        if (PokerActionPanel.Instance.checkPockeBetPlaced())
                        {

                            yield return WaitForBetsToSettle();
                        }

                        //yield return new WaitUntil(() => RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying);

                        bool panelActive = PokerActionPanel.Instance.ActionPanelPoker.activeSelf;
                        bool stateOK = currentState == PlayerState.STATE.ExecutingTurn;
                        int playCount = PlayerStateManager.Instance.PlayingList.Count;
                        Debug.Log($"[ExecutingTurn] BUTTON CHECK: panelActive={panelActive}, stateOK={stateOK}, playCount={playCount}");

                        if (!panelActive && stateOK && playCount > 1)
                        {

                            PokerActionPanel.Instance.callBetPockerBtn.SetActive(playerInfo.checkForAllPlayersbetAreEqual() ? false : true);
                            PokerActionPanel.Instance.checkPokerBtn.SetActive(!PokerActionPanel.Instance.callBetPockerBtn.activeSelf);

                            PokerActionPanel.Instance.SetMinBetAmount();
                            PokerActionPanel.Instance.ActionPanelPoker.SetActive(true);
                            Debug.Log($"[ExecutingTurn] ✓ ACTION PANEL SHOWN for {gameObject.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ExecutingTurn] ✗ ACTION PANEL NOT SHOWN: panelActive={panelActive}, stateOK={stateOK}, playCount={playCount}");
                            yield return null;
                        }
                    }
                }
                else
                {
                    Debug.Log($"[ExecutingTurn] Not my turn or wrong state: isMine={isMine}, roomState={RoomStateManager.Instance.CurrentRoomState}");
                    if (MatchHandler.IsPoker())
                    {

                        if (PokerActionPanel.Instance.ActionPanelPoker.activeSelf)
                        {
                            PokerActionPanel.Instance.ActionPanelPoker.SetActive(false);
                        }
                    }
                }
            }
            else
            {

                if (playerInfo.gameObject.name != LocalSettings.AI_Name && (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn))
                {

                    if (PokerActionPanel.Instance.checkPockeBetPlaced())
                    {
                        Debug.Log("CHeck Bool here " + playerInfo.isBetPlacedPocker);
                        yield return WaitForBetsToSettle();
                    }
                    playerInfo.FillerImage.fillAmount = 1;
                    if (!PokerActionPanel.Instance.ActionPanelPoker.activeSelf && currentState == PlayerState.STATE.ExecutingTurn && PlayerStateManager.Instance.PlayingList.Count > 1)
                    {

                        PokerActionPanel.Instance.callBetPockerBtn.SetActive(playerInfo.checkForAllPlayersbetAreEqual() ? false : true);
                        PokerActionPanel.Instance.checkPokerBtn.SetActive(!PokerActionPanel.Instance.callBetPockerBtn.activeSelf);

                        PokerActionPanel.Instance.SetMinBetAmount();
                        PokerActionPanel.Instance.ActionPanelPoker.SetActive(true);
                    }
                    else
                        yield return null;
                }
                else
                {
                    if (PokerActionPanel.Instance.ActionPanelPoker.activeSelf)
                    {
                        PokerActionPanel.Instance.ActionPanelPoker.SetActive(false);
                    }

                    if (PokerActionPanel.Instance.checkPockeBetPlaced())
                    {

                        yield return WaitForBetsToSettle();

                    }

                    playerInfo.FillerImage.fillAmount = 1;

                    StartCoroutine(waitforAIstate());

                }
            }

            if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
            {
                playerInfo.FillerImage.gameObject.SetActive(true);
            }

            PlayerTurnManager.Instance.alarmTime = LocalSettings.RemainingTikTimer;
            if (!MatchHandler.isOffline())
                NetworkGameManager.Instance.CmdBeginTurn();
            playerInfo.PlayerStateText.text = "Executing Turn";
        }

        IEnumerator waitforAIstate()
        {
            float waitTime = Random.value;
            float delay = waitTime < 0.05f ? Random.Range(0.5f, 18f) : Random.Range(0.5f, 8f);




            yield return new WaitForSeconds(delay);

            if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
            {
                if (currentState == PlayerState.STATE.ExecutingTurn)
                {
                    float outcome = Random.value;
                    //if(outcome < 0.8f)
                    //{
                    //    LocalSettings.AI_StandUP = true;
                    //    playerInfo.StandUp();
                    //    SitHere.Instance.SetThisForAIPositionByPlayer();
                    //}
                    //else


                    if (outcome < 0.06f)
                    {
                        UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                        // PokerActionPanel.Instance.CallAIButtonClick(false);
                    }
                    else
                    {
                        //Debug.LogError("value of OutCome " + outcome);
                        //float outcome = Random.value;
                        //if ()
                        //{

                        //}
                        PokerActionPanel.Instance.CallAIButtonClick(false);

                        //UpdateCurrentPlayerState(PlayerState.STATE.BetPlaced);
                    }

                }

            }
        }







        IEnumerator CheckForContinueTurn()
        {
            if (PokerActionPanel.Instance.checkPockeBetPlaced())
                yield return WaitForBetsToSettle();
            //if (!PokerActionPanel.Instance.ActionPanelPoker.activeSelf)
            //{
            //    PokerActionPanel.Instance.ActionPanelPoker.SetActive(true);
            //}
        }

        //public bool checkPockeBetPlaced()
        //{
        //    foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
        //    {
        //        if (item.isBetPlacedPocker == false)
        //            return false;
        //    }
        //    return true;
        //}
        IEnumerator waitSideShowoff()
        {
            while (playerInfo.IsSideShow)
            {
                playerInfo.FillerImage.gameObject.SetActive(false);

                yield return new WaitUntil(() => playerInfo.IsSideShow);
                StartCoroutine(MethodOfExecutingTurn());
            }
        }

        bool checkisSideShow()
        {
            return playerInfo.IsSideShow;
        }

        public void TriggerStateBetPlaced()
        {
            if (MatchHandler.IsPoker())
            {
                Debug.Log("-------- Bet Placed State Poker");
                playerInfo.PlayerStateText.text = "Bet Placed Poker";
                //                BigInteger chalAmount = Pot.instance.CurrentChalAmount;
                //                if (playerInfo.IsSeen)
                //                    chalAmount = Pot.instance.CurrentChalAmount * 2;



                if (LocalSettings.GetPokerBuyInChips() >= PokerActionPanel.Instance.MinBetAmount)
                {
                    if (playerInfo.IsMine())
                    {
                        if (PokerActionPanel.Instance.isSliderBtnClick)
                            PokerActionPanel.Instance.OnRaiseSliderBtnClick();
                        else
                            PokerActionPanel.Instance.OnCallCheckAllInBtnClick();


                    }
                    PlayerTurnManager.Instance.GoToNextTurn();
                    playerInfo.FillerImage.gameObject.SetActive(true);
                    
                    //Debug.LogError("Changing wait for turn ______________");
                    //if (playerInfo.IsSideShow)
                    //    UpdateCurrentPlayerState(PlayerState.STATE.SenderSideShow);
                    //else
                    if (!GameManager.Instance.isRunINBackGround)
                    {
                        UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);
                        SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
                    }
                    //Debug.LogError("Win sound is playing");
                }
                else
                {

                    if (playerInfo.IsMine())
                    {
                        //   UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                        uiManager.quickShop.SetActive(false);
                        Game_Play.Instance.StandUp();
                        ConstantsData_M.Log(PlayerPrefs.GetString("TotalChips") + "3");
                    }
                }
                if (playerInfo.IsMine())
                    SetNextPlayerToExecutingTurn();
            }

            else
            {
                Debug.Log("-------- Bet Placed State Poker");
                playerInfo.PlayerStateText.text = "Bet Placed Poker";
                //                BigInteger chalAmount = Pot.instance.CurrentChalAmount;
                //                if (playerInfo.IsSeen)
                //                    chalAmount = Pot.instance.CurrentChalAmount * 2;


                if (playerInfo.gameObject.name != LocalSettings.AI_Name)
                {
                    if (LocalSettings.GetPokerBuyInChips() >= PokerActionPanel.Instance.MinBetAmount)
                    {

                        if (PokerActionPanel.Instance.isSliderBtnClick)
                            PokerActionPanel.Instance.OnRaiseSliderBtnClick();
                        else
                            PokerActionPanel.Instance.OnCallCheckAllInBtnClick();



                        PlayerTurnManager.Instance.GoToNextTurn();
                        playerInfo.FillerImage.gameObject.SetActive(true);
                        //Debug.LogError("Her is you Filler Image 4");
                        //Debug.LogError("Changing wait for turn ______________");
                        //if (playerInfo.IsSideShow)
                        //    UpdateCurrentPlayerState(PlayerState.STATE.SenderSideShow);
                        //else
                        if (!GameManager.Instance.isRunINBackGround)
                        {
                            UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);
                            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
                        }
                        //Debug.LogError("Win sound is playing");
                    }
                    else
                    {
                        //   UpdateCurrentPlayerState(PlayerState.STATE.Packed);
                        uiManager.quickShop.SetActive(false);
                        Game_Play.Instance.StandUp();

                    }
                    if (!MatchHandler.isOffline())
                    {
                        if (playerInfo.IsMine())
                            SetNextPlayerToExecutingTurn();
                    }
                    else
                    {
                        SetNextPlayerToExecutingTurn();
                    }
                }
                else
                {
                    //Here is AI Funtionaliy
                    //if (PokerActionPanel.Instance.isAISliderBtnClick)
                    //    PokerActionPanel.Instance.OnRaiseSliderBtnClick();
                    //else
                    PokerActionPanel.Instance.OnCallAICheckAllInBtnClick();

                    SetNextPlayerToExecutingTurn();
                }




            }


        }



        //int nextPlayerInt;
        void SetNextPlayerToExecutingTurn()
        {

            int nextPlayerInt = PlayerStateManager.Instance.PlayingList.IndexOf(playerInfo);
            nextPlayerInt++;
            if (nextPlayerInt >= PlayerStateManager.Instance.PlayingList.Count)
                nextPlayerInt = 0;

            PlayerStateManager.Instance.PlayingList[nextPlayerInt].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
        }

        public void TriggerStatePacked()
        {
            Debug.Log("-------- Packed State");

            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                if (PokerActionPanel.Instance.ActionPanelPoker.activeSelf)
                    PokerActionPanel.Instance.ActionPanelPoker.SetActive(false);

                playerInfo.FillerImage.gameObject.SetActive(false);
                playerAnim.PackAnim();
                playerInfo.PackedText.gameObject.SetActive(true);

                //playerInfo.PlayerStateText.text = "FOLD";
                playerInfo.PackedText.text = "FOLD";
                if (PlayerStateManager.Instance.PlayingList.Count > 1)
                {
                    if (!MatchHandler.isOffline())
                    {
                        // BUG FIX: only pass the turn onward if 2+ players will still remain
                        // AFTER this pack is applied (PlayingList still contains the packing
                        // player at this point, so ">2" means ">1" survivors). With exactly
                        // 2 players, this fold ends the hand outright — RemainingPlayerWonGameAutomatic()
                        // below (once PlayingList.Count <= 1) handles it. Calling
                        // SetNextPlayerToExecutingTurn() here in that case gave the winner a
                        // stray ExecutingTurn/action-panel prompt right as the auto-win fired.
                        if (playerInfo.IsMine() && !playerInfo.IsSideShow && PlayerStateManager.Instance.PlayingList.Count > 2)
                        {
                            Debug.Log($"[Packed] Passing turn onward — PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count} before removing {gameObject.name}");
                            SetNextPlayerToExecutingTurn();
                        }
                        else if (playerInfo.IsMine() && !playerInfo.IsSideShow)
                        {
                            Debug.Log($"[Packed] Skipped extra turn — only {PlayerStateManager.Instance.PlayingList.Count} players before removing {gameObject.name}, hand ends via auto-win instead");
                        }
                    }

                    PlayerStateManager.Instance.UpdateListOnPlayerPack();
                }
                if (PokerActionPanel.Instance.checkPockeBetPlaced() && PlayerStateManager.Instance.PlayingList.Count > 1)//isCircleCheckFlag)
                {
                    Debug.LogError("packed State....1...");
                    if (playerInfo.checkForAllPlayersbetAreEqual() || PokerActionPanel.Instance.CheckBoolAllIN())
                    {
                        Debug.LogError("packed State....2...");
                        playerInfo.BetGoToFinalPointsForPacked(1.5f);
                    }
                }

                if (PlayerStateManager.Instance.PlayingList.Count <= 1)
                {
                    Debug.Log($"[Packed] Auto-win path — PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count} after removing {gameObject.name}");
                    PlayerStateManager.Instance.RemainingPlayerWonGameAutomatic();
                    // PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(PlayerStateManager.Instance.PlayingList[0].photonView.ViewID, PokerActionPanel.Instance.TotalPotAmount());
                    PokerManager.Instance.HistoryFinalCall();
                    //  PokerHistory.Instance.SortPokerRecord();
                }

                PokerManager.Instance.DestroyHoleCards(playerInfo);
                playerInfo.HandRankLabelTxt.transform.parent.gameObject.SetActive(false);
            }


        }
        public void TriggerStateSenderSideShow()
        {
            //if (photonView.IsMine)
            //{
            //    if (uiManager.ActionTable.activeSelf)
            //    {
            //        Debug.LogError("Action panel trouble  1 ----------------------------------------");
            //        uiManager.ActionTable.SetActive(false);
            //    }
            //}

            playerInfo.SideShowIndicatorAnim.SetActive(true);
            playerInfo.FillerImage.gameObject.SetActive(false);
            Debug.Log("-------- Waiting For Turn State");
            playerInfo.PlayerStateText.text = "Waiting For Turn";

        }
        public void TriggerStateRecieverSideShow()
        {
            Debug.Log("-------- Side Show State");

            if (playerInfo.IsMine())
            {
                int requestFromSideShowInt = PlayerStateManager.Instance.SideShowNext();
                uiManager.playerName.text = PlayerStateManager.Instance.PlayingList[requestFromSideShowInt].name.ToString();
                GameStartManager.Instance.sideShowCount = LocalSettings.SideShowCountDownTime;
                LocalSettings.Vibrate();
                uiManager.sideShowPanel.SetActive(true);

            }
            playerInfo.SideShowIndicatorAnim.SetActive(true);
            playerInfo.PlayerStateText.text = "Side Show";

        }

        public void TriggerStateOutOfTable()
        {
            if (!MatchHandler.isOffline())
            {
                if (!MatchHandler.IsPoker())
                    gameObject.SetActive(false);
                else
                {
                    if (playerInfo.IsMine())
                    {
                        GameManager.Instance.position_availability[0].is_reserved = null;
                        if (playerInfo.IsMine())
                            PositionsManager.Instance.ReleasePosition(GameManager.Instance.myLocalSeat);
                        GameManager.Instance.SitHereBtnStatus(true);
                    }
                }
            }



            OutOfTableThings();

            playerInfo.PlayerStateText.text = "";
            if (playerInfo.IsMine())
            {

                PositionsManager.Instance.ReArrangePlayerSeatsAccordingToNetworkPositions();
            }
            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                //if (!playerInfo.gamePokerisStandUp)
                //{
                //  playerInfo.gamePokerisStandUp = true;
                //Invoke(nameof(GameObjecSetActiveFalse), 0f);
                //}
                //else
                {
                    Invoke(nameof(GameObjecSetActiveFalse), 1f);

                }
            }


        }
        void GameObjecSetActiveFalse()
        {
            gameObject.SetActive(false);
        }

        void OutOfTableThings()
        {
            playerInfo.FillerImage.gameObject.SetActive(false);

            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {


                if (CheckOfflineOrOnlineLocalPlayerBool(playerInfo))
                {

                    if (PokerActionPanel.Instance.ActionPanelPoker.activeSelf)
                        PokerActionPanel.Instance.ActionPanelPoker.SetActive(false);
                    if (playerInfo.gameObject.name != LocalSettings.AI_Name)
                    {
                        LocalSettings.SetTotalChips(LocalSettings.GetPokerBuyInChips());

                      //  LocalSettings.SetPokerBuyInChips(-LocalSettings.GetPokerBuyInChips());
                        playerInfo.PokerTotalCash = LocalSettings.GetPokerBuyInChips();
                    }
                    if (!MatchHandler.isOffline())
                    {
                        playerInfo.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, playerInfo.PokerTotalCash);
                    }
                    if (playerInfo.Hole_Card1)
                    {
                        Destroy(playerInfo.Hole_Card1.gameObject);
                        Destroy(playerInfo.Hole_Card2.gameObject);
                        playerInfo.DummyCardsParent.transform.GetChild(0).gameObject.SetActive(false);
                        playerInfo.DummyCardsParent.transform.GetChild(1).gameObject.SetActive(false);
                    }
                }



                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
                {
                    if (PlayerStateManager.Instance.PlayingList.Count > 1)
                    {
                        // BUG FIX: same ordering issue as TriggerStatePacked() — PlayingList
                        // still contains the leaving player here, so ">2" is required for 2+
                        // players to actually remain once they're removed below. With exactly
                        // 2 players, standing up ends the hand — the Count<=1 auto-win check
                        // further down handles it, and giving an extra ExecutingTurn here just
                        // produced a stray action-panel prompt for the remaining player.
                        if (!MatchHandler.isOffline())
                        {
                            if (playerInfo.IsMine() && checkPlayerBool() && !GameManager.Instance.isRunINBackGround && PlayerStateManager.Instance.PlayingList.Count > 2)
                            {
                                Debug.Log($"[OutOfTable] Passing turn onward — PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count} before removing {gameObject.name}");
                                SetNextPlayerToExecutingTurn();
                            }
                        }
                        else
                        {
                            if (playerInfo.gameObject.name != LocalSettings.AI_Name && checkPlayerBool() && !GameManager.Instance.isRunINBackGround && PlayerStateManager.Instance.PlayingList.Count > 2)
                            {
                                Debug.Log($"[OutOfTable] Passing turn onward — PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count} before removing {gameObject.name}");
                                SetNextPlayerToExecutingTurn();
                            }
                        }

                        if (checkPlayerIsRunInBackGround() && checkPlayerBool() && !GameManager.Instance.isRunINBackGround && PlayerStateManager.Instance.PlayingList.Count > 2)
                            SetNextPlayerToExecutingTurn();



                    }
                }

                PlayerStateManager.Instance.UpdateListOnPlayerPack();
                giveTurnToNext = true;

                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
                {

                    if (PokerActionPanel.Instance.checkPockeBetPlaced() && PlayerStateManager.Instance.PlayingList.Count > 1)//isCircleCheckFlag)
                    {
                        // Debug.LogError("packed State....1...");
                        if (playerInfo.checkForAllPlayersbetAreEqual() || PokerActionPanel.Instance.CheckBoolAllIN())
                        {

                            playerInfo.BetGoToFinalPointsForPacked(0f);
                        }
                    }

                }
                updaePlayerProperties(playerInfo);


                if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying || RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.ABFirstTurn)
                    if (PlayerStateManager.Instance.PlayingList.Count <= 1)
                    {
                        Debug.Log($"[OutOfTable] Auto-win path — PlayingList.Count={PlayerStateManager.Instance.PlayingList.Count} after removing {gameObject.name}, isRunINBackGround={GameManager.Instance.isRunINBackGround}");
                        if (!GameManager.Instance.isRunINBackGround)
                            PlayerStateManager.Instance.RemainingPlayerWonGameAutomatic();
                        //  PokerHistory.Instance.SetHistoryBetAmountForEachPlayer(PlayerStateManager.Instance.PlayingList[0].photonView.ViewID, PokerActionPanel.Instance.TotalPotAmount());
                        PokerManager.Instance.HistoryFinalCall();
                        // PokerHistory.Instance.SortPokerRecord();
                    }
                PokerManager.Instance.DestroyHoleCards(playerInfo);
                playerInfo.SideShowIndicatorAnim.SetActive(false);
                playerInfo.HandRankLabelTxt.transform.parent.gameObject.SetActive(false);
                // GameManager.Instance.isRunINBackGround = false;
            }






        }

        bool CheckOfflineOrOnlineLocalPlayerBool(PlayerInfo info)
        {
            if (MatchHandler.IsPoker())
                return info.IsMine();
            return true;
        }


        bool checkPlayerIsRunInBackGround()
        {
            for (int i = 0; i < PlayerStateManager.Instance.PlayingList.Count; i++)
            {
                if (PlayerStateManager.Instance.PlayingList[i].checkApplicationBackground)
                    return true;
            }
            return false;
        }

        bool checkPlayerBool()
        {
            foreach (var item in PlayerStateManager.Instance.PlayingList)
            {
                if (playerInfo.IsMine())
                    if (item.View_ID_Offline == playerInfo.View_ID_Offline)
                        return true;
            }
            return false;
        }

        void updaePlayerProperties(PlayerInfo playerInfo)
        {
            if (playerInfo.IsMine() && RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
            {

                UIManager.Instance.UpdateTheWinAmount(LocalSettings.totalcashWinLossKey, LocalSettings.TotalHandsKey, LocalSettings.WinHandsKey);
            }
        }
    }
}