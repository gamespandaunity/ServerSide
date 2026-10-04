using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
//using Garlic.Plugins.Webview;
//using Garlic.Plugins.Webview.Utils;
using UnityEngine.Networking;
using LitJson;
using System.Text;
//using paytm;
using AssemblyCSharp;
using LudoGame;


public class BuyItemControl : MonoBehaviour {


    public int index = 1;
    public string txnAmount = "";
    public string amt = "";
    public GameObject priceText;
    public GameObject walletResponseText;
    public GameObject walletBalanceText;
    // public GameObject EasyplayLogin;
    // public String urlRazor = "#";
    String uniqueTransId ="";
    String playfabId = "";

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start() {
        walletResponseText.SetActive(false);
        walletResponseText.GetComponent<Text>().text = "";
        LudoGame.GameManager.Instance.BuyItemControl = this;
      //  GarlicWebview.Instance.SetCallbackInterface(new GarlicWebviewCallbackReceiver());
            if (this.index == 1) {
                priceText.GetComponent<Text>().text = "BDT 50.00";
                txnAmount = "50.00";
                amt = "50";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_1000_COINS).metadata.localizedPriceString;
            } else if (this.index == 2) {
                txnAmount = "100.00";
                amt = "100";
                priceText.GetComponent<Text>().text = "BDT 100.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_5000_COINS).metadata.localizedPriceString;
            } 
            else if (this.index == 3) {
                txnAmount = "500.00";
                amt = "500";
                priceText.GetComponent<Text>().text = "BDT 500.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 4) {
                txnAmount = "999.00";
                amt = "999";
                priceText.GetComponent<Text>().text = "BDT 999.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_10000_COINS).metadata.localizedPriceString;
            } else if (this.index == 5) {
                txnAmount = "2499.00";
                amt = "2499";
                priceText.GetComponent<Text>().text = "BDT 2499.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_50000_COINS).metadata.localizedPriceString;
            } else if (this.index == 6) {
                txnAmount = "1000.00";
                amt = "1000";
                priceText.GetComponent<Text>().text = "BDT 1000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 7) {
                txnAmount = "5000.00";
                amt = "5000";
                priceText.GetComponent<Text>().text = "BDT 5000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 8) {
                txnAmount = "10000.00";
                amt = "10000";
                priceText.GetComponent<Text>().text = "BDT 10000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 9) {
                txnAmount = "20000.00";
                amt = "20000";
                priceText.GetComponent<Text>().text = "BDT 20000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 10) {
                txnAmount = "25000.00";
                amt = "25000";
                priceText.GetComponent<Text>().text = "BDT 25000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 11) {
                txnAmount = "30000.00";
                amt = "30000";
                priceText.GetComponent<Text>().text = "BDT 30000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 12) {
                txnAmount = "35000.00";
                amt = "35000";
                priceText.GetComponent<Text>().text = "BDT 35000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 13) {
                txnAmount = "40000.00";
                amt = "40000";
                priceText.GetComponent<Text>().text = "BDT 40000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 14) {
                txnAmount = "45000.00";
                amt = "45000";
                priceText.GetComponent<Text>().text = "BDT 45000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }
            else if (this.index == 15) {
                txnAmount = "50000.00";
                amt = "50000";
                priceText.GetComponent<Text>().text = "BDT 50000.00";
                // priceText.GetComponent<Text>().text = LudoGame.GameManager.Instance.IAPControl.controller.products.WithID(LudoGame.GameManager.Instance.IAPControl.SKU_100000_COINS).metadata.localizedPriceString;
            }

    }

