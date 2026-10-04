 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpExcelationTrigger : MonoBehaviour
{
    public bool disableJumpMessage;
    private void OnTriggerEnter(Collider other)
    {


        //Photon Removal if (!disableJumpMessage && HudMenuManager.instance.isJumpMessageVisible && other.transform.root.GetComponent<PhotonView>() && other.transform.root.GetComponent<PhotonView>().IsMine)
        {
            HudMenuManager.instance.isJumpMessageVisible = false;
            
            if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.DUABI_CHAMPION && MConstants.CurrentLevelNumber == 2)
            {
                Tutorials.instance.OnJumpCompleted();
            }

        }

    }

    private void OnCollisionEnter(Collision other)
    {
        //Photon Removal if (disableJumpMessage && other.transform.root.GetComponent<PhotonView>() && other.transform.root.GetComponent<PhotonView>().IsMine)
        {

            HudMenuManager.instance.isJumpMessageVisible = false;
        }
    }
}
