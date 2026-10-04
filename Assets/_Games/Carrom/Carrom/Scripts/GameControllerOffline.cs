
using System.Collections;
using System.Collections.Generic;
using BEKStudio;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System;
using UnityEngine.Serialization;

namespace BEKStudio
{
    public class GameControllerOffline : MonoBehaviour
    {
        public static GameControllerOffline Instance;
        public enum GameState { READY, WAIT, SWITCH_MASTER, WIN, LOSE };
        public GameState gameState;
        public enum WhichPlayer { ME, OTHER };
        public WhichPlayer whichPlayer;
        public Rigidbody2D playerPuck;
        // public PhotonView playerPhotonView;
        public List<GameObject> homePucksCollected;
        public List<GameObject> awayPucksCollected;
        public List<GameObject> pucksCollected;
        //public Sprite[] avatars;
        [Header("Top")]
        public TextMeshProUGUI topTotalBetText;
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
        public PuckOffline[] allPucks;
        public float playerPuckMinX;
        public float playerPuckMaxX;
        [Header("Others")]
        public bool isPucksFixed;
        public bool shootAgain;
        string targetTag;
        public string masterClientTag;
        public float timer;
        public bool reduceTimer;
        public GameObject redPuck;
        public GameObject statusScreen;
        public Image statusBackground;
        public RectTransform statusPanel;
        public TextMeshProUGUI statusPanelText;
        public bool redPuckCollected;
        public bool practiceMode;
        public bool redPuckWaiting;

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

        private int currentHomeScore;
        private int currentAwayScore;
        public bool isSliding;

        public CircleCollider2D[] puckHoles;

        public StrikerPositionAdjuster strikerPositionAdjuster;
        public PlayerPuckOffline playerPuckScript;
        public GameObject waitingOpponentPanel;
        public bool isCarronMultiplayerGame;

