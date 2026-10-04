using Extensions.Unity.ImageLoader;
//using Firebase.Database;
using Firebase.Sample.Messaging;
using NetworkManagement;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SocketIOUnityAdapter;


/// <summary>
/// The player profile UI.
/// </summary>
public class PlayerProfileUI : MonoBehaviour
{
    public Image avatarImage;
    [SerializeField] private TextMeshProUGUI userName;
    [SerializeField] private TextMeshProUGUI silverBet;
    public Image bgimage, coinimage;
    public Sprite goldcoin, silvercoin;
    public TextMeshProUGUI ChallengeText;
    //[SerializeField] private TextMeshProUGUI waitingOpponent;
    public Button joinBet, deleteBet;
    private float waitingProgress;
    private int waitingProgressInt;
    private float orient = 1.0f;
    private bool isFirstTimeSet = true;

    private string Id;
    public TextMeshProUGUI ChallengeTypeText, Overs;

    public Image isonlineimg;

    public Room room;
    public Requests challengeRequests;

    public RoomData roomData;
    private void OnEnable()
    {
        if (staticVariables.isgoldcoins)
        {
            ChallengeTypeText.text = "Gold match";
            //Color golden = new Color(255f / 255f, 215f / 255f, 0f / 255f);
            //ChallengeTypeText.color =  golden;
            coinimage.sprite = goldcoin;

        }
        else
        {
            ChallengeTypeText.text = "Silver match";
            //Color silver = new Color(192f / 255f, 192f / 255f, 192f / 255f);
            //ChallengeTypeText.color = silver;
            coinimage.sprite = silvercoin;
        }
    }


    public async void Initialize(RoomData data)
    {
        roomData = data;
        Id = data.player_info_id.ToString();
        userName.text = data.player_info_name;
        silverBet.text = "" + data.bet_amount;
        if (ChallengeText) ChallengeText.text = data.bet_type == "gold" ? "Gold Challenge" : "Silver Challenge";
        coinimage.sprite = data.bet_type == "gold" ? goldcoin : silvercoin;
        bgimage.color = data.bet_type == "gold" ? new Color32(255, 190, 0, 255) : new Color32(174, 249, 255, 255);
        //bgimage.GetComponent<UIShadow>().effectColor = data.bet_type == "gold" ? new Color32(255, 200, 0, 255) : new Color32(174, 255, 255, 255);

        if (joinBet)
        {
            joinBet.onClick.AddListener(() =>
            {
                RoomsListManager.instance.currentSelectedRoom = data;
                SocketIOUnityAdapter.transactionId = data.transaction_id;
                UserModel userModel2 = staticVariables.UserProfiledata;
                if (userModel2.user.silver_balance <= data.bet_amount)
                {
                    PopupMessageManager.instance.ShowPopUp("Not enough Coins");
                }
                else
                {
                    HomeMenuManager.instance.browseChallengePopup.SetActive(true);
                }
            });
        }

        if (deleteBet)
        {
            deleteBet.onClick.AddListener(() =>
            {
                PopupMessageManager.instance.ShowConfirmPanel("Are you sure you want to delete this challenge?", () =>
                {
                    ApiAndRoomManager._instance.DeleteChallenge(data.transaction_id, OnSuccess =>
                    {
                        OnSuccess.Show("Bet Deleted");
                        Destroy(this.gameObject);
                    });

                }, "Delete");
            });
        }
        //if (roomData.profilePic == null)
        //{
        //    ServerConnection.DownloadSprite("/" + data.player_info_snap, DownloadedTexture =>
        //    {
        //        if (avatarImage)
        //            avatarImage.sprite = ConstantsData_M.ConvertTextureToSprite(DownloadedTexture);// scriptable.nullProfileImg;
        //        foreach (var room in SocketIOUnityAdapter.instance.publicRooms)
        //        {
        //            if (room.transaction_id == data.transaction_id)
        //            {
        //                room.profilePic = avatarImage.sprite;
        //                Debug.Log("Updated one room...");
        //            }
        //        }
        //        foreach (var room in SocketIOUnityAdapter.instance.privateRooms)
        //        {
        //            if (room.transaction_id == data.transaction_id)
        //            {
        //                room.profilePic = avatarImage.sprite;
        //                Debug.Log("Updated one room...");
        //            }
        //        }

        //    }, OnFailed =>
        //    {
        //        if (avatarImage)
        //            avatarImage.sprite = ConstantsData_M.ConvertTextureToSprite(SpritesManager.Instance.spritesScriptable.nullProfileImg);

        //    });
        //}
        //else
        //{
        //    if (avatarImage)
        //        avatarImage.sprite = roomData.profilePic;
        // }
        //await ImageLoader.LoadSprite(ServerConnection.Main_URL() + "/" + data.player_info_snap).Consume(avatarImage);
        //DatabaseReference databaseRef = FirebaseDatabase.DefaultInstance.GetReference("users/" + int.Parse(Id));

        //databaseRef.ChildAdded += HandleChildAdded;
        //databaseRef.ChildChanged += HandleChildAdded;
    }
    void Update()
    {

        waitingProgress += 2.0f * orient * Time.deltaTime;

        int currentWaitingProgressInt = (int)waitingProgress;
        if (waitingProgress > 3.49f || waitingProgress < -0.49f)
        {
            orient *= -1.0f;
        }
        if (waitingProgressInt != currentWaitingProgressInt)
        {
            waitingProgressInt = currentWaitingProgressInt;
            string txt = "Waiting";
            for (int i = 1; i <= waitingProgressInt; i++)
            {
                txt += ".";
            }
        }
    }
    public NetworkManagement.PlayerProfile player;


