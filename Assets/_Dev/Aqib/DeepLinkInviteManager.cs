using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeepLinkInviteManager : MonoBehaviour
{
    public Image createChallenege;
    public GameObject CreateChallengePanel, SendChallengePanel, ChallengeCreateLoader;
    public InputField BetAmount;
    public Text PlayerName;
    public Button CreateChallengebtn;
    private void OnEnable()
    {
        Image createChallenge_Button_Image = CreateChallengebtn.GetComponent<Image>();
        SpritesManager.Instance.spritesScriptable.CreateCHallenge_SetPanel_Img(createChallenege);
        SpritesManager.Instance.spritesScriptable.CreateButton_ImgSetter(createChallenge_Button_Image);
        CreateChallengebtn.gameObject.SetActive(true);
        ChallengeCreateLoader.SetActive(false);
    }
    private void Start()
    {
        CreateChallengebtn.onClick.AddListener(() => { CreateRoom(); });
        PlayerName.text = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;
        BetAmount.onValueChanged.AddListener((string playerPrize) =>
        {
            HomeMenuManager.instance.UpdatePrizeStatus(int.Parse(playerPrize));
        });
        HomeMenuManager.instance.UpdatePrizeStatus(int.Parse(BetAmount.text));
    }
    public void CreateRoom()
    {
        if (SoundManagerMain.instance != null)
        {
            SoundManagerMain.instance.ClickSoundPlay();

        }
        UserModel userModel2 = staticVariables.UserProfiledata;        
      //  OnCreateChallengePanel.isPanelAnimated = true;
        CreateChallengebtn.gameObject.SetActive(false);
        ChallengeCreateLoader.SetActive(true);
        Dictionary<string, string> betRequestDict = new Dictionary<string, string>();

        // Assign values to the BetRequest fields
        betRequestDict["first_player"] = staticVariables.UserProfiledata.user._id.ToString();
        betRequestDict["game_id"] = ApiAndRoomManager.currentGameId.ToString();
        betRequestDict["remark"] = "Multipler with Golden Coins";
        betRequestDict["screenstatus"] = staticVariables.screenstatus;

        if (staticVariables.isgoldcoins)
        {
            betRequestDict["gold"] = BetAmount.text;
            betRequestDict["silver"] = "0";
        }
        else
        {
            betRequestDict["silver"] = BetAmount.text;
            betRequestDict["gold"] = "0";
        }

        ApiAndRoomManager._instance.PlaceChallenge(betRequestDict,
        successMessage =>
        {
            ApiAndRoomManager._instance.ModifyUserBalance();
            "bets ID should shown here".Show();
            staticVariables.playingroomsids = ApiAndRoomManager._instance.challenge_transaction_id;
            //CreateChallengePanel.SetActive(false);
            //SendChallengePanel.SetActive(true);
            if (staticVariables.isInviteFriendAndPlay) //RAR
            {
                RoomsListManager.instance.iscreatedChallangebtn = false;
            }

        },
        failureMessage =>
        {

            //Debug.Log("Gold Bet Failed to Create: " + failureMessage);
            CreateChallengebtn.gameObject.SetActive(true);
            ChallengeCreateLoader.SetActive(false);
        });
    }



}
