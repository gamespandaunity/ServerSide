using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomState
{
    public enum STATE
    {
        WaitingForPlayers = 0,
        GameIsStarting = 1,
        CardDistributing = 2,
        GameIsPlaying = 3,
        GameSideShow = 4,
        WaitingForResults = 5,
        ShowingResults = 6,
        ABFirstTurn = 7,
        ABSecondTurn = 8,

    };
}
