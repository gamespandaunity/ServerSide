using BallPool;
using BallPool.AI;
using BallPool.Mechanics;
using DG.Tweening;
using Mirror;
using NetworkManagement;
using System;
using System.Collections;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;
//using Mirror;

public class GameManager : MonoBehaviour
{
    // public GameObject EightBallPoolNetworkManager;
    [FormerlySerializedAs("playerName")] public Text mainPlayerName;
    [FormerlySerializedAs("opponentName")] public Text opponentPlayerName;
    [FormerlySerializedAs("selectionBall_text")] public TextMeshProUGUI selectedBallText;
    [FormerlySerializedAs("mainPlayerUI")] public PlayerUI mainPlayerUI;
    [FormerlySerializedAs("otherPlayerUI")] public PlayerUI opponentPlayerUI;
    [FormerlySerializedAs("prize")] public Text prizeText;
    [FormerlySerializedAs("gameInfo")] public Text matchInfoText; //RAR
    [FormerlySerializedAs("gameInfoBg")] public GameObject matchInfoBackground; //RAR
    [FormerlySerializedAs("aiCountText")] public Text aiDifficultyText;
    [FormerlySerializedAs("playAgainMenu")] public PlayAgainMenu playAgainMenu;
    [FormerlySerializedAs("gameUIController")] public GameUIController gameUI;
    [FormerlySerializedAs("shotController")] public ShotController shotController;
    [FormerlySerializedAs("physicsManager")] public PhysicsHandeler physicsHandler;
    [FormerlySerializedAs("aiManager")] public BallPoolAIManager aiManager;
    [FormerlySerializedAs("timeController")] public TimeController timeManager;
    [FormerlySerializedAs("ballSelectionInfo")] public static string selectedBallInfo;
    [FormerlySerializedAs("Totalsolids")] public int totalSolids = 7;
    [FormerlySerializedAs("Totalstripes")] public int totalStripes = 7;
    [FormerlySerializedAs("lostDueToConnectionLost")] public GameObject connectionLostPanel;
    [FormerlySerializedAs("balls")] public Ball[] allBalls;
    public TextMeshProUGUI ping;

    private bool isPlayAgainMenuActive;
    private AightBallPoolGameManager poolGameManager;
    private bool isApplicationPaused;
    private int applicationPausedSeconds;
    bool hasHighlighterShown = false;
    public ShadowToggle shadowToggle;
    // public ConnectAndJoin ConnectAndJoin;     //Photon Removal
    public ShadowToggle ShadowToggle
    {
        get { return shadowToggle; }
        set { shadowToggle = value; }
    }
    public static GameManager instance;
    public Image opponentImage;
    public Sprite AISprite;

    void Awake()
    {
        instance = this;
        DataManager.SaveGameData();
        SetAICount();
        if (BallPoolGameLogic.playMode == BallPool.PlayMode.Replay)
        {
            enabled = false;
            shotController.enabled = false;
            ActivatePlayAgainMenu(false);

            physicsHandler.OnBallMotion += (int ballId, Vector3 position, Vector3 velocity, Vector3 angularVelocity) =>
                {
                    if (allBalls[ballId].inPocket)
                    {
                        allBalls[ballId].OnState(BallState.MoveInPocket);
                    }
                    else
                    {
                        allBalls[ballId].OnState(BallState.Move);
                    }
                };

            return;
        }
        // NetworkManager.network.OnNetwork += NetworkManager_OnNetworkEvent;

        poolGameManager = new AightBallPoolGameManager();
        poolGameManager.Initialize(physicsHandler, aiManager, allBalls);
        poolGameManager.maxPlayTime = timeManager.maxPlayTime;
        poolGameManager.OnEndTime += _OnGameEnd;
        poolGameManager.OnSetGameInfo += _OnSetGameInfo; //RAR
        poolGameManager.gameLogic = new AightBallPoolGameLogic();
    }

