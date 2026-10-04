using NaughtyAttributes;
using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingScript : MonoBehaviour
{
    public static TestingScript instance;
    public bool isGoldCoin;
    public UserModel Profile;
    public PlayerProfile opponentData;
    public UserBalanceResponse LastFetchedUserCoins;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called before the first frame update
    [ContextMenu("Get Profile Data")]
    public void GetProfileData()
    {
        Profile = staticVariables.UserProfiledata;
        isGoldCoin = staticVariables.isgoldcoins;
        opponentData = staticVariables.OpponetProfile;
        LastFetchedUserCoins = ApiAndRoomManager.LastFetchedCoins;// :-) kesa :)heavy ye sceipt ubisoft walon ko bej dy, nai sir khud pesa kamaun ga is sy haha
    }
}
