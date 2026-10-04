using TeenPattiGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TeenPattiGame
{
    public class TeenPattiLogicTesting : MonoBehaviour
    {
        public CardProperty card1;
        public CardProperty card2;
        public CardProperty card3;

        public CardProperty SupportingCard;

        public CardProperty replaceCard;
        public void CalculateResults()
        {
            PlayerCardsRankAndScoreCalc.CalculateRankAndScores(card1, card2, card3);

            HukmGameLogic.GetAlternateCard(card1, card2, card3, SupportingCard);
            replaceCard = HukmGameLogic.cardToReplace;
            //Debug.LogError("Power is " + HukmGameLogic.cardToReplace.Power + " Suit is " + HukmGameLogic.cardToReplace.Suit + ")");

            Debug.Log("Your Rank is " + PlayerCardsRankAndScoreCalc.Rank + " And Score is " + PlayerCardsRankAndScoreCalc.Scores);

            if (replaceCard == card1)
                card1 = SupportingCard;

            if (replaceCard == card2)
                card2 = SupportingCard;

            if (replaceCard == card3)
                card3 = SupportingCard;


            PlayerCardsRankAndScoreCalc.CalculateRankAndScores(card1, card2, card3);

            Debug.Log("Your New Rank is " + PlayerCardsRankAndScoreCalc.Rank + " And Score is " + PlayerCardsRankAndScoreCalc.Scores);

        }


    }
}