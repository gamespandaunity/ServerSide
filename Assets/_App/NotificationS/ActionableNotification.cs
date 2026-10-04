using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionableNotification : MonoBehaviour
{
    public NotificationData item;
    public Button Accept, Reject;
    public NotificationSystemBell notificationSystemBell;
    public void Init(NotificationData item)
    {
        this.item = item;
        transform.Find("Title").GetComponent<Text>().text = item.title;
        transform.Find("Body").GetComponent<Text>().text = item.message;
        Accept.onClick.RemoveAllListeners();
        Reject.onClick.RemoveAllListeners();
        if (item.type == "friend_request")
        {
      
            Accept.onClick.AddListener(AcceptFriendRequest);
            Reject.onClick.AddListener(RejectFriendRequest);
           
        }
       
    }
    public void AcceptFriendRequest()
    {
        ApiAndRoomManager._instance.ApproveFriendRequest(item.data._id);
        notificationSystemBell.DeleteNotification(item._id.ToString());
        Destroy(gameObject);
    }
    public void RejectFriendRequest()
    {
        ApiAndRoomManager._instance.DeclineFriendRequest(item.data._id);
        notificationSystemBell.DeleteNotification(item._id.ToString());

        Destroy(gameObject);
    }
 
}
