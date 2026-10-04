using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CarRace;
public class GameOverMenuManager : MonoBehaviour {
	public GameObject racingModeUi;
	public GameObject timeModeUi;
	
	//
	public GameObject championWinUI;
	public GameObject championFailUI;
	public GameObject modecompleteUI;

	public Text ModeName;
	//
	
	public RCC_DashboardInputs currentCarController;

	 
	public List<Text> nameList;
	public List<Text> posList;
	public List<Text> timeList;

	public Text goldReward;
	public Text cashReward;

	public Text goldRewardForTimeMode;
	public Text cashRewardForTimeMode;
	public Text besttimeForTimeMode;
	public Text currenttimeForTimeMode;

	public RCC_MobileButtons rcMobileButton;
	int index =0;
	// Use this for initialization
	void OnEnable () {
		HudMenuManager.instance.removeLapCompletionNotification ();
		//rcMobileButton.enabled = false;
		//rcMobileButton.isHandBreakByGameOver = true;
		//currentCarController.currentCarController.isHandBreakByGameOver = true;
		int rewardCash = Random.Range (500, 1000);
		int rewardGold = Random.Range (100, 150);
		int xp = Random.Range (200, 300);
		if(LevelsManager.instance.playerPosition == 1){
			rewardCash *= (int)Random.Range (1.5f, 3.2f);
			rewardGold *= (int)Random.Range (1.5f, 2.5f);
			xp *= (int)Random.Range (1.5f, 2f);
		}

		goldReward.text = rewardGold.ToString();
		cashReward.text = rewardCash.ToString();

		goldRewardForTimeMode.text = rewardGold.ToString();
		cashRewardForTimeMode.text = rewardCash.ToString();

		// LevelsManager.instance.Levels [MConstants.CurrentLevelNumber - 1].GetComponent<Level> ().setTimesList ();
		
		//
		if (!MultiPlayerGame.isChampion)
		{
			LevelsManager.instance.raceTracks  [MConstants.CurrentLevelNumber - 1].GetComponent<Level> ().SetLapTimes ();
		}
		//

		// player and opponents positions, reward etc.
		
		//
		if (!MultiPlayerGame.isChampion)
		{
			for (int i = 1; i <= nameList.Count; i++) {
				if (LevelsManager.instance.playerPosition == i) {
					nameList [i - 1].text = "You";
					posList [i - 1].text = "" + LevelsManager.instance.playerPosition;
					timeList [i - 1].text = "" +FloatToTime((LevelsManager.instance.raceEndTime-LevelsManager.instance.raceStartTime),"#0:00.0");
					timeList [i - 1].color = Color.green;
					posList [i - 1].color = Color.green;
					nameList [i - 1].color = Color.green;

				} else {
					float time = LevelsManager.instance.raceTracks  [MConstants.CurrentLevelNumber - 1].GetComponent<Level> ().lapTimes  [index];
					timeList [i - 1].text = "" +FloatToTime((time),"#0:00.0");
					index++;
				}
			}
			if (LevelsManager.instance.playerPosition >= 4) {
				nameList [3].text = "You";
				posList [3].text = "4";
				timeList [3].text = "" +FloatToTime((LevelsManager.instance.raceEndTime-LevelsManager.instance.raceStartTime),"#0:00.0");

				timeList [3].color = Color.green;
				posList [3].color = Color.green;
				nameList [3].color = Color.green;
			}
		}
		//
		
		// for (int i = 1; i <= nameList.Count; i++) {
		// 	if (LevelsManager.instance.PlayerPosition == i) {
		// 		nameList [i - 1].text = "You";
		// 		posList [i - 1].text = "" + LevelsManager.instance.PlayerPosition;
		// 		timeList [i - 1].text = "" +FloatToTime((LevelsManager.instance.endTime-LevelsManager.instance.startTime),"#0:00.0");
		// 		timeList [i - 1].color = Color.green;
		// 		posList [i - 1].color = Color.green;
		// 		nameList [i - 1].color = Color.green;
		//
		// 	} else {
		// 		float time = LevelsManager.instance.Levels [MConstants.CurrentLevelNumber - 1].GetComponent<Level> ().timesList [index];
		// 		timeList [i - 1].text = "" +FloatToTime((time),"#0:00.0");
		// 		index++;
		// 	}
		// }
		// if (LevelsManager.instance.PlayerPosition >= 4) {
		// 	nameList [3].text = "You";
		// 	posList [3].text = "4";
		// 	timeList [3].text = "" +FloatToTime((LevelsManager.instance.endTime-LevelsManager.instance.startTime),"#0:00.0");
		//
		// 	timeList [3].color = Color.green;
		// 	posList [3].color = Color.green;
		// 	nameList [3].color = Color.green;
		// }
		
		if(PlayerDataController.instance == null){
			return;
		}

		if (LevelsManager.instance.playerPosition <= 2) {
			PlayerDataController.instance.playerStats.LastWinCount++;
		} else {
			PlayerDataController.instance.playerStats.LastWinCount--;
		}

		
		// if (PlayerDataController.Instance.playerData.CurrentMode == 1) {
		// 	racingModeUi.SetActive (false);
		// 	timeModeUi.SetActive (true);
		// 	float currentTime = (LevelsManager.instance.endTime - LevelsManager.instance.startTime);
		// 	//if(currentTime < PlayerDataController.Instance.playerData.BestTime){
		// 	//	PlayerDataController.Instance.playerData.BestTime = currentTime;
		// 	//}
		// 	//besttimeForTimeMode.text = FloatToTime (PlayerDataController.Instance.playerData.BestTime, "#0:00.0");
		// 	//currenttimeForTimeMode.text = FloatToTime (currentTime, "#0:00.0");
		//
		// } else {
		// 	racingModeUi.SetActive (true);
		// 	timeModeUi.SetActive (false);
		// }
		
		//
		if (!MultiPlayerGame.isChampion)
		{
			if (PlayerDataController.instance.playerStats.CurrentMode == 1) {
				racingModeUi.SetActive (false);
				timeModeUi.SetActive (true);
				float currentTime = (LevelsManager.instance.raceEndTime - LevelsManager.instance.raceStartTime);
				//if(currentTime < PlayerDataController.Instance.playerData.BestTime){
				//	PlayerDataController.Instance.playerData.BestTime = currentTime;
				//}
				//besttimeForTimeMode.text = FloatToTime (PlayerDataController.Instance.playerData.BestTime, "#0:00.0");
				//currenttimeForTimeMode.text = FloatToTime (currentTime, "#0:00.0");

			} else {
				racingModeUi.SetActive (true);
				timeModeUi.SetActive (false);
			}
		}
		//

		//Unlock next Level for championship
		if (MultiPlayerGame.isChampion)
		{
			if (WinFxController.LevelFailed)
			{
				Invoke("ShowChampionFailedScreen",0.1f);
			}
			else
			{
				Invoke("ShowChampionCompleteScreen",2f);
				UnlockLevel();
			}
		}
		//
		
		PlayerDataController.instance.playerStats.PlayerGold += rewardGold;
		PlayerDataController.instance.playerStats.PlayerCash += rewardCash;
		PlayerDataController.instance.playerStats.xpoints += xp;
		if(PlayerDataController.instance.playerStats.xpoints > PlayerDataController.instance.playerStats.Rank*1000){
			MainMenuManager.isRankUp = true;
		}
		// SocialManager.instance.OnAddScoreToLeaderBorad (PlayerDataController.Instance.playerData.xpoints );
		ConstantsData_M.Log("Un comment above line for live game");

		//
		// if (SocialManager.instance)
		// {
		// 	if (PlayerDataController.Instance)
		// 	{
		// 		SocialManager.instance.OnAddScoreToLeaderBorad (PlayerDataController.Instance.playerData.xpoints);
		// 	}
		// }
		//
		
		ConstantsData_M.Log("Un comment above if for live game");
		
		PlayerDataController.instance.SaveData ();
		// for championship
		// if (MultiPlayerGame.isChampion)
		// {
		// 	Retry();
		// }
		//
	}
	
