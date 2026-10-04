using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Twelve
{
    public class NetworkManagerTwelve : NetworkBehaviour
    {
        public static NetworkManagerTwelve Instance;

        [Header("UI Prefabs")]
        [SerializeField] private GameObject RoomPlayerUI;
        [SerializeField] private GameObject GameplayPlayerUI;
        [SerializeField] private GameObject GP_AvatarPrefab;

        public GameObject beads1, beads2, emptyNodes, beadsParent;

        [Space]
        [Header("Room Info")]
        [SerializeField] private string RoomCode;

        // Events
        public event Action Connected;
        public event Action OnRoomJoined;
        public event Action RoomLeft;
        public event Action PlayerLeft;
        public event Action GameEnded;
        public event Action PlayerJoin;
        public event Action StartGame;
        public event Action TurnChanged;
        public event Action<string> WrongRoomCode;
        public event Action PublicRoomJoined;
        public event Action NewPublicRoomCreated;

        private bool gameOver = false;
        public BeadPositionTwelve[][] beadPositions;

        // Mirror specific
        private Dictionary<int, PlayerInfo> players = new Dictionary<int, PlayerInfo>();
        private bool isRoomLocked = false;

        [System.Serializable]
        public class PlayerInfo
        {
            public string playerName;
            public int avatarId;
            public NetworkConnectionToClient conn;
        }
        private void OnDisable()
        {
            MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
        }

        private void OnEnable()
        {
            MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
        }

        public void DirectResult(bool result)
        {
            if (result)
            {
                // Victory(staticVariables.UserProfiledata.user._id, "Opponent has disconnected. You are declared the winner.");
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }
            else
            {
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(false, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }

        }
        #region Unity Callbacks

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
              //  Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Uncomment if automatic server connection is desired
            // ConnectToServer();
        }

        #endregion

        #region Private Methods

        private void ConnectToServer()
        {
            Debug.Log("Attempting to connect to server...");
            
            if (Mirror.NetworkManager.singleton != null)
            {
                Mirror.NetworkManager.singleton.StartClient();
            }
        }

        private void HandleJoinedRoom()
        {
            if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_RandomPlayer))
            {
                PublicRoomJoined?.Invoke();
            }
            else
            {
                OnRoomJoined?.Invoke();
            }
        }

        private string GenerateRoomCode()
        {
            return UnityEngine.Random.Range(1000, 9999).ToString();
        }

        private void EndGame()
        {
            GameEnded?.Invoke();
            gameOver = true;
            isRoomLocked = true;
            
            if (isServer)
            {
                // Disconnect all clients
                foreach (var conn in NetworkServer.connections.Values)
                {
                    conn.Disconnect();
                }
                Mirror.NetworkManager.singleton.StopHost();
            }
            else
            {
                Mirror.NetworkManager.singleton.StopClient();
            }
            
            Debug.Log("Game Over: Opponent disconnected");
        }

        #endregion

        #region Mirror Callbacks

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("Server started");
            NetworkServer.RegisterHandler<JoinRoomMessage>(OnJoinRoomRequest);
            NetworkServer.RegisterHandler<CreateRoomMessage>(OnCreateRoomRequest);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("Client started");
            
            if (!isServer)
            {
                NetworkClient.RegisterHandler<RoomJoinedMessage>(OnRoomJoinedMessage);
                NetworkClient.RegisterHandler<RoomJoinFailedMessage>(OnRoomJoinFailedMessage);
                Connected?.Invoke();
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            RoomLeft?.Invoke();
            Debug.Log("Client stopped");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            players.Clear();
            Debug.Log("Server stopped");
        }

        // Called when a client connects to the server
        public void OnClientConnected()
        {
            Debug.Log("Successfully connected to server");
            Connected?.Invoke();
        }

        // Called when a client disconnects
        public void OnClientDisconnected()
        {
            if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_Ai))
                return;
                
            RoomLeft?.Invoke();
        }

        #endregion

        #region Custom Messages

        public struct JoinRoomMessage : NetworkMessage
        {
            public string roomCode;
            public string playerName;
            public int avatarId;
        }

        public struct CreateRoomMessage : NetworkMessage
        {
            public string playerName;
            public int avatarId;
        }

        public struct RoomJoinedMessage : NetworkMessage
        {
            public string roomCode;
            public bool success;
        }

        public struct RoomJoinFailedMessage : NetworkMessage
        {
            public string error;
        }

        public struct PlayerJoinedMessage : NetworkMessage
        {
            public string playerName;
            public int avatarId;
        }

        public struct TurnChangedMessage : NetworkMessage { }

        #endregion

        #region Message Handlers

        private void OnJoinRoomRequest(NetworkConnectionToClient conn, JoinRoomMessage msg)
        {
            if (isRoomLocked)
            {
                conn.Send(new RoomJoinFailedMessage { error = "Room is locked." });
                conn.Disconnect();
                return;
            }

            if (msg.roomCode != RoomCode)
            {
                conn.Send(new RoomJoinFailedMessage { error = "Room does not exist." });
                conn.Disconnect();
                return;
            }

            if (players.Count >= 2)
            {
                conn.Send(new RoomJoinFailedMessage { error = "Room is full." });
                conn.Disconnect();
                return;
            }

            // Add player
            PlayerInfo playerInfo = new PlayerInfo
            {
                playerName = msg.playerName,
                avatarId = msg.avatarId,
                conn = conn
            };
            players[conn.connectionId] = playerInfo;

            conn.Send(new RoomJoinedMessage { roomCode = RoomCode, success = true });
            
            // Notify all clients about new player
            ApplyPlayerJoined(msg.playerName, msg.avatarId);
            
            Debug.Log($"{msg.playerName} joined the room.");
        }

        private void OnCreateRoomRequest(NetworkConnectionToClient conn, CreateRoomMessage msg)
        {
            RoomCode = GenerateRoomCode();
            
            PlayerInfo playerInfo = new PlayerInfo
            {
                playerName = msg.playerName,
                avatarId = msg.avatarId,
                conn = conn
            };
            players[conn.connectionId] = playerInfo;

            conn.Send(new RoomJoinedMessage { roomCode = RoomCode, success = true });
            
            if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_RandomPlayer))
            {
                NewPublicRoomCreated?.Invoke();
            }
            
            Debug.Log($"Room created with code: {RoomCode}");
        }

        private void OnRoomJoinedMessage(RoomJoinedMessage msg)
        {
            if (msg.success)
            {
                RoomCode = msg.roomCode;
                HandleJoinedRoom();
                Debug.Log("Joined room successfully");
            }
        }

        private void OnRoomJoinFailedMessage(RoomJoinFailedMessage msg)
        {
            WrongRoomCode?.Invoke(msg.error);
            Debug.LogError($"Join room failed: {msg.error}");
        }

        #endregion

        #region Public Methods

        public void ConnectServer()
        {
            if (Mirror.NetworkManager.singleton == null)
            {
                Debug.LogError("NetworkManager singleton is null");
                return;
            }

            if (!NetworkClient.isConnected)
            {
                ConnectToServer();
            }
            else
            {
                Debug.Log("Already connected to the server");
                Connected?.Invoke();
            }
        }

        public void CreateRoom()
        {
            if (Mirror.NetworkManager.singleton != null)
            {
                Mirror.NetworkManager.singleton.StartHost();
                RoomCode = GenerateRoomCode();
                Debug.Log($"Room created with code: {RoomCode}");
                
                if (Snake_Ladder.GameManager.instance != null)
                {
                    OnRoomJoined?.Invoke();
                }
            }
        }

        public void JoinRoom(string roomCode)
        {
            if (Mirror.NetworkManager.singleton != null)
            {
                Mirror.NetworkManager.singleton.StartClient();
                RoomCode = roomCode;
                
                // Send join request to server
                StartCoroutine(SendJoinRequestWhenConnected(roomCode));
            }
        }

        private IEnumerator SendJoinRequestWhenConnected(string roomCode)
        {
            // Wait for connection
            while (!NetworkClient.isConnected)
            {
                yield return null;
            }

            JoinRoomMessage msg = new JoinRoomMessage
            {
                roomCode = roomCode,
                playerName = Snake_Ladder.GameManager.instance.UserName,
                avatarId = Snake_Ladder.GameManager.instance.AvatarId
            };
            NetworkClient.Send(msg);
        }

        public void LeaveRoom()
        {
            if (isServer)
            {
                Mirror.NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                Mirror.NetworkManager.singleton.StopClient();
            }
        }

        public void disconnectNetwork()
        {
            if (isServer)
            {
                Mirror.NetworkManager.singleton.StopHost();
            }
            else
            {
                Mirror.NetworkManager.singleton.StopClient();
            }
        }

        public void SetRoomLockState(bool state)
        {
            if (isServer)
            {
                isRoomLocked = state;
                Debug.Log($"Room lock state set to: {state}");
            }
            else
            {
                Debug.LogWarning("[12Bead] Legacy room-lock relay was removed; current matchmaking owns room state.");
            }
        }

        public bool GetRoomLockState(string roomCode)
        {
            return isRoomLocked;
        }

        public bool IsAlone()
        {
            return players.Count == 1 || NetworkServer.connections.Count == 1;
        }

        public bool IsRoomOwner()
        {
            return isServer;
        }

        public bool IsMasterClient()
        {
            return isServer;
        }

        public void ChangeTurn()
        {
            // The old authority-free turn relay was removed; the rules owner advances turns.
            TurnChanged?.Invoke();
        }

        public void InstantiatePlayer(string playerName, int index)
        {
            if (isServer)
            {
                GameObject roomPlayer = Instantiate(RoomPlayerUI, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(roomPlayer);
                roomPlayer.GetComponent<SetPlayerInfoTwelve>().BroadCastRoomUi(playerName, index);
                
                ApplyJoinMessage(playerName, index);
            }
            else
            {
                Debug.LogWarning("[12Bead] Legacy player-spawn relay was removed.");
            }
        }

        public string GetRoomCode()
        {
            return RoomCode;
        }

        public void StartMatch()
        {
            SetRoomLockState(true);
        }

        public GameObject BeadsInstantiate(string name, Vector3 position, Quaternion rotation)
        {
            GameObject bead = null;
            
            if (isServer)
            {
                // Find the prefab by name (you'll need to have a reference to your prefabs)
                GameObject prefab = Resources.Load<GameObject>(name);
                bead = Instantiate(prefab, position, rotation);
                NetworkServer.Spawn(bead);
            }
            else
            {
                Debug.LogWarning("[12Bead] Legacy resource-name bead spawn relay was removed.");
            }
            
            return bead;
        }

        public void InstantiateAvatarIn_GP()
        {
            if (isServer)
            {
                GameObject playerAvatar = Instantiate(GP_AvatarPrefab, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(playerAvatar);
                playerAvatar.GetComponent<SetGamePlayAvatarTwelve>().BroadCastGamePlayUI(
                    Snake_Ladder.GameManager.instance.UserName, 
                    Snake_Ladder.GameManager.instance.AvatarId
                );
            }
            else
            {
                Debug.LogWarning("[12Bead] Legacy avatar-spawn relay was removed.");
            }
        }

        public void JoinPublicRandomRoom()
        {
            // Mirror doesn't have built-in matchmaking
            // You'd need to implement a custom matchmaking server or use a third-party service
            // For now, just create or join a room
            CreateRoom();
        }

        #endregion

        #region Legacy local presentation

        private void ApplyJoinMessage(string joinedPlayerName, int joinedPlayerAvatarID)
        {
            Debug.Log($"{joinedPlayerName} joined the room.");
            SetOpponentProperties(joinedPlayerName, joinedPlayerAvatarID);
            if (!IsAlone()) StartGame?.Invoke();
            PlayerJoin?.Invoke();
        }

        private void ApplyPlayerJoined(string playerName, int avatarId)
        {
            Debug.Log($"{playerName} joined the room.");
            SetOpponentProperties(playerName, avatarId);
            if (!IsAlone()) StartGame?.Invoke();
            PlayerJoin?.Invoke();
        }

        #endregion

        #region Player Info Methods

        public void SetOpponentProperties(string name, int AvatarIndex)
        {
            if (Snake_Ladder.GameManager.instance.UserName != name)
            {
                Snake_Ladder.GameManager.instance.OpponentName = name;
                Snake_Ladder.GameManager.instance.OpponentAvatarId = AvatarIndex;
            }
        }

        #endregion

        #region Check Connectivity & Disconnectivity

        public void OnMasterClientDisconnected()
        {
            if (isServer)
            {
                Debug.Log("Master client disconnected, checking guest connection...");
                StartCoroutine(CheckGuestConnection());
            }
        }

        IEnumerator CheckGuestConnection()
        {
            yield return new WaitForSeconds(3f);

            while (!gameOver)
            {
                if (NetworkServer.connections.Count <= 1)
                {
                    EndGame();
                    yield break;
                }

                yield return new WaitForSeconds(2f);
            }
        }

        public void OnPlayerLeftRoom()
        {
            if (!gameOver)
            {
                Debug.Log("Opponent left the room.");
                EndGame();
            }
        }

        #endregion

        #region Sending & Receiving Beads Position

        public void BroadcastBeadPositions(BeadPositionTwelve[][] beadPositions)
        {
            // Arbitrary position snapshots were an obsolete client relay; validated moves own board state.
            Debug.LogWarning("[12Bead] Legacy bead-position broadcast was removed.");
        }

        #endregion
    }
}
// using System;
// using System.Collections;
// using UnityEngine;
// using System.Xml.Linq;
// using Mirror;

