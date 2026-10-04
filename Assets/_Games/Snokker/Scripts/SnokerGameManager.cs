using Mirror;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;
using UnityExtensions;
using UnityStandardAssets.ImageEffects;

public class SnokerGameManager : MonoBehaviour
{
    #region Static Vars
    public static bool bGameOver;
    #endregion
    #region Vars
    public SnookerPhase currentPhase = SnookerPhase.RedAndColor;
    public static SnookerTargetType snookerTargetBall = SnookerTargetType.Red;
    public static int snookerRedPottedCount;
    public int snookerRedsSelected = 15;
    public Action scheduledFunctionAfterNotif;
    public SnokerUIManager _SnokerUIManager;
    public SnokerCameraManager _SnokerCameraManager;
    public SnokerAIManager _SnokerAIManager;
    public mainScript _SnokerCueBall;
    public Camera cameraObjCamera;
    public static string currentTurn = "";
    public string gameWinner;
    public bool bTossDone, isyourturn, ballIsStanding;
    public static bool bBallInHand;
    public int ttGameStartTime;
    public float ballRadius = 0.03245f;
    public GameObject[] ballsArray = new GameObject[21];

    public Rigidbody[] ballsRigidbodyArray = new Rigidbody[21];

    public Vector3[] ballPositions = new Vector3[21];

    public GameObject cuesObjArray;

    public Transform[] holesTriggerPos;
    public TextMeshProUGUI challengeAmount, ping;
    public Image coinSprite, player1Sprite, player2Sprite;
    public TextMeshProUGUI countDownGameTimer;

