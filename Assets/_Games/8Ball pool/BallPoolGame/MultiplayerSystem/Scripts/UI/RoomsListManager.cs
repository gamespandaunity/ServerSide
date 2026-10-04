using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
//using PlayFab.ClientModels;
using System.Collections;
using static SocketIOUnityAdapter;

namespace NetworkManagement
{
    public delegate void SelecRoomHandler(Room room);
    public delegate void SelecPlayerHandler(NetworkManagement.PlayerProfile player);
    /// <summary>
    /// The rooms list manager.
    /// </summary>
    /// 
    [Serializable]
    public struct RoomsCounterText
    {
        public TextMeshProUGUI GoldChallengestxt, SilverChallengestxt;

    }
    public class RoomsListManager : ContentListManager
    {
        public static event SelecPlayerHandler OnSelecPlayerProfile;
        public static event SelecRoomHandler OnSelecRoom;
        public List<Room> currentroomList;
        public List<Room> OtherRoomList, GoldRoomList, SilverRoomList;
        public static List<Room> myChallengeGold, myChallengeSilver;
        public RoomData currentSelectedRoom = null;
        public Room currentSelectedRoomyour = null;
        PlayerProfileUI currentPlayersUI;
        public PlayerProfileUI playerUIBrows;
        public static RoomsListManager instance;
        public List<string> roomids = new List<string>();
        public bool iscreatedChallangebtn = true;
        public bool isbrowseChallenge = false;

        public Button BrowseChallengePopupAccept, BrowseChallengePopupCancel;
        public GameObject BrowseChallengePopupAnimation;
        public GameObject aIsRoomExists;
        public Text msg;
        public int currentRoomPrize;

        public GameObject challengeDeletePopup;
        public delegate void MyChallenge_UpdateCounter();
        public static MyChallenge_UpdateCounter Minechallenge;

        public void challengeDeletePopupYes()
        {
            if (currentSelectedRoomyour != null)
            {
                ApiAndRoomManager._instance.currentRoomId = currentSelectedRoomyour.mainPlayer.roomIds;
                // ApiAndRoomManager._instance.deleteBetUser();

                string roomid = EightBallPoolNetworkManager.PlayerToStringRoom(currentSelectedRoomyour.mainPlayer);
                currentRoomPrize = currentSelectedRoomyour.prize;
                //if (PhotonNetwork.LocalPlayer != PhotonNetwork.MasterClient)
                //{
                //    PhotonNetwork.JoinRoom(ApiAndRoomManager._instance.currentRoomId);        //Photon Removal

                //    roomids.Add(currentSelectedRoomyour.mainPlayer.roomIds);
                //}
                challengeDeletePopup.SetActive(false);
                ConstantsData_M.Log("delete yes =>" + roomid);

            }
            SoundManagerMain.instance.ClickSoundPlay();


        }


        public string ChallengeList_Count(int roomList)
        {
            if (roomList > 98)
            {
                return "99+";
            }
            else
            {
                return roomList.ToString();
            }
        }

        public void challengeDeletePopupNo()
        {
            challengeDeletePopup.SetActive(false);
            SoundManagerMain.instance.ClickSoundPlay();
        }




