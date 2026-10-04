using UnityEngine;
using System.Collections;
using PlayFab.ClientModels;
using PlayFab;
using UnityEngine.SceneManagement;
using AssemblyCSharp;
using LudoGame;
//using Photon.Pun;
using ExitGames.Client.Photon;
//using Photon.Realtime;

public class PlayFabAddFriend : MonoBehaviour
{

    public GameObject menuObject;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {


    }

    public void AddFriend()
    {
        menuObject.GetComponent<Animator>().Play("hideMenuAnimation");
        if (!LudoGame.GameManager.Instance.offlineMode)
        {
            //PhotonNetwork.RaiseEvent(192, 1, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);



            AddFriendRequest request = new AddFriendRequest()
            {
                //FriendPlayFabId = PhotonNetwork.PlayerListOthers[0].NickName
            };



            PlayFabClientAPI.AddFriend(request, (result) =>
            {
                Debug.Log("Added friend successfully");
                LudoGame.GameManager.Instance.friendButtonMenu.SetActive(false);
                LudoGame.GameManager.Instance.smallMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(LudoGame.GameManager.Instance.smallMenu.GetComponent<RectTransform>().sizeDelta.x, 260.0f);
            }, (error) =>
            {
                Debug.Log("Error adding friend: " + error.Error);
            }, null);
        }

    }

    public void showMenu()
    {
        menuObject.GetComponent<Animator>().Play("ShowMenuAnimation");
    }

    public void hideMenu()
    {
        menuObject.GetComponent<Animator>().Play("hideMenuAnimation");
    }

    public void LeaveGame()
    {
        // if (StaticStrings.showAdWhenLeaveGame)
        //     AdsManager.Instance.adsScript.ShowAd();
        SceneManager.LoadScene("MenuScene");
        //PhotonNetwork.KeepAliveInBackground = StaticStrings.photonDisconnectTimeoutLong; 
        Debug.Log("Timeout 3");
        //LudoGame.GameManager.Instance.cueController.removeOnEventCall();
        //PhotonNetwork.LeaveRoom();

        LudoGame.GameManager.Instance.playfabManager.roomOwner = false;
        LudoGame.GameManager.Instance.roomOwner = false;
        LudoGame.GameManager.Instance.resetAllData();

    }
}
