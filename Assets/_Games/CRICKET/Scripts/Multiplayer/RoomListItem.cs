using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 
using TMPro;
using UnityEngine.Serialization;

public class RoomListItem : MonoBehaviour
{ 

    [FormerlySerializedAs("text")] [SerializeField] TMP_Text roomInfoText;

    //RoomInfo roomDetails;                                                             //Photon Removal

    //public void SetUp(RoomInfo _info)                                                                //Photon Removal
    //{
    //    roomDetails = _info;
    //    roomInfoText.text = _info.Name;
    //}

    //public void OnClick()                                                                               //Photon Removal
    //{
    //    Launcher.Instance.JoinRoom(roomDetails);
    //}
}
