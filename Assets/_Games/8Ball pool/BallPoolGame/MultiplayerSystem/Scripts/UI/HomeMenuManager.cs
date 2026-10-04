using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NetworkManagement;

using System.Text;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class HomeMenuManager : MonoBehaviour
{
    [FormerlySerializedAs("BackButton_TopHeader")]
    public Button backButtonHeader;
    [FormerlySerializedAs("prizeInput")]
    public TMP_InputField challengePrizeInput;
    [FormerlySerializedAs("yourChallengePanel")]
    public GameObject yourChallengePanelObject;
    [FormerlySerializedAs("browseChallengePopupPanel")]
    public GameObject browseChallengePopup;
    [FormerlySerializedAs("playerNameTxt")]
    public Text playerNameText;
    [FormerlySerializedAs("yourChallengeLoader")]
    public GameObject yourChallengeLoadingIndicator;
    [FormerlySerializedAs("RejectedOpponentChallengePanel")]
    public GameObject rejectedChallengePanel;
    [FormerlySerializedAs("MainCanvas")]
    public GameObject mainCanvasObject;
    [FormerlySerializedAs("SilverCoinTxt_Header")]
    public TextMeshProUGUI silverCoinTextHeader;
    [FormerlySerializedAs("GoldCoinTxt_Header")]
    public TextMeshProUGUI goldCoinTextHeader;
    [FormerlySerializedAs("createChallenge_newPanel")]
    public CreateChallenge_New createChallengePanel;
    [FormerlySerializedAs("newCreateChallenge_Parent")]
    public GameObject createChallengePanelParent;
    public GameObject SuccessFullPurchasePanel;
    [FormerlySerializedAs("newCreateChallenge_Parent")]
    public GameObject MenuChallengePanelParent;
    public static HomeMenuManager instance;

    [HideInInspector]
    public NetworkGameAdapter networkGameAdapter;
    // string shareLink = "https://gamesbaba.com.au/";
    void OnEnable()
    {
        // ON HEADER SILVER AND GOLD COIN UPDATE
        ApiAndRoomManager.OnCoinsUpdated += ModifyUserCoins;
        Invoke("UpdateBalance", 2);
        if (Borderspanel.ChangeTitle != null)
        {
            Borderspanel.ChangeTitle("");
        }
        Time.timeScale = 1;
        //Photon Removal  PunNetwork.instance?.Cleanup();
    }
    void UpdateBalance()
    {
        ApiAndRoomManager._instance.ModifyUserBalance();
    }
    public void ModifyUserCoins(UserBalanceResponse coins)
    {
        silverCoinTextHeader.text = coins.data.silver_balance.ToString();
        goldCoinTextHeader.text = coins.data.gold_balance.ToString();
    }

    void Start()
    {

        if (Screen.orientation == ScreenOrientation.Portrait)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        if (instance == null)
        {
            instance = this;
        }
        Resources.UnloadUnusedAssets();
        EightBallPoolNetworkManager.LoadMainPlayer();
        UserModel userModel2 = staticVariables.UserProfiledata;
        this.playerNameText.text = userModel2.user.first_name + " " + userModel2.user.last_name;
        //Photon Removal    PunNetwork.instance?.VerifyConnection();
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
        {
            gb.SetActive(gb.name.Equals("MainMenuPanelBg"));
        }
        Resources.UnloadUnusedAssets();
    }
    public void CopyReferrelCode()
    {
        SoundManagerMain.instance.ClickSoundPlay();
        UserModel userModel2 = staticVariables.UserProfiledata;
        GUIUtility.systemCopyBuffer = userModel2.user.referral_code;
    }
    public void ReferralLinkButton()
    {
        SoundManagerMain.instance.ClickSoundPlay();

        StartCoroutine(SendReferralLink());
    }
    private IEnumerator SendReferralLink()
    {
        yield return new WaitForEndOfFrame();
        UserModel userModel2 = staticVariables.UserProfiledata;

        string paramvalues = $"2/1/1/{userModel2.user.referral_code}";

        byte[] bytesToEncode = Encoding.UTF8.GetBytes(paramvalues);

        string base64String = System.Convert.ToBase64String(bytesToEncode);


        string shareText = UnityWebRequest.EscapeURL(base64String);
        //      new NativeShare()
        //.SetSubject(" Exciting Betting Adventure Awaits! ").SetText($"Get Your Bonus Now! Use Code: {staticVariables.UserProfiledata.user.referral_code} \n Claim instant rewards and join the most thrilling betting experience. \n Download now and turn luck into cash! ").SetUrl("shareLink")
        //.SetCallback((result, shareTarget) => { }).Share();
        new NativeShare()
            .SetSubject(" Exciting Betting Adventure Awaits! ").SetText($"Get Your Bonus Now! Use Code: {staticVariables.UserProfiledata.user.referral_code} \n Claim instant rewards and join the most thrilling betting experience. \n Download now and turn luck into cash! ").SetUrl("")
            .SetCallback((result, shareTarget) => { }).Share();
        //Debug.Log("Share result: " + result + ", selected app: " + shareTarget))
    }

    void Awake()
    {
        DataManager.SaveGameData();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        EightBallPoolNetworkManager.initialized = true;
        networkGameAdapter = new AightBallPoolNetworkGameAdapter(this);
        //  NetworkManager.network.SetAdapter(networkGameAdapter);
        DataManager.SetIntData("Is3DGraphics", AightBallPoolNetworkGameAdapter.is3DGraphics ? 0 : 1);


        EightBallPoolNetworkManager.OnMainPlayerLoaded += OnMainPlayerReady;


        RoomsListManager.OnSelecRoom += OnPlayerProfileSelected;
        //Photon Removal   PhotonNetwork.JoinLobby();
        //NetworkManager.network.OnNetwork += NetworkManager_OnNetworkEvent;
        //NetworkManager.network.Resset();
        challengePrizeInput.onEndEdit.AddListener((string playerPrize) =>
        {
            UpdatePrizeStatus(int.Parse(playerPrize));

        });

        CleanUpNetworkDataCorotine = StartCoroutine(CleanUpNetworkData());
    }
    Coroutine CleanUpNetworkDataCorotine;
    IEnumerator CleanUpNetworkData()
    {
        yield return 3;
        MirrorNetwork.Instance.cleanup();

    }

    void OnDisable()
    {
        StopCoroutine(CleanUpNetworkDataCorotine);
        RoomsListManager.OnSelecRoom -= OnPlayerProfileSelected;
        EightBallPoolNetworkManager.Disable();
    }

    public void UpdatePrizeStatus(int prize)
    {

        networkGameAdapter.OnUpdatePrize(prize);
        EightBallPoolNetworkManager.mainPlayer.prize = prize;
        staticVariables.currentPrize = prize;
        staticVariables.currentPrize.Show("UpdatePrizeStatus");
    }
    public void UpdateOverStatus(int overs)
    {

        networkGameAdapter.OnUpdateOver(overs);
        EightBallPoolNetworkManager.mainPlayer.overIndex = overs;
        SelectOver.SelectedOver = overs;

    }



    void OnPlayerProfileSelected(NetworkManagement.Room room)
    {
        PlayerProfile player = room.mainPlayer;
        FetchOpponentAvatarByName(player);
    }



    void OnMainPlayerReady(NetworkManagement.PlayerProfile player)
    {
        if (player != null)
        {
            networkGameAdapter.OnMainPlayerLoaded(int.Parse(player.userId), player.userName, player.coins, player.image, player.imageURL, player.prize);

            challengePrizeInput.text = player.prize + "";
        }

    }

    public void PlayAgainstAI()
    {
        string opponentrName = "AI Player";
        Texture2D opponentImage = SpritesManager.Instance.spritesScriptable.aiImg;
        int opponentCoins = Random.Range(2 * EightBallPoolNetworkManager.mainPlayer.prize, 10 * EightBallPoolNetworkManager.mainPlayer.prize);

        opponentrName = opponentImage.name = "AI";
        networkGameAdapter.OnGoToPlayWithAI(1, opponentrName, opponentCoins, opponentImage, "");
    }

    void FetchOpponentAvatarByName(PlayerProfile player)
    {
        if (player != null && !string.IsNullOrEmpty(player.imageName))
        {
            Texture2D playerImage = null;
            if (playerImage)
            {
                player.SetImage(playerImage);
            }
        }
    }
    public List<GameButton_InfoSetter> All_games_Ref = new List<GameButton_InfoSetter>();
    public GameDetailParent detail;

    public void MakeChallenge()
    {
        //All_games_Ref = staticVariables.All_games_Ref;
        //detail = staticVariables.detail;
        //ApiAndRoomManager.currentGameId.Show("Current Game ID");
        //staticVariables.All_games_Ref.Count.Show("ALL GAMES COUNT");
        foreach (game_detail game in detail.game_detail)
        {
            if (game.game_id == ApiAndRoomManager.currentGameId)
            {
                string category = game.category;
                if (game.category.Contains("Casino"))
                {
                    Debug.Log("CREATE CHALLENGE CATEGORY" + category);
                    createChallengePanel.CreateRoom();
#if !UNITY_EDITOR
            AndroidUtility._ShowAndroidToastMessage("CHALLENGE HAS BEEN CREATED");
#endif
                }
                else
                {
                    Debug.Log("CREATE CHALLENGE CATEGORY" + category);
                    createChallengePanelParent.SetActive(true);
                }
                break;
            }
        }
        //ConstantsData_M.Log("CREATE CHALLENGE CATEGORY" + category);
        //ConstantsData_M.Log("CREATE CHALLENGE CATEGORY" + category);

    }

}