        private void Start()
        {
            if (instance == null)
            {
                instance = this;

            }
        }
        #region BROWSE CHALLENGE POP UP ACCEPT
        public void browseChallengePopupAccept()
        {
            if (currentSelectedRoom != null)
            {
                //HomeMenuManager.instance.acceptBrowsePopuploader.SetActive(true);
                staticVariables.OpponetProfile = new PlayerProfile()
                {
                    userName = currentSelectedRoom.player_info_name,
                    userId = currentSelectedRoom.player_info_id.ToString(),
                    imageURL = currentSelectedRoom.player_info_snap,

                };
                BrowseChallengePopupAccept.gameObject.SetActive(false);
                BrowseChallengePopupCancel.gameObject.SetActive(false);
                BrowseChallengePopupAnimation.SetActive(true);
                //  ApiAndRoomManager._instance.MessageContent = NetworkManager.mainPlayerToString(currentSelectedRoom.mainPlayer);
                ApiAndRoomManager._instance.challenge_transaction_id = currentSelectedRoom.transaction_id;
                ApiAndRoomManager._instance.winLoseChallengeId = currentSelectedRoom.transaction_id;
                Dictionary<string, string> betjoinDict = new Dictionary<string, string>();
                ServerConnection.DownloadSprite("/" + staticVariables.OpponetProfile.imageURL, DownloadedTexture =>
                {
                    staticVariables.opponentImage = DownloadedTexture;// scriptable.nullProfileImg;

                });
                // Assign values to the BetRequest fields
                betjoinDict["transaction_id"] = SocketIOUnityAdapter.transactionId;
                betjoinDict["second_player"] = staticVariables.UserProfiledata.user._id.ToString();
                betjoinDict["second_player_info"] = ApiAndRoomManager._instance.MessageContent;
                betjoinDict["region"] = currentSelectedRoom.region;
                staticVariables.isPlayingWithAI = false;
                ApiAndRoomManager._instance.Join_Challenge(betjoinDict,
                    Sucessfully =>
                    {
                        Single<int> data = JsonUtility.FromJson<Single<int>>(Sucessfully);
                        if (data.status)
                        {
                            if (Sucessfully.Contains("invalid bet"))
                            {
                                //Photon Removal PunNetwork.instance.RoomCollection.Remove(PunNetwork.instance.RoomCollection.Find(x => x.Name == currentSelectedRoom.transaction_id));
                                HomeMenuManager.instance.browseChallengePopup.SetActive(false);

                            }
                            else if (Sucessfully.Contains("low balance"))
                            {
                                ApiAndRoomManager._instance.DisplayError("Low Balance");
                                HomeMenuManager.instance.browseChallengePopup.SetActive(false);

                            }
                            else
                            {
                                GameObject gb = Instantiate(SpritesManager.Instance.spritesScriptable.waitingForOpponentPanel, HomeMenuManager.instance.mainCanvasObject.transform);
                                WaitingPanelScript.WaitingForroomID = currentSelectedRoom.transaction_id;
#if UNITY_EDITOR
                                //Photon Removal  HandleRoomJoined = null;
                                //Photon Removal  HandleRoomJoined += (roomname) =>
                                {
                                    Snake_Ladder.GameManager.instance.currentGameMode = Snake_Ladder.GameManager.GameMode.Against_OnlineFriend;

                                }
                                ;
                                ApiAndRoomManager._instance.winLoseChallengeId = currentSelectedRoom.transaction_id;
#endif
                                HomeMenuManager.instance.browseChallengePopup.SetActive(false);
                            }
                        }
                        else
                        {
                            ApiAndRoomManager._instance.DisplayError("Failed to join challenge");
                            HomeMenuManager.instance.browseChallengePopup.SetActive(false);
                        }
                        BrowseChallengePopupAccept.gameObject.SetActive(true);
                        BrowseChallengePopupCancel.gameObject.SetActive(true);
                        BrowseChallengePopupAnimation.SetActive(false);
                    }, failed =>
                    {
                        BrowseChallengePopupAccept.gameObject.SetActive(true);
                        BrowseChallengePopupCancel.gameObject.SetActive(true);
                        BrowseChallengePopupAnimation.SetActive(false);
                    }
                    );
            }
            SoundManagerMain.instance.ClickSoundPlay();

        }

