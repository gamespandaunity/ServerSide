using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UserModel
{
    public bool status;
    public string message;
    public User user;
    public string access_token;

    public UserModel()
    {
        user = new User();
    }
}

[System.Serializable]

public struct UserBalanceResponse
{
    public bool status;
    public UserBalance data;
    [System.Serializable]

    public struct UserBalance
    {
        public string silver_balance;
        public string gold_balance;

    }
}
public enum SelectedGameType
{
    BallPoll, Carrom, Ludo, CarRacing, Cricket
}
[System.Serializable]
public class CreateBetResponseModel
{
    public bool status;
    public string message;
    public DataModel data;

    [System.Serializable]
    public class DataModel
    {
        public string transaction_id;
        public string first_name;
        public string last_name;
        public int previous_challenges_count;
        public int bet_expires_sec;
        public int challenge_time_minutes;
        public int leaving_time_sec;
        public int silver_balances;
        public int gold_balances;
    }
}

[System.Serializable]

public class JoinBetResponseModel
{
    public bool status;
    public DataModel data;
    [System.Serializable]

    public class DataModel
    {
        public string notification_type;
        public string transaction_id;
        public string bet_type;
        public int bet_amount;
        public string bet_expires_sec;
        public int player_info_id;
        public string player_info_name;
        public string player_info_snap;
    }
}
[System.Serializable]
public class User
{

    public string file_url;
    public int _id;
    public string first_name = "you";
    public string last_name = "";
    public string full_name;
    public string country;
    public string status;
    public string email;
    public string password;
    public string role;
    public string phone;
    public int silver_balance;
    public int gold_balance;
    public string createdAt;
    public string updatedAt;
    public string referral_code;
    public string imei;
    public string user_login_token;
    public object user_ip;
    public string block;
    public bool allow_to_game;
    public string game_restrict_at;
    public string restriction_end_at;
    public int attempts;
    public int __v;
    public List<bool> bet_block;
    public string updated_by;
    public string deviceToken;
    public int is_authorized_merchants;

}

#region Create Borrow Coins Request 
[System.Serializable]
public class CreateBorrowRequest
{
    public int sender;

    public int receiver;

    public string gold_coin;

    public string silver_coin;

    public string remarks;
}
[System.Serializable]
public class CreateBorrowRequest_ResponseStatus
{
    public bool status;
    public CreateBorrowRequest_ResponseDetail responseDetail;
}
[System.Serializable]
public class CreateBorrowRequest_ResponseDetail
{
    public string transaction_id;
    public string borrow_type;
    public int borrow_amount;
    public int player_info_id;
    public string player_info_name;
    public string player_info_snap;
}


#endregion
#region Receive Model
[Serializable]
public class BorrowReceiver_Parent
{
    public bool status;
    public List<BorrowReceiver_Model> data;
}
[Serializable]
public class BorrowReceiver_Model
{
    public int _id;
    public int sender;
    public int receiver;

    public string gold_coin;

    public string silver_coin;

    public string status;

    public string remarks;

    public DateTime createdAt;

    public DateTime updatedAt;

    public string transaction_id;

    public int is_reverse;
    public string first_name;
    public string file_url;
}
#endregion
#region Sender Borrow Request Model
[Serializable]
public class SenderBorrowRequestModel
{
    public int _id;
    public int sender;
    public int receiver;
    public string gold_coin;
    public string silver_coin;
    public string status;
    public string remarks;
    public DateTime createdAt;
    public DateTime updatedAt;
    public string transaction_id;
    public int is_reverse;
    public string first_name;
    public string file_url;

}
[Serializable]
public class SenderBorrowRequestParent
{
    public bool status;
    public List<SenderBorrowRequestModel> data;
}
#endregion
#region Accept / Reject notification Borrow
[Serializable]
public class AcceptReject_BorrowParent
{
    public bool status;

    public AcceptReject_Borrow data;
}

[Serializable]
public class AcceptReject_Borrow
{
    public string notificationStatus;

