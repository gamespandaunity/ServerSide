using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Cricket;
using UnityEngine.Serialization;

public class InterfaceHandler : MonoBehaviour
{
	public static InterfaceHandler instance;


	[FormerlySerializedAs("loading")] public LoadingPanelTransition loadingPanelTransition;

	[FormerlySerializedAs("GameModeSelGO")] public GameObject gameModeSelectionPanel;

	[FormerlySerializedAs("MultiplayerUIGO")] public GameObject multiplayerUIPanel;

	[FormerlySerializedAs("LoadingScreenGO")] public GameObject loadingScreenPanel;

	[FormerlySerializedAs("LoadingScreenCountdown")] public Text loadingScreenCountdownText;

	[FormerlySerializedAs("noInternetPopup")] public GameObject noInternetPopupPanel;

	[FormerlySerializedAs("noVideoPopup")] public GameObject noVideoPopupPanel;

	[FormerlySerializedAs("noPlayersFoundPopup")] public GameObject noPlayersFoundPopupPanel;

	[FormerlySerializedAs("gameAbandonedPopup")] public GameObject gameAbandonedPopupPanel;

	[FormerlySerializedAs("noCoinsPopup")] public GameObject noCoinsPopupPanel;

	[FormerlySerializedAs("duplicatePlayerPopup")] public GameObject duplicatePlayerPopupPanel;

	[FormerlySerializedAs("serverDisconnectedPopup")] public GameObject serverDisconnectedPopupPanel;

	[FormerlySerializedAs("disableOnlineSyncFailed")] public GameObject onlineSyncFailedPopupPanel;

	[FormerlySerializedAs("serverMaintenancePopup")] public GameObject serverMaintenancePopupPanel;

	[FormerlySerializedAs("gameUpdatePopup")] public GameObject gameUpdatePopupPanel;

	[FormerlySerializedAs("gameExitPopup")] public GameObject gameExitPopupPanel;

	[FormerlySerializedAs("roomNotFound")] public GameObject roomNotFoundPopup;

	[FormerlySerializedAs("signinfailed")] public GameObject signInFailedPopup;

	[FormerlySerializedAs("gameupdatePopupNoButton")] public GameObject gameUpdateNoButtonPopup;

	[FormerlySerializedAs("gameupdatePopupYesButton")] public GameObject gameUpdateYesButtonPopup;

	[FormerlySerializedAs("roomNotFoundErrorText")] public Text roomNotFoundErrorMessage;

	[FormerlySerializedAs("gameAbandonedPopupLabel")] public Text gameAbandonedPopupMessage;

	[FormerlySerializedAs("SignInFailedText")] public Text signInFailedMessage;

	[FormerlySerializedAs("gameupdatePopupYesButtonText")] public Text gameUpdateYesButtonText;

	[FormerlySerializedAs("gameupdatePopupNoButtonText")] public Text gameUpdateNoButtonText;

	[FormerlySerializedAs("popupCanvas")] public GameObject popupCanvasObject;

	[FormerlySerializedAs("gameUpdateTitle")] public Text gameUpdateTitleText;

	[FormerlySerializedAs("gameUpdateContent")] public Text gameUpdateContentText;

	[FormerlySerializedAs("bForceConnectMultiplayer")] public bool forceConnectToMultiplayer;

	private float loadingCountdownStartTime;

	private float loadingCountdownTotalTime = 30f;

	private void Start()
	{
		if (instance == null)
		{
			instance = this;
		}
		Screen.sleepTimeout = -2;
		forceConnectToMultiplayer = false;
	}

	private void Awake()
	{
		multiplayerUIPanel.SetActive(value: false);
		gameModeSelectionPanel.SetActive(value: true);
		loadingScreenPanel.SetActive(value: false);
	}

	public void Update()
	{
		if (loadingScreenCountdownText.enabled)
		{
			int num = (int)(loadingCountdownTotalTime - (Time.time - loadingCountdownStartTime));
			if (num <= 0)
			{
				HideLoadingScreen();
			}
			if (num < 0)
			{
				num = 0;
			}
			if ((float)num < loadingCountdownTotalTime - 4f)
			{
				loadingScreenCountdownText.text = num.ToString();
			}
		}
	}

