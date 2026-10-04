using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Mirror;
namespace Snake_Ladder
{
    public class GamePlayController : NetworkBehaviour
    {
        public List<NodeScript> allNodes;

        public List<NodeScript> team1Nodes;
        public List<NodeScript> team2Nodes;

        [HideInInspector]
        public List<BeadScript> team1Beads;
        [HideInInspector]
        public List<BeadScript> team2Beads;
        [HideInInspector]
        public List<BeadScript> emptyBeads;

        public GameObject beadTeamPrefab1;
        public GameObject beadTeamPrefab2;

        public GameObject emptyNode;

        public Transform beadsParentTransform;
        // public TextMeshProUGUI turnText;

        // Player 1 UI
        [Header("Player 1")]
        public GameObject Player1Timer;
        public TextMeshProUGUI player1ScoreTxt;
        public TextMeshProUGUI player1NameTxt;

        // Player 2 UI
        [Header("Player 2")]
        public GameObject Player2Timer;
        public TextMeshProUGUI player2ScoreTxt;
        public TextMeshProUGUI player2NameTxt;

        public GameObject gameOver;
        public GameObject winUI;
        public GameObject looseUI;
        public TextMeshProUGUI winLostText;

        public int player1Score = 0;
        public int player2Score = 0;


        public BeadScript turnSlectedBead;
        public PLAYERS currentPlayerTurn = PLAYERS.EMPTY;

        public bool isWithAI;
        public bool isOnlineMultiplayer;

        public MultiPlayerGameManager multiPlayerGame;
        public int myId;
        public int aiId;
        public GameObject LoadingPanel;
        public GameObject DisconnectedPanel;
        public TextMeshProUGUI DisconnectedText;

        public Sprite redSprite, redUpSprite;
        public Sprite greenSprite, greenUpSprite;
        public TurnAnnouncement turnAnnouncement;

        public GameObject[] offlineTurnHighLighter;

        private bool isTakingNextMove;
        public TurnTimer turnTimer;

