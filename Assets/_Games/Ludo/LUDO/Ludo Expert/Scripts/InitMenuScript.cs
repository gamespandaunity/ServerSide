using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
//using Photon.Chat;
using UnityEngine.SceneManagement;
using PlayFab.ClientModels;
using PlayFab;
using System.Collections.Generic;
//using Garlic.Plugins.Webview;
//using Garlic.Plugins.Webview.Utils;
#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.Advertisements;
#endif
using AssemblyCSharp;
using LudoGame;
//using Photon.Pun;

public class InitMenuScript : MonoBehaviour
{
    public GameObject rateWindow;
    public GameObject FacebookLinkReward;
    public GameObject rewardDialogText;
    public GameObject FacebookLinkButton;
    public GameObject playerName;
    public GameObject videoRewardText;
    public GameObject playerAvatar;
    public GameObject fbFriendsMenu;
    public GameObject matchPlayer;
    public GameObject backButtonMatchPlayers;
    public GameObject MatchPlayersCanvas;
    public GameObject menuCanvas;
    public GameObject tablesCanvas;
    public GameObject gameTitle;
    public GameObject changeDialog;
    public GameObject inputNewName;
    public GameObject tooShortText;
   public GameObject coinsText;
    public GameObject coinsTextShop;
    public GameObject coinsTab;
    public GameObject TheMillButton;
    public GameObject dialog;
    public GameObject termsbtn;
    public GameObject privacyBtn;
    public GameObject dialogNew;
    public GameObject dialogfailedtofindplayers;
    public GameObject dialogPlayers;
    public GameObject easyPlayLoginDialog;
    public GameObject easyPlayWalletDialog;
    // Use this for initialization
    public GameObject GameConfigurationScreen;
    public GameObject computerGame;
    public GameObject FourPlayerMenuButton;
    public String urlText;
    public String termsUrl;
    public String privacyUrl;
    public AudioSource BackgroundSound;
    public AudioClip BackgroundSfx;

