//using TeenPattiGame;
using DG.Tweening;
using Extensions.Unity.ImageLoader;
using Firebase.Sample.Messaging;
using NetworkManagement;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

[System.Serializable]
public struct PlayerInfo_

{
    public Text displayPlayerName;
    public Text playersId;
    public RawImage PlayerProfile_Image;
    public Image imageLoader_Img;
}
public class UIMainMenManager : MonoBehaviour
{
    public static UIMainMenManager instance;

    [Header("PLAYER DATA")]
    [Tooltip("Setting in Runtime No need to assign")]
    [FormerlySerializedAs("playerInfo")] public PlayerInfo_ playerInfo;

    [Header(" -------- HEADER ----")]
    [FormerlySerializedAs("silverCoinsQty")] public Text silverCoinText;
    [FormerlySerializedAs("goldenCoinsQty")] public Text goldCoinText;

    [Header(" -------- Parents ----")]
    [FormerlySerializedAs("SilverCoinParent")] public GameObject silverCoinUI;
    [FormerlySerializedAs("GoldCoinParent")] public GameObject goldCoinUI;

    [Header("--------- History -------")]
    [FormerlySerializedAs("GameHistory")] public GameObject gameHistoryPanel;

    [Header("-------Daily Reward -------")]
    [FormerlySerializedAs("DailyRewardPanel")] public GameObject dailyRewardPanel;

    [Header("-------Support-----------")]
    [FormerlySerializedAs("SupportPanel")] public GameObject supportPanel;
    [FormerlySerializedAs("BorrowParent")] public GameObject borrowUI;

    [Header("------- --------- --")]
    [FormerlySerializedAs("gameSetterMenu")] public GamesSetterMenu gameSettingsMenu;
    //[FormerlySerializedAs("LoadingPanel")] public GameObject LoadingPanel;
    [FormerlySerializedAs("FriendsHolder")] public GameObject friendsPanel;

    [FormerlySerializedAs("Key_loading")] public const string KeyLoading = "loadingShowed";

    [FormerlySerializedAs("BannerListRaw")] public List<Image> bannerImages = new List<Image>();
    [FormerlySerializedAs("RawImagePrefab")] public GameObject bannerImagePrefab;
    [FormerlySerializedAs("BannerHolder")] public GameObject bannerContainer;

    [FormerlySerializedAs("Logos")] public List<Texture2D> logoTextures = new List<Texture2D>();

    [FormerlySerializedAs("GameRequestPanel")] public GameObject gameRequestPanel;


    [FormerlySerializedAs("panelState")] public PanelState currentPanelState;

    [FormerlySerializedAs("settingPanel")] public GameObject settingsPanel;
    [FormerlySerializedAs("logoutPopPanel")] public GameObject logoutPopup;

    [FormerlySerializedAs("sound")] public bool isSoundOn;
    [FormerlySerializedAs("soundTick")] public GameObject soundOnIndicator;
    [FormerlySerializedAs("soundunTick")] public GameObject soundOffIndicator;

    [FormerlySerializedAs("music")] public bool isMusicOn; // music = false means muted
    [FormerlySerializedAs("musicTickCheck")] public GameObject musicOnIndicator;
    [FormerlySerializedAs("musicUntickCheck")] public GameObject musicOffIndicator;

    [Header("BANNER MOVE FACTORS")]
    [FormerlySerializedAs("moveSpeed")] public float bannerMoveSpeed = 100.0f;
    [FormerlySerializedAs("targetPosition")] public Vector3 bannerTargetPosition;
    [FormerlySerializedAs("initPos")] public Vector3 bannerStartPosition;
    [FormerlySerializedAs("delayBetweenMoves")] public float bannerMoveDelay = 1.0f;

    [FormerlySerializedAs("currentIndex")] private int bannerIndex = 0;
    [FormerlySerializedAs("isMoving")] private bool bannerIsMoving = false;
    [FormerlySerializedAs("moveTimer")] private float bannerMoveTimer = 0.0f;
    [FormerlySerializedAs("resetTimer")] public float bannerResetDelay = 0.0f;
    [FormerlySerializedAs("newResetTimer")] private float bannerResetTimer = 0.0f;


    public enum PanelState
    {
        borrowState,
        challengeBorrowState
    }
    public int Borrow
    {
        get { return PlayerPrefs.GetInt("borrow"); }
        set { PlayerPrefs.SetInt("borrow", value); }
    }

