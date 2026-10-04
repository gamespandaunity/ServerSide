using UnityEngine;

public class SnokerUIManager : MonoBehaviour
{
    public string alertOptionalTextPrefix;
    public SnokerGameManager _SnokerGameManager;

    public notificationScript notifScriptComp;

    public GameObject cameraButtonObj;
    public static float btnAnimValue = 1f;
    [HideInInspector]
    public float btnAnimVel;
    [HideInInspector]

    public float btnAnimTarget;
    [HideInInspector]

    public float btnAnimCompleteCheckVal;
    public static string curScreen;
    [HideInInspector]

    public string screenToGoAfterMenuAnim = "null";

    public string prevScreen;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void showNotification(string msg, float timeout = 3f)
    {
        Debug.Log(msg);
        notificationScript.notifText = alertOptionalTextPrefix + msg;
        notificationScript.timeout = timeout;

        notifScriptComp.showNotification();
        alertOptionalTextPrefix = string.Empty;
    }
    public void switchScreen(string targetScreen)
    {
        if (btnAnimValue == 1f)
        {
            btnAnimValue = 0f;
        }
        btnAnimTarget = 1f;
        prevScreen = curScreen;
        screenToGoAfterMenuAnim = targetScreen;
    }
    public void onNotificationComplete()
    {
        if (!_SnokerGameManager.bTossDone)
        {
            _SnokerGameManager.gameStartAfterToss();
        }
        _SnokerGameManager.scheduledFunctionAfterNotif?.Invoke();
        _SnokerGameManager.scheduledFunctionAfterNotif = null;
    }
    public void blurGameView(bool val)
    {
        (_SnokerGameManager.cameraObjCamera.GetComponent("BlurOptimized") as MonoBehaviour).enabled = val;
    }
}
