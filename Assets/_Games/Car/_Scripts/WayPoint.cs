namespace CarRace 
{
using System;
using System.Collections.Generic;
using UnityEngine;

public class WayPoint : MonoBehaviour
{

    bool isPassed = false;
    List<int> AiCarPassedList = new List<int>();
    /// <summary>
    /// If this is crossed, return TRUE, otherwise FALSE, and set it ISPassed to TRUE
    /// </summary>
    /// <returns></returns>
    /// 
    private void OnEnable()
    {
        EventManager.OnLapFinishEvent += OnLapFinished;
        EventManager.OnAILapCompleted += OnLapFinishedbyAI;
        
    }

    private void OnDisable()
    {
        EventManager.OnLapFinishEvent -= OnLapFinished;
        EventManager.OnAILapCompleted -= OnLapFinishedbyAI;
    }
    
    /// <summary>
    /// Methods For User Car
    /// </summary>
    void OnLapFinished()
    {
        isPassed = false;

    }
    public bool IsCarPassed()
    {
        if (isPassed)
            return true;
        isPassed = true;
        return false;
    }


    /// <summary>
    /// Methods For AI Cars
    /// </summary>
    /// <param name="playerID"></param>
    private void OnLapFinishedbyAI(int playerID)
    {
        if (AiCarPassedList.Contains(playerID))
            AiCarPassedList.Remove(playerID);
    }
    public bool IsCarPassed(byte playerID)
    {
        if (!AiCarPassedList.Contains(playerID))
        {
            AiCarPassedList.Add(playerID);
            return false;
        }
        return true;
    }

}

}