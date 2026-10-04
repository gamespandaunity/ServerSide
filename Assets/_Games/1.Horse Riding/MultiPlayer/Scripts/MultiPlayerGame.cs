using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiPlayerGame
{
    public const float ASTEROIDS_MIN_SPAWN_TIME = 5.0f;
    public const float ASTEROIDS_MAX_SPAWN_TIME = 10.0f;

    public const float PLAYER_RESPAWN_TIME = 4.0f;

    public const int PLAYER_MAX_LIVES = 3;

    public const string PLAYER_COUNTRY = "PlayerCountry";
    public const string PLAYER_LIVES = "PlayerLives";
    public const string PLAYER_READY = "IsPlayerReady";
    public const string PLAYER_LOADED_LEVEL = "PlayerLoadedLevel";
    internal static object PLAYER_LOADING = "loadingPlayer";
    public const string PLAYER_AWATAR = "PlayerAwatar";

    internal static bool isSinglePlayer;
    internal static bool isLastManStandingMode;
    internal static bool isTimeTrial;
    //
    internal static bool isChampion;
    internal static int levelId;
    //

    public static int NO_AI_CAR = 2;

    public static Color GetColor(int colorChoice)
    {
        switch (colorChoice)
        {
            case 0: return Color.red;
            case 1: return Color.green;
            case 2: return Color.blue;
            case 3: return Color.yellow;
            case 4: return Color.cyan;
            case 5: return Color.grey;
            case 6: return Color.magenta;
            case 7: return Color.white;
        }

        return Color.black;
    }

    public static List<string> AI_NamesList = new List<string>(new string[]
    {
        "Preston", "Heritic_101 ", "Hulk", "snowflake", "Sophia ", "Ninja", "Lisa23", "GraveDigger ", "Killer",
        "Blikimore", "Amelia", "Jacob", "Kitty", "Connor", "SevenShots", "Noah", "ShortCircuit", "Erak606", "Tracy",
        "King", "Cancer", "Margaret", "Patricia", "e4envy", "Lily", "Victoria", "grox19", "Emma", "Venom", "Hotcakes",
        "Harry", "Dropkick", "Callum", "SoulTaker", "Smith", "Tech Bro", "Mr.Lucky", "Carlos", "FoxHound42", "Don",
        "ValkonX11", "Jackie", "K-9", "Kiri", "Vortex59", "Michael45", "Arrow", "Piyush", "Danger32", "Sanjay",
        "Dude59", "Yuan", "Yukio", "scyp10", "fire3232", "Zee", "Usman", "Toxic", "Wasim", "Tahir", "Yasir", "zada2011",
        "Simran", "Muqaddas", "Gail102", "Waqas", "Ibrahim", "Dr.Cocktail", "Akemi", "Aika", "Devil", "Chika", "Elaine",
        "Stealth09", "Oceanstar11", "Brendan170", "Kristi", "Maya", "Muro45", "Mika",
        "Reena", "Saura", "Riddhi", "Beastkiller", "Vienna", "Po1son", "9Lives", "Wendy", "Xiang", "Maria",
        "CrazyMind", "Spawn99", "Nastya", "Dasha", "SkyGod", "Olga", "wakka102", "Natalia", "Alexandra", "FridayFox",
        "Monika", "Crysweet", "Irina", "SoftDevil", "Elena", "Slinger", "Diana", "Natasha", "Ania", "DZE", "Nina",
        "Fl00d", "Sara", "Sandra", "Eva", "X-Dew", "Andrew", "Nick82", "jakub", "Roman",
        "LoneWalker", "Michal", "Error404", "Paul", "livingfree8", "Kuba", "Petr", "Jax4321", "Tom", "Chris", "Stelios",
        "Attila", "Aaroon21", "Kacper", "Frank", "GuTzd", "Samuel", "Darth44", "Jack", "Jerry", "Dylanf3", "Jose",
        "runnerman1", "Carl", "Roger", "Terry", "atomic7732", "Noah", "Vijy", "Liam",
        "Hazel", "Gr8Flick", "Lily", "SinRostro", "Redwild10", "Alexei", "Chan1455", "catlover2",
        "Andrei", "Shadowkiller98", "Arkady", "Арсений", "Артём", "Артур", "Афанасий", "Богдан", "Борис", "Вадим",
        "Валентин", "Валерий", "Вениамин", "Виктор", "Виталий", "Владимир", "Владислав", "Всеволод", "Вячеслав",
        "Абрам", "Альберт", "Андрей", "Aarav", "Vivaan", "Aditya", "Vihaan", "Arjun", "Reyansh", "Raahithya", "Kabir",
        "Arush", "Rudra",
        "Ajay", "Warior135", "Rajesh", "DivinityV2", "Salman", "n0y0u", "Bodhi", "F0R1", "Rohan", "F0R1", "Infinity"
    });

    public static List<AIPlayerData> TempNamesList;

    public static bool IsInternetConnection()
    {
        bool isConnectedToInternet = false;
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ||
            Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            isConnectedToInternet = true;
        }

        return isConnectedToInternet;
    }

    public static void setAIData()
    {

       
        if (isChampion)
        {
            if (MConstants.CurrentCHAMPION_MODE != MConstants.CHAMPION_MODES.DUABI_CHAMPION)
            {
                NO_AI_CAR =4;// Random.Range(3, 5);
            }
        }
        else
        {
            NO_AI_CAR =4;// Random.Range(3, 5);
        }
        
        if (!(TempNamesList == null || (TempNamesList != null && TempNamesList.Count <= 0)))
        {
            return;
        }

        // NO_AI_CAR = Random.Range(3, 5);
       
        
        //
       
        //

        TempNamesList = new List<AIPlayerData>();
        int rvIndex = 0;
        List<string> playersNamesList = new List<string>(AI_NamesList);
        for (int i = 0; i < 6; i++)
        {
            AIPlayerData aIPlayerData = new AIPlayerData();
            if (RivalsCountryController.Instance.countryData != null &&
                i < RivalsCountryController.Instance.countryData.rivalsCountryCodes.Length)
            {
                aIPlayerData.aiCountry = RivalsCountryController.Instance.countryData.rivalsCountryCodes[rvIndex];
                aIPlayerData.isRival = true;
                aIPlayerData.aiName =
                    RivalsCountryController.Instance.countryData.GetRandomBotName(
                        RivalsCountryController.Instance.countryData.rivalsCountryCodes[rvIndex]);
                int awatarID = 0;

                aIPlayerData.awatarID = awatarID;
                //Debug.Log("RV " + aIPlayerData.aiCountry + " Name " + aIPlayerData.aiName);
                rvIndex++;
            }
            else
            {
                aIPlayerData.aiCountry = CountriesFlags.GetRandomCountryName();
                aIPlayerData.aiName = getRandomeName(playersNamesList);
            }

            TempNamesList.Add(aIPlayerData);
        }
    }

    public static string getPlayerName()
    {
        //Debug.Log("GetPlayerName InIt");
        string name = "Player" + Random.Range(1000, 10000);
        name = PlayerDataController.instance.playerStats.playerName;
        return name;
    }


    public static string getRandomeName(List<string> playersNamesList)
    {
        //Debug.Log("getRandomeName InIt");
        string name = playersNamesList[Random.Range(0, playersNamesList.Count)];
        playersNamesList.Remove(name);
        return name;
    }

    public static string getDefaultPlayerName()
    {
        //Debug.Log("getDefaultPlayerName InIt");
        string name = AI_NamesList[Random.Range(0, AI_NamesList.Count)];
        //Debug.Log("Na " + name);
        return name;
    }
}