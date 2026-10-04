using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Snake_Ladder
{
    public class OfflineGamePlayController : MonoBehaviour
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
        public Text turnText;

        public Text player1ScoreTxt;
        public Text player2ScoreTxt;

        public GameObject gameOver;
        public GameObject winUI;
        public GameObject looseUI;

        public int player1Score = 0;
        public int player2Score = 0;


        public BeadScript turnSlectedBead;
        public PLAYERS currentPlayerTurn = PLAYERS.EMPTY;

        public bool IsWithAI;
        private void Start()
        {
            CreateTeamsBeads();
            StartGame();
        }

        void StartGame()
        {
            if (Random.Range(0, 1000) < 500)
            {
                currentPlayerTurn = PLAYERS.PLAYER1;
            }
            else
            {
                currentPlayerTurn = PLAYERS.PLAYER2;
                if (IsWithAI)
                {
                    StartCoroutine(PerformAIMove());

                }
            }
            //currentPlayerTurn = PLAYERS.PLAYER1;

            turnText.text = currentPlayerTurn.ToString();
            RefreshScore();
        }

        public void NextTurn()
        {
            if (currentPlayerTurn == PLAYERS.PLAYER1)
            {
                currentPlayerTurn = PLAYERS.PLAYER2;
                if (!IsCheckMate(team2Beads))
                {
                    if (IsWithAI)
                    {
                        StartCoroutine(PerformAIMove());

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

                if (IsCheckMate(team1Beads))
                {
                    AnnounceWinOrLoose(false);
                }


            }
            turnText.text = currentPlayerTurn.ToString();
            RefreshScore();

        }


        void RefreshScore()
        {
            if (player1Score >= 12)
            {
                AnnounceWinOrLoose(true);
            }
            else if (player2Score >= 12)
            {
                AnnounceWinOrLoose(false);
            }
            player1ScoreTxt.text = "Player 1: " + player1Score.ToString();
            player2ScoreTxt.text = "Player 2:" + player2Score.ToString();
        }


        void AnnounceWinOrLoose(bool isWin)
        {
            gameOver.SetActive(true);
            winUI.SetActive(isWin);
            looseUI.SetActive(!isWin);
        }

        void CreateTeamsBeads()
        {
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
                team1Nodes[i].OnOccupied(true);
                team1Nodes[i].currentBead = beadScript;
                team1Beads.Add(beadScript);
            }
            for (int i = 0; i < team2Nodes.Count; i++)
            {
                GameObject obj = GameObject.Instantiate(beadTeamPrefab2);
                obj.transform.position = team2Nodes[i].transform.position + new Vector3(0, 0, -2);
                obj.transform.parent = beadsParentTransform;
                BeadScript beadScript = obj.GetComponent<BeadScript>();
                beadScript.playerID = PLAYERS.PLAYER2;
                beadScript.currentNode = team2Nodes[i];
                team2Nodes[i].OnOccupied(true);
                team2Nodes[i].currentBead = beadScript;
                team2Beads.Add(beadScript);
            }
        }
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit2D rayHit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));

                BeadScript beadScript = rayHit.transform.gameObject.GetComponent<BeadScript>();

                Debug.Log("currentPlayerTurn " + currentPlayerTurn.ToString());
                if (beadScript && beadScript.playerID == currentPlayerTurn && beadScript.playerID != PLAYERS.EMPTY)
                {
                    RefreshBeads();
                    ActivateMoveAbleEmptyBeads(beadScript);
                }

                OnMoveSelected(beadScript);

                Debug.Log(rayHit.transform.name);

            }
        }


        public void OnMoveSelected(BeadScript beadScript)
        {
            if (turnSlectedBead && beadScript && beadScript.playerID == PLAYERS.EMPTY)
            {

                turnSlectedBead.currentNode.OnOccupied(false);

                turnSlectedBead.transform.position = beadScript.transform.position;
                turnSlectedBead.currentNode = beadScript.currentNode;
                turnSlectedBead.currentNode.OnOccupied(true);
                turnSlectedBead.currentNode.currentBead = turnSlectedBead;

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
                    beadScript.enmyBead.gameObject.SetActive(false);
                    beadScript.enmyBead.currentNode.OnOccupied(false);
                    RefreshBeads();
                    RefreshScore();
                    if (CheckNextBead(turnSlectedBead))
                    {
                        if (IsWithAI && currentPlayerTurn == PLAYERS.PLAYER2)
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
                }
                RefreshBeads();
                NextTurn();
            }
        }

        void RefreshBeads()
        {
            for (int i = 0; i < emptyBeads.Count; i++)
            {
                emptyBeads[i].gameObject.SetActive(false);
                emptyBeads[i].enmyBead = null;
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

        void ActivateMoveAbleEmptyBeads(BeadScript beadScript)
        {
            turnSlectedBead = beadScript;
            for (int i = 0; i < beadScript.currentNode.connectedNodesList.Count; i++)
            {
                NodeScript nextNode = beadScript.currentNode.connectedNodesList[i].nextNode;
                NodeScript nextToNextNode = beadScript.currentNode.connectedNodesList[i].nextNextNode;

                if (!nextNode.isOccupied)
                {
                    if (IsWithAI && currentPlayerTurn == PLAYERS.PLAYER2)
                    {
                        emptyAIBeads.Add(nextNode.emptyNode.GetComponent<BeadScript>());
                    }
                    nextNode.emptyNode.SetActive(true);
                }
                else if (nextToNextNode != null && !nextToNextNode.isOccupied && nextNode.isOccupied
                    && nextNode.currentBead.playerID != currentPlayerTurn)
                {
                    nextToNextNode.emptyNode.GetComponent<BeadScript>().enmyBead = nextNode.currentBead;
                    nextToNextNode.emptyNode.SetActive(true);
                    if (IsWithAI && currentPlayerTurn == PLAYERS.PLAYER2)
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

        IEnumerator PerformAIMove(bool isNextMove = false)
        {

            if (!isNextMove)
            {
                emptyAIBeads.Clear();
                emptyKillAIBeads.Clear();
                yield return new WaitForSeconds(0.15f);
                SetListOfAllMoveableAIBeads();
                yield return new WaitForSeconds(0.25f);
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
        void SetListOfAllMoveableAIBeads()
        {
            moveAbleBeads.Clear();
            cankillBeads.Clear();
            for (int j = 0; j < team2Beads.Count; j++)
            {

                BeadScript beadScript = team2Beads[j];

                if (beadScript.gameObject.activeSelf)
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
}