    private void blurGameView(bool val)
    {
        (_SnokerCameraManager.cameraObjTransform.gameObject.GetComponent("BlurOptimized") as MonoBehaviour).enabled = val;
    }
    #endregion


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        snookerRedPottedCount = 0;
        bBallInHand = false;
        snookerRedPottedCount = 0;
        snookerTargetBall = SnookerTargetType.Red;
        mainScript.foulInThisTurn = false;
        setLayers();
        Time.fixedDeltaTime = 0.02f;
        Application.targetFrameRate = 30;
        storedVelocities = new Vector3[ballsRigidbodyArray.Length];
        storedAngularVelocities = new Vector3[ballsRigidbodyArray.Length];
        {
            updateCoinStatus();
        }
        if (SnokerNetwork.IsMultiplayer)
        {
            //if (_SnokerCueBall._snokerNetwork.IsMyOriginalMaster())
            //{
            player1Sprite.sprite = ConstantsData_M.ConvertTextureToSprite(staticVariables.ProfilePicture);
            player2Sprite.sprite = ConstantsData_M.ConvertTextureToSprite(staticVariables.opponentImage);
            //}
            //else
            //{
            //    player1Sprite.sprite = ConstantsData_M.ConvertTextureToSprite(staticVariables.opponentImage);
            //    player2Sprite.sprite = ConstantsData_M.ConvertTextureToSprite(staticVariables.ProfilePicture);
            //}

        }
        else
        {
            player1Sprite.sprite = ConstantsData_M.ConvertTextureToSprite(staticVariables.ProfilePicture);
            player2Sprite.sprite = ConstantsData_M.ConvertTextureToSprite(SpritesManager.Instance.spritesScriptable.aiImg);
        }
    }
    private void Update()
    {
        if (SnokerNetwork.IsMultiplayer)
        {
            // if (SnokerNetwork.Instance.isLocalPlayer)
            {
                if (ping)
                    ping.text = ((int)(NetworkTime.rtt * 1000)).ToString() + " ms";
            }
        }
    }
    private void OnApplicationFocus(bool focus)
    {
        //  PauseUnpauseGame(!focus);
    }
    private void OnApplicationPause(bool pause)
    {
        if (_SnokerCueBall.GameMode == MODE_TYPE.Multiplayer) return;
        PauseUnpauseGame(pause);
    }
    public bool bGamePaused;
    private Vector3[] storedVelocities;
    private Vector3[] storedAngularVelocities;
    private Vector3 cueBallVelocity;
    private Vector3 cueBallAngularVelocity;
    public void PauseUnpauseGame(bool pauseState)
    {
        _SnokerCameraManager.blurGameView(pauseState);
        if (pauseState)
        {
            PauseGame();
        }
        else
        {
            UnpauseGame();
        }
    }
    private void PauseGame()
    {
        Debug.Log("Game paused 1: " + bGamePaused);
        if (bGamePaused)
            return;
        bGamePaused = true;
        // Store cue ball velocity
        if (_SnokerCueBall != null && _SnokerCueBall.thisRigidbody != null)
        {
            cueBallVelocity = _SnokerCueBall.thisRigidbody.linearVelocity;
            cueBallAngularVelocity = _SnokerCueBall.thisRigidbody.angularVelocity;
            _SnokerCueBall.thisRigidbody.isKinematic = true;
        }

        // Store and pause all other balls
        for (int i = 0; i < ballsArray.Length; i++)
        {
            if (ballsArray[i].activeSelf && ballsRigidbodyArray[i] != null)
            {
                storedVelocities[i] = ballsRigidbodyArray[i].linearVelocity;
                storedAngularVelocities[i] = ballsRigidbodyArray[i].angularVelocity;
                ballsRigidbodyArray[i].isKinematic = true;
            }
        }
        Debug.Log("Game paused: " + bGamePaused);

    }

    private void UnpauseGame()
    {
        Debug.Log("Game Unpaused 1: " + bGamePaused);
        if (!bGamePaused)
            return; // Not paused, nothing to unpause      
        if (_SnokerCueBall != null && _SnokerCueBall.thisRigidbody != null)
        {
            _SnokerCueBall.thisRigidbody.isKinematic = false;
            _SnokerCueBall.thisRigidbody.linearVelocity = cueBallVelocity;
            _SnokerCueBall.thisRigidbody.angularVelocity = cueBallAngularVelocity;
        }

        // Restore all other balls
        for (int i = 0; i < ballsArray.Length; i++)
        {
            if (ballsArray[i].activeSelf && ballsRigidbodyArray[i] != null)
            {
                ballsRigidbodyArray[i].isKinematic = false;
                ballsRigidbodyArray[i].linearVelocity = storedVelocities[i];
                ballsRigidbodyArray[i].angularVelocity = storedAngularVelocities[i];
            }
        }
        bGamePaused = false;
        Debug.Log("Game Unpaused: " + bGamePaused);

    }


    public void updateCoinStatus()
    {
        if (SnokerNetwork.IsMultiplayer)
        {
            this.DelayUntil(() => NetworkGameManager.Instance && NetworkGameManager.Instance.Prize > 0, () =>
            {
                if (NetworkGameManager.Instance != null)
                {
                    Debug.Log("GOLD");
                    staticVariables.currentPrize = NetworkGameManager.Instance.Prize;
                    staticVariables.isgoldcoins = NetworkGameManager.Instance.IsGoldCoin;
                    challengeAmount.text = (NetworkGameManager.Instance.Prize * 2).ToString();
                    if (NetworkGameManager.Instance.IsGoldCoin)
                    {
                        challengeAmount.color = new Color(1f, 0.84f, 0f); // Golden color
                    }
                    else
                    {
                        challengeAmount.color = new Color(0.75f, 0.75f, 0.75f); // Silver color
                    }
                    coinSprite.sprite = NetworkGameManager.Instance.IsGoldCoin ? SpritesManager.Instance.spritesScriptable.goldenCoin : SpritesManager.Instance.spritesScriptable.silvercoin;
                }
            });
        }
        else
        {
            Debug.Log("GOLD");
            challengeAmount.text = (staticVariables.currentPrize * 2).ToString();
        }
    }
    #region Target Ball Logic

    public int GetActiveColorBall()
    {
        int r = 0;
        for (int i = 0; i < ballsArray.Length; i++)
        {
            if (ballsArray[i].activeSelf)
            {
                r = int.Parse(ballsArray[i].name) - 14;
                break;
            }
        }
        return r;
    }
    public void UpdateTargetBall()
    {
        if (_SnokerCueBall.ballPottedInThisTurn)
        {
            UpdateTargetAfterSuccessfulPot();
        }

        if ((!_SnokerCueBall.ballPottedInThisTurn || mainScript.foulInThisTurn) && GetCurrentTargetAsInt() == 99)
        {
            snookerTargetBall = (SnookerTargetType)Enum.Parse(typeof(SnookerTargetType), (AreAllRedsPotted() ? GetActiveColorBall() : 1).ToString());
        }
        _SnokerCueBall.snookerNominatedBall = 1;
        UpdateBallDisplay();
        if (_SnokerCueBall.GameMode == MODE_TYPE.Multiplayer)
        {
            _SnokerCueBall.thisRigidbody.isKinematic = true;
            _SnokerCueBall.thisRigidbody.linearVelocity = Vector3.zero;

            //_SnokerCueBall._snokerNetwork?.photonView.RPC("changeBall", RpcTarget.All,(int) snookerTargetBall);
        }
    }

    private void UpdateTargetAfterSuccessfulPot()
    {
        switch (GetCurrentTargetAsInt())
        {
            case 1: // Red
                snookerTargetBall = SnookerTargetType.AnyColor; // Any color
                break;
            case 99: // Any color
                snookerTargetBall = (SnookerTargetType)Enum.Parse(typeof(SnookerTargetType), (AreAllRedsPotted() ? 2 : 1).ToString());
                break;
            default: // Colored balls sequence
                if (!mainScript.foulInThisTurn)
                {
                    snookerTargetBall = (SnookerTargetType)Enum.Parse(typeof(SnookerTargetType), (Mathf.Min(GetActiveColorBall(), 7)).ToString());
                }
                break;
        }
    }

    public void UpdateBallDisplay()
    {
        int spriteIndex = GetCurrentTargetAsInt() == 99 ? 22 : GetCurrentTargetAsInt() + 14;
        _SnokerCueBall.igSnookerBallDisplayImg.sprite = _SnokerCueBall.guiBallsTex[spriteIndex];
    }

    #endregion


    public int GetCurrentTargetAsInt()
    {
        return (int)snookerTargetBall;
    }
    public bool AreAllRedsPotted()
    {
        if (snookerRedPottedCount >= snookerRedsSelected) currentPhase = SnookerPhase.ColorSequence;

        return snookerRedPottedCount >= snookerRedsSelected;
    }




    public bool IsValidTarget(int ballNumber)
    {
        //        Debug.Log(snookerTargetBall+": Target :" + ballNumber);
        switch (snookerTargetBall)
        {
            case SnookerTargetType.Red:
                return ballNumber >= 1 && ballNumber <= 15; // Red balls

            case SnookerTargetType.AnyColor:
                return ballNumber >= 16 && ballNumber <= 21; // Colored balls

            case SnookerTargetType.Yellow:
                return ballNumber == 16; // Yellow

            case SnookerTargetType.Green:
                return ballNumber == 17; // Green

            case SnookerTargetType.Brown:
                return ballNumber == 18; // Brown

            case SnookerTargetType.Blue:
                return ballNumber == 19; // Blue

            case SnookerTargetType.Pink:
                return ballNumber == 20; // Pink

            case SnookerTargetType.Black:
                return ballNumber == 21; // Black

            default:
                return false;
        }
    }

    #region EndGame
    public void scheduleGameOverWithNotif(string textVal)
    {
        bGameOver = true;
        scheduledFunctionAfterNotif += gameCompleteEvent;
        _SnokerUIManager.showNotification(textVal);
    }
    public void scheduleGameOverWithNotif(string textVal, Action onCompleteEvent)
    {
        bGameOver = true;
        scheduledFunctionAfterNotif += onCompleteEvent;
        _SnokerUIManager.showNotification(textVal, 10);
    }

    private void gameCompleteEvent()
    {
        SnokerGameManager.bGameOver = true;

        _SnokerUIManager.switchScreen("GameOver");
        if (SnokerNetwork.IsMultiplayer)
        {

            // ResultManagerForSnooker.instance.HandleGameResultAlt(true, gameWinner);
            if (ResultManager.GameSpawnedFinished == false)
            {
                ResultManager.GameSpawnedFinished = true;
                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                if (enemyPrefab != null)
                {
                    "1".Show();
                    // Spawn at position (0,0,0)
                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                    gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, gameWinner);
                }
                else
                {
                    Debug.LogError("WinLose GameManager prefab not found!");
                }
            }
        }
        else
        {

            gameWinner.Show();
            //  ResultManagerForSnooker.instance.HandleGameResultAlt(gameWinner != "ai", staticVariables.UserProfiledata.user._id.ToString());
            if (ResultManager.GameSpawnedFinished == false)
            {
                ResultManager.GameSpawnedFinished = true;
                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");
                if (enemyPrefab != null)
                {
                    // Spawn at position (0,0,0)
                    "2".Show();
                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                    gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(gameWinner != "ai", staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    Debug.LogError("WinLose GameManager prefab not found!");
                }
            }
        }
        //if (modeType == MODE_TYPE.TWO_PLAYER || (modeType == MODE_TYPE.SINGLE_PLAYER && gameWinner == TURN.PLAYER_1))
        //{
        //    Invoke("playGameWinSoundInvoke", 0.7f);
        //}

        _SnokerUIManager.blurGameView(true);
    }

    #endregion

    public void gameStartAfterToss()
    {
        Debug.LogError("game" +
            "Start" +
            "after" +
            "test");
        bTossDone = true;
        ttGameStartTime = (int)Time.time;
        if (SnokerNetwork.IsMultiplayer)
        {
            isyourturn = currentTurn == staticVariables.UserProfiledata.user._id.ToString();

            _SnokerCueBall.switchControls();
            isyourturn.Show();
            if (isyourturn)
            {
                _SnokerCueBall.goToBallInHand();
            }
            //else
            //{
            //    bBallInHand = false;
            //}
            _SnokerCameraManager.cameraSwitchMode(SnokerNetwork.Instance.IsMyTurn() ? CAMERA_MODE.NORMAL : CAMERA_MODE.AI);

        }
        else
        {
            if (currentTurn == staticVariables.UserProfiledata.user._id.ToString())
            {
                _SnokerCueBall.goToBallInHand();
                _SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.NORMAL);

            }
            if (currentTurn == "ai")
            {
                Screen.sleepTimeout = -1;
                bBallInHand = true;

                if (SnokerNetwork.IsMultiplayer && NetworkServer.active)
                {
                    StickManager.instance.SetBallInHand(true);
                }
                _SnokerCameraManager.cameraSwitchMode(CAMERA_MODE.AI);
                _SnokerAIManager.aiStart();
            }

        }


    }
    private static System.Random random = new System.Random();

    public static string GenerateId(int length = 4)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] id = new char[length];
        for (int i = 0; i < length; i++)
        {
            id[i] = chars[random.Next(chars.Length)];
        }
        return new string(id);
    }
    public int getRandomOneOrMinusOne()
    {
        return UnityEngine.Random.Range(1, 3) * 2 - 3;
    }
    public void setLayers()
    {
        // Layer indices based on your Unity layer setup
        int defaultLayer = 0;
        int transparentFX = 1;
        int ignoreRaycast = 2;
        int water = 4;
        int ui = 5;
        int cueBall = 8;
        int tableCoLayer = 9;
        int ballsLayer = 10;
        int cueLayer = 11;
        int boundingBoxLayer = 12;
        int tableMeshLayer = 13;
        int ballsTouchLayer = 14;

        // First, ignore collisions between all layers (set everything to ignore)
        for (int i = 0; i <= 31; i++)
        {
            for (int j = i; j <= 31; j++)
            {
                SetIgnore(i, j, true);
            }
        }

        // Now enable only the specific collisions that have checkmarks in your matrix

        // Default layer collides with: cueBall, tableCoLayer, ballsLayer, cueLayer, boundingBoxLayer, tableMeshLayer, ballsTouchLayer
        SetIgnore(defaultLayer, cueBall, false);
        SetIgnore(defaultLayer, ballsLayer, false);
        SetIgnore(defaultLayer, ballsTouchLayer, false);

        SetIgnore(transparentFX, ballsTouchLayer, false);
        SetIgnore(ignoreRaycast, ballsTouchLayer, false);
        SetIgnore(water, ballsTouchLayer, false);
        SetIgnore(ui, ballsTouchLayer, false);

        SetIgnore(cueBall, ballsTouchLayer, false);
        SetIgnore(cueBall, ballsLayer, false);
        SetIgnore(cueBall, tableCoLayer, false);

        SetIgnore(tableCoLayer, ballsTouchLayer, false);
        SetIgnore(tableCoLayer, ballsLayer, false);

        SetIgnore(ballsLayer, ballsTouchLayer, false);
        SetIgnore(ballsLayer, ballsLayer, false);

        SetIgnore(cueLayer, ballsTouchLayer, false);
        SetIgnore(boundingBoxLayer, ballsTouchLayer, false);
        SetIgnore(tableMeshLayer, ballsTouchLayer, false);
        SetIgnore(ballsTouchLayer, ballsTouchLayer, false);
        void SetIgnore(int layerA, int layerB, bool ignore)
        {
            Physics.IgnoreLayerCollision(layerA, layerB, ignore);
        }
    }
}
#region Enums
public enum MODE_TYPE
{
    AI = 0,
    Multiplayer = 1,
}
[System.Serializable]
public struct ShootInfo
{
    public string ShootID;
    public float ShootPower;
    public bool ballstoped;
    public ShootInfo(string id, float power)
    {
        ShootID = id;
        ShootPower = power;
        ballstoped = false;
    }
}

