using NetworkManagement;
 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SmallChallenge : MonoBehaviour
{
    public GameObject SilverGold_select, betInputField, createCHallengeButton, SelectGame_Panel, BetInput_SendChallenge;
    public TMP_InputField prizeInput;
    public delegate void SmallChallengeSelection();
    public static SmallChallengeSelection GameSelect, SilverGoldChallenge_Selected;
    public Sprite silverBtn, GoldBtn;
    public Text FriendName;
    public FriendsModel frienddata;
    public GameObject overPanel;
    private void OnEnable()
    {
        SelectGame_Panel.SetActive(true);
        GameSelect += OnSelectGame;
        SilverGoldChallenge_Selected += SelectedCoin;
    
    }
    private void Start()
    {
       
    }
    private void OnSelectGame()
    {
        //BetInput_SendChallenge.SetActive(true);
        SilverGold_select.gameObject.SetActive(true);
        SelectGame_Panel.SetActive(false);
        //APIManager.gameid.Show("Game ID");
        if (ApiAndRoomManager.currentGameId == 13)
        {
            overPanel.SetActive(true);
        }
        else
        {
            overPanel.SetActive(false);
        }
    }
    private void SelectedCoin()
    {
        SelectGame_Panel.SetActive(false);
        SilverGold_select.SetActive(false);
        betInputField.SetActive(true);
        createCHallengeButton.SetActive(true);
        BetInput_SendChallenge.SetActive(true);
        createCHallengeButton.GetComponent<Image>().sprite = staticVariables.isgoldcoins ? GoldBtn : silverBtn;
                   //APIManager.gameid.Show("Game ID");
        if (ApiAndRoomManager.currentGameId == 13)
        {
            overPanel.SetActive(true);
        }
        else
        {
            overPanel.SetActive(false);
        }
    }

    //void OnOpenChallengeCreated(Photon.Realtime.Room room)
    //{
    //    PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);
    //    ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;                                                                                                   //Photon Removal
    //    //print("roomlistmanager 00000 " + APIManager.instance.transaction_id + "::" + APIManager.instance.winnerlossBetId);
    //    HomeMenuManager.instance.yourChallengeLoadingIndicator.SetActive(false);
    //    //print("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    //    PhotonNetwork.LeaveRoom();
    //}
    public void CreateRoom()
    {
        //print(staticVariables.isgoldcoins + "=>- create room---");
        SoundManagerMain.instance.ClickSoundPlay();
        //Photon Removal   PunNetwork.HandleRoomJoined = null;
        //Photon Removal   PunNetwork.HandleRoomJoined += OnOpenChallengeCreated;
        string category = GamesSetterMenu.All_games_Ref.Find(x => (x.game_id == ApiAndRoomManager.currentGameId)).category;
        if (category.Contains("Games"))
        {
            EightBallPoolNetworkManager.mainPlayer.prize = int.Parse(prizeInput.text);
        }
        staticVariables.screenstatus = "sad";
        Dictionary<string, string> betRequestDict = new Dictionary<string, string>();
        // Assign values to the BetRequest fields
        betRequestDict["first_player"] = staticVariables.UserProfiledata.user._id.ToString();
        betRequestDict["second_player"] = frienddata._id.ToString();
        betRequestDict["game_id"] = ApiAndRoomManager.currentGameId.ToString();
        betRequestDict["remark"] = "Multipler with Golden Coins";
        betRequestDict["screenstatus"] = staticVariables.screenstatus;
        betRequestDict["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver";
        if (ApiAndRoomManager.currentGameId == 13)
        {
            //SelectOver.SelectedOver.Show("On Create Over");
            betRequestDict["overs"] = SelectOver.SelectedOver.ToString();
        }
        if (staticVariables.isgoldcoins)
        {
            betRequestDict["gold"] = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
            betRequestDict["silver"] = "0";
        }
        else
        {
            betRequestDict["silver"] = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
            betRequestDict["gold"] = "0";
        }

        ApiAndRoomManager._instance.create_Challenge_Invite(betRequestDict,
        successMessage =>
        {
            //print(staticVariables.isgoldcoins + "=>- 1reate success room---");
            ApiAndRoomManager._instance.ModifyUserBalance();
            RoomsListManager.instance.iscreatedChallangebtn = true;
            //print(staticVariables.isgoldcoins + "=>- 2reate success room---");
            ConstantsData_M.Log("Bet ID");
            //PhotonNetwork.CreateRoom()
            Destroy(this.gameObject);
        },
        failureMessage =>
        {

            ApiAndRoomManager._instance.DisplayError("failureMessage");

            //Debug.Log("Gold Bet Failed to Create: " + failureMessage);

        });
    }
    private void OnDisable()
    {
        GameSelect -= OnSelectGame;
        SilverGoldChallenge_Selected -= SelectedCoin;
        Destroy(this.gameObject);
    }
    public void CLOSE()
    {
        Destroy(this.gameObject);
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void SelectGameFunction(string id)
    {
        ApiAndRoomManager.currentGameId = int.Parse(id);
        //print(" NAME => SMALL =>" + frienddata.first_name);
        string name = frienddata.first_name + " " + frienddata.last_name;
        FriendName.text = name;
        if (Borderspanel.ChangeTitle != null)
        {

            Borderspanel.ChangeTitle("select challenge");
        }
        OnSelectGame();
    }

    public void SilverGoldSelect(bool isGold)// function on  buttons
    {
      string category = GamesSetterMenu.All_games_Ref.Find(x => (x.game_id == ApiAndRoomManager.currentGameId)).category;
        //print(category);    
        staticVariables.isgoldcoins = isGold;
        AightBallPoolNetworkGameAdapter.is3DGraphics = false;
        // if category casino  then direct create challenge
        if (category == "Casino")
        {
            CreateRoom();
        }
        else
        {

            if (Borderspanel.coinSelected != null)
            {

                Borderspanel.coinSelected(staticVariables.isgoldcoins);
                //print(staticVariables.isgoldcoins + "=>-----------------------");
            }
            if (Borderspanel.ChangeTitle != null)
            {

                Borderspanel.ChangeTitle("Create Challenge");
            }
            SoundManagerMain.instance.ClickSoundPlay();

            SelectedCoin();
        }
    }
    public void ActivatePanel(string panelName)
    {
        SilverGold_select.SetActive(SilverGold_select.name.Equals(panelName));
        SelectGame_Panel.SetActive(SelectGame_Panel.name.Equals(panelName));
        BetInput_SendChallenge.SetActive(BetInput_SendChallenge.name.Equals(panelName));
    }
}
