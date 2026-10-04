using System.Collections;
using System.Collections.Generic;
 
using UnityEngine;

public class TurnTrigger : MonoBehaviour
{
    public bool IsRightTurn;
    public bool IsLeftTurn;
    
    private void Start()
    {
        if (MConstants.CurrentCHAMPION_MODE != MConstants.CHAMPION_MODES.DUABI_CHAMPION)
        {
            IsRightTurn = false;
            IsLeftTurn = false;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isPlayer(other))
        {
            if (IsRightTurn)
            {
                Tutorials.instance.OnRightTurnStarted();
            }
            
            if (IsLeftTurn)
            {
                Tutorials.instance.OnLeftTurnStarted();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (isPlayer(other))
        {
            if (IsLeftTurn || IsRightTurn)
            {
                Tutorials.instance.OnTurnCompleted();
            }
        }
    }
    
    private bool isPlayer(Collider other)
    {
        var myPlayer = false;
        //Photon Removal if (other.gameObject.transform.root.CompareTag("Player") && other.gameObject.transform.root.GetComponent<PhotonView>() != null && other.gameObject.transform.root.GetComponent<PhotonView>().IsMine)
        {
            myPlayer = true;
        }
        //Photon Removal   else
        {
            myPlayer = false;
        }
        return myPlayer;
    }
}
