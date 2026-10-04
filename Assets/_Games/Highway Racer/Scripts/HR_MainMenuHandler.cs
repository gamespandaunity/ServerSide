//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using CarRace;
using Mirror;

/// <summary>
/// Management of the main menu events. Creates and spawns vehicles, switches them, enables/disables menus.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Main Menu/HR Main Menu Handler")]
public class HR_MainMenuHandler : MonoBehaviour {

    #region SINGLETON PATTERN
    private static HR_MainMenuHandler _instance;
    public static HR_MainMenuHandler Instance {
        get {
            if (_instance == null) {
                _instance = GameObject.FindObjectOfType<HR_MainMenuHandler>();
            }

            return _instance;
        }
    }
    #endregion

    [Header("Spawn Location Of The Cars")]
    public Transform carSpawnLocation;      //  Spawn location.

    private GameObject[] createdCars;       //	All created cars will be stored.
    public RCC_CarControllerV3 currentCar;      //	Current selected car.
    public HR_ModApplier currentApplier;        //  Current mod applier of the selected car.

    internal int carIndex = 0;      //	Current car index.




    internal AudioSource mainMenuSoundtrack;

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON
    private GameObject multiplayerMenu;
#endif

    private void Awake() {

        if (Utils.IsHeadless()) return;
        // Setting time scale, volume, unpause, and target frame rate.
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.pause = false;
        Application.targetFrameRate = 60;

        //	Creating soundtracks for the main menu.
        if (HR_HighwayRacerProperties.Instance.mainMenuClips != null && HR_HighwayRacerProperties.Instance.mainMenuClips.Length > 0) {

            mainMenuSoundtrack = HR_CreateAudioSource.NewAudioSource(gameObject, "Main Menu Soundtrack", 0f, 0f, PlayerPrefs.GetFloat("MusicVolume", .35f), HR_HighwayRacerProperties.Instance.mainMenuClips[UnityEngine.Random.Range(0, HR_HighwayRacerProperties.Instance.mainMenuClips.Length)], true, true, false);
            mainMenuSoundtrack.ignoreListenerPause = true;

        }

        //	If test mode enabled, add 1000000 coins to the balance.
        if (HR_HighwayRacerProperties.Instance._1MMoneyForTesting)
            PlayerPrefs.SetInt("Currency", 1000000);

        //	Getting last selected car index.
        carIndex = PlayerPrefs.GetInt("SelectedPlayerCarIndex", 0);

        CreateCars();   //	Creating all selectable cars at once.
        SpawnCar();     //	Spawning only target car (carIndex).

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON

      //  HR_PhotonLobbyManager multiplayerManager = FindObjectOfType<HR_PhotonLobbyManager>(true);

       // if (multiplayerManager)
       //     multiplayerMenu = multiplayerManager.gameObject;

#endif

    }

    private void Update() {

        //	Displaying currency.

        //	If loading, set value of the loading slider.

    }

    /// <summary>
    /// Creating all spawnable cars at once.
    /// </summary>
    private void CreateCars() {

        //	Creating a new array.
        createdCars = new GameObject[HR_PlayerCars.Instance.cars.Length];

        //	Setting array elements.
        for (int i = 0; i < createdCars.Length; i++) {

            createdCars[i] = (RCC.SpawnRCC(HR_PlayerCars.Instance.cars[i].playerCar.GetComponent<RCC_CarControllerV3>(), carSpawnLocation.position, carSpawnLocation.rotation, false, false, false)).gameObject;
            createdCars[i].GetComponent<RCC_CarControllerV3>().lowBeamHeadLightsOn = true;
            createdCars[i].SetActive(false);

        }

    }

