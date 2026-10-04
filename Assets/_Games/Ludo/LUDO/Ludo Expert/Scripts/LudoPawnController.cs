using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LudoGame;

using NaughtyAttributes;
using Mirror;
public class LudoPawnController : MonoBehaviour
{

    public AudioSource killedPawnSound;
    public AudioSource inHomeSound;
    public GameObject pawnTop;
    public GameObject pawnTopMultiple;

    public GameObject dice;
    private GameDiceController diceController;
    public GameObject pawnInJoint = null;
    public bool mainInJoint = false;
    public GameObject highlight;
    public bool isOnBoard = false;

    private LudoGameController ludoController;

    public RectTransform[] path;
    public int currentPosition = -1;

    private float singlePathSpeed = 0.13f;
    private float MoveToStartPositionSpeed = 0.25f;
    private RectTransform rect;
    private Vector3 initScale;

    public bool isMinePawn = false;

    public int index;
    public bool myTurn;

    [HideInInspector]
    private int playerIndex;
    public AudioSource[] sound;
    public Vector2 initPosition;
    private bool canMakeJoint = false;


    private int currentAudioSource = 0;
    void Start()
    {


        //Debug.Log("Game mode: " + LudoGame.GameManager.Instance.mode.ToString());
        diceController = dice.GetComponent<GameDiceController>();
        ludoController = GameObject.Find("GameSpecific").GetComponent<LudoGameController>();
        rect = GetComponent<RectTransform>();
        initScale = rect.localScale;
        initPosition = rect.anchoredPosition;

        GetComponent<Button>().interactable = false;

        //if (LudoGame.GameManager.Instance.mode == MyGameMode.Master)
        //{
        //    canMakeJoint = false;
        //}
        //Invoke(nameof(NewMethod),5);
        GetGameManager();
    }
    public List<PlayerObject> playerObjects;
    public LudoGame.GameManager gameManager;
    public void GetGameManager()
    {
        gameManager = LudoGame.GameManager.Instance;
    }
    public void GetPlayerAI()
    {
        playerObjects = LudoGame.GameManager.Instance.playerObjects;

        if (LudoGame.GameManager.Instance.currentPlayer.isBot)
        {

            LudoGame.GameManager.Instance.currentPlayer.finishedPawns = 4;
            if (LudoGame.GameManager.Instance.currentPlayer.finishedPawns == 4)
            {
                if (LudoGame.GameManager.Instance.isLocalMultiplayer)
                {
                    ludoController.gUIController.FinishedGame();
                }
                return;
            }
        }
    }

    private void NewMethod()
    {

        LudoGame.GameManager.Instance.currentPlayer.finishedPawns = 4;
        print("  NEW --------------------------------------------------------------------");
        if (LudoGame.GameManager.Instance.currentPlayer.finishedPawns == 4)
        {
            print(" in else" + "FINISHSSSS =>" + LudoGame.GameManager.Instance.currentPlayer.finishedPawns);
            if (LudoGame.GameManager.Instance.isLocalMultiplayer)
            {
                print("LOCAL MULTIPLAYER");
                ludoController.gUIController.FinishedGame();

            }
            else
            {
                print("NOT LOCAL MULTIPLAYER");
                //Wasi    PunNetwork.instance.LudoWinCall();
            }

            //    ludoController.gUIController.FinishedGame();
            return;
        }
    }

    public void setPlayerIndex(int index)
    {
        this.playerIndex = index;
    }

    public void Highlight(bool active)
    {
        //
        if (LudoGame.GameManager.Instance.currentPlayer.isBot)
        {
            if (active)
            {
                GetComponent<Button>().interactable = false;
                GetComponent<Button>().enabled = true;
                //gameObject.transform.SetAsLastSibling();
                highlight.SetActive(true);
                rect.localScale = new Vector3(initScale.x, initScale.y, initScale.z);
            }
            else
            {
                GetComponent<Button>().interactable = false;
                GetComponent<Button>().enabled = false;
                highlight.SetActive(false);
                if (currentPosition >= 0)
                {
                    CheckIfPawnInJoint();
                }
            }

            // gameObject.transform.SetAsFirstSibling();
        }
        else
        {
            if (active)
            {

                GetComponent<Button>().interactable = true;
                GetComponent<Button>().enabled = true;
                //gameObject.transform.SetAsLastSibling();
                highlight.SetActive(true);
                rect.localScale = new Vector3(initScale.x, initScale.y, initScale.z);

            }
            else
            {
                GetComponent<Button>().interactable = false;
                GetComponent<Button>().enabled = false;
                highlight.SetActive(false);
                if (currentPosition >= 0)
                {
                    CheckIfPawnInJoint();
                }

                //gameObject.transform.SetAsFirstSibling();

            }
        }

    }

