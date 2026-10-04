using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

[Serializable]
public class ConsumableData
{
    public string name;
    public string id;
    public string description;
    public float price;
    public string coins;
}

public class ShopScripting : MonoBehaviour
{
    public ConsumableData item;
   


    public void Consumable_Btn_Pressed()
    {
       // ShopUIManager.instance.Consumable_Btn_Pressed(item.id);
    }
                                                                            
    

}
[System.Serializable]
public class GooglePlayReceipt
{
    public string Payload;
    public string Store;
    public string TransactionID;
}

[System.Serializable]
public class PayloadData
{
    public string json;
    public string signature;
    public List<string> skuDetails;
}

[System.Serializable]
public class PurchaseData
{
    public string orderId;
    public string packageName;
    public string productId;
    public long purchaseTime;
    public int purchaseState;
    public string purchaseToken;
    public int quantity;
    public bool acknowledged;
}

[System.Serializable]
public class SkuDetailsData
{
    public string productId;
    public string type;
    public string title;
    public string name;
    public string description;
    public string price;
    public long price_amount_micros;
    public string price_currency_code;
}

