using System;
using UnityEngine;
namespace Snake_Ladder
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Loading Settings")]
        public float loadingDuration;

        [Header("Player Info")]
        public string UserName;
        public int AvatarId;
        public bool playMatchingAnim;

        [Header("Opponent Info")]
        public string OpponentName;
        public int OpponentAvatarId;

        [Header("Game Mode")]
        public GameMode currentGameMode;

        // Events
        public event Action<string, int> PlayerPropertiesUpdated;

        public enum GameMode
        {
            Against_Ai,
            Against_LocalPlayer,
            Against_OnlineFriend,
            Against_RandomPlayer
        }

        #region Unity Callbacks

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
            //    Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadPlayerInfo();
        }

        #endregion

        #region Private Methods

        private void LoadPlayerInfo()
        {
            UserName = PlayerPrefs.GetString("Username", "Guest");
            AvatarId = PlayerPrefs.GetInt("AvatarId", 0);
            SetUserProperties(UserName, AvatarId);
        }

        #endregion

        #region Public Methods

        public void SetUserProperties(string username, int index)
        {
            UserName = username;
            AvatarId = index;

            PlayerPropertiesUpdated?.Invoke(UserName, AvatarId);

            PlayerPrefs.SetString("Username", username);
            PlayerPrefs.SetInt("AvatarId", index);
        }

        #endregion
    }
}
