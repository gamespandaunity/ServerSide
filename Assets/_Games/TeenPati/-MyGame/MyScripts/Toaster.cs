using UnityEngine;

public class Toaster : MonoBehaviour
{
    static string toastString;
    static AndroidJavaObject currentActivity;

    static void showToastOnUiThread(string toastString)
    {
        AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        toastString = toastString;

        currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(showToast));
    }

    static void showToast()
    {
        Debug.Log("Running on UI thread");
        AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
        AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", toastString);
        AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_SHORT"));
        toast.Call("show");
    }
    public static void ShowAToast(string YourText)
    {
        toastString = "";
        if (Application.platform == RuntimePlatform.Android)
        {
            toastString = YourText;
            showToastOnUiThread(toastString);
        }
        else
        {
            toastString = YourText;
            print("Toast: " + toastString);
        }
    }
}