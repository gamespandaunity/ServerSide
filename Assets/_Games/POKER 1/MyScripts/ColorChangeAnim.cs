using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace POKER
{
    public class ColorChangeAnim : MonoBehaviour
    {
        public Image Card;
        void Start()
        {
            Card = GetComponent<Image>();
            Color imgColor = Card.color;
            imgColor.a = 1f;
            Card.DOFade(0, 1.75f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
        }
    }
}