	//
	private void ShowChampionCompleteScreen()
	{
		championWinUI.SetActive(true);
	}

	private void ShowChampionFailedScreen()
	{
		championFailUI.SetActive(true);
	}
	public void UnlockLevel(){
		if (MultiPlayerGame.isChampion && MConstants.CurrentLevelNumber < MConstants.MAX_LEVELS)
		{
			switch (MConstants.CurrentCHAMPION_MODE)
			{
				case MConstants.CHAMPION_MODES.DUABI_CHAMPION:
					if (MConstants.CurrentLevelNumber == PlayerDataController.instance.playerStats.LastUnlockedDubaiChampionLevel)
					{
						PlayerDataController.instance.playerStats.LastUnlockedDubaiChampionLevel += 1;
						PlayerDataController.instance.playerStats.CurrentSelectDubaiChampionLevel += 1;
						PlayerDataController.instance.playerStats.currentSelectLevel += 1;
						MConstants.CurrentLevelNumber = PlayerDataController.instance.playerStats.currentSelectLevel;
						PlayerDataController.instance.SaveData();
					}
					
					break;
				case MConstants.CHAMPION_MODES.BRITISH_CHAMPION:
					PlayerDataController.instance.playerStats.LastUnlockedBritishChampionLevel += 1;
					PlayerDataController.instance.playerStats.CurrentSelectBritishChampionLevel += 1;
					PlayerDataController.instance.playerStats.currentSelectLevel += 1;
					MConstants.CurrentLevelNumber = PlayerDataController.instance.playerStats.currentSelectLevel;
					PlayerDataController.instance.SaveData();
					break;
				case MConstants.CHAMPION_MODES.KENTUCKY_CHAMPION:
					PlayerDataController.instance.playerStats.LastUnlockedKentuckyChampionLevel += 1;
					PlayerDataController.instance.playerStats.CurrentSelectKentuckyChampionLevel += 1;
					PlayerDataController.instance.playerStats.currentSelectLevel += 1;
					MConstants.CurrentLevelNumber = PlayerDataController.instance.playerStats.currentSelectLevel;
					PlayerDataController.instance.SaveData();
					break;
				case MConstants.CHAMPION_MODES.PEGASUS_CHAMPION:
					PlayerDataController.instance.playerStats.LastUnlockedPegasusChampionLevel += 1;
					PlayerDataController.instance.playerStats.CurrentSelectPegasusChampionLevel += 1;
					PlayerDataController.instance.playerStats.currentSelectLevel += 1;
					MConstants.CurrentLevelNumber = PlayerDataController.instance.playerStats.currentSelectLevel;
					PlayerDataController.instance.SaveData();
					break;
			}
		}
		else
		{
			MainMenuManager.isGoToLevels = false;
			MainMenuManager.isGoToGrage = true;
			//Debug.Log("Last Level");
		}
	}
	//

