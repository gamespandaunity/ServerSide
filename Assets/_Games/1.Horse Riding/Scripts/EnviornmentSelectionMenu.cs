using UnityEngine;
using System.Collections;

public class EnviornmentSelectionMenu : MonoBehaviour {
	public MainMenuManager menuManger;
    public MultiLobbyPanel multiLobbyPanel;

    public void SelectEnviornMent(int id){
		
		PlayerDataController.instance.playerStats.CurrentEnvironment = id;
		PlayerDataSerializeable pDta = PlayerDataController.instance.playerStats;

		if (pDta.envioronmentList [id-1].ID == id && pDta.envioronmentList [id-1].isLocked) {
			if (PlayerDataController.instance.playerStats.PlayerGold >= PlayerDataController.instance.playerStats.envioronmentList [id - 1].UnlockPrice) {
				menuManger.showSubMenu (SubMenuNames.ENV_UNLOCK_POPUP);
			} else {
				OutOfCashMenu.isCarBuy = false;
				menuManger.showSubMenu (SubMenuNames.OUT_OF_CASH);
			}

			return;
		}

	
		switch(id){
		case 1:
                //Application.LoadLevel("Race_Track_02-2");
                multiLobbyPanel.levelsToLoad = "Racecourse_Track_Final";
                MainMenuManager.Instance.isLeaveEnable = false;
				MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playRandom;
                multiLobbyPanel.OnOffLineButtonClicked();

                break;

		case 2:
                multiLobbyPanel.levelsToLoad = "Racecourse_Track_Final_Mud";
                MainMenuManager.Instance.isLeaveEnable = false;
                MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playRandom;

                multiLobbyPanel.OnOffLineButtonClicked();

                //Application.LoadLevel("Race_Track_01");

			break;
		case 3:
                multiLobbyPanel.levelsToLoad = "Track01_Mobile";
                MainMenuManager.Instance.isLeaveEnable = false;
                MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playRandom;

                multiLobbyPanel.OnOffLineButtonClicked();
                //Application.LoadLevel("Race_Track_03_Final");

			break;
		case 4:
                multiLobbyPanel.levelsToLoad = "Race_Track_02-2";
                MainMenuManager.Instance.isLeaveEnable = false;
                MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playRandom;

                multiLobbyPanel.OnOffLineButtonClicked();
                break;
		}

		menuManger.showSubMenu (SubMenuNames.LOADING);

		//MainMenuManager.Instance.showMenu (MenuNames.ENVIORNMENT_SELECTION);
	}

	public void UnlockEnviornment(){
	
		PlayerDataController.instance.playerStats.PlayerGold -= PlayerDataController.instance.playerStats.envioronmentList [PlayerDataController.instance.playerStats.CurrentEnvironment- 1].UnlockPrice;
		PlayerDataController.instance.playerStats.envioronmentList [PlayerDataController.instance.playerStats.CurrentEnvironment - 1].isLocked = false;
		PlayerDataController.instance.SaveData ();

		menuManger.RefreshData ();
		SelectEnviornMent(PlayerDataController.instance.playerStats.CurrentEnvironment);
	}
}
