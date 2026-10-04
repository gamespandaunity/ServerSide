using BallPool;
using Mirror;
using Snake_Ladder;
using System.Collections;
using System.Collections.Generic;
using Twelve;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExtensions;


public enum DisconnectReason
{
    UserGoesBackground,
    UserRequested,      // User clicked disconnect/leave button
    ServerShutdown,     // Server closed/terminated
    ConnectionTimeout,  // Network timeout
    ApplicationPause,   // Kicked by server
    ApplicationQuit,    // Game closing
    SceneTransition,   // Changing scenes intentionally
    Connected
}

public class MirrorNetwork : NetworkManager
{
    [Header("Game Scenes")]
    public List<string> gameScenes = new List<string>();
    private Dictionary<string, System.Action> sceneInitMethods = new Dictionary<string, System.Action>();

    public static MirrorNetwork Instance;
    public EdgegapAPIClient edgegapAPIClient;

    public static System.Action<int> OnPlayerCountChanged;
    public static System.Action<string> OnSceneChangeStarting;
    public static System.Action<string> OnGameStarted;
    public static System.Action<bool> OnGameStateChanged;
    public static System.Action<string> OnWinCall;

    public static System.Action<bool> OnDirectWinWithoutInternet;

    public GameObject networkGameManagerPrefab;
    public static string winnerID = null;
    public bool isMasterClient;
    public string mirrorRequestId;

    private Coroutine shutdownCoroutine;
    public static string serverAddress;

    /// <summary>
    /// Entry point the join flow uses to hand Mirror a NEW match's server address. The client repo's copy
    /// also resets its per-match pre-game retry budget here; this copy has no such budget, so it only
    /// assigns — kept as the same call so both repos share one join entry point.
    /// </summary>
    public static void BeginNewMatchJoin(string address)
    {
        serverAddress = address;
    }

    [Header("Reconnection Settings")]
    [SerializeField] private int maxAttempts = 5;
    [SerializeField] private float retryDelay = 2f;
    [SerializeField] private float connectionTimeout = 10f;

    private Coroutine reconnectCoroutine;

    // Set the moment a reconnect is REQUESTED, not when its coroutine starts. reconnectCoroutine is
    // assigned only after the async GetChallengeStatus round-trip in ReconnectAfterDisconnect, so for
    // the whole request window the handle is still null and the StopCoroutine check there cannot see
    // a duplicate. See ReconnectAfterDisconnect for what the duplicate actually costs.
    private bool _reconnectRequestInFlight;

    private Coroutine _reconnectGuardWatchdog;

    // True for exactly as long as an AttemptReconnect coroutine is alive. Deliberately NOT the
    // reconnectCoroutine handle: the isGameFinished exit at the top of AttemptReconnect runs BEFORE any
    // yield, so StartCoroutine returns an already-finished coroutine and the caller's
    // `reconnectCoroutine = StartCoroutine(...)` assignment lands AFTER the body has exited — leaving the
    // handle non-null on a dead coroutine, which would block every later reconnect. A bool set before the
    // start and cleared inside the body has no such ordering hole.
    private bool _reconnectAttemptRunning;

    // A disconnect that arrived while a previous reconnect request or attempt was still running. It must
    // be DEFERRED, never dropped: the first version of this guard simply returned, and when the in-flight
    // request then resolved to "no reconnection attempt" the newer, genuine disconnect was gone with
    // nothing left to act on — the client sat disconnected forever ("now reconnection not happening",
    // build 202608051858). Suppressing a duplicate of the SAME drop is the goal; swallowing the NEXT
    // one is strictly worse than the double attempt this guard exists to prevent.
    private bool _reconnectRequestedWhileInFlight;

    // The reason THIS pass is acting on, snapshotted when the pass starts rather than read again inside the
    // status callback. currentDisconnectReason is global and mutable, and the decision point sits on the far
    // side of an HTTP round-trip: on a slow network the 5 s ResetDisconnectReason timer armed by the PREVIOUS
    // pass lands inside that window and rewrites it to Connected, so the callback concluded "nothing was
    // disconnected" about a drop it was dispatched to handle -- "No reconnection attempt - Reason: Connected"
    // immediately after "Mirror Client Disconnect - Reason: ApplicationPause" in the 17-08 report, with the
    // player left on a dead screen. A pass must decide on the reason it was given.
    private DisconnectReason _passDisconnectReason = DisconnectReason.Connected;

    // Handle for that timer, so a new pass can cancel a stale one instead of racing it.
    private Coroutine _resetReasonCoroutine;

    // Single release point for the guard, so no exit path can forget the deferred request above.
    private void ReleaseReconnectGuard()
    {
        _reconnectRequestInFlight = false;
        _reconnectAttemptRunning = false;
        // Every pass ends here, so this is the one place that can guarantee the delayed reason-reset is
        // POSTPONED rather than DROPPED. ArmDisconnectReasonReset no-ops while a pass is live, so without a
        // re-arm here a reason left at ApplicationPause would be permanent -- and the pre-game retry at the
        // top of ReconnectAfterDisconnect only runs when the reason is Connected, so it would silently stop
        // working for the rest of the session ("stuck at the start", with no retry line in the log).
        // In the re-dispatch case below the new pass arms on its own release, so the chain is never broken.
        if (!_reconnectRequestedWhileInFlight)
        {
            ArmDisconnectReasonReset();
            return;
        }
        _reconnectRequestedWhileInFlight = false;
        if (NetworkClient.isConnected)
        {
            Debug.Log("[Mirror] A disconnect was deferred behind the previous reconnect, but the client is connected again — nothing to do.");
            ArmDisconnectReasonReset();
            return;
        }
        Debug.Log("[Mirror] Handling the disconnect that arrived while the previous reconnect was still resolving.");
        ReconnectAfterDisconnect();
    }

