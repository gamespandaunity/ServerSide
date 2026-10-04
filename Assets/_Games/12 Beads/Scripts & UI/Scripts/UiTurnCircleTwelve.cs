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
            targetCircle.fillAmount = 1;
            elapsedTime = 0f;
        }

        void Update()
        {
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
