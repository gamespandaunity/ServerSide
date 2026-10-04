using NetworkManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameModeManager : MonoBehaviour
{
    public static GameModeManager instance;
    public GameObject playWithAi;
    public Image logo;
    public GameObject topHeader;
    public GameObject SinglePlayerPanels, MultiplayerPanel;
    public static bool IsCoinTypeSelected = false;
    public Button PlayWithAIBtn;
    public Button PlayWithMultiplayerSilver;
    public Button PlayWithMultiplayerGold;
    public GamesTutorial tutorial;

    private void OnEnable()
    {
        //Constants_M.Log("sehi sehi"+ Borderspanel.ChangeTitle);

        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);
        if (Borderspanel.logoHandler != null)
            Borderspanel.logoHandler(true);

        if (Borderspanel.UnselectCoin != null)
            Borderspanel.UnselectCoin(true);

        if (Borderspanel.logoHandler != null)
            Borderspanel.logoHandler(true);

        topHeader.gameObject.SetActive(true);


        SinglePlayerPanels.SetActive(IsCoinTypeSelected);
        IsCoinTypeSelected = false;
        HomeMenuManager.instance.backButtonHeader.onClick.RemoveAllListeners();
        HomeMenuManager.instance.backButtonHeader.onClick.AddListener(Back);
        PlayWithAIBtn.interactable = ApiAndRoomManager.currentGameId == 5 ? false :
            ApiAndRoomManager.currentGameId == 9 ? false :
            ApiAndRoomManager.currentGameId == 10 ? false : true;
        if (Borderspanel.ChangeTitle != null)
        {
            //Constants_M.Log("If you know you know kon Mohsin kon mohsin");
            Borderspanel.ChangeTitle("SELECT GAME MODE");
        }

        Updatefeatures();
        //FirebaseRemoteConfigManager.instance.OnChildChange += Updatefeatures;
    }

    public void Updatefeatures()
    {
        PlayWithMultiplayerGold.gameObject.SetActive( staticVariables.gameFeatures.isGoldCoins);
    }

    public void Back()
    {
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
        {
            gb.SetActive(gb.name.Equals("MainMenuPanelBg"));
        }

        SoundManagerMain.instance.ClickSoundPlay();
    }

    private void Start()
    {
        HomeMenuManager.instance.backButtonHeader.onClick.RemoveAllListeners();
        HomeMenuManager.instance.backButtonHeader.onClick.AddListener(Back);
        if (Screen.orientation == ScreenOrientation.Portrait)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        if (instance == null)
        {
            instance = this;
        }

      //  if (Borderspanel.UnselectCoin != null)
      //   {
      //       Borderspanel.UnselectCoin(true);
      //   }
    }

    public static bool isAI;

    public void SetGameMode(bool isgoldcoins)
    {
        Debug.Log("GameModeManager SetGameMode called with isgoldcoins: " + isgoldcoins);
        if (Borderspanel.coinSelected != null)
        {
            Borderspanel.coinSelected(true);
        }
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
            staticVariables.isgoldcoins = isgoldcoins;
            SoundManagerMain.instance.ClickSoundPlay();

            if (ApiAndRoomManager.currentGameId == 5 || ApiAndRoomManager.currentGameId == 9 ||
                ApiAndRoomManager.currentGameId == 10)
            {
                SinglePlayerPanels.SetActive(true);
            }
            else
            {
                isAI = false;

                AightBallPoolNetworkGameAdapter.is3DGraphics = false;
                foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
                {
                    gb.SetActive(gb.name.Equals("FriendRequest"));
                }
                SocketIOUnityAdapter.instance.RenderPublicRoomTables();
            }
        }
    }

    private void OnDisable()
    {
        playWithAi.gameObject.SetActive(false);
       // if (Borderspanel.ChangeTitle != null) Borderspanel.ChangeTitle("");
       // if (Borderspanel.logoHandler != null)
      //      Borderspanel.logoHandler(false);
        //FirebaseRemoteConfigManager.instance.OnChildChange -= Updatefeatures;
    }

    public void OpenAiPanel()
    {
        playWithAi.gameObject.SetActive(true);

        SoundManagerMain.instance.ClickSoundPlay();
    }

    public void ShowGameRule()
    {
        PopupMessageManager.instance.ShowTextDetailPanel(tutorial.data[ApiAndRoomManager.currentGameId - 1],
            "HOW TO PLAY!");
        SoundManagerMain.instance.ClickSoundPlay();
    }
}