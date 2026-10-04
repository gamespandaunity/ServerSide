using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LudoGame;

public class DialogNoPlayersCtrl : MonoBehaviour
{
    public GameObject dialogTxt;
    // Start is called before the first frame update
    void Start()
    {
        LudoGame.GameManager.Instance.dialogNoPlayers = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
