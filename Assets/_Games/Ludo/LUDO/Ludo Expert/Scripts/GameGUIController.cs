

using LudoGame;
using Mirror;

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Twelve;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensions;
using static GeminiUnityCommander;

public class GameGUIController : NetworkBehaviour
{
    public GameObject Waitingforopp;
    public GameObject PrizeTopBar;
    public static GameGUIController insta;
    public GameObject TIPButtonObject;
    public GameObject TIPObject;
    public GameObject firstPrizeObject;
    public GameObject SecondPrizeObject;
    public GameObject firstPrizeText;
    public GameObject secondPrizeText;
    public GameObject dropdownBtn;

    public AudioSource WinSound;
    public AudioSource myTurnSource;
    public AudioSource oppoTurnSource;
    private bool AllPlayersReady = false;
    // LUDO
    public MultiDimensionalGameObject[] PlayersPawns;
    public GameObject[] PlayersDices;
    public GameObject[] HomeLockObjects;


    [System.Serializable]
    public class MultiDimensionalGameObject
    {
        public GameObject[] objectsArray;
    }

    public GameObject ludoBoard;
    public GameObject micObject, speakerObject;
    public GameObject[] diceBackgrounds;
    public MultiDimensionalGameObject[] playersPawnsColors;
    public MultiDimensionalGameObject[] playersPawnsMultiple;
    private Color colorRed = new Color(250.0f / 255.0f, 12.0f / 255, 12.0f / 255);
    private Color colorBlue = new Color(0, 86.0f / 255, 255.0f / 255);
    private Color colorYellow = new Color(255.0f / 255.0f, 163.0f / 255, 0);
    private Color colorGreen = new Color(8.0f / 255, 174.0f / 255, 30.0f / 255);


    // END LUDO

    public GameObject GameFinishWindow;
    public GameObject ScreenShotController;
    public GameObject invitiationDialog;
    public GameObject addedFriendWindow;
    public GameObject PlayerInfoWindow;
    public GameObject ChatWindow;
    public GameObject ChatButton;
    private bool SecondPlayerOnDiagonal = true;

    private List<string> PlayersIDs;
    public GameObject[] Players;
    public GameObject[] PlayersTimers;
    public GameObject[] PlayersChatBubbles;
    public GameObject[] PlayersChatBubblesText;
    public GameObject[] PlayersChatBubblesImage;
    private GameObject[] ActivePlayers;
    public GameObject[] PlayersAvatarsButton;

    public List<Sprite> avatars = new List<Sprite>();
    public List<string> names = new List<string>();

    public List<PlayerObject> playerObjects;
    private int myIndex;
    private string myId;
    public WinPanelPotrait winPanelPotrait;
    public Sprite Ai;
    private Color[] borderColors = new Color[4] { Color.yellow, Color.green, Color.red, Color.blue };

    public static int currentPlayerIndex = -1;

    // ─── Client-authoritative reconnect snapshot ────────────────────────────────
    // Real player clients report the full board here after every settled move
    // (SaveBoardState event). The dedicated Edgegap server is NOT a player, so its own
    // PlayersPawns/playerObjects simulation is unreliable — on reconnect we relay
    // what a real client last reported instead of reading the server's own board.
    [System.NonSerialized] public string reconnectSnapshot = "";
    private Coroutine _boardReportRoutine;

    private int ActivePlayersInRoom;

    private Sprite[] emojiSprites;

    private string CurrentPlayerID;
    public string url;

    private List<PlayerObject> playersFinished = new List<PlayerObject>();


    public bool iFinished = false;
    private bool FinishWindowActive = false;

    private int firstPlacePrize;
    private int secondPlacePrize;

    private int requiredToStart = 2;
    public bool Ismultiplayer;
    public GameObject winLoseLudo;
    public GameObject winLoseLudoPanel;
    public TextMeshProUGUI countDownGameTimer;
    public GameObject LudoNetworkManagerPrefab;
    //#region Game Timer Syncing
    //[Header("UI")]
    //public TextMeshProUGUI countDownGameTimer;

    //[Header("Timer Settings")]
    //public int startTime = 600;

    //[SyncVar(hook = nameof(OnTimeChanged))]
    //private int remainingTime;

    //private Coroutine timerRoutine;

    //public override void OnStartServer()
    //{
    //    base.OnStartServer();

    //    remainingTime = startTime;
    //    timerRoutine = StartCoroutine(ServerTimer());
    //}

    //[Server]
    //IEnumerator ServerTimer()
    //{
    //    Debug.Log("Server Timer Started");

    //    while (remainingTime > 0)
    //    {
    //        yield return new WaitForSeconds(1f);

    //        if (!NetworkGameManager.Instance.IsPaused)
    //        {
    //            remainingTime--;
    //        }
    //    }

    //    Debug.Log("Timer Finished");

    //    DecideWinner();
    //}

    //void OnTimeChanged(int oldTime, int newTime)
    //{
    //    int minutes = newTime / 60;
    //    int seconds = newTime % 60;

    //    countDownGameTimer.text = $"{minutes:00}:{seconds:00}";

    //    if (newTime > 180)
    //        countDownGameTimer.color = Color.green;
    //    else if (newTime > 60)
    //        countDownGameTimer.color = Color.white;
    //    else
    //        countDownGameTimer.color = Color.red;
    //}

    //[Server]
    //void DecideWinner()
    //{
    //    int creatorFinished = LudoGame.GameManager.Instance.playerObjects[0].finishedPawns;
    //    int joinerFinished = LudoGame.GameManager.Instance.playerObjects[1].finishedPawns;

    //    string winnerID;

    //    if (creatorFinished > joinerFinished)
    //        winnerID = NetworkGameManager.Instance.creatorData.playerId;
    //    else if (joinerFinished > creatorFinished)
    //        winnerID = NetworkGameManager.Instance.joinerData.playerId;
    //    else
    //        winnerID = NetworkGameManager.Instance.creatorData.playerId;

    //    RpcTimerEnded(int.Parse(winnerID));
    //}

    //[ClientRpc]
    //void RpcTimerEnded(int winnerID)
    //{
    //    countDownGameTimer.text = "00:00";
    //    countDownGameTimer.color = Color.red;

    //    NetworkGameManager.Instance.CmdPlayerFinished(true, winnerID);
    //}
    // #endregion
    private void OnEnable()
    {
        //Wasi  PunNetwork.OnLosser += Announcelosser;
        //Wasi   PunNetwork.waitingForOpponent += WaitingForOpponentt;
        //Wasi   PunNetwork.OnWinner += AnnounceWinner;
        // MirrorNetwork.OnWinCall += AnnounceWinner;
        MirrorNetwork.OnGameStateChanged += PauseUnpauseGame;

    }
    private void OnDisable()
    {
        //Wasi   PunNetwork.OnLosser -= Announcelosser;
        //Wasi   PunNetwork.waitingForOpponent -= WaitingForOpponentt;
        //Wasi   PunNetwork.OnWinner -= AnnounceWinner;
        //  MirrorNetwork.OnWinCall -= AnnounceWinner;
        MirrorNetwork.OnGameStateChanged -= PauseUnpauseGame;

    }
    public void WaitingForOpponentt(bool state)
    {
        Waitingforopp.SetActive(state);
        //disconnectedText.text = "Waiting for oppoent";
    }
    public void PauseUnpauseGame(bool pauseState)
    {
        if (playerObjects == null || playerObjects.Count == 0 || currentPlayerIndex < 0)
        {
            Debug.LogWarning("[GameGUIController] PauseUnpauseGame called before playerObjects initialized — skipping.");
            return;
        }
        if (pauseState)
        {
            PauseTimers();
        }
        else
        {
            restartTimer();
        }
    }
    private void AnnounceWinner(string reason)
    {
        NetworkGameManager.Instance.CmdPlayerFinished(false, staticVariables.UserProfiledata.user._id);
        // MultiPlayerGameManagerTwelve.instance.CmdWinPlayer(true, staticVariables.UserProfiledata.user._id);
    }
    //public void AnnounceWinner()
    //{

    //    print(" ANNOUNCE WINNNER"); asdasd
    //    //if (winPanelPotrait != null)
    //    //{

    //    //    winPanelPotrait.onWinInit(true);
    //    //}
    //    //else
    //    //{
    //    //    Debug.LogError("WIN PANEL PORTRAIT IS null in ludo  Winnwe");
    //    //}
    //    SetFinishGame(staticVariables.UserProfiledata.user._id.ToString(), true);
    //    FinishedGame();

