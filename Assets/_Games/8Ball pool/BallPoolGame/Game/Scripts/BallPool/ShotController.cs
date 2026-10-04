using BallPool.AI;
using BallPool.Mechanics;
using Mirror;
using NetworkManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
namespace BallPool
{
    public class ShotController : MonoBehaviour
    {
        public static bool isControlAvailable = true;
        public enum CueViewMode
        {
            FirstPerson = 0,
            ThirdPerson
        }
        public enum CueMode
        {
            Non = 0,
            TargetingAtCueBall,
            TargetingAtTargetBall,
            TargetingFromTop,
            StretchCue
        }

        private float maxSpeed
        {
            get
            {
                float verticalAngleFactor = cueVerticalAlignment.localRotation.eulerAngles.x / (maxCueVerticalAngle - minCueVerticalAngle) > 0.7f ? 1.0f : 0.0f;
                return Mathf.Lerp(maxCueBallSpeed, cueBallJumpForce, jumpPower * verticalAngleFactor);
            }
        }

        [FormerlySerializedAs("cueControlType")] public CueViewMode cueControlMode;
        [FormerlySerializedAs("cueThirdPersonControlMask")] public LayerMask cueThirdPersonControlLayer;
        [FormerlySerializedAs("cueBallMaxVelocity")] public float maxCueBallSpeed;
        [FormerlySerializedAs("cueBallJumpVelocity")] public float cueBallJumpForce;
        [FormerlySerializedAs("mouseRotationSpeed3D")] public float mouseRotationSpeedFor3D;
        [FormerlySerializedAs("rotationSpeed3D")] public float cueRotationSpeed3D;
        [FormerlySerializedAs("rotationSpeed2D")] public float cueRotationSpeed2D;
        [FormerlySerializedAs("minVertical")] public float minCueVerticalAngle;
        [FormerlySerializedAs("maxVertical")] public float maxCueVerticalAngle;
        [FormerlySerializedAs("rotationSpeedCurve")] public AnimationCurve cueRotationSpeedCurve;
        [FormerlySerializedAs("targetingDisplacementSpeed")] public float targetingMovementSpeed;
        [FormerlySerializedAs("cueSlideSpeed")] public float cueSlidingSpeed;
        [FormerlySerializedAs("lineLength")] public float lineIndicatorLength;
        [FormerlySerializedAs("cueSlidingMaxDisplacement")] public float maxCueSlidingDistance;
        [FormerlySerializedAs("gameManager")] public GameManager poolGameManager;
        [FormerlySerializedAs("aiManager")] public AightBallPoolAIManager ballPoolAIManager;
        [FormerlySerializedAs("uiController")] public GameUIController gameUIManager;
        [FormerlySerializedAs("hitBallClip")] public AudioClip ballHitSound;
        [FormerlySerializedAs("cuePivotAfterShotPosition")] public Transform cuePivotPositionPostShot;
        [FormerlySerializedAs("tableCameraCenter")] public Transform cameraTableCenter;
        [FormerlySerializedAs("cameraThirdPersonPosition")] public Transform thirdPersonCameraPosition;
        [FormerlySerializedAs("clothSpace")] public Transform clothPosition;
        [FormerlySerializedAs("cameraStandartPosition")] public Transform defaultCameraPosition;
        [FormerlySerializedAs("cueTargetingIn3DModePosition")] public Transform cueTargeting3DPosition;
        [FormerlySerializedAs("cameraAimingPosition")] public Transform cameraAimingSpot;
        [FormerlySerializedAs("cueCamera")] public Camera cueCameraView;
        [FormerlySerializedAs("hand")] public Transform playerHand;
        [FormerlySerializedAs("cueBallSimpleLine")] public LineRenderer cueBallLineRenderer;
        [FormerlySerializedAs("targetBallSimpleLine")] public LineRenderer targetBallLineRenderer;
        [FormerlySerializedAs("ballChecker")] public Transform ballCheckerTransform;
        [FormerlySerializedAs("ballCheckerRenderer")] public MeshRenderer ballCheckerMeshRenderer;
        [FormerlySerializedAs("targeting2DManager")] public Targeting2DManager targetingManager2D;
        [FormerlySerializedAs("load2DCue")] public LoadCue2D load2DCueModel;
        [FormerlySerializedAs("load3DCue")] public LoadCueModel3D load3DCueModel;
        [FormerlySerializedAs("load3DTable")] public LoadTableScene3D load3DTableScene;
        [FormerlySerializedAs("forceSlider")] public Slider cueForceSlider;
        [FormerlySerializedAs("haveReplayText")] public Text replayAvailableText;
        [FormerlySerializedAs("waitingOpponent")] public Text opponentWaitText;
        [FormerlySerializedAs("cueBall")] public Ball mainCueBall;
        [FormerlySerializedAs("physicsManager")] public PhysicsHandeler cuePhysicsManager;
        [FormerlySerializedAs("cuePivot")] public Transform cuePivotTransform;
        [FormerlySerializedAs("cueVertical")] public Transform cueVerticalAlignment;
        [FormerlySerializedAs("cueDisplacement")] public Transform cueDisplacementTransform;
        public Transform cueSliderTransform;
        [FormerlySerializedAs("firstMoveSpace")] public Transform initialMoveSpace;
        [FormerlySerializedAs("shotBack")] public Image shotBackImage;
        [FormerlySerializedAs("inst")] public static ShotController shotControllerInstance;
        [FormerlySerializedAs("stretchCue")] public bool isCueStretched = false;
        [FormerlySerializedAs("StartingInHand")] public GameObject cueBallInHandStart;


        public int clothLayerIndex { get; set; }
        public int cueBallClothLayer { get; set; }
        public int tableLayer { get; set; }
        public int ballLayerIndex { get; set; }
        public int layerForCueBall { get; set; }
        public float cueSliderZDisplacement { get; private set; }
        public Vector3 savedTargetBallDirection { get; private set; }
        public BallDetector targetBallDetector { get; set; }
        public float cueForce { get; private set; }



        private float cueBallSize;
        private Vector3 cueBallDisplacement;
        private CueMode cueMode;
        private Quaternion cueVerticalInitialRotation;
        private AudioSource cueBallHitSound;
        private float jumpPower;
        private float cameraZRotation;
        private Vector3 savedCueSliderPos;
        private Vector3 cueDirectionOnScreen;
        private Vector3 lastShotPosition;
        private Vector3 previousCueBallPos;
        private float previousShotForce;
        private Vector3 previousForwardDirection;
        private bool isFrom2D;
        private bool hasSimpleControl = true;
        private float cuePivotYRotation;
        private float localCueVerticalRotationX;
        private Vector2 cueLocalDisplacementXY;
        private float cueSliderLocalZPosition;
        private Vector3 currentBallPosition;
        private Vector3 positionToCheck;
        private Vector3 lerpedBallPosition;
        private Impulse networkImpulse;
        private Vector3 shotStartCueBallPosition;
        private Vector3 shotStartCuePivotPosition;
        private float shotStartCuePivotLocalRotationY;
        private float shotStartCueVerticalLocalRotationX;
        private Vector2 shotStartCueDisplacementLocalPositionXY;
        private float shotStartCueSliderLocalPositionZ;
        private float shotStartCueForce;
        private Vector3[] shotStartBallPositions = new Vector3[0];
        private bool isAIShot = false;
        private MouseState currentMouseState;
        private bool isAIEnabled;
        private const float CueLineOriginRebuildEpsilon = 0.0005f;
        private Vector3 lastCueLineRenderOrigin;
        private bool hasCueLineRenderOrigin;


        [System.NonSerialized] public Vector3 strikePoint;

        public bool isInShot
        {
            get;
            private set;
        }
        public bool isMoving
        {
            get;
            private set;
        }
        public bool isAIActionReady
        {
            get;
            private set;
        }
        public bool isBallInHand
        {
            get;
            private set;
        }

        public event System.Action EndAICalculation;
        public event System.Action BallSelected;
        public event System.Action BallDeselected;

        public bool AllowBallUpdateFromNetwork
        {
            get;
            set;
        }

