using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MatchHandler
{
    public enum MATCH
    {
        TeenPatti = 0,
        OffLine = 1,
        Poker = 2,
    };

    public static MATCH CurrentMatch = MATCH.OffLine;

    public static bool IsTeenPatti()
    {
        return CurrentMatch == MATCH.TeenPatti;
    }

    public static bool IsPoker()
    {
        return CurrentMatch == MATCH.Poker;
    }
    public static bool isOffline()
    {
        return CurrentMatch == MATCH.OffLine;
    }




}
