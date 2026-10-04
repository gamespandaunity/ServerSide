using System;
using System.Collections;
using System.Collections.Generic;
//using TeenPattiGame;
using UnityEngine;
using UnityEngine.UI;
using static ServerConnection;
public class NotificationSystemBell : MonoBehaviour
{
    public GameObject NotificaionPanel;
    public Transform NotificaionContent;
    public Text UnreadCount;
    public static NotificationSystemBell _;
    public void Awake()
    {
        _ = this;
    }
    private void OnDisable()
    {
        _ = null;
    }
    void OnEnable()
    {
        _ = this;
        FetchNotification();

    }
    public void ToggleNotificationPanel()
    {
        NotificaionPanel.SetActive(!NotificaionPanel.activeSelf);
        if (NotificaionPanel.activeSelf)
        {
            StartCoroutine(GetApiRequest(Notification_List_Url() + 1, OnSucess =>
            {
                NotificationResponse notificationData = JsonUtility.FromJson<NotificationResponse>(OnSucess);

                foreach (Transform child in NotificaionContent)
                    Destroy(child.gameObject);
                foreach (var item in notificationData.data)
                {
                    if (item.type == "friend_request"|| (item.type == "borrow_request"))
                    {
                        GameObject notificationobj = Instantiate(Resources.Load("NotificationItemAction"), NotificaionContent) as GameObject;
                        ActionableNotification actionnotification = notificationobj.GetComponent<ActionableNotification>();
                        actionnotification.notificationSystemBell = this;
                        actionnotification.Init(item);
                        if (item.type == "borrow_request")
                        {
                            ApiAndRoomManager._instance.ModifyUserBalance();
                        }
                    }

                    else
                    {
                        GameObject notificationobj = Instantiate(Resources.Load("NotificationItem"), NotificaionContent) as GameObject;
                        notificationobj.transform.Find("Title").GetComponent<Text>().text = item.title;
                        notificationobj.transform.Find("Body").GetComponent<Text>().text = item.message;
                    }
                    if (item.is_read == 0)
                    {
                        StartCoroutine(GetApiRequest(Notification_Read_Url() + item._id));
                    }
                }
                UnreadCount.text = "0";
                UnreadCount.transform.parent.gameObject.SetActive(false);

            }, OnFailed =>
            {

            }));
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void DeleteNotification(string id)
    {
        StartCoroutine(GetApiRequest(Notification_Delete_Url() + id));
    }
    public void FetchNotification()
    {
        StartCoroutine(GetApiRequest(Notification_List_Url() + 1, OnSucess =>
        {
            NotificationResponse notificationData = JsonUtility.FromJson<NotificationResponse>(OnSucess);
            foreach (Transform child in NotificaionContent)
                DestroyImmediate(child.gameObject);
            UnreadCount.text = notificationData.data.FindAll(item => item.is_read == 0).Count.ToString();
            if (UnreadCount.text == "0") UnreadCount.transform.parent.gameObject.SetActive(false);

            foreach (var item in notificationData.data)
            {
                if (item.type == "friend_request" || (item.type == "borrow_request"))
                {
                    GameObject notificationobj = Instantiate(Resources.Load("NotificationItemAction"), NotificaionContent) as GameObject;
                    ActionableNotification actionnotification = notificationobj.GetComponent<ActionableNotification>();
                    actionnotification.notificationSystemBell = this;
                    actionnotification.Init(item);
                }

                else
                {
                    GameObject notificationobj = Instantiate(Resources.Load("NotificationItem"), NotificaionContent) as GameObject;
                    notificationobj.transform.Find("Title").GetComponent<Text>().text = item.title;
                    notificationobj.transform.Find("Body").GetComponent<Text>().text = item.message;
                }
                //if (item.is_read == 0)
                //{
                //    StartCoroutine(GetRequest(Notification_Read_Url + item._id)); 
                //}
            }
        }, OnFailed =>
        {

        }));
    }
}
[Serializable]
public class NotificationData
{
    public int _id;
    public string user_id;
    public string title;
    public string message;
    public string type;
    public Data data;
    public int is_read;
    public DateTime updatedAt;
    public DateTime createdAt;

    [Serializable]

    public class Data
    {
        public string notification_type;
        public string _id;
        public string transaction_id;
        public string file_url;
        public string first_name;
    }
}

[Serializable]

public class NotificationResponse
{
    public List<NotificationData> data;
    public string currentPage;
    public int total_pages;
    public string per_page;
    public int total_count;
}