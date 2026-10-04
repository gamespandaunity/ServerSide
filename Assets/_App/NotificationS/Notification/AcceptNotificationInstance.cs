using Firebase.Sample.Messaging;
using NetworkManagement;
using System;
using System.Collections;
using System.Collections.Generic;
 
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Firebase.Sample.Messaging.FirebaseHandler;
 
using static SocketIOUnityAdapter;

public class AcceptNotificationInstance : MonoBehaviour
{
    public TMP_Text PlayerName;
    public Text notificationCounterTxt;
    public Text body;
    public FirebaseHandler.NotificationData notificationBody_;
    public GamePacket notificationPacket;
    string bitid;
    private NotificationUIManager notificationUiManager;

    public Image logo;
    public Sprite carromSp, eightBallSp, pokerSp, ludoSp;
    string carrom = "CARROM", eightBall = "POOL", poker = " POKER", ludo = "LUDO ";
    public AudioSource acceptSourceAudio;
    public AudioClip acceptClip;

    public RawImage rawProfileImage;
    float counter;
    public Image coin;
    public Sprite[] coinSptires;

    string roomId;
    public GameObject LoadingPanel;
    public TextMeshProUGUI acceptButtonText;
    private void OnEnable()
    {

        ConstantsData_M.Log("current time static AACEEPT INST => count down manager " + staticVariables.currentTime);
        counter = staticVariables.currentTime;
        if (counter == 0)
        {
            counter = 120;
        }
        else
        {
            counter = staticVariables.currentTime;

        }
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);

