using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class UiTurnCircle : MonoBehaviour
{
    public Image targetCircle;
    public float RoundTimer;

    bool startAnimating;
    float elapsedTime;

    bool UsesServerTimer()
    {
        return NetworkServer.active
            || NetworkClient.active
            || (Snake_Ladder.GameControllerNew.instance != null
                && Snake_Ladder.GameControllerNew.instance.isOnline);
    }

    void OnEnable()
    {
        // Online fill is driven by SnakeMirrorNetworkManager/GameControllerNew.
        // Do not reset or animate the same Image locally as a second timer source.
        if (UsesServerTimer())
        {
            startAnimating = false;
            return;
        }

        startAnimating = true;
        targetCircle.fillAmount = 1;
        elapsedTime = 0f;
    }

    void Update()
    {
        // Covers the case where this object enabled just before networking became active.
        if (UsesServerTimer())
        {
            startAnimating = false;
            return;
        }

        if (!startAnimating) return;

        // Animate the targetCircle from 1 to 0 in RoundTimer
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / RoundTimer;
        targetCircle.fillAmount = Mathf.Lerp(1, 0, progress);

        // Stop animating once the timer is complete
        if (elapsedTime >= RoundTimer)
        {
            startAnimating = false;
            targetCircle.fillAmount = 0;
        }
    }

}
