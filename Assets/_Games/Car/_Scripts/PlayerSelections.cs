namespace CarRace 
{
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Michsky.LSS;
 
using System.Collections;
/// <summary>
/// This Scripts Controls  user every  Selection and Data
/// </summary>
public class PlayerSelections : MonoBehaviour
{
    [SerializeField] int initialCoins;
    public static PlayerSelections instance;
    public static int GameMode = 0; // 0 = offline , 1 = Online
    [HideInInspector] private int currentSelectedCar = 0;
    [HideInInspector] public int selectedCar;
    [HideInInspector] public int selectedColor = 0;
    [HideInInspector] public int selectedMap = 0;
    [HideInInspector] public int selectedOpponents = 0;
    [HideInInspector] public int selectedLapCount = 2;
    [HideInInspector] public int SelecteddifficultyLevel = 1; //1 : Easy, 2: Medium , 3: Hard

    [Header("Audio Manager Reference")]
    [SerializeField] AudioManager audioManager;
    [SerializeField] LoadingScreenManager loadingScreenManager;

    [Header("Player Name Pop Up")]
    [SerializeField] GameObject userNamePopUp;
    public TMP_InputField userNameInput;
    [SerializeField] GameObject errorPopUp;
    [SerializeField] TextMeshProUGUI errorInfoText;

    [Space(2), Header("Car Objects")]
    [SerializeField] GameObject[] cars;
    [SerializeField] Car[] carData;

    [Space(3), Header("Ads Related UI")]
    [SerializeField] GameObject rewardPopUp;
    [SerializeField] Button showAdButton;

    [Space(3), Header("In App Purchasing")]
    [SerializeField] Button removeAdsButton1;
    [SerializeField] Button removeAdsButton2;
    [SerializeField] Button unlockAllCarsButton;
    [SerializeField] Button unlockAllMapsButton;
    [SerializeField] Button buyCoinsButton1;
    [SerializeField] Button buyCoinsButton2;
    [SerializeField] Button buyCoinsButton3;

    [Space(3), Header("Privacy Policy")]
    [SerializeField] GameObject privacyPopUp;
    [SerializeField] Button privacyAcceptButton;
    [SerializeField] Button privacyVisitButton;
    [SerializeField] Button moreGamesButton;


    #region Level Setting UI

    [Header("Game Settings Screen"), Space(3)]
    [SerializeField] TextMeshProUGUI opponentsCountText;
    [SerializeField] TextMeshProUGUI lapsCountText;
    [SerializeField] TextMeshProUGUI difficultyLevelText;
    [SerializeField] Image selectedMapImage;
    [SerializeField] Sprite[] mapSprites;
    [SerializeField] Button playButton;

    [Header("Level Selection Screen"), Space(3)]
    [SerializeField] int[] mapPrices;
    [SerializeField] GameObject[] levelButtons;
    [SerializeField] GameObject[] levelLockImages;
    [SerializeField] GameObject levelSelectionPlayButton;
    [SerializeField] Button unlockLevelButton;
    [SerializeField] TextMeshProUGUI levelPriceText;

    #endregion

    #region Garage UI
    [Space(2), Header("Car Specs UI")]
    [SerializeField] int speedValueMax;
    [SerializeField] int engineValueMax;
    [SerializeField] int controlValueMax;
    [SerializeField] Image engineValue;
    [SerializeField] Image maxSpeedValue;
    [SerializeField] Image controlValue;
    [SerializeField] TextMeshProUGUI engineValueText;
    [SerializeField] TextMeshProUGUI maxSpeedValueText;
    [SerializeField] TextMeshProUGUI controlValueText;
    [SerializeField] TextMeshProUGUI carNameText;
    [SerializeField] TextMeshProUGUI carNumbertext;
    [SerializeField] GameObject selectCarButton;
    [SerializeField] GameObject buyButton;
    [SerializeField] GameObject priceObject;
    [SerializeField] TextMeshProUGUI carPriceText;
    [SerializeField] GameObject buyCarPopUp;
    [SerializeField] TextMeshProUGUI buyPopUpcarPriceText;
    [SerializeField] GameObject succesfullPurchasePopUp;
    [SerializeField] GameObject failedPurchasePopUp;

    [SerializeField] GameObject[] changeColorButtons;
    [SerializeField] Image[] changeColorImages;

    #endregion

    #region UI to Populate
    [Space(2), Header("UI Inputs To Populate")]
    [SerializeField] TextMeshProUGUI[] textCoins;
    [SerializeField] TextMeshProUGUI[] textUserName;
    [SerializeField] Slider volumeSlider;
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] Slider musicSlider;
    public AudioSource BGM;
    [SerializeField] TextMeshProUGUI sensitivityValue;

    [Header("SettingsMenus")]
    [SerializeField] GameObject settingsMenu;

    [Header("Graphics Toggles")]
    [SerializeField] Toggle graphicsLowToggle;
    [SerializeField] Toggle graphicsMediumToggle;
    [SerializeField] Toggle graphicsHighToggle;

    [Header("Control Type Toggles")]
    [SerializeField] Toggle controlButtonsToggle;
    [SerializeField] Toggle controlJoystickToggle;
    [SerializeField] Toggle controlSteeringToggle;
    [SerializeField] Toggle controlTiltToggle;

    [Header("Vehicle Behavior Toggles")]
    [SerializeField] Toggle behaviorRacingToggle;
    [SerializeField] Toggle behaviorDriftToggle;
    [SerializeField] Toggle behaviorArcadeToggle;

    [Header("Damage Toggle")]
    [SerializeField] Toggle damageEnableToggle;
    [SerializeField] Toggle damageDisbleToggle;

    [Header("Reverse Camera Toggle")]
    [SerializeField] Toggle reverseCamEnableToggle;
    [SerializeField] Toggle reverseCamDisableToggle;

    #endregion

    #region Unity Methods

    void Awake()
    {
        instance = this;
        selectedLapCount = 2;
        SelecteddifficultyLevel = 1;
        AddInitialValues();
        PopulateUSerData();
        currentSelectedCar = PlayerPrefs.GetInt(PPConst.SavedCar);
        selectedCar = currentSelectedCar;
        ActicvateCar(currentSelectedCar);
        for (int i = 0; i < levelButtons.Length; i++)
            levelLockImages[i].SetActive(!UserData.CheckLevel(i));
        SelectMap(0);
        playButton.onClick.AddListener(PlayGame);
        unlockLevelButton.onClick.AddListener(UnlockLevel);
        showAdButton.onClick.AddListener(OnRewardButtonClicked);
        privacyAcceptButton.onClick.AddListener(OnAcceptPolicyClicked);
        privacyVisitButton.onClick.AddListener(OnPrivacyPolicyClicked);
        moreGamesButton.onClick.AddListener(OnMoreGameClicked);
        SetupIAPButtons();
        ShowFirstTimePopUps();
    }
    private void OnEnable()
    {
        EventManager.OnRewardGiven += GiveReward;
        EventManager.OnPurachaseCompleted_Consumable += BuyCoins;
        EventManager.OnPurachaseCompleted_RemoveAds += RemoveAds;
        EventManager.OnPurachaseCompleted_UnlockCars += UnlockAllCars;
        EventManager.OnPurachaseCompleted_UnlockMaps += UnlockAllLevels;
        EventManager.OnToggleIAPButtons += ToggleIAPButtons;
        EventManager.OnUpdateUserCoins += UpdateCoinsUI;
    }
    private void OnDisable()
    {
        EventManager.OnRewardGiven -= GiveReward;
        EventManager.OnPurachaseCompleted_Consumable -= BuyCoins;
        EventManager.OnPurachaseCompleted_RemoveAds -= RemoveAds;
        EventManager.OnPurachaseCompleted_UnlockCars -= UnlockAllCars;
        EventManager.OnPurachaseCompleted_UnlockMaps -= UnlockAllLevels;
        EventManager.OnToggleIAPButtons -= ToggleIAPButtons;
        EventManager.OnUpdateUserCoins -= UpdateCoinsUI;

    }
    #endregion

    #region UI Mehtods
    public void SaveUserName()
    {
        string userName = userNameInput.text;
        if (userName.Length < 5)
        {
            errorInfoText.text = "Name Must Be At leat 5 characters";
            errorPopUp.SetActive(true);
            audioManager.PlaySound("Error");
        }
        else
        {
            userNamePopUp.SetActive(false);
            foreach (TextMeshProUGUI un in textUserName)
            {
                un.text = userNameInput.text;
            }
            PlayerPrefs.SetString(PPConst.UserName, userNameInput.text);
            CheckIfFirstTime();
        }
        cars[currentSelectedCar].GetComponent<CarModification>().SetPlayerNameInNumerPlate(PlayerPrefs.GetString(PPConst.UserName));
        }
    public void ChangeCar(int x)
    {
        currentSelectedCar += x;
        currentSelectedCar = currentSelectedCar >= cars.Length ? 0 : currentSelectedCar < 0 ? cars.Length - 1 : currentSelectedCar;
        ActicvateCar(currentSelectedCar);


    }
    public void SelectCurrentCar(int carNum)
    {
        if (carNum == currentSelectedCar)
            return;
        currentSelectedCar = carNum;
        ActicvateCar(currentSelectedCar);
    }
    public void SelectCar()
    {
        PlayerPrefs.SetInt(PPConst.SavedCar, currentSelectedCar);
        selectedCar = currentSelectedCar;
    }
    public void CancelSelection()
    {
        if (selectedCar == currentSelectedCar)
            return;
        currentSelectedCar = selectedCar;
        ActicvateCar(selectedCar);
    }
    public void ChangeVolume(Slider slider)
    {
        AudioListener.volume = slider.value;
        PlayerPrefs.SetFloat(PPConst.Volume, slider.value);
    }
    public void ChangeMusic(Slider slider)
    {
        BGM.volume = slider.value;
        PlayerPrefs.SetFloat(PPConst.Music, slider.value);
    }
    public void ChangeSensitivity(Slider slider)
    {
        sensitivityValue.text = System.Math.Round(slider.value, 2).ToString();
        PlayerPrefs.SetFloat(PPConst.SensitivityVal, slider.value);
        RCC_Settings.Instance.gyroSensitivity = slider.value;
    }
    public void ChangeQualitySettings(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt(PPConst.QualityLevel, qualityIndex);
    }
    public void ChangeControlType(int controlIndex)
    {

        switch (controlIndex)
        {
            case 0:
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.TouchScreen;
                break;
            case 1:
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.Joystick;
                break;
            case 2:
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.SteeringWheel;
                break;
            case 3:
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.Gyro;
                break;
            default:
                break;
        }
        PlayerPrefs.SetInt(PPConst.ControlType, controlIndex);
    }
    public void ChangeDrivingMode(int modeIndex)
    {
        //1 Racing, 3 Arcade, 2 Drift, 0 Custom
        if (modeIndex == 0)
        {
            RCC_Settings.Instance.overrideBehavior = false;
        }
        else
        {
            RCC_Settings.Instance.overrideBehavior = true;
            RCC_Settings.Instance.behaviorSelectedIndex = modeIndex;
        }
        PlayerPrefs.SetInt(PPConst.VehicleBehavior, modeIndex);

    }
    public void ToggleVehicleDamage(int isEnabled)
    {
        PlayerPrefs.SetInt(PPConst.Damage, isEnabled); // 0 for False, 1 for True
    }
    public void ToggleReverseCamera(int useReverseCamera)
    {
        PlayerPrefs.SetInt(PPConst.ReverseCamera, useReverseCamera); // 0 for False, 1 for True
    }
    public void SelectMap(int mapIndex)
    {
        selectedMap = mapIndex;
        selectedMapImage.sprite = mapSprites[selectedMap];
        CheckIfLevelLocked(mapIndex);
        //PlayerPrefs.SetInt(PPConst.Map, mapIndex);
    }
    public void BuyCar()
    {
        buyCarPopUp.SetActive(false);
        int price = carData[currentSelectedCar].price;

        if (UserData.CheckUserCoins(price))
        {
            audioManager.PlaySound("Purchase");
            succesfullPurchasePopUp.SetActive(true);
            UserData.BuyCar(currentSelectedCar, price);
            CheckIfCarLocked(currentSelectedCar);
            UpdateCoinsUI();

        }
        else
        {
            audioManager.PlaySound("Error");
            failedPurchasePopUp.SetActive(true);
        }
    }
    public void UnlockLevel()
    {
        int price = mapPrices[selectedMap];

        if (UserData.CheckUserCoins(price))
        {
            audioManager.PlaySound("Purchase");
            UserData.UnlockLevel(selectedMap, price);
            CheckIfLevelLocked(selectedMap);
        }
        else
        {
            audioManager.PlaySound("Error");
            failedPurchasePopUp.SetActive(true);
        }
    }
    public void ChangeCarColor(int colorIndex)
    {
        selectedColor = colorIndex;
        var carMod = cars[currentSelectedCar].GetComponent<CarModification>();
        carMod.ChangeColor(colorIndex);
        carMod.SetPlayerNameInNumerPlate(PlayerPrefs.GetString(PPConst.UserName));
            UserData.SaveCarColor(currentSelectedCar, colorIndex);
    }
    public void ChangeOpponentsCount(int val)
    {
        selectedOpponents += val;
        if (selectedOpponents < 2)
            selectedOpponents = 2;
        else if (selectedOpponents > 5)
            selectedOpponents = 5;

        opponentsCountText.text = selectedOpponents.ToString();
    }
    public void ChangeDifficultyLevel(int val)
    {
        SelecteddifficultyLevel += val;
        if (SelecteddifficultyLevel < 1)
            SelecteddifficultyLevel = 1;
        else if (SelecteddifficultyLevel > 4)
            SelecteddifficultyLevel = 4;

        difficultyLevelText.text = SelecteddifficultyLevel == 1 ? "Easy" : SelecteddifficultyLevel == 2 ? "Medium" : SelecteddifficultyLevel == 3 ? "Hard" : "Random";
    }
    public void ChangeLapsCount(int val)
    {
        selectedLapCount += val;
        if (selectedLapCount < 1)
            selectedLapCount = 1;
        else if (selectedLapCount > 5)
            selectedLapCount = 5;

        lapsCountText.text = selectedLapCount.ToString();
    }
    public void ShowInterstitialAd()
    { //called from UI buttons in the inspector
        if (EventManager.OnCheckAdLoaded_Interstitial.Invoke())
            EventManager.OnShowAd_Interstitial?.Invoke();
    }
    void OnMoreGameClicked()
    {
        Application.OpenURL("https://play.google.com/store/apps/dev?id=7496753345913544169");
    }

    #endregion

    #region Private Methods
    void ShowFirstTimePopUps()
    {
        if (PlayerPrefs.GetInt(PPConst.First, 0) == 0)
        {
            if (!UserData.CheckIfPrivacyAccepted())
                privacyPopUp.SetActive(true);
        }
    }
    void CheckIfFirstTime()
    {
        if (PlayerPrefs.GetInt(PPConst.First, 0) == 0)
        {
            PlayerPrefs.SetInt(PPConst.First, 1);
            //loadingScreenManager.LoadScene("Main Menu");
            //Need To Restart The Game
        }
    }
    void AddInitialValues()
    {

        if (PlayerPrefs.GetInt(PPConst.First, 0) == 0)
        {
            PlayerPrefs.SetString(PPConst.UserName, "Player" + Random.Range(1000, 10000));
            PlayerPrefs.SetInt(PPConst.Coins, initialCoins);
            PlayerPrefs.SetInt(PPConst.SavedCar, 0);
            UserData.UnlockCar(0);  //Unlock First Car
            UserData.UnlockLevel(0);  //Unlock First Level
            PlayerPrefs.SetInt(PPConst.QualityLevel, 1);        // 1 Low
            PlayerPrefs.SetFloat(PPConst.Volume, 0.75f);
            PlayerPrefs.SetInt(PPConst.ControlType, 0);        // 0 Buttons
            PlayerPrefs.SetInt(PPConst.VehicleBehavior, 3);    // 0 Custom, 1 Racing, 3 Arcade, 2 Drift, 
            PlayerPrefs.SetInt(PPConst.Damage, 0);             // 1 enable , 0  disable
            PlayerPrefs.SetInt(PPConst.ReverseCamera, 1);      // 1 enable , 0 disable
            PlayerPrefs.SetFloat(PPConst.SensitivityVal, 2.75f);
            PlayerPrefs.SetFloat(PPConst.Music, 0.65f);
                //Photon Removal  PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "asia";
            }
        }
    void ActicvateCar(int index)
    {
        CheckIfCarLocked(index);
        PopulateCarValues(index);
        foreach (GameObject car in cars)
        {
            car.SetActive(false);
        }
      
        cars[index].transform.localPosition = Vector3.zero;
        cars[index].SetActive(true);
        cars[index].GetComponent<RCC_CarControllerV3>().audioType = RCC_CarControllerV3.AudioType.Off;
        ChangeCarColor(UserData.GetCarColor(index));
        SetCarColorsOnImages();

    }
    void CheckIfCarLocked(int carIndex)
    {
        selectCarButton.SetActive(UserData.CheckCar(carIndex));
        buyButton.SetActive(!UserData.CheckCar(carIndex));
        priceObject.SetActive(!UserData.CheckCar(carIndex));
    }
    void CheckIfLevelLocked(int levelIndex)
    {
        levelSelectionPlayButton.SetActive(UserData.CheckLevel(levelIndex));
        levelLockImages[levelIndex].SetActive(!UserData.CheckLevel(levelIndex));
        unlockLevelButton.gameObject.SetActive(!UserData.CheckLevel(levelIndex));
        levelPriceText.text = "$" + mapPrices[levelIndex];
    }
    void PopulateCarValues(int index)
    {
        Car car = carData[index];
        carNameText.text = car.carName;
        carNumbertext.text = (index + 1) + "/" + cars.Length;
        engineValue.fillAmount = car.engine / engineValueMax;
        maxSpeedValue.fillAmount = car.maxSpeed / speedValueMax;
        controlValue.fillAmount = car.control / controlValueMax;

        engineValueText.text = ((int)((car.engine / engineValueMax) * 100)) + "%";
        maxSpeedValueText.text = ((int)((car.maxSpeed / speedValueMax * 100))) + "%";
        controlValueText.text = ((int)((car.control / controlValueMax * 100))) + "%";
        carPriceText.text = car.price.ToString();
        buyPopUpcarPriceText.text = car.price.ToString();
    }
    void PopulateUSerData()
    {
        userNameInput.text = PlayerPrefs.GetString(PPConst.UserName);
        settingsMenu.SetActive(true);
        //to on toggle first Set Ative all then apply values
        foreach (TextMeshProUGUI userName in textUserName)
        {
            userName.text = PlayerPrefs.GetString(PPConst.UserName);
        }
        UpdateCoinsUI();

         //   volumeSlider.value = 0.75f;// PlayerPrefs.GetFloat(PPConst.Volume);
       // AudioListener.volume = 0.75f;

        sensitivitySlider.value = PlayerPrefs.GetFloat(PPConst.SensitivityVal);
        sensitivityValue.text = System.Math.Round(sensitivitySlider.value, 2).ToString();
        RCC_Settings.Instance.gyroSensitivity = sensitivitySlider.value;

        musicSlider.value = PlayerPrefs.GetFloat(PPConst.Music);
        BGM.volume = PlayerPrefs.GetFloat(PPConst.Music);
        BGM.Play();
        QualitySettings.SetQualityLevel(UserData.CheckQualitySettings());


        GraphicsToggles();
        ControlsToggles();
        BehaviorsToggles();
        DamageToggles();
        ReverseCameraToggles();
        GameSettingsMenu();
        settingsMenu.SetActive(false);

    }
    void GraphicsToggles()
    {
        int val = UserData.CheckQualitySettings();
        if (val == 1)
        {
            graphicsLowToggle.isOn = true;
            //graphicsLowToggle.SetIsOnWithoutNotify(true);
        }
        else if (val == 2)
        {
            graphicsMediumToggle.isOn = true;
            //graphicsMediumToggle.SetIsOnWithoutNotify(true);
        }
        else
        {
            graphicsHighToggle.isOn = true;
            //graphicsHighToggle.SetIsOnWithoutNotify(true);
        }
    }
    void ControlsToggles()
    {
        int val = PlayerPrefs.GetInt(PPConst.ControlType);
        if (val == 0)
        {
            controlButtonsToggle.isOn = true;
            //controlButtonsToggle.SetIsOnWithoutNotify(true);
        }
        else if (val == 1)
        {
            controlJoystickToggle.isOn = true;
            //controlJoystickToggle.SetIsOnWithoutNotify(true);
        }
        else if (val == 2)
        {
            controlSteeringToggle.isOn = true;
            //controlSteeringToggle.SetIsOnWithoutNotify(true);
        }
        else
        {
            controlTiltToggle.isOn = true;
            //controlTiltToggle.SetIsOnWithoutNotify(true);
        }
    }
    void BehaviorsToggles()
    {
        //1 Racing, 3 Arcade, 2 Drift
        int val = PlayerPrefs.GetInt(PPConst.VehicleBehavior);
        if (val == 1)
        {
            behaviorRacingToggle.isOn = true;
            //behaviorRacingToggle.SetIsOnWithoutNotify(true);
        }
        else if (val == 2)
        {
            behaviorDriftToggle.isOn = true;
            //behaviorDriftToggle.SetIsOnWithoutNotify(true);
        }
        else
        {
            behaviorArcadeToggle.isOn = true;
            //behaviorArcadeToggle.SetIsOnWithoutNotify(true);
        }
    }
    void DamageToggles()
    {
        int val = PlayerPrefs.GetInt(PPConst.Damage);
        if (val == 1)
        {
            damageEnableToggle.isOn = true;
            //damageEnableToggle.SetIsOnWithoutNotify(true);
        }
        else
        {
            damageDisbleToggle.isOn = true;
            //damageDisbleToggle.SetIsOnWithoutNotify(true);
        }
    }
    void ReverseCameraToggles()
    {
        int val = PlayerPrefs.GetInt(PPConst.ReverseCamera);
        if (val == 1)
        {
            reverseCamEnableToggle.isOn = true;
            //reverseCamEnableToggle.SetIsOnWithoutNotify(true);
        }
        else
        {
            reverseCamDisableToggle.isOn = true;
            //reverseCamDisableToggle.SetIsOnWithoutNotify(true);
        }
    }
    void SetCarColorsOnImages()
    {
        foreach (GameObject colorChangeButton in changeColorButtons)
            colorChangeButton.SetActive(false);
        int materialNum = cars[currentSelectedCar].GetComponent<CarModification>().GetmaterialNumber();
        for (int i = 0; i < materialNum; i++)
        {
            changeColorButtons[i].SetActive(true);
            changeColorImages[i].color = cars[currentSelectedCar].GetComponent<CarModification>().GetMaterialColor(i);
        }
    }
    void GameSettingsMenu()
    {
        selectedOpponents = 3;
        selectedMap = 0;
        SelecteddifficultyLevel = 1;
        opponentsCountText.text = selectedOpponents.ToString();
        difficultyLevelText.text = "Easy";
        selectedMapImage.sprite = mapSprites[selectedMap];
    }
    void PlayGame()
    {
        GameMode = 0;
        EventManager.OnHideBanner?.Invoke();
        string levelname = GetNameFromIndex(selectedMap + 2);
        loadingScreenManager.LoadScene(levelname);
    }
    string GetNameFromIndex(int BuildIndex)
    {
        string path = SceneUtility.GetScenePathByBuildIndex(BuildIndex);
        int slash = path.LastIndexOf('/');
        string name = path.Substring(slash + 1);
        int dot = name.LastIndexOf('.');
        return name.Substring(0, dot);
    }
    void OnRewardButtonClicked()
    {
        bool isAdReady = EventManager.OnCheckAdLoaded_Rewarded.Invoke();
        if (isAdReady)
        {
            EventManager.OnShowAd_Rewarded?.Invoke();
        }
        else
        {
            errorInfoText.text = "No rewards availble! Come Back Later...";
            errorPopUp.SetActive(true);
        }
    }
    void GiveReward()
    {
        rewardPopUp.SetActive(true);
        UserData.AddCoins(100);
        UpdateCoinsUI();
    }
    void UpdateCoinsUI()
    {

        foreach (TextMeshProUGUI cointText in textCoins)
            cointText.text = PlayerPrefs.GetInt(PPConst.Coins).ToString();
    }

    void SetupIAPButtons()
    {
        buyCoinsButton1.onClick.AddListener(delegate { EventManager.OnPurachaseRequested_Consumable?.Invoke(1); });
        buyCoinsButton2.onClick.AddListener(delegate { EventManager.OnPurachaseRequested_Consumable?.Invoke(2); });
        buyCoinsButton3.onClick.AddListener(delegate { EventManager.OnPurachaseRequested_Consumable?.Invoke(3); });
        removeAdsButton1.onClick.AddListener(() => EventManager.OnPurachaseRequested_RemoveAds?.Invoke());
        removeAdsButton2.onClick.AddListener(() => EventManager.OnPurachaseRequested_RemoveAds?.Invoke());
        unlockAllCarsButton.onClick.AddListener(() => EventManager.OnPurachaseRequested_UnlockCars?.Invoke());
        unlockAllMapsButton.onClick.AddListener(() => EventManager.OnPurachaseRequested_UnlockMaps?.Invoke());
    }


    #endregion

    #region In App Purchase Sections

    public void UnlockAllLevels()
    {
        //Debug.Log("All Levels Unlocked ");
        for (int i = 0; i < levelButtons.Length; i++)
        {
            UserData.UnlockLevel(i);
            levelLockImages[i].SetActive(false);
        }

        CheckIfLevelLocked(selectedMap);
    }

    public void UnlockAllCars()
    {
        //Debug.Log("All Car Unlocked ");
        for (int i = 0; i < cars.Length; i++)
            UserData.UnlockCar(i);
        succesfullPurchasePopUp.SetActive(true);
        CheckIfCarLocked(currentSelectedCar);
    }

    public void RemoveAds()
    {
        //Debug.Log("Ads Removed ");
        UserData.RemoveAds();
        EventManager.OnStopAds?.Invoke();
    }
    public void BuyCoins(int coinsAmount)
    {
        //Debug.Log("Coins Added : " + coinsAmount);
        UserData.AddCoins(coinsAmount);
        UpdateCoinsUI();
    }

    void ToggleIAPButtons(bool RemoveAds, bool UnlockCars, bool UnlockMaps)
    {
        removeAdsButton1.interactable = RemoveAds;
        removeAdsButton2.interactable = RemoveAds;
        unlockAllCarsButton.interactable = UnlockCars;
        unlockAllMapsButton.interactable = UnlockMaps;

    }

    #endregion


    #region Privacy Region

    void OnPrivacyPolicyClicked()
    {
        Application.OpenURL("https://sites.google.com/view/super-car-racing-3d/home");
    }
    void OnAcceptPolicyClicked()
    {
        UserData.PrivacyAccepted();
        privacyPopUp.SetActive(false);
        userNamePopUp.SetActive(true);
    }
    #endregion
}

}