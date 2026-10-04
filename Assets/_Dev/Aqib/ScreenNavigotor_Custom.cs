using DG.Tweening;
using DG.Tweening.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Device;

public class ScreenNavigotor_Custom : MonoBehaviour
{
    public static ScreenNavigotor_Custom Instance;
    [Serializable]
    public struct ScreenRef
    {
        public string ScreenName;
        public GameObject Screenobj;
    }

    public List<GameObject> ScreenGameObjects;
    public static List<ScreenRef> GameScreenRefs = new List<ScreenRef>();
    public static Stack<ScreenRef> GameScreenStack = new Stack<ScreenRef>();
    private void Awake()
    {
        Instance = this;    
    }
    private void OnEnable()
    {
        GameScreenRefs = new List<ScreenRef>();
        foreach (GameObject screen in ScreenGameObjects)
        {
            ScreenRef newscreen = new ScreenRef();
            newscreen.Screenobj = screen;
            newscreen.ScreenName = screen.name;
            GameScreenRefs.Add(newscreen);
            GameScreenStack.Push(newscreen); 
        }
        GameScreenStack = ReloadStacks();
    }
    Stack<ScreenRef> ReloadStacks()
    {
        Stack<ScreenRef> reloadedstack = new Stack<ScreenRef>();
        foreach (ScreenRef screen in GameScreenStack)
        {
            reloadedstack.Push(GameScreenRefs.Find((screeninlist) => screeninlist.ScreenName == screen.ScreenName));
        }
        return reloadedstack;
    }
  
   
}
