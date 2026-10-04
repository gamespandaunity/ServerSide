using BallPool.Mechanics;

using UnityEngine;
using DG.Tweening;

namespace BallPool
{
    public class AightBallPoolGameState : GameState
    {
        /// <summary>
        /// The table will close after first shot.
        /// </summary>
        public bool tableIsOpened = true;
        /// <summary>
        /// The type of the players is e stripes or solids.
        /// </summary>
        /// 
        public bool playersHasBallType = false;
        /// <summary>
        /// Balls hit board count in first shot.
        /// </summary>
        public int ballsHitBoardCount = 0;
        /// <summary>
        /// The cue ball has hit right ball, stripes, solids or black.
        /// </summary>
        public bool cueBallHasHitRightBall = false;
        /// <summary>
        /// The cue ball has hit some ball at first hit.
        /// </summary>
        public bool cueBallHasHitSomeBall = false;
        /// <summary>
        /// The cue ball has right ball in pocket, stripes, solids or black.
        /// </summary>
        public bool hasRightBallInPocket = false;
        /// <summary>
        /// The current player has cue ball in his hand
        /// </summary>
        public bool cueBallInHand = true;
        /// <summary>
        /// The cue ball in pocket.
        /// </summary>
        public bool cueBallInPocket = false;

        public AightBallPoolGameState()
            : base()
        {
            tableIsOpened = true;
            playersHasBallType = false;
            ballsHitBoardCount = 0;
            cueBallHasHitRightBall = false;
            cueBallHasHitSomeBall = false;
            hasRightBallInPocket = false;
            cueBallInHand = true;
            cueBallInPocket = false;
        }
    }

    public class AightBallPoolGameLogic : BallPoolGameLogic
    {
        private static AightBallPoolGameState _gameState;

        public static AightBallPoolGameState gameState
        {
            get
            {
                if (_gameState == null)
                {
                    _gameState = new AightBallPoolGameState();
                }
                return _gameState;
            }
        }
        public void RessetState()
        {
            // Guard against a reconnect/background-resume race: this runs from OnStartShot inside the
            // DelayAndStartShot coroutine. After a pause -> resume -> reconnect, the scene reloads and
            // BallPoolPlayer.players / AightBallPoolGameLogic.gameState can still be null/uninitialised
            // ("Timeout waiting for BallPoolPlayer initialization!"). Unguarded, mainPlayer/otherPlayer
            // (= players[0]/[1]) were null and `.isCueBall = false` threw a NullReferenceException —
            // which aborted the shot coroutine and cascaded into the stuck / cue / shadow / turn bugs.
            if (gameState == null) return;

            gameState.gameIsComplete = false;
            gameState.needToChangeTurn = false;
            gameState.cueBallHasHitRightBall = false;
            gameState.cueBallHasHitSomeBall = false;
            gameState.hasRightBallInPocket = false;
            gameState.ballsHitBoardCount = 0;
            gameState.cueBallInHand = false;
            gameState.cueBallInPocket = false;

            if (AightBallPoolPlayer.mainPlayer != null)
                AightBallPoolPlayer.mainPlayer.isCueBall = false;
            if (AightBallPoolPlayer.otherPlayer != null)
                AightBallPoolPlayer.otherPlayer.isCueBall = false;
        }

        public void OnBallHitBoard(int ballId)
        {
            if (gameState.tableIsOpened)
            {
                gameState.ballsHitBoardCount++;
            }
        }

