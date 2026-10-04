// Copyright 2016 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

//using Unity.Notifications.Android;

using NaughtyAttributes;
using UnityEngine.SceneManagement;

namespace Firebase.Sample.Messaging
{
    using NetworkManagement;
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Collections;
    using static Firebase.Sample.Messaging.FirebaseHandler.NotificationData;
    using System.Threading.Tasks;
    using static SocketIOUnityAdapter;
    using UnityExtensions;

    public class FirebaseHandler : MonoBehaviour
    {
        public static FirebaseHandler _instance;
        public List<ChallengeNotificationInfo> challengeNotificationsList;
        public static bool challengeNotificationInProcess;

        private bool firebaseLoaded = false;
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected virtual void Start()
        {
            
          
        }

 

        [Button]
        public void SendDummyNotification()
        {
            GameObject dsf = Instantiate(NotificationUIManager.instance.SimpleNotification);
            SimpleNotification obj = dsf.GetComponent<SimpleNotification>();
            obj.Notification(body: "notification.Body notification ki body", header: "notification.Title", "notificationRecieveData.player_info_snap", yesBtnStatus: true, noBtnStatus: true);
        }
        public class NotificationData
        {
            public NotificationData(IDictionary<string, string> dictionary)
            {

                bool parsesucess = Enum.TryParse(dictionary["notification_type"], true, out _NotificationType ParsedNotificationType);
                ParsedNotificationType = parsesucess ? ParsedNotificationType : _NotificationType.None;
                this.ParsedNotificationType = ParsedNotificationType;
                switch (ParsedNotificationType)
                {

                    case _NotificationType.friend_accept:
                        notification_type = dictionary["notification_type"];
                        this.player_info_id = int.Parse(dictionary["_id"]);
                        this.player_info_name = dictionary["first_name"];
                        this.player_info_snap = dictionary["file_url"];
                        break;
                    case _NotificationType.friend_reject:
                        notification_type = dictionary["notification_type"];
                        this.player_info_id = int.Parse(dictionary["_id"]);
                        this.player_info_name = dictionary["first_name"];
                        this.player_info_snap = dictionary["file_url"];
                        break;
                    case _NotificationType.friend_request:
                        notification_type = dictionary["notification_type"];
                        this.player_info_id = int.Parse(dictionary["_id"]);
                        this.player_info_name = dictionary["first_name"];
                        this.player_info_snap = dictionary["file_url"];
                        break;
                    case _NotificationType.borrow_request:

                        notification_type = dictionary["notification_type"];
                        this.transaction_id = dictionary["transaction_id"];
                        this.bet_type = dictionary["borrow_type"];
                        this.bet_amount = int.Parse(dictionary["borrow_amount"]);
                        this.player_info_id = int.Parse(dictionary["player_info_id"]);
                        this.player_info_name = dictionary["player_info_name"];
                        this.player_info_snap = dictionary["player_info_snap"];
                        //this.game_id = int.Parse(dictionary["game_id"]);
                        break;
                    case _NotificationType.challenge_accepted:
                        this.notification_type = dictionary["notification_type"];
                        this.transaction_id = dictionary["transaction_id"];
                        this.bet_type = dictionary["bet_type"];
                        this.bet_amount = int.Parse(dictionary["bet_amount"]);
                        this.bet_expires_sec = dictionary["bet_expires_sec"];
                        this.player_info_id = int.Parse(dictionary["player_info_id"]);
                        this.player_info_name = dictionary["player_info_name"];
                        this.player_info_snap = dictionary["player_info_snap"];
                        this.game_id = int.Parse(dictionary["game_id"]);
                        break;
                    case _NotificationType.invited_to_play:
                        this.notification_type = dictionary["notification_type"];
                        this.transaction_id = dictionary["transaction_id"];
                        this.bet_type = dictionary["bet_type"];
                        this.bet_amount = int.Parse(dictionary["bet_amount"]);
                        this.bet_expires_sec = dictionary["bet_expires_sec"];
                        this.player_info_id = int.Parse(dictionary["player_info_id"]);
                        this.player_info_name = dictionary["player_info_name"];
                        this.player_info_snap = dictionary["player_info_snap"];
                        this.game_id = int.Parse(dictionary["game_id"]);
                        if (this.game_id == 13)
                            this.overs = int.Parse(dictionary["overs"]);
                        break;
                    case _NotificationType.borrow_approved_Reject:
                        this.notification_type = dictionary["notification_type"];
                        this.bet_type = dictionary["borrow_type"];
                        //this.player_info_snap = dictionary["player_info_snap"];
                        // this.bet_amount = int.Parse(dictionary["borrow_amount"]);
                        break;
                    case _NotificationType.game_play:
                        this.notification_type = dictionary["notification_type"];
                        this.transaction_id = dictionary["transaction_id"];
                        this.bet_type = dictionary["bet_type"];
                        this.coins_type = dictionary["coins_type"];
                        this.bet_amount = int.Parse(dictionary["bet_amount"]);
                        this.bet_expires_sec = dictionary["bet_expires_sec"];
                        this.player_info_id = int.Parse(dictionary["player_info_id"]);
                        this.player_info_name = dictionary["player_info_name"];
                        this.player_info_snap = dictionary["player_info_snap"];
                        this.game_id = int.Parse(dictionary["game_id"]);
                        this.server_address = dictionary["server_address"];
                        break;
                    case _NotificationType.challenge_leaved:
                        this.notification_type = dictionary["notification_type"];
                        this.transaction_id = dictionary["transaction_id"];
                        this.bet_type = dictionary["bet_type"];
                        this.bet_amount = int.Parse(dictionary["bet_amount"]);
                        this.bet_expires_sec = dictionary["bet_expires_sec"];
                        this.player_info_id = int.Parse(dictionary["player_info_id"]);
                        this.player_info_name = dictionary["player_info_name"];
                        this.player_info_snap = dictionary["player_info_snap"];
                        break;
                    case _NotificationType.challenge_rejected:
                        this.notification_type = dictionary["notification_type"];
                        this.transaction_id = dictionary["transaction_id"];
                        this.bet_type = dictionary["bet_type"];
                        this.bet_amount = int.Parse(dictionary["bet_amount"]);
                        this.bet_expires_sec = dictionary["bet_expires_sec"];
                        this.player_info_id = int.Parse(dictionary["player_info_id"]);
                        this.player_info_name = dictionary["player_info_name"];
                        this.player_info_snap = dictionary["player_info_snap"];
                        break;
                    case _NotificationType.challenge_deleted:
                        this.notification_type = dictionary["notification_type"];
                        this.transaction_id = dictionary["transaction_id"];
                        this.player_info_id = int.Parse(dictionary["player_info_id"]);
                        break;
                    case _NotificationType.challenge_expired:
                        ApiAndRoomManager._instance.ModifyUserBalance();
                        break;
                    case _NotificationType.logout:
                        this.notification_type = dictionary["notification_type"];
                        break;
                    case _NotificationType.referral_reward:
                        this.notification_type = dictionary["notification_type"];
                        break;
                    case _NotificationType.gift:
                        this.notification_type = dictionary["notification_type"];
                        this.giftId = dictionary["_id"];
                        break;
                }
            }
            public string notification_type;
            public string transaction_id;
            public string bet_type;
            public int bet_amount;
            public int game_id;
            public int overs;
            public string bet_expires_sec;
            public int player_info_id;
            public string player_info_name;
            public string player_info_snap;
            public string _order_id;
            public string marketTransactionId;
            public string trade_id;
            public string client_id;
            public string client_name;
            public _NotificationType ParsedNotificationType;
            public string f;
            public string s1;
            public string s2;
            public string s3;
            public int drawId;
            public string giftId;
            public string coins_type;
            public string server_address;
            public enum _NotificationType
            {
                None = 0,
                invited_to_play,
                challenge_accepted,
                challenge_rejected,
                game_loose,
                borrow_request,
                borrow_approved_Reject,
                game_play,
                challenge_leaved,
                challenge_deleted,
                challenge_ignored,
                challenge_expired,



