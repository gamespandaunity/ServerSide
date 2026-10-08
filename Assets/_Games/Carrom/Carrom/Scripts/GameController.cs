using System.Runtime.CompilerServices;
using BEKStudio;
using Mirror;
using Snake_Ladder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;

namespace BEKStudio
{
    public class GameController : NetworkBehaviour
    {
        public GameObject CarromNetworkManagerPrefab, PlayerPuckPrefab;

        public static GameController Instance;
        public enum GameState { READY, WAIT, SWITCH_MASTER, WIN, LOSE };

        [SyncVar]
        public GameState gameState;

        public enum CurrentPlayer { ME, OTHER };

        // What the turn banner should announce when CheckTurn hands control back.
        //   NewTurn    - the turn changed hands ("Your Turn!" / "Opponent Turn!").
        //   ShootAgain - the SAME player keeps the turn because they potted their
        //                own bead ("Go On").
        //   Restore    - turn state is only being RE-APPLIED (reconnect resync, or a
        //                SyncVar catch-up after a missed Rpc). Nothing was won here,
        //                so it must never say "Go On" - that reads as a shoot-again
        //                reward the player never earned. Shows the plain turn banner.
        // The old bool `showmsg` conflated ShootAgain with Restore, which is why a
        // reconnecting player was greeted with "Go On".
        public enum TurnBanner { NewTurn, ShootAgain, Restore };

        [SyncVar]
        public CurrentPlayer CurrentTurn;

        public Rigidbody2D playerPuck;
        public List<GameObject> homePucksCollected;
        public List<GameObject> awayPucksCollected;
        public List<GameObject> pucksCollected;

        [Header("Top")]
        public TextMeshProUGUI topTotalBetText, ping;
        public RawImage topHomeAvatar;
        public Image topHomeAvatarTimer;
        public TextMeshProUGUI topHomeNameText;
        public TextMeshProUGUI topHomeScoreText;
        public RawImage topAwayAvatar;
        public Image topAwayAvatarTimer;
        public TextMeshProUGUI topAwayNameText;
        public TextMeshProUGUI topAwayScoreText;
        public Transform topCoinParentForAnim;

        [Header("Toast Message")]
        public GameObject toastMessage;
        public TextMeshProUGUI toastMessageText;

        [Header("Settings")]
        public GameObject settingsScreen;
        public Image settingsBackground;
        public RectTransform settingsContinue;
        public RectTransform settingsMenu;
        public Slider playerSlider;
        public Slider opponentSlider;

        [Header("Puck")]
        public Puck[] allPucks;
        public float playerPuckMinX;
        public float playerPuckMaxX;

        [Header("Others")]
        public bool isPucksFixed;
        public bool shootAgain;
        public string myPuck;

        [SyncVar]
        public string masterClientTag;



        public bool reduceTimer;
        public GameObject redPuck;
        public GameObject statusScreen;
        public Image statusBackground;
        public RectTransform statusPanel;
        public TextMeshProUGUI statusPanelText;

        [SyncVar]
        public bool redPuckCollected;

        public bool practiceMode;

        [SyncVar]
        public bool redPuckWaiting;
        // Colour of the player who potted the queen and still owes the cover shot.
        [SyncVar]
        private string queenPendingTag;

        public string QueenPendingTag => queenPendingTag;

        public RectTransform LeftPlayer;
        public RectTransform RightPlayer;
        public List<GameObject> PuckHolesMaster;
        public List<GameObject> PuckHolesNonMaster;
        public Pool WhitePool;
        public Pool BlackPool;
        public Pool redPool;
        public GameObject leftRedPuckIcon;
        public GameObject rightRedPuckIcon;
        public GameObject gameOverPanel;
        public GameObject DisplayMessage;
        public TextMeshProUGUI DisplayMessageText;
        public SpritesHOlder scriptable;

        private bool queenCheck;
        public Action Event_SwitchingMasterClient;
        public Action Event_MasterClientSwithced;

        public static int currentHomeScore;
        public static int currentAwayScore;
        public bool isSliding;

        public CircleCollider2D[] puckHoles;

        public StrikerPositionAdjuster strikerPositionAdjuster;
        public GameObject waitingOpponentPanel;
        public GameObject micObj, speakerObj;

        public GameObject mainBoard;
        public GameObject playerCamera;
        public bool isCarronMultiplayerGame;
        public static bool isGameStarted;
        public string gameWinner;
        public TextMeshProUGUI countDownGameTimer;
        void Awake()
        {
            isCarronMultiplayerGame = true;
            GameManager.Instance.currentGameMode = GameManager.GameMode.AgainstFriend;
            Debug.Log("CurrentMode: " + GameManager.Instance.currentGameMode);
            Debug.Log("isCarronMultiplayerGame: " + isCarronMultiplayerGame);

            if (Instance == null)
            {
                Instance = this;
            }
            if (!isCarronMultiplayerGame) return;
            MirrorNetwork.Instance.isMasterClient.Show("IsMaster");
            if (MirrorNetwork.Instance.isMasterClient)
            {
                GameManager.Instance.masterClient = true;
                // Set Left Player
                LeftPlayer.anchorMin = new Vector2(0, 1);
                LeftPlayer.anchorMax = new Vector2(0, 1);
                LeftPlayer.pivot = new Vector2(0, 1);
                LeftPlayer.anchoredPosition = new Vector2(73, -38);

                //// Set Right Player
                RightPlayer.anchorMin = new Vector2(1, 1);
                RightPlayer.anchorMax = new Vector2(1, 1);
                RightPlayer.pivot = new Vector2(1, 1);
                RightPlayer.anchoredPosition = new Vector2(-73, -38);
                "Iiiiii mmmmmmmmmm Maaaaasterrrrr Hahahahah".Show();
            }
            //Photon Removal  
            else
            {
                GameManager.Instance.masterClient = false;
                // Set Left Player to Right position
                if (!NetworkServer.active)
                {

                    LeftPlayer.anchorMin = new Vector2(1, 1);
                    LeftPlayer.anchorMax = new Vector2(1, 1);
                    LeftPlayer.pivot = new Vector2(1, 1);
                    LeftPlayer.anchoredPosition = new Vector2(-73, -38);

                    //// Set Right Player to Left position
                    RightPlayer.anchorMin = new Vector2(0, 1);
                    RightPlayer.anchorMax = new Vector2(0, 1);
                    RightPlayer.pivot = new Vector2(0, 1);
                    RightPlayer.anchoredPosition = new Vector2(73, -38);
                    mainBoard.transform.rotation = Quaternion.Euler(0, 0, 180);
                    playerSlider.direction = Slider.Direction.RightToLeft;
                    "Iiiiii mmmmmmmmmm Client hunh".Show();
                }

            }
            // MirrorPlayerPrefab[] playerlist = FindObjectsOfType<MirrorPlayerPrefab>();
            // foreach (var item in playerlist)
            // {
            //     CarromNetworkManager.instance.PlayercurrentTurnId.Show("currentPlayerTurn");
            //     if (item.playerData.playerId == CarromNetworkManager.instance.PlayercurrentTurnId)
            //     {
            //         if (item.IsMaster)
            //         {
            //             Debug.Log("IsMasterpuckRest");
            //             //  transform.localPosition = new Vector2(puckStartPos.x, puckStartPos.y);
            //             transform.localPosition = new Vector2(0f, 1.67f);
            //         }
            //         else
            //         {
            //             Debug.Log("NotIsMasterpuckRest");
            //             playerSlider.direction = Slider.Direction.RightToLeft;
            //             mainBoard.transform.rotation = Quaternion.Euler(0, 0, 180);
            //             Debug.Log("Localtransform" + transform.localPosition);
            //             //   transform.localPosition = new Vector2(puckStartPos.x, puckStartPos.y - 0.08f);
            //         }
            //     }
            // }
        }
        private void OnDisable()
        {
            MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
        }

        private void OnEnable()
        {
            MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
        }

