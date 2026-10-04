using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class EventsManager : NetworkBehaviour
{
    #region Room Delegates And Events
    public delegate void WaitingForPlayersDelegate();
    public delegate void GameIsStartingDelegate();
    public delegate void CardDistributingDelegate();
    public delegate void GameIsPlayingDelegate();
    public delegate void SideShowDelegate();
    public delegate void WaitingForResultsDelegate(string param);
    public delegate void ShowingResultsDelegate();
    public delegate void ABFirstTurnDelegate();
    public delegate void ABSecondTurnDelegate();


    public static event WaitingForPlayersDelegate OnEventReceivedWaitingForPlayers;
    public static event GameIsStartingDelegate OnEventReceivedGameIsStarting;
    public static event CardDistributingDelegate OnEventReceivedCardDistributing;
    public static event GameIsPlayingDelegate OnEventReceivedGameIsPlaying;
    public static event SideShowDelegate OnEventReceivedSideShow;
    public static event WaitingForResultsDelegate OnEventReceivedWaitingForResults;
    public static event ShowingResultsDelegate OnEventReceivedShowingResults;
    public static event ABFirstTurnDelegate OnEventReceivedABFirstTurn;
    public static event ABSecondTurnDelegate OnEventReceivedABSecondTurn;


    #endregion
    #region Triggering Room Events
    protected void TriggerStateWaitingForPlayers()
    {
        OnEventReceivedWaitingForPlayers?.Invoke();
    }

    protected void TriggerStateabFirstTurn()
    {
        OnEventReceivedABFirstTurn?.Invoke();
    }

    protected void TriggerStateabSecondTurn()
    {
        OnEventReceivedABSecondTurn?.Invoke();
    }

    protected void TriggerStateGameIsStarting()
    {
        OnEventReceivedGameIsStarting?.Invoke();
    }
    protected void TriggerStateCardDistributing()
    {
        OnEventReceivedCardDistributing?.Invoke();
        //photon removal Constants_M.Log("CardDisributing");
    }
    protected void TriggerStateGameIsPlaying()
    {
        OnEventReceivedGameIsPlaying?.Invoke();
    }
    protected void TriggerStateSideShow()
    {
        OnEventReceivedSideShow?.Invoke();
    }
    protected void TriggerStateWaitingForResults(string param)
    {
        OnEventReceivedWaitingForResults?.Invoke(param);
    }
    protected void TriggerStateShowingResults()
    {
        OnEventReceivedShowingResults?.Invoke();
    }

    #endregion


}