using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimateChipsOnClick : MonoBehaviour
{
    public GameObject[] Chips;
    public AnimationCurve MovingCurve;
    void Start()
    {
        for (int i = 0; i < Chips.Length; i++)
        {
            int t = i;
            Chips[i].GetComponent<Button>().onClick.AddListener(() => AnimateSelectedChip(t));
        }
    }

    public void AnimateSelectedChip(int index)
    {
        for (int i = 0; i < Chips.Length; i++)
        {
            if (index == i)
            {
                Chips[i].transform.DOScale(Vector3.one * 6f, 0.5f).SetEase(MovingCurve);
            }
            else
            {
                Chips[i].transform.DOScale(Vector3.one * 4.5f, 0.5f).SetEase(MovingCurve);
            }
        }
    }
}
