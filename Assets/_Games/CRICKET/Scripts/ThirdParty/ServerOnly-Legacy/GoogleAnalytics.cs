using UnityEngine;

public class GoogleAnalytics : MonoBehaviour
{
	public string propertyID;

	public static GoogleAnalytics instance;

	public string bundleID;

	public string appName;

	public string appVersion;

	private string screenRes;

	private string clientID;

	protected void Awake()
	{
		if ((bool)instance)
		{
			Object.DestroyImmediate(base.gameObject);
			return;
		}
		Object.DontDestroyOnLoad(base.gameObject);
		instance = this;
	}

	protected void Start()
	{
		screenRes = Screen.width + "x" + Screen.height;
		clientID = SystemInfo.deviceUniqueIdentifier;
	}

	public void LogEvent(string eventCategory, string eventAction)
	{
		eventCategory = WWW.EscapeURL(eventCategory);
		eventAction = WWW.EscapeURL(eventAction);
		string url = "http://www.google-analytics.com/collect?v=1&t=event&tid=" + propertyID + "&cid=" + WWW.EscapeURL(clientID) + "&ec=" + eventCategory + "&ea=" + eventAction;
		WWW wWW = new WWW(url);
	}

	public void LogScreen(string title)
	{
		title = WWW.EscapeURL(title);
		//string url = "http://www.google-analytics.com/collect?v=1&ul=en-us&t=appview&sr=" + screenRes + "&an=" + WWW.EscapeURL(appName) + "&a=448166238&tid=" + propertyID + "&aid=" + bundleID + "&cid=" + WWW.EscapeURL(clientID) + "&_u=.sB&av=" + appVersion + "&_v=ma1b3&cd=" + title + "&qt=2500&z=185";
		//WWW wWW = new WWW(url);
	}
}
