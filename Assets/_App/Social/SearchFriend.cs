using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchFriend : MonoBehaviour
{
    // Start is called before the first frame update
    public TMP_InputField finduserorEmail;
    public GameObject Friendobj;
    public Text FriendIDtxt, FriendNametxt;
    public RawImage profile;

    public void searchUserEmailPlayer()
    {
        if (!string.IsNullOrEmpty(finduserorEmail.text))
        {
            if (finduserorEmail.text == staticVariables.UserProfiledata.user._id.ToString())
                AndroidUtility._ShowAndroidToastMessage("Can't add yourself");
            else
            {
                ApiAndRoomManager._instance.GetUserInfo(finduserorEmail.text, OnSuccess =>
                {
                    if (OnSuccess == null || OnSuccess.ToString() == "{}")
                    {
                        ApiAndRoomManager._instance.DisplayError("Invalid Id! TRY ANOTHER ONE");
                    }
                    else
                    {

                        UserDataParent userData = JsonUtility.FromJson<UserDataParent>(OnSuccess);                   
                    
                        FriendIDtxt.text = userData._id.ToString();
                        FriendNametxt.text = userData.first_name + " " + userData.last_name;
                        Friendobj.SetActive(true);
                        //Debug.Log("<color=green>" + " request success search friend</color>");
                    
                    }
                },
               OnFailed =>
               {
                   ConstantsData_M.Log("request Failed search friend");
               });
            }
        }
    }
    public void SendFriendRequest()
    {
        ApiAndRoomManager._instance.SendRequestToFriends(FriendIDtxt.text);
        Friendobj.SetActive(false);

    }

}