        if (SceneManager.GetActiveScene().name == "Home")
        {
            acceptButtonText.text = "VIEW";
        }
        else
        {
            acceptButtonText.text = "ACCEPT";

        }

    }
    private void Update()
    {
        if (staticVariables.isnotificationcounter)
        {

            // Constants_M.Log("isnotificationcounter" + staticVariables.isnotificationcounter);

            //CountDownTurn();
        }
        CountDownTurn();
    }
    private void Start()
    {
        acceptSourceAudio = acceptSourceAudio.GetComponent<AudioSource>();
        acceptSourceAudio.clip = acceptClip;
        // CountDownTurn();
        // Attempt to find the NotificationUIManager component in the scene
        notificationUiManager = NotificationUIManager.instance;
        staticVariables.isnotificationcounter = true;
        if (notificationUiManager == null)
        {
            ConstantsData_M.Log("NotificationUIManager not found in the scene. Make sure it's attached to a GameObject.");
        }
        else
        {
            //Debug.Log("Accept Notification instantiated");
        }

        if (SceneManager.GetActiveScene().name != "Home")
        {
            notificationPopUpBtnYes();
            "Rematch 1".Show();
        }
        //
    }
    #region set profile image


    public static string savingId;
    #endregion

    public void setProfile(FirebaseHandler.NotificationData notificationbody)
    {

        //   notificationbody = notificationbody.ToUpper();
        //body.text = NotificationUIManager.SplitingString_And_FetchOnlyImportant(notificationbody, 1);
        notificationBody_ = notificationbody;
        //  string[] NG = NotificationUIManager.SplitingString_And_FetchOnlyImportant(notificationbody, 1).Split("(");
        PlayerName.text = notificationbody.player_info_name;
        if (notificationbody.bet_type.Contains("silver"))
        {
            body.text = "Sent challenge \n" + notificationBody_.bet_amount + "  silver coins ";
            coin.sprite = coinSptires[1];
            ConstantsData_M.Log(notificationBody_.bet_amount + "silver coins ");
        }
        else
        {

            body.text = "Sent challenge \n " + notificationBody_.bet_amount + "  gold coins ";
            coin.sprite = coinSptires[0];
            ConstantsData_M.Log(notificationBody_.bet_amount + "gold coins ");
        }

        string imageUrl = $"/{notificationbody.player_info_snap}";
        ConstantsData_M.Log("accept notifucation img url" + imageUrl);
        if (SceneManager.GetActiveScene().name == "Home")
        {
            ServerConnection.DownloadSprite(imageUrl, DownloadedTexture =>
         {
             staticVariables.opponentImage = DownloadedTexture;
             rawProfileImage.texture = staticVariables.opponentImage;
             StartCoroutine(SaveDelay(DownloadedTexture));

         });
        }

    }
    public void setProfile(GamePacket packet)
    {

        //   notificationbody = notificationbody.ToUpper();

        notificationPacket = packet;
        //  string[] NG = NotificationUIManager.SplitingString_And_FetchOnlyImportant(notificationbody, 1).Split("(");
        PlayerName.text = packet.player_info_name;
        if (packet.bet_type.Contains("silver"))
        {
            body.text = "Sent challenge \n" + packet.bet_amount + "  silver coins ";
            coin.sprite = coinSptires[1];
            ConstantsData_M.Log(packet.bet_amount + "silver coins ");
        }
        else
        {

            body.text = "Sent challenge \n " + packet.bet_amount + "  gold coins ";
            coin.sprite = coinSptires[0];
            ConstantsData_M.Log(packet.bet_amount + "gold coins ");
        }

        string imageUrl = $"/{packet.player_info_snap}";
        ConstantsData_M.Log("accept notifucation img url" + imageUrl);
        if (SceneManager.GetActiveScene().name == "Home")
        {
            ServerConnection.DownloadSprite(imageUrl, DownloadedTexture =>
         {
             staticVariables.opponentImage = DownloadedTexture;
             rawProfileImage.texture = staticVariables.opponentImage;
             StartCoroutine(SaveDelay(DownloadedTexture));

         });
        }

    }

    IEnumerator SaveDelay(Texture2D img)
    {
        yield return new WaitForSeconds(0.5f);
        staticVariables.opponentImage = img;
    }

    public void AcceptNotification()
    {
        "Rematch 3".Show();


        if (SceneManager.GetActiveScene().name == "Home")
        {
            RoomsListManager.instance.GetRequests(notificationBody_ != null ? notificationBody_.transaction_id : notificationPacket.transaction_id, notificationBody_ != null ? notificationBody_.game_id : notificationPacket.game_id);
        }
        else
        {
            "Rematch 4".Show();

            acceptSourceAudio.PlayOneShot(acceptClip);
            if (notificationUiManager != null && ApiAndRoomManager._instance != null && notificationBody_ != null)
            {
                Instantiate(LoadingPanel);
                staticVariables.isnotificationcounter = false;
                ApiAndRoomManager._instance.currentRoomId = roomId;
                //Debug.Log(roomId);
                notificationBody_.bet_type.Show("bet_type");
                staticVariables.OpponetProfile = new PlayerProfile(notificationBody_.player_info_name, notificationBody_.player_info_snap, notificationBody_.bet_amount, notificationBody_.game_id.ToString(),
                notificationBody_.player_info_id.ToString(), notificationBody_.transaction_id, notificationBody_.bet_type.Contains("silver") ? false : true, "12000");
                Dictionary<string, string> props = new Dictionary<string, string>();
                props.Add("_transaction_id", notificationBody_.transaction_id.ToString());
                props.Add("_user_id", notificationBody_.player_info_id.ToString());
                props.Add("_server_address", "");
                ApiAndRoomManager._instance.notifyToplayChallenge(props, (response) =>
                {

                });
                //Photon Removal   PunNetwork.instance.OnStartGameWithPlayer(staticVariables.OpponetProfile);
                "Rematch 5".Show();

            }
            else if (notificationUiManager != null && ApiAndRoomManager._instance != null && notificationPacket != null)
            {
                Instantiate(LoadingPanel);
                staticVariables.isnotificationcounter = false;
                ApiAndRoomManager._instance.currentRoomId = roomId;
                //Debug.Log(roomId);
                notificationBody_.bet_type.Show("bet_type");
                staticVariables.OpponetProfile = new PlayerProfile(notificationPacket.player_info_name, notificationPacket.player_info_snap, notificationPacket.bet_amount, notificationPacket.game_id.ToString(),
                notificationPacket.player_info_id.ToString(), notificationPacket.transaction_id, notificationPacket.bet_type.Contains("silver") ? false : true, "12000");
                Dictionary<string, string> props = new Dictionary<string, string>();
                props.Add("_transaction_id", notificationBody_.transaction_id.ToString());
                props.Add("_user_id", notificationBody_.player_info_id.ToString());
                props.Add("_server_address", "");
                ApiAndRoomManager._instance.notifyToplayChallenge(props, (response) =>
                {

                });
                //Photon Removal  PunNetwork.instance.OnStartGameWithPlayer(staticVariables.OpponetProfile);
                "Rematch 6".Show();
            }
            else
            {
                ConstantsData_M.Log("notificationUiManager or APIManager.instance is null.");
            }
        }
        //else
        //{
        //    Constants_M.Log("Accepting-> leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);

        //    if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        //    HomeMenuManager.body = notificationBody_;           
        //    SceneLoaderUtility.LoadScene("Home");
        //}
        Destroy(gameObject);
    }



    public void notificationPopUpBtnYes()
    {
        if (notificationBody_ != null)
        {
            roomId = notificationBody_.transaction_id;
            acceptSourceAudio.PlayOneShot(acceptClip);
            AcceptNotification();
            "Rematch 2".Show();

        }
        else if (notificationPacket != null)
        {
            roomId = notificationPacket.transaction_id;
            acceptSourceAudio.PlayOneShot(acceptClip);
            AcceptNotification();
            "Rematch 2".Show();
        }
        else
        {
            ConstantsData_M.Log("Invalid format for result array");
        }
        _instance.challengeNotificationsList.Clear();
        challengeNotificationInProcess = false;

    }


    public void notificatimonPopUpBtnNo()
    {
        if (notificationBody_ != null)
        {

            roomId = notificationBody_.transaction_id;


            if (notificationUiManager != null && ApiAndRoomManager._instance != null)
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
        else if (notificationPacket != null)
        {
            roomId = notificationPacket.transaction_id;


            if (notificationUiManager != null && ApiAndRoomManager._instance != null)
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
        _instance.challengeNotificationsList.RemoveAt(0);
        challengeNotificationInProcess = false;
        _instance.ShowNextChallengeNotification();
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
                staticVariables.isnotificationcounter = false;
                ApiAndRoomManager._instance.IgnoretChallenge(notificationBody_.transaction_id, notificationBody_.player_info_id.ToString());
                NotificationUIManager.instance.acceptNotificationPopUp.SetActive(false);

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

