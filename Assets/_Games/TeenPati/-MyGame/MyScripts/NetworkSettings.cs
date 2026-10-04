// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using TMPro;
// using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
// using Mirror;


// namespace TeenPattiGame
// {

//     public class NetworkSettings : NetworkBehaviour
//     {

//         public static NetworkSettings Instance;
//         public bool checkGameStart;
//         public GameObject menuPanel, loadingPanel;
//         public int minimumBetAmount;
//         public static List<RoomInfo> roomInfos;

//         TypedLobby sqlLobby = new TypedLobby("MySqlLobby", LobbyType.SqlLobby);

//         public string RoomName;
//         private Menu_Manager menu_Manager;
//         private void Awake()
//         {
//             if (Instance == null)
//                 Instance = this;

//         }



//         // Start is called before the first frame update
//         void Start()
//         {

//             if (!MatchHandler.isOffline())
//                 ActiveMyPanel(loadingPanel.name);
//             if (PlayerPrefs.HasKey("name"))
//                 PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("name") /*+ Random.Range(100, 1000)*/;
//             //PhotonNetwork.LocalPlayer.NickName = "Player " + Random.Range(100, 1000);
//             menu_Manager = Menu_Manager.Instance;
//             if (PhotonNetwork.IsConnected)
//             {
//                 menu_Manager.Active_My_Panel(menu_Manager.Main_Menu_Panel.name);
//             }
//             else
//                 PhotonNetwork.ConnectUsingSettings();

//             //PhotonNetwork.PhotonServerSettings.AppSettings.Server = "127.0.0.1";
//             //PhotonNetwork.PhotonServerSettings.AppSettings.Port = 5055;
//             //PhotonNetwork.ConnectUsingSettings();
//             //loadScene();
//             //StartCoroutine(LoadLevelAsync());
//         }



//         #region swtich Room

//         public override void OnRoomListUpdate(List<RoomInfo> roomList)
//         {
//             if (roomInfos != null)
//                 roomInfos.Clear();
//             roomInfos = roomList;
//             Debug.Log("Room Name: " + roomInfos.Count + " | Player Count: " + "/");
//             foreach (RoomInfo roomInfo in roomInfos)
//             {
//                 if (roomInfo == null)
//                 {
//                     roomInfos.Remove(roomInfo);
//                 }

//                 Debug.Log(roomInfo.CustomProperties +
//                  "   Room Name: " + roomInfo.Name + " | Player Count: " + roomInfo.PlayerCount + "/" + roomInfo.MaxPlayers);

//             }
//         }



//         public void Enter_Room_With_Password(string room_Pass)
//         {
//             if (string.IsNullOrEmpty(room_Pass))
//             {
//                 LocalSettings.Show_Dialogue(menu_Manager.dialogueBoxPanel, "Enter Password");
//                 return;
//             }

//             StartCoroutine(check_Connected_And_Ready(room_Pass));

//         }
//         private RoomInfo room_info;
//         IEnumerator check_Connected_And_Ready(string room_Pass)
//         {
//             PhotonNetwork.ConnectUsingSettings();
//             yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);

//             if (!PhotonNetwork.JoinLobby())
//                 PhotonNetwork.JoinLobby();
//             loadingPanel.SetActive(true);
//             yield return new WaitForSeconds(0.5f);
//             bool isRoomIdTrue = checkRoomId(room_Pass);

//             if (isRoomIdTrue)
//             {
//                 if (room_info.PlayerCount != LocalSettings.GetMaxPlayers())
//                     RoomEntranceProperty(room_Pass);
//                 else
//                 {
//                     loadingPanel.SetActive(false);
//                     LocalSettings.Show_Dialogue(menu_Manager.dialogueBoxPanel, "Please wait....Room is full");
//                 }
//             }
//             else
//             {
//                 loadingPanel.SetActive(false);
//                 LocalSettings.Show_Dialogue(menu_Manager.dialogueBoxPanel, " InCorrect Password");
//             }

//         }
//         public bool checkRoomId(string room_Pass)
//         {



//             foreach (RoomInfo roomInfo in roomInfos)
//             {
//                 // Debug.LogError("PlayerCOunt......" + roomInfo.PlayerCount);
//                 if (roomInfo.PlayerCount == 0)
//                 {
//                     // Debug.LogError("PlayerCOunt....22.." + roomInfo.PlayerCount);
//                     roomInfos.Remove(roomInfo);
//                 }
//             }

