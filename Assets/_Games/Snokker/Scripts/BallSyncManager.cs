//using Mirror;
//using UnityEngine;

//public class BallSyncManager : NetworkBehaviour
//{
//    public SnokerGameManager gameManager; // reference in inspector

//    public GameObject[] ballsArray;
//    public Rigidbody[] ballsRigidbodyArray;

//    [Header("Lerping Settings")]
//    [Range(5f, 25f)] public float positionLerpSpeed = 5f;
//    [Range(5f, 25f)] public float velocityLerpSpeed = 5f;
//    [Range(5f, 25f)] public float angularVelocityLerpSpeed = 3f;
//    [Range(0.001f, 0.1f)] public float stopThreshold = 0.001f;
//    [Range(0.01f, 0.5f)] public float velocityStopThreshold = 0.05f;

//    private Vector3[] targetPositions;
//    private Quaternion[] targetRotations;
//    private Vector3[] targetLinearVelocities;
//    private Vector3[] targetAngularVelocities;
//    private bool[] targetIsKinematic;
//    private bool[] shouldLerp;


//    void Awake()
//    {
//    }

//    public void Start()
//    {
//        targetPositions = new Vector3[ballsArray.Length];
//        targetRotations = new Quaternion[ballsArray.Length];
//        targetLinearVelocities = new Vector3[ballsArray.Length];
//        targetAngularVelocities = new Vector3[ballsArray.Length];
//        targetIsKinematic = new bool[ballsArray.Length];
//        shouldLerp = new bool[ballsArray.Length];

//        for (int i = 0; i < ballsArray.Length; i++)
//        {
//            if (ballsArray[i] != null)
//            {
//                targetPositions[i] = ballsArray[i].transform.position;
//                targetRotations[i] = ballsArray[i].transform.rotation;
//                targetLinearVelocities[i] = ballsRigidbodyArray[i].linearVelocity;
//                targetAngularVelocities[i] = ballsRigidbodyArray[i].angularVelocity;
//                targetIsKinematic[i] = ballsRigidbodyArray[i].isKinematic;
//            }
//        }
//    }

//    void Update()
//    {
//        if (gameManager.isyourturn && !gameManager.ballIsStanding)
//        {
//            // Send ball sync data to other players
//            SendBallSyncData();
//        }

//        if (!gameManager.isyourturn && !gameManager.ballIsStanding)
//        {
//            ApplyLerping();
//        }
//    }

//    void SendBallSyncData()
//    {
//        // Send ball data in chunks to avoid RPC size limits
//        // Fusion has limits on RPC parameter count, so we'll send in smaller chunks

//        for (int i = 0; i < ballsArray.Length; i++)
//        {
//            if (ballsArray[i] == null) continue;

//            Vector3 pos = ballsArray[i].transform.position;
//            Vector3 linearVel = ballsRigidbodyArray[i].linearVelocity;
//            Vector3 angularVel = ballsRigidbodyArray[i].angularVelocity;
//            bool isKinematic = ballsRigidbodyArray[i].isKinematic;

//            // Send each ball's data individually to avoid parameter limits
//            RPC_SyncBallData(i, pos, linearVel, angularVel, isKinematic);
//        }
//    }

//    void RPC_SyncBallData(int ballIndex, Vector3 position, Vector3 linearVelocity, Vector3 angularVelocity, bool isKinematic)
//    {
//        if (ballIndex < 0 || ballIndex >= ballsArray.Length || ballsArray[ballIndex] == null)
//            return;

//        // Your existing game state logic for cue ball (index 0)
//        if (ballIndex == 0 && linearVelocity.magnitude >= 0.02f && gameManager.ballIsStanding)
//        {
//            gameManager.ballIsStanding = false;
//            gameManager._SnokerCueBall.snookerFirstTouchedBallNum = 0;
//            gameManager._SnokerCueBall.snookerBallInvolvedInFoul = 0;
//            gameManager._SnokerCueBall.snookerPointsCurShot = 0;
//            StartCoroutine(gameManager._SnokerCueBall.HandleBallStandingCheck());

