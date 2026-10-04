//using ExitGames.Client.Photon;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
public class MultiLobbyPanel //Photon Removal: MonoBehaviourPunCallbacks
{
    public static MultiLobbyPanel Instance;
    [Header("Login Panel")]
    public GameObject LoginPanel;
    public Text waitingText;


    [Header("Inside Room Panel")]
    public GameObject InsideRoomPanel;
    public GameObject Insideinner;

    public Button StartGameButton;
    public GameObject LeaveButton;

    public GameObject PlayerListEntryPrefab;

    private Dictionary<string, GameObject> roomListEntries;
    private Dictionary<int, GameObject> playerListEntries;
    //Photon Removal private Dictionary<string, RoomInfo> cachedRoomList;
    //Hashtable for room props
    //private Hashtable expectedCustomRoomProperties;
    public MultiInfoPanel multiInfoPanel;
    bool isConnecting;

    public float waitToStartGame = 15;
    public float StartTime = 0;
    public GameState currentGameState;
    public List<string> mEnvList;
    public DumyLobbyPlayerList dumyLobbyPlayerList;
    public GameObject setNamePanel;
    bool isOffLine = false;
    static int lastLevelLoadId = 3;
    public enum MultiplayerType
    {
        playRandom,
        playWithFriend
    }

    [Space]
    [Header("CHANGES")]

    [SerializeField] GameObject selectionPanal;
    public MultiplayerType multiplayerType;
    public string RoomCode;
    [SerializeField] GameObject roomCodeObject;
    [SerializeField] Text roomCodeText;

    #region UNITY
    public string levelsToLoad;
    public void Awake()
    {
        //Photon Removal  cachedRoomList = new Dictionary<string, RoomInfo>();

        roomListEntries = new Dictionary<string, GameObject>();
        currentGameState = GameState.WAITING;
        Instance = this;

    }

    private void Start()
    {

        //Photon Removal  PhotonNetwork.LocalPlayer.NickName = MultiPlayerGame.getPlayerName();  // here we just saved the name as "Player" + Random number
    }

    #endregion

    #region PUN CALLBACKS

    //public override void OnConnectedToMaster()
    //{
    //    if (isConnecting)
    //    {
    //        isConnecting = false;
    //        if (MConstants.multiPlayerType == MConstants.MULTIPLAYER_TYPE.playWithFriend)
    //        {
    //            selectionPanal.SetActive(true);
    //            hideInfoPopup();
    //        }
    //        else                                                                                                                               //Photon Removal
    //        {
    //            MultiPlayerGame.setAIData();
    //            OnJoinRandomRoomButtonClicked();
    //        }
    //    }

    //}


    //public override void OnDisconnected(DisconnectCause cause)
    //{
    //Photon Removal
    //    if (isOffLine)
    //    {
    //        PhotonNetwork.OfflineMode = true;
    //        OnLoginButtonClicked();
    //    }


    //}
    //public override void OnRoomListUpdate(List<RoomInfo> roomList)
    //{
    //    ClearRoomListView();

    //    UpdateCachedRoomList(roomList);                                                                                                                     //Photon Removal
    //    UpdateRoomListView();
    //}

    //public override void OnLeftLobby()
    //{
    //    cachedRoomList.Clear();                                                                                                                                   //Photon Removal

    //    ClearRoomListView();
    //}



    //private void Update()
    //{
    //    if (MConstants.multiPlayerType == MConstants.MULTIPLAYER_TYPE.playRandom)
    //    {
    //        if (PhotonNetwork.IsMasterClient && Time.time >= StartTime + waitToStartGame && currentGameState == GameState.LOBBY && PhotonNetwork.PlayerList.Length <= 1)                                                              //Photon Removal
    //        {

    //            currentGameState = GameState.INGAME;
    //            waitingText.text = "Connecting....";

    //            dumyLobbyPlayerList.DrawDummyPlayerList();
    //            LeaveButton.SetActive(false);
    //            isOffLine = true;

