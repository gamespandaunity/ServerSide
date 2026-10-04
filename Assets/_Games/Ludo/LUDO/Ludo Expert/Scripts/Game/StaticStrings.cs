namespace AssemblyCSharp
{
    public static class StaticStrings
    {

        public static string AndroidPackageName = "com.ludo.super";
        public static string ITunesAppID = "11111111111";



        // Notifications
        public static string notificationTitle = "Ludo Bar";
        public static string notificationMessage = "Get your FREE fortune spin!";
        
        public static string baseURL = "https://ludopay.xyz/admin5/";
        // Game configuration
        public static float WaitTimeUntilStartWithBots = 15f; // Time in seconds. If after that time new player doesnt join room game will start with bots

        

        // Services configration IDS
        public static string PlayFabTitleID = "D0F6F";//"BA0DE";
        public static string appVersionNumber = "V0001";
        public static string PhotonAppID = "d312cf41-b18a-44b9-add5-5a3d586d0e69";//"96ba8070-0958-48ab-bc98-a90d0a52cd5d";
        public static string PhotonChatID = "9c3ea24e-501a-4000-82ac-0293c93f095f";//"eeb8fa2c-148e-4cb0-b0f4-a87b53405360";



        //public static string PlayFabTitleID = "BC75D";//"BA0DE";
        //public static string appVersionNumber = "V0001";
        //public static string PhotonAppID = "9abed2c7-5ba0-4abe-86ac-7d4f63d556aa";
        //public static string PhotonChatID = "6e0795e3-584d-44b0-acd0-95d0a782496a";
        // Admob Ads IDS
        //public static string adMobAndroidID = "ca-app-pub-5318584791748003/4062359747";
        //public static string adMobiOSID = "ca-app-pub-4150875028712336/253535234";

        // Facebook share variables
        public static string facebookShareLinkTitle = "I'm playing Ludo Super!. Available on Android and iOS.";

        // Share private room code
        public static string SharePrivateLinkMessage = "Join me in Ludo Bar. My Private Room Code is:";
        public static string SharePrivateLinkMessage2 = "Download Ludo Bar:";
        public static string ShareApkLink = "http://new.ludoearn.xyz/";
        public static string ShareScreenShotText = "I finished game in Ludo Bar. It's my score :-) Join me and download Ludo Bar:";


        // Initial coins count for new players
        // When logged as Guest
        public static int initCoinsCountGuest = 0;
        //When logged via Facebook
        public static int initCoinsCountFacebook = 5;
        //When logged as Guest and then link to Facebook
        public static int CoinsForLinkToFacebook = 5;

        // Unity Ads - reward coins count for watching video
        public static int rewardForVideoAd = 5;

        // Facebook Invite variables
        public static string facebookInviteMessage = "Come play this great game!";
        public static int rewardCoinsForFriendInvite = 500;
        public static int rewardCoinsForShareViaFacebook = 5;

        // String to add coins for testing - To add coins start game, click "Edit" button on your avatar and put that string
        // It will add 1 000 000 coins so you can test tables, buy items etc.
        public static string addCoinsHackString = "Cheat:AddCoins";



        // Hide Coins tab in shop (In-App Purchases)
        public static bool hideCoinsTabInShop = false;
        public static string runOutOfTime = "ran out of time";
        public static string waitingForOpponent = "Waiting for your opponent";

        // Other strings
        public static string youAreBreaking = "You start, good luck";
        public static string opponentIsBreaking = "is starting";
        public static string IWantPlayAgain = "I want to play again!";
        public static string cantPlayRightNow = "Can't play right now";

        // Players names for training mode
        public static string offlineModePlayer1Name = "Player 1";
        public static string offlineModePlayer2Name = "Player 2";

        // Photon configuration
        // Timeout in second when player will be disconnected when game in background
        public static float photonDisconnectTimeout = 25.0f; // In game scene - its better to don't change it. Player that loose focus on app will be immediately disconnected
        public static float photonDisconnectTimeoutLong = 300.0f; // In menu scene etc. 

        // Bids Values
        public static int[] bidValues = new int[] { 500, 2000, 10000, 50000, 250000, 1000000, 2000000, 5000000 };
        public static string[] bidValuesStrings = new string[] { "500", "2000", "10k", "50k", "250k", "1M", "2M", "5M" };

        public static bool isFourPlayerModeEnabled = true;

        // Settings PlayerPrefs keys
        public static string SoundsKey = "EnableSounds";
        public static string MusicKey = "EnableSounds";
        public static string VibrationsKey = "EnableVibrations";
        public static string NotificationsKey = "EnableNotifications";
        public static string FriendsRequestesKey = "EnableFriendsRequestes";
        public static string PrivateRoomKey = "EnablePrivateRoomRequestes";
        public static string PrefsPlayerRemovedAds = "UserRemovedAds";


        // Standard chat messages
        public static string[] chatMessages = new string[] {
            "Please don't kill",
            "Play Fast",
            "I will eat you",
            "You are good",
            "Well played",
            "Today is your day",
            "Hehehe",
            "Unlucky",
            "Thanks",
            "Yeah",
            "Remove Blockade",
            "Good Game",
            "Oops",
            "Today is my day",
            "All the best",
            "Hi",
            "Hello",
            "Nice move"
        };

        // Additional chat messages
        // Prices for chat packs
        public static int[] chatPrices = new int[] { 10, 10, 10, 10, 10, 10 };
        public static int[] emojisPrices = new int[] { 20, 20, 20, 20, 20 };

        // Chat packs names
        public static string[] chatNames = new string[] { "Motivate", "Emoticons", "Cheers", "Gags", "Laughing", "Talking" };

        // Chat packs strings
        public static string[][] chatMessagesExtended = new string[][] {
            new string[] {
                "Never give up",
                "You can do it",
                "I know you have it in you!",
                "You play like a pro!",
                "You can win now!",
                "You're great!"
            },
            new string[] {
                ":)",
                ":(",
                ":o",
                ";D",
                ":P",
                ":|"
            },
            new string[] {
                "Keep it going",
                "Go opponents!",
                "Fabulastic",
                "You're awesome",
                "Best shot ever",
                "That was amazing",
            },
            new string[] {
                "OMG",
                "LOL",
                "ROFL",
                "O'RLY?!",
                "CYA",
                "YOLO"
            },
            new string[] {
                "Hahaha!!!",
                "Ho ho ho!!!",
                "Mwhahahaa",
                "Jejeje",
                "Booooo!",
                "Muuuuuuuhhh!"
            },
            new string[] {
                "Yes",
                "No",
                "I don't know",
                "Maybe",
                "Definitely",
                "Of course"
            }
        };

    }
}

