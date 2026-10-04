// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController (MAIN partial) — decompiled cricket match engine, being restructured in phases.
// This partial owns: Unity lifecycle (Update/state pump), the match ACTION STATE machine
// (currentActionState: -2 bowler-arm • -1 review-hold • 2 run-up • 3 delivery in flight • 4 post-shot),
// ResetAll (per-delivery state reset — EVERY new flag must be cleared here), the live bat-collider
// contact block (presentation-only under lockstep), the legacy 20Hz ball-position stream, and the
// scheduled run-up start (DelayedFunction, incl. the LOCKED DELIVERY PLAN mint).
// Partials: .Delivery (flight+tables) .Batting (input+params) .Fielding (keeper/fielders)
//           .Camera .Reconnect .ReviewReplay
// ════════════════════════════════════════════════════════════════════════════════════════════
using Beebyte.Obfuscator;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Cricket;
using UnityEngine.Serialization;

public partial class GroundController : Singleton<GroundController>
{
    //-0.6,0.06,5.7 -----> 0.6,0.06,5.7

    //Photon Removal  [FormerlySerializedAs("photonView")] public PhotonView view;

    [FormerlySerializedAs("sixDistanceCamera")] public Camera sixRunCam;

    [FormerlySerializedAs("BG")] public Image backgroundImage;

    private Transform bowlingSpot;

    private Transform _mainUmpireTransform;

    private Transform _batsmanTransform;

    private Transform _runnerTransform;

    private Transform ballStartPoint;

    private Transform fielder10SkinTransform;

    private Transform _wicketKeeperTransform;

    private Transform _bowlerTransform;

    private bool isSwipeAllowed;

    [FormerlySerializedAs("BallHitEffect")] public Transform ballImpactEffect;

    private Transform mainCamTransform;

    private Transform umpireCamTransform;

    private Transform introCamTransform;

    private Transform replayCamTransform;

    private Transform rightCamTransform;

    private Transform leftCamTransform;

    private Transform ultraMotionCameraTransform;

    private Transform closeUpCameraTransform;

    private float degToRad = (float)Math.PI / 180f;

    private float radToDeg = 180f / (float)Math.PI;

    private int selectedBowlerIndex;

    private bool shouldPlayIntro = true;

    private string BattingBy;

    private string BowlingBy;

    private bool isFullToss;

    // --- Ball position streaming (Option A — bowling player is authoritative) ---
    // Bowling player streams ball position to batting player at BALL_STREAM_HZ per second.
    // Batting player lerps its local ball toward the received position each frame,
    // eliminating all trajectory divergence (bat/pad hit-miss, bounce drift, etc.).
    private float _ballStreamTimer;
    private const float BALL_STREAM_INTERVAL = 0.05f; // 20 Hz
    // On batting player: the latest authoritative position received from bowling player.
    // Initialized to Vector3.zero (invalid) so we only lerp once a packet has arrived.
    private Vector3 _networkBallPosition = Vector3.zero;
    private bool _hasNetworkBallPosition = false;

    // [CamFollowDiag] Bowling-side post-shot camera-follow diagnostic. Counts how many post-shot
    // position packets (RPC_SyncBallShot) this client actually applied for the current ball, plus a
    // throttle timer so the per-frame follow logger prints at most ~4 summary lines/second.
    private int _shotSyncRecvCount;
    private float _camFollowDiagTimer;

    // [CamFollow] Bowling-side camera-only PREDICTIVE follow target. The post-shot ball is rebuilt from
    // 20Hz network re-base snaps (RPC_SyncBallShot), so the follow camera jerks each packet. This reads
    // matchBallTransform.position / _ballAngle / horizontalVelocity ONLY and writes ONLY these fields +
    // _camFollowProxy.position — it NEVER writes temporaryPosition or matchBallTransform.position (easing
    // those broke keeper-catch + shot-contact twice; both are read by catch geometry). Camera-only, safe.
    private Vector3 _camFollowTarget;     // smoothed + small-lead follow point the cameras aim at
    private Vector3 _camFollowVel;        // SmoothDamp carried velocity (owned by SmoothDamp)
    private Vector3 _camRawPrev;          // last-frame raw ball pos (world-space reversal/freeze detection)
    private float   _camPredictScale;     // 0..1 eased prediction gain (fades lead in/out, no step engage)
    private bool    _camFollowInit;       // seeded onto the ball this delivery yet
    private Transform _camFollowProxy;    // inert proxy Transform fed to SmoothFollowLookAt (no collider)
    private const float CAM_SMOOTH_TIME       = 0.10f; // SmoothDamp approach time — primary smoothness knob
    private const float CAM_LEAD_SECONDS      = 0.09f; // look-ahead time; ≈ CAM_SMOOTH_TIME so the speed-scaled
                                                       // lead (vel*this) cancels the SmoothDamp follow-lag — a
                                                       // FAST four/six leads further so the camera keeps up (#3).
    private const float CAM_LEAD_MAX          = 1.8f;  // hard clamp on lead DISTANCE (raised so a fast ball is
                                                       // not throttled; lead still collapses to 0 on catch/stop).
    private const float CAM_LEAD_MIN_SPEED    = 4f;    // below this horiz speed -> no lead (slow/settling)
    private const float CAM_PREDICT_FULL_SPEED = 18f;  // horiz speed at which lead gain reaches 1
    private const float CAM_PREDICT_EASE      = 8f;    // 1/s ease rate for the prediction gain fade
    private const float CAM_MAX_LAG           = 4f;    // leash: max trail distance behind the real ball

    // Previous authoritative position (one packet back) so the batting client can
    // interpolate/extrapolate the authoritative X at the stump plane when validating a
    // local stump raycast hit. Prevents false "bowled" caused by lerp-lag / local drift.
    private Vector3 _prevNetworkBallPosition = Vector3.zero;
    private bool _hasPrevNetworkBallPosition = false;

    // Bowling client: ensures a relayed stump (bowled) is applied exactly once per delivery,
    // regardless of how many times the batting client re-broadcasts the collider RPC.
    private bool _remoteBowledAppliedThisDelivery = false;

    // Bowling client: ensures a relayed BAT contact (shot) is applied exactly once per delivery.
    // A genuine bat contact is detected at the very end of the batting client's (lerp-lagged) flight,
    // so the collider + shot-angle relays usually arrive after the bowling ball has already passed the
    // contact z. The old z-window/early-returns then dropped them and the bowling client never launched
    // the shot ("shot connects on batting, miss on bowling"). We apply the relay immediately + once.
    private bool _remoteShotAppliedThisDelivery = false;
    // Post-shot stream: true once the FIRST RPC_SyncBallShot packet fully seeded this delivery's flight
    // (position + params + clock). Later packets snap only on genuine divergence (boundary-jerk fix).
    internal bool _shotStreamSeeded = false;
    // #1 (bowler-side behind-shot ball-clipping): set true once RPC_ChangeBallAngle relays the authoritative
    // bat-contact _ballAngle to the bowling follower, so the 20Hz RPC_SyncBallShot stream stops CLOBBERING it
    // with a stale pre-contact angle (logs: the stream carried ballAngle=81.8 over the real ~301 behind-shot →
    // the deterministic step re-based onto an off-pitch trajectory → the ball froze on the pitch instead of
    // travelling behind the keeper, while the batting side showed the full four/catch).
    private bool _contactAngleRelayedThisDelivery = false;
    // The authoritative bat-contact world position relayed from the batting client. Used as the shot's
    // launch ORIGIN on the bowling client — BALLANGLE/HORIZONTALSPEED/projectile params define the shot
    // SHAPE but not the world origin, on which ballStartPoint / temporaryPosition / fielding & catch
    // geometry all depend, so the origin must be snapped to the batting contact point.
    private Vector3 _remoteContactPos = Vector3.zero;
    private bool _hasRemoteContactPos = false;

    private float fullBallLength;

    private float _fullTossDisplacementFromCrease;

    private float fullTossCalcA;

    private float fullTossCalcB;

    private float angleBetweenX;

    private float tempVal;

    private float angleToStumpLine;

    private float pitchDiagonalDistance;

    private float deltaXFromPitchOrigin;

    private float deltaZFromPitchOrigin;

    private float creaseLineBaseLength;

    private float creaseAngleToDiagonal;

    private float creaseLineLength;

    private float creaseVerticalOffset;

    private float fullTossTriggerPoint;

    private float swingDisplacement;

    private int fullTossOdds;

    private Vector3 ultraEdgeViewPos;

    private Vector3 ultraEdgeViewRot;

    private Vector3 replaySideCamPos;

    private Vector3 replaySideCamRot;

    [FormerlySerializedAs("canShowDRS")] public bool isDRSEnabled;

    [FormerlySerializedAs("canShowHotspot")] public bool isHotspotEnabled;

    private bool shouldDisplayImpact = true;

    private bool isUmpireDecisionDone;

    private bool wasReplayImpacted = true;

    private bool isImpactDetected;

    [FormerlySerializedAs("impactOffSideWithAttemptedShot")] public bool impactOnOffsideDuringShot;

    private GameObject impactMarkerBall;

    [FormerlySerializedAs("drsCount")] public int remainingDRSChances;

    private bool isPitchingDetected;

    private bool isHittingDetected;

    private bool outViaDRS = true;

    private bool isUmpireCallScenario;

    private bool canAIAssessDRS;

    [FormerlySerializedAs("bDRSPitchingOutsideLeg")] public bool isPitchOutsideLegForDRS;

    [FormerlySerializedAs("drsCalledByBattingTeam")] public int drsByBattingSide = -1;

    [FormerlySerializedAs("drsCalledByUser")] public int drsByUser = -1;

    [FormerlySerializedAs("ball")] public GameObject matchBall;

    private Transform matchBallTransform;

    private GameObject raycastAnchorBallGO;

    private Transform raycastAnchorBallTransform;

    private GameObject ballStartPositionGO;

    private GameObject ballTimingStartGO;

    private Vector3 initialBallPosition;

    private float ballSize = 0.05f;

    private int bounceCount;

    private float launchAngle = 270f;

    private float angleChangeRate;

    private float arcHeight = 2.15f;

    private float horizontalVelocity = 22f;

    [FormerlySerializedAs("ballAngle")] public float _ballAngle;

    private float aiBallAngle;

    private string currentBallStatus = string.Empty;

    private float batContactHeight;

    private float firstBounceDistance;

    private GameObject ballFirstBounce;

    private GameObject ballCatchingPoint;

    private Transform ballCatchingPointTransform;

    private float preCatchDistance;

    [FormerlySerializedAs("pauseTheBall")] public bool isBallPaused;

    private string outcomeOfBall = string.Empty;

    private GameObject sliceEffect;

    private TrailRenderer ballTrailRenderer;

    private Gradient trailColorGradient;

    private Material trailMaterial;

    private bool applyBallFriction;

    [FormerlySerializedAs("currentBallNoOfRuns")] public int runsScoredThisBall;

    private float delayBetweenDeliveries;

    private float _stayStartTime;

    private GameObject creaseImpactSpot;

    private GameObject stumpImpactSpot;

    private float minPickupDistance;

    [FormerlySerializedAs("ballReleased")] public bool isBallReleased;

    private bool isHattrickDelivery;

    private int fieldersStoppedBallCount;

    private List<bool> isFielderNearPitch = new List<bool>();

    private float creaseLineLimit = 8.73f;

    private GameObject batEdgeObject;

    private Transform _batTopEdgeTransform;

    private Transform _batShadowHolderTransform;

    private GameObject rightLegEdgeObject;

    private GameObject leftLegEdgeObject;

    private List<float> _scanForUserFielders = new List<float>();

    private List<float> ScanUserFielderListRefined = new List<float>();

    private Vector3 tempPoint1;

    private Vector3 tempPoint2;

    private bool isRunCancelled;

    private bool triggerRunCancel;

    private int cancelRunDirection = 1;

    private bool shouldMoveUmpire = true;

    private float[,] cancelRunData = new float[10, 10];

    private float[,] saveRunRetryData = new float[10, 10];

    private int saveRunRetryCount;

    private int cancelRunTotalCount;

    private int totalSaveAttempts;

    private int totalCancelAttempts;

    private GameObject batterLeftLegEdgePoint;

    private GameObject batterLeftShoeBackEdge;

    private GameObject batterRightShoeBackEdge;

    private bool keeperCaughtSpecialCatch;

    [FormerlySerializedAs("canShowFCLPowers")] public bool canShowFieldControlPowers;

    private bool isBallPickedByFielder;

    private List<bool> fielderChasePointsSet = new List<bool>();

    private List<GameObject> slipFielders = new List<GameObject>();

    private List<GameObject> aiFielderScanList = new List<GameObject>();

    private bool applyFrictionReduction;

    private float velocityDampingFactor;

    private bool didReachFirstBounce;

    private bool hasTopEdge;

    private bool isOutLBW;

    private GameObject throwTarget;

    private bool fielderHasThrown;

    private bool canKeeperCatchBall;

    /// <summary>
    /// ///////////////////////////////////////////////////
    /// </summary>

    [FormerlySerializedAs("edgeCatch")] public bool isEdgeCaught;

    private bool previousEdgeCatch;

    private bool isAICancelRun;

    private float throwFirstBounceDistance;

    [FormerlySerializedAs("batsman")] public GameObject batsmanObject;

    [FormerlySerializedAs("batsmanAnimationComponent")] public Animation batsmanAnim;

    private Animation bowlerAnim;

    private Animator bowlerAnimator;

    private Animation keeperAnim;

    private Animation mainUmpireAnim;

    private Animation sideUmpireAnim;

    private Animation fielder10Anim;

    [FormerlySerializedAs("Stump1AnimationComponent")] public Animation stump1Anim;

    [FormerlySerializedAs("Stump2AnimationComponent")] public Animation stump2Anim;

    private Animation runnerAnim;

    private Animation strikerAnim;

    private Animation nonStrikerAnim;

    private Animation[] fieldersAnim = new Animation[10];

    [Header("Renders")]
    private Renderer fielder10SkinRenderer;

    [FormerlySerializedAs("BatsmanSkinRendererComponent")] public Renderer batsmanSkinRenderer;

    [FormerlySerializedAs("BatsmanCricketKitSkinRendererComponent")] public Renderer BatsmanCricketKitSkinRenderer;

    [FormerlySerializedAs("BatsmanBatSkinRendererComponent")] public Renderer BatsmanBatSkinRenderer;

    private Renderer RunnerSkinRenderer;

    [FormerlySerializedAs("RunnerCricketKitSkinRendererComponent")] public Renderer RunnerCricketKitSkinRenderer;

    [FormerlySerializedAs("RunnerBatSkinRendererComponent")] public Renderer RunnerBatSkinRenderer;

    [FormerlySerializedAs("MainUmpireSkinRendererComponent")] public Renderer MainUmpireSkinRenderer;

    [FormerlySerializedAs("SideUmpireSkinRendererComponent")] public Renderer SideUmpireSkinRenderer;

    private Renderer BowlerSkinRenderer;

    private Renderer WicketKeeperSkinRenderer;

    //public Renderer WicketKeeperCricketKitSkinRendererComponent;

    private Renderer Fielder10BallSkinRenderer;

    private Renderer BallSkinRenderer;

    private Renderer WicketKeeperBallSkin;

    private Renderer BowlerBallSkinRenderer;

    private Renderer DigitalScreenRenderer;

    [FormerlySerializedAs("FielderSkinRendererComponent")][SerializeField] private Renderer[] FielderSkinRenderer = new Renderer[11];

    [FormerlySerializedAs("BatColliderComponent")] public Collider BatCollider;

    [FormerlySerializedAs("batsmanSkin")] public GameObject BatsmanModel;

    private GameObject PrimaryBatCollider;

    private GameObject SecondaryBatCollider;

    private GameObject LeftLowerLeg;

    private GameObject RightLowerLeg;

    private GameObject LeftUpperLeg;

    private GameObject RightUpperLeg;

    private GameObject Stump1Collider;

    private GameObject Stump2Collider;

    private GameObject BoundaryBoardCollider;

    private GameObject RightHandedBatsmanMaxLimit;

    private GameObject RightHandedBatsmanMinLimit;

    private GameObject LeftHandedBatsmanMaxLimit;

    private GameObject LeftHandedBatsmanMinLimit;

    private bool hasLBWAppeal;

    private bool isLBW;

    private float batsmanInitialXPosition;

    private bool initialUmpireDecision;

    private Vector3 rightHandedBatsmanInitialPosition;

    private Vector3 leftHandedBatsmanInitialPosition;

    private GameObject leftHandedBatsmanInitialSpot;

    [FormerlySerializedAs("shotPlayed")] public string currentShotPlayed = string.Empty;

    private bool attemptedSquareLegGlance;

    private bool attemptedSquareCutDrive;

    [FormerlySerializedAs("batsmanAnimation")] public string currentBatsmanAnimation = string.Empty;

    private float desiredAnimationSpeed;

    private GameObject shotActivationMinBoundary;

    private GameObject shotActivationMaxBoundary;

    private bool isShotAllowed;

    private bool batsmanTriggeredAttemptedShot;

    private bool batsmanCompletedShot;
    private Coroutine _ballAngleRPCCoroutine;  // tracks the active CallRPCChangeBallAngle coroutine
    private bool _ballAngleRPCSent;             // guard: only start the coroutine once per delivery

    private bool canBatsmanMoveLaterally;

    private bool isBatsmanMovingLaterally;

    private float lateralMovementDuration = 2f;

    private GameObject rightHandedBatsmanBackwardLimit;

    private GameObject rightHandedBatsmanForwardLimit;

    private GameObject leftHandedBatsmanBackwardLimit;

    private GameObject leftHandedBatsmanForwardLimit;

    private GameObject rightHandedBatsmanMaxWideLimit;

    private GameObject rightHandedBatsmanMinWideLimit;

    private GameObject leftHandedBatsmanMaxWideLimit;

    private GameObject leftHandedBatsmanMinWideLimit;

    [FormerlySerializedAs("currentBatsmanHand")] public string batsmanHand = "right";

    private float animationFramesPerSecond = 25f;

    private float animationFrameInterval = 0.04f;

    private float optimalShotTiming;

    private float ballReleaseTime;

    private float batOptimalShotReachTime;

    private float optimalShotActivationTiming;

    private GameObject wicketKeeperObject;

    [FormerlySerializedAs("wicketKeeperSkin")] public GameObject wicketKeeperModel;

    private GameObject wicketKeeperBallObject;

    private Vector3 wicketKeeperFastPositionForRHB;

    private Vector3 wicketKeeperFastPositionForLHB;

    private Vector3 wicketKeeperSpinPositionForRHB;

    private Vector3 wicketKeeperSpinPositionForLHB;

    private GameObject wicketKeeperReferencePoint;

    private GameObject fielder10ReferencePoint;

    private GameObject wicketKeeperStraightStumpingPosition;

    private GameObject wicketKeeperLegSideStumpingPosition;

    private GameObject wicketKeeperOffSideStumpingPosition;

    private float distanceBetweenBallAndCollectingPlayerWhileThrowing;

    private string wicketKeeperDirectionAfterBatting = string.Empty;

    private GameObject bowlerObject;

    [FormerlySerializedAs("bowlerSkin")] public GameObject bowlerModel;

    private GameObject bowlerBallObject;

    private float spinFactor;

    private GameObject bowlingInterfaceHideSpot;

    private bool isBowlingInterfaceHidden;

    private bool canUserBowlerMoveBowlingSpot;

    private bool isUserBowlingSpotSelected;

    private GameObject userBowlingMinimumLimit;

    private GameObject userBowlingMaximumLimit;

    private GameObject userBowlingFullTossTriggerPoint;

    [FormerlySerializedAs("fielder10")] public GameObject fielder10Object;

    private GameObject fielder10Model;

    private GameObject fielder10BallObject;

    private GameObject bowlingSpotObject;

    private GameObject bowlingSpotModel;

    private GameObject bowlingSpotFullTossObject;

    private GameObject temporaryBallStartPoint;

    private Renderer bowlingSpotRenderer;

    private Renderer fullTossBowlingSpotRenderer;

    private GameObject fielder10FastPosition;

    private GameObject fielder10SpinPosition;

    private float fullTossYPosition;

    private float fullTossXPosition;

    private float ballSpotLength;

    private float ballSpotHeight;

    private float ballHeightAtStumps;

    private float bowlerRunSpeed = 5f;

    [FormerlySerializedAs("currentBowlerType")] public string bowlerType = "fast";

    private int bowlerSpinType = 2;

    [FormerlySerializedAs("currentBowlerHand")] public string bowlerHand = "right";

    private float bowlerRunupDuration = 4.84f;

    private bool isBowlerActivationAllowed;

    private string fielderAction = string.Empty;

    private GameObject fielderStraightStumpingPosition;

    private GameObject fielderLegSideStumpingPosition;

    private GameObject fielderOffSideStumpingPosition;

    private string postBattingStumpingFielderDirection;

    private int numberOfFielders = 9;

    [FormerlySerializedAs("fielder")] public GameObject[] fielders = new GameObject[10];

    private Transform[] fielderTransforms = new Transform[10];

    private GameObject[] fielderBallReleasePoints = new GameObject[10];

    private Transform[] fielderBallReleasePointTransforms = new Transform[10];

    private GameObject[] fielderBalls = new GameObject[10];

    private GameObject[] fielderReferences = new GameObject[10];

    [FormerlySerializedAs("fielderSkin")] public GameObject[] fielderModels = new GameObject[11];

    [FormerlySerializedAs("fielderCap")] public Renderer[] fielderCaps = new Renderer[11];

