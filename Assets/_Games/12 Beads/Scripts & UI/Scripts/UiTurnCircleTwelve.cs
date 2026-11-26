using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Twelve
{
    public class UiTurnCircleTwelve : MonoBehaviour
    {
        public Image targetCircle;
        public float RoundTimer;

        bool startAnimating;
        float elapsedTime;
        float shownFill = 1f;

        void Start()
        {
            // 🔹 Get parent's Image component
            Image parentImage = GetComponentInParent<Image>();

            if (parentImage != null)
            {
                targetCircle = parentImage;
                Debug.Log("✅ Parent Image assigned successfully!");
            }
            else
            {
                Debug.LogWarning("⚠️ No Image component found in parent!");
            }
        }
        void OnEnable()
        {
            startAnimating = true;
            elapsedTime = 0f;
            shownFill = OnlineServerFill(out float serverFill) ? serverFill : 1f;
            if (targetCircle != null) targetCircle.fillAmount = shownFill;
        }

        // Tester 26 Nov ("the turn timer is not displayed on the player's profile"): online, the ring used to run its own
        // 15 s from the moment the object was switched on. Both rings are on in the scene, so they ran out while the
        // match was loading, and the first turn's ring (already on) was never restarted - it stayed empty. A turn that
        // stayed with the same player, a reconnect or a held clock never restarted it either. Online the ring now shows
        // the match server's turn clock (MultiPlayerGameManagerTwelve.turnTimerSecond), so it is full at every new turn,
        // stops while the clock is held and matches the opponent's phone.
        static bool OnlineServerFill(out float fill)
        {
            fill = 0f;
            if (!GamePlayControllerTwelve.isOnlineMultiplayer || NetworkServer.active) return false;
            var game = MultiPlayerGameManagerTwelve.Current;
            if (game == null) return false;
            float round = game.turnTimer != null && game.turnTimer.roundTime > 0f ? game.turnTimer.roundTime : 15f;
            fill = Mathf.Clamp01(game.turnTimerSecond / round);
            return true;
        }

        void Update()
        {
            if (targetCircle == null) return;

            if (OnlineServerFill(out float serverFill))
            {
                // The server's samples come several times a second: glide between them, jump on a new turn.
                if (Mathf.Abs(serverFill - shownFill) > 0.25f) shownFill = serverFill;
                else shownFill = Mathf.MoveTowards(shownFill, serverFill, Time.deltaTime * 0.5f);
                targetCircle.fillAmount = shownFill;
                return;
            }

            if (!startAnimating) return;

            // Pause the visual fill while a multiplayer opponent has dropped — otherwise
            // the circle keeps draining locally even though the server-side turn timer
            // is frozen, making it look like the timer never pauses.
            if (GamePlayControllerTwelve.isOnlineMultiplayer
                && NetworkGameManager.Instance != null
                && NetworkGameManager.Instance.currentPlayerCount < 2)
            {
                return;
            }

            // Animate the targetCircle from 1 to 0 in RoundTimer
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / RoundTimer;
            targetCircle.fillAmount = Mathf.Lerp(1, 0, progress);

            // Stop animating once the timer is complete
            if (elapsedTime >= RoundTimer)
            {
                startAnimating = false;
                targetCircle.fillAmount = 0;
            }
        }

    }
}
