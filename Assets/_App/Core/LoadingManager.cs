using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Android;
using System.Runtime.InteropServices;
using System;
using UnityCipher;
using System.Collections.Generic;
//using Google.Play.AppUpdate;
//using Google.Play.Common;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Mirror;
public class LoadingManager : MonoBehaviour
{
    //public float loadingTime;
    public Image fillImage;
    public static LoadingManager instance;

    [DllImport("AdmobMed")]
    private static extern IntPtr Greeting();
    [DllImport("AdmobMed")]
    private static extern IntPtr Greetingrequest();
    [DllImport("AdmobMed")]
    private static extern IntPtr AdmobRequest(string key);


    private bool isUpdateAvailable;
    private Text inAppStatus;
    //private AppUpdateManager appUpdateManager = new AppUpdateManager();
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);
            staticVariables.uniqueGameIdentifier = SystemInfo.deviceUniqueIdentifier;
        }
        if (PlayerPrefs.HasKey("Sound"))
            AudioListener.volume = PlayerPrefs.GetInt("Sound");

    }
    private void Start()
    {
        if (Screen.orientation == ScreenOrientation.Portrait)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        if (Utils.IsHeadless() || !Utils.IsAndroid())
        {
            ServerConnection.secrets.Add("apk_signature_black_arch", "077586297b5a8145c6317f821a5cc23d390944c87de714c65de476712f9adca7");
            StartCoroutine(Timer());
        }
    }
    public void StartDecryption(string key)
    {

        string keypair = $"<RSAKeyValue><{fillImage.gameObject.name}>kQz56iCo/Y9ifAOBouNtUvqQUv7xW5841cxFEGtORaMcszAikZ+JL7NphtEkifWPnOOYNeHs2jYWLAjkvtgY3yiR2by89LqlR1T6d9LXdREUvhALlZo7enQ8OR/aLhLtWWX1/iZp6DOCXtA2rMVLNmJyC/enu491iilwVUJYypE=</{fillImage.gameObject.name}><Exponent>AQAB</Exponent><P>kYu7rrvNn4dFeB7XFTehK0ME7cv38pWo+dFd6o9V+tTyEm8VMbg9s/eCShZGZTl8By3+rypr9xWUoiGea0EcRw==</P><Q>/yEMOqEiEeDGSzcNdm9lWLtdOy9HS8BDry4HMoelW7rtd74nvxvp/RQYrqnArEpqjUXqgRfVSz2oSKS5q71GZw==</Q><DP>OILOvouAzR4SpQ8kZ8KPu2JsOYBmpzPgxg66rmY09g1UaD/lNMbaflPtrKJ1drwZIhiDuLN59BfW/BSdk/tV5Q==</DP><DQ>wMHMRoO1hQJpbyyB/Gh/jsHI182JtV7nBHTnNTMtKRgbEGxYBVjmubI0T8qrasCyrlgTrENAgJ9uBKyIJ4xSow==</DQ><InverseQ>gLi34UxCVGv+9Rys1HxwLHwQwemlTJnvZ2DhQdINOUt7VRzpqFPCpsImhPsbjCn+DFUjdOS+PoNbc9Otk69w9w==</InverseQ><D>ezeCtRi5dWvwRVjVLwHzRKIFJ/vaG/LN3RCBOsls3EsLfkxNkVbsutC1vAi9+VPYg3XzPxKEmXmIrC+B4XzA4ry9or7kumJ22buHCnVW9xJzA+7tOe8muaUELzWJi/fnkDTb/QUACWDKcuL9rxVhIrHiSLkJwzLzCJtK2/2P9uE=</D></RSAKeyValue>";

        string encryptedText = key; // ye dalni ha firebase pa. ok

        string decryptedText = Decrypt(encryptedText, keypair);
        ServerConnection.secrets.Add("apk_signature_black_arch", decryptedText);
        LoadingManager.instance.timerLoader();
        Debug.Log("Decrypted Message: " + decryptedText);
        ////Debug.Log("Base URL: " + baseUrl);
    }
    public static string Decrypt(string encryptedText, string privateKeyXml)
    {
        try
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKeyXml);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, false);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
        catch (Exception ex)
        {
            ConstantsData_M.Log("RSA Decryption Error: " + ex.Message);
            return null;
        }
    }
    void Update()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount += Time.deltaTime / 2;
        }
    }



    public void timerLoader()
    {
        //  StartCoroutine(CheckForUpdate());
        //APIManager.instance.GetVersionNumber(OnSuccess =>
        //{
        //    Single<int> response = JsonUtility.FromJson<Single<int>>(OnSuccess);
        //    if (response.status)
        //    {
        //        PopupMessageManager.instance.ShowConfirmPanel("New update is available, please download to continue", yesButtonText: "Update", noButtonStatus: false, methodToInvoke: () =>
        //            {
        //                Application.OpenURL("https://play.google.com/store/apps/details?id=com.games.panda.au");
        //                Application.Quit();
        //            });

        //    }
        //    else
        //    {
        StartCoroutine(Timer());
        //    }
        //});
    }



    IEnumerator Timer()
    {
        //if (PlayerPrefs.GetString("GuestId") != Application.identifier)
        {
            //"1".Show();
            if (Application.sandboxType == ApplicationSandboxType.Sandboxed || Application.isEditor || Utils.IsHeadless()
                )
            {
                //"2".Show();
                string userModeljson;
                yield return new WaitForSeconds(2);
                if (!Utils.IsHeadless())
                    userModeljson = PlayerPrefs.GetString("userModel");
                else
                    userModeljson = "{\"status\":true,\"message\":\"\",\"user\":{\"file_url\":\"uploads/51d02ud2rm.jpg\",\"_id\":10201,\"first_name\":\"Server\",\"last_name\":\"Headless\",\"full_name\":\"\",\"country\":\"india\",\"status\":\"active\",\"email\":\"6\",\"password\":\"75\",\"role\":\"player\",\"phone\":\"926\",\"silver_balance\":4977518,\"gold_balance\":4979500,\"createdAt\":\"2025-01-23T09:53:56.000Z\",\"updatedAt\":\"2025-09-18T19:13:28.000Z\",\"referral_code\":\"F657DE\",\"imei\":\"\",\"user_login_token\":\"ffe06542ab3062fe811a13ed14f8b2b7ac4ea3ea\",\"block\":\"\",\"allow_to_game\":false,\"game_restrict_at\":\"\",\"restriction_end_at\":\"\",\"attempts\":0,\"__v\":0,\"bet_block\":[],\"updated_by\":\"\",\"deviceToken\":\"StubToken\",\"is_authorized_merchants\":0},\"access_token\":\"eyqJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJfaWQiOjEwMjAxLCJuYW1lIjoicGxheWVyIDYiLCJjb3VudHJ5IjoiaW5kaWEiLCJlbWFpbCI6IjYiLCJzdGF0dXMiOiJhY3RpdmUiLCJyb2xlIjoicGxheWVyIiwiaWF0IjoxNzU4MjIyODA2LCJleHAiOjE3NTg4Mjc2MDZ9.oWu-EmSTd6TpS0fE2Nmbm1WqeyiVVCg51r6bW1e6Xoo\"}";

                UserModel userModel = JsonUtility.FromJson<UserModel>(userModeljson);
                Debug.Log(Utils.IsHeadless() + " : " + userModeljson);
                //"3".Show();

                if (userModeljson != null && userModeljson != "")
                {
                    ApiAndRoomManager.LastFetchedCoins.data.gold_balance = userModel.user.gold_balance.ToString();
                    ApiAndRoomManager.LastFetchedCoins.data.silver_balance = userModel.user.silver_balance.ToString();

                    staticVariables.UserProfiledata = userModel;
                    ServerConnection.DownloadSprite("/" + staticVariables.UserProfiledata.user.file_url, OnLoaded =>
                    {
                        staticVariables.ProfilePicture = OnLoaded;
                    }, OnFailed =>
                    {
                        staticVariables.ProfilePicture = SpritesManager.Instance.spritesScriptable.nullProfileImg;
                    });
                    ApiAndRoomManager._instance.ModifyUserBalance();
                    //Constants_M.Log("loadiong m,nanger");
                    SceneLoaderUtility.LoadScene("Home");
                }
                else
                {
                    //SceneLoaderUtility.LoadScene("HomeScene");
                    SceneLoaderUtility.LoadScene("LoginScene");
                }
            }
            else
            {
                Application.Quit();
            }
        }
        //else
        //{
        //    SceneLoaderUtility.LoadScene("Home");
        //    staticVariables.UserProfiledata = new UserModel();
        //    GuestDataManager._instance.ModifyCoins();
        //    GuestDataManager._instance.RefreshProfileInfo();
        //    staticVariables.isGuest = true;
        //}
    }
    //IEnumerator CheckForUpdate()
    //{
    //   PlayAsyncOperation <AppUpdateInfo, AppUpdateErrorCode> appUpdateInfoOperation =
    //        appUpdateManager.GetAppUpdateInfo();

    //    // Wait until the asynchronous operation completes.
    //    yield return appUpdateInfoOperation;

    //    if (appUpdateInfoOperation.IsSuccessful)
    //    {
    //        var appUpdateInfoResult = appUpdateInfoOperation.GetResult();
    //        // Check AppUpdateInfo's UpdateAvailability, UpdatePriority,
    //        // IsUpdateTypeAllowed(), ... and decide whether to ask the user
    //        // to start an in-app update.
    //        isUpdateAvailable = true;
    //        PopupMessageManager.instance.ShowConfirmPanel("New update is available, please download to continue", yesButtonText: "Update", noButtonStatus: false, methodToInvoke:
    //                () =>
    //                {
    //                    Application.OpenURL("https://play.google.com/store/apps/details?id=com.games.panda.au");
    //                });
    //    }
    //    else
    //    {
    //        // Log appUpdateInfoOperation.Error.
    //        isUpdateAvailable = false;
    //        StartCoroutine(Timer());
    //    }
    //}
}
