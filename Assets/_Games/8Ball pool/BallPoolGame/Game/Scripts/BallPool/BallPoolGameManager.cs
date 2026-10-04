using BallPool.AI;
using BallPool.Mechanics;
using UnityEngine;

namespace BallPool
{
    /// <summary>
    /// The game manager.
    /// </summary>
    public class BallPoolGameManager
    {
        public PlayAgainMenu _playAgainMenu;
        public PlayAgainMenu playAgainMenu
        {
            get
            {
                if (!_playAgainMenu)
                {
                    _playAgainMenu = PlayAgainMenu.FindObjectOfType<PlayAgainMenu>();
                }
                return _playAgainMenu;
            }
        }
        /// <summary>
        /// Occurs when enable control for current player.
        /// </summary>
        public event System.Action<bool> OnEnableControl;
        /// <summary>
        /// Occurs when game is complite.
        /// </summary>
        public event System.Action OnGameComplite;
        public event System.Action OnShotEnded;
        public event System.Action OnCalculateAI;
        /// <summary>
        /// Occurs when AI player shot.
        /// </summary>
        public event System.Action OnShotAI;
        /// <summary>
        /// Occurs when set game prize.
        /// </summary>
        public event System.Action<int> OnSetPrize;
        /// <summary>
        /// Occurs when set player.
        /// </summary>
        public event System.Action<BallPoolPlayer> OnSetPlayer;
        /// <summary>
        /// Occurs when set active player who will start playing.
        /// </summary>
        public event System.Action<BallPoolPlayer, bool> OnSetActivePlayer;
        /// <summary>
        /// Occurs when set player avatar.
        /// </summary>
        public event System.Action<BallPoolPlayer> OnSetAvatar;
        /// <summary>
        /// Occurs when set active balls identifiers, Which are not in the kinematic space.
        /// </summary>
        public event System.Action<BallPoolPlayer> OnSetActiveBallsIds;
        /// <summary>
        /// Occurs when update game time.
        /// </summary>
        public event System.Action<float> OnUpdateTime;
        /// <summary>
        /// Occurs when start time before the turn.
        /// </summary>
        public event System.Action OnStartTime;
        /// <summary>
        /// Occurs when start time during the turn.
        /// </summary>
        public event System.Action OnStopTime;
        /// <summary>
        /// Occurs when end time for turn.
        /// </summary>
        public event System.Action OnEndTime;
        /// <summary>
        /// Occurs when on set game info, (game result).
        /// </summary>
        public event System.Action<string> OnSetGameInfo;


        public Ball[] balls{ get; private set;}

        public static BallPoolGameManager instance
        {
            get;
            private set;
        }

        public void Initialize(PhysicsHandeler physicsManager, BallPoolAIManager aiManager, Ball[] balls)
        {
            instance = this;
            this.balls = balls;
            this.physicsManager = physicsManager;
            this.aiManager = aiManager;

            physicsManager.OnCueStrike += (string data) => 
                {
                    OnStartShot(data);
                };
            physicsManager.OnShotComplete += (string data) =>
                {
                    OnEndShot(data);
                };
            physicsManager.OnBallMotion += (int ballId, Vector3 position, Vector3 velocity, Vector3 angularVelocity) => 
                {
                    if(balls[ballId].inPocket)
                    {
                        balls[ballId].OnState(BallState.MoveInPocket);
                    }
                    else
                    {
                        balls[ballId].OnState(BallState.Move);
                    }
                };
            physicsManager.OnBallStopped += (int ballId, Vector3 position) => 
                {
                    balls[ballId].OnState(BallState.EndMove);
                };
        }

