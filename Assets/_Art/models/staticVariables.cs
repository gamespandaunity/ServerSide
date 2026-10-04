using NetworkManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UIprofileManager;

public class staticVariables
{
    public static string selectedTab;
    public static string webViewShopUrl;
    public static bool shouldupdate = true;
    public static bool isPokerFinished = false;
    public static Shopdatum shopPackage;
    public static GameDetailParent detail;
   // public static BankDetails bankDetail;
    public static string directSupport;
    public static GameHistoryObj gameHistoryObj;
    public static string firebaseToken = "";
    public static bool isCounterFlag = false;
    public static bool isTeenPattiFinished = false;

    public static bool isAppCheckVerified = true;
    public static bool isGuest;
    public static float currentTime = 120;
    public static float currentTimebetExpire = 0;
    public static int currentPrize = 0;
    public static FriendsModel GiftRecieverId;
    public static bool isnotificationcounter = false;
    public static UserModel UserProfiledata = new UserModel();
    public static string currentRoomName = "";
    public static List<GameButton_InfoSetter> All_games_Ref = new List<GameButton_InfoSetter>();

    public static string playingroomsids = "";
    public static PlayerProfile invitedpersonProfile;
    public static PlayerProfile OpponetProfile = new PlayerProfile();
    public static string lastOpponentId;
    public static bool isInviteFriendAndPlay = false;
    public static bool isfromreferllinks = false;
    public static string decodeddeeplink = "";
    public static string referralCode = "";
    public static UserModel searchFriend;
    public static string inviteBetId;
    public static bool invitationAcceptReject = false;
    public static PlayerProfile invitpersonProfiledirect;
    public static AllChallengeCounts allChallengeCounts;
    public static bool isgoldcoins = false;
    public static string screenstatus = "";
    public static string carromSecondPlayer_info, racingSecondPlayer_Info;
    public static string userNickName = "";
    public static bool Isrejoining = false;
    public static int OtherPlayerPhotonID;
    public static string uniqueGameIdentifier = "";
    public static int betEnteredAmount = 0;
    public static bool isPlayingWithAI;
    public enum GameMode_Api
    {
        Creator,
        Joiner
    }
    public static GameMode_Api gameMode_api;
    public static int C_room_id;
    public static Room newRoom_C;

    public static string CurrentRoundID;
    public static bool isWin__aight;
    public static string headerText = "";
    public static string countryDialCode_general = "";

    public static Texture2D ProfilePicture;
    public static Texture2D opponentImage;
    public static int selectedTable = 0;
    public static bool playOnceAi = true;
    public static Texture cueOpponentTex, cueMainTex;
    public static int opponentIdOnAcceptChallenges;
    public static bool isFromCreateRoom_ = false;
    public static string createPlayer_response_id; // its using in [ generate otp api ] and [create player api]
    public static int otp;


    public static List<game_detail> lotteryGamesList = new List<game_detail>();
    public static List<game_detail> casinoGamesList = new List<game_detail>();
    public static List<string> BannerUrls = new List<string>();
    public static List<GameObject> friends = new List<GameObject>();
    public static List<FriendsModel> friendsRequestList_Save = new List<FriendsModel>();
    public static string privacyPolicyLink;

    public static GameFeature gameFeatures = new GameFeature();

    public static string phoneNumberForSupport = "+923226722955";
    public static string emailForSupport = "recipient@example.com";

    public static void ResetGuestStatus()
    {
        PlayerPrefs.SetString("GuestId", "");
    }




}
[System.Serializable]
public class GameFeature
{
    public bool isGoldCoins = true;
    public bool isAgora;
}
