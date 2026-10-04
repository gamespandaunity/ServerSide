using System.Numerics;
using TMPro;
using UnityEngine;

namespace TeenPattiGame
{
    public class PointBetAmount : MonoBehaviour
    {
        [ShowOnly] public BigInteger MineBetAmount;
        [ShowOnly] public BigInteger TotalBetAmountOnPoint;
        [ShowOnly]
        public TMP_Text BetAmountTxt;
        // Start is called before the first frame update
        void Start()
        {
            BetAmountTxt = transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
            ResetBetAmounts();
        }

        public void SetBetAmount(BigInteger betAmount, bool isMine)
        {
            TotalBetAmountOnPoint += betAmount;
            if (isMine)
                MineBetAmount += betAmount;
            UpdateText();
        }

        public void ResetBetAmounts()
        {
            MineBetAmount = 0;
            TotalBetAmountOnPoint = 0;
            UpdateText();
        }

        void UpdateText()
        {
            BetAmountTxt.text = LocalSettings.Rs(MineBetAmount.ToString()) + "/" + LocalSettings.Rs(TotalBetAmountOnPoint.ToString());
        }
    }
}