using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AssemblyCSharp;
using PlayFab.ClientModels;
//using Facebook.Unity;
using System;
using LudoGame;
using ExitGames.Client.Photon;


public class WinnerControllerScript : MonoBehaviour
{

    public GameObject myObject;
    public GameObject opponentObject;
    public GameObject shareButton;

    public bool isGameScene = false;

    public Image myImage;
    public Image oppoImage;

    public Text myName;
    public Text oppoText;

    public GameObject myMessageBubble;
    public GameObject oppoMessageBubble;

    public GameObject rematchButton;

    public bool rematchRequest = false;
    public bool sentRematch = false;

    public GameObject ChatMessagesList;
    public GameObject ChatMessageButtonPrefab;

    public GameObject ChatMessagesObject;

    public GameObject prizeText;
    private AudioSource[] audioSources;
    public GameObject reardShareText;

    public bool messageDialogVisible = false;
    // Use this for initialization
    void Start()
    {



        audioSources = GetComponents<AudioSource>();

        if (LudoGame.GameManager.Instance.playerDisconnected)
        {
            LudoGame.GameManager.Instance.playerDisconnected = false;
            if (!isGameScene)
            {
                rematchButton.SetActive(false);
            }
        }



        if (!isGameScene)
        {


            //PhotonNetwork.KeepAliveInBackground = StaticStrings.photonDisconnectTimeoutLong; 

            if (LudoGame.GameManager.Instance.payoutCoins > LudoGame.GameManager.Instance.myPlayerData.GetCoins())
            {
                rematchButton.SetActive(false);
            }

            if (reardShareText != null)
                reardShareText.GetComponent<Text>().text = "+" + StaticStrings.rewardCoinsForShareViaFacebook;

            // if (StaticStrings.showAdOnGameOverScene)
            //     AdsManager.Instance.adsScript.ShowAd();

            if (!PlayerPrefs.GetString("LoggedType").Equals("Facebook"))
            {
                shareButton.SetActive(false);
            }

            rematchRequest = false;
            sentRematch = false;

            if (LudoGame.GameManager.Instance.iWon)
            {
                myObject.GetComponent<Animator>().Play("WinnerOpponentAnimation");
                audioSources[0].Play();
                LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(LudoGame.GameManager.Instance.payoutCoins * 2);
            }
            else if (LudoGame.GameManager.Instance.iDraw)
            {
                myObject.GetComponent<Animator>().Play("WinnerOpponentAnimation");
                opponentObject.GetComponent<Animator>().Play("WinnerOpponentAnimation");
                audioSources[0].Play();
                LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(LudoGame.GameManager.Instance.payoutCoins);
            }
            else
            {
                opponentObject.GetComponent<Animator>().Play("WinnerOpponentAnimation");
                audioSources[1].Play();
            }

            if (LudoGame.GameManager.Instance.avatarMy != null)
                myImage.sprite = LudoGame.GameManager.Instance.avatarMy;
            if (LudoGame.GameManager.Instance.avatarOpponent != null)
                oppoImage.sprite = LudoGame.GameManager.Instance.avatarOpponent;

            myName.text = LudoGame.GameManager.Instance.nameMy;
            oppoText.text = LudoGame.GameManager.Instance.nameOpponent;

            int prizeCoins = LudoGame.GameManager.Instance.payoutCoins * 2;

            if (prizeCoins >= 1000)
            {
                if (prizeCoins >= 1000000)
                {
                    if (prizeCoins % 1000000.0f == 0)
                    {
                        prizeText.GetComponent<Text>().text = (prizeCoins / 1000000.0f).ToString("0") + "M";

                    }
                    else
                    {
                        prizeText.GetComponent<Text>().text = (prizeCoins / 1000000.0f).ToString("0.0") + "M";

                    }

                }
                else
                {
                    if (prizeCoins % 1000.0f == 0)
                    {
                        prizeText.GetComponent<Text>().text = (prizeCoins / 1000.0f).ToString("0") + "k";
                    }
                    else
                    {
                        prizeText.GetComponent<Text>().text = (prizeCoins / 1000.0f).ToString("0.0") + "k";
                    }

                }
            }
            else
            {
                prizeText.GetComponent<Text>().text = prizeCoins + "";
            }
        }

        for (int i = 0; i < StaticStrings.chatMessages.Length; i++)
        {
            GameObject button = Instantiate(ChatMessageButtonPrefab);
            button.transform.GetChild(0).GetComponent<Text>().text = StaticStrings.chatMessages[i];
            button.transform.parent = ChatMessagesList.transform;
            button.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
            string index = StaticStrings.chatMessages[i];
            button.GetComponent<Button>().onClick.RemoveAllListeners();
            button.GetComponent<Button>().onClick.AddListener(() => SendMessageEvent(index));
        }

        for (int i = 0; i < StaticStrings.chatMessagesExtended.Length; i++)
        {
            if (LudoGame.GameManager.Instance.myPlayerData.GetChats().Contains("'" + i + "'"))
            {
                for (int j = 0; j < StaticStrings.chatMessagesExtended[i].Length; j++)
                {
                    GameObject button = Instantiate(ChatMessageButtonPrefab);
                    button.transform.GetChild(0).GetComponent<Text>().text = StaticStrings.chatMessagesExtended[i][j];
                    button.transform.parent = ChatMessagesList.transform;
                    button.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    string index = StaticStrings.chatMessagesExtended[i][j];
                    button.GetComponent<Button>().onClick.RemoveAllListeners();
                    button.GetComponent<Button>().onClick.AddListener(() => SendMessageEvent(index));
                }
            }
        }

    }