    public void buyItem()
    {
        //callAPI();
       // LudoGame.GameManager.Instance.IAPControl.OnPurchaseClicked(index);
#if UNITY_ANDROID
       // int marginPx = (int)GarlicUtils.DPToPx(50f);
#elif UNITY_IOS
		//Wasi	int marginPx = (int)GarlicUtils.PtToPx(50);
#endif
        uniqueTransId = System.Guid.NewGuid().ToString();
        PlayerPrefs.SetString("TransId", uniqueTransId);
        PlayerPrefs.Save();
        PlayerPrefs.SetInt("index", index);
        PlayerPrefs.Save();
        playfabId = LudoGame.GameManager.Instance.playfabManager.PlayFabId;
        Debug.Log("Playfab id is : " + playfabId + "transaction amount is : " + txnAmount + " Player Name :" + LudoGame.GameManager.Instance.nameMy + " Unique Transaction Id : " + uniqueTransId);

        string email = PlayerPrefs.GetString("email_account");
        //string paytmURL = StaticStrings.baseURL + "order-buy.php?CUST_ID=" + playfabId + "&ORDER_ID=" + uniqueTransId + "&TXN_AMOUNT=" + txnAmount;

        string paytmURL = StaticStrings.baseURL + "PaytmKit/pgRedirect.php?CUST_ID=" + playfabId + "&ORDER_ID=" + uniqueTransId + "&TXN_AMOUNT=" + txnAmount;
        Debug.Log(paytmURL);
        //InAppBrowser.OpenURL(paytmURL);


        InAppBrowser.DisplayOptions displayOptions = new InAppBrowser.DisplayOptions();
        displayOptions.displayURLAsPageTitle = false;
        InAppBrowser.EdgeInsets insets = new InAppBrowser.EdgeInsets(30, 30, 5, 5);
        displayOptions.insets = insets;
        // string urltoload = urlRazor + "player_id=" + playfabId + "&trans_id=" + uniqueTransId + "&index=" + index;

        InAppBrowser.OpenURL(paytmURL, displayOptions);

        //GarlicWebview.Instance.SetMargins(marginPx, marginPx, marginPx, marginPx);
        //GarlicWebview.Instance.Show(paytmURL);
    }

    // public void buyItemWithEasyPay(){
    //     Debug.Log("Player is Log in : "+PlayerPrefs.GetString("easyplay_isLogged"));
    //     if(PlayerPrefs.GetString("easyplay_isLogged") == "1"){
    //         PlayerPrefs.SetString("tran_amt",amt.ToString());
    //         PlayerPrefs.Save();
    //         Debug.Log("Txn Amount : "+ txnAmount);
    //         StartCoroutine(getUserWalletDetails());
    //         // LudoGame.GameManager.Instance.easyWalletDialog.SetActive(true);
    //     //StartCoroutine(buyItemWithEasy());
    //     }
    //     else
    //     {
    //         LudoGame.GameManager.Instance.easyLoginDialog.SetActive(true);
    //     }
    // }

    // public void payEasyWalletBtn()
    // {
    //     StartCoroutine(buyItemWithEasy());

    //     // Invoke("buyItemWithEasy", 3.0f);
    //     // StartCoroutine(buyItemWithEasy());
    // }

