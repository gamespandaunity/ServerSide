using BallPool;
using NetworkManagement;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class OnLooseConnection_LoosePanel : MonoBehaviour
{
    UserModel userModel;
    public TextMeshProUGUI  looserTotalBalance;
    public RawImage rawProfileImageWinner, rawProfileImageLooser;
    [SerializeField]
    private Text WinplayerNameText, looserPlayerNameText;
    private void OnEnable()
    {
        userModel = staticVariables.UserProfiledata;

        if (staticVariables.isgoldcoins)
        {
           // APIManager.instance.lossGoldenCoins();
            looserTotalBalance.text = userModel.user.gold_balance.ToString();
        }
        else
        {
            //APIManager.instance.lossSilverCoins();
            looserTotalBalance.text = userModel.user.silver_balance.ToString();
        }
    }  
    private void Start()
    {
        looserPlayerNameText.text = userModel.user.first_name;
         WinplayerNameText.text = GameManager.newOpponentName;
        rawProfileImageWinner.texture = staticVariables.opponentImage;
        rawProfileImageLooser.texture = staticVariables.ProfilePicture;
        //BallPoolPlayer winner = BallPoolPlayer.GetWinner(); 
        //if (winner.playerId == AightBallPoolPlayer.mainPlayer.playerId)//main player
        //{
        //    //-------- SET PROFILE IMAGES---------------
        //    rawProfileImageWinner.texture = staticVariables.apiSetImage;            
        //    if (staticVariables.opponentImage != null)
        //    {
        //        rawProfileImageLooser.texture = staticVariables.opponentImage;
        //    }
        //    else
        //    {
        //        rawProfileImageLooser.texture = scriptable.nullProfileImg;
        //    }
        //    WinplayerNameText.text = userModel.user.first_name;
        //    looserPlayerNameText.text = GameManager.newOpponentName;
        //}
        //else// other player
        //{                      
        //    if (staticVariables.opponentImage != null) // OPPONENT WINNER
        //    {
        //        rawProfileImageWinner.texture = staticVariables.opponentImage;
        //    }
        //    else
        //    {
        //        rawProfileImageWinner.texture = scriptable.nullProfileImg;
        //    }
        //    rawProfileImageLooser.texture = staticVariables.apiSetImage; // PLAYER LOOSER
        //    //-------- SET NAME -------------------------
        //    looserPlayerNameText.text = userModel.user.first_name;
        //    WinplayerNameText.text = GameManager.newOpponentName;
        //}
    }
    public void ClosePanelFunction()
    {
        if (RoomsListManager.instance.isbrowseChallenge)
        {
            SceneLoaderUtility.LoadScene("Home");
        }
        else
        {
            SceneLoaderUtility.LoadScene("MainmenuScene");
        }
        //print("status room list" + RoomsListManager.instance.isbrowseChallenge);
    }
}
