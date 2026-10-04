
using DG.Tweening;
using NetworkManagement;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ServerConnection;

public class CreateChallenge_New : MonoBehaviour
{
    public static CreateChallenge_New inst;

    public DOTweenAnimation OpenmyFriendRequestspanel;
    public Transform uiarrowbutton, uiarrowbutotn2;

    public Image CreateChallengeBtn;
    public Sprite Gold, Silver;
    public TMP_Text tmp_title;
    public GameObject topHeader;
    public GameObject DeleteChallengePopUP, browseChallengePopUp;
    [Header("FRIENDS DATA")]
    private List<friendrequestItem> pendingRequests = new List<friendrequestItem>();
    public static List<FriendRequestData> FriendRequests = new List<FriendRequestData>();
    public Transform MyFriendsContainer, pendingFriendsContainer;
    public static string storageMyFriends, StoragependingFriend;
    public string GoToBackPanelName;
    public GameObject CreateRoomAnimations;
    public GameObject privateChallengepanel;

    private void Awake()
    {
        inst = this;
    }

    public void Back()
    {
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
            gb.SetActive(gb.name.Equals(GoToBackPanelName));
        SoundManagerMain.instance.ClickSoundPlay();
    }

    public Sprite GoldBG;
    public Sprite SilverBG;
    public Image OpenChallenge;
    public Image FriendChallenge;
    public Image FirendList;

    private void OnEnable()
    {
        if (Borderspanel.logoHandler != null)
            Borderspanel.logoHandler(true);
        if (Borderspanel.coinSelected != null)
            Borderspanel.coinSelected(true);

        if (staticVariables.isgoldcoins)
        {
            OpenChallenge.sprite = GoldBG;
            FriendChallenge.sprite = GoldBG;
            FirendList.sprite = GoldBG;
        }
        else
        {
            OpenChallenge.sprite = SilverBG;
            FriendChallenge.sprite = SilverBG;
            FirendList.sprite = SilverBG;
        }

        UIMainMenManager.instance.currentPanelState = UIMainMenManager.PanelState.challengeBorrowState;
        topHeader.gameObject.SetActive(true);
        HomeMenuManager.instance.backButtonHeader.onClick.RemoveAllListeners();
        HomeMenuManager.instance.backButtonHeader.onClick.AddListener(Back);
    }

    #region CREATE CHALLENGE & OPEN CHALLENGE

    bool isopen = false;
    public void OpenMyChallenges()
    {
        isopen = !isopen;
        uiarrowbutton.transform.localScale = isopen ? new Vector3(1, -1, 1) : new Vector3(1, 1, 1);
    }

