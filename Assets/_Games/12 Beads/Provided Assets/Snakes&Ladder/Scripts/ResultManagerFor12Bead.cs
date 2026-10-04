
using Mirror;
using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Twelve;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;

public class ResultManagerFor12Bead : NetworkBehaviour//: MonoBehaviourPun
{
    public RawImage winnerImage;
    public RawImage loserImage;
    public TextMeshProUGUI winnerNameText;
    public TextMeshProUGUI loserNameText;
    public TextMeshProUGUI popupMessageText;
    public GameObject gameFinishPanel;
    public GamePlayControllerTwelve gameplayController;
    public static bool isGameFinished = false;
    public GameObject friendRequestBtn;
    public GameObject rematchBtn;
    public bool isAIGameFinished = false;

    public static ResultManagerFor12Bead instance;


    void Awake()
    {
        instance = this;
    }
    //public void WinnerInitialize(Texture2D pp, string name)
    public void InitializeWinnerProfile(Texture2D pp, string name)
    {
        pp.Show();
        winnerImage.texture = pp;
        winnerNameText.text = name;
    }
    // public void LooserInitialize(Texture2D pp, string name)
    public void InitializeLoserProfile(Texture2D pp, string name)
    {

        pp.Show();
        loserImage.texture = pp;
        loserNameText.text = name;
    }

