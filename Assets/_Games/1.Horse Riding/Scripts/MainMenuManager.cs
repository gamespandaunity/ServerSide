using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using CarRace;

public class MainMenuManager : MonoBehaviour {
	
	public Text PlayerGoldText;
	public Text PlayerCashText;
	public Text PlayerRankText;
    public Text PlayerNitrosText;

    public Text playerName;

    public Image promoImage;

	public MenuNames currentMenuName;
	public MenuNames previousMenuName;

	public SubMenuNames currentSubMenu;

	public List<GameObject> menusList;
	public List<GameObject> subMenusList;
	public List<MenuNames> menusStack;
	public static MainMenuManager Instance; 
	public static bool isRankUp = false;
	public static bool isGoToGrage = false;
	public static bool isGoToLevels = false;
	public List<Sprite> helmetsSpriteList;
	bool  isSubMenuVisible ;
    public bool isLeaveEnable = true;
    public GameObject PlayerData;
    public MultiLobbyPanel multiLobbyPanel;


    public Image profilePic;
    public Image flagImage;

    public GameObject profilePicObject;
    void Awake(){
	    
	    //
	    Time.timeScale = 1;
	    Tutorials.aiStopped = false;
	    Tutorials.isTutorialActive = false;
	    //
	    
		Instance = this;

        if (PlayerDataController.instance && PlayerDataController.instance.playerStats != null)
        {
            PlayerDataController.instance.playerStats.isRateUSDone = true;
            PlayerDataController.instance.SaveData();
        }

    }
	// Use this for initialization
	void OnEnable()
	{

		RefreshData ();
		menusStack = new List<MenuNames> ();
        //Debug.Log("changes");
		showMenu (MenuNames.VEHICLE_MENU);
		ToggleSound (PlayerDataController.instance.playerStats.isSoundOn);
		ChangeController (PlayerDataController.instance.playerStats.SelectedControl);
		if(isGoToGrage){
			isGoToGrage = false;
			showMenu (MenuNames.VEHICLE_MENU);
		}
		
		//
		if(isGoToLevels){
			//Debug.Log("isGoToLevels");
			isGoToLevels = false;
			showMenu (MenuNames.MODE_SELCT_MENU);
			showMenu (MenuNames.level_SELECTION);
		}
		//

		if(isRankUp){
			isRankUp = false;
			//showSubMenu (SubMenuNames.LEVEL_UP);
		}
        isLeaveEnable = true;
        refreshPromoData ();
	}

	void Start(){

        if (PlayerDataController.instance.playerStats.playerName.Equals("Player") && MConstants.isShowName)
        {
            StartCoroutine(ShowPlayerName());
            MConstants.isShowName = false;
        }

        //if (PlayerDataController.Instance.playerData.countryCode == null || PlayerDataController.Instance.playerData.countryCode.Equals(""))
        //{
        // SetCountryCode();
        //}
        //else
        //{
        // Sprite sprite = CountriesFlags.LoadFlag(PlayerDataController.Instance.playerData.countryCode);

        // MainMenuManager.Instance.flagMaterial.mainTexture = sprite.texture;
        //}

       
    }

	public void RefreshData(){
		PlayerGoldText.text = PlayerDataController.instance.playerStats.PlayerGold.ToString();
		PlayerCashText.text = PlayerDataController.instance.playerStats.PlayerCash.ToString();
		PlayerRankText.text = PlayerDataController.instance.playerStats.Rank.ToString();
        PlayerNitrosText.text = PlayerDataController.instance.playerStats.NitrosCount.ToString();

        playerName.text = PlayerDataController.instance.playerStats.playerName.ToUpper();
        //if (profilePicObject && profilePic && PlayerDataController.Instance.isProfilePicExist())
        //{
        //    profilePic.sprite = Sprite.Create(PlayerDataController.Instance.LoadTextureFromFile(), new Rect(0, 0, 128, 128), new Vector2());
        //    profilePic.gameObject.SetActive(true);
        //}
        //else 

        Sprite sprite = CountriesFlags.LoadFlag(PlayerDataController.instance.playerStats.countryCode);
        flagImage.sprite = sprite;
    }

    IEnumerator ShowPlayerName()
    {
        yield return new WaitForSeconds(0.1f);
        multiLobbyPanel.showNameMenu();
    }
    void refreshPromoData(){
	  
//		int count = 0;
//		do{
//			promo =
//				PlayerDataController.Instance.playerData.promoList [Random.Range (0, PlayerDataController.Instance.playerData.promoList.Count)];
//			count++;
//		}while(promo!= null && promo.isPromoClicked && count < PlayerDataController.Instance.playerData.promoList.Count*4 );
//
//		if (promo == null || promo.isPromoClicked) {
//			promoImage.gameObject.SetActive (false);
//		} else {
//			promoImage.gameObject.SetActive (true);
//			promoImage.sprite = promoSpriteList [promo.Id - 1];
//		}

	}

