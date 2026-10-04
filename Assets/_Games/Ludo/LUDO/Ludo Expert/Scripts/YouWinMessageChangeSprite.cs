using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using AssemblyCSharp;
using LudoGame;
//using Photon.Pun;

public class YouWinMessageChangeSprite : MonoBehaviour
{

    public Sprite other;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void changeSprite()
    {
        GetComponent<Image>().sprite = other;
    }

    public void loadWinnerScene()
    {
        if (LudoGame.GameManager.Instance.offlineMode)
        {
            LudoGame.GameManager.Instance.playfabManager.roomOwner = false;
            LudoGame.GameManager.Instance.roomOwner = false;
            LudoGame.GameManager.Instance.resetAllData();
            SceneManager.LoadScene("MenuScene");
            //PhotonNetwork.KeepAliveInBackground = StaticStrings.photonDisconnectTimeoutLong; 
            // if (LudoGame.GameManager.Instance.offlineMode && StaticStrings.showAdWhenLeaveGame)
            //     AdsManager.Instance.adsScript.ShowAd();

        }
        else
        {
            SceneManager.LoadScene("WinnerScene");
        }

    }
}
