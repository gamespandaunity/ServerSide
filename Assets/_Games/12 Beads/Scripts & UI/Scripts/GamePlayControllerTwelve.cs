using DG.Tweening;
using Mirror;
using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityExtensions;

namespace Twelve
{
    public class GamePlayControllerTwelve : NetworkBehaviour
    {
        public List<NodeScriptTwelve> allNodes;

        public List<NodeScriptTwelve> team1Nodes;
        public List<NodeScriptTwelve> team2Nodes;

        public List<BeadScriptTwelve> team1Beads;
        public List<BeadScriptTwelve> team2Beads;
        public List<BeadScriptTwelve> emptyBeads;

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
        public TextMeshProUGUI countDownGameTimer;

        public int player1Score = 0;
        public int player2Score = 0;


        public BeadScriptTwelve turnSlectedBead;
        private BeadScriptTwelve lastMovedBeadP1;
        private BeadScriptTwelve lastMovedBeadP2;
        private BeadScriptTwelve pendingDestination;
        private Vector3 pendingDestinationScale;
        private TwelveState offlineRulesState;
        private bool offlineRulesReady;
        public static PLAYERS currentPlayerTurn = PLAYERS.EMPTY;

        public bool isWithAI;
        public static bool isOnlineMultiplayer;

        public MultiPlayerGameManagerTwelve multiPlayerGame;
        public static int myId;
        public static int aiId;
        public GameObject LoadingPanel;
        public GameObject DisconnectedPanel;
        public TextMeshProUGUI DisconnectedText;

        public Sprite redSprite, redUpSprite;
        public Sprite greenSprite, greenUpSprite;
        public TurnAnnouncementTwelve turnAnnouncement;

        public GameObject[] offlineTurnHighLighter;

        private bool isTakingNextMove;
        private Coroutine aiMoveCoroutine;
        public TurnTimerTwelve turnTimer;

        public PoolTwelve poolBeads;
        //Wasi  public SpritesHOlder scriptable;

        public UserModel userProfile;
        public PlayerProfile OpponentProfile;
        public Texture2D image;
        private static GamePlayControllerTwelve _instance;

