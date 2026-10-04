using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BallPool;
using DG.Tweening;
using BallPool.Mechanics;
using UnityEngine.Networking;
using System;
using UnityEngine.Serialization;
public class CircularTimeController : TimeController
{
    [FormerlySerializedAs("slider1")] public Image playerSlider; //Player
    [FormerlySerializedAs("slider2")] public Image aiSlider; //AI
    [FormerlySerializedAs("ballsPlayer")] public GameObject[] playerBalls;//f
    [FormerlySerializedAs("ballsOpponent")] public GameObject[] opponentBalls;//f
    [FormerlySerializedAs("shotSlider")] public GameObject shotPowerSlider;
    [FormerlySerializedAs("particleYoffset")] public float cueParticleYOffset;
    [FormerlySerializedAs("TurnChagerText")] public Text turnChangeText;
    [FormerlySerializedAs("textMove_speed")] public float textMoveSpeed;
    [FormerlySerializedAs("endValue")] public Vector3 textEndPosition;
    [FormerlySerializedAs("A_Source")] public AudioSource audioSource;
    [FormerlySerializedAs("turnSource")] public AudioSource turnAudioSource;
    [FormerlySerializedAs("TurnChangeSound")] public AudioClip turnChangeClip;
    [FormerlySerializedAs("timerSound")] public AudioClip timerClip;
    [FormerlySerializedAs("ballHighlighter")] public GameObject ballHighlighter;
    [FormerlySerializedAs("Instantiated_highlightBall")] public List<GameObject> instantiatedHighlightBalls = new List<GameObject>();


    bool isCurrentPlayerTurn;
    bool isPlayerTurn = false;
    public static bool isSoundPlaying = false;
    List<Ball> stripeBalls = new List<Ball>();
    List<Ball> solidBalls = new List<Ball>();

    public static CircularTimeController instance;

    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        for (int i = 0; i < 7; i++)
        {

            GameObject highlighterContainer = Instantiate(ballHighlighter);
            instantiatedHighlightBalls.Add(highlighterContainer);
            instantiatedHighlightBalls[i].gameObject.SetActive(false);

            instantiatedHighlightBalls[i].transform.position = Vector3.zero;
        }


