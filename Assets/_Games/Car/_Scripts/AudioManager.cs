namespace CarRace
{
    using UnityEngine.Audio;
    using UnityEngine;
    using UnityEngine.Serialization;

    public class AudioManager : MonoBehaviour
    {
        [FormerlySerializedAs("audioSource")][SerializeField] AudioSource uiAudioSource;
        [FormerlySerializedAs("clickNormal")][SerializeField] AudioClip buttonClickSound ;
        [FormerlySerializedAs("clickConfirm")][SerializeField] AudioClip confirmClickSound;
        [FormerlySerializedAs("clickCancel")][SerializeField] AudioClip cancelClickSound;
        [FormerlySerializedAs("clickToggle")][SerializeField] AudioClip toggleClickSound;
        [FormerlySerializedAs("clipError")][SerializeField] AudioClip errorAlertSound ;
        [FormerlySerializedAs("clipPurchase")][SerializeField] AudioClip purchaseSuccessSound ;
        [FormerlySerializedAs("chatNotification")][SerializeField] AudioClip chatNotificationSound ;

        public void PlaySound(string clipName = "Normal")
        {
            switch (clipName)
            {
                case "Normal":
                    uiAudioSource.clip = buttonClickSound ;
                    break;
                case "Confirm":
                    uiAudioSource.clip = confirmClickSound;
                    break;
                case "CLickCancel":
                    uiAudioSource.clip = cancelClickSound;
                    break;
                case "Toggle":
                    uiAudioSource.clip = toggleClickSound;
                    break;
                case "Error":
                    uiAudioSource.clip = errorAlertSound ;
                    break;
                case "Purchase":
                    uiAudioSource.clip = purchaseSuccessSound ;
                    break;
                case "Chat":
                    uiAudioSource.clip = chatNotificationSound ;
                    break;
                default:
                    uiAudioSource.clip = buttonClickSound ;
                    break;
            }

            uiAudioSource.Play();
        }

    }

}