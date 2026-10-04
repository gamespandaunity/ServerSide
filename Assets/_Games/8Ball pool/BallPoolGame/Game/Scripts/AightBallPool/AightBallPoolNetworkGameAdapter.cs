using NetworkManagement;
using BallPool;
using System.Collections.Generic;
using UnityEngine;
//using PlayFab.ClientModels;
using System;
using System.Reflection;

public class AightBallPoolNetworkGameAdapter : NetworkGameAdapter
{
    public static bool is3DGraphics = false;
    public static bool isSameGraphicsMode;
    private HomeMenuManager _homeMenuManager;
    public bool IsInit;
    public HomeMenuManager homeMenuManager
    {
        get { return _homeMenuManager; }
    }
    public AightBallPoolNetworkGameAdapter(HomeMenuManager homeMenuManager)
    {
        _homeMenuManager = homeMenuManager;
    }
    public void SetTurn(int turnId)
    {
        if (turnId == 0)
            BallPoolPlayer.turnId = staticVariables.UserProfiledata.user._id;
        else
        {
            BallPoolPlayer.turnId = int.Parse(EightBallPoolNetworkManager.opponentPlayer.userId);

        }
        BallPoolPlayer.turnId = turnId;
    }
    public void OnMainPlayerLoaded(int playerId, string name, int coins, object avatar, string avatarURL, int prize)
    {
        if (!BallPoolPlayer.initialized)
        {
            BallPoolPlayer.players = new BallPoolPlayer[2];
            BallPoolPlayer.playersCount = 2;
        }
        BallPoolPlayer.players[0] = new AightBallPoolPlayer(playerId, name, coins, avatar, avatarURL);
    }
    public void OnUpdateMainPlayerName(string name)
    {
        AightBallPoolPlayer.mainPlayer.name = name;
    }
    public void OnUpdatePrize(int prize)
    {
    }    
    public void OnUpdateOver(int Over)
    {
        SelectOver.SelectedOver = Over;
    }
    public void OnGoToPlayWithAI(int playerId, string name, int coins, object avatar, string avatarURL)
    {
        //"Idhar bhi aahr hai".Show();
        BallPoolGameLogic.playMode = BallPool.PlayMode.PlayerAI;
        ////Debug.Log("wow :" + NetworkManager.mainPlayer.coins);
        BallPoolPlayer.players[0].SetCoins(EightBallPoolNetworkManager.mainPlayer.coins);
        BallPoolPlayer.players[1] = new AightBallPoolPlayer(1, name, coins, avatar, avatarURL);
        Dictionary<string, string> betRequestDict = new Dictionary<string, string>();

        // Assign values to the BetRequest fields
        betRequestDict["first_player"] = staticVariables.UserProfiledata.user._id.ToString();
        betRequestDict["second_player"] = "ai";
        betRequestDict["game_id"] = ApiAndRoomManager.currentGameId.ToString();
        betRequestDict["remark"] = "Multipler with Golden Coins";
        betRequestDict["screenstatus"] = staticVariables.screenstatus;

        if (staticVariables.isgoldcoins)
        {
            betRequestDict["gold"] = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
            betRequestDict["silver"] = "0";
            betRequestDict["coins_type"] = "gold";
        }
        else
        {
            betRequestDict["silver"] = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
            betRequestDict["gold"] = "0";
            betRequestDict["coins_type"] = "silver";
        }

        ApiAndRoomManager._instance.PlaceAIChallenge(betRequestDict);
        //ApiAndRoomManager._instance.loadSceenName = "SettingScene";
        staticVariables.playOnceAi = false;

        //homeMenuManager.GoToPlay();
    }

  
  
}
