using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Snake_Ladder;

namespace Twelve
{
    public class LobbyEventListenerTwelve : MonoBehaviour
{
    [Header("MainMenuScreen")]
    [SerializeField] TMP_InputField CodeText;
  
    private void Start()
    {
            //NetworkManagerTwelve.Instance.OnRoomJoined += HandleRoomJoined;
            //NetworkManagerTwelve.Instance.StartGame += onStartGame;                                                                       //Photon Removal
            //NetworkManagerTwelve.Instance.WrongRoomCode += onRoomJoinFailed;
        }

        private void OnDestroy()
    {
            //NetworkManagerTwelve.Instance.OnRoomJoined -= HandleRoomJoined;
            //NetworkManagerTwelve.Instance.StartGame -= onStartGame;                                                                                                   //Photon Removal
            //NetworkManagerTwelve.Instance.WrongRoomCode -= onRoomJoinFailed;
        }



        public void HandleRoomJoined()
    {
            //Debug.Log("Creating UI");
            //NetworkManagerTwelve.Instance.InstantiatePlayer(Snake_Ladder.GameManager.instance.UserName, Snake_Ladder.GameManager.instance.AvatarId);
            //MenuManagerTwelve.Instance.SetLoadingPopUpState(false);                                                                                                            //Photon Removal

            //if(!NetworkManagerTwelve.Instance.IsAlone())
            //{
            //    SceneManager.LoadScene("12OnlineGameScene");
            //}

        }

        void onRoomJoinFailed(string errorText)
    {
        MenuManagerTwelve.Instance.SetJoinFailedPopUpState(true, errorText);
        MenuManagerTwelve.Instance.SetLoadingPopUpState(false);
    }
    // Ui Button's EventListners
    public void OnJoinCodeRequest()
    {
            //NetworkManagerTwelve.Instance.JoinRoom(CodeText.text);                                                //Photon Removal
            StartCoroutine(Loading());
    }

    public void OnCreateRoom()
    {
            //NetworkManagerTwelve.Instance.CreateRoom();                                                                                      //Photon Removal

            StartCoroutine(Loading());
        MenuManagerTwelve.Instance.ChangeState(MenuManagerTwelve.AllMenus.MatchFixedScreen);
    }
    IEnumerator Loading()
    {
        MenuManagerTwelve.Instance.SetLoadingPopUpState(true);
        yield return new WaitForSeconds(1f);
        MenuManagerTwelve.Instance.SetLoadingPopUpState(false);
   
    }

    void onStartGame()
    {
     
    }
}
}