//             foreach (RoomInfo item in roomInfos)
//             {
//                 string roomInfoName = stripString(item.CustomProperties.ToStringFull());
//                 if (room_Pass == roomInfoName)
//                 {
//                     room_info = item;
//                     return true;
//                 }
//             }
//             return false;
//         }



//         string stripString(string str)
//         {
//             return str.Substring(1, str.Length - 4);
//         }


//         #endregion

//         #region EnterNewRoom





//         // Joined Room With Password



//         string cheeckStringBool(RoomInfo roomInfo)
//         {
//             string Name = roomInfo.CustomProperties.ToString();
//             if (Name.Contains(RoomName))
//             {
//                 Name = RoomName;
//             }
//             return Name;
//         }

//         public void RoomEntranceProperty(string RoomID)
//         {
//             // AssignMimumBet(EnumNumber);
//             if (LocalSettings.GetTotalChips() <= LocalSettings.MinBetAmount)
//             {
//                 LocalSettings.Show_Dialogue(menu_Manager.dialogueBoxPanel, "you Could not enough Chips");
//             }

//             MatchHandler.CurrentMatch = MatchHandler.MATCH.Classic;
//             // Debug.LogError("MinimumBetAmount   " + LocalSettings.Rs(LocalSettings.MinBetAmount));
//             // return;
//             ActiveMyPanel(loadingPanel.name);
//             RoomName = RoomID;

//             LocalSettings.Room_Password = RoomID;



//             PhotonHashtable NORMAL = new PhotonHashtable() { { RoomName.ToString(), 1 } };
//             byte roomEntranceNumber = (byte)LocalSettings.GetMaxPlayers();
//             PhotonNetwork.JoinRandomRoom(NORMAL, roomEntranceNumber);
//             // PhotonNetwork.JoinRandomRoom(null, 0, MatchmakingMode.FillRoom, sqlLobby);
//             ///

//         }


//         public bool checkRoomsStatus(string Room_Pass)
//         {
//             if (roomInfos.Count == 0 || roomInfos != null)
//                 return true;
//             foreach (RoomInfo roomInfo in roomInfos)
//             {
//                 // Debug.LogError("PlayerCOunt......" + roomInfo.PlayerCount);
//                 if (roomInfo.PlayerCount == 0)
//                 {
//                     // Debug.LogError("PlayerCOunt....22.." + roomInfo.PlayerCount);
//                     roomInfos.Remove(roomInfo);
//                 }
//             }
//             for (int i = 0; i < roomInfos.Count; i++)
//             {
//                 string roomInfoName = stripString(roomInfos[i].CustomProperties.ToStringFull());
//                 string SameRoomName = RoomInfosName(roomInfoName);
//                 if (SameRoomName == Room_Pass)
//                 {
//                     Debug.Log("Room Name  " + RoomName + " SameRoomName " + SameRoomName);
//                     if (roomInfoName[roomInfoName.Length - 1].ToString() == "1")


//                         return false;
//                 }


//             }
//             return true;
//         }

//         string RoomInfosName(string RoomInfoName)
//         {
//             string name = "";
//             for (int i = 0; i < RoomInfoName.Length; i++)
//             {
//                 if (i != RoomInfoName.Length - 1)
//                     name += RoomInfoName[i];
//             }

//             return name;
//         }


//         int checkLastDigitOFTable()
//         {

//             if (!string.IsNullOrEmpty(RoomName))
//             {
//                 for (int i = 0; i < RoomName.Length; i++)
//                 {
//                     if (i == RoomName.Length - 1)
//                     {
//                         string number = RoomName[i].ToString();
//                         if (number == LocalSettings.extraRoomCounter.ToString())
//                         {
//                             Debug.Log("last Char  " + (char)RoomName[i] + "   " + LocalSettings.extraRoomCounter);
//                             if (LocalSettings.extraRoomCounter == 0)
//                                 return 1;
//                         }

//                     }
//                 }


//             }
//             return 0;
//         }


//         #endregion

//         #region Goto other room when switch room

//         public void getRoomList()
//         {
//             //RoomInfo[] rooms = PhotonNetwork.GetRoomList();
//             TypedLobby loby = TypedLobby.Default;
//             Debug.Log("loby name: " + loby.Name);
//         }
//         void JoinExistingRoom()
//         {