        public void OnCueBallHitBall(int cueBallId, int ballId)
        {
            if (gameState.cueBallHasHitSomeBall)
            {
                return;
            }
            gameState.cueBallHasHitSomeBall = true;
            CircularTimeController.instance.audioSource.Stop();
            CircularTimeController.isSoundPlaying = false;

            CircularTimeController.instance.ToggleBallGlowEffects(false);


            if (isBlackBall(ballId))
            {
                if (((AightBallPoolPlayer)BallPoolPlayer.currentPlayer).isBlack)
                {
                    gameState.cueBallHasHitRightBall = true;
                }
            }
            else if (gameState.tableIsOpened)
            {
                gameState.cueBallHasHitRightBall = true;
            }
            else if (!gameState.playersHasBallType)
            {
                gameState.cueBallHasHitRightBall = true;
            }
            else if (AightBallPoolPlayer.PlayerHasSomeBallType((AightBallPoolPlayer)BallPoolPlayer.currentPlayer, ballId))
            {
                gameState.cueBallHasHitRightBall = true;
            }
            if (!gameState.cueBallHasHitRightBall)
            {
                gameState.cueBallInHand = true;
                gameState.needToChangeTurn = true;
            }
        }
        bool isHighlighterShowed = false;

        public void OnBallInPocket(int ballId, ref bool cueBallInPocket)
        {
            float textFadeDuration = 1f;
            float textFadeDelay = 0.05f;

            if (isCueBall(ballId))
            {
                if (AightBallPoolPlayer.mainPlayer.myTurn)
                {
                    AightBallPoolPlayer.mainPlayer.isCueBall = true;
                }
                else if (AightBallPoolPlayer.otherPlayer.myTurn)
                {
                    AightBallPoolPlayer.otherPlayer.isCueBall = true;
                }
                gameState.needToChangeTurn = true;
                gameState.cueBallInHand = true;
                cueBallInPocket = true;
                gameState.cueBallInPocket = true;

                // Sync cue ball in hand online
                if (BallPoolGameLogic.isOnLine && Mirror.NetworkServer.active && MyEightBallNetwork.Instance != null)
                {
                    MyEightBallNetwork.Instance.ServerRegisterCueBallPocketed();
                }
                return;
            }

            if (gameState.tableIsOpened)
            {
                if (!isBlackBall(ballId))
                {
                    gameState.hasRightBallInPocket = true;
                }
            }
            else
            {
                Debug.Log(BallPoolPlayer.turnId);
                Debug.Log(BallPoolPlayer.currentPlayer);
                if (!gameState.playersHasBallType)
                {
                    // (removed) `var loopEnabler = CircularTimeController...` — it was assigned and never
                    // used, and it dereferenced CircularTimeController.instance unguarded, which NREs on the
                    // headless dedicated server right at the START of group assignment (this same callback
                    // runs there via ApplyShooterPocketToServerBoard's synthesized pocket events), aborting
                    // this block before playersHasBallType was even set.
                    //
                    // ONLINE the local first-pot assignment below is DISABLED. It was one of THREE competing
                    // writers (this first-examined-ball rule, the server's per-shot reconcile rule, and the
                    // sender-perspective CmdSetPlayerBallTypes relay), which produced both observed failures:
                    // groups flipping mid-match (types (1,2)<->(2,1)) and groups stuck at (0,0) so every
                    // legal pot read as NoRightBallPotted and the turn kept shifting (reports 8c4afc28,
                    // b15f6d42, a8de6335). The SERVER now assigns alone and broadcasts
                    // OnMainPlayerBallTypeChanged, which sets isSolids/isStripes + playersHasBallType +
                    // tableIsOpened + the UI text on every client. Offline (AI) keeps the original local rule.
                    bool assignTypesLocally = !BallPoolGameLogic.isOnLine;
                    if (assignTypesLocally)
                        gameState.playersHasBallType = true;
                    if (!isBlackBall(ballId))
                    {
                        gameState.hasRightBallInPocket = true;
                    }

                    if (assignTypesLocally && AightBallPoolPlayer.mainPlayer.myTurn)
                    {
                        if (AightBallPoolGameLogic.isStripesBall(ballId))
                        {
                            AightBallPoolPlayer.mainPlayer.isStripes = true;
                            AightBallPoolPlayer.otherPlayer.isSolids = true;
                            string info = "You Are Stripes";
                            GameManager.instance.selectedBallText.text = info;
                            SelectionBallFade_Animate(textFadeDuration, textFadeDelay);

                            // Sync ball types online
                            if (PlayerPrefs.GetInt("EightballMultiplayer") == 1 && MyEightBallNetwork.Instance != null)
                            {
                                Debug.Log("Sync stripes solids types online");
                                MyEightBallNetwork.Instance.CmdSetPlayerBallTypes(false, true, true, false);
                            }
                        }
                        else if (AightBallPoolGameLogic.isSolidsBall(ballId))
                        {
                            AightBallPoolPlayer.mainPlayer.isSolids = true;
                            AightBallPoolPlayer.otherPlayer.isStripes = true;
                            GameManager.instance.selectedBallText.text = "You Are solids";
                            SelectionBallFade_Animate(textFadeDuration, textFadeDelay);

                            // Sync ball types online
                            if (PlayerPrefs.GetInt("EightballMultiplayer") == 1 && MyEightBallNetwork.Instance != null)
                            {
                                Debug.Log("Sync stripes solids types online");

                                MyEightBallNetwork.Instance.CmdSetPlayerBallTypes(true, false, false, true);
                            }
                        }
                    }
                    else if (assignTypesLocally && AightBallPoolPlayer.otherPlayer.myTurn)
                    {
                        if (AightBallPoolGameLogic.isStripesBall(ballId))
                        {
                            AightBallPoolPlayer.otherPlayer.isStripes = true;
                            AightBallPoolPlayer.mainPlayer.isSolids = true;
                            GameManager.instance.selectedBallText.text = "You Are solids";
                            SelectionBallFade_Animate(textFadeDuration, textFadeDelay);

                            // Sync ball types online
                            if (PlayerPrefs.GetInt("EightballMultiplayer") == 1 && MyEightBallNetwork.Instance != null)
                            {
                                Debug.Log("Sync stripes solids types online");

                                MyEightBallNetwork.Instance.CmdSetPlayerBallTypes(true, false, false, true);
                            }
                        }
                        else if (AightBallPoolGameLogic.isSolidsBall(ballId))
                        {
                            AightBallPoolPlayer.otherPlayer.isSolids = true;
                            AightBallPoolPlayer.mainPlayer.isStripes = true;
                            GameManager.instance.selectedBallText.text = "You Are Stripes";
                            SelectionBallFade_Animate(textFadeDuration, textFadeDelay);

                            // Sync ball types online
                            if (PlayerPrefs.GetInt("EightballMultiplayer") == 1 && MyEightBallNetwork.Instance != null)
                            {
                                Debug.Log("Sync stripes solids types online");
                                MyEightBallNetwork.Instance.CmdSetPlayerBallTypes(false, true, true, false);
                            }
                        }
                    }
                }           
                else if (AightBallPoolPlayer.PlayerHasSomeBallType((AightBallPoolPlayer)BallPoolPlayer.currentPlayer, ballId))
                {
                    gameState.hasRightBallInPocket = true;
                }
            }
        }
        private void SelectionBallFade_Animate(float textFadeDuration, float textFadeDelay)
        {
            GameManager.instance.selectedBallText.rectTransform.DOAnchorPos(new Vector3(0, 100f, 0), 2.5f, true).SetEase(Ease.OutQuad).OnComplete(() => GameManager.instance.selectedBallText.DOFade(0f, textFadeDuration).SetDelay(textFadeDelay).OnComplete(() =>
            {
                GameManager.instance.selectedBallText.text = " ";
            }));
        }
        private bool CanLegallyPotBlack(AightBallPoolPlayer player)
        {
            if (player == null || player.isCueBall || !gameState.playersHasBallType)
            {
                return false;
            }

            BallPoolGameManager manager = BallPoolGameManager.instance;
            if (manager == null || manager.balls == null)
            {
                return player.isBlack;
            }

            bool hasAssignedType = player.isSolids || player.isStripes;
            if (!hasAssignedType)
            {
                return false;
            }

            foreach (Ball ball in manager.balls)
            {
                if (ball == null || ball.inPocket)
                {
                    continue;
                }

                if (player.isSolids && isSolidsBall(ball.id))
                {
                    return false;
                }

                if (player.isStripes && isStripesBall(ball.id))
                {
                    return false;
                }
            }

            return true;
        }