    private Vector3[] fielderInitialPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction1FielderPosition")] public Vector3[] restriction1FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction2FielderPosition")] public Vector3[] restriction2FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction3FielderPosition")] public Vector3[] restriction3FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction4FielderPosition")] public Vector3[] restriction4FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction5FielderPosition")] public Vector3[] restriction5FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction6FielderPosition")] public Vector3[] restriction6FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction7FielderPosition")] public Vector3[] restriction7FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction8FielderPosition")] public Vector3[] restriction8FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction9FielderPosition")] public Vector3[] restriction9FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction10FielderPosition")] public Vector3[] restriction10FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction11FielderPosition")] public Vector3[] restriction11FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction12FielderPosition")] public Vector3[] restriction12FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction13FielderPosition")] public Vector3[] restriction13FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction14FielderPosition")] public Vector3[] restriction14FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction15FielderPosition")] public Vector3[] restriction15FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction16FielderPosition")] public Vector3[] restriction16FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction17FielderPosition")] public Vector3[] restriction17FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction18FielderPosition")] public Vector3[] restriction18FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction19FielderPosition")] public Vector3[] restriction19FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction20FielderPosition")] public Vector3[] restriction20FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction21FielderPosition")] public Vector3[] restriction21FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction22FielderPosition")] public Vector3[] restriction22FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction23FielderPosition")] public Vector3[] restriction23FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction24FielderPosition")] public Vector3[] restriction24FielderPositions = new Vector3[10];

    [FormerlySerializedAs("fieldRestriction25FielderPosition")] public Vector3[] restriction25FielderPositions = new Vector3[10];

    private float[] fielderAngles = new float[10];

    private float[] fielderDistances = new float[10];

    private float[] fielderAngleDifferencesToBall = new float[10];

    private GameObject[] fielderChasePoints = new GameObject[10];

    private bool shouldStopFielders;

    private float fielderThrowTimeElapsed;

    private GameObject spinSlipSpotForRHB;

    private GameObject secondSpinSlipSpotForRHB;

    [FormerlySerializedAs("delayMakeFieldersAppeal")] public float fielderAppealDelay;

    [FormerlySerializedAs("runner")] public GameObject currentRunner;

    [FormerlySerializedAs("runnerSkin")] public GameObject runnerAppearance;

    private GameObject rhbNonStrikerRunSpot;

    private GameObject rhbStrikerRunSpot;

    private GameObject strikerRunSpotForRunner;

    private GameObject nonStrikerRunSpotForRunner;

    private GameObject nonStrikerCreaseSpot;

    private GameObject strikerCreaseSpot;

    private float batsmanRunAngle;

    private float runnerRunAngle;

    private Vector3 runnerInitialPosition;

    private GameObject nonStrikerTargetSpot;

    private GameObject strikerTargetSpot;

    [FormerlySerializedAs("mainUmpire")] public Transform mainUmpireTransform;

    [FormerlySerializedAs("sideUmpire")] public GameObject sideUmpireObject;

    [FormerlySerializedAs("mainUmpireSkin")] public GameObject mainUmpireAppearance;

    [FormerlySerializedAs("sideUmpireSkin")] public GameObject sideUmpireAppearance;

    private Vector3 mainUmpireInitialPosition;

    private GameObject umpireLeftSpot;

    private GameObject umpireRightSpot;

    [FormerlySerializedAs("action")] public int currentActionState = -10;

    private GameObject stumpLeft;

    private GameObject stumpRight;

    private GameObject stumpLeftCrease;

    private GameObject stumpRightCrease;

    private GameObject stumpLeftSpot;

    private GameObject stumpRightSpot;

    private float outOfPitchZPosition = 10.5f;

    private float playingAreaRadius = 68.5f;

    private float outerBoundaryRadius = 70f;

    [FormerlySerializedAs("ballOnboundaryLine")] public bool isBallOnBoundaryLine;

    [FormerlySerializedAs("speedThresholdDuringBoundary")] public float boundarySpeedThreshold = 0.5f;

    private int closestFielderIndex = -1;

    private float fenceHeight;

    private string boundaryAnimationName = string.Empty;

    private GameObject uiGameObject;

    private float powerMultiplier;

    [FormerlySerializedAs("controlFactor")] public float controlMultiplier;

    private float agilityMultiplier;

    [FormerlySerializedAs("sideUmpireCameraSpot")]
    [SerializeField]
    private GameObject sideUmpireCameraPosition;

    [FormerlySerializedAs("mainCamera")] public Camera gameplayCamera;

    private Vector3 gameplayCameraZoomOutPosition = new Vector3(0f, 6.8f, -60f);

    private Vector3 fastBowlerCameraZoomOutPosition = new Vector3(0f, 6.8f, -68f);

    private Vector3 gameplayCameraZoomInPosition = new Vector3(0f, 6.8f, -45f);

    private Vector3 gameplayCameraInitialRotation = new Vector3(7.5f, 0f, 0f);

    [FormerlySerializedAs("groundCenterPoint")] public GameObject groundCenterMarker;

    private Transform groundCenterMarkerTransform;

    private GameObject groundGameObject;

    [FormerlySerializedAs("digitalScreen")] public GameObject scoreboardScreen;

    private Vector3 scoreboardScreenScale;

    private GameObject objectsToHide;

    private bool isUpArrowKeyPressed;

    private bool isDownArrowKeyPressed;

    private bool isLeftArrowKeyPressed;

    private bool isRightArrowKeyPressed;

    private bool isPowerKeyPressed;

    private bool isPowerShotActive;

    private bool isRunning;

    private bool canRun;

    [FormerlySerializedAs("rightSideCamera")] public Camera rightFieldCamera;

    [FormerlySerializedAs("leftSideCamera")] public Camera leftFieldCamera;

    [FormerlySerializedAs("previewCamera")] public Camera cameraPreview;

    [FormerlySerializedAs("umpireCamera")] public Camera umpireViewCamera;

    [FormerlySerializedAs("introCamera")] public Camera introCutsceneCamera;

    [FormerlySerializedAs("ultraMotionCamera")] public Camera slowMotionCamera;

    [FormerlySerializedAs("closeUpCamera")] public Camera closeUpViewCamera;

    [FormerlySerializedAs("replayCamera")] public Camera replayViewCamera;

    private Camera uiCamera;

    [FormerlySerializedAs("rightSideBoundaryCamera")] public Camera rightBoundaryCamera;

    [FormerlySerializedAs("leftSideBoundaryCamera")] public Camera leftBoundaryCamera;

    private GameObject introCameraAnchor;

    private bool isSideCameraSelected;

    private ReplaySmoothFollow replayCameraController;

    private float replayCameraBoundaryRotationAngle;

    private float bowlerZoomCameraStartTime;

    [FormerlySerializedAs("limitReplayCameraHeight")] public bool restrictReplayCameraHeight;

    [FormerlySerializedAs("veteranCamera")] public Camera veteranViewCamera;

    [FormerlySerializedAs("zombieCamera")] public Camera zombieViewCamera;

    [FormerlySerializedAs("fireCamera")] public Camera fireEffectCamera;

    [FormerlySerializedAs("googlyCamera")] public Camera googlyEffectCamera;

    private GameObject fireCameraAnchor;

    private GameObject umpireCameraAnchor;

    [FormerlySerializedAs("showShadows")] public bool enableShadows = true;

    private GameObject[] fielderShadowReferences = new GameObject[9];

    private List<GameObject> shadowObjects = new List<GameObject>();

    private List<Transform> shadowTransforms = new List<Transform>();

    private List<GameObject> shadowReferences = new List<GameObject>();

    private List<Transform> shadowReferenceTransforms = new List<Transform>();

    private GameObject bowlerShadowReference;

    private Transform bowlerShadowTransform;

    private Transform bowlerHipBoneTransform;

    private Transform batsmanReferencePoint;

    private bool isBatsmanConfident;

    private bool isBallInline;

    private bool isMouseDown;

    private bool isBowlerWaiting;

    public string bowlerSide = "left";   // online MP default: bowler always starts LEFT so a missed/raced side-sync at game start can't leave the two clients on opposite ends (offline NewInnings still random-picks)

    private float swingAnglePerSecond;

    private float swingAngle;

    private float swingIntensity;

    private bool isBallSwinging;

    [FormerlySerializedAs("slipShot")] public bool isSlipShot;

    private bool isBallToFineLeg;

    private int umpireMainIndex;

    private int umpireSideIndex;

    private bool isFielderAppealingRunOut;

    [FormerlySerializedAs("replayMode")] public bool isReplayModeActive;

    private float savedBallAngle;

    private float savedHorizontalBallSpeed;

    private float savedBallLaunchAngle;

    private float savedBallLaunchHeight;

    private float savedBallFirstBounceDistance;

    private float savedBallLaunchAnglePerSecond;

    private bool isSlipShotSaved;

    private bool isBallHitToFineLegSaved;

    private string savedPlayedShot;

    private float savedShotExecutionZPosition;

    private Vector3 savedBatsmanShotExecutionPosition;

    private float bowlerRunUpStartTime;

    private bool isLeftArrowPressedSaved;

    private bool isRightArrowPressedSaved;

    private List<float> leftArrowKeyDownTimes = new List<float>();

    private List<float> leftArrowKeyUpTimes = new List<float>();

    private List<float> rightArrowKeyDownTimes = new List<float>();

    private List<float> rightArrowKeyUpTimes = new List<float>();

    private float[] slipFieldersExtraActions = new float[5];

    private List<bool> slipFieldersWarmUpStatus = new List<bool>();

    private float[] slipFieldersWarmUpAnimationSpeeds = new float[5];

    private bool isLbwSaved;

    // MP DRS-board fix (#2): expose the authoritative LBW out/not-out so DRS.YesBtnClicked can carry it in
    // CmdDRS_Decision. isLbwSaved is the resolved umpire decision on both clients before the review panel
    // shows. Do NOT use (outViaDRS==isLbwSaved) — outViaDRS is stale-true online. true => OUT, false => NOT OUT.
    public bool IsLbwSavedDecision => isLbwSaved;
    private Coroutine _drsBoardOnlyRoutine;

    private bool isLbwAppealSaved;

    private List<float> takeRunTimings = new List<float>();

    private float ballConnectionTiming;

    private bool isPowerShotActiveSaved;

    private float throwingFirstBounceDistanceSaved;

    private float throwLengthSaved;

    private string summarySaved = string.Empty;

    private GameObject replayController;

    private Transform replayControllerTransform;

    private string replayActionStatus = string.Empty;

    private float ballSpinSpeedX;

    private float ballSpinSpeedZ;

    private float ballSpinSpeedXSaved;

    private float ballSpinSpeedZSaved;

    private float firstBounceBallSpinSpeedZSaved;

    private float ballConnectedSpinSpeedZSaved;

    private bool isRunOutAppealSaved;

    private int pickedUpFielderIndexSaved = -1;

    public static bool isSOMatchStarted;

    public static bool isQPMatchStarted;

    private bool isAIHittingInGap;

    private bool isRunOutSaved;

    private int currentBallRunsSaved;

    private float ballRayCastConnectionZPositionSaved;

    private Collider padColliderSaved;

    private float ballAngleAfterHittingPadsSaved;

    private Vector3 ballConnectionPositionSaved;

    private string throwActionSaved = string.Empty;

    private int celebrationAnimationIndexSaved;

    private string throwTargetSaved;

    // The batting authority's ruling on which end the gathering fielder throws to. Empty until it
    // arrives (or on the authority itself, which never reads it). Cleared per delivery alongside
    // throwTargetSaved. See OnThrowTargetRelayed.
    private string _relayedThrowTarget = string.Empty;


    private string stumpAnimationToPlaySaved;

    private string pickupAnimationToPlaySaved = string.Empty;

    private bool isRunForRunOutFailedPostReplay;

    private GameObject fielderFocusObjectToCollectBall;

    private int boundaryType = 6;

    private string[] teamNames = new string[14];

    private GameObject cameraPivot;

    private int strikerRunOutIndex = -1;

    private int nonStrikerRunOutIndex = -1;

    private int ashCounter = -7;

    private bool isAshCounterActive;

    private Shader diffuseShader;

    private Shader transparentDiffuseShader;

    private Shader transparentSoftEdgeShader;

    private Shader spriteCutoutShader;

    private Shader spriteVertexColoredShader;

    private GameObject outfield;

    private GameObject pitchAndLogo;

    private GameObject crowd;

    private GameObject stadium;

    private GameObject groundShade;

    private GameObject fourLineDiffuse;

    private GameObject fourLineDiffuseInstance;

    private GameObject stump1;

    private GameObject stump2;

    private GameObject stump3;

    private GameObject stump4;

    private GameObject stump5;

    private float batsmanStepSize = 0.3f;

    private Vector2 previousMousePosition;

    private bool isWicketKeeperAtStump;

    [FormerlySerializedAs("CanShowCountDown")] public bool canShowCountdown;

    private float ballHitTime;

    private int difference;

    private bool stopKeeper;

    private GameObject shadowContainer;

    private bool hasBeenBowled;

    private string traceString = string.Empty;

    private bool isRayCastEnabled = true;

    private Collider ballCollider;

    private bool canShowPartnership = true;

    private float initialRotationSpeed = 360f;

    private float currentRotationSpeed = 360f;

    private float zoomSensitivity = 1.5f;

    private float minimumZoom = 0.5f;

    private float maximumZoom = 1f;

    private string currentAnimationStatus = "idle";

    private float freezeStartTime;

    private float freezeDuration = 0.5f;

    private float freezeScale;

    [FormerlySerializedAs("tutorialArrow")] public GameObject tutorialIndicatorArrow;

    private string currentBattingTeam;

    private string currentBowlingTeam;

    private bool isNewInning = true;

    private string previousShader;

    private string stumpPreviousShader;

    private bool isEarthBurnt;

    private bool hasPlayedOnce;

    private bool isStumpBlown;

    private float wkAdjacentLength;

    private float wkHypotenuse;

    private float wkOppositeLength;

    private float wkThetaBetweenAdjAndHyp;

    private bool isWicketKeeperActive;

    private string currentWicketKeeperStatus = string.Empty;

    private bool isWicketKeeperCatchingAnimationSelected;

    private float wicketKeeperCatchingFrameTime;

    private string wicketKeeperCurrentAnimationClip;

    private float wicketKeeperMaxCatchingDistance = 1.75f;

    private bool isWideBallChecked;

    private bool isBallWide;

    private float currentShotExecutionTime;

    private float shotCompletionTime = 0.5f;

    private bool isComputerBatsmanAttemptingNewRun;

    private bool isPsychOutEffectActive;

    private int psychOutEffectCounter;

    private float topDownViewActivationTime;

    private float topDownViewZoomDuration = 0.8f;

    private bool isCameraFocusedOnKeeper;

    private bool isBallOverTheFence;

    private float fenceBoundryHeight = 1.5f;

    private string currentBoundaryAction = string.Empty;

    private bool isBallReflectedFromBoundary;

    private bool isRunOutOccurring;

    private bool isRunBeingTaken;

    private bool hasRunOutOccurred;

    // Set while the bowling side is holding the umpire's gesture, waiting for the batting side's verdict.
    private bool _runOutSignalDeferred;
    private Coroutine _runOutSignalTimeoutCo;
    // How long the held signal waits before falling back to this client's own call.
    private const float RUNOUT_VERDICT_WAIT = 1.2f;

    // -1 = no umpire run-out signal played this delivery, 0 = NOT OUT played, 1 = OUT played.
    private int _runOutSignalPlayed = -1;
    // The batting authority's verdict for this delivery (-1 = not received).
    private int _relayedRunOutVerdict = -1;

    private int umpireRunSignalDirection;

    private float strikerRunningSpeed;

    private float nonStrikerRunningSpeed;

    private string strikerCurrentStatus;

    private string nonStrikerCurrentStatus;

    private GameObject strikerPlayer;

    private GameObject nonStrikerPlayer;

    private float strikerRunAngle;

    private float nonStrikerRunAngle;

    private float introRotationVelocity = 5f;

    private float ballBowlingSpeed;
    //
    private bool isTouchDeviceShotInputEnabled;

    private float defaultFielderSpeed = 7f;

    private List<int> activeFielders = new List<int>();

    private List<string> activeFieldersActions = new List<string>();

    private float batsmanWaitDuration = 0.2f;

    private float bowlerWaitDuration = 0.5f;

    private float ballStartTime;

    private float pausedTimeScale;

    private bool isFreeHitActive;

    private bool isJokerFreeHitActive;

    private bool didBallHitBat;

    // Batting authority's relayed 4-vs-6 verdict for the CURRENT delivery (-1 = none received).
    // Post-contact flight is per-client physics, so a marginal ball can bounce just inside the rope on
    // one peer and clear it on the other (29-07 "aik side 4 aik pa 6"): the authority's verdict wins.
    private int _relayedBoundaryRuns = -1;
    private bool _relayedBoundaryHitBat;

    private bool isOversteppedDelivery;

    private bool isNoBall;

    private string noBallRunStatus = string.Empty;

    private bool isSlowMotionActiveForNoBall;

    [FormerlySerializedAs("lineFreeHit")] public bool isLineFreeHitActive;

    private int lineNoBallRunsScored;

    private float noBallBowlerHeelPosition;

    [FormerlySerializedAs("lastBowledBall")] public string lastBowledBallType = "lineball";

    private float noBallActionDelay;

    private GameObject targetFielderForCatch;

    // Catch-decision sync (online): the BATTING authority's index of the fielder that catches a lofted
    // shot. Applied on the BOWLING follower so its fielders don't independently/divergently decide
    // catch-vs-chase (one screen catches, the other chases to the boundary). -1 = no synced catcher.
    private int _authoritativeCatchFielder = -1;
    // True once the batting authority has relayed a CONFIRMED clean outfield catch this delivery (CmdConfirmOutfieldCatch).
    // Guards the send to once-per-delivery on the authority side. Reset per delivery in ResetAll.
    private bool _sentConfirmCatch = false;
    // BOWLING follower: set by OnConfirmedOutfieldCatch when the authority confirms a clean outfield catch. While
    // true, the fielding loop polls each frame and forces the local ball to resolve as a CATCH the instant it would
    // diverge into a ground-field/throw-back (the follower is ~1 RTT behind, so at relay time the ball usually
    // hasn't reached the fielder yet — the poll catches the divergence the moment it appears). Reset in ResetAll.
    private bool _forceCatchConfirmed = false;
    // The batting authority's exact catch point (ballCatchingPoint), relayed alongside the catcher index so the
    // bowling follower's catcher runs to the SAME spot. ballCatchingPoint is locally derived from a per-client
    // UnityEngine.Random firstBounceDistance, so the bowling-local point diverged (catcher sprinted the wrong
    // way — toward the pitch when the random was small). When set, FixBallCatchingSpot is a no-op on the
    // follower so the authoritative point can't be overwritten. Reset per delivery in ResetAll.
    private bool _hasAuthoritativeCatchPoint = false;
    // Full authoritative fielding setup adopted from the batting authority (active fielder indices + actions +
    // each chase point). The chase RADIUS (num9 in SetActiveFielders) is computed from per-client random
    // firstBounceDistance/horizontalVelocity, so the follower's chaser landed at the wrong distance along the
    // right angle ("idhar udhar nikal jata"), esp. on the RPC_SyncBallShot adopt path (no authoritative
    // distance there). When set, SetActiveFielders is a no-op on the follower so the relayed setup is the
    // single source of truth. Reset per delivery in ResetAll (no sticky leak).
    private bool _hasAuthoritativeFielderSetup = false;

    private GameObject batsmanCelebrationObject;

    [FormerlySerializedAs("previewPanel")] public GameObject ballPreviewPanel;

    private bool isPlayerStumped;

    private bool wasPlayerStumped;

    private bool previousStumpedState;

    private bool isWideWithStumpingSignalDisplayed;

    [FormerlySerializedAs("isnextball")] public bool isNextBallInPlay;

    private bool isEnhancedModeActive;

    private bool isTightRunoutCall;

    private bool isVeryTightRunoutCall;

    private int stumpingAnimationType;

    private string previousKeeperAnimationClip;

    private float previousKeeperCatchingFrame;

    private bool wasKeeperCatchDifferent;

    private float batsmanReturnToCreaseSpeed;

    private bool isThirdUmpireRunoutReplaySkipped;

    private int returnToCreaseAnimationId;

    private int bowlerAnimationNumber = 1;

    private Animator animator;

    private GameObject halfBatsmanObject;

    [Header("Ball texture")]
    public Texture2D ballTextureRed;

    public Texture2D ballTextureWhite;

    public Renderer ballTextureRenderer;

    [FormerlySerializedAs("dummyBallTextureRenderer")] public Renderer[] dummyBallTextureRenderers;

    [FormerlySerializedAs("bowlerBallTexture")] public Renderer bowlerBallTextureRenderer;

    [FormerlySerializedAs("digitalBoardContent")] public Texture2D[] digitalBoardTextures = new Texture2D[4];

    private Color32[] teamUniformColors = new Color32[16];

    [FormerlySerializedAs("teamUniformMaterial")] public Texture2D[] teamUniformMaterials;

    [FormerlySerializedAs("teamKitMaterial")] public Texture2D[] teamKitMaterials;

    [FormerlySerializedAs("teamCapMaterial")] public Texture2D[] teamCapMaterials;

    private Vector3[] teamUniformGreyScales = new Vector3[16];

    private Color32[] teamSkinColors = new Color32[16];

    private Color32[] teamStripColors = new Color32[16];

    [FormerlySerializedAs("umpireTexture")] public Texture2D[] umpireTextures = new Texture2D[3];

    [FormerlySerializedAs("umpireMaterial")] public Material[] umpireMaterials = new Material[2];

    private float distanceToNextPitch;

    [FormerlySerializedAs("resumeGO")] public GameObject resumeGameObject;

    [FormerlySerializedAs("three")] public Text countdownThreeText;

    [FormerlySerializedAs("two")] public Text countdownTwoText;

    [FormerlySerializedAs("one")] public Text countdownOneText;

    private Vector3 brightJerseyGreyScaleValues = new Vector3(0f, 2.9f, -1.08f);

    private Vector3 darkJerseyGreyScaleValues = new Vector3(0.45f, -5.45f, 1.13f);

    [FormerlySerializedAs("shotAngle")] public float shotAngleValue;

    [FormerlySerializedAs("mainCameraOnTopDownView")] public bool isMainCameraInTopDownView;

    private bool isWarmUpDoneOnce;

    private int previousJerseyIndex;

    private int jerseyIndexToChange = 1;

    [FormerlySerializedAs("shotNameText")] public Text shotNameUIText;

    [FormerlySerializedAs("hotspotReference")] public GameObject[] hotspotReferences;

    private int targetScoreToWin;

    [FormerlySerializedAs("noLight")] public Shader noLightShader;

    [FormerlySerializedAs("oneLight")] public Shader oneLightShader;

    [FormerlySerializedAs("lights")] public GameObject[] sceneLights;

    private bool isAutoBowlerActivationAllowed;

    private int batsmanOutIndexValue;

    private bool isBowlerActivatedForReplay;

    [FormerlySerializedAs("edgeRefs")] public Transform edgeReferences;

    [NonSerialized]
    public bool noBall;

    [NonSerialized]
    public bool freeHit;

    private float sixDistanceSaved;

    private bool isHardcoded;

    private float animationValue;

    [FormerlySerializedAs("broadCastCamera")] public GameObject broadcastCamera;

    [FormerlySerializedAs("gamePaused")] public bool isGamePaused;

    [FormerlySerializedAs("fieldRestriction")] public bool isFieldRestrictionActive = true;

    private Vector3 temporaryShadowPosition;

    private float shadowHeightY = -0.1f;

    private int checkCountValue;

    [FormerlySerializedAs("UmpireInitialDecision")] public string umpireInitialDecision = string.Empty;

    // True once the caught-behind umpire decision has been computed (batting) or received (bowling)
    // for the current delivery. Lets the bowling client distinguish "no decision yet" from a real one.
    private bool hasUltraEdgeDecision = false;

    private int umpireDecisionChance = 50;

    private int edgeProbabilityChance = 50;

    private int aiReviewProbability = 50;

    private Vector3[] ballPathPoints = new Vector3[2];

    [FormerlySerializedAs("IrCam")] public GameObject infraredCamera;

    [FormerlySerializedAs("impactImg")] public GameObject impactImage;

    [FormerlySerializedAs("waveImg")] public GameObject waveImage;

    [FormerlySerializedAs("SideCam")] public GameObject sideCamera;

    [FormerlySerializedAs("bat")] public GameObject batObject;

    [FormerlySerializedAs("ultraEdgeImpact")] public GameObject ultraEdgeImpactImage;

    [FormerlySerializedAs("UltraEdgeCam")] public GameObject ultraEdgeCamera;

    private Tweener ballTweener;

    private Tweener waveTweener;

    [FormerlySerializedAs("ShowNotOutAnim")] public bool showNotOutAnimation;

    [FormerlySerializedAs("ballTimeSaved")] public bool isBallTimeSaved;

    private bool isPositionSaved;

    private bool hasWaveTweenPlayed;

    private float ultraEdgeImpactPositionValue;

    [FormerlySerializedAs("customBallMovement")] public bool isCustomBallMovementActive;

    [FormerlySerializedAs("changedBallMovement")] public bool isBallMovementChanged;

    private bool isBallPlaced;

    private bool hasEdgeOccurred;

    private bool isEdgePositionSaved;

    private float safeEdgeDistance = 0.1f;

    private float minEdgeDistance = 0.025f;

    private float ballDeviationThreshold = 0.13f;

    [FormerlySerializedAs("ShadowsToDisable")] public GameObject[] shadowsToDisable;

    private Vector3 savedEdgePositionValue;

    [FormerlySerializedAs("elapsedTime")] public float totalElapsedTime;

    [FormerlySerializedAs("UltraEdgeCutscenePlaying")] public bool isUltraEdgeCutscenePlaying;

    // Caught-behind review-stuck fix (online): a caught-behind OUT is recorded LATE (decisionPending →
    // UpdateCurrentBall, sending RpcBallOutcome). If a review fires first, ForceResolveStuckReview
    // resets the ball and that UpdateCurrentBall is bypassed → the OUT's RpcBallOutcome is never sent →
    // the bowling client waits for it forever (stuck on review, camera jerking, fielders chasing edge).
    // This flag marks "a caught-behind OUT is pending record" so ForceResolveStuckReview can flush it.
    private bool mpCaughtBehindOutPending = false;

    // Online LBW over-hang fix: the LBW dot/wicket is normally recorded at the "waitForLbwResult" gate only
    // AFTER the DRS review resolves, but online the review never resolves cleanly so the outcome is never
    // sent and the over hangs. We commit the authoritative on-field outcome at the appeal mark instead; this
    // flag marks it done so the waitForLbwResult gate can't double-record. Reset per delivery in ResetAll.
    private bool mpLbwOutcomeCommitted = false;
    // True only between an online LBW appeal-mark commit and the next ResetAll. Used by the review resolvers
    // (ForceResolveStuckReview, DRS.NoBtnClicked) to SKIP their currentActionState=3 write: the over already
    // advanced + re-armed the bowler to state -2 via the OppAck handshake, so writing 3 would clobber that -2
    // (only -2 re-arms the auto-bowl) and hang the bowler. The panel/overlay cleanup still runs; only the state
    // write is suppressed when the outcome is already committed.
    public bool MpLbwOutcomeCommitted => mpLbwOutcomeCommitted;

    [FormerlySerializedAs("umpireAnimationPlayed")] public bool hasUmpireAnimationPlayed;

    [FormerlySerializedAs("UserCanAskReview")] public bool canUserAskForReview;

    [FormerlySerializedAs("AiCanAskReview")] public bool canAIAskForReview;

    private Vector3[] defaultHotspotPositions = new Vector3[4];

    private Vector3[] defaultBallPathPoints = new Vector3[4];

    [FormerlySerializedAs("validBall")] public int validBallCount;

    [FormerlySerializedAs("canCountBall")] public int remainingBallCount;

    [FormerlySerializedAs("runsScored")] public int totalRunsScored;

    [FormerlySerializedAs("extraRun")] public int extraRuns;

    [FormerlySerializedAs("batsmanID")] public int currentBatsmanID;

    [FormerlySerializedAs("isWicket")] public int isWicketTaken;

    [FormerlySerializedAs("wicketType")] public int typeOfWicket;

    [FormerlySerializedAs("bowlerID")] public int bowlerId;

    [FormerlySerializedAs("catcherID")] public int catcherId;

    [FormerlySerializedAs("batsmanOut")] public int batsmanOutId;

    [FormerlySerializedAs("isBoundary")] public bool isBoundaryHit;

    [FormerlySerializedAs("animStarted")] public bool isAnimationStarted;

    [FormerlySerializedAs("ballPath1")] public GameObject[] ballPath1Points;

    [FormerlySerializedAs("ballSpeed")] public float ballSpeedValue = 1f;

    [FormerlySerializedAs("travelTime")] public float ballTravelTime = 0.08f;

    private bool isDRSHardcoded;

    private Vector3 savedEdgeTransformValue;

    [FormerlySerializedAs("SnickoMeter")] public GameObject snickoMeterObject;

    private Vector3 snickoTransformPosition;

    [FormerlySerializedAs("referencePath")] public GameObject referencePathObject;

    [FormerlySerializedAs("startingPoint")] public GameObject ballStartingPoint;

    [FormerlySerializedAs("endingPoint")] public GameObject ballEndingPoint;

    private float timeRequiredToTravel;

    private bool isRecordingEnabled = true;

    private bool isShotExecuted;

    private bool isBallPausedAtImpact;

    [FormerlySerializedAs("StartingFrame")] public float startingFramePoint = 0.45f;

    [FormerlySerializedAs("EndingFrame")] public float endingFramePoint = 0.5f;

    [FormerlySerializedAs("midFrame")] public float midFramePoint;

    [FormerlySerializedAs("snickoStatus")] public Image snickoStatusIndicator;

    [FormerlySerializedAs("snickoEdged")] public Sprite snickoEdgedSprite;

    [FormerlySerializedAs("snickoNotEdged")] public Sprite snickoNotEdgedSprite;

    private float battingTimingMeterValue;

    // Issue #2 (too batsman-friendly): timing beyond +/- this on the [-100,100] meter (0 = perfect)
    // turns a bat contact into a play-and-miss. 65 = medium-strict. Lower = harder. TUNABLE.
    private float shotMissTimingThreshold = 65f;

    private float firstBounceSpeedMultiplier = 1f;

    private float horizontalSpeedAdjustment = 1f;

    private bool isPerfectShot;

    private bool isMistimedShot;

    private bool needsBattingTimingMeterNeedleUpdate;

    public static Vector3 temporaryPosition;

    //Multiplayer

    private float multiplayerSecondCounter;

    [FormerlySerializedAs("timerImage")][SerializeField] private Image timerProgressBar;

    [FormerlySerializedAs("timerText")][SerializeField] private Text timerCountdownText;

    [FormerlySerializedAs("Timer")][SerializeField] public GameObject timerObject;

    DG.Tweening.Sequence animationSequence;

    bool isBallHit = false;

    // Bug-2 guard: a delivery must be released exactly ONCE. A re-fired bowler animation
    // event (sender) or a duplicated RPC_SyncBallRelease (receiver, common at high ping)
    // used to re-run the full release → the ball "came out twice" and crossed the pitch
    // twice. This flag is set on the first release and reset at the start of every delivery
    // (BowlNextBall) and in ResetAll, so the first release always works and only duplicates
    // are suppressed. Replays are exempt (they legitimately re-simulate).
    private bool ballReleaseProcessedThisDelivery = false;

    // Orphan-ball discriminator (mid-delivery reconnect fix, 2026-06-22): set TRUE the moment THIS delivery's
    // outcome is committed (GameData.UpdateCurrentBall, past the bowling-follower suppression gate — the single
    // Cmd/RpcBallOutcome commit point), cleared by ResetAll + the BowlNextBall new-delivery reset. Lets
    // CancelInFlightDeliveryForReconnect tell an ORPHAN ball (released, batter disconnected pre-shot, outcome
    // NEVER arrived) apart from a POST-SHOT ball (outcome committed, still mid-animation with isBallReleased not
    // yet cleared) — so the reconnect re-arm re-bowls ONLY the orphan and never double-bowls a counted ball.
    private bool _outcomeCommittedThisDelivery = false;
    // Collect-gate divergence probe (#1). See the report site in ActivateBowler.
    private bool _collectFiredThisDelivery;
    private bool _collectGateReported;











    //Photon Removal  public ConnectAndJoin ConnectAndJoin;
