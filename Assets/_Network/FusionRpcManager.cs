//using AYellowpaper.SerializedCollections;
//using Fusion;
//using System;
//using System.Collections;
//using System.ComponentModel;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class FusionRpcManager : NetworkBehaviour,IPlayerJoined,IPlayerLeft
//{
//    public static event Action OnDisconnect, OnWin, OnLose, OnReconnect;
//    public static event Action<bool> OnAwaitingOpponent;
//    public static event Action<bool, string> OnServerDisconnected;
//    public static event Action<bool, string> OnServerDraw;
//    public Action<float> OnDisconnectionCountdown;

//    public enum ConnectionState
//    {
//        Connected,
//        SelfDisconnected,
//        OpponentDisconnected,
//        GameEnded
//    }

//    public ConnectionState currentState = ConnectionState.Connected;
//    private PlayerRef disconnectedPlayer;
//    private float disconnectionStartTime;
//    private bool isGameActive;
//    public PlayerRef opponent;

//    [SerializedDictionary("Player", "HeartBeat")]
//    public SerializedDictionary<PlayerRef, double> lastHeartbeatTime = new SerializedDictionary<PlayerRef, double>();

//    private Coroutine connectionMonitorCoroutine;
//    private Coroutine HeartBeatCoroutine;
//    [Header("Detection Settings")]
//    public float connectionWarningDelay = 2.5f;
//    public float officialDisconnectTimeout = 30f;
//    public float connectionCheckInterval = 0.5f;
//    public void SetOnServerDisconnected(bool state, string reason)
//    {
//        if (SceneManager.GetActiveScene().name == "Home")
//            PopupMessageManager.instance.ReconnectionPanelStatus(state);
//    }



//    int retrycount = 0;
//    public int sendRate = 30;
//    private void OnEnable()
//    {
//        NetworkRunner.CloudConnectionLost += OnCloudConnectionLost;

//        OnServerDisconnected += SetOnServerDisconnected;
//        "OnEnalbe".Show();
//    }

//    private void OnDisable()
//    {
//        OnServerDisconnected -= SetOnServerDisconnected;
//    }
//    private void OnCloudConnectionLost(NetworkRunner runner, ShutdownReason reason, bool reconnecting)
//    {
//        Debug.Log($"Cloud Connection Lost: {reason} (Reconnecting: {reconnecting})");
//        OnServerDisconnected.Invoke(reconnecting, "");
//        if (reconnecting) 
//        {
//            "isconnecting".Show();
//            StartCoroutine(WaitForReconnection(runner));
//        }
//        else
//        {
//            "Disconnected from server".Show();
//           // OnDisconnect?.Invoke();
//            DeclareDefeat("Disconnected");
//            //Reconnect();
//        }
//    }

//    private IEnumerator WaitForReconnection(NetworkRunner runner)
//    {
//        yield return new WaitUntil(() => runner.IsInSession);
//        OnServerDisconnected.Invoke(false, "");

//    }
//    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
//    public void RPC_RequestLoadSnooker()
//    {
//        Debug.Log("Client requested snooker scene load");
//        SnokerNetwork.IsMultiplayer = true;

//        // ✅ Only host actually loads the scene
//        if (Object.HasStateAuthority)
//        {
//            RPC_LoadSnookerForAll();
//        }
//    }
//    [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = true)]
//    private void RPC_LoadSnookerForAll()
//    {
//        Debug.Log("Loading Snooker Scene for all players");
//        SnokerNetwork.IsMultiplayer = true;
//        var sceneRef = SceneRef.FromIndex(11);
//        Runner.LoadScene(sceneRef);

//    }
//    public override void Spawned()
//    {
//        FusionNetwork._fusionRpcManager = this;
//        base.Spawned();
//        RPC_RequestLoadSnooker();
//        SnokerNetwork.IsMultiplayer = true;
//        DontDestroyOnLoad(gameObject);
//        StartGame();
//    }
//    #region Fusion Callbacks

//    public void PlayerJoined(PlayerRef player)
//    {
//        Debug.Log($"Player {player} joined session");
//        if (!isGameActive || FusionNetwork.HasWinnerBeenDeclared) return;

//        CheckGameStatusOnJoin();
//    }

//    public void PlayerLeft(PlayerRef player)
//    {
//        if (!isGameActive || FusionNetwork.HasWinnerBeenDeclared) return;