    // Deadline for the guard. Only fires when the status callback never came back at all — on every
    // normal path the guard is already released and reconnectCoroutine is running, so this exits
    // without touching anything.
    private IEnumerator ReleaseReconnectGuardIfStalled()
    {
        yield return new WaitForSeconds(20f);
        _reconnectGuardWatchdog = null;
        // Gated on _reconnectAttemptRunning, not the coroutine handle — same ordering hole described on
        // that field. If an attempt is genuinely running it owns the reconnect and must not be disturbed.
        if (_reconnectRequestInFlight && !_reconnectAttemptRunning)
        {
            Debug.LogWarning("[Mirror] Reconnect status call never answered within 20s — releasing the duplicate-reconnect guard so a later drop can still reconnect.");
            ReleaseReconnectGuard();
        }
    }
    public DisconnectReason currentDisconnectReason = DisconnectReason.Connected;
    public DisconnectReason OpponentDisconnectReason = DisconnectReason.Connected;

    /// <summary>
    /// True only after both players are in the game scene.
    /// Reconnect logic must NOT run before this is set — UNLESS
    /// Mirror itself is doing a server-driven scene transition.
    /// </summary>
    public static bool hasJoinedGame = false;
    public static GameObject activeLoadingPanel;

    /// <summary>
    /// True while Mirror is transitioning us to the game scene via ServerChangeScene.
    /// During this window, OnStopClient fires but it's NOT a real disconnect.
    /// </summary>
    public bool isMirrorSceneTransitioning = false;

    public override void Awake()
    {
        base.Awake();
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

    }

    public override void Start()
    {
        base.Start();

        // Subscribe to transport events for automatic disconnect detection
        if (transport != null)
        {
            transport.OnClientError += OnTransportError;
        }
    }

