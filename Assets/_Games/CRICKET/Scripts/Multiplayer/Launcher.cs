using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using System.Data;
using ExitGames.Client.Photon;
using System.Transactions;
using Cricket;
using Mirror;

public class Launcher : MonoBehaviour//Photon Removal : MonoBehaviourPunCallbacks
{
    public static Launcher Instance;


    [SerializeField] TextMeshProUGUI Ping_Text;

    //Photon Removal  [HideInInspector] public Player[] players;
    public bool amIOwner;
    private NetworkIdentity PV;
    int OverIdx;
    private Dictionary<string, GameObject> instantiatedRooms = new Dictionary<string, GameObject>();
    private bool isConnected;
    public bool Reconnecting;
    private string roomName;
    private int reconnectionCount;
    private const byte RPC_CLEAR_BUFFERED_RPCS = 100;
    private int BufferCalledCount = 0;

    private int DisconnectCount = 0;


    private Coroutine opponentRejoining;

    private Coroutine playerRejoining;



    void Awake()
    {
        amIOwner = false;

        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            //       if (PhotonNetwork.CurrentRoom != null)
            //  roomName = PhotonNetwork.CurrentRoom.Name;
        }
        else
        {
            roomName = "";
        }
    }

    void Start()
    {
        CONTROLLER.DIRECTWIN = false;
        //Photon Removal   PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = 2f;
        PV = GetComponent<NetworkIdentity>();

        if (!GameConstants.isWithAI)
        {
            // Behaviour-preserving guard. This whole block is MENU flow: MultiplayerPanel lives in
            // MainMenu, so in Ground its static Instance is null and the first call used to throw,
            // aborting Start() before OnJoinedRoom()/GetMultiplayerState() ever ran. Letting those two
            // run in Ground would be a NEW code path (they re-enter match setup), so keep the original
            // outcome — skip the block entirely when the menu isn't there — and only remove the NRE.
            if (MultiplayerPanel.Instance != null)
            {
                MultiplayerPanel.Instance.CloseMenu("Loading");
                MultiplayerPanel.Instance.OpenMenu("Main");
                OnJoinedRoom();
                if (GameModeTWO.instance != null)
                {
                    GameModeTWO.instance.GetMultiplayerState();
                }
            }
        }

    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            //   Ping_Text.text = "PING : " + PhotonNetwork.GetPing() + " ms";
        }
    }

    public void ConnectToServer()
    {

        // if (!PhotonNetwork.IsConnectedAndReady)
        {
            PV = GetComponent<NetworkIdentity>();
        }
    }



    public void StopSyncing()
    {
        //Photon Removal   PhotonNetwork.IsMessageQueueRunning = false;
        //Photon Removal   PhotonNetwork.AutomaticallySyncScene = false;
    }


    //public override void OnConnectedToMaster()
    //{

    //    PV = GetComponent<PhotonView>();
    //    PhotonNetwork.JoinLobby();
    //    if (SceneManager.GetActiveScene().name == "MainMenu")
    //    {
    //        PhotonNetwork.NickName = "Player" + Random.Range(0, 1000).ToString("0000");                                                                                                                                                //Photon Removal
    //    }
    //    else
    //    {
    //        if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && Singleton<GroundController>.instance.timerObject.activeInHierarchy)
    //        {
    //            Singleton<GroundController>.instance.Invoke("ToggleDOTweenSequence", 0.5f);
    //        }

    //        if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && Singleton<BowlingScoreCard>.instance.Timer.activeInHierarchy)
    //        {
    //            Singleton<BowlingScoreCard>.instance.ResumeTimer();
    //        }
    //    }

    //}

    private void OnConnectedToServer()
    {
    }

    //public override void OnJoinedLobby()
    //{
    //    $"On Join Lobby {CONTROLLER.DIRECTWIN}".Show();                                                                                                                                                                        //Photon Removal
    //    if (SceneManager.GetActiveScene().name != "MainMenu" && CONTROLLER.DIRECTWIN == false)
    //    {
    //        StartCoroutine(RetryRejoin());
    //    }
    //    else
    //    {
    //        if (roomName == "")
    //        {
    //        }
    //        else if (CONTROLLER.DIRECTWIN == false)
    //        {
    //            StartCoroutine(RetryRejoin());
    //        }
    //    }
    //}

    private IEnumerator RetryRejoin()
    {
        int retryCount = 0;
        const int maxRetries = 4;


        if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
                && (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
        {
            Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
            StartCoroutine(AnimateDots());
        }
        else if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Singleton<GameData>.instance.waitingForOpponentText1.text = "Reconnected , Trying to Rejoin now.....";
        }
        while (retryCount < maxRetries)
        {

            //Photon Removal  if (PhotonNetwork.InRoom)
            {
                Invoke("ToggleReconnecting", 1f);

                yield break;


            }

            //Photon Removal  if (PhotonNetwork.IsConnectedAndReady)
            {
                //Photon Removal      PhotonNetwork.RejoinRoom(roomName);
            }


            yield return new WaitForSeconds(0.8f); // Wait for 2 seconds before retrying


            retryCount++;
        }

        //Photon Removal if (!PhotonNetwork.InRoom)
        {

            if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
                && (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
            {
                Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
                StartCoroutine(AnimateDots());
                Singleton<GroundController>.instance.LeaveRoomAutomatically(false);
            }
            else if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                Singleton<GameData>.instance.waitingForOpponentText1.text = "Failed to Rejoin the Room.....";
                Singleton<GroundController>.instance.LeaveRoomAutomatically(false);
            }
        }
    }

    public void CreateRoom()
    {
        amIOwner = true;


        if (CONTROLLER.oversSelectedIndex == 0)
        {
            OverIdx = 0;
        }
        else if (CONTROLLER.oversSelectedIndex == 1)
        {
            OverIdx = 1;
        }
        else
        {
            OverIdx = 2;
        }
        ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
        customProperties.Add("totalOvers", CONTROLLER.oversSelectedIndex);

        //Photon Removal    RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 2 };
        //Photon Removal  roomOptions.CustomRoomProperties = customProperties;
        //Photon Removal  roomOptions.PlayerTtl = 20000;
        MultiplayerPanel.Instance.OpenMenu("Loading");

    }

    //Photon Removal  [PunRPC]
    public void UpdateOvers(int newOvers)
    {
        CONTROLLER.multiPlayerOvers = newOvers;

        //Photon Removal     PV.RPC("UpdateOversOnClients", RpcTarget.All, newOvers);


    }

    //Photon Removal   [PunRPC]
    private void UpdateOversOnClients(int newOvers)
    {
        CONTROLLER.multiPlayerOvers = newOvers; // Update on all clients for consistency
    }

    private void ToggleReconnecting()
    {
        Reconnecting = false;
    }

    //public override void OnConnected()
    //{
    //    if (SceneManager.GetActiveScene().name != "MainMenu")
    //    {                                                                                                        //Photon Removal

    //        if (playerRejoining != null)
    //        {
    //            StopCoroutine(playerRejoining);
    //        }

    //    }
    //}

    //public override void OnDisconnected(DisconnectCause cause)
    //{

    //    PhotonNetwork.RemoveBufferedRPCs();
    //    ConstantsData_M.Log("Disconnect" + cause.ToString());  //ye dek kab call hota ha?ok
    //    Reconnecting = true;
    //    string playerID = PlayerPrefs.GetString("PlayerID", ""); // Default to an empty string if not found

    //    PhotonNetwork.LocalPlayer.CustomProperties["PlayerID"] = playerID;

    //    if (SceneManager.GetActiveScene().name != "MainMenu")
    //    {

    //        if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
    //            && (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
    //        {
    //            Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
    //        }
    //        else
    //        {
    //            Singleton<GameData>.instance.waitingForOpponentText1.text = "Disconnected from Room.......";
    //        }


    //        if (!Singleton<GameData>.instance.waitingForOpponentPanel1.activeInHierarchy)                                                                                                             //Photon Removal
    //        {
    //            Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(true);
    //            if (Singleton<GameData>.instance.waitingForOpponentText1.text == "Loading")
    //            {
    //                StartCoroutine(AnimateDots());
    //            }
    //        }
    //        if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && Singleton<GroundController>.instance.timerObject.activeInHierarchy)
    //        {
    //            Singleton<GroundController>.instance.Invoke("ToggleDOTweenSequence", 0.5f);
    //        }

    //        if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && Singleton<BowlingScoreCard>.instance.Timer.activeInHierarchy)
    //        {
    //            Singleton<BowlingScoreCard>.instance.PauseTimer();
    //        }

    //        Invoke("ReConnect", 10f);
    //    }
    //    else
    //    {
    //        if (roomName != "")
    //        {
    //            Invoke("ReConnect", 10f);
    //        }
    //    }

    //}

    private IEnumerator ReconnectAndRejoinWithRetries()
    {
        int retryCount = 0;
        const int maxRetries = 6;

        if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
    && (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
        {
            Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
            StartCoroutine(AnimateDots());
        }
        else if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Singleton<GameData>.instance.waitingForOpponentText1.text = "Trying to Reconnect and Rejoin the Room .........";
        }

        while (retryCount < maxRetries)
        {

            //Photon Removal   if (PhotonNetwork.IsConnected)
            {
                yield break;
            }


            //Photon Removal   PhotonNetwork.ConnectUsingSettings();



            yield return new WaitForSeconds(0.5f); // Wait for 2 seconds before retrying


            retryCount++;
        }

        //    if (!PhotonNetwork.IsConnected)
        //    {
        //        if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
        //&& (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
        //        {
        //            Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";                                                                                                                   //Photon Removal
        //            StartCoroutine(AnimateDots());
        //            Singleton<GroundController>.instance.LeaveRoomAutomatically(false);
        //        }
        //        else if (SceneManager.GetActiveScene().name != "MainMenu")
        //        {

        //            Singleton<GameData>.instance.waitingForOpponentText1.text = "Failed to Rejoin the Room.....";

        //            Singleton<GroundController>.instance.LeaveRoomAutomatically(false);
        //        }
        //    }
    }

    private void ReConnect()
    {
        //Photon Removal if (PhotonNetwork.IsConnected)
        {
        }
        StartCoroutine(ReconnectAndRejoinWithRetries());
    }

    public int GetReconnectCount()
    {
        return reconnectionCount;
    }
    //Photon Removal  [PunRPC]
    public void RPC_ClearBufferedRPCs()
    {
        BufferCalledCount++;
        if (BufferCalledCount == 1)
        {
            //Photon Removal PhotonNetwork.RemoveBufferedRPCs(PhotonNetwork.LocalPlayer.ActorNumber);
            Invoke("ResetBufferCount", 1f);
        }

    }

    private void ResetBufferCount()
    {
        BufferCalledCount = 0;
    }

    public void CallRemoveBuffered()
    {
        //if (PhotonNetwork.IsConnected)
        //{
        //    PhotonNetwork.RemoveBufferedRPCs();
        //    PhotonNetwork.IsMessageQueueRunning = false;
        //    if (PhotonNetwork.IsMasterClient)
        //    {
        //        PhotonNetwork.DestroyAll();
        //    }
        //}
    }

    private IEnumerator ResendRemoveBuffered()
    {
        for (int i = 0; i < 3; i++)
        {
            //Photon Removal   PV.RPC("RPC_ClearBufferedRPCs", RpcTarget.Others);
            yield return null;
        }
    }

    public void OnJoinedRoom()
    {
        string playerID = staticVariables.UserProfiledata.user._id.ToString();
        CONTROLLER.DIRECTWIN = false;

        PlayerPrefs.SetString("PlayerID", playerID);

        // Set the custom property
        // photon removal PhotonNetwork.LocalPlayer.CustomProperties["PlayerID"] = playerID;

        CONTROLLER.READY = 0;
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            reconnectionCount = 0;
            CONTROLLER.SELECTTEAM = 0;
            if (Singleton<TeamSelectionTWO>.instance.Holder.activeInHierarchy)
            {
                Singleton<TeamSelectionTWO>.instance.OnPlayerReConnect();
            }

            if (Singleton<TossPageTWO>.instance.Holder.activeInHierarchy)
            {
                Singleton<TossPageTWO>.instance.OnPlayerReConnect();
            }

        }


        if (SceneManager.GetActiveScene().name == "MainMenu" && roomName == "")
        {
            MultiplayerPanel.Instance.OpenMenu("Room");
        }
        //Photon Removal roomName = PhotonNetwork.CurrentRoom.Name;

        if (SceneManager.GetActiveScene().name != "MainMenu")                                                                                                                                           //Photon Removal
        {
            if (Singleton<GameData>.instance.waitingForOpponentPanel1.activeInHierarchy)
            {
                Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(false);
            }

            if (Singleton<GameData>.instance.waitingForOpponentPanel.activeInHierarchy)
            {
                Singleton<GameData>.instance.CheckForOverComplete();
            }

            reconnectionCount++;
            // ROOT reconnect-flag fix: OnJoinedRoom re-runs in the Ground scene ONLY on a genuine
            // reconnect (fresh-match Ground load reuses the DontDestroyOnLoad Launcher and does NOT
            // re-enter here). The Photon-era code that set Reconnecting=true on disconnect was commented
            // out in the Mirror migration, so IsReConnecting() was ALWAYS false — which silently
            // DISABLED the entire reconnect-restore machinery gated on it (SetTeamIndex index restore,
            // currentInnings restore, and the Gap-C "don't push reset match-progress while reconnecting"
            // guard). That dead guard let a reconnecting batting client push reset/zeroed indices+totals
            // onto the authoritative SyncVars, flipping the innings on BOTH screens (target→1, scores 0/0,
            // wrong winner). Set it true here so those guards actually run; cleared when the restore
            // completes (ApplyReconnectState) — and IsReConnecting() also OR's RestorePending as a backstop.
            Reconnecting = true;
        }


        //Photon Removal players = PhotonNetwork.PlayerList;

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {

        }


    }

    //Photon Removal [PunRPC]
    private void RPC_OppTeam()
    {

    }



    //public override void OnCreateRoomFailed(short returnCode, string message)
    //{
    //    ConstantsData_M.Log("Room Creation Failed: " + message);                                                                                                                                                                         //Photon Removal
    //    MultiplayerPanel.Instance.OpenMenu("Error");
    //}

    public void StartGame()
    {
        //if (PhotonNetwork.IsConnected)                                                                                                                                                                                                       //Photon Removal
        //{
        //    photonView.RPC("ChangeScene", RpcTarget.AllBuffered);
        //}



    }

    //Photon Removal [PunRPC]
    void ChangeScene()
    {
        //Photon Removal     PhotonNetwork.LoadLevel(3);
    }

    public void LeaveRoom()
    {
        //Photon Removal    PhotonNetwork.LeaveRoom();
        amIOwner = false;
        roomName = "";
        MultiplayerPanel.Instance.OpenMenu("Loading");
    }

    //public void JoinRoom(RoomInfo info)
    //{
    //    PhotonNetwork.JoinRoom(info.Name);
    //    MultiplayerPanel.Instance.OpenMenu("Loading");                                                                                                                                        //Photon Removal
    //}

    public void FindRoom(TMP_InputField RoomName)
    {
        //Photon Removal  PhotonNetwork.JoinRoom(RoomName.text.ToString());
    }

    //public override void OnJoinRoomFailed(short returnCode, string message)                                                                                   //Photon Removal
    //{
    //    if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
    //&& (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
    //    {
    //        Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
    //        StartCoroutine(AnimateDots());
    //        return;
    //    }
    //    else if (SceneManager.GetActiveScene().name != "MainMenu")
    //    {
    //        Singleton<GameData>.instance.waitingForOpponentText1.text = "Room Joining Failed: " + message;
    //        return;
    //    }
    //    MultiplayerPanel.Instance.OpenMenu("Error");
    //}
    //public override void OnLeftRoom()                                                                                                                     //Photon Removal
    //{
    //    if (SceneManager.GetActiveScene().name == "MainMenu" && CONTROLLER.DIRECTWIN == false)
    //    {
    //        Singleton<TeamSelectionTWO>.instance.OnPlayerDisconnect();
    //        Singleton<TossPageTWO>.instance.OnPlayerDisconnect();
    //    }
    //    else if (CONTROLLER.DIRECTWIN == true)
    //    {
    //        {
    //            PhotonNetwork.JoinLobby();
    //        }
    //    }
    //}

    //public override void OnRoomListUpdate(List<RoomInfo> roomList)                                                                                        //Photon Removal
    //{
    //    if (SceneManager.GetActiveScene().name != "MainMenu")
    //    {
    //        return;
    //    }

    //    foreach (RoomInfo roomInfo in roomList)
    //    {
    //        if (roomInfo.RemovedFromList)
    //        {
    //            // Room has been removed, destroy its UI element
    //            if (instantiatedRooms.ContainsKey(roomInfo.Name))
    //            {
    //                Destroy(instantiatedRooms[roomInfo.Name]);
    //                instantiatedRooms.Remove(roomInfo.Name);
    //            }
    //            continue;
    //        }

    //        // Room exists or needs to be created
    //        GameObject roomListItem;

    //    }

    //    // Check for missing room destruction (optional)
    //    if (instantiatedRooms.Count == 0)
    //    {
    //        // No rooms found, potentially missed removal
    //        List<string> roomNamesToDestroy = instantiatedRooms.Keys.ToList();
    //        foreach (string roomName in roomNamesToDestroy)
    //        {
    //            if (!roomList.Any(info => info.Name == roomName))
    //            {
    //                Destroy(instantiatedRooms[roomName]);
    //                instantiatedRooms.Remove(roomName);
    //                // Add a message or logic to indicate a room might have been missed
    //            }
    //        }
    //    }

    //}

    //public int NumberOfPlayersInRoom()                                                                                            //Photon Removal
    //{
    //    return PhotonNetwork.CurrentRoom.PlayerCount;

    //}

    public bool IsReConnecting()
    {
        // Reconnecting (set true at the OnJoinedRoom Ground reconnect entry, cleared when the restore
        // dispatches) covers the early NewGame window; RestorePending (set in DispatchReconnectRestore,
        // cleared at the END of ApplyReconnectState) covers the rest of the restore. The OR keeps the
        // whole reconnect-restore window guarded even though the two flags clear at different points.
        return Reconnecting || CricketNetworkManager.RestorePending;
    }

    public bool Isconnected()
    {
        return true;
    }

    public void SetisConnected(bool Isconnected)
    {
        isConnected = Isconnected;
    }

    //public override void OnPlayerLeftRoom(Player otherPlayer)                                                                                                                        //Photon Removal
    //{
    //    PhotonNetwork.RemoveBufferedRPCs();

    //    if (SceneManager.GetActiveScene().name != "MainMenu" && CONTROLLER.DIRECTWIN == false)
    //    {
    //        isConnected = false;
    //        if (NumberOfPlayersInRoom() == 1)
    //        {
    //            return;
    //        }
    //        PhotonNetwork.RemoveBufferedRPCs();


    //        if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
    //&& (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
    //        {
    //            Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
    //            StartCoroutine(AnimateDots());
    //        }
    //        else
    //        {
    //            Singleton<GameData>.instance.waitingForOpponentText1.text = "Opponent has beend Disconnected and trying to Reconnect .......";
    //        }

    //        StartCoroutine(ActivatePanel());

    //        if (opponentRejoining != null)
    //        {
    //            return;
    //        }
    //        opponentRejoining = StartCoroutine(OpponentRejoining());
    //        if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && Singleton<GroundController>.instance.timerObject.activeInHierarchy)
    //        {
    //            Singleton<GroundController>.instance.Invoke("ToggleDOTweenSequence", 0.5f);

    //        }
    //        return;

    //    }
    //    else if (SceneManager.GetActiveScene().name == "MainMenu")
    //    {
    //        PhotonNetwork.RemoveBufferedRPCs();

    //        amIOwner = true;
    //        if (MultiplayerPanel.Instance.IsMenuActive("Room"))
    //        {
    //            RoomMenu.Instance.OnOpponentLeftRoom();
    //        }
    //    }
    //}

    //private IEnumerator OpponentRejoining()                                                                       //Photon Removal
    //{

    //    yield return new WaitForSeconds(20f);

    //    if (!isConnected && PhotonNetwork.IsConnected)
    //    {
    //        if (!Singleton<GameData>.instance.waitingForOpponentPanel1.activeInHierarchy)
    //        {
    //            Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(true);

    //        }


    //        Singleton<GameData>.instance.waitingForOpponentText1.text = "Your opponent has left the match. Showing the result.....";


    //        CONTROLLER.DIRECTWIN = true;
    //        Singleton<GroundController>.instance.LeaveRoomAutomatically(true);
    //    }
    //    if (opponentRejoining != null)
    //    {
    //        opponentRejoining = null;
    //    }
    //    yield break;
    //}

    private IEnumerator ActivatePanel()
    {
        int sec = 0;
        while (sec < 3)
        {
            if (Singleton<BowlingScoreCard>.instance.scoreCard.activeInHierarchy
    && (CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6) % 2 == 0)
            {
                Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
            }
            else
            {
                Singleton<GameData>.instance.waitingForOpponentText1.text = "Opponent has beend Disconnected and trying to Reconnect .......";
            }
            if (!Singleton<GameData>.instance.waitingForOpponentPanel1.activeInHierarchy)
            {
                Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(true);
                if (Singleton<GameData>.instance.waitingForOpponentText1.text == "Loading")
                {
                    StartCoroutine(Launcher.Instance.AnimateDots());
                }
            }

            sec++;
            yield return new WaitForSeconds(0.1f);
        }

        if (!Singleton<GameData>.instance.waitingForOpponentPanel1.activeInHierarchy)
        {
            Singleton<GameData>.instance.waitingForOpponentText1.text = "Opponent has beend Disconnected and trying to Reconnect .......";
            Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(true);


        }

    }

    public void OnPlayerEnteredRoom()                                                                                                                                          //Photon Removal
    {

        if (SceneManager.GetActiveScene().name != "MainMenu" && CONTROLLER.DIRECTWIN == false)
        {
            if (opponentRejoining != null)
            {
                StopCoroutine(opponentRejoining);
                opponentRejoining = null;
            }

            if (Singleton<GameData>.instance.waitingForOpponentPanel.activeInHierarchy)
            {
                Singleton<GameData>.instance.CheckForOverComplete();
            }

            Singleton<GameData>.instance.waitingForOpponentPanel1.SetActive(false);


            // PV.RPC("RPC_CheckScore", RpcTarget.OthersBuffered, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores, Singleton<ScoreBoardBallList>.instance.ballCount, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets);
            CricketNetworkManager.instance.CmdCheckScore(staticVariables.UserProfiledata.user._id, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores, Singleton<ScoreBoardBallList>.instance.ballCount, CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets);

            if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && Singleton<GroundController>.instance.timerObject.activeInHierarchy)
            {
                Singleton<GroundController>.instance.Invoke("ToggleDOTweenSequence", 0.5f);
            }
            //photonView.RPC("RPC_CheckScene", RpcTarget.OthersBuffered, SceneManager.GetActiveScene().name, CONTROLLER.meFirstBatting);
            CricketNetworkManager.instance.CmdCheckScene(staticVariables.UserProfiledata.user._id, SceneManager.GetActiveScene().name, CONTROLLER.meFirstBatting);
            return;
        }
        else
        {
            RoomMenu.Instance.OnOpponentConnected();
            // if (PhotonNetwork.IsConnected)
            // {
            // }
        }
    }

    //Photon Removal  [PunRPC]
    public void RPC_EqualScore(int MATCHSCORES, int CURRENTBALLS, int MATCHBALLS, int MATCHWICKETS, int CURRENTBALLNUMBER)
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        Singleton<ScoreBoardBallList>.instance.ballCount = CURRENTBALLS;
        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls = MATCHBALLS;
        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores = MATCHSCORES;
        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets = MATCHWICKETS;
        Singleton<GameData>.instance.currentBallNumber = CURRENTBALLNUMBER;
    }
    //Photon Removal  [PunRPC]
    public void RPC_CheckScore(int MATCHSCORES, int CURRENTBALLS, int MATCHWICKETS)                                                                                                                  //Photon Removal
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        // Reply with our own authoritative state so the reconnecting player is synced correctly.
        // Do NOT adopt the incoming values — they are the reconnecting player's stale local data.
        int _scores = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
        int _balls = Singleton<ScoreBoardBallList>.instance.ballCount;
        int _matchBalls = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls;
        int _wickets = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
        int _ballNum = Singleton<GameData>.instance.currentBallNumber;
        CricketNetworkManager.instance.CmdEqualScore(staticVariables.UserProfiledata.user._id, _scores, _balls, _matchBalls, _wickets, _ballNum);
        // Also update SyncVars so future reconnects get correct state immediately via Mirror auto-sync
        CricketNetworkManager.instance.CmdSyncGameState(_scores, _wickets, _balls, _matchBalls, _ballNum,
            CONTROLLER.StrikerIndex, CONTROLLER.NonStrikerIndex,
            Singleton<GameData>.instance.newBatsmanEntryIndex);
        CONTROLLER.UNEQUALSCORE = false;
        CricketNetworkManager.instance.CmdCallRebowl(staticVariables.UserProfiledata.user._id, false);
        if (Singleton<GameData>.instance.currentBallNumber == 5)
        {
            Singleton<GameData>.instance.CallCheckOverComplete();
        }
    }

    //Photon Removal    [PunRPC]
    public void RPC_CallRebowl(bool UneQual)
    {
        CONTROLLER.UNEQUALSCORE = UneQual;
        if (UneQual)
        {
            Singleton<GameData>.instance.RebowlLastBallMultiplayer();
        }
        else if (Singleton<GameData>.instance.currentBallNumber == 5)
        {
            Singleton<GameData>.instance.CallCheckOverComplete();
        }

    }

    //Photon Removal   [PunRPC]
    public void RPC_CheckScene(string SceneName, int BattingFirst)
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            if (BattingFirst == 0)
            {
                //Debug.Log("NOOOOOOOOOOO0");
                Singleton<TossPageTWO>.instance.ReCallChoseTo(false);
            }
            else
            {
                //Debug.Log("NOOOOOOOOOOO0");

                Singleton<TossPageTWO>.instance.ReCallChoseTo(true);
            }
        }
        else
        {
            //Debug.Log("SAMEMEEEE HHH");
        }
    }

    //Photon Removal   [PunRPC]
    private void RPC_SendOutCome(bool IsSame)
    {
        if (!IsSame)
        {
        }
    }

    public IEnumerator AnimateDots()
    {
        while (Singleton<GameData>.instance.waitingForOpponentPanel1.activeInHierarchy)
        {
            yield return new WaitForSeconds(0.5f);
            Singleton<GameData>.instance.waitingForOpponentText1.text += ".";
            //Debug.Log("ANIMATIONS : " + Singleton<GameModel>.instance.WaitForOppText1.text);
            if (Singleton<GameData>.instance.waitingForOpponentText1.text.Length > 15)
            {
                Singleton<GameData>.instance.waitingForOpponentText1.text = "Loading";
            }
        }
    }
}