// namespace Twelve
// {
//     public class NetworkManagerTwelve : NetworkBehaviour
// {
//     public static NetworkManagerTwelve Instance;

//     [Header("UI Prefabs")]
//     [SerializeField] private GameObject RoomPlayerUI;
//     [SerializeField] private GameObject GameplayPlayerUI;
//     [SerializeField] private GameObject GP_AvatarPrefab;

//     public GameObject beads1, beads2, emptyNodes, beadsParent;

//     [Space]
//     [Header("Room Info")]
//     [SerializeField] private string RoomCode;

//     // Events
//     public event Action Connected;
//     public event Action OnRoomJoined;
//     public event Action RoomLeft;
//     public event Action PlayerLeft;
//     public event Action GameEnded;
//     public event Action PlayerJoin;
//     public event Action StartGame;
//     public event Action TurnChanged;
//     public event Action<string> WrongRoomCode;
//     public event Action PublicRoomJoined;
//     public event Action NewPublicRoomCreated;

//     private bool gameOver = false;
//     public BeadPositionTwelve[][] beadPositions;

//     #region Unity Callbacks

//     private void Awake()
//     {
//         // if (Instance == null)
//         // {
//         //     Instance = this;
//         //     DontDestroyOnLoad(this);
//         // }
//         // else
//         // {
//         //     Destroy(gameObject);
//         // }