	public void ToggleSound(bool isOn){
		//if(isOn){
		//	AudioListener.volume = 1;
		//}else{
		//	AudioListener.volume =0;

		//}

        if (PlayerDataController.instance.playerStats.isHighQuality)
        {
            QualitySettings.SetQualityLevel(5,true);
            //toggleSound.isOn = isOn;
        }
        else
        {
            QualitySettings.SetQualityLevel(0, true);

            //toggleSound.isOn = isOn;
        }
    }

    public void ChangeController(int index)
    {

        switch (index)
        {

            case 0://Buttons
                   //RCC_Settings.Instance.useAccelerometerForSteering = false;
                   //RCC_Settings.Instance.useSteeringWheelForSteering = false;
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.TouchScreen;


                break;
            case 1://Tilt
                   //RCC_Settings.Instance.useAccelerometerForSteering = true;
                   //RCC_Settings.Instance.useSteeringWheelForSteering = false;
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.Gyro;

                break;
            case 2://Steering
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.SteeringWheel;

                //RCC_Settings.Instance.useAccelerometerForSteering = false;
                //RCC_Settings.Instance.useSteeringWheelForSteering = true;
                break;

        }

    }

    public void showMenu(MenuNames menuName){
		previousMenuName = currentMenuName;
		menusList [currentMenuName.GetHashCode ()].SetActive (false);
		menusList [menuName.GetHashCode ()].SetActive (true);
		menusStack.Add (menuName);

		currentMenuName = menuName;
	}

	public void showSubMenu(SubMenuNames menuName){

		for (int i = 0; i < subMenusList.Count; i++) {
			subMenusList [i].SetActive (false);
		}
		isSubMenuVisible = true;
		currentSubMenu = menuName;
		subMenusList [menuName.GetHashCode ()].SetActive (true);

	}

	public void handleBackMenu ()
	{
		if (!isLeaveEnable)
		{
			return;
		}
		if(isSubMenuVisible){
			CloseSubMenu ();
			return;
		}
		if (menusStack.Count >= 2)
		{
			MenuNames toshow = menusStack[menusStack.Count - 2];
			MenuNames toRemove = menusStack[menusStack.Count - 1];
			menusStack.Remove(toRemove);
			currentMenuName = toshow;
			previousMenuName = currentMenuName;

			menusList[toRemove.GetHashCode()].SetActive(false);
			menusList[toshow.GetHashCode()].SetActive(true);
			//			if (currentMenuName == MenuNames.MAIN_MENU && isIos) {
			//				backButton.SetActive (false);
			//			} else {
			//				backButton.SetActive (true);
			//			}
		}
		else
		{
			showSubMenu(SubMenuNames.EXIT_GAME);
			//Application.Quit();
		}
	}

    public void ExitGame()
    {
        Application.Quit();
    }

    void Update () {
		if (Input.GetKeyDown (KeyCode.Escape)) {
			handleBackMenu ();
		}
	}
	public void CloseSubMenu(){
		isSubMenuVisible = false;

		subMenusList [currentSubMenu.GetHashCode ()].SetActive (false);

	}

	public void StartMenuDrive(){
		showMenu (MenuNames.VEHICLE_MENU);
		//StartMenu.SetActive (false);
		//GarageMenu.SetActive (true);
		//GarageCars[currentCar-1].SetActive (true);
		//GarageCars[currentCar-1].GetComponent<Animator>().enabled = true;
	}

