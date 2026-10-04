using UnityEngine;
using System.Collections;
 
using UnityEngine.SceneManagement;
using Mirror;

public class PauseMenuManager : NetworkBehaviour
{
    private bool isLeavingMultiplayer;

    void OnEnable()
    {
        Time.timeScale = 0.001f;
        AudioListener.volume = 0;

    }

    public void Resume()
    {
        gameObject.SetActive(false);
        Time.timeScale = MConstants.TIME_SCALE;
        if (PlayerDataController.instance != null && PlayerDataController.instance.playerStats.isSoundOn)
        {
            AudioListener.volume = 1;
        }

    }
    public static bool IsGameQuite;
    public void Continue()
    {
        IsGameQuite = true;

        if (NetworkClient.isConnected && !MultiPlayerGame.isSinglePlayer)
        {
            if (!isLeavingMultiplayer)
                StartCoroutine(LeaveMultiplayerAfterForfeit());
            return;
        }

        CompleteLeave();
    }

    private IEnumerator LeaveMultiplayerAfterForfeit()
    {
        isLeavingMultiplayer = true;
        if (HorseMirrorGameManager.Instance != null)
            HorseMirrorGameManager.Instance.RequestLocalForfeit();

        // Let the reliable forfeit Command reach the Horse server before this
        // client unloads the race scene.
        yield return new WaitForSecondsRealtime(.5f);

        if (MirrorNetwork.Instance != null)
        {
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            Debug.Log("DisconnectReason.ApplicationPause" + MirrorNetwork.Instance.currentDisconnectReason);
        }
        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.CmdSendDisconnectType((int)DisconnectReason.ApplicationQuit);

        yield return new WaitForSecondsRealtime(.2f);
        CompleteLeave();
    }

    private void CompleteLeave()
    {
        HudMenuManager.instance.loadingScreen.SetActive(true);
        Time.timeScale = 1;
        MainMenuManager.isGoToGrage = true;
        //Photon Removal   PhotonNetwork.DestroyAll(true);

        //Photon Removal   PhotonNetwork.OfflineMode = false;
        SceneManager.LoadScene("Home");
        //Photon Removal    PhotonNetwork.LeaveRoom();
        //Photon Removal    PhotonNetwork.Disconnect();
        if (PlayerDataController.instance != null && PlayerDataController.instance.playerStats.isSoundOn)
        {
            //AudioListener.volume = 1;
        }
    }

    public void Retry()
    {
        HudMenuManager.instance.loadingScreen.SetActive(true);
        Time.timeScale = 1;
        if (PlayerDataController.instance != null && PlayerDataController.instance.playerStats.isSoundOn)
        {
           // AudioListener.volume = 1;
        }
        switch (PlayerDataController.instance.playerStats.CurrentEnvironment)
        {
            case 1:
                SceneManager.LoadScene("Race_Track_02-2");

                break;

            case 2:
                SceneManager.LoadScene("Race_Track_01");

                break;
            case 3:
                SceneManager.LoadScene("Race_Track_03_Final");

                break;
            case 4:
                SceneManager.LoadScene("Race_Track_02-2");

                break;
        }

        //MainMenuManager.Instance.showMenu (MenuNames.ENVIORNMENT_SELECTION);
    }
}
