using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cricket;

public class SettingsPageTWO : Singleton<SettingsPageTWO>
{
	public GameObject Holder;

	public GameObject LocalizationHolder;

	public Text Loc_Heading1;

	public Text Loc_Heading2;

	public Text Loc_Heading3;

	private int LanguageWhenOpened;

	public Button[] LanguageButtons;

	public Button LocalizeOKButton;

	public Text CurrentLanguageName;

	private string[] languageCode = new string[9] { "evsjv", "ENGLISH", "gaujrataI", "fganh", "ಕನ\u0ccdನಡ\u0cbfಗ", "aebmfw", "ekjrh", "jkpo;", "త\u0c46ల\u0c41గ\u0c41" };

	public Button[] soundBtns;

	public Slider BGMSlider;

	public Slider SFXSlider;

	public GameObject bgmSlider;

	public GameObject sfxSlider;

	public Text[] heading;

	private string previousPage;

	protected void Start()
	{
		GetPlayerPrefs.instance.getSettingsList();
		setSettingsPage();
		if (SceneManager.GetActiveScene().name == "Ground")
		{
			ValidateQualitySettings();
		}

	}





	private void ValidateQualitySettings()
	{
	}

	public void setBGMSound(float value)
	{
		setBGMVolume(value);
	}

	public void setSFXSound(float value)
	{
		setSFXVolume(value);
	}

	private void setSFXVolume(float _bgVol)
	{
		CONTROLLER.sfxVolume = _bgVol;
		CONTROLLER.sndController.updateSFXVolume();
	}

	private void setBGMVolume(float _bgVol)
	{
		CONTROLLER.menuBgVolume = _bgVol;
		CONTROLLER.sndController.updateBGMVolume();
	}

	private void setSliderPos()
	{
		BGMSlider.value = CONTROLLER.menuBgVolume;
		SFXSlider.value = CONTROLLER.sfxVolume;
	}

	private void setSettingsPage()
	{
		if (CONTROLLER.bgMusicVal == 1)
		{
			soundBtns[1].gameObject.GetComponent<CanvasRenderer>().SetAlpha(0f);
			heading[1].color = Color.white;
			soundBtns[0].gameObject.GetComponent<CanvasRenderer>().SetAlpha(1f);
			heading[0].color = Color.black;
			bgmSlider.SetActive(value: true);
		}
		else
		{
			soundBtns[1].gameObject.GetComponent<CanvasRenderer>().SetAlpha(1f);
			heading[1].color = Color.black;
			soundBtns[0].gameObject.GetComponent<CanvasRenderer>().SetAlpha(0f);
			heading[0].color = Color.white;
			bgmSlider.SetActive(value: true);
		}
		if (CONTROLLER.ambientVal == 1)
		{
			soundBtns[3].gameObject.GetComponent<CanvasRenderer>().SetAlpha(0f);
			heading[3].color = Color.white;
			soundBtns[2].gameObject.GetComponent<CanvasRenderer>().SetAlpha(1f);
			heading[2].color = Color.black;
			sfxSlider.SetActive(value: true);
		}
		else
		{
			soundBtns[3].gameObject.GetComponent<CanvasRenderer>().SetAlpha(1f);
			heading[3].color = Color.black;
			soundBtns[2].gameObject.GetComponent<CanvasRenderer>().SetAlpha(0f);
			heading[2].color = Color.white;
			sfxSlider.SetActive(value: true);
		}
		if (CONTROLLER.tutorialToggle == 1)
		{
			soundBtns[5].gameObject.GetComponent<CanvasRenderer>().SetAlpha(0f);
			heading[5].color = Color.white;
			soundBtns[4].gameObject.GetComponent<CanvasRenderer>().SetAlpha(1f);
			heading[4].color = Color.black;
		}
		else
		{
			soundBtns[5].gameObject.GetComponent<CanvasRenderer>().SetAlpha(1f);
			heading[5].color = Color.black;
			soundBtns[4].gameObject.GetComponent<CanvasRenderer>().SetAlpha(0f);
			heading[4].color = Color.white;
		}
		setSliderPos();
	}

