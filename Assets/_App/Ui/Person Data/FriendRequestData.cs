using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NetworkManagement;
using System.Collections;
 
 
using DG.Tweening;

public class FriendRequestData : MonoBehaviour
{
    public RectTransform setter;
    [Serializable]
    public class RequestsData
    {
        public GameObject Winning;
        public GameObject Matches;
        public TextMeshProUGUI silverWinnings, GoldWinnings;
        public TextMeshProUGUI silverPlayed, goldPlayed;
        public GameObject challengeButton;
    }



    public RequestsData Requestsdata;

    public bool isActive = false;
    public TextMeshProUGUI playerName;
    public RawImage profileImage;
    [HideInInspector]
    public string profileURL;
    public FriendsModel frienddata;
    public GameObject SmallChallenge_, CoinSelection_CreateChallenge;
    GameObject gb;
    public Image isonlineimg;
    private void OnEnable()
    {
        ConstantsData_M.Log("BORROW PREF  VAL" + UIMainMenManager.instance.Borrow);
        if (/*UIMainMenManager.instance.Borrow == 1*/UIMainMenManager.instance.currentPanelState == UIMainMenManager.PanelState.borrowState)
        {
            //   Requestsdata.challengeButton.gameObject.SetActive(false);
            // Requestsdata.borrowButton.gameObject.SetActive(true);
        }
        if (UIMainMenManager.instance.currentPanelState == UIMainMenManager.PanelState.challengeBorrowState)
        {
            //  Requestsdata.borrowButton.gameObject.SetActive(true);
            // Requestsdata.challengeButton.gameObject.SetActive(true);
        }
    }
    public async void Init(FriendsModel frienddata)
    {
        this.frienddata = frienddata;
        string name = frienddata.first_name + " " + frienddata.last_name;
        playerName.text = name;
        profileURL = "/" + frienddata.file_url;
        Requestsdata.silverPlayed.text = frienddata.silver_played.ToString();
        Requestsdata.goldPlayed.text = frienddata.gold_played.ToString();
        Requestsdata.GoldWinnings.text = frienddata.gold_win.ToString();
        Requestsdata.silverWinnings.text = frienddata.silver_win.ToString();
       
    }
   
    public void Start()
    {
        ServerConnection.DownloadSprite(profileURL, DownImage =>
        {
            profileImage.texture = DownImage;
        });
    }

    //void OnChallengeSend(Photon.Realtime.Room room)
    //{
    //    PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);
    //    ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;
    //    ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);
    //    HomeMenuManager.instance.yourChallengeLoadingIndicator.SetActive(false);
    //    PhotonNetwork.CurrentRoom.IsVisible = false;                                                                                                                                                                                                //Photon Removal
    //    ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);

    //    PhotonNetwork.LeaveRoom();
    //}
    #region CHALLENGE SEND
    public void CreatingRoom()
    {
        gb = Instantiate(CoinSelection_CreateChallenge, UserFriendList.instance.gameObject.transform);
        gb.SetActive(true);

        gb.GetComponent<SmallChallenge>().frienddata = frienddata;
        ConstantsData_M.Log(" NAME =>" + frienddata.first_name);

        gb.transform.DOScale(1, 0.25f);
        SoundManagerMain.instance.ClickSoundPlay();
    }

    #endregion

    public void SendGifts()
    {
        UIMainMenManager.instance.openPanelByName("Gift");
        staticVariables.GiftRecieverId = frienddata;
      
        SoundManagerMain.instance.ClickSoundPlay();

    }

    public void CloseSmallChallenge()
    {
        SmallChallenge_.gameObject.SetActive(false);
        SmallChallenge_.transform.localScale = Vector3.zero;
    }
}