//        CheckGameStatusOnJoin();
//        Debug.Log("Player left session");
//         OnAwaitingOpponent?.Invoke(true);
//        // Player left intentionally
//        // DeclareWin("Opponent left the game");
//    }

  
//    public void CheckGameStatusOnJoin()
//    {
//        if (SessionDataManager.HasWinnerBeenDeclared())
//        {
//            FusionNetwork.HasWinnerBeenDeclared = true;
//            currentState = ConnectionState.GameEnded;

//            string reason = SessionDataManager.GetGameEndReason();
//            Debug.Log($"Game already ended when joining. Reason: {reason}");

//            bool wasWinner = SessionDataManager.WasLocalPlayerWinner(Runner.LocalPlayer);

//            if (wasWinner)
//            {
//                OnWin?.Invoke();
//            }
//            else
//            {
//                OnLose?.Invoke();
//            }
//        }
//    }

//    #endregion
//    public void StartGame()
//    {
//        if (isGameActive) return;

//        retrycount = 0;
//        isGameActive = true;
//        FusionNetwork.HasWinnerBeenDeclared = false;
//        currentState = ConnectionState.Connected;

//        InitializeHeartbeatTracking();
//        StartConnectionMonitoring();

//        Debug.Log("Game started with Fusion disconnection detection");
//    }

//    private void InitializeHeartbeatTracking()
//    {
//        lastHeartbeatTime.Clear();
//        double currentTime = Time.time;
//        SendMinimalHeartbeat();

//        foreach (var player in Runner.ActivePlayers)
//        {
//            lastHeartbeatTime[player] = currentTime;
//        }
//    }

//    private void StartConnectionMonitoring()
//    {
//        if (connectionMonitorCoroutine != null)
//            StopCoroutine(connectionMonitorCoroutine);

//        connectionMonitorCoroutine = StartCoroutine(MonitorAllConnections());

//        if (HeartBeatCoroutine != null)
//            StopCoroutine(HeartBeatCoroutine);
//        HeartBeatCoroutine = StartCoroutine(SendHeartBeatAfterDelay());
//    }

//    private IEnumerator MonitorAllConnections()
//    {
//        while (isGameActive && !FusionNetwork.HasWinnerBeenDeclared)
//        {
//            yield return new WaitForSecondsRealtime(connectionCheckInterval);
//            CheckOpponentConnections();
//        }
//    }

//    private IEnumerator SendHeartBeatAfterDelay()
//    {
//        while (isGameActive && !FusionNetwork.HasWinnerBeenDeclared)
//        {
//            yield return new WaitForSecondsRealtime(connectionCheckInterval);
//            if (currentState == ConnectionState.SelfDisconnected) continue;
//            if (Runner.GetPlayerRtt(Runner.LocalPlayer) * 1000f < 350)
//                SendMinimalHeartbeat();
//        }
//    }

//    private void SendMinimalHeartbeat()
//    {
//        RPC_SendHeartbeat(Runner.SimulationTime,Runner.LocalPlayer);
//    }

//    [Rpc(RpcSources.All, RpcTargets.All)]
//    private void RPC_SendHeartbeat(double timestamp,  PlayerRef sender,RpcInfo info = default)
//    {
//        Debug.Log(sender);
//        if (sender == PlayerRef.None)
//        {
//            Debug.Log("Player is None - ignoring reconnection event");
//            return;
//        }
//        if (lastHeartbeatTime.ContainsKey(sender))
//        {
//            lastHeartbeatTime[sender] = timestamp;
//        }
//        else
//        {
//            lastHeartbeatTime.Add(sender, timestamp);
//        }
//            HandleOpponentReconnection(sender);
//    }

//    public Root userModel;

//    public virtual void OnJoinSessionFailed(string message)
//    {
//        if (isGameActive)
//        {
//            ApiAndRoomManager._instance.GetChallengeStatus(ApiAndRoomManager._instance.winLoseChallengeId, success =>
//            {
//                userModel = JsonUtility.FromJson<Root>(success);
//                if (userModel.status)
//                {
//                    if (userModel.data.status.Contains("active") || userModel.data.status.Contains("draw"))
//                    {
//                        OnServerDraw?.Invoke(false, "");
//                    }
//                    else
//                    {
//                        OnServerDraw?.Invoke(true, "");
//                    }
//                }
//            }, failed => { });
//        }

