using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Snake_Ladder
{

    public class LobbyEventListener : MonoBehaviour
    {
        [Header("MainMenuScreen")]
        [SerializeField] TMP_InputField CodeText;

        private void Start()
        {
          //  NetworkManager.Instance.OnRoomJoined += HandleRoomJoined;
          //  NetworkManager.Instance.StartGame += onStartGame;
          //  NetworkManager.Instance.WrongRoomCode += onRoomJoinFailed;
        }

        private void OnDestroy()
        {
          //  NetworkManager.Instance.OnRoomJoined -= HandleRoomJoined;
         //   NetworkManager.Instance.StartGame -= onStartGame;
         //   NetworkManager.Instance.WrongRoomCode -= onRoomJoinFailed;
        }



        public void HandleRoomJoined()
        {
            Debug.Log("Creating UI");
          //  NetworkManager.Instance.InstantiatePlayer(GameManager.instance.UserName, GameManager.instance.AvatarId);
            MenuManager.Instance.SetLoadingPopUpState(false);

          //  if (!NetworkManager.Instance.IsAlone())
            {
                SceneManager.LoadScene("OnlineGameScene");
            }

        }

        void onRoomJoinFailed(string errorText)
        {
            MenuManager.Instance.SetJoinFailedPopUpState(true, errorText);
            MenuManager.Instance.SetLoadingPopUpState(false);
        }
        // Ui Button's EventListners
        public void OnJoinCodeRequest()
        {
        //    NetworkManager.Instance.JoinRoom(CodeText.text);
            StartCoroutine(Loading());
        }

        public void OnCreateRoom()
        {
         //   NetworkManager.Instance.CreateRoom();

            StartCoroutine(Loading());
            MenuManager.Instance.ChangeState(MenuManager.AllMenus.MatchFixedScreen);
        }
        IEnumerator Loading()
        {
            MenuManager.Instance.SetLoadingPopUpState(true);
            yield return new WaitForSeconds(1f);
            MenuManager.Instance.SetLoadingPopUpState(false);

        }

        void onStartGame()
        {
            // NetworkManager.Instance.SetRoomLockState(true);
            // SceneManager.LoadScene("OnlineGameScene");
        }
    }
}