//         Instance = this;
//         DontDestroyOnLoad(this);
//     }

//     private void Start()
//     {
//         // Uncomment if automatic server connection is desired
//         // ConnectToServer();
//     }

//     #endregion

//     #region Private Methods

//     private void ConnectToServer()
//     {
//         Debug.Log("Attempting to connect to server...");
//         PhotonNetwork.ConnectUsingSettings();
//     }

//     private void JoinLobby()
//     {
//         PhotonNetwork.JoinLobby();
//     }

//     private void HandleJoinedRoom()
//     {
//         if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_RandomPlayer))
//         {
//             PublicRoomJoined?.Invoke();
//         }
//         else
//         {
//             OnRoomJoined?.Invoke();
//         }
//     }

//     private string GenerateRoomCode()
//     {
//         return UnityEngine.Random.Range(1000, 9999).ToString();
//     }

//     private bool CanRecoverFromDisconnect(DisconnectCause cause)
//     {
//         switch (cause)
//         {
//             case DisconnectCause.Exception:
//             case DisconnectCause.ServerTimeout:
//             case DisconnectCause.ClientTimeout:
//             case DisconnectCause.DisconnectByServerLogic:
//             case DisconnectCause.DisconnectByServerReasonUnknown:
//                 return true;
//             default:
//                 return false;
//         }
//     }

