using UnityEngine;

public class BundlePurchase : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public UniWebView webView;
    void Start()
    {
        ApiAndRoomManager._instance.CreateSessionCoinsPaymentApi(staticVariables.shopPackage._id, OnSuccess =>
        {
            SessionCreatedData userModeli = JsonUtility.FromJson<SessionCreatedData>(OnSuccess);

            if(userModeli.status)
            {
                Debug.Log("Session Created Successfully");
                Debug.Log(userModeli._id);
                webView.urlOnStart =  $"https://payment.gamesbaba.com.au/coins/?_Id={userModeli._id}&token={staticVariables.UserProfiledata.user.user_login_token}";
                webView.gameObject.SetActive(true);
            }
            else
            {
                Debug.Log("Session Creation Failed");
                Debug.Log(userModeli.message);
            }

        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public class SessionCreatedData
{
    public bool status;
    public string message;
    public int _id;
}