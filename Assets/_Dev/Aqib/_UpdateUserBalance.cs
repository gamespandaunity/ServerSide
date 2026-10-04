using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class _UpdateUserBalance : MonoBehaviour
{
    public Text GoldCoinstxt, SilverCoinstxt;
    // Start is called before the first frame update
    void UpdateUserCoins(UserBalanceResponse coins)
    {
        //Constants_M.Show(coins + "continously");
        GoldCoinstxt.text = coins.data.gold_balance.ToString();
        SilverCoinstxt.text = coins.data.silver_balance.ToString();
    }
    public static void UserUpdateCoin(TextMeshProUGUI silver, TextMeshProUGUI Gold, UserBalanceResponse coins)
    {
        Gold.text = coins.data.gold_balance.ToString();
        silver.text = coins.data.silver_balance.ToString();
    }

    private void OnEnable()
    {     if(staticVariables.isGuest)
        UpdateUserCoins(ApiAndRoomManager.LastFetchedCoins);

        ApiAndRoomManager.OnCoinsUpdated += UpdateUserCoins;
    }
    private void OnDisable()
    {
        ApiAndRoomManager.OnCoinsUpdated -= UpdateUserCoins;

    }
}
