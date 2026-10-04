using UnityEngine;
[CreateAssetMenu(fileName = "FramesCollections", menuName = "ScriptableObjects/FramesCollections", order = 1)]
public class FramesCollections : ScriptableObject
{
    public SpritesWithSize[] Sprites;
}

[System.Serializable]
public class SpritesWithSize
{
    public Sprite Sprites;
    public float size = 1;
}
