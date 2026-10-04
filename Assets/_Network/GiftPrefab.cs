using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiftPrefab : MonoBehaviour
{
    public GiftPlayerData data;
    public RawImage profilePic;
    public Button ClaimButton;
    public Sprite silver, gold;
    public Image Coin;
    public TextMeshProUGUI CoinsText,DateText;
    public GameObject ClaimedObject;
    public void Initialize(GiftPlayerData data, bool isSendData)
    {
        this.data = data;
        Coin.sprite = data.silver != 0 ? silver : gold;
        CoinsText.text = (data.silver != 0 ? data.silver.ToString() : data.gold.ToString());
        DateText.text = DateTime.Parse(data.createdAt).ToString("dd-MM-yyyy");
        if (isSendData == false)
        {
            if (data.is_claimed == 0)
            {
                ClaimButton.gameObject.SetActive(true);
                ClaimedObject.gameObject.SetActive(false);

            }
            else
            {
                ClaimButton.gameObject.SetActive(false);
                ClaimedObject.gameObject.SetActive(true);
            }
        }
        else
        {
            ClaimButton.gameObject.SetActive(false);
            ClaimedObject.gameObject.SetActive(true);
            ClaimedObject.GetComponentInChildren<TextMeshProUGUI>().text = "SENT";

        }
        ServerConnection.DownloadSprite("/" + data.file_url, (sprite) =>
        {
            if (sprite != null)
            {
                profilePic.texture = sprite;
            }
            else
            {
                profilePic.texture = null; // Set to a default texture if needed
            }
        });
    }
    public void Claim()
    {
        ApiAndRoomManager._instance.GetClaimGift(data.gift_id, OnSuccess =>
        {
            PopupMessageManager.instance.ShowPopUp("Gift Claimed Successfully", "Gift", 3, () =>
            {

            });
            ClaimButton.gameObject.SetActive(false);
            ClaimedObject.gameObject.SetActive(true);
            ApiAndRoomManager._instance.ModifyUserBalance();
        });
    }
}