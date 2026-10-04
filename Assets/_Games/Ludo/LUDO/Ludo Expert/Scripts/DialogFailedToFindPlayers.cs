using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LudoGame;

public class DialogFailedToFindPlayers : MonoBehaviour
{
   void Start()
    {
        LudoGame.GameManager.Instance.dialogFailed = this;
    }

    public void ClosePlayerMatch(){
        LudoGame.GameManager.Instance.playfabManager.CloseGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