//     private void Recover()
//     {
//         if (!PhotonNetwork.ReconnectAndRejoin())
//         {
//             Debug.LogError("ReconnectAndRejoin failed, attempting Reconnect");
//             if (!PhotonNetwork.Reconnect())
//             {
//                 Debug.LogError("Reconnect failed, attempting ConnectUsingSettings");
//                 if (!PhotonNetwork.ConnectUsingSettings())
//                 {
//                     Debug.LogError("ConnectUsingSettings failed, ending game");
//                     EndGame();
//                 }
//             }
//         }
//     }
     
//         private void EndGame()
//     {
        

//         GameEnded?.Invoke();
//         gameOver = true;
//         PhotonNetwork.CurrentRoom.IsVisible = false;
//         PhotonNetwork.CurrentRoom.IsOpen = false;
//         PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0; // Immediately remove the room
//         PhotonNetwork.LeaveRoom();
//         Debug.Log("Game Over: Opponent disconnected");
//     }

//     #endregion

//     #region Photon Callbacks

//     public override void OnConnectedToMaster()
//     {
//         base.OnConnectedToMaster();
//         Debug.Log("Successfully connected to server");
//         JoinLobby();
//     }

//     public override void OnJoinedLobby()
//     {
//         base.OnJoinedLobby();
//         Debug.Log("Joined lobby");
//         Connected?.Invoke();
//     }

//     public override void OnJoinedRoom()
//     {
//         base.OnJoinedRoom();
//         bool isRoomLocked = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("IsLocked") ?
//                             (bool)PhotonNetwork.CurrentRoom.CustomProperties["IsLocked"] : false;

