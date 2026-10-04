using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using MalbersAnimations.HAP;

public class VehicleMenuManager : MonoBehaviour {

	public GameObject[] GarageCars;
	public MainMenuManager menuManger;

	//public Transform target;
	//public GameObject carLock;

	public float speed;
	//public GameObject BuyButton;
	public GameObject UpgradeButton;
	public GameObject FullyUpgradedButton;

	public Text BuyPrice;
	public Text UpgradePrice;

	public Image []SpeedBars;
	public Image []AccBars;
	public Image []CtrlBars;
	public Image []N2oBars;


	private bool move = false;
	private int currentCar = 1;
	private int previousCar = 1;
	private  int maxCars = 5; 
	private const int minCars = 1;
	PlayerDataSerializeable playerData;

	public static float MAX_SPEED=220;
	public static float MAX_ACC=50;
	public static float MAX_CTRL=100;
	public static float MAX_N2O=100;
	bool isMoveAble = true;
    public GameObject rateUsMenu;

    public HorseCameraOrbit RccCameraOrbit;
    
	void OnEnable () {

		isMoveAble = true;
		playerData = PlayerDataController.instance.playerStats;
        maxCars = GarageCars.Length;

        currentCar = playerData.CurrentSelectedVehicle;
        if (playerData.CarsList[playerData.CurrentSelectedVehicle - 1].isLocked)
        {
            currentCar = 1;
            playerData.CurrentSelectedVehicle = currentCar;
        }
		GarageCars[playerData.CurrentSelectedVehicle-1].SetActive (true);
		refreshContent ();
        if (MConstants.isPlayerWin && playerData.multiplayerLevel%3==0&& rateUsMenu)
        {
            MConstants.isPlayerWin = false;
            //Invoke("showRateUs",1);
            //rateUsMenu.SetActive(true);
        }
        
        //
        RccCameraOrbit.Reset();
        //

    }