    public void Start()
    {
        bool isNetworkSession = PlayerPrefs.GetInt("EightballMultiplayer") == 1
            || NetworkClient.active || NetworkServer.active;
        if (isNetworkSession)
        {
            BallPoolGameLogic.playMode = BallPool.PlayMode.OnLine;
            GameModeManager.isAI = false;
            // OnWinCall is now handled solely by MyEightBallNetwork.AnnounceWinner, which
            // routes through CmdGameWin -> RpcGameWin -> ResultManager (the shared win/lose
            // result screen, same as every other Mirror game). Subscribing here as well
            // showed the legacy local playAgainMenu.winPanel on top of it — duplicate panel.
            // MirrorNetwork.OnWinCall += AnnounceVictory;
            // Show the total challenge amount INSTANTLY from the value carried over by the
            // challenge/join flow (staticVariables.currentPrize), so the bet/prize text isn't blank
            // during the ~1-2s the Prize SyncVar takes to arrive from the server. The DelayUntil
            // below then corrects it to the authoritative server value once it syncs.
            if (staticVariables.currentPrize > 0)
                prizeText.text = (staticVariables.currentPrize * 2).ToString();
            // Wait until the server has synced the prize before showing it. Prize is
            // still 0 for the first frames after joining, so reading it immediately
            // here printed "0"/blank and was never refreshed. Mirrors Snooker's
            // SnokerGameManager.updateCoinStatus() DelayUntil pattern.
            this.DelayUntil(() => NetworkGameManager.Instance != null && NetworkGameManager.Instance.Prize > 0, () =>
            {
                int prizeAmount = NetworkGameManager.Instance.Prize;
                staticVariables.currentPrize = prizeAmount;
                prizeText.text = (prizeAmount * 2).ToString();
                Debug.Log("Player Online Mode " + prizeAmount);
            });
        }
        if (GameModeManager.isAI) 
        {
            opponentImage.sprite = AISprite;
            prizeText.text = (staticVariables.currentPrize * 2).ToString();
            // gameCanvas.SetActive(false);
        }

            ActivatePlayAgainMenu(false);
        poolGameManager.OnCalculateAI += _OnCalculateAI;
       
        poolGameManager.OnSetPlayer += _OnSetPlayer;
        poolGameManager.OnSetAvatar += _OnSetAvatar;
        poolGameManager.OnSetActivePlayer += _OnSetActivePlayer;
        poolGameManager.OnGameComplite += _OnGameComplite;
        poolGameManager.OnSetActiveBallsIds += _OnSetActiveBallsIds;
        poolGameManager.OnEnableControl += (bool value) =>
        {
            shotController.ActivateControl(value);
        };
        shotController.EndAICalculation += _OnEndCalculateAI;
        shotController.BallSelected += ShotController_OnSelectBall;
        shotController.BallDeselected += ShotController_OnUnselectBall;
        if (!NetworkServer.active)

            physicsHandler.OnReplaySegmentSaved += PhysicsManager_OnSaveEndStartReplay;
        
        if (NetworkServer.active)
        {
            SpawnNetworkObject();
        }
        else
        {
            StartCoroutine(UpdateNetworkTime());
            Debug.Log("PlayerClientSide");
        }
        poolGameManager.Start();
        if (isNetworkSession)
        {
            // The server toss has not necessarily reached this client yet. Reading myTurn
            // here uses its default value and briefly tells both clients "Opponent Turn".
            // ApplyTurn/ShowTurnNotification displays the authoritative turn once it syncs.
            matchInfoText.text = "";
            matchInfoBackground.SetActive(false);
        }
        else
        {
            matchInfoText.text = (BallPoolPlayer.mainPlayer.myTurn ? "You are breaking" : "Opponent Turn") + "\nGood Luck!"; //f
            StartCoroutine(HideGameDetailsText(matchInfoText.text)); //RAR
        }
        if (selectedBallText.text != "")
        {
            StartCoroutine(ToggleBallSelectionText());
        }     
    }

