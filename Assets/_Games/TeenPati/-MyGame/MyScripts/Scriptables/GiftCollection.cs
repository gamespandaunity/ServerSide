using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    [CreateAssetMenu(fileName= "GiftCollection", menuName = "ScriptableObjects/GiftCollection",  order = 1)]
public class GiftCollection : ScriptableObject
{
    public GiftArray[] giftPopularArray;
}

[System.Serializable]
public class GiftArray
{
    public Sprite Sprites;
    public int giftCost;
    public AudioClip GiftSound;
}