    public async void SetPlayer(Room room)
    {

        if (isFirstTimeSet)
        {
            isFirstTimeSet = false;
        }
        this.player = room.mainPlayer;
        if (player == null)
        {
            enabled = true;
            avatarImage.sprite = ConstantsData_M.ConvertTextureToSprite(SpritesManager.Instance.spritesScriptable.nullProfileImg);
            userName.text = "";
            silverBet.text = "";
            return;
        }
        else
        {
            //"uploads/1715067882866-7.jpg",
            userName.text = player.userName;


            silverBet.text = "" + player.prize + "";

            if (ApiAndRoomManager.currentGameId == 13)
            {
                Overs.text = "OVERS: " + (player.overIndex == 0 ? 3 : 5);
            }
            Id = player.userId;

            if (bgimage)
            {
                bgimage.color = player.isgoldcoins == false ? new Color32(174, 249, 255, 255) : new Color32(255, 190, 0, 255);
                //  bgimage.GetComponent<UIShadow>().effectColor = player.isgoldcoins == false ? new Color32(174, 255, 255, 255) : new Color32(255, 200, 0, 255);
            }
            if (coinimage)
            {
                coinimage.sprite = player.isgoldcoins ? goldcoin : silvercoin;

            }
            if (ChallengeText)
            {
                ChallengeText.text = player.isgoldcoins ? "Gold Challenge" : "Silver Challenge";
            }
            if (gameObject.activeInHierarchy)
            {
                OnEnable();
            }

        }
        try
        {
            //await FirebaseHandler._instance.GetPlayerOnlineStatusAsync(int.Parse(Id), (isonline) =>
            //{
            //    isonlineimg.color = isonline ? Color.green : Color.red;
            //});
        }
        catch (Exception e)
        {
        }
        //DatabaseReference databaseRef = FirebaseDatabase.DefaultInstance.GetReference("users/" + int.Parse(Id));

        //databaseRef.ChildAdded += HandleChildAdded;
        //databaseRef.ChildChanged += HandleChildAdded;

        //  enabled = false;

        //StartCoroutine(ServerCall.DownloadImage($"/uploads/{Id}.jpg", DownImage =>
        //{
        //    avatarImage.texture = DownImage;
        //}));
    }

    //void HandleChildAdded(object sender, ChildChangedEventArgs args)
    //{
    //    try
    //    {
    //        if (SceneManager.GetActiveScene().name == "Home")
    //        {
    //            if (args.DatabaseError != null)
    //            {
    //                ConstantsData_M.Log(args.DatabaseError.Message);
    //                return;
    //            }

    //            if (args.Snapshot.Exists && args.Snapshot.Value != null)
    //            {
    //                bool online = bool.Parse(args.Snapshot.Value.ToString());

    //                // Update the image color based on the online status
    //                isonlineimg.color = online ? Color.green : Color.red;
    //            }
    //            else
    //            {
    //                ConstantsData_M.LogInfo("Snapshot does not contain a valid 'online' value.");
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {

    //    }
    //}

    public void CleanupListeners()
    {
        if (joinBet != null)
        {
            joinBet.onClick.RemoveAllListeners();
        }

        if (deleteBet != null)
        {
            deleteBet.onClick.RemoveAllListeners();
        }
    }
    public void destroyThisRoom()
    {
        SocketIOUnityAdapter.instance.RejectedChallengesTransactionId.Add(roomData.transaction_id);
        Destroy(this.gameObject);
    }
    [Header("----REQUESTS----")]
    public GameObject ChallengeItemPrefab;
    public void GetRequests()
    {
        UIMainMenManager.instance.gameRequestPanel.SetActive(true);
        RoomsListManager.instance.CreateRequestPanelImage.sprite = SpritesManager.Instance.spritesScriptable.GameLogos[roomData.game_id - 1];
        RoomsListManager.instance.ChallengeRequestContent.Clear();
        ApiAndRoomManager._instance.GetChallengesRequests(roomData.transaction_id, OnSuccess =>
        {
            OnSuccess.Show("Requests");
            challengeRequests = JsonUtility.FromJson<Requests>(OnSuccess);
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
[Serializable]
public class Request
{
    public int id;
    public string user_id;
    public string transaction_id;
    public DateTime createdAt;
    public string status;
    public string first_name;
    public string last_name;
    public string email;
    public string phone;
    public string file_url;
    public int silver_balance;
    public int gold_balance;
    public int game_id;
    public string bet_type;
    public int bet_amount;
    public string bet_expires_sec;
    public string notification_type;
    public string region;
}
[Serializable]
public class Requests
{
    public bool status;
    public string message;
    public List<Request> data;
}
