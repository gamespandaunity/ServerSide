using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class LevelSelectionManager : MonoBehaviour
{
    PlayerDataSerializeable playerData;
    public ScrollRect ScrollViewContant;
    public List<LevelData> levelsList;
    public Text worlCupTitle;
    public GameObject unlockAllLevelsButton;
    public static LevelSelectionManager Instance;

    void Awake()
    {
	    Instance = this;
    }

    void OnEnable()
    {
	    playerData = PlayerDataController.instance.playerStats;
        //worlCupTitle.text = ChampionModesMenu.WorldCupName.ToUpper();

        if (MultiPlayerGame.isChampion)
        {
            switch (MConstants.CurrentCHAMPION_MODE)
            {
                case MConstants.CHAMPION_MODES.DUABI_CHAMPION:
                    MConstants.CurrentLevelNumber = playerData.CurrentSelectDubaiChampionLevel;
                    break;
                case MConstants.CHAMPION_MODES.BRITISH_CHAMPION:
                    MConstants.CurrentLevelNumber = playerData.CurrentSelectBritishChampionLevel;
                    break;
                case MConstants.CHAMPION_MODES.KENTUCKY_CHAMPION:
                    MConstants.CurrentLevelNumber = playerData.CurrentSelectKentuckyChampionLevel;
                    break;
                case MConstants.CHAMPION_MODES.PEGASUS_CHAMPION:
                    MConstants.CurrentLevelNumber = playerData.CurrentSelectPegasusChampionLevel;
                    break;
            }
            SetScrollView();
        }
    }
    
   
    public void onLevelClick(LevelData LevelData)
    {

	    
	    // //Debug.Log($"LevelData {LevelData.id}");
	    // //Debug.Log($"playerData {playerData.LastUnlockedDubaiChampionLevel}");
	    
		if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.DUABI_CHAMPION)
		{
			if (LevelData.id <= playerData.LastUnlockedDubaiChampionLevel)
			{
				playerData.currentSelectLevel = LevelData.id;
				MConstants.CurrentLevelNumber = LevelData.id;
			}
		}
		
		switch (MConstants.CurrentCHAMPION_MODE)
		{
			case MConstants.CHAMPION_MODES.DUABI_CHAMPION:
				if (LevelData.id <= playerData.LastUnlockedDubaiChampionLevel)
				{
					playerData.currentSelectLevel = LevelData.id;
					MConstants.CurrentLevelNumber = LevelData.id;
				}
				break;
			case MConstants.CHAMPION_MODES.BRITISH_CHAMPION:
				if (LevelData.id <= playerData.LastUnlockedBritishChampionLevel)
				{
					playerData.currentSelectLevel = LevelData.id;
					MConstants.CurrentLevelNumber = LevelData.id;
				}
				break;
			case MConstants.CHAMPION_MODES.KENTUCKY_CHAMPION:
				if (LevelData.id <= playerData.LastUnlockedKentuckyChampionLevel)
				{
					playerData.currentSelectLevel = LevelData.id;
					MConstants.CurrentLevelNumber = LevelData.id;
				}
				break;
			case MConstants.CHAMPION_MODES.PEGASUS_CHAMPION:
				if (LevelData.id <= playerData.LastUnlockedPegasusChampionLevel)
				{
					playerData.currentSelectLevel = LevelData.id;
					MConstants.CurrentLevelNumber = LevelData.id;
				}
				break;
		}
		
		MultiPlayerGame.isChampion = true;
    }
    
    public void UnlockAllLevels()
    {
	    PlayerDataController.instance.playerStats.LastUnlockedDubaiChampionLevel = MConstants.MAX_LEVELS;
	    PlayerDataController.instance.playerStats.LastUnlockedBritishChampionLevel = MConstants.MAX_LEVELS;
	    PlayerDataController.instance.playerStats.LastUnlockedKentuckyChampionLevel = MConstants.MAX_LEVELS;
	    PlayerDataController.instance.playerStats.LastUnlockedKentuckyChampionLevel = MConstants.MAX_LEVELS;
	    
	    PlayerDataController.instance.playerStats.unlockedAllLevels = true;
	    PlayerDataController.instance.SaveData();
	    unlockAllLevelsButton.SetActive(false);
       
	    foreach (var levelUIController in levelsList)
	    {
		    levelUIController.RefreshState();
	    }
        
    }
    
    public void UnlockLevel()
    {
	    if (MultiPlayerGame.isChampion)
	    {
		    switch (MConstants.CurrentCHAMPION_MODE)
		    {
			    case MConstants.CHAMPION_MODES.DUABI_CHAMPION:
				    PlayerDataController.instance.playerStats.LastUnlockedDubaiChampionLevel += 1;
				    PlayerDataController.instance.playerStats.CurrentSelectDubaiChampionLevel += 1;
				    MConstants.CurrentLevelNumber = PlayerDataController.instance.playerStats.currentSelectLevel;
				    break;
			    case MConstants.CHAMPION_MODES.BRITISH_CHAMPION:
				    PlayerDataController.instance.playerStats.LastUnlockedBritishChampionLevel += 1;
				    PlayerDataController.instance.playerStats.CurrentSelectBritishChampionLevel += 1;
				    break;
			    case MConstants.CHAMPION_MODES.KENTUCKY_CHAMPION:
				    PlayerDataController.instance.playerStats.LastUnlockedKentuckyChampionLevel += 1;
				    PlayerDataController.instance.playerStats.CurrentSelectKentuckyChampionLevel += 1;
				    break;
			    case MConstants.CHAMPION_MODES.PEGASUS_CHAMPION:
				    PlayerDataController.instance.playerStats.LastUnlockedPegasusChampionLevel += 1;
				    PlayerDataController.instance.playerStats.CurrentSelectPegasusChampionLevel += 1;
				    break;
		    }
	    }

	    PlayerDataController.instance.SaveData();
	    
        for (int i = 0; i < levelsList.Count; i++)
		{
			levelsList[i].RefreshState();
		}
	}
    
    public void FreeUnlockClik(){
    }


    public void GetScrollValue(Scrollbar scrollbar)
    {
	    // //Debug.Log($"scrollbar value {scrollbar.value}");
    }
    
    private void SetScrollView()
    {
	    if (MConstants.CurrentLevelNumber < 4)
        {
            ScrollViewContant.horizontalNormalizedPosition = (float)((float)MConstants.CurrentLevelNumber / (float)MConstants.MAX_LEVELS-0.1f);
        }
        else if (MConstants.CurrentLevelNumber >= 4 && MConstants.CurrentLevelNumber <= 7)
        {
            ScrollViewContant.horizontalNormalizedPosition = (float)((float)MConstants.CurrentLevelNumber / (float)MConstants.MAX_LEVELS) - 0.1f;
        }
        else if (MConstants.CurrentLevelNumber > 7)
        {
            ScrollViewContant.horizontalNormalizedPosition = (float)((float)MConstants.CurrentLevelNumber / (float)MConstants.MAX_LEVELS);
        }
    }

}
