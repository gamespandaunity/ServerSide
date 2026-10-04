using NetworkManagement;
 
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace BEKStudio
{
    public class ResultManagerForCarromOffline : MonoBehaviour
    {
        public static ResultManagerForCarromOffline instance;
        public RawImage Winner, Looser;
        public TextMeshProUGUI WinnerName, LooserName, popupText;

        public GameObject FinishPanel;
        public GameObject FriendRequest;
        public GameObject Rematch;
        public GameObject chest;
        public GameObject winnerText, loserText, drawText;
        public Image[] coins;
        public Sprite[] gold, silver;
        public static bool IsCarromWin = false;

        private void Awake()
        {
            instance = this;
        }
        public void WinnerInitialize(Texture2D pp, string name)
        {
            Winner.texture = pp;
            WinnerName.text = name;
        }
        public void LooserInitialize(Texture2D pp, string name)
        {
            Looser.texture = pp;
            LooserName.text = name;
        }
        //Photon Removal  [PunRPC]
        public void WinPlayer(bool IsWin, int playerId)
        {
            IsCarromWin = true;
            FinishPanel.SetActive(true);
          
              
               if (!GameControllerOffline.Instance.isCarronMultiplayerGame)
                {
                    FriendRequest.SetActive(false);
                    Rematch.SetActive(false);
                    if (IsWin)
                    {
                        if (playerId == staticVariables.UserProfiledata.user._id)
                        {
                            WinnerInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                            ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                            popupText.text = "Congratulations! Your account has been credited with coins";
                            LooserInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                            "YOU WIN".Show();
                        }
                        else
                        {
                            LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                            WinnerInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                            ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
                            popupText.text = "Your balance has been deducted from your account. Please try again";
                        }
                    }
                    else
                    {
                        if (playerId == staticVariables.UserProfiledata.user._id)
                        {
                            LooserInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                            WinnerInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                            ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                            popupText.text = "Your balance has been deducted from your account. Please try again";
                        }
                        else
                        {
                            LooserInitialize(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                            //GuestGrandScripts.instance.ChangeGuestCoins(-staticVariables.currentPrize);
                            ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
                            WinnerInitialize(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name);
                            "AI WIN".Show();
                            popupText.text = "Congratulations! Your account has been credited with coins";
                        }
                    }
                }
            }
        
        public void Draw()
        {
            winnerText.SetActive(false);
            loserText.SetActive(false);
            drawText.SetActive(true);
            chest.SetActive(false);
            FinishPanel.SetActive(true);
            popupText.text = "Game ended, No balance deducted";
           // PhotonController.Instance.gameOver = true;
            WinnerInitialize(staticVariables.ProfilePicture,
                                staticVariables.UserProfiledata.user.first_name + " " +
                                staticVariables.UserProfiledata.user.last_name);

            LooserInitialize(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
            ApiAndRoomManager._instance.DrawChallenge(ApiAndRoomManager._instance.winLoseChallengeId);
        }
        public void leaveRoom()
        {
      
            SceneManager.LoadScene("Home");
        }
        public void SendRematch()
        {
            //if (PhotonNetwork.InRoom)
            //{                                                                                                     //Photon Removal
            //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
            //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
            //    PhotonNetwork.LeaveRoom();
            //}
            Onleft();
        }
        public void Onleft()
        {
            //PunNetwork.HandleRoomJoined = null;
            //PunNetwork.HandleRoomJoined += OnOpenChallengeCreated;                        //Photon Removal
            {
                EightBallPoolNetworkManager.mainPlayer.prize = EightBallPoolNetworkManager.mainPlayer.prize;
            }
            staticVariables.screenstatus = "sad";
            Dictionary<string, string> betRequestDict = new Dictionary<string, string>();
            betRequestDict["first_player"] = staticVariables.UserProfiledata.user._id.ToString();
            betRequestDict["second_player"] = staticVariables.lastOpponentId.ToString();
            betRequestDict["game_id"] = ApiAndRoomManager.currentGameId.ToString();
            betRequestDict["remark"] = "Multipler with Golden Coins";
            betRequestDict["screenstatus"] = staticVariables.screenstatus;
            betRequestDict["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver";

            if (staticVariables.isgoldcoins)
            {
                betRequestDict["gold"] = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
                betRequestDict["silver"] = "0";
            }
            else
            {
                betRequestDict["silver"] = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
                betRequestDict["gold"] = "0";
            }

            ApiAndRoomManager._instance.create_Challenge_Invite(betRequestDict,
            successMessage =>
            {
                ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 1reate success room---");
                ApiAndRoomManager._instance.ModifyUserBalance();
                ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 2reate success room---");
                ConstantsData_M.Log("Bet ID");
                Rematch.SetActive(false);
                AndroidUtility._ShowAndroidToastMessage("Rematch Request Send");
            },
            failureMessage =>
            {

                ApiAndRoomManager._instance.DisplayError("failureMessage");

                //Debug.Log("Gold Bet Failed to Create: " + failureMessage);

            });
            SoundManagerMain.instance.ClickSoundPlay();
        }
        //        void OnOpenChallengeCreated(Photon.Realtime.Room room)                                                                                                                                                                             //Photon Removal
        //        {
        //            PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);
        //            ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;
        //            ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);
        //            // HomeMenuManager.instance.yourChallengeLoader.SetActive(false);
        //            ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
        //#if !UNITY_EDITOR
        //        PhotonNetwork.LeaveRoom();
        //#endif
        //        }
        public void SendFriendRequest()
        {
            FriendRequest.SetActive(false);
            ApiAndRoomManager._instance.SendRequestToFriends(staticVariables.OpponetProfile.userId, OnSuccess =>
            {
                Single<int> response = JsonUtility.FromJson<Single<int>>(OnSuccess);
                AndroidUtility._ShowAndroidToastMessage(response.message);

            });
        }
    }
}