    //}
    //public void Announcelosser()
    //{
    //    if (!LudoGame.GameManager.Instance.isLocalMultiplayer)
    //    {
    //        print(" ANNOUNCE LOSSER");
    //        SetFinishGame(staticVariables.OpponetProfile.userId.ToString(), false);
    //        FinishedGame();
    //    }
    //    //if (winPanelPotrait != null)
    //    //{
    //    //    winPanelPotrait.onWinInit(false);
    //    //}
    //    //else
    //    //{
    //    //    Debug.LogError("WIN PANEL PORTRAIT IS null in ludo Loose");
    //    //}
    //}
    public void SpawnNetworkObject()
    {
        var network = Instantiate(LudoNetworkManagerPrefab);
        NetworkServer.Spawn(network);
    }
    public void OnPlayerLeftRoom(string otherPlayer)
    {
        print(" LUDO OVERRIDE METHOD");
        if (LudoGame.GameManager.Instance.currentPlayer.finishedPawns != 4)
        {
            print(" FINISHED PAWN 4 ON LEFT");
            //  AnnounceWinner();
        }
    }
    // Use this for initialization
    void Start()
    {
        if (NetworkServer.active)
        {
            SpawnNetworkObject();
        }

        LudoGame.GameManager.Instance.isLocalMultiplayer = !(NetworkServer.active || NetworkClient.active);
        Screen.orientation = ScreenOrientation.Portrait;

        requiredToStart = LudoGame.GameManager.Instance.requiredPlayers;
        LudoGame.GameManager.Instance.dropdownButton = dropdownBtn;

        if (LudoGame.GameManager.Instance.type == MyGameType.Private)
        {
            requiredToStart = 2;
        }
        LudoGame.GameManager.Instance.readyPlayersCount = 2;
        //if (NetworkServer.active)
        //{
        //    LudoGame.GameManager.Instance.readyPlayersCount++;

        //    // PhotonNetwork.RaiseEvent((int)EnumPhoton.ReadyToPlay, 0, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
        //}
        // LUDO
        // Rotate board and set colors

        //int rotation = UnityEngine.Random.Range(0, 4);
        // Update player data in playfab
        //Dictionary<string, string> data = new Dictionary<string, string>();
        //data.Add(MyPlayerData.CoinsKey, (LudoGame.GameManager.Instance.myPlayerData.GetCoins() - LudoGame.GameManager.Instance.payoutCoins).ToString());
        //data.Add(MyPlayerData.GamesPlayedKey, (LudoGame.GameManager.Instance.myPlayerData.GetPlayedGamesCount() + 1).ToString());

        //LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);

        ////call api to update game played
        //string url = StaticStrings.baseURL+"update-games-played.php?playfab_id="+LudoGame.GameManager.Instance.playfabManager.PlayFabId+"&avl_coins="+data[MyPlayerData.CoinsKey]+"&game_played="+data[MyPlayerData.GamesPlayedKey]+"&bet_amount="+LudoGame.GameManager.Instance.payoutCoins;
        //WWW www = new WWW(url);
        //StartCoroutine(updateUserGame(www));
        PlayersIDs = new List<string>();
        if (LudoGame.GameManager.Instance.isLocalMultiplayer)
        {
            names.Add("AI");
            PlayersIDs.Add(UnityEngine.Random.Range(1, 1000) + "_BOT");
            avatars.Add(Ai);
            micObject.SetActive(false);
            speakerObject.SetActive(false);
        }
        myId = staticVariables.UserProfiledata.user._id.ToString();

        if (!LudoGame.GameManager.Instance.isLocalMultiplayer)
        {
            if (staticVariables.opponentImage)
                avatars.Insert(0, (Sprite.Create(staticVariables.opponentImage, new Rect(0, 0, staticVariables.opponentImage.width, staticVariables.opponentImage.height), Vector2.zero)));
            else
                avatars.Insert(0, PlayFabManager.Instance.avatarSprites[UnityEngine.Random.Range(0, PlayFabManager.Instance.avatarSprites.Length - 1)]);// (Sprite.Create(staticVariables.ProfilePicture, new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height), Vector2.zero)));// ;
        }
        if (staticVariables.ProfilePicture)
            avatars.Insert(0, (Sprite.Create(staticVariables.ProfilePicture, new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height), Vector2.zero)));
        else
            avatars.Insert(0, PlayFabManager.Instance.avatarSprites[UnityEngine.Random.Range(0, PlayFabManager.Instance.avatarSprites.Length - 1)]);// (Sprite.Create(staticVariables.ProfilePicture, new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height), Vector2.zero)));// ;


        if (!LudoGame.GameManager.Instance.isLocalMultiplayer)
            names.Insert(0, staticVariables.OpponetProfile.userName);
        names.Insert(0, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);

        if (!LudoGame.GameManager.Instance.isLocalMultiplayer)
            PlayersIDs.Insert(0, staticVariables.OpponetProfile.userId);
        PlayersIDs.Insert(0, staticVariables.UserProfiledata.user._id.ToString());




        LudoGame.GameManager.Instance.avatarMy = avatars[0];// GameObject.Find("StaticGameVariablesContainer").GetComponent<StaticGameVariablesController>().avatars[int.Parse(GetAvatarIndex())];
        LudoGame.GameManager.Instance.nameMy = names[0];
        playerObjects = new List<PlayerObject>();

        for (int i = 0; i < PlayersIDs.Count; i++)
        {

            PlayerObject objnew = new PlayerObject(names[i], PlayersIDs[i], avatars[i]);

            playerObjects.Add(objnew);
        }



        // Bubble sort
        for (int i = 0; i < PlayersIDs.Count; i++)
        {
            for (int j = 0; j < PlayersIDs.Count - 1; j++)
            {
                if (string.Compare(playerObjects[j].id, playerObjects[j + 1].id) == 1)
                {
                    // swaap ids
                    PlayerObject temp = playerObjects[j + 1];
                    playerObjects[j + 1] = playerObjects[j];
                    playerObjects[j] = temp;
                }
            }
        }


        ActivePlayersInRoom = PlayersIDs.Count;

        if (PlayersIDs.Count == 2)
        {
            if (SecondPlayerOnDiagonal)
            {
                Players[1].SetActive(false);
                Players[3].SetActive(false);
                ActivePlayers = new GameObject[2];
                ActivePlayers[0] = Players[0];
                ActivePlayers[1] = Players[2];

                // LUDO
                for (int i = 0; i < PlayersPawns[1].objectsArray.Length; i++)
                {
                    PlayersPawns[1].objectsArray[i].SetActive(false);
                }


                for (int i = 0; i < PlayersPawns[3].objectsArray.Length; i++)
                {
                    PlayersPawns[3].objectsArray[i].SetActive(false);
                }
                // END LUDO
            }
            else
            {
                // LUDO
                for (int i = 0; i < PlayersPawns[2].objectsArray.Length; i++)
                {
                    PlayersPawns[2].objectsArray[i].SetActive(false);
                }

                for (int i = 0; i < PlayersPawns[3].objectsArray.Length; i++)
                {
                    PlayersPawns[3].objectsArray[i].SetActive(false);
                }

                // END LUDO
                Players[2].SetActive(false);
                Players[3].SetActive(false);
                ActivePlayers = new GameObject[2];
                ActivePlayers[0] = Players[0];
                ActivePlayers[1] = Players[1];
            }
        }
        else
        {
            ActivePlayers = Players;
        }



        int startPos = 0;
        for (int i = 0; i < playerObjects.Count; i++)
        {
            if (playerObjects[i].id == staticVariables.UserProfiledata.user._id.ToString())
            {
                startPos = i;
                break;
            }
        }
        int index = 0;
        bool addedMe = false;
        myIndex = startPos;
        LudoGame.GameManager.Instance.myPlayerIndex = myIndex;
        for (int i = startPos; ;)
        {
            if (i == startPos && addedMe) break;

            if (PlayersIDs.Count == 2 && SecondPlayerOnDiagonal)
            {
                if (addedMe)
                {
                    playerObjects[i].timer = PlayersTimers[2];
                    playerObjects[i].ChatBubble = PlayersChatBubbles[2];
                    playerObjects[i].ChatBubbleText = PlayersChatBubblesText[2];
                    playerObjects[i].ChatbubbleImage = PlayersChatBubblesImage[2];
                    string id = playerObjects[i].id;
                    PlayersAvatarsButton[2].GetComponent<Button>().onClick.RemoveAllListeners();
                    PlayersAvatarsButton[2].GetComponent<Button>().onClick.AddListener(() => ButtonClick(id));

                    // LUDO
                    playerObjects[i].dice = PlayersDices[2];
                    playerObjects[i].pawns = PlayersPawns[2].objectsArray;

                    for (int k = 0; k < playerObjects[i].pawns.Length; k++)
                    {
                        playerObjects[i].pawns[k].GetComponent<LudoPawnController>().setPlayerIndex(i);
                    }
                    playerObjects[i].homeLockObjects = HomeLockObjects[2];

                    // END LUDO
                }
                else
                {
                    LudoGame.GameManager.Instance.myPlayerIndex = i;
                    playerObjects[i].timer = PlayersTimers[index];
                    playerObjects[i].ChatBubble = PlayersChatBubbles[index];
                    playerObjects[i].ChatBubbleText = PlayersChatBubblesText[index];
                    playerObjects[i].ChatbubbleImage = PlayersChatBubblesImage[index];
                    string id = playerObjects[i].id;

                    // LUDO
                    playerObjects[i].dice = PlayersDices[index];
                    playerObjects[i].pawns = PlayersPawns[index].objectsArray;

                    for (int k = 0; k < playerObjects[i].pawns.Length; k++)
                    {
                        playerObjects[i].pawns[k].GetComponent<LudoPawnController>().setPlayerIndex(i);
                    }
                    playerObjects[i].homeLockObjects = HomeLockObjects[index];
                    // END LUDO
                }
            }
            else
            {

                playerObjects[i].timer = PlayersTimers[index];
                playerObjects[i].ChatBubble = PlayersChatBubbles[index];
                playerObjects[i].ChatBubbleText = PlayersChatBubblesText[index];
                playerObjects[i].ChatbubbleImage = PlayersChatBubblesImage[index];

                // LUDO
                playerObjects[i].dice = PlayersDices[index];
                playerObjects[i].pawns = PlayersPawns[index].objectsArray;

                for (int k = 0; k < playerObjects[i].pawns.Length; k++)
                {
                    playerObjects[i].pawns[k].GetComponent<LudoPawnController>().setPlayerIndex(i);
                }
                playerObjects[i].homeLockObjects = HomeLockObjects[index];
                // END LUDO

                string id = playerObjects[i].id;
                if (index != 0)
                {
                    PlayersAvatarsButton[index].GetComponent<Button>().onClick.RemoveAllListeners();
                    PlayersAvatarsButton[index].GetComponent<Button>().onClick.AddListener(() => ButtonClick(id));
                }
            }
            playerObjects[i].AvatarObject = ActivePlayers[index];
            ActivePlayers[index].GetComponent<PlayerAvatarController>().Name.GetComponent<Text>().text = playerObjects[i].name;
            if (playerObjects[i].avatar != null)
            {
                ActivePlayers[index].GetComponent<PlayerAvatarController>().Avatar.GetComponent<Image>().sprite = playerObjects[i].avatar;
            }

            index++;

            if (i < PlayersIDs.Count - 1)
            {
                i++;
            }
            else
            {
                i = 0;
            }
            addedMe = true;
        }
        // Capture reconnect flag NOW, before currentPlayerIndex is overwritten below.
        // CmdRiseEvent(ReconnectGame) must NOT fire here — creatorData.playerId is still
        // empty at Start() time. It is deferred into the coroutine below.
        bool isReconnect = currentPlayerIndex != -1;

