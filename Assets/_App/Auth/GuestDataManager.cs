using UnityEngine;

public class GuestDataManager : MonoBehaviour
{

    [HideInInspector] public static string LocalCoinsKey = "GuestCoins";
    [HideInInspector] public static string GuestUsernameKey = "GuestName";
    public Sprite guestProfilePhoto;
    public static GuestDataManager _instance;


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void UpdateGuestCoins(int Coins)
    {
        PlayerPrefs.SetInt(LocalCoinsKey, PlayerPrefs.GetInt(LocalCoinsKey) + Coins);
        ModifyCoins();
    }
    public void UpdateGuestName(string name)
    {
        PlayerPrefs.SetString(GuestUsernameKey, name);
        RefreshProfileInfo();
    }

    public void ModifyCoins()
    {
        staticVariables.UserProfiledata.user.gold_balance = 0;
        staticVariables.UserProfiledata.user.silver_balance = PlayerPrefs.GetInt(LocalCoinsKey);
        ApiAndRoomManager.LastFetchedCoins.data.silver_balance= PlayerPrefs.GetInt(LocalCoinsKey).ToString();
        ApiAndRoomManager.LastFetchedCoins.data.gold_balance=0.ToString();
    }
    public void RefreshProfileInfo()
    {
        staticVariables.UserProfiledata.user.first_name = PlayerPrefs.GetString(GuestUsernameKey);
        staticVariables.ProfilePicture = guestProfilePhoto.texture;
    }
    public static int FetchGuestCoins()
    {
        int coins = PlayerPrefs.GetInt(LocalCoinsKey);
        return coins;
    }
}
