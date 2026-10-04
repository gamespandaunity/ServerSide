namespace CarRace 
{
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This Class will be used to control game events, like start, pause end show result etc
/// </summary>
public class EventManager
{
    /// <summary>
    /// Events that Also Pass integer as Parameter
    /// </summary>
    /// <param name="val"></param>
    public delegate void GameControlDelegateWithIntegerParamenter(int totalLaps=0, int totalPlayers=0);
    public static GameControlDelegateWithIntegerParamenter OnGameStartEvent;



    /// <summary>
    /// Event to call when an AI finishes a Lap, so that it will invoke the event and pass its Player ID
    /// </summary>
    /// <param name="tplayerID"></param>
    public delegate void GameControlDelegateForAILacomple(int tplayerID);
    public static GameControlDelegateForAILacomple OnAILapCompleted;

    /// <summary>
    /// Events With Out any paraments
    /// </summary>
    public delegate void GameControlDelegate();
    public static GameControlDelegate OnShowFinishCinematics;
    public static GameControlDelegate OnGameFinishEvent;
    public static GameControlDelegate OnGamePauseEvent;
    public static GameControlDelegate OnGameResumeEvent;
    public static GameControlDelegate OnLapFinishEvent;
    public static GameControlDelegate OnHalfLapCompleteEvent;
    public static GameControlDelegate OnPositionUpdateEvent;
    public static GameControlDelegate OnLevelCutSceneFinishEvent;
    public static GameControlDelegate OnShowResult;
    public static GameControlDelegate OnUpdateUserCoins;


    /// <summary>
    /// Events To Active Menu In the Main Menu Scene
    /// </summary>
    /// <param name="menuToActivate"> Pass the Main M eju and it activae that Menu</param>
    public delegate void MainMenuControlDelegate(GameObject menuToActivate);
    public static MainMenuControlDelegate OnActivateMenu;


    /// <summary>
    /// Event to Sent Players to Display
    /// </summary>
    /// <param name="playersData"></param>
    public delegate void SubmitPlayerData(List<PlayerDataForPositionSystem> playersData);
    public static SubmitPlayerData PrepareResults;


    /// <summary>
    /// Event to Start Multiplayer Game
    /// </summary>
    public delegate void StartMultiplayerGameDelegate();
    public static StartMultiplayerGameDelegate OnMultiplayergameStarted;


    /// <summary>
    /// Events to Check , Load and Show Ads
    /// </summary>
    /// <returns></returns>

    public delegate bool CheckAdLoaded();
    public static CheckAdLoaded OnCheckAdLoaded_Banner;
    public static CheckAdLoaded OnCheckAdLoaded_Interstitial;
    public static CheckAdLoaded OnCheckAdLoaded_Rewarded;

    public delegate void AdsEvents();
    public static AdsEvents OnLoadAd_Banner;
    public static AdsEvents OnLoadAd_Interstitial;
    public static AdsEvents OnLoadAd_Rewarded;
    public static AdsEvents OnShowAd_Banner;
    public static AdsEvents OnShowAd_Interstitial;
    public static AdsEvents OnShowAd_Rewarded;
    public static AdsEvents OnRewardGiven;
    public static AdsEvents OnInterstitialAdFinished;
    public static AdsEvents OnStopAds;
    public static AdsEvents OnHideBanner;



    /// <summary>
    /// Events for In App Purchasings
    /// </summary>
    /// <param name="purchaseID"></param>
    public delegate void IAPConsumableEvents(int coinID);
    public static IAPConsumableEvents OnPurachaseRequested_Consumable;
    public static IAPConsumableEvents OnPurachaseCompleted_Consumable;

    public delegate void IAPNonConsumableEvents();
    public static IAPNonConsumableEvents OnPurachaseRequested_RemoveAds;
    public static IAPNonConsumableEvents OnPurachaseCompleted_RemoveAds;
    public static IAPNonConsumableEvents OnPurachaseRequested_UnlockCars;
    public static IAPNonConsumableEvents OnPurachaseCompleted_UnlockCars;
    public static IAPNonConsumableEvents OnPurachaseRequested_UnlockMaps;
    public static IAPNonConsumableEvents OnPurachaseCompleted_UnlockMaps;

    public delegate void ToggleIAPButtons(bool removeds, bool unlockCars, bool unlockMaps);
    public static ToggleIAPButtons OnToggleIAPButtons;
}

}