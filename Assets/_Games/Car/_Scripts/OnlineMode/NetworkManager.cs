namespace CarRace
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    using TMPro;
    using UnityEngine.UI;
    using UnityEngine.SceneManagement;
    using UnityEngine.Networking;
    using System;
    /// <summary>
    /// This Class Controls Photon MatchMaking System
    /// </summary>
    public class NetworkManager //Photon Removal : MonoBehaviourPunCallbacks
    {
        [Header("Script References")]
        [SerializeField] AudioManager audioManager;

        #region UI References
        [Header("Main Menu")]
        [SerializeField] Button MultiplayerModeButton;
        [SerializeField] GameObject mainMenuUI;
        [SerializeField] GameObject errorPopUp;
        [SerializeField] TextMeshProUGUI errorInfoText;
        [SerializeField] GameObject quitPopUp;
        [SerializeField] Button quitButton;

        [Header("Game Settings Screen"), Space(3)]
        [SerializeField] GameObject GameSettingsMenuUI;

        [Header("Map Selection Screen"), Space(3)]
        [SerializeField] GameObject LevelSelectionUI;

        [Header("Mode Selection Menu"), Space(3)]
        [SerializeField] GameObject ModeSelectionUI;


        [Space(3), Header("Game Options")]
        [SerializeField] GameObject gameOptionsUI;
        [SerializeField] TMP_InputField privateRoomInput;

        [Space(3), Header("Lobby")]
        [SerializeField] GameObject lobbyUI;
        [SerializeField] GameObject playerListContent;
        [SerializeField] GameObject playerListPrefab;
        [SerializeField] TextMeshProUGUI roomPropsText;
        [SerializeField] TextMeshProUGUI playerPropsText;
        [SerializeField] GameObject startGameButton;

        [Space(3), Header("Loading Screen")]
        [SerializeField] GameObject loadingScreen;

        [Space(3), Header("Shop Screen")]
        [SerializeField] GameObject ShopUI;

        [Space(3), Header("Garage Screen")]
        [SerializeField] GameObject GarageUI;

        [Space(3), Header("Ping")]
        [SerializeField] GameObject pingObject;
        [SerializeField] TextMeshProUGUI pingText;
        [SerializeField] Image pingIcon;
        [SerializeField] TextMeshProUGUI regionText;
        bool changingRegion;
        #endregion

        private Dictionary<int, GameObject> playerListGameobjects;

        #region Unity Methods
        private void Start()
        {
            ActivatePanel(mainMenuUI.name);
            //Photon Removal    PhotonNetwork.AutomaticallySyncScene = true;
            pingObject.SetActive(false);
            MultiplayerModeButton.onClick.AddListener(ConnectToPhoton);
            quitButton.onClick.AddListener(() =>
            {
                Application.Quit();
            });
            bool? ready = EventManager.OnCheckAdLoaded_Banner?.Invoke();
            if ((bool)ready)
            {
                EventManager.OnShowAd_Banner?.Invoke();
            }
        }

        #endregion


        #region Photon Callbacks

        //public override void OnDisconnected(DisconnectCause cause)                                                                         //Photon Removal
        //{

        //    StopCoroutine(UpdatePing());
        //    pingObject.SetActive(false);
        //    if (changingRegion)
        //        return;
        //    ActivatePanel(mainMenuUI.name);

        //}
        //    public override void OnConnectedToMaster()                                                                                                    //Photon Removal
        //{
        //    regionText.text = "Current Region : " + PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion;
        //    pingObject.SetActive(true);
        //    StartCoroutine(UpdatePing());
        //    ActivatePanel(gameOptionsUI.name);
        //    PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString(PPConst.UserName);
        //    PlayerSelections.GameMode = 1;
        //}

        //    public override void OnJoinedRoom()                                                                                                                            //Photon Removal
        //{
        //    ActivatePanel(lobbyUI.name);
        //    roomPropsText.text = PhotonNetwork.CurrentRoom.Name;
        //    playerPropsText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;


        //    if (playerListGameobjects == null)
        //    {

        //        playerListGameobjects = new Dictionary<int, GameObject>();
        //    }

        //    foreach (Player player in PhotonNetwork.PlayerList)
        //    {
        //        GameObject playerListGameObject = Instantiate(playerListPrefab);
        //        playerListGameObject.transform.SetParent(playerListContent.transform);
        //        playerListGameObject.transform.localScale = Vector3.one;
        //        playerListGameObject.GetComponent<PlayerListEntryInitializer>().Initialize(player.ActorNumber, player.NickName);


        //        object isPlayerReady;
        //        if (player.CustomProperties.TryGetValue("PR", out isPlayerReady))
        //        {
        //            playerListGameObject.GetComponent<PlayerListEntryInitializer>().SetPlayerReady((bool)isPlayerReady);
        //        }


        //        playerListGameobjects.Add(player.ActorNumber, playerListGameObject);
        //    }

        //    startGameButton.SetActive(false);
        //}

        //    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)                                                                                //Photon Removal
        //{
        //    GameObject playerListGameObject;
        //    if (playerListGameobjects.TryGetValue(targetPlayer.ActorNumber, out playerListGameObject))
        //    {
        //        object isPlayerReady;
        //        if (changedProps.TryGetValue("PR", out isPlayerReady))
        //        {
        //            playerListGameObject.GetComponent<PlayerListEntryInitializer>().SetPlayerReady((bool)isPlayerReady);
        //        }
        //    }

        //    startGameButton.SetActive(CheckPlyersReady());
        //}
        //public override void OnPlayerEnteredRoom(Player newPlayer)                                                                                                                                //Photon Removal
        //{
        //    roomPropsText.text = PhotonNetwork.CurrentRoom.Name;
        //    playerPropsText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
        //    GameObject playerListGameObject = Instantiate(playerListPrefab);
        //    playerListGameObject.transform.SetParent(playerListContent.transform);
        //    playerListGameObject.transform.localScale = Vector3.one;
        //    playerListGameObject.GetComponent<PlayerListEntryInitializer>().Initialize(newPlayer.ActorNumber, newPlayer.NickName);

        //    playerListGameobjects.Add(newPlayer.ActorNumber, playerListGameObject);

        //    startGameButton.SetActive(CheckPlyersReady());
        //}
        //public override void OnPlayerLeftRoom(Player otherPlayer)                                                                                                                                                      //Photon Removal
        //{
        //    roomPropsText.text = PhotonNetwork.CurrentRoom.Name;
        //    playerPropsText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
        //    Destroy(playerListGameobjects[otherPlayer.ActorNumber].gameObject);
        //    playerListGameobjects.Remove(otherPlayer.ActorNumber);
        //    startGameButton.SetActive(CheckPlyersReady());
        //}
        //public override void OnLeftRoom()                                                                                                                                                                                       //Photon Removal
        //{
        //       // AudioListener.volume = 0.75f;// PlayerPrefs.GetFloat(PPConst.Volume);
        //    PlayerSelections.instance.BGM.volume = PlayerPrefs.GetFloat(PPConst.Music);
        //    ActivatePanel(gameOptionsUI.name);
        //    foreach (GameObject playerListGameobject in playerListGameobjects.Values)
        //    {
        //        Destroy(playerListGameobject);
        //    }
        //    playerListGameobjects.Clear();
        //    playerListGameobjects = null;
        //}
        //public override void OnMasterClientSwitched(Player newMasterClient)                                                                                                                                                                //Photon Removal
        //{
        //    if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        //    {
        //        startGameButton.SetActive(CheckPlyersReady());
        //    }
        //}
        //public override void OnJoinRoomFailed(short returnCode, string message)                                                                                                                                                            //Photon Removal
        //{
        //    ActivatePanel(gameOptionsUI.name);
        //    ShowError("Possible Reasons : Invalid Room ID / Room is Full / Game has Started");
        //}
        //public override void OnJoinRandomFailed(short returnCode, string message)                                                                                                                                                      //Photon Removal
        //{
        //    OnCreateRoomClicked();
        //    ShowError("No Rooms Availble. Creating A New Room");
        //}

        #endregion


        #region UI Methods
        private void ConnectToPhoton()
        {
            //Photon Removal  if (!changingRegion)
            //Photon Removal  StartCoroutine(CheckInternetConnection());
            ActivatePanel(loadingScreen.name);
            //Photon Removal  PhotonNetwork.ConnectUsingSettings();
        }

        public void OnJoinRandomClicked()
        {
            ActivatePanel(loadingScreen.name);
            //Photon Removal  PhotonNetwork.JoinRandomRoom();
        }
        public void OnCreateRoomClicked()
        {
            ActivatePanel(loadingScreen.name);
            string roomName = "Room" + UnityEngine.Random.Range(1000, 100000);
            //RoomOptions roomOptions = new RoomOptions();                                                  //Photon Removal
            //roomOptions.IsOpen = true;
            //roomOptions.IsVisible = true;
            //roomOptions.MaxPlayers = 4;
            //roomOptions.PlayerTtl = 30000;
            //roomOptions.EmptyRoomTtl = 30000;
            //PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        public void OnJoinPrivateClicked()
        {

            string roomName = privateRoomInput.text;
            if (!string.IsNullOrEmpty(roomName))
            {
                audioManager.PlaySound();
                ActivatePanel(loadingScreen.name);
                //Photon Removal  PhotonNetwork.JoinRoom(roomName);
            }
            else
            {
                ShowError("Enter a Room ID to Join");
            }
        }

        public void OnStartClicked()
        {
            EventManager.OnHideBanner?.Invoke();
            ActivatePanel(loadingScreen.name);
            //Photon Removal   PhotonNetwork.CurrentRoom.IsOpen = false;
            //Photon Removal   PhotonNetwork.LoadLevel(PlayerSelections.instance.selectedMap + 2);
        }

        public void Disconnect()
        {
            ActivatePanel(mainMenuUI.name);
            //Photon Removal   PhotonNetwork.Disconnect();
        }

        public void OnLeaveRoomClicked()
        {
            ActivatePanel(loadingScreen.name);
            //Photon Removal  PhotonNetwork.LeaveRoom();
        }

        public void CopyToClipBoard()
        {
            UniClipboard.SetText(roomPropsText.text);
        }

        string[] regionList = { "asia", "eu", "in", "ru", "us" };
        public void ChangeRegion(int regionIndex)
        {
            //if (regionList[regionIndex].Equals(PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion))                                                                             //Photon Removal
            //    return;
            //regionText.text = "Current Region : " + regionList[regionIndex];
            //PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = regionList[regionIndex];
            //StartCoroutine(ChangeRegion());

        }
        IEnumerator ChangeRegion()
        {
            changingRegion = true;
            //Photon Removal    PhotonNetwork.Disconnect();
            ActivatePanel(loadingScreen.name);
            yield return new WaitForSeconds(1f);
            ConnectToPhoton();
            changingRegion = false;
        }

        #endregion

        #region private Methods
        public void ActivatePanel(string panelToBeactivated)
        {
            mainMenuUI.SetActive(mainMenuUI.name.Equals(panelToBeactivated));
            gameOptionsUI.SetActive(gameOptionsUI.name.Equals(panelToBeactivated));
            lobbyUI.SetActive(lobbyUI.name.Equals(panelToBeactivated));
            loadingScreen.SetActive(loadingScreen.name.Equals(panelToBeactivated));
            GameSettingsMenuUI.SetActive(GameSettingsMenuUI.name.Equals(panelToBeactivated));
            LevelSelectionUI.SetActive(LevelSelectionUI.name.Equals(panelToBeactivated));
            ModeSelectionUI.SetActive(ModeSelectionUI.name.Equals(panelToBeactivated));
            ShopUI.SetActive(ShopUI.name.Equals(panelToBeactivated));
            GarageUI.SetActive(GarageUI.name.Equals(panelToBeactivated));
        }

        private bool CheckPlyersReady()
        {
            //if (!PhotonNetwork.IsMasterClient)                                                                                      //Photon Removal
            //{
            //    return false;
            //}
            //if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            //    return false;

            //foreach (Player player in PhotonNetwork.PlayerList)
            //{
            //    object isPlayerReady;
            //    if (player.CustomProperties.TryGetValue("PR", out isPlayerReady))
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

        void ShowError(string errorInfo)
        {

            audioManager.PlaySound("Error");
            errorInfoText.text = errorInfo;
            errorPopUp.SetActive(true);
        }

        IEnumerator CheckInternetConnection()
        {
            UnityWebRequest request = new UnityWebRequest("http://google.com");
            yield return request.SendWebRequest();
            if (request.error != null)
            {
                errorInfoText.text = "No Internet Connection ! \n Please Connect to Internet";
                errorPopUp.SetActive(true);
            }
        }

        //IEnumerator UpdatePing()                                                                                                                   //Photon Removal
        //{

        //    while (PhotonNetwork.IsConnectedAndReady)
        //    {
        //        float pingValue = PhotonNetwork.GetPing();
        //        pingText.text = pingValue + " ms";
        //        if (pingValue < 150)
        //        {
        //            pingIcon.fillAmount = 1.0f;
        //            pingIcon.color = Color.green;
        //            // pingText.text = "<color=green>"+pingValue+ "</color> ms";
        //        }
        //        else if (pingValue < 200)
        //        {
        //            pingIcon.fillAmount = 0.75f;
        //            pingIcon.color = Color.green;

        //        }
        //        else if (pingValue < 250)
        //        {
        //            pingIcon.fillAmount = 0.5f;
        //            pingIcon.color = Color.yellow;

        //        }
        //        else
        //        {
        //            pingIcon.fillAmount = 0.25f;
        //            pingIcon.color = Color.red;

        //        }


        //        yield return new WaitForSeconds(2);

        //    }

        //}
        #endregion
    }

}