using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Mirror;
using UnityExtensions;

namespace Twelve
{
    public class GameplayUIEventListnerTwelve : MonoBehaviour
    {
        [FormerlySerializedAs("myAvatar")] public Image playerAvatarImage;
        [FormerlySerializedAs("OpponentAvatar")] public Image opponentAvatarImage;
        [FormerlySerializedAs("retryBtn")] public GameObject retryButton;
        public static Sprite opponentSprite;

        void Start()
        {
            SetPlayerUI();
            var mode = Snake_Ladder.GameManager.instance.currentGameMode;
            if (mode == Snake_Ladder.GameManager.GameMode.Against_OnlineFriend
                || mode == Snake_Ladder.GameManager.GameMode.Against_RandomPlayer)
            {
                retryButton.SetActive(false);
            }
        }

        void SetPlayerUI()
        {
            if (staticVariables.ProfilePicture)
                playerAvatarImage.sprite = Sprite.Create(staticVariables.ProfilePicture, new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height), Vector2.zero);

            var mode = Snake_Ladder.GameManager.instance.currentGameMode;
            bool isOnline = mode == Snake_Ladder.GameManager.GameMode.Against_OnlineFriend
                         || mode == Snake_Ladder.GameManager.GameMode.Against_RandomPlayer;

            if (isOnline && staticVariables.opponentImage != null)
            {
                Debug.Log("Friend Image Set");
                opponentAvatarImage.sprite = Sprite.Create(staticVariables.opponentImage, new Rect(0, 0, staticVariables.opponentImage.width, staticVariables.opponentImage.height), Vector2.zero);
                opponentSprite = opponentAvatarImage.sprite;
            }
            else
            {
                Debug.Log("AI Image Set");
                opponentAvatarImage.sprite = Sprite.Create(SpritesManager.Instance.spritesScriptable.aiImg, new Rect(0, 0, SpritesManager.Instance.spritesScriptable.aiImg.width, SpritesManager.Instance.spritesScriptable.aiImg.height), Vector2.zero);
                opponentSprite = opponentAvatarImage.sprite;
            }
        }

        public static bool IsGameQuite;

        public void HandleQuitButtonClick()
        {
            IsGameQuite = true;
            TwelveBeadSoundManager.instance.PlayAnySound(10);

            if (NetworkServer.active || NetworkClient.active)
            {
                StartCoroutine(LeaveOnlineGame());
            }
            else
            {
                SceneManager.LoadScene("Home");
            }
        }

        private IEnumerator LeaveOnlineGame()
        {
            if (NetworkGameManager.Instance)
                NetworkGameManager.Instance.RequestExplicitLeave();

            // One frame was not enough. Mirror queues the Command and the transport still has to
            // deliver it, so tearing the connection down this quickly lost the Command outright: the
            // server never learned the player CHOSE to leave, and settled the match through the
            // abandon watch instead, telling the winner their opponent had lost connection. Every
            // other game already waits half a second here for exactly this reason.
            yield return new WaitForSecondsRealtime(0.5f);

            if (NetworkServer.active)
                NetworkManager.singleton.StopHost();
            else
                NetworkManager.singleton.StopClient();

            float timeout = 3f;
            float waited = 0f;
            while ((NetworkServer.active || NetworkClient.active) && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (NetworkServer.active || NetworkClient.active)
                Debug.LogWarning("[12Beads] Network did not shut down within timeout, loading Home anyway.");

            Debug.Log("Quit Online Game");
            SceneManager.LoadScene("Home");
        }
    }
}
