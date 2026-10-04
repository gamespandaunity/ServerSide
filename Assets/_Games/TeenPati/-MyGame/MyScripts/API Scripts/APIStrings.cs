
public static class APIStrings
{
    static string PrefixOfApi
    {
        get { return "https://teenpatti.hizztechnologies.com/"; }
    }
    // Sending gold to other player
    public static string SendGoldWithPlayerIDAPIURL
    {
        get { return PrefixOfApi + "api/chips-transfer"; }
    }

    // Win, loose, tip, gift, bet gold to server
    public static string SendingWinLooseGold
    {
        get { return PrefixOfApi + "api/new-game-report"; }
    }

    // Getting sent gold, collected gold history record
    public static string GettingSentGoldRecordAPIURL
    {
        get { return PrefixOfApi + "api/chips-transfer-to-others/token_id_or_playerID="; }
    }

    // Getting Daily Reward Status get
    public static string GetdailyReardStatusAPIURL
    {
        get { return PrefixOfApi + "api/get_last_reward?player_id="; }
    }
    
    // Sending Daily Reward Status 
    public static string SenddailyReardStatusAPIURL
    {
        get { return PrefixOfApi + "api/get_reward?player_id="; }
    }

    // Getting list of received gold from others to me
    public static string GettingReceivedGoldRecordAPIURL
    {
        get { return PrefixOfApi + "api/chips-transfer-to-me/token_id_or_playerID="; }
    }

    // Accept gold that is sent by others API
    public static string AcceptGoldFromOtherAPIURL
    {
        get { return PrefixOfApi + "api/accept-chips/incremented_id="; }
    }
    // If member is VIP, if gold is not accepted by others, then dealer.VIP can recall gold 
    public static string RecallGoldForVIPMemberAPIURL
    {
        get { return PrefixOfApi + "api/reject-chips/incremented_id="; }
    }

    // Create a new user using guest/Google/Facebook
    public static string CreateUserAPIURL
    {
        get { return PrefixOfApi + "api/players/create"; }
    }

    // Add User XP to server
    public static string AddXPToServerAPIURL
    {
        //get { return PrefixOfApi + "api/change/players/details"; }
        get { return PrefixOfApi + "api/add-xp?player_id="; }
    }

    public static string PlayerChipsUpdate
    {
        get { return PrefixOfApi + "api/players/details/playerID="; }
    }
    // Getting already existed user whole detail
    public static string GetUserDetailAPIURL
    {
        get { return PrefixOfApi + "api/players/details/token_id_or_playerID="; }
    }

    // Getting image by adding image name from server
    public static string ImageURLAPI
    {
        get { return PrefixOfApi + "players/images/"; }
    }

    // Getting all emails list from server
    public static string EmailsListURLAPI
    {
        get { return PrefixOfApi + "api/rejected-chips-list/token_id_or_playerID="; }
    }

    // Getting Email Detail based on email id from server
    public static string EmailDetailURLAPI
    {
        get { return PrefixOfApi + "api/rejected-chips-list-detail/incremented_id="; }
    }

    // Collect gold from detailed email from server
    public static string CollectGoldFromEmailURLAPI
    {
        get { return PrefixOfApi + "api/collect-chips/incremented_id="; }
    }


    // Get Email history list  from server
    public static string EmailsHistoryListURLAPI
    {
        get { return PrefixOfApi + "api/history-of-collected-chips-list/token_id_or_playerID="; }
    }

    // Delete Email history list  from server
    public static string DeleteEmailsHistoryURLAPI
    {
        get { return PrefixOfApi + "api/delete-chips/incremented_id="; }
    }

    // Edit user detail and send it to server
    public static string EditUserDetailURLAPI
    {
        get { return PrefixOfApi + "api/players/update"; }
    }

    // Getting friends list 
    public static string FriendsListURLAPI
    {
        get { return PrefixOfApi + "api/local-friends/token_id_or_playerID="; }
    }

    // change Status of Friend
    public static string FriendStatusChangeURLAPI
    {
        get { return PrefixOfApi + "api/change-friend-status"; }
    }

    // Getting list of Pending Friend Requests
    public static string PendingFriendsRequestsURLAPI
    {
        get { return PrefixOfApi + "api/new-friends-request/token_id_or_playerID="; }
    }

    //Add New Friend 
    public static string AddNewFriendURLAPI
    {
        get { return PrefixOfApi + "api/add-friend"; }
    }

    // searching  Friend By Player ID
    public static string SearchsFriendsPlayerIDURLAPI
    {
        get { return PrefixOfApi + "api/search-player?query="; }
    }
    public static string SearchsFriendsPlayerIDURLAPIPart2
    {
        get { return "&loginPlayerID="; }
    }

    #region game gold handling on leaving game or in game play

    // Dragon tiger game betting chips to server
    public static string DragonTigerURLAPI
    {
        get { return PrefixOfApi + "api/new-game-dragon-tiger"; }
    }
    // Wingo lottary game betting chips to server
    public static string WingoLottaryBetSendURLAPI
    {
        get { return PrefixOfApi + "api/new-game-kala-chitta"; }
    }
    // Lucky War game betting chips to server
    public static string LuckyWarBetSendURLAPI
    {
        get { return PrefixOfApi + "api/new-game-lucky-war"; }
    }

    // Andar Bahar game betting chips to server
    public static string andarBaharBetSendURLAPI
    {
        get { return PrefixOfApi + "api/new-game-andar-bahr"; }
    }

    #endregion

    #region Gold Protection

    public static string SetPasswordAPIURL
    {
        get { return PrefixOfApi + "api/setPassword?playerId="; }
    }
    public static string GetGoldProtectionDetailAPIURL
    {
        get { return PrefixOfApi + "api/goldProtectionStatus?playerId="; }
    }
    public static string VerifyPasswordGoldProtectionAPIURL
    {
        //https://teenpatti.hizztechnologies.com/api/isPasswordTrue?playerId=14255092&password=a123456
        get { return PrefixOfApi + "api/isPasswordTrue?playerId="; }
    }
    public static string RemovePasswordAPIURL
    {
        //https://teenpatti.hizztechnologies.com/api/removePassword?playerId=14255092&protection_status=no&password=a123456

        get { return PrefixOfApi + "api/removePassword?playerId="; }
    }




    #endregion
    // _____________________________________________________________________________________________
    // Variable Strings
    public static string LocalFriendString
    {
        get { return "local_friend"; }
    }
    public static string RejectFriendRequest
    {
        get { return "local_reject_friend"; }
    }
    public static string UnFriend
    {
        get { return "local_unfriend"; }
    }
    public static string CancelFriendRequest
    {
        get { return "cancel_local_friend"; }
    }
    public static string AcceptAllFriendRequests
    {
        get { return "accept_all"; }
    }
    public static string RejectAllFriendRequests
    {
        get { return "reject_all"; }
    }
    public static string Pending
    {
        get { return "pending"; }
    }
}