    public void share()
    {
        if (PlayerPrefs.GetString("LoggedType").Equals("Facebook"))
        {

            Uri myUri = new Uri("https://play.google.com/store/apps/details?id=" + StaticStrings.AndroidPackageName);
#if UNITY_IPHONE
            myUri = new Uri("https://itunes.apple.com/us/app/apple-store/id" + StaticStrings.ITunesAppID);
#endif

            //FB.ShareLink(
            //    myUri,
            //    StaticStrings.facebookShareLinkTitle,
            //    callback: ShareCallback
            //);
        }
    }

    //private void ShareCallback(IShareResult result)
    //{
    //    if (result.Cancelled || !String.IsNullOrEmpty(result.Error))
    //    {
    //        Debug.Log("ShareLink Error: " + result.Error);
    //    }
    //    else if (!String.IsNullOrEmpty(result.PostId))
    //    {
    //        // Print post identifier of the shared content
    //        Debug.Log(result.PostId);
    //    }
    //    else
    //    {
    //        // Share succeeded without postID
    //        LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(StaticStrings.rewardCoinsForShareViaFacebook);
    //        Debug.Log("ShareLink success!");
    //    }
    //}


    void OnDestroy()
    {
        removeOnEventCall();
    }

    public void SendMessageEvent(string index)
    {
        Debug.Log("Button Clicked " + index);
        if (!LudoGame.GameManager.Instance.offlineMode)
    //    PhotonNetwork.RaiseEvent(193, 1, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);

        ChatMessagesObject.GetComponent<Animator>().Play("hideMessageDialog");
        messageDialogVisible = false;

        if (isGameScene)
        {
            myMessageBubble.SetActive(true);
            myMessageBubble.transform.GetChild(0).GetComponent<Text>().text = index;
            if (isGameScene)
            {
                CancelInvoke("hideMyMessageBubble");
                Invoke("hideMyMessageBubble", 6.0f);
            }
        }

    }

