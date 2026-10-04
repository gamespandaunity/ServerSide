using BallPool;
using NetworkManagement;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensions;

public class PlayerDetails
{
    public string playerName;
    public string PlayerUrl;
    public int id;
    public bool isAI;

    public PlayerDetails(int id, string name, string imgurl, bool isai = false)
    {
        playerName = name;
        this.id = id;
        PlayerUrl = imgurl;
        isAI = isai;
    }
}
public class Win8Ball : MonoBehaviour
{
    public static Win8Ball inst;
    public PlayerDetails WinPlayers;
    private RectTransform winnerTransfor, looserTransform;
    private bool isDecrementing = false, isCompleted = false;

    public Image coinInChest, totalCoin_L, totalCoin_W;
    [SerializeField] private Text WinplayerNameText, looserPlayerNameText, WinnerCoinCounter, looserCoinCounter, popUpText;
    [SerializeField] private AudioSource collectCoinsSource;
    [SerializeField] private AudioClip coinCollectionClips;
    [SerializeField] private float waitSeconds_Counter, closeConfettiWait, WinLooseOpenTime, enable2CoinAnim_wait;

    public RectTransform winnerSide, looserSide;
    public GameObject confetti1, confetti2, firework, popUp;
    public Image[] coins;
    public GameObject win, loose, winTotalBlnc_parent, LooseTotalBlnc_parent;
    public GameObject Coin2;
    public Sprite silver, golden;
    public TextMeshProUGUI winnerTotalBalance, looserTotalBalance;
    public RawImage rawProfileImageWinner, rawProfileImageLooser;
    [SerializeField] int betAmount, secondBetAmount, initiator = 0;
    int totalBalance;
    UserModel userModel;
    public string WinMessage = "Congratulations! Your account has been credited with coins";
    public string LooseMessage = "Your balance has been deducted from your account. Please try again";
    public Texture2D AiImg;
    public GameObject FriendRequest;
    public GameObject Rematch;

    private void Awake()
    {
        userModel = staticVariables.UserProfiledata;
        WinnerCoinCounter.text = string.Empty;
        looserCoinCounter.text = string.Empty;
        inst = this;
    }
    private void OnDisable()
    {
        ApiAndRoomManager.OnCoinsUpdated -= UpdateUserCoins;
    }
    public void UpdateUserCoins(UserBalanceResponse coins)
    {
        winnerTotalBalance.text = staticVariables.isgoldcoins ? coins.data.gold_balance.ToString() : coins.data.silver_balance.ToString();
        looserTotalBalance.text = staticVariables.isgoldcoins ? coins.data.gold_balance.ToString() : coins.data.silver_balance.ToString();
    }
    private void OnEnable()
    {
        UpdateUserCoins(ApiAndRoomManager.LastFetchedCoins);

        ApiAndRoomManager.OnCoinsUpdated += UpdateUserCoins;

        if (staticVariables.isgoldcoins)
        {
            for (int i = 0; i < coins.Length; i++)
            {
                coins[i].sprite = SpritesManager.Instance.spritesScriptable.goldenCoin;

            }
            WinnerCoinCounter.text = userModel.user.silver_balance.ToString();
            totalBalance = userModel.user.gold_balance;
        }
        else
        {
            for (int i = 0; i < coins.Length; i++)
            {
                coins[i].sprite = SpritesManager.Instance.spritesScriptable.silvercoin;

            }
            WinnerCoinCounter.text = userModel.user.gold_balance.ToString();
            totalBalance = userModel.user.silver_balance;
        }
        if (staticVariables.isgoldcoins)
        {
            coinInChest.sprite = SpritesManager.Instance.spritesScriptable.coinsInGold;
            totalCoin_L.sprite = SpritesManager.Instance.spritesScriptable.goldenCoin;
            totalCoin_W.sprite = SpritesManager.Instance.spritesScriptable.goldenCoin;
        }
        else
        {
            coinInChest.sprite = SpritesManager.Instance.spritesScriptable.coinsInSilver;
            totalCoin_L.sprite = SpritesManager.Instance.spritesScriptable.silvercoin;
            totalCoin_W.sprite = SpritesManager.Instance.spritesScriptable.silvercoin;
        }
    }

    void Start()
    {
        collectCoinsSource.clip = coinCollectionClips;
        collectCoinsSource.loop = false;
        collectCoinsSource.Play();

        StartCoroutine(Coin2Activate_Animation());

        if (betAmount > 0 && !isDecrementing)
        {
            StartCoroutine(DecrementBetAmount());

        }
        if (initiator <= betAmount && !isCompleted)
        {

            StartCoroutine(InitiatorIncrementor());
        }

    }
    public void InitializeWinnerUI(Texture2D pp, string name)
    {
        rawProfileImageWinner.texture = pp;
        WinplayerNameText.text = name;
    }