    void Start()
    {


       // GarlicWebview.Instance.SetCallbackInterface(new GarlicWebviewCallbackReceiver());

        if (PlayerPrefs.GetInt(StaticStrings.SoundsKey, 0) == 0)
        {
            AudioListener.volume = 1;
        }
        else
        {
            AudioListener.volume = 0;
        }
        // PlayerPrefs.SetInt(StaticStrings.SoundsKey,0);
        // PlayerPrefs.SetInt(StaticStrings.MusicKey,0);
        // PlayerPrefs.Save();
        // if (PlayerPrefs.GetInt(StaticStrings.MusicKey, 0) == 0)
        // {
        //     AudioListener.volume = 1;
        // }
        // else
        // {
        //     AudioListener.volume = 0;
        // }

        //Call Api to get bid values
        string url = StaticStrings.baseURL + "get-bid-values.php";
        WWW www = new WWW(url);
        StartCoroutine(WaitForRequest(www));
        Debug.Log("Full Url to control : " + url);

        //Call Api to get commision value
        getPayoutCommision();


        FacebookLinkReward.GetComponent<Text>().text = "+ " + StaticStrings.CoinsForLinkToFacebook;

        if (!StaticStrings.isFourPlayerModeEnabled)
        {
            FourPlayerMenuButton.SetActive(false);
        }

        LudoGame.GameManager.Instance.FacebookLinkButton = FacebookLinkButton;
        
        LudoGame.GameManager.Instance.type = MyGameType.TwoPlayer;
        LudoGame.GameManager.Instance.dialog = dialog;
        LudoGame.GameManager.Instance.easyLoginDialog = easyPlayLoginDialog;
        LudoGame.GameManager.Instance.easyWalletDialog = easyPlayWalletDialog;
        LudoGame.GameManager.Instance.objectGame = dialogNew;
        LudoGame.GameManager.Instance.dialogPlayer = dialogPlayers;
        LudoGame.GameManager.Instance.dialogFails = dialogfailedtofindplayers;
        videoRewardText.GetComponent<Text>().text = "+" + StaticStrings.rewardForVideoAd;
        LudoGame.GameManager.Instance.tablesCanvas = tablesCanvas;
        //LudoGame.GameManager.Instance.facebookFriendsMenu = fbFriendsMenu.GetComponent<FacebookFriendsMenu>(); ;
        LudoGame.GameManager.Instance.matchPlayerObject = matchPlayer;
        LudoGame.GameManager.Instance.backButtonMatchPlayers = backButtonMatchPlayers;
        playerName.GetComponent<Text>().text = LudoGame.GameManager.Instance.nameMy;
        LudoGame.GameManager.Instance.MatchPlayersCanvas = MatchPlayersCanvas;

        if (PlayerPrefs.GetString("LoggedType").Equals("Facebook"))
        {
            FacebookLinkButton.SetActive(false);
        }

        if (LudoGame.GameManager.Instance.avatarMy != null)
            playerAvatar.GetComponent<Image>().sprite = LudoGame.GameManager.Instance.avatarMy;

        LudoGame.GameManager.Instance.myAvatarGameObject = playerAvatar;
        LudoGame.GameManager.Instance.myNameGameObject = playerName;

        LudoGame.GameManager.Instance.coinsTextMenu = coinsText;
        LudoGame.GameManager.Instance.coinsTextShop = coinsTextShop;
        LudoGame.GameManager.Instance.initMenuScript = this;

        if (StaticStrings.hideCoinsTabInShop)
        {
            coinsTab.SetActive(false);
        }

#if UNITY_WEBGL
        coinsTab.SetActive(false);
#endif
        
        rewardDialogText.GetComponent<Text>().text = "1 Video = " + StaticStrings.rewardForVideoAd + " Coins";
        //coinsText.GetComponent<Text>().text = LudoGame.GameManager.Instance.myPlayerData.GetCoins() + "";



        Debug.Log("Load ad menu");
        //AdsManager.Instance.adsScript.ShowAd(AdLocation.GameStart);

        if (PlayerPrefs.GetInt("GamesPlayed", 1) % 8 == 0 && PlayerPrefs.GetInt("GameRated", 0) == 0)
        {
            // rateWindow.SetActive(true);
            PlayerPrefs.SetInt("GamesPlayed", PlayerPrefs.GetInt("GamesPlayed", 1) + 1);
        }

      
coinsText.GetComponent<Text>().text=LudoGame.GameManager.Instance.myPlayerData.GetCoins().ToString();
    }







    public void callUpdateToken(string token){
        string playerId = LudoGame.GameManager.Instance.playfabManager.PlayFabId;
        string url = StaticStrings.baseURL+"update-fcm-roken.php?player_id="+playerId+"&token="+token;
            WWW www = new WWW(url);
            StartCoroutine(WaitForUpdateToken(www));
            Debug.Log("Full Url to control : "+url);
    }
    IEnumerator WaitForUpdateToken(WWW www)
     {
         yield return www;

         // check for errors
         if (www.error == null)
         {
             Debug.Log("FCM token Updated : "+ www.text);
         }
         else
         {
             Debug.Log("FCM token Update Failed : "+ www.text);
         }
     }

     public void PlayBackgroundMusic()
     {
        if ((PlayerPrefs.GetInt(StaticStrings.MusicKey,0) == 0))
            {
                BackgroundSound.Play();
            }
            else
            {
                BackgroundSound.Stop();
            }
         

     }
    void OnEnable()
    {
        PlayBackgroundMusic();
        
    }

