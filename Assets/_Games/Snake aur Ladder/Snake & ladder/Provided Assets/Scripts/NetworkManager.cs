//using System;
//using System.Collections;
//using UnityEngine;
//using Photon.Pun;
//using Photon.Realtime;
//using Photon.Pun.Demo.PunBasics;
//using Photon.Pun.Demo.Procedural;
//namespace Snake_Ladder
//{
//    public class NetworkManager : MonoBehaviourPunCallbacks
//    {
//        public static NetworkManager Instance;

//        [Header("UI Prefabs")]
//        [SerializeField] private GameObject RoomPlayerUI;
//        [SerializeField] private GameObject GameplayPlayerUI;
//        [SerializeField] private GameObject GP_AvatarPrefab;

//        [Space]
//        [Header("Room Info")]
//        [SerializeField] private string RoomCode;

//        // Events
//        public event Action Connected;
//        public event Action OnRoomJoined;
//        public event Action RoomLeft;
//        public event Action<string, bool> PlayerLeft;
//        public event Action GameEnded;
//        public event Action PlayerJoin;
//        public event Action StartGame;
//        public event Action Disconnected;
//        public event Action<bool> OpponentFirstTurn;
//        public event Action<int> UpdateEnvironment;
//        public event Action<int> OpponentMoved;
//        public event Action<string> WrongRoomCode;
//        public event Action PublicRoomJoined;
//        public event Action NewPublicRoomCreated;

//        public bool gameOver = false;
//        public bool inGame { get; private set; }
//        public BeadPosition[][] beadPositions;

//        #region Unity Callbacks

//        private void Awake()
//        {
//            Instance = this;
//        }

//        private void Start()
//        {
//            // Uncomment if automatic server connection is desired
//            // ConnectToServer();
//        }

//        #endregion

//        #region Private Methods

//        private void ConnectToServer()
//        {
//            Debug.Log("Attempting to connect to server...");
//            PhotonNetwork.ConnectUsingSettings();
//        }

//        private void JoinLobby()
//        {
//            PhotonNetwork.JoinLobby();
//        }

//        private void HandleJoinedRoom()
//        {
//            if (GameManager.instance.currentGameMode.Equals(GameManager.GameMode.Against_RandomPlayer))
//            {
//                PublicRoomJoined?.Invoke();
//            }
//            else
//            {
//                OnRoomJoined?.Invoke();
//            }
//        }

//        private string GenerateRoomCode()
//        {
//            return UnityEngine.Random.Range(1000, 9999).ToString();
//        }

//        private bool CanRecoverFromDisconnect(DisconnectCause cause)
//        {
//            switch (cause)
//            {
//                case DisconnectCause.Exception:
//                case DisconnectCause.ServerTimeout:
//                case DisconnectCause.ClientTimeout:
//                case DisconnectCause.DisconnectByServerLogic:
//                case DisconnectCause.DisconnectByServerReasonUnknown:
//                    return true;
//                default:
//                    return false;
//            }
//        }

//        private void Recover()
//        {
//            if (!PhotonNetwork.ReconnectAndRejoin())
//            {
//                Debug.LogError("ReconnectAndRejoin failed, attempting Reconnect");
//                if (!PhotonNetwork.Reconnect())
//                {
//                    Debug.LogError("Reconnect failed, attempting ConnectUsingSettings");
//                    if (!PhotonNetwork.ConnectUsingSettings())
//                    {
//                        Debug.LogError("ConnectUsingSettings failed, ending game");
//                        EndGame();
//                    }
//                }
//            }
//        }

//        private void EndGame()
//        {


//            GameEnded?.Invoke();
//            gameOver = true;
//            if (PhotonNetwork.CurrentRoom != null)
//            {
//                PhotonNetwork.CurrentRoom.IsVisible = false;
//                PhotonNetwork.CurrentRoom.IsOpen = false;
//                PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0; // Immediately remove the room
//            }
//            PhotonNetwork.LeaveRoom();
//            Debug.Log("Game Over: Opponent disconnected");
//        }

//        #endregion

//        #region Photon Callbacks

//        public override void OnConnectedToMaster()
//        {
//            base.OnConnectedToMaster();
//            Debug.Log("Successfully connected to server");
//            JoinLobby();
//        }

//        public override void OnJoinedLobby()
//        {
//            base.OnJoinedLobby();
//            Debug.Log("Joined lobby");
//            Connected?.Invoke();
//        }

//        public override void OnJoinedRoom()
//        {
//            base.OnJoinedRoom();
//            bool isRoomLocked = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("IsLocked") ?
//                                (bool)PhotonNetwork.CurrentRoom.CustomProperties["IsLocked"] : false;