    public int GetMoveScore(int steps)
    {
        if (steps == 6 && !isOnBoard)
        {
            return 300;
        }
        else
        {
            if (isOnBoard)
            {
                if (LudoGame.GameManager.Instance.mode == MyGameMode.Quick && LudoGame.GameManager.Instance.currentPlayer.canEnterHome)
                {
                    return 500;
                }

                if (pawnInJoint != null)
                {
                    steps = steps / 2;
                }

                // finish
                if (currentPosition + steps == path.Length - 1)
                {
                    return 1000;
                }

                // safe place
                if (!path[currentPosition].GetComponent<LudoPathObjectController>().isProtectedPlace && path[currentPosition + steps].GetComponent<LudoPathObjectController>().isProtectedPlace)
                {
                    return 400;
                }

                // joint
                LudoPathObjectController pathControl = path[currentPosition + steps].GetComponent<LudoPathObjectController>();
                if (pathControl.pawns.Count > 0)
                {
                    for (int i = 0; i < pathControl.pawns.Count; i++)
                    {
                        if (pathControl.pawns[i].GetComponent<LudoPawnController>().playerIndex == playerIndex)
                        {
                            return 700;
                        }
                    }
                }

                if (pathControl.pawns.Count > 0)
                {
                    for (int i = 0; i < pathControl.pawns.Count; i++)
                    {
                        if (pathControl.pawns[i].GetComponent<LudoPawnController>().playerIndex != playerIndex)
                        {
                            return 500;
                        }
                    }
                }

                if (path[currentPosition].GetComponent<LudoPathObjectController>().isProtectedPlace)
                {
                    return -100;
                }

            }
        }
        return 0;
    }

