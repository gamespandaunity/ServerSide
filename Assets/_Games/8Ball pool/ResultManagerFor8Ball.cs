using Mirror;
using NetworkManagement;
 
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ResultManagerFor8Ball : MonoBehaviour
{
    public static ResultManagerFor8Ball instance;
    public RawImage winnerAvatar;
    public RawImage loserAvatar;

    public TextMeshProUGUI winnerNameText;
    public TextMeshProUGUI loserNameText;
    public TextMeshProUGUI resultPopupText;

    public GameObject finishPanelUI;

    public bool isFinishTriggered = false;
    public GameObject friendRequestButton;
    public GameObject rematchButton;
    public GameObject chest;
    public GameObject winnerText, loserText, drawText;
    public Image[] coins;
    public Sprite[] gold, silver;
    void Awake()
    {
        instance= this;
    }
    public void InitializeWinnerUI(Texture2D pp, string name)
    {
        winnerAvatar.texture = pp;
        winnerNameText.text = name;
    }
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
    }

    public void HandleGameResultAlt(bool IsWin, string PlayerId, bool isAi)
    {
        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].sprite = staticVariables.isgoldcoins ? gold[i] : silver[i];

        }
        "Finish Panel".Show();
            finishPanelUI.SetActive(true);
            if (isAi == false)
            {
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
                        ApiAndRoomManager._instance.Show("1");
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        "1".Show();
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);

                        InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"Congratulations! Your account has been credited with {staticVariables.currentPrize} {prizeType} coins";

                }
                    else
                    {
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);
                        InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"{staticVariables.currentPrize} {prizeType} coins has been deducted from your account. Please try again";
                    ApiAndRoomManager._instance.Show("2");
                        staticVariables.OpponetProfile.userId.Show();
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId.ToString());
                        "2".Show();
                    }
                }
                else
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        ApiAndRoomManager._instance.Show("3");
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId);
                        "3".Show();
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name);
                        InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"{staticVariables.currentPrize} {prizeType} coins has been deducted from your account. Please try again";
                }
                    else
                    {
                        ApiAndRoomManager._instance.Show();
                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        "4".Show("4");
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);

                        InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"Congratulations! Your account has been credited with {staticVariables.currentPrize} {prizeType} coins";

                }
                }
            //if (PhotonNetwork.InRoom)
            //{
            //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;                                                                           //Photon Removal
            //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
            //    PhotonNetwork.LeaveRoom();
            //}
        }
        else
            {
                friendRequestButton.SetActive(false);
                rematchButton.SetActive(false);
                if (IsWin)
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name);
                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());

                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"Congratulations! Your account has been credited with {staticVariables.currentPrize} {prizeType} coins";
                    InitializeLoserUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        "YOU WIN".Show();

                    }
                    else
                    {
                        InitializeLoserUI(staticVariables.ProfilePicture,
                             staticVariables.UserProfiledata.user.first_name);
                        InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"{staticVariables.currentPrize} {prizeType} coins has been deducted from your account. Please try again";

                }
                }
                else
                {
                    if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name);
                        InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"Congratulations! Your account has been credited with {staticVariables.currentPrize} {prizeType} coins";

                }
                    else
                    {
                        InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");

                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name);
                        "AI WIN".Show();
                    string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                    resultPopupText.text = $"{staticVariables.currentPrize} {prizeType} coins has been deducted from your account. Please try again";
                }
                }
            }
        //if (PhotonNetwork.InRoom)
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;                                                                                                          //Photon Removal
        //    PhotonNetwork.LeaveRoom();
        //}
    }

    public void LeaveMultiplayerRoom()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            Debug.LogError("photon.leave room");
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            Debug.Log("currentDisconnectReason" + MirrorNetwork.Instance.currentDisconnectReason);
            // if (NetworkGameManager.Instance)
            NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
            Debug.Log("USER" + staticVariables.UserProfiledata.user._id.ToString());
            NetworkGameManager.Instance.CmdReloadServer();
            //  this.Delay(1, () => NetworkManager.singleton.StopClient());
            SceneManager.LoadScene("Home");
        }
        else
        {
            Debug.Log("AI GameLeave");
            SceneManager.LoadScene("Home");
        }
        //SceneManager.LoadScene("Home");
        //Photon Removal   PhotonNetwork.LeaveRoom();
    }
    public void RequestRematch()
    {
    //    if (PhotonNetwork.InRoom)
    //    {
    //        PhotonNetwork.CurrentRoom.PlayerTtl = 0;
    //        PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;                                                                                          //Photon Removal
    //        PhotonNetwork.LeaveRoom();
    //    }
        OnPlayerLeftRoom();
    }


    public void OnPlayerLeftRoom()
    {
        //Photon Removal  PunNetwork.HandleRoomJoined = null;
        //Photon Removal  PunNetwork.HandleRoomJoined += HandleOpenChallengeCreated;
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
            rematchButton.SetActive(false);
            AndroidUtility._ShowAndroidToastMessage("Rematch Request Send");
            PopupMessageManager.instance.ShowPopUp("Rematch Request Send");
            //SceneManager.LoadScene("Home");
        },
        failureMessage =>
        {

            ApiAndRoomManager._instance.DisplayError("failureMessage");

            //Debug.Log("Gold Bet Failed to Create: " + failureMessage);

        });
        SoundManagerMain.instance.ClickSoundPlay();
    }

    //    void HandleOpenChallengeCreated(Photon.Realtime.Room room)
    //    {
    //        PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);
    //        ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;
    //        ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);                                                                       //Photon Removal
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
