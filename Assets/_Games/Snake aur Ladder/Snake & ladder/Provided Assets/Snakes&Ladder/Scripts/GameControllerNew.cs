using DG.Tweening;
using Mirror;
using Mirror.Examples.Basic;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityExtensions;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace Snake_Ladder
{
    public class GameControllerNew : MonoBehaviour
    {
        public static GameControllerNew instance;

        [Header("Game Mode")]
        public GameMode currentGameMode;
        // public Sprite MasterGoti, ClientGoti;

        [Header("Player Prefabs - FOR MULTIPLAYER ONLY")]
        public GameObject player1Prefab; // Multiplayer ke liye Player 1 prefab
        public GameObject player2Prefab; // Multiplayer ke liye Player 2 prefab
        public Transform player1SpawnPoint;
        public Transform player2SpawnPoint;
        public TextMeshProUGUI challengeAmount;
        public Image coinSprite;
        public enum GameMode
        {
            Against_Ai,
            Against_LocalPlayer,
            Against_OnlineFriend,
            Against_RandomPlayer
        }

        [Header("Player 1")]
        [SerializeField] public DiceAnimator player1Dice;
        [SerializeField] GameObject player1Arrow;
        [SerializeField] public Transform Player1Soldier;
        [SerializeField] public Button Player1DiceButton;
        [SerializeField] public Button Player1Button;

        [Header("Player 2")]
        [SerializeField] public DiceAnimator player2Dice;
        [SerializeField] GameObject player2Arrow;
        [SerializeField] public Transform Player2Soldier;
        [SerializeField] public Button Player2DiceButton;
        [SerializeField] public Button Player2Button;

        [Header("Nodes")]
        [SerializeField] Node StartingNode;
        [SerializeField]public Node CurrentNodePlayer1;
        [SerializeField]public Node CurrentNodePlayer2;
        [SerializeField]public Node EndNode;

        [Header("Turn")]
        [SerializeField] public static bool isPlayer1Turn;
        [SerializeField] public static bool isPlayer2Turn;
        [SerializeField] float DurationToNextNode;
        [SerializeField] Vector3 ShrinkScale;
        [SerializeField] float yShrinkOffset;

        [Header("UI")]
        [SerializeField] GameObject gameoverPanel;
        [SerializeField] public GameObject wonPanel;
        [SerializeField] GameObject losePanel;
        [SerializeField] GameObject disconnectedPanel;
        [SerializeField] GameObject WaitingOpponentPanel;
        [SerializeField] TextMeshProUGUI disconnectedText;

        [Header("Environment")]
        [SerializeField] public List<SnakesAndLadders> AllEnvironments;
        [SerializeField] public SnakesAndLadders Environment;
        [SerializeField] public SpriteRenderer EnvironmentImage;
        public int currentEnvironmentIndex = -1;

        [Header("Timer Settings")]
        [SerializeField] public float TimerDuration = 10f;
        [SerializeField] public Image Player1TimerImage;
        [SerializeField] public Image Player2TimerImage;
        [Header("Player UI")]
        [SerializeField] Text player1NameText;
        [SerializeField] Image player1AvatarImage;
        [SerializeField] Text player2NameText;
        [SerializeField] Image player2AvatarImage;

        private int count;
        public bool isOnline;
        public float timer;
        public bool timerRunning;
        private bool finishGame;
        private bool isLeavingOnlineGame;
        public SpritesHOlder scriptable;

        // Player tracking
        public static int myPlayerNumber = -1;
        private bool isServer = false;
        private bool gameStarted = false;
        public GameObject SnakeNetworkManagerPrefab;

        // Multiplayer player instances
        private GameObject player1Instance;
        private GameObject player2Instance;
        private bool playersInstantiated = false;
        public bool PlayerWin;
        public TextMeshProUGUI countDownGameTimer;
        private void Awake()
        {
            instance = this;
            Debug.Log("╔═══════════════════════════════════╗");
            Debug.Log("║    GameController AWAKE           ║");
            Debug.Log("╚═══════════════════════════════════╝");
        }
     
        public void SpawnNetworkObject()
        {
            var network = Instantiate(SnakeNetworkManagerPrefab);
            NetworkServer.Spawn(network);
        }
        [SerializeField] private GameObject speakerObj;
        [SerializeField] private GameObject micObj;
        private IEnumerator Start()
        {
            if (NetworkClient.active)
            {
                if (staticVariables.gameFeatures.isAgora)
                {
                    speakerObj.SetActive(true);
                    micObj.SetActive(true);
                }
                else
                {
                    speakerObj.SetActive(false);
                    micObj.SetActive(false);
                }
            }
            PlayerWin = false;
            ResultManager.GameSpawnedFinished = false;
            ResultManager.isGameFinished = false;
            isServer = NetworkServer.active && !NetworkClient.active;

            if (isServer)
            {
                SpawnNetworkObject();

            }
            // yield return new WaitUntil(() => SnakeMirrorNetworkManager.Instance != null);
            if (NetworkServer.active || NetworkClient.active)
            {

               // MirrorNetwork.OnWinCall += AnnounceVictory;
                isOnline = true;
                SnakeGameManager.instance.currentGameMode = SnakeGameManager.GameMode.Against_OnlineFriend;

            }
            else
            {
                SnakeGameManager.instance.currentGameMode = SnakeGameManager.GameMode.Against_Ai;
            }
            if (isOnline)
            {
                yield return new WaitUntil(() => SnakeMirrorNetworkManager.Instance != null);
            }

            Debug.Log("╔═══════════════════════════════════╗");
            Debug.Log("║    GAMECONTROLLER START           ║");
            Debug.Log($"║  isOnline: {isOnline}                    ║");
            Debug.Log($"║  currentGameMode: {SnakeGameManager.instance.currentGameMode}                    ║");
            Debug.Log($"║  isServer: {isServer}                    ║");
            Debug.Log($"║  isClient: {NetworkClient.active}                    ║");
            Debug.Log("╚═══════════════════════════════════╝");

            Player1TimerImage.gameObject.SetActive(false);
            Player2TimerImage.gameObject.SetActive(false);
            updateCoinStatus();
            Init();

            // Subscribe to events
            SubscribeToNetworkEvents();

            // Start game if not online (AI ya Local)
            if (!isOnline)
            {
                StartGame();
            }
            else if (!isServer)
            {
                ShowWaitingForOpponent(true);
            }
        }
        public void DirectResult(bool result)
        {
            if (SnakeMirrorNetworkManager.Instance == null)
                return;

            string winnerId = result
                ? staticVariables.UserProfiledata.user._id.ToString()
                : staticVariables.OpponetProfile.userId;
            SnakeMirrorNetworkManager.Instance.ShowServerWinner(winnerId);
        }
        public void SetupPlayerProfiles()
        {
            Debug.Log($"🎭 Setting up player profiles - My Player Number: {myPlayerNumber}");

            if (myPlayerNumber == 0)
            {
                // Main Player 1 hoon
                // Player 1 UI - Meri profile
                if (player1NameText != null)
                    player1NameText.text = staticVariables.UserProfiledata.user.first_name+""+staticVariables.UserProfiledata.user.last_name;

                if (player1AvatarImage != null && staticVariables.ProfilePicture != null)
                {
                    player1AvatarImage.sprite = Sprite.Create(
                        staticVariables.ProfilePicture,
                        new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height),
                        Vector2.zero
                    );
                }

                // Player 2 UI - Opponent ki profile
                if (player2NameText != null)
                    player2NameText.text = staticVariables.OpponetProfile.userName;

                if (player2AvatarImage != null && staticVariables.opponentImage != null)
                {
                    player2AvatarImage.sprite = Sprite.Create(
                        staticVariables.opponentImage,
                        new Rect(0, 0, staticVariables.opponentImage.width, staticVariables.opponentImage.height),
                        Vector2.zero
                    );
                }
            }
            else if (myPlayerNumber == 1)
            {
                // Main Player 2 hoon
                // Player 1 UI - Opponent ki profile
                if (player1NameText != null)
                    player1NameText.text = staticVariables.OpponetProfile.userName;

                if (player1AvatarImage != null && staticVariables.opponentImage != null)
                {
                    player1AvatarImage.sprite = Sprite.Create(
                        staticVariables.opponentImage,
                        new Rect(0, 0, staticVariables.opponentImage.width, staticVariables.opponentImage.height),
                        Vector2.zero
                    );
                }

                // Player 2 UI - Meri profile
                if (player2NameText != null)
                    player2NameText.text = staticVariables.UserProfiledata.user.first_name;

                if (player2AvatarImage != null && staticVariables.ProfilePicture != null)
                {
                    player2AvatarImage.sprite = Sprite.Create(
                        staticVariables.ProfilePicture,
                        new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height),
                        Vector2.zero
                    );
                }
            }

            Debug.Log("✅ Player profiles setup complete!");
        }
        void SubscribeToNetworkEvents()
        {
            if (SnakeMirrorNetworkManager.Instance != null)
            {
                SnakeMirrorNetworkManager.Instance.UpdateEnvironment += SetEnvironment;
                //   SnakeMirrorNetworkManager.Instance.PlayerLeft += OnPlayerLeft;
                SnakeMirrorNetworkManager.Instance.FirstTurnPlayer += OnFirstTurnPlayer;
                //  SnakeMirrorNetworkManager.Instance.Disconnected += OnDisconnected;
                SnakeMirrorNetworkManager.Instance.StartGame += OnGameStart;

                Debug.Log("✅ All events subscribed to SnakeMirrorNetworkManager!");
            }
            else
            {
                Debug.LogError("❌ SnakeMirrorNetworkManager.Instance is NULL!");
            }
        }

        void OnFirstTurnPlayer(int firstPlayerNumber)
        {
            count=0;
             if (myPlayerNumber == -1)
            {
                Debug.LogWarning($"⏳ OnFirstTurnPlayer arrived before myPlayerNumber synced (firstPlayer={firstPlayerNumber}). Deferring.");
                StartCoroutine(ApplyFirstTurnWhenReady(firstPlayerNumber));
                return;
            }
            // Compare with my player number
            bool isMyTurn = (firstPlayerNumber == myPlayerNumber);

            if (isMyTurn)
            {
                if (myPlayerNumber == 0)
                {
                    Debug.Log("🎲 MY TURN FIRST!");
                    Player1DiceButton.interactable = true;
                    Player1Button.interactable= true;
                    Player2Button.interactable = false;
                    Player2DiceButton.interactable = false;
                    player1Arrow.gameObject.SetActive(true);
                    player2Arrow.gameObject.SetActive(false);
                    // SnakeMirrorNetworkManager.Instance.CmdTimer();
                    // StartTimer();
                    Debug.LogWarning("startTimer1");

                }
                else
                {
                    Debug.Log("🎲 MY TURN FIRST!");
                    Player1DiceButton.interactable = false;
                    Player2DiceButton.interactable = true;
                    Player1Button.interactable = false;
                    Player2Button.interactable = true;
                    player2Arrow.gameObject.SetActive(true);
                    player1Arrow.gameObject.SetActive(false);
                    Debug.LogWarning("startTimer2");
                    //  SnakeMirrorNetworkManager.Instance.CmdTimer();
                    //   StartTimer();
                }
            }
            else
            {
                Player1DiceButton.interactable = false;
                Player2DiceButton.interactable = false;
                Player1Button.interactable = false;
                Player2Button.interactable = false;
                if (firstPlayerNumber == 0)
                {

                    player1Arrow.gameObject.SetActive(true);
                    player2Arrow.gameObject.SetActive(false);
                    Debug.LogWarning("startTimer3");
                    //  SnakeMirrorNetworkManager.Instance.CmdTimer();
                    // StartTimer();
                }
                else
                {
                    player2Arrow.gameObject.SetActive(true);
                    player1Arrow.gameObject.SetActive(false);
                    Debug.LogWarning("startTimer4");
                    //   SnakeMirrorNetworkManager.Instance.CmdTimer();
                    // StartTimer();
                }
                Debug.Log("⏳ OPPONENT'S TURN FIRST!");
                //  Player2TimerImage.gameObject.SetActive(true);
                //  player1Arrow.gameObject.SetActive(false);
            }
        }
        // ========== INSTANTIATE PLAYERS (MULTIPLAYER ONLY) ==========
        IEnumerator ApplyFirstTurnWhenReady(int firstPlayerNumber)
        {
            yield return new WaitUntil(() => myPlayerNumber != -1);
            Debug.Log($"✅ myPlayerNumber synced ({myPlayerNumber}). Applying deferred first turn = {firstPlayerNumber}.");
            OnFirstTurnPlayer(firstPlayerNumber);
        }
        void OnAssignPlayerNumber(int playerNumber)
        {
            myPlayerNumber = playerNumber;
            if (NetworkServer.active || NetworkClient.active)
            {
                SetupPlayerProfiles();
            }
            // ✅ MULTIPLAYER: Instantiate players

            ShowWaitingForOpponent(true);
        }

        // ========== GAME START ==========
        void OnGameStart()
        {
            gameStarted = true;

            Debug.Log("╔═══════════════════════════════════╗");
            Debug.Log("║   GAMECONTROLLER: GAME STARTING   ║");
            Debug.Log("╠═══════════════════════════════════╣");
            Debug.Log($"║   My Player Number: {myPlayerNumber}             ║");
            Debug.Log($"║   Game Mode: {currentGameMode}              ║");
            Debug.Log("╚═══════════════════════════════════╝");

            ShowWaitingForOpponent(false);
            StartGame();
        }

        void ShowWaitingForOpponent(bool show)
        {
            if (WaitingOpponentPanel != null)
            {
                WaitingOpponentPanel.SetActive(show);
            }
        }

        // ========== INIT ==========
        void Init()
        {
            Debug.Log("Initializing game state...");

            if (GameManager.instance)
            {
                if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_Ai))
                {
                    currentGameMode = GameMode.Against_Ai;
                    isOnline = false;
                }
                if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_LocalPlayer))
                {
                    currentGameMode = GameMode.Against_LocalPlayer;
                    isOnline = false;
                }
                if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_OnlineFriend))
                {
                    currentGameMode = GameMode.Against_OnlineFriend;
                    isOnline = true;
                }
                if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_RandomPlayer))
                {
                    currentGameMode = GameMode.Against_RandomPlayer;
                    isOnline = true;
                }
            }

            player1Arrow.gameObject.SetActive(false);
            player2Arrow.gameObject.SetActive(false);
            Player2TimerImage.gameObject.SetActive(false);

            // ✅ AI MODE: Use existing scene objects
            // ✅ MULTIPLAYER: Players will be instantiated later

            if (currentGameMode == GameMode.Against_Ai || currentGameMode == GameMode.Against_LocalPlayer)
            {
                // For AI and Local, use existing positions
                CurrentNodePlayer1 = StartingNode;
                CurrentNodePlayer2 = StartingNode;

                if (Player1Soldier != null)
                {
                    Player1Soldier.position = new Vector3(StartingNode.transform.position.x,
                        StartingNode.transform.position.y, Player1Soldier.position.z);
                }

                if (Player2Soldier != null)
                {
                    Player2Soldier.position = new Vector3(StartingNode.transform.position.x,
                        StartingNode.transform.position.y, Player2Soldier.position.z);
                }

                HandleNodeConflict();
            }

            // Environment setup
            if (!isOnline)
            {
                Environment = AllEnvironments[UnityEngine.Random.Range(0, AllEnvironments.Count)];
                EnvironmentImage.sprite = Environment.environmentSprite;
            }

            Debug.Log("✅ Initialization complete");
        }

        void StartGame()
        {
            Debug.Log("🎯 Starting game..." + currentGameMode);
            // ✅ MULTIPLAYER: Setup positions after instantiation
            if (currentGameMode != GameMode.Against_Ai)
            {
                // Setup positions for instantiated players

                CurrentNodePlayer1 = StartingNode;
                Player1Soldier.position = new Vector3(
                    StartingNode.transform.position.x,
                    StartingNode.transform.position.y,
                    Player1Soldier.position.z
                );



                CurrentNodePlayer2 = StartingNode;
                Player2Soldier.position = new Vector3(
                    StartingNode.transform.position.x,
                    StartingNode.transform.position.y,
                    Player2Soldier.position.z
                );


                HandleNodeConflict();
            }

            if (!isOnline)
            {
                // AI ya Local Multiplayer
                isPlayer1Turn = true;
                Player1TimerImage.gameObject.SetActive(true);
                player1Arrow.gameObject.SetActive(true);
                StartTimerAI();
                Debug.LogWarning("startTimer5");
            }
            //else if (myPlayerNumber == 0 )
            //{
            //    // Online - Player 1 ka turn hai
            //    Player1TimerImage.gameObject.SetActive(true);
            //    player1Arrow.gameObject.SetActive(true);
            //    // StartTimer();
            //    Debug.LogWarning("startTimer5");
            //}

            finishGame = false;
        }

        // ========== TAKE TURN ==========
        public void TakeTurn(int playerNum)
        {
            if (count >= 1) return;
            // Debug.Log(EventSystem.current.currentSelectedGameObject.name);
            // Player 1's turn
            if (playerNum == 0)
            {
                Debug.Log("🎲 Taking Player 1's turn");
                Snake_Ladder.SoundManager.instance.PlaySound(SoundManager.AllSounds.Dice);
                player1Arrow.gameObject.SetActive(false);
                Player1DiceButton.interactable = false;
                Player1Button.interactable = false;
                int turnNumber = UnityEngine.Random.Range(1, 7);
                count++;

                // ✅ Reset timer (don't stop completely)
                if (isOnline && isServer && SnakeMirrorNetworkManager.Instance != null)
                {
                    SnakeMirrorNetworkManager.Instance.ServerResetTimer(); // Changed
                }

                if (isOnline && SnakeMirrorNetworkManager.Instance != null)
                {
                    Debug.Log($"📤 Sending to server: Player {playerNum + 1}, dice {turnNumber}");
                    SnakeMirrorNetworkManager.Instance.CmdMovePlayern(turnNumber, staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    Debug.Log($"🎲 Player 1 rolled: {turnNumber}");
                    player1Dice.AnimateDice(turnNumber);
                    StartCoroutine(AIInvokeMovePlayer(0, turnNumber));
                }

                StopTimer();
            }

            // Player 2's turn
            if (playerNum == 1)
            {
                Debug.Log("🎲 Taking Player 2's turn");
                SoundManager.instance.PlaySound(SoundManager.AllSounds.Dice);
                player2Arrow.gameObject.SetActive(false);
                Player2TimerImage.gameObject.SetActive(false);

                int turnNumber = UnityEngine.Random.Range(1, 7);
                Debug.Log($"🎲 Player 2 rolled: {turnNumber}");

                count++;

                // ✅ Reset timer (don't stop completely)
                if (isOnline && isServer && SnakeMirrorNetworkManager.Instance != null)
                {
                    SnakeMirrorNetworkManager.Instance.ServerResetTimer(); // Changed
                }

                if (isOnline && SnakeMirrorNetworkManager.Instance != null)
                {
                    Debug.Log($"📤 Sending to server: Player {playerNum + 1}, dice {turnNumber}");
                    SnakeMirrorNetworkManager.Instance.CmdMovePlayern(turnNumber, staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    Debug.Log($"🎲 Player 2 rolled: {turnNumber}");
                    player2Dice.AnimateDice(turnNumber);
                    StartCoroutine(AIInvokeMovePlayer(1, turnNumber));
                }
            }
            GetComponent<AudioSource>().Play();
        }

        public void TakeTurn(int playerNum, bool skipturn)
        {
            if (count >= 1) return;

            if (isOnline && !isServer)
            {
                Debug.LogWarning($"❌ Not my turn! I am Player {myPlayerNumber + 1}");
                return;
            }

            if (playerNum == 0)
            {
                Snake_Ladder.SoundManager.instance.PlaySound(SoundManager.AllSounds.Dice);
                player1Arrow.gameObject.SetActive(false);
                int turnNumber = UnityEngine.Random.Range(1, 7);

                if (skipturn)
                {
                    SwitchTurn();
                    turnNumber = 0;
                }
                else
                {
                    player1Dice.AnimateDice(turnNumber);
                    StartCoroutine(InvokeMovePlayer(staticVariables.UserProfiledata.user._id.ToString(), turnNumber));
                }

                if (isOnline && SnakeMirrorNetworkManager.Instance != null)
                {
                    SnakeMirrorNetworkManager.Instance.CmdMovePlayern(turnNumber, staticVariables.UserProfiledata.user._id.ToString());
                }

                StopTimer();
            }

            if (playerNum == 1)
            {
                SoundManager.instance.PlaySound(SoundManager.AllSounds.Dice);
                player2Arrow.gameObject.SetActive(false);
                Player2TimerImage.gameObject.SetActive(false);

                int turnNumber = UnityEngine.Random.Range(1, 7);
                player2Dice.AnimateDice(turnNumber);
                // StartCoroutine(InvokeMovePlayer(1, turnNumber));
            }
        }

        void AITakeTurn()
        {
            Debug.Log("🤖 AI taking turn");
            TakeTurn(1);
        }
        private IEnumerator AIInvokeMovePlayer(int playerNum, int turnNumber)
        {
            yield return new WaitForSeconds(3f);
            MyAIMovePlayer(playerNum, turnNumber);
        }
        private IEnumerator InvokeMovePlayer(string playerNum, int turnNumber)
        {
            yield return new WaitForSeconds(3f);

            StartCoroutine(MovePlayer(playerNum, turnNumber));



        }
        public void MyAIMovePlayer(int playerNum, int turnNumber)
        {
            Debug.Log($"🏃 Moving Player {playerNum + 1}: {turnNumber} steps");

            Node currentNode = playerNum == 0 ? CurrentNodePlayer1 : CurrentNodePlayer2;
            List<Vector3> path = new List<Vector3>();
            Transform playerSoldier = playerNum == 0 ? Player1Soldier : Player2Soldier;
            float initialZ = playerSoldier.position.z;
            bool isSkipMove = false;
            bool isGameEnd = false;

            for (int i = 0; i < turnNumber; i++)
            {
                if (currentNode == EndNode)
                {
                    isSkipMove = true;
                    break;
                }

                currentNode = currentNode.GetNextNode();
                if (currentNode == null)
                {
                    Debug.LogError("❌ Next Node missing!");
                    break;
                }

                Vector3 newPosition = currentNode.transform.position;
                newPosition.z = initialZ;
                path.Add(newPosition);
            }

            if (currentNode == EndNode && path.Count == turnNumber)
            {
                isGameEnd = true;
            }

            if (isSkipMove)
            {
                AISwitchTurn();
                return;
            }

            if (path.Count > 0)
            {
                if (playerNum == 0)
                {
                    Player1Soldier.DOPath(path.ToArray(), DurationToNextNode * path.Count, PathType.Linear)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            if (isGameEnd)
                            {
                                Debug.Log("Player 1 has reached the end node.");
                                if (isOnline)
                                {
                                    Debug.Log("Sending Player 1 finish command to server.");
                                    SnakeMirrorNetworkManager.Instance.CmdPlayerFinished(staticVariables.UserProfiledata.user._id);
                                }
                                else
                                {
                                    Debug.Log("Calling GameEnd for Player 1.");
                                    AIGameEnd(playerNum);
                                }

                                // GameEnd(playerNum);
                            }
                            else
                            {
                                CurrentNodePlayer1 = currentNode;
                                AICheckForTransitionNode(playerNum);
                            }
                        });
                }
                else
                {
                    Player2Soldier.DOPath(path.ToArray(), DurationToNextNode * path.Count, PathType.Linear)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            if (isGameEnd)
                            {
                                Debug.Log("Player 2 has reached the end node.");
                                if (isOnline)
                                {
                                    Debug.Log("Sending Player 2 finish command to server.");
                                    SnakeMirrorNetworkManager.Instance.CmdPlayerFinished(staticVariables.UserProfiledata.user._id);
                                }
                                else
                                {
                                    Debug.Log("Calling GameEnd for Player 2.");
                                    AIGameEnd(playerNum);
                                }
                                // GameEnd(playerNum);
                            }
                            else
                            {
                                CurrentNodePlayer2 = currentNode;
                                AICheckForTransitionNode(playerNum);
                            }
                        });
                }
            }
        }
        void AICheckForTransitionNode(int playerNum)
        {
            Node currentNode = playerNum == 0 ? CurrentNodePlayer1 : CurrentNodePlayer2;
            Transform playerSoldier = playerNum == 0 ? Player1Soldier : Player2Soldier;

            foreach (var entry in Environment.ListOfSnakesAndLadder)
            {
                if (entry.TriggerNode == currentNode)
                {
                    Debug.Log($"🐍/🪜 Player {playerNum + 1} hit {entry.type}!");

                    if (entry.PathPoints.Count > 0)
                    {
                        List<Vector3> transitionPath = new List<Vector3>();
                        foreach (var point in entry.PathPoints)
                        {
                            Vector3 newPosition = point.position;
                            newPosition.z = playerSoldier.position.z;
                            transitionPath.Add(newPosition);
                        }

                        playerSoldier.DOPath(transitionPath.ToArray(), DurationToNextNode * transitionPath.Count, PathType.Linear)
                            .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                if (playerNum == 0)
                                    CurrentNodePlayer1 = entry.TargetNode;
                                else
                                    CurrentNodePlayer2 = entry.TargetNode;

                                HandleNodeConflict();
                                AISwitchTurn();
                            });
                    }
                    else
                    {
                        Vector3 targetPosition = entry.TargetNode.transform.position;
                        targetPosition.z = playerSoldier.position.z;

                        playerSoldier.DOMove(targetPosition, DurationToNextNode * 3)
                            .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                if (playerNum == 0)
                                    CurrentNodePlayer1 = entry.TargetNode;
                                else
                                    CurrentNodePlayer2 = entry.TargetNode;

                                HandleNodeConflict();
                                AISwitchTurn();
                            });
                    }
                    return;
                }
            }

            HandleNodeConflict();
            AISwitchTurn();
        }
        void AIGameEnd(int playerNum)
        {
            Debug.Log($"🏆 Player {playerNum + 1} WINS!");
            gameoverPanel.SetActive(true);


            {
                // ApiAndRoomManager._instance.WinnerLossAIChallenge(playerNum == 0 ?
                //     staticVariables.UserProfiledata.user._id.ToString() : "ai");
                // GuestDataManager._instance.UpdateGuestCoins(playerNum == 0 ?
                //     staticVariables.currentPrize * 2 : 0);

                if (playerNum == 0)
                {
                    // winPanelPotrait.gameObject.SetActive(true);
                    // wonPanel.SetActive(true);
                    finishGame = true;

                    //winnerLooserInit.WinPlayer(true, staticVariables.UserProfiledata.user._id.ToString());
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, staticVariables.UserProfiledata.user._id.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }

                }
                else
                {
                    // winPanelPotrait.gameObject.SetActive(true);
                    // losePanel.SetActive(true);
                    finishGame = true;
                    //  winnerLooserInit.WinPlayer(false, "ai");
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, "ai");
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                }
            }

        }
        // ========== MOVEMENT ==========
        public IEnumerator MovePlayer(string playerNum, int turnNumber)
        {
            yield return new WaitForSeconds(2f);
            Debug.Log("PLayerNum" + playerNum);
            Debug.Log($"🏃 Moving Player {playerNum}: {turnNumber} steps");

            int playerindex = playerNum == Player1Soldier.GetComponent<SnakePlayerRef>().playerID ? 1 : 2;
            Debug.Log("playerindex" + playerindex);
            Node currentNode = playerNum == Player1Soldier.GetComponent<SnakePlayerRef>().playerID ? CurrentNodePlayer1 : CurrentNodePlayer2;
            string flowFrom = currentNode != null ? currentNode.name : "?"; // match log only
            List<Vector3> path = new List<Vector3>();
            Transform playerSoldier = playerNum == Player1Soldier.GetComponent<SnakePlayerRef>().playerID ? Player1Soldier : Player2Soldier;
            Debug.Log("PLayerNum1" + playerNum);
            float initialZ = playerSoldier.position.z;
            bool isSkipMove = false;
            bool isGameEnd = false;

            for (int i = 0; i < turnNumber; i++)
            {
                if (currentNode == EndNode)
                {
                    isSkipMove = true;
                    break;
                }

                currentNode = currentNode.GetNextNode();
                if (currentNode == null)
                {
                    Debug.LogError("❌ Next Node missing!");
                    break;
                }

                Vector3 newPosition = currentNode.transform.position;
                newPosition.z = initialZ;
                path.Add(newPosition);
            }

            if (currentNode == EndNode && path.Count == turnNumber)
            {
                isGameEnd = true;
            }

            MatchFlow.Log("Snake & Ladder", isSkipMove
                ? $"{MatchFlow.Who(playerNum)} rolled a {turnNumber} — {flowFrom} + {turnNumber} overshoots 100, no move"
                : $"{MatchFlow.Who(playerNum)} rolled a {turnNumber} — {flowFrom} → {(currentNode != null ? currentNode.name : "?")}");

            if (isSkipMove)
            {
                SwitchTurn();
                yield break;
            }
            //for (int i = 0; i < turnNumber; i++)
            //{
            //    currentNode = currentNode.GetNextNode();
            //    if (currentNode == null)
            //    {
            //        Debug.LogError("❌ Next Node missing!");
            //        break;
            //    }

            //    Vector3 newPosition = currentNode.transform.position;
            //    newPosition.z = initialZ;
            //    path.Add(newPosition);

            //    if (currentNode == EndNode)
            //    {
            //        Debug.Log("🏁 Reached the end node!");
            //        isGameEnd = true;
            //        break;
            //    }
            //}

            if (path.Count > 0)
            {

                playerSoldier.DOPath(path.ToArray(), DurationToNextNode * path.Count, PathType.Linear)
                       .SetEase(Ease.Linear)
                       .OnComplete(() =>
                       {
                           if (isGameEnd)
                           {
                                if (playerindex == 1)
                                    CurrentNodePlayer1 = currentNode;
                                else
                                    CurrentNodePlayer2 = currentNode;

                                Debug.Log($"Player {playerindex} has reached the end node.");
                                if (isOnline)
                                {
                                    if (NetworkServer.active && SnakeMirrorNetworkManager.Instance != null)
                                    {
                                        MatchFlow.Log("Snake & Ladder", $"{MatchFlow.Who(playerNum)} reached 100!");
                                        SnakeMirrorNetworkManager.Instance.ServerCompleteMove();
                                        SnakeMirrorNetworkManager.Instance.ServerDeclareBoardWinner(playerNum);
                                    }
                                }
                               else
                               {
                                   Debug.Log("Calling GameEnd for Player 1.");
                                   GameEnd(playerNum);
                               }
                               // GameEnd(playerNum);
                           }
                           else
                           {
                               if (playerindex == 1)
                                   CurrentNodePlayer1 = currentNode;
                               else
                                   CurrentNodePlayer2 = currentNode;
                               CheckForTransitionNode(playerNum);
                           }
                       });

                //playerSoldier.DOPath(path.ToArray(), DurationToNextNode * path.Count, PathType.Linear)
                //        .SetEase(Ease.Linear)
                //        .OnComplete(() =>
                //        {
                //            if (isGameEnd)
                //            {
                //                Debug.Log("Player 1 has reached the end node.");
                //                if (isOnline)
                //                {
                //                    Debug.Log("Sending Player 1 finish command to server.");
                //                    SnakeMirrorNetworkManager.Instance.RpcActivateUIAndHandleResult(true,staticVariables.UserProfiledata.user._id.ToString());
                //                }
                //                else
                //                {
                //                    Debug.Log("Calling GameEnd for Player 1.");
                //                    GameEnd(playerNum);
                //                }
                //                // GameEnd(playerNum);
                //            }
                //            else
                //            {
                //                if (playerindex == 1)
                //                    CurrentNodePlayer1 = currentNode;
                //                else
                //                    CurrentNodePlayer2 = currentNode;
                //                CheckForTransitionNode(playerNum);
                //            }
                //        });

            }
        }
        void MyLevelComplete()
        {
            SnakeMirrorNetworkManager.Instance.CmdPlayerFinished(staticVariables.UserProfiledata.user._id);
        }
        void CheckForTransitionNode(string playerNum)
        {
            Node currentNode = playerNum == Player1Soldier.GetComponent<SnakePlayerRef>().playerID ? CurrentNodePlayer1 : CurrentNodePlayer2;
            Transform playerSoldier = playerNum == Player1Soldier.GetComponent<SnakePlayerRef>().playerID ? Player1Soldier : Player2Soldier;

            foreach (var entry in Environment.ListOfSnakesAndLadder)
            {
                if (entry.TriggerNode == currentNode)
                {
                    Debug.Log($"🐍/🪜 Player {playerNum + 1} hit {entry.type}!");
                    MatchFlow.Log("Snake & Ladder", (entry.type == SnakesAndLadderEntry.Type.Ladder
                        ? $"{MatchFlow.Who(playerNum)} climbed a ladder "
                        : $"{MatchFlow.Who(playerNum)} bitten by a snake ")
                        + $"{(entry.TriggerNode != null ? entry.TriggerNode.name : "?")} → {(entry.TargetNode != null ? entry.TargetNode.name : "?")}");

                    if (entry.PathPoints.Count > 0)
                    {
                        List<Vector3> transitionPath = new List<Vector3>();
                        foreach (var point in entry.PathPoints)
                        {
                            Vector3 newPosition = point.position;
                            newPosition.z = playerSoldier.position.z;
                            transitionPath.Add(newPosition);
                        }

                        playerSoldier.DOPath(transitionPath.ToArray(), DurationToNextNode * transitionPath.Count, PathType.Linear)
                            .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                if (playerNum == Player1Soldier.GetComponent<SnakePlayerRef>().playerID)
                                    CurrentNodePlayer1 = entry.TargetNode;
                                else
                                    CurrentNodePlayer2 = entry.TargetNode;

                                HandleNodeConflict();

                                // ✅ ADD THIS - Stop timer before switching
                                if (isOnline && isServer && SnakeMirrorNetworkManager.Instance != null)
                                {
                                    SnakeMirrorNetworkManager.Instance.ServerResetTimer();
                                }

                                SwitchTurn();
                            });
                    }
                    else
                    {
                        Vector3 targetPosition = entry.TargetNode.transform.position;
                        targetPosition.z = playerSoldier.position.z;

                        playerSoldier.DOMove(targetPosition, DurationToNextNode * 3)
                            .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                if (playerNum == Player1Soldier.GetComponent<SnakePlayerRef>().playerID)
                                    CurrentNodePlayer1 = entry.TargetNode;
                                else
                                    CurrentNodePlayer2 = entry.TargetNode;

                                HandleNodeConflict();

                                // ✅ ADD THIS - Stop timer before switching
                                if (isOnline && isServer && SnakeMirrorNetworkManager.Instance != null)
                                {
                                    SnakeMirrorNetworkManager.Instance.ServerResetTimer();
                                }

                                SwitchTurn();
                            });
                    }
                    return;
                }
            }

            HandleNodeConflict();

            // ✅ ADD THIS - Stop timer before switching
            if (isOnline && isServer && SnakeMirrorNetworkManager.Instance != null)
            {
                SnakeMirrorNetworkManager.Instance.ServerResetTimer();
            }

            SwitchTurn();
        }

        void HandleNodeConflict()
        {
            if (CurrentNodePlayer1 == CurrentNodePlayer2 && CurrentNodePlayer1 != null)
            {
                Vector3 player1NewPosition = Player1Soldier.position;
                Vector3 player2NewPosition = Player2Soldier.position;

                Player1Soldier.DOScale(ShrinkScale, 0.5f);
                Player2Soldier.DOScale(ShrinkScale, 0.5f);

                player1NewPosition.y -= yShrinkOffset;
                player2NewPosition.y += yShrinkOffset;

                Player1Soldier.DOMove(player1NewPosition, 0.5f);
                Player2Soldier.DOMove(player2NewPosition, 0.5f);
            }
            else
            {
                Player1Soldier.DOScale(Vector3.one, 0.5f);
                Player2Soldier.DOScale(Vector3.one, 0.5f);

                Player1Soldier.DOMove(new Vector3(CurrentNodePlayer1.transform.position.x,
                    CurrentNodePlayer1.transform.position.y, Player1Soldier.position.z), 0.5f);
                Player2Soldier.DOMove(new Vector3(CurrentNodePlayer2.transform.position.x,
                    CurrentNodePlayer2.transform.position.y, Player2Soldier.position.z), 0.5f);
            }
        }
        void AISwitchTurn()
        {
            count = 0;
            Debug.Log("🔄 Switching turns");

            if (timerRunning)
                StopTimerAI();

            if (isPlayer1Turn)
            {
                isPlayer1Turn = false;
                isPlayer2Turn = true;
                player1Arrow.gameObject.SetActive(false);
                Player1TimerImage.gameObject.SetActive(false);
                if (currentGameMode.Equals(GameMode.Against_Ai))
                {
                    Player2TimerImage.gameObject.SetActive(true);
                    Invoke(nameof(AITakeTurn), 2.5f);
                }
                else if (isOnline && myPlayerNumber == 1)
                {
                    player2Arrow.gameObject.SetActive(true);
                    Player2TimerImage.gameObject.SetActive(true);
                }
                else if (!isOnline)
                {
                    player2Arrow.gameObject.SetActive(true);
                    Player2TimerImage.gameObject.SetActive(true);
                }
                else
                {
                    Player2TimerImage.gameObject.SetActive(true);
                }
            }
            else
            {
                isPlayer2Turn = false;
                isPlayer1Turn = true;
                player2Arrow.gameObject.SetActive(false);
                Player2TimerImage.gameObject.SetActive(false);
                if (isOnline && myPlayerNumber == 0)
                {
                    player1Arrow.gameObject.SetActive(true);
                    Debug.LogWarning("startTimer6");
                    //  StartTimer();
                }
                else if (!isOnline)
                {
                    Debug.LogWarning("startTimer7");
                    player1Arrow.gameObject.SetActive(true);
                    Player1DiceButton.interactable = true;
                    Player1Button.interactable = true;

                    StartTimerAI();
                }
            }
        }

        void SwitchTurn()
        {
            count = 0;
            Debug.Log("🔄 Switching turns");

            // ✅ Stop server timer when manually switching turn
            if (isOnline && isServer && SnakeMirrorNetworkManager.Instance != null)
            {
                SnakeMirrorNetworkManager.Instance.ServerResetTimer(); // Changed from ServerStopTimer
            }

            if (timerRunning)
                StopTimer();

            // Switch turn on server
            if (isServer)
            {
                SnakeMirrorNetworkManager.Instance.ServerCompleteMove();
                SnakeMirrorNetworkManager.Instance.currentTurnPlayerNumber =
                    SnakeMirrorNetworkManager.Instance.currentTurnPlayerNumber == 0 ? 1 : 0;

                SnakeMirrorNetworkManager.Instance.RpcNotifyFirstTurn(
                    SnakeMirrorNetworkManager.Instance.currentTurnPlayerNumber);
                MatchFlow.Log("Snake & Ladder", $"turn → {SnakeMirrorNetworkManager.FlowWho(SnakeMirrorNetworkManager.Instance.currentTurnPlayerNumber)}");

                // Restart timer for the new player's turn
                SnakeMirrorNetworkManager.Instance.ServerStartTimer();
            }
            // For offline/AI mode
            else if (!isOnline)
            {
                if (isPlayer1Turn)
                {
                    isPlayer1Turn = false;
                    isPlayer2Turn = true;
                    player1Arrow.gameObject.SetActive(false);

                    if (currentGameMode.Equals(GameMode.Against_Ai))
                    {
                        Player2TimerImage.gameObject.SetActive(true);
                        Invoke(nameof(AITakeTurn), 2.5f);
                    }
                    else
                    {
                        player2Arrow.gameObject.SetActive(true);
                        Player2TimerImage.gameObject.SetActive(true);
                    }
                }
                else
                {
                    isPlayer2Turn = false;
                    isPlayer1Turn = true;
                    player2Arrow.gameObject.SetActive(false);
                    player1Arrow.gameObject.SetActive(true);
                    Player1DiceButton.interactable = true;
                    Player1Button.interactable = true;
                }
            }
        }
        int PLayerTurnlimit = 0;
        // ========== TIMER ==========
        //private void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.Space))
        //    {
        //        winPanelPotrait.gameObject.SetActive(true);
        //        wonPanel.SetActive(true);
        //        winnerLooserInit.WinPlayer(true, staticVariables.UserProfiledata.user._id.ToString());
        //    }
        //    if (timerRunning)
        //    {
        //        timer -= Time.deltaTime;
        //        Player1TimerImage.fillAmount = 1 - (timer / TimerDuration);
        //        Debug.Log("SnaketimerRunning" + timerRunning);
        //        if (timer <= 0)
        //        {
        //            Debug.Log("SnakeTimer" + timer);
        //            timerRunning = false;
        //            Player1TimerImage.gameObject.SetActive(false);
        //            if (NetworkServer.active)
        //            {
        //                Debug.Log("ServerTimer");
        //                SnakeMirrorNetworkManager.Instance.currentTurnPlayerNumber = SnakeMirrorNetworkManager.Instance.currentTurnPlayerNumber == 0 ? 1 : 0;

        //                SnakeMirrorNetworkManager.Instance.RpcNotifyFirstTurn(SnakeMirrorNetworkManager.Instance.currentTurnPlayerNumber);
        //            }
        //            else if (NetworkClient.active) return;
        //            SwitchTurn();
        //            PLayerTurnlimit++;
        //            if (PLayerTurnlimit >= 4)
        //            {
        //                Debug.Log("Oppenent Win");
        //            }
        //            if (myPlayerNumber == 0)
        //            {
        //                TakeTurn(0, true);
        //            }
        //        }
        //    }
        //}
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("GamePlayGamePrice" + staticVariables.currentPrize);
                //winPanelPotrait.gameObject.SetActive(true);
                //wonPanel.SetActive(true);
                // winnerLooserInit.WinPlayer(true, staticVariables.UserProfiledata.user._id.ToString());
            }
            if (isOnline)
            {
                if (timerRunning)
                {
                    if (Player1TimerImage.gameObject.activeSelf)
                    {
                        Player1TimerImage.fillAmount = 1 - (timer / TimerDuration);
                    }

                    if (Player2TimerImage.gameObject.activeSelf)
                    {
                        Player2TimerImage.fillAmount = 1 - (timer / TimerDuration);
                    }
                }
            }
            else
            {
                if (timerRunning)
                {
                    timer -= Time.deltaTime;
                    Player1TimerImage.fillAmount = 1 - (timer / TimerDuration);
                   // Debug.Log("SnaketimerRunning" + timerRunning);
                    if (timer <= 0)
                    {
                        Debug.Log("SnakeTimer" + timer);
                        timerRunning = false;
                        Player1TimerImage.gameObject.SetActive(false);
                        Player1DiceButton.interactable = false;
                        Player1Button.interactable = false;
                        AISwitchTurn();
                        PLayerTurnlimit++;
                        if (PLayerTurnlimit >= 4)
                        {
                            Debug.Log("Oppenent Win");
                        }
                        if (myPlayerNumber == 0)
                        {
                            TakeTurn(0, true);
                        }
                    }
                }
            }
            // Only update UI based on timer value (server controls the actual timer)


        }

        // Remove StartTimer and StopTimer methods or make them simpler
        private void StartTimer()
        {
            // Client side - just show UI
            Player1TimerImage.fillAmount = 0;
            Player1TimerImage.gameObject.SetActive(true);
        }

        private void StopTimer()
        {
            // Client side - just hide UI
            timerRunning = false;
            Player1TimerImage.gameObject.SetActive(false);
            Player2TimerImage.gameObject.SetActive(false);
        }

        private void StartTimerAI()
        {
            timer = TimerDuration;
            timerRunning = true;
            Player1TimerImage.fillAmount = 0;
            Player1TimerImage.gameObject.SetActive(true);
            Player1DiceButton.interactable = true;
            Player1Button.interactable = true;
        }

        private void StopTimerAI()
        {
            timerRunning = false;
            Player1TimerImage.gameObject.SetActive(false);
        }

        // ========== GAME END ==========
        void GameEnd(string playerNum)
        {
            Debug.Log($"🏆 Player {playerNum + 1} WINS!");
            gameoverPanel.SetActive(true);

            // if (currentGameMode.Equals(GameMode.Against_Ai))
            {
                // ApiAndRoomManager._instance.WinnerLossAIChallenge(playerNum == staticVariables.UserProfiledata.user._id.ToString() ?
                //     staticVariables.UserProfiledata.user._id.ToString() : "ai");
                //  GuestDataManager._instance.UpdateGuestCoins(playerNum == 0 ?
                //  staticVariables.currentPrize * 2 : 0);

                if (playerNum == staticVariables.UserProfiledata.user._id.ToString())
                {
                    // winPanelPotrait.gameObject.SetActive(true);
                    //wonPanel.SetActive(true);
                    finishGame = true;
                    // winPanelPotrait.onWinInit(true);
                    // winnerLooserInit.WinPlayer(true, staticVariables.UserProfiledata.user._id.ToString());
                    // winnerLooserInit.InitializeLoserProfile(scriptable.aiImg, "Ai");
                    // winnerLooserInit.InitializeWinnerProfile(staticVariables.ProfilePicture,
                    //     staticVariables.UserProfiledata.user.first_name);
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, staticVariables.UserProfiledata.user._id.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                }
                else
                {
                    // winPanelPotrait.gameObject.SetActive(true);
                    //  losePanel.SetActive(true);
                    finishGame = true;
                    // winPanelPotrait.onWinInit(false);
                    // winnerLooserInit.WinPlayer(false, "ai");
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, "ai");
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                }
            }
            //else
            //{
            //    ApiAndRoomManager._instance.WinnerLossChallenge(playerNum == 0 ?
            //        staticVariables.UserProfiledata.user._id.ToString() :
            //        staticVariables.OpponetProfile.userId);

            //    bool iWon = (playerNum == myPlayerNumber);

            //    if (iWon)
            //    {
            //        winPanelPotrait.gameObject.SetActive(true);
            //        wonPanel.SetActive(true);
            //        finishGame = true;
            //        winPanelPotrait.onWinInit(true);
            //        winnerLooserInit.InitializeWinnerProfile(staticVariables.ProfilePicture,
            //            staticVariables.UserProfiledata.user.first_name);
            //        winnerLooserInit.InitializeLoserProfile(staticVariables.opponentImage,
            //            staticVariables.OpponetProfile.userName);
            //    }
            //    else
            //    {
            //        winPanelPotrait.gameObject.SetActive(true);
            //        losePanel.SetActive(true);
            //        finishGame = true;
            //        winPanelPotrait.onWinInit(false);
            //        winnerLooserInit.InitializeLoserProfile(staticVariables.ProfilePicture,
            //            staticVariables.UserProfiledata.user.first_name);
            //        winnerLooserInit.InitializeWinnerProfile(staticVariables.opponentImage,
            //            staticVariables.OpponetProfile.userName);
            //    }
            //}
        }

        // ========== SET ENVIRONMENT ==========
        public void SetEnvironment(int randomIndex)
        {
            Debug.Log($"🎨 Setting Environment randomIndex: {randomIndex}");
            updateCoinStatus();
            if (randomIndex >= 0 && randomIndex < AllEnvironments.Count)
            {
                currentEnvironmentIndex = randomIndex; // ✅ Store current index
                Environment = AllEnvironments[randomIndex];
                EnvironmentImage.sprite = Environment.environmentSprite;

                Debug.Log($"✅ Environment set successfully: {Environment.environmentSprite.name}");
            }
            else
            {
                Debug.LogError($"❌ Invalid environment index: {randomIndex}");
            }
        }
        // ========== NETWORK CALLBACKS ==========
        //void OnPlayerLeft(string playerName, bool isLocal)
        //{
        //    Time.timeScale = 0;
        //    SnakeMirrorNetworkManager.Instance?.EndMatch();

        //    if (isLocal)
        //    {
        //        if (finishGame == false)
        //        {
        //            disconnectedPanel.SetActive(true);
        //            disconnectedText.text = $"<color=red>You Left The Match</color>";
        //        }
        //    }
        //    else
        //    {
        //        GameEnd(myPlayerNumber);
        //        disconnectedText.text = $"<color=green>{playerName} Disconnected!</color>";
        //    }
        //}

        //void OnDisconnected()
        //{
        //    Time.timeScale = 0;
        //    if (SnakeMirrorNetworkManager.Instance != null && !SnakeMirrorNetworkManager.Instance.gameOver)
        //    {
        //        if (finishGame == false)
        //            disconnectedPanel.SetActive(true);
        //    }
        //    disconnectedText.text = "You Got Disconnected";
        //}

        // ========== CLEANUP ==========
        //private void OnDestroy()
        //{
        //    if (SnakeMirrorNetworkManager.Instance != null)
        //    {
        //        Debug.Log("🧹 Unsubscribing from SnakeMirrorNetworkManager events");

        //        SnakeMirrorNetworkManager.Instance.AssignPlayerNumber -= OnAssignPlayerNumber;
        //        SnakeMirrorNetworkManager.Instance.OpponentMoved -= OnOpponentMove;
        //        SnakeMirrorNetworkManager.Instance.PlayerLeft -= OnPlayerLeft;
        //        SnakeMirrorNetworkManager.Instance.FirstTurnPlayer -= OnFirstTurnPlayer;
        //        SnakeMirrorNetworkManager.Instance.Disconnected -= OnDisconnected;
        //        SnakeMirrorNetworkManager.Instance.StartGame -= OnGameStart;
        //    }

        //    if (isOnline)
        //    {
        //        SnakeMirrorNetworkManager.Instance?.EndMatch();
        //        Time.timeScale = 1;
        //    }

        //    // Clean up instantiated players (Multiplayer only)
        //    if (player1Instance != null) Destroy(player1Instance);
        //    if (player2Instance != null) Destroy(player2Instance);
        //}
        public void RestoreTurnState(int turnPlayerNumber)
        {
            Debug.Log($"🔄 Restoring turn state - Player {turnPlayerNumber + 1}'s turn");

            // Reset count to allow turn
            count = 0;

            // Stop any running timers
            StopTimer();
            Player2TimerImage.gameObject.SetActive(false);

            // Set correct turn
            if (turnPlayerNumber == 0)
            {
                // Show arrow and timer for Player 1 if it's my turn
                if (myPlayerNumber == 0)
                {
                    player1Arrow.gameObject.SetActive(true);
                    // StartTimer();
                    Debug.LogWarning("startTimer10");
                    Debug.Log("✅ My turn (Player 1) restored!");
                }
                else
                {
                    player1Arrow.gameObject.SetActive(false);
                    Player2TimerImage.gameObject.SetActive(true);
                    Debug.Log("⏳ Opponent's turn (Player 1)");
                }
            }
            else if (turnPlayerNumber == 1)
            {


                // Show arrow and timer for Player 2 if it's my turn
                if (myPlayerNumber == 1)
                {
                    player2Arrow.gameObject.SetActive(true);
                    Player2TimerImage.gameObject.SetActive(true);
                    Debug.Log("✅ My turn (Player 2) restored!");
                }
                else
                {
                    player2Arrow.gameObject.SetActive(false);
                    Player1TimerImage.gameObject.SetActive(true);
                    Debug.Log("⏳ Opponent's turn (Player 2)");
                }
            }
        }
        public void GameLeaveButton()
        {
            if (NetworkClient.active)
            {
                if (!isLeavingOnlineGame)
                    StartCoroutine(LeaveOnlineGameAfterForfeit());
            }
            else
            {
                SceneManager.LoadScene("Home");
            }
        }

        private IEnumerator LeaveOnlineGameAfterForfeit()
        {
            isLeavingOnlineGame = true;
            if (SnakeMirrorNetworkManager.Instance != null)
                SnakeMirrorNetworkManager.Instance.RequestLocalForfeit();

            yield return new WaitForSecondsRealtime(.5f);

            if (MirrorNetwork.Instance != null)
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            if (NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)DisconnectReason.ApplicationQuit);

            yield return new WaitForSecondsRealtime(.2f);

            if (NetworkClient.active && NetworkManager.singleton != null)
                NetworkManager.singleton.StopClient();
            SceneManager.LoadScene("Home");
        }
        private void OnDestroy()
        {
            MirrorNetwork.OnWinCall -= AnnounceVictory;
        }

        public void AnnounceVictory(string reason)
        {
            if (SnakeMirrorNetworkManager.Instance != null)
                SnakeMirrorNetworkManager.Instance.RequestExistingDisconnectResult();
        }

        public void Announcelosser()
        {
            if (currentGameMode != GameMode.Against_Ai)
            {
                //  GameEnd(1);
            }
        }
        public void updateCoinStatus()
        {
            if (isOnline)
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
                            coinSprite.sprite = SpritesManager.Instance.spritesScriptable.goldenCoin;
                        }
                        else
                        {
                            challengeAmount.color = new Color(0.75f, 0.75f, 0.75f); // Silver color
                            coinSprite.sprite = SpritesManager.Instance.spritesScriptable.goldenCoin;
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

        public void WaitingForOpponentt(bool state)
        {
            ShowWaitingForOpponent(state);
            disconnectedText.text = "Waiting for opponent";
        }
        public static Dictionary<string, SnakeRecord> PlayerSnakeData = new Dictionary<string, SnakeRecord>();
        public struct SnakeRecord
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
            public Quaternion Rotation;

            public int CurrentWaypoint;
            public int Lap;
            public int GrandTotalWaypointPassed;
            public int EnvironmentIndex;
        }

    }
}
