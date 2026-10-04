using System;
using UnityEngine;

namespace TeenPattiGame
{
    public class HukmGameLogic : MonoBehaviour
    {
        public static int CardsRank;

        public static CardProperty cardToReplace;

        public static void GetAlternateCard(CardProperty Card0, CardProperty Card1, CardProperty Card2, CardProperty SupportingCard)
        {
            cardToReplace = null;
            PlayerCardsRankAndScoreCalc.CalculateRankAndScores(Card0, Card1, Card2);
            CardsRank = PlayerCardsRankAndScoreCalc.Rank;
            CardProperty[] SortedCards = CardsArray(Card0, Card1, Card2);
            //Debug.Log("Sorted Cards" + ", " + SortedCards[0].Power + ", " + SortedCards[1].Power + ", " + SortedCards[2].Power);
            int[] tempArr = { SortedCards[0].Power, SortedCards[1].Power, SortedCards[2].Power };
            //Debug.Log("Temp Cards" + ", " + tempArr[0] + ", " + tempArr[1] + ", " + tempArr[2]);

            Debug.Log("Rank is " + CardsRank);

            // If Trail satisfied
            if (CardsRank == 7)
            {
                return;
            }
            // If Pure Sequence satisfied
            else if (CardsRank == 6)
            {
                if (SupportingCard.Suit != SortedCards[0].Suit) //It can be any card to check from because here we have all cards have same suit.
                    return;


                int tempCard = tempArr[0] + 1;

                if (tempCard == SupportingCard.Power)
                {
                    ///Replce The Card
                    ///Remove Card index 2 and add New Supporting Card
                    cardToReplace = SortedCards[2];
                    return;
                }
            }
            // If Sequence Satisfied
            else if (CardsRank == 5)
            {
                // Sequence To Pure Sequence Logic wrt Suit Check
                // 
                if (SortedCards[0].Suit == SortedCards[1].Suit)
                {
                    if (SortedCards[0].Suit == SupportingCard.Suit && SortedCards[2].Power == SupportingCard.Power)
                    {
                        //Replace Card 2 
                        cardToReplace = SortedCards[2];
                        return;
                    }
                }
                else if (SortedCards[0].Suit == SortedCards[2].Suit)
                {
                    if (SortedCards[2].Suit == SupportingCard.Suit && SortedCards[1].Power == SupportingCard.Power)
                    {
                        //Replace Card 1
                        cardToReplace = SortedCards[1];
                        return;
                    }
                }
                else if (SortedCards[1].Suit == SortedCards[2].Suit)
                {
                    if (SortedCards[1].Suit == SupportingCard.Suit && SortedCards[0].Power == SupportingCard.Power)
                    {
                        //Replace Card 0
                        cardToReplace = SortedCards[0];
                        return;
                    }
                }

                // Sequence To High Sequence Logic

                int tempCard = tempArr[0] + 1;

                if (tempCard == SupportingCard.Power)
                {
                    ///Replace The Card
                    ///Remove Card index 2 and add New Supporting Card
                    cardToReplace = SortedCards[2];
                    return;
                }

            }
            // If Suit(Rang) satisfied
            else if (CardsRank == 4)
            {
                //   Making Suit Logic From Suit to Pure Sequence and sequence

                // Eg. 9,8, Any --- Suportin card-- 7
                if (tempArr[0] == tempArr[1] + 1 && SupportingCard.Power + 1 == tempArr[1])
                {
                    // replace the card on index 2
                    cardToReplace = SortedCards[2];
                    return;
                }
                // Eg. Any, 7,6 ---- supporting Card --8
                else if (tempArr[1] == tempArr[2] + 1 && SupportingCard.Power == tempArr[1] + 1)
                {
                    // Replace the card on index 0
                    cardToReplace = SortedCards[0];
                    return;
                }
                // Eg. Greater than 8, 7,6 ---- supporting Card --5
                // By Amjad
                else if (tempArr[1] == tempArr[2] + 1 && SupportingCard.Power + 1 == tempArr[2])
                {
                    // Replace the card on index 0
                    cardToReplace = SortedCards[0];
                    return;
                }


                //Handling ace with cards 2 and 3  Eg. Any,3,2--- Ace
                else if (tempArr[1] == tempArr[2] + 1 && SupportingCard.Power == 14)
                {
                    // Replace the card at 0 index
                    cardToReplace = SortedCards[0];

                    return;
                }
                // A, Any, 2 ---- 3
                else if (tempArr[0] == 14 && tempArr[2] == 2 && SupportingCard.Power == 3)
                {
                    // Replace card 1
                    cardToReplace = SortedCards[1];
                    return;
                }
                else if (tempArr[0] == 14 && tempArr[2] == 3 && SupportingCard.Power == 2)
                {
                    // Replace card 1
                    cardToReplace = SortedCards[1];
                    return;
                }



                //   Making Suit Logic From Suit to Sequence
            }
            // If Pair satisfied
            else if (CardsRank == 3)
            {
                // Making logic from pair to trail
                // Eg 4,4,any --- 4
                if (tempArr[0] == tempArr[1] && SupportingCard.Power == tempArr[0])
                {
                    // Card 2 will be replaced with supporting card
                    cardToReplace = SortedCards[2];
                    return;
                }
                // Eg. Any, 5,5 ---- 5
                else if (tempArr[1] == tempArr[2] && SupportingCard.Power == tempArr[1])
                {
                    // Card 0 will be replaced with supporting card
                    cardToReplace = SortedCards[0];
                    return;
                }

                // -----------------------------------------------------------------------------------------------------------
                // making logic from pair to sequence
                #region Pair To sequence
                // 10,10,9 --- 8
                if (tempArr[0] == tempArr[1] && tempArr[1] == tempArr[2] + 1 && SupportingCard.Power + 1 == tempArr[2])
                {

                    if (SortedCards[0].Suit != SupportingCard.Suit)
                    {
                        // replace with card 0
                        cardToReplace = SortedCards[0];
                    }
                    else
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    return;
                }
                // 10,9,9 --- 8
                else if (tempArr[0] == tempArr[1] + 1 && tempArr[1] == tempArr[2] && SupportingCard.Power + 1 == tempArr[2])
                {
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 2
                        cardToReplace = SortedCards[2];
                    }
                    return;
                }
                // First 2 same and 3rd is diff of 1 ---- Eg. 7,7,6 --- supporting card 8
                else if (tempArr[0] == tempArr[1] && tempArr[1] == tempArr[2] + 1 && SupportingCard.Power == tempArr[0] + 1)
                {
                    // Replace card of zero index
                    if (SortedCards[0].Suit != SupportingCard.Suit)
                    {
                        // replace with card 0
                        cardToReplace = SortedCards[0];
                    }
                    else
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    return;
                }
                // sencond 2 same and 1st card is diff of 1 --  Eg. 8,7,7 --- Supporitng card 6
                else if (tempArr[1] == tempArr[2] && tempArr[1] + 1 == tempArr[0] && SupportingCard.Power == tempArr[1] + 1)
                {
                    // replace card of 2 index
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 2
                        cardToReplace = SortedCards[2];
                    }
                    return;
                }
                // first 2 same and third card is diff of 2 and can make seq --- eg. 7,7,5 --- 6
                else if (tempArr[0] == tempArr[1] && tempArr[1] == tempArr[2] + 2 && SupportingCard.Power == tempArr[1] - 1)
                {
                    // Replace Middle Card or first card
                    if (SortedCards[0].Suit != SupportingCard.Suit)
                    {
                        // replace with card 0
                        cardToReplace = SortedCards[0];
                    }
                    else
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    return;
                }
                // second 2 same and first with diff of 2 ------ Eg. 9,7,7 ---- 8
                else if (tempArr[1] == tempArr[2] && tempArr[0] == tempArr[1] + 2 && SupportingCard.Power == tempArr[1] + 1)
                {
                    //  Replace middle card or third card
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 2
                        cardToReplace = SortedCards[2];
                    }
                    return;
                }
                // A,A,2 --- 3
                else if (tempArr[0] == 14 && tempArr[1] == 14 && tempArr[2] == 2 && SupportingCard.Power == 3)
                {
                    //  Replace 1st card
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 0
                        cardToReplace = SortedCards[0];
                    }
                    return;
                }
                // A,A,3 --- 2
                else if (tempArr[0] == 14 && tempArr[1] == 14 && tempArr[2] == 3 && SupportingCard.Power == 2)
                {
                    // replace 1 st card
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 0
                        cardToReplace = SortedCards[0];
                    }
                    return;
                }
                // A,2,2 ---- 3
                else if (tempArr[0] == 14 && tempArr[1] == 2 && tempArr[2] == 2 && SupportingCard.Power == 3)
                {
                    // replace 3rd 
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 2
                        cardToReplace = SortedCards[2];
                    }
                    return;
                }
                // A,3,3 ---- 2
                else if (tempArr[0] == 14 && tempArr[1] == 3 && tempArr[2] == 3 && SupportingCard.Power == 2)
                {
                    // replace 3rd
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 2
                        cardToReplace = SortedCards[2];
                    }
                    return;
                }
                // 3,2,2 ---- A
                else if (tempArr[0] == 3 && tempArr[1] == 2 && tempArr[2] == 2 && SupportingCard.Power == 14)
                {
                    // replace 3rd
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 2
                        cardToReplace = SortedCards[2];
                    }
                    return;
                }
                // 3,3,2 ---- A
                else if (tempArr[0] == 3 && tempArr[1] == 3 && tempArr[2] == 2 && SupportingCard.Power == 14)
                {
                    // replace 3rd
                    if (SortedCards[1].Suit != SupportingCard.Suit)
                    {
                        // replace with card 1
                        cardToReplace = SortedCards[1];
                    }
                    else
                    {
                        // replace with card 0
                        cardToReplace = SortedCards[0];
                    }
                    return;
                }
                #endregion
                //-----------------------------------------------------------------------------------------------------------------
                // pair to  Suit 
                if (SortedCards[0].Suit == SortedCards[1].Suit && SupportingCard.Suit == SortedCards[0].Suit)
                {
                    // Replace card 2 
                    cardToReplace = SortedCards[2];
                    return;
                }
                else if (SortedCards[0].Suit == SortedCards[2].Suit && SupportingCard.Suit == SortedCards[0].Suit)
                {
                    // Replace card 1
                    cardToReplace = SortedCards[1];
                    return;
                }
                else if (SortedCards[1].Suit == SortedCards[2].Suit && SupportingCard.Suit == SortedCards[1].Suit)
                {
                    // Replace card 0
                    cardToReplace = SortedCards[0];
                    return;
                }
                // _________________________________________
                // pair to high pair
                if (tempArr[1] == tempArr[2] && SupportingCard.Power == tempArr[0])
                {
                    // Replace 2nd or third card
                    cardToReplace = SortedCards[2];
                    Debug.LogError("Checking Pair TO High Pair");
                    return;
                }
                else if (tempArr[1] == tempArr[2] && SupportingCard.Power > tempArr[0])
                {
                    // Replace 2nd or third card
                    cardToReplace = SortedCards[0];
                    // Debug.LogError("Checking Pair TO High Pair");
                    return;
                }
                else if (tempArr[0] == tempArr[1] && SupportingCard.Power > tempArr[2])
                {
                    cardToReplace = SortedCards[2];
                    // Debug.LogError("Checking Pair TO High Pair");
                }
            }
            // If High Card satisfied
            else if (CardsRank == 2)
            {
                // If high card to sequence and Pure sequence
                #region high card to sequence and Pure sequence
                // 7,6,Anmylow ---- 8 or 5
                if (tempArr[0] == tempArr[1] + 1 &&
                    (SupportingCard.Power == tempArr[0] + 1 || SupportingCard.Power == tempArr[1] - 1))
                {
                    // Replace card 2
                    cardToReplace = SortedCards[2];
                    return;
                }
                // AnyHigh, 7,6 ----- 8 or 5
                else if (tempArr[1] == tempArr[2] + 1 &&
                    (SupportingCard.Power == tempArr[1] + 1 || SupportingCard.Power == tempArr[2] - 1))
                {
                    // Replace Card 0
                    cardToReplace = SortedCards[0];
                    return;
                }
                // 7,5,3 ----- 6  (With diff of 2)
                else if (tempArr[0] == tempArr[1] + 2 && SupportingCard.Power == tempArr[1] + 1)
                {
                    // Replace card 2
                    cardToReplace = SortedCards[2];
                    return;
                }
                // 7,5,3 -----  4 (With diff of 2)
                else if (tempArr[1] == tempArr[2] + 2 && SupportingCard.Power == tempArr[2] + 1)
                {
                    // Replace card 0
                    cardToReplace = SortedCards[0];
                    return;
                }
                // A, any, 2 ------ 3
                else if (tempArr[0] == 14 && tempArr[2] == 2 && SupportingCard.Power == 3)
                {
                    // Replace card 1
                    cardToReplace = SortedCards[1];
                    return;
                }
                // A, Any, 3 ----- 2
                else if (tempArr[0] == 14 && tempArr[2] == 3 && SupportingCard.Power == 2)
                {
                    // Replace card 1
                    cardToReplace = SortedCards[1];
                    return;
                }
                #endregion
                // High card to suit
                #region High card to suit
                if (SortedCards[0].Suit == SortedCards[1].Suit && SupportingCard.Suit == SortedCards[0].Suit)
                {
                    // Replace card 2 
                    cardToReplace = SortedCards[2];
                    return;
                }
                else if (SortedCards[0].Suit == SortedCards[2].Suit && SupportingCard.Suit == SortedCards[0].Suit)
                {
                    // Replace card 1
                    cardToReplace = SortedCards[1];
                    return;
                }
                else if (SortedCards[1].Suit == SortedCards[2].Suit && SupportingCard.Suit == SortedCards[1].Suit)
                {
                    // Replace card 0
                    cardToReplace = SortedCards[0];
                    return;
                }
                #endregion
                // High card to pair
                #region High card to pair

                // 10, 6, 4 ------ 10
                if (SupportingCard.Power == tempArr[0] || SupportingCard.Power == tempArr[1])
                {
                    // Replace with card 2
                    cardToReplace = SortedCards[2];
                    return;
                }
                else if (SupportingCard.Power == tempArr[2])
                {
                    // Replace with card 1
                    cardToReplace = SortedCards[1];
                    return;
                }

                #endregion
            }
        }
        public static CardProperty[] CardsArray(CardProperty card0, CardProperty card1, CardProperty card2)
        {
            CardProperty[] cards = { card0, card1, card2 };

            int[] tmpAry = { 0, 0, 0 };
            tmpAry[0] = (int)cards[0].Card;
            tmpAry[1] = (int)cards[1].Card;
            tmpAry[2] = (int)cards[2].Card;
            Array.Sort(tmpAry);
            Array.Reverse(tmpAry);

            CardProperty[] SortedCards = new CardProperty[3];
            for (int i = 0; i < SortedCards.Length; i++)
            {
                for (int j = 0; j < tmpAry.Length; j++)
                {
                    if (cards[i].Power == tmpAry[j])
                    {
                        SortedCards[j] = cards[i];
                    }
                }
            }

            return SortedCards;
        }
    }
}