        public void DirectResult(bool result)
        {
            if (result)
            {
                // Victory(staticVariables.UserProfiledata.user._id, "Opponent has disconnected. You are declared the winner.");
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }
            else
            {
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(false, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }

        }
        public void SpawnNetworkObject()
        {
            var network = Instantiate(CarromNetworkManagerPrefab);
            NetworkServer.Spawn(network);
            var playerpuck = Instantiate(PlayerPuckPrefab, strikerPositionAdjuster.transform);
            NetworkServer.Spawn(playerpuck);

        }

        void Start()
        {

            if (NetworkServer.active || NetworkClient.active)
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
            if (NetworkServer.active)
            {
                SpawnNetworkObject();
            }
            else
            {
                Debug.Log("PlayerClientSide");
            }
            Screen.orientation = ScreenOrientation.Portrait;

            if (!MirrorNetwork.Instance.isMasterClient)
            {
                CurrentTurn = CurrentPlayer.OTHER;
            }

            if (!isCarronMultiplayerGame)
            {
                var playerpuck = Instantiate(PlayerPuckPrefab, strikerPositionAdjuster.transform);
                CurrentTurn = CurrentPlayer.ME;
                //  micObj.SetActive(false);
                //  speakerObj.SetActive(false);
            }
            this.DelayUntil(() => PlayerPuck.Instance != null, () =>
            {
                playerPuck = PlayerPuck.Instance.GetComponent<Rigidbody2D>();
            });
            masterClientTag = "White";
            // CheckTurn();
            // FIRST-TURN fix ("first turn missed/skipped"): BOTH clients used to fire
            // CmdSwitchTurn(their own id) right here at scene start. The server toggled the turn
            // TWICE and the final holder depended on packet arrival order (50/50), so the first
            // turn often disagreed with the "YOU START" banner and got skipped.
            // Now ONLY the MASTER client kicks the first turn, exactly once, passing the JOINER
            // as "current" — the toggle hands the first turn to the CREATOR ("YOU START").
            // CmdgameStarted() additionally assigns it server-side on newer server builds; the
            // PlayercurrentTurnId empty-check + stale-guard make the two paths idempotent.
            if (isCarronMultiplayerGame && !isGameStarted && !NetworkServer.active
                && MirrorNetwork.Instance != null && MirrorNetwork.Instance.isMasterClient
                && staticVariables.OpponetProfile != null
                && !string.IsNullOrEmpty(staticVariables.OpponetProfile.userId))
            {
                CarromNetworkManager.instance.CmdRequestInitialTurn(staticVariables.OpponetProfile.userId);
            }
            homePucksCollected = new List<GameObject>();
            awayPucksCollected = new List<GameObject>();
            pucksCollected = new List<GameObject>();

            myPuck = isCarronMultiplayerGame ? GetPlayerTag() : "White";

            redPuck.SetActive(true);

            if (GameManager.Instance.currentGameMode == GameManager.GameMode.Ai)
            {
                myPuck = "White";
                SetTopUsersForBot();
            }
            else
            {
                if (NetworkServer.active)
                {
                    myPuck = "White";
                }
                else
                {
                    if (MirrorNetwork.Instance.isMasterClient)
                    {
                        myPuck = "White";
                    }
                    else
                    {
                        myPuck = "Black";
                    }
                }
                SetTopUsersForGame();
            }

            if (practiceMode)
            {
                topTotalBetText.text = "0";
                topAwayNameText.transform.parent.gameObject.SetActive(false);
                topHomeScoreText.transform.parent.gameObject.SetActive(false);
            }

            isPucksFixed = true;
            gameOverPanel.SetActive(false);
            StartMessage();
        }

        string GetPlayerTag()
        {
            // Mirror doesn't have custom properties like Photon
            // You'll need to implement your own player data system
            // For now, returning default based on server/client
            return MirrorNetwork.Instance.isMasterClient ? "White" : "Black";
        }

        void SetTopUsersForBot()
        {
            topHomeNameText.text = staticVariables.UserProfiledata.user.first_name;
            topHomeAvatar.texture = staticVariables.ProfilePicture;
            topAwayNameText.text = GameManager.Instance.GetBotName();
            topAwayAvatar.texture = scriptable.aiImg;
        }

        public void SetScore(string text)
        {

        }
        void SetTopUsersForGame()
        {
            if (NetworkServer.connections.Count == 2)
            {

                // Debug.Log("Both Players Connected");
                // if (MirrorNetwork.Instance.isMasterClient)
                // {
                //     Debug.Log("MyName;" + staticVariables.UserProfiledata.user.first_name + staticVariables.UserProfiledata.user.last_name);
                //      Debug.Log("MyName;" +staticVariables.ProfilePicture);
                //     topHomeNameText.text = staticVariables.UserProfiledata.user.first_name + staticVariables.UserProfiledata.user.last_name;
                //     topHomeAvatar.texture = staticVariables.ProfilePicture;
                //     topAwayNameText.text = staticVariables.OpponetProfile.userName;
                //     topAwayAvatar.texture = staticVariables.opponentImage;
                // }
                // else
                // {
                //     Debug.Log("OpponetName;" + staticVariables.OpponetProfile.userName);
                //     Debug.Log("OpponetImage:" + staticVariables.opponentImage);
                //     topAwayNameText.text = staticVariables.UserProfiledata.user.first_name + staticVariables.UserProfiledata.user.last_name;
                //     topAwayAvatar.texture = staticVariables.ProfilePicture;
                //     topHomeNameText.text = staticVariables.OpponetProfile.userName;
                //     topHomeAvatar.texture = staticVariables.opponentImage;

                // }
            }
            else
            {
                topHomeNameText.text = staticVariables.UserProfiledata.user.first_name;
                topAwayNameText.text = "";
                topAwayAvatar.texture = null;
            }
        }

        void StartMessage()
        {
            if (myPuck == "White")
            {
                statusPanelText.text = "YOU START";
                toastMessageText.text = "Pot all white pucks and cover the queen to win the match";

            }
            else
            {
                statusPanelText.text = "OPPONENT START";

                toastMessageText.text = "Pot all black pucks and cover the queen to win the match";

            }
            this.Delay(3f, () =>
            {
                if (statusPanelText.text == "YOU START")
                {
                    if (NetworkServer.active)
                        CarromNetworkManager.instance.ServerGameStarted();
                    else
                        CarromNetworkManager.instance.CmdgameStarted();
                }
            });

            StatusPanelActive();
        }

        public void StatusPanelActive()
        {
            statusPanel.anchoredPosition = new Vector2(-940f, statusPanel.anchoredPosition.y);
            statusBackground.color = new Color(0, 0, 0, 0);
            statusScreen.SetActive(true);

            LeanTween.alpha(statusBackground.GetComponent<RectTransform>(), 0.5f, 0.5f);
            LeanTween.move(statusPanel, new Vector2(0, statusPanel.anchoredPosition.y), 0.3f).setDelay(0.5f)
                .setEase(LeanTweenType.easeOutBack).setOnComplete(() =>
                {
                    LeanTween.move(statusPanel, new Vector2(940, statusPanel.anchoredPosition.y), 0.3f).setDelay(1f)
                        .setEase(LeanTweenType.easeInBack).setOnComplete(
                            () =>
                            {
                                statusScreen.SetActive(false);
                                isGameStarted = true;



                            });
                });
        }

        private bool IsTurnStatusMessage(string message)
        {
            return message == "Your Turn!"
                   || message == "Opponent Turn!"
                   || message == "Go On"
                   || message == "Go On!"
                   || message == "YOU START"
                   || message == "OPPONENT START";
        }

        private void SetTurnStatusMessage(string message, bool showWhenHidden, bool replaceActiveMessage = true)
        {
            if (statusScreen.activeInHierarchy)
            {
                if (replaceActiveMessage || IsTurnStatusMessage(statusPanelText.text))
                {
                    statusPanelText.text = message;
                }

                return;
            }

            if (!showWhenHidden) return;

            statusPanelText.text = message;
            StatusPanelActive();
        }

        public void SetPositionsTestingPucks(int id)
        {
            int whiteCount = 0;
            int blackCount = 0;

            for (int i = 0; i < allPucks.Length; i++)
            {
                if (id == 0 && allPucks[i].gameObject.CompareTag("Red") && !allPucks[i].puckInHole)
                {
                    allPucks[i].transform.position = puckHoles[0].transform.position;
                }

                if (id == 1 && allPucks[i].gameObject.CompareTag("White") && !allPucks[i].puckInHole && whiteCount < 1)
                {
                    allPucks[i].transform.position = puckHoles[0].transform.position;
                    whiteCount++;
                }

                if (id == 2 && allPucks[i].gameObject.CompareTag("Black") && !allPucks[i].puckInHole && blackCount < 1)
                {
                    allPucks[i].transform.position = puckHoles[0].transform.position;
                    blackCount++;
                }

                if (id == 3 && allPucks[i].gameObject.CompareTag("White") && !allPucks[i].puckInHole && whiteCount < 1)
                {
                    allPucks[i].transform.position = puckHoles[0].transform.position;
                    whiteCount++;
                }

                if (id == 3 && allPucks[i].gameObject.CompareTag("Black") && !allPucks[i].puckInHole && blackCount < 1)
                {
                    allPucks[i].transform.position = puckHoles[0].transform.position;
                    blackCount++;
                }
            }
        }

        void Update()
        {
            if (ping)
                ping.text = ((int)(NetworkTime.rtt * 1000)).ToString() + " ms";
            // Manual test turn switch
            if (Input.GetKeyDown(KeyCode.T))
            {
                CarromNetworkManager.instance.CmdSwitchTurn(staticVariables.UserProfiledata.user._id);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                //PlayerPuck.Instance.ResetPosition();
            }
            //Debug.Log("Player " + CurrentTurn);
            // Debug.Log("MasterClient" + GameManager.Instance.masterClient);
            if (practiceMode) return;

            // Sirf server timer control kare
            //if (NetworkServer.active && isCarronMultiplayerGame && !isTimerRunning)
            //{
            //    StartCoroutine(ServerTimerRoutine());
            //}
        }

        void FixedUpdate()
        {
            if (gameState == GameState.READY || gameState == GameState.WAIT)
            {
                isPucksFixed = GetPuckStatus();
            }
        }


        public void SettingsBtn()
        {
            if (gameState == GameState.WIN || gameState == GameState.LOSE) return;

            settingsContinue.anchoredPosition = new Vector2(-940f, settingsContinue.anchoredPosition.y);
            settingsMenu.anchoredPosition = new Vector2(-940f, settingsMenu.anchoredPosition.y);
            settingsBackground.color = new Color(0, 0, 0, 0);
            settingsScreen.SetActive(true);
            LeanTween.alpha(settingsBackground.GetComponent<RectTransform>(), 0.75f, 0.25f).setIgnoreTimeScale(true);
            LeanTween.move(settingsContinue, new Vector2(-177, settingsContinue.anchoredPosition.y), 0.3f).setDelay(0.25f)
                .setIgnoreTimeScale(true)
                .setEase(LeanTweenType.easeOutBack);
            LeanTween.move(settingsMenu, new Vector2(177, settingsMenu.anchoredPosition.y), 0.3f).setDelay(0.5f)
                .setIgnoreTimeScale(true)
                .setEase(LeanTweenType.easeOutBack);
            LeanTween.move(settingsMenu, new Vector2(177, settingsMenu.anchoredPosition.y), 0.3f)
                .setIgnoreTimeScale(true)
                .setDelay(0.5f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => { });

            if (!isCarronMultiplayerGame)
            {
                Time.timeScale = 0;
            }
        }

        public void SettingsContinueBtn()
        {
            if (!isCarronMultiplayerGame)
            {
                Time.timeScale = 1;
            }
            settingsScreen.SetActive(false);
        }
        public static bool IsGameQuite;
        public void SettingsMenuBtn()
        {
            IsGameQuite = true;
            if (!isCarronMultiplayerGame)
            {
                Time.timeScale = 1;
            }
            settingsContinue.GetComponent<Button>().interactable = false;
            settingsMenu.GetComponent<Button>().interactable = false;

            if (isCarronMultiplayerGame)
            {

                //_snokerNetwork._runner.Shutdown(false);
                //Photon Removal PhotonNetwork.LeaveRoom();
                Debug.LogError("photon.leave room");
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                if (NetworkGameManager.Instance)
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                this.Delay(1, () => NetworkManager.singleton.StopClient());

                SceneManager.LoadScene("Home");
            }

            if (!isCarronMultiplayerGame)
            {
                SceneManager.LoadScene("Home");
            }
        }

        public void Shoot()
        {
            playerSlider.interactable = false;
            if (opponentSlider != null)
            {
                opponentSlider.interactable = false;
            }

            shootAgain = false;
            gameState = GameState.WAIT;

            playerSlider.value = 0.5f;
            if (opponentSlider != null)
            {
                opponentSlider.value = 0.5f;
            }

            StartCoroutine(WaitForPucks());
        }

        public void NavigatorSliderValueChanged()
        {
            if (CurrentTurn != CurrentPlayer.ME || gameState != GameState.READY) return;
            playerPuck.transform.localPosition = new Vector2(Mathf.Lerp(playerPuckMinX, playerPuckMaxX, playerSlider.value), playerPuck.transform.localPosition.y);
        }

        public void OnSliderPointerDown()
        {
            if (!playerSlider.interactable) return;
            OnDown();
        }

        public void OnDown()
        {
            if (isCarronMultiplayerGame)
            {
                playerPuck.GetComponent<PlayerPuck>().BroadCastSliderDown();
            }
            else
            {
                HandleOnSliderPointerDown();
            }
        }

        public void HandleOnSliderPointerDown()
        {
            isSliding = true;
            playerPuck.GetComponent<Rigidbody2D>().isKinematic = true;
            foreach (var puck in allPucks)
            {
                puck.GetComponent<Rigidbody2D>().isKinematic = true;
            }
        }

        public void OnSliderPointerUp()
        {
            if (!playerSlider.interactable) return;
            OnUp();
        }

        public void OnUp()
        {
            if (isCarronMultiplayerGame)
            {
                playerPuck.GetComponent<PlayerPuck>().BroadCastSliderUp();
            }
            else
            {
                HandleOnSliderPointerUp();
            }
        }

        public void HandleOnSliderPointerUp()
        {
            isSliding = false;
            StartCoroutine(HandleSliderPointerUp());
        }

        IEnumerator HandleSliderPointerUp()
        {
            isSliding = false;

            if (CurrentTurn == CurrentPlayer.ME)
            {
                strikerPositionAdjuster.SetPosition();
                strikerPositionAdjuster.SetSliderPosition(playerSlider);
            }

            yield return new WaitForSeconds(0.8f);
            if (isSliding) yield break;

            Rigidbody2D playerRb = playerPuck.GetComponent<Rigidbody2D>();
            // Keep the striker's Y frozen through the whole READY/aiming phase (move-area lock).
            // Previously this constraint was cleared to None on the very next line — a no-op that
            // let a puck resting in the striker's lane depenetrate the striker off its baseline in
            // Y. It is now released only at shot start in PlayerPuck.Shoot(). (See codex review.)
            playerRb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            playerRb.isKinematic = false;
            foreach (var puck in allPucks)
            {
                puck.GetComponent<Rigidbody2D>().isKinematic = false;
            }
        }

        public void SendData()
        {
            List<bool> puckInHole = new List<bool>();

            for (int i = 0; i < allPucks.Length; i++)
            {
                puckInHole.Add(allPucks[i].puckInHole);
            }

            playerPuck.GetComponent<PlayerPuck>().SendData(puckInHole.ToArray(), GameManager.Instance.masterClient);
        }

        public void SyncData(bool[] dataArray, bool IsOrigMaster)
        {
            for (int i = 0; i < dataArray.Length; i++)
            {
                if (allPucks[i].puckInHole != dataArray[i])
                {
                    Debug.LogError("Not Synced ..........................");
                }
            }
        }

        bool GetPuckStatus()
        {
            if (allPucks == null)
            {
                return false;
            }

            for (int i = 0; i < allPucks.Length; i++)
            {
                if (allPucks[i] == null)
                {
                    return false;
                }

                if (allPucks[i].isMoving)
                {
                    reduceTimer = false;
                    return false;
                }
            }

            // During a reconnect the scene GameController can begin FixedUpdate before
            // Mirror respawns the networked striker. Treat that window as "not settled"
            // instead of throwing every physics frame and flooding/pausing the Editor.
            if (PlayerPuck.Instance == null || PlayerPuck.Instance.isMoving)
            {
                return false;
            }

            return true;
        }

        public void ApplyAuthoritativeQueenPendingState(bool isWaiting, string pendingTag)
        {
            // Network turn RPCs call this before CheckTurn so the pending-cover
            // decision never depends on which SyncVar update happened to arrive first.
            // This method is only used by the online GameController/network path.
            redPuckWaiting = isWaiting;
            queenPendingTag = isWaiting ? pendingTag : null;
        }

        // Kept for the existing call sites: true = a normal turn change,
        // false = the shoot-again banner. Reconnect / SyncVar catch-up callers must
        // pass TurnBanner.Restore explicitly instead of `false`.
        public void CheckTurn(bool showmsg = true)
        {
            CheckTurn(showmsg ? TurnBanner.NewTurn : TurnBanner.ShootAgain);
        }

        public void CheckTurn(TurnBanner banner)
        {
            // A legally potted queen stays "waiting to be covered" across the SAME
            // shooter's next (shoot-again) turn. CheckTurn runs BEFORE that shot
            // resolves, so clearing the flag here wiped the pending state and hid the
            // queen icon the moment control came back — and HandleWorstCase then pulled
            // the queen back out of the hole. Every real outcome path (CheckPucks /
            // CheckPlayerPenalty) clears redPuckWaiting itself.
            // The tag check keeps this scoped to the shooter who actually potted the
            // queen: if the turn hands over instead (timeout / disconnect / foul), the
            // colour changes, the state clears and the queen returns to the board as
            // before — the opponent can never inherit the pending cover.
            // Online only; AI/offline keeps the old behaviour.
            bool queenStillWithSameShooter = isCarronMultiplayerGame
                                             && redPuckWaiting
                                             && masterClientTag == queenPendingTag;
            if (!queenStillWithSameShooter)
            {
                redPuckWaiting = false;
                queenPendingTag = null;
                if (!redPuckCollected)
                {
                    leftRedPuckIcon.SetActive(false);
                    rightRedPuckIcon.SetActive(false);
                }
            }
            reduceTimer = true;
            //timer = Constants.PLAY_TIME_FOR_PLAYER;
            // topHomeAvatarTimer.fillAmount = 0;
            // topAwayAvatarTimer.fillAmount = 0;
            Event_MasterClientSwithced?.Invoke();
            pucksCollected.Clear();
            if (NetworkServer.active)
            {
                // if (CarromNetworkManager.instance != null)
                //    CarromNetworkManager.instance.AssignStickAuthority();
            }
            if (CurrentTurn == CurrentPlayer.ME)
            {

                PlayerPuck.Instance.ResetPosition();
                // "Go On" belongs to the shoot-again path ONLY (you potted your own
                // bead and keep the strike). A restored turn just says "Your Turn!".
                SetTurnStatusMessage(banner == TurnBanner.ShootAgain ? "Go On" : "Your Turn!",
                    showWhenHidden: true);
                playerSlider.interactable = true;
                GameController.Instance.strikerPositionAdjuster.SetSliderPosition(playerSlider);
                if (opponentSlider != null)
                {
                    opponentSlider.interactable = false;
                }
            }
            else
            {
                if (GameManager.Instance.currentGameMode == GameManager.GameMode.Ai)
                {
                    PlayerPuck.Instance.ShootBot();
                }
                // A restored turn still announces itself - a player who just reconnected
                // has no idea whose strike it is. Only shoot-again stays silent here.
                SetTurnStatusMessage("Opponent Turn!", showWhenHidden: banner != TurnBanner.ShootAgain,
                    replaceActiveMessage: false);
                playerSlider.interactable = false;
                if (opponentSlider != null)
                {
                    opponentSlider.interactable = true;
                }
            }

            gameState = GameState.READY;


            for (int i = 0; i < allPucks.Length; i++)
            {
                allPucks[i].GetComponent<CircleCollider2D>().enabled = true;
            }

            HandleWorstCase();
        }

        // ---- Match-flow log helpers (logging only; MatchFlow writes on the dedicated server only) ----
        // The creator plays White (home score), the joiner Black (away score).
        internal static string FlowShooter()
        {
            var net = CarromNetworkManager.instance;
            return MatchFlow.Who(net != null ? net.PlayercurrentTurnId : null);
        }

        internal static string FlowOwner(string colourTag)
        {
            var ngm = NetworkGameManager.Instance;
            if (ngm == null) return colourTag;
            NetworkPlayerData data = colourTag == "White" ? ngm.creatorData : colourTag == "Black" ? ngm.joinerData : null;
            return data != null ? MatchFlow.Who(data.playerId) : colourTag;
        }

        internal static string FlowScores()
        {
            return $"{FlowOwner("White")} {currentHomeScore} – {currentAwayScore} {FlowOwner("Black")}";
        }

        static string FlowCoin(string tag)
        {
            switch (tag)
            {
                case "White": return "a white coin";
                case "Black": return "a black coin";
                case "Red": return "the queen";
                case "Player": return "the striker";
                default: return tag;
            }
        }

        public void PuckOnHole(string puckTag, string puckName)
        {
            if (practiceMode) return;
            if (gameState == GameState.WIN || gameState == GameState.LOSE) return;
            puckName.Show("puckInHole");
            if (MatchFlow.Enabled) MatchFlow.Log("Carrom", $"{FlowShooter()} pocketed {FlowCoin(puckTag)}");
            if (puckTag == "Player")
            {
                pucksCollected.Add(playerPuck.gameObject);
            }
            else
            {
                Puck getPuck = getPuckFromList(puckName);
                if (getPuck != null)
                {
                    pucksCollected.Add(getPuck.gameObject);
                }
            }
        }

        Puck getPuckFromList(string puckName)
        {
            for (int i = 0; i < allPucks.Length; i++)
            {
                if (allPucks[i].name == puckName)
                {
                    return allPucks[i];
                }
            }
            return null;
        }

        public IEnumerator WaitForPucks()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Debug.Log("Waiting for Pucks to Stop...");
            for (int i = 0; i < allPucks.Length; i++)
            {
                allPucks[i].setVelCheck(true);
            }
            PlayerPuck.Instance.setVelCheck(true);
            yield return new WaitUntil(() => isPucksFixed);
            for (int i = 0; i < allPucks.Length; i++)
            {
                allPucks[i].setVelCheck(false);
            }
            PlayerPuck.Instance.setVelCheck(false);
            //yield return new WaitForSecondsRealtime(1.5f);
            CheckGameStatusAfterStop();
            // if(NetworkServer.active)
            //  {
            //     CarromNetworkManager.instance.RpcCheckGameStatusAfterStop();
            //  } 

        }

