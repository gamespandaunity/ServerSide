using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static SocketIOUnityAdapter;

public class NotificationUIManager : MonoBehaviour //Photon Removal: MonoBehaviourPunCallbacks
{
    public static NotificationUIManager instance;

    public GameObject acceptNotificationPopUp;
    public GameObject InviteAcceptNotificationPopUp;
    public GameObject SimpleNotification;
    GameObject inviteacceptNotificationObj;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);
        }
    }

   
    GameObject acceptNotificationObj;

    public void AcceptNotificationIns(Firebase.Sample.Messaging.FirebaseHandler.NotificationData notificationBody)
    {
        if (acceptNotificationPopUp != null)
        {
            ConstantsData_M.Log("acceptNotificationPopUp instantiated not null");
        }
        else
        {
            ConstantsData_M.Log("acceptNotificationPopUp instantiated  null");
        }

        // Create a new instance of the prefab each time
        acceptNotificationObj = Instantiate(acceptNotificationPopUp);

        acceptNotificationObj.transform.SetParent(this.transform);
        if (acceptNotificationObj.activeSelf)
        {
            acceptNotificationObj.GetComponent<AcceptNotificationInstance>().setProfile(notificationBody);
        }
        else
        {
            acceptNotificationObj.SetActive(true);
            ConstantsData_M.Log("Instantiated Object name " + acceptNotificationObj.name);
            acceptNotificationObj.GetComponent<AcceptNotificationInstance>().setProfile(notificationBody);
        }
    }
    public void AcceptNotificationIns(GamePacket packet)
    {
        if (acceptNotificationPopUp != null)
        {
            ConstantsData_M.Log("acceptNotificationPopUp instantiated not null");
        }
        else
        {
            ConstantsData_M.Log("acceptNotificationPopUp instantiated  null");
        }

        // Create a new instance of the prefab each time
        acceptNotificationObj = Instantiate(acceptNotificationPopUp);

        acceptNotificationObj.transform.SetParent(this.transform);
        if (acceptNotificationObj.activeSelf)
        {
            acceptNotificationObj.GetComponent<AcceptNotificationInstance>().setProfile(packet);
        }
        else
        {
            acceptNotificationObj.SetActive(true);
            ConstantsData_M.Log("Instantiated Object name " + acceptNotificationObj.name);
            acceptNotificationObj.GetComponent<AcceptNotificationInstance>().setProfile(packet);
        }
    }
}
