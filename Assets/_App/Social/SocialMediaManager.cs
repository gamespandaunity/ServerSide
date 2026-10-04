using NetworkManagement;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SocialMediaManager : MonoBehaviour
{
    string  deepurl="https://12starservices.com/deeplink?param=";
    public InputField BetAmountinput;
    string base64String = "";

    public IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(2);
        UserModel userModel2 = staticVariables.UserProfiledata;
        string paramvalues = JsonUtility.ToJson(new PlayerProfile(userModel2.user.first_name+" "+ userModel2.user.last_name, userModel2.user.file_url,int.Parse(BetAmountinput.text), "1", userModel2.user._id.ToString(),ApiAndRoomManager._instance.challenge_transaction_id, false, userModel2.user.referral_code));  
        byte[] bytesToEncode = Encoding.UTF8.GetBytes(paramvalues);
        base64String = Convert.ToBase64String(bytesToEncode);
        //Debug.Log( paramvalues);
    }
    public void ShareOnWhatsApp()
    {
  
        string shareText = UnityWebRequest.EscapeURL(deepurl+base64String);
        Application.OpenURL("whatsapp://send?text=" + shareText);
    }

    public void ShareViaEmail()
    {
        string emailUrl = "mailto:" +
                          "?subject=" + "Play Game with me" +
                          "&body=" + UnityWebRequest.EscapeURL(deepurl + base64String);
        Application.OpenURL(emailUrl);
    }

// Replace with your desired URL

    public void ShareOnFacebook()
    {
        string facebookShareUrl = "https://www.facebook.com/sharer/sharer.php?u=" + UnityWebRequest.EscapeURL(deepurl + base64String);
        Application.OpenURL(facebookShareUrl);
    }
}
