using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Reflection;

public class GiftManager : MonoBehaviour
{
    public GameObject[] panels;
    public Button[] sideButtons;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI idText;
    public TMP_InputField coinsInput;
    public Button silverButton;
    public Button goldButton;
    private bool silverButtonSelected = false;
    private bool goldButtonSelected = false;

    public Color DimColor ;       // Slightly desaturated gold
    public GameObject GiftPrefab;
    public Transform SentGiftTransform;
    public Transform RecievedGiftTransform;

    public Sprite SilverUnselect, SilverSelect, GoldUnselect, GoldSelect,SideButtonSelect,SideButtonUnselect;
    public GiftListData getGiftList;
    void Start()
    {
        nameText.text = staticVariables.GiftRecieverId.first_name + " " + staticVariables.GiftRecieverId.last_name;
        idText.text = staticVariables.GiftRecieverId._id.ToString();
        silverButton.onClick.AddListener(OnSilverButtonClick);
        goldButton.onClick.AddListener(OnGoldButtonClick);
        UpdateButtons(); // Initialize button colors on start
    }
    private void OnEnable()
    {
        OpenPanel(0);
         OnSilverButtonClick();
        goldButton.gameObject.SetActive(staticVariables.gameFeatures.isGoldCoins); 
    }

    void OnSilverButtonClick()
    {
        silverButtonSelected = true;
        goldButtonSelected = false;
        UpdateButtons();
    }

    void OnGoldButtonClick()
    {
        silverButtonSelected = false;
        goldButtonSelected = true;
        UpdateButtons();
    }

    void UpdateButtons()
    {
        // Get the Image components
        Image silverImage = silverButton.GetComponent<Image>();
        Image goldImage = goldButton.GetComponent<Image>();

        if (silverButtonSelected)
        {
            silverImage.color = Color.white;
            goldImage.color = DimColor;
            silverImage.sprite = SilverSelect;
        }
        else
        {
            silverImage.color = DimColor;
            silverImage.sprite=SilverUnselect;
        }

        if (goldButtonSelected)
        {
            goldImage.color = Color.white;
            silverImage.color = DimColor;
            goldImage.sprite = GoldSelect;
        }
        else
        {
            goldImage.color = DimColor;
            goldImage.sprite = GoldUnselect;
        }
    }

    public void OpenPanel(int index)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);
            if (i == index)
            {
                sideButtons[i].GetComponent<Image>().sprite=SideButtonSelect;
            }
            else
            {
                sideButtons[i].GetComponent<Image>().sprite = SideButtonUnselect;

            }
        }

        // Reset the input field when opening the panel
        coinsInput.text = "100"; // Default value or any other logic you want
        silverButtonSelected = true;
        goldButtonSelected = false;
        UpdateButtons(); // Reset button colors
        if (index == 1) // If the panel is the "Sent Gifts" panel
        {
            GetSendList(); // Fetch the sent gift list when opening the panel
        }
        else if (index == 2) // If the panel is the "Received Gifts" panel
        {
            GetRecievedList(); // Fetch the received gift list when opening the panel
        }
    }
    public void SendGift()
    {
        Dictionary<string, string> giftData = new Dictionary<string, string>
        {
            { "receiver_id", staticVariables.GiftRecieverId._id.ToString() },
            { "silver", silverButtonSelected?coinsInput.text:"0"},
            { "gold", goldButtonSelected?coinsInput.text:"0"}
        };
        ApiAndRoomManager._instance.PostSendGift(giftData, succeess =>
        {
            Single<int> var = JsonUtility.FromJson<Single<int>>(succeess);
            if (var.status)
            {
                PopupMessageManager.instance.ShowPopUp( "Your gift has been sent successfully!", "Gift Sent", 3, () =>
                {
                    // Optionally reset the input fields or perform other actions
                    coinsInput.text = "100";
                    silverButtonSelected = false;
                    goldButtonSelected = false;
                    UpdateButtons();
                });
            }
            else
            {
                PopupMessageManager.instance.ShowPopUp(var.message, "Failed", 3);
            }
                UIMainMenManager.instance.openPanelByName("UserFriendsList_");
        });
    }

    public void GetSendList()
    {
        SentGiftTransform.Clear();
        ApiAndRoomManager._instance.GetSentGiftList(OnSuccess =>
        {
            getGiftList = JsonUtility.FromJson<GiftListData>(OnSuccess);

            if (getGiftList.status)
            {
                for (int i = 0; i < getGiftList.data.Count; i++)
                {
                    GameObject giftPlayer = Instantiate(GiftPrefab, SentGiftTransform);
                    giftPlayer.GetComponentInChildren<TextMeshProUGUI>().text = getGiftList.data[i].first_name + " " + getGiftList.data[i].last_name;
                    giftPlayer.GetComponent<GiftPrefab>().Initialize(getGiftList.data[i], true);
                }
            }
        });
    }
    public void GetRecievedList()
    {
        RecievedGiftTransform.Clear();
        ApiAndRoomManager._instance.GetRecievedGiftList(OnSuccess =>
        {
            getGiftList = JsonUtility.FromJson<GiftListData>(OnSuccess);

            if (getGiftList.status)
            {
                for (int i = 0; i < getGiftList.data.Count; i++)
                {
                    GameObject giftPlayer = Instantiate(GiftPrefab, RecievedGiftTransform);
                    giftPlayer.GetComponentInChildren<TextMeshProUGUI>().text = getGiftList.data[i].first_name + " " + getGiftList.data[i].last_name;
                    giftPlayer.GetComponent<GiftPrefab>().Initialize(getGiftList.data[i], false);
                }
            }
        });
    }
}
[System.Serializable]
public class GiftPlayerData
{
    public int _id;
    public int sender_id;
    public int receiver_id;
    public int is_claimed;
    public int silver;
    public int gold;
    public string createdAt;
    public string updatedAt;
    public string first_name;
    public object user_ip;
    public object block;
    public string last_name;
    public string country;
    public string status;
    public object created_by;
    public object updated_by;
    public string email;
    public string password;
    public string role;
    public string phone;
    public int silver_balance;
    public int gold_balance;
    public string file_url;
    public object allow_to_game;
    public object game_restrict_at;
    public object restriction_end_at;
    public object __v;
    public string user_login_token;
    public string deviceToken;
    public int attempts;
    public object userId;
    public object bet_block;
    public string referral_code;
    public object full_name;
    public object social_id;
    public object social_type;
    public int silver_play_allowed;
    public string reset_token;
    public int block_market_ads;
    public int block_market_orders;
    public int block_market_orders_acceptance;
    public string device_id;
    public string auth_identifier;
    public string gift_id;

    public DateTime giftTime;
}
[System.Serializable]
public class GiftListData
{
    public bool status;
    public List<GiftPlayerData> data;
}
