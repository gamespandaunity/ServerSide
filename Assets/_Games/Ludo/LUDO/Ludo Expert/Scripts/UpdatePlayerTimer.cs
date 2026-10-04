using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LudoGame;
using Mirror;

public class UpdatePlayerTimer : MonoBehaviour
{
    private float playerTime;
    public GameObject timerObject;
    private Image timer;
    private bool timeSoundsStarted;
    public AudioSource[] audioSources;
    public GameObject GUIController;
    public bool myTimer;
    public bool paused = false;
    // Use this for initialization
    void Start()
    {
        timer = gameObject.GetComponent<Image>();
        if(NetworkServer.active)
            {
            myTimer = false;
        }
    }

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    void OnEnable()
    {
        timer = gameObject.GetComponent<Image>();
    }

    public void Pause()
    {
        paused = true;
        audioSources[0].Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if (!paused)
            updateClock();
    }

    public void restartTimer()
    {
        paused = false;
        timer.fillAmount = 1.0f;
    }


    void OnDisable()
    {
        if (timer != null)
        {
            timer.fillAmount = 1.0f;
            paused = false;
            audioSources[0].Stop();
        }
    }

    private void updateClock()
    {
        float minus;

        playerTime = LudoGame.GameManager.Instance.playerTime;
        if (LudoGame.GameManager.Instance.offlineMode)
            playerTime = LudoGame.GameManager.Instance.playerTime + LudoGame.GameManager.Instance.cueTime;
        minus = 1.0f / playerTime * Time.deltaTime;

        timer.fillAmount -= minus;

        if (timer.fillAmount < 0.25f && !timeSoundsStarted)
        {
            audioSources[0].Play();
            timeSoundsStarted = true;
        }

        if (timer.fillAmount == 0)
        {

         //   Debug.Log("TIME 0");
            audioSources[0].Stop();
            LudoGame.GameManager.Instance.stopTimer = true;

          //  LudoGame.GameManager.Instance.currentPlayer.dice.GetComponent<GameDiceController>().ReduceGameUnMovedValue();

            if (!LudoGame.GameManager.Instance.offlineMode)
            {
                if (myTimer)
                {
                    Debug.Log("Timer call finish turn");
                    GUIController.GetComponent<GameGUIController>().SendFinishTurn();
                }
                //PhotonNetwork.RaiseEvent(9, null, true, null);
            }
            else
            {
                LudoGame.GameManager.Instance.wasFault = true;
                GUIController.GetComponent<GameGUIController>().SendFinishTurn();
            }




            //showMessage("You " + StaticStrings.runOutOfTime);

            /*if (!LudoGame.GameManager.Instance.offlineMode)
            {
                LudoGame.GameManager.Instance.cueController.setOpponentTurn();
            }*/

        }


    }
}