//         if (isRoomLocked)
//         {
//             PhotonNetwork.LeaveRoom();
//             WrongRoomCode?.Invoke("Room is locked.");
//             Debug.LogWarning("Attempted to join a locked room");
//         }
//         else
//         {
//             HandleJoinedRoom();
//             Debug.Log("Joined room successfully");
//         }
//     }

//     public override void OnJoinRoomFailed(short returnCode, string message)
//     {
//         string errorText = returnCode switch
//         {
//             ErrorCode.GameDoesNotExist => "Room does not exist.",
//             ErrorCode.GameClosed => "Room is closed (full or locked).",
//             ErrorCode.GameFull => "Room is full.",
//             _ => $"Failed to join room: {returnCode}, {message}"
//         };

//         WrongRoomCode?.Invoke(errorText);
//         Debug.LogError($"Join room failed: {errorText}");
//     }

//     public override void OnLeftRoom()
//     {
//         base.OnLeftRoom();
//         RoomLeft?.Invoke();
//         Debug.Log("Left room");
//     }

//     public override void OnPlayerLeftRoom(Player otherPlayer)
//     {
//         base.OnPlayerLeftRoom(otherPlayer);
//         PlayerLeft?.Invoke();
//         Debug.Log($"Player left room: {otherPlayer.NickName}");
//     }

//     public override void OnDisconnected(DisconnectCause cause)
//     {
//         if(Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_Ai)) 
//             return;
//         base.OnDisconnected(cause);
//         if (CanRecoverFromDisconnect(cause))
//         {
//             Recover();
//         }
//         else 
//         {
//             RoomLeft?.Invoke();
//         }
//     }

//     #endregion

//     #region Public Methods

//     public void ConnectServer()
//     {
//         if (!PhotonNetwork.IsConnectedAndReady)
//         {
//             ConnectToServer();
//         }
//         else
//         {
//             Debug.Log("Already connected to the server");
//             Connected?.Invoke();
//         }
//     }

//     public void CreateRoom()
//     {
//         RoomCode = GenerateRoomCode();
//         PhotonNetwork.CreateRoom(RoomCode, new RoomOptions { MaxPlayers = 2, PlayerTtl = 60000, EmptyRoomTtl = 30000 }, null);
//         Debug.Log($"Room created with code: {RoomCode}");
//     }

//     public void JoinRoom(string roomCode)
//     {
//         PhotonNetwork.JoinRoom(roomCode);
//         RoomCode = roomCode;
//     }

//     public void LeaveRoom()
//     {
//         if (PhotonNetwork.InRoom)
//         {
//             PhotonNetwork.LeaveRoom();
//         }
//     }

//     public void disconnectNetwork()
//     {
//         PhotonNetwork.Disconnect();
//     }

//     public void SetRoomLockState(bool state)
//     {
//             PhotonNetwork.CurrentRoom.IsOpen= !state;
//             PhotonNetwork.CurrentRoom.IsVisible= !state;
//         // if (PhotonNetwork.InRoom && IsRoomOwner())
//         // {
//         //     ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable
//         //     {
//         //         { "IsLocked", state }
//         //     };

//         //     PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties);
//         //     Debug.Log($"Room lock state set to: {state}");
//         // }
//         // else
//         // {
//         //     Debug.LogWarning("Only the room owner can set the lock state or not in a room");
//         // }
//     }

//     public bool GetRoomLockState(string roomCode)
//     {
//         if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("IsLocked"))
//         {
//             return (bool)PhotonNetwork.CurrentRoom.CustomProperties["IsLocked"];
//         }
//         else
//         {
            
//             return false;
//         }
//     }


//     public bool IsAlone()
//     {
//         return PhotonNetwork.PlayerList.Length == 1;
//     }

//     public bool IsRoomOwner()
//     {
//         if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.PlayerList.Length > 0)
//         {
//             int roomOwnerID = PhotonNetwork.CurrentRoom.MasterClientId;
//             int localPlayerID = PhotonNetwork.LocalPlayer.ActorNumber;
//             return roomOwnerID == localPlayerID;
//         }

//         return false;
//     }

//     public bool IsMasterClient()
//     {
//         return PhotonNetwork.IsMasterClient;
//     }

//     public void ChangeTurn()
//     {
//         photonView.RPC(nameof(BroadCastTurnChanged), RpcTarget.All);
//     }

