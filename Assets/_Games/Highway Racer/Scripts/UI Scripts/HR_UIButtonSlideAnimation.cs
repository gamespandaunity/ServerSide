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

/// <summary>
/// Slides the UI when enabled.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Button Slide Animation")]
public class HR_UIButtonSlideAnimation : MonoBehaviour {

    public SlideFrom slideFrom;     //  Which direction from slide?
    public enum SlideFrom { Left, Right, Top, Buttom }

    public bool actWhenEnabled = false;     //  Slide when enabled.
    public bool playSound = true;       //  Play audio when sliding.

    private RectTransform getRect;      //  Original position of the UI.
    private Vector2 originalPosition;
    public bool actNow = false;     //  Acting now?
    public bool endedAnimation = false;     //  Ended now?
    public HR_UIButtonSlideAnimation playWhenThisEnds;        //  Trigger this animation on end.

    private AudioSource slidingAudioSource;     //  Audio source.

    private void Awake() {

        //  Getting original position of the UI.
        getRect = GetComponent<RectTransform>();
        originalPosition = GetComponent<RectTransform>().anchoredPosition;

        //  Setting offset.
        SetOffset();

    }

    /// <summary>
    /// Setting offset of the UI first.
    /// </summary>
    private void SetOffset() {

        switch (slideFrom) {

            case SlideFrom.Left:
                GetComponent<RectTransform>().anchoredPosition = new Vector2(-2000f, originalPosition.y);
                break;
            case SlideFrom.Right:
                GetComponent<RectTransform>().anchoredPosition = new Vector2(2000f, originalPosition.y);
                break;
            case SlideFrom.Top:
                GetComponent<RectTransform>().anchoredPosition = new Vector2(originalPosition.x, 1000f);
                break;
            case SlideFrom.Buttom:
                GetComponent<RectTransform>().anchoredPosition = new Vector2(originalPosition.x, -1000f);
                break;

        }

    }

    private void OnEnable() {

        //  Setting offset and moving back to the original position.
        if (actWhenEnabled) {

            endedAnimation = false;
            SetOffset();
            Animate();

        }

    }

    /// <summary>
    /// Sliding animation.
    /// </summary>
    public void Animate() {

        //  Finding audiosource and playing audioclip. Otherwise create a new audio source.
        if (GameObject.Find(HR_HighwayRacerProperties.Instance.labelSlideAudioClip.name))
            slidingAudioSource = GameObject.Find(HR_HighwayRacerProperties.Instance.labelSlideAudioClip.name).GetComponent<AudioSource>();
        else
            slidingAudioSource = HR_CreateAudioSource.NewAudioSource(Camera.main.gameObject, HR_HighwayRacerProperties.Instance.labelSlideAudioClip.name, 0f, 0f, 1f, HR_HighwayRacerProperties.Instance.labelSlideAudioClip, false, false, true);

        //  Make sure audio source is not affected by pause and volume.
        slidingAudioSource.ignoreListenerPause = true;

        actNow = true;

    }

    private void Update() {

        if (!actNow || endedAnimation)
            return;

        if (playWhenThisEnds != null && !playWhenThisEnds.endedAnimation)
            return;

        if (slidingAudioSource && !slidingAudioSource.isPlaying && playSound)
            slidingAudioSource.Play();

        getRect.anchoredPosition = Vector2.MoveTowards(getRect.anchoredPosition, originalPosition, Time.unscaledDeltaTime * 5000f);

        if (Vector2.Distance(GetComponent<RectTransform>().anchoredPosition, originalPosition) < .05f) {

            if (slidingAudioSource && slidingAudioSource.isPlaying && playSound)
                slidingAudioSource.Stop();

            GetComponent<RectTransform>().anchoredPosition = originalPosition;

            if (GetComponentInChildren<HR_UICountAnimation>()) {

                if (!GetComponentInChildren<HR_UICountAnimation>().actNow)
                    GetComponentInChildren<HR_UICountAnimation>().Count();

            } else {

                endedAnimation = true;

            }

        }

        if (endedAnimation && !actWhenEnabled)
            enabled = false;

    }

    public void OnDisable() {

        if (slidingAudioSource)
            slidingAudioSource.Stop();

    }

    public void OnDrestroy() {

        if (slidingAudioSource)
            slidingAudioSource.Stop();

    }

}
