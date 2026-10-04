using UnityEngine;

[CreateAssetMenu(fileName = "CardsSprites", menuName = "ScriptableObjects/CardsContainer", order = 1)]

public class CardsContainer : ScriptableObject
{
    public TeenPattiGame.CardProperty[] Card;
}