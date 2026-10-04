using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enableCue : MonoBehaviour
{
    public GameObject cue;
    public GameObject cueparent;

    private void EnableCueStick()
    {
        //print("cue enabled");
        cue.SetActive(true);
        cueparent.SetActive(true);
    }


    
}
