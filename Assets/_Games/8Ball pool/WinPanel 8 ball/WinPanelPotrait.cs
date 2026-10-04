using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinPanelPotrait : MonoBehaviour
{
    public Image[] coins;
    public Image coinInChest;
    [SerializeField] int betAmount, secondBetAmount, initiator = 0;
    int totalBalance;
    [SerializeField]
    private float waitSeconds_Counter, enable2CoinAnim_wait;
    private bool isDecrementing = false, isCompleted = false;
    public GameObject Coin2, confetti1, confetti2, confetti3;
    public AudioSource collectCoinsSource;
    public AudioClip coinCollectionClips;
    [Serializable]
    public struct PopUp
    {
        public GameObject PopUpObj;
        public TextMeshProUGUI popUpText;
    }
    public PopUp popUp;
    public bool isPlayerWin = false;


    private void OnEnable()
    {
        if (staticVariables.isgoldcoins)
        {
            coinInChest.sprite = SpritesManager.Instance.spritesScriptable.coinsInGold;
            for (int i = 0; i < coins.Length; i++)
            {
                coins[i].sprite = SpritesManager.Instance.spritesScriptable.goldenCoin;

            }
            //    WinnerCoinCounter.text = userModel.user.silver_balance.ToString();
            //   totalBalance = userModel.user.gold_balance;
        }
        else
        {
            coinInChest.sprite = SpritesManager.Instance.spritesScriptable.coinsInSilver;
            for (int i = 0; i < coins.Length; i++)
            {
                coins[i].sprite = SpritesManager.Instance.spritesScriptable.silvercoin;

            }
            //       WinnerCoinCounter.text = userModel.user.gold_balance.ToString();
            //        totalBalance = userModel.user.silver_balance;
        }

    }
    bool iFinished;
    public void onWinInit(bool win)
    {
        if (!iFinished)
        {
            ConstantsData_M.Log("ONwINiNIT" + win);
            iFinished = true;
            if (win)
            {
                confetti1.SetActive(true);
                confetti1.SetActive(true);
                confetti3.SetActive(true);
                popUp.PopUpObj.SetActive(true);
                popUp.popUpText.text = "Congratulations! Your account has been credited with coins";
                if (staticVariables.isGuest)
                {
                    GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);
                }
            }
            else
            {
                popUp.popUpText.text = "Your balance has been deducted from your account. Please try again";
                confetti1.SetActive(false);
                confetti1.SetActive(false);
                confetti3.SetActive(false);
                popUp.PopUpObj.SetActive(true);
                if (staticVariables.isGuest)
                {
                   // GuestGrandScripts.instance.ChangeGuestCoins(-1 * staticVariables.currentPrize);
                }

            }
        }
    }
    void Start()
    {
        collectCoinsSource.clip = coinCollectionClips;
        collectCoinsSource.loop = false;
        collectCoinsSource.Play();

        StartCoroutine(Coin2Activate_Animation());

        if (betAmount > 0 && !isDecrementing)
        {
            //     StartCoroutine(DecrementBetAmount());

        }
        if (initiator <= betAmount && !isCompleted)
        {

            // StartCoroutine(InitiatorIncrementor());
        }

    }
    IEnumerator Coin2Activate_Animation()
    {
        yield return new WaitForSeconds(enable2CoinAnim_wait);
        Coin2.gameObject.SetActive(true);
    }
    IEnumerator DecrementBetAmount()
    {
        isDecrementing = true;


        while (betAmount > 0)
        {
            if (betAmount >= 20 && betAmount <= 100)
            {
                betAmount = Mathf.Max(0, betAmount - 5);
            }

            else
            {
                betAmount = Mathf.Max(0, betAmount - 80);
            }

            //looserCoinCounter.text = betAmount.ToString();
            yield return new WaitForSeconds(waitSeconds_Counter); // Adjust the delay to control the speed
        }


    }
    IEnumerator InitiatorIncrementor()
    {
        isCompleted = true;
        while (initiator < secondBetAmount)
        {

            if (secondBetAmount > 0 && secondBetAmount <= 100)
            {
                //initiator += 20;
                initiator = Mathf.Min(initiator + 20, secondBetAmount);
            }
            else if (secondBetAmount > 100 && secondBetAmount <= 200)
            {
                //initiator += 35;
                initiator = Mathf.Min(initiator + 35, secondBetAmount);
            }
            else if (secondBetAmount > 200 && secondBetAmount <= 1000)
            {
                //  initiator += 110;
                initiator = Mathf.Min(initiator + 110, secondBetAmount);

            }
            else
            {
                //initiator += 850;
                initiator = Mathf.Min(initiator + 250, secondBetAmount);
            }
            totalBalance = initiator;

            //        WinnerCoinCounter.text = totalBalance.ToString();
            yield return new WaitForSeconds(waitSeconds_Counter);
        }

    }
}
