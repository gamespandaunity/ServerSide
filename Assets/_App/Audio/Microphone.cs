using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using Photon.Voice.Unity; //Photon Removal

public class MicrophoneToggle : MonoBehaviour
{
    public Sprite mute;
    public Sprite unMute;

    public Image ToggleButton;
    public bool isAudioOn=true;

    private void Start()
    {
        toggleFunction();
    }
    public void toggleFunction()
    {
        isAudioOn = !isAudioOn;
        UpdateUI();
    }
    public void UpdateUI()
    {
        if (isAudioOn)
        {
          //  ToggleButton.sprite = unMute;
            OnEnableMicrophone();
        }
        else
        {
            //ToggleButton.sprite = mute;
            OnDisableMicrophone();
        }
    }
    public void OnEnableMicrophone()
    {
        //Photon Removal  if (recorder != null)
        {
            //Photon Removal        recorder.TransmitEnabled = true;
        }
    }
   public void OnDisableMicrophone()
    {
        //Photon Removal    if(recorder != null)
        //Photon Removal   recorder.TransmitEnabled = false;

    }

    //Photon Removal public Recorder recorder;


}
