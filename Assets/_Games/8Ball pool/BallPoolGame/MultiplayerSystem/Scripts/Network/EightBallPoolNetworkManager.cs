
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace NetworkManagement
{
    public delegate void UpdateCoinsHandler(int coins);
    public delegate void LoginHandler(LoginedState state);
    public delegate void PurchasedHandler(NetworkManagement.ProductProfile productProfile, PurchasedState state);
    public delegate void LoadPlayersHandler(NetworkManagement.Room[] players);
    public delegate void LoadPlayerHandler(NetworkManagement.PlayerProfile player);
    public delegate void SetPlayerHandler(NetworkManagement.PlayerProfile player);
    public delegate bool ChackIsFriend(string id);

    public enum LoginedState
    {
        Successful = 0,
        Unsuccessful
    }

    public enum PurchasedState
    {
        Successful = 0,
        Unsuccessful
    }

    /// <summary>
    /// Player state.
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// The user is online.
        /// </summary>
        Online = 0,
        /// <summary>
        /// The user is online but away from their computer.
        /// </summary>
        Away,
        /// <summary>
        /// The user is online but set their status to busy.
        /// </summary>
        Busy,
        /// <summary>
        /// The user is offline.
        /// </summary>
        Offline,
        /// <summary>
        /// The user is playing a game.
        /// </summary>
        Playing
    }

    /// <summary>
    /// The Network room, who include players.
    /// </summary>
    /// 
    [Serializable]
    public class Room
    {
        public int id;
        public int prize;


        public Room(int id, int prize, PlayerProfile player)
        {
            this.id = id;
            this.prize = prize;
            mainPlayer =  player;          
        }

        public PlayerProfile mainPlayer;

    }

    /// <summary>
    /// The Product type.
    /// </summary>
    [System.Serializable]
    public class ProductType
    {
        public bool updateIcon { get; set; }

        public string type = "Product type";
        public string nameInВatabase;
        /// <summary>
        /// The product can be bought only with real money.
        /// </summary>
        public bool isRealMoney = false;
        /// <summary>
        /// The product can be bought one time.
        /// </summary>
        public bool oneTimeBought = false;
        /// <summary>
        /// How many time the product can be bought?.
        /// </summary>
        public int maxCount = 0;
        public Sprite icon;
        public DefaultProductProfile[] defaultProducts;

        public static ProductType GetProductTypeByType(string type, ProductType[] productsTypeList)
        {
            foreach (var item in productsTypeList)
            {
                if (item.type == type)
                {
                    return item;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// All product icons which is available by default.
    /// </summary>
    [System.Serializable]
    public class DefaultProductProfile
    {
        /// <summary>
        /// This product name.
        /// </summary>
        public string name;
        /// <summary>
        /// The price in real money.
        /// </summary>
        public int price;
        /// <summary>
        /// Product price.
        /// </summary>
        public Texture2D icon;
        /// <summary>
        /// All possible sources  of product.
        /// </summary>
        public UnityEngine.Object[] sources;
    }

    /// <summary>
    /// The product.
    /// </summary>
    public class ProductProfile
    {
        /// <summary>
        /// Gets the data of product.
        /// </summary>
        public ProductData data
        {
            get;
            private set;
        }

        /// <summary>
        /// Product icone.
        /// </summary>
        public Texture2D icon
        {
            get;
            private set;
        }

        public ProductProfile(ProductData data, Texture2D icon)
        {
            this.data = data;
            this.icon = icon;
        }


        public void SetIcon(Texture2D icon)
        {
            if (icon)
            {
                this.icon = icon;
            }
        }
    }

    /// <summary>
    /// The player profile.
    /// </summary>
    /// 
    [Serializable]
    public class PlayerProfile
    {
        /// <summary>
        /// This users unique identifier.
        /// </summary>
        public string id;


      //  public bool canPlayOffline { get { return coins >= prize && coins >= NetworkManager.social.minCoinsCount; } }

       // public bool canPlayOnLine { get { return coins >= prize && coins >= NetworkManager.social.minOnLinePrize; } }

        /// <summary>
        /// The room id.
        /// </summary>
        public int roomId;


        /// <summary>
        ///  Is this user the main?
        /// </summary>
        public bool isMain;

        /// <summary>
        /// Avatar image of the user.
        /// </summary>
        public Texture2D image;

        /// <summary>
        /// Avatar image URL of the user.
        /// </summary>
        public string imageURL;

        /// <summary>
        /// Avatar image name of the user.
        /// </summary>
        public string imageName;

        /// <summary>
        /// Is this user a friend?
        /// </summary>
        public bool isFriend;

        /// <summary>
        /// This user's username.
        /// </summary>
        public string userName;

        /// <summary>
        /// Player coins.
        /// </summary>
        public int coins;

        /// <summary>
        /// Player prize for the match.
        /// </summary>
        public int prize;
        public int overIndex;
        public string gameId;

        public string userId;
        public string roomIds;
        public bool isgoldcoins;
        public string playerTTL;

        /// <summary>
        /// Presence state of the user.
        /// </summary>
        public PlayerState state;


        public PlayerProfile(string id, bool isMain, Texture2D image, string imageURL, string imageName, bool isFriend, string userName, PlayerState state, int coins, int prize)
        {
            this.id = id;
            this.isMain = isMain;
            this.image = image;
            this.imageURL = imageURL;
            this.imageName = imageName;
            this.isFriend = isFriend;
            this.userName = userName;
            this.state = state;
            this.coins = coins;
            this.prize = prize;



        }
        public PlayerProfile(string userName, string imageurl, int prize, string gameId, string userId, string roomIds, bool isgoldcoins, string playerTTL)
        {
            this.userName = userName;
            this.imageURL = imageurl;
            this.prize = prize;
            this.gameId = gameId;
            this.userId = userId;
            this.roomIds = roomIds;
            this.isgoldcoins = isgoldcoins;
            this.playerTTL = playerTTL;


        }                  
        public PlayerProfile(string userName, string imageurl, int prize, string gameId, string userId, string roomIds, bool isgoldcoins, string playerTTL,int overIndex)
        {
            this.userName = userName;
            this.imageURL = imageurl;
            this.prize = prize;
            this.overIndex = overIndex;
            this.gameId = gameId;
            this.userId = userId;
            this.roomIds = roomIds;
            this.isgoldcoins = isgoldcoins;
            this.playerTTL = playerTTL;


        }
        public PlayerProfile(string id, bool isMain, Texture2D image, string imageURL, string imageName, bool isFriend, string userName, PlayerState state, int coins, int prize, string gameId, string userId, string roomIds, bool isgoldcoins, string playerTTL)
        {
            this.id = id;
            this.isMain = isMain;
            this.image = image;
            this.imageURL = imageURL;
            this.imageName = imageName;
            this.isFriend = isFriend;
            this.userName = userName;
            this.state = state;
            this.coins = coins;
            this.prize = prize;
            this.gameId = gameId;
            this.userId = userId;
            this.roomIds = roomIds;
            this.isgoldcoins = isgoldcoins;
            this.playerTTL = playerTTL;
        }
        public PlayerProfile()
        {

        }

        public void SetImage(Texture2D image)
        {
            try
            {
                if (image)
                {
                    this.image = image;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }


    }

    /// <summary>
    /// All management of network, passes by using this class.
    /// </summary>
    public class EightBallPoolNetworkManager
    {
        public static bool initialized = false;
        private static bool mainPlayerLoadedInProgress = false;

        public static event UpdateCoinsHandler OnCoinsUpdated;

        public static string absoluteURL
        {
            get { return Application.absoluteURL.Replace("%20", "").Replace(" ", ""); }
        }

        public static void CallUpdatedCoins()
        {
            if (OnCoinsUpdated != null)
            {
                OnCoinsUpdated(mainPlayer.coins);
            }
        }

     

        public static event LoadPlayerHandler OnRandomPlayerLoaded;

        public static event LoadPlayersHandler OnFriendsAndRandomPlayersLoaded;

        public static event LoadPlayerHandler OnMainPlayerLoaded;

        public static event LoadPlayerHandler OnFriendLoaded;
 
        public static event SetPlayerHandler OnPlayerSet;


        public static void Disable()
        {
            OnRandomPlayerLoaded = null;
            OnFriendsAndRandomPlayersLoaded = null;

            OnMainPlayerLoaded = null;
            OnFriendLoaded = null;
            OnPlayerSet = null;                           
        }

        /// <summary>
        /// Gets the main player.
        /// </summary>
        public static NetworkManagement.PlayerProfile mainPlayer
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the opponent player, if there is one opponent player on the room.
        /// </summary>
        public static NetworkManagement.PlayerProfile opponentPlayer
        {
            get;
            set;
        }

        public static void LoadMainPlayer()
        {
            string username = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;

            mainPlayer = new PlayerProfile(username, staticVariables.UserProfiledata.user.file_url,
                   100,
                   "0",
                   staticVariables.UserProfiledata.user._id.ToString(),
                      "",
                   staticVariables.isgoldcoins,
                   "12000",SelectOver.SelectedOver);
            
                if (OnMainPlayerLoaded != null)
                {
                    mainPlayer.userId = staticVariables.UserProfiledata.user._id.ToString();
                    OnMainPlayerLoaded(mainPlayer);
                }
            
        }
     

        
        public static PlayerProfile PlayerFromString(string playerData)
        {
            string str = "";
            int step = 0;

            string id = "-1";
            string imageURL = "";
            string imageName = "";
            bool isFrient = false;
            string userName = "";
            PlayerState state = PlayerState.Offline;
            int coins = 0;
            int prize = 0;

            string gameId = "";
            string userId = "";
            string roomId = "";
            bool isgoldcoins = false;
            string playerTtl = "";


            foreach (char item in playerData)
            {
                if (item != ';')
                {
                    str += item;
                }
                else
                {
                    step++;
                    switch (step)
                    {
                        case 1:
                            id = str;

                            break;
                        case 2:
                            imageURL = str;
                            break;
                        case 3:
                            imageName = str;
                            break;
                        case 4:
                            userName = str;
                            break;
                        case 5:
                            int.TryParse(str, out int Pstate);
                            state = (PlayerState)Pstate;
                            break;
                        case 6:
                            int.TryParse(str, out int Cstate);
                            coins = Cstate;
                            // coins = int.Parse(str);
                            break;
                        case 7:
                            int.TryParse(str, out int pRstate);
                            prize = pRstate;
                            // prize = int.Parse(str);
                            break;
                        case 8:
                            gameId = str;
                            break;
                        case 9:
                            userId = str;
                            break;
                        case 10:
                            roomId = str;
                            break;
                        case 11:
                            isgoldcoins = bool.Parse(str);
                            break;
                        case 12:
                            playerTtl = str;
                            break;
                        default:
                            break;
                    }
                    str = "";
                }
            }
            return new PlayerProfile(id, false, null, imageURL, imageName, isFrient, userName, state, coins, prize, gameId, userId, roomId, isgoldcoins, playerTtl);
        }
        public static string PlayerToStringRoom(PlayerProfile playerProfile) //RAR
        {
            if (playerProfile == null)
            {
                return "";
            }
            playerProfile.userName = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;
            playerProfile.gameId = ApiAndRoomManager.currentGameId.ToString();
            playerProfile.userId = staticVariables.UserProfiledata.user._id.ToString();
            string roomId = ApiAndRoomManager._instance.challenge_transaction_id;
            playerProfile.isgoldcoins = staticVariables.isgoldcoins;

            return playerProfile.userName + ";" + playerProfile.imageURL + ";"+
                 playerProfile.prize + ";" + playerProfile.gameId + ";" +
                playerProfile.userId + ";" + roomId + ";" + playerProfile.isgoldcoins + ";" + playerProfile.playerTTL + ";";
            //  Room Name : 0;;;Player 465;0;1609;109;1;59;c6d8da9;True;120000;
        }
     
        public static PlayerProfile PlayerFromStringRoom(string playerData)
        {
            string str = "";
            int step = 0;
            string userName = "";
            string imageurl = "";
            int prize = 0;
            string gameId = "";
            string userId = "";
            string roomIds = "";
            bool isgoldcoins = false;
            string playerTtl = "";

        //Data: ferjad__ 5; 100; 1; 6; ; True; ;
            foreach (char item in playerData)
            {
                if (item != ';')
                {
                    str += item;
                }
                else
                {
                    step++;
                    switch (step)
                    {
                        case 1:
                            userName = str;
                            break;
                        case 2:
                            imageurl = str;
                            break;

                        case 3:
                            int.TryParse(str, out int pRstate);
                            prize = pRstate;
                            // prize = int.Parse(str);
                            break;
                        case 4:
                            gameId = str;
                            break;
                        case 5:
                            userId = str;
                            break;
                        case 6:
                            roomIds = str;
                            break;
                        case 7:
                            isgoldcoins = bool.Parse(str);
                            break;
                        case 8:
                            playerTtl = str;
                            break;
                        default:
                            break;
                    }
                    str = "";
                }
            }
            return new PlayerProfile(userName, imageurl, prize, gameId, userId, roomIds, isgoldcoins, playerTtl);
        }
    }



}