    // IEnumerator getUserWalletDetails() {
    //     string userId = PlayerPrefs.GetString("easyplay_userid");
    //     string token = PlayerPrefs.GetString("easyplay_token");
    //     string tokenid = PlayerPrefs.GetString("easyplay_tokeId");
    //     Debug.Log("Easy play login credentials" + userId +" "+ tokenid);
    //     Debug.Log("Player is Log in : "+PlayerPrefs.GetString("easyplay_userid"));
    //     Debug.Log("Player is Log in : "+PlayerPrefs.GetString("easyplay_token"));
    //     Debug.Log("Player is Log in : "+PlayerPrefs.GetString("easyplay_tokeId"));
    //         var request = new UnityWebRequest(StaticStrings.easyPlayDashboard, "POST");
    //         JsonData str = new JsonData();
    //         str["user_id"] = userId;
    //         str["token"] = token;
    //         str["tokenid"] = tokenid;
    //         string jsonStr = str.ToJson();
    //         byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonStr);
    //         request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    //         string getByte = Encoding.ASCII.GetString(bodyRaw);
    //         Debug.Log("Body Raw : "+bodyRaw);
    //         request.downloadHandler = new DownloadHandlerBuffer();
    //         // request.SetRequestHeader("token", "easyplai11header");
    //         request.SetRequestHeader("Content-Type", "application/json");
    //         yield return request.SendWebRequest();
    //         Debug.Log("Response from Easy Play dashboard" + request.downloadHandler.text);
    //         if(request.isNetworkError){
    //             walletResponseText.SetActive(true);
    //                 walletResponseText.GetComponent<Text>().text = request.error;
    //             Debug.Log("Error While Sending: " + request.error);
    //         }
    //         else{
    //             WalletResponse walletResponse = JsonUtility.FromJson<WalletResponse>(request.downloadHandler.text);
    //             // WalletUserData walletUser = JsonUtility.FromJson<WalletUserData>((walletResponse.user_data.ToString()));
    //             // Debug.Log("Status is : "+rootObjects[].status);
    //             if(walletResponse.status == 200){
    //                 Debug.Log("User Data is : " + (walletResponse.data.currunt_gamewallet));
    //                 // LudoGame.GameManager.Instance.easyplayLogin = true;
    //                 PlayerPrefs.SetString("easyplay_playingWallet",walletResponse.data.currunt_gamewallet);
    //                 PlayerPrefs.SetString("user_isactive",walletResponse.user_data[0].isactive);
    //                 PlayerPrefs.SetString("easyplay_name",walletResponse.user_data[0].full_name);
    //                 // PlayerPrefs.SetString("easyplay_wallet",walletResponse.data.wallet);
    //                 LudoGame.GameManager.Instance.easyWalletBalance = walletResponse.data.currunt_gamewallet;
    //                 PlayerPrefs.Save();
    //                 Debug.Log("Data is : "+walletResponse.message);
    //                 // Debug.Log("Playing wallet is : "+walletResponse.user_data.playing_wallet);
    //                 LudoGame.GameManager.Instance.easyWalletDialog.SetActive(true);
    //                 // walletResponseText.SetActive(true);
    //                 walletBalanceText.GetComponent<Text>().text = "Balance : "+LudoGame.GameManager.Instance.easyWalletBalance;
    //             }
    //             else{
    //                 walletResponseText.SetActive(true);
    //                 walletResponseText.GetComponent<Text>().text = walletResponse.message;
    //             }

    //         }
    // }

    // IEnumerator buyItemWithEasy() {
    //         uniqueTransId = System.Guid.NewGuid().ToString();
    //         var request = new UnityWebRequest(StaticStrings.easyPlayUpdate, "POST");
    //         JsonData str = new JsonData();
    //         str["user_id"] = PlayerPrefs.GetString("easyplay_userid");
    //         str["amount"] = PlayerPrefs.GetString("tran_amt");
    //         str["order_id"] = uniqueTransId;
    //         // str["device_token"] = deviceToken;
    //         string jsonStr = str.ToJson();
    //         byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonStr);
    //         request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    //         string getByte = Encoding.ASCII.GetString(bodyRaw);
    //         Debug.Log(getByte);
    //         request.downloadHandler = new DownloadHandlerBuffer();
    //         // request.SetRequestHeader("token", "easyplai11header");
    //         request.SetRequestHeader("Content-Type", "application/json");
    //         yield return request.SendWebRequest();
    //         Debug.Log("Response from Easy Play Update Api: " + request.downloadHandler.text);
    //         if(request.isNetworkError){
    //             walletResponseText.SetActive(true);
    //                 walletResponseText.GetComponent<Text>().text = request.error;
    //             Debug.Log("Error While Sending: " + request.error);
    //         }
    //         else{
    //             WalletUpdateResponse walletUpdateResponse = JsonUtility.FromJson<WalletUpdateResponse>(request.downloadHandler.text);
    //             if(walletUpdateResponse.status == 200){
    //                 walletResponseText.GetComponent<Text>().text = "Purchase Successfull";
    //                 LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(int.Parse(PlayerPrefs.GetString("tran_amt")));
    //                 LudoGame.GameManager.Instance.easyWalletDialog.SetActive(false);
    //             }
    //             else{
    //                 walletResponseText.SetActive(true);
    //                 walletResponseText.GetComponent<Text>().text = walletUpdateResponse.message;
    //                 // LudoGame.GameManager.Instance.easyWalletDialog.SetActive(false);
    //             }


