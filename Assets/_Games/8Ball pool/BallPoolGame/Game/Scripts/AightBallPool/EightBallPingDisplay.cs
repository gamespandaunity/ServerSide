using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallPool
{
    /// <summary>
    /// Small CLIENT-side ping (network RTT) readout for the 8-ball pool scene, pinned to the
    /// bottom-left corner. Builds its own overlay canvas + label so it needs zero scene wiring.
    /// Hidden in AI / offline mode (ping is only meaningful online). Mirrors the Snooker readout
    /// (SnokerGameManager.Update: NetworkTime.rtt * 1000 + " ms").
    /// </summary>
    public class EightBallPingDisplay : MonoBehaviour
    {
        private TextMeshProUGUI label;

        /// <summary>Spawns the display once. Safe to call from the 8-ball game manager's Start().</summary>
        public static void Spawn()
        {
            // Never spawn on a headless / dedicated server (no rendering, no local RTT).
            if (Application.isBatchMode)
                return;
            new GameObject("EightBallPingDisplay").AddComponent<EightBallPingDisplay>();
        }

        private void Awake()
        {
            // Own overlay canvas — independent of the game's UI, always on top, bottom-left corner.
            var canvasGo = new GameObject("PingCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f; // match height (landscape-only project)

            var raycaster = canvasGo.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false; // never eat touches

            var textGo = new GameObject("PingText");
            textGo.transform.SetParent(canvasGo.transform, false);
            label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = string.Empty;
            label.fontSize = 40f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.raycastTarget = false;

            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(2f, -2f);

            var rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(22f, 20f);
            rt.sizeDelta = new Vector2(340f, 70f);
        }

        private void Update()
        {
            if (label == null)
                return;

            // Online only, and hidden in AI mode — exactly like the Snooker ping readout.
            bool show = BallPoolGameLogic.isOnLine && !GameModeManager.isAI && NetworkClient.active;

            if (label.gameObject.activeSelf != show)
                label.gameObject.SetActive(show);

            if (show)
                label.text = ((int)(NetworkTime.rtt * 1000)).ToString() + " ms";
        }
    }
}
