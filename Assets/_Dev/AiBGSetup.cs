using NetworkManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AiBGSetup : MonoBehaviour
{
    public Image aiBetInpBg, logo;
    public TextMeshProUGUI patti3_joinAi_Text;
    public Sprite betInpBg_withoutText, betInpBg_withText;
    public Sprite logo_withoutTextGold;
    public Sprite[] DefaultBgs;

    public TMP_InputField aiBetInputField;

    public GameObject CoinsCollectorPanel;


    // Start is called before the first frame update
    void OnEnable()
    {
        string category = GamesSetterMenu.All_games_Ref.Find(x => x.game_id == ApiAndRoomManager.currentGameId).category;
        if (category.Contains("Casino"))
        {
            staticVariables.isgoldcoins =  ApiAndRoomManager.currentGameId == 5 || ApiAndRoomManager.currentGameId == 9|| ApiAndRoomManager.currentGameId == 10 ? staticVariables.isgoldcoins:false;
            aiBetInpBg.sprite = staticVariables.isgoldcoins ?  logo_withoutTextGold: betInpBg_withoutText;

            aiBetInputField.gameObject.SetActive(false);
            patti3_joinAi_Text.gameObject.SetActive(true);

            patti3_joinAi_Text.text = ApiAndRoomManager.currentGameId == 5 ? (staticVariables.isgoldcoins ? "PLAY ROULETTE WITH Gold BALANCE" : "PLAY ROULETTE WITH SILVER BALANCE")
            : "play bot using Silver balance";
        }
        else
        {
            staticVariables.isgoldcoins = ApiAndRoomManager.currentGameId == 5 ? staticVariables.isgoldcoins : false;
            aiBetInpBg.sprite = betInpBg_withText;
            aiBetInputField.gameObject.SetActive(true);
            //patti3_joinAi_Text.gameObject.SetActive(false);
        }
        aiBetInputField.onValueChanged.AddListener((string playerPrize) =>
        {
            HomeMenuManager.instance.UpdatePrizeStatus(int.Parse(playerPrize));
        });
        HomeMenuManager.instance.UpdatePrizeStatus(int.Parse(aiBetInputField.text));
        //Constants_M.Log("api game id" + APIManager.gameid);
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);
    }
    public void Back()
    {
        GameModeManager.instance.SinglePlayerPanels.SetActive(false);
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void PlayWithAI()
    {

        //Photon Removal  PunNetwork.instance.HasWinnerBeenDeclared = false;
        GameModeManager.isAI = true;
        staticVariables.isPlayingWithAI = true;
        GameTypeSelection.gameTypeEnum = GameTypeSelection._GameTypeEnum.WithAI;
        AightBallPoolNetworkGameAdapter.is3DGraphics = false;
        SoundManagerMain.instance.ClickSoundPlay();
        string category = GamesSetterMenu.All_games_Ref.Find(x => x.game_id == ApiAndRoomManager.currentGameId).category;
        if(staticVariables.isGuest)
        GuestDataManager._instance.UpdateGuestCoins(-1 * staticVariables.currentPrize);
        if (category.Contains("Casino"))
        {
            CoinsCollectorPanel.gameObject.SetActive(false);
            HomeMenuManager.instance.PlayAgainstAI();
        }
        else
        {
            CoinsCollectorPanel.gameObject.SetActive(true);

        }

    }
    private void OnDisable()
    {
        //Constants_M.Log("AI disabled");
        //scriptableSpriteHolder.LogoChangerWith_GameId(Logo);

        //if (staticVariables.gamesId=="1")
        //{
        //    Bg2.sprite = DefaultBgs[0];
        //}
        //else
        //{
        //    Bg2.sprite = DefaultBgs[1];
        //}

    }

}
