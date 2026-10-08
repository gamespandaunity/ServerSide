using System.Collections;
using System.Collections.Generic;
using Photon;
using UnityEngine;
using LudoGame;
//using Photon.Pun;
using ExitGames.Client.Photon;
//using Photon.Realtime;
using System.Xml.Linq;
using UnityEngine.Analytics;
using System;
using BallPool;
using Mirror;
public class LudoGameController : NetworkBehaviour, IMiniGame
{

    public GameObject[] dice;
    public GameObject GameGui;
    public GameGUIController gUIController;
    public GameObject[] Pawns1;
    public GameObject[] Pawns2;
    public GameObject[] Pawns3;
    public GameObject[] Pawns4;

    // Reconnect is handled entirely by the client-authoritative snapshot path in
    // GameGUIController (EnumPhoton.ReconnectGame + CmdSaveBoardState). See there.

    public GameObject gameBoard;
    public GameObject gameBoardScaler;

    [HideInInspector]
    public int steps = 5;

    public bool nextShotPossible;
    private int SixStepsCount = 0;
    public int finishedPawns = 0;
    private int botCounter = 0;
    private List<GameObject> botPawns;
    public void HighlightPawnsToMove(int player, int steps)
    {

        botPawns = new List<GameObject>();

        gUIController.restartTimer();


        GameObject[] pawns = LudoGame.GameManager.Instance.currentPlayer.pawns;

        this.steps = steps;

        if (steps == 6)
        {
            nextShotPossible = true;
            SixStepsCount++;
            if (SixStepsCount == 3)
            {
                nextShotPossible = false;
                Unhighlight();
                if (GameGui != null)
                {
                    //gUIController.SendFinishTurn();
                    Invoke("sendFinishTurnWithDelay", 1.0f);
                }

                return;
            }
        }
        else
        {
            SixStepsCount = 0;
            nextShotPossible = false;
        }

        bool movePossible = false;

        int possiblePawns = 0;
        GameObject lastPawn = null;
        for (int i = 0; i < pawns.Length; i++)
        {
            bool possible = pawns[i].GetComponent<LudoPawnController>().CheckIfCanMove(steps);
            if (possible)
            {
                lastPawn = pawns[i];
                movePossible = true;
                possiblePawns++;
                botPawns.Add(pawns[i]);
            }
        }



        if (possiblePawns == 1)
        {
            if (LudoGame.GameManager.Instance.currentPlayer.isBot)
            {
                StartCoroutine(movePawn(lastPawn, false));
            }
            else
            {
                lastPawn.GetComponent<LudoPawnController>().MakeMove();
                //StartCoroutine(MovePawnWithDelay(lastPawn));
            }

        }
        else
        {
            if (possiblePawns == 2 && lastPawn.GetComponent<LudoPawnController>().pawnInJoint != null)
            {
                if (LudoGame.GameManager.Instance.currentPlayer.isBot)
                {
                    if (!lastPawn.GetComponent<LudoPawnController>().mainInJoint)
                    {
                        StartCoroutine(movePawn(lastPawn, false));
                        Debug.Log("AAA");
                    }
                    else
                    {
                        StartCoroutine(movePawn(lastPawn.GetComponent<LudoPawnController>().pawnInJoint, false));
                        Debug.Log("BBB");
                    }

                }
                else
                {
                    if (!lastPawn.GetComponent<LudoPawnController>().mainInJoint)
                    {
                        lastPawn.GetComponent<LudoPawnController>().MakeMove();
                    }
                    else
                    {
                        lastPawn.GetComponent<LudoPawnController>().pawnInJoint.GetComponent<LudoPawnController>().MakeMove();
                    }
                    //lastPawn.GetComponent<LudoPawnController>().MakeMove();
                }
            }
            else
            {
                if (possiblePawns > 0 && LudoGame.GameManager.Instance.currentPlayer.isBot)
                {
                    int bestScoreIndex = 0;
                    int bestScore = int.MinValue;
                    // Make bot move
                    for (int i = 0; i < botPawns.Count; i++)
                    {
                        int score = botPawns[i].GetComponent<LudoPawnController>().GetMoveScore(steps);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestScoreIndex = i;
                        }
                    }

                    StartCoroutine(movePawn(botPawns[bestScoreIndex], true));
                }
            }
        }

