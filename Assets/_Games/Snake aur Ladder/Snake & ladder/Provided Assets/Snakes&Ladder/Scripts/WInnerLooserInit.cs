
using Mirror;
using NetworkManagement;
using Snake_Ladder;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Twelve;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensions;

public class WInnerLooserInit : NetworkBehaviour
{
    public RawImage Winner, Looser;
    public TextMeshProUGUI WinnerName, LooserName, popupText;
    public GameControllerNew GameController;
    public GameObject FinishPanel;
    //   public GameObject FriendRequest;
    public GameObject Rematch;
    bool gameOver;
    public static bool isGameFinished = false;
    public void WinnerInitialize(Texture2D pp, string name)
    {
        Winner.texture = pp;
        WinnerName.text = name;
    }
    public void LooserInitialize(Texture2D pp, string name)
    {
        Looser.texture = pp;
        LooserName.text = name;
    }
    //Photon Removal  [PunRPC]
    public void WinPlayer(bool IsWin, string PlayerId)
    {
        FinishPanel.SetActive(true);
        isGameFinished = true;
        if (GameController.isOnline && !gameOver)
        {
            //  Debug.Log("isSinglePlayer " + MultiPlayerGame.isSinglePlayer);
            Rematch.SetActive(true);
            NetworkGameManager.OnRematchRecived += RematchRequestRecived;
            // HorseMirrorGameManager.PlayersHorseData = new Dictionary<string, HorseMirrorGameManager.HorseRecord>();
            gameOver = true;
            if (IsWin)
            {
                //ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, OnSuccess =>
                //{
                //    Single<int> player = JsonUtility.FromJson<Single<int>>(OnSuccess);

                //    if (player.message.Contains("not"))
                //    {
                //        FriendRequest.SetActive(true);
                //    }
                //    else
                //    {
                //        FriendRequest.SetActive(false);
                //    }
                //});
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    Debug.LogError("Win PlayerId: " + PlayerId);
                    WinnerInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                    ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                    popupText.text = "Congratulations! Your account has been credited with coins";
                    LooserInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    Debug.LogError("Winner PlayerId: " + PlayerId);

                }
                else
                {
                    Debug.LogError("Losse PlayerId: " + PlayerId);
                    //LooserInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                    WinnerInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    popupText.text = "Your balance has been deducted from your account. Please try again";
                    Debug.LogError("Looser PlayerId: " + PlayerId);
                }
            }
            else
            {
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    Debug.LogError("1Win PlayerId: " + PlayerId);
                    LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    WinnerInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    Debug.LogError("1Looser PlayerId: " + PlayerId);
                }
                else
                {
                    Debug.LogError("2Win PlayerId: " + PlayerId);
                    Debug.LogError("1Win PlayerId: " + PlayerId);
                    ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId);
                    popupText.text = "Your balance has been deducted from your account. Please try again";


                }
            }
        }
        else
        {
            Debug.Log("isSinglePlayer " + GameGUIController.insta.Ismultiplayer);
            //FriendRequest.SetActive(false);
            Rematch.SetActive(false);
            if (IsWin)
            {
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    Debug.LogError("3Win PlayerId: " + PlayerId);
                    WinnerInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                    popupText.text = "Congratulations! Your account has been credited with coins";
                    LooserInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);
                    "YOU WIN".Show();

                }
                else
                {
                    Debug.LogError("3Win PlayerId: " + PlayerId);
                    LooserInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                }
            }
            else
            {
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    Debug.LogError("4Looser PlayerId: " + PlayerId);
                    LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                }
                else
                {
                    Debug.LogError("4Win PlayerId: " + PlayerId);
                    WinnerInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    //GuestGrandScripts.instance.ChangeGuestCoins(-staticVariables.currentPrize);
                    ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
                    LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    "AI WIN".Show();
                    popupText.text = "Your balance has been deducted from your account. Please try again";
                }
            }
        }
        //if (PhotonNetwork.InRoom)
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;                                                                                        //Photon Removal
        //    PhotonNetwork.LeaveRoom();
        //}
    }

    public void leaveRoom()
    {
        SceneManager.LoadScene("Home");
        //Photon Removal  PhotonNetwork.LeaveRoom();
    }
    bool sender;
    public void RequestRematch()
    {
        sender = true;
        NetworkGameManager.Instance.CmdRequestRematch();

        //if (PhotonNetwork.InRoom)                                                                                                                                                   //Photon Removal
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    PhotonNetwork.LeaveRoom();
        //}
        // OnPlayerLeftRoom();
    }
    public void RematchRequestRecived()
    {
        if (!sender)
        {
            PopupMessageManager.instance.ReconnectionStatus(true, () =>
            {
                NetworkGameManager.Instance.CmdStartRematch();
            }, () =>
            {
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                if (NetworkGameManager.Instance)
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                this.Delay(1, () => NetworkManager.singleton.StopClient());
                SceneManager.LoadScene("Home");
            });

        }
    }

    public void Onleft()
    {
        //Photon Removal   PunNetwork.HandleRoomJoined = null;
        //Photon Removal  PunNetwork.HandleRoomJoined += OnOpenChallengeCreated;
        {
            EightBallPoolNetworkManager.mainPlayer.prize = EightBallPoolNetworkManager.mainPlayer.prize;
        }
        staticVariables.screenstatus = "sad";
        Dictionary<string, string> betRequestDict = new Dictionary<string, string>();
        betRequestDict["first_player"] = staticVariables.UserProfiledata.user._id.ToString();
        betRequestDict["second_player"] = staticVariables.lastOpponentId.ToString();
        betRequestDict["game_id"] = ApiAndRoomManager.currentGameId.ToString();
        betRequestDict["remark"] = "Multipler with Golden Coins";
        betRequestDict["screenstatus"] = staticVariables.screenstatus;
        betRequestDict["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver";

        if (staticVariables.isgoldcoins)
        {
            betRequestDict["gold"] = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
            betRequestDict["silver"] = "0";
        }
        else
        {
            betRequestDict["silver"] = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
            betRequestDict["gold"] = "0";
        }

        ApiAndRoomManager._instance.create_Challenge_Invite(betRequestDict,
        successMessage =>
        {
            ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 1reate success room---");
            ApiAndRoomManager._instance.ModifyUserBalance();
            ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 2reate success room---");
            ConstantsData_M.Log("Bet ID");
            // Rematch.SetActive(false);
            AndroidUtility._ShowAndroidToastMessage("Rematch Request Send");
        },
        failureMessage =>
        {

            ApiAndRoomManager._instance.DisplayError("failureMessage");

            //Debug.Log("Gold Bet Failed to Create: " + failureMessage);

        });
        SoundManagerMain.instance.ClickSoundPlay();
    }

    //    void OnOpenChallengeCreated(Photon.Realtime.Room room)
    //    {
    //        PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);
    //        ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;
    //        ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);                                                                          //Photon Removal
    //        // HomeMenuManager.instance.yourChallengeLoader.SetActive(false);
    //        ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    //#if !UNITY_EDITOR
    //        PhotonNetwork.LeaveRoom();
    //#endif
    //    }
    public void SendFriendRequest()
    {
        //  FriendRequest.SetActive(false);
        ApiAndRoomManager._instance.SendRequestToFriends(staticVariables.OpponetProfile.userId, OnSuccess =>
        {
            Single<int> response = JsonUtility.FromJson<Single<int>>(OnSuccess);
            AndroidUtility._ShowAndroidToastMessage(response.message);

        });
    }

}
