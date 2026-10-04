//using Photon.Pun;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POKER
{
    public class Pot : NetworkBehaviour
    {
        public BigInteger potSize;
        public BigInteger startPotAmount; // Pot limit and chaal limit depends on this amount
        public BigInteger CurrentChalAmount;
        public int potTip;

        #region AndarBaharVars
        public BigInteger AndarTotalBetPlaced;
        public BigInteger BaharTotalBetPlaced;
        public BigInteger SuperAndarTotalBetPlaced;
        public BigInteger SuperBaharTotalBetPlaced;
        #endregion
        #region LuckyWarVars
        public BigInteger TieTotalBetPlaced;
        public BigInteger BetTotalBetPlaced;
        #endregion

        public BigInteger PotLimit;
        BigInteger ChaalLimit;

        public TMP_Text PotTxt;
        public RectTransform PotPosition;
        public GameObject PotPanel;
        public GameObject[] TipToGirl;
        [ShowOnly]
        public GameObject targeToTipGirl;
        [ShowOnly]
        public TMP_Text tipFromPlayerNameText;
        public static Pot instance;
        public GameObject winAmountAnim;
        public TMP_Text winAmountText;

        // For Info Table of 3 Patti
        public GameObject InfoTablePanel;
        public GameObject helpPanel;


        [Header("..... Teen_Patti_Text .....")]
        public TMP_Text modeTxt;
        public TMP_Text bootTxt;
        public TMP_Text maxBlindTxt;
        public TMP_Text chaalLimitTxt;
        public TMP_Text potLimitTxt;
        public TMP_Text ShowBootText;
        [Header("..... Andar_Bahar_Text .....")]
        public TMP_Text minimumBetTxt;
        public TMP_Text maximumBetTxt;
        public TMP_Text maximumPayoutTxt;
        [Header("..... Lucky_War_Text .....")]
        public TMP_Text minimumLWBetTxt;
        public TMP_Text maximumLWBetTxt;
        public TMP_Text maxTieBetText;
        public TMP_Text numberOfDecksTxt;
        [Header("..... Dragon_Tiger_Text .....")]
        public TMP_Text minimumBetTxtDT;
        public TMP_Text maximumBetTxtDT;
        public TMP_Text numberOfDeckDt;
        [Header("..... Poker_Text .....")]
        public TMP_Text BetAmountsPokerTxt;
        public TMP_Text BuyINCashPokerTxt;
        public TMP_Text EntryFeePokerTxt;



        public BigInteger minimumBet;
        public BigInteger maximumBet;
        public BigInteger maximumPayout;
        public BigInteger maximumTieBetAmount;



        public List<GameObject> infoTablePanel = new List<GameObject>();

        //Tip Text From Player
        [ShowOnly]
        public List<string> tipDialogueTextObject = new List<string>();

        private void Awake()
        {
            if (instance == null)
                instance = this;
            startPotAmount = LocalSettings.MinBetAmount;
            SetPotLimit();
        }

        private void Start()
        {



            LocalSettings.SetPosAndRect(PotPanel, PotPosition, PotPosition.transform.parent);
            AndarTotalBetPlaced = 0;
            BaharTotalBetPlaced = 0;
            
            
            
           
           
             if (MatchHandler.IsPoker())
            {
                targeToTipGirl = TipToGirl[3];
                tipFromPlayerNameText = targeToTipGirl.transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>();
                SetPokerTableInfoTexts(BetAmountsPokerTxt, BuyINCashPokerTxt, EntryFeePokerTxt);
                TableInfoActive(5);
            }
            UIManager.Instance.UpDateCurrentChalAmountText(CurrentChalAmount);
        }

        void TableInfoActive(int number)
        {
            foreach (var item in infoTablePanel)
            {
                item.SetActive(false);
            }
            infoTablePanel[number].SetActive(true);
        }

        void SetAndarBaharLimit(TMP_Text MinumumBetText, TMP_Text maximumBetText, TMP_Text maximumPayOut)
        {

            minimumBet = LocalSettings.MinBetAmount;
            
                maximumBet = LocalSettings.MinBetAmount * (LocalSettings.TeenPattiLevelMultiplier / 10);
            maximumPayout = maximumBet * 2;
            MinumumBetText.text = LocalSettings.Rs(minimumBet);
            maximumBetText.text = LocalSettings.Rs(maximumBet);
            maximumPayOut.text = LocalSettings.Rs(maximumPayout);
           
           


        }

        void SetPokerTableInfoTexts(TMP_Text BetAmountsText, TMP_Text Buy_InText, TMP_Text EntryFeesText)
        {
            BetAmountsText.text = LocalSettings.Rs(LocalSettings.GetBlindAmountPoker() / 2) + "/" + LocalSettings.Rs(LocalSettings.GetBlindAmountPoker());
            Buy_InText.text = LocalSettings.Rs(LocalSettings.GetStartingMinAmountPoker()) + "-" + LocalSettings.Rs(LocalSettings.GetStartingMaxAmountPoker());
            EntryFeesText.text = LocalSettings.pokerEntryFeeString;
        }

        public void SetPotLimit()
        {
            CurrentChalAmount = startPotAmount;
            PotLimit = startPotAmount * LocalSettings.PotLimitMultiplier;
            // Debug.Log("PotLimit:   " + PotLimit);
            ChaalLimit = startPotAmount * LocalSettings.ChaalLimitMultiplier;
            //Debug.LogError("Chal Limit:     " + ChaalLimit);
        }

       

        public void TableInfoAboutRules(MatchHandler.MATCH CurrentMatchHandler, string boot, string maxBlind, string chaalLimit, string potLimit)
        {
            modeTxt.text = CurrentMatchHandler + " Mode";
            bootTxt.text = boot;
            maxBlindTxt.text = maxBlind.ToString();
            chaalLimitTxt.text = chaalLimit;
            potLimitTxt.text = potLimit;
            //  ShowBootText.text = "Boot: " + boot;
        }

        public void SetCashText(string cash)
        {
            PotTxt.text = LocalSettings.Rs(cash);
        }

        public void SetCashText(int cash)
        {
            PotTxt.text = LocalSettings.Rs(cash);
        }

        public void ResetPot()
        {
            potSize = 0;
            PotPanel.SetActive(false);
            SetCashText("");
            SetPotLimit();
            // Pot is calculated locally from bets — no network sync needed for table cash.
        }


        public void ResetPotAB()
        {
            potSize = 0;
            AndarTotalBetPlaced = 0;
            BaharTotalBetPlaced = 0;
            SuperAndarTotalBetPlaced = 0;
            SuperBaharTotalBetPlaced = 0;
            UIManager.Instance.TotalBetPlaceFor1Game = 0;
            UIManager.Instance.TotalWinAmountFor1Game = 0;
            UIManager.Instance.AndarBetAmountBtnTxt.text = "";
            UIManager.Instance.BaharBetAmountBtnTxt.text = "";
            PotPanel.SetActive(false);
            SetCashText("");
        }


        public BigInteger ChaalAmountLimit()
        {
            return ChaalLimit;
        }


        public void NextTipOfPlayer(float timeDelay, PlayerInfo playerInfo)
        {
            StartCoroutine(NextTipText(timeDelay, playerInfo));
        }
        int textIndex;
        IEnumerator NextTipText(float timeDelay, PlayerInfo player)
        {

            // Debug.LogError("Tip Dialogue Number is" + timeDelay);
            yield return new WaitForSeconds(timeDelay);
            if (tipDialogueTextObject.Count > 1 && textIndex < tipDialogueTextObject.Count - 1)
            {
                textIndex++;
                tipFromPlayerNameText.text = tipDialogueTextObject[textIndex];

                StartCoroutine(NextTipText(timeDelay, player));
            }
            else
            {
                tipDialogueTextObject.Clear();
                textIndex = 0;
                Pot.instance.tipFromPlayerNameText.transform.parent.gameObject.SetActive(false);
            }


        }

        #region AndarBaharFuntions
        public void AddAndarAmount(BigInteger amount)
        {
            AndarTotalBetPlaced += amount;
        }

        public void AddBaharAmount(BigInteger amount)
        {
            BaharTotalBetPlaced += amount;
        }

        public void AddSuperAndarAmount(BigInteger amount)
        {
            SuperAndarTotalBetPlaced += amount;
        }

        public void AddSuperBaharAmount(int amount)
        {
            SuperBaharTotalBetPlaced += amount;
        }
        #endregion

        #region LuckyWarFunction
        public void AddTieAmount(BigInteger amount)
        {
            TieTotalBetPlaced += amount;
        }

        public void AddBetAmount(BigInteger amount)
        {
            BetTotalBetPlaced += amount;
        }


        #endregion

    }
}