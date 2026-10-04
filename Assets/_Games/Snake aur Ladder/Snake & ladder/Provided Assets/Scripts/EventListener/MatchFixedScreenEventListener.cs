using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
namespace Snake_Ladder
{
    public class MatchFixedScreenEventListener : MonoBehaviour
    {
        public TMP_Text codeText;
        public bool playMatchingAnim;
        //public Button StartMatchBtn;
        private void OnEnable()
        {
           // NetworkManager.Instance.PlayerJoin += onPlayerjoin;
          //  NetworkManager.Instance.StartGame += onStartGame;

            if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_RandomPlayer))
            {
                StartRolling();
                codeText.text = $"FINDING RANDOM PLAYER..";
            }
           // else
              //  codeText.text = $"Lobby Code:<color=green> {NetworkManager.Instance.GetRoomCode()}</color>";
        }

        private void OnDisable()
        {
          //  NetworkManager.Instance.PlayerJoin -= onPlayerjoin;
          //  NetworkManager.Instance.StartGame -= onStartGame;

            sequence.Kill();
        }

        void onPlayerjoin()
        {
          //  if (NetworkManager.Instance.IsAlone())
            {
                playMatchingAnim = true;
                //StartMatchBtn.interactable = false;
                StartRolling();
                Debug.Log("Player Animation");
            }
           // else
            {
                playMatchingAnim = false;
                sequence.Kill();
                Invoke(nameof(ChangeScreen), 0.5f);
                //StartMatchBtn.interactable = true;
                Debug.Log("Not Playing Animation");
            }
        }

        void ChangeScreen()
        {
            //MenuManager.Instance.ChangeState(MenuManager.AllMenus.GameplayScreen);
            //MenuManager.Instance.StartMultiPlayerGame();
            //SceneManager.LoadScene("OnlineGameScene");
        }

        public Image rollerImage;
        public Sprite[] sprites;
        public float rollDuration = 0.5f;
        public float delayBetweenSprites = 0.1f;

        private int currentSpriteIndex = 0;
        Sequence sequence;

        void StartRolling()
        {
            if (sprites.Length > 0)
            {
                // Set the initial sprite
                rollerImage.sprite = sprites[currentSpriteIndex];
                //rollerImage.SetNativeSize();

                // Move the image from below to create a rolling effect
                rollerImage.rectTransform.anchoredPosition = new Vector2(0, -rollerImage.rectTransform.rect.height);

                // Create a sequence of animations to roll through the sprites
                sequence = DOTween.Sequence();

                for (int i = 0; i < sprites.Length; i++)
                {
                    // Add a callback to change the sprite and animate its movement
                    sequence.AppendCallback(() =>
                    {
                        rollerImage.sprite = sprites[currentSpriteIndex];
                    })
                    .Append(rollerImage.rectTransform.DOAnchorPosY(0, rollDuration).SetEase(Ease.InOutCubic))
                    .AppendInterval(delayBetweenSprites)
                    .Append(rollerImage.rectTransform.DOAnchorPosY(-rollerImage.rectTransform.rect.height, rollDuration).SetEase(Ease.InOutCubic))
                    .AppendCallback(() =>
                    {
                        currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Length;
                    });
                }

                // Loop the sequence indefinitely
                sequence.SetLoops(-1);


            }
            else
            {
                Debug.LogError("No sprites assigned!");
            }
        }

        void onStartGame()
        {
          //  NetworkManager.Instance.SetRoomLockState(true);
            SceneManager.LoadScene("OnlineGameScene");
        }
    }
}