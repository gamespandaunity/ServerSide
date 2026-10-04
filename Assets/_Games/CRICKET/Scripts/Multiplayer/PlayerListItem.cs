using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 
using TMPro;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class PlayerListItem //Photon Removal : MonoBehaviourPunCallbacks
{

    [FormerlySerializedAs("text")] [SerializeField] TMP_Text statusText;

    [FormerlySerializedAs("playGame")] public UnityEngine.UI.Button playGameButton;

    //Photon Removal    Player playerInstance;
    //Photon Removal  private PhotonView view;

    private void Awake()
    {
        CONTROLLER.PlayModeSelected = 8;
        //Object.DontDestroyOnLoad(this.gameObject);// Persist across scenes

        //Photon Removal   if (PhotonNetwork.InRoom)
        {
            //Photon Removal     if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("totalOvers"))
            {
                ConstantsData_M.Log("WE GOT THE TOTAL NUMBER OF OVERS");
                //CONTROLLER.multiPlayerOvers = (int)PhotonNetwork.CurrentRoom.CustomProperties["totalOvers"];
                ConstantsData_M.Log(CONTROLLER.multiPlayerOvers);
                //CONTROLLER.
                //CONTROLLER.totalOvers = (int)PhotonNetwork.CurrentRoom.CustomProperties["totalOvers"];
                //Photon Removal       CONTROLLER.oversSelectedIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["totalOvers"];
                // UnityEngine.Debug.Log("#*!OVERSSELECTEDIDX : " + CONTROLLER.oversSelectedIndex);
                //Photon Removal      PhotonNetwork.CurrentRoom.CustomProperties["totalOvers"].Show("Total Over");
                //Photon Removal   CONTROLLER.totalOvers = CONTROLLER.Overs[(int)PhotonNetwork.CurrentRoom.CustomProperties["totalOvers"]];
                // UnityEngine.Debug.Log("#*!TOTALOVERS : " + CONTROLLER.totalOvers);
            }
            //Photon Removal   else
            {
            }
        }
        
    }

    //public void SetUp(Player _player)                                                             //Photon Removal
    //{
    //    view = GetComponent<PhotonView>();

    //    playerInstance = _player;
    //    statusText.text = _player.NickName;
    //    ConstantsData_M.Log(_player.ActorNumber);
    //    ConstantsData_M.Log(CONTROLLER.PlayModeSelected);


    //}

    public void ChangeOvers()
    {
        //if (view.IsMine)                                             //Photon Removal
        //{
        //    view.RPC("UpdateOvers", RpcTarget.AllViaServer, playerInstance.ActorNumber);

        //}

    }

    //public override void OnPlayerLeftRoom(Player otherPlayer)                                  //Photon Removal
    //{
    //    if(playerInstance == otherPlayer)
    //    {
    //        Destroy(gameObject);
    //    }
    //}



    //public override void OnLeftRoom()                                                                 //Photon Removal
    //{
    //    Destroy(gameObject);
    //}
}