        currentPlayerIndex = LudoGame.GameManager.Instance.firstPlayerInGame;
        LudoGame.GameManager.Instance.currentPlayer = playerObjects[currentPlayerIndex];

        if (NetworkServer.active || NetworkClient.active)
        {
            MirrorNetwork.OnWinCall += AnnounceVictory;
            StartCoroutine(ApplyColourWhenCreatorKnown(isReconnect));
        }
        else
        {
            ApplyBoardColours(playerObjects[currentPlayerIndex].id == myId);
        }
        if (LudoGame.GameManager.Instance.isLocalMultiplayer && LudoGame.GameManager.Instance.isPlayingWithComputer)
        {
            PrizeTopBar.SetActive(false);
        }


        LudoGame.GameManager.Instance.playerObjects = playerObjects;



        // LudoGame.GameManager.Instance.payoutCoins = AightBallPoolPlayer.pr;
        firstPlacePrize = 2 * (LudoGame.GameManager.Instance.payoutCoins);
        secondPlacePrize = 0;

        firstPrizeText.GetComponent<Text>().text = firstPlacePrize + " ";
        secondPrizeText.GetComponent<Text>().text = secondPlacePrize + " ";

        if (secondPlacePrize == 0)
        {
            SecondPrizeObject.SetActive(false);
            firstPrizeObject.GetComponent<RectTransform>().anchoredPosition = SecondPrizeObject.GetComponent<RectTransform>().anchoredPosition;
        }


        // LUDO

        // Enable home locks

        //if (LudoGame.GameManager.Instance.mode == MyGameMode.Quick || LudoGame.GameManager.Instance.mode == MyGameMode.Master)
        {
            for (int i = 0; i < LudoGame.GameManager.Instance.playerObjects.Count; i++)
            {
                LudoGame.GameManager.Instance.playerObjects[i].homeLockObjects.SetActive(true);
            }
            LudoGame.GameManager.Instance.needToKillOpponentToEnterHome = true;
        }
        //else
        //{
        //    LudoGame.GameManager.Instance.needToKillOpponentToEnterHome = false;
        //}
        //LudoGame.GameManager.Instance.needToKillOpponentToEnterHome = false;

        // END LUDO

        for (int i = 0; i < playerObjects.Count; i++)
        {
            if (playerObjects[i].id.Contains("_BOT"))
            {
                LudoGame.GameManager.Instance.readyPlayersCount++;
            }
        }

        LudoGame.GameManager.Instance.playerObjects = playerObjects;
        //if (!LudoGame.GameManager.Instance.isLocalMultiplayer)
        //{
        //    // Check if all players are still in room - if not deactivate
        //    for (int i = 0; i < playerObjects.Count; i++)
        //    {
        //        bool contains = false;
        //        Debug.Log(playerObjects[i].id + " == ");
        //        Debug.Log(PhotonNetwork.InRoom + " ==== ");

        //        if (!playerObjects[i].id.Contains("_BOT"))
        //        {
        //            for (int j = 0; j < PhotonNetwork.PlayerList.Length; j++)
        //            {
        //                if (PhotonNetwork.PlayerList[j].NickName.Equals(playerObjects[i].id))
        //                {
        //                    contains = true;
        //                    break;
        //                }
        //            }
        //            if (!contains)
        //            {
        //                LudoGame.GameManager.Instance.readyPlayersCount++;
        //                Debug.Log("Ready players: " + LudoGame.GameManager.Instance.readyPlayersCount);
        //                setPlayerDisconnected(i);
        //            }
        //        }
        //    }
        //}

        CheckPlayersIfShouldFinishGame();

