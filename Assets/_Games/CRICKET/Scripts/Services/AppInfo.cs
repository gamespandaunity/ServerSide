public class AppInfo
{
	public static string AppName = "GamesPanda";

	public static string Version = "1.2.1";

	public static int BuildNumber = 18;

	public static int VersionCode = 1;

	public static string Platform = CONTROLLER.TargetPlatform;

	public static string senderID = "";

	public static string DeviceID = string.Empty;

	public static string deviceRegistrationID = string.Empty;

	public static string Flurry_Code
	{
		get
		{
			string result = string.Empty;
			if (Platform == "android")
			{
				result = "";
			}
			else if (Platform == "ios")
			{
				result = "";
			}
			return result;
		}
	}
}
