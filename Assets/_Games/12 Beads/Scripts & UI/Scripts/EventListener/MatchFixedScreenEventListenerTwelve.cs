using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Snake_Ladder;

namespace Twelve
{
    public class MatchFixedScreenEventListenerTwelve : MonoBehaviour
    {
        public TMP_Text codeText;
        public bool playMatchingAnim;
        //public Button StartMatchBtn;
        private void OnEnable()
        {
            //Photon Removal   NetworkManagerTwelve.Instance.PlayerJoin += onPlayerjoin;
            //Photon Removal       NetworkManagerTwelve.Instance.StartGame += onStartGame;

            if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_RandomPlayer))
            {
                StartRolling();
                codeText.text = $"FINDING RANDOM PLAYER..";
            }
            //Photon Removal      else
            //Photon Removal   codeText.text = $"You Lobby Code:<color=green> {NetworkManagerTwelve.Instance.GetRoomCode()}</color>";
        }

        private void OnDisable()
        {
            //Photon Removal   NetworkManagerTwelve.Instance.PlayerJoin -= onPlayerjoin;
            //Photon Removal       NetworkManagerTwelve.Instance.StartGame -= onStartGame;
        }



        void onPlayerjoin()
        {
            //Photon Removal  if (NetworkManagerTwelve.Instance.IsAlone())
            {
                playMatchingAnim = true;
                StartRolling();
            }
            //Photon Removal  else
            {
                playMatchingAnim = false;
                sequence.Kill();
                Invoke(nameof(ChangeScreen), 0.5f);
            }
        }

        void ChangeScreen()
        {
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
                ConstantsData_M.Log("No sprites assigned!");
            }
        }

        void onStartGame()
        {
            // //Debug.Log($"From MatchFixing:{NetworkManager.Instance.GetRoomLockState()}");

            // if(NetworkManager.Instance.GetRoomLockState()) return;
            //Photon Removal   NetworkManagerTwelve.Instance.SetRoomLockState(true);
            SceneManager.LoadScene("12OnlineGameScene");
        }
    }
}
