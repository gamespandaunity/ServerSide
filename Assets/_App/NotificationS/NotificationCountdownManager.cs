using UnityEngine;
using UnityEngine.UI;

public class NotificationCountdownManager : MonoBehaviour
{
    public Text notificationCounterTxt;

    public static NotificationCountdownManager instance;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    void Update()
    {
        if (staticVariables.isnotificationcounter)
        {
            CountDownTurn();
        }
    }



    public void CountDownTurn() // countdown
    {
        ////print(" notification counter 111111111111111:::::" + staticVariables.currentTimebetExpire);

        if (staticVariables.currentTimebetExpire > 0)
        {
            ////print(" notification counter 2222:::::" + staticVariables.currentTimebetExpire);
            staticVariables.currentTimebetExpire -= Time.deltaTime;
            // countdown timer 

            int minutes = Mathf.FloorToInt(staticVariables.currentTimebetExpire / 60f);
            int seconds = Mathf.FloorToInt(staticVariables.currentTimebetExpire % 60f);
            if(notificationCounterTxt) 
            notificationCounterTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (staticVariables.currentTimebetExpire <= 1)
            {
                ////print("counter 33333:::::" + staticVariables.currentTimebetExpire);
                staticVariables.isnotificationcounter = false;

                //   APIManager.instance.IgnoretBet(notificationBody_.transaction_id, notificationBody_.player_info_id.ToString());
                if (NotificationUIManager.instance.acceptNotificationPopUp)
                    NotificationUIManager.instance.acceptNotificationPopUp.SetActive(false);

                return;

            }
        }
        else
        {
            if (notificationCounterTxt)
                notificationCounterTxt.text = "Time's up";
            if (NotificationUIManager.instance.acceptNotificationPopUp)
                NotificationUIManager.instance.acceptNotificationPopUp.SetActive(false);
        }
    }
}