    public void AnnounceVictory(string reason)
    {
        WinDueToUserDisconnect();

    }
    public GameObject EightBallPoolNetworkObject;
    public void SpawnNetworkObject()
    {
        var network = Instantiate(EightBallPoolNetworkObject);
        NetworkServer.Spawn(network);
    }
    void PhysicsManager_OnSaveEndStartReplay(string impulse)
    {
         //   NetworkManager.network.OnMadeTurn();
         Debug.Log("Calling Rpc For impulse" + impulse);
        if (BallPoolGameLogic.controlInNetwork)
        {
            if (shotController != null && shotController.cuePhysicsManager != null && shotController.cuePhysicsManager.IsInMove)
            {
                Debug.LogWarning("[8Ball][NetworkReplay] Ignored outgoing StartSimulate while local physics is already moving.");
                return;
            }
            shotController.GetLastShotStartSnapshot(out Vector3 cueBallPosition, out Vector3 cuePivotPosition,
                out float cuePivotLocalRotationY, out float cueVerticalLocalRotationX,
                out Vector2 cueDisplacementLocalPositionXY, out float cueSliderLocalPositionZ,
                out float force, out Vector3[] ballPositions);
            MyEightBallNetwork.Instance.StartSimulate(impulse, cueBallPosition, cuePivotPosition,
                cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY,
                cueSliderLocalPositionZ, force, ballPositions);
        }
    }
    private void SendToNetworkOnEnd()
    {
        // Destroyed-object guard: after the match ends the server-spawned MyEightBallNetwork is despawned
        // while this scene object can still tick — invoking a Cmd on it then throws inside Mirror's
        // SendCommandInternal every call (tester log 414fd35f: OnSendTime NRE right after "[Vivox] Left
        // match channel"). Unity's overloaded == makes a destroyed object read as null here.
        if (MyEightBallNetwork.Instance == null || !Mirror.NetworkClient.isConnected)
            return;
        Debug.LogWarning("SendToNetworkOnEnd");
        MyEightBallNetwork.Instance.OnSendTime(poolGameManager.playTime);
    }
    void ShotController_OnSelectBall()
    {
        if (BallPoolGameLogic.controlInNetwork)
        {
            MyEightBallNetwork.Instance.SelectBallPosition(shotController.mainCueBall.position);
        }
    }
    void ShotController_OnUnselectBall()
    {
        if (BallPoolGameLogic.isOnLine)
        {
            Debug.LogWarning("ShotController_OnUnselectBall" + BallPoolGameLogic.controlInNetwork);
            MyEightBallNetwork.Instance.SetBallPosition(shotController.mainCueBall.position);
        }
    }
    IEnumerator UpdateNetworkTime()
    {
        while (true)
        {
            if (BallPoolGameLogic.controlInNetwork && !shotController.isMoving)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                SendToNetwork();
            }
            else
            {
                yield return null;
            }
        }
    }
    private void SendToNetwork()
    {
        // Same destroyed-object guard as SendToNetworkOnEnd — this one runs on the 0.3s timer-push loop.
        if (MyEightBallNetwork.Instance == null || !Mirror.NetworkClient.isConnected)
            return;
        MyEightBallNetwork.Instance.OnSendTime(poolGameManager.playTime);
        if (shotController.IsCueModified)
        {
            MyEightBallNetwork.Instance.OnSendCueControl(shotController.cuePivotTransform.localRotation.eulerAngles.y, shotController.cueVerticalAlignment.localRotation.eulerAngles.x,
                new Vector2(shotController.cueDisplacementTransform.localPosition.x, shotController.cueDisplacementTransform.localPosition.y), shotController.cueSliderTransform.localPosition.z, shotController.cueForce);
        }
        if (shotController.BallStateChanged)
        {
            Debug.LogWarning("ballChanged");
            MyEightBallNetwork.Instance.OnMoveBall(shotController.mainCueBall.position);
        }
    }
    // Recomputes each player's active (remaining) balls -> the potted-ball UI trays AND the
    // on-the-black state (checkIsBlackInEnd / isBlack). Used by the reconnect restore: the
    // reconnect path runs UpdateActiveBalls() once synchronously, but the authoritative ball
    // positions + solids/stripes types arrive a moment later over async TargetRpcs, so that
    // first pass computed the trays on stale data and never refreshed. The server calls this
    // again at the END of the restore (after all data has landed) to fix the trays.
    public void RefreshActiveBallsUI()
    {
        activeBallsRefreshSeq++;
        StartCoroutine(RefreshActiveBallsUIWhenReady(activeBallsRefreshSeq));
    }

    private int activeBallsRefreshSeq;

    private IEnumerator RefreshActiveBallsUIWhenReady(int seq)
    {
        float timeout = 2.0f;
        while (seq == activeBallsRefreshSeq && timeout > 0.0f && !CanRefreshActiveBallsUI())
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (seq != activeBallsRefreshSeq || !CanRefreshActiveBallsUI())
            yield break;

        // Reconnect restore lands through multiple TargetRpcs/coroutines. Let the final ball
        // pocket flags and ball-type restore settle before rebuilding the trays.
        yield return null;

        if (seq != activeBallsRefreshSeq || !CanRefreshActiveBallsUI())
            yield break;

        poolGameManager.UpdateActiveBalls();

        yield return null;

        if (seq == activeBallsRefreshSeq && CanRefreshActiveBallsUI())
            poolGameManager.UpdateActiveBalls();
    }

    private bool CanRefreshActiveBallsUI()
    {
        return poolGameManager != null
            && poolGameManager.balls != null
            && poolGameManager.balls.Length > 0
            && AightBallPoolPlayer.mainPlayer != null
            && AightBallPoolPlayer.otherPlayer != null
            && AightBallPoolGameLogic.gameState != null;
    }

    // Latch for the lazy prize binding below (fires once per scene load).
    private bool prizeBound = false;

    void Update()
    {
        // Bind the total-prize display as soon as the server-synced Prize is available. Doing this
        // lazily here (with a latch) instead of only in Start fixes an order dependence: on one of
        // the two clients GameManager.Start can run BEFORE MyEightBallNetwork.OnStartClient has set
        // up the multiplayer state, so that client never registered the Start-time DelayUntil and
        // its prize stayed blank ("one side shows no prize coins at match start").
        if (!prizeBound && !GameModeManager.isAI && prizeText != null
            && NetworkGameManager.Instance != null && NetworkGameManager.Instance.Prize > 0)
        {
            staticVariables.currentPrize = NetworkGameManager.Instance.Prize;
            prizeText.text = (NetworkGameManager.Instance.Prize * 2).ToString();
            prizeBound = true;
        }

        if (NetworkClient.active && NetworkGameManager.Instance.IsPaused) return;
        poolGameManager.Update(Time.deltaTime);
        timeManager.UpdateTime(poolGameManager.playTime);
        if (BallPoolGameLogic.controlFromNetwork && !shotController.isMoving && !physicsHandler.IsInMove)
        {                                                                                                         //Photon Removal
            shotController.SyncWithNetwork();
        }

        // Keep the cue rig anchored on the cue ball whenever it is idle, on BOTH the
        // active player and the watcher, so the stick can never be left detached from the
        // ball. The pivot's world position is otherwise only set at discrete events
        // (shot end, ball-in-hand drop, ball-position restore); after a reconnect the
        // ball positions are restored but the pivot could be left at a stale spot, so the
        // stick floated mid-table away from the cue ball while the aim line still started
        // at the ball. Skipped while a shot is in flight (the cue retracts/strikes then)
        // and while the cue ball is being placed in hand.
        if (!shotController.isMoving && !physicsHandler.IsInMove && !shotController.isBallInHand
            && shotController.mainCueBall != null
            && shotController.mainCueBall.position.y > -0.1f)
        {
            // Only anchor to the cue ball while it is ON the table. If it is pocketed/below the
            // table (y <= -0.1) we must NOT drag the cue rig down with it — that pushed the stick
            // far below the table out of view ("stick not showing"). It re-anchors automatically
            // once the cue ball is respotted / placed back on the table.
            shotController.cuePivotTransform.position = shotController.mainCueBall.position;
        }


        if (shadowToggle != null)
        {
            if (shotController.isBallInHand)
            {
                shadowToggle.enabled = false;
            }
            else
            {
                shadowToggle.enabled = true;
            }
        }
        else
        {
            //Debug.Log("shadow toggle null");
        }




        //if (ping)
        //    ping.text = "Ping: " + PhotonNetwork.GetPing() + "ms,Region:" + PhotonNetwork.CloudRegion;
    }
    public void WinDueToUserDisconnect()
    {
        //PunNetwork.instance.HasWinnerBeenDeclared = true;
        //if (PhotonNetwork.NetworkClientState == ClientState.Joined)
        //{
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;                                     //Photon Removal
        //    //print("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
        //    PhotonNetwork.LeaveRoom();
        //}
        //PunNetwork.instance.isGameBegun = false;
      //  waitingForOpponentPanel.SetActive(false);
       // ConnectionRetryPanel.SetActive(false);

        // Mark game as finished so the 30-sec waiting timer doesn't start/overlap.
       // Win8Ball.EightisFinishTriggered = true;

        // Stop the waiting timer if it already started (race condition).
        if (PopupMessageManager.instance != null && PopupMessageManager.instance.waitingPanel != null)
            PopupMessageManager.instance.waitingPanel.StopTimer();

        AightBallPoolPlayer.mainPlayer.isWinner = true;
        AightBallPoolPlayer.otherPlayer.isWinner = false;
        AightBallPoolPlayer.mainPlayer.isDraw = false;
        AightBallPoolPlayer.otherPlayer.isDraw = false;
        playAgainMenu.winPanel.SetActive(true);

        Debug.Log("Calling from here win due to winner udpate");
    }

    public void LoseDueToInternetConnectionLost()
    {
        //PunNetwork.instance.HasWinnerBeenDeclared = true;
        //if (PhotonNetwork.NetworkClientState == ClientState.Joined)
        //{
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    //print("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);                             //Photon Removal
        //    PhotonNetwork.LeaveRoom();
        //}
        //PunNetwork.instance.isGameBegun = false;
        waitingForOpponentPanel.SetActive(false);
        ConnectionRetryPanel.SetActive(false);
        AightBallPoolPlayer.mainPlayer.isWinner = false;
        AightBallPoolPlayer.otherPlayer.isWinner = true;

        AightBallPoolPlayer.mainPlayer.isDraw = false;
        AightBallPoolPlayer.otherPlayer.isDraw = false;
        playAgainMenu.winPanel.SetActive(true);

        ConstantsData_M.Log("Calling from here lose due to");
    }
    public IEnumerator WaitForWinPanelDisplay()
    {
        yield return new WaitForSeconds(0.3f);
        if (AightBallPoolPlayer.mainPlayer.isWinner)
        {
            AightBallPoolPlayer.mainPlayer.isWinner = false;

        }
        yield return new WaitForSeconds(1f);
        playAgainMenu?.winPanel?.SetActive(true);
    }

    IEnumerator ToggleBallSelectionText()
    {
        if (selectedBallText.text != null || selectedBallText.text != "")
        {
            yield return new WaitForSecondsRealtime(0.5f);

            selectedBallText.text = "";
        }
    }

    public GameObject cue;
    public GameObject cueparent;
    public GameObject ballLine;
    public GameObject ballCheker;

    public void ActivateCueStick()
    {
        //print("cue enabled");
        cue.SetActive(true);
        cueparent.SetActive(true);
        ballLine.SetActive(true);
        ballCheker.SetActive(true);
    }

    private float elapsedTime = 0f;

    private bool isCounting = false;

    public void UpdateBallsState(int number)
    {
        for (int i = 0; i < allBalls.Length; i++)
        {
            allBalls[i].SetMechanicalState(number);
        }
    }


    void OnEnable()
    {
        // Own the "returning player after the match already ended" result HERE, not on
        // the networked MyEightBallNetwork. That object is despawned on disconnect and
        // never re-created for a lone reconnecting client (server session gone), so it
        // would miss OnDirectWinWithoutInternet and the Lose panel would never show.
        // GameManager is a scene object that DOES exist on the reconnecting client —
        // same idea as 12-Beads keeping its listener alive across the reconnect.
        MirrorNetwork.OnDirectWinWithoutInternet += HandleDirectResult;
    }

    void OnDisable()
    {
        MirrorNetwork.OnDirectWinWithoutInternet -= HandleDirectResult;
        //PunNetwork.OnLose -= LoseDueToInternetConnectionLost;
        //PunNetwork.OnAwaitingOpponent -= SetWaitingForOpponent;                 //Photon Removal
        // Subscription removed — see note in Start(). MyEightBallNetwork now owns OnWinCall.
        // MirrorNetwork.OnWinCall -= AnnounceVictory;

        //PhotonHandler.onWinner -= HandleLeftRoom;
        //PunNetwork.OnServerDraw -= AnnounceDraw;
        if (poolGameManager != null)
        {
            poolGameManager.OnDisable();
            poolGameManager = null;
        }
     //  EightBallPoolNetworkManager.Disable();
    }

    // Fired (via MirrorNetwork.GetChallengeStatus) when this player returns AFTER the
    // match already ended and the challenge is resolved — e.g. they came back past the
    // 30s window, the opponent already won, and the game server is gone (so there is no
    // MyEightBallNetwork to replay the result). Spawns the shared win/lose result
    // screen with the same guarded logic the other Mirror games use.
    public void HandleDirectResult(bool result)
    {
        if (ResultManager.GameSpawnedFinished) return;
        ResultManager.GameSpawnedFinished = true;
        if (PopupMessageManager.instance != null)
            PopupMessageManager.instance.waitingPanel?.StopTimer();
        GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
        if (prefab != null)
            Instantiate(prefab, Vector3.zero, Quaternion.identity)
                .GetComponent<ResultManager>()
                .HandleGameResultAltMultiplayer(result, staticVariables.UserProfiledata.user._id.ToString());
        else
            Debug.LogError("[8Ball] WinLoseGameManager prefab not found on reconnect!");
    }




    public GameObject ConnectionRetryPanel;
    public GameObject waitingForOpponentPanel;
    public Text waitingPannelText;
    public void AwaitingOpponent(bool status)
    {
        //HomeMenuManager.instance.WaitingForOpponent();

        waitingForOpponentPanel.SetActive(status);

        //isPause = status;
    }
    public Coroutine WaitingTimerCorotine;

    // public void SetWaitingForOpponent(bool state)
    //{
    //    if (state == true)
    //    {
    //        {
    //            PopupMessageManager.instance.SetWaitingPanel(true);
    //            //Photon Removal PunNetwork.instance.isPaused = true;
    //        }
    //    }
    //    else
    //    {
    //        if (WaitingTimerCorotine != null)
    //            StopCoroutine(WaitingTimerCorotine);
    //        PopupMessageManager.instance.SetWaitingPanel(false);
    //        //Photon Removal PunNetwork.instance.isPaused = false;
    //    }
    //}
    private void coroutine()
    {
        StartCoroutine(checkPingAfterDelay());

    }

    private IEnumerator checkPingAfterDelay()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);
            //if (PhotonNetwork.GetPing() > 350 && PunNetwork.instance.currentState == PunNetwork.ConnectionState.Connected)
            //{
            //    PopupMessageManager.instance.SetSlowInternetPanel(true);
            //    PunNetwork.instance.isPaused = true;
            //}                                                                                                                            //Photon Removal
            //else
            //{
            //    PopupMessageManager.instance.SetSlowInternetPanel(false);
            //    if (PunNetwork.instance.currentState == PunNetwork.ConnectionState.Connected)
            //        PunNetwork.instance.isPaused = false;
            //}
        }
    }
    public void AnnounceDraw(bool status, string body)
    {
        if (!status)
        {
            PopupMessageManager.instance.SetPanelStaus(false);
            GameWinnerMsg.IsGameFinished = true;
            AightBallPoolPlayer.mainPlayer.isDraw = true;
            AightBallPoolPlayer.otherPlayer.isDraw = true;
            playAgainMenu.winPanel.SetActive(true);
        }
        else
        {
            //Photon Removal  PunNetwork.instance.DeclareDefeat("Unable to join");
        }
    }

    public void LostConnection()
    {

        //Photon Removal if (!GameModeManager.isAI && PunNetwork.instance.isGameBegun && !PunNetwork.instance.HasWinnerBeenDeclared)
        {

            //Photon Removal   PunNetwork.instance.isPaused = true;
            ConnectionRetryPanel.SetActive(false);
        }

    }

    public void ConnectionReset()
    {
        //print("connection Reset pannel disable");
        waitingForOpponentPanel.SetActive(false);

    }



    public void SetAICount()
    {
        aiDifficultyText.text = ProductAI.aiCount + "";
    }
    private void ShowPlayAgainMenuForMainPlayer()
    {
        //Debug.Log("Calling Winner with room settings");
        shotController.shotBackImage.enabled = false;
        BallPoolPlayer.SetWinner(BallPoolPlayer.mainPlayer.playerId);
        playAgainMenu.ShowMainPlayer();

        mainPlayerUI.SetPlayer(AightBallPoolPlayer.mainPlayer);
        opponentPlayerUI.SetPlayer(AightBallPoolPlayer.otherPlayer);

    }
    bool leftBtn = false;




    [FormerlySerializedAs("playerTxtAIMode")] public Text playerTextAIMode; //RAR
    [FormerlySerializedAs("AITxtAIMode")] public Text AITextAIMode; //RAR

    private void ActivatePlayAgainMenu(bool isOn)
    {
        if (isOn)
        {
            if (!playAgainMenu.wasOpened)
            {
                playAgainMenu.winPanel.SetActive(true);
                //Debug.Log("Calling from here trigger paly again menu");
            }
        }
    }
    void _OnGameEnd()
    {
        if (BallPoolGameLogic.controlInNetwork)
        {
            SendToNetworkOnEnd();
        }
        shotController.TimeEnded();
        shotController.ResetShot();
        CircularTimeController.instance.audioSource.Stop();
        CircularTimeController.instance.audioSource.clip = null;
        CircularTimeController.instance.audioSource.loop = false;
    }

    [FormerlySerializedAs("gameInfoInProgress")] public bool gameProgressStatus = false;
    void _OnSetGameInfo(string info)
    {
        StartCoroutine(HideGameDetailsText(info));
    }

    IEnumerator HideGameDetailsText(string info) //RAR
    {
        StartCoroutine(ToggleBallSelectionText());
        while (gameProgressStatus)
        {
            yield return null;
        }

        if (info != "") //RAR
        {
            //print("test");

            gameProgressStatus = true;
            matchInfoBackground.SetActive(true);  //RAR
            matchInfoText.text = info;
        }
        yield return new WaitForSecondsRealtime(2.0f);
        matchInfoBackground.SetActive(false); //RAR
        matchInfoText.text = "";
        gameProgressStatus = false;

    }

    public void ShowBG()
    {
        if (matchInfoText.text != null && matchInfoText.text != "")
        {
            //gameInfoBg.SetActive(true);

            matchInfoBackground.transform.DOMove(new Vector3(-161.4167f, 5f, 0), 1.5f).SetRelative();



        }
        else
        {
            //gameInfoBg.SetActive(false);




            matchInfoBackground.transform.DOMove(new Vector3(-161.4167f, -25f, 0), 1f).SetRelative();
        }
    }

    public void HideBackground()
    {
        if (matchInfoText.text == null && matchInfoText.text == "")
        {
            matchInfoBackground.transform.DOMove(new Vector3(0f, 0, 0), 1f).SetRelative();

        }
    }



    public void DisplayPanel()
    {
        matchInfoBackground.transform.DOScale(new Vector3(1, 1, 1), 2f)
            .OnComplete(() => matchInfoBackground.transform.DOScale(new Vector3(0, 0, 0), 1f)
            .SetEase(Ease.Linear));

        Sequence mySequence = DOTween.Sequence();
        mySequence.AppendInterval(2f); // Add a 1-second delay

        //DOTween.Sequence().SetDelay(1f).Append(transform.DOPath(XXX).OnComplete(XXX)).AppendInterval(1f).SetLoops(-1, LoopType.Yoyo).Play();
    }


    public void ClosePanel()
    {
        matchInfoBackground.transform.DOScale(new Vector3(0, 0, 0), 1f)
            .OnComplete(() => matchInfoBackground.SetActive(true))
            .SetEase(Ease.Linear);
    }

    [FormerlySerializedAs("otherPlayerName")] public string otherPlayerName;
    public static string newOpponentName, newMainPlayerName;
    //void _OnSetPlayer(BallPoolPlayer player)
    //{
    //    if (player.playerId == staticVariables.UserProfiledata.user._id)
    //    {
    //        mainPlayerUI.SetPlayer(player);

    //    }
    //    else
    //    {
    //        opponentPlayerUI.SetPlayer(player);
    //        otherPlayerName = player.name;

    //    }



    //    //Photon Removal  Photon.Realtime.Player[] players = PhotonNetwork.PlayerList;

    //    // Loop through each player and //print their name
    //    //foreach (Photon.Realtime.Player playername in players)
    //    //{

    //    //    if (playername.UserId == PhotonNetwork.LocalPlayer.UserId)
    //    //    {
    //    //        mainPlayerName.text = playername.NickName;
    //                                                                                              //Photon Removal
    //    //        newMainPlayerName = playername.NickName;
    //    //    }
    //    //    else
    //    //    {

    //    //        opponentPlayerName.text = playername.NickName;
    //    //        newOpponentName = playername.NickName;
    //    //    }

    //    //}
    //}
    void _OnSetPlayer(BallPoolPlayer player)
    {
        // Complete null checks
        if (player == null)
        {
            Debug.LogError("[GameManager] Player is null in _OnSetPlayer");
            //return;
        }

        if (staticVariables.UserProfiledata == null || staticVariables.UserProfiledata.user == null)
        {
            Debug.LogError("[GameManager] UserProfiledata is null");
            //  return;
        }

        bool isMultiplayer = PlayerPrefs.GetInt("EightballMultiplayer") == 1;
        bool isMainPlayer = player.playerId == staticVariables.UserProfiledata.user._id;

        Debug.Log($"[GameManager] _OnSetPlayer - IsMultiplayer: {isMultiplayer}, IsMainPlayer: {isMainPlayer}, PlayerId: {player.playerId}");

        if (isMainPlayer)
        {
            if (mainPlayerUI != null)
            {
                mainPlayerUI.SetPlayer(player);
                Debug.Log($"[GameManager] Main player UI set");
            }

            //if (isMultiplayer && NetworkGameManager.Instance != null)
            //{
            //    SetMainPlayerNameFromNetwork();
            //}
        }
        else
        {
            // This is opponent player
            Debug.Log($"[GameManager] Setting opponent player - Name: {player.name}");

            if (opponentPlayerUI != null)
            {
                opponentPlayerUI.SetPlayer(player);
                otherPlayerName = staticVariables.OpponetProfile.userName;
                Debug.Log($"[GameManager] Offline - Opponent name: {otherPlayerName}");
                Debug.Log($"[GameManager] Opponent player UI set");

            }
            else
            {
                Debug.LogError("[GameManager] opponentPlayerUI is null!");
            }

            //if (isMultiplayer && NetworkGameManager.Instance != null)
            //{
            //    SetOpponentNameFromNetwork();
            //}
            //else if (!isMultiplayer)
            //{
            //    otherPlayerName = player.name;
            //    Debug.Log($"[GameManager] Offline - Opponent name: {otherPlayerName}");
            //}
        }
    }

    private IEnumerator AIShotPreparation()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        if (!shotController.isMoving && !shotController.isAIActionReady)
        {
            gameUI.ExecuteShot(false);
        }
    }
    void _OnSetActiveBallsIds(BallPoolPlayer player)
    {
        if (player.playerId == staticVariables.UserProfiledata.user._id)
        {
            mainPlayerUI.SetActiveBallsIds(player);
        }
        else
        {
            opponentPlayerUI.SetActiveBallsIds(player);
        }


    }

    void _OnGameComplite()
    {
        shotController.enabled = false;

        // ONLINE: announce the result over the network so BOTH players get it. Only the
        // player who took the deciding shot announces it (authoritative for that shot, and
        // still the active turn because the game ends with needToChangeTurn = false); the
        // opponent receives it via RpcGameWin -> shared ResultManager (winner -> Win,
        // loser -> Lose). Previously normal completion only showed a LOCAL play-again
        // panel and never told the opponent, so the opponent sat on the "waiting for
        // opponent" panel and after 30s wrongly declared ITSELF the winner. Don't also
        // show the legacy local panel online — it would duplicate the shared result
        // screen that the disconnect flow already uses.
        if (NetworkClient.active && MyEightBallNetwork.Instance != null)
        {
            BallPoolPlayer winner = BallPoolPlayer.GetWinner();
            if (BallPoolPlayer.mainPlayer != null && BallPoolPlayer.mainPlayer.myTurn && winner != null)
            {
                MyEightBallNetwork.Instance.CmdGameWin(winner.playerId);
            }
        }
        else
        {
            ActivatePlayAgainMenu(true);
        }
    }

    void _OnSetActivePlayer(BallPoolPlayer player, bool value) //RAR
    {


        //RAR
        if (player.playerId == staticVariables.UserProfiledata.user._id)
        {
            mainPlayerUI.SetActive(true);
        }
        else
        {
            opponentPlayerUI.SetActive(true);
        }
        //RAR
    }
    Coroutine cr = null;
    void _OnCalculateAI()
    {
        //Debug.Log("AightBallPoolGameManager_OnCalculate AI");

        shotController.enabled = false;
        if (cr != null)
        {
            StopCoroutine(cr);
        }
        cr = StartCoroutine("AIWaitAndCompute");
    }
    private IEnumerator AIWaitAndCompute()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        aiManager.CalculateAI();
    }

    void _OnSetAvatar(BallPoolPlayer player)
    {
    }

    void _OnEndCalculateAI()
    {
        StartCoroutine(AIShotPreparation());
    }
}