        private AightBallPoolPlayer ResolveShotPlayer(int shooterPlayerId)
        {
            if (shooterPlayerId > 0 && BallPoolPlayer.players != null)
            {
                foreach (BallPoolPlayer player in BallPoolPlayer.players)
                {
                    if (player != null && player.playerId == shooterPlayerId)
                    {
                        return player as AightBallPoolPlayer;
                    }
                }
            }

            // Offline games do not have an authoritative network shot id. Keep their
            // existing turn-based behaviour as a fallback only.
            AightBallPoolPlayer current = BallPoolPlayer.currentPlayer as AightBallPoolPlayer;
            if (current != null)
            {
                return current;
            }

            if (AightBallPoolPlayer.mainPlayer != null && AightBallPoolPlayer.mainPlayer.myTurn)
            {
                return AightBallPoolPlayer.mainPlayer;
            }

            if (AightBallPoolPlayer.otherPlayer != null && AightBallPoolPlayer.otherPlayer.myTurn)
            {
                return AightBallPoolPlayer.otherPlayer;
            }

            return null;
        }

        public void OnEndShot(bool blackBallInPocket, out bool gameIsEnd, int shooterPlayerId = -1)
        {
            gameIsEnd = false;
            CircularTimeController.isSoundPlaying = false;
            // Phase 3 (codex): null-safe for a headless/dedicated server that has no audio source.
            if (CircularTimeController.instance != null && CircularTimeController.instance.audioSource != null)
            {
                CircularTimeController.instance.audioSource.loop = false;
                CircularTimeController.instance.audioSource.Stop();
                CircularTimeController.instance.audioSource.clip = null;
            }
            if (BallPoolGameManager.instance == null)
            {
                return;
            }
            string info = "".Trim(); //RAR

            bool canSetInfo = true;

            if (gameState.cueBallInPocket && !blackBallInPocket)
            {
                if (BallPoolPlayer.mainPlayer.myTurn)
                {
                    if (GameModeManager.isAI)
                    {
                        info = "You pocket the cue ball, \n AI  has cue ball in hand";
                    }
                    else
                    {
                        info = "You pocket the cue ball\n" + GameManager.newOpponentName + " has cue ball in hand";
                    }

                    //  info = "You pocket the cue ball, \n" + /*AightBallPoolPlayer.otherPlayer.name*/GameManager.newOpponentName + " has cue ball in hand";
                }
                else
                {
                    if (GameModeManager.isAI)
                    {
                        info = "AI pocket the cue ball, \n You  have cue ball in hand";
                    }
                    else
                    {
                        info = GameManager.newOpponentName + " pocket the cue ball You  have cue ball in hand";
                    }
                    //   info = /*AightBallPoolPlayer.otherPlayer.name*/GameManager.newOpponentName + " pocket the cue ball, \n" + " You have cue ball in hand";
                }
                BallPoolGameManager.instance.SetGameInfo(info);
                canSetInfo = false;
            }


            if (gameState.tableIsOpened)
            {
                info = ""; //RAR
                if (!(gameState.cueBallHasHitRightBall && gameState.hasRightBallInPocket))
                {
                    gameState.needToChangeTurn = true;
                    if (gameState.ballsHitBoardCount < 4)
                    {
                        gameState.cueBallInHand = true;
                        if (BallPoolPlayer.mainPlayer.myTurn)
                        {
                            if (GameModeManager.isAI)
                            {
                                info = "Break up of balls was weak,  \n AI has cue ball in hand";
                            }
                            else
                            {
                                info = "Break up of balls was weak,  \n " + GameManager.newOpponentName + "has cue ball in hand";
                            }
                        }
                        else
                        {
                            info = "Break up of balls was weak,  \n You have cue ball in hand";
                        }
                        // info = "Break up of balls was weak, \n" + (BallPoolPlayer.mainPlayer.myTurn ? /*GameManager.instance.otherPlayerName*/GameManager.newOpponentName + " has cue ball in hand" : "You have cue ball in hand");
                    }
                }
            }
            gameState.tableIsOpened = false;
            // Notify server that the break shot has been completed so reconnecting clients
            // can have tableIsOpened correctly restored to false (cue ball free to move anywhere).
            if (MyEightBallNetwork.Instance != null)
            {
                if (Mirror.NetworkServer.active)
                    MyEightBallNetwork.Instance.ServerSetBreakShotDone(true);
                else
                    MyEightBallNetwork.Instance.CmdSetBreakShotDone(true);
                Debug.Log("[8Ball][AightBallPoolGameLogic] Break shot done marked.");
            }
            if (blackBallInPocket)
            {
                // Black-ball adjudication uses CanLegallyPotBlack in BOTH modes (user decision
                // 2026-07-22): the old offline isBlack-latch rule wrongly awarded the win to the
                // opponent when the AI potted the black on a separate shot after clearing its group
                // (the latch had not been set), and it also counted a same-shot last-ball+black as
                // a foul. The live board check has neither problem.
                //
                // In multiplayer, myTurn can lag behind the server shot state. The player who
                // actually started the authoritative shot must be used for black-ball adjudication;
                // otherwise the other player's remaining balls can be checked and the shooter is
                // incorrectly marked as the loser after a legal black pot.
                AightBallPoolPlayer shooter = ResolveShotPlayer(shooterPlayerId);
                AightBallPoolPlayer opponent = shooter == AightBallPoolPlayer.mainPlayer
                    ? AightBallPoolPlayer.otherPlayer
                    : AightBallPoolPlayer.mainPlayer;

                if (shooter != null && opponent != null)
                {
                    bool shooterWins = CanLegallyPotBlack(shooter);
                    shooter.isWinner = shooterWins;
                    opponent.isWinner = !shooterWins;

                    if (!shooterWins)
                    {
                        bool localPlayerShot = shooter == AightBallPoolPlayer.mainPlayer;
                        string shooterName = localPlayerShot
                            ? "You"
                            : (GameModeManager.isAI ? "AI" : GameManager.newOpponentName);
                        info = shooterName + " pocketed the black ball"
                            + (shooter.isCueBall ? " with the cue ball" : " illegally");
                    }

                    Debug.Log($"[8Ball][BlackResult] shooter={shooter.playerId}, legal={shooterWins}, winner={(shooterWins ? shooter.playerId : opponent.playerId)}.");
                }
                else
                {
                    Debug.LogError($"[8Ball][BlackResult] Could not resolve shooter {shooterPlayerId}; winner was not declared.");
                }
                gameState.needToChangeTurn = false;
                gameIsEnd = true;
                BallPoolGameManager.instance.SetGameInfo(info);
                return;
            }

            if (AightBallPoolPlayer.mainPlayer.checkIsBlackInEnd)
            {
                AightBallPoolPlayer.mainPlayer.isBlack = true;
            }
            if (AightBallPoolPlayer.otherPlayer.checkIsBlackInEnd)
            {
                AightBallPoolPlayer.otherPlayer.isBlack = true;
            }

            if (!gameState.cueBallHasHitRightBall)
            {
                gameState.cueBallInHand = true;
                gameState.needToChangeTurn = true;

                if (AightBallPoolPlayer.mainPlayer.myTurn)
                {
                    if (info == "")
                    {


                        if (AightBallPoolPlayer.mainPlayer.isBlack)
                        {

                            info = "You need to hit black ball" + (GameModeManager.isAI ? "Ai has ball in hand" : GameManager.newOpponentName + " has cue ball in hand");


                        }
                        else if (AightBallPoolPlayer.mainPlayer.isSolids)
                        {
                            info = "You need to hit Solid ball" + (GameModeManager.isAI ? "Ai has ball in hand" : GameManager.newOpponentName + " has cue ball in hand");
                        }
                        else if (AightBallPoolPlayer.mainPlayer.isStripes)
                        {
                            info = "You need to hit Stripes ball" + (GameModeManager.isAI ? "Ai has ball in hand" : GameManager.newOpponentName + " has cue ball in hand");
                        }
                        else
                        {
                            info = "You need to hit solids or stripes ball";
                        }

                        //    info = AightBallPoolPlayer.mainPlayer.isBlack ? "You need to hit black ball" :
                        //    (AightBallPoolPlayer.mainPlayer.isSolids ? "You need to hit solids ball" :
                        //    (AightBallPoolPlayer.mainPlayer.isStripes ? "You need to hit stripes ball" : "You need to hit solids or stripes ball")) +
                        //"\n" +/* AightBallPoolPlayer.otherPlayer.name*/GameManager.newOpponentName + " has cue ball in hand";
                    }
                }
                else
                {
                    if (info == "")
                    {
                        if (AightBallPoolPlayer.otherPlayer.isBlack)
                        {

                            info = (GameModeManager.isAI ? "Ai need to hit black ball\nYou have cue ball in hand" : GameManager.newOpponentName + "need to hit black ball\nYou have cue ball in hand");


                        }
                        else if (AightBallPoolPlayer.otherPlayer.isSolids)
                        {
                            info = (GameModeManager.isAI ? "Ai need to hit Solid ball\nYou has ball in hand" : GameManager.newOpponentName + " need to hit Solid ball\nYou has ball in hand");
                        }
                        else if (AightBallPoolPlayer.otherPlayer.isStripes)
                        {
                            info = (GameModeManager.isAI ? "Ai need to hit Stripes ball\nYou has ball in hand" : GameManager.newOpponentName + " need to hit Solid ball\nYou has ball in hand");
                        }
                        else
                        {
                            info = (GameModeManager.isAI ? "AI need to hit solids or stripes ball" : GameManager.newOpponentName + "  need to hit solids or stripes ball");
                        }
                    }
                    //  info =/* AightBallPoolPlayer.otherPlayer.name */GameManager.newOpponentName + ((AightBallPoolPlayer.otherPlayer.isBlack ? " need to hit black ball" : (AightBallPoolPlayer.otherPlayer.isSolids ? " need to hit solids ball" : (AightBallPoolPlayer.otherPlayer.isStripes ? " need to hit stripes ball" : " need to hit solids or stripes ball")))) +
                    //  ", \nYou have cue ball in hand";
                }
            }
            else if (!gameState.hasRightBallInPocket)
            {
                gameState.needToChangeTurn = true;
                //gameState.cueBallInHand = true;
                if (AightBallPoolPlayer.mainPlayer.myTurn)
                {
                    if (info == "")
                    {
                        if (AightBallPoolPlayer.mainPlayer.isBlack)
                        {

                            info = "You need to pocket solids ball";


                        }
                        else if (AightBallPoolPlayer.mainPlayer.isSolids)
                        {
                            info = "You need to pocket Solid ball";
                        }
                        else if (AightBallPoolPlayer.mainPlayer.isStripes)
                        {
                            info = "You need to pocket Stripes ball";
                        }
                        else
                        {
                            info = "You need to pocket solids or stripes ball";
                        }
                        //info = AightBallPoolPlayer.mainPlayer.isBlack ? "You need to pocket solids ball" : 
                        //    (AightBallPoolPlayer.mainPlayer.isSolids ? "You need to pocket solids ball" : (AightBallPoolPlayer.mainPlayer.isStripes ? "You need to pocket stripes ball" :
                        //    "You need to pocket solids or stripes ball"));
                    }
                }
                else
                {

                    if (info == "")
                    {
                        if (AightBallPoolPlayer.otherPlayer.isBlack)
                        {

                            info = (GameModeManager.isAI ? "Ai need to pocket black ball\nYou have cue ball in hand" : GameManager.newOpponentName + "need to pocket black ball");


                        }
                        else if (AightBallPoolPlayer.otherPlayer.isSolids)
                        {
                            info = (GameModeManager.isAI ? "Ai need to pocket Solid ball" : GameManager.newOpponentName + " need to pocket Solid ball");
                        }
                        else if (AightBallPoolPlayer.otherPlayer.isStripes)
                        {
                            info = (GameModeManager.isAI ? "Ai need to pocket Stripes ball" : GameManager.newOpponentName + " need to pocket Solid ball");
                        }
                        else
                        {
                            info = (GameModeManager.isAI ? "AI need to pocket solids or stripes ball" : GameManager.newOpponentName + "  need to pocket solids or stripes ball");
                        }

                        //info = /*AightBallPoolPlayer.otherPlayer.name*/GameManager.newOpponentName+ (AightBallPoolPlayer.otherPlayer.isBlack ? " need to pocket black ball" :
                        //    ((AightBallPoolPlayer.otherPlayer.isSolids ? " need to pocket solids ball" :
                        //    (AightBallPoolPlayer.otherPlayer.isStripes ? " need to pocket stripes ball" :
                        //    " need to pocket solids or stripes ball"))));
                    }
                }
            }
            if (!isHighlighterShowed && BallPoolPlayer.mainPlayer != null && BallPoolPlayer.mainPlayer.myTurn)
            {
                if (AightBallPoolPlayer.mainPlayer.isSolids || AightBallPoolPlayer.mainPlayer.isStripes)
                {
                    // Phase 3 (codex): null-safe for a headless/dedicated server (no particle system).
                    if (CircularTimeController.instance != null)
                        CircularTimeController.instance.PlayGenericBallParticle();
                    isHighlighterShowed = true;
                }
            }
            if (canSetInfo)
            {
                BallPoolGameManager.instance.SetGameInfo(info);
            }

        }