    public void loadMenuScene()
    {
        // if (LudoGame.GameManager.Instance.offlineMode && StaticStrings.showAdWhenLeaveGame)
        //     AdsManager.Instance.adsScript.ShowAd();
        SceneManager.LoadScene("MenuScene");
        Debug.Log("Timeout 6");
        //PhotonNetwork.KeepAliveInBackground = StaticStrings.photonDisconnectTimeoutLong; 
       // if (!LudoGame.GameManager.Instance.offlineMode)
        //PhotonNetwork.RaiseEvent(194, 1, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);

        removeOnEventCall();

        LudoGame.GameManager.Instance.cueController.removeOnEventCall();
       // PhotonNetwork.LeaveRoom();

        LudoGame.GameManager.Instance.playfabManager.roomOwner = false;
        LudoGame.GameManager.Instance.roomOwner = false;
        LudoGame.GameManager.Instance.resetAllData();

    }

    public void sendRematchRequest()
    {
        if (!rematchRequest)
        {
            sentRematch = true;
            Debug.Log("Send message");
           // if (!LudoGame.GameManager.Instance.offlineMode)
            //PhotonNetwork.RaiseEvent(195,1, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);

            myMessageBubble.SetActive(true);
            myMessageBubble.transform.GetChild(0).GetComponent<Text>().text = StaticStrings.IWantPlayAgain;
            rematchButton.SetActive(false);
        }
        else
        {
            Debug.Log("Send message");
          //  if (!LudoGame.GameManager.Instance.offlineMode)
            //    PhotonNetwork.RaiseEvent(195, 1, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            rematchButton.SetActive(false);
            LudoGame.GameManager.Instance.resetAllData();
            LudoGame.GameManager.Instance.GameScene = "GameScene";
            if (!LudoGame.GameManager.Instance.gameSceneStarted)
            {
                SceneManager.LoadScene(LudoGame.GameManager.Instance.GameScene);
                LudoGame.GameManager.Instance.gameSceneStarted = true;
            }
            removeOnEventCall();
        }
    }

    public void sendMessageButton()
    {
        ChatMessagesObject.GetComponent<Animator>().Play("showMessagesDialog");
        messageDialogVisible = true;

    }

    void Awake()
    {
      //  PhotonNetwork.NetworkingClient.EventReceived += this.OnEvent;

    }

    public void removeOnEventCall()
    {
      // PhotonNetwork.NetworkingClient.EventReceived -= this.OnEvent;
    }

    // Multiplayer data received
    private void OnEvent(EventData photonEvent)
    {
        byte eventcode = photonEvent.Code;
        object content = photonEvent.CustomData;
        if (eventcode == 195)
        {
            if (sentRematch)
            {
                LudoGame.GameManager.Instance.resetAllData();
                LudoGame.GameManager.Instance.GameScene = "GameScene";
                if (!LudoGame.GameManager.Instance.gameSceneStarted)
                {
                    SceneManager.LoadScene(LudoGame.GameManager.Instance.GameScene);
                    LudoGame.GameManager.Instance.gameSceneStarted = true;
                }
                removeOnEventCall();
            }
            else
            {
                rematchRequest = true;
                if (LudoGame.GameManager.Instance.payoutCoins <= LudoGame.GameManager.Instance.myPlayerData.GetCoins())
                {
                    oppoMessageBubble.SetActive(true);
                    oppoMessageBubble.transform.GetChild(0).GetComponent<Text>().text = StaticStrings.IWantPlayAgain;
                }
            }
        }
        else if (eventcode == 194)
        {
            rematchButton.SetActive(false);
            oppoMessageBubble.SetActive(true);
            oppoMessageBubble.transform.GetChild(0).GetComponent<Text>().text = StaticStrings.cantPlayRightNow;

        }
        else if (eventcode == 193)
        {
            string index = (string)content;
            Debug.Log("INDEX: " + index);
            oppoMessageBubble.SetActive(true);
            oppoMessageBubble.transform.GetChild(0).GetComponent<Text>().text = index;
            if (isGameScene)
            {
                CancelInvoke("hideOppoMessageBubble");
                Invoke("hideOppoMessageBubble", 6.0f);
            }

        }
    }

    public void hideOppoMessageBubble()
    {
        oppoMessageBubble.SetActive(false);
    }

    public void hideMyMessageBubble()
    {
        myMessageBubble.SetActive(false);
    }
}
