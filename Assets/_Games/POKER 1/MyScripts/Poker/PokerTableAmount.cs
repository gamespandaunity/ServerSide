using POKER;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace POKER
{
    public class PokerTableAmount : MonoBehaviour
    {
        public Slider BuyInAmountSlider;
        public TMP_Text SlideBuyInAmountTxt;
        public TMP_Text BuyInMinAmountTxt;
        public TMP_Text BuyInMaxAmountTxt;
        public TMP_Text BlindAmountTxt;
        public TMP_Text PlayerTotalAmountTxt;
        public BigInteger OffSet;
        public BigInteger CurrentBuyInAmount;


        private static PokerTableAmount _instance;

        public static PokerTableAmount Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<PokerTableAmount>();
                return _instance;
            }
        }


        private void Awake()
        {


            if (_instance == null)
                _instance = this;
        }
        private void Start()
        {

            if (LocalSettings.GetTotalChips() >= LocalSettings.GetStartingMinAmountPoker())
                AssignStartSliderValues();
            //PokerAddAmount();


        }

        void PokerAddAmount()
        {
            if (LocalSettings.GetPokerBuyInChips() == 0)
            {
                BigInteger amount = LocalSettings.GetStartingMinAmountPoker();
                Debug.LogError("poker entry amount......" + amount);

                LocalSettings.SetPokerBuyInChips(amount);
                PokerManager.Instance.BuyInCashPanel.SetActive(false);
                OnJoinNowBtnClick();
            }
        }
        public void AssignStartSliderValues()
        {
            BuyInMinAmountTxt.text = /*"Min Buy-in \n" + */ LocalSettings.Rs(LocalSettings.GetStartingMinAmountPoker());
            BuyInMaxAmountTxt.text = /*"Max Buy-in \n" +*/ LocalSettings.Rs(LocalSettings.GetStartingMaxAmountPoker());
            OffSet = LocalSettings.GetStartingMinAmountPoker();
            BlindAmountTxt.text = "Blinds: " + LocalSettings.Rs(LocalSettings.GetBlindAmountPoker() / 2) + "/" + LocalSettings.Rs(LocalSettings.GetBlindAmountPoker());
            OnSliderValueChange();
            PlayerTotalAmountTxt.text =/*"Gold: " +*/ LocalSettings.Rs(LocalSettings.GetTotalChips());
            BuyInAmountSlider.minValue = 0;
            LocalSettings.GetBlindAmountPoker().Show("Blind Amount Poker");
            BigInteger divider = (LocalSettings.GetBlindAmountPoker() / 2) * 10;
            BuyInAmountSlider.maxValue = (int)((LocalSettings.GetStartingMaxAmountPoker() - OffSet) / divider);
        }

        public void OnSliderValueChange()
        {
            //double CurrentSliderVal = BuyInAmountSlider.value;
            //BigInteger finalSliderVal = LocalSettings.GetStartingMaxAmountPoker() - OffSet;
            //BigInteger currentAmount = (BigInteger)(CurrentSliderVal * (double)finalSliderVal);
            //CurrentBuyInAmount = currentAmount + OffSet;

            int increment = (Mathf.RoundToInt(BuyInAmountSlider.value));
            BigInteger multiplayer = (LocalSettings.GetBlindAmountPoker() / 2) * 10;
            CurrentBuyInAmount = (increment * multiplayer) + OffSet;
            Debug.Log(" Checek Current Amount" + CurrentBuyInAmount + "....." + LocalSettings.Rs((CurrentBuyInAmount)) + "  Increment.." + " Muliplyaer.." + multiplayer + "  Offset.." + OffSet);
            SlideBuyInAmountTxt.text = LocalSettings.Rs((CurrentBuyInAmount));
            WhenSliderAmountExceedsTotalAmount();
        }

        void WhenSliderAmountExceedsTotalAmount()
        {
            if (CurrentBuyInAmount > LocalSettings.GetTotalChips())
            {
                BuyInAmountSlider.value -= 1f;
                OnSliderValueChange();
            }
        }
        public void SetSliderAmount(string sign)
        {
            BuyInAmountSlider.value = sign == "+" ? BuyInAmountSlider.value + 1 : BuyInAmountSlider.value - 1;
        }

        public void OnJoinNowBtnClick()
        {
              Debug.LogError("curre buy in amount " + LocalSettings.Rs(CurrentBuyInAmount) + " Total AMount" + LocalSettings.Rs(LocalSettings.GetTotalChips()));

            //if (LocalSettings.GetTotalChips() >= CurrentBuyInAmount)
            if (LocalSettings.GetTotalChips() >= 500)
            {
                // Return any leftover buyin from a previous game back to total chips.
                BigInteger AlreadyBuyInChips = LocalSettings.GetPokerBuyInChips();
                if (AlreadyBuyInChips > 0)
                {
                    LocalSettings.SetTotalChips(AlreadyBuyInChips);
                    LocalSettings.SetPokerBuyInChips(-AlreadyBuyInChips);
                }

                // Move the selected buy-in amount from total chips to poker table chips.
                LocalSettings.SetPokerBuyInChips(CurrentBuyInAmount);
                LocalSettings.SetTotalChips(-CurrentBuyInAmount);


                GameManager.Instance.StartingThings();

                PokerManager.Instance.SetStartingAmount();
                PokerActionPanel.Instance.StartForSelectAmount();
            }
            else
            {
                UIManager.Instance.InfoTxt.text = "Not Enough Cash";
                UIManager.Instance.InfoObj.SetActive(true);
                UIManager.Instance.quickShop.SetActive(true);
                ConstantsData_M.Log(PlayerPrefs.GetString("TotalChips") + "7");
            }
        }


    }
}