	public void soundClicked(int index)
	{
		switch (index)
		{
			case 0:
				if (soundBtns[0].gameObject.GetComponent<CanvasRenderer>().GetAlpha() == 1f)
				{
					CONTROLLER.bgMusicVal = 0;
				}
				else
				{
					CONTROLLER.bgMusicVal = 1;
				}
				break;
			case 1:
				if (soundBtns[2].gameObject.GetComponent<CanvasRenderer>().GetAlpha() == 1f)
				{
					CONTROLLER.ambientVal = 0;
				}
				else
				{
					CONTROLLER.ambientVal = 1;
				}
				break;
			case 2:
				if (soundBtns[4].gameObject.GetComponent<CanvasRenderer>().GetAlpha() == 1f)
				{
					//Debug.Log("TUTORIAL HAI");
					CONTROLLER.tutorialToggle = 0;
					if (CONTROLLER.PlayModeSelected == 8)
					{
						//Singleton<GroundController>.instance.CallOppTutorial(false);
					}
				}
				else
				{
					//Debug.Log("TUTORIAL HAI YA NAHI");
					CONTROLLER.tutorialToggle = 0;
					if (CONTROLLER.PlayModeSelected == 8)
					{
						//Singleton<GroundController>.instance.CallOppTutorial(true);
					}
				}
				break;
			case 3:
				if (soundBtns[6].gameObject.GetComponent<CanvasRenderer>().GetAlpha() == 1f)
				{
					CONTROLLER.PlayerMode = false;
				}
				else
				{
					CONTROLLER.PlayerMode = true;
				}
				break;
		}
		if (CONTROLLER.sndController != null)
		{
			CONTROLLER.sndController.bgMusicToggle();
			CONTROLLER.sndController.ambientToggle();
		}
		setSettingsPage();
	}

	public void backSelected()
	{
		if (SceneManager.GetActiveScene().name == "MainMenu")
		{
			Singleton<GameModeTWO>.instance.ShowWithoutAnim();
			CONTROLLER.pageName = "landingPage";
			Singleton<GameModeTWO>.instance.displayGameMode(_bool: true);
		}
		else
		{
			hideMe();
			Singleton<GameData>.instance.AgainToGamePauseScreen();
			Singleton<PauseGameScreen>.instance.Hide(boolean: false);
		}
	}

	public void ChangeQualitySettings(int index)
	{
		QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
		ValidateQualitySettings();
	}

	public void showMe()
	{
		Singleton<NavigationBack>.instance.deviceBack = hideMe;
		previousPage = CONTROLLER.CurrentMenu;
		CONTROLLER.CurrentMenu = "settings";
		Holder.SetActive(value: true);
		CurrentLanguageName.text = languageCode[1];
		Singleton<SettingsPageTWOPanelTransition>.instance.panelTransition();
		if (SceneManager.GetActiveScene().name == "MainMenu")
		{
			Singleton<GameModeTWO>.instance.Holder.SetActive(value: false);
			CONTROLLER.pageName = "settings";
			//if (CONTROLLER.canShowbannerMainmenu == 1)
			//{
			//	Singleton<AdIntegrate>.instance.ShowAd();
			//}
		}
		else
		{
			CONTROLLER.pageName = "settingsGround";
		}
		setSettingsPage();
		if ((bool)GoogleAnalytics.instance)
		{
			GoogleAnalytics.instance.LogEvent("Game", "Settings");
		}
	}

	public void hideMe()
	{
		//Singleton<AdIntegrate>.instance.HideAd();
		SavePlayerPrefs.SetSettingsList();
		if (SceneManager.GetActiveScene().name == "MainMenu")
		{
			Singleton<GameModeTWO>.instance.ShowWithoutAnim();
			CONTROLLER.pageName = "landingPage";
		}
		else if (previousPage == "pauseScreen")
		{
			Singleton<PauseGameScreen>.instance.Hide(boolean: false);
		}
		//else if (previousPage == "SuperOverResult")
		//{
		//	Singleton<SuperOverResult>.instance.ShowMe();
		//}
		//else if (previousPage == "SuperChaseResult")
		//{
		//	Singleton<SuperChaseResult>.instance.ShowMe();
		//}
		Holder.SetActive(value: false);
	}
}
