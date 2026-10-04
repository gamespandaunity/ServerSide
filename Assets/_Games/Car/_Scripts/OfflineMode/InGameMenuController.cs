using UnityEngine.SceneManagement;

namespace CarRace
{
    using Michsky.LSS;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System.Collections.Generic;
    using System;
    using System.Collections;
    using UnityEngine.Serialization;

    /// <summary>
    /// Store the References of UI and UI functions and Events
    /// </summary>
    public class InGameMenuController : MonoBehaviour
    {
        public static InGameMenuController Instance;
        [Space(3), Header("UI References")]
        [FormerlySerializedAs("mobileControls"), SerializeField] GameObject mobileControlPanel;
        [FormerlySerializedAs("changeControlsImage"), SerializeField] Image controlModeIcon ;
        [FormerlySerializedAs("pauseButton"), SerializeField] Button pauseGameButton;
        [FormerlySerializedAs("lapText")] public TextMeshProUGUI currentLapText;
        [FormerlySerializedAs("posText")] public TextMeshProUGUI currentPositionText;
        [FormerlySerializedAs("wrongWayPopUp")] public GameObject wrongDirectionWarning;

        [Space(3), Header("Start Menu")]
        [FormerlySerializedAs("StartMenu"), SerializeField] GameObject startMenuPanel;
        [FormerlySerializedAs("startRaceButton"), SerializeField] Button beginRaceButton ;

        [Space(3), Header("Race Finish Menu")]
        [FormerlySerializedAs("finishPanel"), SerializeField]public GameObject raceFinishPanel;
        [FormerlySerializedAs("winLoseManagerCar"), SerializeField] ResultManagerForCar raceOutcomeManager;
        [FormerlySerializedAs("restartFinishButton"), SerializeField] Button restartRaceButton;
        [FormerlySerializedAs("mainMenuFinishButton"), SerializeField] Button returnToMainMenuButton;
        [FormerlySerializedAs("doubleRewardButton"), SerializeField] Button claimDoubleRewardButton;
        [FormerlySerializedAs("positionLabelTexts"), SerializeField] TextMeshProUGUI[] positionLabels;
        [FormerlySerializedAs("playerNamePositionsTexts"), SerializeField] TextMeshProUGUI[] playerPositionNames;
        [FormerlySerializedAs("rewardTexts"), SerializeField] TextMeshProUGUI[] rewardAmountTexts;
        [FormerlySerializedAs("playerPositiontext"), SerializeField] TextMeshProUGUI finalPlayerPositionText;
        [FormerlySerializedAs("stars"), SerializeField] GameObject[] performanceStars;
        [FormerlySerializedAs("remarksText"), SerializeField] TextMeshProUGUI resultRemarksText;

        [Space(3), Header("Pause Menu")]
        [FormerlySerializedAs("pauseMenu"), SerializeField] GameObject pauseMenuPanel;
        [FormerlySerializedAs("repairButton"), SerializeField] Button repairVehicleButton;
        [FormerlySerializedAs("resumeButtons"), SerializeField] Button[] resumeGameButtons;
        [FormerlySerializedAs("mainMenuButton"), SerializeField] Button pauseToMainMenuButton;
        [FormerlySerializedAs("restartMenuButton"), SerializeField] Button pauseRestartButton;

        [Space(3), Header("Ads Related UI")]
        [FormerlySerializedAs("rewardPopUp"), SerializeField] GameObject adRewardPopup;
        [FormerlySerializedAs("totalRewardText"), SerializeField] TextMeshProUGUI adTotalRewardText;

        [Space(3), Header("Restart Menu")]
        [FormerlySerializedAs("restartMenu"), SerializeField] GameObject restartMenuPanel;
        [FormerlySerializedAs("restartButton"), SerializeField] Button confirmRestartButton;

        [Space(3), Header("Quit Menu")]
        [FormerlySerializedAs("quitMenu"), SerializeField] GameObject quitConfirmationPanel;
        [FormerlySerializedAs("quitButton"), SerializeField] Button confirmQuitButton;

        [Space(3), Header("Scripts References")]
        [FormerlySerializedAs("loadingScreenManager"), SerializeField] LoadingScreenManager loadingManager;
        [HideInInspector, FormerlySerializedAs("RCC_Cam")] public RCC_Camera carCameraController;
        [HideInInspector, FormerlySerializedAs("RCC_Car")] public RCC_CarControllerV3 playerCarController;
        [HideInInspector, FormerlySerializedAs("totalLaps")] public int maxLaps;
        [HideInInspector, FormerlySerializedAs("totalPlayers")] public int playerCount;


        private void Awake()
        {
            Instance = this;
        }

