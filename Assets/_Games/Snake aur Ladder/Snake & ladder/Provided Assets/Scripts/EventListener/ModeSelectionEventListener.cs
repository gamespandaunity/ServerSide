using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Snake_Ladder
{
    public class ModeSelectionEventListener : MonoBehaviour
    {
        //void OnEnable()
        //{
        //    NetworkManager.Instance.Connected += OnConnected;

        //}
        //void OnDisable()
        //{
        //    NetworkManager.Instance.Connected += OnConnected;

        //}
        //void OnConnected()
        //{
        //    MenuManager.Instance.SetLoadingPopUpState(false);
        //}


        // Online
        public void PlayOnlineWithFriend()
        {
            MenuManager.Instance.SetLoadingPopUpState(true);
            // NetworkManager.Instance.ConnectServer();
            SnakeGameManager.instance.currentGameMode = SnakeGameManager.GameMode.Against_OnlineFriend;
            MenuManager.Instance.ChangeState(MenuManager.AllMenus.AvatarScreen);
        }
        public void PlayOnline()
        {
            MenuManager.Instance.SetLoadingPopUpState(true);
            // NetworkManager.Instance.ConnectServer();
            SnakeGameManager.instance.currentGameMode = SnakeGameManager.GameMode.Against_RandomPlayer;
            MenuManager.Instance.ChangeState(MenuManager.AllMenus.AvatarScreen);
        }


        // Offline
        public void PlayOfflineWithFriend()
        {
            SnakeGameConstants.isWithAI = false;
            SnakeGameManager.instance.currentGameMode = SnakeGameManager.GameMode.Against_LocalPlayer;
            SceneManager.LoadScene(1);
        }
        public void PlayWithAI()
        {
            SnakeGameManager.instance.currentGameMode = SnakeGameManager.GameMode.Against_Ai;
           // NetworkManager.Instance.DisconnectFromServer();
            SnakeGameConstants.isWithAI = true;
            SceneManager.LoadScene(1);
        }
    }
}