using UnityEngine;
using System.Collections;

public class MConstants : MonoBehaviour {
	public static int CurrentLevelNumber=1;
    internal static bool isTimeStart;
    internal static bool isTimeOver;
    internal static bool isRaceOver;
    internal static bool isRaceOverEnabled;

    public static bool isPlayerWin;
    public static float TIME_SCALE = 0.95f;

    public static GAME_MODES CurrentGameMode = GAME_MODES.LEVEL;
    public static bool isToShowAd;
    public static string RATE_US = "";
    internal static bool isShowName = true;
    public static bool ISNATIVE_AD_LOADED;
    //
    public static int MAX_LEVELS = 15;
    public static CHAMPION_MODES CurrentCHAMPION_MODE = CHAMPION_MODES.DUABI_CHAMPION;
    public enum CHAMPION_MODES
    {
        DUABI_CHAMPION,
        BRITISH_CHAMPION,
        KENTUCKY_CHAMPION,
        PEGASUS_CHAMPION
    }
    public enum MULTIPLAYER_TYPE
    {
        playRandom,
        playWithFriend
    }
    public static MULTIPLAYER_TYPE multiPlayerType = MULTIPLAYER_TYPE.playRandom;
    //

    public enum GAME_MODES
    {
        FREE,
        LEVEL,
        MULTI_PLAYER
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Menu/ClearData")]
    static void CreateFolder()
    {
        //Debug.Log("delete File");
#if UNITY_EDITOR
        UnityEditor.FileUtil.DeleteFileOrDirectory(Application.persistentDataPath);
#endif
    }
#endif
}