	public void multiplayerButtonClickEvent()
	{
		////Debug.Log(string.Concat(Application.internetReachability, " ", NetworkReachability.NotReachable, " ", Singleton<AdIntegrate>.instance.CheckForInternet()));
		//if (Application.internetReachability != 0 && Singleton<AdIntegrate>.instance.CheckForInternet())
		//{
		//	//FirebaseAnalyticsManager.instance.logEvent("MainMenu_click", "MainMenu", CONTROLLER.userID);
		//	//FirebaseAnalyticsManager.instance.logEvent("Extras", new string[2] { "ExtrasAction", "MP_Clicked" });
		//	if (UpdateSettings.MultiplayerUpdate == 1)
		//	{
		//		ShowGameUpdatePopup();
		//		return;
		//	}
		//	if (UpdateSettings.MultiplayerMaintain == 1)
		//	{
		//		ShowServerMaintenancePopup();
		//		return;
		//	}
		//	if (CONTROLLER.username == string.Empty)
		//	{
		//		ShowSigninfailedPopup();
		//		return;
		//	}
		//	//OpenMultiplayer();
		//	CONTROLLER.GameStartsFromSave = false;
		//}
		//else
		//{
		//	_instance.ShowNoInternetPopup();
		//}
	}

	//public void OpenMultiplayer()
	//{
	//	bForceConnectMultiplayer = false;
	//	CONTROLLER.PlayModeSelected = 6;
	//	CONTROLLER.gameMode = string.Empty;
	//	StartCoroutine(ConnectToMultiplayer());
	//}

	//private IEnumerator ConnectToMultiplayer()
	//{
		//	//ShowLoadingScreen(bCanShowCountdown: true);
		//	//yield return StartCoroutine(NetworkManager.Instance.CheckInternetConnection());
		//	//if (NetworkManager.Instance.IsNetworkConnected)
		//	//{
		//	//	ServerManager.Instance.Connect();
		//	//	yield break;
		//	//}
		//	//HideLoadingScreen();
		//	//ShowNoInternetPopup();
	//}

	public IEnumerator LoadGroundScene()
	{
		//Singleton<AdIntegrate>.instance.HideAd();
		if (Multiplayer.roomType == 2)
		{
			Multiplayer.roomType = -1;
		}
		//Singleton<MultiplayerPage>.instance.backButton.SetActive(value: false);
		//Singleton<MultiplayerPage>.instance.lobbyPage.SetActive(value: false);
		Singleton<NavigationBack>.instance.deviceBack = null;
		loadingPanelTransition.PanelTransition1("Ground");
		yield return 0;
	}

	public void ShowMultiplayerMode()
	{
		//if (CONTROLLER.canShowbannerMainmenu == 1)
		//{
		//	Singleton<AdIntegrate>.instance.ShowAd();
		//}
		HideLoadingScreen();
		CONTROLLER.canPressBackBtn = true;
		CONTROLLER.CurrentPage = "multiplayerpage";
		//multiplayerMode.ShowMe(isTrue: true);
		gameModeSelectionPanel.SetActive(value: false);
		multiplayerUIPanel.SetActive(value: true);
		//Singleton<NavigationBack>.instance.deviceBack = Singleton<MultiplayerPage>.instance.ClickedBack;
		if (PlayerPrefs.HasKey("multiplayerteamlist"))
		{
			string @string = PlayerPrefs.GetString("multiplayerteamlist");
			XMLReader.ParseXML(@string);
		}
	
	}

	public void HideMultiplayerMode()
	{
		CONTROLLER.canPressBackBtn = true;
		CONTROLLER.CurrentPage = "splashpage";
		CONTROLLER.pageName = "landingPage";
		//multiplayerMode.ShowMe(isTrue: false);
		multiplayerUIPanel.SetActive(value: false);
		gameModeSelectionPanel.SetActive(value: true);
		Singleton<GameModeTWO>.instance.showMe();
		//if (ServerManager.Instance != null)
		//{
		//	//ServerManager.Instance.Disconnect();
		//	//ServerManager.Instance.ExitRoom();
		//}
		Screen.sleepTimeout = -2;
		CONTROLLER.gameMode = string.Empty;
		CONTROLLER.PlayModeSelected = -1;
	}

	private void Activate_multiplayerUIGO(bool flag)
	{
		multiplayerUIPanel.SetActive(flag);
	}

	private void ShowLoadingGO(bool flag)
	{
		loadingScreenPanel.SetActive(flag);
	}

	public void ShowLoadingScreen(bool bCanShowCountdown = false)
	{
		CONTROLLER.tempCanPressBackBtn = CONTROLLER.canPressBackBtn;
		CONTROLLER.canPressBackBtn = false;
		ShowLoadingGO(flag: true);
		Singleton<NavigationBack>.instance.disableDeviceBack = true;
		CONTROLLER.pageName = "LoadingMultiplayer";
		loadingCountdownStartTime = Time.time;
		if (bCanShowCountdown)
		{
			loadingScreenCountdownText.enabled = true;
			loadingScreenCountdownText.text = string.Empty;
		}
		else
		{
			loadingScreenCountdownText.enabled = false;
		}
	}