        StartCoroutine(waitForPlayersToStart());
    }

    // ── Colour / board-rotation helpers ─────────────────────────────────────

    /// <param name="triggerReconnect">
    /// Pass true when Start() detected an in-progress game (currentPlayerIndex != -1).
    /// The CmdRiseEvent(ReconnectGame) is deferred here — never in Start() — so that
    /// creatorData.playerId is guaranteed non-empty before the handler runs.
    /// </param>
    private System.Collections.IEnumerator ApplyColourWhenCreatorKnown(bool triggerReconnect = false)
    {
        yield return new WaitUntil(() =>
            NetworkGameManager.Instance != null &&
            !string.IsNullOrEmpty(NetworkGameManager.Instance.creatorData.playerId));

        // Creator = Yellow (bottom, no board rotation).
        // Joiner  = Red   (board rotated 180° so joiner also sees themselves at bottom).
        ApplyBoardColours(NetworkGameManager.Instance.creatorData.playerId == myId);

        // Pawn restore: fire AFTER creatorData is ready so the ReconnectGame handler
        // can correctly tell creator from joiner when placing pawn positions.
        if (triggerReconnect)
        {
            NetworkGameManager.Instance.CmdRiseEvent((int)EnumPhoton.ReconnectGame, string.Empty);
        }
    }

    // ─── Client-authoritative reconnect snapshot helpers ────────────────────────

    /// <summary>
    /// Called after every settled move (from LudoGameController's PawnMove/PawnRemove
    /// handling). Only real player clients report — the dedicated server has no valid
    /// local board so it must never overwrite the snapshot. On a Host build the local
    /// host-client is a real player, so NetworkClient.active correctly includes it.
    /// </summary>
    public void ScheduleBoardReport()
    {
        if (!NetworkClient.active) return;               // excludes the dedicated server
        if (LudoGame.GameManager.Instance != null && LudoGame.GameManager.Instance.isLocalMultiplayer) return;
        if (_boardReportRoutine != null) StopCoroutine(_boardReportRoutine);
        _boardReportRoutine = StartCoroutine(ReportBoardAfterDelay());
    }

    private IEnumerator ReportBoardAfterDelay()
    {
        yield return new WaitForSeconds(2f);             // let move animation + turn change settle
        if (playerObjects == null || playerObjects.Count < 2) yield break;
        if (NetworkGameManager.Instance == null
            || string.IsNullOrEmpty(NetworkGameManager.Instance.creatorData.playerId)) yield break;
        // Route through the proven NetworkGameManager event bus (same channel as
        // ReconnectGame). The server stores it in OnEvent; other clients ignore it.
        NetworkGameManager.Instance.CmdRiseEvent((int)EnumPhoton.SaveBoardState, SerializeBoardCreatorFirst());
    }

    /// <summary>
    /// Serializes the full board as
    ///   "c0;c1;c2;c3;j0;j1;j2;j3;currentPlayerIndex;creatorCanEnterHome;joinerCanEnterHome;"
    /// where c* = creator's pawn positions and j* = joiner's. Ordering is keyed off the
    /// synced creatorData id (identical on every client and the server), NOT on the
    /// per-client seat layout, so the reconnect apply logic can place pawns correctly.
    /// The two canEnterHome flags carry the home-entry unlock ("Off" sign removed) so a
    /// space opened by killing an opponent survives a reconnect.
    /// On this client PlayersPawns[0] = my pawns, PlayersPawns[2] = opponent's.
    /// </summary>
    private string SerializeBoardCreatorFirst()
    {
        string creatorId = NetworkGameManager.Instance.creatorData.playerId;
        string joinerId  = NetworkGameManager.Instance.joinerData.playerId;
        bool iAmCreator = creatorId == myId;
        int creatorLocalSlot = iAmCreator ? 0 : 2;
        int joinerLocalSlot  = iAmCreator ? 2 : 0;

        var sb = new System.Text.StringBuilder();
        for (int j = 0; j < 4; j++)
            sb.Append(PlayersPawns[creatorLocalSlot].objectsArray[j].GetComponent<LudoPawnController>().CurrentPosition).Append(';');
        for (int j = 0; j < 4; j++)
            sb.Append(PlayersPawns[joinerLocalSlot].objectsArray[j].GetComponent<LudoPawnController>().CurrentPosition).Append(';');
        sb.Append(currentPlayerIndex).Append(';');
        sb.Append(CanEnterHomeById(creatorId) ? 1 : 0).Append(';');
        sb.Append(CanEnterHomeById(joinerId)  ? 1 : 0).Append(';');
        return sb.ToString();
    }

    private bool CanEnterHomeById(string id)
    {
        if (playerObjects == null) return false;
        for (int i = 0; i < playerObjects.Count; i++)
            if (playerObjects[i].id == id) return playerObjects[i].canEnterHome;
        return false;
    }

    /// <summary>
    /// After pawns are teleported via AddInstantly on reconnect, rebuild each path cell's
    /// pawn registration so that two pawns sharing a cell are re-offset (side by side)
    /// instead of landing exactly on top of each other (one hidden). Idempotent: clears
    /// every cell first, so it is safe on already-connected clients that receive the
    /// same ReconnectGame broadcast. 2-player uses seat slots 0 and 2.
    /// </summary>
    private void RebuildPawnStacksAfterRestore()
    {
        var pawns = new List<LudoPawnController>();
        foreach (int slot in new[] { 0, 2 })
        {
            if (slot >= PlayersPawns.Length) continue;
            var arr = PlayersPawns[slot].objectsArray;
            for (int j = 0; j < arr.Length; j++)
            {
                var pc = arr[j].GetComponent<LudoPawnController>();
                if (pc != null) pawns.Add(pc);
            }
        }

        foreach (var pc in pawns) pc.ClearAllPathCells();
        foreach (var pc in pawns) pc.RegisterOnPathCellAfterRestore();
        foreach (var pc in pawns) pc.RepositionAfterRestore();
    }

    /// <summary>
    /// Restores a player's home-entry unlock on reconnect: keeps canEnterHome and toggles
    /// the home-lock ("Off") object so a killed-open space is not re-locked by Start().
    /// </summary>
    private void ApplyCanEnterHome(string id, bool canEnter)
    {
        if (playerObjects == null) return;
        for (int i = 0; i < playerObjects.Count; i++)
        {
            if (playerObjects[i].id != id) continue;
            playerObjects[i].canEnterHome = canEnter;
            if (playerObjects[i].homeLockObjects != null)
                playerObjects[i].homeLockObjects.SetActive(!canEnter);
            break;
        }
    }

    private void ApplyBoardColours(bool rotation)
    {
        Color[] colors = rotation
            ? new Color[] { colorYellow, colorGreen, colorRed,    colorBlue }
            : new Color[] { colorRed,    colorGreen, colorYellow, colorBlue };

        if (!rotation)
            ludoBoard.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, -180.0f);

        for (int i = 0; i < diceBackgrounds.Length; i++)
            diceBackgrounds[i].GetComponent<Image>().color = colors[i];

        for (int i = 0; i < playersPawnsColors.Length; i++)
            for (int j = 0; j < playersPawnsColors[i].objectsArray.Length; j++)
            {
                playersPawnsColors[i].objectsArray[j].GetComponent<Image>().color   = colors[i];
                playersPawnsMultiple[i].objectsArray[j].GetComponent<Image>().color = colors[i];
            }
    }

    public void AnnounceVictory(string reason)
    {
        NetworkGameManager.Instance.CmdPlayerFinished(true, staticVariables.UserProfiledata.user._id);
        //SetFinishGame(staticVariables.UserProfiledata.user._id.ToString(), true);
        //FinishedGame();

    }
    IEnumerator updateUserGame(WWW www)
    {
        yield return www;

        // check for errors
        if (www.error == null)
        {
            Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
        }
        else
        {
            Debug.Log("WWW Failed!: " + www.text);// contains all the data sent from the server
        }
    }

    private IEnumerator waitForPlayersToStart()
    {
        Debug.Log("Waiting for players " + LudoGame.GameManager.Instance.readyPlayersCount + " - " + requiredToStart);

        yield return new WaitForSeconds(0.1f);


        if (LudoGame.GameManager.Instance.readyPlayersCount < requiredToStart)
        {
            StartCoroutine(waitForPlayersToStart());
        }
        else
        {
            AllPlayersReady = true;
            SetTurn();
            restartTimer();  // start the turn countdown — SetTurn() only shows the timer UI

            // if (myIndex == 0)
            // {
            //     SetMyTurn();
            //     playerObjects[0].dice.GetComponent<GameDiceController>().DisableDiceShadow();
            // }
            // else
            // {
            //     SetOpponentTurn();
            //     playerObjects[currentPlayerIndex].dice.GetComponent<GameDiceController>().DisableDiceShadow();
            // }

        }


    }

    public int GetCurrentPlayerIndex()
    {
        return currentPlayerIndex;
    }

    public void TIPButton()
    {
        if (TIPObject.activeSelf)
        {
            TIPObject.SetActive(false);
        }
        else
        {
            TIPObject.SetActive(true);
        }
    }

    public void FacebookShare()
    {
        if (PlayerPrefs.GetString("LoggedType").Equals("Facebook"))
        {

            //    Uri myUri = new Uri("https://ludoone.com/ludo.apk");
            //#if UNITY_IPHONE
            //          myUri = new Uri("https://itunes.apple.com/us/app/apple-store/id" + StaticStrings.ITunesAppID);
            //#endif

            //FB.ShareLink(
            //    myUri,
            //    StaticStrings.facebookShareLinkTitle,
            //    callback: ShareCallback
            //);
        }
    }

    //private void ShareCallback(IShareResult result)
    //{
    //    if (result.Cancelled || !String.IsNullOrEmpty(result.Error))
    //    {
    //        Debug.Log("ShareLink Error: " + result.Error);
    //    }
    //    else if (!String.IsNullOrEmpty(result.PostId))
    //    {
    //        // Print post identifier of the shared content
    //        Debug.Log(result.PostId);
    //    }
    //    else
    //    {
    //        // Share succeeded without postID
    //        LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(StaticStrings.rewardCoinsForShareViaFacebook);
    //        Debug.Log("ShareLink success!");
    //    }
    //}

    public void StopAndFinishGame()
    {
        StopTimers();
        SetFinishGame(staticVariables.UserProfiledata.user._id.ToString(), true);
        winPanelPotrait.onWinInit(true);
        ShowGameFinishWindow();
    }
    //public void StopAndFinishGame( bool Isplayerwin)
    //{
    //    StopTimers();
    //    SetFinishGame(staticVariables.UserProfiledata.user._id.ToString(), Isplayerwin);
    //    SetFinishGame(staticVariables.OpponetProfile.id.ToString(), !Isplayerwin);
    //    winPanelPotrait.onWinInit(true);
    //    ShowGameFinishWindow();
    //}

    public void ShareScreenShot()
    {

        //#if UNITY_ANDROID
        //        string text = StaticStrings.ShareScreenShotText;
        //        text = text + " " + "https://mydreamludo.com/" + StaticStrings.AndroidPackageName;
        //        //ScreenShotController.GetComponent<NativeShare>().ShareScreenshotWithText(text);
        //#elif UNITY_IOS
        //        string text = StaticStrings.ShareScreenShotText;
        //        text = text + " " + "https://itunes.apple.com/us/app/apple-store/id" + StaticStrings.ITunesAppID;
        //      //Wasi  ScreenShotController.GetComponent<NativeShare>().ShareScreenshotWithText(text);
        //#endif


    }

    public void ShowGameFinishWindow()
    {
        if (!FinishWindowActive)
        {

            //AdsManager.Instance.adsScript.ShowAd(AdLocation.GameFinishWindow);
            FinishWindowActive = true;

            List<PlayerObject> otherPlayers = new List<PlayerObject>();

            for (int i = 0; i < playerObjects.Count; i++)
            {
                PlayerAvatarController controller = playerObjects[i].AvatarObject.GetComponent<PlayerAvatarController>();
                if (controller.Active && !controller.finished)
                {
                    otherPlayers.Add(playerObjects[i]);
                }
            }
            // LudoGame.GameManager.Instance.initMenuScript.PlayBackgroundMusic();

            GameFinishWindow.GetComponent<GameFinishWindowController>().showWindow(playersFinished, otherPlayers, firstPlacePrize, secondPlacePrize);
        }
    }

    private void ButtonClick(string id)
    {

        //int index = 0;

        //for (int i = 0; i < playerObjects.Count; i++)
        //{
        //    if (playerObjects[i].id == id)
        //    {
        //        index = i;
        //        break;
        //    }
        //}

        //CurrentPlayerID = id;

        //if (playerObjects[index].AvatarObject.GetComponent<PlayerAvatarController>().Active)
        //{
        //    PlayerInfoWindow.GetComponent<PlayerInfoController>().ShowPlayerInfo(playerObjects[index].avatar, playerObjects[index].name, playerObjects[index].data);
        //}

    }

    //public void AddFriendButtonClick()
    //{
    //    if (!CurrentPlayerID.Contains("_BOT"))
    //    {
    //        AddFriendRequest request = new AddFriendRequest()
    //        {
    //            FriendPlayFabId = CurrentPlayerID,
    //        };

    //        PlayFabClientAPI.AddFriend(request, (result) =>
    //        {
    //            PhotonNetwork.RaiseEvent((int)EnumPhoton.AddFriend, PhotonNetwork.NickName + ";" + LudoGame.GameManager.Instance.nameMy + ";" + CurrentPlayerID, true, null);
    //            addedFriendWindow.SetActive(true);
    //            Debug.Log("Added friend successfully");
    //        }, (error) =>
    //        {
    //            addedFriendWindow.SetActive(true);
    //            Debug.Log("Error adding friend: " + error.Error);
    //        }, null);
    //    }
    //    else
    //    {
    //        Debug.Log("Add Friend - It's bot!");
    //        addedFriendWindow.SetActive(true);
    //    }
    //}

    // (A leftover debug Update() sent CmdPlayerFinished(true, me) on the Space key — an instant win for anyone with a
    // keyboard. Removed 8 Oct 2026.)

    public void FinishedGame()
    {
        //if (LudoGame.GameManager.Instance.isLocalMultiplayer)
        //{
        //    ludoController.gUIController.FinishedGame();

        //}
        Debug.Log("FinishedGame1");
        if (!LudoGame.GameManager.Instance.isLocalMultiplayer)
        {
            if (LudoGame.GameManager.Instance.currentPlayer.id == staticVariables.UserProfiledata.user._id.ToString())
            {
                Debug.Log("FinishedGame3");
                NetworkGameManager.Instance.CmdPlayerFinished(true, staticVariables.UserProfiledata.user._id);
                // SetFinishGame(LudoGame.GameManager.Instance.currentPlayer.id, true);
                Debug.Log("PLAYER WIN  " + winPanelPotrait.isPlayerWin);
            }
            else
            {
                Debug.Log("FinishedGame4");
                NetworkGameManager.Instance.CmdPlayerFinished(true, staticVariables.UserProfiledata.user._id);
                // SetFinishGame(LudoGame.GameManager.Instance.currentPlayer.id, false);
                Debug.Log("PLAYER WIN  " + winPanelPotrait.isPlayerWin);
            }
        }
        else
        {
            Debug.Log("FinishedGame5");
            if (LudoGame.GameManager.Instance.currentPlayer.id == staticVariables.UserProfiledata.user._id.ToString())
            {
                Debug.Log("FinishedGame6");
                Debug.Log("PLAYER WIN --- " + winPanelPotrait.isPlayerWin);
                if (NetworkServer.active || NetworkClient.active)
                {
                    Debug.Log("MultiPlayerWin");
                    NetworkGameManager.Instance.CmdPlayerFinished(true, staticVariables.UserProfiledata.user._id);
                }
                else
                {
                    Debug.Log("FinishedGame7");
                    Debug.Log("OfflineWin");
                    winLoseLudoPanel.SetActive(true);
                    winLoseLudo.GetComponent<ResultManagerLudo>().WinPlayer(true, staticVariables.UserProfiledata.user._id.ToString());

                }
                Debug.Log("FinishedGame8");
                //  SetFinishGame(LudoGame.GameManager.Instance.currentPlayer.id, true);
                //   GuestGrandScripts.instance.ChangeGuestCoins(staticVariables.currentPrize*2);


            }
            else
            {
                Debug.Log("FinishedGame9");
                Debug.Log("AIWin");
                winLoseLudoPanel.SetActive(true);
                winLoseLudo.GetComponent<ResultManagerLudo>().WinPlayer(false, "ai");
                //   SetFinishGame(LudoGame.GameManager.Instance.currentPlayer.id, false);
                // Constants_M.Log("AI WIN  " + winPanelPotrait.isPlayerWin);
                //GuestGrandScripts.instance.ChangeGuestCoins(-staticVariables.currentPrize);

            }
            Debug.Log("FinishedGame10");
        }

        // SetFinishGame(PhotonNetwork.player.NickName, true);
    }

    public void SetFinishGame(string id, bool me)
    {
        if (!me || !iFinished)
        {
            // Decrease the active players count safely
            ActivePlayersInRoom = Mathf.Max(0, ActivePlayersInRoom - 1);

            int index = GetPlayerPosition(id);

            // Check if index is within the valid range
            if (index < 0 || index >= playerObjects.Count)
            {
                Debug.LogError("Invalid player index: " + index);
                return;
            }

            foreach (PlayerObject po in playerObjects)
            {
                print("PLAYER obj-----" + po.name + " " + po.id + "\n");
            }
            Debug.Log("PLAYER obj COUNT = > " + playerObjects.Count);
            if (index < 3 && playersFinished.Count < 3)
            {
                if (playerObjects[0].id != playerObjects[1].id)
                {
                    print("playerObjects[index].id != playerObjects[index +1 ].id) " + "\n" + playerObjects[0].id + " & " + playerObjects[1].id);
                    playersFinished.Add(playerObjects[index]);

                }
            }

            PlayerAvatarController controller = playerObjects[index].AvatarObject.GetComponent<PlayerAvatarController>();

            if (controller == null)
            {
                Debug.LogError("PlayerAvatarController not found for player index: " + index);
                return;
            }

            controller.Name.GetComponent<Text>().text = "";
            controller.Active = false;
            controller.finished = true;
            playerObjects[index].dice.SetActive(false);
            int position = playersFinished.Count;
            if (position == 1)
            {
                ApiAndRoomManager._instance.WinnerLossChallenge(id);

                controller.Crown.SetActive(true);
            }
            else
            {
                winPanelPotrait.onWinInit(false);
            }
            if (me)
            {
                //PhotonNetwork.KeepAliveInBackground = StaticStrings.photonDisconnectTimeoutLong;
                iFinished = true;

                if (ActivePlayersInRoom >= 0)
                {
                    //PhotonNetwork.RaiseEvent((int)EnumPhoton.FinishedGame, PhotonNetwork.LocalPlayer.NickName, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);

                    Debug.Log("Set finish call finish turn");
                    SendFinishTurn();
                }
                print("position" + position);
                if (position == 1)
                {
                    winPanelPotrait.onWinInit(true);

                    WinSound.Play();
                }
                else if (position == 2)
                {

                    // Handle second place logic
                }
            }
            else if (LudoGame.GameManager.Instance.currentPlayer.isBot)
            {
                SendFinishTurn();
            }

            CheckPlayersIfShouldFinishGame();

            controller.setPositionSprite(position);
        }
    }

    public int GetPlayerPosition(string id)
    {
        for (int i = 0; i < playerObjects.Count; i++)
        {
            if (playerObjects[i].id.Equals(id))
            {
                return i;
            }
        }
        return 1;
    }

    public void SendFinishTurn()
    {
        if (!FinishWindowActive && ActivePlayersInRoom > 1)
        {
            bool pawnInFinalArea = true;
            for (global::System.Int32 i = 0; i < LudoGame.GameManager.Instance.currentPlayer.pawns.Length; i++)
            {
                if (LudoGame.GameManager.Instance.currentPlayer.pawns[i].GetComponent<LudoPawnController>().currentPosition <= 50)
                {
                    pawnInFinalArea = false;
                }

            }
            //Constants_M.Log($"{LudoGame.GameManager.Instance.playerObjects[currentPlayerIndex==0?1:0].name}  {LudoGame.GameManager.Instance.currentPlayer.name}");
            if (pawnInFinalArea && LudoGame.GameManager.Instance.playerObjects[currentPlayerIndex == 0 ? 1 : 0].canEnterHome == false)
            {
                LudoGame.GameManager.Instance.currentPlayer.finishedPawns = 4;
                if (LudoGame.GameManager.Instance.isLocalMultiplayer)
                {
                    print("LOCAL MULTIPLAYER");

                    FinishedGame();

                }
                else
                {
                    print("NOT LOCAL MULTIPLAYER");
                    //Wasi     PunNetwork.instance.LudoWinCall();
                }
                return;
            }
            if (LudoGame.GameManager.Instance.currentPlayer.isBot)
            {
                BotDelay();
            }
            else
            {
                // PhotonNetwork.RaiseEvent((int)EnumPhoton.NextPlayerTurn, myIndex, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
                Debug.Log("Turn Change Cmd");
                if (Ismultiplayer)
                {
                    Debug.Log("Multiplayer");
                    // setCurrentPlayerIndex(myIndex);
                    // SetTurn();
                    // With the server running the board it passes the turn itself (RpcServerTurn).
                    if (!LudoNetworkManager.ServerAuthority)
                        NetworkGameManager.Instance.CmdSetNextTurn(myIndex.ToString());
                }
                else
                {
                    Debug.Log("Offline");
                    setCurrentPlayerIndex(myIndex);
                    SetTurn();
                }
                //currentPlayerIndex = (myIndex + 1) % playerObjects.Count;

                //  Debug.Log("PLAYER BEFORE: " + currentPlayerIndex);

                //  setCurrentPlayerIndex(myIndex);
                //   print("setCurrentPlayerIndex(myIndex) " + currentPlayerIndex);
                // Debug.Log("PLAYER AFTER: " + currentPlayerIndex + " isbot: " + LudoGame.GameManager.Instance.currentPlayer.isBot);

                //  SetTurn();
                //SetOpponentTurn();

                LudoGame.GameManager.Instance.miniGame.setOpponentTurn();
            }
        }
    }

    public void MyWin()
    {
        winPanelPotrait.onWinInit(true);
    }
    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        insta = this;
        NetworkGameManager.OnEventReceived += OnEvent;
        NetworkGameManager.OnTurnBegin += NextPlayerTurn;
    }


    void OnDestroy()
    {
        NetworkGameManager.OnTurnBegin -= NextPlayerTurn;

        NetworkGameManager.OnEventReceived -= OnEvent;
    }
    public void NextPlayerTurn(string index)
    {
        // Server-authoritative: only the match server passes the turn (ApplyServerTurn); a relayed turn-end is ignored.
        if (LudoNetworkManager.ServerAuthority) return;
        FlowTurnEnded(index);
        if (playerObjects[(int.Parse(index))].AvatarObject.GetComponent<PlayerAvatarController>().Active && currentPlayerIndex == int.Parse(index))
        {
            if (!FinishWindowActive)
            {
                setCurrentPlayerIndex(int.Parse(index));
                SetTurn();

            }
        }
    }
    private void OnEvent(int eventcode, string CustomData, NetworkConnectionToClient sender = null)
    {
        Debug.Log("received event: " + eventcode);
        switch (eventcode)
        {
            //case (int)EnumPhoton.NextPlayerTurn:
            //    if (playerObjects[(int)CustomData].AvatarObject.GetComponent<PlayerAvatarController>().Active &&
            //        currentPlayerIndex == (int)CustomData)
            //    {
            //        if (!FinishWindowActive)
            //        {
            //            setCurrentPlayerIndex((int)CustomData);
            //            SetTurn();
            //        }
            //    }
            //    break;



            case (int)EnumPhoton.FinishedGame:
                {
                    string message = (string)CustomData;
                    SetFinishGame(message, false);
                }
                break;
            case (int)EnumPhoton.SaveBoardState:
                {
                    // Server-only cache of the client-authoritative board snapshot.
                    // Other clients receive this broadcast but ignore it.
                    FlowBoardReported((string)CustomData);
                    // A server that runs its own board keeps that as the snapshot instead.
                    if (NetworkServer.active && !LudoNetworkManager.ServerAuthority)
                        reconnectSnapshot = (string)CustomData;
                }
                break;
            case (int)EnumPhoton.ReconnectGame:
                {
                    string message = (string)CustomData;
                    Debug.Log(message);
                    if (NetworkServer.active)
                    {
                        // ── Server: relay the last board reported by a real player client ──
                        // The dedicated Edgegap server is NOT a player: its own PlayersPawns /
                        // playerObjects are built from empty client-only staticVariables, so
                        // reading positions here would send garbage (works on Host only because
                        // there the server IS the creator player). Instead we echo the
                        // client-authoritative snapshot cached via the SaveBoardState event.
                        // Format: "c0;c1;c2;c3;j0;j1;j2;j3;currentPlayerIndex;"
                        MatchFlow.Log("Ludo", $"a player rejoined (connection {(sender != null ? sender.connectionId.ToString() : "?")}) — " + (string.IsNullOrEmpty(reconnectSnapshot) ? "no saved board to send back" : "last saved board sent to both players"));
                        if (!string.IsNullOrEmpty(reconnectSnapshot))
                            NetworkGameManager.Instance.RiseEventRpc((int)EnumPhoton.ReconnectGame, reconnectSnapshot);
                    }
                    else if (!string.IsNullOrEmpty(message))
                    {
                        // ── Client: data[0-3] = creator's positions, data[4-7] = joiner's, data[8] = currentPlayerIndex ──
                        // On this client: PlayersPawns[0] = MY pawns, PlayersPawns[2] = opponent's.
                        string[] data = ((string)CustomData).Split(';');
                        if (NetworkGameManager.Instance.creatorData.playerId == myId)
                        {
                            // I am creator — my positions are in data[0-3]
                            for (int j = 0; j < 4; j++)
                                PlayersPawns[0].objectsArray[j].GetComponent<LudoPawnController>().AddInstantly(int.Parse(data[j]));
                            for (int j = 0; j < 4; j++)
                                PlayersPawns[2].objectsArray[j].GetComponent<LudoPawnController>().AddInstantly(int.Parse(data[j + 4]));
                        }
                        else
                        {
                            // I am joiner — my positions are in data[4-7]
                            for (int j = 0; j < 4; j++)
                                PlayersPawns[2].objectsArray[j].GetComponent<LudoPawnController>().AddInstantly(int.Parse(data[j]));
                            for (int j = 0; j < 4; j++)
                                PlayersPawns[0].objectsArray[j].GetComponent<LudoPawnController>().AddInstantly(int.Parse(data[j + 4]));
                        }

                        // Re-offset any two pawns that share a cell so one is not hidden
                        // exactly under the other after the teleport-restore above.
                        RebuildPawnStacksAfterRestore();

                        // Restore the authoritative current player from the server.
                        // This corrects any drift that happened while disconnect detection
                        // was in flight (up to ~5 sec) and the turn timer expired locally.
                        if (data.Length > 8 && int.TryParse(data[8], out int syncedIndex)
                            && syncedIndex >= 0 && syncedIndex < playerObjects.Count)
                        {
                            currentPlayerIndex = syncedIndex;
                            LudoGame.GameManager.Instance.currentPlayer = playerObjects[currentPlayerIndex];
                            SetTurn();
                            restartTimer();
                        }

                        // Restore the home-entry unlock ("Off" sign) so a space opened by
                        // killing an opponent is not re-locked by Start() on reconnect.
                        // data[9] = creator flag, data[10] = joiner flag (1 = unlocked).
                        if (data.Length > 10)
                        {
                            ApplyCanEnterHome(NetworkGameManager.Instance.creatorData.playerId, data[9] == "1");
                            ApplyCanEnterHome(NetworkGameManager.Instance.joinerData.playerId,  data[10] == "1");
                        }
                    }
                }
                break;
            case (int)EnumPhoton.ExitGame:
                {
                    string message = (string)CustomData;
                    if (message == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        // PhotonNetwork.LeaveRoom();
                        SceneManager.LoadScene("Home");
                    }
                    else
                    {
                        StopAndFinishGame();
                    }
                }
                break;

            default:
                Debug.LogWarning("Unhandled event code: " + eventcode);
                break;
        }
    }




    /// <summary>The match server says it is now <paramref name="pl"/>'s turn (server-authoritative Ludo).</summary>
    public void ApplyServerTurn(int pl)
    {
        if (FinishWindowActive || playerObjects == null || pl < 0 || pl >= playerObjects.Count) return;
        currentPlayerIndex = pl;
        LudoGame.GameManager.Instance.currentPlayer = playerObjects[pl];
        SetTurn();
        restartTimer();
    }

    private void SetMyTurn()
    {
        LudoGame.GameManager.Instance.isMyTurn = true;

        if (LudoGame.GameManager.Instance.miniGame != null)
            LudoGame.GameManager.Instance.miniGame.setMyTurn();


        StartTimer();
    }

    private void BotTurn()
    {
        oppoTurnSource.Play();
        //LudoGame.GameManager.Instance.currentPlayer = playerObjects[currentPlayerIndex];
        LudoGame.GameManager.Instance.isMyTurn = false;
        Debug.Log("Bot Turn");
        StartTimer();

        LudoGame.GameManager.Instance.miniGame.BotTurn(true);

        //Invoke("BotDelay", 2.0f);

    }

    private void SetTurn()
    {
        Debug.Log("SET TURN CALLED");
        for (int i = 0; i < playerObjects.Count; i++)
        {
            playerObjects[i].dice.GetComponent<GameDiceController>().EnableDiceShadow();
        }

        playerObjects[currentPlayerIndex].dice.GetComponent<GameDiceController>().DisableDiceShadow();

        LudoGame.GameManager.Instance.currentPlayer = playerObjects[currentPlayerIndex];

        Debug.Log("Setting Turn : " + playerObjects[currentPlayerIndex].id + "--" + myId);
        if (playerObjects[currentPlayerIndex].id == myId)
        {
            SetMyTurn();
        }
        else if (playerObjects[currentPlayerIndex].isBot)
        {
            BotTurn();
        }
        else
        {
            SetOpponentTurn();
        }
    }

    private void BotDelay()
    {
        if (!FinishWindowActive)
        {
            setCurrentPlayerIndex(currentPlayerIndex);
            SetTurn();
        }

    }


    private void setCurrentPlayerIndex(int current)
    {

        while (true)
        {
            current = current + 1;
            currentPlayerIndex = (current) % playerObjects.Count;
            LudoGame.GameManager.Instance.currentPlayer = playerObjects[currentPlayerIndex];
            if (playerObjects[currentPlayerIndex].AvatarObject.GetComponent<PlayerAvatarController>().Active) break;
        }

    }

    private void SetOpponentTurn()
    {
        Debug.Log("Opponent turn");
        oppoTurnSource.Play();
        LudoGame.GameManager.Instance.isMyTurn = false;
        /*if (playerObjects[currentPlayerIndex].id.Contains("_BOT"))
        {
            BotTurn();
        }*/

        StartTimer();
    }

    private void StartTimer()
    {
        for (int i = 0; i < playerObjects.Count; i++)
        {
            if (i == currentPlayerIndex)
            {
                playerObjects[currentPlayerIndex].timer.SetActive(true);
            }
            else
            {
                playerObjects[i].timer.SetActive(false);
            }
        }
    }

    public void StopTimers()
    {
        for (int i = 0; i < playerObjects.Count; i++)
        {
            playerObjects[i].timer.SetActive(false);
        }
    }

    public void PauseTimers()
    {
        if (playerObjects == null || playerObjects.Count == 0 || currentPlayerIndex < 0) return;
        playerObjects[currentPlayerIndex].timer.GetComponent<UpdatePlayerTimer>().Pause();
    }

    public void restartTimer()
    {
        if (playerObjects == null || playerObjects.Count == 0 || currentPlayerIndex < 0) return;
        playerObjects[currentPlayerIndex].timer.GetComponent<UpdatePlayerTimer>().restartTimer();
    }
    public void OnPlayerEnteredRoom(string newPlayer)
    {


        //for (int i = 0; i < playerObjects.Count; i++)
        //{
        //    if (playerObjects[i].id.Equals(newPlayer.NickName))
        //    {
        //        setPlayerDisconnected(i);
        //        break;
        //    }
        //}

        CheckPlayersIfShouldFinishGame();
    }

    // public void CheckPlayersIfShouldFinishGame()
    // {
    //     if (!FinishWindowActive)
    //     {
    //         if ((ActivePlayersInRoom == 1 && !iFinished) || ActivePlayersInRoom == 1)
    //         {

    //             StopAndFinishGame();
    //         }

    //         if (iFinished && ActivePlayersInRoom == 1 && CheckIfOtherPlayerIsBot())
    //         {
    //             AddBotToListOfWinners();
    //             StopAndFinishGame();
    //         }
    //     }
    // }


    public void CheckPlayersIfShouldFinishGame()
    {
        if (!FinishWindowActive)
        {
            if ((ActivePlayersInRoom == 1 && !iFinished))
            {
                StopAndFinishGame();
                return;
            }

            if (ActivePlayersInRoom == 0)
            {
                StopAndFinishGame();
                return;
            }

            if (iFinished && ActivePlayersInRoom == 1 && CheckIfOtherPlayerIsBot())
            {
                AddBotToListOfWinners();
                StopAndFinishGame();
                return;
            }

            if (ActivePlayersInRoom > 1 && iFinished)
            {
                TIPButtonObject.SetActive(true);
            }



        }
    }
    public void AddBotToListOfWinners()
    {
        for (int i = 0; i < playerObjects.Count; i++)
        {
            if (playerObjects[i].id.Contains("_BOT") && playerObjects[i].AvatarObject.GetComponent<PlayerAvatarController>().Active)
            {
                playersFinished.Add(playerObjects[i]);
            }
        }
    }

    public bool CheckIfOtherPlayerIsBot()
    {
        for (int i = 0; i < playerObjects.Count; i++)
        {
            if (playerObjects[i].id.Contains("_BOT") && playerObjects[i].AvatarObject.GetComponent<PlayerAvatarController>().Active)
            {
                playerObjects[i].AvatarObject.GetComponent<PlayerAvatarController>().finished = true;
                return true;
            }
        }
        return false;
    }

    public void setPlayerDisconnected(int i)
    {
        requiredToStart--;
        if (!FinishWindowActive)
        {
            if (!playerObjects[i].AvatarObject.GetComponent<PlayerAvatarController>().finished)
                ActivePlayersInRoom--;

            Debug.Log("Active players: " + ActivePlayersInRoom);
            if (currentPlayerIndex == i && ActivePlayersInRoom > 1)
            {

                setCurrentPlayerIndex(currentPlayerIndex);
                if (AllPlayersReady)
                    SetTurn();
            }

            Debug.Log("za petla");
            playerObjects[i].AvatarObject.GetComponent<PlayerAvatarController>().PlayerLeftRoom();

            // LUDO
            playerObjects[i].dice.SetActive(false);
            if (!playerObjects[i].AvatarObject.GetComponent<PlayerAvatarController>().finished)
            {
                for (int j = 0; j < playerObjects[i].pawns.Length; j++)
                {
                    // playerObjects[i].pawns[j].SetActive(false);
                    playerObjects[i].pawns[j].GetComponent<LudoPawnController>().GoToInitPosition(false);
                }
            }
            // END LUDO
        }
    }
    public void MultiplayerLeaveGame()
    {
        MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
        if (NetworkGameManager.Instance)
            NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
        //  NetworkGameManager.Instance.CmdReloadServer();//
        this.Delay(1, () => NetworkManager.singleton.StopClient());
        SceneManager.LoadScene("Home");
    }
    public void LeaveGame(bool finishWindow)
    {
        if (LudoGame.GameManager.Instance.isLocalMultiplayer && !LudoGame.GameManager.Instance.isPlayingWithComputer)
        {
            Debug.Log("LeaveGame_1");
            LudoGame.GameManager.Instance.playfabManager.roomOwner = false;
            LudoGame.GameManager.Instance.roomOwner = false;
            // LudoGame.GameManager.Instance.type = MyGameType.TwoPlayer;
            LudoGame.GameManager.Instance.resetAllData();
            SceneManager.LoadScene("Home");
            // SavingWindow.SetActive(true);
        }
        else
        {
            Debug.Log("LeaveGame_2");
            if (!iFinished || finishWindow)
            {
                Debug.Log("LeaveGame_3");
                PlayerPrefs.SetInt("GamesPlayed", PlayerPrefs.GetInt("GamesPlayed", 1) + 1);

                //PhotonNetwork.KeepAliveInBackground = StaticStrings.photonDisconnectTimeoutLong;

                //LudoGame.GameManager.Instance.cueController.removeOnEventCall();
                //if (PhotonNetwork.InRoom)
                //{
                //    if (PhotonNetwork.CurrentRoom.PlayerCount == 1) SceneManager.LoadScene("Home");
                //    PhotonNetwork.RaiseEvent((byte)EnumPhoton.ExitGame, staticVariables.UserProfiledata.user._id.ToString(), new RaiseEventOptions { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });
                //}
                //else
                //{
                //    SceneManager.LoadScene("Home");
                //}
                if (!NetworkServer.active || !NetworkClient.active)
                {
                    Debug.Log("Not connected to network, loading home scene.");
                    SceneManager.LoadScene("Home");
                }
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                if (NetworkGameManager.Instance)
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                NetworkGameManager.Instance.CmdReloadServer();
                if (NetworkServer.active || NetworkClient.active)
                {
                    // SceneManager.LoadScene("Home");
                }
                //this.Delay(1, () => NetworkManager.singleton.StopClient());
                // SceneManager.LoadScene("Home");
                LudoGame.GameManager.Instance.playfabManager.roomOwner = false;
                LudoGame.GameManager.Instance.roomOwner = false;
                LudoGame.GameManager.Instance.resetAllData();



            }
            else
            {
                Debug.Log("LeaveGame_4");
                ShowGameFinishWindow();
            }
            // SceneManager.LoadScene("Home");
        }
    }

    public void ShowHideChatWindow()
    {
        if (!ChatWindow.activeSelf)
        {
            ChatWindow.SetActive(true);
            ChatButton.GetComponent<Text>().text = "X";
        }
        else
        {
            ChatWindow.SetActive(false);
            ChatButton.GetComponent<Text>().text = "CHAT";
        }
    }

    public void ShowHideDropdown()
    {
        if (!dropdownBtn.activeSelf)
        {
            dropdownBtn.SetActive(true);
            // ChatButton.GetComponent<Text>().text = "X";
        }
        else
        {
            dropdownBtn.SetActive(false);
            // ChatButton.GetComponent<Text>().text = "CHAT";
        }
    }

    // ─── Server match-flow logs ("[Ludo Flow]", see MatchFlow) — logging only, nothing here changes the game ───
    // Written only on the dedicated server. The server's own board is not trusted (see ReconnectGame above), so
    // the lines are built from what the clients send: dice / move events, turn-ends and the SaveBoardState board.
    // Player index = index into playerObjects, which every client sorts by id (ascending).

    static int _flowTurn;                 // player index whose turn it is (by these logs)
    static int _flowRollsThisTurn;
    static int _flowLastRoll;
    static int _flowSixes;                // 6s in a row this turn
    static bool _flowRollPending;         // rolled, no move yet
    static int[] _flowPos;                // creator tokens 0-3, joiner tokens 4-7 (-1 = base)
    static bool _flowCreatorUnlocked, _flowJoinerUnlocked;

    static bool FlowIsCreator(int pl)
    {
        var ngm = NetworkGameManager.Instance;
        if (ngm == null || ngm.creatorData == null || ngm.joinerData == null) return pl == 0;
        bool creatorFirst = string.Compare(ngm.creatorData.playerId, ngm.joinerData.playerId) != 1;
        return (pl == 0) == creatorFirst;
    }

    static string FlowSide(bool creator)
    {
        var ngm = NetworkGameManager.Instance;
        if (ngm == null || ngm.creatorData == null || ngm.joinerData == null) return creator ? "creator" : "joiner";
        return MatchFlow.Who(creator ? ngm.creatorData.playerId : ngm.joinerData.playerId);
    }

    /// <summary>Name for a Ludo player index (0/1).</summary>
    public static string FlowWho(int pl) => (pl < 0 || pl > 1) ? "player " + (pl + 1) : FlowSide(FlowIsCreator(pl));

    static int FlowHomeIndex()
    {
        var g = insta;
        if (g == null || g.PlayersPawns == null || g.PlayersPawns.Length == 0 || g.PlayersPawns[0] == null) return -1;
        var arr = g.PlayersPawns[0].objectsArray;
        if (arr == null || arr.Length == 0 || arr[0] == null) return -1;
        var pc = arr[0].GetComponent<LudoPawnController>();
        return pc != null && pc.path != null && pc.path.Length > 0 ? pc.path.Length - 1 : -1;
    }

    static string FlowSquare(int p, int home) => p < 0 ? "base" : (p == home ? "home" : "square " + p);

    static int FlowHomeCount(bool creator)
    {
        int home = FlowHomeIndex(), n = 0;
        if (_flowPos == null || home < 0) return 0;
        for (int t = 0; t < 4; t++) if (_flowPos[(creator ? 0 : 4) + t] == home) n++;
        return n;
    }

    /// <summary>
    /// Server: tokens home and total progress (sum of track positions, base = 0) of the creator and the joiner, from
    /// the last board a player reported (<see cref="reconnectSnapshot"/>, "c0..c3;j0..j3;…"). False when there is no
    /// report yet or the home square is unknown — the caller then keeps its old count.
    /// </summary>
    public static bool TryReportedBoardScore(out int creatorHome, out int joinerHome, out int creatorProgress, out int joinerProgress)
    {
        creatorHome = joinerHome = creatorProgress = joinerProgress = 0;
        string snap = insta != null ? insta.reconnectSnapshot : null;
        int home = FlowHomeIndex();
        if (string.IsNullOrEmpty(snap) || home < 0) return false;
        string[] d = snap.Split(';');
        if (d.Length < 8) return false;
        for (int s = 0; s < 8; s++)
        {
            int p;
            if (!int.TryParse(d[s], out p)) return false;
            bool creator = s < 4;
            if (p == home) { if (creator) creatorHome++; else joinerHome++; }
            if (p > 0) { if (creator) creatorProgress += p; else joinerProgress += p; }
        }
        return true;
    }

    /// <summary>"Ali 2 – 1 Sara tokens home" from the client-reported board.</summary>
    public static string FlowHomeCounts() =>
        $"tokens home (client board): {FlowSide(true)} {FlowHomeCount(true)} – {FlowHomeCount(false)} {FlowSide(false)}";

    /// <summary>Server-side match start (both players connected).</summary>
    public static void FlowMatchStarted()
    {
        if (!MatchFlow.Enabled) return;
        _flowTurn = LudoGame.GameManager.Instance != null ? LudoGame.GameManager.Instance.firstPlayerInGame : 0;
        _flowRollsThisTurn = 0; _flowLastRoll = 0; _flowSixes = 0; _flowRollPending = false;
        _flowPos = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
        _flowCreatorUnlocked = false; _flowJoinerUnlocked = false;
        MatchFlow.Begin("Ludo", $"game started — {FlowWho(0)} vs {FlowWho(1)}, first turn → {FlowWho(_flowTurn)}");
    }

    public static void FlowDiceRolled(int pl, int value)
    {
        if (!MatchFlow.Enabled) return;
        _flowRollsThisTurn++;
        _flowSixes = value == 6 ? _flowSixes + 1 : 0;
        _flowLastRoll = value;
        _flowRollPending = true;
        string note = _flowRollsThisTurn > 1 ? " (bonus roll)" : "";
        if (value == 6 && _flowSixes >= 3) note += " — third 6 in a row, turn is lost";
        MatchFlow.Log("Ludo", $"{FlowWho(pl)} rolled a {value}{note}");
    }

    public static void FlowPawnMoved(int pl, int token, int steps)
    {
        if (!MatchFlow.Enabled) return;
        _flowRollPending = false;
        try
        {
            bool creator = FlowIsCreator(pl);
            int home = FlowHomeIndex();
            string line = $"{FlowWho(pl)} moved token {token + 1}";
            int slot = (creator ? 0 : 4) + token;
            if (_flowPos == null || token < 0 || token > 3)
            {
                MatchFlow.Log("Ludo", line + $" by {steps}");
                return;
            }
            int from = _flowPos[slot];
            int to = from < 0 ? 0 : from + steps;
            _flowPos[slot] = to;
            line += from < 0 ? $" out of base onto the start square (rolled {steps})" : $" by {steps}: {FlowSquare(from, home)} → {FlowSquare(to, home)}";
            MatchFlow.Log("Ludo", line);
            if (home >= 0 && to == home)
            {
                int n = FlowHomeCount(creator);
                MatchFlow.Log("Ludo", $"{FlowWho(pl)}'s token {token + 1} reached home ({n}/4 home) — bonus roll");
                if (n == 4)
                {
                    var ngm = NetworkGameManager.Instance;
                    string winnerId = ngm == null || ngm.creatorData == null || ngm.joinerData == null ? null
                        : (creator ? ngm.creatorData.playerId : ngm.joinerData.playerId);
                    if (!LudoNetworkManager.ServerAuthority)   // the server board decides it then
                        MatchFlow.SendResult(winnerId, "all 4 tokens home", FlowHomeCounts());
                }
            }
        }
        catch (System.Exception e) { MatchFlow.Log("Ludo", "move log failed: " + e.Message); }
    }

    static void FlowTurnEnded(string index)
    {
        if (!MatchFlow.Enabled) return;
        if (!int.TryParse(index, out int pl)) return;
        string who = FlowWho(pl);
        if (pl != _flowTurn) MatchFlow.Log("Ludo", $"turn-end sent by {who} while it was {FlowWho(_flowTurn)}'s turn");
        if (_flowRollsThisTurn == 0) MatchFlow.Log("Ludo", $"turn timer ran out for {who} (did not roll)");
        else if (_flowRollPending && _flowSixes < 3) MatchFlow.Log("Ludo", $"{who} rolled a {_flowLastRoll} but made no move (no legal move or turn timer ran out)");
        _flowTurn = 1 - pl;
        _flowRollsThisTurn = 0; _flowSixes = 0; _flowRollPending = false;
        MatchFlow.Log("Ludo", $"turn → {FlowWho(_flowTurn)}");
    }

    /// <summary>A client reported the settled board ("c0..c3;j0..j3;turn;creatorCanEnterHome;joinerCanEnterHome;").</summary>
    static void FlowBoardReported(string snap)
    {
        if (!MatchFlow.Enabled || string.IsNullOrEmpty(snap)) return;
        try
        {
            string[] d = snap.Split(';');
            if (d.Length < 8) return;
            int[] now = new int[8];
            for (int s = 0; s < 8; s++) if (!int.TryParse(d[s], out now[s])) return;
            int home = FlowHomeIndex();
            if (_flowPos != null)
            {
                for (int s = 0; s < 8; s++)
                {
                    if (_flowPos[s] >= 0 && now[s] < 0)
                        MatchFlow.Log("Ludo", $"{FlowSide(s < 4)}'s token {s % 4 + 1} was captured by {FlowSide(s >= 4)} — back to base from {FlowSquare(_flowPos[s], home)}, bonus roll");
                    else if (_flowPos[s] != now[s] && now[s] == home && home >= 0)
                        MatchFlow.Log("Ludo", $"{FlowSide(s < 4)}'s token {s % 4 + 1} is home (client board)");
                }
            }
            _flowPos = now;
            if (d.Length > 10)
            {
                bool c = d[9] == "1", j = d[10] == "1";
                if (c && !_flowCreatorUnlocked) MatchFlow.Log("Ludo", $"{FlowSide(true)} has captured — home entry unlocked");
                if (j && !_flowJoinerUnlocked) MatchFlow.Log("Ludo", $"{FlowSide(false)} has captured — home entry unlocked");
                _flowCreatorUnlocked = c; _flowJoinerUnlocked = j;
            }
        }
        catch (System.Exception e) { MatchFlow.Log("Ludo", "board log failed: " + e.Message); }
    }

}
