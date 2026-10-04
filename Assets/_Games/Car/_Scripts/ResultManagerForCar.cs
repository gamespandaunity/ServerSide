using Mirror;
using NetworkManagement;
 
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ResultManagerForCar :NetworkBehaviour //Photon Removal: MonoBehaviourPun
{
    public static ResultManagerForCar instance;

    [FormerlySerializedAs("Winner")] public RawImage winnerAvatar;
    [FormerlySerializedAs("Looser")] public RawImage loserAvatar;


    [FormerlySerializedAs("WinnerName")] public TextMeshProUGUI winnerNameText;
    [FormerlySerializedAs("LooserName")] public TextMeshProUGUI loserNameText;
    [FormerlySerializedAs("popupText")] public TextMeshProUGUI resultPopupText;

    [FormerlySerializedAs("FinishPanel")] public GameObject finishPanelUI;

   

    [FormerlySerializedAs("finish")] private bool isFinishTriggered  = false;
    [FormerlySerializedAs("FriendRequest")] public GameObject friendRequestButton;
    [FormerlySerializedAs("Rematch")] public GameObject rematchButton;
    public static bool IsGameWin=false;
    void Awake()
    {
        instance = this;
    }
  //  public void WinnerInitialize(Texture2D pp, string name)
    public void InitializeWinnerUI(Texture2D pp, string name)
    {
        winnerAvatar.texture = pp;
        winnerNameText.text = name;
    }
  //  public void LooserInitialize(Texture2D pp, string name)
    public void InitializeLoserUI(Texture2D pp, string name)
    {
        loserAvatar.texture = pp;
        loserNameText.text = name;
    }

    
    
    //Photon Removal   [PunRPC]
    public void HandleGameResultAlt(bool IsWin, string PlayerId)
    {
        if (isFinishTriggered  == false)
        {
           
            IsGameWin = true;
            CarRace.MultiPlayerGameManager.PlayersCarsData = new Dictionary<string, CarRace.MultiPlayerGameManager.CarRecord>();
            CarRace.MultiPlayerGameManager.gameStarted = false;

            isFinishTriggered = true;
             Debug.Log("IsGameWin" + IsGameWin);
            "Finish Panel".Show();
            finishPanelUI.SetActive(true);
            if (CarRace.PlayerSelections.GameMode == 1)
            {
                 NetworkGameManager.OnRematchRecived += RematchRequestRecived;
                rematchButton.SetActive(true);
                ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, OnSuccess =>
                {
                    Single<int> player = JsonUtility.FromJson<Single<int>>(OnSuccess);

                    if (player.message.Contains("not"))
                    {
                        friendRequestButton.SetActive(true);
                    }
                    else
                    {
                        friendRequestButton.SetActive(false);
                    }
                });
                if (IsWin)
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);
                      
                        resultPopupText.text = "Congratulations! Your account has been credited with coins";
                        InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());

                    }
                    else
                    {
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);
                        resultPopupText.text = "Your balance has been deducted from your account. Please try again";
                        InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    }
                }
                else
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        resultPopupText.text = "Your balance has been deducted from your account. Please try again";
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);
                        InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId);
                    }
                    else
                    {
                     
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);
                        resultPopupText.text = "Congratulations! Your account has been credited with coins";

                        InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    }
                }


                //if (PhotonNetwork.InRoom)
                //{
                //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;                                                                            //Photon Removal
                //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
                //    PhotonNetwork.LeaveRoom();
                //}
            }
            else
            {
                rematchButton.SetActive(false);
                friendRequestButton.SetActive(false);
                if (IsWin)
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);
                      
                        resultPopupText.text = "Congratulations! Your account has been credited with coins";
                        InitializeLoserUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        "YOU WIN".Show();

                    }
                    else
                    {
                        InitializeLoserUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    }
                }
                else
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);
                        InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    }
                    else
                    {
                        InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
                        
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);
                        "AI WIN".Show();
                        resultPopupText.text = "Your balance has been deducted from your account. Please try again";
                    }
                }
            }
        }
        //if (PhotonNetwork.InRoom)
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;                                                                                                                                //Photon Removal
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    PhotonNetwork.LeaveRoom();
        //}
    }

    //  public void leaveRoom()
    public void LeaveMultiplayerRoom()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            Debug.LogError("photon.leave room");
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            if (NetworkGameManager.Instance)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
            // this.Delay(1, () => NetworkManager.singleton.StopClient());
            NetworkGameManager.Instance?.CmdReloadServer();
            Time.timeScale = 1f;
            //  this.Delay(1, () => NetworkManager.singleton.StopClient());
            IsGameWin = false;
            isFinishTriggered = false;
            SceneManager.LoadScene("Home");
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("AI GameLeave");
            SceneManager.LoadScene("Home");
        }
        //if (NetworkServer.active)
        //{
        //    CmdLeaveMultiplayerRoom();
        //    Debug.Log("CmdLeaveMultiplayerRoom");
        //}
        //else
        //{
        //    RpcIsGameWin();
        //    SceneManager.LoadScene("Home");
        //}        
        //SceneManager.LoadScene("Home");
        //Photon Removal   PhotonNetwork.LeaveRoom();
    }
    [Command(requiresAuthority = false)]
    public void CmdLeaveMultiplayerRoom()
    {
        RpcLeaveMultiplayerRoom();
    }
    [ClientRpc]
    public void RpcLeaveMultiplayerRoom()
    {
        Debug.Log("RpcIsGameWin" + IsGameWin);
        SceneManager.LoadScene("Home");
    }
    [ClientRpc]
    public void RpcIsGameWin()
    {
        IsGameWin = true;
         Debug.Log("RpcIsGameWin" + IsGameWin);
       
    }
    bool sender;
    public void RequestRematch()
    {
        sender =true;
        NetworkGameManager.Instance.CmdRequestRematch();
        //if (PhotonNetwork.InRoom)
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;                                                                                                                                               //Photon Removal
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    PhotonNetwork.LeaveRoom();
        //}
      //  OnPlayerLeftRoom();
    }
     public void RematchRequestRecived()
    {
        if (!sender)
        {
          
        PopupMessageManager.instance.ReconnectionStatus(true, () =>
            {
                NetworkGameManager.Instance. CmdStartRematch();
            }, () =>
            {
                SceneManager.LoadScene("Home");
            });
        }
    }


    //public void OnPlayerLeftRoom()
    //{
    //    //PunNetwork.HandleRoomJoined = null;                                                                                                                                            //Photon Removal
    //    //PunNetwork.HandleRoomJoined += HandleOpenChallengeCreated;
       
    //    staticVariables.screenstatus = "sad";
    //    Dictionary<string, string> betRequestDict = new Dictionary<string, string>();
    //    betRequestDict["first_player"] = staticVariables.UserProfiledata.user._id.ToString();
    //    betRequestDict["second_player"] = staticVariables.lastOpponentId.ToString();
    //    betRequestDict["game_id"] = ApiAndRoomManager.currentGameId.ToString();
    //    betRequestDict["remark"] = "Multipler with Golden Coins";
    //    betRequestDict["screenstatus"] = staticVariables.screenstatus;
    //    betRequestDict["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver";

    //    if (staticVariables.isgoldcoins)
    //    {
    //        betRequestDict["gold"] = NetworkManager.mainPlayer.prize.ToString();
    //        betRequestDict["silver"] = "0";
    //    }
    //    else
    //    {
    //        betRequestDict["silver"] = NetworkManager.mainPlayer.prize.ToString();
    //        betRequestDict["gold"] = "0";
    //    }

    //    ApiAndRoomManager._instance.create_Challenge_Invite(betRequestDict,
    //    successMessage =>
    //    {
    //        ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 1reate success room---");
    //        ApiAndRoomManager._instance.ModifyUserBalance();
    //        ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 2reate success room---");
    //        ConstantsData_M.Log("Bet ID");
    //        rematchButton.SetActive(false);
    //        AndroidUtility._ShowAndroidToastMessage("Rematch Request Send");
    //    },
    //    failureMessage =>
    //    {

    //        ApiAndRoomManager._instance.DisplayError("failureMessage");

    //        //Debug.Log("Gold Bet Failed to Create: " + failureMessage);

    //    });
    //    SoundManagerMain.instance.ClickSoundPlay();
    //}

    //    void HandleOpenChallengeCreated(Photon.Realtime.Room room)
    //    {
    //        PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);                                                                                                                //Photon Removal
    //        ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;
    //        ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);

    //        ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    //#if !UNITY_EDITOR
    //        PhotonNetwork.LeaveRoom();
    //#endif
    //    }
    public void RequestFriendAdd()
    {
        friendRequestButton.SetActive(false);
        ApiAndRoomManager._instance.SendRequestToFriends(staticVariables.OpponetProfile.userId, OnSuccess =>
        {
            Single<int> response = JsonUtility.FromJson<Single<int>>(OnSuccess);
            AndroidUtility._ShowAndroidToastMessage(response.message);

        });
    }
}
