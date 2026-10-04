using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BEKStudio
{
    public class PlayerInfoEventListner : MonoBehaviour
{
    [SerializeField] Image selectedAvatarImage;
    [SerializeField] TMP_InputField userName;
    [SerializeField] List<Image> avatars;

    private int selectedAvatarIndex;
    private void Start()
    {
        SetAvatars();
        SetPlayerInfo();
    }

    void SetAvatars()
    {
        int count = 0;
        foreach (var avatar in avatars) 
        {
            avatar.sprite = GameManager.Instance.GetAvatarSprite(count++);
        }
    }

    void SetPlayerInfo()
    {
        userName.text = GameManager.Instance.userName;
        selectedAvatarImage.sprite = GameManager.Instance.GetAvatarSprite(GameManager.Instance.avatarIndex);
    }

    public void onClickConfirm()
    {
        //AudioManager.Instance.Play(AudioManager.AudioType.click);
        GameManager.Instance.SetPlayerInfo(userName.text, selectedAvatarIndex);
        gameObject.SetActive(false);
    }

    public void OnClickAvatar(int index)
    {
        //AudioManager.Instance.Play(AudioManager.AudioType.click);
        selectedAvatarIndex = index;
        selectedAvatarImage.sprite = GameManager.Instance.GetAvatarSprite(index);
    }
}
}
