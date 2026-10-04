using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SingleWhatsappContacts : MonoBehaviour
{
    // Start is called before the first frame update
    public TextMeshProUGUI srNumber;
    public Text whatsappContacts;





    public void sendMessageWhatsapp()
    {
        RuntimePlatform platform = Application.platform;
        //print("platformplatformplatformplatform"+ platform);

        if (Application.platform == RuntimePlatform.Android)
        {
            Application.OpenURL("https://api.whatsapp.com/send?phone=" + whatsappContacts.text + "&text=Hi" );
        }

        // iOS
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            Application.OpenURL("whatsapp://send?phone=" + whatsappContacts.text + "&text=Hi");
        }
    }
}