//            if (isRoomLocked)
//            {
//                PhotonNetwork.LeaveRoom();
//                WrongRoomCode?.Invoke("Room is locked.");
//                Debug.LogWarning("Attempted to join a locked room");
//            }
//            else
//            {
//                HandleJoinedRoom();
//                Debug.Log("Joined room successfully");
//            }
//        }

//        public override void OnJoinRoomFailed(short returnCode, string message)
//        {
//            string errorText = returnCode switch
//            {
//                ErrorCode.GameDoesNotExist => "Room does not exist.",
//                ErrorCode.GameClosed => "Room is closed (full or locked).",
//                ErrorCode.GameFull => "Room is full.",
//                _ => $"Failed to join room: {returnCode}, {message}"
//            };

//            WrongRoomCode?.Invoke(errorText);
//            Debug.LogError($"Join room failed: {errorText}");
//        }

//        public override void OnLeftRoom()
//        {
//            base.OnLeftRoom();
//            Disconnected?.Invoke();
//            Debug.Log("Left room");
//        }

//        public override void OnPlayerLeftRoom(Player otherPlayer)
//        {
//            base.OnPlayerLeftRoom(otherPlayer);
//            PlayerLeft?.Invoke(otherPlayer.NickName, otherPlayer.IsLocal);
//            Debug.Log($"Player left room: {otherPlayer.NickName}");
//        }

//        public override void OnDisconnected(DisconnectCause cause)
//        {
//            base.OnDisconnected(cause);
//            if (GameManager.instance.currentGameMode.Equals(GameManager.GameMode.Against_Ai))
//                return;

//            if (inGame && CanRecoverFromDisconnect(cause))
//            {
//                Recover();
//            }
//            else
//            {
//                //if (inGame) Disconnected?.Invoke();
//                RoomLeft?.Invoke();

//            }
//        }

//        #endregion

//        #region Public Methods

//        public void ConnectServer()
//        {
//            if (!PhotonNetwork.IsConnectedAndReady)
//            {
//                ConnectToServer();
//            }
//            else
//            {
//                Debug.Log("Already connected to the server");
//                Connected?.Invoke();
//            }
//        }

//        public void DisconnectFromServer()
//        {
//            PhotonNetwork.Disconnect();
//            Debug.Log("User disconnected from server.");
//        }

//        public void CreateRoom()
//        {
//            RoomCode = GenerateRoomCode();
//            PhotonNetwork.CreateRoom(RoomCode, new RoomOptions { MaxPlayers = 2, PlayerTtl = 60000, IsVisible = false }, null);
//            Debug.Log($"Room created with code: {RoomCode}");
//        }

//        public void JoinRoom(string roomCode)
//        {
//            PhotonNetwork.JoinRoom(roomCode);
//            RoomCode = roomCode;
//        }

//        public void LeaveRoom()
//        {
//            if (PhotonNetwork.InRoom)
//            {
//                PhotonNetwork.LeaveRoom();
//            }
//        }

//        public void disconnectNetwork()
//        {
//            PhotonNetwork.Disconnect();
//        }

//        public void SetRoomLockState(bool state)
//        {
//            PhotonNetwork.CurrentRoom.IsOpen = !state;
//            PhotonNetwork.CurrentRoom.IsVisible = !state;
//        }

//        public bool GetRoomLockState(string roomCode)
//        {
//            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("IsLocked"))
//            {
//                return (bool)PhotonNetwork.CurrentRoom.CustomProperties["IsLocked"];
//            }
//            else
//            {
//                return false;
//            }
//        }


//        public bool IsAlone()
//        {
//            return PhotonNetwork.PlayerList.Length == 1;
//        }

//        public bool IsRoomOwner()
//        {
//            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.PlayerList.Length > 0)
//            {
//                int roomOwnerID = PhotonNetwork.CurrentRoom.MasterClientId;
//                int localPlayerID = PhotonNetwork.LocalPlayer.ActorNumber;
//                return roomOwnerID == localPlayerID;
//            }

//            return false;
//        }

//        public bool IsMasterClient()
//        {
//            return PhotonNetwork.IsMasterClient;
//        }

//        public void opponentFirstTurn(bool isTurn)
//        {
//            photonView.RPC(nameof(BroadCastFirstTurn), RpcTarget.OthersBuffered, isTurn);
//        }


//        public void ChangeTurn(int diceNumber)
//        {
//            photonView.RPC(nameof(BroadCastTurnChanged), RpcTarget.OthersBuffered, diceNumber);
//        }

//        public void setEnvironment(int index)
//        {
//            photonView.RPC(nameof(BroadCastSetEnvironment), RpcTarget.OthersBuffered, index);
//        }

