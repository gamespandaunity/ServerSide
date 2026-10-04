//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;

/// <summary>
/// Bomb with timer and SFX.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Misc/HR Bomb")]
public class HR_Bomb : MonoBehaviour {

    private HR_PlayerHandler handler;       //  Player.
    private Light bombLight;        //  Light.

    private float signalTimer = 0f;     //  Timer.

    private AudioSource bombTimerAudioSource;       //  SFX.
    private AudioClip bombTimerAudioClip { get { return HR_HighwayRacerProperties.Instance.bombTimerAudioClip; } }

    private void Start() {

        //  If game mode is bomb, enable it, Otherwise disable it.
        if (HR_GamePlayHandler.Instance) {

            if (HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.Bomb)
                gameObject.SetActive(true);
            else
                gameObject.SetActive(false);

        } else {

            gameObject.SetActive(false);
            return;

        }

        //  Getting player handler and creating light with SFX.
        handler = GetComponentInParent<HR_PlayerHandler>();
        bombTimerAudioSource = HR_CreateAudioSource.NewAudioSource(gameObject, "Bomb Timer AudioSource", 0f, 0f, .25f, bombTimerAudioClip, false, false, false);
        bombLight = GetComponentInChildren<Light>();
        bombLight.enabled = true;
        bombLight.intensity = 0f;

    }

    private void FixedUpdate() {

        //  If no player found, return.
        if (!handler)
            return;

        //  If bomb is not triggered, return.
        if (!handler.bombTriggered)
            return;

        //  Adjusting signal light timer.
        signalTimer += Time.fixedDeltaTime * Mathf.Lerp(5f, 1f, handler.bombHealth / 100f);

        //  Light.
        if (signalTimer >= .5f)
            bombLight.intensity = Mathf.Lerp(bombLight.intensity, 0f, Time.fixedDeltaTime * 50f);
        else
            bombLight.intensity = Mathf.Lerp(bombLight.intensity, .1f, Time.fixedDeltaTime * 50f);

        if (signalTimer >= 1f) {

            signalTimer = 0f;
            bombTimerAudioSource.Play();

        }

    }

}