        public bool IsCueModified
        {
            get
            {
                if (cuePivotYRotation != cuePivotTransform.localRotation.eulerAngles.y || localCueVerticalRotationX != cueVerticalAlignment.localRotation.eulerAngles.x ||
                    cueLocalDisplacementXY != new Vector2(cueDisplacementTransform.localPosition.x, cueDisplacementTransform.localPosition.y) || cueSliderLocalZPosition != cueSliderTransform.localPosition.z)
                {
                    cuePivotYRotation = cuePivotTransform.localRotation.eulerAngles.y;
                    localCueVerticalRotationX = cueVerticalAlignment.localRotation.eulerAngles.x;
                    cueLocalDisplacementXY = new Vector2(cueDisplacementTransform.localPosition.x, cueDisplacementTransform.localPosition.y);
                    cueSliderLocalZPosition = cueSliderTransform.localPosition.z;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool BallStateChanged
        {
            get
            {
                if (Vector3.Distance(positionToCheck, mainCueBall.position) > 0.1f * mainCueBall.radius)
                {
                    positionToCheck = mainCueBall.position;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool StateChanged
        {
            get
            {
                if (!enabled)
                {
                    return false;
                }
                if (lastShotPosition != strikePoint || previousShotForce != cueForce || previousForwardDirection != cueSliderTransform.forward || previousCueBallPos != mainCueBall.position)
                {
                    lastShotPosition = strikePoint;
                    previousShotForce = cueForce;
                    previousForwardDirection = cueSliderTransform.forward;
                    previousCueBallPos = mainCueBall.position;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        void Awake()
        {
            if (shotControllerInstance == null)
            {
                shotControllerInstance = this;

            }

            savedTargetBallDirection = Vector3.zero;

            isAIEnabled = false;
            if (ProductLines.lineLength != 0.0f)
            {
                lineIndicatorLength = ProductLines.lineLength;
            }
            isAIActionReady = false;
            cueBallSize = 0.5f * mainCueBall.transform.lossyScale.x;
            clothLayerIndex = 1 << LayerMask.NameToLayer("Cloth");
            cueBallClothLayer = 1 << LayerMask.NameToLayer("CueBallBase");
            tableLayer = 1 << LayerMask.NameToLayer("Board");
            ballLayerIndex = 1 << LayerMask.NameToLayer("Ball");
            layerForCueBall = 1 << LayerMask.NameToLayer("CueBall");

            cueVerticalInitialRotation = cueVerticalAlignment.localRotation;
            opponentWaitText.enabled = false;
            AllowBallUpdateFromNetwork = false;
            currentBallPosition = mainCueBall.position;
            lerpedBallPosition = mainCueBall.position;
            cuePivotYRotation = cuePivotTransform.localRotation.eulerAngles.y;
            localCueVerticalRotationX = cueVerticalAlignment.localRotation.eulerAngles.x;
            cueLocalDisplacementXY = new Vector2(cueDisplacementTransform.localPosition.x, cueDisplacementTransform.localPosition.y);
            cueSliderLocalZPosition = cueSliderTransform.localPosition.z;


            isInShot = false;
            isMoving = false;
            currentMouseState = MouseState.Down;

            if (cueControlMode == CueViewMode.FirstPerson)
            {
                cueCameraView.transform.position = defaultCameraPosition.position;
                cueCameraView.transform.rotation = defaultCameraPosition.rotation;
            }
            else if (cueControlMode == CueViewMode.ThirdPerson)
            {
                cueCameraView.transform.parent = cameraTableCenter;
                cueCameraView.transform.position = thirdPersonCameraPosition.position;
                cueCameraView.transform.LookAt(cameraTableCenter.position);
            }

            gameUIManager.OnShotExecuted += (bool follow) =>
            {
                isAIActionReady = false;
                if (follow)
                {
                    isInShot = true;
                    isMoving = true;
                    isAIShot = false;
                    StartCoroutine("DelayAndStartShot");
                }
                else if ((enabled || BallPoolGameLogic.playMode == PlayMode.PlayerAI) && !cuePhysicsManager.IsInMove)
                {
                    cueForce = Mathf.Clamp01(-cueSliderZDisplacement / maxCueSlidingDistance);
                    cueMode = CueMode.Non;
                    if (!isInShot && cueForce > 0.03f && (!gameUIManager.isShotOnRelease || isAIShot))
                    {
                        isInShot = true;
                        isMoving = true;
                        isAIShot = false;
                        StartCoroutine("DelayAndStartShot");
                    }
                }
            };

            if (BallPoolGameLogic.playMode == PlayMode.Replay)
            {
                cuePhysicsManager.OnCueStrike += StartReplayShotPhysics;
                cuePhysicsManager.OnShotComplete += EndReplayShotPhysics;
                playerHand.gameObject.SetActive(false);
                enabled = false;
                int replayDataCount = cuePhysicsManager.ReplayManager.GetReplayDataCount();
                if (replayDataCount == 0)
                {
                    shotBackImage.enabled = true;
                    replayAvailableText.enabled = true;
                }
                else
                {
                    shotBackImage.enabled = false;
                }
                ballCheckerTransform.gameObject.SetActive(false);
                if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
                {
                    cuePivotTransform.GetComponentInChildren<MeshRenderer>().enabled = false;
                }
                return;
            }



            isBallInHand = false;
            //Photon Removal  PunNetwork.instance.CallingShadowToggleMethod(isBallInHand);
            ResetChanges();

            // Place the cue pivot on the cue ball AND aim it toward the table centre
            // (i.e. toward the rack) at game start. OnSetState() would normally set both
            // position and rotation, but it is wired to cuePhysicsManager.OnGameStateSet
            // which is declared and never invoked — so without this the cue stick starts
            // at a stale vertical angle (down the short axis) instead of behind the cue
            // ball aligned with the aim. cuePivotYRotation is synced so the aiming system
            // holds this orientation. Both clients run this deterministically.
            // ONLINE ONLY: offline/AI keeps the scene-authored start rotation (exactly down
            // the long axis). Deriving the aim from the runtime ball position made the
            // offline start stick drift off-straight when the ball settled a hair off the
            // centre line during init (manual Physics.Simulate steps). Gate on the live
            // network session (same check GameManager.Start uses for isOnLine) because
            // BallPoolGameLogic.isOnLine itself is not refreshed until after Awake.
            cuePivotTransform.position = mainCueBall.position;
            if ((NetworkClient.active || NetworkServer.active) && cameraTableCenter != null)
            {
                Vector3 cueAimTarget = new Vector3(cameraTableCenter.position.x, cuePivotTransform.position.y, cameraTableCenter.position.z);
                if ((cueAimTarget - cuePivotTransform.position).sqrMagnitude > 0.0001f)
                {
                    cuePivotTransform.LookAt(cueAimTarget);
                    cuePivotYRotation = cuePivotTransform.localRotation.eulerAngles.y;
                }
            }
            isFrom2D = false;
            savedCueSliderPos = cueSliderTransform.localPosition;
            cuePhysicsManager.OnGameStateSet += OnSetState;
            cuePhysicsManager.OnCueStrike += OnStartShot;
            cuePhysicsManager.OnBallEjectedFromPocket += BallExitFromPocketHandler;

            ballPoolAIManager.OnStartCalculateAI += OnStartCalculateAI;
            ballPoolAIManager.OnEndCalculateAI += OnEndCalculateAI;

            cueBallHitSound = gameObject.AddComponent<AudioSource>();
            cueBallHitSound.playOnAwake = false;

            BallPoolGameManager.instance.OnShotEnded += OnShotEnded;

            if (BallPoolGameLogic.isOnLine || NetworkServer.active || NetworkClient.active)
            {
                cuePhysicsManager.OnReplaySegmentSaved += HandlePhysicsSaveAndReplayStart;
            }


            EstimateShot(true);//RAR

        }

        void HandlePhysicsSaveAndReplayStart(string impulse)
        {
            Debug.Log("Control from network " + BallPoolGameLogic.controlFromNetwork);
            if (BallPoolGameLogic.controlFromNetwork)
            {
                if (cuePhysicsManager.IsInMove || isMoving || isInShot)
                {
                    Debug.LogWarning("[8Ball][NetworkReplay] Ignored StartReplayShot while a replay/shot is already active.");
                    return;
                }
                networkImpulse = DataManager.ImpulseFromString(impulse);
                Debug.Log("PhysicsManager_OnSaveEndStartReplay");
                isMoving = true;
                isInShot = true;
                StartCoroutine("DelayAndStartShot");
            }
        }


        void StartReplayShotPhysics(string data)
        {
            shotBackImage.enabled = true;
        }

        void EndReplayShotPhysics(string data)
        {

            int replayNumber = gameUIManager.replayNumberValue;
            int replayDataCount = cuePhysicsManager.ReplayManager.GetReplayDataCount();
            if (replayDataCount != 1 && replayNumber < replayDataCount - 1)
            {
                replayNumber++;
            }
            else
            {
                replayNumber = 0;
            }
            // Constants_M.LogInfo("replayNumber " + replayNumber + "  " + replayDataCount);
            gameUIManager.ApplyReplaySettings(replayNumber, replayDataCount == 1);
            shotBackImage.enabled = false;
        }

        void Start()
        {
            StartCoroutine(AssignControl());
        }

        void OnEnable()
        {
            InputOutput.OnMouseState += OnMouseState;
        }

        public void ToggleJumpState(Toggle value)
        {
            jumpPower = value.isOn ? 1.0f : 0.0f;
        }
        public void ActivateControl(bool value)
        {
            if (cueForceSlider != null)
            {
                cueForceSlider.value = 0.0f;
                cueForce = 0.0f;
                cueForceSlider.enabled = value;
            }

            // Turn-start must never inherit a PRELOADED shot. The block above zeroes cueForce and the
            // slider UI but NOT cueSliderZDisplacement; the post-scratch turn-start branch below skips
            // AlignCueForBreakAndAim and then calls EstimateShot(true), which RECOMPUTES cueForce from the
            // leftover cueSliderZDisplacement (so cueForce > 0.03 again). A stray pointer-up then auto-fires
            // a real StartSimulate on the freshly-activated player (see OnMouseState); that no-aim shot
            // fouls -> needToChangeTurn -> the server flips the turn -> the next holder inherits the SAME
            // loaded pullback and phantom-shoots too -> the multiplayer ~1s turn ping-pong. Zero the
            // pullback on every gain-of-control so no phantom shot can be generated. (The non-scratch turn
            // start already does this via AlignCueForBreakAndAim; this also covers the post-scratch /
            // ball-in-hand branch, which does not.)
            if (value)
            {
                cueSliderZDisplacement = 0.0f;
                cueSliderTransform.localPosition = Vector3.zero;
                savedCueSliderPos = cueSliderTransform.localPosition;
                cueForce = 0.0f;
                isCueStretched = false;
                currentMouseState = MouseState.Up;
                // Gaining control must never inherit the reconnect cue suppression — it is a
                // watcher-side state and this client is now the shooter.
                networkCueHiddenForBallInHandDrag = false;
            }

            if (!value)
            {
                localCueAimRebuildSeq++;
                playerHand.gameObject.SetActive(false);
            }
            if (!isMoving && !isInShot && shotBackImage != null)
            {
                shotBackImage.enabled = false;
            }
            enabled = value;
            if (isBallInHand)
            {
                DeselectBall();
            }
            if (cueMode == CueMode.TargetingAtCueBall)
            {
                ResetCueTargeting();
            }
            if (BallPoolGameLogic.isOnLine && value)
            {
                cueMode = CueMode.Non;
                RestoreCueRigForControlStart(false);
            }
            StartCoroutine(AssignControl());
            if (BallPoolGameLogic.isOnLine && AightBallPoolNetworkGameAdapter.isSameGraphicsMode)
            {
                if (AightBallPoolNetworkGameAdapter.is3DGraphics)
                {
                    StartCoroutine(load3DCueModel.ApplyCue2DTextureOnTurnChange(value));
                }
                else
                {
                    StartCoroutine(load2DCueModel.ApplyCue2DTextureOnTurnChange(value));
                }
            }
            // Issue 4 — turn-start cue RE-SHOW. The ball-in-hand handler (OnMouseState) hides the
            // whole cue rig on pointer-down and only re-shows it on pointer-up; if that pointer-up
            // is missed (ball dropped off a raycastable surface, shotBackImage early-return, or the
            // turn/control flips mid-drag) the next active player is left with NO visible/usable
            // stick. So when this client gains control and is not actively dragging the ball,
            // authoritatively re-enable the whole cue rig.
            if (value && !isBallInHand)
            {
                SwitchCue(true);
                CuePivotStick.SetActive(true);
                ballCheckerBoth.SetActive(true);
                cueline.SetActive(true);
                targetline.SetActive(true);
                // MULTIPLAYER: also route through the choke point so the indicator line comes up and
                // is rebuilt WITH the stick — a turn start can never hand over a stick with no line.
                // Offline/AI keeps exactly the four-object re-show above, untouched.
                if (BallPoolGameLogic.isOnLine)
                    SetCueRigVisible(true);
                if (BallPoolGameLogic.isOnLine && MyEightBallNetwork.Instance != null)
                    MyEightBallNetwork.Instance.CmdSwitchCueState(true);
            }
            // Online: deterministically place + aim the cue only when THIS client gains control.
            // The watcher gets the same pose from ForceSendCueState below. Running this on
            // ActivateControl(false) consumed the post-scratch respot reset on the wrong side,
            // leaving the actual next shooter with the vertical/stale stick. EXCEPT right after a scratch
            // (cueBallInPocket): then the cue ball is ball-in-hand and the player drags it, so a
            // neutral table-centre re-aim would fight the placement — skip the re-aim in that case
            // and let the placement drive the cue. (Note: gating on cueBallInHand would be wrong —
            // it is also true at the opening break, where the deterministic aim IS wanted.)
            if (!value)
            {
                cueBallRespottedAimPending = false;
            }
            if (BallPoolGameLogic.isOnLine && value)
            {
                bool cueBallInPocket = AightBallPoolGameLogic.gameState != null && AightBallPoolGameLogic.gameState.cueBallInPocket;
                if (!cueBallInPocket)
                {
                    AlignCueForBreakAndAim(ConsumeRespottedCueAimPending());
                }
                else if (value)
                {
                    // POST-SCRATCH (ball-in-hand) turn start on the NEW shooter: give the cue the SAME
                    // sensible DEFAULT aim as the break / a normal turn start — horizontal, pointing into
                    // the table centre. Previously this re-aim was skipped ("would fight placement"), which
                    // left the stick VERTICAL with the aim line running off the table until the player
                    // manually rotated. AlignCueForBreakAndAim only sets the pose ONCE (it also refreshes
                    // the cached sync fields + EstimateShot), so the player can still freely drag-place and
                    // re-aim from there — it does not fight the placement. Then broadcast the respot so the
                    // scratcher's screen converges to the SAME position instead of its own independent one.
                    if (mainCueBall.position.y > -0.1f)
                    {
                        AlignCueForBreakAndAim(true);
                        cueBallRespottedAimPending = false;
                    }
                    if (mainCueBall.position.y > -0.1f && MyEightBallNetwork.Instance != null)
                        MyEightBallNetwork.Instance.SetBallPosition(mainCueBall.position);
                }
                if (value)
                {
                    ForceSendCueState();
                    StartCoroutine(RebuildLocalCueAimWhenReady(++localCueAimRebuildSeq));
                }
            }
        }

        private bool _isOpponentReady = false;
        public bool isOpponentReady { get { return _isOpponentReady; } }

        public void OpponentReadyToPlay()
        {
            _isOpponentReady = true;
            if (AightBallPoolNetworkGameAdapter.isSameGraphicsMode)
            {
                //int number = (AightBallPoolPlayer.mainPlayer.coins == AightBallPoolPlayer.otherPlayer.coins) ? 0 : (AightBallPoolPlayer.mainPlayer.coins > AightBallPoolPlayer.otherPlayer.coins ? 1 : 2);

                if (AightBallPoolNetworkGameAdapter.is3DGraphics)
                {
                    load3DCueModel.InitializeOnStart();
                    load3DTableScene.HandleStart();
                    //StartCoroutine(load3DTable.SetTable3DTextureOnStartGame(number));
                }
                else
                {
                    //load2DCue.OnStart();
                    //load2DTable.OnStart();
                    //StartCoroutine(load2DTable.SetTable2DTextureOnStartGame(number));
                }
            }
        }
        public void AssignOpponentCueURL(string url)
        {
            if (AightBallPoolNetworkGameAdapter.is3DGraphics)
            {
                StartCoroutine(load3DCueModel.AssignOpponentCuePath(url));
            }
            else
            {
                StartCoroutine(load2DCueModel.AssignOpponentCuePath(url));
            }
        }
        public void AssignOpponentTableURLs(string boardURL, string clothURL, string clothColor)
        {
            if (AightBallPoolNetworkGameAdapter.is3DGraphics)
            {
                StartCoroutine(load3DTableScene.AssignOpponentTableAssets(boardURL, clothURL, clothColor));
            }
            else
            {
                // StartCoroutine(load2DTable.SetOpponentTableURLs(boardURL, clothURL, clothColor));
            }
        }
        private IEnumerator AssignControl()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            if (BallPoolGameLogic.isOnLine)
            {
                float waitingTime = 0.0f;

                opponentWaitText.enabled = false;
            }
            if (!enabled && !BallPoolPlayer.mainPlayer.myTurn && BallPoolGameLogic.playMode != PlayMode.HotSeat && BallPoolGameLogic.playMode != PlayMode.Replay)
            {
                shotBackImage.enabled = true;
            }
            else
            {
                shotBackImage.enabled = false;
            }
        }
        void OnDisable()
        {
            InputOutput.OnMouseState -= OnMouseState;
        }

        void OnDestroy()
        {
            cuePhysicsManager.OnCueStrike -= OnStartShot;
            cuePhysicsManager.OnBallEjectedFromPocket -= BallExitFromPocketHandler;

            cuePhysicsManager.OnCueStrike -= StartReplayShotPhysics;
            cuePhysicsManager.OnShotComplete -= EndReplayShotPhysics;

            ballPoolAIManager.OnStartCalculateAI -= OnStartCalculateAI;
            ballPoolAIManager.OnEndCalculateAI -= OnEndCalculateAI;
        }
        public void TimeEnded()
        {
            //Debug.Log("OnEndTime");
            aiAimRotateSeq++;
            CircularTimeController.instance.audioSource.Stop();
            CircularTimeController.instance.audioSource.clip = null;
            CircularTimeController.instance.audioSource.loop = false;
            CircularTimeController.isSoundPlaying = false;
            isAIActionReady = false;
            cueVerticalAlignment.parent = cuePivotTransform;
            cueVerticalAlignment.localPosition = Vector3.zero;
            cueVerticalAlignment.localRotation = cueVerticalInitialRotation;
            cueDisplacementTransform.localPosition = Vector3.zero;
            ballCheckerTransform.gameObject.SetActive(true);
            targetingManager2D.Resset();
        }
        public void ResetShot()
        {
            "Reset shot".Show();
            StopCoroutine("DelayAndStartShot");
            aiAimRotateSeq++;
            CircularTimeController.isSoundPlaying = false;
            cueSliderZDisplacement = 0.0f;
            cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderZDisplacement);
            cueSliderTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 0f);
            savedCueSliderPos = cueSliderTransform.localPosition;
            cueForce = Mathf.Clamp01(-cueSliderZDisplacement / maxCueSlidingDistance);
            cuePhysicsManager.HideBallTrajectory();
            ballPoolAIManager.CancelCalculateAI();
        }
        void ResetChanges()
        {
            lastShotPosition = strikePoint;
            previousShotForce = cueForce;
            previousForwardDirection = cueSliderTransform.forward;
            previousCueBallPos = mainCueBall.position;
        }
        public void ReduceAICount()
        {
            if (isAIEnabled && BallPoolPlayer.mainPlayer.myTurn && BallPoolGameLogic.playMode == PlayMode.OnLine)
            {
                //Constants_M.LogInfo("DecreaseAICount");
                ProductAI.aiCount--;
                poolGameManager.SetAICount();
            }
            isAIEnabled = false;
        }
        public IEnumerator DelayAndStartShot()
        {
            Debug.Log("DelayAndStartShot " + BallPoolGameLogic.controlInNetwork + BallPoolGameLogic.playMode);
            // Phase 1b: server-authoritative shoot gate. A shooter may only fire when the SERVER has
            // authorized it (its turn + canShoot + an aiming phase — LocalPlayerCanShoot()). The watcher's
            // REPLAY (controlFromNetwork) is exempt: it replays the shooter's already-approved shot, it is
            // not initiating one. Offline/AI (LocalPlayerCanShoot returns true) is unaffected.
            if (BallPoolGameLogic.isOnLine && !BallPoolGameLogic.controlFromNetwork
                && MyEightBallNetwork.Instance != null && !MyEightBallNetwork.Instance.LocalPlayerCanShoot())
            {
                Debug.LogWarning("[8Ball][ControlState] Shot blocked — server has not authorized this player to shoot (not your turn / a shot is still resolving).");
                isInShot = false;
                isMoving = false;
                yield break;
            }

            if (BallPoolGameLogic.isOnLine && !BallPoolGameLogic.controlFromNetwork
                && BallPoolGameManager.instance != null
                && !BallPoolGameManager.instance.TryCommitTurnTimerForShot())
            {
                Debug.LogWarning("[8Ball][TurnTimer] Shot blocked — the local turn deadline had already expired before shot commit.");
                isInShot = false;
                isMoving = false;
                yield break;
            }

            if (BallPoolPlayer.mainPlayer.myTurn && BallPoolGameLogic.playMode != PlayMode.HotSeat)
            {
                ProductLines.OnShot(ref lineIndicatorLength);
            }
            ReduceAICount();

            cuePhysicsManager.shotDuration = 0.0f;
            cuePhysicsManager.IsEndFromNetwork = false;
            playerHand.gameObject.SetActive(false);
            isInShot = true;
            isMoving = true;
            shotBackImage.enabled = true;

            float checkTime = 0.0f;
            if (!BallPoolGameLogic.controlFromNetwork)
            {
                while (checkTime < 1.0f && cueSliderTransform.localPosition.z < 0.0f)
                {
                    checkTime += Time.fixedDeltaTime;
                    cueSliderTransform.localPosition += Vector3.forward * cueForce;
                    yield return new WaitForFixedUpdate();
                }
                cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            }

            yield return new WaitForSeconds(3.0f * Time.fixedDeltaTime);
            Impulse impulse = new Impulse();
            if (BallPoolGameLogic.playMode == PlayMode.Replay)
            {
                impulse = cuePhysicsManager.ReplayManager.GetImpulse(gameUIManager.replayNumberValue);
                //Debug.Log("set impulse " + impulse.impulse);
                CaptureShotStartSnapshot();
                cuePhysicsManager.ApplyImpulse(impulse);
            }
            else
            {
                if (BallPoolGameLogic.controlFromNetwork)
                {
                    impulse = networkImpulse;
                    yield return new WaitForSeconds(1.0f);
                    while (checkTime < 1.0f && cueSliderTransform.localPosition.z < 0.0f)
                    {
                        checkTime += Time.fixedDeltaTime;
                        cueSliderTransform.localPosition += Vector3.forward * cueForce;
                        yield return new WaitForFixedUpdate();
                    }
                    cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                    "DelayAndStartShot 2".Show();
                }
                else
                {
                    Vector3 forceVector = cueForce * (maxSpeed * cuePhysicsManager.BallMass) * ((Vector3.ProjectOnPlane(cueSliderTransform.forward, Vector3.up) + 0.5f * Vector3.Project(cueSliderTransform.forward, Vector3.up)).normalized);
                    impulse = new Impulse(strikePoint, forceVector);
                }
                CaptureShotStartSnapshot();
                cuePhysicsManager.ApplyImpulse(impulse);
                cuePhysicsManager.ReplayManager.SaveImpulse(impulse);
            }

            if (BallPoolGameLogic.controlInNetwork)
            {
                yield return null;
                cuePhysicsManager.StartReplayShot(DataManager.ImpulseToString(impulse));
            }
            cuePhysicsManager.InitiateShot(mainCueBall.listener);

            if (BallPoolGameLogic.playMode == PlayMode.Replay)
            {
                foreach (var ball in poolGameManager.allBalls)
                {
                    ball.SrartFollow();
                }
            }

            cueForceSlider.value = 0.0f;
            playerHand.gameObject.SetActive(false);
            isInShot = false;

            // Cue stick must retract the instant the shot is struck. The original 0.7s wait left the
            // stick standing on the ball for ~0.7s after the hit (and longer on the watcher), so
            // online moved to a next-frame retract on 2026-06-22 and offline was put back on the
            // 0.7s by the AI-mode gate on 2026-07-22. Offline now retracts on the next frame too
            // (user, 2026-09-16): the stick should leave the table as soon as it strikes. This only
            // changes WHEN the retract below runs - the retract itself is unchanged, the rig is
            // re-parented to cuePivotPositionPostShot exactly as before and is never hidden.
            yield return null;
            if (cuePhysicsManager.IsInMove)
            {
                cueVerticalAlignment.parent = cuePivotPositionPostShot;
                cueVerticalAlignment.localPosition = Vector3.zero;
                cueVerticalAlignment.localRotation = Quaternion.identity;
                cueDisplacementTransform.localPosition = Vector3.zero;
                targetingManager2D.Resset();
            }
        }

        void BallExitFromPocketHandler(BallDetector listener, PocketDetector pocket, BallExitType exitType, bool inSimulate)
        {
            if (listener == mainCueBall.listener)
            {
                mainCueBall.isActive = false;
            }
        }

        // WATCHER ball-in-hand follow speed (multiplayer only — the shooter's own ball is driven by
        // local input). Vector3.Lerp with (speed * dt) is exponential smoothing: it closes ~63% of the
        // gap in (1 / speed) seconds and NEVER catches a target that keeps moving. At the old 5.0 that
        // is a 0.2s time constant, so while the opponent kept dragging, the watcher's cue ball sat
        // visibly BEHIND the shooter's — tester: "draging krty rahein to kabhi kabhi dono side white
        // ball ki position change hojati". 15 cuts that steady-state lag to about a third while still
        // smoothing network jitter. Serialized so the feel can be tuned in the Inspector.
        // NOTE: this does NOT affect the final placement — the drop (SetBallPositionViaNetwork) snaps
        // directly with no lerp, so both clients always agree once the ball is released.
        [SerializeField] private float networkCueBallFollowSpeed = 15.0f;

        // AI aim swing: instead of snapping to the computed angle, the stick sweeps there at this
        // speed (degrees/second) and only strikes once it arrives. MoveTowardsAngle picks the short
        // way around, so it turns left or right by whichever side reaches the target angle first.
        [SerializeField] private float aiAimRotationSpeed = 180.0f;
        private int aiAimRotateSeq;

        void OnEndCalculateAI(BallPoolAIManager aiManager)
        {
            if (aiManager.haveExaption)
            {
                // Constants_M.LogInfo("haveExaption");
            }
            // Constants_M.LogInfo("Trying to do something");

            mainCueBall.position = aiManager.info.shotBallPosition;
            mainCueBall.OnState(BallState.SetState);

            ballCheckerTransform.position = aiManager.info.aimpoint;
            ballCheckerTransform.gameObject.SetActive(true);
            Vector3 impulseVector = aiManager.info.impulse * (aiManager.info.aimpoint - aiManager.info.shotBallPosition).normalized;
            cuePivotTransform.position = aiManager.info.shotBallPosition;
            StartCoroutine(RotateCueToAIAimAndShoot(aiManager, impulseVector, ++aiAimRotateSeq));
        }

        private IEnumerator RotateCueToAIAimAndShoot(BallPoolAIManager aiManager, Vector3 impulseVector, int seq)
        {
            Vector3 worldDir = Vector3.ProjectOnPlane(impulseVector.normalized, Vector3.up);
            Vector3 localDir = cuePivotTransform.parent != null
                ? cuePivotTransform.parent.InverseTransformDirection(worldDir)
                : worldDir;
            localDir.y = 0.0f;
            if (localDir.sqrMagnitude > 0.0001f && aiAimRotationSpeed > 0.0f)
            {
                // Neutral pose while swinging: no pullback, no spin offset yet.
                cueSliderTransform.localPosition = Vector3.zero;
                cueSliderZDisplacement = 0.0f;
                cueDisplacementTransform.localPosition = Vector3.zero;

                float targetY = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                float currentY = cuePivotTransform.localRotation.eulerAngles.y;
                while (Mathf.Abs(Mathf.DeltaAngle(currentY, targetY)) > 0.1f)
                {
                    if (seq != aiAimRotateSeq)
                        yield break;
                    currentY = Mathf.MoveTowardsAngle(currentY, targetY, aiAimRotationSpeed * Time.deltaTime);
                    cuePivotTransform.localRotation = Quaternion.Euler(0.0f, currentY, 0.0f);
                    cuePivotYRotation = currentY;
                    EstimateShot(true);
                    yield return null;
                }
            }
            if (seq != aiAimRotateSeq)
                yield break;
            ApplyAIShot(aiManager, impulseVector);
        }

        // Original OnEndCalculateAI tail — runs once the swing has arrived at the AI's angle.
        private void ApplyAIShot(BallPoolAIManager aiManager, Vector3 impulseVector)
        {
            strikePoint = aiManager.info.shotPoint;
            cuePivotTransform.LookAt(cuePivotTransform.position + Vector3.ProjectOnPlane(impulseVector.normalized, Vector3.up));
            cueSliderTransform.forward = impulseVector.normalized;
            cueDisplacementTransform.position = strikePoint;
            float displacement = (aiManager.info.impulse / cuePhysicsManager.BallMaxVelocity) * maxCueSlidingDistance;
            cueSliderZDisplacement = -displacement;

            cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, -displacement);
            "OnEndCalculateAI".Show();
            ResetChanges();
            EstimateShot(true);
            Impulse impulse = new Impulse(strikePoint, cueForce * (maxSpeed * mainCueBall.listener.body.mass) * cueSliderTransform.forward);
            cuePhysicsManager.ApplyImpulse(impulse);

            if (BallPoolGameLogic.controlInNetwork)
            {

            }
            if (EndAICalculation != null)
            {
                EndAICalculation();
            }
            if (BallPoolGameLogic.playMode != PlayMode.PlayerAI || BallPoolPlayer.mainPlayer.myTurn)
            {
                if (gameUIManager.isShotOnRelease)
                {
                    CircularTimeController.instance.audioSource.Stop();
                    CircularTimeController.instance.audioSource.clip = null;
                    CircularTimeController.isSoundPlaying = false;
                    //Debug.Log("StartShot ");
                    isMoving = true;
                    enabled = false;
                    shotBackImage.enabled = true;
                    isInShot = true;
                    isAIShot = false;
                    StartCoroutine("DelayAndStartShot");
                }
                else
                {
                    //Debug.Log("Activate ");
                    enabled = true;
                    shotBackImage.enabled = false;
                    isAIActionReady = true;
                }
            }
        }

        void OnStartCalculateAI(BallPoolAIManager AIManager)
        {
            aiAimRotateSeq++;
            isAIEnabled = true;
            isAIShot = true;
            enabled = false;
            isAIActionReady = false;
            cueVerticalAlignment.localPosition = Vector3.zero;
            cueVerticalAlignment.localRotation = Quaternion.identity;
            ballCheckerTransform.gameObject.SetActive(false);
        }

        void OnSetState()
        {
            cuePivotTransform.position = mainCueBall.position;
            Vector3 impulse = mainCueBall.impulse.impulse.normalized;
            cuePivotTransform.LookAt(cuePivotTransform.position + Vector3.ProjectOnPlane(impulse, Vector3.up));
            cueSliderTransform.forward = mainCueBall.impulse.impulse.normalized;
            cueDisplacementTransform.position = mainCueBall.impulse.point;
            EstimateShot(true);
        }

        void OnStartShot(string data)
        {
            cueBallHitSound.volume = cueForce;
            cueBallHitSound.PlayOneShot(ballHitSound);
            enabled = false;
            ballCheckerTransform.gameObject.SetActive(false);
            ClearAllLines();
            CircularTimeController.isSoundPlaying = false;
        }


        // Reconnect recovery: clear the local "shot in progress" flag without running the full
        // OnShotEnded turn/aim logic, so a deferred authoritative reconcile can apply immediately.
        public void ClearMovingForReconcile()
        {
            isMoving = false;
        }

        // Phase 3 (codex): an online client's shot ended, but rules are the SERVER's job. Clear ONLY the local
        // moving/settling visual state — OnShotEnded normally clears isMoving, and we intentionally skip
        // OnShotEnded on clients. NO turn change, NO cue respot, NO local control open (all come from the
        // server via RpcApplyShotOutcome / RpcRespotCueBall / currentTurnId + canShoot + the token barrier).
        public void ClearLocalShotSettledFromServer()
        {
            isMoving = false;
            if (BallPoolGameLogic.isOnLine
                && !BallPoolGameLogic.controlFromNetwork
                && MyEightBallNetwork.Instance != null
                && MyEightBallNetwork.Instance.LocalPlayerCanShoot())
            {
                return;
            }
            SuppressCueVisualsWhileShotSettling();
        }

        void OnShotEnded()
        {
            CircularTimeController.instance.audioSource.Stop();
            CircularTimeController.isSoundPlaying = false;
            CircularTimeController.instance.audioSource.clip = null;
            isMoving = false;
            isAIActionReady = false;
            currentBallPosition = mainCueBall.position;
            lerpedBallPosition = currentBallPosition;
            enabled = BallPoolPlayer.mainPlayer.myTurn || BallPoolGameLogic.playMode == PlayMode.HotSeat;
            if (mainCueBall.firstHitInfo.shapeType != ShapeType.Non)
            {
                ballCheckerTransform.position = mainCueBall.firstHitInfo.positionInHit;
            }
            ballCheckerTransform.gameObject.SetActive(true);
            StartCoroutine(AssignControl());

            foreach (var ball in BallPoolGameManager.instance.balls)
            {
                if (!ball.inSpace && !ball.inPocket)
                {
                    bool canReactivate = false;
                    Vector3 ballNewPosition = Vector3.zero;
                    cuePhysicsManager.RepositionBallInCube(ball.radius, clothPosition, clothLayerIndex, ballLayerIndex | layerForCueBall, ref canReactivate, ref ballNewPosition);
                    if (canReactivate)
                    {
                        ball.position = ballNewPosition;
                        ball.isActive = false;
                        ball.inPocket = false;
                        ball.pocketId = -1;
                        ball.hitShapeId = -2;
                        ball.OnState(BallState.ExitFromPocket);
                    }
                }
            }
            bool scratchShot = AightBallPoolGameLogic.gameState != null
                               && AightBallPoolGameLogic.gameState.cueBallInPocket;
            bool cueBallRespotted = false;
            bool shouldComputeLocalScratchRespot = !BallPoolGameLogic.isOnLine || NetworkServer.active;
            if (scratchShot && shouldComputeLocalScratchRespot)
            {
                //Debug.Log("CueBallInPocket");

                bool canReactivate = false;
                Vector3 ballNewPosition = Vector3.zero;
                cuePhysicsManager.RepositionBallInCube(mainCueBall.radius, clothPosition, clothLayerIndex, ballLayerIndex | layerForCueBall, ref canReactivate, ref ballNewPosition);
                if (canReactivate)
                {
                    PocketDetector cuePocket = mainCueBall.listener != null ? mainCueBall.listener.pocket : null;
                    if (cuePocket != null)
                    {
                        cuePhysicsManager.TriggerBallEjectionFromPocket(mainCueBall.listener, cuePocket, true);
                        mainCueBall.listener.pocket = null;
                    }
                    else
                    {
                        Debug.LogWarning("[8Ball][ShotController] Cue ball scratch respot had no pocket reference; continuing respot without pocket-path ejection.");
                    }
                    //Debug.Log("CueBallInPocket canReactivate");
                    mainCueBall.position = ballNewPosition;
                    mainCueBall.isActive = false;
                    mainCueBall.inPocket = false;
                    mainCueBall.pocketId = -1;
                    mainCueBall.hitShapeId = -2;
                    mainCueBall.OnState(BallState.ExitFromPocket);
                    cueBallRespotted = true;
                }
            }
            else if (scratchShot && BallPoolGameLogic.isOnLine)
            {
                // In online play only the server may choose the ball-in-hand respot.
                // Clients keep the cue ball pocketed/invisible until RpcRespotCueBall arrives;
                // otherwise each client briefly shows its own local respot before snapping to
                // the server position.
                SuppressCueVisualsWhileShotSettling();
            }

            // Refresh the position caches AFTER the respot. They were captured at the top of this
            // method while the cue ball was still INSIDE the pocket (y < -0.1) — that stale value
            // (a) let the next force-keyframe re-sink the freshly respotted ball, (b) killed the
            // SyncWithNetwork in-hand follow branch, and (c) made DeselectBall snap the ball below
            // the table on a no-drag release.
            currentBallPosition = mainCueBall.position;
            lerpedBallPosition = currentBallPosition;

            cuePivotTransform.position = mainCueBall.position;
            cueDisplacementTransform.localPosition = Vector3.zero;
            targetingManager2D.Resset();
            cueSliderTransform.localPosition = Vector3.zero;
            cueSliderZDisplacement = 0.0f;
            cueForceSlider.value = 0.0f;

            cueVerticalAlignment.parent = cuePivotTransform;
            cueVerticalAlignment.localPosition = Vector3.zero;
            cueVerticalAlignment.localRotation = cueVerticalInitialRotation;

            if (cueBallRespotted && AightBallPoolGameLogic.gameState != null)
            {
                // The cue ball is physically back on the table now. Keep ball-in-hand true
                // for the incoming player, but do not leave the "still pocketed" flag set;
                // that mixed state shows a wrong cue/stick for ~1s after a scratch.
                AightBallPoolGameLogic.gameState.cueBallInPocket = false;
            }

            bool cueBallNeedsHandPlacement = AightBallPoolGameLogic.gameState != null
                                             && AightBallPoolGameLogic.gameState.cueBallInHand;
            if (enabled && !cueBallNeedsHandPlacement)
                SetCueRigVisible(true);

            EstimateShot(true);
            // POST-SHOT cue-sync cache refresh — fixes the ~1s "weird/unstable cue" right after a SCRATCH
            // in MULTIPLAYER. The block above neutralizes the LIVE cue rig (slider/displacement/vertical),
            // but the four cached SYNC TARGETS (cuePivotYRotation / localCueVerticalRotationX /
            // cueLocalDisplacementXY / cueSliderLocalZPosition) still hold the PREVIOUS shot's loaded pose.
            // On a watching client SyncWithNetwork's unconditional 5*dt lerp — whose >10° SNAP guard keys
            // only on pivot-Y — would then drag the just-neutralized stick BACK toward that stale tilt/spin/
            // pullback for ~1s, until the next shooter's keyframe overwrites the cache. Snapshot the neutral
            // pose into the cache so that lerp is a no-op until a real keyframe arrives.
            // GATE (deliberately narrow so nothing else is disturbed):
            //   • controlFromNetwork — the WATCHER, where the stale lerp actually runs.
            //   • cueBallInPocket    — the SCRATCHER on the scratch frame: its turn hasn't flipped to
            //                          watcher yet (myTurn still true), so controlFromNetwork is still false.
            // NOT on an active same-turn continuation (legal pot, turn kept): there ActivateControl does not
            // run, so the live IsCueModified -> OnSendCueControl stream must still publish this neutral reset
            // (it also keeps the server's remembered cue state fresh for reconnect); refreshing the cache
            // here would suppress that send. AI/HotSeat are excluded by isOnLine.
            if (BallPoolGameLogic.isOnLine && (BallPoolGameLogic.controlFromNetwork || scratchShot))
            {
                cuePivotYRotation = cuePivotTransform.localRotation.eulerAngles.y;
                localCueVerticalRotationX = cueVerticalAlignment.localRotation.eulerAngles.x;
                cueLocalDisplacementXY = new Vector2(cueDisplacementTransform.localPosition.x, cueDisplacementTransform.localPosition.y);
                cueSliderLocalZPosition = cueSliderTransform.localPosition.z;
            }
        }

        // --- Reconnect recovery: server-authoritative cue-ball respot -------------------------------
        // Scratch + INSTANT disconnect: the shooter pockets the cue ball and drops the connection the
        // same instant, so the server's OnShotEnded respot may not have run yet and its cue ball is
        // still below the table (y < -0.1). The reconnect path (MyEightBallNetwork.GetBallsmechanicalStateData)
        // calls this ON THE SERVER to compute ONE valid ball-in-hand position and lift the cue ball
        // onto the table, then broadcasts that single position to BOTH clients (RpcRespotCueBall ->
        // ApplyCueBallRespot). Never let each machine RepositionBallInCube independently — that produced
        // a different cue spot per screen (see project_mirror_pitfalls). Returns the chosen position.
        public Vector3 RespotCueBallInHand()
        {
            Vector3 ballNewPosition = FindCueBallRespotPosition();
            ApplyCueBallRespot(ballNewPosition);
            return ballNewPosition;
        }

        public Vector3 FindCueBallRespotPosition()
        {
            bool canReactivate = false;
            Vector3 ballNewPosition = Vector3.zero;
            cuePhysicsManager.RepositionBallInCube(mainCueBall.radius, clothPosition, clothLayerIndex, ballLayerIndex | layerForCueBall, ref canReactivate, ref ballNewPosition);
            if (!canReactivate)
            {
                ballNewPosition = GetFallbackCueBallRespotPosition();
            }
            return ballNewPosition;
        }

        private Vector3 GetFallbackCueBallRespotPosition()
        {
            Transform cube = clothPosition != null ? clothPosition : initialMoveSpace;
            Vector3 fallback = mainCueBall != null ? mainCueBall.position : Vector3.zero;
            if (cube == null || mainCueBall == null)
            {
                fallback.y = 0.04f;
                return fallback;
            }

            fallback = cube.position;
            RaycastHit clothHit;
            Vector3 origin = cube.position + 0.5f * cube.lossyScale.y * cube.up;
            if (Physics.Raycast(origin, -cube.up, out clothHit, cube.lossyScale.y, clothLayerIndex))
            {
                fallback = clothHit.point + mainCueBall.radius * clothHit.normal;
            }
            else
            {
                fallback.y = 0.04f;
            }

            return BallGeometry.ClampPositionInCube(fallback, mainCueBall.radius, cube);
        }

        // Place the cue ball at an AUTHORITATIVE ball-in-hand position and lift it out of any pocket.
        // Runs on EVERY client (via RpcRespotCueBall) and on the server (via RespotCueBallInHand) so all
        // machines converge to the SAME server-computed spot. The position is given, never recomputed
        // locally. Mirrors the cue-ball un-pocket sequence in OnShotEnded.
        public void ApplyCueBallRespot(Vector3 position)
        {
            if (mainCueBall == null) return;
            if (mainCueBall.listener != null && mainCueBall.listener.pocket != null)
            {
                cuePhysicsManager.TriggerBallEjectionFromPocket(mainCueBall.listener, mainCueBall.listener.pocket, true);
                mainCueBall.listener.pocket = null;
            }
            mainCueBall.position = position;
            mainCueBall.isActive = false;
            mainCueBall.inPocket = false;
            mainCueBall.pocketId = -1;
            mainCueBall.hitShapeId = -2;
            AllowBallUpdateFromNetwork = false;
            mainCueBall.SuppressPlaybackForAuthoritativeCueRespot(position);
            if (AightBallPoolGameLogic.gameState != null && position.y > -0.1f)
            {
                AightBallPoolGameLogic.gameState.cueBallInHand = true;
                AightBallPoolGameLogic.gameState.cueBallInPocket = false;
            }
            if (mainCueBall.listener != null && mainCueBall.listener.body != null)
            {
                mainCueBall.listener.body.linearVelocity = Vector3.zero;
                mainCueBall.listener.body.angularVelocity = Vector3.zero;
            }
            mainCueBall.OnState(BallState.ExitFromPocket);
            if (position.y > -0.1f)
                cuePivotTransform.position = position;
            currentBallPosition = position;
            lerpedBallPosition = position;
            if (position.y > -0.1f)
            {
                ResetCueToHorizontalRespottedPose();
                cueBallRespottedAimPending = true;
            }
            if (enabled && !BallPoolGameLogic.controlFromNetwork && position.y > -0.1f)
            {
                RestoreCueRigForControlStart(false);
                AlignCueForBreakAndAim(true);
            }
        }

        private void CaptureShotStartSnapshot()
        {
            shotStartCueBallPosition = mainCueBall != null ? mainCueBall.position : Vector3.zero;
            shotStartCuePivotPosition = cuePivotTransform != null ? cuePivotTransform.position : shotStartCueBallPosition;
            shotStartCuePivotLocalRotationY = cuePivotTransform != null ? cuePivotTransform.localRotation.eulerAngles.y : 0.0f;
            shotStartCueVerticalLocalRotationX = cueVerticalAlignment != null ? cueVerticalAlignment.localRotation.eulerAngles.x : 0.0f;
            shotStartCueDisplacementLocalPositionXY = cueDisplacementTransform != null
                ? new Vector2(cueDisplacementTransform.localPosition.x, cueDisplacementTransform.localPosition.y)
                : Vector2.zero;
            shotStartCueSliderLocalPositionZ = cueSliderTransform != null ? cueSliderTransform.localPosition.z : 0.0f;
            shotStartCueForce = cueForce;
            shotStartBallPositions = CaptureCurrentBallPositions();
        }

        public void GetLastShotStartSnapshot(out Vector3 cueBallPosition, out Vector3 cuePivotPosition,
            out float cuePivotLocalRotationY, out float cueVerticalLocalRotationX,
            out Vector2 cueDisplacementLocalPositionXY, out float cueSliderLocalPositionZ,
            out float force, out Vector3[] ballPositions)
        {
            if (shotStartBallPositions == null || shotStartBallPositions.Length == 0)
                CaptureShotStartSnapshot();

            cueBallPosition = shotStartCueBallPosition;
            cuePivotPosition = shotStartCuePivotPosition;
            cuePivotLocalRotationY = shotStartCuePivotLocalRotationY;
            cueVerticalLocalRotationX = shotStartCueVerticalLocalRotationX;
            cueDisplacementLocalPositionXY = shotStartCueDisplacementLocalPositionXY;
            cueSliderLocalPositionZ = shotStartCueSliderLocalPositionZ;
            force = shotStartCueForce;
            ballPositions = shotStartBallPositions != null ? (Vector3[])shotStartBallPositions.Clone() : new Vector3[0];
        }

        private Vector3[] CaptureCurrentBallPositions()
        {
            var balls = poolGameManager != null ? poolGameManager.allBalls : null;
            if (balls == null)
                return new Vector3[0];

            Vector3[] positions = new Vector3[balls.Length];
            for (int i = 0; i < balls.Length; i++)
                positions[i] = balls[i] != null ? balls[i].position : Vector3.zero;
            return positions;
        }

        public void ApplyShotStartSnapshot(Vector3 cueBallPosition, Vector3 cuePivotPosition,
            float cuePivotLocalRotationY, float cueVerticalLocalRotationX,
            Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ,
            float force, Vector3[] ballPositions)
        {
            ApplyShotStartBallPositions(ballPositions);

            if (mainCueBall != null && cueBallPosition.y > -0.1f)
            {
                mainCueBall.position = cueBallPosition;
                currentBallPosition = cueBallPosition;
                lerpedBallPosition = cueBallPosition;
            }

            if (cuePivotTransform != null)
            {
                cuePivotTransform.position = cuePivotPosition;
                cuePivotTransform.localRotation = Quaternion.Euler(0.0f, cuePivotLocalRotationY, 0.0f);
            }
            if (cueVerticalAlignment != null)
                cueVerticalAlignment.localRotation = Quaternion.Euler(cueVerticalLocalRotationX, 0.0f, 0.0f);
            if (cueDisplacementTransform != null)
                cueDisplacementTransform.localPosition = new Vector3(cueDisplacementLocalPositionXY.x, cueDisplacementLocalPositionXY.y, 0.0f);
            if (cueSliderTransform != null)
                cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderLocalPositionZ);

            cuePivotYRotation = cuePivotLocalRotationY;
            localCueVerticalRotationX = cueVerticalLocalRotationX;
            cueLocalDisplacementXY = cueDisplacementLocalPositionXY;
            cueSliderLocalZPosition = cueSliderLocalPositionZ;
            cueForce = force;
            cueSliderZDisplacement = Mathf.Lerp(0, -maxCueSlidingDistance, force);

            if (targetingManager2D != null && cueDisplacementTransform != null && cueBallSize > 0.0f)
                targetingManager2D.SetPointTargetingPosition(-cueDisplacementTransform.localPosition / cueBallSize);
            if (!isMoving && !isInShot)
                EstimateShot(true);
        }

        private void ApplyShotStartBallPositions(Vector3[] ballPositions)
        {
            var balls = poolGameManager != null ? poolGameManager.allBalls : null;
            if (balls == null || ballPositions == null)
                return;

            int count = Mathf.Min(balls.Length, ballPositions.Length);
            for (int i = 0; i < count; i++)
            {
                var ball = balls[i];
                if (ball == null)
                    continue;

                Vector3 position = ballPositions[i];
                if (position.y <= -0.1f)
                {
                    ball.position = position;
                    if (!ball.inPocket)
                    {
                        ball.inPocket = true;
                        ball.OnState(BallState.EnterInPocket);
                    }
                }
                else
                {
                    if (ball.inPocket)
                    {
                        ball.isActive = false;
                        ball.inPocket = false;
                        ball.pocketId = -1;
                        ball.hitShapeId = -2;
                        ball.OnState(BallState.ExitFromPocket);
                    }
                    ball.position = position;
                }

                if (ball.listener != null && ball.listener.body != null)
                {
                    ball.listener.body.linearVelocity = Vector3.zero;
                    ball.listener.body.angularVelocity = Vector3.zero;
                }
            }
        }

        void ClearAllLines()
        {
            cueBallLineRenderer.positionCount = 0;
            targetBallLineRenderer.positionCount = 0;
            cuePhysicsManager.HideBallTrajectory();
            InvalidateCueLineRenderOrigin();
        }

        // Set whenever the aim visuals are suppressed while physics settles. EstimateShot's re-enable block
        // only runs on forceCalculate || StateChanged, and the rotation dirty-check can read back unchanged —
        // so once suppressed, nothing guaranteed the guideline ever came back when the player gained control
        // with the board already idle (tester report 74341f8c "indicator ?": canControl=True but no aim line).
        // Update() (which only runs while this client HAS control — ActivateControl drives `enabled`) re-runs
        // EstimateShot(true) once, the first idle frame after a suppression.
        private bool aimVisualsSuppressedPendingReestimate;

        public void SuppressCueVisualsWhileShotSettling()
        {
            aimVisualsSuppressedPendingReestimate = true;
            ClearAllLines();
            if (ballCheckerTransform != null)
                ballCheckerTransform.gameObject.SetActive(false);
            if (cueLineCircleWhite != null)
                cueLineCircleWhite.SetActive(false);
            if (cueLineCircleRed != null)
                cueLineCircleRed.SetActive(false);
        }

        private void SetCueRigVisible(bool active)
        {
            if (GameUIController.instance != null && GameUIController.instance.CueRenderer2D != null)
                GameUIController.instance.CueRenderer2D.gameObject.SetActive(active);
            if (aimLine != null)
                aimLine.SetActive(active);
            if (targetBallLineRenderer != null)
                targetBallLineRenderer.gameObject.SetActive(active);
            if (ballValidator != null)
                ballValidator.SetActive(active);
            if (CuePivotStick != null)
                CuePivotStick.SetActive(active);
            if (ballCheckerBoth != null)
                ballCheckerBoth.SetActive(active);
            if (cueline != null)
                cueline.SetActive(active);
            if (targetline != null)
                targetline.SetActive(active);

            // STITCH — MULTIPLAYER ONLY. Offline/AI keeps the original behaviour, where this method
            // never touched cueBallLineRenderer at all.
            // The aim INDICATOR LINE is part of the rig. cueBallLineRenderer was the one rig object
            // missing from the list above, which is how every "stick is up but there is no line"
            // report happened (reconnect, turn start, post-shot re-show). Tie it to the same switch
            // so the stick and the line can never disagree, and on SHOW guarantee the line is
            // actually DRAWN — not merely an active GameObject holding zero points.
            if (!BallPoolGameLogic.isOnLine)
                return;

            if (active)
            {
                if (cueBallLineRenderer != null)
                    cueBallLineRenderer.gameObject.SetActive(true);
                EnsureAimLineRendered();
            }
            else
            {
                // Drop the geometry as well, so the next show is FORCED to recompute a fresh line
                // instead of flashing the pose the board had before it was hidden.
                if (cueBallLineRenderer != null)
                {
                    cueBallLineRenderer.positionCount = 0;
                    cueBallLineRenderer.gameObject.SetActive(false);
                }
                if (targetBallLineRenderer != null)
                    targetBallLineRenderer.positionCount = 0;
                InvalidateCueLineRenderOrigin();
            }
        }

        // Guarantees the indicator line is DRAWN whenever the cue rig is shown. Safe to call every
        // frame — the watcher's per-frame force-show does — because it only recomputes when the line
        // is actually empty. If that recompute lands while the board is still settling, EstimateShot
        // early-returns without drawing, so arm the pending-reestimate retry and Update picks it up
        // on the first idle frame instead of leaving an empty line until the player re-aims.
        private void EnsureAimLineRendered()
        {
            if (cueBallLineRenderer == null || cueBallLineRenderer.positionCount > 0)
                return;
            EstimateShot(true);
            if (cueBallLineRenderer.positionCount == 0)
                aimVisualsSuppressedPendingReestimate = true;
        }

        // Full inverse of the watcher "show" (SetCueRigVisible(true) + EstimateShot): hide the stick
        // AND every aim visual EstimateShot can light, so the stick and the WHOLE aim-visual set are
        // always hidden together. SetCueRigVisible(false) covers the stick + cueline/targetline/
        // ballCheckerBoth/aimLine/ballValidator/CueRenderer2D and (via the multiplayer stitch) both
        // guide-line renderers. The three below — the ghost-ball checker and the two aim reticle
        // circles — are NOT in SetCueRigVisible, yet EstimateShot turns them on; clearing only the two
        // line renderers (the old HideAimLinesForBallInHandDrag) left these floating on the table with
        // no stick during reconnect suppression / post-scratch respot (adversarially verified). Also
        // zero the two line renderers explicitly + invalidate the origin as belt-and-suspenders.
        private void HideWatcherCueRigAndAimVisuals()
        {
            SetCueRigVisible(false);
            if (ballCheckerTransform != null)
                ballCheckerTransform.gameObject.SetActive(false);
            if (cueLineCircleWhite != null)
                cueLineCircleWhite.SetActive(false);
            if (cueLineCircleRed != null)
                cueLineCircleRed.SetActive(false);
            if (cueBallLineRenderer != null)
            {
                cueBallLineRenderer.positionCount = 0;
                cueBallLineRenderer.gameObject.SetActive(false);
            }
            if (targetBallLineRenderer != null)
            {
                targetBallLineRenderer.positionCount = 0;
                targetBallLineRenderer.gameObject.SetActive(false);
            }
            InvalidateCueLineRenderOrigin();
        }

        // ---- Authoritative ball-in-hand visibility (multiplayer, WATCHER only) --------------------
        // Driven by MyEightBallNetwork's `ballIsDragging` SyncVar hook — a RELIABLE, reconnect-safe
        // signal, unlike the old one-shot SwitchCue(false) relay that could be missed and leave the
        // watcher with the stick standing while the aim line was gone. While the opponent drags the
        // cue ball in ball-in-hand, hide the whole cue rig (stick + line) TOGETHER; when the drag ends,
        // show them TOGETHER and rebuild the aim line. AllowBallUpdateFromNetwork is set from the same
        // authoritative signal so the per-frame SyncWithNetwork follow/hide logic stays in step.
        public void ApplyNetworkBallDragging(bool dragging)
        {
            if (!BallPoolGameLogic.isOnLine || !BallPoolGameLogic.controlFromNetwork)
                return;

            AllowBallUpdateFromNetwork = dragging;
            if (dragging)
            {
                HideWatcherCueRigAndAimVisuals();
            }
            else
            {
                AttachCueRigToCuePivot();
                SetCueRigVisible(true);
                EstimateShot(true);
            }
        }

        // ---- TURN CHANGE — force the cue rig back up (multiplayer, BOTH sides) ---------------------
        // Driven by the server: MyEightBallNetwork.TryChangeTurnOnServer -> RpcResetCueRigOnTurnChange.
        //
        // WHY AN RPC AND NOT THE ballIsDragging SyncVar: the watcher's rig is normally restored by
        // ApplyNetworkBallDragging(false), which runs off that SyncVar's hook. Mirror only invokes a
        // hook when the value CHANGES. On a turn TIMEOUT nobody ever dragged, so the server's
        // `ballIsDragging = false` on turn change writes false OVER false — silent, no hook, and the
        // watcher is left with NO stick and NO line. It only healed once the next player happened to
        // pick the cue ball up and drop it (false->true->false finally changed the value). Tester:
        // "timer khatam hua to turn shift hui, watcher side ki stick aur line gayab ho gayi... drag
        // and drop karne pe sahi ho gaya." A ClientRpc always arrives, so the turn change states it.
        //
        // The server only says "a new turn started, no drag is in progress" — each client still
        // decides for itself whether it may actually show, via the guards below.
        public void ResetCueRigOnTurnChange()
        {
            if (!BallPoolGameLogic.isOnLine)
                return;   // offline/AI is untouched
            if (isMoving || isInShot || (cuePhysicsManager != null && cuePhysicsManager.IsInMove))
                return;   // a shot is still resolving — the settle path owns the visuals right now

            // A turn change means no ball-in-hand drag can be in progress on EITHER side.
            AllowBallUpdateFromNetwork = false;
            networkCueHiddenForBallInHandDrag = false;

            if (isBallInHand)
                return;   // this client is actively placing the cue ball — its own input owns the rig
            if (mainCueBall == null || mainCueBall.position.y <= -0.1f)
                return;   // cue ball not on the table (pocketed / awaiting the server respot); showing
                          // here would park the stick below the table, out of view

            AttachCueRigToCuePivot();
            SetCueRigVisible(true);
            EstimateShot(true);
        }

        private void RestoreCueRigForControlStart(bool estimateShot)
        {
            AttachCueRigToCuePivot();
            if (mainCueBall != null && cuePivotTransform != null && mainCueBall.position.y > -0.1f)
            {
                cuePivotTransform.position = mainCueBall.position;
                SetCueRigVisible(true);
            }
            if (cueVerticalAlignment != null)
                cueVerticalAlignment.localRotation = cueVerticalInitialRotation;
            if (cueDisplacementTransform != null)
                cueDisplacementTransform.localPosition = Vector3.zero;
            if (targetingManager2D != null)
                targetingManager2D.Resset();
            if (estimateShot)
                EstimateShot(true);
        }

        private void ResetCueToHorizontalRespottedPose()
        {
            isCueStretched = false;
            cueMode = CueMode.Non;
            currentMouseState = MouseState.Up;
            cueSliderZDisplacement = 0.0f;
            cueForce = 0.0f;
            if (cueForceSlider != null)
                cueForceSlider.value = 0.0f;
            if (cueSliderTransform != null)
            {
                cueSliderTransform.localPosition = Vector3.zero;
                savedCueSliderPos = cueSliderTransform.localPosition;
            }
            if (cueVerticalAlignment != null)
            {
                if (cuePivotTransform != null)
                    cueVerticalAlignment.parent = cuePivotTransform;
                cueVerticalAlignment.localPosition = Vector3.zero;
                cueVerticalAlignment.localRotation = cueVerticalInitialRotation;
            }
            if (cueDisplacementTransform != null)
                cueDisplacementTransform.localPosition = Vector3.zero;
            if (targetingManager2D != null)
                targetingManager2D.Resset();
        }

        private bool cueBallRespottedAimPending;
        private int localCueAimRebuildSeq;

        private bool ConsumeRespottedCueAimPending()
        {
            bool cueBallInHandResetOnTable = IsCueBallInHandResetOnTable();
            bool useRespottedAim = (cueBallRespottedAimPending || cueBallInHandResetOnTable)
                && mainCueBall != null
                && mainCueBall.position.y > -0.1f;
            if (useRespottedAim)
                cueBallRespottedAimPending = false;
            return useRespottedAim;
        }

        private IEnumerator RebuildLocalCueAimWhenReady(int seq)
        {
            yield return null;

            float deadline = Time.realtimeSinceStartup + 1.25f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (seq != localCueAimRebuildSeq || !enabled || BallPoolGameLogic.controlFromNetwork)
                    yield break;
                if (!isMoving && !isInShot && (cuePhysicsManager == null || !cuePhysicsManager.IsInMove))
                    break;
                yield return null;
            }

            if (seq != localCueAimRebuildSeq || !enabled || BallPoolGameLogic.controlFromNetwork)
                yield break;
            if (isMoving || isInShot || (cuePhysicsManager != null && cuePhysicsManager.IsInMove))
                yield break;
            if (currentMouseState != MouseState.Up || isCueStretched)
                yield break;
            if (isBallInHand || mainCueBall == null || mainCueBall.position.y <= -0.1f)
                yield break;

            bool cueLineMissing = cueBallLineRenderer != null
                && (!cueBallLineRenderer.gameObject.activeInHierarchy || cueBallLineRenderer.positionCount == 0);
            bool checkerHidden = ballCheckerTransform != null && !ballCheckerTransform.gameObject.activeInHierarchy;
            if (!cueLineMissing && !checkerHidden)
                yield break;

            isCueStretched = false;
            cueMode = CueMode.Non;
            currentMouseState = MouseState.Up;
            cueSliderZDisplacement = 0.0f;
            cueForce = 0.0f;
            if (cueForceSlider != null)
                cueForceSlider.value = 0.0f;
            if (cueSliderTransform != null)
            {
                cueSliderTransform.localPosition = Vector3.zero;
                savedCueSliderPos = cueSliderTransform.localPosition;
            }

            RestoreCueRigForControlStart(false);
            AlignCueForBreakAndAim(ConsumeRespottedCueAimPending());
            ForceSendCueState();
        }

        private bool networkCueHiddenForBallInHandDrag;

        // Reconnect restore: the server replays its cached cue-rig visibility (TargetRpcRestoreCueVisibility)
        // because the live show/hide relays are one-shots the rejoining client missed during the scene
        // reload. A plain SetCueRigVisible(false) is not enough — SyncWithNetwork force-shows the rig on
        // the next frame — so arm the sticky flag CanShowCueRigFromNetworkCueControl already checks.
        // Cleared by any live cue-control packet (the opponent is really aiming again), a show relay
        // (SwitchCue(true)), or this client gaining control (ActivateControl(true)).
        public void SuppressNetworkCueRigForReconnect()
        {
            networkCueHiddenForBallInHandDrag = true;
            SetCueRigVisible(false);
        }

        // Online watcher: may the opponent's cue RIG (stick + line) be shown right now from the live
        // cue-control stream? Not while moving / in a shot / ball-update-from-network / ball-in-hand
        // drag, and only when the cue ball is on the table. This is what re-shows the stick after a
        // reconnect — SyncWithNetwork runs every frame while watching and calls it.
        private bool CanShowCueRigFromNetworkCueControl()
        {
            if (!BallPoolGameLogic.isOnLine || !BallPoolGameLogic.controlFromNetwork)
                return false;
            if (isMoving || isInShot || (cuePhysicsManager != null && cuePhysicsManager.IsInMove))
                return false;
            if (AllowBallUpdateFromNetwork || networkCueHiddenForBallInHandDrag)
                return false;
            if (mainCueBall == null || mainCueBall.position.y <= -0.1f)
                return false;
            if (AightBallPoolGameLogic.gameState != null && AightBallPoolGameLogic.gameState.cueBallInPocket)
                return false;

            return true;
        }

        private void ShowCueRigFromNetworkCueControlIfAllowed()
        {
            // The watcher's "stick on, line missing" case (reconnect mid-turn; live aiming only
            // lerps, EstimateShot fires on >10-degree snaps only) is handled INSIDE SetCueRigVisible
            // now — it brings the indicator line up with the stick and rebuilds it whenever it is
            // empty, so this per-frame force-show keeps the two in step by itself.
            if (CanShowCueRigFromNetworkCueControl())
                SetCueRigVisible(true);
        }

        // Keep the visible cue rig parented under the LIVE aim pivot (cuePivotTransform). After a
        // reconnect / post-shot the rig can be left parked under a stale pivot, so the stick draws at
        // the wrong place while the aim line (driven by cuePivotTransform) is correct.
        private void AttachCueRigToCuePivot()
        {
            if (cueVerticalAlignment == null || cuePivotTransform == null)
                return;

            if (cueVerticalAlignment.parent != cuePivotTransform)
                cueVerticalAlignment.parent = cuePivotTransform;
            cueVerticalAlignment.localPosition = Vector3.zero;
        }



        void Update()
        {
            if (InputOutput.isMobilePlatform)
            {
                CheckHandStatus();
            }

            // Online-only guideline upkeep; offline never suppresses the aim visuals, so the
            // original (merge-base) Update body is preserved for AI/offline games.
            if (BallPoolGameLogic.isOnLine)
            {
                UpdateCueLineIfRenderOriginMoved();
                ReestimateAimAfterSuppressionIfIdle();
            }
        }

        // First idle frame after a suppression, while this client holds control (Update only runs then) and is
        // not mid ball-in-hand drag: force one EstimateShot(true) so the guideline/checker/circles return
        // without the player having to rotate the aim first. EstimateShot(true) is idempotent here — if it is
        // still not idle it just re-suppresses and this retries next frame.
        private void ReestimateAimAfterSuppressionIfIdle()
        {
            if (!aimVisualsSuppressedPendingReestimate || isBallInHand || isInShot || isMoving
                || (cuePhysicsManager != null && cuePhysicsManager.IsInMove))
                return;
            aimVisualsSuppressedPendingReestimate = false;
            EstimateShot(true);
        }

        private void UpdateCueLineIfRenderOriginMoved()
        {
            if (isMoving || isInShot || (cuePhysicsManager != null && cuePhysicsManager.IsInMove))
                return;

            if (CueLineRenderOriginChanged())
                EstimateShot(true);
        }


        void OnMouseState(MouseState mouseState)
        {
            if (!InputOutput.inUsedCameraScreen && mouseState != MouseState.Up)
            {
                return;
            }
            if (!isControlAvailable)
            {
                return;
            }
            if (shotBackImage.enabled)
            {
                return;
            }
            this.currentMouseState = mouseState;

            if (!InputOutput.isMobilePlatform)
            {
                CheckHandStatus();
            }
            if (!(mouseState == MouseState.Down || mouseState == MouseState.Up || mouseState == MouseState.Press || mouseState == MouseState.Move))
            {
                return;
            }

            //(mouseState == MouseState.Up).Show("mouseState");
            if (isCueStretched && false && !isInShot && cueForce > 0.03f && gameUIManager.isShotOnRelease && mouseState == MouseState.Up)
            {
                isInShot = true;
                isMoving = true;
                cuePhysicsManager.ApplyImpulse(new Impulse(strikePoint, cueForce * (maxSpeed * cuePhysicsManager.BallMass) * cueSliderTransform.forward));
                StartCoroutine("DelayAndStartShot");
            }
            if (!enabled || (!InputOutput.inUsedCameraScreen && mouseState != MouseState.Up))
            {
                return;
            }
            if (mouseState == MouseState.Up || mouseState == MouseState.Down)
            {
                isCueStretched = false;
            }
            if (isCueStretched)
            {
                return;
            }
            if (AightBallPoolGameLogic.gameState.cueBallInHand)
            {
                if (!isBallInHand && mouseState == MouseState.Down)
                {
                    TryingToPickBall();
                }
                if (isBallInHand)
                {

                    if (mouseState == MouseState.Press) //RAR
                    {
                        TryMoveBall();
                        TrySendCueStateToOpponent(false);



                        CuePivotStick.SetActive(false);
                        ballCheckerBoth.SetActive(false);
                        cueline.SetActive(false);
                        targetline.SetActive(false);



                    }
                    else if (mouseState == MouseState.Up) //RAR
                    {
                        DeselectBall();

                        TrySendCueStateToOpponent(true);


                        CuePivotStick.SetActive(true);
                        ballCheckerBoth.SetActive(true);
                        cueline.SetActive(true);
                        targetline.SetActive(true);
                        // MULTIPLAYER: route through the choke point as well so the stick comes back
                        // TOGETHER with a freshly rebuilt indicator line for the cue ball's new spot.
                        // Offline/AI keeps the original four-object re-show above, untouched.
                        if (BallPoolGameLogic.isOnLine)
                            SetCueRigVisible(true);


                    }
                    return;
                }
            }
            if (!isInShot)
            {
                if (gameUIManager.is3DMode)
                {
                    ControlIn3DMode(mouseState);
                }
                else
                {
                    controlModeIs2D(mouseState);
                }
                cueForce = Mathf.Clamp01(-cueSliderZDisplacement / maxCueSlidingDistance);
                EstimateShot(false);

            }
            if (true && !isInShot && cueForce > 0.03f && gameUIManager.isShotOnRelease && mouseState == MouseState.Up)
            {
                // Sync blur still up => the board has not been synced yet => swallow the shot.
                // blur2D is NULL wherever the scene has no Blur2D component (AightBallPool34, and the
                // headless server scene), so a null reference must read as "no blur" — not as a crash,
                // and not as a permanent shot block. Offline/AI is already covered: GameUIController.Start
                // disables the Blur2D component outright there, so this branch never blocks it.
                if (blur2D == null || !blur2D.enabled)
                {
                    if (cueMode == CueMode.Non || cueMode == CueMode.TargetingAtTargetBall || cueMode == CueMode.StretchCue)
                    {
                        isInShot = true;
                        isMoving = true;
                        cuePhysicsManager.ApplyImpulse(new Impulse(strikePoint, cueForce * (maxSpeed * cuePhysicsManager.BallMass) * cueSliderTransform.forward));
                        StartCoroutine("DelayAndStartShot");
                    }
                }
                else
                {
                    // Slider value FIRST: it fires OnValueChanged -> IsCueExtended(), which sets
                    // isCueStretched/cueMode again. Clearing the flags afterwards keeps them cleared.
                    if (cueForceSlider != null)
                        cueForceSlider.value = 0.0f;

                    isCueStretched = false;
                    cueMode = CueMode.Non;
                    cueSliderTransform.localPosition = Vector3.zero;
                    savedCueSliderPos = cueSliderTransform.localPosition;
                    cueSliderZDisplacement = 0.0f;
                    cueForce = 0.0f;
                }
            }
        }

        // Camera2D's sync blur. Assigned in AightBallPool.unity only — left NULL in scenes that have
        // no Blur2D (AightBallPool34, headless server scene); see the null-safe read in OnMouseState.
        public Blur2D blur2D;

        public GameObject CuePivotStick;
        public GameObject ballCheckerBoth;
        public GameObject cueline;
        public GameObject targetline;




        public void SyncWithNetwork()
        {
            if (CanShowCueRigFromNetworkCueControl())
                AttachCueRigToCuePivot();
            ShowCueRigFromNetworkCueControlIfAllowed();

            // SNAP instead of lerping when the watcher's stick is FAR from the authoritative aim.
            // SyncWithNetwork only runs once this client's sim has settled (it is gated on
            // !IsInMove), so after a scratch the scratcher's stick sat at the stale shot aim for
            // the whole settle window and then visibly swept to the new aim over another second —
            // the "stick abnormal / different on each screen after a scratch" report. A large gap
            // means we are catching up on missed state, not following live aiming: snap once.
            if (Mathf.Abs(Mathf.DeltaAngle(cuePivotTransform.localRotation.eulerAngles.y, cuePivotYRotation)) > 10f)
            {
                cuePivotTransform.localRotation = Quaternion.Euler(0.0f, cuePivotYRotation, 0.0f);
                cueVerticalAlignment.localRotation = Quaternion.Euler(localCueVerticalRotationX, 0.0f, 0.0f);
                cueDisplacementTransform.localPosition = new Vector3(cueLocalDisplacementXY.x, cueLocalDisplacementXY.y, 0.0f);
                cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderLocalZPosition);
                EstimateShot(true);
            }
            cuePivotTransform.localRotation = Quaternion.Lerp(cuePivotTransform.localRotation, Quaternion.Euler(0.0f, cuePivotYRotation, 0.0f), 5.0f * Time.deltaTime);
            cueVerticalAlignment.localRotation = Quaternion.Lerp(cueVerticalAlignment.localRotation, Quaternion.Euler(localCueVerticalRotationX, 0.0f, 0.0f), 5.0f * Time.deltaTime);
            cueDisplacementTransform.localPosition = Vector3.Lerp(cueDisplacementTransform.localPosition, new Vector3(cueLocalDisplacementXY.x, cueLocalDisplacementXY.y, 0.0f), 5.0f * Time.deltaTime);
            targetingManager2D.SetPointTargetingPosition(-cueDisplacementTransform.localPosition / cueBallSize);
            cueSliderTransform.localPosition = Vector3.Lerp(cueSliderTransform.localPosition, new Vector3(0.0f, 0.0f, cueSliderLocalZPosition), 5.0f * Time.deltaTime);
            // Watcher follow during BALL-IN-HAND has two paths:
            // - explicit opponent drag (AllowBallUpdateFromNetwork): smooth toward the latest packet below.
            // - non-drag recovery/respot: snap once so a missed pocket/reconnect state cannot leave the
            //   cue ball hidden below the table while the stick aims from the wrong origin.
            if (!AllowBallUpdateFromNetwork
                && AightBallPoolGameLogic.gameState != null && AightBallPoolGameLogic.gameState.cueBallInHand
                && currentBallPosition.y > -0.1f)
            {
                if (mainCueBall.inPocket)
                {
                    mainCueBall.inPocket = false;
                    mainCueBall.listener.body.linearVelocity = Vector3.zero;
                    mainCueBall.listener.body.angularVelocity = Vector3.zero;
                }
                lerpedBallPosition = currentBallPosition;
                mainCueBall.position = currentBallPosition;
                mainCueBall.OnState(BallState.SetState);
            }
            else if (AightBallPoolGameLogic.gameState != null && AightBallPoolGameLogic.gameState.cueBallInHand
                     && mainCueBall.position.y < -0.1f)
            {
                // Ball-in-hand, but the cue ball is BELOW the table — it free-fell after a scratch and
                // the in-hand placement hasn't synced yet (the synced position is garbage too, e.g.
                // across a reconnect, or while the turn is briefly out of sync). Snap the cue ball back
                // ON the table at its current X/Z (table resting height) and un-pocket it, so the ball
                // and stick are at least VISIBLE/placeable instead of "no cue ball, no stick". The
                // in-hand player's real placement overrides this via the branch above once it arrives.
                Vector3 onTable = mainCueBall.position;
                onTable.y = 0.04f; // resting height of a ball on this table (matches the object balls)
                mainCueBall.inPocket = false;
                mainCueBall.listener.body.linearVelocity = Vector3.zero;
                mainCueBall.listener.body.angularVelocity = Vector3.zero;
                lerpedBallPosition = onTable;
                mainCueBall.position = onTable;
                mainCueBall.OnState(BallState.SetState);
            }
            if (AllowBallUpdateFromNetwork)
            {
                lerpedBallPosition = Vector3.Lerp(lerpedBallPosition, currentBallPosition, networkCueBallFollowSpeed * Time.deltaTime);
                // Only follow if it doesn't teleport the cue ball into a resting ball (see helper).
                TryApplyNetworkCueBallPosition(lerpedBallPosition);
            }
            // Keep the cue stick's origin on the cue ball on the watching (network-driven) client —
            // but ONLY while the cue ball is ON the table. If the cue ball is below the table
            // (pocketed / free-fallen after a scratch that hasn't been respotted yet) following it
            // would drag the whole stick far below the table out of view ("no stick after reconnect").
            // Mirrors the GameManager.Update anchor guard.
            bool cueLineOriginMoved = false;
            if (mainCueBall.position.y > -0.1f)
            {
                cuePivotTransform.position = mainCueBall.position;
                cueLineOriginMoved = CueLineRenderOriginChanged();
            }


            // STICK ↔ LINE COUPLING (watcher): the aim LINE may be drawn ONLY when the stick is allowed
            // to show. The stick's watcher show-path (ShowCueRigFromNetworkCueControlIfAllowed) is gated
            // by CanShowCueRigFromNetworkCueControl(); the line-drawing EstimateShot below was NOT, so
            // whenever CanShow was false for a NON-drag reason and the board was idle — a reconnect
            // suppression (networkCueHiddenForBallInHandDrag) or a post-scratch respot (cueBallInPocket /
            // cue ball below table) — the line kept drawing every frame while the stick stayed hidden
            // ("line on, stick missing"). Gating on !CanShow instead of just AllowBallUpdateFromNetwork
            // hides the line in EXACTLY the same states the stick is hidden, so they can never desync.
            // (AllowBallUpdateFromNetwork — the opponent's drag — is one of the cases CanShow already
            // returns false for, so the drag path is preserved.)
            if (!CanShowCueRigFromNetworkCueControl())
            {
                HideWatcherCueRigAndAimVisuals();
            }
            else
            {
                // Reaching here means CanShow is TRUE, so the stick is allowed up. Ensure it is shown
                // in the SAME frame the line is (re)drawn below — closes the 1-frame line-without-stick
                // flash when CanShow flips false->true mid-method (the cue-ball lift raises the ball
                // onto the table AFTER the top-of-method ShowCueRig already ran with it below-table).
                // Idempotent with that top show; via the stitch it also re-shows the line with the stick.
                SetCueRigVisible(true);
                if (Mathf.Abs(cuePivotTransform.localRotation.eulerAngles.y - cuePivotYRotation) > 0.1f)
                {
                    EstimateShot(true);
                }
                else if (Mathf.Abs(cuePivotTransform.localRotation.eulerAngles.y - cuePivotYRotation) < 0.1f && cuePivotTransform.localRotation.eulerAngles.y != cuePivotYRotation)
                {
                    cuePivotTransform.localRotation = Quaternion.Euler(0.0f, cuePivotYRotation, 0.0f);
                    EstimateShot(true);
                }
                else if (cueLineOriginMoved)
                {
                    EstimateShot(true);
                }
            }
        }

        // Watcher-safe cue-ball placement: applies the position ONLY if it would not overlap another
        // ball. Object balls are on `ballLayerIndex`; the cue ball has its own `layerForCueBall`, so
        // this CheckSphere never self-hits. Without this the watcher teleports the cue ball along the
        // network-driven ball-in-hand path straight through resting balls, and the idle Physics.Simulate
        // step (PhysicsHandeler.FixedUpdate else-branch) depenetrates the overlap and shoves that
        // resting ball out of place — the watcher-only "in-hand touch nudged a ball" bug.
        private bool TryApplyNetworkCueBallPosition(Vector3 pos)
        {
            if (Physics.CheckSphere(pos, cueBallSize, ballLayerIndex))
                return false;
            mainCueBall.position = pos;
            mainCueBall.OnState(BallState.SetState);
            return true;
        }
        public void GetBallPositionFromNetwork(Vector3 ballSelectPosition)
        {
            AllowBallUpdateFromNetwork = true;
            currentBallPosition = ballSelectPosition;
            lerpedBallPosition = ballSelectPosition;
            mainCueBall.position = ballSelectPosition;
            if (ballSelectPosition.y > -0.1f)
                cuePivotTransform.position = ballSelectPosition;
            mainCueBall.OnState(BallState.SetState);
        }
        public void SetBallPositionViaNetwork(Vector3 ballNewPosition)
        {
            //  Constants_M.LogInfo("SetBallPositionFromNetwork");
            AllowBallUpdateFromNetwork = false;
            lerpedBallPosition = ballNewPosition;
            currentBallPosition = ballNewPosition;
            cuePivotTransform.position = ballNewPosition;
            mainCueBall.position = ballNewPosition;
            mainCueBall.OnState(BallState.SetState);
            EstimateShot(true);
        }
        public void NetworkBallMovement(Vector3 ballPosition)
        {
            //  Constants_M.LogInfo("MoveBallFromNetwork " + DataManager.Vector3ToString(ballPosition));
            this.currentBallPosition = ballPosition;
        }
        public void SyncCueControlFromNetwork(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
        {
            // A live cue-control packet means the opponent is actually aiming again — lift the
            // reconnect suppression so the rig may show. (The reconnect restore sends its sticky
            // hide AFTER the forced pose, so this clear cannot race it — TargetRpcs are ordered.)
            networkCueHiddenForBallInHandDrag = false;
            this.cuePivotYRotation = cuePivotLocalRotationY;
            this.localCueVerticalRotationX = cueVerticalLocalRotationX;
            this.cueLocalDisplacementXY = cueDisplacementLocalPositionXY;
            this.cueSliderLocalZPosition = cueSliderLocalPositionZ;
            this.cueForce = force;
            this.cueSliderZDisplacement = Mathf.Lerp(0, -maxCueSlidingDistance, force);
        }
        /// <summary>
        /// Reconnect cue reset. Identical to ForceSyncCueControlFromNetwork(0,0,zero,0,0) except the vertical
        /// alignment returns to the AUTHORED rest pose instead of identity. Quaternion.Euler(0,0,0) is NOT the
        /// rest pose — CueVertical is authored at ~5 degrees on this table (AightBallPool.unity, Transform 500:
        /// m_LocalRotation x=0.0436, w=0.9990) — so forcing identity dropped the stick below its bar right after
        /// a reconnect (tester issue 11, "stick apni bar se down chli jati hai"). Every other reset path in this
        /// file already restores cueVerticalInitialRotation (see AlignCueForBreakAndAim and friends); the
        /// reconnect path was the only one that did not. The cached sync target is re-read from the transform to
        /// match, the same way lines ~258 / ~1057 do.
        /// </summary>
        public void ResetCueOnReconnect()
        {
            ForceSyncCueControlFromNetwork(0f, 0f, Vector2.zero, 0f, 0f);
            if (cueVerticalAlignment != null)
            {
                cueVerticalAlignment.localRotation = cueVerticalInitialRotation;
                localCueVerticalRotationX = cueVerticalAlignment.localRotation.eulerAngles.x;
            }
        }

        public void ForceSyncCueControlFromNetwork(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
        {
            SyncCueControlFromNetwork(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
            AnchorCuePivotToCueBall(false);
            AttachCueRigToCuePivot();
            cuePivotTransform.localRotation = Quaternion.Euler(0.0f, cuePivotLocalRotationY, 0.0f);
            cueVerticalAlignment.localRotation = Quaternion.Euler(cueVerticalLocalRotationX, 0.0f, 0.0f);
            cueDisplacementTransform.localPosition = new Vector3(cueDisplacementLocalPositionXY.x, cueDisplacementLocalPositionXY.y, 0.0f);
            targetingManager2D.SetPointTargetingPosition(-cueDisplacementTransform.localPosition / cueBallSize);
            cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderLocalPositionZ);

            // A cue KEYFRAME must not move the BALL. Ball position changes have their own
            // authoritative messages (SelectBallPosition / OnMoveBall / SetBallPosition); writing
            // the ball here from the (possibly stale, pocket-interior) cached position was the
            // direct mechanism that re-sank the scratcher's freshly respotted cue ball and nudged
            // it during reconnect restores. Only follow the ball during the opponent's explicit
            // pick-up drag (AllowBallUpdateFromNetwork).
            if (AllowBallUpdateFromNetwork)
            {
                lerpedBallPosition = Vector3.Lerp(lerpedBallPosition, currentBallPosition, networkCueBallFollowSpeed * Time.deltaTime);
                // Only follow if it doesn't teleport the cue ball into a resting ball (see helper).
                TryApplyNetworkCueBallPosition(lerpedBallPosition);
            }

            // ALWAYS recalculate the aim/target lines after a forced cue snap. The old code only
            // called EstimateShot when the euler readback differed from the target — but
            // Quaternion.Euler(0,y,0).eulerAngles.y usually reads back EXACTLY equal, so both
            // conditions were false and EstimateShot was skipped: the STICK snapped to the new
            // angle while the LINE RENDERER stayed frozen at the old one (the reconnect
            // "stick and aim line don't match" bug — live aiming was unaffected because the
            // per-frame lerp path recalculates continuously).
            cuePivotTransform.localRotation = Quaternion.Euler(0.0f, cuePivotLocalRotationY, 0.0f);
            ShowCueRigFromNetworkCueControlIfAllowed();
            EstimateShot(true);
        }

        public void AnchorCuePivotToCueBall(bool estimateShot = true)
        {
            if (mainCueBall == null || cuePivotTransform == null)
                return;
            if (mainCueBall.position.y < -0.1f)
                return;

            cuePivotTransform.position = mainCueBall.position;
            currentBallPosition = mainCueBall.position;
            lerpedBallPosition = mainCueBall.position;

            if (estimateShot)
                EstimateShot(true);
        }

        /// <summary>
        /// Re-applies the cue a few frames AFTER the reconnect restore, defeating the race where
        /// the one-shot cue snapshot (TargetRpcForceCueControl) lands before local (re)initialisation
        /// finishes and is then wiped — leaving the stick at a stale angle/origin that only a manual
        /// cue rotation cleared (testers: "stick wrong after reconnect until you rotate it, even
        /// across turn changes"). The authoritative aim is passed in (not read from the possibly-reset
        /// cached fields). pullback/force is sent as 0 to avoid a phantom auto-shot.
        /// </summary>
        public void RefreshCueAfterReconnect(float pivotY, float verticalX, Vector2 displacement, float sliderZ, float force)
        {
            StartCoroutine(RefreshCueAfterReconnectRoutine(pivotY, verticalX, displacement, sliderZ, force));
        }

        private System.Collections.IEnumerator RefreshCueAfterReconnectRoutine(float pivotY, float verticalX, Vector2 displacement, float sliderZ, float force)
        {
            // Let the ball-position restore + cue-ball respot settle first.
            yield return null;
            yield return null;
            yield return null;
            if (!BallPoolGameLogic.isOnLine || mainCueBall == null) yield break;

            if (BallPoolGameLogic.controlFromNetwork)
            {
                // Watcher: re-apply the AUTHORITATIVE aim (re-sent by the server), so a one-shot that
                // was lost to the init race is corrected without waiting for the opponent to nudge.
                ForceSyncCueControlFromNetwork(pivotY, verticalX, displacement, sliderZ, force);
            }
            else
            {
                // Active player: re-anchor the pivot on the cue ball, deterministically re-aim (unless
                // it is ball-in-hand / a scratch), re-render and re-broadcast so the watcher matches.
                if (mainCueBall.position.y > -0.1f)
                    cuePivotTransform.position = mainCueBall.position;
                if (AightBallPoolGameLogic.gameState != null
                    && !AightBallPoolGameLogic.gameState.cueBallInPocket)
                    AlignCueForBreakAndAim(ConsumeRespottedCueAimPending());
                EstimateShot(true);
                ForceSendCueState();
            }
        }

        /// <summary>
        /// Places the cue pivot on the cue ball and aims it down the table toward the
        /// rack (table centre), with a neutral vertical angle, no spin and no power.
        /// The local-Y aim is computed in the pivot's PARENT space so it stays exactly
        /// consistent with how the rest of the cue system reads/writes the aim
        /// (cuePivotTransform.localRotation = Euler(0, cuePivotYRotation, 0)). Cached sync
        /// fields are refreshed so the per-frame IsCueModified delta does not immediately
        /// re-fire a redundant network update.
        /// </summary>
        public void AlignCueForBreakAndAim(bool preferRackSideAxis = false)
        {
            cuePivotTransform.position = mainCueBall.position;
            Vector3 worldDir = ResolveBreakAimWorldDirection(preferRackSideAxis);
            if (worldDir.sqrMagnitude > 0.0001f)
            {
                Vector3 localDir = (cuePivotTransform.parent != null)
                    ? cuePivotTransform.parent.InverseTransformDirection(worldDir)
                    : worldDir;
                localDir.y = 0.0f;
                if (localDir.sqrMagnitude > 0.0001f)
                {
                    float localY = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                    cuePivotTransform.localRotation = Quaternion.Euler(0.0f, localY, 0.0f);
                }
            }
            cueVerticalAlignment.parent = cuePivotTransform;
            cueVerticalAlignment.localPosition = Vector3.zero;
            cueVerticalAlignment.localRotation = cueVerticalInitialRotation;
            cueDisplacementTransform.localPosition = Vector3.zero;
            cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            cueSliderZDisplacement = 0.0f;
            cueForce = 0.0f;
            if (targetingManager2D != null)
            {
                targetingManager2D.Resset();
            }

            // Keep the cached sync fields in step with the transforms we just set so
            // IsCueModified doesn't report a spurious change next frame.
            cuePivotYRotation = cuePivotTransform.localRotation.eulerAngles.y;
            localCueVerticalRotationX = cueVerticalAlignment.localRotation.eulerAngles.x;
            cueLocalDisplacementXY = new Vector2(cueDisplacementTransform.localPosition.x, cueDisplacementTransform.localPosition.y);
            cueSliderLocalZPosition = cueSliderTransform.localPosition.z;
            EstimateShot(true);
        }

        private Vector3 ResolveBreakAimWorldDirection(bool preferRackSideAxis = false)
        {
            Vector3 origin = cuePivotTransform != null
                ? cuePivotTransform.position
                : mainCueBall != null ? mainCueBall.position : transform.position;

            if (preferRackSideAxis || IsCueBallInHandResetOnTable())
            {
                Vector3 rackSideDir = ResolveRackSideAimWorldDirection(origin);
                if (rackSideDir.sqrMagnitude > 0.0001f)
                    return rackSideDir;
            }

            Vector3 worldDir = Vector3.zero;
            if (cameraTableCenter != null)
            {
                worldDir = cameraTableCenter.position - origin;
                worldDir.y = 0.0f;
            }

            if (worldDir.sqrMagnitude <= 0.0001f)
            {
                Transform rackAnchor = FindRackAimAnchor();
                if (rackAnchor != null)
                {
                    worldDir = rackAnchor.position - origin;
                    worldDir.y = 0.0f;
                }
            }

            if (worldDir.sqrMagnitude <= 0.0001f)
            {
                Transform tableRoot = cuePivotTransform != null && cuePivotTransform.parent != null
                    ? cuePivotTransform.parent
                    : transform;
                worldDir = tableRoot.TransformDirection(Vector3.right);
                worldDir.y = 0.0f;
            }

            return worldDir;
        }

        private Vector3 ResolveRackSideAimWorldDirection(Vector3 origin)
        {
            Transform tableRoot = cuePivotTransform != null && cuePivotTransform.parent != null
                ? cuePivotTransform.parent
                : transform;
            Transform rackAnchor = FindRackAimAnchor();

            float side = 1.0f;
            if (rackAnchor != null && tableRoot != null)
            {
                float localDeltaX = tableRoot.InverseTransformPoint(rackAnchor.position).x
                    - tableRoot.InverseTransformPoint(origin).x;
                if (Mathf.Abs(localDeltaX) > 0.001f)
                    side = Mathf.Sign(localDeltaX);
            }

            Vector3 worldDir = tableRoot != null
                ? tableRoot.TransformDirection(side * Vector3.right)
                : side * Vector3.right;
            worldDir.y = 0.0f;
            return worldDir;
        }

        private Transform FindRackAimAnchor()
        {
            if (cuePivotTransform != null && cuePivotTransform.parent != null)
            {
                Transform rackAnchor = cuePivotTransform.parent.Find("PyramidFirstBallPosition");
                if (rackAnchor != null)
                    return rackAnchor;
            }

            GameObject rackObject = GameObject.Find("PyramidFirstBallPosition");
            return rackObject != null ? rackObject.transform : null;
        }

        private bool IsCueBallInHandResetOnTable()
        {
            return AightBallPoolGameLogic.gameState != null
                && AightBallPoolGameLogic.gameState.cueBallInHand
                && mainCueBall != null
                && mainCueBall.position.y > -0.1f;
        }

        /// <summary>
        /// Sends this client's full current cue state to the opponent as one authoritative
        /// keyframe (snap), rather than relying on the incremental IsCueModified deltas
        /// that only fire while the cue is being moved. Called when the local player gains
        /// control so the opponent's cue is correct from the first frame of the turn.
        /// </summary>
        public void ForceSendCueState()
        {
            if (!BallPoolGameLogic.isOnLine || !NetworkClient.active || MyEightBallNetwork.Instance == null)
            {
                return;
            }
            MyEightBallNetwork.Instance.OnForceSendCueControl(
                cuePivotTransform.localRotation.eulerAngles.y,
                cueVerticalAlignment.localRotation.eulerAngles.x,
                new Vector2(cueDisplacementTransform.localPosition.x, cueDisplacementTransform.localPosition.y),
                cueSliderTransform.localPosition.z,
                cueForce);
        }

        private float checkDistanceBetweenCueAndBall;

        private float GetReflectedCueLineLength(float hitDistance)
        {
            float distanceCap = Mathf.Max(2.0f * cueBallSize, hitDistance * 0.45f);
            return Mathf.Min(lineIndicatorLength * 0.35f, distanceCap);
        }

        private Vector3 GetCueLineRenderOrigin()
        {
            if (cuePivotTransform != null && cuePivotTransform.position.y > -0.1f)
                return cuePivotTransform.position;

            return mainCueBall != null ? mainCueBall.position : Vector3.zero;
        }

        private bool CueLineRenderOriginChanged()
        {
            if (cueBallLineRenderer == null || !cueBallLineRenderer.gameObject.activeInHierarchy)
                return false;

            Vector3 origin = GetCueLineRenderOrigin();
            if (!hasCueLineRenderOrigin)
                return true;

            return (origin - lastCueLineRenderOrigin).sqrMagnitude > CueLineOriginRebuildEpsilon * CueLineOriginRebuildEpsilon;
        }

        private void RememberCueLineRenderOrigin(Vector3 origin)
        {
            lastCueLineRenderOrigin = origin;
            hasCueLineRenderOrigin = true;
        }

        private void InvalidateCueLineRenderOrigin()
        {
            hasCueLineRenderOrigin = false;
        }

        public GameObject cueLineCircleWhite; //RAR
        public GameObject cueLineCircleRed; //RAR


        void EstimateShot(bool forceCalculate)
        {
            if (isMoving || isInShot || (cuePhysicsManager != null && cuePhysicsManager.IsInMove))
            {
                SuppressCueVisualsWhileShotSettling();
                return;
            }

            strikePoint = cueDisplacementTransform.position;
            cueForce = Mathf.Clamp01(-cueSliderZDisplacement / maxCueSlidingDistance);
            if (forceCalculate || StateChanged)
            {
                if (cueForce > 0.03f)
                {
                    cuePhysicsManager.ApplyImpulse(new Impulse(strikePoint, cueForce * (maxSpeed * cuePhysicsManager.BallMass) * cueSliderTransform.forward));
                }
                cuePhysicsManager.HideBallTrajectory();

                // codex fix 3: a prior SuppressCueVisualsWhileShotSettling() (e.g. the Phase 3 client-skip settle)
                // hides the checker/circles/lines. On a successful estimate, re-enable the aim visuals BEFORE
                // positions are set so the aim line + circle come back. (Red/white circle logic below stays as-is.)
                if (ballCheckerTransform != null) ballCheckerTransform.gameObject.SetActive(true);
                if (ballCheckerBoth != null) ballCheckerBoth.SetActive(true);
                if (cueline != null) cueline.SetActive(true);
                if (targetline != null) targetline.SetActive(true);
                if (cueBallLineRenderer != null) cueBallLineRenderer.gameObject.SetActive(true);
                if (targetBallLineRenderer != null) targetBallLineRenderer.gameObject.SetActive(true);

                Vector3 origin = GetCueLineRenderOrigin();
                RememberCueLineRenderOrigin(origin);
                Vector3 direction = Vector3.ProjectOnPlane(cuePivotTransform.forward, Vector3.up).normalized;
                RaycastHit targetShapelHit;

                Color ballCheckerColor = Color.white; //RAR
                cueLineCircleWhite.SetActive(true);
                cueLineCircleRed.SetActive(false);

                savedTargetBallDirection = Vector3.zero;
                targetBallDetector = null;

                if (Physics.SphereCast(origin, cueBallSize, direction, out targetShapelHit, 1.2f * clothPosition.lossyScale.x, ballLayerIndex | tableLayer))
                {
                    BallDetector listener = targetShapelHit.collider.gameObject.GetComponent<BallDetector>();
                    if (listener != null)
                    {
                        Vector3 positionInHit = targetShapelHit.point + cueBallSize * targetShapelHit.normal;
                        ballCheckerTransform.position = positionInHit;

                        if (ballPoolAIManager != null && ballPoolAIManager.FindException(listener.id))
                        {
                            ballCheckerColor = Color.red; //RAR
                            cueLineCircleRed.SetActive(true);
                            cueLineCircleWhite.SetActive(false);
                        }

                        cueBallLineRenderer.positionCount = 3;
                        cueBallLineRenderer.SetPosition(0, origin);
                        cueBallLineRenderer.SetPosition(1, ballCheckerTransform.position);

                        Vector3 secontLineDirection = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(targetShapelHit.normal, Vector3.up).normalized);
                        float tangent = Vector3.Dot(Vector3.ProjectOnPlane(cuePivotTransform.forward, Vector3.up), secontLineDirection);
                        if (tangent < 0.0f)
                        {
                            secontLineDirection = -secontLineDirection;
                        }
                        // codex: restore BetsPanda/PUN full-length reflection. Mirror's GetReflectedCueLineLength
                        // cap (lineIndicatorLength*0.35 / hitDistance*0.45) made the 2nd point sit too close, so the
                        // reflection line drew short/wrong. PUN uses the full lineIndicatorLength here.
                        cueBallLineRenderer.SetPosition(2, (positionInHit + Mathf.Clamp(Mathf.Abs(tangent), 0.2f, 1.0f) * lineIndicatorLength * secontLineDirection));

                        targetBallLineRenderer.positionCount = 2;
                        targetBallLineRenderer.SetPosition(0, listener.body.position);
                        targetBallLineRenderer.SetPosition(1, (listener.body.position + Mathf.Clamp(1.0f - Mathf.Abs(tangent), 0.2f, 1.0f) * lineIndicatorLength * (listener.body.position - positionInHit).normalized));

                        savedTargetBallDirection = (targetBallLineRenderer.GetPosition(1) - targetBallLineRenderer.GetPosition(0)).normalized;
                        targetBallDetector = listener;

                    }
                    else
                    {
                        Vector3 positionInHit = targetShapelHit.point + cueBallSize * targetShapelHit.normal;
                        ballCheckerTransform.position = positionInHit;

                        cueBallLineRenderer.positionCount = 3;
                        cueBallLineRenderer.SetPosition(0, origin);
                        cueBallLineRenderer.SetPosition(1, ballCheckerTransform.position);

                        Vector3 projectOnNormal = Vector3.Project(Vector3.ProjectOnPlane(cuePivotTransform.forward, Vector3.up), Vector3.ProjectOnPlane(targetShapelHit.normal, Vector3.up).normalized);
                        Vector3 reactionDirection = Vector3.ProjectOnPlane(cuePivotTransform.forward, Vector3.up) - 2.0f * projectOnNormal;
                        // codex: restore BetsPanda/PUN cushion reflection — full lineIndicatorLength, raw reactionDirection.
                        cueBallLineRenderer.SetPosition(2, positionInHit + lineIndicatorLength * reactionDirection);

                        targetBallLineRenderer.positionCount = 0;
                    }
                    checkDistanceBetweenCueAndBall = Vector3.Distance(origin, ballCheckerTransform.position);
                }
                else
                {
                    ballCheckerTransform.position = origin + checkDistanceBetweenCueAndBall * Vector3.ProjectOnPlane(cuePivotTransform.forward, Vector3.up);
                    cueBallLineRenderer.positionCount = 2;
                    cueBallLineRenderer.SetPosition(0, origin);
                    cueBallLineRenderer.SetPosition(1, ballCheckerTransform.position);

                    targetBallLineRenderer.positionCount = 0;

                    ballCheckerColor = Color.red; //RAR
                    cueLineCircleRed.SetActive(true);
                    cueLineCircleWhite.SetActive(false);
                }

                ballCheckerColor.a = 0.3f;
                ballCheckerMeshRenderer.sharedMaterial.color = ballCheckerColor;
                cuePhysicsManager.ApplyImpulse(new Impulse(strikePoint, cueForce * (maxSpeed * cuePhysicsManager.BallMass) * cueSliderTransform.forward));
            }
        }
        void DeselectBall()
        {
            isBallInHand = false;
            //Photon Removal    PunNetwork.instance.CallingShadowToggleMethod(isBallInHand);
            mainCueBall.position = currentBallPosition;
            mainCueBall.OnState(BallState.SetState);
            cuePivotTransform.position = mainCueBall.position;
            EstimateShot(true);
            if (BallDeselected != null)
            {
                BallDeselected();
            }
        }
        void TryingToPickBall()
        {
            isBallInHand = false;
            //Photon Removal     PunNetwork.instance.CallingShadowToggleMethod(isBallInHand);
            Vector3 handScreenPosition = InputOutput.WorldToScreenPoint(playerHand.position);
            float handRadius = InputOutput.WorldToScreenRadius(0.5f * playerHand.lossyScale.x, playerHand);
            float handDistance = Vector3.Distance(handScreenPosition, InputOutput.mouseScreenPosition);
            if (handDistance < handRadius)
            {
                isBallInHand = true;
                //Photon Removal       PunNetwork.instance.CallingShadowToggleMethod(isBallInHand);
                if (BallSelected != null)
                {
                    BallSelected();
                }
            }
            else if (cueControlMode == CueViewMode.ThirdPerson || !AightBallPoolNetworkGameAdapter.is3DGraphics)
            {
                Vector3 centerPointInSceen = InputOutput.WorldToScreenPoint(cuePivotTransform.position);
                float cueBallRadiusInScreen = InputOutput.WorldToScreenRadius(cueBallSize, mainCueBall.transform);
                Vector3 mouseScreenPosition = InputOutput.mouseScreenPosition;

                if (Vector3.Distance(centerPointInSceen, mouseScreenPosition) <= 5.0f * cueBallRadiusInScreen)
                {
                    isBallInHand = true;
                    //Photon Removal       PunNetwork.instance.CallingShadowToggleMethod(isBallInHand);
                    if (BallSelected != null)
                    {
                        BallSelected();
                    }
                }
            }
        }
        void CheckHandStatus()
        {
            Vector3 handScreenPosition = InputOutput.WorldToScreenPoint(playerHand.position);
            float handRadius = InputOutput.WorldToScreenRadius(0.5f * playerHand.lossyScale.x, playerHand);
            float handDistance = Vector3.Distance(handScreenPosition, InputOutput.mouseScreenPosition);
            //Constants_M.LogInfo("AightBallPoolGameLogic.gameState.cueBallInHand " + AightBallPoolGameLogic.gameState.cueBallInHand);
            if (shotBackImage.enabled || (InputOutput.isMobilePlatform && currentMouseState != MouseState.Up) || (!InputOutput.isMobilePlatform && (currentMouseState == MouseState.Press || currentMouseState == MouseState.PressAndMove || currentMouseState == MouseState.PressAndStay)))
            {
                playerHand.gameObject.SetActive(false);

            }
            // The ball-in-hand HAND indicator must only ever show on the client that actually has
            // control. After a reconnect, gameState.cueBallInHand is restored on the WATCHER too
            // (and shotBackImage can briefly be disabled mid-restore), so without this guard the
            // watcher showed the hand as if it could drag the cue ball — "hand indicator visible
            // as if the player could continue interacting" with the white ball stuck in a pocket.
            else if (!BallPoolGameLogic.controlFromNetwork && cueMode != CueMode.StretchCue && cueMode != CueMode.TargetingAtCueBall && AightBallPoolGameLogic.gameState.cueBallInHand && !isBallInHand && (handDistance < 3.0f * handRadius || InputOutput.isMobilePlatform) && !playerHand.gameObject.activeInHierarchy)
            {
                playerHand.gameObject.SetActive(true);
                //   GameUIController.instance.cue2dRenderer.gameObject.SetActive(false);
            }
            else if (BallPoolGameLogic.controlFromNetwork || !AightBallPoolGameLogic.gameState.cueBallInHand || isBallInHand || (handDistance > 3.5f * handRadius && playerHand.gameObject.activeInHierarchy && !InputOutput.isMobilePlatform) || cueMode == CueMode.TargetingAtCueBall || cueMode == CueMode.StretchCue)
            {
                playerHand.gameObject.SetActive(false);

            }
            if (gameUIManager.is3DMode || InputOutput.isMobilePlatform || !gameUIManager.isShotOnRelease)
            {
                playerHand.position = cuePivotTransform.position + 4.0f * cueBallSize * cuePivotTransform.right;
                //  hand.position = cuePivot.position;
                playerHand.right = cuePivotTransform.right;
            }
            else
            {
                playerHand.position = cuePivotTransform.position - 4.0f * cueBallSize * Vector3.forward;
                //hand.position = cuePivot.position;
                playerHand.localRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
            }
        }
        // Offline/AI games have no MyEightBallNetwork — it is server-spawned in ONLINE matches only — and
        // the Instance can also be briefly null online while the object respawns across a reconnect. The
        // ball-in-hand path used to dereference it unguarded, which threw a NullReferenceException EVERY
        // FRAME the player tried to place the cue ball in an AI game (stack: ShotController.TryMoveBall ->
        // OnMouseState -> InputOutput.Update; tester reports f319794d / 0e095d43 / e56fa82f / 79e90261 —
        // the whole "stuck" cluster: placement never completed, the turn timed out to the AI). The Cmd only
        // mirrors cue visibility to the OPPONENT, which offline does not exist; local rig visibility is
        // handled by the explicit SetActive calls at the call sites. Same guard pattern as line ~507.
        void TrySendCueStateToOpponent(bool isActive)
        {
            if (BallPoolGameLogic.isOnLine && MyEightBallNetwork.Instance != null)
                MyEightBallNetwork.Instance.CmdSwitchCueState(isActive);
        }

        // One-shot so the diagnostic below cannot itself become log spam.
        private bool tryMoveBallNullLogged;

        void TryMoveBall()
        {
            // The Cmd guards (TrySendCueStateToOpponent) killed one NRE family here, but tester log
            // 770ce0cc (2026-07-21, AI game) shows the ball-in-hand drag STILL throwing every frame —
            // 161 identical TryMoveBall stacks. The remaining dereferences are mainCueBall (and its
            // listener/body inside Ball.position's setter) and AightBallPoolGameLogic.gameState. The drag
            // is meaningless without them, so bail out instead of throwing every frame; the one-shot
            // warning names WHICH reference was dead so the next tester log pins the producer.
            if (mainCueBall == null || mainCueBall.listener == null || mainCueBall.listener.body == null
                || AightBallPoolGameLogic.gameState == null)
            {
                if (!tryMoveBallNullLogged)
                {
                    tryMoveBallNullLogged = true;
                    Debug.LogWarning("[8Ball][BallInHand] TryMoveBall aborted — mainCueBall="
                        + (mainCueBall == null ? "NULL" : (mainCueBall.listener == null ? "listener NULL" : (mainCueBall.listener.body == null ? "body NULL" : "ok")))
                        + ", gameState=" + (AightBallPoolGameLogic.gameState == null ? "NULL" : "ok")
                        + ". Drag ignored (was an every-frame NRE, log 770ce0cc).");
                }
                return;
            }
            tryMoveBallNullLogged = false;

            RaycastHit clothHit;
            Vector3 origin = InputOutput.mouseWordRay.origin;
            Vector3 direction = InputOutput.mouseWordRay.direction;
            if (Physics.Raycast(origin, direction, out clothHit, 3.0f, cueBallClothLayer))
            {
                RaycastHit ballHitInfo;
                if (!Physics.SphereCast(origin, cueBallSize, direction, out ballHitInfo, 3.0f, ballLayerIndex))
                {
                    Vector3 ballNewPosition = clothHit.point + cueBallSize * clothHit.normal;
                    Vector3 ballNewPositionInClothSpace = BallGeometry.ClampPositionInCube(ballNewPosition, cueBallSize, AightBallPoolGameLogic.gameState.tableIsOpened ? initialMoveSpace : clothPosition);
                    // FIX (ONLINE ONLY): never place the cue ball where it would touch/overlap another
                    // ball during ball-in-hand. Other balls are on `ballLayerIndex`; the cue ball is on
                    // its own `layerForCueBall`, so this CheckSphere only detects OTHER balls (no
                    // self-hit). If the target would overlap one, keep the cue ball at its last valid
                    // spot instead of pushing that resting ball out of place. Offline/AI keeps the
                    // original unrestricted placement.
                    if (!BallPoolGameLogic.isOnLine || !Physics.CheckSphere(ballNewPositionInClothSpace, cueBallSize, ballLayerIndex))
                    {
                        mainCueBall.position = ballNewPositionInClothSpace;
                        currentBallPosition = ballNewPositionInClothSpace;
                        lerpedBallPosition = currentBallPosition;
                        mainCueBall.OnState(BallState.SetState);
                    }

                }
                TrySendCueStateToOpponent(false);

            }
            else
            {
                TrySendCueStateToOpponent(true);

            }
        }
        [FormerlySerializedAs("ballLine")] public GameObject aimLine;
        [FormerlySerializedAs("BallChecker")] public GameObject ballValidator;
        public void SwitchCue(bool active)
        {
            // OFFLINE/AI: original (merge-base) behaviour — toggle only the line-side objects.
            if (!BallPoolGameLogic.isOnLine)
            {
                GameUIController.instance.CueRenderer2D.gameObject.SetActive(active);
                aimLine.SetActive(active);
                targetBallLineRenderer.gameObject.SetActive(active);
                ballValidator.SetActive(active);
                return;
            }
            // ONLINE: toggle the WHOLE cue rig (stick + line together) so the stick is NEVER left off
            // while the line shows. Earlier this toggled only the line-side objects (CueRenderer2D /
            // aimLine / targetBallLineRenderer / ballValidator) and NOT CuePivotStick / cueline /
            // targetline — so the reconnect path (RpcSwitchCueState -> SwitchCue) showed the aim line
            // with no stick. Any explicit SHOW also lifts the reconnect suppression — suppression only
            // means "keep hidden until real evidence the rig should be up", and a show relay IS that
            // evidence.
            if (active)
                networkCueHiddenForBallInHandDrag = false;
            SetCueRigVisible(active);
            //print("toggle cue");
        }
        public void IsCueExtended()
        {
            isInShot.Show();
            if (isInShot)
            {
                return;
            }
            isCueStretched = true;
            cueMode = CueMode.StretchCue;
            cueSliderZDisplacement = Mathf.Lerp(0, -maxCueSlidingDistance, cueForceSlider.value);
            cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderZDisplacement);
            savedCueSliderPos = cueSliderTransform.localPosition;
            EstimateShot(true);
        }
        void ResetCueTargeting()
        {
            if (cueMode == CueMode.TargetingAtCueBall)
            {
                if (cueControlMode == CueViewMode.FirstPerson)
                {
                    cueCameraView.transform.position = defaultCameraPosition.position;
                    cueCameraView.transform.rotation = defaultCameraPosition.rotation;
                }
                cueSliderTransform.localPosition = savedCueSliderPos;
                if (isFrom2D)
                {
                    gameUIManager.is3DMode = false;
                    gameUIManager.ToggleCameraView();
                    isFrom2D = false;
                }
                cueMode = CueMode.TargetingAtTargetBall;
            }
        }
        public void ResetCueStateAfterTargeting()
        {
            cueSliderTransform.localPosition = savedCueSliderPos;
        }
        public void PrepCueForTargeting()
        {
            cueDisplacementTransform.localPosition = new Vector3(cueDisplacementTransform.localPosition.x, cueDisplacementTransform.localPosition.y, 0.0f);
            savedCueSliderPos = cueSliderTransform.localPosition;
            cueSliderTransform.localPosition = Vector3.zero;
        }
        public void CueAimSnap(Vector3 normalizedPosition)
        {
            cueDisplacementTransform.localPosition = cueBallDisplacement = normalizedPosition * cueBallSize;
        }
        void controlModeIs2D(MouseState mouseState)
        {
            if (cueControlMode == CueViewMode.FirstPerson && mouseState == MouseState.Down)
            {
                Vector3 centerPointInSceen = InputOutput.WorldToScreenPoint(cuePivotTransform.position);
                float cueBallRadiusInScreen = InputOutput.WorldToScreenRadius(cueBallSize, mainCueBall.transform);
                Vector3 mouseScreenPosition = InputOutput.mouseScreenPosition;

                if (AightBallPoolNetworkGameAdapter.is3DGraphics && Vector3.Distance(centerPointInSceen, mouseScreenPosition) <= 5.0f * cueBallRadiusInScreen)
                {
                    cueMode = CueMode.TargetingAtCueBall;
                    if (cueControlMode == CueViewMode.FirstPerson)
                    {
                        cueCameraView.transform.position = cameraAimingSpot.position;
                        cueCameraView.transform.rotation = cameraAimingSpot.rotation;
                    }
                    PrepCueForTargeting();
                    gameUIManager.is3DMode = true;
                    gameUIManager.ToggleCameraView();
                    isFrom2D = true;
                }
                else
                {
                    cueMode = CueMode.Non;
                }
            }
            else if (mouseState == MouseState.Press || (gameUIManager.isShotOnRelease && mouseState == MouseState.Move))
            {
                Vector3 cueScreenPivot0 = InputOutput.WorldToScreenPoint(cueSliderTransform.position);
                Vector3 cueScreenPivot1 = InputOutput.WorldToScreenPoint(cueSliderTransform.position - cueSliderTransform.forward);
                Vector3 cueScreenDirection = -(cueScreenPivot1 - cueScreenPivot0).normalized;

                Vector3 mouseScreenSpeed = InputOutput.mouseScreenSpeed;
                float mouseScreenSpeedToCue = Vector3.Dot(mouseScreenSpeed, cueScreenDirection);
                float mouseScreenSpeedToCueDirection = Vector3.Dot(mouseScreenSpeed.normalized, cueScreenDirection);

                if (!hasSimpleControl && Mathf.Abs(mouseScreenSpeedToCueDirection) > 0.9f)
                {
                    cueMode = CueMode.StretchCue;
                    cueSliderZDisplacement += cueSlidingSpeed * mouseScreenSpeedToCue * Time.deltaTime;
                    cueSliderZDisplacement = Mathf.Clamp(cueSliderZDisplacement, -maxCueSlidingDistance, 0.0f);
                    cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderZDisplacement);
                    savedCueSliderPos = cueSliderTransform.localPosition;
                }
                else
                {
                    cueMode = CueMode.TargetingAtTargetBall;
                    if (InputOutput.isMobilePlatform)
                    {
                        Vector3 cueBallScreenPoint = InputOutput.WorldToScreenPoint(mainCueBall.position);
                        float orientX = InputOutput.mouseScreenPosition.x > cueBallScreenPoint.x ? 1.0f : -1.0f;
                        float orientY = InputOutput.mouseScreenPosition.y > cueBallScreenPoint.y ? 1.0f : -1.0f;
                        float rSpeed2D = (orientY * InputOutput.mouseScreenSpeed.x - orientX * InputOutput.mouseScreenSpeed.y) * Time.deltaTime;

                        float mouseScreenSpeedValue = 100.0f * cueRotationSpeedCurve.Evaluate(rSpeed2D / 100.0f) * cueRotationSpeed2D;
                        cuePivotTransform.Rotate(cuePivotTransform.up, mouseScreenSpeedValue);
                    }
                    else
                    {
                        if (gameUIManager.isShotOnRelease && mouseState == MouseState.Press)
                        {
                            cueMode = CueMode.StretchCue;
                            cueSliderZDisplacement += cueSlidingSpeed * mouseScreenSpeedToCue * Time.deltaTime;
                            cueSliderZDisplacement = Mathf.Clamp(cueSliderZDisplacement, -maxCueSlidingDistance, 0.0f);
                            cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderZDisplacement);
                            savedCueSliderPos = cueSliderTransform.localPosition;
                        }
                        else if (cueForce < 0.03f || !gameUIManager.isShotOnRelease)
                        {
                            Vector3 mouseWordDirectionFromCuePivot = Vector3.ProjectOnPlane(InputOutput.mouseWordPosition - cuePivotTransform.position, Vector3.up).normalized;
                            Quaternion cuePivotRotation = Quaternion.identity;
                            cuePivotRotation.SetLookRotation(mouseWordDirectionFromCuePivot);
                            cuePivotTransform.rotation = Quaternion.Lerp(cuePivotTransform.rotation, cuePivotRotation, 10.0f * Time.deltaTime);
                        }
                    }
                }
            }
            else if (mouseState == MouseState.Up)
            {


            }
        }
        IEnumerator MoveCameraToDefaultPosition()
        {
            while (Vector3.Distance(cueCameraView.transform.position, defaultCameraPosition.position) > 0.1f)
            {
                cueCameraView.transform.position = Vector3.Lerp(cueCameraView.transform.position, defaultCameraPosition.position, 10.0f * Time.deltaTime);
                cueCameraView.transform.rotation = Quaternion.Lerp(cueCameraView.transform.rotation, defaultCameraPosition.rotation, 10.0f * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }
            cueCameraView.transform.position = defaultCameraPosition.position;
            cueCameraView.transform.rotation = defaultCameraPosition.rotation;
        }
        private void TargetBallInThirdPerson()
        {
            if (!InputOutput.isMobilePlatform)
            {
                Ray ray = cueCameraView.ScreenPointToRay(InputOutput.mouseScreenPosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 10.0f, cueThirdPersonControlLayer))
                {
                    Vector3 targetPoint = new Vector3(hit.point.x, cuePivotTransform.position.y, hit.point.z);
                    cuePivotTransform.LookAt(targetPoint);
                }
            }
            else
            {
                Vector3 cueBallScreenPoint = InputOutput.WorldToScreenPoint(mainCueBall.position);
                float orientX = InputOutput.mouseScreenPosition.x > cueBallScreenPoint.x ? 1.0f : -1.0f;
                float orientY = InputOutput.mouseScreenPosition.y > cueBallScreenPoint.y ? 1.0f : -1.0f;
                float rSpeed2D = (orientY * InputOutput.mouseScreenSpeed.x - orientX * InputOutput.mouseScreenSpeed.y) * Time.deltaTime;

                float mouseScreenSpeedValue = 100.0f * cueRotationSpeedCurve.Evaluate(rSpeed2D / 100.0f) * cueRotationSpeed2D;
                cuePivotTransform.Rotate(cuePivotTransform.up, mouseScreenSpeedValue);
            }
        }
        void ControlIn3DMode(MouseState mouseState)
        {
            if (mouseState == MouseState.Down)
            {
                if (cueControlMode == CueViewMode.ThirdPerson && !InputOutput.isMobilePlatform)
                {
                    cueMode = CueMode.StretchCue;
                    cueDirectionOnScreen = (cueCameraView.WorldToScreenPoint(cuePivotTransform.position + cuePivotTransform.forward) - cueCameraView.WorldToScreenPoint(cuePivotTransform.position)).normalized;
                    TargetBallInThirdPerson();
                }
                else if (cueControlMode == CueViewMode.FirstPerson)
                {
                    Vector3 centerPointInSceen = InputOutput.WorldToScreenPoint(cuePivotTransform.position);
                    float cueBallRadiusInScreen = InputOutput.WorldToScreenRadius(cueBallSize, mainCueBall.transform);
                    Vector3 mouseScreenPosition = InputOutput.mouseScreenPosition;

                    if (Vector3.Distance(centerPointInSceen, mouseScreenPosition) <= 5.0f * cueBallRadiusInScreen)
                    {
                        cueMode = CueMode.TargetingAtCueBall;
                        cueCameraView.transform.position = cameraAimingSpot.position;
                        cueCameraView.transform.rotation = cameraAimingSpot.rotation;
                        PrepCueForTargeting();
                    }
                    else if (!hasSimpleControl && Mathf.Abs(InputOutput.mouseViewportSymmetricalPoint.x) < 0.075f)
                    {
                        cueMode = CueMode.TargetingFromTop;
                    }
                    else
                    {
                        cueMode = CueMode.TargetingAtTargetBall;
                    }
                }
            }
            else if (mouseState == MouseState.Up)
            {
                if (cueControlMode == CueViewMode.ThirdPerson)
                {
                    cueMode = CueMode.TargetingAtTargetBall;
                }
                else if (cueControlMode == CueViewMode.FirstPerson)
                {
                    StartCoroutine(MoveCameraToDefaultPosition());
                    cueSliderTransform.localPosition = savedCueSliderPos;
                    if (isFrom2D)
                    {
                        gameUIManager.is3DMode = false;
                        gameUIManager.ToggleCameraView();
                        isFrom2D = false;
                    }
                    cueMode = CueMode.TargetingAtTargetBall;
                }
            }
            else if (mouseState == MouseState.Press || (gameUIManager.isShotOnRelease && mouseState == MouseState.Move))
            {
                if (cueMode == CueMode.TargetingAtCueBall)
                {
                    Vector3 mouseScreenSpeed = targetingMovementSpeed * InputOutput.mouseScreenSpeed * Time.deltaTime;
                    cueBallDisplacement += new Vector3(mouseScreenSpeed.x, mouseScreenSpeed.y, 0.0f);
                    cueBallDisplacement = Vector3.ClampMagnitude(cueBallDisplacement, cueBallSize);
                    cueDisplacementTransform.localPosition = cueBallDisplacement;
                }
                else if (!hasSimpleControl && cueMode == CueMode.TargetingFromTop)
                {
                    Vector3 mouseScreenSpeed = cueRotationSpeed3D * InputOutput.mouseScreenSpeed * Time.deltaTime;
                    cameraZRotation += mouseScreenSpeed.y;
                    cameraZRotation = Mathf.Clamp(cameraZRotation, minCueVerticalAngle, maxCueVerticalAngle);
                    cueVerticalAlignment.localRotation = Quaternion.Euler(cameraZRotation, 0.0f, 0.0f);
                }
                else if (cueMode == CueMode.TargetingAtTargetBall)
                {
                    if (cueControlMode == CueViewMode.FirstPerson)
                    {
                        Vector3 screenRotateSpeed = InputOutput.mouseScreenSpeed * Time.deltaTime;
                        Vector3 screenRotateSpeedNormalized = screenRotateSpeed.normalized;

                        Vector3 screenSlideSpeed = 1.5f * cueSlidingSpeed * InputOutput.mouseScreenSpeed * Time.deltaTime;

                        float k = Mathf.Abs(screenRotateSpeedNormalized.x);
                        if (k < 0.8f)
                        {
                            if (hasSimpleControl)
                            {
                                if (!InputOutput.isMobilePlatform && gameUIManager.isShotOnRelease && mouseState == MouseState.Press)
                                {
                                    Vector3 cueBallScreenPoint = InputOutput.WorldToScreenPoint(mainCueBall.position);
                                    if (cueBallScreenPoint.y > InputOutput.mouseScreenPosition.y || screenSlideSpeed.y > 0.0f)
                                    {
                                        cueSliderZDisplacement += screenSlideSpeed.y;
                                        cueSliderZDisplacement = Mathf.Clamp(cueSliderZDisplacement, -maxCueSlidingDistance, 0.0f);
                                        cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderZDisplacement);
                                        savedCueSliderPos = cueSliderTransform.localPosition;
                                    }
                                    if (cueControlMode == CueViewMode.FirstPerson)
                                    {
                                        if (InputOutput.mouseScreenPosition.y < 0.4f * Screen.height)
                                        {
                                            cueCameraView.transform.position = Vector3.Lerp(cueCameraView.transform.position, defaultCameraPosition.position, 15.0f * Time.deltaTime);
                                            cueCameraView.transform.rotation = Quaternion.Lerp(cueCameraView.transform.rotation, defaultCameraPosition.rotation, 15.0f * Time.deltaTime);
                                        }
                                        else if (InputOutput.mouseScreenPosition.y > 0.5f * Screen.height)
                                        {
                                            cueCameraView.transform.position = Vector3.Lerp(cueCameraView.transform.position, cueTargeting3DPosition.position, 5.0f * Time.deltaTime);
                                            cueCameraView.transform.rotation = Quaternion.Lerp(cueCameraView.transform.rotation, cueTargeting3DPosition.rotation, 5.0f * Time.deltaTime);
                                        }
                                    }
                                }
                                else
                                {
                                    Vector3 mouseScreenSpeed = cueRotationSpeed3D * InputOutput.mouseScreenSpeed * Time.deltaTime;
                                    cameraZRotation += mouseScreenSpeed.y;
                                    cameraZRotation = Mathf.Clamp(cameraZRotation, minCueVerticalAngle, maxCueVerticalAngle);
                                    cueVerticalAlignment.localRotation = Quaternion.Euler(cameraZRotation, 0.0f, 0.0f);
                                }
                            }
                            else
                            {
                                cueSliderZDisplacement += screenSlideSpeed.y;
                                cueSliderZDisplacement = Mathf.Clamp(cueSliderZDisplacement, -maxCueSlidingDistance, 0.0f);
                                cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderZDisplacement);
                                savedCueSliderPos = cueSliderTransform.localPosition;
                            }
                        }
                        else if (mouseState == MouseState.Press)
                        {
                            if (InputOutput.mouseScreenPosition.y > 0.5f * Screen.height)
                            {
                                cueCameraView.transform.position = Vector3.Lerp(cueCameraView.transform.position, cueTargeting3DPosition.position, 5.0f * Time.deltaTime);
                                cueCameraView.transform.rotation = Quaternion.Lerp(cueCameraView.transform.rotation, cueTargeting3DPosition.rotation, 5.0f * Time.deltaTime);
                            }

                            float rSpeed = screenRotateSpeed.x;
                            cuePivotTransform.Rotate(cuePivotTransform.up, (InputOutput.isMobilePlatform ? cueRotationSpeed3D : mouseRotationSpeedFor3D) * 50.0f * cueRotationSpeedCurve.Evaluate(rSpeed / 50.0f));
                        }
                    }
                    else if (cueControlMode == CueViewMode.ThirdPerson)
                    {
                        TargetBallInThirdPerson();
                    }
                }
                else if (cueMode == CueMode.StretchCue)
                {
                    Vector3 mouseScreenSpeed = cueSlidingSpeed * InputOutput.mouseScreenSpeed * Time.deltaTime;
                    cueSliderZDisplacement += Vector3.Dot(mouseScreenSpeed, cueDirectionOnScreen); ;
                    cueSliderZDisplacement = Mathf.Clamp(cueSliderZDisplacement, -maxCueSlidingDistance, 0.0f);
                    cueSliderTransform.localPosition = new Vector3(0.0f, 0.0f, cueSliderZDisplacement);
                    savedCueSliderPos = cueSliderTransform.localPosition;
                }
            }
        }
    }
}

