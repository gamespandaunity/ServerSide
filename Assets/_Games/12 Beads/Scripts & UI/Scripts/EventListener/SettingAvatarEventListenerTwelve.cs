using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using Snake_Ladder;

namespace Twelve
{
    public class SettingAvatarEventListenerTwelve : MonoBehaviour
{
    public TMP_InputField nameInputField;
    [SerializeField] Image SelectedAvatar;
    [SerializeField] List<Image> AvatarsUI;

    int SelectedAvatarIndex;

    void OnEnable()
    {
            //NetworkManagerTwelve.Instance.Connected += OnConnected;
            //NetworkManagerTwelve.Instance.WrongRoomCode += OnRoomJoinFailed;
            //NetworkManagerTwelve.Instance.PublicRoomJoined += OnPublicRoomJoined;
            //NetworkManagerTwelve.Instance.NewPublicRoomCreated += OnNewPublicRoomCreated;                      //Photon Removal
        }

        void OnDisable()
    {
            //NetworkManagerTwelve.Instance.Connected -= OnConnected;                                                                                      //Photon Removal
            //NetworkManagerTwelve.Instance.WrongRoomCode -= OnRoomJoinFailed;
            //NetworkManagerTwelve.Instance.PublicRoomJoined -= OnPublicRoomJoined;
            //NetworkManagerTwelve.Instance.NewPublicRoomCreated -= OnNewPublicRoomCreated;
        }

        void OnConnected()
    {
        MenuManagerTwelve.Instance.SetLoadingPopUpState(false);
    }

    void OnRoomJoinFailed(string errorText)
    {
        MenuManagerTwelve.Instance.SetJoinFailedPopUpState(true, errorText);
        MenuManagerTwelve.Instance.SetLoadingPopUpState(false);
    }


    void OnPublicRoomJoined()
    {
            //Photon Removal   NetworkManagerTwelve.Instance.InstantiatePlayer(Snake_Ladder.GameManager.instance.UserName, Snake_Ladder.GameManager.instance.AvatarId);
            MenuManagerTwelve.Instance.SetLoadingPopUpState(false);
        SceneManager.LoadScene("OnlineGameScene");
    }

    void OnNewPublicRoomCreated()
    {
            //Photon Removal  NetworkManagerTwelve.Instance.InstantiatePlayer(Snake_Ladder.GameManager.instance.UserName, Snake_Ladder.GameManager.instance.AvatarId);
            MenuManagerTwelve.Instance.SetLoadingPopUpState(false);
        MenuManagerTwelve.Instance.ChangeState(MenuManagerTwelve.AllMenus.MatchFixedScreen);
    }



    private void Start()
    {
        SetAvatar();
    }
    private void SetAvatar()
    {
        int length = Math.Min(AvatarsUI.Count, ReferenceManager.Instance.avatarSprites.Count);
        for (int i = 0; i < length; i++)
        {
            AvatarsUI[i].sprite = ReferenceManager.Instance.avatarSprites[i];
        }

        SelectedAvatarIndex = Snake_Ladder.GameManager.instance.AvatarId;
        SelectedAvatar.sprite = AvatarsUI[SelectedAvatarIndex].sprite;

        nameInputField.text = Snake_Ladder.GameManager.instance.UserName;

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

            Snake_Ladder.GameManager.instance.SetUserProperties(nameInputField.text, SelectedAvatarIndex);
        if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_OnlineFriend))
        {
            MenuManagerTwelve.Instance.ChangeState(MenuManagerTwelve.AllMenus.LobbyScreen);
        }
        else if(Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_RandomPlayer))
        {
            MenuManagerTwelve.Instance.SetLoadingPopUpState(true);
                //Photon Removal NetworkManagerTwelve.Instance.JoinPublicRandomRoom();
            }

        }

}
}
