using UnityEngine;
using BallPool.Mechanics;
using UnityEngine.Serialization;

namespace BallPool
{
    public class AightBallPoolBallsSortingManager : MonoBehaviour, BallPoolBallsSortingManager
    {
        [FormerlySerializedAs("balls")] public Transform allBallsRoot;
        [FormerlySerializedAs("BallsListener")] public Transform ballEventsListener;
        [FormerlySerializedAs("physicsManager")] public PhysicsHandeler physicsHandler;
        [FormerlySerializedAs("ballsDistance")] public float ballSpacingDistance;
        [FormerlySerializedAs("cueBallPosition")] public Transform cueBallSpawnPoint;
        [FormerlySerializedAs("pyramidFirstBallPosition")] public Transform rackFirstBallPosition;
        [FormerlySerializedAs("gameManager")] public GameManager gameManager;

        public void SortBallsByType()
        {
            //Debug.Log("Balls sorted by AightBallPoolBallsSortingManager");
            Vector2[] delta = { 
                new Vector2(0.0f, 0.0f),//0
                new Vector2(4.0f, 4.0f),//15
                new Vector2(1.0f, -1.0f),//2
                new Vector2(2.0f, 2.0f),//9
                new Vector2(3.0f, -3.0f),//10
                new Vector2(3.0f, 1.0f),//8
                new Vector2(4.0f, -4.0f),//3
                new Vector2(4.0f, 0.0f),//4
                new Vector2(2.0f, 0.0f),//11
                new Vector2(1.0f, 1.0f),//5
                new Vector2(2.0f, -2.0f),//12
                new Vector2(3.0f, -1.0f),//6
                new Vector2(3.0f, 3.0f),//13
                new Vector2(4.0f, -2.0f),//7
                new Vector2(4.0f, 2.0f),//14
                new Vector2(0.0f, 0.0f)};//1

            gameManager.allBalls = new Ball[allBallsRoot.childCount];
            physicsHandler.BallListeners = new BallDetector[allBallsRoot.childCount];

            for (int i = 0; i < allBallsRoot.childCount; i++)
            {
                Ball ball = allBallsRoot.GetChild(i).GetComponent<Ball>();
                BallDetector listener = ballEventsListener.GetChild(i).GetComponent<BallDetector>();
                listener.body = listener.GetComponent<Rigidbody>();
                ball.listener = listener;
                float distance = listener.GetComponent<SphereCollider>().radius + ballSpacingDistance;
                Vector3 position = cueBallSpawnPoint.position;
                if (i != 0)
                {
                    position = rackFirstBallPosition.position + new Vector3(delta[i].x * Mathf.Sqrt(Mathf.Pow(2.0f * distance, 2.0f) - Mathf.Pow(distance, 2.0f)), 0.0f, delta[i].y * distance);
                }
                ball.id = i;
                ball.transform.position = position;
                listener.transform.position = position;
                listener.id = ball.id;
                listener.physicsManager = physicsHandler;
                ball.name = listener.name = "Ball_" + i;

                gameManager.allBalls[i] = ball;
                physicsHandler.BallListeners[i] = listener;
            }
        }
    }
}