//        ConstantsData_M.Log("Failed to join the session: " + message);
//        Debug.LogError($"Join session failed: {message}");
//    }
   
//    private void CheckOpponentConnections()
//    {
//        if (currentState == ConnectionState.SelfDisconnected) return;

//        double currentTime = Time.time;
//        if (Runner.ActivePlayers.Count() < 2)
//        {
//            HandleOpponentDisconnection(opponent);
//            return;
//        }

//        foreach (var player in Runner.ActivePlayers)
//        {
//            if (player == Runner.LocalPlayer) continue;

//            if (lastHeartbeatTime.ContainsKey(player))
//            {
//                double timeSinceHeartbeat = currentTime - lastHeartbeatTime[player];

//                       //    $"{timeSinceHeartbeat} > {connectionWarningDelay} = {timeSinceHeartbeat > connectionWarningDelay} : Ping {Runner.GetPlayerRtt(Runner.LocalPlayer) * 1000f}".Show();
//                if (timeSinceHeartbeat > connectionWarningDelay &&
//                    Runner.GetPlayerRtt(Runner.LocalPlayer) * 1000f > 350)
//                {
//                    HandleOpponentDisconnection(player);
//                    break;
//                }
//            }
//        }
//    }

//    private void HandleOpponentDisconnection(PlayerRef player)
//    {
//        if (currentState != ConnectionState.Connected || FusionNetwork.HasWinnerBeenDeclared || player == PlayerRef.None) return;

//        Debug.Log($"Opponent disconnection detected: {player}");

//        currentState = ConnectionState.OpponentDisconnected;
//        disconnectedPlayer = player;
//        disconnectionStartTime = Time.time;
//        OnAwaitingOpponent?.Invoke(true);
//    }

//    private void HandleOpponentReconnection(PlayerRef player)
//    {
//        if (disconnectedPlayer != player && currentState != ConnectionState.OpponentDisconnected)
//            return;

//        Debug.Log($"Opponent reconnected: {player}");

//        currentState = ConnectionState.Connected;
//        disconnectedPlayer = default;
//        OnAwaitingOpponent?.Invoke(false);
//        CheckGameStatusOnJoin();
//    }
//    #region Game End

//    public void DeclareDefeat(string reason)
//    {
//        if (FusionNetwork.HasWinnerBeenDeclared) return;

//        // Check session properties first
//        if (SessionDataManager.HasWinnerBeenDeclared())
//        {
//            FusionNetwork.HasWinnerBeenDeclared = true;
//            Cleanup();
//            return;
//        }

//        FusionNetwork.HasWinnerBeenDeclared = true;
//        currentState = ConnectionState.GameEnded;
//        Debug.Log($"Defeat! Reason: {reason}");

//        SessionDataManager.SetGameEnded(reason, false);
//        Cleanup();
//        OnLose?.Invoke();
//    }

//    public void DeclareWin(string reason)
//    {
//        if (FusionNetwork.HasWinnerBeenDeclared) return;

//        if (SessionDataManager.HasWinnerBeenDeclared())
//        {
//            FusionNetwork.HasWinnerBeenDeclared = true;
//            OnLose.Invoke();
//            Cleanup();
//            return;
//        }

//        FusionNetwork.HasWinnerBeenDeclared = true;
//        currentState = ConnectionState.GameEnded;
//        Debug.Log($"Victory! Reason: {reason}");

//        SessionDataManager.SetGameEnded(reason, true);
//        Cleanup();
//        OnWin?.Invoke();
//    }


//    #endregion

//    #region Cleanup

//    public void Cleanup()
//    {
//        isGameActive = false;

//        if (connectionMonitorCoroutine != null)
//        {
//            StopCoroutine(connectionMonitorCoroutine);
//            connectionMonitorCoroutine = null;
//        }
//        if (HeartBeatCoroutine != null)
//        {
//            StopCoroutine(HeartBeatCoroutine);
//            HeartBeatCoroutine = null;
//        }
//        disconnectedPlayer = default;
//        OnAwaitingOpponent?.Invoke(false);
//        CheckGameStatusOnJoin();
//    }

//    void OnDestroy()
//    {
//        Cleanup();
//    }

//    #endregion

//}