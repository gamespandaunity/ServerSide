using System.Collections;
using System.Collections.Generic;
 
using UnityEngine;

public class CoinsPicker : MonoBehaviour
{
   public GameObject PickEffect;
   public GameObject CoinModel;
   void OnTriggerEnter(Collider other)
   {
        //Photon Removal   if (other.gameObject.transform.root.CompareTag("Player") && other.gameObject.transform.root.GetComponent<PhotonView>() != null && other.gameObject.transform.root.GetComponent<PhotonView>().IsMine)
        {
            CoinModel.SetActive(false);
         PickEffect.SetActive(true);
         // add 100 coins
      }
   }
}