        public virtual void OnStartShot(string data)
        {
            for (int i = 0; i < balls.Length; i++) 
            {
                balls[i].OnState(BallState.SetState);
            }
            balls[0].OnState(BallState.StartMove);
            calculateTime = false;
            if(OnStopTime != null)
            {
                OnStopTime();
                CircularTimeController.instance.audioSource.Stop();
            }
        }
        public virtual void OnEndShot(string data)
        {
            // Only the controlling player owns the local turn countdown. Watchers and the
            // dedicated server receive timer UI/state over the network and must not tick it.
            calculateTime = !BallPoolGameLogic.controlFromNetwork;
            playTime = 0.0f;
            if(OnStartTime != null)
            {
                OnStartTime();
            }
            CircularTimeController.instance.audioSource.Stop();
        }
        private string GetBallsMechanicalsState()
        {
            return "";
        }
        public PhysicsHandeler physicsManager
        {
            get;
            private set;
        }

        public BallPoolAIManager aiManager
        {
            get;
            private set;
        }

        public float maxPlayTime
        {
            get;
            set;
        }
        public float playTime
        {
            get;
            set;
        }
        public bool calculateTime
        {
            get;
            private set;
        }
        public bool gameIsComplite
        {
            get;
            private set;
        }
        public string gameInfoText
        {
            get;
            private set;
        }


        public void SetGameInfo(string info)
        {
            //Debug.Log("ss:"+info);
            gameInfoText = info.Trim(); //RAR
            if (OnSetGameInfo != null)
            {
                OnSetGameInfo(info.Trim()); //RAR
            }
        }


        /// <summary>
        /// Timer-only reset for a new turn — exactly the three writes CallOnEnableControl starts with, WITHOUT
        /// firing OnStartTime/OnEnableControl. Needed because Player_OnTurnChanged SUPPRESSES
        /// CallOnEnableControl while a shot is still settling (that suppression exists for the cue VISUALS),
        /// and the timer reset was skipped along with it: the new turn holder inherited the previous turn's
        /// playTime (often 1.0 straight after a timeout) and the OLD holder's calculateTime stayed true, so it
        /// kept ticking as a watcher — tester logs show OnEndPlayTime firing on the client whose turn it was
        /// NOT, and back-to-back timeouts ping-ponging the turn (report a8de6335 "turn got shift").
        /// endTimeFired is private, hence this lives here in the base class.
        /// </summary>
        protected void ResetTurnTimerForNewTurn()
        {
            calculateTime = !BallPoolGameLogic.controlFromNetwork;
            playTime = 0.0f;
            endTimeFired = false;
        }

        protected void CallOnEnableControl(bool value)
        {
            calculateTime = !BallPoolGameLogic.controlFromNetwork;
            playTime = 0.0f;
            endTimeFired = false; // new turn -> re-arm the one-shot timeout latch
            if (OnStartTime != null)
            {
                OnStartTime();
            }
            if (OnEnableControl != null)
            {
                OnEnableControl(value && calculateTime);
            }
        }
        protected void CallOnShotAI()
        {
            if (OnShotAI != null)
            {
                OnShotAI();
            }
        }
        protected void CallOnCalculateAI()
        {
            if (aiManager.calculateAI)
            {
                return;
            }
            if (OnCalculateAI != null)
            {
                OnCalculateAI();
            }
        }
        protected void CallOnEndShot()
        {
            if (OnShotEnded != null)
            {
                OnShotEnded();
            }
        }
        protected void CallOnGameComplite()
        {
            gameIsComplite = true;
            if (OnGameComplite != null)
            {
                OnGameComplite();
            }
        }
        protected void CallOnSetPrize(int prize)
        {
            if (OnSetPrize != null)
            {
                OnSetPrize(prize);
            }
        }
        protected void CallOnSetActivePlayer(BallPoolPlayer player, bool value)
        {
            if (OnSetActivePlayer != null)
            {
                OnSetActivePlayer(player, value);
            }
        }
        protected void CallOnSetPlayer(BallPoolPlayer player)
        {
            if (OnSetPlayer != null)
            {
                OnSetPlayer(player);
            }
        }
        protected void CallOnSetAvatar(BallPoolPlayer player)
        {
            if (OnSetAvatar != null)
            {
                OnSetAvatar(player);
            }
        }
        protected void CallOnSetActiveBallsIds(BallPoolPlayer player)
        {
            if (OnSetActiveBallsIds != null)
            {
                OnSetActiveBallsIds(player);
            }
        }
        protected void CallOnUpdateTime(float deltaTime)
        {
            if (!gameIsComplite && calculateTime)
            {
                if (playTime < 1.0f)
                {
                    playTime += deltaTime / maxPlayTime;
                    if (OnUpdateTime != null)
                    {
                        OnUpdateTime(playTime);
                    }
                }
                else
                {
                    EndTime();
                }
            }
        }
        public void SetPlayTime(float time01)
        {
            if (time01 < 1.0f)
            {
                endTimeFired = false; // fresh (next-turn) time from the network -> re-arm the latch
                playTime = time01;
                if (OnUpdateTime != null)
                {
                    OnUpdateTime(playTime);
                }
            }
            else
            {
                EndTime();
            }
        }