        public Pool poolBeads;
        private void Start()
        {
            if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_Ai))
            {
                isWithAI = true;
                isOnlineMultiplayer = false;
            }
            else if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_LocalPlayer))
            {
                isWithAI = false;
                isOnlineMultiplayer = false;
            }
            else if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_OnlineFriend))
            {
                isWithAI = false;
                isOnlineMultiplayer = true;
            }
            else if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_RandomPlayer))
            {
                isWithAI = false;
                isOnlineMultiplayer = true;
            }


            CreateTeamsBeads();
            if (!isOnlineMultiplayer)
            {
                myId = PLAYERS.PLAYER1.GetHashCode();
                aiId = PLAYERS.PLAYER2.GetHashCode();
                TurnTimer.TunrnTimerHasExpired += OnTurnTimerExipred;

                StartGame();
                turnTimer.ResetRound();
            }
            else
            {

                LoadingPanel.SetActive(true);
            }

        }

        void OnTurnTimerExipred()
        {
            if (!isOnlineMultiplayer)
            {
                NextTurn();

            }
        }

        public void OnDisable()
        {
            TurnTimer.TunrnTimerHasExpired -= OnTurnTimerExipred;
        }

        public void StartGameInCaseOfDisconnect()
        {
            if (isWithAI && aiId == currentPlayerTurn.GetHashCode())
            {
                StartCoroutine(PerformAIMove(false, true));
            }



            if ((PLAYERS)aiId == PLAYERS.PLAYER1)
            {
                offlineTurnHighLighter[0].SetActive(currentPlayerTurn == PLAYERS.PLAYER2 ? true : false);
                offlineTurnHighLighter[1].SetActive(currentPlayerTurn == PLAYERS.PLAYER1 ? true : false);

                for (int i = 0; i < team1Beads.Count; i++)
                {
                    team1Beads[i].selectedSprite = redUpSprite;
                }
                for (int i = 0; i < team2Beads.Count; i++)
                {
                    team2Beads[i].selectedSprite = greenUpSprite;
                }
            }
            else
            {
                offlineTurnHighLighter[0].SetActive(currentPlayerTurn == PLAYERS.PLAYER1 ? true : false);
                offlineTurnHighLighter[1].SetActive(currentPlayerTurn == PLAYERS.PLAYER2 ? true : false);
            }
        }
        public void StartGame()
        {
            if (!isOnlineMultiplayer)
            {
                if (Random.Range(0, 1000) < 500)
                {
                    currentPlayerTurn = PLAYERS.PLAYER1;
                    if (isWithAI && aiId == PLAYERS.PLAYER1.GetHashCode())
                    {
                        StartCoroutine(PerformAIMove(false, true));
                    }
                }
                else
                {
                    currentPlayerTurn = PLAYERS.PLAYER2;
                    if (isWithAI && aiId == PLAYERS.PLAYER2.GetHashCode())
                    {
                        StartCoroutine(PerformAIMove(false, true));
                    }
                }

                turnAnnouncement.SetTurnData(false, isWithAI, currentPlayerTurn);

                if ((PLAYERS)aiId == PLAYERS.PLAYER1)
                {
                    offlineTurnHighLighter[0].SetActive(currentPlayerTurn == PLAYERS.PLAYER2 ? true : false);
                    offlineTurnHighLighter[1].SetActive(currentPlayerTurn == PLAYERS.PLAYER1 ? true : false);
                }
                else
                {
                    offlineTurnHighLighter[0].SetActive(currentPlayerTurn == PLAYERS.PLAYER1 ? true : false);
                    offlineTurnHighLighter[1].SetActive(currentPlayerTurn == PLAYERS.PLAYER2 ? true : false);
                }

            }
            else
            {
                LoadingPanel.SetActive(false);

                if (myId != currentPlayerTurn.GetHashCode())
                {
                    //turnText.text = GameManager.instance.OpponentName.ToString();
                    Player2Timer.SetActive(true);
                    Player1Timer.SetActive(false);
                    turnAnnouncement.SetTurnData(true, isWithAI, currentPlayerTurn, false);

                }
                else
                {

                    Player2Timer.SetActive(false);
                    Player1Timer.SetActive(true);
                    // turnText.text = GameManager.instance.UserName.ToString();
                    turnAnnouncement.SetTurnData(true, isWithAI, currentPlayerTurn, true);

                }

                if (turnSlectedBead)
                {
                    turnSlectedBead.currentNode.emptyNode.SetActive(false);
                    RefreshBeads();
                }
            }
            //turnText.text = currentPlayerTurn.ToString();

            //if (isWithAI && currentPlayerTurn == PLAYERS.PLAYER2)
            //    turnText.text = "AI TURN";


            RefreshScore();
        }



        public void NextTurn()
        {
            if (!isOnlineMultiplayer)
            {
                if (currentPlayerTurn == PLAYERS.PLAYER1)
                {
                    currentPlayerTurn = PLAYERS.PLAYER2;
                    if (!IsCheckMate(team2Beads))
                    {
                        if (isWithAI && aiId == PLAYERS.PLAYER2.GetHashCode())
                        {
                            StartCoroutine(PerformAIMove(false, true));
                        }
                    }
                    else
                    {
                        AnnounceWinOrLoose(true);
                    }
                }
                else
                {
                    currentPlayerTurn = PLAYERS.PLAYER1;

                    if (!IsCheckMate(team1Beads))
                    {
                        if (isWithAI && aiId == PLAYERS.PLAYER1.GetHashCode())
                        {
                            StartCoroutine(PerformAIMove(false, true));
                        }
                    }
                    else
                    {
                        AnnounceWinOrLoose(false);
                    }


                }

                if ((PLAYERS)aiId == PLAYERS.PLAYER1)
                {
                    offlineTurnHighLighter[0].SetActive(currentPlayerTurn == PLAYERS.PLAYER2 ? true : false);
                    offlineTurnHighLighter[1].SetActive(currentPlayerTurn == PLAYERS.PLAYER1 ? true : false);
                }
                else
                {
                    offlineTurnHighLighter[0].SetActive(currentPlayerTurn == PLAYERS.PLAYER1 ? true : false);
                    offlineTurnHighLighter[1].SetActive(currentPlayerTurn == PLAYERS.PLAYER2 ? true : false);
                }

                turnTimer.ResetRound();
                //turnText.text = currentPlayerTurn.ToString();
                //if (isWithAI && currentPlayerTurn == PLAYERS.PLAYER2)
                //    turnText.text = "AI TURN";
            }
            else
            {
                multiPlayerGame.NextPlayerTurn();
            }
            // Debug.LogError("Next Turn Game Play");

            RefreshScore();



        }


        void RefreshScore()
        {
            if (!isOnlineMultiplayer)
            {
                if (player1Score >= SnakeGameConstants.SCORE_TO_WIN)
                {
                    if (isWithAI)
                    {
                        if ((PLAYERS)aiId == PLAYERS.PLAYER1)
                        {
                            AnnounceWinOrLoose(false);
                        }
                        else
                        {
                            AnnounceWinOrLoose(true);
                        }
                    }
                    else
                    {
                        winLostText.text = "Player One Won....";
                        AnnounceWinOrLoose(true);
                    }
                }
                else if (player2Score >= SnakeGameConstants.SCORE_TO_WIN)
                {
                    if (isWithAI)
                    {
                        if ((PLAYERS)aiId == PLAYERS.PLAYER2)
                        {
                            AnnounceWinOrLoose(false);
                        }
                        else
                        {
                            AnnounceWinOrLoose(true);
                        }
                    }
                    else
                    {
                        winLostText.text = "Player Two Won....";

                        AnnounceWinOrLoose(true);
                    }
                }
            }
            else
            {
                if (player1Score >= SnakeGameConstants.SCORE_TO_WIN)
                {
                    if (myId == PLAYERS.PLAYER1.GetHashCode())
                    {
                        AnnounceWinOrLoose(true);
                    }
                    else
                    {
                        AnnounceWinOrLoose(false);

                    }
                }
                else if (player2Score >= SnakeGameConstants.SCORE_TO_WIN)
                {
                    if (myId == PLAYERS.PLAYER2.GetHashCode())
                    {
                        AnnounceWinOrLoose(true);
                    }
                    else
                    {
                        AnnounceWinOrLoose(false);

                    }
                }
            }

            if (isOnlineMultiplayer)
            {
                if (myId == PLAYERS.PLAYER1.GetHashCode())
                {
                    player1ScoreTxt.text = $"Score: <color=green> {player1Score.ToString()}</color>";
                    player2ScoreTxt.text = $"Score: <color=green> {player2Score.ToString()}</color>";
                }
                else
                {
                    player1ScoreTxt.text = $"Score: <color=green> {player2Score.ToString()}</color>";
                    player2ScoreTxt.text = $"Score: <color=green> {player1Score.ToString()}</color>";
                }
                player1NameTxt.text = $"{SnakeGameManager.instance.UserName.ToString()}";
                player2NameTxt.text = $"{SnakeGameManager.instance.OpponentName.ToString()}";
            }
            else
            {
                if (myId == PLAYERS.PLAYER1.GetHashCode())
                {
                    player1ScoreTxt.text = $"Score: <color=green> {player1Score.ToString()}</color>";
                    player2ScoreTxt.text = $"Score: <color=green> {player2Score.ToString()}</color>";
                }
                else
                {
                    player1ScoreTxt.text = $"Score: <color=green> {player2Score.ToString()}</color>";
                    player2ScoreTxt.text = $"Score: <color=green> {player1Score.ToString()}</color>";
                }

                player1NameTxt.text = "You";

                if (isWithAI)
                {
                    player2NameTxt.text = $"AI";
                }

                else
                    player2NameTxt.text = $"Player 2";

            }

        }


        void AnnounceWinOrLoose(bool isWin)
        {
            gameOver.SetActive(true);
            winUI.SetActive(isWin);
            looseUI.SetActive(!isWin);
        }

      void CreateTeamsBeads()
{
    Debug.Log("Creating Team With " + (isServer ? "Server" : "Client"));
    team1Beads = new List<BeadScript>();
    team2Beads = new List<BeadScript>();
    emptyBeads = new List<BeadScript>();
    
    for (int i = 0; i < allNodes.Count; i++)
    {
        GameObject obj = GameObject.Instantiate(emptyNode);
        obj.transform.position = allNodes[i].transform.position + new Vector3(0, 0, -2);
        obj.transform.parent = beadsParentTransform;
        BeadScript beadScript = obj.GetComponent<BeadScript>();
        allNodes[i].emptyNode = obj;
        obj.SetActive(false);
        beadScript.playerID = PLAYERS.EMPTY;
        beadScript.beadId = i;
        beadScript.currentNode = allNodes[i];
        emptyBeads.Add(beadScript);
    }

    for (int i = 0; i < team1Nodes.Count; i++)
    {
        GameObject obj = GameObject.Instantiate(beadTeamPrefab1);
        obj.transform.position = team1Nodes[i].transform.position + new Vector3(0, 0, -2);
        obj.transform.parent = beadsParentTransform;
        BeadScript beadScript = obj.GetComponent<BeadScript>();
        beadScript.playerID = PLAYERS.PLAYER1;
        beadScript.currentNode = team1Nodes[i];
        beadScript.beadId = i;

        team1Nodes[i].OnOccupied(true);
        team1Nodes[i].currentBead = beadScript;
        team1Beads.Add(beadScript);

        if (isOnlineMultiplayer)
        {
            // Client (non-server) swaps sprites to view from their perspective
            if (!NetworkServer.active)
            {
                beadScript.selectedSprite = redUpSprite;
                obj.GetComponent<SpriteRenderer>().sprite = redSprite;
            }
        }
    }
    
    for (int i = 0; i < team2Nodes.Count; i++)
    {
        GameObject obj = GameObject.Instantiate(beadTeamPrefab2);
        obj.transform.position = team2Nodes[i].transform.position + new Vector3(0, 0, -2);
        obj.transform.parent = beadsParentTransform;
        BeadScript beadScript = obj.GetComponent<BeadScript>();
        beadScript.playerID = PLAYERS.PLAYER2;
        beadScript.currentNode = team2Nodes[i];
        beadScript.beadId = i;

        team2Nodes[i].OnOccupied(true);
        team2Nodes[i].currentBead = beadScript;
        team2Beads.Add(beadScript);

        if (isOnlineMultiplayer)
        {
            // Client (non-server) swaps sprites to view from their perspective
            if (!NetworkServer.active)
            {
                beadScript.selectedSprite = greenUpSprite;
                obj.GetComponent<SpriteRenderer>().sprite = greenSprite;
            }
        }
    }
}
        void Update()
        {
            if (myId != currentPlayerTurn.GetHashCode() && isOnlineMultiplayer)
            {
                // turnText.text = GameManager.instance.OpponentName.ToString();
                // Player2Timer.SetActive(true);
                // Player1Timer.SetActive(false);
                return;
            }

            if (isWithAI && aiId == currentPlayerTurn.GetHashCode())
            {
                return;
            }
            // else if (isOnlineMultiplayer)
            // {
            //     Player2Timer.SetActive(true);
            //     Player1Timer.SetActive(false);
            //     turnText.text = GameManager.instance.UserName.ToString();
            // }

            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit2D rayHit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));

                BeadScript beadScript = rayHit.transform.gameObject.GetComponent<BeadScript>();

                Debug.Log("currentPlayerTurn " + currentPlayerTurn.ToString());
                if (beadScript && beadScript.playerID == currentPlayerTurn && beadScript.playerID != PLAYERS.EMPTY)
                {
                    RefreshBeads();
                    ActivateMoveAbleEmptyBeads(beadScript);

                    if (isOnlineMultiplayer && multiPlayerGame)
                        multiPlayerGame.SendBeadSelectedMessage(currentPlayerTurn.GetHashCode(), beadScript.beadId);
                }

                OnMoveSelected(beadScript);

                Debug.Log(rayHit.transform.name);

            }
        }

        GameObject tempObject;
        Vector3 targetPostion = new Vector3(0, 10, 0);
        public void OnMoveSelected(BeadScript beadScript, bool isRemote = false)
        {
            if (turnSlectedBead && beadScript && beadScript.playerID == PLAYERS.EMPTY)
            {
                if (!turnSlectedBead.isCanMove(beadScript.currentNode))
                {
                    //Debug.LogError("Cant Move Bead===========================");
                    return;
                }
                if (!isRemote && isOnlineMultiplayer && multiPlayerGame)
                {
                    multiPlayerGame.SendMoveSelectedMessage(currentPlayerTurn.GetHashCode(), beadScript.beadId);
                }

                turnSlectedBead.currentNode.OnOccupied(false);
                // turnSlectedBead.transform.position = beadScript.transform.position;

                // Tween the movement from current position to beadScript's position
                turnSlectedBead.transform.DOMove(beadScript.transform.position, 0.15f);
                //{
                // Run the rest of the method OnComplete
                turnSlectedBead.currentNode = beadScript.currentNode;
                turnSlectedBead.currentNode.OnOccupied(true);
                turnSlectedBead.currentNode.SetBead(turnSlectedBead);
                turnSlectedBead.AdNode(beadScript.currentNode);

                // turnSlectedBead.currentNode.currentBead = turnSlectedBead;

                if (beadScript.enmyBead != null)
                {
                    if (turnSlectedBead.playerID == PLAYERS.PLAYER1)
                    {
                        player1Score++;
                    }
                    else
                    {
                        player2Score++;
                    }
                    if (myId == turnSlectedBead.playerID.GetHashCode())
                    {
                        targetPostion = Camera.main.ScreenToWorldPoint(Player1Timer.transform.position);
                    }
                    else
                    {
                        targetPostion = Camera.main.ScreenToWorldPoint(Player2Timer.transform.position);
                    }
                    beadScript.enmyBead.gameObject.SetActive(false);
                    if (tempObject)
                    {
                        poolBeads.Restore(tempObject);
                    }
                    tempObject = poolBeads.Retrieve();
                    tempObject.transform.position = beadScript.enmyBead.transform.position;
                    tempObject.GetComponent<SpriteRenderer>().sprite = beadScript.enmyBead.GetComponent<SpriteRenderer>().sprite;
                    beadScript.enmyBead.isDead = true;
                    tempObject.SetActive(true);
                    tempObject.transform.DOMove(targetPostion, 0.4f).OnComplete(() =>
                    {
                        poolBeads.Restore(tempObject);
                    });
                    beadScript.enmyBead.currentNode.OnOccupied(false);
                    RefreshBeads();
                    RefreshScore();
                    if (CheckNextBead(turnSlectedBead))
                    {
                        isTakingNextMove = true;
                        if (isWithAI && currentPlayerTurn.GetHashCode() == aiId)
                        {
                            emptyAIBeads.Clear();
                            emptyKillAIBeads.Clear();
                            ActivateMoveAbleEmptyBeads(turnSlectedBead);

                            StartCoroutine(PerformAIMove(true));
                            Debug.LogError("Ai Move");
                        }
                        else
                        {
                            ActivateMoveAbleEmptyBeads(turnSlectedBead);
                        }
                        return;
                    }
                    else
                    {
                        isTakingNextMove = false;
                    }
                }
                RefreshBeads();
                NextTurn();
                // });
            }
        }

        void RefreshBeads()
        {
            for (int i = 0; i < emptyBeads.Count; i++)
            {
                emptyBeads[i].gameObject.SetActive(false);
                emptyBeads[i].enmyBead = null;
                emptyBeads[i].GetComponent<BoxCollider2D>().enabled = true;
            }


        }

        bool CheckNextBead(BeadScript beadScript)
        {
            for (int i = 0; i < beadScript.currentNode.connectedNodesList.Count; i++)
            {
                NodeScript nextNode = beadScript.currentNode.connectedNodesList[i].nextNode;
                NodeScript nextToNextNode = beadScript.currentNode.connectedNodesList[i].nextNextNode;

                if (nextToNextNode != null && !nextToNextNode.isOccupied && nextNode.isOccupied
                    && nextNode.currentBead.playerID != currentPlayerTurn)
                {
                    return true;
                }
            }

            return false;
        }


        public void RemotedMoveSelected(int playerId, int beadId)
        {

            for (int j = 0; j < emptyBeads.Count; j++)
            {
                if (emptyBeads[j].beadId == beadId)
                {
                    OnMoveSelected(emptyBeads[j], true);
                }
            }

        }

        public void RemotedBeadSelected(int playerId, int beadId)
        {
            if (playerId == PLAYERS.PLAYER1.GetHashCode())
            {
                for (int j = 0; j < team1Beads.Count; j++)
                {
                    if (team1Beads[j].beadId == beadId)
                    {
                        ActivateMoveAbleEmptyBeads(team1Beads[j]);
                    }
                }
            }
            else if (playerId == PLAYERS.PLAYER2.GetHashCode())
            {
                for (int j = 0; j < team2Beads.Count; j++)
                {
                    if (team2Beads[j].beadId == beadId)
                    {
                        ActivateMoveAbleEmptyBeads(team2Beads[j]);
                    }
                }
            }
        }
        void ActivateMoveAbleEmptyBeads(BeadScript beadScript)
        {
            turnSlectedBead = beadScript;
            for (int i = 0; i < beadScript.currentNode.connectedNodesList.Count; i++)
            {
                NodeScript nextNode = beadScript.currentNode.connectedNodesList[i].nextNode;
                NodeScript nextToNextNode = beadScript.currentNode.connectedNodesList[i].nextNextNode;

                if (!nextNode.isOccupied && !isTakingNextMove)
                {
                    if (isWithAI && currentPlayerTurn.GetHashCode() == aiId)
                    {
                        emptyAIBeads.Add(nextNode.emptyNode.GetComponent<BeadScript>());
                    }
                    // Debug.Log("Refresh Name " + turnSlectedBead.selectedSprite.name);
                    nextNode.emptyNode.GetComponent<SpriteRenderer>().sprite = turnSlectedBead.selectedSprite;
                    turnSlectedBead.currentNode.emptyNode.GetComponent<SpriteRenderer>().sprite = turnSlectedBead.selectedSprite;

                    turnSlectedBead.currentNode.emptyNode.GetComponent<BoxCollider2D>().enabled = false;
                    turnSlectedBead.currentNode.emptyNode.SetActive(true);
                    nextNode.emptyNode.SetActive(true);
                }
                else if (nextToNextNode != null && !nextToNextNode.isOccupied && nextNode.isOccupied
                    && nextNode.currentBead.playerID != currentPlayerTurn)
                {
                    nextToNextNode.emptyNode.GetComponent<BeadScript>().enmyBead = nextNode.currentBead;
                    // Debug.Log("Refresh Name " + turnSlectedBead.selectedSprite.name);

                    nextToNextNode.emptyNode.GetComponent<SpriteRenderer>().sprite = turnSlectedBead.selectedSprite;

                    nextToNextNode.emptyNode.SetActive(true);
                    turnSlectedBead.currentNode.emptyNode.GetComponent<SpriteRenderer>().sprite = turnSlectedBead.selectedSprite;
                    turnSlectedBead.currentNode.emptyNode.GetComponent<BoxCollider2D>().enabled = false;
                    turnSlectedBead.currentNode.emptyNode.SetActive(true);

                    if (isWithAI && currentPlayerTurn.GetHashCode() == aiId)
                    {
                        emptyKillAIBeads.Add(nextToNextNode.emptyNode.GetComponent<BeadScript>());
                    }


                }
            }
        }


        bool IsCheckMate(List<BeadScript> teamBeads)
        {

            for (int j = 0; j < teamBeads.Count; j++)
            {

                BeadScript beadScript = teamBeads[j];

                if (beadScript.gameObject.activeSelf)
                {
                    for (int i = 0; i < beadScript.currentNode.connectedNodesList.Count; i++)
                    {
                        NodeScript nextNode = beadScript.currentNode.connectedNodesList[i].nextNode;
                        NodeScript nextToNextNode = beadScript.currentNode.connectedNodesList[i].nextNextNode;

                        if (!nextNode.isOccupied)
                        {
                            return false;
                        }
                        else
                        if (nextToNextNode != null && !nextToNextNode.isOccupied && nextNode.isOccupied
                           && nextNode.currentBead.playerID != currentPlayerTurn)
                        {
                            return false;

                        }
                    }
                }
            }

            return true;

        }


        #region AI------------------------------------------------------

        public List<BeadScript> moveAbleBeads = new List<BeadScript>();
        public List<BeadScript> cankillBeads = new List<BeadScript>();


        public List<BeadScript> emptyAIBeads = new List<BeadScript>();
        public List<BeadScript> emptyKillAIBeads = new List<BeadScript>();

        IEnumerator PerformAIMove(bool isNextMove = false, bool waitForTurn = false)
        {

            if (!isNextMove)
            {
                if (waitForTurn)
                {
                    yield return new WaitForSeconds(1.5f);

                }
                emptyAIBeads.Clear();
                emptyKillAIBeads.Clear();
                yield return new WaitForSeconds(1f);
                if (aiId == PLAYERS.PLAYER1.GetHashCode())
                {
                    SetListOfAllMoveableAIBeads(team1Beads);

                }
                else
                {
                    SetListOfAllMoveableAIBeads(team2Beads);

                }
                yield return new WaitForSeconds(0.4f);
                if (cankillBeads.Count > 0)
                {
                    ActivateMoveAbleEmptyBeads(cankillBeads[Random.Range(0, cankillBeads.Count)]);
                }
                else if (moveAbleBeads.Count > 0)
                {
                    ActivateMoveAbleEmptyBeads(moveAbleBeads[Random.Range(0, moveAbleBeads.Count)]);
                }

                yield return new WaitForSeconds(2f);
            }
            else
            {
                yield return new WaitForSeconds(1f);

            }


            if (emptyKillAIBeads.Count > 0)
            {
                OnMoveSelected(emptyKillAIBeads[Random.Range(0, emptyKillAIBeads.Count)]);
            }
            else if (emptyAIBeads.Count > 0)
            {
                OnMoveSelected(emptyAIBeads[Random.Range(0, emptyAIBeads.Count)]);
            }
        }
        void SetListOfAllMoveableAIBeads(List<BeadScript> beadList)
        {
            moveAbleBeads.Clear();
            cankillBeads.Clear();
            for (int j = 0; j < beadList.Count; j++)
            {

                BeadScript beadScript = beadList[j];

                if (beadScript.gameObject.activeSelf && !beadScript.isDead)
                {
                    for (int i = 0; i < beadScript.currentNode.connectedNodesList.Count; i++)
                    {
                        NodeScript nextNode = beadScript.currentNode.connectedNodesList[i].nextNode;
                        NodeScript nextToNextNode = beadScript.currentNode.connectedNodesList[i].nextNextNode;

                        if (!nextNode.isOccupied && !moveAbleBeads.Contains(beadScript))
                        {
                            moveAbleBeads.Add(beadScript);
                        }
                        else
                        if (!cankillBeads.Contains(beadScript) && nextToNextNode != null && !nextToNextNode.isOccupied && nextNode.isOccupied
                           && nextNode.currentBead.playerID != currentPlayerTurn)
                        {
                            cankillBeads.Add(beadScript);


                        }
                    }
                }
            }

        }


        #endregion
    }

    public enum PLAYERS
    {
        EMPTY,
        PLAYER1,
        PLAYER2
    }

}