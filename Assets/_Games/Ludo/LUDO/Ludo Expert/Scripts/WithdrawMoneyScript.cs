using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using LudoGame;

//using Garlic.Plugins.Webview;
//using Garlic.Plugins.Webview.Utils;
using AssemblyCSharp;

public class WithdrawMoneyScript : MonoBehaviour
{
    public GameObject withdrawlAmount;
    public GameObject fullName;
    public GameObject paypalEmail;
    public GameObject gpayNumber;
    public GameObject phonepeNumber;
    public GameObject saveBtn;
    public GameObject bankName;
    public GameObject accountNumber;
    public GameObject ifscCode;
    // Start is called before the first frame update
    void Start()
    {
      //  GarlicWebview.Instance.SetCallbackInterface(new GarlicWebviewCallbackReceiver());
        LudoGame.GameManager.Instance.saveButton = saveBtn;
        saveBtn.SetActive(true);
        fullName.GetComponent<InputField>().text = LudoGame.GameManager.Instance.nameMy;

        
    }

    public void callAPI(){
        saveBtn.SetActive(false);
        if ((withdrawlAmount.GetComponent<InputField>().text) !="" && (fullName.GetComponent<InputField>().text) !="")
        {
         String paypalId = paypalEmail.GetComponent<InputField>().text;
         int amountWithdrawl = int.Parse(withdrawlAmount.GetComponent<InputField>().text);
         
         String fName = fullName.GetComponent<InputField>().text;
         String gpayName = gpayNumber.GetComponent<InputField>().text;
         String phonepeName = phonepeNumber.GetComponent<InputField>().text;
         String bName = bankName.GetComponent<InputField>().text;
         String aNumber = accountNumber.GetComponent<InputField>().text;
         String ifscC = ifscCode.GetComponent<InputField>().text;

         String playerId = LudoGame.GameManager.Instance.playfabManager.PlayFabId;
         String totalCoins = LudoGame.GameManager.Instance.myPlayerData.GetCoins().ToString();
         String totalEarnings = LudoGame.GameManager.Instance.myPlayerData.GetTotalEarnings().ToString();
        //  int index_num = PlayerPrefs.GetInt("index");
        string url = StaticStrings.baseURL+"payment-request.php?player_id="+playerId+"&amount="+amountWithdrawl+"&full_name="+fName+"&total_coins="+totalCoins;
        Debug.Log("Full Url to control : "+url);
        if ((amountWithdrawl <= LudoGame.GameManager.Instance.myPlayerData.GetCoins()) && (amountWithdrawl >= 200)){
            if((PlayerPrefs.GetInt("payment_method") == 2) && fName !="" && paypalId !=""){
                url += "&paypal_id="+paypalId;
                url += "&payment_method=paytm";
                WWW www = new WWW(url);

                Debug.Log("Complete url for Paytm account : "+ url);
                StartCoroutine(WaitForRequest(www));
                Debug.Log("you have enough coins : "+LudoGame.GameManager.Instance.myPlayerData.GetCoins());
            }
            if((PlayerPrefs.GetInt("payment_method") == 3) && fName !="" && gpayName !=""){
                url += "&gpay_id="+gpayName;
                url += "&payment_method=google_pay";
                WWW www = new WWW(url);

                Debug.Log("Complete url for Google Pay : "+ url);
                StartCoroutine(WaitForRequest(www));
                Debug.Log("you have enough coins : "+LudoGame.GameManager.Instance.myPlayerData.GetCoins());
            }
            if((PlayerPrefs.GetInt("payment_method") == 4) && fName !="" && phonepeName !=""){
                url += "&phonepe_id="+phonepeName;
                url += "&payment_method=phone_pe";
                WWW www = new WWW(url);

                Debug.Log("Complete url for PhonePe : "+ url);
                StartCoroutine(WaitForRequest(www));
                Debug.Log("you have enough coins : "+LudoGame.GameManager.Instance.myPlayerData.GetCoins());
            }
            else if((PlayerPrefs.GetInt("payment_method") == 5) && fName !="" && bName !="" && aNumber !="" && ifscC !=""){
                url += "&bank_name="+bName;
                url += "&account_number="+aNumber;
                url += "&ifsc_code="+ifscC;
                url += "&payment_method=Bank Account";
                WWW www = new WWW(url);

                Debug.Log("Complete url for bank account : "+ url);
                StartCoroutine(WaitForRequest(www));
                Debug.Log("you have enough coins : "+LudoGame.GameManager.Instance.myPlayerData.GetCoins());
            }
            else{
                LudoGame.GameManager.Instance.dialogNew.dialogTxt.GetComponent<Text>().text = "Please select payment method";
                LudoGame.GameManager.Instance.objectGame.SetActive(true);
                saveBtn.SetActive(true);
                Debug.Log("Payment Method Not Selected");
            }
            
        }
        else{
            LudoGame.GameManager.Instance.dialogNew.dialogTxt.GetComponent<Text>().text = "You don't have enough coins";
            LudoGame.GameManager.Instance.objectGame.SetActive(true);
            saveBtn.SetActive(true);
            // WithdrawDetailWindow.saveBtn.SetActive(true);
            Debug.Log("you don't have enough coins : "+amountWithdrawl);
        }
            
        }
        else{
            saveBtn.SetActive(true);
            LudoGame.GameManager.Instance.dialogNew.dialogTxt.GetComponent<Text>().text = "Please fill all the details.";
            LudoGame.GameManager.Instance.objectGame.SetActive(true);
            
        }
    }
    IEnumerator WaitForRequest(WWW www)
     {
         yield return www;
         int amountdeduct = int.Parse(withdrawlAmount.GetComponent<InputField>().text);

         // check for errors
         if (www.error == null)
         {
             if(www.text == "True"){
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    data.Add(MyPlayerData.CoinsKey, (LudoGame.GameManager.Instance.myPlayerData.GetCoins() - amountdeduct).ToString());
                    LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);
                    LudoGame.GameManager.Instance.dialogNew.dialogTxt.GetComponent<Text>().text = "Payment Request Successful";
                    LudoGame.GameManager.Instance.objectGame.SetActive(true);
                    saveBtn.SetActive(true);
                    Debug.Log("Working good : Amount to Deduct "+amountdeduct);
             }
             else {
                 LudoGame.GameManager.Instance.dialogNew.dialogTxt.GetComponent<Text>().text = "Payment Request Unsuccessful, Please try again.";
                 LudoGame.GameManager.Instance.objectGame.SetActive(true);
                 saveBtn.SetActive(true);
                 Debug.Log("There is an error: " + www.text);
             }
             Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
             
         } else {
                    LudoGame.GameManager.Instance.dialogNew.dialogTxt.GetComponent<Text>().text = www.error;
                    LudoGame.GameManager.Instance.objectGame.SetActive(true);
                    saveBtn.SetActive(true);
                    Debug.Log("WWW Error: "+ www.error);
         }    
     }

     public void OnClickShowPaymentHistory()
		{
            
			// #if UNITY_ANDROID
			//int marginPx = (int)GarlicUtils.DPToPx (40f);
			//#elif UNITY_IOS
			//int marginPx = (int)GarlicUtils.PtToPx(50);
			//#endif

   //         String playerId = LudoGame.GameManager.Instance.playfabManager.PlayFabId;
   //         string paymentHistoryURL = StaticStrings.baseURL+"user-payment-history.php?player_id="+playerId;
            
			//// GarlicWebview.Instance.SetMargins(marginPx, marginPx, marginPx, marginPx);
			//GarlicWebview.Instance.SetFixedRatio(6, 9);
			//// GarlicWebview.Instance.Show(urlText);
   //         GarlicWebview.Instance.SetMargins(marginPx, marginPx, marginPx, marginPx);
   //         GarlicWebview.Instance.Show(paymentHistoryURL);
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

    public void HandleDropdownInputData(int val)
    {
        if (val == 0)
        {
            paypalEmail.SetActive(false);
            bankName.SetActive(false);
            accountNumber.SetActive(false);
            ifscCode.SetActive(false);
            gpayNumber.SetActive(false);
            phonepeNumber.SetActive(false);
            PlayerPrefs.SetInt("payment_method", 1);
            Debug.Log("Payment Method Not Selected : " + val);
        }
        else if (val == 1)
        {
            paypalEmail.SetActive(true);
            bankName.SetActive(false);
            accountNumber.SetActive(false);
            ifscCode.SetActive(false);
            gpayNumber.SetActive(false);
            phonepeNumber.SetActive(false);
            PlayerPrefs.SetInt("payment_method", 2);
            Debug.Log("Payment Method Selected : " + val);
        }
        else if (val == 2)
        {
            paypalEmail.SetActive(false);
            bankName.SetActive(false);
            accountNumber.SetActive(false);
            ifscCode.SetActive(false);
            gpayNumber.SetActive(true);
            phonepeNumber.SetActive(false);

            PlayerPrefs.SetInt("payment_method", 3);
            Debug.Log("Payment Method Selected : " + val);
        }
        else if (val == 3)
        {
            paypalEmail.SetActive(false);
            bankName.SetActive(false);
            accountNumber.SetActive(false);
            ifscCode.SetActive(false);
            gpayNumber.SetActive(false);
            phonepeNumber.SetActive(true);

            PlayerPrefs.SetInt("payment_method", 4);
            Debug.Log("Payment Method Selected : " + val);
        }
        else if (val == 4)
        {
            paypalEmail.SetActive(false);
            bankName.SetActive(true);
            accountNumber.SetActive(true);
            ifscCode.SetActive(true);
            gpayNumber.SetActive(false);
            phonepeNumber.SetActive(false);
            PlayerPrefs.SetInt("payment_method", 5);
            Debug.Log("Payment Method Selected : " + val);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
