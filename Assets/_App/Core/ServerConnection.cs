using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


public static class ServerConnection
{
    public static Dictionary<string, string> secrets = new Dictionary<string, string>();
    // Function to make a GET request
    // Headless dedicated server: forward the App Check token handed to this deployment through
    // its environment (APPCHECK_TOKEN, set by the client's Edgegap deployment call). The server
    // cannot attest itself — no Play Services in the container — but the forwarded token is a
    // real, Firebase-issued token with ~1h TTL, comfortably beyond the ~20-minute match window,
    // so backend App Check verification passes with no server-side exemption.
    private static string appCheckEnvToken;
    private static bool appCheckEnvTokenRead;
    private static void AttachAppCheck(UnityWebRequest r)
    {
        if (!appCheckEnvTokenRead)
        {
            appCheckEnvTokenRead = true;
            appCheckEnvToken = Environment.GetEnvironmentVariable("APPCHECK_TOKEN");
        }
        if (!string.IsNullOrEmpty(appCheckEnvToken))
            r.SetRequestHeader("X-Firebase-AppCheck", appCheckEnvToken);
    }

    // ── Settlement auth key (headless server only) ──────────────────────────────────────────────────
    // Win/loss settlement now happens on the Edgegap headless build rather than on a player's device, so
    // the backend needs to tell a real server apart from anyone replaying the call. The key is supplied as
    // a deployment environment variable ("authkey" on the Edgegap version) and never ships inside any APK.
    // Read once and cached: it is fixed for the life of the container.
    private static string _settlementAuthKey;
    private static bool _settlementAuthKeyRead;
    public const string SETTLEMENT_AUTH_HEADER = "gameplaytoken";

    /// <summary>
    /// The build token this process authenticates settlement with, kept separate from
    /// staticVariables.UserProfiledata.access_token. That field is reloaded and cleared while a match
    /// is running (the 401 handler further down deletes the saved userModel), so settlement fired
    /// mid-match was going out under the STALE login token instead. Replaying both tokens against the
    /// live endpoint confirmed it: build token answers 200, stale login token answers 401.
    /// </summary>
    public static string BuildAuthToken;

#if UNITY_EDITOR
    // Editor-only fallback: the Editor is not an Edgegap container, so there is no deployment
    // environment to read the key from and settlement testing from Play Mode would always be
    // unauthenticated. Paste a key here to test. The #if keeps it out of every player build, but it is
    // still plain text in the repository — use a test key, not the production one.
    private const string EDITOR_SETTLEMENT_AUTH_KEY = "d010a9cb93a91531d3eb072463c72bf94947809235f5a84309a3c4aabdd968ae";
#endif

    public static string SettlementAuthKey
    {
        get
        {
            if (!_settlementAuthKeyRead)
            {
                _settlementAuthKeyRead = true;
                try { _settlementAuthKey = Environment.GetEnvironmentVariable(SETTLEMENT_AUTH_HEADER); }
                catch (Exception e) { _settlementAuthKey = null; Debug.LogWarning($"[Settlement] Could not read the {SETTLEMENT_AUTH_HEADER} environment variable: {e.Message}"); }
#if UNITY_EDITOR
                // The environment variable still wins when one is set, so an Editor launched with the real
                // variable behaves exactly like the container.
                if (string.IsNullOrEmpty(_settlementAuthKey) && !string.IsNullOrEmpty(EDITOR_SETTLEMENT_AUTH_KEY))
                {
                    _settlementAuthKey = EDITOR_SETTLEMENT_AUTH_KEY;
                    Debug.LogWarning($"[Settlement] Using the hardcoded Editor {SETTLEMENT_AUTH_HEADER} ({_settlementAuthKey.Length} chars). This path never exists in a build.");
                }
#endif
                if (string.IsNullOrEmpty(_settlementAuthKey))
                    Debug.LogError($"[Settlement] {SETTLEMENT_AUTH_HEADER} is not set in this deployment's environment — settlement calls will be sent without it and the backend is expected to reject them.");
                else
                    Debug.Log($"[Settlement] {SETTLEMENT_AUTH_HEADER} loaded from the deployment environment ({_settlementAuthKey.Length} chars).");
            }
            return _settlementAuthKey;
        }
    }

    // Resolve the key as the process starts instead of on the first settlement call. A container that
    // came up without it then says so in the first lines of its log, rather than twenty minutes later
    // when a hand finishes and the backend rejects the call.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PrimeSettlementAuthKey()
    {
        _ = SettlementAuthKey;
    }

