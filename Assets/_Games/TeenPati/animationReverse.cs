using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationReverse : MonoBehaviour
{

    DOTweenAnimation anim;
    // Start is called before the first frame update
    private void OnEnable()
    {
        if (anim == null)
            anim = GetComponent<DOTweenAnimation>();
        anim.DOPlayForward();
    }


    // Update is called once per frame
    public void ClickOnCloseBtn()
    {
        //anim.DOPlayBackwards().OnComplete(() => DisableObject());
        float delayTime = anim.delay;
        Invoke(nameof(DisableObject), delayTime);
        anim.DOPlayBackwards();
    }


    void DisableObject()
    {
        this.gameObject.SetActive(false);
        
    }
    
}
