
using UnityEngine;
 
 
using System.Collections;
using System.Collections.Generic;
using System;
using static staticVariables;
using static ServerConnection;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GamesPandaLauncher //Photon Removal: MonoBehaviourPunCallbacks
{



    [Tooltip("The maximum number of players per room")]
    [SerializeField]
    private byte maxPlayersPerRoom = 2;
    public static string RoomName;
    public static bool IsMaster;
    public static string SceneToLoad;

    public CreateBetResponseModel currentBetData;

    #region Private Fields

    bool isConnecting;
    public string challenge_transaction_id;
    public string MessageContent;
    public string currentRoomId = "";
    public string winLoseChallengeId = "";
    public string userIdentifierOrEmail = "";

    string gameVersion = "1";
    public static int currentGameId = 1;

    public CreateBetResponseModel userModeliq;


    #endregion

    #region MonoBehaviour CallBacks

    void Awake()
    {

        PlayerPrefs.SetInt("Multiplayer", 1);

        //Photon Removal PhotonNetwork.AutomaticallySyncScene = true;
        Connect();

    }

    #endregion


    #region Public Methods

    public void Connect()
    {

        isConnecting = true;


        //Photon Removal   if (PhotonNetwork.IsConnected)
        {
            LogFeedback("Joining Room...");
            //Photon Removal       PhotonNetwork.JoinRoom(RoomName);
        }
        //Photon Removal  else
        {

            LogFeedback("Connecting...");

            //Photon Removal     PhotonNetwork.ConnectUsingSettings();
            //Photon Removal     PhotonNetwork.GameVersion = this.gameVersion;
        }
    }

    void LogFeedback(string message)
    {
    }

    #endregion


    #region MonoBehaviourPunCallbacks CallBacks
    //public override void OnConnectedToMaster()
    //{
    //    if (isConnecting)
    //    {
    //        LogFeedback("OnConnectedToMaster: Next -> try to Join Random Room");
    //        if (IsMaster)
    //        {
    //            RoomOptions roomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = 2 };                                                                                                                   //Photon Removal
    //            PhotonNetwork.CreateRoom(RoomName, roomOptions);
    //        }
    //        else
    //            StartCoroutine(WaitForJoinRoom());
    //    }
    //}
    //IEnumerator WaitForJoinRoom()
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(1f);
    //        if (PhotonNetwork.InRoom)
    //        {                                                                                                                                                                                                                            //Photon Removal
    //            LogFeedback("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
    //            break;
    //        }
    //        PhotonNetwork.JoinRoom(RoomName);

    //    }
    //}
    //public override void OnJoinRoomFailed(short returnCode, string message)
    //{                                                                                                                                                                                                                           //Photon Removal
    //    base.OnJoinRoomFailed(returnCode, message);
    //}



    //public override void OnDisconnected(DisconnectCause cause)
    //{
    //    LogFeedback("<Color=Red>OnDisconnected</Color> " + cause);                                                                                                                                             //Photon Removal

    //}

    //public override void OnJoinedRoom()
    //{
    //    LogFeedback("<Color=Green>OnJoinedRoom</Color> with " + PhotonNetwork.CurrentRoom.PlayerCount + " Player(s)");
                                                                                                                                                                                                               //Photon Removal
    //}
    //public static Player Opponent;
    //public override void OnPlayerEnteredRoom(Player newPlayer)
    //{
    //    Opponent = newPlayer;                                                                                                                                                                             //Photon Removal
    //    base.OnPlayerEnteredRoom(newPlayer);
    //    if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
    //    {
    //        PhotonNetwork.LoadLevel(SceneToLoad);

    //    }
    //}
    #endregion



}
