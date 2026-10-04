using Mirror;
using UnityEngine;

public class SnokerAIManager : MonoBehaviour
{
    [Header("AI Configuration")]
    public SnokerGameManager _SnokerGameManager;
    public float aiHitForce;
    public float aiTurningSpeed;
    public int aiBallNum;
    public int aiBallsPottedInThisTurn;
    public float buffer = 0.18f;

    [Header("AI Calculation Data")]
    public bool aisaHoleIsOnLeft = true;
    public Vector3 aisaTargetToHoleDir;
    public int aisaTargetHole;
    public float[] aiAllPossibleHoleAngles = new float[6];
    public int[] aiBestPossibleHoleNumPerBall = new int[15];
    public float[] aiBestPossibleHoleAnglePerBall = new float[15];

    [Header("Rebound Shot Data")]
    public float aiReboundTurningStartCueX;
    public bool aiReboundTargetFound;

    // Constants for better maintainability
    private const float SCALE_CORRECTION = 10f;
    private const float ANGLE_THRESHOLD = 100f;
    private const float PRECISE_ANGLE_THRESHOLD = 2f;
    private const float HOLE_DISTANCE_THRESHOLD = 9f / 8f;
    private const float CUE_ANIM_START_VALUE = 0.03f;
    private const float CUE_ANIM_RETURN_VALUE = -0.065f;

    // Cached references
    private Transform cueBallTransform;
    private Transform cueParentTransform;
    private Rigidbody cueBallRigidbody;

    // Reusable variables to reduce GC allocation
    private RaycastHit lineHit;
    private Vector3 directionVector;
    private Vector3 fromVector;
    private Vector3 toVector;