    //             // walletResponseText.GetComponent<Text>().text = "Error : "+request.error;
    //         }



    // }



    // public void OnClickShowWebview()
    // 	{

    // 		#if UNITY_ANDROID
    // 		int marginPx = (int)GarlicUtils.DPToPx (30f);
    // 		#elif UNITY_IOS
    // 		int marginPx = (int)GarlicUtils.PtToPx(50);
    // 		#endif

    // 		GarlicWebview.Instance.SetMargins(marginPx, marginPx, marginPx, marginPx);
    // 		GarlicWebview.Instance.SetFixedRatio(2, 5);
    // 		GarlicWebview.Instance.Show(urlText);
    // 	}

    //private class GarlicWebviewCallbackReceiver : IGarlicWebviewCallback
    //{
    //    public void onClose()
    //    {
    //        // LudoGame.GameManager.Instance.BuyItemControl.callAPI();
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

    IEnumerator WaitForPaymentRequest(WWW www)
     {
         yield return www;

         // check for errors
         if (www.error == null)
         {
            Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
         }
         else{
            Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
         }
     }
}

// [System.Serializable]
//     public class WalletResponse
//     {
//         public int status ;
//         public List<WalletUserData> user_data ;
//         public WalletData data ;
//         public int cool_period ;
//         public string message ;
//     }

// [System.Serializable]
// public class WalletUserData
//     {
//         public string id ;
//         public string ref_id ;
//         public string ref_by ;
//         public string parent_id ;
//         public string leg ;
//         public string left_count ;
//         public string right_count ;
//         public string tokenid ;
//         public object tele_bot_id ;
//         public string email ;
//         public string username ;
//         public string password ;
//         public string password_text ;
//         public string phone ;
//         public string country_code ;
//         public string pan_number ;
//         public string full_name ;
//         public string paid_status ;
//         public string paid_date ;
//         public string default_payment_mode ;
//         public string role ;
//         public string designation ;
//         public string level ;
//         public string enroll_feeId ;
//         public string profile_pic ;
//         public string email_token ;
//         public string wallet_token ;
//         public string password_token ;
//         public string bank_update_token ;
//         public string email_status ;
//         public string password_status ;
//         public string currency ;
//         public object bit_address ;
//         public string bit_address_status ;
//         public string bit_otp ;
//         public string user_type ;
//         public string device_type ;
//         public string device_token ;
//         public string created_date ;
//         public string modified_date ;
//         public string last_resetPass_at ;
//         public string modified_by ;
//         public string paid_by ;
//         public string wallet_amount ;
//         public string playing_wallet ;
//         public string isactive ;
//         public string disable_status ;
//     }
//     [System.Serializable]
//     public class WalletData
//     {
//         public string user_id ;
//         public string token ;
//         public string tokenid ;
//         public string referralEarning ;
//         public string cashbackEarning ;
//         public string cashbackBinary ;
//         public string wallet ;
//         public string curruntworkingBalance ;
//         public string curruntnonworkingBalance ;
//         public string currunt_gamewallet ;
//         public string totalInvestment ;
//         public string kyc ;
//         public int kyc_stats ;
//     }

//     public class WalletUpdateResponse
//     {
//         public int status;
//         public string message;
//     }
    
