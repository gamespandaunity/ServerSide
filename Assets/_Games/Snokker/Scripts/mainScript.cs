using ExitGames.Client.Photon;
using Mirror;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensions;

public class mainScript : MonoBehaviour
{
    public SnokerGameManager _SnokerGameManager;
    public SnokerUIManager _SnokerUIManager;

    #region Variables
    public float ballRadius = 0.03245f;
    public float turnDuration = 10f;
    public Image playerOneTimer; public Image playerTwoTimer;
    public int i;
    public int j;
    public string version = string.Empty;
    public int THIS_GAME_ID = 34;
    public string GAME_NAME_STRING = "REAL POOL 3D";
    public SnokerNetwork _snokerNetwork;
    public float screenWidth = Screen.width;
    public float screenHeight = Screen.height;
    public float screenMul;
    public float SCREEN_REF_WIDTH = 480f;
    public bool iPadDevice;
    public Rect canvasSize;
    public static int startupCounter = 0;
    public float iPhoneXSideIndent;
    public float touchDeviderPlatformWise = 15f;
    public MODE_TYPE GameMode = MODE_TYPE.AI;
    public int TOTAL_SNOOKER_BALLS_COUNT = 21;

    public int TOTAL_BALL_TYPES = 3;

    public int TOTAL_HOLES_COUNT = 6;

    public float BALL_STANDING_VEL = 0.1f;

    public float BALL_ROT_SPEED = 2f;

    public float BALL_STOPPER_A = 0.993f;

    public float BALL_STOPPER_B = 0.99f;

    public float BALL_STOPPER_C = 0.96f;

    public int BALL_STOPPER_CHECK_A = 4;

    public int BALL_STOPPER_CHECK_B = 2;

    public float S_CAST_RADIUS_SM = 0.48f;

    public float S_CAST_RADIUS_BIG = 0.0649f;

    public float S_CAST_RADIUS_FULL = 0.5f;


    public Vector3 CUEBALL_START_SNOOKER_POS = new Vector3(3, 13.95f, -20.93f);

    public int HOLE_GRAVITY_FORCE = -60;

    public Vector3 MAX_PLAY_AREA_LIMIT = new Vector3(11.5f, 0f, 24.3f);


    public int strikeCount;

    public int ballsPottedCount;

    public AI_DIFFICULTY aiDifficulty = AI_DIFFICULTY.HARD;

    public bool canRePlaceCueBall;

    public bool legalBreakDone;

    public static int railHitCountInThisShot = 0;

    public static int[] railHitBallArray;

    public bool ballsReplaced = true;

    public bool ballPottedInThisTurn = true;

    public bool firstBallTouched;

    public static bool foulInThisTurn;

    public bool cueBallPotted;

    public bool cueRailHit;

    public Vector3 cueBallMeshAnimVel;

    public float cueBallVerticalAnimValue;

    public float cueBallVerticalAnimTarget;

    public float cueBallVerticalAnimVel;

    public float cueBallVerticalAnimTopVal = 1f;



    public LayerMask tableSideLayerMask;

    public LayerMask boundingBoxLayerMask;

    public float cueDistance;

    public float aiCueAnimValue;

    public float cueRotValueX;

    public float cueRotValueY;

    public float cueRotTargetY;

    public float cueRotVelY;

    public Vector3 cueSetPosAnimVel;

    public Vector3 cueSetPosRotAnimVel;

    public float touchSpeed;

    public Touch touchDataMove;

    public Touch touchDataZoom0;

    public Touch touchDataZoom1;

    public Vector2 inputValues = Vector2.zero;

    private Vector2 inputToRotValue;

    private Vector2 inputToRotTarget;

    private Vector2 inputToRotVel = Vector2.zero;

    private float fineAimMultiplier = 1f;

    public LayerMask ballLineLayerMask;

    public LayerMask ballsLayerMask;

    public Transform guideMainLineTransform;

    public Renderer guideMainLineRenderer;

    public Transform guideColRingTransform;

    public Renderer guideColRingRenderer;

    public Transform guideDirCueBallTrans;

    public Transform guideDirCueBallScalerTrans;

    public Renderer guideDirCueBallRenderer;

    public Transform guideDirTargetTrans;

    public Transform guideDirTargetScalerTrans;

    public GameObject guideDirTargetMesh;

    public Renderer guideDirTargetRenderer;

    public RaycastHit lineHit;

    public Vector3 guideColRingPosVec;

    public Vector3 guideReflectDirVec;

    public float guideTempAngle;

    public Color guideRedColor = new Color(1f, 0f, 0f, 26f / 51f);

    public Color guideWhiteColor = new Color(1f, 1f, 1f, 0.47058824f);

    public bool showGuideForAI;

    public Vector3 lastTargetVector;

    public Vector3 cueBallReboundVector;

    public float velocityOnHit;

    public Vector3 aimColRingPosOnHit;

    public GameObject firstTargetBallToHit;

    public float angleOnHit;

    public bool checkFixedUpdateBallTouch;

    public float cueBallToAimColRingDistanceOnHit;

    public Vector3 cueBallPosOnHit;

    public bool collidedWithSide;

    public int avatarSetToUse = 2;

    public static Sprite[] avatarTexArray;

    public int avatarPlayerToChoose;

    public static int[] chosenAvatar = new int[2];

    public string[] avatarNames = new string[11]
    {
        "Alice", "Jack", "Emily", "David", "Anna", "James", "Ashley", "Will", "Jennifer", "Adam",
        "Player"
    };

    public string[] playerNames = new string[2]
    {
        string.Empty,
        string.Empty
    };

    public LayerMask ballsTouchLayerMask;

    public int tapToAimBallNum = -1;

    public float tapToAimStartTime;

    private int[] snookerPosSwitchArray = new int[21]
    {
        0, 13, 11, 9, 7, 10, 2, 3, 8, 4,
        14, 1, 12, 6, 5, 15, 16, 17, 18, 19,
        20
    };

    public int practiceCurRackNum;

    public int TIME_TRIAL_TOTAL_TIME = 240;

    public int ttCurrentTime;

    public float ttScoreMultiplier = 1f;

    public int ttBackToBackPots;

    public int ttCurrentRackNum;

    public int MATRIX_DEFAULT_LIVES = 1;

    public int matrixLivesLeft;

    public int[] matrixBallsPottedArray;

    public bool bShowMatrixBallUiNums;

    public LayerMask ballReSpotLayerMask;

    public int snookerFirstTouchedBallNum;

    public int snookerBallInvolvedInFoul;

    public int snookerPointsCurShot;

    public int snookerNominatedBall = 1;

    public int[] snookerScoresVal = new int[2];

    public bool powerMeterActive;

    public float strikePowerVal;

    public bool settingPowerNow;

    public float controlSetPowerLastPower = 1f;

    public float controlPowerFlickSliderVal;

    public LayerMask cueLayerMask;

    public float controlDragSliderVal;

    public Vector2 controlDragTapStartPos = Vector2.zero;

    public bool controlDragDone = true;
    public bool aiPlaying;

    private MOUSE_CLICK_AREA controlDragMouseClickArea = MOUSE_CLICK_AREA.LEFT;

    public float controlDragShotStartTime;

    public string alertOptionalTextPrefix;

    public Sprite[] guiBallsTex = new Sprite[24];

    public int guiBallEmptyRingIndex = 23;

    public int guiBallIntToDisplay;

    public Sprite[] guideSelectionTex;

    public Sprite[] controlsTexArray;

    public Texture tableTexture;

    public Texture tablePatternTexture;

    public static bool spinSetOn;

    public int spinSetVectorMaxDistance = 100;

    public Vector2 spinValues = Vector2.zero;

    public bool spinApply;

    public Vector2 spinCuePos = Vector2.zero;

    public Vector3 cueBallHitVector;

    public AudioSource musicAudioSource;

    public AudioSource thisAudioComponent;

    public Transform railHitAudioTransform;

    public AudioSource railHitAudioComponent;

    public AudioClip buttonClickSound;

    public AudioClip[] cueballHitSounds = new AudioClip[2];

    public AudioClip[] ballPocketSounds = new AudioClip[3];

    public AudioClip[] ballsHitSound = new AudioClip[2];

    public AudioClip railHitSoundClip;

    public AudioClip gameWinSound;

    public static Vector3 specLookAtPos;
    public bool userSelControlDone;
    public Transform thisTransform;

    public Rigidbody thisRigidbody;

    public GameObject cueBallParentObj;

    public Transform cueBallParentTrans;

    public Transform cueBallMashParentTrans;

    public GameObject[] cueBallTypesArray = new GameObject[2];

    public Transform cueParentObjTransform;

    public Transform cueObjectTransform;

    public Transform cueGroupTransform;

    public Transform cueSetPosTransform;

    public Transform cueSetPosHoldingParentTrans;

    public Transform cueSetPosHoldingRotatorTrans;

    public Transform cueShadowTransform;

    public GameObject cueShadowMesh;


    public GameObject ballInHandIndicatorObj;

    public Transform ballInHandIndicatorTrans;

    public Transform ballInHandIndicatorMeshTrans;

    public Renderer tableTopBoardRenderer;

    public RectTransform canvasAllParentRectTrans;

    public RectTransform uiTouchParticleRectTrans;

    public ParticleSystem uiTouchParticleSystem;

    public GameObject okBtnObj;

    public RectTransform okBtnParentRectTrans;

    public RectTransform leftSideBtnsParentRectTrans;

    public GameObject placeCueBtnObj;

    public GameObject spinBtnObj;

    public GameObject matrixBallNumBtnObj;

    public RectTransform powerMetersParentRectTrans;

    public GameObject powerMeterPowerFlickObj;

    public Slider powerFlickSliderObj;

    public Image powerFlickFillImgComp;

    public GameObject bottomBlinkingTextObj;

    public Text bottomBlinkingTextText;

    public GameObject spinControlParentObj;

    public RectTransform spinControlGroupRectTrans;

    public uiAnimatorSpinControl spinControlGroupAnimScript;

    public RectTransform spinSetThumbRectTrans;

    public RectTransform spinOkBtnRectTrans;

    public RectTransform spinThumbInsideBtnRectTrans;

    public GameObject guiBallDisplayObj;

    public Image guiBallDisplayImg;

    public Text guiScoreDisplayText;

    public Text guiScoreMultiplierText;

    public Text guiRightSideText;

    public Text guiRightSideTitleText;

    public GameObject matrixBallNumsParent;

    public Image[] ttBallsDisplayImgs = new Image[6];

    public Image[] matrixBallsDisplayImgs = new Image[15];

    public Text[] matrixBallsDisplayTexts = new Text[15];

    public RectTransform[] matrixBallNumsRects = new RectTransform[15];

    public GameObject igSnookerTextsParent;

    public Image igSnookerBallDisplayImg;

    public Transform igSnookerTurnIndicator;

    public TextMeshProUGUI[] snookerScoresText = new TextMeshProUGUI[2];

    public Text[] snookerScoresNameText = new Text[2];

    public Text selectCueTitleText;

    public Transform[] selectCueBtnObjsTrans = new Transform[6];

    public Transform selectCueSelectedObjTrans;

    public Text gmOverTitleText;

    public GameObject[] gmOverAvatarObjs = new GameObject[2];

    public Text[] gmOverNameTexts = new Text[2];

    public Transform gmOverWinnerParentTrans;

    public GameObject gmOverParticleStarLoopObj;

    public Text gmOverRackText;

    public Text[] gmOverBtnsText = new Text[2];

    public GameObject[] gmOverNextAndAgainBtnImgs = new GameObject[2];
    public int totalTimePlayed;
    public int timeAlreadySaved;
    public static string totalTimeFormated;
    public static int totalBallsPocketed = 0;
    public static int ttBestScore;
    public static int matrixBestScore;
    public int[] breakForceArray = new int[4];

    public float spinRotationY;

    public System.Collections.Hashtable screensNameArray;

    public GameObject[] screensObjArray;

    public string[] screensNameStringsArray = new string[6]
    {
        "MainMenu",
        "InGame", "Pause", "GameOver", "MessageBox", "SelectCue"
    };



    public string prevScreen;
    public static string curScreen = "MainMenu";
    public string screenToGoAfterMenuAnim = "null";

    public static float dtTimeAtLastFrame = 0f;

    public static float dtTimeAtCurrentFrame = 0f;

    public static float deltaTimeCustom = 0f;

    public static bool bAnimateGui = true;

    public static float btnAnimValue = 1f;

    public float btnAnimVel;

    public float btnAnimTarget;

    public float btnAnimCompleteCheckVal;

    public string whatToDoAfterMenuAnim = string.Empty;

    public GameObject gameNameObj;

    public static float gameNamePos = 1f;

    public float gameNameTargetPos;

    public float gameNameVel;

    public bool gameNameHidden;

    public GUIDE_TYPE guideType = GUIDE_TYPE.FULL;

    public const int totalPatternsCount = 11;


    public static bool roomEnabled = true;

    public CONTROLS[] controlMode = new CONTROLS[2]
    {
        CONTROLS.DRAG_CUE,
        CONTROLS.DRAG_CUE
    };

    public HAND_MODE[] handMode = new HAND_MODE[2];

    public float sensitivityValue = 1f;

    public static bool soundEnabled = true;

    public static float musicVolVal = 0.5f;

    public float musicVolMultiplierMenu = 1f;

    public float musicVolMultiplierInGame = 0.5f;

    public static bool redGuideEnabled = true;

    public static bool pinchZoomEnabled = true;

    public static bool tapToAimEnabled = true;

    public static bool autoAimEnabled = true;
    public Transform selControlSelectedTrans;
    public Transform[] selControlBtnsTransArr = new Transform[3];
    public int TOTAL_CUE_COUNT = 6;
    public int[] selectedCue = new int[2] { 5, 4 };
    public int cuePlayerToChoose;
    public bool CHEAT_ENABLED;
    #endregion
    public Action<MODE_TYPE> onBallStopped;
    #region Unity MonoBehaviours
    public bool initialized = false;
    public bool spawned = false;

