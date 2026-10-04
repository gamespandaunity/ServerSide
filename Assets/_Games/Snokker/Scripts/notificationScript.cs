using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class notificationScript : MonoBehaviour
{
    public static string notifText = string.Empty;

    public static float timeout;

    public RectTransform rectTransform;

    public float targetPosY;

    public Text notifTextComp;

    public LayoutElement notifTextLayoutElementComp;

    public GameObject[] avatarObjs = new GameObject[2];

    public Image[] avatarImages = new Image[2];

    private float notifAnimValue = 1f;

    private float notifAnimVel;

    private float notifAnimTarget;

    public SnokerUIManager _SnokerUIManager;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetPosY = rectTransform.anchoredPosition.y;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, targetPosY - notifAnimValue * targetPosY);
        notifTextComp = base.transform.Find("Text").GetComponent<Text>();
        notifTextLayoutElementComp = base.transform.Find("Text").GetComponent<LayoutElement>();
        avatarObjs[0] = base.transform.Find("Avatar1").gameObject;
        avatarObjs[1] = base.transform.Find("Avatar2").gameObject;
        avatarImages[0] = avatarObjs[0].GetComponent<Image>();
        avatarImages[1] = avatarObjs[1].GetComponent<Image>();
    }

    public void showNotification()
    {
        if (notifText != string.Empty)
        {
            //(" NOTIFICATION: " + notifText).Show();
            base.gameObject.SetActive(true);
            notifAnimValue = 1f;
            notifAnimTarget = 0f;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, targetPosY - notifAnimValue * targetPosY);
            notifTextComp.text = notifText;
            StartCoroutine(resizeTheMessage());
            CancelInvoke("hideNotif");
            Invoke("hideNotif", timeout);
        }
    }

    private IEnumerator resizeTheMessage()
    {
        
        if (mainScript.chosenAvatar[SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString() ? 0 : 1] == 10)
        {
            avatarObjs[0].SetActive(false);
            avatarObjs[1].SetActive(false);
        }
        else if (SnokerGameManager.currentTurn == staticVariables.UserProfiledata.user._id.ToString())
        {
            avatarObjs[0].SetActive(false);
            avatarObjs[1].SetActive(false);
        }
        else
        {
            avatarObjs[0].SetActive(false);
            avatarObjs[1].SetActive(false);
        }
        yield return new WaitForEndOfFrame();
        notifTextLayoutElementComp.preferredHeight = notifTextComp.preferredHeight + 80f;
    }

    private void hideNotif()
    {
        notifAnimTarget = 1f;
    }

    private void Update()
    {
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, targetPosY - notifAnimValue * (targetPosY * 1.1f));
        notifAnimValue = Mathf.SmoothDamp(notifAnimValue, notifAnimTarget, ref notifAnimVel, 0.2f);
        if (notifAnimTarget == 1f && notifAnimValue > 0.96f)
        {
            notifText = string.Empty;
            notifAnimValue = 1f;
            _SnokerUIManager.onNotificationComplete();
            base.gameObject.SetActive(false);
        }
        if (notifAnimValue < 0.15f && Input.GetMouseButtonDown(0))
        {
            CancelInvoke("hideNotif");
            hideNotif();
        }
    }
}
