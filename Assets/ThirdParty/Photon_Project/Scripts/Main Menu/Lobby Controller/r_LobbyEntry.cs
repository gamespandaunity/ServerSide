using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
 
 

public class r_LobbyEntry : MonoBehaviour
{
    /// <summary>
    /// This is our lobby entry.
    /// This is used for the player list UI in the lobby.
    /// </summary>

    #region Variables
    //Photon Removal [Header("Lobby Player UI")]
    //Photon Removal  public Player m_PhotonPlayer;

    [Header("Lobby Player UI")]
    public Text m_PlayerNameText;
    #endregion

    #region Actions
    //public void SetupLobbyPlayer(Player _Player)
    //{
    //    m_PhotonPlayer = _Player;
    //    m_PlayerNameText.text = m_PhotonPlayer.NickName;                                                                                                 //Photon Removal
    //}
    #endregion
}