    private void OnDestroy()
    {
        if (transport != null)
        {
            transport.OnClientError -= OnTransportError;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (!NetworkClient.active) return;
        if (isMirrorSceneTransitioning) return;
        if (paused)
        {
            currentDisconnectReason = DisconnectReason.UserGoesBackground;
            if (NetworkGameManager.Instance)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)currentDisconnectReason);
            Debug.Log("[Mirror] App backgrounded — sent UserGoesBackground to server.");
        }
        else
        {
            if (currentDisconnectReason == DisconnectReason.UserGoesBackground)
            {
                if (NetworkGameManager.Instance)
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)DisconnectReason.Connected);
                currentDisconnectReason = DisconnectReason.Connected;
            }
            Debug.Log("[Mirror] App resumed — notified server, cleared UserGoesBackground flag.");
        }
    }

    private void OnApplicationQuit()
    {
        // Mark as intentional disconnect when quitting
        currentDisconnectReason = DisconnectReason.ApplicationQuit;
        if (NetworkGameManager.Instance)
            NetworkGameManager.Instance.CmdSendDisconnectType((int)currentDisconnectReason);
        this.Delay(1, () => NetworkManager.singleton.StopClient());
    }

    // ============ PUBLIC DISCONNECT METHODS ============

    /// <summary>
    /// Call this when user clicks disconnect/leave game button
    /// </summary>
    public void DisconnectByUserRequest()
    {
        Debug.Log("User requested disconnect");
        currentDisconnectReason = DisconnectReason.UserRequested;
        StopClient();
    }

    /// <summary>
    /// Call this when changing scenes intentionally
    /// </summary>
    public void DisconnectForSceneChange(string sceneName)
    {
        Debug.Log($"Disconnecting for scene change to {sceneName}");
        currentDisconnectReason = DisconnectReason.SceneTransition;
        StopClient();
    }



    // ============ TRANSPORT ERROR HANDLER ============

    private void OnTransportError(TransportError error, string reason)
    {
        Debug.LogError($"Transport Error: {error} - {reason}");
        // Transport errors are always unintentional
        currentDisconnectReason = DisconnectReason.ConnectionTimeout;
    }

    // ============ SERVER METHODS ============
    Dictionary<string, int> keyValuePairsPlayerReconnection = new Dictionary<string, int>();

    public override void OnPlayerReconnected(NetworkConnectionToClient conn)
    {
        base.OnPlayerReconnected(conn);

        // Player who reconnected
        // Resolve the reconnecting player from data the SERVER actually owns.
        //
        // MirrorPlayerPrefab.playerId is a [SyncVar] and the only write to it is in RunLocalPlayerSetup —
        // on the player's OWN client. SyncVars flow server -> client, so a client-side write never reaches
        // the server and, on a dedicated build, that field stays empty forever. This DelayUntil was waiting
        // on it, so the whole body below — the reconnect counter AND the five-attempt limit — has simply
        // never run in production. That is why a tester could disconnect seven times and keep playing while
        // the panel still promised five: the limit was not being enforced at all, not set to the wrong
        // number. creatorData/joinerData are written by the server in AssignPlayerRole, and CreatorRef /
        // JoinerRef are re-pointed at the live connection on every reconnect before its early return, so
        // the pair identifies the reconnecting player reliably. The DelayUntil just below this one already
        // reads creatorData/joinerData for the same reason.
        string ResolveReconnectingId() =>
              conn == NetworkGameManager.Instance.CreatorRef ? NetworkGameManager.Instance.creatorData?.playerId
            : conn == NetworkGameManager.Instance.JoinerRef  ? NetworkGameManager.Instance.joinerData?.playerId
            : null;

        this.DelayUntil(() => !string.IsNullOrEmpty(ResolveReconnectingId()), () =>
        {
            string reconnectingPlayerId = ResolveReconnectingId();

            // Increment reconnect count
            if (!keyValuePairsPlayerReconnection.TryGetValue(reconnectingPlayerId, out int count))
                count = 0;
            if (StickManager.instance != null)
                StickManager.instance.AssignStickAuthority();

            keyValuePairsPlayerReconnection[reconnectingPlayerId] = count + 1;
            Debug.Log(reconnectingPlayerId + " = Recoonect Attpemt Overall :" + keyValuePairsPlayerReconnection[reconnectingPlayerId]);
            string winnerId = NetworkGameManager.Instance.joinerData.playerId == reconnectingPlayerId ? NetworkGameManager.Instance.creatorData.playerId : NetworkGameManager.Instance.joinerData.playerId;

            // Snooker keeps its original 5-attempt limit; all other games declare the
            // opponent the winner on the player's 3rd reconnect attempt.
            bool isSnooker = StickManager.instance != null;
            int reconnectAttempts = keyValuePairsPlayerReconnection[reconnectingPlayerId];
            bool reconnectLimitReached = isSnooker ? reconnectAttempts > 6 : reconnectAttempts >= 5;

            if (reconnectLimitReached)
            {
                Debug.LogWarning($"Player {reconnectingPlayerId} exceeded max reconnect attempts. Declaring opponent {winnerId} as winner.");
                // Find opponent: any player except the reconnecting one
                Debug.Log(winnerId);
                // if (isSnooker)
                // {
                //     Debug.Log("Setting disconnect winner in StickManager: " + winnerId);
                //     // Snooker: StickManager owns the win flow
                //     StickManager.instance.GameWinnerId(winnerId, true);
                // }
                // else
                // {
                Debug.Log("Setting disconnect winner in NetworkGameManager: " + winnerId);
                // All other games: use the generic disconnect-win handler in NetworkGameManager.
                // SetDisconnectWinner starts CheckGameStatus() which re-fires the win Rpc
                // every 2 s for 60 s so reconnecting players never miss the win message.
                // Pass the freshly-reconnected conn so a TargetRpc also goes directly to them.
                if (NetworkGameManager.Instance != null)
                    NetworkGameManager.Instance.SetDisconnectWinner(winnerId, conn, NetworkGameManager.DISCONNECT_WIN_RECONNECT_LIMIT);
                //  }

            }
            else
            {
                Debug.Log($"Player {reconnectingPlayerId} reconnect attempt #{keyValuePairsPlayerReconnection[reconnectingPlayerId]} - allowing to rejoin game.");

                // Reconnect bypasses OnServerAddPlayer, so currentPlayerCount is never restored to 2
                // and the remaining player's "Waiting for opponent" timer is never told to stop.
                // Restore the authoritative count (this re-triggers the OnChangePlayer resume chain)
                // and explicitly broadcast a waiting-panel close as a belt-and-suspenders stop.
                if (NetworkGameManager.Instance != null && numPlayers >= 2)
                {
                    NetworkGameManager.Instance.currentPlayerCount = numPlayers;
                    NetworkGameManager.Instance.RpcShowWaitingPanel(false);
                }

                if (numPlayers == 1)
                {
                    ApiAndRoomManager._instance.GetChallengeStatus(ApiAndRoomManager._instance.winLoseChallengeId, success =>
                    {
                        userModel = JsonUtility.FromJson<Root>(success);
                        success.Show("Status");
                        if (userModel.status)
                        {
                            if (userModel.data.status.Contains("active") /*|| userModel.data.status.Contains("expired") */|| userModel.data.status.Contains("draw") || userModel.data.status.Contains("pending"))
                            {
                                Debug.Log("Challenge is still active, starting reconnect timer for player " + reconnectingPlayerId);
                                PopupMessageManager.instance.waitingPanel.StartTimer();

                            }
                            else
                            {
                                Debug.LogWarning("Challenge is no longer active, not starting reconnect timer.");
                                if (NetworkGameManager.Instance != null)
                                {
                                    NetworkGameManager.Instance.SetDisconnectWinner(winnerId, conn);
                                    NetworkGameManager.Instance.RpcSendDisconnectType(conn, 0);
                                }
                            }
                        }
                    });
                }
            }
        });

    }
    // Transaction already reported to /session/play/ by this process — see OnServerAddPlayer.
    private string _sessionPlayReportedFor;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        NetworkGameManager.Instance.currentPlayerCount = numPlayers;
        NetworkGameManager.Instance.ServerCancelAbandonWatch("a player joined or rejoined");
        Debug.Log("Changing Scene~!" + numPlayers);
        if (numPlayers == 2)
        {
            hasJoinedGame = true; // both players joined — reconnect logic now allowed
            NetworkGameManager.Instance.RpcShowWaitingPanel(false);
            this.DelayUntil(() => !string.IsNullOrEmpty(NetworkGameManager.Instance.creatorData.playerId) && !string.IsNullOrEmpty(NetworkGameManager.Instance.joinerData.playerId), () =>
            {
                NetworkGameManager.Instance?.RpcAnnouncePlayerJoined(conn.address, numPlayers);

                // Mark the session as PLAYED, from the server, once both players are actually in.
                //
                // This used to ride on OnEnterSnookerFusion, which is called from the body of
                // RpcAnnouncePlayerJoined — a [ClientRpc]. Mirror never runs a ClientRpc body on a
                // dedicated server, so once the call moved off the player build nothing was sending
                // /session/play/ at all. Here we are already on the server, already inside the
                // numPlayers == 2 branch, and already past the DelayUntil that waits for BOTH player ids
                // to be populated — which is exactly the state StartChallenge needs, since it sends
                // winLoseChallengeId.
                //
                // Latched on the transaction id rather than a bool: a rejoin re-enters this branch and
                // must not re-report the same session (the old client-side caller fired on every
                // player-count change and the 01-09 logs show it running nine times in one match), while
                // a genuinely new match carries a new id and reports once.
                string _playTxn = NetworkGameManager.Instance != null ? NetworkGameManager.Instance.transactionId : null;
                if (!string.IsNullOrEmpty(_playTxn) && _sessionPlayReportedFor != _playTxn
                    && ApiAndRoomManager._instance != null)
                {
                    _sessionPlayReportedFor = _playTxn;
                    Debug.Log($"[Settlement] Both players in — reporting session {_playTxn} as played.");
                    ApiAndRoomManager._instance.StartChallenge();
                }

                if (SceneManager.GetActiveScene().name == "Home" ||
                   SceneManager.GetActiveScene().name == "LoadingScene")
                {
                    Debug.Log("GameIDSceneLoad" + NetworkGameManager.Instance.currentGameId);

                    switch (NetworkGameManager.Instance.currentGameId)
                    {
                        case 1:
                            Debug.Log("AightBallPool");
                            ServerChangeScene("AightBallPool");
                            break;
                        case 3:
                            Debug.Log("TeenPatti");
                            MatchHandler.CurrentMatch = MatchHandler.MATCH.TeenPatti;

                            ServerChangeScene("GameplayTeenPatti");
                            break;
                        case 2:
                            Debug.Log("Ludo");
                            ServerChangeScene("LudoGameScene");
                            break;
                        case 4:
                            ServerChangeScene("CarromOnline");
                            break;
                        case 5:
                            Debug.Log("Roulette");
                            ServerChangeScene("Roulette_Ready");
                            break;
                        case 6:
                            Debug.Log("Snaker");
                            ServerChangeScene("SnakeMultiplayer");
                            break;
                        case 7:
                            ServerChangeScene("12OnlineGameScene");
                            break;
                        case 8:
                            MatchHandler.CurrentMatch = MatchHandler.MATCH.Poker;
                            ServerChangeScene("PokerGameplay");
                            break;
                        case 9:
                            Debug.Log("BigWheel");
                            ServerChangeScene("SnokkerMultiplayer");
                            break;
                        case 11:

                            ServerChangeScene("Map2");
                            break;
                        case 16:
                            Debug.Log("Horse");
                            ServerChangeScene("HorseRacingMultiPlayer");
                            break;
                        case 13:
                            ServerChangeScene("MainMenu");
                            break;
                        case 14:
                            //   HR_MainMenuHandler.Instance.SelectMode(0);
                            PlayerPrefs.SetInt("Multiplayer", 1);
                            ServerChangeScene("HighwayNight");
                            break;
                        case 15:
                            Debug.Log("Snooker");

                            ServerChangeScene("SnokkerMultiplayer");
                            break;
                        default:
                            Debug.LogError("Unknown game ID, cannot change scene");
                            break;
                    }
                    Debug.Log("Changing Scene~!");
                }
            }
            );
        }
    }
    public void SceneChange(string scene)
    {
        ServerChangeScene(scene);
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server Started!");
        SnokerNetwork.IsMultiplayer = true;
        //  PlayerPrefs.SetString(LocalSettings.TotalChips, staticVariables.isgoldcoins ? ApiAndRoomManager.LastFetchedCoins.data.gold_balance.ToString() : ApiAndRoomManager.LastFetchedCoins.data.silver_balance.ToString());// chipsBigint.ToString());
        //  PlayerPrefs.Save();
        LocalSettings.SetPlayername(staticVariables.userNickName);
        isMasterClient = true;
        //Auto-spawn NetworkGameManager if it doesn't exist
        if (NetworkGameManager.Instance == null)
        {
            GameObject gameManagerGO = Instantiate(networkGameManagerPrefab);
            Debug.Log("Adding Network Manager");
            NetworkServer.Spawn(gameManagerGO);
        }
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        // ── Scene-readiness gate ───────────────────────────────────────────────
        // Edgegap marks the deployment "ready" the moment the KCP port opens,
        // which can be BEFORE the server has finished its LoginScene init and
        // changed to the Home scene.  If a client connects during that window,
        // Mirror spawns their PlayerPrefab in LoginScene; the subsequent
        // ServerChangeScene("Home") then destroys it, making every Command from
        // that client fail with "Spawned object not found".
        //
        // Fix: reject the connection immediately — Mirror will call OnClientDisconnect
        // on the joiner's side, where we retry automatically after 3 s.
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "LoginScene" || currentScene == "LoadingScene")
        {
            Debug.LogWarning($"[Server] Not ready yet (still in '{currentScene}') — " +
                             $"rejecting conn={conn.connectionId}. Client will retry.");
            conn.Disconnect();
            return;
        }

        base.OnServerConnect(conn);

        // If someone connects, cancel any pending shutdown
        if (shutdownCoroutine != null)
        {
            StopCoroutine(shutdownCoroutine);
            shutdownCoroutine = null;
            Debug.Log("Client joined, shutdown canceled.");
        }
    }

    // All multiplayer game scenes — server sends customHandling=true so the client
    // owns its own loading: tries Build Settings first, falls back to Addressables.
    // This future-proofs the system: new scenes added here automatically get
    // the fallback logic on both sides without touching Mirror internals.
    private static readonly HashSet<string> _clientManagedScenes = new HashSet<string>
    {
        "AightBallPool", "LudoGameScene", "GameplayTeenPatti", "CarromOnline",
        "Roulette_Ready", "SnakeMultiplayer", "12OnlineGameScene", "PokerGameplay",
        "SnokkerMultiplayer", "Map2", "HorseRacingMultiPlayer", "HighwayNight" ,"MainMenu", "Ground"
    };

    public override void ServerChangeScene(string newSceneName)
    {
        if (_clientManagedScenes.Contains(newSceneName))
        {
            // Guard: already loading this scene
            if (NetworkServer.isLoadingScene && newSceneName == networkSceneName)
            {
                Debug.Log($"[Mirror] ServerChangeScene: already loading {newSceneName}");
                return;
            }

            // Standard Mirror server-side setup
            NetworkServer.SetAllClientsNotReady();
            networkSceneName = newSceneName;
            OnServerChangeScene(newSceneName);

            // Server loads normally (scene IS in server Build Settings)
            NetworkServer.isLoadingScene = true;
            loadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);

            // Tell clients: use customHandling=true → client checks Build Settings first,
            // falls back to Addressables if not found there
            if (NetworkServer.active)
                NetworkServer.SendToAll(new SceneMessage { sceneName = newSceneName, customHandling = true });

            Debug.Log($"[Mirror] ServerChangeScene (Addressable) → {newSceneName}");
        }
        else
        {
            base.ServerChangeScene(newSceneName);
        }
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        // Mark that we're in a Mirror-initiated scene transition.
        // OnStopClient may fire during this window — it's NOT a real disconnect.
        if (gameScenes.Contains(newSceneName))
        {
            isMirrorSceneTransitioning = true;
            hasJoinedGame = false; // will be set true in OnClientSceneChanged
            Debug.Log($"[Mirror] Scene transition starting → '{newSceneName}'");
        }
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (gameScenes.Contains(sceneName))
        {
            isMirrorSceneTransitioning = false;
            hasJoinedGame = true;
            Debug.Log($"[Mirror] Client scene changed to '{sceneName}' — game joined, reconnect enabled");
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        "Disconnected".Show();

        List<NetworkIdentity> ownedObjects = new List<NetworkIdentity>(conn.owned);

        foreach (NetworkIdentity identity in ownedObjects)
        {
            Debug.Log(identity.name);
            if (identity != null)
            {
                // Check if this object should persist
                if (identity.TryGetComponent<PersistentObject>(out var persistent))
                {
                    "Authority Removed from disconnect".Show();
                    identity.RemoveClientAuthority();
                }
            }
        }

        base.OnServerDisconnect(conn);
        currentDisconnectReason.ToString().Show("Disconnect Reason");
        if (currentDisconnectReason == DisconnectReason.UserGoesBackground)
            NetworkGameManager.Instance.RpcSendDisconnectType(conn, (int)currentDisconnectReason);
        numPlayers.Show("DisConnected");

        NetworkGameManager.Instance.currentPlayerCount = numPlayers;

        // A player who drops and does not come back must still settle the match. NetworkGameManager
        // decides whether this particular disconnect qualifies and does nothing when it does not.
        if (NetworkServer.active && NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.ServerBeginAbandonWatch();

        // If a player drops before both have joined, tell the remaining client to show the waiting panel.
        if (!hasJoinedGame && numPlayers < 2 && NetworkGameManager.Instance != null)
        {
            Debug.Log("[Mirror] Server: player disconnected before game started — notifying remaining client.");
            NetworkGameManager.Instance.RpcShowWaitingPanel(true);
        }
        //if (NetworkServer.active && numPlayers == 0 && shutdownCoroutine == null)
        //{
        //    shutdownCoroutine = StartCoroutine(ShutdownAfterDelay());
        //}
    }
    public void cleanup(bool Rematch = false)
    {
        currentDisconnectReason = DisconnectReason.Connected;
        OpponentDisconnectReason = DisconnectReason.Connected;
        Debug.Log("cleanup" + Rematch);

        hasJoinedGame = false; // reset for next game session
        isMirrorSceneTransitioning = false;

        // Reset the authoritative player count so a stale "2" from this match can't make the
        // waiting-panel self-cancel guard close a fresh waiting panel in the next game.
        // Server-only: currentPlayerCount is a SyncVar and may only be written by the server.
        if (NetworkServer.active && NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.currentPlayerCount = 0;

        keyValuePairsPlayerReconnection = new Dictionary<string, int>();
        SnokerNetwork.isTossDone = false;
        SnokerGameManager.bGameOver = false;

        SnokerGameManager.bBallInHand = false;
        SnokerGameManager.snookerRedPottedCount = 0;
        SnokerGameManager.snookerTargetBall = SnookerTargetType.Red;
        mainScript.foulInThisTurn = false;
        keyValuePairsPlayerReconnection = new Dictionary<string, int>();
        Win8Ball.EightisFinishTriggered = false;
        //
        mainScript.curScreen = "MainMenu";
        mainScript.spinSetOn = false;
        mainScript.dtTimeAtLastFrame = 0f;
        mainScript.dtTimeAtCurrentFrame = 0f;
        mainScript.deltaTimeCustom = 0f;
        mainScript.bAnimateGui = true;
        mainScript.btnAnimValue = 1f;
        mainScript.gameNamePos = 0f;
        //Ludo
        GameGUIController.currentPlayerIndex = -1;
        //Snake
        GameControllerNew.myPlayerNumber = -1;
        //12bead
        GamePlayControllerTwelve.isOnlineMultiplayer = false;
        GamePlayControllerTwelve.currentPlayerTurn = Twelve.PLAYERS.EMPTY;
        GamePlayControllerTwelve.myId = 0;
        GamePlayControllerTwelve.aiId = 0;
        GamePlayControllerTwelve.beadsCreated = false;
        TwelveBeadNetworkManager.isTossDone = false;

        //Highway
        // ResultManagerForHighwayRacing.isFinishTriggered = false;
        // ResultManagerForHighwayRacing.isHighWayFinish = false;
        HR_NetworkManager.hasHighWayGameStarted = false;
        HR_NetworkManager.HighWayCarData.Clear();
        //Carrom
        BEKStudio.GameController.currentAwayScore = 0;
        BEKStudio.GameController.currentHomeScore = 0;
        // MultiPlayerGameManagerTwelve.instance.playerLoadedStatus.Clear();
        //Horse
        // CarRace.MultiPlayerGameManager.PlayersCarsData = new Dictionary<string, CarRace.MultiPlayerGameManager.CarRecord>();
        CarRace.MultiPlayerGameManager.PlayersCarsData.Clear();
        CarRace.MultiPlayerGameManager.gameStarted = false;
        HorseMirrorGameManager.PlayersHorseData = new Dictionary<string, HorseMirrorGameManager.HorseRecord>();
        // HR_NetworkManager.HighWayCarData.Clear();

        ReadyToGO.hasGameStarted = false;
        //   HorseAnimationSync.ControlsEnabled = false;
        //  ResultManagerForCar.IsGameWin = false;
        //8balpool
        GameUIController.EightballPoolLeaveGame = true;
        if (!Rematch)
        {
            PlayerPrefs.SetString("ServerRequest_id", "");

            // Clear stale Edgegap session state so entries from this game
            // don't interfere when a different game (e.g. Ludo after Poker) connects.
            edgegapAPIClient?.ClearSession();

            StopClient();
            //  edgegapAPIClient.CleanupServer(mirrorRequestId);
        }
    }
    public bool IsEditor;
    private IEnumerator ShutdownAfterDelay()
    {
        Debug.Log("No players connected. Server will shut down in 2 minutes if no one rejoins...");
        string serverid = NetworkGameManager.Instance.currentServerRequestId;
        yield return new WaitForSeconds(120f);

        // Double-check before quitting
        if (numPlayers == 0)
        {
            edgegapAPIClient.CleanupServer(serverid);
        }

        shutdownCoroutine = null;
    }

    // ============ CLIENT DISCONNECT & RECONNECT ============
    public Root userModel;
    public override void OnStopClient()
    {
        Debug.Log($"Mirror Client Disconnect - Reason: {currentDisconnectReason}");
        if (OpponentDisconnectReason == DisconnectReason.ApplicationQuit) { return; }
        // Stop any existing reconnection attemptcv
        ReconnectAfterDisconnect();

    }
    public void ReconnectAfterDisconnect()
    {
        // An attempt is already running — LET IT FINISH. This used to StopCoroutine it and start over,
        // which is why a reconnect could never complete once disconnects started arriving in a stream:
        // AttemptReconnect gets maxAttempts tries with a 10s timeout each, but every fresh disconnect
        // killed it and restarted the counter, so it never got past attempt 2/5 and never reached
        // OnReconnectFailed either. The 06-08 log shows the whole shape — three clean reconnects, then
        // "[Reconnect Attempt 1/5]" restarting at lines 14822, 14888, 16835, 18560, 21488, 24893, 24956,
        // 25019 ... 36 attempts, 3 successes, and the player left sitting on a live-looking screen with
        // no reconnect and no failure.
        //
        // Defer instead, exactly like the request guard below: the running attempt owns the reconnect,
        // and ReleaseReconnectGuard re-dispatches at its success or final-failure exit if we are still
        // disconnected.
        if (_reconnectAttemptRunning)
        {
            _reconnectRequestedWhileInFlight = true;
            Debug.Log("[Mirror] A reconnect attempt is already running — deferring this disconnect rather than restarting it.");
            return;
        }

        // Never attempt reconnect if we haven't actually entered a game yet —
        // UNLESS Mirror is doing a server-driven scene transition (which temporarily
        // disconnects the client). In that case, Mirror handles the reconnect itself.
        if (!hasJoinedGame && !isMirrorSceneTransitioning)
        {
            Debug.Log("[Mirror] OnStopClient fired before game started — skipping reconnect logic.");
            // During rematch, a disconnect before joining means the server dropped.
            // Destroy the loading panel and show the waiting panel so the user knows.
            if (!string.IsNullOrEmpty(serverAddress))
            {
                GameUIController.EightballPoolLeaveGame = false;
                if (activeLoadingPanel != null) { Destroy(activeLoadingPanel); activeLoadingPanel = null; }
                PopupMessageManager.instance?.SetWaitingPanel(true);
            }
            return;
        }
        if (isMirrorSceneTransitioning)
        {
            Debug.Log("[Mirror] OnStopClient during scene transition — Mirror will handle reconnect.");
            return;
        }

        // ── One reconnect at a time ──────────────────────────────────────────────────────────
        // Placed after every early return above, and BEFORE the async status request below,
        // because the request window is exactly where the duplicate slips through: two disconnect
        // events fire two GetChallengeStatus calls while reconnectCoroutine is still null, both
        // callbacks then StartCoroutine, and the second overwrites the handle while the first
        // coroutine keeps running orphaned. The 05-08 reports show this as two "[Reconnect Attempt
        // 1/5]" lines for a single drop — never 1/5 followed by 2/5, which is what a real retry
        // would look like.
        //
        // The cost is not cosmetic. The second attempt calls StopClient() -> NetworkClient.Shutdown(),
        // which sets NetworkClient.isLoadingScene = false unconditionally — in the middle of the
        // FIRST attempt's Ground load. The client message pump reopens, the server's spawn burst is
        // processed before the scene's objects exist, and the scene identity is never bound:
        // "Spawn scene object not found for E83B315E804ABBBD ... netId=2". That error appears in
        // exactly the 5 of 20 reports carrying the double connect and in none of the other 15.
        // A client that loses that identity keeps playing with no SyncVars and no RPC target on it,
        // which is what "dono screens alag" looks like from the outside.
        if (_reconnectRequestInFlight)
        {
            // DEFER, do not drop — see _reconnectRequestedWhileInFlight. If this is just the second
            // event of one drop the in-flight request handles it and the deferred copy is discarded
            // once we are connected again; if it is a genuinely new drop, ReleaseReconnectGuard
            // re-dispatches it the moment the current request stops being able to.
            _reconnectRequestedWhileInFlight = true;
            Debug.Log("[Mirror] A reconnect is already in progress — deferring this disconnect until it resolves.");
            return;
        }
        _reconnectRequestInFlight = true;
        _reconnectRequestedWhileInFlight = false;

        // The guard is released on every branch below and at every exit of AttemptReconnect — but all
        // of those hang off a status callback that a dead network may never deliver. A latched guard
        // would then swallow every future reconnect, which is worse than the duplicate it prevents,
        // so give it its own deadline rather than trusting the callback to arrive.
        if (_reconnectGuardWatchdog != null) StopCoroutine(_reconnectGuardWatchdog);
        _reconnectGuardWatchdog = StartCoroutine(ReleaseReconnectGuardIfStalled());

        if (currentDisconnectReason == DisconnectReason.Connected)
        {
            currentDisconnectReason = DisconnectReason.ApplicationPause;
        }

        // A timer armed by an earlier pass is now aimed at THIS pass's reason -- cancel it, then take the
        // snapshot the callback below will decide on.
        if (_resetReasonCoroutine != null)
        {
            StopCoroutine(_resetReasonCoroutine);
            _resetReasonCoroutine = null;
        }
        _passDisconnectReason = currentDisconnectReason;

        ApiAndRoomManager._instance.GetChallengeStatus(ApiAndRoomManager._instance.winLoseChallengeId, success =>
        {
            userModel = JsonUtility.FromJson<Root>(success);
            success.Show("Status");
            if (userModel.status)
            {
                if (userModel.data.status.Contains("active") || userModel.data.status.Contains("expired") || userModel.data.status.Contains("draw") || userModel.data.status.Contains("pending"))
                {
                    bool shouldReconnect = ShouldAttemptReconnect(_passDisconnectReason);

                    if (shouldReconnect)
                    {
                        Debug.Log("Starting reconnection attempt...");
                        _reconnectAttemptRunning = true;
                        reconnectCoroutine = StartCoroutine(AttemptReconnect());
                    }
                    else
                    {
                        Debug.Log($"No reconnection attempt - Reason: {_passDisconnectReason}");
                        ReleaseReconnectGuard();
                        OnIntentionalDisconnect();
                    }

                    // Reset for next connection (after a delay)
                    ArmDisconnectReasonReset();
                }
                else
                {
                    ReleaseReconnectGuard();
                    if (staticVariables.UserProfiledata.user._id.ToString() == userModel.data.winner)
                    {
                        OnDirectWinWithoutInternet.Invoke(true);
                        PopupMessageManager.instance.ReconnectionPanelStatus(false);

                    }
                    else
                    {
                        OnDirectWinWithoutInternet.Invoke(false);
                        PopupMessageManager.instance.ReconnectionPanelStatus(false);
                    }
                }
            }
            else
            {
                // Status call answered but reported failure — nothing below will start a reconnect,
                // so the request guard must not stay latched or the next real drop is ignored.
                ReleaseReconnectGuard();
            }
        }, failed =>
        {
            bool shouldReconnect = ShouldAttemptReconnect(_passDisconnectReason);

            if (shouldReconnect)
            {
                Debug.Log("Starting reconnection attempt...");
                _reconnectAttemptRunning = true;
                reconnectCoroutine = StartCoroutine(AttemptReconnect());
            }
            else
            {
                Debug.Log($"No reconnection attempt - Reason: {_passDisconnectReason}");
                ReleaseReconnectGuard();
                OnIntentionalDisconnect();
            }

            // Reset for next connection (after a delay)
            ArmDisconnectReasonReset();
        });
        // Determine if we should attempt reconnect
    }
    public bool ShouldAttemptReconnect() => ShouldAttemptReconnect(currentDisconnectReason);

    /// <summary>
    /// Decides on the reason PASSED IN, not on the live field. Callers inside the status callback must hand
    /// over their pass's snapshot -- see _passDisconnectReason for what reading the field there cost.
    /// </summary>
    public bool ShouldAttemptReconnect(DisconnectReason reason)
    {
        Debug.Log("Checking Disconnect Cause :" + reason.ToString()
                  + (reason != currentDisconnectReason ? $" (live field is now {currentDisconnectReason})" : ""));
        if (ResultManager.isGameFinished)
            return false;
        else
        {
            // Don't reconnect for intentional disconnects
            switch (reason)
            {
                case DisconnectReason.UserRequested:
                case DisconnectReason.ApplicationQuit:
                case DisconnectReason.SceneTransition:
                    return false;
                case DisconnectReason.ApplicationPause:
                case DisconnectReason.UserGoesBackground:
                case DisconnectReason.ConnectionTimeout:
                case DisconnectReason.ServerShutdown:
                    return true;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Arms the delayed reason-reset, but never on top of a live reconnect pass.
    ///
    /// The call site matters: ReleaseReconnectGuard re-dispatches a DEFERRED disconnect, so by the time the
    /// callback reaches its "reset for next connection" line a NEW pass can already be mid-flight — and the
    /// old pass would arm a 5 s timer pointed straight at the new pass's reason. That is the sequence in the
    /// 17-08 report: pass 1 concluded "No reconnection attempt", re-dispatched pass 2, then armed the timer
    /// that wiped pass 2's reason before its status call answered.
    /// </summary>
    private void ArmDisconnectReasonReset()
    {
        if (_reconnectRequestInFlight || _reconnectAttemptRunning)
        {
            Debug.Log("[Mirror] Skipping the disconnect-reason reset — a reconnect pass is live and owns the reason.");
            return;
        }
        if (_resetReasonCoroutine != null) StopCoroutine(_resetReasonCoroutine);
        _resetReasonCoroutine = StartCoroutine(ResetDisconnectReason());
    }

    private IEnumerator ResetDisconnectReason()
    {
        yield return new WaitForSeconds(5f);
        _resetReasonCoroutine = null;
        // Checked again on the far side of the wait: a pass can start during those 5 seconds, and wiping the
        // reason under it is exactly the bug this guard exists for.
        if (_reconnectRequestInFlight || _reconnectAttemptRunning)
        {
            Debug.Log("[Mirror] Disconnect-reason reset fired while a reconnect pass is live — leaving the reason alone.");
            yield break;
        }
        // Reset to Connected so a future clean disconnect doesn't auto-reconnect
        currentDisconnectReason = DisconnectReason.Connected;
    }

    private IEnumerator AttemptReconnect()
    {

        if (ResultManager.isGameFinished)
        {
            PopupMessageManager.instance.ReconnectionPanelStatus(false);
            OnReconnectSuccess();
            Debug.Log("Reconnect aborted: game already finished.");
            ReleaseReconnectGuard();
            yield break;
        }
        int attempt = 0;
        PopupMessageManager.instance.ReconnectionPanelStatus(true);
        // Wait a moment for cleanup
        yield return new WaitForSeconds(0.5f);

        while (attempt < maxAttempts)
        {
            attempt++;
            Debug.Log($"[Reconnect Attempt {attempt}/{maxAttempts}] Connecting to {serverAddress}...");

            // Ensure we're fully disconnected first
            if (NetworkClient.isConnected)
            {
                StopClient();
                yield return new WaitForSeconds(0.5f);
            }

            // Attempt to join
            bool joinInitiated = false;
            try
            {
                if (!string.IsNullOrEmpty(serverAddress))
                {
                    // Direct reconnect -- address already known, bypass JoinGame flow
                    string[] rParts = serverAddress.Split(':');
                    if (rParts.Length >= 2 && int.TryParse(rParts[1], out int rPort))
                    {
                        networkAddress = rParts[0];
                        GetComponent<kcp2k.KcpTransport>().Port = (ushort)rPort;
                    }
                    else
                    {
                        networkAddress = serverAddress;
                    }
                    StartClient();
                    joinInitiated = true;
                }
                else
                {
                    Debug.LogError("Server address is empty, cannot reconnect");
                    break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Reconnect attempt {attempt} failed to initiate: {ex.Message}");
            }

            if (joinInitiated)
            {
                // Wait for connection with a stability window:
                // Mirror fires OnConnected (ready=true) before the server may send a disconnect.
                // We wait for ready AND then hold for a short stability period to confirm
                // the server hasn't rejected us with an immediate disconnect.
                float timeWaited = 0f;
                bool reachedReady = false;
                float stableTime = 0f;
                const float requiredStableSeconds = 0.5f; // must stay connected this long to count

                while (timeWaited < connectionTimeout)
                {
                    if (!reachedReady && NetworkClient.isConnected && NetworkClient.ready)
                    {
                        reachedReady = true;
                        stableTime = 0f;
                    }

                    if (reachedReady)
                    {
                        if (!NetworkClient.isConnected)
                        {
                            // Server rejected us immediately — don't count as success
                            reachedReady = false;
                            Debug.LogWarning($"[Reconnect] Server rejected connection on attempt {attempt}");
                            break;
                        }
                        stableTime += 0.1f;
                        if (stableTime >= requiredStableSeconds)
                        {
                            Debug.Log("✓ Successfully reconnected to server!");
                            reconnectCoroutine = null;
                            ReleaseReconnectGuard();
                            // Set reason to Connected so OnStopClient won't loop if server later disconnects
                            currentDisconnectReason = DisconnectReason.Connected;
                            OnReconnectSuccess();
                            yield break; // Success!
                        }
                    }

                    yield return new WaitForSeconds(0.1f);
                    timeWaited += 0.1f;
                }

                if (reachedReady)
                    Debug.LogWarning($"[Reconnect] Connection unstable on attempt {attempt} — server disconnected immediately");
                else if (timeWaited >= connectionTimeout)
                    Debug.LogWarning($"[Reconnect] Connection timeout on attempt {attempt}");
            }

            // Wait before next attempt
            if (attempt < maxAttempts)
            {
                Debug.Log($"Waiting {retryDelay} seconds before retry...");
                yield return new WaitForSeconds(retryDelay);
            }
        }

        Debug.LogError($"Failed to reconnect after {maxAttempts} attempts.");
        reconnectCoroutine = null;
        ReleaseReconnectGuard();
        OnReconnectFailed();
    }

    // ============ CALLBACKS ============

    private void OnReconnectSuccess()
    {
        Debug.Log("Reconnection successful!");
        currentDisconnectReason = DisconnectReason.Connected;
        "Reconnected to server!".Show();
        PopupMessageManager.instance.ReconnectionPanelStatus(false);
    }

    private void OnReconnectFailed()
    {
        Debug.LogError("All reconnection attempts failed.");
        // Show disconnect screen, return to menu, etc.
        "Connection lost. Returning to menu...".Show();
        PopupMessageManager.instance.ReconnectionPanelStatus(false);
        SceneManager.LoadScene("Home");

        // Optionally return to main menu
        // SceneManager.LoadScene("MainMenu");
    }

    private void OnIntentionalDisconnect()
    {
        Debug.Log("Intentional disconnect - cleaning up...");
        // Add any cleanup logic for intentional disconnects
        PopupMessageManager.instance.ReconnectionPanelStatus(false);
    }
}

[System.Serializable]
public class NetworkPlayerData
{
    public string playerId;
    public string playerName;
    public bool isCreator;
    public int Scores;
    public int goldCoins;
    public int silverCoins;

}
[System.Serializable]
public class ChallengeStatus
{
    public int _id;
    public string first_player;
    public string transaction_id;
    public int ignore_count;
    public int reject_counter;
    public int game_id;
    public string gold;
    public string silver;
    public string status;
    public string remark;
    public object counter;
    public int admin_commission;
    public int user_coins;
    public string main_player_info;
    public string second_player_info;
    public object is_read;
    public string screenstatus;
    //Photon Removal   public DateTime createdAt;
    //Photon Removal  public DateTime updatedAt;
    public string second_join_time;
    public string second_player;
    public string winner;
    public string coins_type;
    public string bet_type;
    //Photon Removal  public DateTime iam_playing;
}
[System.Serializable]
public class Root
{
    public bool status;
    public ChallengeStatus data;
}