    public string goldCoin;

    public string silverCoin;

    public string transactionId;
}
#endregion
#region GET USER INFO

[Serializable]
public class UserDataParent
{

    public int _id;
    public string first_name;
    public string user_ip;
    public string block;
    public string last_name;
    public string country;
    public string status;
    public string created_by;
    public string updated_by;
    public string email;
    public string password;
    public string role;
    public string phone;
    public int silver_balance;
    public int gold_balance;
    public string file_url;
    public string allow_to_game;
    public string game_restrict_at;
    public string restriction_end_at;
    public DateTime createdAt;
    public string updatedAt;
    public int __v;
    public string user_login_token;
    public string deviceToken;
    public string attempts;
    public string userId;
    public string bet_block;
    public string referral_code;
    public string full_name;

}

#endregion
#region Create Player

[Serializable]
public class CreateNewPlayer
{
    public bool status;
    public string message;
    public int _id;
    public string player_status;
}
#endregion
#region VALIDATE-RESPONSE
[Serializable]
public class ValidateResponse
{

    public bool status;
    public string message;
}

#endregion
#region GENERATE_OTP_
[Serializable]
public class GENERATE_OTP_RESPONSE
{
    public bool status;
    public int otp;
    public int _id;
}

#endregion
#region RESET PASSWORD
[Serializable]
public class ResetPAssword_RESPONSE
{
    public bool status;

    public string message;
}

#endregion
#region DALY REWARDS HISTORY
[Serializable]
public class DailyRewardHistoryData
{
    public int _id;
    public int user_id;
    public string country;
    public DateTime createdAt;
    public DateTime updatedAt;
    public int silver;
    public int gold;
    public string status;
    public string transaction_id;
}
[Serializable]
public class DailyRewardHistory
{
    public bool status;
    public string message;
    public List<DailyRewardHistoryData> data;
}


#endregion
#region TimeZone
[Serializable]
public class TimeZoneInfo
{
    public int year;
    public int month;
    public int day;
    public int hour;
    public int minute;
    public int seconds;
    public int milliSeconds;
    public string dateTime;
    public string date;
    public string time;
    public string timeZone;
    public string dayOfWeek;
    public bool dstActive;
}
#endregion
#region SHP PACKAGE
[Serializable]
public class ShoppackageModel
{

    public bool status;
    public string message;
    public List<Shopdatum> data;

}

[Serializable]
public class Shopdatum
{
    public string _id;
    public string status;
    public string file_url;
    public string title;
    public string silver_coin;
    public string gold_coin;
    public string amount_usd;
    public string updated_by;
    public double amount;
    public string user_country;
    public string currency;
}

#endregion
#region Withdraw History


[Serializable]
public class WithdrawalHistoryData
{
    public int _id;
    public string status;
    public string coins;
    public object proved_date;
    public string remarks;
    public object proved_by;
    public object total_amount;
    public object admin_commission;
    public object withdraw_amount;
    public string transaction_id;
    public string client_id;
    public object snap;
    public string createdAt;
    public DateTime _createdAt;
    public string updatedAt;
    public object updated_by;
    public object created_by;
    public object payment_id;


}
[Serializable]
public class Withdrawlhistory
{
    public List<WithdrawalHistoryData> data;
    public string currentPage;
    public string totalPages;
    public string perPage;
}
#endregion
#region GAME HISTORY
[Serializable]
public class GameHistoryObj
{
    public List<Gamehistory> data;
    public string currentPage;
    public string totalPages;
    public string perPage;
}
[Serializable]
public class Gamehistory
{
    //public string transaction_id;
    //public string gold;
    //public Gamedetail gamedetail;
    //public string user_coins;
    //public string createdAt;
    //public string status;
    public int _id;
    public string first_player;
    public string transaction_id;
    public int ignore_count;
    public int reject_counter;
    public string game_id;
    public string gold;
    public string silver;
    public string status;
    public string remark;
    public object counter;
    public object admin_commission;
    public int user_coins;
    public string main_player_info;
    public string second_player_info;
    public object is_read;
    public string screenstatus;
    public string createdAt;
    public string updatedAt;
    public object __v;
    public string second_join_time;
    public string second_player;
    public string winner;
    public string bet_type;
    public string title;
    public string file_url;
    public string challenge_time_minutes;
    public string maximum_challenges;
    public string commission;
    public string time_restrictions;
    public string description;
    public string ignore_bet;
    public string bet_expires_sec;
    public string updated_by;
    public string reject_bet;
    public string leaving_time_sec;
    public string created_by;
    public DateTime _createdAt;
    public string coins_type;
    public string rounds_played;

}