        if (!movePossible)
        {
            if (GameGui != null)
            {
                Debug.Log("game controller call finish turn");
                gUIController.PauseTimers();
                Invoke("sendFinishTurnWithDelay", 1.0f);
            }
        }
    }

    private IEnumerator MovePawnWithDelay(GameObject lastPawn)
    {
        yield return new WaitForSeconds(1.0f);

        lastPawn.GetComponent<LudoPawnController>().MakeMove();
    }

    public void sendFinishTurnWithDelay()
    {
        gUIController.SendFinishTurn();
    }

    public void Unhighlight()
    {
        for (int i = 0; i < Pawns1.Length; i++)
        {
            Pawns1[i].GetComponent<LudoPawnController>().Highlight(false);
        }

        for (int i = 0; i < Pawns2.Length; i++)
        {
            Pawns2[i].GetComponent<LudoPawnController>().Highlight(false);
        }

        for (int i = 0; i < Pawns3.Length; i++)
        {
            Pawns3[i].GetComponent<LudoPawnController>().Highlight(false);
        }

        for (int i = 0; i < Pawns4.Length; i++)
        {
            Pawns4[i].GetComponent<LudoPawnController>().Highlight(false);
        }

    }

    void IMiniGame.BotTurn(bool first)
    {
        if (first)
        {
            SixStepsCount = 0;
        }

        Invoke("RollDiceWithDelay", GetBotDelay());
        botCounter++;
        //throw new System.NotImplementedException();
    }


    public IEnumerator movePawn(GameObject pawn, bool delay)
    {
        if (delay)
        {
            yield return new WaitForSeconds(GetBotDelay());
            botCounter++;
        }
        pawn.GetComponent<LudoPawnController>().MakeMovePC();
    }

    public void RollDiceWithDelay()
    {
        LudoGame.GameManager.Instance.currentPlayer.dice.GetComponent<GameDiceController>().RollDiceBot(GetBotDiceValue());
    }

    // Bot dice/delay data (botDelays / botDiceValues) is populated by
    // PlayofflineMode -> extractBotMoves. In guest mode that path isn't always hit,
    // leaving the lists empty — indexing them then threw ArgumentOutOfRangeException
    // inside BotTurn and froze the game. These helpers guard against empty/mismatched
    // lists, and fix the old bug where the dice value was indexed using
    // botDelays.Count instead of botDiceValues.Count.
    private float GetBotDelay()
    {
        var delays = LudoGame.GameManager.Instance.botDelays;
        if (delays == null || delays.Count == 0) return 1f;
        return delays[(botCounter + 1) % delays.Count];
    }

    private int GetBotDiceValue()
    {
        var values = LudoGame.GameManager.Instance.botDiceValues;
        if (values == null || values.Count == 0) return UnityEngine.Random.Range(1, 7);
        return values[(botCounter + 1) % values.Count];
    }


    void IMiniGame.CheckShot()
    {
        throw new System.NotImplementedException();
    }

    void IMiniGame.setMyTurn()
    {
        SixStepsCount = 0;
        LudoGame.GameManager.Instance.diceShot = false;
        dice[0].GetComponent<GameDiceController>().EnableShot();
    }

    void IMiniGame.setOpponentTurn()
    {
        SixStepsCount = 0;
        LudoGame.GameManager.Instance.diceShot = false;
        dice[0].GetComponent<GameDiceController>().DisableShot();
        Unhighlight();
    }



    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        LudoGame.GameManager.Instance.miniGame = this;
        NetworkGameManager.OnEventReceived += OnEvent;
    }

    // Use this for initialization
    void Start()
    {
        // Scale gameboard


        float scalerWidth = gameBoardScaler.GetComponent<RectTransform>().rect.size.x;
        float boardWidth = gameBoard.GetComponent<RectTransform>().rect.size.x;

        gameBoard.GetComponent<RectTransform>().localScale = new Vector2(scalerWidth / boardWidth, scalerWidth / boardWidth);

        gUIController = GameGui.GetComponent<GameGUIController>();


    }

    void OnDestroy()
    {
        NetworkGameManager.OnEventReceived -= OnEvent;
    }

    private void OnEvent(int eventcode, string CustomData, NetworkConnectionToClient sender = null)
    {
        Debug.Log("Received event Ludo: " + eventcode);

        if (eventcode == (int)EnumGame.DiceRoll)
        {

            gUIController.PauseTimers();
            string[] data = ((string)CustomData).Split(';');
            steps = int.Parse(data[0]);
            int pl = int.Parse(data[1]);
            GameGUIController.FlowDiceRolled(pl, steps);

            LudoGame.GameManager.Instance.playerObjects[pl].dice.GetComponent<GameDiceController>().RollDiceStart(steps);
        }
        else if (eventcode == (int)EnumGame.PawnMove)
        {
            string[] data = ((string)CustomData).Split(';');
            int index = int.Parse(data[0]);
            int pl = int.Parse(data[1]);
            steps = int.Parse(data[2]);
            GameGUIController.FlowPawnMoved(pl, index, steps);
            LudoGame.GameManager.Instance.playerObjects[pl].pawns[index].GetComponent<LudoPawnController>().MakeMovePC();
            // ✅ Real player clients report the settled board to the server (client-authoritative
            //    reconnect snapshot). The dedicated server must not build the snapshot itself.
            gUIController.ScheduleBoardReport();
        }
        else if (eventcode == (int)EnumGame.PawnRemove)
        {
            string data = (string)CustomData;
            string[] messages = data.Split(';');
            int index = int.Parse(messages[1]);
            int playerIndex = int.Parse(messages[0]);

            LudoGame.GameManager.Instance.playerObjects[playerIndex].pawns[index].GetComponent<LudoPawnController>().GoToInitPosition(false);
            // ✅ Report settled board (a kill sends the opponent pawn home) so the reconnect
            //    snapshot stays current. The delay in ScheduleBoardReport covers the animation.
            gUIController.ScheduleBoardReport();
        }

    }

    private void HandleDiceRoll(object content)
    {
        if (content is string data)
        {
            string[] splitData = data.Split(';');
            if (splitData.Length == 2 && int.TryParse(splitData[0], out int steps) && int.TryParse(splitData[1], out int playerIndex))
            {
                gUIController.PauseTimers();
                LudoGame.GameManager.Instance.playerObjects[playerIndex].dice.GetComponent<GameDiceController>().RollDiceStart(steps);
            }
            else
            {
                Debug.LogError("Invalid DiceRoll data: " + data);
            }
        }
        else
        {
            Debug.LogError("Invalid content type for DiceRoll event: " + content.GetType());
        }
    }

    private void HandlePawnMove(object content)
    {
        if (content is string data)
        {
            string[] splitData = data.Split(';');
            if (splitData.Length == 3 &&
                int.TryParse(splitData[0], out int index) &&
                int.TryParse(splitData[1], out int playerIndex) &&
                int.TryParse(splitData[2], out int steps))
            {
                LudoGame.GameManager.Instance.playerObjects[playerIndex].pawns[index].GetComponent<LudoPawnController>().MoveBySteps(steps);
            }
            else
            {
                Debug.LogError("Invalid PawnMove data: " + data);
            }
        }
        else
        {
            Debug.LogError("Invalid content type for PawnMove event: " + content.GetType());
        }
    }
    // #region DISCONNECT
    // public override void OnDisconnected(DisconnectCause cause)
    // {
    //     base.OnDisconnected(cause);
    //     if (LudoGame.GameManager.Instance.isLocalMultiplayer)
    //         return;

    //     if (CanRecoverFromDisconnect(cause))
    //     {
    //         Recover();
    //     }
    //     else
    //     {
    //         //if (inGame) Disconnected?.Invoke();


    //     }
    // }

    // private bool CanRecoverFromDisconnect(DisconnectCause cause)
    // {
    //     switch (cause)
    //     {
    //         case DisconnectCause.Exception:
    //         case DisconnectCause.ServerTimeout:
    //         case DisconnectCause.ClientTimeout:
    //         case DisconnectCause.DisconnectByServerLogic:
    //         case DisconnectCause.DisconnectByServerReasonUnknown:
    //             return true;
    //         default:
    //             return false;
    //     }
    // }

    // private void Recover()
    // {
    //     if (!PhotonNetwork.ReconnectAndRejoin())
    //     {
    //         Debug.LogError("ReconnectAndRejoin failed, attempting Reconnect");
    //         if (!PhotonNetwork.Reconnect())
    //         {
    //             Debug.LogError("Reconnect failed, attempting ConnectUsingSettings");
    //             if (!PhotonNetwork.ConnectUsingSettings())
    //             {
    //                 Debug.LogError("ConnectUsingSettings failed, ending game");
    //                 EndGame();
    //             }
    //         }
    //     }
    // }

    // private void EndGame()
    //{
    //     PhotonNetwork.CurrentRoom.IsVisible = false;
    //     PhotonNetwork.CurrentRoom.IsOpen = false;
    //     PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0; // Immediately remove the room
    //     PhotonNetwork.LeaveRoom();
    //     Debug.Log("Game Over: Opponent disconnected");
    // }


    // #endregion
    // #region AppInBackgroundFunctionalitt

    // public bool Isreconnectingvar = false;
    // public bool isgameInBg = false;
    // DateTime startTime;
    // DateTime endTime;
    // public GameObject waitingForOpponentPanel , ConnectionRetryPanel;
    // void OnCurrentTimeFetchedAfterBackground(DateTime nowdate)
    // {
    //     endTime = nowdate;
    //     TimeSpan totalTime = (endTime - startTime);
    //     double timedifference = MathF.Abs((float)totalTime.TotalSeconds);

    //     Debug.Log("Time Difference :" + timedifference);
    //     if (timedifference >= 20)
    //     {
    //         LoseDuetoInternetCOnnectionLost();
    //         waitingForOpponentPanel.SetActive(false);
    //         // startTime = 0;
    //     }
    //     else
    //     {
    //         isgameInBg = false;
    //         PunNetwork.instance.callrpc(false);
    //         PunNetwork.instance.MyGameTime = PunNetwork.instance.OthersGameTime;
    //         StopCoroutine(PunNetwork.instance.TimerCorotine);
    //         PunNetwork.instance.TimerCorotine = StartCoroutine(PunNetwork.instance.StartLocalTimer());
    //     }

    //     Debug.Log("Game resumed from background");


    // }




    // void OnApplicationPause(bool pauseStatus)
    // {
    //     if (!GameModeManager.isAI && !PunNetwork.instance.IsWinnerDecided)
    //     {
    //         if (pauseStatus)
    //         {
    //             // The game has gone to the background
    //             Debug.Log("Game paused or went to background");
    //             startTime = PunNetwork.instance.MyGameTime;
    //             isgameInBg = true;
    //             PunNetwork.instance.callrpc(true);
    //             if (PunNetwork.instance.TimerCorotine != null)
    //                 StopCoroutine(PunNetwork.instance.TimerCorotine);
    //         }
    //         else
    //         {
    //             APIManager.instance.GetCurrentTime(OnCurrentTimeFetchedAfterBackground, Onfailed =>
    //             {
    //                 LoseDuetoInternetCOnnectionLost();
    //             });
    //         }
    //     }
    //     else
    //     {
    //     //    waitingForOpponentPanel.SetActive(false);

    //     }

    // }

    // public void LoseDuetoInternetCOnnectionLost()
    // {
    //     PunNetwork.instance.IsWinnerDecided = true;
    //     if (PhotonNetwork.NetworkClientState == ClientState.Joined)
    //     {
    //         PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
    //         print("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    //         PhotonNetwork.LeaveRoom();
    //     }
    //     PunNetwork.instance.isGameStarted = false;
    //     waitingForOpponentPanel.SetActive(false);
    //     ConnectionRetryPanel.SetActive(false);
    //     gUIController.StopAndFinishGame(true);

    //     Debug.LogError("Calling from here lose due to");
    // }

    // #endregion

    private void HandlePawnRemove(object content)
    {
        if (content is string data)
        {
            string[] splitData = data.Split(';');
            if (splitData.Length == 2 &&
                int.TryParse(splitData[0], out int playerIndex) &&
                int.TryParse(splitData[1], out int index))
            {
                LudoGame.GameManager.Instance.playerObjects[playerIndex].pawns[index].GetComponent<LudoPawnController>().GoToInitPosition(false);
            }
            else
            {
                Debug.LogError("Invalid PawnRemove data: " + data);
            }
        }
        else
        {
            Debug.LogError("Invalid content type for PawnRemove event: " + content.GetType());
        }
    }

    // Reconnect is handled by GameGUIController's client-authoritative snapshot path
    // (EnumPhoton.ReconnectGame + EnumPhoton.SaveBoardState). No server-side simulation
    // here — the dedicated server is not a player and cannot build a trustworthy board.
}