    IEnumerator WaitForRequest(WWW www)
     {
         yield return www;

         // check for errors
         if (www.error == null)
         {
            string data = www.text;
            string[] values = data.Split(","[0]);  
            StaticStrings.bidValuesStrings = values;
            // StaticStrings.bidValues =  Array.ConvertAll<string, int>(values, int.Parse);
            for (int i=0;i<values.Length;i++)
                {
                StaticStrings.bidValues[i] = int.Parse(values[i]);
                StaticStrings.bidValuesStrings[i] = values[i] + " INR";
                }
            Debug.Log("Data received : " + StaticStrings.bidValues[2]);
            
         }
         else{
            Debug.Log("Error getting data : "+ www.text);
         }
     }

     public void getPayoutCommision(){
         string url = StaticStrings.baseURL+"get-payout-commision.php?";
            WWW www = new WWW(url);
            StartCoroutine(WaitForGetCommision(www));

     }

     IEnumerator WaitForGetCommision(WWW www)
     {
         yield return www;

         // check for errors
         if (www.error == null)
         {
             PlayerPrefs.SetInt("PayoutCommision", int.Parse(www.text));
             PlayerPrefs.Save();
             Debug.Log("Payout Commision is : "+ www.text);
         }
         else{
             Debug.Log("Error getting data : "+ www.text);
         }
     }
    public void openTermsDialog(){
   //     #if UNITY_ANDROID
			//int marginPx = (int)GarlicUtils.DPToPx (40f);
			//#elif UNITY_IOS
			//int marginPx = (int)GarlicUtils.PtToPx(50);
			//#endif

			//GarlicWebview.Instance.SetMargins(marginPx, marginPx, marginPx, marginPx);
			//// GarlicWebview.Instance.SetFixedRatio(2, 5);
			//GarlicWebview.Instance.Show(termsUrl);
    }

    public void openPrivacyDialog(){
   //     #if UNITY_ANDROID
			//int marginPx = (int)GarlicUtils.DPToPx (40f);
			//#elif UNITY_IOS
			//int marginPx = (int)GarlicUtils.PtToPx(50);
			//#endif

			//GarlicWebview.Instance.SetMargins(marginPx, marginPx, marginPx, marginPx);
			//// GarlicWebview.Instance.SetFixedRatio(2, 5);
			//GarlicWebview.Instance.Show(privacyUrl);
    }

    public void QuitApp()
    {
        PlayerPrefs.SetInt("GameRated", 1);
#if UNITY_ANDROID
        Application.OpenURL("market://details?id=" + StaticStrings.AndroidPackageName);
#elif UNITY_IPHONE
        Application.OpenURL("itms-apps://itunes.apple.com/app/id" + StaticStrings.ITunesAppID);
#endif
        //Application.Quit();
    }


    public void LinkToFacebook()
    {
        LudoGame.GameManager.Instance.facebookManager.FBLinkAccount();
    }

    public void ShowGameConfiguration(int index)
    {
        switch (index)
        {
            case 0:
                LudoGame.GameManager.Instance.type = MyGameType.TwoPlayer;
                break;
            case 1:
                LudoGame.GameManager.Instance.type = MyGameType.FourPlayer;
                break;
            case 2:
                LudoGame.GameManager.Instance.type = MyGameType.Private;
                break;
        }
        GameConfigurationScreen.SetActive(true);
        //AdsManager.Instance.adsScript.ShowAd(AdLocation.GamePropertiesWindow);
    }

    public void SetNoOfPlayers(int index)
    {
        switch (index)
        {
            case 0:
                LudoGame.GameManager.Instance.type = MyGameType.TwoPlayer;
                break;
            case 1:
                LudoGame.GameManager.Instance.type = MyGameType.FourPlayer;
                break;
            
        }
    }

    public void TakeScreenshot()
    {
        ScreenCapture.CaptureScreenshot("TestScreenshot.png");
    }


    // Update is called once per frame
    void Update()
    {
    }

    public void showAdStore()
    {
        //AdsManager.Instance.adsScript.ShowAd(AdLocation.StoreWindow);
    }

