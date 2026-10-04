using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using AssemblyCSharp;
using LudoGame;

public class GameControllerScript : MonoBehaviour
{

    private Image imageClock1;
    private Image imageClock2;

    private Animator messageBubble;
    private Text messageBubbleText;

    private int currentImage = 1;

    public float playerTime;

    public float hideBubbleAfter = 3.0f;


    private float messageTime = 0;
    private AudioSource[] audioSources;
    private bool timeSoundsStarted = false;

    int loopCount = 0;

    private float waitingOpponentTime = 0;
    // Use this for initialization
    void Start()
    {

        LudoGame.GameManager.Instance.gameControllerScript = this;

        audioSources = GetComponents<AudioSource>();
        playerTime = LudoGame.GameManager.Instance.playerTime;
        imageClock1 = GameObject.Find("AvatarClock1").GetComponent<Image>();
        imageClock2 = GameObject.Find("AvatarClock2").GetComponent<Image>();

        messageBubble = GameObject.Find("MessageBubble").GetComponent<Animator>();
        messageBubbleText = GameObject.Find("BubbleText").GetComponent<Text>();

        if (LudoGame.GameManager.Instance.offlineMode)
        {
            GameObject.Find("Name1").GetComponent<Text>().text = StaticStrings.offlineModePlayer1Name;
            GameObject.Find("Name2").GetComponent<Text>().text = StaticStrings.offlineModePlayer2Name;
            GameObject.Find("Avatar2").GetComponent<Image>().color = Color.red;
        }
        else
        {
            GameObject.Find("Name1").GetComponent<Text>().text = LudoGame.GameManager.Instance.nameMy;
            if (LudoGame.GameManager.Instance.avatarMy != null)
                GameObject.Find("Avatar1").GetComponent<Image>().sprite = LudoGame.GameManager.Instance.avatarMy;

            GameObject.Find("Name2").GetComponent<Text>().text = LudoGame.GameManager.Instance.nameOpponent;

            if (LudoGame.GameManager.Instance.avatarOpponent != null)
                GameObject.Find("Avatar2").GetComponent<Image>().sprite = LudoGame.GameManager.Instance.avatarOpponent;
        }




        playerTime = playerTime * Time.timeScale;


        if (LudoGame.GameManager.Instance.roomOwner)
        {
            showMessage(StaticStrings.youAreBreaking);
        }
        else
        {
            showMessage(LudoGame.GameManager.Instance.nameOpponent + " " + StaticStrings.opponentIsBreaking);
        }

        if (!LudoGame.GameManager.Instance.roomOwner)
            currentImage = 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (!LudoGame.GameManager.Instance.stopTimer)
        {
            updateClock();
        }
    }


    private void updateClock()
    {
        float minus;
        if (currentImage == 1)
        {
            playerTime = LudoGame.GameManager.Instance.playerTime;
            if (LudoGame.GameManager.Instance.offlineMode)
                playerTime = LudoGame.GameManager.Instance.playerTime + LudoGame.GameManager.Instance.cueTime;
            minus = 1.0f / playerTime * Time.deltaTime;

            imageClock1.fillAmount -= minus;

            if (imageClock1.fillAmount < 0.25f && !timeSoundsStarted)
            {
                audioSources[0].Play();
                timeSoundsStarted = true;
            }

            if (imageClock1.fillAmount == 0)
            {

                audioSources[0].Stop();
                LudoGame.GameManager.Instance.stopTimer = true;
                if (!LudoGame.GameManager.Instance.offlineMode)
                {
                    //PhotonNetwork.RaiseEvent(9, null, true, null);
                }
                else
                {
                    LudoGame.GameManager.Instance.wasFault = true;
                    LudoGame.GameManager.Instance.cueController.setTurnOffline(true);
                }




                showMessage("You " + StaticStrings.runOutOfTime);

                if (!LudoGame.GameManager.Instance.offlineMode)
                {
                    LudoGame.GameManager.Instance.cueController.setOpponentTurn();
                }

            }

        }
        else
        {
            // Debug.Log(LudoGame.GameManager.Instance.opponentCueTime);
            playerTime = LudoGame.GameManager.Instance.playerTime;
            if (LudoGame.GameManager.Instance.offlineMode)
                playerTime = LudoGame.GameManager.Instance.playerTime + LudoGame.GameManager.Instance.opponentCueTime;
            minus = 1.0f / playerTime * Time.deltaTime;
            imageClock2.fillAmount -= minus;

            if (LudoGame.GameManager.Instance.offlineMode && imageClock2.fillAmount < 0.25f && !timeSoundsStarted)
            {
                audioSources[0].Play();
                timeSoundsStarted = true;
            }

            if (imageClock2.fillAmount == 0)
            {
                LudoGame.GameManager.Instance.stopTimer = true;

                if (LudoGame.GameManager.Instance.offlineMode)
                {
                    showMessage("You " + StaticStrings.runOutOfTime);
                }
                else
                {
                    showMessage(LudoGame.GameManager.Instance.nameOpponent + " " + StaticStrings.runOutOfTime);
                }


                if (LudoGame.GameManager.Instance.offlineMode)
                {
                    LudoGame.GameManager.Instance.wasFault = true;
                    LudoGame.GameManager.Instance.cueController.setTurnOffline(true);
                }
            }
        }

    }

