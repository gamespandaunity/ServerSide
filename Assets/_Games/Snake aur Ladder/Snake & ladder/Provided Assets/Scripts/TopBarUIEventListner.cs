using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Snake_Ladder
{

    public class TopBarUIEventListner : MonoBehaviour
    {
        public TextMeshProUGUI UserName;
        public Image Avatar;

        void Start()
        {
            UserName.text = PlayerPrefs.GetString("Username");
          //Wasi    Avatar.sprite = FusionGameReferenceManager.Instance.Avatars[PlayerPrefs.GetInt("AvatarId")];

        }


        void OnEnable()
        {
            SnakeGameManager.instance.PlayerPropertiesUpdated += OnPlayerPropertiesUpdated;
        }
        void OnDisable()
        {
            SnakeGameManager.instance.PlayerPropertiesUpdated -= OnPlayerPropertiesUpdated;
        }

        void OnPlayerPropertiesUpdated(string Username, int avatarIndex)
        {
            this.UserName.text = Username;
         //Wasi     Avatar.sprite = FusionGameReferenceManager.Instance.Avatars[avatarIndex];
        }
    }
}