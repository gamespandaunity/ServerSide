using System.Collections;
using System.Collections.Generic;
 
using UnityEngine;

public class JumpTutorialEvent : MonoBehaviour
{
    public bool isJump;
    public bool isRightTurn;
    public bool isLeftTurn;

    private void Start()
    {
        if (MConstants.CurrentCHAMPION_MODE != MConstants.CHAMPION_MODES.DUABI_CHAMPION)
        {
            isJump = false;
            isRightTurn = false;
            isLeftTurn = false;
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (isPlayer(other))
        {
            if (isJump)
            {
                Tutorials.instance.OnJumpStarted();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (isPlayer(other))
        {
            if (isJump)
            {
                Tutorials.instance.OnJumpCompleted();
            }
        }
    }

    private bool isPlayer(Collider other)
    {
        var myPlayer = false;
        //Photon Removal  if (other.gameObject.transform.root.CompareTag("Player") && other.gameObject.transform.root.GetComponent<PhotonView>() != null && other.gameObject.transform.root.GetComponent<PhotonView>().IsMine)
        {
            myPlayer = true;
        }
        //Photon Removal    else
        {
            myPlayer = false;
        }
        return myPlayer;
    }
}