//         }

//         void joinNewRoom()
//         {
//             byte expectedMaxPlayers;
//             RoomOptions roomOptions = new RoomOptions();
//             expectedMaxPlayers = (byte)LocalSettings.GetMaxPlayers();
//             roomOptions.MaxPlayers = expectedMaxPlayers;

//             PhotonNetwork.CreateRoom(RoomName, roomOptions, TypedLobby.Default);

//         }

//         #endregion

//         #region LoadScene
//         AsyncOperation asyncOperation;
//         public void playgame()
//         {
//             checkGameStart = false;
//             PhotonNetwork.LoadLevel("Gameplay");
//             //asyncOperation.allowSceneActivation = true;
//         }



//         void ActiveMyPanel(string myPanelName)
//         {
//             menuPanel.SetActive(myPanelName.Equals(menuPanel.name));
//             loadingPanel.SetActive(myPanelName.Equals(loadingPanel.name));
//         }

//         private void Update()
//         {
//             if (checkGameStart)
//             {
//                 CheckGameStart();
//             }
//         }

//         void CheckGameStart()
//         {
//             if (PhotonNetwork.CurrentRoom.PlayerCount >= 1)
//             {
//                 PhotonNetwork.CurrentRoom.IsOpen = true;
//                 playgame();
//             }

//         }
//         #endregion

//         #region Assign Player Things
//         // ye method int value k lye tha jab hub profile picture Game main se set kr rahe thy
//         public void AssignPicToPlayerProperties(int val)
//         {
//             PhotonNetwork.LocalPlayer.SetCustomData(LocalSettings.ProfilePic, val);
//         }
//         // ye method int value k lye tha jab hub profile picture Server  main se Set kr rahe thy
//         public void AssignPicToPlayerPropertiesStringForm(string val)
//         {
//             //  Debug.LogError("Check Name of image...." + val);
//             PhotonNetwork.LocalPlayer.SetCustomString(LocalSettings.ProfilePicNameKey, val);
//         }

//         public void AssignFrameToPlayerProperties(int val)
//         {
//             PhotonNetwork.LocalPlayer.SetCustomData(LocalSettings.ProfileFrame, val);
//         }

//         #endregion

//         #region Pun2 Callbacks

//         public override void OnConnectedToMaster()
//         {
//             Debug.Log("Connected To Master");


//             if (!PhotonNetwork.JoinLobby())
//                 PhotonNetwork.JoinLobby();
//             loadingPanel.SetActive(false);

//             ActiveMyPanel(menuPanel.name);
//             AssignPicToPlayerProperties(LocalSettings.GetprofilePic());

//             AssignFrameToPlayerProperties(LocalSettings.GetprofileFrame());
//             PhotonNetwork.LocalPlayer.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
//             //StartCoroutine(SwtichRoom());
//         }

//         void OnPhotonRandomJoinFailed()
//         {
//             byte expectedMaxPlayers;
//             RoomOptions roomOptions = new RoomOptions();


//             expectedMaxPlayers = (byte)LocalSettings.GetMaxPlayers();

//             roomOptions.MaxPlayers = expectedMaxPlayers;
//             //Debug.LogError("TotalPlayer" + roomOptions.MaxPlayers);
//             roomOptions.CustomRoomProperties = new PhotonHashtable() { { RoomName.ToString(), 1 } };
//             roomOptions.CustomRoomPropertiesForLobby = new string[] { RoomName.ToString() };


//             PhotonNetwork.CreateRoom(null, roomOptions, TypedLobby.Default);
//             //Debug.LogError("Check Lobby Name....." + sqlLobby.Name);
//             //Debug.LogError("Check Lobby Name....." + sqlLobby);


//         }
//         public override void OnJoinRandomFailed(short returnCode, string message)
//         {
//             OnPhotonRandomJoinFailed();
//         }

//         public override void OnJoinedRoom()
//         {
//             if (PhotonNetwork.IsMasterClient)
//             {
//                 Debug.Log("Player Joined ");


//                 checkGameStart = true;

//             }





//         }


//         public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
//         {
//             if (changedProps.ContainsKey(LocalSettings.ProfilePic))
//             {
//                 Debug.Log("Pic Updated");
//             }
//             Debug.Log("Call Wasted");
//             base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
//         }

//         #endregion
//     }
// }