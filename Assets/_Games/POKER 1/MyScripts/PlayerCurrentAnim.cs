//
using DG.Tweening;
//using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace POKER
{
    public class PlayerCurrentAnim : MonoBehaviour
    {
        PlayerInfo playerInfo;

        private RectTransform rectTransform;
        private Vector2 originalPos;


        private void OnEnable()
        {
            playerInfo = GetComponent<PlayerInfo>();
        }


        private void Start()
        {
            GameManager gameManager = GameManager.Instance;

            if (playerInfo.GetMineIndexInPlayerList() >= gameManager.position_availability.Length)
            {
                rectTransform = gameManager.shakeAnimationWhenPositionAvailFul;
                originalPos = rectTransform.anchoredPosition;
            }
            else
            {
                rectTransform = GetComponent<RectTransform>();
                originalPos = rectTransform.anchoredPosition;
            }
        }


        public void PackAnim()
        {
            playerInfo.PlayerDummyCardsToShowParent.SetActive(false);
            playerInfo.PlayerOrignalCardsToShowParent.SetActive(false);
            transform.DOScale(new Vector3(0.8f, 0.8f, 0.8f), 1).OnComplete(OnPackPlayer);
        }

        public void PlayShakeAnimation()
        {
            PlayAnimationOnMyAllInstances();
        }


        void PlayAnimationOnMyAllInstances()
        {
            float duration = 0.5f;
            float strength = 5f;

            // Shake on the X-axis
            Vector3 shakeStrength = new Vector3(strength, 0f, 0f);

            // Create a sequence
            Sequence sequence = DOTween.Sequence();

            // Reset position
            sequence.Append(rectTransform.DOAnchorPos(originalPos, 0f));

            // Call the Shake method on DOTween
            sequence.Append(rectTransform.DOShakeAnchorPos(duration, shakeStrength));

            // Play the sequence
            sequence.Play();
        }


        void OnPackPlayer()
        {
            GetComponent<Button>().interactable = false;
        }
    }
}