    //            PhotonNetwork.LeaveRoom();
    //            PhotonNetwork.Disconnect();
    //        }
    //        else if (PhotonNetwork.IsMasterClient && Time.time >= StartTime + 5 && currentGameState == GameState.READY_TO_GO && PhotonNetwork.PlayerList.Length > 1)
    //        {
    //            currentGameState = GameState.INGAME;
    //            OnStartGameButtonClicked();
    //        }
    //    }
    //    else
    //    {
    //        if (PhotonNetwork.IsMasterClient && Time.time >= StartTime + waitToStartGame && currentGameState == GameState.READY_TO_GO && PhotonNetwork.PlayerList.Length > 1)
    //        {
    //            currentGameState = GameState.INGAME;
    //            OnStartGameButtonClicked();
    //        }
    //    }

    //}
    //public override void OnJoinedRoom()
    //{
    //    ////Debug.Log("OnJoinedRoom========= ");
    //    RoomCode = PhotonNetwork.CurrentRoom.Name;
    //    roomCodeObject.SetActive(false);

    //    if (playerListEntries == null)
    //    {
    //        playerListEntries = new Dictionary<int, GameObject>();
    //    }

    //    foreach (Player p in PhotonNetwork.PlayerList)
    //    {
    //        GameObject entry = Instantiate(PlayerListEntryPrefab);
    //        entry.transform.SetParent(Insideinner.transform);
    //        entry.transform.localScale = Vector3.one;                                                                                                                                                                       //Photon Removal
    //        entry.GetComponent<PlayerListEntry>().Initialize(p.ActorNumber, p.NickName);

    //        object isPlayerReady;
    //        if (p.CustomProperties.TryGetValue(AsteroidsGame.PLAYER_READY, out isPlayerReady))
    //        {
    //            entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool)isPlayerReady);
    //        }
    //        if (p.CustomProperties.TryGetValue(MultiPlayerGame.PLAYER_COUNTRY, out isPlayerReady))
    //        {
    //            entry.GetComponent<PlayerListEntry>().SetCountry((string)isPlayerReady);
    //        }
    //        if (p.CustomProperties.TryGetValue(MultiPlayerGame.PLAYER_AWATAR, out isPlayerReady))
    //        {
    //            entry.GetComponent<PlayerListEntry>().SetPlayerAwatar((int)isPlayerReady);
    //        }
    //        playerListEntries.Add(p.ActorNumber, entry);
    //    }


    //    Hashtable props = new Hashtable
    //        {
    //            {AsteroidsGame.PLAYER_LOADED_LEVEL, false},
    //        {"StartTime", (int)PhotonNetwork.ServerTimestamp}
    //        };
    //    PhotonNetwork.LocalPlayer.SetCustomProperties(props);


    //    hideInfoPopup();
    //    if (isOffLine)
    //    {
    //        Invoke("showLoadingMenu", 3f);

    //        Invoke("loadSinglePlayerLevel", 4);

    //    }

    //}

    //public override void OnLeftRoom()
    //{

    //    foreach (GameObject entry in playerListEntries.Values)
    //    {                                                                                                                                                               //Photon Removal
    //        if (entry != null)
    //        {
    //            Destroy(entry.gameObject);
    //        }
    //    }

    //    playerListEntries.Clear();
    //    playerListEntries = null;
    //    currentGameState = GameState.WAITING;
    //    if (!isOffLine)
    //    {
    //        HandleBackButton();
    //    }
    //}

    //public override void OnPlayerEnteredRoom(Player newPlayer)
    //{

    //    GameObject entry = Instantiate(PlayerListEntryPrefab);
    //    entry.transform.SetParent(Insideinner.transform);
    //    entry.transform.localScale = Vector3.one;                                                                                                                                                         //Photon Removal
    //    entry.GetComponent<PlayerListEntry>().Initialize(newPlayer.ActorNumber, newPlayer.NickName);

    //    playerListEntries.Add(newPlayer.ActorNumber, entry);

