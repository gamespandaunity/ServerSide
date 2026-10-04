//using System.Collections;
//using System.Collections.Generic;
//using BEKStudio;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;
//using TMPro;
// 
// 
//using Hashtable = ExitGames.Client.Photon.Hashtable;
//using UnityEngine.Events;

//namespace BEKStudio
//{
//    public class MenuController : MonoBehaviour
//    {
//        public static MenuController Instance;
//        public GameObject dontDestroyPrefab;
//        public GameObject messagePopup;
//        public TextMeshProUGUI coinText;
//        public Image avatarImg;
//        public TextMeshProUGUI avatarUsernameText;
//        //public Sprite[] avatars;
//        public IEnumerator botCountdown;
//        [Header("Main")] public GameObject mainScreen;
//        public RectTransform mainDiscPoolRect;
//        public RectTransform mainCarromRect;
//        public RectTransform mainPracticeRect;
//        [Header("Room")] public GameObject roomScreen;
//        public TextMeshProUGUI roomLondonEntryPriceText;
//        public TextMeshProUGUI roomLondonWinPriceText;
//        public TextMeshProUGUI roomParisEntryPriceText;
//        public TextMeshProUGUI roomParisWinPriceText;
//        public TextMeshProUGUI roomBerlinEntryPriceText;
//        public TextMeshProUGUI roomBerlinWinPriceText;
//        [Header("Vs")] public GameObject vsScreen;
//        public TextMeshProUGUI vsMsgText;
//        public Button vsBackBtn;
//        public GameObject vsUsersParent;
//        public TextMeshProUGUI vsHomeUsernameText;
//        public TextMeshProUGUI vsHomeUserBetText;
//        public TextMeshProUGUI vsAwayUsernameText;
//        public TextMeshProUGUI vsAwayUserBetText;
//        public TextMeshProUGUI vsTotalBetText;
//        public Image LeftAvatar;
//        public Image RightAvatar;
//        [Header("Username")] public GameObject usernameScreen;
//        public TMP_InputField usernameText;
//        [Header("Shop")] public GameObject shopScreen;

//        [Header("PriavteMenu")] public GameObject PrivateRoomScreen;
//        public TMP_InputField JoinCodeInput;
//        public TextMeshProUGUI roomCodeTxt;
//        public GameObject DisplayMessage;
//        public GameObject GameTypePanel;
//        public TextMeshProUGUI DisplayMessageText;
//        public GameObject AnimationSearching;
//        private string roomCode;
//        private UnityAction callback;

//        void Awake()
//        {
//            if (Instance == null)
//            {
//                Instance = this;
//            }
//        }

//        void Start()
//        {
//            Screen.sleepTimeout = SleepTimeout.NeverSleep;

//            GameObject dontDestroyObj = GameObject.Find("DontDestroy");
//            if (dontDestroyObj == null)
//            {
//                dontDestroyObj = Instantiate(dontDestroyPrefab);
//                dontDestroyObj.name = "DontDestroy";
//                DontDestroyOnLoad(dontDestroyObj);
//            }

//            UpdatePlayerInfo();
//            ResetBotSettings();

//            mainScreen.SetActive(false);
//            roomScreen.SetActive(false);
//            vsScreen.SetActive(false);
//            roomCodeTxt.text = "";
//            shopScreen.SetActive(false);
//            usernameScreen.SetActive(false);
//            PrivateRoomScreen.SetActive(false);
//            GameTypePanel.SetActive(false);
//            //PhotonController.Instance.whichMode = "carrom";
//            //PhotonController.Instance.whichMode = "carrom"; 
//            PhotonController.Instance.ConnectedToServer();
//            OpenMainScreen();

//            GameManager.Event_PlayerInfoUpdated += UpdatePlayerInfo;
//            PhotonController.Instance.gameOver = false;