//        public void InstantiatePlayer(string playerName, int index)
//        {
//            PhotonNetwork.LocalPlayer.NickName = playerName;
//            // Room Player
//            GameObject Roomplayer = PhotonNetwork.Instantiate(RoomPlayerUI.name, Vector3.zero, Quaternion.identity);
//            Roomplayer.GetComponent<SetPlayerInfo>().BroadCastRoomUi(playerName, index);

//            PhotonNetwork.NickName = playerName;
//            photonView.RPC(nameof(BroadcastJoinMessage), RpcTarget.AllBuffered, playerName);
//            photonView.RPC(nameof(BroadcaseOpponentProperties), RpcTarget.OthersBuffered, playerName, index);
//        }

//        public string GetRoomCode()
//        {
//            return RoomCode;
//        }

//        public void StartMatch()
//        {
//            inGame = true;
//            SetRoomLockState(true);
//        }
//        public void EndMatch()
//        {
//            inGame = false;
//        }

//        public void InstantiateAvatarIn_GP()
//        {
//            GameObject playerAvatar = PhotonNetwork.Instantiate(GP_AvatarPrefab.name, Vector3.zero, Quaternion.identity);
//            playerAvatar.GetComponent<SetGamePlayAvatar>().BroadCastGamePlayUI(GameManager.instance.UserName, GameManager.instance.AvatarId);
//        }

//        public void JoinPublicRandomRoom()
//        {
//            PhotonNetwork.JoinRandomRoom(null, 2);
//        }

//        public override void OnJoinRandomFailed(short returnCode, string message)
//        {
//            Debug.LogWarning("No available public rooms, creating a new one...");
//            RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2, PlayerTtl = 60000 };
//            PhotonNetwork.CreateRoom(null, roomOptions);
//        }

//        public override void OnCreatedRoom()
//        {
//            base.OnCreatedRoom();
//            if (GameManager.instance.currentGameMode.Equals(GameManager.GameMode.Against_RandomPlayer))
//            {
//                NewPublicRoomCreated?.Invoke();
//            }
//            Debug.Log("New public room created");
//        }

//        #endregion

//        #region Photon RPC

//        [PunRPC]
//        private void BroadcastJoinMessage(string joinedPlayerName)
//        {
//            Debug.Log($"{joinedPlayerName} joined the room.");
//            if (!IsAlone()) StartGame?.Invoke();
//            PlayerJoin?.Invoke();
//        }


//        [PunRPC]
//        private void BroadcaseOpponentProperties(string joinedPlayerName, int joinedPlayerAvatarID)
//        {
//            SetOpponentProperties(joinedPlayerName, joinedPlayerAvatarID);
//        }

//        [PunRPC]
//        private void BroadCastTurnChanged(int diceNumber)
//        {
//            OpponentMoved?.Invoke(diceNumber);
//        }

//        [PunRPC]
//        public void BroadCastFirstTurn(bool isTurn)
//        {
//            OpponentFirstTurn?.Invoke(isTurn);
//        }

//        [PunRPC]
//        private void BroadCastSetEnvironment(int index)
//        {
//            UpdateEnvironment?.Invoke(index);
//        }

//        #endregion

//        #region Player Info Methods

//        public void SetOpponentProperties(string name, int AvatarIndex)
//        {
//            GameManager.instance.OpponentName = name;
//            GameManager.instance.OpponentAvatarId = AvatarIndex;
//        }

//        #endregion

//        #region Check Connectivity & Disconnectivity

//        public void OnMasterClientDisconnected()
//        {
//            if (PhotonNetwork.IsMasterClient)
//            {
//                Debug.Log("Master client disconnected, checking guest connection...");
//                StartCoroutine(CheckGuestConnection());
//            }
//        }

//        IEnumerator CheckGuestConnection()
//        {
//            yield return new WaitForSeconds(3f);

//            while (!gameOver)
//            {
//                if (!PhotonNetwork.PlayerListOthers[0].IsInactive)
//                {
//                    EndGame();
//                    yield break;
//                }

//                yield return new WaitForSeconds(2f);
//            }
//        }

//        #endregion

//        #region Sending & Receiving Beads Position

//        public void BroadcastBeadPositions(BeadPosition[][] beadPositions)
//        {
//            if (!PhotonNetwork.IsConnectedAndReady)
//            {
//                Debug.LogWarning("Not connected to Photon network.");
//                return;
//            }

//            photonView.RPC(nameof(ReceiveBeadPositions), RpcTarget.Others, beadPositions);
//        }

//        [PunRPC]
//        private void ReceiveBeadPositions(BeadPosition[][] receivedPositions)
//        {
//            beadPositions = receivedPositions;
//        }

//        #endregion
//    }
//}