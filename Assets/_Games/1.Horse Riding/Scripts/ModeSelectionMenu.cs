using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class ModeSelectionMenu : MonoBehaviour {
    public MultiLobbyPanel multiLobbyPanel;
    public ModeInfoController infoController;
    public int RankToUnlockLastStandingMode;
    public int RankToUnlockLastTimeTrial;

    public GameObject lastManLockedUI;
    public GameObject timeTrialLockedUI;

    public Text lastManRankText;
    public Text timeTrialTimeTrialText;
    public bool isNitrosMessageShown;
    int selectedIds;
    public static ModeSelectionMenu instance;
    private void Awake()
    {
        instance = this;
    }
    void OnEnable()
    {
       
        if (PlayerDataController.instance.playerStats.Rank >= RankToUnlockLastStandingMode) {
            lastManLockedUI.SetActive(false);
        }
        else
        {
            lastManLockedUI.SetActive(true);
            lastManRankText.text = RankToUnlockLastStandingMode.ToString();
        }

        if (PlayerDataController.instance.playerStats.Rank >= RankToUnlockLastTimeTrial)
        {
            timeTrialLockedUI.SetActive(false);
        }
        else
        {
            timeTrialLockedUI.SetActive(true);
            timeTrialTimeTrialText.text = RankToUnlockLastTimeTrial.ToString();

        }
        //if (PlayerDataController.Instance.playerData.playerName.Equals("Player") && MConstants.isShowName)
        //{
        //    multiLobbyPanel.showNameMenu();
        //    MConstants.isShowName = false;
        //}

        MultiPlayerGame.IsInternetConnection();
    }

    void showAd()
    {
        MainMenuManager.Instance.CloseSubMenu();
    }

    public void CallBackId()
    {
        if (isNitrosMessageShown)
        {
            SelectMode(selectedIds);

        }
    }

    public void SelectMode(int id)
    {
       MConstants.CurrentLevelNumber = 1;
        if (!isNitrosMessageShown && false && PlayerDataController.instance.playerStats.NitrosCount<2 && Random.Range(0,1000)<400)
        {
            MainMenuManager.Instance.AdNitros();
            selectedIds = id;
            isNitrosMessageShown = true;
            return;
        }
        isNitrosMessageShown = false;
        
        //
        MultiPlayerGame.isChampion = false;
        MultiPlayerGame.isTimeTrial = false;
        MultiPlayerGame.isLastManStandingMode = false;
        MultiPlayerGame.isSinglePlayer = false;
        //
        Tutorials.aiStopped = false;

        
        switch (id)
        {
            case 0:
                MultiPlayerGame.isTimeTrial = false;

                if (MultiPlayerGame.IsInternetConnection())
                {
                    MultiPlayerGame.isLastManStandingMode = false;

                    multiLobbyPanel.OnLoginButtonClickedPlayWithFriend();
                   
                }
                else
                {
                    MainMenuManager.Instance.showSubMenu(SubMenuNames.NO_INTERNET);
                }
                break;
            case 1:
                MultiPlayerGame.isTimeTrial = false;

                if (MultiPlayerGame.IsInternetConnection())
                {
                    MultiPlayerGame.isLastManStandingMode = false;

                    multiLobbyPanel.OnLoginButtonClicked();
                   
                }
                else
                {
                    MainMenuManager.Instance.showSubMenu(SubMenuNames.NO_INTERNET);

                }
                break;
            case 2:
                MultiPlayerGame.isTimeTrial = false;

                MultiPlayerGame.isLastManStandingMode = false;
                PlayerDataController.instance.playerStats.CurrentMode = id;
                MainMenuManager.Instance.isLeaveEnable = false;
                MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playRandom;
                MainMenuManager.Instance.showSubMenu(SubMenuNames.LOADING);

                multiLobbyPanel.OnOffLineButtonClicked()
                    ;
                //  MainMenuManager.Instance.showMenu(MenuNames.ENVIORNMENT_SELECTION);
                
                break;
            case 3:
                if (PlayerDataController.instance.playerStats.Rank < RankToUnlockLastStandingMode)
                {
                    infoController.setRankInfo(RankToUnlockLastStandingMode);
                    MainMenuManager.Instance.showSubMenu(SubMenuNames.RANK_INFO);
                    return;
                }
                MultiPlayerGame.isTimeTrial = false;
                if (MultiPlayerGame.IsInternetConnection())
                {
                    MultiPlayerGame.isLastManStandingMode = true;

                    multiLobbyPanel.OnLoginButtonClicked();
                   

                }
                else
                {
                    MainMenuManager.Instance.showSubMenu(SubMenuNames.NO_INTERNET);

                }
                break;
            case 4:
                MultiPlayerGame.isTimeTrial = true;

                if (PlayerDataController.instance.playerStats.Rank < RankToUnlockLastTimeTrial)
                {
                    infoController.setRankInfo(RankToUnlockLastTimeTrial);
                    MainMenuManager.Instance.showSubMenu(SubMenuNames.RANK_INFO);
                    return;
                }
               
                PlayerDataController.instance.playerStats.CurrentMode = id;
                MainMenuManager.Instance.showMenu(MenuNames.ENVIORNMENT_SELECTION);
               
                break;
            case 5:
                MultiPlayerGame.isTimeTrial = false;
                MultiPlayerGame.isLastManStandingMode = false;
                MultiPlayerGame.isSinglePlayer = false;
                MultiPlayerGame.isChampion = true;
                PlayerDataController.instance.playerStats.CurrentMode = id;
                MainMenuManager.Instance.showMenu(MenuNames.CHAMPION_SELECTION);
               
                break;
        }
		
	}

}
