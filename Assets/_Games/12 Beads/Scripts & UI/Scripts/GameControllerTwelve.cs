using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Twelve
{
    public class GameControllerTwelve : MonoBehaviour
{
    public static GameControllerTwelve instance;

    public GameObject boardPointPrefab;
    public GameObject beadGreenPrefab;
    public GameObject beadRedPrefab;
    public Transform boardObjectsParent;
    public Transform referencePosition;
    private BoardPointTwelve[][] boardPoints;
    private GameObject[][] beads;

    private GameObject boardPointHolder;
    private GameObject beadsHolder;

    private const int rows = 5;
    private const int columns = 5;
    private const int cellSize = 2;
    private const int beadsPerPlayer = 12;
    private const float beadSelectionScaleIncrement = 1.5f;

    public enum Player { NONE, ME, OPPONENT, }

    public Player currentPlayer = Player.ME;

    private bool beadSelected = false;
    private int beadPickupRow = -1;
    private int beadPickupColumn = -1;
    private GameObject selectedBeadGameObject;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        boardPoints = new BoardPointTwelve[rows][];
        var boardPointPosition = Vector3.zero;
        boardPointHolder = new GameObject("BoardPointHolder");
        boardPointHolder.transform.SetParent(boardObjectsParent);
        boardPointHolder.transform.localPosition = boardPointPosition;

        var beadCounter = 0;
        var diagonalAllowed = true;

        for (int i = 0; i < rows; i++)
        {
            boardPoints[i] = new BoardPointTwelve[columns];

                //for (int j = 0; j < columns; j++)
                //{
                //    var boardPointGameObject =  PhotonNetwork.Instantiate(boardPointPrefab.name, boardPointPosition, Quaternion.identity);
                //    boardPointGameObject.transform.SetParent(boardPointHolder.transform);
                //    var boardPoint = boardPointGameObject.GetComponent<BoardPointTwelve>();
                //    boardPointGameObject.transform.localPosition = boardPointPosition;
                //    boardPoint.ownerPlayer = beadCounter < beadsPerPlayer ? Player.ME : beadCounter > beadsPerPlayer ? Player.OPPONENT : Player.NONE;
                //    boardPoint.gridRow = i;                                                                                                                                               //Photon Removal
                //    boardPoint.gridColumn = j;
                //    boardPoint.canMoveDiagonally = diagonalAllowed;

                //    boardPoints[i][j] = boardPoint;

                //    boardPointPosition = new Vector3(boardPointPosition.x + cellSize, boardPointPosition.y, 0);

                //    beadCounter++;

                //    diagonalAllowed = !diagonalAllowed;
                //}
                //boardPointPosition = new Vector3(0, boardPointPosition.y + cellSize, 0);
            }

            SetupBeads();
    }


    private void SetupBeads()
    {
        Destroy(beadsHolder);

        beads = new GameObject[boardPoints.Length][];

        beadsHolder = new GameObject("beadsHolder");
        beadsHolder.transform.SetParent(boardObjectsParent);
        beadsHolder.transform.localPosition = Vector3.zero;

        for (int i = 0; i < boardPoints.Length; i++)
        {
            beads[i] = new GameObject[boardPoints[i].Length];

            for (int j = 0; j < boardPoints[i].Length; j++)
            {
                if (boardPoints[i][j].ownerPlayer != Player.NONE)
                {
                    var bead = Instantiate(boardPoints[i][j].ownerPlayer == Player.ME ? beadGreenPrefab : beadRedPrefab, beadsHolder.transform);
                    bead.transform.position = boardPoints[i][j].transform.position;
                    beads[i][j] = bead;
                }
            }
        }
    }

    public void ProcessClick(BoardPointTwelve clickedBoardPoint)
    {
        if (!beadSelected && clickedBoardPoint.ownerPlayer == currentPlayer)
        {
            beadSelected = true;
            beadPickupRow = clickedBoardPoint.gridRow;
            beadPickupColumn = clickedBoardPoint.gridColumn;
            selectedBeadGameObject = beads[beadPickupRow][beadPickupColumn];
            var beadScale = selectedBeadGameObject.transform.localScale;
            selectedBeadGameObject.transform.localScale = new Vector3(beadScale.x * beadSelectionScaleIncrement, beadScale.y * beadSelectionScaleIncrement, beadScale.z * beadSelectionScaleIncrement); ;

            //Debug.Log("bead picked up");
        }
        else if (beadSelected)
        {
            var wasEmpty = clickedBoardPoint.ownerPlayer == Player.NONE;
            var wasCurrentPlayer = clickedBoardPoint.ownerPlayer == currentPlayer;

            var rowStep = clickedBoardPoint.gridRow - beadPickupRow;
            var columnStep = clickedBoardPoint.gridColumn - beadPickupColumn;

            if (!IsMoveValid(clickedBoardPoint, rowStep, columnStep))
            {
                return;
            }

            Move(clickedBoardPoint, rowStep, columnStep);

            if (wasCurrentPlayer || wasEmpty)
            {
                beadSelected = false;
                ConstantsData_M.LogInfo("dropping bead");

                var beadScale = selectedBeadGameObject.transform.localScale;
                selectedBeadGameObject.transform.localScale = new Vector3(beadScale.x / beadSelectionScaleIncrement, beadScale.y / beadSelectionScaleIncrement, beadScale.z / beadSelectionScaleIncrement);

                if (wasEmpty)
                {
                    //Debug.Log("Setting up beads and Switching players");
                    SwitchPlayerTurn();
                    SetupBeads();
                }
            }
        }
    }

    private bool IsMoveValid(BoardPointTwelve clickedBoardPoint, int rowStep, int columnStep)
    {
        var clickedBoardPointRow = clickedBoardPoint.gridRow;
        var clickedBoardPointColumn = clickedBoardPoint.gridColumn;

        if (Mathf.Abs(rowStep) > 0 && Mathf.Abs(columnStep) > 0 && !clickedBoardPoint.canMoveDiagonally)
        {
            //Debug.Log("Invalid Move! Diagonal move not allowed from this point.");

            return false;
        }
        else if (Mathf.Abs(rowStep) > 2 || Mathf.Abs(columnStep) > 2)
        {
            //Debug.Log("Invalid Move! You can not jump more than 2 steps.");

            return false;
        }
        else if (Mathf.Abs(rowStep) == 2 || Mathf.Abs(columnStep) == 2)
        {
            if (boardPoints[clickedBoardPointRow - rowStep / 2][clickedBoardPointColumn - columnStep/2].ownerPlayer == Player.NONE
                ||
                boardPoints[clickedBoardPointRow - rowStep / 2][clickedBoardPointColumn - columnStep / 2].ownerPlayer == currentPlayer)
            {
                //Debug.Log("Invalid Move! You can not jump 2 steps if there is no opponent next to you.");

                return false;
            }
        }

        return true;
    }
    private void Move(BoardPointTwelve clickedBoardPoint, int rowStep, int columnStep)
    {
        var clickedBoardPointRow = clickedBoardPoint.gridRow;
        var clickedBoardPointColumn = clickedBoardPoint.gridColumn;

        //Debug.Log($"rowStep : {rowStep} ::: columnStep : {columnStep}");

        if (boardPoints[clickedBoardPointRow][clickedBoardPointColumn].ownerPlayer == Player.NONE)
        {
            //Debug.Log("Moving");
            boardPoints[clickedBoardPointRow][clickedBoardPointColumn].ownerPlayer = currentPlayer;

            boardPoints[clickedBoardPointRow - rowStep][clickedBoardPointColumn - columnStep].ownerPlayer = Player.NONE;

            if (Mathf.Abs(rowStep) == 2 || Mathf.Abs(columnStep) == 2)
            {
                boardPoints[clickedBoardPointRow - rowStep / 2][clickedBoardPointColumn - columnStep / 2].ownerPlayer = Player.NONE;
            }
        }
       
    }
    public bool isMineTurn()
    {
        return currentPlayer == Player.ME;
    }
    public void SwitchPlayerTurn()
    {
        ChangeTurn();
            //Photon Removal   NetworkManagerTwelve.Instance.ChangeTurn();
        }
        public void ChangeTurn()
    {
        currentPlayer = currentPlayer == Player.ME ? Player.OPPONENT : Player.ME;
    }
    public void SetTurnAndBoard()
    {
            //Photon Removal  if (NetworkManagerTwelve.Instance.IsMasterClient())
            {
                currentPlayer = Player.ME;
        }
            //Photon Removal else
            {
                currentPlayer = Player.OPPONENT;
            boardObjectsParent.localPosition = referencePosition.localPosition;
            boardObjectsParent.localRotation = referencePosition.localRotation;
        }
    }
    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        boardObjectsParent.localPosition = referencePosition.localPosition;
    //        boardObjectsParent.localRotation = referencePosition.localRotation;
    //    }
    //}
}
}
