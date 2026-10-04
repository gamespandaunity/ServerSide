using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AvatarCollections", menuName = "ScriptableObjects/AvatarCollections", order = 1)]
public class AvatarCollections : ScriptableObject
{
    public SpritesWithName[] Sprites;
}

[System.Serializable]
public class SpritesWithName
{
    public Sprite Sprites;
    public string Name= "Male";
}

