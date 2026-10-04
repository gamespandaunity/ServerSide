using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SingleShopPackage : MonoBehaviour
{
    public TextMeshProUGUI packageName;

    //public TextMeshProUGUI silveQty;
    public TextMeshProUGUI goldCoins;
    public TextMeshProUGUI priceAmount;
    public Shopdatum shopPackage;
    public Sprite dollar5, dollar10, dollar20, dollar50, dollar100;
    public Image PanelBG;

    public void addCoins()
    {
        staticVariables.shopPackage = shopPackage;
        ApiAndRoomManager._instance.CreateSessionCoinsPaymentApi(shopPackage._id, OnSuccess =>
        {
            SessionCreatedData userModeli = JsonUtility.FromJson<SessionCreatedData>(OnSuccess);

            if (userModeli.status)
            {
                Debug.Log("Session Created Successfully");
                Debug.Log(userModeli._id);
                staticVariables.webViewShopUrl =
                    $"https://payment.gamesbaba.com.au/coins/?_Id={userModeli._id}&token={staticVariables.UserProfiledata.access_token}";
                PlayerPrefs.SetString("webViewShopUrl",staticVariables.webViewShopUrl);
#if UNITY_EDITOR
                Application.OpenURL(staticVariables.webViewShopUrl);
#else
                 SceneManager.LoadScene("UniWebViewDemo");
                Debug.Log(staticVariables.webViewShopUrl);
#endif
            }
            else
            {
                Debug.Log("Session Creation Failed");
                Debug.Log(userModeli.message);
            }
        });
        SoundManagerMain.instance.ClickSoundPlay();
        //ShopUIManager.instance.PaymentPanel.SetActive(true);
    }
}