//            if (gameManager._SnokerCueBall.guideType != GUIDE_TYPE.NO)
//            {
//                gameManager._SnokerCueBall.showGuideWithType(GUIDE_TYPE.NO);
//            }
//            gameManager._SnokerCueBall.toggleSelectedCue(false);
//        }

//        // Set target values for lerping
//        targetPositions[ballIndex] = position;
//        targetLinearVelocities[ballIndex] = Vector3.zero;
//        targetAngularVelocities[ballIndex] = Vector3.zero;
//        targetLinearVelocities[ballIndex] = linearVelocity;
//        targetAngularVelocities[ballIndex] = angularVelocity;
//        targetIsKinematic[ballIndex] = isKinematic;

//        // Calculate target rotation based on angular velocity
//        if (angularVelocity.magnitude > 0.01f)
//        {
//            Quaternion deltaRotation = Quaternion.Euler(angularVelocity * Time.fixedDeltaTime * Mathf.Rad2Deg);
//            targetRotations[ballIndex] = deltaRotation * ballsArray[ballIndex].transform.rotation;
//        }
//        else
//        {
//            targetRotations[ballIndex] = ballsArray[ballIndex].transform.rotation;
//        }

//        shouldLerp[ballIndex] = true;
//    }

//    // Alternative method using bulk sync (if you want to send all at once)
//    void RPC_SyncAllBallsData(
//        Vector3[] positions,
//        Vector3[] linearVelocities,
//        Vector3[] angularVelocities,
//        bool[] kinematicStates)
//    {
//        {
//            return; // Sender ko apply nahi karna
//        }
//        for (int i = 0; i < ballsArray.Length && i < positions.Length; i++)
//        {
//            if (ballsArray[i] == null) continue;

//            // Same logic as individual sync
//            if (i == 0 && linearVelocities[i].magnitude >= 0.02f && gameManager.ballIsStanding)
//            {
//                gameManager.ballIsStanding = false;
//                gameManager._SnokerCueBall.snookerFirstTouchedBallNum = 0;
//                gameManager._SnokerCueBall.snookerBallInvolvedInFoul = 0;
//                gameManager._SnokerCueBall.snookerPointsCurShot = 0;
//                StartCoroutine(gameManager._SnokerCueBall.HandleBallStandingCheck());

//                if (gameManager._SnokerCueBall.guideType != GUIDE_TYPE.NO)
//                {
//                    gameManager._SnokerCueBall.showGuideWithType(GUIDE_TYPE.NO);
//                }
//                gameManager._SnokerCueBall.toggleSelectedCue(false);
//            }

//            targetPositions[i] = positions[i];
//            targetLinearVelocities[i] = linearVelocities[i];
//            targetAngularVelocities[i] = angularVelocities[i];
//            targetIsKinematic[i] = kinematicStates[i];

//            if (angularVelocities[i].magnitude > 0.01f)
//            {
//                Quaternion deltaRotation = Quaternion.Euler(angularVelocities[i] * Time.fixedDeltaTime * Mathf.Rad2Deg);
//                targetRotations[i] = deltaRotation * ballsArray[i].transform.rotation;
//            }
//            else
//            {
//                targetRotations[i] = ballsArray[i].transform.rotation;
//            }

//            shouldLerp[i] = true;
//        }
//    }

   

//    void ApplyLerping()
//    {
//        float deltaTime = Time.deltaTime;

//        for (int i = 0; i < ballsArray.Length; i++)
//        {
//            if (ballsArray[i] != null && shouldLerp[i])
//            {
//                Transform ballTransform = ballsArray[i].transform;
//                Rigidbody ballRigidbody = ballsRigidbodyArray[i];
//                // Debug.Log("Lerping : " + ballsArray[i].name);

//                // Apply kinematic state first
//                if (ballRigidbody.isKinematic != targetIsKinematic[i])
//                {
//                    ballRigidbody.isKinematic = targetIsKinematic[i];
//                }

