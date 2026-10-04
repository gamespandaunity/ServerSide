using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class OpenDoors : MonoBehaviour
{
    public List<DOTweenAnimation> Animations;
    
    void OnEnable()
    {
        HorseMobileButton.OnGameStarted += PlayDoorsAnimation;
    }

    private void PlayDoorsAnimation()
    {
        foreach (var animation in Animations)
        {
            animation.DOPlay();
        }
    }
    
    void OnDisable()
    {
        HorseMobileButton.OnGameStarted -= PlayDoorsAnimation;
    }
}