        public void CheckGameStatusAfterStop()
        {
            this.Delay(1.5f, () =>
            {
                CheckGameStatus();

            });
        }

        public void HandleWorstCase()
        {
            for (int i = 0; i < allPucks.Length; i++)
            {
                // Only hide a puck if it's still in someone's collected list.
                // A puck returned to the board by a foul is no longer in any
                // list but its puckInHole SyncVar may not have propagated to
                // the reconnected client yet — without the list check we'd
                // re-hide it and the player only sees the Shadow child.
                bool stillCollected = homePucksCollected.Contains(allPucks[i].gameObject)
                                      || awayPucksCollected.Contains(allPucks[i].gameObject);

                if (allPucks[i].puckInHole && stillCollected)
                {
                    allPucks[i].OnDisableProperties(true);
                }

                if (allPucks[i].circleCollider2D && IsInPuckHoles(allPucks[i]) && stillCollected)
                {
                    allPucks[i].OnDisableProperties(true);
                    Debug.Log("In Puck Holes Collide....  ");
                }
            }

            List<GameObject> temList = new List<GameObject>();

            for (int i = 0; i < awayPucksCollected.Count; i++)
            {
                if (!awayPucksCollected[i].GetComponent<Puck>().puckInHole)
                {
                    temList.Add(awayPucksCollected[i]);
                }
            }

            for (int i = 0; i < temList.Count; i++)
            {
                awayPucksCollected.Remove(temList[i]);
                UpdateScoreText();
            }

            temList.Clear();

            for (int i = 0; i < homePucksCollected.Count; i++)
            {
                if (!homePucksCollected[i].GetComponent<Puck>().puckInHole)
                {
                    temList.Add(homePucksCollected[i]);
                }
            }

            for (int i = 0; i < temList.Count; i++)
            {
                homePucksCollected.Remove(temList[i]);
                UpdateScoreText();
            }

            temList.Clear();

            // While the queen is potted-and-waiting-to-be-covered it MUST stay in the
            // hole (hidden, icon shown up top). Without the redPuckWaiting guard this
            // re-activated it mid-cover: sprite turned visible again and the enabled
            // NetworkTransform snapped it right back onto the pocket.
            bool queenPendingCover = isCarronMultiplayerGame && redPuckWaiting;
            if (!redPuckCollected && !queenPendingCover && IsInPuckHoles(redPuck.GetComponent<Puck>()) && redPuck.GetComponent<Puck>().puckInHole)
            {
                redPuck.GetComponent<Puck>().ResetPosition();
                GameController.Instance.leftRedPuckIcon.SetActive(false);
                GameController.Instance.rightRedPuckIcon.SetActive(false);
                Debug.LogError("Red Puck Case  ....  ");
            }
        }

