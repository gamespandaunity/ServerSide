using Mirror;
using UnityEngine;
using UnityEngine.UI;
namespace Twelve
{
    public class TurnTimerTwelve : NetworkBehaviour
    {


        public bool isTimerRunning;
        public GameObject WarnigClock;
        private float startTime;

        public delegate void TurnTimerHasExpired();
        public static event TurnTimerHasExpired TunrnTimerHasExpired;

        [Header("Reference to a Text component for visualizing the countdown")]
        public Text Text;

        [Header("Countdown time in seconds")]
        public float roundTime = 5.0f;

        public float remainValue;
        float timer;
        float currentTime;

        // Server-only: tracks when the turn timer was paused due to a player drop.
        // -1 means "not paused". On resume, startTime is shifted forward by the
        // paused duration so the turn timer continues exactly where it left off.
        private float pauseStartTime = -1f;

        public void Start()
        {
            //if (Text == null)
            //{
            //    Debug.LogError("Reference to 'Text' is not set. Please set a valid reference.", this);
            //    return;
            //}
            WarnigClock.SetActive(false);
        }

        public void Update()
        {
            if (NetworkServer.active)
            {

                if (!isTimerRunning)
                {
                    return;
                }

                // Pause the turn timer while a player is disconnected. We track when the
                // pause began and, on resume, shift startTime forward by the paused duration
                // so the player gets back exactly the same remaining time.
                bool isMultiplayerPaused = GamePlayControllerTwelve.isOnlineMultiplayer
                    && NetworkGameManager.Instance != null
                    && NetworkGameManager.Instance.currentPlayerCount < 2;

                if (isMultiplayerPaused)
                {
                    if (pauseStartTime < 0f)
                        pauseStartTime = (float)NetworkTime.time;
                    return; // freeze: don't tick currentTime or push to SyncVar
                }

                if (pauseStartTime >= 0f)
                {
                    startTime += (float)NetworkTime.time - pauseStartTime;
                    pauseStartTime = -1f;
                }

                if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_Ai))
                {
                    timer = Time.time - startTime;
                    currentTime = roundTime - timer;
                    if (GamePlayControllerTwelve.isOnlineMultiplayer)
                        MultiPlayerGameManagerTwelve.instance.turnTimerSecond = currentTime;
                }
                else
                {
                    timer = (float)NetworkTime.time - startTime;
                    currentTime = roundTime - timer;
                    if (GamePlayControllerTwelve.isOnlineMultiplayer)
                        MultiPlayerGameManagerTwelve.instance.turnTimerSecond = currentTime;
                }

                int minutes = (int)(currentTime / 60);
                int seconds = (int)(currentTime % 60);

                if (Text)
                    Text.text = string.Format("{0:00} : {1:00}", minutes, seconds);
                if (currentTime <= 3)
                {
                    WarnigClock.SetActive(true);
                }
                remainValue = currentTime / roundTime;
                if (currentTime > 0.0f)
                {
                    return;
                }

                isTimerRunning = false;
                if (GamePlayControllerTwelve.isOnlineMultiplayer && MultiPlayerGameManagerTwelve.instance != null)
                    MultiPlayerGameManagerTwelve.instance.ServerExpireTurn();

                if (Text)
                    Text.text = string.Empty;
            }
            else if (GamePlayControllerTwelve.isOnlineMultiplayer)
            {
                // Online multiplayer client: display the server-authoritative timer value
                // from the turnTimerSecond SyncVar. The server owns and ticks the timer;
                // clients only mirror it here so both players see the same countdown.
                if (MultiPlayerGameManagerTwelve.instance == null) return;

                float synced = MultiPlayerGameManagerTwelve.instance.turnTimerSecond;

                int minutes = (int)(synced / 60);
                int seconds = (int)(synced % 60);
                if (Text)
                    Text.text = string.Format("{0:00} : {1:00}", minutes, seconds);

                WarnigClock.SetActive(synced > 0f && synced <= 3f);
                remainValue = roundTime > 0f ? synced / roundTime : 0f;
            }
            else
            {
                // Single-player / AI mode: local timer
                if (!isTimerRunning)
                {
                    return;
                }

                timer = Time.time - startTime;
                currentTime = roundTime - timer;

                int minutes = (int)(currentTime / 60);
                int seconds = (int)(currentTime % 60);

                if (Text)
                    Text.text = string.Format("{0:00} : {1:00}", minutes, seconds);
                if (currentTime <= 3)
                {
                    WarnigClock.SetActive(true);
                }
                remainValue = currentTime / roundTime;
                if (currentTime > 0.0f)
                {
                    return;
                }

                isTimerRunning = false;
                if (TunrnTimerHasExpired != null) TunrnTimerHasExpired();

                if (Text)
                    Text.text = string.Empty;
            }

        }

        public static void ClearTurnTimerEvent()
        {
            TunrnTimerHasExpired = null;
        }
        public void StopTimer()
        {
            isTimerRunning = false;
        }
        public void ResetRound()
        {
            currentTime = roundTime;
            if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_Ai))
            {
                startTime = Time.time;
            }
            else
            {
                startTime = (float)NetworkTime.time;

            }

            pauseStartTime = -1f; // clear any pending pause from previous turn
            isTimerRunning = true;
            WarnigClock.SetActive(false);

        }

    }
}