    public void showMessage(string message)
    {

        float timeDiff = Time.time - messageTime;

        Debug.Log("Time diff: " + timeDiff);

        if (timeDiff > hideBubbleAfter + 1.0f)
        {
            messageBubbleText.text = message;
            messageBubble.Play("ShowBubble");
            if (!message.Contains(StaticStrings.waitingForOpponent))
                Invoke("hideBubble", hideBubbleAfter);
            else
            {
                waitingOpponentTime = StaticStrings.photonDisconnectTimeout;
                StartCoroutine(updateMessageBubbleText());
            }
            messageTime = Time.time;
        }
        else
        {
            Debug.Log("Show message with delay");
            StartCoroutine(showMessageWithDelay(message, (hideBubbleAfter + 1.0f - timeDiff) / 1.0f));
        }
    }

    public void hideBubble()
    {
        messageBubble.Play("HideBubble");
    }

    IEnumerator showMessageWithDelay(string message, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        messageBubbleText.text = message;

        messageBubble.Play("ShowBubble");
        if (!message.Contains(StaticStrings.waitingForOpponent))
            Invoke("hideBubble", hideBubbleAfter);
        else
        {
            waitingOpponentTime = StaticStrings.photonDisconnectTimeout;
            StartCoroutine(updateMessageBubbleText());
        }
        messageTime = Time.time;

    }

    public IEnumerator updateMessageBubbleText()
    {
        yield return new WaitForSeconds(1.0f * 2);
        waitingOpponentTime -= 1;
        if (!LudoGame.GameManager.Instance.opponentDisconnected)
        {
            if (!messageBubbleText.text.Contains("disconnected from room"))
                messageBubbleText.text = StaticStrings.waitingForOpponent + " " + waitingOpponentTime;
        }
        if (waitingOpponentTime > 0 && !LudoGame.GameManager.Instance.opponentActive && !LudoGame.GameManager.Instance.opponentDisconnected)
        {
            StartCoroutine(updateMessageBubbleText());
        }
    }

    public void stopSound()
    {
        audioSources[0].Stop();
    }

    public void resetTimers(int currentTimer, bool showMessageBool)
    {
        stopSound();
        timeSoundsStarted = false;
        imageClock1.fillAmount = 1;
        imageClock2.fillAmount = 1;

        this.currentImage = currentTimer;

        if (LudoGame.GameManager.Instance.offlineMode)
        {
            if (showMessageBool)
            {

                if (currentTimer == 2)
                {
                    showMessage(StaticStrings.offlineModePlayer2Name + " turn");
                }
                else
                {
                    showMessage(StaticStrings.offlineModePlayer1Name + " turn");
                }

            }

        }
        else
        {
            if (currentTimer == 1 && showMessageBool)
            {
                showMessage("It's your turn");
            }
        }


        LudoGame.GameManager.Instance.stopTimer = false;
    }


}
