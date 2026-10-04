using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TeenPattiGame;


public static class LocalSettings
{
    // Add new Things
    public static string Room_Password;
    // Old Things
    private static int MinPlayers = 2;
    private const int MaxPlayers = 2;
    public static Sprite ServerSideImge;
    public static string ProfilePicName;
    public const string ProfilePicNameKey = "Profile_Pic_Name_Key";
    public static BigInteger winOrLoseAmount = 0;
    public static float GamePlayTimeCount = 0;
    public const int TeenPattiLevelMultiplier = 5120;

    public const int PotLimitMultiplier = 2048;
    public const int ChaalLimitMultiplier = 256;
    public const int GameResultWaitingTime = 1;
    public const int ShowingResultAndResetDelayTime = 5;

    public const int PlayerTurnDuration = 15;

    public const int GameStartAfterReset = 5;

    public const int GameStartWaitTime = 6;



    public const int RemainingTikTimer = 5;

    public static BigInteger MinBetAmount = 5;


    public static void firstTimeEntry(int number)
    {
        PlayerPrefs.SetInt("FirstTime", 1);
    }


    public static bool IsMenuScene()
    {
        return SceneManager.GetActiveScene().name == "MainMenu";
    }


    private static string CheckmenuBgIndex(int index)
    {
        return "MainMenuBG" + index + "BG";

    }





    // Poker end
    public const float SideShowCountDownTime = 10;

    public const string Score = "score";
    //public const string Rs = "Rs ";
    public const string ProfilePic = "profile_pic";
    public const string ProfileFrame = "profile_Frame";
    public const string menuProfile = "MenuProfile";
    public const string menuProfileFrame = "MenuProfileFrame";
    public const string roomState = "ROOMSTATE";
    public const string playerState = "PLAYERSTATE";

    public const string playerSeen = "player_seen";
    public const string networkPosition = "network_position";
    public const string TableCashKey = "table_cash";
    public const string playingListToArray = "network_playing_list";
    public const string OrgCardsArray = "PlayerOrgCardsArray";
    public const string ResetAbleRoom = "ResetableRoomKey";


    public static int extraRoomCounter = 0;

    public const string isCashOnNetworkUpdated = "Network_Cash_Update";

    public const string TokenIDKey = "token_ID_key";
    public const string PlayernameKey = "player_name_key";
    public const string PlayerStatus = "player_status";

    public const string MyTotalCashKey = "total_cash_key";
    public const string WinHandsKey = "win_hands_key";
    public const string TotalHandsKey = "total_hands_key";
    public const string totalcashWinLossKey = "total_cash_win_key";

    public const string textStringOfPotLimitReached = "----- POT LIMIT REACHED -----";
    public const string textStringOnShowCard = "----- SHOW CARD -----";
    public const string textStringOnStartBetAmount = "----- COLLECTING BOOT -----";




    // Use in main menu
    public const string TotalChips = "TotalChips";


    public const string RoomIDPref = "player_room_id";
    public const string GameNamepref = "player_game_name";
    public const string TableNamePref = "player_table_name";


    const string RewardDate = "reward_date";
    const string RewardCollectedDate = "reward_Collected_date";
    const string RewardCollectDay = "reward_day";

    const string Vibration = "Vibration";




    public static bool mobilVibration
    {
        get
        {
            if (PlayerPrefs.GetInt(Vibration) == 0)
                return true;
            return false;
        }

        set
        {
            if (value == true)
                PlayerPrefs.SetInt(Vibration, 0);
            else
                PlayerPrefs.SetInt(Vibration, 1);
            PlayerPrefs.Save();
        }
    }



