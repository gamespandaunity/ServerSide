//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using UnityExtensions;


[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Gameplay Options Handler")]
public class HR_UIOptionsHandler : NetworkBehaviour
{

    public GameObject pausedMenu;
    public GameObject pausedButtons;
    public GameObject optionsMenu;
    public GameObject restartButton;

    private void OnEnable()
    {

        HR_GamePlayHandler.OnPaused += OnPaused;
        HR_GamePlayHandler.OnResumed += OnResumed;

        if (PlayerPrefs.GetInt("Multiplayer", 0) == 1)
            restartButton.SetActive(false);
        else
            restartButton.SetActive(true);

    }

    public void ResumeGame()
    {

        HR_GamePlayHandler.Instance.Paused();

    }

    public void RestartGame()
    {

        HR_GamePlayHandler.Instance.RestartGame();

    }
    public static bool IsHighWayGameQuite;
    private bool isLeavingMultiplayer;

    public void MainMenu()
    {
        IsHighWayGameQuite = true;

        if (PlayerPrefs.GetInt("Multiplayer", 0) == 1 && NetworkClient.isConnected)
        {
            if (!isLeavingMultiplayer)
                StartCoroutine(LeaveHighwayMultiplayer());

            return;
        }

        if (SceneManager.GetActiveScene().name == "HighwaySunny")
        {

            SceneManager.LoadScene("Home");
        }
        else
        {
            //Photon Removal    PhotonNetwork.LeaveRoom();
            if (NetworkClient.isConnected)
            {
                  MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                if (NetworkGameManager.Instance)
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                this.Delay(1, () => NetworkManager.singleton.StopClient());
                SceneManager.LoadScene("Home");
            }                                      //Photon Removal
            SceneManager.LoadScene("Home");

        }

    }

    private IEnumerator LeaveHighwayMultiplayer()
    {
        isLeavingMultiplayer = true;

        bool forfeitRequested = HR_NetworkManager.Instance != null &&
                                HR_NetworkManager.Instance.RequestLocalForfeit();
        if (!forfeitRequested)
            Debug.LogWarning("[HighwayServerResult] Could not send pause-menu forfeit before leaving.");

        // Give the reliable Command time to reach the server before this scene and
        // the locally owned car are destroyed. The server derives the opponent winner.
        yield return new WaitForSecondsRealtime(.5f);

        if (MirrorNetwork.Instance != null)
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;

        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.CmdSendDisconnectType((int)DisconnectReason.ApplicationQuit);

        yield return new WaitForSecondsRealtime(.2f);

        if (NetworkClient.active && NetworkManager.singleton != null)
            NetworkManager.singleton.StopClient();

        SceneManager.LoadScene("Home");
    }

    public void OptionsMenu(bool open)
    {

        optionsMenu.SetActive(open);

        if (open)
            pausedButtons.SetActive(false);
        else
            pausedButtons.SetActive(true);

    }

    private void OnPaused()
    {

        pausedMenu.SetActive(true);
        pausedButtons.SetActive(true);

        if (PlayerPrefs.GetInt("Multiplayer", 0) == 1)
            return;

        AudioListener.pause = true;
        Time.timeScale = 0;

    }

    public void OnResumed()
    {

        pausedMenu.SetActive(false);
        pausedButtons.SetActive(false);

        if (PlayerPrefs.GetInt("Multiplayer", 0) == 1)
            return;

        AudioListener.pause = false;
        Time.timeScale = 1;

    }

    public void ChangeCamera()
    {

        if (GameObject.FindObjectOfType<HR_CarCamera>())
            GameObject.FindObjectOfType<HR_CarCamera>().ChangeCamera();

    }

    private void OnDisable()
    {

        HR_GamePlayHandler.OnPaused -= OnPaused;
        HR_GamePlayHandler.OnResumed -= OnResumed;

    }

}
