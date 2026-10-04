using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    [CreateAssetMenu(fileName= "MenuBGCollection", menuName = "ScriptableObjects/MenuBGCollection",  order = 1)]
public class MenuBGCollection : ScriptableObject
{
    public MenuBGInfo[] menuBackgrounds;
}

[System.Serializable]
public class MenuBGInfo
{
    public Sprite shopSprites;
    public Sprite menuBGSprites;
    public int BgCost;
    public string bgName;
    
}

