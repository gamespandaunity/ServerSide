using UnityEngine;
using UnityEngine.Serialization;

public class SoundController : MonoBehaviour
{
	protected AudioSource crowdAudioSource;

	protected AudioSource mainAudioSource;

	protected AudioSource backgroundMusicSource;

	protected AudioSource buttonClickAudioSource;

	protected AudioSource coinAudioSource;

	protected AudioClip backgroundMusicClip;

	protected AudioClip crowdSoundClip;

	protected AudioClip slowMotionSoundClip;

	protected AudioClip buttonClickSoundClip;

	protected AudioClip coinSoundClip;

	protected AudioClip batHitSoundClip;

	protected AudioClip bowledSoundClip;

	protected AudioClip boundarySoundClip;

	protected AudioClip cheerSoundClip;

	protected AudioClip beatenSoundClip;

	protected AudioClip commentarySoundClip;

	protected AudioClip spinWheelSoundClip;

	[FormerlySerializedAs("timeDiff")] public float timeDifference;

	private bool hasPlayedOnce;
	public static SoundController soundControllerInstance;

    private void Awake()
	{
		CONTROLLER.sndController = this;
		backgroundMusicSource = base.gameObject.AddComponent<AudioSource>();
		backgroundMusicSource.playOnAwake = false;
		backgroundMusicSource.loop = true;
		crowdAudioSource = base.gameObject.AddComponent<AudioSource>();
		crowdAudioSource.playOnAwake = false;
		crowdAudioSource.loop = true;
		buttonClickAudioSource = base.gameObject.AddComponent<AudioSource>();
		buttonClickAudioSource.playOnAwake = false;
		coinAudioSource = base.gameObject.AddComponent<AudioSource>();
		coinAudioSource.playOnAwake = false;
		mainAudioSource = base.gameObject.AddComponent<AudioSource>();
		mainAudioSource.playOnAwake = false;
		if (SoundController.soundControllerInstance == null)
		{
			DontDestroyOnLoad(gameObject);
            soundControllerInstance=this;
        }
        else
		{
			Destroy(this.gameObject);
		}
	}

	private void Start()
	{
		buttonClickSoundClip = Resources.Load("Sound/ButtonSound") as AudioClip;
		coinSoundClip = Resources.Load("Sound/CoinSound") as AudioClip;
		crowdSoundClip = Resources.Load("Sound/CrowdSound") as AudioClip;
		backgroundMusicClip = Resources.Load("Sound/music") as AudioClip;
		batHitSoundClip = Resources.Load("Sound/BatSound") as AudioClip;
		spinWheelSoundClip = Resources.Load("Sound/wheel") as AudioClip;
	}

	protected void OnLevelWasLoaded(int index)
	{
		hasPlayedOnce = false;
		if ((index != 1 && index != 2) || hasPlayedOnce)
		{
			return;
		}
		hasPlayedOnce = true;
		if (CONTROLLER.bgMusicVal == 0)
		{
			if (Application.loadedLevelName == "MainMenu")
			{
				crowdAudioSource.Stop();
				crowdAudioSource.clip = null;
				backgroundMusicSource.Stop();
				backgroundMusicSource.clip = null;
			}
			else
			{
				backgroundMusicSource.Stop();
				backgroundMusicSource.clip = null;
			}
		}
		else if (Application.loadedLevelName == "MainMenu")
		{
			crowdAudioSource.Stop();
			crowdAudioSource.clip = null;
			backgroundMusicSource.clip = backgroundMusicClip;
			backgroundMusicSource.Play();
			backgroundMusicSource.volume = CONTROLLER.menuBgVolume;
		}
		else
		{
			backgroundMusicSource.Stop();
			backgroundMusicSource.clip = null;
		}
		if (Application.loadedLevelName == "Ground")
		{
			if (CONTROLLER.ambientVal == 0)
			{
				mainAudioSource.GetComponent<AudioSource>().Stop();
				mainAudioSource.clip = null;
				crowdAudioSource.Stop();
				crowdAudioSource.clip = null;
			}
			else if (CONTROLLER.ambientVal == 1)
			{
				crowdAudioSource.clip = crowdSoundClip;
				crowdAudioSource.Play();
				crowdAudioSource.volume = CONTROLLER.sfxVolume;
			}
		}
	}

	public void CallGarbageCollection()
	{
	}

	public void PlayButtonSnd()
	{
		// NavigationBack.Update fires this on EVERY tap; across a scene unload the click
		// AudioSource can be destroyed while the caller survives, and the clip setter then
		// throws (NavigationBack.Update -> PlayButtonSnd -> AudioSource NRE, 5 of the
		// 2026-07-20/21 cricket reports). A missing click sound is preferable to an NRE.
		if (buttonClickAudioSource == null)
		{
			return;
		}
		buttonClickAudioSource.clip = buttonClickSoundClip;
		if (CONTROLLER.ambientVal == 0)
		{
			buttonClickAudioSource.Stop();
			if (crowdAudioSource != null)
			{
				crowdAudioSource.clip = null;
			}
		}
		else
		{
			buttonClickAudioSource.Play();
			buttonClickAudioSource.volume = CONTROLLER.sfxVolume;
		}
	}

