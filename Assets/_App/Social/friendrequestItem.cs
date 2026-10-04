using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class friendrequestItem : MonoBehaviour
{
    public Button reject ,accept;
    public TMP_Text ChallengeText;
    public string request_ID = "";
    public RawImage profilepicture;
    public void AcceptRequest()
    {
        accept.interactable= false;
        reject.interactable= false;
        ApiAndRoomManager._instance.ApproveFriendRequest(request_ID, OnSuccess =>
        {
            Destroy(this.gameObject);
        });
    }
    public void Init(string request_ID,string UserProfileurl)
    {
        request_ID.Show("Request ID Show");
       this.request_ID = request_ID;
       ServerConnection.DownloadSprite("/" + UserProfileurl, OnDownloaded =>
        {
            profilepicture.texture = OnDownloaded;
        });
    }
    public void RejectRequest()

    {
        accept.interactable = false;
        reject.interactable = false;
        ApiAndRoomManager._instance.DeclineFriendRequest(request_ID, OnSuccess =>
        {
            Destroy(this.gameObject);

        });
    }
}