        public override void OnEndTime()
        {
            string info = "";
            ConstantsData_M.Log("OnEndPlayTime");
            gameState.cueBallInHand = true;
            if (AightBallPoolPlayer.mainPlayer.myTurn)
            {
                if (GameModeManager.isAI)
                {
                    info = "You run out of time\n AI has cue ball in hand";
                }
                else
                {
                    info = "You run out of time\n" + GameManager.newOpponentName + " has cue ball in hand";
                }
                CircularTimeController.instance.ToggleBallGlowEffects(false);
            }
            else
            {
                if (GameModeManager.isAI)
                {
                    info = "AI run out of time\n YOU have cue ball in hand";
                }
                else
                {
                    info = GameManager.newOpponentName + " ran out of time\n  You have cue ball in hand";
                }
                CircularTimeController.instance.ToggleBallGlowEffects(false);
            }
            //string info = AightBallPoolPlayer.mainPlayer.myTurn ? "You run out of time\n" + /*AightBallPoolPlayer.otherPlayer.name */GameManager.newOpponentName + " has cue ball in hand" : 
            ///*AightBallPoolPlayer.otherPlayer.name*/GameManager.instance.otherPlayerName + " run out of time, \nYou have cue ball in hand ";
            BallPoolGameManager.instance.SetGameInfo(info);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            _gameState = null;
        }

        public static Ball GetCueBall(Ball[] balls)
        {
            foreach (Ball ball in balls)
            {
                if (isCueBall(ball.id))
                {
                    return ball;
                }
            }
            return null;
        }

        /// <summary>
        /// Is the ball in pocket?, not for the cue ball, the cue ball will be resetted
        /// </summary>
        public static bool ballInPocket(Ball ball)
        {
            return ball.inPocket;
        }

        public static Ball GetBlackBall(Ball[] balls)
        {
            foreach (Ball ball in balls)
            {
                if (isBlackBall(ball.id))
                {
                    return ball;
                }
            }
            return null;
        }

        public static bool isCueBall(int id)
        {
            return id == 0;
        }

        public static bool isBlackBall(int id)
        {
            return id == 8;
        }

        public static bool isStripesBall(int id)
        {
            return id > 8 && id < 16;
        }

        public static bool isSolidsBall(int id)
        {
            return id > 0 && id < 8;
        }
    }
}