private void OnEnable()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][OnEnable] Subscribing to events.");
       // MirrorNetwork.OnWinCall += AnnounceVictory;
        MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
    }

    private void OnDisable()
    {
        ConstantsData_M.MpLog("[CricketNetworkManager][OnDisable] Unsubscribing from events.");
       // MirrorNetwork.OnWinCall -= AnnounceVictory;
        MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
    }
    public void DirectResult(bool result)
        {
            if (result)
            {
                // Victory(staticVariables.UserProfiledata.user._id, "Opponent has disconnected. You are declared the winner.");
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }
            else
            {
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(false, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }

        }





































































    //public void BallMovement()
    //{
    //	//ConstantsData_M.MpLog("* call");

    //	float x = Mathf.Cos(ballAngle * DEG2RAD) * horizontalSpeed * Time.deltaTime;
    //	float z = Mathf.Sin(ballAngle * DEG2RAD) * horizontalSpeed * Time.deltaTime;
    //	float num = Mathf.Sin(ballProjectileAngle * DEG2RAD) * ballProjectileHeight - ballRadius;
    //	if (float.IsNaN(num))
    //	{
    //		num = 0f;
    //	}



    //	ballTransform.position = new Vector3(ballTransform.position.x, 0f - num, ballTransform.position.z);
    //	ballTransform.position += new Vector3(x, 0f, z);




    //	ballProjectileAngle += ballProjectileAnglePerSecond * Time.deltaTime;
    //	ballRayCastReferenceGOTransform.position = new Vector3(ballTransform.position.x, ballTransform.position.y, ballTransform.position.z);
    //	ballRayCastReferenceGOTransform.eulerAngles = new Vector3(ballRayCastReferenceGOTransform.eulerAngles.x, (90f - ballAngle + 360f) % 360f, ballRayCastReferenceGOTransform.eulerAngles.z);
    //	if (ballOnboundaryLine)
    //	{
    //		ballSpinningSpeedInX = 300 + UnityEngine.Random.Range(0, 61);
    //		ballSpinningSpeedInZ = 300 + UnityEngine.Random.Range(0, 61);
    //	}
    //	ballTransform.Rotate(Vector3.right * Time.deltaTime * ballSpinningSpeedInX, Space.World);
    //	ballTransform.Rotate(Vector3.forward * Time.deltaTime * ballSpinningSpeedInZ, Space.World);
    //	if (replayMode && action == 4 && ballProjectileAngle >= 360f && !ballOnboundaryLine)
    //	{
    //		ballSpinningSpeedInX = UnityEngine.Random.Range(-3600, -1800);
    //		ballSpinningSpeedInZ = UnityEngine.Random.Range(-3600, -1800);
    //	}

    //}




















    IEnumerator LeaveRoomAfterDelay(bool Connected)
    {
        yield return new WaitForSeconds(10f);
        if (Connected)
        {
            AutomaticShowResult();
        }
        else
        {
            //Photon Removal if (!PhotonNetwork.InRoom)
            {
                AutomaticLeaveRoom();
            }
        }
    }



    IEnumerator CallColliderRPCMultipleTimes(string Other, float SavedBallRayCastConnectedZposition)
    {
        int retries = 0;
        while (retries < 25)
        {
            if (GameConstants.isWithAI == false)
            {
                //ConstantsData_M.MpLog("KI=onsaa Collkider : " + Other);
                //Photon Removal view.RPC("RPC_SetMultiplayerCollider", RpcTarget.OthersBuffered, Other, SavedBallRayCastConnectedZposition);
                if (CricketNetworkManager.ReadyToSend) CricketNetworkManager.instance.CmdSetMultiplayerCollider(staticVariables.UserProfiledata.user._id, Other, SavedBallRayCastConnectedZposition);
            }

            yield return new WaitForSeconds(0.08f);
            retries++;
        }
    }

    //Photon Removal  [PunRPC]
    void OnRPCProcessed(int processedData)
    {
    }












    IEnumerator CallRPCWideBall()
    {
        int retries = 0;
        while (retries < maxRetryAttempts)
        {
            if (GameConstants.isWithAI == false)
            {
                //    view.RPC("RPC_WideBall", RpcTarget.OthersBuffered);                                                                         //Photon Removal
                CricketNetworkManager.instance.CmdWideBall(staticVariables.UserProfiledata.user._id);
            }
            yield return new WaitForSeconds(0.05f);
            retries++;
        }
    }














    IEnumerator CallShotPlayedRPC(string SHOTPLAYED, string BATSMANANIM, float BatReachingTimeForOptimalShotLength, float OptimalShotActivationTime, bool PowerShot)
    {
        int retries = 0;
        while (retries < 25)
        {
            if (GameConstants.isWithAI == false)
            {
                //    view.RPC("RPC_ShotPlayed", RpcTarget.OthersBuffered, SHOTPLAYED, BATSMANANIM, BatReachingTimeForOptimalShotLength, OptimalShotActivationTime, PowerShot);
                CricketNetworkManager.instance.CmdShotPlayed(staticVariables.UserProfiledata.user._id, SHOTPLAYED, BATSMANANIM, BatReachingTimeForOptimalShotLength, OptimalShotActivationTime, PowerShot);
                yield break;
            }

            yield return new WaitForSeconds(0.025f);
            retries++;
        }
    }











    IEnumerator CallRPCChangeBallAngle(float BallAngle, float HorizontalSpeed, float BallProjectileAngle, float BallProjectileHeight
        , float BallTimingFirstBounceDistance, float BallProjectileAnglePerSecond, float BallBatMeetingHeight, float HorizontalSpeedMultiplier
        , string Ballstatus, bool EdgeCatch, Vector3 ContactPos)
    {
        int retries = 0;
        // [ShotContact] diag (tester 09-07: "full toss py off-side/straight swing lekin back boundary again
        // again"): the committed contact angle ~86-89° (nearly straight behind) suggests full-toss contacts
        // fall into a glancing/edge-like deflection regardless of the swing. This one line per shot pins the
        // source from a device log: a real swing shows the shot's intended angle; a degenerate contact shows
        // angle≈incoming with the fullToss flag set.
        ConstantsData_M.MpLog($"[ShotContact] angle={BallAngle:F1} status={Ballstatus} edge={EdgeCatch} fullToss={isFullToss} shot={currentShotPlayed} batHeight={BallBatMeetingHeight:F2} contact={ContactPos}");
        while (retries < 3)
        {
            if (GameConstants.isWithAI == false)
            {
                CricketNetworkManager.instance.CmdChangeBallAngle(staticVariables.UserProfiledata.user._id, BallAngle, HorizontalSpeed, BallProjectileAngle, BallProjectileHeight, BallTimingFirstBounceDistance, BallProjectileAnglePerSecond, BallBatMeetingHeight, HorizontalSpeedMultiplier, Ballstatus, EdgeCatch, ContactPos);
                //    //ConstantsData_M.MpLog("ANGLE : " + BallAngle + " HOrizontal Speed : " + HorizontalSpeed);
                //    view.RPC("RPC_ChangeBallAngle", RpcTarget.OthersBuffered, BallAngle, HorizontalSpeed, BallProjectileAngle, BallProjectileHeight
                //  , BallTimingFirstBounceDistance, BallProjectileAnglePerSecond, BallBatMeetingHeight, HorizontalSpeedMultiplier, Ballstatus, EdgeCatch);
            }

            yield return new WaitForSeconds(0.15f);
            retries++;
        }
    }




















    IEnumerator DelayedFunction()
    {
        // Wait for 0.1 seconds
        //yield return new WaitForSeconds(0.4f);
        isBowlingAcknowledged = true;
        //ConstantsData_M.MpLog("BOWLL KIYAA H");
        // ~2s inter-screen delay fix (#3): this fixed bowler wind-up wait was 1.25s — the single
        // largest ping-independent contributor to the gap between the two screens (bowled here ~2s
        // before it appeared on the other). Shortened to 0.4s (kept >0 so the idle->run-up animation
        // blend still reads naturally). CmdSyncBowlerRunUp is still emitted right after, so the batting
        // client's run-up + ball-release stay aligned. (FIX B / ACK-removal was rejected as unsafe.)

        // Lockstep run-up scheduling: relay the shared START TIME first (NetworkTime now + the same 0.4s
        // wind-up), then BOTH clients begin the run-up at that instant — instead of the batting side
        // starting one-way latency late. The 0.4s lead absorbs up to 400ms one-way (800 RTT).
        if (LockstepActive && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && !CONTROLLER.ReplayShowing && CricketNetworkManager.instance != null)
        {
            // SPOT-ADJUST WINDOW (user request): under the locked plan the spot freezes at the mint — give
            // the bowler a few seconds after committing spin/speed to fine-tune the bounce spot first.
            if (ConstantsData_M.useLockedDeliveryPlan && ConstantsData_M.lockstepSpotAdjustSeconds > 0f)
            {
                canUserBowlerMoveBowlingSpot = true;
                // Post-reconnect first ball: the spot-move gate needs BowlingBy=="user" and timeScale>=1 —
                // both can arrive stale from the reload window. In online MP the bowling side is ALWAYS
                // user-driven, and the adjust window is gameplay-live by definition.
                if (BowlingBy != "user") BowlingBy = "user";
                if (Time.timeScale < 1f) Time.timeScale = 1f;
                ConstantsData_M.MpLog($"[Lockstep] Spot-adjust window open: canMove={canUserBowlerMoveBowlingSpot} bowlingBy={BowlingBy} timeScale={Time.timeScale:F2}");
                // Post-reconnect fix (13-07: "bowler can't set the ball marker after reconnect"): the bowling
                // controls panel can come back INACTIVE after the scene reload (same dead-panel class as the
                // batting input + auto-bowl timer) — the spot drag then never registers. No-op when active.
                BowlingControls _bwc = Singleton<BowlingControls>.instance;
                if (_bwc == null) _bwc = UnityEngine.Object.FindFirstObjectByType<BowlingControls>(FindObjectsInactive.Include);
                if (_bwc != null)
                {
                    for (Transform _anc = _bwc.transform; _anc != null; _anc = _anc.parent)
                    {
                        if (!_anc.gameObject.activeSelf) _anc.gameObject.SetActive(true);
                    }
                }
                double spotWindowEnd = Mirror.NetworkTime.time + ConstantsData_M.lockstepSpotAdjustSeconds;
                while (Mirror.NetworkTime.time < spotWindowEnd)
                {
                    yield return null;
                }
            }
            double runUpStartAt = Mirror.NetworkTime.time + 0.4;
            // LOCKED DELIVERY PLAN (user design): the aim is committed — freeze the spot, mint the ENTIRE
            // release (params + seed) NOW and ship it BEFORE the run-up. The batter then launches from its
            // OWN bowler anim event (zero release-time dependency) and THIS side runs its whole delivery
            // presentation lockstepFollowerFlightDelay behind real time as a pure follower.
            if (ConstantsData_M.useLockedDeliveryPlan)
            {
                canUserBowlerMoveBowlingSpot = false;
                FreezeBowlingSpot();
                // The mint can run BEFORE StartBowling's ball placement for this delivery (test log: the
                // packet carried the PREVIOUS ball's resting spot, z≈6.5 → the batter's ball spawned at the
                // bat, tick-0 crossing). Place the ball at the scene-constant release point ourselves —
                // StartBowling does the exact same assignment, so this is idempotent whichever runs first.
                matchBallTransform.position = initialBallPosition;
                temporaryPosition = matchBallTransform.position;
                // Side-aware release point (tester: "bowler side change karo to ball ki direction kharab"):
                // initialBallPosition is the SCENE-INIT capture — one side only. StartBowling always followed
                // its placement with SetBowlerSide(), which moves ballStartPositionGO + the ball to the
                // current end (left x=-0.7 / right mirrored). The mint must do the same or the shipped start
                // position is on the wrong side of the stumps after changing ends.
                SetBowlerSide();
                FindBowlingParameters();
                _lockedPlanMinted = true;
                int planDeliverySeed = UnityEngine.Random.Range(int.MinValue / 2, int.MaxValue / 2);
                DeterministicRng.Seed(planDeliverySeed);
                CricketNetworkManager.instance.CmdSyncBallRelease(
                    staticVariables.UserProfiledata.user._id,
                    matchBallTransform.position,
                    temporaryPosition,
                    _ballAngle,
                    launchAngle,
                    arcHeight,
                    angleChangeRate,
                    horizontalVelocity,
                    spinFactor,
                    swingIntensity,
                    isFullToss,
                    -1d,               // plan sentinel: follower launches at its OWN bowler anim event
                    planDeliverySeed,
                    ballSpotLength,    // batter's tables/shot-selection need the real pitch length (was 0 → dribble fbd)
                    isOversteppedDelivery,
                    CONTROLLER.fielderChangeIndex,
                    isFieldRestrictionActive,    // field placement re-assert — heals one-sided field changes/resets every ball
                    CONTROLLER.CurrentBowlerIndex);   // bowler identity re-assert
                ConstantsData_M.MpLog($"[Lockstep] Delivery PLAN minted+shipped pre-run-up: angle={_ballAngle:F1} hVel={horizontalVelocity:F1} spin={spinFactor:F2} swing={swingIntensity:F2} spotLen={ballSpotLength:F1} seed={planDeliverySeed}");
            }
            CricketNetworkManager.instance.CmdSyncBowlerRunUp(
                staticVariables.UserProfiledata.user._id, bowlerAnimationNumber, runUpStartAt);
            // Plan-mode: THIS side's run-up (and hence its anim-event flight anchor) runs the configured
            // delay behind the batter's — the whole bowling presentation follows, internally seamless.
            double myRunUpStartAt = runUpStartAt
                + (ConstantsData_M.useLockedDeliveryPlan ? ConstantsData_M.lockstepFollowerFlightDelay : 0f);
            while (Mirror.NetworkTime.time < myRunUpStartAt)
            {
                yield return null;
            }
            bowlerAnimator.Play("Blitz_" + bowlerType + "PaceBowling0" + bowlerAnimationNumber);
            isBowlingAcknowledged = false;
            ConstantsData_M.MpLog($"[Lockstep] Bowler run-up started at netTime={myRunUpStartAt:F3} (shared={runUpStartAt:F3})");
            yield break;
        }

        yield return new WaitForSeconds(0.4f);

        ////ConstantsData_M.MpLog("BOWLL KIYAA H");

        bowlerAnimator.Play("Blitz_" + bowlerType + "PaceBowling0" + bowlerAnimationNumber);
        //BowlerAnimatorComponent.SetFloat("SpeedMultiplier", 1f);
        isBowlingAcknowledged = false;

        // RUN-UP SYNC FIX: this is the moment the BOWLING client's run-up actually starts.
        // Tell the batting client to start its run-up NOW (it no longer plays it early in
        // BowlerWaiting). Both this message and the later ball-release RPC travel one-way
        // latency from here, so the batting client's throw frame stays aligned with the ball.
        if (GameConstants.isWithAI == false && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && !CONTROLLER.ReplayShowing
            && CricketNetworkManager.instance != null)
        {
            CricketNetworkManager.instance.CmdSyncBowlerRunUp(
                staticVariables.UserProfiledata.user._id, bowlerAnimationNumber, 0d);
        }
    }







    IEnumerator CallRpcAckBowl()
    {
        // Double-release-animation fix (#5): Mirror Commands are reliably delivered, so the legacy
        // 3× retry (a Photon OthersBuffered-era hack) is redundant — and HARMFUL: a 2nd/3rd ack that
        // lands after DelayedFunction's 1.25s acknowledged-window re-triggers the run-up + ball-
        // release animation, so the ball is thrown twice. Send exactly ONE ack.
        if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null)
        {
            //Photon Removal  view.RPC("AckForBowling", RpcTarget.OthersBuffered);
            CricketNetworkManager.instance.CmdAckForBowling(staticVariables.UserProfiledata.user._id);
        }
        yield break;
    }




















    private Coroutine _mpReviewWatchdog;



    // Bowling-side outcome backstop (B): the bowling client suppresses local physics and waits for the
    // batting authority's RpcBallOutcome each ball. If that outcome never arrives (e.g. a reviewed
    // caught-behind whose send was missed/dropped), the client stays stuck in the post-shot view
    // forever (camera following the dead ball + fielders chasing the edge). Started from
    // GameData.UpdateCurrentBall's bowling-suppress path; after a bounded wait it force-resolves ONLY
    // if the ball is still unresolved. Self-cancels the instant the outcome lands (ball count advances
    // or isBallHit clears), so normal play is untouched. With fix A this rarely fires (A sends the
    // outcome) — it is the safety net for a genuinely lost outcome packet.
    private Coroutine _mpOutcomeWatchdog;
    private bool _strikerZPosNullLogged;   // one-shot for the strikerZPos catch guard log
































    //Photon Removal  [PunRPC]
    // Deadlock safety net for the opponent-ACK barrier. The barrier needs both players'
    // ACKs (OnWaitScreen == 2) before the next ball is bowled. At high ping / packet loss the
    // 2nd ACK can be dropped, leaving OnWaitScreen stuck at 1 forever → the pitch never advances
    // (the reported "stuck on the pitch most of the time"). Arming a watchdog when the first ACK
    // arrives lets us force the advance after a timeout, so a single lost ACK can never freeze
    // the match permanently. The normal (both ACKs received) path is unchanged.
    private bool ackBarrierResolved = true;
    private Coroutine ackWatchdog;
    // G1 v2 staying-client delivery self-heal watchdog. NOT a SyncVar; pure local timer state. Ships DARK
    // (ConstantsData_M.useWatchdogG2 default false). Re-arms a stalled pre-contact in-flight ball after 10s
    // when the opponent is still PRESENT (a genuine stream silence) — every disconnect / slow-mo / review /
    // over-ACK / opponent-gone case is held. See TickDeliveryWatchdog for the full guard predicate.
    private float _deliveryWatchTimer = 0f;       // unscaled seconds a single live-window has stayed continuously TRUE
    private float _deliveryWatchScaledMark = 0f;  // last Time.time sample, to require the SCALED clock advanced (anti slow-mo/pause)
    private float _deliveryWatchProgressMark = 0f; // last unscaled time a delivery-stream packet stamped progress (liveness)





































































































































    IEnumerator CallRPCMultiple(bool CanLoadGround)
    {
        int retries = 0;

        while (retries < maxRetryAttempts)
        {
            if (GameConstants.isWithAI == false)
            {
                //    view.RPC("RPC_UpdateCanLoadGround", RpcTarget.OthersBuffered, CanLoadGround);                                                         //Photon Removal
                // Ghost-session guard. The 30-07 ritu_mp logs show THIS send throwing inside Mirror's
                // SendCommandInternal on a reloaded scene, which aborts the rest of the caller — and for
                // CmdSetBowler that means the bowler is never assigned or relayed, i.e. the two sides end up
                // with different bowlers. Only the sends actually seen throwing are guarded.
                if (CricketNetworkManager.ReadyToSend)
                    CricketNetworkManager.instance.CmdUpdateCanLoadGround(staticVariables.UserProfiledata.user._id, CanLoadGround);
                else
                    ConstantsData_M.MpLog("[GhostSend] UpdateCanLoadGround BLOCKED — network manager not ready (ghost session); caller continues instead of throwing.");
            }
            if (CanLoadGround == false)
            {
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                yield return new WaitForSeconds(retryDelayDuration);
            }
            retries++;
        }

    }











    public Vector3 GetTempPos()
    {
        return temporaryPosition;
    }

    public void enhancedMode()
    {
        agilityMultiplier = (float)ObscuredPrefs.GetInt("agilityGrade") * 0.02f;
        controlMultiplier = (float)ObscuredPrefs.GetInt("controlGrade") * 0.02f;
        powerMultiplier = (float)ObscuredPrefs.GetInt("powerGrade") * 0.02f;
        isEnhancedModeActive = true;
    }

    private void InitializeEnvironment()
    {
        if (CONTROLLER.PlayModeSelected == 2 || CONTROLLER.PlayModeSelected == 7)
        {
        }
        else
        {
            sceneLights[0].SetActive(value: false);
            sceneLights[1].SetActive(value: false);
        }
    }

    public void SaveJerseyColor()
    {
        batsmanSkinRenderer.materials[0].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BattingTeamIndex]); //SetColor("_Color", teamUniformColor[jerseyIndexToChange]);
        batsmanSkinRenderer.materials[1].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BattingTeamIndex]);
        BatsmanCricketKitSkinRenderer.materials[0].SetTexture("_MainTex", teamKitMaterials[CONTROLLER.BattingTeamIndex]);

    }

    public void RevertJerseyColor()
    {
        batsmanSkinRenderer.materials[0].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BattingTeamIndex]);
        batsmanSkinRenderer.materials[1].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BattingTeamIndex]);
        BatsmanCricketKitSkinRenderer.materials[0].SetTexture("_MainTex", teamKitMaterials[CONTROLLER.BattingTeamIndex]);
    }

    public void Awake()
    {
        // Reconnect/scene-reload tween cleanup: Menu/Toss/camera-helper tweens are created
        // persistent (SetLoops(-1) / SetAutoKill(false)) but their owner GameObjects are plain
        // scene objects destroyed on the Ground (re)load. DOTween's driver is DontDestroyOnLoad,
        // so those orphaned tweens keep ticking against destroyed targets and threw one
        // NullReferenceException PER TWEEN PER FRAME — ~1832 NullRefs + ~1700 "Target is null"
        // DOTween errors during reconnect. The project has no tween teardown anywhere; sweep them
        // here, before any Ground gameplay tween is created (those are made later, during play).
        DG.Tweening.DOTween.KillAll(false);

     StadiumObject.SetActive(true);

        isBowlingAcknowledged = false;

        if (CONTROLLER.PlayModeSelected < 4)
        {
            isQPMatchStarted = true;
        }
        CONTROLLER.pageName = "Ground";
        CONTROLLER.fromPreloader = false;
        Screen.sleepTimeout = -1;
        PlayerPrefs.SetInt("EncrytionSuccess", 1);
        bowlerAnimationNumber = 1;
        teamNames[0] = "Australia";
        teamNames[1] = "Bangladesh";
        teamNames[2] = "Canada";
        teamNames[3] = "England";
        teamNames[4] = "India";
        teamNames[5] = "Ireland";
        teamNames[6] = "Kenya";
        teamNames[7] = "Netherlands";
        teamNames[8] = "NewZealand";
        teamNames[9] = "Pakistan";
        teamNames[10] = "SouthAfrica";
        teamNames[11] = "SriLanka";
        teamNames[12] = "WestIndies";
        teamNames[13] = "Zimbabwe";
        Renderer[] array = dummyBallTextureRenderers;
        foreach (Renderer renderer in array)
        {
            renderer.material.mainTexture = ballTextureWhite;
        }

        bowlerBallTextureRenderer.material.mainTexture = ballTextureWhite;
        ballTextureRenderer.material.mainTexture = ballTextureWhite;
        if (CONTROLLER.PlayModeSelected != 2 && CONTROLLER.PlayModeSelected != 7)
        {
            ref Color32 reference = ref teamUniformColors[0];
            reference = new Color32(12, 59, 166, byte.MaxValue);
            ref Color32 reference2 = ref teamUniformColors[1];
            reference2 = new Color32(188, 163, 1, byte.MaxValue);
            ref Color32 reference3 = ref teamUniformColors[2];
            reference3 = new Color32(38, 179, 118, byte.MaxValue);
            ref Color32 reference4 = ref teamUniformColors[3];
            reference4 = new Color32(51, 87, 162, byte.MaxValue);
            ref Color32 reference5 = ref teamUniformColors[4];
            reference5 = new Color32(34, 118, 185, byte.MaxValue);
            ref Color32 reference6 = ref teamUniformColors[5];
            reference6 = new Color32(53, 156, 45, byte.MaxValue);
            ref Color32 reference7 = ref teamUniformColors[6];
            reference7 = new Color32(15, 101, 64, byte.MaxValue);
            ref Color32 reference8 = ref teamUniformColors[7];
            reference8 = new Color32(180, 44, 0, byte.MaxValue);
            ref Color32 reference9 = ref teamUniformColors[8];
            reference9 = new Color32(32, 32, 32, byte.MaxValue);
            ref Color32 reference10 = ref teamUniformColors[9];
            reference10 = new Color32(35, 73, 75, byte.MaxValue);
            ref Color32 reference11 = ref teamUniformColors[10];
            reference11 = new Color32(45, 164, 208, byte.MaxValue);
            ref Color32 reference12 = ref teamUniformColors[12];
            reference12 = new Color32(21, 77, 45, byte.MaxValue);
            ref Color32 reference13 = ref teamUniformColors[11];
            reference13 = new Color32(18, 59, 115, byte.MaxValue);
            ref Color32 reference14 = ref teamUniformColors[13];
            reference14 = new Color32(54, 58, 60, byte.MaxValue);
            ref Color32 reference15 = ref teamUniformColors[14];
            reference15 = new Color32(137, 0, 41, byte.MaxValue);
            ref Color32 reference16 = ref teamUniformColors[15];
            reference16 = new Color32(178, 0, 0, byte.MaxValue);
            ref Vector3 reference17 = ref teamUniformGreyScales[0];
            reference17 = darkJerseyGreyScaleValues;
            ref Vector3 reference18 = ref teamUniformGreyScales[1];
            reference18 = brightJerseyGreyScaleValues;
            ref Vector3 reference19 = ref teamUniformGreyScales[2];
            reference19 = darkJerseyGreyScaleValues;
            ref Vector3 reference20 = ref teamUniformGreyScales[3];
            reference20 = darkJerseyGreyScaleValues;
            ref Vector3 reference21 = ref teamUniformGreyScales[4];
            reference21 = brightJerseyGreyScaleValues;
            ref Vector3 reference22 = ref teamUniformGreyScales[5];
            reference22 = brightJerseyGreyScaleValues;
            ref Vector3 reference23 = ref teamUniformGreyScales[6];
            reference23 = darkJerseyGreyScaleValues;
            ref Vector3 reference24 = ref teamUniformGreyScales[7];
            reference24 = brightJerseyGreyScaleValues;
            ref Vector3 reference25 = ref teamUniformGreyScales[8];
            reference25 = brightJerseyGreyScaleValues;
            ref Vector3 reference26 = ref teamUniformGreyScales[9];
            reference26 = brightJerseyGreyScaleValues;
            ref Vector3 reference27 = ref teamUniformGreyScales[10];
            reference27 = brightJerseyGreyScaleValues;
            ref Vector3 reference28 = ref teamUniformGreyScales[11];
            reference28 = brightJerseyGreyScaleValues;
            ref Vector3 reference29 = ref teamUniformGreyScales[12];
            reference29 = brightJerseyGreyScaleValues;
            ref Vector3 reference30 = ref teamUniformGreyScales[13];
            reference30 = brightJerseyGreyScaleValues;
            ref Vector3 reference31 = ref teamUniformGreyScales[14];
            reference31 = new Vector3(0f, 1.8f, -1.08f);
            ref Vector3 reference32 = ref teamUniformGreyScales[15];
            reference32 = new Vector3(0f, 2.3f, -1.08f);
            ref Color32 reference33 = ref teamStripColors[0];
            reference33 = new Color32(217, 0, 12, byte.MaxValue);
            ref Color32 reference34 = ref teamStripColors[1];
            reference34 = new Color32(37, 46, 0, byte.MaxValue);
            ref Color32 reference35 = ref teamStripColors[2];
            reference35 = new Color32(180, 16, 16, byte.MaxValue);
            ref Color32 reference36 = ref teamStripColors[3];
            reference36 = new Color32(214, 3, 5, byte.MaxValue);
            ref Color32 reference37 = ref teamStripColors[4];
            reference37 = new Color32(161, 60, 21, byte.MaxValue);
            ref Color32 reference38 = ref teamStripColors[5];
            reference38 = new Color32(5, 23, 89, byte.MaxValue);
            ref Color32 reference39 = ref teamStripColors[6];
            reference39 = new Color32(180, 16, 16, byte.MaxValue);
            ref Color32 reference40 = ref teamStripColors[7];
            reference40 = new Color32(8, 18, 53, byte.MaxValue);
            ref Color32 reference41 = ref teamStripColors[8];
            reference41 = new Color32(212, 212, 212, byte.MaxValue);
            ref Color32 reference42 = ref teamStripColors[9];
            reference42 = new Color32(180, 155, 0, byte.MaxValue);
            ref Color32 reference43 = ref teamStripColors[10];
            reference43 = new Color32(7, 32, 49, byte.MaxValue);
            ref Color32 reference44 = ref teamStripColors[12];
            reference44 = new Color32(218, 122, 0, byte.MaxValue);
            ref Color32 reference45 = ref teamStripColors[11];
            reference45 = new Color32(142, 126, 9, byte.MaxValue);
            ref Color32 reference46 = ref teamStripColors[13];
            reference46 = new Color32(169, 0, 18, byte.MaxValue);
            ref Color32 reference47 = ref teamStripColors[14];
            reference47 = new Color32(183, 182, 11, byte.MaxValue);
            ref Color32 reference48 = ref teamStripColors[15];
            reference48 = new Color32(188, 140, 0, byte.MaxValue);
            ref Color32 reference49 = ref teamSkinColors[0];
            reference49 = new Color32(180, 180, 180, 1);
            ref Color32 reference50 = ref teamSkinColors[1];
            reference50 = new Color32(200, 200, 200, 1);
            ref Color32 reference51 = ref teamSkinColors[2];
            reference51 = new Color32(120, 120, 120, 1);
            ref Color32 reference52 = ref teamSkinColors[3];
            reference52 = new Color32(176, 176, 176, 1);
            ref Color32 reference53 = ref teamSkinColors[4];
            reference53 = new Color32(160, 160, 160, 1);
            ref Color32 reference54 = ref teamSkinColors[5];
            reference54 = new Color32(200, 200, 200, 1);
            ref Color32 reference55 = ref teamSkinColors[6];
            reference55 = new Color32(80, 80, 80, 1);
            ref Color32 reference56 = ref teamSkinColors[7];
            reference56 = new Color32(176, 176, 176, 1);
            ref Color32 reference57 = ref teamSkinColors[8];
            reference57 = new Color32(200, 200, 200, 1);
            ref Color32 reference58 = ref teamSkinColors[9];
            reference58 = new Color32(140, 140, 140, 1);
            ref Color32 reference59 = ref teamSkinColors[10];
            reference59 = new Color32(176, 176, 176, 1);
            ref Color32 reference60 = ref teamSkinColors[12];
            reference60 = new Color32(200, 200, 200, 1);
            ref Color32 reference61 = ref teamSkinColors[11];
            reference61 = new Color32(110, 110, 110, 1);
            ref Color32 reference62 = ref teamSkinColors[13];
            reference62 = new Color32(140, 140, 140, 1);
            ref Color32 reference63 = ref teamSkinColors[14];
            reference63 = new Color32(95, 95, 95, 1);
            ref Color32 reference64 = ref teamSkinColors[15];
            reference64 = new Color32(200, 200, 200, 1);
        }
        else
        {

            ref Color32 reference129 = ref teamSkinColors[0];
            reference129 = new Color32(160, 160, 160, 1);
            ref Color32 reference130 = ref teamSkinColors[1];
            reference130 = new Color32(160, 160, 160, 1);
            ref Color32 reference131 = ref teamSkinColors[2];
            reference131 = new Color32(160, 160, 160, 1);
            ref Color32 reference132 = ref teamSkinColors[3];
            reference132 = new Color32(160, 160, 160, 1);
            ref Color32 reference133 = ref teamSkinColors[4];
            reference133 = new Color32(160, 160, 160, 1);
            ref Color32 reference134 = ref teamSkinColors[5];
            reference134 = new Color32(160, 160, 160, 1);
            ref Color32 reference135 = ref teamSkinColors[6];
            reference135 = new Color32(160, 160, 160, 1);
            ref Color32 reference136 = ref teamSkinColors[7];
            reference136 = new Color32(160, 160, 160, 1);
            ref Color32 reference137 = ref teamSkinColors[8];
            reference137 = new Color32(160, 160, 160, 1);
            ref Color32 reference138 = ref teamSkinColors[9];
            reference138 = new Color32(160, 160, 160, 1);
        }
        for (int l = 0; l < 4; l++)
        {
            ref Vector3 reference139 = ref defaultHotspotPositions[l];
            reference139 = hotspotReferences[l].transform.localPosition;
        }
        for (int m = 0; m < 4; m++)
        {
            ref Vector3 reference140 = ref defaultBallPathPoints[m];
            reference140 = ballPath1Points[m].transform.localPosition;
        }
        snickoTransformPosition = snickoMeterObject.transform.localPosition;
        ultraEdgeViewPos = infraredCamera.transform.position;
        ultraEdgeViewRot = infraredCamera.transform.eulerAngles;
        replaySideCamPos = sideCamera.transform.position;
        replaySideCamRot = sideCamera.transform.eulerAngles;
        savedEdgeTransformValue = edgeReferences.localPosition;
        umpireCamTransform = umpireViewCamera.transform;
        introCamTransform = introCutsceneCamera.transform;
        replayCamTransform = replayViewCamera.transform;
        rightCamTransform = rightFieldCamera.transform;
        leftCamTransform = leftFieldCamera.transform;
        ultraMotionCameraTransform = slowMotionCamera.transform;
        closeUpCameraTransform = closeUpViewCamera.transform;
        groundGameObject = GameObject.Find("Blitz");
        objectsToHide = GameObject.Find("Blitz/HideableObjects");
        pitchAndLogo = GameObject.Find("Blitz/Pitch_collections");
        introCameraAnchor = GameObject.Find("IntroCameraPivot");
        replayCameraController = replayViewCamera.GetComponent<ReplaySmoothFollow>();
        replayViewCamera.enabled = false;
        uiGameObject = GameObject.Find("MainCamera");
        uiCamera = uiGameObject.GetComponent("Camera") as Camera;
        gameplayCamera = Camera.main;
        mainCamTransform = gameplayCamera.transform;
        umpireCameraAnchor = new GameObject("umpireCameraPivot");
        matchBall = GameObject.Find("Ball");
        matchBallTransform = matchBall.transform;
        raycastAnchorBallGO = GameObject.Find("BallRayCastReference");
        raycastAnchorBallTransform = raycastAnchorBallGO.transform;
        fielderFocusObjectToCollectBall = new GameObject("Fielder10FocusGObjToCollectTheBall");
        fielderFocusObjectToCollectBall.transform.position = new Vector3(0f, 0f, 0f);
        ballCollider = matchBall.GetComponent("SphereCollider") as SphereCollider;
        ballStartPositionGO = GameObject.Find("BallOrigin");
        bowlingSpotObject = GameObject.Find("BowlingSpot");
        bowlingSpotFullTossObject = GameObject.Find("BowlingSpotFullToss");
        temporaryBallStartPoint = GameObject.Find("TempBallStartPoint");
        bowlingSpotFullTossObject.SetActive(value: false);
        bowlingSpotModel = GameObject.Find("BowlingSpotModel");
        bowlingSpotRenderer = bowlingSpotModel.GetComponent<Renderer>();
        bowlingSpot = bowlingSpotObject.transform;
        ballTimingStartGO = GameObject.Find("BallTimingOrigin");
        ballStartPoint = ballTimingStartGO.transform;
        PrimaryBatCollider = GameObject.Find("BatCollider");
        SecondaryBatCollider = GameObject.Find("BatCollider2");
        // #1 (left-handed batsman can't hit — tester "2nd team not able to hit a single shot"): the bat
        // contact colliders (BatCollider/BatCollider2/edgePlane) are BoxColliders, and Unity does NOT mirror a
        // BoxCollider under negative scale. A left-handed batsman is set up with localScale.x = -1 (see ~3266),
        // so the bat colliders stay on the RIGHT-handed side while the visible bat is mirrored to the left — the
        // contact raycast/trigger then misses and NO shot connects (right-handers are fine, scale = +1). Convert
        // the bat boxes to CONVEX MeshColliders using the unit-cube mesh (identical volume — the boxes are unit
        // cubes scaled by their transform), which DO support negative scale (Unity's own recommendation, and
        // what the leg colliders already use). Idempotent; runs before BatCollider is cached below so refs stay valid.
        MakeBatColliderNegativeScaleSafe(PrimaryBatCollider);
        MakeBatColliderNegativeScaleSafe(SecondaryBatCollider);
        MakeBatColliderNegativeScaleSafe(GameObject.Find("edgePlane"));
        LeftLowerLeg = GameObject.Find("LeftLowerLeg");
        RightLowerLeg = GameObject.Find("RightLowerLeg");
        LeftUpperLeg = GameObject.Find("LeftUpperLeg");
        RightUpperLeg = GameObject.Find("RightUpperLeg");
        sliceEffect = GameObject.Find("NinjaSlice");
        Stump1Collider = GameObject.Find("Stump1Collider");
        Stump2Collider = GameObject.Find("Stump2Collider");
        BoundaryBoardCollider = GameObject.Find("Board");
        ballFirstBounce = GameObject.Find("BallTimingFirstBounce");
        ballCatchingPoint = GameObject.Find("BallCatchingSpot");
        ballCatchingPointTransform = ballCatchingPoint.transform;
        ballTrailRenderer = matchBall.gameObject.GetComponent<TrailRenderer>();
        creaseImpactSpot = GameObject.Find("BallSpotAtCreaseLine");
        stumpImpactSpot = GameObject.Find("BallSpotAtStump");
        _batsmanTransform = batsmanObject.transform;
        batsmanAnim = batsmanObject.GetComponent<Animation>();
        RightHandedBatsmanMaxLimit = GameObject.Find("RHBatsmanMaxBowlLimit");
        RightHandedBatsmanMinLimit = GameObject.Find("RHBatsmanMinBowlLimit");
        LeftHandedBatsmanMaxLimit = GameObject.Find("LHBatsmanMaxBowlLimit");
        LeftHandedBatsmanMinLimit = GameObject.Find("LHBatsmanMinBowlLimit");
        rightHandedBatsmanInitialPosition = _batsmanTransform.position;
        leftHandedBatsmanInitialSpot = GameObject.Find("LHBatsmanInitSpot");
        leftHandedBatsmanInitialPosition = leftHandedBatsmanInitialSpot.transform.position;
        shotActivationMinBoundary = GameObject.Find("ShotActivationMinLimit");
        shotActivationMaxBoundary = GameObject.Find("ShotActivationMaxLimit");
        rightHandedBatsmanBackwardLimit = GameObject.Find("RHBatsmanBackwardLimit");
        rightHandedBatsmanForwardLimit = GameObject.Find("RHBatsmanForwardLimit");
        leftHandedBatsmanBackwardLimit = GameObject.Find("LHBatsmanBackwardLimit");
        leftHandedBatsmanForwardLimit = GameObject.Find("LHBatsmanForwardLimit");
        rightHandedBatsmanMaxWideLimit = GameObject.Find("RHBMaxWideLimit");
        rightHandedBatsmanMinWideLimit = GameObject.Find("RHBMinWideLimit");
        leftHandedBatsmanMaxWideLimit = GameObject.Find("LHBMaxWideLimit");
        leftHandedBatsmanMinWideLimit = GameObject.Find("LHBMinWideLimit");
        bowlerObject = GameObject.Find("/Bowler");
        bowlerModel = GameObject.Find("/Bowler/Bowler");
        bowlerBallObject = GameObject.Find("/Bowler/Sphere");
        bowlerHipBoneTransform = bowlerObject.transform.Find("Armature/Bone/hip");
        groundGameObject = GameObject.Find("Blitz");
        objectsToHide = GameObject.Find("Blitz/HideableObjects");
        pitchAndLogo = GameObject.Find("Blitz/Pitch_collections");
        introCameraAnchor = GameObject.Find("IntroCameraPivot");
        replayCameraController = replayViewCamera.GetComponent<ReplaySmoothFollow>();
        replayViewCamera.enabled = false;
        uiGameObject = GameObject.Find("MainCamera");
        uiCamera = uiGameObject.GetComponent("Camera") as Camera;
        gameplayCamera = Camera.main;
        mainCamTransform = gameplayCamera.transform;
        umpireCameraAnchor = new GameObject("umpireCameraPivot");
        matchBall = GameObject.Find("Ball");
        matchBallTransform = matchBall.transform;
        impactMarkerBall = GameObject.Find("ImpactBall");
        impactMarkerBall.SetActive(value: false);
        raycastAnchorBallGO = GameObject.Find("BallRayCastReference");
        raycastAnchorBallTransform = raycastAnchorBallGO.transform;
        fielderFocusObjectToCollectBall = new GameObject("Fielder10FocusGObjToCollectTheBall");
        fielderFocusObjectToCollectBall.transform.position = new Vector3(0f, 0f, 0f);
        ballCollider = matchBall.GetComponent("SphereCollider") as SphereCollider;
        ballStartPositionGO = GameObject.Find("BallOrigin");
        bowlingSpotObject = GameObject.Find("BowlingSpot");
        bowlingSpotModel = GameObject.Find("BowlingSpotModel");
        bowlingSpot = bowlingSpotObject.transform;
        ballTimingStartGO = GameObject.Find("BallTimingOrigin");
        ballStartPoint = ballTimingStartGO.transform;
        PrimaryBatCollider = GameObject.Find("BatCollider");
        // #1 left-handed-batsman contact fix (see the other Awake branch): negative-scale-safe bat colliders.
        MakeBatColliderNegativeScaleSafe(PrimaryBatCollider);
        MakeBatColliderNegativeScaleSafe(GameObject.Find("BatCollider2"));
        MakeBatColliderNegativeScaleSafe(GameObject.Find("edgePlane"));
        LeftLowerLeg = GameObject.Find("LeftLowerLeg");
        RightLowerLeg = GameObject.Find("RightLowerLeg");
        LeftUpperLeg = GameObject.Find("LeftUpperLeg");
        RightUpperLeg = GameObject.Find("RightUpperLeg");
        sliceEffect = GameObject.Find("NinjaSlice");
        Stump1Collider = GameObject.Find("Stump1Collider");
        Stump2Collider = GameObject.Find("Stump2Collider");
        BoundaryBoardCollider = GameObject.Find("Board");
        ballFirstBounce = GameObject.Find("BallTimingFirstBounce");
        ballCatchingPoint = GameObject.Find("BallCatchingSpot");
        ballCatchingPointTransform = ballCatchingPoint.transform;
        ballTrailRenderer = matchBall.gameObject.GetComponent<TrailRenderer>();
        trailColorGradient = ballTrailRenderer.colorGradient;
        trailMaterial = ballTrailRenderer.material;
        creaseImpactSpot = GameObject.Find("BallSpotAtCreaseLine");
        stumpImpactSpot = GameObject.Find("BallSpotAtStump");
        _batsmanTransform = batsmanObject.transform;
        batsmanAnim = batsmanObject.GetComponent<Animation>();
        RightHandedBatsmanMaxLimit = GameObject.Find("RHBatsmanMaxBowlLimit");
        RightHandedBatsmanMinLimit = GameObject.Find("RHBatsmanMinBowlLimit");
        LeftHandedBatsmanMaxLimit = GameObject.Find("LHBatsmanMaxBowlLimit");
        LeftHandedBatsmanMinLimit = GameObject.Find("LHBatsmanMinBowlLimit");
        rightHandedBatsmanInitialPosition = _batsmanTransform.position;
        leftHandedBatsmanInitialSpot = GameObject.Find("LHBatsmanInitSpot");
        leftHandedBatsmanInitialPosition = leftHandedBatsmanInitialSpot.transform.position;
        shotActivationMinBoundary = GameObject.Find("ShotActivationMinLimit");
        shotActivationMaxBoundary = GameObject.Find("ShotActivationMaxLimit");
        rightHandedBatsmanBackwardLimit = GameObject.Find("RHBatsmanBackwardLimit");
        rightHandedBatsmanForwardLimit = GameObject.Find("RHBatsmanForwardLimit");
        leftHandedBatsmanBackwardLimit = GameObject.Find("LHBatsmanBackwardLimit");
        leftHandedBatsmanForwardLimit = GameObject.Find("LHBatsmanForwardLimit");
        rightHandedBatsmanMaxWideLimit = GameObject.Find("RHBMaxWideLimit");
        rightHandedBatsmanMinWideLimit = GameObject.Find("RHBMinWideLimit");
        leftHandedBatsmanMaxWideLimit = GameObject.Find("LHBMaxWideLimit");
        leftHandedBatsmanMinWideLimit = GameObject.Find("LHBMinWideLimit");
        bowlerObject = GameObject.Find("/Bowler");
        bowlerModel = GameObject.Find("/Bowler/Bowler");
        bowlerBallObject = GameObject.Find("/Bowler/Sphere");
        bowlerHipBoneTransform = bowlerObject.transform.Find("Armature/Bone/hip");
        leftLegEdgeObject = GameObject.Find("Batsman/LeftShoeEdge");

        rightLegEdgeObject = GameObject.Find("Batsman/RightShoeEdge");

        batEdgeObject = GameObject.Find("Batsman/rig/bat/BatEdge");

        _batTopEdgeTransform = GameObject.Find("Batsman/rig/bat/BatTopEdge").transform;

        _batShadowHolderTransform = GameObject.Find("ShadowHolder/BatShadowHolder").transform;
        batterLeftLegEdgePoint = GameObject.Find("Batsman/LeftLegEdgePoint");

        batterLeftShoeBackEdge = GameObject.Find("Batsman/LeftShoeBackEdge");

        batterRightShoeBackEdge = GameObject.Find("Batsman/RightShoeBackEdge");

        bowlingInterfaceHideSpot = GameObject.Find("HideBowlingInterface");
        userBowlingMinimumLimit = GameObject.Find("UserBowlingMinLimit");
        userBowlingMaximumLimit = GameObject.Find("UserBowlingMaxLimit");
        userBowlingFullTossTriggerPoint = GameObject.Find("UserBowlingMinFullTossTrigger");
        fielder10Object = GameObject.Find("/Fielders/Fielder10");
        fielder10SkinTransform = fielder10Object.transform;
        fielder10Model = GameObject.Find("/Fielders/Fielder10/Fielder");
        fielder10BallObject = GameObject.Find("/Fielders/Fielder10/Sphere");
        fielder10FastPosition = GameObject.Find("Fielder10FastInit");
        fielder10SpinPosition = GameObject.Find("Fielder10SpinInit");
        setFieldersPosition();
        fielderModels[10] = GameObject.Find("/Fielders/Fielder10/Fielder");
        FielderSkinRenderer[10] = fielderModels[10].GetComponent<Renderer>();
        spinSlipSpotForRHB = GameObject.Find("SlipFielderSpotForRHBSpin");
        secondSpinSlipSpotForRHB = GameObject.Find("SlipFielder2SpotForRHBSpin");
        wicketKeeperObject = GameObject.Find("WicketKeeper");
        _wicketKeeperTransform = wicketKeeperObject.transform;
        //wicketKeeperSkin = GameObject.Find("WicketKeeper/Armature/Wicket_keeper_");
        wicketKeeperModel = GameObject.Find("WicketKeeper/keeper");

        //wicketKeeperBall = GameObject.Find("WicketKeeper/Armature/Sphere");
        wicketKeeperBallObject = GameObject.Find("WicketKeeper/Sphere");

        wicketKeeperFastPositionForRHB = GameObject.Find("WicketKeeperInitPos4RHBFast").transform.position;
        wicketKeeperFastPositionForLHB = GameObject.Find("WicketKeeperInitPos4LHBFast").transform.position;
        wicketKeeperSpinPositionForRHB = GameObject.Find("WicketKeeperInitPos4RHBSpin").transform.position;
        wicketKeeperSpinPositionForLHB = GameObject.Find("WicketKeeperInitPos4LHBSpin").transform.position;
        wicketKeeperStraightStumpingPosition = GameObject.Find("WicketKeeperStraightBallStumpingPos");
        wicketKeeperLegSideStumpingPosition = GameObject.Find("WicketKeeperLegSideBallStumpingPos");
        wicketKeeperOffSideStumpingPosition = GameObject.Find("WicketKeeperOffSideBallStumpingPos");
        //WKRefPoint = GameObject.Find("WicketKeeper/Armature/WKRefPoint");
        fielder10ReferencePoint = GameObject.Find("/Fielders/Fielder10/Ref");
        fielderStraightStumpingPosition = GameObject.Find("FielderStraightBallStumpingPos");
        fielderLegSideStumpingPosition = GameObject.Find("FielderLegSideBallStumpingPos");
        fielderOffSideStumpingPosition = GameObject.Find("FielderOffSideBallStumpingPos");
        _runnerTransform = currentRunner.transform;
        rhbNonStrikerRunSpot = GameObject.Find("RHBNonStickerRunningSpot");
        rhbStrikerRunSpot = GameObject.Find("RHBStickerRunningSpot");
        strikerRunSpotForRunner = GameObject.Find("RunnerStickerRunningSpot");
        nonStrikerRunSpotForRunner = GameObject.Find("RunnerNonStickerRunningSpot");
        nonStrikerCreaseSpot = GameObject.Find("NonStickerNearCreaseSpot");
        strikerCreaseSpot = GameObject.Find("StickerNearCreaseSpot");
        runnerInitialPosition = _runnerTransform.position;
        nonStrikerTargetSpot = GameObject.Find("NonStickerReachSpot");
        strikerTargetSpot = GameObject.Find("StickerReachSpot");
        groundCenterMarker = GameObject.Find("GroundCenterPoint");
        groundCenterMarkerTransform = groundCenterMarker.transform;
        stumpLeft = GameObject.Find("Stump1");
        stumpRight = GameObject.Find("Stump2");
        stumpLeftCrease = GameObject.Find("Stump1Crease");
        stumpRightCrease = GameObject.Find("Stump2Crease");
        stumpLeftSpot = GameObject.Find("Stump1Spot");
        stumpRightSpot = GameObject.Find("Stump2Spot");
        scoreboardScreenScale = scoreboardScreen.transform.localScale;
        _mainUmpireTransform = mainUmpireTransform;
        mainUmpireAppearance = GameObject.Find("1MainUmpire/Umpire");
        sideUmpireAppearance = GameObject.Find("1SideUmpire/Umpire");
        mainUmpireInitialPosition = _mainUmpireTransform.position;
        umpireLeftSpot = GameObject.Find("UmpireLeftSideSpot");
        umpireRightSpot = GameObject.Find("UmpireRightSideSpot");
        replayController = GameObject.Find("ReplayController");
        replayControllerTransform = replayController.transform;
        replayController = GameObject.Find("ReplayController");
        replayControllerTransform = replayController.transform;
        uiGameObject = GameObject.Find("MainCamera");
        HideBowlingSpot();
        ShowFullTossSpot(_Value: false);
        initialBallPosition = matchBallTransform.position;
        shadowContainer = GameObject.Find("ShadowHolder");
        batsmanAnim = batsmanObject.GetComponent<Animation>();
        //BowlerAnimationComponent = bowler.GetComponent<Animation>();
        bowlerAnimator = bowlerObject.GetComponent<Animator>();
        keeperAnim = wicketKeeperObject.GetComponent<Animation>();
        mainUmpireAnim = mainUmpireTransform.GetComponent<Animation>();
        sideUmpireAnim = sideUmpireObject.GetComponent<Animation>();
        fielder10Anim = fielder10Object.GetComponent<Animation>();
        //ConstantsData_M.MpLog(Fielder10AnimationComponent + "ANIMATION");
        stump1Anim = stumpLeft.GetComponent<Animation>();
        stump2Anim = stumpRight.GetComponent<Animation>();
        runnerAnim = currentRunner.GetComponent<Animation>();
        fielder10SkinRenderer = fielder10Model.GetComponent<Renderer>();
        batsmanSkinRenderer = BatsmanModel.GetComponent<Renderer>();
        RunnerSkinRenderer = runnerAppearance.GetComponent<Renderer>();
        BowlerSkinRenderer = bowlerModel.GetComponent<Renderer>();
        WicketKeeperSkinRenderer = wicketKeeperModel.GetComponent<Renderer>();
        Fielder10BallSkinRenderer = fielder10BallObject.GetComponent<Renderer>();
        BallSkinRenderer = matchBall.GetComponent<Renderer>();
        WicketKeeperBallSkin = wicketKeeperBallObject.GetComponent<Renderer>();
        BowlerBallSkinRenderer = bowlerBallObject.GetComponent<Renderer>();
        DigitalScreenRenderer = scoreboardScreen.GetComponent<Renderer>();
        BatCollider = PrimaryBatCollider.GetComponent<Collider>();
        if (enableShadows)
        {
            GameObject item;
            GameObject item2;
            for (int n = 1; n <= numberOfFielders; n++)
            {
                item = GameObject.Find("FielderShadow" + n);
                shadowObjects.Add(item);
                //if (n == 7)
                //{
                //    item2 = GameObject.Find("Fielders/Fielder" + n + "/ShadowRef");
                //}
                //else
                //{
                //}
                //item2 = GameObject.Find("Fielders/Fielder" + n + "/Armature/Bone/hip/ShadowRef");
                item2 = GameObject.Find("Fielders/Fielder" + n + "/ShadowRef");

                shadowReferences.Add(item2);
            }
            item = GameObject.Find("ShadowHolder/BowlerShadow");
            shadowObjects.Add(item);
            //item2 = GameObject.Find("Fielders/Fielder10/Armature/Bone/hip/ShadowRef");
            item2 = GameObject.Find("Fielders/Fielder10/ShadowRef");

            shadowReferences.Add(item2);
            item = GameObject.Find("ShadowHolder/WicketKeeperShadow");
            shadowObjects.Add(item);
            //item2 = GameObject.Find("WicketKeeper/Armature/Bone/hip/ShadowRef");
            item2 = GameObject.Find("WicketKeeper/rig/c_pos/c_traj/c_root_master.x/c_root.x/c_root_bend.x/root.x/ShadowRef78");

            shadowReferences.Add(item2);
            item = GameObject.Find("ShadowHolder/BatsmanShadow");
            shadowObjects.Add(item);
            //item2 = GameObject.Find("Batsman/Armature/Bone/hip/ShadowRef");
            item2 = GameObject.Find("Batsman/rig/c_pos/c_traj/c_root_master.x/c_root.x/c_root_bend.x/root.x/ShadowRef22");
            shadowReferences.Add(item2);
            item = GameObject.Find("ShadowHolder/RunnerShadow");
            shadowObjects.Add(item);
            //item2 = GameObject.Find("Runner/Armature/Bone/hip/ShadowRef");
            item2 = GameObject.Find("Runner/rig/c_pos/c_traj/c_root_master.x/c_root.x/c_root_bend.x/root.x/ShadowRef22");

            shadowReferences.Add(item2);
            item = GameObject.Find("ShadowHolder/MainUmpireShadow");
            shadowObjects.Add(item);
            //item2 = GameObject.Find("MainUmpire/metarig/hips/ShadowRef");
            item2 = GameObject.Find("MainUmpire/rig/c_pos/c_traj/c_root_master.x/c_root.x/c_root_bend.x/root.x/ShadowRef");

            shadowReferences.Add(item2);
            item = GameObject.Find("ShadowHolder/SideUmpireShadow");
            shadowObjects.Add(item);
            //item2 = GameObject.Find("SideUmpire/metarig/hips/ShadowRef");
            item2 = GameObject.Find("SideUmpire/rig/c_pos/c_traj/c_root_master.x/c_root.x/c_root_bend.x/root.x/ShadowRef");

            shadowReferences.Add(item2);
            item = GameObject.Find("ShadowHolder/BallShadow");
            shadowObjects.Add(item);
            item2 = GameObject.Find("Ball/ShadowRef");
            shadowReferences.Add(item2);
            bowlerShadowReference = GameObject.Find("Bowler/Armature/Bone/hip/ShadowRef");
            // bowlerShadowReference = GameObject.Find("Bowler/ShadowRef");

            bowlerShadowTransform = bowlerShadowReference.transform;
            for (int num = 0; num < shadowReferences.Count; num++)
            {
                shadowReferenceTransforms.Add(shadowReferences[num].transform);
                shadowTransforms.Add(shadowObjects[num].transform);
            }
            UpdateShadow();
            shadowContainer.SetActive(true);
        }
        else
        {
            shadowContainer.SetActive(false);
        }
        batsmanReferencePoint = GameObject.Find("Batsman/Armature/BatsmanRefPoint").transform;
        ShotVariables.InitShotVariables();
        resumeGameObject.SetActive(value: false);
        HideBatShadow();
        if (!Singleton<NavigationBack>.instance.disableDeviceBack)
        {
            Singleton<NavigationBack>.instance.disableDeviceBack = true;
        }
        Singleton<NavigationBack>.instance.deviceBack = OpenPause;
        if (CONTROLLER.PlayModeSelected != 7)
        {
            MainUmpireSkinRenderer.materials[0].SetTexture("_PatternTex", umpireTextures[umpireMainIndex]);
            SideUmpireSkinRenderer.materials[0].SetTexture("_PatternTex", umpireTextures[umpireSideIndex]);
        }
        else
        {
            MainUmpireSkinRenderer.materials[0].SetTexture("_PatternTex", umpireTextures[2]);
            SideUmpireSkinRenderer.materials[0].SetTexture("_PatternTex", umpireTextures[2]);
        }
    }

    private void OpenPause()
    {
        if (Singleton<Scoreboard>.instance.pauseBtn.gameObject.activeInHierarchy && Singleton<Scoreboard>.instance.pauseBtn.enabled)
        {
            Singleton<PauseGameScreen>.instance.Hide(boolean: false);
        }
    }

    private void DisableBG()
    {
        Time.timeScale = 1f;
        backgroundImage.gameObject.SetActive(value: false);
    }

    public void Start()
    {

        enhancedMode();
        isBallPickedByFielder = false;
        velocityDampingFactor = 0f;
        applyFrictionReduction = false;
        triggerRunCancel = false;
        isOutLBW = false;
        isEdgeCaught = false;
        mpCaughtBehindOutPending = false;
        hasTopEdge = false;
        didReachFirstBounce = false;
        Physics.IgnoreLayerCollision(11, 8, ignore: true);
        Physics.IgnoreLayerCollision(11, 8, ignore: true);
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        showPreviewCamera(status: false);
        umpireViewCamera.enabled = false;
        closeUpViewCamera.enabled = false;
        UltraEdgeVariables.InitUltraEdgeVariables();
        if (CONTROLLER.isFreeHitBall && CONTROLLER.matchType == "oneday" && CONTROLLER.GameStartsFromSave)
        {
            isLineFreeHitActive = true;
            lastBowledBallType = "overstep";
            CONTROLLER.isFreeHitBall = false;
            ////ConstantsData_M.MpLog("SHOWFREEHIT");
            Singleton<Scoreboard>.instance.showFreeHitBg(canShow: true);
        }
        ShowFielder10(fielder10Status: false, ball10Status: false);
        ResetAll();
        if (Singleton<GameData>.instance.currentAction == -20)
        {
            Singleton<Intro>.instance.initGameIntro();
        }
        setPreviewCamPanel();
        ActivateColliders(boolean: false);
        Stump2Collider.SetActive(false);
        BoundaryBoardCollider.SetActive(false);
        if (CONTROLLER.PlayModeSelected == 8)
        {
            if (CONTROLLER.tutorialToggle == 0)
            {
                CallOppTutorial(false);
            }
            else
            {
                CallOppTutorial(true);
            }
        }
        //if (GameConstants.isWithAI == false)
        //{                                                                                   //Photon Removal
        //    ConnectAndJoin.RoomName = PhotonNetwork.CurrentRoom.Name;
        //    ConnectAndJoin.gameObject.SetActive(true);
        //}
    }

    public void ResetAll()
    {
        ////ConstantsData_M.MpLog("RESETTT");
        distanceToNextPitch = 0f;
        didBallHitBat = false;
        isNoBall = false;
        isSlowMotionActiveForNoBall = false;
        noBallActionDelay = 0f;
        distanceBetweenUmpireAndFielder(boolean: true);
        keeperCaughtSpecialCatch = false;
        fielderThrowTimeElapsed = 0f;
        canShowFieldControlPowers = false;
        isBallPickedByFielder = false;
        applyFrictionReduction = false;
        hasBeenBowled = false;
        umpireDecisionChance = 50;
        edgeProbabilityChance = 50;
        bowlingSpotFullTossObject.SetActive(value: false);
        closestFielderIndex = -1;
        boundaryAnimationName = string.Empty;
        fenceHeight = 0f;
        hasUmpireAnimationPlayed = false;
        tempPoint1 = default(Vector3);
        tempPoint2 = default(Vector3);
        isSwipeAllowed = false;
        edgeReferences.localPosition = savedEdgeTransformValue;
        cancelRunDirection = 1;
        isRunCancelled = false;
        triggerRunCancel = false;
        shouldMoveUmpire = true;
        isPerfectShot = false;
        fielderHasThrown = false;
        Singleton<MainCameraController>.instance.CanMoveCamera = false;
        hasTopEdge = false;
        didReachFirstBounce = false;
        sixRunCam.enabled = false;
        isHardcoded = false;
        ShowUltraEdgeCam(canShow: false);
        isDRSHardcoded = false;
        sixDistanceSaved = 0f;
        if (batsmanHand == "right")
        {
            _batsmanTransform.position = rightHandedBatsmanInitialPosition;
        }
        else
        {
            _batsmanTransform.position = leftHandedBatsmanInitialPosition;
        }
        canKeeperCatchBall = false;
        isEdgeCaught = false;
        needsBattingTimingMeterNeedleUpdate = false;
        Singleton<BattingControls>.instance.battingMeter.SetActive(value: false);
        Singleton<BattingControls>.instance.GreedyAdsImage.SetActive(value: true);
        isOutLBW = false;
        throwTarget = wicketKeeperObject;
        Singleton<UILookAt>.instance.show(flag: false);
        ballTrailRenderer.enabled = false;
        isAICancelRun = false;
        isRunning = false;
        if (!isReplayModeActive)
        {
            isEdgePositionSaved = false;
            totalElapsedTime = 0f;
        }
        if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected != 8)
        {
            defaultFielderSpeed = 7f * (1f + agilityMultiplier);
        }
        else
        {
            defaultFielderSpeed = 7f;
        }
        isTouchDeviceShotInputEnabled = false;
        bounceCount = 0;
        launchAngle = 270f;
        arcHeight = 2.15f;
        horizontalVelocity = 18f;
        swingAngle = 0f;
        swingIntensity = 0f;
        isBallSwinging = false;
        isSlipShot = false;
        isBallToFineLeg = false;
        Singleton<Intro>.instance.TempCutsceneCamera.enabled = false;
        preCatchDistance = 1f;
        bowlerRunSpeed = 5f;
        isBallPaused = false;
        currentShotPlayed = string.Empty;
        outcomeOfBall = string.Empty;
        canRun = false;
        applyBallFriction = false;
        runsScoredThisBall = 0;
        if (Singleton<MainCameraController>.instance != null)
        {
            Singleton<MainCameraController>.instance.Reset();
        }
        if (Singleton<LeftFovLerp>.instance != null)
        {
            Singleton<LeftFovLerp>.instance.Reset();
        }
        if (Singleton<RightSmoothFov>.instance != null)
        {
            Singleton<RightSmoothFov>.instance.Reset();
        }
        Cricket.GradualLookAt.CanLookAtTarget = true;
        isWideBallChecked = false;
        minPickupDistance = 1000f;
        isBallReleased = false;
        ballReleaseProcessedThisDelivery = false;
        _outcomeCommittedThisDelivery = false;   // re-arm orphan detection for the next delivery
        _collectFiredThisDelivery = false;
        _collectGateReported = false;
        _bowlerStumpReviewHold = false;          // clear any leftover bowler stump-review board hold
        _stumpFollowerHoldStart = -1f;           // clear the stump-decision relay-wait for the next delivery
        isBallOverTheFence = false;
        // Reset ball position stream state for the new delivery
        _hasNetworkBallPosition = false;
        _networkBallPosition = Vector3.zero;
        _prevNetworkBallPosition = Vector3.zero;
        _hasPrevNetworkBallPosition = false;
        _remoteBowledAppliedThisDelivery = false;
        _remoteShotAppliedThisDelivery = false;
        _shotStreamSeeded = false;               // next delivery's first stream packet must fully re-seed
        _lockstepSwingArmed = false;             // lockstep: fresh swing state per delivery
        _lockstepContactResolved = false;
        _lockstepSendSwingNextFrame = false;
        _lockstepInputKnown = false;
        _lockstepCrossed = false;
        _lockstepCrossTick = 0;
        _lockstepSwingArmedTick = 0;
        _lockstepBatterX = 0f;
        _lockstepHasRelayedResult = false;   // new delivery: no stale relayed contact numbers
        _lockstepRelayedNoBatContact = false;   // ...and no stale non-bat ruling
        _lockedPlanMinted = false;           // locked delivery plan: fresh mint/arm per delivery
        // A plan that is armed but not yet LAUNCHED is a delivery this client has been promised and has not
        // yet played. Clearing it here is correct per-delivery housekeeping, but if it happens between the
        // arm and the bowler's release anim event the launch silently no-ops (TryLaunchFromLockedPlan just
        // returns false) and this client gets no ball at all while the opponent bowls — the 11-08 15:27 pair
        // shows exactly that shape: "PLAN armed" + "Batting run-up started" and then no LAUNCH. Name it when
        // it happens so the next capture separates this cause from a run-up that never reached its event.
        if (_lockedPlanArmed && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            ConstantsData_M.MpLog($"[Lockstep] ResetAll cleared a PENDING delivery plan before it launched (state={currentActionState} released={isBallReleased} status='{currentBallStatus}') — this side will not get that ball.");
        _lockedPlanArmed = false;
        if (bowlerAnimator != null) bowlerAnimator.speed = 1f;   // belt-and-braces: never leave the release-pose hold frozen across deliveries
        _contactAngleRelayedThisDelivery = false;   // #1: new delivery → allow the stream to seed the angle until RPC_ChangeBallAngle relays the contact angle
        // Deterministic post-shot (Phase 1): disarm for the new delivery so the legacy frame-based integrator
        // owns the run-up / pre-contact flight again; the next bat contact re-arms it (authority) / the next
        // RPC_SyncBallShot re-bases it (follower).
        _useDeterministicBallStep = false;
        _deterministicShotArmed = false;
        _ballSimTicksDone = 0;
        _ballSimMaxTargetTicks = -1;   // Issue #3: clear per-flight monotonic target high-water mark for the new delivery
        _shotSyncRecvCount = 0;            // [CamFollowDiag] per-ball post-shot stream receive counter
        _camFollowDiagTimer = 999f;        // force the first post-shot frame to log immediately
        _camFollowInit = false;            // [CamFollow] re-seed predictive cam onto the ball next delivery
        mpCaughtBehindOutPending = false;  // per-delivery clear: a non-flushed caught-behind defer can't leak a phantom wicket into a later ball
        mpLbwOutcomeCommitted = false;     // per-delivery clear for the online LBW appeal-mark outcome-commit
        _authoritativeCatchFielder = -1;   // clear the synced catcher for the new delivery
        _sentConfirmCatch = false;         // re-arm the once-per-delivery confirmed-catch relay
        _forceCatchConfirmed = false;      // clear the bowling-follower forced-catch poll
        _hasAuthoritativeCatchPoint = false;
        _hasAuthoritativeFielderSetup = false;
        // Per-delivery clear for the lockstep follower's stashed fielder setup — a leftover from the
        // previous ball must never place this ball's chasers.
        _hasStashedFielderSetup = false;
        _stashedFielderIndices = null;
        _stashedFielderActions = null;
        _stashedFielderChasePoints = null;
        _remoteContactPos = Vector3.zero;
        _hasRemoteContactPos = false;
        _relayedBoundaryRuns = -1;         // per-delivery clear: last ball's boundary verdict must not leak
        // Clear the caught-behind umpire decision for a genuinely NEW delivery so a previous ball's
        // decision can't leak on the bowling receiver before this ball's RPC lands. Guarded by
        // !isReplayModeActive because the replay/DRS system reuses ResetAll() (ShowReplay/ReviewReplay
        // set isReplayModeActive=true first) and the UltraEdge cutscene reads these during the replay.
        if (!isReplayModeActive)
        {
            umpireInitialDecision = string.Empty;
            hasEdgeOccurred = false;
            hasUltraEdgeDecision = false;
            canUserAskForReview = false;
            canAIAskForReview = false;
        }
        _ballStreamTimer = 0f;
        hasLBWAppeal = false;
        isLBW = false;
        isBallInline = false;
        CONTROLLER.LBWAPPEAL = false;
        CONTROLLER.BALLINLINE = false;
        CONTROLLER.Lbw = false;
        throwFirstBounceDistance = 0f;
        boundaryType = 6;
        isAutoBowlerActivationAllowed = false;
        isFielderAppealingRunOut = false;
        stump1Anim.Play("idle");
        stump2Anim.Play("idle");
        BoundaryBoardCollider.SetActive(false);
        _mainUmpireTransform.position = mainUmpireInitialPosition;
        //mainUmpireTransform.localScale = new Vector3(1f, mainUmpireTransform.localScale.y, mainUmpireTransform.localScale.z);
        _mainUmpireTransform.eulerAngles = new Vector3(_mainUmpireTransform.eulerAngles.x, 0f, _mainUmpireTransform.eulerAngles.z);
        mainUmpireAnim.Play("IdleGetReady");
        sideUmpireAnim.Play("Idle");
        umpireCamTransform.eulerAngles = new Vector3(10f, umpireCamTransform.eulerAngles.y, umpireCamTransform.eulerAngles.z);
        isBowlerActivationAllowed = false;
        fielderAction = string.Empty;
        fielder10Anim.Play("idle");
        fielder10SkinTransform.eulerAngles = new Vector3(0f, 0f, 0f);
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            //WicketKeeperAnimationComponent.Play("WCCLite_KeeperPreIdle01");
        }
        else
        {
        }
        keeperAnim.CrossFade("WCCLite_KeeperPreIdle02");
        WicketKeeperBallSkin.enabled = false;
        _wicketKeeperTransform.eulerAngles = new Vector3(0f, 180f, 0f);
        isWicketKeeperCatchingAnimationSelected = false;
        ResetFielders();
        if (!isReplayModeActive)
        {
            Singleton<BowlingControls>.instance.SpeedArrow.transform.localPosition = new Vector3(-57f, Singleton<BowlingControls>.instance.SpeedArrow.transform.localPosition.y, Singleton<BowlingControls>.instance.SpeedArrow.transform.localPosition.z);
            Singleton<BowlingControls>.instance.YelloFillMeter.fillAmount = 0f;
            ballSpinSpeedX = UnityEngine.Random.Range(-3600, -1800);
            ballSpinSpeedZ = UnityEngine.Random.Range(-3600, -1800);
            ballSpinSpeedXSaved = ballSpinSpeedX;
            ballSpinSpeedZSaved = ballSpinSpeedZ;
            isFullToss = false;
        }
        else
        {
            ballSpinSpeedX = ballSpinSpeedXSaved;
            ballSpinSpeedZ = ballSpinSpeedZSaved;
        }
        if (bowlerType == "fast")
        {
            fielder10SkinTransform.position = new Vector3(fielder10FastPosition.transform.position.x, 0f, -4.01f);
            if (bowlerAnimationNumber == 1)
            {
                //bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, -25.1f);
                bowlerObject.transform.position = new Vector3(bowlerObject.transform.position.x, bowlerObject.transform.position.y, -24.77f);

            }
            else if (bowlerAnimationNumber == 2)
            {
                //bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, -19.5f);
                bowlerObject.transform.position = new Vector3(bowlerObject.transform.position.x, bowlerObject.transform.position.y, -24.5f);

            }
            if (!isReplayModeActive)
            {
                ballSpinSpeedZ = UnityEngine.Random.Range(-500, 500);
                ballSpinSpeedZSaved = ballSpinSpeedZ;
            }
            else
            {
                ballSpinSpeedZ = ballSpinSpeedZSaved;
            }
            ballSpinSpeedZSaved = ballSpinSpeedZ;
            if (batsmanHand == "right")
            {
                _wicketKeeperTransform.position = wicketKeeperFastPositionForRHB;
            }
            else if (batsmanHand == "left")
            {
                _wicketKeeperTransform.position = wicketKeeperFastPositionForLHB;
            }
        }
        else if (bowlerType == "spin")
        {
            fielder10SkinTransform.position = new Vector3(fielder10SpinPosition.transform.position.x, 0f, -6.9f);
            if (bowlerAnimationNumber == 1)
            {
                //bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, -6f);
                bowlerObject.transform.position = new Vector3(bowlerObject.transform.position.x, bowlerObject.transform.position.y, -5.89f);

            }
            else if (bowlerAnimationNumber == 2)
            {
                //bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, -6f);
                bowlerObject.transform.position = new Vector3(bowlerObject.transform.position.x, bowlerObject.transform.position.y, -5.89f);

            }
            if (batsmanHand == "right")
            {
                _wicketKeeperTransform.position = wicketKeeperSpinPositionForRHB;
            }
            else if (batsmanHand == "left")
            {
                _wicketKeeperTransform.position = wicketKeeperSpinPositionForLHB;
            }
        }
        else if (bowlerType == "medium")
        {
            fielder10SkinTransform.position = new Vector3(fielder10SpinPosition.transform.position.x, 0f, -5.74f);
            if (bowlerAnimationNumber == 1)
            {
                //bowler.transform.position = new Vector3(0.91f, bowler.transform.position.y, -12.48f);
                bowlerObject.transform.position = new Vector3(0.91f, bowlerObject.transform.position.y, -12.04f);

            }
            else if (bowlerAnimationNumber == 2)
            {
                //bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, -10.5f);
                bowlerObject.transform.position = new Vector3(bowlerObject.transform.position.x, bowlerObject.transform.position.y, -12.04f);

            }
            if (batsmanHand == "right")
            {
                _wicketKeeperTransform.position = wicketKeeperFastPositionForRHB;
            }
            else if (batsmanHand == "left")
            {
                _wicketKeeperTransform.position = wicketKeeperFastPositionForLHB;
            }
        }
        matchBallTransform.position = initialBallPosition;
        ////ConstantsData_M.MpLog("* position change");
        temporaryPosition = matchBallTransform.position;
        matchBallTransform.eulerAngles = new Vector3(0f, 2f, 180f);
        if (!isReplayModeActive)
        {
            previousEdgeCatch = false;
            isPowerShotActive = false;
            noBallBowlerHeelPosition = 0f;
            isOversteppedDelivery = false;
            noBall = false;
            noBallRunStatus = string.Empty;
        }
        if (!isReplayModeActive && lastBowledBallType == "overstep" && isLineFreeHitActive)
        {
            ////ConstantsData_M.MpLog("SHOWFREEHIT");
            Singleton<Scoreboard>.instance.showFreeHitBg(canShow: true);
        }
        if (!isReplayModeActive && !isLineFreeHitActive)
        {
            ////ConstantsData_M.MpLog("HERE");
            CONTROLLER.isLineFreeHitBallCompleted = true;
        }
        if (!isReplayModeActive && isLineFreeHitActive)
        {
            ////ConstantsData_M.MpLog("HERE");

            CONTROLLER.isLineFreeHitBallCompleted = false;
        }
        ShowBall(status: false);
        if (batsmanHand == "right")
        {
            _batsmanTransform.position = rightHandedBatsmanInitialPosition;
            _batsmanTransform.localScale = new Vector3(1f, _batsmanTransform.localScale.y, _batsmanTransform.localScale.z);
            _batsmanTransform.eulerAngles = new Vector3(_batsmanTransform.eulerAngles.x, 270f, _batsmanTransform.eulerAngles.z);
            batsmanInitialXPosition = _batsmanTransform.position.x;
        }
        else if (batsmanHand == "left")
        {
            _batsmanTransform.position = leftHandedBatsmanInitialPosition;
            _batsmanTransform.localScale = new Vector3(-1f, _batsmanTransform.localScale.y, _batsmanTransform.localScale.z);
            _batsmanTransform.eulerAngles = new Vector3(_batsmanTransform.eulerAngles.x, 90f, _batsmanTransform.eulerAngles.z);
            batsmanInitialXPosition = _batsmanTransform.position.x;
        }
        batsmanAnim.Play("WCCLite_BatsmanIdle");
        attemptedSquareLegGlance = false;
        attemptedSquareCutDrive = false;
        if (bowlerHand == "right")
        {
            bowlerObject.transform.localScale = new Vector3(1f, bowlerObject.transform.localScale.y, bowlerObject.transform.localScale.z);
        }
        else if (bowlerHand == "left")
        {
            bowlerObject.transform.localScale = new Vector3(-1f, bowlerObject.transform.localScale.y, bowlerObject.transform.localScale.z);
        }
        //BowlerAnimationComponent.Play("WCCLite_BowlerIdle");
        //BowlerAnimatorComponent.SetFloat("SpeedMultiplier", 1f);

        bowlerAnimator.Play("WCCLite_BowlerIdle");
        BowlerBallSkinRenderer.enabled = true;
        isBowlingInterfaceHidden = false;
        canUserBowlerMoveBowlingSpot = false;
        isUserBowlingSpotSelected = false;
        currentBallStatus = string.Empty;
        isShotAllowed = false;
        batsmanTriggeredAttemptedShot = false;
        batsmanCompletedShot = false;
        // Online batter's self-echo guard (see ShotSelected): if the echo never came back for the previous
        // delivery the flag must not swallow the next one.
        _shotSelectionArmedLocally = false;
        _ballAngleRPCSent = false;
        if (_ballAngleRPCCoroutine != null) { StopCoroutine(_ballAngleRPCCoroutine); _ballAngleRPCCoroutine = null; }
        canBatsmanMoveLaterally = false;
        isBatsmanMovingLaterally = false;
        isPowerKeyPressed = false;
        isBallOnBoundaryLine = false;
        isWicketKeeperActive = false;
        currentWicketKeeperStatus = string.Empty;
        shouldStopFielders = false;
        _runnerTransform.position = runnerInitialPosition;
        runnerInitialPosition = _runnerTransform.position;
        runnerAnim.Play("WCCLite_RunnerIdle");
        runnerAnim["WCCLite_RunnerIdle"].speed = 0.5f;
        _runnerTransform.eulerAngles = new Vector3(_runnerTransform.eulerAngles.x, 180f, _runnerTransform.eulerAngles.z);
        strikerCurrentStatus = "idle";
        nonStrikerCurrentStatus = "idle";
        isRunBeingTaken = false;
        hasRunOutOccurred = false;
        _runOutSignalPlayed = -1;      // new delivery: no umpire signal played, no relayed verdict yet
        _runOutSignalDeferred = false;
        if (_runOutSignalTimeoutCo != null) { StopCoroutine(_runOutSignalTimeoutCo); _runOutSignalTimeoutCo = null; }
        _relayedRunOutVerdict = -1;
        isRunOutOccurring = false;
        currentBoundaryAction = string.Empty;
        isPlayerStumped = false;
        wasPlayerStumped = false;
        isMainCameraInTopDownView = false;
        gameplayCamera.fieldOfView = 8f;
        mainCamTransform.eulerAngles = new Vector3(7.5f, mainCamTransform.eulerAngles.y, mainCamTransform.eulerAngles.z);
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        showPreviewCamera(status: false);
        umpireViewCamera.enabled = false;
        closeUpViewCamera.enabled = false;
        isSideCameraSelected = false;
        if (CONTROLLER.cameraType == 1)
        {
            rightCamTransform.position = new Vector3(-28f, 7.5f, 0f);
            leftCamTransform.position = new Vector3(28f, 7.5f, 0f);
        }
        if (CONTROLLER.cameraType == 0)
        {
            rightFieldCamera.fieldOfView = 50f;
            leftFieldCamera.fieldOfView = 50f;
        }
        else
        {
            rightFieldCamera.fieldOfView = 50f;
            leftFieldCamera.fieldOfView = 50f;
        }
        // Brackets the CamDiag reading at shot time: this is the per-delivery reset to 50, so if CamDiag then
        // reports ~30-38 at side-camera selection, something between the two drove it back down and the shot
        // starts already zoomed in (tester: "camera bht close ho jata"). If this line is MISSING for a
        // delivery, the reset never ran and the previous ball's zoom simply carried over.
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            ConstantsData_M.MpLog($"[CamDiag] ResetAll: side-camera FOV reset to left={leftFieldCamera.fieldOfView:F1} right={rightFieldCamera.fieldOfView:F1} (cameraType={CONTROLLER.cameraType}).");
        isCameraFocusedOnKeeper = false;
        isUpArrowKeyPressed = false;
        isDownArrowKeyPressed = false;
        isLeftArrowKeyPressed = false;
        isRightArrowKeyPressed = false;
        ShowBowler(showStatus: true);
        ShowFielder10(fielder10Status: false, ball10Status: false);
        bowlerZoomCameraStartTime = -1f;
        if (shouldPlayIntro)
        {
            currentActionState = -1;
            gameplayCamera.enabled = false;
            InitCamera();
        }
        else
        {
            currentActionState = -2;
            InitCamera();
            bowlerZoomCameraStartTime = Time.time;
            FielderExtraActions();
            gameplayCamera.enabled = true;
            introCutsceneCamera.enabled = false;
            StartCoroutine(FieldersRandomWarmUpAnimation());
        }
        SetBowlerSide();
        resetUltraMotionVariables();
        matchBall.GetComponent<Rigidbody>().Sleep();
        isWicketKeeperAtStump = false;
        UpdateShadowsAndPreview();
        CONTROLLER.CURRENTCOLLIDER = string.Empty;
    }

    private void resetUltraMotionVariables()
    {
        slowMotionCamera.enabled = false;
        difference = 0;
        stopKeeper = false;
        if (batsmanHand == "left")
        {
            ultraMotionCameraTransform.position = new Vector3(9f, 2f, 9f);
            ultraMotionCameraTransform.eulerAngles = new Vector3(7f, -90f, 0f);
        }
        else
        {
            ultraMotionCameraTransform.position = new Vector3(-9f, 2f, 9f);
            ultraMotionCameraTransform.eulerAngles = new Vector3(7f, 90f, 0f);
        }
    }

    public void AutoplaySettings()
    {
        if (CONTROLLER.isFromAutoPlay)
        {
            ResetAll();
            Singleton<Scoreboard>.instance.freeHitGO.SetActive(value: false);
            if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
            {
                Singleton<PreviewScreen>.instance.hideBtns(boolean: false);
            }
            HideBowlingSpot();
            ShowFullTossSpot(_Value: false);
            pauseCountdown();
            ActivateStadiumAndSkybox(boolean: true);
            CONTROLLER.isFromAutoPlay = false;
        }
    }

    public void NewInnings()
    {
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8)
        {
            CONTROLLER.CanLoadGround = false;
            CONTROLLER.CanBowlerBowl = false;
            Singleton<BowlingScoreCard>.instance.ContinueBtn.gameObject.GetComponent<Button>().interactable = false;
        }
        else if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected == 8)
        {
            CONTROLLER.CanLoadGround = false;
            CONTROLLER.CanBowlerBowl = false;
            Singleton<BowlingScoreCard>.instance.ContinueBtn.gameObject.GetComponent<Button>().interactable = true;
        }

        AutoplaySettings();
        SetDefaultDigitalDisplayContent();
        EnableFielders(boolean: true);
        setFieldersPosition();
        isNewInning = true;
        InitCamera();
        ActivateColliders(boolean: false);
        replayViewCamera.enabled = false;
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        bowlerZoomCameraStartTime = -1f;
        isBatsmanConfident = CONTROLLER.isConfidenceLevel;
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
        {
            isBatsmanConfident = false;
        }
        else
        {
            isBatsmanConfident = true;
            if (!(CONTROLLER.difficultyMode == "hard"))
            {
            }
        }
        // Online MP: NEVER random-pick the side — both clients must deterministically agree from the first
        // ball. Online is forced to "left" below; this random pick is offline/AI variety only. (Tester: bowler
        // side not synced at game start — the bowling client could random-pick "right" while the batting client
        // showed "left", and the start-up role/reconnect race could skip the left-force, leaving them split.)
        if (CONTROLLER.PlayModeSelected != 8)
        {
            if (UnityEngine.Random.Range(0f, 10f) <= 5f)
            {
                bowlerSide = "left";
            }
            else
            {
                bowlerSide = "right";
            }
        }
        // Issue #3(B): during a reconnect restore, NewInnings must NOT force bowlerSide="left" nor
        // re-push it — the restore (RestartBowlerSide from syncedBowlerSide) is authoritative and
        // runs AFTER this. Forcing/re-pushing here clobbered the real over's side, flipping the
        // bowler's end on a 1st-innings reconnect (tester issue #3).
        bool reconnectRestorePending = (CricketNetworkManager.RestorePending
            || (Launcher.Instance != null && Launcher.Instance.IsReConnecting()));
        if (CONTROLLER.PlayModeSelected == 8 && !reconnectRestorePending)
        {
            // Both players share the batting-end camera (no Y flip), so world-space left/right
            // is the same for everyone. Default to left (standard over-the-wicket delivery).
            // The bowling player can change this mid-over via the UI; that change is synced
            // to the batting player by CmdSyncBowlerSide below.
            bowlerSide = "left";
            // Game-start side desync root: CONTROLLER.BowlerSide is a STATIC that survives the previous
            // match/innings. GameData.NewGame calls RestartBowlerSide(CONTROLLER.BowlerSide) right AFTER
            // this force — a stale "right" there overwrote the forced left on ONE client only (the other,
            // fresh-launched, stayed left) → bowler/non-striker on opposite ends at ball one. Pin the
            // static here so that restart re-applies the same deterministic LEFT on both clients.
            CONTROLLER.BowlerSide = "left";
        }
        // Multiplayer: bowling player is authoritative for bowlerSide; sync to other player.
        if (GameConstants.isWithAI == false && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && CricketNetworkManager.instance != null && !reconnectRestorePending)
        {
            CricketNetworkManager.instance.CmdSyncBowlerSide(staticVariables.UserProfiledata.user._id, bowlerSide);
        }
        if (UnityEngine.Random.Range(0f, 10f) < 5f)
        {
            umpireMainIndex = 0;
            umpireSideIndex = 1;
        }
        else
        {
            umpireMainIndex = 1;
            umpireSideIndex = 0;
        }
        currentBattingTeam = TrimSpaces(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].teamName);
        currentBowlingTeam = TrimSpaces(CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].teamName);
        CONTROLLER.battingTeamUniform = currentBattingTeam;
        CONTROLLER.bowlingTeamUniform = currentBowlingTeam;
        InitializeEnvironment();

        for (int i = 1; i < 10; i++)
        {
            var Fielder = fielderModels[i].GetComponent<Renderer>();
            Fielder.materials[0].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BowlingTeamIndex]);
            Fielder.materials[1].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BowlingTeamIndex]);
            Fielder.materials[2].SetColor("_Color", teamSkinColors[CONTROLLER.BowlingTeamIndex]);
            fielderCaps[i].materials[0].SetTexture("_MainTex", teamCapMaterials[CONTROLLER.BowlingTeamIndex]);
        }
        fielder10SkinRenderer.materials[0].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BowlingTeamIndex]);
        fielder10SkinRenderer.materials[1].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BowlingTeamIndex]);
        fielder10SkinRenderer.materials[2].SetColor("_Color", teamSkinColors[CONTROLLER.BowlingTeamIndex]);
        //wicket Keeper
        WicketKeeperSkinRenderer.materials[0].SetTexture("_MainTex", teamKitMaterials[CONTROLLER.BowlingTeamIndex]);
        WicketKeeperSkinRenderer.materials[1].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BowlingTeamIndex]);
        WicketKeeperSkinRenderer.materials[2].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BowlingTeamIndex]);
        WicketKeeperSkinRenderer.materials[3].SetColor("_Color", teamSkinColors[CONTROLLER.BowlingTeamIndex]);
        //Bowler
        BowlerSkinRenderer.materials[0].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BowlingTeamIndex]);
        BowlerSkinRenderer.materials[1].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BowlingTeamIndex]);
        BowlerSkinRenderer.materials[2].SetColor("_Color", teamSkinColors[CONTROLLER.BowlingTeamIndex]);
        //batsman
        batsmanSkinRenderer.materials[0].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BattingTeamIndex]);
        batsmanSkinRenderer.materials[1].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BattingTeamIndex]);
        batsmanSkinRenderer.materials[2].SetColor("_Color", teamSkinColors[CONTROLLER.BattingTeamIndex]);
        BatsmanCricketKitSkinRenderer.materials[0].SetTexture("_MainTex", teamKitMaterials[CONTROLLER.BattingTeamIndex]);
        //Runner 
        RunnerSkinRenderer.materials[0].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BattingTeamIndex]);
        RunnerSkinRenderer.materials[1].SetTexture("_MainTex", teamUniformMaterials[CONTROLLER.BattingTeamIndex]);
        RunnerSkinRenderer.materials[2].SetColor("_Color", teamSkinColors[CONTROLLER.BattingTeamIndex]);
        RunnerCricketKitSkinRenderer.materials[0].SetTexture("_MainTex", teamKitMaterials[CONTROLLER.BattingTeamIndex]);


        showPreviewCamera(status: false);
        fielder10Anim.Play("idle");
        mainUmpireAnim.Play("Idle");
        sideUmpireAnim.Play("Idle");
        stump1Anim.Play("idle");
        stump2Anim.Play("idle");
        keeperAnim.Play("idle");
        batsmanAnim.Play("WCCLite_BatsmanIdle");
        runnerAnim.Play("WCCLite_RunnerIdle");
        //BowlerAnimationComponent.Play("Blitz_" + currentBowlerType + "Idle");
        //BowlerAnimatorComponent.SetFloat("SpeedMultiplier", 1f);

        bowlerAnimator.Play("Blitz_" + bowlerType + "Idle");
        ShowBall(status: false);
        isReplayModeActive = false;
        UpdateShadowsAndPreview();
    }

    public string TrimSpaces(string teamName)
    {
        string text = string.Empty;
        string[] array = teamName.Split(" "[0]);
        for (int i = 0; i < array.Length; i++)
        {
            text += array[i];
        }
        return text;
    }

    private void AiFieldScan()
    {
        aiFielderScanList.Clear();
        for (int i = 1; i <= numberOfFielders; i++)
        {
            if (DistanceBetweenTwoVector2(groundCenterMarker, fielders[i]) < 45f)
            {
                aiFielderScanList.Add(fielders[i]);
            }
        }
    }

    // Bowling-follower CATCH-divergence corrector. Called from GameData when the AUTHORITATIVE network outcome
    // says CAUGHT (wicketType==3) on an OUTFIELD catch. Sometimes the catch-fielder sync (RPC_SetCatchFielder)
    // lands AFTER our local deterministic ball already bounced near the fielder, so the fielder picks it up
    // (currentBallStatus="throw") and throws it back — the ball lies on the ground + the camera chases the
    // throw-back (jerk), even though the score is already correct via RpcBallOutcome. This forces our visual to
    // match the authoritative catch: stop the ball + hide it + settle it in the catcher's hand. Only fires when
    // a real outfield catcher was synced (_authoritativeCatchFielder>=0), so keeper caught-behind / reviewed
    // dismissals are untouched. No-ops when our local sim already caught it (outcomeOfBall=="wicket").
    // isBallPaused/outcomeOfBall/_authoritativeCatchFielder are all reset per delivery in ResetAll → no leak,
    // and a normal catch sets the same two flags (see ~5764-5767), so this mirrors proven behaviour.
    public void ForceFollowerCatchVisualOnDivergence()
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return;
        int cf = _authoritativeCatchFielder;
        if (cf < 0 || fielders == null || cf >= fielders.Length || fielders[cf] == null) return; // outfield catch only
        if (outcomeOfBall == "wicket") return;          // local sim already caught it — nothing to correct (common case)
        // Only correct a GENUINE divergence — the ball has gone to a ground-field / throw-back. If the local sim
        // is still on the catch path (ball flying toward the catcher), do NOT pre-empt it with an abrupt hide;
        // let the normal local catch play. The per-delivery poll re-checks each frame, so this fires the instant
        // the ball actually diverges (≈no visible jerk).
        bool diverged = (currentBallStatus == "throw");
        if (!diverged && activeFielders != null && activeFieldersActions != null)
        {
            int slot = activeFielders.IndexOf(cf);
            if (slot >= 0 && slot < activeFieldersActions.Count)
            {
                string act = activeFieldersActions[slot];
                diverged = (act == "throw" || act == "pickedup" || act == "pickeup"
                    || act == "stopChasing" || act == "end" || act == "goToBoundary" || act == "goingToBoundary");
            }
        }
        if (!diverged) return;                           // still on the catch path — leave the local catch alone
        outcomeOfBall = "wicket";                        // halt the ground-field / throw-back state machine
        isBallPaused = true;                             // freeze ball movement (throw path is gated on !isBallPaused)
        ShowBall(status: false);                         // hide the rolling/thrown ball so it doesn't lie on the ground
        if (fielderBalls != null && cf < fielderBalls.Length && fielderBalls[cf] != null)
        {
            Renderer r = fielderBalls[cf].GetComponent<Renderer>();
            if (r != null) r.enabled = true;            // show the ball in the catcher's hand
        }
        if (fieldersAnim != null && cf < fieldersAnim.Length && fieldersAnim[cf] != null)
        {
            fieldersAnim[cf].Play("highCatch");         // brief catch pose on the authoritative catcher
        }
        ConstantsData_M.MpLog("[CatchSync] Bowling follower diverged to ground-field on an authoritative outfield CATCH — forced catch visual (ball hidden, catcher settled).");
    }

    // Confirmed-catch relay receiver (bowling follower). Fired the instant the batting authority completes a CLEAN
    // outfield catch (CmdConfirmOutfieldCatch) — EARLIER than the authoritative RpcBallOutcome. Resolves our local
    // ball as that catch now, so the follower's ball doesn't keep falling/throwing-back + the camera stops chasing
    // it. Reuses the same correction as the outcome-time backstop (ForceFollowerCatchVisualOnDivergence), which
    // no-ops if our local sim already caught it. Only sent on a real catch, so there's no dropped-catch risk.
    public void OnConfirmedOutfieldCatch(int catcherIndex)
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return;
        // Stale-relay guard (reconnect phantom catch, tester #4): a catch relay for a CANCELLED ball (the
        // opponent disconnected mid-flight; the server delivers the buffered Cmd to this rebuilt connection
        // AFTER the restore) must not latch catch state with no ball in flight — it replayed a full catch-out
        // scene on the NEXT ball. Only accept while a delivery is actually live.
        if (!isBallReleased || isReplayModeActive) return;
        if (catcherIndex >= 0) _authoritativeCatchFielder = catcherIndex; // ensure the catcher is known for the correction
        // Lockstep: the batter's confirm is REAL-TIME while this side's ball runs the flight delay behind —
        // arming the force immediately hid the ball right off the bat (13-07 log: "forced catch visual" on
        // the resolve frame, before the local flight even played). Re-time to OUR ball's equivalent moment;
        // the local fielder usually completes the same catch himself by then and the guard no-ops.
        if (LockstepActive && ConstantsData_M.lockstepFollowerFlightDelay > 0f)
        {
            StartCoroutine(ArmConfirmedCatchOnMyTimeline());
            return;
        }
        _forceCatchConfirmed = true;       // arm the per-frame poll (the divergence usually appears a few frames later)
        ForceFollowerCatchVisualOnDivergence(); // and correct immediately if it has ALREADY diverged
    }

    private IEnumerator ArmConfirmedCatchOnMyTimeline()
    {
        float wait = ConstantsData_M.lockstepFollowerFlightDelay - (float)(Mirror.NetworkTime.rtt / 2.0);
        if (wait > 0f)
        {
            double until = Mirror.NetworkTime.time + wait;
            while (Mirror.NetworkTime.time < until && isBallReleased && !isReplayModeActive)
            {
                yield return null;
            }
        }
        if (!isBallReleased || isReplayModeActive) yield break;   // delivery reset while waiting — stale confirm
        _forceCatchConfirmed = true;                              // per-frame poll + diverged-guard take it from here
        ForceFollowerCatchVisualOnDivergence();
    }

    public bool IsPlaying(string animationName)
    {
        if (bowlerAnimator == null)
        {
            ConstantsData_M.Log("Animator component not assigned!");
            return false;
        }

        AnimatorStateInfo currentState = bowlerAnimator.GetCurrentAnimatorStateInfo(0);
        return currentState.IsName(animationName) &&
               currentState.normalizedTime >= 0f && currentState.normalizedTime < 1.0f;
    }

    // Receiver for the batting authority's 4-vs-6 verdict (see the rope-cross relay above). Runs on
    // every client via ClientRpc; the sender recognises its own echo by senderId and ignores it.
    public void OnBoundaryVerdictRelayed(int senderId, int runs, bool hitBat)
    {
        if (staticVariables.UserProfiledata != null && staticVariables.UserProfiledata.user != null
            && staticVariables.UserProfiledata.user._id == senderId)
            return;   // own echo
        _relayedBoundaryRuns = runs;
        _relayedBoundaryHitBat = hitBat;
        ConstantsData_M.MpLog($"[GroundController][BoundaryVerdict] Received authority verdict: runs={runs} hitBat={hitBat} (local guess runs={runsScoredThisBall}).");
    }

    // Receiver for the batting authority's throw-target ruling (keeper end vs bowler end).
    //
    // The choice used to be made independently on each client from two purely LOCAL inputs — which end
    // the gathering fielder is nearer to, and fielderAction — so the two screens routinely disagreed:
    // one side threw to the bowler while the other threw to the keeper, and the bowler correspondingly
    // either walked in to receive or stayed at the pitch. That is the same divergence seen from both
    // ends of the pitch, and neither input is synchronised, so it could never converge on its own.
    //
    // Now the batting side rules once, at the moment its fielder starts the throw animation, and the
    // bowling side adopts it — deciding locally only if the ruling has not arrived yet, exactly like
    // the 4-vs-6 boundary verdict above.
    // Receiver for the batting authority's run-out verdict. The signal may already have been played from
    // this client's own (possibly different) call — in that case correct it now rather than leave the two
    // screens showing opposite umpire decisions, exactly as OnThrowTargetRelayed corrects a wrong throw.
    // Reconcile the umpire's run-out signal against the AUTHORITATIVE outcome (the score), called from
    // GameData.UpdateCurrentBall on both sides once the delivery's wicket state is committed.
    //
    // The verdict relay alone was not enough. It is one-directional — the batting side sends and never
    // receives — so when the BATTING side's own call was wrong nothing corrected it, and the 13-08 19:04
    // pair shows exactly that: batting played NOT OUT (hasRunOutOccurred=False) on a delivery the bowling
    // side signalled OUT, and the two sides did not even see the same NUMBER of run-out events (5 vs 4),
    // so a verdict from one event could be read against another. The score is the one thing both sides
    // already agree on, so the SIGNAL is now made to match it rather than any client's local judgement.
    // Fallback for a held signal whose verdict never arrives — the umpire must not stay silent.
    private System.Collections.IEnumerator PlayRunOutSignalIfVerdictNeverArrives(bool localCall)
    {
        float _waited = 0f;
        while (_waited < RUNOUT_VERDICT_WAIT && _runOutSignalDeferred)
        { _waited += Time.unscaledDeltaTime; yield return null; }
        _runOutSignalTimeoutCo = null;
        if (!_runOutSignalDeferred) yield break;   // the verdict arrived and played it
        _runOutSignalDeferred = false;
        _runOutSignalPlayed = localCall ? 1 : 0;
        ConstantsData_M.MpLog($"[RunOutDiag] No verdict after {_waited:F1}s — falling back to this client's own call ({(localCall ? "OUT" : "NOT OUT")}).");
        if (mainUmpireAnim == null) yield break;
        if (localCall) mainUmpireAnim.Play("Out2_New");
        else mainUmpireAnim.CrossFade("Crouch_toNotOut_New");
    }

    public void ReconcileRunOutSignalWithOutcome(bool isRunOutWicket)
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return;
        if (_runOutSignalPlayed < 0) return;                      // no run-out signal played this delivery
        int _want = isRunOutWicket ? 1 : 0;
        if (_runOutSignalPlayed == _want) return;                 // already matches the score
        ConstantsData_M.MpLog($"[RunOutDiag] Umpire signal did not match the authoritative outcome — showed {(_runOutSignalPlayed == 1 ? "OUT" : "NOT OUT")}, score says {(isRunOutWicket ? "OUT" : "NOT OUT")}; correcting.");
        _runOutSignalPlayed = _want;
        _relayedRunOutVerdict = _want;
        hasRunOutOccurred = isRunOutWicket;
        if (mainUmpireAnim == null) return;
        if (isRunOutWicket) mainUmpireAnim.Play("Out2_New");
        else mainUmpireAnim.CrossFade("Crouch_toNotOut_New");
    }

    public void OnRunOutVerdictRelayed(int senderId, bool isOut)
    {
        if (staticVariables.UserProfiledata != null && staticVariables.UserProfiledata.user != null
            && staticVariables.UserProfiledata.user._id == senderId)
            return;   // own echo
        int _want = isOut ? 1 : 0;
        _relayedRunOutVerdict = _want;
        // The signal was held for exactly this: play the authority's decision now, first time, rather than
        // showing a guess and replacing it.
        if (_runOutSignalDeferred)
        {
            _runOutSignalDeferred = false;
            if (_runOutSignalTimeoutCo != null) { StopCoroutine(_runOutSignalTimeoutCo); _runOutSignalTimeoutCo = null; }
            _runOutSignalPlayed = _want;
            hasRunOutOccurred = isOut;
            ConstantsData_M.MpLog($"[RunOutDiag] Verdict arrived — playing the umpire signal once, as {(isOut ? "OUT" : "NOT OUT")}.");
            if (mainUmpireAnim != null)
            {
                if (isOut) mainUmpireAnim.Play("Out2_New");
                else mainUmpireAnim.CrossFade("Crouch_toNotOut_New");
            }
            return;
        }
        if (_runOutSignalPlayed < 0 || _runOutSignalPlayed == _want) return;   // nothing played yet, or already agrees
        ConstantsData_M.MpLog($"[RunOutDiag] Umpire signal corrected from the batting authority: local={( _runOutSignalPlayed == 1 ? "OUT" : "NOT OUT")} -> relayed={(isOut ? "OUT" : "NOT OUT")}.");
        _runOutSignalPlayed = _want;
        if (mainUmpireAnim == null) return;
        if (isOut) mainUmpireAnim.Play("Out2_New");
        else mainUmpireAnim.CrossFade("Crouch_toNotOut_New");
    }

    public void OnThrowTargetRelayed(int senderId, string target)
    {
        if (staticVariables.UserProfiledata != null && staticVariables.UserProfiledata.user != null
            && staticVariables.UserProfiledata.user._id == senderId)
            return;   // own echo
        if (string.IsNullOrEmpty(target)) return;
        _relayedThrowTarget = target;
        if (throwTargetSaved == target) return;

        // Already guessed, and guessed differently — correct it now rather than let the two screens
        // play out different throws. Both objects are plain scene refs; re-pointing throwTarget is what
        // the local decision would have done anyway.
        if (throwTargetSaved != string.Empty)
            ConstantsData_M.MpLog($"[GroundController][ThrowTarget] Authority ruled '{target}' but this side had already chosen '{throwTargetSaved}' — correcting.");
        throwTargetSaved = target;
        throwTarget = (target == "Fielder10") ? fielder10Object : wicketKeeperObject;
    }

    public float strikerZPos()
    {
        // Called ONLY from the catch (wicketType 3) branch of GameData.WicketBall. batsmanObject is a
        // serialized scene ref that is null on a client where the batsman rig isn't live at the moment
        // the caught-out outcome is processed (bowling side / mid catch-swap / just after a reconnect
        // scene reload) — dereferencing it there NREs and freezes both players on a catch out (reports
        // ab189747 / d795287e / 6f6f70e1, "game stuck on catch out"). Return 0 when unavailable: the
        // caller reads only sign(z), and >= 0 gives the common "new batsman takes strike" outcome.
        if (batsmanObject == null)
        {
            // One-shot: this is the catch-out freeze guard. Silent until now, so a tester log could never
            // show whether it had actually saved a delivery.
            if (!_strikerZPosNullLogged)
            {
                _strikerZPosNullLogged = true;
                ConstantsData_M.MpLog("[GroundController][strikerZPos] batsmanObject was NULL on a catch — returning 0 instead of throwing (this is the catch-out freeze guard).");
            }
            return 0f;
        }
        return batsmanObject.transform.position.z;
    }

    [Skip]
    private void stopITween(GameObject FielderGO)
    {
        iTween.Stop(FielderGO);
    }

    private void setChasePoint(int val)
    {
        GameObject gameObject = fielders[val];
        GameObject gameObject2 = fielderChasePoints[val];
        float num = DistanceBetweenTwoGameObjects(gameObject, gameObject2);
        float num2 = num / defaultFielderSpeed;
        float num3 = DistanceBetweenTwoGameObjects(matchBall, gameObject) / horizontalVelocity;
        if (fielderChasePointsSet[val])
        {
            float num4 = fielderAngleDifferencesToBall[val];
            float num5 = fielderDistances[val];
            float f = Mathf.Sin(num4 * degToRad) * num5;
            float num6 = Mathf.Sqrt(Mathf.Pow(fielderDistances[val], 2f) - Mathf.Pow(f, 2f));
            float x = ballStartPoint.position.x + num6 * Mathf.Cos(_ballAngle * degToRad);
            float z = ballStartPoint.position.z + num6 * Mathf.Sin(_ballAngle * degToRad);
            gameObject2.transform.position = new Vector3(x, gameObject2.transform.position.y, z);
            fielderChasePointsSet[val] = false;
        }
        if (num2 <= num3 && DistanceBetweenTwoGameObjects(gameObject2, matchBall) > 1f)
        {
            float x = Mathf.Cos(_ballAngle * degToRad) * (1.05f * (num3 / num2) * Time.deltaTime);
            float z = Mathf.Sin(_ballAngle * degToRad) * (1.05f * (num3 / num2) * Time.deltaTime);
            gameObject2.transform.position -= new Vector3(x, 0f, z);
        }
        else if (num2 > num3 && DistanceBetweenTwoGameObjects(gameObject2, matchBall) > 1f)
        {
            float x = Mathf.Cos(_ballAngle * degToRad) * (3.85f * (num3 / num2) * Time.deltaTime);
            float z = Mathf.Sin(_ballAngle * degToRad) * (3.85f * (num3 / num2) * Time.deltaTime);
            gameObject2.transform.position += new Vector3(x, 0f, z);
        }
        else
        {
            //gameObject2.transform.position = new Vector3(ballTransform.position.x, gameObject2.transform.position.y, ballTransform.position.z);
            gameObject2.transform.position = new Vector3(temporaryPosition.x, gameObject2.transform.position.y, temporaryPosition.z);
        }
    }

    private float DistanceBetweenTwoVector2(GameObject go1, GameObject go2)
    {
        float num = go1.transform.position.x - go2.transform.position.x;
        float num2 = go1.transform.position.z - go2.transform.position.z;
        return Mathf.Sqrt(num * num + num2 * num2);
    }

    private float DistanceBetweenTwoGameObjects(GameObject go1, GameObject go2)
    {
        return Vector3.Distance(go1.transform.position, go2.transform.position);
    }

    private float AngleBetweenTwoGameObjects(GameObject go1, GameObject go2)
    {
        float y = go1.transform.position.x - go2.transform.position.x;
        float x = go1.transform.position.z - go2.transform.position.z;
        float num = Mathf.Atan2(y, x) * radToDeg;
        return (270f - num + 360f) % 360f;
    }

    public float AngleBetweenTwoVector3(Vector3 v1, Vector3 v2)
    {
        float y = v1.x - v2.x;
        float x = v1.z - v2.z;
        float num = Mathf.Atan2(y, x) * radToDeg;
        return (270f - num + 360f) % 360f;
    }

    public void EnablePauseCountDown()
    {
        // Singleton<Scoreboard>.instance.pauseBtn.enabled = false;
        resumeGameObject.SetActive(value: true);
        DG.Tweening.Sequence sequence = DOTween.Sequence();
        sequence.Append(countdownThreeText.transform.DOScale(new Vector3(1f, 1f, 1f), 0.8f));
        sequence.Insert(0.8f, countdownThreeText.DOFade(0f, 0.2f));
        sequence.SetUpdate(isIndependentUpdate: true);
        sequence.OnComplete(EnablePauseCountDown2);
        canUserBowlerMoveBowlingSpot = false;
        Singleton<GameData>.instance.HideUIMenu(hide: true);
    }

    private void EnablePauseCountDown2()
    {
        DG.Tweening.Sequence sequence = DOTween.Sequence();
        sequence.Insert(0f, countdownTwoText.transform.DOScale(new Vector3(1f, 1f, 1f), 0.8f));
        sequence.Insert(0.8f, countdownTwoText.DOFade(0f, 0.2f));
        sequence.SetUpdate(isIndependentUpdate: true);
        sequence.OnComplete(EnablePauseCountDown3);
    }

    private void EnablePauseCountDown3()
    {
        DG.Tweening.Sequence sequence = DOTween.Sequence();
        sequence.Insert(0f, countdownOneText.transform.DOScale(new Vector3(1f, 1f, 1f), 0.8f));
        sequence.Insert(0.8f, countdownOneText.DOFade(0f, 0.2f));
        sequence.SetUpdate(isIndependentUpdate: true);
        sequence.OnComplete(pauseCountdown);
    }

    public void destroyCountDown()
    {
        Singleton<Scoreboard>.instance.pauseBtn.enabled = true;
        resumeGameObject.SetActive(value: false);
        DG.Tweening.Sequence sequence = DOTween.Sequence();
        sequence.Append(countdownThreeText.transform.DOScale(new Vector3(0f, 0f, 1f), 0f));
        sequence.Insert(0f, countdownTwoText.transform.DOScale(new Vector3(0f, 0f, 1f), 0f));
        sequence.Insert(0f, countdownOneText.transform.DOScale(new Vector3(0f, 0f, 1f), 0f));
        sequence.Insert(0f, countdownThreeText.DOFade(1f, 0f));
        sequence.Insert(0f, countdownTwoText.DOFade(1f, 0f));
        sequence.Insert(0f, countdownOneText.DOFade(1f, 0f));
        sequence.SetUpdate(isIndependentUpdate: true);
        canUserBowlerMoveBowlingSpot = true;
        Singleton<GameData>.instance.HideUIMenu(hide: false);
    }

    // Bowling client: apply a relayed BAT contact (shot) immediately + once per delivery, bypassing the
    // latency-fragile z-window. Either relay can arrive first — the collider relay (RPC_SetMultiplayerCollider)
    // or the shot-angle relay (RPC_ChangeBallAngle) — so BOTH call this; whichever sets the last required
    // piece (CURRENTCOLLIDER or BALLANGLE) triggers the apply. The launch ORIGIN is snapped to the batting
    // contact point (relayed Vector3) because BALLANGLE/HORIZONTALSPEED define the shot shape but not the
    // world origin that ballStartPoint / temporaryPosition / fielding & catch geometry depend on.
    // OnCustomTriggerEnter(BatCollider) itself transitions currentBallStatus to "shotSuccess", so callers
    // must NOT stamp the status before this runs (its bat branch is gated on currentBallStatus=="bowling").
    private bool TryApplyRemoteBatContact()
    {
        if (CONTROLLER.PlayModeSelected != 8) return false;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return false;
        if (currentBallStatus != "bowling") return false;       // delivery already resolved
        if (_remoteShotAppliedThisDelivery) return false;       // idempotent — re-broadcasts ignored
        string col = CONTROLLER.CURRENTCOLLIDER;
        if (col != "BatCollider" && col != "BatCollider2") return false; // collider relay not here yet (or not a bat)
        if (CONTROLLER.BALLANGLE == 0f || CONTROLLER.HORIZONTALSPEED == 0f) return false; // angle relay not here yet

        _remoteShotAppliedThisDelivery = true;

        // Snap the launch origin to the authoritative batting contact point so the shot, fielders and
        // catch geometry start from the exact spot the batter hit it (option B from the design review).
        if (_hasRemoteContactPos)
        {
            matchBallTransform.position = _remoteContactPos;
            temporaryPosition = _remoteContactPos;
            raycastAnchorBallTransform.position = _remoteContactPos;
        }

        col.Show(); // GameObject.Find only returns ACTIVE objects, so activate the bat collider first
        GameObject batColObj = GameObject.Find(col);
        if (batColObj != null)
        {
            Collider batCol = batColObj.GetComponent<Collider>();
            if (batCol != null)
            {
                OnCustomTriggerEnter(batCol);
            }
        }
        return true;
    }

    //Photon Removal [PunRPC]
    public void RPC_SetMultiplayerCollider(string Other, float SavedBallRayCastConnectedZposition)
    {
        // Clean-bowled relay is already authority-gated on the batting client (IsAuthoritativeLineInlineWithStumps).
        // A genuine bowled is detected near the very end of flight, so by the time this reliable RPC reaches the
        // bowling client its ball has usually passed the stump z — the 0.3f early-return / 0.2f z-window below would
        // then drop it, leaving "bowled on batting, miss on bowling". Apply the relayed clean-bowled immediately + once.
        // NOTE: only Stump1Collider (batsman's stumps) is a clean bowled during "bowling"; Stump2Collider is the
        // post-shot/hit-wicket path ("shotSuccess") and is left to the original relay logic untouched.
        if (Other == "Stump1Collider"
            && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            if (currentBallStatus != "bowling") return;       // delivery already resolved (bat/pad/etc.)
            if (_remoteBowledAppliedThisDelivery) return;      // idempotent — re-broadcasts are ignored
            _remoteBowledAppliedThisDelivery = true;
            CONTROLLER.CURRENTCOLLIDER = Other;
            ballRayCastConnectionZPositionSaved = SavedBallRayCastConnectedZposition;
            Stump1Collider.SetActive(true);
            OnCustomTriggerEnter(GameObject.Find(Other).GetComponent<Collider>());
            return;
        }

        // Bat-contact relay: same race as the clean-bowled relay above. A genuine bat contact is detected at
        // the end of the batting client's flight, so this reliable RPC usually arrives after the bowling ball
        // has passed the contact z — the 0.3f early-return below would then drop it and the shot would never
        // launch on the bowling client ("shot connects on batting, miss on bowling"). Force-set the collider +
        // saved z and apply immediately + once. TryApplyRemoteBatContact is idempotent and also called from
        // RPC_ChangeBallAngle, because either relay can arrive first (it no-ops here if BALLANGLE isn't set yet,
        // then the angle relay completes the apply).
        if ((Other == "BatCollider" || Other == "BatCollider2")
            && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            if (currentBallStatus == "bowling" && !_remoteShotAppliedThisDelivery)
            {
                CONTROLLER.CURRENTCOLLIDER = Other;
                ballRayCastConnectionZPositionSaved = SavedBallRayCastConnectedZposition;
                TryApplyRemoteBatContact();
            }
            return;
        }

        if ((CONTROLLER.CURRENTCOLLIDER == "Stump2Collider" || CONTROLLER.CURRENTCOLLIDER == "Stump1Collider") && Other != CONTROLLER.CURRENTCOLLIDER)
        {
            return;
        }

        if (CONTROLLER.CURRENTCOLLIDER == Other && ballRayCastConnectionZPositionSaved == SavedBallRayCastConnectedZposition && (Other != "Stump2Collider" && Other != "Stump1Collider"))
        {
            return;
        }

        if (SavedBallRayCastConnectedZposition - raycastAnchorBallTransform.position.z < 0.3f)
        {
            return;
        }

        if (CONTROLLER.CURRENTCOLLIDER != Other)
        {
            //ConstantsData_M.MpLog("KI=onsaa Collkider : " + Other);

            CONTROLLER.CURRENTCOLLIDER = Other;
            ballRayCastConnectionZPositionSaved = SavedBallRayCastConnectedZposition;
            //ConstantsData_M.MpLog("SAVED BALL " + savedBallRayCastConnectedZposition);
        }

        if (ballRayCastConnectionZPositionSaved != SavedBallRayCastConnectedZposition)
        {
        }
        //shotExecutionTime = ShotTime +0.7f;
        ////ConstantsData_M.MpLog("OTHERR : " + CONTROLLER.CURRENTCOLLIDER);
        //StartCoroutine(SendAcknowledgementWithRetry(1));
        //photonView.RPC("OnRPCProcessed", RpcTarget.OthersBuffered, 1);
    }

    public void CallPlayerLeftRoom()
    {
        //if (GameConstants.isWithAI == false)
        //{                                                                                                                                                                //Photon Removal
        //    view.RPC("RPC_OtherPlayerLeftTheRoom", RpcTarget.OthersBuffered);
        //}


        Time.timeScale = 1f;
        Invoke("LeaveGame", 0.1f);
    }

    //Photon Removal [PunRPC]
    public void RPC_ChangeOppTutorial(bool On)
    {
        ////ConstantsData_M.MpLog("ONN HAI ?? " + On);
        CONTROLLER.OpponentTutorial = On;
    }

    public void CallOppTutorial(bool On)
    {
        if (GameConstants.isWithAI == false)
        {
            //Photon Removal    view.RPC("RPC_ChangeOppTutorial", RpcTarget.OthersBuffered, On);
            // Same ghost-session guard as CmdChangeBatsmanPosition/CmdSetMultiplayerCollider:
            // after a teardown the spawned manager is destroyed and Mirror's SendCommandInternal
            // NREs on every call (report 62250255, 2026-07-20).
            if (CricketNetworkManager.ReadyToSend)
            {
                CricketNetworkManager.instance.CmdChangeOppTutorial(staticVariables.UserProfiledata.user._id, On);
            }

        }



    }

    public Vector3 getMaxMinPoint()
    {
        if (batsmanHand == "right")
        {
            if (batterLeftShoeBackEdge.transform.position.x >= batterRightShoeBackEdge.transform.position.x && batterLeftShoeBackEdge.transform.position.x >= batterLeftLegEdgePoint.transform.position.x)
            {
                return batterLeftShoeBackEdge.transform.position;
            }
            if (batterRightShoeBackEdge.transform.position.x >= batterLeftShoeBackEdge.transform.position.x && batterRightShoeBackEdge.transform.position.x >= batterLeftLegEdgePoint.transform.position.x)
            {
                return batterRightShoeBackEdge.transform.position;
            }
            if (batterLeftLegEdgePoint.transform.position.x >= batterLeftShoeBackEdge.transform.position.x && batterLeftLegEdgePoint.transform.position.x >= batterRightShoeBackEdge.transform.position.x)
            {
                return batterLeftLegEdgePoint.transform.position;
            }
        }
        else if (batsmanHand == "left")
        {
            if (batterLeftShoeBackEdge.transform.position.x <= batterRightShoeBackEdge.transform.position.x && batterLeftShoeBackEdge.transform.position.x <= batterLeftLegEdgePoint.transform.position.x)
            {
                return batterLeftShoeBackEdge.transform.position;
            }
            if (batterRightShoeBackEdge.transform.position.x <= batterLeftShoeBackEdge.transform.position.x && batterRightShoeBackEdge.transform.position.x <= batterLeftLegEdgePoint.transform.position.x)
            {
                return batterRightShoeBackEdge.transform.position;
            }
            if (batterLeftLegEdgePoint.transform.position.x <= batterLeftShoeBackEdge.transform.position.x && batterLeftLegEdgePoint.transform.position.x <= batterRightShoeBackEdge.transform.position.x)
            {
                return batterLeftLegEdgePoint.transform.position;
            }
        }
        return Vector3.zero;
    }

    private float setRandomFriction()
    {
        // Ground-shot friction (tester: "halki si shot bhi bagair loft ke boundary hi hoti hai"): at the old
        // value 10 (=10%/s decay) a ~22 u/s ground shot could travel ~220u — the rope is only ~58u away, so
        // EVERY clean ground shot reached the boundary and 1s/2s/3s never happened. At 30 the same shot's total
        // travel caps near ~73u: a full clean hit still just reaches the rope, lighter/placed shots pull up in
        // the field. TUNE HERE from playtest feedback (higher = shots die sooner). Power shots are unaffected
        // while isPowerShotActive (friction applies only after it clears), so boundaries off power stay.
        // The authority's value ships to the follower via the shot stream's dampFactor, so both screens decay
        // identically whatever this returns.
        List<float> list = new List<float>(new float[1] { 30f });
        int count = list.Count;
        int index = UnityEngine.Random.Range(0, count);
        return list[index];
    }

    /// <summary>
    /// Null-safe replacement for `batsmanAnim[currentBatsmanAnimation].speed = 1f`. Unity's Animation
    /// indexer returns NULL for an empty/unknown clip name, and currentBatsmanAnimation is a non-static
    /// string that resets to "" on the reconnect scene reload — the bare indexer then NREd inside
    /// GroundController.Update (stack: Update -> Action3Functions -> LookForMainCameraTopDownView in the
    /// 2026-07-20 "still stuck here" logs), aborting the rest of that frame's Update. Ten call sites all
    /// shared the same latent crash; they now go through this.
    /// </summary>
    private void SetCurrentBatsmanAnimSpeed(float speed)
    {
        if (batsmanAnim == null || string.IsNullOrEmpty(currentBatsmanAnimation)) return;
        var _st = batsmanAnim[currentBatsmanAnimation];
        if (_st != null) _st.speed = speed;
    }

    private int getNoOfWicket()
    {
        int num = 0;
        for (int i = 0; i < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate.Length; i++)
        {
            if (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[i] == "W")
            {
                num++;
            }
        }
        return num;
    }

    /// <summary>
    /// RUN-UP SYNC FIX (batting client). Called via RpcSyncBowlerRunUp when the bowling
    /// client actually begins its run-up. Starts the batting client's bowler run-up so the
    /// throw frame coincides with the authoritative ball-release RPC. Guarded so a stray /
    /// late message can't replay the animation mid-delivery.
    /// </summary>
    // Reconnect scorecard-stuck self-heal: a delivery is about to start, so NO scorecard overlay can
    // legitimately be open. If the opponent disconnected/reconnected during an over-change (bowler
    // selection), the BowlingScoreCard (or BattingScoreCard) can stay stuck on the STAYING player's
    // screen while the game proceeds behind it (auto-bowl fires) — exactly the tester's "bowling
    // scorecard aa gaya batter side, ja nahi raha". The game logic already advances; only the visual
    // overlay is left up. Force-dismiss any lingering scorecard via its normal Hide(true) path before
    // the ball. Online-only; the staying-player Hide is a pure visual dismiss (no state advance).
    public void DismissStaleScoreCardsForDelivery()
    {
        if (CONTROLLER.PlayModeSelected != 8) return;
        var bsc = Singleton<BowlingScoreCard>.instance;
        if (bsc != null && bsc.scoreCard != null && bsc.scoreCard.activeInHierarchy)
        {
            ConstantsData_M.MpLog("[GroundController][DismissStaleScoreCardsForDelivery] Hiding stuck BowlingScoreCard before delivery.");
            bsc.Hide(true);
        }
        var btsc = Singleton<BattingScoreCard>.instance;
        if (btsc != null && btsc.scoreCard != null && btsc.scoreCard.activeInHierarchy)
        {
            ConstantsData_M.MpLog("[GroundController][DismissStaleScoreCardsForDelivery] Hiding stuck BattingScoreCard before delivery.");
            btsc.Hide(true);
        }
    }

    private void HidePause()
    {
        Singleton<Scoreboard>.instance.HidePause(boolean: true);
    }

    public void ScanForBoundaryOrSix()
    {
        float num = temporaryPosition.x - groundCenterMarkerTransform.position.x;
        float num2 = temporaryPosition.z - groundCenterMarkerTransform.position.z;

        float num3 = Mathf.Sqrt(num * num + num2 * num2);
        if (num3 > playingAreaRadius && !isBallOnBoundaryLine && currentBallStatus != "bowled")
        {
            isBallOnBoundaryLine = true;
            canRun = false;
            matchBall.GetComponent<Rigidbody>().WakeUp();
            if (Singleton<GameData>.instance != null)
            {
                disableRunCancelBtn();
            }
            _stayStartTime = Time.time;
            isBallReflectedFromBoundary = false;
            currentBoundaryAction = "boundary";
            showPreviewCamera(status: false);
            // Four-vs-six VALUE fix: boundaryType DEFAULTS to 6 and is only demoted to 4 by the
            // keeper-miss (~7961) / first-bounce-inside (~10401) paths. A ball that reached the rope
            // without those firing kept the default 6 and was scored + announced as a SIX on both
            // clients. Two distinct leak cases are caught here on the batting authority:
            //   (a) NO real shot — a clean miss / leave / byes. currentBallStatus stays "bowled"/
            //       "bowling"/"onPads" (never "shotSuccess", which is only set on a played shot at
            //       OnCustomTriggerEnter ~16102 with bounceCount reset). Can never be a struck six.
            //   (b) A played shot that ONLY edged/deflected and the ball is ROLLING ON THE GROUND when
            //       it crosses the rope (e.g. a power-hit on a fast ball, thin edge, ball rolls away
            //       behind). An edge off a real shot DOES set "shotSuccess", so (a) misses it — but a
            //       grounded ball (height < 0.5, the same "is the ball on the ground" threshold used
            //       for fielder pickups ~5769) physically cannot have cleared the rope on the full, so
            //       it is a FOUR. A genuine SIX is airborne well above the rope here, so it is untouched.
            // The Phantom-FOUR fix below still uses didBallHitBat to credit batsman-runs vs extras.
            if ((currentBallStatus != "shotSuccess" || matchBallTransform.position.y < 0.5f) && !isReplayModeActive)
            {
                boundaryType = 4;
            }
            if (boundaryType == 6 && !isReplayModeActive)
            {
                CONTROLLER.SixDistance = Mathf.FloorToInt(firstBounceDistance);
                runsScoredThisBall = 6;
                if (Singleton<GameData>.instance != null)
                {
                    Singleton<GameData>.instance.PlayGameSound("Boundary");
                }
            }
            else if (!isReplayModeActive)
            {
                runsScoredThisBall = 4;
                if (Singleton<GameData>.instance != null)
                {
                    Singleton<GameData>.instance.PlayGameSound("Boundary");
                }
                if (currentBallStatus == "shotSuccess" && Singleton<GameData>.instance != null && !isReplayModeActive && isSlipShot)
                {
                }
            }
            // Relay the batting authority's 4-vs-6 verdict the moment it is decided (rope-cross). The
            // bowling side applies it at scoring time, which sits 2s later in the stay window — ample
            // room for the RPC even with the 0.45s lockstep offset. If it never arrives the receiver
            // falls back to its local value, which is exactly today's behaviour.
            if (!isReplayModeActive && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex && CricketNetworkManager.ReadyToSend)
            {
                ConstantsData_M.MpLog($"[GroundController][BoundaryVerdict] Authority sending verdict: runs={runsScoredThisBall} hitBat={didBallHitBat} ballY={matchBallTransform.position.y:F2} status={currentBallStatus}.");
                CricketNetworkManager.instance.CmdBoundaryVerdict(staticVariables.UserProfiledata.user._id, runsScoredThisBall, didBallHitBat);
            }
            if (currentWicketKeeperStatus == "catchMissed")
            {
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Beaten");
                }
                if (!isEdgeCaught && isBallWide)
                {
                    runsScoredThisBall = 5;
                }
                else if (!isBallWide)
                {
                    // #1 fix (behind != automatic 4): a ball that merely BEAT the bat (no genuine
                    // contact) and ran past the keeper is collected behind as 1-2 BYES, not an
                    // automatic boundary. A genuinely edged/glanced ball (didBallHitBat) that
                    // reaches the rope behind is still a real 4. Scored as extras via the same
                    // waitForWideSignal RPC on the batting authority, so no cross-client desync.
                    if (didBallHitBat)
                        runsScoredThisBall = 4;
                    else
                        runsScoredThisBall = UnityEngine.Random.Range(1, 3);
                }
                currentBoundaryAction = "wideAndBoundary";
            }
        }
        if (currentBoundaryAction == "boundary")
        {
            // Bowling side: prefer the batting authority's relayed 4-vs-6 verdict over the local physics
            // guess. Applied HERE, at the top, because this block has THREE resolve branches — overstepped
            // (lineNoBallBoundary), line-free-hit, and normal — and each scores independently. Placing it
            // inside the normal branch left the other two on their local guess, which is exactly the
            // tester's "free hit pe straight shot: ik side 4, ik side 6". Runs every frame of the stay
            // window so the verdict lands as soon as it arrives, and is idempotent: once applied the values
            // match and the condition stops firing.
            if (_relayedBoundaryRuns > 0 && CONTROLLER.BattingTeamIndex != CONTROLLER.myTeamIndex
                && (_relayedBoundaryRuns != runsScoredThisBall || _relayedBoundaryHitBat != didBallHitBat))
            {
                ConstantsData_M.MpLog($"[GroundController][BoundaryVerdict] Overriding local runs={runsScoredThisBall} hitBat={didBallHitBat} with authority runs={_relayedBoundaryRuns} hitBat={_relayedBoundaryHitBat}.");
                runsScoredThisBall = _relayedBoundaryRuns;
                didBallHitBat = _relayedBoundaryHitBat;
            }
            if (isOversteppedDelivery)
            {
                ////ConstantsData_M.MpLog("NOOO BALLLL");
                float num4 = 0.5f;
                //if (CONTROLLER.PlayModeSelected == 7)
                //{
                //	num4 = 0f;
                //}
                if (_stayStartTime + num4 + delayBetweenDeliveries < Time.time)
                {
                    if (isReplayModeActive)
                    {
                        currentBoundaryAction = string.Empty;
                        HideReplay();
                        return;
                    }
                    currentBoundaryAction = "lineNoBallBoundary";
                    lineNoBallRunsScored = runsScoredThisBall;
                    if (runsScoredThisBall == 4)
                    {
                        Singleton<GameData>.instance.InitAnimation(0);
                    }
                    else if (runsScoredThisBall == 6)
                    {
                        Singleton<GameData>.instance.InitAnimation(1);
                    }
                }
            }
            else if (isLineFreeHitActive)
            {
                if (_stayStartTime + 1.5f + delayBetweenDeliveries < Time.time)
                {
                    currentBoundaryAction = string.Empty;
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: true);
                    isLineFreeHitActive = false;
                    freeHit = false;
                    lastBowledBallType = "lineball";
                    Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                }
            }
            else
            {
                if (isReplayModeActive && _stayStartTime + 0.5f + delayBetweenDeliveries < Time.time)
                {
                    HideReplay();
                    return;
                }
                if (_stayStartTime + 2f + delayBetweenDeliveries < Time.time)
                {
                    currentBoundaryAction = string.Empty;
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        if (noBall)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: true);
                            CONTROLLER.isJokerCall = false;
                        }
                        else if (freeHit)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: true);
                            freeHit = false;
                            CONTROLLER.isFreeHit = false;
                        }
                        else
                        {
                            // Phantom-FOUR fix (#2): only credit the boundary to the STRIKER if the bat
                            // actually made contact. A ball that BEAT the bat and reached the rope (e.g.
                            // past a missed keeper, going behind the batsman) is BYES — extras, not a
                            // batsman's 4/6. Legit boundaries (didBallHitBat==true, incl. edges) are
                            // UNCHANGED. noBall/freeHit are handled in the branches above; a wide
                            // keeper-miss already routes to "wideAndBoundary". The team total is the
                            // same 4 either way, so no cross-client desync — only the attribution
                            // (batsman runs vs extras) changes, and it travels via the same RPC params.
                            if (didBallHitBat)
                                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: true);
                            else
                                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, runsScoredThisBall, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: true);
                        }
                    }
                }
            }
        }
        else if (currentBoundaryAction == "lineNoBallBoundary")
        {
            if (_stayStartTime + 4f + delayBetweenDeliveries < Time.time)
            {
                currentBoundaryAction = string.Empty;
                Singleton<GameData>.instance.setReplayCompletedVariable();
                showMainUmpireForNoBallAction();
                bool flag = checkForMatchComplete(runsScoredThisBall + 1, 0);
                if (CONTROLLER.matchType == "oneday" && !flag)
                {
                    mainUmpireAnim.Play("NoBallFreeHit_New");
                    isLineFreeHitActive = true;
                    lastBowledBallType = "overstep";
                }
                else
                {
                    mainUmpireAnim.Play("NoBallFreeHit_New");
                }
            }
        }
        else if (currentBoundaryAction == "wideAndBoundary")
        {
            if (_stayStartTime + 2.5f + delayBetweenDeliveries < Time.time)
            {
                showMainUmpireForNoBallAction();
                if (isOversteppedDelivery)
                {
                    if (isReplayModeActive)
                    {
                        currentBoundaryAction = string.Empty;
                        HideReplay();
                        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball += 5;
                        Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 5, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                        {
                        }
                        return;
                    }
                    currentBoundaryAction = "umpireNoBallAction";
                    bool flag2 = checkForMatchComplete(5, 0);
                    if (CONTROLLER.matchType == "oneday" && !flag2)
                    {
                        noBallActionDelay = 4f;
                        mainUmpireAnim.Play("NoBallFreeHit_New");
                        isLineFreeHitActive = true;
                        lastBowledBallType = "overstep";
                    }
                    else
                    {
                        noBallActionDelay = 1.5f;
                        mainUmpireAnim.Play("NoBallFreeHit_New");
                    }
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Beaten");
                    }
                }
                else if (isLineFreeHitActive)
                {
                    umpireCamTransform.position = new Vector3(0f, 2f, -11f);
                    currentBoundaryAction = "waitForWideSignal";
                    iTween.MoveTo(umpireViewCamera.gameObject, iTween.Hash("y", UnityEngine.Random.Range(1.4f, 1.8f), "time", 2));
                    if (isBallWide)
                    {
                        mainUmpireAnim.Play("WideBall_New");
                        isLineFreeHitActive = true;
                        lastBowledBallType = "overstep";
                        ////ConstantsData_M.MpLog("SHOWFREEHIT");
                        Singleton<Scoreboard>.instance.showFreeHitBg(canShow: true);
                    }
                    else
                    {
                        // #1 fix: only signal a FOUR when 4 byes were actually awarded; a 1-2 bye
                        // collected behind shows the neutral idle instead of the boundary signal.
                        mainUmpireTransform.GetComponent<Animation>().Play(runsScoredThisBall >= 4 ? "ByesFour_New" : "IdleGetReady");
                        isLineFreeHitActive = false;
                        freeHit = false;
                        lastBowledBallType = "lineball";
                        Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                    }
                    if (BowlingBy == "computer")
                    {
                        bowlerSide = "left";
                    }
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Beaten");
                    }
                }
                else
                {
                    currentBoundaryAction = "waitForWideSignal";
                    umpireCamTransform.position += new Vector3(0f, 0f, 0f);
                    iTween.MoveTo(umpireViewCamera.gameObject, iTween.Hash("y", UnityEngine.Random.Range(1.4f, 1.8f), "time", 2));
                    if (isBallWide)
                    {
                        mainUmpireAnim.Play("WideBall_New");
                    }
                    else
                    {
                        // #1 fix: only signal a FOUR when 4 byes were actually awarded; a 1-2 bye
                        // collected behind shows the neutral idle instead of the boundary signal.
                        mainUmpireTransform.GetComponent<Animation>().Play(runsScoredThisBall >= 4 ? "ByesFour_New" : "IdleGetReady");
                    }
                    if (BowlingBy == "computer")
                    {
                        bowlerSide = "left";
                    }
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Beaten");
                    }
                }
            }
        }
        else if (currentBoundaryAction == "umpireNoBallAction")
        {
            if (_stayStartTime + noBallActionDelay + delayBetweenDeliveries < Time.time)
            {
                currentBoundaryAction = "loopEnd";
                if (Singleton<GameData>.instance != null)
                {
                    if (isOversteppedDelivery)
                    {
                        if (!isReplayModeActive)
                        {
                            noBallRunStatus = "wideboundary";
                            if (CONTROLLER.canShowReplay)
                            {
                                Singleton<GameData>.instance.GameIsOnReplay();
                                ShowReplay();
                            }
                            else
                            {
                                Singleton<GameData>.instance.ReplayIsNotShown();
                            }
                        }
                        else
                        {
                            HideReplay();
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball += 5;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, 5, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                            {
                            }
                        }
                    }
                    else if (isLineFreeHitActive)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                        isLineFreeHitActive = false;
                        lastBowledBallType = "lineball";
                    }
                }
            }
        }
        else if (currentBoundaryAction == "waitForWideSignal" && _stayStartTime + 4f + delayBetweenDeliveries < Time.time)
        {
            currentBoundaryAction = string.Empty;
            if (Singleton<GameData>.instance != null)
            {
                if (isBallWide)
                {
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWideBall += runsScoredThisBall;
                    Singleton<GameData>.instance.UpdateCurrentBall(0, 0, 0, runsScoredThisBall, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                }
                else
                {
                    CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWideBall += runsScoredThisBall;
                    Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, runsScoredThisBall, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                }
            }
        }
        if (num3 > outerBoundaryRadius - 2f && !isBallReflectedFromBoundary && !isBallOverTheFence)
        {
            CustomRayCastForBattingBallMovement();
        }
        if (num3 > outerBoundaryRadius && !isBallReflectedFromBoundary && !isBallOverTheFence)
        {
            //if (ballTransform.position.y > boundaryFenceHeight)
            if (temporaryPosition.y > fenceBoundryHeight)
            {
                isBallOverTheFence = true;
            }
            else
            {
                BallRebouncesFromBoundary();
            }
        }
        if (num3 > playingAreaRadius + 15f && !isBallReflectedFromBoundary && isBallOverTheFence)
        {
            horizontalVelocity *= 0.2f;
            applyBallFriction = true;
            isBallReflectedFromBoundary = true;
        }
    }

    public void EnableAllSkinRenderers(bool state)
    {
        fielder10SkinRenderer.enabled = state;
        batsmanSkinRenderer.enabled = state;
        BatsmanCricketKitSkinRenderer.enabled = state;
        BatsmanBatSkinRenderer.enabled = state;
        RunnerSkinRenderer.enabled = state;
        RunnerCricketKitSkinRenderer.enabled = state;
        RunnerBatSkinRenderer.enabled = state;
        MainUmpireSkinRenderer.enabled = state;
        SideUmpireSkinRenderer.enabled = state;
        BowlerSkinRenderer.enabled = state;
        WicketKeeperSkinRenderer.enabled = state;
        //WicketKeeperCricketKitSkinRendererComponent.enabled = state;
        Fielder10BallSkinRenderer.enabled = state;
        WicketKeeperBallSkin.enabled = state;
        BowlerBallSkinRenderer.enabled = state;
        DigitalScreenRenderer.enabled = state;
        for (int i = 0; i < fielders.Length; i++)
        {
            if (FielderSkinRenderer[i] != null)
            {
                FielderSkinRenderer[i].enabled = state;
                fielderCaps[i].enabled = state;
            }
        }
    }

    public void ProcessOnImpact()
    {
        impactMarkerBall.SetActive(value: true);
        impactMarkerBall.GetComponent<Renderer>().material.mainTexture = ballTextureRenderer.material.mainTexture;
        impactMarkerBall.transform.position = matchBallTransform.position;
        //impactBall.transform.position = tempPos;
        shouldDisplayImpact = true;
        isBallPaused = false;
        Singleton<DRS>.instance.DRSBallTrailMaterial.color = Color.blue;
        Singleton<DRS>.instance.DRSRedTrail.transform.SetParent(impactMarkerBall.transform);
        Singleton<DRS>.instance.DRSRedTrail.transform.localPosition = Vector3.zero;
        Singleton<DRS>.instance.DRSRedTrail.transform.localEulerAngles = Vector3.zero;
        Singleton<DRS>.instance.DRSBlueTrail.SetActive(value: true);
        Singleton<DRS>.instance.DRSBlueTrail.GetComponent<TrailRenderer>().time = 30f;
        CheckDRSImpactOnPad();
    }

    private void GetInputs()
    {
        GetBattingInput();
        StartCoroutine(BowlerSideChange("key"));
    }

    private void UpdateShadowsAndPreview()
    {
        UpdateBallShadow();
        UpdatePreview();
        if (enableShadows)
        {
            UpdateShadow();
        }
    }

    private void Update()
    {
        if (_lockstepSendSwingNextFrame) { _lockstepSendSwingNextFrame = false; LockstepSendShotInput(); }
        TickStuckHeartbeat();   // silent-stall trace (both-sides-disconnect)
        TickDeliveryWatchdog(); // G1 v2: staying-client delivery self-heal (kill-switched, ships dark)
        // ---- Ball position streaming (visual trajectory sync) ----
        // Authority for the VISIBLE flight flips at bat contact, so the sync does too:
        //   PRE-SHOT  (!isBallHit): the BOWLING client owns the delivery → it streams the ball
        //                           POSITION to the BATTING client (CmdSyncBallPosition); the
        //                           batting client lerps toward it (mirror block below).
        //   POST-SHOT ( isBallHit): the BATTING client owns the outcome (runs/wicket/boundary are
        //                           committed batting-side), so it must ALSO own the visible flight.
        //                           It streams the FULL post-shot kinematic state (CmdSyncBallShot)
        //                           to the BOWLING client, which overwrites its parametric
        //                           integrator inputs in RPC_SyncBallShot so its per-frame
        //                           BattingBallMovement()/BallMovement() continues from the SAME
        //                           state → identical six/four/out on both screens.
        //
        // Why the post-shot side streams FULL state instead of just position: BallMovement()
        // re-derives matchBallTransform.position from temporaryPosition + launchAngle (+ arcHeight/
        // _ballAngle/horizontalVelocity) EVERY frame, later in this same Update(). A position-only
        // lerp here is therefore overwritten by the integrator on the very next line of execution —
        // which is exactly why the earlier position-only attempt failed (six-vs-four persisted).

        // PRE-SHOT authority: bowling client streams delivery position to the batting client.
        bool _amDeliveryAuthority =
            GameConstants.isWithAI == false
            && CONTROLLER.PlayModeSelected == 8
            && CricketNetworkManager.instance != null
            && !isBallHit
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex;

        // POST-SHOT authority: batting client streams full kinematic state to the bowling client.
        // Phantom-shot fix (v2): only stream when the batsman ACTUALLY PLAYED A SHOT, i.e. currentShotPlayed
        // is a real shot name — NOT "bt6Leave" (a left ball) and not "" (no input). On a left ball the sharply
        // spin-turned ball can clip the idle bat collider and set isBallHit=true with NO shot played; streaming
        // on bare isBallHit then leaked a CmdSyncBallShot that the bowling client adopted, flying the ball past
        // the keeper while the batting screen correctly let the keeper take it. currentShotPlayed is set at
        // batsman input (before bat contact) and stays for the whole delivery (reset only in ResetAll), so this
        // is stable for the ENTIRE real-shot flight — unlike the earlier currentBallStatus=="shotSuccess" gate
        // which flipped off mid-flight (shotSuccess→throw) and cut the stream (that regressed real shots).
        bool _amShotAuthority =
            GameConstants.isWithAI == false
            && CONTROLLER.PlayModeSelected == 8
            && CricketNetworkManager.instance != null
            && isBallHit
            && currentShotPlayed != "bt6Leave"
            && currentShotPlayed != string.Empty
            && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex;

        if (isBallReleased && !isReplayModeActive && (_amDeliveryAuthority || _amShotAuthority))
        {
            _ballStreamTimer += Time.deltaTime;
            if (_ballStreamTimer >= BALL_STREAM_INTERVAL)
            {
                _ballStreamTimer = 0f;
                if (_amShotAuthority)
                {
                    // Deterministic post-shot (Phase 1): arm this authority's fixed-step integrator ONCE, on
                    // the first post-contact stream, onto shared NetworkTime so its own flight advances the same
                    // BALL_SIM_FIXED_STEP path the follower will replay. velocityDampingFactor is whatever the
                    // first BattingBallMovement frame already rolled (setRandomFriction) — capture & ship it so
                    // the follower uses the identical friction instead of rolling its own → bit-matching flight.
                    if (!_deterministicShotArmed)
                    {
                        _deterministicShotArmed   = true;
                        _ballSimStartNetTime      = Mirror.NetworkTime.time;
                        _ballSimTicksDone         = 0;
                        _ballSimMaxTargetTicks    = -1;   // Issue #3: reset per-flight monotonic target high-water mark
                        _useDeterministicBallStep = true;
                    }
                    CricketNetworkManager.instance.CmdSyncBallShot(
                        staticVariables.UserProfiledata.user._id,
                        matchBallTransform.position,
                        temporaryPosition,
                        _ballAngle,
                        launchAngle,
                        arcHeight,
                        angleChangeRate,
                        horizontalVelocity,
                        bounceCount,
                        boundaryType,
                        Mirror.NetworkTime.time,   // follower re-bases its integrator onto THIS send instant
                        velocityDampingFactor);
                }
                else
                {
                    CricketNetworkManager.instance.CmdSyncBallPosition(
                        staticVariables.UserProfiledata.user._id,
                        matchBallTransform.position,
                        temporaryPosition,
                        launchAngle);
                }
            }
        }
        else
        {
            _ballStreamTimer = 0f;
        }

        // MIRROR side (PRE-SHOT only): the BATTING client lerps the local ball toward the latest
        // authoritative delivery position each frame. The local delivery simulation still runs for
        // smooth motion between packets, but is continuously pulled toward the bowling authority.
        // POST-SHOT mirroring is NOT done here — it's handled by RPC_SyncBallShot overwriting the
        // full parametric state (a lerp would be clobbered by BallMovement(), see note above).
        bool _amDeliveryMirror =
            GameConstants.isWithAI == false
            && CONTROLLER.PlayModeSelected == 8
            && !isBallHit
            && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex;

        // Stage 3 (ping-stable delivery rework): when useLocalDeliverySim is ON we SKIP this per-frame
        // lerp entirely. The local deterministic simulation (BallMovement, from the authoritative
        // release params + the Stage 1 swing fix) OWNS matchBallTransform.position AND temporaryPosition
        // (the collision/timing source) — so the batsman times a smooth, ping-INDEPENDENT ball instead
        // of one dragged toward 20Hz packets every frame. _networkBallPosition is still received (the
        // bowling client keeps streaming), so the false-bowled guard still has authoritative data, and
        // the per-bounce RpcSyncBallTrajectory still snaps the bounce as the safety-net. Flag OFF
        // (default) = unchanged stream-driven lerp (the previous jitter fix).
        if (_hasNetworkBallPosition && isBallReleased && !isReplayModeActive && _amDeliveryMirror
            && !ConstantsData_M.useLocalDeliverySim)
        {
            matchBallTransform.position = Vector3.Lerp(
                matchBallTransform.position,
                _networkBallPosition,
                0.4f); // 40% per frame → snappy correction without visible jitter

            // Collision-alignment ("spin ball beats the bat" fix): the lerp above only moves the
            // VISIBLE transform, but BallMovement() rebuilds the ball every frame from
            // temporaryPosition, and the bat-hit raycast (raycastAnchorBallTransform) reads THAT —
            // not the lerped visible position. So the batting player swings at where the ball is
            // SHOWN while the collision tests a different (un-lerped local-integration) position →
            // well-timed shots miss, worst on spin (where local vs authoritative paths diverge
            // most). Pull temporaryPosition toward the same authoritative delivery position so the
            // collision and the visible ball — and the real bowling-authority path — agree.
            temporaryPosition = Vector3.Lerp(temporaryPosition, _networkBallPosition, 0.4f);
        }

        if (hasCancelledRun && isBallHit && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            cancelRunDuration = Time.time - ballConnectionTiming;
            if (cancelRunDuration > (cancelRunTimestamp - 0.01f))
            {
                isRunCancelled = true;
                cancelRunningBetweenWicket();
                hasCancelledRun = false;
                cancelRunTimestamp = 0f;
                cancelRunDuration = 0f;
            }
        }



        if (tutorialIndicatorArrow.activeSelf)
        {
            tutorialIndicatorArrow.transform.eulerAngles = new Vector3(0f, 0f, 0f);
        }
        if (!CONTROLLER.GameIsOnFocus)
        {
            return;
        }
        if (isFullToss && bowlingSpotFullTossObject.transform.localPosition.y > 0.58f)
        {
            bowlingSpotFullTossObject.transform.position = new Vector3(bowlingSpotFullTossObject.transform.position.x, 0.56f, bowlingSpotFullTossObject.transform.position.z);
        }
        if (isFullToss && (double)bowlingSpotFullTossObject.transform.localPosition.x > 1.2)
        {
            bowlingSpotFullTossObject.transform.position = new Vector3(1.2f, bowlingSpotFullTossObject.transform.position.y, bowlingSpotFullTossObject.transform.position.z);
        }
        if (isFullToss && (double)bowlingSpotFullTossObject.transform.localPosition.x < -1.02)
        {
            bowlingSpotFullTossObject.transform.position = new Vector3(-1.02f, bowlingSpotFullTossObject.transform.position.y, bowlingSpotFullTossObject.transform.position.z);
        }
        if (isFullToss && (double)bowlingSpot.localPosition.z < 9.3 && CONTROLLER.BowlingTeamIndex == CONTROLLER.opponentTeamIndex && bowlingSpotFullTossObject.activeInHierarchy)
        {
            ShowFullTossSpot(_Value: false);
            bowlingSpotObject.SetActive(value: true);
            if (BattingBy == "user")
            {
                bowlingSpotRenderer.enabled = false;
                if (tutorialIndicatorArrow != null)
                {
                    tutorialIndicatorArrow.SetActive(value: false);
                }
            }
            else
            {
                bowlingSpotRenderer.enabled = true;
                if (tutorialIndicatorArrow != null)
                {
                    tutorialIndicatorArrow.SetActive(value: true);
                }
            }
        }
        GetInputs();


        switch (currentActionState)
        {
            case -2:

                if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && CONTROLLER.PlayModeSelected == 8 && Time.timeScale != 0)
                {
                    if (CONTROLLER.CanBowlerBowl == false)
                    {
                        CONTROLLER.CanBowlerBowl = true;
                    }
                    UpdateCanBowlerBowl(true);
                }

                if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.CanBowlerBowl && currentActionState == -2 && !timerObject.activeInHierarchy && CONTROLLER.PlayModeSelected == 8)
                {
                    timerObject.SetActive(true);
                    // Reconnect-hang fix (root): a mid-delivery reconnect can leave the timer's PARENT panel
                    // inactive, so timerObject.activeInHierarchy stays FALSE even after SetActive(true) above —
                    // and the ENTIRE auto-bowl chain (AutomaticBall + CallAutomaticBall both early-return on
                    // !activeInHierarchy) then never fires, so the bowler never re-bowls and this -2 branch loops
                    // every frame (the "inactive/killed Sequence" spam). Re-activate any inactive ancestor so the
                    // timer is genuinely live; SetActive is synchronous, so SetAutoBowlingTimer below then sees
                    // activeInHierarchy==true and arms a real sequence whose InsertCallback fires CallAutomaticBall.
                    // (The bowling UI SHOULD be visible — we ARE bowling.)
                    for (Transform _anc = timerObject.transform.parent; _anc != null; _anc = _anc.parent)
                    {
                        if (!_anc.gameObject.activeSelf) _anc.gameObject.SetActive(true);
                    }
                    SetAutoBowlingTimer(10f);
                }

                ////ConstantsData_M.MpLog("CASEEEEE : " + -2);
                if (Singleton<Scoreboard>.instance.freeHitGO.activeInHierarchy)
                {
                    if (!freeHit)
                    {
                        ////ConstantsData_M.MpLog("ISFREEHIT : " + freeHit);
                        {
                            Singleton<Scoreboard>.instance.showFreeHitBg(canShow: false);
                        }
                    }
                }
                ZoomCameraToBowler();
                UpdateShadowsAndPreview();
                if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
                {
                    Singleton<GameData>.instance.canPauseGameplay = false;
                    Singleton<GameData>.instance.canPauseGameplay = true;
                }
                else if (!CONTROLLER.ReplayShowing)
                {
                    Singleton<GameData>.instance.canPauseGameplay = true;
                }
                else
                {
                    Singleton<GameData>.instance.canPauseGameplay = false;
                }
                break;
            case -1:
                UpdateShadowsAndPreview();
                break;
            case -11:
                ////ConstantsData_M.MpLog("CASEEEEE : " + -11);
                RotateUltraMotionCameraBatsmanCelebration();
                break;
            case -10:
                ////ConstantsData_M.MpLog("CASEEEEE : " + -10);
                UpdateShadowsAndPreview();
                break;
            case 0:
                ////ConstantsData_M.MpLog("CASEEEEE : " + 0);
                BatsmanWaiting();
                UpdateShadowsAndPreview();
                break;
            case 1:
                ////ConstantsData_M.MpLog("CASEEEEE : " + 1);
                BowlerWaiting();
                break;
            case 2:
                ////ConstantsData_M.MpLog("CASEEEEE : " + 2);
                if (CONTROLLER.CanBowlerBowl && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && CONTROLLER.PlayModeSelected == 8 && Time.timeScale != 0)
                {
                    UpdateCanBowlerBowl(false);
                }
                enableBowlingSpot();
                UserChangingBowlingSpot();
                checkForLineNoBall();
                UpdateShadowsAndPreview();
                break;
            case 3:
                ////ConstantsData_M.MpLog("CASEEEEE : " + 3);
                if (CONTROLLER.CanBowlerBowl && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && CONTROLLER.PlayModeSelected == 8 && Time.timeScale != 0)
                {
                    UpdateCanBowlerBowl(false);
                }
                Action3Functions();
                break;
            case 4:
                ////ConstantsData_M.MpLog("CASEEEEE : " + 4);
                if (CONTROLLER.CanBowlerBowl && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex && CONTROLLER.PlayModeSelected == 8 && Time.timeScale != 0)
                {
                    UpdateCanBowlerBowl(false);
                }
                Action4Functions();
                break;
            case 22:
                moveFielders();
                break;
        }
        ReplayCameraMovement();
        CheckForHotspotPosition();
    }

    private void WarmUpOnce()
    {
        int num = currentActionState;
        currentActionState = 3;
        Action3Functions();
        currentActionState = 4;
        Action4Functions();
        currentActionState = num;
    }

    private void Action3Functions()
    {
        WicketKeeperPreBattingActions();
        if (_useDeterministicBallStep)
            StepDeliveryDeterministic();   // fixed-step network-clock catch-up (delivery, Phase 2)
        else
            BowlingBallMovement();         // legacy: one frame-delta step (offline / delivery not armed)
        FindBatsmanCanMakeShot();
        ExecuteTheShot();
        RunnerActions();
        ActivateBowler();
        LookForMainCameraTopDownView();
        ScanForBoundaryOrSix();
        UpdateShadowsAndPreview();
    }

    private void Action4Functions()
    {
        if (isEdgeCaught)
        {
            WicketKeeperPreBattingActions();
        }
        else
        {
            WicketKeeperPostBattingActions();
        }
        if (_useDeterministicBallStep)
            StepBallDeterministic();   // fixed-step network-clock catch-up (matches across clients)
        else
            BattingBallMovement();     // legacy: one frame-delta step (offline / not-yet-enabled)
        ActivateFielders();
        ActivateBowler();
        LookForRunByComputerBatsman();
        ThrowingBallMovement();
        RunnerActions();
        LookForMainCameraTopDownView();
        ScanForBoundaryOrSix();
        UpdateShadowsAndPreview();
    }

    private void OnCustomTriggerEnter(Collider other)
    {
        // Lockstep: bat contact is resolved on the deterministic tick path (LockstepTryResolveContact) —
        // the bat colliders stay presentation-only so frame-dependent physics can never diverge the clients.
        if (LockstepActive && (other.gameObject.name == "BatCollider" || other.gameObject.name == "BatCollider2"))
            return;

        if (isDRSHardcoded)
        {
            if (!isReplayModeActive || !(summarySaved != "onPads"))
            {
                currentBallStatus = "onPads";
                currentRunner.transform.eulerAngles = new Vector3(currentRunner.transform.eulerAngles.x, 0f, currentRunner.transform.eulerAngles.z);

                runnerAnim.Play("BackToCreaseNew");
                if (isBatsmanConfident)
                {
                    DecreaseConfidenceLevel(currentBallStatus);
                }
                ActivateColliders(boolean: false);
                ActivateStadiumAndSkybox(boolean: true);
                if (currentBatsmanAnimation != string.Empty)
                {
                    SetCurrentBatsmanAnimSpeed(1f);
                }
                HideBowlingSpot();
                ShowFullTossSpot(_Value: false);
                if (!isReplayModeActive)
                {
                    summarySaved = "onPads";
                    padColliderSaved = other;
                    _ballAngle = _ballAngle + 180f + (float)DetRange(-20, 20);   // lockstep: seeded — fence/pad deflections were per-client random (a known cross-screen diverger)
                    ballAngleAfterHittingPadsSaved = _ballAngle;
                    ballRayCastConnectionZPositionSaved = raycastAnchorBallTransform.position.z;
                }
                else
                {
                    _ballAngle = ballAngleAfterHittingPadsSaved;
                }
                _ballAngle %= 360f;
                horizontalVelocity *= 0.05f;
                angleChangeRate *= 1.5f;
                arcHeight *= 0.5f;
                applyBallFriction = true;
                hasLBWAppeal = true;
                if (bowlerType == "spin")
                {
                    isBowlerActivationAllowed = false;
                    ShowFielder10(fielder10Status: true, ball10Status: false);
                    ShowBowler(showStatus: false);
                }
                if (!isReplayModeActive)
                {
                    isLbwAppealSaved = hasLBWAppeal;
                }
                if (bowlerType == "fast" && (isOversteppedDelivery || !isLineFreeHitActive))
                {
                    keeperAnim.Play("appealFast");
                }
                if (bowlerType == "medium" && (isOversteppedDelivery || !isLineFreeHitActive))
                {
                    keeperAnim.Play("appealFast");
                }
                else if (bowlerType == "spin" && (isOversteppedDelivery || !isLineFreeHitActive))
                {
                    keeperAnim.Play("appealSpin");
                }
                makeFieldersToCelebrate(null);
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Cheer");
                }
                if (bounceCount == 0 && Mathf.Abs(matchBallTransform.position.x) <= 0.1f)
                {
                    isBallInline = true;
                }
                if (matchBallTransform.position.y < 0.6f && Mathf.Abs(stumpImpactSpot.transform.position.x) < 0.12f && isBallInline)
                {
                    isLBW = true;
                }
                if (!isReplayModeActive)
                {
                    isLbwSaved = isLBW;
                }
                else if (isReplayModeActive)
                {
                    isLBW = isLbwSaved;
                }
                initialUmpireDecision = isLBW;
            }
            return;

        }
        if ((other.gameObject.name == "BatCollider" || other.gameObject.name == "BatCollider2") && currentBallStatus == "bowling" && currentBallStatus != "onPads" && BattingBy == "user")
        {
            didBallHitBat = true;
            // Issue #2 (too batsman-friendly): a badly MIS-TIMED contact is a play-and-miss, not a free
            // valid shot. battingTimingMeterValue is [-100,100] (0 = perfect). Medium-strict gate:
            // |timing| > shotMissTimingThreshold => convert the attempted shot into a leave so the ball
            // carries through to the keeper via the existing (proven) leave path — no softlock risk.
            // TUNABLE: lower shotMissTimingThreshold = harder. PLAYTEST before shipping.
            // ONLINE GUARD (#1 "2nd team can't hit"): battingTimingMeterValue = temporaryPosition.z/5*100 treats
            // z=0 as perfect, but in the 2nd innings the bat meets the ball at z≈3.15 (meter≈63), so a well-timed
            // 2nd-innings shot already reads as "late" and any slightly-later contact tips over the 65 cut-off →
            // EVERY contact becomes a play-and-miss → the chasing batsman can't hit a single ball (logs 26-06:
            // meterZ≈3.15 on real shots, the rest auto-leave). The meter mis-calibration is innings-specific
            // (team-switch reproduced it), so until it's recalibrated DON'T apply this experimental miss-gate
            // online — revert to the proven "bat contact = valid shot". Offline AI difficulty path untouched.
            if (!isReplayModeActive && CONTROLLER.PlayModeSelected != 8 && Mathf.Abs(battingTimingMeterValue) > shotMissTimingThreshold)
            {
                didBallHitBat = false;
                currentShotPlayed = "bt6Leave";
            }
        }
        if (((other.gameObject.name == "BatCollider" || other.gameObject.name == "BatCollider2") && currentBallStatus == "bowling" && currentBallStatus != "onPads") || (isReplayModeActive && summarySaved != "onPads" && summarySaved != "bowled" && ballRayCastConnectionZPositionSaved != 0f))
        {
            HideBowlingSpot();
            ShowFullTossSpot(_Value: false);
            if (currentShotPlayed != "bt6Leave" && currentShotPlayed != string.Empty)
            {
                if (isReplayModeActive && summarySaved != "connected" && summarySaved != "catch" && summarySaved != "picked")
                {
                    return;
                }
                if (!isReplayModeActive)
                {
                    summarySaved = "connected";
                    ballConnectionPositionSaved = matchBallTransform.position;
                    ballSpinSpeedZ = UnityEngine.Random.Range(-3600, -1800);
                    ballConnectedSpinSpeedZSaved = ballSpinSpeedZ;
                }
                else if (isReplayModeActive)
                {
                    ////ConstantsData_M.MpLog("* position change");
                    matchBallTransform.position = ballConnectionPositionSaved;
                    ballSpinSpeedZ = ballConnectedSpinSpeedZSaved;
                    temporaryPosition = matchBallTransform.position;
                }

                ballStartPoint.position = new Vector3(temporaryPosition.x, ballStartPoint.position.y, temporaryPosition.z);
                batContactHeight = temporaryPosition.y;
                bounceCount = 0;
                currentBallStatus = "shotSuccess";
                ActivateColliders(boolean: false);
                ActivateStadiumAndSkybox(boolean: true);
                BoundaryBoardCollider.SetActive(value: true);
                if (currentShotPlayed != "bt6Defense" && currentShotPlayed != "backFootDefenseHighBall" && currentShotPlayed != "frontFootOffSideDefense")
                {
                    canRun = true;
                    if (Singleton<GameData>.instance != null)
                    {
                        Singleton<GameData>.instance.EnableRun(boolean: true);
                    }
                }
                SetCurrentBatsmanAnimSpeed(1f);
                BallTiming();


                if (isEdgeCaught && DistanceBetweenTwoGameObjects(wicketKeeperObject, matchBall) < 13.5f)
                {
                    canKeeperCatchBall = true;
                }
                else if (!stopKeeper)
                {
                    MoveWicketKeeperToStumps();
                }
                GetFieldersAngle();
                GetFieldersDistance();
                if (!stopKeeper)
                {
                    SetActiveFielders();
                }
                if (_ballAngle >= 90f && _ballAngle <= 270f)
                {
                    umpireRunSignalDirection = -1;
                }
                else
                {
                    umpireRunSignalDirection = 1;
                }
                didBallHitBat = true;
                currentActionState = 4;
            }
        }
        else if ((other.gameObject.name == "Stump1Collider" && currentBallStatus == "bowling") || (isReplayModeActive && summarySaved == "bowled"))
        {
            if (isReplayModeActive && summarySaved != "bowled")
            {
                return;
            }
            ActivateColliders(boolean: false);
            ActivateStadiumAndSkybox(boolean: true);
            currentBallStatus = "bowled";
            if (!isReplayModeActive)
            {
                summarySaved = "bowled";
                ballRayCastConnectionZPositionSaved = raycastAnchorBallTransform.position.z;
            }
            else if (isReplayModeActive)
            {
                replayActionStatus = "bowledSlowDown";
            }
            float x = matchBallTransform.position.x;
            StumpAnimation(stumpLeft, x);
            if (Singleton<GameData>.instance != null && !isReplayModeActive)
            {
                Singleton<GameData>.instance.PlayGameSound("Bowled");
                Singleton<GameData>.instance.PlayGameSound("Cheer");
            }
            SetCurrentBatsmanAnimSpeed(1f);

            if (isOversteppedDelivery || isLineFreeHitActive)
            {
                mainUmpireAnim.CrossFade("Crouch_toNotOut_New");
            }
            else
            {
                mainUmpireAnim.Play("Out");
            }
            ZoomCameraToUmpire();
            makeFieldersToCelebrate(null);
        }
        else if (other.gameObject.name == "Stump2Collider" && currentBallStatus == "shotSuccess")
        {
            float x2 = matchBallTransform.position.x;
            StumpAnimation(stumpRight, x2);
            if (Singleton<GameData>.instance != null && !isReplayModeActive)
            {
                Singleton<GameData>.instance.PlayGameSound("Bowled");
            }
        }
        else if (other.gameObject.name == "Board" && currentBallStatus == "shotSuccess")
        {
            BallRebouncesFromBoundary();
        }
        else if (currentBallStatus == "bowling" && (other.gameObject.name == "LeftLowerLeg" || other.gameObject.name == "RightLowerLeg" || other.gameObject.name == "LeftUpperLeg" || other.gameObject.name == "RightUpperLeg" || (isReplayModeActive && summarySaved == "onPads")))
        {
            if (isReplayModeActive && summarySaved != "onPads")
            {
                return;
            }
            currentBallStatus = "onPads";
            currentRunner.transform.eulerAngles = new Vector3(currentRunner.transform.eulerAngles.x, 0f, currentRunner.transform.eulerAngles.z);

            runnerAnim.Play("BackToCreaseNew");

            if (isBatsmanConfident)
            {
                DecreaseConfidenceLevel(currentBallStatus);
            }
            ActivateColliders(boolean: false);
            ActivateStadiumAndSkybox(boolean: true);
            if (currentBatsmanAnimation != string.Empty)
            {
                SetCurrentBatsmanAnimSpeed(1f);
            }
            HideBowlingSpot();
            ShowFullTossSpot(_Value: false);
            if (!isReplayModeActive)
            {
                summarySaved = "onPads";
                padColliderSaved = other;
                _ballAngle = _ballAngle + 180f + (float)DetRange(-20, 20);   // lockstep: seeded — fence/pad deflections were per-client random (a known cross-screen diverger)
                ballAngleAfterHittingPadsSaved = _ballAngle;
                ballRayCastConnectionZPositionSaved = raycastAnchorBallTransform.position.z;
            }
            else
            {
                _ballAngle = ballAngleAfterHittingPadsSaved;
            }
            _ballAngle %= 360f;
            horizontalVelocity *= 0.05f;
            angleChangeRate *= 1.5f;
            arcHeight *= 0.5f;
            applyBallFriction = true;
            if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
            {
                // MP LBW fix: compute + relay the LBW/appeal decision to the bowling client BEFORE the
                // "onPads" ball-angle RPC below. CmdLBWDecision must be SENT FIRST so that, on the bowling
                // client, RPC_LBWDecision (which now sets hasLBWAppeal/initialUmpireDecision) is processed
                // before the relayed currentBallStatus=="onPads" drives ShowFielder10 -> the lbwAppeal
                // review flow. Mirror delivers in send order on the reliable channel, so order matters here.
                if ((temporaryPosition.y < 0.7f && Mathf.Abs(stumpImpactSpot.transform.position.x) < 0.2f))
                {
                    CONTROLLER.LBWAPPEAL = true;
                }

                if (bounceCount == 0 && Mathf.Abs(matchBallTransform.position.x) <= 0.1f)
                {
                    CONTROLLER.BALLINLINE = true;
                }

                if (matchBallTransform.position.y < 0.6f && Mathf.Abs(stumpImpactSpot.transform.position.x) < 0.12f && CONTROLLER.BALLINLINE)
                {
                    CONTROLLER.Lbw = true;
                }

                if (GameConstants.isWithAI == false)
                {
                    //    view.RPC("RPC_LBWDecision", RpcTarget.OthersBuffered, CONTROLLER.LBWAPPEAL, CONTROLLER.BALLINLINE, CONTROLLER.Lbw);
                    CricketNetworkManager.instance.CmdLBWDecision(staticVariables.UserProfiledata.user._id, CONTROLLER.LBWAPPEAL, CONTROLLER.BALLINLINE, CONTROLLER.Lbw);  //Photon Addition
                }

                // Pad contact: stop any in-flight "shotSuccess" coroutine and override with correct "onPads" status.
                // No bat launch origin for pads — pass Vector3.zero (TryApplyRemoteBatContact ignores non-bat colliders).
                if (_ballAngleRPCCoroutine != null) StopCoroutine(_ballAngleRPCCoroutine);
                _ballAngleRPCSent = true;
                _ballAngleRPCCoroutine = StartCoroutine(CallRPCChangeBallAngle(_ballAngle, horizontalVelocity, launchAngle, arcHeight, firstBounceDistance, angleChangeRate, batContactHeight, horizontalSpeedAdjustment, currentBallStatus, isEdgeCaught, Vector3.zero));

                //TEST


            }

            if ((temporaryPosition.y < 0.7f && Mathf.Abs(stumpImpactSpot.transform.position.x) < 0.2f && CONTROLLER.PlayModeSelected != 8) || (isReplayModeActive && isLbwAppealSaved) || (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.LBWAPPEAL))
            {
                hasLBWAppeal = true;

                if (bowlerType == "spin" || bowlerType == "fast" || bowlerType == "medium")
                {
                    isBowlerActivationAllowed = false;
                    ShowFielder10(fielder10Status: true, ball10Status: false);
                    ShowBowler(showStatus: false);
                }
                if (!isReplayModeActive)
                {
                    isLbwAppealSaved = hasLBWAppeal;
                }

                if (bowlerType == "fast" && (isOversteppedDelivery || !isLineFreeHitActive))
                {
                    keeperAnim.Play("appealFast");
                }
                if (bowlerType == "medium" && (isOversteppedDelivery || !isLineFreeHitActive))
                {
                    keeperAnim.Play("appealFast");
                }
                else if (bowlerType == "spin" && (isOversteppedDelivery || !isLineFreeHitActive))
                {
                    keeperAnim.Play("appealSpin");
                }
                makeFieldersToCelebrate(null);
                if (Singleton<GameData>.instance != null && !isReplayModeActive)
                {
                    Singleton<GameData>.instance.PlayGameSound("Cheer");
                }
                if ((bounceCount == 0 && Mathf.Abs(matchBallTransform.position.x) <= 0.1f && CONTROLLER.PlayModeSelected != 8) || (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BALLINLINE))
                {
                    isBallInline = true;
                }
                if ((matchBallTransform.position.y < 0.6f && Mathf.Abs(stumpImpactSpot.transform.position.x) < 0.12f && isBallInline && CONTROLLER.PlayModeSelected != 8) || (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.Lbw))
                {
                    isLBW = true;
                }
                if (!isReplayModeActive)
                {
                    isLbwSaved = isLBW;
                }
                else if (isReplayModeActive)
                {
                    isLBW = isLbwSaved;
                }
                initialUmpireDecision = isLBW;
            }
            else
            {
                keeperAnim.Play("waitForBall");
                keeperAnim["waitForBall"].time = keeperAnim["waitForBall"].length;
                keeperAnim["waitForBall"].speed = -1f;
            }
        }

        canShowCountdown = false;
    }

    private void ActivateColliders(bool boolean)
    {
        PrimaryBatCollider.SetActive(boolean);
        SecondaryBatCollider.SetActive(boolean);
        LeftLowerLeg.SetActive(boolean);
        RightLowerLeg.SetActive(boolean);
        LeftUpperLeg.SetActive(boolean);
        RightUpperLeg.SetActive(boolean);
        Stump1Collider.SetActive(boolean);
    }

    // #1 left-handed-batsman contact fix (see the call site in the collider-init block). Converts a bat
    // BoxCollider — which Unity does NOT mirror under the left-handed batsman's negative scale, leaving the
    // collider on the wrong side of the visible bat so no shot connects — into a CONVEX MeshCollider, which
    // Unity DOES mirror. The boxes are unit cubes (size 1,1,1 / center 0) scaled by their transform, so a
    // convex collider built from the object's own unit-cube MeshFilter has the identical collision volume:
    // right-handers (scale +1) are unchanged, left-handers (scale -1) now get a correctly-mirrored collider.
    // Idempotent (no-op once converted / if not a box). DestroyImmediate so the BatCollider cache picks up the
    // new collider in the same init pass.
    private void MakeBatColliderNegativeScaleSafe(GameObject go)
    {
        if (go == null) return;
        BoxCollider box = go.GetComponent<BoxCollider>();
        if (box == null) return;                       // already converted, or not a box-based collider
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return; // need the unit-cube mesh to preserve the volume
        bool wasTrigger = box.isTrigger;
        Mesh cube = mf.sharedMesh;
        UnityEngine.Object.DestroyImmediate(box);
        MeshCollider mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = cube;
        mc.convex = true;                              // convex required before isTrigger
        mc.isTrigger = wasTrigger;
        ConstantsData_M.MpLog($"[GroundController] Converted bat collider '{go.name}' BoxCollider -> convex MeshCollider (negative-scale-safe for left-handed batsmen).");
    }

    public void GameIsPaused(bool pauseStatus)
    {
        if (pauseStatus)
        {
            pausedTimeScale = Time.timeScale;
            if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
            {
                Singleton<GroundController>.instance.UpdateCanBowlerBowl(false);
            }
            Time.timeScale = 0f;
            ActivateColliders(boolean: false);
            return;
        }
        isGamePaused = true;
        if (!CONTROLLER.isQuit)
        {
            if (!canShowCountdown)
            {
                pauseCountdown();
            }
            else if (!CONTROLLER.isFromAutoPlay)
            {
                EnablePauseCountDown();
            }
            else
            {
                CONTROLLER.isFromAutoPlay = false;
                ResetAll();
                EnablePauseCountDown();
            }
        }
        ActivateColliders(boolean: true);
    }

    private void pauseCountdown()
    {
        if (isGamePaused)
        {
            Time.timeScale = pausedTimeScale;
        }
        else
        {
            Time.timeScale = 1f;
        }
        isGamePaused = false;
        destroyCountDown();
        Singleton<Tutorial>.instance.setBoolean();
    }

    private void SetComputerFieldIndex(int _index)
    {
        if (CONTROLLER.noBallFacedBatsmanId != CONTROLLER.StrikerIndex || !isLineFreeHitActive)
        {
            CONTROLLER.computerFielderChangeIndex = _index;
        }
    }

    public void UpdatePreview()
    {
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary.Add("Striker", _batsmanTransform.position);
        dictionary.Add("NonStriker", _runnerTransform.position);
        dictionary.Add("field_01", fielderTransforms[1].position);
        dictionary.Add("field_02", fielderTransforms[2].position);
        dictionary.Add("field_03", fielderTransforms[3].position);
        dictionary.Add("field_04", fielderTransforms[4].position);
        dictionary.Add("field_05", fielderTransforms[5].position);
        dictionary.Add("field_06", fielderTransforms[6].position);
        dictionary.Add("field_07", fielderTransforms[7].position);
        dictionary.Add("field_08", fielderTransforms[8].position);
        dictionary.Add("field_09", fielderTransforms[9].position);
        if (enableShadows)
        {
            dictionary.Add("field_10", shadowTransforms[9].position);
        }
        if (isBallReleased)
        {
            //dictionary.Add("Ball", ballTransform.position);
            dictionary.Add("Ball", temporaryPosition);
        }
        else if (enableShadows)
        {
            dictionary.Add("Ball", shadowTransforms[9].position);
        }
        if (enableShadows)
        {
            dictionary.Add("field_11", shadowTransforms[10].position);
        }
        if (Singleton<GameData>.instance != null)
        {
            Singleton<GameData>.instance.UpdatePreview(dictionary);
        }
    }

    public void initGameIntro()
    {
        introCameraAnchor.transform.eulerAngles = new Vector3(0f, 0f, 0f);
        introCamTransform.parent = introCameraAnchor.transform;
        introCamTransform.localPosition = new Vector3(115f, 80f, 0f);
        introCamTransform.localEulerAngles = new Vector3(40f, -90f, introCamTransform.localEulerAngles.z);
        introCutsceneCamera.fieldOfView = 40f;
        closeUpViewCamera.enabled = false;
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        showPreviewCamera(status: false);
        umpireViewCamera.enabled = false;
        replayViewCamera.enabled = false;
        gameplayCamera.enabled = false;
        introCutsceneCamera.enabled = true;
        showPreviewCamera(status: false);
    }

    public void introCompleted()
    {
        batsmanSkinRenderer.enabled = true;
        RunnerSkinRenderer.enabled = true;
        BatsmanCricketKitSkinRenderer.enabled = true;
        BatsmanBatSkinRenderer.enabled = true;
        RunnerCricketKitSkinRenderer.enabled = true;
        RunnerBatSkinRenderer.enabled = true;
        shouldPlayIntro = false;
        introCutsceneCamera.enabled = false;
        gameplayCamera.enabled = true;
        EnableFielders(boolean: true);
        StopIntroFielderAnimation();
    }

    private void CheckAndHideShadow(GameObject targetGameObject, int arrayIndex)
    {
        if (targetGameObject != null && !targetGameObject.GetComponent<Renderer>().enabled)
        {
            temporaryShadowPosition = shadowTransforms[arrayIndex].position;
            temporaryShadowPosition.y = -100f;
            shadowTransforms[arrayIndex].position = temporaryShadowPosition;
        }
    }

    private void UpdateShadow()
    {
        shadowTransforms[0].position = new Vector3(shadowReferenceTransforms[0].position.x, shadowHeightY, shadowReferenceTransforms[0].position.z);
        CheckAndHideShadow(fielderModels[1], 0);
        shadowTransforms[1].position = new Vector3(shadowReferenceTransforms[1].position.x, shadowHeightY, shadowReferenceTransforms[1].position.z);
        CheckAndHideShadow(fielderModels[2], 1);
        shadowTransforms[2].position = new Vector3(shadowReferenceTransforms[2].position.x, shadowHeightY, shadowReferenceTransforms[2].position.z);
        CheckAndHideShadow(fielderModels[3], 2);
        shadowTransforms[3].position = new Vector3(shadowReferenceTransforms[3].position.x, shadowHeightY, shadowReferenceTransforms[3].position.z);
        CheckAndHideShadow(fielderModels[4], 3);
        shadowTransforms[4].position = new Vector3(shadowReferenceTransforms[4].position.x, shadowHeightY, shadowReferenceTransforms[4].position.z);
        CheckAndHideShadow(fielderModels[5], 4);
        shadowTransforms[5].position = new Vector3(shadowReferenceTransforms[5].position.x, shadowHeightY, shadowReferenceTransforms[5].position.z);
        CheckAndHideShadow(fielderModels[6], 5);
        shadowTransforms[6].position = new Vector3(shadowReferenceTransforms[6].position.x, shadowHeightY, shadowReferenceTransforms[6].position.z);
        CheckAndHideShadow(fielderModels[7], 6);
        shadowTransforms[7].position = new Vector3(shadowReferenceTransforms[7].position.x, shadowHeightY, shadowReferenceTransforms[7].position.z);
        CheckAndHideShadow(fielderModels[8], 7);
        shadowTransforms[8].position = new Vector3(shadowReferenceTransforms[8].position.x, shadowHeightY, shadowReferenceTransforms[8].position.z);
        CheckAndHideShadow(fielderModels[9], 8);
        if (!isBowlerActivationAllowed)
        {
            shadowTransforms[9].position = new Vector3(bowlerShadowTransform.position.x, shadowHeightY, bowlerShadowTransform.position.z);
            CheckAndHideShadow(bowlerModel, 9);
        }
        else if (isBowlerActivationAllowed && (fielder10SkinRenderer.enabled || Singleton<BallSimulationManager>.instance.isShowingSimulation))
        {
            shadowTransforms[9].position = new Vector3(shadowReferenceTransforms[9].position.x, shadowHeightY, shadowReferenceTransforms[9].position.z);
            CheckAndHideShadow(fielder10Model, 9);
        }
        shadowTransforms[10].position = new Vector3(shadowReferenceTransforms[10].position.x, shadowHeightY, shadowReferenceTransforms[10].position.z);
        CheckAndHideShadow(wicketKeeperModel, 10);
        shadowTransforms[11].position = new Vector3(shadowReferenceTransforms[11].position.x, shadowHeightY, shadowReferenceTransforms[11].position.z);
        CheckAndHideShadow(BatsmanModel, 11);
        shadowTransforms[12].position = new Vector3(shadowReferenceTransforms[12].position.x, shadowHeightY, shadowReferenceTransforms[12].position.z);
        CheckAndHideShadow(runnerAppearance, 12);
        shadowTransforms[13].position = new Vector3(shadowReferenceTransforms[13].position.x, shadowHeightY, shadowReferenceTransforms[13].position.z);
        CheckAndHideShadow(mainUmpireAppearance, 13);
        shadowTransforms[14].position = new Vector3(shadowReferenceTransforms[14].position.x, shadowHeightY, shadowReferenceTransforms[14].position.z);
        CheckAndHideShadow(sideUmpireAppearance, 14);
    }

    public void MoveLeftSide(bool boolean)
    {
        if (boolean)
        {
            if (batsmanHand == "right")
            {
                isLeftArrowKeyPressed = true;
            }
            else
            {
                isRightArrowKeyPressed = true;
            }
        }
        else if (batsmanHand == "right")
        {
            isLeftArrowKeyPressed = false;
        }
        else
        {
            isRightArrowKeyPressed = false;
        }
    }

    public void MoveRightSide(bool boolean)
    {
        if (boolean)
        {
            if (batsmanHand == "right")
            {
                isRightArrowKeyPressed = true;
            }
            else
            {
                isLeftArrowKeyPressed = true;
            }
        }
        else if (batsmanHand == "right")
        {
            isRightArrowKeyPressed = false;
        }
        else
        {
            isLeftArrowKeyPressed = false;
        }
    }

    private IEnumerator MyCoroutine(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            MyFunction();
            yield return null;
            elapsedTime += Time.deltaTime;
            if (isRunning)
            {
                yield break;
            }
        }
    }

    private void MyFunction()
    {

        if (hasTakenRun && isBallHit)
        {

            takeRunDuration = Time.time - ballConnectionTiming;
            if (takeRunDuration > (takeRunTimestamp - 0.01f))
            {
                if (!isRunning)
                {
                }
                isRunning = true;
                ////ConstantsData_M.MpLog("!!@@##$$ : " + takeRun);
                Invoke("InItRunNot", 0.8f);
                //TookRun = false;
                takeRunTimestamp = -0.5f;
                takeRunDuration = -1f;
            }
        }
    }

    public void ActivateStadiumAndSkybox(bool boolean)
    {
        objectsToHide.SetActive(boolean);
    }

    public void TurnOffSkins(bool boolean)
    {
        stump1Anim.Play("idle");
        stump2Anim.Play("idle");
        iTween.Stop(introCutsceneCamera.gameObject);
        gameplayCamera.enabled = !boolean;
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        showPreviewCamera(status: false);
        umpireViewCamera.enabled = false;
        slowMotionCamera.enabled = false;
        closeUpViewCamera.enabled = false;
        replayViewCamera.enabled = false;
        introCutsceneCamera.enabled = boolean;
        BallSkinRenderer.enabled = !boolean;
        batsmanSkinRenderer.enabled = !boolean;
        BatsmanCricketKitSkinRenderer.enabled = !boolean;
        BatsmanBatSkinRenderer.enabled = !boolean;
        RunnerCricketKitSkinRenderer.enabled = !boolean;
        RunnerBatSkinRenderer.enabled = !boolean;
        //WicketKeeperCricketKitSkinRendererComponent.enabled = !boolean;
        RunnerSkinRenderer.enabled = !boolean;
        WicketKeeperSkinRenderer.enabled = !boolean;
        BowlerSkinRenderer.enabled = !boolean;
        fielder10SkinRenderer.enabled = !boolean;
        MainUmpireSkinRenderer.enabled = !boolean;
        SideUmpireSkinRenderer.enabled = !boolean;
        Fielder10BallSkinRenderer.enabled = !boolean;
        WicketKeeperBallSkin.enabled = !boolean;
        for (int i = 1; i <= numberOfFielders; i++)
        {
            GameObject gameObject = fielderModels[i];
            gameObject.GetComponent<Renderer>().enabled = !boolean;
            fielderCaps[i].enabled = !boolean;
        }
        HideShadows(boolean);
        if (CONTROLLER.currentInnings == 1 && Singleton<GameData>.instance.isInningsComplete)
        {
            hideBatsmenAndUmpires();
        }
        else if (CONTROLLER.currentInnings == 0 && Singleton<GameData>.instance.isInningsComplete)
        {
            fielder10SkinRenderer.enabled = false;
            batsmanSkinRenderer.enabled = false;
            BatsmanCricketKitSkinRenderer.enabled = false;
            BatsmanBatSkinRenderer.enabled = false;
            RunnerCricketKitSkinRenderer.enabled = false;
            RunnerBatSkinRenderer.enabled = false;
            RunnerSkinRenderer.enabled = false;
            BowlerSkinRenderer.enabled = false;
        }
    }

    private void HideShadows(bool boolean)
    {
        if (boolean)
        {
            if (shadowContainer != null)
            {
                shadowContainer.SetActive(false);
            }
        }
        else if (shadowContainer != null)
        {
            shadowContainer.SetActive(true);
        }
    }

    public void fireworksStarted()
    {
        hideBatsmenAndUmpires();
        gatherFielders();
        currentActionState = 22;
        showPreviewCamera(status: false);
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        umpireViewCamera.enabled = false;
        introCutsceneCamera.enabled = false;
        slowMotionCamera.enabled = false;
        closeUpViewCamera.enabled = false;
        replayViewCamera.enabled = false;
        for (int i = 0; i < fielders.Length; i++)
        {
            if (fielders[i] != null)
            {
                FielderSkinRenderer[i].enabled = true;
                fielderCaps[i].enabled = true;

            }
        }
        for (int j = 1; j <= 9; j++)
        {
            if (!FielderSkinRenderer[j].enabled)
            {
                FielderSkinRenderer[j].enabled = true;
                fielderCaps[j].enabled = true;

            }
        }
        gameplayCamera.enabled = true;
        mainCamTransform.position = new Vector3(48f, 2f, -43f);
        mainCamTransform.eulerAngles = new Vector3(350f, 308f, 0f);
        gameplayCamera.fieldOfView = 35f;
    }

    private void hideBatsmenAndUmpires()
    {
        fielder10SkinRenderer.enabled = false;
        batsmanSkinRenderer.enabled = false;
        BatsmanCricketKitSkinRenderer.enabled = false;
        BatsmanBatSkinRenderer.enabled = false;
        RunnerCricketKitSkinRenderer.enabled = false;
        RunnerBatSkinRenderer.enabled = false;
        //WicketKeeperCricketKitSkinRendererComponent.enabled = !boolean;
        RunnerSkinRenderer.enabled = false;
        MainUmpireSkinRenderer.enabled = false;
        SideUmpireSkinRenderer.enabled = false;
        BowlerSkinRenderer.enabled = false;
    }

    public void AssignTrace(string str)
    {
        traceString = traceString + str + "\n";
    }

    public void ClearTrace()
    {
        traceString = string.Empty;
    }

    public void ToggleDOTweenSequence()
    {
        if (!animationSequence.IsPlaying())
        {
            animationSequence.Play();
        }
        else
        {
            animationSequence.Pause();
        }
    }

    public void SetSecond()
    {
        timerCountdownText.text = multiplayerSecondCounter.ToString();
        multiplayerSecondCounter--;

    }

    private bool checkForMatchComplete(int runInThisBall, int currentMatchWickets)
    {
        int num = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + runInThisBall;
        if (CONTROLLER.PlayModeSelected == 4 && CONTROLLER.InningsCompleted)
        {
            return true;
        }
        if (CONTROLLER.currentInnings == 1 && num >= CONTROLLER.TargetToChase)
        {
            return true;
        }
        if (currentMatchWickets >= CONTROLLER.totalWickets)
        {
            return true;
        }
        return false;
    }

    public void ShowAllPlayers()
    {
        for (int i = 0; i < fielders.Length; i++)
        {
            if (fielders[i] != null)
            {
                FielderSkinRenderer[i].enabled = true;
                fielderCaps[i].enabled = true;

            }
        }
        for (int j = 1; j <= 9; j++)
        {
            if (!FielderSkinRenderer[j].enabled)
            {
                FielderSkinRenderer[j].enabled = true;
                fielderCaps[j].enabled = true;

            }
        }
        batsmanSkinRenderer.enabled = true;
        RunnerSkinRenderer.enabled = true;
        MainUmpireSkinRenderer.enabled = true;
        SideUmpireSkinRenderer.enabled = true;
        BowlerSkinRenderer.enabled = true;
        WicketKeeperSkinRenderer.enabled = true;
        BatsmanCricketKitSkinRenderer.enabled = true;
        BatsmanBatSkinRenderer.enabled = true;
        RunnerCricketKitSkinRenderer.enabled = true;
        RunnerBatSkinRenderer.enabled = true;
        //WicketKeeperCricketKitSkinRendererComponent.enabled = true;
    }

    private void UpdateBatShadow()
    {
        _batShadowHolderTransform.position = new Vector3(_batTopEdgeTransform.position.x, 0f, _batTopEdgeTransform.position.z);
        _batShadowHolderTransform.eulerAngles = new Vector3(0f, 270f - AngleBetweenTwoVector3(batEdgeObject.transform.position, _batTopEdgeTransform.position), 0f);
        Vector3 localScale = _batShadowHolderTransform.localScale;
        localScale.z = DistanceBetweenTwoVector2(_batTopEdgeTransform.gameObject, batEdgeObject) / 0.7f + 0.05f;
        _batShadowHolderTransform.localScale = localScale;
    }

    private void HideBatShadow()
    {
        _batShadowHolderTransform.position = new Vector3(_batTopEdgeTransform.position.x, -3000f, _batTopEdgeTransform.position.z);
    }

    private float DistanceBetweenTwoVector2(Vector3 vector1, Vector3 vector2)
    {
        float num = vector1.x - vector2.x;
        float num2 = vector1.z - vector2.z;
        return Mathf.Sqrt(num * num + num2 * num2);
    }

    //Photon Removal  [PunRPC]
    public void RPC_EnableContinueBtn()
    {
        Singleton<BowlingScoreCard>.instance.ContinueBtn.gameObject.GetComponent<Button>().interactable = true;
    }

    public void EnableContinueBtn()
    {
        if (GameConstants.isWithAI == false)
        {                                                                                                                                                                                                  //Photon Removal
                                                                                                                                                                                                           //view.RPC("RPC_EnableContinueBtn", RpcTarget.OthersBuffered);
            CricketNetworkManager.instance.CmdEnableContinueBtn(staticVariables.UserProfiledata.user._id);
        }

    }

    //Photon Removal  [PunRPC]
    public void RPC_UpdateCanLoadGround(bool CanLoadGround)
    {
        if (CONTROLLER.CanLoadGround == CanLoadGround)
        {
            return;
        }
        CONTROLLER.CanLoadGround = CanLoadGround;

        // Scorecard-stuck fix (#2): the bowling player's BowlingScoreCard.Continue() is GATED on
        // CanLoadGround (delivered here, with only a 3-retry/3s window vs the batting side's 25s). If
        // the bowler tapped Continue before this flag arrived, Continue() returned early and NOTHING
        // re-invoked it when the flag landed → the bowling scorecard stuck while the batting player
        // advanced to the pitch. Resume it here when the flag flips true.
        // This used to call Continue() UNCONDITIONALLY, which silently dismissed a card the bowler had
        // never touched: the opponent's tick opens the bowling card and CanLoadGround flips at almost the
        // same instant, so the over-end bowler selection was destroyed before it could be read (logs 12-08
        // 02:36 — 'panelIndex = 2' followed immediately by the dismiss, no tap involved). It now resumes
        // only a continue the bowler actually asked for; with no pending request the card is left alone.
        if (CanLoadGround && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && Singleton<BowlingScoreCard>.instance != null
            && Singleton<BowlingScoreCard>.instance.scoreCard != null
            && Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy)
        {
            Singleton<BowlingScoreCard>.instance.FlushPendingBowlingContinue();
        }
    }

    public void UpdateCanLoadGround(bool CanLoadGround)
    {
        if (GameConstants.isWithAI == false)
        {
            StartCoroutine(CallRPCMultiple(CanLoadGround));                                                                                                   //Photon Removal
        }
        CONTROLLER.CanLoadGround = CanLoadGround;

        // Batting-side bowling-scorecard-stuck fix: this is where the BATTING client sets its own
        // CanLoadGround true (from BattingScoreCard continue). If the bowler had already tapped Continue
        // before this point, the batting client's RPC_Continue early-returned (gated on CanLoadGround)
        // and the bowling scorecard stayed stuck on the batting screen while the game ran behind it.
        // Now that the ground is ready, flush that deferred dismiss.
        if (CanLoadGround && CONTROLLER.PlayModeSelected == 8
            && Singleton<BowlingScoreCard>.instance != null)
        {
            Singleton<BowlingScoreCard>.instance.FlushPendingBattingContinue();
        }

    }
}
