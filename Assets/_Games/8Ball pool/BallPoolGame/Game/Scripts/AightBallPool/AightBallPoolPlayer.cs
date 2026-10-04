using System.Collections.Generic;
using BallPool.Mechanics;

namespace BallPool
{
    public class AightBallPoolPlayer : BallPoolPlayer
    {
        public new static AightBallPoolPlayer mainPlayer
        {
            get{ return (players == null || players.Length < 1) ? null : (AightBallPoolPlayer)players[0]; }
        }

        public static AightBallPoolPlayer otherPlayer
        {
            get{ return (players == null || players.Length < 2) ? null : (AightBallPoolPlayer)players[1]; }
        }

        public bool checkIsBlackInEnd
        {
            get;
            private set;
        }

        /// <summary>
        /// The Player need to put the black ball
        /// </summary>
        public bool isBlack
        {
            get;
            set;
        }

        public bool isCueBall
        {
            get;
            set;
        }

        private bool _isStripes;
        private bool _isSolids;

        public bool isStripes
        {
            get{ return _isStripes; }
            set
            {
                _isStripes = value;
                _isSolids = !_isStripes;
            }
        }

        public bool isSolids
        {
            get{ return _isSolids; }
            set
            {
                _isSolids = value;
                _isStripes = !_isSolids;
            }
        }

        public override void OnDeactivate()
        {
            checkIsBlackInEnd = false;
            isBlack = false;
            isCueBall = false;
            _isSolids = false;
            _isStripes = false;
        }

        public override void SetActiveBalls(Ball[] balls)
        {
            if (balls == null || balls.Length == 0)
            {
                return;
            }
            this.balls = new List<Ball>(0);
            checkIsBlackInEnd = false;
            isBlack = false;
            foreach (Ball ball in balls)
            {
                if (!ball.inPocket)
                {
                    if (isStripes && AightBallPoolGameLogic.isStripesBall(ball.id))
                    {
                        this.balls.Add(ball);
                    }
                    else if (isSolids && AightBallPoolGameLogic.isSolidsBall(ball.id))
                    {
                        this.balls.Add(ball);
                    }
                }
            }

            if (AightBallPoolGameLogic.gameState.playersHasBallType)
            {
                if (this.balls.Count == 0)
                {
                    Ball blackBall = AightBallPoolGameLogic.GetBlackBall(BallPoolGameManager.instance.balls);
                    if (!blackBall.inPocket)
                    {
                        checkIsBlackInEnd = true;
                        isBlack = true;
                        this.balls.Add(blackBall);
                    }
                }
            }
        }

        public static bool PlayerHasSomeBallType(AightBallPoolPlayer player, int ballId)
        {
            return (player.isSolids && AightBallPoolGameLogic.isSolidsBall( ballId)) || (player.isStripes && AightBallPoolGameLogic.isStripesBall( ballId));
        }

        public AightBallPoolPlayer(int playerId, string name, int coins, object avatar, string avatarURL) : base(playerId, name, coins, avatar, avatarURL)
        {
            _isStripes = false;
            _isSolids = false;
            isBlack = false;
            isCueBall = false;
            checkIsBlackInEnd = false;
        }

     
    }
}