        bool IsInPuckHoles(Puck puck)
        {
            for (int i = 0; i < puckHoles.Length; i++)
            {
                if (puckHoles[i].bounds.Contains(puck.transform.position))
                {
                    return true;
                }
            }
            return false;
        }
        public IEnumerator ResetPuck()
        {
            yield return new WaitForSeconds(3f);
            //Debug.Log("ResetPosition");
            //PlayerPuck.Instance.ResetPosition();
        }
        void CheckGameStatus()
        {
            Debug.Log("Checking Game Status...");
            int flowHomeBefore = homePucksCollected != null ? homePucksCollected.Count : 0;
            int flowAwayBefore = awayPucksCollected != null ? awayPucksCollected.Count : 0;
            if (pucksCollected.Contains(playerPuck.gameObject))
            {
                Debug.Log("CheckPlayerPenalty");
                CheckPlayerPenalty();
            }
            else if (pucksCollected.Contains(redPuck))
            {
                Debug.Log("CheckRedPuckPenalty");
                CheckRedPuckPenalty();
            }
            else
            {
                Debug.Log("CheckPucks");
                CheckPucks();
            }

            OnSliderPointerDown();
            PlayerPuck.Instance.ResetPosition();
            OnSliderPointerUp();

            UpdateScoreText();
            if (MatchFlow.Enabled && homePucksCollected != null && awayPucksCollected != null
                && (homePucksCollected.Count != flowHomeBefore || awayPucksCollected.Count != flowAwayBefore))
                MatchFlow.Log("Carrom", $"score updated: {FlowScores()}" + (redPuckCollected ? " (queen covered)" : ""));

            if (practiceMode)
            {
                CheckTurn();
                return;
            }

            if (redPuckCollected)
            {
                if (homePucksCollected.Count == 9)
                {
                    gameState = myPuck == "White" ? GameState.WIN : GameState.LOSE;
                    if (!isCarronMultiplayerGame)
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                }
                else if (awayPucksCollected.Count == 9)
                {
                    gameState = myPuck == "Black" ? GameState.WIN : GameState.LOSE;
                    if (!isCarronMultiplayerGame)
                        ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
                }

                Debug.LogError("/............................................." + gameState);
            }

            if (gameState == GameState.WIN || gameState == GameState.LOSE)
            {
                if (isCarronMultiplayerGame)
                    if (CarromNetworkManager.instance != null)
                    {
                        // Pass winner ID as string — int.Parse would throw on MongoDB ObjectId player IDs
                        MatchFlow.SendResult(CarromNetworkManager.instance.PlayercurrentTurnId,
                            homePucksCollected.Count == 9 ? $"{FlowOwner("White")} pocketed all white coins + queen"
                            : awayPucksCollected.Count == 9 ? $"{FlowOwner("Black")} pocketed all black coins + queen"
                            : "board cleared", FlowScores());
                        CarromNetworkManager.instance.RpcGameOver(CarromNetworkManager.instance.PlayercurrentTurnId);
                    }
                    else
                        return;
            }

            reduceTimer = true;

            if (gameState == GameState.WAIT)
            {
                Debug.Log("Shoot Again Status: " + shootAgain);
                if (!shootAgain)
                {
                    gameState = GameState.SWITCH_MASTER;
                    StartCoroutine(SwitchMasterDelay());
                }
                else
                {
                    if (isCarronMultiplayerGame)
                    {
                        if (CarromNetworkManager.instance != null)
                        {
                            Debug.Log("🎯 Giving turn again to same player");

                            if (NetworkServer.active)
                            {
                                // Route through the same authoritative server path the client
                                // Cmd uses: TryAssignStrikerAuthority's server-first handoff +
                                // reliable ready-pose Rpc. The old inline block here assigned
                                // authority BEFORE posing and skipped the pose Rpc entirely.
                                CarromNetworkManager.instance.ServerGiveTurnToSamePlayer(
                                    CarromNetworkManager.instance.PlayercurrentTurnId);
                            }
                            else
                            {
                                // ✅ Client side se bhi same player ko turn do.
                                // CmdGiveTurnToSamePlayer (not CmdSwitchTurn) — the string
                                // overload of CmdSwitchTurn only sets masterClientTag and
                                // never restarts the turn timer, which we now stop on shoot.
                                string currentPlayerId = staticVariables.UserProfiledata.user._id.ToString();
                                CarromNetworkManager.instance.CmdGiveTurnToSamePlayer(currentPlayerId);
                            }


                        }

                        // if(NetworkServer.active)
                        // {
                        //     string currentPlayerId = CarromNetworkManager.instance.PlayercurrentTurnId;
                        // CarromNetworkManager.instance.CmdGiveTurnToSamePlayer(currentPlayerId);
                        // CarromNetworkManager.instance.CmdResetShootAgain();
                        // }

                    }
                    // if (isCarronMultiplayerGame)
                    //     if (CarromNetworkManager.instance != null)
                    //     {
                    //         //  CarromNetworkManager.instance.CmdResetOpponentTimer();
                    //     }
                    //     //CarromNetworkManager.instance.CmdResetOpponentTimer();
                    //     else
                    //         StartCoroutine(WiatForGameState());
                    // CarromNetworkManager.instance.RpcResetOpponentTimer();

                    gameState = GameState.READY;
                    if (CurrentTurn == CurrentPlayer.OTHER)
                    {
                        PlayerPuck.Instance.ResetPosition();
                        //  PlayerPuck.Instance.ShootBot();
                        Debug.Log("Shhhhotbot");
                        playerSlider.interactable = false;
                    }
                    else
                    {
                        if (!queenCheck)
                        {
                            SetTurnStatusMessage(!redPuckWaiting ? "Go On!" : "Cover The Queen!", showWhenHidden: true);
                        }
                        else if (!statusScreen.activeInHierarchy)
                        {
                            StatusPanelActive();
                        }
                        playerSlider.interactable = true;
                    }
                }
            }
            else
            {
                gameState = GameState.READY;
            }
        }



