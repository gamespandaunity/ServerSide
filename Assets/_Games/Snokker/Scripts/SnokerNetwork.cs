using CarRace;
using Mirror;
using NaughtyAttributes;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensions;

public class SnokerNetwork : NetworkBehaviour
{
    public SnokerGameManager _SnokerGameManager;
    public SnokerUIManager _SnokerUIManager;

    private void OnEnable()
    {

        MirrorNetwork.OnWinCall += AnnounceVictory;
        MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
        MirrorNetwork.OnGameStateChanged += _SnokerGameManager.PauseUnpauseGame;
    }

    private void OnDisable()
    {
        MirrorNetwork.OnWinCall -= AnnounceVictory;
        MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
        MirrorNetwork.OnGameStateChanged -= _SnokerGameManager.PauseUnpauseGame;
    }
    private void OnDestroy()
    {
        Instance = null;
    }

    #region Public Fields

    static public SnokerNetwork Instance;
    static public bool IsMultiplayer = false;
    static public bool isTossDone = false;
    static public bool dataisset = false;
    public bool isReversed = false;
    public mainScript _mainScript;
    public Collider Holestrigger;
    public GameObject StickPrefab;

    #endregion
    #region MonoBehaviour CallBacks
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        IsMultiplayer = true;
        if (IsMultiplayer)
        {
            Debug.Log("Total Player Count: " + MirrorNetwork.Instance.numPlayers);
            if (NetworkServer.active)
            {
                SpawnStick();
                if (countdownCoroutine != null)
                    StopCoroutine(countdownCoroutine);
                countdownCoroutine = StartCoroutine(ServerCountdown());
            }

            StartCoroutine(checkPingAfterDelay());
        }
    }
    #region Game Timer Syncing
    [Header("UI Timer")]
    //public TextMeshProUGUI countDownGameTimer;

    [Header("Countdown Settings")]
    [SyncVar(hook = nameof(OnTimeChanged))]
    public int remainingTime = 600; // server owns this

    public int remainingminutes;
    public int remainingseconds;

    private Coroutine countdownCoroutine;

    [Server]
    private IEnumerator ServerCountdown()
    {
        remainingTime = 600;
        Debug.Log("Server Countdown Started." + remainingTime);
        while (remainingTime > 0)
        {
            while (NetworkGameManager.Instance != null && NetworkGameManager.Instance.currentPlayerCount < 2)
            {
                yield return new WaitForSeconds(0.5f);
            }

            remainingTime--; 
            yield return new WaitForSeconds(1f);
        }
        //while (remainingTime > 0)
        //{
        //    if (!NetworkGameManager.Instance.IsPaused)
        //        remainingTime--; // SyncVar updates all clients automatically
        //    yield return new WaitForSeconds(1f);
        //}
        _SnokerGameManager.gameWinner = NetworkGameManager.Instance.joinerData.Scores > NetworkGameManager.Instance.creatorData.Scores ? NetworkGameManager.Instance.joinerData.playerId : NetworkGameManager.Instance.creatorData.playerId;
        SnookerFlow.SendResult(_SnokerGameManager.gameWinner, "match time over");
        NetworkGameManager.Instance.creatorData.Scores = 0;
        NetworkGameManager.Instance.joinerData.Scores = 0;
        ApiAndRoomManager._instance.WinnerLossChallenge(_SnokerGameManager.gameWinner);
        RpcTimerEnded(_SnokerGameManager.gameWinner);
    }

    // This runs on all clients whenever remainingTime changes
    private void OnTimeChanged(int oldTime, int newTime)
    {
        remainingminutes = Mathf.FloorToInt(newTime / 60);
        remainingseconds = Mathf.FloorToInt(newTime % 60);
        _SnokerGameManager.countDownGameTimer.text = string.Format("{0:00}:{1:00}", remainingminutes, remainingseconds);

        if (newTime > 180)
            _SnokerGameManager.countDownGameTimer.color = Color.green;
        else if (newTime > 60)
            _SnokerGameManager.countDownGameTimer.color = Color.white;
        else
            _SnokerGameManager.countDownGameTimer.color = Color.red;
    }

    [ClientRpc]
    private void RpcTimerEnded(string winnerID)
    {
        _SnokerGameManager.countDownGameTimer.text = "00:00";
        _SnokerGameManager.countDownGameTimer.color = Color.red;
        _mainScript.NetworkWinner(winnerID, timerEnd: true);
    }
    #endregion


    private void SpawnStick()
    {
        GameObject go = Instantiate(StickPrefab);
        NetworkServer.Spawn(go);
    }
    public bool IsMyTurn()
    {
        if (NetworkServer.active)
            return false;
        else
            return SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString();
    }
    #endregion

    #region Fusion RPCs

    [ClientRpc]
    public void RpcValuesSync(int challengeAmount, bool isGoldCoin)
    {
        staticVariables.currentPrize = challengeAmount;
        staticVariables.isgoldcoins = isGoldCoin;
        _SnokerGameManager.updateCoinStatus();

        Debug.LogFormat("RpcValuesSync called with {0} and {1}", challengeAmount, isGoldCoin);
    }


    #endregion

    #region Public Methods
    public void DoToss()
    {
        if (isTossDone) return;
        Debug.Log($"{_mainScript.initialized} max player: {MirrorNetwork.Instance.numPlayers}");
        SnokerGameManager.currentTurn = NetworkGameManager.Instance.creatorData.playerId;
        StickManager.instance?.AssignStickAuthority();
        SnokerGameManager.bBallInHand = true;
        StickManager.instance.SetBallInHand(true);
        _SnokerUIManager.showNotification(_mainScript.playerNames[IsMyTurn() ? 0 : 1] + " has won the toss\nand will break first.", 2f);
        UpdateTimerDisplay();
        _mainScript.igSnookerTurnIndicator.SetParent(IsMyTurn() ? GetTransformByName("P1") : GetTransformByName("P2"), false);
        UpdateTurnIndicatorPosition();
        //  StickManager.instance.RpcStartTurnTimer();
        StartTurnTimer();
        _mainScript.switchScreen("InGame");
        _mainScript.cueBallParentObj.SetActive(true); // ensure cue ball is visible on server too
        Holestrigger.enabled = true;
        _SnokerGameManager.isyourturn = IsMyTurn();
        _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(IsMyTurn() ? CAMERA_MODE.NORMAL : CAMERA_MODE.AI);
        StickManager.instance.NetorkedTurn = SnokerGameManager.currentTurn;
        isTossDone = true;
        SnookerFlow.Log($"{SnookerFlow.Who(SnokerGameManager.currentTurn)} won the toss and will break");

    }

    private void UpdateTimerDisplay()
    {
        if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            _mainScript.playerOneTimer.fillAmount = 1f;
            _mainScript.playerOneTimer.gameObject.SetActive(true);
            _mainScript.playerTwoTimer.gameObject.SetActive(false);
        }
        else
        {
            _mainScript.playerOneTimer.gameObject.SetActive(false);
            _mainScript.playerTwoTimer.gameObject.SetActive(true);
            _mainScript.playerTwoTimer.fillAmount = 1f;
        }
    }


    public void NotifyTossResult(string num)
    {
        num.Show("Num");
        SnokerGameManager.currentTurn = num;
        _SnokerGameManager.isyourturn = IsMyTurn();
        _mainScript.switchScreen("InGame");
        _mainScript.igSnookerTurnIndicator.SetParent(IsMyTurn() ? GetTransformByName("P1") : GetTransformByName("P2"), false);
        UpdateTurnIndicatorPosition();
        "NotifyTossResult".Show();
        SnokerGameManager.bBallInHand = true;
        if (NetworkServer.active)
        {
            StickManager.instance.SetBallInHand(true);
        }
        StickManager.instance.CmdSetBallInHand(true);
        _SnokerUIManager.showNotification(_mainScript.playerNames[IsMyTurn() ? 0 : 1] + " has won the toss\nand will break first.", 2f);
        UpdateTimerDisplay();
        Holestrigger.enabled = true;
        _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(IsMyTurn() ? CAMERA_MODE.NORMAL : CAMERA_MODE.AI);
        isTossDone = true;
        SnookerFlow.Log($"{SnookerFlow.Who(num)} won the toss and will break");
    }

    public IEnumerator ChangeTurn()
    {
        yield return new WaitForEndOfFrame();
        //StickManager.instance.CurrentTurn = SnokerGameManager.currentTurn;
        StickManager.instance.NextTurn(SnokerGameManager.currentTurn);
    }

    public void NextTurn(string changeTurn)
    {
        SnokerGameManager.currentTurn = changeTurn;
        SnookerFlow.Log($"turn → {SnookerFlow.Who(changeTurn)}");
        _SnokerGameManager.isyourturn = IsMyTurn();
        UpdateTimerDisplay();
        this.Delay(1, () => _SnokerUIManager.showNotification(_mainScript.playerNames[IsMyTurn() ? 0 : 1] + " to Play"));
        _mainScript.switchControls();
        _mainScript.spinBtnObj.SetActive(_SnokerGameManager.isyourturn);
        _mainScript.thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
        StickManager.instance.RpcChangeturnrpc(SnokerGameManager.currentTurn, _mainScript.cueBallPotted, _mainScript.firstBallTouched, true, (int)SnokerGameManager.snookerTargetBall);
        _SnokerGameManager.ballIsStanding = true;
        _mainScript.igSnookerTurnIndicator.SetParent(IsMyTurn() ? GetTransformByName("P1") : GetTransformByName("P2"), false);
        UpdateTurnIndicatorPosition();
        _mainScript.AfterChangingTurnMultiplayer();
    }

    public void FetchCurrentTurnStatus(string changeTurn)
    {
        SnokerGameManager.currentTurn = changeTurn;
        _SnokerGameManager.isyourturn = IsMyTurn();
        UpdateTimerDisplay();

        _mainScript.thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
        _mainScript.igSnookerTurnIndicator.SetParent(IsMyTurn() ? GetTransformByName("P1") : GetTransformByName("P2"), false);
        UpdateTurnIndicatorPosition();
        _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(_SnokerGameManager.isyourturn ? _SnokerGameManager._SnokerCameraManager.cameraMode : CAMERA_MODE.AI);
        _mainScript.SetupPlayerControls(MODE_TYPE.Multiplayer);
        _mainScript.HandlePlayerTurns(MODE_TYPE.Multiplayer);
        if ((_SnokerGameManager.isyourturn))
        {
            _mainScript.HandleAutoAim();
        }
        _mainScript.ResetTurnState();
        //cueBallPotted = false;

    }



    private void UpdateTurnIndicatorPosition()
    {
        SnokerGameManager.currentTurn.Show("Current Turn indicator");
        staticVariables.UserProfiledata.user._id.ToString().Show("My profile id incicator");
        if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            _mainScript.igSnookerTurnIndicator.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 180, 0f);
            _mainScript.igSnookerTurnIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(32.6f, 0);
        }
        else
        {
            _mainScript.igSnookerTurnIndicator.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0f);
            _mainScript.igSnookerTurnIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(-32.6f, 0);
        }
    }
    Transform GetTransformByName(string objectName)
    {
        foreach (TextMeshProUGUI tmp in _mainScript.snookerScoresText)
        {
            if (tmp.gameObject.name == objectName)
            {
                return tmp.transform;
            }
        }
        return null; // Return null if the object wasn't found
    }

    public void RPC_Changeturnrpc(string turn, bool cueBallPotted, bool firstballTouched, bool TurnChanged, int val)
    {
        _SnokerGameManager.ballIsStanding = true;
        _mainScript.thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
        _mainScript.snookerSetTargetBAll(val);
        waitforballsync(turn, cueBallPotted, firstballTouched, TurnChanged);
    }

    void waitforballsync(string turn, bool cueBallPotted, bool firstballTouched, bool TurnChanged)
    {
        if (!firstballTouched && !cueBallPotted)
        {
            if (_SnokerUIManager != null)
            {
                _SnokerUIManager.showNotification("Missed!\n", 1.5f);
            }
        }
        Physics.RebuildBroadphaseRegions(new Bounds(Vector3.zero, Vector3.one * 1000f), 128);

        Physics.SyncTransforms();
        Physics.simulationMode = SimulationMode.Script;
        Physics.Simulate(Time.fixedDeltaTime);
        Physics.simulationMode = SimulationMode.FixedUpdate;
        foreach (GameObject ball in _SnokerGameManager.ballsArray)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            Collider col = ball.GetComponent<Collider>();
            ball.transform.hasChanged = false; // Clear transform change flag

            if (col != null)
            {
                col.enabled = false;
                col.enabled = true;
            }
            rb.WakeUp();
            rb.position = rb.position;

            rb.isKinematic = !rb.isKinematic;
            rb.isKinematic = !rb.isKinematic;

        }
        SnokerGameManager.currentTurn = turn;
        if (cueBallPotted) _mainScript.resetCueBallToStart();
        _SnokerGameManager.isyourturn = IsMyTurn();
        _SnokerGameManager._SnokerUIManager.cameraButtonObj.SetActive(IsMyTurn());
        _mainScript.cueBallPotted = cueBallPotted;
        if (cueBallPotted)
        {
            mainScript.foulInThisTurn = true;
        }
        if (_SnokerGameManager.isyourturn)
        {
            CueBallStartingpos = _mainScript.transform.position;
        }
        SnookerFlow.Log(TurnChanged ? $"turn → {SnookerFlow.Who(turn)}" : $"turn continues — {SnookerFlow.Who(turn)} plays again");
        if (TurnChanged)
            this.Delay(1, () => _SnokerUIManager.showNotification(_mainScript.playerNames[IsMyTurn() ? 0 : 1] + " to Play"));
        _mainScript.switchControls();

        _mainScript.spinBtnObj.SetActive(_SnokerGameManager.isyourturn);
        UpdateTimerDisplay();
        _mainScript.igSnookerTurnIndicator.SetParent(IsMyTurn() ? GetTransformByName("P1") : GetTransformByName("P2"), false);
        UpdateTurnIndicatorPosition();
        _mainScript.AfterChangingTurnMultiplayer();

    }

    public void OnScoreSync(int creatorscore, int joinerscore)
    {
        NetworkGameManager.Instance.creatorData.Scores = creatorscore;
        NetworkGameManager.Instance.joinerData.Scores = joinerscore;
        if (MirrorNetwork.Instance.isMasterClient)
        {
            _mainScript.snookerScoresText[0].text = string.Empty + creatorscore;
            _mainScript.snookerScoresText[1].text = string.Empty + joinerscore;
        }
        else
        {
            _mainScript.snookerScoresText[1].text = string.Empty + creatorscore;
            _mainScript.snookerScoresText[0].text = string.Empty + joinerscore;
        }
    }

    public float elapsedTime = 0f;
    public float turnDuration = 10f;
    public Coroutine TimerCorotine;
    public Coroutine WaitingTimerCorotine;

    public void SetBallInHandUI(bool ballInHand)
    {
        SnokerGameManager.bBallInHand = ballInHand;
        _mainScript.toggleSelectedCue(!ballInHand);

        if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            if (ballInHand)
            {
                _mainScript.goToBallInHand();

            }
            else
            {
                _mainScript.onClickPlaceCueOkBtn();

            }
        }
        if (ballInHand)
            _mainScript.resetCueBallToStart();

    }
    public void StartTurnTimer()
    {
        if (TimerCorotine != null)
            StopCoroutine(TimerCorotine);
        turnDuration = 30;
        StickManager.instance.isTimerRunning = true;
        TimerCorotine = StartCoroutine(TurnTimer());
    }

    public IEnumerator TurnTimer(float elapsedTimeParam = 0f)
    {
        Debug.LogError("Turn Timer Started");
        StickManager.instance.turnTimer = elapsedTimeParam;
        while (StickManager.instance.turnTimer < turnDuration)
        {
            if (!_SnokerGameManager.bGamePaused && _SnokerGameManager.ballIsStanding && NetworkGameManager.Instance.currentPlayerCount > 1 && StickManager.instance.isTimerRunning)
            {
                StickManager.instance.turnTimer += Time.deltaTime;
            }

            float fillAmount = 1f - (StickManager.instance.turnTimer / turnDuration);
            fillAmount = Mathf.Clamp01(fillAmount);
            if (_mainScript.playerOneTimer.gameObject.activeInHierarchy)
            {
                changeUI(_mainScript.playerOneTimer, fillAmount);
            }
            else
            {
                changeUI(_mainScript.playerTwoTimer, fillAmount);

            }
            yield return null;
        }
        // TIMEOUT AUTO-HIT IS SERVER-ONLY. This coroutine runs on every peer (clients need it for the timer
        // fill UI), so the expiry used to fire ExecuteBallHit LOCALLY on the client too: the client resolved a
        // full "missed" turn on its own, its RpcExecuteBallHit was dropped by Mirror ("called without an active
        // server"), and moments later the server's real broadcast resolved the SAME miss a second time. Two
        // turn rotations on one client = both players looking at an active cue ("stick dono side"). Clients now
        // just let the bar empty and wait for the server's RpcExecuteBallHit, which was always the one that
        // actually reached both sides.
        if (!NetworkServer.active)
        {
            Debug.Log("[SnokerNetwork] Turn timer expired on a client — waiting for the server's timeout hit (client no longer fires it locally).");
            yield break;
        }
        SnookerFlow.Log($"turn timer ran out for {SnookerFlow.Who(SnokerGameManager.currentTurn)}");
        StickManager.instance.ExecuteBallHit(0f,
                _mainScript.cueParentObjTransform.forward, _mainScript.guideDirCueBallTrans.forward, _mainScript.transform.position,
                _mainScript.lastTargetVector, _SnokerGameManager.GetCurrentTargetAsInt(),
                _mainScript.cueParentObjTransform.position, _mainScript.cueObjectTransform.localEulerAngles, _mainScript.cueParentObjTransform.eulerAngles);
    }
    private void Update()
    {
        float fillAmount = 1f - (StickManager.instance.turnTimer / 20);
        fillAmount = Mathf.Clamp01(fillAmount);
        if (_mainScript.playerOneTimer.gameObject.activeInHierarchy)
        {
            changeUI(_mainScript.playerOneTimer, fillAmount);
        }
        else
        {
            changeUI(_mainScript.playerTwoTimer, fillAmount);

        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StickManager.instance.turnTimer = 19f;
            AppealForWin("Manual");

        }
    }


    public void changeUI(Image image, float fillAmount)
    {
        if (image != null)
        {
            image.fillAmount = fillAmount;
        }
    }


    public void RpcOnClickCuePlaced()
    {
        if (_SnokerGameManager.ballIsStanding)
        {
            _mainScript.toggleSelectedCue(true);
            _mainScript.showGuideWithType(GUIDE_TYPE.NO);
            SnokerGameManager.bBallInHand = false;
            if (IsMultiplayer)
            {
                if (NetworkServer.active)
                {
                    StickManager.instance.SetBallInHand(false);
                }
                _mainScript.doAutoTarget();

            }
            _mainScript.ballInHandIndicatorObj.SetActive(false);
            if ((IsMultiplayer && NetworkServer.active) || !IsMultiplayer)
                _mainScript.setAllBallKinematic(false, 99);
            _mainScript.thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
        }
    }

    public IEnumerator TaskToDo(Action method, float delay)
    {
        yield return new WaitForSeconds(delay);
        method?.Invoke();
    }


    private IEnumerator checkPingAfterDelay()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);
            double pingMs = NetworkTime.rtt * 1000;
            if (!ResultManager.isGameFinished)
            {
                if (pingMs > 350 /*&& PunNetwork.instance.currentState == PunNetwork.ConnectionState.Connected*/) // Changed from PhotonNetwork.GetPing()
                {
                    PopupMessageManager.instance.SetSlowInternetPanel(true);
                }
                else
                {
                    PopupMessageManager.instance.SetSlowInternetPanel(false);
                }
            }
            else
            {
                PopupMessageManager.instance.SetSlowInternetPanel(false);

            }
        }
    }

    public Coroutine CameraEffect;
    public Vector3 CueBallStartingpos;


    public void RpcExecuteBallHit(float shotPower, Vector3 direction, Vector3 cueBallReboundVector, Vector3 cueballpos, Vector3 lastTargetVector, int TargetBall, Vector3 cuestickpos, Vector3 x, Vector3 y)
    {
        //if (StickManager.instance.isTimerRunning)
        {
            Debug.Log("Client Rpc Execution.");//test kr le log a rha k nhi aur extra logs hata yar build cy.

            if (SnokerGameManager.bGameOver)
            {
                return;
            }
            if (_mainScript.closestPot)
            {
                if (IsMyTurn())
                {
                    _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.POCKET);
                    CameraEffect = StartCoroutine(TaskToDo(() =>
                    {
                        if (!_SnokerGameManager.ballIsStanding)
                            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.TOP);
                    }, 3f));
                }
            }
            else
            {
                if (IsMyTurn())
                {
                    CameraEffect = StartCoroutine(TaskToDo(() =>
                    {
                        if (!_SnokerGameManager.ballIsStanding)
                            _SnokerGameManager._SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.TOP);
                    }, 1f));
                }
            }
            _SnokerGameManager.ballIsStanding = false;
            _SnokerGameManager._SnokerCueBall.snookerFirstTouchedBallNum = 0;
            _SnokerGameManager._SnokerCueBall.snookerBallInvolvedInFoul = 0;
            _SnokerGameManager._SnokerCueBall.snookerPointsCurShot = 0;
            if (_SnokerGameManager._SnokerCueBall.guideType != GUIDE_TYPE.NO)
            {
                _SnokerGameManager._SnokerCueBall.showGuideWithType(GUIDE_TYPE.NO);
            }
            _SnokerGameManager._SnokerCueBall.toggleSelectedCue(false);
            _mainScript.cueParentObjTransform.position = cuestickpos;
            _mainScript.cueObjectTransform.localEulerAngles = x;
            _mainScript.cueParentObjTransform.eulerAngles = y;
            CueBallStartingpos = cueballpos;

            _mainScript.transform.position = cueballpos;
            SnokerGameManager.snookerTargetBall = (SnookerTargetType)TargetBall;
            _mainScript.lastTargetVector = lastTargetVector;
            _SnokerGameManager.ballIsStanding = false;



            if (SnokerGameManager.bBallInHand)
            {
                _mainScript.onClickPlaceCueOkBtn();
            }
            _mainScript.strikeCount++;
            SnookerFlow.Shot(_mainScript.strikeCount, shotPower);

            _mainScript.canRePlaceCueBall = false;
            _mainScript.placeCueBtnObj.SetActive(false);
            _mainScript.spinBtnObj.SetActive(false);
            _mainScript.guiBallDisplayObj.SetActive(false);
            _mainScript.cueBallPosOnHit = _mainScript.thisTransform.position;
            _mainScript.aimColRingPosOnHit = _mainScript.guideColRingPosVec;
            _mainScript.cueBallReboundVector = cueBallReboundVector;

            if (shotPower > 0)
                _mainScript.playSoundFX(_mainScript.cueballHitSounds[UnityEngine.Random.Range(0, _mainScript.cueballHitSounds.Length)], 1f);
            _mainScript.toggleSelectedCue(false);
            _mainScript.showGuideWithType(GUIDE_TYPE.NO);
            _mainScript.ballPottedInThisTurn = false;
            mainScript.foulInThisTurn = false;
            _mainScript.cueBallPotted = false;
            _mainScript.firstBallTouched = false;
            _mainScript.collidedWithSide = false;
            _mainScript.cueRailHit = false;
            _mainScript.spinApply = false;
            _mainScript.snookerFirstTouchedBallNum = 0;
            _mainScript.snookerBallInvolvedInFoul = 0;
            _mainScript.snookerPointsCurShot = 0;
            if (_SnokerGameManager.GetCurrentTargetAsInt() != 1 && _mainScript.checkFixedUpdateBallTouch)
            {
                _mainScript.snookerNominatedBall = int.Parse(_mainScript.firstTargetBallToHit.name) - 14;
            }
            mainScript.railHitCountInThisShot = 0;
            mainScript.railHitBallArray = new int[21];
            if (_mainScript.controlMode[0] == CONTROLS.SET_POWER)
            {
                _mainScript.controlSetPowerLastPower = shotPower;
            }
            _mainScript.showPowerMeter(false);
            mainScript.spinSetOn = false;
            _mainScript.spinControlGroupAnimScript.hideSpinControl(false);
            _mainScript.hideBottomBlinkingText();
            if (shotPower == 0 && SnokerGameManager.bBallInHand) _mainScript.cueBallPotted = true;
            _mainScript.okBtnObj.SetActive(false);
        }
    }
    public void ExecuteHit(float shotPower, Vector3 direction, Vector3 cueBallReboundVector, Vector3 cueballpos, Vector3 lastTargetVector, int TargetBall, Vector3 cuestickpos, Vector3 x, Vector3 y)
    {
        _SnokerGameManager._SnokerCueBall.toggleSelectedCue(false);
        _mainScript.toggleSelectedCue(false);
        _mainScript.showGuideWithType(GUIDE_TYPE.NO);
        if (StickManager.instance.isTimerRunning)
        {
            StickManager.instance.isTimerRunning = false;
            print("Execute Hit Called");
            _SnokerGameManager.ballIsStanding = false;
            _SnokerGameManager._SnokerCueBall.snookerFirstTouchedBallNum = 0;
            _SnokerGameManager._SnokerCueBall.snookerBallInvolvedInFoul = 0;
            _SnokerGameManager._SnokerCueBall.snookerPointsCurShot = 0;
            if (_SnokerGameManager._SnokerCueBall.guideType != GUIDE_TYPE.NO)
            {
                _SnokerGameManager._SnokerCueBall.showGuideWithType(GUIDE_TYPE.NO);
            }
            _mainScript.onBallStopped += _mainScript.OnBallStand;
            _mainScript.cueParentObjTransform.position = cuestickpos;
            _mainScript.cueObjectTransform.localEulerAngles = x;
            _mainScript.cueParentObjTransform.eulerAngles = y;
            CueBallStartingpos = cueballpos;
            // if(HasStateAuthority)
            StartCoroutine(_mainScript.HandleBallStandingCheck());

            _mainScript.transform.position = cueballpos;
            SnokerGameManager.snookerTargetBall = (SnookerTargetType)TargetBall;
            _mainScript.lastTargetVector = lastTargetVector;
            _SnokerGameManager.ballIsStanding = false;
            _mainScript.thisRigidbody.isKinematic = false;
            _mainScript.thisRigidbody.linearVelocity = Vector3.zero;
            _mainScript.thisRigidbody.linearVelocity = direction * 10 * shotPower;

            if (SnokerGameManager.bBallInHand)
            {
                _mainScript.onClickPlaceCueOkBtn();
            }
            _mainScript.strikeCount++;
            SnookerFlow.Shot(_mainScript.strikeCount, shotPower);

            _mainScript.canRePlaceCueBall = false;
            _mainScript.placeCueBtnObj.SetActive(false);
            _mainScript.spinBtnObj.SetActive(false);
            _mainScript.guiBallDisplayObj.SetActive(false);

            _mainScript.cueBallPosOnHit = _mainScript.thisTransform.position;
            _mainScript.aimColRingPosOnHit = _mainScript.guideColRingPosVec;
            _mainScript.cueBallReboundVector = cueBallReboundVector;
            _mainScript.checkFixedUpdateBallTouch = true;
            _mainScript.velocityOnHit = _mainScript.thisRigidbody.linearVelocity.magnitude;
            if (Physics.SphereCast(_mainScript.thisTransform.position, _mainScript.ballRadius, direction, out _mainScript.lineHit, 100f, _mainScript.ballsLayerMask))
            {
                _mainScript.checkFixedUpdateBallTouch = true;
                _mainScript.velocityOnHit = _mainScript.thisRigidbody.linearVelocity.magnitude;
                _mainScript.firstTargetBallToHit = _mainScript.lineHit.collider.gameObject;
                _mainScript.angleOnHit = Vector3.Angle(direction, -_mainScript.lineHit.normal);
                _mainScript.cueBallToAimColRingDistanceOnHit = Vector3.Distance(_mainScript.cueBallPosOnHit, _mainScript.aimColRingPosOnHit);
                _mainScript.cueBallHitVector = direction;
            }
            else
            {
                _mainScript.checkFixedUpdateBallTouch = false;
            }
            if (shotPower > 0)
                _mainScript.playSoundFX(_mainScript.cueballHitSounds[UnityEngine.Random.Range(0, _mainScript.cueballHitSounds.Length)], 1f);

            _mainScript.ballPottedInThisTurn = false;
            mainScript.foulInThisTurn = false;
            _mainScript.cueBallPotted = false;
            _mainScript.firstBallTouched = false;
            _mainScript.collidedWithSide = false;
            _mainScript.cueRailHit = false;
            _mainScript.spinApply = false;
            _mainScript.snookerFirstTouchedBallNum = 0;
            _mainScript.snookerBallInvolvedInFoul = 0;
            _mainScript.snookerPointsCurShot = 0;
            if (_SnokerGameManager.GetCurrentTargetAsInt() != 1 && _mainScript.checkFixedUpdateBallTouch)
            {
                _mainScript.snookerNominatedBall = int.Parse(_mainScript.firstTargetBallToHit.name) - 14;
            }
            mainScript.railHitCountInThisShot = 0;
            mainScript.railHitBallArray = new int[21];
            if (_mainScript.controlMode[0] == CONTROLS.SET_POWER)
            {
                _mainScript.controlSetPowerLastPower = shotPower;
            }
            _mainScript.showPowerMeter(false);
            mainScript.spinSetOn = false;
            _mainScript.spinControlGroupAnimScript.hideSpinControl(false);
            _mainScript.hideBottomBlinkingText();
            if (shotPower == 0 && SnokerGameManager.bBallInHand) _mainScript.cueBallPotted = true;
            _mainScript.okBtnObj.SetActive(false);
            StickManager.instance.RpcExecuteBallHit(shotPower, direction, cueBallReboundVector, cueballpos,
           lastTargetVector, TargetBall, cuestickpos, x, y);
        }
    }
    public void ChangeScoreText(string toPlayer, int val)
    {
        StickManager.instance.ChangeScoreText(toPlayer, val);
    }

    public void RpcChangeScoreText(string toPlayer, int val)
    {
        if (NetworkGameManager.Instance.creatorData.playerId == toPlayer)
            NetworkGameManager.Instance.creatorData.Scores += val;
        if (NetworkGameManager.Instance.joinerData.playerId == toPlayer)
            NetworkGameManager.Instance.joinerData.Scores += val;
        if (val != 0) SnookerFlow.Log($"score updated: {SnookerFlow.Who(toPlayer)} +{val} → {SnookerFlow.Scores()}");
        StickManager.instance.RpcUpdateScoreText(NetworkGameManager.Instance.creatorData.Scores, NetworkGameManager.Instance.joinerData.Scores);

    }

    public void RpcCueBallPotted()
    {
        _mainScript.CueBallPotted();
    }

    public void RpcBallPotted(int ballnumber, int targetBall)
    {
        _mainScript.holesTriggerOnEnter(ballnumber, targetBall);
    }


    public void RpcSetBallInHand(bool ballInHand)
    {
        SnokerGameManager.bBallInHand = ballInHand;
    }



    public void RpcShowNotification(string text, bool isFoul)

    {
        _SnokerUIManager.showNotification(text);
        mainScript.foulInThisTurn = isFoul;
    }



    public void QuitApplication()
    {
        Application.Quit();
    }

    #endregion

    #region Private Methods

    void LoadArena()
    {
        //if (!Object.HasStateAuthority) // Changed from PhotonNetwork.IsMasterClient
        //{
        //    return;
        //}

        //Debug.LogFormat("Fusion Network : Loading Level : {0}", _runner.ActivePlayers.Count()); // Changed from PhotonNetwork.CurrentRoom.PlayerCount
    }

    #endregion

    #region Win Lose Functions


    public void AppealForWin(string reason)
    {
        AnnounceVictory(reason);
    }

    public void AnnounceVictory(string reason)
    {
        StickManager.instance.CmdAnnounceVictory(staticVariables.UserProfiledata.user._id, reason);

    }
    public void Victory(int id, string reason)
    {
        if (TimerCorotine != null)
            StopCoroutine(TimerCorotine);
        if (ResultManager.isGameFinished == false)
        {
            PopupMessageManager.instance.SetPanelStaus(false, body: "You have been disconnected. The opponent is declared the winner.");
            PopupMessageManager.instance.waitingPanel.StopTimer();
            PopupMessageManager.instance.disconnectedPanel.PanelStatus(false);
            PopupMessageManager.instance.slowInternetPanel.PanelStatus(false);
            ResultManager.isGameFinished = true;
            _SnokerGameManager.scheduleGameOverWithNotif(reason, () =>
            {
                // ResultManagerForSnooker.instance.HandleGameResultAlt(true, id.ToString());
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");
                    if (enemyPrefab != null)
                    {
                        "3".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
                ConstantsData_M.Log(true + "1");
            });
        }
    }

    public void AnnounceDefeat()
    {
        if (ResultManager.isGameFinished == false)
        {
            PopupMessageManager.instance.SetPanelStaus(false, body: "You have been disconnected. The opponent is declared the winner.");
            PopupMessageManager.instance.waitingPanel.StopTimer();
            PopupMessageManager.instance.disconnectedPanel.PanelStatus(false);
            PopupMessageManager.instance.slowInternetPanel.PanelStatus(false);
            ResultManager.isGameFinished = true;
            _SnokerGameManager.scheduleGameOverWithNotif("You have been disconnected. The opponent is declared the winner.", () =>
            {
                // ResultManagerForSnooker.instance.HandleGameResultAlt(false, staticVariables.UserProfiledata.user._id.ToString());
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");
                    if (enemyPrefab != null)
                    {
                        "4".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(false, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            });
        }
    }

    public void AnnounceDraw(bool status, string body)
    {
        if (!status)
        {
            if (ResultManager.isGameFinished == false)
            {

                PopupMessageManager.instance.SetPanelStaus(false);
                PopupMessageManager.instance.waitingPanel.StopTimer();
                PopupMessageManager.instance.disconnectedPanel.PanelStatus(false);
                PopupMessageManager.instance.slowInternetPanel.PanelStatus(false);
                ResultManager.isGameFinished = true;
                _SnokerGameManager.scheduleGameOverWithNotif("Game ended in a draw. No winner this time.", () =>
                {
                    // ResultManagerForSnooker.instance.Draw();
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");
                        if (enemyPrefab != null)
                        {
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().Draw();
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                });
            }
        }
        else
        {
            // FusionNetwork._fusionRpcManager.DeclareDefeat("Unable to join");
        }
    }

    public void DirectResult(bool result)
    {
        if (result)
        {
            Victory(staticVariables.UserProfiledata.user._id, "Opponent has disconnected. You are declared the winner.");
        }
        else
        {
            AnnounceDefeat();
        }
    }

    public void RunRPCAfterInternet(Action action)
    {
        action?.Invoke();
        //this.Delay(1f, () =>
        //{
        //    this.DelayUntil(() => PunNetwork.instance.currentState == PunNetwork.ConnectionState.Connected && !_SnokerGameManager.bGamePaused && _runner.SessionInfo != null && _runner.IsConnectedToServer, () =>
        //    {
        //        action?.Invoke();
        //    });
        //});
    }

    //public void OnDisconnect(bool status, string body)
    //{
    //    PopupMessageManager.instance.SetDisconnectedPanel(status);
    //    _SnokerGameManager.PauseUnpauseGame(status);
    //}


    #endregion



}