using System;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using UnityEngine.UI;
using Cricket;
using UnityEngine.Serialization;

public class QuickPlayFreeEntry : MonoBehaviour
{
	[FormerlySerializedAs("loggedIn")] public bool isLoggedIn;

	[FormerlySerializedAs("canCheckFreeEntry")] public bool canCheckForFreeEntry;

	[FormerlySerializedAs("firstTime")] public bool isFirstTime;

	public static QuickPlayFreeEntry instance;

	[FormerlySerializedAs("timeOffset")] [HideInInspector]
	public long timeOffsetInMilliseconds;

	private bool isPowerUpgradeSet;

	private bool isControlUpgradeSet;

	private bool isAgilityUpgradeSet;

	private bool isPowerTimerEnded;

	private bool isControlTimerEnded;

	private bool isAgilityTimerEnded;

	private DateTime freeEntryExpiry;

	private DateTime doubleRewardsExpiry;

	private DateTime sevenDayRewardsExpiry;

	private DateTime dailyRewardExpiry;

	private DateTime powerUpgradeExpiry;

	private DateTime controlUpgradeExpiry;

	private DateTime agilityUpgradeExpiry;

	private DateTime freeSpinExpiry;

	private string sessionValue = string.Empty;

	private string[] upgradeExpirationPrefs = new string[3] { "ubPowerUpgrade", "ubControlUpgrade", "ubAgilityUpgrade" };

	private string[] upgradeTimerPrefs = new string[3] { "PowerUpgradeTimer", "ControlUpgradeTimer", "AgilityUpgradeTimer" };

	public TimeSpan powerUpgradeTime;

	public TimeSpan controlUpgradeTime;

	public TimeSpan agilityUpgradeTime;

	private DateTime retentionStartDateTime;

	private string retentionPrefsKey = "RetentionPrefs";

	private string retentionPrefsTimerKey = "RetentionPrefsTimer";

	public TimeSpan retentionStartTime;

	[FormerlySerializedAs("guiLabel")] public Text retentionGUILabel;

	private DateTime retentionEndDateTime;

	private string retentionEndPrefsKey = "RetentionPrefsEnd";

	private string retentionEndPrefsTimerKey = "RetentionPrefsTimerEnd";

	public TimeSpan retentionEndTime;

	private bool hasCalledOnceA;

	private bool hasCalledOnceB;

	private bool isPowerUpgradeCalledOnce;

	private bool isControlUpgradeCalledOnce;

	private bool isAgilityUpgradeCalledOnce;

	private bool isSpinCalledOnce;

	[FormerlySerializedAs("showSpinPopup")] public bool shouldShowSpinPopup;

