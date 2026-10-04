using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingForOpponent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnDisable()
    {
        //Photon Removal  PunNetwork.instance.PlayerGameTime = PunNetwork.instance.OpponentGameTime;

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