	public void PlayCoinSnd()
	{
		// Same destroyed-AudioSource hazard as PlayButtonSnd above.
		if (buttonClickAudioSource == null)
		{
			return;
		}
		buttonClickAudioSource.clip = coinSoundClip;
		if (CONTROLLER.ambientVal == 0)
		{
			buttonClickAudioSource.Stop();
			if (crowdAudioSource != null)
			{
				crowdAudioSource.clip = null;
			}
		}
		else
		{
			buttonClickAudioSource.Play();
			buttonClickAudioSource.volume = CONTROLLER.sfxVolume;
		}
	}

	public void updateBGMVolume()
	{
		backgroundMusicSource.volume = CONTROLLER.menuBgVolume;
	}

	public void updateSFXVolume()
	{
		mainAudioSource.volume = CONTROLLER.sfxVolume;
		crowdAudioSource.volume = CONTROLLER.sfxVolume;
	}

	public void PlayGameSnd(string SoundType)
	{
		if (CONTROLLER.ambientVal == 1)
		{
			float sfxVolume = CONTROLLER.sfxVolume;
			if (SoundType == "wheel")
			{
				spinWheelSoundClip = Resources.Load("Sound/wheel") as AudioClip;
				mainAudioSource.clip = spinWheelSoundClip;
				mainAudioSource.GetComponent<AudioSource>().PlayOneShot(spinWheelSoundClip, sfxVolume);
				backgroundMusicSource.volume = CONTROLLER.menuBgVolume;
			}
			if (SoundType == "Bat")
			{
				batHitSoundClip = Resources.Load("Sound/BatSound") as AudioClip;
				mainAudioSource.clip = batHitSoundClip;
				mainAudioSource.GetComponent<AudioSource>().PlayOneShot(batHitSoundClip, sfxVolume);
				backgroundMusicSource.volume = CONTROLLER.sfxVolume;
			}
			if (SoundType == "Bowled")
			{
				bowledSoundClip = Resources.Load("Sound/BowledSnd") as AudioClip;
				mainAudioSource.clip = bowledSoundClip;
				mainAudioSource.GetComponent<AudioSource>().PlayOneShot(bowledSoundClip, sfxVolume);
			}
			if (SoundType == "Boundary")
			{
				boundarySoundClip = Resources.Load("Sound/BoundarySnd") as AudioClip;
				mainAudioSource.clip = boundarySoundClip;
				mainAudioSource.GetComponent<AudioSource>().PlayOneShot(boundarySoundClip, sfxVolume);
			}
			if (SoundType == "Cheer")
			{
				cheerSoundClip = Resources.Load("Sound/Cheer") as AudioClip;
				mainAudioSource.clip = cheerSoundClip;
				mainAudioSource.GetComponent<AudioSource>().PlayOneShot(cheerSoundClip, sfxVolume);
			}
			if (SoundType == "Beaten")
			{
				beatenSoundClip = Resources.Load("Sound/BeatenSnd") as AudioClip;
				mainAudioSource.clip = beatenSoundClip;
				mainAudioSource.GetComponent<AudioSource>().PlayOneShot(beatenSoundClip, sfxVolume);
			}
			if (SoundType == "Won")
			{
				bowledSoundClip = Resources.Load("Sound/WinningSound") as AudioClip;
				mainAudioSource.clip = bowledSoundClip;
				mainAudioSource.GetComponent<AudioSource>().PlayOneShot(bowledSoundClip, sfxVolume);
			}
			if (SoundType == "Lost")
			{
				bowledSoundClip = Resources.Load("Sound/LosingSound") as AudioClip;
				mainAudioSource.clip = bowledSoundClip;
				mainAudioSource.GetComponent<AudioSource>().PlayOneShot(bowledSoundClip, sfxVolume);
			}
		}
	}

	public void PlayCommentarySnd(string SoundType)
	{
	}

	public void stopCommentary()
	{
	}

	public void bgMusicToggle()
	{
		if (CONTROLLER.bgMusicVal == 0)
		{
			backgroundMusicSource.Stop();
			backgroundMusicSource.clip = null;
		}
		else if (Application.loadedLevelName == "MainMenu" || Application.loadedLevelName == "Preloader")
		{
			if (backgroundMusicSource.clip == null)
			{
				backgroundMusicSource.clip = backgroundMusicClip;
				backgroundMusicSource.Play();
				backgroundMusicSource.volume = CONTROLLER.menuBgVolume;
			}
		}
		else if (crowdAudioSource.clip == null)
		{
			crowdAudioSource.clip = crowdSoundClip;
			crowdAudioSource.Play();
			crowdAudioSource.volume = CONTROLLER.sfxVolume;
		}
	}

	public void muteBGMVolume(float _vol)
	{
		backgroundMusicSource.volume = _vol;
	}

	public void ambientToggle()
	{
		if (CONTROLLER.ambientVal == 0)
		{
			if (Application.loadedLevelName == "Ground")
			{
				mainAudioSource.GetComponent<AudioSource>().Stop();
				mainAudioSource.clip = null;
				crowdAudioSource.Stop();
				crowdAudioSource.clip = null;
			}
		}
		else if (Application.loadedLevelName == "Ground")
		{
			crowdAudioSource.clip = crowdSoundClip;
			crowdAudioSource.Play();
			crowdAudioSource.volume = CONTROLLER.sfxVolume;
		}
	}

	public void RemoveGameSounds()
	{
		mainAudioSource.clip = null;
	}
}