	public void Retry(){
		// HudMenuManager.instance.loading.SetActive (true);

		
		// switch(PlayerDataController.Instance.playerData.CurrentEnvironment){
		// 	case 1:
		// 		Application.LoadLevel("Race_Track_02-2");
		//
		// 		break;
		//
		// 	case 2:
		// 		Application.LoadLevel("Race_Track_01");
		//
		// 		break;
		// 	case 3:
		// 		Application.LoadLevel("Race_Track_03_Final");
		//
		// 		break;
		// 	case 4:
		// 		Application.LoadLevel("Race_Track_02-2");
		//
		// 		break;
		// }
		//
		
		//
		if (!MultiPlayerGame.isChampion)
		{
			HudMenuManager.instance.loadingScreen.SetActive (true);
			switch(PlayerDataController.instance.playerStats.CurrentEnvironment){
				case 1:
					Application.LoadLevel("Race_Track_02-2");

					break;

				case 2:
					Application.LoadLevel("Race_Track_01");

					break;
				case 3:
					Application.LoadLevel("Race_Track_03_Final");

					break;
				case 4:
					Application.LoadLevel("Race_Track_02-2");

					break;
			}
		}
		else
		{
			// if (MConstants.CurrentLevelNumber == MConstants.MAX_LEVELS)
			// {
			// 	ShowModeComplete(ModeName);
			// }
			
			if (MConstants.CurrentLevelNumber == MConstants.MAX_LEVELS)
			{
				ShowModeComplete(ModeName);
			}
			else
			{
				HudMenuManager.instance.loadingScreen.SetActive (true);
				Application.LoadLevel(SceneManager.GetActiveScene().name);
			}
		}
		//

		 
		//MainMenuManager.Instance.showMenu (MenuNames.ENVIORNMENT_SELECTION);
	}

