using UnityEngine;
using UnityEngine.UI;

public class InfoManager : MonoBehaviour 
{
    private static InfoManager[] instances;
    private Component sender;
    private string message;
    [SerializeField] private string infoName;
    [SerializeField] private Text infoText;

    void Awake()
    {
        instances = InfoManager.FindObjectsOfType<InfoManager>();
        transform.GetChild(0).gameObject.SetActive(false);
    }
    public static void Open(string infoName, Component sender, string message, string info = "")
    {
        foreach (var item in instances)
        {
            if (item.infoName == infoName)
            {
                item.OpenWindow(sender, message, info);
                return;
            }
        }
        ConstantsData_M.LogInfo("There is no InfoManager with name " + infoName);
    }
    private void OpenWindow(Component sender, string message, string info = "")
    {
        this.sender = sender;
        this.message = message;
        if (infoText != null && !string.IsNullOrEmpty(info))
        {
            infoText.text = info;
        }
        transform.GetChild(0).gameObject.SetActive(true);
    }
    public void CLose()
    {
        if (sender && !string.IsNullOrEmpty(message))
        {
            sender.SendMessage(message);
        }
        transform.GetChild(0).gameObject.SetActive(false);
    }
}
