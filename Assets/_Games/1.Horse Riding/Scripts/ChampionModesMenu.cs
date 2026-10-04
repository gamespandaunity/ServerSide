using System.Collections;
using System.Collections.Generic;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

public class ChampionModesMenu : MonoBehaviour
{
    public static string WorldCupName = "";
    public GameObject unlockAllChampion;
    public List<ChampionModesInfo> ModesInfo;
    private bool _isChampionUnlocked;
    public GameObject UnlockPopUp;
    public Text UnlockInfo;

    void OnEnable()
    {
        if (!PlayerDataController.instance.playerStats.UnlockedBritishChampion || !PlayerDataController.instance.playerStats.UnlockedKentuckyChampion || !PlayerDataController.instance.playerStats.UnlockedPegasusChampion)
        {
            unlockAllChampion.SetActive(true);
        }
    }
    
    public void ShowLevelMenu(int id)
    {
        // set champion title
        switch (id)
        {
            case 0:
                MConstants.CurrentCHAMPION_MODE = MConstants.CHAMPION_MODES.DUABI_CHAMPION;
                break;
            case 1:
                MConstants.CurrentCHAMPION_MODE = MConstants.CHAMPION_MODES.BRITISH_CHAMPION;
                break;
            case 2:
                MConstants.CurrentCHAMPION_MODE = MConstants.CHAMPION_MODES.KENTUCKY_CHAMPION;
                break;
            case 3:
                MConstants.CurrentCHAMPION_MODE = MConstants.CHAMPION_MODES.PEGASUS_CHAMPION;
                break;
        }


        if (IsChampionUnlocked(ModesInfo[id]))
        {
            MainMenuManager.Instance.showMenu(MenuNames.level_SELECTION);
        }
        else
        {
            ShowUnlockChampion(ModesInfo[id]);
        }
    }
    
    public void UnlockAllChampionship()
    {
        PlayerDataController.instance.playerStats.UnlockedBritishChampion = true;
        PlayerDataController.instance.playerStats.UnlockedKentuckyChampion = true;
        PlayerDataController.instance.playerStats.UnlockedPegasusChampion = true;
	    
        PlayerDataController.instance.SaveData();
        unlockAllChampion.SetActive(false);
    }

    private void ShowUnlockChampion(ChampionModesInfo championModesInfo)
    {
        UnlockInfo.text = championModesInfo.UnlockInfo.ToUpper();
        UnlockPopUp.SetActive(true);    
    }

    private bool IsChampionUnlocked(ChampionModesInfo championModesInfo)
    {
        return championModesInfo.IsUnlocked;
    }
}
