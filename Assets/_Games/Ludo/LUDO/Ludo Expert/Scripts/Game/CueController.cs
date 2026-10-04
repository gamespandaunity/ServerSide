using UnityEngine;
using System.Collections;
using AssemblyCSharp;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using LudoGame;
//using Photon.Pun;
using ExitGames.Client.Photon;
//using Photon.Realtime;

public class CueController : MonoBehaviour
{


    [HideInInspector]
    public bool isServer;

    public GameObject youWonMessage;
    private bool canShowControllers = true;
    public GameObject prizeText;
    private AudioSource[] audioSources;
    public GameObject audioController;
    public GameObject invitiationDialog;
    public GameObject chatButton;
    public GameControllerScript gameControllerScript;


    void Start()
    {

        gameControllerScript = GameObject.Find("GameController").GetComponent<GameControllerScript>();

        if (LudoGame.GameManager.Instance.offlineMode)
        {
            chatButton.SetActive(false);
        }


        if (!LudoGame.GameManager.Instance.offlineMode)
            LudoGame.GameManager.Instance.playfabManager.addCoinsRequest(-LudoGame.GameManager.Instance.payoutCoins);


        LudoGame.GameManager.Instance.audioSources = audioController.GetComponents<AudioSource>();
        audioSources = GetComponents<AudioSource>();

        LudoGame.GameManager.Instance.iWon = false;
        LudoGame.GameManager.Instance.iLost = false;
        LudoGame.GameManager.Instance.iDraw = false;


        setPrizeText();


        LudoGame.GameManager.Instance.cueController = this;

        isServer = false;

        if (LudoGame.GameManager.Instance.roomOwner)
        {
            isServer = true;
        }

    }


    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
          //  PhotonNetwork.RaiseEvent(151, 1, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            //PhotonNetwork.SendAllOutgoingCommands();
            Debug.Log("Application pause");
        }
        else
        {
            //PhotonNetwork.RaiseEvent(152, 1, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            //PhotonNetwork.SendAllOutgoingCommands();
            Debug.Log("Application resume");
        }
    }



    private void setPrizeText()
    {
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

        if (LudoGame.GameManager.Instance.offlineMode)
        {
            prizeText.GetComponent<Text>().text = "Practice";
        }
    }




    void Awake()
    {
     //   PhotonNetwork.OnEventCall += this.OnEvent;
    }

    public void removeOnEventCall()
    {
      //  PhotonNetwork.OnEventCall -= this.OnEvent;
    }


    void Update()
    {

    }

    void FixedUpdate()
    {

    }


    void OnDestroy()
    {
       // PhotonNetwork.OnEventCall -= this.OnEvent;
    }

    // Multiplayer data received
    private void OnEvent(byte eventcode, object content, int senderid)
    {

        // if (!isServer && eventcode == 0)
        // {

        // }
        // else if (eventcode == 19)
        // { // Opponent Won!
        //     HideAllControllers();
        //     LudoGame.GameManager.Instance.audioSources[3].Play();
        //     youWonMessage.SetActive(true);
        //     youWonMessage.GetComponent<YouWinMessageChangeSprite>().changeSprite();
        //     youWonMessage.GetComponent<Animator>().Play("YouWinMessageAnimation");
        //     LudoGame.GameManager.Instance.iWon = false;
        // }
        // else if (eventcode == 20)
        // { // You won!
        //     HideAllControllers();
        //     LudoGame.GameManager.Instance.audioSources[3].Play();
        //     youWonMessage.SetActive(true);
        //     youWonMessage.GetComponent<Animator>().Play("YouWinMessageAnimation");
        //     LudoGame.GameManager.Instance.iWon = true;
        // }
        // else if (eventcode == 21)
        // { // You draw!
        //     HideAllControllers();
        //     LudoGame.GameManager.Instance.audioSources[3].Play();
        //     youWonMessage.SetActive(true);
        //     youWonMessage.GetComponent<Animator>().Play("YouWinMessageAnimation");
        //     LudoGame.GameManager.Instance.iDraw = true;
        // }
        // else if (eventcode == 192)
        // { // Invitiation received
        //     invitiationDialog.GetComponent<PhotonChatListener2>().showInvitationDialog(null, null, null);
        // }
        // else if (eventcode == 151)
        // { // Opponent paused game
        //     // if (isServer)
        //     //     ShotPowerIndicator.anim.Play("ShotPowerAnimation");
        //     LudoGame.GameManager.Instance.opponentActive = false;
        //     LudoGame.GameManager.Instance.stopTimer = true;
        //     LudoGame.GameManager.Instance.gameControllerScript.showMessage(StaticStrings.waitingForOpponent + " " + StaticStrings.photonDisconnectTimeout);
        // }
        // else if (eventcode == 152)
        // { // Opponent resumed game
        //     // if (canShowControllers && isServer && !shotMyTurnDone)
        //     //     ShotPowerIndicator.anim.Play("MakeVisible");
        //     LudoGame.GameManager.Instance.opponentActive = true;

        //     // if ((isServer && !shotMyTurnDone) || !isServer)
        //     LudoGame.GameManager.Instance.stopTimer = false;
        //     // LudoGame.GameManager.Instance.gameControllerScript.hideBubble();

        // }
        // else if (eventcode == 9)
        // { // My turn - show cue and lines

        //     setMyTurn();
        // }

    }

    public void setOpponentTurn()
    {
        isServer = false;
        gameControllerScript.resetTimers(2, true);
        LudoGame.GameManager.Instance.miniGame.setOpponentTurn();
    }

    public void setMyTurn()
    {
        LudoGame.GameManager.Instance.myTurnDone = false;
        isServer = true;
        gameControllerScript.resetTimers(1, true);
        LudoGame.GameManager.Instance.miniGame.setMyTurn();
    }

    public void checkShot()
    {
        if (LudoGame.GameManager.Instance.iWon)
        {
            IWon();
        }
        else if (LudoGame.GameManager.Instance.iLost)
        {
            ILost();
        }
    }


    public void IWon()
    {
        LudoGame.GameManager.Instance.iWon = true;
        HideAllControllers();
        LudoGame.GameManager.Instance.audioSources[3].Play();
        youWonMessage.SetActive(true);
        youWonMessage.GetComponent<Animator>().Play("YouWinMessageAnimation");
        //if (!LudoGame.GameManager.Instance.offlineMode)
          //  PhotonNetwork.RaiseEvent(19, null, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }

    public void Draw()
    {
        LudoGame.GameManager.Instance.iDraw = true;
        HideAllControllers();
        LudoGame.GameManager.Instance.audioSources[3].Play();
        youWonMessage.SetActive(true);
        youWonMessage.GetComponent<Animator>().Play("YouWinMessageAnimation");
       // if (!LudoGame.GameManager.Instance.offlineMode)
         //   PhotonNetwork.RaiseEvent(21, null, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }

    public void ILost()
    {
        LudoGame.GameManager.Instance.iWon = false;
        HideAllControllers();
        LudoGame.GameManager.Instance.audioSources[3].Play();
        youWonMessage.SetActive(true);
        youWonMessage.GetComponent<YouWinMessageChangeSprite>().changeSprite();
        youWonMessage.GetComponent<Animator>().Play("YouWinMessageAnimation");
      //  if (!LudoGame.GameManager.Instance.offlineMode)
        //    PhotonNetwork.RaiseEvent(20, null, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }

    public void setTurnOffline(bool showTurnMessage)
    {

    }





    private void ShowAllControllers()
    {
        if (canShowControllers)
        {
            Debug.Log("Showing controllers");
        }
    }

    public void HideAllControllers()
    {

    }

    public void stopTimer()
    {
        LudoGame.GameManager.Instance.stopTimer = true;
    }

}
