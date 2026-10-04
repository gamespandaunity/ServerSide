using System.Collections;
using System.Collections.Generic;
 
using UnityEngine;

public class AIStartTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (isPlayer(other))
        {
            Tutorials.aiStopped = false;
        }
    }
    
    private bool isPlayer(Collider other)
    {
        var myPlayer = false;
        //Photon Removal if (other.gameObject.transform.root.CompareTag("Player") && other.gameObject.transform.root.GetComponent<PhotonView>() != null && other.gameObject.transform.root.GetComponent<PhotonView>().IsMine)
        {
            myPlayer = true;
        }
        //Photon Removal else
        {
            myPlayer = false;
        }
        return myPlayer;
    }
}