    //}

    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{
    //    Destroy(playerListEntries[otherPlayer.ActorNumber].gameObject);                                                                                                                  //Photon Removal
    //    playerListEntries.Remove(otherPlayer.ActorNumber);

    //}

    //public override void OnMasterClientSwitched(Player newMasterClient)
    //{                                                                                                                                                                                               //Photon Removal
    //    if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
    //    {
    //        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    //    }
    //}

    //public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    //{                                                                                                                                                                                                                         //Photon Removal

    //    if (playerListEntries == null)
    //    {
    //        playerListEntries = new Dictionary<int, GameObject>();
    //    }

    //    GameObject entry;
    //    if (playerListEntries.TryGetValue(targetPlayer.ActorNumber, out entry))
    //    {
    //        object isPlayerReady;
    //        if (changedProps.TryGetValue(AsteroidsGame.PLAYER_READY, out isPlayerReady))
    //        {
    //            entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool)isPlayerReady);
    //        }
    //        if (changedProps.TryGetValue(MultiPlayerGame.PLAYER_COUNTRY, out isPlayerReady))
    //        {
    //            entry.GetComponent<PlayerListEntry>().SetCountry((string)isPlayerReady);
    //        }
    //        if (changedProps.TryGetValue(MultiPlayerGame.PLAYER_AWATAR, out isPlayerReady))
    //        {
    //            entry.GetComponent<PlayerListEntry>().SetPlayerAwatar((int)isPlayerReady);
    //        }
    //    }

    //    if (PhotonNetwork.PlayerList.Length > 1 && LeaveButton != null)
    //    {
    //        LeaveButton.SetActive(false);
    //        waitingText.text = "Connecting....";
    //    }
    //    else
    //    {
    //        waitingText.text = "Waiting for Players";

    //    }

    //    if (PhotonNetwork.PlayerList.Length > 1 && CheckPlayersReady())
    //    {
    //        StartTime = Time.time;
    //        waitingText.text = "Ready....";

    //        currentGameState = GameState.READY_TO_GO;
    //    }
    //}
    #endregion

    #region UI CALLBACKS
    public void OnCreateRoomButtonClicked()
    {
        
    }
    public void OnSelectedMultiplayerGame()
    {
        //Debug.Log("Login Panal opened");
        selectionPanal.SetActive(true);
    }
    //void displayRoomCode()
    //{
    //    roomCodeObject.SetActive(true);
    //    if (PhotonNetwork.InRoom)
    //    {
    //        roomCodeText.text = PhotonNetwork.CurrentRoom.Name;                                                        //Photon Removal
    //    }

    //}
    [SerializeField] GameObject joinRoomFailedText;
    [SerializeField] GameObject JoinPanel;
    //public void JoinRoom(InputField input)
    //{
    //    //Debug.Log("Joining");
    //    joinRoomFailedText.SetActive(false);
    //    if (!input.text.IsNullOrEmpty())
    //    {                                                                                                                                  //Photon Removal
    //        string roomName = input.text;
    //        PhotonNetwork.JoinRoom(roomName);
    //        RoomCode = roomName;
    //        JoinPanel.SetActive(false);
    //        input.text = "";
    //    }
    //    else
    //    {
    //        //Debug.Log("in else of join room");
    //        joinRoomFailedText.SetActive(true);
    //        Invoke("disappearToast", 1f);
    //    }
    //}
    void disappearToast()
    {
        joinRoomFailedText.SetActive(false);
    }
    public void JoinPanelOff(InputField input)
    {
        JoinPanel.SetActive(false);
        input.text = "";
    }
    //public void CreateRoom()   
    //{
    //    string roomName = Random.Range(1000, 10000).ToString();
    //    RoomOptions options = new RoomOptions { MaxPlayers = 4 };
    //    options.IsOpen = true;
    //    options.IsVisible = false;                                                                                                                                                              //Photon Removal
    //    options.CustomRoomPropertiesForLobby = new string[1] { "gameType" };
    //    options.CustomRoomProperties = new Hashtable(1) { { "gameType", MultiPlayerGame.isLastManStandingMode ? 2 : 1 } }; // add this line
    //    PhotonNetwork.CreateRoom(roomName, options, null);
    //}
    private string GenerateRoomCode()
    {
        return Random.Range(1000, 9999).ToString();
    }
    //public void OnJoinRandomRoomButtonClicked()
    //{
    //    showInfoPopup("joining  room...");
    //    expectedCustomRoomProperties = new Hashtable { { "gameType", MultiPlayerGame.isLastManStandingMode ? 2 : 1 } };
    //    if (PhotonNetwork.IsConnectedAndReady)                                                                                                                                                                                      //Photon Removal
    //    {
    //        bool isSucces = PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 4);
    //        if (!isSucces)
    //        {
    //            hideInfoPopup();
    //        }
    //    }
    //    else
    //    {
    //        PhotonNetwork.ConnectUsingSettings();
    //    }
    //}

    public void OnLeaveGameButtonClicked()
    {
        //Photon Removal  PhotonNetwork.LeaveRoom();
    }

    public void OnOffLineButtonClicked()
    {
        MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;
        string playerName = MultiPlayerGame.getPlayerName();
        isOffLine = true;
        MultiPlayerGame.isSinglePlayer = true;
        MultiPlayerGame.setAIData();

        //if (PhotonNetwork.IsConnected)
        //{

        //    PhotonNetwork.Disconnect();

        //}
        //else                                                                                                                                                                          //Photon Removal
        //{
        //    PhotonNetwork.OfflineMode = true;
        //    OnJoinRandomRoomButtonClicked();
        //              isConnecting = true; ;
        //}

        StartTime = Time.time;
        currentGameState = GameState.LOBBY;

    }


    private int lastLevelIndex = -1;
    int LevelIndex()
    {
        lastLevelIndex = PlayerPrefs.GetInt("lastPlayerLevelIndex", 0);
        if (lastLevelIndex >= mEnvList.Count)
        {
            lastLevelIndex = 0;
        }
        PlayerPrefs.SetInt("lastPlayerLevelIndex", lastLevelIndex + 1);
        //Debug.Log("Random Number: " + lastLevelIndex);
        return lastLevelIndex;
    }
    void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public void OnLoginButtonClicked()
    {
        levelsToLoad = mEnvList[LevelIndex()];
        Shuffle(mEnvList);
        // levelsToLoad = mEnvList[lastLevelLoadId];

        MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;
        MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playRandom;
        string playerName = MultiPlayerGame.getPlayerName();
        if (!isOffLine)
        {
            MultiPlayerGame.isSinglePlayer = false;
        }

        //Photon Removal if (PhotonNetwork.IsConnected)
        {

            //Photon Removal    OnJoinRandomRoomButtonClicked();

        }
        //Photon Removal  else
        {
            if (!playerName.Equals(""))
            {
                //Photon Removal    PhotonNetwork.LocalPlayer.NickName = playerName;
                //Photon Removal   bool isSucces = PhotonNetwork.ConnectUsingSettings();
                //Photon Removal  if (isSucces)
                {
                    showInfoPopup("Connecting....");

                }
            }
            else
            {
                ConstantsData_M.Log("Player Name is invalid.");
            }

            isConnecting = true; ;
        }

        StartTime = Time.time;
        currentGameState = GameState.LOBBY;

    }

    public void OnLoginButtonClickedPlayWithFriend()
    {
     
        levelsToLoad = mEnvList[LevelIndex()];
        MConstants.CurrentGameMode = MConstants.GAME_MODES.MULTI_PLAYER;

        MConstants.multiPlayerType = MConstants.MULTIPLAYER_TYPE.playWithFriend;
        string playerName = MultiPlayerGame.getPlayerName();
        if (!isOffLine)
        {
            MultiPlayerGame.isSinglePlayer = false;
        }

        //Photon Removal   if (PhotonNetwork.IsConnected)
        {

            OnSelectedMultiplayerGame();

        }
        //Photon Removal else
        {
            if (!playerName.Equals(""))
            {
                //Photon Removal    PhotonNetwork.LocalPlayer.NickName = playerName;
                //Photon Removal   bool isSucces = PhotonNetwork.ConnectUsingSettings();
                //Photon Removal  if (isSucces)
                {
                    showInfoPopup("Connecting....");
                }
            }
            else
            {
                ConstantsData_M.Log("Player Name is invalid.");
            }

            isConnecting = true; ;
        }

        StartTime = Time.time;
        currentGameState = GameState.LOBBY;

    }

    public void OnRoomListButtonClicked()
    {
        //if (!PhotonNetwork.InLobby)
        //{
        //    PhotonNetwork.JoinLobby();
        //}
        //SetActivePanel(RoomListPanel.name);

    }

    //public void OnStartGameButtonClicked()
    //{
    //    lastLevelLoadId++;
    //    lastLevelLoadId %= mEnvList.Count;
    //    PhotonNetwork.CurrentRoom.IsOpen = false;
    //    PhotonNetwork.CurrentRoom.IsVisible = false;
    //    showLoadingMenu();                                                                                                                               //Photon Removal

    //    Hashtable props = new Hashtable
    //                {
    //                    {MultiPlayerGame.PLAYER_LOADING, true}
    //                };
    //    PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    //    PhotonNetwork.LoadLevel(levelsToLoad);
    //    MConstants.isRaceOver = false;

    //}

    public void loadSinglePlayerLevel()
    {
        lastLevelLoadId++;
        lastLevelLoadId %= mEnvList.Count;
        levelsToLoad = mEnvList[LevelIndex()];
        //  PhotonNetwork.LoadLevel(levelsToLoad);                                                             //Photon Removal
        MConstants.isRaceOver = false;
        waitingText.text = "Ready....";

    }

    #endregion



    private bool CheckPlayersReady()
    {
        //if (!PhotonNetwork.IsMasterClient)
        //{
        //    return false;
        //}

        //foreach (Player p in PhotonNetwork.PlayerList)
        //{                                                                                                                                                                                               //Photon Removal
        //    object isPlayerReady;
        //    if (p.CustomProperties.TryGetValue(MultiPlayerGame.PLAYER_READY, out isPlayerReady))
        //    {
        //        if (!(bool)isPlayerReady)
        //        {
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        return true;
    }

    private void ClearRoomListView()
    {
        foreach (GameObject entry in roomListEntries.Values)
        {
            //Photon Removal  Destroy(entry.gameObject);
        }

        roomListEntries.Clear();
    }

    public void LocalPlayerPropertiesUpdated()
    {
    }

    private void SetActivePanel(string activePanel)
    {
        LoginPanel.SetActive(activePanel.Equals(LoginPanel.name));
       
        InsideRoomPanel.SetActive(activePanel.Equals(InsideRoomPanel.name));
    }

    //private void UpdateCachedRoomList(List<RoomInfo> roomList)
    //{
    //    foreach (RoomInfo info in roomList)
    //    {
    //        // Remove room from cached room list if it got closed, became invisible or was marked as removed
    //        if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)                                                                                                                      //Photon Removal
    //        {
    //            if (cachedRoomList.ContainsKey(info.Name))
    //            {
    //                cachedRoomList.Remove(info.Name);
    //            }

    //            continue;
    //        }

    //        // Update cached room info
    //        if (cachedRoomList.ContainsKey(info.Name))
    //        {
    //            cachedRoomList[info.Name] = info;
    //        }
    //        // Add new room info to cache
    //        else
    //        {
    //            cachedRoomList.Add(info.Name, info);
    //        }
    //    }
    //}

    private void UpdateRoomListView()
    {
     
    }

    void showLoadingMenu()
    {
        if (MainMenuManager.Instance && MainMenuManager.Instance.currentSubMenu != SubMenuNames.LOADING)
        {
            MainMenuManager.Instance.showSubMenu(SubMenuNames.LOADING);
        }
    }

    void HandleBackButton()
    {
        MainMenuManager.Instance.isLeaveEnable = true;

        MainMenuManager.Instance.handleBackMenu();
    }

    void showLobbyMenu()
    {
        MainMenuManager.Instance.showMenu(MenuNames.MULTI_PLAYER);
        MainMenuManager.Instance.isLeaveEnable = false;
    }
    public void showInfoPopup(string message)
    {
        multiInfoPanel.DisplayMessage(message);

    }
    public void hideInfoPopup()
    {
        multiInfoPanel.gameObject.SetActive(false);

    }

    public void DisconnectGame()
    {
        //Photon Removal   PhotonNetwork.Disconnect();

    }

    public void showNamePanel(bool isHide)
    {
        setNamePanel.SetActive(isHide);
    }


    //public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    //{
    //    object startTimeFromProps;
                                                                                                                                                                         //Photon Removal
    //    if (propertiesThatChanged.TryGetValue(MultiPlayerGame.PLAYER_LOADING, out startTimeFromProps))
    //    {
    //        if ((bool)startTimeFromProps)
    //        {
    //            showLoadingMenu();
    //        }
    //    }
    //}


    public void showNameMenu()
    {

        showNamePanel(true);

    }
}
public enum GameState
{

    WAITING,
    LOBBY,
    READY_TO_GO,
    INGAME
}
