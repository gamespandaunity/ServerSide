using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace BEKStudio
{
    public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Player Properties
    public string userName { get; private set; }
    public int avatarIndex { get; private set; }

    [SerializeField] List<Sprite> avatarsSprites;
    public GameMode currentGameMode;
    public string typedRoomCode { get; private set; }
    public string joinedRoomCode { get; private set; }
    public bool isCreateRoom { get; private set; }

    public bool isNetWorking;
    public bool masterClient;
    // List of random names
    private List<string> randomNames;

    public int botAvatarIndex;
    private string botUserName="AI";

    public static Action Event_PlayerInfoUpdated;

    public enum GameMode
    {
        Ai,
        AgainstFriend,
        QuickMatch,
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            isNetWorking = true;
            userName = PlayerPrefs.GetString("userName", "Player");
            avatarIndex = PlayerPrefs.GetInt("avatar", 0);
            InitializeRandomNames();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeRandomNames()
    {
        randomNames = new List<string>
        {
            "Alex",
            "Max",
            "Sam",
            "Eli",
            "Leo",
            "Jay",
            "Mia",
            "Ava",
            "Ian",
            "Zoe"
        };
    }

    private string GetRandomName()
    {
        if (randomNames.Count == 0)
        {
            // Reinitialize the list if all names have been used
            InitializeRandomNames();
        }

        // Get a random index
        int randomIndex = UnityEngine.Random.Range(0, randomNames.Count);

        // Get the name at the random index
        string randomName = randomNames[randomIndex];

        // Remove the name from the list
        randomNames.RemoveAt(randomIndex);

        return randomName;
    }

    public bool isOnline()
    {
        if (currentGameMode.Equals(GameMode.Ai)) return false;
        return true;
    }

    public Sprite GetAvatarSprite(int index)
    {
        if (index > avatarsSprites.Count) return null;
        return avatarsSprites[index];
    }

    public void SetPlayerInfo(string userName, int avatarIndex)
    {
        this.userName = userName;
        this.avatarIndex = avatarIndex;

        PlayerPrefs.SetString("userName", userName);
        PlayerPrefs.SetInt("avatar", avatarIndex);

        Event_PlayerInfoUpdated?.Invoke();
    }

    public void setRoomCode(string roomCode)
    {
        if (roomCode.Equals(""))
        {
            isCreateRoom = true;
            return;
        }

        isCreateRoom = false;
        this.typedRoomCode = roomCode;
    }

    public void SetBotInfo()
    {
        //botAvatarIndex = GetRandomSpriteIndex();
        botUserName = "AI";
    }
    public void setJoinRoomCode(string roomCode) => joinedRoomCode = roomCode;
    public int GetRandomSpriteIndex() => UnityEngine.Random.Range(0, avatarsSprites.Count);
    public Sprite GetUserAvatar() => avatarsSprites[avatarIndex];
    public string GetBotName() => botUserName;
    public Sprite GetBotSprite() => avatarsSprites[botAvatarIndex];
    public int GetBotSpriteIndex() => botAvatarIndex;

    public List<Sprite> GetAllAvatars() => avatarsSprites;
}
}
