using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ─── Data Models (unchanged) ────────────────────────────────────────────────

[System.Serializable]
public class EdgegapDeploymentRequest
{
    public string application;
    public string version;
    public bool require_cached_locations;
    public List<UserInfo> users;
    public List<EnvironmentVariable> environment_variables;
    public List<string> tags;
    public Webhook webhook_on_ready;
    public Webhook webhook_on_error;
    public Webhook webhook_on_terminated;
}

[System.Serializable]
public class EnvironmentVariable
{
    public string key;
    public string value;
    public bool is_hidden;
}

[System.Serializable]
public class Webhook
{
    public string url;
}

[System.Serializable]
public class UserInfo
{
    public string user_type;
    public UserData user_data;
}

[System.Serializable]
public class UserData
{
    //  public float latitude;
    // public float longitude;
    public string ip_address;

}

[System.Serializable]
public class EdgegapDeploymentResponse
{
    public string request_id;
    public string fqdn;
    public string app_name;
    public string app_version;
    public string current_status;
    public string current_status_label;
    public bool running;
    public string start_time;
    public string removal_time;
    public int elapsed_time;
    public bool error;
    public string public_ip;
    public bool whitelisting_active;
    public Ports ports;
    public string command;
    public string arguments;
    public int max_duration;
    public string last_status;
    public string last_status_label;
    public Location location;
}

[System.Serializable]
public class Ports
{
    public GamePort gameport;
}

[System.Serializable]
public class Location
{
    public string city;
    public string country;
    public string continent;
    public string administrative_division;
    public string timezone;
    public double latitude;
    public double longitude;
}

[System.Serializable]
public class GamePort
{
    public int external;
    public int @internal;
    public string protocol;
    public string name;
    public bool tls_upgrade;
    public string link;
    public string proxy;
}

[System.Serializable]
public class ServerConnectionInfo
{
    public string address;
    public int port;
    public bool isReady;

    public override string ToString() => isReady ? $"{address}:{port}" : "Server not ready";
}

// ─── Deployment List Models ──────────────────────────────────────────────────

[System.Serializable]
public class EdgegapDeploymentListResponse
{
    public List<EdgegapDeploymentSummary> data;
    public int total_count;
}

[System.Serializable]
public class EdgegapDeploymentSummary
{
    public string request_id;
    public string public_ip;
    public bool ready;
    public Ports ports;
    public List<string> tags; // plain strings — matches GET /v1/deployments response
}

// ─── Main Class ─────────────────────────────────────────────────────────────

public class EdgegapAPIClient : MonoBehaviour
{
    public MirrorNetwork mirrorNetwork;

    [Header("Local Testing")]
    [Tooltip("TRUE = skip Edgegap. Creator calls StartHost(), joiner connects to localTestAddress.")]
    public bool localTestMode = false;
    [Tooltip("Address joiner uses in local test mode.")]
    public string localTestAddress = "localhost:7777";

    [Header("Edgegap Configuration")]
    public string apiToken = "0c86a4d6-5a4a-479e-af1d-d1dcf063352d";
    public string applicationName = "games-baba";
    public string versionName = "v2.99.17.47.49.28.utc";

    private const string BASE_URL = "https://api.edgegap.com/v2";

    // In-memory session tracking — source of truth is the live Edgegap API.
    // On app restart Deploy() will query Edgegap by tag instead of reading stale prefs.
    private readonly Dictionary<string, string> _deploymentMap = new Dictionary<string, string>(); // transactionId -> requestId
    private readonly Dictionary<string, string> _readyServers = new Dictionary<string, string>(); // transactionId -> "ip:port"

    /// <summary>
    /// Fired when a server becomes ready. Carries the transactionId so listeners
    /// can filter for the challenge they care about.
    /// </summary>
    public static event Action<string> OnServerReady;

    [Header("Join / Timeout Settings")]
    [Tooltip("Seconds to wait for a server before showing an error to the user.")]
    public float joinTimeoutSeconds = 120f;

    [Tooltip("Seconds to wait after Edgegap reports READY before connecting clients. " +
             "The container port opens before the Mirror server finishes booting, so a " +
             "small warmup prevents connecting into a half-initialized server.")]
    public float serverWarmupDelay = 3f;

