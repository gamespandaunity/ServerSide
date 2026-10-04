using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI script for fading the panel on enable.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Canvas Fade")]
public class HR_UIFade : MonoBehaviour {

    public CanvasGroup canvasGroup;

    private void OnEnable() {

        StartCoroutine(Fade());

    }

    private IEnumerator Fade() {

        canvasGroup.alpha = 0f;

        float timer = 1f;

        while (timer > 0) {

            timer -= Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * 5f);
            yield return null;

        }

        canvasGroup.alpha = 1f;

    }

    private void OnDisable() {

        StopAllCoroutines();

    }

}