    public static IEnumerator GetApiRequest(string url, Action<string> onSuccess = null, Action<string> onFailure = null, bool withSettlementAuth = false)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {

            //ApiAndRoomManager._instance.LoadingObject.SetActive(true);
            //(Main_URL() + url).Show("URL");
            Scene currentScene = SceneManager.GetActiveScene();

            using (UnityWebRequest webRequest = UnityWebRequest.Get(Main_URL() + url))
            {
                // A settlement call must present the build token; every other call keeps using whatever
                // the profile currently holds. The endpoint needs BOTH this header and gameplaytoken:
                // dropping either one answers 401.
                string bearerToken = withSettlementAuth && !string.IsNullOrEmpty(BuildAuthToken)
                    ? BuildAuthToken
                    : (staticVariables.UserProfiledata != null ? staticVariables.UserProfiledata.access_token : null);
                if (!string.IsNullOrEmpty(bearerToken))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + bearerToken);
                if (secrets.ContainsKey("apk_signature_black_arch"))
                    webRequest.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                // Deliberately outside the if above -- the indentation used to suggest otherwise.
                AttachAppCheck(webRequest);
                // Only settlement calls carry the deployment key, so an ordinary request cannot be
                // repurposed into one if it is ever observed.
                if (withSettlementAuth && !string.IsNullOrEmpty(SettlementAuthKey))
                {
                    webRequest.SetRequestHeader(SETTLEMENT_AUTH_HEADER, SettlementAuthKey);
#if UNITY_EDITOR
                    string attachedGameplayToken = webRequest.GetRequestHeader(SETTLEMENT_AUTH_HEADER);
                    bool matchesResolvedToken = string.Equals(attachedGameplayToken, SettlementAuthKey, StringComparison.Ordinal);
                    bool usingEditorFallback = string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SETTLEMENT_AUTH_HEADER));
                    Debug.Log($"[Settlement][Editor Header Check] url={Main_URL() + url}, header={SETTLEMENT_AUTH_HEADER}, attached={!string.IsNullOrEmpty(attachedGameplayToken)}, matchesResolvedToken={matchesResolvedToken}, source={(usingEditorFallback ? "EDITOR_SETTLEMENT_AUTH_KEY" : "environment")}, length={attachedGameplayToken?.Length ?? 0}");
#endif
                }
                webRequest.SetRequestHeader("accept", "*/*");
                webRequest.SetRequestHeader("Content-Type", "application/json");
                yield return webRequest.SendWebRequest();
                if (webRequest.downloadHandler.text.Contains("Unauthorized"))
                {
                    PlayerPrefs.DeleteKey("userModel");
                    if (!Utils.IsHeadless())
                        SceneManager.LoadScene(1);
                    ("Token miss hai bhai").Show();
                }
                if (webRequest.downloadHandler.text.Contains("Service Unavailable"))
                {
                    PlayerPrefs.DeleteKey("userModel");
                    if (!Utils.IsHeadless())
                        SceneManager.LoadScene(1);
                    PopupMessageManager.instance.ShowConfirmPanel("Sorry! Service unavailable due to maintainance.", yesButtonText: "OK", noButtonStatus: false, methodToInvoke: () => { SceneManager.LoadScene(2); });
                    ("Token miss hai bhai").Show();
                }
                ($"{webRequest.downloadHandler.text}").Show((Main_URL() + url));
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    // ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onSuccess?.Invoke(webRequest.downloadHandler.text);

                }
                else
                {
                    ("<color=green>" + Main_URL() + url + "</color>, <color=red>" + webRequest.error + "</color>").Show();
                    // ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onFailure?.Invoke(webRequest.error);

                }
            }
        }
        else
        {
            yield return null;
            //Application.Quit();
            //  ApiAndRoomManager._instance.LoadingObject.SetActive(false);

        }
    }
    public static IEnumerator GetBearerApiRequest(string url, Action<string> onSuccess = null, Action<string> onFailure = null)
    {


        //ApiAndRoomManager._instance.LoadingObject.SetActive(true);
        //(Main_URL() + url).Show("URL");
        Scene currentScene = SceneManager.GetActiveScene();

        using (UnityWebRequest webRequest = UnityWebRequest.Get(Main_URL() + url))
        {

            if (secrets.ContainsKey("apk_signature_black_arch"))
                webRequest.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                AttachAppCheck(webRequest);
            webRequest.SetRequestHeader("accept", "*/*");
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return webRequest.SendWebRequest();
            if (webRequest.downloadHandler.text.Contains("Unauthorized"))
            {
                PlayerPrefs.DeleteKey("userModel");
                if (!Utils.IsHeadless())
                    SceneManager.LoadScene(1);
                ("Token miss hai bhai").Show();
            }
            if (webRequest.downloadHandler.text.Contains("Service Unavailable"))
            {
                PlayerPrefs.DeleteKey("userModel");
                if (!Utils.IsHeadless())
                    SceneManager.LoadScene(1);
                PopupMessageManager.instance.ShowConfirmPanel("Sorry! Service unavailable due to maintainance.", yesButtonText: "OK", noButtonStatus: false, methodToInvoke: () => { SceneManager.LoadScene(2); });
                ("Token miss hai bhai").Show();
            }
            ($"{webRequest.downloadHandler.text}").Show((Main_URL() + url));
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                onSuccess?.Invoke(webRequest.downloadHandler.text);

            }
            else
            {
                ("<color=green>" + Main_URL() + url + "</color>, <color=red>" + webRequest.error + "</color>").Show();
                // ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                onFailure?.Invoke(webRequest.error);

            }
        }

    }
    public static IEnumerator DeleteApiRequest(string url, Action<string> onSuccess = null, Action<string> onFailure = null)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            (EventSystem.current.gameObject.name + " event object name ").Show();
            //(  Main_URL() + url  ).Show("URL");
            Scene currentScene = SceneManager.GetActiveScene();

            using (UnityWebRequest webRequest = UnityWebRequest.Delete(Main_URL() + url))
            {
                if (staticVariables.UserProfiledata != null && !string.IsNullOrEmpty(staticVariables.UserProfiledata.access_token))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + staticVariables.UserProfiledata.access_token);
                webRequest.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                AttachAppCheck(webRequest);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("accept", "*/*");
                webRequest.SetRequestHeader("Content-Type", "application/json");
                yield return webRequest.SendWebRequest();
                if (webRequest.downloadHandler.text.Contains("Unauthorized Token is missing."))
                {
                    PlayerPrefs.DeleteKey("userModel");
                    if (!Utils.IsHeadless())
                        SceneManager.LoadScene(1);
                }
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    //("<color=green>" + Main_URL() + url + "</color>, <color=red>" + webRequest.downloadHandler.text + "</color>").Show();

                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                }
                else
                {
                    //("<color=green>" + Main_URL() + url + "</color>, <color=red>" + webRequest.error + "</color>").Show();

                    onFailure?.Invoke(webRequest.error);
                }
            }
        }
        else
        {
            yield return null;
            //Application.Quit();
            //  ApiAndRoomManager._instance.LoadingObject.SetActive(false);

        }
    }
    public static IEnumerator GetTimeRequest(string url, Action<string> onSuccess = null, Action<string> onFailure = null)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            //("<color=green>" + url + "</color>").Show();

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                AttachAppCheck(webRequest);
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    //("<color=green>" + Main_URL() + url + "</color>, <color=red>" + webRequest.downloadHandler.text + "</color>").Show();

                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                }
                else
                {
                    onFailure?.Invoke(webRequest.error);
                }
            }
        }
        else
        {
            yield return null;
            //Application.Quit();
            //ApiAndRoomManager._instance.LoadingObject.SetActive(false);

        }
    }
    private static Dictionary<string, TaskCompletionSource<Texture2D>> DownloadQueue = new Dictionary<string, TaskCompletionSource<Texture2D>>();
    private static Dictionary<string, List<Action<Texture2D>>> URL_CallBacks = new Dictionary<string, List<Action<Texture2D>>>();

    public static async void DownloadSprite(string url, Action<Texture2D> DownloadedImage, Action<string> OnFailed = null)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            if (DownloadQueue.ContainsKey(url))
            {
                // A download is already in progress for this URL, so skip the new request.
                //("Download already in progress for: " + url).Show();
                URL_CallBacks[url].Add(DownloadedImage);

                return;
            }

            // Track the task for this URL
            TaskCompletionSource<Texture2D> taskCompletionSource = new TaskCompletionSource<Texture2D>();
            DownloadQueue[url] = taskCompletionSource;
            if (!URL_CallBacks.ContainsKey(url))
            {
                URL_CallBacks[url] = new List<Action<Texture2D>>();
            }
            // Add the current callback to the list
            URL_CallBacks[url].Add(DownloadedImage);
            Texture2D texture = GetTexture(url);
            if (texture)
            {
                // Every invoke is isolated and the bookkeeping runs in a finally — see the comment on the
                // download branch below for why a single throwing callback used to poison this URL forever.
                try
                {
                    foreach (var callback in URL_CallBacks[url])
                    {
                        try { callback?.Invoke(texture); }
                        catch (Exception cbEx) { Debug.LogWarning("[ServerConnection] DownloadSprite cached callback threw (target likely destroyed): " + cbEx.Message); }
                    }
                    //("Done Loading Texture").Show();
                    try { DownloadedImage?.Invoke(GetTexture(url)); }
                    catch (Exception cbEx) { Debug.LogWarning("[ServerConnection] DownloadSprite cached DownloadedImage threw (target likely destroyed): " + cbEx.Message); }
                    taskCompletionSource.TrySetResult(texture);  // Mark the task as completed
                }
                finally
                {
                    DownloadQueue.Remove(url);
                    URL_CallBacks.Remove(url);
                }
            }
            else
            {
                UnityWebRequest Request = UnityWebRequestTexture.GetTexture(Main_URL() + url);
                Request.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                AttachAppCheck(Request);
                await Task.Yield(); // Ensure this runs asynchronously

                // Send the request and await its completion
                await SendRequestAsync(Request);
                // The await means these callbacks land an arbitrary time later — routinely AFTER a scene
                // reload (reconnect, or the opponent leaving) has destroyed the Image they were going to
                // write to. The assignment then throws inside the loop, and the damage was never the throw
                // itself:
                //   * it aborted the whole foreach, so every OTHER avatar queued on the same URL was skipped;
                //   * it skipped the two Remove calls below, leaving the URL in DownloadQueue permanently.
                //     From then on every DownloadSprite for it hit the "already in progress" early-return and
                //     just appended a callback that could never be invoked — that avatar was dead for the rest
                //     of the app session, and anything waiting on it waited forever.
                //   * being async void, it surfaced as an unhandled ThrowAsync on the sync context.
                // Seen 4x at the end of the 06-08 session (ServerConnection.DownloadSprite ->
                // Image.set_sprite -> Behaviour.get_isActiveAndEnabled NRE), right where the tester reported
                // the opponent stuck on the game screen after the other side left.
                // Each invoke is isolated, and the bookkeeping moves into a finally so the URL is always freed.
                try
                {
                    if (Request.result != UnityWebRequest.Result.Success)
                    {
                        try { OnFailed?.Invoke(Request.error); }
                        catch (Exception cbEx) { Debug.LogWarning("[ServerConnection] DownloadSprite OnFailed threw: " + cbEx.Message); }
                        taskCompletionSource.TrySetException(new Exception(Request.error));
                    }
                    else
                    {
                        Texture2D textured = DownloadHandlerTexture.GetContent(Request);
                        if (URL_CallBacks.TryGetValue(url, out var pendingCallbacks))
                        {
                            foreach (var callback in pendingCallbacks)
                            {
                                try { callback?.Invoke(textured); }
                                catch (Exception cbEx) { Debug.LogWarning("[ServerConnection] DownloadSprite callback threw (target likely destroyed by a scene reload): " + cbEx.Message); }
                            }
                        }
                        SaveTexture(textured, url);
                        try { DownloadedImage?.Invoke(textured); }
                        catch (Exception cbEx) { Debug.LogWarning("[ServerConnection] DownloadSprite DownloadedImage threw (target likely destroyed by a scene reload): " + cbEx.Message); }
                        taskCompletionSource.TrySetResult(textured);
                    }
                }
                finally
                {
                    DownloadQueue.Remove(url);
                    URL_CallBacks.Remove(url);
                }
            }
        }
    }
    private static async Task SendRequestAsync(UnityWebRequest request)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        request.SendWebRequest().completed += (asyncOp) =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                tcs.SetResult(true);
            }
            else
            {
                tcs.SetResult(false);
            }
        };
        await tcs.Task;
    }
    public static Texture2D GetTexture(string filename)
    {
        string path = Path.Combine(Application.persistentDataPath, filename);

        if (!File.Exists(path))
        {
            return null;
        }

        // Read the file bytes
        byte[] fileData;
        try
        {
            fileData = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            ($"Failed to read file at: {path}, Error: {ex.Message}").Show();
            return null;
        }

        // Create a new Texture2D and load the image data into it
        var loadedTexture = new Texture2D(2, 2); // Dimensions don't matter, will be updated by LoadImage

        if (loadedTexture.LoadImage(fileData))
        {
            // Successfully loaded texture
            return loadedTexture;
        }
        else
        {
            ("Failed to load texture from image data.").Show();
            return null;
        }
    }

    public static void SaveTexture(Texture2D texture, string filename)
    {
        byte[] textureBytes = texture.EncodeToPNG(); // or EncodeToJPG() if you prefer JPG format        
        string path = Path.Combine(Application.persistentDataPath, filename.Replace('/', '_'));
        File.WriteAllBytes(path, textureBytes);
    }
    // Function to make a POST request
    public static IEnumerator PostApiRequest(string url, Dictionary<string, string> jsonData, Action<string> onSuccess = null, Action<string> onFailure = null, bool withSettlementAuth = false)
    {
        //($"Harpis{staticVariables.isAppCheckVerified} {!staticVariables.isGuest}").Show();
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            //("Harpis").Show();
            // ApiAndRoomManager._instance.LoadingObject.SetActive(true);
            if (ApiAndRoomManager._instance.LoadingObject.transform.childCount > 0)
            {
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                {
                    if (EventSystem.current.currentSelectedGameObject.transform == null)
                    {
                        ("The selected GameObject's transform is null").Show();
                    }
                    else
                    {
                        //ApiAndRoomManager._instance.LoadingObject.transform.GetChild(0).gameObject.transform.position = EventSystem.current.currentSelectedGameObject.transform.position;
                    }
                }

            }
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name != "LoginScene")
            {
                if (staticVariables.UserProfiledata.user.user_login_token != staticVariables.uniqueGameIdentifier)
                {
                    ("Deleteing Pref").Show();
                }
            }
            string json = JsonConvert.SerializeObject(jsonData);
            json.Show("JSON Send");
            using (UnityWebRequest webRequest = UnityWebRequest.Post(Main_URL() + url, json, "application/json"))
            {
                webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                if (staticVariables.UserProfiledata != null && !string.IsNullOrEmpty(staticVariables.UserProfiledata.access_token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + staticVariables.UserProfiledata.access_token);
                }
                webRequest.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                AttachAppCheck(webRequest);
                if (withSettlementAuth && !string.IsNullOrEmpty(SettlementAuthKey))
                    webRequest.SetRequestHeader(SETTLEMENT_AUTH_HEADER, SettlementAuthKey);
                yield return webRequest.SendWebRequest();
                //("<color=green> Url =" + url + "</color>").Show();
                (" data = " + webRequest.downloadHandler.text).Show();
                if (webRequest.downloadHandler.text.Contains("Unauthorized"))
                {
                    PlayerPrefs.DeleteKey("userModel");
                    if (!Utils.IsHeadless())
                        SceneManager.LoadScene(1);
                }
                if (webRequest.result == UnityWebRequest.Result.Success)
                {

                    //ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                }
                else
                {
                    //  ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onFailure?.Invoke(webRequest.error);
                    //("Web Request" + webRequest.error).Show();

                }
            }
        }
        else
        {
            yield return null;
            // ApiAndRoomManager._instance.LoadingObject.SetActive(false);

        }
    }
    public static IEnumerator PostApiRequest(string url, Dictionary<object, object> jsonData, Action<string> onSuccess = null, Action<string> onFailure = null, bool withSettlementAuth = false)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            //ApiAndRoomManager._instance.LoadingObject.SetActive(true);
            //if (ApiAndRoomManager._instance.LoadingObject.transform.childCount > 0)
            //{
            //    if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            //    {
            //        if (EventSystem.current.currentSelectedGameObject.transform == null)
            //        {
            //            ("The selected GameObject's transform is null").Show();
            //        }
            //        else
            //        {
            //            ("Event system pos: " + EventSystem.current.currentSelectedGameObject.transform.position).Show();
            //            ApiAndRoomManager._instance.LoadingObject.transform.GetChild(0).gameObject.transform.position = EventSystem.current.currentSelectedGameObject.transform.position;
            //        }
            //    }

            //}
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name != "LoginScene")
            {
                if (staticVariables.UserProfiledata.user.user_login_token != staticVariables.uniqueGameIdentifier)
                {
                    ("Deleteing Pref").Show();
                }
            }
            string json = JsonConvert.SerializeObject(jsonData);
            (json).Show();
            using (UnityWebRequest webRequest = UnityWebRequest.Post(Main_URL() + url, json, "application/json"))
            {
                webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                if (staticVariables.UserProfiledata != null && !string.IsNullOrEmpty(staticVariables.UserProfiledata.access_token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + staticVariables.UserProfiledata.access_token);
                }
                webRequest.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                AttachAppCheck(webRequest);
                if (withSettlementAuth && !string.IsNullOrEmpty(SettlementAuthKey))
                    webRequest.SetRequestHeader(SETTLEMENT_AUTH_HEADER, SettlementAuthKey);
                yield return webRequest.SendWebRequest();
                ("" + Main_URL() + url).Show("URL");
                ("<color=yellow> data = " + webRequest.downloadHandler.text + "</color>").Show();
                (" data = " + webRequest.downloadHandler.text).Show();
                if (webRequest.downloadHandler.text.Contains("Unauthorized Token is missing."))
                {
                    PlayerPrefs.DeleteKey("userModel");
                    if (!Utils.IsHeadless())
                        SceneManager.LoadScene(1);
                }
                if (webRequest.result == UnityWebRequest.Result.Success)
                {

                    // ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                }
                else
                {
                    // ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onFailure?.Invoke(webRequest.error);
                    //("Web Request" + webRequest.error).Show();

                }
            }
        }
        else
        {
            yield return null;
            // ApiAndRoomManager._instance.LoadingObject.SetActive(false);

        }
    }

    public static IEnumerator PostApiRequestWithForm(string url, WWWForm form, Action<string> onSuccess, Action<string> onFailure)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            // ApiAndRoomManager._instance.LoadingObject.SetActive(true);

            Scene currentScene = SceneManager.GetActiveScene();
            using (UnityWebRequest webRequest = UnityWebRequest.Post(Main_URL() + url, form))
            {
                webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                if (staticVariables.UserProfiledata != null && !string.IsNullOrEmpty(staticVariables.UserProfiledata.access_token))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + staticVariables.UserProfiledata.access_token);
                webRequest.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                AttachAppCheck(webRequest);
                yield return webRequest.SendWebRequest();
                if (webRequest.downloadHandler.text.Contains("Unauthorized Token is missing."))
                {
                    PlayerPrefs.DeleteKey("userModel");
                    if (!Utils.IsHeadless())
                        SceneManager.LoadScene(1);
                }
                //("<color=green> Url =" + Main_URL() + url + "</color>").Show();
                //("<color=yellow> data = " + webRequest.downloadHandler.text + "</color>").Show();
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    //ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                }
                else
                {
                    onFailure?.Invoke(webRequest.error);
                    // ApiAndRoomManager._instance.LoadingObject.SetActive(false);

                }
            }
        }
        else
        {
            yield return null;
            //  ApiAndRoomManager._instance.LoadingObject.SetActive(false);

        }
    }
    public static IEnumerator PatchApiRequest(string url, Dictionary<string, string> jsonData, Action<string> onSuccess, Action<string> onFailure)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            (url).Show();
            (EventSystem.current.currentSelectedGameObject.name + " event object name ").Show();
            //ApiAndRoomManager._instance.LoadingObject.SetActive(true);
            //  ApiAndRoomManager._instance.LoadingObject.transform.position = EventSystem.current.currentSelectedGameObject.transform.position;
            string json = JsonConvert.SerializeObject(jsonData);

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            // Using UnityWebRequest for PATCH instead of PUT
            using (UnityWebRequest webRequest = new UnityWebRequest(Main_URL() + url, "PATCH"))
            {
                // Upload handler for sending the JSON data
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                // Set headers
                webRequest.SetRequestHeader("Content-Type", "application/json");
                if (staticVariables.UserProfiledata != null && !string.IsNullOrEmpty(staticVariables.UserProfiledata.access_token))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + staticVariables.UserProfiledata.access_token);
                webRequest.SetRequestHeader("apk_signature_black_arch", secrets["apk_signature_black_arch"]);
                AttachAppCheck(webRequest);
                // Send the request
                yield return webRequest.SendWebRequest();

                // Check for a successful response
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    if (webRequest.downloadHandler.text.Contains("Unauthorized Token is missing."))
                    {
                        PlayerPrefs.DeleteKey("userModel");
                        if (!Utils.IsHeadless())
                            SceneManager.LoadScene(1);
                    }

                    //("<color=green>" + url + "</color>, <color=red>" + webRequest.downloadHandler.text + "</color>").Show();
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                    //ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                }
                else
                {
                    // ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    //("<color=green>" + url + "</color>, <color=red>" + webRequest.error + "</color>").Show();

                    onFailure?.Invoke(webRequest.error);
                }
            }
        }
        else
        {
            yield return null;
            // ApiAndRoomManager._instance.LoadingObject.SetActive(false);

        }
    }

    public static IEnumerator PutApiRequest(string url, string jsonData, Action<string> onSuccess, Action<string> onFailure)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            (EventSystem.current.currentSelectedGameObject.name + " event object name ").Show();
            //ApiAndRoomManager._instance.LoadingObject.SetActive(true);
            // ApiAndRoomManager._instance.LoadingObject.transform.position = EventSystem.current.currentSelectedGameObject.transform.position;
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest webRequest = UnityWebRequest.Put(url, jsonData))
            {
                webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                AttachAppCheck(webRequest);
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    if (webRequest.downloadHandler.text.Contains("Unauthorized Token is missing."))
                    {
                        PlayerPrefs.DeleteKey("userModel");
                        if (!Utils.IsHeadless())
                            SceneManager.LoadScene(1);
                    }
                    //("<color=green>" + url + "</color>, <color=red>" + webRequest.downloadHandler.text + "</color>").Show();

                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                    //ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                }
                else
                {
                    // ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onFailure?.Invoke(webRequest.error);
                }
            }
        }
        else
        {
            yield return null;
        }
    }
    public static IEnumerator PostRequestobject(string url, RouletteRound jsonData, Action<string> onSuccess = null, Action<string> onFailure = null)
    {
        if (staticVariables.isAppCheckVerified && !staticVariables.isGuest)
        {
            //Debug.Log(EventSystem.current.currentSelectedGameObject.name + " event object name ");
            ApiAndRoomManager._instance.LoadingObject.SetActive(true);
            if (ApiAndRoomManager._instance.LoadingObject.transform.childCount > 0)
            {
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                {
                    if (EventSystem.current.currentSelectedGameObject.transform == null)
                    {
                        Debug.Log("The selected GameObject's transform is null");
                    }
                    else
                    {
                        Debug.Log("Event system pos: " + EventSystem.current.currentSelectedGameObject.transform.position);
                        ApiAndRoomManager._instance.LoadingObject.transform.GetChild(0).gameObject.transform.position = EventSystem.current.currentSelectedGameObject.transform.position;
                    }
                }

            }
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name != "LoginScene")
            {
                //Debug.Log("Active scene is login");
                if (staticVariables.UserProfiledata.user.user_login_token != staticVariables.uniqueGameIdentifier)
                {
                    //   SceneLoaderUtility.LoadScene("LoginScene");
                    Debug.Log("Deleteing Pref");
                    //PlayerPrefs.DeleteAll();
                }
            }
            string json = JsonConvert.SerializeObject(jsonData);
            Debug.Log(json);
            using (UnityWebRequest webRequest = UnityWebRequest.Post(Main_URLLottery + url, json, "application/json"))
            {
                webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                if (staticVariables.UserProfiledata != null && !string.IsNullOrEmpty(staticVariables.UserProfiledata.access_token))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + staticVariables.UserProfiledata.access_token);

                webRequest.SetRequestHeader("apk_signature_black_arch", "077586297b5a8145c6317f821a5cc23d390944c87de714c65de476712f9adca7");
                AttachAppCheck(webRequest);
                yield return webRequest.SendWebRequest();
                //Wasi    Debug.Log("<color=green> Url =" + url + "</color>");
                //Wasi     Debug.Log("<color=yellow> data = " + webRequest.downloadHandler.text + "</color>");
                if (webRequest.downloadHandler.text.Contains("Unauthorized Token is missing."))
                {
                    PlayerPrefs.DeleteKey("userModel");
                    if (!Utils.IsHeadless())
                        SceneManager.LoadScene(1);
                }
                if (webRequest.result == UnityWebRequest.Result.Success)
                {

                    ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                }
                else
                {
                    ApiAndRoomManager._instance.LoadingObject.SetActive(false);
                    onFailure?.Invoke(webRequest.error);
                    Debug.LogError("Web Request" + webRequest.error);
                }
            }
        }
        else
        {
            yield return null;
            //Application.Quit();
        }
    }



    //09098

    #region API Urls 

    public static string Main_URL() => "https://api-player.gamespanda.net";

    public static string Main_URLLottery = "https://api-player.gamespanda.net";
    public static string TimeZoneApi_Url() => "https://timeapi.io/api/Time/current/zone?timeZone=Europe/London";
    public static string ServerTime_Url() => "/games/standard/server-time";
    public static string UpdateProfile_Url() => "/user/mobile/profile/";
    public static string UpdatePassword_Url() => "/user/mobile/update/password";
    public static string Create_Bet_Url() => "/bets/create-new-bet";
    public static readonly string End_RoundAI_casino_Url = "/casinos/handle-rounds-ai";
    public static readonly string Start_Round_casino_Url = "/casinos/create-new-round/";

    public static string Join_Bet_Url() => "/bets/join";
    public static string Start_Bet_Url() => "/session/play/";
    public static string Update_Bet_Url() => "/bets/iam-playing/";
    public static string Leave_Bet_Url() => "/bets/leave-bet/";
    public static string Reject_Bet_Url() => "/bets/reject-bet/";
    public static string Delete_Bet_Url() => "/bets/delete-bet/";
    public static string Ignore_Bet_Url() => "/bets/ignore-bet/";
    public static string Winner_Bet_Url() => "/session/session-winner/";
    public static string Winner_AIBet_Url() => "/session/session-ai-win-loss/";
    public static string DrawBet_Url() => "/session/update-status/to-draw/";
    public static string ChallengeStatus_Url() => "/session/get/session-status/";
    public static readonly string End_Round_casino_Url = "/casinos/handle-rounds";

    public static readonly string Create_casino_Url = "/casinos/create-new-bet";
    public static readonly string Join_casino_Url = "/casinos/join";

    public static string Rematch_Url() => "/bets/request/re-match/";

    public static string Validate_URL() => "/bets/request/validate-re-match/";

    public static string Notify_BetPlay_Url() => "/bets/notify-to-play/user";
    public static string purchase() => "/in-app-purchase";
    public static string getBetRequest() => "/bets/bets-info/get-bet-players/";
    //roulettes
    public static string Create_roulette_Url = "/roulettes/create-new-bet";
    public static string Start_Round_roulette_Url = "/roulettes/create-new-round/";
    public static string End_Round_roulette_Url = "/roulettes/handle-round";
    public static string End_roulette_Url = "/roulettes/game-end/";
    //Big Wheel
    public static string Create_BigWheel_Url = "/big-wheels/create-new-bet";
    public static string Start_Round_BigWheel_Url = "/big-wheels/create-new-round/";
    public static string End_Round_BigWheel_Url = "/big-wheels/handle-round";
    public static string End_BigWheel_Url = "/big-wheels/game-end/";
    // Casino
    public static string Bet_info_Url() => "/bets/getBetsInfo/";
    public static string Shop_Packages_Url() => "/packages/country/mobile/pakistan";
    public static string Update_FirebaseToken_Url() => "/user/mobile/profile/";

    public static string Social_Login() => "/auth/login/social";
    public static string Social_Otp_Generate() => "/auth/otp-generate/social";
    public static string User_Balance_Url() => "/user/get-my-balances/";
    public static string Support_Contacts_Url() => "/whatsapp/find/country/";
    public static string Friend_Request_Send_Url() => "/friends/request";
    public static string Friend_Request_Accept_Url() => "/friends/accept";
    public static string Friend_Request_Reject_Url() => "/friends/reject";
    public static string Friend_Requests_Pending_Url() => "/friends/my-pending/";
    public static string Friend_List_Url() => "/friends/my-friends/";
    public static string IsFriend_Url() => "/friends/is-my-friends/";
    public static string Notification_List_Url() => $"/notifications?_user_id={staticVariables.UserProfiledata?.user?._id}&_per_page=50&_page=";
    public static string Notification_Read_Url() => $"/notifications/read/";
    public static string Notification_Delete_Url() => $"/notifications/delete/";

    public static string VersionCheckURL() => "/google-play/version?v=";
    public static string GetUser_Info_Url() => "/user/";
    public static string createPlayerUrl() => "/user/create";
    public static string ValidateOtpUrl() => "/user/validate-otp";
    public static string ResetPasswordUrl() => "/auth/update-password";
    public static string GenereateOtpUrl() => "/auth/phone-otp/";
    public static string logout() => "/user/destroy-session";
    public static string GameHistoryUrl() => "/games/player-wise/history";
    public static string gameDetailUrl() => "/games/mobile/";
    public static string ClientBanner_Url() => "/banner-collections/get/user-banners";
    public static string requestReset_otpGenerator() => "/auth/request/update-password";
    public static string ValidateNewOtp() => "/auth/validate/otp-reset-password";
    public static string UpdatePasswordNewApi() => "/auth/update/reset-password";
    public static string CreateSessionCoinsPayment() => "/crypto-coins/generate-session/";

    public static string GiftSend() => "/gifts";
    public static string GiftMothlyLimitationLeft() => "/gifts/get-my-monthly-limits";
    public static string GiftClaim() => "/gifts/claim-gift/";
    public static string GiftOfSender() => "/gifts/list/sender";
    public static string GiftOfReciever() => "/gifts/list/receiver";

    #endregion

}

