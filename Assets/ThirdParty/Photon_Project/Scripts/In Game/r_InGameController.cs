using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
 
 
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class r_InGameController : MonoBehaviour
{
    public static r_InGameController instance;

    #region Variables
    [Header("Player Prefab")]
    public GameObject m_PlayerPrefab;

    [Header("SpawnPoints")]
    public bool m_Spawned;
    public Transform[] m_SpawnPoints;

    [Header("UI")]
    public Button m_SpawnButton;
    public Button m_LeaveButton;
    public Button m_EndGameButton;
    #endregion

    #region Unity Calls
    private void Awake()
    {
        if (instance)
        {
            Destroy(instance);
            Destroy(instance.gameObject);
        }

        instance = this;
        HandleButtons();
    }

    private void HandleButtons()
    {
        CheckMasterClient();

        m_SpawnButton.onClick.AddListener(delegate { SpawnPlayer(); r_AudioController.instance.PlayClickSound(); });
        //Photon Removal  m_LeaveButton.onClick.AddListener(delegate { LeaveRoom(); r_AudioController.instance.PlayClickSound(); } );
        m_EndGameButton.onClick.AddListener(delegate { StartCoroutine(EndGame()); m_EndGameButton.interactable = false; r_AudioController.instance.PlayClickSound(); });
    }
    #endregion

    /// <summary>
    /// If a player left the room, we are checking the masterclient again and making the button interactable or not.
    /// </summary>

    #region Masterclient
    public void CheckMasterClient()
    {
        //Photon Removal   if (PhotonNetwork.LocalPlayer.IsMasterClient)
        //Photon Removal   m_EndGameButton.interactable = true; else m_EndGameButton.interactable = false;
    }
    #endregion

    /// <summary>
    /// Spawning the player
    /// </summary>

    #region Spawning
    public void SpawnPlayer()
    {
        if (m_Spawned) return;

        Transform m_SpawnPoint = m_SpawnPoints[Random.Range(0, m_SpawnPoints.Length)];

        if (m_SpawnPoint)
        {
            //Photon Removal     GameObject _Player = (GameObject)PhotonNetwork.Instantiate("Player/" + m_PlayerPrefab.name, m_SpawnPoint.position, m_SpawnPoint.rotation, 0);
            //Photon Removal     _Player.GetComponent<r_CharacterConfig>().SetupLocalPlayer();
        }

        m_Spawned = true;
        m_SpawnButton.interactable = false;
    }
    #endregion

    /// <summary>
    /// Leave the current room.
    /// </summary>

    #region Room
    private IEnumerator EndGame()
    {
        //Hashtable _State = new Hashtable(); _State.Add("RoomState", "InLobby");                            //Photon Removal
        //PhotonNetwork.CurrentRoom.SetCustomProperties(_State);

        yield return new WaitForSeconds(3f);

        //if (PhotonNetwork.IsMasterClient)
        //    PhotonNetwork.LoadLevel(0);
    }

    //Photon Removal  private void LeaveRoom() => PhotonNetwork.LeaveRoom();
    #endregion
}