    private void Start()
    {
        SnookerFlow.Main = this;
        Debug.Log("snoker" + SnokerNetwork.IsMultiplayer);
        if (!SnokerNetwork.IsMultiplayer)
        {
            start();

        }
        lastvalidpos = CUEBALL_START_SNOOKER_POS;
        lastvalidpos.x -= 0.1f;
    }
    public void start()
    {
        //1.Show("start");

        Time.timeScale = 1f;
        base.useGUILayout = false;
        screenMul = (float)Screen.width / 480f;
        canvasSize = GameObject.Find("Canvas").GetComponent<RectTransform>().rect;
        chosenAvatar = new int[2] { 0, 7 };
        playerNames = new string[2]
        {
            playerNames[0],
            avatarNames[chosenAvatar[1]]
        };
        selectedCue = new int[2] { 1, 1 };
        loadSavedData();
        //2.Show("start");
        inputToRotValue = new Vector2(0f, _SnokerGameManager._SnokerCameraManager.camParentRotTargetY);
        inputToRotTarget = new Vector2(0f, _SnokerGameManager._SnokerCameraManager.camParentRotTargetY);
        findUIObjectsAndComponents();
        showPowerMeter(false);
        if (Mathf.Floor(screenWidth / screenHeight * 100f) == 133f)
        {
            powerMetersParentRectTrans.localScale = new Vector3(0.5f, 0.5f, powerMetersParentRectTrans.localScale.z);
            iPadDevice = true;
        }
        //3.Show("start");
        specLookAtPos = _SnokerGameManager._SnokerCameraManager.cameraObjTransform.position;
        if (!Utils.IsHeadless())
            toggleSelectedCue(false);
        showGuideWithType(GUIDE_TYPE.NO);
        ballInHandIndicatorObj.SetActive(false);
        activateBalls(false);
        switchControls();
        for (i = 0; i < guiBallsTex.Length; i++)
        {
            guiBallsTex[i] = Resources.Load<Sprite>("GuiBalls/" + (i + 1));
        }
        //4.Show("start");
        if (startupCounter > 1)
        {
            if (PlayerPrefs.HasKey("useAvatarSet2"))
            {
                avatarSetToUse = 2;
            }
            else
            {
                avatarSetToUse = 1;
            }
        }
        else
        {
            avatarSetToUse = 2;
            PlayerPrefs.SetInt("useAvatarSet2", 1);
        }
        avatarTexArray = new Sprite[11];
        for (i = 0; i < 11; i++)
        {
            if (i < 10)
            {
                avatarTexArray[i] = Resources.Load<Sprite>("Avatars/Set" + avatarSetToUse + "/" + i);
            }
            else
            {
                avatarTexArray[i] = Resources.Load<Sprite>("Avatars/" + i);
            }
        }
        //5.Show("start");
        buttonClickSound = Resources.Load<AudioClip>("Sounds/buttonClick");
        cueballHitSounds[0] = Resources.Load<AudioClip>("Sounds/cueballHit0");
        cueballHitSounds[1] = Resources.Load<AudioClip>("Sounds/cueballHit1");
        ballPocketSounds[0] = Resources.Load<AudioClip>("Sounds/ballPocket0");
        ballPocketSounds[1] = Resources.Load<AudioClip>("Sounds/ballPocket1");
        ballPocketSounds[2] = Resources.Load<AudioClip>("Sounds/ballPocket2");
        ballsHitSound[0] = Resources.Load<AudioClip>("Sounds/ballHit0");
        ballsHitSound[1] = Resources.Load<AudioClip>("Sounds/ballHit1");
        railHitSoundClip = Resources.Load<AudioClip>("Sounds/railHit");
        gameWinSound = Resources.Load<AudioClip>("Sounds/harpFlourish06");
        //6.Show("start");
        setMusicVol(musicVolMultiplierMenu);
        menuSystemInit();
        savePlayersData();
        onClickSelControlBtns(1);
        playerOneTimer.gameObject.SetActive(false);
        playerTwoTimer.gameObject.SetActive(false);
        initialized = true;
        ResultManager.isGameFinished = false;

        //7.Show("start");
    }
    private void FixedUpdate()
    {
        //SnokerNetwork.IsMultiplayer.Show("FixedUpdate CueBall");
        //NetworkServer.active.Show("NetworkServer.active CueBall");
        if (!NetworkServer.active && SnokerNetwork.IsMultiplayer) return;
        if (initialized)
        {
            // Reduced damping for less friction � ball rolls longer
            if (thisRigidbody.linearVelocity.magnitude > 0.22f)
            {
                thisRigidbody.linearVelocity *= 0.993f;
                velocityOnHit *= 0.993f;
            }
            else if (thisRigidbody.linearVelocity.magnitude > 0.11f)
            {
                thisRigidbody.linearVelocity *= 0.99f;
                velocityOnHit *= 0.99f;
            }
            else if (thisRigidbody.linearVelocity.magnitude > 0f)
            {
                thisRigidbody.linearVelocity *= 0.96f;
                velocityOnHit *= 0.96f;
                if (thisRigidbody.linearVelocity.magnitude < 0.0055f)
                {
                    thisRigidbody.linearVelocity = Vector3.zero;
                }
            }

            // Rolling ball mesh rotation scaled down
            if (thisRigidbody.linearVelocity.magnitude > 0)
            {
                cueBallMashParentTrans.Rotate(thisRigidbody.linearVelocity.z * 0.5f, 0f, -thisRigidbody.linearVelocity.x * 0.5f, Space.World);
            }

            // Spin rotation Y axis
            if (Mathf.Abs(spinRotationY) > 0f)
            {
                cueBallMashParentTrans.Rotate(0f, spinRotationY, 0f, Space.World);
                if (spinRotationY > 0f) spinRotationY -= 0.5f;
                else if (spinRotationY < 0f) spinRotationY += 0.5f;
                if (Mathf.Abs(spinRotationY) < 0.01f) spinRotationY = 0f;
            }

            // Gravity compensation for small scale
            //thisRigidbody.AddForce(0f, -9f, 0f, ForceMode.Force);
            if (checkFixedUpdateBallTouch && !firstBallTouched && !_SnokerGameManager.ballIsStanding &&
              Vector3.Distance(thisTransform.position, cueBallPosOnHit) > cueBallToAimColRingDistanceOnHit + 0.01f)
            {
                Vector3.Distance(thisTransform.position, cueBallPosOnHit).Show("Distance Ball");
                (cueBallToAimColRingDistanceOnHit).Show("Distance Target");
                checkFixedUpdateBallTouch = false;
                firstBallTouched = true;

                Vector3 velocity = lastTargetVector * (velocityOnHit * Mathf.Abs(1f - angleOnHit / 90f));
                firstTargetBallToHit.GetComponent<Rigidbody>().linearVelocity = velocity;

                if (velocity.magnitude > 0.95f)
                {
                    spinRotationY = 10f;
                }

                playBallHitSound(Mathf.Clamp(thisRigidbody.linearVelocity.magnitude / 2f, 0.04f, 1f));

                if (angleOnHit > 8f)
                {
                    thisRigidbody.linearVelocity = cueBallReboundVector * (angleOnHit / 90f * velocityOnHit);
                }
                else
                {
                    thisRigidbody.linearVelocity = cueBallReboundVector * (0.11f * velocityOnHit);
                }
                //thisRigidbody.AddForce(0f, -9f, 0f, ForceMode.Force);

                collidedWithSide = true;
                spinApply = true;
                checkFirstLegalBallTouch(int.Parse(firstTargetBallToHit.name));
            }

            if (spinApply)
            {
                thisRigidbody.linearVelocity += cueBallHitVector * spinValues.y;
                spinValues.y *= 0.94f;
            }
        }
    }
    // Cached values to avoid repeated calculations
    private float cachedTime;
    private float cachedDeltaTime;
    private Vector3 cachedMousePosition;
    private Vector3 cachedThisPosition;
    public Vector3 CueLastPosition;
    private bool canUpdateInput;
    bool kaka;
    private void Update()
    {
        if (initialized && !ResultManager.isGameFinished)
        {
            if (transform.position.y < 7.9f && !SnokerGameManager.bBallInHand && !cueBallPotted)
            {
                if ((thisRigidbody.constraints & RigidbodyConstraints.FreezePositionY) != 0)
                    transform.position = new Vector3(transform.position.x, 7.98f, transform.position.z);
            }
            if (_SnokerGameManager.cuesObjArray)
                if (_SnokerGameManager.cuesObjArray.activeSelf == true)
                {

                    if (cueParentObjTransform.position != thisTransform.position)
                        cueParentObjTransform.position = thisTransform.position;
                }
            //UpdateNetworkDate();
            // Cache frequently used values
            cachedTime = Time.time;
            cachedDeltaTime = Time.deltaTime;
            cachedMousePosition = Input.mousePosition;
            cachedThisPosition = thisTransform.position;

            // Early returns and state checks
            updateMenuSystem();
            HandleUITouchEffects();

            if (_SnokerGameManager.bGamePaused || SnokerGameManager.bGameOver) return;

            // Cache game state
            canUpdateInput = !SnokerGameManager.bBallInHand && _SnokerGameManager.ballIsStanding && !aiPlaying && _SnokerGameManager.bTossDone && !spinSetOn && ballsReplaced;

            HandleInGameLogic();
            HandleCameraAndCueUpdates();

            if (cueSetPosTransform && cueSetPosTransform.parent != null && cueGroupTransform != null)
                HandleCueAnimations();
        }
    }


