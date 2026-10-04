using NetworkManagement;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class DeepLinkManager : MonoBehaviour
{
    public static DeepLinkManager Instance { get; private set; }

    private string deeplinkURL;


    private void Awake()
    {


        if (Instance == null)
        {
            Instance = this;
            //       onDeepLinkActivated(testdeeplink);
            Application.deepLinkActivated += onDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                onDeepLinkActivated(Application.absoluteURL);
            }
            // Initialize DeepLink Manager global variable.
            else
                deeplinkURL = "[none]";
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        if (string.IsNullOrEmpty(Application.absoluteURL))
        {
            //LoadingManager.instance.timerLoader();

        }
    }



    private void onDeepLinkActivated(string url)
    {
        //Debug.Log("Doing Cold Start With DeepLink" + url);
        // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
        deeplinkURL = url;
        string paramatersvalue = url.Split('?')[1];
        if (paramatersvalue != null)
        {
            byte[] bytesToDecode = Convert.FromBase64String(paramatersvalue);

            // Convert the bytes back to string
            string decodedString = Encoding.UTF8.GetString(bytesToDecode);
            staticVariables.decodeddeeplink = decodedString;
            PlayerProfile DeeplinkUserProfile = JsonUtility.FromJson<PlayerProfile>(decodedString);
            ConstantsData_M.Log("deep links" + decodedString);
            staticVariables.isfromreferllinks = true;
            staticVariables.invitedpersonProfile = DeeplinkUserProfile;
            staticVariables.isgoldcoins = DeeplinkUserProfile.isgoldcoins;
            staticVariables.referralCode = DeeplinkUserProfile.playerTTL;
            LoadingManager.instance.timerLoader();
            //    if (validScene) SceneLoaderUtility.LoadScene(sceneName);
        }
        else
        {
            LoadingManager.instance.timerLoader();
        }

    }


}