    void Awake()
    {
        PlayerPrefs.DeleteKey("borrow");
        if (instance == null)
        {
            instance = this;
        }

        //staticVariables.gameFeatures.isGoldCoins.Show("Gold ki value bhai");
        goldCoinUI.SetActive(staticVariables.gameFeatures.isGoldCoins);

    }

    void OnEnable()
    {
        if (Screen.orientation == ScreenOrientation.Portrait)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        UIprofileManager.onUpdateName += UpdateTheName;

        if (staticVariables.directSupport == "setting")
        {
            settingsPanel.SetActive(true);
        }
        else if (staticVariables.directSupport == "directsupport")
        {
            settingsPanel.SetActive(false);
        }

        if (staticVariables.isGuest)
        {
            if (PlayerPrefs.GetInt("GuestCoins") <= 0)
            {

                PopupMessageManager.instance.ShowConfirmPanel(body: "Yoy have not enough Coins Please Sign In to continue", () =>
                {
                    SceneManager.LoadSceneAsync(1);
                    staticVariables.ResetGuestStatus();
                }, yesButtonText: "SIGN IN", noButtonStatus: false);

            }
        }
        ApiAndRoomManager._instance.RevertToOriginalStates();
    }
    public void Updatefeatures()
    {
        goldCoinUI.SetActive(staticVariables.gameFeatures.isGoldCoins);
    }


    public void Back()
    {
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
        {
            gb.SetActive(gb.name.Equals("MainMenuPanelBg"));
        }
        SoundManagerMain.instance.ClickSoundPlay();

    }

    public void openPanelByName(string str)
    {
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
        {
            gb.SetActive(gb.name.Equals(str));
        }
    }

    public string SplitUrl(string str)
    {
        string[] splittedform = str.Split("public");
        return splittedform[1];
    }
    public void UpdateTheName()
    {
        FetchAndDisplay_PLayerData(staticVariables.UserProfiledata);
    }


    private void Start()
    {
        if (!PlayerPrefs.HasKey("Music"))
        {
            PlayerPrefs.SetInt("Music", 1);
        }
        bannerResetTimer = bannerResetDelay;
        SaveMusicValue();
        Move();
        FetchAndDisplay_PLayerData(staticVariables.UserProfiledata);
        HomeMenuManager.instance.backButtonHeader?.onClick.AddListener(Back);

    }

    private void FetchAndDisplay_PLayerData(UserModel userModel)
    {
        playerInfo.displayPlayerName.text = userModel.user.first_name.ToString() + " " + userModel.user.last_name.ToString();
        staticVariables.userNickName = playerInfo.displayPlayerName.text;
        playerInfo.playersId.text = "UserID: " + userModel.user._id.ToString();
        // if (staticVariables.ProfilePicture == null)
        {

            if (userModel.user.file_url != null && userModel.user.file_url != "")
            {

                ServerConnection.DownloadSprite("/" + userModel.user.file_url
                    , ImageTexture =>
                    {
                        staticVariables.ProfilePicture = ImageTexture;

                        if (playerInfo.PlayerProfile_Image != null)
                        {
                            playerInfo.PlayerProfile_Image.texture = ImageTexture;
                        }
                        ;
                    },
                    Onfailed =>
                    {
                        //Debug.Log("Failed To Load Image with url " + userModel.user.file_url + " with " + Onfailed);
                    }
                    );

            }
        }
        //else
        //{
        //    //Constants_M.Log("PROFILE PICTURE NIOT NULL");
        //    playerInfo.PlayerProfile_Image.texture = staticVariables.ProfilePicture;
        //}
        if (staticVariables.isGuest)
        {
            silverCoinText.text = userModel.user.silver_balance.ToString();
            goldCoinText.text = userModel.user.gold_balance.ToString();
        }
        else
        {
            silverCoinText.text = "-----".ToString();
            goldCoinText.text = "-----".ToString();
        }

    }


    #region DOWNLOAD banner 
    public void FetchAndShowBanners()
    {
        for (int i = 0; i < staticVariables.BannerUrls.Count; i++)
        {
            BannerDownaloadAndShow(staticVariables.BannerUrls[i], i);
        }
    }

    public async void BannerDownaloadAndShow(string url, int i)
    {
        url = "/" + url;
        await ImageLoader.LoadSprite(ServerConnection.Main_URL() + url).Consume(bannerImages[i]);
        //ServerConnection.DownloadSprite(url
        //  , DownloadedTexture =>
        //  {
        //      bannerImages[i].texture = DownloadedTexture;

        //  },
        //  Onfailed =>
        //  {
        //      //Debug.Log("Failed To Load Image Main menu" + url + " with " + Onfailed);
        //  }
        //  );
    }

