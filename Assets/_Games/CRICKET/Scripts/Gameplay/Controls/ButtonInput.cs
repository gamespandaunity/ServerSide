using UnityEngine;
using UnityEngine.Serialization;

public class ButtonInput : MonoBehaviour
{
	public static ButtonInput ButtonInputInstance;

	[FormerlySerializedAs("audioSource")] public AudioSource buttonAudioSource;

	protected void Awake()
	{
		if (CONTROLLER.bgMusicVal == 0)
		{
			buttonAudioSource.mute = true;
		}
		else
		{
			buttonAudioSource.mute = false;
		}
	}
}
