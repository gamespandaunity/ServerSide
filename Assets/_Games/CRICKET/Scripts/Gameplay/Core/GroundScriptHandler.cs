using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cricket;
using UnityEngine.Serialization;

public class GroundScriptHandler : MonoBehaviour
{
	private static GroundScriptHandler instance;

	[FormerlySerializedAs("BG")] public Image backgroundImage;

	[FormerlySerializedAs("GroundUI")] public GameObject groundUIObject;

	[FormerlySerializedAs("loadingScreen")] public GameObject loadingScreenObject;

	[FormerlySerializedAs("noInternetPopup")] public GameObject noInternetPopupObject;

	[FormerlySerializedAs("serverDisconnectedPopup")] public GameObject serverDisconnectedPopupObject;

	public static GroundScriptHandler Instance => instance;

	private void Start()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	public void ShowLoadingScreen()
	{
		groundUIObject.SetActive(value: true);
		loadingScreenObject.SetActive(value: true);
	}

	public void HideLoadingScreen()
	{
		groundUIObject.SetActive(value: false);
		loadingScreenObject.SetActive(value: false);
	}

	public void ShowNoInternetPopup()
	{
		groundUIObject.SetActive(value: true);
		noInternetPopupObject.SetActive(value: true);
	}

	public void HideNoInternetPopup()
	{
		groundUIObject.SetActive(value: false);
		serverDisconnectedPopupObject.SetActive(value: false);
		noInternetPopupObject.SetActive(value: false);
		//if (CONTROLLER.PlayModeSelected == 6)
		//{
		//	LoadMainMenuScene();
		//}
	}

	public void ShowServerDisconnectedPopup()
	{
		CONTROLLER.canPressBackBtn = true;
		groundUIObject.SetActive(value: true);
		noInternetPopupObject.SetActive(value: false);
		serverDisconnectedPopupObject.SetActive(value: true);
	}

	public void HideServerDisconnectedPopup()
	{
		backgroundImage.gameObject.SetActive(value: true);
		groundUIObject.SetActive(value: false);
		serverDisconnectedPopupObject.SetActive(value: false);
		LoadMainMenuScene();
	}

	public void LoadMainMenuScene()
	{
		CONTROLLER.MPInningsCompleted = false;
		Singleton<GameData>.instance.ResetCurrentMatchDetails();
		Singleton<GameData>.instance.ResetVariables();
		//Singleton<GameModel>.instance.ResetAllLocalVariables();
		CONTROLLER.PlayModeSelected = -1;
		ShowLoadingScreen();
		SceneManager.LoadSceneAsync("MainMenu");
	}
}
