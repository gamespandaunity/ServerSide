using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DirectInvite : MonoBehaviour
{
    // Start is called before the first frame update
    public Text finduserorEmail;
    public Text FriendIDtxt, FriendNametxt;
    public InputField SendDirectInvite_BetInput;
    public Button sendchallengebtn;
    public GameObject UserFetchedPanel;

    void Start()
    {

    }
    public void searchUserEmailPlayer()
    {

        if (!string.IsNullOrEmpty(finduserorEmail.text))
        {
            if (finduserorEmail.text == staticVariables.UserProfiledata.user._id.ToString())
                AndroidUtility._ShowAndroidToastMessage("Invite Can't be Send to yourself");
            else
            {
                ApiAndRoomManager._instance.GetUserInfo(finduserorEmail.text, OnSuccess =>
                {
                    UserDataParent userData = JsonUtility.FromJson<UserDataParent>(OnSuccess);
                    FriendIDtxt.text = userData._id.ToString();
                    FriendNametxt.text = userData.first_name + " " + userData.last_name;
                },
               OnFailed =>
               {
                   ConstantsData_M.LogCritical("request Failed search friend");
               });
            }
        }
    }
    public void sendChallengeToFriendBtn()
    {
        sendchallengebtn.interactable = false;
        if (!string.IsNullOrEmpty(SendDirectInvite_BetInput.text))
        {
            int coinsqty = int.Parse(SendDirectInvite_BetInput.text);

            if (coinsqty >= 20)
            {

                EightBallPoolNetworkManager.mainPlayer.prize = coinsqty;
               // HomeMenuManager.instance.CreateRoom(int.Parse(FriendIDtxt.text));
                //APIManager.instance.send
            }
            else
            {
                sendchallengebtn.interactable = true;

                //  errorMsgPopupPanel.SetActive(true);
                // errorMsgTxt.text = "Please enter at least 20 coins";

            }
        }
        else
        {
            sendchallengebtn.interactable = true;

            //  errorMsgPopupPanel.SetActive(true);
            //   errorMsgTxt.text = "Please enter valid Coins";

        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}
