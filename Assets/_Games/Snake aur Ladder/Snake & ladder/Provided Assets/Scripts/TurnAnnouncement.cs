using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Snake_Ladder
{
    public class TurnAnnouncement : MonoBehaviour
    {
        public TextMeshProUGUI turnText;

        bool isMtTurnAnnounced = false;

        public GameObject wonObj;
        public GameObject lostObject;


        public void SetTurnData(bool isOnlineMultiplayer, bool isWithAi, PLAYERS turn, bool isLocalWon = false)
        {
            if (isMtTurnAnnounced)
            {
                return;
            }
            isMtTurnAnnounced = true;
            if (isOnlineMultiplayer)
            {

                if (!isLocalWon)
                {
                    wonObj.SetActive(false);
                    lostObject.SetActive(true);
                    turnText.text = "You Lost The Toss";
                }
                else
                {
                    turnText.text = "You Won The Toss";
                    wonObj.SetActive(true);
                    lostObject.SetActive(false);
                }
            }
            else
            {
                if (isWithAi)
                {

                    if (turn == PLAYERS.PLAYER2)
                    {
                        turnText.text = "You Lost The Toss";
                        wonObj.SetActive(false);
                        lostObject.SetActive(true);
                    }
                    else
                    {
                        turnText.text = "You Won The Toss";
                        wonObj.SetActive(true);
                        lostObject.SetActive(false);
                    }
                }
                else
                {
                    if (turn == PLAYERS.PLAYER2)
                    {
                        turnText.text = "Player Two Won the Toss";
                        wonObj.SetActive(false);
                        lostObject.SetActive(true);
                    }
                    else
                    {
                        turnText.text = "Player One Won the Toss";
                        wonObj.SetActive(true);
                        lostObject.SetActive(false);

                    }
                }
            }

            gameObject.SetActive(true);
        }

    }
}