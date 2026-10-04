namespace POKER 
{
using DG.Tweening;
using POKER;
using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;
public class PokerBetAmountAnim : MonoBehaviour
{
    public TMP_Text PokerBetAmountTxt;
    [ShowOnly] public Transform TargetPosition;
    [ShowOnly] BigInteger bettedAmount = 0;
    public void PlayAnimation(Transform targetPosition, bool isSelfDestroy, BigInteger BetAmount)
    {
        bettedAmount += BetAmount;
        //GameObject objectToActivate = ObjToActivate;
        //if (targetPosition.GetComponent<PokerBetAmountAnim>() != null)
        //{
        //    isSelfDestroy = true;
        //    BigInteger amount = targetPosition.GetComponent<PokerBetAmountAnim>().bettedAmount += BetAmount;
        //    targetPosition.GetComponent<PokerBetAmountAnim>().PokerBetAmountTxt.text = LocalSettings.Rs(amount);
        //}
        PokerBetAmountTxt.text = LocalSettings.Rs(bettedAmount);

        Transform ObjToAnimate = gameObject.transform;
        if (targetPosition != null)
            ObjToAnimate.DOMove(targetPosition.position, 0.5f, false).OnComplete(() => OnCompleteAnim(isSelfDestroy, targetPosition, BetAmount));

        //ObjToAnimate.DOScale(new UnityEngine.Vector3(1.5f, 1.5f, 1.5f), 0.5f);
    }

    void OnCompleteAnim(bool isSelfDestroy, Transform targetPosition, BigInteger BetAmount)
    {
        if (targetPosition.GetComponent<PokerBetAmountAnim>() != null)
        {
            isSelfDestroy = true;
            BigInteger amount = targetPosition.GetComponent<PokerBetAmountAnim>().bettedAmount += BetAmount;
            targetPosition.GetComponent<PokerBetAmountAnim>().PokerBetAmountTxt.text = LocalSettings.Rs(amount);
        }
        if (isSelfDestroy)
            Destroy(gameObject);
    }

}
}