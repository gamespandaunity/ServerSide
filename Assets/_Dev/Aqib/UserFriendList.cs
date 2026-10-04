using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ServerConnection;
public class UserFriendList : MonoBehaviour
{
    public DOTweenAnimation OpenmyFriendRequestspanel;
    public Transform uiarrowbutton, uiarrowbutotn2;
    public Transform Container, pendingContainer;
    private List<friendrequestItem> pendingRequests = new List<friendrequestItem>();
    public static List<FriendRequestData> FriendRequests = new List<FriendRequestData>();
    public bool getList = false;
    public static UserFriendList instance;
    public GameObject SearchById;
    public TMP_InputField serchfriendinput;
    public static string storageMyFriends, StoragependingFriend;
    public Button PendingFriendsButton;
    Image PendingFriendsImage;
    public string GoToBackPanelName;

    public static int panelIndex;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        HomeMenuManager.instance.backButtonHeader.onClick.RemoveAllListeners();
        HomeMenuManager.instance.backButtonHeader.onClick.AddListener(Back);
        PendingFriendsImage = PendingFriendsButton.GetComponent<Image>();
    }
    private void OnEnable()
    {
        if (gameObject.name == "Friend Module")
        {

            CreateChallenge_New.inst.isopenreqeust = true;
            CreateChallenge_New.inst.OpenFriendsRequests();
        }
        GetUserFriendsList();
        if (Borderspanel.logoHandler != null)
        {
            Borderspanel.logoHandler(false);
        }
        if (Borderspanel.ChangeTitle != null)
        {
            if (panelIndex == 0)
            {
                Borderspanel.ChangeTitle("FRIEND REQUESTS");
            }
            else
            {
                Borderspanel.ChangeTitle("BORROW FROM FRIEND");
            }
        }
        serchfriendinput.onValueChanged.AddListener(OnFilterFriend);
        HomeMenuManager.instance.backButtonHeader.onClick.RemoveAllListeners();
        HomeMenuManager.instance.backButtonHeader.onClick.AddListener(Back);
    }
    public bool isopenreqeust = false;
    public void Back()
    {
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
        {
            gb.SetActive(gb.name.Equals(GoToBackPanelName));
        }
        SoundManagerMain.instance.ClickSoundPlay();


    }
   
    public void OpenFriendsRequests()
    {
        isopenreqeust = !isopenreqeust;
        if (isopenreqeust)
        {
            for (int i = 0; i < pendingContainer.childCount; i++)
            {
                DestroyImmediate(pendingContainer.GetChild(i).gameObject);
            }
            GetPendingFriendRequests();
            OpenmyFriendRequestspanel.DOPlay();
            uiarrowbutotn2.transform.localScale = new Vector3(1, -1, 1);
            // Color color = new Color(67 / 255.0f, 151 / 255.0f, 192 / 255.0f, 255 / 255.0f);
            if (PendingFriendsImage != null)
                PendingFriendsImage.color = Color.grey;
        }
        else
        {
            OpenmyFriendRequestspanel.DORewind();
            uiarrowbutotn2.transform.localScale = new Vector3(1, 1, 1);
            //  Color color = new Color(68 / 255.0f, 68 / 255.0f,68 / 255.0f, 255 / 255.0f);
            Color color = new Color(67 / 255.0f, 151 / 255.0f, 192 / 255.0f, 255 / 255.0f);
            if (PendingFriendsImage != null)
                PendingFriendsImage.color = color;
            for (int i = 0; i < pendingContainer.childCount; i++)
            {
                DestroyImmediate(pendingContainer.GetChild(i).gameObject);
            }
        }

    }
    public void GetPendingFriendRequests()
    {
        FriendRequests.Clear();
        pendingRequests.Clear();
        StartCoroutine(GetApiRequest(Friend_Requests_Pending_Url() + staticVariables.UserProfiledata.user._id.ToString(),
             OnSuccess =>
             {
                 if (StoragependingFriend != OnSuccess)
                 {
                     for (int i = 0; i < pendingContainer.childCount; i++)
                     {
                         DestroyImmediate(pendingContainer.GetChild(i).gameObject);
                     }
                     FriendsModelParent friendsData = JsonUtility.FromJson<FriendsModelParent>(OnSuccess);
                     GameObject obj = Resources.Load<GameObject>("FriendRequestItem");
                     if (friendsData.status)
                     {
                         //Debug.Log("TotalFriendsListPending=" + friendsData.data.Count);
                         for (int i = 0; i < friendsData.data.Count; i++)
                         {
                             GameObject freinds = Instantiate(obj, pendingContainer);

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
    public void OnFilterFriend(string text)
    {
        List<FriendRequestData> newfilterfriend = FriendRequests.FindAll(friend => friend.frienddata.first_name.ToLower().Contains(text.ToLower()));
        List<FriendsModel> newfilterfriendmodel = new List<FriendsModel>();
        foreach (FriendRequestData data in newfilterfriend)
        {
            newfilterfriendmodel.Add(data.frienddata);
        }
        for (int i = 0; i < Container.childCount; i++)
        {
            Destroy(Container.GetChild(i).gameObject);
        }
        StopCoroutine(DisplayFilterFriends(newfilterfriendmodel));
        StartCoroutine(DisplayFilterFriends(newfilterfriendmodel));
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
        {
            for (int i = 0; i < Container.childCount; i++)
            {
                Destroy(Container.GetChild(i).gameObject);
            }
            GetUserFriendsList();
        }
    }
    IEnumerator DisplayFilterFriends(List<FriendsModel> newfilterfriendmodel)
    {
        yield return new WaitUntil(() => Container.childCount == 0);
        GameObject obj = Resources.Load<GameObject>("friend_List_Prefab");
        for (int i = 0; i < newfilterfriendmodel.Count; i++)
        {
            GameObject freinds = Instantiate(obj, Container);
            FriendsModel frienddata = newfilterfriendmodel[i];
            FriendRequestData data = freinds.GetComponent<FriendRequestData>();
            if (data != null)
            {
                data.Init(frienddata);
            }

        }
    }
    public void GetUserFriendsList()
    {
        pendingRequests.Clear();
        StartCoroutine(GetApiRequest(Friend_List_Url() + staticVariables.UserProfiledata.user._id.ToString(),
    OnSuccess =>
    {
        if (storageMyFriends != OnSuccess || Container.transform.childCount == 0)
        {
            FriendRequests.Clear();

            FriendsModelParent friendsData = JsonUtility.FromJson<FriendsModelParent>(OnSuccess);
            GameObject obj = Resources.Load<GameObject>("friend_List_Prefab");
            storageMyFriends = OnSuccess;
            for (int i = Container.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(Container.transform.GetChild(i).gameObject);
            }
            if (friendsData.status)
            {
                for (int i = 0; i < friendsData.data.Count; i++)
                {
                    GameObject freinds = Instantiate(obj, Container);
                    FriendRequests.Add(freinds.GetComponent<FriendRequestData>());
                    FriendsModel frienddata = friendsData.data[i];
                    FriendRequestData data = freinds.GetComponent<FriendRequestData>();
                    if (data != null)
                    {
                        data.Init(frienddata);
                    }
                }
            }
        }
    }));
    }

    private void OnDisable()
    {
        SearchById?.gameObject.SetActive(false);

        if (gameObject.name == "Friend Module")
        {
            CreateChallenge_New.inst.isopenreqeust = true;
            CreateChallenge_New.inst.OpenFriendsRequests();
        }
        if (Borderspanel.ChangeTitle != null)
        {
            Borderspanel.ChangeTitle("");
        }
        isopenreqeust = false;
        serchfriendinput.text = "";
        serchfriendinput.text = string.Empty;
        OpenmyFriendRequestspanel.DORewind();
        uiarrowbutotn2.transform.localScale = new Vector3(1, 1, 1);
        //  Color color = new Color(68 / 255.0f, 68 / 255.0f,68 / 255.0f, 255 / 255.0f);
        Color color = new Color(67 / 255.0f, 151 / 255.0f, 192 / 255.0f, 255 / 255.0f);
        if (PendingFriendsImage != null)
            PendingFriendsImage.color = color;
        for (int i = 0; i < pendingContainer.childCount; i++)
        {
            DestroyImmediate(pendingContainer.GetChild(i).gameObject);
        }
    }

}
// Models
[Serializable]
public class FriendsModel
{
    public int _id;
    public string first_name;
    public string last_name;
    public string country;
    public string status;
    public string email;
    public string file_url;
    public int request_id;
    public int silver_played;
    public int? silver_win;
    public int gold_played;
    public int? gold_win;
}
[Serializable]
public class FriendsModelParent
{
    public bool status;
    public List<FriendsModel> data;
}
