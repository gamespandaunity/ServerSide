namespace CarRace
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    using UnityEngine.Playables;
    using UnityEngine.UI;
    using UnityEngine.Serialization;

    /// <summary>
    /// Game Controller Of Offline Mode
    /// </summary>
    [RequireComponent(typeof(InGameMenuController))]
    [RequireComponent(typeof(PlayerPositionSystem))]
    public class GameManager : MonoBehaviour
    {
        public static GameManager _Instance;
        Unity.Cinemachine.CinemachineTrackedDolly cinemachineTrackedDolly;

        [Header("Cinematics Objects")]
        [FormerlySerializedAs("timeLine"), SerializeField] PlayableDirector introTimeline;
        [FormerlySerializedAs("virtualCam"), SerializeField] Unity.Cinemachine.CinemachineVirtualCamera mainVirtualCamera;
        [FormerlySerializedAs("cameraTrackCarts"), SerializeField] Unity.Cinemachine.CinemachineSmoothPath[] cameraPaths;
        [FormerlySerializedAs("cinemachineBrain"), SerializeField] Unity.Cinemachine.CinemachineBrain cameraController;

        [Header("Player Prefab and Spawn Locations")]
        [FormerlySerializedAs("carPrefabs"), SerializeField] RCC_CarControllerV3[] playerCarPrefabs;
        [FormerlySerializedAs("aiCars"), SerializeField] RCC_CarControllerV3[] aiCarPrefabs;
        [FormerlySerializedAs("spawnPoints"), SerializeField] Transform[] playerSpawnPoints;
        [FormerlySerializedAs("waypointsContainer"), SerializeField] RCC_AIWaypointsContainer aiWaypointPath;



        int selectedCarIndex = 0;
        [Space(3), Header("Scripts References")]
        [FormerlySerializedAs("RCC_Cam")] public RCC_Camera carCameraController;
        [HideInInspector, FormerlySerializedAs("RCC_Car")] public RCC_CarControllerV3 playerCarController;
        [FormerlySerializedAs("totalLaps")] public int maxLapsPerRace = 2;

        [Space(3), Header("Wrong Direction Check WayPoint")]
        [FormerlySerializedAs("waypoints")] public List<Transform> wrongWayCheckpoints;

        [Space(3), Header("Lap Triggers")]
        [FormerlySerializedAs("halfLapTrigger"), SerializeField] BoxCollider midLapTriggerZone;
        [FormerlySerializedAs("finishLapTrigger"), SerializeField] BoxCollider lapFinishTriggerZone;

        [Space(3), Header("Development Values")]
        [FormerlySerializedAs("testingMode"), SerializeField] bool enableTestingMode;
        [FormerlySerializedAs("spawnCar"), SerializeField] bool spawnPlayerCar;
        [FormerlySerializedAs("spawnAICars"), SerializeField] bool spawnEnemyCars;
        [SerializeField] int numberOfAICars = 1;
        [FormerlySerializedAs("playCinematics"), SerializeField] bool enableCinematicIntro;
        [FormerlySerializedAs("difficultyLevel"), SerializeField] int gameDifficultyLevel;
        [HideInInspector, FormerlySerializedAs("isGameFinished")] public bool hasGameEnded = false;

        void Awake()
        {
            _Instance = this;
            maxLapsPerRace = 2;
            numberOfAICars = 1;
            gameDifficultyLevel = 2;
            SpawnUserCar();
            SpawnAI();

            if (enableCinematicIntro)
            {
                StartCutScene();
            }
            else
            {
                GetComponent<InGameMenuController>().ActivateMenu("StartMenu");
                introTimeline.gameObject.SetActive(false);
            }

        }
        private void OnEnable()
        {
            EventManager.OnShowFinishCinematics += ShowFinishCinematics;
            EventManager.OnGameStartEvent += OnGameStart;
            EventManager.OnGameFinishEvent += OnGameFinish;
            EventManager.OnGamePauseEvent += OnGamePause;
            EventManager.OnGameResumeEvent += OnGameResume;
            EventManager.OnLapFinishEvent += OnLapFinished;
            EventManager.OnHalfLapCompleteEvent += OnHalfLapComplete;
            introTimeline.stopped += StopCutScene;
        }
        private void OnDisable()
        {
            EventManager.OnShowFinishCinematics -= ShowFinishCinematics;
            EventManager.OnGameStartEvent -= OnGameStart;
            EventManager.OnGameFinishEvent -= OnGameFinish;
            EventManager.OnGamePauseEvent -= OnGamePause;
            EventManager.OnGameResumeEvent -= OnGameResume;
            EventManager.OnLapFinishEvent -= OnLapFinished;
            EventManager.OnHalfLapCompleteEvent -= OnHalfLapComplete;
            introTimeline.stopped += StopCutScene;
        }
        void SpawnAI()
        {
            if (spawnEnemyCars)
            {
                List<string> aiNames = UserData.GetRandomNames(numberOfAICars);
                Debug.Log($"AI Names: {string.Join(", ", aiNames)}");
                List<int> randomIndexNumberForAICars = UserData.GetRandomNumbers(range: playerCarPrefabs.Length - 1, count: numberOfAICars);
                Debug.Log($"Random Index Numbers for AI Cars: {string.Join(", ", randomIndexNumberForAICars)}");

                for (int i = 0; i < numberOfAICars; i++)
                {
                    Debug.Log($"Spawning AI Car {i}");
                    int index = randomIndexNumberForAICars[i];
                    Debug.Log(index);
                    var aiCar = RCC.SpawnRCC(aiCarPrefabs[index], playerSpawnPoints[i + 1].position, playerSpawnPoints[i + 1].rotation, false, false, true).GetComponent<RCC_AICarController>();
                    aiCar.waypointsContainer = aiWaypointPath;
                    aiCar.GetComponent<AIController>().SetPlayerNameandDifficulty(aiNames[i], gameDifficultyLevel);
                }
            }
        }
        void SpawnUserCar()
        {

            selectedCarIndex = PlayerPrefs.GetInt(PPConst.SavedCar);

            if (spawnPlayerCar)
            {
                playerCarController = RCC.SpawnRCC(playerCarPrefabs[selectedCarIndex], playerSpawnPoints[0].position, playerSpawnPoints[0].rotation, true, false, true);
                RCC.RegisterPlayerVehicle(playerCarController);
                if (RCC_SceneManager.Instance.activePlayerCamera)
                    RCC_SceneManager.Instance.activePlayerCamera.SetTarget(playerCarController.gameObject);
                PassReferences();
            }
        }
        void PassReferences()
        {
            GetComponent<PlayerPositionSystem>().waypoints = wrongWayCheckpoints;

            InGameMenuController menuController = GetComponent<InGameMenuController>();
            menuController.carCameraController = carCameraController;
            menuController.playerCarController = playerCarController;
            menuController.maxLaps = maxLapsPerRace;
            menuController.playerCount = numberOfAICars + 1;

            var checkDir = playerCarController.GetComponent<CheckWrongDirection>();
            checkDir.wrongDirectionCheckpoints = wrongWayCheckpoints;
            checkDir.wrongWayPopUp = menuController.wrongDirectionWarning;

            var userCarController = playerCarController.GetComponent<UserCarController>();
            userCarController.lapText = menuController.currentLapText;
            userCarController.posText = menuController.currentPositionText;
        }
        void StartCutScene()
        {
            cinemachineTrackedDolly = mainVirtualCamera.GetCinemachineComponent<Unity.Cinemachine.CinemachineTrackedDolly>();
            int dollyTrack = Random.Range(0, cameraPaths.Length);
            cinemachineTrackedDolly.m_Path = cameraPaths[dollyTrack];
            cameraController.enabled = true;
            introTimeline.gameObject.SetActive(true);
            carCameraController.isRendering = false;
        }
        void StopCutScene(PlayableDirector playableDirector)
        {
            carCameraController.isRendering = true;
            EventManager.OnLevelCutSceneFinishEvent?.Invoke();
            introTimeline.gameObject.SetActive(false);
        }
        void OnGameStart(int lapCount, int totalPlayers)
        {
            carCameraController.useOrbitInTPSCameraMode = true;
            StartCoroutine(ToggleLapTriggers(halfLap: true, finishLap: false));
            //  SpawnMiniMap();
        }
        void SpawnMiniMap()
        {
            GameObject miniMapObject = Instantiate(Resources.Load("MiniMap", typeof(GameObject))) as GameObject;
            Transform miniMapCam = miniMapObject.transform.GetChild(0).transform;
            miniMapCam.transform.SetParent(playerCarController.transform, false);
            miniMapCam.transform.localPosition = new Vector3(0f, 40f, 0f);
            miniMapCam.transform.localScale = Vector3.one;
            miniMapCam.transform.localEulerAngles = new Vector3(90, 0, 0);
        }
        void OnGamePause()
        {
            AudioListener.volume = 0;
            Time.timeScale = 0f;
        }
        void OnGameResume()
        {
            Time.timeScale = 1f;

            AudioListener.volume = PlayerPrefs.GetInt("Sound");
        }
        void OnHalfLapComplete()
        {
            StartCoroutine(ToggleLapTriggers(halfLap: true, finishLap: true));
        }
        void OnLapFinished()
        {
            //StartCoroutine(ToggleLapTriggers(halfLap: true, finishLap: false));
        }
        void OnGameFinish()
        {
            //Debug.Log("<color=orange> Game Finished </color>");

        }
        void ShowFinishCinematics()
        {
            int rand = Random.Range(10, 100);
            if (rand % 2 == 0)
            {
                carCameraController.useFixedCameraMode = true;
                carCameraController.cameraMode = RCC_Camera.CameraMode.FIXED;
            }
            else
            {
                carCameraController.useCinematicCameraMode = true;
                carCameraController.cameraMode = RCC_Camera.CameraMode.CINEMATIC;
            }

            Invoke(nameof(RaiseGameFinishEvent), 8);
        }
        void RaiseGameFinishEvent()
        {
            hasGameEnded = true;
            carCameraController.cameraMode = RCC_Camera.CameraMode.TPS;
            EventManager.OnShowResult.Invoke();
        }
        IEnumerator ToggleLapTriggers(bool halfLap, bool finishLap)
        {
            yield return new WaitForSeconds(2f);
            midLapTriggerZone.isTrigger = halfLap;
            lapFinishTriggerZone.isTrigger = finishLap;
        }


    }

}