    public void backToMenuFromTableSelect()
    {
        LudoGame.GameManager.Instance.offlineMode = false;
        tablesCanvas.SetActive(false);
        menuCanvas.SetActive(true);
        gameTitle.SetActive(true);
    }

    public void showSelectTableScene(bool challengeFriend)
    {
        if (!challengeFriend)
            LudoGame.GameManager.Instance.inviteFriendActivated = false;

        //AdsManager.Instance.adsScript.ShowAd(AdLocation.GameStart);
        if (LudoGame.GameManager.Instance.offlineMode)
        {
            // TheMillButton.SetActive(false);
        }
        else
        {
            TheMillButton.SetActive(true);
        }
        menuCanvas.SetActive(false);
        tablesCanvas.SetActive(true);
        gameTitle.SetActive(false);
    }

    // public void playOffline()
    // {
    //     //LudoGame.GameManager.Instance.tableNumber = 0;
    //     LudoGame.GameManager.Instance.offlineMode = true;
    //     LudoGame.GameManager.Instance.roomOwner = true;
    //     showSelectTableScene(false);
    //     //SceneManager.LoadScene(LudoGame.GameManager.Instance.GameScene);
    // }

    public void playOffline()
    {
        //LudoGame.GameManager.Instance.tableNumber = 0;
        LudoGame.GameManager.Instance.offlineMode = true;
        LudoGame.GameManager.Instance.roomOwner = true;
        // showSelectTableScene(false);
        LudoGame.GameManager.Instance.playfabManager.PlayofflineMode();
        //SceneManager.LoadScene(LudoGame.GameManager.Instance.GameScene);
    }

    public void switchUser()
    {
        LudoGame.GameManager.Instance.playfabManager.destroy();
        LudoGame.GameManager.Instance.facebookManager.destroy();
        LudoGame.GameManager.Instance.connectionLost.destroy();

        LudoGame.GameManager.Instance.avatarMy = null;
        //PhotonNetwork.Disconnect();

        PlayerPrefs.DeleteAll();
        LudoGame.GameManager.Instance.resetAllData();
        LocalNotification.ClearNotifications();
        //LudoGame.GameManager.Instance.myPlayerData.GetCoins() = 0;
        SceneManager.LoadScene("LoginSplash");
    }

    public void showChangeDialog()
    {
        changeDialog.SetActive(true);
    }

    public void changeUserName()
    {
        Debug.Log("Change Nickname");

        string newName = inputNewName.GetComponent<Text>().text;
        if (newName.Equals(StaticStrings.addCoinsHackString))
        {
            LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(1000000);
            changeDialog.SetActive(false);
        }
        else
        {
            if (newName.Length > 0)
            {
                UpdateUserTitleDisplayNameRequest displayNameRequest = new UpdateUserTitleDisplayNameRequest()
                {
                    //DisplayName = newName
                    DisplayName = LudoGame.GameManager.Instance.playfabManager.PlayFabId
                };

                PlayFabClientAPI.UpdateUserTitleDisplayName(displayNameRequest, (response) =>
                {
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    data.Add("PlayerName", newName);
                    UpdateUserDataRequest userDataRequest = new UpdateUserDataRequest()
                    {
                        Data = data,
                        Permission = UserDataPermission.Public
                    };

                    PlayFabClientAPI.UpdateUserData(userDataRequest, (result1) =>
                    {
                        Debug.Log("Data updated successfull ");
                        Debug.Log("Title Display name updated successfully");
                        PlayerPrefs.SetString("GuestPlayerName", newName);
                        PlayerPrefs.Save();
                        LudoGame.GameManager.Instance.nameMy = newName;
                        playerName.GetComponent<Text>().text = newName;
                    }, (error1) =>
                    {
                        Debug.Log("Data updated error " + error1.ErrorMessage);
                    }, null);

                }, (error) =>
                {
                    Debug.Log("Title Display name updated error: " + error.Error);

                }, null);

                changeDialog.SetActive(false);
            }
            else
            {
                tooShortText.SetActive(true);
            }
        }



    }