    public static void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        // if (mobilVibration)
        //Handheld.Vibrate();
#endif
    }
    public static int GetMaxPlayers()
    {
        return MaxPlayers;
    }
    public static int GetMinPlayers()
    {
        return MinPlayers;
    }

    public static void SetMinPlayers(int val)
    {
        MinPlayers = val;
    }


    public static void SetprofilePic(int value)
    {
        PlayerPrefs.SetInt(menuProfile, value);
    }

    public static void SetprofileFrame(int value)
    {
        PlayerPrefs.SetInt(menuProfileFrame, value);
    }

    // public static Room GetCurrentRoom
    // {
    //     get
    //     {
    //         return PhotonNetwork.CurrentRoom;        //photon removal
    //     }
    // }

    public static bool IsProfilePicSet()
    {
        return PlayerPrefs.HasKey(menuProfile);
    }
    public static bool IsProfileFrameSet()
    {
        return PlayerPrefs.HasKey(menuProfileFrame);
    }

    public static int GetprofilePic()
    {
        return PlayerPrefs.GetInt(menuProfile);
    }
    public static int GetprofileFrame()
    {
        return PlayerPrefs.GetInt(menuProfileFrame);
    }


    public const string Game_Counter_Key = "Game_Counter_Key";


    public static BigInteger AI_Amount;

    public static BigInteger GetTotalChips()
    {
        if (PlayerPrefs.HasKey(TotalChips))
        {
            string chipsString = PlayerPrefs.GetString(TotalChips);
            BigInteger chipsBigint = 0;
            if (BigInteger.TryParse(chipsString, out BigInteger result))
            {
                chipsBigint = result;
            }
            return chipsBigint;
        }
        else
        {
            SetTotalChips(0);
            return 0;
        }
    }


    public static void SetTotalChips(BigInteger ChipsToAdd)
    {
        //Debug.Log("Updating Coins SomeWhere :"+ ChipsToAdd.ToString());
        //Debug.Log("Updating Coins SomeWhere error :"+ PlayerPrefs.GetString(TotalChips));
        PlayerPrefs.GetString(TotalChips).Show("Total Chips values");
        if (PlayerPrefs.GetString(TotalChips) == "")
        {
            PlayerPrefs.SetString(TotalChips, "0");
        }
        BigInteger chipsBigint = ChipsToAdd + BigInteger.Parse(PlayerPrefs.GetString(TotalChips));
        PlayerPrefs.SetString(TotalChips, chipsBigint.ToString());// chipsBigint.ToString());
        PlayerPrefs.Save();
    }





    public static string Rs(BigInteger amount)
    {
        return ConvertToPakistaniNumberingSystem(amount);
    }
    public static string Rs(string amount)
    {
        if (amount == "")
            return "0";
        //return FormatNumberString(amount);
        BigInteger chips = BigInteger.Parse(amount.Trim());
        return ConvertToPakistaniNumberingSystem(chips);
    }

    private static string[] suffixes = { "", "L", "Cr", "Ar", "Kh", "Ne", "Pad", "Sh", "Msh" };
    public static string ConvertToPakistaniNumberingSystem(BigInteger number)
    {

        string formattedScore;

        if (number >= 1000000)
        {
            // Display in millions format (e.g., 1,000,000)
            formattedScore = number.ToString("N0");
        }
        else if (number >= 1000)
        {
            // Display in thousands format (e.g., 1,000)
            formattedScore = number.ToString("N0");
        }
        else
        {
            // Display as is
            formattedScore = number.ToString("N0");
        }

        //Debug.LogError("Check Foramt" + formattedScore);
        return number.ToString();
        //scoreText.text = formattedScore;



    }


    public static string FormatNumber(string numberString)
    {
        string formatOpt = "N2";
        // Convert the input string to a long integer
        BigInteger number = BigInteger.Parse(numberString);

        // Determine the appropriate suffix based on the number of digits
        string suffix;
        if (number >= 10000000)
        {
            suffix = "Cr";
            number /= 10000000;
        }
        else if (number >= 100000)
        {
            suffix = "L";
            number /= 100000;
        }
        else
        {
            suffix = "";
            formatOpt = "";
        }

        // Format the number as a string with commas

        string formattedNumber = number.ToString();

        // Add the suffix to the end of the string
        if (!string.IsNullOrEmpty(suffix))
        {
            formattedNumber += suffix;
        }

        // Return the formatted string
        return formattedNumber;
    }


    public static void ToggleObjectState(GameObject[] obj, bool isTrue)
    {
        foreach (GameObject go in obj)
        {
            if (go)
                go.SetActive(isTrue);
        }
    }

    public static void SetPosAndRect(GameObject InstantiatedObj, RectTransform ALReadyObjPos, Transform Parentobj)
    {
        InstantiatedObj.transform.parent = Parentobj;
        RectTransform MyPlayerRectTransform = InstantiatedObj.GetComponent<RectTransform>();
        MyPlayerRectTransform.localScale = ALReadyObjPos.localScale;
        MyPlayerRectTransform.localPosition = ALReadyObjPos.localPosition;
        MyPlayerRectTransform.anchorMin = ALReadyObjPos.anchorMin;
        MyPlayerRectTransform.anchorMax = ALReadyObjPos.anchorMax;
        MyPlayerRectTransform.anchoredPosition = ALReadyObjPos.anchoredPosition;
        MyPlayerRectTransform.sizeDelta = ALReadyObjPos.sizeDelta;
        MyPlayerRectTransform.localRotation = ALReadyObjPos.localRotation;

    }


    public static void CheckAvatarName(Image image, AvatarCollections avatarList, int avatarIndex, TMP_Text nameText)
    {

        image.sprite = avatarList.Sprites[avatarIndex].Sprites;
        nameText.text = avatarList.Sprites[avatarIndex].Name;


        // image.transform.GetComponent<RectTransform>().localScale = new UnityEngine.Vector3(adScale, adScale, adScale);
    }


    public static void SetPlayername(string Pname)
    {
        PlayerPrefs.SetString(PlayernameKey, Pname);
    }

    public static string GetPlayerName()
    {
        return PlayerPrefs.GetString(PlayernameKey);
    }



    public const string player_ID_Key = "playerID_Key";
    public const string player_Incremented_ID_Key = "player_Incremented_ID_Key";


    //roomID, gameName, tableName,

    public static string GetSetRoomID
    {
        set { PlayerPrefs.SetString(RoomIDPref, value); }
        get { return PlayerPrefs.GetString(RoomIDPref); }
    }


    public static string GetSetGameName
    {
        set { PlayerPrefs.SetString(GameNamepref, value); }
        get { return PlayerPrefs.GetString(GameNamepref); }
    }
    public static string GetSetTableName
    {
        set { PlayerPrefs.SetString(TableNamePref, value); }
        get { return PlayerPrefs.GetString(TableNamePref); }
    }




    #region Game Setting 

    #region Sound Status

    public const string SoundKey = "SOUND_TEENPATTI";
    public static void SetSoundEffect(bool isTrue)
    {
        PlayerPrefs.SetInt(SoundKey, isTrue ? 0 : 1);
    }

    public static bool GetSoundEffect()
    {
        return PlayerPrefs.GetInt(SoundKey) == 0;
    }

    #endregion

    public static void Show_Dialogue(GameObject msgBox, string dialogue)
    {

        msgBox.transform.GetChild(0).GetComponent<TMP_Text>().text = dialogue;
        msgBox.transform.parent.gameObject.SetActive(true);
    }

    #endregion



    public static void Add_ChipsFrom()
    {
        LocalSettings.SetTotalChips(LocalSettings.GetTotalChips() + 10000000);
        if (!MatchHandler.isOffline())
            //if (PhotonNetwork.IsConnectedAndReady)

            GameObject.Find("Canvas").GetComponent<UIManager>().myPlayerInfo.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
    }



    #region AI_Offline Settings


    public const string AI_Name = "AI";
    public static bool AI_StandUP = false;

    #endregion

}