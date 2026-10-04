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

[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Modification Decal")]
public class HR_UI_SetDecal : MonoBehaviour {

    public int index = 0;
    public int price = 5000;

    public bool purchased = false;
    public GameObject buyButton;
    public Text priceText;

    public AudioClip purchaseSound;

    private void OnEnable() {

        CheckPurchase();

    }

    public void CheckPurchase() {

        purchased = PlayerPrefs.HasKey("Decal_" + index.ToString());

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

    public void SetDecal() {

        HR_VehicleUpgrade_DecalManager dm = FindObjectOfType<HR_VehicleUpgrade_DecalManager>();

        if (!dm)
            return;

        dm.SetDecalMaterial(index);

        CheckPurchase();

    }

    public void Buy() {

        if (HR_API.GetCurrency() >= price) {

            HR_API.ConsumeCurrency(price);
            PlayerPrefs.SetInt("Decal_" + index.ToString(), 1);
            SetDecal();

            if (purchaseSound)
                RCC_Core.NewAudioSource(gameObject, purchaseSound.name, 0f, 0f, 1f, purchaseSound, false, true, true);

        } else {

            HR_UIInfoDisplayer.Instance.ShowInfo("Not Enough Coins", "You have to earn " + (price - HR_API.GetCurrency()).ToString() + " more coins to purchase this decal", HR_UIInfoDisplayer.InfoType.NotEnoughMoney);
            return;

        }

    }

}
