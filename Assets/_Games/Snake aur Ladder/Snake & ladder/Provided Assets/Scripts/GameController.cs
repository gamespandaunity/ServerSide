
using UnityEngine;

namespace Snake_Ladder
{
    public class GameController : MonoBehaviour
    {
        public static GameController instance;

        public GameObject boardPointPrefab;
        public GameObject beadGreenPrefab;
        public GameObject beadRedPrefab;
        public Transform boardObjectsParent;
        public Transform referencePosition;
        private BoardPoint[][] boardPoints;
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

            boardPoints = new BoardPoint[rows][];
            var boardPointPosition = Vector3.zero;
            boardPointHolder = new GameObject("BoardPointHolder");
            boardPointHolder.transform.SetParent(boardObjectsParent);
            boardPointHolder.transform.localPosition = boardPointPosition;

            var beadCounter = 0;
            var diagonalAllowed = true;

            for (int i = 0; i < rows; i++)
            {
                boardPoints[i] = new BoardPoint[columns];

                for (int j = 0; j < columns; j++)
                {
                    //var boardPointGameObject = PhotonNetwork.Instantiate(boardPointPrefab.name, boardPointPosition, Quaternion.identity);
                  //  boardPointGameObject.transform.SetParent(boardPointHolder.transform);
                   // var boardPoint = boardPointGameObject.GetComponent<BoardPoint>();
                  //  boardPointGameObject.transform.localPosition = boardPointPosition;
                  //  boardPoint.player = beadCounter < beadsPerPlayer ? Player.ME : beadCounter > beadsPerPlayer ? Player.OPPONENT : Player.NONE;
                  //  boardPoint.row = i;
                  //  boardPoint.column = j;
                  //  boardPoint.diagonalAllowed = diagonalAllowed;

                  //  boardPoints[i][j] = boardPoint;

                    boardPointPosition = new Vector3(boardPointPosition.x + cellSize, boardPointPosition.y, 0);

                    beadCounter++;

                    diagonalAllowed = !diagonalAllowed;
                }
                boardPointPosition = new Vector3(0, boardPointPosition.y + cellSize, 0);
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
                    if (boardPoints[i][j].player != Player.NONE)
                    {
                        var bead = Instantiate(boardPoints[i][j].player == Player.ME ? beadGreenPrefab : beadRedPrefab, beadsHolder.transform);
                        bead.transform.position = boardPoints[i][j].transform.position;
                        beads[i][j] = bead;
                    }
                }
            }
        }

        public void ProcessClick(BoardPoint clickedBoardPoint)
        {
            if (!beadSelected && clickedBoardPoint.player == currentPlayer)
            {
                beadSelected = true;
                beadPickupRow = clickedBoardPoint.row;
                beadPickupColumn = clickedBoardPoint.column;
                selectedBeadGameObject = beads[beadPickupRow][beadPickupColumn];
                var beadScale = selectedBeadGameObject.transform.localScale;
                selectedBeadGameObject.transform.localScale = new Vector3(beadScale.x * beadSelectionScaleIncrement, beadScale.y * beadSelectionScaleIncrement, beadScale.z * beadSelectionScaleIncrement); ;

                Debug.Log("bead picked up");
            }
            else if (beadSelected)
            {
                var wasEmpty = clickedBoardPoint.player == Player.NONE;
                var wasCurrentPlayer = clickedBoardPoint.player == currentPlayer;

                var rowStep = clickedBoardPoint.row - beadPickupRow;
                var columnStep = clickedBoardPoint.column - beadPickupColumn;

                if (!IsMoveValid(clickedBoardPoint, rowStep, columnStep))
                {
                    return;
                }

                Move(clickedBoardPoint, rowStep, columnStep);

                if (wasCurrentPlayer || wasEmpty)
                {
                    beadSelected = false;
                    Debug.LogWarning("dropping bead");

                    var beadScale = selectedBeadGameObject.transform.localScale;
                    selectedBeadGameObject.transform.localScale = new Vector3(beadScale.x / beadSelectionScaleIncrement, beadScale.y / beadSelectionScaleIncrement, beadScale.z / beadSelectionScaleIncrement);

                    if (wasEmpty)
                    {
                        Debug.Log("Setting up beads and Switching players");
                        SwitchPlayerTurn();
                        SetupBeads();
                    }
                }
            }
        }

        private bool IsMoveValid(BoardPoint clickedBoardPoint, int rowStep, int columnStep)
        {
            var clickedBoardPointRow = clickedBoardPoint.row;
            var clickedBoardPointColumn = clickedBoardPoint.column;

            if (Mathf.Abs(rowStep) > 0 && Mathf.Abs(columnStep) > 0 && !clickedBoardPoint.diagonalAllowed)
            {
                Debug.Log("Invalid Move! Diagonal move not allowed from this point.");

                return false;
            }
            else if (Mathf.Abs(rowStep) > 2 || Mathf.Abs(columnStep) > 2)
            {
                Debug.Log("Invalid Move! You can not jump more than 2 steps.");

                return false;
            }
            else if (Mathf.Abs(rowStep) == 2 || Mathf.Abs(columnStep) == 2)
            {
                if (boardPoints[clickedBoardPointRow - rowStep / 2][clickedBoardPointColumn - columnStep / 2].player == Player.NONE
                    ||
                    boardPoints[clickedBoardPointRow - rowStep / 2][clickedBoardPointColumn - columnStep / 2].player == currentPlayer)
                {
                    Debug.Log("Invalid Move! You can not jump 2 steps if there is no opponent next to you.");

                    return false;
                }
            }

            return true;
        }
        private void Move(BoardPoint clickedBoardPoint, int rowStep, int columnStep)
        {
            var clickedBoardPointRow = clickedBoardPoint.row;
            var clickedBoardPointColumn = clickedBoardPoint.column;

            Debug.Log($"rowStep : {rowStep} ::: columnStep : {columnStep}");

            if (boardPoints[clickedBoardPointRow][clickedBoardPointColumn].player == Player.NONE)
            {
                Debug.Log("Moving");
                boardPoints[clickedBoardPointRow][clickedBoardPointColumn].player = currentPlayer;

                boardPoints[clickedBoardPointRow - rowStep][clickedBoardPointColumn - columnStep].player = Player.NONE;

                if (Mathf.Abs(rowStep) == 2 || Mathf.Abs(columnStep) == 2)
                {
                    boardPoints[clickedBoardPointRow - rowStep / 2][clickedBoardPointColumn - columnStep / 2].player = Player.NONE;
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
            //NetworkManager.Instance.ChangeTurn();
        }
        public void ChangeTurn()
        {
            currentPlayer = currentPlayer == Player.ME ? Player.OPPONENT : Player.ME;
        }
        public void SetTurnAndBoard()
        {
           // if (NetworkManager.Instance.IsMasterClient())
            {
                currentPlayer = Player.ME;
            }
           // else
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