        public static GamePlayControllerTwelve instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogWarning("GameManager instance not found in scene!");
                }
                return _instance;
            }
        }
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(_instance.gameObject);
            }

            _instance = this;

            // Reset all static variables to prevent stale state from previous games
            beadsCreated = false;
            currentPlayerTurn = PLAYERS.EMPTY;
            isOnlineMultiplayer = false;
            ResultManager.GameSpawnedFinished = false;
        }
        void OnDestroy()
        {
            TurnTimerTwelve.TunrnTimerHasExpired -= OnTurnTimerExipred;
            if (_instance == this)
                _instance = null;
        }
        //Wasi  [Button]
        public void UpdateData()
        {
            userProfile = staticVariables.UserProfiledata;
            OpponentProfile = staticVariables.OpponetProfile;
            image = staticVariables.opponentImage;
        }
        [SerializeField] private GameObject speakerObj;
        [SerializeField] private GameObject micObj;
        private void Start()
        {
            var mode = Snake_Ladder.GameManager.instance.currentGameMode;
            Debug.Log("Current Mode :" + mode);

            if (mode == Snake_Ladder.GameManager.GameMode.Against_Ai)
            {
                Debug.Log("Current Mode AI");
                isWithAI = true;
                isOnlineMultiplayer = false;
            }
            else if (mode == Snake_Ladder.GameManager.GameMode.Against_LocalPlayer)
            {
                Debug.Log("Current Mode localPlayer");
                isWithAI = false;
                isOnlineMultiplayer = false;
            }
            else if (mode == Snake_Ladder.GameManager.GameMode.Against_OnlineFriend)
            {
                Debug.Log("Current Mode Against_OnlineFriend");
                isWithAI = false;
                isOnlineMultiplayer = true;
            }
            else if (mode == Snake_Ladder.GameManager.GameMode.Against_RandomPlayer)
            {
                Debug.Log("Current Mode Against_RandomPlayer");
                isWithAI = false;
                isOnlineMultiplayer = true;
            }

            // Safety override: if Mirror is already active, this MUST be an online match.
            // Handles stale GameMode from a previous session. Also clear isWithAI to keep state consistent.
            if (NetworkClient.active || NetworkServer.active)
            {
                isOnlineMultiplayer = true;
                isWithAI = false;
            }


            if (!isOnlineMultiplayer)
            {
                CreateTeamsBeads();
                TwelveRulesEngine.CreateInitialState(ref offlineRulesState);
                offlineRulesReady = true;
                ValidateTwelveTopologyShadow();
                StartCoroutine(StartCountdown());
            }
            else
            {
                if (NetworkServer.active)
                    this.DelayUntil(() => NetworkGameManager.Instance && NetworkGameManager.Instance.currentPlayerCount >= 2, () =>
                    {
                        MirrorCreateTeamsBeads();
                    });
            }


            if (!isOnlineMultiplayer)
            {
                Debug.Log("!isOnlineMultiplayer");
                myId = PLAYERS.PLAYER1.GetHashCode();
                aiId = PLAYERS.PLAYER2.GetHashCode();
                TurnTimerTwelve.TunrnTimerHasExpired -= OnTurnTimerExipred;
                TurnTimerTwelve.TunrnTimerHasExpired += OnTurnTimerExipred;
                StartGame();
                turnTimer.ResetRound();
            }
            else
            {
                "ONMultiplayerTurnTimerExplained".Show();
                TurnTimerTwelve.TunrnTimerHasExpired -= OnTurnTimerExipred;
                TurnTimerTwelve.TunrnTimerHasExpired += OnTurnTimerExipred;
                // The server starts the online turn timer after the toss establishes a turn.
                // Clients only render the synced value and never start/reset it themselves.
                turnTimer.StopTimer();
            }
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private IEnumerator StartCountdown()
        {
            float remainingTime = 300;

            while (remainingTime > 0)
            {
                // Update timer text
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                countDownGameTimer.text = string.Format("{0:00}:{1:00}", minutes, seconds);

                // Update text color
                if (remainingTime > 180)
                {
                    countDownGameTimer.color = Color.green;
                }
                else if (remainingTime <= 180 && remainingTime > 60)
                {
                    countDownGameTimer.color = Color.white;
                }
                else
                {
                    countDownGameTimer.color = Color.red;
                }

                // Wait for 1 second
                yield return new WaitForSeconds(1);

                // Decrease time
                remainingTime--;
            }

            // Final text update
            countDownGameTimer.text = "00:00";
            countDownGameTimer.color = Color.red;

            // Call the function when the timer ends
            OnTimerEnd();
        }



        public void OnTurnTimerExipred()
        {
            StopAiMove();
            isTakingNextMove = false;
            turnSlectedBead = null;
            RefreshBeads();

            if (!isOnlineMultiplayer)
            {
                if (offlineRulesReady)
                    TwelveRulesEngine.ExpireTurn(ref offlineRulesState);
                NextTurn();
            }
            else
            {
                // Presentation only. ServerExpireTurn() already advanced the authoritative
                // turn before RpcOnTurnTimerEnd invoked this callback.
                Debug.Log("OnTurnTimerExipred");
            }
        }



        public void StartGameInCaseOfDisconnect()
        {
            if (isWithAI && aiId == currentPlayerTurn.GetHashCode())
            {
                StartAiMove(false, true);
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
            // Reset consecutive kill flag at the start of each turn to prevent stuck state
            isTakingNextMove = false;

            // NOTE: Do NOT clear moveHistory here. StartGame() runs on every turn change
            // (via SetPlayerTurn -> RpcSetTurnData), so clearing here would wipe the A-B-A-B
            // history every turn and make the restriction ineffective. Fresh bead spawns
            // already start with empty history; rematch flows should handle their own reset.

            Debug.LogError($"Is Online Multiplayer {isOnlineMultiplayer}");
            if (!isOnlineMultiplayer)
            {
                if (Random.Range(0, 1000) < 500)
                {
                    currentPlayerTurn = PLAYERS.PLAYER1;
                    Debug.Log("currentPlayerTurn" + currentPlayerTurn);
                    if (isWithAI && aiId == PLAYERS.PLAYER1.GetHashCode())
                    {
                        StartAiMove(false, true);
                    }
                }
                else
                {
                    currentPlayerTurn = PLAYERS.PLAYER2;
                    Debug.Log("currentPlayerTurn" + currentPlayerTurn);
                    if (isWithAI && aiId == PLAYERS.PLAYER2.GetHashCode())
                    {
                        StartAiMove(false, true);
                    }
                }

                turnAnnouncement.SetTurnData(false, isWithAI, currentPlayerTurn);
                if (offlineRulesReady) offlineRulesState.turn = currentPlayerTurn;

                if ((PLAYERS)aiId == PLAYERS.PLAYER1)
                {
                    Debug.Log("PLAYERS)aiId == PLAYERS.PLAYER1");
                    offlineTurnHighLighter[0].SetActive(currentPlayerTurn == PLAYERS.PLAYER2 ? true : false);
                    offlineTurnHighLighter[1].SetActive(currentPlayerTurn == PLAYERS.PLAYER1 ? true : false);
                }
                else
                {
                    Debug.Log("!PLAYERS)aiId == PLAYERS.PLAYER1");
                    offlineTurnHighLighter[0].SetActive(currentPlayerTurn == PLAYERS.PLAYER1 ? true : false);
                    offlineTurnHighLighter[1].SetActive(currentPlayerTurn == PLAYERS.PLAYER2 ? true : false);
                }

            }
            else
            {
                if (NetworkServer.active == true)
                    return;
                LoadingPanel.SetActive(false);

                myId.Show("myId");
                currentPlayerTurn.GetHashCode().Show("CurrentPlayerTurn");
                if (myId != currentPlayerTurn.GetHashCode())
                {
                    //turnText.text = GameManager.instance.OpponentName.ToString();
                    Player2Timer.SetActive(true);
                    Player1Timer.SetActive(false);
                    turnAnnouncement.ApplyTurnUI(true, isWithAI, currentPlayerTurn, false);

                }
                else
                {

                    Player2Timer.SetActive(false);
                    Player1Timer.SetActive(true);
                    // turnText.text = GameManager.instance.UserName.ToString();
                    turnAnnouncement.ApplyTurnUI(true, isWithAI, currentPlayerTurn, true);

                }

                if (turnSlectedBead)
                {
                    if (isOnlineMultiplayer)
                    {
                        //   turnSlectedBead.MP_currentNode.emptyNode.SetActive(false);
                        turnSlectedBead.currentNode.emptyNode.SetActive(false);

                    }
                    else
                    {
                        turnSlectedBead.currentNode.emptyNode.SetActive(false);
                    }

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
            isTakingNextMove = false;
            turnSlectedBead = null;
            RefreshBeads();

            if (!isOnlineMultiplayer)
            {
                if (currentPlayerTurn == PLAYERS.PLAYER1)
                {
                    currentPlayerTurn = PLAYERS.PLAYER2;
                    if (!IsCheckMate(team2Beads))
                    {
                        if (isWithAI && aiId == PLAYERS.PLAYER2.GetHashCode())
                        {
                            StartAiMove(false, true);
                        }
                    }
                    else
                    {
                        countDownGameTimer.gameObject.SetActive(false);
                        //AnnounceWinOrLoose(true);
                        //FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId : aiId);
                        if (ResultManager.GameSpawnedFinished == false)
                        {
                            ResultManager.GameSpawnedFinished = true;
                            GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                            if (enemyPrefab != null)
                            {
                                "1".Show();
                                // Spawn at position (0,0,0)
                                var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                                gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId.ToString() : aiId.ToString());
                            }
                            else
                            {
                                Debug.LogError("WinLose GameManager prefab not found!");
                            }
                        }
                    }
                }
                else
                {
                    currentPlayerTurn = PLAYERS.PLAYER1;

                    if (!IsCheckMate(team1Beads))
                    {
                        if (isWithAI && aiId == PLAYERS.PLAYER1.GetHashCode())
                        {
                            StartAiMove(false, true);
                        }
                    }
                    else
                    {
                        countDownGameTimer.gameObject.SetActive(false);

                        //AnnounceWinOrLoose(false);
                        //FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(false, currentPlayerTurn == PLAYERS.PLAYER1 ? myId : aiId);
                        if (ResultManager.GameSpawnedFinished == false)
                        {
                            ResultManager.GameSpawnedFinished = true;
                            GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                            if (enemyPrefab != null)
                            {
                                "1".Show();
                                // Spawn at position (0,0,0)
                                var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                                gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, currentPlayerTurn == PLAYERS.PLAYER1 ? myId.ToString() : aiId.ToString());
                            }
                            else
                            {
                                Debug.LogError("WinLose GameManager prefab not found!");
                            }
                        }
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
                // Online turns are advanced only by the server move transaction or timer.
                Debug.LogWarning("[12Beads] Ignoring client-side NextTurn in online play");
            }
            // Debug.LogError("Next Turn Game Play");

            RefreshScore();



        }

        public void Player2Win()
        {
            //FindObjectOfType<ResultManagerFor12Bead>().CmdWinPlayer(true, aiId);
        }
        public void PlayerWin_AIMode()
        {
            // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, myId);
        }
        public void AIWin_AIMode()
        {
            // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, aiId);
        }

        public void OnTimerEnd()
        {
            Debug.Log("Timer has ended! Running the specified function.");
            if (staticVariables.isGuest)
            {
                
            }
            if (!isOnlineMultiplayer)
            {
                if (offlineRulesReady)
                {
                    TwelveMatchResult rulesResult = TwelveRulesEngine.ExpireMatch(ref offlineRulesState);
                    player1Score = offlineRulesState.player1Score;
                    player2Score = offlineRulesState.player2Score;
                    Debug.Log($"[12Bead] Offline match expiry outcome={rulesResult.outcome} P1={player1Score} P2={player2Score}");
                }
                // The two offline draw branches (equal score, and 0-0) live only in the
                // client build. They call ResultManager.AIDraw(), which this repo's
                // _Network/ResultManager.cs does not have — the two ResultManager copies
                // have drifted ~190 lines apart and merging them is not this change's job.
                // Nothing is lost: a headless build never runs offline/AI mode, and AIDraw()
                // is pure client UI (profile picture, guest coins, SpritesManager).
                // This is the ONLY place the two GamePlayControllerTwelve copies are allowed
                // to differ beyond the Text/TextMeshProUGUI field types.

                if (player1Score > player2Score)
                {
                    countDownGameTimer.gameObject.SetActive(false);
                    // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, myId);
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, myId.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                }
                else
                {
                    countDownGameTimer.gameObject.SetActive(false);
                    // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, aiId);
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, aiId.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                }
            }
            else
            {
                // Unreachable online: the match clock is server-owned, and the server now
                // decides and settles the timeout itself in ServerFinalize(Timeout) before
                // sending RpcMatchFinalized. OnTimerEnd() is reached only from the offline
                // StartCountdown() coroutine, so nothing should spawn a result here.
                Debug.LogWarning("[12Beads] OnTimerEnd() reached in online mode — ignoring; " +
                                 "the server owns the timeout result.");
            }
        }

        // SpawnTwelveMultiplayerResult() was removed. It decided the online timeout result
        // locally (draw on equal score, otherwise higher score wins) on whichever client
        // got there first. That decision now belongs to the server:
        // MultiPlayerGameManagerTwelve.ServerFinalize() applies the same rules to its own
        // scores, settles with the backend, and sends RpcMatchFinalized for the UI.

        // True only on the Edgegap headless build (server with no local client).
        // Host mode is deliberately NOT covered: a host has a real HUD and a logged-in
        // user, so it must keep running the presentation pass below.
        private static bool IsDedicatedServer => NetworkServer.active && !NetworkClient.active;

        public void RefreshScore()
        {
            //Wasi player1Score.Show("player1Score");
            //Wasi player2Score.Show("player2Score");

            // RefreshScore() runs every turn via NextTurn(), and on the headless server that
            // reached staticVariables.UserProfiledata.user — which is never populated there,
            // because a dedicated build never logs anyone in. That threw a NullReference on
            // every single turn (win-check line and the player-name lines below).
            // The server keeps its authoritative scores on MultiPlayerGameManagerTwelve; it
            // has no HUD to refresh and must not run the client-side win trigger at all.
            if (IsDedicatedServer) return;

            if (!isOnlineMultiplayer)
            {
                if (player1Score >= GameConstants.SCORE_TO_WIN)
                {
                    if (isWithAI)
                    {
                        if ((PLAYERS)aiId == PLAYERS.PLAYER1)
                        {
                            //AnnounceWinOrLoose(false);
                            countDownGameTimer.gameObject.SetActive(false);
                            // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(false, currentPlayerTurn == PLAYERS.PLAYER1 ? myId : aiId);
                            if (ResultManager.GameSpawnedFinished == false)
                            {
                                ResultManager.GameSpawnedFinished = true;
                                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                                if (enemyPrefab != null)
                                {
                                    "1".Show();
                                    // Spawn at position (0,0,0)
                                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                                    gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, currentPlayerTurn == PLAYERS.PLAYER1 ? myId.ToString() : aiId.ToString());
                                }
                                else
                                {
                                    Debug.LogError("WinLose GameManager prefab not found!");
                                }
                            }
                        }
                        else
                        {
                            //AnnounceWinOrLoose(true);
                            countDownGameTimer.gameObject.SetActive(false);
                            //  FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId : aiId);
                            if (ResultManager.GameSpawnedFinished == false)
                            {
                                ResultManager.GameSpawnedFinished = true;
                                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                                if (enemyPrefab != null)
                                {
                                    "1".Show();
                                    // Spawn at position (0,0,0)
                                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                                    gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId.ToString() : aiId.ToString());
                                }
                                else
                                {
                                    Debug.LogError("WinLose GameManager prefab not found!");
                                }
                            }
                        }
                    }
                    else
                    {
                        winLostText.text = "Player One Won....";
                        // AnnounceWinOrLoose(true);
                        countDownGameTimer.gameObject.SetActive(false);
                        // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId : aiId);
                        if (ResultManager.GameSpawnedFinished == false)
                        {
                            ResultManager.GameSpawnedFinished = true;
                            GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                            if (enemyPrefab != null)
                            {
                                "1".Show();
                                // Spawn at position (0,0,0)
                                var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                                gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId.ToString() : aiId.ToString());
                            }
                            else
                            {
                                Debug.LogError("WinLose GameManager prefab not found!");
                            }
                        }
                    }
                }
                else if (player2Score >= GameConstants.SCORE_TO_WIN)
                {
                    if (isWithAI)
                    {
                        if ((PLAYERS)aiId == PLAYERS.PLAYER2)
                        {
                            //AnnounceWinOrLoose(false);
                            countDownGameTimer.gameObject.SetActive(false);
                            // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(false, currentPlayerTurn == PLAYERS.PLAYER1 ? myId : aiId);
                            if (ResultManager.GameSpawnedFinished == false)
                            {
                                ResultManager.GameSpawnedFinished = true;
                                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                                if (enemyPrefab != null)
                                {
                                    "1".Show();
                                    // Spawn at position (0,0,0)
                                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                                    gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, currentPlayerTurn == PLAYERS.PLAYER1 ? myId.ToString() : aiId.ToString());
                                }
                                else
                                {
                                    Debug.LogError("WinLose GameManager prefab not found!");
                                }
                            }
                        }
                        else
                        {
                            //AnnounceWinOrLoose(true);
                            countDownGameTimer.gameObject.SetActive(false);
                            // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId : aiId);
                            if (ResultManager.GameSpawnedFinished == false)
                            {
                                ResultManager.GameSpawnedFinished = true;
                                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                                if (enemyPrefab != null)
                                {
                                    "1".Show();
                                    // Spawn at position (0,0,0)
                                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                                    gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId.ToString() : aiId.ToString());
                                }
                                else
                                {
                                    Debug.LogError("WinLose GameManager prefab not found!");
                                }
                            }
                        }
                    }
                    else
                    {
                        winLostText.text = "Player Two Won....";
                        countDownGameTimer.gameObject.SetActive(false);
                        // FindObjectOfType<ResultManagerFor12Bead>().HandlePlayerWinState(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId : aiId);
                        if (ResultManager.GameSpawnedFinished == false)
                        {
                            ResultManager.GameSpawnedFinished = true;
                            GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                            if (enemyPrefab != null)
                            {
                                "1".Show();
                                // Spawn at position (0,0,0)
                                var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                                gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, currentPlayerTurn == PLAYERS.PLAYER1 ? myId.ToString() : aiId.ToString());
                            }
                            else
                            {
                                Debug.LogError("WinLose GameManager prefab not found!");
                            }
                        }
                        //AnnounceWinOrLoose(true);
                    }
                }
            }
            else
            {
                // The score-12 win used to be decided right here, on whichever client got
                // there first, via CmdWinPlayer(true, staticVariables.UserProfiledata.user._id).
                // Two problems: the winner was self-declared, and on the headless server this
                // line threw a NullReference because a dedicated build never logs anyone in.
                // The server-side committed move now checks SCORE_TO_WIN
                // on the server as each capture lands and calls ServerFinalize() itself.
            }

            if (isOnlineMultiplayer)
            {
                if (myId == PLAYERS.PLAYER1.GetHashCode())
                {
                    player1ScoreTxt.text = $"Score: <color=green> {multiPlayerGame.Myplayer1Score.ToString()}</color>";
                    player2ScoreTxt.text = $"Score: <color=green> {multiPlayerGame.Myplayer2Score.ToString()}</color>";
                    player1Score = multiPlayerGame.Myplayer1Score;
                    player2Score = multiPlayerGame.Myplayer2Score;

                }
                else
                {
                    player1ScoreTxt.text = $"Score: <color=green> {multiPlayerGame.Myplayer2Score.ToString()}</color>";
                    player2ScoreTxt.text = $"Score: <color=green> {multiPlayerGame.Myplayer1Score.ToString()}</color>";
                    player1Score = multiPlayerGame.Myplayer2Score;
                    player2Score = multiPlayerGame.Myplayer1Score;
                }
                player1NameTxt.text = $"{staticVariables.UserProfiledata.user.first_name} {staticVariables.UserProfiledata.user.last_name}";
                player2NameTxt.text = $"{staticVariables.OpponetProfile.userName}";
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
                player1NameTxt.text = $"{staticVariables.UserProfiledata.user.first_name} {staticVariables.UserProfiledata.user.last_name}";
                //  player2NameTxt.text = $"{staticVariables.OpponetProfile.userName}";
                //player1NameTxt.text = "You";

                //if (isWithAI)
                //{
                //    player2NameTxt.text = $"Computer";
                //}

                //else
                //    player2NameTxt.text = $"Player 2";

            }

        }

        private void OnEnable()
        {
            MirrorNetwork.OnWinCall += AnnounceWinner;
            // Add your event subscriptions here
        }

        private void OnDisable()
        {
            MirrorNetwork.OnWinCall -= AnnounceWinner;
            TurnTimerTwelve.TunrnTimerHasExpired -= OnTurnTimerExipred;
        }

        // Raised only on the player whose opponent dropped (NetworkGameManager fires OnWinCall
        // with the leaver excluded). Explicit Leave-button forfeits are settled by the generic
        // NetworkGameManager server path; this remains only as legacy presentation fallback.
        private void AnnounceWinner(string reason)
        {
            if (ResultManager.GameSpawnedFinished) return;
            ResultManager.GameSpawnedFinished = true;

            GameObject prefab = Resources.Load<GameObject>("WinLoseGameManager");
            if (prefab == null)
            {
                Debug.LogError("WinLose GameManager prefab not found!");
                return;
            }

            var resultManager = Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<ResultManager>();
            resultManager.HandleGameResultAltMultiplayer(true, staticVariables.UserProfiledata.user._id.ToString());
        }
        void AnnounceWinOrLoose(bool isWin)
        {
            gameOver.SetActive(true);
            winUI.SetActive(isWin);
            looseUI.SetActive(!isWin);
        }
        public static bool beadsCreated = false;
        public void MirrorCreateTeamsBeads()
        {
            if (beadsCreated)
            {
                Debug.Log("[CreateTeamsBeads] Beads already created, skipping...");
                return;
            }
            Debug.Log("[CreateTeamsBeads]...");

            // Only server should instantiate objects

            if (!NetworkServer.active)
            {
                Debug.Log("[CreateTeamsBeads] Client skipping bead creation - waiting for server spawn");
                return;
            }
            team1Beads = new List<BeadScriptTwelve>();
            team2Beads = new List<BeadScriptTwelve>();
            emptyBeads = new List<BeadScriptTwelve>();

            // Create empty nodes
            for (int i = 0; i < allNodes.Count; i++)
            {
                // Server spawns the object across network
                GameObject obj = Instantiate(emptyNode);
                NetworkServer.Spawn(obj);

                obj.transform.position = allNodes[i].transform.position + new Vector3(0, 0, -2);
                obj.transform.parent = beadsParentTransform;
                BeadScriptTwelve beadScript = obj.GetComponent<BeadScriptTwelve>();
                allNodes[i].emptyNode = obj;
                beadScript.SetVisible(false);
                beadScript.playerID = PLAYERS.EMPTY;
                beadScript.currentBeadId = i;
                beadScript.beadId = i;
                beadScript.currentNodeId = allNodes[i].nodeNo;
                beadScript.currentNode = allNodes[i];
                emptyBeads.Add(beadScript);
            }

            // Create team 1 beads
            for (int i = 0; i < team1Nodes.Count; i++)
            {
                // Server spawns the object across network
                GameObject obj = Instantiate(beadTeamPrefab1);
                NetworkServer.Spawn(obj);

                obj.transform.position = team1Nodes[i].transform.position + new Vector3(0, 0, -2);
                obj.transform.parent = beadsParentTransform;
                BeadScriptTwelve beadScript = obj.GetComponent<BeadScriptTwelve>();
                beadScript.playerID = PLAYERS.PLAYER1;
                beadScript.currentNode = team1Nodes[i];
                beadScript.currentBeadId = i;
                beadScript.beadId = i;

                beadScript.currentNodeId = team1Nodes[i].nodeNo;
                team1Nodes[i].OnOccupied(true);
                team1Nodes[i].currentBead = beadScript;
                team1Beads.Add(beadScript);
            }

            // Create team 2 beads
            for (int i = 0; i < team2Nodes.Count; i++)
            {
                // Server spawns the object across network
                GameObject obj = Instantiate(beadTeamPrefab2);
                NetworkServer.Spawn(obj);

                obj.transform.position = team2Nodes[i].transform.position + new Vector3(0, 0, -2);
                obj.transform.parent = beadsParentTransform;
                BeadScriptTwelve beadScript = obj.GetComponent<BeadScriptTwelve>();
                beadScript.playerID = PLAYERS.PLAYER2;
                beadScript.currentNode = team2Nodes[i];
                beadScript.currentBeadId = i;
                beadScript.beadId = i;
                beadScript.currentNodeId = team2Nodes[i].nodeNo;

                team2Nodes[i].OnOccupied(true);
                team2Nodes[i].currentBead = beadScript;
                team2Beads.Add(beadScript);
            }

            beadsCreated = true;
        }
        void CreateTeamsBeads()
        {
            if (isOnlineMultiplayer && beadsCreated)
            {
                Debug.Log("[CreateTeamsBeads] Beads already created in online mode, skipping...");
                return;
            }

            team1Beads = new List<BeadScriptTwelve>();
            team2Beads = new List<BeadScriptTwelve>();
            emptyBeads = new List<BeadScriptTwelve>();
            //Debug.Log("Creating Team With " + Photon.Pun.PhotonNetwork.IsMasterClient);

            for (int i = 0; i < allNodes.Count; i++)
            {
                GameObject obj = GameObject.Instantiate(emptyNode);
                obj.transform.position = allNodes[i].transform.position + new Vector3(0, 0, -2);
                obj.transform.parent = beadsParentTransform;
                BeadScriptTwelve beadScript = obj.GetComponent<BeadScriptTwelve>();
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
                BeadScriptTwelve beadScript = obj.GetComponent<BeadScriptTwelve>();
                beadScript.playerID = PLAYERS.PLAYER1;
                beadScript.currentNode = team1Nodes[i];
                beadScript.beadId = i;

                team1Nodes[i].OnOccupied(true);
                team1Nodes[i].currentBead = beadScript;
                team1Beads.Add(beadScript);

                if (isOnlineMultiplayer)
                {
                    //if (!Photon.Pun.PhotonNetwork.IsMasterClient)
                    //{
                    //    Debug.Log("Creating Team With inside" + Photon.Pun.PhotonNetwork.IsMasterClient);

                    //    beadScript.selectedSprite = redUpSprite;
                    //    obj.GetComponent<SpriteRenderer>().sprite = redSprite;
                    //}
                }
            }
            for (int i = 0; i < team2Nodes.Count; i++)
            {
                GameObject obj = GameObject.Instantiate(beadTeamPrefab2);
                obj.transform.position = team2Nodes[i].transform.position + new Vector3(0, 0, -2);
                obj.transform.parent = beadsParentTransform;
                BeadScriptTwelve beadScript = obj.GetComponent<BeadScriptTwelve>();
                beadScript.playerID = PLAYERS.PLAYER2;
                beadScript.currentNode = team2Nodes[i];
                beadScript.beadId = i;

                team2Nodes[i].OnOccupied(true);
                team2Nodes[i].currentBead = beadScript;
                team2Beads.Add(beadScript);

                if (isOnlineMultiplayer)
                {
                    beadsCreated = true;
                }
            }

        }
        void Update()
        {
            if (isOnlineMultiplayer && multiPlayerGame != null && multiPlayerGame.IsMovePending)
                return;

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

                if (rayHit.transform == null) return;

                BeadScriptTwelve beadScript = rayHit.transform.gameObject.GetComponent<BeadScriptTwelve>();

                if (beadScript == null) return;

                Debug.Log("beadScript " + beadScript.name.ToString());
                Debug.Log("beadScript " + beadScript.playerID.ToString());
                Debug.Log("currentPlayerTurn " + currentPlayerTurn.ToString());
                if (beadScript.playerID == currentPlayerTurn && beadScript.playerID != PLAYERS.EMPTY && !isTakingNextMove)
                {

                    RefreshBeads();
                    ActivateMoveAbleEmptyBeads(beadScript);

                }

                OnMoveSelected(beadScript);

                Debug.Log(rayHit.transform.name);

            }
        }

        GameObject tempObject;
        Vector3 targetPostion = new Vector3(0, 10, 0);
        public void OnMoveSelected(BeadScriptTwelve beadScript, bool isRemote = false)
        {
            // Capture chain state BEFORE any move modifies it — used to skip history recording
            // for capture chain continuations (otherwise legitimate long chains trip the A-B-A-B detector).
            bool wasInCaptureChain = isTakingNextMove;
            if (turnSlectedBead && beadScript && beadScript.playerID == PLAYERS.EMPTY)
            {

                if (!isRemote && isOnlineMultiplayer && multiPlayerGame)
                {
                    int fromNode = allNodes.IndexOf(turnSlectedBead.currentNode);
                    int toNode = allNodes.IndexOf(beadScript.currentNode);
                    if (fromNode < 0 || toNode < 0 || fromNode > byte.MaxValue || toNode > byte.MaxValue)
                    {
                        Debug.LogError("[12Beads] Cannot submit move: node is not in allNodes");
                        return;
                    }

                    SetTwelveMovePending(beadScript);
                    multiPlayerGame.SubmitTwelveMove((byte)fromNode, (byte)toNode);
                    return; // no optimistic movement; wait for RpcTwelveMoveCommitted
                }

                TwelveApplyResult offlineApply = default;
                bool appliedByRules = false;
                if (!isRemote && !isOnlineMultiplayer)
                {
                    int fromNode = allNodes.IndexOf(turnSlectedBead.currentNode);
                    int toNode = allNodes.IndexOf(beadScript.currentNode);
                    bool legacyAllowed = LegacyWouldAllowOfflineMove(turnSlectedBead, beadScript);
                    offlineApply = TwelveRulesEngine.TryApplyMove(ref offlineRulesState,
                        currentPlayerTurn, (byte)fromNode, (byte)toNode);
                    appliedByRules = offlineApply.applied;
                    if (legacyAllowed != offlineApply.applied)
                        Debug.LogError($"[12Bead] Rules shadow mismatch offline from={fromNode} to={toNode} legacy={legacyAllowed} engine={offlineApply.applied} reason={offlineApply.rejectReason}");
                    if (!offlineApply.applied)
                    {
                        const string msg = "That move is not allowed. Choose a different move.";
                        Debug.LogWarning($"[12Bead] Offline move rejected reason={offlineApply.rejectReason} from={fromNode} to={toNode}");
                        PopupMessageManager.instance?.ShowPopUp(msg, "Move Blocked", 3f);
                        return;
                    }
                }
                TwelveBeadSoundManager.instance?.PlayAnySound(0);

                turnSlectedBead.currentNode.OnOccupied(false);
                // turnSlectedBead.transform.position = beadScript.transform.position;

                // Tween the movement from current position to beadScript's position.
                // Previously this only ran on server/offline, leaving pure clients waiting on
                // the SyncVar hook to update position — which made the bead appear "stuck"
                // when the hook threw or was delayed. Tween locally on every side so the
                // visual is immediate; the hook still corrects position when sync arrives.
                Transform movingTf = turnSlectedBead.transform;
                Vector3 movingStart = movingTf.position;
                Vector3 movingEnd = beadScript.transform.position;
                movingTf.DOKill();
                movingTf.position = movingEnd;
                movingTf.DOMove(movingEnd, 0.15f)
                    .From(movingStart, false)
                    .OnKill(() =>
                    {
                        if (movingTf != null) movingTf.position = movingEnd;
                    });
                turnSlectedBead.currentNode = beadScript.currentNode;
                // Only the move originator should drive the authoritative SyncVar; remote
                // clients running this via RemotedMoveSelected must not echo a Cmd back to
                // the server with their local view (could overwrite with stale state).
                turnSlectedBead.currentNode.OnOccupied(true);
                turnSlectedBead.currentNode.SetBead(turnSlectedBead);
                // Only record history for the FIRST move of a turn, not capture chain continuations.
                // Capture chains are legitimate forced moves and shouldn't trip the A-B-A-B detector.
                if (!wasInCaptureChain)
                {
                    // Per-player tracking so alternating turns don't wipe each other's history.
                    bool isP1Move = turnSlectedBead.playerID == PLAYERS.PLAYER1;
                    BeadScriptTwelve prevLastMoved = isP1Move ? lastMovedBeadP1 : lastMovedBeadP2;
                    // If the player moved a different bead than last turn, clear old bead's history (cycle broken).
                    if (prevLastMoved != null && prevLastMoved != turnSlectedBead)
                    {
                        prevLastMoved.ClearMoveHistory();
                    }
                    if (isP1Move) lastMovedBeadP1 = turnSlectedBead;
                    else lastMovedBeadP2 = turnSlectedBead;
                    turnSlectedBead.RecordMoveNode(beadScript.currentNode);
                }


                // turnSlectedBead.currentNode.currentBead = turnSlectedBead;

                if (beadScript.enmyBead != null)
                {
                    if (!isOnlineMultiplayer && !isRemote)
                    {
                        player1Score = offlineRulesState.player1Score;
                        player2Score = offlineRulesState.player2Score;
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
                        poolBeads.ReturnToPool(tempObject);
                    }
                    tempObject = poolBeads.GetPooledObject();
                    tempObject.transform.position = beadScript.enmyBead.transform.position;
                    tempObject.GetComponent<SpriteRenderer>().sprite = beadScript.enmyBead.GetComponent<SpriteRenderer>().sprite;

                    beadScript.enmyBead.isDead = true;


                    GameObject captureVisual = tempObject;
                    Vector3 captureStartPosition = captureVisual.transform.position;
                    captureVisual.SetActive(true);
                    captureVisual.transform.DOKill();
                    captureVisual.transform.position = targetPostion;
                    captureVisual.transform.DOMove(targetPostion, 0.4f)
                        .From(captureStartPosition, false)
                        .OnComplete(() =>
                        {
                            if (captureVisual != null && captureVisual.activeSelf)
                                poolBeads.ReturnToPool(captureVisual);
                        })
                        .OnKill(() =>
                        {
                            if (captureVisual != null && captureVisual.activeSelf)
                                poolBeads.ReturnToPool(captureVisual);
                        });

                    beadScript.enmyBead.currentNode.OnOccupied(false);

                    //beadScript.enmyBead.currentNode.OnOccupied(false);
                    RefreshBeads();
                    RefreshScore();
                    bool chainContinues = appliedByRules
                        ? offlineApply.chainContinues : CheckNextBead(turnSlectedBead);
                    chainContinues.Show("After Capture");
                    if (chainContinues)
                    {
                        isTakingNextMove = true;
                        if (isWithAI && currentPlayerTurn.GetHashCode() == aiId)
                        {
                            emptyAIBeads.Clear();
                            emptyKillAIBeads.Clear();
                            ActivateMoveAbleEmptyBeads(turnSlectedBead);

                            StartAiMove(true);
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

        public void RefreshBeads()
        {
            Debug.Log("Refreshing beads...");
            for (int i = 0; i < emptyBeads.Count; i++)
            {
                emptyBeads[i].SetVisible(false);
                emptyBeads[i].enmyBead = null;
                emptyBeads[i].GetComponent<BoxCollider2D>().enabled = true;
            }


        }

        bool CheckNextBead(BeadScriptTwelve beadScript)
        {

            for (int i = 0; i < beadScript.currentNode.connectedNodesList.Count; i++)
            {
                NodeScriptTwelve nextNode = beadScript.currentNode.connectedNodesList[i].nextNode;
                NodeScriptTwelve nextToNextNode = beadScript.currentNode.connectedNodesList[i].nextNextNode;

                if (nextToNextNode != null && !nextToNextNode.isOccupied && nextNode.isOccupied
                    && nextNode.currentBead.playerID != currentPlayerTurn)
                {
                    return true;
                }
            }

            return false;


        }


        // RemotedBeadSelected/RemotedMoveSelected were removed with the opponent relay.
        // Selection is local and board movement is rendered only from a committed server delta.

        public void SetTwelveMovePending(BeadScriptTwelve destination)
        {
            ClearTwelveMovePending();
            pendingDestination = destination;
            if (pendingDestination == null) return;

            pendingDestinationScale = pendingDestination.transform.localScale;
            pendingDestination.transform.DOKill();
            pendingDestination.transform.DOPunchScale(Vector3.one * 0.12f, 0.35f, 6, 0.4f);
        }

        public void ClearTwelveMovePending()
        {
            if (pendingDestination == null) return;
            pendingDestination.transform.DOKill();
            pendingDestination.transform.localScale = pendingDestinationScale;
            pendingDestination = null;
        }

        public void ReRenderTwelveServerState()
        {
            ClearTwelveMovePending();
            if (allNodes == null) return;

            if (NetworkServer.active)
            {
                HideTwelveMoveMarkers();
                return; // host shares the server's already-authoritative board objects
            }

            for (int i = 0; i < allNodes.Count; i++)
            {
                allNodes[i].isOccupied = false;
                allNodes[i].currentBead = allNodes[i].emptyNode != null
                    ? allNodes[i].emptyNode.GetComponent<BeadScriptTwelve>() : null;
            }

            RebuildTwelveTeamNodes(team1Beads);
            RebuildTwelveTeamNodes(team2Beads);
            HideTwelveMoveMarkers();
        }

        private void RebuildTwelveTeamNodes(List<BeadScriptTwelve> beads)
        {
            if (beads == null) return;
            for (int i = 0; i < beads.Count; i++)
            {
                BeadScriptTwelve bead = beads[i];
                if (bead == null || bead.isDead || !bead.gameObject.activeSelf || bead.currentNode == null)
                    continue;
                bead.currentNode.isOccupied = true;
                bead.currentNode.currentBead = bead;
            }
        }

        private void HideTwelveMoveMarkers()
        {
            if (emptyBeads == null) return;
            for (int i = 0; i < emptyBeads.Count; i++)
            {
                if (emptyBeads[i] == null) continue;
                emptyBeads[i].enmyBead = null;
                emptyBeads[i].gameObject.SetActive(false);
                BoxCollider2D collider = emptyBeads[i].GetComponent<BoxCollider2D>();
                if (collider != null) collider.enabled = true;
            }
        }

        public void ApplyTwelveMoveCommitted(byte fromNode, byte toNode, byte capturedNode,
            PLAYERS seat, bool chainContinues)
        {
            ClearTwelveMovePending();
            if (fromNode >= allNodes.Count || toNode >= allNodes.Count) return;

            NodeScriptTwelve origin = allNodes[fromNode];
            NodeScriptTwelve destination = allNodes[toNode];
            List<BeadScriptTwelve> movingTeam = seat == PLAYERS.PLAYER1 ? team1Beads : team2Beads;
            BeadScriptTwelve movingBead = null;

            for (int i = 0; i < movingTeam.Count; i++)
            {
                BeadScriptTwelve candidate = movingTeam[i];
                if (candidate != null && !candidate.isDead &&
                    (candidate.currentNode == destination || candidate.currentNode == origin))
                {
                    movingBead = candidate;
                    break;
                }
            }

            if (movingBead == null)
            {
                Debug.LogError($"[12Beads] Committed move has no {seat} bead at {fromNode}/{toNode}");
                ReRenderTwelveServerState();
                return;
            }

            if (!NetworkServer.active)
            {
                origin.isOccupied = false;
                origin.currentBead = origin.emptyNode != null
                    ? origin.emptyNode.GetComponent<BeadScriptTwelve>() : null;
                movingBead.currentNode = destination;
                destination.isOccupied = true;
                destination.currentBead = movingBead;
            }

            Vector3 startPosition = origin.transform.position + new Vector3(0, 0, -2);
            Vector3 endPosition = destination.transform.position + new Vector3(0, 0, -2);
            Transform beadTf = movingBead.transform;
            Debug.Log($"[12Bead] ApplyTwelveMoveCommitted bead={movingBead.name} startPosition={startPosition} endPosition={endPosition}");
            beadTf.DOKill();
            beadTf.position = endPosition;
            beadTf.DOMove(endPosition, 0.15f)
                .From(startPosition, false)
                .OnKill(() =>
                {
                    if (beadTf != null) beadTf.position = endPosition;
                });
            TwelveBeadSoundManager.instance?.PlayAnySound(0);

            if (capturedNode != byte.MaxValue && capturedNode < allNodes.Count)
            {
                NodeScriptTwelve capturedAt = allNodes[capturedNode];
                PLAYERS capturedSeat = seat == PLAYERS.PLAYER1 ? PLAYERS.PLAYER2 : PLAYERS.PLAYER1;
                List<BeadScriptTwelve> capturedTeam = capturedSeat == PLAYERS.PLAYER1 ? team1Beads : team2Beads;
                for (int i = 0; i < capturedTeam.Count; i++)
                {
                    BeadScriptTwelve captured = capturedTeam[i];
                    if (captured != null && captured.currentNode == capturedAt)
                    {
                        if (!NetworkServer.active)
                        {
                            captured.isDead = true;
                            captured.gameObject.SetActive(false);
                        }
                        break;
                    }
                }
                if (!NetworkServer.active)
                {
                    capturedAt.isOccupied = false;
                    capturedAt.currentBead = capturedAt.emptyNode != null
                        ? capturedAt.emptyNode.GetComponent<BeadScriptTwelve>() : null;
                }
            }

            HideTwelveMoveMarkers();
            isTakingNextMove = chainContinues;
            turnSlectedBead = chainContinues ? movingBead : null;
            if (chainContinues && myId == (int)seat)
                ActivateMoveAbleEmptyBeads(movingBead);
        }
        public void ActivateMoveAbleEmptyBeads(BeadScriptTwelve beadScript)
        {
            turnSlectedBead = beadScript;
            if (beadScript == null || beadScript.currentNode == null) return;
            int fromNode = allNodes.IndexOf(beadScript.currentNode);
            if (fromNode < 0 || fromNode >= TwelveBoardTopology.CellCount) return;

            TwelveState viewState = !isOnlineMultiplayer && offlineRulesReady
                ? offlineRulesState : BuildTwelveViewState();
            TwelveMove[] legalMoves = new TwelveMove[32];
            int legalCount = TwelveRulesEngine.GetLegalMovesFrom(viewState, (byte)fromNode, legalMoves);
            for (int i = 0; i < legalCount; i++)
            {
                TwelveMove move = legalMoves[i];
                NodeScriptTwelve destination = allNodes[move.to];
                if (destination == null || destination.emptyNode == null) continue;
                BeadScriptTwelve marker = destination.emptyNode.GetComponent<BeadScriptTwelve>();
                marker.enmyBead = move.IsCapture && move.capturedNode < allNodes.Count
                    ? allNodes[move.capturedNode].currentBead : null;
                destination.emptyNode.GetComponent<SpriteRenderer>().sprite = turnSlectedBead.selectedSprite;
                destination.emptyNode.SetActive(true);

                if (isWithAI && currentPlayerTurn.GetHashCode() == aiId)
                {
                    if (move.IsCapture) emptyKillAIBeads.Add(marker);
                    else emptyAIBeads.Add(marker);
                }
            }

            if (turnSlectedBead.currentNode.emptyNode != null)
            {
                turnSlectedBead.currentNode.emptyNode.GetComponent<SpriteRenderer>().sprite = turnSlectedBead.selectedSprite;
                turnSlectedBead.currentNode.emptyNode.GetComponent<BoxCollider2D>().enabled = false;
                turnSlectedBead.currentNode.emptyNode.SetActive(true);
            }
        }

        private TwelveState BuildTwelveViewState()
        {
            TwelveState state = default;
            TwelveRulesEngine.CreateInitialState(ref state);
            System.Array.Clear(state.cells, 0, state.cells.Length);
            state.turn = currentPlayerTurn;
            AddTwelveTeamToViewState(ref state, team1Beads, 1);
            AddTwelveTeamToViewState(ref state, team2Beads, 13);
            if (isTakingNextMove && turnSlectedBead != null)
                state.forcedPieceId = (byte)(turnSlectedBead.beadId +
                    (turnSlectedBead.playerID == PLAYERS.PLAYER1 ? 1 : 13));
            return state;
        }

        private void AddTwelveTeamToViewState(ref TwelveState state,
            List<BeadScriptTwelve> team, int pieceOffset)
        {
            if (team == null) return;
            for (int i = 0; i < team.Count; i++)
            {
                BeadScriptTwelve bead = team[i];
                if (bead == null || bead.isDead || !bead.gameObject.activeSelf || bead.currentNode == null)
                    continue;
                int node = allNodes.IndexOf(bead.currentNode);
                if (node >= 0 && node < state.cells.Length)
                    state.cells[node] = (byte)(bead.beadId + pieceOffset);
            }
        }

        private bool LegacyWouldAllowOfflineMove(BeadScriptTwelve movingBead, BeadScriptTwelve destination)
        {
            if (movingBead == null || destination == null || movingBead.playerID != currentPlayerTurn ||
                movingBead.currentNode == null || destination.currentNode == null || destination.currentNode.isOccupied)
                return false;
            if (isTakingNextMove && movingBead != turnSlectedBead) return false;

            bool connected = false;
            bool capture = false;
            for (int i = 0; i < movingBead.currentNode.connectedNodesList.Count; i++)
            {
                ConnectedNodesTwelve edge = movingBead.currentNode.connectedNodesList[i];
                if (edge.nextNode == destination.currentNode)
                {
                    connected = !isTakingNextMove;
                    break;
                }
                if (edge.nextNextNode == destination.currentNode && edge.nextNode != null &&
                    edge.nextNode.isOccupied && edge.nextNode.currentBead != null &&
                    edge.nextNode.currentBead.playerID != currentPlayerTurn)
                {
                    connected = true;
                    capture = true;
                    break;
                }
            }
            if (!connected || isTakingNextMove && !capture) return false;
            if (movingBead.isCanMove(destination.currentNode)) return true;
            List<BeadScriptTwelve> team = currentPlayerTurn == PLAYERS.PLAYER1 ? team1Beads : team2Beads;
            return !HasAnyUnrestrictedMove(team);
        }

        private void ValidateTwelveTopologyShadow()
        {
            if (allNodes == null || allNodes.Count != TwelveBoardTopology.CellCount)
            {
                Debug.LogError($"[12Bead] Topology shadow mismatch nodeCount={allNodes?.Count ?? 0}");
                return;
            }

            for (byte from = 0; from < TwelveBoardTopology.CellCount; from++)
            {
                NodeScriptTwelve node = allNodes[from];
                System.ReadOnlySpan<TwelveTopologyEdge> canonical = TwelveBoardTopology.GetEdges(from);
                if (node == null || node.nodeNo != from || node.connectedNodesList.Count != canonical.Length)
                {
                    Debug.LogError($"[12Bead] Topology shadow mismatch node={from}");
                    continue;
                }
                for (int i = 0; i < canonical.Length; i++)
                {
                    ConnectedNodesTwelve sceneEdge = node.connectedNodesList[i];
                    int adjacent = sceneEdge != null && sceneEdge.nextNode != null ? sceneEdge.nextNode.nodeNo : -1;
                    int landing = sceneEdge != null && sceneEdge.nextNextNode != null
                        ? sceneEdge.nextNextNode.nodeNo : TwelveBoardTopology.NoNode;
                    if (adjacent != canonical[i].adjacent || landing != canonical[i].landing)
                        Debug.LogError($"[12Bead] Topology shadow mismatch node={from} edge={i}");
                }
            }
        }


        // Returns true if the current player has at least one board-legal move
        // that is also NOT blocked by the repetitive-move restriction.
        // Used as a safety net so the restriction never creates a stuck state.
        bool HasAnyUnrestrictedMove(List<BeadScriptTwelve> teamBeads)
        {
            // During a capture chain, ONLY the currently-selected bead may continue.
            // Scanning the whole team here would give false positives and cause a stuck state.
            if (isTakingNextMove && turnSlectedBead != null)
            {
                return HasAnyUnrestrictedCaptureForBead(turnSlectedBead);
            }

            for (int j = 0; j < teamBeads.Count; j++)
            {
                BeadScriptTwelve bead = teamBeads[j];
                if (bead == null || !bead.gameObject.activeSelf) continue;
                if (bead.currentNode == null) continue;

                for (int i = 0; i < bead.currentNode.connectedNodesList.Count; i++)
                {
                    NodeScriptTwelve nextNode = bead.currentNode.connectedNodesList[i].nextNode;
                    NodeScriptTwelve nextToNextNode = bead.currentNode.connectedNodesList[i].nextNextNode;

                    // Normal move
                    if (nextNode != null && !nextNode.isOccupied)
                    {
                        if (bead.isCanMove(nextNode)) return true;
                    }
                    // Capture move
                    else if (nextToNextNode != null && !nextToNextNode.isOccupied
                             && nextNode != null && nextNode.isOccupied
                             && nextNode.currentBead != null
                             && nextNode.currentBead.playerID != currentPlayerTurn)
                    {
                        if (bead.isCanMove(nextToNextNode)) return true;
                    }
                }
            }
            return false;
        }

        // Helper used during capture chains: only captures from the given bead are legal.
        bool HasAnyUnrestrictedCaptureForBead(BeadScriptTwelve bead)
        {
            if (bead == null || !bead.gameObject.activeSelf || bead.currentNode == null) return false;
            for (int i = 0; i < bead.currentNode.connectedNodesList.Count; i++)
            {
                NodeScriptTwelve nextNode = bead.currentNode.connectedNodesList[i].nextNode;
                NodeScriptTwelve nextToNextNode = bead.currentNode.connectedNodesList[i].nextNextNode;
                if (nextToNextNode != null && !nextToNextNode.isOccupied
                    && nextNode != null && nextNode.isOccupied
                    && nextNode.currentBead != null
                    && nextNode.currentBead.playerID != currentPlayerTurn)
                {
                    if (bead.isCanMove(nextToNextNode)) return true;
                }
            }
            return false;
        }

        bool IsCheckMate(List<BeadScriptTwelve> teamBeads)
        {

            for (int j = 0; j < teamBeads.Count; j++)
            {

                BeadScriptTwelve beadScript = teamBeads[j];

                if (beadScript.gameObject.activeSelf)
                {
                    for (int i = 0; i < beadScript.currentNode.connectedNodesList.Count; i++)
                    {
                        NodeScriptTwelve nextNode = beadScript.currentNode.connectedNodesList[i].nextNode;
                        NodeScriptTwelve nextToNextNode = beadScript.currentNode.connectedNodesList[i].nextNextNode;

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

        private void StartAiMove(bool isNextMove = false, bool waitForTurn = false)
        {
            StopAiMove();
            aiMoveCoroutine = StartCoroutine(PerformAIMove(isNextMove, waitForTurn));
        }

        private void StopAiMove()
        {
            if (aiMoveCoroutine != null)
            {
                StopCoroutine(aiMoveCoroutine);
                aiMoveCoroutine = null;
            }
        }

        public List<BeadScriptTwelve> moveAbleBeads = new List<BeadScriptTwelve>();
        public List<BeadScriptTwelve> cankillBeads = new List<BeadScriptTwelve>();


        public List<BeadScriptTwelve> emptyAIBeads = new List<BeadScriptTwelve>();
        public List<BeadScriptTwelve> emptyKillAIBeads = new List<BeadScriptTwelve>();

        IEnumerator PerformAIMove(bool isNextMove = false, bool waitForTurn = false)
        {
            if (waitForTurn) yield return new WaitForSeconds(1.5f);
            yield return new WaitForSeconds(isNextMove ? 1f : 1.4f);

            RefreshBeads();
            TwelveMove[] legalMoves = new TwelveMove[128];
            PLAYERS aiPlayer = (PLAYERS)aiId;
            int legalCount = TwelveRulesEngine.GetLegalMoves(offlineRulesState, aiPlayer, legalMoves);
            if (legalCount == 0)
            {
                aiMoveCoroutine = null;
                Debug.LogWarning("[12Bead] AI rules engine returned no legal move");
                yield break;
            }

            int captureCount = 0;
            for (int i = 0; i < legalCount; i++)
                if (legalMoves[i].IsCapture) captureCount++;
            int selected = Random.Range(0, captureCount > 0 ? captureCount : legalCount);
            TwelveMove chosen = default;
            for (int i = 0; i < legalCount; i++)
            {
                if (captureCount > 0 && !legalMoves[i].IsCapture) continue;
                if (selected-- == 0)
                {
                    chosen = legalMoves[i];
                    break;
                }
            }

            BeadScriptTwelve movingBead = FindRulesPiece(chosen.pieceId);
            BeadScriptTwelve destination = chosen.to < allNodes.Count && allNodes[chosen.to].emptyNode != null
                ? allNodes[chosen.to].emptyNode.GetComponent<BeadScriptTwelve>() : null;
            if (movingBead == null || destination == null)
            {
                aiMoveCoroutine = null;
                Debug.LogError($"[12Bead] AI could not resolve rules move piece={chosen.pieceId} from={chosen.from} to={chosen.to}");
                yield break;
            }

            emptyAIBeads.Clear();
            emptyKillAIBeads.Clear();
            ActivateMoveAbleEmptyBeads(movingBead);
            yield return new WaitForSeconds(2f);
            aiMoveCoroutine = null;
            OnMoveSelected(destination);
        }

        private BeadScriptTwelve FindRulesPiece(byte pieceId)
        {
            List<BeadScriptTwelve> team = TwelveRulesEngine.Owner(pieceId) == PLAYERS.PLAYER1
                ? team1Beads : team2Beads;
            int beadId = pieceId <= 12 ? pieceId - 1 : pieceId - 13;
            for (int i = 0; i < team.Count; i++)
                if (team[i] != null && team[i].beadId == beadId) return team[i];
            return null;
        }
        void SetListOfAllMoveableAIBeads(List<BeadScriptTwelve> beadList)
        {
            moveAbleBeads.Clear();
            cankillBeads.Clear();
            for (int j = 0; j < beadList.Count; j++)
            {

                BeadScriptTwelve beadScript = beadList[j];

                if (beadScript.gameObject.activeSelf && !beadScript.isDead)
                {

                    for (int i = 0; i < beadScript.currentNode.connectedNodesList.Count; i++)
                    {
                        NodeScriptTwelve nextNode = beadScript.currentNode.connectedNodesList[i].nextNode;
                        NodeScriptTwelve nextToNextNode = beadScript.currentNode.connectedNodesList[i].nextNextNode;

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

        //        #region RECONNECT
        // private GameStateData savedGameState;
        // private float currentGameTime = 300f;

        // /// <summary>
        // /// Save current game state - called by server periodically
        // /// </summary>
        // public GameStateData SaveGameState()
        // {
        //     // Don't save if game hasn't started
        //     if (currentPlayerTurn == PLAYERS.EMPTY)
        //     {
        //         Debug.LogWarning("📌 [SaveGameState] Game not started (Turn=EMPTY), skipping save");
        //         return null;
        //     }

        //     Debug.Log("📌 [SaveGameState] Saving CURRENT game state...");

        //     GameStateData state = new GameStateData();

        //     // Save current turn
        //     state.currentPlayerTurnHash = currentPlayerTurn.GetHashCode();

        //     // Use online multiplayer scores if in online mode
        //     if (isOnlineMultiplayer && multiPlayerGame != null)
        //     {
        //         state.player1Score = multiPlayerGame.Myplayer1Score;
        //         state.player2Score = multiPlayerGame.Myplayer2Score;
        //         Debug.Log($"[SaveGameState] Using ONLINE scores: P1={state.player1Score}, P2={state.player2Score}");
        //     }
        //     else
        //     {
        //         state.player1Score = player1Score;
        //         state.player2Score = player2Score;
        //         Debug.Log($"[SaveGameState] Using LOCAL scores: P1={state.player1Score}, P2={state.player2Score}");
        //     }

        //     state.remainingTime = currentGameTime;

        //     Debug.Log($"➡️ Saving: Turn={currentPlayerTurn}({state.currentPlayerTurnHash}), P1={state.player1Score}, P2={state.player2Score}, Time={state.remainingTime}");

        //     // Save team 1 beads
        //     if (team1Beads != null)
        //     {
        //         for (int i = 0; i < team1Beads.Count; i++)
        //         {
        //             var bead = team1Beads[i];
        //             if (bead == null) continue;

        //             GameStateData.BeadState beadState = new GameStateData.BeadState();
        //             beadState.beadId = bead.beadId;
        //             beadState.currentNodeIndex = allNodes.IndexOf(bead.currentNode);
        //             beadState.isDead = bead.isDead;
        //             beadState.isActive = bead.gameObject.activeSelf;

        //             state.team1BeadStates.Add(beadState);
        //         }
        //     }

        //     // Save team 2 beads
        //     if (team2Beads != null)
        //     {
        //         for (int i = 0; i < team2Beads.Count; i++)
        //         {
        //             var bead = team2Beads[i];
        //             if (bead == null) continue;

        //             GameStateData.BeadState beadState = new GameStateData.BeadState();
        //             beadState.beadId = bead.beadId;
        //             beadState.currentNodeIndex = allNodes.IndexOf(bead.currentNode);
        //             beadState.isDead = bead.isDead;
        //             beadState.isActive = bead.gameObject.activeSelf;

        //             state.team2BeadStates.Add(beadState);
        //         }
        //     }

        //     Debug.Log($"🎉 [SaveGameState] Saved successfully - T1:{state.team1BeadStates.Count} beads, T2:{state.team2BeadStates.Count} beads");

        //     return state;
        // }

        // /// <summary>
        // /// Restore game state - called when player reconnects
        // /// </summary>
        // public void RestoreGameState(GameStateData state)
        // {
        //     if (state == null)
        //     {
        //         Debug.LogError("💀 [RestoreGameState] State was NULL.");
        //         return;
        //     }

        //     Debug.Log("📌 [RestoreGameState] Starting restore process...");

        //     // Stop all timers first
        //     StopAllCoroutines();

        //     // CRITICAL: Check if beads exist
        //     bool beadsExist = (team1Beads != null && team1Beads.Count > 0 && 
        //                        team2Beads != null && team2Beads.Count > 0);

        //     if (!beadsExist)
        //     {
        //         Debug.LogWarning("⚠️ [RestoreGameState] Beads don't exist! Creating them first...");
        //         beadsCreated = false; // Reset flag to force creation
        //        // CreateTeamsBeads();

        //         // Wait a frame for beads to be fully created
        //         StartCoroutine(RestoreAfterBeadsCreated(state));
        //         return;
        //     }

        //     Debug.Log($"✅ [RestoreGameState] Beads exist - T1:{team1Beads.Count}, T2:{team2Beads.Count}");

        //     // If beads exist, restore immediately
        //     PerformRestore(state);
        // }

        // /// <summary>
        // /// Wait for beads to be created, then restore
        // /// </summary>
        // private IEnumerator RestoreAfterBeadsCreated(GameStateData state)
        // {
        //     yield return null; // Wait one frame

        //     Debug.Log("🔄 [RestoreGameState] Beads created, performing restore...");
        //     PerformRestore(state);
        // }

        // /// <summary>
        // /// Actual restore logic
        // /// </summary>
        // private void PerformRestore(GameStateData state)
        // {
        //     Debug.Log($"📦 [PerformRestore] Starting - T1 Beads: {team1Beads?.Count}, T2 Beads: {team2Beads?.Count}");
        //     Debug.Log($"📦 [PerformRestore] State - T1 States: {state.team1BeadStates.Count}, T2 States: {state.team2BeadStates.Count}");

        //     // Restore game state variables
        //     currentPlayerTurn = (PLAYERS)state.currentPlayerTurnHash;

        //     // Restore scores
        //     if (isOnlineMultiplayer && multiPlayerGame != null)
        //     {
        //         multiPlayerGame.Myplayer1Score = state.player1Score;
        //         multiPlayerGame.Myplayer2Score = state.player2Score;
        //         Debug.Log($"[PerformRestore] Restored ONLINE scores: P1={state.player1Score}, P2={state.player2Score}");
        //     }
        //     else
        //     {
        //         player1Score = state.player1Score;
        //         player2Score = state.player2Score;
        //         Debug.Log($"[PerformRestore] Restored LOCAL scores: P1={state.player1Score}, P2={state.player2Score}");
        //     }

        //     currentGameTime = state.remainingTime;

        //     Debug.Log($"➡️ Restored: Turn={currentPlayerTurn}, P1={state.player1Score}, P2={state.player2Score}, Time={currentGameTime}");

        //     // Clear all nodes first
        //     for (int i = 0; i < allNodes.Count; i++)
        //     {
        //         if (allNodes[i] != null)
        //         {
        //             allNodes[i].OnOccupied(false);
        //             allNodes[i].currentBead = null;
        //         }
        //     }

        //     // Restore team 1 beads using INDEX
        //     Debug.Log($"🟦 [Restore T1] Processing {state.team1BeadStates.Count} beads...");

        //     for (int i = 0; i < state.team1BeadStates.Count; i++)
        //     {
        //         if (i >= team1Beads.Count)
        //         {
        //             Debug.LogError($"💀 [Restore T1] Index {i} out of bounds! team1Beads.Count = {team1Beads.Count}");
        //             break;
        //         }

        //         var beadState = state.team1BeadStates[i];
        //         var bead = team1Beads[i];

        //         if (bead != null && beadState.currentNodeIndex >= 0 && beadState.currentNodeIndex < allNodes.Count)
        //         {
        //             // Remove from old node
        //             if (bead.currentNode != null)
        //             {
        //                 bead.currentNode.OnOccupied(false);
        //                 bead.currentNode.currentBead = null;
        //             }

        //             // Set to new node
        //             bead.currentNode = allNodes[beadState.currentNodeIndex];
        //             bead.transform.position = bead.currentNode.transform.position + new Vector3(0, 0, -2);

        //             bead.isDead = beadState.isDead;
        //             bead.gameObject.SetActive(beadState.isActive);

        //             // Update node
        //             if (beadState.isActive && !beadState.isDead)
        //             {
        //                 bead.currentNode.OnOccupied(true);
        //                 bead.currentNode.currentBead = bead;
        //             }

        //             Debug.Log($"✅ T1 Bead {i}: Node={beadState.currentNodeIndex}, Active={beadState.isActive}, Dead={beadState.isDead}");
        //         }
        //         else
        //         {
        //             Debug.LogError($"💀 [Restore T1] Bead {i} is null or invalid node: {beadState.currentNodeIndex}");
        //         }
        //     }

        //     // Restore team 2 beads using INDEX
        //     Debug.Log($"🟥 [Restore T2] Processing {state.team2BeadStates.Count} beads...");

        //     for (int i = 0; i < state.team2BeadStates.Count; i++)
        //     {
        //         if (i >= team2Beads.Count)
        //         {
        //             Debug.LogError($"💀 [Restore T2] Index {i} out of bounds! team2Beads.Count = {team2Beads.Count}");
        //             break;
        //         }

        //         var beadState = state.team2BeadStates[i];
        //         var bead = team2Beads[i];

        //         if (bead != null && beadState.currentNodeIndex >= 0 && beadState.currentNodeIndex < allNodes.Count)
        //         {
        //             // Remove from old node
        //             if (bead.currentNode != null)
        //             {
        //                 bead.currentNode.OnOccupied(false);
        //                 bead.currentNode.currentBead = null;
        //             }

        //             // Set to new node
        //             bead.currentNode = allNodes[beadState.currentNodeIndex];
        //             bead.transform.position = bead.currentNode.transform.position + new Vector3(0, 0, -2);

        //             bead.isDead = beadState.isDead;
        //             bead.gameObject.SetActive(beadState.isActive);

        //             // Update node
        //             if (beadState.isActive && !beadState.isDead)
        //             {
        //                 bead.currentNode.OnOccupied(true);
        //                 bead.currentNode.currentBead = bead;
        //             }

        //             Debug.Log($"✅ T2 Bead {i}: Node={beadState.currentNodeIndex}, Active={beadState.isActive}, Dead={beadState.isDead}");
        //         }
        //         else
        //         {
        //             Debug.LogError($"💀 [Restore T2] Bead {i} is null or invalid node: {beadState.currentNodeIndex}");
        //         }
        //     }

        //     Debug.Log("📊 Refreshing UI...");
        //     RefreshScore();
        //     RefreshBeads();

        //     Debug.Log("🚀 Calling StartGameInCaseOfDisconnect");
        //     StartGameInCaseOfDisconnect();

        //     // Only start countdown timer if NOT in online multiplayer
        //     // (Online multiplayer uses server-synced timer)
        //     if (!isOnlineMultiplayer)
        //     {
        //         Debug.Log($"⏳ Restarting countdown from {state.remainingTime}s (Offline Mode)");
        //         StartCoroutine(StartCountdown(state.remainingTime));
        //     }
        //     else
        //     {
        //         Debug.Log("⏳ Skipping local countdown (Online Mode - using server timer)");
        //     }

        //     Debug.Log("🎉 [RestoreGameState] Restore complete!");
        // }

        // /// <summary>
        // /// Countdown coroutine for offline mode
        // /// </summary>
        // private IEnumerator StartCountdown(float startTime = 300f)
        // {
        //     currentGameTime = startTime;

        //     while (currentGameTime > 0)
        //     {
        //         int minutes = Mathf.FloorToInt(currentGameTime / 60);
        //         int seconds = Mathf.FloorToInt(currentGameTime % 60);

        //         if (countDownGameTimer != null)
        //         {
        //             countDownGameTimer.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        //             if (currentGameTime > 180)
        //                 countDownGameTimer.color = Color.green;
        //             else if (currentGameTime <= 180 && currentGameTime > 60)
        //                 countDownGameTimer.color = Color.white;
        //             else
        //                 countDownGameTimer.color = Color.red;
        //         }

        //         yield return new WaitForSeconds(1);
        //         currentGameTime--;
        //     }

        //     if (countDownGameTimer != null)
        //     {
        //         countDownGameTimer.text = "00:00";
        //         countDownGameTimer.color = Color.red;
        //     }

        //     OnTimerEnd();
        // }

        // #endregion
    }

    // [System.Serializable]
    // public class GameStateData
    // {
    //     public int currentPlayerTurnHash;
    //     public int player1Score;
    //     public int player2Score;
    //     public float remainingTime;

    //     // Bead positions and states
    //     public List<BeadState> team1BeadStates = new List<BeadState>();
    //     public List<BeadState> team2BeadStates = new List<BeadState>();

    //     [System.Serializable]
    //     public class BeadState
    //     {
    //         public int beadId;
    //         public int currentNodeIndex;
    //         public bool isDead;
    //         public bool isActive;
    //     }
    // }


}