	public void HideLoadingScreen()
	{
		Singleton<NavigationBack>.instance.disableDeviceBack = false;
		CONTROLLER.canPressBackBtn = CONTROLLER.tempCanPressBackBtn;
		ShowLoadingGO(flag: false);
		CONTROLLER.pageName = "landingPage";
		loadingScreenCountdownText.enabled = false;
	}

	public void ShowNoInternetPopup()
	{
		Singleton<NavigationBack>.instance.tempDeviceBack = Singleton<NavigationBack>.instance.deviceBack;
		Singleton<NavigationBack>.instance.deviceBack = HideNoInternetPopup;
		noInternetPopupPanel.SetActive(value: true);
	}

	public void HideNoInternetPopup()
	{
		Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
		noInternetPopupPanel.SetActive(value: false);
		//if (CONTROLLER.PlayModeSelected == 6)
		//{
		//	HideMultiplayerMode();
		//	HideNoPlayersFoundPopup();
		//}
	}

	public void showNoVideoPopup()
	{
		Singleton<NavigationBack>.instance.tempDeviceBack = Singleton<NavigationBack>.instance.deviceBack;
		Singleton<NavigationBack>.instance.deviceBack = HideNoVideoPopup;
		instance.noVideoPopupPanel.SetActive(value: true);
		CONTROLLER.pageName = "noVideoMP";
	}

	public void HideNoVideoPopup()
	{
		Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
		instance.noVideoPopupPanel.SetActive(value: false);
		CONTROLLER.pageName = "mpPage1";
	}

	public void ShowNoPlayersFoundPopup()
	{
		Singleton<NavigationBack>.instance.tempDeviceBack = Singleton<NavigationBack>.instance.deviceBack;
		Singleton<NavigationBack>.instance.deviceBack = HideNoPlayersFoundPopup;
		if (CONTROLLER.CurrentPage == "InfoPopUp")
		{
			HideInfoPopUp();
		}
		CONTROLLER.pageName = "noplayers";
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		CONTROLLER.canPressBackBtn = true;
		noPlayersFoundPopupPanel.SetActive(value: true);
	}

	public void HideNoPlayersFoundPopup()
	{
		Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		noPlayersFoundPopupPanel.SetActive(value: false);
		HideMultiplayerMode();
		CONTROLLER.CurrentPage = "splashpage";
	}

	public void ShowRoomNotFoundPopup(string errorText)
	{
		Singleton<NavigationBack>.instance.tempDeviceBack = Singleton<NavigationBack>.instance.deviceBack;
		Singleton<NavigationBack>.instance.deviceBack = HideRoomNotFoundPopup;
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		roomNotFoundPopup.SetActive(value: true);
		roomNotFoundErrorMessage.text = errorText;
	}

	public void HideRoomNotFoundPopup()
	{
		Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		roomNotFoundPopup.SetActive(value: false);
	}

	public void ShowNoCoinsPopup()
	{
		Singleton<NavigationBack>.instance.tempDeviceBack = Singleton<NavigationBack>.instance.deviceBack;
		Singleton<NavigationBack>.instance.deviceBack = HideNoCoinsPopup;
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		CONTROLLER.CurrentPage = " PopupPage";
		noCoinsPopupPanel.SetActive(value: true);
	}

	public void HideNoCoinsPopup()
	{
		Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		noCoinsPopupPanel.SetActive(value: false);
	}

	public void ShowSigninfailedPopup()
	{
		Singleton<NavigationBack>.instance.tempDeviceBack = Singleton<NavigationBack>.instance.deviceBack;
		Singleton<NavigationBack>.instance.deviceBack = HideSignInfailedPopup;
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		signInFailedPopup.SetActive(value: true);
		signInFailedMessage.text = LocalizationData.localizationInstance.getText(656);
	}

	public void GooglSignIn()
	{
		forceConnectToMultiplayer = true;
	}

	public void HideSignInfailedPopup()
	{
		Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		signInFailedPopup.SetActive(value: false);
		CONTROLLER.CurrentPage = "splashpage";
	}

