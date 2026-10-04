using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakesAndLadders : MonoBehaviour
{
    public List<SnakesAndLadderEntry> ListOfSnakesAndLadder;
    public Sprite environmentSprite;
}

[System.Serializable]
public class SnakesAndLadderEntry
{
    [System.Serializable]
    public enum Type
    {
        Snake,
        Ladder
    }

    public Type type;
    public Node TriggerNode;
    public Node TargetNode;
    public List<Transform> PathPoints;
}
