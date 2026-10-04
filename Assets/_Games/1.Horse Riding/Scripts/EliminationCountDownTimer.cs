using UnityEngine;
using UnityEngine.UI;

//using ExitGames.Client.Photon;
 
using UnityEngine.Serialization;

public class EliminationCountDownTimer //Photon Removal : MonoBehaviourPunCallbacks
{

    public const string CountdownStartTime = "EliminationCountDownTimer";

    /// <summary>
    /// OnCountdownTimerHasExpired delegate.
    /// </summary>
    public delegate void CountdownTimerHasExpired();

    /// <summary>
    /// Called when the timer has expired.
    /// </summary>
    public static event CountdownTimerHasExpired OnCountdownTimerHasExpired;

    private bool isTimerRunning;

    private float startTime;

    [FormerlySerializedAs("Text")]
    [Header("Reference to a Text component for visualizing the countdown")]
    public Text countdownText;

    [FormerlySerializedAs("Countdown")]
    [Header("Countdown time in seconds")]
    public float countdownDuration = 5.0f;


    public void Start()
    {
        if (countdownText == null)
        {
           // Constants_M.Log("Reference to 'Text' is not set. Please set a valid reference.", this);
            return;
        }
    }

    public void Update()
    {
        if (!isTimerRunning)
        {
            return;
        }

        //Photon Removal float timer = (float)PhotonNetwork.Time - startTime;
        //Photon Removal float countdown = countdownDuration - timer;

        //Photon Removal  countdownText.text = string.Format("{0}", countdown.ToString("n2"));

        //Photon Removal if (countdown > 0.0f)
        {
            return;
        }

        isTimerRunning = false;

        countdownText.text = string.Empty;

        if (OnCountdownTimerHasExpired != null)
        {
            OnCountdownTimerHasExpired();
        }
    }

    //public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    //{
    //    object startTimeFromProps;

    //    if (propertiesThatChanged.TryGetValue(CountdownStartTime, out startTimeFromProps))
    //    {                                                                                                                                                                                      //Photon Removal
    //        isTimerRunning = true;
    //        startTime = (float)startTimeFromProps;
    //    }
    //}
}

