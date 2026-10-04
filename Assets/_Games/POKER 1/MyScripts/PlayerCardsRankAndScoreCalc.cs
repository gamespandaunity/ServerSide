//
using System;

namespace POKER
{
    public static class PlayerCardsRankAndScoreCalc
    {
        public static int Rank;
        public static int Scores;
        public static int[] CardValuesArrayForPlayer = { 0, 0, 0 };

        // Getting Rank and Score w.r.t cards
        public static void CalculateRankAndScores(CardProperty card0, CardProperty card1, CardProperty card2)
        {
            if (IsTrailSatisfied(card0, card1, card2))
            {
                Rank = 7;
                Scores = card0.Power + card1.Power + card2.Power;
            }
            else if (IsPureSequenceSatisfied(card0, card1, card2))
            {
                Rank = 6;
                Scores = card0.Power + card1.Power + card2.Power;
            }
            else if (isSequenceSatisfied(card0, card1, card2))
            {
                Rank = 5;
                Scores = card0.Power + card1.Power + card2.Power;
            }
            else if (isSameColorSatisfied(card0, card1, card2))
            {
                Rank = 4;
                Scores = card0.Power + card1.Power + card2.Power;
            }
            else if (isPairSatisfied(card0, card1, card2))
            {
                Rank = 3;
                //if (NetworkSettings.Instance.currentRoomFilter != NetworkSettings.RoomFilters.MUFFLIS)
                //{
                    if (card0.Card == card1.Card)
                        Scores = card0.Power + card1.Power;
                    else if (card1.Card == card2.Card)
                        Scores = card1.Power + card2.Power;                    
                    else if (card0.Card == card2.Card)
                        Scores = card0.Power + card2.Power;
                //}
            }
            else
            {
                Rank = 2;
                Scores = card0.Power + card1.Power + card2.Power;
            }
        }


        // Ranking calculatin
        #region Checking teenpatti Card ranking and values
        static bool IsTrailSatisfied(CardProperty card0, CardProperty card1, CardProperty card2)
        {
            if (card0.Card == card1.Card && card1.Card == card2.Card)
                return true;

            else
                return false;
        }
        static bool IsPureSequenceSatisfied(CardProperty card0, CardProperty card1, CardProperty card2)
        {

            int[] aryOfEnums = CardsArray(card0, card1, card2);
            if (card0.Suit == card1.Suit && card1.Suit == card2.Suit &&
            aryOfEnums[0] == aryOfEnums[1] + 1 && aryOfEnums[1] == aryOfEnums[2] + 1)
                return true;

            else if (card0.Suit == card1.Suit && card1.Suit == card2.Suit &&
                aryOfEnums[0] == 14 && aryOfEnums[1] == 3 && aryOfEnums[2] == 2)
                return true;

            else
                return false;
        }

        static bool isSequenceSatisfied(CardProperty card0, CardProperty card1, CardProperty card2)
        {

            int[] aryOfEnums = CardsArray(card0, card1, card2);
            if (aryOfEnums[0] == aryOfEnums[1] + 1 && aryOfEnums[1] == aryOfEnums[2] + 1)
                return true;

            else if (aryOfEnums[0] == 14 && aryOfEnums[1] == 3 && aryOfEnums[2] == 2)
                return true;

            else
                return false;
        }

        static bool isSameColorSatisfied(CardProperty card0, CardProperty card1, CardProperty card2)
        {
            if (card0.Suit == card1.Suit && card1.Suit == card2.Suit)
                return true;
            else
                return false;
        }

        static bool isPairSatisfied(CardProperty card0, CardProperty card1, CardProperty card2)
        {
            if (card0.Card == card1.Card || card1.Card == card2.Card || card0.Card == card2.Card)
                return true;

            else
                return false;
        }

        #endregion

        // Cards sorting ascending
        #region Sorting the array
        static int[] CardsArray(CardProperty card0, CardProperty card1, CardProperty card2)
        {
            int[] tmpAry = { 0, 0, 0 };
            tmpAry[0] = (int)card0.Card;
            tmpAry[1] = (int)card1.Card;
            tmpAry[2] = (int)card2.Card;
            Array.Sort(tmpAry);
            Array.Reverse(tmpAry);
            CardValuesArrayForPlayer = tmpAry;
            return tmpAry;
        }
        #endregion
    }
}