using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 
public class PhotonConnection //Photon Removal : MonoBehaviourPunCallbacks
{
    private void Awake()
    {
        //Photon Removal DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        //Photon Removal  if (!PhotonNetwork.IsConnected || !PhotonNetwork.InLobby)
        {
            //Photon Removal      PhotonNetwork.ConnectUsingSettings();
        }
    }

    //public override void OnConnectedToMaster()
    //{
    //    PhotonNetwork.JoinLobby();                                                                                 //Photon Removal
    //}

    //public override void OnJoinedLobby()                                                                                      //Photon Removal
    //{
    //}

}

