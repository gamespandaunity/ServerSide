using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class CalculateNextRewardTime : MonoBehaviour
{

    public TextMeshProUGUI timeRemainingText; // Assign this in the Unity Inspector
    public static string NextRemainingTime_;

    void Update()
    {
        // Get the current time
        DateTime now = DateTime.Now;

        // Calculate the next midnight (12:00 AM)
        DateTime nextMidnight = now.Date.AddDays(1); // .Date gives today's date at 12:00 AM, so we add 1 day

        // Calculate the remaining time until midnight
        TimeSpan remainingTime = nextMidnight - now;

        // Format the remaining time into hours, minutes, and seconds
        string timeRemainingFormatted = string.Format("{0:D2}:{1:D2}:{2:D2}",
            remainingTime.Hours,
            remainingTime.Minutes,
            remainingTime.Seconds

        );
        NextRemainingTime_ = remainingTime.Hours.ToString();

        // Display the remaining time
        timeRemainingText.text = "Time until midnight: " + timeRemainingFormatted;
    }
}