//            if (MessageDisplayScript.instance == null)
//            {
//                GameObject.Instantiate(messagePopup);
//            }
//        }
//        private void OnDestroy()
//        {
//            GameManager.Event_PlayerInfoUpdated -= UpdatePlayerInfo;
//        }

//        public void UpdatePlayerInfo()
//        {
//            avatarImg.sprite = GameManager.Instance.GetUserAvatar();
//            avatarUsernameText.text = GameManager.Instance.userName;
//            PhotonNetwork.NickName = PlayerPrefs.GetString("userName", "Player");
//            //LeftAvatar.sprite = GameManager.Instance.GetUserAvatar();
//        }

//        void ResetBotSettings()
//        {
//            PhotonController.Instance.playWithBot = false;
//            PhotonController.Instance.botAvatar = 0;
//            PhotonController.Instance.botName = "";
//        }

//        public void OpenMainScreen()
//        {
//            mainScreen.SetActive(true);
//            roomScreen.SetActive(false);
//            vsScreen.SetActive(false);
//            roomCodeTxt.text = "";
//            vsMsgText.text = "";
//            shopScreen.SetActive(false);
//            usernameScreen.SetActive(false);
//            GameTypePanel.SetActive(false);
//            PrivateRoomScreen.SetActive(false);
//            mainDiscPoolRect.anchoredPosition = new Vector2(-759f, -198f);
//            mainCarromRect.anchoredPosition = new Vector2(759f, -198f);
//            mainPracticeRect.anchoredPosition = new Vector2(0, -1103);

//            LeanTween.move(mainDiscPoolRect, new Vector2(-216f, -198f), 0.5f).setEaseOutBack();
//            LeanTween.move(mainCarromRect, new Vector2(227, -198f), 0.5f).setEaseOutBack();
//            LeanTween.move(mainPracticeRect, new Vector2(0, -744), 0.5f).setEaseOutBack();
//        }

//        public void UpdateCurrencyText()
//        {
//            if (PlayerPrefs.GetInt("coin") == 0)
//            {
//                coinText.text = "0";
//            }
//            else
//            {
//                coinText.text = PlayerPrefs.GetInt("coin", 0).ToString("###,###,###");
//            }
//        }

//        public void FreeCoinsBtn()
//        {
//            AdsManager.Instance.ShowRewardedAd();
//        }

//        public void PracticeModeBtn()
//        {
//            if (!PhotonNetwork.IsConnectedAndReady)
//            {
//                PhotonController.Instance.ConnectedToServer();
//                return;
//            }

//            PhotonController.Instance.whichMode = "practice";
//            PhotonController.Instance.CreatePracticeRoom();
//        }

//        public void OnClickPublicRoom()
//        {
//            if (!GameManager.Instance.isNetWorking)
//            {
//                DisplayPopUp(true, "Please Connect your Wifi!");
//                return;
//            }
//            if (!PhotonNetwork.IsConnectedAndReady)
//            {
//                DisplayPopUp(true, "Connecting To Server...");
//                callback = OnClickPublicRoom;
//                PhotonController.Instance.ConnectedToServer();
//                return;
//            }
//            GameManager.Instance.currentGameMode = GameManager.GameMode.QuickMatch;
//            GameTypePanel.SetActive(true);
//        }

//        public void OnClickVsAi()
//        {
//            GameManager.Instance.currentGameMode = GameManager.GameMode.Ai;
//            GameTypePanel.SetActive(true);
//        }

//        public void OnClickPrivateRoom()
//        {
//            if (!GameManager.Instance.isNetWorking)
//            {
//                DisplayPopUp(true, "Please Connect your Wifi!");
//                return;
//            }
//            if (!PhotonNetwork.IsConnectedAndReady)
//            {
//                DisplayPopUp(true, "Connecting To Server...");
//                callback = OnClickPrivateRoom;
//                PhotonController.Instance.ConnectedToServer();
//                return;
//            }
//            GameManager.Instance.currentGameMode = GameManager.GameMode.AgainstFriend;
//            GameTypePanel.SetActive(true);
//        }

