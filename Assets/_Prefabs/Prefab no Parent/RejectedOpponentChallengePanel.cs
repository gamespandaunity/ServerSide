using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RejectedOpponentChallengePanel : MonoBehaviour
{
    public Image Logo;
    public Text playerNameTxtField;
    public Text rejectMsgResponseTxt;
    public RawImage rejectPlayerImg;

    [SerializeField] private string messageResposeSaver = "";
    public string MessageREsponseSaver
    {
        get { return messageResposeSaver; }
        set { messageResposeSaver = value; }
    }

    public static RejectedOpponentChallengePanel instance;



    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }



    private void Start()
    {
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(Logo);
    }


    public void ProfileData(string playername, string responsetxt, string profilesnap)
    {
        string saveName = playername;
        string responnse = responsetxt;


        if (saveName != string.Empty)
        {

            playerNameTxtField.text = saveName;
        }
        else
        {
            ConstantsData_M.Log("Null string on firebase notification");
        }

        string url = "https://api.freebackgroundremover.net/" + profilesnap;
        ServerConnection.DownloadSprite(url, DownloadedTexture =>
        {
            rejectPlayerImg.texture = DownloadedTexture;
            staticVariables.opponentImage = DownloadedTexture;
        });
    }

    public void closePanel()
    {
        WaitingPanelScript gb = FindObjectOfType<WaitingPanelScript>();

        if (gb != null)
        {
            Destroy(gb.gameObject);
        }
        Destroy(gameObject);
    }


}
