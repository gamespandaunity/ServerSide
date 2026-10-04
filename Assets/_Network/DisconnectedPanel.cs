using UnityEngine;
using UnityEngine.UI;

public class DisconnectedPanel : MonoBehaviour
{
    public Text notificationText;
    public GameObject panel;

    public void PanelStatus(bool status)
    {
        panel.SetActive(status);
        if (status)
        {
            notificationText.text = "Reconnecting... Your internet connection is Disconnected.";
        }
    }

}