	public void openGameModesMenu(){
		 PlayerDataSerializeable playerData;

		playerData = PlayerDataController.instance.playerStats;

		if (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].isLocked) {
			openCarUnlockPopup ();
		} else {
			showMenu (MenuNames.MODE_SELCT_MENU);

		}

	}
	public void BackToStartMenu(){
        showMenu(MenuNames.VEHICLE_MENU);
        //showMenu (MenuNames.MAIN_MENU);

        //GarageCars[currentCar-1].transform.position = new Vector3(-24.36f, 1.51f, 13.45f);
        //GarageCars[currentCar-1].SetActive(false);
    }

	public void openSettings(){
		showSubMenu (SubMenuNames.SETTING_MENU);
	}



	public void openCarUnlockPopup(){
		if (PlayerDataController.instance.playerStats.PlayerGold >= PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].UnlockPrice) {
			showSubMenu (SubMenuNames.CAR_UNLOCK_POPUP);
		} else {
			OutOfCashMenu.isCarBuy = true;
			showSubMenu (SubMenuNames.OUT_OF_CASH);
		}
	}


	public void openCarUpGradePopup(){
		if (PlayerDataController.instance.playerStats.PlayerCash >= PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].UpgradePrice) {
			showSubMenu (SubMenuNames.CAR_UPGRADE_POPUP);
		} else {
			showSubMenu (SubMenuNames.OUT_OF_CASH_UPGRADE);
		}
	}

	public void RateUs(){
		Application.OpenURL(MConstants.RATE_US);
	}

    public void MoreGames()
    {
        Application.OpenURL("https://play.google.com/store/apps/developer?id=Frenzy+Games+Studio");
    }

    public void youtubeSub()
    {
        Application.OpenURL("https://www.youtube.com/channel/UCGdM7_wDDteLs1F_02Zc8tQ?sub_confirmation=1");
    }


    public void FB(){
		Application.OpenURL("https://web.facebook.com/top3Dgamessimstudio/");
	}

    string subject = "Frenzy Games Studio";
    string body = "Free Download Horse Riding Rival: Multiplayer Derby Racing from Google Play Store. " + MConstants.RATE_US;

    public void ShareGame()
    {
        //execute the below lines if being run on a Android device
#if UNITY_ANDROID
        //Refernece of AndroidJavaClass class for intent
        AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
        //Refernece of AndroidJavaObject class for intent
        AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
        //call setAction method of the Intent object created
        intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
        //set the type of sharing that is happening
        intentObject.Call<AndroidJavaObject>("setType", "text/plain");
        //add data to be passed to the other activity i.e., the data to be sent
        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), body);
        //get the current activity
        AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        //start the activity by sending the intent data
        currentActivity.Call("startActivity", intentObject);
#endif

    }


    public void PromoClicked(){
//		for (int i = 0; i < PlayerDataController.Instance.playerData.promoList.Count; i++) {
//			if(PlayerDataController.Instance.playerData.promoList[i].Id == promo.Id){
//				PlayerDataController.Instance.playerData.promoList [i].isPromoClicked = true;
//			}
//		}
//		PlayerDataController.Instance.Save ();
//		Application.OpenURL(promo.gameLink);
//
//		refreshPromoData ();
	}

	public void ShowLeaderBoard(){
		SocialManager.instance.OnShowLeaderBoard ();
	}

	public void AdGold(){
		showSubMenu (SubMenuNames.FREE_GOLD_POPUP);
	}

    public void AdNitros()
    {
        showSubMenu(SubMenuNames.NITROS);
    }


    public void ShowRewardedAddForNitros()
    {
    }

    public  Sprite GetSprite(int colorChoice)
    {
       
        if (colorChoice>= 0&& colorChoice < helmetsSpriteList.Count)
        {
            return helmetsSpriteList[colorChoice];
        }

        return helmetsSpriteList[Random.Range(0, helmetsSpriteList.Count)];
    }

    public void SetCountryCode()
    {
        CountriesFlags.GetLocation(this, (LocationData, isNetworkError) => {
            if (!isNetworkError)
            {
                PlayerDataController.instance.playerStats.countryCode = LocationData.countryCode;
                PlayerDataController.instance.SaveData();
                //Reading and viewing the location data
                //Debug.Log("CountryCode: " + LocationData.countryCode);
                RivalsCountryController.Instance.SetRivalCountry(LocationData.countryCode);
                Sprite sprite = CountriesFlags.LoadFlag(PlayerDataController.instance.playerStats.countryCode);

                RefreshData();
                //Flag.sprite = sprite;
                //_3DFlag.material.mainTexture = sprite.texture;
                //CountryName.text = CountriesFlags.countryCodes_NamesMapping[LocationData.countryCode] + ", " + LocationData.countryCode;
            }
            else
            {
                //Debug.Log("You must be connected to internet");
            }
        });
    }

    public void OpenMoreGames()
    {
        // Application.OpenURL("https://play.google.com/store/apps/developer?id=Frenzy+Games+Studio");
        showSubMenu(SubMenuNames.MORE_GAMES);
       
    }

    public void MoreGames(string URL)
    {

        Application.OpenURL("https://play.google.com/store/apps/details?id=" + URL);
    }
    int count;
    public void UnlockAll()
    {
        count++;

        //Debug.Log("Count" + count);
        if (count==30)
        {
            PlayerDataController.instance.playerStats.LastUnlockedBritishChampionLevel = 15;
            PlayerDataController.instance.playerStats.LastUnlockedDubaiChampionLevel = 15;
            PlayerDataController.instance.playerStats.LastUnlockedKentuckyChampionLevel = 15;
            PlayerDataController.instance.playerStats.LastUnlockedPegasusChampionLevel = 15;
            PlayerDataController.instance.playerStats.Rank = 20;
            
            

            PlayerDataController.instance.playerStats.PlayerCash = 50000;
            PlayerDataController.instance.playerStats.PlayerGold = 50000;
            PlayerDataController.instance.playerStats.Rank = 10;
            PlayerDataController.instance.playerStats.xpoints = 500;

            

            PlayerDataController.instance.SaveData();

            RefreshData();
            
        }
    }


}
