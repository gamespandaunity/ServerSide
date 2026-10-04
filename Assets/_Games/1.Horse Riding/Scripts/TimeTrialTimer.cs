 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeTrialTimer : MonoBehaviour {
    [HideInInspector]
    public bool isTimerRunning;
    public Text TimerText;

    public void Update()
    {
        if (!isTimerRunning)
        {
            return;
        }

        float timer = (float)Time.time - LevelsManager.instance.raceStartTime;

        TimerText.text = string.Format("{0:#0}:{1:00}.{2:00}",
                    Mathf.Floor(timer / 60),//minutes
                    Mathf.Floor(timer) % 60,//seconds
                    Mathf.Floor((timer * 100) % 100));//miliseconds

                                                          // TimerText.text = string.Format("{0}", timer.ToString("n2"));
    }
    }
