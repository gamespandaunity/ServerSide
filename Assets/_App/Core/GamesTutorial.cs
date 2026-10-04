using UnityEngine;

[CreateAssetMenu(fileName = "HowToPlay",menuName = "Tutorial",order = 1)]
public class GamesTutorial : ScriptableObject
{
    [TextArea(minLines:15,maxLines:30)]
    public string[] data;
   
}