	public void ShowGameAbandonedPopup(string hostName)
	{
		Singleton<NavigationBack>.instance.tempDeviceBack = Singleton<NavigationBack>.instance.deviceBack;
		Singleton<NavigationBack>.instance.deviceBack = HideGameAbandonedPopup;
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		if (ManageScenes._instance.getCurrentLoadedSceneName() == "MainMenu")
		{
			HideLoadingScreen();
		}
		else if (ManageScenes._instance.getCurrentLoadedSceneName() == "Ground")
		{
			HideLoadingScreen();
		}
		gameAbandonedPopupMessage.text = LocalizationData.localizationInstance.getText(373);
		gameAbandonedPopupMessage.text = ReplaceText(gameAbandonedPopupMessage.text, hostName);
		gameAbandonedPopupPanel.SetActive(value: true);
	}

	private string ReplaceText(string original, string replace)
	{
		string result = string.Empty;
		//Debug.Log(original + " " + replace + " ");
		if (original.Contains("#"))
		{
			result = original.Replace("#", replace);
		}
		return result;
	}

	public void HideGameAbandonedPopup()
	{
		Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		gameAbandonedPopupPanel.SetActive(value: false);
		HideMultiplayerMode();
		CONTROLLER.CurrentPage = "splashpage";
	}

	public void ShowDuplicatePlayerPopup()
	{
		Singleton<NavigationBack>.instance.tempDeviceBack = Singleton<NavigationBack>.instance.deviceBack;
		Singleton<NavigationBack>.instance.deviceBack = HideDuplicatePlayerPopup;
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		duplicatePlayerPopupPanel.SetActive(value: true);
	}

	public void HideDuplicatePlayerPopup()
	{
		Singleton<NavigationBack>.instance.deviceBack = Singleton<NavigationBack>.instance.tempDeviceBack;
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		duplicatePlayerPopupPanel.SetActive(value: false);
		HideMultiplayerMode();
	}

	public void ShowServerDisconnectedPopup()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		if (serverDisconnectedPopupPanel != null)
		{
			serverDisconnectedPopupPanel.SetActive(value: true);
		}
	}

	public void HideServerDisconnectedPopup()
	{
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		serverDisconnectedPopupPanel.SetActive(value: false);
		HideNoPlayersFoundPopup();
	}

	public void ShowDisableOnlineSyncPopup()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
	}

	public void HideDisableOnlineSyncPopup()
	{
	}

	public void ShowDisableOnlineSyncFailedPopup()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		onlineSyncFailedPopupPanel.SetActive(value: true);
	}

	public void HideDisableOnlineSyncFailedPopup()
	{
		onlineSyncFailedPopupPanel.SetActive(value: false);
	}

	public void ShowInfoPopUp()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
	}

	public void HideInfoPopUp()
	{
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
	}

	public void ShowServerMaintenancePopup()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		serverMaintenancePopupPanel.SetActive(value: true);
	}

	public void HideServerMaintenancePopup()
	{
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		serverMaintenancePopupPanel.SetActive(value: false);
	}

	public void ShowGameUpdatePopup()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		gameUpdatePopupPanel.SetActive(value: true);
	}

	public void ShowStoreGameUpdate()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		Application.OpenURL(AppUpdate.AppUpdateUri);
	}

	public void HideGameUpdatePopup()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		gameUpdatePopupPanel.SetActive(value: false);
	}

	public void ShowGameExitPopup()
	{
		CONTROLLER.tempCurrentPage = CONTROLLER.CurrentPage;
		gameExitPopupPanel.SetActive(value: true);
	}

	public void HideGameExitPopup()
	{
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
		gameExitPopupPanel.SetActive(value: false);
	}

	public void GameExit()
	{
		Application.Quit();
	}

	public void HideAllPopups()
	{
		noInternetPopupPanel.SetActive(value: false);
		noPlayersFoundPopupPanel.SetActive(value: false);
		gameAbandonedPopupPanel.SetActive(value: false);
		noCoinsPopupPanel.SetActive(value: false);
		duplicatePlayerPopupPanel.SetActive(value: false);
		onlineSyncFailedPopupPanel.SetActive(value: false);
		serverMaintenancePopupPanel.SetActive(value: false);
		gameUpdatePopupPanel.SetActive(value: false);
		gameExitPopupPanel.SetActive(value: false);
		roomNotFoundPopup.SetActive(value: false);
		signInFailedPopup.SetActive(value: false);
		if (serverDisconnectedPopupPanel.activeSelf)
		{
			serverDisconnectedPopupPanel.SetActive(value: false);
			HideMultiplayerMode();
		}
		CONTROLLER.CurrentPage = CONTROLLER.tempCurrentPage;
	}
}