    // public void WinPlayer(bool IsWin, int PlayerId)
    public void RpcHandlePlayerWinState(bool IsWin, int PlayerId)
    {
        Debug.Log("isGameFinish" + isGameFinished);

        if (isGameFinished == false)
        {
            gameFinishPanel.SetActive(true);
            isGameFinished = true;
            Debug.Log("RpcPlayerId" + PlayerId);
            // FinishPanel.SetActive(true);
            if (GamePlayControllerTwelve.isOnlineMultiplayer)
            {
                NetworkGameManager.OnRematchRecived += RematchRequestRecived;
                if (IsWin)
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id)
                    {
                        Debug.LogError("Win PlayerId: " + PlayerId);
                        InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        popupMessageText.text = "Congratulations! Your account has been credited with coins";
                        InitializeLoserProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        Debug.LogError("Winner PlayerId: " + PlayerId);

                    }
                    else
                    {
                        Debug.LogError("Losse PlayerId: " + PlayerId);
                        //LooserInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                        Debug.LogError("Looser PlayerId: " + PlayerId);
                    }
                    //if (PlayerId == GamePlayControllerTwelve.myId)
                    //{
                    //    Debug.Log("RpcWinPlayer1");
                    //    InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                    //    InitializeLoserProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    //      ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                    //  //  ConfettiEffect1.SetActive(true);
                    // //   ConfettiEffect2.SetActive(true);
                    //    popupMessageText.text = "Congratulations! Your account has been credited with coins";

                    //}
                    //else
                    //{
                    //    Debug.Log("RpcWinPlayer11");
                    //    InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    //    InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                    //      popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                    //}
                }
                else
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id)
                    {
                        Debug.LogError("Losse PlayerId: " + PlayerId);
                        //LooserInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                        popupMessageText.text = $"{NetworkGameManager.Instance.Prize} {prizeType} coins has been deducted from your account. Please try again";
                        Debug.LogError("Looser PlayerId: " + PlayerId);
                    }
                    else
                    {
                        Debug.LogError("Win PlayerId: " + PlayerId);
                        InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        popupMessageText.text = "Congratulations! Your account has been credited with coins";
                        InitializeLoserProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        Debug.LogError("Winner PlayerId: " + PlayerId);
                    }
                }
            }
            else
            {
                if (IsWin)
                {
                    if (PlayerId == GamePlayControllerTwelve.myId)
                    {
                        Debug.Log("RpcWinPlayer3");
                        InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        // ConfettiEffect1.SetActive(true);
                        // ConfettiEffect2.SetActive(true);
                        popupMessageText.text = "Congratulations! Your account has been credited with coins";
                        //  GuestGrandScripts.instance.ChangeGuestCoins(staticVariables.currentPrize);

                    }
                    else
                    {
                        Debug.Log("RpcWinPlayer33");
                        InitializeLoserProfile(staticVariables.OpponetProfile.image, staticVariables.OpponetProfile.userName);
                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                    }
                }
                else
                {
                    if (PlayerId == Twelve.PLAYERS.PLAYER1.GetHashCode())
                    {
                        Debug.Log("RpcWinPlayer4");
                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                    }
                    else
                    {
                        Debug.Log("RpcWinPlayer44");
                        InitializeWinnerProfile(staticVariables.OpponetProfile.image, staticVariables.OpponetProfile.userName);
                        //GuestGrandScripts.instance.ChangeGuestCoins(-staticVariables.currentPrize);
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.OpponetProfile.id);
                        // ConfettiEffect1.SetActive(false);
                        // ConfettiEffect2.SetActive(false);
                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                    }
                }
            }
        }
        // gameFinishPanel.SetActive(true);
        // if (isGameFinished == false)
        // {
        //     isGameFinished = true;
        //     if (gameplayController.multiPlayerGame)
        //     {
        //         $"Is Win {IsWin} Winning Player ID {PlayerId} My Player Id{Twelve.PLAYERS.PLAYER1.GetHashCode()}".Show();
        //         rematchBtn.SetActive(true);
        //         //if (PhotonNetwork.InRoom)
        //         //{
        //         //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //         //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //         //    PhotonNetwork.LeaveRoom();
        //         //}
        //         ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, OnSuccess =>
        //         {
        //             Single<int> player = JsonUtility.FromJson<Single<int>>(OnSuccess);

        //             if (player.message.Contains("not"))
        //             {
        //                 friendRequestBtn.SetActive(true);
        //             }
        //             else
        //             {
        //                 friendRequestBtn.SetActive(false);
        //             }
        //         });
        //         if (IsWin)
        //         {
        //             if (PlayerId == GamePlayControllerTwelve.myId)
        //             {
        //                 Debug.Log("RpcWinPlayer1"+PlayerId);
        //                 InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
        //                 InitializeLoserProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
        //                 ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
        //                 popupMessageText.text = "Congratulations! Your account has been credited with coins";

        //             }
        //             else
        //             {
        //                 Debug.Log("RpcWinPlayer11"+PlayerId);
        //                 InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
        //                 InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
        //                 popupMessageText.text = "Your balance has been deducted from your account. Please try again";
        //             }
        //         }
        //         else
        //         {
        //             if (PlayerId == Twelve.PLAYERS.PLAYER1.GetHashCode())
        //             {
        //                 Debug.Log("PlayerId" + PlayerId);
        //                 Debug.Log("Win6");

        //                 InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
        //                 InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
        //             }
        //             else
        //             {
        //                 Debug.Log("PlayerId" + PlayerId);
        //                 Debug.Log("Win66");

        //                 InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
        //                 InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
        //                 ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId);

        //                 popupMessageText.text = "Your balance has been deducted from your account. Please try again";


        //             }
        //         }

        //         //if (PhotonNetwork.InRoom)
        //         //{                                                                             //Photon Removal
        //         //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //         //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //         //    PhotonNetwork.LeaveRoom();
        //         //}
        //     }
        //     else
        //     {
        //         if (IsWin)
        //         {
        //             if (PlayerId == Twelve.PLAYERS.PLAYER1.GetHashCode())
        //             {

        //                 Debug.Log("PlayerId" + PlayerId);
        //                 Debug.Log("Win3");



        //                 InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
        //                 InitializeLoserProfile(staticVariables.OpponetProfile.image, staticVariables.OpponetProfile.userName);
        //                // ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
        //                 ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
        //                 popupMessageText.text = "Congratulations! Your account has been credited with coins";
        //                 GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);

        //             }
        //             else
        //             {
        //                 Debug.Log("PlayerId" + PlayerId);
        //                 Debug.Log("Win33");

        //                 InitializeLoserProfile(staticVariables.OpponetProfile.image, staticVariables.OpponetProfile.userName);
        //                 InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
        //                 //ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
        //                 ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
        //                  popupMessageText.text = "Your balance has been deducted from your account. Please try again";
        //             }
        //         }
        //         else
        //         {
        //             if (PlayerId == Twelve.PLAYERS.PLAYER1.GetHashCode())
        //             {
        //                 Debug.Log("PlayerId" + PlayerId);
        //                 Debug.Log("Win4");
        //                 InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
        //                 InitializeWinnerProfile(staticVariables.OpponetProfile.image, staticVariables.OpponetProfile.userName);
        //                 ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.OpponetProfile.userId);
        //             }
        //             else
        //             {
        //                 Debug.Log("PlayerId" + PlayerId);
        //                 Debug.Log("Win44");
        //                 InitializeWinnerProfile(staticVariables.OpponetProfile.image, staticVariables.OpponetProfile.userName);
        //                 InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
        //                 //GuestGrandScripts.instance.ChangeGuestCoins(-staticVariables.currentPrize);
        //                 ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.OpponetProfile.userId);

        //                 popupMessageText.text = "Your balance has been deducted from your account. Please try again";
        //             }
        //         }
        //     }
        // }

    }
    public void HandlePlayerWinState(bool IsWin, int PlayerId)
    {
        gameFinishPanel.SetActive(true);
        //isGameFinished.Show("Is Finished");
        //gameplayController.multiPlayerGame.Show("Is Multiplayer Game");
        IsWin.Show("Is Win");
        PlayerId.Show("Player Id");
        if (isAIGameFinished == false)
        {
            isAIGameFinished = true;
            if (gameplayController.multiPlayerGame)
            {
                $"Is Win {IsWin} Winning Player ID {PlayerId} My Player Id{Twelve.PLAYERS.PLAYER1.GetHashCode()}".Show();
                rematchBtn.SetActive(true);

                ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, OnSuccess =>
                {
                    Single<int> player = JsonUtility.FromJson<Single<int>>(OnSuccess);

                    if (player.message.Contains("not"))
                    {
                        friendRequestBtn.SetActive(true);
                    }
                    else
                    {
                        friendRequestBtn.SetActive(false);
                    }
                });
                if (IsWin)
                {
                    if (PlayerId == Twelve.PLAYERS.PLAYER1.GetHashCode())
                    {
                        Debug.Log("PlayerId" + PlayerId);
                        Debug.Log("Win3");
                        InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        InitializeLoserProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());

                        popupMessageText.text = "Congratulations! Your account has been credited with coins";

                    }
                    else
                    {
                        Debug.Log("PlayerId" + PlayerId);
                        Debug.Log("Win33");
                        //InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        // InitializeLoserProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);

                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId);
                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                    }
                }
                else
                {
                    if (PlayerId == Twelve.PLAYERS.PLAYER1.GetHashCode())
                    {
                        Debug.Log("PlayerId" + PlayerId);
                        Debug.Log("Win4");
                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    }
                    else
                    {
                        Debug.Log("PlayerId" + PlayerId);
                        Debug.Log("Win44");
                        InitializeWinnerProfile(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId);

                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";


                    }
                }

            }
            else
            {
                if (IsWin)
                {
                    if (PlayerId == Twelve.PLAYERS.PLAYER1.GetHashCode())
                    {
                        Debug.Log("PlayerId" + PlayerId);
                        Debug.Log("Win1");
                        InitializeWinnerProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                        InitializeLoserProfile(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        popupMessageText.text = "Congratulations! Your account has been credited with coins";
                        GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);

                    }
                    else
                    {
                        Debug.Log("PlayerId" + PlayerId);
                        Debug.Log("Win11");
                        InitializeWinnerProfile(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.OpponetProfile.userId);
                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                    }
                }
                else
                {
                    Twelve.PLAYERS.PLAYER1.GetHashCode().Show("Player Id Check");
                    if (PlayerId == Twelve.PLAYERS.PLAYER1.GetHashCode())
                    {
                        Debug.Log("PlayerId" + PlayerId);
                        Debug.Log("Win2");
                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                        InitializeWinnerProfile(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.OpponetProfile.userId);
                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                    }
                    else
                    {
                        Debug.Log("PlayerId" + PlayerId);
                        Debug.Log("Win22");
                        InitializeWinnerProfile(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        InitializeLoserProfile(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                        //GuestGrandScripts.instance.ChangeGuestCoins(-staticVariables.currentPrize);
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.OpponetProfile.userId);

                        popupMessageText.text = "Your balance has been deducted from your account. Please try again";
                    }
                }
            }
        }

    }
    // public void SendRematch()
    bool sender;
    public void RequestRematch()
    {
        sender = true;
        MultiPlayerGameManagerTwelve.instance.CmdOnServerResetValues();
        NetworkGameManager.Instance.CmdRequestRematch();
        //if (PhotonNetwork.InRoom)
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;                           //Photon Removal
        //    PhotonNetwork.LeaveRoom();
        //}
        // HandlePlayerLeftRoom();
        rematchBtn.SetActive(false);
        TwelveBeadSoundManager.instance.PlayAnySound(1);

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
    // public void Onleft()
    public void HandlePlayerLeftRoom()
    {
        //PunNetwork.HandleRoomJoined = null;
        //PunNetwork.HandleRoomJoined += HandleOpenChallengeCreated;                             //Photon Removal
        {
            //Wasi  NetworkManager.mainPlayer.prize = NetworkManager.mainPlayer.prize;
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
            //Wasi  betRequestDict["gold"] = NetworkManager.mainPlayer.prize.ToString();
            betRequestDict["silver"] = "0";
        }
        else
        {
            //Wasi betRequestDict["silver"] = NetworkManager.mainPlayer.prize.ToString();
            betRequestDict["gold"] = "0";
        }

        ApiAndRoomManager._instance.create_Challenge_Invite(betRequestDict,
        successMessage =>
        {
            ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 1reate success room---");
            ApiAndRoomManager._instance.ModifyUserBalance();
            ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 2reate success room---");
            ConstantsData_M.Log("Bet ID");
            rematchBtn.SetActive(false);
            AndroidUtility._ShowAndroidToastMessage("Rematch Request Send");
        },
        failureMessage =>
        {

            ApiAndRoomManager._instance.DisplayError("failureMessage");

            //Debug.Log("Gold Bet Failed to Create: " + failureMessage);

        });
        SoundManagerMain.instance.ClickSoundPlay();
    }
    // void OnOpenChallengeCreated(Photon.Realtime.Room room)
    //    void HandleOpenChallengeCreated(Photon.Realtime.Room room)
    //    {
    //        PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);
    //        ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;
    //        ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);
    //        // HomeMenuManager.instance.yourChallengeLoader.SetActive(false);
    //        ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    //#if !UNITY_EDITOR
    //        PhotonNetwork.LeaveRoom();
    //#endif
    //    }
    //public void SendFriendRequest()
    public void SendOpponentFriendRequest()
    {
        friendRequestBtn.SetActive(false);
        ApiAndRoomManager._instance.SendRequestToFriends(staticVariables.OpponetProfile.userId, OnSuccess =>
        {
            Single<int> response = JsonUtility.FromJson<Single<int>>(OnSuccess);
            AndroidUtility._ShowAndroidToastMessage(response.message);

        });
        TwelveBeadSoundManager.instance.PlayAnySound(1);

    }
}
