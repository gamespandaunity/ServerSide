using System.Collections;
using UnityEngine;
using BallPool.Mechanics;
using Mechanics;

namespace BallPool
{
    public class Pocket : MonoBehaviour
    {
        public class BallRoll
        {
            public BallDetector ball{ get; private set; }
            public float maxLength{ get; private set; }

            public BallRoll(BallDetector ball, float maxLength)
            {
                this.ball = ball;
                this.maxLength = maxLength;
            }
        }
        private PhysicsHandeler physicsManager;
        public int id;
        private Vector3[] nodes;
        private float length;
        private float currentLength;

        void Start()
        {
            physicsManager = PhysicsHandeler.FindObjectOfType<PhysicsHandeler>();
            if (!physicsManager)
            {
                Destroy(gameObject);
                return;
            }
            physicsManager.OnBallPocketed += PhysicsManager_OnBallHitPocket;
            physicsManager.OnBallEjectedFromPocket += PhysicsManager_OnBallExitFromPocket;
            nodes = new Vector3[transform.childCount];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = transform.GetChild(i).position;
            }
            length = QuadraticCurve.CalculateLength(nodes); 
            currentLength = length;
        }

        void OnDisable()
        {
            if (physicsManager)
            {
                physicsManager.OnBallPocketed -= PhysicsManager_OnBallHitPocket;
                physicsManager.OnBallEjectedFromPocket -= PhysicsManager_OnBallExitFromPocket;
            }
        }
        void OnDestroy()
        {
            if (physicsManager)
            {
                physicsManager.OnBallPocketed -= PhysicsManager_OnBallHitPocket;
                physicsManager.OnBallEjectedFromPocket -= PhysicsManager_OnBallExitFromPocket;
            }
        }
        void PhysicsManager_OnBallExitFromPocket (BallDetector ball, PocketDetector pocket, BallExitType exitType, bool inMove)
        {
            if (ball == null || pocket == null)
            {
                return;
            }
            if(pocket.id == id)
            {
                currentLength += 2.0f * ball.Radius;
            }
        }

        void PhysicsManager_OnBallHitPocket (BallDetector ball, PocketDetector pocket, bool inMove)
        {
            if(pocket.id == id)
            {
                ball.pocket = pocket;
                OnBallHit(ball, inMove);
            }
        }

        void OnBallHit(BallDetector ball, bool inMove)
        {
            BallRoll roll = new BallRoll(ball, currentLength);

            ball.body.isKinematic = true;
            StartCoroutine(RollTheBall(roll));
            currentLength -= 2.0f * ball.Radius;
        }
//        void Update()
//        {
//            QuadraticCurve.CalculateLength(10, nodes); 
//        }
        IEnumerator RollTheBall(BallRoll roll)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            float time = 0.0f;
            Vector3 ballOldPosition = roll.ball.body.position;
            while (time < roll.maxLength && roll.ball.pocket && roll.ball.body.isKinematic) 
            {
                roll.ball.transform.position = QuadraticCurve.CalculateValue(time / length, nodes);
                Vector3 ballVelocity = (roll.ball.body.position - ballOldPosition) / Time.fixedDeltaTime;
                ballOldPosition = roll.ball.body.position;
                roll.ball.transform.Rotate(Vector3.Cross(ballVelocity, Vector3.up) / roll.ball.Radius);
                time += 0.5f * Time.fixedDeltaTime;
                physicsManager.TriggerBallMove(roll.ball.id, roll.ball.body.position, Vector3.zero, Vector3.zero);
                yield return new WaitForFixedUpdate();
            }
            if (roll.ball.pocket && roll.ball.body.isKinematic)
            {
                roll.ball.transform.position = QuadraticCurve.CalculateValue(0.99f * roll.maxLength / length, nodes);
                physicsManager.TriggerBallMove(roll.ball.id, roll.ball.body.position, Vector3.zero, Vector3.zero);
            }
            yield return new WaitForFixedUpdate();
        }
    }
}
