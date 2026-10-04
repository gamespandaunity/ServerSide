using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
namespace Twelve
{
    public class TurnAnnouncementTwelve : NetworkBehaviour
    {
        public TextMeshProUGUI turnText;

        bool isMtTurnAnnounced = false;

        public GameObject wonObj;
        public GameObject lostObject;
        
        public void ApplyTurnUI(bool isOnlineMultiplayer, bool isWithAi, PLAYERS turn, bool isLocalWon)
        {
            if (TwelveBeadNetworkManager.isTossDone)
                return;

            TwelveBeadNetworkManager.isTossDone = true;

             if (isOnlineMultiplayer)
            {
                Debug.Log("Inside Online Multiplayer branch");
                if (!isLocalWon)
                {
                    Debug.Log("Local player did NOT win the toss");
                    wonObj.SetActive(false);
                    Debug.Log("wonObj deactivated");
                    lostObject.SetActive(true);
                    Debug.Log("lostObject activated");
                    turnText.text = "You Lost The Toss";
                    Debug.Log("turnText set to 'You Lost The Toss'");
                }
                else
                {
                    Debug.Log("Local player WON the toss");
                    turnText.text = "You Won The Toss";
                    Debug.Log("turnText set to 'You Won The Toss'");
                    wonObj.SetActive(true); Debug.Log("wonObj activated");
                    lostObject.SetActive(false);
                    Debug.Log("lostObject deactivated");
                }
            }
            gameObject.SetActive(true);
        }
    

        public void SetTurnData(bool isOnlineMultiplayer, bool isWithAi, PLAYERS turn, bool isLocalWon = false)
        {
//            Debug.Log("SetTurnData called with values -> isOnlineMultiplayer: " + isOnlineMultiplayer + ", isWithAi: " + isWithAi + ", turn: " + turn + ", isLocalWon: " + isLocalWon);
            if (isMtTurnAnnounced)
            {
//                Debug.Log("Turn already announced. Returning early.");
                return;
            }
            isMtTurnAnnounced = true;
            Debug.Log("isMtTurnAnnounced set to true");
            if (isOnlineMultiplayer)
            {
                Debug.Log("Inside Online Multiplayer branch");
                if (!isLocalWon)
                {
                    Debug.Log("Local player did NOT win the toss");
                    wonObj.SetActive(false);
                    Debug.Log("wonObj deactivated");
                    lostObject.SetActive(true);
                    Debug.Log("lostObject activated");
                    turnText.text = "You Lost The Toss";
                    Debug.Log("turnText set to 'You Lost The Toss'");
                }
                else
                {
                    Debug.Log("Local player WON the toss");
                    turnText.text = "You Won The Toss";
                    Debug.Log("turnText set to 'You Won The Toss'");
                    wonObj.SetActive(true); Debug.Log("wonObj activated");
                    lostObject.SetActive(false);
                    Debug.Log("lostObject deactivated");
                }
            }
            else
            {
                Debug.Log("Inside Offline branch");
                if (isWithAi)
                {
                    Debug.Log("Game is with AI");
                    if (turn == PLAYERS.PLAYER2)
                    {
                        Debug.Log("Turn is PLAYER2 (AI won)");
                        turnText.text = "You Lost The Toss";
                        Debug.Log("turnText set to 'You Lost The Toss'");
                        wonObj.SetActive(false);
                        Debug.Log("wonObj deactivated");
                        lostObject.SetActive(true);
                        Debug.Log("lostObject activated");
                    }
                    else
                    {
                        Debug.Log("Turn is PLAYER1 (Local won)");
                        turnText.text = "You Won The Toss";
                        Debug.Log("turnText set to 'You Won The Toss'");
                        wonObj.SetActive(true); Debug.Log("wonObj activated");
                        lostObject.SetActive(false); Debug.Log("lostObject deactivated");
                    }
                }
                else
                {
                    Debug.Log("Game is with another local player (No AI)");
                    if (turn == PLAYERS.PLAYER2)
                    {
                        Debug.Log("Turn is PLAYER2 (Player Two won)");
                        turnText.text = "Player Two Won the Toss";
                        Debug.Log("turnText set to 'Player Two Won the Toss'");
                        wonObj.SetActive(false); Debug.Log("wonObj deactivated");
                        lostObject.SetActive(true); Debug.Log("lostObject activated");
                    }
                    else
                    {
                        Debug.Log("Turn is PLAYER1 (Player One won)");
                        turnText.text = "Player One Won the Toss";
                        Debug.Log("turnText set to 'Player One Won the Toss'");
                        wonObj.SetActive(true); Debug.Log("wonObj activated");
                        lostObject.SetActive(false); Debug.Log("lostObject deactivated");
                    }
                }
            }
            gameObject.SetActive(true); Debug.Log("This gameObject activated");
        }
    //     public void SetTurnData(bool isOnlineMultiplayer, bool isWithAi, PLAYERS turn, bool isLocalWon = false)
    //     {
    //         if (isMtTurnAnnounced)
    //         {
    //             return;
    //         }
    //         isMtTurnAnnounced = true;
    //         if (isOnlineMultiplayer)
    //         {

    //             if (!isLocalWon)
    //             {
    //                 wonObj.SetActive(false);
    //                 lostObject.SetActive(true);
    //                 turnText.text = "You Lost The Toss";
    //             }
    //             else
    //             {
    //                 turnText.text = "You Won The Toss";
    //                 wonObj.SetActive(true);
    //                 lostObject.SetActive(false);
    //             }
    //         }
    //         else
    //         {
    //             if (isWithAi)
    //             {

    //                 if (turn == PLAYERS.PLAYER2)
    //                 {
    //                     turnText.text = "You Lost The Toss";
    //                     wonObj.SetActive(false);
    //                     lostObject.SetActive(true);
    //                 }
    //                 else
    //                 {
    //                     turnText.text = "You Won The Toss";
    //                     wonObj.SetActive(true);
    //                     lostObject.SetActive(false);
    //                 }
    //             }
    //             else
    //             {
    //                 if (turn == PLAYERS.PLAYER2)
    //                 {
    //                     turnText.text = "Player Two Won the Toss";
    //                     wonObj.SetActive(false);
    //                     lostObject.SetActive(true);
    //                 }
    //                 else
    //                 {
    //                     turnText.text = "Player One Won the Toss";
    //                     wonObj.SetActive(true);
    //                     lostObject.SetActive(false);

    //                 }
    //             }
    //         }

    //         gameObject.SetActive(true);
    //     }

    }
}
