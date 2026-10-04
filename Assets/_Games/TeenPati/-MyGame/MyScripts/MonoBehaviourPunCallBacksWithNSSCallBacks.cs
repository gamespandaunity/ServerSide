using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TeenPattiGame
{
  public class MonoBehaviourPunCallBacksWithNSSCallBacks : WrapperEvents
  {
    private void OnEnable()
    {
      //  RegisterRoomEvents();
    }

    private void OnDisable()
    {
      //  UnregisterRoomEvents();
    }


    public void RegisterRoomEvents()
    {
      OnEventReceivedWaitingForPlayers += OnRoomStateChangeToWaitingForPlayers;
      OnEventReceivedGameIsStarting += OnRoomStateChangeToGameIsStarting;
      OnEventReceivedCardDistributing += OnRoomStateChangeToCardDistributing;
      OnEventReceivedGameIsPlaying += OnRoomStateChangeToGameIsPlaying;
      OnEventReceivedWaitingForResults += OnRoomStateChangeToWaitingForResults;
      OnEventReceivedShowingResults += OnRoomStateChangeToShowingResults;
      OnEventReceivedSideShow += OnRoomStateChangeToSideShow;
      OnEventReceivedABFirstTurn += OnRoomStateChangeToABFirstTurn;
      OnEventReceivedABSecondTurn += OnRoomStateChangeToABSecondTurn;
    }

    public void UnregisterRoomEvents()
    {
      OnEventReceivedWaitingForPlayers -= OnRoomStateChangeToWaitingForPlayers;
      OnEventReceivedGameIsStarting -= OnRoomStateChangeToGameIsStarting;
      OnEventReceivedCardDistributing -= OnRoomStateChangeToCardDistributing;
      OnEventReceivedGameIsPlaying -= OnRoomStateChangeToGameIsPlaying;
      OnEventReceivedWaitingForResults -= OnRoomStateChangeToWaitingForResults;
      OnEventReceivedShowingResults -= OnRoomStateChangeToShowingResults;
      OnEventReceivedSideShow -= OnRoomStateChangeToSideShow;
      OnEventReceivedABFirstTurn -= OnRoomStateChangeToABFirstTurn;
      OnEventReceivedABSecondTurn -= OnRoomStateChangeToABSecondTurn;
    }


  }
}