    public void startQuickGame()
    {
        LudoGame.GameManager.Instance.facebookManager.startRandomGame();
    }

    public void startQuickGameTableNumer(int tableNumer, int fee)
    {
        LudoGame.GameManager.Instance.payoutCoins = fee;
        LudoGame.GameManager.Instance.tableNumber = tableNumer;
        LudoGame.GameManager.Instance.facebookManager.startRandomGame();
    }

    public void showFacebookFriends()
    {

        //AdsManager.Instance.adsScript.ShowAd(AdLocation.FacebookFriends);
        //LudoGame.GameManager.Instance.playfabManager.GetPlayfabFriends();
    }

    public void setTableNumber()
    {
        LudoGame.GameManager.Instance.tableNumber = Int32.Parse(GameObject.Find("TextTableNumber").GetComponent<Text>().text);
    }
    public void buy()
    {
       Application.OpenURL("https://t.me/+ORu03eONSd9jOTU1");
    }
    public void tel()
    {
        Application.OpenURL("https://t.me/+ORu03eONSd9jOTU1");
    }

    public void dep()
    {
        Application.OpenURL("https://forms.gle/ctxKvqr9KjB6s8Cw9");
    }
    public void group()
    {
        Application.OpenURL("https://chat.whatsapp.com/Jnyc93ZA3Jy97DSHRYTQ7S");
    }
    //     public void ShowRewardedAd()
    //     {
    // #if UNITY_ANDROID || UNITY_IOS
    //         if (Advertisement.IsReady("rewardedVideo"))
    //         {
    //             var options = new ShowOptions { resultCallback = HandleShowResult };
    //             Advertisement.Show("rewardedVideo", options);
    //         }
    // #endif
    //     }


    // #if UNITY_ANDROID || UNITY_IOS
    //     private void HandleShowResult(ShowResult result)
    //     {
    //         switch (result)
    //         {
    //             case ShowResult.Finished:
    //                 Debug.Log("The ad was successfully shown.");
    //                 LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(StaticStrings.rewardForVideoAd);
    //                 //
    //                 // YOUR CODE TO REWARD THE GAMER
    //                 // Give coins etc.
    //                 break;
    //             case ShowResult.Skipped:
    //                 Debug.Log("The ad was skipped before reaching the end.");
    //                 break;
    //             case ShowResult.Failed:
    //                 Debug.LogError("The ad failed to be shown.");
    //                 break;
    //         }
    //     }
    // #endif

    public void OnClickShowWebview()
		{
            
			//#if UNITY_ANDROID
			//int marginPx = (int)GarlicUtils.DPToPx (30f);
			//#elif UNITY_IOS
			//int marginPx = (int)GarlicUtils.PtToPx(50);
			//#endif

			//GarlicWebview.Instance.SetMargins(marginPx, marginPx, marginPx, marginPx);
			//GarlicWebview.Instance.SetFixedRatio(2, 5);
			//GarlicWebview.Instance.Show(urlText);
		}

    //private class GarlicWebviewCallbackReceiver : IGarlicWebviewCallback
    //{
    //    public void onClose()
    //    {
    //        Debug.Log("GarlicWebview: onClose");
    //    }

    //    public void onPageFinished(string url)
    //    {
    //        Debug.Log("GarlicWebview: onPageFinished [" + url + "]");
    //    }

    //    public void onPageStarted(string url)
    //    {
    //        Debug.Log("GarlicWebview: onPageStarted [" + url + "]");
    //    }

    //    public void onReceivedError(string errorMessage)
    //    {
    //        Debug.Log("GarlicWebview: onReceivedError [" + errorMessage + "]");
    //    }

    //    public void onShow()
    //    {
    //        Debug.Log("GarlicWebview: onShow");
    //    }
    //}

}
