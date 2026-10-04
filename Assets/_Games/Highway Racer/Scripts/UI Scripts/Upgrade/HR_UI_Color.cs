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

[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Modification Color")]
public class HR_UI_Color : MonoBehaviour {

    public Color color = Color.white;
    public int price = 5000;

    public bool purchased = false;
    public GameObject buyButton;
    public Text priceText;

    public AudioClip purchaseSound;

    private void OnEnable() {

        CheckPurchase();

    }

    public void CheckPurchase() {

        HR_VehicleUpgrade_PaintManager dm = FindObjectOfType<HR_VehicleUpgrade_PaintManager>();

        if (!dm)
            return;

        if (PlayerPrefs.HasKey(dm.transform.root.name + "BodyColor")) {

            if (PlayerPrefs.HasKey(dm.transform.root.name + "BodyColor" + color.ToString()))
                purchased = true;
            else
                purchased = false;

        } else {

            purchased = false;

        }

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

        HR_VehicleUpgrade_PaintManager dm = FindObjectOfType<HR_VehicleUpgrade_PaintManager>();

        dm.Paint(color);

        CheckPurchase();

    }

    public void Buy() {

        if (HR_API.GetCurrency() >= price) {

            HR_API.ConsumeCurrency(price);
            Upgrade();

            if (purchaseSound)
                RCC_Core.NewAudioSource(gameObject, purchaseSound.name, 0f, 0f, 1f, purchaseSound, false, true, true);

        } else {

            HR_UIInfoDisplayer.Instance.ShowInfo("Not Enough Coins", "You have to earn " + (price - HR_API.GetCurrency()).ToString() + " more coins to purchase this paint", HR_UIInfoDisplayer.InfoType.NotEnoughMoney);
            return;

        }

    }

}
