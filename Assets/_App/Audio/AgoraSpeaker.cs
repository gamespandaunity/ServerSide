using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityExtensions;

public class AgoraSpeaker : MonoBehaviour
{
    public Sprite mute;
    public Sprite unMute;

    public Image ToggleButton;
    public bool isAudioOn = true;
    //Photon Removal   public Speaker speaker;
    //Photon Removal   public Recorder recorder;
    public UnityEvent OnSpeakerDisable, OnSpeakerEnable;

    private void OnEnable()
    {
        //Photon Removal    PunNetwork.OnAwaitingOpponent += Reconnects;
    }

    private void OnDisable()
    {
        //Photon Removal  PunNetwork.OnAwaitingOpponent -= Reconnects;

    }
    private void Start()
    {
        toggleFunction();
        //this.DelayUntil(() => recorder.GetComponentInChildren<Speaker>() != null, () =>
        //{
        //    if (speaker == null)
        //    {
        //        speaker = recorder.GetComponentInChildren<Speaker>();
        //    }                                                                                                                            //Photon Removal
        //    if (speaker != null)
        //    {
        //        speaker.enabled = isAudioOn;
        //        UpdateUI();
        //    }
        //    "Speaker found".Show();
        //});

    }


    public void toggleFunction()
    {
        //if (speaker == null)
        //{                                                                                                       //Photon Removal
        //    speaker = recorder.GetComponentInChildren<Speaker>();
        //}
        isAudioOn = !isAudioOn;
        UpdateUI();
    }
    public void UpdateUI()
    {
        if (isAudioOn)
        {
            ToggleButton.sprite = unMute;
            UnmuteSpeaker();
        }
        else
        {
            ToggleButton.sprite = mute;
            MuteSpeaker();
        }
    }

    public void MuteSpeaker()
    {
        //if (speaker != null)
        //{
        //    speaker.enabled = false;                                                                                                    //Photon Removal
        //    OnSpeakerDisable.Invoke();
        //}
    }

    public void UnmuteSpeaker()
    {
        //if (speaker != null)
        //{
        //    speaker.enabled = true;                                                                                                    //Photon Removal
        //    OnSpeakerEnable.Invoke();
        //}
    }

    public void Reconnects(bool value)
    {
        //value.Show("Speaker reconnects: ");
        if (value == false)
        {
            //if (speaker != null)
            //{
            //    speaker.enabled = isAudioOn;
            //    UpdateUI();                                                                                                                            //Photon Removal
            //    return;
            //}
            Start();
        }
        else
        {
            //if (speaker != null)
            //{                                                                                                                                              //Photon Removal
            //    isAudioOn = false;
            //    UpdateUI();
            //}
        }
    }
}
