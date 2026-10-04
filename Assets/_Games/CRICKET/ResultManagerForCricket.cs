using Mirror;
using NetworkManagement;

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensions;

public class ResultManagerForCricket : MonoBehaviour
{
    public static ResultManagerForCricket instance;
    public RawImage Winner, Looser;
    public TextMeshProUGUI WinnerName, LooserName, popupText;

    public GameObject FinishPanel;
    public GameObject ConfettiEffect1, ConfettiEffect2;
    public GameObject FriendRequest;
    public GameObject Rematch;
    public bool isGameFinished = false;
    void Awake()
    {
        instance = this;
    }
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
    //Photon Removal [PunRPC]
    public void WinPlayer(bool IsWin, string PlayerId)
    {
        FinishPanel.SetActive(true);
        isGameFinished = true;
        if (!GameConstants.isWithAI)
        {
            //if (PhotonNetwork.InRoom)
            //{
            //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;                                                                                                                            //Photon Removal
            //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
            //    PhotonNetwork.LeaveRoom();
            //}
            ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, OnSuccess =>
                {
                    Single<int> player = JsonUtility.FromJson<Single<int>>(OnSuccess);

                    if (player.message.Contains("not"))
                    {
                        FriendRequest.SetActive(true);
                    }
                    else
                    {
                        FriendRequest.SetActive(false);
                    }
                });
            if (IsWin)
            {
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    WinnerInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                    ConfettiEffect1.SetActive(true);
                    ConfettiEffect2.SetActive(true);
                    popupText.text = "Congratulations! Your account has been credited with coins";
                    LooserInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);

                }
                else
                {
                    LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    WinnerInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                }
            }
            else
            {
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    WinnerInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                }
                else
                {
                    ConfettiEffect1.SetActive(false);
                    ConfettiEffect2.SetActive(false);
                    popupText.text = "Your balance has been deducted from your account. Please try again";
                    WinnerInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                    LooserInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);

                }
            }
        }
        else
        {
            FriendRequest.SetActive(false);
            Rematch.SetActive(false);
            if (IsWin)
            {
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    WinnerInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    LooserInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    ConfettiEffect1.SetActive(true);
                    ConfettiEffect2.SetActive(true);
                    popupText.text = "Congratulations! Your account has been credited with coins";
                    GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);
                    "YOU WIN".Show();

                }
                else
                {
                    LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    WinnerInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                }
            }
            else
            {
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    WinnerInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");

                }
                else
                {
                    WinnerInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                    LooserInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    ConfettiEffect1.SetActive(false);
                    ConfettiEffect2.SetActive(false);
                    "AI WIN".Show();
                    popupText.text = "Your balance has been deducted from your account. Please try again";
                }
            }
        }

    }

    public void leaveRoom()
    {
        MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
        if (NetworkGameManager.Instance)
            NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
        // Give the Command one frame to reach the server before tearing down the connection
        this.Delay(0.5f, () =>
        {
            NetworkManager.singleton.StopClient();
            SceneManager.LoadScene("Home");
        });
    }
    public void SendRematch()
    {

        //if (PhotonNetwork.InRoom)
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    PhotonNetwork.LeaveRoom();                                                                                                                   //Photon Removal
        //}
        //if (!PhotonNetwork.IsConnectedAndReady)
        //{
        //        PhotonNetwork.JoinLobby();
        //}
        Onleft();
    }


    public void Onleft()
    {
        //PunNetwork.HandleRoomJoined = null;
        //PunNetwork.HandleRoomJoined += OnOpenChallengeCreated;                                                                                         //Photon Removal
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
        if (ApiAndRoomManager.currentGameId == 13)
        {
            SelectOver.SelectedOver.Show("On Create Over");
            betRequestDict["overs"] = SelectOver.SelectedOver.ToString();
        }

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
            Rematch.SetActive(false);
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
    //        ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;                                                                                                                       //Photon Removal
    //        ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);
    //        // HomeMenuManager.instance.yourChallengeLoader.SetActive(false);
    //        ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    //#if !UNITY_EDITOR
    //        PhotonNetwork.LeaveRoom();
    //#endif
    //    }
    public void SendFriendRequest()
    {
        FriendRequest.SetActive(false);
        ApiAndRoomManager._instance.SendRequestToFriends(staticVariables.OpponetProfile.userId, OnSuccess =>
        {
            Single<int> response = JsonUtility.FromJson<Single<int>>(OnSuccess);
            AndroidUtility._ShowAndroidToastMessage(response.message);

        });
    }


}
