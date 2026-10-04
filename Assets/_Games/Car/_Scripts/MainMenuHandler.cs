namespace CarRace 
{
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Michsky.LSS;
using TMPro;

public class MainMenuHandler : MonoBehaviour
{
    public static MainMenuHandler _Instance;

    [Header("Main Screen"), Space(3)]
    [SerializeField] GameObject MainMenuUI;
    [SerializeField] GameObject ErrorPopUp;
    [SerializeField] TextMeshProUGUI errorInfoText;
    [SerializeField] GameObject quitPopUp;

    [Header("Settings Screen"), Space(3)]
    [SerializeField] GameObject SettingsMenuUI;


    [Header("Map Selection Screen"), Space(3)]
    [SerializeField] GameObject MapSeletionMenuUI;


    [Header("Game Settings Screen"), Space(3)]
    [SerializeField] GameObject GameSettingsMenuUI;

    /// <summary>
    /// Multiplayer Mode UIs
    /// </summary>
    
    [Space(3), Header("Game Options")]
    [SerializeField] GameObject GameOptionsUI;
    [SerializeField] TMP_InputField privateRoomInput;


    [Space(3), Header("Lobby")]
    [SerializeField] GameObject LobbyUI;

    [Space(3), Header("Loading Sreen")]
    [SerializeField] GameObject loadingScreen;


    private void Awake()
    {
        _Instance = this;
    }
    public void CloseGame()
    {
        Application.Quit();
    }
}

}