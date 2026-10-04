using UnityEngine;
using UnityEngine.UI;
 
 
using UnityEngine.SceneManagement;

public class CountDownManager : MonoBehaviour
{


    public Text counterTxt;
    public GameObject WaitingopponentPanel;
    public static CountDownManager instance;


    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    private void OnEnable()
    {

        staticVariables.currentTime = 120;
        //APIManager.instance.CurrentBetResponse.data.bet_expires_sec;

    }
    void Update()
    {
        CountDownTurn();
    }



    public void CountDownTurn() // countdown
    {
   
        if (staticVariables.currentTime > 0)
        {
            staticVariables.currentTime -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(staticVariables.currentTime / 60f);
            int seconds = Mathf.FloorToInt(staticVariables.currentTime % 60f);
            counterTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (staticVariables.currentTime <= 1)
            {
                staticVariables.isCounterFlag = false;
                staticVariables.currentTime = 120;
                PopupMessageManager.instance.ShowPopUp( "Your time is up, please try again.",timeToShow:2);
                //Photon Removal if (PhotonNetwork.InRoom)
                {
                    //Photon Removal     PhotonNetwork.LeaveRoom();
                }
                ApiAndRoomManager._instance.LeaveChallenge(WaitingPanelScript.WaitingForroomID);
                Destroy(WaitingopponentPanel, 0.1f);
                return;

            }
        }
        else
        {
            counterTxt.text = "Time's up";
            Destroy(WaitingopponentPanel, 0.1f);

        }
    }

}
