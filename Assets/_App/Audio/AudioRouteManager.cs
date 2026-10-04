using UnityEngine;

public class AudioRouteManager : MonoBehaviour
{
    private static AndroidJavaObject _audioManager;

    private void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // Access the Android AudioManager through Unity's Java bridge
            AndroidJavaObject unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = unityActivity.Call<AndroidJavaObject>("getApplicationContext");
            _audioManager = context.Call<AndroidJavaObject>("getSystemService", "audio");
        }
    }

    // Set audio route to speaker
    public static void SetAudioRouteToSpeaker()
    {
        if (Application.platform == RuntimePlatform.Android && _audioManager != null)
        {
            _audioManager.Call("setSpeakerphoneOn", true); // Enable speakerphone
            //Debug.Log("Audio route set to speakerphone.");
        }
    }

    // Set audio route to earpiece
    public void SetAudioRouteToEarpiece()
    {
        if (Application.platform == RuntimePlatform.Android && _audioManager != null)
        {
            _audioManager.Call("setSpeakerphoneOn", false); // Disable speakerphone
            _audioManager.Call("setMode", 3); // MODE_IN_COMMUNICATION
            //Debug.Log("Audio route set to earpiece.");
        }
    }
}
