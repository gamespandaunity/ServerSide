using UnityEngine;
 
 

public class PhotonTest //Photon Removal: MonoBehaviourPunCallbacks
{
    private static PhotonTest _instance;

    public static PhotonTest Instance
    {
        get
        {
            if (_instance == null)
            {
                //_instance = FindObjectOfType<PhotonTest>();
                //if (_instance == null)
                //{                                                                                                                             //Photon Removal
                //    GameObject go = new GameObject("PhotonTest");
                //    _instance = go.AddComponent<PhotonTest>();
                //}
            }
            return _instance;
        }
    }

    private void Start()
    {
        // Connect to Photon when the game starts
        ConnectToPhoton();
    }

    private void ConnectToPhoton()
    {
        //Photon Removal PhotonNetwork.ConnectUsingSettings(); // Connect to Photon using the settings from the PhotonServerSettings asset
    }

    //public override void OnConnectedToMaster()                                                                              //Photon Removal
    //{

    //    JoinLobby();
    //}


    public bool isLobbyJoined;

    //private void JoinLobby()
    //{                                                                                                           //Photon Removal
    //    PhotonNetwork.JoinLobby();

    //    isLobbyJoined = true;
    //}

    //public override void OnJoinedLobby()                                                                                        //Photon Removal
    //{

    //}

    //public override void OnDisconnected(DisconnectCause cause)
    //{
    //    ConstantsData_M.LogInfo($"Disconnected from Photon. Reason: {cause}");                                                         //Photon Removal
    //}
}
