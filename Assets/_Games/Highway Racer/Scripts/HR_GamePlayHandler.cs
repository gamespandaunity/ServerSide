//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------
using CarRace;

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mirror;

#if PHOTON_UNITY_NETWORKING
//using Photon;


#endif

/// <summary>
/// Gameplay management. Spawns player vehicle, sets volume, set mods, listens player events.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Gameplay/HR Gameplay Handler")]
public class HR_GamePlayHandler : MonoBehaviour
{

    #region SINGLETON PATTERN
    public static HR_GamePlayHandler _instance;
    public static HR_GamePlayHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<HR_GamePlayHandler>();
            }

            return _instance;
        }
    }
    #endregion

    [Header("Time Of The Scene")]
    public DayOrNight dayOrNight;
    public enum DayOrNight { Day, Night }

    [Header("Current Mode")]
    internal Mode mode;
    internal enum Mode { OneWay, TwoWay, TimeAttack, Bomb }

    [Header("Spawn Location Of The Cars")]
    public Transform spawnLocation;

    public HR_PlayerHandler player;
    public HR_PlayerHandler player2;

    public float overtakingDistance = 0f;
    public float maxDistance = 100f;

    private int selectedCarIndex = 0;
    private int selectedModeIndex = 0;

    public bool gameStarted = false;
    private bool paused = false;
    private readonly float minimumSpeed = 20f;

    public delegate void onCountDownStarted();
    public static event onCountDownStarted OnCountDownStarted;

    public delegate void onRaceStarted();
    public static event onRaceStarted OnRaceStarted;

    public delegate void onPlayerSpawned(HR_PlayerHandler player);
    public static event onPlayerSpawned OnPlayerSpawned;

    public delegate void onPlayerDied(HR_PlayerHandler player, int[] scores);
    public static event onPlayerDied OnPlayerDied;

    public delegate void onPaused();
    public static event onPaused OnPaused;

    public delegate void onResumed();
    public static event onResumed OnResumed;

    internal AudioSource gameplaySoundtrack;
    public static bool isCarMultiplayer;
    [SerializeField] private GameObject RaceTrack;

    private void Awake()
    {
        RaceTrack.SetActive(true);
        RCC.SetMobileController(RCC_Settings.MobileController.TouchScreen);
        //  Make sure time scale is 1. We are setting volume to 0, we'll be increase it smoothly in update method.
        Time.timeScale = 1f;
        AudioListener.volume = 0f;
        AudioListener.pause = false;
        gameStarted = false;

        //  Creating soundtrack.
        if (HR_HighwayRacerProperties.Instance.gameplayClips != null && HR_HighwayRacerProperties.Instance.gameplayClips.Length > 0)
        {

            gameplaySoundtrack = HR_CreateAudioSource.NewAudioSource(gameObject, "GamePlay Soundtrack", 0f, 0f, .35f, HR_HighwayRacerProperties.Instance.gameplayClips[UnityEngine.Random.Range(0, HR_HighwayRacerProperties.Instance.gameplayClips.Length)], true, true, false);
            gameplaySoundtrack.volume = PlayerPrefs.GetFloat("MusicVolume", .35f);
            gameplaySoundtrack.ignoreListenerPause = true;

        }

        //  Getting selected player car index and mode index.
        selectedCarIndex = PlayerPrefs.GetInt("SelectedPlayerCarIndex");
        selectedModeIndex = PlayerPrefs.GetInt("SelectedModeIndex");

        //  Setting proper mode.
        switch (selectedModeIndex)
        {

            case 0:
                mode = Mode.OneWay;
                break;
            case 1:
                mode = Mode.TwoWay;
                break;
            case 2:
                mode = Mode.TimeAttack;
                break;
            case 3:
                mode = Mode.Bomb;
                break;

        }

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON

        // if (PlayerPrefs.GetInt("Multiplayer", 0) == 1) {

        //   //  if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom) {

        //         Debug.LogError("Not connected to the server, or not in a room yet. Returning to the main menu...");
        //         SceneManager.LoadScene("Home");
        //         return;

        //   //  }

        //     HR_FixFloatingOrigin fixFloatingOrigin = FindObjectOfType<HR_FixFloatingOrigin>();

        //     if (fixFloatingOrigin)
        //         Destroy(fixFloatingOrigin);

        // }

#endif

    }

    private void Start()
    {
        StartCoroutine(BrightenNightScene());
        StartCoroutine(SpawnCar());
        {

            if (PlayerPrefs.GetInt("Multiplayer", 0) == 0)
            {
                StartCoroutine(StartRaceDelayed());
            }
            else
            {
                //if (PhotonNetwork.IsConnected)
                //{
                //    ConnectAndJoin.RoomName = PhotonNetwork.CurrentRoom.Name;         //Photon Removal
                //    ConnectAndJoin.gameObject.SetActive(true);
                //}
            }

        }
    }
    #region Night scene brightness

    // QA 29 Oct: "game screen is very dark, colours dull". The online race runs in HighwayNight: its two
    // directional lights are 0.5 and 0.1, the ambient comes from a dark night skybox, and the camera renders
    // without post-processing (the HR_Camera instance drops its URP camera data), so nothing lifts it.
    // Brighten it at runtime - here rather than in the scene so it ships with a code update and is easy to tune:
    //  - ambient switched to a brighter trilight (lights the cars and all non-lightmapped objects),
    //  - directional lights doubled (capped), so the road and cars get a real key light,
    //  - a global URP volume adds exposure and saturation (the only lever that also lifts the baked lightmaps),
    //    and post-processing is turned on for the game cameras so the volume is applied.
    // Only the night scene; skipped on the headless server.
    private const float NightLightMultiplier = 2f;
    private const float NightLightMaxIntensity = 1.2f;
    private const float NightPostExposure = 0.6f;    // EV, about 1.5x brighter
    private const float NightSaturation = 20f;       // -100..100
    private const float NightContrast = 8f;          // -100..100
    private static readonly Color NightAmbientSky = new Color(0.50f, 0.55f, 0.65f);
    private static readonly Color NightAmbientEquator = new Color(0.40f, 0.42f, 0.46f);
    private static readonly Color NightAmbientGround = new Color(0.22f, 0.21f, 0.19f);

    private bool nightSceneBrightened;

    private IEnumerator BrightenNightScene()
    {
        if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            yield break;
        if (gameObject.scene.name.IndexOf("Night", StringComparison.OrdinalIgnoreCase) < 0)
            yield break;

        if (!nightSceneBrightened)
        {
            nightSceneBrightened = true;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = NightAmbientSky;
            RenderSettings.ambientEquatorColor = NightAmbientEquator;
            RenderSettings.ambientGroundColor = NightAmbientGround;

            foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (sceneLight.type == LightType.Directional && sceneLight.isActiveAndEnabled)
                    sceneLight.intensity = Mathf.Min(sceneLight.intensity * NightLightMultiplier, NightLightMaxIntensity);
            }

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(NightPostExposure);
            colorAdjustments.saturation.Override(NightSaturation);
            colorAdjustments.contrast.Override(NightContrast);

            GameObject volumeObject = new GameObject("HR_NightBrightnessVolume");
            SceneManager.MoveGameObjectToScene(volumeObject, gameObject.scene);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
        }

        // The race camera can be (re)enabled after Start - car spawn, reconnect - so enable post-processing
        // a few times over the first seconds.
        for (int i = 0; i < 5; i++)
        {
            foreach (Camera cam in Camera.allCameras)
            {
                if (cam == null || cam.targetTexture != null) // leave the minimap / render-texture cameras alone
                    continue;
                UniversalAdditionalCameraData cameraData = cam.GetUniversalAdditionalCameraData();
                if (cameraData != null && cameraData.renderType == CameraRenderType.Base)
                    cameraData.renderPostProcessing = true;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    #endregion

    //Photon Removal  public ConnectAndJoin ConnectAndJoin;
    private void Update()
    {

        //  Adjusting volume smoothly.
        float targetVolume = 1f;

        if (AudioListener.volume < targetVolume && !paused && Time.timeSinceLevelLoad > .5f)
        {

            if (AudioListener.volume < targetVolume)
            {

                targetVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
                AudioListener.volume = Mathf.MoveTowards(AudioListener.volume, targetVolume, Time.deltaTime);

            }

        }

        if (PlayerPrefs.GetInt("Multiplayer", 0) == 1)
            CheckNetworkPlayers();

    }


    private void CheckNetworkPlayers()
    {

        if (!gameStarted)
            return;

        if (!player || !player2)
            return;

        overtakingDistance = player.transform.position.z - player2.transform.position.z;
        player.distanceToNextPlayer = overtakingDistance;

        //  if (overtakingDistance > maxDistance)
        //    HR_PhotonHandler.Instance.NetworkPlayerWon(player.GetComponent<PhotonView>());

    }

    /// <summary>
    /// Spawning player car.
    /// </summary>
    private IEnumerator SpawnCar()
    {

        bool isMultiplayer = PlayerPrefs.GetInt("Multiplayer", 0) == 0 ? false : true;
        isCarMultiplayer = isMultiplayer;
        if (NetworkClient.active)
        {
            isMultiplayer = true;
        }
        else if (NetworkServer.active)
        {
            isMultiplayer = true;
        }
        else
        {
            isMultiplayer = false;
        }
        // isMultiplayer=true;
        isMultiplayer.Show("is Multiplayer");
        if (!isMultiplayer)
        {
            yield return new WaitForEndOfFrame();
            player = (RCC.SpawnRCC(HR_PlayerCars.Instance.cars[selectedCarIndex].playerCar.GetComponent<RCC_CarControllerV3>(), spawnLocation.position, spawnLocation.rotation, true, false, true)).GetComponent<HR_PlayerHandler>();
            player.canCrash = false;
            player.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, minimumSpeed / 1.75f);
            RCC_Customization.LoadStats(player.GetComponent<RCC_CarControllerV3>());
            StartCoroutine(CheckDayTime());

        }
        else
        {
            //yield return new WaitUntil(()=>PhotonNetwork.CurrentRoom.PlayerCount==2);

            //    PhotonNetwork.LocalPlayer.ActorNumber.Show("is Multiplayer");

            //    if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            //        spawnLocation.transform.position += Vector3.right * 1.25f;
            //    else
            //        spawnLocation.transform.position -= Vector3.right * 1.25f;
            //    //
            //    player = PhotonNetwork.Instantiate("Photon Player Vehicles/" + HR_PlayerCars.Instance.cars[PhotonNetwork.IsMasterClient ? 0 : 1].playerCar.name, spawnLocation.transform.position, spawnLocation.rotation).GetComponent<HR_PlayerHandler>();

            //    player.canCrash = false;
            //    RegisterCarWithProgressUI(player.gameObject);
            //    RCC.RegisterPlayerVehicle(player.GetComponent<RCC_CarControllerV3>(), false, true);
            //    player.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, minimumSpeed / 1.75f);
            //    RCC_Customization.LoadStats(player.GetComponent<RCC_CarControllerV3>());
            //    StartCoroutine(CheckDayTime());
            //    HR_PhotonHandler.Instance.NetworkPlayerSpawned(player.gameObject.GetPhotonView());

        }

        //	Listening event when player spawned.
        if (OnPlayerSpawned != null)
            OnPlayerSpawned(player);

    }
    void RegisterCarWithProgressUI(GameObject spawnedCar)
    {
        // Option 1: If your car has CarController component
        CarController carController = spawnedCar.GetComponent<CarController>();
        if (carController != null && RaceProgressUI.Instance != null)
        {
            //Photon Removal    RaceProgressUI.Instance.RegisterCar(carController);
            Debug.Log($"Registered car with progress UI: {spawnedCar.name}");
            return;
        }

        // Option 2: If you don't have CarController, create a simple wrapper
        CarController newCarController = spawnedCar.GetComponent<CarController>();
        if (newCarController == null)
        {
            //Photon Removal newCarController = spawnedCar.AddComponent<CarController>();
        }

        if (RaceProgressUI.Instance != null)
        {
            //Photon Removal   RaceProgressUI.Instance.RegisterCar(newCarController);
            Debug.Log($"Added CarController and registered: {spawnedCar.name}");
        }
        else
        {
            Debug.LogError("RaceProgressUI.Instance is null! Make sure RaceProgressUI is in the scene.");
        }
    }

    /// <summary>
    /// Countdown before the game.
    /// </summary>
    /// <returns></returns>
    public IEnumerator StartRaceDelayed()
    {

        if (OnCountDownStarted != null)
            OnCountDownStarted();

        yield return new WaitForSeconds(4);

        gameStarted = true;
        RCC.SetControl(player.GetComponent<RCC_CarControllerV3>(), true);

        if (OnRaceStarted != null)
            OnRaceStarted();

    }
   public GameObject gamePlayButton;
    public IEnumerator ServerStartRaceDelayed()
    {
        //gameStarted = true;
        //RCC.SetControl(player.GetComponent<RCC_CarControllerV3>(), true);
        if (OnCountDownStarted != null)
            OnCountDownStarted();
        gamePlayButton.SetActive(false);
        Debug.Log("GameplayerButton"+gamePlayButton);
        yield return new WaitForSeconds(4);
         gamePlayButton.SetActive(true);
         Debug.Log("GameplayerButton"+gamePlayButton);
        gameStarted = true;
        RCC.SetControl(player.GetComponent<RCC_CarControllerV3>(), true);


        if (OnRaceStarted != null)
            OnRaceStarted();

    }

    /// <summary>
    /// Reconnect ke baad race pehle se chal rahi hoti hai - koi countdown nahi. Server sirf
    /// rejoin karne wale client ko ye bhejta hai (HR_NetworkManager.Target_ResumeRace), taake
    /// wohi state set ho jaye jo ServerStartRaceDelayed ke aakhir me hoti hai. Warna us client
    /// par gameStarted false reh jata tha aur gameplay button wapas nahi aata tha.
    /// </summary>
    public void ResumeRaceAfterReconnect()
    {
        gameStarted = true;

        if (gamePlayButton != null)
            gamePlayButton.SetActive(true);

        if (player != null)
        {
            RCC_CarControllerV3 controller = player.GetComponent<RCC_CarControllerV3>();

            if (controller != null)
                RCC.SetControl(controller, true);
        }

        if (OnRaceStarted != null)
            OnRaceStarted();
    }

    /// <summary>
    /// Checking time.
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckDayTime()
    {

        yield return new WaitForFixedUpdate();

        if (dayOrNight == DayOrNight.Night)
            player.GetComponent<RCC_CarControllerV3>().lowBeamHeadLightsOn = true;
        else
            player.GetComponent<RCC_CarControllerV3>().lowBeamHeadLightsOn = false;

    }

    /// <summary>
    /// When player crashed.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="scores"></param>
    public void CrashedPlayer(HR_PlayerHandler player, int[] scores)
    {

        gameStarted = false;

        if (OnPlayerDied != null)
            OnPlayerDied(player, scores);

        StartCoroutine(FinishRaceDelayed(1f));

    }

    /// <summary>
    /// Finished the game after the crash and saves the highscore.
    /// </summary>
    /// <param name="delayTime"></param>
    /// <returns></returns>
    public IEnumerator FinishRaceDelayed(float delayTime)
    {

        yield return new WaitForSecondsRealtime(delayTime);
        FinishRace();

    }

    /// <summary>
    /// Finishes the game after the crash and saves the highscore instantly.
    /// </summary>
    /// <param name="delayTime"></param>
    /// <returns></returns>
    public void FinishRace()
    {

        OnPaused();

        switch (mode)
        {

            case Mode.OneWay:
                PlayerPrefs.SetInt("bestScoreOneWay", (int)player.GetComponent<HR_PlayerHandler>().score);
                break;
            case Mode.TwoWay:
                PlayerPrefs.SetInt("bestScoreTwoWay", (int)player.GetComponent<HR_PlayerHandler>().score);
                break;
            case Mode.TimeAttack:
                PlayerPrefs.SetInt("bestScoreTimeAttack", (int)player.GetComponent<HR_PlayerHandler>().score);
                break;
            case Mode.Bomb:
                PlayerPrefs.SetInt("bestScoreBomb", (int)player.GetComponent<HR_PlayerHandler>().score);
                break;

        }

    }

    /// <summary>
    /// Main menu.
    /// </summary>
    public void MainMenu()
    {

        if (PlayerPrefs.GetInt("Multiplayer", 0) == 0)
        {

            SceneManager.LoadScene("Home");

        }
        else
        {

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON
            //    PhotonNetwork.LeaveRoom();
#endif

        }

    }

    /// <summary>
    /// Restart the game.
    /// </summary>
    public void RestartGame()
    {

        if (PlayerPrefs.GetInt("Multiplayer", 0) == 1)
            return;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    /// <summary>
    /// Pause or resume the game.
    /// </summary>
    public void Paused()
    {

        paused = !paused;

        if (paused)
            OnPaused();
        else
            OnResumed();

    }

}