        // A shot is committed when the player releases the cue, before the cue animation and
        // network StartSimulate command finish. Freeze the local turn timer at that boundary so
        // the final animation frames cannot also raise a timeout for an already-committed shot.
        public bool TryCommitTurnTimerForShot()
        {
            if (gameIsComplite || endTimeFired || playTime >= 1.0f)
                return false;

            calculateTime = false;
            return true;
        }

        public void ResumeTurnTimerAfterRejectedShot()
        {
            if (!gameIsComplite && !endTimeFired && playTime < 1.0f)
                calculateTime = !BallPoolGameLogic.controlFromNetwork;
        }

        // Latch so the turn-timeout fires exactly ONCE per turn. Without it, once playTime hit 1.0
        // CallOnUpdateTime re-entered EndTime EVERY FRAME (nothing reset playTime until the next
        // turn activated), so ChangeTurn -> CmdChangeTurn spammed the server dozens of times per
        // round-trip — console flooded with "Turn changed", turns flipped on their own, and the
        // resulting churn opened the turn-desync windows ("double turn" / stuck game).
        private bool endTimeFired = false;

        private void EndTime()
        {
            if (endTimeFired) return;
            if (ShouldDeferOnlineTimeoutForShot()) return;
            endTimeFired = true;
            Physics.Simulate(Time.fixedDeltaTime); //RAR
            playTime = 1.0f;
            BallPoolGameLogic.instance.OnEndTime();
            if (OnEndTime != null)
            {
                Physics.Simulate(Time.fixedDeltaTime); //RAR
                OnEndTime();
            }
            BallPoolPlayer.ChangeTurn(BallPoolPlayer.TurnChangeReason.Timeout);
            Physics.Simulate(Time.fixedDeltaTime); //RAR
        }

        private bool ShouldDeferOnlineTimeoutForShot()
        {
            if (!BallPoolGameLogic.isOnLine)
                return false;

            if (MyEightBallNetwork.Instance != null
                && MyEightBallNetwork.Instance.ShotPhase == EightBallShotPhase.Simulating)
                return true;

            ShotController shot = ShotController.shotControllerInstance;
            return shot != null
                && (shot.isInShot
                    || (shot.cuePhysicsManager != null && shot.cuePhysicsManager.IsInMove));
        }

        public virtual void OnForceGoHome(int winnerId)
        {
            BallPoolPlayer.SetWinner(winnerId);
        }
        public virtual void OnDisable()
        {
            OnGameComplite = null;
            OnShotEnded = null;
            OnCalculateAI = null;
            OnShotAI = null;
            OnSetPrize = null;
            OnSetPlayer = null;
            OnSetAvatar = null;
            OnSetActiveBallsIds = null;
            OnUpdateTime = null;
            OnStartTime = null;
            OnStopTime = null;
            OnEndTime = null;
            OnSetGameInfo = null;
            physicsManager = null;
            aiManager = null;
            instance = null;
        }
    }
}