        public IEnumerator SwitchMasterDelay()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            if (!NetworkServer.active)
                Event_SwitchingMasterClient?.Invoke();
            yield return new WaitForSecondsRealtime(0.2f);

            if (GameManager.Instance.currentGameMode == GameManager.GameMode.Ai)
            {
                if (CurrentTurn == CurrentPlayer.ME)
                {
                    CurrentTurn = CurrentPlayer.OTHER;
                }
                else
                {
                    CurrentTurn = CurrentPlayer.ME;
                }

                if (masterClientTag == "White")
                {
                    masterClientTag = "Black";
                }
                else
                {
                    masterClientTag = "White";
                }

                CheckTurn();
            }
            else
            {
                // Mirror doesn't have SetMasterClient equivalent
                // You'll need to implement your own host migration system
                // For now, just switch turns
                // if (MirrorNetwork.Instance.isMasterClient)
                // {
                //     CarromNetworkManager.instance.CmdSwitchTurn();
                // }
                if (CarromNetworkManager.instance != null)
                {
                    Debug.Log("🔄 Calling CarromNetworkManager.CmdSwitchTurn()");
                    if (NetworkServer.active)
                    {
                        // Route through the authoritative path (server-first handoff,
                        // ready-pose Rpc, server-side masterClientTag, stale-guard vs
                        // the shooter-client's racing Cmd). The old inline block here
                        // assigned authority BEFORE posing, skipped the pose Rpc, never
                        // updated the server's own masterClientTag, and could double-
                        // switch if the client's CmdSwitchTurn landed first.
                        CarromNetworkManager.instance.ServerSwitchTurnAfterShot();
                    }
                    else
                    {
                        CarromNetworkManager.instance.CmdSwitchTurn(staticVariables.UserProfiledata.user._id);

                    }
                }



            }
        }



