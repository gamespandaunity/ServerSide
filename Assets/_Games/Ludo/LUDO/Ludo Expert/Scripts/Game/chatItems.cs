using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class chatItems : MonoBehaviour
{
    public Text smsText;
    public Image profilePic;
    public GameObject Chat;
    public string playfabID;

    public void profile()
    {
        GameObject.Find("noticeCanvas").GetComponent<noticeboard>().profilePanel.SetActive(true);
    }

    public void FriendsProfile()
    {
        GameObject.Find("noticeCanvas").GetComponent<noticeboard>().
            addFriendPanel.GetComponent<FriendsProfile>().loadProfile(playfabID, profilePic);

        GameObject.Find("noticeCanvas").GetComponent<noticeboard>().
            addFriendPanel.SetActive(true);
    }

    public void wait()
    {
        Invoke("init", 0.1f);
    }

    private void init()
    {

        Chat.GetComponent<RectTransform>().sizeDelta = new Vector2
        (Chat.GetComponent<RectTransform>().sizeDelta.x,
        smsText.gameObject.GetComponent<RectTransform>().sizeDelta.y + 15f);
        var layoutGroup = GameObject.Find("container").GetComponent<VerticalLayoutGroup>();
        if (layoutGroup.childScaleHeight)
            layoutGroup.childScaleHeight = false;
        else
            layoutGroup.childScaleHeight = true;
    }
}
