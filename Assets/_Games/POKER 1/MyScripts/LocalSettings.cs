
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace POKER
{
    public static class LocalSettings
    {
        // For AI_Things
        public const string AI_Name = "AI";
        public static BigInteger AI_Amount;
        public static bool AI_StandUP = false;
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
        public const int PokerMultiplayerMax = 5000;
        public const int PokerMultiplayermin = 50;
        public const int PokerMultiplayerBet = 50;
        public const int DragonTigerMultiplier = 1000;
        public const int PotLimitMultiplier = 1024;
        public const int ChaalLimitMultiplier = 128;
        public const int GameResultWaitingTime = 1;
        public const int ShowingResultAndResetDelayTime = 5;
        public const int ShowingResultAndResetDelayTimeOfAB = 5;
        public const int PlayerTurnDuration = 15;
        public const int PlayerTurnDurationAndarBahar = 15;
        public const int PlayerTurnDurationLuckyWar = 15;
        public const int PlayerTurnDurationWingowLottery = 10;
        public const int PlayerTurnDurationDragonTiger = 10;
        public const int PlayerTurnDurationPoker = 12;
        public const int GameStartAfterReset = 5;
        public const int PokerGameStartAfterReset = 5;
        public const int WingoGameStartAfterReset = 4;
        public const int LuckyWarGameStartAfterReset = 2;
        public const int GameStartWaitTime = 10;
        public const int GameStartWaitTimeAndarBahar = 5;
        public const int GameStartWaitTimeWingoLottery = 4;
        public const int GameStartWaitTimeDragonTiger = 4;
        public const int GameStartWaitTimePoker = 6;
        public const int GameStartWaitTimeLukyWar = 6;
        public const int NumberofDecksOfLW = 6;
        public const int NumberofDecksOfDT = 8;
        public const int levelUpRewardAmount = 10000;
        public const int RemainingTikTimer = 5;

        public static BigInteger MinBetAmount = 1000;
        public static BigInteger MinTieBetAmount = 64;
        /// <summary>
        /// Poker
        /// </summary>
        public static BigInteger Poker_Min_Entry_Fee = 0;
        public static BigInteger Poker_Max_Entry_Fee = 2500000;

        public const int PokerMinTakinCashMultiplier = 10;
        public const int PokerMaxTakinCashMultiplier = 20;
        public const int PokerBlindMultiplier = 5000;
        public const string Poker_card_listKey = "poker_card_list";
        public const string pockerNumberofdropCummunityCardsKey = "Pocker_Droped_Cumminity_card";
        public const string pokerHoleCard1ForPlayer = "Pokcer_Player_Card_1";
        public const string pokerHoleCard2ForPlayer = "Pokcer_Player_Card_2";
        public const string pokerCommunityCardsArray = "Pokcer_Community_Cards_Array";
        public static string pokerEntryFeeString = "";

        public const string PokerMatchHistory = "poker_match_history";



        public const string DIndexKey = "D_INDEX_KEY";
        public const string DT_card_listKey = "dragon_tiger_card_list";


        const string paswordChecked = "password_checked";
        const string PlayerTotalXp = "player_total_xp";
        public const string PlayerTotalLevelKey = "player_total_Level_Key";

        public static bool IsMenuScene()
        {
            return SceneManager.GetActiveScene().name == "MainMenu";
        }


        private static string CheckmenuBgIndex(int index)
        {
            return "MainMenuBG" + index + "BG";

        }

        public static int GetMenuBgSaveIndex(int index)
        {
            return PlayerPrefs.GetInt(CheckmenuBgIndex(index), 0);
        }

        public static void SetMenuBgSaveIndex(int index, int value)
        {
            PlayerPrefs.SetInt(CheckmenuBgIndex(index), value);
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

        public const string WingoTunDurationSave = "turnDuration";
        public const string WingoluckyNumberSave = "WingoluckyNumber";
        public const string WingoCurrentRemainingTime = "WingoUVRectVal";
        public const string DragonTigerTurnDurationSave = "DT_turnDuration";

        public static int extraRoomCounter = 0;

        public const string isCashOnNetworkUpdated = "Network_Cash_Update";

        public const string AuthTypeKey = "auth_type_key";
        public const string TokenIDKey = "token_ID_key";
        public const string PlayernameKey = "player_name_key";
        public const string PlayerStatus = "player_status";
        public const string VIPStatus = "vip_status";

        public const string ABTunDurationSave = "ABturnDuration";
        public const string LWTunDurationSave = "LWturnDuration";
        public const string AbturnKey = "ABTurnKey";
        public const string LWturnKey = "LWTurnKey";
        public const string LWTieWinKey = "LWTieWinKey";
        public const string LwFirstCardKey = "Lw_first_card";
        public const string LwPlayerCardKey = "Lw_Player_card";
        public const string abPlayCardKey = "ab_first_card";
        public const string ab_card_listKey = "ab_card_list";
        public const string abAndarAmountKey = "ab_andar_amount_key";
        public const string abBaharAmountKey = "ab_bahar_amount_key";
        public const string abSuperAndarAmountKey = "ab_Super_andar_amount_key";
        public const string LWTieAmountKey = "Lw_Tie_amount_key";
        public const string LWBetAmountKey = "Lw_Bet_amount_key";
        public const string abSuperBaharAmountKey = "ab_Super_bahar_amount_key";
        public const string abAndarCardkey = "abAndarCardkey";
        public const string abBaharCardkey = "abBaharCardkey";
        public const string IsNotCardDistribute = "IsNotCardDistribute";
        public const string ABTotalAndarWinKey = "ab_total_andar_win";
        public const string ABTotalBaharWinKey = "ab_total_bahar_win";
        public const string ABTotalSuperBaharWinKey = "ab_total_super_bahar_win";

        public const string WingoLastRecordsKey = "wingo_last_record";
        public const string WingoHistoryAllPointsKey = "wingo_history_all_points_record";
        public const string ABCardLastRecordsKey = "ab_card_last_record";
        public const string ABWinIndexRecordsKey = "ab_win_card_record";

        public const string DTCard0LastRecordsKey = "dt_card0_last_record";
        public const string DTCard1LastRecordsKey = "dt_card1_last_record";

        public const string DTTotalDragonWinKey = "dt_total_dragon_win";
        public const string DTTotalTigerWinKey = "dt_total_tiger_win";
        public const string DTTotalTieWinKey = "dt_total_tie_win";


        public const string MyTotalCashKey = "total_cash_key";
        public const string WinHandsKey = "win_hands_key";
        public const string TotalHandsKey = "total_hands_key";
        public const string totalcashWinLossKey = "total_cash_win_key";

        public const string textStringOfPotLimitReached = "----- POT LIMIT REACHED -----";
        public const string textStringOnShowCard = "----- SHOW CARD -----";
        public const string textStringOnStartBetAmount = "----- COLLECTING BOOT -----";


        public const string PlayerPokerTableCashKey = "player_poker_table_cash_key";



        public const string PokerTotalBuyInChips = "poker_buy_in_chips";

        // Use in main menu
        public const string TotalChips = "PTotalChips";
        public const string PlayerID = "player_ID";
        public const string PlayerIncrementedID = "player_incremented_ID";


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
            if (mobilVibration)
                Handheld.Vibrate();
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

        //public static Room GetCurrentRoom
        //{
        //    get
        //    {
        //        return PhotonNetwork.CurrentRoom;
        //    }
        //}

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

        public static BigInteger GetTotalChips()
        {
            Debug.Log("Total Chips Getting :" + PlayerPrefs.GetString(TotalChips));
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


        public static void SetVIPStatus(string vipStatus)
        {
            PlayerPrefs.SetString(VIPStatus, vipStatus);
        }

        public static string GetVIPStatus()
        {
            return PlayerPrefs.GetString(VIPStatus);
        }

        public static void SetTotalChips(BigInteger ChipsToAdd)
        {
            if (ChipsToAdd != 0)
            {
                // networkkkkkkkkkkkkkkkkkkk
                // Should uncomment to get network cash
                //SetNetworkCashBool(false);

                BigInteger chipsBigint = ChipsToAdd + GetTotalChips();
                PlayerPrefs.SetString(TotalChips, chipsBigint.ToString());
                PlayerPrefs.Save();
            }
        }

        public static void SetTotalServerChips(string ServerChips)
        {
            PlayerPrefs.SetString(TotalChips, ServerChips);
            PlayerPrefs.Save();
        }

        private static bool CardDistributed = false;

        public static bool isCardDistributed
        {
            get
            {
                return CardDistributed;
            }
            set
            {
                CardDistributed = value;
            }
        }

        public static void SetChipsToServer(BigInteger ChipsToAdd)
        {
            // networkkkkkkkkkkkkkkkkkkk
            // Should uncomment to get network cash
            //SetNetworkCashBool(false);
            PlayerPrefs.SetString(TotalChips, ChipsToAdd.ToString());
        }


        public static void SetNetworkCashBool(bool val)
        {
            PlayerPrefs.SetInt(isCashOnNetworkUpdated, val ? 1 : 0);
        }

        public static bool GetNetworkCashBool()
        {
            return PlayerPrefs.GetInt(isCashOnNetworkUpdated) == 1;
        }

        public static BigInteger GetChipsFromServer()
        {
            // Later to modify
            string chipsString = PlayerPrefs.GetString(TotalChips);
            return BigInteger.Parse(chipsString);
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



        public static string TimeCount(float timeValue)
        {
            int totalTime = Mathf.RoundToInt(timeValue);
            int hours = totalTime / 3600;
            int minutes = totalTime % 3600 / 60;
            int seconds = totalTime % 60;

            string timeString = hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            //Debug.Log("Time in HH:MM:SS format: " + timeString);
            return timeString;
        }


        public static BigInteger StringToBigInteger(string number)
        {
            string chipsString = number;
            BigInteger chipsBigint = 0;
            if (BigInteger.TryParse(chipsString, out BigInteger result))
            {
                chipsBigint = result;
            }
            return chipsBigint;
        }

        public static string BigIntegerToString(BigInteger number)
        {
            return number.ToString();
        }


        public static void CheckAvatarName(Image image, AvatarCollections avatarList, int avatarIndex, TMP_Text nameText)
        {

            image.sprite = avatarList.Sprites[avatarIndex].Sprites;
            nameText.text = avatarList.Sprites[avatarIndex].Name;


            // image.transform.GetComponent<RectTransform>().localScale = new UnityEngine.Vector3(adScale, adScale, adScale);
        }


        public static void SetAuthType(int authType)
        {
            PlayerPrefs.SetInt(AuthTypeKey, authType);
            PlayerPrefs.Save();
        }

        public static int GetAuthType()
        {
            return PlayerPrefs.GetInt(AuthTypeKey);
        }

        public static void SetTokenID(string tokenID)
        {
            PlayerPrefs.SetString(TokenIDKey, tokenID);
        }

        public static string GetTokenID()
        {
            return PlayerPrefs.GetString(TokenIDKey);
        }

        public static void SetPlayername(string Pname)
        {
            PlayerPrefs.SetString(PlayernameKey, Pname);
        }

        public static string GetPlayerName()
        {
            return PlayerPrefs.GetString(PlayernameKey);
        }
        public static void SetPlayerStatus(string pStatus)
        {
            PlayerPrefs.SetString(PlayerStatus, pStatus);
        }

        public static string GetPlayerStatus()
        {
            return PlayerPrefs.GetString(PlayerStatus);
        }

        public static int GetPlayerID()
        {
            return PlayerPrefs.GetInt(PlayerID);
        }
        public static void SetPlayerID(string playerid)
        {
            int idplayer = int.Parse(playerid);
            PlayerPrefs.SetInt(PlayerID, idplayer);

        }
        public const string player_ID_Key = "playerID_Key";
        public const string player_Incremented_ID_Key = "player_Incremented_ID_Key";
        public static int GetIncrementedPlayerID()
        {
            return PlayerPrefs.GetInt(PlayerIncrementedID);
        }
        public static void SetIncrementedPlayerID(int playerIncrementedid)
        {
            PlayerPrefs.SetInt(PlayerIncrementedID, playerIncrementedid);
        }

        //roomID, gameName, tableName,

        public static string GetSetRoomID
        {
            set { PlayerPrefs.SetString(RoomIDPref, value); }
            get { return PlayerPrefs.GetString(RoomIDPref); }
        }

        const string xpRewardCollectedAt = "sp_reward_collected_at";
        public static BigInteger GetSetXPLevelRewardCollectedAt
        {
            set
            {
                PlayerPrefs.SetString(xpRewardCollectedAt, BigIntegerToString(value));
                PlayerPrefs.Save();
            }
            get
            {
                string amount = PlayerPrefs.GetString(xpRewardCollectedAt);
                return StringToBigInteger(amount);
            }
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

        public static string GetRewardDate()
        {
            return PlayerPrefs.GetString(RewardDate);
        }

        public static void SetRewardDate(string Date)
        {
            PlayerPrefs.SetString(RewardDate, Date);
            PlayerPrefs.Save();
        }

        public static int GetRewardDay()
        {
            return PlayerPrefs.GetInt(RewardCollectDay, 0);
        }
        public static void SetRewardDay(int day)
        {
            PlayerPrefs.SetInt(RewardCollectDay, day);
            PlayerPrefs.Save();
        }

        public static string GetRewardCollectedDate()
        {
            return PlayerPrefs.GetString(RewardCollectedDate);
        }

        public static void SetRewardCollectedDate(string Date)
        {
            PlayerPrefs.SetString(RewardCollectedDate, Date);
            PlayerPrefs.Save();
        }
        const string PendingXP = "pending_xp";
        public static int GetPendingXP()
        {
            return PlayerPrefs.GetInt(PendingXP);
        }

        public static void SetPendingXP(int xp)
        {
            PlayerPrefs.SetInt(PendingXP, PlayerPrefs.GetInt(PendingXP) + xp);
            PlayerPrefs.Save();

        }

        public static bool IsPasswordChecked
        {
            get { return PlayerPrefs.GetInt(paswordChecked) == 0 ? false : true; }
            set { PlayerPrefs.SetInt(paswordChecked, value == true ? 1 : 0); }
        }
        const string TodayReward = "today_reward";
        //public static bool IsTodayRewardCollected
        //{
        //    get { return PlayerPrefs.GetInt(TodayReward) == 0 ? false : true; }
        //    set { PlayerPrefs.SetInt(TodayReward, value == true ? 1 : 0); }
        //}

        public static int TotalXpMyPlayer
        {
            get { return PlayerPrefs.GetInt(PlayerTotalXp); }
            set { PlayerPrefs.SetInt(PlayerTotalXp, value); }
        }

        #region For Frame Use Only
        public static void CheckFrameNumber(Image image, FramesCollections frameList, int frameIndex)
        {
            float adScale = 0;
            image.sprite = frameList.Sprites[frameIndex].Sprites;
            if (SceneManager.GetActiveScene().name == "Gameplay" && frameList.Sprites[frameIndex].size == 1.3f)
                adScale = frameList.Sprites[frameIndex].size - 0.175f;
            else
                adScale = frameList.Sprites[frameIndex].size;

            image.transform.GetComponent<RectTransform>().localScale = new UnityEngine.Vector3(adScale, adScale, adScale);
        }

        //public static bool checkFrameScaleValue(int index)
        //{
        //    return index == 9 || index == 10 || index == 13 || index == 14;
        //}
        //public static bool checkFrameFlyScaleValue(int index)
        //{
        //    return index >= 16;
        //}
        #endregion


        #region Poker 
        public static BigInteger GetStartingMaxAmountPoker()
        {
            return GetTotalChips(); // Poker_Max_Entry_Fee / PokerMaxTakinCashMultiplier;
        }
        public static BigInteger GetStartingMinAmountPoker()
        {
            return GetTotalChips(); //GetStartingMaxAmountPoker() / PokerMinTakinCashMultiplier;
        }
        public static BigInteger GetBlindAmountPoker()
        {
            Poker_Max_Entry_Fee.Show("Poker_Max_Entry_Fee");
            PokerBlindMultiplier.Show("PokerBlindMultiplier");
            return Poker_Max_Entry_Fee / PokerBlindMultiplier;
        }

        public static BigInteger GetPokerBuyInChips()
        {
            string chipsString = PlayerPrefs.GetString(PokerTotalBuyInChips);
            Debug.Log("BuyinChips Getting :" + chipsString);

            BigInteger chipsBigint = 0;
            if (BigInteger.TryParse(chipsString, out BigInteger result))
            {
                chipsBigint = result;
            }
            return chipsBigint;
        }

        public static void SetPokerBuyInChips(BigInteger chips)
        {
            BigInteger newChips = GetPokerBuyInChips() + chips;
            if (newChips < 0) newChips = 0;
            PlayerPrefs.SetString(PokerTotalBuyInChips, newChips.ToString());
            PlayerPrefs.Save();
        }

        // SetPokerBuyInChips above ADDS to the stored value, which is right for a bet or a payout but
        // useless when the stored value itself is wrong. The reconnect restore needs to overwrite it
        // outright with the stack the server still holds - same shape as SetTotalServerChips does for
        // the wallet.
        public static void SetPokerBuyInChipsAbsolute(BigInteger chips)
        {
            if (chips < 0) chips = 0;
            PlayerPrefs.SetString(PokerTotalBuyInChips, chips.ToString());
            PlayerPrefs.Save();
        }

        #endregion



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



        #region  Auto Seated btn Status

        const string Auto_Seated = "Auto_Seated";

        public static bool Get_Auto_Seated_Status()
        {
            return PlayerPrefs.GetInt(Auto_Seated) == 1 ? false : true;
        }

        public static void Set_Auto_Seated_Status(bool isTrue)
        {
            PlayerPrefs.SetInt(Auto_Seated, isTrue == true ? 0 : 1);
            PlayerPrefs.Save();
        }

        #endregion

        #endregion

        public static void Add_ChipsFrom()
        {
            SetTotalChips(GetTotalChips() + 125000);
            //if (!MatchHandler.isOffline())
            //    if (PhotonNetwork.IsConnectedAndReady)
            //        PhotonNetwork.LocalPlayer.SetCustomBigIntegerData(MyTotalCashKey, GetTotalChips());
        }
    }
}