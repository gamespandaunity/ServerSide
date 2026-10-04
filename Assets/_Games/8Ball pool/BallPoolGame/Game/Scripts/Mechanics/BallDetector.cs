using System.Collections;
using BallPool.Mechanics;
using UnityEngine;

namespace BallPool
{
    public class BallDetector : MonoBehaviour
    {
        public int id;
        public Rigidbody body;
        public SphereCollider _collider;
        public PocketDetector pocket { get; set; }

        public PhysicsHandeler physicsManager;

        public float Radius { get; private set; }

        public int PocketID { get; set; }

        public int HitShapeID { get; set; }

        public Vector3 NormalizedVelocity { get { return body.linearVelocity / physicsManager.BallMaxVelocity; } }

        private bool isFirstHit = false;
        private bool isInMove;

        void OnCollisionEnter(Collision other)
        {
            if (physicsManager.IsInMove)
            {
                isFirstHit = true;
            }
            if (!isFirstHit && other.gameObject.layer == LayerMask.NameToLayer("Cloth"))
            {
                isFirstHit = true;
                if (id != 0)
                {
                    body.Sleep();
                }
            }
        }



        public void OnTriggerEnter(Collider other)
        {
            if (BallPoolGameLogic.playMode == PlayMode.Replay || BallPoolGameLogic.controlFromNetwork)
            {
                return;
            }
            PocketDetector pocket = other.GetComponent<PocketDetector>();
            if (pocket)
            {
                OnBallEnterPocket(pocket);
            }
        }

        public void OnBallEnterPocket(PocketDetector pocket)
        {
            if (!body.isKinematic)
            {
                body.isKinematic = true;
                PocketID = pocket.id;
                HitShapeID = -2;

                //Debug.Log(id + " OnEnterPocket");

                physicsManager.TriggerBallPocketed(this, pocket, true);

                if (id != 0)
                {
                    // Issue 2: show the ball in the pocketed-balls tray only AFTER the pocket drop
                    // animation (Pocket.RollTheBall) has visibly finished — not the instant the ball
                    // crosses the trigger — so the tray icon no longer pops while the ball is still
                    // rolling into the pocket.
                    StartCoroutine(SpawnTrayBallAfterAnimation(id));
                }


            }
        }

        private IEnumerator SpawnTrayBallAfterAnimation(int ballId)
        {
            yield return new WaitForSeconds(0.5f);
            // All tray spawns go through ONE global queue (deduped + spaced) so simultaneous
            // sources can never drop overlapping balls onto the tray spawn point. The registry
            // also lets the server reconciliation remove this icon if the pocket was divergent.
            BallSpawner.EnqueueTrayBall(ballId);
        }



        public void OnCollisionExit(Collision collision)
        {
            if (BallPoolGameLogic.playMode == PlayMode.Replay || BallPoolGameLogic.controlFromNetwork)
            {
                return;
            }
            BallDetector ball = collision.collider.GetComponent<BallDetector>();

            if (ball)
            {
                OnBallCollision(ball);
            }
            else if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Board"))
            {
                OnBallHitWall();
            }
        }

        //public void OnTriggerExit(Collider other)
        //{

        //}
        private void Start()
        {
            _collider = GetComponent<SphereCollider>();
        }
        public void OnBallCollision(BallDetector ball)
        {
            PocketID = -1;
            HitShapeID = ball.id;
            physicsManager.TriggerBallCollision(this, ball, true);
        }

        public void OnBallHitWall()
        {
            PocketID = -1;
            HitShapeID = -1;
            physicsManager.TriggerBallWallHit(this, true);
        }

        void Awake()
        {
            Radius = body.GetComponent<SphereCollider>().radius;
            PocketID = -1;
            HitShapeID = -2;
            isInMove = physicsManager.IsInMove;
        }

        void FixedUpdate()
        {
            if (!body.isKinematic && !body.IsSleeping() && physicsManager.IsInMove)
            {
                physicsManager.TriggerBallMove(id, body.position, body.linearVelocity, body.angularVelocity);
            }
            if (isInMove != physicsManager.IsInMove)
            {
                isInMove = physicsManager.IsInMove;
                if (!isInMove && !body.isKinematic)
                {
                    body.Sleep();
                }
            }
        }
    }
}
