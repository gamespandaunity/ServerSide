//
using System;

namespace POKER
{
    public static class PokerScoreCalculation
    {
        public static int Rank;
        public static int Scores;
        public static int[] CardValuesArrayForPlayer = { 0, 0, 0, 0, 0 };

        // Getting Rank and Score w.r.t cards
        public static void CalculateRankAndScores(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            if (IsRoyalFlush(card1, card2, card3, card4, card5))
            {
                Rank = 10;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else if (IsStraightFlush(card1, card2, card3, card4, card5))
            {
                Rank = 9;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else if (IsFourOfAKind(card1, card2, card3, card4, card5))
            {
                Rank = 8;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else if (IsFullHouse(card1, card2, card3, card4, card5))
            {
                Rank = 7;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else if (isFlush(card1, card2, card3, card4, card5))
            {
                Rank = 6;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else if (IsStraight(card1, card2, card3, card4, card5))
            {
                Rank = 5;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else if (IsThreeOfAKind(card1, card2, card3, card4, card5))
            {
                Rank = 4;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else if (IsTwoPair(card1, card2, card3, card4, card5))
            {
                Rank = 3;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else if (IsOnePair(card1, card2, card3, card4, card5))
            {
                Rank = 2;
                //Scores = card1.Power + card2.Power + card3.Power;
            }
            else //if (IsHighCard(card1, card2, card3, card4, card5))
            {
                Rank = 1;
                //Scores = card1.Power + card2.Power + card3.Power;
            }

        }


        public static void CalculateRankAndScores(CardProperty card1, CardProperty card2)
        {
            if (card1.Power == card2.Power)
            {
                Rank = 2;
            }
            else
            {
                Rank = 1;
            }
        }

        static bool isSameSuit(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            return card1.Suit == card2.Suit && card2.Suit == card3.Suit && card3.Suit == card4.Suit && card4.Suit == card5.Suit;
        }

        // Ranking calculatin
        #region Checking Poker Community Card ranking and values
        static bool IsRoyalFlush(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] aryOfEnums = CardsArray(card1, card2, card3, card4, card5);

            if (isSameSuit(card1, card2, card3, card4, card5))
            {
                if (aryOfEnums[0] == aryOfEnums[1] + 1 && aryOfEnums[1] == aryOfEnums[2] + 1 && aryOfEnums[2] == aryOfEnums[3] + 1 && aryOfEnums[3] == aryOfEnums[4] + 1 && aryOfEnums[0] == 14)
                {
                    Scores = aryOfEnums[0] + aryOfEnums[1] + aryOfEnums[2] + aryOfEnums[3] + aryOfEnums[4];
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        static bool IsStraightFlush(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] aryOfEnums = CardsArray(card1, card2, card3, card4, card5);

            if (isSameSuit(card1, card2, card3, card4, card5))
            {
                if (aryOfEnums[0] == aryOfEnums[1] + 1 && aryOfEnums[1] == aryOfEnums[2] + 1 && aryOfEnums[2] == aryOfEnums[3] + 1 && aryOfEnums[3] == aryOfEnums[4] + 1)
                {
                    Scores = aryOfEnums[0] + aryOfEnums[1] + aryOfEnums[2] + aryOfEnums[3] + aryOfEnums[4];
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        static bool IsFourOfAKind(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] aryOfEnums = CardsArray(card1, card2, card3, card4, card5);

            if (aryOfEnums[0] == aryOfEnums[1] && aryOfEnums[1] == aryOfEnums[2] && aryOfEnums[2] == aryOfEnums[3])
            {
                Scores = aryOfEnums[0] + aryOfEnums[1] + aryOfEnums[2] + aryOfEnums[3];
                return true;
            }
            else if (aryOfEnums[1] == aryOfEnums[2] && aryOfEnums[2] == aryOfEnums[3] && aryOfEnums[3] == aryOfEnums[4])
            {

                Scores = aryOfEnums[1] + aryOfEnums[2] + aryOfEnums[3] + aryOfEnums[4];
                return true;
            }
            else
                return false;
        }


        static bool IsFullHouse(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] aryOfEnums = CardsArray(card1, card2, card3, card4, card5);

            if (aryOfEnums[0] == aryOfEnums[1] && aryOfEnums[1] == aryOfEnums[2] && aryOfEnums[3] == aryOfEnums[4])
            {
                Scores = aryOfEnums[0] + aryOfEnums[1] + aryOfEnums[2];
                return true;
            }
            else if (aryOfEnums[0] == aryOfEnums[1] && aryOfEnums[2] == aryOfEnums[3] && aryOfEnums[3] == aryOfEnums[4])
            {
                Scores = aryOfEnums[2] + aryOfEnums[3] + aryOfEnums[4];
                return true;
            }
            else
                return false;
        }

        static bool isFlush(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            Scores = card1.Power + card2.Power + card3.Power + card4.Power + card5.Power;
            return card1.Suit == card2.Suit && card2.Suit == card3.Suit && card3.Suit == card4.Suit && card4.Suit == card5.Suit;
        }

        static bool IsStraight(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] aryOfEnums = CardsArray(card1, card2, card3, card4, card5);

            if (aryOfEnums[0] == aryOfEnums[1] + 1 && aryOfEnums[1] == aryOfEnums[2] + 1 && aryOfEnums[2] == aryOfEnums[3] + 1 && aryOfEnums[3] == aryOfEnums[4] + 1)
            {
                Scores = aryOfEnums[0] + aryOfEnums[1] + aryOfEnums[2] + aryOfEnums[3] + aryOfEnums[4];
                return true;
            }
            else
                return false;

        }

        static bool IsThreeOfAKind(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] aryOfEnums = CardsArray(card1, card2, card3, card4, card5);

            if (aryOfEnums[0] == aryOfEnums[1] && aryOfEnums[1] == aryOfEnums[2])
            {
                Scores = aryOfEnums[0] + aryOfEnums[1] + aryOfEnums[2];
                return true;
            }
            else if (aryOfEnums[1] == aryOfEnums[2] && aryOfEnums[2] == aryOfEnums[3])
            {
                Scores = aryOfEnums[1] + aryOfEnums[2] + aryOfEnums[3];
                return true;
            }
            else if (aryOfEnums[2] == aryOfEnums[3] && aryOfEnums[3] == aryOfEnums[4])
            {
                Scores = aryOfEnums[2] + aryOfEnums[3] + aryOfEnums[4];
                return true;
            }
            else
                return false;
        }

        static bool IsTwoPair(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] aryOfEnums = CardsArray(card1, card2, card3, card4, card5);

            if (aryOfEnums[0] == aryOfEnums[1] && aryOfEnums[2] == aryOfEnums[3])
            {
                Scores = aryOfEnums[0] + aryOfEnums[1];
                return true;
            }
            else if (aryOfEnums[1] == aryOfEnums[2] && aryOfEnums[3] == aryOfEnums[4])
            {
                Scores = aryOfEnums[1] + aryOfEnums[2];
                return true;
            }
            else if (aryOfEnums[0] == aryOfEnums[1] && aryOfEnums[3] == aryOfEnums[4])
            {
                Scores = aryOfEnums[0] + aryOfEnums[1];
                return true;
            }
            else
                return false;
        }

        static bool IsOnePair(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] aryOfEnums = CardsArray(card1, card2, card3, card4, card5);

            if (aryOfEnums[0] == aryOfEnums[1] || aryOfEnums[1] == aryOfEnums[2] || aryOfEnums[2] == aryOfEnums[3] || aryOfEnums[3] == aryOfEnums[4])
            {
                if (aryOfEnums[0] == aryOfEnums[1])
                    Scores = aryOfEnums[0] + aryOfEnums[1];
                if (aryOfEnums[1] == aryOfEnums[2])
                    Scores = aryOfEnums[1] + aryOfEnums[2];
                if (aryOfEnums[2] == aryOfEnums[3])
                    Scores = aryOfEnums[2] + aryOfEnums[3];
                if (aryOfEnums[3] == aryOfEnums[4])
                    Scores = aryOfEnums[3] + aryOfEnums[4];
                return true;
            }
            else
                return false;
        }

        static bool IsHighCard(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            return true;
        }

        #endregion

        // Cards sorting ascending
        #region Sorting the array
        static int[] CardsArray(CardProperty card1, CardProperty card2, CardProperty card3, CardProperty card4, CardProperty card5)
        {
            int[] tmpAry = { 0, 0, 0, 0, 0 };
            tmpAry[0] = (int)card1.Card;
            tmpAry[1] = (int)card2.Card;
            tmpAry[2] = (int)card3.Card;
            tmpAry[3] = (int)card4.Card;
            tmpAry[4] = (int)card5.Card;


            Array.Sort(tmpAry);
            Array.Reverse(tmpAry);
            CardValuesArrayForPlayer = tmpAry;
            return tmpAry;
        }
        #endregion
    }
}