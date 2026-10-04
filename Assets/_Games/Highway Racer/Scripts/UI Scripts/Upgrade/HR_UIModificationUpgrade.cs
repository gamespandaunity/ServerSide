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
/// UI upgrade button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Modification Upgrade")]
public class HR_UIModificationUpgrade : MonoBehaviour {

    public UpgradeClass upgradeClass;
    public enum UpgradeClass { Engine, Handling, Brake, NOS, Speed }
    internal HR_ModApplier applier;

    public int upgradePrice = 2500;
    private bool fullyUpgraded = false;

    public Text priceLabel;
    private Image priceImage;

    private void Awake() {

        priceImage = priceLabel.GetComponentInParent<Image>();

    }

    private void OnEnable() {

        applier = FindObjectOfType<HR_ModApplier>();

    }

    public void OnClick() {

        int playerCoins = HR_API.GetCurrency();

        if (playerCoins < upgradePrice)
            return;

        if (!fullyUpgraded)
            BuyUpgrade();

    }

    private void Update() {

        switch (upgradeClass) {

            case UpgradeClass.Engine:
                if (applier.upgradeManager.engineLevel >= 5)
                    fullyUpgraded = true;
                else
                    fullyUpgraded = false;
                break;
            case UpgradeClass.Handling:
                if (applier.upgradeManager.handlingLevel >= 5)
                    fullyUpgraded = true;
                else
                    fullyUpgraded = false;
                break;
            case UpgradeClass.Brake:
                if (applier.upgradeManager.brakeLevel >= 5)
                    fullyUpgraded = true;
                else
                    fullyUpgraded = false;
                break;
            case UpgradeClass.NOS:
                if (applier.upgradeManager.nosState)
                    fullyUpgraded = true;
                else
                    fullyUpgraded = false;
                break;
            case UpgradeClass.Speed:
                if (applier.upgradeManager.speedLevel >= 5)
                    fullyUpgraded = true;
                else
                    fullyUpgraded = false;
                break;

        }

        if (!fullyUpgraded) {

            if (!priceImage.gameObject.activeSelf)
                priceImage.gameObject.SetActive(true);

            if (priceLabel.text != upgradePrice.ToString())
                priceLabel.text = upgradePrice.ToString();

        } else {

            if (priceImage.gameObject.activeSelf)
                priceImage.gameObject.SetActive(false);

            if (priceLabel.text != "UPGRADED")
                priceLabel.text = "UPGRADED";

        }

    }

    private void BuyUpgrade() {

        int playerCoins = HR_API.GetCurrency();
        HR_ModApplier applier = FindObjectOfType<HR_ModApplier>();

        if (playerCoins >= upgradePrice) {

            switch (upgradeClass) {

                case UpgradeClass.Engine:
                    if (applier.upgradeManager.engineLevel < 5) {
                        applier.upgradeManager.UpgradeEngine();
                        HR_API.ConsumeCurrency(upgradePrice);
                    }
                    break;
                case UpgradeClass.Handling:
                    if (applier.upgradeManager.handlingLevel < 5) {
                        applier.upgradeManager.UpgradeHandling();
                        HR_API.ConsumeCurrency(upgradePrice);
                    }
                    break;
                case UpgradeClass.Brake:
                    if (applier.upgradeManager.brakeLevel < 5) {
                        applier.upgradeManager.UpgradeBrake();
                        HR_API.ConsumeCurrency(upgradePrice);
                    }
                    break;
                case UpgradeClass.NOS:
                    if (!applier.upgradeManager.nosState) {
                        applier.upgradeManager.UpgradeNOS();
                        HR_API.ConsumeCurrency(upgradePrice);
                    }
                    break;
                case UpgradeClass.Speed:
                    if (applier.upgradeManager.speedLevel < 5) {
                        applier.upgradeManager.UpgradeSpeed();
                        HR_API.ConsumeCurrency(upgradePrice);
                    }
                    break;

            }

        } else {

            HR_UIInfoDisplayer.Instance.ShowInfo("Not Enough Coins", "You have to earn " + (upgradePrice - HR_API.GetCurrency()).ToString() + " more coins to purchase this upgrade", HR_UIInfoDisplayer.InfoType.NotEnoughMoney);
            return;

        }

    }

}
