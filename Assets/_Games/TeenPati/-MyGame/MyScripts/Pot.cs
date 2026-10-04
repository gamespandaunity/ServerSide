using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeenPattiGame
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
        public BigInteger ChaalLimit;

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



            AndarTotalBetPlaced = 0;
            BaharTotalBetPlaced = 0;

            targeToTipGirl = TipToGirl[0];
            tipFromPlayerNameText = targeToTipGirl.transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>();
            // TableInfoActive(0);
            ShowBootText.text = "Boot: " + LocalSettings.Rs(startPotAmount);
            //Invoke(nameof(TableInfoPanel), 1f);





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



        public void SetPotLimit()
        {
            CurrentChalAmount = startPotAmount;
            //Debug.LogError(CurrentChalAmount + " 2");
            PotLimit = startPotAmount * LocalSettings.PotLimitMultiplier;
            // Debug.Log("PotLimit:   " + PotLimit);
            ChaalLimit = startPotAmount * LocalSettings.ChaalLimitMultiplier;
            // ChaalLimit = 1000;
            //Debug.LogError("Chal Limit:     " + ChaalLimit);
        }

        public void TableInfoPanel()
        {
            //InfoTablePanel.SetActive(MatchHandler.IsTeenPatti() ? true : false);
            //helpPanel.SetActive(MatchHandler.IsAndarBahar() ? true : false);
            //if (MatchHandler.isWingoLottary())
            //    helpPanel.SetActive(MatchHandler.isWingoLottary() ? true : false);
            if (MatchHandler.IsTeenPatti())
            {
                TableInfoAboutRules(MatchHandler.CurrentMatch, LocalSettings.Rs(startPotAmount), UIManager.Instance.TotalChals.ToString(), LocalSettings.Rs(ChaalLimit), LocalSettings.Rs(PotLimit));
            }

            InfoTablePanel.SetActive(true);

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
            // if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.CurrentRoom != null)
            //     LocalSettings.GetCurrentRoom.SetTableCollectedCash(LocalSettings.TableCashKey, potSize);         //photon removal
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





    }
}