//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI change wheel button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Modification Wheel")]
public class HR_UIModificationWheel : MonoBehaviour {

    public int wheelIndex = 0;
    public int wheelPrice { get { return HR_Wheels.Instance.wheels[wheelIndex].price; } }

    private Text priceLabel;
    private Image priceImage;

    private void Start() {

        priceLabel = GetComponentInChildren<Text>();
        priceImage = priceLabel.GetComponentInParent<Image>();

    }

    public void OnClick() {

        if (!PlayerPrefs.HasKey("OwnedWheel" + wheelIndex)) {

            BuyWheel();
            return;

        }

        HR_VehicleUpgrade_WheelManager dm = FindObjectOfType<HR_VehicleUpgrade_WheelManager>();

        if (!dm)
            return;

        dm.UpdateWheel(wheelIndex);

    }

    private void Update() {

        if (wheelPrice <= 0)
            PlayerPrefs.SetInt("OwnedWheel" + wheelIndex, 1);

        if (PlayerPrefs.HasKey("OwnedWheel" + wheelIndex)) {

            if (priceImage.gameObject.activeSelf)
                priceImage.gameObject.SetActive(false);

            if (priceLabel.text != "UNLOCKED")
                priceLabel.text = "UNLOCKED";

        } else {

            if (!priceImage.gameObject.activeSelf)
                priceImage.gameObject.SetActive(true);

            if (priceLabel.text != wheelPrice.ToString())
                priceLabel.text = wheelPrice.ToString();

        }

    }

    private void BuyWheel() {

        if (HR_API.GetCurrency() >= wheelPrice) {

            PlayerPrefs.SetInt("OwnedWheel" + wheelIndex, 1);
            HR_API.ConsumeCurrency(wheelPrice);

        } else {

            HR_UIInfoDisplayer.Instance.ShowInfo("Not Enough Coins", "You have to earn " + (wheelPrice - HR_API.GetCurrency()).ToString() + " more coins to buy this wheel", HR_UIInfoDisplayer.InfoType.NotEnoughMoney);

        }

    }

}
