using BallPool.Mechanics;
using Mirror;
using Mirror.Examples.CharacterSelection;
using NetworkManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallPool
{
    public class AightBallPoolGameManager : BallPoolGameManager
    {
        private bool onBallHitPocket = false;
        private bool playAgainMenuIsActive;


        public AightBallPoolGameLogic gameLogic { get; set; }

        public void Start()
        {
            PlayerPrefs.SetInt("Multiplayer", 1);
            playAgainMenuIsActive = false;
            if (NetworkClient.active || NetworkServer.active)
                EightBallPingDisplay.Spawn(); // client-side bottom-left ping readout — online only, never spawned in AI/offline games
            physicsManager.OnBallCollision += PhysicsManager_OnBallHitBall;
            physicsManager.OnBallWallCollision += PhysicsManager_OnBallHitBoard;
            physicsManager.OnBallPocketed += PhysicsManager_OnBallHitPocket;
            bool isNetworkSession = NetworkClient.active || NetworkServer.active;
            PlayerPrefs.SetInt("EightballMultiplayer", isNetworkSession ? 1 : 0);
            BallPoolPlayer.OnTurnChanged += Player_OnTurnChanged;
            if (!isNetworkSession)
            {
                BallPoolGameLogic.isOnLine = false;
                BallPoolPlayer.turnId = (new System.Random()).Next(0, 2);

                if (BallPoolPlayer.turnId == 0)
                    BallPoolPlayer.SetTurn(staticVariables.UserProfiledata.user._id);
                else
                {
                    BallPoolPlayer.SetTurn(GameModeManager.isAI ? 1 : int.Parse(EightBallPoolNetworkManager.opponentPlayer.userId));
                }
                UpdateActiveBalls();

            }
            else
            {
                BallPoolGameLogic.playMode = BallPool.PlayMode.OnLine;
                BallPoolGameLogic.isOnLine = true;
                GameModeManager.isAI = false;
                if (NetworkServer.active)
                {
                    NetworkPlayerData mainplayer = NetworkGameManager.Instance.creatorData;
                    NetworkPlayerData otherplayer = NetworkGameManager.Instance.joinerData;
                    BallPoolPlayer.players[0] = new AightBallPoolPlayer(int.Parse(mainplayer.playerId),mainplayer.playerName,mainplayer.silverCoins, null, null);
                    BallPoolPlayer.players[1] = new AightBallPoolPlayer(int.Parse(otherplayer.playerId), otherplayer.playerName, otherplayer.silverCoins, null, null);
                }
                else {
                    BallPoolPlayer.players[1] = new AightBallPoolPlayer(
                        int.Parse(staticVariables.OpponetProfile.userId),
                        staticVariables.OpponetProfile.userName,
                        staticVariables.OpponetProfile.coins,
                        staticVariables.OpponetProfile.image,
                        staticVariables.OpponetProfile.imageURL); }
                    GameManager.instance.StartCoroutine(WaitForMultiplayerPlayersReady());
                    UpdateActiveBalls();


                Debug.Log("Waiting for server to set initial turn...");
            }
            CallOnSetPlayer(AightBallPoolPlayer.mainPlayer);
            CallOnSetPlayer(AightBallPoolPlayer.otherPlayer);

            CallOnSetAvatar(AightBallPoolPlayer.mainPlayer);
            CallOnSetAvatar(AightBallPoolPlayer.otherPlayer);



        }
        private System.Collections.IEnumerator WaitForMultiplayerPlayersReady()
        {
            while (NetworkGameManager.Instance == null)
            {
                yield return new WaitForSeconds(0.1f);
            }

            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (NetworkGameManager.Instance.creatorData != null &&
                    !string.IsNullOrEmpty(NetworkGameManager.Instance.creatorData.playerId) &&
                    NetworkGameManager.Instance.joinerData != null &&
                    !string.IsNullOrEmpty(NetworkGameManager.Instance.joinerData.playerId))
                {
                    Debug.Log("[Multiplayer] Both players ready"+ MyEightBallNetwork.IsTossDone);

                    if (Mirror.NetworkServer.active && MyEightBallNetwork.Instance != null)
                    {

                        MyEightBallNetwork.Instance.StartCoroutine(MyEightBallNetwork.Instance.InitializeOnlineGameTurn());
                    }

                    yield return WaitForPlayersInitialized();
                    // R3: the static players[] have just been REPLACED, which silently drops myTurn (the
                    // currentTurnId SyncVar hook may have already written it to the stale objects). Re-apply
                    // the authoritative turn to the live ones — see ReapplyTurnAfterPlayersRebuilt.
                    if (MyEightBallNetwork.Instance != null)
                        MyEightBallNetwork.Instance.ReapplyTurnAfterPlayersRebuilt();
                    // The scene-authored BLUR (not the old black loading overlay) now covers the stale
                    // board on the client: it is already up from scene load, and MyEightBallNetwork drops
                    // it a few frames after this snapshot's balls are applied
                    // (TargetRpcReconnectStateSyncComplete -> HideReconnectStateOverlayWhenReady).
                    // Nothing to hide or show here — just request the sync.
                    MyEightBallNetwork.Instance.GetBallsmechanicalStateData(MyEightBallNetwork.Instance.BreakShotDone);

                    UpdateActiveBalls();

                    yield break;
                }

                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }

            Debug.LogWarning("[Multiplayer] Timeout waiting for both players!");
        }

        private System.Collections.IEnumerator WaitForPlayersInitialized()
        {
            float timeout = 5f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                Debug.Log("Init : " + AightBallPoolPlayer.mainPlayer.name + AightBallPoolPlayer.otherPlayer.name);
                if (AightBallPoolPlayer.mainPlayer != null && AightBallPoolPlayer.otherPlayer != null)
                {
                    Debug.Log("[Multiplayer] Players initialized successfully");
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            Debug.LogWarning("[Multiplayer] Timeout waiting for players initialization!");
        }

        public void Update(float deltaTime)
        {
            if (onBallHitPocket)
            {
                onBallHitPocket = false;
                UpdateActiveBalls();
            }
            CallOnUpdateTime(deltaTime);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            BallPoolPlayer.Deactivate();
            BallPoolGameLogic.instance.Deactivate();
        }

        private void OpenPlayAgainMenu()
        {
            AightBallPoolGameLogic.gameState.gameIsComplete = true;
            playAgainMenuIsActive = true;
            CallOnGameComplite();
            {
                playAgainMenu.HidePlayAgainButton();
            }
        }

        public void UpdateActiveBalls()
        {
            if (AightBallPoolPlayer.mainPlayer == null || AightBallPoolPlayer.otherPlayer == null)
            {
                Debug.LogWarning("[UpdateActiveBalls] Players not ready yet");
                return;
            }
            AightBallPoolPlayer.mainPlayer.SetActiveBalls(balls);
            AightBallPoolPlayer.otherPlayer.SetActiveBalls(balls);

            CallOnSetActiveBallsIds(AightBallPoolPlayer.mainPlayer);
            CallOnSetActiveBallsIds(AightBallPoolPlayer.otherPlayer);
        }

        public override void OnStartShot(string data)
        {
            base.OnStartShot(data);
            gameLogic.RessetState();
            CircularTimeController.isSoundPlaying = false;

        }
        public override void OnEndShot(string data)
        {
            base.OnEndShot(data);

            if (BallPoolGameLogic.isOnLine && !NetworkServer.active)
            {



                if (shotController != null)
                    shotController.ClearLocalShotSettledFromServer();
                Debug.Log("[8Ball][Phase3] Online client skipped local rule eval — awaiting server RpcApplyShotOutcome.");
                return;
            }



            int shooterId = (MyEightBallNetwork.Instance != null) ? MyEightBallNetwork.Instance.ActiveShotPlayerId : -1;
            bool wasBreakOpen = AightBallPoolGameLogic.gameState != null && AightBallPoolGameLogic.gameState.tableIsOpened;
            int boardHitsThisShot = AightBallPoolGameLogic.gameState != null ? AightBallPoolGameLogic.gameState.ballsHitBoardCount : 0;
            bool serverOnline = NetworkServer.active && BallPoolGameLogic.isOnLine && MyEightBallNetwork.Instance != null;

            if (serverOnline)
            {
                MyEightBallNetwork.Instance.ReconcileAuthoritativePocketResult(shooterId);
                // FIX (opponent gets the black after the shooter cleared their group): on the
                // dedicated server checkIsBlackInEnd/isBlack are ONLY recomputed by
                // UpdateActiveBalls(), which is gated behind onBallHitPocket — a flag set exclusively
                // inside PhysicsManager_OnBallHitPocket. The server runs no physics (playback mode),
                // so if that pocket EVENT is lost the board diff above still credits the pot and the
                // turn is correctly held, but the shooter is never marked on-black server-side. Their
                // next shot at the black then hits OnCueBallHitBall's `currentPlayer.isBlack == false`
                // branch and is scored WrongBallHit — turn + ball-in-hand handed to the opponent.
                // Recompute from the authoritative board instead of trusting the event.
                UpdateActiveBalls();
            }

            bool gameIsEnd;
            gameLogic.OnEndShot(AightBallPoolGameLogic.GetBlackBall(balls).inPocket, out gameIsEnd, shooterId);
            bool cueBallWasPocketedAtShotEnd = AightBallPoolGameLogic.gameState != null
                                               && AightBallPoolGameLogic.gameState.cueBallInPocket;

            if (gameIsEnd)
            {
                BroadcastServerBallPositions();
                if (serverOnline)
                {
                    int winnerId = ResolveWinnerId();
                    EightBallOutcomeCode endCode = (winnerId == shooterId)
                        ? EightBallOutcomeCode.LegalBlackWin : EightBallOutcomeCode.IllegalBlackLoss;
                    MyEightBallNetwork.Instance.ServerPublishShotOutcome(shooterId, (int)endCode, false, true, winnerId);
                    MyEightBallNetwork.Instance.ServerDeclareGameWin(winnerId);
                }
                OpenPlayAgainMenu();
            }
            else if (!playAgainMenuIsActive)
            {
                CallOnEndShot();
                BroadcastServerBallPositions();
                if (serverOnline)
                {

                    bool needChange = AightBallPoolGameLogic.gameState.needToChangeTurn;
                    int outcome = cueBallWasPocketedAtShotEnd
                        ? (int)EightBallOutcomeCode.Scratch
                        : (int)DeriveOutcomeCode(wasBreakOpen, boardHitsThisShot);
                    // Use the same immutable per-shot scratch result for both movement and outcome.
                    // Do not let later/transient cue component state make those decisions disagree.
                    MyEightBallNetwork.Instance.OnAuthoritativeShotEnded(needChange, cueBallWasPocketedAtShotEnd);
                    MyEightBallNetwork.Instance.ServerPublishShotOutcome(shooterId, outcome, needChange, false, -1);
                }
                if (AightBallPoolGameLogic.gameState.needToChangeTurn)
                {
                    if (!NetworkServer.active)
                        BallPoolPlayer.ChangeTurn();
                }

                if (BallPoolGameLogic.playMode == PlayMode.PlayerAI && AightBallPoolPlayer.otherPlayer.myTurn)
                {
                    CallOnCalculateAI();
                }
            }
        }

        int ResolveWinnerId()
        {
            if (BallPoolPlayer.players != null)
                foreach (var p in BallPoolPlayer.players)
                    if (p != null && p.isWinner) return p.playerId;
            return -1;
        }

        EightBallOutcomeCode DeriveOutcomeCode(bool wasBreakOpen, int boardHitsThisShot)
        {
            var gs = AightBallPoolGameLogic.gameState;
            if (gs == null) return EightBallOutcomeCode.None;
            if (gs.cueBallInPocket) return EightBallOutcomeCode.Scratch;
            if (wasBreakOpen && gs.needToChangeTurn && boardHitsThisShot < 4) return EightBallOutcomeCode.WeakBreak;
            if (!gs.cueBallHasHitRightBall) return EightBallOutcomeCode.WrongBallHit;
            if (gs.needToChangeTurn) return EightBallOutcomeCode.NoRightBallPotted;
            return EightBallOutcomeCode.LegalPotContinue;
        }

        void BroadcastServerBallPositions()
        {
            if (NetworkServer.active && BallPoolGameLogic.isOnLine && MyEightBallNetwork.Instance != null)
            {
                Vector3[] serverBallPositions = new Vector3[balls.Length];
                for (int i = 0; i < balls.Length; i++)
                {
                    serverBallPositions[i] = balls[i].position;





                    if (i != 0 && balls[i].inPocket)
                        serverBallPositions[i].y = -1f;
                }
                MyEightBallNetwork.Instance.RpcSyncBallsAfterShot(serverBallPositions, MyEightBallNetwork.Instance.ActiveShotSequence);
            }
        }

        private ShotController _shotController;
        public ShotController shotController
        {
            get
            {
                if (_shotController == null)
                {
                    _shotController = ShotController.FindObjectOfType<ShotController>();
                }
                return _shotController;
            }
        }
        void PhysicsManager_OnBallHitBall(BallDetector ball, BallDetector hitBall, bool inMove)
        {
            if (!inMove)
            {
                return;
            }
            if (shotController.targetBallDetector == hitBall)
            {
                hitBall.body.linearVelocity = hitBall.body.linearVelocity.magnitude * shotController.savedTargetBallDirection;
                shotController.targetBallDetector = null;
            }
            balls[ball.id].OnState(BallState.HitBall);
            bool isCueBall = AightBallPoolGameLogic.isCueBall(ball.id);

            if (isCueBall)
            {
                gameLogic.OnCueBallHitBall(ball.id, hitBall.id);
            }
        }

        void PhysicsManager_OnBallHitBoard(BallDetector ball, bool inMove)
        {
            if (!inMove)
            {
                return;
            }
            balls[ball.id].OnState(BallState.HitBoard);
            gameLogic.OnBallHitBoard(ball.id);
        }

        void PhysicsManager_OnBallHitPocket(BallDetector ball, PocketDetector pocket, bool inMove)
        {
            if (!inMove)
            {
                return;
            }
            balls[ball.id].OnState(BallState.EnterInPocket);

            // Online clients only render the pocket event. Scratch detection, cue-ball respot,
            // and the resulting turn decision belong exclusively to the authoritative server.
            if (BallPoolGameLogic.isOnLine && !NetworkServer.active && AightBallPoolGameLogic.isCueBall(ball.id))
            {
                Debug.Log("[8Ball][Scratch] Client observed cue-ball pocket; awaiting authoritative server respot/outcome.");
                GameManager.instance.ShadowToggle.SetBallShadows();
                return;
            }

            bool cueBallInPocket = false;
            gameLogic.OnBallInPocket(ball.id, ref cueBallInPocket);
            if (!cueBallInPocket)
            {
                onBallHitPocket = true;
            }

            GameManager.instance.ShadowToggle.SetBallShadows();
        }

        public override void OnForceGoHome(int winnerId)
        {
            DebugManager.DebugLog("leavebtn  " + winnerId);
            BallPoolPlayer.SetWinner(winnerId);
            GameManager.instance.WinDueToUserDisconnect();

        }

        private void Player_OnTurnChanged()
        {
            if (!playAgainMenuIsActive)
            {
                bool onlineShotStillSettling = BallPoolGameLogic.isOnLine
                    && shotController != null
                    && (shotController.isMoving
                        || (shotController.cuePhysicsManager != null && shotController.cuePhysicsManager.IsInMove));

                if (!onlineShotStillSettling)
                {
                    CallOnEnableControl(BallPoolPlayer.mainPlayer.myTurn || BallPoolGameLogic.playMode == PlayMode.HotSeat);
                }
                else
                {
                    shotController.SuppressCueVisualsWhileShotSettling();
                    // The suppression above is for the cue VISUALS/control only — the turn TIMER must still
                    // reset for the new turn, or the new holder inherits the previous turn's playTime (1.0
                    // right after a timeout => instant re-timeout => turn ping-pong) and the old holder keeps
                    // ticking as a watcher. See ResetTurnTimerForNewTurn.
                    ResetTurnTimerForNewTurn();
                    Debug.Log("[8Ball][TurnControl] Suppressed immediate OnEnableControl while shot is still settling; delayed network control refresh will apply after physics stops (turn timer reset anyway).");
                }

                CallOnSetActivePlayer(AightBallPoolPlayer.mainPlayer, BallPoolPlayer.turnId == AightBallPoolPlayer.mainPlayer.playerId);
                CallOnSetActivePlayer(AightBallPoolPlayer.otherPlayer, BallPoolPlayer.turnId == AightBallPoolPlayer.otherPlayer.playerId);

                if (BallPoolGameLogic.playMode == PlayMode.PlayerAI && AightBallPoolPlayer.otherPlayer.myTurn)
                {
                    CallOnCalculateAI();
                }
            }
        }
    }
}
