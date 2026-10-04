using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleNotification : MonoBehaviour
{
    public Text headerText;
    public Text bodyText;
    public TextMeshProUGUI yesBtnText, noBtnText;
    public RawImage image;
    [SerializeField] private RectTransform targetRect;
    public Action onYes, onNo;

    private void Start()
    {
        targetRect.DOAnchorPosY(350f, 0.5f).SetEase(Ease.OutQuad); 
    }

    public void Notification(string body = "", string header = "",string ImageUrl="", string yesBtnText = "YES", string noBtnText = "CANCEL", bool yesBtnStatus = true, bool noBtnStatus = true, Action onYes = null, Action onNo = null, int destroyTime = 0)
    {
        bodyText.text = body;
        headerText.text = header;
        this.yesBtnText.text = yesBtnText;
        this.noBtnText.text = noBtnText;
        this.yesBtnText.transform.parent.gameObject.SetActive(yesBtnStatus);
        this.noBtnText.transform.parent.gameObject.SetActive(noBtnStatus);
        if (onYes != null)
        {
            this.onYes = onYes;
        }
        if (onNo != null)
        {
            this.onNo = onNo;
        }
        if(destroyTime != 0)
        {
            Destroy(this.gameObject,destroyTime);
        }
        if (ImageUrl != "")
        {
            ServerConnection.DownloadSprite(ImageUrl, DownloadedTexture =>
            {
                image.texture = DownloadedTexture;
            });
        }
        GetComponent<RectTransform>().ForceUpdateRectTransforms();
        Canvas.ForceUpdateCanvases();
        GetComponentInChildren<VerticalLayoutGroup>().CalculateLayoutInputVertical();
    }
    public void YesBtnClick()
    {
        if(onYes != null)
        {
            onYes.Invoke();
        }
        Destroy(this.gameObject);
    }
    public void NoBtnClick()
    {
        if (onNo != null)
        {
            onNo.Invoke();
        }
        Destroy(this.gameObject);
    }

    public void Cross()
    {
        Destroy(this.gameObject);
    }

}
