using Firebase.Sample.Messaging;
using Mirror;
using NaughtyAttributes;
using NetworkManagement;
using Newtonsoft.Json;
using SocketIOClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExtensions;
using static Firebase.Sample.Messaging.FirebaseHandler;
using static Firebase.Sample.Messaging.FirebaseHandler.NotificationData;
using static SocketIOUnityAdapter;

public class SocketIOUnityAdapter : MonoBehaviour
{
    public static SocketIOUnityAdapter instance;
    public TMP_Text connectionStatusText;

    public GameObject openChallengePrefab;
    public GameObject myChallengePrefab;

    private SocketIOUnity socket;

    public Transform contentForPublicRoom;
    public Transform contentForMyRoom;

    public List<RoomData> publicRooms = new List<RoomData>();
    public List<RoomData> privateRooms = new List<RoomData>();
    public List<RoomData> myRooms = new List<RoomData>();
    public List<string> RejectedChallengesTransactionId;
    public static string transactionId;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public async void Start()
    {
        if (!Utils.IsHeadless())
        {
            await StartAsync();
            RenderPublicRoomTables();
        }
    }

    private async Task StartAsync()
    {
        var uri = new Uri("https://api-dev.myluckygames.com/");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Reconnection = true,
            ReconnectionAttempts = 30,
            ReconnectionDelay = 2000,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Socket Connected!");
            socket.EmitAsync("_syncStore").ContinueWith(task =>
            {
                socket.EmitAsync("_syncStore", response =>
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        try
                        {
                            string json = response.GetValue().ToString();
                            connectionStatusText.text = "";
                            var store = JsonConvert.DeserializeObject<RoomStore>(json);
                            publicRooms = store.publicRooms;
                            privateRooms = store.privateRooms;
                            Debug.Log("_syncStore: " + publicRooms.Count);
                            RenderPublicRoomTables();
                        }
                        catch (Exception ex) { Debug.LogError("JSON parse error: " + ex.Message); }
                    });
                });
            });
        };

        socket.OnDisconnected += (s, e) =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                if (SceneManager.GetActiveScene().name == "Home")
                {
                    PopupMessageManager.instance.SetPanelStaus(true, "Disconnected from server.\r\nReconnecting...");
                    StartCoroutine(ReconnectSocket());
                }
                connectionStatusText.text = "PLEASE CONNECT THE INTERNET";
                publicRooms.Clear();
            });
        };

        socket.OnError += (s, e) => Debug.LogError("Socket.IO Error: " + e);
        socket.OnReconnectAttempt += (s, e) => Debug.LogWarning("Socket Trying to reconnect...");
        socket.OnReconnectFailed += (s, e) => Debug.LogError("Socket Reconnect failed");

        socket.On("publicRooms", response =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                publicRooms = JsonConvert.DeserializeObject<List<RoomData>>(response.GetValue().ToString());
                SaveMyChallenges(publicRooms);
                RenderPublicRoomTables();
                Debug.Log("publicRooms update: " + response.GetValue().ToString());
            });
        });

        socket.On("roomCreated", response =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                List<RoomList<RoomData>> roomList =
                    JsonConvert.DeserializeObject<List<RoomList<RoomData>>>(response.ToString());

                if (roomList[0].packet.type == "private" &&
                    roomList[0].packet.second_player == staticVariables.UserProfiledata.user._id.ToString())
                {
                    Debug.Log("PrivateRoomUser" + roomList[0].packet.second_player);
                    privateRooms.Add(roomList[0].packet);
                    ApiAndRoomManager.currentGameId = roomList[0].packet.game_id;
                    ApiAndRoomManager._instance.userIdentifierOrEmail = roomList[0].packet.player_info_id.ToString();
                    ApiAndRoomManager._instance.winLoseChallengeId = roomList[0].packet.transaction_id;
                    staticVariables.currentTime = int.Parse(roomList[0].packet.bet_expires_sec);
                    staticVariables.isgoldcoins = !roomList[0].packet.bet_type.Contains("silver");
                }
                else
                {
                    if (roomList[0].packet.type != "private")
                    {
                        Debug.Log("PublicRoomUser" + roomList[0].packet.second_player);
                        publicRooms.Add(roomList[0].packet);
                    }
                }
                SaveMyChallenges(publicRooms);
                RenderPublicRoomTables();
                Debug.Log("roomCreated: " + response.GetValue().ToString());
            });
        });

        socket.On("childAdded", response =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var roomList = JsonConvert.DeserializeObject<List<RoomList<GamePacket>>>(response.ToString());
                RoomData room = publicRooms.FirstOrDefault(r => r.transaction_id == roomList[0].transaction_id)
                             ?? privateRooms.FirstOrDefault(r => r.transaction_id == roomList[0].transaction_id);
                room.participants.Add(roomList[0].packet);
                if (staticVariables.UserProfiledata.user._id == room.player_info_id)
                    ShowNotification(roomList[0].packet.notification_type, roomList[0].packet);
                SaveMyChallenges(publicRooms);
                RenderPublicRoomTables();
                Debug.Log("childAdded: " + response.GetValue().ToString());
            });
        });

        socket.On("childActivated", response =>
        {
            "Child will be activated".Show();
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var roomList = JsonConvert.DeserializeObject<List<RoomList<GamePacket>>>(response.ToString());
                if (roomList == null || roomList.Count == 0) return;

                var packet = roomList[0];

                // Only act if this event is for the current user (the challenge accepter)
                if (packet.player_info_id != staticVariables.UserProfiledata.user._id) return;

                // Find room data to build opponent profile
                tojoin = publicRooms.FirstOrDefault(r => r.transaction_id == packet.transaction_id)
                      ?? privateRooms.FirstOrDefault(r => r.transaction_id == packet.transaction_id);

                if (tojoin == null)
                {
                    Debug.LogError($"[childActivated] Room not found for transaction {packet.transaction_id}");
                    return;
                }

                staticVariables.OpponetProfile = new PlayerProfile(
                    tojoin.player_info_name, tojoin.player_info_snap, tojoin.bet_amount,
                    tojoin.game_id.ToString(), tojoin.player_info_id.ToString(),
                    tojoin.transaction_id, !tojoin.bet_type.Contains("silver"), "12000");

                ApiAndRoomManager._instance.winLoseChallengeId = packet.transaction_id;
                Debug.Log("childActivated: " + response.GetValue().ToString());

                // childActivated fires on the challenge CREATOR when the second player joins.
                // Mark ourselves as master/creator BEFORE connecting so CmdSetPlayerData gets
                // the correct role regardless of who arrives at the Mirror server first.
                MirrorNetwork.Instance.isMasterClient = true;

                // Delegate entirely to EdgegapAPIClient.JoinGame.
                // The server_address from the socket event is passed as an override
                // so we connect immediately without waiting for prefs or polling.
                MirrorNetwork.Instance.edgegapAPIClient.JoinGame(
                    packet.transaction_id,
                    null,                  // no Request object here; backend notify skipped (server already notified by creator)
                    packet.server_address);
            });
        });

        socket.On("childRemoved", response =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var roomList = JsonConvert.DeserializeObject<List<RoomList<GamePacket>>>(response.ToString());
                publicRooms
                    .FirstOrDefault(room => room.transaction_id == roomList[0].transaction_id)
                    ?.participants.RemoveAll(p => p.player_info_id == roomList[0].player_info_id);
                SaveMyChallenges(publicRooms);
                RenderPublicRoomTables();
                Debug.Log("childRemoved: " + response.GetValue().ToString());
            });
        });

        socket.On("roomRemoved", response =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var roomList = JsonConvert.DeserializeObject<List<RoomList<GamePacket>>>(response.ToString());
                publicRooms.RemoveAll(room => room.transaction_id == roomList[0].transaction_id);
                myRooms.RemoveAll(room => room.transaction_id == roomList[0].transaction_id);
                SaveMyChallenges(publicRooms);
                RenderPublicRoomTables();
            });
        });

        socket.On("error", response => { });

        await socket.ConnectAsync();
    }

    public RoomData tojoin;

    public IEnumerator ReconnectSocket()
    {
        yield return new WaitForSeconds(2f);
        socket.ConnectAsync().ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    connectionStatusText.text = "";
                    PopupMessageManager.instance.SetPanelStaus(false, "Reconnect");
                }
                else
                {
                    Debug.LogError("Reconnection failed: " + task.Exception);
                    if (SceneManager.GetActiveScene().name == "Home")
                        PopupMessageManager.instance.SetPanelStaus(true, "Disconnected from server.\r\nReconnecting...");
                    ReconnectSocket();
                }
            });
        });
    }

    public void SaveMyChallenges(List<RoomData> list)
    {
        foreach (var room in list)
            if (room.player_info_id == staticVariables.UserProfiledata.user._id)
                myRooms.Add(room);
    }

    public void RenderPublicRoomTables()
    {
        LoadTable(publicRooms);
        UpdateRoomsCounter(publicRooms);
        Debug.Log("RenderPublicRoomTables: " + publicRooms.Count);
    }

    public void RenderPrivateRoomTables()
    {
        LoadTable(privateRooms);
        UpdateRoomsCounter(privateRooms);
        Debug.Log("RenderprivateRooms: " + privateRooms.Count);
    }

    void UpdateRoomsCounter(List<RoomData> currentroomList)
    {
        foreach (GameButton_InfoSetter txt in staticVariables.All_games_Ref)
        {
            if (txt.game_id == 5 || txt.game_id == 9 || txt.game_id == 10)
            {
                txt.GameChallengestxt.GoldChallengestxt.text = "0";
                txt.GameChallengestxt.SilverChallengestxt.text = "0";
            }
            else
            {
                List<RoomData> allgold = currentroomList.Where(r =>
                    r.bet_type == "gold" && txt.game_id == r.game_id &&
                    r.player_info_id.ToString() != staticVariables.UserProfiledata.user._id.ToString() &&
                    !RejectedChallengesTransactionId.Contains(r.transaction_id)).ToList();

                List<RoomData> allsilver = currentroomList.Where(r =>
                    r.bet_type == "silver" && txt.game_id == r.game_id &&
                    r.player_info_id.ToString() != staticVariables.UserProfiledata.user._id.ToString() &&
                    !RejectedChallengesTransactionId.Contains(r.transaction_id)).ToList();

                txt.GameChallengestxt.GoldChallengestxt.text = ChallengeList_Count(allgold.Count());
                txt.GameChallengestxt.SilverChallengestxt.text = ChallengeList_Count(allsilver.Count());
            }
        }
    }

    public string ChallengeList_Count(int count) => count > 98 ? "99+" : count.ToString();

    private void LoadTable(List<RoomData> rooms)
    {
        if (SceneManager.GetActiveScene().name != "Home") return;

        contentForPublicRoom.Clear();
        contentForMyRoom.Clear();
        rooms.Reverse();

        foreach (var room in rooms)
        {
            bool isgold = room.bet_type == "gold";
            if (room.game_id != ApiAndRoomManager.currentGameId || isgold != staticVariables.isgoldcoins) continue;
            if (RejectedChallengesTransactionId.Contains(room.transaction_id)) continue;

            if (staticVariables.UserProfiledata.user._id != room.player_info_id)
            {
                GameObject row = Instantiate(openChallengePrefab, contentForPublicRoom);
                row.GetComponent<PlayerProfileUI>().Initialize(room);
            }
            else
            {
                GameObject row = Instantiate(myChallengePrefab, contentForMyRoom);
                PlayerProfileUI ui = row.GetComponent<PlayerProfileUI>();
                ui.Initialize(room);
                ui.deleteBet.gameObject.SetActive(true);
            }
        }
    }

    public static void openPanelByName(string str)
    {
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
            gb.SetActive(gb.name.Equals(str));
    }

    public void ShowNotification(string notificationType, GamePacket gamepacket = null)
    {
        switch (notificationType)
        {
            case "challenge_accepted":
                if (SceneManager.GetActiveScene().name == "Home" &&
                    UIMainMenManager.instance.gameRequestPanel.activeSelf &&
                    RoomsListManager.instance.RoomId == gamepacket.transaction_id)
                {
                    RoomsListManager.instance.GetRequests(gamepacket.transaction_id, gamepacket.game_id);
                }
                else
                {
                    if (!challengeNotificationInProcess)
                    {
                        ApiAndRoomManager.currentGameId = gamepacket.game_id;
                        staticVariables.currentTime = int.Parse(gamepacket.bet_expires_sec);
                        ApiAndRoomManager._instance.winLoseChallengeId = gamepacket.transaction_id;
                        ApiAndRoomManager._instance.userIdentifierOrEmail = gamepacket.player_info_id.ToString();
                        NotificationUIManager.instance.AcceptNotificationIns(gamepacket);
                        staticVariables.isnotificationcounter = true;
                        challengeNotificationInProcess = true;
                    }

                    ChallengeNotificationInfo data = new ChallengeNotificationInfo
                    {
                        gameId = gamepacket.game_id,
                        userIdOrEmail = gamepacket.player_info_id.ToString(),
                        winnerLoseId = gamepacket.transaction_id,
                        currentTime = gamepacket.bet_expires_sec,
                        packet = gamepacket
                    };
                    FirebaseHandler._instance.challengeNotificationsList.Add(data);
                }
                break;
        }
    }

    private async void OnDisable() => await DisconnectAsync();

    [Button]
    public async Task DisconnectAsync()
    {
        try
        {
            if (socket != null && socket.Connected)
            {
                Debug.Log("Disconnecting from socket...");
                await socket.DisconnectAsync();
                Debug.Log("Socket disconnected successfully!");
            }
            else Debug.Log("Socket already disconnected or null.");
        }
        catch (Exception ex) { Debug.LogError($"Error disconnecting socket: {ex.Message}"); }
    }

    // ─── Data Models ─────────────────────────────────────────────────────────

    [Serializable]
    public class RoomData
    {
        public string type;
        public string overs;
        public int game_id;
        public string bet_type;
        public int bet_amount;
        public int player_info_id;
        public string transaction_id;
        public string bet_expires_sec;
        public string player_info_name;
        public string player_info_snap;
        public Sprite profilePic;
        public string notification_type;
        public string created_at;
        public List<GamePacket> participants;
        public string region;
        public string second_player;
        public string server_address;
    }

    [Serializable]
    public class GamePacket
    {
        public int game_id;
        public string bet_type;
        public int bet_amount;
        public int player_info_id;
        public string transaction_id;
        public string bet_expires_sec;
        public string player_info_name;
        public string player_info_snap;
        public string notification_type;
        public string region;
        public string server_address;
    }

    [Serializable]
    public class RoomList<T>
    {
        public string room;
        public string transaction_id;
        public int player_info_id;
        public string server_address;
        public T packet;
    }

    [Serializable]
    public class RoomStore
    {
        public List<RoomData> publicRooms;
        public List<RoomData> privateRooms;
    }
}
