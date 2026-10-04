using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Snake_Ladder;
namespace Twelve
{
    public class TopBarUIEventListnerTwelve : MonoBehaviour
{
    public TextMeshProUGUI UserName;
    public Image Avatar;

    void Start()
    {
        UserName.text = PlayerPrefs.GetString("Username");
        Avatar.sprite = ReferenceManager.Instance.avatarSprites[PlayerPrefs.GetInt("AvatarId")];
        
    }


    void OnEnable()
    {
            Snake_Ladder.GameManager.instance.PlayerPropertiesUpdated += OnPlayerPropertiesUpdated;   
    }
    void OnDisable()
    {
            Snake_Ladder.GameManager.instance.PlayerPropertiesUpdated -= OnPlayerPropertiesUpdated;
    }

    void OnPlayerPropertiesUpdated(string Username, int avatarIndex)
    {
        this.UserName.text = Username;
        Avatar.sprite = ReferenceManager.Instance.avatarSprites[avatarIndex];
    }
}
}
