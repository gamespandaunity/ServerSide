using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WrapperEvents : EventsManager
{

    #region Room Events Virtual Functions

    public virtual void OnRoomStateChangeToWaitingForPlayers()
    {
        //Debug.LogError("2 bar chalta hai k nahi");
        Debug.Log("Base Waiting For Players Called");
    }

    public virtual void OnRoomStateChangeToGameIsStarting()
    {
        Debug.Log("Base Game Is Starting Called");
    }

    public virtual void OnRoomStateChangeToCardDistributing()
    {
        Debug.Log("Base Card Distributing Called");
    }

    public virtual void OnRoomStateChangeToGameIsPlaying()
    {
        Debug.Log("Base Game Is Playing Called");
    }
    public virtual void OnRoomStateChangeToSideShow()
    {
        Debug.Log("Base Side Show Is Called");
    }

    public virtual void OnRoomStateChangeToWaitingForResults(string param)
    {
        Debug.Log("Base Waiting For Results Called");
    }

    public virtual void OnRoomStateChangeToShowingResults()
    {
        Debug.Log("Base Showing Results Called");
    }

    public virtual void OnRoomStateChangeToABFirstTurn()
    {
        Debug.Log("Base Side Show Is Called");
    }

    public virtual void OnRoomStateChangeToABSecondTurn()
    {
        Debug.Log("Base Side Show Is Called");
    }

    #endregion


}
