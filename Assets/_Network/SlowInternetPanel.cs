using UnityEngine;
using UnityEngine.UI;

public class SlowInternetPanel : MonoBehaviour
{
    public Text notificationText;
    public GameObject panel;

    public void PanelStatus(bool status)
    {
        panel.SetActive(status);
        if (status)
        {
            notificationText.text = "Connecting... Your internet connection appears to be slow.";
        }
    }

}