        void CheckPlayerPenalty()
        {
            Debug.Log("🔍 [PenaltyCheck] Started checking player penalty...");
            MatchFlow.Log("Carrom", $"foul — {FlowShooter()} pocketed the striker");

            // --- RED PUCK SECTION ---
            if (pucksCollected.Contains(redPuck))
            {
                Debug.Log("⚠️ [PenaltyCheck] Red puck found in pucksCollected. Resetting...");
                MatchFlow.Log("Carrom", "queen pocketed with the striker — returned to the centre");
                pucksCollected.Remove(redPuck);
                redPuck.GetComponent<Puck>().ResetPosition();
                redPuck.GetComponent<Puck>().BroadCastResetPosition();
            }

            if (redPuckWaiting)
            {
                Debug.Log("🕐 [PenaltyCheck] Red puck was waiting. Resetting its position...");
                MatchFlow.Log("Carrom", "queen not covered (foul) — returned to the centre");
                redPuckWaiting = false;
                redPuck.GetComponent<Puck>().BroadCastResetPosition();
            }

            // --- MAIN LOOP THROUGH COLLECTED PUCKS ---
            for (int i = 0; i < pucksCollected.Count; i++)
            {
                GameObject targetPuck = pucksCollected[i];
                Debug.Log($"🎯 [PenaltyCheck] Checking puck: {targetPuck.name} | Tag: {targetPuck.tag}");

                if (targetPuck.CompareTag("Player"))
                {
                    Debug.Log("➡️ [PenaltyCheck] Skipping 'Player' puck.");
                    CarromNetworkManager.instance.RpcResetStrikerPosition();
                    continue;
                }

                if (targetPuck.CompareTag(masterClientTag))
                {
                    Debug.Log($"🚫 [PenaltyCheck] Target puck ({targetPuck.name}) matches masterClientTag ({masterClientTag}). Resetting and removing...");
                    MatchFlow.Log("Carrom", $"coin returned — {FlowCoin(targetPuck.tag)} pocketed on the foul goes back to the board");
                    targetPuck.GetComponent<Puck>().ResetAndRemove();
                }
                else
                {
                    if (masterClientTag == "White")
                    {
                        if (!awayPucksCollected.Contains(targetPuck))
                        {
                            Debug.Log($"📥 [PenaltyCheck] Adding {targetPuck.name} to awayPucksCollected.");
                            awayPucksCollected.Add(targetPuck);
                        }
                        else
                        {
                            Debug.Log($"⚪ [PenaltyCheck] {targetPuck.name} already in awayPucksCollected, skipping.");
                        }
                    }
                    else
                    {
                        if (!homePucksCollected.Contains(targetPuck))
                        {
                            Debug.Log($"📥 [PenaltyCheck] Adding {targetPuck.name} to homePucksCollected.");
                            homePucksCollected.Add(targetPuck);
                        }
                        else
                        {
                            Debug.Log($"⚫ [PenaltyCheck] {targetPuck.name} already in homePucksCollected, skipping.");
                        }
                    }
                }
            }

            Debug.Log($"🧹 [PenaltyCheck] Clearing pucksCollected. Count before clear: {pucksCollected.Count}");
            pucksCollected.Clear();

            // --- HOME/AWAY PUCK REMOVAL LOGIC ---
            if (masterClientTag == "White")
            {
                Debug.Log($"⚪ [PenaltyCheck] Master is White. HomePucksCollected count: {homePucksCollected.Count}");
                if (homePucksCollected.Count > 0)
                {
                    GameObject targetPuck = homePucksCollected[0];
                    Debug.Log($"🗑️ [PenaltyCheck] Removing first home puck: {targetPuck.name}");
                    MatchFlow.Log("Carrom", $"penalty: one of {FlowOwner("White")}'s white coins returned to the board");
                    targetPuck.GetComponent<Puck>().ResetAndRemove();
                }
            }
            else
            {
                Debug.Log($"⚫ [PenaltyCheck] Master is Black. AwayPucksCollected count: {awayPucksCollected.Count}");
                if (awayPucksCollected.Count > 0)
                {
                    GameObject targetPuck = awayPucksCollected[0];
                    Debug.Log($"🗑️ [PenaltyCheck] Removing first away puck: {targetPuck.name}");
                    MatchFlow.Log("Carrom", $"penalty: one of {FlowOwner("Black")}'s black coins returned to the board");
                    targetPuck.GetComponent<Puck>().ResetAndRemove();
                }
            }

            // --- 9 PUCK PENALTY CHECK ---
            if (homePucksCollected.Count.Equals(9) && !redPuckCollected)
            {
                Debug.Log("⚪ [PenaltyCheck] Home player reached 9 pucks without red puck! Resetting last puck...");
                MatchFlow.Log("Carrom", "last white coin before the queen — returned to the board");
                homePucksCollected[8].GetComponent<Puck>().ResetAndRemove();
            }

            if (awayPucksCollected.Count.Equals(9) && !redPuckCollected)
            {
                Debug.Log("⚫ [PenaltyCheck] Away player reached 9 pucks without red puck! Resetting last puck...");
                MatchFlow.Log("Carrom", "last black coin before the queen — returned to the board");
                awayPucksCollected[8].GetComponent<Puck>().ResetAndRemove();
            }

            Debug.Log("✅ [PenaltyCheck] Completed penalty check successfully.");
        }
        public void MasterClientWin()
        {
            if (GameManager.Instance.masterClient)
            {
                gameState = GameState.WIN;
            }
            else
            {
                gameState = GameState.LOSE;
            }
            CarromNetworkManager.instance.CmdGameOver();
            Debug.Log("MasterClientWin Player Wins!");
        }

        public void OtherPlayerWin()
        {
            if (!GameManager.Instance.masterClient)
            {

                gameState = GameState.WIN;
            }
            else
            {
                gameState = GameState.LOSE;
            }
            CarromNetworkManager.instance.CmdGameOver();
            Debug.Log("Other Player Wins!");
        }

        void CheckRedPuckPenalty()
        {
            bool isValid = true;
            Debug.Log("🔴 [RedPuckPenalty] --- Checking red puck penalty ---");
            Debug.Log($"[RedPuckPenalty] Starting isValid = {isValid}");
            Debug.Log($"[RedPuckPenalty] masterClientTag = {masterClientTag}");
            Debug.Log($"[RedPuckPenalty] pucksCollected count = {pucksCollected.Count}");

            // --- MAIN VALIDATION LOOP ---
            for (int i = 0; i < pucksCollected.Count; i++)
            {
                GameObject targetPuck = pucksCollected[i];
                Debug.Log($"🎯 [RedPuckPenalty] Checking puck: {targetPuck.name} | Tag: {targetPuck.tag}");

                if (targetPuck.CompareTag("Player") || targetPuck.CompareTag("Red"))
                {
                    Debug.Log($"⚠️ [RedPuckPenalty] Skipping {targetPuck.name} (Tag: {targetPuck.tag}) for special red/player handling.");

                    if (masterClientTag == "White")
                    {
                        if (targetPuck.CompareTag("Red") && homePucksCollected.Count < 8)
                        {
                            Debug.Log($"❌ [RedPuckPenalty] Red puck hit before 8 white pucks! homePucksCollected = {homePucksCollected.Count}");
                            isValid = false;
                        }
                    }
                    else
                    {
                        if (targetPuck.CompareTag("Red") && awayPucksCollected.Count < 8)
                        {
                            Debug.Log($"❌ [RedPuckPenalty] Red puck hit before 8 black pucks! awayPucksCollected = {awayPucksCollected.Count}");
                            isValid = false;
                        }
                    }

                    continue;
                }

                if (!targetPuck.CompareTag(masterClientTag))
                {
                    Debug.Log($"❌ [RedPuckPenalty] Invalid puck detected! {targetPuck.name} tag = {targetPuck.tag}, masterClientTag = {masterClientTag}");
                    isValid = false;
                }
                else
                {
                    Debug.Log($"✅ [RedPuckPenalty] Valid puck collected: {targetPuck.name} ({targetPuck.tag})");
                }
            }

            // --- DECISION RESULT ---
            Debug.Log($"[RedPuckPenalty] Final validation result: isValid = {isValid}");

            if (isValid)
            {
                Debug.Log("🟢 [RedPuckPenalty] Red puck collection is valid. Player will shoot again.");
                MatchFlow.Log("Carrom", $"queen pocketed by {FlowShooter()} — needs cover");
                redPuckWaiting = true;
                // Remember WHO owes the cover, so CheckTurn only preserves the pending
                // state while that same player keeps the turn.
                queenPendingTag = masterClientTag;
                shootAgain = true;
                if (pucksCollected.Contains(redPuck))
                {
                    Debug.Log("🔴 [RedPuckPenalty] Removing red puck from pucksCollected.");
                    pucksCollected.Remove(redPuck);
                }
            }
            else
            {
                Debug.Log("🔴 [RedPuckPenalty] Invalid red puck collection. Resetting red puck and removing invalid pucks...");
                MatchFlow.Log("Carrom", $"foul — queen pocketed by {FlowShooter()} not allowed yet, returned to the centre");
                redPuck.GetComponent<Puck>().ResetPosition();
                CarromNetworkManager.instance.RpcResetRedPunk();
                List<GameObject> tempList = new List<GameObject>();

                for (int i = 0; i < pucksCollected.Count; i++)
                {
                    GameObject targetPuck = pucksCollected[i];
                    if (targetPuck.CompareTag("Player") || !targetPuck.CompareTag(masterClientTag)) continue;

                    Debug.Log($"🗑️ [RedPuckPenalty] Removing {targetPuck.name} (Tag: {targetPuck.tag}) due to invalid red puck penalty.");
                    MatchFlow.Log("Carrom", $"coin returned — {FlowCoin(targetPuck.tag)} from the queen foul goes back to the board");
                    targetPuck.GetComponent<Puck>().ResetAndRemove();
                    tempList.Add(targetPuck);
                }

                for (int i = 0; i < tempList.Count; i++)
                {
                    Debug.Log($"🧹 [RedPuckPenalty] Cleaning up removed puck: {tempList[i].name}");
                    pucksCollected.Remove(tempList[i]);
                }
                tempList.Clear();
            }

            // --- ADD VALID PUCKS TO HOME/AWAY LISTS ---
            for (int i = 0; i < pucksCollected.Count; i++)
            {
                var targetPuck = pucksCollected[i];
                Debug.Log($"🏁 [RedPuckPenalty] Adding valid puck: {targetPuck.name} | Tag: {targetPuck.tag}");

                if (targetPuck.CompareTag("White"))
                {
                    if (!homePucksCollected.Contains(targetPuck))
                    {
                        homePucksCollected.Add(targetPuck);
                        Debug.Log($"⚪ [RedPuckPenalty] Added {targetPuck.name} to homePucksCollected.");
                    }
                }
                else if (targetPuck.CompareTag("Black"))
                {
                    if (!awayPucksCollected.Contains(targetPuck))
                    {
                        awayPucksCollected.Add(targetPuck);
                        Debug.Log($"⚫ [RedPuckPenalty] Added {targetPuck.name} to awayPucksCollected.");
                    }
                }
            }

            Debug.Log($"🧹 [RedPuckPenalty] Clearing pucksCollected. Count before clear: {pucksCollected.Count}");
            pucksCollected.Clear();

            // --- PENALTY RULE CHECK FOR 9 PUCKS ---
            if (homePucksCollected.Count.Equals(9))
            {
                Debug.Log("⚪ [RedPuckPenalty] Home player reached 9 pucks! Resetting last puck...");
                MatchFlow.Log("Carrom", "last white coin before the queen is covered — returned to the board");
                homePucksCollected[8].GetComponent<Puck>().ResetAndRemove();
            }
            if (awayPucksCollected.Count.Equals(9))
            {
                Debug.Log("⚫ [RedPuckPenalty] Away player reached 9 pucks! Resetting last puck...");
                MatchFlow.Log("Carrom", "last black coin before the queen is covered — returned to the board");
                awayPucksCollected[8].GetComponent<Puck>().ResetAndRemove();
            }

            // --- SUMMARY ---
            // Debug.Log($"📊 [RedPuckPenalty] Summary:");
            // Debug.Log($"  - homePucksCollected: {homePucksCollected.Count}");
            // Debug.Log($"  - awayPucksCollected: {awayPucksCollected.Count}");
            // Debug.Log($"  - redPuckWaiting: {redPuckWaiting}");
            // Debug.Log($"  - shootAgain: {shootAgain}");
            // Debug.Log("✅ [RedPuckPenalty] --- Red puck penalty check completed ---");
        }

