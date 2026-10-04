using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeenPattiManager : MonoBehaviour
{
    public GameObject[] objectsToDisable;
    public GameObject[] objectsToEnable;
    // Start is called before the first frame update
    void Start()
    {
        if(MatchHandler.IsTeenPatti())
        {
            LocalSettings.ToggleObjectState(objectsToDisable, false);
            LocalSettings.ToggleObjectState(objectsToEnable, true);
        }
    }

}