    #region Initialization

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (_SnokerGameManager?._SnokerCueBall != null)
        {
            cueBallTransform = _SnokerGameManager._SnokerCueBall.thisTransform;
            cueParentTransform = _SnokerGameManager._SnokerCueBall.cueParentObjTransform;
            cueBallRigidbody = _SnokerGameManager._SnokerCueBall.thisRigidbody;
        }
    }

    #endregion

    #region AI Ball Placement

    private void aiPlaceBall()
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;

        // Detach from parent
        cueBall.cueBallParentTrans.parent = null;

        // Position cue ball with random offset
        Vector3 startPos = cueBall.CUEBALL_START_SNOOKER_POS +
                          new Vector3(0, 0f, 0.2f * _SnokerGameManager.getRandomOneOrMinusOne());
        cueBallTransform.position = startPos;

        // Reattach and reset physics
        cueBall.cueBallParentTrans.parent = cueBallTransform;
        cueBallRigidbody.linearVelocity = Vector3.zero;
        cueParentTransform.position = cueBallTransform.position;

        Invoke(nameof(aiPlaceBallNextStep), 1f);
    }

    private void aiPlaceBallNextStep()
    {
        SnokerGameManager.bBallInHand = false;
        if (SnokerNetwork.IsMultiplayer && NetworkServer.active)
        {
            StickManager.instance.SetBallInHand(true);
        }
        _SnokerGameManager._SnokerCueBall.setAllBallKinematic(false, 99);
        aiModeBasedCall();
    }

    #endregion

    #region AI Main Logic

    public void aiStart()
    {
        Debug.Log("AI Thinking to play");

        var cueBall = _SnokerGameManager._SnokerCueBall;
        var cameraManager = _SnokerGameManager._SnokerCameraManager;

        // Switch to AI camera if needed
        if (!cueBall.aiPlaying)
        {
            cameraManager.cameraSwitchMode(CAMERA_MODE.AI);
        }

        // Setup AI state
        cueBall.aiPlaying = true;
        cueBall.showBottomBlinkingText("Thinking...");
        cueBall.placeCueBtnObj.SetActive(false);
        cueBall.spinBtnObj.SetActive(false);
        cueBall.cueDistance = 0f;

        // Show guide if enabled
        if (!SnokerGameManager.bBallInHand && cueBall.showGuideForAI)
        {
            cueBall.showGuideWithType(GUIDE_TYPE.FULL);
        }

        // Execute AI logic
        if (string.IsNullOrEmpty(notificationScript.notifText))
        {
            aiModeBasedCall();
        }
        else
        {
            _SnokerGameManager.scheduledFunctionAfterNotif += aiModeBasedCall;
        }
    }

    private void aiModeBasedCall()
    {
        _SnokerGameManager.scheduledFunctionAfterNotif = null;
        Debug.Log("AI ModeBased Call");
        Invoke(nameof(aiStepSnooker), 1f);
    }

    private void aiStepSnooker()
    {
        if (SnokerGameManager.bBallInHand)
        {
            aiPlaceBall();
            return;
        }

        var cueBall = _SnokerGameManager._SnokerCueBall;

        // First strike - break shot
        if (cueBall.strikeCount == 0)
        {
            ExecuteBreakShot();
        }
        // Red balls or any ball mode
        else if (cueBall._SnokerGameManager.GetCurrentTargetAsInt() == 1 || cueBall._SnokerGameManager.GetCurrentTargetAsInt() == 99)
        {
            aiStepForBestBall(21);
        }
        // Specific colored ball
        else
        {
            aiBallNum = cueBall._SnokerGameManager.GetCurrentTargetAsInt() + 14 - 1;
            aiStepForTargetBallOnly();
        }
    }

    private void ExecuteBreakShot()
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;
        cueBall.spinValues.y = 0.2f;

        // Choose break target based on cue ball position
        int[] positiveTargets = { 1, 6, 10 };
        int[] negativeTargets = { 5, 9, 14 };

        aiBallNum = cueBallTransform.position.z > 0
            ? positiveTargets[Random.Range(0, positiveTargets.Length)]
            : negativeTargets[Random.Range(0, negativeTargets.Length)];

        aiJustHitTargetBall(0.5f);
    }

    #endregion

    #region Ball Analysis and Selection

    private void aiStepForBestBall(int arraySize)
    {
        // Initialize arrays
        aiBestPossibleHoleNumPerBall = new int[arraySize];
        aiBestPossibleHoleAnglePerBall = new float[arraySize];

        // Determine ball range
        int startBall, endBall;
        GetBallRange(out startBall, out endBall);

        // Analyze each ball
        AnalyzeBallsForBestShot(startBall, endBall);

        // Execute best shot
        ExecuteBestShotDecision(startBall, endBall);
    }

    private void GetBallRange(out int startBall, out int endBall)
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;

        if (cueBall._SnokerGameManager.GetCurrentTargetAsInt() == 99)
        {
            startBall = 15;
            endBall = 21;
        }
        else
        {
            startBall = 0;
            endBall = cueBall._SnokerGameManager.snookerRedsSelected;
        }
    }

    private void AnalyzeBallsForBestShot(int startBall, int endBall)
    {
        for (int ballIndex = startBall; ballIndex < endBall; ballIndex++)
        {
            if (!_SnokerGameManager.ballsArray[ballIndex].activeSelf)
            {
                SetBallAsInvalid(ballIndex);
                continue;
            }

            // Check if cue ball has clear path to target ball
            if (HasClearPathToBall(ballIndex))
            {
                CalculateBestHoleForBall(ballIndex);
            }
            else
            {
                SetBallAsInvalid(ballIndex);
            }
        }
    }

    private bool HasClearPathToBall(int ballIndex)
    {
        directionVector = _SnokerGameManager.ballsArray[ballIndex].transform.position - cueBallTransform.position;
        float distance = directionVector.magnitude - buffer;

        return !Physics.SphereCast(
            cueBallTransform.position,
            _SnokerGameManager.ballRadius,
            directionVector.normalized,
            out lineHit,
            distance,
            _SnokerGameManager._SnokerCueBall.ballLineLayerMask
        );
    }

    private void CalculateBestHoleForBall(int ballIndex)
    {
        aiAllPossibleHoleAngles = new float[6];
        fromVector = cueParentTransform.position - _SnokerGameManager.ballsArray[ballIndex].transform.position;

        // Check each hole
        for (int holeIndex = 0; holeIndex < 6; holeIndex++)
        {
            if (HasClearPathBallToHole(ballIndex, holeIndex))
            {
                toVector = _SnokerGameManager.holesTriggerPos[holeIndex].position -
                          _SnokerGameManager.ballsArray[ballIndex].transform.position;
                aiAllPossibleHoleAngles[holeIndex] = Vector3.Angle(fromVector, toVector);
            }
            else
            {
                aiAllPossibleHoleAngles[holeIndex] = 0f;
            }
        }

        // Find best hole for this ball
        float maxAngle = Mathf.Max(aiAllPossibleHoleAngles);
        for (int holeIndex = 0; holeIndex < 6; holeIndex++)
        {
            if (aiAllPossibleHoleAngles[holeIndex] == maxAngle)
            {
                aiBestPossibleHoleNumPerBall[ballIndex] = holeIndex;
                aiBestPossibleHoleAnglePerBall[ballIndex] = maxAngle;
                break;
            }
        }
    }

    private bool HasClearPathBallToHole(int ballIndex, int holeIndex)
    {
        Vector3 ballPos = _SnokerGameManager.ballsArray[ballIndex].transform.position;
        Vector3 holePos = _SnokerGameManager.holesTriggerPos[holeIndex].position;
        directionVector = holePos - ballPos;

        // Start the raycast slightly away from the ball to avoid self-collision
        Vector3 startPos = ballPos + directionVector.normalized * _SnokerGameManager.ballRadius;

        return !Physics.SphereCast(
            startPos,
            _SnokerGameManager.ballRadius,
            directionVector.normalized,
            out lineHit,
            directionVector.magnitude - _SnokerGameManager.ballRadius,
            _SnokerGameManager._SnokerCueBall.ballsLayerMask
        );
    }

    private void SetBallAsInvalid(int ballIndex)
    {
        aiBestPossibleHoleNumPerBall[ballIndex] = 0;
        aiBestPossibleHoleAnglePerBall[ballIndex] = 0f;
    }

    private void ExecuteBestShotDecision(int startBall, int endBall)
    {
        float bestAngle = Mathf.Max(aiBestPossibleHoleAnglePerBall);

        if (bestAngle > ANGLE_THRESHOLD)
        {
            ExecuteHighQualityShot(startBall, endBall, bestAngle);
        }
        else if (bestAngle > 0f)
        {
            ExecuteMediumQualityShot(startBall, endBall, bestAngle);
        }
        else
        {
            aiReboundShot();
        }
    }

    private void ExecuteHighQualityShot(int startBall, int endBall, float bestAngle)
    {
        for (int ballIndex = startBall; ballIndex < endBall; ballIndex++)
        {
            if (aiBestPossibleHoleAnglePerBall[ballIndex] == bestAngle)
            {
                aiBallNum = ballIndex;

                if (ShouldMakeMistake())
                {
                    aiJustHitTargetBall();
                }
                else
                {
                    aiShoot(aiBestPossibleHoleNumPerBall[ballIndex]);
                }
                break;
            }
        }
    }

    private void ExecuteMediumQualityShot(int startBall, int endBall, float bestAngle)
    {
        for (int ballIndex = startBall; ballIndex < endBall; ballIndex++)
        {
            if (aiBestPossibleHoleAnglePerBall[ballIndex] == bestAngle)
            {
                RecalculateAngleForBall(ballIndex);
                break;
            }
        }
    }

    private void RecalculateAngleForBall(int ballIndex)
    {
        aiAllPossibleHoleAngles = new float[6];
        fromVector = cueParentTransform.position - _SnokerGameManager.ballsArray[ballIndex].transform.position;

        for (int holeIndex = 0; holeIndex < 6; holeIndex++)
        {
            toVector = _SnokerGameManager.holesTriggerPos[holeIndex].position -
                      _SnokerGameManager.ballsArray[ballIndex].transform.position;
            aiAllPossibleHoleAngles[holeIndex] = Vector3.Angle(fromVector, toVector);
        }

        float maxAngle = Mathf.Max(aiAllPossibleHoleAngles);
        for (int holeIndex = 0; holeIndex < 6; holeIndex++)
        {
            if (aiAllPossibleHoleAngles[holeIndex] == maxAngle)
            {
                aiBallNum = ballIndex;
                aiShoot(holeIndex);
                break;
            }
        }
    }

    private bool ShouldMakeMistake()
    {
        var difficulty = _SnokerGameManager._SnokerCueBall.aiDifficulty;

        return (difficulty == AI_DIFFICULTY.EASY && aiBallsPottedInThisTurn > 1) ||
               (difficulty == AI_DIFFICULTY.MEDIUM && aiBallsPottedInThisTurn > 3);
    }

    #endregion

    #region Target Ball Only Logic

    private void aiStepForTargetBallOnly()
    {
        if (!HasClearPathToBall(aiBallNum))
        {
            aiReboundShot();
            return;
        }

        // Calculate best hole for target ball
        aiAllPossibleHoleAngles = new float[6];
        fromVector = cueParentTransform.position - _SnokerGameManager.ballsArray[aiBallNum].transform.position;

        for (int holeIndex = 0; holeIndex < 6; holeIndex++)
        {
            if (HasClearPathBallToHole(aiBallNum, holeIndex))
            {
                toVector = _SnokerGameManager.holesTriggerPos[holeIndex].position -
                          _SnokerGameManager.ballsArray[aiBallNum].transform.position;
                aiAllPossibleHoleAngles[holeIndex] = Vector3.Angle(fromVector, toVector);
            }
            else
            {
                aiAllPossibleHoleAngles[holeIndex] = 0f;
            }
        }

        float bestAngle = Mathf.Max(aiAllPossibleHoleAngles);
        for (int holeIndex = 0; holeIndex < 6; holeIndex++)
        {
            if (aiAllPossibleHoleAngles[holeIndex] == bestAngle)
            {
                if (ShouldMakeMistakeForTargetBall())
                {
                    aiJustHitTargetBall();
                }
                else
                {
                    aiShoot(holeIndex);
                }
                break;
            }
        }
    }

    private bool ShouldMakeMistakeForTargetBall()
    {
        var difficulty = _SnokerGameManager._SnokerCueBall.aiDifficulty;

        return (difficulty == AI_DIFFICULTY.EASY && aiBallsPottedInThisTurn > 1) ||
               (difficulty == AI_DIFFICULTY.MEDIUM && aiBallsPottedInThisTurn > 2);
    }

    #endregion

    #region Shooting Logic

    private void aiJustHitTargetBall(float forceOptional = 0f)
    {
        // Aim at target ball with slight randomization
        cueParentTransform.LookAt(_SnokerGameManager.ballsArray[aiBallNum].transform);

        var cueBall = _SnokerGameManager._SnokerCueBall;
        cueBall.cueRotValueX = cueParentTransform.rotation.eulerAngles.y;
        cueBall.cueRotValueX += 0.3f * _SnokerGameManager.getRandomOneOrMinusOne();
        cueParentTransform.eulerAngles = new Vector3(0f, cueBall.cueRotValueX, 0f);

        // Calculate force
        float forceMultiplier = 1.5f;

        // Increase force if there's clear path to table edge (for safety play)
        if (cueBall.aiDifficulty != AI_DIFFICULTY.EASY && HasClearPathToTableEdge())
        {
            forceMultiplier = 3.5f;
        }

        CancelInvoke("aiSetAngle");

        if (forceOptional == 0f)
        {
            float distance = Vector3.Distance(cueBallTransform.position,
                                            _SnokerGameManager.ballsArray[aiBallNum].transform.position);
            aiHitForce = distance * forceMultiplier / 65f * SCALE_CORRECTION;
        }
        else
        {
            aiHitForce = forceOptional;
        }

        Debug.Log("AI Power: " + aiHitForce);
        aiCueAnimStart();
    }

    private bool HasClearPathToTableEdge()
    {
        return Physics.Raycast(
            cueParentTransform.position,
            -cueParentTransform.forward,
            out lineHit,
            100f,
            _SnokerGameManager._SnokerCueBall.tableSideLayerMask
        ) && lineHit.distance > Vector3.Distance(cueBallTransform.position,
                                               _SnokerGameManager.ballsArray[aiBallNum].transform.position);
    }

    private void aiShoot(int targetHole)
    {
        // Initial aim at target ball
        cueParentTransform.LookAt(_SnokerGameManager.ballsArray[aiBallNum].transform);

        var cueBall = _SnokerGameManager._SnokerCueBall;
        cueBall.cueRotValueX = cueParentTransform.rotation.eulerAngles.y;

        // Calculate vectors for angle adjustment
        Vector3 cueToTargetBall = cueParentTransform.position -
                                 _SnokerGameManager.ballsArray[aiBallNum].transform.position;
        aisaTargetToHoleDir = _SnokerGameManager.holesTriggerPos[targetHole].position -
                             _SnokerGameManager.ballsArray[aiBallNum].transform.position;
        aisaTargetHole = targetHole;

        // Set turning speed based on distance
        float distanceToTarget = Vector3.Distance(cueBallTransform.position,
                                                _SnokerGameManager.ballsArray[aiBallNum].transform.position);
        aiTurningSpeed = distanceToTarget > 0.33f ? 0.04f : 0.2f;

        // Determine turning direction
        Vector3 crossProduct = Vector3.Cross(cueToTargetBall, aisaTargetToHoleDir);

        if (Mathf.Abs(crossProduct.y) > 0.001f) // Avoid floating point precision issues
        {
            aisaHoleIsOnLeft = crossProduct.y > 0f;
            InvokeRepeating("aiSetAngle", 0.1f, 0.01f);
        }
        else
        {
            // Direct shot
            aiJustHitTargetBall();
        }
    }

    private void aiSetAngle()
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;

        // Adjust angle
        if (aisaHoleIsOnLeft)
        {
            cueBall.cueRotValueX += aiTurningSpeed;
        }
        else
        {
            cueBall.cueRotValueX -= aiTurningSpeed;
        }

        cueParentTransform.eulerAngles = new Vector3(0f, cueBall.cueRotValueX, 0f);

        // Check if we're aiming at the correct ball
        if (!Physics.SphereCast(cueBallTransform.position, _SnokerGameManager.ballRadius,
                               cueParentTransform.forward, out lineHit, 100f,
                               _SnokerGameManager._SnokerCueBall.ballsLayerMask))
        {
            aiJustHitTargetBall();
            return;
        }

        int hitBallNum = int.Parse(lineHit.collider.name);

        // Validate target ball
        if (!IsCorrectTargetBall(hitBallNum))
        {
            aiJustHitTargetBall();
            return;
        }

        // Check if angle is correct for potting
        if (Vector3.Angle(aisaTargetToHoleDir, -lineHit.normal) < PRECISE_ANGLE_THRESHOLD)
        {
            // Additional check: make sure we won't scratch
            if (WillScratchAfterShot())
            {
                aiJustHitTargetBall();
                return;
            }

            CancelInvoke("aiSetAngle");

            // Calculate shot force
            float totalDistance = Vector3.Distance(cueBallTransform.position,
                                                 _SnokerGameManager.ballsArray[aiBallNum].transform.position) +
                                Vector3.Distance(_SnokerGameManager.ballsArray[aiBallNum].transform.position,
                                               _SnokerGameManager.holesTriggerPos[aisaTargetHole].position);

            aiHitForce = totalDistance / 70f * SCALE_CORRECTION;
            aiCueAnimStart();
        }
    }

    private bool IsCorrectTargetBall(int hitBallNum)
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;
        int expectedTarget;

        if (hitBallNum < 16)
        {
            expectedTarget = 1; // Red ball
        }
        else if (cueBall._SnokerGameManager.GetCurrentTargetAsInt() == 99)
        {
            expectedTarget = 99; // Any colored ball
        }
        else if (SnokerGameManager.snookerRedPottedCount != cueBall._SnokerGameManager.snookerRedsSelected)
        {
            expectedTarget = 99; // Still potting reds
        }
        else
        {
            expectedTarget = hitBallNum - 14; // Specific colored ball
        }

        return expectedTarget == cueBall._SnokerGameManager.GetCurrentTargetAsInt();
    }

    private bool WillScratchAfterShot()
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;

        return cueBall.aiDifficulty != AI_DIFFICULTY.EASY &&
               Physics.Raycast(cueBall.guideColRingPosVec, cueBall.guideDirCueBallTrans.forward,
                              out lineHit, 100f, cueBall.tableSideLayerMask) &&
               lineHit.collider.CompareTag("colHoleTag") &&
               lineHit.distance < HOLE_DISTANCE_THRESHOLD;
    }

    #endregion

    #region Rebound Shot Logic

    private void aiReboundShot()
    {
        if (_SnokerGameManager._SnokerCueBall.aiDifficulty == AI_DIFFICULTY.EASY)
        {
            aiJustHitTargetBall();
            return;
        }

        // Find first available target ball
        FindFirstAvailableTargetBall();

        // Setup rebound shot
        cueParentTransform.LookAt(_SnokerGameManager.ballsArray[aiBallNum].transform);

        var cueBall = _SnokerGameManager._SnokerCueBall;
        cueBall.cueRotValueX = cueParentTransform.rotation.eulerAngles.y;
        aiReboundTurningStartCueX = cueBall.cueRotValueX;
        aiReboundTargetFound = false;

        InvokeRepeating("aiReboundTurningInvoke", 0.1f, 0.01f);
    }

    private void FindFirstAvailableTargetBall()
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;
        int startBall, endBall;

        if (cueBall._SnokerGameManager.GetCurrentTargetAsInt() == 1)
        {
            startBall = 0;
            endBall = cueBall._SnokerGameManager.snookerRedsSelected;
        }
        else if (cueBall._SnokerGameManager.GetCurrentTargetAsInt() == 99)
        {
            startBall = 15;
            endBall = 21;
        }
        else
        {
            // Target ball is already set
            return;
        }

        for (int i = startBall; i < endBall; i++)
        {
            if (_SnokerGameManager.ballsArray[i].activeSelf)
            {
                aiBallNum = i;
                break;
            }
        }
    }

    private void aiReboundTurningInvoke()
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;

        // Rotate cue
        cueBall.cueRotValueX += 0.8f;
        cueParentTransform.eulerAngles = new Vector3(0f, cueBall.cueRotValueX, 0f);

        // Check for wall hit and subsequent ball hit
        if (CheckReboundShot())
        {
            CancelInvoke("aiReboundTurningInvoke");

            // Calculate rebound shot force
            float distance1 = Vector3.Distance(cueBallTransform.position, cueBall.guideColRingPosVec);
            float distance2 = Vector3.Distance(cueBall.guideColRingPosVec, lineHit.point);
            aiHitForce = (distance1 + distance2) / 75f * SCALE_CORRECTION;

            aiCueAnimStart();
            return;
        }

        // Check if we've completed a full rotation
        if (cueBall.cueRotValueX > aiReboundTurningStartCueX + 360f)
        {
            CancelInvoke("aiReboundTurningInvoke");
            aiJustHitTargetBall();
        }
    }

    private bool CheckReboundShot()
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;

        // Check for wall collision
        if (!Physics.SphereCast(cueBallTransform.position, _SnokerGameManager.ballRadius,
                               cueParentTransform.forward, out lineHit, 100f,
                               cueBall.ballLineLayerMask) ||
            !lineHit.collider.CompareTag("colSideTag"))
        {
            return false;
        }

        // Check for ball hit after rebound
        RaycastHit ballHit;
        if (!Physics.SphereCast(cueBall.guideColRingPosVec, _SnokerGameManager.ballRadius,
                               cueBall.guideReflectDirVec, out ballHit, 100f,
                               cueBall.ballsLayerMask))
        {
            return false;
        }

        int hitBallNum = int.Parse(ballHit.collider.name);

        // Check if this is the correct target
        if (cueBall._SnokerGameManager.GetCurrentTargetAsInt() == 1)
        {
            aiReboundTargetFound = hitBallNum < 16; // Red ball
        }
        else if (cueBall._SnokerGameManager.GetCurrentTargetAsInt() == 99)
        {
            aiReboundTargetFound = hitBallNum >= 16; // Colored ball
        }
        else
        {
            aiReboundTargetFound = (hitBallNum - 1) == aiBallNum; // Specific ball
        }

        return aiReboundTargetFound;
    }

    #endregion

    #region Cue Animation

    private void aiCueAnimStart()
    {
        _SnokerGameManager._SnokerCueBall.toggleSelectedCue(true);
        _SnokerGameManager._SnokerCueBall.aiCueAnimValue = CUE_ANIM_START_VALUE;
        InvokeRepeating("aiAnimateCue", 1.1f, 0.01f);
    }

    private void aiAnimateCue()
    {
        var cueBall = _SnokerGameManager._SnokerCueBall;

        cueBall.cueDistance += cueBall.aiCueAnimValue;

        if (cueBall.cueDistance > 1f)
        {
            cueBall.aiCueAnimValue = CUE_ANIM_RETURN_VALUE;
        }

        if (cueBall.cueDistance <= 0f)
        {
            CancelInvoke("aiAnimateCue");
            Debug.Log("AI Shooting");

            cueBall.hitTheBall(
                aiHitForce,
                cueParentTransform.forward,
                cueBall.guideDirCueBallTrans.forward,
                true
            );
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Cancels all active AI coroutines and resets state
    /// </summary>
    public void CancelAllAIOperations()
    {
        CancelInvoke("aiSetAngle");
        CancelInvoke("aiReboundTurningInvoke");
        CancelInvoke("aiAnimateCue");
        CancelInvoke("aiStepSnooker");
        CancelInvoke("aiPlaceBallNextStep");

        aiReboundTargetFound = false;
        _SnokerGameManager._SnokerCueBall.aiPlaying = false;
    }

    /// <summary>
    /// Debug method to visualize AI calculations
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || _SnokerGameManager == null) return;

        // Draw line to target ball
        if (aiBallNum >= 0 && aiBallNum < _SnokerGameManager.ballsArray.Length &&
            _SnokerGameManager.ballsArray[aiBallNum] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cueBallTransform.position,
                           _SnokerGameManager.ballsArray[aiBallNum].transform.position);
        }

        // Draw line to target hole
        if (aisaTargetHole >= 0 && aisaTargetHole < _SnokerGameManager.holesTriggerPos.Length)
        {
            Gizmos.color = Color.green;
            if (aiBallNum >= 0 && aiBallNum < _SnokerGameManager.ballsArray.Length &&
                _SnokerGameManager.ballsArray[aiBallNum] != null)
            {
                Gizmos.DrawLine(_SnokerGameManager.ballsArray[aiBallNum].transform.position,
                               _SnokerGameManager.holesTriggerPos[aisaTargetHole].position);
            }
        }
    }

    /// <summary>
    /// Get AI difficulty as string for debugging
    /// </summary>
    public string GetAIDifficultyString()
    {
        if (_SnokerGameManager?._SnokerCueBall == null) return "Unknown";

        switch (_SnokerGameManager._SnokerCueBall.aiDifficulty)
        {
            case AI_DIFFICULTY.EASY: return "Easy";
            case AI_DIFFICULTY.MEDIUM: return "Medium";
            case AI_DIFFICULTY.HARD: return "Hard";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// Get current AI state for debugging
    /// </summary>
    public string GetAIState()
    {
        var cueBall = _SnokerGameManager?._SnokerCueBall;
        if (cueBall == null) return "Not Initialized";

        if (!cueBall.aiPlaying) return "Waiting";
        if (IsInvoking("aiSetAngle")) return "Adjusting Angle";
        if (IsInvoking("aiReboundTurningInvoke")) return "Calculating Rebound";
        if (IsInvoking("aiAnimateCue")) return "Executing Shot";
        if (IsInvoking("aiStepSnooker")) return "Thinking";

        return "Processing";
    }
}

#endregion