        #endregion
        public void removelistManager()
        {
            // Constants_M.Log("removelistManager removelistManager::11111 ");

            if (currentroomList != null)
            {

                //    Constants_M.Log("removelistManager removelistManager::22222 ");

                List<Room> roomList = currentroomList.ToList();

                Room roomsss = roomList.Find(x => x.mainPlayer.roomIds == currentSelectedRoomyour.mainPlayer.roomIds);

                if (roomsss != null)
                {
                    //   Constants_M.Log("removelistManager removelistManager::333333 "+roomsss.mainPlayer);

                    roomList.Remove(roomsss);


                }
                else
                {
                    //  Constants_M.Log("removelistManager removelistManager::4444444 ");

                }


            }

        }
        Transform p2;
        public void ShowOthersRooms(Room rooms)
        {
            GameObject prefab = Instantiate(Challenge_browse, BrowseOthersRoomsContent);
            PlayerProfileUI playerUI = prefab.GetComponent<PlayerProfileUI>();
            Room currentRoom = rooms;
            Button currentButton;

            if (playerUI != null)
            {
                playerUI.SetPlayer(rooms);
            }
            else
            {
                ConstantsData_M.Log("player ui is null in show other room()");
            }
            currentButton = playerUI.joinBet;
            currentButton.onClick.AddListener(() =>
            {
                if (OnSelecRoom != null)
                {
                    OnSelecRoom(currentRoom);
                    // currentSelectedRoom = currentRoom;
                    playerUIBrows = playerUI;
                    UserModel userModel2 = staticVariables.UserProfiledata;
                    if (userModel2.user.silver_balance <= currentRoom.mainPlayer.prize)
                    {
                        PopupMessageManager.instance.ShowPopUp("Not enough Coins");
                    }
                    else
                    {
                        HomeMenuManager.instance.browseChallengePopup.SetActive(true);
                    }
                }

            });
            if (prefab != null)
            {
                AddButtonBrowse(prefab);
            }
            else
            {
                ConstantsData_M.Log("Prefab is null in show other room()");
            }
        }
        public void ShowMyRooms(Room rooms)
        {
            if (rooms.mainPlayer.isgoldcoins == staticVariables.isgoldcoins)
            {
                GameObject prefab = Instantiate(MyChallengeItem, BrowseYourRoomsContent);
                PlayerProfileUI playerUI = prefab.GetComponent<PlayerProfileUI>();
                playerUI.room = rooms;
                Room currentRoom = rooms;
                playerUI.SetPlayer(rooms);
                playerUI.deleteBet.onClick.AddListener(() =>
                {
                    if (OnSelecRoom != null)
                    {
                        OnSelecRoom(currentRoom);
                        currentSelectedRoomyour = currentRoom;
                        challengeDeletePopup.SetActive(true);
                    }
                });
                AddButtonBrowse(prefab);
            }
        }


        //private void OnGameChallengeCounter(Room[] rooms)
        //{
        //    GoldRoomList = currentroomList.Where(r => r.mainPlayer.isgoldcoins == true && r.mainPlayer.userId != staticVariables.UserProfiledata.user._id.ToString()).ToList();
        //    SilverRoomList = currentroomList.Where(r => r.mainPlayer.isgoldcoins == false && r.mainPlayer.userId != staticVariables.UserProfiledata.user._id.ToString()).ToList();
        //    foreach (GameButton_InfoSetter txt in staticVariables.All_games_Ref)
        //    {
        //        if (txt.game_id == 5 || txt.game_id == 9 || txt.game_id == 10)
        //        {
        //            txt.GameChallengestxt.GoldChallengestxt.text = "0";
        //            txt.GameChallengestxt.SilverChallengestxt.text = "0";
        //        }
        //        else
        //        {
        //            List<Room> allgold = rooms.Where(r => r.mainPlayer.isgoldcoins == true && txt.game_id.ToString() == r.mainPlayer.gameId && r.mainPlayer.userId != staticVariables.UserProfiledata.user._id.ToString()).ToList();
        //            List<Room> allsilver = rooms.Where(r => r.mainPlayer.isgoldcoins == false && txt.game_id.ToString() == r.mainPlayer.gameId && r.mainPlayer.userId != staticVariables.UserProfiledata.user._id.ToString()).ToList();
        //            txt.GameChallengestxt.GoldChallengestxt.text = ChallengeList_Count(allgold.Count());
        //            txt.GameChallengestxt.SilverChallengestxt.text = ChallengeList_Count(allsilver.Count());
        //        }
        //    }
        //}

        public GameObject ChallengeItemPrefab;
        public string RoomId;
        public Image CreateRequestPanelImage;
        public void GetRequests(string roomIds, int gameId)
        {
            RoomId = roomIds;
            UIMainMenManager.instance.gameRequestPanel.SetActive(true);
            RoomsListManager.instance.ChallengeRequestContent.Clear();
            CreateRequestPanelImage.sprite = SpritesManager.Instance.spritesScriptable.GameLogos[gameId - 1];
            ApiAndRoomManager._instance.GetChallengesRequests(roomIds, OnSuccess =>
            {
                OnSuccess.Show("Requests");
                var challengeRequests = JsonUtility.FromJson<Requests>(OnSuccess);
                if (challengeRequests.status)
                {
                    foreach (var item in challengeRequests.data)
                    {
                        var obj = Instantiate(ChallengeItemPrefab, RoomsListManager.instance.ChallengeRequestContent);
                        ChallengeItemPrefab prefab = obj.GetComponent<ChallengeItemPrefab>();
                        prefab.Initialized(item);
                    }
                }
            });
        }
    }

}
