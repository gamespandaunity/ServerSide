using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeenPattiGame
{
    public class VoiceHolder : MonoBehaviour
    {
        [ShowOnly] public AudioClip voice;
        public TMP_Text voiceLength;
        public Image profilePic;
        public void AssignAudioClip(AudioClip audioClip, Sprite profileSpjrite)
        {
            voice = audioClip;
            int val = (int)voice.length;
            if (val <= 0)
                val = 1;
            voiceLength.text = val + "\"";
            profilePic.sprite = profileSpjrite;
        }

        public void PlayThisVoice()
        {
            VoiceRecorder.Instance.PlayThisAudioClip(voice);
        }
    }
}