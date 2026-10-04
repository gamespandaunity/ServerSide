using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
//using Photon.Chat;
using ExitGames.Client.Photon;
//using Photon.Pun;

public class UpdateActivePlayersFrame : MonoBehaviour
{

    private string currentValue = "Player Online : 0";
    private Text text;
    // Use this for initialization
    void Start()
    {
        text = GetComponent<Text>();
        InvokeRepeating("CheckAndUpdatePlayersValue", 0.2f, 0.2f);
    }

    private void CheckAndUpdatePlayersValue()
    {
        // if (currentValue != LudoGame.GameManager.Instance.myPlayerData.GetCoins())
        // {
        //     currentValue = LudoGame.GameManager.Instance.myPlayerData.GetCoins();
        //     if (currentValue != 0)
        //     {
        //         text.text = LudoGame.GameManager.Instance.myPlayerData.GetCoins().ToString("0,0", CultureInfo.InvariantCulture).Replace(',', ' ') + " INR";
        //     }
        //     else
        //     {
        //         text.text = "0 INR";
        //     }
        // }
        // Debug.Log("Total count of active player : " +PhotonNetwork.countOfPlayers);
        //text.text = "Players Online : " + PhotonNetwork.CountOfPlayers;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
