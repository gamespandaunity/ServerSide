//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CarRace;

[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Modification Neon")]
public class HR_UI_Neon : MonoBehaviour {

    private string key;

    public int index = 0;
    public int price = 5000;

    public bool purchased = false;
    public GameObject buyButton;
    public Text priceText;

    public AudioClip purchaseSound;

    private void OnEnable() {

        HR_VehicleUpgrade_NeonManager dm = FindObjectOfType<HR_VehicleUpgrade_NeonManager>();

        if (!dm)
            return;

        switch (index) {

            case 0:
                key = dm.red;
                break;

            case 1:
                key = dm.green;
                break;

            case 2:
                key = dm.blue;
                break;

            case 3:
                key = dm.yellow;
                break;

            case 4:
                key = dm.orange;
                break;

            case 5:
                key = dm.white;
                break;

        }

        CheckPurchase();

    }

    public void CheckPurchase() {

        HR_VehicleUpgrade_NeonManager dm = FindObjectOfType<HR_VehicleUpgrade_NeonManager>();

        purchased = PlayerPrefs.HasKey(dm.transform.root.name + key);

        if (index == -1)
            purchased = true;

        if (purchased) {

            if (buyButton)
                buyButton.SetActive(false);

            if (priceText)
                priceText.text = "";

        } else {

            if (buyButton)
                buyButton.SetActive(true);

            if (priceText)
                priceText.text = price.ToString();

        }

    }

    public void Upgrade() {

        HR_VehicleUpgrade_NeonManager dm = FindObjectOfType<HR_VehicleUpgrade_NeonManager>();

        dm.Upgrade(index);

        CheckPurchase();

    }

    public void Buy() {

        if (HR_API.GetCurrency() >= price) {

            HR_API.ConsumeCurrency(price);
            Upgrade();

            if (purchaseSound)
                RCC_Core.NewAudioSource(gameObject, purchaseSound.name, 0f, 0f, 1f, purchaseSound, false, true, true);

        } else {

            HR_UIInfoDisplayer.Instance.ShowInfo("Not Enough Coins", "You have to earn " + (price - HR_API.GetCurrency()).ToString() + " more coins to purchase this neon", HR_UIInfoDisplayer.InfoType.NotEnoughMoney);
            return;

        }

    }

}
