using UnityEngine;
using System.Collections;
using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine.SceneManagement;
//using Facebook.Unity;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using AssemblyCSharp;
using System.Globalization;
using UnityEngine.Networking;
using Mirror;
using LudoGame;
//using Photon.Pun;
//using Photon.Realtime;
using UnityEngine.SocialPlatforms.Impl;
[System.Serializable]
public class ReferralDetails
{
    public string referral_code;
    public string referral_earnings;
    public string otp_code;
    public string status;
    // public string date_created;
}

[System.Serializable]
public class ReferralDetail
{
    // public List<ReferralDetails> value;
    public List<ReferralDetails> data;
    // public List<ReferralDetails> message;
}

[System.Serializable]
public class ServerDetails
{
    public string status;
    public string api_key;
    public string app_version;
    // public string date_created;
}

[System.Serializable]
public class ServerDetail
{
    // public List<ReferralDetails> value;
    public List<ServerDetails> data;
    // public List<ReferralDetails> message;
}

public class PlayFabManager : NetworkBehaviour//, IChatClientListener
{


    public Sprite[] avatarSprites;

    public string PlayFabId;
    public string authToken;
    public string POSTAddUserURL = "#";
    public bool multiGame = true;
    public bool roomOwner = false;
    public GameObject fbButton;
    //private FacebookFriendsMenu facebookFriendsMenu;
    //public ChatClient chatClient;
    private bool alreadyGotFriends = false;
    public GameObject menuCanvas;
    public GameObject MatchPlayersCanvas;
    public GameObject splashCanvas;
    public bool opponentReady = false;
    public bool imReady = false;
    public GameObject playerAvatar;
    public GameObject playerName;
    public InputField InputField;
    public InputField InputField2;
    public GameObject backButtonMatchPlayers;

    public static PlayFabManager Instance;
    public StaticGameVariablesController staticvars;

    public bool isInLobby = false;
    public bool isInMaster = false;

    void Awake()
    {
        if (Instance != null)
        {

            Destroy(gameObject);
            return;
        }
        ;
        Instance = this;
        PlayFabSettings.TitleId = StaticStrings.PlayFabTitleID;

        // PhotonNetwork.NetworkingClient.EventReceived += this.OnEvent;
        Login();
        DontDestroyOnLoad(transform.gameObject);
    }

    void OnDestroy()
    {
        //PhotonNetwork.NetworkingClient.EventReceived -= this.OnEvent;
    }

    public void destroy()
    {
        if (this.gameObject != null)
            DestroyImmediate(this.gameObject);
    }

    // Use this for initialization
    void Start()
    {


        //PlayerPrefs.DeleteAll();
        //PhotonNetwork.KeepAliveInBackground = StaticStrings.photonDisconnectTimeoutLong;
        LudoGame.GameManager.Instance.playfabManager = this;
        //facebookFriendsMenu = LudoGame.GameManager.Instance.facebookFriendsMenu;
        // PlayerPrefs.SetInt(StaticStrings.VibrationsKey, 0);
        avatarSprites = staticvars.avatars;

        // showSelectTableScene(false);
        if (SceneManager.GetActiveScene().name.Contains("LudoGameAI"))
            LudoGame.GameManager.Instance.playfabManager.PlayofflineMode();
    }




    void Update()
    {
        // Debug.Log("Total count of active player : " +PhotonNetwork.countOfPlayers);
        // if (chatClient != null) { chatClient.Service(); }

    }



