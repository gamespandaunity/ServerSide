//
//using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace POKER
{
    public class PhotonConnectionHandler : MonoBehaviour
    {

        public Text SlowInternetInfoText;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            //if (!PhotonNetwork.IsConnectedAndReady)
            //{
            //    if (!UIManager.Instance.LoadingPanel.activeSelf)
            //        UIManager.Instance.LoadingPanel.SetActive(true);
            //}
            //else
            //{
            //    if (UIManager.Instance.LoadingPanel.activeSelf)
            //        UIManager.Instance.LoadingPanel.SetActive(false);
            //}

            // Check for slow internet connection
            //if (PhotonNetwork.NetworkingClient.LoadBalancingPeer.RoundTripTime > 500)
            //{
            //    if (SlowInternetInfoText != null)
            //        SlowInternetInfoText.text = "Slow Internet";
            //    The round-trip time is greater than 500 milliseconds, which indicates a slow internet connection.
            //    Debug.Log("Slow internet connection detected.");
            //}
        }
    }
}