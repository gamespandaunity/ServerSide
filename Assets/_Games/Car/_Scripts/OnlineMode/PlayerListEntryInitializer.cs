namespace CarRace 
{
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
using TMPro;
public class PlayerListEntryInitializer : MonoBehaviour
{

    [Header("UI References")]
    public TextMeshProUGUI playerNameText;
    public Button playerReadyButton;
    public Image playerReadyImage;
    private bool isplayerReady = false;
    public void Initialize(int playerID, string playerName)
    {
        playerNameText.text = playerName;

            //Photon Removal  if (PhotonNetwork.LocalPlayer.ActorNumber != playerID)
            {
                playerReadyButton.gameObject.SetActive(false);
        }
            //Photon Removal  else
            {
                //ExitGames.Client.Photon.Hashtable initialProps = new ExitGames.Client.Photon.Hashtable() { { "PR", isplayerReady } };
                //Photon Removal    PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);

                playerReadyButton.onClick.AddListener(() => {

                isplayerReady = !isplayerReady;
                SetPlayerReady(isplayerReady);

               // ExitGames.Client.Photon.Hashtable newProps = new ExitGames.Client.Photon.Hashtable() { { "PR", isplayerReady } };
                    //Photon Removal      PhotonNetwork.LocalPlayer.SetCustomProperties(newProps);
                });
        }
    }

    public void SetPlayerReady(bool playerReady)
    {
        playerReadyImage.enabled = playerReady;

        if (playerReady)
        {
            playerReadyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Cancel!";
        }
        else
        {
            playerReadyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
        }
    }
}

}