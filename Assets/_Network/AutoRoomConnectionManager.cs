using System.Collections.Generic;
using UnityEngine;
 
 
using UnityEngine.SceneManagement;

public class AutoRoomConnectionManager  //Photon Removal: MonoBehaviourPunCallbacks
{
    [Header("Room Settings")]
    [SerializeField] private string roomNamePrefix = "PrivateRoom_";
    [SerializeField] private int maxPlayersPerRoom = 2;
    [SerializeField] private string sceneToLoad = "GameScene";

    [Header("Connection Settings")]
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private bool autoConnect = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private string currentRoomName;
    private bool isConnecting = false;
    private bool sceneLoadInitiated = false;

    private void Start()
    {
        if (autoConnect)
            ConnectToPhoton();
    }

    private void OnEnable()
    {
        //Photon Removal  PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        //Photon Removal   PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void ConnectToPhoton()
    {
        if (isConnecting) return;

        isConnecting = true;

        //Photon Removal  PhotonNetwork.GameVersion = gameVersion;
        //Photon Removal  PhotonNetwork.AutomaticallySyncScene = true;

        //Photon Removal  if (!PhotonNetwork.IsConnected)
        {
            Log("Connecting to Photon...");
            //Photon Removal        PhotonNetwork.ConnectUsingSettings();
        }
        //Photon Removal   else
        {
            Log("Already connected. Joining lobby...");
            //Photon Removal       PhotonNetwork.JoinLobby();
        }
    }

    #region Photon Callbacks

    //public override void OnConnectedToMaster()
    //{                                                                                                                    //Photon Removal
    //    Log("Connected to Photon Master Server.");
    //    PhotonNetwork.JoinLobby();
    //}

    //public override void OnDisconnected(DisconnectCause cause)
    //{                                                                                                                  //Photon Removal
    //    Log($"Disconnected: {cause}");
    //    isConnecting = false;
    //    sceneLoadInitiated = false;
    //}

    //public override void OnJoinedLobby()
    //{                                                                                                                  //Photon Removal
    //    Log("Joined lobby. Trying to join or create room...");
    //    TryJoinOrCreateRoom();
    //}

    //public override void OnCreateRoomFailed(short returnCode, string message)
    //{                                                                                                                                                 //Photon Removal
    //    Log($"Create room failed: {message}. Retrying...");
    //    TryJoinOrCreateRoom();
    //}

    //public override void OnJoinRoomFailed(short returnCode, string message)
    //{
    //    Log($"Join room failed: {message}. Creating new room...");                                                                                           //Photon Removal
    //    CreatePrivateRoom();
    //}

    //public override void OnJoinRandomFailed(short returnCode, string message)
    //{                                                                                                                                                             //Photon Removal
    //    Log($"Join random room failed: {message}. Creating new room...");
    //    CreatePrivateRoom();
    //}

    //public override void OnJoinedRoom()
    //{
    //    Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");                                                                                                        //Photon Removal
    //    Log($"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    //    CheckPlayersAndLoadScene();
    //}

    //public override void OnPlayerEnteredRoom(Player newPlayer)
    //{                                                                                                                                                                     //Photon Removal
    //    Log($"Player joined: {newPlayer.NickName}");
    //    CheckPlayersAndLoadScene();
    //}

    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{                                                                                                                                                              //Photon Removal
    //    Log($"Player left: {otherPlayer.NickName}");
    //    sceneLoadInitiated = false;
    //}

    //public override void OnRoomListUpdate(List<RoomInfo> roomList)
    //{
    //    Log($"Room list updated. Count: {roomList.Count}");                                                                                                                   //Photon Removal
    //}

    //public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    //{
    //    Log($"Player properties updated: {targetPlayer.NickName}");                                                                                                               //Photon Removal
    //}

    //public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    //{
    //    Log("Room properties updated.");                                                                                                                                                //Photon Removal
    //}

    //public override void OnMasterClientSwitched(Player newMasterClient)
    //{                                                                                                                                                                                             //Photon Removal
    //    Log($"New master client: {newMasterClient.NickName}");
    //}

    #endregion

    private void TryJoinOrCreateRoom()
    {
        currentRoomName = roomNamePrefix + System.Guid.NewGuid().ToString("N")[..8];

        //Photon Removal    RoomOptions options = new RoomOptions
        {
            //Photon Removal        MaxPlayers = maxPlayersPerRoom,
            //Photon Removal        IsVisible = false,
            //Photon Removal       IsOpen = true
        }
        ;

        //Photon Removal    PhotonNetwork.JoinRandomRoom(null, maxPlayersPerRoom);
    }

    private void CreatePrivateRoom()
    {
        currentRoomName = roomNamePrefix + System.Guid.NewGuid().ToString("N")[..8];

        //Photon Removal    RoomOptions options = new RoomOptions
        {
            //Photon Removal      MaxPlayers = maxPlayersPerRoom,
            //Photon Removal      IsVisible = false,
            //Photon Removal     IsOpen = true
        }
        ;

        Log($"Creating room: {currentRoomName}");
        //Photon Removal   PhotonNetwork.CreateRoom(currentRoomName, options);
    }

    private void CheckPlayersAndLoadScene()
    {
        if (sceneLoadInitiated) return;

        //Photon Removal   if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom)
        {
            Log($"Room is full. Loading scene: {sceneToLoad}");
            LoadGameScene();
        }
        //Photon Removal  else
        {
            //Photon Removal    Log($"Waiting for players... ({PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom})");
        }
    }

    private void LoadGameScene()
    {
        if (sceneLoadInitiated) return;

        sceneLoadInitiated = true;

        //Photon Removal   if (PhotonNetwork.IsMasterClient)
        {
            Log("Loading game scene...");
            //Photon Removal    PhotonNetwork.LoadLevel(sceneToLoad);
        }
        //Photon Removal   else
        {
            Log("Waiting for master client to load scene...");
        }
    }

    private void Log(string msg)
    {
        if (showDebugLogs)
            Debug.Log($"[AutoRoomConnectionManager] {msg}");
    }

    // Optional Public Utilities
    public void ManualConnect() => ConnectToPhoton();

    public void LeaveRoom()
    {
        //Photon Removal  if (PhotonNetwork.InRoom)
        //Photon Removal PhotonNetwork.LeaveRoom();
    }

    public void SetSceneToLoad(string sceneName)
    {
        sceneToLoad = sceneName;
    }

    public string GetConnectionStatus()
    {
        //Photon Removal    if (!PhotonNetwork.IsConnected) return "Disconnected";
        //Photon Removal   if (PhotonNetwork.InRoom)
        //Photon Removal  return $"In Room: {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        //Photon Removal if (PhotonNetwork.InLobby) return "In Lobby";
        return "Connected";
    }
}