//        public void OnClickGameType(string gametype)
//        {
//            PhotonController.Instance.whichMode = gametype;
//            GameTypePanel.SetActive(false);
//            vsMsgText.text = "";
//            if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.QuickMatch))
//            {
//                AnimationSearching.SetActive(true);
//                roomScreen.SetActive(false);
//                vsBackBtn.gameObject.SetActive(false);
//                vsBackBtn.interactable = true;
//                vsUsersParent.SetActive(false);
//                vsScreen.SetActive(true);
//                PhotonController.Instance.whichRoom = "berlin";
//                PhotonController.Instance.roomEntryPice = 0;
//                PhotonController.Instance.FindPublicRoom();
//            }
//            else if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.AgainstFriend))
//            {
//                roomCodeTxt.text = string.Empty;
//                PrivateRoomScreen.SetActive(true);
//            }
//            else if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.Ai))
//            {
//                PhotonController.Instance.playWithBot = true;
//                GameManager.Instance.SetBotInfo();
//                GameManager.Instance.botAvatarIndex = GameManager.Instance.GetRandomSpriteIndex();
//                PhotonController.Instance.botAvatar = GameManager.Instance.GetBotSpriteIndex();
//              //  PhotonController.Instance.botName = GameManager.Instance.GetBotName();
//                SceneManager.LoadScene("Game");
//            }
//        }

//        public void OnClickCreatePrivateRoom()
//        {
//            AnimationSearching.SetActive(true);
//            PrivateRoomScreen.SetActive(false);
//            roomScreen.SetActive(false);
//            vsBackBtn.gameObject.SetActive(false);
//            vsBackBtn.interactable = true;
//            vsUsersParent.SetActive(false);
//            vsScreen.SetActive(true);
//            PhotonController.Instance.whichRoom = "berlin";
//            PhotonController.Instance.roomEntryPice = 0;
//            PhotonController.Instance.CreatePrivateRoom();
//        }

//        public void OnClickJoinPrivateRoom()
//        {
//            if (JoinCodeInput.text.ToString().Equals(string.Empty))
//            {
//                DisplayPopUp(true, "Please Type In The RoomCode!");
//                PrivateRoomScreen.SetActive(true);
//                return;
//            }
//            AnimationSearching.SetActive(true);
//            PrivateRoomScreen.SetActive(false);
//            roomScreen.SetActive(false);
//            vsBackBtn.gameObject.SetActive(false);
//            vsBackBtn.interactable = true;
//            vsUsersParent.SetActive(false);
//            vsScreen.SetActive(true);
//            PhotonController.Instance.whichRoom = "berlin";
//            PhotonController.Instance.roomEntryPice = 0;
//            PhotonController.Instance.FindPrivateRoom(JoinCodeInput.text.ToString());
//        }

//        public void RoomsBackBtn()
//        {
//            mainScreen.SetActive(true);
//            roomScreen.SetActive(false);
//        }

//        public void VsBackBtn()
//        {
//            vsBackBtn.interactable = false;
//            vsMsgText.text = "Leaving room...";
//            if (botCountdown != null)
//            {
//                StopCoroutine(botCountdown);
//            }
//            ResetBotSettings();
//            vsAwayUsernameText.text = string.Empty;
//            PhotonNetwork.LeaveRoom();
//        }

//        public void VsOnLeftRoom()
//        {
//            vsBackBtn.gameObject.SetActive(false);
//            vsBackBtn.interactable = true;
//            vsUsersParent.SetActive(false);
//            vsScreen.SetActive(false);
//            roomCodeTxt.text = "";
//            mainScreen.SetActive(true);
//        }

//        public void VsJoinedRoom()
//        {

//            if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.AgainstFriend))
//            {
//                if (PhotonNetwork.IsMasterClient)
//                {
//                    roomCodeTxt.text = "RoomCode: " + roomCode.Split("_")[0];
//                }
//            }
//            else
//            {
//                roomCodeTxt.text = string.Empty;
//            }

