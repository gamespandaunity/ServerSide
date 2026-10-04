using System.Collections;
using System.Collections.Generic;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

public class LevelData : MonoBehaviour
{
   public int id;
   public int Opponents;
   public GameObject freeLockedImage;
   public GameObject lockImage;
   public GameObject levelButtonCurrent;
   public GameObject levelButton;
   public Text levelNoText;
   PlayerDataSerializeable playerData;

   void OnEnable()
   {
	   levelNoText.text = id.ToString();
	   RefreshState();
   }
   
   public void SelectLevel()
   {
      MultiPlayerGame.NO_AI_CAR = Opponents;
      MConstants.CurrentLevelNumber = id;
   }

  public void RefreshState()
  {
	  //
	  freeLockedImage.SetActive(false);
	  lockImage.SetActive(false);
	  levelButtonCurrent.SetActive(false);
	  levelButton.SetActive(false);
	  //
        playerData = PlayerDataController.instance.playerStats;

        if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.DUABI_CHAMPION)
		{
			if (id <= playerData.LastUnlockedDubaiChampionLevel)
			{
				freeLockedImage.SetActive(false);
				lockImage.SetActive(false);
				if (id == playerData.CurrentSelectDubaiChampionLevel)
				{
					levelButtonCurrent.gameObject.SetActive(true);
					levelButton.gameObject.SetActive(true);
				}
				else
				{
					levelButton.gameObject.SetActive(true);
					levelButtonCurrent.gameObject.SetActive(false);
				}
				levelNoText.gameObject.SetActive(true);
			}
			else if ((id == playerData.LastUnlockedDubaiChampionLevel + 1) && false)
			{
				freeLockedImage.SetActive(true);
				lockImage.SetActive(false);
				levelButton.gameObject.SetActive(false);
				levelButtonCurrent.gameObject.SetActive(false);
				levelNoText.gameObject.SetActive(false);
			}
			else
			{
				freeLockedImage.SetActive(false);
				lockImage.SetActive(true);
				levelButton.gameObject.SetActive(false);
				levelButtonCurrent.gameObject.SetActive(false);
				levelNoText.gameObject.SetActive(false);
			}
		}
        if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.BRITISH_CHAMPION)
        {
	        if (id <= playerData.LastUnlockedBritishChampionLevel)
	        {
		        freeLockedImage.SetActive(false);
		        lockImage.SetActive(false);
		        if (id == playerData.CurrentSelectBritishChampionLevel)
		        {
			        levelButtonCurrent.gameObject.SetActive(true);
			        levelButton.gameObject.SetActive(true);
		        }
		        else
		        {
			        levelButton.gameObject.SetActive(true);
			        levelButtonCurrent.gameObject.SetActive(false);
		        }
		        levelNoText.gameObject.SetActive(true);
	        }
	        else if ((id == playerData.LastUnlockedBritishChampionLevel + 1) && false)
	        {
		        freeLockedImage.SetActive(true);
		        lockImage.SetActive(true);
		        levelButton.gameObject.SetActive(false);
		        levelButtonCurrent.gameObject.SetActive(false);
		        levelNoText.gameObject.SetActive(false);
	        }
	        else
	        {
		        freeLockedImage.SetActive(false);
		        lockImage.SetActive(true);
		        levelButton.gameObject.SetActive(false);
		        levelButtonCurrent.gameObject.SetActive(false);
		        levelNoText.gameObject.SetActive(false);
	        }
        }
        if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.KENTUCKY_CHAMPION)
        {
	        if (id <= playerData.LastUnlockedKentuckyChampionLevel)
	        {
		        freeLockedImage.SetActive(false);
		        lockImage.SetActive(false);
		        if (id == playerData.CurrentSelectKentuckyChampionLevel)
		        {
			        levelButtonCurrent.gameObject.SetActive(true);
			        levelButton.gameObject.SetActive(true);
		        }
		        else
		        {
			        levelButton.gameObject.SetActive(true);
			        levelButtonCurrent.gameObject.SetActive(false);
		        }
		        levelNoText.gameObject.SetActive(true);
	        }
	        else if ((id == playerData.LastUnlockedKentuckyChampionLevel + 1) && false)
	        {
		        freeLockedImage.SetActive(true);
		        lockImage.SetActive(false);
		        levelButton.gameObject.SetActive(false);
		        levelButtonCurrent.gameObject.SetActive(false);
		        levelNoText.gameObject.SetActive(false);
	        }
	        else
	        {
		        freeLockedImage.SetActive(false);
		        lockImage.SetActive(true);
		        levelButton.gameObject.SetActive(false);
		        levelButtonCurrent.gameObject.SetActive(false);
		        levelNoText.gameObject.SetActive(false);
	        }
        }
        if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.PEGASUS_CHAMPION)
        {
	        if (id <= playerData.LastUnlockedPegasusChampionLevel)
	        {
		        freeLockedImage.SetActive(false);
		        lockImage.SetActive(false);
		        if (id == playerData.CurrentSelectPegasusChampionLevel)
		        {
			        levelButtonCurrent.gameObject.SetActive(true);
			        levelButton.gameObject.SetActive(true);
		        }
		        else
		        {
			        levelButton.gameObject.SetActive(true);
			        levelButtonCurrent.gameObject.SetActive(false);
		        }
		        levelNoText.gameObject.SetActive(true);
	        }
	        else if ((id == playerData.LastUnlockedPegasusChampionLevel + 1) && false)
	        {
		        freeLockedImage.SetActive(true);
		        lockImage.SetActive(false);
		        levelButton.gameObject.SetActive(false);
		        levelButtonCurrent.gameObject.SetActive(false);
		        levelNoText.gameObject.SetActive(false);
	        }
	        else
	        {
		        freeLockedImage.SetActive(false);
		        lockImage.SetActive(true);
		        levelButton.gameObject.SetActive(false);
		        levelButtonCurrent.gameObject.SetActive(false);
		        levelNoText.gameObject.SetActive(false);
	        }
        }
  }
}