    public bool isopenreqeust = false;
    public void OpenFriendsRequests()
    {
        isopenreqeust = !isopenreqeust;
        if (isopenreqeust)
        {
            OpenmyFriendRequestspanel.DOPlay();
            uiarrowbutotn2.transform.localScale = new Vector3(1, -1, 1);
        }
        else
        {
            OpenmyFriendRequestspanel.DORewind();
            uiarrowbutotn2.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public void CreateRoom()
    {
        CreateChallengeBtn.gameObject.SetActive(false);
        CreateRoomAnimations.SetActive(true);
        SoundManagerMain.instance?.ClickSoundPlay();

        HomeMenuManager.instance.challengePrizeInput.text = EightBallPoolNetworkManager.mainPlayer.prize + "";
        staticVariables.betEnteredAmount = int.Parse(HomeMenuManager.instance.challengePrizeInput.text);
        staticVariables.currentPrize = int.Parse(HomeMenuManager.instance.challengePrizeInput.text);
        staticVariables.currentPrize.Show("Created prize");

        Dictionary<string, string> betRequestDict = new Dictionary<string, string>
        {
            ["first_player"] = staticVariables.UserProfiledata.user._id.ToString(),
            ["second_player"] = "",
            ["game_id"] = ApiAndRoomManager.currentGameId.ToString(),
            ["remark"] = "Multipler with Golden Coins",
            ["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver",
            ["screenstatus"] = staticVariables.screenstatus,
            ["region"] = "au"
        };

        if (ApiAndRoomManager.currentGameId == 13)
        {
            SelectOver.SelectedOver.Show("On Create Over");
            betRequestDict["overs"] = SelectOver.SelectedOver.ToString();
        }

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

        ApiAndRoomManager._instance.PlaceChallenge(betRequestDict,
        successMessage =>
        {
            Single<ChallengeStatus> challengeInfo =
                JsonConvert.DeserializeObject<Single<ChallengeStatus>>(successMessage);

            successMessage.Show("Response Place Challenge");
            ApiAndRoomManager._instance.ModifyUserBalance();
            HomeMenuManager.instance.createChallengePanelParent.SetActive(false);
            CreateChallengeBtn.gameObject.SetActive(true);
            CreateRoomAnimations.SetActive(false);
            HomeMenuManager.instance.MenuChallengePanelParent.SetActive(true);
            GameObject parent = HomeMenuManager.instance.MenuChallengePanelParent;
            parent.transform.GetChild(0).gameObject.SetActive(false);
            parent.transform.GetChild(1).gameObject.SetActive(true);

            challengeInfo.data.transaction_id.Show("Challenge info");

            // Always deploy on Edgegap — isCreator check was only for local testing
            MirrorNetwork.Instance.edgegapAPIClient.Deploy(
                challengeInfo.data.transaction_id,
                staticVariables.UserProfiledata.user._id.ToString());
        },
        failureMessage =>
        {
            HomeMenuManager.instance.MenuChallengePanelParent.SetActive(true);
            GameObject parent = HomeMenuManager.instance.MenuChallengePanelParent;
            parent.transform.GetChild(0).gameObject.SetActive(false);
            parent.transform.GetChild(1).gameObject.SetActive(true);
            CreateChallengeBtn.gameObject.SetActive(true);
            CreateRoomAnimations.SetActive(false);
        });
    }

    [SerializeField] TMP_InputField PlayerIDInput;
    [SerializeField] TMP_InputField prizeInput;

    public void CreatePrivateRoom()
    {
        SoundManagerMain.instance.ClickSoundPlay();

        string category = staticVariables.All_games_Ref
            .Find(x => x.game_id == ApiAndRoomManager.currentGameId).category;
        if (category.Contains("Games"))
            EightBallPoolNetworkManager.mainPlayer.prize = int.Parse(prizeInput.text);

        staticVariables.screenstatus = "sad";

        Dictionary<string, string> betRequestDict = new Dictionary<string, string>
        {
            ["first_player"] = staticVariables.UserProfiledata.user._id.ToString(),
            ["second_player"] = PlayerIDInput.text,
            ["game_id"] = ApiAndRoomManager.currentGameId.ToString(),
            ["remark"] = "Multipler with Golden Coins",
            ["screenstatus"] = staticVariables.screenstatus,
            ["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver"
        };

        if (ApiAndRoomManager.currentGameId == 13)
            betRequestDict["overs"] = SelectOver.SelectedOver.ToString();

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
            Single<ChallengeStatus> challengeInfo =
                JsonConvert.DeserializeObject<Single<ChallengeStatus>>(successMessage);

            ApiAndRoomManager._instance.ModifyUserBalance();
            RoomsListManager.instance.iscreatedChallangebtn = true;
            HomeMenuManager.instance.MenuChallengePanelParent.SetActive(true);
            GameObject parent = HomeMenuManager.instance.MenuChallengePanelParent;
            parent.transform.GetChild(0).gameObject.SetActive(false);
            parent.transform.GetChild(1).gameObject.SetActive(true);
            privateChallengepanel.SetActive(false);

            ConstantsData_M.Log("Bet ID");

            // Always deploy on Edgegap — isCreator check was only for local testing
            MirrorNetwork.Instance.edgegapAPIClient.Deploy(
                challengeInfo.data.transaction_id,
                staticVariables.UserProfiledata.user._id.ToString());
        },
        failureMessage =>
        {
            ApiAndRoomManager._instance.DisplayError("failureMessage");
            HomeMenuManager.instance.MenuChallengePanelParent.SetActive(true);
            GameObject parent = HomeMenuManager.instance.MenuChallengePanelParent;
            parent.transform.GetChild(0).gameObject.SetActive(false);
            parent.transform.GetChild(1).gameObject.SetActive(true);
            privateChallengepanel.SetActive(false);
        });
    }

    [SerializeField] private Button PublicChallengeBtn, PrivateChallengeBtn;
    public Sprite SelectChallange, UnSelectChallange;

    public void PublicChallenge()
    {
        PublicChallengeBtn.GetComponent<Image>().sprite = SelectChallange;
        PrivateChallengeBtn.GetComponent<Image>().sprite = UnSelectChallange;
        SocketIOUnityAdapter.instance.RenderPublicRoomTables();
    }

    public void PrivateChallenge()
    {
        PrivateChallengeBtn.GetComponent<Image>().sprite = SelectChallange;
        PublicChallengeBtn.GetComponent<Image>().sprite = UnSelectChallange;
        SocketIOUnityAdapter.instance.RenderPrivateRoomTables();
    }

    #endregion

    #region USER FRIENDS LIST

    public void GetPendingFriendRequests()
    {
        StartCoroutine(GetApiRequest(
            Friend_Requests_Pending_Url() + staticVariables.UserProfiledata.user._id.ToString(),
            OnSuccess =>
            {
                if (StoragependingFriend != OnSuccess)
                {
                    for (int i = 0; i < pendingFriendsContainer.childCount; i++)
                        DestroyImmediate(pendingFriendsContainer.GetChild(i).gameObject);

                    FriendsModelParent friendsData = JsonUtility.FromJson<FriendsModelParent>(OnSuccess);
                    GameObject obj = Resources.Load<GameObject>("FriendRequestItem");

                    if (friendsData.status)
                    {
                        for (int i = 0; i < friendsData.data.Count; i++)
                        {
                            GameObject freinds = Instantiate(obj, pendingFriendsContainer);
                            pendingRequests.Add(freinds.GetComponent<friendrequestItem>());
                            friendrequestItem data = freinds.GetComponent<friendrequestItem>();
                            if (data != null)
                            {
                                data.ChallengeText.text = friendsData.data[i].first_name + " " + friendsData.data[i].last_name;
                                data.Init(friendsData.data[i].request_id.ToString(), friendsData.data[i].file_url);
                            }
                        }
                    }
                }
                ApiAndRoomManager._instance.LoadingObject.gameObject.SetActive(false);
            }));
    }

    public void GetUserFriendsList()
    {
        StartCoroutine(GetApiRequest(
            Friend_List_Url() + staticVariables.UserProfiledata.user._id.ToString(),
            OnSuccess =>
            {
                if (storageMyFriends != OnSuccess || MyFriendsContainer.transform.childCount == 0)
                {
                    FriendsModelParent friendsData = JsonUtility.FromJson<FriendsModelParent>(OnSuccess);
                    GameObject obj = Resources.Load<GameObject>("friend_List_Prefab");
                    storageMyFriends = OnSuccess;

                    for (int i = MyFriendsContainer.transform.childCount - 1; i >= 0; i--)
                        Destroy(MyFriendsContainer.transform.GetChild(i).gameObject);

                    if (friendsData.status)
                    {
                        for (int i = 0; i < friendsData.data.Count; i++)
                        {
                            GameObject freinds = Instantiate(obj, MyFriendsContainer);
                            FriendRequests.Add(freinds.GetComponent<FriendRequestData>());
                            FriendRequestData data = freinds.GetComponent<FriendRequestData>();
                            if (data != null)
                                data.Init(friendsData.data[i]);
                        }
                    }
                }
            }));
    }

    #endregion

    private void OnDisable()
    {
        if (Borderspanel.logoHandler != null)
            Borderspanel.logoHandler(true);
        HomeMenuManager.instance.createChallengePanelParent.SetActive(false);
        DeleteChallengePopUP.gameObject.SetActive(false);
        browseChallengePopUp.gameObject.SetActive(false);
    }
}
