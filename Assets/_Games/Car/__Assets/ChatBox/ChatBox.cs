namespace CarRace 
{
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
 
//using ExitGames.Client.Photon;
using TMPro;

public class ChatBox //Photon Removal : MonoBehaviourPunCallbacks
    {
    [SerializeField] AudioManager audioManager;
    [SerializeField] GameObject chatBoxGameobject;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Button sendButton;
    [SerializeField] TextMeshProUGUI chatText;
    [SerializeField] Button closeChatBoxButton;
    [SerializeField] Button openChatBoxButton;
    [SerializeField] Button clearChatBoxButton;
    [SerializeField] GameObject notificationIcon;
    [SerializeField] TMP_Dropdown recipientList;
    [SerializeField] TextMeshProUGUI selectedrecipientText;
    List<string> recipientNames = new List<string> { "All" };

    [SerializeField] Button openPhrasesButton;
    [SerializeField] GameObject phrasesBox;
    [SerializeField] List<string> shortMsgs;
    TextMeshProUGUI[] phraseTexts;
    Button[] phraseButtons;
    bool isPhraseBoxOpen;

    bool isChatBoxOpened;
        //public override void OnDisable()                                                                                  //Photon Removal
        //{
        //    PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        //}

        //public override void OnEnable()                                                                                  //Photon Removal
        //{
        //    PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        //}
        private void Start()
    {
            //Photon Removal sendButton.onClick.AddListener(SendTextMessage);
            closeChatBoxButton.onClick.AddListener(CloseChatBox);
        openChatBoxButton.onClick.AddListener(OpenChatBox);
        clearChatBoxButton.onClick.AddListener(ClearChatBox);
        CloseChatBox();
        notificationIcon.SetActive(false);
        SetUpPhraseBox();
    }

    private void ClearChatBox()
    {
        chatText.text = string.Empty;
    }

    private void OpenChatBox()
    {
        AddRecipientToList();
        chatBoxGameobject.SetActive(true);
        openChatBoxButton.gameObject.SetActive(false);
        isChatBoxOpened = true;
        notificationIcon.SetActive(false);

    }
    private void CloseChatBox()
    {
        chatBoxGameobject.SetActive(false);
        openChatBoxButton.gameObject.SetActive(true);
        isChatBoxOpened = false;
    }

    public enum RaiseEventsCode
    {
        chatMsg = 1
    }

        //private void SendTextMessage()                                                                                                                                                   //Photon Removal
        //{
        //    string recipientName = selectedrecipientText.text;
        //    string msg = inputField.text;

        //    if (!string.IsNullOrEmpty(msg))
        //    {
        //        inputField.text = "";
        //        sendButton.gameObject.SetActive(false);
        //        string playerName = PlayerPrefs.GetString(PPConst.UserName);

        //        object[] data = new object[] { playerName, msg, recipientName };

        //        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        //        {
        //            Receivers = ReceiverGroup.All,
        //            CachingOption = EventCaching.AddToRoomCache
        //        };

        //        SendOptions sendOptions = new SendOptions
        //        {
        //            Reliability = false
        //        };

        //        PhotonNetwork.RaiseEvent((byte)RaiseEventsCode.chatMsg, data, raiseEventOptions, sendOptions);
        //        sendButton.gameObject.SetActive(true);
        //    }

        //}

    //    void OnEvent(EventData photonEvent)
    //{

    //    if (photonEvent.Code == (byte)RaiseEventsCode.chatMsg)
    //    {
    //        object[] data = (object[])photonEvent.CustomData;
    //        string playerName = (string)data[0];
    //        string playerMsg = (string)data[1];
    //        string recepientName = (string)data[2];
    //        string localPlayer = PlayerPrefs.GetString(PPConst.UserName);

    //        if (recepientName.Equals("ALL") || recepientName.Equals(localPlayer) || playerName.Equals(localPlayer))
    //        {
    //            chatText.text += "<u><color=#00FFF0>" + playerName + "</color></u> : " + playerMsg + "\n";
    //            if (!isChatBoxOpened)
    //            {
    //                notificationIcon.SetActive(true);
    //                audioManager.PlaySound("Chat");
    //            }
    //        }


    //    }
    //}
    public void AddRecipientToList()
    {
        recipientNames.Clear();
        recipientNames.Add("ALL");
            //foreach (Player player in PhotonNetwork.PlayerList)                                                                                //Photon Removal
            //{
            //    recipientNames.Add(player.NickName);
            //}
            recipientList.ClearOptions();
        recipientList.AddOptions(recipientNames);
    }

    void SetUpPhraseBox()
    {
        isPhraseBoxOpen = false;
        openPhrasesButton.onClick.RemoveAllListeners();
        openPhrasesButton.onClick.AddListener(() => 
        {
            isPhraseBoxOpen = !isPhraseBoxOpen;
            phrasesBox.SetActive(isPhraseBoxOpen);
        });
        phraseButtons = phrasesBox.GetComponentsInChildren<Button>(); //Cache Buttons
        phraseTexts = phrasesBox.GetComponentsInChildren<TextMeshProUGUI>(); //Cache Text Objects
        for (int i = 0; i < phraseTexts.Length; i++)
        {
            phraseTexts[i].text = shortMsgs[i]; //Populate Phrases
        }

        for (int i = 0; i < phraseButtons.Length; i++)
        {
            int index = i;
            phraseButtons[index].onClick.RemoveAllListeners();
            phraseButtons[index].onClick.AddListener(delegate { SendPhrase(index); });
            
        }
        phrasesBox.SetActive(isPhraseBoxOpen);
    }

    void SendPhrase(int praseIndex)
    {
        string recipientName = selectedrecipientText.text;
        string msg = phraseTexts[praseIndex].text;

        string playerName = PlayerPrefs.GetString(PPConst.UserName);

        object[] data = new object[] { playerName, msg, recipientName };

            //RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            //{                                                                                                                                                                                          //Photon Removal
            //    Receivers = ReceiverGroup.All,
            //    CachingOption = EventCaching.AddToRoomCache
            //};

            //SendOptions sendOptions = new SendOptions
            //{
            //    Reliability = false
            //};

            //PhotonNetwork.RaiseEvent((byte)RaiseEventsCode.chatMsg, data, raiseEventOptions, sendOptions);
        }
    }

}