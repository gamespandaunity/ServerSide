using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LudoGame;
//using Photon.Pun;
//using Photon.Realtime;

public class EnterPrivateCodeDialogController : MonoBehaviour
{

    public GameObject inputField;
    public GameObject confirmationText;
    public GameObject joinButton;
    private Button join;
    private InputField field;
    public GameObject GameConfiguration;
    public GameObject failedDialog;
    void OnEnable()
    {
        if (field != null)
            field.text = "";
        if (confirmationText != null)
            confirmationText.SetActive(false);
        if (join != null)
            join.interactable = false;
    }

    // Use this for initialization
    void Start()
    {
        field = inputField.GetComponent<InputField>();
        join = joinButton.GetComponent<Button>();
        join.interactable = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void onValueChanged()
    {

        if (field.text.Length < 8)
        {
            confirmationText.SetActive(true);
            join.interactable = false;
        }
        else
        {
            confirmationText.SetActive(false);
            join.interactable = true;
        }
    }

    public void JoinByRoomID()
    {
        LudoGame.GameManager.Instance.JoinedByID = true;
        LudoGame.GameManager.Instance.payoutCoins = 0;
        string roomID = field.text;
        //PhotonNetwork.JoinRoom(roomID);
    GameConfiguration.GetComponent<GameConfigrationController>().startGame();
   
        




    }
}
