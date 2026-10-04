using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;

public class ResultManagerForSnooker : MonoBehaviour
{
    public static ResultManagerForSnooker instance;

    [FormerlySerializedAs("Winner")] public RawImage winnerAvatar;
    [FormerlySerializedAs("Looser")] public RawImage loserAvatar;

    [FormerlySerializedAs("WinnerName")] public TextMeshProUGUI winnerNameText;
    [FormerlySerializedAs("LooserName")] public TextMeshProUGUI loserNameText;
    [FormerlySerializedAs("popupText")] public TextMeshProUGUI resultPopupText;

    [FormerlySerializedAs("FinishPanel")] public GameObject finishPanelUI;



    [FormerlySerializedAs("finish")] public bool isFinishTriggered = false;
    [FormerlySerializedAs("FriendRequest")] public GameObject friendRequestButton;
    [FormerlySerializedAs("Rematch")] public GameObject rematchButton;
    public GameObject chest;
    public GameObject winnerText, loserText, drawText;
    public Image[] coins;
    public Sprite[] gold, silver;
    public static bool isGameFinished = false;
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
    public void Draw()
    {
        winnerText.SetActive(false);
        loserText.SetActive(false);
        drawText.SetActive(true);
        chest.SetActive(false);
        finishPanelUI.SetActive(true);
        resultPopupText.text = "Game ended, No balance deducted";
        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);

        InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
        ApiAndRoomManager._instance.DrawChallenge(ApiAndRoomManager._instance.winLoseChallengeId);
        isGameFinished = true;
    }
    void OnEnable()
    {
        if (SnokerNetwork.IsMultiplayer)
        {

        }
    }
    public void HandleGameResultAlt(bool IsWin, string PlayerId)
    {

        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].sprite = staticVariables.isgoldcoins ? gold[i] : silver[i];

        }
        isGameFinished = true;

        //Photon Removal   PunNetwork.instance.HasWinnerBeenDeclared = true;
        $"{PlayerId} {IsWin}Finish Panel".Show();
        finishPanelUI.SetActive(true);
        if (SnokerNetwork.IsMultiplayer)
        {
            // ApiAndRoomManager._instance.GetValidateApi(NetworkGameManager.Instance.transactionId, (success) =>
            // {
            //     ValidateRematch validate=JsonUtility.FromJson<ValidateRematch>(success);
            //     success.Show("Validation Result");
            //     if (validate.status)
            //     {
            //         if (validate.message.Contains("allowed"))
            //         {
            //             rematchButton.SetActive(true);
            //         }
            //     }
            //     else
            //     {
            //         rematchButton.SetActive(false);
            //     }
            // });
            rematchButton.SetActive(false);

            CheckRematchStatus();
            NetworkGameManager.OnRematchRecived += RematchRequestRecived;
            //Photon Removal     if (PhotonNetwork.InRoom)
            {
                var roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["HasWinnerBeenDeclared"] = true;
                //Photon Removal        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
            $"1".Show();

            //  rematchButton.SetActive(false);
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
                $"2".Show();
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    InitializeWinnerUI(staticVariables.ProfilePicture,
                        staticVariables.UserProfiledata.user.first_name + " " +
                        staticVariables.UserProfiledata.user.last_name);

                    InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"Congratulations! Your account has been credited with {staticVariables.currentPrize} {prizeType} coins";
                    ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                    $"3".Show();
                }
                else
                {
                    InitializeLoserUI(staticVariables.ProfilePicture,
                        staticVariables.UserProfiledata.user.first_name + " " +
                        staticVariables.UserProfiledata.user.last_name);
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"{staticVariables.currentPrize} {prizeType} coins has been deducted from your account. Please try again";
                    InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    $"4".Show();
                }
            }
            else
            {
                $"5".Show();
                if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                {
                    $"6".Show();
                    InitializeLoserUI(staticVariables.ProfilePicture,
                        staticVariables.UserProfiledata.user.first_name + " " +
                        staticVariables.UserProfiledata.user.last_name);
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"{staticVariables.currentPrize} {prizeType} coins has been deducted from your account. Please try again";
                    InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);

                }
                else
                {
                    $"7".Show();
                    InitializeWinnerUI(staticVariables.ProfilePicture,
                        staticVariables.UserProfiledata.user.first_name + " " +
                        staticVariables.UserProfiledata.user.last_name);

                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"Congratulations! Your account has been credited with {staticVariables.currentPrize} {prizeType} coins";
                    InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                }
            }

            //if (PhotonNetwork.InRoom)                                                                                                                             //Photon Removal
            //{
            //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
            //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
            //    PhotonNetwork.LeaveRoom();
            //}
        }
        else
        {
            $"8".Show();
            rematchButton.SetActive(false);
            friendRequestButton.SetActive(false);
            if (IsWin)
            {

                InitializeWinnerUI(staticVariables.ProfilePicture,
                    staticVariables.UserProfiledata.user.first_name);
                ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);

                string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                resultPopupText.text = $"Congratulations! Your account has been credited with {staticVariables.currentPrize} {prizeType} coins";
                InitializeLoserUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                "YOU WIN".Show();
                $"9".Show();

            }
            else
            {

                InitializeLoserUI(staticVariables.ProfilePicture,
                    staticVariables.UserProfiledata.user.first_name + " " +
                    staticVariables.UserProfiledata.user.last_name);
                string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                resultPopupText.text = $"{staticVariables.currentPrize} {prizeType} coins has been deducted from your account. Please try again";
                InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
                $"10".Show();

            }


        }
        if (NetworkServer.active)
        {
            //   MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
        }
        //if (PhotonNetwork.InRoom)                                                                                                          //Photon Removal
        //{
        //    if (PhotonNetwork.CurrentRoom != null)
        //    {
        //        PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //        PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    }

        //    PhotonNetwork.LeaveRoom();
        //}
    }
    public void CheckRematchStatus()
    {
        StickManager.instance.CmdCheckRematchStatus(staticVariables.UserProfiledata.user._id.ToString());
    }
    public void RematchStatus(int playerCount)
    {
        if (playerCount >= 2)
        {
            rematchButton.SetActive(true);
        }
    }
    public void LeaveMultiplayerRoom()
    {
        //if (SnokerNetwork.IsMultiplayer)                                                                                                                                     //Photon Removal
        //{
        //    if (PhotonNetwork.InRoom)
        //    {
        //        PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //        PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    }
        //    PhotonNetwork.LeaveRoom();
        //}
        if (SnokerNetwork.IsMultiplayer)
        {
            GameModeManager.isAI = false;
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            if (NetworkGameManager.Instance)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
            Mirror.NetworkManager.singleton.StopClient();
            SnokerGameManager.bGameOver = false;
            Time.timeScale = 1f;
            //switchScreen("MainMenu");
            if (SnokerNetwork.IsMultiplayer)
            {
                //_snokerNetwork._runner.Shutdown(false);
                //Photon Removal PhotonNetwork.LeaveRoom();
                Debug.LogError("photon.leave room");
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                if (NetworkGameManager.Instance)
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                NetworkGameManager.Instance.CmdReloadServer();//  this.Delay(1, () => NetworkManager.singleton.StopClient());
            }
            else
            {

            }
        }
        SceneManager.LoadScene("Home");
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

    public void OnPlayerLeftRoom()
    {
        //Photon Removal  PunNetwork.HandleRoomJoined = null;
        //Photon Removal  PunNetwork.HandleRoomJoined += HandleOpenChallengeCreated;
        //{
        //    NetworkManager.mainPlayer.prize = NetworkManager.mainPlayer.prize;
        //}
        //staticVariables.screenstatus = "sad";
        //Dictionary<string, string> betRequestDict = new Dictionary<string, string>();
        //betRequestDict["first_player"] = staticVariables.UserProfiledata.user._id.ToString();
        //betRequestDict["second_player"] = staticVariables.lastOpponentId.ToString();
        //betRequestDict["game_id"] = ApiAndRoomManager.currentGameId.ToString();
        //betRequestDict["remark"] = "Multipler with Golden Coins";
        //betRequestDict["screenstatus"] = staticVariables.screenstatus;
        //betRequestDict["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver";

        //if (staticVariables.isgoldcoins)
        //{
        //    betRequestDict["gold"] = NetworkManager.mainPlayer.prize.ToString();
        //    betRequestDict["silver"] = "0";
        //}
        //else
        //{
        //    betRequestDict["silver"] = NetworkManager.mainPlayer.prize.ToString();
        //    betRequestDict["gold"] = "0";
        //}

        //ApiAndRoomManager._instance.create_Challenge_Invite(betRequestDict,
        //successMessage =>
        //{
        //    ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 1reate success room---");
        //    ApiAndRoomManager._instance.ModifyUserBalance();
        //    ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 2reate success room---");
        //    ConstantsData_M.Log("Bet ID");
        //    rematchButton.SetActive(false);
        //    AndroidUtility._ShowAndroidToastMessage("Rematch Request Send");
        //},
        //failureMessage =>
        //{

        //    ApiAndRoomManager._instance.DisplayError("failureMessage");

        //    //Debug.Log("Gold Bet Failed to Create: " + failureMessage);

        //});
        SoundManagerMain.instance.ClickSoundPlay();
    }

    //    void HandleOpenChallengeCreated(Photon.Realtime.Room room)                                                                                             //Photon Removal
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