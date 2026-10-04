using UnityEngine;
using System.Collections;
//using Photon.Chat;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using Photon;
using AssemblyCSharp;
using LudoGame;

public class PhotonChatListener : MonoBehaviour
{

    private Animator animator;
    public Text text;
    private string senderID;
    private string roomName;
    // "invited"
    // "accepted"
    public string type;
    public GameObject okButton;
    public GameObject rejectButton;
    public GameObject acceptButton;
    public GameObject matchPlayersCanvas;
    public GameObject friendsCanvas;
    public GameObject menuCanvas;
    public GameObject gameTitle;
    public GameObject payoutCoinsText;
    bool leftRoom = false;
    bool Joined = false;
    // Use this for initialization
    void Start()
    {
     ///   LudoGame.GameManager.Instance.invitationDialog = this.gameObject;
     //   animator = GetComponent<Animator>();

    }

    public void showInvitationDialog(int type, string name, string id, string room, int tableNumber)
    {

        if (PlayerPrefs.GetInt(StaticStrings.PrivateRoomKey, 0) == 0)
        {
            leftRoom = false;
            Joined = false;

            payoutCoinsText.GetComponent<Text>().text = "" + LudoGame.GameManager.Instance.payoutCoins;
            rejectButton.SetActive(true);
            acceptButton.SetActive(true);
            okButton.SetActive(false);

            this.type = "invited";
            senderID = id;
            roomName = room;

            text.text = name + " invite you to private room.";
            animator.Play("InvitationDialogShow");
        }
        else
        {
            Debug.Log("Invitations OFF");
        }



    }



    //public override void OnConnectedToMaster()
    //{
    //    if (!Joined && leftRoom)
    //    {
    //        JoinRoom("accepted");
    //        Joined = true;
    //    }
    //}

    public void JoinRoom(string a)
    {

        if (a.Equals("accepted"))
        {

            Debug.Log("Trying to join room: " + roomName);
            if (LudoGame.GameManager.Instance.myPlayerData.GetCoins() >= LudoGame.GameManager.Instance.payoutCoins)
            {
             //   PhotonNetwork.JoinRoom(roomName);
                if (LudoGame.GameManager.Instance.type != MyGameType.Private)
                {
                    LudoGame.GameManager.Instance.facebookManager.startRandomGame();
                }
                else
                {
                    if (LudoGame.GameManager.Instance.JoinedByID)
                    {
                        Debug.Log("Joined by id!");

                        LudoGame.GameManager.Instance.matchPlayerObject.GetComponent<SetMyData>().MatchPlayer();
                    }
                    else
                    {
                        Debug.Log("Joined and created");
                        LudoGame.GameManager.Instance.playfabManager.CreatePrivateRoom();
                        LudoGame.GameManager.Instance.matchPlayerObject.GetComponent<SetMyData>().MatchPlayer();
                    }

                }
            }
            else
            {
                LudoGame.GameManager.Instance.dialog.SetActive(true);
            }
        }

    }

    public void hideDialog(string a)
    {
        LudoGame.GameManager.Instance.type = MyGameType.Private;

        LudoGame.GameManager.Instance.JoinedByID = true;

        //if (PhotonNetwork.inRoom)
        //{
        //    leftRoom = true;
        //    PhotonNetwork.LeaveRoom();
        //}
        //else
        //{
        //    JoinRoom(a);
        //}


        animator.Play("InvitationDialogHide");
    }

}