    public bool CheckIfCanMove(int steps)
    {
        if (steps == 6 && !isOnBoard)
        {
            Highlight(true);
            return true;
        }
        else
        {
            if (isOnBoard)
            {

                if (pawnInJoint != null)
                {
                    if (steps % 2 != 0)
                        return false;
                    else
                    {
                        steps = steps / 2;
                    }
                }

                if (currentPosition + steps < path.Length)
                {
                    LudoPathObjectController pathControl = path[currentPosition + steps].GetComponent<LudoPathObjectController>();

                    Debug.Log("pawns count on destination: " + pathControl.pawns.Count);
                    if (pathControl.pawns.Count == 2 && pathControl.pawns[0].GetComponent<LudoPawnController>().pawnInJoint != null)
                    {
                        Debug.Log("im inside");
                        if (pawnInJoint != null)
                        {
                            Debug.Log("return true");
                            if (pathControl.pawns[0].GetComponent<LudoPawnController>().playerIndex != playerIndex)
                            {
                                Highlight(true);
                                return true;
                            }
                            else return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }


                for (int i = 1; i < steps + 1; i++)
                {
                    if (currentPosition + i < path.Length)
                    {
                        Debug.Log("check count: " + path[currentPosition + i].GetComponent<LudoPathObjectController>().pawns.Count);
                        if (path[currentPosition + i].GetComponent<LudoPathObjectController>().pawns.Count > 1)
                        {
                            Debug.Log("more than 1");
                            if (path[currentPosition + i].GetComponent<LudoPathObjectController>().pawns[0].GetComponent<LudoPawnController>().pawnInJoint != null)
                            {
                                Debug.Log("blockade");
                                return false;
                            }
                        }
                    }
                }


                if (currentPosition == path.Length - 1 || currentPosition + steps > path.Length - 1)
                {
                    return false;
                }

                if ((currentPosition + steps > path.Length - 1 - 6) &&
                    LudoGame.GameManager.Instance.needToKillOpponentToEnterHome &&
                !LudoGame.GameManager.Instance.playerObjects[playerIndex].canEnterHome)
                {
                    return false;
                }


                Highlight(true);
                return true;
            }
        }
        return false;
    }

    public void GoToStartPosition()
    {
        rect.SetAsLastSibling();
        currentPosition = 0;
        StartCoroutine(MoveDelayed(0, initPosition, path[currentPosition].anchoredPosition, MoveToStartPositionSpeed, true, true));
        pawnTop.SetActive(false);
        if (pawnInJoint != null)
        {
            pawnInJoint.GetComponent<LudoPawnController>().pawnInJoint = null;
            pawnInJoint.GetComponent<LudoPawnController>().GoToStartPosition();
            pawnInJoint = null;
        }
    }

    public void GoToInitPosition(bool callEnd)
    {
        killedPawnSound.Play();
        rect.SetAsLastSibling();
        isOnBoard = false;
        currentPosition = -1;
        pawnTop.SetActive(true);
        pawnTopMultiple.SetActive(false);
        StartCoroutine(MoveDelayed(0, rect.anchoredPosition, initPosition, MoveToStartPositionSpeed, true, false));
        if (pawnInJoint != null)
        {
            pawnInJoint.GetComponent<LudoPawnController>().pawnInJoint = null;
            pawnInJoint.GetComponent<LudoPawnController>().GoToInitPosition(true);
            pawnInJoint = null;
        }
        //path[currentPosition].GetComponent<LudoPathObjectController>().RemovePawn(this.gameObject);
    }
    public void GoToPosition(int TargetPosition)
    {
        currentPosition = TargetPosition;
        StartCoroutine(MoveDelayed(0, rect.anchoredPosition, path[TargetPosition].anchoredPosition, MoveToStartPositionSpeed, true, false));
        Debug.Log(index + " : " + TargetPosition);
        //path[currentPosition].GetComponent<LudoPathObjectController>().RemovePawn(this.gameObject);
    }

    public void MoveBySteps(int steps)
    {
        LudoPathObjectController controller = path[currentPosition].GetComponent<LudoPathObjectController>();

        controller.RemovePawn(this.gameObject);

        RepositionPawns(controller.pawns.Count, currentPosition);

        rect.SetAsLastSibling();




        for (int i = 0; i < steps; i++)
        {
            bool last = false;
            if (i == steps - 1) last = true;

            currentPosition++;
            StartCoroutine(MoveDelayed(i, path[currentPosition - 1].anchoredPosition, path[currentPosition].anchoredPosition, singlePathSpeed, last, true));
        }
    }

    void CheckIfPawnInJoint()
    {
        LudoPathObjectController pathController = path[currentPosition].GetComponent<LudoPathObjectController>();
        Debug.Log("currentPosition   " + currentPosition + "   count  " + pathController.pawns.Count);
        if (pathController.pawns.Count > 1)
        {
            RepositionPawns(pathController.pawns.Count, currentPosition);
        }
    }

    public int CurrentPosition
    {
        get
        {
            return currentPosition;
        }
    }

    public void MakeMove()
    {
        Debug.Log("Make move button");

        string data = index + ";" + ludoController.gUIController.GetCurrentPlayerIndex() + ";" + ludoController.steps;
        if (NetworkServer.active || NetworkClient.active)
        {
            NetworkGameManager.Instance.CmdRiseEvent((int)EnumGame.PawnMove, data);
        }
        else
        {
            myTurn = true;
        }


        if (pawnInJoint != null) ludoController.steps /= 2;
        LudoGame.GameManager.Instance.diceShot = true;
        ludoController.gUIController.PauseTimers();
        ludoController.Unhighlight();


        if (LudoGame.GameManager.Instance.isLocalMultiplayer)
        {
            if (!isOnBoard)
            {
                GoToStartPosition();
            }
            else
            {
                if (pawnInJoint != null)
                {
                    pawnInJoint.GetComponent<LudoPawnController>().MoveBySteps(ludoController.steps);
                }
                MoveBySteps(ludoController.steps);
            }


            isOnBoard = true;
        }
    }

    public void MakeMovePC()
    {
        if (pawnInJoint != null) ludoController.steps /= 2;

        ludoController.gUIController.PauseTimers();

        if (!isOnBoard)
        {
            GoToStartPosition();
        }
        else
        {
            if (pawnInJoint != null)
            {
                pawnInJoint.GetComponent<LudoPawnController>().MoveBySteps(ludoController.steps);
            }
            MoveBySteps(ludoController.steps);
        }

        isOnBoard = true;
    }

    private IEnumerator MoveDelayed(int delay, Vector2 from, Vector2 to, float time, bool last, bool playSound)
    {

        rect.localScale = new Vector3(initScale.x * 1.2f, initScale.y * 1.2f, initScale.z);




        yield return new WaitForSeconds(delay * singlePathSpeed);

        if (playSound)
        {
            sound[currentAudioSource % sound.Length].Play();
            currentAudioSource++;
        }

        if (last)
        {
            iTween.ValueTo(gameObject, iTween.Hash("from", from, "to", to, "time", time, "easetype", iTween.EaseType.linear, "onupdate", "UpdatePosition", "oncomplete", "MoveFinished"));
        }
        else
        {
            iTween.ValueTo(gameObject, iTween.Hash("from", from, "to", to, "time", time, "easetype", iTween.EaseType.linear, "onupdate", "UpdatePosition"));
        }

    }

    private void resetScale()
    {
        rect.localScale = initScale;
    }

    private void MoveFinished()
    {
        resetScale();

        if (currentPosition >= 0)
        {
            bool canSendFinishTurn = true;

            LudoPathObjectController pathController = path[currentPosition].GetComponent<LudoPathObjectController>();


            pathController.AddPawn(this.gameObject);


            if (pawnInJoint == null || (pawnInJoint != null && mainInJoint))
            {



                Debug.Log("Main in joint");
                int otherCount = pathController.pawns.Count;

                Debug.Log("Pawns count: " + otherCount);



                if (!pathController.isProtectedPlace)
                {
                    if (otherCount > 1) // Check and remove opponent pawns to home
                    {
                        for (int i = otherCount - 2; i >= 0; i--)
                        {
                            if (pathController.pawns[i].GetComponent<LudoPawnController>().playerIndex != playerIndex)
                            {
                                int color = pathController.pawns[i].GetComponent<LudoPawnController>().playerIndex;
                                // Coutn pawns in this color
                                int pawnsInColor = 0;
                                for (int k = 0; k < otherCount; k++)
                                {
                                    if (pathController.pawns[k].GetComponent<LudoPawnController>().playerIndex == color)
                                    {
                                        pawnsInColor++;
                                    }
                                }

                                if (pawnsInColor == 1 || canMakeJoint)
                                {
                                    // Killed opponent pawn, Additional turn
                                    ludoController.nextShotPossible = true;
                                    LudoGame.GameManager.Instance.playerObjects[playerIndex].canEnterHome = true;
                                    LudoGame.GameManager.Instance.playerObjects[playerIndex].homeLockObjects.SetActive(false);
                                    // Move killed pawn to start position and remove from list
                                    pathController.pawns[i].GetComponent<LudoPawnController>().GoToInitPosition(false);


                                    pathController.RemovePawn(pathController.pawns[i]);
                                }
                            }
                            else
                            {
                                if (canMakeJoint && pawnInJoint == null)
                                {
                                    Debug.Log("Joint");
                                    pawnInJoint = pathController.pawns[i];
                                    mainInJoint = true;
                                    pathController.pawns[i].GetComponent<LudoPawnController>().mainInJoint = false;
                                    pathController.pawns[i].GetComponent<LudoPawnController>().pawnInJoint = this.gameObject;
                                    pawnTop.SetActive(false);
                                    pawnTopMultiple.SetActive(true);
                                    pathController.pawns[i].GetComponent<LudoPawnController>().pawnTop.SetActive(false);
                                    pathController.pawns[i].GetComponent<LudoPawnController>().pawnTopMultiple.SetActive(true);
                                }
                            }
                        }

                    }
                }
                else
                {
                    if (pawnInJoint != null)
                    {
                        canSendFinishTurn = false;
                        pawnTop.SetActive(true);
                        pawnTopMultiple.SetActive(false);
                        pawnInJoint.GetComponent<LudoPawnController>().pawnTop.SetActive(true);
                        pawnInJoint.GetComponent<LudoPawnController>().pawnTopMultiple.SetActive(false);

                        pawnInJoint.GetComponent<LudoPawnController>().pawnInJoint = null;
                        pawnInJoint = null;
                    }
                }

                otherCount = pathController.pawns.Count;

                if (pawnInJoint == null)
                    RepositionPawns(otherCount, currentPosition);

                if (currentPosition == path.Length - 1)
                {
                    inHomeSound.Play();
                }

                if ((LudoGame.GameManager.Instance.isMyTurn || LudoGame.GameManager.Instance.currentPlayer.isBot) && currentPosition == path.Length - 1)
                {
                    Debug.Log("FINISHSSSS");
                   
                    LudoGame.GameManager.Instance.currentPlayer.finishedPawns++;
                    Debug.Log("FINISHSSSS =>" + LudoGame.GameManager.Instance.currentPlayer.finishedPawns);
                    //ludoController.finishedPawns++;
                    if (LudoGame.GameManager.Instance.mode == MyGameMode.Quick)
                    {
                        if (LudoGame.GameManager.Instance.currentPlayer.finishedPawns == 1)
                        {
                            if (LudoGame.GameManager.Instance.currentPlayer.isBot)
                            {
                                ludoController.gUIController.FinishedGame();
                                Debug.Log("Win1");
                            }
                            else
                            {
                                Debug.Log("Win2");
                                //Wasi  PunNetwork.instance.LudoWinCall();
                            }
                            return;
                        }
                    }
                    else
                    {
                        print(" in else");
                        if (LudoGame.GameManager.Instance.currentPlayer.finishedPawns == 4)
                        {
                            print(" in else" + "FINISHSSSS =>" + LudoGame.GameManager.Instance.currentPlayer.finishedPawns);
                            if (LudoGame.GameManager.Instance.isLocalMultiplayer)
                            {
                                ludoController.gUIController.FinishedGame();
                                Debug.Log("Win3");
                            }
                            else
                            {
                                Debug.Log("Win4");
                                NetworkGameManager.Instance.CmdPlayerFinished(true,staticVariables.UserProfiledata.user._id);
                                //Wasi    PunNetwork.instance.LudoWinCall();
                            }

                            //    ludoController.gUIController.FinishedGame();
                            return;
                        }
                    }
                    ludoController.nextShotPossible = true;
                }

                if (((LudoGame.GameManager.Instance.isMyTurn && LudoGame.GameManager.Instance.diceShot) || LudoGame.GameManager.Instance.currentPlayer.isBot) && canSendFinishTurn)
                {
                    if (ludoController.nextShotPossible)
                    {
                        LudoGame.GameManager.Instance.currentPlayer.dice.GetComponent<GameDiceController>().EnableShot();
                        ludoController.gUIController.restartTimer();
                    }
                    else
                    {
                        Debug.Log("move finished call finish turn");
                        StartCoroutine(CheckTurnDelay());
                    }
                }
                else
                {
                    ludoController.gUIController.restartTimer();
                }
            }
        }




    }


    private IEnumerator CheckTurnDelay()
    {
        Debug.Log("   isBot               " + LudoGame.GameManager.Instance.currentPlayer.isBot);
        if (LudoGame.GameManager.Instance.currentPlayer.isBot)
        {
            ludoController.Unhighlight();
        }
        yield return new WaitForSeconds(1.0f);
        ludoController.gUIController.SendFinishTurn();

    }

    private void RepositionPawns(int otherCount, int currentPosition)
    {

        LudoPathObjectController pathController = path[currentPosition].GetComponent<LudoPathObjectController>();

        float scale = 0.8f;
        float offset = 20f / otherCount;
        float startPos = 0;

        startPos = (-offset / 2) * otherCount + offset / 2;
        scale = 1 - 0.05f * otherCount + 0.05f;

        /*if (otherCount == 1)
        {
            startPos = 0;
            scale = 1;
        }
        else if (otherCount == 2)
        {
            startPos = -offset / 2;
            scale = 0.95f;
        }
        else if (otherCount == 3)
        {
            startPos = -offset;
            scale = 0.85f;
        }
        else if (otherCount == 4)
        {
            startPos = -offset * 1.5f;
            scale = 0.75f;
        }*/


        // Get my pawns, push on top of stack
        List<int> orderPawns = new List<int>();

        for (int i = 0; i < otherCount; i++)
        {
            if (pathController.pawns[i].GetComponent<LudoPawnController>().playerIndex == LudoGame.GameManager.Instance.myPlayerIndex)
            {
                orderPawns.Add(i);
            }
            else
            {
                orderPawns.Insert(0, i);
            }
        }
        // Reposition pawns if more than 1 on spot
        for (int i = 0; i < otherCount; i++)
        {
            RectTransform rT = pathController.pawns[orderPawns[i]].GetComponent<RectTransform>();
            pathController.pawns[orderPawns[i]].GetComponent<RectTransform>().anchoredPosition = new Vector2(
                path[currentPosition].GetComponent<RectTransform>().anchoredPosition.x + startPos + i * offset,
                path[currentPosition].GetComponent<RectTransform>().anchoredPosition.y);
            pathController.pawns[orderPawns[i]].GetComponent<RectTransform>().localScale = new Vector2(initScale.x * scale, initScale.y * scale);

            pathController.pawns[orderPawns[i]].GetComponent<RectTransform>().SetAsLastSibling();

        }


        // }
    }

    private void UpdatePosition(Vector2 pos)
    {
        rect.anchoredPosition = pos;
    }


    // Update is called once per frame
    void Update()
    {

    }

    public int SiblingIndex
    {
        get
        {
            return path[CurrentPosition].GetComponent<LudoPathObjectController>().SiblingIndex;
        }
    }

    public void AddInstantly(int currentPos)
    {
        currentPosition = currentPos;
        if (currentPosition != -1)
        {
            isOnBoard = true;
            rect.anchoredPosition = path[CurrentPosition].anchoredPosition;
        }
        else
        {
            isOnBoard = false;
            rect.anchoredPosition = initPosition;
            pawnTop.SetActive(true);
            pawnTopMultiple.SetActive(false);
            if (pawnInJoint != null)
            {
                var partner = pawnInJoint.GetComponent<LudoPawnController>();
                if (partner != null) partner.pawnInJoint = null;
                pawnInJoint = null;
            }
        }
    }

    // ─── Reconnect stack rebuild ────────────────────────────────────────────────
    // AddInstantly only sets the raw position; it does not register the pawn into its
    // path cell's pawns list, so stacked pawns land exactly on top of each other and
    // become invisible after a reconnect. The three helpers below rebuild that state
    // idempotently (clear → register → reposition) so overlapping pawns are re-offset.

    /// <summary>Clears the pawns list of every cell in this pawn's path (idempotent reset).</summary>
    public void ClearAllPathCells()
    {
        if (path == null) return;
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == null) continue;
            var cell = path[i].GetComponent<LudoPathObjectController>();
            if (cell != null) cell.pawns.Clear();
        }
    }

    /// <summary>Re-registers this pawn into its current cell so stacking can be computed.</summary>
    public void RegisterOnPathCellAfterRestore()
    {
        if (!isOnBoard || currentPosition < 0 || currentPosition >= path.Length) return;
        var cell = path[currentPosition].GetComponent<LudoPathObjectController>();
        if (cell != null && !cell.pawns.Contains(this.gameObject)) cell.AddPawn(this.gameObject);
    }

    /// <summary>Re-offsets a stacked cell (or resets a single pawn) after restore.</summary>
    public void RepositionAfterRestore()
    {
        if (!isOnBoard || currentPosition < 0 || currentPosition >= path.Length) return;
        var cell = path[currentPosition].GetComponent<LudoPathObjectController>();
        if (cell == null) return;
        if (cell.pawns.Count > 1)
        {
            RepositionPawns(cell.pawns.Count, currentPosition);
        }
        else
        {
            rect.anchoredPosition = path[currentPosition].GetComponent<RectTransform>().anchoredPosition;
            rect.localScale = initScale;
        }
    }
}
