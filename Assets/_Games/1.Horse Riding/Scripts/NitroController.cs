using System.Collections;
using System.Collections.Generic;
using CarRace;
 
using UnityEngine;

public class NitroController : MonoBehaviour {
    public GameObject nitroInner;
    bool isNitrosEnabled = true;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.GetComponent<PlayerPositionController>().isLocalPlayer)
        {
            if (!Tutorials.isTutorialActive)
            {
                HorseMobileButton.Instance.TriggerAutoNitro();
            }
            else
            {
                if (isPlayer(other))
                {
                    Tutorials.instance.ShowNitroTutorial();
                }
            }
            isNitrosEnabled = false;
            nitroInner.SetActive(false);
            Invoke("ReEnableNitros",10);
            DemoGameManagers.Instance.playNitroSound();
           
        }
    }

    void ReEnableNitros()
    {
        isNitrosEnabled = true;
        nitroInner.SetActive(true);
    }
    
    private bool isPlayer(Collider other)
    {
        var myPlayer = false;
        //Photon Removal  if (other.gameObject.CompareTag("PlayerHorse") && other.gameObject.transform.root.GetComponent<PhotonView>() != null && other.gameObject.transform.root.GetComponent<PhotonView>().IsMine)
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