        for (int i = 1; i < 9; i++)
        {
            solidBalls.Add(GameManager.instance.allBalls[i]);

        }
        for (int i = 9; i < 16; i++)
        {
            stripeBalls.Add(GameManager.instance.allBalls[i]);

        }



    }

    bool isPlayingSound = false;
    protected override void OnUpdateTimer(float time01)
    {
        // Turn-gated UI must not trust BallPoolPlayer.myTurn alone online: the players array is a
        // STATIC that survives the reconnect scene reload, so a turn that flipped while this device
        // was disconnected leaves myTurn stale-true until ApplyTurn runs (and the settling delay can
        // hold that off for seconds). Cross-check against the server's turn SyncVar, which resyncs
        // the moment the network object respawns — otherwise the power slider (the "UI cue") shows
        // on BOTH devices at once after a reconnect (tester issue 8, 2026-07-21).
        bool isMyTurnUi = BallPoolPlayer.mainPlayer != null && BallPoolPlayer.mainPlayer.myTurn;
        if (isMyTurnUi
            && BallPool.BallPoolGameLogic.isOnLine
            && MyEightBallNetwork.Instance != null
            && MyEightBallNetwork.Instance.CurrentTurnId != -1
            && staticVariables.UserProfiledata != null && staticVariables.UserProfiledata.user != null
            && MyEightBallNetwork.Instance.CurrentTurnId != staticVariables.UserProfiledata.user._id)
        {
            isMyTurnUi = false;
        }
        if (isMyTurnUi) //Player Turn ( Your Turn )
        {
            aiSlider.fillAmount = 0f;//f
            ApplyBallColorState(opponentBalls, true); //f
            ApplyBallColorState(playerBalls, false); // f

            playerSlider.color = Color.Lerp(Color.white, Color.white, Mathf.Clamp01(time01 + 0.1f));
            playerSlider.color = new Color(playerSlider.color.r, playerSlider.color.g, playerSlider.color.b, Mathf.Clamp01(1));
            playerSlider.fillAmount = 1.0f - time01;

            shotPowerSlider.SetActive(true);
            if (playerSlider.fillAmount <= 0.2 && isSoundPlaying == false)
            {
                ////print("paly sound");
                //ParticleSetter();
                isSoundPlaying = true;
                //A_Source.loop = true;
                //A_Source.clip = timerSound;
                //A_Source.Play();
                if (isPlayingSound == false)
                {
                    isPlayingSound = true;

                    if (GameModeManager.isAI)
                    {
                        audioSource.gameObject.SetActive(true);
                        audioSource.clip = CircularTimeController.instance.timerClip;
                        audioSource.loop = true;

                        audioSource.Play();
                        InitializeBallParticles();

                    }
                    else
                    {
                        //Photon Removal PunNetwork.instance.PlayTickSound(true);
                    }
                }
            }
            // Tick-loop teardown on timer RESET. The loop starts at <=5s remaining, but the only stop was
            // fillAmount == 0 (full expiry) — so when the turn changed / was re-granted while the tick was
            // looping (timeout turn-change, reconnect re-grant), the fresh timer sat at full and NEITHER
            // condition fired: the 5-second tick kept looping under the new turn (tester issue 4, 2026-07-21).
            // Same teardown as the expiry stop.
            else if (playerSlider.fillAmount * maxPlayTime > 5f && isPlayingSound)
            {
                isPlayingSound = false;
                isSoundPlaying = false;
                audioSource.Stop();
                audioSource.gameObject.SetActive(false);
                ToggleBallGlowEffects(false);
            }
            if (playerSlider.fillAmount == 0)
            {
                if (isPlayingSound == true)
                {
                    isPlayingSound = false;

                    if (GameModeManager.isAI)
                    {
                        audioSource.gameObject.SetActive(false);
                        audioSource.clip = CircularTimeController.instance.timerClip;
                        audioSource.loop = true;


                        ToggleBallGlowEffects(false);
                        audioSource.Stop();

                    }
                    else
                    {
                        //Photon Removal  PunNetwork.instance.PlayTickSound(false);
                    }


                }
                //A_Source.Stop();
                //A_Source.clip = null;
                //A_Source.loop = false;
                isSoundPlaying = false;
                //  DisableParticlesArondBalls(false);
                if (Targeting2DManager.instance != null)
                {
                    Targeting2DManager.instance.whiteBallScreenShow?.SetActive(false);
                }
            }


        }
        else //AI Turn 
        {

            //  slider1.fillAmount = 1.0f; //RAR                  previous code uncomment to revert
            playerSlider.fillAmount = 0f; // 

            shotPowerSlider.SetActive(false);//f
            ApplyBallColorState(opponentBalls, false); //f
            ApplyBallColorState(playerBalls, true); // f
            aiSlider.color = Color.Lerp(Color.white, Color.red, Mathf.Clamp01(time01 + 0.1f));
            aiSlider.color = new Color(playerSlider.color.r, playerSlider.color.g, playerSlider.color.b, Mathf.Clamp01(1));
            aiSlider.fillAmount = 1.0f - time01;
            if (aiSlider.fillAmount <= 0.2)
            {

                var loopEnabler = ballHighlighter.GetComponent<ParticleSystem>().main.loop;
                if (!loopEnabler)
                {
                    loopEnabler = true;
                }
                if (isPlayingSound == true)
                {
                    isPlayingSound = false;
                    //Photon Removal   PunNetwork.instance.PlayTickSound(false);

                }
               

            }
            if (aiSlider.fillAmount == 0)
            {
                if (isPlayingSound == true)
                {
                    isPlayingSound = false;
                    //Photon Removal     PunNetwork.instance.PlayTickSound(false);

                }

            }
        }

        if (BallPoolPlayer.mainPlayer != null)
        {
            bool currentPlayerTurn = BallPoolPlayer.mainPlayer.myTurn;
            isCurrentPlayerTurn = currentPlayerTurn;
            if (currentPlayerTurn != isPlayerTurn)
            {



                // Turn has changed, show the text
                isPlayerTurn = currentPlayerTurn;

                if (isPlayerTurn)
                {


                    turnAudioSource.PlayOneShot(turnChangeClip);
                    if (GameManager.instance.selectedBallText.text != null || GameManager.instance.selectedBallText.text != "")
                    {

                        GameManager.instance.selectedBallText.text = "";
                    }


                    turnChangeText.text = "YOUR TURN";
                    GameManager.instance.ActivateCueStick();
                    turnChangeText.rectTransform.DOAnchorPos(textEndPosition, textMoveSpeed, true).SetEase(Ease.OutQuad);
                    StartCoroutine(AnimateTextFadeOut());

                }
                else
                {
                    ToggleBallGlowEffects(false);
                    turnAudioSource.PlayOneShot(turnChangeClip);
                    if (GameManager.instance.selectedBallText.text != null || GameManager.instance.selectedBallText.text != "")
                    {
                        GameManager.instance.selectedBallText.text = "";
                    }


                    turnChangeText.text = "OPPONENT TURN";
                    GameManager.instance.ActivateCueStick();
                    turnChangeText.rectTransform.DOAnchorPos(textEndPosition, textMoveSpeed, true).SetEase(Ease.OutQuad);
                    StartCoroutine(AnimateTextFadeOut());

                }
            }
        }


    }
    private void ApplyBallColorState(GameObject[] balls, bool isActive) // if it's player turn then player balls in ui will be bright and opponent balls will be dull      {
    {


        foreach (GameObject ball in balls)
        {
            ball.SetActive(isActive);
        }
    }
    IEnumerator AnimateTextFadeOut()
    {
        //  isPlayerShowed = true;
        yield return new WaitForSeconds(1f);

        turnChangeText.text = "";
        turnChangeText.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -260f, 0);
        turnChangeText.rectTransform.DOKill();
    }
    public void ActivateStripeBallParticles(BallPoolPlayer player)
    {
        string[] activeBallsIds = player.GetActiveBallsIds();
        for (int i = 0; i < instantiatedHighlightBalls.Count; i++)
        {
            instantiatedHighlightBalls[i].transform.position = new Vector3(stripeBalls[i].position.x, stripeBalls[i].position.y, stripeBalls[i].position.z);
            instantiatedHighlightBalls[i].SetActive(true);
        }
    }
    public void ActivateSolidBallParticles(BallPoolPlayer player)
    {
        string[] activeBallsIds = player.GetActiveBallsIds();
        for (int i = 0; i < instantiatedHighlightBalls.Count; i++)
        {
            instantiatedHighlightBalls[i].transform.position = new Vector3(solidBalls[i].position.x, solidBalls[i].position.y, solidBalls[i].position.z);
            instantiatedHighlightBalls[i].SetActive(true);
        }
    }
    public void InitializeBallParticles()
    {
        if (AightBallPoolPlayer.mainPlayer.myTurn)
        {
            if (AightBallPoolPlayer.mainPlayer.isStripes == true && AightBallPoolPlayer.otherPlayer.isSolids == true)
            {

                if (AightBallPoolPlayer.mainPlayer.isStripes == true && AightBallPoolPlayer.otherPlayer.isSolids == true)
                {
                    ActivateStripeBallParticles(AightBallPoolPlayer.mainPlayer);
                }



            }
            else if (AightBallPoolPlayer.mainPlayer.isSolids == true && AightBallPoolPlayer.otherPlayer.isStripes == true)
            {

                ActivateSolidBallParticles(AightBallPoolPlayer.mainPlayer);
                // IF MAIN PLAYER TURN AND MAIN PLAYER HAS POTTED SOLIDS BALL   ===>  YOU ARE SOLIDS 
            }
        }
        else if (AightBallPoolPlayer.otherPlayer.myTurn)
        {
            if (AightBallPoolPlayer.otherPlayer.isStripes == true && AightBallPoolPlayer.mainPlayer.isSolids == true)
            {
                ActivateStripeBallParticles(AightBallPoolPlayer.otherPlayer);
            }
            else if (AightBallPoolPlayer.otherPlayer.isSolids == true && AightBallPoolPlayer.mainPlayer.isStripes == true)
            {
                ActivateStripeBallParticles(AightBallPoolPlayer.otherPlayer);
            }
        }
    }
    public void ToggleBallGlowEffects(bool isActive)
    {
        for (int i = 0; i < instantiatedHighlightBalls.Count; i++)
        {

            instantiatedHighlightBalls[i].SetActive(isActive);
            instantiatedHighlightBalls[i].transform.position = Vector3.zero;
        }
    }
    public void PlayGenericBallParticle()
    {
        var loopEnabler = CircularTimeController.instance.ballHighlighter.GetComponent<ParticleSystem>().main.loop;


        if (AightBallPoolPlayer.mainPlayer.myTurn)
        {
            if (AightBallPoolPlayer.mainPlayer.isSolids)
            {



                CircularTimeController.instance.ActivateSolidBallParticles(AightBallPoolPlayer.mainPlayer);
                //loopEnabler = false;

            }
            else if (AightBallPoolPlayer.mainPlayer.isStripes)
            {

                CircularTimeController.instance.ActivateStripeBallParticles(AightBallPoolPlayer.mainPlayer);
                //   loopEnabler = false;
            }
        }
        else
        {
            ToggleBallGlowEffects(false);
        }

    }
}