    // handle events:
    private void OnEvent(EventData photonEvent)
    {

        byte eventCode = photonEvent.Code;
        int senderId = photonEvent.Sender;
        switch (eventCode)
        {
            case (byte)EnumPhoton.BeginPrivateGame:
                LudoGame.GameManager.Instance.firstPlayerInGame = (int)photonEvent.CustomData;
                LoadGameScene();
                break;

            case (byte)EnumPhoton.StartWithBots:
                // if (senderId != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    LoadBots();
                }
                break;

            case (byte)EnumPhoton.StartGame:
                LoadGameScene();
                break;

            case (byte)EnumPhoton.ReadyToPlay:
                LudoGame.GameManager.Instance.readyPlayersCount++;
                break;

            default:
                // Debug.LogWarning($"Unhandled event code: {eventCode}");
                break;
        }

    }

    public void LoadGameWithDelay()
    {
        LoadGameScene();
    }

    //public override void OnMasterClientSwitched(Player newMasterClient)
    //{
    //    //if (LudoGame.GameManager.Instance.controlAvatars != null && LudoGame.GameManager.Instance.type == MyGameType.Private)
    //    //{
    //    //    PhotonNetwork.LeaveRoom();
    //    //    LudoGame.GameManager.Instance.controlAvatars.ShowJoinFailed("Room closed");
    //    //}
    //    //else
    //    //{
    //    //    if (newMasterClient.NickName == PhotonNetwork.LocalPlayer.NickName)
    //    //    {
    //    //        Debug.Log("Im new master client");
    //    //        WaitForNewPlayer();
    //    //    }
    //    //}

    //}



    public void StartGame()
    {
        //CancelInvoke("StartGameWithBots");
        Invoke("startGameScene", 3.0f);

    }

    private IEnumerator waitAndStartGame()
    {
        // while (!opponentReady || !imReady /*|| (!LudoGame.GameManager.Instance.roomOwner && !LudoGame.GameManager.Instance.receivedInitPositions)*/)
        // {
        //     yield return 0;
        // }
        while (LudoGame.GameManager.Instance.readyPlayers < LudoGame.GameManager.Instance.requiredPlayers - 1 || !imReady /*|| (!LudoGame.GameManager.Instance.roomOwner && !LudoGame.GameManager.Instance.receivedInitPositions)*/)
        {
            yield return 0;
        }
        startGameScene();
        LudoGame.GameManager.Instance.readyPlayers = 0;
        opponentReady = false;
        imReady = false;
    }

    public void startGameScene()
    {

        LoadGameScene();
        byte eventCode;
        if (LudoGame.GameManager.Instance.type == MyGameType.Private)
        {
            eventCode = (byte)EnumPhoton.BeginPrivateGame;
        }
        else
        {
            eventCode = (byte)EnumPhoton.StartGame;
        }

        //RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        //{
        //    Receivers = ReceiverGroup.Others
        //};
        SendOptions sendOptions = new SendOptions
        {
            Reliability = true
        };
        LudoGame.GameManager.Instance.firstPlayerInGame = UnityEngine.Random.Range(0, LudoGame.GameManager.Instance.requiredPlayers);
        // PhotonNetwork.RaiseEvent(eventCode, LudoGame.GameManager.Instance.firstPlayerInGame, raiseEventOptions, sendOptions);

    }


    public void LoadGameScene()
    {
        LudoGame.GameManager.Instance.GameScene = "GameScene";
        //APIManager.instance.StartBet();
        //if(!LudoGame.GameManager.Instance.isLocalMultiplayer)
        //PhotonNetwork.CurrentRoom.PlayerTtl = 60000;

        LudoGame.GameManager.Instance.opponentsAvatars[0] = avatarSprites[UnityEngine.Random.Range(0, avatarSprites.Length - 1)];
        if (!LudoGame.GameManager.Instance.gameSceneStarted)
        {
            //SceneManager.LoadScene(LudoGame.GameManager.Instance.GameScene);
            // AddressablesBundleSpawning.instance.LoadAddressableScene(AddressablesBundleSpawning.instance.ludoAddressableSceneName);

            LudoGame.GameManager.Instance.gameSceneStarted = true;
        }

    }
    public void WaitForNewPlayer()
    {
        // if (PhotonNetwork.IsMasterClient && LudoGame.GameManager.Instance.type != MyGameType.Private)
        {
            CancelInvoke("StartGameWithBots");

            Invoke("StartGameWithBots", StaticStrings.WaitTimeUntilStartWithBots);
            // Invoke("FailedToFindPlayers", StaticStrings.WaitTimeUntilStartWithBots);


            //Invoke("FailedToFindPlayers", StaticStrings.WaitTimeUntilStartWithBots);
        }
    }

    public void FailedToFindPlayers()
    {
        // if(!(LudoGame.GameManager.Instance.currentPlayersCount > 1)){
        //     LudoGame.GameManager.Instance.dialogFails.SetActive(true);
        // }
        LudoGame.GameManager.Instance.dialogFails.SetActive(true);

        // LudoGame.GameManager.Instance.roomOwner = false;
        // LudoGame.GameManager.Instance.resetAllData();
        // PhotonNetwork.room.open = false;
        // showMenu();
        // LudoGame.GameManager.Instance.controlAvatars.ShowJoinFailed("Players Not Available");
    }

    public void CloseGame()
    {
        PlayerPrefs.SetInt("GamesPlayed", PlayerPrefs.GetInt("GamesPlayed", 1) + 1);

        //PhotonNetwork.KeepAliveInBackground = StaticStrings.photonDisconnectTimeoutLong;

        //LudoGame.GameManager.Instance.cueController.removeOnEventCall();
        //PhotonNetwork.LeaveRoom();

        LudoGame.GameManager.Instance.playfabManager.roomOwner = false;
        LudoGame.GameManager.Instance.roomOwner = false;
        LudoGame.GameManager.Instance.resetAllData();
        SceneManager.LoadScene("MenuScene");
    }
    public void StartGameWithBots()
    {

        //if (PhotonNetwork.IsMasterClient)
        //{
        //    if (PhotonNetwork.CurrentRoom.PlayerCount < LudoGame.GameManager.Instance.requiredPlayers)
        //    {
        //        // PhotonNetwork.RaiseEvent((int)EnumPhoton.StartWithBots, null, true, null);
        //        LoadBots();
        //    }
        //}
        //else
        //{
        //    Debug.Log("Not Master client");
        //}
    }

    public void LoadBots()
    {
        //PhotonNetwork.CurrentRoom.IsOpen = false;
        //PhotonNetwork.CurrentRoom.IsVisible = false;

        //if (PhotonNetwork.IsMasterClient)
        //{
        //    Invoke("AddBots", 3.0f);
        //}
        //else
        //{
        //    AddBots();
        //}

    }

    public void PlayofflineMode()
    {
        LudoGame.GameManager.Instance.isLocalMultiplayer = true;
        LudoGame.GameManager.Instance.offlineMode = true;
        LudoGame.GameManager.Instance.roomOwner = true;
        // LudoGame.GameManager.Instance.isPlayingWithComputer = true;
        string BotMoves = generateBotMoves();
        extractBotMoves(BotMoves);
        //if (LudoGame.GameManager.Instance.type == MyGameType.TwoPlayer) {
        LudoGame.GameManager.Instance.requiredPlayers = 2;
        //} else
        //    LudoGame.GameManager.Instance.requiredPlayers = 4;

        for (int i = 0; i < LudoGame.GameManager.Instance.requiredPlayers - 1; i++)
        {
            if (LudoGame.GameManager.Instance.opponentsIDs[i] == null)
            {
                // StartCoroutine(AddBot(i));
                LudoGame.GameManager.Instance.opponentsAvatars[i] = avatarSprites[UnityEngine.Random.Range(0, avatarSprites.Length - 1)];
                LudoGame.GameManager.Instance.opponentsIDs[i] = "_BOT" + i;
                LudoGame.GameManager.Instance.opponentsNames[i] = "AI " /*+ (i + 1)*/;
                Debug.Log("adding bot on    " + LudoGame.GameManager.Instance.opponentsIDs[i]);
            }
        }
        LoadGameScene();
    }

    public void AddBots()
    {
        // Add Bots here


        //if (PhotonNetwork.CurrentRoom.PlayerCount < LudoGame.GameManager.Instance.requiredPlayers)
        //{

        //    if (PhotonNetwork.IsMasterClient)
        //    {
        //        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        //        {
        //            Receivers = ReceiverGroup.All
        //        };
        //        SendOptions sendOptions = new SendOptions
        //        {
        //            Reliability = true
        //        };

        //        PhotonNetwork.RaiseEvent((byte)EnumPhoton.StartWithBots, null, raiseEventOptions, sendOptions);
        //    }

        //    for (int i = 0; i < LudoGame.GameManager.Instance.requiredPlayers - 1; i++)
        //    {
        //        if (LudoGame.GameManager.Instance.opponentsIDs[i] == null)
        //        {
        //            StartCoroutine(AddBot(i));
        //        }
        //    }
        //}
    }


    public IEnumerator AddBot(int i)
    {
        yield return new WaitForSeconds(i + UnityEngine.Random.Range(0.0f, 0.9f));

        LudoGame.GameManager.Instance.opponentsAvatars[i] = avatarSprites[UnityEngine.Random.Range(0, avatarSprites.Length - 1)];
        LudoGame.GameManager.Instance.opponentsIDs[i] = "_BOT" + i;
        var MyIndex = UnityEngine.Random.Range(0, LudoGame.GameManager.Instance.botsName.Length);
        // LudoGame.GameManager.Instance.opponentsNames[i] = "Guest" + UnityEngine.Random.Range(100000, 999999);
        LudoGame.GameManager.Instance.opponentsNames[i] = LudoGame.GameManager.Instance.botsName[MyIndex];
        LudoGame.GameManager.Instance.controlAvatars.PlayerJoined(i, "_BOT" + i);
    }



    public void setInitNewAccountData(bool fb)
    {
        Dictionary<string, string> data = MyPlayerData.InitialUserData(fb);
        LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);
    }


    public void updateBoughtChats(int index)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add(MyPlayerData.ChatsKey, LudoGame.GameManager.Instance.myPlayerData.GetChats() + ";'" + index + "'");


        LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);


    }

    public void UpdateBoughtEmojis(int index)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add(MyPlayerData.EmojiKey, LudoGame.GameManager.Instance.myPlayerData.GetEmoji() + ";'" + index + "'");


        LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);
    }

    public void addCoinsRequest(int count)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add(MyPlayerData.CoinsKey, "" + (LudoGame.GameManager.Instance.myPlayerData.GetCoins() + count));

        LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);

        //Call Api to update user coins
        string url = StaticStrings.baseURL + "update-user-coins.php?playfab_id=" + LudoGame.GameManager.Instance.playfabManager.PlayFabId + "&avl_coins=" + data[MyPlayerData.CoinsKey];
        WWW www = new WWW(url);
        StartCoroutine(updateUserCoins(www));
    }

    public void minusCoinsRequest(int count)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add(MyPlayerData.CoinsKey, "" + (LudoGame.GameManager.Instance.myPlayerData.GetCoins() - count));

        LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);

        //Call Api to update user coins
        string url = StaticStrings.baseURL + "update-user-coins.php?playfab_id=" + LudoGame.GameManager.Instance.playfabManager.PlayFabId + "&avl_coins=" + data[MyPlayerData.CoinsKey];
        WWW www = new WWW(url);
        StartCoroutine(updateUserCoins(www));
    }

    public void getPlayerDataRequest()
    {
        //Debug.Log("Get player data request!!");
        GetUserDataRequest getdatarequest = new GetUserDataRequest()
        {
            PlayFabId = LudoGame.GameManager.Instance.playfabManager.PlayFabId,
        };

        PlayFabClientAPI.GetUserData(getdatarequest, (result) =>
        {

            Dictionary<string, UserDataRecord> data = result.Data;

            LudoGame.GameManager.Instance.myPlayerData = new MyPlayerData(data, true);
            if (LudoGame.GameManager.Instance.myPlayerData.GetCoins() <= 0)
            {
                addCoinsRequest(5000);
            }

            string url = StaticStrings.baseURL + "get-user-verification-status-new.php?playfab_id=" + LudoGame.GameManager.Instance.playfabManager.PlayFabId;
            WWW www = new WWW(url);
            // StartCoroutine(getVerificationStatus(www));
            //StartCoroutine(loadSceneMenu());
        }, (error) =>
        {
        }, null);
    }





    [SerializeField]
    private string verificationId;
    private string lastPhoneNumber = "null";


    public String SendMessageButton()
    {
        string phoneNumber = PlayerPrefs.GetString("phone_number");

        if (CheckPhoneNumberText(phoneNumber))
        {

            print(phoneNumber);


        }
        return phoneNumber;
    }


    public void ResendPasswordButton()
    {

    }



    private bool CheckPhoneNumberText(string text)
    {
        bool result = true;

        if (result)
            for (int i = 0; i < text.Length; i++)
                if (!System.Char.IsDigit(text[i]))
                {
                    result = false;
                    break;
                }

        return result;
    }




    private void VerificationBeenFailed(string error)
    {

        Debug.LogError("VerificationBeenFailed : " + error);
    }





    private IEnumerator loadSceneMenu()
    {
        yield return new WaitForSeconds(0.1f);

        if (isInMaster && isInLobby)
        {
            //    Debug.Log("Total count of active player : " + PhotonNetwork.CountOfPlayers);
            //SceneManager.LoadScene("MenuScene");
            SceneManager.LoadScene("MenuScene");
        }
        else
        {
            StartCoroutine(loadSceneMenu());
        }

    }


    IEnumerator createInitUser(WWW www)
    {
        yield return www;

        // check for errors
        if (www.error == null)
        {
            Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
        }
        else
        {
            Debug.Log("WWW Failed!: " + www.text);// contains all the data sent from the server
        }
    }
    IEnumerator updateUser(WWW www)
    {
        yield return www;

        // check for errors
        if (www.error == null)
        {
            Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
        }
        else
        {
            Debug.Log("WWW Failed!: " + www.text);// contains all the data sent from the server
        }
    }
    IEnumerator updateUserCoins(WWW www)
    {
        yield return www;

        // check for errors
        if (www.error == null)
        {
            Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
        }
        else
        {
            Debug.Log("WWW Failed!: " + www.text);// contains all the data sent from the server
        }
    }
    //  }

    // IEnumerator Upload(String PlayfabId) {
    //     Debug.Log("Playfab id in upload : "+PlayfabId);
    //     WWWForm form = new WWWForm();
    //     String emailId = PlayerPrefs.GetString("email_account");
    //     Dictionary<string, string> headers = new Dictionary<string,string>();
    //     headers.Add("Content-Type", "application/json");
    //     // postHeader.Add("api-key", "cda1112Ok5UvAyDRrt934xs7KgVSbY0uiGjecHNfhdqBwPaTmn");
    //     List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
    //     formData.Add(new MultipartFormDataSection("playfab_id=12345678"));
    //     // form.AddField("email", PlayerPrefs.GetString("email_account"));
    //     // form.AddField("password", PlayerPrefs.GetString("password"));
    //     // form.AddField("playfab_id", PlayFabId);
    //     // form.AddField("player_name", LudoGame.GameManager.Instance.nameMy);
    //     // form.AddField("logged_type", PlayerPrefs.GetString("LoggedType"));
    //     // byte[] rawData = form.data;
    //     Debug.Log(PlayerPrefs.GetString("email_account"));
    //     UnityWebRequest www = UnityWebRequest.Post("https://dtsolutions.biz/Ludo-Script/create-users.php", formData);
    //     // www.SetRequestHeader("Content-Type", "application/json");
    //     yield return www.SendWebRequest();

    //     if(www.isNetworkError || www.isHttpError) {
    //         Debug.Log(www.error);
    //     }
    //     else {
    //         Debug.Log("Form upload complete! " + www.downloadHandler.text);
    //     }
    // }

    IEnumerator WaitForRequest(WWW data)
    {
        yield return data; // Wait until the download is done
        Debug.Log(data.error);
        if (data.error != null)
        {
            Debug.Log("Working perfect" + data.text);
            //  MainUI.ShowDebug("There was an error sending request: " + data.error);
        }
        else
        {
            Debug.Log("Not Working" + data.text);
            //  MainUI.ShowDebug("WWW Request: " + data.text);
        }
    }

    public void LinkFacebookAccount()
    {
        LinkFacebookAccountRequest request = new LinkFacebookAccountRequest()
        {
            // AccessToken = Facebook.Unity.AccessToken.CurrentAccessToken.TokenString,
            ForceLink = true
        };

        PlayFabClientAPI.LinkFacebookAccount(request, (result) =>
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            data.Add("LoggedType", "Facebook");
            //     data.Add("FacebookID", Facebook.Unity.AccessToken.CurrentAccessToken.UserId);
            data.Add("PlayerAvatarUrl", LudoGame.GameManager.Instance.avatarMyUrl);
            data.Add(MyPlayerData.PlayerName, LudoGame.GameManager.Instance.nameMy);
            data.Add(MyPlayerData.AvatarIndexKey, "fb");
            data.Add(MyPlayerData.CoinsKey, (LudoGame.GameManager.Instance.myPlayerData.GetCoins() + StaticStrings.CoinsForLinkToFacebook).ToString());
            LudoGame.GameManager.Instance.myAvatarGameObject.GetComponent<Image>().sprite = LudoGame.GameManager.Instance.facebookAvatar;
            LudoGame.GameManager.Instance.myNameGameObject.GetComponent<Text>().text = LudoGame.GameManager.Instance.nameMy;
            LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);

            // LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);
            Debug.Log("FacebookLinkAccount : " + data["FacebookID"]);
            string loggedType = data["LoggedType"];
            string facebookId = data["FacebookID"];

            // Call Api to update user data
            string url = StaticStrings.baseURL + "update-users.php?playfab_id=" + LudoGame.GameManager.Instance.playfabManager.PlayFabId + "&avl_coins=" + data[MyPlayerData.CoinsKey] + "&logged_type=" + loggedType + "&facebook_id=" + facebookId;
            WWW www = new WWW(url);
            StartCoroutine(updateUser(www));

            LudoGame.GameManager.Instance.FacebookLinkButton.SetActive(false);
        },
        (error) =>
        {
            Debug.Log("Error linking facebook account: " + error.ErrorMessage + "\n" + error.ErrorDetails);
            LudoGame.GameManager.Instance.connectionLost.showDialog();
        });



    }

    public void LoginWithFacebook()
    {


        LoginWithFacebookRequest request = new LoginWithFacebookRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CreateAccount = true,
            //    AccessToken = Facebook.Unity.AccessToken.CurrentAccessToken.TokenString
        };

        PlayFabClientAPI.LoginWithFacebook(request, (result) =>
        {
            PlayFabId = result.PlayFabId;
            Debug.Log("Got PlayFabID: " + PlayFabId);



            if (result.NewlyCreated)
            {
                Debug.Log("(new account)");
                setInitNewAccountData(true);
                Dictionary<string, string> data1 = new Dictionary<string, string>();
                data1.Add(MyPlayerData.AvatarIndexKey, "fb");
                LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data1);
            }
            else
            {
                CheckIfFirstTitleLogin(PlayFabId, true);
                Debug.Log("(existing account)");
            }


            UpdateUserTitleDisplayNameRequest displayNameRequest = new UpdateUserTitleDisplayNameRequest()
            {
                DisplayName = LudoGame.GameManager.Instance.playfabManager.PlayFabId
            };

            PlayFabClientAPI.UpdateUserTitleDisplayName(displayNameRequest, (response) =>
            {
                Debug.Log("Title Display name updated successfully");
            }, (error) =>
            {
                Debug.Log("Title Display name updated error: " + error.Error);

            }, null);


            Dictionary<string, string> data = new Dictionary<string, string>();

            data.Add("LoggedType", "Facebook");
            //      data.Add("FacebookID", Facebook.Unity.AccessToken.CurrentAccessToken.UserId);
            if (result.NewlyCreated)
            {
                data.Add("PlayerName", LudoGame.GameManager.Instance.nameMy);
                // Call Backend Api
                string logged_type = data["LoggedType"];
                string facebookId = data["FacebookID"];
                string plr_name = data["PlayerName"];
                string email = "";
                string password = "";
                PlayerPrefs.SetString("referral_code", UnityEngine.Random.Range(100000, 999999).ToString());
                PlayerPrefs.Save();

                string referCode = PlayerPrefs.GetString("referral_code");
                // email="+emailId+"&password="+PlayerPrefs.GetString("password")+"&playfab_id="+PlayFabId+"&player_name="+LudoGame.GameManager.Instance.nameMy+"&logged_type="+PlayerPrefs.GetString("LoggedType")
                string url = StaticStrings.baseURL + "create-users.php?playfab_id=" + PlayFabId + "&email=" + email + "&password=" + password + "&player_name=" + plr_name + "&logged_type=" + logged_type + "&facebook_id=" + facebookId + "&referral_code=" + referCode;
                WWW www = new WWW(url);
                StartCoroutine(createInitUser(www));
            }

            else
            {
                GetUserDataRequest getdatarequest = new GetUserDataRequest()
                {
                    PlayFabId = result.PlayFabId,

                };

                PlayFabClientAPI.GetUserData(getdatarequest, (result2) =>
                {

                    Dictionary<string, UserDataRecord> data2 = result2.Data;

                    if (data2.ContainsKey("PlayerName"))
                    {
                        LudoGame.GameManager.Instance.nameMy = data2["PlayerName"].Value;
                    }
                    else
                    {
                        Dictionary<string, string> data5 = new Dictionary<string, string>();
                        data5.Add("PlayerName", LudoGame.GameManager.Instance.nameMy);
                        data5.Add(MyPlayerData.AvatarIndexKey, "fb");
                        LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data5);
                    }
                }, (error) =>
                {
                    Debug.Log("Data updated error " + error.ErrorMessage);
                }, null);
            }
            data.Add("PlayerAvatarUrl", LudoGame.GameManager.Instance.avatarMyUrl);

            LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);
            GetUserReferCode();
            GetPhotonToken();

        },
            (error) =>
            {
                Debug.Log("Error logging in player with custom ID: " + error.ErrorMessage + "\n" + error.ErrorDetails);
                LudoGame.GameManager.Instance.connectionLost.showDialog();
            });
    }


    public void GetUserReferCode()
    {
        string referralUrl = StaticStrings.baseURL + "get-referral-code.php?playfab_id=" + LudoGame.GameManager.Instance.playfabManager.PlayFabId;
        WWW www = new WWW(referralUrl);
        StartCoroutine(getReferCode(www));
    }
    IEnumerator getReferCode(WWW www)
    {
        yield return www;

        // check for errors
        string data1 = www.text;
        Debug.Log("WWW Result!: " + data1);

        if (www.error == null)
        {
            ReferralDetail C = JsonUtility.FromJson<ReferralDetail>(data1);

            for (int i = 0; i < C.data.Count; i++)
            {
                Debug.Log("WWW Result!: " + C.data[i].referral_code);// contains all the data sent from the server
                PlayerPrefs.SetString("referral_code", C.data[i].referral_code);
                PlayerPrefs.SetString("otp_code", C.data[i].otp_code);
                PlayerPrefs.SetString("user_status", C.data[i].status);
                PlayerPrefs.SetString("referral_earnings", C.data[i].referral_earnings);
                PlayerPrefs.Save();
            }
        }
        else
        {
            PlayerPrefs.SetString("referral_code", "NIL");
            PlayerPrefs.SetString("referral_earnings", "NIL");
            PlayerPrefs.Save();

            Debug.Log("WWW Failed!: " + www.text);// contains all the data sent from the server
        }
    }

    public void CheckIfFirstTitleLogin(string id, bool fb)
    {
        GetUserDataRequest getdatarequest = new GetUserDataRequest()
        {
            PlayFabId = id,

        };

        PlayFabClientAPI.GetUserData(getdatarequest, (result) =>
        {
            Dictionary<string, UserDataRecord> data = result.Data;

            if (!data.ContainsKey(MyPlayerData.TitleFirstLoginKey))
            {
                Debug.Log("First login for this title. Set initial data");
                setInitNewAccountData(fb);
            }

        }, (error) =>
        {
            Debug.Log("Data updated error " + error.ErrorMessage);
        }, null);
    }

    private string androidUnique()
    {
        AndroidJavaClass androidUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityPlayerActivity = androidUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject unityPlayerResolver = unityPlayerActivity.Call<AndroidJavaObject>("getContentResolver");
        AndroidJavaClass androidSettingsSecure = new AndroidJavaClass("android.provider.Settings$Secure");
        return androidSettingsSecure.CallStatic<string>("getString", unityPlayerResolver, "android_id");
    }


    public void Login()
    {

        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CreateAccount = true,
            CustomId = staticVariables.UserProfiledata.user._id.ToString() //SystemInfo.deviceUniqueIdentifier
        };
        PlayFabClientAPI.LoginWithCustomID(request, (result) =>
        {
            PlayFabId = result.PlayFabId;
            //Debug.Log("Got PlayFabID: " + PlayFabId);

            Dictionary<string, string> data = new Dictionary<string, string>();

            if (result.NewlyCreated)
            {
                //Debug.Log("(new account)");
                setInitNewAccountData(false);

                string name = result.PlayFabId;
                name = "Guest";
                for (int i = 0; i < 6; i++)
                {
                    name += UnityEngine.Random.Range(0, 9);
                }

                data.Add("PlayerName", name);
                //addCoinsRequest(StaticStrings.initCoinsCount);
            }
            else
            {
                CheckIfFirstTitleLogin(PlayFabId, false);
                //Debug.Log("(existing account)");
            }
            UpdateUserTitleDisplayNameRequest displayNameRequest = new UpdateUserTitleDisplayNameRequest()
            {
                //DisplayName = name,
                DisplayName = LudoGame.GameManager.Instance.playfabManager.PlayFabId
            };

            PlayFabClientAPI.UpdateUserTitleDisplayName(displayNameRequest, (response) =>
            {
                //Debug.Log("Title Display name updated successfully");
            }, (error) =>
            {
                Debug.Log("Title Display name updated error: " + error.Error);

            }, null);


            LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);

            LudoGame.GameManager.Instance.nameMy = name;

            PlayerPrefs.SetString("LoggedType", "Guest");
            PlayerPrefs.Save();
            GetPhotonToken();

        },
            (error) =>
            {
                Debug.Log("Error logging in player with custom ID:");
                Debug.Log(error.ErrorMessage);
                //LudoGame.GameManager.Instance.connectionLost.showDialog();
            });
    }




    void OnPlayFabError(PlayFabError error)
    {
        Debug.Log("Playfab Error: " + error.ErrorMessage);
    }

    // #######################  PHOTON  ##########################

    void GetPhotonToken()
    {
        string url = StaticStrings.baseURL + "get-server-status-new.php";
        WWW www = new WWW(url);
        // StartCoroutine(getServerStatus(www));

        GetPhotonAuthenticationTokenRequest request = new GetPhotonAuthenticationTokenRequest();
        request.PhotonApplicationId = StaticStrings.PhotonAppID.Trim();

        PlayFabClientAPI.GetPhotonAuthenticationToken(request, OnPhotonAuthenticationSuccess, OnPlayFabError);
    }



    public void QuitApp()
    {
        Application.Quit(); ;
    }

    void OnPhotonAuthenticationSuccess(GetPhotonAuthenticationTokenResult result)
    {
        string photonToken = result.PhotonCustomAuthenticationToken;
        //Debug.Log(string.Format("Yay, logged in session token: {0}", photonToken));
        //PhotonNetwork.AuthValues = new AuthenticationValues();
        //PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.Custom;
        //PhotonNetwork.AuthValues.AddAuthParameter("username", this.PlayFabId);
        //PhotonNetwork.AuthValues.AddAuthParameter("Token", result.PhotonCustomAuthenticationToken);
        //PhotonNetwork.AuthValues.UserId = this.PlayFabId;
        //PhotonNetwork.ConnectUsingSettings();
        authToken = result.PhotonCustomAuthenticationToken;
        getPlayerDataRequest();
        //connectToChat();

    }

    //public void connectToChat()
    //{
    //    chatClient = new ChatClient(this);
    //    LudoGame.GameManager.Instance.chatClient = chatClient;
    //    ExitGames.Client.Photon.Chat.AuthenticationValues authValues = new ExitGames.Client.Photon.Chat.AuthenticationValues();
    //    authValues.UserId = this.PlayFabId;
    //    authValues.AuthType = ExitGames.Client.Photon.Chat.CustomAuthenticationType.Custom;
    //    authValues.AddAuthParameter("username", this.PlayFabId);
    //    authValues.AddAuthParameter("Token", authToken);
    //    chatClient.Connect(StaticStrings.PhotonChatID, "1.4", authValues);
    //}

    //public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    //{
    //    base.OnCustomAuthenticationResponse(data);


    //    Debug.Log("Custom properties changed: " + DateTime.Now.ToString());
    //}


    public void showMenu()
    {
        menuCanvas.gameObject.SetActive(true);

        playerName.GetComponent<Text>().text = LudoGame.GameManager.Instance.nameMy;

        if (LudoGame.GameManager.Instance.avatarMy != null)
            playerAvatar.GetComponent<Image>().sprite = LudoGame.GameManager.Instance.avatarMy;

        splashCanvas.SetActive(false);
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        Debug.Log("Subscribed to CHAT - set online status!");
        // chatClient.SetOnlineStatus(ChatUserStatus.Online);
    }


    public void challengeFriend(string id, string message)
    {
        //if (LudoGame.GameManager.Instance.invitationID.Length == 0 || !LudoGame.GameManager.Instance.invitationID.Equals(id))
        //{
        // chatClient.SendPrivateMessage(id, "INVITE_TO_PLAY_PRIVATE;" + /*id + this.PlayFabId + ";" +*/ LudoGame.GameManager.Instance.nameMy + ";" + message);
        LudoGame.GameManager.Instance.invitationID = id;
        Debug.Log("Send invitation to: " + id);
        // }
    }

    string roomname;

    //public void join()
    //{
    //    PhotonNetwork.JoinRoom(roomname);
    //}

    public void DebugReturn(DebugLevel level, string message)
    {

    }





    //public override void OnJoinedLobby()
    //{
    //    //Debug.Log("Joined lobby");
    //    isInLobby = true;
    //}

    public void JoinRoomAndStartGame()
    {
        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable() {
            {"m", LudoGame.GameManager.Instance.mode.ToString() +  LudoGame.GameManager.Instance.type.ToString() + LudoGame.GameManager.Instance.payoutCoins.ToString()},
            {"gm", LudoGame.GameManager.Instance.mode.ToString()}
         };

        StartCoroutine(TryToJoinRandomRoom(expectedCustomRoomProperties));

        //PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 0);
    }

    public IEnumerator TryToJoinRandomRoom(ExitGames.Client.Photon.Hashtable roomOptions)
    {
        while (true)
        {
            Debug.Log("here: " + LudoGame.GameManager.Instance.requiredPlayers + "players in the romm right now : " + LudoGame.GameManager.Instance.currentPlayersCount);
            if (isInLobby && isInMaster)
            {
                //  PhotonNetwork.JoinRandomRoom(roomOptions, 0);
                break;
            }
            else
            {
                yield return new WaitForSeconds(5.0f);
            }
        }
    }



    public string generateBotMoves()
    {
        LudoGame.GameManager.Instance.isPlayingWithComputer = true;
        if (LudoGame.GameManager.Instance.isLocalMultiplayer)
        {
            LudoGame.GameManager.Instance.payoutCoins = 0;
        }
        // Generate BOT moves
        string BotMoves = "";
        int BotCount = 100;
        int count = 0;
        int step = 0;
        // Generate dice values
        for (int i = 0; i < BotCount; i++)
        {
            if (i == 3 || i == 6 || i == 14 || i == 21 || i == 25 || i == 26 || i == 30 || i == 36 || i == 37 || i == 40 || i == 44 || i == 45 || i == 49 || i == 55 || i == 56)
            {
                step = 6;
            }
            else
            {
                step = UnityEngine.Random.Range(1, 7);
                if (step == 6)
                {
                    if (count == 2)
                    {
                        step = UnityEngine.Random.Range(1, 6);
                        count = 0;
                    }
                    count++;
                }
                else
                {
                    count = 0;
                }
            }
            BotMoves += (step).ToString();
            if (i < BotCount - 1)
            {
                BotMoves += ",";
            }
        }

        BotMoves += ";";

        // Generate delays
        float minValue = LudoGame.GameManager.Instance.playerTime / 10;
        if (minValue < 1.5f) minValue = 1.5f;
        for (int i = 0; i < BotCount; i++)
        {
            BotMoves += (UnityEngine.Random.Range(minValue, LudoGame.GameManager.Instance.playerTime / 8)).ToString();
            if (i < BotCount - 1)
            {
                BotMoves += ",";
            }
        }
        Debug.Log("BotMoves   " + BotMoves);
        return BotMoves;
    }

    public void extractBotMoves(string data)
    {
        LudoGame.GameManager.Instance.botDiceValues = new List<int>();
        LudoGame.GameManager.Instance.botDelays = new List<float>();
        string[] d1 = data.Split(';');


        string[] diceValues = d1[0].Split(',');
        for (int i = 0; i < diceValues.Length; i++)
        {
            LudoGame.GameManager.Instance.botDiceValues.Add(int.Parse(diceValues[i]));
        }

        string[] delays = d1[1].Split(',');
        for (int i = 0; i < delays.Length; i++)
        {
            LudoGame.GameManager.Instance.botDelays.Add(float.Parse(delays[i]));
        }
    }

    public void OnLeftLobby()
    {
        isInLobby = false;
        isInMaster = false;
    }

    public IEnumerator TryToCreateGameAfterFailedToJoinRandom(string roomOptions)
    {
        while (true)
        {
            if (isInLobby && isInMaster)
            {
                //PhotonNetwork.CreateRoom(null, roomOptions, TypedLobby.Default);


                break;
            }
            else
            {
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    public void CreatePrivateRoom()
    {
        LudoGame.GameManager.Instance.JoinedByID = false;
        //RoomOptions roomOptions = new RoomOptions();
        //  roomOptions.MaxPlayers = 4;


        string roomName = "";
        for (int i = 0; i < 8; i++)
        {
            roomName = roomName + UnityEngine.Random.Range(0, 10);
        }

        //roomOptions.CustomRoomPropertiesForLobby = new String[] { "pc" };
        //roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() {
        //    { "pc", LudoGame.GameManager.Instance.payoutCoins},
        //      {"gm", LudoGame.GameManager.Instance.mode.ToString()}
        // };
        //Debug.Log("Private room name: " + roomName);
        //PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public void OnCreatedRoom()
    {
        if (ApiAndRoomManager.currentGameId == 2)
        {
            Debug.Log("OnCreatedRoom");
            roomOwner = true;
            LudoGame.GameManager.Instance.roomOwner = true;
            LudoGame.GameManager.Instance.currentPlayersCount = 1;
            //  LudoGame.GameManager.Instance.controlAvatars.updateRoomID(PhotonNetwork.CurrentRoom.Name);
        }
    }

    public void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom called");
        roomOwner = false;
        LudoGame.GameManager.Instance.roomOwner = false;
        LudoGame.GameManager.Instance.resetAllData();
    }

    public int GetFirstFreeSlot()
    {
        int index = 0;
        for (int i = 0; i < LudoGame.GameManager.Instance.opponentsIDs.Count; i++)
        {
            if (LudoGame.GameManager.Instance.opponentsIDs[i] == null)
            {
                index = i;
                break;
            }
        }
        return index;
    }
    private void getOpponentData(Dictionary<string, UserDataRecord> data, int index, bool fbAvatar, int avatarIndex, string id)
    {
        if (data.ContainsKey("PlayerName"))
        {
            LudoGame.GameManager.Instance.opponentsNames[index] = data["PlayerName"].Value;
        }
        else
        {
            LudoGame.GameManager.Instance.opponentsNames[index] = "Guest857643";
        }

        if (data.ContainsKey("PlayerAvatarUrl") && fbAvatar)
        {
            StartCoroutine(loadImageOpponent(data["PlayerAvatarUrl"].Value, index, id));
        }
        else
        {
            Debug.Log("GET OPPONENT DATA: " + avatarIndex);
            LudoGame.GameManager.Instance.opponentsAvatars[index] = GameObject.Find("StaticGameVariablesContainer").GetComponent<StaticGameVariablesController>().avatars[avatarIndex];
            //LudoGame.GameManager.Instance.opponentsAvatars[index] = null;
            LudoGame.GameManager.Instance.controlAvatars.PlayerJoined(index, id);
        }

    }

    public IEnumerator loadImageOpponent(string url, int index, string id)
    {
        WWW www = new WWW(url);

        yield return www;

        LudoGame.GameManager.Instance.opponentsAvatars[index] = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f), 32);
        LudoGame.GameManager.Instance.controlAvatars.PlayerJoined(index, id);
    }


}
