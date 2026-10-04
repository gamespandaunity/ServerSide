using UnityEngine;
namespace BallPool.AI
{
    public class AightBallPoolAIManager : BallPoolAIManager
    {
        public override bool FindException(int ballId)
        {
            if (AightBallPoolGameLogic.isCueBall(ballId))
            {
                return true;
            }
            bool isBlackBall = AightBallPoolGameLogic.isBlackBall(ballId);
            if (!AightBallPoolGameLogic.gameState.playersHasBallType && isBlackBall)
            {
                return true;
            }
            if (!AightBallPoolGameLogic.gameState.playersHasBallType && !isBlackBall)
            {
                return false;
            }

            bool mainPlayerIsBlack = AightBallPoolPlayer.mainPlayer.isBlack;
            bool otherPlayerIsBlack = AightBallPoolPlayer.otherPlayer.isBlack;
            bool ballIsStripes = AightBallPoolGameLogic.isStripesBall(ballId);
            bool ballIsSolids = AightBallPoolGameLogic.isSolidsBall(ballId);

            if (AightBallPoolPlayer.mainPlayer.myTurn)
            {
                if (mainPlayerIsBlack)
                {
                    return !isBlackBall;
                }
                else if(isBlackBall)
                {
                    return true;
                }

                bool mainPlayerIsStripes = AightBallPoolPlayer.mainPlayer.isStripes;
                bool mainPlayerIsSolids = AightBallPoolPlayer.mainPlayer.isSolids;
                if (ballIsStripes)
                {
                    return !mainPlayerIsStripes;
                }
                else if (ballIsSolids)
                {
                    return !mainPlayerIsSolids;
                }
            }
            else if (AightBallPoolPlayer.otherPlayer.myTurn)
            {
                if (otherPlayerIsBlack)
                {
                    return !isBlackBall;
                }
                else if(isBlackBall)
                {
                    return true;
                }

                bool otherPlayerIsStripes = AightBallPoolPlayer.otherPlayer.isStripes;
                bool otherPlayerIsSolids = AightBallPoolPlayer.otherPlayer.isSolids;

                if (ballIsStripes)
                {
                    return !otherPlayerIsStripes;
                }
                else if (ballIsSolids)
                {
                    return !otherPlayerIsSolids;
                }
            }
            return false;
        }
    }
}