    #endregion

    #region Games ButtonFunction

    public void loadProfileView()
    {
        if (staticVariables.isGuest)
        {
            PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
            {
                SceneManager.LoadSceneAsync(1);
                staticVariables.ResetGuestStatus();
            }, yesButtonText: "SIGN IN");
        }
        else
        {
            staticVariables.selectedTab = "profile";
            //SceneLoaderUtility.LoadScene("ProfileScene");
            //ScreenNavigotor_Custom.OpenScreen("USER_Profile_Panel");
            openPanelByName("USER_Profile_Panel");
            UIprofileManager.instance.selectTabs("profile");
            SoundManagerMain.instance.ClickSoundPlay();
        }
    }
    //RAR
    public void GameBtn(string GameID)
    {
        staticVariables.directSupport = "";
        ApiAndRoomManager.currentGameId = int.Parse(GameID);
        if (Borderspanel.ChangeTitle != null)
        {

            Borderspanel.ChangeTitle("SELECT MODE");
        }
        openPanelByName("PlayWithScreen");
        // SceneLoaderUtility.LoadScene("Start");
        SoundManagerMain.instance.ClickSoundPlay();

    }

    #endregion

    #region SCENE LOAD FUNCTIONS
    public void dailyRewardsSceneLoad()
    {
        if (staticVariables.isGuest)
        {
            PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
            {
                SceneManager.LoadSceneAsync(1);
                staticVariables.ResetGuestStatus();
            }, yesButtonText: "SIGN IN");
        }
        else
        {

            staticVariables.directSupport = "";
            //SceneLoaderUtility.LoadScene("DailyRewardScene");
            //DailyRewardPanel.SetActive(true);
            openPanelByName("DailyReward Panel");
        }
        //ScreenNavigotor_Custom.OpenScreen("DailyReward Panel");
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void loadsupportScene()
    {

        openPanelByName("SUPPORT PANEL");
        staticVariables.directSupport = "directsupport";
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void loadsupportfromsettingScene()
    {
        staticVariables.directSupport = "setting";

        if (Borderspanel.logoHandler != null)
        {
            Borderspanel.logoHandler(false);
        }
        openPanelByName("SUPPORT PANEL");
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void loadshopScene()
    {
        if (staticVariables.gameFeatures.isGoldCoins)
        {
            if (staticVariables.isGuest)
            {
                PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
                {
                    SceneManager.LoadSceneAsync(1);
                    staticVariables.ResetGuestStatus();
                }, yesButtonText: "SIGN IN");
            }
            else
            {
                staticVariables.directSupport = "";
                openPanelByName("ShopPanel");
            }
        }
        else
        {
            PopupMessageManager.instance.ShowPopUp("Shop is not available right now.It will be available soon", "NOTE");
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void loadHistoryScene()
    {
        if (staticVariables.isGuest)
        {
            PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
            {
                SceneManager.LoadSceneAsync(1);
                staticVariables.ResetGuestStatus();
            }, yesButtonText: "SIGN IN");
        }
        else
        {
            openPanelByName("GameHistory");
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void loadWithdrawScene()
    {
        if (staticVariables.isGuest)
        {
            PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
            {
                SceneManager.LoadSceneAsync(1);
                staticVariables.ResetGuestStatus();
            }, yesButtonText: "SIGN IN");
        }
        else
        {
            openPanelByName("Withdraw_Panel");
            SoundManagerMain.instance.ClickSoundPlay();
        }
    }


    #endregion

    #region UI BUTTON FUNCTIONS
    public void settingBtn()
    {
        staticVariables.directSupport = "setting";
        ConstantsData_M.Log(" staticVariables.directSupport " + staticVariables.directSupport);

        openPanelByName("SettingPanelBG");

        if (Borderspanel.logoHandler != null)
        {
            Borderspanel.logoHandler(false);
        }

        //ScreenNavigotor_Custom.OpenScreen("SettingPanelBG");
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void mainMenuBackBtn()
    {

        staticVariables.directSupport = "";
        settingsPanel.SetActive(false);

        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void logoutBtn()
    {

        logoutPopup.SetActive(true);
        staticVariables.directSupport = "";
        staticVariables.ResetGuestStatus();

        PlayerPrefs.DeleteKey("userModel");
        PlayerPrefs.DeleteAll();

        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save(); // It's a good practice to call Save after deleting PlayerPrefs
        //Debug.Log("PlayerPrefs cleared.");
    }
    public void ExitGameBtnYes()
    {
        staticVariables.ProfilePicture = null;
        ApiAndRoomManager._instance.ForceDeviceLogout(sucess =>
        {
            Single<int> val = JsonUtility.FromJson<Single<int>>(sucess);
            if (val.status)
            {
                "Log Out".Show();
            }
        });
        SceneLoaderUtility.LoadScene("LoginScene");
        ScreenNavigotor_Custom.GameScreenStack.Clear();
        SoundManagerMain.instance.ClickSoundPlay();

        // ClearPrefs();
        //SaveMusicValueLogin();
    }
    public void ExitGameBtnNo()
    {
        logoutPopup.SetActive(false);
        SoundManagerMain.instance.ClickSoundPlay();
    }

    #endregion

    #region SOUND SYSTEM 


    public void SaveSoundValue()
    {
        if (PlayerPrefs.GetInt("Sound") == 1)
        {
            soundOffIndicator.SetActive(false);
            SoundManagerMain.instance.clickSound.mute = false;
            soundOnIndicator.SetActive(true);

            isSoundOn = true;

        }
        else
        {
            soundOnIndicator.SetActive(false);
            SoundManagerMain.instance.clickSound.mute = true;
            soundOffIndicator.SetActive(true);

            isSoundOn = false;
        }
    }

    public void SoundBtn()
    {
        if (isSoundOn)
        {
            // off
            soundOnIndicator.SetActive(false);
            SoundManagerMain.instance.clickSound.mute = true;
            soundOffIndicator.SetActive(true);

            isSoundOn = false;

            PlayerPrefs.SetInt("Sound", 0);


        }
        else
        {
            SoundManagerMain.instance.ClickSoundPlay();
            // on
            soundOffIndicator.SetActive(false);
            SoundManagerMain.instance.clickSound.mute = false;
            soundOnIndicator.SetActive(true);

            isSoundOn = true;

            PlayerPrefs.SetInt("Sound", 1);
        }
        SoundManagerMain.instance.clickSound.volume = PlayerPrefs.GetInt("Sound");
        SaveSoundValue();
    }


    public void SaveMusicValue()
    {
        if (PlayerPrefs.GetInt("Music") == 0) // music bool value false so it means here music false
        {
            musicOnIndicator.SetActive(false);
            MusicManagerMainMenu.instance.musicBG.mute = true;
            musicOffIndicator.SetActive(true);
            isMusicOn = false;
        }
        else
        {
            musicOffIndicator.SetActive(false);
            MusicManagerMainMenu.instance.musicBG.mute = false;
            musicOnIndicator.SetActive(true);
            isMusicOn = true;
        }
    }

    public void MusicBtn()
    {
        SoundManagerMain.instance.ClickSoundPlay();
        PlayerPrefs.SetInt("Music", PlayerPrefs.GetInt("Music", 1) == 1 ? 0 : 1);

        SaveMusicValue();
    }
    #endregion
    #region Move Banner
    void Move()
    {
        // Initialize starting positions of images
        foreach (Image img in bannerImages)
        {
            img.rectTransform.localPosition = bannerStartPosition; // You can set the initial position here
        }

        // Start the movement coroutine
        StartCoroutine(MoveImagesLoop());
    }

    IEnumerator MoveImagesLoop()
    {
        while (true) // Infinite loop to continuously move images
        {

            if (bannerIndex < bannerImages.Count)
            {
                yield return StartCoroutine(MoveImageCoroutine(bannerImages[bannerIndex]));
            }
            else
            {
                bannerIndex = 0; // Reset index to start over the array
                yield return new WaitForSeconds(bannerMoveDelay); // Wait before starting again
            }
        }
    }

    IEnumerator MoveImageCoroutine(Image img)
    {
        bannerIsMoving = true;
        while (img.rectTransform.localPosition != bannerTargetPosition)
        {
            img.rectTransform.localPosition = Vector3.MoveTowards(img.rectTransform.localPosition, bannerTargetPosition, bannerMoveSpeed * Time.deltaTime);
            yield return null;
        }

        // Wait for a delay between moves
        yield return new WaitForSeconds(bannerMoveDelay);

        // Reset the image position or perform any other actions
        StartCoroutine(ResetAfterDelay(img));
        // Move to the next image in the array
        bannerIndex++;
        bannerIsMoving = false;
    }
    IEnumerator ResetAfterDelay(Image img)
    {
        if (bannerIndex >= bannerImages.Count)
        {
            bannerResetDelay = 0;
        }
        else
        {
            bannerResetDelay = bannerResetTimer;
        }
        yield return new WaitForSeconds(bannerResetDelay);
        img.rectTransform.localPosition = bannerStartPosition; // Reset to initial position or set a new position


    }
    #endregion

    public void SilverGameBtn(string GameID)
    {
        staticVariables.isgoldcoins = false;
        HomeMenuManager.instance.createChallengePanelParent.SetActive(false);
        if (Borderspanel.coinSelected != null)
        {
            Borderspanel.coinSelected(staticVariables.isgoldcoins);
        }
        AightBallPoolNetworkGameAdapter.is3DGraphics = false;
        staticVariables.directSupport = "";
        ApiAndRoomManager.currentGameId = int.Parse(GameID);
        //  ScreenNavigotor_Custom.OpenScreen("FriendRequest");
        openPanelByName("FriendRequest");

        if (Borderspanel.logoHandler != null)
            Borderspanel.logoHandler(true);

        if (Borderspanel.ChangeTitle != null)
        {
            Borderspanel.ChangeTitle("SILVER CHALLENGES");
        }
        // SceneLoaderUtility.LoadScene("Start");
        SoundManagerMain.instance.ClickSoundPlay();

    }
    public void GoldGameBtn(string GameID)
    {
        staticVariables.isgoldcoins = true;

        HomeMenuManager.instance.createChallengePanelParent.SetActive(false);

        if (Borderspanel.coinSelected != null)
        {
            Borderspanel.coinSelected(staticVariables.isgoldcoins);
        }
        AightBallPoolNetworkGameAdapter.is3DGraphics = false;
        staticVariables.directSupport = "";
        ApiAndRoomManager.currentGameId = int.Parse(GameID);

        if (Borderspanel.logoHandler != null)
            Borderspanel.logoHandler(true);

        //  ScreenNavigotor_Custom.OpenScreen("FriendRequest");
        openPanelByName("FriendRequest");

        if (Borderspanel.ChangeTitle != null)
        {

            Borderspanel.ChangeTitle("GOLD CHALLENGES");
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void BorrowSilverCoin()
    {
        if (staticVariables.isGuest)
        {
            PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
            {
                SceneManager.LoadSceneAsync(1);
                staticVariables.ResetGuestStatus();
            }, yesButtonText: "SIGN IN");
        }
        else
        {
            UserFriendList.panelIndex = 1;
            //  PlayerPrefs.DeleteKey("borrow");
            currentPanelState = PanelState.borrowState;
            //    ScreenNavigotor_Custom.OpenScreen("UserFriendsList_");
            openPanelByName("UserFriendsList_");
            Borrow = 1;
            staticVariables.isgoldcoins = true;
            if (Borderspanel.coinSelected != null)
            {
                Borderspanel.coinSelected(staticVariables.isgoldcoins);
            }
            if (Borderspanel.logoHandler != null)
            {
                Borderspanel.logoHandler(false);
            }
        }
    }
    public void OpenFriendPAnel_Button()
    {
        if (staticVariables.isGuest)
        {
            PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
            {
                SceneManager.LoadSceneAsync(1);
                staticVariables.ResetGuestStatus();
            }, yesButtonText: "SIGN IN");
        }
        else
        {
            UserFriendList.panelIndex = 0;

            currentPanelState = PanelState.challengeBorrowState;
            openPanelByName("UserFriendsList_");
            if (Borderspanel.UnselectCoin != null)
            {
                Borderspanel.UnselectCoin(true);
            }
            Borrow = 0;
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void OpenPrivacyPolicy()
    {
        if (!string.IsNullOrEmpty(staticVariables.privacyPolicyLink))
        {
            Application.OpenURL(staticVariables.privacyPolicyLink);
        }
        else
        {
            ConstantsData_M.LogInfo("Privacy Policy URL is not set.");
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    private void OnDisable()
    {
        UIprofileManager.onUpdateName -= UpdateTheName;

    }
    public void OpenWebView()
    {
        SceneManager.LoadScene("UniWebViewDemo");
        SoundManagerMain.instance.ClickSoundPlay();
    }
}
