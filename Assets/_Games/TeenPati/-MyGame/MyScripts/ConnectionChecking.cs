using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectionChecking : NetworkBehaviour
{

    public GameObject loadingPanel;
    public GameObject NointernetConnection;
    public Text loadingText;

    private bool networkSpeedIsGood = false;

    private void Start()
    {

        loadingPanel.SetActive(false);
        NointernetConnection.SetActive(false);
        // StartCoroutine(CheckNetworkSpeedAndConnect());
    }
    bool isdisconnected;
    private void FixedUpdate()
    {
        if (!isdisconnected)
            return;
        if (Application.internetReachability == NetworkReachability.NotReachable)
            return;

        //photon removal  ConnectToMaster();
        if (NointernetConnection.activeInHierarchy)
            NointernetConnection.SetActive(false);
    }

    // public override void OnDisconnected(DisconnectCause cause)
    // {
    //     // Handle disconnection
    //     Debug.LogError("Disconnected from internet");
    //     //Game_Play.Instance.StandUp();
    //     if (!MatchHandler.isOffline())
    //     {
    //         if (Application.internetReachability == NetworkReachability.NotReachable)                //photon removal
    //         {
    //             if (!NointernetConnection.activeInHierarchy)
    //                 NointernetConnection.SetActive(true);
    //             isdisconnected = true;
    //             return;
    //         }
    //         if (NointernetConnection.activeInHierarchy)
    //             NointernetConnection.SetActive(false);
    //         loadingPanel.SetActive(true);


    //         if (cause != DisconnectCause.DisconnectByClientLogic) // Avoid infinite loop
    //         {
    //             ConnectToMaster(); // Reconnect to master server if disconnected
    //         }
    //     }
    // }
    // private void ConnectToMaster()
    // {
    //     PhotonNetwork.ConnectUsingSettings();
    //     //loadingPanel.SetActive(false);// Connect to the master server
    //     //isdisconnected = true;
    //     if (!MatchHandler.isOffline())//photon removal
    //         if (SceneManager.GetActiveScene().name == "Gameplay")
    //             SceneManager.LoadScene("MainMenu");

    // }

    // public override void OnConnectedToMaster()
    // {
    //     NointernetConnection.SetActive(false);//photon removal
    //     loadingPanel.SetActive(false);
    //     isdisconnected = false;

    // }


}