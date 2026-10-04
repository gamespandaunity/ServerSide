using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Text;
using System;
using System.Linq;

using static staticVariables;
using static ServerConnection;
using System.Globalization;
using UnityEngine.Events;
//using static PunNetwork;

using Facebook.Unity;
using NaughtyAttributes;
using static LoginUIManager;
using Mirror;
using SimpleJSON;
using UnityExtensions;
//using SimpleJSON;


public class ApiAndRoomManager : MonoBehaviour
{

    public static event Action<UserBalanceResponse> OnCoinsUpdated;
    public static UserBalanceResponse LastFetchedCoins;
    public static int currentGameId = 1;
    public static ApiAndRoomManager _instance;

    public CreateBetResponseModel currentBetData;

    public string challenge_transaction_id;
    public string MessageContent;
    public string currentRoomId = "";
    public string winLoseChallengeId = "";
    public string userIdentifierOrEmail = "";

    public GameObject LoadingObject;
    public AddressablesBundleSpawning addressablesManager;
    private Dictionary<(int, int), bool> originalStates = new();
    private readonly HashSet<string> submittedDrawTransactions = new(StringComparer.Ordinal);


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SaveOriginalStates();
        }
        else
        {
            Destroy(gameObject);
        }

    }
    private void Start()
    {
        if (Utils.IsHeadless())
        {
            this.Delay(3, () =>
            {
                GetAnonymousBuildToken(onSuccess =>
           {
               onSuccess.Show("Api and Room Manager get token");

               var node = JSON.Parse(onSuccess);

               // Get access_token directly
               string token = node["access_token"];
               token.Show("Token");
               staticVariables.UserProfiledata.access_token = token;
               // Settlement reads this copy instead: the profile field gets reloaded and cleared
               // during a match, which is what sent an expired token to the settlement endpoint.
               ServerConnection.BuildAuthToken = token;
               Debug.Log("Access Token: " + token);
           });
            });

        }
    }
    void SaveOriginalStates()
    {
        for (int i = 0; i < 32; i++)
        {
            for (int j = i; j < 32; j++)
            {
                bool ignores = Physics.GetIgnoreLayerCollision(i, j);
                originalStates[(i, j)] = ignores;
            }
        }
    }
    public void RevertToOriginalStates()
    {
        foreach (var entry in originalStates)
        {
            int layerA = entry.Key.Item1;
            int layerB = entry.Key.Item2;
            bool original = entry.Value;
            Physics.IgnoreLayerCollision(layerA, layerB, original);
        }

        Debug.Log("Layer collision settings reverted to original states.");
    }
    public void DisplayError(string message)
    {
        GameObject errorpopup = Instantiate(Resources.Load("ErrorPopUp")) as GameObject;

        errorpopup.GetComponent<ErrorPopUp>().InitMessage(message);
    }

    public void SocialLogin(Dictionary<string, string> socialLoginparam, Action<string> OnSuccess = null, Action<string> Failed = null)
    {
        StartCoroutine(PostApiRequest(Social_Login(), socialLoginparam, OnSuccess, Failed));
    }
    public void SendRequestToFriends(string id, Action<string> OnSuccess = null, Action<string> Failed = null)
    {
        Dictionary<string, string> Requestbody = new Dictionary<string, string>();

        Requestbody["request_by"] = UserProfiledata.user._id.ToString();
        Requestbody["requested"] = id;
        StartCoroutine(PostApiRequest(Friend_Request_Send_Url(), Requestbody, OnSuccess, Failed));
    }
    public void ApproveFriendRequest(string id, Action<string> OnSuccess = null, Action<string> Failed = null)
    {

        Dictionary<string, string> Requestbody = new Dictionary<string, string>();

        Requestbody["accepted_by"] = UserProfiledata.user._id.ToString();
        Requestbody["request_id"] = id;

        StartCoroutine(PostApiRequest(Friend_Request_Accept_Url(), Requestbody, OnSuccess, Failed));
    }
    public void DeclineFriendRequest(string id, Action<string> OnSuccess = null, Action<string> Failed = null)
    {
        Dictionary<string, string> Requestbody = new Dictionary<string, string>();
        Requestbody["rejected_by"] = UserProfiledata.user._id.ToString();
        Requestbody["request_id"] = id;
        StartCoroutine(PostApiRequest(Friend_Request_Reject_Url(), Requestbody, OnSuccess, Failed));
    }
    public void UnfriendUser(string id)
    {
        Dictionary<string, string> Requestbody = new Dictionary<string, string>();
        Requestbody["user_id"] = UserProfiledata.user._id.ToString();
        Requestbody["request_id"] = id;
        StartCoroutine(PostApiRequest(Friend_Request_Reject_Url(), Requestbody));
    }

    public void ModifyUserBalance(UnityAction<UserBalanceResponse> OnBalanceUpdate = null)
    {
        if (!staticVariables.isGuest)
        {
            UserBalanceResponse coins = new UserBalanceResponse();
            coins.data.gold_balance = LastFetchedCoins.data.gold_balance;
            coins.data.silver_balance = LastFetchedCoins.data.silver_balance;
            if (OnCoinsUpdated != null)
                OnCoinsUpdated(coins);
        }
        StartCoroutine(GetApiRequest(User_Balance_Url() + UserProfiledata.user._id, Balance =>
        {

            UserBalanceResponse Userbalnace = JsonUtility.FromJson<UserBalanceResponse>(Balance);

            //LastFetchedUserCoins = Userbalnace;
            if (OnCoinsUpdated != null)
                OnCoinsUpdated(Userbalnace);
            if (OnBalanceUpdate != null)
                OnBalanceUpdate(Userbalnace);
            if (Borderspanel.balanceUpdate != null)
                Borderspanel.balanceUpdate(Userbalnace);
            if (int.Parse(Userbalnace.data.gold_balance) < 0)
            {

            }
            LastFetchedCoins.data.gold_balance = Userbalnace.data.gold_balance;
            LastFetchedCoins.data.silver_balance = Userbalnace.data.silver_balance;

        }));
    }
    public void PlaceChallenge(Dictionary<string, string> betRequest, Action<string> callbackSucess = null, Action<string> Callbackfailed = null)
    {
        callbackSucess += SucessResult =>
        {
            CreateBetResponseModel userModeli = JsonUtility.FromJson<CreateBetResponseModel>(SucessResult);
            if (userModeli.status)
            {
                currentBetData = userModeli;
                challenge_transaction_id = userModeli.data.transaction_id;
                winLoseChallengeId = userModeli.data.transaction_id;
                Debug.Log("Challenge Placed with ID: " + userModeli.data.transaction_id);

            }
            else
            {
                DisplayError(userModeli.message);
                Callbackfailed?.Invoke(userModeli.message);
            }
        };
        string category = GamesSetterMenu.All_games_Ref.Find(x => x.game_id == currentGameId).category;

        StartCoroutine(PostApiRequest(Create_Bet_Url(), betRequest, callbackSucess, Callbackfailed));
    }

    public void PlaceAIChallenge(Dictionary<string, string> betRequest, Action<string> callbackSucess = null, Action<string> Callbackfailed = null)
    {
        Physics.gravity = new Vector3(0, -9.81f, 0);
        //Photon Removal PunNetwork.instance.IsAIControlledHorse = false;

        //Reconnect.isReconnectig = false;
        if (!staticVariables.isGuest)
        {
            callbackSucess += SucessResult =>
            {
                CreateBetResponseModel userModeli = JsonUtility.FromJson<CreateBetResponseModel>(SucessResult);
                if (userModeli.status)
                {
                    currentBetData = userModeli;
                    challenge_transaction_id = userModeli.data.transaction_id;
                    winLoseChallengeId = userModeli.data.transaction_id;
                    switch (currentGameId)
                    {
                        case 2:
                            Debug.Log("LudoGame");
                            Snake_Ladder.SnakeGameManager.instance.currentGameMode = Snake_Ladder.SnakeGameManager.GameMode.Against_Ai;
                            SnakeGameConstants.isWithAI = true;
                            SceneManager.LoadScene("LudoGameAI");
                            break;
                        case 3:
                            Debug.Log("teenPatti");
                            MatchHandler.CurrentMatch = MatchHandler.MATCH.OffLine;
                            LocalSettings.SetPlayername(staticVariables.userNickName);
                            PlayerPrefs.SetString(LocalSettings.TotalChips, LastFetchedCoins.data.silver_balance.ToString());
                            Debug.Log("Coins To Start With :" + LocalSettings.TotalChips);
                            PlayerPrefs.SetString("CashInHand", PlayerPrefs.GetString(LocalSettings.TotalChips));
                            PlayerPrefs.Save();
                            SceneManager.LoadScene("GameplayTeenPatti");
                            break;
                        case 5:
                            Debug.Log("Roulette");
                            SceneManager.LoadScene("Roulette_Ready");

                            break;
                        case 6:
                            Debug.Log("Snake AI");
                            SceneLoaderUtility.LoadScene("SnakeAi");
                            break;


                        case 9:
                            Debug.Log("BigWheel");
                            break;

                        case 15:
                            Debug.Log("Snooker");
                            SnokerNetwork.IsMultiplayer = false;
                            Physics.gravity = new Vector3(0, -19.81f, 0);
                            SceneManager.LoadScene("SnokkerAI");
                            break;
                    }

                }
                else
                {
                    DisplayError(userModeli.message);
                    Callbackfailed?.Invoke(userModeli.message);
                }
            };
        }
        else
        {
            switch (currentGameId)
            {
                case 2:
                    Debug.Log("LudoGame");
                    Snake_Ladder.SnakeGameManager.instance.currentGameMode = Snake_Ladder.SnakeGameManager.GameMode.Against_Ai;
                    SnakeGameConstants.isWithAI = true;
                    SceneManager.LoadScene("LudoGameAI");
                    break;
                case 3:
                    Debug.Log("teenPatti");
                    MatchHandler.CurrentMatch = MatchHandler.MATCH.OffLine;
                    LocalSettings.SetPlayername(staticVariables.userNickName);
                    PlayerPrefs.SetString(LocalSettings.TotalChips, LastFetchedCoins.data.silver_balance.ToString());
                    Debug.Log("Coins To Start With :" + LocalSettings.TotalChips);
                    PlayerPrefs.SetString("CashInHand", PlayerPrefs.GetString(LocalSettings.TotalChips));
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("GameplayTeenPatti");
                    break;
                case 5:
                    Debug.Log("Roulette");
                    SceneManager.LoadScene("Roulette_Ready");

                    break;
                case 6:
                    Debug.Log("Snake AI");
                    SceneLoaderUtility.LoadScene("SnakeAi");
                    break;

                case 9:
                    Debug.Log("BigWheel");
                    break;

                case 15:
                    Debug.Log("Snooker");
                    SnokerNetwork.IsMultiplayer = false;
                    Physics.gravity = new Vector3(0, -19.81f, 0);
                    SceneManager.LoadScene("SnokkerAI");
                    break;

            }
        }
        string category = GamesSetterMenu.All_games_Ref.Find(x => x.game_id == currentGameId).category;
        StartCoroutine(PostApiRequest(Create_Bet_Url(), betRequest, callbackSucess, Callbackfailed));
    }
    public void updatedeviceuniqueIdentifier() => StartCoroutine(updateDeviceToken_coroutine());
    [Obsolete]
    public IEnumerator updateDeviceToken_coroutine()
    {

        UserModel userModel2 = staticVariables.UserProfiledata;
        string uri = $"{Main_URL()}/user/mobile/profile/{userModel2.user._id}";
        print("uriuri 111" + uri + ":::" + userModel2.access_token + ":::" + "::::" + staticVariables.firebaseToken);
        WWWForm form = new WWWForm();
        if (staticVariables.firebaseToken != "")
        {
            form.AddField("deviceToken", firebaseToken);
        }


        using (UnityWebRequest request = UnityWebRequest.Post(uri, form))
        {
            request.SetRequestHeader("Authorization", "Bearer " + userModel2.access_token);
            request.SetRequestHeader("apk_signature_black_arch", "077586297b5a8145c6317f821a5cc23d390944c87de714c65de476712f9adca7");
            yield return request.SendWebRequest();

            try
            {
                UserModel userModel = JsonUtility.FromJson<UserModel>(request.downloadHandler.text);
                userModel.access_token = UserProfiledata.access_token;
                staticVariables.UserProfiledata = userModel;
                PlayerPrefs.SetString("userModel", JsonUtility.ToJson(userModel));
                UserProfiledata = userModel;
                staticVariables.UserProfiledata = userModel;
                SceneLoaderUtility.LoadScene("Home");
                if (request.isNetworkError || request.isHttpError)
                {
                    print(" incorrect 8" + request.error);

                }
                else
                {
                    if (userModel.status)
                    {
                        if (userModel.user != null && UIMainMenManager.instance != null)
                        {
                            UIMainMenManager.instance.playerInfo.displayPlayerName.text = userModel.user.first_name.ToString() + " " + userModel.user.last_name.ToString();
                            UIMainMenManager.instance.playerInfo.playersId.text = "UserID: " + userModel.user._id.ToString();
                            UIMainMenManager.instance.silverCoinText.text = userModel.user.silver_balance.ToString();
                            UIMainMenManager.instance.goldCoinText.text = userModel.user.gold_balance.ToString();
                            if (userModel.user.file_url != null && userModel.user.file_url != "")
                            {
                                //  StartCoroutine(setImage(APIManager.instance.baseUrl + "/" + userModel.user.file_url));

                            }

                        }
                    }

                }

            }
            catch (Exception e) { }
        }
    }
    public CreateBetResponseModel userModeliq;

    public void create_Challenge_Invite(Dictionary<string, string> betRequest, Action<string> callbackSucess = null, Action<string> Callbackfailed = null)
    {
        callbackSucess += SucessResult =>
        {
            CreateBetResponseModel userModeli = JsonUtility.FromJson<CreateBetResponseModel>(SucessResult);
            userModeliq = userModeli;
            if (userModeli.status)
            {
                currentTime = userModeli.data.bet_expires_sec;
                WaitingPanelScript.WaitingForroomID = userModeli.data.transaction_id;
                winLoseChallengeId = userModeli.data.transaction_id;
                challenge_transaction_id = userModeli.data.transaction_id;
                currentBetData = userModeli;
                //Photon Removal   if(SceneManager.GetActiveScene().name!="Home")
                //Photon Removal PunNetwork.instance.CreateRoom(PunNetwork.ConfigureRoomSettings(false));  
            }
            else
            {
                DisplayError(userModeli.message);

                Callbackfailed?.Invoke(userModeli.message);
            }
        };
        //
        string category = GamesSetterMenu.All_games_Ref.Find(x => x.game_id == currentGameId).category;

        StartCoroutine(PostApiRequest(Create_Bet_Url(), betRequest, callbackSucess, Callbackfailed));

    }
    public void StartChallenge()
    {
        // Marking the session as played now happens here rather than on a player's device, and carries the
        // deployment auth key like the win/loss and draw calls. Three things had to change for it to work
        // headless:
        //   - the URL was still the stale /bets/play/ form; the live one is /session/play/;
        //   - it appended staticVariables.UserProfiledata.user._id, which is empty on a build that never
        //     logs anyone in, so the server would have asked the backend to start a session for user "";
        //   - it opened with GamesSetterMenu.All_games_Ref.Find(...).category, and that list is filled by the
        //     menu UI, which never runs here — Find returns null and the whole call would have thrown before
        //     reaching the request. The value was never used.
        // The path shape now matches what the player build was sending: /session/play/{transactionId}.
        ConstantsData_M.Log($"[Settlement] Server marking session {winLoseChallengeId} as played.");
        StartCoroutine(GetApiRequest(Start_Bet_Url() + winLoseChallengeId, onsucess =>
        {
            // HTTP 200 only means the request arrived — the backend refuses in the body
            // ({"status":false,"message":"invalid player"}), and the session then stays "pending".
            if (IsSettlementBodyFailure(onsucess))
                ConstantsData_M.LogCritical($"[Settlement] Session {winLoseChallengeId} play-start REFUSED by the backend body: {onsucess}");
            else
                ConstantsData_M.Log($"[Settlement] Session {winLoseChallengeId} accepted as played.");
        }, onFailure =>
        {
            ConstantsData_M.Log($"[Settlement] Session {winLoseChallengeId} play-start REJECTED: {onFailure}");
        }, withSettlementAuth: true));
    }
    public void FetchCurrentTime(Action<DateTime> OnTimeFetched, Action<string> OnFailedFetch)
    {
        StartCoroutine(GetTimeRequest(TimeZoneApi_Url(), SucessResult =>
        {
            TimeZoneInfo TimeFormApi = JsonUtility.FromJson<TimeZoneInfo>(SucessResult);
            ConstantsData_M.Log("Current date " + TimeFormApi.dateTime);
            string[] possibleFormats = new string[]
             {
                          "yyyy-MM-dd'T'HH:mm:ss.fffffff", // 7 fractional digits
                "yyyy-MM-dd'T'HH:mm:ss.ffffff",  // 6 fractional digits

             };
            DateTime parsedatetime = DateTime.Now;
            foreach (string format in possibleFormats)
            {
                if (DateTime.TryParseExact(TimeFormApi.dateTime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedatetime))
                {
                    DateTime dateTime = DateTime.ParseExact(TimeFormApi.dateTime, format, CultureInfo.InvariantCulture);

                    OnTimeFetched.Invoke(dateTime);
                    break;
                }
            }


        }, OnFailed =>
        {
            OnFailedFetch.Invoke(OnFailed);

        }));

    }
    [Button("Fetch Time")]
    public void fetchtime()
    {
        FetchCurrentTime((res) =>
        {
            Debug.Log("Time fetched successfully");
        }, OnFailed =>
        {
            Debug.LogError("Failed to fetch time: " + OnFailed);
        });
    }
    public void LeaveChallenge(string transaction_id)
    {
        StartCoroutine(GetApiRequest(Leave_Bet_Url() + transaction_id + "/" + UserProfiledata.user._id));

    }
    public void RejectChallenge(string transaction_id, Action<string> onSuccess = null, Action<string> onFail = null)
    {
        StartCoroutine(GetApiRequest(Reject_Bet_Url() + transaction_id + "/" + userIdentifierOrEmail, onSuccess, onFail));
    }
    public void IgnoretChallenge(string transaction_id, string userid)
    {
        StartCoroutine(GetApiRequest(Ignore_Bet_Url() + transaction_id + "/" + userid));
    }
    public void DeleteChallenge(string transaction_id, Action<string> callbackSucess = null)
    {
        AcceptNotificationInstance[] Activenotifications = FindObjectsOfType<AcceptNotificationInstance>();
        foreach (var notificationobj in Activenotifications)
        {
            if (notificationobj.notificationBody_.transaction_id == transaction_id)
            {
                Destroy(notificationobj.gameObject);
            }
        }
        StartCoroutine(GetApiRequest(Delete_Bet_Url() + transaction_id, callbackSucess));
    }
    public void notifyToplayChallenge(Dictionary<string, string> jsonData, Action<string> callbackSucess = null)
    {
        string category = GamesSetterMenu.All_games_Ref.Find(x => x.game_id == currentGameId).category;
        StartCoroutine(PostApiRequest(Notify_BetPlay_Url(), jsonData, callbackSucess));
    }
    /// <summary>
    /// True when the backend answered HTTP 200 but refused the settlement in the body. Treating that
    /// as success is how a rejected settlement got logged as "accepted by the backend" while nobody
    /// was actually paid.
    /// </summary>
    private static bool IsSettlementBodyFailure(string body)
    {
        if (string.IsNullOrEmpty(body)) return true;
        return body.Replace(" ", "").IndexOf("\"status\":false", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Mint a build token for the settlement that is about to go out.
    ///
    /// The backend keeps only the MOST RECENTLY issued build token valid: minting a new one silently
    /// kills the previous. Verified by issuing two in a row -- the first stops working the instant the
    /// second exists. This process took its token once at startup, so anything that minted one
    /// afterwards (another instance coming up, a restart, a second editor) left this server holding a
    /// dead token, and the settlement five minutes later answered 401 with a perfectly good
    /// gameplaytoken. Taking a fresh one immediately before the request closes that window.
    /// </summary>
    private IEnumerator RefreshBuildToken()
    {
        yield return GetBearerApiRequest("/auth/generate/build-token", json =>
        {
            try
            {
                string token = JSON.Parse(json)["access_token"];
                if (string.IsNullOrEmpty(token)) return;
                ServerConnection.BuildAuthToken = token;
                if (staticVariables.UserProfiledata != null)
                    staticVariables.UserProfiledata.access_token = token;
            }
            catch (Exception e)
            {
                ConstantsData_M.Log($"[Settlement] Could not read the refreshed build token: {e.Message}");
            }
        },
        fail => ConstantsData_M.Log($"[Settlement] Build-token refresh failed ({fail}); falling back to the token already held."));
    }

    /// <summary>
    /// Send one settlement request under a fresh build token, and give it a second try if the backend
    /// still answers 401 -- another instance can mint a token in the gap and kill ours again.
    /// </summary>
    private IEnumerator SubmitSettlement(string url, string what, Action onFailed = null)
    {
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            yield return RefreshBuildToken();

            string body = null;
            string failure = null;
            yield return GetApiRequest(url, ok => body = ok, err => failure = err, withSettlementAuth: true);

            if (failure == null)
            {
                if (IsSettlementBodyFailure(body))
                {
                    onFailed?.Invoke();
                    ConstantsData_M.LogCritical($"[Settlement] {what} was REFUSED by the backend body: {body}");
                }
                else
                {
                    ConstantsData_M.Log($"[Settlement] {what} accepted by the backend.");
                }
                yield break;
            }

            if (attempt == 1 && failure.Contains("401"))
            {
                ConstantsData_M.Log($"[Settlement] {what} answered 401 -- the build token was already stale. Minting another and retrying once.");
                continue;
            }

            onFailed?.Invoke();
            ConstantsData_M.LogCritical($"[Settlement] {what} REJECTED: {failure}");
            yield break;
        }
    }

    /// <summary>
    /// True only inside 12 Beads, Carrom and 8 Ball Pool -- the three games this settlement rework was
    /// asked for. Every other game must keep submitting exactly the request it always sent, so a fix
    /// made for 12 Beads cannot change how Ludo, Cricket, Snooker or Snake settle.
    /// </summary>
    private static bool IsScopedSettlementScene()
    {
        return NetworkGameManager.IsExplicitLeaveSettlementScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void WinnerLossChallenge(string WinnerID)
    {
        ConstantsData_M.Log($"Win Loss {winLoseChallengeId} Winner ID {WinnerID}");
        if (SnokerNetwork.IsMultiplayer)
        {
            if (NetworkServer.active)
            {
                winLoseChallengeId = NetworkGameManager.Instance.transactionId;
            }
        }
        // Settlement now runs on this headless build instead of a player's device, and carries the
        // deployment auth key so the backend can tell a real game server from anyone replaying the call.
        // The player build no longer sends this request at all.
        ConstantsData_M.Log($"[Settlement] Server submitting win/loss for {winLoseChallengeId}, winner {WinnerID}.");
        // No ModifyUserBalance() here: that call reads staticVariables.UserProfiledata, and a headless
        // build never logs anyone in, so it would throw. Each player's own client refreshes its balance.
        string winUrl = Winner_Bet_Url() + winLoseChallengeId + "/user/" + WinnerID;

        // Outside the three scoped games this is the request as it stood before any of this work:
        // no build-token refresh, no 401 retry, no response-body check, no settled latch.
        if (!IsScopedSettlementScene())
        {
            StartCoroutine(GetApiRequest(winUrl, onsucess =>
            {
                ConstantsData_M.Log($"[Settlement] Win/loss for {winLoseChallengeId} accepted by the backend.");
            }, onFailure =>
            {
                ConstantsData_M.Log($"[Settlement] Win/loss for {winLoseChallengeId} REJECTED: {onFailure}");
            }
            , withSettlementAuth: true));
            return;
        }

        // Latched BEFORE the request so a Leave arriving mid-flight cannot submit a second result,
        // and released again if the backend refuses -- otherwise a transient failure left the match
        // looking settled for good and nobody was ever paid.
        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.ServerMarkMatchSettled("win/loss submitted");
        StartCoroutine(SubmitSettlement(winUrl,
                                        $"Win/loss for {winLoseChallengeId} (winner {WinnerID})",
                                        () =>
                                        {
                                            if (NetworkGameManager.Instance != null)
                                                NetworkGameManager.Instance.ServerClearMatchSettled("win/loss was refused");
                                        }));
    }
    public void WinnerLossAIChallenge(string WinnerID)
    {
        StartCoroutine(GetApiRequest(Winner_AIBet_Url() + winLoseChallengeId + "/" + WinnerID, onsucess =>
        {
            ApiAndRoomManager._instance.ModifyUserBalance();
        }, onFailure =>
        {
            ModifyUserBalance();
        }));
    }
    public void DrawChallenge(string transactionId)
    {
        // Multiplayer settlement must only originate from the authoritative Mirror server.
        // Client result screens can still call this legacy method, but they must never reach
        // the money-moving backend endpoint themselves.
        if (!NetworkServer.active)
        {
            ConstantsData_M.Log($"[Settlement] Ignored client draw submission for {transactionId}.");
            return;
        }

        // Never trust an id supplied by presentation/client code when the server owns the match id.
        if (NetworkGameManager.Instance != null &&
            !string.IsNullOrWhiteSpace(NetworkGameManager.Instance.transactionId))
        {
            transactionId = NetworkGameManager.Instance.transactionId;
        }

        transactionId = transactionId?.Trim();
        if (string.IsNullOrEmpty(transactionId))
        {
            ConstantsData_M.Log("[Settlement] Draw was not submitted: server transaction ID is empty.");
            return;
        }

        // A server result and its local host RPC can arrive in the same frame. Submit each draw once.
        if (!submittedDrawTransactions.Add(transactionId))
        {
            ConstantsData_M.Log($"[Settlement] Duplicate draw submission ignored for {transactionId}.");
            return;
        }

        // The draw endpoint is GET and requires the deployment settlement key.
        ConstantsData_M.Log($"[Settlement] Server submitting draw for {transactionId}.");
        // On any failure the transaction is un-marked so a later authoritative attempt may retry.
        string drawTx = transactionId;

        // Same scope as the win/loss path above.
        if (!IsScopedSettlementScene())
        {
            StartCoroutine(GetApiRequest(DrawBet_Url() + drawTx, onsucess =>
            {
                ConstantsData_M.Log($"[Settlement] Draw for {drawTx} accepted by the backend.");
            }, onFailure =>
            {
                // Permit a later authoritative retry if this request did not reach the backend.
                submittedDrawTransactions.Remove(drawTx);
                ConstantsData_M.Log($"[Settlement] Draw for {drawTx} REJECTED: {onFailure}");
            }, withSettlementAuth: true));
            return;
        }

        // Same reasoning as the win/loss path above.
        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.ServerMarkMatchSettled("draw submitted");
        StartCoroutine(SubmitSettlement(DrawBet_Url() + drawTx,
                                        $"Draw for {drawTx}",
                                        () =>
                                        {
                                            submittedDrawTransactions.Remove(drawTx);
                                            if (NetworkGameManager.Instance != null)
                                                NetworkGameManager.Instance.ServerClearMatchSettled("draw was refused");
                                        }));
    }
    public void GetChallengeStatus(string transactionId, Action<string> callbackSucess = null, Action<string> Callbackfailed = null)
    {
        StartCoroutine(GetApiRequest(ChallengeStatus_Url() + transactionId, callbackSucess, Callbackfailed));
    }

    public void GetRematchApi(string transactionId, Action<string> callbackSucess = null, Action<string> Callbackfailed = null)
    {
        StartCoroutine(GetApiRequest(Rematch_Url() + transactionId, callbackSucess, Callbackfailed));
    }

    public void GetValidateApi(string transactionId, Action<string> callbackSucess = null, Action<string> Callbackfailed = null)
    {
        StartCoroutine(GetApiRequest(Validate_URL() + transactionId, callbackSucess, Callbackfailed));
    }
    public void Join_Challenge(Dictionary<string, string> betjoinRequest, Action<string> callbackSucess = null, Action<string> Callbackfailed = null)
    {
        ConstantsData_M.Log("joining bet");
        callbackSucess += SucessResult =>
        {
            JoinBetResponseModel userModel = JsonUtility.FromJson<JoinBetResponseModel>(SucessResult);
            if (userModel.status)
            {
                ModifyUserBalance();
                isCounterFlag = true;
                currentTime = int.Parse(userModel.data.bet_expires_sec);
            }
        };
        string category = GamesSetterMenu.All_games_Ref.Find(x => x.game_id == currentGameId).category;

        StartCoroutine(PostApiRequest(Join_Bet_Url(), betjoinRequest, callbackSucess, Callbackfailed));
    }
    public void GetUserInfo(string searchId, Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(GetUser_Info_Url() + searchId, OnSuccess, OnFailed));
    }
    #region PREVIOUS IMPLEMENTED
    public int IsFirstTime
    {
        get { return PlayerPrefs.GetInt("FirstTime"); }
        set { PlayerPrefs.SetInt("FirstTime", value); }
    }

    public void ForceDeviceLogout(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.logout(), OnSuccess, OnFailed));
    }

    public void RegisterPlayer(Dictionary<string, string> data, Action<string> succeess = null, Action<string> onFailed = null)
    {
        StartCoroutine(PostApiRequest(ServerConnection.createPlayerUrl(), data, succeess, onFailed));
    }
    public void VerifyOtp(int otp, string user_id, Action<string> onSuccess = null, Action<string> OnFailed = null)
    {
        string url = $"{ValidateOtpUrl()}/{user_id.ToString()}/{otp.ToString()}";
        onSuccess += Success =>
        {
            ValidateResponse resopnse = JsonUtility.FromJson<ValidateResponse>(Success);
            if (resopnse.status)
            {
                ConstantsData_M.Log("response" + resopnse.message);
            }
        };
        OnFailed += Fail =>
        {
        };
        StartCoroutine(GetApiRequest(url, onSuccess, OnFailed));
    }

    public void CreateOtp(string PHONE, Action<string> onSuccess = null, Action<string> OnFailed = null)
    {
        string url = GenereateOtpUrl() + PHONE;
        StartCoroutine(GetApiRequest(url, onSuccess, OnFailed));
    }
    public void ChallengesHistory(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(GameHistoryUrl(), OnSuccess, OnFailed));
    }
    public void FetchGameDetails(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.gameDetailUrl(), OnSuccess, OnFailed));
    }
    public void DisplayBanner(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.ClientBanner_Url(), OnSuccess, OnFailed));
    }

    public void GetChallengeInfo(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(Bet_info_Url() + winLoseChallengeId, OnSuccess, OnFailed));
    }

    public void UpdateProfile(WWWForm form, Action<string> onsuccess = null, Action<string> Failed = null)
    {
        StartCoroutine(PostApiRequestWithForm(UpdateProfile_Url() + UserProfiledata.user._id, form, onsuccess, Failed));
    }
    public void UpdatePassword(WWWForm form, Action<string> onsuccess = null, Action<string> Failed = null)
    {
        StartCoroutine(PostApiRequestWithForm(UpdatePassword_Url(), form, onsuccess, Failed));
    }
    public void UpdateFirebaseToken(WWWForm form, Action<string> onsuccess = null, Action<string> Failed = null)
    {
        StartCoroutine(PostApiRequestWithForm(Update_FirebaseToken_Url() + UserProfiledata.user._id, form, onsuccess, Failed));
    }

    public void GetShopPackages(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(Shop_Packages_Url(), OnSuccess, OnFailed));
    }

    public void BuyCoins(Dictionary<string, string> data, Action<string> onsuccess = null, Action<string> Failed = null)
    {
        StartCoroutine(PostApiRequest(purchase(), data, onsuccess, Failed));
    }

    #endregion

    public void CheckFriendStatus(string friendId, Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.IsFriend_Url() + friendId, OnSuccess, OnFailed));
    }

    public void GetChallengesRequests(string id, Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(getBetRequest() + id, OnSuccess, OnFailed));
    }

    public void GetLiveAppVersionNumber(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.VersionCheckURL() + Application.version, OnSuccess, OnFailed));
    }

    public void CreateSessionCoinsPaymentApi(string BundleId, Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.CreateSessionCoinsPayment() + $"{BundleId}", OnSuccess, OnFailed));
    }


    #region Gift
    public void PostSendGift(Dictionary<string, string> data, Action<string> succeess = null, Action<string> onFailed = null)
    {
        StartCoroutine(PostApiRequest(ServerConnection.GiftSend(), data, succeess, onFailed));
    }
    public void GetSentGiftList(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.GiftOfSender(), OnSuccess, OnFailed));
    }
    public void GetRecievedGiftList(Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.GiftOfReciever(), OnSuccess, OnFailed));
    }
    public void GetClaimGift(string giftId, Action<string> OnSuccess = null, Action<string> OnFailed = null)
    {
        StartCoroutine(GetApiRequest(ServerConnection.GiftClaim() + $"{giftId}", OnSuccess, OnFailed));
    }

    #endregion

    public void GetAnonymousBuildToken(Action<string> onSuccess = null, Action<string> onFail = null)
    {
        StartCoroutine(GetBearerApiRequest("/auth/generate/build-token", onSuccess, onFail));
    }
}

public class ValidateRematch
{
    public bool status;
    public string message;
    public string data;
}