    private void HandleUITouchEffects()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 viewport = _SnokerGameManager._SnokerCameraManager.cameraObjCamera.ScreenToViewportPoint(cachedMousePosition);
            uiTouchParticleRectTrans.anchoredPosition = new Vector2(
                viewport.x * canvasSize.width - iPhoneXSideIndent,
                viewport.y * canvasSize.height
            );
            uiTouchParticleSystem.Play();
        }
    }
    public bool PlayedbyOpponent = false;
    private void HandleInGameLogic()
    {
        cueBallVerticalAnimTarget = SnokerGameManager.bBallInHand ? 1 : 0;
        HandleCueRotation();

        if (SnokerNetwork.IsMultiplayer && !_snokerNetwork.IsMyTurn()) return;
        HandleInputAndSpin();
        if (!SnokerNetwork.IsMultiplayer)
            HandleBallInHand();
        else
            HandleBallInHandNetwork();
    }

    public IEnumerator HandleBallStandingCheck()
    {
        yield return new WaitForSeconds(0.2f);
        while (!_SnokerGameManager.ballIsStanding)
        {
            yield return new WaitForFixedUpdate();

            if (_SnokerGameManager.bGamePaused)
                continue;

            bool allStopped = true;

            // Check all balls instead of first checking only cue
            for (int i = 0; i < _SnokerGameManager.ballsArray.Length; i++)
            {
                if (!_SnokerGameManager.ballsArray[i].activeSelf)
                    continue;

                float speed = _SnokerGameManager.ballsRigidbodyArray[i].linearVelocity.magnitude;

                if (speed > 0.02f) // tolerance threshold
                {
                    allStopped = false;
                    break;
                }
            }
            if (thisRigidbody.linearVelocity.magnitude > 0.02f)
                allStopped = false;
            // also check cue spin
            if (Mathf.Abs(spinRotationY) > 0.01f)
                allStopped = false;

            _SnokerGameManager.ballIsStanding = allStopped;

            if (allStopped && _SnokerGameManager.bTossDone)
            {
                Debug.Log("All balls stopped → resetting cue");
                ResetCuePosition();
                if (SnokerNetwork.IsMultiplayer)
                {
                    onBallStopped?.Invoke(GameMode);
                    onBallStopped = null;
                }
                else
                    OnBallStand(GameMode);
            }
        }
    }

    private void HandleInputAndSpin()
    {
        // Set fine aim multiplier
        fineAimMultiplier = (cachedMousePosition.y < screenHeight * 0.844f) ? 1f : 0.15f;

        //#if UNITY_EDITOR || UNITY_STANDALONE
        // HandleMouseInput();
        //#else
        HandleTouchInput();
        //#endif

        HandleCameraEasing();
        HandleSpinInput();
    }

    private void HandleCameraEasing()
    {
        if (_SnokerGameManager._SnokerCameraManager.camEasingActive)
        {
            inputValues.x *= 0.951f;
            if (Mathf.Abs(inputValues.x) < 0.0001f || spinSetOn || SnokerGameManager.bBallInHand || _SnokerGameManager._SnokerCameraManager.cameraMode == CAMERA_MODE.TOP)
            {
                inputValues.x = 0f;
                _SnokerGameManager._SnokerCameraManager.camEasingActive = false;
            }
        }
    }

    private void HandleSpinInput()
    {
        if (!spinSetOn) return;

        spinSetThumbRectTrans.anchoredPosition += inputValues * 20f;

        // Clamp to circle
        if (Vector2.Distance(Vector2.zero, spinSetThumbRectTrans.anchoredPosition) > 100f)
        {
            spinSetThumbRectTrans.anchoredPosition = Vector2.zero +
                (spinSetThumbRectTrans.anchoredPosition - Vector2.zero).normalized * 100f;
        }

        Vector2 thumbPos = spinSetThumbRectTrans.anchoredPosition;
        spinThumbInsideBtnRectTrans.anchoredPosition = thumbPos / 6f;

        // Cache calculations
        float normalizedX = -thumbPos.x / 800f;
        float normalizedY = -thumbPos.y / 800f;

        spinCuePos.x = -normalizedX * 1.5f;
        spinCuePos.y = -normalizedY * 1.5f;
        spinValues.x = normalizedX;
        spinValues.y = -normalizedY;

        //  _snokerNetwork?.photonView.RPC("SyncSpinValue", RpcTarget.Others, spinCuePos, spinValues);

    }
    public void HandleBallInHand()
    {
        if (SnokerNetwork.IsMultiplayer)
            return;

        if (!SnokerGameManager.bBallInHand)
        {
            cueBallVerticalAnimTarget = 0f;
            ballInHandIndicatorObj.SetActive(false);
            return;
        }
        ballInHandIndicatorObj.SetActive(true);
        cueBallVerticalAnimTarget = 1f;
        if (SnokerNetwork.IsMultiplayer)
        {
            if (!_SnokerGameManager.isyourturn)
                return;

        }
        else
        {
            if (aiPlaying) return;

        }
        // Cache initial position for comparison
        Vector3 initialPosition = cachedThisPosition;

        // Handle ball movement input
        if (_SnokerGameManager._SnokerCameraManager.cameraObjTransform.parent == _SnokerGameManager._SnokerCameraManager.cameraTopParentObjTransform)
        {
            float moveScale = 0.2f;
            Vector3 position = cachedThisPosition;
            position.x += inputValues.x * moveScale;
            position.z += inputValues.y * moveScale;
            thisTransform.position = position;
        }
        else
        {
            thisTransform.position += (inputValues.x * cueParentObjTransform.right +
                inputValues.y * cueParentObjTransform.forward) * 2f * cachedDeltaTime;
        }
        limitBallInHand();
        UpdateBallInHandVisuals();
    }

    public void HandleBallInHandNetwork()
    {
        if (cueParentObjTransform == null) return;
        if (!SnokerGameManager.bBallInHand)
        {
            cueBallVerticalAnimTarget = 0f;
            ballInHandIndicatorObj.SetActive(false);
            return;
        }

        ballInHandIndicatorObj.SetActive(true);
        cueBallVerticalAnimTarget = 1f;
        if (!_snokerNetwork.IsMyTurn()) return;

        Vector3 DesirePos = Vector3.zero;

        if (_SnokerGameManager._SnokerCameraManager.cameraObjTransform.parent == _SnokerGameManager._SnokerCameraManager.cameraTopParentObjTransform)
        {
            float moveScale = 0.2f;
            Vector3 position = cachedThisPosition;
            position.x += inputValues.x * moveScale;
            position.z += inputValues.y * moveScale;
            DesirePos = position;
            if (IsValidBallPosition(DesirePos))
            {
                thisTransform.position = DesirePos;
            }
        }
        else
        {
            DesirePos = thisTransform.position;
            DesirePos += (inputValues.x * cueParentObjTransform.right +
                 inputValues.y * cueParentObjTransform.forward) * 2f * cachedDeltaTime;
            if (IsValidBallPosition(DesirePos))
            {
                thisTransform.position = DesirePos;
            }
        }

        limitBallInHand();
        UpdateBallInHandVisuals();


    }
    private bool IsValidBallPosition(Vector3 position)
    {
        //  float ballRadius = 0.0286f; // Standard pool/snooker ball radius, adjust as needed
        float checkRadius = ballRadius * 2f; // Double radius for ball-to-ball distance
        foreach (GameObject ball in _SnokerGameManager.ballsArray)
        {
            // Skip checking against self
            if (ball == gameObject) continue;

            // Check distance between centers
            float distance = Vector3.Distance(position, ball.transform.position);
            // Debug.Log(ball.name + " = Dis : " + distance);
            // If balls would overlap, position is invalid
            if (distance < checkRadius)
            {
                return false;
            }
        }

        return true;
    }


    // OPTIMIZATION: Separated visual updates for cleaner code
    private void UpdateBallInHandVisuals()
    {
        // Update ball in hand indicator position
        Vector3 indicatorPos = ballInHandIndicatorTrans.position;
        indicatorPos.x = cachedThisPosition.x;
        indicatorPos.z = cachedThisPosition.z;
        indicatorPos.y = 7.96f;
        ballInHandIndicatorTrans.position = indicatorPos;

        // Rotate indicator
        ballInHandIndicatorTrans.Rotate(0f, 50f * cachedDeltaTime, 0f, Space.Self);

        // Scale animations
        ballInHandIndicatorMeshTrans.localScale = new Vector3(cueBallVerticalAnimValue, cueBallVerticalAnimValue, 1f);
        float pingPong = Mathf.PingPong(cachedTime * 2f, 1.8f) + 1f;
        ballInHandIndicatorTrans.localScale = new Vector3(pingPong, 1f, pingPong);
    }

    private void HandleCueRotation()
    {
        if (SnokerGameManager.bBallInHand || !_SnokerGameManager.bTossDone || spinSetOn) return;
        HandleInputToRotation();
        if (!SnokerNetwork.IsMultiplayer)
            UpdateCueRotation();
        else
            UpdateCueRotationNetwork();

    }
    public float minThreshold = 25;
    private void HandleInputToRotation()
    {
        // Validate input values first
        if (float.IsNaN(inputValues.x) || float.IsInfinity(inputValues.x))
        {
            Debug.LogError("inputValues.x is invalid: " + inputValues.x);
            inputValues.x = 0f;
        }
        if (float.IsNaN(inputValues.y) || float.IsInfinity(inputValues.y))
        {
            Debug.LogError("inputValues.y is invalid: " + inputValues.y);
            inputValues.y = 0f;
        }

        if (_SnokerGameManager._SnokerCameraManager.cameraObjCamera.transform.parent != _SnokerGameManager._SnokerCameraManager.cameraTopParentObjTransform)
        {
            inputToRotTarget.x += inputValues.x;
        }
        else
        {
            Vector3 worldToScreen = _SnokerGameManager._SnokerCameraManager.cameraObjCamera.WorldToScreenPoint(cachedThisPosition);

            // Validate WorldToScreenPoint result
            if (float.IsNaN(worldToScreen.x) || float.IsNaN(worldToScreen.y) || float.IsInfinity(worldToScreen.x) || float.IsInfinity(worldToScreen.y))
            {
                Debug.LogError("WorldToScreenPoint returned invalid values: " + worldToScreen);
                return; // Skip this frame if world to screen conversion failed
            }

            float deltaX = cachedMousePosition.x - worldToScreen.x;
            float deltaY = cachedMousePosition.y - worldToScreen.y;

            // Validate delta values
            if (float.IsNaN(deltaX) || float.IsInfinity(deltaX)) deltaX = 0f;
            if (float.IsNaN(deltaY) || float.IsInfinity(deltaY)) deltaY = 0f;

            float xSign = (Mathf.Abs(deltaY) > minThreshold) ? Mathf.Sign(deltaY) : 1f;
            float ySign = (Mathf.Abs(deltaX) > minThreshold) ? Mathf.Sign(deltaX) : 1f;
            float rotationSpeed = 2.0f;

            inputToRotTarget.x += inputValues.x * xSign * rotationSpeed;
            inputToRotTarget.x -= inputValues.y * ySign * rotationSpeed;
        }

        // Validate and clamp inputToRotTarget.x
        if (float.IsNaN(inputToRotTarget.x) || float.IsInfinity(inputToRotTarget.x))
        {
            Debug.LogError("inputToRotTarget.x is invalid: " + inputToRotTarget.x);
            inputToRotTarget.x = 0f;
        }
        inputToRotTarget.x = Mathf.Clamp(inputToRotTarget.x, -3600f, 3600f); // Prevent extreme values

        // Validate SmoothDampAngle inputs
        if (float.IsNaN(inputToRotValue.x) || float.IsInfinity(inputToRotValue.x))
        {
            inputToRotValue.x = 0f;
        }
        if (float.IsNaN(inputToRotVel.x) || float.IsInfinity(inputToRotVel.x))
        {
            inputToRotVel.x = 0f;
        }

        inputToRotValue.x = Mathf.SmoothDampAngle(inputToRotValue.x, inputToRotTarget.x, ref inputToRotVel.x, 0.05f);

        // Validate SmoothDampAngle result
        if (float.IsNaN(inputToRotValue.x) || float.IsInfinity(inputToRotValue.x))
        {
            Debug.LogError("SmoothDampAngle returned invalid value: " + inputToRotValue.x);
            inputToRotValue.x = 0f;
        }

        if (_SnokerGameManager._SnokerCameraManager.cameraMode != CAMERA_MODE.TOP && Mathf.Abs(inputValues.y) > Mathf.Abs(inputValues.x))
        {
            if (inputValues.y < 0f)
            {
                if (_SnokerGameManager._SnokerCameraManager.camDistance > -3.2f)
                {
                    _SnokerGameManager._SnokerCameraManager.camDistance += inputValues.y;
                }
                else
                {
                    inputToRotValue.y -= inputValues.y;
                }
            }
            else if (inputValues.y > 0f)
            {
                _SnokerGameManager._SnokerCameraManager.camDistance += inputValues.y;
                inputToRotValue.y = rotationValueByMoni;

            }
        }
        else if (Input.touchCount == 0)
        {
            inputToRotValue.y = rotationValueByMoni;
        }
        _SnokerGameManager._SnokerCameraManager.camDistance = Mathf.Clamp(_SnokerGameManager._SnokerCameraManager.camDistance, -3.2f, -0.5f);

        // Validate inputToRotValue.y
        if (float.IsNaN(inputToRotValue.y) || float.IsInfinity(inputToRotValue.y))
        {
            Debug.LogError("inputToRotValue.y is invalid: " + inputToRotValue.y);
            inputToRotValue.y = 0f;
        }

        float offset = 0;
        if (firstTargetBallToHit && _SnokerGameManager._SnokerCameraManager.camParentRotValueY > 15.4f)
        {
            Vector3 toTarget = firstTargetBallToHit.transform.position - transform.position;
            toTarget.y = 0;
            Vector3 forward = cueParentObjTransform.forward;
            forward.y = 0;

            // Validate vectors before SignedAngle calculation
            if (toTarget.magnitude < 0.001f || forward.magnitude < 0.001f)
            {
                Debug.LogWarning("Vector magnitude too small for SignedAngle calculation");
            }
            else
            {
                float signedAngle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

                // Validate SignedAngle result
                if (float.IsNaN(signedAngle) || float.IsInfinity(signedAngle))
                {
                    Debug.LogError("SignedAngle returned invalid value: " + signedAngle);
                    signedAngle = 0f;
                }

                int direction = signedAngle < 0f ? 1 : -1;

                // Validate camParentRotValueY
                float camParentRotY = _SnokerGameManager._SnokerCameraManager.camParentRotValueY;
                if (float.IsNaN(camParentRotY) || float.IsInfinity(camParentRotY))
                {
                    Debug.LogError("camParentRotValueY is invalid: " + camParentRotY);
                    camParentRotY = 0f;
                }

                offset = 15f * (15f - camParentRotY) * direction;

                // Validate offset
                if (float.IsNaN(offset) || float.IsInfinity(offset))
                {
                    Debug.LogError("Calculated offset is invalid: " + offset);
                    offset = 0f;
                }

                // Clamp offset to reasonable range
                offset = Mathf.Clamp(offset, -360f, 360f);
            }
        }

        // Final validation before Quaternion.Euler
        float finalYRotation = _SnokerGameManager._SnokerCameraManager.camParentRotValueY;
        float finalXRotation = inputToRotValue.x + offset;
        float finalZRotation = 0f;

        // Validate all final rotation values
        if (float.IsNaN(finalYRotation) || float.IsInfinity(finalYRotation))
        {
            Debug.LogError("Final Y rotation is invalid: " + finalYRotation);
            finalYRotation = 0f;
        }
        if (float.IsNaN(finalXRotation) || float.IsInfinity(finalXRotation))
        {
            Debug.LogError("Final X rotation is invalid: " + finalXRotation + " (inputToRotValue.x: " + inputToRotValue.x + ", offset: " + offset + ")");
            finalXRotation = 0f;
        }

        // Normalize angles to prevent extreme values
        finalYRotation = Mathf.Repeat(finalYRotation + 180f, 360f) - 180f;
        finalXRotation = Mathf.Repeat(finalXRotation + 180f, 360f) - 180f;

        // Safely create quaternion
        try
        {
            _SnokerGameManager._SnokerCameraManager.camParentRotation = Quaternion.Euler(finalYRotation, finalXRotation, finalZRotation);
            _SnokerGameManager._SnokerCameraManager.camParentObjInGameTransform.rotation = _SnokerGameManager._SnokerCameraManager.camParentRotation;
            _SnokerGameManager._SnokerCameraManager.cameraFreeViewParentObjTransform.rotation = _SnokerGameManager._SnokerCameraManager.camParentRotation;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to create Quaternion with values: Y=" + finalYRotation + ", X=" + finalXRotation + ", Z=" + finalZRotation + ". Error: " + e.Message);
        }
    }
    public float rotationValueByMoni = 10;


    public void UpdateCueRotationNetwork()
    {
        if (!StickManager.instance) return;
        if (cueParentObjTransform)
        {
            if (_SnokerGameManager.ballIsStanding && Physics.SphereCast(cueParentObjTransform.position, 0.03245f,
                -cueParentObjTransform.forward, out lineHit, 100f, ballLineLayerMask))
            {
                float angleclamp = Vector3.Angle(
                    -cueParentObjTransform.forward,
                    lineHit.point + new Vector3(0f, 0.03245f, 0f) - cueParentObjTransform.position);
                cueRotTargetY = Mathf.Clamp(angleclamp, 5f, 14f);
            }

            cueRotValueY = Mathf.SmoothDampAngle(cueRotValueY, cueRotTargetY, ref cueRotVelY, 0.07f);
            Vector3 localEulerAngles = cueObjectTransform.localEulerAngles;
            localEulerAngles.x = cueRotValueY;
            cueObjectTransform.localEulerAngles = localEulerAngles;
        }

        if ((!aiPlaying && !SnokerNetwork.IsMultiplayer) || (SnokerNetwork.IsMultiplayer && _snokerNetwork.IsMyTurn()))
        {
            cueRotValueX = inputToRotValue.x;
            StickManager.instance.transform.eulerAngles = new Vector3(0f, cueRotValueX, 0f);
        }

    }

    private void UpdateCueRotation()
    {
        // Y Rotation (vertical angle) calculation
        if (_SnokerGameManager.ballIsStanding && Physics.SphereCast(cueParentObjTransform.position, 0.03245f,
            -cueParentObjTransform.forward, out lineHit, 100f, ballLineLayerMask))
        {
            float angleclamp = Vector3.Angle(
                -cueParentObjTransform.forward,
                lineHit.point + new Vector3(0f, 0.03245f, 0f) - cueParentObjTransform.position);
            cueRotTargetY = Mathf.Clamp(angleclamp, 5f, 16f);
        }

        cueRotValueY = Mathf.SmoothDampAngle(cueRotValueY, cueRotTargetY, ref cueRotVelY, 0.07f);


        Vector3 localEulerAngles = cueObjectTransform.localEulerAngles;
        localEulerAngles.x = cueRotValueY;
        cueObjectTransform.localEulerAngles = localEulerAngles;


        if ((!aiPlaying && !SnokerNetwork.IsMultiplayer) || (SnokerNetwork.IsMultiplayer && _SnokerGameManager.isyourturn))
        {
            cueRotValueX = inputToRotValue.x;
            cueParentObjTransform.eulerAngles = new Vector3(0f, cueRotValueX, 0f);
        }
    }



    public float offsetMultipler = 20;
    public float limit = 20;
    private void HandleCameraAndCueUpdates()
    {
        // Update camera rotation
        inputToRotValue.y = Mathf.Clamp(inputToRotValue.y, 14f, 16.5f);
        _SnokerGameManager._SnokerCameraManager.camParentRotValueY = Mathf.SmoothDampAngle(_SnokerGameManager._SnokerCameraManager.camParentRotValueY, inputToRotValue.y, ref _SnokerGameManager._SnokerCameraManager.camParentRotVelY, 0.2f);
        // Update guide if ball is standing
        if (_SnokerGameManager.ballIsStanding && !SnokerGameManager.bBallInHand && _SnokerGameManager.bTossDone)
        {
            if (SnokerNetwork.IsMultiplayer)
            {
                if (guideType != GUIDE_TYPE.FULL && _snokerNetwork.IsMyTurn())
                    showGuideWithType(GUIDE_TYPE.FULL);

                else if (guideType != GUIDE_TYPE.MED && !_snokerNetwork.IsMyTurn())
                    showGuideWithType(GUIDE_TYPE.MED);
            }
            else
            {
                if (guideType != GUIDE_TYPE.FULL && SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
                    showGuideWithType(GUIDE_TYPE.FULL);

                else if (guideType != GUIDE_TYPE.MED && aiPlaying)
                    showGuideWithType(GUIDE_TYPE.MED);
            }
            if (NetworkServer.active)
                updateGuide();
            else
                updateguideai();
        }
        else
        {
            if (guideType != GUIDE_TYPE.NO)
                showGuideWithType(GUIDE_TYPE.NO);
        }

        cueBallParentTrans.localPosition = Vector3.SmoothDamp(cueBallParentTrans.localPosition,
            Vector3.zero, ref cueBallMeshAnimVel, 0.3f);
        cueBallVerticalAnimValue = Mathf.SmoothDamp(cueBallVerticalAnimValue, 0.09f,
            ref cueBallVerticalAnimVel, 0.5f);
        cueBallMashParentTrans.SetLocalPositionY(cueBallVerticalAnimTarget);
    }
    private void HandleCueAnimations()
    {
        if (cueSetPosTransform.parent == cueGroupTransform)
        {
            cueSetPosTransform.localPosition = Vector3.SmoothDamp(cueSetPosTransform.localPosition,
                Vector3.zero, ref cueSetPosAnimVel, 0.3f);

            SmoothDampEulerAngles(cueSetPosTransform, Vector3.zero, ref cueSetPosRotAnimVel, 0.15f);
        }
        else if (cueSetPosTransform.parent == cueSetPosHoldingRotatorTrans)
        {
            Vector3 targetPos = new Vector3(0f, 0f, 6f);
            cueSetPosTransform.localPosition = Vector3.SmoothDamp(cueSetPosTransform.localPosition,
                targetPos, ref cueSetPosAnimVel, 0.4f);

            float distance = Vector3.Distance(cueSetPosTransform.localPosition, targetPos);

            if (distance < 10f)
            {
                SmoothDampEulerAngles(cueSetPosTransform, Vector3.zero, ref cueSetPosRotAnimVel, 0.3f);
            }

            if (distance < 1f && _SnokerGameManager.cuesObjArray.activeSelf)
            {
                _SnokerGameManager.cuesObjArray.SetActive(false);
                cueShadowMesh.SetActive(false);
            }
        }

        // Update cue shadow
        UpdateCueShadow();
    }

    private void UpdateCueShadow()
    {
        if (SnokerNetwork.IsMultiplayer)
            if (cueShadowTransform == null)
            {
                cueShadowTransform = StickManager.instance.cueShadowTransform;
            }

        if (cueShadowTransform != null)
        {
            Vector3 shadowPos = cueShadowTransform.localPosition;
            shadowPos.z = -cueDistance * 3.5f - Vector3.Distance(cueGroupTransform.position, cueSetPosTransform.position);
            cueShadowTransform.localPosition = shadowPos;

            if (Physics.Raycast(cueParentObjTransform.position, -cueParentObjTransform.forward,
                out lineHit, 100f, tableSideLayerMask))
            {
                Vector3 shadowScale = cueShadowTransform.localScale;
                shadowScale.z = Mathf.Clamp(lineHit.distance + 1f, 1f, 21.8f);
                cueShadowTransform.localScale = shadowScale;
            }

            if (spinSetOn)
            {
                shadowPos.x = spinCuePos.x;
                cueShadowTransform.localPosition = shadowPos;
                cueGroupTransform.localPosition = new Vector3(spinCuePos.x, spinCuePos.y, -(0.6f + cueDistance * 3.5f));

            }
        }
    }

    private void SmoothDampEulerAngles(Transform target, Vector3 targetAngles, ref Vector3 velocity, float smoothTime)
    {
        Vector3 currentAngles = target.localEulerAngles;
        currentAngles.x = Mathf.SmoothDampAngle(currentAngles.x, targetAngles.x, ref velocity.x, smoothTime);
        currentAngles.y = Mathf.SmoothDampAngle(currentAngles.y, targetAngles.y, ref velocity.y, smoothTime);
        currentAngles.z = Mathf.SmoothDampAngle(currentAngles.z, targetAngles.z, ref velocity.z, smoothTime);
        target.localEulerAngles = currentAngles;
    }

    public void CueBallPotted()
    {
        foulInThisTurn = true;
        cueBallPotted = true;
        thisRigidbody.linearVelocity = Vector3.zero;
        thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
        spinRotationY = 0f;
        Debug.Log("Cue Ball Potted");
        SnookerFlow.Log("foul — cue ball potted (scratch)");

        _SnokerUIManager.showNotification("Scratch!\nCue Ball Pocketed");
    }

    public void OnTriggerEnter(Collider collision)
    {
        if (SnokerGameManager.bGameOver || cueBallPotted || SnokerGameManager.bBallInHand)
        {
            return;
        }

        if (collision.GetComponent<Collider>().name == "holesTrigger")
        {
            Debug.Log("OnTrigger : " + SnokerGameManager.bGameOver + cueBallPotted + SnokerGameManager.bBallInHand + _SnokerGameManager.isyourturn);

            if (GameMode == MODE_TYPE.AI)
                CueBallPotted();
            else if (GameMode == MODE_TYPE.Multiplayer)
            {
                thisRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionY;
                _snokerNetwork.RunRPCAfterInternet(() =>
                {
                    StickManager.instance.CueBallPotted();
                });
            }
        }
        if (collision.GetComponent<Collider>().name == "holesSoundTrigger")
        {
            playSoundFX(ballPocketSounds[UnityEngine.Random.Range(0, ballPocketSounds.Length)], 1f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (SnokerGameManager.bGameOver)
        {
            return;
        }
        if (SnokerNetwork.IsMultiplayer && !NetworkServer.active)
        {
            return;
        }

        //1.Show("OnCollisionEnter");
        if ((collision.collider.CompareTag("colSideTag") || collision.collider.CompareTag("colSideEndTag")) && thisTransform.position.y > CUEBALL_START_SNOOKER_POS.y - 0.05f)
        {
            if (!collidedWithSide)
            {
                thisTransform.position = aimColRingPosOnHit;
                thisRigidbody.linearVelocity = cueBallReboundVector * thisRigidbody.linearVelocity.magnitude;
                collidedWithSide = true;
            }
            cueRailHit = true;
            spinApply = false;
            spinRotationY = 0f;
            if (Vector3.Angle(thisRigidbody.linearVelocity, collision.contacts[0].normal) < 80f)
            {
                playRailHitSound(thisTransform.position, Mathf.Clamp(thisRigidbody.linearVelocity.magnitude / 45f, 0.06f, 1f));
            }
        }
        //2.Show("OnCollisionEnter");
        if (collision.collider.CompareTag("ballTag"))
        {
            //3.Show("OnCollisionEnter");
            if (thisRigidbody.linearVelocity.magnitude > 15f || collision.contacts[0].otherCollider.GetComponent<Rigidbody>().linearVelocity.magnitude > 15f)
            {
                spinRotationY = 10;
            }
            else if (spinRotationY > 0f)
            {
                spinRotationY = 0f;
            }
            //firstBallTouched.Show("firstBallTouched");
            //_SnokerGameManager.bBallInHand.Show("bBallInHand");
            //checkFixedUpdateBallTouch.Show("checkFixedUpdateBallTouch");
            //collidedWithSide.Show("collidedWithSide");
            //_SnokerGameManager.ballIsStanding.Show("_SnokerGameManager.ballIsStanding");
            if (!firstBallTouched && !SnokerGameManager.bBallInHand && !checkFixedUpdateBallTouch && collidedWithSide && !_SnokerGameManager.ballIsStanding)
            {
                //5.Show("OnCollisionEnter");
                int intName = int.Parse(collision.collider.name);
                checkFirstLegalBallTouch(intName);
                firstBallTouched = true;
            }
            if (firstBallTouched && !SnokerGameManager.bBallInHand && thisRigidbody.linearVelocity.magnitude > 0.05f)
            {
                playBallHitSound(Mathf.Clamp(thisRigidbody.linearVelocity.magnitude / 30f, 0.04f, 1f));
            }
        }

    }
    #endregion

    #region Input
    void HandleTouchInput()
    {
        // Handle Touch Input
        if (Input.touchCount == 1)
        {
            Touch touchDataMove = Input.GetTouch(0);
            HandleInputPhase(touchDataMove.phase, touchDataMove.deltaPosition, touchDataMove.deltaTime);
        }
        // Handle Mouse Input (when no touch input)
        else if (Input.touchCount == 0)
        {
            HandleMouseInput();
        }
    }

    // Store previous mouse position for delta calculation
    private Vector2 previousMousePosition;
    private bool mouseWasPressed = false;

    void HandleMouseInput()
    {
        Vector2 currentMousePosition = Input.mousePosition;

        // Mouse Button Down (equivalent to TouchPhase.Began)
        if (Input.GetMouseButtonDown(0))
        {
            previousMousePosition = currentMousePosition;
            mouseWasPressed = true;
            HandleInputPhase(TouchPhase.Began, Vector2.zero, 0f);
        }
        // Mouse Button Held and Moving (equivalent to TouchPhase.Moved)
        else if (Input.GetMouseButton(0) && mouseWasPressed)
        {
            Vector2 deltaPosition = currentMousePosition - previousMousePosition;
            float deltaTime = Time.deltaTime;

            if (deltaPosition.magnitude > 0.1f) // Only process if there's meaningful movement
            {
                HandleInputPhase(TouchPhase.Moved, deltaPosition, deltaTime);
            }
            else
            {
                HandleInputPhase(TouchPhase.Stationary, Vector2.zero, deltaTime);
            }

            previousMousePosition = currentMousePosition;
        }
        // Mouse Button Up (equivalent to TouchPhase.Ended)
        else if (Input.GetMouseButtonUp(0) && mouseWasPressed)
        {
            Vector2 deltaPosition = currentMousePosition - previousMousePosition;
            HandleInputPhase(TouchPhase.Ended, deltaPosition, Time.deltaTime);
            mouseWasPressed = false;
        }
        // Mouse not pressed
        else if (!Input.GetMouseButton(0))
        {
            mouseWasPressed = false;
            inputValues = Vector2.zero;
        }
    }

    void HandleInputPhase(TouchPhase phase, Vector2 deltaPosition, float deltaTime)
    {
        switch (phase)
        {
            case TouchPhase.Began:
                inputValues = Vector2.zero;
                _SnokerGameManager._SnokerCameraManager.camEasingActive = false;
                if (SnokerGameManager.bBallInHand || !_SnokerGameManager.ballIsStanding)
                {
                    _SnokerGameManager._SnokerCameraManager.camCanRotate = true;
                }
                else if (controlMode[0] == CONTROLS.DRAG_CUE)
                {
                    _SnokerGameManager._SnokerCameraManager.camCanRotate = !controlDragDone ? false : true;
                }
                break;

            case TouchPhase.Moved:
                if (_SnokerGameManager._SnokerCameraManager.camCanRotate)
                {
                    // Normalize speed and clamp to a more reasonable range
                    touchSpeed = Mathf.Clamp(deltaPosition.magnitude / Mathf.Max(deltaTime, 0.01f), 10f, 300f) / 100f;
                    if (!float.IsNaN(touchSpeed))
                    {
                        float baseMultiplier = sensitivityValue * fineAimMultiplier;
                        // Scale movement more softly (screenMul should already be scaled based on resolution)
                        inputValues.x = Mathf.Clamp(
                            deltaPosition.x / screenMul * baseMultiplier * touchSpeed,
                            -10f, 10f
                        );
                        inputValues.y = Mathf.Clamp(
                            deltaPosition.y / screenMul * baseMultiplier * touchSpeed,
                            -2.5f, 2.5f
                        );
                    }
                }
                break;

            case TouchPhase.Stationary:
            case TouchPhase.Canceled:
                inputValues = Vector2.zero;
                break;

            case TouchPhase.Ended:
                if (Mathf.Abs(inputValues.x) > 0.5f)
                {
                    _SnokerGameManager._SnokerCameraManager.camEasingActive = true;
                }
                else
                {
                    inputValues.x = 0f;
                }
                inputValues.y = 0f;
                _SnokerGameManager._SnokerCameraManager.camCanRotate = true;
                break;
        }
    }
    #endregion

    #region UISystem
    public void backButtonFunctions()
    {

        switch (curScreen)
        {
            case "MainMenu":
                askCloseGame();
                break;
            case "Pause":
                switchScreen("InGame");
                doThisAfterAnim("pauseUnpauseGame");
                break;
            case "GameOver":

            case "MessageBox":
                messageBoxOnCancel();
                break;


        }
    }

    private void findUIObjectsAndComponents()
    {

        _SnokerUIManager.notifScriptComp.gameObject.SetActive(false);
        okBtnObj.SetActive(false);
        placeCueBtnObj.SetActive(false);
        spinBtnObj.SetActive(false);
        bottomBlinkingTextObj.SetActive(false);
        spinControlParentObj.SetActive(false);
        guiBallDisplayObj.SetActive(false);
        guiScoreDisplayText.gameObject.SetActive(false);
        //  guiRightSideText.gameObject.SetActive(false);
    }
    public void changeUI(Image image, float fillAmount)
    {
        if (image != null)
        {
            image.fillAmount = fillAmount;
        }
    }
    public void quitToMainMenu()
    {
        _SnokerGameManager.bGamePaused = false;
        SnokerGameManager.bGameOver = false;
        Time.timeScale = 1f;
        playerNames[1] = PlayerPrefs.GetString("playerNames1", playerNames[1]);
        activateBalls(false);
        cueBallParentObj.SetActive(false);
        //toggleSelectedCue(false);
        ballInHandIndicatorObj.SetActive(false);
        showGuideWithType(GUIDE_TYPE.NO);
        hideBottomBlinkingText();
        CancelInvoke("guiBallDisplay8BallInvoke");
        CancelInvoke("guiBallDisplayUk8BallInvoke");
        CancelInvoke("doToss");
        setMusicVol(musicVolMultiplierMenu);
        gmOverParticleStarLoopObj.SetActive(false);
        //switchScreen("MainMenu");
        gameNameIn();
        GameModeManager.isAI = false;
        if (SnokerNetwork.IsMultiplayer)
        {
            //_snokerNetwork._runner.Shutdown(false);
            //Photon Removal PhotonNetwork.LeaveRoom();
            Debug.LogError("photon.leave room");
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            if (NetworkGameManager.Instance)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
            this.Delay(1, () => NetworkManager.singleton.StopClient());
        }
        else
        {

        }
        SceneManager.LoadScene("Home");
    }
    public void showBottomBlinkingText(string val)
    {
        bottomBlinkingTextObj.SetActive(true);
        bottomBlinkingTextText.text = val;
    }

    public void hideBottomBlinkingText()
    {
        bottomBlinkingTextObj.SetActive(false);
    }






    private void menuSystemInit()
    {
        gameNameObj = GameObject.Find("Canvas/AllParent/GameName");
        curScreen = screensNameStringsArray[0];
        prevScreen = screensNameStringsArray[0];
        screensNameArray = new System.Collections.Hashtable();
        screensObjArray = new GameObject[screensNameStringsArray.Length];
        for (i = 0; i < screensObjArray.Length; i++)
        {
            screensNameArray.Add(screensNameStringsArray[i], i);
            screensObjArray[i] = GameObject.Find("Canvas/AllParent/" + screensNameStringsArray[i]);
            if (i > 0)
            {
                // if (screensObjArray[i])
                //  screensObjArray[i].SetActive(false);
            }
        }
        onClickStartGameBtn();

    }

    private void updateMenuSystem()
    {
        dtTimeAtCurrentFrame = Time.realtimeSinceStartup;
        deltaTimeCustom = dtTimeAtCurrentFrame - dtTimeAtLastFrame;
        dtTimeAtLastFrame = dtTimeAtCurrentFrame;
        if (!(Time.timeSinceLevelLoad > 0.5f))
        {
            return;
        }
        if (curScreen != "InGame")
        {
            gameNamePos = Mathf.SmoothDamp(gameNamePos, gameNameTargetPos, ref gameNameVel, 0.15f, float.PositiveInfinity, deltaTimeCustom);
        }
        if (gameNameTargetPos == 1f && gameNameTargetPos - gameNamePos < 0.05f)
        {
            if (!gameNameHidden)
            {
                gameNameHidden = true;
                gameNameObj.SetActive(false);
            }
        }
        else if (gameNameHidden)
        {
            gameNameHidden = false;
            gameNameObj.SetActive(true);
        }
        btnAnimCompleteCheckVal = ((btnAnimTarget != 1f) ? 0.003f : 0.05f);
        if (Mathf.Abs(btnAnimValue - btnAnimTarget) >= btnAnimCompleteCheckVal)
        {
            bAnimateGui = true;
            btnAnimValue = Mathf.SmoothDamp(btnAnimValue, btnAnimTarget, ref btnAnimVel, 0.15f, float.PositiveInfinity, deltaTimeCustom);
        }
        else if (Mathf.Abs(btnAnimValue - btnAnimTarget) > 0f)
        {
            bAnimateGui = false;
            btnAnimValue = btnAnimTarget;
            if (whatToDoAfterMenuAnim != string.Empty)
            {
                Invoke(whatToDoAfterMenuAnim, 0f);
            }
            if (screenToGoAfterMenuAnim != "null")
            {
                if (screensObjArray[(int)screensNameArray[curScreen]])
                    screensObjArray[(int)screensNameArray[curScreen]].SetActive(false);
                btnAnimTarget = 0f;
                curScreen = screenToGoAfterMenuAnim;
                if (screensObjArray[(int)screensNameArray[curScreen]])
                    screensObjArray[(int)screensNameArray[curScreen]].SetActive(true);
            }
            whatToDoAfterMenuAnim = string.Empty;
            screenToGoAfterMenuAnim = "null";
        }
    }

    public void switchScreen(string targetScreen)
    {
        if (btnAnimValue == 1f)
        {
            btnAnimValue = 0f;
        }
        btnAnimTarget = 1f;
        prevScreen = curScreen;
        screenToGoAfterMenuAnim = targetScreen;
    }

    private void doThisAfterAnim(string action)
    {
        if (btnAnimValue == 1f)
        {
            btnAnimValue = 0f;
        }
        btnAnimTarget = 1f;
        whatToDoAfterMenuAnim = action;
    }

    private void switchToPrevScreen()
    {
        if (btnAnimValue == 1f)
        {
            btnAnimValue = 0f;
        }
        btnAnimTarget = 1f;
        screenToGoAfterMenuAnim = prevScreen;
    }

    public void gameNameIn()
    {
        gameNameTargetPos = 0f;
    }

    public void gameNameOut()
    {
        gameNameTargetPos = 1f;
    }
    public void playSoundFX(AudioClip sClip, float vol)
    {
        if (soundEnabled)
        {
            thisAudioComponent.volume = vol;
            thisAudioComponent.PlayOneShot(sClip);
        }
    }

    public void playButtonClickSound()
    {
        playSoundFX(buttonClickSound, 1f);
    }

    public void playBallHitSound(float vol)
    {
        playSoundFX(ballsHitSound[UnityEngine.Random.Range(0, ballsHitSound.Length)], vol);
    }

    public void playRailHitSound(Vector3 pos, float vol)
    {
        if (soundEnabled && !railHitAudioComponent.isPlaying)
        {
            railHitAudioTransform.position = pos;
            railHitAudioComponent.volume = vol;
            railHitAudioComponent.PlayOneShot(railHitSoundClip);
        }
    }

    private void playGameWinSoundInvoke()
    {
        playSoundFX(gameWinSound, 1f);
    }



    private int getRandomOneOrMinusOne()
    {
        return UnityEngine.Random.Range(1, 3) * 2 - 3;
    }




    public void askRestartGame()
    {
        messageBoxCancelYes("ARE YOU SURE YOU WANT TO RESTART THE GAME? GAME PROGRESS WILL BE LOST.", string.Empty, "callbackRestart", new string[2] { "CANCEL", "YES" });
    }

    public void askQuitGame()
    {
        messageBoxCancelYes("ARE YOU SURE YOU WANT TO QUIT THE GAME?", string.Empty, "callbackQuit", new string[2] { "CANCEL", "YES" });
    }



    private void askCloseGame()
    {
        messageBoxCancelYes("ARE YOU SURE YOU WANT TO CLOSE THE GAME?", string.Empty, "callbackCloseGame", new string[2] { "CANCEL", "YES" });
    }

    private void messageBoxOk(string msg, string callback1)
    {
        messageBoxScript.msgText = msg;
        messageBoxScript.msgConfirmCallback1 = callback1;
        messageBoxScript.messageType = MESSAGE_TYPE.OK;
        switchScreen("MessageBox");
    }

    private void messageBoxCancelYes(string msg, string callback1, string callback2, string[] btnsText)
    {
        messageBoxScript.msgText = msg;
        messageBoxScript.msgConfirmCallback1 = callback1;
        messageBoxScript.msgConfirmCallback2 = callback2;
        messageBoxScript.msgBtnsText[0] = btnsText[0];
        messageBoxScript.msgBtnsText[1] = btnsText[1];
        messageBoxScript.messageType = MESSAGE_TYPE.CANCEL_YES;
        switchScreen("MessageBox");
    }

    private void messageBoxOnCancel()
    {
        if (messageBoxScript.msgConfirmCallback1 != string.Empty)
        {
            Invoke(messageBoxScript.msgConfirmCallback1, 0f);
        }
        switchToPrevScreen();
    }

    public void messageBoxOnYes()
    {
        if (messageBoxScript.msgConfirmCallback2 != string.Empty)
        {
            Invoke(messageBoxScript.msgConfirmCallback2, 0f);
        }
        switchToPrevScreen();
    }


    public void setModeType(int val)
    {
        GameMode = (MODE_TYPE)val;

    }


    public void onClickPlaceCueOkBtn()
    {
        if (!SnokerGameManager.bGameOver)
        {
            SnokerGameManager.bBallInHand = false;
            transform.hasChanged = false;
            if (SnokerNetwork.IsMultiplayer)
            {
                if (NetworkServer.active)
                {
                    StickManager.instance.SetBallInHand(false);
                }
                StickManager.instance.CmdSetBallInHand(false);
                SnokerNetwork.Instance._mainScript.GetComponent<NetworkRigidbodyUnreliable>().syncDirection = SyncDirection.ServerToClient;
                SnokerNetwork.Instance._mainScript.GetComponent<NetworkTransformUnreliable>().syncDirection = SyncDirection.ServerToClient;

            }

            canRePlaceCueBall = true;
            thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
            if (strikeCount == 0 && thisTransform.position.x != 0.12f)
            {
                resetCueAndCamDirection();
            }

            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager._SnokerCameraManager.cameraMode);

            toggleSelectedCue(true);
            showGuideWithType(GUIDE_TYPE.FULL);
            ballInHandIndicatorObj.SetActive(false);
            if (SnokerNetwork.IsMultiplayer)
            {
                if (NetworkServer.active)
                    setAllBallKinematic(false, 99);
            }
            else
            {
                setAllBallKinematic(false, 99);
            }
            placeCueBtnObj.SetActive(true);
            spinBtnObj.SetActive(true);

            okBtnObj.SetActive(false);
            hideBottomBlinkingText();
            this.Delay(2, () =>
            {
                showPowerMeter(true);

            });
            if (_snokerNetwork && _snokerNetwork.IsMyTurn() && !NetworkServer.active)
            {
                _snokerNetwork.CueBallStartingpos = transform.position;
                StickManager.instance.CmdOnClickCuePlaced();
            }
            //_snokerNetwork.RunRPCAfterInternet(() =>
            //{
            //});
        }
    }

    public void onClickSpinBtn()
    {
        spinSetOn = !spinSetOn;
        if (spinSetOn)
        {
            spinControlGroupAnimScript.showSpinControl();
            placeCueBtnObj.SetActive(false);
            showPowerMeter(false);
            return;
        }
        spinControlGroupAnimScript.hideSpinControl(true);
        if (canRePlaceCueBall)
        {
            placeCueBtnObj.SetActive(true);
        }
        showPowerMeter(true);
    }

    public void resetSpinControl()
    {
        spinSetThumbRectTrans.anchoredPosition = Vector2.zero;
        spinThumbInsideBtnRectTrans.anchoredPosition = Vector2.zero;
        spinValues = Vector2.zero;
        spinApply = false;
        spinCuePos = Vector2.zero;
        cueShadowTransform.SetLocalPositionX(0f);
    }

    public void matrixBallNumBtnPointerDown()
    {
        bShowMatrixBallUiNums = true;
        matrixBallNumsParent.SetActive(true);
    }

    public void matrixBallNumBtnPointerUp()
    {
        bShowMatrixBallUiNums = false;
        matrixBallNumsParent.SetActive(false);
    }




    public void onSoundEnabledToggle(bool val)
    {
        soundEnabled = val;
    }

    public void onMusicSliderValueChange(float val)
    {
        musicVolVal = val;
        if (_SnokerGameManager.bGamePaused)
        {
            setMusicVol(musicVolMultiplierInGame);
        }
        else
        {
            setMusicVol(musicVolMultiplierMenu);
        }
    }

    public void onMusicSliderInGameValueChange(float val)
    {
        musicVolMultiplierInGame = val;
        if (_SnokerGameManager.bGamePaused)
        {
            setMusicVol(musicVolMultiplierInGame);
        }
    }

    private void setMusicVol(float val)
    {
        //musicAudioSource.volume = musicVolVal * val;
    }



    public void onRedGuideEnabledToggle(bool val)
    {
        redGuideEnabled = val;
    }

    public void onPinchZoomEnabledToggle(bool val)
    {
        pinchZoomEnabled = val;
    }



    public void onTapToAimToggle(bool val)
    {
        tapToAimEnabled = val;
    }

    public void onAutoAimToggle(bool val)
    {
        autoAimEnabled = val;
    }
    public void onClickSelControlContinue()
    {
        PlayerPrefs.SetInt("controlMode0", (int)controlMode[0]);
        PlayerPrefs.SetInt("controlMode1", (int)controlMode[1]);
        if (!userSelControlDone)
        {
            PlayerPrefs.SetInt("userSelControlDone", 1);
            userSelControlDone = true;
        }
    }

    public void onClickSelControlBtns(int val)
    {
        controlMode[0] = (CONTROLS)val;
        controlMode[1] = (CONTROLS)val;
        //selControlSelectedTrans.SetParent(selControlBtnsTransArr[val], false);
        onClickSelControlContinue();
    }
    public void onAvatarSelected(int val)
    {
        if (val == 10 || val != chosenAvatar[(avatarPlayerToChoose == 0) ? 1u : 0u])
        {

            if (avatarPlayerToChoose == 1 && (playerNames[avatarPlayerToChoose] == avatarNames[chosenAvatar[avatarPlayerToChoose]] || playerNames[avatarPlayerToChoose] == string.Empty))
            {
                playerNames[avatarPlayerToChoose] = avatarNames[val];
            }
            chosenAvatar[avatarPlayerToChoose] = val;
            if (avatarPlayerToChoose == 0)
            {
            }
            playButtonClickSound();
        }
        else
        {
            messageBoxOk("THIS AVATAR IS ALREADY CHOSEN BY YOUR OPPONENT. PLEASE CHOOSE ANOTHER ONE.", string.Empty);
        }
    }
    public void onAiLevelSelected(int val)
    {
        aiDifficulty = (AI_DIFFICULTY)val;
    }
    public void goToSelectCue(int val)
    {
        cuePlayerToChoose = val;
        selectCueTitleText.text = "Select Cue [ " + playerNames[cuePlayerToChoose] + " ]";
        switchScreen("SelectCue");
        selectCueSelectedObjTrans.SetParent(selectCueBtnObjsTrans[selectedCue[cuePlayerToChoose]], false);
    }

    public void onCueSelected(int val)
    {
        selectedCue[cuePlayerToChoose] = val;
        saveSelectedCue();
        playButtonClickSound();
        if (_SnokerGameManager.bGamePaused)
        {
            switchScreen("Pause");
        }

    }



    public void toggleSelectedCue(bool val)
    {
        Debug.Log("Toggle Cue: " + val);
        if (val)
        {
            ResetCuePosition();
            _SnokerGameManager.cuesObjArray.SetActive(true);
            cueShadowMesh.SetActive(true);
            cueSetPosHoldingParentTrans.position = thisTransform.position + -cueParentObjTransform.forward * 100f;
            if (Physics.Raycast(cueSetPosHoldingParentTrans.position, cueParentObjTransform.forward, out lineHit, 100f, boundingBoxLayerMask))
            {
                cueSetPosHoldingParentTrans.position = lineHit.point;
                cueSetPosHoldingParentTrans.position += cueParentObjTransform.right * 1f;
            }
            cueSetPosHoldingParentTrans.forward = thisTransform.position - cueSetPosHoldingParentTrans.position;
            Vector3 localEulerAngles = cueObjectTransform.localEulerAngles;
            localEulerAngles.x = 0f;
            cueObjectTransform.localEulerAngles = localEulerAngles;
            cueSetPosTransform.parent = cueGroupTransform;
            ResetCuePosition();
        }
        else
        {
            cueSetPosHoldingParentTrans.position = thisTransform.position + -cueParentObjTransform.forward * 100f;
            if (Physics.Raycast(cueSetPosHoldingParentTrans.position, cueParentObjTransform.forward, out lineHit, 100f, boundingBoxLayerMask))
            {
                cueSetPosHoldingParentTrans.position = lineHit.point;
                cueSetPosHoldingParentTrans.position += cueParentObjTransform.right * 4f;
            }
            else if (Vector3.Distance(cueSetPosHoldingParentTrans.position, new Vector3(0f, CUEBALL_START_SNOOKER_POS.y, 0f)) > 32f)
            {
                cueSetPosHoldingParentTrans.position = new Vector3(0f, CUEBALL_START_SNOOKER_POS.y, 0f) + (cueSetPosHoldingParentTrans.position - new Vector3(0f, CUEBALL_START_SNOOKER_POS.y, 0f)).normalized * 32f;
            }
            cueSetPosHoldingParentTrans.forward = thisTransform.position - cueSetPosHoldingParentTrans.position;
            cueSetPosTransform.parent = cueSetPosHoldingRotatorTrans;
            _SnokerGameManager.cuesObjArray.SetActive(false);
            cueShadowMesh.SetActive(false);
        }
    }
    #endregion

    #region GuideLine
    public Transform closestPot = null;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.0167f; // 20 updates per second instead of 60
    public GuideLineData dataguide;
    // Call this from Update or wherever you currently call updateGuide()
    void updateguideai()
    {
        dataguide = CalculateGuideLineData();

    }
    [Server]
    private void updateGuide()
    {    // Throttle updates
        dataguide = CalculateGuideLineData();
        // StickManager.instance.RpcUpdateGuideLine(dataguide);
    }
    [Serializable]
    public struct GuideLineData
    {
        // Transform data
        public Vector3 mainLinePosition;
        public Quaternion mainLineRotation;
        public float mainLineScaleZ;

        public Vector3 colRingPosition;
        public Vector3 dirCueBallPosition;
        public Quaternion dirCueBallRotation;
        public Vector3 dirTargetPosition;
        public Quaternion dirTargetRotation;

        // Visibility flags
        public bool hitBall;

        // Object references as identifiers
        public int closestPotIndex; // -1 if none
        public int targetBallIndex; // 0 if none (Mirror uses uint for netId)
    }
    private GuideLineData CalculateGuideLineData()
    {
        if (cueParentObjTransform == null)
        {
            Debug.Log("Cue Parent Transform is null");
            return new GuideLineData();
        }
        GuideLineData data = new GuideLineData();

        float radius = ballRadius;
        Vector3 origin = thisTransform.position;
        Vector3 direction = cueParentObjTransform.forward;

        // Perform SphereCast
        Debug.DrawRay(origin, direction, Color.red, 1);
        if (!Physics.SphereCast(origin, radius, direction, out lineHit, 100f, ballLineLayerMask))
        {
            //data.showTargetMesh = false;
            //data.showCueBallRenderer = false;
            data.closestPotIndex = -1;
            data.targetBallIndex = -1;
            return data;
        }

        // Reflect direction
        guideReflectDirVec = Vector3.Reflect(direction, lineHit.normal).normalized;

        // Calculate positions
        guideColRingPosVec = lineHit.point + lineHit.normal * radius;
        data.colRingPosition = new Vector3(guideColRingPosVec.x, guideColRingTransform.position.y, guideColRingPosVec.z);

        data.mainLinePosition = new Vector3(thisTransform.position.x, guideMainLineTransform.position.y, thisTransform.position.z);
        data.mainLineRotation = Quaternion.Euler(0f, cueParentObjTransform.rotation.eulerAngles.y, 0f);
        data.mainLineScaleZ = lineHit.distance - 0.02f;

        // Calculate directions based on hit type
        if (lineHit.collider.CompareTag("ballTag"))
        {
            data.hitBall = true;
            data.dirCueBallPosition = guideColRingPosVec;

            Vector3 forward = -lineHit.normal;
            guideTempAngle = Vector3.Angle(direction, forward);
            float crossY = Vector3.Cross(forward, thisTransform.position - (lineHit.point - lineHit.normal * radius)).y;

            Vector3 eulerAngles = Quaternion.LookRotation(forward).eulerAngles;
            if (crossY > 0f)
                eulerAngles.y -= (guideTempAngle <= 5f) ? 90f * (guideTempAngle / 5f) : 90f;
            else if (crossY < 0f)
                eulerAngles.y += (guideTempAngle <= 5f) ? 90f * (guideTempAngle / 5f) : 90f;

            data.dirCueBallRotation = Quaternion.Euler(eulerAngles);
            //Debug.Log("Ball Hitting : " + lineHit.collider.gameObject.name);
            data.targetBallIndex = FindBallIndexInArray(lineHit.collider.gameObject);
        }
        else
        {
            data.hitBall = false;
            data.dirCueBallPosition = guideColRingPosVec;
            data.dirCueBallRotation = Quaternion.LookRotation(guideReflectDirVec);
            data.targetBallIndex = -1;
        }

        data.dirTargetPosition = lineHit.point - lineHit.normal * radius;
        Vector3 targetForward = -lineHit.normal;

        // Find closest pot - store INDEX
        float closestAngle = 15;
        data.closestPotIndex = -1;
        for (int i = 0; i < _SnokerGameManager.holesTriggerPos.Length; i++)
        {
            Transform pot = _SnokerGameManager.holesTriggerPos[i];
            Vector3 dirToPot = (pot.position - data.dirTargetPosition).normalized;
            float angle = Vector3.Angle(targetForward, dirToPot);

            if (angle < closestAngle)
            {
                closestAngle = angle;
                data.closestPotIndex = i;
            }
        }

        // Apply spin
        float targetYRotation = Quaternion.LookRotation(targetForward).eulerAngles.y;
        if (spinValues.x != 0f)
            targetYRotation += spinValues.x * 6f;

        data.dirTargetRotation = Quaternion.Euler(0f, targetYRotation, 0f);

        // Determine visibility
        if (guideType == GUIDE_TYPE.NO)
        {
            ///  data.showTargetMesh = false;
            //  data.showCueBallRenderer = false;
            return data;
        }
        ApplyGuideLineVisuals(data);
        return data;
    }


    // Apply the visual changes on client
    public void ApplyGuideLineVisuals(GuideLineData data)
    {
        // Apply transforms
        guideMainLineTransform.position = data.mainLinePosition;
        data.mainLineRotation = Quaternion.Euler(0f, cueParentObjTransform.rotation.eulerAngles.y, 0f);
        guideMainLineTransform.rotation = data.mainLineRotation;

        Vector3 localScale = guideMainLineTransform.localScale;
        localScale.z = data.mainLineScaleZ;
        guideMainLineTransform.localScale = localScale;

        guideColRingTransform.position = data.colRingPosition;
        guideDirCueBallTrans.position = data.dirCueBallPosition;
        guideDirCueBallTrans.rotation = data.dirCueBallRotation;
        guideDirTargetTrans.position = data.dirTargetPosition;
        guideDirTargetTrans.rotation = data.dirTargetRotation;

        // Look up closest pot from index
        if (data.closestPotIndex >= 0 && data.closestPotIndex < _SnokerGameManager.holesTriggerPos.Length)
        {
            closestPot = _SnokerGameManager.holesTriggerPos[data.closestPotIndex];
        }
        else
        {
            closestPot = null;
        }

        // Look up first target ball from NetworkIdentity
        if (data.targetBallIndex >= 0)
        {
            // Find ball by number
            //firstTargetBallToHit = GameObject.Find(data.targetBallNumber.ToString());

            // OR if you have a reference to all balls:
            firstTargetBallToHit = _SnokerGameManager.ballsArray[data.targetBallIndex];
            bool showdir = (guideType == GUIDE_TYPE.FULL || guideType == GUIDE_TYPE.LONG) &&
                                   _SnokerGameManager.isyourturn || (!SnokerNetwork.IsMultiplayer && SnokerGameManager.currentTurn != "ai") &&
                                   _SnokerGameManager.bTossDone;
            guideDirTargetMesh.SetActive(showdir);
            guideDirCueBallRenderer.enabled = showdir;
        }
        else
        {
            guideDirTargetMesh.SetActive(false);
            // guideDirCueBallRenderer.enabled = lineHit.collider.CompareTag("colSideTag");
            firstTargetBallToHit = null;
        }

        // Update lastTargetVector (used by other code)
        lastTargetVector = data.dirTargetRotation * Vector3.forward;

        // Apply visibility
        if (data.targetBallIndex >= 0)
        {
            int hitBallNum = int.Parse(_SnokerGameManager.ballsArray[data.targetBallIndex].name);
            if (!_SnokerGameManager.IsValidTarget(hitBallNum))
                setGuideColorRed();
            else
                setGuideColorWhite();
        }
        else
        {
            setGuideColorWhite();
        }
    }
    private int FindBallIndexInArray(GameObject ball)
    {
        for (int i = 0; i < _SnokerGameManager.ballsArray.Length; i++)
        {
            if (_SnokerGameManager.ballsArray[i] == ball)
            {
                return i;
            }
        }
        return -1; // Not found
    }
    public void setGuideColorRed()
    {
        guideMainLineRenderer.sharedMaterial.color = guideRedColor;
        guideDirTargetRenderer.sharedMaterial.color = guideRedColor;
        guideColRingRenderer.sharedMaterial.color = guideRedColor;
    }

    public void setGuideColorWhite()
    {
        guideMainLineRenderer.sharedMaterial.color = guideWhiteColor;
        guideDirTargetRenderer.sharedMaterial.color = guideWhiteColor;
        guideColRingRenderer.sharedMaterial.color = guideWhiteColor;
    }
    #endregion

    public void showGuideWithType(GUIDE_TYPE val)
    {
        guideType = val;
        switch (val)
        {
            case GUIDE_TYPE.NO:
                guideMainLineRenderer.enabled = false;
                guideColRingRenderer.enabled = false;
                guideDirCueBallRenderer.enabled = false;
                guideDirTargetMesh.SetActive(false);
                break;
            case GUIDE_TYPE.MED:
                guideMainLineRenderer.enabled = true;
                guideColRingRenderer.enabled = true;
                guideDirCueBallRenderer.enabled = false;
                guideDirTargetMesh.SetActive(false);
                break;
            case GUIDE_TYPE.FULL:
                guideMainLineRenderer.enabled = true;
                guideColRingRenderer.enabled = true;
                guideDirCueBallRenderer.enabled = true;
                guideDirTargetMesh.SetActive(true);
                guideDirCueBallScalerTrans.localScale = new Vector3(guideDirCueBallScalerTrans.localScale.x, guideDirCueBallScalerTrans.localScale.y, 0.10f);
                guideDirTargetScalerTrans.localScale = new Vector3(guideDirTargetScalerTrans.localScale.x, guideDirTargetScalerTrans.localScale.y, 0.25f);
                break;
            case GUIDE_TYPE.LONG:
                guideMainLineRenderer.enabled = true;
                guideColRingRenderer.enabled = true;
                guideDirCueBallRenderer.enabled = true;
                guideDirTargetMesh.SetActive(true);
                guideDirCueBallScalerTrans.localScale = new Vector3(guideDirCueBallScalerTrans.localScale.x, guideDirCueBallScalerTrans.localScale.y, 4f);
                guideDirTargetScalerTrans.localScale = new Vector3(guideDirTargetScalerTrans.localScale.x, guideDirTargetScalerTrans.localScale.y, 10f);
                break;
        }
    }

    #region CameraAndCue

    public void onClickCameraBtn()
    {
        if (aiPlaying) return;
        if (_SnokerGameManager._SnokerCameraManager.cameraObjTransform.parent == _SnokerGameManager._SnokerCameraManager.cameraAiParentObjTransform)
        {
            _SnokerGameManager._SnokerCameraManager.cameraMode = CAMERA_MODE.NORMAL;
            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.NORMAL);
        }
        else if (_SnokerGameManager._SnokerCameraManager.cameraMode == CAMERA_MODE.NORMAL)
        {
            _SnokerGameManager._SnokerCameraManager.cameraMode = CAMERA_MODE.TOP;
            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager._SnokerCameraManager.cameraMode);
        }
        else if (_SnokerGameManager._SnokerCameraManager.cameraMode == CAMERA_MODE.POCKET)
        {
            _SnokerGameManager._SnokerCameraManager.cameraMode = CAMERA_MODE.NORMAL;
            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager._SnokerCameraManager.cameraMode);
        }
        else if (_SnokerGameManager._SnokerCameraManager.cameraMode == CAMERA_MODE.TOP)
        {
            _SnokerGameManager._SnokerCameraManager.cameraMode = CAMERA_MODE.NORMAL;
            if (aiPlaying)
            {
                _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.AI);
            }
            else
            {
                _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager._SnokerCameraManager.cameraMode);
            }
        }
    }

    private void resetCueAndCamDirection(int lookTarget = -1)
    {
        _SnokerGameManager._SnokerCameraManager.cameraObjTransform.parent = null;
        inputValues = Vector2.zero;
        if (lookTarget == -1)
        {

            if (thisTransform.position.x > 0f)
            {
                cueParentObjTransform.LookAt(_SnokerGameManager.ballsArray[13].transform);
            }
            else
            {
                cueParentObjTransform.LookAt(_SnokerGameManager.ballsArray[11].transform);
            }


        }
        else
        {
            cueParentObjTransform.LookAt(_SnokerGameManager.ballsArray[lookTarget].transform);
        }
        inputToRotTarget.x = cueParentObjTransform.eulerAngles.y;
        inputToRotValue.x = inputToRotTarget.x;
        cueObjectTransform.localEulerAngles = new Vector3(cueRotValueY, cueObjectTransform.localEulerAngles.y, cueObjectTransform.localEulerAngles.z);
        cueGroupTransform.localPosition = new Vector3(spinCuePos.x, spinCuePos.y, 0f - (0.5f + cueDistance * 3.5f));
        cueRotValueX = inputToRotValue.x;
        cueParentObjTransform.eulerAngles = new Vector3(0f, cueRotValueX, 0f);
        _SnokerGameManager._SnokerCameraManager.camParentRotation = Quaternion.Euler(_SnokerGameManager._SnokerCameraManager.camParentRotValueY, inputToRotValue.x, 0f);
        _SnokerGameManager._SnokerCameraManager.camParentObjInGameTransform.rotation = _SnokerGameManager._SnokerCameraManager.camParentRotation;
        if (GameMode == MODE_TYPE.Multiplayer)
        {
            if (_SnokerGameManager.isyourturn)
                _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager._SnokerCameraManager.cameraMode);
            else
                _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.AI);
        }
        else
        {
            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager._SnokerCameraManager.cameraMode);

        }
    }
    #endregion

    #region BallMovement
    public void hitTheBall(float shotPower, Vector3 direction, Vector3 cueBallReboundVector, bool isAI = false)
    {
        if (SnokerGameManager.bGameOver)
        {
            return;
        }
        if (closestPot)
        {
            if (isAI == false)
            {
                _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.POCKET);
                StartCoroutine(PauseAwareTaskToDo(() =>
                {
                    if (!_SnokerGameManager.ballIsStanding)
                        _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.TOP);
                }, 3f));
            }

        }
        else
        {
            if (isAI == false)
            {
                StartCoroutine(PauseAwareTaskToDo(() =>
                {
                    if (!_SnokerGameManager.ballIsStanding)
                        _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.TOP);
                }, 1f));
            }
        }
        Debug.Log("Try to Shoot");
        StartCoroutine(HandleBallStandingCheck());

        thisRigidbody.isKinematic = false;
        thisRigidbody.linearVelocity = Vector3.zero;
        strikeCount++;
        SnookerFlow.Shot(strikeCount, shotPower);

        canRePlaceCueBall = false;
        placeCueBtnObj.SetActive(false);
        spinBtnObj.SetActive(false);
        guiBallDisplayObj.SetActive(false);

        cueBallPosOnHit = thisTransform.position;
        aimColRingPosOnHit = guideColRingPosVec;
        this.cueBallReboundVector = cueBallReboundVector;
        thisRigidbody.linearVelocity = direction * 10 * shotPower;
        checkFixedUpdateBallTouch = true;
        velocityOnHit = thisRigidbody.linearVelocity.magnitude;
        if (Physics.SphereCast(thisTransform.position, ballRadius, direction, out lineHit, 100f, ballsLayerMask))
        {
            checkFixedUpdateBallTouch = true;
            velocityOnHit = thisRigidbody.linearVelocity.magnitude;
            firstTargetBallToHit = lineHit.collider.gameObject;
            angleOnHit = Vector3.Angle(direction, -lineHit.normal);
            cueBallToAimColRingDistanceOnHit = Vector3.Distance(cueBallPosOnHit, aimColRingPosOnHit);
            cueBallHitVector = direction;

        }
        else
        {
            checkFixedUpdateBallTouch = false;
        }
        playSoundFX(cueballHitSounds[UnityEngine.Random.Range(0, cueballHitSounds.Length)], 1f);
        toggleSelectedCue(false);
        showGuideWithType(GUIDE_TYPE.NO);
        _SnokerGameManager.ballIsStanding = false;
        ballPottedInThisTurn = false;
        foulInThisTurn = false;
        cueBallPotted = false;
        firstBallTouched = false;
        collidedWithSide = false;
        cueRailHit = false;
        spinApply = false;
        snookerFirstTouchedBallNum = 0;
        snookerBallInvolvedInFoul = 0;
        snookerPointsCurShot = 0;
        if (_SnokerGameManager.GetCurrentTargetAsInt() != 1 && checkFixedUpdateBallTouch)
        {
            snookerNominatedBall = int.Parse(firstTargetBallToHit.name) - 14;
        }
        railHitCountInThisShot = 0;
        railHitBallArray = new int[21];
        if (controlMode[0] == CONTROLS.SET_POWER && !aiPlaying)
        {
            controlSetPowerLastPower = shotPower;
        }
        showPowerMeter(false);
        spinSetOn = false;
        spinControlGroupAnimScript.hideSpinControl(false);
        hideBottomBlinkingText();
        if (shotPower == 0 && SnokerGameManager.bBallInHand) cueBallPotted = true;
        okBtnObj.SetActive(false);

        if (aiPlaying)
        {
            return;
        }

    }
    private IEnumerator PauseAwareTaskToDo(System.Action action, float delay)
    {
        float elapsedTime = 0f;

        while (elapsedTime < delay)
        {
            if (!_SnokerGameManager.bGamePaused)
            {
                elapsedTime += Time.deltaTime;
            }
            yield return null;
        }

        if (!_SnokerGameManager.bGamePaused)
        {
            action?.Invoke();
        }
    }
    public void doAutoTarget()
    {
        int lookTarget = 0;
        int bestTarget = -1;
        float bestScore = float.MaxValue;
        if (_SnokerGameManager.GetCurrentTargetAsInt() == 1)
        {
            // Looking for red balls - evaluate all options
            for (int k = 0; k < _SnokerGameManager.snookerRedsSelected; k++)
            {
                if (_SnokerGameManager.ballsArray[k].activeSelf)
                {
                    float score = EvaluateShotDifficulty(k);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = k;
                    }
                }
            }

            if (bestTarget != -1)
            {
                lookTarget = bestTarget;
            }
        }
        else if (_SnokerGameManager.GetCurrentTargetAsInt() == 99)
        {
            // Looking for colored balls in sequence
            for (int num3 = 20; num3 > 14; num3--)
            {
                if (_SnokerGameManager.ballsArray[num3].activeSelf)
                {
                    float score = EvaluateShotDifficulty(num3);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = num3;
                    }
                }
            }

            if (bestTarget != -1)
            {
                lookTarget = bestTarget;
            }
        }
        else
        {
            // Specific color ball - must hit this one
            lookTarget = _SnokerGameManager.GetCurrentTargetAsInt() + 14 - 1;
        }
        resetCueAndCamDirection(lookTarget);
    }

    private float EvaluateShotDifficulty(int ballIndex)
    {
        Vector3 cueBallPos = thisTransform.position;
        Vector3 targetBallPos = _SnokerGameManager.ballsArray[ballIndex].transform.position;
        Vector3 directionToBall = targetBallPos - cueBallPos;
        float distanceToBall = directionToBall.magnitude;

        float difficultyScore = 0f;

        // 1. Check if path to ball is clear
        RaycastHit[] hits = Physics.SphereCastAll(
            cueBallPos,
            ballRadius * 0.9f,
            directionToBall.normalized,
            distanceToBall - ballRadius * 2f,
            ballLineLayerMask
        );

        // Count obstructions
        int obstructionCount = 0;
        float minClearance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject != _SnokerGameManager.ballsArray[ballIndex])
            {
                obstructionCount++;

                // Calculate how close the obstruction is to our path
                Vector3 closestPoint = GetClosestPointOnLine(cueBallPos, targetBallPos, hit.point);
                float clearance = Vector3.Distance(closestPoint, hit.point);
                minClearance = Mathf.Min(minClearance, clearance);
            }
        }

        // Heavy penalty for obstructed shots
        if (obstructionCount > 0)
        {
            difficultyScore += 1000f * obstructionCount;
            // Additional penalty for tight clearances
            if (minClearance < ballRadius * 2.5f)
            {
                difficultyScore += 500f;
            }
        }

        // 2. Check if target ball is in a cluster
        float clusterPenalty = CalculateClusterPenalty(ballIndex);
        difficultyScore += clusterPenalty;

        // 3. Check for easy pot opportunities
        float bestPotScore = EvaluatePotOpportunities(ballIndex);

        // 4. Distance factor (slight preference for closer balls)
        difficultyScore += distanceToBall * 0.1f;

        // 5. If ball has good pot opportunity, significantly reduce difficulty
        if (bestPotScore < 100f)
        {
            difficultyScore *= 0.1f; // Make pottable balls very attractive
        }

        // 6. Check table position (prefer balls away from rails)
        float railProximity = GetRailProximity(targetBallPos);
        difficultyScore += railProximity * 50f;

        return difficultyScore;
    }

    private float CalculateClusterPenalty(int ballIndex)
    {
        Vector3 ballPos = _SnokerGameManager.ballsArray[ballIndex].transform.position;
        float penalty = 0f;
        int nearbyBalls = 0;

        // Check how many balls are near this one
        for (int i = 0; i < _SnokerGameManager.ballsArray.Length; i++)
        {
            if (i != ballIndex && _SnokerGameManager.ballsArray[i].activeSelf)
            {
                float distance = Vector3.Distance(ballPos, _SnokerGameManager.ballsArray[i].transform.position);

                if (distance < ballRadius * 3f)
                {
                    nearbyBalls++;
                    // Closer balls add more penalty
                    penalty += (ballRadius * 3f - distance) * 100f;
                }
            }
        }

        // Extra penalty if surrounded by multiple balls
        if (nearbyBalls >= 2)
        {
            penalty += 200f * nearbyBalls;
        }

        return penalty;
    }

    private float EvaluatePotOpportunities(int ballIndex)
    {
        Vector3 cueBallPos = thisTransform.position;
        Vector3 targetBallPos = _SnokerGameManager.ballsArray[ballIndex].transform.position;
        float bestScore = float.MaxValue;

        // Quick evaluation of pot opportunities
        for (int p = 0; p < _SnokerGameManager.holesTriggerPos.Length; p++)
        {
            Vector3 pocketPos = _SnokerGameManager.holesTriggerPos[p].position;
            Vector3 ballToPocket = pocketPos - targetBallPos;
            float distanceToPocket = ballToPocket.magnitude;

            // Skip if too far
            if (distanceToPocket > 100f) continue; // Adjust this threshold as needed

            ballToPocket.Normalize();

            // Check basic angle
            Vector3 cueToTarget = (targetBallPos - cueBallPos).normalized;
            float angle = Vector3.Angle(cueToTarget, ballToPocket);

            // Good angle for potting?
            if (angle < 45f)
            {
                // Quick check if path to pocket seems clear
                bool pathClear = !Physics.SphereCast(
                    targetBallPos,
                    ballRadius * 0.9f,
                    ballToPocket,
                    out RaycastHit hit,
                    distanceToPocket - ballRadius,
                    ballLineLayerMask
                );

                if (pathClear)
                {
                    float score = angle + distanceToPocket * 0.1f;
                    bestScore = Mathf.Min(bestScore, score);
                }
            }
        }

        return bestScore;
    }

    private float GetRailProximity(Vector3 ballPos)
    {
        // Define table boundaries (adjust these based on your table size)
        float tableWidth = 3.569f; // Standard snooker table width in meters
        float tableHeight = 1.778f; // Standard snooker table height in meters

        float minDistToRail = float.MaxValue;

        // Check distance to each rail
        minDistToRail = Mathf.Min(minDistToRail, Mathf.Abs(ballPos.x + tableWidth / 2));
        minDistToRail = Mathf.Min(minDistToRail, Mathf.Abs(ballPos.x - tableWidth / 2));
        minDistToRail = Mathf.Min(minDistToRail, Mathf.Abs(ballPos.z + tableHeight / 2));
        minDistToRail = Mathf.Min(minDistToRail, Mathf.Abs(ballPos.z - tableHeight / 2));

        // Return normalized proximity (0 = on rail, 1 = center of table)
        return 1f - Mathf.Clamp01(minDistToRail / (ballRadius * 5f));
    }

    private Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 line = lineEnd - lineStart;
        float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, line) / Vector3.Dot(line, line));
        return lineStart + t * line;
    }
    public void setAllBallKinematic(bool val, int exception)
    {
        for (i = 0; i < 21; i++)
        {
            if (exception != i && _SnokerGameManager.ballsArray[i].activeSelf)
            {
                _SnokerGameManager.ballsRigidbodyArray[i].isKinematic = val;
            }
        }
    }
    private void checkFirstLegalBallTouch(int intName)
    {
        0.Show("check first legal ball");
        if (SnokerNetwork.IsMultiplayer && !NetworkServer.active)
        {
            return;
        }
        int num = 0;
        if (intName < 16)
        {
            num = 1;
            snookerFirstTouchedBallNum = 1;
        }
        else
        {
            snookerFirstTouchedBallNum = intName - 14;
            num = ((_SnokerGameManager.GetCurrentTargetAsInt() == 99) ? 99 : ((SnokerGameManager.snookerRedPottedCount != _SnokerGameManager.snookerRedsSelected) ? 99 : (intName - 14)));
            if (snookerNominatedBall == 1)
            {
                snookerNominatedBall = snookerFirstTouchedBallNum;
            }
        }
        //if (SnokerNetwork.IsMultiplayer && !_SnokerGameManager.isyourturn) return;
        1.Show("check first legal ball");
        if (num != _SnokerGameManager.GetCurrentTargetAsInt())
        {
            2.Show("check first legal ball");
            foulInThisTurn = true;
            SnookerFlow.Log($"foul — cue ball hit the {SnookerFlow.Ball(snookerFirstTouchedBallNum)} first, target was {SnookerFlow.Target}");

            if (SnokerNetwork.IsMultiplayer)
                StickManager.instance?.RpcShowNotification("Foul!\nIllegal Ball Hit!", foulInThisTurn);
            else
            {
                _SnokerUIManager.showNotification("Foul!\nIllegal Ball Hit!");
            }
        }

    }
    private void activateBalls(bool val)
    {
        //if (SnokerNetwork.IsMultiplayer) return;
        if (val)
        {

            for (i = 0; i < 21; i++)
            {
                if ((i < _SnokerGameManager.snookerRedsSelected || i > 14) && !_SnokerGameManager.ballsArray[i].activeSelf)
                {
                    _SnokerGameManager.ballsArray[i].SetActive(true);
                }
                _SnokerGameManager.ballsArray[i].transform.position = _SnokerGameManager.ballPositions[i];
                if (!_SnokerGameManager.ballsRigidbodyArray[i].isKinematic)
                    _SnokerGameManager.ballsRigidbodyArray[i].linearVelocity = Vector3.zero;
                _SnokerGameManager.ballsRigidbodyArray[i].constraints |= RigidbodyConstraints.FreezePositionY;
            }

        }
        else
        {
            for (i = 0; i < 21; i++)
            {
                _SnokerGameManager.ballsArray[i].SetActive(false);
            }
        }
    }
    public List<int> pottedBallsInThisTurn = new List<int>();
    public List<int> pottedBallsNumbers = new List<int>();
    public void holesTriggerOnEnter(int ballName, int snookerTargetBall)
    {
        if (SnokerGameManager.bGameOver)
        {
            return;
        }
        pottedBallsNumbers.Add(ballName - 1);
        _SnokerGameManager.ballsArray[ballName - 1].SetActive(false);
        _SnokerGameManager.ballsRigidbodyArray[ballName - 1].constraints |= RigidbodyConstraints.FreezePositionY;
        ballsPottedCount++;
        int ballPoint = ballName > 15 ? ballName - 14 : 1;
        pottedBallsInThisTurn.Add(ballPoint);
        if (!aiPlaying)
        {
            totalBallsPocketed++;
        }
        int num = 0;
        int num2 = 0;
        if (ballName < 16)
        {
            num = 1;
            num2 = 1;
            SnokerGameManager.snookerRedPottedCount++;
        }
        else
        {
            num = ballName - 14;
            if (snookerTargetBall == 99)
            {
                num2 = 99;
                if (snookerPointsCurShot != 0)
                {
                    num2 = num;
                }
                if (num != snookerNominatedBall)
                {
                    num2 = num;
                }
            }
            else if (SnokerGameManager.snookerRedPottedCount == _SnokerGameManager.snookerRedsSelected)
            {
                num2 = num;
            }
        }
        if (num2 == snookerTargetBall)
        {
            ballPottedInThisTurn = true;
            snookerPointsCurShot += num;
            SnookerFlow.Log($"{SnookerFlow.Who(SnokerGameManager.currentTurn)} potted the {SnookerFlow.Ball(num)} (+{num})");
        }
        else
        {
            foulInThisTurn = true;
            snookerPointsCurShot += num;
            SnookerFlow.Log($"foul — {SnookerFlow.Ball(num)} potted, target was {SnookerFlow.Ball(snookerTargetBall)}");
            if (SnokerNetwork.IsMultiplayer)
                _snokerNetwork.RunRPCAfterInternet(() =>
                {
                    StickManager.instance?.showNotificationRpc("Foul!\nIllegal Ball Pocketed!", foulInThisTurn);
                });
            _SnokerUIManager.showNotification("Foul!\nIllegal Ball Pocketed!");
        }

        if (aiPlaying && ballPottedInThisTurn && strikeCount > 1)
        {
            _SnokerGameManager._SnokerAIManager.aiBallsPottedInThisTurn++;
        }

    }
    public void holesSoundTriggerOnEnter()
    {
        playSoundFX(ballPocketSounds[UnityEngine.Random.Range(0, ballPocketSounds.Length)], 1f);
    }
    [Button]
    public void goToBallInHand()
    {
        placeCueBtnObj.SetActive(false);
        spinBtnObj.SetActive(false);
        SnokerGameManager.bBallInHand = true;

        if (SnokerNetwork.IsMultiplayer)
        {
            if (NetworkServer.active)
            {
                StickManager.instance.SetBallInHand(true);
            }
            StickManager.instance?.CmdSetBallInHand(true);
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkRigidbodyUnreliable>().syncDirection = SyncDirection.ClientToServer;
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkTransformUnreliable>().syncDirection = SyncDirection.ClientToServer;
        }
        ballInHandIndicatorTrans.position = new Vector3(thisTransform.position.x, ballInHandIndicatorTrans.position.y, thisTransform.position.z);
        ballInHandIndicatorObj.SetActive(true);
        okBtnObj.SetActive(true);
        showBottomBlinkingText("Place the Cue Ball");
        toggleSelectedCue(false);
        showGuideWithType(GUIDE_TYPE.NO);
        showPowerMeter(false);
        setAllBallKinematic(true, 99);
        cueRotValueX = inputToRotValue.x;
        cueParentObjTransform.eulerAngles = new Vector3(0f, 90, 0f);
        _SnokerGameManager._SnokerCameraManager.camParentObjInGameTransform.eulerAngles = new Vector3(15, 90, 0);
    }
    #endregion

    #region SnokkerGamePlayLogics

    public void resetCueBallToStart()
    {
        thisTransform.position = CUEBALL_START_SNOOKER_POS/* + new Vector3(3.7f, 0f, 0f)*/;
        thisRigidbody.linearVelocity = Vector3.zero;
        thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
        cueParentObjTransform.position = thisTransform.position;
    }

    #region Main Turn Logic

    #endregion
    public void OnBallStand(MODE_TYPE gameMode)
    {
        if (_snokerNetwork && _snokerNetwork.CameraEffect != null)
            StopCoroutine(_snokerNetwork.CameraEffect);
        if (SnokerGameManager.bGameOver) return;
        ResetTurnState();
        HandleCueBallIfPotted();
        HandleBallRespotting();
        if (strikeCount > 0 || SnokerNetwork.IsMultiplayer)
        {
            ProcessTurnResults();   // may find a foul (e.g. no ball touched), so log after it
            if (!ballPottedInThisTurn && !foulInThisTurn)
                SnookerFlow.Log($"{SnookerFlow.Who(SnokerGameManager.currentTurn)}: no ball potted, no foul");
            if (HandleGameEndConditions(gameMode))
                return;

            _SnokerGameManager.UpdateTargetBall();
            SnookerFlow.Log($"next target: {SnookerFlow.Target}");

        }
        if (ShouldChangeTurn())
            SnookerFlow.Log($"{SnookerFlow.Who(SnokerGameManager.currentTurn)}'s turn ends" + (foulInThisTurn ? " (foul)" : ""));
        else
            SnookerFlow.Log($"turn continues — {SnookerFlow.Who(SnokerGameManager.currentTurn)} plays again");
        if (ShouldChangeTurn())
        {
            if (gameMode == MODE_TYPE.AI)
            {
                ChangeTurn();
                AfterChangingTurn(gameMode);
                StartTurnTimerAI();
            }
            else
            {

                StartCoroutine(_snokerNetwork.ChangeTurn());
            }
        }
        else
        {
            if (gameMode == MODE_TYPE.AI)
            {
                resetTimer = true;
            }
            else
            {
                StickManager.instance.RpcChangeturnrpc(SnokerGameManager.currentTurn, cueBallPotted, firstBallTouched, false, (int)SnokerGameManager.snookerTargetBall);
            }
            AfterChangingTurn(gameMode);

        }

    }

    /// <summary>
    /// Unified method for post-turn handling
    /// </summary>
    public void AfterChangingTurn(MODE_TYPE gameMode = MODE_TYPE.AI)
    {
        ResetCuePosition();
        HandleFoulConsequences(gameMode);
        SetupPlayerControls(gameMode);
        HandlePlayerTurns(gameMode);
        HandleAutoAim();
        ResetTurnState();
        //cueBallPotted = false;
        pottedBallsInThisTurn.Clear();
        pottedBallsNumbers.Clear();
        if (SnokerNetwork.IsMultiplayer)
        {
            //  _SnokerGameManager.ballSyncManager.Start();
            if (NetworkServer.active)
                _snokerNetwork.StartTurnTimer();

        }
        else
        {
            StartTurnTimerAI();
        }
    }

    #endregion

    #region Turn State Management

    public void ResetTurnState()
    {
        alertOptionalTextPrefix = string.Empty;
        resetSpinControl();
    }

    private void HandleCueBallIfPotted()
    {
        if (cueBallPotted)
        {
            resetCueBallToStart();
        }
    }

    private bool ShouldChangeTurn()
    {
        return !ballPottedInThisTurn || foulInThisTurn;
    }

    #endregion

    #region Foul and Scoring Logic

    private void ProcessTurnResults()
    {
        CheckFoulConditions();
        //foulInThisTurn.Show("Foul in this turn");
        if (foulInThisTurn)
        {
            HandleFoulPenalties();
        }
        else
        {
            AwardPoints();
        }
    }

    private void CheckFoulConditions()
    {
        if (!firstBallTouched)
        {
            foulInThisTurn = true;
        }
    }

    private void HandleFoulPenalties()
    {
        int penaltyPoints = CalculatePenaltyPoints();
        Debug.Log("Award Total Plenty Points : " + penaltyPoints);

        snookerGivePenaltyPoints(penaltyPoints);
        firstBallTouched.Show("first BALL TOUCHED");
        cueBallPotted.Show("cueBallPotted");
        if (!firstBallTouched && !cueBallPotted)
        {
            alertOptionalTextPrefix = "Missed!\n";
            SnookerFlow.Log("foul — missed, no ball touched");
            // Only show notification in AI mode
            if (_SnokerUIManager != null)
            {
                _SnokerUIManager.showNotification(alertOptionalTextPrefix, 1.5f);
            }
        }
    }

    private int CalculatePenaltyPoints()
    {
        //snookerNominatedBall.Show("Nominated Ball");
        //snookerPointsCurShot.Show("Points Cur Shot");
        Debug.Log("TargetBall Number : " + _SnokerGameManager.GetCurrentTargetAsInt());
        if (cueBallPotted)
        {
            if (_SnokerGameManager.GetCurrentTargetAsInt() > 4 && _SnokerGameManager.GetCurrentTargetAsInt() <= 7)
            {
                return _SnokerGameManager.GetCurrentTargetAsInt();
            }
            else
            {
                Debug.Log("Award Plenty Points : " + snookerFirstTouchedBallNum + "-" + snookerPointsCurShot + "-" + snookerFirstTouchedBallNum);
                return ballPottedInThisTurn ?
                Mathf.Max(snookerFirstTouchedBallNum, snookerPointsCurShot) :
                snookerFirstTouchedBallNum;
            }
        }
        else if (firstBallTouched)
        {
            return ballPottedInThisTurn ? snookerPointsCurShot > 4 ? pottedBallsInThisTurn.Count > 0 ? pottedBallsInThisTurn.Max() : snookerPointsCurShot : Mathf.Max(snookerFirstTouchedBallNum, snookerPointsCurShot) : snookerPointsCurShot > 4 ? snookerPointsCurShot : snookerFirstTouchedBallNum;
        }
        else
        {
            return _SnokerGameManager.GetCurrentTargetAsInt() > 4 ? _SnokerGameManager.GetActiveColorBall() : snookerNominatedBall;
        }
    }

    private void AwardPoints()
    {
        snookerGiveScorePoints(SnokerGameManager.currentTurn, snookerPointsCurShot);
    }

    #endregion

    #region Game End Conditions

    private bool HandleGameEndConditions(MODE_TYPE gameMode)
    {
        if (IsBlackBallScenario())
        {
            if (AreScoresTied())
            {
                HandleTiedGame();
                return false; // Game continues
            }
            else
            {
                DetermineWinner();
                if (SnokerNetwork.IsMultiplayer)
                {
                    StickManager.instance.GameWinnerId(_SnokerGameManager.gameWinner, false);
                }
                else
                {
                    if (_SnokerGameManager.gameWinner == staticVariables.UserProfiledata.user._id.ToString())
                        _SnokerGameManager.scheduleGameOverWithNotif($"With a higher score, {playerNames[0]} wins the game.");
                    else
                        _SnokerGameManager.scheduleGameOverWithNotif($"With a higher score, {playerNames[1]} wins the game.");
                }
                //// Multiplayer specific: notify other players
                //if (gameMode == MODE_TYPE.Multiplayer)
                //{
                //    _snokerNetwork.RunRPCAfterInternet(() =>
                //    {
                //        StickManager.instance.RpcGameCompleted(_SnokerGameManager.gameWinner);
                //    });
                //}

                return true; // Game ended
            }
        }

        return false; // Game continues
    }
    public void NetworkWinner(string winnerId, bool DueToDisconnect = false, bool timerEnd = false)
    {
        _SnokerGameManager.gameWinner = winnerId;
        SnookerFlow.Log($"result: {SnookerFlow.Who(winnerId)} wins" + (DueToDisconnect ? " — opponent disconnected" : timerEnd ? " — match time over" : "") + $" ({SnookerFlow.Scores()})");

        if (winnerId == staticVariables.UserProfiledata.user._id.ToString())
        {
            if (ResultManager.isGameFinished == false)
            {
                var name = playerNames[0];

                // Check if win is due to opponent's multiple disconnects
                if (DueToDisconnect)
                {
                    _SnokerGameManager.scheduleGameOverWithNotif($"{name} wins! Opponent disconnected multiple times.");
                }
                else
                {
                    if (timerEnd)
                    {
                        _SnokerGameManager.scheduleGameOverWithNotif($"Time’s up! With a higher score, {name} wins the game.");

                    }
                    else
                        _SnokerGameManager.scheduleGameOverWithNotif($"With a higher score, {name} wins the game.");
                }

                ResultManager.isGameFinished = true;
            }
        }
        else
        {
            if (ResultManager.isGameFinished == false)
            {
                var name = playerNames[1];

                // Check if loss is due to current user's multiple disconnects
                if (DueToDisconnect)
                {
                    _SnokerGameManager.scheduleGameOverWithNotif($"You lost due to multiple disconnections (5+).");
                }
                else
                {
                    if (timerEnd)
                    {
                        _SnokerGameManager.scheduleGameOverWithNotif($"Time’s up! With a higher score, {name} wins the game.");
                    }
                    else
                        _SnokerGameManager.scheduleGameOverWithNotif($"With a higher score, {name} wins the game.");
                }

                ResultManager.isGameFinished = true;
            }
        }

        MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ServerShutdown;
        NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
    }

    private bool IsBlackBallScenario()
    {
        return !_SnokerGameManager.ballsArray[20].activeSelf && _SnokerGameManager.GetCurrentTargetAsInt() == 7;
    }

    private bool AreScoresTied()
    {
        if (SnokerNetwork.IsMultiplayer)
            return NetworkGameManager.Instance.creatorData.Scores == NetworkGameManager.Instance.joinerData.Scores;
        else
            return snookerScoresVal[0] == snookerScoresVal[1];
    }

    private void HandleTiedGame()
    {
        snookerReSpotColorBall(_SnokerGameManager.GetCurrentTargetAsInt() + 14 - 1);
        resetCueBallToStart();
        SnokerGameManager.bBallInHand = true;
        if (SnokerNetwork.IsMultiplayer)
        {
            if (NetworkServer.active)
            {
                StickManager.instance.SetBallInHand(true);
            }
            StickManager.instance?.CmdSetBallInHand(true);
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkRigidbodyUnreliable>().syncDirection = SyncDirection.ClientToServer;
            SnokerNetwork.Instance._mainScript.GetComponent<NetworkTransformUnreliable>().syncDirection = SyncDirection.ClientToServer;
        }
        if (GameMode != 0 || SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            goToBallInHand();
        }

        setAllBallKinematic(true, 99);
        SnookerFlow.Log($"scores tied ({SnookerFlow.Scores()}) — black re-spotted, {SnookerFlow.Who(SnokerGameManager.currentTurn)} plays with ball in hand");
        _SnokerUIManager.showNotification(
            $"Scores Tied, Re-spotting the Black.\n{playerNames[SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString() ? 0 : 1]} won the toss and will strike first.",
            7f);
    }

    private void DetermineWinner()
    {
        if (SnokerNetwork.IsMultiplayer)
        {
            _SnokerGameManager.gameWinner = NetworkGameManager.Instance.joinerData.Scores > NetworkGameManager.Instance.creatorData.Scores ? NetworkGameManager.Instance.joinerData.playerId : NetworkGameManager.Instance.creatorData.playerId;
        }
        else
        {
            _SnokerGameManager.gameWinner = snookerScoresVal[0] > snookerScoresVal[1] ? staticVariables.UserProfiledata.user._id.ToString() : "ai";
        }
        Debug.Log("Who is winner " + _SnokerGameManager.gameWinner + "--" + snookerScoresVal[0] + "--" + snookerScoresVal[1]);
        SnookerFlow.Log($"black potted — game over, {SnookerFlow.Who(_SnokerGameManager.gameWinner)} wins ({SnookerFlow.Scores()})");
    }

    public void HandleBallRespotting()
    {
        if (SnokerGameManager.snookerRedPottedCount < _SnokerGameManager.snookerRedsSelected || _SnokerGameManager.GetCurrentTargetAsInt() == 99)
        {
            snookerReSpotColorBall(15);
        }
        if (foulInThisTurn && SnokerGameManager.snookerRedPottedCount == _SnokerGameManager.snookerRedsSelected)
        {
            snookerReSpotColorBall(_SnokerGameManager.GetCurrentTargetAsInt() + 14 - 1);
        }
    }


    #endregion

    #region Turn Management

    private void HandleFoulConsequences(MODE_TYPE gameMode)
    {


        if (foulInThisTurn)
        {
            if (gameMode == MODE_TYPE.Multiplayer && !NetworkServer.active)
            {

                SnokerGameManager.bBallInHand.Show("Ball in hand Server");
                cueBallPotted.Show("Ball in hand cuepotted");
                foulInThisTurn.Show("foul in this turn");
                _SnokerGameManager.isyourturn.Show("Is turn");

            }
            SnokerGameManager.bBallInHand = cueBallPotted;

            if (SnokerGameManager.bBallInHand)
            {
                SnookerFlow.Log($"ball in hand for {SnookerFlow.Who(SnokerGameManager.currentTurn)}");
                if (gameMode == MODE_TYPE.Multiplayer)
                {
                    setAllBallKinematic(true, 99);
                    resetCueBallToStart();
                    if (!_SnokerGameManager.isyourturn)
                        return;
                    goToBallInHand();
                }
                else // AI mode
                {
                    if (GameMode != 0 || SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        goToBallInHand();
                    }
                    setAllBallKinematic(true, 99);
                }
            }
            else
            {
                toggleSelectedCue(!SnokerGameManager.bBallInHand);

            }
        }
    }
    [Button]
    private void ResetCuePosition()
    {
        while (cueParentObjTransform.position != thisTransform.position)
            cueParentObjTransform.position = thisTransform.position;
    }

    public void SetupPlayerControls(MODE_TYPE gameMode)
    {
        if (!SnokerGameManager.bBallInHand && _SnokerGameManager.bTossDone)
        {
            if (gameMode == MODE_TYPE.AI)
            {
                SetupAIControls();
            }
            else // Multiplayer
            {
                SetupMultiplayerControls();
            }

            thisRigidbody.linearVelocity = Vector3.zero;
        }
    }

    private void SetupAIControls()
    {
        if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            showGuideWithType(GUIDE_TYPE.FULL);
            showPowerMeter(true);
            spinBtnObj.SetActive(true);
        }
        toggleSelectedCue(true);
    }

    private void SetupMultiplayerControls()
    {
        toggleSelectedCue(!SnokerGameManager.bBallInHand);
        if (_snokerNetwork.IsMyTurn())
        {
            showGuideWithType(GUIDE_TYPE.FULL);
            showPowerMeter(true);
            spinBtnObj.SetActive(true);
        }
        else
        {
            showGuideWithType(GUIDE_TYPE.NO);
            spinBtnObj.SetActive(false);

        }
    }

    public void HandlePlayerTurns(MODE_TYPE gameMode)
    {
        if (!_SnokerGameManager.bTossDone) return;

        if (gameMode == MODE_TYPE.AI)
        {
            HandleAIOrPlayerTurn();
        }
        else
        {
            _snokerNetwork.RunRPCAfterInternet(() =>
            {
                if (NetworkServer.active) SnokerNetwork.Instance.StartTurnTimer(); ;
            });
            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager.isyourturn ? _SnokerGameManager._SnokerCameraManager.cameraMode : CAMERA_MODE.AI);
        }
    }

    private void HandleAIOrPlayerTurn()
    {
        if (SnokerGameManager.currentTurn == "ai")
        {
            _SnokerGameManager._SnokerAIManager.aiStart();
        }
        else
        {
            aiPlaying = false;
            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager._SnokerCameraManager.cameraMode);
        }

    }
    Coroutine turnTimer;
    public void StartTurnTimerAI()
    {
        Debug.Log("Trying to run Timer");
        if (turnTimer != null)
        {
            StopCoroutine(turnTimer);
        }
        turnDuration = 35;
        turnTimer = StartCoroutine(TurnTimerAI(SnokerGameManager.currentTurn));
    }
    public void HandleAutoAim()
    {
        if ((SnokerNetwork.IsMultiplayer && _SnokerGameManager.isyourturn))
        {
            if (!aiPlaying &&
            !SnokerGameManager.bBallInHand &&
            _SnokerGameManager.bTossDone)
            {
                this.DelayUntil(() => StickManager.instance.netIdentity.isOwned, () =>
                {
                    doAutoTarget();

                });
            }
        }
        else if (!SnokerNetwork.IsMultiplayer)
        {
            if (!aiPlaying &&
            !SnokerGameManager.bBallInHand &&
            _SnokerGameManager.bTossDone)
            {
                doAutoTarget();
            }
        }
    }

    #endregion

    #region Multiplayer Specific Methods



    /// <summary>
    /// Legacy method for backward compatibility - calls unified method
    /// </summary>
    public void AfterChangingTurnMultiplayer()
    {
        AfterChangingTurn(MODE_TYPE.Multiplayer);
    }





    public void snookerSetTargetBAll(int targetBall)
    {
        SnokerGameManager.snookerTargetBall = (SnookerTargetType)targetBall;
        _SnokerGameManager.UpdateBallDisplay();
        thisRigidbody.isKinematic = true;
        thisRigidbody.linearVelocity = Vector3.zero;
    }
    public void ChangeTurn()
    {
        if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            SnokerGameManager.currentTurn = "ai";
        }
        else
        {
            SnokerGameManager.currentTurn = staticVariables.UserProfiledata.user._id.ToString();
        }

        {
            if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
            {
                playerOneTimer.fillAmount = 1f;
                playerOneTimer.gameObject.SetActive(true);
                playerTwoTimer.gameObject.SetActive(false);
            }
            else
            {
                playerOneTimer.gameObject.SetActive(false);
                playerTwoTimer.gameObject.SetActive(true);
                playerTwoTimer.fillAmount = 1f;
            }
        }

        if (GameMode != 0 || SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            _SnokerUIManager.showNotification(playerNames[SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString() ? 0 : 1] + " to Play");
            switchControls();
            spinBtnObj.SetActive(true);
        }
        if (GameMode == MODE_TYPE.AI && SnokerGameManager.currentTurn == "ai")
        {
            _SnokerGameManager._SnokerAIManager.aiBallsPottedInThisTurn = 0;
        }

        SnookerFlow.Log($"turn → {SnookerFlow.Who(SnokerGameManager.currentTurn)}");
        igSnookerTurnIndicator.SetParent(snookerScoresText[SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString() ? 0 : 1].transform, false);
        if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            igSnookerTurnIndicator.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 180, 0f);
            igSnookerTurnIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(32.6f, 0);
        }
        else
        {
            igSnookerTurnIndicator.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0f);
            igSnookerTurnIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(-32.6f, 0);


        }
    }
    public float elapsedTime = 0f;
    public bool resetTimer = false; // Add this as a class variable

    public IEnumerator TurnTimerAI(string TimerTurn)
    {
        elapsedTime = 0f;
        while (elapsedTime < turnDuration)
        {
            // Check if turn changed at the beginning of each frame
            if (TimerTurn != SnokerGameManager.currentTurn)
            {
                Debug.Log("Timer Breaked - Turn Changed");
                yield break; // Use yield break instead of break to properly exit coroutine
            }
            if (resetTimer)
            {
                elapsedTime = 0f;
                resetTimer = false;
                Debug.Log("Timer Reset");
            }
            if (!_SnokerGameManager.bGamePaused && _SnokerGameManager.ballIsStanding)
            {
                elapsedTime += Time.deltaTime;
            }

            float fillAmount = 1f - (elapsedTime / turnDuration);
            fillAmount = Mathf.Clamp01(fillAmount);


            if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
                changeUI(playerOneTimer, fillAmount);
            else
                changeUI(playerTwoTimer, fillAmount);


            yield return null;
        }

        // Only execute ball hit if timer naturally expired (wasn't broken by turn change)
        if (TimerTurn == SnokerGameManager.currentTurn)
        {

            SnookerFlow.Log($"turn timer ran out for {SnookerFlow.Who(TimerTurn)}");
            hitTheBall(0, cueParentObjTransform.forward, guideDirCueBallTrans.forward, true);

        }
        else
        {
            Debug.Log("Timer ended but turn had changed - skipping ball hit");
        }
    }

    private void snookerReSpotColorBall(int loopStart)
    {
        setAllBallKinematic(true, 99);
        if (loopStart < 15)
        {
            loopStart = 15;
        }
        for (int i = loopStart; i < 21; i++)
        {
            bool isPotted = pottedBallsNumbers.Contains(i);

            if (!isPotted)
            {
                continue;
            }

            Vector3 vector = _SnokerGameManager.ballPositions[i];
            Collider[] array = Physics.OverlapSphere(vector, ballRadius, ballReSpotLayerMask);
            if (array.Length > 0)
            {
                bool flag = true;
                for (int num = 20; num > 14; num--)
                {
                    vector = _SnokerGameManager.ballPositions[num];
                    array = Physics.OverlapSphere(vector, ballRadius, ballReSpotLayerMask);
                    if (array.Length == 0)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    Vector3 vector2 = Vector3.back;
                    int num2;
                    if (i < 19)
                    {
                        num2 = 15;
                    }
                    else
                    {
                        Vector3 mAX_PLAY_AREA_LIMIT = MAX_PLAY_AREA_LIMIT;
                        num2 = Mathf.FloorToInt(mAX_PLAY_AREA_LIMIT.z - _SnokerGameManager.ballPositions[i].z) * 2;
                    }
                    vector = _SnokerGameManager.ballPositions[i];
                    do
                    {
                        vector.z += 0.5f;
                        array = Physics.OverlapSphere(vector, ballRadius, ballReSpotLayerMask);
                        num2--;
                    }
                    while (array.Length > 0 && num2 > 0);
                    if (array.Length > 0)
                    {
                        vector2 = Vector3.forward;
                        num2 = 15;
                        vector = _SnokerGameManager.ballPositions[i];
                        do
                        {
                            vector.z -= ballRadius;
                            array = Physics.OverlapSphere(vector, ballRadius, ballReSpotLayerMask);
                            num2--;
                        }
                        while (array.Length > 0 && num2 > 0);
                    }
                    RaycastHit hitInfo;
                    if (Physics.SphereCast(vector, ballRadius, vector2, out hitInfo, 100f, ballReSpotLayerMask))
                    {
                        vector += vector2 * hitInfo.distance;
                    }
                }
            }
            Debug.Log("Re-spotting ball: " + (i + 1) + " at position: " + vector);
            SnookerFlow.Log($"{SnookerFlow.Ball(i - 13)} re-spotted");

            _SnokerGameManager.ballsArray[i].SetActive(true);
            _SnokerGameManager.ballsArray[i].transform.position = vector;
            _SnokerGameManager.ballsRigidbodyArray[i].linearVelocity = Vector3.zero;
            _SnokerGameManager.ballsRigidbodyArray[i].constraints |= RigidbodyConstraints.FreezePositionY;
            if (SnokerNetwork.IsMultiplayer)
                StickManager.instance.RespotRpc(i, vector);
        }
        Invoke("snookerNonKinematicAfterReSpot", 0.5f);
    }

    private void snookerNonKinematicAfterReSpot()
    {
        if (!SnokerGameManager.bBallInHand && (NetworkServer.active || !SnokerNetwork.IsMultiplayer))
        {
            setAllBallKinematic(false, 99);
        }
    }

    private void snookerGiveScorePoints(string toPlayer, int val)
    {
        //toPlayer.Show("Current Turn");
        if (SnokerNetwork.IsMultiplayer)
        {
            _snokerNetwork.ChangeScoreText(toPlayer, val);
        }
        else
        {
            snookerScoresVal[toPlayer == "ai" ? 1 : 0] += val;
            snookerScoresText[toPlayer == "ai" ? 1 : 0].text = string.Empty + snookerScoresVal[toPlayer == "ai" ? 1 : 0];
            if (val != 0) SnookerFlow.Log($"score updated: {SnookerFlow.Who(toPlayer)} +{val} → {SnookerFlow.Scores()}");
        }
    }

    private void snookerGivePenaltyPoints(int val)
    {
        string PenaltyPointID = "ai";
        if (!SnokerNetwork.IsMultiplayer)
        {
            if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
            {
                if (GameMode == MODE_TYPE.AI)
                {
                    PenaltyPointID = "ai";
                }
                else
                {
                    PenaltyPointID = staticVariables.OpponetProfile.userId;
                }
            }
            else
            {
                PenaltyPointID = staticVariables.UserProfiledata.user._id.ToString();

            }
        }
        else
        {
            if (SnokerGameManager.currentTurn == NetworkGameManager.Instance.creatorData.playerId.ToString())
            {
                PenaltyPointID = NetworkGameManager.Instance.joinerData.playerId;

            }
            else
            {
                PenaltyPointID = NetworkGameManager.Instance.creatorData.playerId;

            }
        }
        SnookerFlow.Log($"foul penalty: {Mathf.Clamp(val, 4, 7)} points to {SnookerFlow.Who(PenaltyPointID)}");
        snookerGiveScorePoints(PenaltyPointID, Mathf.Clamp(val, 4, 7));
    }
    Vector3 lastvalidpos = Vector3.zero;
    private void limitBallInHand()
    {
        Vector3 currentPosition = thisTransform.position;
        Vector3 targetPosition = currentPosition;

        // Smooth D-zone limit (circular area)
        float distanceFromStart = Vector3.Distance(CUEBALL_START_SNOOKER_POS, currentPosition);
        if (distanceFromStart > 0.30f)
        {
            // Smoothly pull back instead of hard snapping
            Vector3 direction = (currentPosition - CUEBALL_START_SNOOKER_POS).normalized;
            targetPosition = CUEBALL_START_SNOOKER_POS + direction * 0.30f;
            targetPosition.y = CUEBALL_START_SNOOKER_POS.y;
        }

        // Apply table boundary limits with smooth clamping
        targetPosition.x = Mathf.Clamp(targetPosition.x, -MAX_PLAY_AREA_LIMIT.x, MAX_PLAY_AREA_LIMIT.x);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -MAX_PLAY_AREA_LIMIT.z, MAX_PLAY_AREA_LIMIT.z);

        // Additional constraint for snooker D-zone (behind the baulk line)
        if (targetPosition.x > CUEBALL_START_SNOOKER_POS.x)
        {
            targetPosition.x = CUEBALL_START_SNOOKER_POS.x;
        }
        if (IsValidBallPosition(targetPosition))
        {
            // Smooth transition to target position
            float smoothSpeed = 50f; // Adjust for desired smoothness
            thisTransform.position = Vector3.Lerp(currentPosition, targetPosition, smoothSpeed * cachedDeltaTime);
            lastvalidpos = thisTransform.position;
            // Smooth cue parent following
            cueParentObjTransform.position = Vector3.Lerp(
                cueParentObjTransform.position,
                thisTransform.position,
                smoothSpeed * cachedDeltaTime
            );
        }
        else
        {
            thisTransform.position = lastvalidpos;
        }

        // Smooth velocity dampening instead of hard stop
        if (thisRigidbody.linearVelocity.magnitude > 0f)
        {
            thisRigidbody.linearVelocity = Vector3.Lerp(
                thisRigidbody.linearVelocity,
                Vector3.zero,
                5f * cachedDeltaTime
            );
        }
    }
    #endregion

    #region PowerMeter


    public void powerFlickOnPointerDown()
    {
        if (powerMeterActive)
        {
            _SnokerGameManager._SnokerCameraManager.camCanRotate = false;
        }
    }

    public void powerFlickOnPointerUp()
    {
        if (powerMeterActive)
        {
            if (controlPowerFlickSliderVal != 0)
            {
                if (GameMode == MODE_TYPE.Multiplayer)
                {
                    showPowerMeter(false);
                    toggleSelectedCue(false);
                    if (SnokerNetwork.Instance.IsMyTurn())
                        StickManager.instance.CmdExecuteBallHit(strikePowerVal, cueParentObjTransform.forward, guideDirCueBallTrans.forward, transform.position, lastTargetVector, _SnokerGameManager.GetCurrentTargetAsInt(),
                          cueParentObjTransform.position, cueObjectTransform.localEulerAngles, cueParentObjTransform.eulerAngles);
                }
                else
                    hitTheBall(strikePowerVal, cueParentObjTransform.forward, guideDirCueBallTrans.forward);
            }
            controlPowerFlickSliderVal = 0f;
            strikePowerVal = 0f;
            cueDistance = 0f;
            _SnokerGameManager._SnokerCameraManager.camCanRotate = true;
        }
        powerFlickSliderObj.value = 0f;
        powerFlickFillImgComp.fillAmount = 0f;
    }

    public void setStrikePowerVal_PowerFlick(float val)
    {
        if (powerMeterActive)
        {
            controlPowerFlickSliderVal = val;
            strikePowerVal = controlPowerFlickSliderVal;
            cueDistance = controlPowerFlickSliderVal;
            powerFlickFillImgComp.fillAmount = strikePowerVal;
        }
    }

    private void setInitialStrikePower()
    {
        cueDistance = strikePowerVal;
    }

    public void showPowerMeter(bool val)
    {
        if (val)
        {
            uiAnimatorPowerMeter.animTarget = 0f;
            _SnokerGameManager._SnokerCameraManager.camCanRotate = true;
            powerMeterActive = true;
            return;
        }
        uiAnimatorPowerMeter.animTarget = 1f;
        powerMeterActive = false;

    }



    public void switchControls()
    {
        setInitialStrikePower();

        powerMetersParentRectTrans.anchorMin = new Vector2(1f, powerMetersParentRectTrans.anchorMin.y);
        powerMetersParentRectTrans.anchorMax = new Vector2(1f, powerMetersParentRectTrans.anchorMax.y);
        powerMetersParentRectTrans.pivot = new Vector2(0f, powerMetersParentRectTrans.pivot.y);
        uiAnimatorPowerMeter.targetPosX = ((!iPadDevice) ? (-130) : (-70));
        leftSideBtnsParentRectTrans.anchorMin = new Vector2(0f, leftSideBtnsParentRectTrans.anchorMin.y);
        leftSideBtnsParentRectTrans.anchorMax = new Vector2(0f, leftSideBtnsParentRectTrans.anchorMax.y);
        leftSideBtnsParentRectTrans.pivot = new Vector2(1f, leftSideBtnsParentRectTrans.pivot.y);
        leftSideBtnsParentRectTrans.anchoredPosition = new Vector2(100f, leftSideBtnsParentRectTrans.anchoredPosition.y);
        spinOkBtnRectTrans.anchorMin = new Vector2(1f, spinOkBtnRectTrans.anchorMin.y);
        spinOkBtnRectTrans.anchorMax = new Vector2(1f, spinOkBtnRectTrans.anchorMax.y);
        spinOkBtnRectTrans.pivot = new Vector2(1f, spinOkBtnRectTrans.pivot.y);
        okBtnParentRectTrans.anchorMin = new Vector2(1f, okBtnParentRectTrans.anchorMin.y);
        okBtnParentRectTrans.anchorMax = new Vector2(1f, okBtnParentRectTrans.anchorMax.y);
        okBtnParentRectTrans.pivot = new Vector2(1f, okBtnParentRectTrans.pivot.y);
        spinControlGroupRectTrans.anchorMin = new Vector2(0f, spinControlGroupRectTrans.anchorMin.y);
        spinControlGroupRectTrans.anchorMax = new Vector2(0f, spinControlGroupRectTrans.anchorMax.y);
        spinControlGroupAnimScript.handModeSpinControl = HAND_MODE.Right;
    }
    #endregion

    #region StartGameFlow
    public void onClickStartGameBtn()
    {
        savePlayersData();
        startNewGame();
    }

    private void startNewGame()
    {
        setMusicVol(musicVolMultiplierInGame);
        startGame();
    }

    private void startGame()
    {
        if (GameMode == MODE_TYPE.AI && !playerNames[1].Contains("[ CPU ]") && !SnokerNetwork.IsMultiplayer)
        {
            string[] array = default(string[]);
            (array = playerNames)[1] = array[1];
        }
        Time.timeScale = 1f;
        resetCueBallToStart();
        cueBallParentObj.SetActive(true);

        cueBallTypesArray[0].SetActive(false);
        cueBallTypesArray[1].SetActive(true);


        cueBallPosOnHit = thisTransform.position;
        _SnokerGameManager.bGamePaused = false;
        SnokerGameManager.bGameOver = false;
        _SnokerGameManager.bTossDone = false;
        aiPlaying = false;
        _SnokerGameManager.ballIsStanding = true;
        //_SnokerGameManager.bBallInHand = false;
        canRePlaceCueBall = false;
        strikeCount = 0;
        ballsPottedCount = 0;
        pottedBallsInThisTurn.Clear();
        pottedBallsNumbers.Clear();
        railHitCountInThisShot = 0;
        railHitBallArray = new int[21];
        ballsReplaced = true;
        ballPottedInThisTurn = false;
        firstBallTouched = false;
        foulInThisTurn = false;
        cueBallPotted = false;
        cueRailHit = false;
        okBtnObj.SetActive(false);
        placeCueBtnObj.SetActive(false);
        spinBtnObj.SetActive(false);
        showPowerMeter(false);
        hideBottomBlinkingText();
        spinSetOn = false;
        spinControlGroupAnimScript.hideSpinControl(false);
        toggleSelectedCue(false);
        showGuideWithType(GUIDE_TYPE.NO);
        ballInHandIndicatorObj.SetActive(false);
        guiBallDisplayObj.SetActive(false);
        matrixBallNumBtnObj.SetActive(false);

        snookerFirstTouchedBallNum = 0;
        snookerBallInvolvedInFoul = 0;
        //"start to reset curshot".Show();
        snookerPointsCurShot = 0;
        snookerNominatedBall = 1;
        snookerScoresVal = new int[2];
        igSnookerTextsParent.SetActive(true);
        igSnookerBallDisplayImg.sprite = guiBallsTex[15];
        for (i = 0; i < 2; i++)
        {
            snookerScoresText[i].text = "0";
            snookerScoresNameText[i].text = playerNames[i];
        }
        bShowMatrixBallUiNums = false;
        matrixBallNumsParent.SetActive(false);
        _SnokerUIManager.notifScriptComp.gameObject.SetActive(false);
        _SnokerGameManager.scheduledFunctionAfterNotif = null;
        for (i = 0; i < 21; i++)
        {
            _SnokerGameManager.ballPositions[i] = _SnokerGameManager.ballsArray[i].transform.position;

        }
        activateBalls(true);
        CancelInvoke();
        inputToRotTarget.x = UnityEngine.Random.Range(-0.4f, 0.4f);
        inputToRotValue.x = inputToRotTarget.x;
        cueRotValueX = inputToRotValue.x;
        resetCueAndCamDirection();
        setGuideColorWhite();
        SnookerFlow.GameStarted($"game started — {(SnokerNetwork.IsMultiplayer ? "multiplayer" : "vs " + playerNames[1])}, {_SnokerGameManager.snookerRedsSelected} reds");
        if (!SnokerNetwork.IsMultiplayer)
        {
            switchScreen("InGame");
            CancelInvoke("doToss");
            Invoke(nameof(doToss), 2f);

        }
        else
        {
            switchScreen("InGame");
        }
    }
    private void doToss()
    {
        string num = "ai";
        if (UnityEngine.Random.Range(0, 100) > 0)
        {
            num = staticVariables.UserProfiledata.user._id.ToString();
        }
        SnokerGameManager.currentTurn = num;
        SnookerFlow.Log($"{SnookerFlow.Who(num)} won the toss and will break");
        _SnokerUIManager.showNotification((string.Empty) + playerNames[SnokerGameManager.currentTurn == "ai" ? 1 : 0] + " has won the toss\nand will break first.", 5f);
        if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            playerOneTimer.fillAmount = 1f;
            playerOneTimer.gameObject.SetActive(true);
            playerTwoTimer.gameObject.SetActive(false);
        }
        else
        {
            playerOneTimer.gameObject.SetActive(false);
            playerTwoTimer.gameObject.SetActive(true);
            playerTwoTimer.fillAmount = 1f;
        }
        StartTurnTimerAI();
        igSnookerTurnIndicator.SetParent(snookerScoresText[SnokerGameManager.currentTurn == "ai" ? 1 : 0].transform, false);
        if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            igSnookerTurnIndicator.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 180, 0f);
            igSnookerTurnIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(32.6f, 0);
        }
        else
        {
            igSnookerTurnIndicator.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0f);
            igSnookerTurnIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(-32.6f, 0);


        }
        switchControls();
    }
    #endregion

    #region PlayerData
    private void savePlayersData()
    {
        applyPlayerNamesInputData();
        PlayerPrefs.SetString("playerNames0", playerNames[0]);
        PlayerPrefs.SetString("playerNames1", playerNames[1]);
        PlayerPrefs.SetInt("chosenAvatar0", chosenAvatar[0]);
        PlayerPrefs.SetInt("chosenAvatar1", chosenAvatar[1]);
        saveSelectedCue();
    }

    private void saveSelectedCue()
    {
        PlayerPrefs.SetInt("selectedCue0", selectedCue[0]);
        PlayerPrefs.SetInt("selectedCue1", selectedCue[1]);
    }

    private void applyPlayerNamesInputData()
    {
        if (SnokerNetwork.IsMultiplayer)
        {

            playerNames[0] = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;
            playerNames[1] = staticVariables.OpponetProfile.userName;


        }
        else
        {
            playerNames[0] = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;
            playerNames[1] = "AI";
        }
    }

    private void loadSavedData()
    {
        startupCounter = PlayerPrefs.GetInt("startupCounter", 0);
        startupCounter++;
        PlayerPrefs.SetInt("startupCounter", startupCounter);
        //playerNames[0] = PlayerPrefs.GetString("playerNames0", playerNames[0]);
        //playerNames[1] = PlayerPrefs.GetString("playerNames1", playerNames[1]);
        chosenAvatar[0] = PlayerPrefs.GetInt("chosenAvatar0", chosenAvatar[0]);
        chosenAvatar[1] = PlayerPrefs.GetInt("chosenAvatar1", chosenAvatar[1]);
        selectedCue[0] = PlayerPrefs.GetInt("selectedCue0", selectedCue[0]);
        selectedCue[1] = PlayerPrefs.GetInt("selectedCue1", selectedCue[1]);
        controlMode[0] = (CONTROLS)PlayerPrefs.GetInt("controlMode0", 2);
        controlMode[1] = (CONTROLS)PlayerPrefs.GetInt("controlMode1", 2);
        handMode[0] = (HAND_MODE)PlayerPrefs.GetInt("handMode0", 0);
        handMode[1] = (HAND_MODE)PlayerPrefs.GetInt("handMode1", 0);
        soundEnabled = PlayerPrefs.GetInt("soundEnabled", soundEnabled ? 1 : 0) == 1;
        musicVolVal = PlayerPrefs.GetFloat("musicVolVal", musicVolVal);
        musicVolMultiplierInGame = PlayerPrefs.GetFloat("musicVolMultiplierInGame", musicVolMultiplierInGame);
        sensitivityValue = PlayerPrefs.GetFloat("sensitivityValue", sensitivityValue);
        guideType = (GUIDE_TYPE)PlayerPrefs.GetInt("guideType", 2);
        roomEnabled = PlayerPrefs.GetInt("roomEnabled", roomEnabled ? 1 : 0) == 1;
        redGuideEnabled = PlayerPrefs.GetInt("redGuideEnabled", redGuideEnabled ? 1 : 0) == 1;
        pinchZoomEnabled = PlayerPrefs.GetInt("pinchZoomEnabled", pinchZoomEnabled ? 1 : 0) == 1;
        tapToAimEnabled = PlayerPrefs.GetInt("tapToAimEnabled", tapToAimEnabled ? 1 : 0) == 1;
        if (PlayerPrefs.HasKey("musicVol"))
        {
            musicVolVal = PlayerPrefs.GetFloat("musicVol", musicVolVal);
            PlayerPrefs.SetFloat("musicVolVal", musicVolVal);
            PlayerPrefs.DeleteKey("musicVol");
        }
        if (PlayerPrefs.HasKey("autoAimEnabled"))
        {
            autoAimEnabled = PlayerPrefs.GetInt("autoAimEnabled", autoAimEnabled ? 1 : 0) == 1;
        }
        else
        {
            if (startupCounter > 1)
            {
                autoAimEnabled = false;
            }
            PlayerPrefs.SetInt("autoAimEnabled", autoAimEnabled ? 1 : 0);
        }
        userSelControlDone = PlayerPrefs.HasKey("userSelControlDone");

    }


    #endregion
    public float smoothTime = 0.1f;

    [ContextMenu("Change Turn")]
    public void ChangeTurnTemp()
    {
        StickManager.instance.CmdNextTurn(SnokerGameManager.currentTurn);
    }
    public List<int> PottedBallsInThisTurn { get => pottedBallsInThisTurn; set => pottedBallsInThisTurn = value; }
}

