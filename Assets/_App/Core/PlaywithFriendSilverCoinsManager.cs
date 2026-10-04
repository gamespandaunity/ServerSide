using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using static GameTypeSelection;

public class PlaywithFriendSilverCoinsManager : MonoBehaviour
{
    public Text playwithsilverCoins;

    public Image logo;
    public Image coinImg, headerImg , Bg;
    public Sprite goldHeader, silverHeader;
    private int functionNumber;
    public GameObject InternetConnectionPanel;
    public TMP_Text msg;
   
    private void OnEnable()
    {
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);
        SpritesManager.Instance.spritesScriptable.CoinSpriteChangerAccordingToDecision(coinImg);
        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(Bg);
        if (staticVariables.isgoldcoins)
        {
            headerImg.sprite = goldHeader;
        }
        else
        {
            headerImg.sprite= silverHeader;
        }
    }
    private void Start()
    {
        if (Screen.orientation == ScreenOrientation.Portrait)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        if (staticVariables.isgoldcoins)
        {
            playwithsilverCoins.text = "Play With Friend";
        }
        else
        {
            playwithsilverCoins.text = "Play With Friend";

        }
    }


    public void BackBtntoModeSelection()
    {
        staticVariables.isInviteFriendAndPlay = false;
        if (ApiAndRoomManager.currentGameId == 1)
        {

            SceneLoaderUtility.LoadScene("Start");
        }
        else if (ApiAndRoomManager.currentGameId == 2)
        {
            SceneLoaderUtility.LoadScene("CarromMainMenuScene");
        } else if (ApiAndRoomManager.currentGameId == 6)
        {
            SceneLoaderUtility.LoadScene("Main_Menu");
        }
    }

    public void inviteFriendAndPlay()
    {

            GameTypeSelection.gameTypeEnum = GameTypeSelection._GameTypeEnum.DirectInvite;
            staticVariables.screenstatus = "inviteFriendAndPlay";
            staticVariables.isInviteFriendAndPlay = true;
            //staticVariables.isSearchFriendIdAndEmailAddress = false;
            SoundManagerMain.instance.ClickSoundPlay();
            staticVariables.headerText = " Invite Friend And Play ";
        
    }

    public void searchFriendIdAndEmailAddressBtn()
    {

            GameTypeSelection.gameTypeEnum = GameTypeSelection._GameTypeEnum.SocialInvite;

            staticVariables.screenstatus = "searchFriendIdAndEmailAddress";
      //      staticVariables.isSearchFriendIdAndEmailAddress = true;
            staticVariables.isInviteFriendAndPlay = false;        
            SoundManagerMain.instance.ClickSoundPlay();
            //print("  email friend true done");
        

    }

    public void OpenChallengeBtn()
    {

            GameTypeSelection.gameTypeEnum = GameTypeSelection._GameTypeEnum.OpenChallenge;

        //    staticVariables.isSearchFriendIdAndEmailAddress = false;
            staticVariables.isInviteFriendAndPlay = false;
            staticVariables.screenstatus = "OpenChallenge";
            SoundManagerMain.instance.ClickSoundPlay();
            staticVariables.headerText = " Challenges ";
        
    }
    void CheckInternetConnection()
    {
        // Check the current internet reachability status
            msg.text = "No Internet Connection Please Connect your Internet and try Again";
            //Debug.Log("No internet connection available.");

            // If no internet connection, enable the panel or take appropriate action
            // For example:
            InternetConnectionPanel.SetActive(true);
        
    }



}