        int userCoins = 0;
        int currentControls;
        private void Start()
        {
            mobileControlPanel.SetActive(false);
            SetButtonListeners();
            ApplyUserSeetings();
            //Disable All Entries first
            for (int i = 0; i < positionLabels.Length; i++)
            {
                positionLabels[i].gameObject.SetActive(false);
                playerPositionNames[i].gameObject.SetActive(false);
                rewardAmountTexts[i].gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventManager.OnRewardGiven += GiveReward;
            EventManager.OnGameStartEvent += OnGameStart;
            EventManager.OnGameFinishEvent += OnGameFinish;
            EventManager.OnLevelCutSceneFinishEvent += OnCutSceneFinish;
            EventManager.PrepareResults += PrepareResults;
            EventManager.OnShowResult += ShowResult;
            EventManager.OnInterstitialAdFinished += PauseAfterAd;
        }

        private void OnDisable()
        {
            EventManager.OnRewardGiven -= GiveReward;
            EventManager.OnGameStartEvent -= OnGameStart;
            EventManager.OnGameFinishEvent -= OnGameFinish;
            EventManager.OnLevelCutSceneFinishEvent -= OnCutSceneFinish;
            EventManager.PrepareResults -= PrepareResults;
            EventManager.OnShowResult -= ShowResult;
            EventManager.OnInterstitialAdFinished += PauseAfterAd;
        }

        void OnCutSceneFinish()
        {
            ActivateMenu(startMenuPanel.name);
        }
        private void OnGameStart(int totalLaps, int totalPlayers)
        {
            mobileControlPanel.SetActive(true);
            ActivateMenu("");
            currentLapText.text = "Laps " + 1 + "/" + totalLaps;
        }
        void OnGameFinish()
        {
            currentLapText.transform.parent.gameObject.SetActive(false);
            currentPositionText.transform.parent.gameObject.SetActive(false);
            // DisplayResult();
        }

        private void PrepareResults(List<PlayerDataForPositionSystem> playersData)
        {
            int position = 0;
            int reward = 150 * playersData.Count;
            foreach (PlayerDataForPositionSystem playerData in playersData)
            {
                positionLabels[position].gameObject.SetActive(true);
                playerPositionNames[position].gameObject.SetActive(true);
                rewardAmountTexts[position].gameObject.SetActive(true);

                playerPositionNames[position].text = playerData.playerName;
                rewardAmountTexts[position].text = (reward / (position + 1)).ToString();
                position += 1;
            }                             
            "finish".Show();
            GiveRewardToUser(playersData);
        }
        void ShowResult()
        {
            ActivateMenu(raceFinishPanel.name);
        }
        void GiveRewardToUser(List<PlayerDataForPositionSystem> playersData)
        {
            int playerPosition = playersData.FindIndex(playerData => playerData.playerName == PlayerPrefs.GetString(PPConst.UserName));
            int userRewared = (150 * playersData.Count) / (playerPosition + 1);
            UserData.AddCoins(userRewared);
            userCoins = userRewared;
            SetLocalPlayerData(playerPosition + 1);
            //doubleRewardButton.onClick.AddListener(() => { EventManager.OnShowAd_Rewarded?.Invoke(); });

            void SetLocalPlayerData(int position)
            {
                performanceStars[0].SetActive(false);
                performanceStars[1].SetActive(false);
                performanceStars[2].SetActive(false);
                switch (position)
                {
                    case 1:
                        performanceStars[0].SetActive(true);
                        performanceStars[1].SetActive(true);
                        performanceStars[2].SetActive(true);
                        finalPlayerPositionText.text = "1st";
                        resultRemarksText.text = "Beat The Opponents";
                        Invoke("CompleteAfterTime", 4);
                        ; break;
                    case 2:
                        performanceStars[0].SetActive(true);
                        performanceStars[1].SetActive(true);
                        performanceStars[2].SetActive(true);
                        finalPlayerPositionText.text = "2nd";
                        resultRemarksText.text = "Runner Up!";
                        Invoke("FailAfterTime", 4);
                        break;
                    case 3:
                        performanceStars[0].SetActive(true);
                        performanceStars[1].SetActive(true);
                        finalPlayerPositionText.text = "3rd";
                        resultRemarksText.text = "Good Race!!";
                        Invoke("FailAfterTime", 4);
                        break;
                    case 4:
                        performanceStars[0].SetActive(true);
                        performanceStars[1].SetActive(true);
                        finalPlayerPositionText.text = "4th";
                        resultRemarksText.text = "Better Luck Next Time";
                        Invoke("FailAfterTime", 4);
                        break;
                    case 5:
                        performanceStars[0].SetActive(true);
                        finalPlayerPositionText.text = "5th";
                        resultRemarksText.text = "Practice Makes a Man Perfect";
                        Invoke("FailAfterTime", 4);
                        break;
                    case 6:
                        performanceStars[0].SetActive(true);
                        finalPlayerPositionText.text = "6th";
                        resultRemarksText.text = "Need improvement! Keep Practing";
                        Invoke("FailAfterTime", 4);
                        break;
                    default:
                        break;
                }
            }
        }

        public void CompleteAfterTime()
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
                    gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    Debug.LogError("WinLose GameManager prefab not found!");
                }
            }
          //  raceOutcomeManager.HandleGameResultAlt(true, staticVariables.UserProfiledata.user._id.ToString());
        }
        public void FailAfterTime()
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
                    gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    Debug.LogError("WinLose GameManager prefab not found!");
                }
            }
           // raceOutcomeManager.HandleGameResultAlt(false, staticVariables.UserProfiledata.user._id.ToString());

        }
        void ApplyUserSeetings()
        {
            currentControls = PlayerPrefs.GetInt(PPConst.ControlType);
            carCameraController.TPSAutoReverse = Convert.ToBoolean(PlayerPrefs.GetInt(PPConst.ReverseCamera));
            playerCarController.useDamage = Convert.ToBoolean(PlayerPrefs.GetInt(PPConst.Damage));
            repairVehicleButton.gameObject.SetActive(Convert.ToBoolean(PlayerPrefs.GetInt(PPConst.Damage)));
            //AudioListener.volume = 0.75f; // PlayerPrefs.GetFloat(PPConst.Volume);
        }
        void SetButtonListeners()
        {
            //HUD  Buttons
            controlModeIcon .GetComponent<Button>().onClick.AddListener(ChangeControlType);
            pauseGameButton.onClick.AddListener(PauseGame);

            //Pause Menu Buttons
            foreach (Button resumeButton in resumeGameButtons)
                resumeButton.onClick.AddListener(ResumeGame);
            pauseRestartButton.onClick.AddListener(delegate { ActivateMenu(restartMenuPanel.name); });
            pauseToMainMenuButton.onClick.AddListener(delegate { ActivateMenu(quitConfirmationPanel.name); });
            repairVehicleButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                ResumeGame();
                playerCarController.Repair();
            });


            //Restart Menu Buttons
            confirmRestartButton.onClick.AddListener(() =>
            {
                loadingManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
            confirmQuitButton.onClick.AddListener(LoadMainMenu);
            returnToMainMenuButton.onClick.AddListener(LoadMainMenu);
            restartRaceButton.onClick.AddListener(() =>
            {
                loadingManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
            beginRaceButton .onClick.AddListener(() =>
            {
                EventManager.OnGameStartEvent.Invoke(maxLaps, playerCount);
            });
        }
        void PauseGame()
        {
            ////Debug.Log("Un Comment ads here");
            ShowInterstitialAd();
            ActivateMenu(pauseMenuPanel.name);
            EventManager.OnGamePauseEvent.Invoke();

        }
        void PauseAfterAd()
        {
            if (GameManager._Instance.hasGameEnded)
                return;
            EventManager.OnGamePauseEvent?.Invoke();
        }
        void ResumeGame()
        {
            EventManager.OnGameResumeEvent();
            ActivateMenu("");

        }
        public void ActivateMenu(string Menu)
        {
            pauseMenuPanel.SetActive(pauseMenuPanel.name.Equals(Menu));
            quitConfirmationPanel.SetActive(quitConfirmationPanel.name.Equals(Menu));
            restartMenuPanel.SetActive(restartMenuPanel.name.Equals(Menu));
            startMenuPanel.SetActive(startMenuPanel.name.Equals(Menu));
            raceFinishPanel.SetActive(raceFinishPanel.name.Equals(Menu));
        }
        void LoadMainMenu()
        {
            //enable TPS when loadind new scene, othjerwise it throws error.
            carCameraController.cameraMode = RCC_Camera.CameraMode.TPS;
            SceneManager.LoadScene("Home");
        }
        void ChangeControlType()
        {
            currentControls += 1;
            currentControls = currentControls > 3 ? 0 : currentControls;

            switch (currentControls)
            {

                case 0:
                    RCC.SetMobileController(RCC_Settings.MobileController.TouchScreen);
                    break;
                case 1:
                    RCC.SetMobileController(RCC_Settings.MobileController.Joystick);
                    break;
                case 2:
                    RCC.SetMobileController(RCC_Settings.MobileController.SteeringWheel);
                    break;
                case 3:
                    RCC.SetMobileController(RCC_Settings.MobileController.Gyro);
                    break;

            }
            PlayerPrefs.SetInt(PPConst.ControlType, currentControls);
        }
        void GiveReward()
        {
            claimDoubleRewardButton.interactable = false;
            adRewardPopup.SetActive(true);
            int doubleReward = 2 * userCoins;
            adTotalRewardText.text = " Congratulations!!\n You Got $" + doubleReward;
            UserData.SubtractCoins(userCoins);
            UserData.AddCoins(doubleReward);
        }
        public void ShowInterstitialAd()
        {
            //if (EventManager.OnCheckAdLoaded_Interstitial.Invoke())
            //    EventManager.OnShowAd_Interstitial?.Invoke();
        }

    }

}