        void ChangePuckMessage()
        {
            toastMessageText.text = "Pot queen and cover it to win the match";
        }

        bool IsContainMyPuck(string puckType)
        {
            bool isContainMyPuck = false;

            for (int i = 0; i < pucksCollected.Count; i++)
            {
                if (pucksCollected[i].CompareTag(puckType))
                {
                    isContainMyPuck = true;
                    break;
                }
            }
            return isContainMyPuck;
        }

        void CheckPucks()
        {
            bool tempRedPuckWaiting = redPuckWaiting;
            bool isContainMyPuck = this.IsContainMyPuck(masterClientTag);

            Debug.Log("🟡 CheckPucks() Called | redPuckWaiting: " + redPuckWaiting + " | masterClientTag: " + masterClientTag);
            Debug.Log("🟢 Contains My Color Puck: " + isContainMyPuck + " | pucksCollected Count: " + pucksCollected.Count);

            if (pucksCollected.Count == 0)
            {
                Debug.Log("⚪ No pucks collected this turn.");
                MatchFlow.Log("Carrom", $"{FlowShooter()} pocketed nothing");

                if (redPuckWaiting)
                {
                    Debug.Log("❌ Queen not covered! Resetting queen to board center...");
                    MatchFlow.Log("Carrom", $"queen not covered by {FlowShooter()} — returned to the centre");
                    redPuckWaiting = false;
                    redPuck.GetComponent<Puck>().ResetPosition();
                    redPuck.GetComponent<Puck>().BroadCastResetPosition();

                    // ⚠️ Penalty: Remove one puck of the current player
                    if (masterClientTag == "White" && homePucksCollected.Count > 0)
                    {
                        var lastPuck = homePucksCollected[homePucksCollected.Count - 1];
                        homePucksCollected.Remove(lastPuck);
                        lastPuck.GetComponent<Puck>().ResetAndRemove();
                        Debug.Log("🚨 Penalty: White loses one puck (returned to board).");
                        MatchFlow.Log("Carrom", $"penalty: one of {FlowOwner("White")}'s white coins returned to the board");
                    }
                    else if (masterClientTag == "Black" && awayPucksCollected.Count > 0)
                    {
                        var lastPuck = awayPucksCollected[awayPucksCollected.Count - 1];
                        awayPucksCollected.Remove(lastPuck);
                        lastPuck.GetComponent<Puck>().ResetAndRemove();
                        Debug.Log("🚨 Penalty: Black loses one puck (returned to board).");
                        MatchFlow.Log("Carrom", $"penalty: one of {FlowOwner("Black")}'s black coins returned to the board");
                    }
                }

                return;
            }

            // 🔴 Handling queen (red puck)
            if (tempRedPuckWaiting)
            {
                if (isContainMyPuck)
                {
                    // Player covered the queen successfully
                    redPuckCollected = true;
                    queenCheck = true;
                    tempRedPuckWaiting = false;
                    statusPanelText.text = "✅ Queen Covered!";
                    Debug.Log("✅ Queen Covered Successfully!");
                    MatchFlow.Log("Carrom", $"queen covered by {FlowShooter()}");
                }
                else
                {
                    // Player failed to cover queen → penalty
                    tempRedPuckWaiting = false;
                    queenCheck = false;

                    Debug.Log("❌ Queen not covered! Resetting queen + applying penalty...");
                    MatchFlow.Log("Carrom", $"queen not covered by {FlowShooter()} — returned to the centre");

                    // Reset the queen
                    redPuck.GetComponent<Puck>().ResetPosition();
                    redPuck.GetComponent<Puck>().BroadCastResetPosition();

                    // Apply penalty (deduct one puck)
                    if (masterClientTag == "White" && homePucksCollected.Count > 0)
                    {
                        var lastPuck = homePucksCollected[homePucksCollected.Count - 1];
                        homePucksCollected.RemoveAt(homePucksCollected.Count - 1);

                        lastPuck.GetComponent<Puck>().ResetAndRemove();
                        // lastPuck.GetComponent<Puck>().BroadCastResetPosition();
                        Debug.Log("⚪ Penalty applied: White player puck returned to board.");
                        MatchFlow.Log("Carrom", $"penalty: one of {FlowOwner("White")}'s white coins returned to the board");
                    }
                    else if (masterClientTag == "Black" && awayPucksCollected.Count > 0)
                    {
                        var lastPuck = awayPucksCollected[awayPucksCollected.Count - 1];
                        awayPucksCollected.RemoveAt(awayPucksCollected.Count - 1);

                        lastPuck.GetComponent<Puck>().ResetAndRemove();
                        // lastPuck.GetComponent<Puck>().BroadCastResetPosition();
                        Debug.Log("⚫ Penalty applied: Black player puck returned to board.");
                        MatchFlow.Log("Carrom", $"penalty: one of {FlowOwner("Black")}'s black coins returned to the board");
                    }
                    else
                    {
                        Debug.LogWarning("⚠ No puck available to deduct for penalty!");
                    }
                }
            }

            // 🟠 Process all collected pucks
            for (int i = 0; i < pucksCollected.Count; i++)
            {
                Debug.Log("pucksCollected" + pucksCollected[i]);
                var targetPuck = pucksCollected[i];
                Debug.Log($"🎯 Processing Puck {i}: {targetPuck.name} (Tag: {targetPuck.tag})");

                if (targetPuck.CompareTag(masterClientTag))
                {
                    shootAgain = true;
                    Debug.Log("🔁 Same color puck pocketed, player gets another turn!");
                }

                // ♟ White Player Section
                if (masterClientTag == "White")
                {
                    if (targetPuck.CompareTag("White"))
                    {
                        if (homePucksCollected.Count == 8 && !redPuckCollected)
                        {
                            targetPuck.GetComponent<Puck>().ResetAndRemove();
                            shootAgain = false;
                            if (CurrentTurn.Equals(GameController.CurrentPlayer.ME))
                            {
                                statusPanelText.text = "⚠️ Foul: Pot Queen First!";
                                StatusPanelActive();
                            }
                            Debug.Log("🚫 White potted all before queen covered → foul");
                            MatchFlow.Log("Carrom", "foul — last white coin before the queen is covered, returned to the board");
                        }
                        else
                        {
                            if (!homePucksCollected.Contains(targetPuck))
                                homePucksCollected.Add(targetPuck);
                        }
                    }
                    else
                    {
                        if (awayPucksCollected.Count == 8 && !redPuckCollected)
                        {
                            targetPuck.GetComponent<Puck>().ResetAndRemove();
                            shootAgain = false;
                            // if (CurrentTurn.Equals(GameController.CurrentPlayer.ME))
                            // {
                            //     statusPanelText.text = "⚠️ Foul: Pot Queen First!";
                            //     StatusPanelActive();
                            // }
                            // Debug.Log("🚫 White potted all before queen covered → foul");
                            MatchFlow.Log("Carrom", "opponent's last black coin pocketed before the queen is covered — returned to the board");
                        }
                        else
                        {
                            // opponent's puck handling
                            if (!awayPucksCollected.Contains(targetPuck))
                                awayPucksCollected.Add(targetPuck);
                        }
                    }
                }

                // ♟ Black Player Section
                else if (masterClientTag == "Black")
                {
                    if (targetPuck.CompareTag("Black"))
                    {
                        if (awayPucksCollected.Count == 8 && !redPuckCollected)
                        {
                            targetPuck.GetComponent<Puck>().ResetAndRemove();
                            shootAgain = false;
                            if (CurrentTurn.Equals(CurrentPlayer.ME))
                            {
                                statusPanelText.text = "⚠️ Foul: Pot Queen First!";
                                StatusPanelActive();
                            }
                            Debug.Log("🚫 Black potted all before queen covered → foul");
                            MatchFlow.Log("Carrom", "foul — last black coin before the queen is covered, returned to the board");
                        }
                        else
                        {
                            if (!awayPucksCollected.Contains(targetPuck))
                                awayPucksCollected.Add(targetPuck);
                        }
                    }
                    else
                    {
                        if (homePucksCollected.Count == 8 && !redPuckCollected)
                        {
                            targetPuck.GetComponent<Puck>().ResetAndRemove();
                            shootAgain = false;
                            // if (CurrentTurn.Equals(CurrentPlayer.ME))
                            // {
                            //     statusPanelText.text = "⚠️ Foul: Pot Queen First!";
                            //     StatusPanelActive();
                            // }
                            // Debug.Log("🚫 Black potted all before queen covered → foul");
                            MatchFlow.Log("Carrom", "opponent's last white coin pocketed before the queen is covered — returned to the board");
                        }
                        else
                        {
                            // opponent's puck handling
                            if (!homePucksCollected.Contains(targetPuck))
                                homePucksCollected.Add(targetPuck);
                        }
                    }
                }
            }

            pucksCollected.Clear();
            redPuckWaiting = tempRedPuckWaiting;

            Debug.Log("✅ CheckPucks() Completed | redPuckWaiting: " + redPuckWaiting);
        }