                logout,
                referral_reward,
                friend_request,
                friend_accept,
                friend_reject,
                gift
            }
        }




        public void UpdateFirebaseToken()
        {
            if (staticVariables.firebaseToken != null)
            {
                WWWForm form = new WWWForm();
                if (staticVariables.firebaseToken != "")
                {
                    form.AddField("deviceToken", staticVariables.firebaseToken);
                    form.AddField("user_login_token", SystemInfo.deviceUniqueIdentifier);
                }
                ApiAndRoomManager._instance.UpdateFirebaseToken(form, onsuccess =>
                {
                    UserModel userModel = JsonUtility.FromJson<UserModel>(onsuccess);
                    userModel.access_token = staticVariables.UserProfiledata.access_token;
                    staticVariables.UserProfiledata = userModel;
                    PlayerPrefs.SetString("userModel", JsonUtility.ToJson(userModel));

                    if (userModel.user != null && UIMainMenManager.instance != null && userModel.status)
                    {
                        UIMainMenManager.instance.playerInfo.displayPlayerName.text = userModel.user.first_name.ToString() + " " + userModel.user.last_name.ToString();
                        UIMainMenManager.instance.playerInfo.playersId.text = "UserID: " + userModel.user._id.ToString();
                        UIMainMenManager.instance.silverCoinText.text = userModel.user.silver_balance.ToString();
                        UIMainMenManager.instance.goldCoinText.text = userModel.user.gold_balance.ToString();

                    }

                });
            }
        }
       
