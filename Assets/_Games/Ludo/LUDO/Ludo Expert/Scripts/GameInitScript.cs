using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LudoGame;
using ExitGames.Client.Photon;
//using Photon.Realtime;
//using Photon.Pun;

public class GameInitScript : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {


        if (LudoGame.GameManager.Instance.roomOwner)
        {
            //RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            //{
            //    Receivers = ReceiverGroup.All
            //};
            //SendOptions sendOptions = new SendOptions
            //{
            //    Reliability = true
            //};
            //PhotonNetwork.RaiseEvent(198, null, raiseEventOptions, sendOptions);
        }
        else
        {
            // for (int i = 0; i < LudoGame.GameManager.Instance.initPositions.Length; i++) {
            //     LudoGame.GameManager.Instance.balls[i + 1].GetComponent<Rigidbody>().transform.position = LudoGame.GameManager.Instance.initPositions[i];
            //     LudoGame.GameManager.Instance.balls[i + 1].SetActive(true);
            // }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