#endregion
#region BORROW HISTORY
[Serializable]
public class BorrowHistoryModel
{
    public int _id;
    public int sender;
    public int receiver;
    public string gold_coin;
    public string silver_coin;
    public string status;
    public string remarks;
    public string createdAt;
    public DateTime _createdAt;
    public string updatedAt;
    public string transaction_id;
    public int is_reverse;
    public string sender_first_name;
    public string sender_file_url;
    public string receiver_first_name;
    public string receiver_file_url;
    public string iam;


}
[Serializable]
public class BorrowHistory
{
    public List<BorrowHistoryModel> data;
    public string currentPage;
    public int totalPages;
    public string perPage;
}
#endregion
#region GameDetail
[Serializable]
public class game_detail
{
    public int _id;
    public int game_id;
    public string title;
    public string file_url;
    public bool status;
    public string challenge_time_minutes;
    public string maximum_challenges;
    public string commission;
    public string time_restrictions;
    public string description;
    public string ignore_bet;
    public string bet_expires_sec;
    public string category;
    public string createdAt;
    public string updatedAt;
    public string updated_by;
    public string reject_bet;
    public string leaving_time_sec;
    public string created_by;
    public Texture2D LOGO;
}

[Serializable]
public class GameDetailParent
{
    public bool status;
    public string message;
    public List<game_detail> game_detail;
}
#endregion
#region BannerModel
[Serializable]
public class BannersModel
{
    public int _id;
    public string status;
    public string title;
    public string description;
    public string file_url;
    public string createdAt;
    public string updatedAt;
}
[Serializable]
public class BannersModelParent
{
    public List<BannersModel> BannerDetails;
}
#endregion
#region Round History
[Serializable]
public class RoundHistoryModel
{
    public int _id;
    public string transaction_id;
    public string winner;
    public string coins;
    public string created_at;
    public int commission;
    public string status;
    public int first_player_invest;
    public int second_player_invest;
}
[Serializable]
public class RoundHistoryParent
{
    public bool status;
    public List<RoundHistoryModel> data;
    public string message;
}


#endregion
#region Probablity
[Serializable]
public class ProbablityResponse
{
    public bool status;
    public string value;
}
#endregion
#region BetInfo
[Serializable]
public class BetInfoModel
{
    public bool status;
    public Data data;
    public string message;
    [Serializable]
    public class Data
    {
        public string transaction_id;
        public string winner;
        public string looser;
        public string status;
        public string coins_type;
        public string bet_type;
    }
}
#endregion
#region SIINGLE CREATE ROUND

[Serializable]
public class CreateRoundSingle
{
    public bool status;
    public string message;
    public int _id;
}
#endregion
#region HANDLE  SIINGLE  ROUND

[System.Serializable]
public class RouletteBet
{
    public string type;  // Type of bet (e.g., "line_1", "straight", "street")
    public object value; // This can hold different types of data, so use 'object'
    public float amount;   // Amount of the bet
    public RouletteBet(string type, object value, float amount)
    {
        this.type = type;
        this.value = value;
        this.amount = amount;
    }
}
[Serializable]
public class Probability
{
    public string _1;
    public string _2;
    public string _5;
    public string _10;
    public string _20;
    public string _40;
}
[Serializable]
public class Response
{
    public bool Status;
    public bool Win;
    public string Message;
    public Probability Probability;
    public int RoundResultNumber;
    public int WinAmount;
    public int SilverBalance;
    public int GoldBalance;
}

