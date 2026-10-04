//
using NaughtyAttributes;
using System;
using System.Linq;
using UnityEngine;

namespace POKER
{
    [CreateAssetMenu(fileName = "CardsSprites", menuName = "ScriptableObjects/CardsContainer", order = 1)]
    public class CardsContainer : ScriptableObject
    {
        public CardProperty[] Card;
        [Button]
        public void listrever()
        {
            Array.Reverse(Card);
        }
        [Button]
        void ShuffleArray()
        {
            for (int i = Card.Length - 1; i > 0; i--)
            {
                int randomIndex =UnityEngine.Random.Range(0, i + 1);

                // Swap elements
                CardProperty temp = Card[i];
                Card[i] = Card[randomIndex];
                Card[randomIndex] = temp;
            }
        }
    }
}