//     public void InstantiatePlayer(string playerName, int index)
//     {
//         PhotonNetwork.LocalPlayer.NickName = playerName;
//         // Room Player
//         GameObject Roomplayer = PhotonNetwork.Instantiate(RoomPlayerUI.name, Vector3.zero, Quaternion.identity);
//         Roomplayer.GetComponent<SetPlayerInfoTwelve>().BroadCastRoomUi(playerName, index);

//         PhotonNetwork.NickName = playerName;
//         photonView.RPC(nameof(BroadcastJoinMessage), RpcTarget.AllBuffered, playerName, index);
//     }

//     public string GetRoomCode()
//     {
//         return RoomCode;
//     }

//     public void StartMatch()
//     {
//         SetRoomLockState(true);
//     }

//     public GameObject BeadsInstantiate(string name, Vector3 position, Quaternion rotation)
//     {
//         return PhotonNetwork.Instantiate(name, position, rotation);
//     }

//     public void InstantiateAvatarIn_GP()
//     {
//         GameObject playerAvatar = PhotonNetwork.Instantiate(GP_AvatarPrefab.name, Vector3.zero, Quaternion.identity);
//         playerAvatar.GetComponent<SetGamePlayAvatarTwelve>().BroadCastGamePlayUI(Snake_Ladder.GameManager.instance.UserName, Snake_Ladder.GameManager.instance.AvatarId);
//     }

//     public void JoinPublicRandomRoom()
//     {
//         PhotonNetwork.JoinRandomRoom(null, 2);
//     }

//     public override void OnJoinRandomFailed(short returnCode, string message)
//     {
//         Debug.LogWarning("No available public rooms, creating a new one...");
//         RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2, PlayerTtl= 60000, EmptyRoomTtl = 30000  };
//         PhotonNetwork.CreateRoom(null, roomOptions);
//     }

//     public override void OnCreatedRoom()
//     {
//         base.OnCreatedRoom();
//         if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_RandomPlayer))
//         {
//             NewPublicRoomCreated?.Invoke();
//         }
//         Debug.Log("New public room created");
//     }

//     #endregion

//     #region Photon RPC

//     [PunRPC]
//     private void BroadcastJoinMessage(string joinedPlayerName, int joinedPlayerAvatarID)
//     {
//         Debug.Log($"{joinedPlayerName} joined the room.");
//         SetOpponentProperties(joinedPlayerName, joinedPlayerAvatarID);
//         if (!IsAlone()) StartGame?.Invoke();
//         PlayerJoin?.Invoke();
//     }

//     [PunRPC]
//     private void BroadCastTurnChanged()
//     {
//         TurnChanged?.Invoke();
//     }

//     #endregion

//     #region Player Info Methods

//     public void SetOpponentProperties(string name, int AvatarIndex)
//     {
//         if (PhotonNetwork.NickName != name)
//         {
//                 Snake_Ladder.GameManager.instance.OpponentName = name;
//                 Snake_Ladder.GameManager.instance.OpponentAvatarId = AvatarIndex;
//         }
//     }

//     #endregion

//     #region Check Connectivity & Disconnectivity

//     public void OnMasterClientDisconnected()
//     {
//         if (PhotonNetwork.IsMasterClient)
//         {
//             Debug.Log("Master client disconnected, checking guest connection...");
//             StartCoroutine(CheckGuestConnection());
//         }
//     }

//     IEnumerator CheckGuestConnection()
//     {
//         yield return new WaitForSeconds(3f);

//         while (!gameOver)
//         {
//             if (!PhotonNetwork.PlayerListOthers[0].IsInactive)
//             {
//                 EndGame();
//                 yield break;
//             }

//             yield return new WaitForSeconds(2f);
//         }
//     }

//     public void OnPlayerLeftRoom()
//     {
//         if (!gameOver)
//         {
//             Debug.Log("Opponent left the room.");
//             EndGame();
//         }
//     }

//     #endregion

//     #region Sending & Receiving Beads Position

//     public void BroadcastBeadPositions(BeadPositionTwelve[][] beadPositions)
//     {
//         if (!PhotonNetwork.IsConnectedAndReady)
//         {
//             Debug.LogWarning("Not connected to Photon network.");
//             return;
//         }

//         photonView.RPC(nameof(ReceiveBeadPositions), RpcTarget.Others, beadPositions);
//     }

//     [PunRPC]
//     private void ReceiveBeadPositions(BeadPositionTwelve[][] receivedPositions)
//     {
//         beadPositions = receivedPositions;
//     }

//     #endregion
// }
// }