//            vsHomeUserBetText.text = PhotonController.Instance.roomEntryPice.ToString("###,###,###");
//            vsAwayUserBetText.text = PhotonController.Instance.roomEntryPice.ToString("###,###,###");
//            vsTotalBetText.text = "0";

//            vsBackBtn.gameObject.SetActive(true);
//            vsBackBtn.interactable = true;
//            PhotonController.Instance.StopTimeOut();
//            vsMsgText.text = "Waiting opponent...";
//            vsUsersParent.SetActive(true);

//            if (PhotonNetwork.PlayerList.Length == 1)
//            {
//                vsHomeUsernameText.text = PhotonNetwork.PlayerList[0].NickName;
//                vsUsersParent.transform.GetChild(0).GetComponent<Image>().sprite = GameManager.Instance.GetUserAvatar();

//                if (botCountdown == null)
//                {
//                    botCountdown = BotCountdown();
//                }
//                else
//                {
//                    StopCoroutine(botCountdown);
//                    botCountdown = BotCountdown();
//                }

//                StartCoroutine(botCountdown);
//            }
//            else if (PhotonNetwork.PlayerList.Length == 2)
//            {
//                AnimationSearching.SetActive(false);

//                if (PhotonNetwork.IsMasterClient)
//                {
//                    vsHomeUsernameText.text = PhotonNetwork.PlayerList[0].NickName;
//                    vsAwayUsernameText.text = PhotonNetwork.PlayerList[1].NickName;

//                    vsUsersParent.transform.GetChild(0).GetComponent<Image>().sprite = GameManager.Instance.GetAvatarSprite((int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"]);
//                    vsUsersParent.transform.GetChild(1).GetComponent<Image>().sprite = GameManager.Instance.GetAvatarSprite((int)PhotonNetwork.PlayerList[1].CustomProperties["avatar"]);
//                }
//                else
//                {
//                    vsHomeUsernameText.text = PhotonNetwork.PlayerList[1].NickName;
//                    vsAwayUsernameText.text = PhotonNetwork.PlayerList[0].NickName;

//                    vsUsersParent.transform.GetChild(0).GetComponent<Image>().sprite = GameManager.Instance.GetAvatarSprite((int)PhotonNetwork.PlayerList[1].CustomProperties["avatar"]);
//                    vsUsersParent.transform.GetChild(1).GetComponent<Image>().sprite = GameManager.Instance.GetAvatarSprite((int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"]);
//                }


//                VsStartMatch();
//            }
//        }

//        IEnumerator BotCountdown()
//        {
//            yield return new WaitForSeconds(Random.Range(10f, 15f));

//            if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.QuickMatch) && PhotonNetwork.CurrentRoom.IsOpen)
//            {
//                AnimationSearching.SetActive(false);
//                PhotonNetwork.CurrentRoom.IsOpen = false;
//                PhotonNetwork.CurrentRoom.IsVisible = false;
//                GameManager.Instance.SetBotInfo();
//                GameManager.Instance.botAvatarIndex = GameManager.Instance.GetRandomSpriteIndex();
//                PhotonController.Instance.botAvatar = GameManager.Instance.GetBotSpriteIndex();
//                PhotonController.Instance.botName = GameManager.Instance.GetBotName();
//                PhotonController.Instance.playWithBot = true;
//                VsOnPlayerJoinedRoom();
//            }
//        }

//        public void VsOnPlayerJoinedRoom()
//        {
//            if (PhotonController.Instance.playWithBot)
//            {
//                AnimationSearching.SetActive(false);
//                vsAwayUsernameText.text = PhotonController.Instance.botName;
//                RightAvatar.sprite = GameManager.Instance.GetAvatarSprite(PhotonController.Instance.botAvatar);
//                VsStartMatch();
//            }
//            else
//            {
//                if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
//                {
//                    AnimationSearching.SetActive(false);
//                    PhotonNetwork.CurrentRoom.IsOpen = false;
//                    PhotonNetwork.CurrentRoom.IsVisible = false;
//                    PhotonController.Instance.playWithBot = false;