	private void Awake()
	{
		if (instance == null)
		{
			canCheckForFreeEntry = false;
			instance = this;
			CustomStart();
			//StartUpgradeTimer(0);
			//StartSpinTimer();
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		StartRT_Timer();
	}

	public void StartRT_Timer()
	{
		if (!ObscuredPrefs.HasKey(retentionPrefsKey))
		{
			SessionStart();
			retentionStartDateTime = Now();
			WriteTimestamp(retentionPrefsKey, retentionStartDateTime);
			ObscuredPrefs.SetInt(retentionPrefsKey, 1);
		}
		else
		{
			SessionStart();
			retentionStartDateTime = ReadTimestamp(retentionPrefsKey, Now().AddSeconds(0.0));
		}
		retentionEndTime = Now() - retentionStartDateTime;
		if (!(retentionEndTime.TotalDays < 1.0))
		{
			if (retentionEndTime.TotalDays >= 1.0 && retentionEndTime.TotalDays < 2.0)
			{
				//FirebaseAnalyticsManager.instance.logEvent("Retention_Day1", "Retention", CONTROLLER.userID);
			}
			else if (retentionEndTime.TotalDays >= 2.0 && retentionEndTime.TotalDays < 5.0)
			{
				//FirebaseAnalyticsManager.instance.logEvent("Retention_Day4", "Retention", CONTROLLER.userID);
			}
			else if (retentionEndTime.TotalDays >= 5.0 && retentionEndTime.TotalDays < 8.0)
			{
				//FirebaseAnalyticsManager.instance.logEvent("Retention_Day7", "Retention", CONTROLLER.userID);
			}
			else if (retentionEndTime.TotalDays >= 8.0 && retentionEndTime.TotalDays < 15.0)
			{
				//FirebaseAnalyticsManager.instance.logEvent("Retention_Day14", "Retention", CONTROLLER.userID);
			}
			else if (retentionEndTime.TotalDays >= 15.0 && retentionEndTime.TotalDays < 31.0)
			{
				//FirebaseAnalyticsManager.instance.logEvent("Retention_Day30", "Retention", CONTROLLER.userID);
			}
			else if (retentionEndTime.TotalDays >= 1.0)
			{
				retentionStartDateTime = Now();
			}
		}
	}

	public void CustomStart()
	{
		if (ManageScene.activeSceneName() == "MainMenu" && ObscuredPrefs.HasKey("doubleRewards"))
		{
			if (!ObscuredPrefs.HasKey("ubDoubleRewards"))
			{
				int num = SetSessionTime();
				SessionStart();
				doubleRewardsExpiry = Now().AddSeconds(num);
				WriteTimestamp("ubDoubleRewards", doubleRewardsExpiry);
			}
			else
			{
				SessionStart();
				doubleRewardsExpiry = ReadTimestamp("ubDoubleRewards", Now().AddSeconds(0.0));
			}
		}
	}

	public DateTime GetDailyRewardUB()
	{
		return ReadTimestamp("DailyRewards", Now().AddSeconds(0.0));
	}

	public static float Tosingle(double value)
	{
		return (float)value;
	}

	public void FinishTime(int index)
	{
		switch (index)
		{
		case 1:
			powerUpgradeExpiry = Now();
			WriteTimestamp(upgradeExpirationPrefs[0], powerUpgradeExpiry);
			break;
		case 2:
			controlUpgradeExpiry = Now();
			WriteTimestamp(upgradeExpirationPrefs[1], controlUpgradeExpiry);
			break;
		case 3:
			agilityUpgradeExpiry = Now();
			WriteTimestamp(upgradeExpirationPrefs[2], agilityUpgradeExpiry);
			break;
		}
		//Singleton<PowerUps>.instance.timerStarted[index - 1] = false;
	}

	

	private void OnDisable()
	{
		SessionEnd();
		if (CONTROLLER.IsQuickPlayFree)
		{
			WriteTimestamp("ubFreeEntry", freeEntryExpiry);
		}
		if (CONTROLLER.powerUpgradeTimerStarted)
		{
			WriteTimestamp(upgradeExpirationPrefs[0], powerUpgradeExpiry);
		}
		if (CONTROLLER.controlUpgradeTimerStarted)
		{
			WriteTimestamp(upgradeExpirationPrefs[1], controlUpgradeExpiry);
		}
		if (CONTROLLER.agilityUpgradeTimerStarted)
		{
			WriteTimestamp(upgradeExpirationPrefs[2], agilityUpgradeExpiry);
		}
		if (ObscuredPrefs.HasKey(retentionPrefsKey))
		{
			WriteTimestamp(retentionPrefsKey, retentionStartDateTime);
		}
		if (ObscuredPrefs.HasKey(retentionEndPrefsKey))
		{
			WriteTimestamp(retentionEndPrefsKey, retentionEndDateTime);
		}
	}

	private int SetSessionTimeQP()
	{
		return 60;
	}

	private int SetSpinTime()
	{
		return 86400;
	}

	private int SetSessionTime()
	{
		return 3600;
	}

	private int SetOneDaySessionTime()
	{
		return 86400;
	}

	private void OnGUI()
	{
		if (ManageScene.activeSceneName() == "MainMenu")
		{
			TimeSpan timeSpan;
			if (ObscuredPrefs.HasKey("freeEntry"))
			{
				timeSpan = freeEntryExpiry - Now();
				if (timeSpan.TotalSeconds > 0.0)
				{
					hasCalledOnceA = false;
					string text = "00";
					string text2 = ((timeSpan.Seconds <= 9) ? ("0" + timeSpan.Seconds) : timeSpan.Seconds.ToString());
					string text3 = ((timeSpan.Minutes <= 9) ? ("0" + timeSpan.Minutes) : timeSpan.Minutes.ToString());
				}
				else
				{
					sessionValue = string.Empty;
					if (!hasCalledOnceA)
					{
						ObscuredPrefs.DeleteKey("ubFreeEntry");
						hasCalledOnceA = true;
					}
				}
			}
		
			timeSpan = doubleRewardsExpiry - Now();
		
		}
		if (ObscuredPrefs.HasKey(upgradeTimerPrefs[0]) && isPowerUpgradeSet)
		{
			TimeSpan timeSpan = (powerUpgradeTime = powerUpgradeExpiry - Now());
			if (timeSpan.TotalSeconds > 0.0)
			{
				isPowerUpgradeCalledOnce = false;
				//Singleton<PowerUps>.instance.TimerStarted(1);
				string text4 = ((timeSpan.Seconds <= 9) ? ("0" + timeSpan.Seconds) : timeSpan.Seconds.ToString());
				string text5 = ((timeSpan.Minutes <= 9) ? ("0" + timeSpan.Minutes) : timeSpan.Minutes.ToString());
				string text6 = ((timeSpan.Hours <= 9) ? ("0" + timeSpan.Hours) : timeSpan.Hours.ToString());
				string text7 = ((timeSpan.Days <= 9) ? ("0" + timeSpan.Days) : timeSpan.Days.ToString());
				if (timeSpan.Days > 1)
				{
					//Singleton<PowerUps>.instance.timerText[0].text = text7 + " " + LocalizationData.instance.getText(549);
					if (ManageScene.activeSceneName() == "MainMenu")
					{
						Singleton<GameModeTWO>.instance.timerText[0].text = text7 + " " + LocalizationData.localizationInstance.getText(549);
					}
				}
				else
				{
					//Singleton<PowerUps>.instance.timerText[0].text = text6 + ":" + text5 + ":" + text4;
					if (ManageScene.activeSceneName() == "MainMenu")
					{
						Singleton<GameModeTWO>.instance.timerText[0].text = text6 + ":" + text5 + ":" + text4;
					}
				}
			}
			else
			{
				sessionValue = string.Empty;
				powerUpgradeTime = new TimeSpan(0, 0, 0, 0);
				if (!isPowerUpgradeCalledOnce)
				{
					isPowerTimerEnded = true;
					//Singleton<PowerUps>.instance.TimerEnded(1);
					ObscuredPrefs.DeleteKey(upgradeExpirationPrefs[0]);
					isPowerUpgradeCalledOnce = true;
				}
			}
		}
		if (ObscuredPrefs.HasKey(upgradeTimerPrefs[1]) && isControlUpgradeSet)
		{
			TimeSpan timeSpan = (controlUpgradeTime = controlUpgradeExpiry - Now());
			if (timeSpan.TotalSeconds > 0.0)
			{
				isControlUpgradeCalledOnce = false;
				//Singleton<PowerUps>.instance.TimerStarted(2);
				string text8 = ((timeSpan.Seconds <= 9) ? ("0" + timeSpan.Seconds) : timeSpan.Seconds.ToString());
				string text9 = ((timeSpan.Minutes <= 9) ? ("0" + timeSpan.Minutes) : timeSpan.Minutes.ToString());
				string text10 = ((timeSpan.Hours <= 9) ? ("0" + timeSpan.Hours) : timeSpan.Hours.ToString());
				string text11 = ((timeSpan.Days <= 9) ? ("0" + timeSpan.Days) : timeSpan.Days.ToString());
				if (timeSpan.Days > 1)
				{
					//Singleton<PowerUps>.instance.timerText[1].text = text11 + " " + LocalizationData.instance.getText(549);
					if (ManageScene.activeSceneName() == "MainMenu")
					{
						Singleton<GameModeTWO>.instance.timerText[1].text = text11 + " " + LocalizationData.localizationInstance.getText(549);
					}
				}
				else
				{
					//Singleton<PowerUps>.instance.timerText[1].text = text10 + ":" + text9 + ":" + text8;
					if (ManageScene.activeSceneName() == "MainMenu")
					{
						Singleton<GameModeTWO>.instance.timerText[1].text = text10 + ":" + text9 + ":" + text8;
					}
				}
			}
			else
			{
				sessionValue = string.Empty;
				controlUpgradeTime = new TimeSpan(0, 0, 0, 0);
				if (!isControlUpgradeCalledOnce)
				{
					isControlTimerEnded = true;
					//Singleton<PowerUps>.instance.TimerEnded(2);
					ObscuredPrefs.DeleteKey(upgradeExpirationPrefs[1]);
					isControlUpgradeCalledOnce = true;
				}
			}
		}
		if (ObscuredPrefs.HasKey(upgradeTimerPrefs[2]) && isAgilityUpgradeSet)
		{
			TimeSpan timeSpan = (agilityUpgradeTime = agilityUpgradeExpiry - Now());
			if (timeSpan.TotalSeconds > 0.0)
			{
				isAgilityUpgradeCalledOnce = false;
				//Singleton<PowerUps>.instance.TimerStarted(3);
				string text12 = ((timeSpan.Seconds <= 9) ? ("0" + timeSpan.Seconds) : timeSpan.Seconds.ToString());
				string text13 = ((timeSpan.Minutes <= 9) ? ("0" + timeSpan.Minutes) : timeSpan.Minutes.ToString());
				string text14 = ((timeSpan.Hours <= 9) ? ("0" + timeSpan.Hours) : timeSpan.Hours.ToString());
				string text15 = ((timeSpan.Days <= 9) ? ("0" + timeSpan.Days) : timeSpan.Days.ToString());
				if (timeSpan.Days > 1)
				{
					//Singleton<PowerUps>.instance.timerText[2].text = text15 + " " + LocalizationData.instance.getText(549);
					if (ManageScene.activeSceneName() == "MainMenu")
					{
						Singleton<GameModeTWO>.instance.timerText[2].text = text15 + " " + LocalizationData.localizationInstance.getText(549);
					}
				}
				else
				{
					//Singleton<PowerUps>.instance.timerText[2].text = text14 + ":" + text13 + ":" + text12;
					if (ManageScene.activeSceneName() == "MainMenu")
					{
						Singleton<GameModeTWO>.instance.timerText[2].text = text14 + ":" + text13 + ":" + text12;
					}
				}
			}
			else
			{
				sessionValue = string.Empty;
				agilityUpgradeTime = new TimeSpan(0, 0, 0, 0);
				if (!isAgilityUpgradeCalledOnce)
				{
					isAgilityTimerEnded = true;
					//Singleton<PowerUps>.instance.TimerEnded(3);
					ObscuredPrefs.DeleteKey(upgradeExpirationPrefs[2]);
					isAgilityUpgradeCalledOnce = true;
				}
			}
		}
		if (!ObscuredPrefs.HasKey("freeSpinTimer"))
		{
			return;
		}
		if ((freeSpinExpiry - Now()).TotalSeconds > 0.0)
		{
			isSpinCalledOnce = false;
			return;
		}
		sessionValue = string.Empty;
		if (!isSpinCalledOnce)
		{
			ObscuredPrefs.DeleteKey("ubFreeSpin");
			ObscuredPrefs.DeleteKey("freeSpinTimer");
			isSpinCalledOnce = true;
			if (ManageScene.activeSceneName() == "MainMenu")
			{
				shouldShowSpinPopup = true;
			}
			else
			{
				shouldShowSpinPopup = true;
			}
		}
	}

	private DateTime ReadTimestamp(string key, DateTime defaultValue)
	{
		long num = Convert.ToInt64(ObscuredPrefs.GetString(key, "0"));
		if (num == 0)
		{
			if (key == "SevenDayRewards" || key == "DailyRewards")
			{
				isFirstTime = true;
			}
			else
			{
				isFirstTime = false;
			}
			return defaultValue;
		}
		return DateTime.FromBinary(num);
	}

	private void WriteTimestamp(string key, DateTime time)
	{
		ObscuredPrefs.SetString(key, time.ToBinary().ToString());
	}

	private void OnApplicationPause(bool pause)
	{
		if (pause)
		{
			SessionEnd();
			if (CONTROLLER.IsQuickPlayFree)
			{
				WriteTimestamp("ubFreeEntry", freeEntryExpiry);
			}
			if (CONTROLLER.powerUpgradeTimerStarted)
			{
				WriteTimestamp(upgradeExpirationPrefs[0], powerUpgradeExpiry);
			}
			if (CONTROLLER.controlUpgradeTimerStarted)
			{
				WriteTimestamp(upgradeExpirationPrefs[1], controlUpgradeExpiry);
			}
			if (CONTROLLER.agilityUpgradeTimerStarted)
			{
				WriteTimestamp(upgradeExpirationPrefs[2], agilityUpgradeExpiry);
			}
			if (ObscuredPrefs.HasKey("ubDoubleRewards"))
			{
				WriteTimestamp("ubDoubleRewards", doubleRewardsExpiry);
			}
			if (ObscuredPrefs.HasKey("SevenDayRewards"))
			{
				WriteTimestamp("SevenDayRewards", sevenDayRewardsExpiry);
			}
			if (ObscuredPrefs.HasKey("DailyRewards"))
			{
				WriteTimestamp("DailyRewards", dailyRewardExpiry);
			}
			if (ObscuredPrefs.HasKey("freeSpinTimer"))
			{
				WriteTimestamp("ubFreeSpin", freeSpinExpiry);
			}
			if (ObscuredPrefs.HasKey(retentionPrefsKey))
			{
				WriteTimestamp(retentionPrefsKey, retentionStartDateTime);
			}
			if (ObscuredPrefs.HasKey(retentionEndPrefsKey))
			{
				WriteTimestamp(retentionEndPrefsKey, retentionEndDateTime);
			}
		}
		else
		{
			SessionStart();
			if (CONTROLLER.IsQuickPlayFree)
			{
				freeEntryExpiry = ReadTimestamp("ubFreeEntry", Now().AddSeconds(0.0));
			}
			if (CONTROLLER.powerUpgradeTimerStarted)
			{
				powerUpgradeExpiry = ReadTimestamp(upgradeExpirationPrefs[0], Now().AddSeconds(0.0));
			}
			if (CONTROLLER.controlUpgradeTimerStarted)
			{
				controlUpgradeExpiry = ReadTimestamp(upgradeExpirationPrefs[1], Now().AddSeconds(0.0));
			}
			if (CONTROLLER.agilityUpgradeTimerStarted)
			{
				agilityUpgradeExpiry = ReadTimestamp(upgradeExpirationPrefs[2], Now().AddSeconds(0.0));
			}
			if (ObscuredPrefs.HasKey(retentionPrefsKey))
			{
				retentionStartDateTime = ReadTimestamp(retentionPrefsKey, Now());
			}
			if (ObscuredPrefs.HasKey(retentionEndPrefsKey))
			{
				retentionEndDateTime = ReadTimestamp(retentionEndPrefsKey, Now());
			}
		}
	}

	private void OnApplicationFocus(bool focus)
	{
		if (!focus)
		{
			SessionEnd();
			if (CONTROLLER.IsQuickPlayFree)
			{
				WriteTimestamp("ubFreeEntry", freeEntryExpiry);
			}
			if (CONTROLLER.powerUpgradeTimerStarted)
			{
				WriteTimestamp(upgradeExpirationPrefs[0], powerUpgradeExpiry);
			}
			if (CONTROLLER.controlUpgradeTimerStarted)
			{
				WriteTimestamp(upgradeExpirationPrefs[1], controlUpgradeExpiry);
			}
			if (CONTROLLER.agilityUpgradeTimerStarted)
			{
				WriteTimestamp(upgradeExpirationPrefs[2], agilityUpgradeExpiry);
			
			}
			if (ObscuredPrefs.HasKey(retentionPrefsKey))
			{
				WriteTimestamp(retentionPrefsKey, retentionStartDateTime);
			}
			if (ObscuredPrefs.HasKey(retentionEndPrefsKey))
			{
				WriteTimestamp(retentionEndPrefsKey, retentionEndDateTime);
			}
		}
		else
		{
			SessionStart();
			if (CONTROLLER.IsQuickPlayFree)
			{
				freeEntryExpiry = ReadTimestamp("ubFreeEntry", Now().AddSeconds(0.0));
			}
			if (CONTROLLER.powerUpgradeTimerStarted)
			{
				powerUpgradeExpiry = ReadTimestamp(upgradeExpirationPrefs[0], Now().AddSeconds(0.0));
			}
			if (CONTROLLER.controlUpgradeTimerStarted)
			{
				controlUpgradeExpiry = ReadTimestamp(upgradeExpirationPrefs[1], Now().AddSeconds(0.0));
			}
			if (CONTROLLER.agilityUpgradeTimerStarted)
			{
				agilityUpgradeExpiry = ReadTimestamp(upgradeExpirationPrefs[2], Now().AddSeconds(0.0));
			}
			if (ObscuredPrefs.HasKey(retentionPrefsKey))
			{
				retentionStartDateTime = ReadTimestamp(retentionPrefsKey, Now());
			}
			if (ObscuredPrefs.HasKey(retentionEndPrefsKey))
			{
				retentionEndDateTime = ReadTimestamp(retentionEndPrefsKey, Now());
			}
		}
	}

	private void OnApplicationQuit()
	{
		SessionEnd();
		if (CONTROLLER.IsQuickPlayFree)
		{
			WriteTimestamp("ubFreeEntry", freeEntryExpiry);
		}
		if (CONTROLLER.powerUpgradeTimerStarted)
		{
			WriteTimestamp(upgradeExpirationPrefs[0], powerUpgradeExpiry);
		}
		if (CONTROLLER.controlUpgradeTimerStarted)
		{
			WriteTimestamp(upgradeExpirationPrefs[1], controlUpgradeExpiry);
		}
		if (CONTROLLER.agilityUpgradeTimerStarted)
		{
			WriteTimestamp(upgradeExpirationPrefs[2], agilityUpgradeExpiry);
		}
		if (ObscuredPrefs.HasKey("ubDoubleRewards"))
		{
			WriteTimestamp("ubDoubleRewards", doubleRewardsExpiry);
		}
		if (ObscuredPrefs.HasKey("SevenDayRewards"))
		{
			WriteTimestamp("SevenDayRewards", sevenDayRewardsExpiry);
		}
		if (ObscuredPrefs.HasKey("DailyRewards"))
		{
			WriteTimestamp("DailyRewards", dailyRewardExpiry);
		}
		if (ObscuredPrefs.HasKey("freeSpinTimer"))
		{
			WriteTimestamp("ubFreeSpin", freeSpinExpiry);
		}
		if (!ObscuredPrefs.HasKey(retentionPrefsKey))
		{
			WriteTimestamp(retentionPrefsKey, retentionStartDateTime);
		}
		if (ObscuredPrefs.HasKey(retentionEndPrefsKey))
		{
			WriteTimestamp(retentionEndPrefsKey, retentionEndDateTime);
		}
	}

	public DateTime Now()
	{
		return DateTime.Now.AddSeconds(-1f * (float)timeOffsetInMilliseconds);
	}

	public void UpdateTimeOffset()
	{
		UpdateTimeOffsetAndroid();
	}

	public bool IsUsingSystemTime()
	{
		return UsingSystemTimeAndroid();
	}

	private void SessionStart()
	{
		StartAndroid();
	}

	private void SessionEnd()
	{
		EndAndroid();
	}

	// UnbiasedTime native plugin guard: `new AndroidJavaClass("com.vasilij.unbiasedtime.UnbiasedTime")`
	// throws AndroidJavaException (ClassNotFoundException) when the .aar isn't in the build — which
	// it currently isn't (seen repeatedly in device logs). The throw escaped these methods and could
	// interrupt the calling flow. Wrap each in try/catch and fall back to system time on failure.
	private void UpdateTimeOffsetAndroid()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		try
		{
			using AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			using AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("com.vasilij.unbiasedtime.UnbiasedTime");
			AndroidJavaObject @static = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
			if (@static != null && androidJavaClass2 != null)
			{
				timeOffsetInMilliseconds = androidJavaClass2.CallStatic<long>("vtcTimestampOffset", new object[1] { @static });
			}
		}
		catch (System.Exception)
		{
			timeOffsetInMilliseconds = 0; // plugin unavailable → system time
		}
	}

