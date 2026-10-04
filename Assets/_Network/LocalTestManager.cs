using Mirror;
using NetworkManagement;
using UnityEngine;

/// <summary>
/// Runtime OnGUI panel for offline/local multiplayer testing.
/// Visible only when EdgegapAPIClient.localTestMode == true.
///
/// TWO MODES:
///  1. Direct Connect  – normal connect, you choose Creator or Joiner.
///  2. Simulate Invite – skips Firebase + Join_Challenge API; player 2 just
///                       enters the txId shown on player 1 and connects as Joiner.
///
/// HOW TO USE (local test):
///  a) RituGamesServer editor runs the dedicated server (Play).
///  b) Player 1 editor:  Direct Connect → Is Creator ✓ → Connect
///  c) Player 2 editor:  Simulate Invite → paste txId from P1 log → Accept Invite
/// </summary>
public class LocalTestManager : MonoBehaviour
{
    // ── Inspector / defaults ─────────────────────────────────────────────────
    private string _playerName  = "TestPlayer1";
    private string _playerId    = "10001";
    private string _token       = "";           // Bearer token for API calls
    private string _serverIp    = "127.0.0.1";
    private int    _port        = 7777;
    private bool   _isCreator   = true;
    private string _gameIdStr   = "8";  // type game id directly

    // Direct-Invite simulation
    private string _inviteTxId  = "local-test-txid";

    // UI state
    private bool   _visible     = true;
    private int    _mode        = 0;   // 0 = Direct Connect, 1 = Simulate Invite

    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Only keep one instance across scene loads
        if (FindObjectsByType<LocalTestManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        var profile = staticVariables.UserProfiledata;
        var u       = profile.user;
        if (u._id != 0)
        {
            _playerId   = u._id.ToString();
            _playerName = (u.first_name + " " + u.last_name).Trim();
        }
        // Preserve real token so API calls (bets/play etc.) don't get 401
        if (!string.IsNullOrEmpty(profile.access_token))
            _token = profile.access_token;
    }

    private void OnGUI()
    {
        if (MirrorNetwork.Instance == null || !MirrorNetwork.Instance.edgegapAPIClient.localTestMode) return;

        // Toggle button top-left
        if (GUI.Button(new Rect(5, 5, 140, 24), _visible ? "▼ LocalTest" : "▶ LocalTest"))
            _visible = !_visible;

        if (!_visible) return;

        // Panel background
        GUI.Box(new Rect(5, 32, 340, _mode == 0 ? 220 : 200), "");

        GUILayout.BeginArea(new Rect(12, 40, 320, 400));

        // Mode tabs
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = _mode == 0 ? Color.cyan : Color.white;
        if (GUILayout.Button("Direct Connect", GUILayout.Width(155))) _mode = 0;
        GUI.backgroundColor = _mode == 1 ? Color.cyan : Color.white;
        if (GUILayout.Button("Simulate Invite", GUILayout.Width(155))) _mode = 1;
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // ── Common fields ────────────────────────────────────────────────────
        GUILayout.Label("Player Name:");
        _playerName = GUILayout.TextField(_playerName, GUILayout.Width(300));

        GUILayout.Label("Player ID (int):");
        _playerId = GUILayout.TextField(_playerId, GUILayout.Width(300));

        GUILayout.Label("Auth Token (JWT):");
        _token = GUILayout.TextField(_token, GUILayout.Width(300));

        GUILayout.Label("Server IP:");
        _serverIp = GUILayout.TextField(_serverIp, GUILayout.Width(300));

        GUILayout.BeginHorizontal();
        GUILayout.Label("Port:", GUILayout.Width(40));
        string portStr = GUILayout.TextField(_port.ToString(), GUILayout.Width(60));
        if (int.TryParse(portStr, out int p)) _port = p;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Game ID:", GUILayout.Width(60));
        _gameIdStr = GUILayout.TextField(_gameIdStr, GUILayout.Width(60));
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // ── Mode-specific ────────────────────────────────────────────────────
        if (_mode == 0)
            DrawDirectConnectMode();
        else
            DrawSimulateInviteMode();

        GUILayout.EndArea();
    }

    private void DrawDirectConnectMode()
    {
        _isCreator = GUILayout.Toggle(_isCreator, " I am the Creator (Player 1)");

        GUILayout.Space(6);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Connect", GUILayout.Height(32)))
            ConnectDirect();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(4);
        GUILayout.Label("<size=10><color=grey>Creator calls Deploy(); Joiner calls JoinGame().\nBoth land on localTestAddress.</color></size>");
    }

