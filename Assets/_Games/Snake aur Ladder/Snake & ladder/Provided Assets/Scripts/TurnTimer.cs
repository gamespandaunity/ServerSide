
using UnityEngine;
using UnityEngine.UI;

namespace Snake_Ladder
{
    public class TurnTimer : MonoBehaviour
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

        public void Start()
        {
            //if (Text == null)
            //{
            //    Debug.LogError("Reference to 'Text' is not set. Please set a valid reference.", this);
            //    return;
            //}
            Screen.orientation = ScreenOrientation.Portrait;

            WarnigClock.SetActive(false);
        }

        public void Update()
        {
            if (!isTimerRunning)
            {
                return;
            }

            if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_Ai))
            {
                timer = Time.time - startTime;
                currentTime = roundTime - timer;
            }
            else
            {
              //  timer = (float)PhotonNetwork.Time - startTime;
                currentTime = roundTime - timer;
            }




            //Text.text = string.Format("{0}", currentTime.ToString("n2"));

            int minutes = (int)(currentTime / 60); //Divide the guiTime by sixty to get the minutes.
            int seconds = (int)(currentTime % 60);//Use the euclidean division for the seconds.
                                                  //var fraction = (time * 100) % 100;

            //update the label value
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

        public void ResetRound()
        {
            currentTime = roundTime;
            if (SnakeGameManager.instance.currentGameMode.Equals(SnakeGameManager.GameMode.Against_Ai))
            {
                startTime = Time.time;
            }
            else
            {
                //startTime = (float)PhotonNetwork.Time;

            }

            isTimerRunning = true;
            WarnigClock.SetActive(false);

        }

    }
}