	private void StartAndroid()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		try
		{
			using AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			using AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("com.vasilij.unbiasedtime.UnbiasedTime");
			AndroidJavaObject @static = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
			if (@static != null && androidJavaClass2 != null)
			{
				androidJavaClass2.CallStatic("vtcOnSessionStart", @static);
				timeOffsetInMilliseconds = androidJavaClass2.CallStatic<long>("vtcTimestampOffset", new object[0]);
			}
		}
		catch (System.Exception)
		{
			timeOffsetInMilliseconds = 0; // plugin unavailable → system time
		}
	}

	private void EndAndroid()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		try
		{
			using AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			using AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("com.vasilij.unbiasedtime.UnbiasedTime");
			AndroidJavaObject @static = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
			if (@static != null)
			{
				androidJavaClass2?.CallStatic("vtcOnSessionEnd", @static);
			}
		}
		catch (System.Exception)
		{
			// plugin unavailable → nothing to end
		}
	}

	private bool UsingSystemTimeAndroid()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return true;
		}
		try
		{
			using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				using AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("com.vasilij.unbiasedtime.UnbiasedTime");
				AndroidJavaObject @static = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
				if (@static != null && androidJavaClass2 != null)
				{
					return androidJavaClass2.CallStatic<bool>("vtcUsingDeviceTime", new object[0]);
				}
			}
		}
		catch (System.Exception)
		{
			// plugin unavailable → assume system time
		}
		return true;
	}
}