    private void DrawSimulateInviteMode()
    {
        GUILayout.Label("Transaction ID (from Player 1 log):");
        _inviteTxId = GUILayout.TextField(_inviteTxId, GUILayout.Width(300));

        GUILayout.Space(6);
        GUI.backgroundColor = new Color(1f, 0.6f, 0f);
        if (GUILayout.Button("Accept Invite  (connect as Joiner)", GUILayout.Height(32)))
            SimulateAcceptInvite();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(4);
        GUILayout.Label("<size=10><color=grey>Skips Firebase + Join_Challenge API.\nSets isMasterClient=false and calls JoinGame directly.</color></size>");
    }

    // ────────────────────────────────────────────────────────────────────────
    private void ConnectDirect()
    {
        if (!ValidateInputs()) return;

        InjectFakeProfile();

        MirrorNetwork.Instance.isMasterClient = _isCreator;
        ApplyLocalTestAddress();

        int gameId = int.TryParse(_gameIdStr, out int gid) ? gid : 8;
        ApiAndRoomManager.currentGameId = gameId;
        string fakeTxId = $"local-{(int)(Time.realtimeSinceStartup * 1000)}";
        ApiAndRoomManager._instance.winLoseChallengeId = fakeTxId;

        if (_isCreator)
            MirrorNetwork.Instance.edgegapAPIClient.Deploy(fakeTxId, _playerId);
        else
            MirrorNetwork.Instance.edgegapAPIClient.JoinGame(fakeTxId, null);

        _visible = false;
        Debug.Log($"[LocalTest] DirectConnect | creator={_isCreator} | game={gameId} | addr={_serverIp}:{_port}");
    }

    private void SimulateAcceptInvite()
    {
        if (!ValidateInputs()) return;
        if (string.IsNullOrWhiteSpace(_inviteTxId))
        {
            Debug.LogError("[LocalTest] Transaction ID is empty!");
            return;
        }

        InjectFakeProfile();

        // Joiner role
        MirrorNetwork.Instance.isMasterClient = false;
        ApplyLocalTestAddress();

        int gameId = int.TryParse(_gameIdStr, out int gid) ? gid : 8;
        ApiAndRoomManager.currentGameId = gameId;
        ApiAndRoomManager._instance.winLoseChallengeId = _inviteTxId;
        staticVariables.isgoldcoins = true; // default gold for local test

        // Skip Join_Challenge API — go straight to JoinGame (same as the real invite accept
        // flow minus the server-side API call which isn't needed locally)
        MirrorNetwork.Instance.edgegapAPIClient.JoinGame(_inviteTxId, null);

        _visible = false;
        Debug.Log($"[LocalTest] SimulateInviteAccept | txId={_inviteTxId} | game={gameId} | addr={_serverIp}:{_port}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private bool ValidateInputs()
    {
        if (!int.TryParse(_playerId, out _))
        {
            Debug.LogError("[LocalTest] Player ID must be an integer.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(_playerName))
        {
            Debug.LogError("[LocalTest] Player name is empty.");
            return false;
        }
        return true;
    }

    private void InjectFakeProfile()
    {
        int id = int.Parse(_playerId);
        string[] parts = _playerName.Split(' ');
        string first = parts[0];
        string last  = parts.Length > 1 ? parts[1] : "";

        // uniqueGameIdentifier is set from SystemInfo.deviceUniqueIdentifier in LoadingManager.
        // ServerConnection checks user_login_token == uniqueGameIdentifier before every API call
        // and redirects to login if they differ. We set user_login_token to match so the check passes.
        string deviceId = staticVariables.uniqueGameIdentifier;
        if (string.IsNullOrEmpty(deviceId))
            deviceId = SystemInfo.deviceUniqueIdentifier;

        staticVariables.UserProfiledata = new UserModel
        {
            access_token = _token,          // ← real JWT so Bearer header is sent correctly
            user = new User
            {
                _id              = id,
                first_name       = first,
                last_name        = last,
                gold_balance     = 99999,
                silver_balance   = 99999,
                user_login_token = deviceId  // ← matches uniqueGameIdentifier → bypasses login-redirect
            }
        };

        staticVariables.userNickName = _playerName;
        staticVariables.isgoldcoins  = true;
        SnokerNetwork.IsMultiplayer  = true;
        GameConstants.isWithAI       = false;

        // Fake coins for PlayerPrefs (used by MirrorPlayerPrefab.OnStartClient)
        PlayerPrefs.SetString(LocalSettings.TotalChips, "99999");
        PlayerPrefs.Save();

        Debug.Log($"[LocalTest] Injected profile: {_playerName} (id={id}) | token={(string.IsNullOrEmpty(_token) ? "EMPTY" : "SET")}");
    }

    private void ApplyLocalTestAddress()
    {
        string addr = _serverIp;
        // Update EdgegapAPIClient so JoinGame/Deploy use the right address
        MirrorNetwork.Instance.edgegapAPIClient.localTestAddress = $"{addr}:{_port}";
    }
}