//                    vsHomeUsernameText.text = PhotonNetwork.PlayerList[0].NickName;
//                    vsAwayUsernameText.text = PhotonNetwork.PlayerList[1].NickName;
//                    RightAvatar.sprite = GameManager.Instance.GetAvatarSprite((int)PhotonNetwork.PlayerList[1].CustomProperties["avatar"]);

//                    VsStartMatch();
//                }
//            }

//            StopBotCoroutine();

//        }


//       public void StopBotCoroutine()
//        {
//            if (botCountdown != null)
//                StopCoroutine(botCountdown);
//        }

//        void VsStartMatch()
//        {
//            if (PhotonNetwork.PlayerList.Length == 2)
//            {
//                if (botCountdown != null)
//                {
//                    StopCoroutine(botCountdown);
//                }
//            }

//            float currentCoin = PlayerPrefs.GetInt("coin");
//            float newCoin = currentCoin - PhotonController.Instance.roomEntryPice;

//            PlayerPrefs.SetInt("coin", PlayerPrefs.GetInt("coin") - PhotonController.Instance.roomEntryPice);
//            PlayerPrefs.Save();

//            vsBackBtn.gameObject.SetActive(false);

//            LeanTween.value(PhotonController.Instance.roomEntryPice, 0, 1f).setOnUpdate((float val) =>
//            {
//                vsHomeUserBetText.text = "" + (int)val;
//                vsAwayUserBetText.text = "" + (int)val;
//            });

//            LeanTween.value(currentCoin, newCoin, 1f).setOnUpdate((float val) =>
//            {
//                if (val < 1)
//                {
//                    coinText.text = "0";
//                }
//                else
//                {
//                    coinText.text = ((int)val).ToString("###,###,###");
//                }
//            });

//            LeanTween.value(0, PhotonController.Instance.roomEntryPice * 2, 1f).setOnUpdate((float val) =>
//            {
//                vsTotalBetText.text = "" + (int)val;
//            }).setOnComplete(() =>
//            {
//                UpdateCurrencyText();
//                if (PhotonNetwork.IsMasterClient)
//                {
//                    SceneManager.LoadScene("Game");
//                }
//            });
//        }


//        public void DisplayRoomCode(string roomCode)
//        {
//            this.roomCode = roomCode;
//        }


//        private Coroutine displayPopupCoroutine;

//        public void DisplayPopUp(bool state, string error = "")
//        {
//            if (!state)
//            {
//                DisplayMessage.SetActive(state);
//                if (callback != null)
//                {
//                    callback.Invoke();
//                    callback = null;
//                }

//                // Stop the specific coroutine if it's running
//                if (displayPopupCoroutine != null)
//                {
//                    StopCoroutine(displayPopupCoroutine);
//                    displayPopupCoroutine = null;
//                }

//                return;
//            }

//            // Show Error Message
//            //Constants_M.Log(error);
//            DisplayMessageText.text = error;
//            DisplayMessage.SetActive(state);
//            vsScreen.SetActive(false);
//            StopCoroutine(BotCountdown());
//            roomCodeTxt.text = "";

//            // Start the coroutine and store the reference
//            if (displayPopupCoroutine != null)
//            {
//                StopCoroutine(displayPopupCoroutine);
//            }
//            displayPopupCoroutine = StartCoroutine(DisaplayPopupAfterDelay());
//        }

//        IEnumerator DisaplayPopupAfterDelay()
//        {
//            yield return new WaitForSeconds(1.5f);
//            DisplayMessage.SetActive(false);
//            displayPopupCoroutine = null; // Reset the reference after it finishes
//        }

//    }

//}