        bool istokkenfetch = false;
       
      
        
        public void ShowNextChallengeNotification()
        {
            Debug.Log("bhai log chal rha");
            if (challengeNotificationsList.Count > 0&& challengeNotificationsList[0].notificationRecieveData!=null)
            {
            Debug.Log("1sdf");
                ApiAndRoomManager.currentGameId = challengeNotificationsList[0].gameId;
                ApiAndRoomManager._instance.userIdentifierOrEmail = challengeNotificationsList[0].userIdOrEmail.ToString();
                ApiAndRoomManager._instance.winLoseChallengeId = challengeNotificationsList[0].winnerLoseId;
                staticVariables.currentTime = int.Parse(challengeNotificationsList[0].currentTime);
                NotificationUIManager.instance.AcceptNotificationIns(challengeNotificationsList[0].notificationRecieveData);
                staticVariables.isnotificationcounter = true;
                challengeNotificationInProcess = true;
            }
            else if (challengeNotificationsList.Count > 0 && challengeNotificationsList[0].packet != null)
            {
            Debug.Log("2sdf");
                ApiAndRoomManager.currentGameId = challengeNotificationsList[0].gameId;
                ApiAndRoomManager._instance.userIdentifierOrEmail = challengeNotificationsList[0].userIdOrEmail.ToString();
                ApiAndRoomManager._instance.winLoseChallengeId = challengeNotificationsList[0].winnerLoseId;
                staticVariables.currentTime = int.Parse(challengeNotificationsList[0].currentTime);
                NotificationUIManager.instance.AcceptNotificationIns(challengeNotificationsList[0].packet);
                staticVariables.isnotificationcounter = true;
                challengeNotificationInProcess = true;
            }

        }

        [Serializable]
        public class ChallengeNotificationInfo
        {
            public int gameId;
            public string userIdOrEmail;
            public string winnerLoseId;
            public string currentTime;
            public NotificationData notificationRecieveData;
            public GamePacket packet;

        }

    }
}