//                // Position lerping with boundary clamping
//                Vector3 currentPos = ballTransform.position;
//                Vector3 newPos = Vector3.Lerp(currentPos, targetPositions[i], positionLerpSpeed * deltaTime);

//                // Clamp to table boundaries (this prevents balls going off table)
//                newPos.x = Mathf.Clamp(newPos.x, -2.07f, 2.07f);
//                newPos.z = Mathf.Clamp(newPos.z, -1.031f, 1.031f);

//                ballTransform.position = newPos;

//                // Rotation lerping
//                Quaternion newRotation = Quaternion.Slerp(
//                    ballTransform.rotation,
//                    targetRotations[i],
//                    positionLerpSpeed * deltaTime
//                );
//                ballTransform.rotation = newRotation;

//                // Velocity lerping (only for non-kinematic bodies)
//                if (!ballRigidbody.isKinematic)
//                {
//                    // Linear velocity lerping
//                    Vector3 newLinearVel = Vector3.Lerp(
//                        ballRigidbody.linearVelocity,
//                        targetLinearVelocities[i],
//                        velocityLerpSpeed * deltaTime
//                    );
//                    ballRigidbody.linearVelocity = newLinearVel;

//                    // Angular velocity lerping
//                    Vector3 newAngularVel = Vector3.Lerp(
//                        ballRigidbody.angularVelocity,
//                        targetAngularVelocities[i],
//                        angularVelocityLerpSpeed * deltaTime
//                    );
//                    ballRigidbody.angularVelocity = newAngularVel;
//                }

//                // Check if lerping should stop (your existing logic)
//                bool positionClose = Vector3.Distance(currentPos, targetPositions[i]) < stopThreshold;
//                bool linearVelClose = Vector3.Distance(ballRigidbody.linearVelocity, targetLinearVelocities[i]) < velocityStopThreshold;
//                bool angularVelClose = Vector3.Distance(ballRigidbody.angularVelocity, targetAngularVelocities[i]) < velocityStopThreshold;

//                if (positionClose && (ballRigidbody.isKinematic || (linearVelClose && angularVelClose)))
//                {
//                    // Snap to final values with clamping
//                    Vector3 finalPos = targetPositions[i];
//                    finalPos.x = Mathf.Clamp(finalPos.x, -2.07f, 2.07f);
//                    finalPos.z = Mathf.Clamp(finalPos.z, -1.031f, 1.031f);

//                    ballTransform.position = finalPos;
//                    ballTransform.rotation = targetRotations[i];

//                    if (!ballRigidbody.isKinematic)
//                    {
//                        ballRigidbody.linearVelocity = targetLinearVelocities[i];
//                        ballRigidbody.angularVelocity = targetAngularVelocities[i];
//                    }

//                    shouldLerp[i] = false;
//                }
//            }
//        }
//    }

//    // Helper methods for debugging/manual control
//    public void ForceStopLerping(int ballIndex = -1)
//    {
//        if (ballIndex == -1)
//        {
//            // Stop all
//            for (int i = 0; i < shouldLerp.Length; i++)
//            {
//                shouldLerp[i] = false;
//            }
//        }
//        else if (ballIndex >= 0 && ballIndex < shouldLerp.Length)
//        {
//            shouldLerp[ballIndex] = false;
//        }
//    }

//    public void ForceSnapToTarget(int ballIndex)
//    {
//        if (ballIndex >= 0 && ballIndex < ballsArray.Length && ballsArray[ballIndex] != null)
//        {
//            ballsArray[ballIndex].transform.position = targetPositions[ballIndex];
//            ballsArray[ballIndex].transform.rotation = targetRotations[ballIndex];
//            ballsRigidbodyArray[ballIndex].linearVelocity = targetLinearVelocities[ballIndex];
//            ballsRigidbodyArray[ballIndex].angularVelocity = targetAngularVelocities[ballIndex];
//            ballsRigidbodyArray[ballIndex].isKinematic = targetIsKinematic[ballIndex];
//            shouldLerp[ballIndex] = false;
//        }
//    }
//}