    /// <summary>
    /// Spawns target car (carIndex).
    /// </summary>
    private void SpawnCar() {

        //	If price of the car is 0, or unlocked, save it as owned car.
        if (HR_PlayerCars.Instance.cars[carIndex].price <= 0 || HR_PlayerCars.Instance.cars[carIndex].unlocked)
            HR_API.UnlockVehice(carIndex);

        //	If current spawned car is owned, enable buy button, disable select button. Do opposite otherwise.
        if (HR_API.OwnedVehicle(carIndex)) {


        } else {


        }

        //	Disabling all cars at once. And then enabling only target car (carIndex). And make sure spawned cars are always at spawn point.
        for (int i = 0; i < createdCars.Length; i++) {

            if (createdCars[i].activeInHierarchy) {

                createdCars[i].SetActive(false);
                createdCars[i].transform.position = carSpawnLocation.position;
                createdCars[i].transform.rotation = carSpawnLocation.rotation;

            }

        }

        //	Enabling only target car (carIndex).
        createdCars[carIndex].SetActive(true);

        //	Setting current car.
        currentCar = createdCars[carIndex].GetComponent<RCC_CarControllerV3>();
        currentApplier = currentCar.GetComponent<HR_ModApplier>();

        //	Displaying car name text.


    }

    /// <summary>
    /// Purchases current car.
    /// </summary>
    public void BuyCar() {

        // If we own the car, don't consume currency.
        if (HR_API.OwnedVehicle(carIndex)) {

            Debug.LogError("Car is already owned!");
            return;

        }

        //	If currency is enough, save it and consume currency. Otherwise display the informer.
        if (HR_API.GetCurrency() >= HR_PlayerCars.Instance.cars[carIndex].price) {

            HR_API.ConsumeCurrency(HR_PlayerCars.Instance.cars[carIndex].price);

        } else {

            HR_UIInfoDisplayer.Instance.ShowInfo("Not Enough Coins", "You have to earn " + (HR_PlayerCars.Instance.cars[carIndex].price - HR_API.GetCurrency()).ToString() + " more coins to buy this wheel", HR_UIInfoDisplayer.InfoType.NotEnoughMoney);
            return;

        }

        //	Saving the car.
        HR_API.UnlockVehice(carIndex);

        //	And spawning again to check modders of the car.
        SpawnCar();

    }

    /// <summary>
    /// Selects the current car with carIndex.
    /// </summary>
    public void SelectCar() {

        PlayerPrefs.SetInt("SelectedPlayerCarIndex", carIndex);

    }

    /// <summary>
    /// Switch to next car.
    /// </summary>
    public void PositiveCarIndex() {

        carIndex++;

        if (carIndex >= createdCars.Length)
            carIndex = 0;

        SpawnCar();

    }

    /// <summary>
    /// Switch to previous car.
    /// </summary>
    public void NegativeCarIndex() {

        carIndex--;

        if (carIndex < 0)
            carIndex = createdCars.Length - 1;

        SpawnCar();

    }

    /// <summary>
    /// Enables target menu and disables all other menus.
    /// </summary>
    /// <param name="activeMenu"></param>
    public void EnableMenu(GameObject activeMenu) {


        activeMenu.SetActive(true);


    }

    /// <summary>
    /// Selects the scene with int.
    /// </summary>
    /// <param name="levelIndex"></param>
    public void SelectScene(string levelName) {

        PlayerPrefs.SetString("SelectedScene", levelName);

    }

    /// <summary>
    /// Selects the mode with int.
    /// </summary>
    /// <param name="_modeIndex"></param>
    public void SelectMode(int _modeIndex) {

        //	Saving the selected mode, and enabling scene selection menu.
        PlayerPrefs.SetInt("SelectedModeIndex", _modeIndex);

    }

    /// <summary>
    /// Selects the scene with int.
    /// </summary>
    /// <param name="levelIndex"></param>
    public void StartRace() {

        SelectCar();

    }

    /// <summary>
    /// Displays best scores of all four modes.
    /// </summary>
    private void BestScores() {

        int[] scores = HR_API.GetHighScores();


    }

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON

    public void SetMultiplayer(bool state) {

        PlayerPrefs.SetInt("Multiplayer", state ? 1 : 0);

        if (state) {

          //  HR_PhotonHandler photonHandler = HR_PhotonHandler.Instance;

         //   if (!photonHandler)
                Instantiate(Resources.Load("HR_Photon Handler", typeof(GameObject)));

            if (multiplayerMenu)
                EnableMenu(multiplayerMenu);

        } else {

        //    HR_PhotonHandler photonHandler = HR_PhotonHandler.Instance;

         //   if (photonHandler)
         //       Destroy(photonHandler.gameObject);

        }

    }

#endif

    /// <summary>
    /// Quits the game.
    /// </summary>
    public void QuitGame() {

        Application.Quit();

    }

}
