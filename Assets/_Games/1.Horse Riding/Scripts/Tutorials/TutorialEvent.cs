using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEvent : MonoBehaviour
{
   public HorseUIController RccUiController;
   public GameObject runTutorial;

   private void Update() 
   {
      if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.DUABI_CHAMPION && MultiPlayerGame.isChampion && MConstants.CurrentLevelNumber == 1)
      {
         if (RccUiController.pressing)
         {
            runTutorial.SetActive(false);
         }
         else
         {
            runTutorial.SetActive(true);
         }
      }
   }
}