    void showRateUs()
    {
        rateUsMenu.SetActive(true);

    }
    void OnDisable () {
        //GarageCars [playerData.CurrentSelectedVehicle - 1].transform.position = target.position;

        if (gameObject)
        {
			GarageCars[playerData.CurrentSelectedVehicle - 1].SetActive(false);
		}

	}

   
    public void refreshContent(){
		//carLock.SetActive (false);

		//if(playerData.CarsList[playerData.CurrentSelectedVehicle-1].isLocked){
		//	BuyButton.SetActive (true);
		//	UpgradeButton.SetActive (false);
		//	FullyUpgradedButton.SetActive (false);
		//	carLock.SetActive (true);
  //      }
  //      else
  //      {
  //          BuyButton.SetActive(false);
  //          UpgradeButton.SetActive(false);
  //          FullyUpgradedButton.SetActive(false);
  //          carLock.SetActive(false);
  //      }
  //      else if(!playerData.CarsList[playerData.CurrentSelectedVehicle-1].isLocked && !playerData.CarsList[playerData.CurrentSelectedVehicle-1].isFullyUpgraded ){
		//	BuyButton.SetActive (false);
		//	UpgradeButton.SetActive (true);
		//	FullyUpgradedButton.SetActive (false);
		//}else if(playerData.CarsList[playerData.CurrentSelectedVehicle-1].isFullyUpgraded){
		//	BuyButton.SetActive (false);
		//	UpgradeButton.SetActive (false);
		//	FullyUpgradedButton.SetActive (true);
		//}

		BuyPrice.text = playerData.CarsList[playerData.CurrentSelectedVehicle-1].UnlockPrice.ToString();
		UpgradePrice.text = playerData.CarsList[playerData.CurrentSelectedVehicle-1].UpgradePrice.ToString();


		for (int i = 0; i < SpeedBars.Length; i++) {
			if(i == 1){
				SpeedBars [i].fillAmount = ((playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Speed))/MAX_SPEED;					
			}else{
				float upgradeValue = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Speed + (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Speed * 10) / 100;
				SpeedBars [i].fillAmount = ((upgradeValue))/MAX_SPEED;	;
			}
		}

		for (int i = 0; i < AccBars.Length; i++) {
			if(i == 1){
				AccBars [i].fillAmount = ((playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Acceleration))/MAX_ACC;					
			}else{
				float upgradeValue = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Acceleration + (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Acceleration * 10) / 100;
				AccBars [i].fillAmount = ((upgradeValue))/MAX_ACC;	;
			}
		}



		for (int i = 0; i < CtrlBars.Length; i++) {
			if(i == 1){
				CtrlBars [i].fillAmount = ((playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Control))/MAX_CTRL;					
			}else{
				float upgradeValue = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Control + (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Control * 10) / 100;
				CtrlBars [i].fillAmount = ((upgradeValue))/MAX_CTRL;	;
			}
		}

		for (int i = 0; i < N2oBars.Length; i++) {
			if(i == 1){
				N2oBars [i].fillAmount = ((playerData.CarsList [playerData.CurrentSelectedVehicle - 1].N2O))/MAX_N2O;					
			}else{
				float upgradeValue = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].N2O + (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].N2O * 10) / 100;
				N2oBars [i].fillAmount = ((upgradeValue))/MAX_N2O;	;
			}
		}


	}


	public void loadPreviousCar(){
		//if(!isMoveAble){
		//	return;
		//}
		//isMoveAble = false;
		//Invoke ("makeMoveAble",1.5f);
		previousCar = currentCar;
		if (currentCar == minCars) {
			currentCar = maxCars;
		} else {
			currentCar -= 1;
		}
		playerData.CurrentSelectedVehicle = currentCar;
		move = true;
		isReset = false;
		refreshContent ();
        GarageCars[previousCar - 1].SetActive(false);
        GarageCars[currentCar - 1].SetActive(true);
    }
	void makeMoveAble(){
		isMoveAble = true;

	}
	public void loadNextCar(){
		//if(!isMoveAble){
		//	return;
		//}
		//isMoveAble = false;
		//Invoke ("makeMoveAble",1.5f);
		previousCar = currentCar;
		if (currentCar == maxCars) {
			currentCar = 1;
		} else {
			currentCar += 1;
		}
		move = true;
		isReset = false;
		playerData.CurrentSelectedVehicle = currentCar;
		refreshContent ();
        GarageCars[previousCar - 1].SetActive(false);
       	GarageCars[currentCar-1].SetActive(true);
        // move previous car to next point and then reset it to start
        // move current car to given location to show to user
    }

    private void moveCars(int prevCar, int currCar){

	}

	bool isReset = false;
	void Update () {
		//if (move) {
		//	float distance = Vector3.Distance(GarageCars[previousCar-1].transform.position, target.position);

		//	//			//Debug.Log("distance"+distance);

		//	if(distance < 2 && !isReset){
		//		isReset = true;
		//		move = false;
		//		GarageCars[previousCar-1].transform.position = new Vector3(-24.36f, 1.51f, 13.45f);
		//		GarageCars[previousCar-1].SetActive(false);
		//		GarageCars[currentCar-1].SetActive(true);
		//		GarageCars[currentCar-1].GetComponent<Animator>().enabled = true;
		//	} else {
		//		float step = speed * Time.deltaTime;
		//		GarageCars[previousCar-1].transform.position = Vector3.MoveTowards (GarageCars[previousCar-1].transform.position, target.position, step);
		//	}
		//}
	}

	public void upgrade(){
		PlayerDataController.instance.playerStats.PlayerCash -= PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle- 1].UpgradePrice;
		PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].UpgradeLevel++;

		float upgradeValue = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Speed + (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Speed * 10) / 100;
		PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].Speed = upgradeValue;

		upgradeValue = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Acceleration + (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Acceleration * 10) / 100;
		PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].Acceleration = upgradeValue;
	
		upgradeValue = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Control + (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].Control * 10) / 100;
		PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].Control = upgradeValue;

		upgradeValue = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].N2O + (playerData.CarsList [playerData.CurrentSelectedVehicle - 1].N2O * 10) / 100;
		PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].N2O = upgradeValue;
		if(PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].UpgradeLevel>=5){
			PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].isFullyUpgraded = true;
		}
		PlayerDataController.instance.SaveData ();

		menuManger.RefreshData ();
		refreshContent ();
	}

	public void Unlock(){
		PlayerDataController.instance.playerStats.PlayerGold -= PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle- 1].UnlockPrice;
		PlayerDataController.instance.playerStats.CarsList [PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1].isLocked = false;
		PlayerDataController.instance.SaveData ();

		menuManger.RefreshData ();
		refreshContent ();
	}

	public void ShowRewardedAdd(){
	}
}