        public GameObject mainBoard;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //PhotonController.Instance.gameOver = false;
            }
            //   GameManager.Instance.masterClient = true;


        }


        void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            // if (!PhotonNetwork.IsMasterClient)
            // {
            //     //layerPuck.GetComponent<PlayerPuck>().playerCollider.enabled = false; 
            //     whichPlayer = WhichPlayer.OTHER;
            // }
            isCarronMultiplayerGame = false;

            if (!GameManager.Instance.isOnline())
            {
                //playerPuck.GetComponent<PlayerPuck>().playerCollider.enabled = true;
                whichPlayer = WhichPlayer.ME;

            }

            masterClientTag = "White";
            CheckTurn();

            homePucksCollected = new List<GameObject>();
            awayPucksCollected = new List<GameObject>();
            pucksCollected = new List<GameObject>();

            targetTag = "White";
            //GetComponent<PhotonView>().


            redPuck.SetActive(true);

            // topTotalBetText.text = (PhotonController.Instance.roomEntryPice * 2).ToString("###,###,###"); // Carromintegration

            if (GameManager.Instance.currentGameMode == GameManager.GameMode.Ai)
            {
                targetTag = "White";
                SetTopUsersForBot();
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

            //  PhotonController.Instance.isOtherPlayerLeft = false;// Carromintegration



        }

        void SetTopUsersForBot()
        {
            topHomeNameText.text = staticVariables.UserProfiledata.user.first_name;
            topHomeAvatar.texture = staticVariables.ProfilePicture;
            topAwayNameText.text = GameManager.Instance.GetBotName();
            topAwayAvatar.texture = scriptable.aiImg;
        }



        void StartMessage()
        {

            //Here debug Tom
            if (targetTag == "White")
            {
                statusPanelText.text = "YOU START";
                toastMessageText.text = "Pot all white pucks and cover the queen to win the match";
            }
            else /*if (targetTag == "Black")*/
            {
                statusPanelText.text = "OPPONENT START";

                toastMessageText.text = "Pot all black pucks and cover the queen to win the match";

            }

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

                            });


                });
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


            if (practiceMode) return;

            RunTimer();
        }

        void FixedUpdate()
        {
            if (gameState == GameState.READY || gameState == GameState.WAIT)
            {
                isPucksFixed = GetPuckStatus();
            }
        }

        void RunTimer()
        {
            Debug.Log("Timer");
            if (gameState != GameState.READY) return;

            if (reduceTimer)
            {
                timer -= 1 * Time.deltaTime;
            }

            if (!GameManager.Instance.isOnline())
            {
                if (masterClientTag == "White")
                {
                    topHomeAvatarTimer.fillAmount = Mathf.InverseLerp(0, Constants.PLAY_TIME_FOR_PLAYER, timer);
                }
                else
                {
                    topAwayAvatarTimer.fillAmount = Mathf.InverseLerp(0, Constants.PLAY_TIME_FOR_PLAYER, timer);
                }
            }

            if (timer <= 0)
            {
                gameState = GameState.WAIT;
                PlayerPuckOffline.Instance.isTouch = false;
                PlayerPuckOffline.Instance.arrow.SetActive(false);
                PlayerPuckOffline.Instance.tutorial.SetActive(false);
                PlayerPuckOffline.Instance.tutorialShowDelay = 2f;
                shootAgain = false;

                StartCoroutine(WaitForPucks());
            }
        }

        public void SettingsBtn()
        {
            Debug.Log("SettingsBtn");
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
                .setOnComplete(() =>
                {

                });

            if (!GameManager.Instance.isOnline())
            {
                Time.timeScale = 0;
            }

        }

        public void SettingsContinueBtn()
        {
            Debug.Log("SettingsContinueBtn");
            if (!GameManager.Instance.isOnline())
            {
                Time.timeScale = 1;
            }
            settingsScreen.SetActive(false);
        }

        public void SettingsMenuBtn()
        {
            Debug.Log("SettingsMenuBtn");
            if (!GameManager.Instance.isOnline())
            {
                Time.timeScale = 1;
            }
            settingsContinue.GetComponent<Button>().interactable = false;
            settingsMenu.GetComponent<Button>().interactable = false;

            //PhotonNetwork.AutomaticallySyncScene = false;
            //AdsManager.Instance.ShowInterstitialAd();
            if (!GameManager.Instance.isOnline())
            {
                SceneManager.LoadScene("Home");
            }
        }

        public void Shoot()
        {
            Debug.Log("Shoot");
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
            Debug.Log("NavigatorSliderValueChanged");
            if (gameState != GameState.READY) return;
            playerPuck.transform.localPosition = new Vector2(Mathf.Lerp(playerPuckMinX, playerPuckMaxX, playerSlider.value), playerPuck.transform.localPosition.y);
        }

        public void OnSliderPointerDown()
        {
            Debug.Log("OnSliderPointerDown");
            if (!playerSlider.interactable) return;
            OnDown();
        }

        public void OnDown()
        {
            Debug.Log("OnDown");

            HandleOnSliderPointerDown();

        }

        public void HandleOnSliderPointerDown()
        {
            Debug.Log("HandleOnSliderPointerDown");
            isSliding = true;
            playerPuck.GetComponent<Rigidbody2D>().isKinematic = true;
            foreach (var puck in allPucks)
            {
                puck.GetComponent<Rigidbody2D>().isKinematic = true;
            }
        }

        public void OnSliderPointerUp()
        {
            Debug.Log("OnSliderPointerUp");
            if (!playerSlider.interactable) return;
            OnUp();
        }

        public void OnUp()
        {
            Debug.Log("OnUp");
            HandleOnSliderPointerUp();

        }

        public void HandleOnSliderPointerUp()
        {
            Debug.Log("HandleOnSliderPointerUp");
            isSliding = false;
            StartCoroutine(HandleSliderPointerUp());

        }

        IEnumerator HandleSliderPointerUp()
        {
            Debug.Log("HandleSliderPointerUp");
            isSliding = false;


            strikerPositionAdjuster.SetPosition();
            strikerPositionAdjuster.SetSliderPosition(playerSlider);


            yield return new WaitForSeconds(0.8f);
            if (isSliding) yield break;
            //strikerPositionAdjuster.SetPosition();

            Rigidbody2D playerRb = playerPuck.GetComponent<Rigidbody2D>();
            playerRb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            playerRb.isKinematic = false;
            foreach (var puck in allPucks)
            {
                puck.GetComponent<Rigidbody2D>().isKinematic = false;
            }
            //yield return new WaitForSeconds(0.1f);
            playerRb.constraints = RigidbodyConstraints2D.None;
        }




        public void SendData()
        {
            Debug.Log("SendData");
            List<bool> puckInHole = new List<bool>();

            for (int i = 0; i < allPucks.Length; i++)
            {
                puckInHole.Add(allPucks[i].puckInHole);

            }

            //   playerPuck.GetComponent<PlayerPuckOffline>().SendData(puckInHole.ToArray(), GameManager.Instance.masterClient);

            //SyncData(puckInHole.ToArray());
        }
        public void SyncData(bool[] dataArray, bool IsOrigMaster)
        {
            Debug.Log("SyncData");
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
            Debug.Log("GetPuckStatus");
            for (int i = 0; i < allPucks.Length; i++)
            {
                if (allPucks[i].isMoving)
                {
                    reduceTimer = false;
                    return false;
                }
            }

            if (PlayerPuckOffline.Instance.isMoving)
            {
                return false;
            }



            return true;
        }

        public void CheckTurn()
        {
            Debug.Log("CheckTurn");
            redPuckWaiting = false;
            if (!redPuckCollected)
            {
                leftRedPuckIcon.SetActive(false);
                rightRedPuckIcon.SetActive(false);

            }
            reduceTimer = true;
            timer = Constants.PLAY_TIME_FOR_PLAYER;
            topHomeAvatarTimer.fillAmount = 0;
            topAwayAvatarTimer.fillAmount = 0;
            Event_MasterClientSwithced?.Invoke();
            pucksCollected.Clear();

            if (whichPlayer == WhichPlayer.ME)
            {
                PlayerPuckOffline.Instance.ResetPosition();
                if (!statusScreen.activeInHierarchy)
                {
                    statusPanelText.text = "Your Turn!";
                    StatusPanelActive();
                }
                playerSlider.interactable = true;
                GameControllerOffline.Instance.strikerPositionAdjuster.SetSliderPosition(playerSlider);
                //playerPuck.GetComponent<PlayerPuck>().playerCollider.enabled = true;
                if (opponentSlider != null)
                {
                    opponentSlider.interactable = false;
                }
            }
            else
            {
                if (GameManager.Instance.currentGameMode == GameManager.GameMode.Ai)
                {
                    PlayerPuckOffline.Instance.ShootBot();
                }
                if (!statusScreen.activeInHierarchy)
                {
                    statusPanelText.text = "Opponent Turn!";
                    StatusPanelActive();
                }
                playerSlider.interactable = false;
                //playerPuck.GetComponent<PlayerPuck>().playerCollider.enabled = false;
                if (opponentSlider != null)
                {
                    opponentSlider.interactable = true;
                }
            }

            gameState = GameState.READY;

            if (practiceMode)
            {
                allPucks[0].ResetPosition();
            }

            for (int i = 0; i < allPucks.Length; i++)
            {
                allPucks[i].GetComponent<CircleCollider2D>().enabled = true;
            }

            HandleWorstCase();
        }

        public void PuckOnHole(string puckTag, string puckName)
        {
            Debug.Log("PuckOnHole");
            if (practiceMode) return;
            if (gameState == GameState.WIN || gameState == GameState.LOSE) return;

            if (puckTag == "Player")
            {
                pucksCollected.Add(playerPuck.gameObject);
            }
            else
            {
                PuckOffline getPuck = getPuckFromList(puckName);
                if (getPuck != null)
                {
                    pucksCollected.Add(getPuck.gameObject);
                }
            }
        }

        PuckOffline getPuckFromList(string puckName)
        {
            Debug.Log("getPuckFromList");
            for (int i = 0; i < allPucks.Length; i++)
            {
                if (allPucks[i].name == puckName)
                {
                    return allPucks[i];
                }
            }

            return null;
        }

        IEnumerator WaitForPucks()
        {
            Debug.Log("WaitForPucks");
            yield return new WaitForSecondsRealtime(0.5f);
            for (int i = 0; i < allPucks.Length; i++)
            {
                allPucks[i].setVelCheck(true);
            }
            playerPuckScript.setVelCheck(true);
            yield return new WaitUntil(() => isPucksFixed);
            //  Debug.LogError("isPucksFixed////////////////////////////////////////  ....  "+ Time.time);
            for (int i = 0; i < allPucks.Length; i++)
            {
                allPucks[i].setVelCheck(false);
            }
            playerPuckScript.setVelCheck(false);
            yield return new WaitForSecondsRealtime(1.5f);
            CheckGameStatus();
        }


        public void HandleWorstCase()
        {
            Debug.Log("HandleWorstCase");
            for (int i = 0; i < allPucks.Length; i++)
            {
                if (allPucks[i].puckInHole)
                {
                    //Debug.LogError("In Puck Holes Collide....  " + allPucks[i].gameObject.name);

                    allPucks[i].OnDisableProperties(true);
                }

                if (allPucks[i].circleCollider2D && IsInPuckHoles(allPucks[i]))
                {
                    allPucks[i].OnDisableProperties(true);

                    Debug.Log("In Puck Holes Collide....  ");
                }

            }

            List<GameObject> temList = new List<GameObject>();

            for (int i = 0; i < awayPucksCollected.Count; i++)
            {
                if (!awayPucksCollected[i].GetComponent<PuckOffline>().puckInHole)
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
                if (!homePucksCollected[i].GetComponent<PuckOffline>().puckInHole)
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


            if (!redPuckCollected && IsInPuckHoles(redPuck.GetComponent<PuckOffline>()) && redPuck.GetComponent<PuckOffline>().puckInHole)
            {
                redPuck.GetComponent<PuckOffline>().ResetPosition();
                GameControllerOffline.Instance.leftRedPuckIcon.SetActive(false);
                GameControllerOffline.Instance.rightRedPuckIcon.SetActive(false);
                Debug.LogError("Red Puck Case  ....  ");
            }
        }

        bool IsInPuckHoles(PuckOffline puck)
        {
            Debug.Log("IsInPuckHoles");
            for (int i = 0; i < puckHoles.Length; i++)
            {
                if (puckHoles[i].bounds.Contains(puck.transform.position))
                {
                    return true;
                }
            }
            return false;
        }
        void CheckGameStatus()
        {
            Debug.Log("CheckGameStatus");
            if (pucksCollected.Contains(playerPuck.gameObject))
            {
                Debug.Log("pucksCollectedplayerPuck");
                CheckPlayerPenalty();
            }
            else if (pucksCollected.Contains(redPuck))
            {
                Debug.Log("pucksCollectedredPuck");
                CheckRedPuckPenalty();
            }
            else
            {
                Debug.Log("SimpleCheckPucks");
                CheckPucks();
            }

            OnSliderPointerDown();
            playerPuck.GetComponent<PlayerPuckOffline>().ResetPosition();
            OnSliderPointerUp();

            UpdateScoreText();

            if (practiceMode)
            {
                CheckTurn();
                return;
            }

            //if (PhotonController.Instance.whichMode == "disc")
            //{
            //    if (homePucksCollected.Count == 9)
            //    {
            //        gameState = targetTag == "White" ? GameState.WIN : GameState.LOSE;
            //    }
            //    else if (awayPucksCollected.Count == 9)
            //    {
            //        gameState = targetTag == "Black" ? GameState.WIN : GameState.LOSE;
            //    }
            //}
            //else
            // if (PhotonController.Instance.whichMode == "carrom")
            {
                if (redPuckCollected)
                {
                    if (homePucksCollected.Count == 9)
                    {
                        gameState = targetTag == "White" ? GameState.WIN : GameState.LOSE;
                        //if (targetTag != "White")
                        //{
                        //    APIManager.instance.WinnerLossAIBet("ai");
                        //}
                        //else
                        //{
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        //  GuestGrandScripts.instance.ChangeGuestCoins(staticVariables.currentPrize*2);

                        // }
                    }
                    else if (awayPucksCollected.Count == 9)
                    {
                        gameState = targetTag == "Black" ? GameState.WIN : GameState.LOSE;
                        ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
                        // GuestGrandScripts.instance.ChangeGuestCoins(-staticVariables.currentPrize);

                        //if (targetTag == "Black")
                        //{
                        //}
                        //else
                        //{
                        //    APIManager.instance.WinnerLossAIBet(staticVariables.UserProfiledata.user._id.ToString());
                        //}
                    }

                    Debug.LogError("/............................................." + gameState);

                }

            }

            if (gameState == GameState.WIN || gameState == GameState.LOSE)
            {
                GameOver();
                return;
            }

            PlayerPuckOffline.Instance.ResetPosition();
            reduceTimer = true;
            if (gameState == GameState.WAIT)
            {
                if (!shootAgain)
                {
                    gameState = GameState.SWITCH_MASTER;
                    StartCoroutine(SwitchMasterDelay());
                }
                else
                {

                    RPC_ResetOpponentTimer();

                    gameState = GameState.READY;
                    if (whichPlayer == WhichPlayer.OTHER)
                    {
                        //playerPuck.GetComponent<PlayerPuck>().playerCollider.enabled = false;
                        PlayerPuckOffline.Instance.ShootBot();
                        playerSlider.interactable = false;
                    }
                    else
                    {
                        if (!statusScreen.activeInHierarchy && !queenCheck)
                        {
                            if (!redPuckWaiting)
                            {
                                statusPanelText.text = "Go On!";
                            }
                            else
                            {
                                statusPanelText.text = "Cover The Queen!";
                            }
                        }
                        //playerPuck.GetComponent<PlayerPuck>().playerCollider.enabled = true;
                        playerSlider.interactable = true;
                        StatusPanelActive();
                    }
                }
            }
            else
            {
                gameState = GameState.READY;
            }
        }

        void RPC_ResetOpponentTimer()
        {
            Debug.Log("RPC_ResetOpponentTimer");
            timer = Constants.PLAY_TIME_FOR_PLAYER;
        }

        IEnumerator SwitchMasterDelay()
        {
            Debug.Log("SwitchMasterDelay");
            yield return new WaitForSecondsRealtime(0.5f);
            Event_SwitchingMasterClient?.Invoke();
            yield return new WaitForSecondsRealtime(0.2f);
            // SendData();
            if (GameManager.Instance.currentGameMode == GameManager.GameMode.Ai)
            {
                if (whichPlayer == WhichPlayer.ME)
                {
                    //playerPuck.GetComponent<PlayerPuck>().playerCollider.enabled = false;
                    whichPlayer = WhichPlayer.OTHER;
                }
                else
                {
                    //playerPuck.GetComponent<PlayerPuck>().playerCollider.enabled = true;
                    whichPlayer = WhichPlayer.ME;
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


        }

        void CheckPlayerPenalty()
        {
            Debug.Log("CheckPlayerPenalty");

            if (pucksCollected.Contains(redPuck))
            {
                pucksCollected.Remove(redPuck);
                redPuck.GetComponent<PuckOffline>().ResetPosition();
                // redPuck.GetComponent<PuckOffline>().BroadCastResetPosition();
            }

            if (redPuckWaiting)
            {
                redPuckWaiting = false;
                // redPuck.GetComponent<PuckOffline>().BroadCastResetPosition();
            }

            for (int i = 0; i < pucksCollected.Count; i++)
            {
                GameObject targetPuck = pucksCollected[i];

                if (targetPuck.CompareTag("Player")) continue;


                if (targetPuck.CompareTag(masterClientTag))
                {
                    targetPuck.GetComponent<PuckOffline>().ResetAndRemove();
                }
                else
                {
                    if (masterClientTag == "White")
                    {

                        if (!awayPucksCollected.Contains(targetPuck))
                        {
                            awayPucksCollected.Add(targetPuck);
                        }
                    }
                    else
                    {
                        if (!homePucksCollected.Contains(targetPuck))
                        {
                            homePucksCollected.Add(targetPuck);
                        }

                    }

                }
            }

            pucksCollected.Clear();

            if (masterClientTag == "White")
            {
                if (homePucksCollected.Count > 0)
                {
                    GameObject targetPuck = homePucksCollected[0];
                    targetPuck.GetComponent<PuckOffline>().ResetAndRemove();
                }
            }
            else
            {
                if (awayPucksCollected.Count > 0)
                {
                    GameObject targetPuck = awayPucksCollected[0];
                    targetPuck.GetComponent<PuckOffline>().ResetAndRemove();

                }
            }

            //  if (PhotonController.Instance.whichMode == "carrom")
            {
                if (homePucksCollected.Count.Equals(9) && !redPuckCollected)
                {
                    homePucksCollected[8].GetComponent<PuckOffline>().ResetAndRemove();
                }
                if (awayPucksCollected.Count.Equals(9) && !redPuckCollected)
                {
                    awayPucksCollected[8].GetComponent<PuckOffline>().ResetAndRemove();
                }
            }

        }

        void CheckRedPuckPenalty()
        {
            Debug.Log("CheckRedPuckPenalty");
            bool isValid = true;

            for (int i = 0; i < pucksCollected.Count; i++)
            {
                GameObject targetPuck = pucksCollected[i];

                if (targetPuck.CompareTag("Player") || targetPuck.CompareTag("Red"))
                {
                    if (masterClientTag == "White")
                    {
                        if (targetPuck.CompareTag("Red") && homePucksCollected.Count < 8)
                        {
                            isValid = false;
                        }
                    }
                    else
                    {
                        if (targetPuck.CompareTag("Red") && awayPucksCollected.Count < 8)
                        {
                            isValid = false;
                        }
                    }

                    continue;
                }

                // Not Valid Case
                if (!targetPuck.CompareTag(masterClientTag))
                {
                    isValid = false;
                }
            }

            //Debug.LogError($"IsValid Status {isValid}");

            if (isValid)
            {
                redPuckWaiting = true;
                shootAgain = true;
                pucksCollected.Remove(redPuck);
            }
            else
            {
                redPuck.GetComponent<PuckOffline>().ResetPosition();
                List<GameObject> tempList = new List<GameObject>();


                // Reset Every puck except, Player and Opponent Puck
                for (int i = 0; i < pucksCollected.Count; i++)
                {
                    GameObject targetPuck = pucksCollected[i];
                    if (targetPuck.CompareTag("Player") || !targetPuck.CompareTag(masterClientTag)) continue;

                    // Only the turn player puck && Red
                    //targetPuck.GetComponent<Puck>().ResetPosition();
                    targetPuck.GetComponent<PuckOffline>().ResetAndRemove();

                    tempList.Add(targetPuck);
                    //pucksCollected.Remove(targetPuck);
                }

                for (int i = 0; i < tempList.Count; i++)
                {
                    pucksCollected.Remove(tempList[i]);
                }
                tempList.Clear();
            }



            // Clearing the PucksCollected List
            for (int i = 0; i < pucksCollected.Count; i++)
            {
                var targetPuck = pucksCollected[i];

                if (targetPuck.CompareTag("White"))
                {
                    if (!homePucksCollected.Contains(targetPuck))
                    {
                        homePucksCollected.Add(targetPuck);
                    }
                }
                else if (targetPuck.CompareTag("Black"))
                {
                    if (!awayPucksCollected.Contains(targetPuck))
                    {
                        awayPucksCollected.Add(targetPuck);
                    }
                }
            }

            // Clear the pucksCollected list after handling each puck
            pucksCollected.Clear();


            //Checking 9th Puck with red Condition
            if (homePucksCollected.Count.Equals(9))
            {
                //Puck puck = homePucksCollected[8].GetComponent<Puck>();
                //puck.ResetPosition();
                //homePucksCollected.RemoveAt(8);

                homePucksCollected[8].GetComponent<PuckOffline>().ResetAndRemove();
            }
            if (awayPucksCollected.Count.Equals(9))
            {
                awayPucksCollected[8].GetComponent<PuckOffline>().ResetAndRemove();

                //Puck puck = awayPucksCollected[8].GetComponent<Puck>();
                //puck.ResetPosition();
                //awayPucksCollected.RemoveAt(8);
            }
        }

        void ChangePuckMessage()
        {
            Debug.Log("ChangePuckMessage");
            toastMessageText.text = "Pot queen and cover it to win the match";
        }

        bool IsContainMyPuck(string puckType)
        {
            Debug.Log("IsContainMyPuck");
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
            Debug.Log("CheckPucks");
            bool tempRedPuckWaiting = redPuckWaiting;
            bool isContainMyPuck = this.IsContainMyPuck(masterClientTag);



            Debug.Log("Contains Other " + isContainMyPuck);

            if (pucksCollected.Count == 0)
            {
                if (redPuckWaiting)
                {
                    redPuckWaiting = false;
                    redPuck.GetComponent<PuckOffline>().ResetPosition();
                    //   redPuck.GetComponent<PuckOffline>().BroadCastResetPosition();
                }
                return;
            }

            if (tempRedPuckWaiting)
            {
                if (isContainMyPuck)
                {

                    redPuckCollected = true;
                    //leftRedPuckIcon.SetActive(true);
                    queenCheck = true;
                    tempRedPuckWaiting = false;
                    statusPanelText.text = "Queen Covered!";

                }
                else
                {
                    tempRedPuckWaiting = false;
                    queenCheck = false;
                    redPuck.GetComponent<PuckOffline>().ResetPosition();
                    // targetPuck.GetComponent<Puck>().ResetPosition();
                }
            }

            for (int i = 0; i < pucksCollected.Count; i++)
            {
                var targetPuck = pucksCollected[i];

                if (targetPuck.CompareTag(masterClientTag))
                {
                    shootAgain = true;
                }

                //if (PhotonController.Instance.whichMode == "carrom")
                {
                    if (masterClientTag == "White")
                    {
                        if (targetPuck.CompareTag("White"))
                        {

                            if (homePucksCollected.Count == 8 && !redPuckCollected)
                            {
                                targetPuck.GetComponent<PuckOffline>().ResetAndRemove();
                                shootAgain = false;
                                if (whichPlayer.Equals(GameControllerOffline.WhichPlayer.ME))
                                {
                                    statusPanelText.text = " Foul \n Pot Queen First";
                                    StatusPanelActive();
                                }

                            }
                            else
                            {
                                if (!homePucksCollected.Contains(targetPuck))
                                {
                                    homePucksCollected.Add(targetPuck);
                                }
                            }
                        }
                        else
                        {

                            if (awayPucksCollected.Count == 8)
                            {
                                if (redPuckCollected)
                                {
                                    if (!awayPucksCollected.Contains(targetPuck))
                                    {
                                        awayPucksCollected.Add(targetPuck);
                                    }

                                }
                                else
                                {
                                    targetPuck.GetComponent<PuckOffline>().ResetAndRemove();
                                }
                            }
                            else
                            {
                                if (!awayPucksCollected.Contains(targetPuck))
                                {
                                    awayPucksCollected.Add(targetPuck);
                                }
                            }

                        }
                    }
                    else if (masterClientTag == "Black")
                    {
                        if (targetPuck.CompareTag("Black"))
                        {

                            if (awayPucksCollected.Count == 8 && !redPuckCollected)
                            {
                                targetPuck.GetComponent<PuckOffline>().ResetAndRemove();
                                shootAgain = false;
                                if (whichPlayer.Equals(GameControllerOffline.WhichPlayer.ME))
                                {
                                    statusPanelText.text = " Foul \n Pot Queen First";
                                    StatusPanelActive();
                                }

                            }
                            else
                            {
                                if (!awayPucksCollected.Contains(targetPuck))
                                {
                                    awayPucksCollected.Add(targetPuck);
                                }
                                // awayPucksCollected.Add(targetPuck);
                            }
                        }
                        else
                        {

                            if (homePucksCollected.Count == 8)
                            {
                                if (redPuckCollected)
                                {
                                    //homePucksCollected.Add(targetPuck);
                                    if (!homePucksCollected.Contains(targetPuck))
                                    {
                                        homePucksCollected.Add(targetPuck);
                                    }
                                }
                                else
                                {
                                    targetPuck.GetComponent<PuckOffline>().ResetAndRemove();
                                }
                            }
                            else
                            {
                                if (!homePucksCollected.Contains(targetPuck))
                                {
                                    homePucksCollected.Add(targetPuck);
                                }
                                //  homePucksCollected.Add(targetPuck);
                            }
                            // }
                        }
                    }
                }
                //else
                //{
                //    if (targetPuck.CompareTag("White"))
                //    {
                //        homePucksCollected.Add(targetPuck);
                //    }
                //    else if (targetPuck.CompareTag("Black"))
                //    {
                //        awayPucksCollected.Add(targetPuck);
                //    }
                //}
            }
            pucksCollected.Clear();
            redPuckWaiting = tempRedPuckWaiting;
        }

        public void UpdateScoreText()
        {
            Debug.Log("UpdateScoreText");
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

            //    if (PhotonController.Instance.whichMode == "carrom")
            {

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

            //Debug.LogError($"MyScore: {homePucksCollected.Count} && EnemyScore: {awayPucksCollected.Count}");
        }


        public void CheckState(bool isWhiteColledQueen)
        {
            Debug.Log("CheckState");
            if (!GameManager.Instance.isOnline())
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
                else

                if (homePucksCollected.Count == 9)
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
                else
                if (awayPucksCollected.Count == 9)
                {
                    gameState = GameState.WIN;
                }
                else
                {
                    gameState = GameState.LOSE;
                }
            }
        }



        void GameOver()
        {
            Debug.Log("GameOver");
            StartCoroutine(WiatForGameState());
        }
        bool finishGame;
        private void OnEnable()
        {
            //Wasi  PunNetwork.OnLosser += Announcelosser;
            //Wasi   PunNetwork.waitingForOpponent += WaitingForOpponentt;
            //Wasi   PunNetwork.OnWinner += AnnounceWinner;
        }
        private void OnDisable()
        {
            //Wasi   PunNetwork.OnLosser -= Announcelosser;
            //Wasi   PunNetwork.waitingForOpponent -= WaitingForOpponentt;
            //Wasi   PunNetwork.OnWinner -= AnnounceWinner;
        }
        public void WaitingForOpponentt(bool state)
        {
            Debug.Log("WaitingForOpponentt");
            if (state == true)
            {
                if (finishGame == false)
                    PopupMessageManager.instance.SetPanelStaus(state, body: "Waiting for oppoent");
            }
            else
            {
                PopupMessageManager.instance.SetPanelStaus(state, body: "Waiting for oppoent");
            }
        }
        public void AnnounceWinner()
        {
            Debug.Log("AnnounceWinner");
            if (masterClientTag == "White")
            {
                homePucksCollected = new List<GameObject>(new GameObject[9]);
            }
            else
            {
                awayPucksCollected = new List<GameObject>(new GameObject[9]);
            }
            gameState = GameState.WIN;

            gameOverPanel.SetActive(true);


        }
        public void Announcelosser()
        {
            //if (masterClientTag == "White")
            //{

            //    homePucksCollected = new List<GameObject>(0);
            //    CheckState(true);
            //}
            //else
            //{
            //    CheckState(false);
            //    awayPucksCollected = new List<GameObject>(0);
            //}
            //gameState = GameState.LOSE;
            //gameOverPanel.SetActive(true);
        }

        IEnumerator WiatForGameState()
        {
            Debug.LogError("Game State.... 1" + gameState);

            yield return new WaitUntil(() => (gameState != GameState.WAIT));
            //   PhotonController.Instance.gameOver = true;  // Carromintegration
            settingsScreen.SetActive(false);
            //  PhotonNetwork.AutomaticallySyncScene = false;
            gameOverPanel.SetActive(true);
        }

        public void IncScoreOnCollected(bool isHome)
        {
            Debug.Log("IncScoreOnCollected");
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
            Debug.Log("DisplayPopUp");
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