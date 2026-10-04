using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class TeenPattiPlayer : NetworkBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text turnTimerText;
    public Image highlightImage;
    public GameObject actionPanel;

    private int playerIndex;
    private bool isMyTurn;
    private float turnEndTime;

    private void Start()
    {
        if (netIdentity.isOwned)
        {
            // Set the player name and UI elements for the local player
            highlightImage.enabled = true;
        }
        else
        {
            // Set the player name for the remote players
            highlightImage.enabled = false;
        }
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    public void Highlight()
    {
        highlightImage.enabled = true;
    }

    public void Unhighlight()
    {
        highlightImage.enabled = false;
    }

    public void StartTurn(int duration)
    {
        isMyTurn = netIdentity.isOwned;
        turnEndTime = Time.time + duration;
        turnTimerText.gameObject.SetActive(true);
        UpdateUI();
    }

    public void EndTurn()
    {
        isMyTurn = false;
        turnTimerText.gameObject.SetActive(false);
        //actionPanel.SetActive(false);
        TeenPattiGameManager.Instance.ActionPanel.SetActive(false);
        UpdateUI();
    }

    public void OnActionSelected(string action)
    {
        // TODO: Handle the player action and update the game state
    }

    private void Update()
    {
        if (isMyTurn)
        {
            float remainingTime = Mathf.Max(turnEndTime - Time.time, 0f);
            if (remainingTime > 0f)
            {
                turnTimerText.text = "Time left: " + remainingTime.ToString("0.0") + "s";
            }
            else
            {
                // Time's up, end the turn
                //photon removal  photonView.RPC("RPC_EndTurn", RpcTarget.MasterClient, playerIndex);
            }
        }
    }
    // [Command(requiresAuthority = false)]
    // public void CmdEndTurn(int playerIndex)
    // {
    //     RpcEndTurn(playerIndex);
    // }

    // [ClientRpc]
    // public void RpcEndTurn(int playerIndex)
    // {
    //     RPC_EndTurn(playerIndex);
    // }
    private void UpdateUI()
    {
        // Update the UI elements for the active player
        if (isMyTurn && netIdentity.isOwned)
        {
            TeenPattiGameManager.Instance.ActionPanel.SetActive(true);
            turnTimerText.text = "Time left: " + turnEndTime.ToString("0.0") + "s";      //photon removal
        }
        else
        {
            TeenPattiGameManager.Instance.ActionPanel.SetActive(false);
            turnTimerText.text = "";
        }
    }
}
//This code assumes that you have assigned the player name text, turn timer text, highlight image, and action panel game objects to the corresponding public fields in the TeenPattiPlayer component. The StartTurn, EndTurn, and UpdateUI methods handle the UI updates and turn timer for each player's turn, while the OnActionSelected method handles the player's action selection and sends the action to the game manager via Photon RPCs. You can customize this code to implement your own player actions and UI elements, and add more network events and error handling as needed.




