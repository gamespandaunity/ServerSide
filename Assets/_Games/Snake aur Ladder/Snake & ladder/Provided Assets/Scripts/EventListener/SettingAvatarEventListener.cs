using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
namespace Snake_Ladder
{
    public class SettingAvatarEventListener : MonoBehaviour
    {
        public TMP_InputField nameInputField;
        [SerializeField] Image SelectedAvatar;
        [SerializeField] List<Image> AvatarsUI;

        int SelectedAvatarIndex;

        void OnEnable()
        {
          //  NetworkManager.Instance.Connected += OnConnected;
          //  NetworkManager.Instance.WrongRoomCode += OnRoomJoinFailed;
          //  NetworkManager.Instance.PublicRoomJoined += OnPublicRoomJoined;
          //  NetworkManager.Instance.NewPublicRoomCreated += OnNewPublicRoomCreated;
        }

        void OnDisable()
        {
          //  NetworkManager.Instance.Connected -= OnConnected;
          //  NetworkManager.Instance.WrongRoomCode -= OnRoomJoinFailed;
          //  NetworkManager.Instance.PublicRoomJoined -= OnPublicRoomJoined;
          //  NetworkManager.Instance.NewPublicRoomCreated -= OnNewPublicRoomCreated;
        }

        void OnConnected()
        {
            MenuManager.Instance.SetLoadingPopUpState(false);
        }

        void OnRoomJoinFailed(string errorText)
        {
            MenuManager.Instance.SetJoinFailedPopUpState(true, errorText);
            MenuManager.Instance.SetLoadingPopUpState(false);
        }


        void OnPublicRoomJoined()
        {
         //   NetworkManager.Instance.InstantiatePlayer(GameManager.instance.UserName, GameManager.instance.AvatarId);
            MenuManager.Instance.SetLoadingPopUpState(false);
            SceneManager.LoadScene("OnlineGameScene");
        }

        void OnNewPublicRoomCreated()
        { 
         //   NetworkManager.Instance.InstantiatePlayer(GameManager.instance.UserName, GameManager.instance.AvatarId);
            MenuManager.Instance.SetLoadingPopUpState(false);
            MenuManager.Instance.ChangeState(MenuManager.AllMenus.MatchFixedScreen);
        }



        private void Start()
        {
            SetAvatar();
        }
        private void SetAvatar()
        {
            //Wasi  int length = Math.Min(AvatarsUI.Count, FusionGameReferenceManager.Instance.Avatars.Count);
           //Wasi   for (int i = 0; i < length; i++)
            {
            //Wasi      AvatarsUI[i].sprite = FusionGameReferenceManager.Instance.Avatars[i];
            }

            SelectedAvatarIndex = SnakeGameManager.instance.AvatarId;
            SelectedAvatar.sprite = AvatarsUI[SelectedAvatarIndex].sprite;

            nameInputField.text = SnakeGameManager.instance.UserName;

        }

        public void OnClickAvatar(int index)
        {
            SelectedAvatar.sprite = AvatarsUI[index].sprite;
            SelectedAvatarIndex = index;
        }
        public void GetUserName()
        {
            if (nameInputField != null && string.IsNullOrEmpty(nameInputField.text))
                return;

            SnakeGameManager.instance.SetUserProperties(nameInputField.text, SelectedAvatarIndex);
            if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_OnlineFriend))
            {
                MenuManager.Instance.ChangeState(MenuManager.AllMenus.LobbyScreen);
            }
            else if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_RandomPlayer))
            {
                MenuManager.Instance.SetLoadingPopUpState(true);
             //   NetworkManager.Instance.JoinPublicRandomRoom();
            }

        }

    }
}