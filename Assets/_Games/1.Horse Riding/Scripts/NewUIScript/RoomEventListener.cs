using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class RoomEventListener : MonoBehaviour
{
    [SerializeField] MultiLobbyPanel multiLobbyPanel;
    [SerializeField] GameObject avatarPrefab;
    [SerializeField] Transform avatarContainer;
    [SerializeField] Text roomCode;
    //private void OnEnable()
    //{
    //    roomCode.text = multiLobbyPanel.RoomCode;
    //    multiLobbyPanel.InstantiatePlayer(avatarPrefab,avatarContainer);
    //}

    
}
