using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
 
 
using UnityEngine.SceneManagement;

public class r_PhotonHandler //Photon Removal : MonoBehaviourPunCallbacks
{
    public static r_PhotonHandler instance;

    /// <summary>
    /// Here we are instancing our script to call easily from other scripts.
    /// We don't destroy it because this is our main helper. This script is still available when we are in game in another scene
    /// </summary>

    #region Unity Calls
    //private void Awake()
    //{
    //    if (instance)
    //    {
    //        Destroy(instance);
    //        Destroy(instance.gameObject);                                                                                         //Photon Removal
    //    }

    //    instance = this;
    //    PhotonNetwork.IsMessageQueueRunning = false;
    //}

    //private void Start()
    //{
    //    DontDestroyOnLoad(this);                                                                                                                             //Photon Removal
    //    PhotonNetwork.AutomaticallySyncScene = true;
    //}
    #endregion

    /// <summary>
    /// Here we are connecting to Photon and make our connection ready to play.
    /// </summary>

    #region Connecting
    //public void ConnectToPhoton() => PhotonNetwork.ConnectUsingSettings();                                                                       //Photon Removal

    //public override void OnConnected() => ConstantsData_M.Log("Connected to Photon");                                                                 //Photon Removal

    //public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby(TypedLobby.Default);                                                         //Photon Removal
    #endregion

    /// <summary>
    /// We are creating a room easily from here. For this we need the room name and room options.
    /// </summary>

    #region Room Creation
    //Photon Removal   public void CreateRoom(string _RoomName, RoomOptions _RoomOptions) => PhotonNetwork.CreateRoom(_RoomName, _RoomOptions, null, null);

    //Photon Removal  public override void OnCreatedRoom() => ConstantsData_M.Log("Created Room");
    #endregion

    /// <summary>
    /// In the section below the are catching the room list. We are displaying the rooms in the room browser.
    /// </summary>

    #region Room List
    //Photon Removal  public override void OnJoinedLobby() => ConstantsData_M.Log("Connected to Lobby");

    //public override void OnRoomListUpdate(List<RoomInfo> roomList)
    //{
    //    if (r_RoomBrowserController.instance != null)
    //    {                                                                                                                                                                    //Photon Removal
    //        r_RoomBrowserController.instance.m_RoomBrowserList = roomList;
    //        r_RoomBrowserController.instance.RefreshRoomBrowser();
    //    }

    //    r_LobbyController.instance.ReceiveRoomList(roomList);
    //}
    #endregion

    /// <summary>
    /// When we joined a room, we are checking if the room players are in the lobby or in the game. 
    /// If they are waiting in the lobby, we have to call the enter lobby function. See the Lobby Controller for the functionalities.
    /// If the room players already started with playing the game, we join the game scene.
    /// </summary>

    #region On Join
    //public override void OnJoinedRoom()
    //{
    //    PhotonNetwork.IsMessageQueueRunning = true;

    //    string _RoomState = (string)PhotonNetwork.CurrentRoom.CustomProperties["RoomState"];
    //Photon Removal
    //    switch (_RoomState)
    //    {
    //        case "InLobby":
    //            r_LobbyController.instance.EnterLobby(); //Debug.Log("Joined Game");
    //            break;
    //        case "InGame":
    //            LoadGame(); //Debug.Log("Joined Game Lobby");
    //            break;
    //    }
    //}
    #endregion

    /// <summary>
    /// When we left the room we have to load the main menu scene. This is the build index 0.
    /// We also have to update the player list for players who still in the lobby.
    /// </summary>

    #region On Left
    //public override void OnLeftRoom()
    //{
    //    if (SceneManager.GetActiveScene().buildIndex != 0)                                                                                      //Photon Removal
    //        PhotonNetwork.LoadLevel(0);
    //}

    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{
    //    string _RoomState = (string)PhotonNetwork.CurrentRoom.CustomProperties["RoomState"];
    //Photon Removal
    //    switch (_RoomState)
    //    {
    //        case "InLobby": r_LobbyController.instance.ListLobbyPlayers(); break;
    //        case "InGame": r_InGameController.instance.CheckMasterClient(); break;
    //    }
    //}
    #endregion

    /// <summary>
    /// We are loading the game scene. Loading the game scene is used by the game map + "_" + the game mode.
    /// Like this we can have the same map with different game modes.
    /// </summary>

    #region Matchmaking
    //public void LoadGame()
    //{
    //    if (PhotonNetwork.IsMasterClient)                                                                                                                                                                             //Photon Removal
    //        PhotonNetwork.LoadLevel(PhotonNetwork.CurrentRoom.CustomProperties["GameMap"].ToString() + "_" + PhotonNetwork.CurrentRoom.CustomProperties["GameMode"].ToString());
    //}
    #endregion
}