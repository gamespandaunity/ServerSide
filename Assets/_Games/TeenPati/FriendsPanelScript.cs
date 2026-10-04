using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FriendsPanelScript : MonoBehaviour
{


    public GameObject facebookFriendPanel;
    public GameObject dreamFirendpanel;
    public GameObject addNewFriend;

    public GameObject FbFriendGold;
    public GameObject dreamFriendBtn;
    public GameObject newFriendBtn;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ActivemyBtnandPanel(string firendPanel, string btnPanle)
    {
        facebookFriendPanel.SetActive(firendPanel.Equals(facebookFriendPanel.name));
        dreamFirendpanel.SetActive(firendPanel.Equals(dreamFirendpanel.name));
        addNewFriend.SetActive(firendPanel.Equals(addNewFriend.name));

        FbFriendGold.SetActive(firendPanel.Equals(FbFriendGold.name));
        dreamFriendBtn.SetActive(firendPanel.Equals(dreamFriendBtn.name));
        newFriendBtn.SetActive(firendPanel.Equals(newFriendBtn.name));
    }
}
