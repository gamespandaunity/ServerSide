using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using static Firebase.Sample.Messaging.FirebaseHandler;
using NetworkManagement;
using System.Collections.Generic;
 
using System;
 

public class InviteAcceptNotificationInstance : MonoBehaviour
{
    public Text notificationBody, notificationCounterTxt;
    public Firebase.Sample.Messaging.FirebaseHandler.NotificationData body;
    bool lobbyjoined = false;
    public Image logo;
    public Sprite carromSp, eightBallSp, pokerSp, ludoSp;
    string carrom = "CARROM", eightBall = "POOL", poker = " POKER", ludo = "LUDO ";
    public AudioSource acceptSourceAudio;
    public AudioClip acceptClip;
    float counter;
    public string imageUrl;
    public RawImage rawProfileImage;
    string roomId;
    public GameObject LoadingPanel;

    private void Start()
    {
        //print("InviteAcceptNotificationInstance");
        acceptSourceAudio.clip = acceptClip;
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);
    }
    private void OnEnable()
    {
        //print("current time static  INVITE INST=> count down manager " + staticVariables.currentTime);
        counter = staticVariables.currentTime;
    }



    public void setProfile(Firebase.Sample.Messaging.FirebaseHandler.NotificationData _body)
    {
        body = _body;
        ServerConnection.DownloadSprite($"/{_body.player_info_snap}", DownloadedTexture =>
        {
            rawProfileImage.texture = DownloadedTexture;
            staticVariables.opponentImage = DownloadedTexture;
        });
    }


    private void Update()
    {
        CountDownTurn();
    }
    public void notificatimonPopUpBtnNo()
    {
        if (body != null)
        {

            roomId = body.transaction_id;


            if (NotificationUIManager.instance != null && ApiAndRoomManager._instance != null)
            {
                staticVariables.isnotificationcounter = false;
                ApiAndRoomManager._instance.currentRoomId = roomId;
                ApiAndRoomManager._instance.RejectChallenge(roomId, onSuccess =>
                {

                });
                Destroy(this.gameObject);
            }
            else
            {
                ConstantsData_M.Log("NotificationUIManager or APIManager.instance is null");
            }
        }
    }

    public void inviteNotificationPopUpAcceptBtn()
    {
        //PunNetwork.instance.Cleanup();
        //if (PhotonNetwork.InRoom)                                                                                                                                                      //Photon Removal
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    PhotonNetwork.LeaveRoom();
        //}
        transform.GetChild(0).gameObject.SetActive(false);
        //staticVariables.isSearchFriendIdAndEmailAddress = true;
        staticVariables.isInviteFriendAndPlay = false;
        staticVariables.inviteBetId = body.transaction_id;
        staticVariables.isnotificationcounter = false;
        Dictionary<string, string> betjoinDict = new Dictionary<string, string>();
        // Assign values to the BetRequest fields
        body.transaction_id.Show();
        betjoinDict["transaction_id"] = body.transaction_id;
        betjoinDict["second_player"] = staticVariables.UserProfiledata.user._id.ToString();
        betjoinDict["second_player_info"] = ApiAndRoomManager._instance.MessageContent;
        ApiAndRoomManager._instance.Join_Challenge(betjoinDict,
            OnJoinSucess =>
            {
                "1".Show();
                staticVariables.OpponetProfile = new PlayerProfile(body.player_info_name, body.player_info_snap, body.bet_amount, body.game_id.ToString(),
                  body.player_info_id.ToString(), body.transaction_id, body.bet_type.Contains("silver") ? false : true, "12000");
                "2".Show();
                ApiAndRoomManager._instance.winLoseChallengeId = body.transaction_id;
                "3".Show();
                body.bet_type.Show("Bet type");
                staticVariables.isnotificationcounter = false;
                staticVariables.isgoldcoins = body.bet_type.Contains("silver") ? false : true;
                ApiAndRoomManager._instance.currentRoomId = roomId;
                "4".Show();
                //   GameObject gb = Instantiate(SpritesManager.Instance.spritesScriptable.waitingForOpponentPanel, HomeMenuManager.instance.mainCanvasObject.transform);
                "5".Show();
                WaitingPanelScript.WaitingForroomID = roomId;
                "6".Show();
            }
            );

        acceptSourceAudio.Play();

        Destroy(gameObject);
    }
    public void CountDownTurn() // countdown
    {
        if (counter > 0)
        {
            counter -= Time.deltaTime;
            // countdown timer 
            int minutes = Mathf.FloorToInt(counter / 60f);
            int seconds = Mathf.FloorToInt(counter % 60f);
            notificationCounterTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (counter <= 1)
            {
                ////print("counter 33333:::::" + staticVariables.currentTimebetExpire);
                staticVariables.isnotificationcounter = false;
                ApiAndRoomManager._instance.IgnoretChallenge(body.transaction_id, body.player_info_id.ToString());
                NotificationUIManager.instance.InviteAcceptNotificationPopUp.SetActive(false);
                return;
            }
        }
        else
        {
            notificationCounterTxt.text = "Time's up";
            Destroy(gameObject);
        }
    }

}
