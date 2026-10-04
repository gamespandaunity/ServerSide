using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using AssemblyCSharp;
using LudoGame;


public class UpdateCoinsFrameValue : MonoBehaviour
{

    private int currentValue = 0;
    private Text text;
    public int coinValue = 0;
    public int deductcoinValue = 0;
    // Use this for initialization
    void Start()
    {
        text = GetComponent<Text>();
       // InvokeRepeating("CheckAdminAndUpdateValue", 0.2f, 0.2f);
       // InvokeRepeating("CheckAndUpdateValue", 0.2f, 0.2f);
       // InvokeRepeating("CheckAndDeductCoinValue", 0.2f, 0.2f);
    }

    private void CheckAndUpdateValue()
    {
        if (currentValue != LudoGame.GameManager.Instance.myPlayerData.GetCoins())
        {
            currentValue = LudoGame.GameManager.Instance.myPlayerData.GetCoins();
            if (currentValue != 0)
            {
                text.text = LudoGame.GameManager.Instance.myPlayerData.GetCoins().ToString("0,0", CultureInfo.InvariantCulture).Replace(',', ' ') + " ";
            }
            else
            {
                text.text = "0";
            }
        }
    }

    private void CheckAdminAndUpdateValue()
    {
        string url = StaticStrings.baseURL+"get-user-purchased-coins-value.php?playfab_id="+LudoGame.GameManager.Instance.playfabManager.PlayFabId;
            WWW www = new WWW(url);
            StartCoroutine(getPurchasedCoins(www));
    }

    IEnumerator getPurchasedCoins(WWW www)
     {
         yield return www;

         // check for errors
         if (www.error == null)
         {
            
                    coinValue = int.Parse(www.text);
                    if (coinValue != 0)
                    {
                        LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(coinValue);
                        // text.text = LudoGame.GameManager.Instance.myPlayerData.GetCoins().ToString("0,0", CultureInfo.InvariantCulture).Replace(',', ' ') + " ";
                    }
                    // else
                    // {
                    //     Debug.Log("There is no pending coins to credit: ");
                    // }
            // Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
         }
         else{
            Debug.Log("WWW Failed!: " + www.text);// contains all the data sent from the server
         }
     }

     private void CheckAndDeductCoinValue()
    {
        string url = StaticStrings.baseURL+"get-user-deduct-coins-value.php?playfab_id="+LudoGame.GameManager.Instance.playfabManager.PlayFabId;
            WWW www = new WWW(url);
            StartCoroutine(getDeductCoins(www));
    }

    IEnumerator getDeductCoins(WWW www)
     {
         yield return www;

         // check for errors
         if (www.error == null)
         {
            // Debug.Log("Deducted coin value : "+www.text);
                    deductcoinValue = int.Parse(www.text);
                    if (deductcoinValue != 0)
                    {
                        LudoGame.GameManager.Instance.playfabManager.minusCoinsRequest(deductcoinValue);
                        // Dictionary<string, string> data = new Dictionary<string, string>();
                        // data.Add(MyPlayerData.CoinsKey, (LudoGame.GameManager.Instance.myPlayerData.GetCoins() - deductcoinValue).ToString());
                        // LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);
                        // text.text = LudoGame.GameManager.Instance.myPlayerData.GetCoins().ToString("0,0", CultureInfo.InvariantCulture).Replace(',', ' ') + "";
                    }
                    // else
                    // {
                    //     Debug.Log("There is no pending coins to credit: ");
                    // }
            // Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
         }
         else{
            Debug.Log("WWW Failed!: " + www.text);// contains all the data sent from the server
         }
     }

    // Update is called once per frame
    void Update()
    {

    }
}