    public string publicIp;
    public string tagName;
    public string TagnewValue;
    public string currentServerRequestId; // used by Tag section

    // Active polling coroutines keyed by transactionId — prevents double-polling
    private readonly Dictionary<string, Coroutine> _pollingCoroutines = new Dictionary<string, Coroutine>();
    // Active join-timeout coroutines keyed by transactionId
    private readonly Dictionary<string, Coroutine> _joinTimeoutCoroutines = new Dictionary<string, Coroutine>();

    private void Start()
    {
        mirrorNetwork = GetComponent<MirrorNetwork>();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deploy a game server for this challenge. Safe to call multiple times —
    /// if a deployment is already tracked for this transactionId it resumes polling instead.
    /// Called by CreateChallenge_New after a challenge is created successfully.
    /// </summary>
    public void Deploy(string transactionId, string userId)
    {
        if (localTestMode)
        {
            // Local test: both creator AND joiner connect as pure clients to the
            // editor host on localTestAddress. No Edgegap, no StartHost().
            Debug.Log($"[Edgegap] localTestMode ON — creator connecting as client to {localTestAddress}");
            SaveReadyServer(transactionId, localTestAddress);
            OnServerReady?.Invoke(transactionId);
            return;
        }
        if (IsDeploymentTracked(transactionId))
        {
            Debug.Log($"[Edgegap] Deploy: already tracked for {transactionId}. Resuming poll if needed.");
            string requestId = GetRequestIdForTransaction(transactionId);
            if (!string.IsNullOrEmpty(requestId) && !IsServerReadySaved(transactionId))
                EnsurePolling(transactionId, requestId);
            return;
        }

        // Not in local prefs — check live Edgegap deployments for an existing
        // server tagged with this transactionId before spinning up a new one.
        Debug.Log($"[Edgegap] Deploy: checking live deployments for tag {transactionId}");
        StartCoroutine(FindOrCreateDeploymentCoroutine(transactionId));
    }

    /// <summary>
    /// Unified join entry point. Call this from ChallengeItemPrefab and SocketIOUnityAdapter.
    ///
    /// - If an overrideAddress is provided (e.g. from a socket event), use it immediately.
    /// - If the server is already saved as ready in prefs, join immediately.
    /// - Otherwise subscribe to OnServerReady and start a timeout. If the timeout expires
    ///   the user sees an error popup instead of the flow hanging silently.
    /// </summary>
    public void JoinGame(string transactionId, Request request, string overrideAddress = null)
    {
        mirrorNetwork.cleanup();
        if (localTestMode)
        {
            Debug.Log("[Edgegap] localTestMode ON - connecting joiner to " + localTestAddress);
            ConnectClient(transactionId, request, localTestAddress);
            return;
        }
        // Socket event gave us the address directly — trust it and go
        if (!string.IsNullOrEmpty(overrideAddress))
        {
            Debug.Log($"[Edgegap] JoinGame: override address {overrideAddress} for {transactionId}");
            SaveReadyServer(transactionId, overrideAddress);
            ConnectClient(transactionId, request, overrideAddress);
            return;
        }

        // Already ready from a previous poll
        string savedAddress = GetReadyServerAddress(transactionId);
        if (!string.IsNullOrEmpty(savedAddress))
        {
            Debug.Log($"[Edgegap] JoinGame: server already ready at {savedAddress}");
            ConnectClient(transactionId, request, savedAddress);
            return;
        }

        // Server not ready yet — wait with a timeout
        Debug.Log($"[Edgegap] JoinGame: server not ready for {transactionId}, waiting (max {joinTimeoutSeconds}s)...");
        StartJoinTimeout(transactionId, request);
    }

    // ════════════════════════════════════════════════════════════════════════
    // DEPLOYMENT
    // ════════════════════════════════════════════════════════════════════════

    private IEnumerator CreateDeploymentCoroutine(string transactionId)
    {
        yield return GetPublicIP(ip => publicIp = ip);

        EdgegapDeploymentRequest requestData = new EdgegapDeploymentRequest
        {
            application = applicationName,
            version = versionName,
            require_cached_locations = false,
            users = new List<UserInfo>
            {
                new UserInfo
                {
                    user_type = "ip_address",
                    user_data = new UserData { ip_address = publicIp }

                   // user_data = new UserData { latitude = 1.34097f, longitude = 103.84299f }
                }
            },
            tags = new List<string> { transactionId }, // tag with transactionId for future lookup
            webhook_on_ready = new Webhook { url = "https://my-webhook.com" },
            webhook_on_error = new Webhook { url = "https://my-webhook.com" },
            webhook_on_terminated = new Webhook { url = "https://my-webhook.com" }
        };

        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
        string url = $"{BASE_URL}/deployments";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", $"token {apiToken}");
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "*/*");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    EdgegapDeploymentResponse response =
                        JsonUtility.FromJson<EdgegapDeploymentResponse>(req.downloadHandler.text);

                    SaveDeploymentMapping(transactionId, response.request_id);
                    currentServerRequestId = response.request_id;
                    Debug.Log($"[Edgegap] Deployment created. requestId={response.request_id} for tx={transactionId}");

                    EnsurePolling(transactionId, response.request_id);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Edgegap] Failed to parse deployment response: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[Edgegap] Deployment failed: {req.error}\n{req.downloadHandler.text}");
            }
        }
    }

    /// <summary>
    /// Checks live Edgegap deployments for one tagged with transactionId.
    /// Reuses it if found; otherwise creates a fresh deployment.
    /// </summary>
    private IEnumerator FindOrCreateDeploymentCoroutine(string transactionId)
    {
        EdgegapDeploymentSummary found = null;
        string findError = null;
        yield return FindDeploymentByTagCoroutine(transactionId, r => found = r, e => findError = e);

        if (findError != null)
            Debug.LogWarning($"[Edgegap] Tag search failed ({findError}), creating new deployment.");

        if (found != null)
        {
            Debug.Log($"[Edgegap] Found existing deployment {found.request_id} tagged '{transactionId}'");
            SaveDeploymentMapping(transactionId, found.request_id);
            currentServerRequestId = found.request_id;

            if (found.ready && found.ports?.gameport != null)
            {
                string fullAddress = $"{found.public_ip}:{found.ports.gameport.external}";
                Debug.Log($"[Edgegap] Reusing ready server at {fullAddress}");
                SaveReadyServer(transactionId, fullAddress);
                OnServerReady?.Invoke(transactionId);
            }
            else
            {
                Debug.Log($"[Edgegap] Deployment found but not ready yet — polling {found.request_id}");
                EnsurePolling(transactionId, found.request_id);
            }
            yield break;
        }

        Debug.Log($"[Edgegap] No existing deployment for tag '{transactionId}' — creating new");
        yield return CreateDeploymentCoroutine(transactionId);
    }

    /// <summary>
    /// Calls GET /v1/deployments and returns the first deployment whose tags
    /// list contains transactionId. Returns null (no error) if none found.
    /// </summary>
    private IEnumerator FindDeploymentByTagCoroutine(string transactionId,
        Action<EdgegapDeploymentSummary> onResult, Action<string> onError)
    {
        string url = "https://api.edgegap.com/v1/deployments";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", $"token {apiToken}");
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "*/*");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            try
            {
                var listResponse = JsonUtility.FromJson<EdgegapDeploymentListResponse>(req.downloadHandler.text);
                Debug.Log(req.downloadHandler.text);
                if (listResponse?.data != null)
                {
                    foreach (var deployment in listResponse.data)
                    {
                        if (deployment.tags != null && deployment.tags.Contains(transactionId))
                        {
                            onResult?.Invoke(deployment);
                            yield break;
                        }
                    }
                }
                onResult?.Invoke(null); // not found — not an error
            }
            catch (Exception e)
            {
                onError?.Invoke($"Parse error: {e.Message}");
            }
        }
    }

    /// <summary>Start polling only if not already doing so for this transactionId.</summary>
    private void EnsurePolling(string transactionId, string requestId)
    {
        if (_pollingCoroutines.ContainsKey(transactionId)) return;
        Coroutine c = StartCoroutine(PollUntilReady(transactionId, requestId));
        _pollingCoroutines[transactionId] = c;
    }

    private IEnumerator PollUntilReady(string transactionId, string requestId)
    {
        const int maxAttempts = 40; // ~2 minutes at 3s intervals
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            yield return new WaitForSeconds(3f);

            // Yield the HTTP request directly so we WAIT for the result before
            // evaluating — fixes repeated READY logs caused by the old callback
            // approach where the coroutine loop continued before the cb fired.
            EdgegapDeploymentResponse pollResult = null;
            string pollError = null;

            yield return CheckStatusCoroutineInline(requestId,
                r => pollResult = r,
                e => pollError = e);

            if (pollError != null)
            {
                Debug.LogError($"[Edgegap] Poll error for {transactionId}: {pollError}");
                attempts++;
                continue;
            }

            if (pollResult == null) { attempts++; continue; }

            if (pollResult.current_status == "Status.READY" && pollResult.ports?.gameport != null)
            {
                // Always use public_ip — FQDN DNS resolution fails on KCP/Mirror (mobile networks)
                string addr = pollResult.public_ip;
                string fullAddress = $"{addr}:{pollResult.ports.gameport.external}";
                Debug.Log($"[Edgegap] Server READY for {transactionId} -> {fullAddress}");
                SaveReadyServer(transactionId, fullAddress);
                _pollingCoroutines.Remove(transactionId);

                // Edgegap marks a deployment READY as soon as the container port is allocated,
                // but the Mirror NetworkServer inside the container may still be booting.
                // Connecting immediately causes SyncVar desync, null NetworkGameManager, or
                // abnormal game state. Wait for the server binary to finish initializing.
                if (serverWarmupDelay > 0f)
                {
                    Debug.Log($"[Edgegap] Waiting {serverWarmupDelay}s for server warmup before connecting...");
                    yield return new WaitForSeconds(serverWarmupDelay);
                }

                OnServerReady?.Invoke(transactionId);
                yield break; // stop immediately — no more polling
            }
            else if (pollResult.current_status == "ERROR" || pollResult.current_status == "TERMINATED")
            {
                Debug.LogError($"[Edgegap] Server failed for {transactionId}: {pollResult.current_status}");
                _pollingCoroutines.Remove(transactionId);
                yield break;
            }
            else
            {
                Debug.Log($"[Edgegap] Still deploying {transactionId}: {pollResult.current_status}");
            }

            attempts++;
        }

        Debug.LogError($"[Edgegap] Polling timed out for {transactionId}");
        _pollingCoroutines.Remove(transactionId);
    }

    // Inline coroutine yielded directly by PollUntilReady so HTTP response is
    // fully awaited before the poll loop continues to the next iteration.
    private IEnumerator CheckStatusCoroutineInline(string requestId,
        System.Action<EdgegapDeploymentResponse> onSuccess, System.Action<string> onError)
    {
        string url = $"https://api.edgegap.com/v1/status/{requestId}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", $"token {apiToken}");
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "*/*");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                try { onSuccess?.Invoke(JsonUtility.FromJson<EdgegapDeploymentResponse>(req.downloadHandler.text)); }
                catch (Exception e) { onError?.Invoke($"Parse error: {e.Message}"); }
            }
            else { onError?.Invoke($"Status check failed: {req.error}"); }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // JOIN & TIMEOUT
    // ════════════════════════════════════════════════════════════════════════

    private void StartJoinTimeout(string transactionId, Request request)
    {
        // Only one timeout per transactionId
        if (_joinTimeoutCoroutines.ContainsKey(transactionId)) return;

        // Subscribe to OnServerReady so we join as soon as the server is up
        Action<string> handler = null;
        handler = (readyTxId) =>
        {
            if (readyTxId != transactionId) return;

            OnServerReady -= handler;
            CancelJoinTimeout(transactionId);

            string address = GetReadyServerAddress(transactionId);
            if (!string.IsNullOrEmpty(address))
                ConnectClient(transactionId, request, address);
        };
        OnServerReady += handler;

        // Joiner never called Deploy() so nothing on this device is polling Edgegap.
        // Actively find the deployment by tag and start polling so OnServerReady fires here too.
        StartCoroutine(JoinerFindAndPollCoroutine(transactionId));

        // Also start the safety-net timeout
        Coroutine c = StartCoroutine(JoinTimeoutCoroutine(transactionId, request, handler));
        _joinTimeoutCoroutines[transactionId] = c;
    }

    /// <summary>
    /// Joiner-side polling: repeatedly query Edgegap for a deployment tagged with
    /// transactionId. Once found, hand off to EnsurePolling so OnServerReady fires
    /// on this device when the server becomes ready.
    /// </summary>
    private IEnumerator JoinerFindAndPollCoroutine(string transactionId)
    {
        const float retryInterval = 5f;
        const float maxWait       = 90f;   // give up after 90s (timeout handles the rest)
        float elapsed = 0f;

        Debug.Log($"[Edgegap] Joiner: starting active search for deployment '{transactionId}'");

        while (elapsed < maxWait)
        {
            // Already polling or server already found — nothing more to do
            if (_pollingCoroutines.ContainsKey(transactionId) || IsServerReadySaved(transactionId))
                yield break;

            EdgegapDeploymentSummary found = null;
            string findError = null;
            yield return FindDeploymentByTagCoroutine(transactionId, r => found = r, e => findError = e);

            if (found != null)
            {
                Debug.Log($"[Edgegap] Joiner found deployment {found.request_id} for '{transactionId}'");
                SaveDeploymentMapping(transactionId, found.request_id);
                currentServerRequestId = found.request_id;

                if (found.ready && found.ports?.gameport != null)
                {
                    // Already ready — fire immediately (no warmup needed; creator handled warmup)
                    string fullAddress = $"{found.public_ip}:{found.ports.gameport.external}";
                    SaveReadyServer(transactionId, fullAddress);
                    OnServerReady?.Invoke(transactionId);
                }
                else
                {
                    // Found but still spinning up — start polling
                    EnsurePolling(transactionId, found.request_id);
                }
                yield break;
            }

            if (findError != null)
                Debug.LogWarning($"[Edgegap] Joiner tag search error ({findError}), retry in {retryInterval}s");
            else
                Debug.Log($"[Edgegap] Joiner: no deployment yet for '{transactionId}', retry in {retryInterval}s");

            yield return new WaitForSeconds(retryInterval);
            elapsed += retryInterval;
        }

        Debug.LogWarning($"[Edgegap] Joiner: gave up finding deployment for '{transactionId}' after {maxWait}s");
    }

    private IEnumerator JoinTimeoutCoroutine(string transactionId, Request request, Action<string> handler)
    {
        float elapsed = 0f;

        while (elapsed < joinTimeoutSeconds)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;

            // Every 5 seconds do a defensive prefs check in case the event was missed
            if (elapsed % 5 == 0)
            {
                string savedAddress = GetReadyServerAddress(transactionId);
                if (!string.IsNullOrEmpty(savedAddress))
                {
                    OnServerReady -= handler;
                    CancelJoinTimeout(transactionId);
                    ConnectClient(transactionId, request, savedAddress);
                    yield break;
                }
            }
        }

        // Timeout — server never became ready in time
        OnServerReady -= handler;
        _joinTimeoutCoroutines.Remove(transactionId);
        Debug.LogError($"[Edgegap] JoinGame timed out for {transactionId}");
        ShowJoinTimeoutError();
    }

    private void CancelJoinTimeout(string transactionId)
    {
        if (_joinTimeoutCoroutines.TryGetValue(transactionId, out Coroutine c))
        {
            StopCoroutine(c);
            _joinTimeoutCoroutines.Remove(transactionId);
        }
    }

    private void ShowJoinTimeoutError()
    {
        if (PopupMessageManager.instance != null)
            PopupMessageManager.instance.SetPanelStaus(true,
                "Could not connect to game server.\nPlease try again.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // CONNECT CLIENT (single place all Mirror connect logic lives)
    // ════════════════════════════════════════════════════════════════════════

    private void ConnectClient(string transactionId, Request request, string fullAddress)
    {
        string[] parts = fullAddress.Split(':');
        if (parts.Length < 2 || !int.TryParse(parts[1], out int serverPort))
        {
            Debug.LogError($"[Edgegap] Invalid address format: {fullAddress}");
            return;
        }

        string serverIp = parts[0];

        SnokerNetwork.dataisset = false;

        if (request != null)
        {
            Dictionary<string, string> props = new Dictionary<string, string>
            {
                { "_transaction_id", transactionId },
                { "_user_id",        request.user_id },
                { "_server_address", fullAddress }
            };
            ApiAndRoomManager._instance.notifyToplayChallenge(props, _ =>
            {
                MirrorNetwork.BeginNewMatchJoin(fullAddress);   // also resets the per-match pre-game retry budget
            });
        }
        else
        {
            MirrorNetwork.BeginNewMatchJoin(fullAddress);   // also resets the per-match pre-game retry budget
        }

        // Guard: wait for any previous session to fully close before starting a new one.
        // StopClient() is asynchronous — NetworkClient.isConnected stays true for a frame or two.
        // Calling StartClient() before it clears causes Mirror's "NetworkClient is already ready" error.
        if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
        StartCoroutine(ConnectAfterDisconnect(serverIp, serverPort));
    }

    private IEnumerator ConnectAfterDisconnect(string serverIp, int serverPort)
    {
        float waited = 0f;
        while (NetworkClient.isConnected && waited < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;
        }
        mirrorNetwork.networkAddress = serverIp;
        mirrorNetwork.GetComponent<kcp2k.KcpTransport>().Port = (ushort)serverPort;
        mirrorNetwork.StartClient();
        Debug.Log($"[Edgegap] Client connecting to {serverIp}:{serverPort}");
    }

    // ════════════════════════════════════════════════════════════════════════
    // STATUS CHECK
    // ════════════════════════════════════════════════════════════════════════

    public void CheckDeploymentStatus(string requestId,
        Action<EdgegapDeploymentResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(CheckStatusCoroutine(requestId, onSuccess, onError));
    }

    private IEnumerator CheckStatusCoroutine(string requestId,
        Action<EdgegapDeploymentResponse> onSuccess, Action<string> onError)
    {
        string url = $"https://api.edgegap.com/v1/status/{requestId}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", $"token {apiToken}");
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "*/*");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    EdgegapDeploymentResponse response =
                        JsonUtility.FromJson<EdgegapDeploymentResponse>(req.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e) { onError?.Invoke($"Parse error: {e.Message}"); }
            }
            else
            {
                onError?.Invoke($"Status check failed: {req.error}");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // CLEANUP
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clears all in-memory session state from the previous game.
    /// Call from MirrorNetwork.cleanup() (non-rematch) so stale server entries
    /// from game A don't interfere when game B tries to connect.
    /// </summary>
    public void ClearSession()
    {
        foreach (var kvp in _pollingCoroutines)
            if (kvp.Value != null) StopCoroutine(kvp.Value);
        _pollingCoroutines.Clear();

        foreach (var kvp in _joinTimeoutCoroutines)
            if (kvp.Value != null) StopCoroutine(kvp.Value);
        _joinTimeoutCoroutines.Clear();

        _deploymentMap.Clear();
        _readyServers.Clear();
        Debug.Log("[Edgegap] Session cleared.");
    }

    public void CleanupServer(string transactionId)
    {
        string requestId = GetRequestIdForTransaction(transactionId);
        if (string.IsNullOrEmpty(requestId))
        {
            Debug.Log($"[Edgegap] CleanupServer: no deployment tracked for {transactionId}");
            return;
        }
        DeleteDeployment(requestId,
            () => Debug.Log($"[Edgegap] Server cleaned up for {transactionId}"),
            err => Debug.LogError($"[Edgegap] Cleanup failed for {transactionId}: {err}"));
    }

    public void DeleteDeployment(string requestId, Action onSuccess, Action<string> onError)
    {
        StartCoroutine(DeleteDeploymentCoroutine(requestId, onSuccess, onError));
    }

    private IEnumerator DeleteDeploymentCoroutine(string requestId,
        Action onSuccess, Action<string> onError)
    {
        string url = $"https://api.edgegap.com/v1/stop/{requestId}";

        using (UnityWebRequest req = new UnityWebRequest(url, "DELETE"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", $"token {apiToken}");
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "*/*");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Edgegap] Deployment {requestId} deleted");
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke($"Delete failed: {req.error}");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // IN-MEMORY TRACKING  (session-only, no PlayerPrefs)
    // ════════════════════════════════════════════════════════════════════════

    private void SaveDeploymentMapping(string transactionId, string requestId) =>
        _deploymentMap[transactionId] = requestId;

    private bool IsDeploymentTracked(string transactionId) =>
        _deploymentMap.ContainsKey(transactionId);

    private string GetRequestIdForTransaction(string transactionId)
    {
        _deploymentMap.TryGetValue(transactionId, out string id);
        return id;
    }

    private void SaveReadyServer(string transactionId, string fullAddress) =>
        _readyServers[transactionId] = fullAddress;

    private bool IsServerReadySaved(string transactionId) =>
        _readyServers.ContainsKey(transactionId);

    private string GetReadyServerAddress(string transactionId)
    {
        _readyServers.TryGetValue(transactionId, out string address);
        return address;
    }

    // ════════════════════════════════════════════════════════════════════════
    // MISC
    // ════════════════════════════════════════════════════════════════════════

    private IEnumerator GetPublicIP(Action<string> callback)
    {
        UnityWebRequest req = UnityWebRequest.Get("https://api.ipify.org");
        yield return req.SendWebRequest();
        publicIp = req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
        callback(publicIp);
    }

    public void OnApplicationQuit() { /* Cleanup is now per-transactionId via CleanupServer() */ }

    // ════════════════════════════════════════════════════════════════════════
    // TAGS
    // ════════════════════════════════════════════════════════════════════════

    #region TAGS
    public void CreateTagButton() => StartCoroutine(CreateTag(tagName));
    IEnumerator CreateTag(string tName)
    {
        string url = $"https://api.edgegap.com/v1/deployments/{currentServerRequestId}/tags";
        string jsonBody = JsonUtility.ToJson(new TagName(tName));
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"token {apiToken}");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                Debug.Log("Tag created: " + req.downloadHandler.text);
            else
                Debug.LogError("Tag create failed: " + req.error + "\n" + req.downloadHandler.text);
        }
    }
    public void GetTagButton() => StartCoroutine(GetTag());
    IEnumerator GetTag()
    {
        string url = $"https://api.edgegap.com/v1/deployments/{currentServerRequestId}/tags/{tagName}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", $"token {apiToken}");
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            Debug.Log(req.result == UnityWebRequest.Result.Success
                ? "Tag: " + req.downloadHandler.text
                : "Tag get failed: " + req.error);
        }
    }
    public void GetTagsList() => StartCoroutine(GetAllTags());
    IEnumerator GetAllTags()
    {
        string url = $"https://api.edgegap.com/v1/deployments/{currentServerRequestId}/tags";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + apiToken);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            Debug.Log(req.result == UnityWebRequest.Result.Success
                ? "Tags: " + req.downloadHandler.text
                : "Tags get failed: " + req.error);
        }
    }
    public void DeleteTagButton() => StartCoroutine(DeleteTag());
    IEnumerator DeleteTag()
    {
        string url = $"https://api.edgegap.com/v1/deployments/{currentServerRequestId}/tags/{tagName}";
        using (UnityWebRequest req = UnityWebRequest.Delete(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + apiToken);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            Debug.Log(req.result == UnityWebRequest.Result.Success ? "Tag deleted" : "Tag delete failed: " + req.error);
        }
    }
    public void UpdateTagButton() => StartCoroutine(UpdateTagValue(TagnewValue));
    IEnumerator UpdateTagValue(string newValue)
    {
        string url = $"https://api.edgegap.com/v1/deployments/{currentServerRequestId}/tags/{tagName}";
        string jsonBody = JsonUtility.ToJson(new TagUpdate(newValue));
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        using (UnityWebRequest req = new UnityWebRequest(url, "PATCH"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + apiToken);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            Debug.Log(req.result == UnityWebRequest.Result.Success ? "Tag updated" : "Tag update failed: " + req.error);
        }
    }

    [System.Serializable] public class TagUpdate { public string value; public TagUpdate(string v) { value = v; } }
    [System.Serializable] public class Tag { public string name; public string value; }
    [System.Serializable] public class TagList { public Tag[] tags; }
    [System.Serializable] public class TagName { public string name; public TagName(string n) { name = n; } }
    #endregion
}

public enum ServerState { Lobby, InGame, Finished }