    public void InitializeLoserUI(Texture2D pp, string name)
    {
        rawProfileImageLooser.texture = pp;
        looserPlayerNameText.text = name;
    }
    public static bool EightisFinishTriggered = false;
    public void HandleGameResultAlt(bool isWin, string playerId)
    {

        Debug.Log("isFinishTriggeredMain" + EightisFinishTriggered);
        if (!EightisFinishTriggered)
        {
            
            "Finish Panel".Show();

            if (!GameModeManager.isAI)
            {

                Rematch.SetActive(true);

                ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, OnSuccess =>
                {
                    Single<int> player = JsonUtility.FromJson<Single<int>>(OnSuccess);
                    FriendRequest.SetActive(player.message.Contains("not"));
                });
                NetworkGameManager.OnRematchRecived += RematchRequestRecived;
                if (isWin)
                {
                    if (playerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);

                        popUpText.text = "Congratulations! Your account has been credited with coins";
                        InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);

                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);

                        popUpText.text = "Your balance has been deducted. Please try again.";
                        InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    }
                }
                else
                {
                    if (playerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        popUpText.text = "Your balance has been deducted. Please try again.";
                        InitializeLoserUI(staticVariables.ProfilePicture, staticVariables.UserProfiledata.user.first_name);
                        InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);

                        ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.OpponetProfile.userId);
                    }
                    else
                    {
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);

                        popUpText.text = "Congratulations! Your account has been credited with coins";
                        InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                    }
                }
            }
            else
            {
                Rematch.SetActive(false);
                FriendRequest.SetActive(false);

                if (isWin)
                {
                    if (playerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name);

                        ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                        GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);

                        popUpText.text = "Congratulations! Your account has been credited with coins";
                        InitializeLoserUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        "YOU WIN".Show();
                    }
                    else
                    {
                        InitializeLoserUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    }
                }
                else
                {
                    if (playerId == staticVariables.UserProfiledata.user._id.ToString())
                    {
                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name);

                        InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                    }
                    else
                    {
                        InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
                        ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");

                        InitializeLoserUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name);

                        "AI WIN".Show();
                        popUpText.text = "Your balance has been deducted. Please try again.";
                    }
                }
            }
        }
    }
    public IEnumerator Init(PlayerDetails winnerName, PlayerDetails looserName, int betAmount, bool IsWin = false)
    {
        //PunNetwork.instance.HasWinnerBeenDeclared = true;
        //SetWinnerLooserProfile(winnerName, looserName);
        //WinplayerNameText.text = winnerName.playerName;
        //looserPlayerNameText.text = looserName.playerName;
        //win.SetActive(IsWin);
        //loose.SetActive(!IsWin);                                                                                                                                                                                          //Photon Removal
        //Confetti(IsWin);
        yield return new WaitForSeconds(1.5f);
        //if (PhotonNetwork.InRoom)
        //{                                                                                                                                                                                                                       //Photon Removal
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    PhotonNetwork.LeaveRoom();
        //}
        popUpText.text = IsWin ? WinMessage : LooseMessage;
    }
    public void SetWinnerLooserProfile(PlayerDetails WinID, PlayerDetails loserId)
    {
        if (WinID.isAI)
        {
            rawProfileImageWinner.texture = SpritesManager.Instance.spritesScriptable.aiImg;
            FriendRequest.SetActive(false);
            Rematch.SetActive(false);
        }
        else
        {
            ServerConnection.DownloadSprite("/" + WinID.PlayerUrl, DownloadedImage =>
            {
                ConstantsData_M.Log("<color=pink>win success</color>");
                rawProfileImageWinner.texture = DownloadedImage;
            },
            OnFailed =>
            {
                rawProfileImageWinner.texture = SpritesManager.Instance.spritesScriptable.nullProfileImg;
                ConstantsData_M.Log("<color=pink>win failedimg</color>");
            });
            ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, OnSuccess =>
            {
                Single<int> player = JsonUtility.FromJson<Single<int>>(OnSuccess);

                if (player.message.Contains("not"))
                {
                    FriendRequest.SetActive(true);
                }
                else
                {
                    FriendRequest.SetActive(false);
                }
            });
        }
        if (loserId.isAI)
        {
            rawProfileImageLooser.texture = SpritesManager.Instance.spritesScriptable.aiImg;
            FriendRequest.SetActive(false);
            Rematch.SetActive(false);
        }
        else
        {
            ServerConnection.DownloadSprite("/" + loserId.PlayerUrl, DownloadedImage =>
            {
                rawProfileImageLooser.texture = DownloadedImage;
                ConstantsData_M.Log("<color=pink>looser success img</color>");
            },
        OnFailed =>
        {
            rawProfileImageLooser.texture = SpritesManager.Instance.spritesScriptable.nullProfileImg;
            ConstantsData_M.Log("<color=pink>failed looser img</color>");
        });
        }
    }
    IEnumerator Coin2Activate_Animation()
    {
        yield return new WaitForSeconds(enable2CoinAnim_wait);
        Coin2.gameObject.SetActive(true);
    }

    public void Confetti(bool isTrue)
    {
        confetti1.gameObject.SetActive(isTrue);
        confetti2.gameObject.SetActive(isTrue);
        firework.gameObject.SetActive(isTrue);
    }
    IEnumerator DecrementBetAmount()
    {
        isDecrementing = true;


        while (betAmount > 0)
        {
            if (betAmount >= 20 && betAmount <= 100)
            {
                betAmount = Mathf.Max(0, betAmount - 5);
            }

            else
            {
                betAmount = Mathf.Max(0, betAmount - 80);
            }

            looserCoinCounter.text = betAmount.ToString();
            yield return new WaitForSeconds(waitSeconds_Counter); // Adjust the delay to control the speed
        }


    }
    IEnumerator InitiatorIncrementor()
    {
        isCompleted = true;
        while (initiator < secondBetAmount)
        {

            if (secondBetAmount > 0 && secondBetAmount <= 100)
            {
                //initiator += 20;
                initiator = Mathf.Min(initiator + 20, secondBetAmount);
            }
            else if (secondBetAmount > 100 && secondBetAmount <= 200)
            {
                //initiator += 35;
                initiator = Mathf.Min(initiator + 35, secondBetAmount);
            }
            else if (secondBetAmount > 200 && secondBetAmount <= 1000)
            {
                //  initiator += 110;
                initiator = Mathf.Min(initiator + 110, secondBetAmount);

            }
            else
            {
                //initiator += 850;
                initiator = Mathf.Min(initiator + 250, secondBetAmount);
            }
            totalBalance = initiator;

            WinnerCoinCounter.text = totalBalance.ToString();
            yield return new WaitForSeconds(waitSeconds_Counter);
        }

    }
    public void ClosePanelFunction()
    {
        SceneLoaderUtility.LoadScene("Home");
    }
    public void SendFriendRequest()
    {
        FriendRequest.SetActive(false);
        ApiAndRoomManager._instance.SendRequestToFriends(staticVariables.OpponetProfile.userId, OnSuccess =>
        {
            Single<int> response = JsonUtility.FromJson<Single<int>>(OnSuccess);
            AndroidUtility._ShowAndroidToastMessage(response.message);

        });
    }
    bool sender;
    public void RematchRequestRecived()
    {
        if (!sender)
        {
            PopupMessageManager.instance.ReconnectionStatus(true, () =>
            {
                NetworkGameManager.Instance.CmdStartRematch();
            }, () =>
            {
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                if (NetworkGameManager.Instance)
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                this.Delay(1, () => NetworkManager.singleton.StopClient());
                SceneManager.LoadScene("Home");
            });

        }
    }
    public void SendRematch()
    {
        sender = true;
        NetworkGameManager.Instance.CmdRequestRematch();
        //if (PhotonNetwork.InRoom)
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;                                                                         //Photon Removal
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    PhotonNetwork.LeaveRoom();
        //}
        // Onleft();
    }


    public void Onleft()
    {
        //PunNetwork.HandleRoomJoined = null;
        //PunNetwork.HandleRoomJoined += OnOpenChallengeCreated;                                                  //Photon Removal
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
            ApiAndRoomManager._instance.ModifyUserBalance();
            Rematch.SetActive(false);
            AndroidUtility._ShowAndroidToastMessage("Rematch Request Send");
        },
        failureMessage =>
        {
            ApiAndRoomManager._instance.DisplayError("failureMessage");

        });
        SoundManagerMain.instance.ClickSoundPlay();
    }

    //    void OnOpenChallengeCreated(Photon.Realtime.Room room)
    //    {
    //        PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);
    //        ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;
    //        ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);                                                                       //Photon Removal
    //        ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    //#if !UNITY_EDITOR
    //        PhotonNetwork.LeaveRoom();
    //#endif
    //    }
}