	public string FloatToTime (float toConvert, string format){
		switch (format){
		case "00.0":
			return string.Format("{0:00}:{1:0}", 
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*10) % 10));//miliseconds
			break;
		case "#0.0":
			return string.Format("{0:#0}:{1:0}", 
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*10) % 10));//miliseconds
			break;
		case "00.00":
			return string.Format("{0:00}:{1:00}", 
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*100) % 100));//miliseconds
			break;
		case "00.000":
			return string.Format("{0:00}:{1:000}", 
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*1000) % 1000));//miliseconds
			break;
		case "#00.000":
			return string.Format("{0:#00}:{1:000}", 
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*1000) % 1000));//miliseconds
			break;
		case "#0:00":
			return string.Format("{0:#0}:{1:00}",
				Mathf.Floor(toConvert / 60),//minutes
				Mathf.Floor(toConvert) % 60);//seconds
			break;
		case "#00:00":
			return string.Format("{0:#00}:{1:00}", 
				Mathf.Floor(toConvert / 60),//minutes
				Mathf.Floor(toConvert) % 60);//seconds
			break;
		case "0:00.0":
			return string.Format("{0:0}:{1:00}.{2:0}",
				Mathf.Floor(toConvert / 60),//minutes
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*10) % 10));//miliseconds
			break;
		case "#0:00.0":
			return string.Format("{0:#0}:{1:00}.{2:0}",
				Mathf.Floor(toConvert / 60),//minutes
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*10) % 10));//miliseconds
			break;
		case "0:00.00":
			return string.Format("{0:0}:{1:00}.{2:00}",
				Mathf.Floor(toConvert / 60),//minutes
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*100) % 100));//miliseconds
			break;
		case "#0:00.00":
			return string.Format("{0:#0}:{1:00}.{2:00}",
				Mathf.Floor(toConvert / 60),//minutes
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*100) % 100));//miliseconds
			break;
		case "0:00.000":
			return string.Format("{0:0}:{1:00}.{2:000}",
				Mathf.Floor(toConvert / 60),//minutes
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*1000) % 1000));//miliseconds
			break;
		case "#0:00.000":
			return string.Format("{0:#0}:{1:00}.{2:000}",
				Mathf.Floor(toConvert / 60),//minutes
				Mathf.Floor(toConvert) % 60,//seconds
				Mathf.Floor((toConvert*1000) % 1000));//miliseconds
			break;
		}
		return "error";
	}

	public void Continue(){
		
		HudMenuManager.instance.loadingScreen.SetActive (true);

		MainMenuManager.isGoToGrage = true;

		Application.LoadLevel("UIScene");
		 
	}
	
	
	//
	
	public void GoToLevelMenu(){
		HudMenuManager.instance.loadingScreen.SetActive (true);


		Application.LoadLevel("UIScene");
		

	}
	
	public void Replay()
	{
		if (MultiPlayerGame.isChampion)
		{
			MConstants.CurrentLevelNumber = PlayerDataController.instance.playerStats.currentSelectLevel;
		}
		Retry();
	}
	
	public void WinReplay()
	{
		if (MultiPlayerGame.isChampion)
		{
			MConstants.CurrentLevelNumber = PlayerDataController.instance.playerStats.currentSelectLevel -1;
		}
		Retry();
	}

	private void ShowModeComplete(Text modeName)
	{
		switch (MConstants.CurrentCHAMPION_MODE.GetHashCode())
		{
			case 0:
				modeName.text = MConstants.CHAMPION_MODES.DUABI_CHAMPION.ToString();
				break;
			case 1:
				modeName.text = MConstants.CHAMPION_MODES.BRITISH_CHAMPION.ToString();
				break;
			case 2:
				modeName.text = MConstants.CHAMPION_MODES.KENTUCKY_CHAMPION.ToString();
				break;
			case 3:
				modeName.text = MConstants.CHAMPION_MODES.PEGASUS_CHAMPION.ToString();
				break;
		}
		modecompleteUI.SetActive(true);
	}
	//

}
