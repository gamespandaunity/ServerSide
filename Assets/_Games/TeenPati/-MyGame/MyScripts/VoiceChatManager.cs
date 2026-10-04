using UnityEngine;
//using Photon.Voice.Unity;

public class VoiceChatManager : MonoBehaviour
{
    //private Recorder recorder;

    private void Start()
    {
        // Get the Recorder component from your scene
        //recorder = GetComponent<Recorder>();

        // Automatically create and initialize the microphone device
        //PhotonVoiceNetwork.CreateRecorder();

        // Start capturing audio from the microphone
        //recorder.TransmitEnabled = true;
    }

    // Call this method to stop transmitting voice
    public void StopTransmittingVoice()
    {
        //recorder.TransmitEnabled = false;
    }
}