[System.Serializable]
public class RouletteRound
{
    public string round_id; // The ID of the round
    public List<RouletteBet> bets;  // List of bets in this round
}

[System.Serializable]
public class RouletteHandleResponse
{
    public bool status;                      // Status of the round
    public bool win;                         // Indicates if the player won
    public string message;                   // Message from the server
    public int probability;                  // Probability of winning
    public List<int> selectedNumber;         // List of selected numbers
    public List<string> selectedTypes;       // List of selected bet types
    public string @break;                    // Break or separator message (Note: '@' is used because 'break' is a reserved keyword in C#)
    public int roundResultNumber;            // The result number of the round
    public List<string> roundResultTypes;    // List of result types (e.g., even, low, etc.)
    public List<string> roundWinningResultTypes; // List of winning result types
    public int winAmount;                    // Amount won
    public int silver_balance;               // Silver balance after the round
    public int gold_balance;                 // Gold balance after the round
}

#endregion
#region MERCHANT -LIST - MODEL
[Serializable]
public class MerchantsDetail
{
    public int _id;
    public string country;
    public DateTime createdAt;
    public DateTime updatedAt;
    public int client_id;
    public string remarks;
    public int silver_balance;
    public int gold_balance;
    public string first_name;
    public string last_name;
    public string file_url;
    public int coins;
    public double pricing_in_dollar;
    public int completed_orders;
    public float rating;
    public string currency_code;
    public double local_pricing;
    public double per_coin_local_price;
    public double per_coin_dollar_price;
}
[Serializable]
public class MerchantListModel_Parent
{
    public bool status;
    public string message;
    public List<MerchantsDetail> data;
}


#endregion
#region PRICING-MARKETPLACE
[Serializable]
public class PricingRespomse
{
    public bool status;
    public string message;
}
#endregion
#region Place-Order-Model
[Serializable]
public class PlaceOrderModel
{
    public bool status;
    public string message;
    public int order_id;
}
#endregion
#region MERCHANT HISTORY
[Serializable]

public class MerchantDetailedHistory
{
    public int _id;
    public string client_id;
    public DateTime created_at;
    public int coins;
    public string pricing_dollar;
    public string status;
    public DateTime updated_at;
    public string snap;
    public string remarks;
    public int merchant_id;
    public string transaction_id;
    public string review;
    public string rating;
    public string pricing_local;
    public string currency_code;
}
[Serializable]
public class MerchantHistoryModelParent
{
    public bool status;
    public string message;
    public List<MerchantDetailedHistory> data;
}
#endregion
#region ClientHistory
[Serializable]
public class ClientDetail
{
    public DateTime created_at;
    public DateTime updated_at;
    public int _id;
    public int coins;
    public int merchant_id;
    public string client_id;
    public string status;
    public string snap;
    public string remarks;
    public string pricing_dollar;
    public string transaction_id;
    public string pricing_local;
    public string currency_code;   
    public string rating;
    public string review;
}
[Serializable]
public class ClientHistoryParent
{
    public bool status;
    public string message;
    public List<ClientDetail> data;
}
#endregion
#region APPROVE__oRDER
[Serializable]
public class ApproveOrderDetail
{
    public int silver_balance;
    public int gold_balance;
    public string client_id;
    public string coins;
    public string message;
}
[Serializable]
public class ApproveMarketOrder
{
    public bool status;
    public ApproveOrderDetail data;
}


#endregion
#region Reject__oRDER
[Serializable]
public class RejectOrderDetail
{
    public int silver_balance;
    public int gold_balance;
    public string client_id;
    public string coins;
    public string message;
}
[Serializable]
public class RejectMarketOrder
{
    public bool status;
    public ApproveOrderDetail data;
}


#endregion