public enum AI_DIFFICULTY
{
    EASY = 0,
    MEDIUM = 1,
    HARD = 2
}

public enum CAMERA_MODE
{
    NORMAL = 1,
    TOP = 2,
    AI = 3,
    POCKET = 5
}

public enum MOUSE_CLICK_AREA
{
    TOP = 0,
    RIGHT = 1,
    BOTTOM = 2,
    LEFT = 3
}

public enum GUIDE_TYPE
{
    NO = 0,
    MED = 1,
    FULL = 2,
    LONG = 3
}

public enum CONTROLS
{
    SET_POWER = 0,
    POWER_FLICK = 1,
    DRAG_CUE = 2
}

public enum SnookerPhase
{
    RedAndColor,    // Potting reds and colors alternately
    ColorSequence   // Potting colors in sequence (yellow to black)
}

public enum SnookerTargetType
{
    Red = 1,           // Must pot a red ball
    AnyColor = 99,     // Can pot any colored ball (after red)
    Yellow = 2,        // Must pot yellow (15 points)
    Green = 3,         // Must pot green (6 points)
    Brown = 4,         // Must pot brown (4 points)
    Blue = 5,          // Must pot blue (5 points)
    Pink = 6,          // Must pot pink (6 points)
    Black = 7          // Must pot black (7 points)
}
#endregion