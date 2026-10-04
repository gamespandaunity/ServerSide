//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Count Animation")]
public class HR_UICountAnimation : MonoBehaviour {

    private Text text;
    public string originalText;

    public float originalValue = 0f;
    public float targetValue = 0f;

    public bool actNow = false;
    public bool endedAnimation = false;

    private AudioSource countingAudioSource;

    private void Awake() {

        text = GetComponent<Text>();
        originalText = text.text;

    }

    public void GetNumber() {

        originalValue = float.Parse(text.text, System.Globalization.NumberStyles.Number);
        text.text = "0";

    }

    public void Count() {

        if (GameObject.Find(HR_HighwayRacerProperties.Instance.countingPointsAudioClip.name))
            countingAudioSource = GameObject.Find(HR_HighwayRacerProperties.Instance.countingPointsAudioClip.name).GetComponent<AudioSource>();
        else
            countingAudioSource = HR_CreateAudioSource.NewAudioSource(Camera.main.gameObject, HR_HighwayRacerProperties.Instance.countingPointsAudioClip.name, 0f, 0f, 1f, HR_HighwayRacerProperties.Instance.countingPointsAudioClip, true, true, true);

        countingAudioSource.ignoreListenerPause = true;
        countingAudioSource.ignoreListenerVolume = true;

        actNow = true;

    }

    private void Update() {

        if (!actNow || endedAnimation)
            return;

        if (countingAudioSource && !countingAudioSource.isPlaying)
            countingAudioSource.Play();

        targetValue = Mathf.MoveTowards(targetValue, originalValue, Time.unscaledDeltaTime * 3000f);

        text.text = targetValue.ToString("F0");

        if ((originalValue - targetValue) <= .05f) {

            if (countingAudioSource && countingAudioSource.isPlaying)
                countingAudioSource.Stop();

            text.text = originalValue.ToString("F0");

            if (GetComponentInParent<HR_UIButtonSlideAnimation>())
                GetComponentInParent<HR_UIButtonSlideAnimation>().endedAnimation = true;

            endedAnimation = true;

        }

        if (endedAnimation)
            enabled = false;

    }

}
