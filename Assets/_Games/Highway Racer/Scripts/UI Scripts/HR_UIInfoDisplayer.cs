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
/// UI Info Displayer. Used for informing the player when rewarded, out of money, and about infos.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Info Displayer")]
public class HR_UIInfoDisplayer : MonoBehaviour {

    #region SINGLETON PATTERN
    public static HR_UIInfoDisplayer _instance;
    public static HR_UIInfoDisplayer Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<HR_UIInfoDisplayer>();
            }

            return _instance;
        }
    }
    #endregion

    public InfoType infoType;
    public enum InfoType { NotEnoughMoney, Rewarded, Info }

    public GameObject notEnoughMoney;
    public GameObject reward;
    public GameObject info;

    public Text notEnoughMoneyDescText;
    public Text rewardDescText;
    public Text infoDescText;

    public Button close;

    public void ShowInfo(string title, string description, InfoType type) {

        switch (type) {

            case InfoType.NotEnoughMoney:
                notEnoughMoney.SetActive(true);
                notEnoughMoneyDescText.text = description;
                StartCoroutine("CloseInfoDelayed");
                break;

            case InfoType.Rewarded:
                reward.SetActive(true);
                rewardDescText.text = description;
                StartCoroutine("CloseInfoDelayed");
                break;

            case InfoType.Info:
                info.SetActive(false);
                infoDescText.text = description;
                StartCoroutine("CloseInfoDelayed");
                break;

        }

    }

    public void CloseInfo() {

        notEnoughMoney.SetActive(false);

    }

    private IEnumerator CloseInfoDelayed() {

        yield return new WaitForSeconds(3);

        notEnoughMoney.SetActive(false);
        reward.SetActive(false);
        info.SetActive(false);

    }

}
