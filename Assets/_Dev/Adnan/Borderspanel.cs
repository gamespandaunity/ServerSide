using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Borderspanel : MonoBehaviour
{
    public Sprite[] Borders;
    public Image logo;
    public Sprite[] Coins;
    public Sprite[] CoinsShine;
    public TMP_Text headerText;
    public TMP_Text gericPanelText;
    public Sprite[] aiBg;


    public GameObject SilverBalance, GoldBalance;
    public TextMeshProUGUI SilverText, goldText;
    public delegate void CoinSelected(bool isGoldSelected);
    public static CoinSelected coinSelected, UnselectCoin, logoHandler;
    public delegate void SetTittle(string title);
    public static SetTittle ChangeTitle;

    public delegate void BalanceUpdate(UserBalanceResponse response);
    public static BalanceUpdate balanceUpdate;

    public void OnEnable()
    {

        //if (headerText.text == " Invite Friend And Play ")
        //{
        //    gericPanelText.text = " Invite Friend ";
        //}
        //else
        //{
        //    gericPanelText.text = " Your Challenges ";
        //}
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);



        coinSelected = null;
        coinSelected += OnCoinSelected;

        UnselectCoin = null;
        UnselectCoin += UnSelectedCoin;

        ChangeTitle = null;
        ChangeTitle += OnChangeTitle;

        logoHandler = null;
        logoHandler += GetHandleLogo;

        balanceUpdate = null;
        balanceUpdate += UpdateBalance;

        _UpdateUserBalance.UserUpdateCoin(SilverText, goldText, ApiAndRoomManager.LastFetchedCoins);
    }
    public void UpdateBalance(UserBalanceResponse res)
    {
        if (staticVariables.isGuest)
        {
            SilverText.text = ApiAndRoomManager.LastFetchedCoins.data.silver_balance.ToString();
            goldText.text = ApiAndRoomManager.LastFetchedCoins.data.gold_balance.ToString();
        }

        else
        {
            SilverText.text = res.data.silver_balance.ToString();
            goldText.text = res.data.gold_balance.ToString();
        }
    }

    public void GetHandleLogo(bool isLogoActive)
    {
        //Constants_M.Log($"isLogoActive {isLogoActive}");
        logo.gameObject.SetActive(isLogoActive);
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);

    }

    public void Updatefeatures()
    {
        GoldBalance.SetActive(staticVariables.gameFeatures.isGoldCoins);

    }
    public void OnCoinSelected(bool is_gold_Selected)
    {
        GoldBalance?.SetActive(is_gold_Selected&&staticVariables.gameFeatures.isGoldCoins);
        SilverBalance?.SetActive(is_gold_Selected);
        _UpdateUserBalance.UserUpdateCoin(SilverText, goldText, ApiAndRoomManager.LastFetchedCoins);
    }
    public void UnSelectedCoin(bool is_gold_Selected)
    {
        GoldBalance.SetActive(is_gold_Selected && staticVariables.gameFeatures.isGoldCoins);
        SilverBalance.SetActive(is_gold_Selected);
        _UpdateUserBalance.UserUpdateCoin(SilverText, goldText, ApiAndRoomManager.LastFetchedCoins);
    }
    public void OnChangeTitle(string title)
    {
        headerText.text = title;
        _UpdateUserBalance.UserUpdateCoin(SilverText, goldText, ApiAndRoomManager.LastFetchedCoins);
    }
    private void OnDisable()
    {
        coinSelected -= OnCoinSelected;
        UnselectCoin -= UnSelectedCoin;
        ChangeTitle -= OnChangeTitle;
        logoHandler -= GetHandleLogo;
        balanceUpdate = null;
        OnChangeTitle("");

    }

}
