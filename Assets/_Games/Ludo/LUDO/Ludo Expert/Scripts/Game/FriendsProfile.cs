using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LudoGame;
//using Photon.Pun;

public class FriendsProfile : MonoBehaviour
{
    public Image avater;
    public Text PlayerName, playfabId, GamePlayed, GameWin, GameLost, twoPlayer, fourPlayer;
    public GameObject waitText, addedFriendWindow, DetailsPanel;
    private string friendsID;

    private void OnEnable()
    {
        waitText.SetActive(true);
        DetailsPanel.SetActive(false);
    }

    public void loadProfile(string id, Image pic)
    {
        friendsID = id;

        avater.sprite = pic.sprite;

        GetUserDataRequest getdatarequest = new GetUserDataRequest()
        {
            PlayFabId = id,

        };

        PlayFabClientAPI.GetUserData(getdatarequest, (result) =>
        {

            Dictionary<string, UserDataRecord> data = result.Data;

            PlayerName.text = data["PlayerName"].Value.ToString();
            playfabId.text = "Player ID : " + id;
            GamePlayed.text = data["GamesPlayed"].Value.ToString();

            GameWin.text = (int.Parse(data["TwoPlayerWins"].Value.ToString())
            + int.Parse(data["FourPlayerWins"].Value.ToString())).ToString();

            GameLost.text = (int.Parse(data["GamesPlayed"].Value.ToString()) -
             +int.Parse(data["TwoPlayerWins"].Value.ToString())
            + int.Parse(data["FourPlayerWins"].Value.ToString())).ToString();

            twoPlayer.text = data["TwoPlayerWins"].Value.ToString();
            fourPlayer.text = data["FourPlayerWins"].Value.ToString();

            waitText.SetActive(false);
            DetailsPanel.SetActive(true);

        }, (error) =>
        {
            Debug.Log("Data updated error " + error.ErrorMessage);
        }, null);

    }

    //public void AddToFriend()
    //{
    //    AddFriendRequest request = new AddFriendRequest()
    //    {
    //        FriendPlayFabId = friendsID,
    //    };

    //    PlayFabClientAPI.AddFriend(request, (result) =>
    //    {
    //        PhotonNetwork.RaiseEvent((int)EnumPhoton.AddFriend, PhotonNetwork.NickName + ";" + LudoGame.GameManager.Instance.nameMy + ";" + friendsID, true, null);
    //        addedFriendWindow.SetActive(true);
    //        Debug.Log("Added friend successfully");
    //    }, (error) =>
    //    {
    //        addedFriendWindow.SetActive(true);
    //        Debug.Log("Error adding friend: " + error.Error);
    //    }, null);
    //}
}

