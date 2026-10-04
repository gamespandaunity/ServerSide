using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twelve
{
    public class ModeSelectionEventListenerTwelve : MonoBehaviour
{
    void OnEnable()
    {
            //Photon Removal  NetworkManagerTwelve.Instance.Connected += OnConnected;

        }
        void OnDisable()
    {
            //Photon Removal   NetworkManagerTwelve.Instance.Connected += OnConnected;

        }
        void OnConnected()
    {
        MenuManagerTwelve.Instance.SetLoadingPopUpState(false);
    }


    // Online
    public void PlayOnlineWithFriend()
    {
        MenuManagerTwelve.Instance.SetLoadingPopUpState(true);
            //Photon Removal  NetworkManagerTwelve.Instance.ConnectServer();
            Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;
        MenuManagerTwelve.Instance.ChangeState(MenuManagerTwelve.AllMenus.AvatarScreen);
    }
    public void PlayOnline()
    {
        MenuManagerTwelve.Instance.SetLoadingPopUpState(true);
            //Photon Removal  NetworkManagerTwelve.Instance.ConnectServer();
            Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_RandomPlayer;
        MenuManagerTwelve.Instance.ChangeState(MenuManagerTwelve.AllMenus.AvatarScreen);
    }


    // Offline
    public void PlayOfflineWithFriend()
    {
            GameConstants.isWithAI = false;
            Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_LocalPlayer;
        SceneManager.LoadScene("12OfflineGameScene");
    }
    public void PlayWithAI()
    {
            Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_Ai;
            //Photon Removal    NetworkManagerTwelve.Instance.disconnectNetwork();
            GameConstants.isWithAI = true;
        SceneManager.LoadScene("12OfflineGameScene");
    }
}
}
