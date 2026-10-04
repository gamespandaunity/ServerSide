using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class NotificationController : MonoBehaviour {
    public iTweenPositionTo[] ListPos;
    public float timeToMove;
    public Text notificationText;
    public EliminationModeController eliminationModeController;
    public bool showingNotification = false;
    public void showNotification(string msg)
    {
        gameObject.SetActive(true);
        showingNotification = true;

        notificationText.text = msg;
    }

   

    public void OnAnimationEnd()
    {
      
        showingNotification = false;
        ListPos[0].enabled = false;
        ListPos[1].enabled = false;
        eliminationModeController.HandleNotificationEnd();

        gameObject.SetActive(false);
    }

    // Use this for initialization
    void OnEnable () {
        ListPos[0].enabled = true;
        ListPos[1].enabled = false;
        Invoke("MoveForward", timeToMove);

    }

    // Update is called once per frame
    void MoveForward () {
        ListPos[1].enabled = true;
        ListPos[0].enabled = false;
    }
}