        public void UpdateScoreText()
        {
            "pucksCollected Count: ".Show(pucksCollected.Count);
            homePucksCollected.Contains(redPuck).Show(" homePucksCollected Contains Red Puck: ");
            if (homePucksCollected.Contains(redPuck))
            {
                currentHomeScore = homePucksCollected.Count - 1;
                topHomeScoreText.text = currentHomeScore.ToString();
            }
            else
            {
                currentHomeScore = homePucksCollected.Count;
                topHomeScoreText.text = homePucksCollected.Count.ToString();
            }

            awayPucksCollected.Contains(redPuck).Show(" awayPucksCollected Contains Red Puck: ");
            if (awayPucksCollected.Contains(redPuck))
            {
                currentAwayScore = awayPucksCollected.Count - 1;
                topAwayScoreText.text = currentAwayScore.ToString();
            }
            else
            {
                currentAwayScore = awayPucksCollected.Count;
                topAwayScoreText.text = currentAwayScore.ToString();
            }

            if (GameManager.Instance.masterClient || GameManager.Instance.currentGameMode == GameManager.GameMode.Ai)
            {
                if (homePucksCollected.Count >= 8 && !redPuckCollected)
                {
                    ChangePuckMessage();
                }
            }
            else
            {
                if (awayPucksCollected.Count >= 8 && !redPuckCollected)
                {
                    ChangePuckMessage();
                }
            }
        }

        public void ScoreText()
        {
            topHomeScoreText.text = BEKStudio.GameController.currentHomeScore.ToString();
            topAwayScoreText.text = BEKStudio.GameController.currentAwayScore.ToString();
            topHomeScoreText.text.Show(" Home Score Text: ");
            topAwayScoreText.text.Show(" Away Score Text: ");

        }
        public void CheckState(bool isWhiteColledQueen)
        {
            if (!isCarronMultiplayerGame)
            {
                if (homePucksCollected.Count == 9 && awayPucksCollected.Count == 9)
                {
                    gameState = isWhiteColledQueen ? GameState.WIN : GameState.LOSE;
                    Debug.Log("Worst Case .." + gameState);
                }
                return;
            }

            Debug.Log("Home Pucks .." + homePucksCollected.Count + " awayPucksCollected .." + awayPucksCollected.Count);
            Debug.Log("isWhiteColledQueen .." + isWhiteColledQueen);

            if (GameManager.Instance.masterClient)
            {
                if (homePucksCollected.Count == 9 && awayPucksCollected.Count == 9)
                {
                    gameState = isWhiteColledQueen ? GameState.WIN : GameState.LOSE;
                    Debug.Log("Worst Case .." + gameState);
                }
                else if (homePucksCollected.Count == 9)
                {
                    gameState = GameState.WIN;
                }
                else
                {
                    gameState = GameState.LOSE;
                }
            }
            else
            {
                if (homePucksCollected.Count == 9 && awayPucksCollected.Count == 9)
                {
                    gameState = isWhiteColledQueen ? GameState.LOSE : GameState.WIN;
                    Debug.Log("Worst Case .." + gameState);
                }
                else if (awayPucksCollected.Count == 9)
                {
                    gameState = GameState.WIN;
                }
                else
                {
                    gameState = GameState.LOSE;
                }
            }
        }

        public void CheckRoomPlayers()
        {
            if (NetworkServer.connections.Count == 1)
            {
                DisplayPopUp(true, $"Opponent Left!");
                PopupMessageManager.instance.SetPanelStaus(false, body: "Wait for other player");
                gameState = GameState.WIN;
                if (CarromNetworkManager.instance != null)
                {
                    // CarromNetworkManager.instance.RpcGameOver();
                }
            }
        }



        bool finishGame;

        //private void OnEnable()
        //{
        //    // MirrorNetwork.OnWinCall += AnnounceWinner;
        //    // Add your event subscriptions here
        //}

        //private void OnDisable()
        //{
        //    // MirrorNetwork.OnWinCall -= AnnounceWinner;
        //    // Remove your event subscriptions here
        //}

        public void WaitingForOpponentt(bool state)
        {
            if (state == true)
            {
                if (finishGame == false)
                    PopupMessageManager.instance.SetPanelStaus(state, body: "Waiting for opponent");
            }
            else
            {
                PopupMessageManager.instance.SetPanelStaus(state, body: "Waiting for opponent");
            }
        }

        public void AnnounceWinner(string reason)
        {
            // if (masterClientTag == "White")
            // {
            //     homePucksCollected = new List<GameObject>(new GameObject[9]);
            // }
            // else
            // {
            //     awayPucksCollected = new List<GameObject>(new GameObject[9]);
            // }
            gameState = GameState.WIN;
            gameOverPanel.SetActive(true);
        }

        public void Announcelosser()
        {
            // Implement loser logic
        }

        public IEnumerator WiatForGameState()
        {
            Debug.LogError("Game State.... 1" + gameState);

            yield return new WaitUntil(() => (gameState != GameState.WAIT));
            settingsScreen.SetActive(false);
            gameOverPanel.SetActive(true);
        }

        public void IncScoreOnCollected(bool isHome)
        {
            if (isHome)
            {
                currentHomeScore++;
                topHomeScoreText.text = currentHomeScore.ToString();
            }
            else
            {
                currentAwayScore++;
                topAwayScoreText.text = currentAwayScore.ToString();
            }
        }

        public void DisplayPopUp(bool state, string error = "")
        {
            if (!state)
            {
                DisplayMessage.SetActive(state);
                return;
            }
            DisplayMessageText.text = error;
            DisplayMessage.SetActive(state);
        }


    }
}
